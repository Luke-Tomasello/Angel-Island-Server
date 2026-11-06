/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* MarineRope.cs
 * CHANGELOG:
 *  5/15/2026, Adam
 */
namespace Server.Items
{
    [Flipable(0x14F8, 0x14FA)]
    public class MarineRope : Item
    {
        [Constructable]
        public MarineRope()
            : this(1)
        {
        }

        [Constructable]
        public MarineRope(int amount)
            : base(0x14F8)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
            Hue = 0x33;

            Name = "marine rope";
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new MarineRope(amount), amount);
        }

        public MarineRope(Serial serial)
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