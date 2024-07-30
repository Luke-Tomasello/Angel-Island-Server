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

/* Scripts/Mobiles/Monsters/AOS/DarknightCreeper.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a darknight creeper corpse")]
    public class DarknightCreeper : BaseCreature
    {
        [Constructable]
        public DarknightCreeper()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = NameList.RandomName("darknight creeper");
            Body = 313;
            BaseSoundID = 0xE0;
            BardImmune = true;

            SetStr(301, 330);
            SetDex(101, 110);
            SetInt(301, 330);

            SetHits(4000);

            SetDamage(22, 26);

            SetSkill(SkillName.EvalInt, 118.1, 120.0);
            SetSkill(SkillName.Magery, 112.6, 120.0);
            SetSkill(SkillName.Meditation, 150.0);
            SetSkill(SkillName.Poisoning, 120.0);
            SetSkill(SkillName.MagicResist, 90.1, 90.9);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 90.9);

            Fame = 22000;
            Karma = -22000;

            VirtualArmor = 34;
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (!Summoned && !NoKillAwards && DemonKnight.CheckArtifactChance(this))
                DemonKnight.DistributeArtifact(this);
        }

        public override int Meat { get { return 8; } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override Poison HitPoison { get { return Poison.Lethal; } }

        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 1 : 0; } }

        public DarknightCreeper(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            PackGem();
            PackGold(1300, 1500);
            PackMagicEquipment(2, 3, 0.75, 0.75);
            PackMagicEquipment(2, 3, 0.35, 0.35);
        }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.FeyAndUndead; }
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

            if (BaseSoundID == 471)
                BaseSoundID = 0xE0;
        }
    }
}