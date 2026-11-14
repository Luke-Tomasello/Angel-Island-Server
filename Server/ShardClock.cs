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

/* Server\ShardClock.cs
 * ChangeLog
 *  11/8/26, Adam
 *      Describe(zone): a zone's display name (ZoneName uses it; so does an Event's own EventTimeZone).
 *      AdjustedDateTime deleted: its last users (AutomatedEventSystem, Event, AutoRestart, [time, GameTimeFileStamp,
 *      Patcher, PlayerMobile's report log) keep times in UTC and show them with ShardClock. Its history follows.
 *
 *  AdjustedDateTime history (Server/AdjustedDateTime.cs, before that Utility.cs)
 *  10/6/2026
 *      Moved from Utility.cs as part of a structural cleanup; no behavior change.
 *      Game-time / server-time conversion (AdjustedDateTime).
 *  3/25/08, Pix
 *      Added AdjustedDateTime class. (Utility.cs changelog)
 *  What it was: GameTime = UTC-8 ("PST Is the Pacific Time Zone"), plus an hour when the server machine's own zone
 *  was in daylight saving; GameTimeSansDst = the same without daylight saving ("flattened" Pacific time, so an hourly
 *  timer isn't upset by a daylight-saving change; never for display); ServerTime = DateTime.UtcNow; CronTick = true
 *  once per UTC minute; GameTimezone / ServerTimezone = the first system zone whose base offset matched (-8 / 0),
 *  which is how the banner came to say "British Columbia Standard Time". GetTime(offset) dropped the value's
 *  DateTimeKind, so a save converted it as the machine's local time.
 *
 *  11/7/26, Adam
 *      Created. The shard's time zone: the zone its operator schedules in (events, wall-clock cron jobs, times shown to
 *      players and staff). Set with the startup switch -timezone=<zone>: an IANA name (America/Los_Angeles,
 *      Europe/Berlin: no spaces, so it survives the restart's argument rebuild) or a fixed offset (UTC, UTC-5, UTC+5:30;
 *      no daylight saving). Not set: the server machine's own zone. A server may run anywhere: nothing here assumes
 *      Pacific time.
 *      The rule it serves: every time is stored and compared in UTC (Serialization.cs writes UTC ticks, 8/18/22), so a
 *      world runs the same on any machine. The shard's zone is used only at the edges: showing a time (ToShard, Now)
 *      and turning a wall-clock time into a UTC moment (ToUtc).
 *      Replaces AdjustedDateTime (fixed UTC-8, daylight saving taken from the machine's own zone, zone names guessed by
 *      offset) as its users move over (done 11/8/26).
 */

using System;
using System.Text.RegularExpressions;

namespace Server
{
    public static class ShardClock
    {
        private static TimeZoneInfo m_Zone = TimeZoneInfo.Local;
        private static string? m_Switch;        // the -timezone value as given (null: not set)
        private static string? m_SwitchError;   // why that value was refused (null: none)

        #region The zone
        /// <summary>The shard's time zone: -timezone, else the server machine's own zone.</summary>
        public static TimeZoneInfo Zone { get { return m_Zone; } }

        /// <summary>True when -timezone set the zone (false: the server machine's zone).</summary>
        public static bool FromSwitch { get { return m_Switch != null && m_SwitchError == null; } }

        /// <summary>Why the -timezone value was refused (null: it wasn't). Main stops startup on it.</summary>
        public static string? SwitchError { get { return m_SwitchError; } }

        /// <summary>
        /// For display: "America/Los_Angeles (Pacific Standard Time)", "UTC-05:00", or the machine's zone with
        /// "(server local)".
        /// </summary>
        public static string ZoneName
        {
            get
            {
                string name = Describe(m_Zone);
                return FromSwitch ? name : name + " (server local)";
            }
        }

        /// <summary>A zone for display: "Europe/Paris (Central European Standard Time)", "UTC+01:00".</summary>
        public static string Describe(TimeZoneInfo zone)
        {
            return string.Equals(zone.Id, zone.StandardName, StringComparison.OrdinalIgnoreCase)
                ? zone.Id : string.Format("{0} ({1})", zone.Id, zone.StandardName);
        }

        /// <summary>The zone's name at a moment: its standard or daylight name ("Pacific Daylight Time").</summary>
        public static string NameAt(DateTime utc)
        {
            return m_Zone.IsDaylightSavingTime(AsUtc(utc)) ? m_Zone.DaylightName : m_Zone.StandardName;
        }

        /// <summary>
        /// Sets the zone from the -timezone value (Main, while reading the startup switches). A value it can't read
        /// leaves the machine's zone in place and records why (SwitchError).
        /// </summary>
        public static void SetFromSwitch(string value)
        {
            m_Switch = value;
            TimeZoneInfo? zone = Parse(value, out string? error);
            if (zone != null)
            {
                m_Zone = zone;
                m_SwitchError = null;
            }
            else
            {
                m_Zone = TimeZoneInfo.Local;
                m_SwitchError = error;
            }
        }

        private static readonly Regex Offset = new Regex(@"^UTC(?:(?<sign>[+-])(?<h>\d{1,2})(?::?(?<m>\d{2}))?)?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// A zone from its name: "UTC", "UTC-5", "UTC+5:30" (fixed, no daylight saving), or a system zone by IANA name
        /// ("America/Los_Angeles") or Windows id. Null, with the reason, if there is none.
        /// </summary>
        public static TimeZoneInfo? Parse(string value, out string? error)
        {
            error = null;
            value = (value ?? "").Trim();
            if (value.Length == 0)
            {
                error = "no zone given (e.g. -timezone=America/Los_Angeles, -timezone=UTC-5)";
                return null;
            }

            Match m = Offset.Match(value);
            if (m.Success)
            {
                if (!m.Groups["sign"].Success)
                    return TimeZoneInfo.Utc;
                int hours = int.Parse(m.Groups["h"].Value), minutes = m.Groups["m"].Success ? int.Parse(m.Groups["m"].Value) : 0;
                TimeSpan offset = new TimeSpan(hours, minutes, 0);
                if (minutes > 59 || offset > TimeSpan.FromHours(14))
                {
                    error = string.Format("'{0}' is not an offset from UTC (-14:00 to +14:00)", value);
                    return null;
                }
                if (m.Groups["sign"].Value == "-")
                    offset = offset.Negate();
                string id = string.Format("UTC{0}{1:hh\\:mm}", offset < TimeSpan.Zero ? "-" : "+", offset.Duration());
                return TimeZoneInfo.CreateCustomTimeZone(id, offset, id, id);
            }

            // as given, then with each part capitalized (IANA names are case-sensitive on some lookups:
            //  "europe/berlin" -> "Europe/Berlin")
            foreach (string id in new[] { value, Capitalized(value) })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (Exception ex) when (ex is TimeZoneNotFoundException || ex is InvalidTimeZoneException)
                {
                }
            }

            error = string.Format("'{0}' is not a time zone this computer knows (use an IANA name such as America/Los_Angeles, or UTC, UTC-5, UTC+5:30)", value);
            return null;
        }

        // "america/los_angeles" -> "America/Los_Angeles": the first letter of each part (between '/', '_' and '-')
        private static string Capitalized(string value)
        {
            char[] c = value.ToLowerInvariant().ToCharArray();
            for (int i = 0; i < c.Length; i++)
                if (i == 0 || c[i - 1] == '/' || c[i - 1] == '_' || c[i - 1] == '-')
                    c[i] = char.ToUpperInvariant(c[i]);
            return new string(c);
        }
        #endregion The zone

        #region Conversions
        /// <summary>Now, in shard time (for display).</summary>
        public static DateTime Now { get { return ToShard(DateTime.UtcNow); } }

        /// <summary>A UTC moment in shard time (for display). MinValue and MaxValue pass through unchanged.</summary>
        public static DateTime ToShard(DateTime utc)
        {
            return ToShard(utc, m_Zone);
        }

        /// <summary>A UTC moment as wall-clock time in the given zone. MinValue and MaxValue pass through unchanged.</summary>
        public static DateTime ToShard(DateTime utc, TimeZoneInfo zone)
        {
            if (utc == DateTime.MinValue || utc == DateTime.MaxValue)
                return utc;
            return TimeZoneInfo.ConvertTimeFromUtc(AsUtc(utc), zone);
        }

        /// <summary>
        /// A shard wall-clock time as a UTC moment (to schedule it). On a daylight-saving day, a time that doesn't
        /// exist (the skipped hour) moves forward by the change; a time that happens twice (the repeated hour) is the
        /// first one. MinValue and MaxValue pass through unchanged.
        /// </summary>
        public static DateTime ToUtc(DateTime shardTime)
        {
            return ToUtc(shardTime, m_Zone);
        }

        /// <summary>A wall-clock time in the given zone as a UTC moment, with ToUtc's daylight-saving rules.</summary>
        public static DateTime ToUtc(DateTime wallTime, TimeZoneInfo zone)
        {
            if (wallTime == DateTime.MinValue || wallTime == DateTime.MaxValue)
                return DateTime.SpecifyKind(wallTime, DateTimeKind.Utc);

            DateTime wall = DateTime.SpecifyKind(wallTime, DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(wall))
            {   // the skipped hour: the clocks jumped over it, so it happens that much later
                TimeSpan before = zone.GetUtcOffset(DateTime.SpecifyKind(wall.AddHours(-3), DateTimeKind.Unspecified));
                TimeSpan after = zone.GetUtcOffset(DateTime.SpecifyKind(wall.AddHours(3), DateTimeKind.Unspecified));
                wall = wall + (after - before);
            }

            TimeSpan offset;
            if (zone.IsAmbiguousTime(wall))
            {   // the repeated hour: the first time through it is the larger offset (daylight time, still in effect)
                TimeSpan[] offsets = zone.GetAmbiguousTimeOffsets(wall);
                offset = offsets[0] > offsets[offsets.Length - 1] ? offsets[0] : offsets[offsets.Length - 1];
            }
            else
                offset = zone.GetUtcOffset(wall);

            return DateTime.SpecifyKind(wall - offset, DateTimeKind.Utc);
        }

        // a DateTime the caller means as UTC (an unmarked value is taken as UTC, never as the machine's local time)
        private static DateTime AsUtc(DateTime t)
        {
            return t.Kind == DateTimeKind.Utc ? t : t.Kind == DateTimeKind.Local ? t.ToUniversalTime() : DateTime.SpecifyKind(t, DateTimeKind.Utc);
        }
        #endregion Conversions
    }
}
