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

/* Scripts\Mobiles\Special\Christmas Elves\AI\ElfPeasantAI.cs
 * ChangeLog
 * 12/12/23, Yoar
 *      Moved to a separate source file.
 */

using System;
using static Server.Utility;

namespace Server.Mobiles
{
    public class ElfPeasantAI : BaseAI
    {
        public static int ShootRange { get { return 10; } }
        public static TimeSpan ShotDelay { get { return TimeSpan.FromSeconds(1.8); } }

        private DateTime m_NextSwitch;
        private DateTime m_NextShotTime;

        public ElfPeasantAI(BaseCreature m)
            : base(m)
        {
        }

        public override void OnActionChanged(ActionType oldAction)
        {
            base.OnActionChanged(oldAction);

            if (this.Action == ActionType.Combat)
                m_NextSwitch = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(15, 30));
        }

        public override bool DoActionWander()
        {
            if (m_Mobile.Combatant == null)
                m_Mobile.DebugSay(DebugFlags.AI, "I have no combatant");

            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I have detected {0} and I will attack", m_Mobile.FocusMob.Name);

                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;
                return true;
            }
            else
            {
                return base.DoActionWander();
            }
        }

        // we're having a fun snowball fight
        public override bool DoActionCombat(MobileInfo info)
        {
            Mobile combatant = info.target;

            m_Mobile.Warmode = true;

            m_Mobile.DebugSay(DebugFlags.AI, "Doing ElfPeasantAI DoActionCombat");

            if (DateTime.UtcNow >= m_NextSwitch || combatant == null || info.gone || info.dead || info.hidden || info.fled)
            {
                m_NextSwitch = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(15, 30));

                if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "Something happened to my combatant, so I am going to fight {0}", m_Mobile.FocusMob.Name);

                    m_Mobile.Combatant = combatant = m_Mobile.FocusMob;
                    m_Mobile.FocusMob = null;
                }
                else
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "Something happened to my combatant, and nothing is around. I am on guard.");
                    // 8/25/2023, Adam: unset everything or else the creature will just stand and spin (pissed off) there.
                    //  By unsetting, the creature returns to master, and resumes guard.
                    m_Mobile.Combatant = combatant = m_Mobile.FocusMob = null;
                    Action = ActionType.Guard;
                    return true;
                }

                if (!m_Mobile.InRange(combatant, m_Mobile.RangePerception))
                {
                    // they are somewhat far away, can we find something else?
                    if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
                    {
                        m_Mobile.Combatant = m_Mobile.FocusMob;
                        m_Mobile.FocusMob = null;
                    }
                    else if (!m_Mobile.InRange(combatant, m_Mobile.RangePerception * 3))
                    {
                        m_Mobile.Combatant = null;
                    }

                    combatant = m_Mobile.Combatant;

                    if (combatant == null)
                    {
                        m_Mobile.DebugSay(DebugFlags.AI, "My combatant has fled, so I am on guard");
                        Action = ActionType.Guard;
                        return true;
                    }
                }
            }

            if (combatant != null)
            {
                // pause to fire when need be, based on swing timer and core delay
                if (m_Mobile.InRange(combatant, ShootRange) && DateTime.UtcNow >= m_Mobile.NextCombatTime)
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "pausing to shoot");

                    m_Mobile.Direction = m_Mobile.GetDirectionTo(combatant);

                    m_NextShotTime = DateTime.UtcNow + ShotDelay;
                }

                // only run when were not waiting for a shot delay
                if (DateTime.UtcNow >= m_NextShotTime)
                {
                    int range = Utility.RandomMinMax(m_Mobile.RangeFight, ShootRange - 2);

                    // run to our combatant
                    if (WalkMobileRange(combatant, 1, true, range, range))
                    {
                        m_Mobile.DebugSay(DebugFlags.AI, "I am in range");
                    }
                }

                // uh oh, this just got serious... let's get outta here
                if (m_Mobile.Hits < m_Mobile.HitsMax)
                    Action = ActionType.Flee;
            }

            return true;
        }

        public override bool DoActionGuard()
        {
            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I have detected {0}, attacking", m_Mobile.FocusMob.Name);

                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;
                return true;
            }
            else
            {
                return base.DoActionGuard();
            }
        }

        private LocalTimer FleeTimer = new LocalTimer();
        public override bool DoActionFlee()
        {
            if (m_Mobile.FocusMob == null)
                m_Mobile.FocusMob = m_Mobile.Combatant;

            if (m_Mobile.FocusMob is BaseCreature bc && !bc.Controlled)
            {   // forget about other elves attacking us
                if (FleeTimer.Running == false)
                    FleeTimer.Start(30000);
                else if (FleeTimer.Triggered)
                {
                    FleeTimer.Stop();
                    m_Mobile.FocusMob = m_Mobile.Combatant = null;
                }
            }

            return base.DoActionFlee();
        }

        [Flags]
        private enum SaveFlag : int
        {
            None = 0x0000,
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            int version = 1; // current version (up to 4095)

            SaveFlag flags = (SaveFlag)version;

            writer.Write((int)flags); // save the version and flags
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            SaveFlag flags = (SaveFlag)reader.ReadInt(); // grab the version and flags

            int version = (int)(flags & (SaveFlag)0xFFF); // mask out the version
        }
    }
}