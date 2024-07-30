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

/* Scripts/Mobiles/Monsters/Reptile/Magic/OphidianArchmage.cs
 * ChangeLog
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Mobiles
{
    [CorpseName("an ophidian corpse")]
    [TypeAlias("Server.Mobiles.OphidianJusticar", "Server.Mobiles.OphidianZealot")]
    public class OphidianArchmage : BaseCreature
    {
        private static string[] m_Names = new string[]
            {
                "an ophidian justicar",
                "an ophidian zealot"
            };

        [Constructable]
        public OphidianArchmage()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = m_Names[Utility.Random(m_Names.Length)];
            Body = 85;
            BaseSoundID = 639;

            SetStr(281, 305);
            SetDex(191, 215);
            SetInt(226, 250);

            SetHits(169, 183);
            SetStam(36, 45);

            SetDamage(5, 10);

            SetSkill(SkillName.EvalInt, 95.1, 100.0);
            SetSkill(SkillName.Magery, 95.1, 100.0);
            SetSkill(SkillName.MagicResist, 75.0, 97.5);
            SetSkill(SkillName.Tactics, 65.0, 87.5);
            SetSkill(SkillName.Wrestling, 20.2, 60.0);

            Fame = 11500;
            Karma = -11500;

            VirtualArmor = 44;
        }

        public override int Meat { get { return 1; } }

        public OphidianArchmage(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(190, 220);
                PackScroll(1, 6);
                PackScroll(1, 6);
                PackReg(5, 15);
                // Category 2 MID
                PackMagicItem(1, 1, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020214224334/uo.stratics.com/hunters/ophzealot.shtml
                    // http://web.archive.org/web/20020202091744/uo.stratics.com/hunters/ophjust.shtml
                    // (same creature)
                    // 300 Gold, Scrolls.
                    if (Spawning)
                    {
                        PackGold(300);
                    }
                    else
                    {
                        PackScroll(1, 6);
                        PackScroll(1, 6);
                    }
                }
                else
                {   // Standard RunUO
                    if (Spawning)
                    {
                        PackReg(5, 15);

                        if (Core.RuleSets.AOSRules())
                            PackNecroReg(5, 15);
                    }

                    AddLoot(LootPack.Rich);
                    AddLoot(LootPack.MedScrolls, 2);
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