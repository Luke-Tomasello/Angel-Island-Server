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

/* Scripts/Engines/AIMusicSystem/Core/MusicEngineMetrics.cs
 * ChangeLog
 *  11/6/26, Adam (2026 modernization, 10/26/26 to 11/6/26)
 *      Created. Measures the music engine without changing playback: note and tick lateness, timer and script
 *      activity, main-thread time, sessions, drift per engine run (actual against scored time) beside a plain 100 ms
 *      reference probe, and sound packets; for MusicLoadTest, the AI's scheduling, main-thread CPU and a stall log.
 *      Anchored runs keep their own drift and re-anchor counts. A summary per 5-minute window goes to
 *      Logs/MusicEngine.log ([MusicMetrics report closes a window now).
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: the Logs folder, and whether MusicEngine.log is written (MusicEngineLog; measuring goes on either
 *      way). Angel Island only (#if ANGEL_ISLAND): the timer queue depth, cron's recent tasks and the AI mux observer.
 */

using Server.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Server.Commands
{
    public static class MusicEngineMetrics
    {
        public enum Kind { Engine, Script }

        // a lateness distribution (milliseconds)
        public sealed class Lateness
        {
            public long Count, Over10, Over25, Over50, Over100, Over250;
            public double Sum, Max;
            private readonly int[] m_hist = new int[1001];  // 1 ms buckets; the last holds 1000 ms and over
            public double Average => Count == 0 ? 0 : Sum / Count;
            public void Add(double ms)
            {
                if (ms < 0) ms = 0;                 // early (the timer thread runs a timer once now > due)
                Count++;
                Sum += ms;
                if (ms > Max) Max = ms;
                if (ms > 10) Over10++;
                if (ms > 25) Over25++;
                if (ms > 50) Over50++;
                if (ms > 100) Over100++;
                if (ms > 250) Over250++;
                m_hist[Math.Min(1000, (int)ms)]++;
            }
            // the lateness (whole ms) that p percent of the samples do not exceed
            public int Percentile(double p)
            {
                long target = (long)Math.Ceiling(Count * p / 100.0), seen = 0;
                for (int i = 0; i < m_hist.Length; i++)
                    if ((seen += m_hist[i]) >= target)
                        return i;
                return m_hist.Length - 1;
            }
            public override string ToString() => Count == 0 ? "none" :
                string.Format("avg {0:0.0} ms, p99 {7} ms, max {1:0} ms, >25 ms {2}, >50 ms {3}, >100 ms {4}, >250 ms {5} (of {6})",
                    Average, Max, Over25, Over50, Over100, Over250, Count, Percentile(99));
        }

        // drift of completed engine runs: actual playing time minus scored time (milliseconds; negative = early)
        public sealed class Drift
        {
            public long Count;
            public double Sum, Max, MaxPercent, ScoredSum;
            public double Average => Count == 0 ? 0 : Sum / Count;
            public double Percent => ScoredSum <= 0 ? 0 : Sum * 100.0 / ScoredSum;
            public void Add(double driftMs, double scoredMs)
            {
                Count++;
                Sum += driftMs;
                ScoredSum += scoredMs;
                if (driftMs > Max) Max = driftMs;
                if (scoredMs > 0 && driftMs * 100.0 / scoredMs > MaxPercent) MaxPercent = driftMs * 100.0 / scoredMs;
            }
            public override string ToString() => Count == 0 ? "none" :
                string.Format("avg {0:0} ms, max {1:0} ms; {2:0.0}% over {3:0.0} s scored (worst run {4:0.0}%; {5} runs)",
                    Average, Max, Percent, ScoredSum / 1000.0, MaxPercent, Count);
        }

        // one engine run: a session's timer from ProcessPlayQueue until its play list runs out
        public sealed class Run
        {
            internal readonly long Start = Stopwatch.GetTimestamp();
            internal readonly int Saves = s_saves;
            internal double ScoredMs;                               // the delays the score asked for so far
            internal bool Ended;
            internal bool Anchored;                                 // an Anchored session's run (its own drift figures)
        }

        // one window's measurements
        public sealed class Window
        {
            public DateTime Start = DateTime.UtcNow;
            public long Notes, ClassicTicks, DynamicTicks, ScriptCallbacks, Packets;
            public long EngineTicks, ScriptTicks;                   // Stopwatch ticks on the main thread
            public int PeakSessions, PeakTimers;
            public long RunsInterrupted, RunsExcluded;
            public readonly Lateness Classic = new(), Dynamic = new(), Probe = new();
            public readonly Drift Drift = new(), AnchoredDrift = new();
            public long Reanchors, ScriptReanchors;                 // Anchored schedules shifted after an interruption (notes, script waits)
            public long SoundTicks, SendTicks;                      // PlaySound, and its packet build and send (Stopwatch ticks)
            public long QueueSum, QueueSamples, Stalls;
            public int QueueMax;
            // AI scheduling (BaseAI.MuxTick), while held
            public readonly Lateness Mux = new();
            public long MuxTicks, DMuxTicks, DMuxMaxTicks, Thoughts, ActiveSum;
            public int ActiveMax;
            public TimeSpan? CpuAtStart;                            // the main thread's processor time when the window began (held)
            public bool Active => Notes + ClassicTicks + DynamicTicks + ScriptCallbacks > 0;
            public double EngineMs => EngineTicks * 1000.0 / Stopwatch.Frequency;
            public double ScriptMs => ScriptTicks * 1000.0 / Stopwatch.Frequency;
            public double SoundMs => SoundTicks * 1000.0 / Stopwatch.Frequency;
            public double SendMs => SendTicks * 1000.0 / Stopwatch.Frequency;
            public double DMuxMs => DMuxTicks * 1000.0 / Stopwatch.Frequency;
            public double DMuxMaxMs => DMuxMaxTicks * 1000.0 / Stopwatch.Frequency;  // the longest single DMux
            public double ActiveAverage => MuxTicks == 0 ? 0 : (double)ActiveSum / MuxTicks;
            public double QueueAverage => QueueSamples == 0 ? 0 : (double)QueueSum / QueueSamples;
            public double? ClosedSeconds;                           // the window's length, fixed when it closes
            public double Seconds => ClosedSeconds ?? Math.Max(0.001, (DateTime.UtcNow - Start).TotalSeconds);
            public double? CpuPercent;                              // set when the window closes (held)
        }

        public static TimeSpan ReportInterval = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan ProbeInterval = TimeSpan.FromMilliseconds(100);
        private static Window s_window = new();
        private static Timer? s_reportTimer;
        private static Timer? s_probe;
        private static long s_probeLast;                            // Stopwatch time of the probe's previous tick
        private static int s_probeSaves;
        private static int s_gc0, s_gc1, s_gc2;                     // GC collection counts at the probe's previous tick
        public const double StallMs = 150;                          // a probe gap this late is a stall (logged)
        private static int s_saves;                                 // world saves so far (a save stalls every timer)
        // engine timers running per session (by reference: MusicContext is a record); a session can briefly have two
        private static readonly Dictionary<MusicContext, int> s_running = new(ReferenceEqualityComparer.Instance);
        private static int s_timers;
        private static int s_depth;                                 // nested measured calls (recursion) count once
        private static Kind s_outer;

        public static Window Current => s_window;
        public static int Sessions => s_running.Count;
        public static int Timers => s_timers;

        #region Main-thread time
        // using var scope = MusicEngineMetrics.Enter(Kind.Engine): times the outermost measured call on the main thread
        public readonly struct Scope : IDisposable
        {
            private readonly long m_start;
            internal Scope(long start) { m_start = start; }
            public void Dispose() { Exit(m_start); }
        }
        public static Scope Enter(Kind kind)
        {
            if (s_depth++ == 0)
            {
                s_outer = kind;
                Activity();
                return new Scope(Stopwatch.GetTimestamp());
            }
            return new Scope(0);
        }
        private static void Exit(long start)
        {
            if (--s_depth < 0)
                s_depth = 0;
            if (start == 0)
                return;
            long elapsed = Stopwatch.GetTimestamp() - start;
            if (s_outer == Kind.Engine)
                s_window.EngineTicks += elapsed;
            else
                s_window.ScriptTicks += elapsed;
        }
        // true while inside a measured call that is not the outermost one (a recursive step of the same tick)
        public static bool Nested => s_depth > 1;
        #endregion Main-thread time

        #region Events
        public static void ClassicTick(double lateMs, bool measured)
        {
            s_window.ClassicTicks++;
            if (measured)
                s_window.Classic.Add(lateMs);
        }
        public static void DynamicTick(double lateMs, bool measured)
        {
            s_window.DynamicTicks++;
            if (measured)
                s_window.Dynamic.Add(lateMs);
        }
        public static void ScriptCallback() { s_window.ScriptCallbacks++; }
        public static void Note() { s_window.Notes++; }
        public static void Packet() { s_window.Packets++; }
        // one PlaySound: its whole time since start (Stopwatch ticks), of which sendTicks built and sent packets
        public static void Sound(long start, long sendTicks)
        {
            s_window.SoundTicks += Stopwatch.GetTimestamp() - start;
            s_window.SendTicks += sendTicks;
        }

        // AI scheduling (BaseAI.MuxObserver while held): one MuxTick
        private static void MuxSample(double lateMs, long dmuxTicks, int thoughts, int active)
        {
            if (lateMs >= 0)
                s_window.Mux.Add(lateMs);
            s_window.MuxTicks++;
            s_window.DMuxTicks += dmuxTicks;
            if (dmuxTicks > s_window.DMuxMaxTicks) s_window.DMuxMaxTicks = dmuxTicks;
            s_window.Thoughts += thoughts;
            s_window.ActiveSum += active;
            if (active > s_window.ActiveMax) s_window.ActiveMax = active;
        }

        // engine runs: started by ProcessPlayQueue, scored time added as the timer schedules, ended once
        public static Run RunStarted(bool anchored = false) => new Run { Anchored = anchored };
        // an Anchored schedule shifted after an interruption: a note's (script: false) or a script wait's (script: true)
        public static void Reanchor(bool script)
        {
            if (script)
                s_window.ScriptReanchors++;
            else
                s_window.Reanchors++;
        }
        public static void RunScored(Run? run, double ms)
        {
            if (run != null && !run.Ended)
                run.ScoredMs += ms;
        }
        public static void RunEnded(Run? run, bool interrupted)
        {
            if (run == null || run.Ended)
                return;
            run.Ended = true;
            if (interrupted)
                s_window.RunsInterrupted++;
            else if (run.Saves != s_saves || World.Saving)
                s_window.RunsExcluded++;
            else
                (run.Anchored ? s_window.AnchoredDrift : s_window.Drift).Add(LateMs(run.Start) - run.ScoredMs, run.ScoredMs);
        }

        // an engine timer started / ended for a session
        public static void TimerStarted(MusicContext? session)
        {
            if (session == null)
                return;
            s_running.TryGetValue(session, out int n);
            s_running[session] = n + 1;
            s_timers++;
            if (s_running.Count > s_window.PeakSessions) s_window.PeakSessions = s_running.Count;
            if (s_timers > s_window.PeakTimers) s_window.PeakTimers = s_timers;
            Activity();
        }
        public static void TimerEnded(MusicContext? session)
        {
            if (session == null || !s_running.TryGetValue(session, out int n))
                return;
            if (n <= 1)
                s_running.Remove(session);
            else
                s_running[session] = n - 1;
            s_timers--;
        }

        // the due time carried by a timer state (Stopwatch ticks), and how late 'now' is against it
        public static long Due(TimeSpan delay) => Stopwatch.GetTimestamp() + (long)(delay.TotalSeconds * Stopwatch.Frequency);
        public static double LateMs(long due) => (Stopwatch.GetTimestamp() - due) * 1000.0 / Stopwatch.Frequency;
        public static long Now => Stopwatch.GetTimestamp();
        #endregion Events

        #region Reference probe
        // a plain 100 ms timer: how late the main loop runs timers, with or without music
        private static void StartProbe()
        {
            if (s_probe != null && s_probe.Running)
                return;
            s_probeLast = 0;
            s_gc0 = GC.CollectionCount(0); s_gc1 = GC.CollectionCount(1); s_gc2 = GC.CollectionCount(2);
            s_probe = Timer.DelayCall(ProbeInterval, ProbeInterval, OnProbe);
        }
        private static void StopProbe()
        {
            s_probe?.Stop();
            s_probe = null;
        }
        private static void OnProbe()
        {
            long now = Stopwatch.GetTimestamp();
            bool saved = s_probeSaves != s_saves || World.Saving;
            int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
            if (s_probeLast != 0)
            {
                double late = LateMs(s_probeLast) - ProbeInterval.TotalMilliseconds;
                if (!saved)
                    s_window.Probe.Add(late);
                if (late > StallMs)
                {   // a stall: what was the main thread doing?
                    s_window.Stalls++;
                    AppendLog(string.Format("STALL {0:0} ms: world save {1}; GC during it gen0 +{2}, gen1 +{3}, gen2 +{4}; cron recent tasks (id, time) [{5}]",
                        late, saved ? "yes" : "no", gc0 - s_gc0, gc1 - s_gc1, gc2 - s_gc2, CronRecentTasks()));
                }
            }
            int depth;
#if ANGEL_ISLAND
            lock (Timer.Queue) { depth = Timer.Queue.Count; }
#else
            depth = 0;  // the timer queue's depth: Angel Island's core only
#endif
            s_window.QueueSum += depth;
            s_window.QueueSamples++;
            if (depth > s_window.QueueMax) s_window.QueueMax = depth;
            s_probeLast = now;
            s_probeSaves = s_saves;
            s_gc0 = gc0; s_gc1 = gc1; s_gc2 = gc2;
        }
        private static string CronRecentTasks()
        {
#if ANGEL_ISLAND
            try { return Server.Engines.CronScheduler.Cron.GetRecentTasks(); }
            catch (Exception ex) { return "unavailable: " + ex.Message; }
#else
            return "unavailable";
#endif
        }
        #endregion Reference probe

        #region Hold (MusicLoadTest)
        // a load test holds the metrics: its phases close the windows, the AI scheduler and the main thread's CPU are
        //  measured, and the 5-minute report timer stays out of the way
        public static bool Held { get; private set; }
        private static int s_mainThreadId;
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
        public static void BeginHold()
        {
            Held = true;
            try { s_mainThreadId = (int)GetCurrentThreadId(); }     // called on the main thread (a command or a timer)
            catch { s_mainThreadId = 0; }
#if ANGEL_ISLAND
            Server.Mobiles.BaseAI.MuxObserver = MuxSample;
#endif
            Activity();
        }
        public static void EndHold()
        {
#if ANGEL_ISLAND
            Server.Mobiles.BaseAI.MuxObserver = null;
#endif
            Held = false;
        }
        // the main thread's processor time (null when it cannot be read)
        public static TimeSpan? MainThreadCpu()
        {
            if (s_mainThreadId == 0)
                return null;
            try
            {
                using Process process = Process.GetCurrentProcess();
                foreach (ProcessThread thread in process.Threads)
                    if (thread.Id == s_mainThreadId)
                        return thread.TotalProcessorTime;
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
            return null;
        }
        private static Window NewWindow() => new Window { PeakSessions = s_running.Count, PeakTimers = s_timers, CpuAtStart = Held ? MainThreadCpu() : null };
        #endregion Hold (MusicLoadTest)

        #region Command
        public static void Initialize()
        {
            EventSink.WorldSave += new WorldSaveEventHandler(OnWorldSave);
            CommandSystem.Register("MusicMetrics", AccessLevel.Administrator, new CommandEventHandler(MusicMetrics_OnCommand));
        }
        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            s_saves++;
        }
        [Usage("MusicMetrics report [label]")]
        [Description("Closes the music engine's measurement window now and writes its summary to the console and Logs/MusicEngine.log; measuring continues.")]
        private static void MusicMetrics_OnCommand(CommandEventArgs e)
        {
            if (e.Length < 1 || !e.GetString(0).Equals("report", StringComparison.OrdinalIgnoreCase))
            {
                e.Mobile.SendMessage("Usage: [MusicMetrics report [label]");
                return;
            }
            string label = e.Length > 1 ? string.Join(" ", e.Arguments, 1, e.Length - 1) : "report";
            Activity();                                             // measure from this mark on
            Window w = s_window;
            Report(label);
            e.Mobile.SendMessage("Music engine window closed ({0}, {1:0} s): notes {2}, probe {3}, drift {4}. {5}",
                label, (DateTime.UtcNow - w.Start).TotalSeconds, w.Notes,
                w.Probe.Count == 0 ? "none" : string.Format("avg {0:0.0} ms, max {1:0} ms", w.Probe.Average, w.Probe.Max), w.Drift,
                AimsHost.Current.MusicEngineLog ? "Details in Logs/MusicEngine.log." : "(MusicEngine.log is off on this shard.)");
        }
        #endregion Command

        #region Reporting
        private static void Activity()
        {
            if (s_reportTimer == null || !s_reportTimer.Running)
            {
                s_reportTimer = Timer.DelayCall(ReportInterval, ReportInterval, OnReport);
                AimsLog.DebugOut("Music engine: measuring (Phase 4 Step 4a); a summary every " + ReportInterval.TotalMinutes + " minutes while music plays", ConsoleColor.Cyan);
            }
            StartProbe();
        }
        private static void OnReport()
        {
            if (Held)
                return;                                             // a load test closes its own windows
            if (!s_window.Active && s_running.Count == 0)
            {   // nothing played in a whole window: stop until the next activity
                s_reportTimer?.Stop();
                s_reportTimer = null;
                StopProbe();
                s_window = new Window();
                AimsLog.DebugOut("Music engine: idle; measurements paused", ConsoleColor.Cyan);
                return;
            }
            Report("window");
        }
        /// <summary>
        /// Writes the current window's summary (console in cyan: Debug builds; Logs/MusicEngine.log: every build) and
        /// starts a new window. Returns the summary.
        /// </summary>
        public static string Report(string reason)
        {
            return Report(reason, out _);
        }
        public static string Report(string reason, out Window closed)
        {
            Window w = closed = s_window;
            double seconds = w.Seconds;
            w.ClosedSeconds = seconds;
            if (w.CpuAtStart is TimeSpan cpu0 && MainThreadCpu() is TimeSpan cpu1)
                w.CpuPercent = (cpu1 - cpu0).TotalMilliseconds / (seconds * 10.0);
            s_window = NewWindow();
            string text = string.Format(
                "Music engine ({0}, {1:0}s): sessions now {2} (engine timers {3}), peak {4} ({5}); notes {6}; engine ticks {7} (classic {8}, dynamic {9}), " +
                "script callbacks {10}; main thread {11:0.0} ms (engine {12:0.0}, script {13:0.0}; {14:0.000}% of the window); " +
                "classic tick lateness {15}; dynamic note lateness {16}; probe (main loop, 100 ms timer) lateness {17}; " +
                "packets {18} ({19:0.0} per note); run drift {20}; runs interrupted {21}, excluded (world save) {22}",
                reason, seconds, s_running.Count, s_timers, w.PeakSessions, w.PeakTimers, w.Notes, w.ClassicTicks + w.DynamicTicks, w.ClassicTicks,
                w.DynamicTicks, w.ScriptCallbacks, w.EngineMs + w.ScriptMs, w.EngineMs, w.ScriptMs, (w.EngineMs + w.ScriptMs) / (seconds * 10.0),
                w.Classic, w.Dynamic, w.Probe, w.Packets, w.Notes == 0 ? 0.0 : (double)w.Packets / w.Notes, w.Drift, w.RunsInterrupted, w.RunsExcluded);
            if (w.AnchoredDrift.Count > 0 || w.Reanchors + w.ScriptReanchors > 0)
                text += string.Format("; anchored run drift {0}; re-anchors {1} (script waits {2})", w.AnchoredDrift, w.Reanchors, w.ScriptReanchors);
            text += string.Format("; sound {0:0.0} ms (packets built and sent {1:0.0} ms); timer queue avg {2:0.0}, max {3}; stalls {4}",
                w.SoundMs, w.SendMs, w.QueueAverage, w.QueueMax, w.Stalls);
            if (w.MuxTicks > 0)
                text += string.Format("; AI scheduler (MuxTick) ticks {0}, lateness {1}; DMux {2:0.0} ms ({3:0.000}% of the window, longest tick {8:0.0} ms); thoughts {4} ({5:0.0}/s); running AIs avg {6:0.0}, max {7}",
                    w.MuxTicks, w.Mux, w.DMuxMs, w.DMuxMs / (seconds * 10.0), w.Thoughts, w.Thoughts / seconds, w.ActiveAverage, w.ActiveMax, w.DMuxMaxMs);
            if (w.CpuPercent is double percent)
                text += string.Format("; main-thread CPU {0:0.00}%", percent);
            AimsLog.DebugOut(text, ConsoleColor.Cyan);
            AppendLog(text);
            return text;
        }
        // one line in Logs/MusicEngine.log (unless the shard turns it off: AimsHost.MusicEngineLog)
        public static void AppendLog(string text)
        {
            if (!AimsHost.Current.MusicEngineLog)
                return;

            try
            {
                string path = Path.Combine(AimsHost.Current.LogsDirectory, "MusicEngine.log");
                AimsUtil.EnsurePath(path);
                File.AppendAllText(path, string.Format("{0:u} {1}{2}", DateTime.UtcNow, text, Environment.NewLine));
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        #endregion Reporting
    }
}
