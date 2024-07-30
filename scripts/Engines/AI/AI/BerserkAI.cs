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

/* Scripts/Engines/AI/AI/BerserkerAI.cs
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

namespace Server.Mobiles
{
    public class BerserkAI : BaseAI
    {
        public BerserkAI(BaseCreature m)
            : base(m)
        {
        }

        public override bool DoActionWander()
        {
            m_Mobile.DebugSay(DebugFlags.AI, "I have No Combatant");

            if (AcquireFocusMob(m_Mobile.RangePerception, FightMode.All | FightMode.Closest, false, true, true))
            {

                m_Mobile.DebugSay(DebugFlags.AI, "I have detected " + m_Mobile.FocusMob.Name + " and I will attack");

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
                m_Mobile.DebugSay(DebugFlags.AI, "My combatant is deleted");
                Action = ActionType.Guard;
                return true;
            }

            if (WalkMobileRange(m_Mobile.Combatant, 1, true, m_Mobile.RangeFight, m_Mobile.RangeFight))
            {
                // Be sure to face the combatant
                m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant.Location);
            }
            else
            {
                if (m_Mobile.Combatant != null)
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "I am still not in range of " + m_Mobile.Combatant.Name);

                    if ((int)m_Mobile.GetDistanceToSqrt(m_Mobile.Combatant) > m_Mobile.RangePerception + 1)
                    {

                        m_Mobile.DebugSay(DebugFlags.AI, "I have lost " + m_Mobile.Combatant.Name);

                        Action = ActionType.Guard;
                        return true;
                    }
                }
            }

            return true;
        }

        public override bool DoActionGuard()
        {
            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, true, true))
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