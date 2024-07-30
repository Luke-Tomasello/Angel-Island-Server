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

/* Scripts/Mobiles/Healers/EvilWanderingHealer.cs 
 * Changelog
 *  6/13/23, Yoar
 *      Conditioned criminal check for AOS only, as per RunUO
 *	2/10/11, Adam
 *		Add criminal check to CheckResurrect() and disallow if criminal - Thou art a criminal.  I shall not resurrect thee.
 *  07/14/06, Kit
 *		Added InitOutfit override, keep staves when on template spawner!
 *	06/28/06, Adam
 *		Logic cleanup
 */

using Server.Items;

namespace Server.Mobiles
{
    public class EvilWanderingHealer : BaseHealer
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
        public EvilWanderingHealer()
        {
            AI = (Core.RuleSets.AngelIslandRules()) ? AIType.AI_HumanMage : AIType.AI_Mage;
            ActiveSpeed = 0.2;
            PassiveSpeed = 0.8;
            RangePerception = BaseCreature.DefaultRangePerception;
            FightMode = FightMode.Aggressor;

            Title = "the wandering healer";
            IsInvulnerable = false;
            NameHue = -1;

            Karma = -10000;

            SetSkill(SkillName.Camping, 80.0, 100.0);
            SetSkill(SkillName.Forensics, 80.0, 100.0);
            SetSkill(SkillName.SpiritSpeak, 80.0, 100.0);
        }
        public override Characteristics MyCharacteristics { get { return base.MyCharacteristics & ~Characteristics.DamageSlows; } }
        public override bool AlwaysMurderer { get { return true; } }
        public override bool ClickTitle { get { return false; } } // Do not display title in OnSingleClick

        public override bool CheckResurrect(Mobile m)
        {
            if (Core.RuleSets.AOSRules() && m.Criminal)
            {
                Say(501222); // Thou art a criminal.  I shall not resurrect thee.
                return false;
            }

            return true;
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            base.InitOutfit();
            AddItem(new GnarledStaff());
        }
        public EvilWanderingHealer(Serial serial)
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