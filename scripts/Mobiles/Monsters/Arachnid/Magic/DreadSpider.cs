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

/* Scripts/Mobiles/Monsters/Arachnid/Magic/DreadSpider.cs 
 * ChangeLog
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	8/9/04, Adam
 *		1. Add 10-20 Spider's Silk to drop
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a dread spider corpse")]
    public class DreadSpider : BaseCreature
    {
        [Constructable]
        public DreadSpider()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a dread spider";
            Body = 11;
            BaseSoundID = 1170;

            SetStr(196, 220);
            SetDex(126, 145);
            SetInt(286, 310);

            SetHits(118, 132);

            SetDamage(5, 17);

            SetSkill(SkillName.EvalInt, 65.1, 80.0);
            SetSkill(SkillName.Magery, 65.1, 80.0);
            SetSkill(SkillName.Meditation, 65.1, 80.0);
            SetSkill(SkillName.MagicResist, 45.1, 60.0);
            SetSkill(SkillName.Tactics, 55.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 75.0);

            Fame = 5000;
            Karma = -5000;

            VirtualArmor = 36;
        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override Poison HitPoison { get { return Poison.Lethal; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 3 : 0; } }

        public DreadSpider(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackMagicEquipment(1, 2, 0.20, 0.20);
                PackGold(180, 200);

                // Category 2 MID 
                PackMagicItem(1, 1, 0.05);

                // pack bulk reg
                PackItem(new SpidersSilk(Utility.RandomMinMax(10, 20)));
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20011205081312/uo.stratics.com/hunters/dreadspider.shtml
                    // 	200 to 250 Gold, Potions, Arrows, 8 spider silk
                    if (Spawning)
                    {
                        PackGold(200, 250);
                    }
                    else
                    {
                        PackPotion();
                        PackItem(new Arrow(Utility.RandomMinMax(1, 4)));
                        if (Spawning)
                            PackItem(new SpidersSilk(8));
                    }
                }
                else
                {
                    if (Spawning)
                        PackItem(new SpidersSilk(8));

                    AddLoot(LootPack.FilthyRich);
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
                BaseSoundID = 1170;
        }
    }
}