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

/* Scripts\Engines\Seasons\SeasonalHelper.cs
 * ChangeLog
 *  3/27/2026, Adam
 *      First time checkin.
 */


using Server.Accounting;
using Server.Engines.DataRecorder;
using System;
using System.Collections.Generic;

namespace Server.Engines.Seasons
{
    #region Seasons

    /// <summary>
    /// Seasonal Accounting Module
    ///
    /// Purpose:
    /// Converts cumulative gameplay earnings (recorded in DataRecorder) into
    /// seasonal point progression using a delta-based, per-category model.
    ///
    /// Responsibilities:
    /// - Build a per-account snapshot of cumulative earnings from DataRecorder
    /// - Track per-category credited baselines (what has already been converted)
    /// - Compute deltas between current totals and credited baselines
    /// - Apply category-specific multipliers (PvM, Chest, Theft, BODs, Music)
    /// - Convert eligible gold into seasonal points
    /// - Apply banded progression scaling (BPS) around the seasonal threshold
    /// - Update SeasonalAccount point totals
    ///
    /// Key Design Principles:
    /// - Delta-Based Accounting:
    ///   Only newly earned gold since the last processing pass is eligible for conversion.
    ///   Previously processed gold is never re-credited.
    ///
    /// - Per-Category Isolation:
    ///   Each earning stream is tracked and processed independently:
    ///     PvM Loot, Chest Loot, Theft, BOD professions, Music, etc.
    ///   This allows precise tuning via independent multipliers.
    ///
    /// - Non-Deferred Conversion:
    ///   Gold is evaluated once at the time of processing.
    ///   Any value suppressed by diminishing returns or caps is not banked
    ///   or recoverable later.
    ///
    /// - Forward-Only Progression:
    ///   Credited baselines do not rewind automatically if recorder values decrease.
    ///   This prevents double-crediting and preserves accounting integrity.
    ///
    /// Separation of Concerns:
    /// - DataRecorder:
    ///   Source of truth for raw gameplay earnings (gold totals).
    ///
    /// - Seasonal Accounting (this module):
    ///   Converts recorder totals into seasonal points using delta logic.
    ///
    /// - Leaderboard System:
    ///   Independently reads DataRecorder for rankings and display.
    ///   This module does not influence leaderboard calculations.
    ///
    /// Execution Model:
    /// - Invoked periodically (currently after leaderboard job execution)
    /// - Stateless snapshot + stateful baseline = deterministic point updates
    ///
    /// Notes:
    /// - SnapshotTotals is an internal accounting structure and should not be used
    ///   for presentation or leaderboard display.
    /// - Any "total earned gold" display logic belongs in the leaderboard layer,
    ///   not in this module.
    ///
    /// </summary>

    #endregion Seasons
    public static class SeasonalHelper
    {
        private class SnapshotTotals
        {
            public int MusicGold;
            public int SmithBODGold;
            public int TailorBODGold;
            public int CarpenterBODGold;
            public int PvMLootGold;
            public int ChestGold;
            public int TheftGold;
        }
        // Called after the leaderboard job runs.
        // Builds a per-account snapshot from DataRecorder, compares it against
        // credited seasonal baselines, and converts newly earned category deltas
        // into seasonal point credits.
        public static void UpdateSeasonalCreditsFromSnapshot()
        {
            if (!Core.RuleSets.SeasonsRules())
                return;

            Dictionary<uint, SnapshotTotals> snapshot = BuildSnapshot();

            foreach (KeyValuePair<uint, SnapshotTotals> kvp in snapshot)
            {
                uint guid = kvp.Key;
                SnapshotTotals current = kvp.Value;

                SeasonalAccount sa = SeasonalAccounts.GetAccount(guid);
                if (sa == null)
                    continue;

                SeasonalEarningBaselineEntry state = SeasonalEarningBaselineState.GetEntry(guid);

                state.MusicGoldCredited = ProcessCategory(
                    sa,
                    current.MusicGold,
                    state.MusicGoldCredited,
                    CoreAI.SeasonsMusicMultiplier);

                state.SmithBODGoldCredited = ProcessCategory(
                    sa,
                    current.SmithBODGold,
                    state.SmithBODGoldCredited,
                    CoreAI.SeasonsSmithBODMultiplier);

                state.TailorBODGoldCredited = ProcessCategory(
                    sa,
                    current.TailorBODGold,
                    state.TailorBODGoldCredited,
                    CoreAI.SeasonsTailorBODMultiplier);

                state.CarpenterBODGoldCredited = ProcessCategory(
                    sa,
                    current.CarpenterBODGold,
                    state.CarpenterBODGoldCredited,
                    CoreAI.SeasonsCarpenterBODMultiplier);

                state.PvMLootGoldCredited = ProcessCategory(
                    sa,
                    current.PvMLootGold,
                    state.PvMLootGoldCredited,
                    CoreAI.SeasonsPvMLootMultiplier);

                state.ChestGoldCredited = ProcessCategory(
                    sa,
                    current.ChestGold,
                    state.ChestGoldCredited,
                    CoreAI.SeasonsChestMultiplier);

                state.TheftGoldCredited = ProcessCategory(
                    sa,
                    current.TheftGold,
                    state.TheftGoldCredited,
                    CoreAI.SeasonsTheftMultiplier);
            }
        }

        private static Dictionary<uint, SnapshotTotals> BuildSnapshot()
        {
            Dictionary<uint, SnapshotTotals> table = new Dictionary<uint, SnapshotTotals>();

            AccumulateMusic(table);
            AccumulateBODs(table);
            AccumulateLoot(table);

            return table;
        }

        private static void AccumulateMusic(Dictionary<uint, SnapshotTotals> table)
        {
            foreach (var kvp in DataRecorder.DataRecorder.Database)
            {
                Mobile m = kvp.Key;
                if (m == null || m.Account is not Account acct)
                    continue;

                SnapshotTotals totals = GetSnapshot(table, acct.GUID);

                double musicGold = DataRecorder.DataRecorder.Get(m, RecorderDomain.MusicGold);
                totals.MusicGold += Math.Max(0, (int)musicGold);
            }
        }

        private static void AccumulateBODs(Dictionary<uint, SnapshotTotals> table)
        {
            foreach (var kvp in DataRecorder.DataRecorder.Database)
            {
                Mobile m = kvp.Key;
                if (m == null || m.Account is not Account acct)
                    continue;

                SnapshotTotals totals = GetSnapshot(table, acct.GUID);

                double smithGold = DataRecorder.DataRecorder.Get(m, RecorderDomain.SmithBODGold);
                double tailorGold = DataRecorder.DataRecorder.Get(m, RecorderDomain.TailorBODGold);
                double carpenterGold = DataRecorder.DataRecorder.Get(m, RecorderDomain.CarpenterBODGold);

                totals.SmithBODGold += Math.Max(0, (int)smithGold);
                totals.TailorBODGold += Math.Max(0, (int)tailorGold);
                totals.CarpenterBODGold += Math.Max(0, (int)carpenterGold);
            }
        }

        private static void AccumulateLoot(Dictionary<uint, SnapshotTotals> table)
        {
            foreach (var kvp in DataRecorder.DataRecorder.Database)
            {
                Mobile m = kvp.Key;
                if (m == null || m.Account is not Account acct)
                    continue;

                SnapshotTotals totals = GetSnapshot(table, acct.GUID);

                double lootGold = DataRecorder.DataRecorder.Get(m, RecorderDomain.PvMLootGold);
                double chestGold = DataRecorder.DataRecorder.Get(m, RecorderDomain.ChestGold);
                double theftGold = DataRecorder.DataRecorder.Get(m, RecorderDomain.TheftGold);

                totals.PvMLootGold += Math.Max(0, (int)lootGold);
                totals.ChestGold += Math.Max(0, (int)chestGold);
                totals.TheftGold += Math.Max(0, (int)theftGold);
            }
        }

        private static SnapshotTotals GetSnapshot(Dictionary<uint, SnapshotTotals> table, uint guid)
        {
            if (!table.TryGetValue(guid, out SnapshotTotals? totals))
            {
                totals = new SnapshotTotals();
                table[guid] = totals;
            }

            return totals;
        }

        private static int ProcessCategory(
            SeasonalAccount sa,
            int currentTotalGold,
            int processedGold,
            double categoryMultiplier)
        {
            int deltaGold = currentTotalGold - processedGold;

            if (deltaGold <= 0)
                return processedGold;

            int totalRawPoints = ConvertGoldToPoints(currentTotalGold, categoryMultiplier);
            int processedRawPoints = ConvertGoldToPoints(processedGold, categoryMultiplier);
            int rawPoints = totalRawPoints - processedRawPoints;

            if (rawPoints <= 0)
                return currentTotalGold;

            int awardedPoints = ApplyBandedProgressionScaling(sa, rawPoints);

            if (awardedPoints > 0)
            {
                sa.SeasonPoints += awardedPoints;
                sa.LifetimeEarnedPoints += awardedPoints;
            }

            return currentTotalGold;
        }

        /// <summary>
        /// Applies Banded Progression Scaling (BPS) to a raw seasonal point award.
        ///
        /// Conceptually, this replaces the older "late-stage diminishing returns" model
        /// with a simpler, explicit progression schedule:
        ///
        /// - Early progression is awarded at full rate.
        /// - The run-up to the primary threshold is mildly softened.
        /// - Beyond the threshold, progression continues at sharply reduced rates.
        ///
        /// The current seasonal point cap setting is used here as the primary progression
        /// threshold (the point where strong throttling begins), not as a hard stop.
        /// This keeps the system easy to reason about:
        ///
        ///   0 .. threshold          -> normal / near-normal progression
        ///   threshold+             -> continued progress at reduced rates
        ///
        /// The implementation is banded rather than formula-driven so that pacing can be
        /// tuned by editing a small number of clearly named ranges and multipliers.
        /// </summary>
        private static int ApplyBandedProgressionScaling(SeasonalAccount sa, int rawPoints)
        {
            if (sa == null || rawPoints <= 0)
                return 0;

            int primaryThreshold = Math.Max(1, CoreAI.SeasonsPointCap);

            // We advance through the bands using projected awarded points, not raw points.
            // This is important: a 0.10x band means it should take roughly 10 raw points
            // to produce 1 awarded seasonal point within that band.
            double projectedSeasonPoints = sa.SeasonPoints;
            double awarded = 0.0;
            int remainingRaw = rawPoints;

            while (remainingRaw > 0)
            {
                ProgressionBand band = GetProgressionBand(projectedSeasonPoints, primaryThreshold);

                if (band.Multiplier <= 0.0)
                    break;

                if (band.EndExclusive == int.MaxValue)
                {
                    awarded += remainingRaw * band.Multiplier;
                    break;
                }

                double awardedCapacity = band.EndExclusive - projectedSeasonPoints;

                if (awardedCapacity <= 0.0)
                {
                    projectedSeasonPoints = band.EndExclusive;
                    continue;
                }

                // How much raw input would it take to fill the remainder of this band?
                int rawNeededToFillBand = (int)Math.Ceiling(awardedCapacity / band.Multiplier);

                if (rawNeededToFillBand <= 0)
                    rawNeededToFillBand = 1;

                int rawConsumed = Math.Min(remainingRaw, rawNeededToFillBand);

                double bandAward = rawConsumed * band.Multiplier;

                if (bandAward > awardedCapacity)
                    bandAward = awardedCapacity;

                awarded += bandAward;
                projectedSeasonPoints += bandAward;
                remainingRaw -= rawConsumed;
            }

            int adjusted = (int)Math.Floor(awarded);

            // Preserve the legacy "never erase a legitimate gain entirely" behavior.
            // If you later decide post-threshold scaling should be stricter, this is the
            // first place to revisit.
            if (adjusted <= 0)
                adjusted = 1;

            return adjusted;
        }

        /// <summary>
        /// Describes a single progression band.
        ///
        /// EndExclusive:
        ///   The first seasonal point total that is NOT included in this band.
        ///   Example: EndExclusive = 800 means the band covers values below 800.
        ///
        /// Multiplier:
        ///   The scaling applied to raw seasonal points while the player is inside this band.
        /// </summary>
        private struct ProgressionBand
        {
            public readonly int EndExclusive;
            public readonly double Multiplier;

            public ProgressionBand(int endExclusive, double multiplier)
            {
                EndExclusive = endExclusive;
                Multiplier = multiplier;
            }
        }

        /// <summary>
        /// Returns the progression band that applies at the given seasonal point total.
        ///
        /// Current calibration philosophy:
        /// - Strong early progression
        /// - Mild softening on approach to the primary threshold
        /// - Heavy throttling after the threshold
        ///
        /// With a threshold of 1000, this yields approximately:
        /// - 0 .. 800      : full rate
        /// - 800 .. 1000   : slight softening
        /// - 1000 .. 1200  : strong post-threshold throttle
        /// - 1200+         : continued reduced growth
        ///
        /// These values are intentionally explicit and easy to tune as real season data arrives.
        /// </summary>
        private static ProgressionBand GetProgressionBand(double seasonPoints, int primaryThreshold)
        {
            int preCapSoftStart = (int)Math.Floor(primaryThreshold * CoreAI.SeasonsBpsPreCapSoftStartFraction);
            int firstPostCapEnd = primaryThreshold + CoreAI.SeasonsBpsFirstPostCapWidth;
            int secondPostCapEnd = primaryThreshold + CoreAI.SeasonsBpsSecondPostCapWidth;
            int thirdPostCapEnd = primaryThreshold + CoreAI.SeasonsBpsThirdPostCapWidth;

            if (seasonPoints < preCapSoftStart)
                return new ProgressionBand(preCapSoftStart, CoreAI.SeasonsBpsEarlyMultiplier);

            if (seasonPoints < primaryThreshold)
                return new ProgressionBand(primaryThreshold, CoreAI.SeasonsBpsPreCapMultiplier);

            if (seasonPoints < firstPostCapEnd)
                return new ProgressionBand(firstPostCapEnd, CoreAI.SeasonsBpsFirstPostCapMultiplier);

            if (seasonPoints < secondPostCapEnd)
                return new ProgressionBand(secondPostCapEnd, CoreAI.SeasonsBpsSecondPostCapMultiplier);

            if (seasonPoints < thirdPostCapEnd)
                return new ProgressionBand(thirdPostCapEnd, CoreAI.SeasonsBpsThirdPostCapMultiplier);

            return new ProgressionBand(int.MaxValue, CoreAI.SeasonsBpsFinalMultiplier);
        }

        private static int ConvertGoldToPoints(int gold, double categoryMultiplier)
        {
            double goldPerPoint = CoreAI.SeasonsGoldPerPoint;

            if (goldPerPoint <= 0.0)
                goldPerPoint = 1.0;

            double points = (gold / goldPerPoint) * categoryMultiplier;

            return Math.Max(0, (int)Math.Floor(points));
        }

    }

}