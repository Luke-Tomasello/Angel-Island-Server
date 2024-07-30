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

/* Scripts\Mobiles\Special\Christmas Elves\EvilElfMage.cs
 * ChangeLog
 * 12/12/23, Yoar
 *      Moved to a separate source file.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("an elf corpse")]
    public class EvilElfMage : BaseChristmasElf
    {
        [Constructable]
        public EvilElfMage()
            : base(Core.RuleSets.AngelIslandRules() ? AIType.AI_BaseHybrid : AIType.AI_Mage, FightMode.All | FightMode.Closest, 16, 1, 0.1, 0.25)
        {
            Title = "the elf storyteller";
            FightStyle = FightStyle.Magic | FightStyle.Smart | FightStyle.Bless | FightStyle.Curse;

            Hue = 0x835; // pale

            SetStr(76, 95);
            SetDex(51, 60);
            SetInt(151, 175);

            SetDamage(6, 12);

            SetSkill(SkillName.EvalInt, 95.1, 100.0);
            SetSkill(SkillName.Magery, 95.1, 100.0);
            SetSkill(SkillName.Meditation, 95.1, 100.0);
            SetSkill(SkillName.MagicResist, 102.5, 125.0);
            SetSkill(SkillName.Tactics, 65.0, 87.5);
            SetSkill(SkillName.Wrestling, 65.0, 87.5);

            Spellbook spellbook = new Spellbook();
            spellbook.Name = "story book";
            spellbook.Movable = false;
            ElfHelper.ForceAddItem(this, spellbook);

            if (Core.RuleSets.AngelIslandRules())
            {
                BardImmune = true;
                UsesHumanWeapons = false;
                UsesBandages = true;
                UsesPotions = true;
                CanRun = true;
                CanReveal = true;
                CrossHeals = true;

                PackItem(new Bandage(Utility.RandomMinMax(VirtualArmor, VirtualArmor * 2)), lootType: LootType.UnStealable);
                PackStrongPotions(6, 12);
                PackItem(new Pouch(), lootType: LootType.UnStealable);
            }

            Fame = 4000;
            Karma = -4000;
        }

        public override bool ClickTitle { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool AlwaysMurderer { get { return true; } }
        public override bool UsesOppositionGroups { get { return true; } }
        public override OppositionGroup OppositionGroup { get { return OppositionGroup.ElvesAndEvilElves; } }

        public EvilElfMage(Serial serial)
            : base(serial)
        {
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