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

/* Scripts\Engines\CronScheduler\CronScheduler.cs
 * CHANGELOG:
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
                if (arg == null || e.Arguments.Length < 2)
                {
                    PauseUsage(e.Mobile);
                    return;
                }
                string command = arg.ToLower();
                string[] toks = command.Split(' ');
                string span = toks.Last();
                TimeSpan ts;
                if (TimeSpan.TryParse(span, out ts) == false)
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
                    CronEventEntry cee = Cron.Find(cmd);
                    if (cee == null)
                        e.Mobile.SendMessage("No task named \"{0}\".", cmd);
                    else
                    {   // running certain commands can do damage on the production server.
                        //  check to make sure we are on TC, or that the user is sure thay want to run this command.
                        if (cee.Name.ToLower() == "plantgrowth" && Core.UOTC_CFG == false)
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
                            Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(PriorityTick), new object[] { null });
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
                    Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(PriorityTick), new object[] { null });
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
            CronEventEntry cee = Cron.Find("ReceiveMessage");
            if (cee == null)
                return; // No so named task named task
            else
            {   // queue it!
                lock (m_PriorityQueue.SyncRoot)
                    m_PriorityQueue.Enqueue(cee);

                lock (m_MessageQueue.SyncRoot)
                    m_MessageQueue.Enqueue(new ServerMessage(message, args));

                // launch the Priority timer
                Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(PriorityTick), new object[] { null });
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
            lock (m_IdleQueue.SyncRoot)
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

        public static void QueuePriorityTask(string Name, CronEventHandler handler, string CronSpec, bool Unique)
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

        public static void QueueIdleTask(string Name, CronEventHandler handler, string CronSpec, bool Unique)
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

        public static void QueueTask(string Name, CronEventHandler handler, string CronSpec, bool Unique)
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
        public static void Register(CronEventHandler handler, string CronSpec)
        {
            Register(handler, CronSpec, true);
        }
        public static void Register(CronEventHandler handler, string CronSpec, bool Unique)
        {
            Register(handler, CronSpec, Unique, null);
        }
        public static void Register(CronEventHandler handler, string CronSpec, bool Unique, CronLimit limit)
        {
            lock (m_Handlers.SyncRoot)
            {
                m_Handlers.Add(new CronEventEntry(null, handler, new CronJob(CronSpec), Unique, limit));
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
                    CronEventEntry cee = o as CronEventEntry;
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
                    CronEventEntry cee = o as CronEventEntry;
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
                    CronEventEntry cee = o as CronEventEntry;
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
                    CronEventEntry cee = o as CronEventEntry;
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

        public static CronEventEntry Find(string pattern)
        {
            lock (m_Handlers.SyncRoot)
            {
                foreach (object o in m_Handlers)
                {
                    CronEventEntry cee = o as CronEventEntry;
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
                Console.WriteLine("Scheduler: ({0} Server Time): ", serverTime);
                Console.WriteLine("Scheduler: Timer Queue: {0}, Priority Queue: {1}, Task Queue: {2}, Idle Queue: {3}", timerQueueDepth, GetPriorityQueueDepth(), GetTaskQueueDepth(), GetIdleQueueDepth());

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
                    {   // is there anything to do in the Priority queue?
                        if (m_PriorityQueue.Count != 0)
                        {   // process the next scheduled job
                            object o;
                            lock (m_PriorityQueue.SyncRoot)
                                o = m_PriorityQueue.Dequeue();
                            CronProcess(o as CronEventEntry);
                        }
                        // is there anything to do in the main 'Task' queue?
                        else if (m_TaskQueue.Count != 0)
                        {   // process the next scheduled job
                            object o;
                            lock (m_TaskQueue.SyncRoot)
                                o = m_TaskQueue.Dequeue();
                            CronProcess(o as CronEventEntry);
                        }
                        // if nothing in the main queue, check the idle queue
                        else if (m_IdleQueue.Count != 0)
                        {   // process the next scheduled job
                            object o;
                            lock (m_IdleQueue.SyncRoot)
                                o = m_IdleQueue.Dequeue();
                            CronProcess(o as CronEventEntry);
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

                    anyWork = m_PriorityQueue.Count + m_TaskQueue.Count + m_IdleQueue.Count > 0;

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

        } // CronProcess()

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
                            CronEventEntry cee = o as CronEventEntry;
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
                                        CronEventEntry temp = m_TaskQueue.Dequeue() as CronEventEntry;
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
            private CronLimit m_Limit;

            public CronEventHandler Handler
            { get { return m_Handler; } }

            public CronJob Cronjob
            { get { return m_Cronjob; } }

            public string Name
            { get { return m_Name; } }

            public bool Unique
            { get { return m_Unique; } }

            public bool Running
            {
                get { return m_Running; }
                set { m_Running = value; }
            }

            public CronLimit Limit
            { get { return m_Limit; } }

            public CronEventEntry(string Name, CronEventHandler handler, CronJob Cronjob, bool Unique, CronLimit limit)
            {
                // infer a name from the handler passed in (if one was not supplied)
                m_Name = (Name != null) ? Name : (handler != null && handler.Method != null && handler.Method.Name != null) ? handler.Method.Name : "Unknown";
                m_Unique = Unique;
                m_Handler = handler;
                m_Cronjob = Cronjob;
                m_Limit = limit;
            }
        }

        // limit the cron specification to a specific day within the month. e.g., 3rd Sunday
        public class CronLimit
        {   // is this the last day of the month? (ldom)
            public enum isldom { dont_care, must_be_ldom, must_not_be_ldom };
            private isldom m_isldom;
            private int m_Nth;
            private DayOfWeek m_dayName;
            public CronLimit(int Nth, DayOfWeek dayName)
            {
                m_Nth = Nth;
                m_dayName = dayName;
                m_isldom = isldom.dont_care;
            }
            public CronLimit(int Nth, DayOfWeek dayName, isldom status)
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

                if (m_isldom == isldom.dont_care) // the right day and nothing else to check
                    return true;

                // sometimes the user wants to know if this is the last day of the month
                //	if IsTomorrowThisMonth then it can't be the last day of the month (tomorrow = +24 hours)
                bool last_dom = Cron.CronJob.IsTomorrowThisMonth() == false;

                // if last_dom == true and the user wanted the last day of the month, yay
                if (last_dom == true && m_isldom == isldom.must_be_ldom)
                    return true;

                // if last_dom == false and the user did not want the last day of the monthg, yay
                if (last_dom == false && m_isldom == isldom.must_not_be_ldom)
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
            string m_specification;
            public CronJob(string specification)
            {   // compile will normalize the specification, including locking '?' to a random value
                m_specification = specification;
                m_specification = Compile();
            }

            public string Specification
            {
                get { return m_specification; }
                set { m_specification = value; }
            }

            //	are today and tomorrow in the same month?
            public static bool IsTomorrowThisMonth()
            {
                DateTime now = DateTime.UtcNow;
                DateTime tomorrow = now + TimeSpan.FromDays(1);
                if (tomorrow.Month == now.Month)
                    return true;
                return false;
            }

            // is it the Nth day of the month.
            //	I.e., 3rd Sunday
            public static bool IsNthDayOfMonth(int Nth, DayOfWeek dayName)
            {
                int day = (int)dayName;

                DateTime now = DateTime.UtcNow;
                if ((int)now.DayOfWeek != day)
                    return false;

                int count = 0;
                DateTime start = now;
                while (true)
                {
                    // okay, count this day
                    if ((int)start.DayOfWeek == day)
                        count++;

                    // are we at the beginning of the month?
                    if (start.Day == 1)
                        break;

                    // move backwards to the 1st
                    start = start.AddDays(-1.0);
                }

                // are we on the Nth DayOfWeek of the month?
                return (Nth == count);
            }

            public string Compile()
            {
                if (m_specification == null)
                    return null;

                string delimStr = " ";
                char[] delimiter = delimStr.ToCharArray();
                string[] split = m_specification.Split(delimiter, 5);
                if (split.Length != 5) return null; // format error

                return
                    this.Normalize(split[0], 60, 0, 59) + " " +   // minute
                    this.Normalize(split[1], 24, 0, 23) + " " +   // hour
                    this.Normalize(split[2], 31, 1, 31) + " " +   // day of the month
                    this.Normalize(split[3], 12, 1, 12) + " " +   // month
                    this.Normalize(split[4], 7, 0, 6);            // day of the week
            }

            public bool Match(DateTime time)
            {
                if (m_specification == null) return false;
                return Match(m_specification, time);
            }

            public bool Match(string specification, DateTime time)
            {
                string delimStr = " ";
                char[] delimiter = delimStr.ToCharArray();
                string[] split = specification.Split(delimiter, 5);
                if (split.Length != 5) return false; // format error
                return Minute(split[0], time.Minute) && Hour(split[1], time.Hour) && DayOfMonth(split[2], time.Day) && Month(split[3], time.Month) && DayOfWeek(split[4], (int)time.DayOfWeek);
            }

            // Minute (0-59)
            bool Minute(string specification, int minute)
            {
                specification = Normalize(specification, 60, 0, 59);
                if (RangeCheck(specification, 60, 0, 59) == false) return false;
                return Common(specification, 60, minute);
            }

            // Hour (0-23)
            bool Hour(string specification, int hour)
            {
                specification = Normalize(specification, 24, 0, 23);
                if (RangeCheck(specification, 24, 0, 23) == false) return false;
                return Common(specification, 24, hour);
            }

            // Day of Month (1-31)
            bool DayOfMonth(string specification, int dayOfMonth)
            {
                specification = Normalize(specification, 31, 1, 31);
                if (RangeCheck(specification, 31, 1, 31) == false) return false;
                return Common(specification, 31, dayOfMonth);
            }

            // Month (1-12)
            bool Month(string specification, int month)
            {
                specification = Normalize(specification, 12, 1, 12);
                if (RangeCheck(specification, 12, 1, 12) == false) return false;
                return Common(specification, 12, month);
            }

            // Day of Week (0-6 or Sun-Sat)
            bool DayOfWeek(string specification, int dayOfWeek)
            {
                specification = Normalize(specification, 7, 0, 6);
                if (RangeCheck(specification, 7, 0, 6) == false) return false;
                return Common(specification, 7, dayOfWeek);
            }

            bool Common(string specification, int elements, int value)
            {
                // always a match
                if (specification == "*")
                    return true;

                string delimStr = ",";
                char[] delimiter = delimStr.ToCharArray();
                string[] split = specification.Split(delimiter, elements);

                for (int ix = 0; ix < split.Length; ix++)
                    if (Convert.ToInt32(split[ix]) == value)
                        return true;

                return false;
            }

            bool RangeCheck(string specification, int elements, int min, int max)
            {
                // always a match
                if (specification == "*")
                    return true;

                string delimStr = ",";
                char[] delimiter = delimStr.ToCharArray();
                string[] split = specification.Split(delimiter, elements);

                for (int ix = 0; ix < split.Length; ix++)
                {
                    int value = Convert.ToInt32(split[ix]);
                    if (value < min || value > max)
                    {
                        Console.WriteLine("Error: Bad format in Cron Matcher");
                        return false;
                    }
                }

                return true;
            }

            //////////////////////////////////////////////////////////////////////////////////
            //		Ranges of numbers are allowed.  Ranges are two numbers separated with a
            //		hyphen.  The specified range is inclusive.	 For example, 8-11 for an
            //		``hours'' entry specifies execution at hours 8, 9, 10 and 11.
            //
            //		Lists are allowed.	 A list is a set of numbers (or ranges) separated by
            //		commas.  Examples: `1,2,5,9', `0-4,8-12'.
            //
            //		Step values can be used in conjunction with ranges.  Following a range
            //		with `/' specifies skips of the number's value through the
            //		range.  For example, `0-23/2' can be used in the hours field to specify
            //		command execution every other hour (the alternative in the V7 standard is
            //		`0,2,4,6,8,10,12,14,16,18,20,22').  Steps are also permitted after an
            //		asterisk, so if you want to say `every two hours', just use `*/2'.

            string Normalize(string specification, int elements, int min, int max)
            {
                // always a match
                if (specification == "*")
                    return specification;

                // hmm, not safe if we NEED to hit our time
                if (specification.Contains("?"))
                {   // generate a random value for this specification
                    int rnd = Utility.Random(min, max + 1);
                    specification = specification.Replace("?", rnd.ToString());
                }

                string delimStr = ",";
                char[] delimiter = delimStr.ToCharArray();
                string[] split = specification.Split(delimiter, elements);
                string NewString = "";

                for (int ix = 0; ix < split.Length; ix++)
                {
                    string termDelimStr = "-/";
                    char[] termDelim = termDelimStr.ToCharArray();

                    // if it's just an number append it to the output
                    if (split[ix].IndexOfAny(termDelim) == -1)
                    {
                        NewString += (NewString.Length > 0) ? "," + split[ix] : split[ix];
                        continue;
                    }

                    // now parse start, stop, [and step] 
                    string[] term = split[ix].Split(termDelim, 3);

                    int start = 0;
                    int stop = 0;
                    int step = 0;

                    if (term[0] == "*")
                    {
                        start = min;
                        stop = max;
                        step = 0;
                        if (term.Length == 2)
                            step = Fix(Convert.ToInt32(term[1]), min, max);
                    }
                    else
                    {

                        if (term.Length < 2)
                        {
                            Console.WriteLine("Error: Bad format in Cron Matcher");
                            return specification;
                        }

                        // normalize start/stop
                        start = Fix(Convert.ToInt32(term[0]), min, max);
                        stop = Fix(Convert.ToInt32(term[1]), min, max);

                        step = 0;
                        if (term.Length == 3)
                            step = Fix(Convert.ToInt32(term[2]), min, max);
                    }

                    for (int jx = start; jx <= stop; jx++)
                    {
                        if (step > 0)
                        {
                            if (jx % step == 0)
                                NewString += (NewString.Length > 0) ? "," + jx.ToString() : jx.ToString();
                        }
                        else
                            NewString += (NewString.Length > 0) ? "," + jx.ToString() : jx.ToString();
                    }
                }

                return NewString;
            }

            int Fix(int value, int min, int max)
            {
                value = Math.Min(value, max);   // if value > max, value = max
                value = Math.Max(value, min);   // if value < min, value = min
                return value;
            }
        }
        #endregion CRON ENGINE
    }
}