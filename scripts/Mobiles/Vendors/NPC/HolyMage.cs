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

/* Scripts/Mobiles/Vendors/NPC/HolyMage.cs
 * ChangeLog
 *  10/18/04, Froste
 *      Modified Restock to use OnRestock() because it's fixed now
 *	4/29/04, mith
 *		Modified Restock to use OnRestockReagents() to restock 100 of each item instead of only 20.
 */

using Server.Items;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class HolyMage : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        [Constructable]
        public HolyMage()
            : base("the Holy Mage")
        {
            SetSkill(SkillName.EvalInt, 65.0, 88.0);
            SetSkill(SkillName.Inscribe, 60.0, 83.0);
            SetSkill(SkillName.Magery, 64.0, 100.0);
            SetSkill(SkillName.Meditation, 60.0, 83.0);
            SetSkill(SkillName.MagicResist, 65.0, 88.0);
            SetSkill(SkillName.Wrestling, 36.0, 68.0);
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBHolyMage());
        }

        public Item ApplyHue(Item item, int hue)
        {
            item.Hue = hue;

            return item;
        }

        public override void InitOutfit()
        {
            AddItem(ApplyHue(new Robe(), 0x47E));
            AddItem(ApplyHue(new ThighBoots(), 0x47E));
            AddItem(ApplyHue(new BlackStaff(), 0x47E));

            if (Female)
            {
                AddItem(ApplyHue(new LeatherGloves(), 0x47E));
                AddItem(ApplyHue(new GoldNecklace(), 0x47E));
            }
            else
            {
                AddItem(ApplyHue(new PlateGloves(), 0x47E));
                AddItem(ApplyHue(new PlateGorget(), 0x47E));
            }

            switch (Utility.Random(Female ? 2 : 1))
            {
                case 0: AddItem(ApplyHue(new LongHair(), 0x47E)); break;
                case 1: AddItem(ApplyHue(new PonyTail(), 0x47E)); break;
            }

            PackGold(100, 200);
        }

        public override void Restock()
        {
            base.LastRestock = DateTime.UtcNow;

            IBuyItemInfo[] buyInfo = this.GetBuyInfo();

            foreach (IBuyItemInfo bii in buyInfo)
                bii.OnRestock(); // change bii.OnRestockReagents() to OnRestock()
        }

        public HolyMage(Serial serial)
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