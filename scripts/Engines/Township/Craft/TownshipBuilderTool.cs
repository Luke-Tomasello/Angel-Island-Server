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

/* Engines/Township/Craft/TownshipBuilderTool.cs
 * CHANGELOG:
 *  6/26/2024, Adam
 *      increase m_SizeMax from 20 to 22 (to handle Tabby's Yew tree.)
 *  7/2/23, Yoar
 *      Fixes to building rules:
 *      1. Cannot build on boats
 *      2. Movable items cannot count as valid surfaces
 *  4/29/22, Yoar
 *      - Added TownshipSettings.HouseClearance: Clearance requirement to any house (guilded or not).
 *      - Added the ability to build stuff (notably floors) underneath the front-steps of houses.
 *        For this, TownshipSettings.HouseClearance must be set to 0.
 *  1/26/22, Yoar
 *      Added BuildEntry struct and BuildOptions class
 *      Renamed TownshipBuilder.Canbuild -> TownshipBuilder.Validate
 *      The build configurations are now tied to the BuildOptions object
 *  1/17/22, Yoar
 *      Added support for township addons.
 * 1/14/22, Yoar
 *      Rewrote placement checker completely:
 *      - Now using ushort[,] distance matrix so that we can bring back the number of nested for-loops;
 *      - Added more descriptive invalid placement messages;
 *      - Added try-catch for Live server.
 * 11/24/21, Yoar
 *	    Added 2 new rules: InsideHouse, NeutralHouseTooClose
 * 11/23/21, Yoar
 *	    Initial version.
 */

using Server.Diagnostics;
using Server.Engines.Craft;
using Server.Items;
using Server.Multis;
using Server.Regions;
using System;
using System.Collections.Generic;

namespace Server.Township
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TownshipAddonAttribute : Attribute
    {
        public static bool Check(object obj)
        {
            return (obj.GetType().GetCustomAttributes(typeof(TownshipAddonAttribute), true).Length != 0);
        }

        public TownshipAddonAttribute()
            : base()
        {
        }
    }

    public class TownshipBuilderTool : BaseTool
    {
        public override CraftSystem CraftSystem { get { return DefTownshipCraft.CraftSystem; } }

        [Constructable]
        public TownshipBuilderTool()
            : this(500)
        {
        }

        [Constructable]
        public TownshipBuilderTool(int uses)
            : base(uses, 0xFC1)
        {
            Name = "Township Builder Tool";
            LootType = LootType.Blessed;
        }

        public override void OnSingleClick(Mobile from)
        {
            // force durability display
            if (!ToolsDisplayDurability)
                DisplayDurabilityTo(from);

            base.OnSingleClick(from);
        }

        public TownshipBuilderTool(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public static class TownshipBuilder
    {
        [Flags]
        public enum BuildFlag : byte
        {
            None = 0x00,

            IgnoreGuildedPercentage = 0x01,
            AllowInsideHouse = 0x02,
            IgnorePlacer = 0x04,
            CheckFloors = 0x08,
            NeedsGround = 0x10,
            NeedsSurface = 0x20,
        }

        public enum BuildResult : byte
        {
            Invalid,
            Valid,
            OutsideTownship,
            HousingPercentage,
            InsideHouse,
            InsideNeutralHouse,
            PartInsideHouse,
            PartOutsideHouse,
            Blocked,
            NoSurface,
            BlockedByMobile,
            AgainstHouse,
            NearNeutralHouse,
            NoClearance,
            NearTeleporter,
            DuplicateFloor,
            NoGround,
        }

        private static readonly BuildOptions m_Options = new BuildOptions();

        public static BuildOptions Options { get { return m_Options; } }

        public static bool Validate(Mobile from, Point3D loc, Map map, int itemID, bool message = true)
        {
            return Process(from, Check(from, loc, map, itemID), message);
        }

        public static bool Validate(Mobile from, Point3D loc, Map map, int itemID, BuildOptions options, bool message = true)
        {
            return Process(from, Check(from, loc, map, itemID, options), message);
        }

        public static bool Validate(Mobile from, Point3D loc, Map map, Item item, bool message = true)
        {
            return Process(from, Check(from, loc, map, item), message);
        }

        public static bool Validate(Mobile from, Point3D loc, Map map, Item item, BuildOptions options, bool message = true)
        {
            return Process(from, Check(from, loc, map, item, options), message);
        }

        public static bool Validate(Mobile from, Point3D loc, Map map, BaseAddon addon, bool message = true)
        {
            return Process(from, Check(from, loc, map, addon), message);
        }

        public static bool Validate(Mobile from, Point3D loc, Map map, BaseAddon addon, BuildOptions options, bool message = true)
        {
            return Process(from, Check(from, loc, map, addon, options), message);
        }

        public static bool Process(Mobile from, BuildResult result, bool message)
        {
            if (message)
                TextDefinition.SendMessageTo(from, GetMessage(result));

            return (result == BuildResult.Valid);
        }

        private static readonly BuildEntry[] m_SingleEntry = new BuildEntry[1];

        public static BuildResult Check(Mobile from, Point3D loc, Map map, int itemID)
        {
            return Check(from, loc, map, itemID, m_Options);
        }

        public static BuildResult Check(Mobile from, Point3D loc, Map map, int itemID, BuildOptions options)
        {
            m_SingleEntry[0] = new BuildEntry(itemID, Point3D.Zero);

            return Check(from, loc, map, m_SingleEntry, options);
        }

        public static BuildResult Check(Mobile from, Point3D loc, Map map, Item item)
        {
            return Check(from, loc, map, item, m_Options);
        }

        public static BuildResult Check(Mobile from, Point3D loc, Map map, Item item, BuildOptions options)
        {
            m_SingleEntry[0] = new BuildEntry(item.ItemID, Point3D.Zero);

            return Check(from, loc, map, m_SingleEntry, options);
        }

        public static BuildResult Check(Mobile from, Point3D loc, Map map, BaseAddon addon)
        {
            return Check(from, loc, map, addon, m_Options);
        }

        public static BuildResult Check(Mobile from, Point3D loc, Map map, BaseAddon addon, BuildOptions options)
        {
            if (addon.Components.Count == 0)
                return BuildResult.Invalid; // sanity

            if (addon.Components.Count == 1)
                return Check(from, loc, map, (Item)addon.Components[0], options);

            List<BuildEntry> buildList = new List<BuildEntry>();

            for (int i = 0; i < addon.Components.Count; i++)
            {
                AddonComponent c = (AddonComponent)addon.Components[i];

                // ignore nodraw tiles
                if (c.ItemID == 0x1 || (c.ItemID >= 0x2198 && c.ItemID <= 0x21A4) || c.ItemID == 0x3F28)
                    continue;

                buildList.Add(new BuildEntry(c.ItemID, c.Offset));
            }

            return Check(from, loc, map, buildList.ToArray(), options);
        }

        public static BuildResult Check(Mobile from, Point3D loc, Map map, BuildEntry[] build)
        {
            return Check(from, loc, map, build, m_Options);
        }

        public static BuildResult Check(Mobile from, Point3D loc, Map map, BuildEntry[] build, BuildOptions options)
        {
#if DEBUG
            return CheckInternal(from, loc, map, build, options);
#else
            try
            {
                return CheckInternal(from, loc, map, build, options);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            return BuildResult.Invalid;
#endif
        }

        public static int OffsetZ = 0;

        private static BuildResult CheckInternal(Mobile from, Point3D loc, Map map, BuildEntry[] build, BuildOptions options)
        {
            if (build.Length == 0)
                return BuildResult.Invalid;

            if (map == null || map == Map.Internal)
                return BuildResult.Invalid;

            TownshipRegion tsr = TownshipRegion.GetTownshipAt(loc, map);

            if (tsr == null || tsr.TStone == null)
                return BuildResult.OutsideTownship; // there is no township here

            if (from != null && !tsr.TStone.AllowBuilding(from))
                return BuildResult.OutsideTownship; // 'from' is not a member of the township

            if (!options[BuildFlag.IgnoreGuildedPercentage] && TownshipSettings.ReqGuildedPercentage > 0.0)
            {
                double guildedPerc = TownshipDeed.CalculateHouseOwnership(tsr.TStone.TownshipCenter, tsr.TStone.Map, tsr.TStone.Extended ? TownshipStone.EXTENDED_RADIUS : TownshipStone.INITIAL_RADIUS, tsr.TStone.Guild, false);

                if (guildedPerc < TownshipSettings.ReqGuildedPercentage)
                    return BuildResult.HousingPercentage; // the guild must own all houses in the township
            }

            BaseHouse house = BaseHouse.FindHouseAt(loc, map, 16);

            OffsetZ = 0;

            if (house != null)
            {
                if (IsSteps(house, loc))
                {
                    OffsetZ = -2; // let's try building under the steps
                }
                else
                {
                    if (!options[BuildFlag.AllowInsideHouse])
                        return BuildResult.InsideHouse; // cannot build inside houses

                    if (house.Owner == null || !tsr.TStone.IsMember(house.Owner))
                        return BuildResult.InsideNeutralHouse; // cannot build inside neutral houses
                }
            }

            BaseBoat boat = BaseBoat.FindBoatAt(loc, map, 16);

            if (boat != null)
                return BuildResult.Blocked; // cannot build on boats

            if (options[BuildFlag.NeedsGround] && loc.Z != map.GetAverageZ(loc.X, loc.Y))
                return BuildResult.NoGround;

            for (int i = 0; i < build.Length; i++)
            {
                Point3D worldLoc = new Point3D(
                    loc.X + build[i].Offset.X,
                    loc.Y + build[i].Offset.Y,
                    loc.Z + OffsetZ + build[i].Offset.Z);

                int height = TileData.ItemTable[build[i].ItemID & 0x3FFF].Height;

                if (TownshipRegion.GetTownshipAt(worldLoc, map) != tsr)
                    return BuildResult.OutsideTownship; // the entire footprint must be in the same township

                if (house != BaseHouse.FindHouseAt(worldLoc, map, height))
                    return house == null ? BuildResult.PartInsideHouse : BuildResult.PartOutsideHouse; // the entire footprint must be outside of a house or inside the same house

                bool hasSurface = false;

                LandTile landTile = map.Tiles.GetLandTile(worldLoc.X, worldLoc.Y);

                int lowZ = 0, avgZ = 0, topZ = 0;

                map.GetAverageZ(worldLoc.X, worldLoc.Y, ref lowZ, ref avgZ, ref topZ);

                TileFlag landFlags = TileData.LandTable[landTile.ID & 0x3FFF].Flags;

                if ((landFlags & TileFlag.Impassable) != 0 && avgZ > worldLoc.Z && (worldLoc.Z + height) > lowZ)
                    return BuildResult.Blocked;
                else if ((landFlags & TileFlag.Impassable) == 0 && worldLoc.Z == avgZ && !landTile.Ignored)
                    hasSurface = true;

                foreach (StaticTile tile in map.Tiles.GetStaticTiles(worldLoc.X, worldLoc.Y, true))
                {
                    ItemData id = TileData.ItemTable[tile.ID & 0x3FFF];

                    if ((id.Surface || id.Impassable) && tile.Z + id.Height > worldLoc.Z && worldLoc.Z + height > tile.Z)
                        return BuildResult.Blocked;
                    else if (id.Surface && !id.Impassable && worldLoc.Z == (tile.Z + id.Height))
                        hasSurface = true;
                }

                foreach (Item item in map.GetItemsInRange(worldLoc, 0))
                {
                    if (options.Ignore.Contains(item))
                        continue;

                    ItemData id = TileData.ItemTable[item.ItemID & 0x3FFF];

                    if ((id.Surface || id.Impassable) && item.Z + id.Height > worldLoc.Z && worldLoc.Z + height > item.Z)
                        return BuildResult.Blocked;
                    else if (!item.Movable && id.Surface && !id.Impassable && worldLoc.Z == (item.Z + id.Height))
                        hasSurface = true;
                }

                if (build[i].Offset.Z == 0 && options[BuildFlag.NeedsSurface] && !hasSurface)
                    return BuildResult.NoSurface;

                foreach (Mobile m in map.GetMobilesInRange(worldLoc, 0))
                {
                    if (options.Ignore.Contains(m))
                        continue;
                    else if (options[BuildFlag.IgnorePlacer] && m == from)
                        continue;
                    else if (m.AccessLevel == AccessLevel.Player || !m.Hidden)
                        return BuildResult.BlockedByMobile; // note: ghosts also block
                }
            }

            if (house != null)
                return BuildResult.Valid; // if we're placing in a house, skip the special checks

            #region Calculate Distance Matrix

            int xMin = int.MaxValue;
            int yMin = int.MaxValue;
            int zMin = int.MaxValue;
            int xMax = int.MinValue;
            int yMax = int.MinValue;
            int zMax = int.MinValue;

            for (int k = 0; k < build.Length; k++)
            {
                BuildEntry e = build[k];

                xMin = Math.Min(xMin, e.Offset.X);
                yMin = Math.Min(yMin, e.Offset.Y);
                zMin = Math.Min(zMin, e.Offset.Z);
                xMax = Math.Max(xMax, e.Offset.X);
                yMax = Math.Max(yMax, e.Offset.Y);
                zMax = Math.Max(zMax, e.Offset.Z) + TileData.ItemTable[e.ItemID & 0x3FFF].Height;
            }

            const int addSize = 5; // add 5 tiles on either side

            int placeSize = 1 + Math.Max(xMax - xMin, yMax - yMin); // size of square box around what we're trying to place
            int checkSize = placeSize + 2 * addSize;        // size of square box around the area we need to check

            ushort[,] distances = GetMatrix(checkSize); // square matrix of size checkSize with all elements set to '1'

            int offset = (checkSize - placeSize) / 2;

            // now "draw" the footprint in the center of the matrix as '0' values
            for (int k = 0; k < build.Length; k++)
            {
                int i = build[k].Offset.Y - yMin + offset;
                int j = build[k].Offset.X + xMin + offset;

                if (i >= 0 && i < checkSize && j >= 0 && j < checkSize)
                    distances[i, j] = 0;
            }

            // calculate chebyshev distances
            DistanceMatrix.Fill(distances);

            #endregion

            for (int i = 0; i < checkSize; i++)
            {
                try
                {
                    for (int j = 0; j < checkSize; j++)
                    {
                        Point3D worldLoc = new Point3D(
                            loc.X + j + xMin - offset,
                            loc.Y + i + yMin - offset,
                            loc.Z + OffsetZ + zMin);

                        int height = zMax - zMin;

                        ushort dist = distances[i, j]; // chebyshev distance to the nearest footprint tile

                        if (dist <= TownshipSettings.HouseClearance || dist <= TownshipSettings.NeutralHouseClearance)
                        {
                            BaseHouse otherHouse = BaseHouse.FindHouseAt(worldLoc, map, height);

                            if (otherHouse != null)
                            {
                                if (IsSteps(otherHouse, worldLoc))
                                    continue; // ignore steps

                                if (dist <= TownshipSettings.HouseClearance)
                                    return BuildResult.AgainstHouse; // cannot build against any house

                                if (dist <= TownshipSettings.NeutralHouseClearance && (otherHouse.Owner == null || !tsr.TStone.IsMember(otherHouse.Owner)))
                                    return BuildResult.NearNeutralHouse; // cannot build too close to unguilded houses
                            }
                        }

                        if (dist <= options.Clearance)
                        {
                            foreach (StaticTile tile in map.Tiles.GetStaticTiles(worldLoc.X, worldLoc.Y, true))
                            {
                                ItemData id = TileData.ItemTable[tile.ID & 0x3FFF];

                                if ((id.Surface || id.Impassable) && tile.Z + id.Height > worldLoc.Z && worldLoc.Z + height > tile.Z)
                                    return BuildResult.NoClearance; // blocked by a static
                            }

                            foreach (Item item in map.GetItemsInRange(worldLoc, 0))
                            {
                                if (options.Ignore.Contains(item))
                                    continue;
                                else if (typeof(FortificationWall).IsAssignableFrom(item.GetType()) || typeof(TownshipDoor).IsAssignableFrom(item.GetType()))
                                    continue; // ignore walls/doors

                                ItemData id = TileData.ItemTable[item.ItemID & 0x3FFF];

                                if ((id.Surface || id.Impassable) && item.Z + id.Height > worldLoc.Z && worldLoc.Z + height > item.Z)
                                    return BuildResult.NoClearance; // blocked by an item
                            }
                        }

                        if (dist <= TownshipSettings.TeleporterClearance)
                        {
                            foreach (Item item in map.GetItemsInRange(worldLoc, 0))
                            {
                                if (options.Ignore.Contains(item))
                                    continue;
                                else if (item is Teleporter || item is WelcomeMat)
                                    return BuildResult.NearTeleporter; // cannot build too close to a teleporter
                            }
                        }

                        if (options[BuildFlag.CheckFloors] && dist == 0)
                        {
                            foreach (Item item in map.GetItemsInRange(worldLoc, 0))
                            {
                                if (options.Ignore.Contains(item))
                                    continue;
                                else if (!item.Movable && item.Z == worldLoc.Z && TileData.ItemTable[item.ItemID & 0x3FFF].Height == 0)
                                    return BuildResult.DuplicateFloor; // can't place floors on top of other zero-height statics
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    return BuildResult.Invalid;
                }
            }

            return BuildResult.Valid;
        }

        private static bool IsSteps(BaseHouse house, Point3D loc)
        {
            MultiComponentList mcl = house.Components;

            int vx = loc.X - (house.X + mcl.Min.X);
            int vy = loc.Y - (house.Y + mcl.Min.Y);

            if (vx < 0 || vx >= mcl.Width || vy < 0 || vy >= mcl.Height)
                return false; // sanity

            foreach (StaticTile tile in mcl.Tiles[vx][vy])
            {
                ItemData id = TileData.ItemTable[tile.ID & 0x3FFF];

                if ((id.Flags & TileFlag.Roof) != 0)
                    continue; // ignore roofs
                else if (tile.Z >= 5 || (id.Flags & TileFlag.Bridge) == 0)
                    return false; // not a staircase
            }

            return true;
        }
        // 6/26/2024, Adam: increase m_SizeMax from 20 to 22 (to handle Tabby's Yew tree.)
        private const int m_SizeMax = 22; // should never need a matrix larger than this
        private static readonly Dictionary<int, ushort[,]> m_Matrices = new Dictionary<int, ushort[,]>();

        private static ushort[,] GetMatrix(int size)
        {
            size = Math.Max(0, Math.Min(m_SizeMax, size));

            ushort[,] matrix;

            if (!m_Matrices.TryGetValue(size, out matrix))
                m_Matrices[size] = matrix = new ushort[size, size];

            // reset values to 1
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                    matrix[i, j] = 1;
            }

            return matrix;
        }

        public static TextDefinition GetMessage(BuildResult result)
        {
            switch (result)
            {
                case BuildResult.Valid:
                    return 0;
                case BuildResult.OutsideTownship:
                    return "You can only place this within the bounds of your township.";
                case BuildResult.HousingPercentage:
                    return "Your guild must own all houses in this township to place this.";
                case BuildResult.InsideHouse:
                    return "You can't place this inside a house.";
                case BuildResult.InsideNeutralHouse:
                    return "You can't place this inside a house that isn't part of your township.";
                case BuildResult.Blocked:
                    return "Something is preventing you from placing this here.";
                case BuildResult.NoSurface:
                    return "You can only place this on a flat and walkable surface.";
                case BuildResult.BlockedByMobile:
                    return "Someone is preventing you from placing this here.";
                case BuildResult.AgainstHouse:
                    return "You can't place this against a house.";
                case BuildResult.NearNeutralHouse:
                    return "You can't place this near a house that isn't part of your township.";
                case BuildResult.NoClearance:
                    return "Something nearby is preventing you from placing this here.";
                case BuildResult.NearTeleporter:
                    return "You can't place this near a teleporter.";
                case BuildResult.DuplicateFloor:
                    return "You can't place this on top of another floor.";
                case BuildResult.NoGround:
                    return "You need solid ground to place this on.";
            }

            return 501942; // That location is blocked.
        }

        public struct BuildEntry
        {
            private int m_ItemID;
            private Point3D m_Offset;

            public int ItemID { get { return m_ItemID; } }
            public Point3D Offset { get { return m_Offset; } }

            public BuildEntry(int itemID, Point3D offset)
            {
                m_ItemID = itemID;
                m_Offset = offset;
            }
        }

        public class BuildOptions
        {
            private BuildFlag m_Flags;
            private int m_Clearance;
            private List<object> m_Ignore;

            public BuildFlag Flags
            {
                get { return m_Flags; }
                set { m_Flags = value; }
            }

            public bool this[BuildFlag flag]
            {
                get { return m_Flags.HasFlag(flag); }
                set
                {
                    if (value)
                        m_Flags |= flag;
                    else
                        m_Flags &= ~flag;
                }
            }

            public int Clearance
            {
                get { return m_Clearance; }
                set { m_Clearance = value; }
            }

            public List<object> Ignore
            {
                get { return m_Ignore; }
                set { m_Ignore = value; }
            }

            public BuildOptions()
            {
                m_Ignore = new List<object>();
            }

            public void Configure(BuildFlag flags = BuildFlag.None, int clearance = 0, params object[] ignore)
            {
                m_Flags = flags;
                m_Clearance = clearance;
                m_Ignore.Clear();
                m_Ignore.AddRange(ignore);
            }
        }
    }
}