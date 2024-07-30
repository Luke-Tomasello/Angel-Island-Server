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
    public class BagOfingots : Bag
    {
        [Constructable]
        public BagOfingots()
            : this(5000)
        {
        }

        [Constructable]
        public BagOfingots(int amount)
        {
            DropItem(new DullCopperIngot(amount));
            DropItem(new ShadowIronIngot(amount));
            DropItem(new CopperIngot(amount));
            DropItem(new BronzeIngot(amount));
            DropItem(new GoldIngot(amount));
            DropItem(new AgapiteIngot(amount));
            DropItem(new VeriteIngot(amount));
            DropItem(new ValoriteIngot(amount));
            DropItem(new IronIngot(amount));
            DropItem(new Tongs());
            DropItem(new TinkerTools());

        }

        public BagOfingots(Serial serial)
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