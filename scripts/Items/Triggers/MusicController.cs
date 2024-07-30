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

/* Scripts\Items\Triggers\MusicController.cs
 *	ChangeLog :
 *	4/20/2024, Adam
 *	    For UOMusic:
 *	        1. support all of range: Player, Party, Area, and Region
 *	        2. Add Looping support
 *	3/22/2024, Adam
 *	    Created.
 *	    Based off the MusicMotionController (12/13/2023, Adam)
 *		Purpose:
 *		    Allows a music to be played when triggered
 */

using Server.Commands;
using Server.Engines;
using Server.Engines.PartySystem;
using Server.Misc;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Items.Triggers
{
    [NoSort]
    public class MusicController : Item, ITriggerable
    {
        [Constructable]
        public MusicController()
            : base(0x1B72)
        {
            Running = true;
            Movable = false;
            Visible = false;
            Name = "music controller";
            NextMusicTime.Start(0);                 // we can play now
            m_MusicPlayer.AllDone = EndMusic;
        }

        public MusicController(Serial serial)
            : base(serial)
        {
        }
        private CalypsoMusicPlayer m_MusicPlayer = new CalypsoMusicPlayer();
        private Utility.LocalTimer NextMusicTime = new Utility.LocalTimer();
        public override Item Dupe(int amount)
        {
            MusicController new_item = new MusicController();
            if (GetRSM() != null)
            {
                // make a copy
                RolledUpSheetMusic dupe = Utility.Dupe(GetRSM()) as RolledUpSheetMusic;
                new_item.AddItem(dupe);
            }
            return base.Dupe(new_item, amount);
        }
        public void OnTrigger(Mobile from)
        {
            if (from == null)
                return;

            object song = GetMusic();
            if (song != null)
            {
                if (song is string && Utility.IsUOMusic(song as string))
                {
                    if (!IsUOPlayTimerRunning())
                    {
                        PlayUOMusic(from);
                        StartUOPlayTimer(from);
                    }
                    else
                        // CanTrigger should have blocked this
                        System.Diagnostics.Debug.Assert(false);
                }
                else
                {
                    // some music devices use a fake mobile for music context. This will be the mobile we check for distance etc.
                    m_MusicPlayer.PlayerMobile = from;
                    m_MusicPlayer.RangeExit = m_range_exit;         // how far the mobile can be from this
                                                                    //m_MusicPlayer.Play(m, this, song);
                    m_MusicPlayer.Play(Utility.MakeTempMobile(), this, song);
                    NextMusicTime.Stop();                           // just to block us. See EndMusic
                }
            }
        }
        public bool IsPlaying(Mobile from)
        {
            return SystemMusicPlayer.MusicConfig.ContainsKey(from) && SystemMusicPlayer.MusicConfig[from].Playing/* .PlayList.Count > 0*/;
        }
        private bool PlayMobile(Mobile m)
        {
            if (m is PlayerMobile pm)
            {
                if (pm.Hidden)
                {
                    if (pm.AccessLevel > AccessLevel.Player)
                        Utility.SendSystemMessage(this, second_timeout: 60.0, "Hidden staff do not trigger music playback");

                    return false;
                }

                return true;
            }
            return false;
        }
        private Timer m_UOPlayTimer = null;
        private bool IsUOPlayTimerRunning()
        {
            return m_UOPlayTimer != null;
        }
        private static double UOMusicDuration(MusicName id)
        {
            if (id != MusicName.Invalid)
                return Utility.UOMusicInfo[(int)id].Item1;
            return 0;
        }
        private static bool UOMusicCanLoop(MusicName id)
        {
            if (id != MusicName.Invalid)
                return Utility.UOMusicInfo[(int)id].Item2;
            return false;
        }
        private void StartUOPlayTimer(Mobile from)
        {
            TimeSpan duration = TimeSpan.FromMilliseconds(UOMusicDuration(m_UOMusic) + m_UOMusicLatency.TotalMilliseconds);
            DebugLabelTo(string.Format("Start play timer {0}", duration));
            NextMusicTime.Stop();                           // just to block us. See StopUOPlayTimer
            m_UOPlayTimer = Timer.DelayCall(duration, new TimerStateCallback(StopUOPlayTimer), from);
        }
        private void StopUOPlayTimer(object o)
        {
            try
            {
                Mobile from = o as Mobile;
                if (m_UOPlayTimer != null && m_UOPlayTimer.Running)
                    // maybe we got a manual 'double-click' to stop
                    m_UOPlayTimer.Stop();
                m_UOPlayTimer = null;
                if (!UOMusicLoop)
                    // The client knows how to naturally loop *certain* tracks. This property simply allows the client to loop those tracks without a manual stop from us.                  
                    StopUOMusic(from);
                NextMusicTime.Start(m_time_between);    // how long we must wait before we play again
                                                        //NextMusicTime.Start(0); // okay to play now
                DebugLabelTo(string.Format("Stop play timer"));
            }
            catch
            {
                ;/* MusicController was deleted? */
            }
        }
        private void UOMusicReset(double new_value)
        {   // We need the mobile to stop the music (for them.) We therefore ask them to double-click the controller.
            this.SendSystemMessage("Please double click the controller to reset the UOMusic.");
        }
        public override void Usage(Mobile from)
        {
            from.SendMessage("RazorFile: The Razor file you would like to play.");
            from.SendMessage("SheetMusic: The Rolled Up Sheet music you would like to play. Target the RSM you would like to use.");
            from.SendMessage("UOMusic: The classic UO music you would like to play.");
            from.SendMessage("UOMMusicLoop: False cuts off after the first play. True allows the client to loop if it can.");
            from.SendMessage("UOMusicRange: Player only, Player's Party, 15 tile Area, and entire Region.");
            from.SendMessage("UOMusicLatency: Add additional time to 'Duration' to account for the delay in starting to play.");
            from.SendMessage("DelayBetween: How long the controller will wait before playing the next song.");
            from.SendMessage("Duration: Only known for UOMusic.");
            from.SendMessage("RangeExit: When the player gets this far away, the music stops. -1 for no range checking.");
            from.SendMessage("Running: Enable/disable this controller.");
        }
        public override void OnDoubleClick(Mobile from)
        {
            try
            {
                object music = null;
                if ((music = GetMusic()) != null)
                {
                    if (music is string && Utility.IsUOMusic(music as string))
                    {
                        if (UOMusicLoop && UOMusicLoopPlaying)
                        {
                            from.Emote("*you cancel what you were playing*");
                            UOMusicLoop = !UOMusicLoop; // turn off
                            StopUOPlayTimer(from);
                            UOMusicLoop = !UOMusicLoop; // turn on
                            UOMusicLoopPlaying = false;
                            NextMusicTime.Start(0);     // okay to play now
                        }
                        else if (IsUOPlayTimerRunning())
                        {
                            from.Emote("*you cancel what you were playing*");
                            StopUOPlayTimer(from);
                            NextMusicTime.Start(0); // okay to play now
                        }
                        else
                        {
                            PlayUOMusic(from);
                            StartUOPlayTimer(from);
                        }
                    }
                    else
                    {
                        // check if this user is playing music elsewhere
                        bool blocked = SystemMusicPlayer.IsUserBlocked(m_MusicPlayer.MusicContextMobile);   // should always be false for these 'temp mobiles'
                        if (m_MusicPlayer.Playing)
                        {
                            //m_MusicPlayer.CancelPlayback(from);
                            m_MusicPlayer.CancelPlayback(m_MusicPlayer.MusicContextMobile);
                            if (m_MusicPlayer.MusicContextMobile != from)
                                from.Emote("*you cancel what you were playing*");
                            NextMusicTime.Stop();
                        }
                        else if (!blocked)
                        {
                            // some music devices use a fake mobile for music context. This will be the mobile we check for distance etc.
                            m_MusicPlayer.PlayerMobile = from;
                            m_MusicPlayer.RangeExit = m_range_exit;         // how far the mobile can be from this
                                                                            //m_MusicPlayer.Play(from, this, music);
                            m_MusicPlayer.Play(Utility.MakeTempMobile(), this, music);
                            NextMusicTime.Stop();                           // just to block us. See EndMusic
                        }
                        else if (Core.Debug && blocked)
                            Utility.SendSystemMessage(from, 5, "Debug message: music not started because this player has initiated music elsewhere.");
                    }
                }
                else
                    SendSystemMessage("No associated music");
            }
            catch
            {
                ;
            }
        }
        public void EndMusic(Mobile from)
        {   // called back from RazorInstrument
            NextMusicTime.Start(m_time_between);
            return;
        }
        private bool IsPlayLocked()
        {
            return !NextMusicTime.Running;
        }
        private bool UOMusicLoopPlaying = false;    // not serialized
        public void PlayUOMusic(Mobile primary)
        {
            List<Mobile> list = GetUOMAudience(primary);            // who is listening?
            list = ClipUOMAudience(ClipMode.Play, primary, list);   // who is not around?
            // stop everyone
            foreach (Mobile m in list)
            {
                if (m_UOMusic != MusicName.Invalid && m.NetState != null)
                {   // The Client ignores requests to play the same "music wave" twice. This is correct. Probably
                    //  to prevent region music from starting and stopping and restarting as you enter/exit the same region.
                    //  However, we override this behavior for our controller.
                    if (Utility.SoundCanvas.ContainsKey(m) && Utility.SoundCanvas[m] == m_UOMusic)
                        StopUOMusic(m);
                }
            }

            // start everyone
            foreach (Mobile m in list)
            {
                if (m_UOMusic != MusicName.Invalid && m.NetState != null)
                {
                    Utility.SoundCanvas[m] = m_UOMusic;
                    m.Send(Network.PlayMusic.GetInstance(m_UOMusic));
                }
            }

            UOMusicLoopPlaying = UOMusicLoop && list.Count > 0;
        }
        List<Mobile> GetUOMAudience(Mobile primary)
        {
            List<Mobile> list = new List<Mobile>();

            if (m_UOMusicRange == _UOMusicRange.Player)
            {
                list.Add(primary);
            }
            else if (m_UOMusicRange == _UOMusicRange.Party)
            {
                Party party = Engines.PartySystem.Party.Get(primary);

                if (party != null)
                {
                    for (int j = 0; j < party.Members.Count; ++j)
                    {
                        PartyMemberInfo info = party.Members[j] as PartyMemberInfo;

                        if (info != null && info.Mobile != null && !list.Contains(info.Mobile))
                            list.Add(info.Mobile);
                    }
                }
            }
            else if (m_UOMusicRange == _UOMusicRange.Area)
            {
                if (!this.Deleted && this.Map != null)
                {
                    IPooledEnumerable eable = this.Map.GetMobilesInRange(this.Location, 15);
                    foreach (Mobile m in eable)
                        if (m is PlayerMobile)
                            if (!list.Contains(m))
                                list.Add(m);
                    eable.Free();
                }
            }
            else if (m_UOMusicRange == _UOMusicRange.Region)
            {
                if (!this.Deleted && this.Map != null)
                {
                    Region rx = Region.Find(this.Location, this.Map);
                    if (rx != null && !rx.IsDefault)
                        foreach (Mobile m in rx.Players.Values)
                            if (!list.Contains(m))
                                list.Add(m);
                }
            }
            return list;
        }
        public void StopUOMusic(Mobile primary)
        {
            List<Mobile> list = GetUOMAudience(primary);            // who was listening?
            list = ClipUOMAudience(ClipMode.Stop, primary, list);   // handle a region/music change
            foreach (Mobile m in list)
            {
                if (m_UOMusic != MusicName.Invalid && m.NetState != null)
                {   // There is no way to simply stop the current music. Regions send this InvalidInstance, but that doesn't actually stop the music
                    //  However, ClassicUO respects music names 0-150. The UO music only goes to 50 (TokunoDungeon). If you pass ClassicUO a value greater than
                    //  50, yet less than 150, ClassicUO stops playback cleanly. (Their code supports this.)
                    m.Send(Network.PlayMusic.GetInstance((MusicName)100));  // <= anything > 50 && < 150 stops the music
                    m.Send(Network.PlayMusic.InvalidInstance);
                }
            }
            UOMusicLoopPlaying = false;
        }
        public enum ClipMode
        {
            Play,
            Stop,
        }
        List<Mobile> ClipUOMAudience(ClipMode mode, Mobile primary, List<Mobile> list)
        {   // Scenario: If a mobile is listening to UOMusic because of the music controller, and they recall away to say Vesper, and that music starts playing
            //  because of the Region change, we don't want to cancel their music.
            List<Mobile> new_list = new();
            if (mode == ClipMode.Play)
            {
                return list;    // punt until we gigure this out
                foreach (Mobile m in list)
                    if (m.Region == primary.Region)
                        new_list.Add(m);
            }
            else if (mode == ClipMode.Stop)
            {

                foreach (Mobile m in list)
                    if (Utility.SoundCanvas.ContainsKey(m) && Utility.SoundCanvas[m] == m_UOMusic)
                        new_list.Add(m);
            }

            return new_list;
        }
        private RolledUpSheetMusic GetRSM()
        {
            foreach (Item item in PeekItems)
                if (item is RolledUpSheetMusic rsm)
                    return rsm;
            return null;
        }
        private string GetRZR(RazorName razorFile)
        {
            if (razorFile == RazorName.Invalid)
                return null;

            string enum_name = Enum.GetName(typeof(RazorName), razorFile);
            if (string.IsNullOrEmpty(enum_name))
                return null;

            string name = Utility.SplitCamelCase(enum_name);
            name += ".rzr";

            // apply file mask
            string name_pattern = name.Replace(" ", "*");

            // does it exist?
            File.Exists(Path.Combine(Core.DataDirectory, "song files", name_pattern));
            return name;

            return null;
        }
        private string GetRZR()
        {
            // RazorFile
            if (RazorFile == RazorName.Invalid)
                return null;

            string enum_name = Enum.GetName(typeof(RazorName), RazorFile);
            if (string.IsNullOrEmpty(enum_name))
                return null;

            string name = Utility.SplitCamelCase(enum_name);
            name += ".rzr";

            // apply file mask
            string name_pattern = name.Replace(" ", "*");

            // does it exist?
            if (CheckFileExists(Path.Combine(Core.DataDirectory, "song files"), name_pattern))
                return name;

            return null;
        }
        private string GetUOMusic()
        {
            return UOMusic.ToString();
        }
        private static bool CheckFileExists(string path, string pattern)
        {
            return Directory.EnumerateFiles(path, pattern).Any();
        }
        private object GetMusic()
        {
            object music = GetRSM();
            if (music == null)
                music = GetRZR();
            if (music == null)
                music = GetUOMusic();
            return music;
        }
        private RazorName m_RazorFile = RazorName.Invalid;
        public enum RazorName
        {
            Invalid = -1,
            BareNecessities,
            BewareTheForestsMushrooms,
            BoomBoomBatBat,
            ConversationWithGwennoU7,
            DouceDameJolie,
            Drippy,
            DungeonStrut,
            HesAPirate,
            JingleBells,
            MortalKombatTheme,
            MusingsOnAMusicbox,
            TakeMeToTheRiver,
            TheDecisiveBattleFFVI,
            ToBeNamed,
            TotoAfrica,
            Tribute,
        }
        #region Properties

        [CommandProperty(AccessLevel.GameMaster)]
        public RazorName RazorFile
        {
            get { return m_RazorFile; }
            set
            {
                string musicObject = m_RazorFile == RazorName.Invalid ? null : m_RazorFile.ToString();

                if (m_RazorFile != value)
                {
                    if (value != RazorName.Invalid)
                    {
                        if (musicObject != null)
                            SendSystemMessage(string.Format("Disabling music {0}", musicObject));

                        CalypsoMusicPlayer.Metadata data = CalypsoMusicPlayer.GetMetaData(GetRZR(value));
                        SendSystemMessage(string.Format("Adding music {0}", data?.Song));
                    }
                    m_RazorFile = value;
                }

                MusicChanged(typeof(RazorName));
                InvalidateProperties();
            }
        }

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.Seer)]
        public virtual Item SheetMusic
        {
            get
            {
                foreach (Item item in PeekItems)
                    if (item is RolledUpSheetMusic rsm)
                        return rsm;
                return null;
            }
            set
            {
                Item musicObject = null;

                if (value != null && value is not RolledUpSheetMusic)
                {
                    this.SendSystemMessage("That is not rolled up sheet music");
                    return;
                }

                foreach (Item item in PeekItems)
                    if (item is RolledUpSheetMusic rsm)
                    {
                        musicObject = rsm;
                        break;
                    }

                if (musicObject != value)
                {
                    if (musicObject != null)
                    {
                        SendSystemMessage(string.Format("Deleting music {0}", musicObject));
                        RemoveItem(musicObject);
                        musicObject.Delete();
                    }

                    musicObject = value;

                    if (musicObject != null)
                    {
                        Item item = Utility.Dupe(musicObject);
                        item.SetItemBool(ItemBoolTable.StaffOwned, false);
                        CalypsoMusicPlayer.Metadata data = CalypsoMusicPlayer.GetMetaData(item);
                        SendSystemMessage(string.Format("Adding music {0}", data?.Song));
                        AddItem(item);
                    }
                }

                MusicChanged(typeof(RolledUpSheetMusic));
                InvalidateProperties();
            }
        }

        private MusicName m_UOMusic = MusicName.Invalid;
        [CommandProperty(AccessLevel.Seer)]
        public MusicName UOMusic
        {
            get { return m_UOMusic; }
            set
            {
                string musicObject = m_UOMusic == MusicName.Invalid ? null : m_UOMusic.ToString();

                if (m_UOMusic != value)
                {
                    if (value != MusicName.Invalid)
                    {
                        if (musicObject != null)
                            SendSystemMessage(string.Format("Disabling music {0}", musicObject));

                        CalypsoMusicPlayer.Metadata data = CalypsoMusicPlayer.GetMetaData(value.ToString());
                        SendSystemMessage(string.Format("Adding music {0}", data?.Song));
                    }
                    m_UOMusic = value;
                }

                MusicChanged(typeof(MusicName));
                InvalidateProperties();
            }
        }

        private bool m_UOMusicLoop = false;
        [CommandProperty(AccessLevel.Seer)]
        public bool UOMusicLoop
        {   // The client knows how to naturally loop *certain* tracks. This property simply allows the client to loop those tracks without a manual stop from us.
            get { return m_UOMusicLoop; }
            set
            {
                if (value == true && !UOMusicCanLoop(m_UOMusic))
                {
                    SendSystemMessage("The client does not support looping of this track");
                    m_UOMusicLoop = false;
                }
                else
                    m_UOMusicLoop = value;

                InvalidateProperties();
            }
        }

        public enum _UOMusicRange
        {
            Player,
            Party,
            Area,
            Region
        }
        private _UOMusicRange m_UOMusicRange = _UOMusicRange.Player;
        [CommandProperty(AccessLevel.Seer)]
        public _UOMusicRange UOMusicRange
        {
            get { return m_UOMusicRange; }
            set
            {
                Region rx = Region.Find(this.Location, this.Map);
                if (value == _UOMusicRange.Region && (rx == null || rx.IsDefault))
                    SendSystemMessage("You cannot address the entire default region.");
                else
                    m_UOMusicRange = value;
            }
        }
        TimeSpan m_UOMusicLatency = TimeSpan.Zero;
        [CommandProperty(AccessLevel.Seer)]
        public TimeSpan UOMusicLatency
        {
            get { return m_UOMusicLatency; }
            set { m_UOMusicLatency = value; }
        }
        [CommandProperty(AccessLevel.Seer)]
        public string NextPlay
        {
            get
            {
                object o = GetMusic();
                if (o != null)
                {
                    if (NextMusicTime.Triggered)
                        return "Now";
                    else if (!IsPlayLocked())
                        return string.Format("in {0} seconds", NextMusicTime.Remaining / 1000);
                }
#if false
                RolledUpSheetMusic rsm = GetRSM();
                if (rsm != null && !IsPlayLocked())
                {
                    if (NextMusicTime.Triggered)
                        return "Now";
                    else
                        return string.Format("in {0} seconds", NextMusicTime.Remaining / 1000);
                }
#endif
                return "Unknown at this time";
            }
        }

        [CommandProperty(AccessLevel.Seer)]
        public string Song
        {
            get
            {
                CalypsoMusicPlayer.Metadata data = CalypsoMusicPlayer.GetMetaData(GetMusic());
                return data?.Song;
            }
        }

        private double m_time_between = 120000;             // two minutes until the next playback - in milliseconds
        [CommandProperty(AccessLevel.Seer)]
        public TimeSpan DelayBetween
        {   // convert to milliseconds
            get { return TimeSpan.FromMilliseconds(m_time_between); }
            set { m_time_between = value.TotalMilliseconds; }
        }

        private double m_uo_music_cutoff = 300.0;           // five minutes default
        //[CommandProperty(AccessLevel.Seer)]
        //public double UOMusicCutoff
        //{
        //    get { return m_uo_music_cutoff; }
        //    set 
        //    {
        //        if (m_uo_music_cutoff != value)
        //            UOMusicReset(value);
        //        m_uo_music_cutoff = value; 
        //    }
        //}
        [CommandProperty(AccessLevel.Seer)]
        public string Duration
        {
            get
            {
                if (m_UOMusic != MusicName.Invalid && UOMusicDuration(m_UOMusic) != 0.0)
                    return TimeSpan.FromMilliseconds(UOMusicDuration(m_UOMusic)).ToString();
                return "??";
            }
        }

        // UOMusicDuration(m_UOMusic)

        // Handled by whoever called us
        //private int m_range_enter = 15;                      // I get this close to trigger the device
        //[CommandProperty(AccessLevel.Seer)]
        //public int RangeEnter
        //{
        //    get { return m_range_enter; }
        //    set { m_range_enter = value; }
        //}

        private int m_range_exit = -1;                       // I get this far away and the music stops. -1 means no range checking
        [CommandProperty(AccessLevel.Seer)]
        public int RangeExit
        {
            get
            {
                //this.SendSystemMessage("RangeExit: I get this far away and the music stops. -1 means no range checking");
                //this.SendSystemMessage("RangeExit: Currently disabled for MusicControllers");
                return m_range_exit;
            }
            set { m_range_exit = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Running { get => base.IsRunning; set => base.IsRunning = value; }
        #endregion Properties
        private void MusicChanged(Type t)
        {
            if (t == typeof(RolledUpSheetMusic))
            {
                if (GetRSM() != null)
                {
                    if (RazorFile != RazorName.Invalid)
                    {
                        SendSystemMessage(string.Format("Disabling RZR music {0}", GetRZR()));
                        RazorFile = RazorName.Invalid;
                    }

                    if (UOMusic != MusicName.Invalid)
                    {
                        SendSystemMessage(string.Format("Disabling UO music {0}", UOMusic));
                        UOMusic = MusicName.Invalid;
                    }
                }
            }
            else if (t == typeof(RazorName))
            {
                if (RazorFile != RazorName.Invalid)
                {
                    // delete the rolled up sheet music if it exists
                    RolledUpSheetMusic rsm = GetRSM();
                    if (rsm != null)
                    {
                        SendSystemMessage(string.Format("Deleting music {0}", rsm));
                        RemoveItem(rsm);
                        rsm.Delete();
                    }

                    if (UOMusic != MusicName.Invalid)
                    {
                        SendSystemMessage(string.Format("Disabling UO music {0}", UOMusic));
                        UOMusic = MusicName.Invalid;
                    }
                }
            }
            else if (t == typeof(MusicName))
            {
                if (UOMusic != MusicName.Invalid)
                {
                    // delete the rolled up sheet music if it exists
                    RolledUpSheetMusic rsm = GetRSM();
                    if (rsm != null)
                    {
                        SendSystemMessage(string.Format("Deleting music {0}", rsm));
                        RemoveItem(rsm);
                        rsm.Delete();
                    }

                    // unlink RZR music if it exists
                    if (RazorFile != RazorName.Invalid)
                    {
                        SendSystemMessage(string.Format("Disabling RZR music {0}", GetRZR()));
                        RazorFile = RazorName.Invalid;
                    }
                }
            }

            if (t == typeof(MusicName))
            {
                if (UOMusic != MusicName.Invalid && UOMusicCanLoop(UOMusic))
                    UOMusicLoop = true;
                else
                    UOMusicLoop = false;
            }
            else
                UOMusicLoop = false;
        }
        #region ITriggerable

        bool ITriggerable.CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            if (Running)
            {
                if (GetRSM() != null || GetRZR() != null)
                {
                    // CanPlay blocks if: the player is already playing, OR another player nearby (15 tiles) is playing
                    bool all_good = PlayMobile(from) && NextMusicTime.Triggered && !IsPlayLocked() /*&& from.InRange(this, m_range_enter)*/ && m_MusicPlayer.CanPlay(this);
                    // check if this user is playing music elsewhere
                    bool blocked = SystemMusicPlayer.IsUserBlocked(m_MusicPlayer.MusicContextMobile);
                    if (all_good && !blocked)
                    {
                        DebugLabelTo("CanTrigger: true");
                        return true;
                    }
                    else if (Core.Debug && all_good && blocked)
                    {
                        Utility.SendSystemMessage(from, 5, "Debug message: music not started because this player has initiated music elsewhere.");
                        DebugLabelTo("CanTrigger: false (blocked)");
                        return false;
                    }
                }
                else if (GetUOMusic() != null)
                {
                    if (!IsUOPlayTimerRunning())
                    {
                        DebugLabelTo("CanTrigger: true");
                        return true;
                    }
                    else
                    {
                        DebugLabelTo("(busy)");
                        return false;
                    }
                }
                else
                {
                    DebugLabelTo("CanTrigger: false (no music)");
                    return false;
                }
            }

            DebugLabelTo("CanTrigger: false (controller not running)");
            return false;
        }

        void ITriggerable.OnTrigger(Mobile from)
        {
            OnTrigger(from);
        }

        #endregion
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)4);

            // version 4
            writer.Write(m_UOMusicLatency);

            // version 3
            writer.Write((int)m_UOMusicRange);

            // version 2
            writer.Write(m_UOMusicLoop);

            // version 1
            writer.WriteEncodedInt((int)m_RazorFile);
            writer.WriteEncodedInt((int)m_UOMusic);
            //writer.WriteEncodedInt(m_range_enter);
            writer.WriteEncodedInt(m_range_exit);
            writer.Write(m_time_between);
            //writer.Write(m_uo_music_cutoff);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 4:
                    {
                        m_UOMusicLatency = reader.ReadTimeSpan();
                        goto case 3;
                    }
                case 3:
                    {
                        m_UOMusicRange = (_UOMusicRange)reader.ReadInt();
                        goto case 2;
                    }
                case 2:
                    {
                        m_UOMusicLoop = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        m_RazorFile = (RazorName)reader.ReadEncodedInt();
                        m_UOMusic = (MusicName)reader.ReadEncodedInt();
                        //m_range_enter = reader.ReadEncodedInt();
                        m_range_exit = reader.ReadEncodedInt();
                        m_time_between = reader.ReadDouble();
                        //m_uo_music_cutoff = reader.ReadDouble();
                        break;
                    }
            }

            NextMusicTime.Start(0);                 // we can play now
            m_MusicPlayer.AllDone = EndMusic;
        }

        #region Dynamic Add Listener
        public override bool HandlesOnMovement
        {
            get
            {   // we only handle movement if we are already playing UO Music
                object song = GetMusic();
                if (song != null)
                    if (song is string && Utility.IsUOMusic(song as string))
                        if (IsUOPlayTimerRunning())
                            return true;
                return false;
            }
        }
        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m is PlayerMobile pm)
                if (OnEnterMusicRegion(m, oldLocation))
                    if (m_UOMusic != MusicName.Invalid && m.NetState != null)
                    {
                        Utility.SoundCanvas[m] = m_UOMusic;
                        m.Send(Network.PlayMusic.GetInstance(m_UOMusic));
                    }

            base.OnMovement(m, oldLocation);
        }
        private bool OnEnterMusicRegion(Mobile m, Point3D oldLocation)
        {   // fake region OnEnter
            // Be aware, everyone in the tavern is listening to the music synchronized with the controller's timer.
            //  When we start music in this way (fake OnEnter,) this player will get a fresh copy of the music - NOT synchronized with the controller.
            //  This is fine until the controller's timer stops. This guy's partially played music will also stop, maybe abruptly, and will resume playing on the 
            //  next timer tick. He will now be in sync with everyone else in the tavern.
            //  These's simply no way to have him enter the tavern and hear the current song, in progress.
            if (m_UOMusicRange == _UOMusicRange.Area)
            {
                if (m.Map == this.Map)
                {
                    Rectangle2D controller_rect = new Rectangle2D(15, 15, new Point2D(this.Location));
                    Point2D old_location = new Point2D(oldLocation);
                    Point2D new_location = new Point2D(m.Location);
                    if (!controller_rect.Contains(old_location) && controller_rect.Contains(new_location))
                    {
                        if (m_UOMusic != MusicName.Invalid && m.NetState != null)
                        {
                            Utility.SoundCanvas[m] = m_UOMusic;
                            m.Send(Network.PlayMusic.GetInstance(m_UOMusic));
                            return true;
                        }
                    }
                }

            }
            // fake region OnEnter
            // Be aware, everyone in the tavern is listening to the music synchronized with the controller's timer.
            //  When we start music in this way (fake OnEnter,) this player will get a fresh copy of the music - NOT synchronized with the controller.
            //  This is fine until the controller's timer stops. This guy's partially played music will also stop, maybe abruptly, and will resume playing on the 
            //  next timer tick. He will now be in sync with everyone else in the tavern.
            //  These's simply no way to have him enter the tavern and hear the current song, in progress.
            else if (m_UOMusicRange == _UOMusicRange.Region)
            {
                Region controller_region = Region.Find(this.Location, this.Map);
                if (controller_region != null && !controller_region.IsDefault)
                {
                    Region old_region = Region.Find(oldLocation, m.Map);
                    Region new_region = Region.Find(m.Location, m.Map);
                    if (old_region != new_region && new_region == controller_region)
                    {
                        if (m_UOMusic != MusicName.Invalid && m.NetState != null)
                        {
                            Utility.SoundCanvas[m] = m_UOMusic;
                            m.Send(Network.PlayMusic.GetInstance(m_UOMusic));
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        #endregion Dynamic Add Listener
    }
}