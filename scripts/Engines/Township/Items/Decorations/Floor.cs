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

/* Engines/Township/Items/Decorations/Floor.cs
 * CHANGELOG:
 * 3/17/22, Adam
 *  Update OnBuild to use new parm list
 * 1/17/22, Yoar
 *      Floor ItemIDs are now randomized on craft.
 * 11/23/21, Yoar
 *      Initial version.
*/

using System;
using System.Collections.Generic;

namespace Server.Township
{
    /// <summary>
    /// Township floors abide by different placement rules than regular township statics.
    /// </summary>
    public class TownshipFloor : TownshipStatic
    {
        private static readonly int[][] m_TileIDs = new int[][]
            {
                new int[] { 0x519, 0x51A, 0x51B, 0x51C }, // stone paver (light)
                new int[] { 0x51D, 0x51E, 0x51F, 0x520 }, // stone paver (overgrown)
                new int[] { 0x521, 0x522, 0x523, 0x524 }, // stone paver (dark)
                new int[] { 0x49D, 0x49E, 0x49F, 0x4A0 }, // sandstone paver
                new int[] { 0x525, 0x526, 0x527, 0x528 }, // sandstone floor (south)
                new int[] { 0x529, 0x52A, 0x52B, 0x52C }, // sandstone floor (east)
                new int[] { 0x52F, 0x530, 0x531, 0x532 }, // dark sandstone floor (south)
                new int[] { 0x533, 0x534, 0x535, 0x536 }, // dark sandstone floor (east)
                new int[] { 0x500, 0x501, 0x502, 0x503 }, // sandstone flagstones
                new int[] { 0x57F, 0x580, 0x581, 0x582, 0x583, 0x584, 0x585 }, // light flagstones
                new int[] { 0x4FC, 0x4FD, 0x4FE, 0x4FF }, // dark flagstones
                new int[] { 0x4E2, 0x4E3, 0x4E4, 0x4E5 }, // bricks
                new int[] { 0x4E6, 0x4E7, 0x539, 0x53A }, // bricks (south)
                new int[] { 0x4E8, 0x4E9, 0x537, 0x538 }, // bricks (east)
                new int[] { 0x50D, 0x50E }, // marble floor
                new int[] { 0x495, 0x496, 0x497, 0x498 }, // marble paver
                new int[] { 0x53B, 0x53C, 0x53D, 0x53E, 0x53F }, // cave floor
                new int[] { 0x31F4, 0x31F5, 0x31F6, 0x31F7 }, // dirt floor
                new int[] { 0x515, 0x516, 0x517, 0x518 }, // cobblestones
            };

        public static int[][] TileIDs { get { return m_TileIDs; } }

        [Constructable]
        public TownshipFloor(int itemID)
            : base(itemID)
        {
        }

        public override void OnBuild(Mobile from)
        {
            base.OnBuild(from);

            RandomizeItemID();
        }

        private void RandomizeItemID()
        {
            for (int i = 0; i < m_TileIDs.Length; i++)
            {
                int[] tileIDs = m_TileIDs[i];

                if (Array.IndexOf(tileIDs, this.ItemID) != -1)
                    this.ItemID = Utility.RandomList(tileIDs);
            }
        }

        public override void OnDelete()
        {
            // clean up any grass tiles that were placed on top of this floor
            RemoveGrass(Location, Map, this);

            base.OnDelete();
        }

        public static void RemoveGrass(Point3D loc, Map map, Item ignore)
        {
            if (map == null)
                return;

            List<Item> toDelete = null;

            foreach (Item item in map.GetItemsInRange(loc, 0))
            {
                if (item == null || item.Deleted || item == ignore)
                    continue;

                if (item.Z == loc.Z && Array.IndexOf(m_GrassIDs, item.ItemID) != -1 && item is TownshipStatic)
                {
                    if (toDelete == null)
                        toDelete = new List<Item>();

                    toDelete.Add(item);
                }
            }

            if (toDelete != null)
            {
                foreach (Item item in toDelete)
                    item.Delete();
            }
        }

        private static readonly int[] m_GrassIDs = new int[]
            {
                0x1782, 0x1783, 0x1784, 0x1785,
                0x1786, 0x1787, 0x1788, 0x1789,
            };

        public TownshipFloor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }
}