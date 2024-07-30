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

/* Scripts/Mobiles/Healers/Healer.cs 
 * Changelog
 *	06/28/06, Adam
 *		Logic cleanup
 */

namespace Server.Mobiles
{
    public class Healer : BaseHealer
    {
        public override bool CanTeach { get { return Core.RuleSets.SiegeStyleRules() ? false : true; } }

        public override bool CheckTeach(SkillName skill, Mobile from)
        {
            if (!base.CheckTeach(skill, from))
                return false;

            return (skill == SkillName.Forensics)
                || (skill == SkillName.Healing)
                || (skill == SkillName.SpiritSpeak)
                || (skill == SkillName.Swords);
        }

        [Constructable]
        public Healer()
        {
            Title = "the healer";

            SetSkill(SkillName.Forensics, 80.0, 100.0);
            SetSkill(SkillName.SpiritSpeak, 80.0, 100.0);
            SetSkill(SkillName.Swords, 80.0, 100.0);
        }

        public override bool IsActiveVendor { get { return true; } }

        public override void InitSBInfo()
        {
            SBInfos.Add(new SBHealer());
        }

        public override bool CheckResurrect(Mobile m)
        {
            if (m.Criminal)
            {
                Say(501222); // Thou art a criminal.  I shall not resurrect thee.
                return false;
            }
            // 8/10/22, Adam: I don't think Core.RedsInTown matters here. 
            // UOGuide is not clear on this: There will be Wandering Healers willing to resurrect those that stray from the path of good. (only on the old Felucca facet)
            // https://www.uoguide.com/Siege_Perilous
            //  We'll assume that they are talking about red healers (we already got those)
            else if (m.Red)
            {
                Say(501223); // Thou'rt not a decent and good person. I shall not resurrect thee.
                return false;
            }

            return true;
        }

        public Healer(Serial serial)
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