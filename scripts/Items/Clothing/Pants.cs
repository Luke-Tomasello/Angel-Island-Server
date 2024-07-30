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
    public abstract class BasePants : BaseClothing
    {
        public BasePants(int itemID)
            : this(itemID, 0)
        {
        }

        public BasePants(int itemID, int hue)
            : base(itemID, Layer.Pants, hue)
        {
        }

        public BasePants(Serial serial)
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

    [FlipableAttribute(0x152e, 0x152f)]
    public class ShortPants : BasePants
    {
        [Constructable]
        public ShortPants()
            : this(0)
        {
        }

        [Constructable]
        public ShortPants(int hue)
            : base(0x152E, hue)
        {
            Weight = 2.0;
        }

        public ShortPants(Serial serial)
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

    [FlipableAttribute(0x1539, 0x153a)]
    public class LongPants : BasePants
    {
        [Constructable]
        public LongPants()
            : this(0)
        {
        }

        [Constructable]
        public LongPants(int hue)
            : base(0x1539, hue)
        {
            Weight = 2.0;
        }

        public LongPants(Serial serial)
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