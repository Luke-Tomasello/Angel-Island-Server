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

/* Misc/ArtReader.cs
 * CHANGELOG:
 *    12/30/21, Yoar
 *        Initial version.
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Misc
{
    public static class ArtReader
    {
        private static readonly Dictionary<int, ArtEntry> m_Cache = new Dictionary<int, ArtEntry>();

        private static BinaryReader m_IndexReader;
        private static BinaryReader m_DataReader;

        public static ArtEntry GetEntry(int itemID)
        {
            ArtEntry ae;

            if (m_Cache.TryGetValue(itemID, out ae))
                return ae;

            return ReadEntry(itemID);
        }

        public static ArtEntry ReadEntry(int itemID, bool cache = true)
        {
            if (itemID < 0 || itemID >= TileData.ItemTable.Length)
                return ArtEntry.Empty;

            try
            {
                OpenReaders();

                itemID += 0x4000;
                itemID &= 0xFFFF;

                m_IndexReader.BaseStream.Seek(itemID * 12, SeekOrigin.Begin);

                int lookup = m_IndexReader.ReadInt32();
                int length = m_IndexReader.ReadInt32();

                m_IndexReader.ReadInt32(); // extra

                if (length == 0)
                    return ArtEntry.Empty;

                m_DataReader.BaseStream.Seek(lookup, SeekOrigin.Begin);

                m_DataReader.ReadInt32();

                int width = m_DataReader.ReadInt16();
                int height = m_DataReader.ReadInt16();

                ArtEntry ae = new ArtEntry(lookup, new Point2D(width, height));

                if (cache)
                    m_Cache[itemID] = ae;

                return ae;
            }
            catch (Exception e)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(e));
            }

            return ArtEntry.Empty;
        }

        /// <summary>
        /// Get image width and height.
        /// </summary>
        /// <param name="itemID"></param>
        /// <returns></returns>
        public static Point2D GetSize(int itemID)
        {
            ArtEntry ae = GetEntry(itemID);

            if (ae.IsEmpty())
                return Point2D.Zero;

            return ae.Size;
        }

        /// <summary>
        /// Get bounding rectangle of non-zero pixels in the image. These rectangles are stored in ItemBounds.bin (see <see cref="ItemBounds"/>).
        /// </summary>
        /// <param name="itemID"></param>
        /// <returns></returns>
        public static Rectangle2D GetBounds(int itemID)
        {
            ArtEntry ae = GetEntry(itemID);

            if (ae.IsEmpty())
                return new Rectangle2D(Point2D.Zero, Point2D.Zero);

            int xMin, xMax, yMin, yMax;

            xMin = yMin = int.MaxValue;
            xMax = yMax = int.MinValue;

            try
            {
                m_DataReader.BaseStream.Seek(ae.Lookup, SeekOrigin.Begin);

                m_DataReader.ReadInt32();
                m_DataReader.ReadInt16(); // width
                m_DataReader.ReadInt16(); // height

                int[] lookup = new int[ae.Size.Y];

                int start = (int)m_DataReader.BaseStream.Position + 2 * ae.Size.Y;

                for (int i = 0; i < ae.Size.Y; i++)
                    lookup[i] = start + 2 * m_DataReader.ReadUInt16();

                for (int i = 0; i < ae.Size.Y; i++)
                {
                    m_DataReader.BaseStream.Seek(lookup[i], SeekOrigin.Begin);

                    ushort off = m_DataReader.ReadUInt16();
                    ushort run = m_DataReader.ReadUInt16();

                    if (run == 0)
                        continue;

                    xMin = Math.Min(xMin, off);
                    yMin = Math.Min(yMin, i);
                    yMax = Math.Max(yMax, i);

                    int x = 0;

                    while (run != 0)
                    {
                        x += off + run;

                        m_DataReader.BaseStream.Seek(2 * run, SeekOrigin.Current);

                        off = m_DataReader.ReadUInt16();
                        run = m_DataReader.ReadUInt16();
                    }

                    xMax = Math.Max(xMax, x);
                }
            }
            catch (Exception e)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(e));
            }

            if (xMin == int.MaxValue)
                return new Rectangle2D(Point2D.Zero, Point2D.Zero);

            return new Rectangle2D(new Point2D(xMin, yMin), new Point2D(xMax, yMax));
        }

        private static void OpenReaders()
        {
            if (m_IndexReader == null)
                m_IndexReader = new BinaryReader(new FileStream(Core.FindDataFile("Artidx.mul"), FileMode.Open, FileAccess.Read, FileShare.Read));

            if (m_DataReader == null)
                m_DataReader = new BinaryReader(new FileStream(Core.FindDataFile("Art.mul"), FileMode.Open, FileAccess.Read, FileShare.Read));
        }
    }

    public struct ArtEntry
    {
        public static readonly ArtEntry Empty = new ArtEntry();

        private int m_Lookup;
        private Point2D m_Size;

        public int Lookup { get { return m_Lookup; } }
        public Point2D Size { get { return m_Size; } }

        public ArtEntry(int lookup, Point2D size)
        {
            m_Lookup = lookup;
            m_Size = size;
        }

        public bool IsEmpty()
        {
            return (m_Size.X == 0 || m_Size.Y == 0);
        }
    }
}