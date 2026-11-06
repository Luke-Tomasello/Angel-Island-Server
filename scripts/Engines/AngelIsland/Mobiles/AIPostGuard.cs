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

/* Scripts/Engines/AngelIsland/AIGuardSpawn/AIPostGuard.cs
 * Created 4/1/04 by mith
 * ChangeLog
 * 10/17/26, Adam
 *		After hours club: only post guards spawned by a champion engine take part. The door is the one
 *		next to that engine (BaseAIGuard.FindClubDoor), on its own facet. Checked once, in OnAfterSpawn (not MoveToWorld).
 *		If the door was left unlocked (a prisoner used the captain's key), the first guard to spawn moves beside the
 *		engine and re-keys and locks it (DebugFlags.Decisions). A locked door (and any unused key) is left alone.
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

using Server.Engines.ChampionSpawn;
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
        public override void OnAfterSpawn()
        {   // ChampEngine.PrepMob calls this once, after MoveToWorld, so wandering or teleporting never repairs the door
            base.OnAfterSpawn();
            if (SpawnerEngine != null)
                RepairAfterHoursClub();
        }
        private void RepairAfterHoursClub()
        {   // a locked door (and any unused captain's key) is left alone
            BaseDoor? door = FindClubDoor(this);
            ChampEngine? engine = SpawnerEngine;
            if (door == null || door.Locked || engine == null)
                return;

            // the first guard to find it unlocked goes to the engine (3 tiles from the door) and repairs the lock
            MoveToWorld(Spawner.GetSpawnPosition(engine.Map, engine.Location, 1, Utility.SpawnFlags.None, this), engine.Map);
            door.LockAll(Key.RandomValue());
            DebugSay(DebugFlags.Decisions, "Found door unlocked, locking");
        }
        public AIPostGuard(Serial serial)
            : base(serial)
        {
        }

        public override bool CanBandage { get { return true; } }
        public override TimeSpan BandageDelay { get { return TimeSpan.FromSeconds(12.0); } }
        public override int BandageMin { get { return 15; } }
        public override int BandageMax { get { return 30; } }
        public override void GenerateLoot()
        {
            if (Spawning) { PackGold(10, 25); }
            else
            {
                // Angel Island creatures that exist on multiple shards use common loot
                //  i.e., that loot doesn't change across different shard configs

                DropWeapon(1, 1);
                DropWeapon(1, 1);

                PackItem(new BagOfReagents(CoreAI.PostGuardNumRegDrop));
                PackItem(new Bandage(CoreAI.PostGuardNumBandiesDrop));
                PackItem(new ParalyzeScroll());
                PackItem(new FireballScroll());
                PackItem(new NightSightScroll());

                if (Utility.Chance(0.2))
                {
                    IDWand wand = new IDWand();
                    wand.MagicCharges = 5;
                    PackItem(wand);
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
                PlayerMobile? pm = m as PlayerMobile;

                if (pm != null && pm.AccessLevel == AccessLevel.Player &&
                    !m.Hidden && m.Alive &&
                    m.Location != oldLocation &&
                    m.InRange(this, 8))
                {
                    if (Utility.RandomBool())
                    {
                        switch (Utility.Random(14))
                        {
                            case 0:
                                {
                                    this.Say("Back to your cage, wretched dog!");
                                    break;
                                }
                            case 1:
                                {
                                    this.Say("Thinking of escape, eh?");
                                    this.Say("We'll just see about that!");
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
                                    this.Say("I see you've lost your way.");
                                    this.Say("Shall I escort you to the prison cemetery?");
                                    break;
                                }
                            case 4:
                                {
                                    this.Say("Yes, run away!");
                                    this.Say("Ah hah hah hah!");
                                    break;
                                }
                            case 5:
                                {
                                    this.Say("Back behind the bars where you belong!");
                                    break;
                                }
                            case 6:
                                {
                                    this.Say("Another rat sniffing for freedom.");
                                    break;
                                }
                            case 7:
                                {
                                    this.Say("Careful now, prisoner.");
                                    this.Say("The last one who ran bled all over the yard.");
                                    break;
                                }
                            case 8:
                                {
                                    this.Say("Move along, convict.");
                                    this.Say("The walls are higher than your hopes.");
                                    break;
                                }
                            case 9:
                                {
                                    this.Say("You look nervous.");
                                    this.Say("Good.");
                                    break;
                                }
                            case 10:
                                {
                                    this.Say("Trying the gates again?");
                                    this.Say("You lot never learn.");
                                    break;
                                }
                            case 11:
                                {
                                    this.Say("*rattles keys*");
                                    this.Say("Hear that?");
                                    this.Say("That's the sound of your future.");
                                    break;
                                }
                            case 12:
                                {
                                    this.Say("One more step and you'll feed the crows.");
                                    break;
                                }
                            case 13:
                                {
                                    this.Say("The dead outside the walls once had plans too.");
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