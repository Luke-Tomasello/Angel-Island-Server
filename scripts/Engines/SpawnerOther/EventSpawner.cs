using Server.Engines;
using System;

namespace Server.Mobiles
{
    public class EventSpawner : Spawner
    {
        private Event m_event;
        #region Event Properties
        [CommandProperty(AccessLevel.GameMaster)]
        public bool TimerRunning
        {
            get { return m_event.TimerRunning; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EventRunning
        {
            get { return m_event.EventRunning; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string EventStart
        {
            get { return m_event.EventStart; }
            set
            {
                m_event.EventStart = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string EventEnd
        {
            get { return m_event.EventEnd; }
            set
            {
                m_event.EventEnd = value;
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public int OffsetFromUTC
        {
            get { return m_event.OffsetFromUTC; }
            set
            {
                m_event.OffsetFromUTC = value;
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public TimeSpan Countdown
        {
            get { return m_event.Countdown; }
            set
            {
                m_event.Countdown = value;
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public TimeSpan Duration
        {
            get { return m_event.Duration; }
            set
            {
                m_event.Duration = value;
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.Owner, AccessLevel.Owner)]
        public bool DurationOverride
        {
            get { return m_event.DurationOverride; }
            set
            {
                m_event.DurationOverride = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Timezone
        {
            get
            { return m_event.Timezone; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EventDebug
        {
            get { return m_event.EventDebug; }
            set
            {
                m_event.EventDebug = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string EventTimeRemaining
        {
            get { return m_event.EventTimeRemaining; }
        }
        #endregion Event Properties
        #region hidden properties
        // don't make these a command property, we don't want GM's starting/stopping in this way
        public override bool Running
        {
            get { return base.Running; }
            set
            {
                base.Running = value;
                InvalidateProperties();
            }
        }

        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                base.Visible = value;
                InvalidateProperties();
            }
        }
        #endregion hidden properties
        [Constructable]
        public EventSpawner()
            : base()
        {
            m_event = new Event(this, null, EventStarted, EventEnded);
            base.Running = false;
        }
        public override void OnSingleClick(Mobile from)
        {
            if (!EventRunning)
            {
                LabelTo(from, "event not running");
                if (Countdown > TimeSpan.Zero)
                    LabelTo(from, string.Format("{0} activating in {1} seconds",
                        this.GetType().Name, string.Format("{0:N2}", this.Countdown.TotalSeconds)));

                LabelTo(from, "(inactive)");
            }
            else
            {
                LabelTo(from, "event running");
                LabelTo(from, "(active)");
            }
        }
        public EventSpawner(Serial serial)
            : base(serial)
        {

        }
        public virtual void EventStarted(object o)
        {
            base.Running = true;
            base.Respawn();
            if (Core.Debug && false)
                Utility.Monitor.WriteLine(string.Format("{0} got 'Event started' event.", this), ConsoleColor.Yellow);
        }
        public virtual void EventEnded(object o)
        {
            base.Running = false;
            if (!GetFlag(SpawnerAttribs.External))
                // when are being 'triggered' externally. we don't cleanup the mobile(s)
                base.RemoveObjects();
            else
                ;// debug
            if (Core.Debug && false)
                Utility.Monitor.WriteLine(string.Format("{0} got 'Event ended' event.", this), ConsoleColor.Yellow);
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(3);   // version
            m_event.Serialize(writer);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_event = new Event(this, null, EventStarted, EventEnded);
                        m_event.Deserialize(reader);
                        break;
                    }
                case 2:
                    {

                        reader.ReadDeltaTime(); // removed m_EventStart in version 3
                        goto case 1;
                    }
                case 1:
                    {
                        reader.ReadDeltaTime(); // removed m_EventEnd in version 3
                        // remove in version 3
                        //m_Timer = new InternalTimer(this, CountDown);
                        //m_Timer.Start();
                        goto case 0;
                    }
                case 0:
                    {
                    }
                    break;
            }
            if (version < 3)
            {
                m_event = new Event(this, null, EventStarted, EventEnded);
                // must kill it immediately
                m_event.Duration = TimeSpan.Zero;
                m_event.Countdown = TimeSpan.Zero;
            }
        }
    }
}
