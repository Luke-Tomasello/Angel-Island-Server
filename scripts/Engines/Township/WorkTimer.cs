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

/* Engines/Township/WorkTimer.cs
 * CHANGELOG:
 * 11/20/21, Yoar
 *	    Initial version.
 */

using System;

namespace Server.Township
{
    public abstract class WorkTimer : Timer
    {
        private Mobile m_Mobile;
        private int m_Ticks;
        private int m_Count;

        private long m_NextSkillTime;
        private DateTime m_NextSpellTime;
        private long m_NextActionTime;
        private DateTime m_LastMoveTime;

        public Mobile Mobile { get { return m_Mobile; } }
        public int Ticks { get { return m_Ticks; } }
        public int Count { get { return m_Count; } }

        public WorkTimer(Mobile m, int ticks)
            : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
        {
            m_Mobile = m;
            m_Ticks = ticks;

            m_NextSkillTime = m.NextSkillTime;
            m_NextSpellTime = m.NextSpellTime;
            m_NextActionTime = m.NextActionTime;
            m_LastMoveTime = m.LastMoveTime;
        }

        protected override void OnTick()
        {
            if (!m_Mobile.Alive || m_Mobile.Map == null || m_Mobile.Map == Map.Internal)
            {
                // no message
            }
            else if (m_NextSkillTime != m_Mobile.NextSkillTime || m_NextSpellTime != m_Mobile.NextSpellTime || m_NextActionTime != m_Mobile.NextActionTime || m_LastMoveTime != m_Mobile.LastMoveTime)
            {
                OnInterrupted();
            }
            else if (Validate())
            {
                m_Mobile.RevealingAction();

                m_Count++;

                bool done = (m_Count >= m_Ticks);

                if (!done && Core.UOTC_CFG && m_Count >= 12)
                {
                    done = true;

                    TownshipItemHelper.SendTCNotice(m_Mobile);
                }

                if (done)
                {
                    OnFinished();

                    Stop();
                }
                else if (m_Count % 3 == 0)
                {
                    OnWork();
                }

                return;
            }

            OnFailed();

            Stop();
        }

        protected virtual bool Validate()
        {
            return true;
        }

        protected virtual void OnWork()
        {
        }

        protected virtual void OnInterrupted()
        {
            m_Mobile.SendMessage("Your work was interrupted.");
        }

        protected virtual void OnFinished()
        {
        }

        protected virtual void OnFailed()
        {
        }

        protected void Animate(int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay)
        {
            if (!Mobile.Mounted)
                Mobile.Animate(action, frameCount, repeatCount, forward, repeat, delay);
        }

        protected void Emote(string format, params object[] args)
        {
            Mobile.Emote(format, args);
        }

        protected void PlaySound(int soundID)
        {
            Mobile.PlaySound(soundID);
        }
    }
}