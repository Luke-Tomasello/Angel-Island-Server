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

/* Scripts/Items/Special/Holiday/Halloween/Spiderweb.cs
 * CHANGELOG
 *  10/4/23, Yoar
 *      Initial commit.
 */

namespace Server.Items
{
    [Flipable(
        0xEE3, 0xEE4, 0xEE3,
        0xEE5, 0xEE6, 0xEE5)]
    public class Spiderweb : Item
    {
        public override double DefaultWeight { get { return 1.0; } }

        [Constructable]
        public Spiderweb()
            : this(Utility.Random(0xEE3, 4))
        {
        }

        [Constructable]
        public Spiderweb(int itemID)
            : base(itemID)
        {
        }

        public Spiderweb(Serial serial)
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