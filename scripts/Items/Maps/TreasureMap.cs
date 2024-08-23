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

/* Scripts/Items/Maps/TreasureMap.cs
 * ChangeLog
 *  5/5/2024, Adam (TreasureMapQuest)
 *      Add TreasureMapQuest maps. These maps have two distinct differences over regular treasure maps.
 *          1. They load their locations from a special table located in \data\Binary Map Data
 *          2. They require a RaresPack. One item from this rares pack is added to the usual loot (as specified by level.)
 * 6/12/23, Yoar
 *      Treasure maps can now be dug up by those who did not decode it, providing that they have enough cartography skill.
 *      https://www.uoguide.com/Publish_13
 * 1/11/23, Adam (GetRandomLocation())
 *      We now filter our dynamic treasure map locations for BaseMulti during the GetRandomLocation() call
 * 9/6/22, Adam
 *      remove the early exit after loading the standard OSI t-map locations if (!Core.RuleSets.AngelIslandRules())
 * 5/31/22, Adam (RemoveBadRegions)
 *  This filters out all dungeons + green acres and whatnot
 *  if (location.X >= 5120) continue;
 *	2/3/11, Adam
 *		Make extended map locations based upon Core.AngelIsland
 *	9/1/10, Adam
 *		Make treasure maps ReadOnly 
 *	8/27/10, adam
 *		Use Utility.BritWrap rects from boat nav to test for valid treasure map locations.
 *		Turns out that the rects defined in Utility.BritWrap are the two halves of the world excluding T2A and the dungeons
 *		which is what we want.
 *	8/12/10, adam
 *		Add tax collector spawn
 *	1/20/06, Adam
 *		Add new OnNPCBeginDig() function so that NPC's can dig treasure.
 *	5/07/05, Adam
 *		Remove hue and replace the the text 
 *			"for somewhere in Felucca" with "for somewhere in Ocllo"
 *		if the map is in Ocllo
 *	5/07/05, Kit
 *		Hued maps to faction orange if for withen ocllo
 *	4/17/05, Kitaras	
 *		Fixed bug regarding level 1 and 2 chests being set to themed
 *	4/14/04, Adam
 *		1. Put back lost change to treasure map drop rate
 *		2. Convert LootChance to a property, and attach it to CoreAI.TreasureMapDrop
 *	4/07/05, Kitaras
 *		Fixed static access variable issue
 *	4/03/05, Kitaras	
 *		Added check to spawn only one level 5 mob on themed chests
 *	3/30/05, Kitaras
 *		Redesigned system to use new TreasureThemes.cs control system for spawn generation.
 *	3/1/05, mith
 *		OnDoubleClick(): modified difficulty check so that if base Carto skill is less than midPoint, players gets failure message.
 *  12/05/04, Jade
 *      Reverted the chance to drop t-map back to 1%
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/29/04, mith
 *		Changed percentage chance for creature to drop t-map from 1% to 3.5%
 *
 *      8/30/04, Lego Eater
 *               changed the on singleclick so tmaps displayed properly (sorry for spelling)
 *
 *
 */

using Server.Commands;
using Server.Diagnostics;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using static Server.Utility;

namespace Server.Commands
{
    #region COMMANDS and SUPPORT
    public class LoadTreasureMapLocations
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("LoadTreasureMapLocations", AccessLevel.Owner, new CommandEventHandler(LoadTreasureMapLocations_OnCommand));
            Server.CommandSystem.Register("ReportBadTile", AccessLevel.GameMaster, new CommandEventHandler(ReportBadTile_OnCommand));
        }

        [Usage("LoadTreasureMapLocations <?>")]
        [Description("Loads all OSI and custom AI treasure map locations.")]
        public static void LoadTreasureMapLocations_OnCommand(CommandEventArgs e)
        {
            Console.Write("Loading treasure map locations...");
            int lcount = Server.Items.TreasureMap.LoadLocations();
            Mobiles.PlayerMobile pm = e.Mobile as Mobiles.PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new System.Collections.ArrayList();
            foreach (Point2D px in Server.Items.TreasureMap.Locations)
            {
                pm.JumpList.Add(px);
            }
            Console.WriteLine("done ({0} locations loaded.)", lcount.ToString());
        }


        [Usage("ReportBadTile")]
        [Description("Records the tile you are standing and writes it to \"BadTiles.log\".")]
        public static void ReportBadTile_OnCommand(CommandEventArgs e)
        {
            try
            {
                int count = ReportBadTileWorker(e);
                e.Mobile.SendMessage("Reported bad tile. Please see: {0}.", "BadTiles.log");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        private static int ReportBadTileWorker(CommandEventArgs e)
        {

            // get the land tile at our location and recore it in the BadSpawnTiles.log
            LandTile landTile = Server.Items.TreasureMap.BadTile(e.Mobile.Location);
            Console.WriteLine(string.Format("0x{0:X}", landTile.ID));
            LogHelper TileLogger = new LogHelper("BadTiles.log", false);
            TileLogger.Log(LogType.Text, string.Format("0x{0:X},", landTile.ID));
            TileLogger.Finish();
            return 0;
        }
    }
    #endregion COMMANDS and SUPPORT
}
namespace Server.Items
{
    public class TreasureMap : MapItem
    {
        #region SETUP
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
        }

        public static void OnLoad()
        {
            if (SpawnerCache.Spawners.Count == 0)
            {   // we use the spawner cache to help with treasure map locations
                Console.Write("Loading spawner cache...");
                int count = SpawnerCache.LoadSpawnerCache();
                Console.WriteLine("done ({0} spawners loaded.)", count.ToString());
                // since we just loaded the spawner cache, now is  good time to look for broken template spawners
                int bcount = Commands.FindSpawner.LogBrokenTemplateSpawners();
                if (bcount > 0)
                {
                    Utility.Monitor.WriteLine("Warning: {0} Broken Template Spawners detected. Please see: {1}", ConsoleColor.Red, bcount, "FindBrokenTemplateSpawners.log");
                }
                // now see if there are any spawners that are trying to spawn something that doesn't exist
                LogHelper logger = new LogHelper("badspawn.log", false);
                int kcount = 0;
                foreach (Spawner sx in SpawnerCache.Spawners)
                {
                    if (sx == null) continue;
                    if (sx.ObjectNames == null) continue;
                    if (sx.Running == false) continue;

                    foreach (string typeName in sx.ObjectNames)
                    {
                        if (SpawnerType.GetType(typeName) == null)
                        {
                            kcount++;
                            logger.Log(LogType.Text, string.Format("Spawner at location {0}, on map {1}, was trying to spawn {2} (which is not a valid type)", sx.Location, sx.Map, typeName));
                        }
                    }
                }
                logger.Finish();

                if (kcount > 0)
                {
                    Utility.Monitor.WriteLine("Warning: {0} Bad Spawners detected. Please see: {1}", ConsoleColor.Red, kcount, "badspawn.log");
                }

            }

            // now check ChestItemSpawners
            //LogHelper logger2 = new LogHelper("badspawn.log", false);
            //int ccount = 0;
            //foreach (Item ix in World.Items.Values)
            //{
            //    if (ix == null) continue;
            //    if (!(ix is ChestItemSpawner)) continue;
            //    ChestItemSpawner cis = ix as ChestItemSpawner;
            //    if (cis.ItemsName == null) continue;
            //    foreach (string typeName in cis.ItemsName)
            //    {
            //        if (ChestItemSpawnerType.GetType(typeName) == null && ChestItemSpawnerType.GetLootPack(typeName) == null)
            //        {
            //            ccount++;
            //            logger2.Log(LogType.Text, string.Format("Chest Item Spawner at location {0}, on map {1}, was trying to spawn {2} (if it looks like a serial it's probably a loot pack)", cis.Location, cis.Map, typeName));
            //        }
            //    }
            //}
            //logger2.Finish();

            //if (ccount > 0)
            //{
            //    Utility.PushColor(ConsoleColor.Red);
            //    Console.WriteLine("Warning: {0} Bad Chest Item Spawners detected. Please see: {1}", ccount, "badspawn.log");
            //    Utility.PopColor();
            //}

            /*
             * treasure map stuffs
             */
            Console.Write("Loading treasure map locations...");
            int lcount = LoadLocations();
            Console.WriteLine("done ({0} locations loaded.)", lcount.ToString());
        }
        #endregion SETUP

        public override bool ReadOnly { get { return true; } set {; } }
        #region DATA
        private int m_Level;
        private bool m_Completed;
        private Mobile m_Decoder;
        private Map m_Map;
        private Point2D m_ChestLocation;
        private bool m_Themed;
        private ChestThemeType m_type;

        #region Command Props
        [CommandProperty(AccessLevel.GameMaster)]
        public int Level { get { return m_Level; } set { m_Level = value; InvalidateProperties(); } }

        //set theme type
        [CommandProperty(AccessLevel.GameMaster)]
        public ChestThemeType Theme { get { return m_type; } set { m_type = value; InvalidateProperties(); } }

        //set if map is themed or not
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Themed { get { return m_Themed; } set { m_Themed = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Completed { get { return m_Completed; } set { m_Completed = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Decoder { get { return m_Decoder; } set { m_Decoder = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map ChestMap { get { return m_Map; } set { m_Map = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D ChestLocation { get { return m_ChestLocation; } set { m_ChestLocation = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double LootChance { get { return CoreAI.TreasureMapDrop; } }
        #endregion Command Props
        public static Point2D[] Locations { get { return m_Locations; } }

        private static Point2D[] m_Locations;
        #endregion DATA
        public virtual Point2D GetRandomLocation(Map map)
        {
            return GetRandomTreasureMapLocation(map);
        }
        public static Point2D GetRandomTreasureMapLocation(Map map)
        {
            if (m_Locations == null)
                LoadLocations();

            if (m_Locations.Length > 0)
                for (int test = 0; test < 100; test++)
                {   // make sure a witch hasn't dropped a house on poor Dorthy
                    Point2D p2d = m_Locations[Utility.Random(m_Locations.Length)];
                    BaseMulti bm = BaseMulti.Find(new Point3D(p2d.X, p2d.Y, Utility.GetAverageZ(map, p2d.X, p2d.Y)), map);
                    if (bm == null)
                        return p2d;
                }

            Utility.Monitor.WriteLine(string.Format("Running out of valid treasure map locations: {0}. ", Utility.FileInfo()), ConsoleColor.Red);

            return Point2D.Zero;
        }
        #region TOOLS
        public static int LoadLocations()
        {
            string OSIFileName = Path.Combine(Core.DataDirectory, "treasure.cfg");
            string AIFileName = Path.Combine(Core.DataDirectory, "Angel Island Treasure.cfg");
            if (File.Exists(Path.Combine(Core.BaseDirectory, OSIFileName)) == false)
            {
                Core.LoggerShortcuts.BootError(string.Format("\nError while reading Treasure map data from \"{0}\".", OSIFileName));
                return 0;
            }
            else if (File.Exists(Path.Combine(Core.BaseDirectory, AIFileName)) == false)
            {
                Core.LoggerShortcuts.BootError(string.Format("Error while reading Treasure map data from \"{0}\".", AIFileName));
                // we'll continue and generate one (with this warning)
            }

            ArrayList OSI_Locations = new ArrayList();

            // first load up the standard OSI locations (we will still randomize around this point)
            string filePath = Path.Combine(Core.BaseDirectory, OSIFileName);
            if (File.Exists(filePath))
            {
                using (StreamReader ip = new StreamReader(filePath))
                {
                    string line;

                    while ((line = ip.ReadLine()) != null)
                    {
                        try
                        {
                            string[] split = line.Split(' ');

                            int x = Convert.ToInt32(split[0]), y = Convert.ToInt32(split[1]);

                            OSI_Locations.Add(new Point2D(x, y));
                        }
                        catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                    }
                }
            }

            string treasurePath = Path.Combine(Core.BaseDirectory, AIFileName);
            ArrayList AI_Locations = new ArrayList();
            if (File.Exists(treasurePath))
            {
                using (StreamReader ip = new StreamReader(treasurePath))
                {
                    string line;

                    while ((line = ip.ReadLine()) != null)
                    {
                        try
                        {
                            string[] split = line.Split(' ');

                            int x = Convert.ToInt32(split[0]), y = Convert.ToInt32(split[1]);

                            AI_Locations.Add(new Point2D(x, y));
                        }
                        catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                    }
                }

                m_Locations = (Point2D[])AI_Locations.ToArray(typeof(Point2D));
                return m_Locations.Length;
            }

            // No AI treasure map, generate one
            Console.WriteLine("\n{0} does not exist. Generating... This may make 10 minutes", treasurePath);
            AI_Locations = GenAITreasureMapLocations(OSI_Locations);
            m_Locations = (Point2D[])AI_Locations.ToArray(typeof(Point2D));
            LogLocations(); // Finally, we will write a log file with all treasuremap locations - for diagnostic purposes only

            using (StreamWriter ip = new StreamWriter(treasurePath))
            {
                foreach (Point2D px in AI_Locations)
                {
                    try
                    {
                        ip.WriteLine(string.Format("{0} {1}", px.X, px.Y));
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }

            return m_Locations.Length;

        }

        public static ArrayList GenAITreasureMapLocations(ArrayList OSI_Locations)
        {
            ArrayList temp_list = new ArrayList();
            /// Phase 1
            // now load up the spawner locations. We do this as an enhanced map of treasure map locations.
            // The basic idea is that if there is a spawner there, it's not in town or green acres, it's not a dungeon or T2A,
            //	then it's a legit place for a treasure chest.
            Console.Write("\n-Phase 1. Loading Felucca spawner locations...");
            temp_list = LoadSpawnerSpawnPoints();
            Console.WriteLine("({0}) points created.", temp_list.Count);

            // expand the list of spawner locations by selecting up to 10 locations around the spawner
            Console.Write("-Phase 1.a. Expand points up to 10x...");
            temp_list = ExpandList(temp_list);
            Console.WriteLine("({0}) total points thus far.", temp_list.Count);

            /// Phase 2
            // now test all to see if we have any invalid locations.
            //	if so, add them to a 'bad' list and remove them from the clean list
            // Utility.BritWrap contains T2A and dungeons and is not compatible with treasure maps
            Console.Write("-Phase 2. Remove bad regions...");
            temp_list = RemoveBadRegions(temp_list);
            Console.WriteLine("({0}) points remaining.", temp_list.Count);

            // Phase 3
            // remove duplicate locations
            Console.Write("-Phase 3. Remove duplicate points...");
            temp_list = RemoveDuplicates(temp_list);
            Console.WriteLine("({0}) points remaining.", temp_list.Count);

            // Phase 4
            // remove locations that point to bad tiles (usually floor tiles, docks, water, etc.)
            Console.Write("-Phase 4. Remove bad tiles...");
            temp_list = RemoveBadTiles(temp_list);
            Console.WriteLine("({0}) points remaining.", temp_list.Count);

            // Phase 4a
            // remove locations that point to a location that an NPC could not reasonably 'path' to
            //  If an NPC cannot 'path' there, a player is likely to have trouble as well
            Console.Write("-Phase 4a (EXPENSIVE). Remove inaccessible points...");
            temp_list = RemoveInaccessible(temp_list);
            Console.WriteLine("({0}) points remaining.", temp_list.Count);

            // Phase 5
            // combine the original OSI locations with our expanded list
            //  Note we convert from 3D to 2D here
            Console.Write("-Phase 5. Combining OSI and AI Lists...");
            ArrayList Point2D_list = new ArrayList();
            foreach (Point3D px in temp_list)
                Point2D_list.Add(new Point2D(px));
            Point2D_list.AddRange(OSI_Locations);
            //m_Locations = (Point2D[])Point2D_list.ToArray(typeof(Point2D));
            Console.WriteLine("({0}) total Angel Island treasure map locations", Point2D_list.Count);

            return Point2D_list;
        }

        // load the locations of all spawners
        public static ArrayList LoadSpawnerSpawnPoints()
        {
            ArrayList list = new ArrayList();
            foreach (Spawner sx in SpawnerCache.Spawners)
            {
                // add the point which 'fanned out' from the spawner, but still legal.
                list.Add(sx.Location);
            }

            return list;
        }
        // expand of the list of spawner locations, with up to 10 locations around the spawner.
        public static ArrayList ExpandList(ArrayList points)
        {
            ArrayList pointList = new ArrayList();
            foreach (Point3D px in points)
            {
                for (int ix = 0; ix < 10; ix++)
                {
                    Point3D px3 = FanOut(px);
                    if (pointList.Contains(px3))
                        continue;
                    else
                        pointList.Add(px3);
                }
            }

            return pointList;
        }
        public static ArrayList RemoveBadRegions(ArrayList points)
        {
            ArrayList filteredPoints = new ArrayList();
            foreach (Point3D location in points)
            {
                Region region = Server.Region.Find(location, Map.Felucca);

                if (region != null)
                {   // No Guarded Towns
                    if (SpawnerCache.IsTown(region.Name))
                        if (region.IsGuarded)
                            continue;

                    // no green acres, inside houses, etc..
                    // BTW - SpawnerCache.IsValidRegion() is too restrictive 
                    if (region.IsAngelIslandRules) continue;
                    if (region.IsGreenAcresRules) continue;
                    if (region.IsHouseRules) continue;
                    if (region.IsJailRules) continue;
                    if (region.IsDungeonRules) continue;
                    if (region.Name == "Broken ML Map") continue;
                    if (region.Name == "Britannia Royal Zoo") continue;
                    if (region.Name == "Restricted Area") continue;
                    if (region.Name == "Misc Dungeons") continue;
                    if (region.Name == "Terathan Keep") continue;
                    //if (!(Map.Felucca.Bound.Contains(new Point2D (location)))) continue;
                    //Rectangle2D rect = new Rectangle2D()

                }

                // This filters out all dungeons + green acres and whatnot
                if (location.X >= 5120)
                    continue;

                // remove other illegal areas
                Point2D px3 = new Point2D(location.X, location.Y);
                // Utility.BritWrap[1] contains T2A and is not compatible with treasure maps
                if (Utility.World.BritWrap[1].Contains(new Point2D(px3.X, px3.Y)))
                    continue;

                // The Solen Hive
                if (new Rectangle2D(new Point2D(5650, 1786), new Point2D(5925, 2030)).Contains(px3))
                    continue;

                filteredPoints.Add(location);
            }

            return filteredPoints;
        }
        public static ArrayList RemoveDuplicates(ArrayList points)
        {
            ArrayList temp_list = new ArrayList(); // remove any duplicates
            foreach (Point3D px in points)
            {
                if (temp_list.Contains(px))
                    continue;

                temp_list.Add(px);
            }

            return temp_list;
        }
        public static ArrayList RemoveBadTiles(ArrayList list)
        {
            ArrayList temp_list = new ArrayList(); // remove any duplicates
            ArrayList bad_tile_list = new ArrayList(); // exclude and bad tiles
            foreach (Point3D p in list)
            {
                // check placement rules
                if (!CheckPlacement(new Point2D(p.X, p.Y), bad_tile_list))
                    continue;

                temp_list.Add(p);
            }

            // debug
            LogHelper TileLogger = new LogHelper("BadTileSkip.log", true);
            foreach (int tile in bad_tile_list) { TileLogger.Log(LogType.Text, string.Format("0x{0:X}", tile)); }
            TileLogger.Finish();

            return temp_list;
        }
        public static ArrayList RemoveInaccessible(ArrayList points)
        {
            ArrayList AccessiblePoints = new ArrayList();
            int count = 0;
            foreach (Point3D px3 in points)
            {
                // look for spawners in the local area. If we can walk to any of them, then we are likely not in a blocked location
                //  We do this check in case the 'valid' location(road, land, grass, etc.) we have now is itself blocked, like within a wall,
                //  like at serpent's hold (hollow walls)
                ArrayList spawners = new ArrayList(Server.Commands.FindSpawner.FindLocalSpawners(new Point2D(px3.X, px3.Y), 32));
                if (spawners == null || spawners.Count == 0)
                    continue;

                // okay, now we have a list of spawners in the general vicinity of px3/px2
                //  we will get the location, validate it, and see if a human could march there.
                //  again, if an NPC could pathfind there, a player is probably not blocked from getting there in the first place.
                foreach (FindSpawner.SpawnerMatch spawner_match in spawners)
                {
                    if (count++ % 500 == 0)
                        Console.Write(".");

                    Point3D temp_location = new Point3D(Point3D.Zero);
                    if ((spawner_match.Sp is Spawner))
                        temp_location = (spawner_match.Sp as Spawner).Location;
                    else if ((spawner_match.Sp is ChestItemSpawner))
                        temp_location = (spawner_match.Sp as ChestItemSpawner).Location;
                    else if (temp_location == Point3D.Zero) // likely a different kind of spawner, maybe EventSpawner
                        continue;

                    // can we get there from here?
                    // that is, could a human walk from the proposed treasure map location to this nearby spawner.
                    Movement.MovementObject obj_start = new Movement.MovementObject(px3, temp_location, null);
                    if (!MovementPath.PathTo(obj_start))
                        continue;   // can't get there from here

                    // okay, looks like we're not blocked
                    AccessiblePoints.Add(px3);
                    break;
                }
            }

            return AccessiblePoints;
        }
        public static void LogLocations()
        {
            /* log all locations */
            // for diagnostic purposes only
            {
                LogHelper Logger = new LogHelper("TreasureMapLocations.log", true);
                ArrayList locations = new ArrayList(TreasureMap.Locations);
                foreach (Point2D loc in locations)
                {
                    Region rx = Region.Find(new Point3D(loc as IPoint2D, 0), Map.Felucca);
                    if (rx == null)
                        Logger.Log(LogType.Text, "region is null at :" + loc.ToString());
                    else
                        Logger.Log(LogType.Text, string.Format("Region: '{0}', Coords: {1}", (rx == Map.Felucca.DefaultRegion) ? "DefaultRegion" : rx.Name, loc.ToString()));
                }
                Logger.Finish();
            }
        }

        // BadTile is called by [ReportBadTile command to simply return the tile I am standing on
        //  we then record it for later processing
        public static LandTile BadTile(Point3D p)
        {
            LandTile landTile = Map.Felucca.Tiles.GetLandTile(p.X, p.Y);
            return landTile;
        }
        public static bool CheckPlacement(Point2D p, ArrayList bad_tile_list)
        {
            int[] bad_tiles = new int[] { 0x3, 0x4, 0x5, 0x6, 0x5A, 0x64, 0x78, 0x8A, 0x22C, 0x22F, 0x406, 0x407, 0x408, 0x409, 0x411, 0x40A, 0x40B, 0x40C, 0x40D, 0x40E, 0x40F, 0x410, 0x412, 0x414, 0x415, 0x41E, 0x428, 0x436, 0x437, 0x43A, 0x43B, 0x43C, 0x43D, 0x43E, 0x43F, 0x440, 0x441, 0x486, 0x487, 0x488, 0x489, 0xC5 };

            int tileX = p.X;
            int tileY = p.Y;
            // look at the land tiles (water etc.)
            LandTile landTile = Map.Felucca.Tiles.GetLandTile(tileX, tileY);
            int landID = landTile.ID & 0x3FFF;

            for (int ix = 0; ix < bad_tiles.Length; ix++)
            {
                if (bad_tiles[ix].CompareTo(landID) == 0 || bad_tiles[0].CompareTo(landTile.ID) == 0)
                {
                    bad_tile_list.Add(landTile.ID);
                    return false;
                }
            }

            return true;
        }
        // Look for locations adjacent to the spawner, a location that can be spawned on (item or mobile)
        public static Point3D FanOut(Point3D px2)
        {
            Point3D px3;
            // even though GetSpawnPosition will retry 10x to get a legal spawn position, we will 
            //  will try up to an additional 10x to get a position which isn't a bad tile for a treasure chest
            //  we will also force a retry is we can't actually put a mobile there (CanFit() basically)
            for (int ix = 0; ix < 10; ix++)
            {
                // find a location where a mobile MIGHT be spawned
                px3 = Spawner.GetSpawnPosition(Map.Felucca, px2, 45, SpawnFlags.None, null);

                // did we get good place other than the starting point?
                if (px3 == Point3D.Zero || px3 == px2)
                    continue;

                // now see of a mobile can ACTUALLY be spawned there
                if (!Map.Felucca.CanSpawnLandMobile(px3))
                    continue;

                return px3;     // new spawn point
            }

            return px2;     // we will accept this case.
        }
        #endregion TOOLS
        #region CONSTRUCRORS
        // [add treasuremap <level>
        [Constructable]
        public TreasureMap(int level)
            : this(level, Map.Felucca)
        {
        }
        // [add treasuremap <level> <map>
        [Constructable]
        public TreasureMap(int level, Map map)
            : this(level, map, false, ChestThemeType.None)
        {
        }

        //[add treasuremap <level> <map> <theme>
        [Constructable]
        public TreasureMap(int level, Map map, ChestThemeType type)
            : this(level, map, true, type)
        {
        }

        [Constructable]
        public TreasureMap(int level, Map map, bool themed, ChestThemeType type)
        {
            if (level > 5 || level < 0)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(MapLevelErrorTick), new object[] { null });
                m_Level = 0;    // Youthful treasure map ( < 100 gold)
            }
            else
                m_Level = level;
            m_Map = map;
            m_Themed = themed;
            m_type = type;

            Setup();
        }
        #region Map Level Error
        private void MapLevelErrorTick(object state)
        {
            this.SendSystemMessage("Illegal map level", AccessLevel.GameMaster, 0x33);
        }
        #endregion Map Level Error
        #endregion CONSTRUCRORS
        public virtual void Setup()
        {
            m_ChestLocation = GetRandomLocation(map: m_Map);

            Width = 300;
            Height = 300;

            int width = 600;
            int height = 600;

            int x1 = m_ChestLocation.X - Utility.RandomMinMax(width / 4, (width / 4) * 3);
            int y1 = m_ChestLocation.Y - Utility.RandomMinMax(height / 4, (height / 4) * 3);

            if (x1 < 0)
                x1 = 0;

            if (y1 < 0)
                y1 = 0;

            int x2 = x1 + width;
            int y2 = y1 + height;

            if (x2 >= 5120)
                x2 = 5119;

            if (y2 >= 4096)
                y2 = 4095;

            x1 = x2 - width;
            y1 = y2 - height;

            Bounds = new Rectangle2D(x1, y1, width, height);

            Protected = true;

            AddWorldPin(m_ChestLocation.X, m_ChestLocation.Y);

            return;
        }
        public virtual void ChestCreated(TreasureMapChest chest)
        {

        }
        public TreasureMap(Serial serial)
            : base(serial)
        {
        }
        #region DIGGING
        public void OnBeginDig(Mobile from)
        {
            if (m_Completed)
            {
                from.SendLocalizedMessage(503014); // This treasure hunt has already been completed.
            }
            else if (PublishInfo.Publish < 13.0 && from != m_Decoder)
            {
                from.SendLocalizedMessage(503016); // Only the person who decoded this map may actually dig up the treasure.
            }
            else if (PublishInfo.Publish >= 13.0 && from != m_Decoder && !HasRequiredSkill(from))
            {
                from.SendLocalizedMessage(503031); // You did not decode this map and have no clue where to look for the treasure.
            }
            else if (!from.CanBeginAction(typeof(TreasureMap)))
            {
                from.SendLocalizedMessage(503020); // You are already digging treasure.
            }
            else
            {
                from.SendLocalizedMessage(503033); // Where do you wish to dig?
                from.Target = new DigTarget(this);
            }
        }

        public void OnNPCBeginDig(Mobile from)
        {
            TreasureMap m_TMap = this;
            Point2D loc = m_TMap.m_ChestLocation;
            int x = loc.X, y = loc.Y;
            Map map = m_TMap.m_Map;
            int z = map.GetAverageZ(x, y);

            if (from.BeginAction(typeof(TreasureMap)))
            {
                new DigTimer(from, m_TMap, new Point3D(x, y, z - 14), map, z, m_TMap.m_type).Start();
            }
        }

        private class DigTarget : Target
        {
            private TreasureMap m_Map;

            public DigTarget(TreasureMap map)
                : base(6, true, TargetFlags.None)
            {
                m_Map = map;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Map.Deleted)
                    return;

                if (m_Map.m_Completed)
                {
                    from.SendLocalizedMessage(503014); // This treasure hunt has already been completed.
                }
                else if (PublishInfo.Publish < 13.0 && from != m_Map.m_Decoder)
                {
                    from.SendLocalizedMessage(503016); // Only the person who decoded this map may actually dig up the treasure.
                }
                else if (PublishInfo.Publish >= 13.0 && from != m_Map.m_Decoder && !m_Map.HasRequiredSkill(from))
                {
                    from.SendLocalizedMessage(503031); // You did not decode this map and have no clue where to look for the treasure.
                }
                else if (!from.CanBeginAction(typeof(TreasureMap)))
                {
                    from.SendLocalizedMessage(503020); // You are already digging treasure.
                }
                else
                {
                    IPoint3D p = targeted as IPoint3D;

                    if (p is Item)
                        p = ((Item)p).GetWorldLocation();

                    int maxRange;
                    double skillValue = from.Skills[SkillName.Mining].Value;

                    if (skillValue >= 100.0)
                        maxRange = 4;
                    else if (skillValue >= 81.0)
                        maxRange = 3;
                    else if (skillValue >= 51.0)
                        maxRange = 2;
                    else
                        maxRange = 1;

                    Point2D loc = m_Map.m_ChestLocation;
                    int x = loc.X, y = loc.Y;
                    Map map = m_Map.m_Map;

                    if (map == from.Map && Utility.InRange(new Point3D(p), new Point3D(loc, 0), maxRange))
                    {
                        if (from.Location.X == loc.X && from.Location.Y == loc.Y)
                        {
                            from.SendLocalizedMessage(503030); // The chest can't be dug up because you are standing on top of it.
                        }
                        else if (map != null)
                        {
                            int z = map.GetAverageZ(x, y);

                            if (!Utility.CanFit(map, x, y, z, 16, Utility.CanFitFlags.checkBlocksFit | Utility.CanFitFlags.checkMobiles | Utility.CanFitFlags.requireSurface))
                            {
                                from.SendLocalizedMessage(503021); // You have found the treasure chest but something is keeping it from being dug up.
                            }
                            else if (from.BeginAction(typeof(TreasureMap)))
                            {
                                new DigTimer(from, m_Map, new Point3D(x, y, z - 14), map, z, m_Map.m_type).Start();
                            }
                            else
                            {
                                from.SendLocalizedMessage(503020); // You are already digging treasure.
                            }
                        }
                    }
                    else if (Utility.InRange(new Point3D(p), from.Location, 8)) // We're close, but not quite
                    {
                        from.SendLocalizedMessage(503032); // You dig and dig but no treasure seems to be here.
                    }
                    else
                    {
                        from.SendLocalizedMessage(503035); // You dig and dig but fail to find any treasure.
                    }
                }
            }

        }

        private class DigTimer : Timer
        {
            private Mobile m_From;
            private TreasureMap m_TreasureMap;
            private Map m_Map;
            private TreasureMapChest m_Chest;
            private int m_Count;
            private int m_Z;
            private long m_NextSkillTime;
            private DateTime m_NextSpellTime;
            private long m_NextActionTime;
            private DateTime m_LastMoveTime;
            private ChestThemeType type;
            private bool themed;
            public DigTimer(Mobile from, TreasureMap treasureMap, Point3D p, Map map, int z, ChestThemeType m_type)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {

                m_From = from;
                m_TreasureMap = treasureMap;
                m_Map = map;
                m_Z = z;
                type = m_type;
                themed = m_TreasureMap.m_Themed;

                if (Core.RuleSets.ThemedTreasureMaps())
                {
                    if (themed == false)
                        themed = TreasureTheme.GetIsThemed(m_TreasureMap.Level);

                    m_TreasureMap.m_Themed = themed;

                    if (themed == true && type == ChestThemeType.None)
                    {
                        type = (ChestThemeType)TreasureTheme.GetThemeType(m_TreasureMap.Level);
                    }
                }

                m_TreasureMap.m_type = type;
                m_Chest = new TreasureMapChest(from.Player ? from : null, m_TreasureMap.m_Level, themed, type);
                m_TreasureMap.ChestCreated(m_Chest);
                m_Chest.MoveToWorld(p, map);

                m_NextSkillTime = from.NextSkillTime;
                m_NextSpellTime = from.NextSpellTime;
                m_NextActionTime = from.NextActionTime;
                m_LastMoveTime = from.LastMoveTime;
            }

            protected override void OnTick()
            {
                if (m_NextSkillTime != m_From.NextSkillTime || m_NextSpellTime != m_From.NextSpellTime || m_NextActionTime != m_From.NextActionTime)
                {
                    Stop();
                    m_From.EndAction(typeof(TreasureMap));
                    m_Chest.Delete();
                }
                else if (m_LastMoveTime != m_From.LastMoveTime)
                {
                    m_From.SendLocalizedMessage(503023); // You cannot move around while digging up treasure. You will need to start digging anew.

                    Stop();
                    m_From.EndAction(typeof(TreasureMap));
                    m_Chest.Delete();
                }
                /*else if ( !m_Map.CanFit( m_Chest.X, m_Chest.Y, m_Z, 16, true, true ) )
				{
					m_From.SendLocalizedMessage( 503024 ); // You stop digging because something is directly on top of the treasure chest.

					Stop();
					m_From.EndAction( typeof( TreasureMap ) );
					m_Chest.Delete();
				}*/
                else
                {
                    m_From.RevealingAction();

                    m_Count++;

                    m_Chest.Location = new Point3D(m_Chest.Location.X, m_Chest.Location.Y, m_Chest.Location.Z + 1);
                    m_From.Direction = m_From.GetDirectionTo(m_Chest.GetWorldLocation());

                    if (m_Count == 14)
                    {
                        Stop();
                        m_From.EndAction(typeof(TreasureMap));

                        m_TreasureMap.Completed = true;

                        // checks to see if the map is a themed map and if so gets the theme type based on level of map
                        // and sends appropriate theme message/warning

                        // checks to see if the map is a themed map and already has a theme set
                        // and sends appropriate theme message/warning
                        if (themed == true && type != ChestThemeType.None) m_From.SendMessage(TreasureTheme.GetThemeMessage(type));

                        if (m_TreasureMap.Level >= 2)
                        {
                            //generates 1 of the highest mobs for pirate or undead iob chests
                            TreasureTheme.Spawn(m_TreasureMap.Level, m_Chest.Location, m_Chest.Map, null, themed, type, true, true);
                            //generates guardian spawn numbers based on if themed or not
                            for (int i = 0; i < TreasureTheme.GetGuardianSpawn(themed, type); ++i)
                            {
                                if (type == ChestThemeType.Undead || type == ChestThemeType.Pirate)
                                {
                                    //spawns rest of pirate or undead initial guardian spawn with our highest rank mobs appearing
                                    TreasureTheme.Spawn(m_TreasureMap.Level, m_Chest.Location, m_Chest.Map, null, themed, type, true, false);
                                }
                                else
                                {
                                    //not pirate or undead chest spawn as per normal random guardians
                                    TreasureTheme.Spawn(m_TreasureMap.Level, m_Chest.Location, m_Chest.Map, null, themed, type, false, false);
                                }
                            }

                            // 25% chance to spawn a tax collector on a regular chest, 100% on themed chests
                            if ((m_TreasureMap.Level > 3 &&
                                (Utility.RandomChance(25)) || themed == true) &&
                                Core.RuleSets.ThemedTreasureMaps() && Core.RuleSets.AngelIslandRules())
                            {
                                TaxCollector tc = new TaxCollector(m_Chest);
                                Point3D px = Spawner.GetSpawnPosition(m_Chest.Map, m_Chest.Location, 25, SpawnFlags.SpawnFar | SpawnFlags.AvoidPlayers, tc);
                                if (px != m_Chest.Location)
                                {   // got a good location
                                    tc.MoveToWorld(px, m_Chest.Map);
                                }

                                // if we get a tax collector, add a chance to get an additional rare. The chance is calc in the rare drop code
                                // this chest 'hides' another treasure
                                m_Chest.SetFlag(TreasureMapChest.iFlags.Hides, true);
                            }
                        }
                    }
                    else
                    {
                        if (m_From.Body.IsHuman && !m_From.Mounted)
                            m_From.Animate(11, 5, 1, true, false, 0);

                        new SoundTimer(m_From, 0x125 + (m_Count % 2)).Start();
                    }
                }
            }

            private class SoundTimer : Timer
            {
                private Mobile m_From;
                private int m_SoundID;

                public SoundTimer(Mobile from, int soundID)
                    : base(TimeSpan.FromSeconds(0.9))
                {
                    m_From = from;
                    m_SoundID = soundID;
                }

                protected override void OnTick()
                {
                    m_From.PlaySound(m_SoundID);
                }
            }
        }
        #endregion DIGGING
        #region CLICKING
        public virtual double GetReqSkillLevel(Mobile from)
        {
            switch (m_Level)
            {
                case 1: return 27.0;
                case 2: return 71.0;
                case 3: return 81.0;
                case 4: return 91.0;
                case 5: return 100.0;

                default: return 0.0;
            }
        }

        private bool HasRequiredSkill(Mobile from)
        {
            return (from.Skills[SkillName.Cartography].Value >= GetReqSkillLevel(from));
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (!m_Completed && m_Decoder == null)
            {
                double midPoint = GetReqSkillLevel(from);
                double minSkill = midPoint - 30.0;
                double maxSkill = midPoint + 30.0;

                if (from.Skills[SkillName.Cartography].Value < midPoint)
                {
                    from.SendLocalizedMessage(503013); // The map is too difficult to attempt to decode.
                    return;
                }

                if (from.CheckSkill(SkillName.Cartography, minSkill, maxSkill, contextObj: new object[2]))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503019); // You successfully decode a treasure map!
                    from.SendMessage("You may need a cartographer's sextant to locate this treasure.");
                    Decoder = from;

                    from.PlaySound(0x249);
                    base.OnDoubleClick(from);

                    if (m_Level > 3)
                        UsageReport(from, string.Format("level {0} map decoded", m_Level));
                }
                else
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503018); // You fail to make anything of the map.
                }
            }
            else if (m_Completed)
            {
                from.SendLocalizedMessage(503014); // This treasure hunt has already been completed.
                base.OnDoubleClick(from);
            }
            else
            {
                from.SendMessage("You will need a cartographer's sextant to locate this treasure.");
                from.SendLocalizedMessage(503017); // The treasure is marked by the red pin. Grab a shovel and go dig it up!
                base.OnDoubleClick(from);
            }
        }

        public static void UsageReport(Mobile m, string text)
        {
            // Tell staff that an a player is using this system
            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.TreasureMapUsageReport))
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Administrator,
                0x482,
                string.Format("At location: {0}, {1} ", m.Location, text));
        }

        public override int LabelNumber { get { return (m_Decoder != null ? 1041516 + m_Level : 1041510 + m_Level); } }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(m_Map == Map.Felucca ? 1041502 : 1041503); // for somewhere in Felucca : for somewhere in Trammel

            if (m_Completed)
                list.Add(1041507, m_Decoder == null ? "someone" : m_Decoder.Name); // completed by ~1_val~
        }
        // changed this 
        // public override void OnSingleClick( Mobile from )
        //{
        //if ( m_Completed )
        //from.Send( new MessageLocalizedAffix( Serial, ItemID, MessageType.Label, 0x3B2, 3, 1048030, "", AffixType.Append, string.Format( " completed by {0}", m_Decoder == null ? "someone" : m_Decoder.Name ), "" ) );
        //else if ( m_Decoder != null )
        //LabelTo( from, 1041516 + m_Level );
        //else
        //LabelTo( from, 1041522, string.Format( "#{0}\t \t#{1}", 1041510 + m_Level, m_Map == Map.Felucca ? 1041502 : 1041503 ) );
        //}
        //to this
        public override void OnSingleClick(Mobile from)
        {
            if (m_Completed)
                from.Send(new MessageLocalizedAffix(Serial, ItemID, MessageType.Label, 0x3B2, 3, 1048030, "", AffixType.Append, string.Format(" completed by {0}", m_Decoder == null ? "someone" : m_Decoder.Name), ""));
            else if (m_Decoder != null)
            {   // non tattered
                // "an adeptly drawn treasure map";
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules() || Core.RuleSets.RenaissanceRules() || (Core.RuleSets.SiegeRules() && PublishInfo.Publish >= 13))
                    LabelTo(from, 1041516 + m_Level);
                else
                    LabelTo(from, string.Format("a treasure map"));
            }
            else
            {   // tattered
                // "a tattered, adeptly drawn treasure map"
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules() || Core.RuleSets.RenaissanceRules() || (Core.RuleSets.SiegeRules() && PublishInfo.Publish >= 13))
                {
                    LabelTo(from, 1041510 + m_Level);
                    LabelTo(from, m_Map == Map.Felucca ? 1041502 : 1041503);
                }
                else
                {
                    LabelTo(from, string.Format("a tattered treasure map", m_Level));
                }
            }
        }
        #endregion CLICKING
        #region Serialization
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
            writer.Write(m_Themed);
            writer.Write((int)m_type);

            writer.Write(m_Level);
            writer.Write(m_Completed);
            writer.Write(m_Decoder);
            writer.Write(m_Map);
            writer.Write(m_ChestLocation);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Themed = reader.ReadBool();
                        m_type = (ChestThemeType)reader.ReadInt();
                        goto case 0;
                    }

                case 0:
                    {
                        m_Level = (int)reader.ReadInt();
                        m_Completed = reader.ReadBool();
                        m_Decoder = reader.ReadMobile();
                        m_Map = reader.ReadMap();
                        m_ChestLocation = reader.ReadPoint2D();

                        break;
                    }
            }
            if (version < 1)
            {
                m_Themed = false;
                m_type = ChestThemeType.None;
            }
        }
        #endregion Serialization
    }

    public class TreasureMapQuest : TreasureMap
    {
        Item m_RaresPack;
        string m_MapFile;
        [CommandProperty(AccessLevel.GameMaster)]
        public Item RaresPack { get { return m_RaresPack; } /*set { m_RaresPack = value; }*/ }
        private string m_DecodedMsg = null;
        [CommandProperty(AccessLevel.GameMaster)]
        public string DecodedMsg
        {
            get { return m_DecodedMsg; }
            set
            {
                m_DecodedMsg = value;
                SendSystemMessage("Hint: you can use the {decoder} macro to substitute in the decoder's name.");
            }
        }

        private string m_NotDecodedMsg = null;
        [CommandProperty(AccessLevel.GameMaster)]
        public string NotDecodedMsg { get { return m_NotDecodedMsg; } set { m_NotDecodedMsg = value; } }

        private string m_CompletedMsg = null;
        [CommandProperty(AccessLevel.GameMaster)]
        public string CompletedMsg
        {
            get { return m_CompletedMsg; }
            set
            {
                m_CompletedMsg = value;
                SendSystemMessage("Hint: you can use the {decoder} macro to substitute in the decoder's name.");
            }
        }

        [Constructable]
        public TreasureMapQuest(int level, Map map, string MapFile, int RaresPackSerial)
            : base(level, map, false, ChestThemeType.None)
        {
            /* Fail vs ApplicationException
             * 1. Commandline [Add x y z
             *      Fail will generate a nice system message for the GM to see
             *      ApplicationException will generate template exception information issued by the CLR. You can't catch this exception without some fancy tricks.
             * 2. Created by the spawner (SpawnEngine.Build<Item>(value, ref reason))
             *      Fail generates a message likely on the internal or null map, so the GM never sees it
             *      ApplicationException will generate a nice system message for the GM to see
             * --- Conclusion ---
             * We'll just go with the ApplicationException since:
             * A. the most dangerous case is when the GM adds TreasureMapQuest to the LootString. We want the best possible message for this scenario.
             * B. if the GM issues the [Add x y z command line, we can survive with the template exception with the required param list
             * Final note: When using the Fail technique, the property doesn't get cleared like it does using the ApplicationException
             */
            m_MapFile = MapFile;
            if (!MapFileOK())
            {
                throw new ApplicationException(string.Format("Map file '{0}' could not be found", m_MapFile));
                //Timer.DelayCall(Fail, string.Format("Failure: Map file '{0}' could not be found", m_MapFile));
                goto done;
            }

            Item pack = World.FindItem(RaresPackSerial);
            if (pack is Container cont)
            {
                if (cont.Items.Count == 0)
                {
                    throw new ApplicationException(string.Format("No rares in rare pack ({0})", RaresPackSerial));
                    //Timer.DelayCall(Fail, string.Format("Failure: No rares in rare pack ({0})", RaresPackSerial));
                    goto done;
                }
                m_RaresPack = Utility.DeepDupe(pack);
                m_RaresPack.MoveToIntStorage();
                base.Setup();
            }
            else
            {
                throw new ApplicationException(string.Format("No rares pack with serial {0} could be found", RaresPackSerial));
                //Timer.DelayCall(Fail, string.Format("Failure: No rares pack with serial {0} could be found", RaresPackSerial));
                goto done;
            }

            if (level > 1)
                Timer.DelayCall(Warning, "Warning: Only level 1 maps can be decoded by anyone. All other maps require the usual skill.");

            done:
            return;
        }
        public override void OnSingleClick(Mobile from)
        {
            if (Completed)
            {
                string decoder = string.Format("{0}", Decoder == null ? "someone" : Decoder.Name);
                if (!string.IsNullOrEmpty(m_CompletedMsg))
                    //from.Send(new MessageLocalizedAffix(Serial, ItemID, MessageType.Label, 0x3B2, 3, 1048030, "", AffixType.Append, m_CompletedMsg.Replace("{decoder}", decoder), ""));
                    LabelTo(from, m_CompletedMsg.Replace("{decoder}", decoder));
                else
                    from.Send(new MessageLocalizedAffix(Serial, ItemID, MessageType.Label, 0x3B2, 3, 1048030, "", AffixType.Append, string.Format(" completed by {0}", Decoder == null ? "someone" : Decoder.Name), ""));
            }
            else if (Decoder != null)
            {
                string decoder = string.Format("{0}", Decoder == null ? "someone" : Decoder.Name);
                if (!string.IsNullOrEmpty(m_DecodedMsg))
                    LabelTo(from, m_DecodedMsg.Replace("{decoder}", decoder));
                else
                    // non tattered
                    // "an adeptly drawn treasure map";
                    LabelTo(from, 1041516 + Level);
            }
            else
            {
                if (!string.IsNullOrEmpty(m_NotDecodedMsg))
                    LabelTo(from, m_NotDecodedMsg);
                else
                {
                    // tattered
                    // "a tattered, adeptly drawn treasure map"
                    LabelTo(from, 1041510 + Level);
                    LabelTo(from, Map == Map.Felucca ? 1041502 : 1041503);
                }
            }
        }
        public override void OnDelete()
        {
            base.OnDelete();
            if (m_RaresPack != null && !m_RaresPack.Deleted)
                m_RaresPack.Delete();
        }
        private void Warning(object o)
        {
            if (o is string s)
                SendSystemMessage(s, AccessLevel.GameMaster, 0x33);
        }
        //private void Fail(object o)
        //{
        //    if (o is string s)
        //        SendSystemMessage(s, AccessLevel.GameMaster, 0x33);

        //    this.Delete();
        //}
        private bool MapFileOK()
        {
            string path = string.Format("Binary Map Data/{0}", (ChestMap == Map.Felucca || ChestMap == Map.Trammel) ? "Britannia" : ChestMap.Name);
            return File.Exists(Path.Combine(Core.DataDirectory, path, m_MapFile + ".bin"));
        }
        private static Dictionary<string, List<Point2D>> LocationsTable = new Dictionary<string, List<Point2D>>(StringComparer.OrdinalIgnoreCase);
        public override Point2D GetRandomLocation(Map map)
        {
            Point2D point = Point2D.Zero;
            List<Point2D> locations;
            if (LocationsTable.ContainsKey(m_MapFile))
                locations = LocationsTable[m_MapFile];
            else
            {
                string path = string.Format("Binary Map Data/{0}", (ChestMap == Map.Felucca || ChestMap == Map.Trammel) ? "Britannia" : ChestMap.Name);
                locations = LocationsTable[m_MapFile] = LoadSpawnablePoints(Path.Combine(Core.DataDirectory, path, m_MapFile + ".bin"));
            }

            List<Point2D> list = new();
            for (int ix = 0; ix < locations.Count; ix++)
            {
                Point2D current = locations[Utility.Random(locations.Count)];
                if (Utility.CanSpawnLandMobile(map, new Point3D(current.X, current.Y, map.GetAverageZ(current.X, current.Y))))
                {
                    point = current;
                    break;
                }
            }

            return point;
        }
        public override double GetReqSkillLevel(Mobile from)
        {
            switch (base.Level)
            {   // level 1 maps can be decoded by anyone (whatever their skill is, that's enough)
                case 1: return from.Skills[SkillName.Cartography].Value;
                case 2: return 71.0;
                case 3: return 81.0;
                case 4: return 91.0;
                case 5: return 100.0;

                default: return 0.0;
            }
        }
        public List<Point2D> LoadSpawnablePoints(string pathname)
        {
            List<Point2D> list = new();
            if (File.Exists(pathname))
                Console.WriteLine("Spawnable points Loading...");
            else
            {
                Console.WriteLine("No spawnable points to Load.");
                return list;
            }
            try
            {
                Utility.TimeCheck tc = new Utility.TimeCheck();
                tc.Start();
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(pathname, FileMode.Open, FileAccess.Read)));

                int count = reader.ReadInt();
                for (int ix = 0; ix < count; ix++)
                    list.Add(reader.ReadPoint2D());

                reader.Close();
                tc.End();
                Console.WriteLine($"Loaded {list.Count} points in {tc.TimeTaken}");

                Console.WriteLine("Cleaning up points database...");
                tc.Start();
                List<Point2D> bad = new();
                foreach (var p2d in list)
                    if (BadLocation(p2d))
                        bad.Add(p2d);

                foreach (var p2d in bad)
                    list.Remove(p2d);

                tc.End();
                Console.WriteLine($"Removed {bad.Count} points, leaving in {list.Count} valid in {tc.TimeTaken}");
            }
            catch
            {
                Utility.Monitor.WriteLine($"Error reading {pathname}, using default values:", ConsoleColor.Red);
            }

            return list;
        }
        private static List<int> SnowTiles = new List<int>()
        {
            0x10C, 0x10F,
            0x114, 0x117,
            0x119, 0x11D,
            0x179, 0x18A,
            0x385, 0x38C,
            0x391, 0x394,
            0x39D, 0x3A4,
            0x3A9, 0x3AC,
            0x5BF, 0x5D6,
            0x5DF, 0x5E2,
            0x745, 0x748,
            0x751, 0x758,
            0x75D, 0x760,
            0x76D, 0x773
        };
        private bool BadLocation(Point2D loc)
        {
            Point3D p = new Point3D(loc.X, loc.Y, ChestMap.GetAverageZ(loc.X, loc.Y));

            LandTarget land = new LandTarget(p, ChestMap);
            if (land != null)
            {
                int tileID = land.TileID & 0x3FFF;
                bool contains = false;

                for (int i = 0; !contains && i < SnowTiles.Count; i += 2)
                    contains = (tileID >= SnowTiles[i] && tileID <= SnowTiles[i + 1]);

                if (contains)
                    return false;
            }

            return true;
        }
        public override void Setup() {; }
        public override void ChestCreated(TreasureMapChest chest)
        {
            // easy mode
            if (chest is ILockable lockable && base.Level == 1)
                lockable.Locked = false;

            if (m_RaresPack is Container c)
            {
                ArrayList alist = c.GetDeepItems();
                if (alist.Count > 0)
                {
                    Item reward = (Item)alist[Utility.Random(alist.Count)];
                    if (reward != null)
                    {
                        if ((reward = Utility.Dupe(reward)) != null)
                            ManagedDropItem(chest, reward);
                    }
                }
                return;
            }
            System.Diagnostics.Debug.Assert(false);
        }
        private void ManagedDropItem(Container c, Item item)
        {
            int before = c.Items.Count;     // TryDropItem is funky because it requires a Mobile from
            c.DropItem(item);               // this gives us nice placement withing the chest, AddItem does not (always upper right corner.)
            int after = c.Items.Count;
            if (before == after)            // check if we overflowed
                c.AddItem(item);            // force the add
        }
        #region Serialization
        public TreasureMapQuest(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 2
            writer.Write(m_DecodedMsg);
            writer.Write(m_NotDecodedMsg);
            writer.Write(m_CompletedMsg);

            // version 1
            writer.Write(m_RaresPack);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_DecodedMsg = reader.ReadString();
                        m_NotDecodedMsg = reader.ReadString();
                        m_CompletedMsg = reader.ReadString();

                        goto case 1;
                    }
                case 1:
                    {
                        m_RaresPack = reader.ReadItem();
                        break;
                    }
            }
        }
        #endregion Serialization
    }
}