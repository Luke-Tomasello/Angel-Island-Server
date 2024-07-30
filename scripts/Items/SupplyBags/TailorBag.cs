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
    public class TailorBag : Bag
    {
        [Constructable]
        public TailorBag()
            : this(1)
        {
            Movable = true;
            Hue = 0x315;
            Name = "a Tailoring Kit";
        }
        [Constructable]
        public TailorBag(int amount)
        {
            DropItem(new SewingKit(5));
            DropItem(new Scissors());
            DropItem(new Hides(500));
            DropItem(new BoltOfCloth(20));
            DropItem(new DyeTub());
            DropItem(new DyeTub());
            DropItem(new BlackDyeTub());
            DropItem(new Dyes());
        }


        public TailorBag(Serial serial)
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