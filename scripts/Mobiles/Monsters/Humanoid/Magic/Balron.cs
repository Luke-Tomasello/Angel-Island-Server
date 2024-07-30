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

/* Scripts/Mobiles/Monsters/Humanoid/Magic/Balron.cs
 * ChangeLog
 *	4/12/09, Adam
 *		Update special armor drop to not use SDrop system
 *  7/08/06, Kit
 *		Added hellish bustier/shorts/skirt
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 8 lines removed.
 * 	4/11/05, Adam
 *		Update to use new version of Loot.ImbueWeaponOrArmor()
 *	3/28/05, Adam
 *		Use weighted table selection code for weapon/armor attr in Loot.cs
 *	3/21/05, Adam
 *		Cleaned up weighted table selection code for weapon/armor attr
 *  9/14/04, Pigpen
 *		Removed treasure map as loot.
 *  9/11/04, Pigpen
 *		add Armor type of Hellish to random drop.
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
    [CorpseName("a balron corpse")]
    public class Balron : BaseCreature
    {
        [Constructable]
        public Balron()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = NameList.RandomName("balron");
            Body = 40;
            BaseSoundID = 357;

            SetStr(986, 1185);
            SetDex(177, 255);
            SetInt(151, 250);

            SetHits(592, 711);

            SetDamage(22, 29);

            SetSkill(SkillName.Anatomy, 25.1, 50.0);
            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Magery, 95.5, 100.0);
            SetSkill(SkillName.Meditation, 25.1, 50.0);
            SetSkill(SkillName.MagicResist, 100.5, 150.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 24000;
            Karma = -24000;

            VirtualArmor = 90;

        }
        public override Characteristics MyCharacteristics { get { return Characteristics.Run | Characteristics.Fly; } }
        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : true; } }
        public override Poison PoisonImmune { get { return Poison.Deadly; } }
        public override int Meat { get { return 1; } }
        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 0 : 5; } }

        public Balron(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackReg(3);
                PackItem(new Longsword());
                PackGold(2500, 3500);
                PackScroll(6, 8);
                PackScroll(6, 8);
                //PackMagicEquipment( 2, 3, 0.80, 0.80 );
                //PackMagicEquipment( 2, 3, 0.20, 0.20 );

                // adam: add 25% chance to get a Random Slayer Instrument
                PackSlayerInstrument(.25);

                // Pigpen: Add the hellish (rare) armor to this mini-boss.
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
                {   // http://web.archive.org/web/20021014234137/uo.stratics.com/hunters/balron.shtml
                    // 800 to 1200 Gold, Magic items, Scrolls, Reagents, Level 5 treasure maps, 1 Raw Ribs (carved)
                    // https://web.archive.org/web/20010804025848fw_/http://uo.stratics.com/hunters/balron.shtml
                    // 800 to 1200 gold, Magic items, Scrolls, Reagents, Level 5 treasure maps, 1 Raw Ribs (carved)

                    if (Spawning)
                    {
                        PackGold(800, 1200);
                    }
                    else
                    {
                        // Add the hellish (rare) armor to this mini-boss.
                        if (Core.RuleSets.MiniBossArmor())
                            DoMiniBossArmor();

                        PackMagicStuff(2, 3, 0.05); // TODO: no idea the level, but we'll guess the highest level
                        PackMagicStuff(2, 3, 0.05);
                        PackScroll(1, 6, .9);
                        PackScroll(1, 6, .5);
                        PackReg(1, .9);
                        PackReg(2, .5);

                        // standard runuo and classic OSI
                        PackItem(new Longsword());
                    }
                }
                else
                {   // standard runuo
                    if (Spawning)
                        PackItem(new Longsword());

                    AddLoot(LootPack.FilthyRich, 2);
                    AddLoot(LootPack.Rich);
                    AddLoot(LootPack.MedScrolls, 2);
                }
            }
        }
        private void DoMiniBossArmor()
        {
            if (Utility.Chance(0.30))
            {
                switch (Utility.Random(10))
                {
                    case 0: PackItem(new HellishArmor(), no_scroll: true); break;     // female chest
                    case 1: PackItem(new HellishArms(), no_scroll: true); break;      // arms
                    case 2: PackItem(new HellishTunic(), no_scroll: true); break;     // male chest
                    case 3: PackItem(new HellishGloves(), no_scroll: true); break;    // gloves
                    case 4: PackItem(new HellishGorget(), no_scroll: true); break;    // gorget
                    case 5: PackItem(new HellishLeggings(), no_scroll: true); break;  // legs
                    case 6: PackItem(new HellishHelmet(), no_scroll: true); break;    // helm
                    case 7: PackItem(new HellishBustier(), no_scroll: true); break;   //bustier
                    case 8: PackItem(new HellishShorts(), no_scroll: true); break;    //shorts
                    case 9: PackItem(new HellishSkirt(), no_scroll: true); break;    //skirt
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