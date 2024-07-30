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

/*	Scripts/Mobiles/Vendors/NPC/Blacksmith.cs
 *	CHANGELOG
 * 10/14/21, Yoar
 *      Bulk Order System overhaul:
 *      - Re-enabled support for bulk orders.
 *      - The bulk order system remains disabled unless BulkOrderSystem.Enabled returns true.
 * 	03/29/2004 - Pulse
 * 		Removed ability for this NPC vendor to support Bulk Order Deeds
 * 		Blacksmiths will no longer issue or accept these deeds.
 */

using Server.Engines.BulkOrders;
using System.Collections;

namespace Server.Mobiles
{
    public class Blacksmith : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        public override NpcGuild NpcGuild { get { return NpcGuild.BlacksmithsGuild; } }

        [Constructable]
        public Blacksmith()
            : base("the blacksmith")
        {
            SetSkill(SkillName.ArmsLore, 36.0, 68.0);
            SetSkill(SkillName.Blacksmith, 65.0, 88.0);
            SetSkill(SkillName.Fencing, 60.0, 83.0);
            SetSkill(SkillName.Macing, 61.0, 93.0);
            SetSkill(SkillName.Swords, 60.0, 83.0);
            SetSkill(SkillName.Tactics, 60.0, 83.0);
            SetSkill(SkillName.Parry, 61.0, 93.0);
        }

        public override void InitSBInfo()
        {
            /*m_SBInfos.Add(new SBAxeWeapon());
			m_SBInfos.Add(new SBKnifeWeapon());
			m_SBInfos.Add(new SBMaceWeapon());
			m_SBInfos.Add(new SBSmithTools());
			m_SBInfos.Add(new SBPoleArmWeapon());
			m_SBInfos.Add(new SBSpearForkWeapon());
			m_SBInfos.Add(new SBSwordWeapon());

			m_SBInfos.Add(new SBMetalShields());

			m_SBInfos.Add(new SBHelmetArmor());
			m_SBInfos.Add(new SBPlateArmor());
			m_SBInfos.Add(new SBChainmailArmor());
			m_SBInfos.Add(new SBRingmailArmor());
			m_SBInfos.Add(new SBStuddedArmor());
			m_SBInfos.Add(new SBLeatherArmor());*/

            m_SBInfos.Add(new SBBlacksmith());
        }

        public override VendorShoeType ShoeType
        {
            get { return VendorShoeType.None; }
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            Item item = (Utility.RandomBool() ? null : new Server.Items.RingmailChest());

            if (item != null && !EquipItem(item))
            {
                item.Delete();
                item = null;
            }

            if (item == null)
                AddItem(new Server.Items.FullApron());

            AddItem(new Server.Items.Bascinet());
            AddItem(new Server.Items.SmithHammer());
        }

        public override BulkOrderSystem BulkOrderSystem { get { return DefSmith.System; } }

        public Blacksmith(Serial serial)
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