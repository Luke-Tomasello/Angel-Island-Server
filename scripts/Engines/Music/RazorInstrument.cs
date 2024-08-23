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

/* Scripts/Skill Items/Musical Instruments/RazorInstrument.cs
 * ChangeLog
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
        private string m_songBytes = null;                         // simple song notes
        private int m_place_obsolete = 0;
        private int m_year_obsolete = 0;
        private string m_songFile = null;                           // read song from file
        private string m_songMemory = null;                         // read song from temporal memory buffer
        private string m_preferredInstrument_obsolete = null;       // instrument specified in the comments of the song file
        private bool m_newTimer;                                    // timer model specified in the comments of the song file
        private int m_tempo;                                        // songs speed (timer speed in milliseconds)
        private bool m_prefetch;                                    // prefetch pauses from queue, speeds playback
        private bool m_concertMode;                                 // concert mode updates sound packets for adjusting volume
        private Queue<string> m_playQueue = new Queue<string>();
        private Stack<string> m_songBuffer = new Stack<string>();
        #region configuration
        private Item m_playItem;                                    // this is the item that 'sings the lyrics' (like the big fish trophy) (possibly obsolete)
        private Mobile m_playMobile;                                // this is the optional mobile that owns the MusicContext.
        private IEntity m_source;                                   // this is the item that 'sings the lyrics' (like the big fish trophy)
        private BaseInstrument m_instrument;
        private double m_minMusicSkill_obsolete = 0.0;
        private bool m_requiresBackpack = false;
        private bool m_musicChatter_doWeNeedThis = false;
        private bool m_checkMusicianship_obsolete = false;
        private bool m_ignoreDirectives_obsolete = false;
        private bool m_instance_playing = false;                    // is this particular instance playing? 
        private bool m_instance_debugging = false;                  // display debug for this instance
        public delegate void FinishCallback();
        public delegate bool CheckSourceStatusCallback(Mobile m, IEntity source);
        private FinishCallback m_allDone = null;
        private CheckSourceStatusCallback m_checkSourceStatus = null;
        public bool IsInstancePlaying { get { return m_instance_playing; } }
        public IEntity Source { get { return m_source; } }
        public bool IgnoreDirectives_obsolete { get { return m_ignoreDirectives_obsolete; } set { m_ignoreDirectives_obsolete = value; } }
        public bool RequiresBackpack { get { return m_requiresBackpack; } set { m_requiresBackpack = value; } }
        public bool MusicChatter_doWeNeedThis { get { return m_musicChatter_doWeNeedThis; } set { m_musicChatter_doWeNeedThis = value; } }
        public bool CheckMusicianship_obsolete { get { return m_checkMusicianship_obsolete; } set { m_checkMusicianship_obsolete = value; } }
        public FinishCallback AllDone { get { return m_allDone; } set { m_allDone = value; } }
        public CheckSourceStatusCallback CheckSourceStatus { get { return m_checkSourceStatus; } set { m_checkSourceStatus = value; } }
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
        public string PreferredInstrument_obsolete { get { return m_preferredInstrument_obsolete; } set { m_preferredInstrument_obsolete = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public double MinMusicSkill { get { return m_minMusicSkill_obsolete; } set { m_minMusicSkill_obsolete = value; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Author { get { return m_author; } set { m_author = value; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public string SongName { get { return m_songName; } set { m_songName = value; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public string SongBytes { get { return m_songBytes; } set { m_songBytes = value; } }
        //[CommandProperty(AccessLevel.GameMaster)]
        public int Place_obsolete { get { return m_place_obsolete; } set { m_place_obsolete = value; /*ModelInitialize(); InvalidateProperties();*/ } }
        public int Year_obsolete { get { return m_year_obsolete; } set { m_year_obsolete = value; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public string SongFile
        {
            get { return m_songFile; }
            set
            {
                if (value != null)
                {
                    if (value.ToLower().Contains(".rzr") && File.Exists(Path.Combine(Core.DataDirectory, "song files", value)))
                        m_songFile = value;
                    else if (!value.ToLower().Contains(".rzr") || !File.Exists(Path.Combine(Core.DataDirectory, "song files", value)))
                        m_songFile = "Please enter: <name>.rzr";
                }
                else
                    // user is setting 
                    m_songFile = value;
            }
        }
        public Item PlayItem { get { return m_playItem; } set { m_playItem = value; } }
        public Mobile PlayMobile { get { return m_playMobile; } set { m_playMobile = value; } }
        public string SongMemory { get { return m_songMemory; } set { m_songMemory = value; } }
        public BaseInstrument Instrument { get { return m_instrument; } set { m_instrument = value; } }
        public Queue<string> PlayQueue { get { return m_playQueue; } }
        public Stack<string> SongBuffer { get { return m_songBuffer; } }
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
                Utility.Monitor.WriteLine("GetInstrumentType() returning default Harp", ConsoleColor.Cyan);
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
        public RazorInstrument(int itemID, int wellSound, int badlySound, Item playItem)
            : base(itemID, wellSound, badlySound)
        {
            m_year_obsolete = DateTime.UtcNow.Year;
            //Name = GetName();
            //  ride of the Valkyries will be our default
            SongBytes = "b 0.1 fs b dh 0.3 b 0.3 dh 0.1 b dh fsh 0.3 dh 0.3 fsh 0.1 dh fsh ah 0.3 a 0.3 dh 0.1 a dh fsh";
            SongName = "Ride of the Valkyries";
            LootType = LootType.UnStealable | LootType.UnLootable; // a style of blessed, includes reds
            m_playItem = playItem;
            m_instrument = this;
        }
        public RazorInstrument(Serial serial)
            : base(serial)
        {
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
            from = m_playMobile ?? from;
            DebugOut(string.Format("OnDoubleClick in RZR: begin"));
            if (m_requiresBackpack && !IsChildOf(from.Backpack))
            {
                DebugOut(string.Format("OnDoubleClick in RZR: needs backpack"));
                from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
                return;
            }
            DebugOut(string.Format("OnDoubleClick in RZR: ready to call IsThisDevicePlaying"));
            if (IsThisDevicePlaying(from))
            {
                DebugOut(string.Format("OnDoubleClick in RZR: IsThisDevicePlaying returned true"));
                CancelPlayback(from);
                return;
            }
            DebugOut(string.Format("OnDoubleClick in RZR: ready to call ConflictedPlayContexts"));
            if (!ConflictedPlayContexts(from))
            {
                DebugOut(string.Format("OnDoubleClick in RZR: ready to call StartMusic"));
                StartMusic(from, m_instrument);
            }
            else if (WarnPlaybackConflicts())
            {
                DebugOut(string.Format("OnDoubleClick in RZR: ConflictedPlayContexts returned true"));
                from.SendMessage("You have initiated music in another context. You will need to cancel that or wait for it to finish.");
            }
            else if (Core.Debug)
                Utility.SendSystemMessage(from, 5, "Debug message: music not started because this player has initiated music elsewhere.");

            DebugOut(string.Format("OnDoubleClick in RZR: returning"));
        }
        public void CancelPlayback(Mobile from, bool say = true)
        {
            from = m_playMobile ?? from;
            if (IsThisDevicePlaying(from))
            {
                if (say && from != null)
                    from.Emote("*you cancel what you were playing*");
                m_paused = false;
                m_playQueue.Clear();
                m_songBuffer.Clear();
                if (from != null && SystemMusicPlayer.MusicConfig.ContainsKey(from))
                    SystemMusicPlayer.MusicConfig[from].PlayList.Clear();
                m_instance_playing = false;

                if (m_allDone != null)
                    m_allDone();
            }
        }
        public bool IsThisDevicePlaying(Mobile from)
        {
            from = m_playMobile ?? from;
            return m_playQueue.Count > 0 || m_songBuffer.Count > 0 || (from != null && SystemMusicPlayer.MusicConfig.ContainsKey(from) && SystemMusicPlayer.MusicConfig[from].Playing && IsInstancePlaying);
        }
        Utility.LocalTimer CheckUserTick = new Utility.LocalTimer();
        private void PlayerProgressCallback(Mobile m, IEntity source)
        {
            m = m_playMobile ?? m;
            // poll the user every two seconds to see if they should still be getting music
            if (CheckUserTick.Triggered)
            {
                CheckUserTick.Start(2000);

                // notify the caller to see if the mobile is around the hear the music
                if (m_checkSourceStatus != null)
                {
                    if (m_checkSourceStatus(m, source) == false)
                    {   // mobile is no longer online or close enough to the play item
                        CancelPlayback(m, false);
                        // let the caller know we are all done playing the song
                        // notes may still be playing on the client at this point, so we'll wait for them as well
                        if (m_allDone != null)
                            Timer.DelayCall(TimeSpan.FromSeconds(.5), new TimerStateCallback(WaitDone), new object[] { m, null });
                        return;
                    }
                }
            }
        }
        private IEntity EstablishSource()
        {
            IEntity source = null;
            object rootParent = (PlayItem is Item ix && ix.RootParent != null) ? ix.RootParent : this.RootParent;
            if (rootParent is Mobile)
                source = (Mobile)rootParent;   // have the mobile 'sing'.
            else if (rootParent is Item)
                source = (Item)rootParent;     // music object in another item, like a container for instance. Have the container 'sing'
            else if (PlayItem != null)
                source = PlayItem;
            else
                source = null;
            return source;
        }
        private Server.Commands.MusicContext.TickCallback EstablishCallback(Server.Commands.MusicContext.TickCallback callback)
        {
            return (callback == null) ? PlayerProgressCallback : null;    // use our default handler
        }
        private bool ConflictedPlayContexts(Mobile from)
        {
            from = m_playMobile ?? from;
            if (SystemMusicPlayer.IsUserBlocked(from))
                return true;
            return false;
        }
        public void StartMusic(Mobile from, BaseInstrument instrument, Server.Commands.MusicContext.TickCallback callback = null)
        {
            from = m_playMobile ?? from;
            if (!ConflictedPlayContexts(from))
            {
                m_instrument = instrument != null ? instrument : this;
                SetInstrument(from, m_instrument);
                m_playQueue.Clear();
                m_songBuffer.Clear();
                m_instance_playing = false;
                string song = null;
                m_paused = false;
                callback = EstablishCallback(callback);     // use our default handler if none is specified
                m_source = EstablishSource();               // what entity is acting as the 'source' of the music? Item or Mobile?
                CheckUserTick.Start(0);                     // two second user check
                                                            // these will be defaults. We will call this again if we are reading a 'file' and find music directives.
                SystemMusicPlayer.ConfigurePlayer(
                    from,
                    NewTimer,
                    Tempo,
                    Prefetch,
                    (m_instrument as RazorInstrument).GetInstrumentType().ToString(),
                    chatter: false,
                    concertMode: ConcertMode,
                    callback: callback,
                    source: Source);
                if (m_songBytes != null && m_songFile == null && m_songMemory == null)
                {   // just play the canned song
                    song = m_songBytes;
                    string[] notes = song.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string note in notes)
                        m_playQueue.Enqueue(note);

                    if (m_playQueue.Count > 0)
                        Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(NotePumpTick), new object[] { from });

                    m_instance_playing = true;
                }
                else
                {   // read and process a Razor style 'song file'
                    if (LoadSongFile(from) == true)
                    {
                        // configure the player based on the directives found in the file
                        SystemMusicPlayer.ConfigurePlayer(
                            from,
                            NewTimer,
                            Tempo,
                            Prefetch,
                            (m_instrument as RazorInstrument).GetInstrumentType().ToString(),
                            false,
                            ConcertMode,
                            callback: callback,
                            source: Source);

                        // call our baby Razor to process the script
                        Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), new object[] { from });

                        m_instance_playing = true;
                    }
                    else
                        from.Emote("*plays nothing*"); // Player emotes to indicate they are playing
                }
            }
            else if (WarnPlaybackConflicts())
                from.SendMessage("You have initiated music in another context. You will need to cancel that or wait for it to finish.");
            else if (Core.Debug)
                Utility.SendSystemMessage(from, 5, "Debug message: music not started because this player has initiated music elsewhere.");
        }
        private bool WarnPlaybackConflicts()
        {
            if (PlayItem is BaseAddon || PlayItem is TrophyAddon || PlayItem is MusicMotionController /*|| PlayItem is MusicBox || PlayItem is RolledUpSheetMusic*/)
                return false;
            return true;
        }
        private bool m_paused = false;
        public void Pause(Mobile m)
        {
            m = m_playMobile ?? m;
            if (IsThisDevicePlaying(m))
                m_paused = true;
        }
        public void Resume(Mobile m)
        {
            m = m_playMobile ?? m;
            if (IsThisDevicePlaying(m))
                m_paused = false;

        }
        private void BabyRazor(object state)
        {
            object[] aState = (object[])state;
            Mobile m = aState[0] as Mobile;

            // pause handling
            if (m_paused == true)
            {
                int count = 0;
                if (aState.Length > 1 && aState[1] is int)
                    count = (int)aState[1];

                if (count >= 500)
                {   /* 250 seconds, then cancel their song*/
                    this.OnDoubleClick(m);
                    if (m_allDone != null)
                        Timer.DelayCall(TimeSpan.FromSeconds(.5), new TimerStateCallback(WaitDone), new object[] { m, null });
                }
                else
                    Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(BabyRazor), new object[] { m, ++count });
                return;
            }

            if (m_songBuffer.Count > 0)
            {
                string line = m_songBuffer.Pop().Trim();
                // Debug
                //Utility.ConsoleOut(line, ConsoleColor.Green);
                // there are only 4 type of valid input
                if (line.ToLower().StartsWith("#"))
                    DoProcessComment(m, line);
                else if (line.ToLower().StartsWith("say '[play"))
                    DoProcessPlay(m, line);
                else if (line.ToLower().StartsWith("say"))
                    DoProcessSay(m, line);
                else if (line.ToLower().StartsWith("wait"))
                    DoProcessWait(m, line);
                else // ignore the line
                {
                    // call our baby Razor to continue processing this script 
                    Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), new object[] { m });
                }
            }
            else
            {
                // let the caller know we are all done playing the song
                // notes may still be playing on the client at this point, so we'll wait for them as well
                if (m_allDone != null)
                    Timer.DelayCall(TimeSpan.FromSeconds(.5), new TimerStateCallback(WaitDone), new object[] { m, null });
            }
        }
        private void WaitDone(object state)
        {
            object[] aState = (object[])state;
            Mobile m = aState[0] as Mobile;

            if (m != null)
                ;// debug

            // wait for this instance to finish
            if (!IsThisDevicePlaying(m))
            {
                m_instance_playing = false;
                if (m_allDone != null)
                    m_allDone();
            }
            else
                Timer.DelayCall(TimeSpan.FromSeconds(.5), new TimerStateCallback(WaitDone), new object[] { m, null });
        }
        private void DoProcessComment(Mobile m, string line)
        {
            m = m_playMobile ?? m;
            // call our baby Razor to continue processing this script 
            Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), new object[] { m });
        }
        private static Regex true_match = new Regex(@"true", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex int_match = new Regex(@"\d+", RegexOptions.Compiled);
        private static Regex wait_regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$", RegexOptions.Compiled);
        private void ParseDirectives(Mobile m, string line)
        {
            m = m_playMobile ?? m;
            if (line.ToLower().Contains("author:"))
            {   // razor instruments only care about the music, not author, how the player placed, etc.
                m_author = line;
                m_author = m_author.Replace("#", "");
                m_author = m_author.Replace("author:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
            }
            else if (line.ToLower().Contains("instrument:"))
            {
                string instrument = line;
                instrument = instrument.Replace("#", "");
                instrument = instrument.Replace("instrument:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                ConfigureInstrument(instrument);
            }
            else if (line.ToLower().Contains("prefetch:"))
            {
                string sx;
                bool prefetch;
                sx = line;
                sx = sx.Replace("#", "");
                sx = sx.Replace("prefetch:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                string resultString = true_match.Match(sx).Value;
                if (bool.TryParse(resultString, out prefetch) == false)
                    this.Prefetch = false;
                else
                    this.Prefetch = prefetch;
            }
            else if (line.ToLower().Contains("newtimer:"))
            {
                string sx;
                bool newTimer;
                sx = line;
                sx = sx.Replace("#", "");
                sx = sx.Replace("newtimer:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                string resultString = true_match.Match(sx).Value;
                if (bool.TryParse(resultString, out newTimer) == false)
                    this.NewTimer = false;
                else
                    this.NewTimer = newTimer;
            }
            else if (line.ToLower().Contains("tempo:"))
            {
                string sx;
                int tempo;
                sx = line;
                sx = sx.Replace("#", "");
                sx = sx.Replace("tempo:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                string resultString = int_match.Match(sx).Value;
                if (int.TryParse(resultString, out tempo) == false)
                    this.Tempo = Commands.SystemMusicPlayer.DefaultTempo;
                else
                    this.Tempo = tempo;
            }
            else if (line.ToLower().Contains("concertmode:"))
            {
                string sx;
                bool concertMode;
                sx = line;
                sx = sx.Replace("#", "");
                sx = sx.Replace("concertmode:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                string resultString = true_match.Match(sx).Value;
                if (bool.TryParse(resultString, out concertMode) == false)
                    this.ConcertMode = false;
                else
                    this.ConcertMode = concertMode;
            }
            else if (line.ToLower().Contains("song:"))
            {
                m_songName = line;
                m_songName = m_songName.Replace("#", "");
                m_songName = m_songName.Replace("song:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
            }
        }
        public void ConfigureInstrument(string instrument)
        {
            if (instrument.ToLower() == "harp")
            {
                Instrument.SuccessSound = (int)RazorInstrument.InstrumentSoundType.Harp;
                Utility.Monitor.WriteLine("configure as instrument {0}", ConsoleColor.Yellow, RazorInstrument.InstrumentSoundType.Harp.ToString());
            }
            else if (instrument.ToLower() == "lapharp")
            {
                Instrument.SuccessSound = (int)RazorInstrument.InstrumentSoundType.LapHarp;
                Utility.Monitor.WriteLine("configure as instrument {0}", ConsoleColor.Yellow, RazorInstrument.InstrumentSoundType.LapHarp.ToString());
            }
            else if (instrument.ToLower() == "lute")
            {
                Instrument.SuccessSound = (int)RazorInstrument.InstrumentSoundType.Lute;
                Utility.Monitor.WriteLine("configure as instrument {0}", ConsoleColor.Yellow, RazorInstrument.InstrumentSoundType.Lute.ToString());
            }
            else
            {
                Instrument.SuccessSound = (int)RazorInstrument.InstrumentSoundType.Harp;
                Utility.Monitor.WriteLine("instrument {0} unknown", ConsoleColor.Yellow, instrument);
            }
        }
        private void DoProcessPlay(Mobile m, string line)
        {
            m = m_playMobile ?? m;
            string song = line.Replace("[play", "", StringComparison.CurrentCultureIgnoreCase).Replace("say", "", StringComparison.CurrentCultureIgnoreCase).Replace("'", "").Trim();

            string[] notes = song.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string note in notes)
                m_playQueue.Enqueue(note);

            if (m_playQueue.Count > 0)
                Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(NotePumpTick), new object[] { m });

            // call our baby Razor to continue processing this script 
            Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), new object[] { m });
        }
        private void DoProcessSay(Mobile m, string line)
        {
            m = m_playMobile ?? m;
            line = line.Replace("say ", "", StringComparison.CurrentCultureIgnoreCase);
            line = line.Substring(1, line.Length - 2); // remove =>'xxxx'<= and this

            if (Source is Item item)
                item.PublicOverheadMessage(Network.MessageType.Regular, 0x32, true, line);
            else if (Source is Mobile sm)
                sm.Say(line);

            // this is the case when text is spoken via RSM or other RazorInstrument
            //  the other case when text is spoken via Razor (in PlayerMobile)
            Engines.MusicRecorder.Record(m, line);

            // call our baby Razor to continue processing this script 
            Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(BabyRazor), new object[] { m });
        }
        private void DoProcessWait(Mobile m, string line)
        {
            m = m_playMobile ?? m;
            double delay = 0;
            string wait;
            wait = line;
            wait = wait.Replace("wait", "").Trim();
            string resultString = wait_regex.Match(wait).Value;
            double.TryParse(resultString, out delay);

            // call our baby Razor to process the script after this WAIT
            Timer.DelayCall(TimeSpan.FromMilliseconds(delay), new TimerStateCallback(BabyRazor), new object[] { m });
        }
        private void NotePumpTick(object state)
        {
            double delay = 0.001;
            object[] aState = (object[])state;
            Mobile pm = aState[0] as Mobile;
            int notesQueued = (SystemMusicPlayer.MusicConfig[pm].PlayList != null) ? SystemMusicPlayer.MusicConfig[pm].PlayList.Count : 0;    // how many notes are currently queued
            int notesToPublish = Math.Min(SystemMusicPlayer.MaxQueueSize - notesQueued, m_playQueue.Count);                      // how many we should publish

            if (notesToPublish == 0 && m_playQueue.Count > 0)
            {   // still stuff left to play, but no room at the inn
                // Don't use the delay variable here - this is just a stay-alive / wait
                Timer.DelayCall(TimeSpan.FromSeconds(0.001), new TimerStateCallback(NotePumpTick), new object[] { pm });
                return;
            }
            else if (notesToPublish == 0)
                return; // we're done

            string playString = "";   // we only construct this for CommandEventArgs()
            for (int ix = 0; ix < notesToPublish; ix++)
            {
                string temp = m_playQueue.Dequeue();
                string[] chunks = temp.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (chunks.Length > 1)
                {   // the second chunk is the pause. 
                    //      Razor represents pauses in miliseconds,
                    //      so we will / 1000 for our time DelayCall timer that wants seconds as a double
                    double pause = 0;
                    if (double.TryParse(chunks[1], out pause))
                        delay = pause / 1000;

                    playString += chunks[0] + " ";
                    break;                          // we need to break here to process the pause
                }
                else
                    playString += temp + " ";
            }

            // call the music player 
            SystemMusicPlayer.Player(pm, playString);

            // this timer will incorporate the delay specified 'as a pause' in the song file, or 0.001 if none specified
            if (m_playQueue.Count > 0)
                Timer.DelayCall(TimeSpan.FromSeconds(delay), new TimerStateCallback(NotePumpTick), new object[] { pm });
            else return;    // breakpoint here if you like, just means we are done
        }
        private bool LoadSongFile(Mobile from)
        {
            from = m_playMobile ?? from;
            List<string> songLines = new List<string>();

            // we can read a song from either of memory of a file
            if (!string.IsNullOrEmpty(m_songMemory))
            {
                StringReader strReader = new StringReader(m_songMemory);
                while (true)
                {
                    string line = strReader.ReadLine();
                    if (line == null)
                        break;

                    songLines.Add(line);

                    if (line.ToLower().StartsWith("#"))
                        ParseDirectives(from, line);    // turn on/off player features
                }

                songLines.Reverse();
                foreach (string tmp in songLines)
                    m_songBuffer.Push(tmp);

                return true;
            }
            else if (m_songFile != null)
            {

                string filePath = Path.Combine(Core.DataDirectory, "song files", m_songFile);
                if (!File.Exists(filePath))
                    return false;
                try
                {
                    foreach (var line in File.ReadLines(filePath))
                    {
                        songLines.Add(line);

                        if (line.ToLower().StartsWith("#"))
                            ParseDirectives(from, line);    // turn on/off player features
                    }

                    songLines.Reverse();
                    foreach (string tmp in songLines)
                        m_songBuffer.Push(tmp);
                    return true;
                }
                catch
                {
                    from.SendMessage("Cannot load {0}", filePath);
                }
            }

            return false;
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 7;
            writer.Write(version); // version
            switch (version)
            {
                case 7:
                    {
                        writer.Write(m_concertMode);
                        goto case 6;
                    }
                case 6:
                    {
                        writer.Write(m_prefetch);
                        goto case 5;
                    }
                case 5:
                    {
                        writer.WriteEncodedInt(m_tempo);
                        goto case 4;
                    }
                case 4:
                    {
                        writer.Write(m_newTimer);
                        goto case 3;
                    }
                case 3:
                    {
                        writer.Write(m_preferredInstrument_obsolete);
                        goto case 2;
                    }
                case 2:
                    {
                        writer.Write(m_playItem);
                        writer.Write(m_instrument);
                        writer.Write(m_minMusicSkill_obsolete);
                        writer.Write(m_requiresBackpack);
                        writer.Write(m_musicChatter_doWeNeedThis);
                        writer.Write(m_checkMusicianship_obsolete);
                        writer.Write(m_ignoreDirectives_obsolete);
                        goto case 1;
                    }
                case 1:
                    {
                        writer.Write(m_author);
                        writer.Write(m_songName);
                        writer.Write(m_songBytes);
                        writer.Write(m_place_obsolete);
                        writer.Write(m_year_obsolete);
                        writer.Write(m_songFile);
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
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
                        m_preferredInstrument_obsolete = reader.ReadString();
                        goto case 2;
                    }
                case 2:
                    {
                        m_playItem = reader.ReadItem();
                        m_instrument = (BaseInstrument)reader.ReadItem();
                        m_minMusicSkill_obsolete = reader.ReadDouble();
                        m_requiresBackpack = reader.ReadBool();
                        m_musicChatter_doWeNeedThis = reader.ReadBool();
                        m_checkMusicianship_obsolete = reader.ReadBool();
                        m_ignoreDirectives_obsolete = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        m_author = reader.ReadString();
                        m_songName = reader.ReadString();
                        m_songBytes = reader.ReadString();
                        m_place_obsolete = reader.ReadInt();
                        m_year_obsolete = reader.ReadInt();
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
                this.SendSystemMessage(text);
        }
    }
}