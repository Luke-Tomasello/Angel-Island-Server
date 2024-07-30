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

/* Scripts/Mobiles/Monsters/Elemental/Magic/PoisonElemental.cs
 * ChangeLog
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  9/26/04, Jade
 *      Decreased gold drop from (450, 500) to (250, 325)
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a poison elementals corpse")]
    public class PoisonElemental : BaseCreature
    {
        [Constructable]
        public PoisonElemental()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a poison elemental";
            Body = 162;
            BaseSoundID = 263;

            SetStr(426, 515);
            SetDex(166, 185);
            SetInt(361, 435);

            SetHits(256, 309);

            SetDamage(12, 18);

            SetSkill(SkillName.EvalInt, 80.1, 95.0);
            SetSkill(SkillName.Magery, 80.1, 95.0);
            SetSkill(SkillName.Meditation, 80.2, 120.0);
            SetSkill(SkillName.Poisoning, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 85.2, 115.0);
            SetSkill(SkillName.Tactics, 80.1, 100.0);
            SetSkill(SkillName.Wrestling, 70.1, 90.0);

            Fame = 12500;
            Karma = -12500;

            VirtualArmor = 70;
        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        public override Poison HitPoison { get { return Poison.Lethal; } }
        public override double HitPoisonChance { get { return 0.75; } }

        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 5 : 0; } }

        public PoisonElemental(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(250, 325);
                PackScroll(1, 7);
                PackItem(new Nightshade(9));
                PackItem(new DeadlyPoisonPotion());
                PackItem(new GreaterPoisonPotion());
                PackMagicEquipment(1, 3, 0.30, 0.30);
                // Category 3 MID
                PackMagicItem(1, 2, 0.10);
                PackMagicItem(1, 2, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20011205104734/uo.stratics.com/hunters/poisonelemental.shtml
                    // 800 to 1100 Gold, Magic Items, Gems

                    if (Spawning)
                    {
                        PackGold(800, 1100);
                    }
                    else
                    {
                        PackMagicEquipment(1, 3);
                        PackMagicItem(1, 2, 0.05);
                        PackGem(1, .9);
                        PackGem(1, .05);
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        PackItem(new Nightshade(4));
                        PackItem(new LesserPoisonPotion());
                    }

                    AddLoot(LootPack.FilthyRich);
                    AddLoot(LootPack.Rich);
                    AddLoot(LootPack.MedScrolls);
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