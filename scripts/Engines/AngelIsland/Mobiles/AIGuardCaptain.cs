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

/*	Scripts/Mobiles/Gaurds/AIGuardCaptain.cs
 * ChangeLog:
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 1 lines removed.
 *  7/21/04, Adam
 *		1. FightMode.Closest
 *		2. Redo the setting of skills and setting of Damage 
 *  7/17/04, Adam
 *		1. Add NightSightScroll to drop
 *	5/23/04 smerX
 *		Enabled healing
 *	4/29/04, mith
 *		Modified to use variables in CoreAI.
 *	4/12/04 mith
 *		Converted stats/skills to use dynamic values defined in CoreAI.	
 *	4/10/04 changes by mith
 *		Added bag of reagents and scrolls to loot.
 *		Changed name to "The Captain of the Guard".
 *	4/08/04 changes by smerX
 *		Added "YOU'LL NEVER GET OUT ALIVE" OnBeforeDeath
 *		Dropped 4 Lighthouse Passes OnBeforeDeath
 *	 4/1/04 Created by mith
 *		Changed starting skills to be from a range of 70-80 rather than flat 75.0.
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    public class AIGuardCaptain : BaseAIGuard
    {
        [Constructable]
        public AIGuardCaptain()
            : base()
        {
            Name = "The Captain of the Guard";
            Title = "";

            FightMode = FightMode.All | FightMode.Closest;

            InitStats(CoreAI.CaptainGuardStrength, 100, 100);

            // Set the BroadSword damage
            SetDamage(14, 25);

            SetSkill(SkillName.Anatomy, CoreAI.CaptainGuardSkillLevel);
            SetSkill(SkillName.Tactics, CoreAI.CaptainGuardSkillLevel);
            SetSkill(SkillName.Swords, CoreAI.CaptainGuardSkillLevel);
            SetSkill(SkillName.MagicResist, CoreAI.CaptainGuardSkillLevel);
        }

        public AIGuardCaptain(Serial serial)
            : base(serial)
        {
        }

        public override bool CanBandage { get { return true; } }
        public override TimeSpan BandageDelay { get { return TimeSpan.FromSeconds(12.0); } }
        public override int BandageMin { get { return 15; } }
        public override int BandageMax { get { return 30; } }
        private void AICrossShardLoot()
        {   // Angel Island creatures that exist on multiple shards use common loot
            //  i.e., that loot doesn't change across different shard configs
            for (int i = 0; i <= CoreAI.CaptainGuardWeapDrop; ++i)
                DropWeapon(CoreAI.CaptainGuardWeapDrop, CoreAI.CaptainGuardWeapDrop);

            for (int i = 0; i <= CoreAI.CaptainGuardGHPotsDrop; ++i)
                DropItem(new GreaterHealPotion());

            for (int i = 1; i <= CoreAI.CaptainGuardNumLighthousePasses; ++i)
                DropItem(new LightHousePass());

            DropItem(new BagOfReagents(CoreAI.CaptainGuardNumRegDrop));
            DropItem(new Bandage(CoreAI.CaptainGuardNumBandiesDrop));

            DropItem(new EnergyBoltScroll(CoreAI.CaptainGuardScrollDrop));
            DropItem(new PoisonScroll(CoreAI.CaptainGuardScrollDrop));
            DropItem(new HealScroll(CoreAI.CaptainGuardScrollDrop));
            DropItem(new GreaterHealScroll(CoreAI.CaptainGuardScrollDrop));
            DropItem(new CureScroll(CoreAI.CaptainGuardScrollDrop));
            DropItem(new NightSightScroll());
        }
        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {   // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                AICrossShardLoot();
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {
                    if (Spawning)
                    {   // ai special
                        PackGold(0);
                    }
                    else
                    {
                        AICrossShardLoot();
                    }
                }
                else
                {   // Standard RunUO
                    // ai special
                }
            }
        }

        public override bool OnBeforeDeath()
        {
            this.SpeechHue = 0;
            this.Say(true, "You'll never get out alive!");
            // trigger our spawner to spawn a chest
            foreach (var spawner in TriggerSpawner.Instances)
                spawner.Trigger(0xCE1EB734);

            // unlock the after hours club?
            if (AfterHoursClub != null)
                if (Utility.RandomChance(50))
                {
                    Key key = new Key(Key.RandomValue());
                    key.Description = "After hours club";
                    AfterHoursClub.KeyValue = key.KeyValue;
                    DropItem(key);
                }

            return base.OnBeforeDeath();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}