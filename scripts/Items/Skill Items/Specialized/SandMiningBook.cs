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

using Server.Mobiles;

namespace Server.Items
{
    public class SandMiningBook : Item
    {
        [Constructable]
        public SandMiningBook()
            : base(0xFF4)
        {
            Name = "Find Glass-Quality Sand";
            Weight = 1.0;
        }

        public SandMiningBook(Serial serial)
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

        public override void OnDoubleClick(Mobile from)
        {
            PlayerMobile pm = from as PlayerMobile;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (pm == null || from.Skills[SkillName.Mining].Base < 100.0)
            {
                pm.SendMessage("Only a Grandmaster Miner can learn from this book.");
            }
            else if (pm.SandMining)
            {
                pm.SendMessage("You have already learned this information.");
            }
            else
            {
                pm.SandMining = true;
                pm.SendMessage("You have learned how to mine fine sand. Target sand areas when mining to look for fine sand.");
                Delete();
            }
        }
    }
}