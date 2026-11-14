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


/* Scripts\Engines\AIMusicSystem\Library\MusicLibrary.Composers.cs
 * Changelog
 *  11/8/26, Adam (2026 modernization, 10/29/26 to 11/8/26)
 *      The library's composers, moved out of MusicBox unchanged but for the names: the scoring factors, reconnecting a
 *      player's music ownership (ReconnectPlayer), music karma (GetMusicKarma), publishing points and sales figures.
 *      Moved from MusicBox (MusicBox.Library.cs); its history before 10/29/26 is in MusicBox.cs.
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: account GUIDs.
 *      Music box credit (a sale's price the composer's bank box couldn't take) is paid at last: CollectCredits pays it,
 *      each song's whole, when the composer next uses a music box (speech, double-click, its menu, walking up to it), at
 *      most one try every two minutes. It's paid only to the character and on the shard that earned it: ReconnectPlayer
 *      (a library moved from another shard, a recreated character) and the maintenance pass (composer gone) forfeit it
 *      (ForfeitCredit). Payments and forfeits are written to MusicCredits.log (account, slot, character, song, amount).
 *      Composers hear review decisions: at their next music box visit (ComposerVisit: CollectCredits, then
 *      TellComposer) each decision on their songs they haven't heard is told once (approved, remastered, or not
 *      approved with the reason and staff's note); ComposerSongs lists their songs for the 'status' command, all of
 *      them or those with one status ('status pending|approved|rejected').
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Misc;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public static partial class MusicLibrary
    {
        #region Scoring
        public const double PlayFactor = 1.0;      // one point per play
        public const double PublishFactor = 0.1;   // not too much here as the song may be rubbish
        public const double SalesFactor = 25.0;    // Okay, the song was liked enough to be purchased.
        #endregion Scoring

        #region Ownership
        /// <summary>
        /// Reconnects music ownership for a player after a shard transfer / relink.
        ///
        /// Goals:
        /// 1) In the authoritative music database (Repository), rebind MusicInfo.Owner to the
        ///    current PlayerMobile if the record belongs to the account GUID and the Owner ref is missing/deleted.
        /// 2) In the world, rebind "wild" RolledUpSheetMusic items (not library masters) that belong to this GUID.
        /// 3) Optionally backfill legacy wild RSM items that have OwnerGUID==0 by matching HashCode to the DB.
        /// </summary>
        public static void ReconnectPlayer(Mobile m)
        {
            try
            {
                if (m is not PlayerMobile pm || pm.Deleted)
                    return;

                Account? acct = pm.Account as Account;
                if (acct == null)
                    return;

                uint guid = AimsHost.Current.AccountGuid(acct);
                if (guid == 0)
                    return;

                // The owner setters (MusicInfo.Owner, RolledUpSheetMusic.Owner) stamp OwnerGUID from pm.AcctGUID: a character
                //  whose AcctGUID is not its account's GUID (0, or another identity) would overwrite valid ownership GUIDs.
                if (AimsHost.Current.AccountGuid(pm) != guid)
                {
                    Console.WriteLine(
                        $"[MUSIC RECONNECT ERROR] GUID mismatch: AccountGUID={guid}, PlayerMobileGUID={AimsHost.Current.AccountGuid(pm)}, Player={pm.Name}: nothing reconnected");
                    return;
                }

                int dbScanned = 0;
                int dbRebound = 0;
                int dbSkipped = 0;

                int worldScanned = 0;
                int worldRebound = 0;
                int worldSkipped = 0;

                int worldBackfillGuid = 0;
                int worldBackfillOwner = 0;

                // HashCode -> authoritative OwnerGUID (from DB).
                Dictionary<int, uint> ownerByHash = new Dictionary<int, uint>();

                // Library masters by reference: RSM equality is by HashCode, so ContainsKey() is also true for every copy.
                HashSet<RolledUpSheetMusic> masters = new HashSet<RolledUpSheetMusic>(ReferenceEqualityComparer.Instance);
                foreach (RolledUpSheetMusic key in Repository.Keys)
                    if (key != null)
                        masters.Add(key);

                // 1) Reconnect in the music database (authoritative source)
                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in Repository)
                {
                    RolledUpSheetMusic rsm = kvp.Key;
                    MusicInfo mi = kvp.Value;

                    if (rsm == null || mi == null)
                        continue;

                    if (rsm.Deleted)
                        continue;

                    // Only touch entries belonging to this account identity.
                    if (mi.OwnerGUID != guid)
                    {
                        dbSkipped++;
                        continue;
                    }

                    dbScanned++;

                    // If the owner is missing or points at a deleted mobile, rebind to the current mobile.
                    Mobile existingOwner = mi.Owner;

                    if (existingOwner == null || existingOwner.Deleted)
                    {
                        // credit stays with the character and shard that earned it: a relink (a library moved from another
                        //  shard, or a recreated character) never pays it
                        ForfeitCredit(rsm, mi, string.Format("relinked to a new character, 0x{0:X} {1}", pm.Serial.Value, pm.Name));

                        mi.Owner = pm; // setter should reassert mi.OwnerGUID as pm.GUID (should already match)
                        dbRebound++;
                    }
                    else
                    {
                        // Owner is present and not deleted; leave it alone.
                        dbSkipped++;
                    }

                    // Prepare lookup for legacy wild backfill (OwnerGUID == 0 items).
                    int hash = rsm.HashCode;
                    if (!ownerByHash.ContainsKey(hash))
                        ownerByHash[hash] = mi.OwnerGUID;
                }

                // 2) Reconnect "wild" RSM items owned by this GUID
                if (World.Items != null)
                {
                    foreach (Item item in World.Items.Values)
                    {
                        if (item == null || item.Deleted)
                            continue;

                        if (item is not RolledUpSheetMusic rsm)
                            continue;

                        // Skip library masters (keys in the DB, by reference).
                        if (masters.Contains(rsm))
                            continue;

                        worldScanned++;

                        // Case A: Wild RSM already carries this owner GUID -> rebind owner ref if missing/deleted.
                        //  A songless sheet keeps the GUID it carries but is not given an owner (it is not that song).
                        if (rsm.OwnerGUID == guid && rsm.HasComposition)
                        {
                            Mobile existingOwner = rsm.Owner;

                            if (existingOwner == null || existingOwner.Deleted)
                            {
                                rsm.Owner = pm; // setter should keep/stamp rsm.OwnerGUID
                                worldRebound++;
                            }
                            else
                            {
                                worldSkipped++;
                            }

                            continue;
                        }

                        // Case B (optional): Wild RSM has no GUID yet, but matches a library song owned by this GUID.
                        // This covers legacy dupes that never carried identity metadata. Only a real song identifies one: a
                        //  songless sheet's hash can collide with a real song, and hash 0 is no identity.
                        if (rsm.OwnerGUID == 0 && rsm.HashCode != 0 && rsm.HasComposition)
                        {
                            if (ownerByHash.TryGetValue(rsm.HashCode, out uint ownerGuid) && ownerGuid == guid)
                            {
                                rsm.OwnerGUID = guid;
                                worldBackfillGuid++;

                                Mobile existingOwner = rsm.Owner;
                                if (existingOwner == null || existingOwner.Deleted)
                                {
                                    rsm.Owner = pm;
                                    worldBackfillOwner++;
                                }
                            }
                        }
                    }
                }

                Console.WriteLine(
                    $"Music Reconnect: Player={pm.Name}, GUID={guid} | DB scanned={dbScanned}, rebound={dbRebound}, skipped={dbSkipped} | " +
                    $"World scanned={worldScanned}, rebound={worldRebound}, skipped={worldSkipped}, backfillGuid={worldBackfillGuid}, backfillOwner={worldBackfillOwner}");
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        #endregion Ownership

        #region Music karma and publishing points
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
        /// <summary>
        /// Returns cached "music karma" for a composer. Cache entries refresh at most every 15 minutes.
        ///
        /// Defensive rules:
        /// - m must be non-null and not deleted
        /// - ComposerKarmaCache must exist
        /// - Use TryGetValue to avoid double dictionary lookups
        /// - Fail closed and log on exception
        /// </summary>
        public static int GetMusicKarma(Mobile m)
        {
            try
            {
                if (m == null || m.Deleted)
                    return 0;

                if (ComposerKarmaCache == null)
                    return 0;

                // Fast path: cache hit
                if (ComposerKarmaCache.TryGetValue(m, out KarmaObject? ko) && ko != null)
                {
                    // NOTE: LastChecked is acting as "next refresh time".
                    if (DateTime.UtcNow > ko.LastChecked)
                    {
                        ko.LastChecked = DateTime.UtcNow.AddMinutes(15);
                        ko.Karma = ComputeMusicKarma(m);
                    }

                    return ko.Karma;
                }

                // Cache miss: compute and add.
                int karma = ComputeMusicKarma(m);
                ComposerKarmaCache[m] = new KarmaObject(karma); // overwrite-safe
                return karma;
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return 0;
            }
        }

        /// <summary>
        /// Computes music karma from raw publishing points and scales it.
        ///
        /// NOTE: This intentionally applies a 100x multiplier (legacy behavior).
        /// </summary>
        private static int ComputeMusicKarma(Mobile m)
        {
            try
            {
                if (m == null || m.Deleted)
                    return 0;

                int sales, plays, publications;

                int raw = GetPublishingPointsRaw(m, out sales, out plays, out publications);

                // Preserve existing behavior: karma is raw points scaled by 100.
                return raw * 100;
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return 0;
            }
        }

        /// <summary>
        /// Publishing points used for gating/eligibility (e.g., Bards Guild join).
        ///
        /// Outputs:
        /// - sales: number of units sold across owned tracks
        /// - plays: total play count across owned tracks
        /// - publications: count of approved tracks
        /// </summary>
        public static int GetPublishingPoints(Mobile m, out int sales, out int plays, out int publications)
        {
            sales = 0;
            plays = 0;
            publications = 0;

            try
            {
                if (m == null || m.Deleted)
                    return 0;

                // Reuse raw aggregation and apply weighting.
                GetPublishingPointsRaw(m, out sales, out plays, out publications);

                // Preserve original weighting semantics.
                return (int)((sales * SalesFactor) + (publications * PublishFactor) + (plays * PlayFactor));
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                sales = plays = publications = 0;
                return 0;
            }
        }

        /// <summary>
        /// Raw aggregation of publishing activity.
        ///
        /// Returns:
        /// - raw points = sales + publications + plays (unweighted)
        ///
        /// Defensive rules:
        /// - m must be non-null
        /// - kvp.Value may be null
        /// </summary>
        private static int GetPublishingPointsRaw(Mobile m, out int sales, out int plays, out int publications)
        {
            sales = 0;
            plays = 0;
            publications = 0;

            try
            {
                if (m == null || m.Deleted)
                    return 0;

                if (Repository.Count == 0)
                    return 0;

                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in Repository)
                {
                    MusicInfo info = kvp.Value;

                    if (info == null)
                        continue;

                    if (info.Owner != m)
                        continue;

                    // Sold is units sold (not gold).
                    sales += info.Sold;

                    if (info.Status == SubmissionStatus.Approved)
                        publications++;

                    plays += info.Plays;
                }

                return sales + publications + plays;
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                sales = plays = publications = 0;
                return 0;
            }
        }
        #endregion Music karma and publishing points

        #region Sales
        /// <summary>
        /// Aggregates seller statistics for the specified mobile.
        ///
        /// Outputs:
        ///     totalSales - Total gold earned from all tracks owned by this mobile.
        ///     sold       - Total number of tracks sold.
        ///     credit     - Gold owed (bank overflow / deferred credit).
        ///
        /// Defensive rules:
        /// - m must not be null
        /// - MusicInfo entries must not be null
        /// - Fail closed and log on exception
        /// </summary>
        public static void CalculateSellerData(Mobile m, out int totalSales, out int sold, out int credit)
        {
            totalSales = 0;
            sold = 0;
            credit = 0;

            try
            {
                if (m == null)
                    return;

                if (Repository.Count == 0)
                    return;

                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in Repository)
                {
                    MusicInfo info = kvp.Value;

                    if (info == null)
                        continue;

                    // Only aggregate tracks owned by this mobile.
                    if (info.Owner == m)
                    {
                        totalSales += info.TotalSales;
                        sold += info.Sold;
                        credit += info.Credit;
                    }
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);

                // Fail closed: leave outputs as zeroed.
                totalSales = 0;
                sold = 0;
                credit = 0;
            }
        }
        /// <summary>
        /// Aggregated competition stats for a single seller.
        ///
        /// TotalSales - cumulative gold earned.
        /// Sold       - cumulative units sold.
        /// </summary>
        private record Competition
        {
            public int TotalSales = 0;
            public int Sold = 0;

            public Competition(int totalSales, int sold)
            {
                TotalSales = totalSales;
                Sold = sold;
            }
        }
        /// <summary>
        /// Calculates the top competitor statistics relative to the supplied mobile.
        ///
        /// Outputs:
        ///     totalSales - Highest TotalSales among all other sellers.
        ///     sold       - Highest Sold count among all other sellers.
        ///
        /// Notes:
        /// - Excludes the provided mobile (m).
        /// - Excludes null owners.
        /// - Aggregates per owner first, then extracts maxima.
        /// - Fails closed on error.
        /// </summary>
        public static void CalculateCompetitorData(Mobile m, out int totalSales, out int sold)
        {
            totalSales = 0;
            sold = 0;

            try
            {
                if (Repository.Count == 0)
                    return;

                // Aggregate per-owner stats.
                Dictionary<Mobile, Competition> stats = new Dictionary<Mobile, Competition>();

                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in Repository)
                {
                    MusicInfo info = kvp.Value;

                    if (info == null)
                        continue;

                    Mobile owner = info.Owner;

                    // Skip:
                    // - current mobile
                    // - null owners
                    if (owner == null || owner == m)
                        continue;

                    if (!stats.TryGetValue(owner, out Competition? comp))
                    {
                        stats[owner] = new Competition(info.TotalSales, info.Sold);
                    }
                    else
                    {
                        comp.TotalSales += info.TotalSales;
                        comp.Sold += info.Sold;
                    }
                }

                // Extract maxima.
                foreach (KeyValuePair<Mobile, Competition> entry in stats)
                {
                    Competition comp = entry.Value;
                    if (comp == null)
                        continue;

                    if (comp.TotalSales > totalSales)
                        totalSales = comp.TotalSales;

                    if (comp.Sold > sold)
                        sold = comp.Sold;
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);

                // Fail closed.
                totalSales = 0;
                sold = 0;
            }
        }
        #endregion Sales

        #region Credit
        // Credit is a sale's price the composer's bank box couldn't take (MusicBox.Prompts.cs). It's paid to the composer
        //  who earned it, on the shard it was earned on, the next time they use a music box (CollectCredits), and nowhere
        //  else: when a record's composer is gone, or the record is relinked to a new character (a library moved to another
        //  shard, a character recreated), its credit is forfeited (ForfeitCredit). Payments and forfeits go to
        //  MusicCredits.log.
        public const string CreditLog = "MusicCredits.log";

        // one attempt per composer per two minutes: a failed deposit makes, then deletes, a check or a pile of gold
        private const double CreditRetrySeconds = 120;

        /// <summary>
        /// Pays the music box credit owed to this composer, if any: each song's credit whole, into their bank (AimsHost.Deposit),
        /// and tells them. Called whenever they use a music box (speech, double-click, its menu, walking up to it).
        /// </summary>
        public static void CollectCredits(Mobile m)
        {
            try
            {
                if (m == null || m.Deleted || !m.Player)
                    return;

                List<KeyValuePair<RolledUpSheetMusic, MusicInfo>>? owed = null;
                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in Repository)
                {
                    if (kvp.Key != null && !kvp.Key.Deleted && kvp.Value != null && kvp.Value.Credit > 0 && kvp.Value.Owner == m)
                        (owed ??= new List<KeyValuePair<RolledUpSheetMusic, MusicInfo>>()).Add(kvp);
                }

                // nothing owed: no deposit, and no throttle entry either
                if (owed == null || !AimsUtil.AntiSpam("music credit " + m.Serial.Value, CreditRetrySeconds))
                    return;

                uint guid = AimsHost.Current.AccountGuid(m);
                int paid = 0, waiting = 0;
                List<object> lines = new List<object>();

                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in owed)
                {
                    MusicInfo info = kvp.Value;
                    int amount = info.Credit;

                    // the record says another account composed it: not this character's to collect
                    if (info.OwnerGUID != 0 && info.OwnerGUID != guid)
                    {
                        lines.Add(string.Format("not paid: {0:N0} for {1}, the record's account GUID {2} isn't this character's ({3})",
                            amount, DescribeSong(kvp.Key), info.OwnerGUID, guid));
                        continue;
                    }

                    if (AimsHost.Current.Deposit(m, amount))
                    {
                        info.Credit = 0;
                        paid += amount;
                        lines.Add(string.Format("paid: {0:N0} for {1}", amount, DescribeSong(kvp.Key)));
                    }
                    else
                        waiting += amount;          // the bank box is still full: it stays owed (not logged: it would repeat)
                }

                if (lines.Count > 0)
                    AimsHost.Current.WriteLog(CreditLog, m, Prepend(DescribeComposer(m), lines));

                if (paid > 0)
                    m.SendMessage(54, string.Format("{0:N0} gold from sales of your music has been deposited in your bank box.", paid));

                if (waiting > 0)
                    m.SendMessage(54, string.Format("{0:N0} gold from sales of your music is waiting for you, but your bank box is full. " +
                        "Make room, then visit a music box again.", waiting));
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }

        /// <summary>
        /// Writes off a song's credit (nothing if none is owed), with the reason, in MusicCredits.log: its composer is gone,
        /// or it was relinked to a new character. The amount is never paid anywhere.
        /// </summary>
        public static void ForfeitCredit(RolledUpSheetMusic? rsm, MusicInfo? info, string reason)
        {
            try
            {
                if (info == null || info.Credit == 0)
                    return;

                int amount = info.Credit;
                info.Credit = 0;

                AimsHost.Current.WriteLog(CreditLog, info.Owner, new object[]
                {
                    DescribeComposer(info.Owner),
                    string.Format("forfeited: {0:N0} for {1}, composer's account GUID {2}: {3}", amount, DescribeSong(rsm), info.OwnerGUID, reason)
                });
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }

        // "'Title' (0x40089B9C)"
        private static string DescribeSong(RolledUpSheetMusic? rsm)
        {
            return rsm == null ? "(no sheet)" : string.Format("'{0}' (0x{1:X})", rsm, rsm.Serial.Value);
        }

        // "account adam, slot 2, 0x119 Galois" (what a log reader needs to find the player)
        private static string DescribeComposer(Mobile? m)
        {
            if (m == null)
                return "composer: none (no character)";

            string account = "none";
            int slot = -1;
            if (m.Account is Account acct)
            {
                account = acct.Username;
                for (int i = 0; i < acct.Length; i++)
                    if (acct[i] == m)
                        slot = i + 1;
            }

            return string.Format("composer: account {0}, slot {1}, 0x{2:X} {3}{4}",
                account, slot > 0 ? slot.ToString() : "?", m.Serial.Value, m.Name, m.Deleted ? " (deleted)" : "");
        }

        private static IEnumerable<object> Prepend(object first, IEnumerable<object> rest)
        {
            yield return first;
            foreach (object o in rest)
                yield return o;
        }
        #endregion Credit

        #region Review news
        /// <summary>
        /// A composer uses a music box (speech, double-click, its menu, walking up to it): the credit owed them is paid
        /// (CollectCredits) and they hear the review decisions on their songs they haven't heard yet (TellComposer).
        /// </summary>
        public static void ComposerVisit(Mobile m)
        {
            CollectCredits(m);
            TellComposer(m);
        }

        /// <summary>
        /// Tells the composer, once each, the review decisions on their songs they haven't heard: "Your song X was
        /// approved!", "Your remaster of X was approved!", "Your song X wasn't approved: reason." (with staff's note).
        /// </summary>
        public static void TellComposer(Mobile m)
        {
            try
            {
                if (m == null || m.Deleted || !m.Player || m.NetState == null)
                    return;

                bool rejected = false;
                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in Repository)
                {
                    MusicInfo info = kvp.Value;
                    if (kvp.Key == null || kvp.Key.Deleted || info == null || info.Owner != m || info.ComposerTold
                        || info.Status == SubmissionStatus.Pending)
                        continue;

                    if (info.Status == SubmissionStatus.Approved)
                        m.SendMessage(54, info.Remastered ? "Your remaster of '{0}' was approved!" :
                            "Your song '{0}' was approved! It's in the music library now.", kvp.Key);
                    else
                    {
                        m.SendMessage(54, "Your song '{0}' wasn't approved: {1}", kvp.Key, Sentence(ReviewReason(info)));
                        rejected = true;
                    }
                    info.ComposerTold = true;
                }

                if (rejected)
                    m.SendMessage(54, "Say 'status' to a music box to see all your songs.");
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }

        /// <summary>
        /// The composer's songs for the 'status' command, one line each (by title): waiting for review, approved,
        /// remastered, or not approved with the reason and staff's note; only those with the given status when one is
        /// given ('status rejected'). The decisions shown count as told.
        /// </summary>
        public static List<string> ComposerSongs(Mobile m, SubmissionStatus? only = null)
        {
            List<KeyValuePair<RolledUpSheetMusic, MusicInfo>> mine = new List<KeyValuePair<RolledUpSheetMusic, MusicInfo>>();
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in Repository)
                if (kvp.Key != null && !kvp.Key.Deleted && kvp.Value != null && m != null && kvp.Value.Owner == m
                    && (only == null || kvp.Value.Status == only))
                    mine.Add(kvp);
            mine.Sort((a, b) => string.Compare(a.Key.ToString(), b.Key.ToString(), StringComparison.OrdinalIgnoreCase));

            List<string> lines = new List<string>();
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in mine)
            {
                MusicInfo info = kvp.Value;
                switch (info.Status)
                {
                    case SubmissionStatus.Pending:
                        lines.Add(string.Format("'{0}': waiting for review (submitted {1})", kvp.Key, Day(info.DateSubmitted)));
                        break;
                    case SubmissionStatus.Approved:
                        lines.Add(info.Remastered
                            ? string.Format("'{0}': approved; remastered {1}", kvp.Key, Day(info.DateRemastered))
                            : string.Format("'{0}': approved {1}", kvp.Key, Day(info.DateApproved)));
                        info.ComposerTold = true;
                        break;
                    default:
                        lines.Add(string.Format("'{0}': not approved: {1}", kvp.Key, Sentence(ReviewReason(info))));
                        info.ComposerTold = true;
                        break;
                }
            }
            return lines;
        }

        /// <summary>Why a song wasn't approved, for its composer: the reason, then staff's note if there is one.</summary>
        public static string ReviewReason(MusicInfo info)
        {
            string reason = info.Reason switch
            {
                RejectionReason.TooLong => "it's too long",
                RejectionReason.TooShort => "it's too short",
                RejectionReason.Inappropriate => "it isn't appropriate",
                RejectionReason.Duplicate => "it's already in the library",
                _ => "",
            };
            if (info.Note != null)
                return reason.Length == 0 ? info.Note : reason + ". " + info.Note;
            return reason.Length == 0 ? "no reason was given" : reason;
        }

        // text that ends a sentence ends with a period (a note may have its own)
        private static string Sentence(string text)
        {
            text = text.TrimEnd();
            return text.Length > 0 && ".!?".IndexOf(text[text.Length - 1]) >= 0 ? text : text + ".";
        }

        // a date for a composer ("11/8/2026"); "(date unknown)" for a record saved before it was kept
        private static string Day(DateTime utc)
        {
            return utc == DateTime.MinValue || utc.Year < 2000 ? "(date unknown)" : utc.ToString("M/d/yyyy");
        }
        #endregion Review news
    }
}