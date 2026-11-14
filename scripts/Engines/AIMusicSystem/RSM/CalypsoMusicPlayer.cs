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

/* Scripts\Engines\AIMusicSystem\RSM\CalypsoMusicPlayer.cs
 * Changelog
 *  11/6/26, Adam (2026 modernization, 10/25/26 to 11/6/26)
 *      The playback the Calypso devices (MusicController, MusicMotionController, BaseAddon, TrophyAddon) each
 *      duplicated lives here: the play lock, the "playing elsewhere" check, starting and cancelling a performance. A
 *      device plays on its own session (no temporary mobile), released when it ends, and follows its performance's
 *      Completed, raised once: a double-click during a pause cancels instead of starting a second performance, and an
 *      earlier performance's end never touches a newer one. GetMetaData no longer leaves orphaned instruments. A new
 *      device is ready to play at once (it was ready only from the next clock tick). The temporary playback mobile's
 *      release is gone: no device has made one since devices got their own sessions.
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: staff messages. AimsUtil: GetDistanceToSqrt and IsUOMusic.
 *  12/26/2023, Adam
 *		Initial creation.
 */


using Server.Items;
using Server.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Server.Engines
{
    public delegate void FinishCallback(Mobile from);

    public class CalypsoMusicPlayer
    {
        private RazorInstrument? m_Instrument;
        private Mobile? m_MusicContextMobile;    // the mobile whose music context plays (null: a music device's own session, m_Session)
        private Mobile? m_PlayerMobile;          // the optional mobile to check for distance and other checks.
        private FinishCallback m_AllDone;
        private int m_range_exit = 15;
        private Commands.MusicContext? m_Session;   // a music device's own session for the current performance (no mobile)
        private RazorInstrument.Performance? m_Performance;     // the performance in progress (null: none)
        private RazorInstrument.Performance? m_Latest;          // the most recent performance: its completion unlocks the device

        public RazorInstrument? Instrument { get { return m_Instrument; } }
        public int RangeExit { get { return m_range_exit; } set { m_range_exit = value; } }
        public Mobile? MusicContextMobile { get { return m_MusicContextMobile; } }
        public Mobile? PlayerMobile { get { return m_PlayerMobile; } set { m_PlayerMobile = value; } }
        public Mobile GetStatusMobile { get { return m_PlayerMobile ?? m_MusicContextMobile; } }
        public FinishCallback AllDone { get { return m_AllDone; } set { m_AllDone = value; } }
        private static Dictionary<Item, object> PlayersNearby = new();  // we don't currently us the Value field.
        private static Item? PlayersInRange(Item playItem, int range)
        {
            if (playItem == null) { return null; }
            foreach (var player in PlayersNearby)
                if (playItem.Map != Map.Internal && playItem.Map == player.Key.Map && AimsUtil.GetDistanceToSqrt(playItem, player.Key) < range)
                    if (playItem != player.Key)
                        return player.Key;

            return null;
        }
        public bool CanPlay(Item playItem)
        {
            bool playing = Playing;
            Item? playerNearby = PlayersInRange(playItem, range: 25);    // 25 is approximately the distance between the two furthest music devices at WBB
            bool isPlayerNearby = playerNearby != null;
            if (Core.Debug && isPlayerNearby && playItem != null)
                AimsHost.Current.SendSystemMessage(playItem, second_timeout: 5.0,
                    string.Format("Playback for {0} blocked because {1} is playing at {2}", playItem, playerNearby, playerNearby.Location));
            return !playing && !isPlayerNearby;
        }
        // a performance is in progress: from Play until it ends (completed, cancelled, or its status check failed), pauses
        //  included; the drain after a cancel or a failed check is not playing (the play lock waits for it)
        public bool Playing { get { return m_Performance != null && m_Performance.InProgress; } }

        public CalypsoMusicPlayer()
        {
            m_NextMusicTime.Start(-1);              // we can play now (Triggered is strictly after the timeout: Start(0) is ready only from the next tick)
        }

        #region Device playback
        // Shared by the Calypso music devices. The play lock is stopped while a performance runs and restarted with the
        //  device's delay when the most recent performance completes (see OnCompleted; after a cancel, once its last
        //  notes have drained).
        private readonly AimsUtil.LocalTimer m_NextMusicTime = new AimsUtil.LocalTimer();
        private Func<double>? m_DelayBetween;   // milliseconds, read when the performance ends
        public AimsUtil.LocalTimer NextMusicTime { get { return m_NextMusicTime; } }
        public bool IsPlayLocked { get { return !m_NextMusicTime.Running; } }
        public bool ReadyToPlay { get { return m_NextMusicTime.Triggered && !IsPlayLocked; } }
        // is the user playing music elsewhere?
        public bool UserBlocked
        {
            get
            {
                if (m_MusicContextMobile != null)
                    return Commands.SystemMusicPlayer.IsUserBlocked(m_MusicContextMobile);
                return m_Session != null && m_Session.Playing;
            }
        }
        public void StartPerformance(Mobile player, Item device, object song, int rangeExit, Func<double> delayBetween)
        {
            m_DelayBetween = delayBetween;
            // the player is the mobile we check for distance etc.; the music plays on the device's own session
            m_PlayerMobile = player;
            m_range_exit = rangeExit;                   // how far the mobile can be from the device
            Play(null, device, song);
            if (Playing)
                m_NextMusicTime.Stop();                 // just to block us. See OnCompleted
        }
        public void CancelPerformance()
        {
            RazorInstrument.Performance? performance = m_Performance;
            m_Performance = null;                       // the device is no longer playing
            m_NextMusicTime.Stop();                     // the performance's completion (after the drain) unlocks us
            performance?.Cancel();
        }
        #endregion Device playback

        // from: the mobile whose music context plays, or null for a music device's own session (no mobile)
        public bool Play(Mobile? from, Item playItem, object song)
        {
            RazorInstrument instrument;
            if (song is string songFile)
            {   // notes stored in 'song file': the performance's own instrument
                instrument = new RazorInstrument();
                instrument.SongFile = songFile;         // the notes
            }
            else if (song is RolledUpSheetMusic rsm)
            {   // notes stored in 'rolled up sheet music'
                instrument = rsm;
                rsm.Instrument = rsm;
                instrument.SongMemory = rsm.Composition;   // the notes
            }
            else
                return false;

            m_Instrument = instrument;
            m_MusicContextMobile = from;
            m_Session = null;

            instrument.CheckSourceStatus = new RazorInstrument.CheckSourceStatusCallback(CheckSourceStatus);   // make sure the mobile is still around
            instrument.PlayItem = playItem;
            instrument.RequiresBackpack = false;

            RazorInstrument.Performance? performance = from != null ? instrument.StartMusic(from, instrument) : instrument.StartDeviceMusic(instrument);
            if (performance == null)
            {   // nothing started
                EndInstrument(instrument);
                return true;
            }

            m_Performance = m_Latest = performance;
            Commands.MusicContext? session = performance.Unowned ? performance.Session : null;
            m_Session = session;
            performance.Completed += (p, reason) => OnCompleted(p, from, instrument, playItem, session);

            if (playItem != null && !PlayersNearby.ContainsKey(playItem))
                PlayersNearby.Add(playItem, instrument);

            return true;
        }

        [Flags]
        public enum CheckMobileProps
        {
            None = 0x0,
            Player = 0x01,      // must be a player mobile
            Connected = 0x02,   // must be connected
        }
        private CheckMobileProps m_check_mobile_props = CheckMobileProps.Player | CheckMobileProps.Connected;
        public CheckMobileProps MobileProps { get { return m_check_mobile_props; } set { m_check_mobile_props = value; } }
        public bool CheckSourceStatus(Mobile m, IEntity source)
        {   // called back from RazorInstrument
            return CheckMobile(m, source);
        }
        public bool CheckMobile(Mobile m, IEntity source)
        {
            // sanity
            if (m != m_MusicContextMobile && m != m_PlayerMobile)
                ;// debug break

            // get the preferred mobile for status checking, i.e., the one that triggered the music device
            m = GetStatusMobile;

            //System.Diagnostics.Debug.Assert(m != null, "GetStatusMobile returned null");
            // we don't serialize, so after a server restart, all will be null
            if (m == null)
                return false;

            #region For future use, currently always defaults to CheckMobileProps.Player | CheckMobileProps.Connected;
            bool mobile_ok = false;
            if (((m_check_mobile_props & CheckMobileProps.Player) != 0))
                mobile_ok = m is Mobiles.PlayerMobile;  // must be a player
            else
                mobile_ok = true;                       // any mobile is ok
            bool connect_ok = false;
            if (((m_check_mobile_props & CheckMobileProps.Connected) != 0))
                connect_ok = m.NetState != null && m.NetState.Mobile != null && m.NetState.Running == true; // must be connected
            else
                connect_ok = true;                      // no need to be connected
            #endregion For future use, currently always defaults to CheckMobileProps.Player | CheckMobileProps.Connected;

            // is the mobile still available?
            bool result = (m != null && mobile_ok && connect_ok);
            result &= source != null && !source.Deleted;            // mostly for sanity. Maybe a GM deleted this source during playback
            if (result && source is Item playItem)                  // if a mobile, no range checking - the music follows the mobile
                // -1 means no checking, i.e., player keeps playing
                if (m_range_exit > 0 /*&& source is not BaseAddon && source is not MusicMotionController*/)
                    // are we sufficiently close to our PlayItem?
                    result &= playItem.Map == m.Map && AimsUtil.GetDistanceToSqrt(playItem, m) < m_range_exit;

            return result;
        }
        // A performance of this player has ended (Completed: raised once, after the drain of its last notes when it was
        //  cancelled). performer, instrument, playItem and session are the ones it was started with. Only the most
        //  recent performance frees the device (nearby list, play lock, mobiles); an earlier one never touches a newer
        //  one, it only releases what was its own.
        private void OnCompleted(RazorInstrument.Performance performance, Mobile? performer, RazorInstrument instrument, Item playItem,
            Commands.MusicContext? session)
        {
            if (ReferenceEquals(performance, m_Performance))
                m_Performance = null;

            if (ReferenceEquals(performance, m_Latest) && m_Performance == null)
            {
                m_Latest = null;

                // remove this player from our list of nearby players
                if (playItem != null && PlayersNearby.ContainsKey(playItem))
                    PlayersNearby.Remove(playItem);

                // device playback: how long we must wait before we play again
                if (m_DelayBetween != null)
                    m_NextMusicTime.Start(m_DelayBetween());

                if (ReferenceEquals(m_Instrument, instrument) && instrument is not RolledUpSheetMusic)
                    m_Instrument = null;
                if (ReferenceEquals(m_Session, session))
                    m_Session = null;
                m_MusicContextMobile = null;
                m_PlayerMobile = null;
            }

            EndInstrument(instrument);

            // tell the caller we're done
            if (m_AllDone != null)
                m_AllDone(performer);

            ReleaseDeviceSession(session);
        }
        // we only have a temp RZR instrument when we are playing .rzr files
        //  otherwise we are playing rolled up sheet music (RSM) and we don't delete those
        private static void EndInstrument(RazorInstrument instrument)
        {
            if (instrument is not RolledUpSheetMusic && !instrument.Deleted)
                instrument.Delete();
        }
        // The device's own session exists only for its performance: release it, so the engine's timers and the
        //  instrument's callbacks for it stop (SystemMusicPlayer.IsCurrent). Deferred: the completion can be raised from
        //  inside the session's own timer tick.
        private static void ReleaseDeviceSession(Commands.MusicContext? session)
        {
            if (session == null)
                return;

            Timer.DelayCall(TimeSpan.Zero, () => session.Released = true);
        }
        public class Metadata
        {
            private string? m_song;      // not a directive
            private bool m_newTimer;
            private int m_tempo;
            private bool m_prefetch;
            private string m_instrument;
            private bool m_chatter;
            private int m_volume;
            private bool m_concertmode;
            public string? Song { get { return m_song; } }
            public bool NewTimer { get { return m_newTimer; } }
            public int Tempo { get { return m_tempo; } }
            public bool Prefetch { get { return m_prefetch; } }
            public string Instrument { get { return m_instrument; } }
            public bool Chatter { get { return m_chatter; } }
            public int Volume { get { return m_volume; } }
            public bool ConcertMode { get { return m_concertmode; } }
            public Metadata(string? song, bool newTimer, int tempo, bool prefetch, string instrument, bool chatter, int volume, bool concertmode)
            {
                m_song = song;
                m_newTimer = newTimer;
                m_tempo = tempo;
                m_prefetch = prefetch;
                m_instrument = instrument;
                m_chatter = chatter;
                m_volume = volume;
                m_concertmode = concertmode;
            }
        }
        public static Metadata GetMetaData(object? song)
        {
            Metadata? data = null;

            // initialize with default values
            Commands.MusicContext default_mc = new Commands.MusicContext();
            string songName = "unknown";
            bool newTimer = default_mc.NewTimer;
            int tempo = default_mc.Tempo;
            bool prefetch = default_mc.Prefetch;
            string instrument = default_mc.Instrument.ToString();
            bool chatter = default_mc.MusicChatter;
            int volume = default_mc.Volume;
            bool concertmode = default_mc.ConcertMode;
            default_mc.Finish();    // only its default values are needed (a context holds no items since Phase 4 Step 2a)
            int needed = 8;
            int have = 0;
            if (song != null)
            {
                try
                {
                    if (song is string && AimsUtil.IsUOMusic(song as string))
                    {
                        data = new Metadata(song as string, newTimer, tempo, prefetch, instrument, chatter, volume, concertmode);
                    }
                    else if (song is string razorFile)
                    {
                        if (razorFile.EndsWith(".rzr") && File.Exists(Path.Combine(AimsUtil.DataDirectory, "song files", razorFile)))
                        {
                            razorFile = Path.Combine(AimsUtil.DataDirectory, "song files", razorFile);
                            foreach (var line in File.ReadLines(razorFile))
                            {
                                string pattern = @"(\w+)\s*\:\s*(.*)";
                                MatchCollection matches = Regex.Matches(line, pattern);
                                if (matches.Count != 1) continue;
                                GroupCollection groups = matches[0].Groups;
                                string left = groups[1].Value;
                                string right = groups[2].Value;

                                if (left.Equals("song", StringComparison.OrdinalIgnoreCase))
                                {
                                    have++;
                                    songName = right;
                                }
                                else if (left.Equals("newtimer", StringComparison.OrdinalIgnoreCase))
                                {
                                    have++;
                                    newTimer = bool.Parse(right);
                                }
                                else if (left.Equals("tempo", StringComparison.OrdinalIgnoreCase))
                                {
                                    have++;
                                    tempo = int.Parse(right);
                                }
                                else if (left.Equals("prefetch", StringComparison.OrdinalIgnoreCase))
                                {
                                    have++;
                                    prefetch = bool.Parse(right);
                                }
                                else if (left.Equals("instrument", StringComparison.OrdinalIgnoreCase))
                                {
                                    have++;
                                    instrument = right;
                                }
                                else if (left.Equals("chatter", StringComparison.OrdinalIgnoreCase))
                                {
                                    have++;
                                    chatter = bool.Parse(right);
                                }
                                else if (left.Equals("volume", StringComparison.OrdinalIgnoreCase))
                                {
                                    have++;
                                    volume = int.Parse(right);
                                }
                                else if (left.Equals("concertmode", StringComparison.OrdinalIgnoreCase))
                                {
                                    have++;
                                    concertmode = bool.Parse(right);
                                }

                                if (have == needed)
                                    break;
                            }
                            data = new Metadata(songName, newTimer, tempo, prefetch, instrument, chatter, volume, concertmode);
                        }
                    }
                    else if (song is RolledUpSheetMusic rsm)
                    {   // (m_instrument as RazorInstrument).GetInstrumentType().ToString()
                        data = new Metadata(rsm.SongName, rsm.NewTimer, rsm.Tempo, rsm.Prefetch, (rsm.Instrument as RazorInstrument).GetInstrumentType().ToString(),
                            chatter: false, volume: volume, rsm.ConcertMode);     // a sheet carries no chatter setting (the session has it)
                    }
                }
                catch {; }
            }

            return data;
        }
    }
}