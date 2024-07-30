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

/* Scripts\Engines\Music\CalypsoMusicPlayer.cs
 * Changelog
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
        private RazorInstrument m_Instrument;
        private Mobile m_MusicContextMobile;    // the mobile that will own the music context (required) (may be a temp/fake mobile)
        private Mobile m_PlayerMobile;          // the optional mobile to check for distance and other checks.
        private FinishCallback m_AllDone;
        private int m_range_exit = 15;

        public RazorInstrument Instrument { get { return m_Instrument; } }
        public int RangeExit { get { return m_range_exit; } set { m_range_exit = value; } }
        public Mobile MusicContextMobile { get { return m_MusicContextMobile; } }
        public Mobile PlayerMobile { get { return m_PlayerMobile; } set { m_PlayerMobile = value; } }
        public Mobile GetStatusMobile { get { return m_PlayerMobile ?? m_MusicContextMobile; } }
        public FinishCallback AllDone { get { return m_AllDone; } set { m_AllDone = value; } }
        private static Dictionary<Item, object> PlayersNearby = new();  // we don't currently us the Value field.
        private static Item PlayersInRange(Item playItem, int range)
        {
            if (playItem == null) { return null; }
            foreach (var player in PlayersNearby)
                if (playItem.Map != Map.Internal && playItem.Map == player.Key.Map && playItem.GetDistanceToSqrt(player.Key) < range)
                    if (playItem != player.Key)
                        return player.Key;

            return null;
        }
        public bool CanPlay(Item playItem)
        {
            bool playing = Playing;
            Item playerNearby = PlayersInRange(playItem, range: 25);    // 25 is approximately the distance between the two furthest music devices at WBB
            bool isPlayerNearby = playerNearby != null;
            if (Core.Debug && isPlayerNearby && playItem != null)
                Utility.SendSystemMessage(playItem, second_timeout: 5.0,
                    string.Format("Playback for {0} blocked because {1} is playing at {2}", playItem, playerNearby, playerNearby.Location));
            return !playing && !isPlayerNearby;
        }
        public bool Playing
        {
            get
            {
                bool mobile = m_MusicContextMobile != null;
                bool instrument_playing = mobile && m_Instrument is RazorInstrument ri && ri.IsThisDevicePlaying(m_MusicContextMobile);
                return mobile && instrument_playing;
            }
        }

        public CalypsoMusicPlayer()
        {
        }

        public bool Play(Mobile from, Item playItem, object song)
        {
            if (song is string songFile)
            {   // notes stored in 'song file'
                m_Instrument = new RazorInstrument();

                m_MusicContextMobile = from;

                m_Instrument.AllDone = new RazorInstrument.FinishCallback(EndMusic);                                 // establishes NextMusicTime
                m_Instrument.CheckSourceStatus = new RazorInstrument.CheckSourceStatusCallback(CheckSourceStatus);   // make sure the mobile is still around

                m_Instrument.SongFile = songFile;   // the notes
                m_Instrument.MinMusicSkill = 0;
                m_Instrument.PlayItem = playItem;
                m_Instrument.RequiresBackpack = false;
                //m_Instrument.MusicChatter_doWeNeedThis = false;
                m_Instrument.StartMusic(from, m_Instrument);
                if (playItem != null && !PlayersNearby.ContainsKey(playItem))
                    PlayersNearby.Add(playItem, m_Instrument);
            }
            if (song is RolledUpSheetMusic rsm)
            {   // notes stored in 'rolled up sheet music'
                m_Instrument = (rsm.Instrument = rsm) as RazorInstrument;

                m_MusicContextMobile = from;

                m_Instrument.AllDone = new RazorInstrument.FinishCallback(EndMusic);                                 // establishes NextMusicTime
                m_Instrument.CheckSourceStatus = new RazorInstrument.CheckSourceStatusCallback(CheckSourceStatus);   // make sure the mobile is still around

                m_Instrument.SongMemory = rsm.Composition;   // the notes
                m_Instrument.MinMusicSkill = 0;
                m_Instrument.PlayItem = playItem;
                m_Instrument.RequiresBackpack = false;
                //m_Instrument.MusicChatter_doWeNeedThis = false;
                m_Instrument.StartMusic(from, m_Instrument);
                if (playItem != null && !PlayersNearby.ContainsKey(playItem))
                    PlayersNearby.Add(playItem, m_Instrument);
            }
            else
                return false;

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
                    result &= playItem.Map == m.Map && playItem.GetDistanceToSqrt(m) < m_range_exit;

            return result;
        }
        public void CancelPlayback(Mobile from, bool say = true)
        {
            if (m_Instrument != null)
                m_Instrument.CancelPlayback(from, say);
            EndMusic();
        }
        public void EndMusic()
        {
            // remove this player from our list of nearby players
            if (m_Instrument != null)
                if (m_Instrument.PlayItem != null && PlayersNearby.ContainsKey(m_Instrument.PlayItem))
                    PlayersNearby.Remove(m_Instrument.PlayItem);

            // we only have a temp RZR instrument when we are playing .rzr files
            //  otherwise we are playing rolled up sheet music (RSM) and we don't delete those
            if (m_Instrument != null && m_Instrument is not RolledUpSheetMusic)
            {
                m_Instrument.Delete();
                m_Instrument = null;
            }

            // tell the caller we're done
            if (m_AllDone != null)
                m_AllDone(m_MusicContextMobile);


            m_MusicContextMobile = null;
            m_PlayerMobile = null;
        }
        public class Metadata
        {
            private string m_song;      // not a directive
            private bool m_newTimer;
            private int m_tempo;
            private bool m_prefetch;
            private string m_instrument;
            private bool m_chatter;
            private int m_volume;
            private bool m_concertmode;
            public string Song { get { return m_song; } }
            public bool NewTimer { get { return m_newTimer; } }
            public int Tempo { get { return m_tempo; } }
            public bool Prefetch { get { return m_prefetch; } }
            public string Instrument { get { return m_instrument; } }
            public bool Chatter { get { return m_chatter; } }
            public int Volume { get { return m_volume; } }
            public bool ConcertMode { get { return m_concertmode; } }
            public Metadata(string song, bool newTimer, int tempo, bool prefetch, string instrument, bool chatter, int volume, bool concertmode)
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
        public static Metadata GetMetaData(object song)
        {
            Metadata data = null;

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
            int needed = 8;
            int have = 0;
            if (song != null)
            {
                try
                {
                    if (song is string && Utility.IsUOMusic(song as string))
                    {
                        data = new Metadata(song as string, newTimer, tempo, prefetch, instrument, chatter, volume, concertmode);
                    }
                    else if (song is string razorFile)
                    {
                        if (razorFile.EndsWith(".rzr") && File.Exists(Path.Combine(Core.DataDirectory, "song files", razorFile)))
                        {
                            razorFile = Path.Combine(Core.DataDirectory, "song files", razorFile);
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
                            rsm.MusicChatter_doWeNeedThis, volume: volume, rsm.ConcertMode);
                    }
                }
                catch {; }
            }

            return data;
        }
    }
}