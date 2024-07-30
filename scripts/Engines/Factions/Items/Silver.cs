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

/* Scripts\Engines\Factions\Items\Silver.cs
 * ChangeLog
 *  3/21/11, Adam
 *		- Added Dupe() override to ensure Silver is properly duplicated (needed for stacking)
 *		- first time checkin
 */

namespace Server.Factions
{
    public class Silver : Item
    {
        public override double DefaultWeight
        {
            get { return 0.02; }
        }

        [Constructable]
        public Silver() : this(1)
        {
        }

        [Constructable]
        public Silver(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructable]
        public Silver(int amount) : base(0xEF0)
        {
            Stackable = true;
            Amount = amount;
        }

        public Silver(Serial serial) : base(serial)
        {
        }

        public override int GetDropSound()
        {
            if (Amount <= 1)
                return 0x2E4;
            else if (Amount <= 5)
                return 0x2E5;
            else
                return 0x2E6;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Silver(amount), amount);
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