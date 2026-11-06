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


/* Server/AdjustedDateTime.cs
 * ChangeLog:
 *  10/6/2026
 *      Moved from Utility.cs as part of a structural cleanup; no behavior change.
 *      Game-time / server-time conversion (AdjustedDateTime).
 */

using System;
using System.Linq;
namespace Server
{
    #region GameTime
    // PST Is the Pacific Time Zone
    // In everyday usage, PST is often referred to as Pacific Time(PT) or the Pacific Time Zone.
    // This can add a bit of confusion as the term Pacific Time does not differentiate between standard time and Daylight Saving Time,
    // so Pacific Time switches between PST and PDT in areas that use DST during part of the year.
    // https://www.timeanddate.com/time/zones/pst
    public class AdjustedDateTime
    {
        private const int GameTimeOffset = -8;  // UTC -8 == pacific time

        public static string GameTimezone
        {
            get
            {
                var allTimeZones = TimeZoneInfo.GetSystemTimeZones();
                var newTimeZone = allTimeZones.FirstOrDefault(x => x.BaseUtcOffset == new TimeSpan(GameTimeOffset, 0, 0));
                var actual = newTimeZone.StandardName;
                return actual;
            }
        }
        public static string ServerTimezone
        {
            get
            {
                var allTimeZones = TimeZoneInfo.GetSystemTimeZones();
                var newTimeZone = allTimeZones.FirstOrDefault(x => x.BaseUtcOffset == new TimeSpan(0, 0, 0));
                var actual = newTimeZone.StandardName;
                return actual;
            }
        }

        // GameTimeSansDst is centered around a flattened Pacific Time.
        //  Flattening removes the effects of daylight savings time. 
        //  This flattening keeps game time linear which is important for Cron Jobs. E.g.,
        //  If you want something to fire every hour, a change in DST will mess that up.
        //  a side-effect of this is that part of the year,
        //  during DST, GameTimeSansDst and Pacific time will diverge by one hour.
        //  GameTimeSansDst should not be used for any display purposes, it's purely a timing function
        //  See: GameTime below
        public static DateTime GameTimeSansDst
        {   // Flatten time: time that is not adjusted for daylight savings time
            //  by adding 1 hour to the time when we enter DST
            get { return GetTime(GameTimeOffset, sansDST: true); }
        }
        // GameTime is the time we use for things like scheduled events and it's the time we display
        //  to the user ([time command,) and how the AES displays an events progression
        //  Note: the edge case here is where DST changes in the middle of an AES event. In this case
        //  we should continue to display GameTime, but maintain timers on GameTimeSansDst.
        // See: GameTimeSansDst above
        public static DateTime GameTime { get { return GetTime(GameTimeOffset, sansDST: false); } }
        // ServerTime is the time in whatever timezone the server is running in
        public static DateTime ServerTime { get { return DateTime.UtcNow; } }
        // CronTick is guaranteed to only tick once per minute
        static int lastCronMinute = DateTime.UtcNow.Minute;
        public static bool CronTick
        {
            get
            {
                int minute = DateTime.UtcNow.Minute;
                bool change = minute != lastCronMinute;
                lastCronMinute = minute;
                return change;
            }
        }
        public static DateTime GetTime(int offset, bool sansDST = false)
        {
            DateTime localTime = DateTime.UtcNow.AddHours(offset);
            if (!sansDST && TimeZoneInfo.Local.IsDaylightSavingTime(localTime))
                localTime = localTime.AddHours(1);

            // we need to switch the 'Kind' away from Utc
            return new DateTime(localTime.Ticks);
        }
    }
    #endregion GameTime
}
