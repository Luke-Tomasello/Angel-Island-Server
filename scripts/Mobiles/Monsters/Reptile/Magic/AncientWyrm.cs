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

/* Scripts/Mobiles/Monsters/Reptile/Magic/AncientWyrm.cs
 * ChangeLog
 *	4/12/09, Adam
 *		Update special armor drop to not use SDrop system
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 * 	4/11/05, Adam
 *		Update to use new version of Loot.ImbueWeaponOrArmor()
 *	3/28/05, Adam
 *		Use weighted table selection code for weapon/armor attr in Loot.cs
 *	3/21/05, Adam
 *		Cleaned up weighted table selection code for weapon/armor attr
 *	9/14/04, Pigpen
 *		Remove Treasure Map from loot.
 *	9/11/04, Adam
 *		Minor cleanup
 *  9/11/04, Pigpen
 *		add Armor type of Wyrm Skin to random drop.
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
    [CorpseName("a dragon corpse")]
    public class AncientWyrm : BaseCreature
    {
        [Constructable]
        public AncientWyrm()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "an ancient wyrm";
            Body = 46;
            BaseSoundID = 362;

            SetStr(1096, 1185);
            SetDex(86, 175);
            SetInt(686, 775);

            SetHits(658, 711);

            SetDamage(29, 35);

            SetSkill(SkillName.EvalInt, 80.1, 100.0);
            SetSkill(SkillName.Magery, 80.1, 100.0);
            SetSkill(SkillName.Meditation, 52.5, 75.0);
            SetSkill(SkillName.MagicResist, 100.5, 150.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);

            Fame = 22500;
            Karma = -22500;

            VirtualArmor = 70;
        }
        public override Characteristics MyCharacteristics { get { return Characteristics.Run | Characteristics.Fly; } }
        public override int GetIdleSound()
        {
            return 0x2D3;
        }

        public override int GetHurtSound()
        {
            return 0x2D1;
        }

        public override bool HasBreath { get { return true; } } // fire breath enabled
                                                                // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }
        // See links below of era data for Hides
        public override HideType HideType { get { return (PublishInfo.Publish >= 15) ? HideType.Barbed : HideType.Regular; } }
        public override int Hides { get { return 20; } }
        public override int Meat { get { return 19; } }
        public override int Scales { get { return 12; } }
        public override ScaleType ScaleType { get { return (ScaleType)Utility.Random(4); } }
        public override Poison PoisonImmune { get { return Poison.Regular; } }

        public AncientWyrm(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGem();
                PackGem();
                PackGold(2500, 3500);
                //PackMagicEquipment( 2, 3, 1.0, 1.0 );
                //PackMagicEquipment( 2, 3, 0.60, 0.60 );
                PackScroll(6, 8);
                PackScroll(6, 8);
                PackPotion();

                // adam: add 25% chance to get a Random Slayer Instrument
                PackSlayerInstrument(.25);

                // Pigpen: Add the WyrmSkin (rare) armor to this mini-boss.
                if (Core.RuleSets.MiniBossArmor())
                    DoMiniBossArmor();

                // Use our unevenly weighted table for chance resolution
                Item item;
                item = Loot.RandomArmorOrShieldOrWeapon();
                PackItem(Loot.ImbueWeaponOrArmor(noThrottle: false, item, Loot.ImbueLevel.Level6 /*6*/, 0, false));
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20021014224951/uo.stratics.com/hunters/ancientwyrm.shtml
                    // 1100 to 1300 Gold, Magic Items, Gems, 19 Raw Ribs (carved), 20 Barbed Hides (carved), 12 Scales of any color except blue
                    // https://web.archive.org/web/20010808171621fw_/http://uo.stratics.com/hunters/wyrm.shtml
                    // 1100 to 1300 gold, Magic Items, 19 Raw Ribs (carved), 20 Hides (carved)

                    if (Spawning)
                    {
                        PackGold(1100, 1300);
                    }
                    else
                    {
                        // Pigpen: Add the WyrmSkin (rare) armor to this mini-boss.
                        if (Core.RuleSets.MiniBossArmor())
                            DoMiniBossArmor();

                        PackMagicStuff(2, 3, 0.06); // TODO: no idea the level, but we'll guess the highest level
                        PackMagicStuff(2, 3, 0.06);
                        if (PublishInfo.Publish >= 15)
                            PackGem(5);                 // runuo says 5, so we'll say 5

                        if (PublishInfo.Publish >= 15)
                            ;                           // 12 Scales of any color except blue
                    }
                }
                else
                {   // standard runuo
                    AddLoot(LootPack.FilthyRich, 3);
                    AddLoot(LootPack.Gems, 5);
                }
            }
        }
        private void DoMiniBossArmor()
        {
            if (Utility.Chance(0.30))
            {
                switch (Utility.Random(7))
                {
                    case 0: PackItem(new WyrmSkinArmor(), no_scroll: true); break;    // female chest
                    case 1: PackItem(new WyrmSkinArms(), no_scroll: true); break;     // arms
                    case 2: PackItem(new WyrmSkinTunic(), no_scroll: true); break;    // male chest
                    case 3: PackItem(new WyrmSkinGloves(), no_scroll: true); break;   // gloves
                    case 4: PackItem(new WyrmSkinGorget(), no_scroll: true); break;   // gorget
                    case 5: PackItem(new WyrmSkinLeggings(), no_scroll: true); break; // legs
                    case 6: PackItem(new WyrmSkinHelmet(), no_scroll: true); break;   // helm
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