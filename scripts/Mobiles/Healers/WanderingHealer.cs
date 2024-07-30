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

/* Scripts/Mobiles/Healers/WanderingHealer.cs 
 * Changelog
 *  07/14/06, Kit
 *		Added InitOutfit override, keep staves when on template spawner!
 *	06/28/06, Adam
 *		Logic cleanup
 */

using Server.Items;

namespace Server.Mobiles
{
    public class WanderingHealer : BaseHealer
    {
        public override bool CanTeach { get { return Core.RuleSets.SiegeStyleRules() ? false : true; } }

        public override bool CheckTeach(SkillName skill, Mobile from)
        {
            if (!base.CheckTeach(skill, from))
                return false;

            return (skill == SkillName.Anatomy)
                || (skill == SkillName.Camping)
                || (skill == SkillName.Forensics)
                || (skill == SkillName.Healing)
                || (skill == SkillName.SpiritSpeak);
        }

        [Constructable]
        public WanderingHealer()
        {
            AI = (Core.RuleSets.AngelIslandRules()) ? AIType.AI_HumanMage : AIType.AI_Mage;
            ActiveSpeed = 0.2;
            PassiveSpeed = 0.8;
            RangePerception = BaseCreature.DefaultRangePerception;
            FightMode = FightMode.Aggressor;

            Title = "the wandering healer";
            IsInvulnerable = false;
            NameHue = -1;

            SetSkill(SkillName.Camping, 80.0, 100.0);
            SetSkill(SkillName.Forensics, 80.0, 100.0);
            SetSkill(SkillName.SpiritSpeak, 80.0, 100.0);
        }
        public override Characteristics MyCharacteristics { get { return base.MyCharacteristics & ~Characteristics.DamageSlows; } }
        public override bool ClickTitle { get { return false; } } // Do not display title in OnSingleClick

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            base.InitOutfit();
            AddItem(new GnarledStaff());
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

        public WanderingHealer(Serial serial)
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