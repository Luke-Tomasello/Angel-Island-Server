/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Engines\Spawner\Spawner.SpawnPosition.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using Server.Items;
using Server.Multis;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Server.Utility;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        class Circle
        {
            public Vector2 Origin { get; set; }
            public float Radius { get; set; }
            public List<Vector2> Points { get; set; }
            public Circle(float radius, Vector2 origin)
            {
                Radius = radius;
                Origin = origin;
                Points = new List<Vector2>();

                for (int i = 0; i < 360; i++)
                {
                    float x = origin.X + radius * (float)Math.Cos(i * Math.PI / 180.0);
                    float y = origin.Y + radius * (float)Math.Sin(i * Math.PI / 180.0);
                    Points.Add(new Vector2(x, y));
                }
            }

            public int GetConcentricHomeRange()
            {   // 20% of the radius
                return Math.Max(4, (int)Utility.GetDistanceToSqrt(
                    new Point3D((int)Origin.X, (int)Origin.Y, 0),
                    new Point3D((int)Points[0].X, (int)Points[0].Y, 0)) / 4);
            }
            private Point3D Vector2ToPoint3D(Vector2 point)
            {
                return new Point3D((int)point.X, (int)point.Y, 0);
            }
            /// <summary>
            /// Select a RandomPoint within our spawn circle
            /// Results using Gravity 0, and Gravity 1
            /// Gravity set to 0
            /// 614 Objects nearby, 32.32%%
            /// 637 Objects medium distance, 33.53%
            /// 649 Objects far distance, 34.16%
            /// 1900 total objects spawned
            ///  ---
            /// Gravity set to 1
            /// 217 Objects nearby, 11.42%
            /// 646 Objects medium distance, 34.00%
            /// 1037 Objects far distance, 54.58%
            /// 1900 total objects spawned
            /// See Also: nuke SetSpawnerEdgeGravity and TestSpawnerClumping to understand the tuning
            /// </summary>
            /// <param name="spawn_far"></param>
            /// <param name="far_wiggle"></param>
            /// <returns>RandomPoint within our spawn circle</returns>
            public Point3D RandomPointAdjust(bool spawn_far, int far_wiggle)
            {
                // pick a random point around the circumference of our circle
                Vector2 edge = Points[Utility.Random(Points.Count)];
                if (!spawn_far)
                {
                    // Performs a linear interpolation between two vectors based on the given weighting (pull).
                    float pull = (float)Utility.RandomDouble();
                    // Apply gravity. See nuke SetSpawnerEdgeGravity and TestSpawnerClumping to understand the tuning (results in above comment)
                    for (int ix = 0; ix < EdgeGravity; ix++)
                    {   // trying to toss the mobiles farther away from the spawner
                        float temp = (float)Utility.RandomDouble();
                        if (temp > pull)
                            pull = temp;
                    }
                    // pull: A value between 0 and 1 that indicates the weight of the edge. (Edge Gravity)
                    Vector2 newPos = Vector2.Lerp(Origin, edge, pull);
                    return Vector2ToPoint3D(newPos);
                }
                else // (used rarely: Pirate Champ spawn, where the spawner is on the island, but we want all creatures out in the water.)
                {   // here all we are really doing is +/- wiggle
                    // Note: here we could have a gravity of say 100, but it's more expensive and doesn't
                    //  really buy us anything over just going directly to the edge.
                    int x = ((int)edge.X) + ((Utility.RandomList(new int[] { 0, 1 }) * 2 - 1) * far_wiggle);
                    int y = ((int)edge.Y) + ((Utility.RandomList(new int[] { 0, 1 }) * 2 - 1) * far_wiggle);
                    // if we're spawning far, just return the edge point +/- wiggle
                    return new Point3D(x, y, 0);
                }
            }
            // debug
            public void Draw()
            {
                for (int i = 0; i < Points.Count; i++)
                {
                    RecallRune rr = new RecallRune();
                    rr.MoveToWorld(new Point3D((int)Points[i].X, (int)Points[i].Y, 0), Map.Felucca);
                }
            }
        }
        public virtual Point3D GetSpawnPosition(object o)
        {
            return GetSpawnPosition(Map, Location, m_HomeRange, m_SpawnerFlags, o, ValidateSpawnPosition);
        }
        public virtual bool ValidateSpawnPosition(Point3D loc, Map map)
        {
            return true;
        }

        public delegate bool LocationValidator(Point3D loc, Map map);
        public static Point3D GetSpawnPosition(Map map, Point3D location, int homeRange, SpawnFlags sflags, object? o, LocationValidator? validator = null)
        {
            #region Flags
            bool allowChangeZ = (sflags & SpawnFlags.AllowChangeZ) != 0;
            bool forceZ = (sflags & SpawnFlags.ForceZ) != 0;
            bool avoidPlayers = (sflags & SpawnFlags.AvoidPlayers) != 0;
            bool boat = (sflags & SpawnFlags.Boat) != 0;
            bool clearPath = (sflags & SpawnFlags.ClearPath) != 0;
            bool noBlock = (sflags & SpawnFlags.NoBlock) != 0;
            #endregion Flags
            Utility.CanFitFlags flags = Utility.CanFitFlags.requireSurface;
            bool waterMob = false;
            bool waterOnlyMob = false;
            if (o is Mobile m)
            {
                if (m != null && m.CanSwim == true) flags |= Utility.CanFitFlags.canSwim;
                if (m != null && m.CantWalkLand == true) flags |= Utility.CanFitFlags.cantWalk;
                waterMob = m.CanSwim;
                waterOnlyMob = m.CanSwim && m.CantWalkLand;
            }
            else if (o == null)
            {   // assume spawn anywhere
                waterMob = true;
            }

            if (map == null)
                return location;

            /* get a reasonable set of points */
            List<Point3D> p3Dlist = new();
            for (int jx = 0; jx < SpawnPointTries; jx++)
            {
                Point3D point = GetPoint(map, location, homeRange, sflags);
                List<int> HardZs = new(Utility.GetHardZs(map, point));
                List<int> UseableZs = new(HardZs);
                if (allowChangeZ)
                {   // allow spawning on different floors/levels. For instance, inside bank, and on the roof
                    Utility.Shuffle(UseableZs);
                }
                else if (forceZ)
                {   // needs to be on the same z as the spawner
                    UseableZs = new List<int>() { location.Z };
                }
                else
                {   // we prefer the level on which the spawner is located, otherwise, average z
                    //  We also include nearby water tiles
                    UseableZs = PreferredZ(map, location.Z, point, HardZs);
                }

                // sometimes, the map lays water tiles over gullies/ditches in dungeons (lakes). Avoid these for land-only mobiles
                //  Example (5228, 911, -45) / (5228, 911, -65) (Destard)
                if (HasWater(map, point, HardZs))
                    if (!waterMob)
                        continue;
                    else
                        // this prevents watermobs from spawning on the landtile under the water
                        RemoveNonWater(map, point, UseableZs);

                foreach (int z in UseableZs)
                {
                    point.Z = z;

                    if (waterOnlyMob && !Utility.IsValidWater(map, point.X, point.Y, z))
                        continue;

                    if (validator != null && !validator(point, map))
                        continue;

                    if (waterMob || waterOnlyMob)
                    {
                        if (Utility.CanSpawnWaterMobile(map, point))
                            if (!p3Dlist.Contains(point))
                                p3Dlist.Add(new Point3D(point.X, point.Y, z));
                    }

                    if (!waterOnlyMob && Utility.CanSpawnLandMobile(map, point, flags))
                        if (!p3Dlist.Contains(point))
                            p3Dlist.Add(new Point3D(point.X, point.Y, z));
                }
            }
            Utility.Shuffle(p3Dlist);

            List<Point3D> nearPlayer = new();

            // process point rules
            for (int ix = 0; ix < p3Dlist.Count; ix++)
            {
                Point3D point = p3Dlist[ix];    // pull from the possibly ordered list

                // don't allow spawn from Shame to spill over into Wind for example.
                if (ChangingDungeons(starting_location: location, proposed_location: point))
                    continue;

                if (avoidPlayers && Utility.NearPlayer(map, new Point3D(point.X, point.Y, location.Z), 15))
                {   // don't fail here, instead add to a list and select the best one later
                    if (point != location)
                        nearPlayer.Add(point);
                    continue;
                }
                if (boat && BaseBoat.FindBoatAt(new Point2D(point.X, point.Y), map) == null)
                    continue;
                if (clearPath && ClearPathLand(location, point, map: map) == false)
                    continue;
                if (noBlock && Blocked(location, point, map: map) == true)
                    continue;

                if (waterMob)
                {   // water mobs, just get the point
                    if (Utility.CanSpawnWaterMobile(map, point))
                        return point;
                }
                if (!waterOnlyMob)
                {   // land/water mobs get CanSpawnLandMobile
                    if (Utility.CanSpawnLandMobile(map, point, flags))
                        return point;
                }
            }

            // okay, don't have a perfect point, use one from our nearPlayer list if we have one.
            if (nearPlayer.Count > 0)
            {
                if ((sflags & SpawnFlags.SpawnFar) != 0)
                {
                    nearPlayer.Sort((x1, x2) =>
                    {
                        return Utility.GetDistanceToSqrt(location, x2).CompareTo(Utility.GetDistanceToSqrt(location, x1));
                    });

                    //foreach (var vp in nearPlayer)
                    //Console.WriteLine("{0}", Utility.GetDistanceToSqrt(location, vp));
                    //Console.WriteLine("--------------------------");

                    return nearPlayer[0];
                }
                else
                    return nearPlayer[Utility.Random(nearPlayer.Count)];
            }

            // give up and return the starting location
            SpawnPointFails++;
            return location;
        }
        private static bool HasWater(Map map, Point3D point, List<int> Zs)
        {
            foreach (int z in Zs)
                if (Utility.IsWater(map, point.X, point.Y, z) && Utility.FindOneItemAt(new Point3D(point.X, point.Y, z), map, typeof(Item), 2, false) == null)
                    return true;

            return false;
        }
        private static void RemoveNonWater(Map map, Point3D point, List<int> Zs)
        {
            List<int> toRemove = new List<int>();
            foreach (int z in Zs)
                if (!Utility.IsWater(map, point.X, point.Y, z))
                    toRemove.Add(z);

            foreach (int z in toRemove)
                Zs.Remove(z);

            return;
        }
        private static bool ChangingDungeons(Point3D starting_location, Point3D proposed_location)
        {
            Region r1 = Region.Find(starting_location, Map.Felucca);
            Region r2 = Region.Find(proposed_location, Map.Felucca);
            if (r1 != null && r1.IsDungeonRules && r1 != r2)
                return true;
            return false;
        }
        private static List<int> PreferredZ(Map map, int baseZ, Point3D point, List<int> Zs)
        {
            List<int> list = new();
            if (Zs.Contains(baseZ)) // some GMs put spawners in trees and on rocks. Don't add these
                list.Add(baseZ);

            // get this reasonable z
            int avz = map.GetAverageZ(point.X, point.Y);
            if (!list.Contains(avz))
                list.Add(avz);

            // add in water tiles, usually -5 relative to nearby land at 0
            foreach (int z in Zs)
                if (!list.Contains(z))
                    if (Utility.IsWater(map, point.X, point.Y, z))
                        list.Add(z);

            return list;
        }
        #region SpawnPointTries and analysis 
        /* SpawnPointTries is heuristically derived.
         * The commands below returned the following values.
         * tries 10
         * fails 440
         * tries 100
         * fails 263
         * tries 50
         * fails 261
         * tries 25
         * fails 272
         * tries 15
         * fails 309
         * Because of these results, it looks like 25 (re)tries results in a reasonable
         *  cost benefit payoff
         */
        public static int SpawnPointFails;
        public static int SpawnPointTries = 25;
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldLoad += new WorldLoadEventHandler(Check);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
        }
        public static void Initialize()
        {
            // world building
            CommandSystem.Register("AdjustSpawn", AccessLevel.Administrator, new CommandEventHandler(AdjustSpawn_OnCommand));

            // diagnostics
            CommandSystem.Register("SpawnPointFails", AccessLevel.Owner, new CommandEventHandler(SpawnPointFails_OnCommand));
            CommandSystem.Register("SpawnPointTries", AccessLevel.Owner, new CommandEventHandler(SpawnPointTries_OnCommand));
        }
        public static void SpawnPointTries_OnCommand(CommandEventArgs e)
        {
            try
            {
                SpawnPointTries = int.Parse(e.ArgString);
            }
            catch
            {
                e.Mobile.SendMessage("Usage: SpawnPointTries <number of spawner tries>");
            }
        }
        public static void SpawnPointFails_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("SpawnPointFails: {0}", SpawnPointFails);
            SpawnPointFails = 0;
        }
        #endregion
        public static Point3D GetPoint(Map map, Point3D location, int homeRange, SpawnFlags sflags)
        {
            int x, y, z;
            if ((sflags & SpawnFlags.Concentric) != 0)
            {
                Circle c = new Circle(homeRange, new Vector2(location.X, location.Y));
                if ((sflags & SpawnFlags.SpawnFar) == 0)
                {
                    Point3D rpa = c.RandomPointAdjust(spawn_far: false, far_wiggle: 0);
                    x = rpa.X;
                    y = rpa.Y;
                }
                else
                {   // need to handle 'far' spawns ('5' is just heuristically derived.)
                    Point3D rpa = c.RandomPointAdjust(spawn_far: true, far_wiggle: 5);
                    x = rpa.X;
                    y = rpa.Y;
                }
            }
            else
            {
                if ((sflags & SpawnFlags.SpawnFar) != 0)
                {
                    x = (int)((double)location.X + Spawner.RandomFar() * (double)homeRange);
                    y = (int)((double)location.Y + Spawner.RandomFar() * (double)homeRange);
                }
                else
                {
                    x = location.X + (Utility.Random((homeRange * 2) + 1) - homeRange);
                    y = location.Y + (Utility.Random((homeRange * 2) + 1) - homeRange);
                }
            }

            z = map.GetAverageZ(x, y);

            return new Point3D(x, y, z);
        }
        const int BlockDistance = 10;
        public static bool Blocked(Point3D start, Point3D dest, Map map)
        {
            if (ClearPathLand(dest, new Point3D(dest.X + BlockDistance, dest.Y, dest.Z), map: map))                   // right
                return false;
            if (ClearPathLand(dest, new Point3D(dest.X - BlockDistance, dest.Y, dest.Z), map: map))                   // left
                return false;
            if (ClearPathLand(dest, new Point3D(dest.X - BlockDistance, dest.Y - BlockDistance, dest.Z), map: map))   // upper left
                return false;
            if (ClearPathLand(dest, new Point3D(dest.X + BlockDistance, dest.Y - BlockDistance, dest.Z), map: map))   // upper right
                return false;
            if (ClearPathLand(dest, new Point3D(dest.X, dest.Y - BlockDistance, dest.Z), map: map))                   // top
                return false;
            if (ClearPathLand(dest, new Point3D(dest.X, dest.Y + BlockDistance, dest.Z), map: map))                   // bottom
                return false;
            if (ClearPathLand(dest, new Point3D(dest.X - BlockDistance, dest.Y + BlockDistance, dest.Z), map: map))   // lower left
                return false;
            if (ClearPathLand(dest, new Point3D(dest.X + BlockDistance, dest.Y + BlockDistance, dest.Z), map: map))   // lower right
                return false;
            return true;
        }
        public static double RandomFar()
        {
            if (Utility.RandomBool())
                return -Math.Sqrt(Utility.RandomDouble());
            else
                return Math.Sqrt(Utility.RandomDouble());

            // if you want a steeper congregation toward the outside, replace Math.Sqrt(...) with Math.Pow(Utility.RandomDouble(), .33);
            // Pow(x, .33) is cube root, .25 is 4th root etc, etc
            // basically the higher the root, the sharper the dropoff toward center will be
        }
    }
}
