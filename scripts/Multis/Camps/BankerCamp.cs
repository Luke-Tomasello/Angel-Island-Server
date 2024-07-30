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

using Server.Items;
using Server.Mobiles;

namespace Server.Multis
{
    public class BankerCamp : BaseCamp
    {
        [Constructable]
        public BankerCamp()
            : base(0x1F6)
        {
        }

        public override void AddComponents()
        {
            BaseDoor west, east;

            AddItem(west = new LightWoodGate(DoorFacing.WestCW), -4, 4, 7);
            AddItem(east = new LightWoodGate(DoorFacing.EastCCW), -3, 4, 7);

            west.Link = east;
            east.Link = west;

            AddItem(new Sign(SignType.Bank, SignFacing.West), -5, 5, -4);

            AddMobile(new Banker(), 4, -4, 3, 7);
            AddMobile(new Banker(), 5, 4, -2, 0);
        }

        public BankerCamp(Serial serial)
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
}