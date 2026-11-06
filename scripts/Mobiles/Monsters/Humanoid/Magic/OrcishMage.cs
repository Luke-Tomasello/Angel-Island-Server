/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts/Mobiles/Monsters/Humanoid/Magic/OrcishMage.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 *	12/11/04, Pix
 *		Changed ControlSlots for IOBF.
 *  11/10/04, Froste
 *      Implemented new random IOB drop system and changed drop change to 12%
 *	11/05/04, Pigpen
 *		Made changes for Implementation of IOBSystem. Changes include:
 *		Removed IsEnemy and Aggressive Action Checks. These are now handled in BaseCreature.cs
 *		Set Creature IOBAlignment to Orcish.
 *	9/19/04, Adam
 *		Add IOB drop 5%
 *  9/16/04, Pigpen
 * 		Added IOB Functionality to item OrcishKinHelm
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Alignment;
using Server.Items;
using Server.Misc;

namespace Server.Mobiles
{
    [CorpseName("a glowing orc corpse")]
    public class OrcishMage : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Orc }); } }

        public override InhumanSpeech SpeechType { get { return InhumanSpeech.Orc; } }

        [Constructable]
        public OrcishMage()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "an orcish mage";
            Body = 140;
            BaseSoundID = 0x45A;
            IOBAlignment = IOBAlignment.Orcish;
            ControlSlots = 2;

            SetStr(116, 150);
            SetDex(91, 115);
            SetInt(161, 185);

            SetHits(70, 90);

            SetDamage(4, 14);

            SetSkill(SkillName.EvalInt, 60.1, 72.5);
            SetSkill(SkillName.Magery, 60.1, 72.5);
            SetSkill(SkillName.MagicResist, 60.1, 75.0);
            SetSkill(SkillName.Tactics, 50.1, 65.0);
            SetSkill(SkillName.Wrestling, 40.1, 50.0);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 30;
        }

        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : true; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 1 : 0; } }
        public override int Meat { get { return 1; } }

        public OrcishMage(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            // http://web.archive.org/web/20011217100121/uo.stratics.com/hunters/orcmage.shtml
            // 50 to 150 Gold, Potions, Arrows, Gems, Scrolls (circle 1-4), Reagents, Mask of Orcish Kin, 1 Raw Ribs (carved)

            if (Spawning)
            {
                PackGold(50, 150);
            }
            else
            {
                PackPotion();
                PackItem(new Arrow(Utility.RandomMinMax(1, 4)));
                PackGem(1, .9);
                PackGem(1, .05);
                PackScroll(1, 4);
                PackReg(3);
                PackReg(3, 0.3);
                // http://www.uoguide.com/Savage_Empire
                // http://uo.stratics.com/secrets/archive/orcsavage.shtml
                if (PublishInfo.PublishDate >= Core.EraSAVE)
                    if (0.05 > Utility.RandomDouble())
                        PackItem(new OrcishKinMask());
            }
        }

        public override void GenerateNonStandardLoot()
        {
            // 4/28/23, Adam: Non-standard loot.
            // Reasoning: We are at least starting out with a healthy RP orc population.
            // These masks should not be super rare, so drop them unless the standard
            // loot path already produced a Mask of Orcish Kin.
            if (Backpack != null && Backpack.FindItemByType(typeof(OrcishKinMask)) != null)
                return;

            if (0.1 > Utility.RandomDouble())
                PackItem(new OrcishMask());
        }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.SavagesAndOrcs; }
        }

        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            bool useMask = (Core.RuleSets.SeasonsRules(2, 3) || Core.RuleSets.UpheavalRules());

            if (useMask ||
                (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled))
            {
                if (m.Player &&
                    m.FindItemOnLayer(Layer.Helm) is OrcishKinMask)
                {
                    return false;
                }
            }

            return base.IsEnemy(m, filter);
        }

        public override void AggressiveAction(Mobile aggressor, bool criminal, object? source = null)
        {
            base.AggressiveAction(aggressor, criminal);

            if (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled)
            {
                Item? item = aggressor.FindItemOnLayer(Layer.Helm);

                if (item is OrcishKinMask)
                {
                    AOS.Damage(aggressor, 50, new DamageContext { Source = this });
                    item.Delete();
                    aggressor.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                    aggressor.PlaySound(0x307);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}