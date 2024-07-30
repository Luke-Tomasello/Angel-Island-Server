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

/* Scripts/Mobiles/Monsters/Arachnid/Magic/TerathanMatriarch.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
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
    [CorpseName("a terathan matriarch corpse")]
    public class TerathanMatriarch : BaseCreature
    {
        [Constructable]
        public TerathanMatriarch()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a terathan matriarch";
            Body = 72;
            BaseSoundID = 599;

            SetStr(316, 405);
            SetDex(96, 115);
            SetInt(366, 455);

            SetHits(190, 243);

            SetDamage(11, 14);

            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 50.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 10000;
            Karma = -10000;
        }

        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 4 : 0; } }

        public TerathanMatriarch(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGold(300, 400);
                PackItem(new SpidersSilk(10));
                PackScroll(1, 7);
                PackScroll(1, 7);
                PackMagicEquipment(1, 2, 0.20, 0.20);
                PackPotion();
                // Category 3 MID 
                PackMagicItem(1, 2, 0.10);
                PackMagicItem(1, 2, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020806192318/uo.stratics.com/hunters/termat.shtml
                    // 150 to 300 Gold, Magic Items, Gems, Scrolls, 5 Spiders Silk
                    if (Spawning)
                    {
                        PackGold(150, 300);
                    }
                    else
                    {
                        if (Utility.RandomBool())
                            PackMagicEquipment(1, 2);
                        else
                            PackMagicItem(1, 2, 0.05);

                        PackGem(2, .9);
                        PackGem(1, .5);
                        PackScroll(1, 7);
                        PackScroll(1, 7, 0.8);
                        PackItem(new SpidersSilk(5));
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        PackItem(new SpidersSilk(5));

                        if (Core.RuleSets.AOSRules())
                            PackNecroReg(Utility.RandomMinMax(4, 10));
                    }

                    AddLoot(LootPack.Rich);
                    AddLoot(LootPack.Average, 2);
                    AddLoot(LootPack.MedScrolls, 2);
                    AddLoot(LootPack.Potions);
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