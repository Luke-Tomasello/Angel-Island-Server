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
 *  11/20/26, Adam (cron playback break)
 *      Let Cron take a normal processing pass between autoplay tracks.
 *  11/19/26, Adam (full plays)
 *      FullPlays on the Music Conservatory: every library song in full, Napster tracks too; plays earn nothing.
 *  11/14/26, Adam (arranged by)
 *      Arranger: AIMS names a song's arranger after its author (its owner, when the Author names someone else). Here a
 *      song held for its composer on another shard names that composer (MusicShare.ComposerOf), and an Author that is
 *      one of the owner's characters, in their household, or a name their account composed under elsewhere, names no
 *      arranger. The "New music" post names the arranger too. AuthorRefusal compares names with AimsHost.SameName.
 *  11/14/26, Adam (the Author may not name another player)
 *      RefuseSubmission refuses a song whose Author names another player (AuthorRefusal): a character on this shard
 *      passes only on the submitter's account or in their household (IsHousehold); a composer known only on another
 *      shard (the shared music database) only for the same account. At most 40 characters. Staff are exempt. Money
 *      follows the sheet's owner, so a copy credited to its composer sold under their name for the copier.
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
        public override void BeforeAutoplay() => Server.Engines.CronScheduler.Cron.ProcessPlaybackBreak();

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
        // the Music Conservatory is a school: every song in full, plays earn nothing
        public override bool FullPlays
        {
            get { return Core.GMNMC_CFG; }
        }

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
            string? arranger = Arranger(song, info);
            if (by.Length > 0 && arranger != null)
                by += ", arranged by " + arranger;
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

        // who arranged a song, named after its author: AIMS's rule (its owner, when the Author names someone else), with
        //  Angel Island's. A song held for its composer on another shard (owned by the system account) names that composer
        //  (the shared database's name). An Author that is one of the owner's own characters, or a character in their
        //  household, names no arranger (Jocelyn submitting as "Galois", Galois's other account, stays "by Galois"); nor
        //  does one the owner's account has composed under on another shard.
        public override string? Arranger(Misc.RolledUpSheetMusic song, Items.MusicLibrary.MusicInfo? info)
        {
            if (song == null || info == null || info.Imported || string.IsNullOrWhiteSpace(song.Author))
                return null;
            if (info.Owner != null && info.Owner == SystemMobile)
            {
                if (Engines.MusicShare.MusicShare.ComposerOf(song.HashCode) is not (string Name, uint Guid) composer
                    || SameName(composer.Name, song.Author) || Engines.MusicShare.MusicShare.ComposedAs(composer.Guid, song.Author))
                    return null;
                return composer.Name;
            }
            string? arranger = base.Arranger(song, info);
            if (arranger == null || info.Owner is not Mobile owner)
                return null;
            if (Engines.MusicShare.MusicShare.ComposedAs(AccountGuid(owner), song.Author))
                return null;
            foreach (PlayerMobile pm in PlayerMobile.Instances)
                if (pm != null && !pm.Deleted && SameName(pm.Name, song.Author) && pm.Account != null
                    && (pm.Account == owner.Account || SameHousehold(owner, pm)))
                    return null;
            return arranger;
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
            if (composer != null && sheet != null && AuthorRefusal(composer, sheet.Author) is string refused)
                return refused;
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

        public const int MaxAuthorLength = 40;

        // a song's Author is credit text ("Beethoven", a band, the composer's own name), but money follows the sheet's
        //  owner: a song submitted as another player's would be sold under their name with the gold going to the
        //  submitter. So the Author may not name another player: a character on this shard passes only if it is on the
        //  submitter's account or in their household (accounts sharing an IP, the rule music credit uses); a composer
        //  known only on another shard (the shared music database) passes only for the same account. Staff are exempt.
        //  Names compare whole (trimmed, spaces collapsed, any case): "Galois & Jocelyn" is credit text. Null: no objection.
        public string? AuthorRefusal(Mobile composer, string? author)
        {
            if (composer == null || composer.AccessLevel > AccessLevel.Player)
                return null;

            string name = PlainName(author);
            if (name.Length == 0)
                return null;
            if (name.Length > MaxAuthorLength)
                return string.Format("An author's name is at most {0} characters.", MaxAuthorLength);

            string refusal = string.Format("{0} is another player's name: a song you submit can't be credited to them.", name);
            uint guid = AccountGuid(composer);

            // a character of that name on this shard
            bool found = false;
            foreach (PlayerMobile pm in PlayerMobile.Instances)
            {
                if (pm == null || pm.Deleted || !SameName(pm.Name, name))
                    continue;
                found = true;
                if (pm.Account != null && (pm.Account == composer.Account || SameHousehold(composer, pm)))
                    return null;
            }
            if (found)
                return refusal;

            // a composer known only on another shard: the same account, or refused. The database unreadable: no objection
            //  from here (a database problem doesn't stop every submission)
            if (!Engines.MusicShare.MusicShare.Enabled)
                return null;
            List<Engines.MusicShare.SongRow>? songs = Engines.MusicShare.MusicShareDatabase.GetSongs();
            if (songs == null)
            {
                Utility.Monitor.WriteLine("Music: AuthorRefusal couldn't read the shared music database; '{0}' not checked there.", ConsoleColor.Yellow, name);
                return null;
            }
            foreach (Engines.MusicShare.SongRow row in songs)
            {
                if (row.Deleted || !SameName(row.ComposerName, name))
                    continue;
                if (row.ComposerGuid == guid)
                    return null;            // the submitter's own name there (two players of one name: theirs is enough)
                found = true;
            }
            return found ? refusal : null;
        }

        private bool SameHousehold(Mobile composer, Mobile other)
        {
            try
            {
                return IsHousehold(composer, other);
            }
            catch (Exception ex)
            {
                LogException(ex, null);
                return false;
            }
        }
        #endregion Events
    }
}
