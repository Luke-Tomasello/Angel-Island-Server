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

/* SScripts\Mobiles\Vendors\NPC\RugMerchant.CS
 * ChangeLog
 *  9/16/04, Pigpen
 * 		Created RugMerchant.cs
 */

using System.Collections;

namespace Server.Mobiles
{
    public class RugMerchant : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        [Constructable]
        public RugMerchant()
            : base("the Rug Merchant")
        {
            //SetSkill( SkillName.Camping, 55.0, 78.0 );
            //SetSkill( SkillName.Alchemy, 60.0, 83.0 );
            //SetSkill( SkillName.AnimalLore, 85.0, 100.0 );
            //SetSkill( SkillName.Cooking, 45.0, 68.0 );
            //SetSkill( SkillName.Tracking, 36.0, 68.0 );
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBRugMerchant());
        }

        public RugMerchant(Serial serial)
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