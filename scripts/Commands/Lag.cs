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

/* Scripts/Commands/Lag.cs
 * ChangeLog
 *  11/14/26, Adam (what ran)
 *      The log entry names what ran lately: the main-thread work that took 50 ms or longer in the last 5 minutes
 *      (Diagnostics\SlowWork.cs) and cron's recent tasks with when and how long. Staff get that report in game
 *      (they were told to wait five minutes: the limit is for players, but staff fell into its message).
 *	3/267/08, Adam
 *		Switch from logging last 5 Heartbeat tasks to logging Scheduled tasks
 *			Heartbeat is now obsolete.
 *  11/28/06, plasma
 *      Added last 5 heartbeat tasks to log
 *	8/13/06, Pix
 *		Added console log at Adam's request.
 * 	08/3/06, weaver
 *		Initial creation.
*/
using Server.Diagnostics;
using Server.Engines.CronScheduler;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Commands
{
    public class LagReport
    {

        public static void Initialize()
        {
            Server.CommandSystem.Register("Lag", AccessLevel.Player, new CommandEventHandler(LagReport_OnCommand));
        }

        [Usage("Lag")]
        [Description("Reports lag to the administrators")]
        private static void LagReport_OnCommand(CommandEventArgs arg)
        {
            Mobile from = arg.Mobile;

            if (from.AccessLevel > AccessLevel.Player)
            {   // staff: what ran lately, in game
                foreach (string line in Report(shardTime: true))
                    from.SendMessage(line);
                return;
            }

            if (from is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)from;

                // Limit to 5 minutes between lag reports
                if ((pm.LastLagTime + TimeSpan.FromMinutes(5.0)) < DateTime.UtcNow && pm.AccessLevel == AccessLevel.Player)
                {
                    // Let them log again
                    LogHelper lh = new LogHelper("lagreports.log", false, true);
                    lh.Log(LogType.Mobile, from, string.Join(" ", Report(shardTime: false))); //adam: added schduled tasks! (and the slow work)

                    //Requested by Adam:
                    Console.WriteLine("Lag at: {0}", DateTime.UtcNow.ToShortTimeString());

                    // Update LastLagTime on PlayerMobile
                    pm.LastLagTime = DateTime.UtcNow;

                    lh.Finish();

                    from.SendMessage("The fact that you are experiencing lag has been logged. We will review this with other data to try and determine the cause of this lag. Thank you for your help.");
                }
                else
                {

                    from.SendMessage("It has been less than five minutes since you last reported lag. Please wait five minutes between submitting lag reports.");
                }

            }

        }


        public static readonly TimeSpan ReportSpan = TimeSpan.FromMinutes(5);

        // what ran lately: the slow main-thread work of the last 5 minutes (the 10 slowest, in time order) and cron's
        //  tasks in that span (the last 10); times in shard time (staff, in game) or UTC (the log)
        public static List<string> Report(bool shardTime)
        {
            DateTime since = DateTime.UtcNow - ReportSpan;
            string When(DateTime utc) => shardTime ? ShardClock.ToShard(utc).ToString("HH:mm:ss") : utc.ToString("HH:mm:ss") + "Z";
            List<string> lines = new List<string>();

            List<SlowWork.Entry> slow = SlowWork.Slowest(SlowWork.EndedSince(since), 10).OrderBy(e => e.EndedUtc).ToList();
            lines.Add(slow.Count == 0 ? string.Format("Lag: no main-thread work took {0} ms or longer in the last {1} minutes.", SlowWork.ThresholdMs, ReportSpan.TotalMinutes)
                : string.Format("Lag: the slowest main-thread work of the last {0} minutes:", ReportSpan.TotalMinutes));
            foreach (SlowWork.Entry e in slow)
                lines.Add(string.Format("  {0} {1:0} ms: {2}", When(e.EndedUtc), e.Ms, e.What));

            List<Cron.RecentTask> tasks = Cron.RecentTasks(since);
            tasks = tasks.Skip(Math.Max(0, tasks.Count - 10)).ToList();
            lines.Add(tasks.Count == 0 ? "Cron: no tasks in that time." : "Cron's tasks in that time:");
            foreach (Cron.RecentTask t in tasks)
                lines.Add(string.Format("  {0} {1:0} ms: {2}", When(t.EndedUtc), t.Ms, t.Name));
            return lines;
        }
    }

}