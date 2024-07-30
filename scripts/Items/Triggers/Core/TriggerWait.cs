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

/* Items/Triggers/Core/TriggerWait.cs
 * CHANGELOG:
 *  3/17/2024, (Deserialize)
 *      Filter for null mobile
 *  2/19/2024, Adam
 *      Add try/catch before trying to write an illegal TimeSpan. 
 *      (Likely due to simple overflow)
 * 	3/7/23, Yoar
 * 		Initial version.
 */

using System;
using System.Collections;

namespace Server.Items.Triggers
{
    public class TriggerWait : Item, ITriggerable, ITrigger
    {
        public override string DefaultName { get { return "Trigger Wait"; } }

        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }

        private Item m_Link;

        private TimeSpan m_Delay;

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get { return m_Link; }
            set { m_Link = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; }
        }

        [Constructable]
        public TriggerWait()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;
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

        public void OnTrigger(Mobile from)
        {
            StartTimer(from);
        }

        private InternalTimer m_Timer;

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

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextTick
        {
            get { return (m_Timer == null ? DateTime.MinValue : m_Timer.NextTick); }
            set { StartTimer(null, value - DateTime.UtcNow); }
        }

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
            StartTimer(from, m_Delay);
        }

        private void StartTimer(Mobile from, TimeSpan delay)
        {
            StopTimer();

            (m_Timer = new InternalTimer(this, from, delay)).Start();
        }

        private void OnTick(Mobile from)
        {
            StopTimer();
            TriggerSystem.CheckTrigger(from, m_Link);
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

        public TriggerWait(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            writer.Write(m_Event);

            // version 0
            writer.Write((Item)m_Link);

            writer.Write((TimeSpan)m_Delay);

            if (m_Timer != null)
            {
                writer.Write((bool)true);

                writer.Write((Mobile)m_Timer.From);
                TimeSpan ts;
                try
                {
                    ts = m_Timer.NextTick - DateTime.UtcNow;
                }
                catch
                {   // or something, depending on the application
                    ts = TimeSpan.Zero;
                }

                writer.Write(ts);
            }
            else
            {
                writer.Write((bool)false);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Event = reader.ReadString();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Link = reader.ReadItem();

                        m_Delay = reader.ReadTimeSpan();

                        if (reader.ReadBool())
                        {
                            Mobile m = reader.ReadMobile();
                            TimeSpan ts = reader.ReadTimeSpan();
                            if (m != null)
                                StartTimer(m, ts);
                        }

                        break;
                    }
            }
        }

        private class InternalTimer : Timer
        {
            private TriggerWait m_Trigger;
            private Mobile m_From;

            public Mobile From { get { return m_From; } }

            public InternalTimer(TriggerWait trigger, Mobile from, TimeSpan delay)
                : base(delay)
            {
                m_Trigger = trigger;
                m_From = from;
            }

            protected override void OnTick()
            {
                m_Trigger.OnTick(m_From);
            }
        }
    }
}