/***************************************************************************
 *
 *                   The Angel Island Music System (AIMS)
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

/* Scripts/Skill Items/Musical Instruments/RazorInstrument.cs
 * ChangeLog
 *  11/8/26, Adam (2026 modernization, 10/23/26 to 11/8/26)
 *      A song's performance is an object (Performance, returned by StartMusic and StartDeviceMusic) with a State,
 *      Cancel() and Completed, raised exactly once with why it ended; a timer callback of an earlier performance stops
 *      quietly, and a performance whose session is replaced or released ends instead of playing into the new one.
 *      Music devices play on their own sessions. One performance per performer (PerformanceOf): a second sheet can no
 *      longer start in a song's pause and interleave with it. '# Timer: Classic|Dynamic|Anchored' selects a song's
 *      timer; in an Anchored song the waits keep exact time too. A song's '# Instrument:' takes effect on its first
 *      play. Duplicates no longer keep their source's playback links, and a songless sheet plays nothing (not 'Ride of
 *      the Valkyries'). Five unused settings are gone (MusicChatter, CheckMusicianship, IgnoreDirectives, MinMusicSkill,
 *      PreferredInstrument): Serialize version 8. Place_obsolete and Year_obsolete are gone too (the first award
 *      instruments' place and year; AwardInstrument keeps its own): Serialize version 9, and an older save's values are
 *      kept, unsaved, for [Nuke ReplaceAwardInstruments (LegacyAwardPlace, LegacyAwardYear). A world saved as version 8
 *      or 9 needs this build or later to load.
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: staff messages. Angel Island only (#if ANGEL_ISLAND): the unstealable, unlootable loot type (else
 *      blessed) and MusicMotionController.
 *      A '# Author:' line in the notes sets Author as the song plays unless the instrument keeps its own
 *      (KeepsItsAuthor: sheet music does, so a sheet keeps the author it was transcribed with).
 *  12/28/23, Adam (PlayerProgressCallback() => MusicConfig[player].Tick)
 *      Pass  a callback to the system player to notify us if the system is still processing notes. 
 *      Since this is called on every tick of the system player, it is our responsibility to limit processing so as to not impact playback.
 *      Typically, we (the caller) will only process the tick every two seconds or so to verify the player-mobile is still around to hear the music.
 *      Hierarchy: system player generates a timer tick to process some notes.
 *          the system player calls the MusicConfig[player].Tick()
 *          We (the RazorInstrument,) will only process this tick every two seconds. We will callback to the Calypso Music Player to handle it.
 *          The Calypso Music Player implements a CheckUser() function that verifies the mobile that played the music is still around to hear it.
 *          If Elvis has left the building, playback is canceled and everything cleaned up. 
 *  6/3/2023, Adam (Newbie'ness)
 *      Make RazorInstrument (LootType.UnStealable | LootType.UnLootable) so that is newbied for reds too 
 *  2/26/22, Adam
 *      Add support for ConcertMode
 *  2/19/22, Adam
 *      Major rework of the music system. Obsolete 'award instrument' specific variables from RazorInstrument and move them to AwardInstrument (new)
 *  2/10/22, Adam (StartMusic)
 *      Add a check to set a null instrument to 'this'
 *          i.e., m_instrument = instrument != null ? instrument : this;
 *  1/31/22, Adam
 *      Add a new callback to check the mobiles status (like have they logged out or moved away).
 *  1/27/22, Adam
 *      Have pause kill the song after 250 seconds.
 *  1/25/22, Adam
 *      Add support for a preferred instrument that overrides the RazorInstrument default (of harp)
 *  1/12/22, Adam
 *      Change Place 0 from "Loser" to "Honorable Mention"
 *  1/8/22, Adam (BabyRazor)
 *      Restart BabyRazor when a line (like blank lines) is unrecognized.
 *  1/2/21, adam (Play.Player)
 *      Use the new, more flexable Player interface that accepts finer grained configuration of the music player
 *          New properties include minMusicSkill, instrument, requiresBackpack, musicChatter, and checkMusicianship
 *  12/13/21, Adam
 *      First time checkin
 */

using Server.Commands;
using Server.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Server.Items
{
    public class RazorInstrument : BaseInstrument
    {

        private string m_author = null;
        private string m_songName = null;
        private string? m_songBytes = null;                         // simple song notes
        private string m_songFile = null;                           // read song from file
        private string m_songMemory = null;                         // read song from temporal memory buffer
        private bool m_newTimer;                                    // timer model specified in the comments of the song file
        private int m_tempo;                                        // songs speed (timer speed in milliseconds)
        private bool m_prefetch;                                    // prefetch pauses from queue, speeds playback
        private bool m_concertMode;                                 // concert mode updates sound packets for adjusting volume
        #region Performance
        // why a performance ended (Performance.Completed)
        public enum PerformanceEnd
        {
            Finished,           // the song played to its end (and its last notes have drained)
            Cancelled,          // cancelled by the player or the device (double-click, Cancel, the pause time-out, a newer start)
            StatusFailed,       // the status check (CheckSourceStatus) failed: the listener left, disconnected, ...
            SessionLost,        // its music context was replaced ([play reset) or released, or its performer is gone
            NothingToPlay,      // there was no playable song
        }
        public enum PerformanceState
        {
            Playing,            // under way, pauses between notes included (and the drain after the last note)
            Paused,             // paused by the player (Pause/Resume)
            Ending,             // cancelled or failed: nothing more is played; Completed follows once its notes have drained
            Completed,          // over: Completed has been raised
        }
        public delegate void PerformanceCompleted(Performance performance, PerformanceEnd reason);

        /// <summary>
        /// One execution of a song on this instrument. Everything that belongs to a single performance lives here: the
        /// note queue and script buffer, its playing and paused state, the session it plays on, the user check and the
        /// callbacks it was started with (AllDone and CheckSourceStatus are read from the instrument when it starts).
        /// Each start returns a new Performance, and the instrument plays its current one.
        /// The contract: State; Cancel(); and Completed(performance, reason), raised exactly once, at the real end of the
        /// performance: a cancelled or failed performance ends after the drain of its last notes (about half a second).
        /// Completed is never raised during the start, so subscribe right after it. A timer callback carries the
        /// Performance it was scheduled for: an ended or superseded performance's callbacks stop quietly and never touch
        /// a newer one's state.
        /// </summary>
        public sealed class Performance
        {
            internal readonly Queue<string> PlayQueue = new Queue<string>();     // notes waiting for the music player
            internal readonly Stack<string> SongBuffer = new Stack<string>();    // script lines still to run
            internal bool Playing = false;                                       // notes or script are being played (IsInstancePlaying)
            internal bool Paused = false;
            internal bool Unowned = false;                                       // plays on a music device's own session (no mobile; see StartDeviceMusic)
            internal Server.Commands.MusicContext? Session = null;               // the MusicContext it was configured with (session identity)
            internal bool SessionLost = false;                                   // that context was replaced or released mid-performance; the performance has been ended
            internal readonly AimsUtil.LocalTimer UserCheck = new AimsUtil.LocalTimer();   // the two second user check (PlayerProgressCallback)
            internal Mobile? Performer = null;                                   // the mobile whose context it plays on (null: a device session)
            internal FinishCallback? AllDone = null;                             // the instrument's AllDone when it started
            internal CheckSourceStatusCallback? CheckSourceStatus = null;        // the instrument's CheckSourceStatus when it started
            internal long ScriptDue = 0;                                         // Anchored: when the script's current wait is due to end (Stopwatch ticks; 0: none yet)
            private bool m_ending = false;
            private bool m_completed = false;
            private PerformanceEnd m_endReason = PerformanceEnd.Finished;

            public RazorInstrument Instrument { get; }
            public PerformanceState State =>
                m_completed ? PerformanceState.Completed : m_ending ? PerformanceState.Ending : Paused ? PerformanceState.Paused : PerformanceState.Playing;
            public bool InProgress => !m_ending && !m_completed;                // playing or paused
            public PerformanceEnd? Reason => m_completed ? m_endReason : null;  // why it ended (once Completed)
            public event PerformanceCompleted? Completed;

            internal Performance(RazorInstrument instrument, bool idle = false)
            {
                Instrument = instrument;
                m_completed = idle;                                             // the instrument's idle placeholder never runs
            }

            /// <summary>Cancels this performance if it is in progress; Completed(Cancelled) follows after the drain.</summary>
            public void Cancel(bool say = true)
            {
                Instrument.Cancel(this, PerformanceEnd.Cancelled, Instrument.m_playMobile ?? Performer, say);
            }

            // nothing more is played; Completed follows after the drain (WaitDone)
            internal bool BeginEnding(PerformanceEnd reason)
            {
                if (!InProgress)
                    return false;
                m_ending = true;
                m_endReason = reason;
                return true;
            }
            internal PerformanceEnd EndReason => m_endReason;
            // raises the end exactly once
            internal void Complete(PerformanceEnd reason)
            {
                if (m_completed)
                    return;
                if (!m_ending)
                    m_endReason = reason;
                m_completed = true;
                m_ending = false;
                Unregister(this);                                       // before the handlers: one of them may start the next song
                Playing = false;
                Paused = false;
                PlayQueue.Clear();
                SongBuffer.Clear();

                try { AllDone?.Invoke(); }
                catch (Exception ex) { AimsLog.LogException(ex); }

                PerformanceCompleted? completed = Completed;
                Completed = null;
                if (completed != null)
                    foreach (PerformanceCompleted handler in completed.GetInvocationList())
                        try { handler(this, m_endReason); }
                        catch (Exception ex) { AimsLog.LogException(ex); }
            }
        }
        private Performance m_current;                                      // the current (or last) performance: an idle one until the first start (not serialized)
        /// <summary>The current performance, or the last one (an idle, completed placeholder before the first start).</summary>
        public Performance CurrentPerformance { get { return m_current; } }

        // the performance each mobile's context is playing, from its start until it completes (one at a time per mobile)
        private static readonly Dictionary<Mobile, Performance> s_performing = new();
        /// <summary>
        /// The performance in progress (playing or paused) on this mobile's music context, or null. A cancelled or failing
        /// performance (Ending) no longer counts: nothing more of it is played. Nor does one whose context was released or
        /// replaced ([play reset, the invalid-context rebuild): it is over, though it completes only at its next timer
        /// tick. (A performance still starting has no session yet and counts.)
        /// </summary>
        public static Performance? PerformanceOf(Mobile? m)
        {
            return m != null && s_performing.TryGetValue(m, out Performance? p) && p.InProgress && !p.SessionLost
                && (p.Session == null || SystemMusicPlayer.IsCurrent(p.Session, m)) ? p : null;
        }
        private static void Register(Mobile m, Performance performance)
        {
            s_performing[m] = performance;
        }
        private static void Unregister(Performance performance)
        {
            if (performance.Performer is Mobile m && s_performing.TryGetValue(m, out Performance? p) && ReferenceEquals(p, performance))
                s_performing.Remove(m);
        }
        #endregion Performance
        #region configuration
        private Item? m_playItem;                                    // this is the item that 'sings the lyrics' (like the big fish trophy) (possibly obsolete)
        private Mobile m_playMobile;                                // this is the optional mobile that owns the MusicContext.
        private IEntity m_source;                                   // this is the item that 'sings the lyrics' (like the big fish trophy)
        private BaseInstrument m_instrument;
        private bool m_requiresBackpack = false;
        private bool m_instance_debugging = false;                 // display debug for this instance
        public delegate void FinishCallback();
        public delegate bool CheckSourceStatusCallback(Mobile m, IEntity source);
        private FinishCallback? m_allDone = null;
        private CheckSourceStatusCallback? m_checkSourceStatus = null;
        public bool IsInstancePlaying { get { return m_current.Playing; } }
        public IEntity Source { get { return m_source; } }
        public bool RequiresBackpack { get { return m_requiresBackpack; } set { m_requiresBackpack = value; } }
        // runtime playback links, set by the caller before a start: each Performance keeps the ones set when it started
        //  (changing them later does not affect a performance in progress). AllDone is raised with Completed, once.
        [CopyableAttribute(CopyType.DoNotCopy)]
        public FinishCallback? AllDone { get { return m_allDone; } set { m_allDone = value; } }
        [CopyableAttribute(CopyType.DoNotCopy)]
        public CheckSourceStatusCallback? CheckSourceStatus { get { return m_checkSourceStatus; } set { m_checkSourceStatus = value; } }
        // does this device fall back to the canned song (SongBytes) when it has no song file or song memory?
        protected virtual bool PlaysDefaultSong => true;
        private static readonly HashSet<Serial> s_reportedNothing = new HashSet<Serial>(); // devices already logged for "*plays nothing*"
        #endregion configuration
        [CommandProperty(AccessLevel.Administrator)]
        public bool InstanceDebugging { get { return m_instance_debugging; } set { m_instance_debugging = value; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool ConcertMode { get { return m_concertMode; } set { m_concertMode = value; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Prefetch { get { return m_prefetch; } set { m_prefetch = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Tempo { get { return m_tempo; } set { m_tempo = value; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool NewTimer { get { return m_newTimer; } set { m_newTimer = value; } }
        // the song's own '# Timer:' directive (the song being loaded or played), which takes precedence over NewTimer
        private TimerMode? m_timerHeader = null;
        /// <summary>The timer this device's song plays with: its '# Timer:' directive, else NewTimer (True: Dynamic, False: Classic).</summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public TimerMode TimerMode { get { return m_timerHeader ?? (m_newTimer ? TimerMode.Dynamic : TimerMode.Classic); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Author { get { return m_author; } set { m_author = value; } }
        // a '# Author:' line in the notes sets Author as the song plays, unless the instrument keeps its own (sheet music)
        protected virtual bool KeepsItsAuthor => false;
        [CommandProperty(AccessLevel.GameMaster)]
        public string SongName { get { return m_songName; } set { m_songName = value; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public string? SongBytes { get { return m_songBytes; } set { m_songBytes = value; } }
        // The place and year of the first award instruments (AwardHarp, AwardLapHarp, AwardLute, 2021-22: AwardInstrument
        //  replaced them and keeps its own), as read from a save before version 9, so [Nuke ReplaceAwardInstruments can
        //  still carry them over from an old world. Not saved any more (nothing else used them).
        public int LegacyAwardPlace { get; private set; }
        public int LegacyAwardYear { get; private set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public string SongFile
        {
            get { return m_songFile; }
            set
            {
                if (value != null)
                {
                    if (value.ToLower().Contains(".rzr") && File.Exists(Path.Combine(AimsUtil.DataDirectory, "song files", value)))
                        m_songFile = value;
                    else if (!value.ToLower().Contains(".rzr") || !File.Exists(Path.Combine(AimsUtil.DataDirectory, "song files", value)))
                        m_songFile = "Please enter: <name>.rzr";
                }
                else
                    // user is setting 
                    m_songFile = value;
            }
        }
        [CopyableAttribute(CopyType.DoNotCopy)]     // runtime playback link: set by the caller for each performance
        public Item? PlayItem { get { return m_playItem; } set { m_playItem = value; } }
        [CopyableAttribute(CopyType.DoNotCopy)]     // runtime playback link: set by the caller for each performance
        public Mobile PlayMobile { get { return m_playMobile; } set { m_playMobile = value; } }
        public string SongMemory { get { return m_songMemory; } set { m_songMemory = value; } }
        [CopyableAttribute(CopyType.DoNotCopy)]     // a copy plays itself, never its source
        public BaseInstrument Instrument { get { return m_instrument; } set { m_instrument = value; } }
        public Queue<string> PlayQueue { get { return m_current.PlayQueue; } }      // the current performance's
        public Stack<string> SongBuffer { get { return m_current.SongBuffer; } }
        public enum InstrumentSoundType : int
        {   // this enum defines the tonal qualties of the RazorInstrument default instrument
            None,
            Harp = 0x43,
            BadHarp,
            LapHarp = 0x45,
            BadLapHarp,
            Lute = 0x4C,
            BadLute
        }
        public static InstrumentSoundType GetSoundType(string instrument)
        {
            switch (instrument.ToLower())
            {
                default:
                case "harp": return InstrumentSoundType.Harp;
                case "lapharp": return InstrumentSoundType.LapHarp;
                case "lute": return InstrumentSoundType.Lute;
            }
        }
        public Server.Misc.InstrumentType GetInstrumentType()
        {
            if (m_instrument != null)
                switch (m_instrument.SuccessSound)
                {
                    default:
                    case (int)InstrumentSoundType.Harp:
                        return Server.Misc.InstrumentType.Harp;
                    case (int)InstrumentSoundType.LapHarp:
                        return Server.Misc.InstrumentType.LapHarp;
                    case (int)InstrumentSoundType.Lute:
                        return Server.Misc.InstrumentType.Lute;
                }
            else
            {
                AimsLog.WriteLine("GetInstrumentType() returning default Harp", ConsoleColor.Cyan);
                return Server.Misc.InstrumentType.Harp;
            }
        }
        public RazorInstrument(InstrumentSoundType wellSound)
            : this(0x1F65, (int)wellSound, (int)wellSound + 1, null)
        {
        }
        public RazorInstrument()
            : this(0x1F65, (int)InstrumentSoundType.Harp, (int)InstrumentSoundType.BadHarp, null)
        {
        }
        public RazorInstrument(int itemID, int wellSound, int badlySound)
            : this(itemID, wellSound, badlySound, null)
        {
        }
        public RazorInstrument(int itemID, int wellSound, int badlySound, Item? playItem)
            : base(itemID, wellSound, badlySound)
        {
            //Name = GetName();
            //  ride of the Valkyries will be our default
            SongBytes = "b 0.1 fs b dh 0.3 b 0.3 dh 0.1 b dh fsh 0.3 dh 0.3 fsh 0.1 dh fsh ah 0.3 a 0.3 dh 0.1 a dh fsh";
            SongName = "Ride of the Valkyries";
#if ANGEL_ISLAND
            LootType = LootType.UnStealable | LootType.UnLootable; // a style of blessed, includes reds
#else
            LootType = LootType.Blessed;
#endif
            m_playItem = playItem;
            m_instrument = this;
            m_current = new Performance(this, idle: true);
        }
        public RazorInstrument(Serial serial)
            : base(serial)
        {
            m_current = new Performance(this, idle: true);
        }
        public override void ConsumeUse(Mobile from)
        {
            // razor instruments last forever.
        }
        public override void OnSingleClick(Mobile from)
        {
            if (this.Name != null)
                LabelTo(from, this.Name);
            if (m_author != null)
                LabelTo(from, m_author);
            if (m_songName != null)
                LabelTo(from, m_songName);
        }
        public override void OnDoubleClick(Mobile from)
        {
            // NOTE: This item can be "played" by either the mobile interacting with it (from)
            // or by a resolved "play mobile" context (m_playMobile). Do not mutate the input
            // parameter; resolve the effective actor once and use it consistently.
            Mobile actor = m_playMobile ?? from;

            // Robustness: if we cannot resolve a valid actor, there is nothing meaningful to do.
            if (actor == null)
                return;

            try
            {
                DebugOut("OnDoubleClick in RZR: begin");

                // Robustness: Backpack can be null in some edge cases; treat as not eligible.
                // Also, IsChildOf(null) would throw, so guard it.
                if (m_requiresBackpack)
                {
                    if (actor.Backpack == null || !IsChildOf(actor.Backpack))
                    {
                        DebugOut("OnDoubleClick in RZR: needs backpack");
                        actor.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
                        return;
                    }
                }

                DebugOut("OnDoubleClick in RZR: ready to call IsThisDevicePlaying");

                // If this device is already playing for this actor, double-click acts as a toggle: cancel playback.
                if (IsThisDevicePlaying(actor))
                {
                    DebugOut("OnDoubleClick in RZR: IsThisDevicePlaying returned true");
                    CancelPlayback(actor);
                    return;
                }

                DebugOut("OnDoubleClick in RZR: ready to call ConflictedPlayContexts");

                // Conflicts mean the player has an active playback context elsewhere (another device/container/mobile).
                // Only start if no conflicts are detected.
                if (!ConflictedPlayContexts(actor))
                {
                    DebugOut("OnDoubleClick in RZR: ready to call StartMusic");

                    // Robustness: Ensure instrument is present before starting.
                    if (m_instrument == null)
                    {
                        // Keep this silent unless you prefer a player-facing message.
                        DebugOut("OnDoubleClick in RZR: instrument was null; StartMusic not called");
                        return;
                    }

                    StartMusic(actor, m_instrument);
                }
                else if (WarnPlaybackConflicts())
                {
                    DebugOut("OnDoubleClick in RZR: ConflictedPlayContexts returned true");

                    // Player-facing explanation when configured to warn.
                    actor.SendMessage("You have initiated music in another context. You will need to cancel that or wait for it to finish.");
                }
                else if (Core.Debug)
                {
                    // Debug-only explanation when we are intentionally not warning the player.
                    AimsHost.Current.SendSystemMessage(actor, 5, "Debug message: music not started because this player has initiated music elsewhere.");
                }

                DebugOut("OnDoubleClick in RZR: returning");
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        public void CancelPlayback(Mobile? from, bool say = true)
        {
            // Prefer the current "play mobile" (the singer / controller) if one is established.
            // Do NOT mutate the input parameter; use a local for clarity and to avoid surprises.
            Mobile? actor = m_playMobile ?? from;

            // Robustness: if we cannot resolve an actor, we cannot safely evaluate playback state
            // or emit feedback. Treat as a no-op. (A music device's own session has no actor.)
            if (actor == null && !m_current.Unowned)
                return;

            // Robustness: only cancel if this specific device is currently playing for this actor.
            if (!IsThisDevicePlaying(actor))
                return;

            Cancel(m_current, PerformanceEnd.Cancelled, actor, say);
        }
        // Ends a performance in progress: nothing more is played (its queues and its own session's playlist are dropped)
        //  and Completed(reason) follows after the drain of its last notes (WaitDone, half a second). Only the current
        //  performance can be cancelled; any other has already ended or been superseded.
        private void Cancel(Performance performance, PerformanceEnd reason, Mobile? actor, bool say)
        {
            if (!ReferenceEquals(performance, m_current) || !performance.InProgress)
                return;

            if (actor == null && !m_current.Unowned)
                return;

            try
            {
                // Optional player feedback.
                if (say)
                    actor?.Emote("*you cancel what you were playing*");

                // Reset playback state first so any downstream callbacks/handlers see "not playing".
                m_current.Paused = false;
                m_current.Playing = false;

                // Drop any pending and buffered music data.
                m_current.PlayQueue.Clear();
                m_current.SongBuffer.Clear();

                // Clear any per-mobile playlist state if present.
                // Use TryGetValue to avoid double dictionary lookups and racey ContainsKey/indexing.
                // Only our own session's playlist: a replacement context ([play reset) belongs to someone else.
                Server.Commands.MusicContext? cfg = null;
                if (m_current.Unowned)
                    cfg = SystemMusicPlayer.IsCurrent(m_current.Session, null) ? m_current.Session : null;
                else if (SystemMusicPlayer.MusicConfig != null)
                    SystemMusicPlayer.MusicConfig.TryGetValue(actor!, out cfg);
                if (cfg != null &&
                    (m_current.Session == null || ReferenceEquals(cfg, m_current.Session)) &&
                    cfg.PlayList != null)
                {
                    cfg.PlayList.Clear();
                }
                else if (m_current.Session != null)
                {   // our session was already replaced or released: this cancel is the performance's one end
                    //  (below), so the dead performance's remaining callbacks stop quietly (SessionLost)
                    m_current.SessionLost = true;
                    m_current.Session = null;
                }

                // Completed (and AllDone) once its last notes have drained; every other callback of it stops now.
                if (performance.BeginEnding(reason))
                    Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(WaitDone), new object?[] { actor, performance, null });
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        public bool IsThisDevicePlaying(Mobile? from)
        {
            try
            {
                from = m_playMobile ?? from;

                bool queueHasItems = m_current.PlayQueue.Count > 0;
                bool bufferHasItems = m_current.SongBuffer.Count > 0;

                if (queueHasItems || bufferHasItems)
                    return true;

                // a music device's own session
                if (m_current.Unowned)
                    return SystemMusicPlayer.IsCurrent(m_current.Session, null) && m_current.Session!.Playing && IsInstancePlaying;

                if (from == null)
                    return false;

                // Defensive: MusicConfig or entry may be null / missing during teardown.
                if (SystemMusicPlayer.MusicConfig == null)
                    return false;

                if (!SystemMusicPlayer.MusicConfig.TryGetValue(from, out var cfg) || cfg == null)
                    return false;

                return cfg.Playing && IsInstancePlaying;
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return false;
            }
        }
        // configures this performance's session: the mobile's context in MusicConfig, or the music device's own session
        private void ConfigureSession(Mobile? from, string instrumentType, Server.Commands.MusicContext.TickCallback? callback)
        {
            if (m_current.Unowned)
                m_current.Session = SystemMusicPlayer.ConfigureDeviceSession(m_current.Session, NewTimer, Tempo, Prefetch, instrumentType,
                    chatter: false, concertMode: ConcertMode, callback: callback, source: Source, timer: TimerMode);
            else
            {
                SystemMusicPlayer.ConfigurePlayer(from!, NewTimer, Tempo, Prefetch, instrumentType,
                    chatter: false, concertMode: ConcertMode, callback: callback, source: Source, timer: TimerMode);
                CaptureSession(from!);
            }
        }
        private void CaptureSession(Mobile from)
        {   // called right after ConfigurePlayer: the context it configured is this performance's session
            m_current.Session = null;
            m_current.SessionLost = false;
            if (from != null && SystemMusicPlayer.MusicConfig.TryGetValue(from, out var cfg))
                m_current.Session = cfg;
        }
        private bool SessionLost(Mobile? m)
        {   // true when this performance's context has been replaced ([play reset) or released since it started.
            //  The first detection ends the performance through the usual AllDone notification, exactly once; it never
            //  touches the replacement context. Every later callback of the dead performance simply stops.
            if (m_current.SessionLost)
                return true;

            if (m_current.Session == null || (m == null && !m_current.Unowned))
                return false;

            if (SystemMusicPlayer.IsCurrent(m_current.Session, m_current.Unowned ? null : m))
                return false;

            m_current.SessionLost = true;
            m_current.Session = null;
            DebugOut("RazorInstrument: music context replaced or released; ending this performance");
            m_current.Complete(PerformanceEnd.SessionLost);     // nothing left to drain: there is no session to play it
            return true;
        }
        private void PlayerProgressCallback(Mobile m, IEntity source, Performance performance)
        {
            using MusicEngineMetrics.Scope metrics = MusicEngineMetrics.Enter(MusicEngineMetrics.Kind.Script);
            MusicEngineMetrics.ScriptCallback();
            try
            {
                // a tick of an ended performance, or of an earlier one on this instrument (the newer one owns the state)
                if (!ReferenceEquals(performance, m_current) || !performance.InProgress)
                    return;

                m = m_playMobile ?? m;

                // If we have no mobile context (or it's gone), fail closed and cancel.
                if (PerformerGone(m))
                {
                    try { Cancel(performance, PerformanceEnd.SessionLost, m, false); }
                    catch (Exception ex) { AimsLog.LogException(ex); }
                    return;
                }

                // our music context was replaced or released: the performance is over
                if (SessionLost(m))
                    return;

                // Poll the user every two seconds to see if they should still be getting music.
                if (!m_current.UserCheck.Triggered)
                    return;

                // Restart poll window.
                m_current.UserCheck.Start(2000);

                // Notify the caller to see if the mobile is around to hear the music (the check this performance started with).
                CheckSourceStatusCallback? check = performance.CheckSourceStatus;
                if (check == null)
                    return;

                bool ok = true;

                try
                {
                    ok = check(m, source);
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                    ok = false; // fail closed
                }

                if (!ok)
                {   // mobile is no longer online or close enough to the play item: the performance ends, and the caller
                    //  is told once its last notes have drained (Completed: StatusFailed)
                    try
                    {
                        Cancel(performance, PerformanceEnd.StatusFailed, m, false);
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                    }

                    return;
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        #region Performance state
        // Timer state for the current performance's callbacks: { mobile (null on a music device's own session), Performance, extra }
        private object?[] State(Mobile? m, object? extra = null)
        {
            return new object?[] { m, m_current, extra };
        }
        // a callback of an earlier performance on this instrument, or of one that has ended (cancelled, failed, completed):
        //  it stops quietly and touches nothing (the newer performance owns the queues and the session; an ending one's
        //  completion is WaitDone's)
        private bool Superseded(object[] aState)
        {
            return aState.Length < 2 || aState[1] is not Performance performance || !ReferenceEquals(performance, m_current) || !performance.InProgress;
        }
        // the performing mobile is gone (a music device's own session has none, and that is normal)
        private bool PerformerGone(Mobile? m)
        {
            return m_current.Unowned ? false : (m == null || m.Deleted);
        }
        #endregion Performance state
        private IEntity? EstablishSource()
        {
            try
            {
                IEntity? source = null;

                // Prefer the PlayItem root parent (embedded cases), otherwise fall back to our own RootParent.
                object? rootParent = null;

                try
                {
                    if (PlayItem is Item ix && ix.RootParent != null)
                        rootParent = ix.RootParent;
                    else
                        rootParent = this.RootParent;
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                    rootParent = this.RootParent;
                }

                if (rootParent is Mobile mob)
                {
                    source = mob; // have the mobile 'sing'
                }
                else if (rootParent is Item item)
                {
                    source = item; // embedded in another item; have the container sing
                }
                else if (PlayItem != null)
                {
                    source = PlayItem;
                }
                else
                {
                    source = null;
                }

                return source;
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return null;
            }
        }
        private Server.Commands.MusicContext.TickCallback? EstablishCallback(Server.Commands.MusicContext.TickCallback? callback)
        {
            Performance performance = m_current;
            return (callback == null) ? (m, source) => PlayerProgressCallback(m, source, performance) : null;    // use our default handler
        }
        private bool ConflictedPlayContexts(Mobile from)
        {
            from = m_playMobile ?? from;
            if (SystemMusicPlayer.IsUserBlocked(from))
                return true;
            // a performance in progress on this context, even between its notes (a script 'wait' that outlasts its notes)
            if (PerformanceOf(from) != null)
                return true;
            return false;
        }
        /// <summary>
        /// Starts a performance on the mobile's music context (players, NPCs, MusicBox). Returns the new Performance, or
        /// null if none was started (performer gone, or the performer is playing music elsewhere).
        /// </summary>
        public Performance? StartMusic(Mobile from, BaseInstrument instrument, Server.Commands.MusicContext.TickCallback? callback = null)
        {
            return StartMusic(from, instrument, callback, unowned: false);
        }
        /// <summary>
        /// Starts a performance for a music device: it plays on its own session (a MusicContext with no mobile, never in
        /// MusicConfig), configured exactly as a fresh mobile's context would be. A PlayMobile set on this instrument
        /// still plays on that mobile's context, as before. Returns the new Performance (null if none was started).
        /// </summary>
        public Performance? StartDeviceMusic(BaseInstrument instrument)
        {
            return StartMusic(null, instrument, null, unowned: true);
        }
        private Performance? StartMusic(Mobile? from, BaseInstrument instrument, Server.Commands.MusicContext.TickCallback? callback, bool unowned)
        {
            try
            {
                from = m_playMobile ?? from;
                if (from != null)
                    unowned = false;

                if (!unowned && (from == null || from.Deleted))
                    return null;

                // a music device's own session is new: nothing else plays on it
                if (unowned || !ConflictedPlayContexts(from!))
                {
                    // a new performance (empty queues, not playing, no session yet), with the callbacks set on this
                    //  instrument now: callbacks of the previous one stop quietly from here on
                    Performance previous = m_current;
                    m_current = new Performance(this)
                    {
                        Unowned = unowned,
                        Performer = from,
                        AllDone = m_allDone,
                        CheckSourceStatus = m_checkSourceStatus,
                    };
                    Performance performance = m_current;
                    if (!unowned && from != null)
                        Register(from, performance);                // this mobile's context now plays this performance

                    // a performance still running (or draining) on this instrument is superseded: it ends now, once, as
                    //  Cancelled (or with its own reason if it was already ending). Deferred: its handlers run after this
                    //  start, never inside it.
                    if (previous.State != PerformanceState.Completed)
                        Timer.DelayCall(TimeSpan.Zero, () => previous.Complete(PerformanceEnd.Cancelled));

                    m_instrument = (instrument != null) ? instrument : this;

                    try
                    {
                        if (from != null)
                            SetInstrument(from, m_instrument);
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                    }

                    callback = EstablishCallback(callback);  // use our default handler if none is specified
                    m_source = EstablishSource();            // what entity is acting as the 'source' of the music? Item or Mobile?

                    m_current.UserCheck.Start(0);            // two second user check

                    // Resolve instrument type safely (do not assume instrument is a RazorInstrument).
                    string instrumentType = "Unknown";

                    try
                    {
                        RazorInstrument? ri = m_instrument as RazorInstrument;
                        if (ri == null)
                            ri = this;

                        instrumentType = ri.GetInstrumentType().ToString();
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                    }

                    // a '# Timer:' directive belongs to the song that carries it (read again when this song loads)
                    m_timerHeader = null;

                    try
                    {
                        // These will be defaults. We will call this again if we are reading a 'file' and find music directives.
                        ConfigureSession(from, instrumentType, callback);
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                        Abandon(performance);
                        return null;
                    }

                    if (PlaysDefaultSong && m_songBytes != null && m_songFile == null && m_songMemory == null)
                    {   // just play the canned song
                        try
                        {
                            string song = m_songBytes;

                            string[] notes = song.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string note in notes)
                                m_current.PlayQueue.Enqueue(note);

                            if (m_current.PlayQueue.Count > 0)
                                Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(NotePumpTick), State(from));

                            m_current.Playing = true;

                            // its end: Completed (Finished) once the notes have played
                            Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(WaitDone), State(from));
                        }
                        catch (Exception ex)
                        {
                            AimsLog.LogException(ex);
                        }
                    }
                    else
                    {   // read and process a Razor style 'song file'
                        bool loaded = false;

                        try
                        {
                            loaded = LoadSongFile(from) == true;
                        }
                        catch (Exception ex)
                        {
                            AimsLog.LogException(ex);
                            loaded = false;
                        }

                        if (loaded)
                        {
                            try
                            {   // the song's '# Instrument:' directive (applied while loading) sets the main instrument,
                                //  so read it again here rather than reusing the value captured before the song was loaded
                                RazorInstrument? ri = m_instrument as RazorInstrument;
                                if (ri == null)
                                    ri = this;

                                instrumentType = ri.GetInstrumentType().ToString();
                            }
                            catch (Exception ex)
                            {
                                AimsLog.LogException(ex);
                            }

                            try
                            {
                                // configure the player based on the directives found in the file
                                ConfigureSession(from, instrumentType, callback);
                            }
                            catch (Exception ex)
                            {
                                AimsLog.LogException(ex);
                                Abandon(performance);
                                return null;
                            }

                            try
                            {
                                // call our baby Razor to process the script
                                Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), State(from));
                                m_current.Playing = true;
                            }
                            catch (Exception ex)
                            {
                                AimsLog.LogException(ex);
                            }
                        }
                        else
                        {
                            try
                            {
                                from?.Emote("*plays nothing*"); // Player emotes to indicate they are playing
                            }
                            catch (Exception ex)
                            {
                                AimsLog.LogException(ex);
                            }

                            // nothing was started: log once per device, and release whoever is waiting on completion
                            try
                            {
                                if (s_reportedNothing.Add(Serial))
                                    AimsLog.WriteLine("RazorInstrument: {0} (0x{1:X8}) has no playable song", ConsoleColor.Yellow,
                                        GetType().Name, (int)Serial);
                            }
                            catch (Exception ex)
                            {
                                AimsLog.LogException(ex);
                            }

                            // deferred: callers finish their own bookkeeping (and subscribe) after StartMusic returns
                            Timer.DelayCall(TimeSpan.Zero, () => performance.Complete(PerformanceEnd.NothingToPlay));
                        }
                    }

                    return performance;
                }
                else if (WarnPlaybackConflicts())
                {   // (only a mobile's context can be in conflict)
                    from!.SendMessage("You have initiated music in another context. You will need to cancel that or wait for it to finish.");
                }
                else if (Core.Debug)
                {
                    AimsHost.Current.SendSystemMessage(from!, 5, "Debug message: music not started because this player has initiated music elsewhere.");
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }

            return null;
        }
        // a start that failed after its Performance was created: it ends quietly (nobody was told it started)
        private static void Abandon(Performance performance)
        {
            performance.AllDone = null;
            performance.CheckSourceStatus = null;
            performance.Complete(PerformanceEnd.NothingToPlay);
        }
        private bool WarnPlaybackConflicts()
        {
#if ANGEL_ISLAND
            if (PlayItem is BaseAddon || PlayItem is TrophyAddon || PlayItem is MusicMotionController /*|| PlayItem is MusicBox || PlayItem is RolledUpSheetMusic*/)
#else
            if (PlayItem is BaseAddon || PlayItem is TrophyAddon)
#endif
                return false;
            return true;
        }
        public void Pause(Mobile m)
        {
            m = m_playMobile ?? m;
            if (IsThisDevicePlaying(m))
                m_current.Paused = true;
        }
        public void Resume(Mobile m)
        {
            m = m_playMobile ?? m;
            if (IsThisDevicePlaying(m))
                m_current.Paused = false;

        }
        private void BabyRazor(object state)
        {
            using MusicEngineMetrics.Scope metrics = MusicEngineMetrics.Enter(MusicEngineMetrics.Kind.Script);
            MusicEngineMetrics.ScriptCallback();
            try
            {
                object[]? aState = state as object[];

                if (aState == null || aState.Length == 0)
                    return;

                // a callback of an earlier performance on this instrument: stop quietly
                if (Superseded(aState))
                    return;

                Mobile? m = aState[0] as Mobile;

                // If we lost our mobile context, fail closed and finish (Completed: SessionLost, after the drain).
                if (PerformerGone(m))
                {
                    Performance performance = m_current;
                    if (performance.BeginEnding(PerformanceEnd.SessionLost))
                        Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(WaitDone), State(m));

                    return;
                }

                // our music context was replaced or released: the performance is over
                if (SessionLost(m))
                    return;

                // pause handling
                if (m_current.Paused)
                {
                    int count = 0;

                    if (aState.Length > 2 && aState[2] is int)
                        count = (int)aState[2];

                    if (count >= 500)
                    {   // 250 seconds, then cancel their song (Completed: Cancelled, after the drain)
                        try
                        {
                            Cancel(m_current, PerformanceEnd.Cancelled, m_playMobile ?? m, say: !m_current.Unowned);
                        }
                        catch (Exception ex)
                        {
                            AimsLog.LogException(ex);
                        }
                    }
                    else
                    {
                        Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(BabyRazor), State(m, ++count));
                    }

                    return;
                }

                if (m_current.SongBuffer.Count > 0)
                {
                    string? line = null;

                    try
                    {
                        line = m_current.SongBuffer.Pop();
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                        line = null;
                    }

                    if (string.IsNullOrEmpty(line))
                    {
                        Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), State(m));
                        return;
                    }

                    line = line.Trim();

                    // there are only 4 type of valid input
                    string lower = line.ToLower();

                    if (lower.StartsWith("#"))
                        DoProcessComment(m, line);
                    else if (lower.StartsWith("say '[play"))
                        DoProcessPlay(m, line);
                    else if (lower.StartsWith("say"))
                        DoProcessSay(m, line);
                    else if (lower.StartsWith("wait"))
                        DoProcessWait(m, line);
                    else
                    {
                        // ignore the line; continue processing this script
                        Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), State(m));
                    }
                }
                else
                {
                    // the script is done: Completed (Finished) once its notes have played
                    // notes may still be playing on the client at this point, so we'll wait for them as well
                    Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(WaitDone), State(m));
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        private void WaitDone(object state)
        {
            using MusicEngineMetrics.Scope metrics = MusicEngineMetrics.Enter(MusicEngineMetrics.Kind.Script);
            MusicEngineMetrics.ScriptCallback();
            try
            {
                object[]? aState = state as object[];

                if (aState == null || aState.Length < 2 || aState[1] is not Performance performance)
                    return;

                // already over (several waits may be pending for one performance: it completes once)
                if (performance.State == PerformanceState.Completed)
                    return;

                // cancelled or failed: its last notes have drained, the performance is over
                if (performance.State == PerformanceState.Ending)
                {
                    performance.Complete(performance.EndReason);
                    return;
                }

                // an earlier performance on this instrument: the start that superseded it ends it
                if (!ReferenceEquals(performance, m_current))
                    return;

                Mobile? m = aState[0] as Mobile;

                // If we lost our mobile context, we cannot continue polling safely. (A music device's own session has none.)
                if (m == null && !m_current.Unowned)
                {
                    performance.Complete(PerformanceEnd.SessionLost);
                    return;
                }

                // our music context was replaced or released: the performance is over (SessionLost completes it once)
                if (SessionLost(m))
                    return;

                // wait for this instance to finish
                if (!IsThisDevicePlaying(m))
                {
                    performance.Complete(PerformanceEnd.Finished);
                }
                else
                {
                    Timer.DelayCall(
                        TimeSpan.FromSeconds(0.5),
                        new TimerStateCallback(WaitDone),
                        State(m));
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }

        private static Regex true_match = new Regex(@"true", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex int_match = new Regex(@"\d+", RegexOptions.Compiled);
        private static Regex wait_regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$", RegexOptions.Compiled);
        private void ParseDirectives(Mobile? m, string line)
        {
            try
            {
                m = m_playMobile ?? m;

                if (string.IsNullOrEmpty(line))
                    return;

                // Defensive: regexes may be null during teardown/partial init.
                if (true_match == null || int_match == null)
                    return;

                string lower = line.ToLower();

                // '# Timer: Classic|Dynamic|Anchored': matched exactly ('# NewTimer:' also contains "timer:")
                string directive = lower.TrimStart('#', ' ', '\t');
                if (directive.StartsWith("timer:"))
                {
                    string value = directive.Substring("timer:".Length).Trim();
                    if (!int.TryParse(value, out _) && Enum.TryParse(value, true, out TimerMode timer) && Enum.IsDefined(timer))
                        m_timerHeader = timer;
                }
                else if (lower.Contains("author:"))
                {   // razor instruments only care about the music, not author, how the player placed, etc.
                    try
                    {
                        if (!KeepsItsAuthor)    // sheet music keeps the author it was transcribed with
                        {
                            m_author = line.Replace("#", "");
                            m_author = m_author.Replace("author:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                        }
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                    }
                }
                else if (lower.Contains("instrument:"))
                {
                    try
                    {
                        string instrument = line.Replace("#", "");
                        instrument = instrument.Replace("instrument:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                        ConfigureInstrument(instrument);
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                    }
                }
                else if (lower.Contains("prefetch:"))
                {
                    try
                    {
                        string sx = line.Replace("#", "");
                        sx = sx.Replace("prefetch:", "", StringComparison.CurrentCultureIgnoreCase).Trim();

                        string resultString = true_match.Match(sx).Value;

                        if (!bool.TryParse(resultString, out bool prefetch))
                            Prefetch = false;
                        else
                            Prefetch = prefetch;
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                        Prefetch = false;
                    }
                }
                else if (lower.Contains("newtimer:"))
                {
                    try
                    {
                        string sx = line.Replace("#", "");
                        sx = sx.Replace("newtimer:", "", StringComparison.CurrentCultureIgnoreCase).Trim();

                        string resultString = true_match.Match(sx).Value;

                        if (!bool.TryParse(resultString, out bool newTimer))
                            NewTimer = false;
                        else
                            NewTimer = newTimer;
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                        NewTimer = false;
                    }
                }
                else if (lower.Contains("tempo:"))
                {
                    try
                    {
                        string sx = line.Replace("#", "");
                        sx = sx.Replace("tempo:", "", StringComparison.CurrentCultureIgnoreCase).Trim();

                        string resultString = int_match.Match(sx).Value;

                        if (!int.TryParse(resultString, out int tempo))
                            Tempo = Commands.SystemMusicPlayer.DefaultTempo;
                        else
                            Tempo = tempo;
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                        Tempo = Commands.SystemMusicPlayer.DefaultTempo;
                    }
                }
                else if (lower.Contains("concertmode:"))
                {
                    try
                    {
                        string sx = line.Replace("#", "");
                        sx = sx.Replace("concertmode:", "", StringComparison.CurrentCultureIgnoreCase).Trim();

                        string resultString = true_match.Match(sx).Value;

                        if (!bool.TryParse(resultString, out bool concertMode))
                            ConcertMode = false;
                        else
                            ConcertMode = concertMode;
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                        ConcertMode = false;
                    }
                }
                else if (lower.Contains("song:"))
                {
                    try
                    {
                        m_songName = line.Replace("#", "");
                        m_songName = m_songName.Replace("song:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        public void ConfigureInstrument(string instrument)
        {
            try
            {
                if (string.IsNullOrEmpty(instrument))
                    instrument = "harp";

                string i = instrument.Trim().ToLower();

                // Defensive: Instrument may be null during teardown / partial init.
                if (Instrument == null)
                    return;

                if (i == "harp")
                {
                    Instrument.SuccessSound = (int)RazorInstrument.InstrumentSoundType.Harp;
                    AimsLog.WriteLine("configure as instrument {0}", ConsoleColor.Yellow, RazorInstrument.InstrumentSoundType.Harp.ToString());
                }
                else if (i == "lapharp")
                {
                    Instrument.SuccessSound = (int)RazorInstrument.InstrumentSoundType.LapHarp;
                    AimsLog.WriteLine("configure as instrument {0}", ConsoleColor.Yellow, RazorInstrument.InstrumentSoundType.LapHarp.ToString());
                }
                else if (i == "lute")
                {
                    Instrument.SuccessSound = (int)RazorInstrument.InstrumentSoundType.Lute;
                    AimsLog.WriteLine("configure as instrument {0}", ConsoleColor.Yellow, RazorInstrument.InstrumentSoundType.Lute.ToString());
                }
                else
                {
                    Instrument.SuccessSound = (int)RazorInstrument.InstrumentSoundType.Harp;
                    AimsLog.WriteLine("instrument {0} unknown", ConsoleColor.Yellow, instrument);
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        private void DoProcessComment(Mobile? m, string line)
        {
            m = m_playMobile ?? m;
            // call our baby Razor to continue processing this script 
            Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), State(m));
        }
        private void DoProcessPlay(Mobile? m, string line)
        {
            try
            {
                m = m_playMobile ?? m;

                if (PerformerGone(m))
                    return;

                if (string.IsNullOrEmpty(line))
                {
                    Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), State(m));
                    return;
                }

                string song = line
                    .Replace("[play", "", StringComparison.CurrentCultureIgnoreCase)
                    .Replace("say", "", StringComparison.CurrentCultureIgnoreCase)
                    .Replace("'", "")
                    .Trim();

                if (song.Length == 0)
                {
                    Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), State(m));
                    return;
                }

                string[] notes = song.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string note in notes)
                {
                    if (!string.IsNullOrEmpty(note))
                        m_current.PlayQueue.Enqueue(note);
                }

                if (m_current.PlayQueue.Count > 0)
                    Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(NotePumpTick), State(m));

                // call our baby Razor to continue processing this script
                Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), State(m));
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        private void DoProcessSay(Mobile? m, string line)
        {
            try
            {
                m = m_playMobile ?? m;

                // We can still speak even if m is null, but recording requires a Mobile.
                if (string.IsNullOrEmpty(line))
                {
                    if (!PerformerGone(m))
                        Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), State(m));

                    return;
                }

                line = line.Replace("say ", "", StringComparison.CurrentCultureIgnoreCase);

                // Expecting quotes: 'xxxx'
                if (line.Length >= 2 && line[0] == '\'' && line[line.Length - 1] == '\'')
                    line = line.Substring(1, line.Length - 2);
                else if (line.Length == 0)
                    line = string.Empty;

                // Speak from the source (best effort).
                try
                {
                    if (Source is Item item)
                    {
                        item.PublicOverheadMessage(Network.MessageType.Regular, 0x32, true, line);
                    }
                    else if (Source is Mobile sm)
                    {
                        sm.Say(line);
                    }
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                }

                // This is the case when text is spoken via RSM or other RazorInstrument
                // (the other case is when text is spoken via Razor: MusicRecorder hears that speech itself).
                try
                {
                    if (m != null && !m.Deleted)
                        Engines.MusicRecorder.Record(m, line);
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                }

                // call our baby Razor to continue processing this script
                if (!PerformerGone(m))
                    Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), State(m));
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        private void DoProcessWait(Mobile? m, string line)
        {
            try
            {
                m = m_playMobile ?? m;

                if (PerformerGone(m))
                    return;

                if (string.IsNullOrEmpty(line) || wait_regex == null)
                {
                    Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), State(m));
                    return;
                }

                string wait = line.Replace("wait", "", StringComparison.CurrentCultureIgnoreCase).Trim();

                double delay = 0;

                string resultString = string.Empty;

                try
                {
                    resultString = wait_regex.Match(wait).Value;
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                    resultString = string.Empty;
                }

                if (!string.IsNullOrEmpty(resultString))
                    double.TryParse(resultString, out delay);

                if (delay < 0)
                    delay = 0;

                // Anchored: this wait ends a wait's length after the previous one was due to end (the script's lines in
                //  between take no time in the score), and ScriptWaitDone checks it for an interruption
                if (TimerMode == TimerMode.Anchored)
                {
                    long now = MusicEngineMetrics.Now;
                    Performance performance = m_current;
                    performance.ScriptDue = (performance.ScriptDue != 0 ? performance.ScriptDue : now) + SystemMusicPlayer.MsToTicks(delay);
                    double remaining = Math.Max(0, SystemMusicPlayer.TicksToMs(performance.ScriptDue - now));
                    Timer.DelayCall(TimeSpan.FromMilliseconds(remaining), new TimerStateCallback(ScriptWaitDone), State(m));
                    return;
                }

                // call our baby Razor to process the script after this WAIT
                Timer.DelayCall(TimeSpan.FromMilliseconds(delay), new TimerStateCallback(BabyRazor), State(m));
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        // Anchored: an Anchored song's wait has ended. More than ReanchorMs late is an interruption: the script's schedule
        //  shifts once, to now. Then the script continues (BabyRazor).
        private void ScriptWaitDone(object state)
        {
            if (state is object[] aState && aState.Length > 1 && aState[1] is Performance performance
                && ReferenceEquals(performance, m_current) && performance.ScriptDue != 0)
            {
                long now = MusicEngineMetrics.Now;
                if (now - performance.ScriptDue > SystemMusicPlayer.MsToTicks(SystemMusicPlayer.ReanchorMs))
                {
                    performance.ScriptDue = now;
                    MusicEngineMetrics.Reanchor(script: true);
                }
            }
            BabyRazor(state);
        }
        private void NotePumpTick(object state)
        {
            using MusicEngineMetrics.Scope metrics = MusicEngineMetrics.Enter(MusicEngineMetrics.Kind.Script);
            MusicEngineMetrics.ScriptCallback();
            try
            {
                double delay = 0.001;

                object[]? aState = state as object[];
                if (aState == null || aState.Length == 0)
                    return;

                // a callback of an earlier performance on this instrument: stop quietly
                if (Superseded(aState))
                    return;

                Mobile? pm = aState[0] as Mobile;
                if (PerformerGone(pm))
                    return;

                // Defensive: config may be null during teardown.
                if (SystemMusicPlayer.MusicConfig == null)
                    return;

                // our music context was replaced or released: publish nothing to the replacement
                if (SessionLost(pm))
                    return;

                Server.Commands.MusicContext? cfg = null;
                if (m_current.Unowned)
                    cfg = m_current.Session;                                // the music device's own session
                else if (!SystemMusicPlayer.MusicConfig.TryGetValue(pm!, out cfg))
                    return;
                if (cfg == null)
                    return;

                int notesQueued = (cfg.PlayList != null) ? cfg.PlayList.Count : 0; // how many notes are currently queued
                int notesToPublish = Math.Min(SystemMusicPlayer.MaxQueueSize - notesQueued, m_current.PlayQueue.Count); // how many we should publish

                if (notesToPublish == 0 && m_current.PlayQueue.Count > 0)
                {   // still stuff left to play, but no room at the inn
                    // stay-alive / wait
                    Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(NotePumpTick), State(pm));
                    return;
                }
                else if (notesToPublish == 0)
                {
                    return; // we're done
                }

                // We only construct this for CommandEventArgs()/Player()
                string playString = "";

                for (int ix = 0; ix < notesToPublish; ix++)
                {
                    string temp;

                    try
                    {
                        temp = m_current.PlayQueue.Dequeue();
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                        break;
                    }

                    if (string.IsNullOrEmpty(temp))
                        continue;

                    string[] chunks = temp.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                    if (chunks.Length > 1)
                    {   // the second chunk is the pause.
                        // Razor represents pauses in milliseconds,
                        // so we will /1000 for our DelayCall timer (seconds).
                        if (double.TryParse(chunks[1], out double pause))
                            delay = pause / 1000.0;

                        playString += chunks[0] + " ";
                        break; // break here to process the pause
                    }
                    else
                    {
                        playString += temp + " ";
                    }
                }

                if (playString.Length == 0)
                {
                    // Nothing to play; try again shortly if there is still data.
                    if (m_current.PlayQueue.Count > 0)
                        Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(NotePumpTick), State(pm));

                    return;
                }

                // call the music player (non-critical; never crash the shard)
                try
                {
                    if (m_current.Unowned)
                        SystemMusicPlayer.PlaySession(cfg, null, playString);
                    else
                        SystemMusicPlayer.Player(pm!, playString);
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                    return;
                }

                // this timer will incorporate the delay specified 'as a pause' in the song file, or 0.001 if none specified
                if (m_current.PlayQueue.Count > 0)
                    Timer.DelayCall(TimeSpan.FromSeconds(delay), new TimerStateCallback(NotePumpTick), State(pm));
                else
                    return; // done
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        private bool LoadSongFile(Mobile? from)
        {
            try
            {
                from = m_playMobile ?? from;

                List<string> songLines = new List<string>();

                // We can read a song from either memory or a file.
                if (!string.IsNullOrEmpty(m_songMemory))
                {
                    try
                    {
                        using (StringReader strReader = new StringReader(m_songMemory))
                        {
                            while (true)
                            {
                                string? line = strReader.ReadLine();
                                if (line == null)
                                    break;

                                songLines.Add(line);

                                // Directives
                                if (line.Length > 0 && line[0] == '#')
                                {
                                    try
                                    {
                                        if (!PerformerGone(from))
                                            ParseDirectives(from, line);
                                    }
                                    catch (Exception ex)
                                    {
                                        AimsLog.LogException(ex);
                                    }
                                }
                            }
                        }

                        songLines.Reverse();
                        foreach (string tmp in songLines)
                            m_current.SongBuffer.Push(tmp);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                        return false;
                    }
                }

                if (!string.IsNullOrEmpty(m_songFile))
                {
                    string filePath = Path.Combine(AimsUtil.DataDirectory, "song files", m_songFile);

                    if (!File.Exists(filePath))
                        return false;

                    try
                    {
                        foreach (string line in File.ReadLines(filePath))
                        {
                            if (line == null)
                                continue;

                            songLines.Add(line);

                            // Directives
                            if (line.Length > 0 && line[0] == '#')
                            {
                                try
                                {
                                    if (!PerformerGone(from))
                                        ParseDirectives(from, line);
                                }
                                catch (Exception ex)
                                {
                                    AimsLog.LogException(ex);
                                }
                            }
                        }

                        songLines.Reverse();
                        foreach (string tmp in songLines)
                            m_current.SongBuffer.Push(tmp);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);

                        if (from != null && !from.Deleted)
                            from.SendMessage("Cannot load {0}", filePath);

                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return false;
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(9); // version

            writer.Write(m_concertMode);
            writer.Write(m_prefetch);
            writer.WriteEncodedInt(m_tempo);
            writer.Write(m_newTimer);
            writer.Write(m_playItem);
            writer.Write(m_instrument);
            writer.Write(m_requiresBackpack);
            writer.Write(m_author);
            writer.Write(m_songName);
            writer.Write(m_songBytes);
            writer.Write(m_songFile);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 9:
                case 8:
                    {
                        m_concertMode = reader.ReadBool();
                        m_prefetch = reader.ReadBool();
                        m_tempo = reader.ReadEncodedInt();
                        m_newTimer = reader.ReadBool();
                        m_playItem = reader.ReadItem();
                        m_instrument = (BaseInstrument)reader.ReadItem();
                        m_requiresBackpack = reader.ReadBool();
                        m_author = reader.ReadString();
                        m_songName = reader.ReadString();
                        m_songBytes = reader.ReadString();
                        if (version < 9)
                        {   // the award place and year (versions 1-8: not saved since)
                            LegacyAwardPlace = reader.ReadInt();
                            LegacyAwardYear = reader.ReadInt();
                        }
                        m_songFile = reader.ReadString();
                        break;
                    }
                // versions 0-7 (before 10/30/26) also carry five settings nothing used: read and dropped
                case 7:
                    {
                        m_concertMode = reader.ReadBool();
                        goto case 6;
                    }
                case 6:
                    {
                        m_prefetch = reader.ReadBool();
                        goto case 5;
                    }
                case 5:
                    {
                        m_tempo = reader.ReadEncodedInt();
                        goto case 4;
                    }
                case 4:
                    {
                        m_newTimer = reader.ReadBool();
                        goto case 3;
                    }
                case 3:
                    {
                        reader.ReadString();                    // the preferred instrument (removed)
                        goto case 2;
                    }
                case 2:
                    {
                        m_playItem = reader.ReadItem();
                        m_instrument = (BaseInstrument)reader.ReadItem();
                        reader.ReadDouble();                    // the minimum music skill (removed)
                        m_requiresBackpack = reader.ReadBool();
                        reader.ReadBool();                      // music chatter (removed: chatter is the session's)
                        reader.ReadBool();                      // check musicianship (removed)
                        reader.ReadBool();                      // ignore directives (removed)
                        goto case 1;
                    }
                case 1:
                    {
                        m_author = reader.ReadString();
                        m_songName = reader.ReadString();
                        m_songBytes = reader.ReadString();
                        LegacyAwardPlace = reader.ReadInt();    // the award place and year (not saved since version 9)
                        LegacyAwardYear = reader.ReadInt();
                        m_songFile = reader.ReadString();
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
        }
        public void DebugOut(string text)
        {
            if (InstanceDebugging)
                AimsHost.Current.SendMessageNearby(this, text);
        }
    }
}