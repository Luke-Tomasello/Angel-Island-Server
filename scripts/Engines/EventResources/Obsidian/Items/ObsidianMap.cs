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

/* Server/Engines/EventResources/Obsidian/Items/ObsidianMap.cs
 * CHANGELOG:
 *  10/18/23, Yoar
 *      Initial version.
 */

using Server.Engines.Harvest;

namespace Server.Items
{
    [TypeAlias("Server.Engines.Obsidian.ObsidianMap")]
    public class ObsidianMap : ResourceMap
    {
        public override HarvestDefinition HarvestDefinition { get { return Mining.System.OreAndStone; } }

        [Constructable]
        public ObsidianMap()
            : this(50)
        {
        }

        [Constructable]
        public ObsidianMap(int uses)
            : base(CraftResource.Obsidian, uses, Map.Felucca)
        {
        }

        protected override Point2D GetRandomLocation()
        {
            if (m_Locations.Length == 0)
                return Point2D.Zero;

            Point2D loc = m_Locations[Utility.Random(m_Locations.Length)];

            return new Point2D(loc.X / 8, loc.Y / 8);
        }

        public ObsidianMap(Serial serial)
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

        private static readonly Point2D[] m_Locations = new Point2D[]
            {
                #region Minoc
                new Point2D(2568, 368),
                new Point2D(2480, 384),
                new Point2D(2552, 384),
                new Point2D(2560, 384),
                new Point2D(2568, 384),
                new Point2D(2448, 392),
                new Point2D(2480, 392),
                new Point2D(2544, 392),
                new Point2D(2552, 392),
                new Point2D(2560, 392),
                new Point2D(2568, 392),
                new Point2D(2576, 392),
                new Point2D(2424, 400),
                new Point2D(2544, 400),
                new Point2D(2552, 400),
                new Point2D(2568, 400),
                new Point2D(2576, 400),
                new Point2D(2544, 408),
                new Point2D(2576, 408),
                new Point2D(2584, 408),
                new Point2D(2544, 416),
                new Point2D(2552, 416),
                new Point2D(2584, 416),
                new Point2D(2592, 416),
                new Point2D(2552, 424),
                new Point2D(2592, 424),
                new Point2D(2600, 424),
                new Point2D(2608, 424),
                new Point2D(2552, 432),
                new Point2D(2560, 432),
                new Point2D(2608, 432),
                new Point2D(2616, 432),
                new Point2D(2552, 440),
                new Point2D(2560, 440),
                new Point2D(2616, 440),
                new Point2D(2552, 448),
                new Point2D(2560, 448),
                new Point2D(2592, 448),
                new Point2D(2600, 448),
                new Point2D(2608, 448),
                new Point2D(2616, 448),
                new Point2D(2416, 456),
                new Point2D(2424, 456),
                new Point2D(2552, 456),
                new Point2D(2560, 456),
                new Point2D(2576, 456),
                new Point2D(2584, 456),
                new Point2D(2592, 456),
                new Point2D(2600, 456),
                new Point2D(2608, 456),
                new Point2D(2408, 464),
                new Point2D(2552, 464),
                new Point2D(2576, 464),
                new Point2D(2584, 464),
                new Point2D(2592, 464),
                new Point2D(2600, 464),
                new Point2D(2608, 464),
                new Point2D(2616, 464),
                new Point2D(2544, 472),
                new Point2D(2552, 472),
                new Point2D(2560, 472),
                new Point2D(2568, 472),
                new Point2D(2576, 472),
                new Point2D(2584, 472),
                new Point2D(2592, 472),
                new Point2D(2600, 472),
                new Point2D(2608, 472),
                new Point2D(2552, 480),
                new Point2D(2560, 480),
                new Point2D(2568, 480),
                new Point2D(2576, 480),
                new Point2D(2584, 480),
                new Point2D(2592, 480),
                new Point2D(2600, 480),
                new Point2D(2608, 480),
                new Point2D(2552, 488),
                new Point2D(2560, 488),
                new Point2D(2568, 488),
                new Point2D(2576, 488),
                new Point2D(2584, 488),
                new Point2D(2592, 488),
                new Point2D(2600, 488),
                new Point2D(2608, 488),
                new Point2D(2504, 496),
                new Point2D(2552, 496),
                new Point2D(2560, 496),
                new Point2D(2568, 496),
                new Point2D(2576, 496),
                new Point2D(2584, 496),
                new Point2D(2592, 496),
                new Point2D(2600, 496),
                new Point2D(2608, 496),
                new Point2D(2504, 504),
                new Point2D(2512, 504),
                new Point2D(2568, 504),
                new Point2D(2576, 504),
                new Point2D(2584, 504),
                new Point2D(2408, 512),
                new Point2D(2440, 512),
                new Point2D(2496, 520),
                new Point2D(2496, 528),
                new Point2D(2440, 536),
                new Point2D(2432, 544),
                new Point2D(2440, 544),
                new Point2D(2456, 552),
                new Point2D(2488, 552),
                new Point2D(2488, 560),
                new Point2D(2496, 560),
                new Point2D(2544, 568),
                new Point2D(2544, 592),
                #endregion
            };
    }
}