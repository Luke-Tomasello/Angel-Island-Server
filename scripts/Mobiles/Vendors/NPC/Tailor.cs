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

//  /Scripts/Mobiles/Vendors/NPC/Tailor.cs
//	CHANGE LOG
//  10/14/21, Yoar
//      Bulk Order System overhaul:
//      - Re-enabled support for bulk orders.
//      - The bulk order system remains disabled unless BulkOrderSystem.Enabled returns true.
//  03/29/2004 - Pulse
//		Removed ability for this NPC vendor to support Bulk Order Deeds
//		Tailors will no longer issue or accept these deeds.

using Server.Engines.BulkOrders;
using System.Collections;

namespace Server.Mobiles
{
    public class Tailor : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        public override NpcGuild NpcGuild { get { return NpcGuild.TailorsGuild; } }

        [Constructable]
        public Tailor()
            : base("the tailor")
        {
            SetSkill(SkillName.Tailoring, 64.0, 100.0);
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBTailor());
        }

        public override VendorShoeType ShoeType
        {
            get { return Utility.RandomBool() ? VendorShoeType.Sandals : VendorShoeType.Shoes; }
        }

        public override BulkOrderSystem BulkOrderSystem { get { return DefTailor.System; } }

        public Tailor(Serial serial)
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