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

/* Misc/SeaTreasure.cs
 * CHANGELOG:
 *  1/14/22, Yoar
 *        Moved CalculateDistances to the MathUtility class.
 *  12/21/21, Yoar
 *        Initial version.
 */

using Server.Items;
using Server.Multis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using static Server.Utility;

namespace Server.Misc
{
    public static class SeaTreasure
    {
        // config
        public static Map Map = Map.Felucca;
        public static int Width = 5120;
        public static int Height = 4096;
        public static int WaterZ = -5;
        public static int NumLocs = 500;
        public static int PinDistMin = 150;
        public static int PinDistMax = 200;
        public static string BinFileName = Path.Combine(Core.DataDirectory, "seatreasure.bin");
        public static string TxtFileName = Path.Combine(Core.DataDirectory, "seatreasure.txt");

        private static readonly LevelData[] m_LevelTable = new LevelData[]
            {
                // distanceMin, distanceMax, no. pins
                new LevelData( 6, 20, 2),
                new LevelData(21, 30, 3),
                new LevelData(31, 40, 4),
                new LevelData(41, 50, 5),
                new LevelData(50, 64, 6),
            };

        public static LevelData GetLevelData(int level)
        {
            int index = level - 1;

            if (index >= 0 && index < m_LevelTable.Length)
                return m_LevelTable[index];

            return LevelData.Empty;
        }

        public static int MaxLevel { get { return m_LevelTable.Length; } }

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(EventSink_WorldLoad);
        }

        private static void EventSink_WorldLoad()
        {
            LoadMapPoints();
        }

        public static void Initialize()
        {
            CommandSystem.Register("LoadMapPoints", AccessLevel.Administrator, LoadMapPoints_OnCommand);
            CommandSystem.Register("GenMapPoints", AccessLevel.Administrator, GenMapPoints_OnCommand);
        }

        [Usage("LoadMapPoints")]
        [Description("Loads the list of interesting points out in the water.")]
        private static void LoadMapPoints_OnCommand(CommandEventArgs e)
        {
            try
            {
                LoadMapPoints();
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage("SeaTreasure Error: {0}", ex);

                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            e.Mobile.SendMessage("Reloaded map points.");
        }

        [Usage("GenMapPoints")]
        [Description("Generates a list of interesting points out in the water. Slow.")]
        private static void GenMapPoints_OnCommand(CommandEventArgs e)
        {
            try
            {
                GenMapPoints();
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage("SeaTreasure Error: {0}", ex);

                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            e.Mobile.SendMessage("Generated map points.");
        }

        // Point2D[level][mapIndex][pinIndex]
        private static Point2D[][][] m_Locations = new Point2D[0][][];

        private static readonly Point2D[] m_EmptyPins = new Point2D[0];

        public static Point2D[] GetRandomPins(int level)
        {
            if (level <= 0 || level - 1 >= m_Locations.Length)
                return m_EmptyPins;

            Point2D[][] locs = m_Locations[level - 1];

            if (locs == null || locs.Length == 0)
                return m_EmptyPins;

            Point2D[] pins = locs[Utility.Random(locs.Length)];

            if (pins == null || pins.Length == 0)
                return m_EmptyPins;

            return pins;
        }

        private static void LoadMapPoints()
        {
            using (Stream fs = File.Open(GetFilePath(BinFileName), FileMode.Open, FileAccess.Read))
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(fs));

                if (reader.ReadString() != "seatreasurebin")
                    throw new InvalidOperationException();

                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {
                            int nlvls = reader.ReadInt();

                            m_Locations = new Point2D[nlvls][][];

                            for (int i = 0; i < nlvls; i++)
                            {
                                int level = i + 1;

                                int nlocs = reader.ReadInt();

                                Point2D[][] locs = m_Locations[i] = new Point2D[nlocs][];

                                for (int j = 0; j < nlocs; j++)
                                {
                                    int npins = reader.ReadInt();

                                    Point2D[] pins = locs[j] = new Point2D[npins];

                                    for (int k = 0; k < npins; k++)
                                        pins[k] = reader.ReadPoint2D();
                                }
                            }

                            break;
                        }
                }

                reader.Close();
            }
        }

        private static void GenMapPoints()
        {
            Utility.TimeCheck tc = new Utility.TimeCheck();

            tc.Start();

            Log("Generating sea treasure map points...");

            Log("Calculating distance matrix...");

            ushort[,] distances = CalculateDistanceMatrix();

            Point2D[][][] result = new Point2D[MaxLevel][][];

            for (int i = 0; i < MaxLevel; i++)
            {
                int level = i + 1;

                LevelData data = GetLevelData(level);

                Log("Generating level {0} locations...", level);

                Point2D[][] locs = new Point2D[NumLocs][];

                // list of valid start points
                List<Point2D> startPoints = new List<Point2D>();

                // find valid starting points
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Point2D start = new Point2D(x, y);

                        if (Validate(level, start, distances))
                            startPoints.Add(start);
                    }
                }

                // hash set of start points that we've already used
                HashSet<Point2D> used = new HashSet<Point2D>();

                int locCount = 0;

                // generate a set of pins
                for (int m = 0; m < 10000 && locCount < locs.Length && startPoints.Count - used.Count != 0; m++)
                {
                    Point2D start = startPoints[Utility.Random(startPoints.Count)];

                    // check for duplicate start
                    if (used.Contains(start))
                        continue;

                    used.Add(start);

                    Point2D[] pins = new Point2D[data.NumPins];

                    pins[0] = start;

                    int pinCount = 1;

                    // we got a starting pin - now generate the other pins
                    for (int n = 0; n < 100 && pinCount < pins.Length; n++)
                    {
                        Point2D prev = pins[pinCount - 1];

                        pins[pinCount] = new Point2D(
                            prev.X + (int)((Utility.RandomBool() ? +1 : -1) * Math.Sqrt(Utility.RandomDouble()) * (double)PinDistMax),
                            prev.Y + (int)((Utility.RandomBool() ? +1 : -1) * Math.Sqrt(Utility.RandomDouble()) * (double)PinDistMax));

                        if (Validate(level, pins, pinCount, distances))
                            pinCount++;
                    }

                    if (pinCount >= pins.Length)
                        locs[locCount++] = pins;
                }

                if (locCount < locs.Length)
                    Log("Failed generation for level {0}!", level);

                result[i] = locs;
            }

            Log("Writing locations to {0}...", BinFileName);

            using (Stream fs = File.Open(GetFilePath(BinFileName), FileMode.Create))
            {
                BinaryFileWriter writer = new BinaryFileWriter(fs, true);

                writer.Write((string)"seatreasurebin");

                writer.Write((int)0); // version

                writer.Write((int)result.Length);

                for (int i = 0; i < result.Length; i++)
                {
                    Point2D[][] locs = result[i];

                    writer.Write((int)locs.Length);

                    for (int j = 0; j < locs.Length; j++)
                    {
                        Point2D[] pins = locs[j];

                        writer.Write((int)pins.Length);

                        for (int k = 0; k < pins.Length; k++)
                            writer.Write((Point2D)pins[k]);
                    }
                }

                writer.Close();
            }

            Log("Writing locations to {0}...", TxtFileName);

            using (Stream fs = File.Open(GetFilePath(TxtFileName), FileMode.Create))
            using (StreamWriter writer = new StreamWriter(fs))
            {
                for (int i = 0; i < result.Length; i++)
                {
                    Point2D[][] locs = result[i];

                    for (int j = 0; j < locs.Length; j++)
                    {
                        Point2D[] pins = locs[j];

                        writer.Write("{0}", i + 1);

                        for (int k = 0; k < pins.Length; k++)
                            writer.Write("\t{0}", pins[k]);

                        writer.Write("\n");
                    }
                }
            }

            Log("Loading locations from {0}...", BinFileName);

            LoadMapPoints();

            tc.End();

            Log("Done! It took {0}.", tc.TimeTaken);
        }

        private static readonly Point2D[] m_SinglePin = new Point2D[1];

        public static bool Validate(int level, Point2D start, ushort[,] distances)
        {
            m_SinglePin[0] = start;

            return Validate(level, m_SinglePin, 0, distances);
        }

        public static bool Validate(int level, Point2D[] pins, ushort[,] distances)
        {
            // check no. pins
            if (pins.Length != GetLevelData(level).NumPins)
                return false;

            for (int pinIndex = 0; pinIndex < pins.Length; pinIndex++)
            {
                if (!Validate(level, pins, pinIndex, distances))
                    return false;
            }

            return true;
        }

        public static bool Validate(int level, Point2D[] pins, int pinIndex, ushort[,] distances)
        {
            Map map = Map;

            if (map == null || pinIndex < 0 || pinIndex >= pins.Length || pins.Length == 0)
                return false; // sanity

            Point2D curr = pins[pinIndex];

            // check world map bounds
            if (curr.X < 0 || curr.X >= Width || curr.Y < 0 || curr.Y >= Height)
                return false;

            // check boat wrap bounds
            Rectangle2D[] wrap = BaseBoat.GetWrapFor(new Point3D(curr, 0), map);
            if (wrap == null || !Utility.World.AreaContains(wrap, curr))
                return false;

            ushort dist = distances[curr.Y, curr.X];

            // check distance to nearest land
            if (dist < 6)
                return false;

            if (pinIndex == 0)
            {
                LevelData data = GetLevelData(level);

                // check leveled distance
                if (dist < data.DistMin || dist > data.DistMax)
                    return false;
            }
            else
            {
                // check distance to previous pin
                if (GetDistanceSquared(curr, pins[pinIndex - 1]) < PinDistMin * PinDistMin)
                    return false;

                // check for duplicate pin
                if (Array.IndexOf(pins, curr, 0, pinIndex) != -1)
                    return false;
            }

            // check map item bounds
            int mapDist = 2 * SeaChart.GetDist(level);
            if (!CartographersSextant.CalculateMapBounds(pins[0], mapDist, mapDist).Contains(curr))
                return false;

            // check special regions (e.g. angel island prison)
            if (HasSpecialRegion(map, curr))
                return false;

            // check can spawn
            if (!CanSpawnLandMobile(map, new Point3D(curr, WaterZ), CanFitFlags.requireSurface | CanFitFlags.canSwim))
                return false;

            return true;
        }

        public static ushort[,] CalculateDistanceMatrix()
        {
            // 42 MB matrix
            ushort[,] distances = GenerateWaterMap(Map, Width, Height, WaterZ);

            // fix bugged tile off the shore of Trinsic
            distances[2865, 2256] = 1;

            // fix bugged tiles along the eastern map edge
            distances[1588, 5119] = 1;
            distances[1589, 5119] = 1;
            distances[1590, 5119] = 1;
            distances[1591, 5119] = 1;
            distances[1592, 5119] = 1;
            distances[1593, 5119] = 1;

            DistanceMatrix.Fill(distances);

            return distances;
        }

        #region Water Map

        private static ushort[,] GenerateWaterMap(Map map, int width, int height, int waterZ)
        {
            if (map == null)
                return new ushort[0, 0];

            ushort[,] matrix = new ushort[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (IsWater(map, x, y, waterZ))
                        matrix[y, x] = 1;
                }
            }

            return matrix;
        }

        private static bool IsWater(Map map, int x, int y, int waterZ)
        {
            if (map == null)
                return false;

            LandTile landTile = map.Tiles.GetLandTile(x, y);

            if (landTile.Z == waterZ && ContainsID(m_WaterLandTiles, landTile.ID))
                return true;

            foreach (StaticTile statTile in map.Tiles.GetStaticTiles(x, y))
            {
                if (statTile.Z == waterZ && ContainsID(m_WaterStatTiles, statTile.ID))
                    return true;
            }

            return false;
        }

        private static readonly int[] m_WaterLandTiles = new int[]
            {
                0x00A8, 0x00AB,
                0x0136, 0x0137,
            };

        private static readonly int[] m_WaterStatTiles = new int[]
            {
                0x5797, 0x579C,
                0x746E, 0x7485,
                0x7490, 0x74AB,
                0x74B5, 0x75D5,
            };

        private static bool ContainsID(int[] array, int itemID)
        {
            for (int i = 0; i < array.Length - 1; i += 2)
            {
                if (itemID >= array[i] && itemID <= array[i + 1])
                    return true;
            }

            return false;
        }

        #endregion

        private static int GetDistanceSquared(Point2D p1, Point2D p2)
        {
            int dx = p2.X - p1.X;
            int dy = p2.Y - p1.Y;

            return dx * dx + dy * dy;
        }

        private static bool HasSpecialRegion(Map map, Point2D p)
        {
            ArrayList regions = Region.FindAll(new Point3D(p, 0), map);

            foreach (Region region in regions)
            {
                if (region.IsAngelIslandRules || region.IsDungeonRules)
                    return true;
            }

            return false;
        }

        private static string GetFilePath(string fileName)
        {
            return Path.Combine(Core.BaseDirectory, fileName);
        }

        private static void Log(string format, params object[] args)
        {
            Console.WriteLine("SeaTreasure [{0}]: {1}", DateTime.UtcNow.ToLongTimeString(), String.Format(format, args));
        }

        public struct LevelData
        {
            public static readonly LevelData Empty = new LevelData();

            private int m_DistMin;
            private int m_DistMax;
            private int m_NumPins;

            public int DistMin { get { return m_DistMin; } }
            public int DistMax { get { return m_DistMax; } }
            public int NumPins { get { return m_NumPins; } }

            public LevelData(int distMin, int distMax, int numPins)
            {
                m_DistMin = distMin;
                m_DistMax = distMax;
                m_NumPins = numPins;
            }
        }
    }
}