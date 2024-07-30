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

/* Scripts/Mobiles/Monsters/Elemental/Magic/AirElemental.cs
 * ChangeLog
 *	7/16/10, adam
 *		o decrease average int
 *		o decrease average hp
 *		o decrease average damage
 *		o increase average magery
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 8 lines removed.
 *	4/27/05, Kit
 *		Adjusted dispell difficulty
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Mobiles
{
    [CorpseName("an air elemental corpse")]
    public class AirElemental : BaseCreature
    {
        public override double DispelDifficulty { get { return 56; } }
        public override double DispelFocus { get { return 45.0; } }

        [Constructable]
        public AirElemental()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "an air elemental";
            Body = 13;
            Hue = 0x4001;
            BaseSoundID = 655;

            SetStr(126, 155);
            SetDex(166, 185);
            SetInt(101, 125);
            SetHits(76, 93);
            SetDamage(8, 10);

            SetSkill(SkillName.EvalInt, 60.1, 75.0);
            SetSkill(SkillName.Magery, 60.1, 75.0);
            SetSkill(SkillName.MagicResist, 60.1, 75.0);
            SetSkill(SkillName.Tactics, 60.1, 80.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 4500;
            Karma = -4500;

            VirtualArmor = 40;
            // https://uo.stratics.com/database/view.php?db_content=hunters&id=744
            // (unchanged)
            ControlSlots = 2;
        }

        public AirElemental(bool summoned)
            : this()
        {
            if (summoned == true)
            {
                SetStr(126, 155);
                SetDex(166, 185);
                SetInt(100 - 10, 100 + 10);
                SetHits(75 - 10, 75 + 10);
                SetDamage(11 - 1, 11 + 1);

                SetSkill(SkillName.EvalInt, 60.1, 75.0);
                SetSkill(SkillName.Magery, 70 - 10, 70 + 10);
                SetSkill(SkillName.MagicResist, 60.1, 75.0);
                SetSkill(SkillName.Tactics, 60.1, 80.0);
                SetSkill(SkillName.Wrestling, 60.1, 80.0);
            }
        }

        public override int TreasureMapLevel { get { return 2; } }

        public AirElemental(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(100, 150);
                PackMagicEquipment(1, 1, 0.10, 0.10);
                PackScroll(1, 6);
                PackScroll(1, 6);
                // Category 2 MID
                PackMagicItem(1, 1, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20021015004827/uo.stratics.com/hunters/airelemental.shtml
                    // 200 to 300 Gold, Scrolls, Potions

                    if (Spawning)
                    {
                        PackGold(200, 300);
                    }
                    else
                    {
                        PackScroll(1, 6, .9);
                        PackScroll(1, 6, .5);
                        PackPotion();
                    }
                }
                else
                {   // standard runuo
                    AddLoot(LootPack.Average);
                    AddLoot(LootPack.Meager);
                    AddLoot(LootPack.LowScrolls);
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

            if (BaseSoundID == 263)
                BaseSoundID = 655;
        }
    }
}