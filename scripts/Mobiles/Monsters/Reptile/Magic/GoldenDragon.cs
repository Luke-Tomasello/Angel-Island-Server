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

/* Scripts/Mobiles/Monsters/Reptile/Magic/GoldenDragon.cs
 * ChangeLog
 *  9/22/21, Adam
 *		First time checkin
 */

namespace Server.Mobiles
{
    [CorpseName("a golden dragon corpse")]
    public class GoldenDragon : BaseCreature
    {
        [Constructable]
        public GoldenDragon()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a golden dragon";
            Body = 106;
            BaseSoundID = 362;
            Hue = 0x8A5;
            Tamable = false;        // nice try
            BardImmune = true;      // too smart for that

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

            // our golden dragon is smarter than most
            FightStyle = FightStyle.Magic | FightStyle.Smart | FightStyle.Bless | FightStyle.Curse;
            BardImmune = true;
            UsesHumanWeapons = false;
            UsesBandages = false;
            UsesPotions = false;
            CanRun = true;
            CanReveal = true;   // magic and smart
            CrossHeals = false;

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
        public override HideType HideType { get { return HideType.Barbed; } }

        public GoldenDragon(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackItem(new Items.GoldOre(Utility.RandomMinMax(7, 13)));
                PackGem();
                PackGem();
                PackGold(2390, 3275);
                PackScroll(6, 8);
                PackScroll(6, 8);
                PackPotion();

                // adam: add 25% chance to get a Random Slayer Instrument
                PackSlayerInstrument(.25);

                // Use our unevenly weighted table for chance resolution
                Item item;
                item = Loot.RandomArmorOrShieldOrWeapon();
                PackItem(Loot.ImbueWeaponOrArmor(noThrottle: false, item, Loot.ImbueLevel.Level4, 0, false));
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20021014225931/uo.stratics.com/hunters/shadowwyrm.shtml
                    // 1800 to 2400 Gold, Gems, Magic items, 10 Black Scales, 19 Raw Ribs (carved), 20 Barbed Hides (carved), Level 5 treasure maps

                    if (Spawning)
                    {
                        PackGold(1800, 2400);
                    }
                    else
                    {
                        PackGem(Utility.Random(4, 6), .9);  // TODO: no idea as to the level and rate
                        PackGem(Utility.Random(4, 6), .5);

                        PackMagicEquipment(1, 3);           // TODO: no idea as to the level and rate
                        PackMagicEquipment(1, 3);
                        PackMagicItem(3, 3);
                        PackMagicItem(3, 3, 0.10);
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