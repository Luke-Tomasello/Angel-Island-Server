/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
 *   email                   : luke@tomasello.com
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

/* Scripts\Engines\Spawner\PBSDistrictManager.cs
 * CHANGELOG:
 *	9/8/22, Adam
 *	    First time check in
 *	    Add [ListActiveDistricts
 *	        Lists the active districts along with some info about the pbs-web that makes up the district
 *	    Add [SpawnDistricts
 *	        This is the real workhorse routine that enumerates districts and spawns guardians for the mobile there.
 *	        Example console output:
 *	            'Spawner 10 vendor guardians for zone (1444, 3670)+(47, 22)'
 *	            In reading this - 10 vendor guardians, means a 'web' of 10 PBS.
 *	            This means, to conquer that a particular zone is going to be tougher than say a web of 'one'. It makes it really interesting.
 *	            Each entry in that list, where 'Spawned' > 0, indicates the number of web-linked spawners. 
 *	        I cleave each District into 4 rectangles on the Y axis. These are my zones.
 *	        I then process the spawners/mobiles in these zones and generate the guardian spawners. All guardians in a zone are 'web-linked'. (Meaning, they must all be deactivated to capture the area.)
 *	        So it may be easy to capture Zone1 where there is only one spawner with a crow on it, but it will be much more difficult to capture the heart of the city with many vendors etc.
 */
using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Server.Engines.PushBackSpawner;
using static Server.Utility;

namespace Server.Engines
{
    public static class PBSDistrictManager
    {
        private static Dictionary<PushBackSpawner, List<PushBackSpawner>> Web = new();      // web of push back spawners as they relate to one another
        private static Dictionary<Rectangle2D, List<Rectangle2D>> SubDistrictMap = new();   // the DistrictMap rectangles broken into smaller rects (zones)
        private static Dictionary<string, List<Rectangle2D>> DistrictMap = new();           // name of district(Key) and the rects that makup the district (I.e., Jhelom small, med, large)
        private static Dictionary<uint, CZoneInfo> ZoneInfo = new();                        // each zone has a zoneID, this is the uint key
        private static List<StackedSpawnerConsole> WebConsoles = new();                     // we keep this list so that during cleanup we can delete the SSCs
        private static Dictionary<string, List<PushBackSpawner>> DistrictWebs = new();      // keyed on Region Name, list of all PBSs for this District

        public class CZoneInfo                                  // info about the zone in which a spawned mobile was killed
        {
            private Map m_Map;                                  // not serialized
            private int m_Serial;
            private Mobile m_Mobile;
            private List<AggressorInfo> m_Aggressors = new();   // not serialized
            public Mobile Mobile { get { return m_Mobile; } set { m_Mobile = value; } }
            public List<AggressorInfo> Aggressors { get { return m_Aggressors; } set { m_Aggressors = value; } }
            public int Serial { get { return m_Serial; } }
            public Map Map { get { return m_Map; } set { m_Map = value; } }
            public bool Valid { get { return m_Map != null; } }

            public CZoneInfo(Mobile mobile, List<AggressorInfo> aggressors)
            {
                m_Mobile = mobile;
                m_Aggressors = aggressors;
            }
            public CZoneInfo(int serial)
            {
                m_Serial = serial;
            }

            public void Serialize(BinaryFileWriter writer)
            {
                int version = 1;
                writer.Write(version);

                switch (version)
                {
                    case 1:
                        {
                            writer.Write(m_Serial);
                            writer.Write(m_Mobile);
                            break;
                        }
                }
            }
            public CZoneInfo(BinaryFileReader reader)
            {
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            m_Serial = reader.ReadInt();
                            m_Mobile = reader.ReadMobile();
                            break;
                        }
                }
            }
        }

        public static void InitializeZoneInfo()
        {
            Console.WriteLine("Creating ZoneInfo...");
            int serial = 0;
            foreach (var kvp in SubDistrictMap)
                foreach (Rectangle2D small in kvp.Value)
                {
                    uint zid = CreateZoneID(small);
                    if (ZoneInfo.ContainsKey(zid))
                    {
                        Utility.ConsoleWriteLine("{0} already added this rectangle: {1}", ConsoleColor.Red, SubDistrictToDistrictName(kvp.Value), small);
                        continue;
                    }
                    ZoneInfo.Add(zid, new CZoneInfo(++serial));
                }
        }
        public static void DeinitializeZoneInfo()
        {
            Console.WriteLine("Deleting ZoneInfo...");
            ZoneInfo.Clear();
        }

        public static void GenerateDistricts()
        {
            Console.WriteLine("Push Back Spawner Districts loading...");
            List<uint> seenZone = new();
            foreach (Region rx in Map.Felucca.Regions.Values.ToList())
            {
                if (rx is not HouseRegion)
                {
                    if (rx.Name == "Minoc" || rx.Name == "Arena")
                        ;

                    if (true /*rx.Registered*/)
                    {
                        if (rx == Map.Felucca.DefaultRegion)
                            continue;

                        foreach (Rectangle3D rect3D in rx.Coords)
                        {
                            // we'll use this rect
                            Rectangle2D rect = new Rectangle2D(rect3D.Start, rect3D.End);

                            // filter out
                            if (ConfigureDistricts.Exclude.Contains(rect))
                                continue;

                            uint zid = CreateZoneID(rect);
                            if (seenZone.Contains(zid))
                                continue;
                            else
                                seenZone.Add(zid);

                            // normalize the name - make it unique
                            string name;
                            if (string.IsNullOrEmpty(rx.Name))
                                name = string.Format("(null):{0}", rx.UId);
                            else
                                name = string.Format("{0}:{1}", rx.Name, rx.UId);

                            // first, add/update the District
                            if (DistrictMap.ContainsKey(name))
                            {   // add this rect to the Districts
                                DistrictMap[name].Add(rect);
                            }
                            else
                            {   // create a new District
                                DistrictMap.Add(name, new List<Rectangle2D>() { rect });
                            }
                            // ===================================================================
                            // now for the SubDistricts
                            if (SubDistrictMap.ContainsKey(rect))
                            {
                                Utility.ConsoleWriteLine("Logic error: {0}", ConsoleColor.Red, Utility.FileInfo());
                            }
                            else
                            {
                                if (ConfigureDistricts.Include.Contains(rect))
                                    // handled by 'zonal' spawner. See: CreateSpawners
                                    SubDistrictMap.Add(rect, new List<Rectangle2D> { rect });
                                else
                                    // handled by spawner-based spawner. See: CreateSpawners
                                    SubDistrictMap.Add(rect, SplitRect(rect).ToList());
                            }
                        }
                    }
                }
            }
            Console.WriteLine("{0} Districts loaded with a total of {1} Subdistricts.", DistrictMap.Count, SubDistrictMap.Count);
        }
        public static void DegenerateDistricts()
        {
            Console.WriteLine("Push Back Spawner Districts degenerating...");
            int DistrictMapCount = DistrictMap.Count;
            int SubDistrictMapCount = SubDistrictMap.Count;
            if (DistrictMapCount + SubDistrictMapCount > 0)
            {
                DistrictMap.Clear();
                SubDistrictMap.Clear();
                Console.WriteLine("{0} Districts deleted with a total of {1} Subdistricts.", DistrictMapCount, SubDistrictMapCount);
            }
            else
            {
                Console.WriteLine("There are no Districts to delete.");
            }
        }

        private static Rectangle2D[] SplitRect(Rectangle2D rect)
        {   // 4 sections for now, we may want to tweak this
            int sections = 4;
            List<Rectangle2D> list = new List<Rectangle2D>();

            for (int i = 0; i < sections; ++i)
                list.Add(new Rectangle2D(rect.X + rect.Width / sections * i, rect.Y, rect.Width / sections, rect.Height));

            return list.ToArray();
        }
        #region Commands
        public static void Initialize()
        {
            CommandSystem.Register("ListActiveDistricts", AccessLevel.Administrator, new CommandEventHandler(ListActiveDistricts_OnCommand));
            CommandSystem.Register("SpawnDistricts", AccessLevel.Administrator, new CommandEventHandler(SpawnDistricts_OnCommand));
            CommandSystem.Register("DespawnDistricts", AccessLevel.Administrator, new CommandEventHandler(DespawnDistricts_OnCommand));
            CommandSystem.Register("LoadDistricts", AccessLevel.Administrator, new CommandEventHandler(LoadDistricts_OnCommand));
            CommandSystem.Register("UnloadDistricts", AccessLevel.Administrator, new CommandEventHandler(UnloadDistricts_OnCommand));
            CommandSystem.Register("ZoneBounds", AccessLevel.GameMaster, new CommandEventHandler(ZoneBounds_OnCommand));
        }

        [Usage("UnloadDistricts")]
        [Description("Deletes PBS districts.")]
        public static void UnloadDistricts_OnCommand(CommandEventArgs e)
        {
            if (PBSDistrictManager.Web.Count > 0)
            {
                e.Mobile.SendMessage("Please Despawn Districts first.");
                return;
            }

            e.Mobile.SendMessage("Unloading {0} Districts.", DistrictMap.Count);
            DeinitializeZoneInfo();
            DegenerateDistricts();
            e.Mobile.SendMessage("Done.");
        }
        [Usage("LoadDistricts")]
        [Description("Setups PBS districts.")]
        public static void LoadDistricts_OnCommand(CommandEventArgs e)
        {
            if (Core.RuleSets.SiegeRules())
                if (DistrictMap.Count == 0)
                {
                    e.Mobile.SendMessage("Creating Districts....");
                    GenerateDistricts();
                    e.Mobile.SendMessage("done.");
                    if (ZoneInfo.Count == 0)
                    {
                        e.Mobile.SendMessage("Setting up default zones.");
                        InitializeZoneInfo();
                        e.Mobile.SendMessage("done.");
                    }
                    else
                        e.Mobile.SendMessage("No default zones needed.");
                }
                else
                    e.Mobile.SendMessage("Districts already exist!");
            else
                e.Mobile.SendMessage("This command can only be run on Island Siege or Mortalis.");
        }
        [Usage("DespawnDistricts")]
        [Description("Despawns all PBS districts.")]
        private static void DespawnDistricts_OnCommand(CommandEventArgs e)
        {
            int count = 0;
            List<PushBackSpawner> toDelete = new();
            foreach (PushBackSpawner pbs in PBSDistrictManager.Web.Keys)
                toDelete.Add(pbs);

            foreach (PushBackSpawner pbs in toDelete)
            {
                count++;
                PBSDistrictManager.Web.Remove(pbs);
                pbs.Delete();
            }

            int wcc = WebConsoles.Count;
            foreach (StackedSpawnerConsole console in WebConsoles)
                console.Delete();
            WebConsoles.Clear();

            DistrictWebs.Clear();

            e.Mobile.SendMessage("{0} PushBackSpawners deleted, {1} StackedSpawnerConsoles deleted.", count, wcc);
        }
        [Usage("SpawnDistricts")]
        [Description("Spawns all PBS districts.")]
        public static void SpawnDistricts_OnCommand(CommandEventArgs e)
        {
            if (SubDistrictMap.Count > 0)
                if (Web.Count == 0)
                    foreach (KeyValuePair<Rectangle2D, List<Rectangle2D>> kvp in SubDistrictMap)
                    {
                        int total_count = 0; int total_webs = 0; int webs = 0;
                        foreach (Rectangle2D zone in kvp.Value)
                        {
                            //Rectangle2D zone = new Rectangle2D(rect.Start.X, rect.Start.Y, rect.Width, rect.Height);
                            List<Spawner> spawners = GetSpawnersInZone(zone, Map.Felucca /*district.Map*/);
                            List<Mobile> mobiles = GetMobilesInZone(spawners);
                            List<Mobile> vendors = GetVendorsInZone(mobiles);
                            List<Mobile> animals = GetAnimalsInZone(mobiles);
                            List<Mobile> others = GetOthersInZone(mobiles, vendors, animals);
                            int vcount, acount, ocount, zcount;
                            total_count += (vcount = DoVendorGuardians(zone, vendors, out webs, Map.Felucca /*district.Map*/));
                            total_webs += webs;
                            Utility.ConsoleWriteLine("Spawned {0} vendor guardians in {2} webs for zone {1}", ConsoleColor.Green, vcount, zone, webs);
                            total_count += (acount = DoAnimalGuardians(zone, animals, out webs, Map.Felucca /*district.Map*/));
                            total_webs += webs;
                            Utility.ConsoleWriteLine("Spawned {0} animal guardians in {2} webs for zone {1}", ConsoleColor.Green, acount, zone, webs);
                            total_count += (ocount = DoOtherGuardians(zone, others, out webs, Map.Felucca /*district.Map*/));
                            total_webs += webs;
                            Utility.ConsoleWriteLine("Spawned {0} other guardians in {2} webs for zone {1}", ConsoleColor.Green, ocount, zone, webs);

                            total_count += (zcount = DoZonalGuardians(zone, out webs, Map.Felucca /*district.Map*/));
                            total_webs += webs;
                            Utility.ConsoleWriteLine("Spawned {0} zonal guardians in {2} webs for zone {1}", ConsoleColor.Green, zcount, zone, webs);
                        }
                        var tmp = FindDistrict(kvp.Value);
                        var name = string.Empty;
                        var location = string.Empty;
                        if (tmp != null)
                        {
                            name = tmp.GetValueOrDefault().Key.ToString();
                            location = FormatList(tmp.GetValueOrDefault().Value);
                        }
                        Utility.ConsoleWriteLine("{0} total guardians, total webs {1}, in {2} zones, in district(s): {3} {4}", ConsoleColor.Blue,
                            total_count,
                            total_webs,
                            kvp.Value.Count,
                            name,
                            location
                            );
                    }
                else
                    e.Mobile.SendMessage("Districts have already been spawned. Please run DespawnDistricts first.");
            else
                e.Mobile.SendMessage("There are no districts. Please run LoadDistricts first.");
        }
        [Usage("ZoneBounds")]
        [Description("Displays the bounding area of the current district.")]
        public static void ZoneBounds_OnCommand(CommandEventArgs e)
        {
            var tmp = FindZone(e.Mobile);
            if (tmp == null)
            {
                e.Mobile.SendMessage("unable to locate a zone at this location.");
                return;
            }
            Server.Gumps.EditAreaGump.FlashArea(e.Mobile, tmp.GetValueOrDefault(), e.Mobile.Map);
            e.Mobile.SendMessage("{0}", tmp.GetValueOrDefault());
        }
        [Usage("ListActiveDistricts")]
        [Description("Lists all active PBS districts.")]
        private static void ListActiveDistricts_OnCommand(CommandEventArgs e)
        {
            List<PushBackSpawner> PbsList = GetAllPushBackSpawners(e.Mobile.Map);
            if (PbsList.Count > 0)
            {
                Defrag();
                List<Rectangle2D> disList = BreakIntoDistricts(PbsList);
                if (disList.Count > 0)
                {
                    foreach (Rectangle2D rect in disList)
                        ShowDistrict(e.Mobile, rect);
                }
                else
                    e.Mobile.SendMessage("Logic Error: {0}", Utility.FileInfo());
            }
            else
                e.Mobile.SendMessage("There are no active districts.");
        }
        #endregion Commands
        private static Rectangle2D? FindZone(Mobile m)
        {
            foreach (var zl in SubDistrictMap.Values)
                foreach (Rectangle2D zone in zl)
                    if (zone.Contains(m.Location))
                        return zone;

            return null;
        }
        private static string FormatList(List<Rectangle2D> list)
        {
            string output = string.Empty;
            foreach (Rectangle2D rect in list)
                output += rect.ToString() + "/";
            return output.Trim().Replace(" ", "").TrimEnd('/');
        }
        private static List<Rectangle2D> SubDistrictToDistrict(List<Rectangle2D> listToFind)
        {
            KeyValuePair<string, List<Rectangle2D>>? tmp = FindDistrict(listToFind);

            if (tmp == null)
                return new List<Rectangle2D>();

            return tmp.GetValueOrDefault().Value;
        }
        private static string SubDistrictToDistrictName(List<Rectangle2D> listToFind)
        {
            KeyValuePair<string, List<Rectangle2D>>? tmp = FindDistrict(listToFind);

            if (tmp == null)
                return string.Empty;

            return tmp.GetValueOrDefault().Key;

        }
        private static KeyValuePair<string, List<Rectangle2D>>? FindDistrict(List<Rectangle2D> listToFind)
        {
            foreach (var kvp in DistrictMap)
                foreach (var rect in kvp.Value)
                {
                    if (IsZonal(rect))
                    {
                        if (1 != listToFind.Count)
                            continue;
                        if (MatchList(new List<Rectangle2D> { rect }, listToFind))
                            return kvp;
                    }
                    else
                    {
                        var tmp = SplitRect(rect).ToList();
                        if (tmp.Count != listToFind.Count)
                            continue;
                        if (MatchList(tmp, listToFind))
                            return kvp;
                    }
                }

            Utility.ConsoleWriteLine("Logic error: {0}", ConsoleColor.Red, Utility.FileInfo());
            return null;
        }
        private static string FindDistrict(Point3D pointoFind)
        {
            foreach (var kvp in DistrictMap)
                foreach (var rect in kvp.Value)
                    if (rect.Contains(new Point2D(pointoFind.X, pointoFind.Y)))
                        return kvp.Key;

            return null;
        }
        private static bool MatchList(List<Rectangle2D> core, List<Rectangle2D> match)
        {
            foreach (Rectangle2D rx in match)
                if (!core.Contains(rx))
                    return false;

            return true;
        }
        private static int SumWorth(List<Mobile> mobiles)
        {
            /*
                public override MeatType MeatType { get { return MeatType.Bird; } }
                public override int Meat { get { return 1; } }
                public override int Feathers { get { return 25; } }
                public override int Meat { get { return 10; } }
                public override int Hides { get { return 15; } }
            */

            int str = 0;
            int resources = 0;
            foreach (Mobile mobile in mobiles)
                if (mobile is BaseCreature bc)
                    if (bc.Spawner != null)
                        if (bc.Spawner.Objects != null)
                            foreach (object obj in bc.Spawner.Objects)
                            {
                                str += (obj as BaseCreature).Str;
                                resources += (obj as BaseCreature).Meat +
                                    (obj as BaseCreature).Feathers +
                                    (obj as BaseCreature).Hides;
                            }

            return str + resources;
        }
        private static bool IsZonal(Rectangle2D zone)
        {
            return ConfigureDistricts.Include.Contains(zone);
        }
        private static int DoAnimalGuardians(Rectangle2D zone, List<Mobile> animals, out int webs, Map map)
        {
            /*
                [0]	{0xA4DD "a swift"}	object {Server.Mobiles.Bird}
        +		[1]	{0xA4DE "a swallow"}	object {Server.Mobiles.Bird}
        +		[2]	{0xA4DF "a plover"}	object {Server.Mobiles.Bird}
        +		[3]	{0xA4E0 "a thrush"}	object {Server.Mobiles.Bird}
        +		[4]	{0xA4E1 "a tern"}	object {Server.Mobiles.Bird}
        +		[5]	{0xA4E2 "a cat"}	object {Server.Mobiles.Cat}
        +		[6]	{0xA4E3 "a cat"}	object {Server.Mobiles.Cat}
        +		[7]	{0xA4E4 "a cat"}	object {Server.Mobiles.Cat}
        +		[8]	{0xA4E5 "a cat"}	object {Server.Mobiles.Cat}
        +		[9]	{0xA4E6 "a cat"}	object {Server.Mobiles.Cat}
        +		[10]	{0xA4E7 "a dog"}	object {Server.Mobiles.Dog}
        +		[11]	{0xA4E8 "a dog"}	object {Server.Mobiles.Dog}
        +		[12]	{0xA4E9 "a dog"}	object {Server.Mobiles.Dog}
        +		[13]	{0xA4EA "a dog"}	object {Server.Mobiles.Dog}
        +		[14]	{0xA4EB "a dog"}	object {Server.Mobiles.Dog}
        +		[15]	{0xA4EC "a rat"}	object {Server.Mobiles.Rat}
        +		[16]	{0xA4ED "a rat"}	object {Server.Mobiles.Rat}
        +		[17]	{0xA4EE "a rat"}	object {Server.Mobiles.Rat}
        +		[18]	{0xA4EF "a rat"}	object {Server.Mobiles.Rat}
        +		[19]	{0xA4F0 "a rat"}	object {Server.Mobiles.Rat}
            */
            // The above table results in a 'worth' or 8940
            //  this is calculated by Str + Resources.
            //  We will use this as a rough gage in estimating guardian strength

            int worth = SumWorth(animals);
            int guardian_count = worth / 4000;
            if (guardian_count == 0) guardian_count = 1;

            webs = 0;                                                   // how many webs we created
            if (!IsZonal(zone))
                return DoGuardians(zone, animals, guardian_count, out webs, map);
            return 0;
        }
        private static int DoVendorGuardians(Rectangle2D zone, List<Mobile> vendors, out int webs, Map map)
        {
            webs = 0;                                                   // how many webs we created
            if (!IsZonal(zone))
                return DoGuardians(zone, vendors, vendors.Count, out webs, map);
            return 0;
        }
        private static int DoOtherGuardians(Rectangle2D zone, List<Mobile> others, out int webs, Map map)
        {
            webs = 0;                                                   // how many webs we created
            if (!IsZonal(zone))
                return DoGuardians(zone, others, others.Count, out webs, map);
            return 0;
        }
        private static int DoZonalGuardians(Rectangle2D zone, out int webs, Map map)
        {
            webs = 0;                                                   // how many webs we created
            if (IsZonal(zone))
            {
                Dictionary<Point2D, int> spawnerData = new() { { zone.Center, 10 } };
                if (ConfigureDistricts.GetSpawnerData != null)
                    ConfigureDistricts.GetSpawnerData(spawnerData, zone);
                if (spawnerData.Count > 0)
                {
                    List<Spawner> spawners = new();
                    return CreateSpawners(zone, spawnerData, spawners, out webs, map);
                }
                else
                    ; // debug break
            }
            return 0;
        }
        private static int DoGuardians(Rectangle2D zone, List<Mobile> mobiles, int guardian_count, out int webs, Map map)
        {
            webs = 0;                                                   // how many webs we created
            #region location and count
            List<Spawner> spawners = GetUniqueSpawners(mobiles);        // how many spawners there are
            if (spawners.Count == 0 || mobiles.Count == 0 || guardian_count == 0)
                return 0;                                               // nothing to do
            Dictionary<Point2D, int> SpawnerData = new();               // location and count or mobiles for the next spawner
            int pairs_per_spawner = 0;
            int extra_mobile = 0;

            // determine how many pairs of guardians per spawner.
            //  also see if we need one extra
            pairs_per_spawner = Math.DivRem(guardian_count, 2, out extra_mobile);

            // initialize our location generator
            List<Point3D> seeds = new();
            foreach (Spawner spawner in spawners)
                if (!seeds.Contains(spawner.Location))
                    seeds.Add(spawner.Location);

            ZoneSpawnerLocations zpl = new ZoneSpawnerLocations(map, zone, seeds);

            if (zpl.AnyValid() == false)
            {
                foreach (Spawner spawner in spawners)
                    Utility.ConsoleWriteLine("No spawnable locations at {0}", ConsoleColor.Red, spawner.Location);
                return 0;
            }

            // allocate spawner locations, and the number of mobiles per spawner
            //  the number of records in SpawnerData will be the number of web-connected spawners
            for (int ix = 0; ix < pairs_per_spawner; ix++)
            {   // two guardians per spawner
                Point2D nextSpawner = zpl.GetNext();
                if (SpawnerData.ContainsKey(nextSpawner))
                {
                    if (SpawnerData[nextSpawner] >= 4)                  // this spawner is maxed out
                    {
                        Point2D px = zpl.AllocateNext();
                        if (px == Point2D.Zero)
                            continue;                                   // no room left at the inn (sorry, no space to spawn)
                        SpawnerData[px] = 2;                            // create a new spawner location and use it
                    }
                    else
                        SpawnerData[nextSpawner] += 2;                  // add two more guardians to this spawner
                }
                else                                                    // create a new spawner location with two guardians
                    SpawnerData.Add(nextSpawner, 2);                    // two guardians go at this new spawner
            }
            if (extra_mobile > 0)
            {   // the odd guardian
                Point2D nextSpawner = zpl.GetNext();
                if (SpawnerData.ContainsKey(nextSpawner))
                    SpawnerData[nextSpawner] += 1;                      // add two more guardians to this spawner
                else                                                    // create a new spawner location with two guardians
                    SpawnerData.Add(nextSpawner, 1);
            }
            #endregion location and count
            return CreateSpawners(zone, SpawnerData, spawners, out webs, map);
        }
        // Configure Districts 
        public static class ConfigureDistricts
        {
            // rectangles to include/exclude on top of the ones gleaned from the map regions
            public static List<Rectangle2D> Include = new();    // special zone list?
            public static List<Rectangle2D> Exclude = new();    // everything is included unless explicitly excluded
            public static Action<object> PointSelected = null;  // remote control: A spawn point has been selected
            public static Action<object, object> GetSpawnerData = null; // remote control: Get the spawner location and creature count
            public static Action<object> SpawnerCreated = null; // remote control: A spawner has been created
            // spawner setup
            public static ArrayList ObjectNamesRaw = new() { "CouncilMember" };                 // what we will spawn here
            public static bool Running = true;                                                  // are we running?
            public static bool DynamicCount = true;                                             // let the engine figure out the count?
            public static int Count = 0;                                                        // initial spawn levels - the most the spawner will produce at a time
            public static TimeSpan MinDelay = TimeSpan.FromSeconds(1);                          // don't make players wait. Remember, they are trying to complete the level in the time alloted
            public static TimeSpan MaxDelay = TimeSpan.FromSeconds(2);                          // same
            public static int SpawnerKillsNeeded = 10;                                          // local to the spawner
            public static int WebKillsNeeded = 30;                                              // global to the web of spawners
            public static bool DynamicHomeRange = true;                                         // let the engine figure it out
            public static int HomeRange = 0;                                                    // pick the farthest homeRange without exiting our zone
            public static int WalkRange = -1;                                                   // will calculate below
            public static bool WalkRangeCalc = true;                                            // automatically calculate the maximum walk range for this guardian
            public static TimeSpan SpawnerLevelRestartDelay = TimeSpan.FromMinutes(5);          // how long you have to beat this level (spawner)
            public static bool GuardIgnore = true;                                              // guards ignore these creatures                                                         
            public static bool IsConcentric = false;                                            // use the Concentric algo to distribute mobs evenly over spawn range
            public static SpawnFlags SpawnerFlags = SpawnFlags.None;                            // special flags to use when spawning creatures

            public static bool Respawn = true;                                                  // do we start spawning now? (no for events)       

            public static void ResetSpawnerParms()
            {   // reset to all default parms
                ObjectNamesRaw = new() { "CouncilMember" };
                Running = true;
                DynamicCount = true;
                Count = 0;
                MinDelay = TimeSpan.FromSeconds(1);
                MaxDelay = TimeSpan.FromSeconds(2);
                SpawnerKillsNeeded = 10;
                WebKillsNeeded = 30;
                DynamicHomeRange = true;
                HomeRange = 0;
                WalkRange = -1;
                WalkRangeCalc = true;
                SpawnerLevelRestartDelay = TimeSpan.FromMinutes(5);
                GuardIgnore = true;
                IsConcentric = false;
                SpawnerFlags = SpawnFlags.None;
                Respawn = true;
            }
        }
        private static int CreateSpawners(Rectangle2D zone, Dictionary<Point2D, int> spawnerData, List<Spawner> spawners, out int webs, Map map)
        {
            // create one spawner per SpawnerData record
            List<PushBackSpawner> list = new();                                                     // the list of siblings (web network)
            for (int ix = 0; ix < spawnerData.Count; ix++)
                list.Add(new PushBackSpawner());

            for (int ix = 0; ix < list.Count; ix++)                                                 // cycle through the chosen locations
            {
                Point3D px = MakePt3D(map, spawnerData.ElementAt(ix).Key);
                if (px == Point3D.Zero)
                {                                                                                   // no spawnable point
                    list[ix].Delete();
                    continue;
                }
                if (ConfigureDistricts.PointSelected != null)                                       // tell the caller about this spawn point
                    ConfigureDistricts.PointSelected(px);                                           // they can at this time, mod the config params
                list[ix].MoveToWorld(px, map);                                                      // move to this location
                list[ix].ObjectNamesRaw.AddRange(ConfigureDistricts.ObjectNamesRaw);                // what we will spawn
                list[ix].Running = ConfigureDistricts.Running;                                      // are we running?
                if (ConfigureDistricts.DynamicCount)
                    list[ix].Count = spawnerData.ElementAt(ix).Value;                               // initial spawn levels - the most the spawner will produce at a time
                else
                    list[ix].Count = ConfigureDistricts.Count;
                list[ix].MinDelay = ConfigureDistricts.MinDelay;                                    // don't make players wait. Remember, they are trying to complete the level in the time alloted
                list[ix].MaxDelay = ConfigureDistricts.MaxDelay;                                    // same
                list[ix].SpawnerKillsNeeded = ConfigureDistricts.SpawnerKillsNeeded;                                                   // local to the spawner
                list[ix].WebKillsNeeded = ConfigureDistricts.WebKillsNeeded;                        // global to the web of spawners
                if (ConfigureDistricts.DynamicHomeRange)
                {
                    list[ix].HomeRange = CheckRange(zone, minimum: 10, spawners);                   // pick the farthest homeRange without exiting our zone
                    list[ix].WalkRange = -1;                                                        // will calculate below
                    list[ix].WalkRangeCalc = true;                                                  // automatically calculate the maximum walk range for this guardian
                }
                else
                {
                    list[ix].HomeRange = ConfigureDistricts.HomeRange;                              // pick the farthest homeRange without exiting our zone
                    list[ix].WalkRange = ConfigureDistricts.WalkRange;                              // will calculate below
                    list[ix].WalkRangeCalc = ConfigureDistricts.WalkRangeCalc;                      // automatically calculate the maximum walk range for this guardian
                }
                list[ix].SpawnerLevelRestartDelay = ConfigureDistricts.SpawnerLevelRestartDelay;    // how long you have to beat this level (spawner)
                list[ix].GuardIgnore = ConfigureDistricts.GuardIgnore;                              // guards ignore these creatures                                                         
                list[ix].Concentric = ConfigureDistricts.IsConcentric;                            // use the Concentric algo to distribute mobs evenly over spawn range
                list[ix].SpawnerFlags = ConfigureDistricts.SpawnerFlags;                            // special flags to use when spawning creatures
                // registration
                PushBackSpawner.Web[list[ix]] = new List<PushBackSpawner>(list);                    // PBS registration with the PushBackSpawner.Web
                PBSDistrictManager.Web[list[ix]] = new List<PushBackSpawner>(list);                 // PBS registration with the PBSDistrictManager.Web
                if (ConfigureDistricts.Respawn) list[ix].Respawn();                                 // spawn it baby!
                list[ix].ReceiveMessage(list[ix], WebMessage.Registered);                           // tell our siblings we have been created!
                if (ConfigureDistricts.SpawnerCreated != null)
                    ConfigureDistricts.SpawnerCreated(list[ix]);                                    // tell the caller about this new spawner
            }

            // remove thyself - we are not our sibling! (PushBackSpawner)
            foreach (KeyValuePair<PushBackSpawner, List<PushBackSpawner>> kvp in PushBackSpawner.Web)
                if (kvp.Value.Contains(kvp.Key))
                    kvp.Value.Remove(kvp.Key);

            // remove thyself - we are not our sibling! (PBSDistrictManager)
            foreach (KeyValuePair<PushBackSpawner, List<PushBackSpawner>> kvp in PBSDistrictManager.Web)
                if (kvp.Value.Contains(kvp.Key))
                    kvp.Value.Remove(kvp.Key);

            // put a StackedSpawnerConsole on top of the stack of spawners
            foreach (Point2D px in spawnerData.Keys)
                if (Commands.OwnerTools.StackDepth(px.X, px.Y, map) > 1 && !AlreadySpawned(px.X, px.Y, typeof(StackedSpawnerConsole), map))
                {
                    int Z = 0;  // get a reasonable Z
                    Utility.CanSpawnMobile(map, px.X, px.Y, ref Z);
                    Items.StackedSpawnerConsole stackedSpawnerConsole = new();
                    stackedSpawnerConsole.Movable = false;
                    stackedSpawnerConsole.Visible = false;
                    stackedSpawnerConsole.MoveToWorld(new Point3D(px.X, px.Y, Z + 1), map);
                    WebConsoles.Add(stackedSpawnerConsole);
                }

            // calc total guardians spawned
            int total_guardians = 0;
            foreach (KeyValuePair<Point2D, int> sd in spawnerData)
                total_guardians += sd.Value;

            // tell the caller (for display purposes) how many webs were created.
            webs = spawnerData.Count;

            // finally, update the districts table of PBSs
            foreach (PushBackSpawner pdb in list)
                if (pdb.Deleted == false)
                {
                    if (!PushBackSpawner.Web.ContainsKey(pdb))
                        ;
                    // only need the one of the siblings in the web since the sibling knows the status of the whole web
                    UpdateDistrictWebsTable(zone, pdb);
                    break;
                }
                else
                    ;

            return total_guardians;
        }
        private static void UpdateDistrictWebsTable(Rectangle2D zone, PushBackSpawner pbs)
        {
            // all spawners passed in will belong to the same zone, so we will cache the district so we don't need to look it up for each spawner.
            string districtName = string.Empty;
            foreach (var kvp in DistrictMap)
                foreach (var rect in kvp.Value)
                    if (rect.Contains(new Point2D(zone.X, zone.Y)))
                    {
                        districtName = kvp.Key;
                        goto GOT_IT;
                    }
                GOT_IT:
            if (string.IsNullOrEmpty(districtName))
            {
                Utility.ConsoleWriteLine("Logic error: {0}", ConsoleColor.Red, Utility.FileInfo());
                return;
            }
            else
            {   // add this spawner to our table of district webs
                if (DistrictWebs.ContainsKey(districtName))
                    DistrictWebs[districtName].Add(pbs);
                else
                    DistrictWebs.Add(districtName, new List<PushBackSpawner>() { pbs });
            }
        }
        private static Point3D MakePt3D(Map map, Point2D px)
        {
            int z = 0;
            if (Utility.CanSpawnMobile(map, px.X, px.Y, ref z, allowExotics: true))
                return new Point3D(px.X, px.Y, z);

            // shit happens .. can't spawn here
            //Utility.ConsoleOut("Logic error: {0}", ConsoleColor.Red, Utility.FileInfo());
            return Point3D.Zero;
        }
        private static int CheckRange(Rectangle2D rect, int minimum, List<Spawner> spawners)
        {
            List<Spawner> homeRanges = new();
            foreach (Spawner spawner in spawners)
                homeRanges.Add(spawner);

            // farthest on top
            homeRanges.Sort((e1, e2) =>
            {
                return e2.HomeRange.CompareTo(e1.HomeRange);
            });
            int best = 0;
            int range = 0;
            foreach (Spawner spawner in homeRanges)
                if (CanMakeFit(spawner.HomeRange, rect, new Point2D(spawner.X, spawner.Y), out range))
                {
                    if (range > best)
                        best = range;
                }
                else if (spawner.HomeRange > best)
                    best = spawner.HomeRange;

            return Math.Max(minimum, best);
        }
        private static bool CanMakeFit(int range, Rectangle2D rect, Point2D px, out int newRange)
        {
            newRange = 0;
            while (range > 0 && range >= Math.Abs(GetRangeMax(rect, px)))
                range--;
            newRange = range;
            return range > 0;
        }
        private static int GetRangeMax(Rectangle2D rect, Point2D loc)
        {
            int xMin = rect.Start.X, xMax = rect.End.X - 1;
            int yMin = rect.Start.Y, yMax = rect.End.Y - 1;

            int xRangeMax = Math.Min(loc.X - xMin, xMax - loc.X);
            int yRangeMax = Math.Min(loc.Y - yMin, yMax - loc.Y);

            return Math.Min(xRangeMax, yRangeMax);
        }
        private static bool AlreadySpawned(int X, int Y, Type type, Map map)
        {
            int Z = 0;  // get a reasonable Z
            Utility.CanSpawnMobile(map, X, Y, ref Z);

            foreach (object o in map.GetItemsInBounds(new Rectangle2D(X, Y, 1, 1)))   // does 0,0 work? seems not
                if (o.GetType() == type)
                    if (o is Item item)
                    {   // stacked spawner consoles usually are spawner Z + 1. We'll afford a slop of 2
                        if (item.Location.X == X && item.Location.Y == Y && Math.Abs(item.Location.Z - Z) < 3)
                            return true;
                    }
                    else
                        Utility.ConsoleWriteLine("Unsupported error: {0}", ConsoleColor.Red, Utility.FileInfo());

            return false;
        }
        public class ZoneSpawnerLocations
        {
            public uint ZoneID = 0; // for debugging
            private int m_next = 0;
            private Rectangle2D m_rect = new Rectangle2D();
            private Dictionary<Point2D, bool> m_lookup = new();
            public ZoneSpawnerLocations(Map map, Rectangle2D rect, List<Point3D> seeds)
            {
                uint ZoneID = CreateZoneID(rect);
                m_rect = rect;
                int dummyZ = 0;

                foreach (Point3D point in seeds)                                                            // we were past 0-n spawners, use them as seeds
                {
                    dummyZ = point.Z;
                    if (m_lookup.ContainsKey(new Point2D(point.X, point.Y)))                                // already know about this point
                        continue;
                    if (Utility.CanSpawnMobile(map, point.X, point.Y, ref dummyZ))                          // spawnable point?
                        m_lookup.Add(new Point2D(point.X, point.Y), true);                                  // throw away the Z
                }

                Point2D newPoint = new Point2D(rect.X + rect.Width / 2, rect.Y);                            // starting point
                                                                                                            // how far we will march down the rect
                for (int stride = 8; newPoint.Y + stride < rect.End.Y; stride += 8)
                    // make sure we can spawn something there
                    if (Utility.CanSpawnMobile(map, rect.X + rect.Width / 2, rect.Y + stride, ref dummyZ))
                        // create a new possible point [disabled]
                        if (m_lookup.ContainsKey(new Point2D(rect.X + rect.Width / 2, rect.Y + stride)))    // already know about this point
                            continue;
                        else
                            m_lookup.Add(new Point2D(rect.X + rect.Width / 2, rect.Y + stride), false);


                foreach (KeyValuePair<Point2D, bool> pair in m_lookup)
                    if (pair.Key == Point2D.Zero)
                        Utility.ConsoleWriteLine("Logic error: {0}", ConsoleColor.Red, Utility.FileInfo());

                // if we were not past seeds, or seeds that cannot be spawned, allocate one
                if (AnyValid() == false)
                    AllocateNext();
            }
            public bool AnyValid()
            {
                foreach (KeyValuePair<Point2D, bool> pair in m_lookup)
                    if (pair.Value == true)
                        return true;
                return false;
            }
            public Point2D GetNext()
            {
                if (m_next >= m_lookup.Count) m_next = 0;
                bool found = false;
                for (int ix = m_next; ix < m_lookup.Count; ix++)
                {
                    if (m_lookup.ElementAt(ix).Value == true)                                   // is it enabled?
                    {
                        found = true;                                                           // cool. got the next valid location
                        m_next = ix;
                        break;
                    }
                }
                if (found)
                    return m_lookup.ElementAt(m_next++).Key;
                else if (m_next != 0 && m_next != m_lookup.Count)
                {   // start at the top
                    m_next = 0;
                    return GetNext();
                }
                else
                {
                    Utility.ConsoleWriteLine("Logic error: {0}", ConsoleColor.Red, Utility.FileInfo());
                    return Point2D.Zero;
                }
            }
            public Point2D AllocateNext()
            {
                // we want to shuffle the dictionary, but we can do it like this
                List<KeyValuePair<Point2D, bool>> keyValuePairs = new();
                foreach (KeyValuePair<Point2D, bool> kvp in m_lookup)
                    keyValuePairs.Add(kvp);
                Utility.Shuffle(keyValuePairs);

                // now look for the first disabled dictionary entry and enable it, and use it
                foreach (KeyValuePair<Point2D, bool> kvp in keyValuePairs)
                    if (kvp.Value == false)
                    {   // cool, we'll use this one
                        // first, update the actual dictionary
                        m_lookup[kvp.Key] = true;
                        return kvp.Key;
                    }

                // fail sauce
                //  I think a failure here is ok.. we simply cannot spawn
                //Utility.ConsoleOut("Logic error: {0}", ConsoleColor.Red, Utility.FileInfo());
                return Point2D.Zero;
            }
        }
        private static List<Spawner> GetUniqueSpawners(List<Mobile> mobiles)
        {
            List<Spawner> spawners = new();
            foreach (Mobile mobile in mobiles)
                if (mobile is BaseCreature bc && bc.Spawner != null)
                    if (!spawners.Contains(bc.Spawner))
                        spawners.Add(bc.Spawner);
            return spawners;
        }
        private static List<Mobile> GetOthersInZone(List<Mobile> mobiles, List<Mobile> vendors, List<Mobile> animals)
        {
            List<Mobile> others = new();
            foreach (Mobile mobile in mobiles)
                if (!vendors.Contains(mobile))
                    if (!animals.Contains(mobile))
                        others.Add(mobile);
            return others;
        }
        private static List<Mobile> GetAnimalsInZone(List<Mobile> mobiles)
        {
            List<Mobile> animals = new();
            foreach (Mobile mobile in mobiles)
                if (mobile is BaseCreature bc && bc.AI == AIType.AI_Animal)
                    animals.Add(mobile);
            return animals;
        }
        private static List<Mobile> GetVendorsInZone(List<Mobile> mobiles)
        {
            List<Mobile> vendors = new();
            foreach (Mobile mobile in mobiles)
                if (mobile is BaseVendor)
                    vendors.Add((BaseVendor)mobile);
            return vendors;
        }
        private static List<Mobile> GetMobilesInZone(List<Spawner> spawners)
        {
            List<Mobile> mobiles = new();
            foreach (Spawner spawner in spawners)
                if (spawner.Objects != null)
                    foreach (object o in spawner.Objects)
                        if (o is Mobile)
                            mobiles.Add((Mobile)o);
            return mobiles;
        }
        private static List<Spawner> GetSpawnersInZone(Rectangle2D zone, Map map)
        {
            List<Spawner> spawners = new();
            foreach (object o in map.GetItemsInBounds(zone))
                if (o is Spawner && o is not PushBackSpawner)
                    spawners.Add((Spawner)o);
            return spawners;
        }
        private static void Defrag()
        {
            List<PushBackSpawner> toDelete = new();
            List<List<PushBackSpawner>> listToDeleteFrom = new();

            // get a list of PBSs to delete
            foreach (KeyValuePair<PushBackSpawner, List<PushBackSpawner>> kvp in PBSDistrictManager.Web)
                foreach (PushBackSpawner pbs in kvp.Value)
                    if (pbs.Deleted)
                    {
                        listToDeleteFrom.Add(kvp.Value);
                        toDelete.Add(pbs);
                    }

            // remove deleted PBS from sibling lists
            foreach (List<PushBackSpawner> list in listToDeleteFrom)
                foreach (PushBackSpawner pbs in toDelete)
                    if (list.Contains(pbs))
                    {
                        list.Remove(pbs);
                        break;
                    }

            // now for the key
            foreach (PushBackSpawner pbs in toDelete)
                if (PBSDistrictManager.Web.ContainsKey(pbs))
                    PBSDistrictManager.Web.Remove(pbs);

            return;
        }
        private static void ShowDistrict(Mobile m, Rectangle2D rect)
        {
            LogHelper logger = new LogHelper("ListActiveDistricts.log", false, true, true);
            string text = string.Format("At {0}", rect);
            m.SendMessage(text); logger.Log(LogType.Text, text);
            bool header = false;
            foreach (object o in m.Map.GetItemsInBounds(rect))
            {
                if (o is PushBackSpawner pbs)
                {
                    if (header == false)
                    {
                        header = true;
                        text = string.Format("Web is {0}", pbs.WebActive ? "Active" : "Inactive");
                        m.SendMessage(text); logger.Log(LogType.Text, text);
                        text = string.Format("{0}/{1} Web kills", pbs.WebTotalKills, pbs.WebKillsNeeded);
                        m.SendMessage(text); logger.Log(LogType.Text, text);
                    }
                    text = string.Format("{0}: Spawner is {1}: {2}/{3} spawner kills",
                        pbs.Serial,
                        pbs.SpawnerDeactivated ? "Inactive" : "Active",
                        pbs.SpawnerKills, pbs.SpawnerKillsNeeded);
                    m.SendMessage(text); logger.Log(LogType.Text, text);
                }
            }
            m.SendMessage("See also: ListActiveDistricts.log");
            logger.Finish();
        }
        private static List<PushBackSpawner> GetAllPushBackSpawners(Map map)
        {
            List<PushBackSpawner> list = new List<PushBackSpawner>();
            foreach (Item item in World.Items.Values)
                if (item is PushBackSpawner pbs && pbs.Map == map && pbs.Deleted == false)
                    list.Add(pbs);

            return list;
        }
        private static List<Rectangle2D> BreakIntoDistricts(List<PushBackSpawner> PbsList)
        {
            List<PushBackSpawner> seenIt = new();
            List<Rectangle2D> list = new();
            foreach (PushBackSpawner pbs in PbsList)
                if (seenIt.Contains(pbs))
                    continue;
                else
                {
                    // get the siblings for this pbs.
                    //  we will add this spawner and siblings to the seenIt table so we don't reprocess any of these guys again
                    seenIt.Add(pbs);
                    List<PushBackSpawner> processing = PBSDistrictManager.Web[pbs];
                    seenIt.AddRange(processing);

                    // build the rect that holds this web
                    Rectangle2D rect = new Rectangle2D(pbs.X, pbs.Y, 1, 1);
                    foreach (PushBackSpawner sibling in processing)
                        rect.MakeHold(new Point2D(sibling.X, sibling.Y));

                    // okay, now we have a rect that holds this entire web.
                    list.Add(rect);
                }

            return list;
        }
        private static CZoneInfo GetZoneInfo(Mobile m)
        {
            foreach (var tmp in SubDistrictMap)
                foreach (Rectangle2D rect in tmp.Value)
                    if (rect.Contains(m.Location))
                        foreach (var kvp in ZoneInfo)
                            if (kvp.Key == CreateZoneID(rect))
                                return kvp.Value;
            return null;
        }
        private static uint CreateZoneID(Rectangle2D zone)
        {
            return (uint)Utility.GetStableHashCode(string.Format("{0}, {1}, {2}, {3}", zone.X, zone.Y, zone.Width, zone.Height), version: 1);
        }
        #region EventSink
        public static void EventSink_SpawnedMobileCreated(SpawnedMobileCreatedEventArgs e)
        {
            if (ZoneInfo.Count != 0 && DistrictMap.Count != 0)
            {
                if (e.Mobile is BaseCreature bc && bc.Spawner is PushBackSpawner)
                {
                    e.Mobile.Name = NameList.RandomName("gargoyle");
                }
            }
        }
        public static void EventSink_SpawnedMobileKilled(SpawnedMobileKilledEventArgs e)
        {
            if (ZoneInfo.Count != 0 && DistrictMap.Count != 0)
            {
                if (e is SpawnedMobileKilledInfo info && info.Mobile is BaseCreature bc && bc.Spawner is PushBackSpawner)
                {
                    if (GetZoneInfo(info.Mobile) is CZoneInfo zinfo)
                    {
                        zinfo.Mobile = info.Mobile;
                        zinfo.Aggressors = info.Aggressors;
                        zinfo.Map = info.Map;
                    }
                }
                else if (e is SpawnedMobileKilled killinfo)
                {
                    if (killinfo.Spawner is PushBackSpawner pbs)
                    {   // Add mobiles to notify
                        List<Mobile> mobiles = new();
                        foreach (var tmp in SubDistrictMap)
                            foreach (Rectangle2D rect in tmp.Value)
                                if (ZoneInfo.ContainsKey(CreateZoneID(rect)))
                                {
                                    if (ZoneInfo[CreateZoneID(rect)].Valid)
                                        foreach (Mobile m in ZoneInfo[CreateZoneID(rect)].Map.GetMobilesInBounds(rect))
                                            if (m is PlayerMobile pm)
                                                mobiles.Add(pm);
                                }
                                else
                                    Utility.ConsoleWriteLine("Logic error: {0}", ConsoleColor.Red, Utility.FileInfo());
                        // process messages
                        foreach (var tmp in SubDistrictMap)
                            foreach (Rectangle2D rect in tmp.Value)
                                if (rect.Contains(pbs.Location))
                                    if (ZoneInfo.ContainsKey(CreateZoneID(rect)))
                                    {
                                        if (ZoneInfo[CreateZoneID(rect)].Valid)
                                            foreach (Mobile mx in mobiles)
                                                if (DistrictConquered(killinfo.Mobile.Location))
                                                {                                                                                           // killed all mobiles in the district (region)
                                                    mx.SendMessage(0x21, "{0} Captured!", DistrictFriendlyName(killinfo.Mobile.Location));
                                                    DeactivatedDistrict(killinfo.Mobile.Location);
                                                    Celebrate(killinfo.Mobile.Location);

                                                }
                                                else if (pbs.WebTotalKills == pbs.WebKillsNeeded)
                                                    mx.SendMessage(0x59, "Zone {0} Cleared!", ZoneInfo[CreateZoneID(rect)].Serial);         // killed all mobiles in the web (zone)
                                                else if (pbs.SpawnerKills == pbs.SpawnerKillsNeeded)
                                                    mx.SendMessage(0x40, "{0}/{1} Areas Cleared for zone {2}.",
                                                        (killinfo.Spawner as PushBackSpawner).DeactivatedSpawners,
                                                        (killinfo.Spawner as PushBackSpawner).ConnectedSpawners,
                                                        ZoneInfo[CreateZoneID(rect)].Serial);                                               // killed all the mobiles on this spawner (area)
                                                else
                                                {
                                                    mx.SendMessage("{0}/{1} kills needed for area",
                                                        pbs.SpawnerKills,
                                                        pbs.SpawnerKillsNeeded);
                                                    mx.SendMessage("{0}/{1} kills needed for zone {2}",
                                                        pbs.WebTotalKills,
                                                        pbs.WebKillsNeeded,
                                                        ZoneInfo[CreateZoneID(rect)].Serial);
                                                }
                                    }
                                    else
                                        Utility.ConsoleWriteLine("Logic error: {0}", ConsoleColor.Red, Utility.FileInfo());
                    }
                }
            }
        }
        private static void Celebrate(Point3D px)
        {
            Effects.PlaySound(px, Map.Felucca, 0x2F3);  // earthquake
        }
        private static bool DistrictConquered(Point3D px)
        {
            string districtName = FindDistrict(px);
            if (districtName == null) return false;
            if (DistrictWebs.ContainsKey(districtName) == false)
                Utility.ConsoleWriteLine("Logic error: Cannot find District Name '{1}' in DistrictWebs: {0}", ConsoleColor.Red, Utility.FileInfo(), districtName);
            else
                foreach (PushBackSpawner pbs in DistrictWebs[districtName])
                    if (pbs.WebActive == true)
                        return false;

            return true;
        }
        private static void DeactivatedDistrict(Point3D px)
        {
            string districtName = FindDistrict(px);
            if (districtName == null) return;
            if (DistrictWebs.ContainsKey(districtName) == false)
                Utility.ConsoleWriteLine("Logic error: Cannot find District Name '{1}' in DistrictWebs: {0}", ConsoleColor.Red, Utility.FileInfo(), districtName);
            else
                foreach (PushBackSpawner pbs in DistrictWebs[districtName])
                    if (pbs.SpawnerDeactivated == false)
                        pbs.SpawnerDeactivated = true;
        }
        private static string DistrictFriendlyName(Point3D px)
        {
            string friendlyName = FindDistrict(px);
            return friendlyName[..friendlyName.IndexOf(':')];
        }
        private static bool EventSink_OnSingleClick(OnSingleClickEventArgs e)
        {   // TO DO
            return false;
        }
        #endregion EventSink
        #region IO 
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
            EventSink.SpawnedMobileKilled += new SpawnedMobileKilledEventHandler(EventSink_SpawnedMobileKilled);
            EventSink.SpawnedMobileCreated += new SpawnedMobileCreatedEventHandler(EventSink_SpawnedMobileCreated);
            EventSink.OnSingleClick += new OnSingleClickEventHandler(EventSink_OnSingleClick);
        }
        public static void Load()
        {
            if (!File.Exists("Saves/DistrictManager.bin"))
                return;

            Console.WriteLine("DistrictManager Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/DistrictManager.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            // DistrictWebs
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                                DistrictWebs.Add(reader.ReadString(), reader.ReadItemList<PushBackSpawner>());

                            // DistrictMap
                            count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                                DistrictMap.Add(reader.ReadString(), reader.ReadRectangle2DList());

                            // SubDistrictMap
                            count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                                SubDistrictMap.Add(reader.ReadRect2D(), reader.ReadRectangle2DList());

                            // ZoneInfo
                            int ZoneInfoTableSize = reader.ReadInt();
                            for (int ix = 0; ix < ZoneInfoTableSize; ix++)
                                ZoneInfo.Add(reader.ReadUInt(), new CZoneInfo(reader));

                            // WebConsoles
                            WebConsoles = reader.ReadItemList<StackedSpawnerConsole>();

                            // PBSDistrictManager.Web
                            count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Item item = reader.ReadItem();
                                if (item is not null && item.Deleted == false)
                                    PBSDistrictManager.Web.Add(item as PushBackSpawner, reader.ReadItemList<PushBackSpawner>());
                                else
                                    reader.ReadItemList(); // discard list
                            }
                            break;
                        }

                    default:
                        {
                            reader.Close();
                            throw new Exception("Invalid version in DistrictManager.bin.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error reading DistrictManager.bin, using default values:");
                Utility.PopColor();
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("DistrictManager Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/DistrictManager.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            // DistrictWebs
                            writer.Write(DistrictWebs.Count);
                            foreach (var kvp in DistrictWebs)
                            {
                                writer.Write(kvp.Key);
                                writer.WriteItemList(kvp.Value);
                            }

                            // DistrictMap
                            writer.Write(DistrictMap.Count);
                            foreach (var kvp in DistrictMap)
                            {
                                writer.Write(kvp.Key);
                                writer.WriteRectangle2DList(kvp.Value);
                            }

                            // SubDistrictMap
                            writer.Write(SubDistrictMap.Count);
                            foreach (var kvp in SubDistrictMap)
                            {
                                writer.Write(kvp.Key);
                                writer.WriteRectangle2DList(kvp.Value);
                            }

                            // ZoneInfo
                            writer.Write(ZoneInfo.Count);
                            foreach (KeyValuePair<uint, CZoneInfo> kvp in ZoneInfo)
                            {
                                writer.Write(kvp.Key);
                                kvp.Value.Serialize(writer);
                            }

                            // WebConsoles
                            writer.WriteItemList<StackedSpawnerConsole>(WebConsoles);

                            // PBSDistrictManager.Web
                            writer.Write(PBSDistrictManager.Web.Count);
                            foreach (KeyValuePair<PushBackSpawner, List<PushBackSpawner>> kvp in PBSDistrictManager.Web)
                            {
                                writer.Write(kvp.Key);
                                writer.WriteItemList(kvp.Value);
                            }
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing DistrictManager.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion IO 
    }
}