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
    public class CityMap : MapItem
    {
        [Constructable]
        public CityMap()
        {
            SetDisplay(0, 0, 5119, 4095, 400, 400);
        }

        public override void CraftInit(Mobile from)
        {
            double skillValue = from.Skills[SkillName.Cartography].Value;
            int dist = 64 + (int)(skillValue * 4);

            if (dist < 200)
                dist = 200;

            int size = 32 + (int)(skillValue * 2);

            if (size < 200)
                size = 200;
            else if (size > 400)
                size = 400;

            SetDisplay(from.X - dist, from.Y - dist, from.X + dist, from.Y + dist, size, size);
        }

        public override int LabelNumber { get { return 1015231; } } // city map

        public CityMap(Serial serial)
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