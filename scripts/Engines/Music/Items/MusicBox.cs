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

/* Scripts\Items\Misc\MusicBox.cs
 * Changelog
 *  4/24/2024, Adam
 *      1. Allow remastering of tracks
 *      2. Rename AddMusic to Submit
 *  1/8/2024, Adam
 *      Disallow certain commands for embedded music boxes (buy, dupe, etc.)
 *  1/2/2024, Adam
 *      Allow TextCommands.What by anyone. (They are just asking the track name and author.)
 *      all other commands require house rights.
 *  5/24/2023, Adam
 *      Add verbal Context Menu replacements for Siege
 *      - AddMusic  // adds music to the music box
 *      - Rename    // renames he music box
 *  8/17/22, Adam
 *      Moved AdminGump.GetSharedAccounts() ==> Utility.GetSharedAccounts()
 *  3/7/22, Adam (FindSong/.List)
 *      Use kvp.Title.ToString() instead of kvp.ToString() for string matching
 *      Allow listing by author: Pastor Wydstrin, Galois, etc. 
 *  3/5/22, Adam
 *      Initialize m_playContext for the Add context menu
 *  3/3/22, Adam
 *      Reduce PublishFactor even more (0.1). Only a token really.
 *      I don't want to encourage publishers to submit junk for points.
 *  3/3/22. Adam
 *      1. Award Fame and Karma for Sales, Plays, and Publication
 *      2. Reduce awards for Publishing (the song may be rubbish) and Plays.
 *      3. Disallow credits when Alts buy or listen to an author's music
 *  2/28/22, Adam
 *      Apply Skill, Fame, and Karma for Sales, Plays, and Publication
 *      Don't let players buy a track if they already have access to it.
 *  2/27/22, Adam (Skill points)
 *      25 points for a sale
 *      10 point for a play
 *      100 points for having a piece published
 *  2/23/22, Adam (Find)
 *      Add a .Find command (staff only) that returns, among other things, the serial number of the track (RSM.)
 *      Update the .Delete command to accept a serial number.
 *      These two together give staff the ability to better manage broken or duplicate tracks.
 *      Note: duplicates are rare as the songs are hashed... but it is possible if say we have a very old track, pre-hashing.
 *      Another situation where may use this is (again old tracks) that may not have an owner.
 *  2/18/22, Adam (Napster)
 *      Add the notion of a 'Napster' model for music sharing
 *  2/18/22, Adam (IncPlayCount)
 *      Increment play count for song each time it's played
 *  2/11/22, Adam (rsm.Owner)
 *      Add a check for rsm.Owner == null when adding music to the musicbox
 *      Battle Of the Bards (1) didn't use Manuscript Paper to create the RSM and therefore there is no owner,
 *      and music cannot be correctly attributed.
 *  2/9/22, (SeekEnd())
 *      When reviewing a track, SeekEnd() of the playlist preventing us from just playing the next track.
 *      When reviewing a track, display the name title
 *  2/7/22, Adam (GetMusicKarma)
 *      Enable sounds to those with sufficient music Karma.
 *      Basically, you don't have access to special effect sounds unless you have sufficient music karma.
 *  1/30/22, Adam
 *      Allow playing of premium tracks if they are PurchasedOrOwned()
 *      Connect to RazorInstrument's callback to check the mobiles status (like have they logged out or moved away)
 *  1/30/22, Adam (MusicBoxMaintenance)
 *      Make MusicBoxMaintenance and associated timer static
 *      Make m_playerNotificationList local
 *  1/20/22, Adam
 *		Initial creation.
 */

using Server.Commands;
using Server.ContextMenus;
using Server.Engines;
using Server.Gumps;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Prompts;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using static Server.Items.MusicBox;

namespace Server.Items
{
    public class MusicContext
    {
        private List<RolledUpSheetMusic> m_ignoreList = new List<RolledUpSheetMusic>();
        public List<RolledUpSheetMusic> IgnoreList => m_ignoreList;
        private List<YouSoldMusic> m_playerNotificationList = new List<YouSoldMusic>();
        public List<YouSoldMusic> PlayerNotificationList => m_playerNotificationList;
        public MusicContext(List<RolledUpSheetMusic> ignoreList, List<YouSoldMusic> playerNotificationList)
        {
            m_ignoreList = ignoreList;
            m_playerNotificationList = playerNotificationList;
        }
    }
    [Flipable(0x2AF9, 0x2AFD)]
    public class MusicBox : Item, ISecurable
    {
        private Dictionary<Mobile, MusicContext> MusicConfig = new();
        const double PlayFactor = 1.0;      // one point per play
        const double PublishFactor = 0.1;   // not too much here as the song may be rubbish
        const double SalesFactor = 25.0;    // Okay, the song was liked enough to be purchased.
        private Mobile m_playMobile = null; // mobile that will play the back end music, only used for embedded MusicBoxes (Minstrel)
        public Mobile PlayMobile { get { return m_playMobile; } set { m_playMobile = value; } }
        #region Utilities
        private Direction Facing
        {
            get { if (ItemID == 0x2AF9 || ItemID == 0x2AF9 + 1) return Direction.East; else return Direction.South; }
            set
            {
                if (value == Direction.East && ItemID == 0x2AF9) ItemID = 0x2AFD;
                else if (value == Direction.East && ItemID == 0x2AF9 + 1) ItemID = 0x2AFD + 1;
                else if (value == Direction.South && ItemID == 0x2AFD) ItemID = 0x2AF9;
                else if (value == Direction.South && ItemID == 0x2AFD + 1) ItemID = 0x2AF9 + 1;
            }
        }
        private void Animate()
        {
            if (!IsAnimated)
            {
                if (Facing == Direction.East) ItemID = 0x2AF9 + 1;
                else ItemID = 0x2AFD + 1;
            }
        }
        private bool IsAnimated
        {
            get
            {
                return (ItemID == 0x2AF9 + 1 || ItemID == 0x2AFD + 1);
            }
        }
        private void StopAnimation()
        {
            if (IsAnimated)
            {
                if (ItemID == 0x2AFD + 1) ItemID = 0x2AFD;
                else if (ItemID == 0x2AF9 + 1) ItemID = 0x2AF9;
            }
        }
        private SecureLevel m_Level;
        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level
        {
            get { return m_Level; }
            set { m_Level = value; }
        }
        public bool HasMusicBoxAccess(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
                return true;

            // a minstrel for instance (parent backpack would not be supported.)
            if (this.Parent is Mobile)
                return true;

            BaseHouse house = BaseHouse.FindHouseAt(this);

            return (house != null && house.HasAccess(m));
        }
        public bool GMLHasTrackID(RolledUpSheetMusic rsm)
        {
            int trackID = rsm.TrackID;
            bool remaster_found = false;
            foreach (var kvp in GlobalMusicRepository)
                if (kvp.Key.TrackID == trackID)
                {
                    remaster_found = true;
                    break;
                }
            return remaster_found;
        }
        #endregion Utilities
        #region Context Menus
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.CheckAlive() && HasMusicBoxAccess(from))
            {
                list.Add(new AddSheetMusicEntry(from, this, remaster: false));
                list.Add(new NameMusicBoxEntry(from, this));
            }

            SetSecureLevelEntry.AddTo(from, this, list);
        }
        /*
		m_map[3000075] = "Name";
		m_map[3000154] = "Clear";
		m_map[3000163] = "Remove";
		m_map[3000175] = "Add";
		m_map[3000176] = "Delete";
		m_map[3000186] = "Continue";
		m_map[3000090] = "Apply";
        m_map[3000091] = "Cancel";
		m_map[3000094] = "Default";
		m_map[3000098] = "Information";
		m_map[3000123] = "Credits";
		m_map[3000165] = "Title";
		m_map[3000170] = "Sort by";
		m_map[3000191] = "Quit";
		m_map[3000362] = "Open";
        m_map[3000363] = "Close";
		m_map[3000390] = "Sound";
		m_map[3000395] = "Interface";
		m_map[3000410] = "Off";
        m_map[3000411] = "On";
		m_map[3000431] = "Inventory";
		m_map[3000548] = "Done";
        m_map[3000549] = "Close";
		m_map[3000564] = "Volume";
		m_map[3005113] = "Start";
        m_map[3005114] = "Stop";
		m_map[3010002] = "Back";
        m_map[3010003] = "Index";
        m_map[3010004] = "History";
        m_map[3010005] = "Search";
		m_map[3010041] = "Inventory";
		*/
        private class AddSheetMusicEntry : ContextMenuEntry
        {
            private Mobile m_From;
            private MusicBox m_musicBox;
            bool m_remaster;

            public AddSheetMusicEntry(Mobile from, MusicBox musicbox, bool remaster)
                : base(0175/*add*/)
            {
                m_From = from;
                m_musicBox = musicbox;
                m_remaster = remaster;
            }

            public override void OnClick()
            {
                if (m_From.CheckAlive() && m_musicBox.HasMusicBoxAccess(m_From))
                {
                    m_From.SendMessage("Target the music you would like to add...");
                    m_From.Target = new AddMusicTarget(m_musicBox, m_remaster); // Call our target
                }
            }
        }
        public class AddMusicTarget : Target // Create our targeting class (which we derive from the base target class)
        {
            MusicBox m_musicBox;
            bool m_remaster;
            public AddMusicTarget(MusicBox musicbox, bool remaster)
                : base(5, true, TargetFlags.None)
            {
                m_musicBox = musicbox;
                m_remaster = remaster;
            }
            protected override void OnTarget(Mobile from, object target)
            {   // buyer added for clarity only
                try
                {
                    // make sure we have a valid Play Context
                    m_musicBox.RefreshPlayContext(from);

                    Mobile buyer = from;
                    if (target is RolledUpSheetMusic rsm)
                    {
                        if (rsm.Deleted == false)
                        {
                            if (rsm.Movable)
                            {
                                if (m_remaster && m_musicBox.GMLHasTrackID(rsm) == false)
                                {
                                    from.SendMessage("Unable to locate {0} in the global music library for remastering.", rsm.Title);
                                    return;
                                }
                                else if (!m_remaster && m_musicBox.GMLHasTrackID(rsm) == true)
                                {
                                    from.SendMessage("The global music library already contains {0}.", rsm.Title);
                                    from.SendMessage("You will need to 'remaster' the track to submit it.");
                                    return;
                                }

                                if (rsm.Napster == true)
                                {
                                    if (GlobalMusicRepository.ContainsKey(rsm))
                                    {   // already in the global library, just add this player to the list of 'purchasers'
                                        if (m_musicBox.HasRights(from, rsm) || GlobalMusicRepository[rsm].Owner == from)
                                        {
                                            from.SendMessage("You already have access to {0}.", rsm.Title);
                                            return;
                                        }
                                        if (!m_musicBox.Approved(rsm))
                                        {
                                            from.SendMessage("{0} is pending approval.", rsm.Title);
                                            return;
                                        }
                                        // update the users library
                                        from.SendMessage("You acquired {0} for {1} gold.", rsm, 0);
                                        //m_musicBox.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("'{0}', added to your library.", rsm));
                                        m_musicBox.Say(string.Format("'{0}', added to your library.", rsm), hue: 54);
                                        GlobalMusicRepository[rsm].Purchasers.Add(new PurchaserInfo(from, GlobalMusicRepository[rsm].Price));
                                        m_musicBox.m_playContext.RefreshPlayQueue();
                                        // and delete the copy 'added'
                                        rsm.Delete();
                                    }
                                    else
                                    {
                                        if (rsm.Owner == null)
                                        {
                                            // Battle Of the Bards 1 didn't use Manuscript Paper to create the RSM and therefore
                                            //  there is no owner. 
                                            from.SendMessage("This old version of {0} is not comparable with the musicbox.", rsm.Title);
                                            from.SendMessage("Please recreate using Manuscript Paper.");
                                            return;
                                        }

                                        // add some new Napster music
                                        // reconfigure the RSM to suit music box needs
                                        rsm.RequiresBackpack = false;
                                        rsm.MusicChatter_doWeNeedThis = false;
                                        rsm.CheckMusicianship_obsolete = false;

                                        // remove from the players backpack or other container
                                        if (rsm.Parent != null && rsm.Parent is Container c)
                                            c.RemoveItem(rsm);

                                        // finally, stick this thing on the internal map
                                        rsm.MoveToIntStorage();

                                        // now add to the global library
                                        MusicInfo info = new MusicInfo(rsm.Owner, rsm.Price);
                                        info.Napster = true;
                                        GlobalMusicRepository.Add(rsm, info);
                                        // add this person to Purchasers
                                        GlobalMusicRepository[rsm].Purchasers.Add(new PurchaserInfo(from, GlobalMusicRepository[rsm].Price));
                                        // add it to the list songs that need to be reviewed.
                                        UnpublishedNeedsReview.Add(rsm);
                                        from.SendMessage("Added {0} (Pending review.)", rsm.Title);
                                    }
                                }
                                else
                                {
                                    if (GlobalMusicRepository.ContainsKey(rsm))
                                    {
                                        from.SendMessage("The global music library already contains {0}.", rsm.Title);
                                        return;
                                    }
                                    if (rsm.Owner == null)
                                    {
                                        // Battle Of the Bards 1 didn't use Manuscript Paper to create the RSM and therefore
                                        //  there is no owner. 
                                        from.SendMessage("This old version of {0} is not compatible with the musicbox.", rsm.Title);
                                        from.SendMessage("Please recreate using Manuscript Paper.");
                                        return;
                                    }

                                    // reconfigure the RSM to suit music box needs
                                    rsm.RequiresBackpack = false;
                                    rsm.MusicChatter_doWeNeedThis = false;
                                    rsm.CheckMusicianship_obsolete = false;

                                    // remove from the players backpack or other container
                                    if (rsm.Parent != null && rsm.Parent is Container c)
                                        c.RemoveItem(rsm);

                                    // finally, stick this thing on the internal map
                                    rsm.MoveToIntStorage();

                                    // now add to the global library if you are the owner.
                                    if (from == rsm.Owner)
                                    {
                                        GlobalMusicRepository.Add(rsm, new MusicInfo(rsm.Owner, rsm.Price));
                                        // add it to the list songs that need to be reviewed.
                                        UnpublishedNeedsReview.Add(rsm);
                                        from.SendMessage("Added {0} (Pending review.)", rsm.Title);
                                    }
                                    else
                                        from.SendMessage("You do now own {0}.", rsm.Title);
                                }
                            }
                            else
                                from.SendMessage("That is locked down.");
                        }
                    }
                    else
                    {
                        from.SendMessage("That is not rolled up sheet music.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Diagnostics.LogHelper.LogException(ex);
                }
            }
        }
        private class NameMusicBoxEntry : ContextMenuEntry
        {
            private Mobile m_From;
            private MusicBox m_musicBox;

            public NameMusicBoxEntry(Mobile from, MusicBox musicbox)
                : base(0075/*name*/)
            {
                m_From = from;
                m_musicBox = musicbox;
            }

            public override void OnClick()
            {
                if (m_From.CheckAlive() && m_musicBox.HasMusicBoxAccess(m_From))
                {
                    m_From.Prompt = new NameMusicBoxPrompt(m_musicBox);
                    m_From.SendMessage("Type in the new name of the music box.");
                }
            }
        }
        private class NameMusicBoxPrompt : Prompt
        {
            private MusicBox m_musicBox;

            public NameMusicBoxPrompt(MusicBox musicbox)
            {
                m_musicBox = musicbox;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (text.Length > 40)
                    text = text.Substring(0, 40);

                if (from.CheckAlive() && m_musicBox.HasMusicBoxAccess(from))
                {
                    m_musicBox.Name = Utility.FixHtml(text.Trim());

                    from.SendMessage("The music box has been renamed.");
                }
            }

            public override void OnCancel(Mobile from)
            {
            }
        }
        #endregion Context Menus
        [Constructable]
        public MusicBox() : base(0x2AF9)
        {
            Weight = 1.0;
            Name = "music box";
            if (!MusicBoxRegistry.Contains(this))
                MusicBoxRegistry.Add(this);
        }
        public MusicBox(Serial serial) : base(serial)
        {
            // we will use this to detect changes to the global repository
            m_globalRepositoryHash = GetGlobalRepositoryHash();
            if (!MusicBoxRegistry.Contains(this))
                MusicBoxRegistry.Add(this);
        }
        public bool HasRights(Mobile m, RolledUpSheetMusic rsm)
        {
            if (GlobalMusicRepository.ContainsKey(rsm) && GlobalMusicRepository[rsm].Purchasers != null && GlobalMusicRepository[rsm].Purchasers.Count > 0)
            {
                foreach (PurchaserInfo pi in GlobalMusicRepository[rsm].Purchasers)
                    if (pi.Purchaser == m)
                        return true;
            }
            return false;
        }
        public bool GlobalRepositoryChanged()
        {
            bool changed;
            changed = m_globalRepositoryHash != GetGlobalRepositoryHash();  // log the change
            m_globalRepositoryHash = GetGlobalRepositoryHash();             // sync
            if (changed)
                Utility.ConsoleWriteLine("Global CoreMusicPlayer Repository changed.", ConsoleColor.Cyan);
            return changed;
        }
        public class PlayContext
        {
            private int _index = 0;                                 // current list position
            private List<RolledUpSheetMusic> m_playList = new List<RolledUpSheetMusic>();
            public List<RolledUpSheetMusic> Playlist => m_playList;
            private List<RolledUpSheetMusic> m_ignoreList = new List<RolledUpSheetMusic>();
            public List<RolledUpSheetMusic> IgnoreList { get => m_ignoreList; set => m_ignoreList = value; }

            private List<YouSoldMusic> m_playerNotificationList = new List<YouSoldMusic>();
            public List<YouSoldMusic> PlayerNotificationList { get => m_playerNotificationList; set => m_playerNotificationList = value; }

            public int Count => m_playList.Count;                   // total items in the list
            public int QueueCount => m_playList.Count - _index;     // remaining to be played
            private RolledUpSheetMusic m_nowPlaying = null;         // current track
            public RolledUpSheetMusic NowPlaying(RolledUpSheetMusic rsm = null)
            {
                if (rsm != null)
                {
                    int temp = Sync();
                    if (temp > -1)
                    {   // sync our index to the selected track
                        _index = temp + 1;
                    }
                    m_nowPlaying = SetNowPlaying(rsm);
                }

                return m_nowPlaying;
            }
            private bool m_loop = false;
            public bool Loop { get { return m_loop; } set { m_loop = value; } }
            Mobile m_from;
            public Mobile Mobile => m_from;
            private bool m_autoPlay = true;                         // Prev/Next and Play a specific song disables autoPlay
            public bool AutoPlay { get => m_autoPlay; set => m_autoPlay = value; }
            MusicBox m_musicbox;
            public PlayContext(Mobile from, MusicBox musicbox, List<RolledUpSheetMusic> ignoreList, List<YouSoldMusic> playerNotificationList)
            {   // whose playing the music
                m_from = from;
                m_musicbox = musicbox;
                m_ignoreList = ignoreList;
                m_playerNotificationList = playerNotificationList;
                LoadPlayQueue();
            }
            private RolledUpSheetMusic SetNowPlaying(RolledUpSheetMusic rsm)
            {
                if (rsm != null)
                    m_nowPlaying = (RolledUpSheetMusic)Utility.MakeTempItem(rsm);    // make a copy/instance for play
                else
                    m_nowPlaying = null;

                return m_nowPlaying;
            }
            public RolledUpSheetMusic Next()
            {
                if (_index < m_playList.Count)
                    m_nowPlaying = SetNowPlaying(m_playList[_index++]);
                else
                    m_nowPlaying = SetNowPlaying(null);

                return m_nowPlaying;
            }
            public RolledUpSheetMusic Prev()
            {
                if (_index > 1)
                    m_nowPlaying = SetNowPlaying(m_playList[_index++ - 2]);
                else
                    m_nowPlaying = SetNowPlaying(null);

                return m_nowPlaying;
            }
            public RolledUpSheetMusic PeekPrev()
            {
                RolledUpSheetMusic prev = null;
                if (_index > 1)
                    prev = m_playList[_index - 2];
                else
                    prev = null;

                return prev;
            }
            public void Shuffle()
            {
                Utility.Shuffle(m_playList);
                int temp = Sync();
                if (temp > -1)
                {   // sync our index to the selected track
                    _index = temp + 1;
                }
                else
                    Rewind();
            }
            private int Sync()
            {
                if (m_nowPlaying == null)
                    return -1;
                else
                    return Playlist.FindIndex(a => a.CompareTo(m_nowPlaying) == 0);
            }
            public void Rewind()
            {
                _index = 0;
            }

            public void SeekEnd()
            {
                _index = Playlist.Count;
            }

            public void ResetPlayQueue()
            {
                Playlist.Clear();
                LoadPlayQueue();
                Rewind();
            }
            public int RefreshPlayQueue()
            {
                int before = Count;
                LoadPlayQueue();
                if (Count - before != 0)
                    Rewind();
                return Count - before;
            }
            private void LoadPlayQueue()
            {
                List<RolledUpSheetMusic> killList = new List<RolledUpSheetMusic>();
                bool needsReset = false;

                // first cleanup the user's playlist
                if (Playlist.Count > 0)
                {
                    foreach (RolledUpSheetMusic rsm in Playlist)
                        if (rsm.Deleted || !GlobalMusicRepository.ContainsKey(rsm) || !m_musicbox.Approved(rsm) || (rsm.Napster && !m_musicbox.HasNapsterAccess(m_from, rsm)))
                            killList.Add(rsm);

                    foreach (RolledUpSheetMusic rsm in killList)
                    {
                        Playlist.Remove(rsm);
                        needsReset = true;

                        if (m_ignoreList.Contains(rsm))
                            m_ignoreList.Remove(rsm);
                    }
                }

                // now check the global library for deleted/unapproved
                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
                {
                    // handle deleted/unapproved
                    if (kvp.Key.Deleted || !m_musicbox.Approved(kvp.Key))
                    {
                        if (m_playList.Contains(kvp.Key))
                        {
                            m_playList.Remove(kvp.Key);
                            needsReset = true;
                        }
                        if (m_ignoreList.Contains(kvp.Key))
                            m_ignoreList.Remove(kvp.Key);
                    }

                    // now add
                    if (!kvp.Key.Deleted && (kvp.Key.Price == 0 || m_musicbox.PurchasedOrOwned(m_from, kvp.Key)) && m_musicbox.Approved(kvp.Key))
                        if (!m_playList.Contains(kvp.Key))
                            if (!m_ignoreList.Contains(kvp.Key))
                                if (!kvp.Key.Napster || (kvp.Key.Napster && m_musicbox.HasNapsterAccess(m_from, kvp.Key)))
                                    m_playList.Add(kvp.Key);

                }

                if (needsReset)
                {
                    Rewind();
                }
            }
        }
        private PlayContext m_playContext = null;
        private enum Rights
        {
            okay,
            lockdown,
            access,
            fail
        }
        private Rights CheckRights(Mobile from, bool message = true)
        {
            if (from == null)
            {
                if (message) from.SendMessage("fail");
                return Rights.fail;
            }
            else if (Parent == null)
            {
                if (!IsLockedDown)
                {
                    if (message) from.SendLocalizedMessage(502692); // This must be in a house and be locked down to work.
                    return Rights.lockdown;
                }
                else if (!HasMusicBoxAccess(from))
                {
                    if (message) from.SendLocalizedMessage(502436); // That is not accessible.
                    return Rights.access;
                }

                return Rights.okay;
            }
            else if (Parent is Mobile m)
            {
                if (GetMusicBox(m) != null)
                    return Rights.okay;
            }

            if (message) from.SendMessage("fail");
            return Rights.fail;
        }
        private MusicBox GetMusicBox(Mobile m)
        {
            if (m != null && m.Items != null)
                foreach (Item item in m.Items)
                    if (item is MusicBox mb)
                        return mb;
            return null;
        }
        public void RefreshPlayContext(Mobile from)
        {
            from = PlayMobile ?? from;
            if (m_playContext == null || m_playContext.Mobile != from)
            {
                // ** 1.0 musicboxes patch **
                // 1.0 musicboxes had a shared notion of the ignore list and playerNotificationList.
                //  We've moved that into an individual's MusicConfig. To do this we assigned World.GetSystemAcct() on deserialization as the default placeholder
                //  When we see the system account, we reassign that config to the first user that accesses the musicbox.
                //  Not a perfect solution, but should catch 99% of the cases
                if (MusicConfig.ContainsKey(World.GetSystemAcct()))
                {   // one-time patch
                    MusicContext mc = MusicConfig[World.GetSystemAcct()];
                    MusicConfig.Remove(World.GetSystemAcct());
                    MusicConfig.Add(from, mc);
                    m_playContext = new PlayContext(from, this, MusicConfig[from].IgnoreList, MusicConfig[from].PlayerNotificationList);
                }
                // normal cases
                else if (MusicConfig.ContainsKey(from))
                    m_playContext = new PlayContext(from, this, MusicConfig[from].IgnoreList, MusicConfig[from].PlayerNotificationList);
                else
                {
                    m_playContext = new PlayContext(from, this, new List<RolledUpSheetMusic>(), new List<YouSoldMusic>());
                    MusicConfig.Add(from, new MusicContext(new List<RolledUpSheetMusic>(), new List<YouSoldMusic>()));
                }
            }
        }
        public override void OnDoubleClick(Mobile from)
        {
            // make sure we have a valid Play Context
            RefreshPlayContext(from);

            // update the playlist
            if (GlobalRepositoryChanged())
                m_playContext.RefreshPlayQueue();

            if (CheckRights(from, message: true) != Rights.okay)
            {
                return;
            }
            else if (m_playContext.Count == 0 && IsThisDevicePlaying())
            {
                EndMusic(from);
                return;
            }
            else if (m_playContext.Count == 0)
            {
                SendMessage(from, "Your music box is empty.");
                return;
            }
            else
            {
                if (IsThisDevicePlaying())
                {   // stop player
                    EndMusic(from);
                }
                else if (IsUserBlocked(from))
                {
                    SendMessage(from, "You have initiated music in another context. You will need to cancel that or wait for it to finish.");
                    if (Core.Debug)
                        Utility.SendSystemMessage(from, 5, "Debug message: music not started because this player has initiated music elsewhere.");
                    // do nothing, user has already been notified
                }
                else
                {   // start player

                    // turn on auto play
                    m_playContext.AutoPlay = true;

                    // if it existed, we need to rewind it
                    m_playContext.Rewind();

                    // pick song
                    PlayMusic(from, m_playContext.Next());
                }
            }
        }
        // detects changes to the global repository
        private int m_globalRepositoryHash = 0;
        public void PlayMusic(Mobile m, RolledUpSheetMusic rsm)
        {
            Animate();
            rsm.AllDone = new RazorInstrument.FinishCallback(EndMusic);
            rsm.CheckSourceStatus = new RazorInstrument.CheckSourceStatusCallback(CheckMobileStatus);
            rsm.Instrument = rsm;           // default instrument is a harp
            rsm.PlayItem = this;            // item that sings (possibly obsolete)
            rsm.PlayMobile = PlayMobile;    // mobile responsible for back end music context ownership
            IncPlayCount(m, rsm);           // give the author credit for this play
            rsm.OnDoubleClick(m);           // do it
            return;
        }
        private void IncPlayCount(Mobile m, RolledUpSheetMusic rsm)
        {   // you don't 'play' credits for playing your own music
            if (m != rsm.Owner)
            {
                if (GlobalMusicRepository.ContainsKey(rsm))
                    GlobalMusicRepository[rsm].Plays++;

                // award points for having your track played
                if (rsm.Owner != null && !FamilyIP(rsm.Owner, m) && rsm.Owner is PlayerMobile pm && pm.NpcGuild == NpcGuild.BardsGuild)
                    AwardSkillFameKarma(pm, PlayFactor);
            }
        }
        private bool FamilyIP(Mobile owner, Mobile other)
        {
            System.Net.IPAddress ip = (owner.Account as Accounting.Account).LastGAMELogin;
            if (ip != null)
            {
                ArrayList list = Utility.GetSharedAccounts(ip);
                if (list.Contains(other.Account) == true)
                    return true;
            }

            return false;
        }
        public void EndMusic()
        {   // called back from RazorInstrument
            StopAnimation();
            if (m_playContext != null && CheckMobile(m_playContext.Mobile, this) && m_playContext.AutoPlay)
                if (GlobalRepositoryChanged())
                    m_playContext.RefreshPlayQueue();
                else
                    // if the user is playing a playlist, just continue
                    ContinueMusic(m_playContext.Mobile);
            return;
        }
        public void EndMusic(Mobile m)
        {   // player initiated cancel
            if (IsThisDevicePlaying())
            {
                m_playContext.NowPlaying().AllDone = null;              // we don't want RazorInstrument calling back to tell us the song ended - we know!
                m_playContext.NowPlaying().CheckSourceStatus = null;    // no more queries plz
                m_playContext.NowPlaying().OnDoubleClick(m);
            }
            StopAnimation();
        }
        public bool CheckMobileStatus(Mobile m, IEntity source)
        {   // called back from RazorInstrument
            return CheckMobile(m, source);
        }
        private bool CheckMobile(Mobile m, IEntity source)
        {
            bool result = false;
            if (RootParent is Mobile parent_mob)
            {   // this is an embedded musicbox, like on a minstrel
                if (parent_mob is HireMinstrel hm && hm.ControlMaster != null)
                    result = parent_mob.Map == hm.ControlMaster.Map && hm.ControlMaster.GetDistanceToSqrt(parent_mob) < 15;
                else
                    return false; // commands likely coming from a GM (no control master)
            }
            else if (m is PlayerMobile pm)
            {   // standard music box
                result = (pm.NetState != null && pm.NetState.Mobile != null && pm.NetState.Running == true) &&
                    this.Map == pm.Map && this.GetDistanceToSqrt(pm) < 15;
            }
            else if (m is Mobile && m.Deleted)
                result = false; // ya mobile deleted
            else
                result = false; // don't know this case

            return result;
        }
        private void ContinueMusic(Mobile m)
        {
            if (CheckMobile(m, this) && m_playContext != null && m_playContext.QueueCount > 0)
                PlayMusic(m, m_playContext.Next());
            else if (CheckMobile(m, this) && m_playContext != null && m_playContext.QueueCount == 0 && m_playContext.Loop == true)
            {
                m_playContext.Rewind();
                PlayMusic(m, m_playContext.Next());
            }
        }
        #region Text Interface
        public override bool HandlesOnSpeech => Parent == null;

        static List<string> Help = new()
        {
            /*
             * square brackets [optional option]
             * angle brackets <required argument>
             * curly braces {default values}
             * parenthesis (miscellaneous info)
             */
            "play [song]",
            "Cancel, stops playback",
            "Pause, pauses playback",
            "Continue, resume playback",
            "What, composer and song name",
            "Previous, previous song",
            "Next, next song",
            "Repeat, repeat last song",
            "Loop, loop playlist",
            "List, list all tracks",
            "Add, add track (previously removed)",
            "Remove, add a global track to the ignore list",
            "Refresh, adds any new global (free) tracks to your library",
            "Buy, user wishes to buy a track",
            "Shuffle, shuffle users playlist",
            "New, list new releases",
            "Remaster, request to remaster a previously approved song",
            "Submit, add music to the music box",
            "Rename, rename the music box",
            "Help, list of commands",
        };
        public enum TextCommands
        {
            #region player issued commands
            Play,           // request to play
            Cancel,         // stops playback (RazorInstrument stops)
            Pause,          // pauses playback (RazorInstrument still looping/waiting)
            Continue,       // resume playback
            What,           // who is the composer
            Previous,       // previous song
            Next,           // next song
            Repeat,         // repeat last song
            Loop,           // loop playlist
            List,           // list all tracks
            Add,            // add track (previously removed)
            Remove,         // add a global track to the ignore list
            Refresh,        // adds any new global (free) tracks to your library
            Buy,            // user wishes to buy a track
            Shuffle,        // shuffle users playlist
            New,            // list new releases
            Help,           // list of commands
            Remaster,       // request to remaster a previously approved song

            // composer/purchaser
            Sales,          // total composers sales
            Purchases,      // player purchases
            #endregion  player issued commands

            Unknown = 100,  // unknown command

            // siege has no context menus, so the next two commands are verbal replacements
            Submit,         // add music to the music box
            Rename,         // rename the music box

            // staff and TC commands
            FlushAll = 200,         // flush all tracks - everywhere
            Review,                 // review submitted tracks
            Approve,                // approve a track
            Deny,                   // deny (reject) a track
            Delete,                 // delete a track from the library
            Find,                   // find a track and display: serial number, owner, name, etc.
            Dupe,                   // makes a copy of the specified track and puts it in your backpack

            // return values
            Okay = 300,     // all is well (return value)
            NoTrack,        // can't find track (return value)
            Playing,        // we are playing (return value)
        }
        string[] Tokenizer(string text, string name)
        {
            int index = text.IndexOf(name);
            if (index == 0)
            {   // they said my name, make it the first token in the list of tokens (it may be more than one word.)
                List<string> arg_string = new() { name };
                arg_string.AddRange(text.Remove(index, name.Length).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                return arg_string.ToArray();
            }

            return text.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        public bool YouTalkingToMe(SpeechEventArgs e, out bool wasNamed)
        {
            wasNamed = false;
            if (e.Handled == true) return false;

            string[] tokens = Tokenizer(e.Speech.ToLower(), Name.ToLower());
            if (tokens.Length == 0 || tokens[0] != Name.ToLower()) return false; else wasNamed = true;
            if (this.Parent == null || this.Parent is Mobile)
            {
                if (CheckRights(e.Mobile, message: false) != Rights.okay)
                    return false;
            }
            else return false;
            if (TextCommand(tokens) == TextCommands.Unknown) return false;
            return true;
        }
        public void Say(string text, int hue = 54)
        {
            if (this.Parent is Mobile m)
                m.PublicOverheadMessage(Network.MessageType.Regular, hue, true, text);
            else if (Parent == null)
                this.PublicOverheadMessage(Network.MessageType.Regular, hue, true, text);
        }
        private void SendMessage(Mobile m, string text)
        {
            this.SendMessage(m, 0x3B2, text);
        }
        private void SendMessage(Mobile m, int hue, string text)
        {
            if (this.Parent is BaseCreature bc && bc.ControlMaster != null)
                bc.ControlMaster.SendMessage(hue, text);
            else if (Parent == null)
                m.SendMessage(hue, text);
        }
        public bool IsUserBlocked(Mobile m)
        {
            return SystemMusicPlayer.IsUserBlocked(m) && !IsThisDevicePlaying();
        }

        private Queue<SpeechEventArgs> SpamQueue = new Queue<SpeechEventArgs>();
        public override void OnSpeech(SpeechEventArgs e)
        {
            string[] tokens = Tokenizer(e.Speech.ToLower(), Name.ToLower());
            bool wasNamed = false;
            if (YouTalkingToMe(e, out wasNamed))
                e.Handled = true;
            else
            {
                if (wasNamed && CheckRights(e.Mobile, message: false) == Rights.okay)
                    //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, "Hmm, I don't know that.");
                    Say("Hmm, I don't know that.");
                return;
            }
            if (RootParent is not Mobile)
                if (GetDistanceToSqrt(e.Mobile) > 5) return;

            // make sure we have a valid Play Context
            RefreshPlayContext(e.Mobile);

            // check to see if the global repository has changed
            if (GlobalRepositoryChanged())
                m_playContext.RefreshPlayQueue();

            // this user is playing music elsewhere
            bool blocked = IsUserBlocked(e.Mobile);
            switch (TextCommand(tokens))
            {
                case TextCommands.Play:
                case TextCommands.Continue:
                case TextCommands.Next:
                case TextCommands.Previous:
                case TextCommands.Repeat:
                    if (blocked)
                    {
                        SendMessage(e.Mobile, "You have initiated music in another context. You will need to cancel that or wait for it to finish.");
                        if (Core.Debug)
                            Utility.SendSystemMessage(e.Mobile, 5, "Debug message: music not started because this player has initiated music elsewhere.");
                        return;
                    }
                    break;
            }

            // okay, their talking to us
            switch (TextCommand(tokens))
            {
                case TextCommands.Unknown:
                    {
                        //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, "Hmm, I don't know that.");
                        Say("Hmm, I don't know that.", hue: 54);
                        break;
                    }

                case TextCommands.Play:
                    {
                        m_playContext.AutoPlay = false;
                        // cancel whatever we were playing
                        if (IsThisDevicePlaying())
                        {
                            StopPlaying();
                            // this forces an immediate shutdown of the context and blows away the various queues
                            //  which are still loaded. 
                            SystemMusicPlayer.ForceStop(m_playContext.Mobile);
                        }

                        // start a new play
                        object info = null;
                        if (tokens.Length == 2)
                        {
                            // just play / restart play of the users playlist and free global tracks
                            this.OnDoubleClick(e.Mobile);
                        }
                        else if (FindSong(tokens, out info))
                        {
                            // make sure we have a valid Play Context
                            RefreshPlayContext(e.Mobile);

                            KeyValuePair<RolledUpSheetMusic, MusicInfo> globalBest = (KeyValuePair<RolledUpSheetMusic, MusicInfo>)info;
                            bool approved = Approved(globalBest.Key);
                            bool admin = e.Mobile.AccessLevel >= AccessLevel.Administrator;
                            if (approved || admin)
                            {   // we don't check ignore list here since they are explicitly asking to play a song.
                                if (globalBest.Key.Price == 0 || m_playContext.Playlist.Contains(globalBest.Key) || PurchasedOrOwned(e.Mobile, globalBest.Key) || admin)
                                {
                                    if (!globalBest.Key.Napster || (globalBest.Key.Napster && HasNapsterAccess(e.Mobile, globalBest.Key)))
                                    {
                                        //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("{0} by {1}.", globalBest.Key, globalBest.Key.Author));
                                        Say(string.Format("{0} by {1}.", globalBest.Key, globalBest.Key.Author), hue: 54);
                                        PlayMusic(e.Mobile, m_playContext.NowPlaying(globalBest.Key));
                                    }
                                    else if (globalBest.Key.Napster)
                                    {
                                        SendMessage(e.Mobile, string.Format("'{0}' is a Napster track. You will need to get a copy directly from {1}.", globalBest.Key, globalBest.Key.Owner.Name));
                                        //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("Here is a sample of {0} by {1}.", globalBest.Key, globalBest.Key.Author));
                                        Say(string.Format("Here is a sample of {0} by {1}.", globalBest.Key, globalBest.Key.Author), hue: 54);
                                        PlayMusic(e.Mobile, m_playContext.NowPlaying(globalBest.Key));
                                        Timer.DelayCall(TimeSpan.FromSeconds(10), new TimerStateCallback(EndSample), new object[] { this, e.Mobile });
                                    }
                                }
                                else
                                {
                                    //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("Here is a sample of {0} by {1}.", globalBest.Key, globalBest.Key.Author));
                                    Say(string.Format("Here is a sample of {0} by {1}.", globalBest.Key, globalBest.Key.Author), hue: 54);
                                    PlayMusic(e.Mobile, m_playContext.NowPlaying(globalBest.Key));
                                    Timer.DelayCall(TimeSpan.FromSeconds(10), new TimerStateCallback(EndSample), new object[] { this, e.Mobile });
                                }
                            }
                            else
                                //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("'{0}' is still pending approval", globalBest.Key));
                                Say(string.Format("'{0}' is still pending approval", globalBest.Key), hue: 54);
                        }
                        else
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("I couldn't find '{0}' in your music library", SongName(tokens)));
                            Say(string.Format("I couldn't find '{0}' in your music library", SongName(tokens)), hue: 54);

                        break;
                    }
                case TextCommands.Cancel:
                    {
                        if (IsThisDevicePlaying())
                            this.OnDoubleClick(e.Mobile);
                        break;
                    }
                case TextCommands.Pause:
                    {
                        if (IsThisDevicePlaying())
                            m_playContext.NowPlaying().Pause(m_playContext.Mobile);
                        break;
                    }
                case TextCommands.Continue:
                    {
                        if (IsThisDevicePlaying())
                            m_playContext.NowPlaying().Resume(m_playContext.Mobile);
                        break;
                    }
                case TextCommands.What: /*who*/
                    {
                        if (IsThisDevicePlaying())
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("This is {0} by {1}.", m_playContext.NowPlaying(), m_playContext.NowPlaying().Author));
                            Say(string.Format("This is {0} by {1}.", m_playContext.NowPlaying(), m_playContext.NowPlaying().Author), hue: 54);
                        else if (m_playContext.PeekPrev() != null)
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("That was {0} by {1}.", m_playContext.PeekPrev(), m_playContext.PeekPrev().Author));
                            Say(string.Format("That was {0} by {1}.", m_playContext.PeekPrev(), m_playContext.PeekPrev().Author), hue: 54);
                        else
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, "Hmm, I don't know that.");
                            Say("Hmm, I don't know that.", hue: 54);

                        break;
                    }
                case TextCommands.Previous:
                    {
                        //m_playContext.AutoPlay = false;
                        if (m_playContext.PeekPrev() != null)
                        {
                            if (IsThisDevicePlaying())
                            {
                                StopPlaying(true);
                                // this forces an immediate shutdown of the context and blows away the various queues
                                //  which are still loaded. 
                                SystemMusicPlayer.ForceStop(m_playContext.Mobile);
                            }
                            // and get the previously played on the next tick
                            PlayMusic(e.Mobile, m_playContext.NowPlaying(m_playContext.Prev()));
                        }
                        else
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, "I can't go back any further.");
                            Say("I can't go back any further.", hue: 54);
                        break;
                    }
                case TextCommands.Next:
                    {
                        //m_playContext.AutoPlay = false;
                        if (m_playContext.QueueCount > 0)
                        {
                            if (IsThisDevicePlaying())
                            {
                                StopPlaying(true);
                                // this forces an immediate shutdown of the context and blows away the various queues
                                //  which are still loaded. 
                                SystemMusicPlayer.ForceStop(m_playContext.Mobile);
                            }
                            // and get the next song on the next tick
                            PlayMusic(e.Mobile, m_playContext.Next());
                        }
                        else
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, "You're at the end of your playlist.");
                            Say("You're at the end of your playlist.", hue: 54);
                        break;
                    }
                case TextCommands.Repeat:
                    {
                        m_playContext.AutoPlay = false;
                        if (m_playContext.NowPlaying() != null)
                        {
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, "I'll repeat the song.");
                            Say("I'll repeat the song.", hue: 54);
                            if (IsThisDevicePlaying())
                            {
                                StopPlaying(true);
                                // this forces an immediate shutdown of the context and blows away the various queues
                                //  which are still loaded. 
                                SystemMusicPlayer.ForceStop(m_playContext.Mobile);
                            }
                            // and get the previously played
                            PlayMusic(e.Mobile, m_playContext.NowPlaying());
                        }
                        else if (m_playContext.PeekPrev() != null)
                        {
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, "I'll repeat the song.");
                            Say("I'll repeat the song.", hue: 54);
                            if (IsThisDevicePlaying())
                            {
                                StopPlaying(true);
                                // this forces an immediate shutdown of the context and blows away the various queues
                                //  which are still loaded. 
                                SystemMusicPlayer.ForceStop(m_playContext.Mobile);
                            }
                            // and get the previously played
                            PlayMusic(e.Mobile, m_playContext.NowPlaying(m_playContext.Prev()));
                        }
                        else
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, "Hmm, I have nothing in the queue.");
                            Say("Hmm, I have nothing in the queue.", hue: 54);
                        break;
                    }
                case TextCommands.Loop:
                    {
                        if (m_playContext.Loop == false)
                        {
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, "Okay, I will loop your playlist.");
                            Say("Okay, I will loop your playlist.", hue: 54);
                            m_playContext.Loop = true;
                        }
                        else
                        {
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, "Okay, I will stop looping your playlist.");
                            Say("Okay, I will stop looping your playlist.", hue: 54);
                            m_playContext.Loop = false;
                        }
                        break;
                    }
                case TextCommands.List:
                    {
                        List<KeyValuePair<int, string>> list = CompositeList(e.Mobile, tokens, SetFilter());

                        // okay, show the user
                        foreach (KeyValuePair<int, string> kvp in list)
                            SendMessage(e.Mobile, kvp.Key, kvp.Value);

                        if (GlobalMusicRepository.Count == 0)
                            SendMessage(e.Mobile, 54, "There are no music tracks available.");

                        break;
                    }
                case TextCommands.Add:
                    {
                        string title = SongName(tokens);
                        object info = null;
                        if (string.IsNullOrEmpty(title))
                        {
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, "Tell me what you would like to add.");
                            Say("Tell me what you would like to add.", hue: 54);
                            break;
                        }
                        else if (FindSong(tokens, out info))
                        {
                            // find a specific song
                            KeyValuePair<RolledUpSheetMusic, MusicInfo> globalBest = (KeyValuePair<RolledUpSheetMusic, MusicInfo>)info;
                            if (!Approved(globalBest.Key))
                            {
                                //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("Sorry, '{0}' is currently unavailable.", globalBest.Key));
                                Say(string.Format("Sorry, '{0}' is currently unavailable.", globalBest.Key), hue: 54);
                                break;
                            }
                            if (globalBest.Key.Price == 0)
                            {
                                if (!m_playContext.Playlist.Contains(globalBest.Key))
                                {
                                    //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("Added {0} by {1} to your library.", globalBest.Key, globalBest.Key.Author));
                                    Say(string.Format("Added {0} by {1} to your library.", globalBest.Key, globalBest.Key.Author), hue: 54);
                                    if (m_playContext.IgnoreList.Contains(globalBest.Key))
                                    {
                                        m_playContext.IgnoreList.Remove(globalBest.Key);
                                        m_playContext.RefreshPlayQueue();
                                    }
                                }
                                else
                                    //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("Your library already contains {0} by {1}.", globalBest.Key, globalBest.Key.Author));
                                    Say(string.Format("Your library already contains {0} by {1}.", globalBest.Key, globalBest.Key.Author), hue: 54);
                            }
                            else
                            {
                                if (!m_playContext.Playlist.Contains(globalBest.Key))
                                {
                                    //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true,
                                    //string.Format("{0} by {1} is a premium track. You will need to buy it.", globalBest.Key, globalBest.Key.Author));
                                    Say(string.Format("{0} by {1} is a premium track. You will need to buy it.", globalBest.Key, globalBest.Key.Author), hue: 54);
                                    if (m_playContext.IgnoreList.Contains(globalBest.Key))
                                        m_playContext.IgnoreList.Remove(globalBest.Key);
                                }
                                else
                                    //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("Your library already contains {0} by {1}.", globalBest.Key, globalBest.Key.Author));
                                    Say(string.Format("Your library already contains {0} by {1}.", globalBest.Key, globalBest.Key.Author), hue: 54);
                            }
                        }
                        else
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)));
                            Say(string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)), hue: 54);
                        break;
                    }
                case TextCommands.Remove:
                    {
                        string title = SongName(tokens);
                        if (string.IsNullOrEmpty(title))
                        {
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, "Tell me what you would like to remove.");
                            Say("Tell me what you would like to remove.", hue: 54);
                            break;
                        }
                        object info = null;
                        RolledUpSheetMusic rsm = null;
                        if (FindSong(tokens, out info))
                        {
                            rsm = ((KeyValuePair<RolledUpSheetMusic, MusicInfo>)info).Key;
                            if (m_playContext.Playlist.Contains(rsm) || GlobalMusicRepository.ContainsKey(rsm))
                            {
                                if (!m_playContext.IgnoreList.Contains(rsm))
                                {
                                    //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true,
                                    //string.Format("Okay, I'll remove {0} from your library.", rsm));
                                    Say(string.Format("Okay, I'll remove {0} from your library.", rsm), hue: 54);
                                    m_playContext.IgnoreList.Add(rsm);
                                    m_playContext.ResetPlayQueue();
                                }
                                else
                                    //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true,
                                    //string.Format("Hmm, you've already removed {0} from your library.", rsm));
                                    Say(string.Format("Hmm, you've already removed {0} from your library.", rsm), hue: 54);
                            }
                            else
                                //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true,
                                //string.Format("I could not find {0} in your library.", rsm));
                                Say(string.Format("I could not find {0} in your library.", rsm), hue: 54);
                        }
                        else
                        {
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true,
                            //string.Format("I could not find {0} in your library.", SongName(tokens)));
                            Say(string.Format("I could not find {0} in your library.", SongName(tokens)), hue: 54);
                        }

                        break;
                    }
                case TextCommands.Refresh:
                    {
                        int added = m_playContext.RefreshPlayQueue();
                        //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true,
                        //      string.Format("{1} {0} tracks to your library.", Math.Abs(added), added > 0 ? "Added" : "Removed"));
                        Say(string.Format("{1} {0} tracks to your library.", Math.Abs(added), added > 0 ? "Added" : "Removed"), hue: 54);
                        break;
                    }
                case TextCommands.Buy:
                    {
                        if (e.Mobile.AccessLevel > AccessLevel.Player && !Core.UOTC_CFG)
                        {
                            SendMessage(e.Mobile, "Staff cannot buy music on the production shard.");
                            break;
                        }
                        if (CheckEmbedded(e.Mobile, message: true))
                            break;
                        string title = SongName(tokens);
                        if (string.IsNullOrEmpty(title))
                        {
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, "Tell me what you would like to buy.");
                            Say("Tell me what you would like to buy.", hue: 54);
                            break;
                        }

                        object info = null;
                        if (!FindSong(tokens, out info))
                        {
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)));
                            Say(string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)), hue: 54);
                            break;
                        }

                        RolledUpSheetMusic rsm = ((KeyValuePair<RolledUpSheetMusic, MusicInfo>)info).Key;

                        if (!Approved(rsm))
                        {
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("Sorry, '{0}' is currently unavailable.", rsm));
                            Say(string.Format("Sorry, '{0}' is currently unavailable.", rsm), hue: 54);
                            break;
                        }

                        if (rsm.Napster == true)
                        {   // you cannot buy Napster tracks, you must get the rolled-up sheet music from the composer
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("'{0}' is a Napster track. You will need to get a copy directly from {1}.", rsm, rsm.Owner.Name));
                            Say(string.Format("'{0}' is a Napster track. You will need to get a copy directly from {1}.", rsm, rsm.Owner.Name), hue: 54);
                            break;
                        }

                        if (m_playContext.IgnoreList.Contains(rsm))
                            m_playContext.IgnoreList.Remove(rsm);

                        int price = ((KeyValuePair<RolledUpSheetMusic, MusicInfo>)info).Value.Price;
                        if (price == 0)
                        {
                            if (m_playContext.Playlist.Contains(rsm))
                            {
                                //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("'{0}', is already in your playlist.", rsm));
                                Say(string.Format("'{0}', is already in your playlist.", rsm), hue: 54);
                                break;
                            }
                            else
                            {
                                m_playContext.RefreshPlayQueue();
                                //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("'{0}', added to your playlist.", rsm));
                                Say(string.Format("'{0}', added to your playlist.", rsm), hue: 54);
                                break;
                            }
                        }
                        else if (HasRights(e.Mobile, rsm) || GlobalMusicRepository[rsm].Owner == e.Mobile)
                        {
                            SendMessage(e.Mobile, string.Format("You already have access to {0}.", rsm.Title));
                            return;
                        }
                        else
                        {
                            e.Mobile.Prompt = new BuyMusicBoxPrompt(e.Mobile, rsm, this, price);
                            SendMessage(e.Mobile, string.Format("The cost to buy '{0}' is {1} gold. Are you sure you wish to buy this track?", rsm, price));
                        }

                        break;
                    }
                case TextCommands.Shuffle:
                    {
                        //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true,
                        //          string.Format("Okay, I'll shuffle your library."));
                        Say(string.Format("Okay, I'll shuffle your library."), hue: 54);
                        //if (IsPlaying()) StopPlaying(true);
                        m_playContext.Shuffle();
                        break;
                    }
                case TextCommands.New:
                    {
                        List<KeyValuePair<int, string>> list = CompositeList(e.Mobile, tokens, SetFilter(Filter.New | Filter.Remastered));

                        if (list.Count == 0)
                            SendMessage(e.Mobile, 54, "There are no new releases.");

                        // okay, show the user
                        foreach (KeyValuePair<int, string> kvp in list)
                            SendMessage(e.Mobile, kvp.Key, kvp.Value);

                        if (GlobalMusicRepository.Count == 0)
                            SendMessage(e.Mobile, 54, "There are no music tracks available.");

                        break;
                    }
                case TextCommands.Help:
                    {
                        foreach (string sx in Help)
                            SendMessage(e.Mobile, sx);
                        break;
                    }

                // composer/purchaser
                case TextCommands.Sales:
                    {
                        if (!CheckEmbedded(e.Mobile, message: true))
                        {
                            int totalSales, sold, credit;
                            CalculateSellerData(e.Mobile, out totalSales, out sold, out credit);
                            SendMessage(e.Mobile, string.Format("You have sold {0} tracks.", sold));
                            SendMessage(e.Mobile, string.Format("For a total of {0} gold", totalSales));
                            SendMessage(e.Mobile, string.Format("You have {0} credit due you.", credit));
                        }
                        break;
                    }
                case TextCommands.Purchases:
                    {
                        if (!CheckEmbedded(e.Mobile, message: true))
                        {
                            int total = 0;
                            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
                                foreach (PurchaserInfo lml in kvp.Value.Purchasers)
                                    if (lml.Purchaser == e.Mobile)
                                    {
                                        SendMessage(e.Mobile, string.Format("On {0}, you purchased {1} by {2} for {3} gold.", lml.PurchaseDate, kvp.Key, kvp.Key.Author, lml.Price));
                                        if (kvp.Value.Price != lml.Price)
                                            // this can happen when the owner's character is deleted - all music gets repriced to zero
                                            SendMessage(e.Mobile, string.Format("Note: {0} is currently priced at {0} gold.", kvp.Key, kvp.Value.Price));
                                        total += lml.Price;
                                    }

                            SendMessage(e.Mobile, string.Format("You have spent a total of {0} gold on music.", total));
                        }
                        break;
                    }

                case TextCommands.Remaster:
                    {
                        if (!CheckEmbedded(e.Mobile, message: true) && e.Mobile.CheckAlive() && this.HasMusicBoxAccess(e.Mobile))
                        {
                            SendMessage(e.Mobile, "Target the remastered music...");
                            e.Mobile.Target = new AddMusicTarget(this, remaster: true);
                        }
                        break;
                    }
                // siege has no context menus, so the next two commands are verbal replacements
                case TextCommands.Submit:
                    {
                        if (!CheckEmbedded(e.Mobile, message: true) && e.Mobile.CheckAlive() && this.HasMusicBoxAccess(e.Mobile))
                        {
                            SendMessage(e.Mobile, "Target the music you would like to add...");
                            e.Mobile.Target = new AddMusicTarget(this, remaster: false);
                        }
                        break;
                    }
                case TextCommands.Rename:
                    {
                        if (!CheckEmbedded(e.Mobile, message: true) && e.Mobile.CheckAlive() && this.HasMusicBoxAccess(e.Mobile))
                        {
                            e.Mobile.Prompt = new NameMusicBoxPrompt(this);
                            SendMessage(e.Mobile, "Type in the new name of the music box.");
                        }
                        break;
                    }

                #region Staff only commands
                // staff commands go here
                case TextCommands.FlushAll:
                    {
                        if (e.Mobile.AccessLevel == AccessLevel.Owner && Core.UOTC_CFG)
                        {
                            List<RolledUpSheetMusic> list = new List<RolledUpSheetMusic>();
                            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
                                if (kvp.Key.Deleted == false)
                                    list.Add(kvp.Key);

                            m_playContext.Playlist.Clear();
                            m_playContext.Rewind();
                            m_playContext.NowPlaying(null);
                            m_playContext.IgnoreList.Clear();
                            GlobalMusicRepository.Clear();
                            foreach (RolledUpSheetMusic rsm in list)
                                if (rsm != null && rsm.Deleted == false)
                                    rsm.Delete();

                            SendMessage(e.Mobile, "Done.");
                        }
                        else
                            SendMessage(e.Mobile, "Unauthorized.");
                        break;
                    }
                case TextCommands.Review:
                    {
                        if (e.Mobile.AccessLevel < AccessLevel.Owner)
                        {
                            SendMessage(e.Mobile, "Unauthorized.");
                            break;
                        }

                        if (UnpublishedNeedsReview.Count == 0)
                        {
                            SendMessage(e.Mobile, "There are no songs to review.");
                            break;
                        }
                        RolledUpSheetMusic rsm = UnpublishedNeedsReview[0];
                        if (rsm.Owner == null)
                            SendMessage(e.Mobile, 33, string.Format("Warning: {0} by {1} has no Owner.", rsm.Title, rsm.Author));

                        //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("{0} by {1}.", rsm.Title, rsm.Author));
                        Say(string.Format("{0} by {1}.", rsm.Title, rsm.Author), hue: 54);
                        PlayMusic(e.Mobile, m_playContext.NowPlaying(UnpublishedNeedsReview[0]));
                        m_playContext.SeekEnd();    // ensures we don't start playing our playlist
                        SendMessage(e.Mobile, "Please type Approve, Deny or Delete:");
                        e.Mobile.Prompt = new ReviewMusicBoxPrompt(e.Mobile, UnpublishedNeedsReview[0], this);

                        break;
                    }
                case TextCommands.Approve:
                    {
                        if (e.Mobile.AccessLevel < AccessLevel.Owner)
                        {
                            SendMessage(e.Mobile, "Unauthorized.");
                            break;
                        }
                        if (tokens.Length == 3 && tokens[2] == "*")
                        {   // alexa approve *
                            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
                            {
                                if (!Approved(kvp.Key))
                                {
                                    SetFileStatus(e.Mobile, kvp.Key, SubmissionStatus.Approved);
                                    if (UnpublishedNeedsReview.Contains(kvp.Key))
                                        UnpublishedNeedsReview.Remove(kvp.Key);

                                    // award points for getting a track approved
                                    if (kvp.Key.Owner != null && kvp.Key.Owner is PlayerMobile pm && pm.NpcGuild == NpcGuild.BardsGuild)
                                        AwardSkillFameKarma(pm, PublishFactor);
                                }
                            }
                        }
                        else
                        {
                            object info = null;
                            if (!FindSong(tokens, out info))
                            {
                                //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)));
                                Say(string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)), hue: 54);
                            }
                            else
                            {
                                RolledUpSheetMusic rsm = ParseInfo(info);
                                if (!Approved(rsm))
                                {
                                    SetFileStatus(e.Mobile, rsm, SubmissionStatus.Approved);
                                    if (UnpublishedNeedsReview.Contains(rsm))
                                        UnpublishedNeedsReview.Remove(rsm);

                                    // award points for getting a track approved
                                    if (rsm.Owner != null && rsm.Owner is PlayerMobile pm && pm.NpcGuild == NpcGuild.BardsGuild)
                                        AwardSkillFameKarma(pm, PublishFactor);
                                }
                                else
                                    SendMessage(e.Mobile, "That track has already been approved.");
                            }
                        }

                        break;
                    }
                case TextCommands.Deny:
                    {
                        if (e.Mobile.AccessLevel < AccessLevel.Owner)
                        {
                            SendMessage(e.Mobile, "Unauthorized.");
                            break;
                        }
                        if (tokens.Length == 3 && tokens[2] == "*")
                        {   // alexa reject *
                            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
                            {
                                SetFileStatus(e.Mobile, kvp.Key, SubmissionStatus.Rejected);
                                if (UnpublishedNeedsReview.Contains(kvp.Key))
                                    UnpublishedNeedsReview.Remove(kvp.Key);
                            }
                        }
                        else
                        {
                            object info = null;
                            if (!FindSong(tokens, out info))
                            {
                                //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)));
                                Say(string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)), hue: 54);
                                break;
                            }
                            else
                            {
                                RolledUpSheetMusic rsm = ParseInfo(info);
                                SetFileStatus(e.Mobile, rsm, SubmissionStatus.Rejected);
                                if (UnpublishedNeedsReview.Contains(rsm))
                                    UnpublishedNeedsReview.Remove(rsm);
                            }
                        }

                        break;
                    }
                case TextCommands.Delete:
                    {
                        if (e.Mobile.AccessLevel < AccessLevel.Owner)
                        {
                            SendMessage(e.Mobile, "Unauthorized.");
                            break;
                        }

                        List<RolledUpSheetMusic> list = new List<RolledUpSheetMusic>();
                        int serial;
                        if (tokens.Length == 3 && tokens[2] == "*")
                        {   // alexa delete *
                            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
                                list.Add(kvp.Key);
                        }
                        else if (tokens.Length == 3 && tokens[2].Length > 2 && tokens[2].StartsWith("0x", StringComparison.CurrentCultureIgnoreCase) &&
                            Int32.TryParse(tokens[2].Substring(2, tokens[2].Length - 2), System.Globalization.NumberStyles.HexNumber, null, out serial))
                        {
                            Item item = World.FindItem(serial);
                            if (item != null && item is RolledUpSheetMusic rsm)
                                list.Add(rsm);
                            else
                                SendMessage(e.Mobile, string.Format("{0} is not the serial of any RolledUpSheetMusic.", tokens[2]));
                        }
                        else
                        {
                            object info = null;
                            if (!FindSong(tokens, out info))
                            {
                                //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)));
                                Say(string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)), hue: 54);
                                break;
                            }
                            RolledUpSheetMusic rsm = ParseInfo(info);
                            list.Add(rsm);
                        }

                        foreach (RolledUpSheetMusic rsm in list)
                        {
                            if (UnpublishedNeedsReview.Contains(rsm))
                                UnpublishedNeedsReview.Remove(rsm);

                            // remove from playlist
                            if (m_playContext.Playlist.Contains(rsm))
                                m_playContext.Playlist.Remove(rsm);

                            // remove from global library
                            if (GlobalMusicRepository.ContainsKey(rsm))
                                GlobalMusicRepository.Remove(rsm);

                            rsm.Delete();

                            SendMessage(e.Mobile, string.Format("{0} deleted.", rsm));
                        }

                        break;
                    }
                case TextCommands.Find:
                    {
                        if (CheckEmbedded(e.Mobile, message: true))
                            break;
                        if (e.Mobile.AccessLevel < AccessLevel.Owner)
                        {
                            SendMessage(e.Mobile, "Unauthorized.");
                            break;
                        }
                        if (string.IsNullOrEmpty(SongName(tokens)))
                        {
                            SendMessage(e.Mobile, "No song specified.");
                            break;
                        }
                        int count = 0;
                        foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
                        {
                            if (kvp.Key.SongName.ToLower().Contains(SongName(tokens).ToLower()))
                            {
                                SendMessage(e.Mobile, string.Format("{0}, Owner: {1}, Serial: {2:X}, Hash Code: {3:X}",
                                    kvp.Key.Name,                                       // 0
                                    kvp.Key.Owner != null ? kvp.Key.Owner : "(null)",   // 1
                                    kvp.Key.Serial,                                     // 2
                                    kvp.Key.HashCode                                    // 3
                                    ));
                                count++;
                            }
                        }

                        if (count == 0)
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("I couldn't find '{0}' in the music library", SongName(tokens)));
                            Say(string.Format("I couldn't find '{0}' in the music library", SongName(tokens)), hue: 54);
                        break;
                    }
                case TextCommands.Dupe:
                    {
                        if (CheckEmbedded(e.Mobile, message: true))
                            break;
                        if (e.Mobile.AccessLevel < AccessLevel.Seer)
                        {
                            SendMessage(e.Mobile, "Unauthorized.");
                            break;
                        }
                        if (string.IsNullOrEmpty(SongName(tokens)))
                        {
                            SendMessage(e.Mobile, "No song specified.");
                            break;
                        }
                        int count = 0;
                        foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
                        {
                            if (kvp.Key.SongName.ToLower().Contains(SongName(tokens).ToLower()))
                            {
                                SendMessage(e.Mobile, string.Format("Duping {0}, Owner: {1}, Serial: {2:X}, Hash Code: {3:X}",
                                    kvp.Key.Name,                                       // 0
                                    kvp.Key.Owner != null ? kvp.Key.Owner : "(null)",   // 1
                                    kvp.Key.Serial,                                     // 2
                                    kvp.Key.HashCode                                    // 3
                                    ));
                                Item dupe = Utility.Dupe(kvp.Key);
                                dupe.SetItemBool(ItemBoolTable.StaffOwned, true);
                                e.Mobile.AddToBackpack(dupe);
                                SendMessage(e.Mobile, string.Format("a copy of {0} has been placed in your backpack. Do not redistribute.", dupe));
                                Utility.TrackStaffOwned(e.Mobile, dupe);
                                count++;
                            }
                        }

                        if (count == 0)
                            //this.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("I couldn't find '{0}' in the music library", SongName(tokens)));
                            Say(string.Format("I couldn't find '{0}' in the music library", SongName(tokens)), hue: 54);
                        break;
                    }
                    #endregion Staff only commands
            }

            return;
        }
        [Flags]
        protected enum Filter
        {
            None,
            New,
            Remastered,
        }
        private Filter m_Filter = Filter.None;  // not serialized
        private Filter SetFilter(Filter flags = Filter.None, bool value = true)
        {
            m_Filter = Filter.None;
            if (value)
                m_Filter |= flags;
            else
                m_Filter &= ~flags;

            return m_Filter;
        }
        private bool GetFilter(Filter flag)
        {
            return ((m_Filter & flag) != 0);
        }
        private List<KeyValuePair<int, string>> CompositeList(Mobile from, string[] tokens, Filter filter = Filter.None)
        {
            string author = string.Empty;
            if (tokens.Length >= 4)
            {
                if (tokens[2].ToLower() == "by")
                {
                    string temp = string.Empty;
                    for (int ix = 3; ix < tokens.Length; ix++)
                    {
                        temp += tokens[ix] + " ";
                    }
                    author = temp.Trim();
                }
            }
            bool admin = from.AccessLevel > AccessLevel.Player;
            List<KeyValuePair<int, string>> list = new List<KeyValuePair<int, string>>();
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
            {
                int color =
                    PendingReview(kvp.Key) ||
                    (!PurchasedOrOwned(from, kvp.Key) && (kvp.Value.Price > 0)) ||
                    kvp.Key.Napster && !HasNapsterAccess(from, kvp.Key)
                    ? 33 : 54;

                string text = string.Format("{0} by {1}{2}{3}{4}{5}{6}{7}",
                        kvp.Key,                                                            // 0
                        kvp.Key.Author,                                                     // 1
                        m_playContext.IgnoreList.Contains(kvp.Key) ? " (ignored)" : "",     // 2
                        Rejected(kvp.Key) ? " (Rejected)" : "",                             // 3
                        PendingReview(kvp.Key) ? " (Pending)" : "",                         // 4
                        kvp.Key.Napster ? " (Napster)" : "",                                // 5
                        (kvp.Value.Price > 0) ? " (Premium)" : "",                          // 6
                        kvp.Value.Remastered ? "(Remastered)" : ""                          // 7
                    );

                // filter by date approved for new tracks
                bool ok = !GetFilter(Filter.New) || (DateTime.UtcNow - kvp.Value.DateApproved).TotalDays < 8;
                // filter by date remastered for remastered tracks
                if (!ok) ok = !GetFilter(Filter.Remastered) || kvp.Value.Remastered && (DateTime.UtcNow - kvp.Value.DateRemastered).TotalDays < 8;
                if (ok)
                {
                    // only list 'rejected' tracks for staff
                    if (admin && Rejected(kvp.Key) || !Rejected(kvp.Key))
                        if (author == string.Empty)
                            list.Add(new KeyValuePair<int, string>(color, text));
                        else if (kvp.Key.Author.ToLower().Contains(author))
                            list.Add(new KeyValuePair<int, string>(color, text));
                }
            }

            // order the list so that 'owned' (color 54) are at the top
            list.Sort((e1, e2) =>
            {
                return e2.Key.CompareTo(e1.Key);
            });

            return list;
        }
        private bool CheckEmbedded(Mobile m = null, bool message = false)
        {
            if (Parent is Mobile)
            {
                if (m != null && message) SendMessage(m, "You will need a music box for this functionality.");
                return true;
            }

            return false;
        }
        private static void AwardSkillFameKarma(PlayerMobile pm, double skill_points)
        {
            int fameBump = 0;
            int karmaBump = 0;
            int fameOld = pm.Fame;
            if (skill_points == PublishFactor)
            {   // got a track approved. Good job!
                fameBump = 14000;                               // Nightmare level
                karmaBump = -14000;                             //
            }
            else if (skill_points == SalesFactor)
            {   // award points for selling a track
                fameBump = 8000;                                // lich level
                karmaBump = -8000;                              //
            }
            else if (skill_points == PlayFactor)
            {   // award points for having your track played
                fameBump = 5000;                                // BronzeElemental level
                karmaBump = -5000;                              //
            }
            else
            {
                Utility.ConsoleWriteLine("Error: invalid skill points in CoreMusicPlayer Box.", ConsoleColor.Red);
                return;
            }

            int totalFame = fameBump / 100;
            int totalKarma = -karmaBump / 100;
            totalFame += ((totalFame / 10) * 3);
            totalKarma += ((totalKarma / 10) * 3);

            Titles.AwardFame(pm, totalFame, true);
            Titles.AwardKarma(pm, totalKarma, true);
            // skill gain
            pm.NpcGuildPoints += skill_points;              // skill points
            pm.OnFameChange(fameOld);                       // old fame
            Commands.SystemMusicPlayer.OnSkillChange(pm, skill_points);
        }
        private string[] PreprocessTokens(string[] tokens)
        {
            List<string> list = new();
            if (tokens.Length >= 3)
            {   // collapse to a single command
                if (tokens[1] == "new" && (tokens[2] == "releases" || tokens[2] == "music"))
                {
                    list.Add(tokens[0]);                        // music box name
                    list.Add("newreleases");                    // new releases => newreleases
                    for (int ix = 3; ix < tokens.Length; ix++)  // add the rest of the command line
                        list.Add(tokens[ix]);
                }
                else list = new List<string>(tokens);
            }
            else list = new List<string>(tokens);
            return list.ToArray();
        }
        private TextCommands TextCommand(string[] tokens)
        {
            if (tokens.Length > 1)
            {
                tokens = PreprocessTokens(tokens);
                string text = tokens[1].Replace("?", "").Trim();
                switch (text)
                {
                    // regular commands
                    case "play":
                        return TextCommands.Play;
                    case "cancel":
                    case "stop":
                        return TextCommands.Cancel;
                    case "pause":
                        return TextCommands.Pause;
                    case "continue":
                    case "resume":
                        return TextCommands.Continue;
                    case "who":
                    case "what":
                    case "who's":
                    case "what's":
                        return TextCommands.What;
                    case "prev":
                    case "previous":
                        return TextCommands.Previous;
                    case "next":
                        return TextCommands.Next;
                    case "repeat":
                        return TextCommands.Repeat;
                    case "loop":
                        return TextCommands.Loop;
                    case "list":
                        return TextCommands.List;
                    case "add":
                        return TextCommands.Add;
                    case "remove":
                    case "ignore":
                        return TextCommands.Remove;
                    case "refresh":
                        return TextCommands.Refresh;
                    case "buy":
                        return TextCommands.Buy;
                    case "shuffle":
                        return TextCommands.Shuffle;
                    case "new":
                    case "newreleases":
                        return TextCommands.New;
                    case "help":
                    case "commands":
                        return TextCommands.Help;

                    case "remaster":
                        return TextCommands.Remaster;

                    // composer/purchaser
                    case "sales":
                        return TextCommands.Sales;
                    case "purchases":
                        return TextCommands.Purchases;

                    // siege has no context menus, so the next two commands are verbal replacements
                    case "submit":
                        return TextCommands.Submit;
                    case "rename":
                        return TextCommands.Rename;


                    // staff and TC commands
                    case "flushall":
                        return TextCommands.FlushAll;
                    case "review":
                        return TextCommands.Review;
                    case "approve":
                        return TextCommands.Approve;
                    case "deny":
                        return TextCommands.Deny;
                    case "delete":
                        return TextCommands.Delete;
                    case "find":
                        return TextCommands.Find;
                    case "dupe":
                        return TextCommands.Dupe;

                    default:
                        return TextCommands.Unknown;
                }
            }
            return TextCommands.Unknown;
        }
        public bool PurchasedOrOwned(Mobile m, RolledUpSheetMusic rsm)
        {
            if (CheckRights(m, message: false) != Rights.okay)
                return false;
            else if (CheckEmbedded())
                return true;

            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
                if (kvp.Key == rsm)
                    if (kvp.Key.Owner == m)
                        return true;            // owner/composer
                    else
                        foreach (PurchaserInfo lml in kvp.Value.Purchasers)
                            if (lml.Purchaser == m)
                                return true;    // bought it
            return false;
        }
        public bool HasNapsterAccess(Mobile m, RolledUpSheetMusic rsm)
        {
            if (GlobalMusicRepository.ContainsKey(rsm))
            {
                if (rsm.Napster)
                {
                    if (CheckRights(m, message: false) != Rights.okay)
                        return false;
                    else if (CheckEmbedded())
                        return true;

                    foreach (PurchaserInfo lml in GlobalMusicRepository[rsm].Purchasers)
                        if (lml.Purchaser == m)
                            return true;    // was given this Napster track
                }
            }

            return false;
        }
        RolledUpSheetMusic ParseInfo(object info)
        {
            if (info is KeyValuePair<RolledUpSheetMusic, MusicInfo>)
                return ((KeyValuePair<RolledUpSheetMusic, MusicInfo>)info).Key;

            return null;
        }
        private void SetFileStatus(Mobile m, RolledUpSheetMusic rsm, SubmissionStatus status)
        {
            if (GlobalMusicRepository.ContainsKey(rsm) && GlobalMusicRepository[rsm].Status != status)
            {
                GlobalMusicRepository[rsm].Status = status;
                GlobalMusicRepository[rsm].Reason = RejectionReason.None;
            }

            if (rsm != null)
                m.SendMessage("{0} {1}.", rsm, status.ToString());
        }
        private int GetGlobalRepositoryHash()
        {   // detect changes in the globalMusicRepository
            // We currently detect:
            //  tracks added or removed - kvp.Key.GetHashCode()
            //  status change on a track - kvp.Value.Status.GetHashCode()
            //  and if the track has been deleted - kvp.Key.Deleted.GetHashCode()
            int hash = GlobalMusicRepository.GetHashCode();
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
            {
                // if any of these things change, then the library has changed and the user's music box must resync
                hash = hash * 23 + kvp.Key.GetHashCode();
                hash = hash * 23 + kvp.Value.Status.GetHashCode();
                hash = hash * 23 + kvp.Key.Deleted.GetHashCode();
            }
            // unreliable
            // hash_code ^= hash_code ^ kvp.Key.GetHashCode() ^ kvp.Value.Status.GetHashCode() ^ kvp.Key.Deleted.GetHashCode();
            // https://stackoverflow.com/questions/19139240/why-is-the-xor-operator-used-in-computing-hash-code?lq=1
            // prefer
            // https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode/263416#263416

            return hash;
        }
        private class ReviewMusicBoxPrompt : Prompt
        {
            Mobile m_mobile;
            RolledUpSheetMusic m_rsm = null;
            private MusicBox m_musicbox;

            public ReviewMusicBoxPrompt(Mobile m, RolledUpSheetMusic rsm, MusicBox musicbox)
            {
                m_mobile = m;
                m_rsm = rsm;
                m_musicbox = musicbox;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (text.Length > 40)
                    text = text.Substring(0, 40).ToLower().Trim();

                if (m_musicbox.CheckMobile(from, m_musicbox) && m_musicbox.HasMusicBoxAccess(from))
                {
                    if (m_musicbox.IsThisDevicePlaying())
                        m_musicbox.StopPlaying();

                    if (text == "approve" || text == "deny" || text == "delete")
                    {
                        if (text == "approve")
                        {
                            // let's see if this is a remaster
                            int trackID = m_rsm.TrackID;
                            RolledUpSheetMusic remaster_target = null;
                            MusicInfo remaster_info = null;
                            bool remaster = false;
                            foreach (var kvp in GlobalMusicRepository)
                                if (kvp.Value.Status == SubmissionStatus.Approved)
                                    if (kvp.Key.TrackID == trackID)
                                    {
                                        remaster_target = kvp.Key;
                                        remaster_info = kvp.Value;
                                        remaster = true;
                                        break;
                                    }

                            if (remaster)
                            {
                                GlobalMusicRepository.Remove(remaster_target);                      // remove the old copy
                                GlobalMusicRepository[m_rsm] = remaster_info;                       // add the new copy while preserving music info (sales and whatnot)
                                GlobalMusicRepository[m_rsm].DateRemastered = DateTime.UtcNow;      // date remastered
                                GlobalMusicRepository[m_rsm].Remastered = true;                     // Remastered
                                if (UnpublishedNeedsReview.Contains(m_rsm))
                                    UnpublishedNeedsReview.Remove(m_rsm);
                                // no points for a remaster
                                // don't change the original publish date
                            }
                            else
                            {
                                if (GlobalMusicRepository.ContainsKey(m_rsm))
                                    GlobalMusicRepository[m_rsm].Status = SubmissionStatus.Approved;
                                else
                                {
                                    GlobalMusicRepository.Add(m_rsm, new MusicInfo(m_rsm.Owner, m_rsm.Price));
                                    GlobalMusicRepository[m_rsm].Status = SubmissionStatus.Approved;
                                }

                                if (UnpublishedNeedsReview.Contains(m_rsm))
                                    UnpublishedNeedsReview.Remove(m_rsm);

                                // award points for getting a track approved
                                if (m_rsm.Owner != null && m_rsm.Owner is PlayerMobile pm && pm.NpcGuild == NpcGuild.BardsGuild)
                                    AwardSkillFameKarma(pm, PublishFactor);

                                // users asking for recent additions from the music box
                                GlobalMusicRepository[m_rsm].DateApproved = DateTime.UtcNow;
                            }

                            if (remaster)
                                from.SendMessage("You approve the remastered track {0}.", m_rsm);
                            else
                                from.SendMessage("You approve the track {0}.", m_rsm);
                        }
                        else if (text == "delete")
                        {
                            if (GlobalMusicRepository.ContainsKey(m_rsm))
                                GlobalMusicRepository.Remove(m_rsm);

                            if (UnpublishedNeedsReview.Contains(m_rsm))
                                UnpublishedNeedsReview.Remove(m_rsm);

                            from.SendMessage("You delete the track {0}.", m_rsm);
                        }
                        else if (text == "deny")
                        {
                            if (GlobalMusicRepository.ContainsKey(m_rsm))
                                GlobalMusicRepository[m_rsm].Status = SubmissionStatus.Rejected;
                            else
                            {
                                GlobalMusicRepository.Add(m_rsm, new MusicInfo(m_rsm.Owner, m_rsm.Price));
                                GlobalMusicRepository[m_rsm].Status = SubmissionStatus.Rejected;
                            }

                            if (UnpublishedNeedsReview.Contains(m_rsm))
                                UnpublishedNeedsReview.Remove(m_rsm);

                            m_mobile.SendMessage("Please give one of the following reasons:");
                            foreach (string name in Enum.GetNames(typeof(RejectionReason)))
                            {
                                m_mobile.SendMessage(name);
                            }
                            m_mobile.Prompt = new RejectionReasonPrompt(m_rsm, m_musicbox);
                        }
                    }
                    else
                    {
                        from.SendMessage("You decide not to review the track.");
                    }
                }
            }
            public override void OnCancel(Mobile from)
            {
            }
        }
        private class RejectionReasonPrompt : Prompt
        {
            RolledUpSheetMusic m_rsm = null;
            private MusicBox m_musicbox;
            public RejectionReasonPrompt(RolledUpSheetMusic rsm, MusicBox musicbox)
            {
                m_rsm = rsm;
                m_musicbox = musicbox;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (text.Length > 40)
                    text = text.Substring(0, 40).ToLower().Trim();

                if (m_musicbox.CheckMobile(from, m_musicbox) && m_musicbox.HasMusicBoxAccess(from))
                {
                    RejectionReason reason = RejectionReason.Other;
                    if (!Enum.TryParse<RejectionReason>(text, true, out reason))
                        reason = RejectionReason.Other;

                    if (GlobalMusicRepository.ContainsKey(m_rsm))
                        GlobalMusicRepository[m_rsm].Reason = reason;

                    from.SendMessage("Your reason for the rejection was {0}.", reason.ToString());
                }
            }
            public override void OnCancel(Mobile from)
            {
            }
        }
        private class BuyMusicBoxPrompt : Prompt
        {
            Mobile m_mobile;
            RolledUpSheetMusic m_rsm = null;
            int m_price = 0;
            private MusicBox m_musicBox;

            public BuyMusicBoxPrompt(Mobile m, RolledUpSheetMusic rsm, MusicBox musicbox, int price)
            {
                m_mobile = m;
                m_rsm = rsm;
                m_musicBox = musicbox;
                m_price = price;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (text.Length > 40)
                    text = text.Substring(0, 40).ToLower().Trim();

                if (m_musicBox.CheckMobile(from, m_musicBox) && m_musicBox.HasMusicBoxAccess(from))
                {
                    if (text == "yes" || text == "y")
                    {
                        if (Mobiles.Banker.CombinedWithdrawFromAllEnrolled(m_mobile, m_price))
                        {
                            // update the users library
                            from.SendMessage("You purchased {0} for {1} gold.", m_rsm, m_price);
                            //m_musicBox.PublicOverheadMessage(Network.MessageType.Regular, 54, true, string.Format("'{0}', added to your library.", m_rsm));
                            m_musicBox.Say(string.Format("'{0}', added to your library.", m_rsm), hue: 54);
                            GlobalMusicRepository[m_rsm].Purchasers.Add(new PurchaserInfo(from, GlobalMusicRepository[m_rsm].Price));
                            m_musicBox.m_playContext.RefreshPlayQueue();

                            // Now pay the author
                            if (GlobalMusicRepository.ContainsKey(m_rsm))
                            {
                                if (GlobalMusicRepository[m_rsm].Owner != null)
                                {
                                    if (Mobiles.Banker.Deposit(GlobalMusicRepository[m_rsm].Owner, m_price))
                                    {
                                        // all done!
                                        //  you made a sale!
                                        //  user can ask the music box for Sales and Credits for info
                                        m_musicBox.m_playContext.PlayerNotificationList.Add(new YouSoldMusic(GlobalMusicRepository[m_rsm].Owner, from, m_rsm, GlobalMusicRepository[m_rsm]));
                                    }
                                    else
                                    { // else bankbox can't hold gold
                                        GlobalMusicRepository[m_rsm].Credit += m_price;
                                    }

                                    GlobalMusicRepository[m_rsm].TotalSales += m_price;
                                    GlobalMusicRepository[m_rsm].Sold += 1;
                                }
                                else
                                { // else no mobile: music becomes free
                                    GlobalMusicRepository[m_rsm].Price = 0;
                                    GlobalMusicRepository[m_rsm].Sold += 1;
                                }

                                // award points for selling a track
                                if (m_rsm.Owner != null && !m_musicBox.FamilyIP(m_rsm.Owner, from) && m_rsm.Owner is PlayerMobile pm && pm.NpcGuild == NpcGuild.BardsGuild)
                                    AwardSkillFameKarma(pm, SalesFactor);
                            }
                            else
                            {   // else error, song not in database
                                Utility.ConsoleWriteLine("MusicBox error: song not in database", ConsoleColor.Red);
                            }
                        }
                        else
                        {
                            from.SendMessage("You do not have sufficient funds to purchase that track.");
                        }
                    }
                    else
                    {
                        from.SendMessage("You decide not to buy the track.");
                    }
                }
            }
            public override void OnCancel(Mobile from)
            {
            }
        }
        public record YouSoldMusic
        {
            public RolledUpSheetMusic Rsm;
            public MusicInfo Gml;
            public Mobile You;
            public Mobile To;
            public YouSoldMusic(Mobile m, Mobile to, RolledUpSheetMusic rsm, MusicInfo gml)
            {
                You = m;    // you sold music
                To = to;    // To
                Rsm = rsm;  // this music
                Gml = gml;  // and global music library entry
            }
            public YouSoldMusic(GenericReader reader)
            {
                int version = reader.ReadEncodedInt();
                switch (version)
                {
                    case 0:
                        {
                            Rsm = (RolledUpSheetMusic)reader.ReadItem();
                            Gml = new MusicInfo(reader);
                            You = reader.ReadMobile();
                            To = reader.ReadMobile();
                            break;
                        }
                }
            }
            public void Serialize(GenericWriter writer)
            {
                int version = 0;
                writer.WriteEncodedInt(version);
                switch (version)
                {
                    case 0:
                        {
                            writer.Write(Rsm);
                            Gml.Serialize(writer);
                            writer.Write(You);
                            writer.Write(To);
                            break;
                        }
                }
            }
        }
        private bool IsThisDevicePlaying()
        {
            if (m_playContext != null && m_playContext.NowPlaying() != null && m_playContext.NowPlaying().IsThisDevicePlaying(m_playContext.Mobile))
                return true;

            return false;
        }
        private void StopPlaying(bool preservePlaylist = false)
        {
            if (IsThisDevicePlaying())
            {
                if (m_playContext.NowPlaying() != null)
                    m_playContext.NowPlaying().Resume(m_playContext.Mobile);
                this.EndMusic(m_playContext.Mobile);
            }

            // this will clear their current play list, which is necessary if they issue another Play command
            if (preservePlaylist == false)
                m_playContext.ResetPlayQueue();
        }
        private void EndSample(object state)
        {
            object[] aState = (object[])state;
            MusicBox musicbox = aState[0] as MusicBox;
            Mobile from = aState[1] as Mobile;
            if (musicbox != null && from != null)
            {   // cancel song.
                musicbox.EndMusic(from);
            }
        }
        private bool Approved(RolledUpSheetMusic rsm)
        {
            return Status(rsm, SubmissionStatus.Approved);
        }
        private bool Rejected(RolledUpSheetMusic rsm)
        {
            return Status(rsm, SubmissionStatus.Rejected);
        }
        private bool PendingReview(RolledUpSheetMusic rsm)
        {
            return Status(rsm, SubmissionStatus.Pending);
        }
        private bool Status(RolledUpSheetMusic rsm, SubmissionStatus status)
        {
            if (rsm != null && GlobalMusicRepository.ContainsKey(rsm))
                return GlobalMusicRepository[rsm].Status == status;

            return false;
        }
        private static string SongName(string[] tokens)
        {
            if (tokens.Length < 3) return string.Empty;
            string text = string.Empty;
            for (int ix = 2; ix < tokens.Length; ix++)
                text += tokens[ix] + " ";

            return text.Trim();
        }
        #endregion Text Interface
        #region Seller Data
        public void CalculateSellerData(Mobile m, out int totalSales, out int sold, out int credit)
        {
            totalSales = sold = credit = 0;
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
            {
                if (kvp.Value.Owner == m)
                {
                    totalSales += kvp.Value.TotalSales;
                    sold += kvp.Value.Sold;
                    credit += kvp.Value.Credit;
                }
            }
        }
        public record Competition
        {
            public int TotalSales = 0;
            public int Sold = 0;
            public Competition(int totalSales, int sold)
            {
                TotalSales = totalSales;
                Sold = sold;
            }
        }
        public void CalculateCompetitorData(Mobile m, out int totalSales, out int sold)
        {
            Dictionary<Mobile, Competition> stats = new Dictionary<Mobile, Competition>();
            totalSales = sold = 0;
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
            {
                if (kvp.Value.Owner != m && kvp.Value.Owner != null)
                {
                    if (!stats.ContainsKey(kvp.Value.Owner))
                    {
                        stats.Add(kvp.Value.Owner, new Competition(kvp.Value.TotalSales, kvp.Value.Sold));
                    }
                    else
                    {
                        stats[kvp.Value.Owner].TotalSales += kvp.Value.TotalSales;
                        stats[kvp.Value.Owner].Sold += kvp.Value.Sold;
                    }
                }
            }

            foreach (KeyValuePair<Mobile, Competition> kvp in stats)
            {
                if (kvp.Value.TotalSales > totalSales)
                    totalSales = kvp.Value.TotalSales;
                if (kvp.Value.Sold > sold)
                    sold = kvp.Value.Sold;
            }
        }
        #endregion Seller Data
        #region Music Database Format
        // we never want these tracks to disappear forever. Angel Island will keep a global repository of 
        //	player created music. (Could be used elsewhere.)
        public static Dictionary<RolledUpSheetMusic, MusicInfo> GlobalMusicRepository = new Dictionary<RolledUpSheetMusic, MusicInfo>();
        private static List<RolledUpSheetMusic> UnpublishedNeedsReview = new List<RolledUpSheetMusic>();
        public enum SubmissionStatus
        {
            Pending,
            Approved,
            Rejected
        }
        public enum RejectionReason
        {
            None,
            TooLong,
            TooShort,
            Inappropriate,
            Duplicate,
            Other
        }
        public record PurchaserInfo
        {
            Mobile m_purchaser = null;          // who bought it
            DateTime m_PurchaseDate;            // when purchased
            int m_Price;                        // price paid
            SubmissionStatus m_status;          // submission status
            RejectionReason m_rejectionReason;  // why rejected
            public SubmissionStatus Status { get { return m_status; } set { m_status = value; } }
            public RejectionReason Reason { get { return m_rejectionReason; } set { m_rejectionReason = value; } }
            public Mobile Purchaser => m_purchaser;
            public DateTime PurchaseDate => m_PurchaseDate;
            public int Price => m_Price;
            public PurchaserInfo(Mobile m, int price)
            {
                m_purchaser = m;
                m_PurchaseDate = DateTime.UtcNow;
                m_Price = price;
            }
            public PurchaserInfo(GenericReader reader)
            {
                int version = reader.ReadEncodedInt();

                switch (version)
                {
                    case 0:
                        {
                            m_purchaser = reader.ReadMobile();
                            m_PurchaseDate = reader.ReadDateTime();
                            m_Price = reader.ReadEncodedInt();
                            break;
                        }
                }
            }
            public void Serialize(GenericWriter writer)
            {
                int version = 0;
                writer.WriteEncodedInt(version);
                switch (version)
                {
                    case 0:
                        {
                            writer.Write(m_purchaser);
                            writer.Write(m_PurchaseDate);
                            writer.WriteEncodedInt(m_Price);
                            break;
                        }
                }
            }
        }
        [Flags]
        public enum MusicInfoFlags
        {
            None,
            Napster = 0x01,
            Remastered = 0x02,
        }
        public record MusicInfo
        {
            List<PurchaserInfo> m_purchasers = new List<PurchaserInfo>();
            Mobile m_Owner = null;             // who authored it
            DateTime m_DateSubmitted;           // when submitted
            DateTime m_DateApproved;            // when approved
            DateTime m_DateRemastered;          // when remastered
            int m_Price;                        // cost

            int m_totalSales = 0;               // total track sales in gold
            int m_credit = 0;                   // couldn't fit in bank box .. credit (we'll try to play later[TBD])
            int m_sold = 0;                     // units sold
            int m_plays = 0;                    // number of times this track has been played
            SubmissionStatus m_status;          // submission status
            RejectionReason m_rejectionReason;  // why rejected
            MusicInfoFlags m_flags = MusicInfoFlags.None;
            public List<PurchaserInfo> Purchasers => m_purchasers;
            public SubmissionStatus Status { get { return m_status; } set { m_status = value; } }
            public RejectionReason Reason { get { return m_rejectionReason; } set { m_rejectionReason = value; } }
            public int TotalSales { get { return m_totalSales; } set { m_totalSales = value; } }
            public int Credit { get { return m_credit; } set { m_credit = value; } }
            public int Sold { get { return m_sold; } set { m_sold = value; } }
            public int Plays { get { return m_plays; } set { m_plays = value; } }
            public bool Napster { get { return m_flags.HasFlag(MusicInfoFlags.Napster); } set { m_flags = (value == true) ? m_flags | MusicInfoFlags.Napster : m_flags & ~MusicInfoFlags.Napster; } }
            public bool Remastered { get { return m_flags.HasFlag(MusicInfoFlags.Remastered); } set { m_flags = (value == true) ? m_flags | MusicInfoFlags.Remastered : m_flags & ~MusicInfoFlags.Remastered; } }
            public Mobile Owner { get { return m_Owner; } set { m_Owner = value; } }
            public DateTime DateSubmitted => m_DateSubmitted;
            public DateTime DateApproved { get { return m_DateApproved; } set { m_DateApproved = value; } }

            public DateTime DateRemastered { get { return m_DateRemastered; } set { m_DateRemastered = value; } }
            public int Price { get { return m_Price; } set { m_Price = value; } }
            public MusicInfo(Mobile m, int price)
            {
                m_Owner = m;
                m_DateSubmitted = DateTime.UtcNow;
                m_Price = price;
                m_status = SubmissionStatus.Pending;
                m_rejectionReason = RejectionReason.None;
            }
            public MusicInfo(GenericReader reader)
            {
                int version = reader.ReadEncodedInt();

                switch (version)
                {
                    case 3:
                        {
                            m_DateRemastered = reader.ReadDateTime();
                            goto case 2;
                        }
                    case 2:
                        {
                            m_DateApproved = reader.ReadDateTime();
                            goto case 1;
                        }
                    case 1:
                        {
                            m_flags = (MusicInfoFlags)reader.ReadEncodedInt();
                            goto case 0;
                        }
                    case 0:
                        {
                            int count = reader.ReadEncodedInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                PurchaserInfo lml = new PurchaserInfo(reader);
                                m_purchasers.Add(lml);
                            }
                            m_status = (SubmissionStatus)reader.ReadEncodedInt();
                            m_rejectionReason = (RejectionReason)reader.ReadEncodedInt();
                            m_totalSales = reader.ReadEncodedInt();
                            m_credit = reader.ReadEncodedInt();
                            m_sold = reader.ReadEncodedInt();
                            m_plays = reader.ReadEncodedInt();
                            m_Owner = reader.ReadMobile();
                            m_DateSubmitted = reader.ReadDateTime();
                            m_Price = reader.ReadEncodedInt();
                            break;
                        }
                }
            }
            public void Serialize(GenericWriter writer)
            {
                int version = 3;
                writer.WriteEncodedInt(version);
                switch (version)
                {
                    case 3:
                        {
                            writer.Write(m_DateRemastered);
                            goto case 2;
                        }
                    case 2:
                        {
                            writer.Write(m_DateApproved);
                            goto case 1;
                        }
                    case 1:
                        {
                            writer.WriteEncodedInt((int)m_flags);
                            goto case 0;
                        }
                    case 0:
                        {
                            writer.WriteEncodedInt(m_purchasers.Count);
                            for (int ix = 0; ix < m_purchasers.Count; ix++)
                                m_purchasers[ix].Serialize(writer);
                            writer.WriteEncodedInt((int)m_status);
                            writer.WriteEncodedInt((int)m_rejectionReason);
                            writer.WriteEncodedInt(m_totalSales);
                            writer.WriteEncodedInt(m_credit);
                            writer.WriteEncodedInt(m_sold);
                            writer.WriteEncodedInt(m_plays);
                            writer.Write(m_Owner);
                            writer.Write(m_DateSubmitted);
                            writer.WriteEncodedInt(m_Price);
                            break;
                        }
                }
            }
        }
        #endregion Music Database Format
        #region Serialization
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;
            writer.WriteEncodedInt(version);

            switch (version)
            {
                case 1:
                    {
                        writer.WriteEncodedInt(MusicConfig.Count);
                        foreach (var mc in MusicConfig)
                        {
                            writer.Write(mc.Key);
                            writer.WriteEncodedInt(mc.Value.PlayerNotificationList.Count);
                            foreach (YouSoldMusic ysm in mc.Value.PlayerNotificationList)
                                ysm.Serialize(writer);

                            writer.WriteEncodedInt((int)mc.Value.IgnoreList.Count);
                            foreach (var ignore in mc.Value.IgnoreList)
                                writer.Write(ignore);
                        }
                        goto case 0;
                    }
                case 0:
                    {
                        //writer.WriteEncodedInt(m_playerNotificationList.Count);
                        //foreach (YouSoldMusic ysm in m_playerNotificationList)
                        //    ysm.Serialize(writer);

                        //writer.WriteEncodedInt((int)m_ignoreList.Count);
                        //foreach (var ignore in m_ignoreList)
                        //    writer.Write(ignore);

                        writer.WriteEncodedInt((int)m_Level);
                        break;
                    }
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                    {
                        int musicConfigCount = reader.ReadEncodedInt();
                        for (int ix = 0; ix < musicConfigCount; ix++)
                        {
                            List<YouSoldMusic> playerNotificationList = new List<YouSoldMusic>();
                            List<RolledUpSheetMusic> ignoreList = new List<RolledUpSheetMusic>();

                            Mobile m = reader.ReadMobile();
                            int playerNotificationListCount = reader.ReadEncodedInt();
                            for (int jx = 0; jx < playerNotificationListCount; jx++)
                                playerNotificationList.Add(new YouSoldMusic(reader));

                            int ignoreListCount = reader.ReadEncodedInt();
                            for (int mx = 0; mx < ignoreListCount; mx++)
                            {
                                Item item = reader.ReadItem();
                                if (item != null)
                                    ignoreList.Add(item as RolledUpSheetMusic);
                            }

                            if (m != null)
                                MusicConfig.Add(m, new MusicContext(ignoreList, playerNotificationList));
                        }

                        m_Level = (SecureLevel)reader.ReadEncodedInt();
                        break;
                    }
                case 0: // OBSOLETE
                    {
                        List<YouSoldMusic> playerNotificationList = new List<YouSoldMusic>();
                        List<RolledUpSheetMusic> ignoreList = new List<RolledUpSheetMusic>();
                        int count = reader.ReadEncodedInt();
                        for (int ix = 0; ix < count; ix++)
                            playerNotificationList.Add(new YouSoldMusic(reader));

                        count = reader.ReadEncodedInt();
                        for (int ix = 0; ix < count; ix++)
                        {
                            Item item = reader.ReadItem();
                            if (item != null)
                                ignoreList.Add(item as RolledUpSheetMusic);
                        }
                        MusicConfig.Add(World.GetSystemAcct(), new MusicContext(ignoreList, playerNotificationList));
                        m_Level = (SecureLevel)reader.ReadEncodedInt();
                        break;
                    }
            }

            if (IsAnimated)
                StopAnimation();
        }
        #endregion Serialization
        #region Global Music Repository Serialization
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }
        public static void Load()
        {
            if (!File.Exists("Saves/Music.bin"))
                return;

            Console.WriteLine("Global music library Loading...");
            BinaryFileReader reader = null;
            try
            {
                reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/Music.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadEncodedInt();

                switch (version)
                {
                    case 0:
                        {
                            int count = reader.ReadEncodedInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Item item = reader.ReadItem();
                                if (item is RolledUpSheetMusic rsm && rsm != null)
                                    UnpublishedNeedsReview.Add(rsm);
                            }

                            count = reader.ReadEncodedInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                RolledUpSheetMusic rsm = (RolledUpSheetMusic)reader.ReadItem();
                                if (rsm != null)
                                    GlobalMusicRepository.Add(rsm, new MusicInfo(reader));
                                else
                                    // throw it away
                                    new MusicInfo(reader);
                            }
                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid Music.bin savefile version.");
                        }
                }
            }
            catch
            {
                Utility.ConsoleWriteLine("Error reading Saves/Music.bin, using default values...", ConsoleColor.Red);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Global music library Saving...");
            BinaryFileWriter writer = null;
            try
            {
                writer = new BinaryFileWriter("Saves/Music.bin", true);
                int version = 0;
                writer.WriteEncodedInt(version);

                switch (version)
                {
                    case 0:
                        {
                            writer.WriteEncodedInt(UnpublishedNeedsReview.Count);
                            foreach (RolledUpSheetMusic rsp in UnpublishedNeedsReview)
                                writer.Write(rsp);

                            writer.WriteEncodedInt(GlobalMusicRepository.Count);
                            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
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
                Utility.ConsoleWriteLine("Error writing Saves/Music.bin", ConsoleColor.Red);
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }
        #endregion Global Music Repository Serialization
        #region PlayerNotify
        public static List<MusicBox> MusicBoxRegistry = new List<MusicBox>();
        public static void Initialize()
        {
            Timer.DelayCall(TimeSpan.FromMinutes(5), new TimerCallback(MaintenanceTimer));
            // get staff 'get a copy' of rolled up sheet music
            Server.CommandSystem.Register("DupeMusic", AccessLevel.Seer, new CommandEventHandler(DupeMusic_OnCommand));
        }
        private static void ProcessPlayerNotification(MusicBox mb, List<YouSoldMusic> playerNotificationList)
        {
            List<YouSoldMusic> notificationList = new List<YouSoldMusic>();
            if (playerNotificationList.Count > 0)
            {
                foreach (YouSoldMusic usm in playerNotificationList)
                {
                    if (usm.You != null && usm.You.NetState != null && usm.You.NetState.Running)
                        notificationList.Add(usm);
                }
            }

            foreach (YouSoldMusic usm in notificationList)
            {
                int competitorTotalSales = 0;
                int competitorSold = 0;
                mb.CalculateCompetitorData(usm.You, out competitorTotalSales, out competitorSold);
                int yourTotalSales = 0;
                int youSold = 0;
                int credit = 0;
                mb.CalculateSellerData(usm.You, out yourTotalSales, out youSold, out credit);
                playerNotificationList.Remove(usm);
                Server.Engines.DataRecorder.DataRecorder.RecordMusicSales(usm.You, usm.Gml.Price, 1);
                usm.You.SendMessage(54, "You sold music to{0}!", (usm != null && usm.To != null) ? " " + usm.To.Name : "");
                if (yourTotalSales > competitorTotalSales)
                    usm.You.SendMessage(54, "You have made more profit than anyone else.");
                else if (youSold > competitorSold)
                    usm.You.SendMessage(54, "You are the best selling composer on the shard.");
                switch (Utility.Random(5))
                {
                    case 0:
                        {
                            usm.You.SendMessage(54, "Keep up the good work.");
                            break;
                        }
                    case 1:
                        {
                            usm.You.SendMessage(54, "Write more!");
                            break;
                        }
                    case 2:
                        {
                            usm.You.SendMessage(54, "Sell more!");
                            break;
                        }
                    case 3:
                        {
                            usm.You.SendMessage(54, "Be famous!");
                            break;
                        }
                    case 4:
                        {
                            usm.You.SendMessage(54, "Check the leaderboard!");
                            break;
                        }
                }
            }
        }
        public static void MaintenanceTimer()
        {
            // schedule the next tick
            Timer.DelayCall(TimeSpan.FromMinutes(5), new TimerCallback(MaintenanceTimer));

            // process player 'you sold music' notifications
            foreach (MusicBox mb in MusicBoxRegistry)
                foreach (var mc in mb.MusicConfig)
                    if (mc.Value.PlayerNotificationList.Count > 0)
                        ProcessPlayerNotification(mb, mc.Value.PlayerNotificationList);

            // notify staff some unpublished work needs review
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
                if (UnpublishedNeedsReview.Contains(kvp.Key) && kvp.Value.Status == SubmissionStatus.Rejected)
                    UnpublishedNeedsReview.Remove(kvp.Key);

            if (UnpublishedNeedsReview.Count > 0)
                ReviewNotify();

            // flush all deleted tracks
            MusicBoxMaintenance();
        }

        private static void MusicBoxMaintenance()
        {
            List<RolledUpSheetMusic> killList = new List<RolledUpSheetMusic>();

            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
            {
                if (kvp.Key.Deleted)
                    killList.Add(kvp.Key);

                // if the composer leaves the shard or deletes their character, all their music
                //  goes into the free bin.
                if (kvp.Value.Owner == null || kvp.Value.Owner.Deleted)
                    kvp.Value.Price = 0;
            }

            foreach (RolledUpSheetMusic rsm in killList)
                if (GlobalMusicRepository.ContainsKey(rsm))
                    GlobalMusicRepository.Remove(rsm);
        }
        private static void ReviewNotify()
        {
            Commands.CommandHandlers.BroadcastMessage(AccessLevel.Administrator, 54, string.Format("[{0}] {1}", "Music System", "New music, review required."));
        }

        #endregion PlayerNotify
        #region Public Utilities
        [Usage("[DupeMusic <(partial) song text>")]
        [Description("creates a copy of music box music. (not for sale or redistribution!)")]
        public static void DupeMusic_OnCommand(CommandEventArgs e)
        {
            object info;
            RolledUpSheetMusic rsm = null;
            string text = ". . " + e.ArgString; // need to add placeholders for "alexa play"
            if (MusicBox.FindSong(text.Split(), out info))
                rsm = ((KeyValuePair<RolledUpSheetMusic, MusicInfo>)info).Key;
            if (rsm != null)
            {
                Item dupe = Utility.Dupe(rsm);
                dupe.SetItemBool(ItemBoolTable.StaffOwned, true);
                e.Mobile.AddToBackpack(dupe);
                e.Mobile.SendMessage(string.Format("a copy of {0} has been placed in your backpack. Do not redistribute.", dupe));
                Utility.TrackStaffOwned(e.Mobile, dupe);
            }
            else
            {
                e.Mobile.SendMessage(string.Format("no songs matching '{0}' found.", e.ArgString));
            }
        }
        public static bool FindSong(string[] tokens, out object info)
        {
            info = null;
            string text = SongName(tokens);
            IntelligentDialogue.RollingHash rh = new IntelligentDialogue.RollingHash();

            // check the global library
            KeyValuePair<RolledUpSheetMusic, MusicInfo> globalBest = new KeyValuePair<RolledUpSheetMusic, MusicInfo>();
            int globalScore = 0;
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
            {
                int temp = rh.FindCommonSubstring(kvp.Key.ToString().ToLower(), text).Length;
                if (temp > globalScore)
                {
                    globalScore = temp;
                    globalBest = kvp;
                }
            }

            if (globalScore > 0)
            {
                info = globalBest;
                return true;
            }

            return false;
        }
        public record KarmaObject
        {
            public int Karma;
            public DateTime LastChecked = DateTime.UtcNow;
            public KarmaObject(int karma)
            {
                Karma = karma;
            }
        }
        private static Dictionary<Mobile, KarmaObject> ComposerKarmaCache = new Dictionary<Mobile, KarmaObject>();   // not serialized
        public static int GetMusicKarma(Mobile m)
        {
            if (ComposerKarmaCache.ContainsKey(m))
            {
                if (DateTime.UtcNow > ComposerKarmaCache[m].LastChecked)
                {
                    ComposerKarmaCache[m].LastChecked = DateTime.UtcNow.AddMinutes(15);
                    ComposerKarmaCache[m].Karma = ComputeMusicKarma(m);
                }
            }
            else
                ComposerKarmaCache.Add(m, new KarmaObject(ComputeMusicKarma(m)));

            return ComposerKarmaCache[m].Karma;
        }
        private static int ComputeMusicKarma(Mobile m)
        {
            int sales, plays, publications;
            int karma = GetPublishingPointsRaw(m, out sales, out plays, out publications);
            return karma * 100;
        }
        // we'll use this if someone should join the bards guild
        public static int GetPublishingPoints(Mobile m, out int sales, out int plays, out int publications)
        {
            sales = plays = publications = 0;
            GetPublishingPointsRaw(m, out sales, out plays, out publications);
            return (int)((sales * SalesFactor) + (publications * PublishFactor) + (plays * PlayFactor));
        }
        private static int GetPublishingPointsRaw(Mobile m, out int sales, out int plays, out int publications)
        {
            sales = plays = publications = 0;
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in GlobalMusicRepository)
            {
                if (kvp.Value.Owner != m)
                    continue;

                sales += kvp.Value.Sold;

                if (kvp.Value.Status == SubmissionStatus.Approved)
                    publications++;

                plays += kvp.Value.Plays;
            }

            return (sales) + (publications) + (plays);
        }
        #endregion Public Utilities
    }
}