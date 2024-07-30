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
    public class LargePainting : Item
    {
        [Constructable]
        public LargePainting()
            : base(0x0EA0)
        {
            Movable = false;
        }

        public LargePainting(Serial serial)
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

    [FlipableAttribute(0x0E9F, 0x0EC8)]
    public class WomanPortrait1 : Item
    {
        [Constructable]
        public WomanPortrait1()
            : base(0x0E9F)
        {
            Movable = false;
        }

        public WomanPortrait1(Serial serial)
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

    [FlipableAttribute(0x0EE7, 0x0EC9)]
    public class WomanPortrait2 : Item
    {
        [Constructable]
        public WomanPortrait2()
            : base(0x0EE7)
        {
            Movable = false;
        }

        public WomanPortrait2(Serial serial)
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

    [FlipableAttribute(0x0EA2, 0x0EA1)]
    public class ManPortrait1 : Item
    {
        [Constructable]
        public ManPortrait1()
            : base(0x0EA2)
        {
            Movable = false;
        }

        public ManPortrait1(Serial serial)
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

    [FlipableAttribute(0x0EA3, 0x0EA4)]
    public class ManPortrait2 : Item
    {
        [Constructable]
        public ManPortrait2()
            : base(0x0EA3)
        {
            Movable = false;
        }

        public ManPortrait2(Serial serial)
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

    [FlipableAttribute(0x0EA6, 0x0EA5)]
    public class LadyPortrait1 : Item
    {
        [Constructable]
        public LadyPortrait1()
            : base(0x0EA6)
        {
            Movable = false;
        }

        public LadyPortrait1(Serial serial)
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

    [FlipableAttribute(0x0EA7, 0x0EA8)]
    public class LadyPortrait2 : Item
    {
        [Constructable]
        public LadyPortrait2()
            : base(0x0EA7)
        {
            Movable = false;
        }

        public LadyPortrait2(Serial serial)
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