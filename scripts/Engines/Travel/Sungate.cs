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

/* scripts\Engines\Travel\Sungate.cs
 *	ChangeLog:
 *  1/2/23, Adam
 *      Move ValidateUse down from Sungate to the base Teleporter
 *      (Sungates are Teleporters!)
 *	10/22/22, Adam
 *	    Merging our Teleporters and Sungates to provide a consistent set of travel rules
 */

using Server.Engines;
using Server.Misc;
using System;
namespace Server.Items
{
    [DispellableFieldAttribute]
    public class Sungate : Teleporter
    {
        private bool m_bDispellable;
        public override bool IsGate => true;
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Dispellable
        {
            get
            {
                return m_bDispellable;
            }
            set
            {
                m_bDispellable = value;
            }
        }

        public virtual bool ShowFeluccaWarning { get { return false; } }

        [Constructable]
        public Sungate()
            : this(Point3D.Zero, null, true)
        {
        }

        [Constructable]
        public Sungate(bool bDispellable)
            : this(Point3D.Zero, null, bDispellable)
        {

        }

        [Constructable]
        public Sungate(Point3D target, Map targetMap, bool dispellable)
            : base(0xF6C)
        {
            Movable = false;
            Light = LightType.Circle300;

            base.PointDest = target;
            base.MapDest = targetMap;
            m_bDispellable = dispellable;
            Visible = true;
            SoundID = 0x20E;
            Delay = TimeSpan.FromSeconds(1);
            Pets = true;
        }

        public Sungate(Serial serial)
            : base(serial)
        {
        }
        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Name != null ? Name : "a blue moongate");
            if (!GetSpecialFlag(TeleporterFlags.Running))
                LabelTo(from, "(inactive)");
        }

        public override bool ValidateUse(Mobile from, bool message)
        {
            if (from.Deleted || this.Deleted)
                return false;

            if (from.Map != this.Map || !from.InRange(this, 1))
            {
                if (message)
                    from.SendLocalizedMessage(500446); // That is too far away.

                return false;
            }

            return true;
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (CanTeleport(from) && ValidateUse(from, true))
                TryUseTeleport(from);
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // Version 1
            writer.Write(m_bDispellable);

        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    {
                        m_bDispellable = reader.ReadBool();
                        break;
                    }
            }
        }
    }

    [DispellableFieldAttribute]
    public class EventSungate : Sungate
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
            set { m_event.EventEnd = value; }
        }
        [CommandProperty(AccessLevel.Seer)]
        public int OffsetFromUTC
        {
            get { return m_event.OffsetFromUTC; }
            set { m_event.OffsetFromUTC = value; InvalidateProperties(); }
        }
        [CommandProperty(AccessLevel.Seer)]
        public TimeSpan Countdown
        {
            get { return m_event.Countdown; }
            set { m_event.Countdown = value; InvalidateProperties(); }
        }
        [CommandProperty(AccessLevel.Seer)]
        public TimeSpan Duration
        {
            get { return m_event.Duration; }
            set { m_event.Duration = value; InvalidateProperties(); }
        }
        [CommandProperty(AccessLevel.Owner, AccessLevel.Owner)]
        public bool DurationOverride
        {
            get { return m_event.DurationOverride; }
            set { m_event.DurationOverride = value; }
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
            set { m_event.EventDebug = value; }
        }
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
        public EventSungate()
            : base()
        {
            m_event = new Event(this, null, EventStarted, EventEnded);
            base.Running = false;
            Visible = false;
        }
        public EventSungate(Serial serial)
            : base(serial)
        {

        }
        public void EventStarted(object o)
        {
            base.Running = true;
            Visible = true;
            if (Core.Debug && false)
                Utility.ConsoleWriteLine(string.Format("{0} got 'Event started' event.", this), ConsoleColor.Yellow);
        }
        public void EventEnded(object o)
        {
            base.Running = false;
            Visible = false;
            if (Core.Debug && false)
                Utility.ConsoleWriteLine(string.Format("{0} got 'Event ended' event.", this), ConsoleColor.Yellow);
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);   // version
            m_event.Serialize(writer);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_event = new Event(this);
                        m_event.Deserialize(reader);
                        break;
                    }
            }
        }
    }
}