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

/* Scripts\Engines\Music\SystemMusicPlayer.cs
 * Changelog
 *  11/6/26, Adam (2026 modernization, 10/24/26 to 11/6/26)
 *      Sessions: ReleaseContext is the one way a context leaves MusicConfig, and the timers stop when their context is
 *      released or replaced (a [play reset left a second timer running); a music device plays on its own session
 *      (ConfigureDeviceSession), not a temporary mobile; a context holds an InstrumentType instead of three virtual
 *      instruments (33,006 orphans on Upheaval). The Anchored timer (TimerMode Classic, Dynamic, Anchored; [play timer):
 *      notes keep exact time instead of running about 10% slow. One performance per performer: a typed [play no longer
 *      joins a sheet's song, and [StopMusic ends a performance properly. [play preview hears a book before transcribing
 *      it. Percussion sounds free of music karma (PercussionSounds; MidiToAims's drums use them). The timers report to
 *      MusicEngineMetrics. MusicContexts.bin context versions 4 (no items) and 5 (the timer): an older build cannot
 *      read them.
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: Music Karma, the music filter and [play's access level. Angel Island only (#if ANGEL_ISLAND): the
 *      rule-set check before the commands register.
 *  12/28/23, Adam (MusicConfig[player].Tick)
 *      Add a callback to notify the caller we are still processing notes. 
 *      Since this is called on every tick, it is the callers responsibility to limit processing so as to not impact playback.
 *      Typically, the caller will only process the tick every two seconds or so to verify the player is still around to hear the music.
 *  3/12/22, Adam
 *      Improve console out based on whether the player is 'listening to' or 'composing' music.
 *  3/3/22, Adam
 *      1. Add new skill gain system
 *      2. Add test mode for new skill gain system
 *      3. add [play MusicKarma command so players can learn what their karma is
 *      4. better reporting of point gains
 *      5. disallow the use of sound effects only when composing, playback via RSM is allowed
 *          This is how players with no music karma can listen to songs with sound effects when they themselves have no music karma.
 *      6. replace GetHashcode() with GetStableHashCode() so that hashes are valid across server restarts.
 *  3/2/22, Adam
 *      Serialize SameLine memory table.
 *  2/28/22, Adam
 *      Implement a skill gain system for composers.
 *      Skill gain is based on line complexity.
 *      There is also a database of previous lines. Gains get harder the more times we see the same line.
 *  2/26/22, Adam
 *      Add a new AI special 'skill' for Composers
 *      Process point accumulation for composition.
 *  2/26/22, Adam
 *      Fix the regular expression for processing tempo changes .. it was grabbing volume and changing it!
 *  2/26/22, Adam
 *      massive changes
 *      1. Removed all barding requirements (skill, backpack, etc.)
 *      2. increased queue size from 32 notes to 1024 objects (queue now contains volume objects, instrument objects, etc.)
 *      3. added macros (and a list command)
 *      4. added 'music algebra' expressions that can be applied to macros. Expressions include:
 *          4a. Tempo changes
 *          4b. Transposition
 *          4c. reversing
 *      5. Added Volume and ConcertMode. Together, these provide separate 'sound packets' for each player hearing the tune.
 *      6. added an inspect command for staff to see the queue/depth
 *  2/19/22, Adam
 *      Cleanup string constants
 *  2/9/22, Adam
 *      Move virtual instruments to ItemToIntStorage for safe keeping.
 *  2/8/22, Adam
 *      Replace ~MusicContext() with an explicit MusicContext.Finish() method for reliable control over when virtual instruments get deleted.
 *  2/7/22
 *      Sound effects enabled for players with 1000 music karma.
 *      You get music karma by publishing songs, and selling songs.
 *      implement 'note offsets' to select a different instrument.
 *  2/6/11, Adam,
 *      serialize music contexts associated with a player
 *      Sound effects currently turned off pending further review
 *  1/4/22, Adam
 *  pretty much a rewrite of the legacy music system.
 *  Remove all 'pick instrument' stuff since the music system now keeps a table of virtual instruments for players.
 *  Add sounds: +NNN
 *  Add support for changing instruments intra-song.
 *  Remove the requirement for picking an instrument. Instead you 'configure' the player with commands such as:
 *  [play instrument lute
 *  [play prefetch true
 *  [play newtimer true
 *  [play tempo NNN
 *  1/2/21, Adam (Play.Player)
 *      Create the new, more flexable Player interface that accepts finer grained configuration of the music player
 *          New properties include minMusicSkill, instrument, requiresBackpack, musicChatter, and checkMusicianship
 *  12/10/21, Adam (PlayerMobile)
 *      Convert most all uses of PlayerMobile to Mobile as thos function have moved down to Mobile.
 *      This was done so that NPC can eventually 'play music'.
 *  12/7/21, Adam (MaxQueueSize)
 *      Make MaxQueueSize public so automated of song playing can be managed
 *	3/12/11, Adam
 *		Music is only available for AI and MO
 *	3/22/08, Adam
 *		Add checks to OnPickedInstrument
 *			Disallow percussion instruments
 *			Make sure it's in your backpack
 *			Convert all the specific Catch types to generic catches and use LogHelper to log them
 *  09/03/06, Rhiannon
 *		Fixed bug in StopMusic_OnCommand().
 *  08/20/06, Rhiannon
 *		Added emote when player starts playing
 *		Added detection of repeated notes
 *	07/30/06, Rhiannon
 *		Initial creation.
 */
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
namespace Server.Commands
{
    // how a session's notes are timed: Classic (a 100 ms tick per note), Dynamic (each note's own delay, scheduled from
    //  when the previous note ran), Anchored (Dynamic's delays, scheduled from when the previous note was due)
    public enum TimerMode
    {
        Classic,
        Dynamic,
        Anchored,
    }
    public record MusicContext
    {
        public bool Configured;                                 // flag to tell us if this object has yet been configured.
        public TimerMode TimerMode;                             // the timer model
        public bool NewTimer                                    // the legacy form of TimerMode: True is Dynamic, False is Classic
        {
            get { return TimerMode != TimerMode.Classic; }
            set { TimerMode = value ? TimerMode.Dynamic : TimerMode.Classic; }
        }
        public int Tempo;                                     // timer frequency
        public bool Prefetch;                                   // schedule the next timer tick immediately without finishing current timer cycle
        public bool MusicChatter;                               // turn on/off music chatter
        public int Volume = 20;                                 // values 0-20, 20 being the loudest
        public bool ConcertMode = false;                        // disables volume and enables relative distribution of sound packets
        public RazorInstrument.InstrumentSoundType Instrument;  // currently selected instrument - pointed to by Selector (pending instrument change)
        public RazorInstrument.InstrumentSoundType Selector;    // Selector pointing at current instrument
        public InstrumentType VirtualInstrument;                // actual instrument (its note sounds)
        private const string harp = "harp";
        private const string lapHarp = "lapharp";
        private const string lute = "lute";
        #region Source
        IEntity m_Source = null;
        public IEntity Source { get { return m_Source; } set { m_Source = value; } }
        #endregion Source   
        #region Callback
        public delegate void TickCallback(Mobile m, IEntity source);    // notify our caller of progress
        public TickCallback Tick = null;                                // the caller may want to check if, for instance, the mobile is still online
        #endregion Callback
        #region Composition points
        public int ObjectCount = 0;                                         // More objects, a better chance at a gain (Notes, Volume, etc.)
        public int ComplexityPoints = 0;                                    // earned for complexity: music algebra, volume, etc.
        public double LastGain = 0;
        public bool SkillTesting = false;                                   // true when we are running skill tests
        public Dictionary<int, int> SameLine = new Dictionary<int, int>();  // keep track of repeated lines and mak gains harder
        #endregion Composition points
        #region Session Data, Not Serialized
        public Queue<object> PlayList = new Queue<object>();
        public bool Playing = false;
        public bool Released = false;                           // a music device's own session has ended (see SystemMusicPlayer.IsCurrent)
        public long AnchorNext = 0;                             // Anchored: when the next note is due (Stopwatch ticks; 0: no schedule yet)
        #endregion Session Data, Not Serialized
        #region User Macros
        public SortedList<string, string> Macros = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase);
        #endregion User Macros
        public static RazorInstrument.InstrumentSoundType GetSelector(string? instrument)
        {
            if (string.IsNullOrEmpty(instrument))
                return RazorInstrument.InstrumentSoundType.None;

            switch (instrument.ToLower())
            {
                case harp: return RazorInstrument.InstrumentSoundType.Harp;
                case lapHarp: return RazorInstrument.InstrumentSoundType.LapHarp;
                case lute: return RazorInstrument.InstrumentSoundType.Lute;
                default: return RazorInstrument.InstrumentSoundType.None;
            }
        }
        public InstrumentType? GetOffsetInstrument(int offset)
        {   // instruments in order harp, lap harp, lute: an offset of +1/+2 or -1/-2 from the current one
            if (offset == 0 || offset > 2 || offset < -2)
                return null;

            int target = (int)VirtualInstrument + offset;
            if (target < (int)InstrumentType.Harp || target > (int)InstrumentType.Lute)
                // no instrument there
                return null;

            return (InstrumentType)target;
        }
        public void SelectInstrument()
        {
            switch (Selector)
            {
                default:
                case RazorInstrument.InstrumentSoundType.Harp: VirtualInstrument = InstrumentType.Harp; break;
                case RazorInstrument.InstrumentSoundType.LapHarp: VirtualInstrument = InstrumentType.LapHarp; break;
                case RazorInstrument.InstrumentSoundType.Lute: VirtualInstrument = InstrumentType.Lute; break;
            }
        }
        public MusicContext(bool ignored)
            : this() // construct the default object
        {
            Configured = false;
        }
        public MusicContext(bool newTimer = false, int tempo = SystemMusicPlayer.DefaultTempo, bool prefetch = false, string? instrument = null, bool chatter = true, bool concertMode = false,
            TimerMode? timer = null)
        {
            Configured = true;
            TimerMode = timer ?? (newTimer ? TimerMode.Dynamic : TimerMode.Classic);
            Tempo = tempo;
            Prefetch = prefetch;
            MusicChatter = chatter;
            ConcertMode = concertMode;
            // current Instrument and Selector are different so that we can detect instrument changes
            if (string.IsNullOrEmpty(instrument))
            {   // default instrument
                Instrument = GetSelector(harp);               // currently selected instrument - pointed to by Selector
                Selector = GetSelector(harp);                 // Selector pointing at current instrument
            }
            else
            {   // specific instrument
                Instrument = GetSelector(instrument);           // currently selected instrument - pointed to by Selector
                Selector = GetSelector(instrument);             // Selector pointing at current instrument
            }
        }
        public bool IsValid()
        {   // a context holds no items since version 4: nothing can go missing
            return true;
        }
        public void Finish()
        {   // nothing to release since version 4 (contexts no longer own virtual instruments)
        }
        #region Legacy virtual instruments (context versions 0-3)
        // the virtual instruments older contexts referenced (harp, lap harp, lute, and the selected one): deleted once loaded
        private static readonly List<Item> s_legacyInstruments = new List<Item>();
        private static void KeepLegacyInstrument(Item? item)
        {
            if (item != null && !s_legacyInstruments.Contains(item))
                s_legacyInstruments.Add(item);
        }
        public static void ReleaseLegacyInstruments()
        {   // deferred: the world is still loading
            if (s_legacyInstruments.Count == 0)
                return;
            List<Item> items = new List<Item>(s_legacyInstruments);
            s_legacyInstruments.Clear();
            Timer.DelayCall(TimeSpan.Zero, () =>
            {
                int deleted = 0;
                foreach (Item item in items)
                    // only the context's own virtual instruments: a RazorInstrument parked in internal storage
                    if (item is RazorInstrument && !item.Deleted && item.Map == Map.Internal && item.Parent == null)
                    {
                        item.Delete();
                        deleted++;
                    }
                AimsLog.WriteLine("Music contexts: {0} legacy virtual instruments deleted ({1} referenced)", ConsoleColor.Yellow, deleted, items.Count);
            });
        }
        #endregion Legacy virtual instruments (context versions 0-3)
        public MusicContext(GenericReader reader)
        {
            int version = reader.ReadEncodedInt();
            TimerMode? timer = null;
            switch (version)
            {
                case 5:
                    {   // the timer mode (applied after NewTimer, its legacy form, is read below)
                        timer = (TimerMode)reader.ReadEncodedInt();
                        goto case 4;
                    }
                case 4:
                    {   // no virtual instruments
                        goto case 3;
                    }
                case 3:
                    {
                        int count = reader.ReadEncodedInt();
                        for (int ix = 0; ix < count; ix++)
                        {
                            int hash = reader.ReadEncodedInt();
                            int seen = reader.ReadEncodedInt();
                            SameLine.Add(hash, seen);
                        }
                        goto case 2;
                    }
                case 2:
                    {
                        Volume = reader.ReadEncodedInt();
                        ConcertMode = reader.ReadBool();
                        int count = reader.ReadEncodedInt();
                        for (int ix = 0; ix < count; ix++)
                        {
                            string? key = reader.ReadString();
                            string? value = reader.ReadString();
                            Macros.Add(key, value);
                        }
                        goto case 1;
                    }
                case 1:
                    {
                        MusicChatter = reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {
                        Configured = reader.ReadBool();
                        NewTimer = reader.ReadBool();
                        Tempo = reader.ReadEncodedInt();
                        Prefetch = reader.ReadBool();
                        Instrument = (RazorInstrument.InstrumentSoundType)reader.ReadEncodedInt();
                        Selector = (RazorInstrument.InstrumentSoundType)reader.ReadEncodedInt();
                        if (version < 4)
                        {   // VirtualInstrument, harp, lap harp, lute: the instrument type is all we need (SelectInstrument)
                            for (int ix = 0; ix < 4; ix++)
                                KeepLegacyInstrument(reader.ReadItem());
                        }
                        SelectInstrument();
                        break;
                    }
            }
            if (timer is TimerMode mode && Enum.IsDefined(mode))
                TimerMode = mode;
        }
        public void Serialize(GenericWriter writer)
        {
            int version = 5;
            writer.WriteEncodedInt(version);
            switch (version)
            {
                case 5:
                    {
                        writer.WriteEncodedInt((int)TimerMode);
                        goto case 4;
                    }
                case 4:
                    {   // no virtual instruments
                        goto case 3;
                    }
                case 3:
                    {
                        writer.WriteEncodedInt(SameLine.Count);
                        foreach (KeyValuePair<int, int> kvp in SameLine)
                        {
                            writer.WriteEncodedInt(kvp.Key);
                            writer.WriteEncodedInt(kvp.Value);
                        }
                        goto case 2;
                    }
                case 2:
                    {
                        writer.WriteEncodedInt(Volume);
                        writer.Write(ConcertMode);
                        writer.WriteEncodedInt(Macros.Count);
                        foreach (KeyValuePair<string, string> kvp in Macros)
                        {
                            writer.Write(kvp.Key);
                            writer.Write(kvp.Value);
                        }
                        goto case 1;
                    }
                case 1:
                    {
                        writer.Write(MusicChatter);
                        goto case 0;
                    }
                case 0:
                    {
                        writer.Write(Configured);
                        writer.Write(NewTimer);
                        writer.WriteEncodedInt(Tempo);
                        writer.Write(Prefetch);
                        writer.WriteEncodedInt((int)Instrument);
                        writer.WriteEncodedInt((int)Selector);
                        break;
                    }
            }
        }
    }
    public class SystemMusicPlayer
    {
        public static Dictionary<Mobile, MusicContext> MusicConfig = new Dictionary<Mobile, MusicContext>();
        public const int DefaultTempo = 100;
        // Anchored: a note (or a script's wait) this late has met a real interruption: the schedule shifts once, from now.
        //  Smaller lateness is absorbed: the next time is computed from the scheduled one, not the actual one.
        public const double ReanchorMs = 100;
        public static long MsToTicks(double ms) => (long)(ms * System.Diagnostics.Stopwatch.Frequency / 1000.0);
        public static double TicksToMs(long ticks) => ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        // Anchored: where a run that starts now begins its schedule. A run that starts within ReanchorMs of where the
        //  session's schedule ran out continues it (the next block of a script); otherwise a new schedule starts from now
        //  after the tempo, as Dynamic starts every run. Returns the delay to the first note.
        private static TimeSpan AnchorStart(MusicContext ctx, int tempo)
        {
            long now = MusicEngineMetrics.Now;
            long due = ctx.AnchorNext != 0 && now - ctx.AnchorNext <= MsToTicks(ReanchorMs)
                ? Math.Max(ctx.AnchorNext, now)
                : now + MsToTicks(tempo);
            ctx.AnchorNext = due;
            return TimeSpan.FromMilliseconds(TicksToMs(due - now));
        }
        // Anchored: this note's scheduled time. More than ReanchorMs late is a real interruption: the schedule shifts
        //  once, to now (it is never rushed to catch up). Smaller lateness is absorbed by the next note's wait.
        private static long AnchorDue(MusicContext ctx)
        {
            long now = MusicEngineMetrics.Now;
            long due = ctx.AnchorNext != 0 ? ctx.AnchorNext : now;
            if (now - due > MsToTicks(ReanchorMs))
            {
                due = now;
                MusicEngineMetrics.Reanchor(script: false);
            }
            ctx.AnchorNext = due;
            return due;
        }
        private static RazorInstrument.InstrumentSoundType SelectInstrument(string instrument)
        {
            if (instrument.ToLower() == "harp")
                return RazorInstrument.InstrumentSoundType.Harp;
            else if (instrument.ToLower() == "lapharp")
                return RazorInstrument.InstrumentSoundType.LapHarp;
            else if (instrument.ToLower() == "lute")
                return RazorInstrument.InstrumentSoundType.Lute;
            else
                return RazorInstrument.InstrumentSoundType.None;
        }
        public static bool ForceStop(Mobile m)
        {
            if (MusicConfig.ContainsKey(m))
            {   // this forces an immediate shutdown of the context and blows away the various queues
                //  which may still be loaded with notes. 
                //MusicConfig.Remove(m);
                //MusicConfig.Add(m, new MusicContext(true));
                MusicConfig[m].Source = null;
                MusicConfig[m].Tick = null;
                MusicConfig[m].PlayList.Clear();
                MusicConfig[m].Playing = false;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Removes a mobile's music context and releases its virtual instruments (MusicContext.Finish). Every removal
        /// from MusicConfig goes through here, so a context's instruments are released exactly once.
        /// Returns false if the mobile had no context.
        /// </summary>
        public static bool ReleaseContext(Mobile m)
        {
            if (m == null || !MusicConfig.TryGetValue(m, out MusicContext? context))
                return false;

            MusicConfig.Remove(m);
            context?.Finish();
            return true;
        }
        private static void InitializeContext(Mobile m)
        {
            // if the user has no music object
            if (!MusicConfig.ContainsKey(m))
                // create a default one
                MusicConfig.Add(m, new MusicContext(true));
            else if (MusicConfig[m].IsValid() == false)
            {   // This should never execute
                //  it's a patch for a bug in an early version of [play that didn't move
                //  instruments to IntMapstorage and instruments were getting deleted
                AimsLog.WriteLine("Resetting player context object.", ConsoleColor.Red);
                ReleaseContext(m);
                MusicConfig.Add(m, new MusicContext(true));
            }
        }
        /// <summary>
        /// Is this session still the one its performance should play on? A mobile's session: while it is that mobile's
        /// context in MusicConfig (not released or replaced). A music device's own session (no mobile, never in
        /// MusicConfig): until it is released.
        /// </summary>
        public static bool IsCurrent(MusicContext? session, Mobile? owner)
        {
            if (session == null)
                return false;
            if (owner != null)
                return MusicConfig.TryGetValue(owner, out MusicContext? current) && ReferenceEquals(current, session);
            return !session.Released;
        }
        public static bool IsUserBlocked(Mobile from)
        {
            return from != null && SystemMusicPlayer.MusicConfig.ContainsKey(from) && SystemMusicPlayer.MusicConfig[from].Playing;
        }
        public static void ConfigurePlayer(
            Mobile m,
            bool newTimer = false,
            int tempo = DefaultTempo,
            bool prefetch = false,
            string? instrument = null,
            bool chatter = true,
            bool concertMode = false,
            MusicContext.TickCallback callback = null,
            IEntity source = null,
            TimerMode? timer = null)
        {   // called via API (RSM uses this); timer, when given, takes precedence over newTimer (its legacy form)
            if (MusicConfig.ContainsKey(m))
                ConfigureSession(MusicConfig[m], newTimer, tempo, prefetch, instrument, chatter, concertMode, callback, source, timer);
            else
                MusicConfig.Add(m, new MusicContext(newTimer, tempo, prefetch, instrument, chatter, concertMode, timer));

            // record this session data change (if the recorder is turned on)
            Engines.MusicRecorder.Record(m);
        }
        /// <summary>
        /// Configures a music device's own session (no mobile, never in MusicConfig) exactly as ConfigurePlayer configures
        /// a mobile's context: the first call creates the session (session == null), later calls update it.
        /// </summary>
        public static MusicContext ConfigureDeviceSession(
            MusicContext? session,
            bool newTimer = false,
            int tempo = DefaultTempo,
            bool prefetch = false,
            string? instrument = null,
            bool chatter = true,
            bool concertMode = false,
            MusicContext.TickCallback callback = null,
            IEntity source = null,
            TimerMode? timer = null)
        {
            if (session == null)
                return new MusicContext(newTimer, tempo, prefetch, instrument, chatter, concertMode, timer);

            ConfigureSession(session, newTimer, tempo, prefetch, instrument, chatter, concertMode, callback, source, timer);
            return session;
        }
        private static void ConfigureSession(MusicContext ctx, bool newTimer, int tempo, bool prefetch, string? instrument, bool chatter, bool concertMode,
            MusicContext.TickCallback callback, IEntity source, TimerMode? timer)
        {
            ctx.TimerMode = timer ?? (newTimer ? TimerMode.Dynamic : TimerMode.Classic);   // the timer model
            ctx.Tempo = tempo;                                       // tempo
            ctx.Prefetch = prefetch;                                 // prefetch
            ctx.Instrument = MusicContext.GetSelector(instrument);   // what instrument we would like to play
            ctx.Selector = MusicContext.GetSelector(instrument);     // instrument to select
            ctx.SelectInstrument();                                  // select that instrument
            ctx.MusicChatter = chatter;
            ctx.ConcertMode = concertMode;
            ctx.Tick = callback;                                     // generic callback to notify the caller of progress
            ctx.Source = source;                                     // from what object does the music emanate? Mobile or Item?
        }
        private static List<string> DefineMacro(string[] arguments)
        {
            List<string> macro = new List<string>();
            string text = string.Empty;
            // glue
            for (int ix = 1; ix < arguments.Length; ix++)
                text += arguments[ix].ToString() + " ";

            if (!text.Contains("="))
                return macro;

            string[] words = text.Split('=', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2 || string.IsNullOrEmpty(words[1].Trim()))
            {   // xxx= is how you delete a macro
                macro.Add(words[0].Trim());
                return macro;
            }

            // okay, we have a good macro
            if (words.Length == 2)
            {   // foo=a b c d
                macro.Add(words[0].Trim());
                macro.Add(words[1].Trim());
            }

            return macro;
        }
        // ctx: the session being configured (m's context, or a music device's own session, which has no mobile: m == null)
        private static bool DoConfigure(MusicContext ctx, Mobile? m, string[] arguments, bool bSilent)
        {   // called by the user via the [play command

            // Error: ill-formed [play command
            if (arguments == null || arguments.Length == 0)
            {
                // don't allow the player to attempy yo play this
                if (!bSilent)
                    m?.SendMessage("Error: ill-formed [play command.");
                return true;
            }

            string command = arguments[0].ToLower();

            if (
                // music configuration directives
                command == "newtimer" ||
                command == "timer" ||
                command == "tempo" ||
                command == "prefetch" ||
                command == "instrument" ||
                command == "chatter" ||
                command == "volume" ||          // only useful while in concert mode. Not currently processed in (RZR) directives. Should add
                command == "concertmode" ||
                // commands
                command == "reset" ||
                command == "config" ||
                command == "inspect" ||
                command == "macro" ||
                command == "list" ||
                command == "musickarma" ||
                command == "test")
            {
                if (command == "reset")
                {
                    if (m == null)
                        // a device session cannot be replaced: it is over (its performance ends, as a replaced context's does)
                        ctx.Released = true;
                    else if (MusicConfig.ContainsKey(m))
                    {
                        // save macros
                        SortedList<string, string> temp = ctx.Macros;

                        // free old (and its instruments), and create new context
                        ReleaseContext(m);
                        ///  create a default context
                        MusicConfig.Add(m, new MusicContext());

                        // reassign saved elements. users don't lose macros etc. on a "reset"
                        MusicConfig[m].Macros = temp;
                    }
                    if (!bSilent)
                        m?.SendMessage("Music object reset.");
                }
                else if (command == "test")
                {
                    if ((m?.AccessLevel ?? AccessLevel.Player) > AccessLevel.GameMaster)
                    {
                        AimsHost.Current.SetMusicKarma(m!, 0);
                        ctx.SameLine.Clear();
                        ctx.SkillTesting = true;
                        if (!bSilent)
                            m?.SendMessage("Guild points are now {0}.", AimsHost.Current.GetMusicKarma(m!));
                        Console.Clear();
                        AimsLog.WriteLine(string.Format("Start test at {0}", DateTime.UtcNow), ConsoleColor.Red);
                    }
                    else
                        return true;
                }
                else if (command == "musickarma")
                {
                    if (!bSilent)
                        m?.SendMessage("Your music karma is {0}.", MusicLibrary.GetMusicKarma(m));
                }
                else if (command == "volume")
                {
                    int volume;
                    if (arguments.Length == 2 && int.TryParse(arguments[1], out volume) == true)
                    {
                        // we call map volume twice to return the User's setting and not the internal representation
                        // (map volume handles clipping values)
                        ctx.Volume = CoreMusicPlayer.MapVolume(CoreMusicPlayer.MapVolume(volume));
                        ctx.Configured = true;
                    }
                    else
                    {
                        if (!bSilent)
                            m?.SendMessage("Error in Volume format.");
                        return true;
                    }
                    if (!bSilent)
                        m?.SendMessage("Volume set to {0}.", ctx.Volume);
                }
                else if (command == "concertmode")
                {
                    bool concertmode;
                    if (arguments.Length == 2 && bool.TryParse(arguments[1], out concertmode) == true)
                    {
                        ctx.ConcertMode = concertmode;
                        ctx.Configured = true;
                    }
                    else
                    {
                        if (!bSilent)
                            m?.SendMessage("Error in ConcertMode format.");
                        return true;
                    }
                    if (!bSilent)
                        m?.SendMessage("ConcertMode set to {0}.", ctx.ConcertMode);
                }
                else if (command == "list")
                {
                    if (ctx.Macros.Count > 0)
                    {
                        if (!bSilent)
                            foreach (KeyValuePair<string, string> kvp in ctx.Macros)
                                m?.SendMessage("{0}={1}", kvp.Key, kvp.Value);
                    }
                    else
                    {
                        if (!bSilent)
                            m?.SendMessage("No macros to list.");
                    }
                }
                else if (command == "macro")
                {
                    {
                        List<string> macro = DefineMacro(arguments);

                        if (ctx.Macros.Count > 128 && macro.Count == 2)
                        {
                            if (!bSilent)
                                m?.SendMessage("Too many macros.");
                        }
                        else if (macro.Count == 1)
                        {   // delete macro
                            if (ctx.Macros.ContainsKey(macro[0]))
                            {
                                ctx.Macros.Remove(macro[0]);
                                if (!bSilent)
                                    m?.SendMessage("Macro {0} deleted.", macro[0]);
                            }
                            else
                            {
                                if (!bSilent)
                                    m?.SendMessage("Macro not found.");
                            }
                        }
                        else if (macro.Count == 2)
                        {   // defining a new macro
                            bool overwrite = false;
                            if (overwrite = ctx.Macros.ContainsKey(macro[0]))
                                ctx.Macros.Remove(macro[0]);

                            ctx.Macros.Add(macro[0], macro[1]);
                            if (!bSilent)
                                m?.SendMessage("{0} {2}defined as '{1}'.", macro[0], macro[1], overwrite ? "re" : "");
                        }
                        else
                        {
                            if (!bSilent)
                                m?.SendMessage("Error: ill-formed macro.");
                        }
                    }
                }
                else if (command == "inspect" && (m?.AccessLevel ?? AccessLevel.Player) >= AccessLevel.GameMaster)
                {
                    if (ctx.PlayList.Count > 0)
                    {
                        string text = string.Empty;
                        foreach (object o in ctx.PlayList)
                        {
                            if (o is RazorInstrument.InstrumentSoundType)
                                text += ((RazorInstrument.InstrumentSoundType)o).ToString() + " ";
                            else if (o is double)
                                text += ((double)o).ToString() + " ";
                            else if (o is string)
                                text += (o as string) + " ";
                        }

                        Console.WriteLine(text);
                    }
                    else
                    {
                        if (!bSilent)
                            m?.SendMessage("Nothing to inspect.");
                    }
                }
                else if (command == "config")
                {
                    if (ctx.Configured == false)
                    {
                        if (!bSilent)
                            m?.SendMessage("Default values:");
                    }

                    // if a dependent mode is off, then the this mode will show in a special hue
                    int disabled = 0x3B2;
                    int enabled = 0x40;
                    int vmode = ctx.ConcertMode == false ? disabled : enabled;
                    int nmode = ctx.NewTimer == false ? disabled : enabled;

                    if (!bSilent)
                    {
                        m?.SendMessage(enabled, "Instrument set to {0}.", ctx.Instrument.ToString());
                        m?.SendMessage(enabled, "Timer set to {0}.", ctx.TimerMode);  // the authoritative timer (NewTimer is its legacy form)
                        m?.SendMessage(nmode, "Prefetch set to {0}.", ctx.Prefetch);
                        m?.SendMessage(nmode, "Tempo set to {0} milliseconds.", ctx.Tempo);
                        m?.SendMessage(vmode, "Volume set to {0}.", ctx.Volume);
                        m?.SendMessage(enabled, "ConcertMode set to {0}.", ctx.ConcertMode);
                        m?.SendMessage(enabled, "Chatter set to {0}.", ctx.MusicChatter);
                    }
                }
                else if (command == "instrument")
                {
                    if (arguments.Length == 2 && SelectInstrument(arguments[1]) != RazorInstrument.InstrumentSoundType.None)
                    {
                        // check for too many notes.
                        if (ctx.PlayList.Count > MaxQueueSize)
                        {
                            if (!bSilent)
                                m?.SendMessage("Warning: Max queued notes reached.");
                            return true;
                        }

                        // we will change to this instrument...
                        ctx.Instrument = MusicContext.GetSelector(arguments[1]);

                        // ...at this place in the queue
                        ctx.PlayList.Enqueue(ctx.Instrument);

                        ctx.Configured = true;
                        //Utility.ConsoleOut("request to change instrument to {0}", ConsoleColor.Yellow, MusicContext.GetSelector(arguments[1]));
                    }
                    else
                    {
                        if (!bSilent)
                            m?.SendMessage("Error in Instrument format.");
                        return true;
                    }

                    if (!bSilent)
                        m?.SendMessage("Instrument set to {0}.", ctx.Instrument.ToString());
                }
                else if (command == "prefetch")
                {
                    bool prefetch;
                    if (arguments.Length == 2 && bool.TryParse(arguments[1], out prefetch) == true)
                    {
                        ctx.Prefetch = prefetch;
                        ctx.Configured = true;
                    }
                    else
                    {
                        if (!bSilent)
                            m?.SendMessage("Error in Prefetch format.");
                        return true;
                    }

                    if (!bSilent)
                        m?.SendMessage("Prefetch set to {0}.", ctx.Prefetch);
                }
                else if (command == "newtimer")
                {
                    bool newTimer;
                    if (arguments.Length == 2 && bool.TryParse(arguments[1], out newTimer) == true)
                    {
                        ctx.NewTimer = newTimer;
                        ctx.Configured = true;
                    }
                    else
                    {
                        if (!bSilent)
                            m?.SendMessage("Error in NewTimer format.");
                        return true;
                    }

                    if (!bSilent)
                        m?.SendMessage("NewTimer set to {0}.", ctx.NewTimer);
                }
                else if (command == "timer")
                {
                    if (arguments.Length == 2 && !int.TryParse(arguments[1], out _) && Enum.TryParse(arguments[1], true, out TimerMode timer) && Enum.IsDefined(timer))
                    {
                        ctx.TimerMode = timer;
                        ctx.Configured = true;
                    }
                    else
                    {
                        if (!bSilent)
                            m?.SendMessage("Usage: [play timer classic|dynamic|anchored");
                        return true;
                    }

                    if (!bSilent)
                        m?.SendMessage("Timer set to {0}.", ctx.TimerMode);
                }
                else if (command == "tempo")
                {
                    int tempo = DefaultTempo;
                    if (arguments.Length == 2 && int.TryParse(arguments[1], out tempo) == true)
                    {
                        ctx.Tempo = tempo;
                        ctx.Configured = true;
                    }
                    else
                    {
                        if (!bSilent)
                            m?.SendMessage("Error in Tempo format.");
                        return true;
                    }
                    if (!bSilent)
                        m?.SendMessage("Tempo set to {0} milliseconds.", ctx.Tempo);
                }
                else if (command == "chatter")
                {
                    bool chatter;
                    if (arguments.Length == 2 && bool.TryParse(arguments[1], out chatter) == true)
                    {
                        ctx.MusicChatter = chatter;
                        ctx.Configured = true;
                    }
                    else
                    {
                        if (!bSilent)
                            m?.SendMessage("Error in Chatter format.");
                        return true;
                    }
                    if (!bSilent)
                        m?.SendMessage("Chatter set to {0}.", ctx.MusicChatter);
                }
                else
                {
                    if (!bSilent)
                        m?.SendMessage("Unknown instrument setting.");
                }

                // record this session data change (if the recorder is turned on)
                if (m != null)
                    Engines.MusicRecorder.Record(m);

                return true;
            }
            else
                return false;
        }
        public static void Initialize()
        {   // too good to limit to any specific shard.
#if ANGEL_ISLAND
            if (Core.RuleSets.AllServerRules())
#endif
            {
                CommandSystem.Register("Play", AimsHost.Current.PlayAccessLevel, new CommandEventHandler(Play_OnCommand));
                CommandSystem.Register("StopMusic", AccessLevel.Player, new CommandEventHandler(StopMusic_OnCommand));
                CommandSystem.Register("FilterMusic", AccessLevel.Player, new CommandEventHandler(FilterMusic_OnCommand));
            }
        }
        private const int MaxLines = 128;
        public static int MaxQueueSize { get { return 1024; } }
        public static void Pack(WorldSaveEventArgs e)
        {
            Console.WriteLine("Music System Packing...");
            try
            {
                foreach (var mc in MusicConfig)
                    while (mc.Value.SameLine.Count > MaxLines + MaxLines / 2)
                        if (mc.Value.SameLine.ContainsKey(mc.Value.SameLine.Keys.First()))
                            mc.Value.SameLine.Remove(mc.Value.SameLine.Keys.First());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while packing music data.");
                Console.WriteLine(ex.ToString());
            }
        }
        //public static Mobile NewTempMobile(string name = null)
        //{   // 10 minutes should outlast the longest song
        //    return new Mobile(name: name ?? "music context object", 10.0);
        //}
        public static bool IsSound(string sound)
        {
            if (string.IsNullOrEmpty(sound) || sound.Length < 2 || sound[0] != '+')
                return false;

            for (int i = 1; i < sound.Length; i++)
            {
                if (char.IsDigit(sound[i]))
                    continue;
                else return false;
            }

            return true;
        }
        // percussion: sound effects any composer may use, whatever their music karma (chosen by ear for MidiToAims drums).
        //  MidiToAims.Core keeps a copy (Percussion.Sounds): change them together ([m2a refuses when they differ).
        //  Chosen by ear (Adam, 11/2/26): clicks, knocks, thuds, swishes, the anvil and wind chimes; with (11/3/26) 305
        //  feet15a and 943 hit21, two of Galois's most used drum sounds (his hi-hat and his second downbeat).
        public static readonly HashSet<int> PercussionSounds = new HashSet<int>()
        {
            42, 51, 66, 73, 81, 295, 296, 304, 305, 309, 310, 328, 563, 569, 737, 835, 836,
            931, 937, 942, 943, 944, 946, 1285, 1334, 1336, 1337, 1338,
        };
        public static bool IsPercussion(string sound)
        {
            return IsSound(sound) && int.TryParse(sound.Substring(1), out int id) && PercussionSounds.Contains(id);
        }
        private static bool IsSelector(string? sound)
        {
            int direction;
            return IsSelector(sound, out direction);
        }
        private static bool IsSelector(string? sound, out int direction)
        {
            direction = 0;

            // must be of the form +[+] | -[-] note (note can be 1-3 characters)
            // there are also only 3 instruments, so the offset can never exceed 2 (or be 0)
            /* Explained:
             * in order:
             * Harp < Lapharp < Lute
             * Whatever your main instrument ([play instrument harp) is set to, you add a "+" or "-" or two of them to play the note from the instrument to the left or right of your main selected.
             * So if harp were my set instrument, and i wanted a note from the lute, i'd add ++.
             * If Lute were my set instrument, and i wanted a note from the Harp, i'd add --.
             * If Lute were my set instrument, and i wanted a note from the Lapharp, i'd add -.
             * If lapharp were my set instrument, and i would only need a single + or - to play the other instruments.
             */
            if (string.IsNullOrEmpty(sound) || sound.Length < 2 || (sound[0] != '+' && sound[0] != '-'))
                return false;

            int pluses = 0;
            int minuses = 0;
            int chars = 0;
            bool havenote = false;
            for (int i = 0; i < sound.Length; i++)
            {
                if (char.IsLower(sound[i]))
                {
                    havenote = true;
                    chars++;
                }
                else if (sound[i] == '+')
                {
                    if (havenote || minuses > 0)
                        return false;
                    pluses++;
                    direction++;
                }
                else if (sound[i] == '-')
                {
                    if (havenote || pluses > 0)
                        return false;
                    minuses++;
                    direction--;
                }
                else return false;
            }
            if (pluses > 2 || minuses > 2 || chars > 3 || direction == 0 || Math.Abs(direction) > 2)
                return false;

            return true;
        }
        private static bool ParseSelector(string? sound, out int direction)
        {
            return IsSelector(sound, out direction);
        }
        public static HashSet<string> validNotes = new HashSet<string>()
        {   "cl", "csl", "d", "ds", "e", "f", "fs", "g", "gs", "a", "as",
            "b", "c", "cs", "dh", "dsh", "eh", "fh", "fsh", "gh", "gsh",
            "ah", "ash", "bh", "ch"
        };
        private static Regex transpose = new Regex(@"\[([+-])(\d\d?)\]", RegexOptions.Compiled);
        private static Regex tempo = new Regex(@"\<([+*-\/])([0-9]*[.]?[0-9]+)\>", RegexOptions.Compiled);
        private static Regex pause = new Regex(@"^([-]?)([0-9]*[\.]?[0-9]+)$", RegexOptions.Compiled);
        public static string? ExpandMacros(Mobile m, string musicString)
        {
            return ExpandMacros(MusicConfig[m], m, musicString);
        }
        // ctx: the session whose macros apply (m's context, or a music device's own session: m == null)
        public static string? ExpandMacros(MusicContext ctx, Mobile? m, string musicString)
        {
            // first for a quick test
            int first;
            if ((first = musicString.IndexOf('%')) == -1)
                // no macros here
                return musicString;

            Queue<int> positions = new Queue<int>();
            for (int ix = 0; ix < musicString.Length; ix++)
                if (musicString[ix] == '%')
                    // record it
                    positions.Enqueue(ix);

            List<string> macros = new List<string>();
            while (positions.Count > 0)
            {
                int head, tail;
                head = positions.Dequeue();
                if (positions.Count == 0)
                {   // error in macro format
                    m?.SendMessage("Error: ill-formed macro.");
                    return null;
                }
                tail = positions.Dequeue();
                macros.Add(musicString.Substring(head + 1, tail - head - 1));
            }
            ///
            // process decorations
            ///
            for (int ix = 0; ix < macros.Count; ix++)
            {
                string macro = macros[ix];
                string raw = Raw(macro);    // macro name without decorations
                if (!ctx.Macros.ContainsKey(raw))
                {
                    m?.SendMessage(string.Format("Error: Unknown macro '{0}'.", macro));
                    return null;
                }

                // this is the macro we will apply transformations on
                string modified_macro = ctx.Macros[raw];

                // macro B7 =  b dsh fsh ah
                // - reversing([play % -B7 % would do [play a fs ds b)
                if (macro[0] == '-')
                {   // simple reverse
                    modified_macro = ReverseWords(modified_macro);
                    ctx.ComplexityPoints++;
                }

                // - adjusting tempo([play % blues < *2 >% would multiply all pauses by 2, [play % blues < -0.05 >% could subtract 0.05 from all pauses)
                var match_tempo = tempo.Match(macro);
                if (match_tempo.Success)
                {
                    string text = match_tempo.Groups[0].Value;
                    string op = match_tempo.Groups[1].Value;
                    string val = match_tempo.Groups[2].Value;
                    object[] chunks = modified_macro.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int jx = 0; jx < chunks.Length; jx++)
                    {
                        var match_pause = pause.Match(chunks[jx] as string);
                        if (!match_pause.Success)
                            continue;

                        string opx = match_pause.Groups[1].Value;
                        string number = match_pause.Groups[2].Value;
                        double d;
                        if (double.TryParse(number, out d))
                            ;

                        double valx;
                        if (double.TryParse(val, out valx))
                            ;

                        switch (op)
                        {
                            case "+":
                                {
                                    chunks[jx] = opx + (d + valx).ToString();
                                    break;
                                }
                            case "-":
                                {
                                    chunks[jx] = opx + (d - valx).ToString();
                                    break;
                                }
                            case "*":
                                {
                                    chunks[jx] = opx + (d * valx).ToString();
                                    break;
                                }
                            case "/":
                                {
                                    chunks[jx] = opx + (d / valx).ToString();
                                    break;
                                }
                        }

                    }

                    string temp = string.Empty;
                    for (int kx = 0; kx < chunks.Length; kx++)
                        temp += chunks[kx].ToString() + " ";

                    modified_macro = temp.Trim();
                    ctx.ComplexityPoints++;
                }

                // -transposing([play % blues[-1] %) would transpose all of the notes in % blues % down by 1 halfstep(c becomes b, ds becomes d, etc)
                var match_transpose = transpose.Match(macros[ix]);
                if (match_transpose.Success)
                {
                    string opx = match_transpose.Groups[1].Value;
                    string number = match_transpose.Groups[2].Value;
                    string[] chunks = modified_macro.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int mx = 0; mx < chunks.Length; mx++)
                    {
                        int index = Array.IndexOf(NoteLookup, chunks[mx]);
                        if (index == -1)
                            // not a note
                            continue;

                        int offset;
                        int.TryParse(number, out offset);

                        // still need instrument wraparound
                        index += (opx == "+" ? offset : -offset);
                        if (index < 0 || index >= NoteLookup.Length)
                            chunks[mx] = string.Empty;  // drop the note
                        else
                            chunks[mx] = NoteLookup[index];
                    }

                    string temp = string.Empty;
                    for (int kx = 0; kx < chunks.Length; kx++)
                        temp += chunks[kx].ToString() + " ";

                    modified_macro = temp.Trim();
                    ctx.ComplexityPoints++;
                }

                // replace the text verbatim
                musicString = musicString.Replace("%" + macros[ix] + "%", modified_macro);
            }
            m?.SendMessage(musicString);
            AimsLog.WriteLine("macro: {0}", ConsoleColor.Cyan, musicString);
            return musicString;
        }
        private static string[] NoteLookup = new string[]
        {
            "cl",
            "csl",
            "d",
            "ds",
            "e",
            "f",
            "fs",
            "g",
            "gs",
            "a",
            "as",
            "b",
            "c",
            "cs",
            "dh",
            "dsh",
            "eh",
            "fh",
            "fsh",
            "gh",
            "gsh",
            "ah",
            "ash",
            "bh",
            "ch"
        };
        public static string ReverseWords(string s)
        {
            string[] wordArray = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Array.Reverse(wordArray);
            string temp = string.Empty;
            foreach (string word in wordArray)
                temp += word + " ";
            return new string(temp);
        }
        public static string Raw(string s)
        {
            string raw = string.Empty;
            bool first = true;
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c))
                    raw += c;
                else if (first == false)    // handles the leading '-' for reversing
                    return raw;         // we've started decorations, just return what we have
                first = false;
            }
            return raw;                     // no decorations beyond maybe a leading '-' for reversing
        }
        public static bool IsVolume(string? text)
        {
            // v[0-99]
            // Although i think the effective volumn cuts off at about 15, so any thing above that will be clipped
            if (!string.IsNullOrEmpty(text) && (text.Length == 2 || text.Length == 3) && text.ToLower()[0] == 'v' && char.IsDigit(text[1]))
                if (text.Length == 2) return true;
                else if (text.Length == 3) return char.IsDigit(text[2]);

            return false;
        }
        [Usage("Play note|pause [note|pause]")]
        [Description("Plays a note or a series of notes and pauses.")]
        public static void Play_OnCommand(CommandEventArgs e)
        {   // called via the [play command on the commandline
            // [play preview: hear a composition book (compiled) before transcribing it; again to stop (ManuscriptPaper)
            if (e.Length == 1 && e.GetString(0).Equals("preview", StringComparison.OrdinalIgnoreCase))
            {
                ManuscriptPaper.Preview(e.Mobile);
                return;
            }

            // one performance at a time: while a sheet's performance plays on this context, typed notes and settings would
            //  join its play list or change its playback; only looking, or a reset (which ends the performance), is allowed
            if (RazorInstrument.PerformanceOf(e.Mobile) is RazorInstrument.Performance performance)
            {
                string first = e.Length > 0 ? e.GetString(0).ToLowerInvariant() : "";
                if (first == "reset")
                    performance.Cancel(say: false);                 // ends it cleanly; then the context is replaced
                else if (!LookOnlyPlayCommands.Contains(first))
                {
                    if (ManuscriptPaper.IsPreview(performance.Instrument))
                    {
                        e.Mobile.SendMessage("You are previewing {0}. Type [play preview to stop it, or wait for it to finish.", SongTitle(performance));
                        return;
                    }
                    e.Mobile.SendMessage("You are performing {0}. Double-click the sheet to stop it, or wait for it to finish.", SongTitle(performance));
                    return;
                }
            }
            Player(e.Mobile, e.ArgString, true);
        }
        // [play commands that change nothing (allowed during a performance)
        private static readonly HashSet<string> LookOnlyPlayCommands = new() { "config", "list", "inspect", "musickarma" };
        private static string SongTitle(RazorInstrument.Performance performance)
        {
            string? title = (performance.Instrument as RolledUpSheetMusic)?.Title;
            if (string.IsNullOrWhiteSpace(title))
                title = performance.Instrument.SongName;
            return string.IsNullOrWhiteSpace(title) ? "a song" : title.Trim();
        }
        public static void Player(Mobile m, string musicString, bool IsCommandLine = false, bool bSilent = false)
        {   // API called via RSM
            PlaySession(null, m, musicString, IsCommandLine, bSilent);
        }
        // session: a music device's own session (m == null), or null to use (and create if needed) m's context
        public static void PlaySession(MusicContext? session, Mobile? m, string musicString, bool IsCommandLine = false, bool bSilent = false)
        {
            // lets see who's using the music system
            if (Utility.Random(10) == 0)
                if (IsCommandLine)
                    AimsLog.DebugOut(string.Format("{0} composing music.", m?.ToString() ?? "a music device"), ConsoleColor.Cyan);
                else
                    AimsLog.DebugOut(string.Format("{0} is listening to music.", m?.ToString() ?? "a music device"), ConsoleColor.Cyan);

            // Allows dynamic control through the CoreManagementConsole.
            if ((m?.AccessLevel ?? AccessLevel.Player) < AimsHost.Current.PlayAccessLevel)
            {
                m?.SendMessage("Playing music is currently disabled.");
                return;
            }
            if (string.IsNullOrEmpty(musicString) || string.IsNullOrWhiteSpace(musicString))
            {
                m?.SendMessage("Error: ill-formed [play command.");
                return;
            }

            // initialize player context for this mobile
            MusicContext ctx;
            if (session != null)
                ctx = session;
            else
            {
                InitializeContext(m!);
                ctx = MusicConfig[m!];
            }

            // setup the source for this music
            if (ctx.Source == null)
                ctx.Source = m;

            // expand macros
            if ((musicString = ExpandMacros(ctx, m, musicString)) == null)
                // something went wrong with macro expansion, the user has already been informed
                return;

            // chop up the string into 'notes'
            string[] notes = musicString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Object? LastItem = null;

            // keep track of how many times we've seen this line and scale gains accordingly
            int gain_offset = 0;
            if (IsCommandLine)
                gain_offset = GetGainOffset(ctx, musicString);

            // process configure options ... don't try and 'play' them
            if (DoConfigure(ctx, m, notes, bSilent) == true)
                return;

            // used to unlock special sound effects
            int musicKarma = 1000;                          // rolled up sheet music will play sound effects
            if (IsCommandLine)
                musicKarma = MusicLibrary.GetMusicKarma(m!);     // composers won't be able to use them intil music karma is >= 1000

            // check for too many notes.
            if (ctx.PlayList.Count > MaxQueueSize)
            {
                m?.SendMessage("Warning: Max queued notes reached.");
                return;
            }

            // record this session (if the recorder is turned on)
            if (m != null)                                  // a music device records nothing
                Engines.MusicRecorder.Record(m, musicString);

            // points for each note
            ctx.ObjectCount += notes.Length;

            for (int i = 0; i < notes.Length; ++i)
            {
                string note = notes[i].ToLower();
                double d;
                // If the argument is a note or a sound, add it directly to the queue.
                //  You need 1000 music karma to play sound effects (but not percussion)
                if (IsSound(note) && !IsPercussion(note) && musicKarma < 1000)
                {
                    m?.SendMessage("Warning: You do not yet have sufficient CoreMusicPlayer Karma to play sound effects.");
                    continue;
                }
                // it's a play object
                else if (validNotes.Contains(note) || IsSound(note) || IsSelector(note) || IsVolume(note))
                {
                    // Detect repeated notes
                    if (!ctx.NewTimer)
                        if (ctx.PlayList.Count > 0 && LastItem is String && ((String)LastItem).ToLower() == note && ctx.MusicChatter)
                            m?.SendMessage("Warning: Repeated note detected. Some notes may not play. Insert a 0.3 pause between repeated notes.");
                    ctx.PlayList.Enqueue(note);
                    LastItem = note;
                    continue;
                }
                // it's a pause
                else if (double.TryParse(note, out d) == true)
                {
                    if (!ctx.NewTimer)
                        // classic timer only supports pauses of this range
                        if (!(d >= 0.0 && d <= 1.0))
                        {
                            Usage(ctx, m);
                            return;
                        }

                    ctx.PlayList.Enqueue(d); // If so, add it to the queue as a double.
                    LastItem = note;
                    continue;
                }
                else
                {
                    Usage(ctx, m);
                    return;
                }
            }

            // did we get anything to play?
            if (ctx.PlayList.Count == 0)
            {
                Usage(ctx, m);
                return;
            }

            // process the queue
            ProcessPlayQueue(ctx, m);

            // if they made it this far, we will award some points for composition
            // do they gain a point?
            if (m is PlayerMobile pm && pm.NpcGuild == NpcGuild.BardsGuild)
            {   // 1. must come in on a commandline which indicates 'composition' and not a replay of RSM.
                // 2. make gains more difficult the more times we've seen this line.
                //  we count the times we have seen this line (gain_offset) and add that the the random chance 1-in-10
                //  so gains get progressively the more repeat lines we see (anti-macro)
                if (IsCommandLine && Utility.Random(10 + gain_offset) == 0)
                {   // how gains are calculated:
                    //  they get 1 point per note, capped at 10.
                    //  they get a 1/2 point for each complex musical construct (music algebra)
                    //  We then use AdjustedGain to reduce these points to some fractional value based on their current skill level.
                    //  Example: At Novice they are recveiving nearly full point credits, and nearing Legendary, they are recveiving only 1/100 of a point
                    double point_gain = ctx.ObjectCount * 0.5;
                    // factor in the bonus for using music algebra
                    point_gain += (ctx.ComplexityPoints * .5);
                    // max out at 10 points
                    // point_gain = Math.Min(point_gain, 10.0);
                    double adjusted_gain = AdjustedGain(AimsHost.Current.GetMusicKarma(pm), point_gain);
                    AimsHost.Current.SetMusicKarma(pm, AimsHost.Current.GetMusicKarma(pm) + adjusted_gain);
                    OnSkillChange(m, adjusted_gain);
                }
                //Console.WriteLine("Chance to gain is 1/{0}", 10 + gain_offset);
            }
            ctx.ObjectCount = ctx.ComplexityPoints = 0;
        }

        /*
         * DifficultyTable will remain only until we have nailed down the skill gain formula.
         * Once set in stone, we can delete this.
         * PS. I think we are very close now.
         */
        public class DifficultyTable
        {
            public double[] Table;
            public DifficultyTable()
            {   // I think those could be reduced to 0.1 after gm,
                //  and the gains rates for all levels after 100 reduced to 1/10 of their present levels
                List<double> dlist = new List<double>();
                for (double ix = 0.001; ix <= 1.0; ix += 0.01)
                    dlist.Add(ix);

                Table = dlist.ToArray();
                Array.Reverse(Table);
            }
        }
        private static DifficultyTable DifficultyLookup = new DifficultyTable();

        /*	** OSI MODEL **
			1.  Legendary 120 
			2.  Elder 110 
			3.  Grandmaster 100 
			4.  Master 90 
			5.  Adept 80 
			6.  Expert 70 
			7.  Journeyman 60 
			8.  Apprentice 50 
			9.  Novice 40 
			10. Neophyte 30 
			No Title 29 or below 
		 */
        public static double AdjustedGain(in double current_skill, double proposed_gain)
        {
            /*LogHelper logger = new LogHelper("'991-'001.log", true, true);
            for (int ix=0; ix < DifficultyLookup.Table.Count(); ix++)
            {
                logger.Log(string.Format("Table[{0:D2}]={1:N3}", ix.ToString(), DifficultyLookup.Table[ix]));
            }
            logger.Finish();*/
            double gain = 0;
            if (current_skill < 10)
                gain = AdjustedGain(10.0, proposed_gain);
            // very quick gains, nearly full point gains
            else if (current_skill < 200)   // No Title 29 or below 
                gain = proposed_gain * DifficultyLookup.Table[0];
            else if (current_skill < 300)   // Neophyte 30 
                gain = proposed_gain * DifficultyLookup.Table[50];
            else if (current_skill < 400)   // Novice 40 
                gain = proposed_gain * DifficultyLookup.Table[70];
            else if (current_skill < 500)   // Apprentice 50 
                gain = proposed_gain * DifficultyLookup.Table[83];
            else if (current_skill < 600)   // Journeyman 60 
                gain = proposed_gain * DifficultyLookup.Table[84];
            else if (current_skill < 700)   // Expert 70 
                gain = proposed_gain * DifficultyLookup.Table[85];
            else if (current_skill < 800)   // Adept 80 
                gain = proposed_gain * DifficultyLookup.Table[86];
            else if (current_skill < 900)   // Master 90 
                gain = proposed_gain * DifficultyLookup.Table[95];
            else if (current_skill < 1000)  // Grandmaster 100 
                gain = proposed_gain * DifficultyLookup.Table[96];
            else if (current_skill < 1100)  // Elder 110 
                gain = proposed_gain * DifficultyLookup.Table[97];
            else if (current_skill < 1190)
                gain = proposed_gain * DifficultyLookup.Table[98];
            // very slow, 0.001 of point gains
            else if (current_skill < 1200)  // Legendary 120 
                gain = proposed_gain * DifficultyLookup.Table[99];

            // we allow the user to keep collecting points, we may use them later.
            //  Note the MusicBox is similar in that it continues to add whole, unmodified points for Sales, Plays, and Publications
            else if (current_skill >= 1200)
                gain = proposed_gain * DifficultyLookup.Table[99];

            return gain;
        }
        private static int GetGainOffset(MusicContext ctx, string line)
        {
            // keep track of how many times we've seen this line and scale gains accordingly
            //  special note: I wanted to 'trim' the database here, but it seemed to lag the player so 
            //  I moved it to a Pack() routine called during World Save

            ///////////////////////////
            // This is really fast
            // Time taken 00:00:00.0000013
            //Stopwatch Timer = new Stopwatch();
            //Timer.Start();

            int hash = AimsUtil.GetStableHashCode(line, version: 1);
            if (ctx.SameLine.ContainsKey(hash))
                ctx.SameLine[hash]++;
            else
                ctx.SameLine.Add(hash, 0);

            //Timer.Stop();
            //Console.WriteLine("Time taken {0}", Timer.Elapsed);
            ///////////////////////////

            //Console.WriteLine("SameLine depth {0}", ctx.SameLine.Count);

            // return the number of times we've seen this line
            return ctx.SameLine[hash];
        }
        public static void OnSkillChange(Mobile m, double points_gained)
        {
            double real = AimsHost.Current.GetMusicKarma(m);                                                   // actual real skill points
            double before_change = real - points_gained;                                                        // points before the gain
            MusicConfig[m].LastGain = MusicConfig[m].LastGain == 0 ? before_change : MusicConfig[m].LastGain;   // on server restart, seed last gain with a reasonable value
            double delta = real - MusicConfig[m].LastGain;                                                      // how many points gained since last gain
            double display = ((real > 1200) ? 1200 : real) / 10.0;                                              // total display points
            if (before_change < 1200)
                if (delta >= .1 || display == 120)
                    m.SendMessage("Your skill in CoreMusicPlayer Composition increased. It is now {0:N1}", display);
            // else, don't show sub .1 gains
            AimsLog.DebugOut(string.Format("{1}: skill is now {0}", display, m), ConsoleColor.Cyan);
            if (real >= 1200)
            {
                AimsLog.DebugOut(string.Format("{1}: skill is now {0}", display, m), ConsoleColor.Red);
                if (MusicConfig[m].SkillTesting)
                {
                    AimsLog.DebugOut(string.Format("End test at {0}", DateTime.UtcNow), ConsoleColor.Red);
                    MusicConfig[m].SkillTesting = false;
                }
            }
            MusicConfig[m].LastGain = real;                                                                     // accumulator for point gains < .1
        }
        public static void ProcessPlayQueue(Mobile from)
        {
            ProcessPlayQueue(MusicConfig[from], from);
        }
        // ctx: the session to play (pm's context, or a music device's own session: pm == null)
        public static void ProcessPlayQueue(MusicContext ctx, Mobile? pm)
        {
            int tempo = ctx.Tempo;
            bool newTimer = ctx.NewTimer;
            bool prefetch = ctx.Prefetch;
            bool musicChatter = ctx.MusicChatter;

            if (!ctx.Playing) // If this is a new tune, create a new timer and start it.
            {
                if (musicChatter)
                    pm?.Emote("*plays a tune*"); // Player emotes to indicate they are playing
                ctx.Playing = true;
                if (newTimer == true)
                {
                    //Utility.ConsoleOut("new timer model", ConsoleColor.Cyan);
                    TimerStateCallback callback = new TimerStateCallback(DynamicTimer);
                    // the sixth element is the note's due time, the seventh the run (MusicEngineMetrics: lateness, drift)
                    bool anchored = ctx.TimerMode == TimerMode.Anchored;
                    TimeSpan first = anchored ? AnchorStart(ctx, tempo) : TimeSpan.FromMilliseconds(tempo);
                    MusicEngineMetrics.Run run = MusicEngineMetrics.RunStarted(anchored);
                    MusicEngineMetrics.RunScored(run, first.TotalMilliseconds);
                    Timer.DelayCall(first, callback,
                        new object?[] { pm, callback, tempo, prefetch, ctx, MusicEngineMetrics.Due(first), run });
                    MusicEngineMetrics.TimerStarted(ctx);
                }
                else
                {
                    //Utility.ConsoleOut("old timer model", ConsoleColor.Cyan);
                    //if (instrument is RazorInstrument)
                    //Utility.ConsoleOut("at time start, instrument is {0}", ConsoleColor.Cyan, (instrument as RazorInstrument).GetInstrumentType().ToString());
                    ClassicTimer pt = new ClassicTimer(ctx, pm);
                    pt.Run = MusicEngineMetrics.RunStarted();
                    pt.Start();
                    pt.Measured = true;
                    MusicEngineMetrics.TimerStarted(ctx);
                }
            }
            else
            {
                //Utility.ConsoleOut("Queueing while playing. Backlog: {0}", ConsoleColor.Cyan, ctx.PlayList.Count);
            }
        }
        private static int NoteCount(string[] notes)
        {
            int count = 0;
            foreach (string note in notes)
                if (char.IsLetter(note[0]))
                    count++;
            return count;
        }
        public static void Usage(Mobile to)
        {
            Usage(MusicConfig[to], to);
        }
        public static void Usage(MusicContext ctx, Mobile? to)
        {
            to?.SendMessage("Usage: [play note|pause [note|pause] ...");
            // you messed up .. you lose
            // Reset the [play context variables
            ctx.ComplexityPoints = 0;
            ctx.ObjectCount = 0;
        }

        public class ClassicTimer : Timer
        {
            private Mobile? m_Mobile;                           // the session's mobile (null for a music device's own session)
            private MusicContext? m_Session;                    // the context this performance started with (session identity)
            public DateTime m_PauseTime;
            internal bool Measured;                             // started by ProcessPlayQueue (MusicEngineMetrics counts it)
            private long m_LastTick;                            // Stopwatch time of the previous tick (MusicEngineMetrics: lateness)
            internal MusicEngineMetrics.Run? Run;               // this timer's run (MusicEngineMetrics: drift)
            private bool m_Played;                              // a tick has played (each later tick that plays scores 100 ms)

            public ClassicTimer(Mobile pm)
                : this(MusicConfig.TryGetValue(pm, out MusicContext? session) ? session : null, pm)
            {
            }
            public ClassicTimer(MusicContext? session, Mobile? pm)
                : base(TimeSpan.FromSeconds(0.0), TimeSpan.FromSeconds(0.1), 0)
            {
                m_Mobile = pm;
                m_Session = session;
                Priority = TimerPriority.FiftyMS;
                m_PauseTime = DateTime.UtcNow;
            }

            // the timer is done with its session (MusicEngineMetrics); interrupted: the session was released or replaced
            private void Ended(bool interrupted)
            {
                if (Measured)
                    MusicEngineMetrics.TimerEnded(m_Session);
                Measured = false;
                MusicEngineMetrics.RunEnded(Run, interrupted);
            }
            protected override void OnTick()
            {
                using MusicEngineMetrics.Scope scope = MusicEngineMetrics.Enter(MusicEngineMetrics.Kind.Engine);
                if (!MusicEngineMetrics.Nested)
                {   // how late this tick is against the 100 ms interval
                    long now = MusicEngineMetrics.Now;
                    MusicEngineMetrics.ClassicTick(m_LastTick == 0 ? 0 : MusicEngineMetrics.LateMs(m_LastTick) - 100, Measured && m_LastTick != 0);
                    m_LastTick = now;
                }

                // we can't use MusicConfig[].VirtualInstrument past this point since we may be using
                //  instrument offsets, that is, instruments relative to the VirtualInstrument. I.e., -cl +cl
                InstrumentType instrument = InstrumentType.Harp;

                // the context was released (ReleaseContext, or a device session's end) or replaced ([play reset) while we
                //  were playing: this performance is over. Stop without touching the replacement context.
                if (!IsCurrent(m_Session, m_Mobile))
                {
                    Ended(interrupted: true);
                    Stop();
                    return;
                }
                MusicContext live = m_Session!;

                // let the caller know we are still running
                if (live.Tick != null)
                    live.Tick(m_Mobile, live.Source);

                // wait on pause
                if (DateTime.UtcNow < m_PauseTime)
                    return;

                // MusicEngineMetrics: each tick that plays after the first is one 100 ms step of the score
                if (!MusicEngineMetrics.Nested)
                {
                    if (m_Played)
                        MusicEngineMetrics.RunScored(Run, 100);
                    m_Played = true;
                }

                if (live.PlayList.Count == 0) // If the tune is done, stop the timer.
                {
                    live.Playing = false;
                    Ended(interrupted: false);
                    Stop();
                    return;
                }
                else
                {
                    try
                    {
                        // always select the default instrument before proceeding
                        MusicContext mo = live;
                        mo.SelectInstrument();
                        instrument = mo.VirtualInstrument;
                        // dequeue an object
                        object obj = live.PlayList.Dequeue();

                        // switch instruments now?
                        if (obj is RazorInstrument.InstrumentSoundType)
                        {
                            mo = live;
                            mo.Selector = (RazorInstrument.InstrumentSoundType)obj;
                            mo.SelectInstrument();
                            instrument = mo.VirtualInstrument;
                            //Utility.ConsoleOut("Instrument {0}", ConsoleColor.Red, mo.Selector.ToString());
                            OnTick();
                            return;
                        }
                        else if (obj.GetType() == typeof(string) && IsVolume(obj as string))
                        {
                            int volume;
                            int.TryParse((obj as string).Substring(1), out volume);
                            live.Volume = volume;
                            OnTick();
                            return;
                        }
                        else if (obj.GetType() == typeof(string) && IsSelector(obj as string))
                        {
                            int index;
                            if (ParseSelector(obj as string, out index) && live.GetOffsetInstrument(index) is InstrumentType offsetInstrument)
                            {
                                instrument = offsetInstrument;
                                // okay, now fixup the note by stripping away all selector decorations
                                obj = (obj as string).Replace("+", "").Replace("-", "").Trim();
                                if (!validNotes.Contains(obj as string))
                                {
                                    m_Mobile?.SendMessage("Error: bad note{0}.", obj as string);
                                    return;
                                }
                            }
                            else
                            {
                                // bad selector. 
                                m_Mobile?.SendMessage("Error: bad instrument offset.");
                                return;
                            }
                        }

                        // okay, play the note!
                        if (obj.GetType() == typeof(string))
                        {
                            CoreMusicPlayer.PlayNote(live, m_Mobile, (string)obj, instrument);
                            MusicEngineMetrics.Note();
                            //Utility.ConsoleOut("Note {0}", ConsoleColor.Red, (string)obj);
                        }
                        // If the first item is a double, treat it as a pause.
                        else if (obj is double)
                        {
                            double pause = (double)obj;
                            m_PauseTime = DateTime.UtcNow + TimeSpan.FromSeconds(pause);
                            // MusicEngineMetrics: the rest lasts until the first 100 ms tick at or after its end (that tick scores its own 100 ms)
                            MusicEngineMetrics.RunScored(Run, Math.Max(0, Math.Ceiling(pause * 10 - 1e-9) * 100 - 100));
                            return;
                        }
                        else
                            AimsLog.WriteLine("Error: Unknown object type {0}", ConsoleColor.Red, (string)obj);
                    }
                    catch (Exception ex)
                    {
                        AimsLog.LogException(ex);
                    }
                }
            }
        }
        private static void DynamicTimer(object state)
        {
            object[] aState = (object[])state;
            Mobile? player = aState[0] as Mobile;                           // mobile playing this song (null for a music device's own session)
            TimerStateCallback callback = (TimerStateCallback)aState[1];    // this routine
            TimeSpan tempo = TimeSpan.FromMilliseconds((int)aState[2]);     // tempo
            bool prefetch = (bool)aState[3];                                // prefetch next timer delay
            MusicContext? session = aState[4] as MusicContext;              // the context this performance started with (session identity)
            TimeSpan delay = tempo;                                         // default delay = tempo

            // MusicEngineMetrics: the note's due time (absent when a test drives this directly)
            bool measured = aState.Length > 5 && aState[5] is long;
            MusicEngineMetrics.Run? run = aState.Length > 6 ? aState[6] as MusicEngineMetrics.Run : null;
            using MusicEngineMetrics.Scope scope = MusicEngineMetrics.Enter(MusicEngineMetrics.Kind.Engine);
            if (!MusicEngineMetrics.Nested)
                MusicEngineMetrics.DynamicTick(measured ? MusicEngineMetrics.LateMs((long)aState[5]) : 0, measured);

            // we can't use MusicConfig[].VirtualInstrument past this point since we may be using
            //  instrument offsets, that is, instruments relative to the VirtualInstrument. I.e., -cl +cl
            InstrumentType instrument = InstrumentType.Harp;

            // incase we defraged the database while a timer was running (released), or
            //  the context was replaced ([play reset) while we were playing: this performance is over.
            //  Stop without touching the replacement context.
            if (!IsCurrent(session, player))
            {
                if (measured)
                    MusicEngineMetrics.TimerEnded(session);
                MusicEngineMetrics.RunEnded(run, interrupted: true);
                return;
            }
            MusicContext live = session!;

            // let the caller know we are still running
            if (live.Tick != null)
                live.Tick(player, live.Source);

            // Anchored: this note's scheduled time (the next one is due a delay after it, whenever this one actually ran)
            bool anchored = live.TimerMode == TimerMode.Anchored;
            long scheduled = anchored ? AnchorDue(live) : 0;

            if (live.PlayList.Count == 0) // If the tune is done, stop the timer.
            {   // (Anchored: the session keeps its schedule, AnchorNext; the next block may continue it)
                live.Playing = false;
                if (measured)
                    MusicEngineMetrics.TimerEnded(session);
                MusicEngineMetrics.RunEnded(run, interrupted: false);
                return;
            }
            else
            {
                try
                {
                    // always select the default instrument before proceeding
                    MusicContext mo = live;
                    mo.SelectInstrument();
                    instrument = live.VirtualInstrument;

                    // dequeue an object to play
                    object obj = live.PlayList.Dequeue();

                    // switch instruments now
                    if (obj is RazorInstrument.InstrumentSoundType)
                    {
                        mo = live;
                        mo.Selector = (RazorInstrument.InstrumentSoundType)obj;
                        mo.SelectInstrument();
                        instrument = live.VirtualInstrument;
                        DynamicTimer(state);
                        return;
                    }
                    else if (obj.GetType() == typeof(string) && IsVolume(obj as string))
                    {
                        int volume;
                        int.TryParse((obj as string).Substring(1), out volume);
                        live.Volume = volume;
                        DynamicTimer(state);
                        return;
                    }
                    else if (obj.GetType() == (typeof(string)) && IsSelector(obj as string))
                    {
                        int index;
                        if (ParseSelector(obj as string, out index) && live.GetOffsetInstrument(index) is InstrumentType offsetInstrument)
                        {
                            instrument = offsetInstrument;
                            // okay, now fixup the note by stripping away all selector decorations
                            obj = (obj as string).Replace("+", "").Replace("-", "").Trim();
                            if (!validNotes.Contains(obj as string))
                            {
                                player?.SendMessage("Error: bad note{0}.", obj as string);
                                goto skip;
                            }
                        }
                        else
                        {
                            // bad selector. 
                            player?.SendMessage("Error: bad instrument offset.");
                            goto skip;
                        }

                    }

                    // play the note!
                    if (obj.GetType() == (typeof(string)))
                    {
                        // play the note
                        CoreMusicPlayer.PlayNote(live, player, (string)obj, instrument);
                        MusicEngineMetrics.Note();
                        //Utility.ConsoleOut("note {0}", ConsoleColor.Green, (string)obj);

                        bool fauxPrefetch = false;
                        // faux prefetch negative pauses
                        if (live.PlayList.Count != 0 && live.PlayList.Peek() is double)
                        {
                            if ((double)live.PlayList.Peek() < 0)
                                fauxPrefetch = true;
                        }

                        // now look ahead to see if the next element is a pause. If so  we should process it now.
                        if (live.PlayList.Count != 0 && (prefetch || fauxPrefetch))
                        {
                            if (live.PlayList.Peek() is double)
                            {
                                obj = live.PlayList.Dequeue();
                                double offset = (double)obj * 1000;
                                if (fauxPrefetch)
                                    offset = Math.Abs(offset);
                                double pause = offset;

                                //Utility.ConsoleOut("(p)delay {0}", ConsoleColor.Cyan, delay.TotalMilliseconds);
                                //Utility.ConsoleOut("(p)offset {0}", ConsoleColor.Cyan, offset);
                                //Utility.ConsoleOut("(p)pause {0}", ConsoleColor.Cyan, pause);

                                delay = TimeSpan.FromMilliseconds(pause);
                            }
                        }
                    }
                    // If the first item is a double, treat it as a pause.
                    else if (obj is double)
                    {
                        double offset = (double)obj * 1000;
                        double pause = offset;

                        //Utility.ConsoleOut("delay {0}", ConsoleColor.Cyan, delay.TotalMilliseconds);
                        //Utility.ConsoleOut("offset {0}", ConsoleColor.Cyan, offset);
                        //Utility.ConsoleOut("pause {0}", ConsoleColor.Cyan, pause);

                        delay = TimeSpan.FromMilliseconds(pause);
                    }
                    else
                        AimsLog.WriteLine("Error: Unknown object type {0}", ConsoleColor.Red, (string)obj);
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                }
            }

        skip:
            MusicEngineMetrics.RunScored(run, delay.TotalMilliseconds);
            TimeSpan wait = delay;
            if (anchored)
            {   // the next note is due a delay after this one was scheduled, not after it ran
                long next = scheduled + MsToTicks(delay.TotalMilliseconds);
                live.AnchorNext = next;
                wait = TimeSpan.FromMilliseconds(Math.Max(0, TicksToMs(next - MusicEngineMetrics.Now)));
            }
            Timer.DelayCall(wait, callback,
                new object?[] { player, callback, (int)tempo.TotalMilliseconds, prefetch, session!, measured ? MusicEngineMetrics.Due(wait) : null, run });
        }

        [Usage("[StopMusic")]
        [Description("Stops a current melody.")]
        public static void StopMusic_OnCommand(CommandEventArgs e)
        {
            Mobile pm = (Mobile)e.Mobile;
            // a sheet's performance: end it properly, as double-clicking the sheet does (clearing the play list alone
            //  left its script running: the next block played after its wait)
            if (RazorInstrument.PerformanceOf(pm) is RazorInstrument.Performance performance)
            {
                performance.Cancel();
                pm.SendMessage("Music stopped.");
                return;
            }
            if (!MusicConfig.TryGetValue(pm, out MusicContext? ctx) || ctx.PlayList == null || (ctx.PlayList.Count == 0 && !ctx.Playing))
                pm.SendMessage("You are not playing anything. To stop hearing other players' music, use [FilterMusic.");
            else
            {
                ctx.PlayList.Clear();
                ctx.Playing = false;
                pm.SendMessage("Music stopped.");
            }
        }

        [Usage("[FilterMusic")]
        [Description("Toggles the ability to hear music")]
        public static void FilterMusic_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                bool filter = AimsHost.Current.GetFilterMusic(pm);
                if (!filter) pm.SendMessage("You are now filtering music.");
                else pm.SendMessage("You are no longer filtering music.");
                AimsHost.Current.SetFilterMusic(pm, !filter);
            }
        }

        #region Global Music Context Serialization
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Pack);
        }
        public static void Load()
        {
            if (!File.Exists("Saves/MusicContexts.bin"))
                return;

            Console.WriteLine("Global music contexts Loading...");
            BinaryFileReader? reader = null;
            try
            {
                reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/MusicContexts.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadEncodedInt();

                switch (version)
                {
                    case 0:
                        {
                            int count = reader.ReadEncodedInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Mobile? m = reader.ReadMobile();
                                if (m != null)
                                    MusicConfig.Add(m, new MusicContext(reader));
                                else
                                {
                                    // throw it away
                                    MusicContext temp = new MusicContext(reader);
                                    // free up instruments
                                    temp.Finish();
                                }
                            }
                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid MusicContexts.bin savefile version.");
                        }
                }
            }
            catch
            {
                AimsLog.WriteLine("Error reading Saves/MusicContexts.bin, using default values...", ConsoleColor.Red);
            }
            finally
            {
                if (reader != null)
                    reader.Close();

                // contexts saved before version 4 referenced three virtual instruments each: delete them
                MusicContext.ReleaseLegacyInstruments();
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Global music contexts Saving...");
            BinaryFileWriter? writer = null;
            try
            {
                writer = new BinaryFileWriter("Saves/MusicContexts.bin", true);
                int version = 0;
                writer.WriteEncodedInt(version);

                switch (version)
                {
                    case 0:
                        {
                            writer.WriteEncodedInt(MusicConfig.Count);
                            foreach (KeyValuePair<Mobile, MusicContext> kvp in MusicConfig)
                            {
                                writer.Write(kvp.Key);
                                kvp.Value.Serialize(writer);
                            }

                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                AimsLog.WriteLine("Error writing Saves/MusicContexts.bin", ConsoleColor.Red);
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }
        #endregion Global Music Context Serialization
    }

}