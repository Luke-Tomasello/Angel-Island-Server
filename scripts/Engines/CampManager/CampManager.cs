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

/* Scripts\Engines\CampManager\CampManager.cs
 * CHANGELOG:
 *	8/12/2023, Adam
 *		first time check in
 */

using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Server.Utility;

namespace Server.Engines
{
    public static class CampManager
    {
        private static List<Point3D> m_database = new List<Point3D>();
        public static List<Point3D> Database { get { return m_database; } }

        private static List<Point3D> m_mineable = new List<Point3D>();
        public static List<Point3D> Mineable { get { return m_mineable; } }
        public static List<Point3D> NonMineable { get { return GetNonMineableCache(); } }
        public static List<Point3D> All { get { return GetAllCache(); } }
        public static List<Point3D> m_nonMineableCache = null;
        public static List<Point3D> m_allCache = null;
        private static List<Point3D> GetNonMineableCache()
        {
            if (m_nonMineableCache == null)
                m_nonMineableCache = m_database.Where(p => !m_mineable.Any(p2 => p2 == p)).ToList();
            return m_nonMineableCache;
        }
        private static List<Point3D> GetAllCache()
        {
            if (m_allCache == null)
                m_allCache = m_database.Union(m_mineable).ToList();
            return m_allCache;
        }
        /*
         * if (cache == null)
                cache = Engines.CampManager.Database.Where(p => !Engines.CampManager.Mineable.Any(p2 => p2 == p)).ToList();
         */
        public static void Configure()
        {   // spawners were already loaded in Configure which comes before Initialize
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            //EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }

        public static void Initialize()
        {
            Server.CommandSystem.Register("GenerateDynCamps", AccessLevel.Owner, new CommandEventHandler(GenerateDynCamps));

        }
        public static void GenerateDynCamps(CommandEventArgs e)
        {
            // spawners were already loaded in Configure which comes before Initialize
            Database.Clear();
            Mineable.Clear();
            Generate();
            Save();
        }

        private static void Generate()
        {
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();

            #region General Locations
            Utility.ConsoleWriteLine("CampManager: Generating camp locations, this will take a while...", ConsoleColor.Yellow);
            if (TreasureMap.Locations == null || TreasureMap.Locations.Length == 0)
            {   // TreasureMap Locations should already have been loaded
                Utility.ConsoleWriteLine("CampManager: No treasure map locations", ConsoleColor.Red);
                return;
            }

            foreach (Point2D p2d in TreasureMap.Locations)
            {
                Point3D p3d = new Point3D(p2d.X, p2d.Y, Map.Felucca.GetAverageZ(p2d.X, p2d.Y));
                // try 17 times (one screen) to get a new spawn position
                if (CheckLocation(ref p3d, Map.Felucca))
                    m_database.Add(p3d);
            }
            #endregion General Locations

            #region Mineable Locations
            HarvestDefinition def = Mining.System.OreAndStone;
            m_mineable.Clear();
            Hashtable banks = (Hashtable)def.Banks[Map.Felucca];
            int[] tiles = def.Tiles;
            const int extendedRange = 0;
            foreach (Point3D gp3d in m_database)
            {
                Rectangle2D rect = new Rectangle2D(width: 42, height: 42, new Point2D(gp3d.X, gp3d.Y));

                List<Point2D> list = rect.PointsInRect2D();
                foreach (var px in list)
                {
                    LandTile landTile = Map.Felucca.Tiles.GetLandTile(px.X, px.Y);
                    if (tiles.Contains(landTile.ID))
                        if (!m_mineable.Contains(gp3d))
                            m_mineable.Add(gp3d);
                }
            }
            #endregion Mineable Locations

            tc.End();
            Utility.ConsoleWriteLine("CampManager: Generated {0}/{1} camp/mineable locations in {2}", ConsoleColor.Yellow, Database.Count, m_mineable.Count, tc.TimeTaken);
        }
        public static bool CheckLocation(ref Point3D p3d, Map map, int retries = 17)
        {
            bool rule1 = BaseMulti.Find(p3d, map) == null;  // should be fast
            bool rule2 = CheckFit(ref p3d, map, retries);       // could be slow
            return (rule1 && rule2);
        }
        private static bool CheckFit(ref Point3D location, Map map, int retries)
        {
            int multiID = 0x6A;        // wooden house 7x7
            ArrayList toMove = new ArrayList();
            int homeRange;
            Point3D new_location = Point3D.Zero;
            HousePlacementResult res = HousePlacementResult.BadLand;
            for (homeRange = 0; homeRange < retries; homeRange++)
            {   // try N times to get a new spawn position
                if (retries > 1)
                    new_location = Spawner.GetSpawnPosition(Map.Felucca, location, homeRange, SpawnFlags.SpawnFar | SpawnFlags.AvoidPlayers | SpawnFlags.ClearPath, null);
                else
                    new_location = location;
                res = HousePlacement.Check(null, multiID, new_location, out toMove, false);

                if (res == HousePlacementResult.Valid)
                {   // save the actual location we will be using
                    location = new_location;
                    break;
                }
            }
            // now, if there were any animals/items that were in the way, move them at this time.
            if (res == HousePlacementResult.Valid && toMove.Count > 0)
            {
                foreach (object ox in toMove)
                {
                    for (homeRange = 0; homeRange < 50; homeRange++)
                    {   // try 50 times to get a new spawn position
                        new_location = Spawner.GetSpawnPosition(Map.Felucca, location, homeRange, SpawnFlags.SpawnFar, null);
                        if (new_location != location)
                            break;
                    }

                    if (new_location == location)
                    {   // give up (we will delete the spawn.)
                        if (ox is Mobile && (ox as Mobile).Player != true)
                            (ox as Mobile).Delete();
                        /*else item (maybe spawner,) or real player. In this case we just punt, and will deal with it as needed.
                         spawner under camp, stuck player, etc. */
                    }
                    else
                    {
                        if (ox is Mobile mobile && !mobile.Player)
                            (ox as Mobile).MoveToWorld(new_location, Map.Felucca);
                        else if (ox is Item item && item.Visible && item.Movable)
                            (ox as Item).MoveToWorld(new_location, Map.Felucca);
                    }
                }
            }

            return res == HousePlacementResult.Valid;
        }
        #region Serialization
        public static void Load()
        {
            string CMFileName = Path.Combine(Core.DataDirectory, "CampManager.bin");
            if (!File.Exists(CMFileName))
                return;

            Console.WriteLine("Camp Manager Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(CMFileName, FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                                m_database.Add(reader.ReadPoint3D());

                            count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                                m_mineable.Add(reader.ReadPoint3D());
                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid CampManager.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error reading Data/CampManager.bin, using default values:");
                Utility.PopColor();
            }
        }
        public static void Save(/*WorldSaveEventArgs e*/)
        {
            string CMFileName = Path.Combine(Core.DataDirectory, "CampManager.bin");
            if (m_database.Count == 0)
                return;

            Console.WriteLine("CampManager Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter(CMFileName, true);
                int version = 1;
                writer.Write((int)version);

                switch (version)
                {
                    case 1:
                        {
                            writer.Write(m_database.Count);
                            foreach (Point3D p in m_database)
                                writer.Write(p);

                            writer.Write(m_mineable.Count);
                            foreach (Point3D p in m_mineable)
                                writer.Write(p);
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Data/CampManager.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion Serialization
    }
}