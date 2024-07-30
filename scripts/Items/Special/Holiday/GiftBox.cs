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

/* Scripts\Items\Special\Holiday\GiftBox.cs
 * Changelog:
 *	12/11/05, Adam
 *		Add copyright
 */

namespace Server.Items
{
    [Furniture]
    [Flipable(0x232A, 0x232B)]
    public class GiftBox : BaseContainer
    {
        public override int DefaultGumpID { get { return 0x102; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(35, 10, 155, 85); }
        }

        [Constructable]
        public GiftBox()
            : this(Utility.RandomDyedHue())
        {
        }

        [Constructable]
        public GiftBox(int hue)
            : base(Utility.Random(0x232A, 2))
        {
            Weight = 2.0;
            Hue = hue;
        }

        public GiftBox(Serial serial)
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