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

using System;

namespace Server.Misc
{
    public class SeasonsHelper
    {
        public static void Initialize()
        {
        }

        public enum SeasonRequest
        {
            Reset,
            Countdown
        }

        /// <summary>
        /// Returns formatted information about the current/next season reset.
        /// Supports two main request types:
        /// - Reset:     the exact date/time of the next season reset
        /// - Countdown: a human-readable or structured countdown until the next reset
        /// 
        /// The returned string is formatted according to the <paramref name="format"/> parameter.
        /// 
        /// Supported format tokens (case-sensitive):
        ///   {days}     - total whole days remaining (no padding)
        ///   {days2}    - total days, zero-padded to 2 digits
        ///   {days3}    - total days, zero-padded to 3 digits
        ///   {hours}    - hours component (0–23, no padding)
        ///   {hours2}   - hours, zero-padded to 2 digits
        ///   {minutes}  - minutes component (0–59, no padding)
        ///   {minutes2} - minutes, zero-padded to 2 digits
        ///   {seconds}  - seconds component (0–59, no padding)
        ///   {seconds2} - seconds, zero-padded to 2 digits
        /// 
        /// Examples of valid format strings and possible outputs (assuming next reset is in 7 days, 14 hours, 35 minutes, 12 seconds):
        /// 
        ///   Format string                        →  Example output
        ///   -------------------------------------------------------------------------
        ///   "{days} days left"                   →  "7 days left"
        ///   "{days2}d {hours2}h {minutes2}m"     →  "07d 14h 35m"
        ///   "{days3}:{hours2}:{minutes2}:{seconds2}" →  "007:14:35:12"
        ///   "{days} days, {hours} hours"         →  "7 days, 14 hours"
        ///   "Reset in {days2} days"              →  "Reset in 07 days"
        ///   "{hours2}:{minutes2}"                →  "14:35"
        ///   "Season resets on {days} days"       →  "Season resets on 7 days"  (countdown mode)
        /// 
        /// When request = SeasonRequest.Reset:
        ///   The format string is passed directly to DateTime.ToString(format)
        ///   Common useful formats:
        ///     "MMMM d, yyyy 'at' h:mm tt"      →  "March 21, 2026 at 12:00 AM"
        ///     "yyyy-MM-dd HH:mm:ss"            →  "2026-03-21 00:00:00"
        ///     "ddd, MMM d"                     →  "Sat, Mar 21"
        /// 
        /// Returns:
        ///   - A formatted string according to the request and format
        ///   - "(format error)" on invalid format string, parsing error, or unsupported request
        /// </summary>
        /// <param name="request">Reset = next reset date/time | Countdown = time remaining</param>
        /// <param name="format">Format string containing supported tokens (for Countdown) or standard DateTime format (for Reset)</param>
        /// <returns>Formatted string or error message</returns>
        public static string SeasonInfo(SeasonRequest request, string format)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(format))
                    return "(format error)";

                DateTime now = DateTime.UtcNow;
                DateTime nextReset = GetNextSeasonReset(now);

                switch (request)
                {
                    case SeasonRequest.Reset:
                        return nextReset.ToString(format) + " UTC";

                    case SeasonRequest.Countdown:
                        {
                            TimeSpan ts = nextReset - now;

                            if (ts < TimeSpan.Zero)
                                ts = TimeSpan.Zero;

                            return ApplyCountdownFormat(format, ts);
                        }

                    default:
                        return "(format error)";
                }
            }
            catch
            {
                return "(format error)";
            }
        }

        public static DateTime GetNextSeasonReset(DateTime now)
        {
            int year = now.Year;

            // Astronomical-style season boundaries for the shard model:
            // Spring  = Mar 21
            // Summer  = Jun 22
            // Autumn  = Sep 23
            // Winter  = Dec 22
            //
            // Shard/world time is represented in UTC for consistency across
            // scheduling, persistence, countdowns, and seasonal reset logic.
            // Resets occur at UTC midnight on these dates
            DateTime[] candidates =
            {
                new DateTime(year, 3, 21, 0, 0, 0, now.Kind),
                new DateTime(year, 6, 22, 0, 0, 0, now.Kind),
                new DateTime(year, 9, 23, 0, 0, 0, now.Kind),
                new DateTime(year, 12, 22, 0, 0, 0, now.Kind)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (now < candidates[i])
                    return candidates[i];
            }

            return new DateTime(year + 1, 3, 21, 0, 0, 0, now.Kind);
        }

        private static string ApplyCountdownFormat(string format, TimeSpan ts)
        {
            // Supported tokens:
            // {days}       -> total whole days remaining
            // {hours}      -> hour component
            // {minutes}    -> minute component
            // {seconds}    -> second component
            //
            // Zero-padded variants:
            // {days2}      {days3}
            // {hours2}
            // {minutes2}
            // {seconds2}
            //
            // Examples:
            // "{days} days"
            // "{days}d {hours}h"
            // "{days3}:{hours2}:{minutes2}"
            // "{days} days, {hours} hours, {minutes} minutes"

            int totalDays = (int)Math.Floor(ts.TotalDays);

            return format
                .Replace("{days3}", totalDays.ToString("000"))
                .Replace("{days2}", totalDays.ToString("00"))
                .Replace("{days}", totalDays.ToString())
                .Replace("{hours2}", ts.Hours.ToString("00"))
                .Replace("{hours}", ts.Hours.ToString())
                .Replace("{minutes2}", ts.Minutes.ToString("00"))
                .Replace("{minutes}", ts.Minutes.ToString())
                .Replace("{seconds2}", ts.Seconds.ToString("00"))
                .Replace("{seconds}", ts.Seconds.ToString());
        }
    }
}