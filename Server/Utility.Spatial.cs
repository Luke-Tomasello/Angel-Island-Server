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


/* Server/Utility.Spatial.cs
 * ChangeLog:
 *  10/14/2026, Adam
 *      GetSurfaceTop (BadZ, GoodZ) no longer counts a multi (house, boat, tent) as an item on the tile. On a house's
 *      anchor tile the house itself was taken for an item at the house's Z, so Help Stuck's Z correction dropped a
 *      player standing on the floor into it (e.g. (1337,1964,7) -> (1337,1964,0)). Help Stuck was the only caller,
 *      and has since dropped Z correction, so BadZ, GoodZ and CheatingZ have no callers now.
 *  10/6/2026
 *      Moved from Utility.cs as part of a structural cleanup; no behavior change.
 *      Map fit/spawn tests, Z handling, paths, area operations and named world areas.
 */

using Server.Items;
using Server.Mobiles;
using Server.Spells;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static Server.Commands.Decorate;
namespace Server
{
    public partial class Utility
    {
        #region Map Fit, Spawning, Z & Paths
        #region Geometry
        public static Rectangle2D GetBounds(IEnumerable<Point2D> points)
        {
            bool any = false;

            int minX = 0;
            int minY = 0;
            int maxX = 0;
            int maxY = 0;

            foreach (Point2D p in points)
            {
                if (!any)
                {
                    minX = maxX = p.X;
                    minY = maxY = p.Y;
                    any = true;
                }
                else
                {
                    if (p.X < minX) minX = p.X;
                    else if (p.X > maxX) maxX = p.X;

                    if (p.Y < minY) minY = p.Y;
                    else if (p.Y > maxY) maxY = p.Y;
                }
            }

            if (!any)
                return Rectangle2D.Empty;

            return new Rectangle2D(
                minX,
                minY,
                (maxX - minX) + 1,
                (maxY - minY) + 1
            );
        }
        public static string GetSextantLocation(Mobile m)
        {
            // Resolve location
            string location = "????";
            Map map = m.Map;

            if (map != null && map != Map.Internal)
            {
                int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
                bool xEast = false, ySouth = false;

                bool valid = Sextant.Format(
                    m.Location,
                    map,
                    ref xLong,
                    ref yLat,
                    ref xMins,
                    ref yMins,
                    ref xEast,
                    ref ySouth);

                if (valid)
                    location = Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
                else
                    location = string.Format("{0} {1}", m.X, m.Y);
            }

            return location;
        }
        public static double GetDistanceToSqrt(Point3D p1, Point3D p2)
        {
            int xDelta = p1.m_X - p2.m_X;
            int yDelta = p1.m_Y - p2.m_Y;

            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }
        public static double GetDistanceToSqrt(Point2D p1, Point2D p2)
        {
            int xDelta = p1.m_X - p2.X;
            int yDelta = p1.m_Y - p2.Y;

            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }
        #endregion Geometry
        #region Z Management
        public static bool BadZ(Mobile from)
        {   // am I maybe stuck in the floor?
            IPoint3D px = from.Location;
            Utility.GetSurfaceTop(from, ref px);
            return from.Location.Z != px.Z;
        }
        public static int GoodZ(Mobile from)
        {   // get the best Z
            IPoint3D px = from.Location;
            Utility.GetSurfaceTop(from, ref px);
            return px.Z;
        }
        public static bool CheatingZ(Mobile from)
        {   // if they are surrounded by movable objects, crates for example, assume a cheat
            IPooledEnumerable eable = from.Map.GetItemsInRange(from.Location, 1);
            foreach (Item item in eable)
                if (item != null && item.Deleted == false)
                    if (item.Movable && item.ItemData.Impassable && (item.Z + item.ItemData.Height) > from.Z && (from.Z + 16) > item.Z)
                        if (from.InRange(item.GetWorldLocation(), 1))
                        {
                            eable.Free();
                            return true;
                        }

            eable.Free();

            return false;
        }
        public static void GetSurfaceTop(Mobile from, ref IPoint3D p)
        {   // rise to the top of the stack. Usually GM created structures like stairs
            List<IPoint3D> list = new();
            IPooledEnumerable eable = from.Map.GetItemsInRange((Point3D)p, 0);
            foreach (Item thing in eable)
                // not a multi (house, boat, tent): it is an item at its origin, but its ItemData is not its geometry,
                //  so its 'top' is meaningless. Map.CanFit and movement skip it the same way.
                if (thing != null && thing.Deleted == false && thing is not BaseMulti)
                {
                    IPoint3D ipx = thing as IPoint3D;
                    SpellHelper.GetSurfaceTop(ref ipx);
                    list.Add(ipx);
                }

            eable.Free();

            if (list.Count > 0)
            {
                list.Sort((p, q) => q.Z.CompareTo(p.Z));
                p = list[0];
            }

            return;
        }
        #endregion Z Management
        #region Pathing
        private static List<Direction> dirs = new()
        {
                Direction.North,
                Direction.Right,
                Direction.East,
                Direction.Down,
                Direction.South,
                Direction.Left,
                Direction.West,
                Direction.Up
        };
        public static Point3D OffsetPoint(Point3D oldLocation, Direction d)
        {
            int x = oldLocation.m_X, y = oldLocation.m_Y;
            switch (d & Direction.Mask)
            {
                case Direction.North:
                    --y;
                    break;
                case Direction.Right:
                    ++x;
                    --y;
                    break;
                case Direction.East:
                    ++x;
                    break;
                case Direction.Down:
                    ++x;
                    ++y;
                    break;
                case Direction.South:
                    ++y;
                    break;
                case Direction.Left:
                    --x;
                    ++y;
                    break;
                case Direction.West:
                    --x;
                    break;
                case Direction.Up:
                    --x;
                    --y;
                    break;
            }

            return new Point3D(x, y, oldLocation.m_Z);
        }
        public static List<Point3D> SimplePath(Map map, Point3D startLocation, Point3D goal, Direction d)
        {
            List<Point3D> list = new();
            Point3D newLocation = startLocation;
            do
            {
                list.Add(newLocation = OffsetPoint(new Point3D(newLocation.X, newLocation.Y, map.GetAverageZ(newLocation.X, newLocation.Y)), d));
            } while (newLocation.X != goal.X || newLocation.Y != goal.Y);

            return list;
        }
        public class MobileInfo
        {
            public Mobile target;
            public bool unavailable = false;

            public bool dead = false;
            public bool fled = false;
            public bool gone = false;
            public bool hidden = false;
            public bool path = false;
            public bool range = false;

            public double distance = 0.0;
            public bool in_range = false;   // range
            public bool available = false;  // !unavailable
            public bool in_los = false;     // we don't factor in LOS here as the different AIs handle it differently
            public bool can_see = false;    // can we see the target?
            public bool alive = false;      // !dead
            public Mobile? attacker = null;  // target
            public Mobile? defender = null;  // target
            public MobileInfo(Mobile c)
            {
                target = c;
            }
        }
        public static MobileInfo GetMobileInfo(Mobile source, PathFollower? path, MobileInfo info)
        {
            if (info.target != null)
            {   // we don't factor in 'in LOS' here as the different AIs handle it differently
                info.in_los = source.InLOS(info.target);
                info.can_see = source.CanSee(info.target);
                info.gone = info.target.Deleted || !source.CanBeHarmful(info.target, false) || info.target.Map != source.Map;
                info.dead = !info.target.Alive || info.target.IsDeadBondedPet || info.target is BaseCreature bc && bc.IsDeadPet;
                info.hidden = !info.gone && !info.dead && !info.can_see || info.target.Hidden;
                info.path = !info.gone && !info.dead && path != null;
                info.range = (source is BaseCreature) ? source.InRange(info.target, (source as BaseCreature).RangePerception) : info.can_see && info.in_los;
                info.fled = !info.dead && !info.gone && !info.hidden && !info.range;

                if (info.gone || info.dead || info.hidden || info.fled)
                    info.unavailable = true;

                info.distance = source.GetDistanceToSqrt(info.target);
            }
            else info.unavailable = true;

            // semantic convenience maps
            info.available = !info.unavailable;
            info.in_range = info.range;
            info.alive = !info.dead;
            info.attacker = info.defender = info.target;
            return info;
        }

        public static bool GetPathArray(Map map, Point3D start, Point3D _goal, ref Direction[] directions, ref Point3D[] points)
        {
            // can we get there from here?
            Movement.MovementObject obj_start = new Movement.MovementObject(start, _goal, null);
            IPoint3D goal = obj_start.Goal;

            // get the target surface
            Spells.SpellHelper.GetSurfaceTop(ref goal);
            obj_start.Goal = new Point3D(goal.X, goal.Y, goal.Z);

            // can we get there?
            MovementPath path = new MovementPath(obj_start);
            if (path != null && path.Success)
            {
                Stack<Point3D> stack = new();
                stack.Push(start);

                foreach (Direction d in path.Directions)
                {
                    Point3D pxn = stack.Peek();
                    switch (d & Direction.Mask)
                    {
                        case Direction.North:
                            --pxn.Y;
                            break;
                        case Direction.Right:
                            ++pxn.X;
                            --pxn.Y;
                            break;
                        case Direction.East:
                            ++pxn.X;
                            break;
                        case Direction.Down:
                            ++pxn.X;
                            ++pxn.Y;
                            break;
                        case Direction.South:
                            ++pxn.Y;
                            break;
                        case Direction.Left:
                            --pxn.X;
                            ++pxn.Y;
                            break;
                        case Direction.West:
                            --pxn.X;
                            break;
                        case Direction.Up:
                            --pxn.X;
                            --pxn.Y;
                            break;
                    }

                    int newZ = 0;
                    if (map.GetTopSurface(new Point3D(pxn.X, pxn.Y, map.GetAverageZ(pxn.X, pxn.Y)), out newZ) != null)
                        stack.Push(new Point3D(new Point3D(pxn.X, pxn.Y, newZ)));
                    else
                        ; // error
                }

                directions = path.Directions.ToArray();
                points = stack.Reverse<Point3D>().ToArray();
                return true;
            }
            else
                return false;
        }
        #endregion Pathing
        #region Nearby Points
        private static bool NearDoor(Map map, Point3D[] locs)
        {
            foreach (var point in locs)
                foreach (Item item in map.GetItemsInRange(point, 2))
                    if (item is BaseDoor)
                        return true;
            return false;
        }
        private static bool NearDoor(Map map, Point3D point)
        {
            foreach (Item item in map.GetItemsInRange(point, 2))
                if (item is BaseDoor)
                    return true;
            return false;
        }
        public static Point3D GetPointNearby(Map map, Point3D loc, int max_dist, bool avoid_doors = true, Point3D[]? avoid = null)
        {
            if (avoid_doors == false)
                return GetPointNearby(map, loc, max_dist, avoid);
            else
            {
                for (int ix = 0; ix < 100; ix++)
                {
                    Point3D px = GetPointNearby(map, loc, max_dist, avoid);
                    if (!NearDoor(map, px))
                        return px;
                }

                return Point3D.Zero;
            }
        }
        private static Point3D GetPointNearby(Map map, Point3D loc, int max_dist, Point3D[]? avoid = null)
        {
            Point3D px = Point3D.Zero;
            Point3D new_loc = Point3D.Zero;
            Keg temp = new Keg();
            try
            {
                for (int check_loc = 1; check_loc <= max_dist; check_loc++)
                {
                    if (avoid != null)
                        for (int ix = 0; ix < avoid.Length; ix++)
                        {
                            px = Spawner.GetSpawnPosition(map, loc, homeRange: check_loc, SpawnFlags.ForceZ, temp);
                            if (avoid.Contains(px))
                                continue;
                            else break;
                        }
                    else
                        px = Spawner.GetSpawnPosition(map, loc, homeRange: check_loc, SpawnFlags.ForceZ, temp);
                    if (px != loc)
                        return px;
                }

                return new_loc;
            }
            catch { }
            finally
            {
                if (temp != null)
                    temp.Delete();
            }

            return new_loc;
        }
        #endregion Nearby Points
        #region RunUO 2.6 Map Port
        public static int GetObjectZ(object o)
        {
            if (o is LandTile)
                return ((LandTile)o).Z;

            if (o is StaticTile)
                return ((StaticTile)o).Z;

            if (o is Item)
                return ((Item)o).Z;

            return 0;
        }
        public static int GetObjectID(object o)
        {
            if (o is LandTile)
                return ((LandTile)o).ID;

            if (o is StaticTile)
                return ((StaticTile)o).ID;

            if (o is Item)
                return ((Item)o).ItemID;

            return 0;
        }
        [Flags]
        public enum CanFitFlags
        {
            none = 0x0000,
            checkBlocksFit = 0x0001,
            checkMobiles = 0x0002,
            requireSurface = 0x0004,
            ignoreDeadMobiles = 0x0008,
            canSwim = 0x0010,
            cantWalk = 0x0020,
        }
        const CanFitFlags FitFlagsDefault = CanFitFlags.checkMobiles | CanFitFlags.requireSurface;
        public static bool IsValidWater(Map map, int x, int y, int z)
        {
            Region region;
            if (map == null || (region = Region.Find(new Point3D(x, y, z), map)) == null)
                return false;

            if (!region.AllowSpawn() || !map.CanFit(x, y, z, 16, false, true, false))
                return false;

            return IsWater(map, x, y, z);
        }
        public static bool IsWater(Map map, int x, int y, int z)
        {
            LandTile landTile = map.Tiles.GetLandTile(x, y);

            if (landTile.Z == z && (TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0)
                return true;

            StaticTile[] staticTiles = map.Tiles.GetStaticTiles(x, y, true);

            for (int i = 0; i < staticTiles.Length; ++i)
            {
                StaticTile staticTile = staticTiles[i];

                if (staticTile.Z == z && (TileData.ItemTable[staticTile.ID & TileData.MaxItemValue].Flags & TileFlag.Wet) != 0)
                    return true;
            }

            return false;
        }
        public static bool CanSpawnWaterMobile(Map map, Point3D p)
        {
            return CanSpawnWaterMobile(map, p, CanFitFlags.none);
        }
        public static bool CanSpawnWaterMobile(Map map, Point3D p, CanFitFlags flags)
        {   // currently we do no processing of flags
            return IsValidWater(map, p.X, p.Y, p.Z);
        }
        public static bool CanSpawnLandMobile(Map map, Point3D p)
        {
            if (map == null || map == Map.Internal) return false;
            return CanSpawnLandMobile(map, p.X, p.Y, p.Z, FitFlagsDefault);
        }
        public static bool CanSpawnLandMobile(Map map, Point3D p, CanFitFlags flags)
        {
            if (map == null || map == Map.Internal) return false;
            return CanSpawnLandMobile(map, p.X, p.Y, p.Z, flags);
        }
        public static bool CanSpawnLandMobile(Map map, Point2D p, int z)
        {
            if (map == null || map == Map.Internal) return false;
            return CanSpawnLandMobile(map, p.X, p.Y, z, FitFlagsDefault);
        }
        public static bool CanSpawnLandMobile(Map map, Point2D p, int z, CanFitFlags flags)
        {
            if (map == null || map == Map.Internal) return false;
            return CanSpawnLandMobile(map, p.X, p.Y, z, flags);
        }
        public static bool CanSpawnLandMobile(Map? map, int x, int y, int z)
        {
            if (map == null || map == Map.Internal) return false;
            return CanSpawnLandMobile(map, new Point3D(x, y, z), FitFlagsDefault);
        }
        public static bool CanSpawnLandMobile(Map map, int x, int y, int z, CanFitFlags flags)
        {
            if (map == null || map == Map.Internal) return false;
            if (!Region.Find(new Point3D(x, y, z), map).AllowSpawn())
                return false;

            return CanFit(map, x, y, z, 16, flags);
        }

        public static bool CanFit(Map map, Point2D p, int z, int height)
        {
            if (map == null || map == Map.Internal) return false;
            return CanFit(map, p.X, p.Y, z, height, FitFlagsDefault);
        }
        public static bool CanFit(Map map, Point3D p, int height)
        {
            if (map == null || map == Map.Internal) return false;
            return CanFit(map, p.X, p.Y, p.Z, height, FitFlagsDefault);
        }
        public static bool CanFit(Map map, Point2D p, int z, int height, CanFitFlags flags)
        {
            if (map == null || map == Map.Internal) return false;
            return CanFit(map, p.X, p.Y, z, height, flags);
        }
        public static bool CanFit(Map map, Point3D p, int height, CanFitFlags flags)
        {
            if (map == null || map == Map.Internal) return false;
            return CanFit(map, p.X, p.Y, p.Z, height, flags);
        }
        public bool CanFit(Map map, int x, int y, int z, int height)
        {
            if (map == null || map == Map.Internal) return false;
            return CanFit(map, x, y, z, height, FitFlagsDefault);
        }
        public static bool CanFit(Map map, int x, int y, int z, int height, CanFitFlags flags)
        {
            if (map == null || map == Map.Internal) return false;
            bool checkBlocksFit = (flags & CanFitFlags.checkBlocksFit) != 0;
            bool checkMobiles = (flags & CanFitFlags.checkMobiles) != 0;
            bool requireSurface = (flags & CanFitFlags.requireSurface) != 0;
            bool ignoreDeadMobiles = (flags & CanFitFlags.ignoreDeadMobiles) != 0;
            bool canSwim = (flags & CanFitFlags.canSwim) != 0;
            bool cantWalk = (flags & CanFitFlags.cantWalk) != 0;

            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                return false;

            bool hasSurface = false;

            LandTile lt = map.Tiles.GetLandTile(x, y);
            int lowZ = 0, avgZ = 0, topZ = 0;

            map.GetAverageZ(x, y, ref lowZ, ref avgZ, ref topZ);
            TileFlag landFlags = TileData.LandTable[lt.ID & 0x3FFF].Flags;

            if ((landFlags & TileFlag.Impassable) != 0 && avgZ > z && (z + height) > lowZ)
                return false;
            // can walk on land
            else if (!cantWalk && (landFlags & TileFlag.Impassable) == 0 && z == avgZ && !lt.Ignored)
                hasSurface = true;
            // can swim in water
            else if (canSwim && (landFlags & TileFlag.Impassable) != 0 && (landFlags & TileFlag.Wet) != 0 && z == avgZ && !lt.Ignored)
                hasSurface = true;

            StaticTile[] staticTiles = map.Tiles.GetStaticTiles(x, y, true);

            bool surface, impassable;

            for (int i = 0; i < staticTiles.Length; ++i)
            {
                ItemData id = TileData.ItemTable[staticTiles[i].ID & 0x3FFF];
                surface = id.Surface;
                impassable = id.Impassable;

                if ((surface || impassable) && (staticTiles[i].Z + id.CalcHeight) > z && (z + height) > staticTiles[i].Z)
                    return false;
                else if (surface && !impassable && z == (staticTiles[i].Z + id.CalcHeight))
                    hasSurface = true;
            }

            Sector sector = map.GetSector(x, y);
            //Dictionary<Server.Serial, object>.ValueCollection items = sector.Items.Values, mobs = sector.Mobiles.Values;

            foreach (Item item in sector.Items)
            {
                if (item == null)
                    continue;

                if (item.ItemID < 0x4000 && item.AtWorldPoint(x, y))
                {
                    ItemData id = item.ItemData;
                    surface = id.Surface;
                    impassable = id.Impassable;

                    if ((surface || impassable || (checkBlocksFit && item.BlocksFit)) && (item.Z + id.CalcHeight) > z && (z + height) > item.Z)
                        return false;
                    else if (surface && !impassable && z == (item.Z + id.CalcHeight))
                        hasSurface = true;
                }
            }

            if (checkMobiles)
            {
                foreach (Mobile m in sector.Mobiles)
                {
                    if (m == null)
                        continue;

                    if (m.Alive == true || ignoreDeadMobiles == false)
                    {
                        if (m.Location.m_X == x && m.Location.m_Y == y)
                        {
                            if ((m.Z + 16) > z && (z + height) > m.Z)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return !requireSurface || hasSurface;
        }
        public static bool LineOfSight(Map map, object from, object dest, bool strict)
        {
            if (from == dest || ((from is Mobile && ((Mobile)from).AccessLevel > AccessLevel.Player) && strict == false))
                return true;

            else if (dest is Item && from is Mobile && ((Item)dest).RootParent == from)
                return true;

            return map.LineOfSight(map.GetPoint(from, true), map.GetPoint(dest, false));
        }
        #endregion RunUO 2.6 Map Port
        #region WipeRect & Z Breaks
        [Flags]
        public enum WipeRectFlags
        {
            None = 0x00,
            Items = 0x01,
            Mobiles = 0x02,
            All = Items | Mobiles,
        }
        [Usage("WipeRect")]
        [Description("Wipes the rectangle of items and/or Mobiles.")]
        public static int WipeRect(Map map, Rectangle2D rect,
            int startingZ = 0, WipeRectFlags flags = WipeRectFlags.All,
            Type[]? excludeByType = null, int[]? excludeByID = null, int lenientZ = int.MinValue, string? logFile = null)
        {
            if (lenientZ == int.MinValue)
                lenientZ = CalcZSlop(map, rect, startingZ, flags);

            IPooledEnumerable eable = map.GetObjectsInBounds(rect);
            List<object> list = new();
            foreach (object ix in eable)
            {   // list of objects to delete
                if ((flags & WipeRectFlags.Items) != 0)
                {
                    if (ix is Item item)
                        if (item != null && !item.Deleted && rect.Contains(item))
                            if (!ExcludeThisType(item, excludeByType) && !ExcludeThisID(item, excludeByID))
                                if (Math.Abs(item.Z - startingZ) <= lenientZ)
                                    list.Add(item);
                                else
                                    ; // debug break
                }
                else if ((flags & WipeRectFlags.Mobiles) != 0)
                {
                    if (ix is Mobile mob)
                        if (mob != null && !mob.Deleted && rect.Contains(mob))
                            if (!ExcludeThisType(mob, excludeByType) && !ExcludeThisID(mob, excludeByID))
                                if (Math.Abs(mob.Z - startingZ) <= lenientZ)
                                    list.Add(mob);
                }
            }
            eable.Free();

            foreach (object ix in list)
            {   // log in our standard cfg format
                WriteObjectCfg(ix, logFile, false);

                if (ix is Item item)
                    item.Delete();
                else if (ix is Mobile mob)
                    mob.Delete();
            }

            return list.Count;
        }
        public static int CalcZSlop(Map map, Rectangle2D rect, int startingZ, WipeRectFlags flags = WipeRectFlags.All)
        {   // see how many Zs we can include before changing 'floors'

            int floor = startingZ;
            int ceiling = floor;
            IPooledEnumerable eable = map.GetObjectsInBounds(rect);
            List<IEntity> list = new();
            foreach (object o in eable) { list.Add((IEntity)o); }
            eable.Free();
            foreach (IEntity o in list)
            {
                if (o is Mobile && (flags & WipeRectFlags.Mobiles) == 0)
                    continue;
                if (o is Item && (flags & WipeRectFlags.Items) == 0)
                    continue;

                int hc = HardCeiling(map, o.X, o.Y, startingZ);
                int hf = HardFloor(map, o.X, o.Y, startingZ);

                if (o.Z < hc)
                    if (o.Z > ceiling)
                        ceiling = o.Z;

                if (o.Z >= hf)
                    if (o.Z <= floor)
                        floor = o.Z;
            }

            return Math.Abs(floor - ceiling);
        }
        public static int HardCeiling(Map map, int x, int y, int z)
        {
            List<int> hardZBreaks = HardZBreaks(map, x, y, z);
            ;
            ;
            int stop = z;
            foreach (int ctr in hardZBreaks)
            {
                if (ctr > z)
                    return ctr;
            }
            ;
            // there is no ceiling here, so we are inclusive of this z (an all above)
            //return stop >= 0 ? ++stop : --stop;
            return stop >= 0 ? short.MaxValue : short.MinValue;
            //return stop + 1;
        }
        public static int HardFloor(Map map, int x, int y, int z)
        {
            List<int> hardZBreaks = HardZBreaks(map, x, y, z);
            ;
            ;
            int stop = z;
            foreach (int ctr in hardZBreaks)
            {
                if (ctr <= z)
                    return ctr;
            }
            ;
            // there is no floor here, so we are inclusive of this z
            return stop;
        }
        public static List<int> HardZBreaks(Map map, int x, int y, int z)
        {
            List<int> list = new();
            StaticTile[] staticTiles = map.Tiles.GetStaticTiles(x, y, true);
            foreach (StaticTile st in staticTiles) { list.Add(st.Z); }
            list.Add(map.Tiles.GetLandTile(x, y).Z);
            list.Sort();
            return list;
        }
        public static int GetAverageZ(Map map, int x, int y)
        {
            int landZ = 0, landAvg = 0, landTop = 0;
            map.GetAverageZ(x, y, ref landZ, ref landAvg, ref landTop);
            return landAvg;
        }
        private static bool ExcludeThisID(object obj, int[]? exclude)
        {
            if (obj != null && exclude != null)
            {
                if (obj is Mobile mob)
                {
                    Monitor.ErrorOut(string.Format("Warning: Unsure what the 'ID' of a mobile should be. Using BodyValue {0}.", mob.BodyValue), ConsoleColor.Red);
                    foreach (int type in exclude)
                        if (mob.BodyValue == type)
                            return true;
                }
                else if (obj is Item item)
                {
                    foreach (int type in exclude)
                        if (item.ItemID == type)
                            return true;
                }
            }

            return false;
        }
        private static bool ExcludeThisType(object obj, Type[]? exclude)
        {
            if (obj != null && exclude != null)
            {
                if (obj is Mobile mob)
                {
                    foreach (Type type in exclude)
                        if (type.IsAssignableFrom(mob.GetType()))
                            return true;
                }
                else if (obj is Item item)
                {
                    foreach (Type type in exclude)
                        if (type.IsAssignableFrom(item.GetType()))
                            return true;
                }
            }

            return false;
        }
        #endregion WipeRect & Z Breaks
        #region Area Operations
        public static int RefreshDeco(Map map, List<Rectangle2D> rects,
            int startingZ = 0, Type[]? typeExclude = null, int[]? idExclude = null, int lenientZ = 0)
        {
            int count = 0;
            if (rects == null)
                return count;

            foreach (Rectangle2D rect in rects)
                Utility.WipeRect(map, rect, startingZ, WipeRectFlags.Items, typeExclude, idExclude, lenientZ);

            // Felucca
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Felucca"), DecoMode.add, file: "*.cfg", listOnly: false,
                types: null, boundingRect: rects, exclude: typeExclude, maps: new Map[] { map });

            // Britannia
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Britannia"), DecoMode.add, file: "*.cfg", listOnly: false,
                types: null, boundingRect: rects, exclude: typeExclude, maps: new Map[] { map });

            // count up the changes
            foreach (Item item in Server.World.Items.Values)
                if (item != null && item.Deleted == false && item.Map == map)
                    foreach (Rectangle2D rect in rects)
                        if (rect.Contains(item))
                            if (typeExclude == null || !Array.Exists(typeExclude, element => element == item.GetType()))
                                count++;
                            else
                                ;

            // Debug break point
            return count;
        }
        public static int CountItems(Map map, Rectangle2D rect)
        {
            int count = 0;
            IPooledEnumerable eable = map.GetItemsInBounds(rect);
            foreach (Item item in eable)
                if (item is Item && item.Deleted == false)
                    count++;
            eable.Free();
            return count;
        }
        public static int MakeStatic(Rectangle2D rect, Map map)
        {
            List<Item> list = new List<Item>();
            IPooledEnumerable eable = map.GetItemsInBounds(rect);
            foreach (Item item in eable)
                if (item is Item && item.Deleted == false)
                    list.Add(item);

            foreach (Item item in list)
                Utility.ReplaceWithStatic(item);
            return list.Count;
        }
        public static bool ReplaceWithStatic(object targ)
        {
            try
            {
                Item? item = Dupe(targ);
                Static? sItem = null;
                if (item is Item && targ is Item source)
                {
                    if (item is Addon)
                    {
                        if (item is Addon addon)
                        {
                            List<Item> addonItems = new();
                            foreach (AddonComponent comp in addon.Components)
                                addonItems.Add(Utility.Dupe(comp));

                            foreach (Item item1 in addonItems)
                                ReplaceWithStatic(item1);

                            addon.Delete();
                        }
                    }
                    else
                    {
                        // strip the derived class glop
                        sItem = new Static(source.ItemID);
                        foreach (var prop in typeof(Static).GetProperties())
                        {
                            if (prop.CanRead && prop.CanWrite)
                            {
                                PropertyInfo? myPropInfo = typeof(Item).GetProperty(prop.Name);
                                prop.SetValue(sItem, myPropInfo.GetValue(source, null), null);
                            }
                        }
                    }

                    if (sItem != null)
                    {
                        sItem.MoveToWorld(source.Location, source.Map);
                        sItem.Movable = false;
                        item.Delete();
                        source.Delete();
                        return true;
                    }
                    else return false;
                }
                return false;
            }
            catch { return false; }
        }
        #endregion Area Operations
        #region Find Items At
        public static List<Item> FindItemsAt(Point3D px, Map map, int lenientZ = 0)
        {
            List<Item> list = new();
            Point3D target = px;
            IPooledEnumerable eable = map.GetItemsInRange(target, 0);
            foreach (Item item in eable)
                if (item != null && !item.Deleted && item.Map == map)
                    if (Math.Abs(item.Z - px.Z) <= lenientZ)
                        list.Add(item);
            eable.Free();

            return list;
        }
        public static List<Item> FindItemAt(Point3D px, Map map, Type type, int lenientZ = 0)
        {
            List<Item> list = new();
            Point3D target = px;
            IPooledEnumerable eable = map.GetItemsInRange(target, 0);
            foreach (Item item in eable)
                if (item != null && !item.Deleted && item.Map == map)
                    if (type.IsAssignableFrom(item.GetType()) && Math.Abs(item.Z - px.Z) <= lenientZ)
                        list.Add(item);
            eable.Free();

            return list;
        }
        public static Item? FindOneItemAt(Point3D px, Map map, Type type, int lenientZ = 0, bool warnMulti = true)
        {
            List<Item> list = FindItemAt(px, map, type, lenientZ);
            if (list.Count > 1 && warnMulti)
                Utility.Monitor.WriteLine(string.Format("Logic Error: Found more than one {0} where expected {1}. ", type.Name, px), ConsoleColor.Red);
            if (list.Count > 0)
                return list[0];
            return null;
        }
        public static List<Item> FindItemAt(Point3D px, Map map, int ItemID, int lenientZ = 0)
        {
            List<Item> list = new();
            Point3D target = px;
            IPooledEnumerable eable = map.GetItemsInRange(target, 0);
            foreach (Item item in eable)
                if (item != null && !item.Deleted && item.Map == map)
                    if (item.ItemID == ItemID && Math.Abs(item.Z - px.Z) <= lenientZ)
                        list.Add(item);
            eable.Free();

            return list;
        }
        public static Item? FindOneItemAt(Point3D px, Map map, int ItemID, int lenientZ = 0, bool warnMulti = true)
        {
            List<Item> list = FindItemAt(px, map, ItemID, lenientZ);
            if (list.Count > 1 && warnMulti)
                Utility.Monitor.WriteLine(string.Format("Logic Error: Found more than one {0:X} where expected {1}. ", ItemID, px), ConsoleColor.Red);
            if (list.Count > 0)
                return list[0];
            return null;
        }
        #endregion Find Items At
        #region CanSpawnMobile
        /// <summary>
        /// Determine if we can spawn a mobile there, and return the Z(s)
        /// </summary>
        /// <param name="map"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static List<int> GetHardZs(Map map, Point3D point, bool allowExotics = true)
        {
            // assume the spawner location is a valid z
            List<int> list = new List<int>();
            list.Add(point.Z);

            // get the standards
            int landZ = 0, landAvg = 0, landTop = 0, docks = 0;
            map.GetAverageZ(point.X, point.Y, ref landZ, ref landAvg, ref landTop);

            if (!list.Contains(landZ)) list.Add(landZ);
            if (!list.Contains(landAvg)) list.Add(landAvg);
            if (!list.Contains(landTop)) list.Add(landTop);

            // now for the exotics
            List<int> exotics = new();
            if (allowExotics)
                // see if we can find a Z on another floor
                exotics = Utility.HardZBreaks(map, point.X, point.Y, landAvg);

            foreach (int ix in exotics)
                if (!list.Contains(ix)) list.Add(ix);

            return list;
        }

        public static bool CanSpawnMobile(Map map, int x, int y, ref int z, bool allowExotics = false)
        {
            CanFitFlags flags = Utility.CanFitFlags.requireSurface | Utility.CanFitFlags.canSwim;
            return CanSpawnMobile(map, x, y, ref z, flags, allowExotics);
        }
        public static bool CanSpawnMobile(Map map, int x, int y, ref int z, CanFitFlags flags, bool allowExotics = false, List<int>? Zs = null)
        {
            List<int> exotics = new();
            if (allowExotics)
                // see if we can find a Z on another floor
                // public static List<int> HardZBreaks(Map map, int x, int y, int z)
                exotics = Utility.HardZBreaks(map, x, y, Utility.GetAverageZ(map, x, y));

            int landZ = 0, landAvg = 0, landTop = 0, docks = 0;
            map.GetAverageZ(x, y, ref landZ, ref landAvg, ref landTop);

            Point2D px = new Point2D(x, y);
            if (Zs == null)
                Zs = new List<int>();

            // try the original location first
            if (CanSpawnLandMobile(map, px, z, flags))
            {
                if (!Zs.Contains(z))
                    Zs.Add(z);
            }

            if (CanSpawnLandMobile(map, px, landZ, flags))
            {
                z = landZ;
                if (!Zs.Contains(z))
                    Zs.Add(z);
            }

            if (CanSpawnLandMobile(map, px, landAvg, flags))
            {
                z = landAvg;
                if (!Zs.Contains(z))
                    Zs.Add(z);
            }

            if (CanSpawnLandMobile(map, px, landTop, flags))
            {
                z = landTop;
                if (!Zs.Contains(z))
                    Zs.Add(z);
            }

            // check the docks
            for (docks = -1; docks > -4; docks--)
                if (CanSpawnLandMobile(map, px, docks, flags))
                {
                    z = docks;
                    if (!Zs.Contains(z))
                        Zs.Add(z);
                }

            // now for the exotics
            if (allowExotics)
                foreach (int Z in exotics)
                    if (CanSpawnLandMobile(map, x, y, Z))
                    {
                        z = Z;
                        if (!Zs.Contains(z))
                            Zs.Add(z);
                    }

            // return the first one found, which should be the best / most normal
            if (Zs.Count > 0)
                z = Zs[0];

            #region Finish
            var Zlist = from num in Zs
                        orderby num descending
                        select num;

            foreach (int ix in Zlist)
                if (!Zs.Contains(ix))
                    Zs.Add(ix);
            #endregion Finish

            return Zs.Count > 0;
        }
        #endregion CanSpawnMobile
        #endregion Map Fit, Spawning, Z & Paths

        #region World Areas
        #region Dungeons
        public static bool TeleporterFromTo(Teleporter tp, List<Rectangle2D>? from = null, List<Rectangle2D>? to = null)
        {
            if (from != null)
                foreach (Rectangle2D rect in from)
                    if (rect.Contains(tp.Location))
                        return true;

            if (to != null)
                foreach (Rectangle2D rect in to)
                    if (rect.Contains(tp.PointDest))
                        return true;

            return false;
        }
        public static bool IsDungeonTeleporter(Teleporter tp)
        {
            if (tp.MapDest != Map.Felucca)
                // need to handle other map configurations
                return false;

            // first check to see if this teleporter is in a dungeon
            foreach (Region rx in Region.Regions)
                if (rx != null && rx.Map == Map.Felucca && rx.IsDungeonRules)
                    foreach (Rectangle3D r in rx.Coords)
                        if (r.Contains(tp.Location) || r.Contains(tp.PointDest))
                            return true;
            return false;
        }
        // 
        public static bool IsDungeonEnterExitTeleporter(Teleporter tp)
        {
            if (tp.MapDest != Map.Felucca)
                // need to handle other map configurations
                return false;

            // first check to see if this teleporter is in a dungeon
            foreach (Region rx in Region.Regions)
                if (rx != null && rx.Map == Map.Felucca && rx.IsDungeonRules)
                    foreach (Rectangle3D r in rx.Coords)
                        if ((Utility.IsDungeon(tp.Location) && !Utility.IsDungeon(tp.PointDest)) || (!Utility.IsDungeon(tp.Location) && Utility.IsDungeon(tp.PointDest)))
                            return true;
            return false;
        }
        public static bool IsDungeon(Point3D px)
        {
            // first check to see if this point is in a dungeon
            foreach (Region rx in Region.Regions)
                if (rx != null && rx.Map == Map.Felucca && rx.IsDungeonRules)
                    foreach (Rectangle3D r in rx.Coords)
                        if (r.Contains(px))
                            return true;
            return false;
        }
        #endregion Dungeons
        #region Teleporter Rules
        public static class TeleporterRuleHelpers
        {
            // Britain
            public static bool FromBrit(Teleporter teleporter)
            { return WorldMap.BritMainLandWrap.Contains(teleporter.Location) || WorldMap.Dungeons.Contains(teleporter.Location) && teleporter.Map == Map.Felucca; }

            // Lost Lands
            public static bool ToLostLands(Teleporter teleporter)
            { return WorldMap.LostLandsWrap.Contains(teleporter.PointDest) && teleporter.MapDest == Map.Felucca; }

            // "Terathan Keep"

            // "Khaldun"

            /// <summary>
            /// Checks if a location is inside a region on a specific map.
            /// </summary>
            public static bool InRegion(Point3D loc, Map map, string regionName)
            {
                if (map == null || string.IsNullOrEmpty(regionName))
                    return false;

                Region? reg = Region.FindByName(regionName, map);

                if (reg != null)
                    return reg.Contains(loc);

                return false;
            }

            /// <summary>
            /// Overload that accepts an IEntity (Mobile or Item).
            /// Automatically uses the entity's location and map.
            /// </summary>
            public static bool InRegion(IEntity entity, string regionName)
            {
                if (entity == null || entity.Deleted)
                    return false;

                // If it's an item inside a container, use the root parent
                if (entity is Item item && item.RootParent != null)
                    entity = item.RootParent as IEntity;

                if (entity == null)
                    return false;

                return InRegion(entity.Location, entity.Map, regionName);
            }
        }
        #endregion Teleporter Rules
        #region Region Bounds
        /// <summary>
        /// Calculate and return a list of points that define a regions bounds
        /// </summary>
        /// <param name="r"></param>
        /// <param name="m"></param>
        /// <param name="points"></param>
        public static void CalcRegionBounds(Rectangle2D r, Map m, ref List<Point3D> points)
        {
            if (m == Map.Internal || m == null)
                return;

            Point3D p1 = new Point3D(r.X, r.Y - 1, 0);              //So we don't need to create a new one each point
            Point3D p2 = new Point3D(r.X, r.Y + r.Height - 1, 0);   //So we don't need to create a new one each point

            points.Add(new Point3D(r.X - 1, r.Y - 1, m.GetAverageZ(r.X, r.Y - 1))); //Top Corner

            for (int x = r.X; x <= (r.X + r.Width - 1); x++)
            {
                p1.X = x;
                p2.X = x;

                p1.Z = m.GetAverageZ(p1.X, p1.Y);
                p2.Z = m.GetAverageZ(p2.X, p2.Y);

                points.Add(p1); //North bound
                points.Add(p2); //South bound
            }

            p1 = new Point3D(r.X - 1, r.Y - 1, 0);
            p2 = new Point3D(r.X + r.Width - 1, r.Y, 0);

            for (int y = r.Y; y <= (r.Y + r.Height - 1); y++)
            {
                p1.Y = y;
                p2.Y = y;

                p1.Z = m.GetAverageZ(p1.X, p1.Y);
                p2.Z = m.GetAverageZ(p2.X, p2.Y);

                points.Add(p1); //West Bound
                points.Add(p2); //East Bound
            }
        }
        public static void CalcRegionBounds(Region r, ref List<Point3D> points)
        {
            if (r == null || r.Coords.Count == 0)
                return;

            foreach (Rectangle3D rect3D in r.Coords)
            {
                Rectangle2D rect = new Rectangle2D(rect3D.Start, rect3D.End);
                CalcRegionBounds(rect, r.Map, ref points);
            }
        }
        #endregion Region Bounds
        #region World Map

        public static class WorldMap
        {
            private static Rectangle2D[] m_BritWrap = new Rectangle2D[] { new Rectangle2D(16, 16, 5120 - 32, 4096 - 32), new Rectangle2D(5136, 2320, 992, 1760) };
            private static Rectangle2D[] m_IlshWrap = new Rectangle2D[] { new Rectangle2D(16, 16, 2304 - 32, 1600 - 32) };
            private static Rectangle2D[] m_TokunoWrap = new Rectangle2D[] { new Rectangle2D(16, 16, 1448 - 32, 1448 - 32) };
            private static Rectangle2D[] m_SolenCaves = new Rectangle2D[] { new Rectangle2D(5632, 1775, 305, 266) };
            private static Rectangle2D[] m_Dungeons = new Rectangle2D[] { new Rectangle2D(new Point2D(5120, 3), new Point2D(6143, 2303)) };
            private static Rectangle2D[] m_DaggerIsland = new Rectangle2D[] { new Rectangle2D(3865, 175, 432, 584) };


            // Rectangle to define Angel Island boundaries.
            // used to define where boats can be placed, and where they can sail.
            private static Rectangle2D m_AngelIsland = new Rectangle2D(140, 690, 270, 180);
            public static Rectangle2D AngelIsland { get { return m_AngelIsland; } }

            // Rectangle to define The New Player Starting Area boundaries.
            private static Rectangle2D m_NewPlayerStartingArea = new Rectangle2D(5700, 940, 65, 60);
            public static Rectangle2D NewPlayerStartingArea { get { return m_NewPlayerStartingArea; } }

            public static Rectangle2D[] BritWrap { get { return m_BritWrap; } }
            public static Rectangle2D BritMainLandWrap { get { return m_BritWrap[0]; } }
            public static Rectangle2D LostLandsWrap { get { return m_BritWrap[1]; } }
            public static Rectangle2D SolenCaves { get { return m_SolenCaves[0]; } }
            public static Rectangle2D Dungeons { get { return m_Dungeons[0]; } }
            public static Rectangle2D DaggerIsland { get { return m_DaggerIsland[0]; } }

            public static bool IsValidLocation(Point3D p, Map map)
            {
                Rectangle2D[] wrap = GetWrapFor(map);

                for (int i = 0; i < wrap.Length; ++i)
                {
                    if (wrap[i].Contains(p))
                        return true;
                }

                return false;
            }
            public static bool IsIllegalInTestCenter(Mobile m)
            {
                if (m is not PlayerMobile pm)
                    return false;

                if (pm.Clone)
                    return false;

                if (pm.Map != Map.TestCenter)
                    return false;

                if (pm.AccessLevel != AccessLevel.Player)
                    return false;

                if (Utility.WorldMap.NewPlayerStartingArea.Contains(pm.Location))
                    return false;

                return true;
            }
            public static bool InaccessibleMapLoc(Point3D p, Map map)
            {
                RecallRune marker = new RecallRune();
                marker.MoveToWorld(p, map);

                IPooledEnumerable eable;
                eable = map.GetItemsInRange(p, 0);
                bool found = false;
                foreach (Item item in eable)
                {
                    if (item == null || item.Deleted || item is not RecallRune)
                        continue;

                    // yeah, GetItemsInRange() does not respect exact Z, not sure why
                    if (item.Z != p.Z)
                        continue;

                    found = true;
                    break;
                }
                eable.Free();

                marker.Delete();

                return !found;
            }

            public static Rectangle2D[] GetWrapFor(Map map)
            {
                if (map == Map.Ilshenar)
                    return m_IlshWrap;
                else if (map == Map.Tokuno)
                    return m_TokunoWrap;
                else
                    return m_BritWrap;
            }

            public static bool AreaContains(Rectangle2D[] area, Point2D p)
            {
                for (int i = 0; i < area.Length; i++)
                {
                    if (area[i].Contains(p))
                        return true;
                }

                return false;
            }
        }
        #endregion World Map
        #endregion World Areas
    }
}
