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

/* Scripts\Engines\CronScheduler\CronScheduler.cs
 * CHANGELOG:
 *  10/25/26, Adam ([cron resume)
 *      "[cron resume" works without a timespan (it printed the usage: every form required two arguments and a
 *      timespan). "[cron resume <hh:mm:ss>" is still accepted (the timespan is ignored); pause is unchanged.
 *  6/21/2024, Adam (CronProcess())
 *      CronProcess was getting called once per minute to process one job from one of the queues.
 *      Change: Now CronProcess will loop over the queues, processing jobs, until the clock runs out (currently set to 0.08 seconds.)
 *      This change should keep queue depth to a minimum while tightening the bond between requested job time, and actual time executed.
 *      (less jobs waiting in the queue for the next one-minute time tick.)
 *  6/19/2024, Adam (Time)
 *      Remove all notions of locale specific time. Everything is now UTC based.
 *  3/29/22, Adam ("plantgrowth")
 *      running certain commands can do damage on the production server.
 *      check to make sure we are on TC, or that the user is sure they want to run this command.
 *      We use a confirmation prompt
 *  2/28/22, Adam
 *      add new Cron command to pause/resume cron tasks
 *  7/27/2021, adam
 *      Add the Cron.PostMessage() function.
 *      This function allows server entities like mobiles, engines like the harvest system, etc to post a message for cron priority processing.
 *      This is different from standard cron processing where a time specification dictates when a job is run.
 *      When a message is posted via PostMessage, a special one-time sub second timer is kicked off to process the PriorityQueue which holds
 *      this type of message.
 *	3/20/10, adam
 *		Add compiler so we can use the '?' specification.
 *		Remember we need to precompile any specification that uses the '?' since the specification is reinterpreted each pass
 *		which means it is possibe a job will not fire since the time is marching forward and the random ('?') value keeps moving.
 *		We precompile these specifications to lock the random value at job creation time.
 *	3/16/10, adam
 *		Add a new '?' specification which means; generate a random value for this specification
 *  11/26/08, Adam
 *      More repairs to Normalize()
 *      - Add normaization to START/STOP/STEP limiters
 *      - Have loop stop at Min(Stop, Max) 
 *	11/24/07, Adam
 *		Fix loop error (should have been < X, not <= X)
 *	4/14/08, Adam
 *		Replace all explicit use of AdjustedDateTime(DateTime.UtcNow).Value with the new:
 *			public static DateTime AdjustedDateTime.GameTimeSansDst
 *		We do this so that it is clear what files need to opperate in this special time mode:
 *			CronScheduler.cs, AutoRestart.cs, AutomatedEventSystem.cs
 *	4/7/08, Adam
 *		- merge the Nth-Day-Of-The-Month checking into the Register() function.
 *		- make last-day-of-the-month processing explicit.
 *			This is important because AES events schedule events 24 hours in advance, because of this we need to know
 *			if what we believe to be the 4th Sunday (scheduled on Saturday) is in fact part of this month. heh, complicated:
 *			Example: To schedule and event on the 4th Sunday, yet have AES kicked off 24 hours in advance for annnouncements and such.
 *			We look to see if today (Saturday) is the 4th Satrurday and that tomorrow is part of this month. That test tells us that tomorrow
 *			is in fact the 4th Sunday of the month. (This would be WAY easier if we didn't try to schedule events 24 hours in advance.)
 *			See the AES tasks in CronTasks.cs for actual examples of how this is setup.
 *			The good news is that 99% of Cron Tasks don't care about whether this is the last day of the month or not.
 *	3/29.08, Adam
 *		Have the console output say which time is Server amd which is Game
 *	3/20/08, Adam
 *		I'm rewriting the heartbeat system (Engines\Heartbeat\heartbeat.cs)
 *		Over time we added lots of functionality to heartbeat, and it's time for a major cleanup.
 *		I'm using the system from my shareware product WinCron.
 */

using Server.Diagnostics;           // log helper
using Server.Prompts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Server.Engines.CronScheduler
{
    public delegate void CronEventHandler();

    public class Cron
    {

        #region INITIALIZATION
        public static void Initialize()
        {   //run a specific task 
            CommandSystem.Register("Run", AccessLevel.Administrator, new CommandEventHandler(Run_OnCommand));
            CommandSystem.Register("Cron", AccessLevel.Administrator, new CommandEventHandler(Cron_OnCommand));
        }

        private static ArrayList m_Handlers = ArrayList.Synchronized(new ArrayList());
        #endregion INITIALIZATION

        #region Cron Commands
        // pause/resume Cron scheduler
        private static void PauseUsage(Mobile m)
        {
            m.SendMessage("Usage: cron pause <hh:mm:ss>");
            m.SendMessage("Usage: cron resume");
        }
        public static void Cron_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from.AccessLevel >= AccessLevel.Administrator)
            {
                string arg = e.ArgString;
                if (arg == null || e.Arguments.Length < 1)
                {
                    PauseUsage(e.Mobile);
                    return;
                }
                string command = arg.ToLower();
                string[] toks = command.Split(' ');
                string span = toks.Last();
                TimeSpan ts = TimeSpan.Zero;
                // only pause needs a timespan; resume takes none (a trailing one, e.g. "resume 0:0:0", is accepted and ignored)
                if (toks[0] != "resume" && (e.Arguments.Length < 2 || TimeSpan.TryParse(span, out ts) == false))
                {
                    PauseUsage(e.Mobile);
                    return;
                }

                if (toks[0] == "pause")
                {
                    if (CronPaused == true)
                        e.Mobile.SendMessage("Cron already paused.");
                    else
                    {
                        CronPauseTimer.Start((long)ts.TotalMilliseconds);
                        CronPaused = true;
                        e.Mobile.SendMessage("Paused.");
                    }
                }
                else if (toks[0] == "resume")
                {
                    if (CronPaused == false)
                        e.Mobile.SendMessage("Cron already running.");
                    else
                    {
                        CronPaused = false;
                        e.Mobile.SendMessage("Resumed.");
                    }
                }
                else
                    PauseUsage(e.Mobile);
            }
        }
        // enqueue a task normally scheduled on the heartbeat
        public static void Run_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from.AccessLevel >= AccessLevel.Administrator)
            {
                string arg = e.GetString(0);
                if (arg == null || arg.Length == 0)
                {
                    e.Mobile.SendMessage("Usage: run <TaskID>");
                    return;
                }
                // allow ';' demilited command lists
                string commandLine = e.ArgString;
                string temp = commandLine.Replace(';', ' ');
                string[] commands = temp.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string cmd in commands)
                {
                    CronEventEntry? cee = Cron.Find(cmd);
                    if (cee == null)
                        e.Mobile.SendMessage("No task named \"{0}\".", cmd);
                    else
                    {   // running certain commands can do damage on the production server.
                        //  check to make sure we are on TC, or that the user is sure thay want to run this command.
                        if ((cee.Name.ToLower() == "plantgrowth" || cee.Name.ToLower() == "seasonalwipe"))
                        {   // confirmation prompt for dangerous tasks.
                            e.Mobile.Prompt = new ConfirmTaskPrompt(cee);
                            e.Mobile.SendMessage("You are about to run a dangerous task on the production server.");
                            e.Mobile.SendMessage("Type 'yes' to confirm.");
                        }
                        else
                        {
                            // queue it!
                            lock (m_PriorityQueue.SyncRoot)
                                m_PriorityQueue.Enqueue(cee);

                            e.Mobile.SendMessage("Task \"{0}\" enqueued.", cee.Name);

                            // launch the Priority timer
                            Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(PriorityTick), new object?[] { null });
                        }
                    }
                }
            }
        }
        private class ConfirmTaskPrompt : Prompt
        {
            private CronEventEntry m_command;

            public ConfirmTaskPrompt(CronEventEntry cmd)
            {
                m_command = cmd;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (text.Length > 40)
                    text = text.Substring(0, 40).ToLower();

                if (text == "y" || text == "yes")
                {
                    lock (m_PriorityQueue.SyncRoot)
                        m_PriorityQueue.Enqueue(m_command);

                    from.SendMessage("Task \"{0}\" enqueued.", m_command.Name);

                    // launch the Priority timer
                    Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(PriorityTick), new object?[] { null });
                }
            }

            public override void OnCancel(Mobile from)
            {
                from.SendMessage("You decide not to run \"{0}\".", m_command.Name);
            }
        }
        #endregion Cron Commands

        #region PostMessage
        public enum MessageType
        {
            MSG_DUMMY,          // do nothing. Good for testing

            /*put mobile/item messages here*/

            /*put global system messages here*/
            GBL_MSG_START = 100,
            GBL_ORE_SPAWN,      // notification of an ore spawn

            /* other messages go here*/
        }
        public class ServerMessage
        {
            private MessageType m_message;
            public MessageType Message { get { return m_message; } }
            private object[] m_args;
            public object[] Args { get { return m_args; } }
            public ServerMessage(MessageType message, object[] args)
            {
                m_message = message;
                m_args = args;
            }
        }
        // enqueue a task normally scheduled on the heartbeat
        public static void PostMessage(MessageType message, object[] args)
        {
            CronEventEntry? cee = Cron.Find("ReceiveMessage");
            if (cee == null)
                return; // No so named task named task
            else
            {   // queue it!
                lock (m_PriorityQueue.SyncRoot)
                    m_PriorityQueue.Enqueue(cee);

                lock (m_MessageQueue.SyncRoot)
                    m_MessageQueue.Enqueue(new ServerMessage(message, args));

                // launch the Priority timer
                Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(PriorityTick), new object?[] { null });
            }
        }
        private static void PriorityTick(object state)
        {   // nudge our cron to process this priority message
            try { Cron.CronProcess(); }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in CronScheduler.CronProcess: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
        #endregion

        #region QUEUE MANAGEMENT
        private static Queue m_TaskQueue = Queue.Synchronized(new Queue());
        private static Queue m_IdleQueue = Queue.Synchronized(new Queue());
        private static Queue m_PriorityQueue = Queue.Synchronized(new Queue());
        private static Queue m_MessageQueue = Queue.Synchronized(new Queue());
        public static Queue MessageQueue { get { return m_MessageQueue; } }

        private static int GetTaskQueueDepth()
        {
            lock (m_TaskQueue.SyncRoot)
            {
                return m_TaskQueue.Count;
            }
        }

        private static int GetIdleQueueDepth()
        {
            lock (m_IdleQueue.SyncRoot)
            {
                return m_IdleQueue.Count;
            }
        }

        private static int GetPriorityQueueDepth()
        {
            lock (m_PriorityQueue.SyncRoot)
            {
                return m_PriorityQueue.Count;
            }
        }

        public static void QueuePriorityTask(CronEventHandler handler)
        {
            QueuePriorityTask(null, handler, null, true);
        }

        public static void QueuePriorityTask(string Name, CronEventHandler handler)
        {
            QueuePriorityTask(Name, handler, null, true);
        }

        public static void QueuePriorityTask(string Name, CronEventHandler handler, string CronSpec)
        {
            QueuePriorityTask(Name, handler, CronSpec, true);
        }

        public static void QueuePriorityTask(string? Name, CronEventHandler handler, string? CronSpec, bool Unique)
        {
            lock (m_PriorityQueue.SyncRoot)
            {
                CronEventEntry task = new CronEventEntry(Name, handler, new CronJob(CronSpec), Unique, null);
                if (Unique == true)
                {   // only one
                    if (m_PriorityQueue.Contains(task) == false)
                        m_PriorityQueue.Enqueue(task);
                }
                else
                    m_PriorityQueue.Enqueue(task);
            }
        }
        public static void QueueIdleTask(CronEventHandler handler)
        {
            QueueIdleTask(null, handler, null, true);
        }

        public static void QueueIdleTask(string Name, CronEventHandler handler)
        {
            QueueIdleTask(Name, handler, null, true);
        }

        public static void QueueIdleTask(string Name, CronEventHandler handler, string CronSpec)
        {
            QueueIdleTask(Name, handler, CronSpec, true);
        }

        public static void QueueIdleTask(string? Name, CronEventHandler handler, string? CronSpec, bool Unique)
        {
            lock (m_IdleQueue.SyncRoot)
            {
                CronEventEntry task = new CronEventEntry(Name, handler, new CronJob(CronSpec), Unique, null);
                if (Unique == true)
                {   // only one
                    if (m_IdleQueue.Contains(task) == false)
                        m_IdleQueue.Enqueue(task);
                }
                else
                    m_IdleQueue.Enqueue(task);
            }
        }

        public static void QueueTask(string Name, CronEventHandler handler)
        {
            QueueTask(Name, handler, null, true);
        }

        public static void QueueTask(string Name, CronEventHandler handler, string CronSpec)
        {
            QueueTask(Name, handler, CronSpec, true);
        }

        public static void QueueTask(string Name, CronEventHandler handler, string? CronSpec, bool Unique)
        {
            lock (m_TaskQueue.SyncRoot)
            {
                CronEventEntry task = new CronEventEntry(Name, handler, new CronJob(CronSpec), Unique, null);
                if (Unique == true)
                {   // only one
                    if (m_TaskQueue.Contains(task) == false)
                        m_TaskQueue.Enqueue(task);
                }
                else
                    m_TaskQueue.Enqueue(task);
            }
        }
        #endregion QUEUE MANAGEMENT

        #region JOB REGISTRATION
        // register a new job
        public static void Register(CronEventHandler handler, string? CronSpec)
        {
            Register(handler, CronSpec, true);
        }
        public static void Register(CronEventHandler handler, string? CronSpec, bool Unique)
        {
            Register(handler, CronSpec, Unique, null);
        }
        public static void Register(CronEventHandler handler, string? CronSpec, bool Unique, CronLimit? limit)
        {
            lock (m_Handlers.SyncRoot)
            {
                m_Handlers.Add(new CronEventEntry(null, handler, new CronJob(CronSpec), Unique, limit));
            }
        }
        #region Timezone-Aware Registration (zero impact on existing jobs)

        public static void RegisterLocal(CronEventHandler handler, string CronSpec, string windowsTimeZoneId)
        {
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZoneId);
            RegisterLocal(handler, CronSpec, tz);
        }

        public static void RegisterLocal(CronEventHandler handler, string CronSpec, TimeZoneInfo tz)
        {
            lock (m_Handlers.SyncRoot)
            {
                m_Handlers.Add(new CronEventEntry(null, handler, new CronJob(CronSpec, tz), true, null));
            }
        }

        // Quick helpers if you schedule a lot for one country
        public static void RegisterNetherlands(CronEventHandler h, string spec)
            => RegisterLocal(h, spec, "Central European Standard Time");

        public static void RegisterPacific(CronEventHandler h, string spec)
            => RegisterLocal(h, spec, "Pacific Standard Time");
        #endregion
        public static bool Update(string name, string cronSpec)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            lock (m_Handlers.SyncRoot)
            {
                CronEventEntry? cee = Find(name); // assumes Find searches m_Handlers
                if (cee == null)
                    return false;

                cee.SetCronSpec(cronSpec);
                return true;
            }
        }
        #endregion JOB REGISTRATION

        #region JOB MANAGEMENT
        public static string[] Kill()
        {
            return Kill(".*");
        }

        public static string[] Kill(string pattern)
        {
            lock (m_Handlers.SyncRoot)
            {
                ArrayList list = new ArrayList();
                ArrayList ToDelete = new ArrayList();
                Regex Pattern = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.ECMAScript);

                foreach (object o in m_Handlers)
                {
                    CronEventEntry? cee = o as CronEventEntry;
                    if (cee == null) continue;

                    if (Pattern.IsMatch(cee.Name))
                    {
                        list.Add(string.Format("Deleted: '{0}', Cron: '{1}', Task: {2}\r\n",
                            cee.Name,
                            cee.Cronjob.Specification,
                            cee.Handler.Target == null ? cee.Name : cee.Handler.Target.ToString()));

                        ToDelete.Add(cee);
                    }
                }

                foreach (object o in ToDelete)
                {
                    CronEventEntry? cee = o as CronEventEntry;
                    if (cee == null) continue;

                    cee.Running = false;        // stop any ones queued in the 'temp' list from running
                    m_Handlers.Remove(cee);     // remove from master list
                }

                return (string[])list.ToArray(typeof(string));
            }
        }

        public static string[] List()
        {
            return List(".*");
        }

        public static string[] List(string pattern)
        {
            ArrayList list = new ArrayList();
            Regex Pattern = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.ECMAScript);

            lock (m_Handlers.SyncRoot)
            {
                foreach (object o in m_Handlers)
                {
                    CronEventEntry? cee = o as CronEventEntry;
                    if (cee == null) continue;

                    if (Pattern.IsMatch(cee.Name))
                    {
                        list.Add(string.Format("Job: '{0}', Cron: '{1}', Task: {2}\r\n",
                            cee.Name,
                            cee.Cronjob.Specification,
                            cee.Handler.Target == null ? cee.Name : cee.Handler.Target.ToString()));
                    }
                }
                return (string[])list.ToArray(typeof(string));
            }
        }

        public static bool Run(string pattern)
        {
            lock (m_Handlers.SyncRoot)
            {
                foreach (object o in m_Handlers)
                {
                    CronEventEntry? cee = o as CronEventEntry;
                    if (cee == null) continue;

                    if (pattern.ToLower() == cee.Name.ToLower())
                    {
                        try
                        {   // call as a foreground process
                            cee.Handler();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogException(ex);
                            Console.WriteLine("Exception caught in User Code: {0}", ex.Message);
                            Console.WriteLine(ex.StackTrace);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public static CronEventEntry? Find(string pattern)
        {
            lock (m_Handlers.SyncRoot)
            {
                foreach (object o in m_Handlers)
                {
                    CronEventEntry? cee = o as CronEventEntry;
                    if (cee == null) continue;

                    if (pattern.ToLower() == cee.Name.ToLower())
                    {
                        return cee;
                    }
                }
            }

            return null;
        }
        #endregion JOB MANAGEMENT

        #region CronProcess
        private static bool CronPaused = false;
        private static Utility.LocalTimer CronPauseTimer = new Utility.LocalTimer();    // how long we are to pause
        private static Utility.LocalTimer CronTaskTimer = new Utility.LocalTimer();     // how long our execution slice runs

        public static void CronProcess()
        {
            try
            {
                // tell the world we're looking for work
                string serverTime = DateTime.UtcNow.ToString();
                int timerQueueDepth = 0;
                lock (Timer.Queue) { timerQueueDepth = Timer.Queue.Count; }

                // Compute queue depths under locks (no TOCTOU)
                int pqDepth, tqDepth, iqDepth;
                GetQueueDepths(out pqDepth, out tqDepth, out iqDepth);

                Console.WriteLine("Scheduler: ({0} Server Time): ", serverTime);
                Console.WriteLine(
                    "Scheduler: Timer Queue: {0}, Priority Queue: {1}, Task Queue: {2}, Idle Queue: {3}",
                    timerQueueDepth, pqDepth, tqDepth, iqDepth);

                // Belt and suspenders: remove accidental duplicates of Unique jobs before working this slice.
                // This should normally remove 0 entries; if it removes more, you have an upstream enqueue bug.
                int dupesRemoved = RemoveDuplicateJobs();
                if (dupesRemoved > 0)
                    Console.WriteLine("Scheduler: Removed {0} duplicate Unique job(s).", dupesRemoved);

                // now, run as many tasks as we can in this slice
                CronTaskTimer.Stop();
                CronTaskTimer.Start(millisecond_timeout: TimeSpan.FromSeconds(0.08).TotalMilliseconds);

                bool anyWork = false;

                do
                {
                    if (CronPauseTimer.Triggered == true)
                    {
                        CronPaused = false;
                        CronPauseTimer.Stop();
                    }

                    if (CronPaused == false)
                    {
                        // Avoid TOCTOU: do not check Count outside the lock. Dequeue only while locked.
                        CronEventEntry? cee = null;

                        // Prefer Priority -> Task -> Idle, but make the "is there work" decision
                        // inside each queue lock so Count and Dequeue are consistent.
                        if (cee == null)
                        {
                            lock (m_PriorityQueue.SyncRoot)
                            {
                                if (m_PriorityQueue.Count != 0)
                                    cee = m_PriorityQueue.Dequeue() as CronEventEntry;
                            }
                        }

                        if (cee == null)
                        {
                            lock (m_TaskQueue.SyncRoot)
                            {
                                if (m_TaskQueue.Count != 0)
                                    cee = m_TaskQueue.Dequeue() as CronEventEntry;
                            }
                        }

                        if (cee == null)
                        {
                            lock (m_IdleQueue.SyncRoot)
                            {
                                if (m_IdleQueue.Count != 0)
                                    cee = m_IdleQueue.Dequeue() as CronEventEntry;
                            }
                        }

                        if (cee != null)
                        {
                            CronProcess(cee);
                        }
                        else
                        {
                            Console.WriteLine("No scheduled work.");
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Scheduler: Paused.");
                        break;
                    }

                    // Compute anyWork under locks (no TOCTOU)
                    GetQueueDepths(out pqDepth, out tqDepth, out iqDepth);
                    anyWork = (pqDepth + tqDepth + iqDepth) > 0;

                    if (CronTaskTimer.Triggered == true)
                        Utility.Monitor.WriteLine("Cron Slice: WorkTimer Timed out", ConsoleColor.Red);
                    else if (anyWork)
                        Utility.Monitor.WriteLine("Cron Slice: More Work to do", ConsoleColor.DarkCyan);
                    else
                        Utility.Monitor.WriteLine("Cron Slice: No more Work to do", ConsoleColor.Yellow);

                } while (anyWork && CronTaskTimer.Triggered == false);

                CronTaskTimer.Stop();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Console.WriteLine("Exception caught in CronProcessor: {0}", ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void GetQueueDepths(out int pqDepth, out int tqDepth, out int iqDepth)
        {
            lock (m_PriorityQueue.SyncRoot) pqDepth = m_PriorityQueue.Count;
            lock (m_TaskQueue.SyncRoot) tqDepth = m_TaskQueue.Count;
            lock (m_IdleQueue.SyncRoot) iqDepth = m_IdleQueue.Count;
        }

        // Removes duplicates ONLY for jobs explicitly marked Unique.
        // First occurrence wins; later duplicates are dropped. Order is preserved.
        // Removes duplicates ONLY for jobs explicitly marked Unique.
        // First occurrence wins; later duplicates are dropped. Order is preserved.
        private static int RemoveDuplicateJobs()
        {
            int removed = 0;

            // Uses CronEventEntry.Equals/GetHashCode (same identity as queue Contains()).
            HashSet<CronEventEntry> seen = new HashSet<CronEventEntry>();

            removed += RemoveDuplicateJobsFromQueue(m_PriorityQueue, seen);
            removed += RemoveDuplicateJobsFromQueue(m_TaskQueue, seen);
            removed += RemoveDuplicateJobsFromQueue(m_IdleQueue, seen);

            return removed;
        }

        private static int RemoveDuplicateJobsFromQueue(Queue q, HashSet<CronEventEntry> seen)
        {
            int removed = 0;

            lock (q.SyncRoot)
            {
                int originalCount = q.Count;
                if (originalCount == 0)
                    return 0;

                ArrayList keep = new ArrayList(originalCount);

                for (int i = 0; i < originalCount; i++)
                {
                    object? o = q.Dequeue();
                    CronEventEntry? cee = o as CronEventEntry;

                    // Keep non-CEE items as-is.
                    if (cee == null)
                    {
                        keep.Add(o);
                        continue;
                    }

                    // Only dedupe jobs explicitly marked Unique.
                    if (!cee.Unique)
                    {
                        keep.Add(o);
                        continue;
                    }

                    // De-dupe using CronEventEntry identity (Equals/GetHashCode).
                    if (seen.Add(cee))
                    {
                        keep.Add(o);
                    }
                    else
                    {
                        removed++;
                        Console.WriteLine("Scheduler: Dropped duplicate Unique job '{0}'.", cee.Name);
                    }
                }

                // Restore queue in original order (minus removed duplicates).
                for (int i = 0; i < keep.Count; i++)
                    q.Enqueue(keep[i]);
            }

            return removed;
        }
        private static void CronProcess(CronEventEntry cee)
        {
            if (cee == null) return;

            try
            {
                // run the user code                               
                if (cee.Running == false)
                    Console.WriteLine("Skipping queued Job {0} because it was killed", cee.Name);
                else
                {
                    // okay, run the scheduled task.
                    Utility.TimeCheck tc = new Utility.TimeCheck();
                    //Console.Write("{0}: ", cee.Name);
                    Utility.Monitor.Write(string.Format($"{cee.Name}: "), ConsoleColor.Green/*Utility.RandomConsoleColor(Utility.GetStableHashCode(cee.Name))*/);
                    tc.Start();                 // track the total time for [lag forensics
                    cee.Handler();              // execute the next task in the queue
                    tc.End();
                    AuditTask(cee, tc);         // maintain our list of 5 most recent tasks
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Console.WriteLine("Exception caught in scheduled task: {0}", ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
        #endregion

        #region TASK AUDIT
        private static ArrayList m_RecentTasks = new ArrayList();   // to hold last 5 tasks for logging purposes
        /// <summary>
        /// Returns a : delemited list of up to the last 5 tasks run
        /// </summary>
        /// <returns></returns>
        public static string GetRecentTasks()
        {
            //Plasma:
            //prevention rather than crash...
            if (m_RecentTasks == null)
                return "";

            try
            {
                // clear a temp string
                string temp = "";

                for (int i = 0; i < m_RecentTasks.Count; ++i)
                {
                    //add new job description
                    temp += ((LagStats)m_RecentTasks[i]).ToString();
                    //stick a : (delimiter) on if not the last item
                    if (i != m_RecentTasks.Count - 1)
                        temp += ": ";
                }
                return temp;
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("String error in Heartbeat.GetRecentTasks()");
                Console.WriteLine(e.Message + " " + e.StackTrace);
                return "";
            }

        }

        /// <summary>
        /// Holds information for the [lag command
        /// </summary>
        private struct LagStats
        {
            //plasma: used to help reporting [lag
            private string ID;          //task name
            private string TimeTaken;   //Time elapsed

            public LagStats(string id, string tt)
            {
                ID = id;
                // set default string if null (just in case)
                if (tt == null)
                    TimeTaken = "00:00";
                else
                {
                    try
                    {
                        // remove the "seconds" part of the string to be tidier
                        TimeTaken = tt.Substring(0, tt.IndexOf(" ")).Trim();
                    }
                    catch
                    {
                        // just in case
                        TimeTaken = "Error!";
                        return;
                    }
                }
            }

            public override string ToString()
            {
                return ID.ToString() + ", " + TimeTaken;
            }
        }

        private static void AuditTask(CronEventEntry cee, Utility.TimeCheck tc)
        {
            // here we maintain our list of 5 most recent tasks
            if (m_RecentTasks.Count == 5)
                m_RecentTasks.RemoveAt(0);  //remove first (oldest) task
            else if (m_RecentTasks.Count > 5)
                m_RecentTasks.Clear();      // this shouldn't be possible 
                                            // but stranger things have happened!

            // add new task and elapsed time as a LagStats struct at bottom of the list
            m_RecentTasks.Add(new LagStats(cee.Name, tc.TimeTaken));
        }
        #endregion TASK AUDIT

        #region CronSlice
        /*  CronSlice simply loads the task queue with jobs that match the current time
         */

        // Cron is guaranteed to only tick once per minute
        private static int LastCronMinute = DateTime.UtcNow.Minute;
        private static bool HasMinuteRolledOver
        {
            get
            {
                int minute = DateTime.UtcNow.Minute;
                bool change = minute != LastCronMinute;
                LastCronMinute = minute;
                return change;
            }
        }
        public static void CronSlice()
        {
            try
            {
                bool quit = false;
                DateTime thisTime = DateTime.UtcNow;
                while (quit == false)
                {
                    // Cron is guaranteed to only tick once per minute
                    // HasMinuteRolledOver returns true if this minute is != last minute
                    // Note: We never want to execute the cron jobs twice in the same minute (since Cron is by nature, one-minute granularity.)
                    //  This test simply ensures the minute has rolled over (changed) since the last time we ran.
                    if (!HasMinuteRolledOver)
                        break;

                    lock (m_Handlers.SyncRoot)
                    {
                        foreach (object o in m_Handlers)
                        {
                            CronEventEntry? cee = o as CronEventEntry;
                            if (cee == null) continue;

                            // match the time if the job with the current time
                            if (cee.Cronjob.Match(thisTime))
                            {   // we have a match

                                // process special CronLimit specifications, like 3rd Sunday
                                if (cee.Limit != null && cee.Limit.Execute() == false)
                                {
                                    Console.WriteLine("Note: Scheduled Job '{0}' does not meet nth day limitation.", cee.Name);
                                    continue;
                                }

                                // Queue it!
                                lock (m_TaskQueue.SyncRoot)
                                {
                                    bool Add = true;

                                    // only queue 'unique' tasks
                                    if (m_TaskQueue.Count > 0 && cee.Unique && m_TaskQueue.Contains(cee))
                                    {
                                        Add = false;
                                        int index = m_TaskQueue.ToArray().ToList().IndexOf(cee);
                                        Console.WriteLine($"Note: Duplicate Job '{cee.Name}' ignored. {index} tasks ahead of me.");
                                    }

                                    // max job queue size = 128
                                    if (Add && m_TaskQueue.Count == 128)
                                    {   // should probably add an exception here
                                        CronEventEntry? temp = m_TaskQueue.Dequeue() as CronEventEntry;
                                        Console.WriteLine("Warning: Job queue overflow. Lost job '{0}'", temp.Name);
                                    }

                                    // add the task to the queue
                                    if (Add == true)
                                        m_TaskQueue.Enqueue(cee);
                                }
                            }
                        }
                    } // lock

                    // since we're not running as a thread, just one pass
                    quit = true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Console.WriteLine("Exception caught in CronScheduler: {0}", ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

        } // CronScheduler()
        #endregion CronSlice

        #region CronEventEntry
        public class CronEventEntry
        {
            private CronEventHandler m_Handler;
            private CronJob m_Cronjob;
            private string m_Name;
            private bool m_Unique;
            private volatile bool m_Running = true;
            private CronLimit? m_Limit;

            // Stable logical identity for "Unique" enforcement.
            private readonly string m_UniqueKey;

            public CronEventHandler Handler { get { return m_Handler; } }
            public CronJob Cronjob { get { return m_Cronjob; } }
            public void SetCronSpec(string cronSpec)
            {
                m_Cronjob = new CronJob(cronSpec);
            }
            public string Name { get { return m_Name; } }
            public bool Unique { get { return m_Unique; } }
            public bool Running { get { return m_Running; } set { m_Running = value; } }
            public CronLimit? Limit { get { return m_Limit; } }

            public CronEventEntry(string? Name, CronEventHandler handler, CronJob Cronjob, bool Unique, CronLimit? limit)
            {
                m_Name = (Name != null)
                    ? Name
                    : (handler != null && handler.Method != null && handler.Method.Name != null)
                        ? handler.Method.Name
                        : "Unknown";

                m_Unique = Unique;
                m_Handler = handler;
                m_Cronjob = Cronjob;
                m_Limit = limit;

                // Identity: name + method. (Name is often derived from method anyway.)
                string methodKey =
                    (handler != null && handler.Method != null)
                        ? handler.Method.DeclaringType.FullName + "." + handler.Method.Name
                        : "null-handler";

                m_UniqueKey = m_Name + "|" + methodKey;
            }

            public override bool Equals(object? obj)
            {
                CronEventEntry? other = obj as CronEventEntry;
                if (other == null)
                    return false;

                return StringComparer.OrdinalIgnoreCase.Equals(m_UniqueKey, other.m_UniqueKey);
            }

            public override int GetHashCode()
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(m_UniqueKey);
            }
        }


        // limit the cron specification to a specific day within the month. e.g., 3rd Sunday
        public class CronLimit
        {   // is this the last day of the month? (ldom)
            public enum IsLastDayOfMonth { dont_care, must_be_ldom, must_not_be_ldom };
            private IsLastDayOfMonth m_isldom;
            private int m_Nth;
            private DayOfWeek m_dayName;
            public CronLimit(int Nth, DayOfWeek dayName)
            {
                m_Nth = Nth;
                m_dayName = dayName;
                m_isldom = IsLastDayOfMonth.dont_care;
            }
            public CronLimit(int Nth, DayOfWeek dayName, IsLastDayOfMonth status)
            {
                m_Nth = Nth;
                m_dayName = dayName;
                m_isldom = status;
            }
            public bool Execute()
            {   // is it the 3rd Sunday of the month?
                bool IsNthDayOfMonth = Cron.CronJob.IsNthDayOfMonth(m_Nth, m_dayName);

                if (IsNthDayOfMonth == false)   // not the right day
                    return false;

                if (m_isldom == IsLastDayOfMonth.dont_care) // the right day and nothing else to check
                    return true;

                // sometimes the user wants to know if this is the last day of the month
                //	if IsTomorrowThisMonth then it can't be the last day of the month (tomorrow = +24 hours)
                bool last_dom = Cron.CronJob.IsTomorrowThisMonth() == false;

                // if last_dom == true and the user wanted the last day of the month, yay
                if (last_dom == true && m_isldom == IsLastDayOfMonth.must_be_ldom)
                    return true;

                // if last_dom == false and the user did not want the last day of the monthg, yay
                if (last_dom == false && m_isldom == IsLastDayOfMonth.must_not_be_ldom)
                    return true;

                // otherwise, it's not what the user wants
                return false;
            }
        }
        #endregion CronEventEntry

        #region TIMERS
        private static ProcessTimer m_ProcessTimer = new ProcessTimer();    // processes queued tasks
        private static CronTimer m_CronTimer = new CronTimer();             // schedules tasks
        private static bool m_Running = false;
        private class ProcessTimer : Timer
        {
            public ProcessTimer()
                : base(TimeSpan.FromMinutes(0.5), TimeSpan.FromMinutes(0.9))
            {   // we want to come in just under 1 minute since that's the granularity of Cron
                // a TimerPriority.OneMinute does not guarantee that
                Priority = TimerPriority.FiveSeconds;
                System.Console.WriteLine("Starting Cron Process Timer.");
                Start();
            }

            protected override void OnTick()
            {
                try { Cron.CronProcess(); }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    System.Console.WriteLine("Exception Caught in CronScheduler.CronProcess: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                }
            }
        }

        private class CronTimer : Timer
        {
            public CronTimer()
                : base(TimeSpan.FromMinutes(0.5), TimeSpan.FromMilliseconds(350))
            {
                Priority = TimerPriority.TwoFiftyMS;
                System.Console.WriteLine("Starting Cron Schedule Timer.");
                Start();
            }

            protected override void OnTick()
            {
                try { Cron.CronSlice(); }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    System.Console.WriteLine("Exception Caught in CronScheduler.CronSlice: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                }
            }
        }
        #endregion TIMERS

        #region CRON ENGINE
        // format of a cron job is as follows
        // Minute (0-59)  Hour (0-23)  Day of Month (1-31)  Month (1-12 or Jan-Dec)  Day of Week (0-6 or Sun-Sat)
        // Read this to learn cron syntax: http://www.ss64.com/osx/crontab.html
        public class CronJob
        {
            private readonly string? m_originalSpec;
            private readonly TimeZoneInfo m_TimeZone;

            /// <summary>
            /// Legacy constructor — keeps existing registrations working (pure UTC).
            /// </summary>
            public CronJob(string? specification)
                : this(specification, null)
            {
            }

            /// <summary>
            /// Constructor for timezone-aware jobs.
            /// </summary>
            public CronJob(string? specification, TimeZoneInfo? timeZone)
            {
                m_originalSpec = specification;
                m_TimeZone = timeZone ?? TimeZoneInfo.Utc;
            }
            public string? Specification
            {
                get { return m_originalSpec; }
                //set { m_originalSpec = value; }
            }
            /// <summary>
            /// Main matching function — called every minute with current UTC time.
            /// </summary>
            public bool Match(DateTime utcNow)
            {
                if (string.IsNullOrEmpty(m_originalSpec)) return false;

                // Convert current UTC time → target timezone's local time
                DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, m_TimeZone);

                // Normalize the original spec **now**, so ? picks a fresh random each check
                string normalizedSpec = NormalizeFullSpec(m_originalSpec);

                // Match against the normalized spec using the current local time
                return InternalMatch(normalizedSpec, localTime);
            }

            /// <summary>
            /// Normalizes the full 5-field cron spec (once per match call).
            /// Handles ?, ranges, steps, etc.
            /// </summary>
            private string NormalizeFullSpec(string rawSpec)
            {
                string[] fields = rawSpec.Split(' ', 5);
                if (fields.Length != 5) return null; // invalid

                return string.Join(" ",
                    Normalize(fields[0], 60, 0, 59),   // minute
                    Normalize(fields[1], 24, 0, 23),   // hour
                    Normalize(fields[2], 31, 1, 31),   // day of month
                    Normalize(fields[3], 12, 1, 12),   // month
                    Normalize(fields[4], 7, 0, 6)      // day of week
                );
            }

            /// <summary>
            /// Internal match logic — unchanged from your original.
            /// </summary>
            private bool InternalMatch(string normalizedSpec, DateTime time)
            {
                string[] fields = normalizedSpec.Split(' ', 5);
                if (fields.Length != 5) return false;

                return Minute(fields[0], time.Minute) &&
                       Hour(fields[1], time.Hour) &&
                       DayOfMonth(fields[2], time.Day) &&
                       Month(fields[3], time.Month) &&
                       DayOfWeek(fields[4], (int)time.DayOfWeek);
            }

            // ──────────────────────────────────────────────
            //  Your existing field-specific methods
            //  (they now receive already-normalized strings)
            // ──────────────────────────────────────────────

            private bool Minute(string spec, int value) => Common(Normalize(spec, 60, 0, 59), 60, value);
            private bool Hour(string spec, int value) => Common(Normalize(spec, 24, 0, 23), 24, value);
            private bool DayOfMonth(string spec, int value) => Common(Normalize(spec, 31, 1, 31), 31, value);
            private bool Month(string spec, int value) => Common(Normalize(spec, 12, 1, 12), 12, value);
            private bool DayOfWeek(string spec, int value) => Common(Normalize(spec, 7, 0, 6), 7, value);

            private bool Common(string normalized, int _, int value)
            {
                if (normalized == "*") return true;

                string[] parts = normalized.Split(',');
                foreach (string part in parts)
                {
                    if (int.TryParse(part, out int num) && num == value)
                        return true;
                }
                return false;
            }

            // ──────────────────────────────────────────────
            //  Core normalization logic (your original code)
            // ──────────────────────────────────────────────

            private string Normalize(string specification, int elements, int min, int max)
            {
                if (specification == "*") return "*";

                // Handle random ? — fresh random every time (good for long-running servers)
                if (specification.Contains("?"))
                {
                    int rnd = Utility.Random(min, max + 1);
                    specification = specification.Replace("?", rnd.ToString());
                }

                string[] split = specification.Split(',');
                var result = new System.Text.StringBuilder();

                int rangeSize = max - min + 1;

                foreach (string termStr in split)
                {
                    if (result.Length > 0) result.Append(",");

                    string[] term = termStr.Split(new[] { '-', '/' }, 3);

                    if (term[0] == "*")
                    {
                        int start = min;
                        int stop = max;
                        int step = term.Length > 1 ? int.Parse(term[1]) : 1;

                        if (step >= rangeSize)
                        {
                            result.Append(start);
                            continue;
                        }

                        step = Math.Max(1, Math.Min(step, rangeSize));

                        for (int v = start; v <= stop; v += step)
                            result.Append(v).Append(",");
                        if (result.Length > 0 && result[^1] == ',') result.Length--;
                    }
                    else
                    {
                        int start = Fix(int.Parse(term[0]), min, max);
                        int stop = term.Length > 1 ? Fix(int.Parse(term[1]), min, max) : start;
                        int step = term.Length > 2 ? int.Parse(term[2]) : 1;

                        if (step >= rangeSize)
                        {
                            result.Append(start);
                            continue;
                        }

                        step = Math.Max(1, Math.Min(step, rangeSize));

                        for (int v = start; v <= stop; v += step)
                            result.Append(v).Append(",");
                        if (result.Length > 0 && result[^1] == ',') result.Length--;
                    }
                }

                return result.ToString();
            }

            private static int Fix(int value, int min, int max)
            {
                return Math.Max(min, Math.Min(max, value));
            }

            /// <summary>
            /// Determines if tomorrow is still in the same month as today (UTC-based).
            /// Useful for scheduling logic that needs to know if a date crosses month boundaries.
            /// </summary>
            /// <returns>true if tomorrow has the same month number as today</returns>
            public static bool IsTomorrowThisMonth()
            {
                DateTime now = DateTime.UtcNow;
                DateTime tomorrow = now.AddDays(1);

                return tomorrow.Month == now.Month;
            }
            /// <summary>
            /// Checks if the current day is the Nth occurrence of a specific weekday in the current month.
            /// Example: IsNthDayOfMonth(3, DayOfWeek.Sunday) → true if today is the 3rd Sunday of the month.
            /// </summary>
            /// <param name="Nth">Which occurrence (1 = first, 2 = second, etc.)</param>
            /// <param name="dayName">The weekday to check for (e.g. DayOfWeek.Sunday)</param>
            /// <returns>true if today is the Nth occurrence of that weekday in the current month</returns>
            /// <exception cref="ArgumentOutOfRangeException">If Nth is less than 1</exception>
            public static bool IsNthDayOfMonth(int Nth, DayOfWeek dayName)
            {
                if (Nth < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(Nth), "Nth must be 1 or greater.");
                }

                DateTime now = DateTime.UtcNow;

                // Quick exit if today isn't even the right weekday
                if (now.DayOfWeek != dayName)
                {
                    return false;
                }

                int count = 0;
                DateTime current = now.Date;  // start from midnight today (removes time component)

                // Walk backwards to the 1st of the month, counting occurrences of the target day
                while (current.Month == now.Month)
                {
                    if (current.DayOfWeek == dayName)
                    {
                        count++;

                        // Early exit if we've already passed the requested Nth
                        if (count > Nth)
                        {
                            return false;
                        }
                    }

                    // Stop when we reach the first day of the month
                    if (current.Day == 1)
                    {
                        break;
                    }

                    current = current.AddDays(-1);
                }

                // True only if we found exactly the requested Nth occurrence
                return count == Nth;
            }
        }
        #endregion CRON ENGINE

        #region Cron Helpers
        public static bool TryBuildCronSpecForMinutes(int minutes, out string spec)
        {
            spec = null;

            if (minutes <= 0)
                return false;

            // Case A: Every N minutes, but only if it tiles evenly into an hour.
            // Example: 15 -> */15 * * * *
            if (minutes < 60)
            {
                if (60 % minutes == 0)
                {
                    spec = string.Format("*/{0} * * * *", minutes);
                    return true;
                }

                return false;
            }

            // Case B: Every N hours at minute 0, only if it tiles evenly into a day.
            // Example: 120 minutes -> 2 hours -> 0 */2 * * *
            if (minutes % 60 == 0)
            {
                int hours = minutes / 60;

                if (hours > 0 && hours <= 24 && (24 % hours) == 0)
                {
                    spec = string.Format("0 */{0} * * *", hours);
                    return true;
                }
            }

            // Case C: Every N days at 00:00 (server-local), using day-of-month stepping.
            // Example: 1440 minutes -> 1 day -> 0 0 */1 * *
            // Note: This is "cron reasonable" but not identical to "every N*1440 minutes" across month boundaries.
            if (minutes % 1440 == 0)
            {
                int days = minutes / 1440;

                if (days > 0 && days <= 31) // keep within reason
                {
                    spec = string.Format("0 0 */{0} * *", days);
                    return true;
                }
            }

            return false;
        }
        public static int Flush(string taskName)
        {
            if (string.IsNullOrWhiteSpace(taskName))
                return 0;

            int removed = 0;

            // Flush normal task queue
            lock (m_TaskQueue.SyncRoot)
            {
                removed += FlushQueue_NoLock(m_TaskQueue, taskName);
            }

            // Flush priority queue too (admin forced runs)
            lock (m_PriorityQueue.SyncRoot)
            {
                removed += FlushQueue_NoLock(m_PriorityQueue, taskName);
            }

            if (removed > 0)
                Console.WriteLine("Cron.Flush: Removed {0} queued instance(s) of task \"{1}\".", removed, taskName);

            return removed;
        }
        private static int FlushQueue_NoLock(Queue q, string taskName)
        {
            int removed = 0;

            if (q == null || q.Count == 0)
                return 0;

            int originalCount = q.Count;

            // Rebuild queue without the matching items.
            Queue rebuilt = new Queue(originalCount);

            while (q.Count > 0)
            {
                object? o = q.Dequeue();
                CronEventEntry? cee = o as CronEventEntry;

                if (cee != null && StringComparer.OrdinalIgnoreCase.Equals(cee.Name, taskName))
                {
                    removed++;
                    continue;
                }

                rebuilt.Enqueue(o);
            }

            // Put back
            while (rebuilt.Count > 0)
                q.Enqueue(rebuilt.Dequeue());

            return removed;
        }
        #endregion Cron Helpers
    }
}