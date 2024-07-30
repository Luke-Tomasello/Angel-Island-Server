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

//  /Scripts/Mobiles/Vendors/NPC/Weaponsmith.cs
//	CHANGE LOG
//  10/14/21, Yoar
//      Bulk Order System overhaul:
//      - Re-enabled support for bulk orders.
//      - The bulk order system remains disabled unless BulkOrderSystem.Enabled returns true.
//  03/29/2004 - Pulse
//		Removed ability for this NPC vendor to support Bulk Order Deeds
//		Weaponsmiths will no longer issue or accept these deeds.

using Server.Engines.BulkOrders;
using System.Collections;

namespace Server.Mobiles
{
    public class Weaponsmith : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        [Constructable]
        public Weaponsmith()
            : base("the weaponsmith")
        {
            SetSkill(SkillName.ArmsLore, 64.0, 100.0);
            SetSkill(SkillName.Blacksmith, 65.0, 88.0);
            SetSkill(SkillName.Fencing, 45.0, 68.0);
            SetSkill(SkillName.Macing, 45.0, 68.0);
            SetSkill(SkillName.Swords, 45.0, 68.0);
            SetSkill(SkillName.Tactics, 36.0, 68.0);
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBAxeWeapon());
            m_SBInfos.Add(new SBKnifeWeapon());
            m_SBInfos.Add(new SBMaceWeapon());
            m_SBInfos.Add(new SBPoleArmWeapon());
            m_SBInfos.Add(new SBSpearForkWeapon());
            m_SBInfos.Add(new SBSwordWeapon());
        }

        public override VendorShoeType ShoeType
        {
            get { return Utility.RandomBool() ? VendorShoeType.Boots : VendorShoeType.ThighBoots; }
        }

        public override int GetShoeHue()
        {
            return 0;
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new Server.Items.HalfApron());
        }

        public override BulkOrderSystem BulkOrderSystem { get { return DefSmith.System; } }

        public Weaponsmith(Serial serial)
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