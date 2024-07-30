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

/* Scripts\Engines\Township\Items\Fortifications\HedgeRows.cs
 * CHANGELOG:
 * 3/10/22, Adam
 *	    Initial creation
 */

namespace Server.Township
{
    public class BoxwoodHedge : FortificationWall
    {
        public const int Boxwood = 0x0C90;
        public const int BoxwoodUntrimmed = 0x0C92;
        public const int Holly = 0x0C8F;
        public const int HollyUntrimmed = 0x0C91;
        public override string DefaultName { get { return "boxwood"; } }
        [Constructable]
        public BoxwoodHedge()
            : this(Utility.RandomList(Boxwood, Holly))
        {
        }

        [Constructable]
        public BoxwoodHedge(int itemID)
            : base(itemID)
        {
            Name = DefaultName;
        }

        public BoxwoodHedge(Serial serial)
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

    public class HollyHedge : FortificationWall
    {

        public const int Holly = 0x0C8F;
        public const int HollyUntrimmed = 0x0C91;
        public override string DefaultName { get { return "holly"; } }

        [Constructable]
        public HollyHedge()
            : this(Holly)
        {
        }

        [Constructable]
        public HollyHedge(int itemID)
            : base(itemID)
        {
            Name = DefaultName;
        }

        public HollyHedge(Serial serial)
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

    public class Holly : Item
    {
        public const int YoungHolly = 0xCB0;
        public override string DefaultName { get { return "holly"; } }
        [Constructable]
        public Holly()
            : this(YoungHolly)
        {
        }

        [Constructable]
        public Holly(int itemID)
            : base(itemID)
        {
            Name = DefaultName;
            Weight = 1;
        }

        public Holly(Serial serial)
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

    public class Boxwood : Item
    {
        public const int YoungBoxwood = 0xCB4;
        public override string DefaultName { get { return "boxwood"; } }
        [Constructable]
        public Boxwood()
            : this(YoungBoxwood)
        {
        }

        [Constructable]
        public Boxwood(int itemID)
            : base(itemID)
        {
            Name = DefaultName;
            Weight = 1;
        }

        public Boxwood(Serial serial)
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