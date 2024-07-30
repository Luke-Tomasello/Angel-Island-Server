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

/* Scripts\Engines\CrownSterlingSystem\Items\Sterling.cs
 * ChangeLog
 *  6/20/2024, Adam
 *      created
 */

namespace Server.Engines.CrownSterlingSystem
{
    public class Sterling : Item
    {
        public override string DefaultName { get { return "sterling"; } }

        [Constructable]
        public Sterling()
            : this(1)
        {
        }

        [Constructable]
        public Sterling(int amountFrom, int amountTo)
            : this(Utility.Random(amountFrom, amountTo - amountFrom))
        {
        }

        [Constructable]
        public Sterling(int amount)
            : base(0xEF0)
        {
            Stackable = true;
            Weight = 0.02;
            Amount = amount;
            LootType = LootType.Regular;
        }

        public Sterling(Serial serial)
            : base(serial)
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
            return base.Dupe(new Sterling(amount), amount);
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