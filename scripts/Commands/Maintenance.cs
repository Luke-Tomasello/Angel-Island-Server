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

/* Scripts/Commands/Maintenance.cs
 * CHANGELOG
 *  11/22/26, Adam (the saves-disabled warning)
 *      Scheduling a maintenance while saves are disabled says how to save: cancel it, [AutoSave on, schedule it again. It
 *      said "[AutoSave on before it ends", which is refused: [AutoSave is locked while a maintenance is scheduled. The
 *      skipped final save no longer suggests it either (it's too late by then).
 *  11/22/26, Adam (one source of truth for saves)
 *      Whether this world saves is AutoSave.SavesEnabled alone. The final save (shutdown, restart, rebuild, the save
 *      before Server Wars) happens when saves are enabled and is skipped, and said, when they're not: a -nosaves shard
 *      starts with them off, and [AutoSave on turns them back on (a development world can then be kept with a shutdown).
 *      It read the launch switch (Core.NoSaves, now gone), so [AutoSave on was ignored. Maintenance no longer turns
 *      automatic saves off for its countdown, or remembers and restores them (SuspendAutoSaves / RestoreAutoSaves are
 *      gone): the cron autosave simply doesn't run while a maintenance is scheduled (CronTasks). Scheduling one while
 *      saves are disabled warns the caller that it won't save.
 *  11/22/26, Adam (nothing to cancel)
 *      cancel with nothing scheduled (no restart, shutdown, rebuild or Server Wars) says "There is no maintenance to
 *      cancel." to the caller and changes nothing; it told everyone "Server maintenance has been canceled." every time.
 *  11/22/26, Adam (shutting down)
 *      A shutdown's countdown says so: "Server shutting down in N minutes for maintenance...", "Server shutting down
 *      now..." (it said restarting for both). A restart still says restarting. "The server is already restarting", when
 *      maintenance is already scheduled, names which it is too.
 *  11/22/26, Adam (now)
 *      restart, shutdown, rebuild and serverwars take "now" as well as a datetime: no countdown, the timer's first tick
 *      does it (the final save, unless -nosaves, then the restart or shutdown). Players get no warning. Works on every
 *      shard. The usage says what no time means (5 minutes from now), and cancel no longer shows a datetime (it never
 *      took one).
 *  11/21/26, Adam (no final save on a server launched without saves)
 *      The final save (shutdown, restart, rebuild, and the one before Server Wars) is skipped when the server was
 *      launched with -nosaves (Core.NoSaves), and the console and staff are told. A development server's world on disk
 *      is its baseline: a maintenance shutdown no longer writes over it (and moves the old one into Backups).
 *      Production never has -nosaves; a Debug build with -saves (a stand-in for production) still saves. [save still
 *      saves: save first to keep a development world.
 *  11/21/26, Adam (automatic saves restored, not forced on)
 *      Scheduling a maintenance (restart, shutdown, rebuild, Server Wars) remembers whether automatic saves were on;
 *      cancelling it, or a rebuild that fails to start, puts that back. Both turned them on: on a -nosaves shard
 *      (or after a staff [SetSaves false) a cancelled maintenance left automatic saves running until a restart.
 *  1/4/22, Adam
 *      Have output to console be red
 *	12/23/08, Adam
 *		Update to use RunUO 2.0 restart model
 *	5/7/08, Adam
 *		echo countdown to the console
 *  11/13/06, Kit
 *      Added in Rebuild option, shutdown server, merge cvs, and restart server.
 *	6/2/06, Adam
 *		Initial Revision.
 *		Shuts the server down or restarts it with a minutely message of the form:
 *		"Server restarting in N minutes for maintenance..."
 */

using Server.Diagnostics;
using Server.Engines.CronScheduler;
using Server.Regions;
using System.Collections.Generic;

namespace Server.Misc
{
    using System;
    using System.Globalization;

    public class Maintenance
    {
        private static MaintenanceTimer m_Maintenance;

        private static bool m_Scheduled;
        private static bool m_Shutdown;
        private static bool m_Rebuild;

        private static DateTime m_When;
        private static int m_Countdown;

        // scheduled with saves disabled: tell the caller this one won't save (and how to make it)
        private static void WarnIfNoSave(CommandEventArgs e, string what)
        {
            if (!AutoSave.SavesEnabled)
                e.Mobile.SendMessage("Saves are disabled: this {0} will not save. To save: [Maintenance cancel, [AutoSave on, then schedule it again.", what);
        }

        // Server Wars state
        private static bool m_ServerWarsScheduled; // "this maintenance run is server wars flavored"
        private static bool m_InServerWars;        // "final save has happened; wars are in progress"
        private static DateTime m_ServerWarsEndLocal;

        public static bool Scheduled { get { return m_Scheduled; } set { m_Scheduled = value; } }
        public static bool Shutdown { get { return m_Shutdown; } set { m_Shutdown = value; } }
        public static bool Rebuild { get { return m_Rebuild; } set { m_Rebuild = value; } }

        public static DateTime When { get { return m_When; } set { m_When = value; } }
        public static int Countdown { get { return m_Countdown; } set { m_Countdown = value; } }

        public static bool ServerWarsScheduled { get { return m_ServerWarsScheduled; } set { m_ServerWarsScheduled = value; } }
        public static bool InServerWars { get { return m_InServerWars; } set { m_InServerWars = value; } }
        public static DateTime ServerWarsEndLocal { get { return m_ServerWarsEndLocal; } set { m_ServerWarsEndLocal = value; } }
        private static bool m_FinalSave;
        public static bool FinalSave
        {
            get { return m_FinalSave; }
            private set { m_FinalSave = value; }
        }

        public static void Initialize()
        {
            CommandSystem.Register("Maintenance", AccessLevel.Administrator, new CommandEventHandler(Maintenance_OnCommand));
            m_Maintenance = new MaintenanceTimer();
        }

        private static void Usage(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Usage:");
            e.Mobile.SendMessage("[Maintenance restart|shutdown|rebuild [now|datetime]   (no time: in 5 minutes)");
            e.Mobile.SendMessage("[Maintenance serverwars [restart] [now|datetime]");
            e.Mobile.SendMessage("[Maintenance cancel");
            e.Mobile.SendMessage("Datetime examples: \"2026-02-27 22:30\" or \"2026-02-27 22:30Z\" or \"2026-02-27 22:30 -0800\"");
        }

        // Normalized datetime parsing: treat inputs as server-local unless an offset/UTC is explicitly present.
        //  "now" is now: no countdown (NormalizeCountdown gives 0).
        private static bool TryParseWhenLocal(string sx, out DateTime whenLocal)
        {
            whenLocal = DateTime.MinValue;

            if (sx.Trim().Equals("now", StringComparison.OrdinalIgnoreCase))
            {
                whenLocal = DateTime.Now;
                return true;
            }

            // Prefer DateTimeOffset to honor offsets/zulu if provided.
            DateTimeOffset dto;
            if (DateTimeOffset.TryParse(
                sx,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                out dto))
            {
                whenLocal = dto.LocalDateTime;
                return true;
            }

            // Fallback: DateTime parse, assume local for Unspecified.
            DateTime dt;
            if (DateTime.TryParse(
                sx,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                out dt))
            {
                if (dt.Kind == DateTimeKind.Utc)
                    whenLocal = dt.ToLocalTime();
                else
                    whenLocal = dt;

                return true;
            }

            return false;
        }

        private static void ComputeScheduleDefaultsAndOptionalWhen(CommandEventArgs e, out DateTime whenLocal, out int countdownMinutes)
        {
            // Default is 5 minutes from now (LOCAL).
            whenLocal = DateTime.Now + TimeSpan.FromMinutes(5);
            countdownMinutes = 5;

            if (e.Length <= 1)
                return;

            string sx = string.Empty;
            for (int ix = 1; ix < e.Length; ix++)
                sx += (e.GetString(ix) ?? string.Empty) + " ";

            sx = sx.Trim();

            DateTime parsedLocal;
            if (!TryParseWhenLocal(sx, out parsedLocal))
                throw new FormatException("Bad date format.");

            whenLocal = parsedLocal;

            // Normalize countdown: based on LOCAL now, not UTC.
            TimeSpan diff = whenLocal.Subtract(DateTime.Now);
            countdownMinutes = NormalizeCountdown(diff);
        }
        // Use Ceiling so partial minutes (e.g., 4m59s) are treated as 5 minutes remaining.
        // This preserves intuitive countdown behavior and prevents losing nearly a full minute.
        private static int NormalizeCountdown(TimeSpan diff)
        {
            int mins = (int)Math.Ceiling(diff.TotalMinutes);
            return mins < 0 ? 0 : mins;
        }
        private static void CancelMaintenance(CommandEventArgs e)
        {
            // nothing scheduled: say so, to the caller alone (no broadcast about nothing)
            if (!m_Scheduled && !m_ServerWarsScheduled && !m_InServerWars && !m_Rebuild)
            {
                e.Mobile.SendMessage("There is no maintenance to cancel.");
                return;
            }

            bool wasServerWars = m_ServerWarsScheduled || m_InServerWars;

            m_Scheduled = false;
            m_Shutdown = true;

            m_When = DateTime.MinValue;
            m_Countdown = 5;

            m_ServerWarsScheduled = false;
            m_InServerWars = false;
            m_ServerWarsEndLocal = DateTime.MinValue;

            if (wasServerWars)
                OnServerWarsCanceled();

            if (m_Rebuild)
            {
                if (KillRebuild())
                {
                    e.Mobile.SendMessage("Rebuild.exe canceled successfully.");
                    Rebuild = false;
                }
                else
                {
                    e.Mobile.SendMessage("Error closing rebuild.exe!!!");
                }
            }

            e.Mobile.SendMessage("Maintenance has been canceled.");
            World.Broadcast(0x482, true, "Server maintenance has been canceled.");

            m_Maintenance.Stop();
        }

        public static void Maintenance_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.Length == 0)
                {
                    Usage(e);
                    return;
                }

                string action = (e.GetString(0) ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant();

                // -------------------------------------------------
                // CANCEL
                // -------------------------------------------------
                if (action == "cancel")
                {
                    CancelMaintenance(e);
                    return;
                }

                // -------------------------------------------------
                // REBUILD
                // -------------------------------------------------
                if (action == "rebuild")
                {
                    if (Rebuild)
                    {
                        e.Mobile.SendMessage("The server is already preparing for a rebuild.");
                        return;
                    }

                    DateTime whenLocal;
                    int countdownMinutes;

                    try
                    {
                        ComputeScheduleDefaultsAndOptionalWhen(e, out whenLocal, out countdownMinutes);
                    }
                    catch (FormatException)
                    {
                        e.Mobile.SendMessage("Bad date format.");
                        e.Mobile.SendMessage("Maintenance not initiated.");
                        Usage(e);
                        return;
                    }

                    m_When = whenLocal;
                    m_Countdown = countdownMinutes;

                    Rebuild = true;
                    Shutdown = true;
                    Scheduled = true;

                    m_ServerWarsScheduled = false;
                    m_InServerWars = false;
                    m_ServerWarsEndLocal = DateTime.MinValue;

                    e.Mobile.SendMessage("You have initiated a server rebuild.");
                    WarnIfNoSave(e, "rebuild");
                    m_Maintenance.Start();

                    if (!StartRebuild(Engines.TestCenterServer.Enabled))
                    {
                        e.Mobile.SendMessage("Rebuild.exe failed to start, canceling rebuild.");

                        Rebuild = false;
                        Scheduled = false;
                        Shutdown = false;

                        m_Maintenance.Stop();
                    }

                    return;
                }

                // -------------------------------------------------
                // SERVER WARS  (replaces old [ServerWars])
                // -------------------------------------------------
                if (action == "serverwars")
                {
                    // [Maintenance serverwars cancel]
                    if (e.Length > 1)
                    {
                        string t1 = (e.GetString(1) ?? string.Empty)
                            .Trim()
                            .ToLowerInvariant();

                        if (t1 == "cancel")
                        {
                            CancelMaintenance(e);
                            return;
                        }
                    }

                    if (m_Scheduled)
                    {
                        e.Mobile.SendMessage("The server is already {0}.", m_Shutdown ? "shutting down" : "restarting");
                        return;
                    }

                    // Defaults: 5 minutes from now
                    DateTime whenLocal = DateTime.Now + TimeSpan.FromMinutes(5);
                    int countdownMinutes = 5;

                    bool restart = false;
                    int dtIndex = 1;

                    // Optional "restart"
                    if (e.Length > 1)
                    {
                        string t1 = (e.GetString(1) ?? string.Empty)
                            .Trim()
                            .ToLowerInvariant();

                        if (t1 == "restart")
                        {
                            restart = true;
                            dtIndex = 2;
                        }
                    }

                    // Optional datetime
                    if (e.Length > dtIndex)
                    {
                        string sx = string.Empty;
                        for (int ix = dtIndex; ix < e.Length; ix++)
                            sx += (e.GetString(ix) ?? string.Empty) + " ";

                        sx = sx.Trim();

                        DateTime parsedLocal;
                        if (!TryParseWhenLocal(sx, out parsedLocal))
                        {
                            e.Mobile.SendMessage("Bad date format.");
                            e.Mobile.SendMessage("Server Wars not initiated.");
                            Usage(e);
                            return;
                        }

                        whenLocal = parsedLocal;

                        // Normalize countdown: based on LOCAL now, not UTC.
                        TimeSpan diff = whenLocal.Subtract(DateTime.Now);
                        countdownMinutes = NormalizeCountdown(diff);
                    }

                    m_When = whenLocal;
                    m_Countdown = countdownMinutes;

                    m_Scheduled = true;
                    m_Shutdown = !restart;   // default shutdown after wars

                    m_ServerWarsScheduled = true;
                    m_InServerWars = false;
                    m_ServerWarsEndLocal = DateTime.MinValue;

                    e.Mobile.SendMessage(
                        "You have initiated Server Wars ({0} after wars).",
                        restart ? "restart" : "shutdown"
                    );
                    WarnIfNoSave(e, "Server Wars");

                    m_Maintenance.Start();
                    return;
                }

                // -------------------------------------------------
                // RESTART / SHUTDOWN
                // -------------------------------------------------
                if (action == "restart" || action == "shutdown")
                {
                    if (m_Scheduled)
                    {
                        e.Mobile.SendMessage("The server is already {0}.", m_Shutdown ? "shutting down" : "restarting");
                        return;
                    }

                    DateTime whenLocal;
                    int countdownMinutes;

                    try
                    {
                        ComputeScheduleDefaultsAndOptionalWhen(e, out whenLocal, out countdownMinutes);
                    }
                    catch (FormatException)
                    {
                        e.Mobile.SendMessage("Bad date format.");
                        e.Mobile.SendMessage("Maintenance not initiated.");
                        Usage(e);
                        return;
                    }

                    m_When = whenLocal;
                    m_Countdown = countdownMinutes;

                    m_Shutdown = (action != "restart");
                    m_Scheduled = true;

                    m_ServerWarsScheduled = false;
                    m_InServerWars = false;
                    m_ServerWarsEndLocal = DateTime.MinValue;

                    e.Mobile.SendMessage(
                        "You have initiated server {0}.",
                        m_Shutdown ? "shutdown" : "restart"
                    );
                    WarnIfNoSave(e, m_Shutdown ? "shutdown" : "restart");

                    m_Maintenance.Start();
                    return;
                }

                Usage(e);
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                e.Mobile.SendMessage("There was a problem with the [Maintenance command!!  See console log");
                Console.WriteLine("Error with [Maintenance!");
                Console.WriteLine(exc.Message);
                Console.WriteLine(exc.StackTrace);
            }
        }

        private static bool ShouldRestart
        {
            get { return !m_Shutdown; } // shutdown is default
        }

        internal static void Terminate(bool finalSave)
        {
            // Single choke point for "we are about to end the world"
            try
            {
                if (finalSave)
                    DoFinalSave();
            }
            finally
            {
                Core.Kill(ShouldRestart);
            }
        }

        // Stubs you already have elsewhere
        private static bool KillRebuild() { return true; }
        private static bool StartRebuild(bool testCenter) { return true; }

        private static Dictionary<GuardedRegion, bool>? m_GuardStateBackup;
        internal static void OnServerWarsStart()
        {
            m_GuardStateBackup = new Dictionary<GuardedRegion, bool>();

            foreach (Region rx in Region.Regions)
            {
                if (rx is GuardedRegion gr)
                {
                    m_GuardStateBackup[gr] = gr.IsGuarded;
                    gr.IsGuarded = false;
                }
            }
        }
        internal static void OnServerWarsCanceled()
        {
            RestoreGuards();
        }
        internal static void OnServerWarsEnd()
        {
            RestoreGuards();
        }
        private static void RestoreGuards()
        {
            if (m_GuardStateBackup == null)
                return;

            foreach (var kvp in m_GuardStateBackup)
            {
                if (kvp.Key != null)
                    kvp.Key.IsGuarded = kvp.Value;
            }

            m_GuardStateBackup.Clear();
            m_GuardStateBackup = null;
        }
        internal static void DoFinalSave()
        {
            // NO FINAL SAVE WHEN SAVES ARE DISABLED. One source of truth: AutoSave.SavesEnabled ("this world saves on its
            //  own"), read now, as the maintenance ends. A -nosaves shard starts with it off (its world on disk is a
            //  baseline: a development shutdown doesn't write over it); [AutoSave on, before the maintenance is scheduled
            //  ([AutoSave is locked while one is), turns it on, and then this saves.
            //  Production saves unless staff turned saves off (scheduling a maintenance then warns that it won't save).
            //  This covers every maintenance (shutdown, restart, rebuild) and the save before Server Wars.
            if (!AutoSave.SavesEnabled)
            {
                string text = "Maintenance: final save skipped: saves are disabled.";
                Utility.Monitor.WriteLine(text, ConsoleColor.Yellow);
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, text);
                return;
            }

            try
            {
                FinalSave = true;
                AutoSave.Save();
            }
            finally
            {
                FinalSave = false;
            }
        }
    }

    public class MaintenanceTimer : Timer
    {
        // Used to track which messages have been given already, to avoid spamming.
        private Memory PlayerMessageGiven = new Memory();
        private Memory StaffMessageGiven = new Memory();

        public MaintenanceTimer()
            : base(TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(1.0))
        {
            Priority = TimerPriority.FiveSeconds;
        }

        private static int CeilMinutesRemaining(DateTime endLocal)
        {
            TimeSpan rem = endLocal.Subtract(DateTime.Now);
            if (rem.TotalMinutes <= 0)
                return 0;

            return (int)Math.Ceiling(rem.TotalMinutes);
        }

        protected override void OnTick()
        {
            if (!Maintenance.Scheduled)
                return;

            string text;

            // -----------------------------------------
            // Phase B: Server Wars In Progress
            // -----------------------------------------
            if (Maintenance.ServerWarsScheduled && Maintenance.InServerWars)
            {
                int minsLeft = CeilMinutesRemaining(Maintenance.ServerWarsEndLocal);

                if (minsLeft == 0)
                {
                    PlayerMessageGiven.WipeMemory();
                    StaffMessageGiven.WipeMemory();

                    Maintenance.OnServerWarsEnd();

                    Maintenance.Terminate(finalSave: false);
                    return;
                }

                // Only announce final 5 minutes
                if (minsLeft <= 5)
                {
                    text = minsLeft > 1
                        ? $"Server Wars ending in {minsLeft} minutes..."
                        : "Server Wars ending in 1 minute...";

                    if (PlayerMessageGiven.Recall(text) == null)
                    {
                        PlayerMessageGiven.Remember(text, seconds: 90);
                        World.Broadcast(0x22, true, text);
                        Utility.Monitor.WriteLine(text, ConsoleColor.Red);
                    }
                }

                return;
            }

            // -----------------------------------------
            // Phase A: Pre-Save Countdown
            // -----------------------------------------

            if (Maintenance.Countdown > 5)
            {
                text = Maintenance.ServerWarsScheduled
                    ? $"[Staff] Server Wars begin in {Maintenance.Countdown} minutes"
                    : $"[Staff] Server {(Maintenance.Shutdown ? "shutdown" : "restart")} in {Maintenance.Countdown} minutes";

                if (StaffMessageGiven.Recall(text) == null)
                {
                    StaffMessageGiven.Remember(text, seconds: 90);
                    Server.Commands.CommandHandlers.BroadcastMessage(
                        AccessLevel.Counselor, 0x482, text);

                    Utility.Monitor.WriteLine(text, ConsoleColor.Red);
                }
            }
            else
            {
                if (Maintenance.ServerWarsScheduled)
                {
                    if (Maintenance.Countdown > 0)
                    {
                        text = $"Server Wars begin in {Maintenance.Countdown} minutes. Final save in {Maintenance.Countdown} minutes.";

                        if (PlayerMessageGiven.Recall(text) == null)
                        {
                            PlayerMessageGiven.Remember(text, seconds: 90);
                            World.Broadcast(0x22, true, text);
                            Utility.Monitor.WriteLine(text, ConsoleColor.Red);
                        }
                    }
                    else
                    {
                        // Transition into wars mode, perform final save, then announce.
                        try
                        {
                            Cron.Flush("AutoSave");
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogException(ex);
                        }

                        Maintenance.DoFinalSave();

                        Maintenance.InServerWars = true;
                        Maintenance.ServerWarsEndLocal = DateTime.Now + CoreAI.ServerWarsDuration;

                        PlayerMessageGiven.WipeMemory();
                        StaffMessageGiven.WipeMemory();

                        Maintenance.OnServerWarsStart();

                        const string startMsg = "Server Wars have begun!";
                        World.Broadcast(0x22, true, startMsg);
                        Utility.Monitor.WriteLine(startMsg, ConsoleColor.Red);

                        return;
                    }
                }
                else
                {
                    string doing = Maintenance.Shutdown ? "shutting down" : "restarting";
                    if (Maintenance.Countdown > 1)
                        text = $"Server {doing} in {Maintenance.Countdown} minutes for maintenance...";
                    else if (Maintenance.Countdown == 1)
                        text = $"Server {doing} in 1 minute for maintenance...";
                    else
                        text = $"Server {doing} now...";

                    if (PlayerMessageGiven.Recall(text) == null)
                    {
                        PlayerMessageGiven.Remember(text, seconds: 90);
                        World.Broadcast(0x22, true, text);
                        Utility.Monitor.WriteLine(text, ConsoleColor.Red);
                    }
                }
            }

            // -----------------------------------------
            // Termination Check
            // -----------------------------------------
            if (Maintenance.Countdown == 0)
            {
                PlayerMessageGiven.WipeMemory();
                StaffMessageGiven.WipeMemory();

                Maintenance.Terminate(finalSave: true);
                return;
            }

            Maintenance.Countdown--;
        }

    }
}

