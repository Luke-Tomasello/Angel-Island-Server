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

/* Items/RabbitFur.cs
 * ChangeLog:
 *	3/26/05, Adam
 *		First time checkin
 */

namespace Server.Items
{
    public class RabbitFur1 : Item
    {
        [Constructable]
        public RabbitFur1()
            : base(0x11F4)
        {
            Name = "rabbit fur";
            Weight = 1.0;
        }

        public RabbitFur1(Serial serial)
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

    public class RabbitFur2 : Item
    {
        [Constructable]
        public RabbitFur2()
            : base(0x11F5)
        {
            Name = "rabbit fur";
            Weight = 1.0;
        }

        public RabbitFur2(Serial serial)
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

    public class RabbitFur3 : Item
    {
        [Constructable]
        public RabbitFur3()
            : base(0x11F6)
        {
            Name = "rabbit fur";
            Weight = 1.0;
        }

        public RabbitFur3(Serial serial)
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

    public class RabbitFur4 : Item
    {
        [Constructable]
        public RabbitFur4()
            : base(0x11F7)
        {
            Name = "rabbit fur";
            Weight = 1.0;
        }

        public RabbitFur4(Serial serial)
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

    public class RabbitFur5 : Item
    {
        [Constructable]
        public RabbitFur5()
            : base(0x11F8)
        {
            Name = "rabbit fur";
            Weight = 1.0;
        }

        public RabbitFur5(Serial serial)
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

    public class RabbitFur6 : Item
    {
        [Constructable]
        public RabbitFur6()
            : base(0x11F9)
        {
            Name = "rabbit fur";
            Weight = 1.0;
        }

        public RabbitFur6(Serial serial)
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

    public class RabbitFur7 : Item
    {
        [Constructable]
        public RabbitFur7()
            : base(0x11FA)
        {
            Name = "rabbit fur";
            Weight = 1.0;
        }

        public RabbitFur7(Serial serial)
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

    public class RabbitFur8 : Item
    {
        [Constructable]
        public RabbitFur8()
            : base(0x11FB)
        {
            Name = "rabbit fur";
            Weight = 1.0;
        }

        public RabbitFur8(Serial serial)
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