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

/* Scripts/Mobiles/Monsters/Elemental/Magic/WaterElemental.cs
 * ChangeLog
 *	7/16/10, adam
 *		o increase average int 
 *		o increase average hp
 *		o increase average damage
 *		o increase average wrestling
 *		o increase average magery
 *		o new skill meditation
 *		o increase Dispel Difficulty
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	4/27/05, Kit
 *		Adjusted dispell difficulty
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a water elemental corpse")]
    public class WaterElemental : BaseCreature
    {
        public override double DispelDifficulty { get { return Summoned ? 75 : 73; } }
        public override double DispelFocus { get { return 45.0; } }

        [Constructable]
        public WaterElemental()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a water elemental";
            Body = 16;
            BaseSoundID = 278;

            SetStr(126, 155);
            SetDex(66, 85);
            SetInt(101, 125);
            SetHits(76, 93);
            SetDamage(7, 9);

            SetSkill(SkillName.EvalInt, 60.1, 75.0);
            SetSkill(SkillName.Magery, 60.1, 75.0);
            SetSkill(SkillName.MagicResist, 100.1, 115.0);
            SetSkill(SkillName.Tactics, 50.1, 70.0);
            SetSkill(SkillName.Wrestling, 50.1, 70.0);

            Fame = 4500;
            Karma = -4500;

            VirtualArmor = 40;
            // https://uo.stratics.com/database/view.php?db_content=hunters&id=747
            // (unchanged)
            ControlSlots = 3;
            CanSwim = true;
        }

        public WaterElemental(bool summoned)
            : this()
        {
            if (summoned == true)
            {
                SetStr(126, 155);
                SetDex(66, 85);
                SetInt(150 - 10, 150 + 10);
                SetHits(120 - 10, 120 + 10);
                SetDamage(13 - 2, 13 + 2);

                SetSkill(SkillName.EvalInt, 60.1, 75.0);
                SetSkill(SkillName.Magery, 80 - 10, 80 + 10);
                SetSkill(SkillName.MagicResist, 100.1, 115.0);
                SetSkill(SkillName.Tactics, 50.1, 70.0);
                SetSkill(SkillName.Wrestling, 80 - 10, 80 + 10);
                SetSkill(SkillName.Meditation, 50 - 10, 50.0 + 10);
            }
        }

        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 2 : 0; } }

        public WaterElemental(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackPotion();
                PackReg(2);
                PackGold(100, 130);
                PackItem(new BlackPearl(3));
                // Category 2 MID
                PackMagicItem(1, 1, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020214225041/uo.stratics.com/hunters/waterelemental.shtml
                    //	200 to 250 Gold, Potions, Reagents

                    if (Spawning)
                    {
                        PackGold(200, 250);
                    }
                    else
                    {
                        PackPotion();
                        // 2002 stratics says 'reagents', but later startics says 3 black pearl.
                        //	we'll assume this is what was meant in 2002
                        PackItem(new BlackPearl(3));
                    }

                }
                else
                {
                    if (Spawning)
                        PackItem(new BlackPearl(3));

                    AddLoot(LootPack.Average);
                    AddLoot(LootPack.Meager);
                    AddLoot(LootPack.Potions);
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