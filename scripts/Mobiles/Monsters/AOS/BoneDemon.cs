/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts/Mobiles/Monsters/AOS/BoneDemon.cs
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
 *		Remove treasure map from Loot.
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
    [CorpseName("a bone demon corpse")]
    public class BoneDemon : BaseCreature
    {
        [Constructable]
        public BoneDemon()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a bone demon";
            Body = 308;
            BaseSoundID = 0x48D;
            BardImmune = true;

            SetStr(1000);
            SetDex(151, 175);
            SetInt(171, 220);

            SetHits(2200);

            SetDamage(34, 36);

            SetSkill(SkillName.EvalInt, 77.6, 87.5);
            SetSkill(SkillName.Magery, 77.6, 87.5);
            SetSkill(SkillName.Meditation, 100.0);
            SetSkill(SkillName.MagicResist, 50.1, 75.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 100.0);

            Fame = 20000;
            Karma = -20000;

            VirtualArmor = 44;
        }

        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(this); } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        public BoneDemon(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (SpawnerEngine != null)
            {
                GenerateMiniBossLoot();
                return;
            }

            // https://web.archive.org/web/20011201000000/http://uo.stratics.com/hunters/bonedemon.shtml
            // 300 - 400 Gold
            // Special: 3-7 Gems, Magic Weapons/Armor

            if (Spawning)
            {
                PackGold(300, 400);
            }
            else
            {
                PackGem(3, 7);

                PackMagicEquipment(2, 3, 1.0, 1.0);
                PackMagicEquipment(2, 3, 0.50, 0.50);
            }
        }

        private void GenerateMiniBossLoot()
        {
            // Champ-engine mini-boss variant.
            // Higher gold, bone resources, slayer instrument chance, mini-boss armor,
            // and one high-end unevenly weighted magic equipment roll.

            if (Spawning)
            {
                PackGold(2500, 3500);
            }
            else
            {
                PackGem();
                PackItem(new Bone(100));

                // 25% chance to drop a random slayer instrument
                if (Core.RuleSets.SlayerInstruments())
                    PackSlayerInstrument(0.25);

                // Rare mini-boss armor drop, ruleset-gated
                if (Core.RuleSets.MiniBossArmor())
                    DoMiniBossArmor();

                // High-end unevenly weighted armor/shield/weapon roll
                Item item = Loot.RandomArmorOrShieldOrWeapon();
                PackItem(Loot.ImbueWeaponOrArmor(noThrottle: false, item, Loot.ImbueLevel.Level6, 0, false));
            }
        }
        private void DoMiniBossArmor()
        {
            if (Utility.Chance(0.30))
            {
                switch (Utility.Random(5))
                {
                    case 0: PackItem(new UnholyBoneLegs(), no_scroll: true); break;   // Leggings
                    case 1: PackItem(new UnholyBoneArms(), no_scroll: true); break;   // arms
                    case 2: PackItem(new UnholyBoneArmor(), no_scroll: true); break;  // Chest
                    case 3: PackItem(new UnholyBoneGloves(), no_scroll: true); break; // gloves
                    case 4: PackItem(new UnholyBoneHelm(), no_scroll: true); break;   // helm
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