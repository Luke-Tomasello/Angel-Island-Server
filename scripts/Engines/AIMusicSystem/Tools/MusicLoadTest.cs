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

/* Scripts/Engines/AIMusicSystem/Tools/MusicLoadTest.cs
 * ChangeLog
 *  10/26/26, Adam (AIMusicSystem Phase 4 Step 4a addendum: music versus AI load test)
 *      Created. Does music take main-thread time from gameplay? Plays sheet music through the normal double-click path
 *      (RolledUpSheetMusic.OnDoubleClick by the sheet's owner) in alternating phases, OFF ON OFF ON OFF, and measures
 *      each phase with MusicEngineMetrics: AI scheduling (MuxTick lateness, DMux time, thoughts, running AIs),
 *      main-thread CPU, music engine, script and sound/packet time, the probe, timer queue depth and stalls.
 *      An ON phase lasts until every performance has completed; an OFF phase lasts OffPhase (3 minutes by default).
 *      [MusicLoadTest check <serials>: read only. The serials are the sheets (each in its performer's backpack) and the
 *          champion engines. Checks the sheets and performers (a real client, not on the internal map, FilterMusic
 *          off, not playing, out of each other's music range), the engines (running, creatures) and, over a 5 second
 *          sample, that the engines' creatures are actually thinking. Reports the cron scheduler's state (it is never
 *          changed here).
 *      [MusicLoadTest run [offSeconds] [label]: runs the phases with the sheets and engines of the last passing check.
 *      [MusicLoadTest stop: cancels a run (and its performances).  [MusicLoadTest status
 *      Results: Logs/MusicEngine.log (one window per phase, then a summary). Nothing is changed in the world except the
 *      performances themselves.
 */

using Server.Engines.ChampionSpawn;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Server.Commands
{
    public static class MusicLoadTest
    {
        public static TimeSpan OffPhase = TimeSpan.FromMinutes(3);
        public static readonly TimeSpan MaxOnPhase = TimeSpan.FromMinutes(10);     // a performance still playing then is cancelled
        public static readonly TimeSpan CheckSample = TimeSpan.FromSeconds(5);     // how long check watches the creatures think
        public static readonly TimeSpan CheckValidFor = TimeSpan.FromMinutes(30);  // run needs a check passed this recently
        public const double ThinkingShare = 0.8;                                    // of each engine's creatures, at least this many must think
        private const int Phases = 5;                                               // OFF ON OFF ON OFF

        // the last passing check
        private static List<RolledUpSheetMusic> s_sheets = new();
        private static List<ChampEngine> s_engines = new();
        private static DateTime s_checkPassedAt = DateTime.MinValue;
        private static bool s_checking;

        // the run
        private static bool s_running;
        private static string s_label = "";
        private static int s_phase;
        private static DateTime s_phaseStart;
        private static Timer? s_phaseTimer;
        private static Mobile? s_operator;
        private static readonly List<RazorInstrument.Performance> s_performances = new();
        private static readonly List<string> s_reasons = new();
        private static Dictionary<BaseCreature, long> s_thoughtsAtPhaseStart = new();
        private static readonly List<(bool on, MusicEngineMetrics.Window w)> s_results = new();

        public static void Initialize()
        {
            Server.CommandSystem.Register("MusicLoadTest", AccessLevel.Administrator, new CommandEventHandler(MusicLoadTest_OnCommand));
        }

        [Usage("MusicLoadTest check <sheet and engine serials> | run [offSeconds] [label] | stop | status")]
        [Description("Music versus AI load test: alternating music-off and music-on phases, measured (Logs/MusicEngine.log).")]
        private static void MusicLoadTest_OnCommand(CommandEventArgs e)
        {
            string sub = e.Length > 0 ? e.GetString(0).ToLowerInvariant() : "";
            switch (sub)
            {
                case "check": Check(e.Mobile, e.Arguments.Skip(1).ToArray()); break;
                case "run": Run(e.Mobile, e.Arguments.Skip(1).ToArray()); break;
                case "stop": Stop(e.Mobile); break;
                case "status": Status(e.Mobile); break;
                default:
                    e.Mobile.SendMessage("Usage: [MusicLoadTest check <sheet and engine serials> | run [offSeconds] [label] | stop | status");
                    break;
            }
        }

        #region Check
        private static void Check(Mobile from, string[] args)
        {
            if (s_running) { from.SendMessage("A load test is running ([MusicLoadTest status)."); return; }
            if (s_checking) { from.SendMessage("A check is already sampling; wait a few seconds."); return; }
            if (args.Length == 0) { from.SendMessage("Usage: [MusicLoadTest check <sheet and engine serials>"); return; }

            List<string> lines = new();
            bool ok = true;
            void Fail(string text) { ok = false; lines.Add("FAIL " + text); }
            void Info(string text) { lines.Add("     " + text); }

            List<RolledUpSheetMusic> sheets = new();
            List<ChampEngine> engines = new();
            foreach (string arg in args)
            {
                int value = Utility.ToInt32(arg, -1);
                Item? item = value > 0 ? World.FindItem((Serial)value) : null;
                if (item is RolledUpSheetMusic rsm && !rsm.Deleted) sheets.Add(rsm);
                else if (item is ChampEngine engine && !engine.Deleted) engines.Add(engine);
                else Fail(string.Format("{0}: not a sheet of music or a champion engine", arg));
            }
            if (sheets.Count == 0) Fail("no sheets of music");
            if (engines.Count == 0) Fail("no champion engines");

            // sheets and performers
            List<PlayerMobile> performers = new();
            foreach (RolledUpSheetMusic rsm in sheets)
            {
                PlayerMobile? pm = rsm.RootParent as PlayerMobile;
                string head = string.Format("sheet 0x{0:X8} '{1}'", rsm.Serial.Value, rsm.Title);
                if (pm == null) { Fail(head + ": not carried by a player"); continue; }
                head += string.Format(" / {0} (0x{1:X8})", pm.Name, pm.Serial.Value);
                List<string> problems = new();
                if (pm.Backpack == null || !rsm.IsChildOf(pm.Backpack)) problems.Add("not in the backpack");
                if (pm.NetState == null) problems.Add("no client connected");
                if (pm.Map == null || pm.Map == Map.Internal) problems.Add("not in the world");
                if (pm.FilterMusic) problems.Add("FilterMusic is on");
                if (rsm.CurrentPerformance.InProgress) problems.Add("already playing");
                if (performers.Contains(pm)) problems.Add("this player already has a sheet in the test");
                if (problems.Count > 0) Fail(head + ": " + string.Join(", ", problems));
                else Info(head + string.Format(": ready at {0} {1}; other clients within music range {2}", pm.Location, pm.Map, OtherClients(pm)));
                performers.Add(pm);
            }
            if (sheets.Select(s => s.Title).Distinct().Count() > 1)
                Info("note: the sheets are not all the same song: " + string.Join(", ", sheets.Select(s => "'" + s.Title + "'").Distinct()));

            // music-range separation (a performer within range of another hears both)
            int range = Core.GlobalMaxUpdateRange;
            for (int i = 0; i < performers.Count; i++)
                for (int j = i + 1; j < performers.Count; j++)
                    if (performers[i].Map == performers[j].Map && performers[i].InRange(performers[j], range))
                        Fail(string.Format("{0} and {1} are within music range of each other ({2} tiles)", performers[i].Name, performers[j].Name, range));

            // engines and their creatures
            Dictionary<BaseCreature, long> thoughts0 = new();
            foreach (ChampEngine engine in engines)
            {
                List<BaseCreature> creatures = Creatures(engine);
                int aiRunning = creatures.Count(bc => bc.AIObject != null && bc.AIObject.Active);
                PlayerMobile? near = performers.Where(p => p.Map == engine.Map).OrderBy(p => p.GetDistanceToSqrt(engine.Location)).FirstOrDefault();
                string head = string.Format("engine 0x{0:X8} at {1} {2}: running {3}, creatures {4}, AI running {5}; nearest performer {6}",
                    engine.Serial.Value, engine.Location, engine.Map, engine.Running ? "yes" : "NO", creatures.Count, aiRunning,
                    near == null ? "none on this map" : string.Format("{0} ({1:0} tiles)", near.Name, near.GetDistanceToSqrt(engine.Location)));
                if (creatures.Count == 0) Fail(head + ": no creatures");
                else Info(head);
                foreach (BaseCreature bc in creatures)
                    if (bc.AIObject != null)
                        thoughts0[bc] = bc.AIObject.MuxThoughts;
            }
            Info("cron scheduler: " + CronState() + " (not changed by this test)");

            if (!ok)
            {
                lines.Add("CHECK FAILED");
                Send(from, lines, "check");
                return;
            }

            // are the creatures actually thinking?
            s_checking = true;
            from.SendMessage("Watching the creatures think for {0} seconds...", CheckSample.TotalSeconds);
            Timer.DelayCall(CheckSample, () =>
            {
                s_checking = false;
                bool thinking = true;
                foreach (ChampEngine engine in engines)
                {
                    List<BaseCreature> creatures = Creatures(engine);
                    int thought = creatures.Count(bc => bc.AIObject != null && thoughts0.TryGetValue(bc, out long t0) && bc.AIObject.MuxThoughts > t0);
                    string text = string.Format("engine 0x{0:X8}: {1} of {2} creatures thought in {3} s", engine.Serial.Value, thought, creatures.Count, CheckSample.TotalSeconds);
                    if (creatures.Count == 0 || thought < Math.Max(1, ThinkingShare * creatures.Count))
                    {
                        thinking = false;
                        lines.Add("FAIL " + text + " (is a performer near enough to keep the area active?)");
                    }
                    else
                        lines.Add("     " + text);
                }
                if (thinking)
                {
                    s_sheets = sheets;
                    s_engines = engines;
                    s_checkPassedAt = DateTime.UtcNow;
                    lines.Add(string.Format("CHECK PASSED: {0} sheets, {1} engines. [MusicLoadTest run [offSeconds] [label] within {2} minutes.",
                        sheets.Count, engines.Count, CheckValidFor.TotalMinutes));
                }
                else
                    lines.Add("CHECK FAILED");
                Send(from, lines, "check");
            });
        }

        private static List<BaseCreature> Creatures(ChampEngine engine)
        {
            List<BaseCreature> list = new();
            if (engine.Monsters != null)
                foreach (object o in engine.Monsters)
                    if (o is BaseCreature bc && !bc.Deleted)
                        list.Add(bc);
            return list;
        }
        private static int OtherClients(Mobile m)
        {
            int count = 0;
            if (m.Map == null || m.Map == Map.Internal)
                return 0;
            IPooledEnumerable eable = m.Map.GetClientsInRange(m.Location, Core.GlobalMaxUpdateRange);
            foreach (NetState ns in eable)
                if (ns.Mobile != null && ns.Mobile != m)
                    count++;
            eable.Free();
            return count;
        }
        // the cron scheduler's pause state, read only (the field is private to the scheduler)
        private static string CronState()
        {
            try
            {
                FieldInfo? f = typeof(Server.Engines.CronScheduler.Cron).GetField("CronPaused", BindingFlags.NonPublic | BindingFlags.Static);
                if (f?.GetValue(null) is bool paused)
                    return paused ? "PAUSED" : "running";
            }
            catch (Exception ex)
            {
                Server.Diagnostics.LogHelper.LogException(ex);
            }
            return "unknown";
        }
        #endregion Check

        #region Run
        private static void Run(Mobile from, string[] args)
        {
            if (s_running) { from.SendMessage("A load test is already running ([MusicLoadTest status)."); return; }
            if (s_checking) { from.SendMessage("A check is sampling; wait for its result."); return; }
            if (DateTime.UtcNow - s_checkPassedAt > CheckValidFor || s_sheets.Count == 0)
            {
                from.SendMessage(0x22, string.Format("Not started: run [MusicLoadTest check first (a passing check is valid for {0} minutes).", CheckValidFor.TotalMinutes));
                return;
            }
            // the world may have moved on since the check
            foreach (RolledUpSheetMusic rsm in s_sheets)
            {
                PlayerMobile? pm = rsm.RootParent as PlayerMobile;
                if (rsm.Deleted || pm == null || pm.NetState == null || pm.Backpack == null || !rsm.IsChildOf(pm.Backpack) || rsm.CurrentPerformance.InProgress)
                {
                    from.SendMessage("Sheet 0x{0:X8} is no longer ready; run [MusicLoadTest check again.", rsm.Serial.Value);
                    return;
                }
            }

            TimeSpan off = OffPhase;
            int next = 0;
            if (args.Length > 0 && int.TryParse(args[0], out int seconds) && seconds >= 30)
            {
                off = TimeSpan.FromSeconds(seconds);
                next = 1;
            }
            OffPhase = off;
            s_label = args.Length > next ? string.Join(" ", args.Skip(next)) : "run";
            s_operator = from;
            s_running = true;
            s_phase = 0;
            s_results.Clear();
            s_performances.Clear();
            s_reasons.Clear();

            MusicEngineMetrics.BeginHold();
            MusicEngineMetrics.AppendLog(string.Format("LOADTEST {0}: begin; {1} sheets, {2} engines; OFF phases {3:0} s; cron {4}",
                s_label, s_sheets.Count, s_engines.Count, OffPhase.TotalSeconds, CronState()));
            MusicEngineMetrics.Report(string.Format("LOADTEST {0}: before the test (not a phase)", s_label));
            from.SendMessage("Load test '{0}' started: OFF ON OFF ON OFF. [MusicLoadTest status to follow, [MusicLoadTest stop to cancel.", s_label);
            NextPhase();
        }

        private static bool IsOn(int phase) => phase % 2 == 0;     // 1 OFF, 2 ON, 3 OFF, 4 ON, 5 OFF

        private static void NextPhase()
        {
            if (!s_running)
                return;
            s_phase++;
            if (s_phase > Phases)
            {
                Finish();
                return;
            }
            s_phaseStart = DateTime.UtcNow;
            s_thoughtsAtPhaseStart = EngineThoughts();
            s_operator?.SendMessage("Load test: phase {0}/{1} {2}", s_phase, Phases, IsOn(s_phase) ? "ON (music)" : "OFF");

            if (!IsOn(s_phase))
            {
                s_phaseTimer = Timer.DelayCall(OffPhase, EndPhase);
                return;
            }

            // ON: every sheet starts through the normal double-click path, together
            s_performances.Clear();
            foreach (RolledUpSheetMusic rsm in s_sheets)
            {
                PlayerMobile? pm = rsm.RootParent as PlayerMobile;
                RazorInstrument.Performance? before = rsm.CurrentPerformance;
                if (pm != null)
                    rsm.OnDoubleClick(pm);
                RazorInstrument.Performance performance = rsm.CurrentPerformance;
                if (pm == null || ReferenceEquals(performance, before) || !performance.InProgress)
                {
                    Abort(string.Format("sheet 0x{0:X8} did not start playing", rsm.Serial.Value));
                    return;
                }
                performance.Completed += (p, reason) => s_reasons.Add(reason.ToString());
                s_performances.Add(performance);
            }
            // the phase ends when every performance has completed
            s_phaseTimer = Timer.DelayCall(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), WatchOnPhase);
        }

        private static void WatchOnPhase()
        {
            if (s_performances.All(p => p.State == RazorInstrument.PerformanceState.Completed))
            {
                EndPhase();
                return;
            }
            if (DateTime.UtcNow - s_phaseStart > MaxOnPhase)
            {
                MusicEngineMetrics.AppendLog(string.Format("LOADTEST {0}: phase {1} still playing after {2} minutes; cancelling", s_label, s_phase, MaxOnPhase.TotalMinutes));
                foreach (RazorInstrument.Performance p in s_performances)
                    if (p.InProgress)
                        p.Cancel(say: false);
            }
        }

        private static void EndPhase()
        {
            s_phaseTimer?.Stop();
            s_phaseTimer = null;
            if (!s_running)
                return;

            // the engines' creatures during this phase
            Dictionary<BaseCreature, long> now = EngineThoughts();
            int creatures = now.Count, thought = 0;
            long thoughts = 0;
            foreach (var kvp in now)
                if (s_thoughtsAtPhaseStart.TryGetValue(kvp.Key, out long t0) && kvp.Value > t0)
                {
                    thought++;
                    thoughts += kvp.Value - t0;
                }
            int connected = s_sheets.Count(s => s.RootParent is PlayerMobile pm && pm.NetState != null);
            string reason = string.Format("LOADTEST {0}: phase {1}/{2} {3} (cron {4}; engine creatures thinking {5}/{6}, their thoughts {7}; performers connected {8}/{9}{10})",
                s_label, s_phase, Phases, IsOn(s_phase) ? "ON" : "OFF", CronState(), thought, creatures, thoughts, connected, s_sheets.Count,
                IsOn(s_phase) ? "; performances ended: " + string.Join(", ", s_reasons) : "");
            s_reasons.Clear();
            MusicEngineMetrics.Report(reason, out MusicEngineMetrics.Window w);
            s_results.Add((IsOn(s_phase), w));
            NextPhase();
        }

        private static Dictionary<BaseCreature, long> EngineThoughts()
        {
            Dictionary<BaseCreature, long> thoughts = new();
            foreach (ChampEngine engine in s_engines)
                if (!engine.Deleted)
                    foreach (BaseCreature bc in Creatures(engine))
                        if (bc.AIObject != null)
                            thoughts[bc] = bc.AIObject.MuxThoughts;
            return thoughts;
        }

        private static void Finish()
        {
            MusicEngineMetrics.EndHold();
            s_running = false;
            List<string> lines = new() { string.Format("LOADTEST {0}: summary (per phase)", s_label) };
            int n = 0;
            foreach ((bool on, MusicEngineMetrics.Window w) in s_results)
                lines.Add(Row(++n, on, w));
            lines.Add(Compare());
            foreach (string line in lines)
                MusicEngineMetrics.AppendLog(line);
            s_operator?.SendMessage("Load test '{0}' finished. Results in Logs/MusicEngine.log.", s_label);
            s_operator?.SendMessage(lines[lines.Count - 1]);
        }

        private static string Row(int n, bool on, MusicEngineMetrics.Window w)
        {
            double s = w.Seconds;
            return string.Format("  phase {0} {1,-3} {2,4:0} s | MuxTick late avg {3:0.0} p99 {4} max {5:0} ms | DMux {6:0.00} ms/s, longest {20:0.0} ms | thoughts {7:0}/s | running AIs {8:0} | " +
                "main-thread CPU {9} | music {10:0.00} ms/s (sound {11:0.00}, send {12:0.00}) | notes {13} packets {14} | probe avg {15:0.0} p99 {16} ms | queue avg {17:0.0} max {18} | stalls {19}",
                n, on ? "ON" : "OFF", s, w.Mux.Average, w.Mux.Percentile(99), w.Mux.Max, w.DMuxMs / s, w.Thoughts / s, w.ActiveAverage,
                w.CpuPercent is double c ? c.ToString("0.00") + "%" : "n/a", (w.EngineMs + w.ScriptMs) / s, w.SoundMs / s, w.SendMs / s, w.Notes, w.Packets,
                w.Probe.Average, w.Probe.Percentile(99), w.QueueAverage, w.QueueMax, w.Stalls, w.DMuxMaxMs);
        }

        // OFF phases against ON phases (time-weighted averages)
        private static string Compare()
        {
            string Side(bool on)
            {
                var ws = s_results.Where(r => r.on == on).Select(r => r.w).ToList();
                double secs = ws.Sum(w => w.Seconds);
                if (ws.Count == 0 || secs <= 0) return "none";
                double late = ws.Sum(w => w.Mux.Sum) / Math.Max(1, ws.Sum(w => w.Mux.Count));
                double dmux = ws.Sum(w => w.DMuxMs) / secs;
                double music = ws.Sum(w => w.EngineMs + w.ScriptMs) / secs;
                var cpus = ws.Where(w => w.CpuPercent != null).ToList();
                string cpu = cpus.Count == 0 ? "n/a" : (cpus.Sum(w => w.CpuPercent!.Value * w.Seconds) / cpus.Sum(w => w.Seconds)).ToString("0.00") + "%";
                return string.Format("MuxTick late avg {0:0.00} ms, worst p99 {1} ms; DMux {2:0.00} ms/s; main-thread CPU {3}; music {4:0.00} ms/s; stalls {5}",
                    late, ws.Max(w => w.Mux.Percentile(99)), dmux, cpu, music, ws.Sum(w => w.Stalls));
            }
            return string.Format("LOADTEST {0}: OFF: {1} || ON: {2}", s_label, Side(false), Side(true));
        }

        private static void Stop(Mobile from)
        {
            if (!s_running) { from.SendMessage("No load test is running."); return; }
            Abort("stopped by " + from.Name);
            from.SendMessage("Load test stopped.");
        }

        private static void Abort(string why)
        {
            s_phaseTimer?.Stop();
            s_phaseTimer = null;
            foreach (RazorInstrument.Performance p in s_performances)
                if (p.InProgress)
                    p.Cancel(say: false);
            MusicEngineMetrics.Report(string.Format("LOADTEST {0}: ABORTED in phase {1}: {2}", s_label, s_phase, why));
            MusicEngineMetrics.EndHold();
            s_running = false;
            s_operator?.SendMessage("Load test '{0}' aborted: {1}", s_label, why);
        }

        private static void Status(Mobile from)
        {
            if (s_running)
                from.SendMessage("Load test '{0}': phase {1}/{2} {3}, {4:0} s in; performances in progress {5}; cron {6}.", s_label, s_phase, Phases,
                    IsOn(s_phase) ? "ON" : "OFF", (DateTime.UtcNow - s_phaseStart).TotalSeconds, s_performances.Count(p => p.InProgress), CronState());
            else
                from.SendMessage("No load test running. Last passing check: {0}; cron {1}.",
                    s_checkPassedAt == DateTime.MinValue ? "none" : string.Format("{0:0} minutes ago", (DateTime.UtcNow - s_checkPassedAt).TotalMinutes), CronState());
        }
        #endregion Run

        private static void Send(Mobile from, List<string> lines, string what)
        {
            foreach (string line in lines)
            {
                from.SendMessage(line);
                MusicEngineMetrics.AppendLog("LOADTEST " + what + ": " + line.Trim());
            }
        }
    }
}
