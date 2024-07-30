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

/* Scripts\Items\Special\Holiday\Winter\HolidayFoods.cs
 * Changelog:
 *	12/6/23, Yoar
 *		Merge from RunUO
 */

namespace Server.Items
{
    [Furniture]
    [FlipableAttribute(0x2bd9, 0x2bda)]
    public class GreenStocking : BaseContainer
    {
        public override int DefaultGumpID { get { return 0x103; } }
        public override int DefaultDropSound { get { return 0x42; } }

        [Constructable]
        public GreenStocking() : base(Utility.Random(0x2BD9, 2))
        {
        }

        public GreenStocking(Serial serial) : base(serial)
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

    [Furniture]
    [FlipableAttribute(0x2bdb, 0x2bdc)]
    public class RedStocking : BaseContainer
    {
        public override int DefaultGumpID { get { return 0x103; } }
        public override int DefaultDropSound { get { return 0x42; } }

        [Constructable]
        public RedStocking() : base(Utility.Random(0x2BDB, 2))
        {
        }

        public RedStocking(Serial serial) : base(serial)
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