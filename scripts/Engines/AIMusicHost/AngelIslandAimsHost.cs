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


/* Scripts\Engines\AIMusicHost\AngelIslandAimsHost.cs
 * Changelog
 *  11/14/26, Adam (a synced decision is final)
 *      QuietApprovals: while music sharing re-approves a song the world lost to a crash or rollback, OnTrackApproved
 *      posts nothing (the song's "New music" post went out when it was first approved).
 *  11/9/26, Adam (review notice)
 *      ReviewNeeded: while music is shared and this shard writes it, AIMS's notice is off: the sync's notice covers every
 *      shard's songs awaiting review, this one's among them, for staff and reviewers. Otherwise AIMS's own notice.
 *  11/9/26, Adam (music reviewers)
 *      RefuseSubmission: a review copy ([MusicShare / [MusicReview listen: MusicShare.IsReviewCopy) can't be added to a
 *      music box, so a reviewer (a player) can't submit a song awaiting review as theirs.
 *  11/7/26, Adam
 *      Music shared between shards (Engines\AIMusicShare): Deposit to the system account (a song held for a composer on
 *      another shard) pays the LB fund; OnMusicPurchased records such a purchase for the composer's recognition at home.
 *      TrackNote: staff looking at such a song see its home shard, in the system message 'find' sends (and 'who' sends
 *      them too); players hear the box as always. RefuseSubmission: such a song is
 *      remastered at its home, not here ("Golden's home is Siege Perilous: remaster it there.").
 *  11/6/26, Adam (11/5/26 to 11/6/26)
 *      Angel Island's AimsHost: wires the music system (Scripts\Engines\AIMusicSystem) to Angel Island's systems.
 *      It lives outside the AIMS folder, so the AIMS folder drops into other RunUO servers unchanged; a shard without
 *      one gets AimsHost's defaults. Every member does what AIMS did here before it was portable:
 *      Logging: LogHelper.LogException, Utility.Monitor, and log files through LogHelper. Staff messages:
 *      Item.SendMessageNearby and Utility.SendSystemMessage. Folders: Core.DataDirectory and Core.LogsDirectory.
 *      Identity: the account's GUID (Mobile.AcctGUID), households by accounts shared by IP. Music Karma:
 *      PlayerMobile.NpcGuildPoints; the music filter: PlayerMobile.FilterMusic. Purchases: all enrolled bank accounts.
 *      Copies: Utility.Dupe, staff copies tracked (Utility.TrackStaffOwned), temporary items (TempObject, staff owned:
 *      cron's safety net), internal storage (MoveToIntStorage), the system account. Settings: Test Center
 *      (Core.GMNTC_CFG), [play access (CoreAI.PlayAccessLevel). Sales: the data recorder's leaderboards (music gold and
 *      one sale).
 *  11/11/2026, Adam
 *      OnTrackApproved: new and remastered songs go to the Herald (Interest.Music, for Discord), gathered for 30
 *      seconds so 'approve *' makes one post.
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Engines.DataRecorder;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace Server
{
    public class AngelIslandAimsHost : AimsHost
    {
        #region Logging
        public override void LogException(Exception ex, string? message)
        {
            if (message != null)
                LogHelper.LogException(ex, message);
            else
                LogHelper.LogException(ex);
        }

        public override void WriteLine(string text, ConsoleColor color)
        {
            Utility.Monitor.WriteLine(text, color);
        }

        public override void WriteLog(string fileName, Mobile? about, IEnumerable<object> lines)
        {
            LogHelper logger = new LogHelper(fileName, false, false);
            if (about != null)
                logger.Log(LogType.Mobile, about);
            foreach (object line in lines)
                logger.Log(line);
            logger.Finish();
        }
        #endregion Logging

        #region Messages
        public override void SendMessageNearby(Item item, string text, AccessLevel accesslevel = AccessLevel.GameMaster, int hue = 0x3B2)
        {
            item.SendMessageNearby(text, accesslevel, hue);
        }

        public override void SendSystemMessage(IEntity source, double second_timeout, string text, AccessLevel accesslevel = AccessLevel.GameMaster, int hue = 0x3B2)
        {
            Utility.SendSystemMessage(source, second_timeout, text, accesslevel, hue);
        }
        #endregion Messages

        #region Files
        public override string DataDirectory
        {
            get { return Core.DataDirectory; }
        }

        public override string LogsDirectory
        {
            get { return Core.LogsDirectory; }
        }
        #endregion Files

        #region Identity
        public override uint AccountGuid(IAccount? acct)
        {
            return (acct as Account)?.GUID ?? 0;
        }

        // no Deleted check: a sheet's TrackID uses its owner's GUID as it is
        public override uint AccountGuid(Mobile? m)
        {
            return m != null ? m.AcctGUID : 0;
        }

        // accounts shared by IP (the owner's last game login); the caller catches and fails closed
        public override bool IsHousehold(Mobile owner, Mobile other)
        {
            if (owner.Account is not Account acct)
                return false;

            IPAddress ip = acct.LastGAMELogin;

            if (ip == null)
                return false;

            ArrayList list = Utility.GetSharedAccounts(ip);

            if (list == null || list.Count == 0)
                return false;

            if (other.Account == null)
                return false;

            return list.Contains(other.Account);
        }
        #endregion Identity

        #region Player data
        public override double GetMusicKarma(Mobile m)
        {
            return m is PlayerMobile pm ? pm.NpcGuildPoints : 0.0;
        }

        public override void SetMusicKarma(Mobile m, double value)
        {
            if (m is PlayerMobile pm)
                pm.NpcGuildPoints = value;
        }

        public override bool GetFilterMusic(Mobile m)
        {
            return m is PlayerMobile pm && pm.FilterMusic;
        }

        public override void SetFilterMusic(Mobile m, bool value)
        {
            if (m is PlayerMobile pm)
                pm.FilterMusic = value;
        }
        #endregion Player data

        #region Gold
        public override bool Withdraw(Mobile buyer, int amount)
        {
            return Banker.CombinedWithdrawFromAllEnrolled(buyer, amount);
        }

        // a song held for a composer on another shard (owned by the system account) pays this shard's LB fund: no gold
        //  crosses shards (Engines\AIMusicShare); any other composer, their bank box
        public override bool Deposit(Mobile composer, int amount)
        {
            if (composer != null && composer == SystemMobile)
            {
                Engines.BountyKeeper.LBFund += amount;
                return true;
            }
            return base.Deposit(composer!, amount);
        }
        #endregion Gold

        #region Items
        public override Item? Dupe(Item item)
        {
            return Utility.Dupe(item);
        }

        // TrackStaffOwned marks the copy staff owned and logs it (staff dupe.log)
        public override void MarkStaffOwned(Mobile staff, Item copy)
        {
            Utility.TrackStaffOwned(staff, copy);
        }

        public override void MarkTemporary(Item item)
        {
            item.SetItemBool(Item.ItemBoolTable.TempObject, true);  // okay to delete
            item.IsStaffOwned = true;                               // keeps other rules from deleting it
        }

        public override void MoveToInternal(Item item)
        {
            item.MoveToIntStorage();
        }

        public override Mobile? SystemMobile
        {
            get { return World.GetSystemAcct(); }
        }
        #endregion Items

        #region Settings
        public override bool IsTestCenter
        {
            get { return Core.GMNTC_CFG; }
        }

        public override AccessLevel PlayAccessLevel
        {
            get { return CoreAI.PlayAccessLevel; }
        }
        #endregion Settings

        #region Events
        // the leaderboards: gold earned and one more sale
        public override void OnMusicSold(Mobile composer, int price)
        {
            DataRecorder.Add(composer, RecorderDomain.MusicGold, price);
            DataRecorder.Add(composer, RecorderDomain.MusicPoints, 1);
        }

        public override bool HasLeaderboard
        {
            get { return true; }
        }

        // a purchase of a song held for a composer on another shard is recorded for their recognition at home
        public override void OnMusicPurchased(Mobile buyer, Misc.RolledUpSheetMusic song, Items.MusicLibrary.MusicInfo info, int price)
        {
            Engines.MusicShare.MusicShare.OnPurchase(buyer, song, info, price);
        }

        // New music for Discord (Herald, Interest.Music): approvals within 30 seconds make one post ('approve *').
        //  Only the approving (home) shard hears this: other shards' copies are built approved, not approved.
        private static readonly List<string> s_Approved = new();

        /// <summary>
        /// While true, an approval isn't news: music sharing re-approving a song the world lost to a crash or rollback (its
        /// post went out the first time). Set only around that call (MusicShare: ActKind.Adopt), on the game thread.
        /// </summary>
        public static bool QuietApprovals { get; set; }

        public override void OnTrackApproved(Mobile approver, Misc.RolledUpSheetMusic song, Items.MusicLibrary.MusicInfo info, bool remaster)
        {
            if (QuietApprovals)
                return;
            string title = string.IsNullOrWhiteSpace(song.SongName) ? song.ToString() : song.SongName;
            string by = string.IsNullOrWhiteSpace(song.Author) ? (song.Owner?.Name ?? "") : song.Author;
            s_Approved.Add(string.Format("{0}\"{1}\"{2}", remaster ? "(remastered) " : "", title, by.Length > 0 ? " by " + by : ""));
            if (s_Approved.Count == 1)
                Timer.DelayCall(TimeSpan.FromSeconds(30), () =>
                {
                    string list = string.Join("; ", s_Approved);
                    s_Approved.Clear();
                    // music is shared by every shard: no shard name
                    Herald.Publish(Interest.Music, Audience.World, string.Format("\U0001F3B5 New music: {0}.", list));
                });
        }

        // staff looking at a song held for a composer on another shard ('find'; 'who' for Owners) see where it comes from
        public override string? TrackNote(Mobile viewer, Misc.RolledUpSheetMusic song, Items.MusicLibrary.MusicInfo? info)
        {
            if (viewer == null || viewer.AccessLevel < AccessLevel.GameMaster || info == null || info.Owner == null || info.Owner != SystemMobile)
                return null;
            string? home = Engines.MusicShare.MusicShare.HomeOf(song.HashCode);
            return home != null ? "from " + Engines.MusicShare.MusicShare.ShardName(home) : "held for a composer on another shard";
        }

        // songs submitted here await review: while music is shared (and this shard writes it), the sync's review notice
        //  covers every shard's songs, this one's among them, for staff and reviewers (MusicShare.NotifyPending); otherwise
        //  AIMS's own notice, to staff
        public override void ReviewNeeded(int count)
        {
            if (!Engines.MusicShare.MusicShare.Enabled || !Engines.MusicShare.MusicShare.CanWrite)
                base.ReviewNeeded(count);
        }

        // a song held for its composer on another shard is remastered at its home: a remaster here (or the same song
        //  submitted again as new) would give it a second home. A review copy ([MusicShare / [MusicReview listen) of a
        //  song awaiting review is for hearing: submitted, it would be someone else's song under the holder's name.
        public override string? RefuseSubmission(Mobile composer, Misc.RolledUpSheetMusic sheet, bool remaster)
        {
            if (Engines.MusicShare.MusicShare.IsReviewCopy(sheet))
                return "That's a review copy of a song awaiting review: it can't be added to a music box.";
            if (composer == null || sheet == null || SystemMobile is not Mobile system || !Engines.MusicShare.MusicShare.Enabled)
                return null;
            uint guid = AccountGuid(composer);
            foreach (KeyValuePair<Misc.RolledUpSheetMusic, Items.MusicLibrary.MusicInfo> kvp in Items.MusicLibrary.Repository)
            {
                if (kvp.Key == null || kvp.Value == null || kvp.Value.Owner != system || kvp.Value.OwnerGUID != guid)
                    continue;
                if (!string.Equals(kvp.Key.Title, sheet.Title, StringComparison.OrdinalIgnoreCase))
                    continue;
                string? home = Engines.MusicShare.MusicShare.HomeOf(kvp.Key.HashCode);
                return string.Format("{0}'s home is {1}: {2} it there.", kvp.Key.Title,
                    home != null ? Engines.MusicShare.MusicShare.ShardName(home) : "another shard", remaster ? "remaster" : "submit a new version of");
            }
            return null;
        }
        #endregion Events
    }
}
