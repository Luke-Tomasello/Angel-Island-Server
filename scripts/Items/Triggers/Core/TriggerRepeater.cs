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

/* Items/Triggers/Core/TriggerRepeater.cs
 * CHANGELOG:
 *  5/1/2024, Adam (WaitReady)
 *      Playing music is complicated by latency. 
 *      Example: Song is 59 seconds, TriggerReater is set to 59 seconds. TriggerReater starts it's time first, then calls MusicController to play music. 
 *          The MusicController then sends packets (latency) to the client to play the requested song (No idea how much the client takes to actually start playing.)
 *          This round trip takes time, and the TriggerReater will likely fire before the MusicController has finished playing. This is problematic under the old design,
 *          since we will need to wait a full 59 seconds before the TriggerReater retries the song (big ugly gap.)
 *          When the WaitReady flag is set, 'ticks'/count are not consumed while the  MusicController is still 'busy' playing music.
 *          It is recommended that the duration for the TriggerReater be ~1 second. this gives a nice tight loop for music looping.
 *  4/28/2024, Adam (finish implementation)
 *      1. Make ITriggerable
 *      2. Serialize all of m_Link, m_Final, m_Count, and m_Interval
 *      3. Disable StartTimer(null) from Running property - this is a dangerous construct as it presupposes all triggers that follow handle a null Mobile
 * 	3/22/24, Yoar
 * 		Initial version.
 */

using System;
using System.Collections;

namespace Server.Items.Triggers
{
    [NoSort]
    public class TriggerRepeater : Item, ITriggerable
    {
        public override string DefaultName { get { return "Trigger Repeater"; } }

        #region Properties
        private Item m_Link;
        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get { return m_Link; }
            set
            {
                m_Link = value;
                if (value is MusicController)
                {
                    if (WaitReady == false)
                        SendSystemMessage("You should set WaitReady when targeting a Music Controller.");
                    if (Interval > TimeSpan.FromSeconds(1))
                        SendSystemMessage("It is also recommended Interval be short, maybe 1 second.");
                }
            }
        }

        private Item m_Final;
        [CommandProperty(AccessLevel.GameMaster)]
        public Item Final
        {
            get { return m_Final; }
            set { m_Final = value; }
        }

        private int m_Count;
        [CommandProperty(AccessLevel.GameMaster)]
        public int Count
        {
            get { return m_Count; }
            set { m_Count = value; }
        }

        private TimeSpan m_Interval;
        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Interval
        {
            get { return m_Interval; }
            set { m_Interval = value; }
        }

        bool m_WaitReady = false;
        [CommandProperty(AccessLevel.Seer)]
        public bool WaitReady
        {
            get { return m_WaitReady; }
            set { m_WaitReady = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Running
        {
            get { return (m_Timer != null); }
            set
            {
                if (Running != value)
                {
                    if (value)
                        StartTimer(null);
                    else
                        StopTimer();
                }
            }
        }
        #endregion Properties

        [Constructable]
        public TriggerRepeater()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_Count = 1;
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public bool CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            return (!Running && TriggerSystem.CanTrigger(from, m_Link));
        }

        public virtual void OnTrigger(Mobile from)
        {
            if (m_Count <= 1 || m_Interval == TimeSpan.Zero)
            {
                for (int i = 0; i < m_Count; i++)
                {
                    DebugLabelTo("(tick)");
                    TriggerSystem.CheckTrigger(from, m_Link);
                }

                if (m_Final != null)
                {
                    DebugLabelTo("(final tick)");
                    TriggerSystem.CheckTrigger(from, m_Final);
                }
            }
            else
            {
                DebugLabelTo(string.Format("Start play timer {0}", m_Interval));
                TriggerSystem.CheckTrigger(from, m_Link);

                StartTimer(from);
            }
        }

        private InternalTimer m_Timer;
        private void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        private void StartTimer(Mobile from)
        {
            StartTimer(from, 1, m_Interval);
        }

        private void StartTimer(Mobile from, int tick, TimeSpan delay)
        {
            StopTimer();

            (m_Timer = new InternalTimer(this, m_Link, m_WaitReady, from, tick, delay)).Start();
        }

        private void OnTick(Mobile from, int tick)
        {
            DebugLabelTo("(tick)");

            TriggerSystem.CheckTrigger(from, m_Link);

            if (tick >= m_Count)
            {
                if (m_Final != null)
                    TriggerSystem.CheckTrigger(from, m_Final);

                StopTimer();
            }
        }
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new TriggerSystem.ActivateCME(CanTrigger(from)));
                list.Add(new TriggerSystem.LinkCME());
            }
        }

        public override void OnAfterDelete()
        {
            StopTimer();
        }

        public TriggerRepeater(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            int version = 2;
            writer.Write(version);

            switch (version)
            {
                case 2:
                    {
                        writer.Write(m_WaitReady);
                        goto case 1;
                    }
                case 1:
                    {
                        writer.Write(m_Link);
                        writer.Write(m_Final);
                        writer.Write(m_Count);
                        writer.Write(m_Interval);
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_WaitReady = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        m_Link = reader.ReadItem();
                        m_Final = reader.ReadItem();
                        m_Count = reader.ReadInt();
                        m_Interval = reader.ReadTimeSpan();
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
        }

        private class InternalTimer : Timer
        {
            private TriggerRepeater m_Trigger;
            private Item m_Link;
            private bool m_WaitReady;
            private Mobile m_From;
            private int m_Tick;


            public Mobile From { get { return m_From; } }
            public int Tick { get { return m_Tick; } }

            public InternalTimer(TriggerRepeater trigger, Item link, bool waitReady, Mobile from, int tick, TimeSpan delay)
                : base(delay, trigger.Interval)
            {
                m_Trigger = trigger;
                m_Link = link;
                m_WaitReady = waitReady;
                m_From = from;
                m_Tick = tick;
            }

            protected override void OnTick()
            {
                if (m_WaitReady && !TriggerSystem.CanTrigger(m_From, m_Link))
                {
                    m_Trigger.DebugLabelTo("(waiting)");
                    return;
                }

                m_Trigger.OnTick(m_From, ++m_Tick);
            }
        }
    }
}