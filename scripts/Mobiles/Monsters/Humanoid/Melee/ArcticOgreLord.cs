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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/ArticOgreLord.cs
 * ChangeLog
 *	4/12/09, Adam
 *		Update special armor drop to not use SDrop system
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 * 	4/11/05, Adam
 *		Update to use new version of Loot.ImbueWeaponOrArmor()
 *	3/28/05, Adam
 *		Use weighted table selection code for weapon/armor attr in Loot.cs
 *	3/21/05, Adam
 *		Cleaned up weighted table selection code for weapon/armor attr
 *	9/14/04, Pigpen
 *		Remove Treasure map from loot.
 *	9/11/04, Adam
 *		Replace lvl 3 Treasure Map with lvl 5
 *		Change helm drop to 2%
 *  9/10/04, Pigpen
 *  	add Armor type of Arctic Storm to Random Drop
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
    [CorpseName("a frozen ogre lord's corpse")]
    [TypeAlias("Server.Mobiles.ArticOgreLord")]
    public class ArcticOgreLord : BaseCreature
    {
        [Constructable]
        public ArcticOgreLord()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = "an arctic ogre lord";
            Body = 135;
            BaseSoundID = 427;

            SetStr(1100, 1200);
            SetDex(66, 75);
            SetInt(46, 70);

            SetHits(1100, 1200);

            SetDamage(20, 25);

            SetSkill(SkillName.MagicResist, 125.1, 140.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 50;
        }

        public override int Meat { get { return 2; } }
        public override Poison PoisonImmune { get { return Poison.Regular; } }
        public override AuraType MyAura { get { return AuraType.Ice; } }
        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }

        public ArcticOgreLord(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(2500, 3500);
                PackItem(new Club());
                //PackMagicEquipment( 1, 3, 0.30, 0.30 );

                // adam: add 25% chance to get a Random Slayer Instrument
                PackSlayerInstrument(.25);

                // Pigpen: Add the Arctic Storm (rare) armor to this mini-boss.
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
                {   // http://web.archive.org/web/20020202092201/uo.stratics.com/hunters/arcticogrelord.shtml
                    // 500 - 700 Gold, Magic Items, a normal club and a gem
                    if (Spawning)
                    {
                        PackGold(500, 700);
                    }
                    else
                    {
                        // Pigpen: Add the Arctic Storm (rare) armor to this mini-boss.
                        if (Core.RuleSets.MiniBossArmor())
                            DoMiniBossArmor();

                        if (Utility.RandomBool())
                            PackMagicEquipment(1, 3);
                        else
                            PackMagicItem(2, 3, 0.10);

                        PackItem(new Club());
                        PackGem();
                    }
                }
                else
                {
                    if (Spawning)
                        PackItem(new Club());

                    AddLoot(LootPack.FilthyRich);
                    AddLoot(LootPack.Rich);
                }
            }
        }
        private void DoMiniBossArmor()
        {
            if (Utility.Chance(0.30))
            {
                switch (Utility.Random(7))
                {
                    case 0: PackItem(new ArcticStormArmor(), no_scroll: true); break;    // female chest
                    case 1: PackItem(new ArcticStormArms(), no_scroll: true); break;     // arms
                    case 2: PackItem(new ArcticStormTunic(), no_scroll: true); break;    // male chest
                    case 3: PackItem(new ArcticStormGloves(), no_scroll: true); break;   // gloves
                    case 4: PackItem(new ArcticStormGorget(), no_scroll: true); break;   // gorget
                    case 5: PackItem(new ArcticStormLeggings(), no_scroll: true); break; // legs
                    case 6: PackItem(new ArcticStormHelm(), no_scroll: true); break;     // helm
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