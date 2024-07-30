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

/* scripts\Engines\Events\Event.cs
 * Changelog:
 *  12/28/23, Yoar
 *      Added PropertyObjectAttribute
 *  5/25/2023, Adam (TimerMode)
 *      Add a dual mode timer that speeds up when the event is within 3 minutes of EventStart time. 
 *      This change allows for programming more accurate (500ms) EventStart times. 
 *  10/13/22, Adam
 *     First time check in.
 *      This module manages the start/stop of event Spawners, Sungates (moongates), Teleporters and the like.
 */

using System;
using System.Linq;
using System.Reflection;

namespace Server.Engines
{
    [PropertyObject]
    public class Event
    {
        private TimeSpan m_Offset = TimeSpan.Zero;              // internal duration marker
        private TimeSpan m_Duration = TimeSpan.Zero;            // For example: event runs 24 hours
        private TimeSpan m_Countdown = TimeSpan.Zero;           // For example: event will start in 24 hours
        private int m_OffsetFromUTC = -8;                       // default to pacific time
        DateTime m_EventStart, m_EventEnd;                      // start/end of event (calculated, not serialized.)
        private Server.Timer m_Timer;                           // not serialized
        private bool m_Running = false;                         // true if the event is running (all event objects need to start in the disabled state)
        private Item m_item;                                    // the caller (spawner, sungate, teleporter, etc.)
        private object m_context;                               // optional caller context object
        private string m_startedCallbackName;                   // optional caller event started function
        private string m_endedCallbackName;                     // optional caller event ended function
        private bool m_durationOverride;                        // shard owners can set this to allow staff to create a long running event
        /// <summary>
        /// Include this class in your 'Event' class and provide accessors for all the properties described herein.
        /// Make sure to call this classes Serialize and Deserialize methods in you class's Serialize and Deserialize
        /// </summary>
        /// <param name="you"> the item we are controlling. (we need to know if it is deleted.)</param>
        /// <param name="context"> optional caller content object.</param>
        /// <param name="startedCallback"> optional callback when the event starts. Usually Activate the object.</param>
        /// <param name="endedCallback"> optional callback when the event ends. Usually Deactivate the object.</param>
        [Constructable]
        public Event(Item you, object context = null, Action<object> startedCallback = null, Action<object> endedCallback = null)
        {
            m_item = you;
            m_context = context;
            if (startedCallback != null)
                m_startedCallbackName = startedCallback.Method.Name;
            if (endedCallback != null)
                m_endedCallbackName = endedCallback.Method.Name;

            CalculatedMetrics(true);
        }
        private bool m_Debug = false;
        private ConsoleColor m_ConsoleHue = ConsoleColor.Red;
        private int m_sayHue = 0;
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EventDebug
        {
            get { return m_Debug; }
            set
            {
                if (value == true)
                {
                    if (m_item != null)
                    {
                        m_ConsoleHue = Utility.RandomConsoleColor(m_item.Serial.Value);
                        m_sayHue = Utility.RandomSpeechHue();
                        m_item.SendSystemMessage("debug on.", AccessLevel.GameMaster, m_sayHue);
                    }
                }
                else if (m_item != null)
                    m_item.SendSystemMessage("debug off.", AccessLevel.GameMaster, m_sayHue);
                m_Debug = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool TimerRunning
        {
            get { return m_Timer == null ? false : m_Timer.Running; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EventRunning
        {
            get
            {
                DateTime yourTime = AdjustedDateTime.GetTime(m_OffsetFromUTC);
                if (yourTime >= EventStartInternal && yourTime < EventEndInternal)
                    return true;
                else
                    return false;
            }
        }
        private DateTime EventStartInternal
        {
            get { return m_EventStart; }
            set { m_EventStart = value; }
        }
        private DateTime EventEndInternal
        {
            get { return m_EventEnd; }
            set { m_EventEnd = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string EventStart
        {
            get { return EventStartInternal.ToString(); }
            set
            {
                if (string.IsNullOrEmpty(value))
                    value = DateTime.MinValue.ToString();
                EventStartInternal = DateTime.Parse(value);
                TimeSpan duration = EventEndInternal - EventStartInternal;
                TimeSpan countdown = EventStartInternal - AdjustedDateTime.GetTime(m_OffsetFromUTC);
                Duration = duration;
                Countdown = countdown;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string EventEnd
        {
            get { return EventEndInternal.ToString(); }
            set
            {
                if (string.IsNullOrEmpty(value))
                    value = DateTime.MinValue.ToString();
                EventEndInternal = DateTime.Parse(value);
                TimeSpan duration = EventEndInternal - EventStartInternal;
                TimeSpan countdown = EventStartInternal - AdjustedDateTime.GetTime(m_OffsetFromUTC);
                Duration = duration;
                Countdown = countdown;
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public int OffsetFromUTC
        {
            get { return m_OffsetFromUTC; }
            set
            {
                bool change = m_OffsetFromUTC != value;
                m_OffsetFromUTC = value;
                CheckReset(change);
                CalculatedMetrics(change);
                CheckTimer();
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public TimeSpan Countdown
        {
            get { return m_Countdown; }
            set
            {
                bool change = m_Offset != value;
                m_Offset = m_Countdown = value;
                CheckReset(change);
                CalculatedMetrics(change);
                CheckTimer();
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public string EventTimeRemaining
        {
            get
            {
                if (EventRunning)
                {
                    DateTime yourTime = AdjustedDateTime.GetTime(m_OffsetFromUTC);
                    return (m_EventEnd - yourTime).ToString(@"dd\.hh\:mm\:ss");
                }
                else
                    return "(not yet begun)";
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public TimeSpan Duration
        {
            get { return m_Duration; }
            set
            {
                if (value > TimeSpan.FromDays(20) && !m_durationOverride)
                {
                    m_item.SendSystemMessage("An event may last no longer than 20 days.", AccessLevel.GameMaster, m_sayHue);
                    m_item.SendSystemMessage("If your event needs to be longer, get a shard Owner to override this limitation.", AccessLevel.GameMaster, m_sayHue);
                }
                else
                {   // probably don't have to CheckReset() here. Normal timer processing should handle it fine.
                    bool change = m_Duration != value; m_Duration = value; CalculatedMetrics(change); CheckTimer();
                }
            }
        }
        [CommandProperty(AccessLevel.Owner)]
        public bool DurationOverride
        {
            get { return m_durationOverride; }
            set { m_durationOverride = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Timezone
        {
            get
            {
                var allTimeZones = TimeZoneInfo.GetSystemTimeZones();
                var newTimeZone = allTimeZones.FirstOrDefault(x => x.BaseUtcOffset == new TimeSpan(m_OffsetFromUTC, 0, 0));
                var actual = newTimeZone.StandardName;
                return actual;
            }
        }
        private void Tick(object state)
        {
            if (m_item.Deleted)
            {
                KillTimer();
                return;
            }
            if (m_Countdown >= TimerMode()) // only one minute granularity usually
                m_Countdown -= TimerMode(); // speeds up nearing EventStart for accuracy
            else
                m_Countdown = TimeSpan.Zero;

            if (this.EventDebug && EventRunning == false)
                DebugOut(String.Format("Event timer tick. Countdown = {0}", m_Countdown), m_ConsoleHue);
            else if (this.EventDebug && EventRunning == true)
                DebugOut(String.Format("Event timer tick. Checking for begin/end of event"), m_ConsoleHue);

            CheckTimer();
            CheckBeginEnd();

        }
        private void Initializing(object state)
        {   // called when deserializing 
            //  We need this delay to Start/Stop events as we want to ensure the object
            //      we are managing is completely initialized.
            if (m_item.Deleted)
            {
                KillTimer();
                return;
            }
            // start the timers and begin/end event
            CheckTimer();
            CheckBeginEnd();
        }
        private void CheckReset(bool change)
        {   // if someone changes certain fields after the event has started, we reset the event.
            if (change == false)
                return;
            if (EventRunning)
            {
                EventEnded();           // end this event
                m_Running = false;      // we will call start again when we reach countdown
                m_item.SendSystemMessage("You have canceled this event and rescheduled it.", AccessLevel.GameMaster, m_sayHue);
            }
        }
        private void CheckBeginEnd()
        {
            DateTime yourTime = AdjustedDateTime.GetTime(m_OffsetFromUTC);
            if (yourTime >= EventStartInternal && yourTime < EventEndInternal)
            {
                if (m_Running == false)
                {
                    m_Running = true;
                    EventStarted();
                }
                else if (this.EventDebug)
                    DebugOut(String.Format("Event running with {0} remaining", EventTimeRemaining), m_ConsoleHue);
            }

            if (yourTime > EventEndInternal)
            {
                m_Running = false;
                m_Offset = TimeSpan.Zero;
                EventEnded();
            }
        }
        private void RepairCountdown()
        {   // under normal circumstances (no server restart) countdown won't ever be fractional, or less than zero
            //  with a server restart, this uglies up the countdown timer, so we pretty it up here
            DateTime yourTime = AdjustedDateTime.GetTime(m_OffsetFromUTC);
            if (yourTime >= EventStartInternal && yourTime < EventEndInternal)
            {   // we're already running
                m_Countdown = TimeSpan.Zero;
            }

            if (yourTime > EventEndInternal)
            {   // we've already ended
                m_Countdown = TimeSpan.Zero;
            }
            // remove the fractional bits
            m_Countdown = TimeSpan.FromMinutes(m_Countdown.Minutes);
        }
        private void ResetTimer(bool start)
        {
            bool stop = !start;

            if (stop && TimerRunning == false)
                // all good, we're already stopped
                return;
            else if (stop)
            {
                KillTimer();
                return;
            }

            if (start && TimerRunning == false)
            {
                m_Timer = Timer.DelayCall(TimerMode(), new TimerStateCallback(Tick), new object[] { null });
                return;
            }
            else if (start)
                // all good, we're already running
                return;
        }
        private TimeSpan TimerMode()
        {
            if (m_Countdown < TimeSpan.FromMinutes(3) && !EventRunning) // getting close, maybe time to speed up
                return TimeSpan.FromMilliseconds(500);                  // fast timer
            else
                return TimeSpan.FromMinutes(1);                         // slow timer
        }
        private void KillTimer()
        {
            if (m_Timer != null && m_Timer.Running)
            {
                m_Timer.Stop();
                m_Timer.Flush();
                m_Timer = null;
            }
        }
        private void CheckTimer()
        {
            if (m_item.Deleted)
            {
                KillTimer();
                return;
            }
            DateTime yourTime = AdjustedDateTime.GetTime(m_OffsetFromUTC);
            if (yourTime >= EventStartInternal && yourTime < EventEndInternal)
            {
                ResetTimer(start: true);
                // don't report the new timer if we are already running
                if (this.EventDebug && EventRunning == false)
                    DebugOut(String.Format("Event timer started."), m_ConsoleHue);
            }

            if (yourTime > EventEndInternal)
            {
                ResetTimer(start: false);
                if (this.EventDebug)
                    DebugOut(String.Format("Event timer stopped."), m_ConsoleHue);
            }

            // this keeps us ticking
            if (yourTime < EventStartInternal)
                ResetTimer(start: true);
        }
        private void CalculatedMetrics(bool change)
        {
            if (change)
            {
                // handle the overflow
                m_EventStart = Utility.DTOverflowHandler(AdjustedDateTime.GetTime(m_OffsetFromUTC), m_Offset);
                DateTime temp;
                try
                {   // handle the overflow
                    temp = Utility.DTOverflowHandler(AdjustedDateTime.GetTime(m_OffsetFromUTC), m_Offset + m_Duration);
                }
                catch
                {
                    temp = DateTime.MaxValue;
                }
                m_EventEnd = temp;
                m_Countdown = m_Offset;
            }
        }
        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)1);   // version

            // version 1
            // note: don't use delta time here as it negates the passage of time. For events, we want the passage of time to stand.
            writer.Write(DateTime.UtcNow);
            writer.Write(m_Duration);
            writer.Write(m_Running);
            writer.Write(m_OffsetFromUTC);
            writer.Write(m_Offset);
            writer.Write(m_startedCallbackName);
            writer.Write(m_endedCallbackName);
            writer.Write(m_durationOverride);
            writer.Write(m_EventStart);
            writer.Write(m_EventEnd);
        }
        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        DateTime temp = reader.ReadDateTime();
                        m_Countdown = DateTime.UtcNow - temp;
                        m_Duration = reader.ReadTimeSpan();
                        m_Running = reader.ReadBool();
                        m_OffsetFromUTC = reader.ReadInt();
                        m_Offset = reader.ReadTimeSpan();
                        m_startedCallbackName = reader.ReadString();
                        m_endedCallbackName = reader.ReadString();
                        m_durationOverride = reader.ReadBool();
                        m_EventStart = reader.ReadDateTime();
                        m_EventEnd = reader.ReadDateTime();
                        break;
                    }
            }
            RepairCountdown();              // fixup the countdown to account for elapsed downtime

            //  Okay, see if we need to Start/Stop an event
            //  We need this delay to Start/Stop events as we want to ensure the object
            //      we are managing is completely initialized.
            Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerStateCallback(Initializing), new object[] { null });
        }
        private void EventStarted()
        {
            if (!string.IsNullOrEmpty(m_startedCallbackName))
            {
                MethodInfo mis = m_item.GetType().GetMethod(m_startedCallbackName);
                mis.Invoke(m_item, new object[] { m_context });
            }
            if (this.EventDebug)
                DebugOut(String.Format("Event started."), m_ConsoleHue);
        }
        private void EventEnded()
        {
            if (!string.IsNullOrEmpty(m_endedCallbackName))
            {
                MethodInfo mie = m_item.GetType().GetMethod(m_endedCallbackName);
                mie.Invoke(m_item, new object[] { m_context });
            }
            if (this.EventDebug)
                DebugOut(String.Format("Event ended."), m_ConsoleHue);
        }
        private void DebugOut(string text, ConsoleColor color)
        {
            Utility.ConsoleWriteLine(text, color);
            if (m_item != null) m_item.SendSystemMessage(text, AccessLevel.GameMaster, m_sayHue);
        }
        public override string ToString()
        {
            return "...";
        }
    }
}