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

/* Scripts\Engines\Spawner\Spawner.Timer.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using System;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        public void DoTimer()
        {
            if (!Running)
                return;

            ResetTimer();
        }
        public void DoTimer(TimeSpan delay)
        {
            if (!Running)
                return;

            m_End = DateTime.UtcNow + delay;

            if (m_Timer != null)
            {
                m_Timer.Stop();         // stop the timer
                m_Timer.Flush();        // remove any queued ticks
            }

            m_Timer = new InternalTimer(this, delay);
            m_Timer.Start();
        }
        private void ResetTimer()
        {
            if (!Running)
                return;

            int minSeconds = (int)m_MinDelay.TotalSeconds;
            int maxSeconds = (int)m_MaxDelay.TotalSeconds;
            TimeSpan delay = TimeSpan.FromSeconds(Utility.RandomMinMax(minSeconds, maxSeconds));
            DoTimer(delay);
        }
        private class InternalTimer : Timer
        {
            private Spawner m_Spawner;

            public InternalTimer(Spawner spawner, TimeSpan delay)
                : base(delay)
            {
                if (spawner.IsFull)
                    Priority = TimerPriority.FiveSeconds;
                else
                    Priority = TimerPriority.OneSecond;

                m_Spawner = spawner as Spawner;
            }

            protected override void OnTick()
            {
                if (m_Spawner != null && !m_Spawner.Deleted && m_Spawner.Running == true)
                    m_Spawner.OnTick();
            }
        }
    }
}
