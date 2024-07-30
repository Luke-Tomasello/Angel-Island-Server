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

/* Scripts/Items/Construction/Decorative/Bookcase.cs
 * CHANGELOG:
 *	06/04/05 : Pixie
 *		Initial Version
 */

namespace Server.Items
{
    [Furniture]
    [Flipable(0xA97, 0xA99)]
    public class Bookcase : Item
    {
        [Constructable]
        public Bookcase()
            : base(0xA97)
        {
            Weight = 15.0;
        }
        public Bookcase(Serial serial)
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

    [Furniture]
    [Flipable(0xA98, 0xA9A)]
    public class Bookcase2 : Item
    {
        [Constructable]
        public Bookcase2()
            : base(0xA98)
        {
            Weight = 15.0;
        }
        public Bookcase2(Serial serial)
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

    [Furniture]
    [Flipable(0xA9B, 0xA9C)]
    public class Bookcase3 : Item
    {
        [Constructable]
        public Bookcase3()
            : base(0xA9C)
        {
            Weight = 15.0;
        }
        public Bookcase3(Serial serial)
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