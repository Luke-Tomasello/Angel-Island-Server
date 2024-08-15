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

/* scripts\Commands\AccessLevel\EventManager.cs
 * Changelog
 *	10/16/22, Adam
 *		Initial creation.
 *		1. Allow Seers+ to add (Event)teleporters, spawners, and Sungates to a named event
 *		2. Allow Seers+ to 'Start' the named event
 */

using Server.Engines;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Commands
{
    public class EventManager
    {
        private static Dictionary<string, List<object>> m_table = new();
        public static void Initialize()
        {
            Server.CommandSystem.Register("Event", AccessLevel.Seer, new CommandEventHandler(Event_OnCommand));
        }
        public static void EventHelp(Mobile from, string text)
        {
            if (from != null)
            {
                from.SendMessage("Error: Bad or missing '{0}'", text);
                from.SendMessage("Usage: Event list");
                from.SendMessage("Usage: Event <add> <eventname> <target object>");
                from.SendMessage("Usage: Event <run> <eventname> <countdown> <duration>");
                from.SendMessage("Usage: Event <run> <eventname> <DateTimeStart> <DateTimeStop>");
                from.SendMessage("Example1: Event RUN LastStanding 5/18/2023 5 PM 5/18/2023 9:30 PM");
                from.SendMessage("Example2: Event RUN LastStanding 0:0:20 1:0:0");
                from.SendMessage("Example3: Event DELETE LastStanding");
            }
        }
        private static bool LooksLikeDateTime(CommandEventArgs e, int index)
        {
            return
                !string.IsNullOrEmpty(e.GetString(index)) &&
                !string.IsNullOrEmpty(e.GetString(index + 1)) &&
                !string.IsNullOrEmpty(e.GetString(index + 2));
        }
        public static object ParseTimeObject(CommandEventArgs e, ref int index)
        {
            // try for the date time ([m/d/y] [h/m/s] [am/pm])
            if (LooksLikeDateTime(e, index))
            {
                var dt = DateTime.MinValue; bool dt_ok = DateTime.TryParse(e.GetString(index) + " " + e.GetString(index + 1) + " " + e.GetString(index + 2) + " ", out dt);
                if (dt_ok) { index += 3; return dt; }
            }

            // try for the timespan ([d:h:m:s])
            var ts = TimeSpan.Zero; bool ts_ok = Utility.TryParseTimeSpan(e.GetString(index), out ts);
            if (ts_ok) { index++; return ts; }
            return null;
        }

        [Usage("Event <eventname> <add> | [ <run> <countdown> [Duration] ]")]
        [Description("Configures or Runs the named event.")]
        public static void Event_OnCommand(CommandEventArgs e)
        {
            Defrag();
            Mobile from = e.Mobile;
            /*
             * Event <command> [<eventnamne>]
             *  Commands: Add, Run, List, Delete
             *  Usage: [Event list
             *  Usage: [Event <add> <eventname> <target object>
             *  Usage: [Event <run> <eventname> <countdown> <duration>
             *  Usage: [Event <run> <eventname> <DateTimeStart> <DateTimeStop>
             *  Example1: [Event RUN LastStanding 5/18/2023 5 PM 5/18/2023 9:30 PM
             *  Example2: [Event RUN LastStanding 0:0:20 1:0:0
             *  Example3: [Event DELETE LastStanding
             */
            int index = 0;
            string command = e.GetString(index++).ToLower(); bool command_okay = !string.IsNullOrEmpty(command);
            string eventName = e.GetString(index++).ToLower(); bool eventName_okay = !string.IsNullOrEmpty(eventName);
            object countdown = ParseTimeObject(e, ref index); bool countdown_ok = (countdown != null);
            object duration = ParseTimeObject(e, ref index); bool duration_ok = (duration != null);

            if (command_okay && e.Length == 1)
            {
                if (command == "list")
                {
                    if (m_table.Count > 0)
                    {
                        foreach (var kvp in m_table)
                        {
                            from.SendMessage(kvp.Key);
                            foreach (object o in kvp.Value)
                                from.SendMessage(string.Format("-- {0} ({1})",
                                    (o as Item).GetType().Name,
                                    (o as Item).GetWorldLocation()));
                            from.SendMessage("{0} objects comprise this event", kvp.Value.Count);
                        }
                        from.SendMessage("done.");
                    }
                    else
                        from.SendMessage("There are no defined events.");
                }
                else
                {   // bad command
                    EventHelp(from, "command");
                    return;
                }
            }
            else if (eventName_okay && command_okay)
            {
                if (command == "add")
                {
                    from.SendMessage("Target the item you wish to add.");
                    from.Target = new AddEventTarget(eventName);
                    return;
                }
                else if (command == "run")
                {   // for reporting
                    TimeSpan ts_countdown = TimeSpan.Zero;
                    TimeSpan ts_duration = TimeSpan.Zero;
                    // make sure both countdown and duration are correct before proceeding
                    if (!countdown_ok || !duration_ok)
                    {   // bad or missing countdown or duration
                        if (!countdown_ok)
                            EventHelp(from, "countdown");
                        else
                            EventHelp(from, "duration");
                        return;
                    }
                    else if (!m_table.ContainsKey(eventName))
                    {
                        EventHelp(from, "event name");
                        return;
                    }
                    else
                    {   // set the countdown
                        bool found = false;
                        foreach (var kvp in m_table)
                            if (kvp.Key == eventName)
                            {
                                found = true;
                                foreach (object o in kvp.Value)
                                    if (o is EventTeleporter teleporter)
                                    {
                                        if (countdown is TimeSpan)
                                            teleporter.Countdown = (TimeSpan)countdown;
                                        else
                                            teleporter.EventStart = countdown.ToString();

                                        ts_countdown = teleporter.Countdown;
                                    }
                                    else if (o is EventKeywordTeleporter keywordteleporter)
                                    {
                                        if (countdown is TimeSpan)
                                            keywordteleporter.Countdown = (TimeSpan)countdown;
                                        else
                                            keywordteleporter.EventStart = countdown.ToString();

                                        ts_countdown = keywordteleporter.Countdown;
                                    }
                                    else if (o is EventSpawner spawner)
                                    {
                                        if (countdown is TimeSpan)
                                            spawner.Countdown = (TimeSpan)countdown;
                                        else
                                            spawner.EventStart = countdown.ToString();

                                        ts_countdown = spawner.Countdown;
                                    }
                                    else if (o is EventSungate sungate)
                                    {
                                        if (countdown is TimeSpan)
                                            sungate.Countdown = (TimeSpan)countdown;
                                        else
                                            sungate.EventStart = countdown.ToString();

                                        ts_countdown = sungate.Countdown;
                                    }
                                    else
                                        Utility.ConsoleWriteLine(string.Format("Logic Error: Unexpected event object {0}. ", o), ConsoleColor.Red);
                            }

                        if (found == false)
                        {
                            from.SendMessage("No event named '{0}' was found.", eventName);
                            return;
                        }
                    }
                    if (duration_ok)
                    {   // set the duration
                        foreach (var kvp in m_table)
                            if (kvp.Key == eventName)
                                foreach (object o in kvp.Value)
                                    if (o is EventTeleporter teleporter)
                                    {
                                        if (from.AccessLevel == AccessLevel.Owner)
                                            teleporter.DurationOverride = true;

                                        if (duration is TimeSpan)
                                            teleporter.Duration = (TimeSpan)duration;
                                        else
                                            teleporter.EventEnd = duration.ToString();

                                        ts_duration = teleporter.Duration;
                                    }
                                    else if (o is EventKeywordTeleporter keywordteleporter)
                                    {
                                        if (from.AccessLevel == AccessLevel.Owner)
                                            keywordteleporter.DurationOverride = true;

                                        if (duration is TimeSpan)
                                            keywordteleporter.Duration = (TimeSpan)duration;
                                        else
                                            keywordteleporter.EventEnd = duration.ToString();

                                        ts_duration = keywordteleporter.Duration;
                                    }
                                    else if (o is EventSpawner spawner)
                                    {
                                        if (from.AccessLevel == AccessLevel.Owner)
                                            spawner.DurationOverride = true;

                                        if (duration is TimeSpan)
                                            spawner.Duration = (TimeSpan)duration;
                                        else
                                            spawner.EventEnd = duration.ToString();

                                        ts_duration = spawner.Duration;
                                    }
                                    else if (o is EventSungate sungate)
                                    {
                                        if (from.AccessLevel == AccessLevel.Owner)
                                            sungate.DurationOverride = true;

                                        if (duration is TimeSpan)
                                            sungate.Duration = (TimeSpan)duration;
                                        else
                                            sungate.EventEnd = duration.ToString();

                                        ts_duration = sungate.Duration;
                                    }
                                    else
                                        Utility.ConsoleWriteLine(string.Format("Logic Error: Unexpected event object {0}. ", o), ConsoleColor.Red);
                    }
                    from.SendMessage("Event '{0}'", eventName);
                    from.SendMessage("Execution in {0}.",
                        string.Format("{0} days, {1} hours, {2} minutes, and {3} seconds", ts_countdown.Days, ts_countdown.Hours, ts_countdown.Minutes, ts_countdown.Seconds));
                    from.SendMessage("Duration for {0}.",
                        string.Format("{0} days, {1} hours, {2} minutes, and {3} seconds", ts_duration.Days, ts_duration.Hours, ts_duration.Minutes, ts_duration.Seconds));
                }
                else if (command == "delete")
                {
                    if (!m_table.ContainsKey(eventName))
                    {
                        EventHelp(from, "event name");
                        return;
                    }
                    else
                    {   // delete event
                        m_table.Remove(eventName);
                        from.SendMessage("event '{0}' deleted.", eventName);
                        return;
                    }
                }
                else
                {   // bad command
                    EventHelp(from, "command");
                    return;
                }
            }
            else
            {
                // Badly formatted
                EventHelp(from, "format");
            }
        }
        private static void Defrag()
        {
            foreach (var element in m_table)
            {
                List<object> list = new List<object>();
                foreach (var obj in element.Value)
                    if (obj is Item item && item.Deleted)
                        list.Add(item);
                foreach (Item ix in list)
                    element.Value.Remove(ix);
            }
        }
        public static bool AddToEvent(Item it, string eventName, Mobile from = null)
        {
            Defrag();
            if (ValidType(it))
            {
                if (m_table.ContainsKey(eventName))
                {
                    if (m_table[eventName] != null)
                    {
                        if (!m_table[eventName].Contains(it))
                            m_table[eventName].Add(it);
                        else
                        {
                            if (from != null)
                                from.SendMessage("That {0} ({1}) is already part of your event.", it.GetType().Name, it.Serial);
                            return false;
                        }
                    }
                    else
                        m_table[eventName] = new List<object>() { it };
                }
                else
                    m_table[eventName] = new List<object>() { it };
            }
            else
            {
                if (from != null)
                    from.SendMessage("That is not a {0} or {1}, {2}, {3}, or {4}.",
                        typeof(EventTeleporter).Name,
                        typeof(EventKeywordTeleporter).Name,
                        typeof(EventSpawner).Name, typeof(PushBackSpawner).Name,
                        typeof(EventSungate).Name);
                return false;
            }

            if (from != null)
                from.SendMessage("{0} ({1}) added to event '{2}'.", it.GetType().Name, it.Serial, eventName);
            return true;
        }
        private static bool ValidType(Item item)
        {
            if (item == null ||
                item.GetType() != typeof(EventTeleporter) &&
                item.GetType() != typeof(EventKeywordTeleporter) &&
                item.GetType() != typeof(EventSpawner) && item.GetType() != typeof(PushBackSpawner) &&
                item.GetType() != typeof(EventSungate))
                return false;
            return true;
        }
        public class AddEventTarget : Target
        {
            string m_eventName;
            public AddEventTarget(object o)
                : base(12, true, TargetFlags.None)
            {
                m_eventName = o as string;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Item it = targeted as Item;
                AddToEvent(it, m_eventName, from);
            }
        }

        #region IO 
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
        }
        public static void Load()
        {
            if (!File.Exists("Saves/EventManager.bin"))
                return;

            Console.WriteLine("EventManager Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/EventManager.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            // EventManager
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                string eventName = reader.ReadString();
                                m_table.Add(eventName, reader.ReadItemList<Item>().Cast<object>().ToList());
                            }
                            break;
                        }

                    default:
                        {
                            reader.Close();
                            throw new Exception("Invalid version in EventManager.bin.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error reading EventManager.bin, using default values:");
                Utility.PopColor();
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("EventManager Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/EventManager.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            writer.Write(m_table.Count);
                            foreach (KeyValuePair<string, List<object>> kvp in m_table)
                            {
                                writer.Write(kvp.Key);
                                writer.WriteItemList(kvp.Value.OfType<Item>().ToList());
                            }
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing EventManager.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion IO 
    }
}