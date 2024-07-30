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

/* Scripts/Misc/LightCycle.cs
 * ChangeLog:
 *	3/30/23, Yoar
 *	    Added NightSight effect controls:
 *	    UnderEffect, BeginEffect, EndEffect
 */

using Server.Network;
using System;
using System.Collections.Generic;

namespace Server
{
    public class LightCycle
    {
        public const int DayLevel = 0;
        public const int NightLevel = 12;
        public const int DungeonLevel = 26;
        public const int JailLevel = 9;

        private static int m_LevelOverride = int.MinValue;

        public static int LevelOverride
        {
            get { return m_LevelOverride; }
            set
            {
                m_LevelOverride = value;

                for (int i = 0; i < NetState.Instances.Count; ++i)
                {
                    NetState ns = (NetState)NetState.Instances[i];
                    Mobile m = ns.Mobile;

                    if (m != null)
                        m.CheckLightLevels(false);
                }
            }
        }

        public static void Initialize()
        {
            new LightCycleTimer().Start();
            EventSink.Login += new LoginEventHandler(OnLogin);

            Server.CommandSystem.Register("GlobalLight", AccessLevel.GameMaster, new CommandEventHandler(Light_OnCommand));
        }

        [Usage("GlobalLight <value>")]
        [Description("Sets the current global light level.")]
        private static void Light_OnCommand(CommandEventArgs e)
        {
            if (e.Length >= 1)
            {
                LevelOverride = e.GetInt32(0);
                e.Mobile.SendMessage("Global light level override has been changed to {0}.", m_LevelOverride);
            }
            else
            {
                LevelOverride = int.MinValue;
                e.Mobile.SendMessage("Global light level override has been cleared.");
            }
        }

        public static void OnLogin(LoginEventArgs args)
        {
            Mobile m = args.Mobile;

            m.CheckLightLevels(true);
        }

        public static int ComputeLevelFor(Mobile from)
        {
            if (m_LevelOverride > int.MinValue)
                return m_LevelOverride;

            int hours, minutes;

            Server.Items.Clock.GetTime(from.Map, from.X, from.Y, out hours, out minutes);

            /* OSI times:
			 * 
			 * Midnight ->  3:59 AM : Night
			 *  4:00 AM -> 11:59 PM : Day
			 * 
			 * RunUO times:
			 * 
			 * 10:00 PM -> 11:59 PM : Scale to night
			 * Midnight ->  3:59 AM : Night
			 *  4:00 AM ->  5:59 AM : Scale to day
			 *  6:00 AM ->  9:59 PM : Day
			 */

            if (hours < 4)
                return NightLevel;

            if (hours < 6)
                return NightLevel + (((((hours - 4) * 60) + minutes) * (DayLevel - NightLevel)) / 120);

            if (hours < 22)
                return DayLevel;

            if (hours < 24)
                return DayLevel + (((((hours - 22) * 60) + minutes) * (NightLevel - DayLevel)) / 120);

            return NightLevel; // should never be
        }

        private class LightCycleTimer : Timer
        {
            public LightCycleTimer()
                : base(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5.0))
            {
                Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                for (int i = 0; i < NetState.Instances.Count; ++i)
                {
                    NetState ns = (NetState)NetState.Instances[i];
                    Mobile m = ns.Mobile;

                    if (m != null)
                        m.CheckLightLevels(false);
                }
            }
        }

        private static readonly Dictionary<Mobile, Timer> m_Timers = new Dictionary<Mobile, Timer>();

        public static bool UnderEffect(Mobile m)
        {
            return !m.CanBeginAction(typeof(LightCycle));
        }

        public static void BeginEffect(Mobile m, int level)
        {
            BeginEffect(m, level, TimeSpan.Zero);
        }

        public static void BeginEffect(Mobile m, int level, TimeSpan duration)
        {
            m.BeginAction(typeof(LightCycle));
            m.LightLevel = level;

            if (duration != TimeSpan.Zero)
                StartTimer(m, duration);
        }

        public static void EndEffect(Mobile m)
        {
            m.EndAction(typeof(LightCycle));
            m.LightLevel = 0;

            StopTimer(m);
        }

        private static void StartTimer(Mobile m, TimeSpan duration)
        {
            StopTimer(m);

            (m_Timers[m] = new NightSightTimer(m, duration)).Start();
        }

        private static void StopTimer(Mobile m)
        {
            Timer timer;

            if (!m_Timers.TryGetValue(m, out timer))
                return;

            timer.Stop();

            m_Timers.Remove(m);
        }

        public class NightSightTimer : Timer
        {
            private Mobile m_Owner;

            [Obsolete]
            public NightSightTimer(Mobile owner) : this(owner, TimeSpan.FromMinutes(Utility.Random(15, 25)))
            {
            }

            public NightSightTimer(Mobile owner, TimeSpan duration) : base(duration)
            {
                m_Owner = owner;
                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                EndEffect(m_Owner);
            }
        }
    }
}