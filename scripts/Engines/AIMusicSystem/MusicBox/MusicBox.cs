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

/* Scripts\Engines\AIMusicSystem\MusicBox\MusicBox.cs
 * Changelog
 *  11/5/26, Adam (2026 modernization: the music box cleanup, 10/29/26 to 11/5/26)
 *      MusicBox is a partial class in several files (ContextMenus, PlayContext, Playback, Speech, Commands,
 *      StaffCommands, CommandHelpers, Prompts, Notifications, Placement); the library moved to Library\MusicLibrary.cs.
 *      The history below stays here for all of them. The speech commands are a table; where a box is decides whom it
 *      serves (Placement). Names no longer shared with the engine (MusicBoxProfile, Profiles, PlayingElsewhere).
 *      Behavior unchanged (MusicSpeechTests).
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: the placeholder owner of old saves' shared settings (Angel Island: World.GetSystemAcct).
 *  10/29/26, Adam (2026 modernization: fixes, 10/25/26 to 10/29/26)
 *      Submitting a sheet you don't own is refused before the sheet is touched (the player lost it), and a songless
 *      sheet is refused. The review prompt's delete deletes the track; typed approvals are dated and handle remasters;
 *      autoplay continues after a library change; free tracks play free. Command words match in any case; a Napster
 *      track's composer has access to it. A song's end is its performance's Completed, run once, and the box won't
 *      start during the pause of a sheet already playing. ReconnectPlayer rebinds copies of library songs to their
 *      owners (it skipped them all), and never when the character's AcctGUID isn't its account's.
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

using Server.Accounting;
using Server.Commands;
using Server.ContextMenus;
using Server.Diagnostics;
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
using static Server.Items.MusicLibrary;

namespace Server.Items
{
    public class MusicBoxProfile
    {
        private List<RolledUpSheetMusic> m_ignoreList = new List<RolledUpSheetMusic>();
        public List<RolledUpSheetMusic> IgnoreList => m_ignoreList;
        private List<YouSoldMusic> m_playerNotificationList = new List<YouSoldMusic>();
        public List<YouSoldMusic> PlayerNotificationList => m_playerNotificationList;
        public MusicBoxProfile(List<RolledUpSheetMusic> ignoreList, List<YouSoldMusic> playerNotificationList)
        {
            m_ignoreList = ignoreList;
            m_playerNotificationList = playerNotificationList;
        }
    }
    [Flipable(0x2AF9, 0x2AFD)]
    public partial class MusicBox : Item, ISecurable
    {
        private Dictionary<Mobile, MusicBoxProfile> Profiles = new();
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
        public bool GMLHasTrackID(RolledUpSheetMusic rsm)
        {
            try
            {
                // Robustness: null input cannot have a valid TrackID.
                if (rsm == null)
                    return false;

                int trackID = rsm.TrackID;

                // Defensive: if repository is null or empty, nothing can match.
                if (MusicLibrary.Repository.Count == 0)
                    return false;

                // Iterate repository keys and look for a matching TrackID.
                // Fail fast on first match.
                foreach (var kvp in MusicLibrary.Repository)
                {
                    // Defensive: kvp.Key may theoretically be null depending on how the repository is built.
                    if (kvp.Key != null && kvp.Key.TrackID == trackID)
                        return true;
                }

                // No match found.
                return false;
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return false; // Fail closed on error.
            }
        }
        #endregion Utilities
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
            try
            {
                // Robustness: invalid inputs cannot have rights.
                if (m == null || rsm == null)
                    return false;

                // An empty library has nothing to compare.
                if (MusicLibrary.Repository.Count == 0)
                    return false;

                // Safely attempt to retrieve the repository entry.
                if (!MusicLibrary.Repository.TryGetValue(rsm, out MusicInfo? info) || info == null)
                    return false;

                // No purchasers list means no explicit rights granted.
                if (info.Purchasers == null || info.Purchasers.Count == 0)
                    return false;

                // Iterate purchasers and check identity match.
                foreach (PurchaserInfo pi in info.Purchasers)
                {
                    // Defensive: pi may theoretically be null depending on list integrity.
                    if (pi != null && pi.Purchaser == m)
                        return true;
                }

                // No matching purchaser found.
                return false;
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return false; // Fail closed on error.
            }
        }
        public bool GlobalRepositoryChanged()
        {
            try
            {
                // Capture current hash once to avoid computing it twice and
                // to ensure consistent comparison vs sync value.
                int currentHash = GetGlobalRepositoryHash();

                // Determine whether the repository state has changed since last check.
                bool changed = (m_globalRepositoryHash != currentHash);

                // Synchronize stored hash with current state.
                m_globalRepositoryHash = currentHash;

                // Emit diagnostic only when a change is detected.
                if (changed)
                    AimsLog.WriteLine(
                        "Global CoreMusicPlayer Repository changed.",
                        ConsoleColor.Cyan);

                return changed;
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return false; // Fail closed: assume no change on error.
            }
        }
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
                        writer.WriteEncodedInt(Profiles.Count);
                        foreach (var mc in Profiles)
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

                            Mobile? m = reader.ReadMobile();
                            int playerNotificationListCount = reader.ReadEncodedInt();
                            for (int jx = 0; jx < playerNotificationListCount; jx++)
                                playerNotificationList.Add(new YouSoldMusic(reader));

                            int ignoreListCount = reader.ReadEncodedInt();
                            for (int mx = 0; mx < ignoreListCount; mx++)
                            {
                                Item? item = reader.ReadItem();
                                if (item != null)
                                    ignoreList.Add(item as RolledUpSheetMusic);
                            }

                            if (m != null)
                                Profiles.Add(m, new MusicBoxProfile(ignoreList, playerNotificationList));
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
                            Item? item = reader.ReadItem();
                            if (item != null)
                                ignoreList.Add(item as RolledUpSheetMusic);
                        }
                        Mobile? system = AimsHost.Current.SystemMobile;     // the old shared settings' placeholder owner
                        if (system != null)
                            Profiles.Add(system, new MusicBoxProfile(ignoreList, playerNotificationList));
                        m_Level = (SecureLevel)reader.ReadEncodedInt();
                        break;
                    }
            }

            if (IsAnimated)
                StopAnimation();
        }
        #endregion Serialization
    }
}