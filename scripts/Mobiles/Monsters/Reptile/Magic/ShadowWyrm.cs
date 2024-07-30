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

/* Scripts/Mobiles/Monsters/Reptile/Magic/ShadowWyrm.cs
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

namespace Server.Mobiles
{
    [CorpseName("a shadow wyrm corpse")]
    public class ShadowWyrm : BaseCreature
    {
        [Constructable]
        public ShadowWyrm()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a shadow wyrm";
            Body = 106;
            BaseSoundID = 362;

            SetStr(898, 1030);
            SetDex(68, 200);
            SetInt(488, 620);

            SetHits(558, 599);

            SetDamage(29, 35);

            SetSkill(SkillName.EvalInt, 80.1, 100.0);
            SetSkill(SkillName.Magery, 80.1, 100.0);
            SetSkill(SkillName.Meditation, 52.5, 75.0);
            SetSkill(SkillName.MagicResist, 100.3, 130.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);

            Fame = 22500;
            Karma = -22500;

            VirtualArmor = 70;
        }
        public override Characteristics MyCharacteristics { get { return Characteristics.Run | Characteristics.Fly; } }
        public override int GetIdleSound()
        {
            return 0x2D5;
        }

        public override int GetHurtSound()
        {
            return 0x2D1;
        }

        public override bool HasBreath { get { return true; } } // fire breath enabled
                                                                // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }
        public override Poison PoisonImmune { get { return Poison.Deadly; } }
        public override Poison HitPoison { get { return Poison.Deadly; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 5 : 5; } }

        public override int Meat { get { return Core.RuleSets.AngelIslandRules() ? 20 : 19; } }
        public override int Hides { get { return Core.RuleSets.AngelIslandRules() ? 40 : 20; } }
        public override int Scales { get { return 10; } }
        public override ScaleType ScaleType { get { return ScaleType.Black; } }
        public override HideType HideType { get { return (PublishInfo.Publish >= 15) ? HideType.Barbed : HideType.Regular; } }

        public ShadowWyrm(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                for (int i = 0; i < 5; ++i)
                    PackGem();

                PackGold(850, 1100);
                PackMagicEquipment(1, 3, 0.50, 0.50);
                PackMagicEquipment(1, 3, 0.20, 0.20);
                PackScroll(6, 8);
                PackScroll(6, 8);
                // Category 5 MID
                PackMagicItem(3, 3, 0.20);
                PackMagicItem(3, 3, 0.10);
                PackMagicItem(3, 3, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20021014225931/uo.stratics.com/hunters/shadowwyrm.shtml
                    // 1800 to 2400 Gold, Gems, Magic items, 10 Black Scales, 19 Raw Ribs (carved), 20 Barbed Hides (carved), Level 5 treasure maps
                    // https://web.archive.org/web/20010808121033fw_/http://uo.stratics.com/hunters/shadowwyrm.shtml
                    // 	1200 to 2000 gold, Gems, Magic items, 19 Raw Ribs (carved), 20 Hides (carved), Level 5 treasure maps

                    if (Spawning)
                    {
                        PackGold(1200, 2000);
                    }
                    else
                    {
                        PackGem(Utility.Random(4, 6), .9);  // TODO: no idea as to the level and rate
                        PackGem(Utility.Random(4, 6), .5);

                        PackMagicEquipment(1, 3);           // TODO: no idea as to the level and rate
                        PackMagicEquipment(1, 3);
                        PackMagicItem(3, 3);
                        PackMagicItem(3, 3, 0.10);

                        // We handle TreasureMaps generically in BaseCreature
                    }
                }
                else
                {
                    AddLoot(LootPack.FilthyRich, 3);
                    AddLoot(LootPack.Gems, 5);
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