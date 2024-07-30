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

/* scripts\Engines\Events\SiegeLaunchEvent\Controller.cs
 * Changelog:
 *  4/14/23, remove faction regions
 *  12/29/22, Adam
 *     First time check in.
 *      This module manages the Siege Launch Event
 */

using Server;
using Server.Commands;
using Server.Factions;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Server.Engines.PBSDistrictManager;
using static Server.Utility;
namespace Engines
{

    public static class Controller
    {
        protected enum Stronghold
        {
            None = 0,
            Minax,
            CouncilofMages,
            TrueBritannians,
            Shadowlords,
        }

        public static void Initialize()
        {
            //            CommandSystem.Register("ListActiveDistricts", AccessLevel.Administrator, new CommandEventHandler(ListActiveDistricts_OnCommand));
            CommandSystem.Register("SiegeLaunchEventSpawn", AccessLevel.Administrator, new CommandEventHandler(SiegeLaunchEventSpawn_OnCommand));
            //            CommandSystem.Register("DespawnDistricts", AccessLevel.Administrator, new CommandEventHandler(DespawnDistricts_OnCommand));
            CommandSystem.Register("SiegeLaunchEventLoad", AccessLevel.Administrator, new CommandEventHandler(SiegeLaunchEventLoad_OnCommand));
            //            CommandSystem.Register("ZoneBounds", AccessLevel.GameMaster, new CommandEventHandler(ZoneBounds_OnCommand));
        }
        [Usage("SiegeLaunchEventLoad")]
        [Description("Setups PBS districts.")]
        private static void SiegeLaunchEventLoad_OnCommand(CommandEventArgs e)
        {
            Map map = Map.Felucca;

            List<Region> exclude = new()
            {
                Region.FindByName("Angel Island", map),         // leave alone
                Region.FindByName("Jail", map),                 // leave alone
                Region.FindByName("Green Acres", map),          // leave alone
                Region.FindByName("Britain", map),              // safe place for players
                Region.FindByName("Britain Graveyard", map)     // safe place for players
                
            };
            List<Region> include;
            if (Core.Factions)
                include = new()
                {
                    Region.FindByName("Minax", map),                // special spawn
                    Region.FindByName("Council of Mages", map),     // special spawn
                    Region.FindByName("True Britannians", map),     // special spawn
                    Region.FindByName("Shadowlords", map)           // special spawn
                };
            else
                include = new();

            // region rectangles we don't want processed
            foreach (Region rx in exclude)
            {
                if (rx != null)
                {
                    foreach (Rectangle3D r3d in rx.Coords)
                    {
                        Rectangle2D r2d = new Rectangle2D(r3d.Start, r3d.End);
                        ConfigureDistricts.Exclude.Add(r2d);
                    }
                }
            }
            // zones we want handled as a whole, not split, and not based on the spawners within (zonal)
            foreach (Region rx in include)
            {
                if (rx != null)
                {
                    foreach (Rectangle3D r3d in rx.Coords)
                    {
                        Rectangle2D r2d = new Rectangle2D(r3d.Start, r3d.End);
                        ConfigureDistricts.Include.Add(r2d);
                    }
                }
            }

            LoadDistricts_OnCommand(e);
        }

        [Usage("SiegeLaunchEventSpawn")]
        [Description("Spawns all PBS districts.")]
        private static void SiegeLaunchEventSpawn_OnCommand(CommandEventArgs e)
        {   // our callback when the District Manager selects a point to spawn.
            //  when we are called back, we can adjust the spawn.
            ConfigureDistricts.PointSelected = SpawnPointSelected;
            // this callback is called when the district manager wants to spawn a zone in zonal mode.
            // the district manager doesn't know how to do this, so we'll figure out the spawner location and creature count for him
            ConfigureDistricts.GetSpawnerData = GetSpawnerData;
            // when a spawner is created, it is added to our EventManager
            ConfigureDistricts.SpawnerCreated = SpawnerCreated;
            // These spawners are controlled by the EventManager and therefore should not be spawned 'running'
            ConfigureDistricts.Running = ConfigureDistricts.Respawn = false;

            SpawnDistricts_OnCommand(e);
        }
        public static void SpawnerCreated(object o)
        {
            // add this spawner to the Siege Launch Event
            EventManager.AddToEvent(o as Item, "sle");
        }

        private static Stronghold GetStronghold(Point2D px, Map map)
        {
            Region rx = Region.FindByName("Minax", map);
            if (rx != null && rx.Contains(px))
                return Stronghold.Minax;
            rx = Region.FindByName("Council of Mages", map);
            if (rx != null && rx.Contains(px))
                return Stronghold.CouncilofMages;
            rx = Region.FindByName("True Britannians", map);
            if (rx != null && rx.Contains(px))
                return Stronghold.TrueBritannians;
            rx = Region.FindByName("Shadowlords", map);
            if (rx != null && rx.Contains(px))
                return Stronghold.Shadowlords;

            return Stronghold.None;
        }
        private static Stronghold GetStronghold(Point3D point, Map map)
        {
            FactionStone factionStone = null;
            IPooledEnumerable eable = map.GetItemsInRange(point, 10);
            foreach (Item item in eable)
                if (item is FactionStone fs && !fs.Deleted)
                {
                    factionStone = fs;
                    break;
                }
            eable.Free();

            if (factionStone == null)
                // no faction stone here
                return Stronghold.None;

            return GetStronghold(new Point2D(factionStone.Location.X, factionStone.Location.Y), Map.Felucca);
        }
        public static void GetSpawnerData(object o, object p)
        {   // spawner data will already have been initialized to some default data, we will overwrite
            Dictionary<Point2D, int> spawnerData = (Dictionary<Point2D, int>)o;
            Rectangle2D zone = (Rectangle2D)p;

            FactionStone factionStone = null;
            IPooledEnumerable eable = Map.Felucca.GetItemsInBounds(zone);
            foreach (Item item in eable)
                if (item is FactionStone fs && !fs.Deleted)
                {
                    factionStone = fs;
                    break;
                }
            eable.Free();

            spawnerData.Clear();

            if (factionStone == null)
                // nothing to do - assumes we are only processing faction strongholds
                return;

            int creatureCount = 0;
            switch (GetStronghold(new Point2D(factionStone.Location.X, factionStone.Location.Y), Map.Felucca))
            {
                case Stronghold.TrueBritannians: creatureCount = 10; break;
                default: creatureCount = 30; break;
            }
            int killsNeeded = creatureCount;

            Point3D spawnerLocation = Spawner.GetSpawnPosition(Map.Felucca, factionStone.Location, 2, SpawnFlags.None, factionStone);
            // we spawn light in the True Britannians stronghold since that's the launching point for all factions.
            spawnerData.Add(new Point2D(spawnerLocation.X, spawnerLocation.Y), creatureCount);
        }
#if false
        private static List <Point2D> FindSpawnerPoints(List<Point2D> points, int count)
        {   
            List<Point2D> list = new();
            if (points.Count < count) return list;
            List<Point2D> seen = new();
            List<Point2D> copy= new List<Point2D>(points);
            Utility.Shuffle(copy);
            for (int ix = 0; ix < count; ix++)
                list.Add(copy[ix]);

            return list;
        }
#endif
        /*
         * Slain monsters with faction affiliation:
         * Minax: Ogre Lords – 30 silver
         * Council of Mages: Wisps – 20 silver
         * True Britannians: Silver Serpents – 30 silver
         * Shadowlords: Daemons (not summoned daemons) – 30 silver
         */
        public static void SpawnPointSelected(object o)
        {
            // reset all the spawner parms to defaults
            ConfigureDistricts.ResetSpawnerParms();
            // These spawners are controlled by the EventManager and therefore should not be spawned 'running'
            ConfigureDistricts.Running = ConfigureDistricts.Respawn = false;

            Point3D point = (Point3D)o;

            Map map = Map.Felucca;
            // our default creature is the SLEGargoyle
            if (Core.Factions)
                ConfigureDistricts.ObjectNamesRaw = new() { "SLEGargoyle" };
            else
                ConfigureDistricts.ObjectNamesRaw = new() { Utility.RandomObjectMinMaxScaled<string>(new string[] { "Gargoyle", "FireGargoyle", "StoneGargoyle" }) };

            // override our spawn for the strongholds.
            foreach (Rectangle2D r2d in ConfigureDistricts.Include)
                if (r2d.Contains(point))
                {
                    // what we spawn
                    // exact placement of the spawner, and how many creatures will be calculated in
                    //  the GetSpawnerData() callback above.
                    ConfigureDistricts.ObjectNamesRaw = WhatToSpawn(r2d, map);
                    switch (GetStronghold(point, map))
                    {
                        case Stronghold.TrueBritannians:
                            // we go easy on TBs as that's the starting place for the invasion
                            ConfigureDistricts.SpawnerKillsNeeded = 10;
                            ConfigureDistricts.WebKillsNeeded = 10;
                            // note: Concentric will spread the mobs out nicely, but ClearPath will typically keep them on the same Z
                            // (unless they can 'path' back to the spawner.) This is a trade off. But without ClearPath, areas like Brit's Castle
                            //  will get spawn inside walls and other player-inaccessible areas.
                            // Note: NoBlock would work here, but with a different spawn distribution. I decided to go with ClearPath
                            //  since brit's castle has the single room where I want the action to take place.
                            ConfigureDistricts.SpawnerFlags = SpawnFlags.ClearPath | SpawnFlags.SpawnFar;
                            break;
                        case Stronghold.CouncilofMages:
                            ConfigureDistricts.SpawnerKillsNeeded = 30;
                            ConfigureDistricts.WebKillsNeeded = 30;
                            // there are a couple of 'stuck spots' in COM stronghold, that's why we use NoBlock here
                            ConfigureDistricts.SpawnerFlags = SpawnFlags.NoBlock | SpawnFlags.SpawnFar;
                            break;
                        case Stronghold.Shadowlords:
                            ConfigureDistricts.SpawnerKillsNeeded = 32;
                            ConfigureDistricts.WebKillsNeeded = 32;
                            // no problems, spawn anywhere including 'up z' (roof) or lower floor
                            ConfigureDistricts.SpawnerFlags = SpawnFlags.AllowChangeZ | SpawnFlags.SpawnFar;
                            break;
                        case Stronghold.Minax:
                            ConfigureDistricts.SpawnerKillsNeeded = 34;
                            ConfigureDistricts.WebKillsNeeded = 34;
                            // no problems, spawn anywhere
                            ConfigureDistricts.SpawnerFlags = SpawnFlags.SpawnFar;
                            break;

                        default:
                            break;
                    }

                    // we want this setup for strongholds
                    ConfigureDistricts.DynamicHomeRange = false;
                    ConfigureDistricts.HomeRange = 30;                                      // fixed amount for strongholds
                    ConfigureDistricts.IsConcentric = true;                                 // distribute creatures evenly
                    ConfigureDistricts.SpawnerLevelRestartDelay = TimeSpan.FromMinutes(30); // how long you have to beat this level (spawner)

                    break;
                }
        }
#if false
        private static bool IsStronghold(Stronghold shid, Point3D point)
        {
            FactionStone factionStone = null;
            IPooledEnumerable eable = Map.Felucca.GetItemsInRange(point, 10);
            foreach (Item item in eable)
                if (item is FactionStone fs && !fs.Deleted)
                {
                    factionStone = fs;
                    break;
                }
            eable.Free();

            if (factionStone == null)
                // no faction stone here
                return false;

            if (GetStronghold(new Point2D(factionStone.Location.X, factionStone.Location.Y), Map.Felucca) == shid)
                return true;

            return false;
        }
#endif
        public static ArrayList WhatToSpawn(Rectangle2D point, Map map)
        {
            List<Region> stronghold = new()
            {
                Region.FindByName("Minax", map),
                Region.FindByName("Council of Mages", map),
                Region.FindByName("True Britannians", map),
                Region.FindByName("Shadowlords", map)
            };
            /*
             * Slain monsters with faction affiliation:
             * Minax: Ogre Lords – 30 silver
             * Council of Mages: Wisps – 20 silver
             * True Britannians: Silver Serpents – 30 silver
             * Shadowlords: Daemons (not summoned daemons) – 30 silver
             */
            Dictionary<string, string> lookup = new()
            {
                {"Minax", "Ogrelord"},
                {"Council of Mages", "Wisp" },
                {"True Britannians", "SilverSerpent" },
                {"Shadowlords", "Daemon" }
            };

            foreach (Region region in stronghold)
            {
                if (region.Contains(point))
                {
                    List<string> list = lookup.Where(x => x.Key != region.Name).Select(x => x.Value).ToList();
                    return new ArrayList(list);
                }
            }

            // default to this... if we see gargoyls, we know we missed some case.
            return new ArrayList(new List<string> { "SLEGargoyle" });
        }
    }
}