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

/* Misc/ItemPicture.cs
 * CHANGELOG:
 *	12/30/21, Yoar
 *	    Initial version.
 */

using Server.Gumps;
using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Misc
{
    public class ItemPicture
    {
        private InternalEntry[] m_Entries;
        private Rectangle2D m_Bounds;

        public Rectangle2D Bounds
        {
            get
            {
                if (m_Bounds.End == Point2D.Zero && m_Entries.Length != 0)
                    CalculateDimensions();

                return m_Bounds;
            }
        }

        public ItemPicture()
            : this(new InternalEntry[0])
        {
        }

        public ItemPicture(int itemID)
            : this(new InternalEntry[] { new InternalEntry(Point3D.Zero, itemID, 0) })
        {
        }

        public ItemPicture(int itemID, int hue)
            : this(new InternalEntry[] { new InternalEntry(Point3D.Zero, itemID, hue) })
        {
        }

        private ItemPicture(InternalEntry[] entries)
        {
            m_Entries = entries;
        }

        public void CompileCentered(Gump g, int x, int y, int width, int height)
        {
            Rectangle2D bounds = this.Bounds;

            Compile(g, x + (width - bounds.Width) / 2, y + (height - bounds.Height) / 2);
        }

        public void Compile(Gump g, int x, int y)
        {
            Rectangle2D bounds = this.Bounds;

            for (int i = 0; i < m_Entries.Length; i++)
            {
                InternalEntry entry = m_Entries[i];

                Point2D p = CalculateBitmapLocation(entry.ItemID, entry.Offset);

                g.AddItem(x - bounds.X + p.X, y - bounds.Y + p.Y, entry.ItemID);
            }
        }

        private void CalculateDimensions()
        {
            int xMin, xMax, yMin, yMax;

            xMin = yMin = int.MaxValue;
            xMax = yMax = int.MinValue;

            for (int i = 0; i < m_Entries.Length; i++)
            {
                InternalEntry entry = m_Entries[i];

                Rectangle2D r = CalculateBitmapRectangle(entry.ItemID, entry.Offset);

                xMin = Math.Min(xMin, r.X);
                yMin = Math.Min(yMin, r.Y);
                xMax = Math.Max(xMax, r.X + r.Width);
                yMax = Math.Max(yMax, r.Y + r.Height);
            }

            m_Bounds = new Rectangle2D(new Point2D(xMin, yMin), new Point2D(xMax, yMax));
        }

        private const int m_DeltaXY = 22; // pixel offset by x, y unit
        private const int m_DeltaZ = 4; // pixel offset by z unit

        /// <summary>
        /// Calculates, in bitmap coordinates, the location of the graphic corresponding to <paramref name="itemID"/> placed at in-game coordinates <paramref name="offset"/>.
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static Point2D CalculateBitmapLocation(int itemID, Point3D offset)
        {
            Rectangle2D rect = CalculateBitmapRectangle(itemID, offset);

            return rect.Start;
        }

        /// <summary>
        /// Calculates, in bitmap coordinates, the bounding rectangle of the graphic corresponding to <paramref name="itemID"/> placed at in-game coordinates <paramref name="offset"/>.
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static Rectangle2D CalculateBitmapRectangle(int itemID, Point3D offset)
        {
            Point2D size = ArtReader.GetSize(itemID);

            int px = m_DeltaXY * (offset.X - offset.Y) + (45 - size.X) / 2;
            int py = m_DeltaXY * (offset.X + offset.Y) - m_DeltaZ * offset.Z + 44 - size.Y;

            return new Rectangle2D(px, py, size.X, size.Y);
        }

        public static ItemPicture FromArray(params int[] array)
        {
            int count = array.Length / 5;

            InternalEntry[] entries = new InternalEntry[count];

            for (int i = 0; i < count; i++)
            {
                int index = 5 * i;

                entries[i] = new InternalEntry(new Point3D(array[index], (byte)array[index + 1], (byte)array[index + 2]), array[index + 3], array[index + 4]);
            }

            Array.Sort(entries, EntryComparer.Instance);

            return new ItemPicture(entries);
        }

        public static ItemPicture FromItem(Item item)
        {
            if (item is BaseAddon)
            {
                BaseAddon addon = (BaseAddon)item;

                InternalEntry[] entries = new InternalEntry[addon.Components.Count];

                for (int i = 0; i < addon.Components.Count; i++)
                {
                    AddonComponent c = (AddonComponent)addon.Components[i];

                    entries[i] = new InternalEntry(c.Offset, c.ItemID, c.Hue);
                }

                Array.Sort(entries, EntryComparer.Instance);

                return new ItemPicture(entries);
            }

            return new ItemPicture(item.ItemID, item.Hue);
        }

        private class EntryComparer : IComparer<InternalEntry>
        {
            public static readonly EntryComparer Instance = new EntryComparer();

            public EntryComparer()
            {
            }

            public int Compare(InternalEntry a, InternalEntry b)
            {
                if (a.Offset.Z > b.Offset.Z)
                    return +1;

                if (a.Offset.Z < b.Offset.Z)
                    return -1;

                int delta = b.Offset.X - a.Offset.X + b.Offset.Y - a.Offset.Y;

                if (a.Offset.X < b.Offset.X)
                    return (delta <= 0 ? +1 : -1);
                else
                    return (delta < 0 ? +1 : -1);
            }
        }

        private struct InternalEntry
        {
            private Point3D m_Offset;
            private int m_ItemID;
            private int m_Hue;

            public Point3D Offset { get { return m_Offset; } }
            public int ItemID { get { return m_ItemID; } }
            public int Hue { get { return m_Hue; } }

            public InternalEntry(Point3D offset, int itemID, int hue)
            {
                m_Offset = offset;
                m_ItemID = itemID;
                m_Hue = hue;
            }
        }
    }
}