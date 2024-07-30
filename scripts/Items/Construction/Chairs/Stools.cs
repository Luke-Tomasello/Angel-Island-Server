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

/* Items/Construction/Chairs/Stools.cs
 * CHANGELOG:
 *  11/14/21, Yoar
 *		Inheritance of BaseCraftableItem for carpentry craftables.
 */

namespace Server.Items
{
    [Furniture]
    public class Stool : BaseCraftableItem
    {
        [Constructable]
        public Stool()
            : base(0xA2A)
        {
            Weight = 10.0;
        }

        public Stool(Serial serial)
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
                Weight = 10.0;
        }
    }

    [Furniture]
    public class FootStool : BaseCraftableItem
    {
        [Constructable]
        public FootStool()
            : base(0xB5E)
        {
            Weight = 6.0;
        }

        public FootStool(Serial serial)
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
                Weight = 10.0;
        }
    }
}