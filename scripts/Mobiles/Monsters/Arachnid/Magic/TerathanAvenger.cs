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

/* Scripts/Mobiles/Monsters/Arachnid/Magic/TerathanAvenger.cs
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
    [CorpseName("a terathan avenger corpse")]
    public class TerathanAvenger : BaseCreature
    {
        [Constructable]
        public TerathanAvenger()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a terathan avenger";
            Body = 152;
            BaseSoundID = 0x24D;

            SetStr(467, 645);
            SetDex(77, 95);
            SetInt(126, 150);

            SetHits(296, 372);
            SetMana(46, 70);

            SetDamage(18, 22);

            SetSkill(SkillName.EvalInt, 70.3, 100.0);
            SetSkill(SkillName.Magery, 70.3, 100.0);
            SetSkill(SkillName.Poisoning, 60.1, 80.0);
            SetSkill(SkillName.MagicResist, 65.1, 80.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 50;
        }

        public override Poison PoisonImmune { get { return Poison.Deadly; } }
        public override Poison HitPoison { get { return Poison.Deadly; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 3 : 0; } }
        public override int Meat { get { return 2; } }

        public TerathanAvenger(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGold(400, 500);
                PackItem(new SpidersSilk(10));
                PackMagicEquipment(1, 2, 0.20, 0.20);
                PackMagicEquipment(1, 2, 0.05, 0.05);
                // Category 4 MID 
                PackMagicItem(2, 3, 0.10);
                PackMagicItem(2, 3, 0.05);
                PackMagicItem(2, 3, 0.02);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020806192748/uo.stratics.com/hunters/teraveng.shtml
                    // 200 to 250 Gold, Magic Items, Gems, Scrolls
                    if (Spawning)
                    {
                        PackGold(200, 250);
                    }
                    else
                    {
                        if (Utility.RandomBool())
                            PackMagicEquipment(1, 2);
                        else
                            PackMagicItem(2, 3, 0.05);

                        PackGem(1, .9);
                        PackGem(1, .5);
                        PackScroll(1, 6);
                    }
                }
                else
                {
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

            if (BaseSoundID == 263)
                BaseSoundID = 0x24D;
        }
    }
}