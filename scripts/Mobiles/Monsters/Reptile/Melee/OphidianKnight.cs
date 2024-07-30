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

/* Scripts/Mobiles/Monsters/Reptile/Melee/OphidianKnight.cs
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
    [TypeAlias("Server.Mobiles.OphidianAvenger")]
    public class OphidianKnight : BaseCreature
    {
        private static string[] m_Names = new string[]
            {
                "an ophidian knight-errant",
                "an ophidian avenger"
            };

        [Constructable]
        public OphidianKnight()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = m_Names[Utility.Random(m_Names.Length)];
            Body = 86;
            BaseSoundID = 634;

            SetStr(417, 595);
            SetDex(166, 175);
            SetInt(46, 70);

            SetHits(266, 342);
            SetMana(0);

            SetDamage(16, 19);

            SetSkill(SkillName.Poisoning, 60.1, 80.0);
            SetSkill(SkillName.MagicResist, 65.1, 80.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 10000;
            Karma = -10000;

            VirtualArmor = 40;
        }

        public override int Meat { get { return 2; } }
        public override int Hides { get { return 10; } }
        public override HideType HideType { get { return HideType.Spined; } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override Poison HitPoison { get { return Poison.Lethal; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 3 : 0; } }

        public OphidianKnight(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(400, 500);
                PackItem(new DeadlyPoisonPotion());
                PackMagicEquipment(1, 2, 0.20, 0.20);
                PackMagicEquipment(1, 2, 0.05, 0.05);
                // Category 3 MID
                PackMagicItem(1, 2, 0.10);
                PackMagicItem(1, 2, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020403074442/uo.stratics.com/hunters/ophaveng.shtml
                    // 200 to 300 Gold, Potions, Arrows, Gems, Scrolls, Magic items
                    // http://web.archive.org/web/20020301014712/uo.stratics.com/hunters/ophknight.shtml
                    // 200 to 300 Gold, Potions, Arrows, Gems
                    // (Hmm, interesting. RunUO has this as the same mob, yet They have different loot!
                    if (Spawning)
                    {
                        PackGold(200, 300);
                    }
                    else
                    {
                        PackPotion(0.9);
                        PackPotion(0.6);
                        PackPotion(0.2);

                        PackItem(new Arrow(Utility.Random(1, 4)));

                        PackGem(1, .9);
                        PackGem(1, .05);

                        if (Name == "an ophidian avenger")
                        {
                            PackScroll(1, 6);
                            PackScroll(1, 6, 0.8);

                            if (Utility.RandomBool())
                                PackMagicEquipment(1, 2);
                            else
                                PackMagicItem(1, 2, 0.10);
                        }
                    }
                }
                else
                {   // Standard RunUO
                    if (Spawning)
                        PackItem(new LesserPoisonPotion());

                    AddLoot(LootPack.Rich, 2);
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