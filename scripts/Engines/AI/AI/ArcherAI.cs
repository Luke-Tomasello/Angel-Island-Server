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

/* Scripts/Engines/AI/AI/ArcherAI.cs
 * CHANGELOG
 *	1/1/09, Adam
 *		Add new Serialization model for creature AI.
 *		BaseCreature will already be serializing this data when I finish the upgrade, so all you need to do is add your data to serialize. 
 *		Make sure to use the SaveFlags to optimize saves by not writing stuff that is non default.
 *  7/03/06, Kit
 *		Rewrote delta time caculation method on when to stop and fire, removed emergency patch try/catch logic.
 *  7/02/06, Kit
 *		Fixed bug dealing with flee/guard mode and not detecting ammo in pack
 *		optimized stop to shot arrow logic and general AI logic.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using Server.Items;
using System;
using static Server.Utility;

namespace Server.Mobiles
{
    public class ArcherAI : BaseAI
    {
        private TimeSpan m_ShotDelay = TimeSpan.FromSeconds(0.7);
        public DateTime m_NextShotTime;

        public ArcherAI(BaseCreature m)
            : base(m)
        {
            m_NextShotTime = DateTime.UtcNow + m_ShotDelay;
        }

        public override bool DoActionWander()
        {
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

            return true;
        }

        public override bool DoActionCombat(MobileInfo info)
        {
            Mobile combatant = info.target;
            m_Mobile.Warmode = true;


            m_Mobile.DebugSay(DebugFlags.AI, "Doing ArcherAI DoActionCombat");

            if (combatant == null || info.gone || info.dead || info.hidden || info.fled)
            {
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
                    // They are somewhat far away, can we find something else?
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
                //caculate delta offset for when to stop and fire.
                DateTime NextFire = m_Mobile.NextCombatTime;
                bool bTimeout = (DateTime.UtcNow + TimeSpan.FromSeconds(0.25)) >= NextFire;

                //pause to fire when need be, based on swing timer and core delay
                //computer swing time via next combat time and then subtract 0.25 as
                //that is the delay returned when moving and a bow is equipped.
                if (m_Mobile.InRange(combatant, m_Mobile.Weapon.MaxRange) && bTimeout == true)
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "pauseing to shoot");

                    m_NextShotTime = DateTime.UtcNow + m_ShotDelay;
                    m_Mobile.Direction = m_Mobile.GetDirectionTo(combatant);
                }

                //only run when were not waiting for a shot delay
                if (DateTime.UtcNow >= m_NextShotTime)
                {
                    const int iRangeFightMin = 4;
                    const int iRangeFightMax = 6;
                    if (WalkMobileRange(combatant, 1, true, iWantDistMin: iRangeFightMin, iWantDistMax: iRangeFightMax))
                    {
                        m_Mobile.DebugSay(DebugFlags.AI, "I am in range");
                    }
                }

                // At 20% we should check if we must leave
                if (m_Mobile.Hits < m_Mobile.HitsMax * 20 / 100)
                {
                    bool bFlee = false;
                    // if my current hits are more than my opponent, i don't care
                    if (m_Mobile.Combatant != null && m_Mobile.Hits < m_Mobile.Combatant.Hits)
                    {
                        int iDiff = m_Mobile.Combatant.Hits - m_Mobile.Hits;

                        if (Utility.Random(0, 100) > 10 + iDiff) // 10% to flee + the diff of hits
                        {
                            bFlee = true;
                        }
                    }
                    else if (m_Mobile.Combatant != null && m_Mobile.Hits >= m_Mobile.Combatant.Hits)
                    {
                        if (Utility.Random(0, 100) > 10) // 10% to flee
                        {
                            bFlee = true;
                        }
                    }

                    if (bFlee)
                    {
                        Action = ActionType.Flee;
                    }
                }

                return true;
            }
            return true;
        }
        public override void AmmoStatus(Item weapon)
        {
            // When we have no ammo, we flee
            if (m_Mobile.UsesHumanWeapons)
            {
                Container pack = m_Mobile.Backpack;

                Item twoHanded = weapon; /* m_Mobile.FindItemOnLayer(Layer.TwoHanded);*/
                if (twoHanded == null)
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "My weapon is destroyed, thus going to flee");
                    Action = ActionType.Flee;
                    return;
                }
                Type ammoType = twoHanded.GetType() == typeof(HeavyCrossbow) ? typeof(Bolt) : typeof(Arrow);
                Item ammo;
                if (pack == null || (ammo = pack.FindItemByType(ammoType)) == null)
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "I am out of ammo and thus going to flee");
                    if (twoHanded != null && !twoHanded.Deleted)
                        pack.AddItem(twoHanded);    // unequip when no ammo
                    Action = ActionType.Flee;
                    return;
                }
                else
                {
                    ammo.Consume(1);
                    m_Mobile.Notify(notification: Notification.AmmoStatus, ammo);
                    //Utility.ConsoleOut(string.Format("Consuming ammo. {0} remaining", ammo.Amount), ConsoleColor.Green);
                }
            }
        }
        public override bool DoActionGuard()
        {
            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {

                m_Mobile.DebugSay(DebugFlags.AI, "I have detected {0}, attacking", m_Mobile.FocusMob.Name);

                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;
            }
            else
            {
                base.DoActionGuard();
            }

            return true;
        }

        #region Serialize
        private SaveFlags m_flags;

        [Flags]
        private enum SaveFlags
        {   // 0x00 - 0x800 reserved for version
            unused = 0x1000
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;                                // current version (up to 4095)
            m_flags = m_flags | (SaveFlags)version;         // save the version and flags
            writer.Write((int)m_flags);

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            m_flags = (SaveFlags)reader.ReadInt();              // grab the version an flags
            int version = (int)(m_flags & (SaveFlags)0xFFF);    // maskout the version

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
            switch (version)
            {
                default: break;
            }

        }
        #endregion Serialize
    }
}