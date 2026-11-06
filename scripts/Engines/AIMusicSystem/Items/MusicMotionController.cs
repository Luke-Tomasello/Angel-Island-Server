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

/* Scripts\Items\misc\MusicMotionController.cs
 *	ChangeLog :
 *	11/5/26, Adam (portable AIMS)
 *	    The play lock is an AimsUtil.LocalTimer (the music system's copy of Utility.LocalTimer). No behavior change.
 *	10/25/26, Adam (AIMusicSystem Phase 4 Step 1: shared device playback)
 *	    The play lock, start, cancel and "playing elsewhere" check now come from CalypsoMusicPlayer
 *	    (NextMusicTime, ReadyToPlay, StartPerformance, CancelPerformance, UserBlocked); EndMusic is gone (the player
 *	    restarts the lock with DelayBetween when the performance ends). No behavior change.
 *	12/13/2023, Adam
 *		Created. 
 *		Allows a music to be played when players pass nearby
 */

using Server.Commands;
using Server.Engines;
using Server.Misc;
using Server.Mobiles;
using System.IO;

namespace Server.Items
{
    [NoSort]
    public class MusicMotionController : Item
    {
        [Constructable]
        public MusicMotionController()
            : base(0x1B7A)
        {
            Running = true;
            Movable = false;
            Visible = false;
            Name = "music motion controller";
        }

        public MusicMotionController(Serial serial)
            : base(serial)
        {
        }
        private CalypsoMusicPlayer m_MusicPlayer = new CalypsoMusicPlayer();
        private AimsUtil.LocalTimer NextMusicTime { get { return m_MusicPlayer.NextMusicTime; } }   // the play lock
        public override bool HandlesOnMovement { get { return GetMusic() != null; } }
        public override Item Dupe(int amount)
        {
            MusicMotionController new_item = new MusicMotionController();
            if (GetRSM() != null)
            {
                // make a copy
                RolledUpSheetMusic? dupe = Utility.Dupe(GetRSM()) as RolledUpSheetMusic;
                new_item.AddItem(dupe);
            }
            return base.Dupe(new_item, amount);
        }
        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (Running)
            {
                // CanPlay blocks if: the player is already playing, OR another player nearby (15 tiles) is playing
                bool all_good = PlayMobile(m) && m_MusicPlayer.ReadyToPlay && m.InRange(this, m_range_enter) && m_MusicPlayer.CanPlay(this);
                // check if this user is playing music elsewhere
                bool blocked = m_MusicPlayer.UserBlocked;
                if (all_good && !blocked)
                {
                    object song = GetMusic();
                    if (song != null)
                        m_MusicPlayer.StartPerformance(m, this, song, m_range_exit, () => m_time_between);
                }
                else if (Core.Debug && all_good && blocked)
                    Utility.SendSystemMessage(m, 5, "Debug message: music not started because this player has initiated music elsewhere.");
            }

            base.OnMovement(m, oldLocation);
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
        public override void OnDoubleClick(Mobile from)
        {
            try
            {
                object? music = null;
                // check if this user is playing music elsewhere
                bool blocked = m_MusicPlayer.UserBlocked;   // should always be false for these 'temp mobiles'
                if ((music = GetMusic()) != null)
                {
                    if (m_MusicPlayer.Playing)
                    {
                        m_MusicPlayer.CancelPerformance();
                        if (m_MusicPlayer.MusicContextMobile != from)
                            from.Emote("*you cancel what you were playing*");
                    }
                    else if (!blocked)
                        m_MusicPlayer.StartPerformance(from, this, music, m_range_exit, () => m_time_between);
                    else if (Core.Debug && blocked)
                        Utility.SendSystemMessage(from, 5, "Debug message: music not started because this player has initiated music elsewhere.");
                }
                else
                    SendMessageNearby("No associated music");
            }
            catch
            {
                ;
            }
        }
        private bool IsPlayLocked()
        {
            return m_MusicPlayer.IsPlayLocked;
        }
        private RolledUpSheetMusic? GetRSM()
        {
            foreach (Item item in PeekItems)
                if (item is RolledUpSheetMusic rsm)
                    return rsm;
            return null;
        }
        private string? GetRZR()
        {
            if (m_RazorFile != null && m_RazorFile.EndsWith(".rzr") && File.Exists(Path.Combine(Core.DataDirectory, "song files", m_RazorFile)))
                return m_RazorFile;
            return null;
        }
        private object GetMusic()
        {
            object? music = GetRSM();
            if (music == null)
                music = GetRZR();
            return music;
        }
        private string? m_RazorFile;
        [CommandProperty(AccessLevel.GameMaster)]
        public string? RazorFile
        {
            get { return m_RazorFile; }
            set
            {
                if (value == null)
                    m_RazorFile = null;
                else if (!value.EndsWith(".rzr") || !File.Exists(Path.Combine(Core.DataDirectory, "song files", value)))
                    m_RazorFile = "Please enter: <name>.rzr";
                else
                {
                    CalypsoMusicPlayer.Metadata data = CalypsoMusicPlayer.GetMetaData(value);
                    SendMessageNearby(string.Format("Adding music {0}", data?.Song));

                    m_RazorFile = value;
                    // delete the rolled up sheet music if it exists
                    RolledUpSheetMusic? rsm = GetRSM();
                    if (rsm != null)
                    {
                        SendMessageNearby(string.Format("Deleting music {0}", rsm));
                        RemoveItem(rsm);
                        rsm.Delete();
                    }
                }
            }
        }
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.Seer)]
        public virtual Item? SheetMusic
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
                Item? musicObject = null;

                if (value != null && value is not RolledUpSheetMusic)
                {
                    this.SendMessageNearby("That is not rolled up sheet music");
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
                        SendMessageNearby(string.Format("Deleting music {0}", musicObject));
                        RemoveItem(musicObject);
                        musicObject.Delete();
                    }

                    musicObject = value;

                    if (musicObject != null)
                    {
                        Item? item = Utility.Dupe(musicObject);
                        item.SetItemBool(ItemBoolTable.StaffOwned, false);
                        SendMessageNearby(string.Format("Adding music {0}", item));
                        AddItem(item);
                    }
                }
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public string NextPlay
        {
            get
            {
                RolledUpSheetMusic? rsm = GetRSM();
                if (rsm != null && !IsPlayLocked())
                {
                    if (NextMusicTime.Triggered)
                        return "Now";
                    else
                        return string.Format("in {0} seconds", NextMusicTime.Remaining / 1000);
                }

                return "Unknown at this time";
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public string? Song
        {
            get
            {
                CalypsoMusicPlayer.Metadata data = CalypsoMusicPlayer.GetMetaData(GetMusic());
                return data?.Song;
            }
        }
        private int m_time_between = 12000;                 // two minutes until the next playback
        [CommandProperty(AccessLevel.Seer)]
        public int DelayBetween
        {
            get { return m_time_between; }
            set { m_time_between = value; }
        }
        private int m_range_enter = 15;                      // I get this close to trigger the device
        [CommandProperty(AccessLevel.Seer)]
        public int RangeEnter
        {
            get { return m_range_enter; }
            set { m_range_enter = value; }
        }
        private int m_range_exit = -1;                       // I get this far away and the music stops. -1 means no range checking
        [CommandProperty(AccessLevel.Seer)]
        public int RangeExit
        {
            get
            {
                this.SendMessageNearby("RangeExit: I get this far away and the music stops. -1 means no range checking");
                //this.SendSystemMessage("RangeExit: Currently disabled for MusicMotionControllers");
                return m_range_exit;
            }
            set { m_range_exit = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Running { get => base.IsRunning; set => base.IsRunning = value; }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);

            // version 1
            writer.WriteEncodedInt(m_range_enter);
            writer.WriteEncodedInt(m_range_exit);

            // version 0
            writer.Write(m_time_between);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_range_enter = reader.ReadEncodedInt();
                        m_range_exit = reader.ReadEncodedInt();
                        goto case 0;
                    }
                case 0:
                    {
                        m_time_between = reader.ReadInt();
                        break;
                    }
            }
        }
    }
}