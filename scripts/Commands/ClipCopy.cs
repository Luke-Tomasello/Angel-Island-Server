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

/* Scripts/Commands/ClipCopy.cs
 * Changelog
 *	1/27/24, Yoar
 *		Initial creation.
 */

using Server.Items;
using Server.Targeting;
using System.Collections.Generic;

namespace Server.Commands.Generic
{
    public static class ClipCopy
    {
        private static readonly Dictionary<Mobile, TileEntry[]> m_Clipboards = new Dictionary<Mobile, TileEntry[]>();

        public static void Initialize()
        {
            CommandSystem.Register("ClipCopy", AccessLevel.GameMaster, new CommandEventHandler(ClipCopy_OnCommand));
            CommandSystem.Register("ClipPaste", AccessLevel.GameMaster, new CommandEventHandler(ClipPaste_OnCommand));
        }

        [Usage("ClipCopy [clientStatics=false]")]
        [Description("Copies to clipboard the bounded statics.")]
        private static void ClipCopy_OnCommand(CommandEventArgs e)
        {
            BoundingBoxPicker.Begin(e.Mobile, ClipCopy_OnBoundPicked, e);
        }

        private static void ClipCopy_OnBoundPicked(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            if (map == null || map == Map.Internal)
                return;

            CommandEventArgs args = (CommandEventArgs)state;

            Rectangle2D bounds = new Rectangle2D(start, new Point2D(end.X + 1, end.Y + 1));

            List<TileEntry> list = new List<TileEntry>();

            if (args.GetBoolean(0)) // client statics
            {
                for (int y = bounds.Y; y < bounds.Y + bounds.Height; y++)
                {
                    for (int x = bounds.X; x < bounds.X + bounds.Width; x++)
                    {
                        StaticTile[] tiles = map.Tiles.GetStaticTiles(x, y, true);

                        foreach (StaticTile tile in tiles)
                            list.Add(new TileEntry(tile.ID & TileData.MaxItemValue, tile.Hue, new Point3D(x, y, tile.Z)));
                    }
                }
            }

            List<Item> items = new List<Item>();

            foreach (Item item in map.GetItemsInBounds(bounds))
            {
                if (item.Movable || !item.Visible || !(item is Static || item is AddonComponent))
                    continue;

                items.Add(item);
            }

            items.Sort(ItemComparer.Instance);

            foreach (Item item in items)
                list.Add(new TileEntry(item.ItemID, item.Hue, item.Location));

            if (list.Count == 0)
            {
                from.SendMessage("Nothing to copy.");
                return;
            }

            TileEntry[] entries = list.ToArray();

            Point2D min = new Point2D(int.MaxValue, int.MaxValue);
            Point2D max = new Point2D(0, 0);
            int z = int.MaxValue;

            for (int i = 0; i < entries.Length; i++)
            {
                TileEntry e = entries[i];

                if (e.Offset.X < min.X)
                    min.X = e.Offset.X;

                if (e.Offset.Y < min.Y)
                    min.Y = e.Offset.Y;

                if (e.Offset.X > max.X)
                    max.X = e.Offset.X;

                if (e.Offset.Y > max.Y)
                    max.Y = e.Offset.Y;

                if (e.Offset.Z < z)
                    z = e.Offset.Z;
            }

            Point3D origin = new Point3D((min.X + max.X) / 2, (min.Y + max.Y) / 2, z);

            for (int i = 0; i < entries.Length; i++)
                entries[i].Offset -= origin;

            m_Clipboards[from] = entries;

            from.SendMessage("Copied {0} statics.", entries.Length);

            DoPaste(from);
        }

        [Usage("ClipPaste")]
        [Description("Pastes your ClipCopy clipboard to the targeted location.")]
        private static void ClipPaste_OnCommand(CommandEventArgs e)
        {
            DoPaste(e.Mobile);
        }

        private static void DoPaste(Mobile from)
        {
            if (!m_Clipboards.ContainsKey(from))
            {
                from.SendMessage("Nothing to paste.");
                return;
            }

            from.SendMessage("Target the location you wish to paste this to.");
            from.BeginTarget(-1, true, TargetFlags.None, DoPaste_OnTarget);
        }

        private static void DoPaste_OnTarget(Mobile from, object targeted)
        {
            Map map = from.Map;

            if (map == null || map == Map.Internal)
                return;

            IPoint3D p = targeted as IPoint3D;

            if (p == null)
                return;

            TileEntry[] clipboard;

            if (!m_Clipboards.TryGetValue(from, out clipboard) || clipboard.Length == 0)
            {
                from.SendMessage("Nothing to paste.");
                return;
            }

            Point3D origin;

            if (p is Item)
                origin = ((Item)p).GetWorldTop();
            else
                origin = new Point3D(p);

            for (int i = 0; i < clipboard.Length; i++)
            {
                TileEntry e = clipboard[i];

                Static stat = new Static(e.ItemID);

                stat.Hue = e.Hue;

                stat.MoveToWorld(origin + e.Offset, map);
            }

            from.SendMessage("Pasted {0} statics.", clipboard.Length);
        }

        private struct TileEntry
        {
            private int m_ItemID;
            private int m_Hue;
            private Point3D m_Offset;

            public int ItemID { get { return m_ItemID; } }
            public int Hue { get { return m_Hue; } }
            public Point3D Offset { get { return m_Offset; } set { m_Offset = value; } }

            public TileEntry(int itemID, int hue, Point3D offset)
            {
                m_ItemID = itemID;
                m_Hue = hue;
                m_Offset = offset;
            }
        }

        private class ItemComparer : IComparer<Item>
        {
            public static readonly ItemComparer Instance = new ItemComparer();

            private ItemComparer()
            {
            }

            public int Compare(Item a, Item b)
            {
                int result = a.Y.CompareTo(b.Y);

                if (result != 0)
                    return result;

                result = a.X.CompareTo(b.X);

                if (result != 0)
                    return result;

                result = a.Z.CompareTo(b.Z);

                if (result != 0)
                    return result;

                return a.Serial.CompareTo(b.Serial);
            }
        }
    }
}