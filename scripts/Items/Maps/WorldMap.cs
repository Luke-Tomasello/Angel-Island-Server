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

namespace Server.Items
{
    public class WorldMap : MapItem
    {
        [Constructable]
        public WorldMap()
        {
            SetDisplay(0, 0, 5119, 4095, 400, 400);
        }

        public override void CraftInit(Mobile from)
        {
            // Unlike the others, world map is not based on crafted location

            double skillValue = from.Skills[SkillName.Cartography].Value;
            int x20 = (int)(skillValue * 20);
            int size = 25 + (int)(skillValue * 6.6);

            if (size < 200)
                size = 200;
            else if (size > 400)
                size = 400;

            SetDisplay(1344 - x20, 1600 - x20, 1472 + x20, 1728 + x20, size, size);
        }

        public override int LabelNumber { get { return 1015233; } } // world map

        public WorldMap(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}