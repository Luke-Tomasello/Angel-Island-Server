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

/* Scripts/Engines/AI/AI/AnimalAI.cs
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

// Ideas
// When you run on animals the panic
// When if ( distance < 8 && Utility.RandomDouble() * Math.Sqrt( (8 - distance) / 6 ) >= incoming.Skills[SkillName.AnimalTaming].Value )
// More your close, the more it can panic
/*
 * AnimalHunterAI, AnimalHidingAI, AnimalDomesticAI...
 * 
 */

namespace Server.Mobiles
{
    public class AnimalAI : BaseAI
    {
        public AnimalAI(BaseCreature m)
            : base(m)
        {
        }

        public override bool DoActionWander()
        {
            // Old:
#if false
			if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, true, false, true))
			{
				m_Mobile.DebugSay( "There is something near, I go away" );
				Action = ActionType.Backoff;
			}
			else if ( m_Mobile.IsHurt() || m_Mobile.Combatant != null )
			{
				m_Mobile.DebugSay( "I am hurt or being attacked, I flee" );						
				Action = ActionType.Flee;
			}
			else
			{
				base.DoActionWander();
			}

			return true;
#endif

            // New, only flee @ 10%

            double hitPercent = (double)m_Mobile.Hits / m_Mobile.HitsMax;

            if (!m_Mobile.Summoned && !m_Mobile.Controlled && hitPercent < 0.1) // Less than 10% health
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am low on health!");
                Action = ActionType.Flee;
            }
            else if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {

                m_Mobile.DebugSay(DebugFlags.AI, "I have detected {0}, attacking", m_Mobile.FocusMob.Name);

                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;
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
                m_Mobile.DebugSay(DebugFlags.AI, "My combatant is gone..");

                Action = ActionType.Wander;

                return true;
            }

            /*if (WalkMobileRange(combatant, 1, true, m_Mobile.RangeFight, m_Mobile.RangeFight))
            {
                m_Mobile.Direction = m_Mobile.GetDirectionTo(combatant);
            }*/
            bool inRange, moveTo;
            if ((inRange = m_Mobile.InRange(combatant, m_Mobile.RangeFight)) || (moveTo = MoveTo(combatant, true, m_Mobile.RangeFight)))
            {
                if (inRange)
                    m_Mobile.DebugSay(DebugFlags.AI | DebugFlags.Pursuit, "I am in range of {0}", combatant);
                else
                    m_Mobile.DebugSay(DebugFlags.AI | DebugFlags.Pursuit, "Moving towards {0}", combatant);

                m_Mobile.Direction = m_Mobile.GetDirectionTo(combatant);
            }
            else
            {
                if (m_Mobile.GetDistanceToSqrt(combatant) > m_Mobile.RangePerception + 1)
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "I cannot find {0}", combatant.Name);

                    Action = ActionType.Wander;

                    return true;
                }
                else
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "I should be closer to {0}", combatant.Name);
                }
            }

            if (!m_Mobile.Controlled && !m_Mobile.Summoned)
            {
                double hitPercent = (double)m_Mobile.Hits / m_Mobile.HitsMax;

                if (hitPercent < 0.1)
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "I am low on health!");
                    Action = ActionType.Flee;
                }
            }

            return true;
        }

        public override bool DoActionBackoff()
        {
            double hitPercent = (double)m_Mobile.Hits / m_Mobile.HitsMax;

            if (!m_Mobile.Summoned && !m_Mobile.Controlled && hitPercent < 0.1) // Less than 10% health
            {
                Action = ActionType.Flee;
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

        public override bool DoActionFlee()
        {
            AcquireFocusMob(m_Mobile.RangePerception * 2, m_Mobile.FightMode, true, false, true);

            if (m_Mobile.FocusMob == null)
                m_Mobile.FocusMob = m_Mobile.Combatant;

            return base.DoActionFlee();
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