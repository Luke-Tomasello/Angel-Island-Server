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


/* Scripts\Engines\AIMusicSystem\Library\MusicLibrary.Composers.cs
 * Changelog
 *  11/5/26, Adam (2026 modernization, 10/29/26 to 11/5/26)
 *      The library's composers, moved out of MusicBox unchanged but for the names: the scoring factors, reconnecting a
 *      player's music ownership (ReconnectPlayer), music karma (GetMusicKarma), publishing points and sales figures.
 *      Moved from MusicBox (MusicBox.Library.cs); its history before 10/29/26 is in MusicBox.cs.
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: account GUIDs.
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
    }
}