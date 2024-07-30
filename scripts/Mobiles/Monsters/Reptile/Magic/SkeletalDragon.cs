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

/* Scripts/Mobiles/Monsters/Reptile/Magic/SkeletalDragon.cs
 * ChangeLog
 *	4/12/09, Adam
 *		Update special armor drop to not use SDrop system
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 * 	4/11/05, Adam
 *		Update to use new version of Loot.ImbueWeaponOrArmor()
 *	3/28/05, Adam
 *		Use weighted table selection code for weapon/armor attr in Loot.cs
 *	3/21/05, Adam
 *		Cleaned up weighted table selection code for weapon/armor attr
 *	9/14/04, Pigpen
 *		Remove Treasure map from Loot.
 *	9/11/04, Adam
 *		Add TreasureMapLevel 5
 *		UnholyBoneTunic ==> UnholyBoneArmor
 *  9/11/04, Pigpen
 *		add Armor type of Unholy Bone to random drop.
 *		add Weighted system of high end loot. with 5% chance of slayer on wep drops.
 *		Changed gold drop to 2500-3500gp 
 *  7/24/04, Adam
 *		add 25% chance to get a Random Slayer Instrument
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a skeletal dragon corpse")]
    public class SkeletalDragon : BaseCreature
    {
        [Constructable]
        public SkeletalDragon()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a skeletal dragon";
            Body = 104;
            BaseSoundID = 0x488;

            SetStr(898, 1030);
            SetDex(68, 200);
            SetInt(488, 620);

            SetHits(558, 599);

            SetDamage(29, 35);

            SetSkill(SkillName.EvalInt, 80.1, 100.0);
            SetSkill(SkillName.Magery, 80.1, 100.0);
            SetSkill(SkillName.MagicResist, 100.3, 130.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);

            Fame = 22500;
            Karma = -22500;

            VirtualArmor = 80;
        }

        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }
        public override bool HasBreath { get { return true; } } // fire breath enabled
        public override int BreathFireDamage { get { return 0; } }
        public override int BreathColdDamage { get { return 100; } }
        public override int BreathEffectHue { get { return 0x480; } }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override int Meat { get { return 0; } }  // nada
        public override int Hides { get { return 0; } } // nada
        public override HideType HideType { get { return HideType.Barbed; } }

        public SkeletalDragon(Serial serial)
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

                PackGold(2500, 3500);
                //PackMagicEquipment( 1, 5, 0.50, 0.50 );
                //PackMagicEquipment( 1, 3, 0.20, 0.20 );
                PackItem(new Bone(Utility.Random(55, 80)));

                // adam: add 25% chance to get a Random Slayer Instrument
                PackSlayerInstrument(.25);

                // Pigpen: Add the Unholy Bone (rare) armor to this mini-boss.
                if (Utility.RandomDouble() < 0.10)
                {
                    switch (Utility.Random(5))
                    {
                        case 0: PackItem(new UnholyBoneLegs()); break;   // Leggings
                        case 1: PackItem(new UnholyBoneArms()); break;   // arms
                        case 2: PackItem(new UnholyBoneArmor()); break;  // Chest
                        case 3: PackItem(new UnholyBoneGloves()); break; // gloves
                        case 4: PackItem(new UnholyBoneHelm()); break;       // helm
                    }
                }

                // Use our unevenly weighted table for chance resolution
                Item item;
                item = Loot.RandomArmorOrShieldOrWeapon();
                PackItem(Loot.ImbueWeaponOrArmor(noThrottle: false, item, Loot.ImbueLevel.Level6 /*6*/, 0, false));
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020214230303/uo.stratics.com/hunters/skeletaldragon.shtml
                    // 2000 - 3000 Gold, gems, scrolls, magic items
                    // https://web.archive.org/web/20010805081053fw_/http://uo.stratics.com/hunters/skeletaldragon.shtml
                    // 2000 - 3000 Gold, gems, scrolls, magic items
                    if (Spawning)
                    {
                        PackGold(2000, 3000);
                    }
                    else
                    {
                        PackGem(1, .9);
                        PackGem(1, .6);
                        PackGem(1, .2);
                        PackScroll(3, 7);
                        PackScroll(3, 7, 0.8);
                        PackScroll(3, 7, 0.3);

                        if (Utility.RandomBool())
                            PackMagicEquipment(1, 3);
                        else
                            PackMagicItem(2, 3, 0.10);
                    }
                }
                else
                {   // Standard RunUO
                    AddLoot(LootPack.FilthyRich, 4);
                    AddLoot(LootPack.Gems, 5);
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
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}