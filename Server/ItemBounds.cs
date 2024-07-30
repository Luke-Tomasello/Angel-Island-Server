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

/* Server\ItemBounds.cs
 * ChangeLog:
 *  7/4/2024, Adam
 *		Upgrade ItemBounds and Date/Binary/Bounds.bin modules to RunUO 2.6
 */


using System;
using System.IO;

namespace Server
{
    // Upgrade ItemBounds and Date/Binary/Bounds.bin modules to RunUO 2.6
    public static class ItemBounds
    {
        private static Rectangle2D[] m_Bounds;

        public static Rectangle2D[] Table
        {
            get
            {
                return m_Bounds;
            }
        }

        static ItemBounds()
        {
            m_Bounds = new Rectangle2D[TileData.ItemTable.Length];

            if (File.Exists(Path.Combine(Core.DataDirectory, "Binary", "Bounds.bin")))
            {
                using (FileStream fs = new FileStream(Path.Combine(Core.DataDirectory, "Binary", "Bounds.bin"), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    BinaryReader bin = new BinaryReader(fs);

                    int count = Math.Min(m_Bounds.Length, (int)(fs.Length / 8));

                    for (int i = 0; i < count; ++i)
                    {
                        int xMin = bin.ReadInt16();
                        int yMin = bin.ReadInt16();
                        int xMax = bin.ReadInt16();
                        int yMax = bin.ReadInt16();

                        m_Bounds[i].Set(xMin, yMin, (xMax - xMin) + 1, (yMax - yMin) + 1);
                    }

                    bin.Close();
                }
            }
            else
            {
                Console.WriteLine($"Warning: {Path.Combine(Core.DataDirectory, "Binary", "Bounds.bin")} does not exist");
            }
        }
    }
#if false
    public class ItemBounds
    {
        private static Rectangle2D[] m_Bounds;

        public static Rectangle2D[] Table
        {
            get
            {
                return m_Bounds;
            }
        }

        static ItemBounds()
        {
            if (File.Exists(Path.Combine(Core.DataDirectory, "Binary", "Bounds.bin")))
            {
                using (FileStream fs = new FileStream(Path.Combine(Core.DataDirectory, "Binary", "Bounds.bin"), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    BinaryReader bin = new BinaryReader(fs);

                    m_Bounds = new Rectangle2D[0x4000];

                    for (int i = 0; i < 0x4000; ++i)
                    {
                        int xMin = bin.ReadInt16();
                        int yMin = bin.ReadInt16();
                        int xMax = bin.ReadInt16();
                        int yMax = bin.ReadInt16();

                        m_Bounds[i].Set(xMin, yMin, (xMax - xMin) + 1, (yMax - yMin) + 1);
                    }

                    bin.Close();
                }
            }
            else
            {
                Console.WriteLine("Warning: {0} does not exist", Path.Combine(Core.DataDirectory, "Binary", "Bounds.bin"));

                m_Bounds = new Rectangle2D[0x4000];
            }
        }
    }
#endif
}