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

/* Items/Construction/Misc/MusicStand.cs
 * ChangeLog:
 *  11/14/21, Yoar
 *		Inheritance of BaseCraftableItem for carpentry craftables.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Items
{
    [Furniture]
    [Flipable(0xEBB, 0xEBC)]
    public class TallMusicStand : BaseCraftableItem
    {
        [Constructable]
        public TallMusicStand()
            : base(0xEBB)
        {
            Weight = 10.0;
        }

        public TallMusicStand(Serial serial)
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

            if (Weight == 8.0)
                Weight = 10.0;
        }
    }

    [Furniture]
    [Flipable(0xEB6, 0xEB8)]
    public class ShortMusicStand : BaseCraftableItem
    {
        [Constructable]
        public ShortMusicStand()
            : base(0xEB6)
        {
            Weight = 10.0;
        }

        public ShortMusicStand(Serial serial)
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