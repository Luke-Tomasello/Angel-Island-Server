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

/* Scripts\Mobiles\Special\Christmas Elves\EvilElfPeasant.cs
 * ChangeLog
 * 12/12/23, Yoar
 *      Moved to a separate source file.
 */

namespace Server.Mobiles
{
    [CorpseName("an elf corpse")]
    public class EvilElfPeasant : BaseChristmasElf
    {
        protected override bool HasSnowballFights { get { return true; } }

        [Constructable]
        public EvilElfPeasant()
            : base(AIType.AI_ElfPeasant, FightMode.All | FightMode.Closest, 12, 6, 0.1, 0.25)
        {
            Title = "the elf";
            FightStyle = FightStyle.Melee;

            Hue = 0x835; // pale

            SetStr(96, 115);
            SetDex(86, 105);
            SetInt(51, 65);

            SetDamage(9, 15);

            SetSkill(SkillName.Wrestling, 60.0, 82.5);
            SetSkill(SkillName.Tactics, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 57.5, 80.0);
            SetSkill(SkillName.Healing, 60.0, 82.5);
            SetSkill(SkillName.Anatomy, 60.0, 82.5);

            Fame = 2000;
            Karma = -2000;
        }

        public override bool ClickTitle { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool AlwaysMurderer { get { return true; } }
        public override bool UsesOppositionGroups { get { return true; } }
        public override OppositionGroup OppositionGroup { get { return OppositionGroup.ElvesAndEvilElves; } }

        public EvilElfPeasant(Serial serial)
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