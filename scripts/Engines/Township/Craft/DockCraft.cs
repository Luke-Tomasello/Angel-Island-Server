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

/* Engines/Township/Craft/DockCraft.cs
 * CHANGELOG:
 *  8/15/23, Yoar
 *	    Initial version.
 */

using Server.Engines.Craft;
using Server.Items;
using Server.Targeting;
using System;

namespace Server.Township
{
    public class DockCraft : TargetCraft
    {
        public override int Range { get { return 3; } }

        private bool m_South;

        public DockCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality, bool south)
            : base(from, craftItem, craftSystem, typeRes, tool, quality)
        {
            m_South = south;
        }

        protected override bool ValidateTarget(object targeted, out TextDefinition message)
        {
            if (From.Map == null || !(targeted is IPoint2D))
            {
                message = 0;
                return false;
            }

            IPoint2D p = (IPoint2D)targeted;

            InternalResult result = Validate(From.Map, p.X, p.Y);

            if (result != InternalResult.Valid)
            {
                message = GetMessage(result);
                return false;
            }

            Point3D loc = new Point3D(p, GetWaterLevel(From.Map, p.X, p.Y) + DockElevation);

            TownshipBuilder.Options.Configure(TownshipBuilder.BuildFlag.IgnorePlacer);

            TownshipBuilder.BuildResult buildResult = TownshipBuilder.Check(From, loc, From.Map, 0x7C9);

            if (buildResult != TownshipBuilder.BuildResult.Valid)
            {
                message = TownshipBuilder.GetMessage(buildResult);
                return false;
            }

            message = 0;
            return true;
        }

        protected override void OnCraft(object targeted, out TextDefinition message)
        {
            message = 0;

            IPoint2D p = (IPoint2D)targeted;

            Point3D loc = new Point3D(p.X, p.Y, GetWaterLevel(From.Map, p.X, p.Y) + DockElevation);

            DefTownshipCraft.BuildItem(From, new TownshipStatic(RandomItemID(m_South)), loc);
        }

        public static int RandomItemID(bool south)
        {
            int itemID = Utility.RandomList(m_ItemIDs);

            if (south)
                itemID += 4;

            return itemID;
        }

        private static readonly int[] m_ItemIDs = new int[]
            {
                0x7C9, 0x7C9, 0x7C9, 0x7C9, 0x7C9, 0x7C9,
                0x7CA,
                0x7CB,
                0x7CC,
            };

        public static int DockElevation = 2;

        public enum InternalResult
        {
            Valid,
            DeepWater,
            NeedsWaterOrShore,
            NeedsSupport,
        }

        public static InternalResult Validate(Map map, int x, int y)
        {
            if (SpecialFishingNet.ValidateDeepWater(map, x, y))
                return InternalResult.DeepWater;

            if (!IsWaterOrShore(map, x, y))
                return InternalResult.NeedsWaterOrShore;

            bool isSupported =
                IsLandOrDock(map, x, y - 1) ||
                IsLandOrDock(map, x + 1, y) ||
                IsLandOrDock(map, x, y + 1) ||
                IsLandOrDock(map, x - 1, y);

            if (!isSupported)
                return InternalResult.NeedsSupport;

            return InternalResult.Valid;
        }

        public static TextDefinition GetMessage(InternalResult result)
        {
            switch (result)
            {
                default:
                case InternalResult.Valid:
                    return 0;
                case InternalResult.DeepWater:
                    return "You cannot build this in deep water.";
                case InternalResult.NeedsWaterOrShore:
                    return "You can only build this on water or on the shoreline.";
                case InternalResult.NeedsSupport:
                    return "This must be supported at the shoreline or attached to another dock platform.";
            }
        }

        public static bool IsWaterOrShore(Map map, int x, int y)
        {
            return (IsWater(map, x, y) || IsShore(map, x, y));
        }

        public static bool IsWater(Map map, int x, int y)
        {
            int waterLevel = GetWaterLevel(map, x, y);

            if (map.GetAverageZ(x, y) > waterLevel)
                return false;

            LandTile landTile = map.Tiles.GetLandTile(x, y);

            bool hasWater = ContainsRanged(m_WaterTileIDs, landTile.ID);

            foreach (StaticTile staticTile in map.Tiles.GetStaticTiles(x, y))
            {
                if (staticTile.Z == waterLevel && ContainsRanged(m_WaterItemIDs, staticTile.ID))
                    hasWater = true;
                else if (staticTile.Z >= waterLevel)
                    return false;
            }

            return hasWater;
        }

        public static bool IsShore(Map map, int x, int y)
        {
            int shoreLevel = GetShoreLevel(map, x, y);

            if (map.GetAverageZ(x, y) > shoreLevel)
                return false;

            LandTile landTile = map.Tiles.GetLandTile(x, y);

            if (!ContainsRanged(m_ShoreTileIDs, landTile.ID))
                return false;

            int waterLevel = GetWaterLevel(map, x, y);

            foreach (StaticTile staticTile in map.Tiles.GetStaticTiles(x, y))
            {
                if (staticTile.Z >= waterLevel)
                    return false;
            }

            return true;
        }

        public static bool IsLandOrDock(Map map, int x, int y)
        {
            return (IsLand(map, x, y) || IsDock(map, x, y));
        }

        public static bool IsLand(Map map, int x, int y)
        {
            int shoreLevel = GetShoreLevel(map, x, y);

            int landZ = map.GetAverageZ(x, y);

            return (landZ >= shoreLevel - 5 && landZ < shoreLevel + 5 && map.CanFit(x, y, landZ, 16, false, false, true));
        }

        public static bool IsDock(Map map, int x, int y)
        {
            int dockLevel = GetWaterLevel(map, x, y) + DockElevation;

            foreach (Item item in map.GetItemsInRange(new Point3D(x, y, 0), 0))
            {
                if (!item.Movable && item.Z == dockLevel && ContainsRanged(m_DockItemIDs, item.ItemID))
                    return true;
            }

            return false;
        }

        public static int GetWaterLevel(Map map, int x, int y)
        {
            return -5;
        }

        public static int GetShoreLevel(Map map, int x, int y)
        {
            return 0;
        }

        private static bool ContainsRanged(int[] array, int value)
        {
            for (int i = 0; i < array.Length - 1; i += 2)
            {
                if (value >= array[i] && value <= array[i + 1])
                    return true;
            }

            return false;
        }

        private static readonly int[] m_WaterTileIDs = new int[]
            {
                0x00A8, 0x00AB,
                0x0136, 0x0137
            };

        private static readonly int[] m_WaterItemIDs = new int[]
            {
                0x1797, 0x179C, // full water tiles
                0x179D, 0x17B2  // shoreline
            };

        private static readonly int[] m_ShoreTileIDs = new int[]
            {
                0x001A, 0x0032,
                0x0044, 0x004B
            };

        private static readonly int[] m_DockItemIDs = new int[]
            {
                0x07C9, 0x07D0
            };
    }

    public class ExtendDockCraft : TargetCraft
    {
        public enum ExtendType
        {
            North,
            East,
            South,
            West,
        }

        public override int Range { get { return 3; } }

        private ExtendType m_Type;

        public ExtendDockCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality, ExtendType type)
            : base(from, craftItem, craftSystem, typeRes, tool, quality)
        {
            m_Type = type;
        }

        protected override void OnBeginTarget()
        {
            From.SendMessage("Target the dock platform you wish to extend.");
        }

        protected override bool ValidateTarget(object targeted, out TextDefinition message)
        {
            if (From.Map == null)
            {
                message = 0;
                return false;
            }

            Item item = targeted as Item;

            if (item == null || item.Movable || (!ContainsRanged(m_DockEastItemIDs, item.ItemID) && !ContainsRanged(m_DockSouthItemIDs, item.ItemID)))
            {
                message = "You must target a dock platform to extend it.";
                return false;
            }

            Point3D loc = new Point3D(item.X, item.Y, DockCraft.GetWaterLevel(From.Map, item.X, item.Y) + DockCraft.DockElevation) + GetOffset();

            DockCraft.InternalResult result = DockCraft.Validate(From.Map, loc.X, loc.Y);

            if (result != DockCraft.InternalResult.Valid)
            {
                message = DockCraft.GetMessage(result);
                return false;
            }

            TownshipBuilder.Options.Configure(TownshipBuilder.BuildFlag.IgnorePlacer);

            TownshipBuilder.BuildResult buildResult = TownshipBuilder.Check(From, loc, From.Map, 0x7C9);

            if (buildResult != TownshipBuilder.BuildResult.Valid)
            {
                message = TownshipBuilder.GetMessage(buildResult);
                return false;
            }

            message = 0;
            return true;
        }

        protected override void OnCraft(object targeted, out TextDefinition message)
        {
            message = 0;

            Item item = (Item)targeted;

            bool south = ContainsRanged(m_DockSouthItemIDs, item.ItemID);

            Point3D loc = new Point3D(item.X, item.Y, DockCraft.GetWaterLevel(From.Map, item.X, item.Y) + DockCraft.DockElevation) + GetOffset();

            DefTownshipCraft.BuildItem(From, new TownshipStatic(DockCraft.RandomItemID(south)), loc);
        }

        private Point3D GetOffset()
        {
            switch (m_Type)
            {
                case ExtendType.North: return new Point3D(0, -1, 0);
                case ExtendType.East: return new Point3D(+1, 0, 0);
                case ExtendType.South: return new Point3D(0, +1, 0);
                case ExtendType.West: return new Point3D(-1, 0, 0);
            }

            return Point3D.Zero;
        }

        private static bool ContainsRanged(int[] array, int value)
        {
            for (int i = 0; i < array.Length - 1; i += 2)
            {
                if (value >= array[i] && value <= array[i + 1])
                    return true;
            }

            return false;
        }

        private static readonly int[] m_DockEastItemIDs = new int[]
            {
                0x07C9, 0x07CC
            };

        private static readonly int[] m_DockSouthItemIDs = new int[]
            {
                0x07CD, 0x07D0
            };
    }

    public class PierFoundationCraft : TargetCraft
    {
        public override int Range { get { return 3; } }

        public PierFoundationCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality)
            : base(from, craftItem, craftSystem, typeRes, tool, quality)
        {
        }

        protected override bool ValidateTarget(object targeted, out TextDefinition message)
        {
            if (From.Map == null || !(targeted is IPoint2D))
            {
                message = 0;
                return false;
            }

            IPoint2D p = (IPoint2D)targeted;

            if (SpecialFishingNet.ValidateDeepWater(From.Map, p.X, p.Y))
            {
                message = "You cannot build this in deep water.";
                return false;
            }

            if (!DockCraft.IsWater(From.Map, p.X, p.Y))
            {
                message = "You can only build this on water or on top of another pier.";
                return false;
            }

            Point3D loc = new Point3D(p.X, p.Y, DockCraft.GetWaterLevel(From.Map, p.X, p.Y));

            TownshipBuilder.Options.Configure(TownshipBuilder.BuildFlag.IgnorePlacer);

            TownshipBuilder.BuildResult buildResult = TownshipBuilder.Check(From, loc, From.Map, 0x3AE);

            if (buildResult != TownshipBuilder.BuildResult.Valid)
            {
                message = TownshipBuilder.GetMessage(buildResult);
                return false;
            }

            message = 0;
            return true;
        }

        protected override void OnCraft(object targeted, out TextDefinition message)
        {
            message = 0;

            IPoint2D p = (IPoint2D)targeted;

            Point3D loc = new Point3D(p.X, p.Y, DockCraft.GetWaterLevel(From.Map, p.X, p.Y));

            DefTownshipCraft.BuildItem(From, new TownshipStatic(0x3AE), loc);
        }
    }

    public class PierCraft : TargetCraft
    {
        public override int Range { get { return 3; } }
        public override bool AllowGround { get { return false; } }

        private int m_ItemID;

        public PierCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality, int itemID)
            : base(from, craftItem, craftSystem, typeRes, tool, quality)
        {
            m_ItemID = itemID;
        }

        protected override void OnBeginTarget()
        {
            From.SendMessage("Target a pier foundation to build this on.");
        }

        protected override bool ValidateTarget(object targeted, out TextDefinition message)
        {
            if (From.Map == null)
            {
                message = 0;
                return false;
            }

            Item item = targeted as Item;

            if (item == null || item.Movable || item.Parent != null || item.ItemID != 0x3AE)
            {
                message = "You can only build this on top of a pier foundation.";
                return false;
            }

            if (item.ItemID != 0x3AE)
            {
                message = "You cannot extend this pier any higher.";
                return false;
            }

            Point3D loc = new Point3D(item.X, item.Y, item.Z + item.ItemData.CalcHeight);

            TownshipBuilder.Options.Configure(TownshipBuilder.BuildFlag.IgnorePlacer);

            TownshipBuilder.BuildResult buildResult = TownshipBuilder.Check(From, loc, From.Map, m_ItemID);

            if (buildResult != TownshipBuilder.BuildResult.Valid)
            {
                message = TownshipBuilder.GetMessage(buildResult);
                return false;
            }

            message = 0;
            return true;
        }

        protected override void OnCraft(object targeted, out TextDefinition message)
        {
            message = 0;

            Item item = (Item)targeted;

            Point3D loc = new Point3D(item.X, item.Y, item.Z + item.ItemData.CalcHeight);

            DefTownshipCraft.BuildItem(From, new TownshipStatic(m_ItemID), loc);
        }
    }

    public class RopeCraft : CustomCraft
    {
        private Item m_First;
        private Item m_Secnd;

        public RopeCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality)
            : base(from, craftItem, craftSystem, typeRes, tool, quality)
        {
        }

        public override void EndCraftAction()
        {
            From.SendMessage("Target the first pier.");
            From.Target = new FirstTarget(this);
        }

        private bool ValidateFirstTarget(out TextDefinition message)
        {
            if (m_First == null || m_First.Movable || m_First.Parent != null || !ContainsRanged(m_PierItemIDs, m_First.ItemID))
            {
                message = "You must target a pier.";
                return false;
            }

            message = 0;
            return true;
        }

        private bool ValidateSecndTarget(out TextDefinition message)
        {
            if (m_Secnd == null || m_Secnd.Movable || m_Secnd.Parent != null || !ContainsRanged(m_PierItemIDs, m_Secnd.ItemID))
            {
                message = "You must target a pier.";
                return false;
            }

            if (m_First.Z != m_Secnd.Z)
            {
                message = "The piers must have the same elevation.";
                return false;
            }

            Item first = m_First;
            Item secnd = m_Secnd;

            if (first.X > secnd.X || first.Y > secnd.Y)
            {
                Item temp = first;
                first = secnd;
                secnd = temp;
            }

            int dx = (secnd.X - first.X);
            int dy = (secnd.Y - first.Y);

            if (dx != 0 && dy != 0)
            {
                message = "The piers must be aligned.";
                return false;
            }

            int dist = Math.Max(dx, dy);

            if (dist > 5)
            {
                message = "The piers are too far away from each other.";
                return false;
            }

            bool vertical = (dy > 0);

            Point3D offset = new Point3D(vertical ? 0 : 1, vertical ? 1 : 0, 0);

            TownshipBuilder.BuildEntry[] build = new TownshipBuilder.BuildEntry[dist + 1];

            build[0] = new TownshipBuilder.BuildEntry(0x1, Point3D.Zero);

            for (int i = 0; i < dist; i++)
                build[i + 1] = new TownshipBuilder.BuildEntry(0x1, build[i].Offset + offset);

            TownshipBuilder.Options.Configure(TownshipBuilder.BuildFlag.IgnorePlacer, 0, first, secnd);

            TownshipBuilder.BuildResult buildResult = TownshipBuilder.Check(From, first.Location, From.Map, build);

            if (buildResult != TownshipBuilder.BuildResult.Valid)
            {
                message = TownshipBuilder.GetMessage(buildResult);
                return false;
            }

            message = 0;
            return true;
        }

        public override Item CompleteCraft(out TextDefinition message)
        {
            message = 0;

            Item first = m_First;
            Item secnd = m_Secnd;

            if (first.X > secnd.X || first.Y > secnd.Y)
            {
                Item temp = first;
                first = secnd;
                secnd = temp;
            }

            int dx = (secnd.X - first.X);
            int dy = (secnd.Y - first.Y);

            int dist = Math.Max(dx, dy);

            bool vertical = (dy > 0);

            Point3D offset = new Point3D(vertical ? 0 : 1, vertical ? 1 : 0, 0);

            if (first.ItemID == 0x3A5 || first.ItemID == 0x3AA)
                first.ItemID = (vertical ? 0x3AC : 0x3AB);
            else
                first.ItemID = 0x3AA;

            if (secnd.ItemID == 0x3A5)
                secnd.ItemID = 0x3AA;

            Point3D loc = first.Location;

            for (int i = 0; i < dist - 1; i++)
            {
                loc += offset;

                DefTownshipCraft.BuildItem(From, new TownshipStatic(vertical ? 0x3A9 : 0x3A6), loc);
            }

            return null;
        }

        private class FirstTarget : Target
        {
            private RopeCraft m_Craft;

            public FirstTarget(RopeCraft craft)
                : base(3, false, TargetFlags.None)
            {
                m_Craft = craft;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                m_Craft.m_First = targeted as Item;

                TextDefinition message;

                if (!m_Craft.ValidateFirstTarget(out message))
                {
                    m_Craft.Failure(message);
                    return;
                }

                m_Craft.From.SendMessage("Target the second pier.");
                m_Craft.From.Target = new SecndTarget(m_Craft);
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                if (cancelType == TargetCancelType.Canceled)
                    m_Craft.Failure(null);
            }
        }

        private class SecndTarget : Target
        {
            private RopeCraft m_Craft;

            public SecndTarget(RopeCraft craft)
                : base(3, false, TargetFlags.None)
            {
                m_Craft = craft;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                m_Craft.m_Secnd = targeted as Item;

                TextDefinition message;

                if (!m_Craft.ValidateFirstTarget(out message))
                {
                    m_Craft.Failure(message);
                    return;
                }

                if (!m_Craft.ValidateSecndTarget(out message))
                {
                    m_Craft.Failure(message);
                    return;
                }

                m_Craft.CraftItem.CompleteCraft(m_Craft.Quality, false, m_Craft.From, m_Craft.CraftSystem, m_Craft.TypeRes, m_Craft.Tool, m_Craft);
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                if (cancelType == TargetCancelType.Canceled)
                    m_Craft.Failure(null);
            }
        }

        private void Failure(TextDefinition message)
        {
            if (Tool != null && !Tool.Deleted && Tool.UsesRemaining > 0)
                From.SendGump(new CraftGump(From, CraftSystem, Tool, message));
            else
                TextDefinition.SendMessageTo(From, message);
        }

        private static bool ContainsRanged(int[] array, int value)
        {
            for (int i = 0; i < array.Length - 1; i += 2)
            {
                if (value >= array[i] && value <= array[i + 1])
                    return true;
            }

            return false;
        }

        private static readonly int[] m_PierItemIDs = new int[]
            {
                0x03A5, 0x03A5,
                0x03AA, 0x03AD
            };
    }
}