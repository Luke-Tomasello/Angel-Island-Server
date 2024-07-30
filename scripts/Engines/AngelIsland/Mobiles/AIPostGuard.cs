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

/* Scripts/Engines/AngelIsland/AIGuardSpawn/AIPostGuard.cs
 * Created 4/1/04 by mith
 * ChangeLog
 * 5/29/2021, Adam
 *		Updated the chance for a guard to drop a key to 20% from 1%
 *		The rares there are due to their scarcity and the difficulties associated with prison.
 *		Updated the teleporter to allow these rares out.
 *	3/17/10, adam
 *		Every once ana while a guard will drop a key the the guard's after hours club.
 *		Logic:
 *			Dying guards rekey lock and generate+drop a key IF the door is locked
 *			Spawned guards lock and rekey lock IF the door is unlocked
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 1 lines removed.
 *  7/21/04, Adam
 *		1. Redo the setting of skills and setting of Damage 
 *  7/17/04, Adam
 *		1. Add NightSightScroll to drop
 *		2. Replace MindBlastScroll with FireballScroll
 *	5/23/04 smerX
 *		Enabled healing
 *	5/14/04, mith
 *		Modified FightMode from Aggressor to Closest.
 *		Added Speech.
 *	4/12/04 mith
 *		Converted stats/skills to use dynamic values defined in CoreAI.
 *	4/10/04 changes by mith
 * 		Added bag of reagents and scrolls to loot.
 *	4/1/04
 * 		Changed starting skills to be from a range of 70-80 rather than flat 75.0.
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    public class AIPostGuard : BaseAIGuard
    {
        private TimeSpan m_SpeechDelay = TimeSpan.FromSeconds(120.0); // time between speech
        public DateTime m_NextSpeechTime;

        [Constructable]
        public AIPostGuard()
            : base()
        {
            FightMode = FightMode.All | FightMode.Closest;

            InitStats(CoreAI.PostGuardStrength, 100, 100);

            // Set the BroadSword damage
            SetDamage(14, 25);

            SetSkill(SkillName.Anatomy, CoreAI.PostGuardSkillLevel);
            SetSkill(SkillName.Tactics, CoreAI.PostGuardSkillLevel);
            SetSkill(SkillName.Swords, CoreAI.PostGuardSkillLevel);
            SetSkill(SkillName.MagicResist, CoreAI.PostGuardSkillLevel);
        }
        public override void MoveToWorld(Point3D newLocation, Map map)
        {
            base.MoveToWorld(newLocation, map);
            if (map == Map.Felucca && AfterHoursClub == null)
                InitializeAfterHoursClub();
        }
        private bool InitializeAfterHoursClub()
        {   // small chance we won't find, that's ok
            if (AfterHoursClub == null)
            {
                IPooledEnumerable eable = this.GetItemsInRange(20);
                foreach (Item item in eable)
                    if (item is BaseDoor bd)
                    {   // lock it up!
                        AfterHoursClub = bd;
                        AfterHoursClub.KeyValue = Key.RandomValue();
                        AfterHoursClub.Locked = true;
                        eable.Free();
                        return AfterHoursClub.Locked == true;
                    }
                eable.Free();
            }

            return AfterHoursClub != null && AfterHoursClub.Locked == true;
        }
        public AIPostGuard(Serial serial)
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
            DropWeapon(1, 1);
            DropWeapon(1, 1);

            DropItem(new BagOfReagents(CoreAI.PostGuardNumRegDrop));
            DropItem(new Bandage(CoreAI.PostGuardNumBandiesDrop));
            DropItem(new ParalyzeScroll());
            DropItem(new FireballScroll());
            DropItem(new NightSightScroll());

            if (Utility.Chance(0.2))
            {
                IDWand wand = new IDWand();
                wand.MagicCharges = 5;
                DropItem(wand);
            }
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
                {   // ai special
                    if (Spawning)
                    {
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
            return base.OnBeforeDeath();
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (DateTime.UtcNow >= m_NextSpeechTime)
            {
                PlayerMobile pm = m as PlayerMobile;
                if (pm != null && pm.AccessLevel == AccessLevel.Player && !m.Hidden && m.Alive && m.Location != oldLocation && m.InRange(this, 8))
                {
                    if (Utility.RandomBool())
                    {
                        switch (Utility.Random(5))
                        {
                            case 0:
                                {
                                    this.Say("Back to your cage wretched dog!");
                                    break;
                                }
                            case 1:
                                {
                                    this.Say("Thinking of escape eh?");
                                    this.Say("We�ll just see about that!");
                                    break;
                                }
                            case 2:
                                {
                                    this.Say("*blows whistle*");
                                    this.Say("Escape! Escape!");
                                    break;
                                }
                            case 3:
                                {
                                    this.Say("I see you�ve lost your way.");
                                    this.Say("Shall I see you to the prison cemetery?");
                                    break;
                                }
                            case 4:
                                {
                                    this.Say("Yes, run away!");
                                    this.Say("Ah, hah hah hah!");
                                    break;
                                }
                        }

                        m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;
                    }
                }
            }

            base.OnMovement(m, oldLocation);
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