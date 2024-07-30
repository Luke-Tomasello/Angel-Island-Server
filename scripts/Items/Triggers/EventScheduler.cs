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

/* Items/Triggers/Core/EventScheduler.cs
 * CHANGELOG:
 * 	3/17/24, Adam
 * 		Initial version.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Items.Triggers
{
    [NoSort]
    public class EventScheduler : Item
    {
        public static SortedDictionary<string, EventScheduler> EventSchedulerDatabase = new();

        #region Flags
        [Flags]
        public enum TZEnablement : byte
        {
            None = 0x0,
            TZ1 = 0x01,
            TZ2 = 0x02,
            TZ3 = 0x04,
        }
        TZEnablement m_TZFlags;
        public bool GetFlag(TZEnablement flag)
        {
            return ((m_TZFlags & flag) != 0);
        }
        public void SetFlag(TZEnablement flag, bool value)
        {
            if (value)
                m_TZFlags |= flag;
            else
                m_TZFlags &= ~flag;
        }
        #endregion Flags

        public override string DefaultName { get { return "Event Scheduler"; } }

        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get
            {
                return m_Event;
            }
            set
            {
                if (value != null)
                {
                    value = value.ToLower();
                    if (ScheduleExists(value))
                        this.SendSystemMessage(string.Format("{0} already defines the '{1}' event.", EventSchedulerDatabase[value], value));
                    else
                    {
                        AddSchedule(m_Event = value);
                        this.SendSystemMessage(string.Format("Now scheduling for the '{0}' event.", value));
                    }
                }
                else
                {   // deschedule if appropriate
                    if (ScheduleExists(m_Event))
                    {
                        RemoveSchedule(m_Event);
                        this.SendSystemMessage(string.Format("De-scheduling the '{0}' event.", m_Event));
                        m_Event = value;
                    }
                }
            }
        }
        #region Schedule Management
        private static void DefragSchedule()
        {
            List<string> toDelete = new List<string>();
            foreach (var kvp in EventSchedulerDatabase)
                if (kvp.Value == null || kvp.Value.Deleted)
                    toDelete.Add(kvp.Key);

            foreach (string key in toDelete)
                EventSchedulerDatabase.Remove(key);
        }
        public static bool ScheduleExists(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                DefragSchedule();
                return EventSchedulerDatabase.ContainsKey(name);
            }
            else
                return false;
        }
        private void AddSchedule(string name)
        {
            if (!string.IsNullOrEmpty(name) && !ScheduleExists(name))
                EventSchedulerDatabase.Add(name, this);
        }
        private void RemoveSchedule(string name)
        {
            if (!string.IsNullOrEmpty(name) && ScheduleExists(name))
                EventSchedulerDatabase.Remove(name);
        }
        #endregion Schedule Management
        #region Overrides
        public override void OnDelete()
        {
            if (ScheduleExists(m_Event))
                RemoveSchedule(m_Event);
            base.OnDelete();
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.GameMaster)
            {
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
            }
        }
        #endregion Overrides
        /* ------------- Zone 1 ---------------*/
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EnableOffset1 { get { return GetFlag(TZEnablement.TZ1); } set { SetFlag(TZEnablement.TZ1, value); InvalidateProperties(); } }
        private int m_GMTOffset1;
        [CommandProperty(AccessLevel.GameMaster)]
        public int GMTOffset1 { get { return m_GMTOffset1; } set { m_GMTOffset1 = value; InvalidateProperties(); } }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Zone1 { get { return EnableOffset1 ? EventScheduler.Timezone(m_GMTOffset1) : "(not set)"; } }
        /* ------------- Zone 2 ---------------*/
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EnableOffset2 { get { return GetFlag(TZEnablement.TZ2); } set { SetFlag(TZEnablement.TZ2, value); InvalidateProperties(); } }
        private int m_GMTOffset2;
        [CommandProperty(AccessLevel.GameMaster)]
        public int GMTOffset2 { get { return m_GMTOffset2; } set { m_GMTOffset2 = value; InvalidateProperties(); } }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Zone2 { get { return EnableOffset2 ? EventScheduler.Timezone(m_GMTOffset2) : "(not set)"; } }
        /* ------------- Zone 3 ---------------*/
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EnableOffset3 { get { return GetFlag(TZEnablement.TZ3); } set { SetFlag(TZEnablement.TZ3, value); InvalidateProperties(); } }
        private int m_GMTOffset3;
        [CommandProperty(AccessLevel.GameMaster)]
        public int GMTOffset3 { get { return m_GMTOffset3; } set { m_GMTOffset3 = value; InvalidateProperties(); } }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Zone3 { get { return EnableOffset3 ? EventScheduler.Timezone(m_GMTOffset3) : "(not set)"; } }
        /* ------------- Dates ---------------*/
        private DateTime m_EventStart = DateTime.MinValue;
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime EventStart { get { return m_EventStart; } set { m_EventStart = value; InvalidateProperties(); } }
        private DateTime m_EventEnd = DateTime.MaxValue - TimeSpan.FromDays(1024);
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime EventEnd { get { return m_EventEnd; } set { m_EventEnd = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get
            {
                List<int> offsets = new List<int>();
                if (EnableOffset1) offsets.Add(GMTOffset1);
                if (EnableOffset2) offsets.Add(GMTOffset2);
                if (EnableOffset3) offsets.Add(GMTOffset3);

                DateTime now = DateTime.UtcNow;
                foreach (int offset in offsets)
                {
                    int total_offset = offset + GetDSTOffset(offset);
                    if (now + TimeSpan.FromHours(total_offset) >= m_EventStart && now + TimeSpan.FromHours(total_offset) < m_EventEnd)
                        return true;
                }

                return false;
            }
        }
        #region Time Tools 
        private static int GetDSTOffset(int offset)
        {
            if (offset != 0)
            {
                DateTime theirTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, Timezone(offset));
                TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(Timezone(offset));
                if (tst.IsDaylightSavingTime(theirTime))
                    return 1;
            }
            return 0;
        }
        public static string Timezone(int offset)
        {
            var allTimeZones = TimeZoneInfo.GetSystemTimeZones();
            var newTimeZone = allTimeZones.FirstOrDefault(x => x.BaseUtcOffset == new TimeSpan(offset, 0, 0));
            var actual = newTimeZone.StandardName;
            return actual;
        }
        #endregion Time Tools 

        [Constructable]
        public EventScheduler()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;
        }

        public EventScheduler(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write((byte)m_TZFlags);
            writer.Write(GMTOffset1);
            writer.Write(GMTOffset2);
            writer.Write(GMTOffset3);
            writer.Write(m_Event);
            writer.Write(m_EventStart);
            writer.Write(m_EventEnd);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_TZFlags = (TZEnablement)reader.ReadByte();
                        m_GMTOffset1 = reader.ReadInt();
                        m_GMTOffset2 = reader.ReadInt();
                        m_GMTOffset3 = reader.ReadInt();
                        m_Event = reader.ReadString();
                        m_EventStart = reader.ReadDateTime();
                        m_EventEnd = reader.ReadDateTime();
                        break;
                    }
            }

            if (m_Event != null)
                if (!ScheduleExists(m_Event))
                    AddSchedule(m_Event);
                else
                    // only one event controller per event
                    m_Event = null;
        }
    }
}