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

/* Scripts/Engines/AI/AI/PredatorAI.cs
 * CHANGELOG
 *	1/1/09, Adam
 *		Add new Serialization model for creature AI.
 *		BaseCreature will already be serializing this data when I finish the upgrade, so all you need to do is add your data to serialize. 
 *		Make sure to use the SaveFlags to optimize saves by not writing stuff that is non default.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using System;
using static Server.Utility;

/*
 * PredatorAI, its an animal that can attack
 *	Dont flee but dont attack if not hurt or attacked
 * 
 */

namespace Server.Mobiles
{
    public class PredatorAI : BaseAI
    {
        public PredatorAI(BaseCreature m)
            : base(m)
        {
        }

        public override bool DoActionWander()
        {
            if (m_Mobile.Combatant != null)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am hurt or being attacked, I kill him");
                Action = ActionType.Combat;
            }
            else if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, true, false, true))
            {
                m_Mobile.DebugSay(DebugFlags.AI, "There is something near, I go away");
                Action = ActionType.Backoff;
                return true;
            }
            else
            {
                base.DoActionWander();
            }

            return true;
        }

        public override bool DoActionCombat(MobileInfo info)
        {
            Mobile combatant = info.target;

            if (combatant == null || info.gone || info.dead || info.hidden || info.fled)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "My combatant is gone, so my guard is up");
                Action = ActionType.Wander;
                return true;
            }

            if (WalkMobileRange(combatant, 1, true, m_Mobile.RangeFight, m_Mobile.RangeFight))
            {
                m_Mobile.Direction = m_Mobile.GetDirectionTo(combatant);
            }
            else
            {
                if (m_Mobile.GetDistanceToSqrt(combatant) > m_Mobile.RangePerception + 1)
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "I cannot find {0} him", combatant.Name);

                    Action = ActionType.Wander;
                    return true;
                }
                else
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "I should be closer to {0}", combatant.Name);
                }
            }

            return true;
        }

        public override bool DoActionBackoff()
        {
            if (m_Mobile.IsHurt() || m_Mobile.Combatant != null)
            {
                Action = ActionType.Combat;
            }
            else
            {
                if (AcquireFocusMob(m_Mobile.RangePerception * 2, FightMode.All | FightMode.Closest, true, false, true))
                {
                    if (WalkMobileRange(m_Mobile.FocusMob, 1, false, m_Mobile.RangePerception, m_Mobile.RangePerception * 2))
                    {
                        m_Mobile.DebugSay(DebugFlags.AI, "Well, here I am safe");
                        Action = ActionType.Wander;
                    }
                }
                else
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "I have lost my focus, lets relax");
                    Action = ActionType.Wander;
                }
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