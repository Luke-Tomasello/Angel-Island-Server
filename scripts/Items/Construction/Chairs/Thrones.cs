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

/* Scripts/Items/Construction/Chairs/Thrones.cs
 * ChangeLog
 *  11/14/21, Yoar
 *		Inheritance of BaseCraftableItem for carpentry craftables.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Items
{
    [Furniture]
    [Flipable(0xB32, 0xB33)]
    public class Throne : BaseCraftableItem
    {
        [Constructable]
        public Throne()
            : base(0xB33)
        {
            Weight = 1.0;
        }

        public Throne(Serial serial)
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

            if (Weight == 6.0)
                Weight = 1.0;
        }
    }

    [Furniture]
    [Flipable(0xB2E, 0xB2F, 0xB31, 0xB30)]
    public class WoodenThrone : BaseCraftableItem
    {
        [Constructable]
        public WoodenThrone()
            : base(0xB2E)
        {
            Weight = 15.0;
        }

        public WoodenThrone(Serial serial)
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

            if (Weight == 6.0)
                Weight = 15.0;
        }
    }
}