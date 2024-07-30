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

/* Scripts/Mobiles/Animals/Mounts/Kirin.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 9 lines removed.
 *  3/23/05, Kit 
 * 		Added VirtualArmor value of 60
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Mobiles
{
    [CorpseName("a ki-rin corpse")]
    public class Kirin : BaseMount
    {
        public override bool AllowFemaleRider { get { return false; } }
        public override bool AllowFemaleTamer { get { return false; } }

        public override bool InitialInnocent { get { return true; } }

        public override void OnDisallowedRider(Mobile m)
        {
            m.SendLocalizedMessage(1042319); // The Ki-Rin refuses your attempts to mount it.
        }

        [Constructable]
        public Kirin()
            : this("a ki-rin")
        {
            // untame any that were tamed
            Controlled = false;
            ControlMaster = null;
        }

        [Constructable]
        public Kirin(string name)
            : base(name, 132, 0x3EAD, AIType.AI_Mage, FightMode.Aggressor | FightMode.Evil, 10, 1, 0.25, 0.5)
        {
            BaseSoundID = 0x3C5;

            SetStr(296, 325);
            SetDex(86, 105);
            SetInt(186, 225);

            SetHits(191, 210);

            SetDamage(16, 22);

            SetSkill(SkillName.EvalInt, 80.1, 90.0);
            SetSkill(SkillName.Magery, 60.4, 100.0);
            SetSkill(SkillName.Meditation, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 85.3, 100.0);
            SetSkill(SkillName.Tactics, 20.1, 22.5);
            SetSkill(SkillName.Wrestling, 80.5, 92.5);
            VirtualArmor = 60;
            Fame = 9000;
            Karma = 9000;

            Tamable = false;        // 8/24/21, Adam: setting to false. Someone tamed this from the oaks spawn :/
            ControlSlots = 2;
            MinTameSkill = 95.1;
        }

        public override int Meat { get { return 3; } }
        public override int Hides { get { return 10; } }
        public override HideType HideType { get { return HideType.Horned; } }
        public override FoodType FavoriteFood { get { return FoodType.FruitsAndVegies | FoodType.GrainsAndHay; } }

        public Kirin(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(175, 225);
                PackScroll(1, 3);
                PackPotion();
                // Category 2 MID
                PackMagicItem(1, 1, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020810153644/uo.stratics.com/hunters/kirin.shtml
                    // 	200 - 400 Gold, scrolls, potions, gems
                    if (Spawning)
                    {
                        PackGold(200, 400);
                    }
                    else
                    {
                        PackScroll(1, 4);
                        PackPotion();
                        PackGem(1, .9);
                        PackGem(1, .05);
                    }
                }
                else
                {
                    AddLoot(LootPack.Rich);
                    AddLoot(LootPack.LowScrolls);
                    AddLoot(LootPack.Potions);
                }
            }
        }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.FeyAndUndead; }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version == 0)
                AI = AIType.AI_Mage;
        }
    }
}