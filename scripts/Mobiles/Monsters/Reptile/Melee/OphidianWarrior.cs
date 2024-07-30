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

/* Scripts/Mobiles/Monsters/Reptile/Melee/OphidianWarrior.cs
 * ChangeLog
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("an ophidian corpse")]
    public class OphidianWarrior : BaseCreature
    {
        private static string[] m_Names = new string[]
            {
                "an ophidian warrior",
                "an ophidian enforcer"
            };

        [Constructable]
        public OphidianWarrior()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = m_Names[Utility.Random(m_Names.Length)];
            Body = 86;
            BaseSoundID = 634;

            SetStr(150, 320);
            SetDex(94, 190);
            SetInt(64, 160);

            SetHits(128, 155);
            SetMana(0);

            SetDamage(5, 11);

            SetSkill(SkillName.MagicResist, 70.1, 85.0);
            SetSkill(SkillName.Swords, 60.1, 85.0);
            SetSkill(SkillName.Tactics, 75.1, 90.0);

            Fame = 4500;
            Karma = -4500;

            VirtualArmor = 36;
        }

        public override int Meat { get { return 1; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 1 : 0; } }

        public OphidianWarrior(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(100, 140);
                // Category 2 MID
                PackMagicItem(1, 1, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020213035113/uo.stratics.com/hunters/ophwarr.shtml
                    // 50 to 150 Gold, Potions, Arrows, Gems, Weapons
                    // http://web.archive.org/web/20020414104242/uo.stratics.com/hunters/ophenfor.shtml
                    // 50 to 150 Gold, Potions, Arrows, Gems, Weapons
                    // (same creature)
                    if (Spawning)
                    {
                        PackGold(50, 150);
                    }
                    else
                    {
                        PackPotion(0.9);
                        PackPotion(0.6);

                        PackItem(new Arrow(Utility.Random(1, 4)));

                        PackGem(1, .9);
                        PackGem(1, .05);

                        PackMagicEquipment(1, 2, 0.00, 0.25);   // no chance at armor, 25% (12.5% actual) at low end magic weapon
                    }
                }
                else
                {   // Standard RunUO
                    AddLoot(LootPack.Meager);
                    AddLoot(LootPack.Average);
                    AddLoot(LootPack.Gems);
                }
            }
        }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.TerathansAndOphidians; }
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