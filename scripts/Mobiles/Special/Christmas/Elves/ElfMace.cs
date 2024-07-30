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

/* Scripts\Mobiles\Special\Christmas Elves\ElfMace.cs
 * ChangeLog
 * 12/12/23, Yoar
 *      Moved to a separate source file.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("an elf corpse")]
    public class ElfMace : BaseChristmasElf
    {
        [Constructable]
        public ElfMace()
            : base(AIType.AI_HumanMelee, FightMode.Aggressor | FightMode.Closest, 16, 1, 0.1, 0.25)
        {
            Title = "the elf toymaker";
            FightStyle = FightStyle.Melee;

            SetStr(96, 115);
            SetDex(86, 105);
            SetInt(51, 65);

            SetDamage(16, 22);

            SetSkill(SkillName.Macing, 60.0, 82.5);
            SetSkill(SkillName.Tactics, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 57.5, 80.0);
            SetSkill(SkillName.Healing, 60.0, 82.5);
            SetSkill(SkillName.Anatomy, 60.0, 82.5);

            SmallWarHammer hammer = new SmallWarHammer();
            hammer.AccuracyLevel = WeaponAccuracyLevel.Surpassingly;
            hammer.DamageLevel = WeaponDamageLevel.Might;
            hammer.Quality = WeaponQuality.Exceptional;
            ElfHelper.ForceAddItem(this, hammer);

            ElfHelper.ForceAddItem(this, new FullApron());

            if (Core.RuleSets.AngelIslandRules())
            {
                BardImmune = true;
                UsesHumanWeapons = true;
                UsesBandages = true;
                UsesPotions = true;
                CanRun = true;
                CrossHeals = true;

                PackItem(new Bandage(Utility.RandomMinMax(VirtualArmor, VirtualArmor * 2)), lootType: LootType.UnStealable);
                PackStrongPotions(6, 12);
                PackItem(new Pouch(), lootType: LootType.UnStealable);
            }

            Fame = 4000;
            Karma = 4000;
        }

        public override Characteristics MyCharacteristics { get { return base.MyCharacteristics & ~Characteristics.DamageSlows; } }
        public override bool ClickTitle { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool UsesOppositionGroups { get { return true; } }
        public override OppositionGroup OppositionGroup { get { return OppositionGroup.ElvesAndEvilElves; } }

        public override void AggressiveAction(Mobile aggressor, bool criminal, object source = null)
        {
            base.AggressiveAction(aggressor, criminal, source);

            ElfPeasant.HandleAggressiveAction(aggressor, this);
        }

        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            return (base.IsEnemy(m, filter) || ElfPeasant.IsElfEnemy(m));
        }

        public ElfMace(Serial serial)
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