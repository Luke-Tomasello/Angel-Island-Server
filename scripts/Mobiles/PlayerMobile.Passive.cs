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

/* Scripts\Mobiles\PlayerMobile.Passive.cs
 * ChangeLog
 *  12/20/25, Adam
 *      Created.
 */

/*
 * Passive progression (PlayerMobile.Passive.cs)
 *
 * Purpose
 *   This module is the sole authority for converting *normalized passive credit*
 *   (points forwarded from upstream observers: pets, commands, etc.) into
 *   player-facing progression effects (skill gain attempts).
 *
 * Scope & Separation of Concerns
 *   - This file owns interpretation + policy only (banking, throttles, apply cadence,
 *     and the "cost" model for converting credit -> discrete gain attempts).
 *   - This file intentionally does NOT observe pet behavior directly.
 *     All signal collection, eligibility checks, exploit mitigation, and context
 *     gating are handled upstream (e.g., BaseCreature.Passive.cs).
 *
 * Core Model (NEW)
 *   - Points are "pre-qualified" at accrual time.
 *   - At AddPassivePoints():
 *       1) Compute min/max window NOW (pet + source + context).
 *       2) Compute OSI-style chance:
 *              chance = (skill.Value - minSkill) / (maxSkill - minSkill)
 *       3) Clamp chance to [0..1].
 *       4) Bank expected value:
 *              effectivePoints = rawPoints * chance
 *
 *   - Apply is simple:
 *       - Spend whole points as discrete gain attempts.
 *       - Each whole point funds one Gain() call (conservative clamp per tick).
 *
 * Why this exists
 *   - Eliminates folded-window history inertia and mixed-source window contamination.
 *   - Preserves OSI semantics (skill lock/cap rules) without carrying difficulty state forward.
 *   - Dramatically simplifies persistence: no per-bucket banks, no window history, no migration traps.
 *
 * Serialization policy
 *   - Production only knows about Version 1.
 *   - Version 2 is a clean break:
 *       - Read v1 and discard it.
 *       - Write v2 using only the new single-bank model.
 */

using Server.Diagnostics;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    internal static class PassiveProgressionPolicy
    {
        /// <summary>
        /// Centralized "can this skill gain right now?" policy for passive progression.
        /// This preserves OSI/UO semantics: if the destination skill is locked or capped,
        /// passive credit is discarded (not banked).
        /// </summary>
        public static bool CanGainSkillNow(PlayerMobile pm, SkillName sn, out Skill skill, out string denyReason)
        {
            skill = null;
            denyReason = null;

            if (pm == null || pm.Deleted || !pm.Alive || pm.Skills == null)
            {
                denyReason = "pm invalid";
                return false;
            }

            skill = pm.Skills[sn];

            if (skill == null)
            {
                denyReason = "skill missing";
                return false;
            }

            if (skill.Lock != SkillLock.Up)
            {
                denyReason = "skill lock not Up";
                return false;
            }

            if (skill.Base >= skill.Cap)
            {
                denyReason = "skill at cap";
                return false;
            }

            return true;
        }
    }

    public partial class PlayerMobile
    {
        #region Passive Progression Framework (Pet-driven credit -> player progression)

        // Identifies the *player progression destination* funded by passive credit.
        // Kept for compatibility with upstream hooks; currently all passive credit funds AnimalTaming.
        public enum PassiveProgressionBucket
        {
            AnimalTaming = 0,
            //AnimalLore = 1,
            //Veterinary = 2,
        }

        // Independent passive credit sources (each can have its own cooldown + diminishing returns tracking).
        // These represent *where the credit came from*, not what progression it funds.
        public enum PassiveGainSource
        {
            Command,
            Feeding,
            Heal,
            Cure,
            Training,
            Engagement,
        }

        private PassiveGainState m_PassiveTaming;

        /// <summary>
        /// Master toggle. Keep false until you're ready to start awarding passive progression.
        /// You can also convert this into a CoreAI feature bit later.
        /// </summary>
        public virtual bool PassiveTamingEnabled
        {
            get { return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.PassiveTamingEnabled); }
        }

        private PassiveGainState GetPassiveTamingState()
        {
            if (m_PassiveTaming == null)
                m_PassiveTaming = new PassiveGainState();

            return m_PassiveTaming;
        }

        /// <summary>
        /// Adds passive credit to the player's progression bank.
        /// This does NOT directly raise skills. A separate Apply phase converts banked credit -> discrete gain attempts.
        ///
        /// IMPORTANT: This is the authoritative accrual gate. Even if upstream hooks perform early
        /// logging/gating for readability, policy enforcement belongs here to prevent banking/serialization
        /// for locked/capped skills and to preserve UO semantics.
        /// </summary>
        public void AddPassivePoints(
            PassiveGainSource source,
            PassiveProgressionBucket bucket,
            double points,
            BaseCreature? pet = null,
            object? context = null)
        {
            if (!PassiveTamingEnabled)
                return;

            if (points <= 0.0)
                return;

            // Basic eligibility
            if (Deleted || !Alive)
                return;

            // UO semantics: do not bank credit if the destination skill cannot currently gain.
            if (!CanAccruePassiveProgression(bucket, out _))
                return;

            PassiveGainState st = GetPassiveTamingState();

            // Source throttle / diminishing returns (independent per source).
            if (!st.Sources[(int)source].TryConsumeCredit(out double scale))
                return;

            points *= scale;

            if (points <= 0.0)
                return;

            // --------------------------------------------------------------------
            // Event-time qualification (NEW MODEL):
            //   - Compute the effective skill window NOW (using pet + source + context).
            //   - Compute OSI-style chance in that window.
            //   - Bank expected value: effectivePoints = rawPoints * clamp01(chance).
            //
            // Rationale:
            //   Points are "pre-qualified" at accrual time. We do not carry difficulty
            //   context forward (no folded windows, no bucket window history). This
            //   eliminates mixed-source window contamination and history inertia.
            // --------------------------------------------------------------------
            st.AddQualifiedPoints(pm: this, source: source, bucket: bucket, rawPoints: points, pet: pet, context: context);

            // Apply on a coarse schedule to avoid performance churn on spammy events.
            st.TryApply(this);
        }

        /// <summary>
        /// Converts banked passive credit into discrete gain attempts (centralized policy).
        /// Goal: meaningful trickle, not a macro engine.
        /// </summary>
        internal void ApplyPassivePoints(PassiveGainState st)
        {
            if (!PassiveTamingEnabled)
                return;

            // Global governor: clamp total apply work per window.
            if (!st.Global.TryConsumeApplyBudget())
                return;

            // If the destination skill cannot gain right now (locked/capped/dead),
            // discard any stored credit to preserve UO semantics.
            if (!PassiveProgressionPolicy.CanGainSkillNow(this, SkillName.AnimalTaming, out Skill taming, out _))
            {
                st.ClearBank();
                return;
            }

            // Spend whole points as discrete gain attempts (+0.01-style intent).
            // Credit is already "pre-qualified" at accrual time via effectivePoints = rawPoints * chance.
            int attempts = st.ConsumeWholePointAttempts(maxAttempts: 1); // conservative; tune later

            if (attempts <= 0)
                return;

            object ctx = st.GetLastContext();

            for (int i = 0; i < attempts; i++)
            {
                if (Alive && PassiveAllowGain(taming, ctx))
                    Gain(taming);
            }
        }

        private bool PassiveAllowGain(Skill skill, object context)
        {
            if (Region != null && Region.IsJailRules)
                return false;

            // You cannot gain skill from BlockDamage creatures unless it is a training mobile.
            // Context is optional under the passive-credit model.
            if (context is BaseCreature bc)
            {
                if (bc.BlockDamage && !bc.TrainingMobile)
                {
                    Utility.Monitor.DebugOut(string.Format($"Cannot gain skill from {context}"), ConsoleColor.DarkRed);
                    return false;
                }
            }

            return true;
        }

        #endregion Passive Progression Framework

        // Only accrue passive credit when the destination skill is currently eligible to gain.
        // UO semantics: if a skill is locked (or at cap), potential gains are discarded (not banked).
        public bool CanAccruePassiveProgression(PassiveProgressionBucket bucket, out string denyReason)
        {
            // Today, all passive credit funds AnimalTaming.
            // If you add other buckets later, map them here.
            if (bucket != PassiveProgressionBucket.AnimalTaming)
            {
                denyReason = "unknown bucket";
                return false;
            }

            if (!PassiveProgressionPolicy.CanGainSkillNow(this, SkillName.AnimalTaming, out _, out denyReason))
                return false;

            return true;
        }

        // Query-only helper (for upstream hook readability).
        public bool CanAccruePassiveProgression(PassiveProgressionBucket bucket)
        {
            return CanAccruePassiveProgression(bucket, out _);
        }

        // Tuneable policy knobs (start conservative)
        private const int KillRewardDistanceTiles = 18;          // "in the room" feel; adjust as desired
        private const double MinDamageShareToQualify = 0.60;     // 60% share in contested fights
        private const int MinTotalDamageToConsider = 1;          // defensive

        /// <summary>
        /// Determines whether this player "earned" kill credit for a lethal hit on <paramref name="target"/>.
        /// Centralizes attribution policy so hooks stay clean.
        ///
        /// Attribution policy:
        ///  - Player must be within KillRewardDistanceTiles of the target at time of evaluation.
        ///  - Uses target.DamageEntries (non-expired) to compute contributions.
        ///  - Controlled pet damage is attributed to ControlMaster.
        ///  - If only one player contributed damage: that player qualifies.
        ///  - Else the top total-damage dealer qualifies.
        ///  - If multiple players tie for top damage: tie is broken by most recent damager.
        /// </summary>
        public bool EarnedKillReward(Mobile target, out string reason)
        {
            reason = null;

            if (target == null || target.Deleted)
            {
                reason = "target null/deleted";
                return false;
            }

            // Distance gate (anti "wait outside" behavior)
            // NOTE: if your shard has LOS/region rules you prefer, this is the place to add them.
            double dist = GetDistanceToSqrt(target.Location);
            if (dist > KillRewardDistanceTiles)
            {
                reason = $"too far ({dist:0.0} > {KillRewardDistanceTiles})";
                return false;
            }

            // We only have DamageEntries on BaseCreature in your use-case.
            // If you later want this to work for Player targets, extend it here.
            if (target is not BaseCreature bcTarget || bcTarget.DamageEntries == null)
            {
                reason = "target has no DamageEntries";
                return false;
            }

            if (!TryComputeKillRewardWinner(
                    bcTarget,
                    out int totalDamage,
                    out int distinctPlayers,
                    out PlayerMobile winner,
                    out int winnerDamage,
                    out DateTime winnerLastDamageUtc,
                    out int ourDamage,
                    out DateTime ourLastDamageUtc))
            {
                reason = "no valid non-expired player damage entries";
                return false;
            }

            if (totalDamage < MinTotalDamageToConsider)
            {
                reason = $"totalDamage<{MinTotalDamageToConsider}";
                return false;
            }

            if (winner == null)
            {
                reason = "winner null";
                return false;
            }

            if (winner != this)
            {
                // Keep this fairly verbose; it is only used under debug instrumentation.
                reason = $"lost winner={winner.Name}:{winnerDamage} last={winnerLastDamageUtc:O} ours={ourDamage} last={ourLastDamageUtc:O} players={distinctPlayers} total={totalDamage}";
                return false;
            }

            // Winner
            if (distinctPlayers <= 1)
                reason = $"onlyDamager dmg={winnerDamage}/{totalDamage}";
            else
                reason = $"topDealer dmg={winnerDamage}/{totalDamage} last={winnerLastDamageUtc:O}";

            return true;
        }

        /// <summary>
        /// Computes an exclusive winner for kill reward from non-expired DamageEntries.
        /// Winner = highest total damage; ties broken by most recent damager (latest damage timestamp).
        /// Controlled pet damage is attributed to ControlMaster.
        /// </summary>
        private static bool TryComputeKillRewardWinner(
            BaseCreature target,
            out int totalDamage,
            out int distinctPlayers,
            out PlayerMobile winner,
            out int winnerDamage,
            out DateTime winnerLastDamageUtc,
            out int ourDamage,
            out DateTime ourLastDamageUtc)
        {
            totalDamage = 0;
            distinctPlayers = 0;
            winner = null;
            winnerDamage = 0;
            winnerLastDamageUtc = DateTime.MinValue;
            ourDamage = 0;
            ourLastDamageUtc = DateTime.MinValue;

            // Low-N in practice; keep it allocation-light and deterministic.
            // Track: pm -> (totalDamage, lastDamageUtc)
            List<(PlayerMobile pm, int dmg, DateTime lastUtc)> perPlayer = new List<(PlayerMobile, int, DateTime)>(8);

            for (int i = 0; i < target.DamageEntries.Count; i++)
            {
                if (target.DamageEntries[i] is not DamageEntry de)
                    continue;

                if (de.HasExpired)
                    continue;

                Mobile damager = de.Damager;
                if (damager == null || damager.Deleted)
                    continue;

                // Attribute controlled pet damage to owner (ControlMaster)
                if (damager is BaseCreature bc)
                {
                    Mobile master = bc.ControlMaster;
                    if (master != null && !master.Deleted)
                        damager = master;
                }

                if (damager is not PlayerMobile pm)
                    continue;

                if (!pm.Alive)
                    continue;

                int dmg = de.DamageGiven;
                if (dmg <= 0)
                    continue;

                DateTime whenUtc = de.LastDamage; // adjust if your DamageEntry uses a different field name

                totalDamage += dmg;

                bool found = false;
                for (int j = 0; j < perPlayer.Count; j++)
                {
                    if (perPlayer[j].pm == pm)
                    {
                        int newDmg = perPlayer[j].dmg + dmg;
                        DateTime newLast = whenUtc > perPlayer[j].lastUtc ? whenUtc : perPlayer[j].lastUtc;
                        perPlayer[j] = (pm, newDmg, newLast);
                        found = true;
                        break;
                    }
                }

                if (!found)
                    perPlayer.Add((pm, dmg, whenUtc));
            }

            if (perPlayer.Count == 0)
                return false;

            distinctPlayers = perPlayer.Count;

            // Find top damage and resolve tie by most recent damager
            int maxDamage = 0;
            for (int i = 0; i < perPlayer.Count; i++)
                if (perPlayer[i].dmg > maxDamage)
                    maxDamage = perPlayer[i].dmg;

            PlayerMobile? bestPm = null;
            int bestDmg = 0;
            DateTime bestLastUtc = DateTime.MinValue;

            for (int i = 0; i < perPlayer.Count; i++)
            {
                var entry = perPlayer[i];
                if (entry.dmg != maxDamage)
                    continue;

                if (bestPm == null || entry.lastUtc > bestLastUtc)
                {
                    bestPm = entry.pm;
                    bestDmg = entry.dmg;
                    bestLastUtc = entry.lastUtc;
                }
            }

            winner = bestPm;
            winnerDamage = bestDmg;
            winnerLastDamageUtc = bestLastUtc;

            return true;
        }
    }

    #region PassiveGainState implementation

    internal sealed class PassiveGainState
    {
        public readonly SourceState[] Sources;
        public readonly GlobalState Global;

        // Single bank of pre-qualified passive credit (expected value).
        // When bank crosses whole-point thresholds, we fund discrete gain attempts.
        private double m_Bank;

        // Last context token (optional) for diagnostics / policy checks.
        private object? m_LastContext;

        // Apply throttling (coarse scheduling)
        private DateTime m_NextApply;

        // Pre-engagement hooks (feed/command/heal/cure/etc.) are supportive-only.
        // Engagement is the primary progression driver; pre-engagement events are hard-capped per event.
        private const double PreEngagementMaxBank = 0.25;
        public PassiveGainState()
        {
            Sources = new SourceState[Enum.GetValues(typeof(PlayerMobile.PassiveGainSource)).Length];
            for (int i = 0; i < Sources.Length; i++)
                Sources[i] = new SourceState((PlayerMobile.PassiveGainSource)i);

            Global = new GlobalState();

            m_Bank = 0.0;
            m_LastContext = null;
            m_NextApply = DateTime.MinValue;
        }

        public void ClearBank()
        {
            m_Bank = 0.0;
        }

        public object GetLastContext()
        {
            return m_LastContext;
        }

        private void UpdateContext(object? context)
        {
            if (context != null)
                m_LastContext = context;
        }

        /// <summary>
        /// Bank passive progression credit for a single qualifying event.
        /// 
        /// Contract:
        /// - This is called at event time (heal/cure/feed/command/engagement, etc.) after the caller has already
        ///   applied common gates (ownership, cooldowns, validity, etc.).
        /// - We compute a probability-like acceptance value ("chance") based on the source/context, then convert
        ///   rawPoints into banked points using the source policy:
        ///     • Engagement: bank expected value directly -> rawPoints * chance
        ///       (chance is a product of meaningfulness signals such as threat/participation/skillFactor)
        ///     • Non-engagement hooks: update rolling context, compute chance, then apply a weighting function
        ///       -> rawPoints * GainWeightFromChance(chance)
        /// - If the computed bank increment is <= 0, nothing is recorded.
        /// 
        /// Notes:
        /// - Today, all passive progression funds AnimalTaming (single bank). If you later fund multiple skills,
        ///   map bucket -> SkillName (or split into per-skill banks) here.
        /// - Debug output is intentionally GM-only and feature-flagged to keep production quiet.
        /// </summary>
        public void AddQualifiedPoints(
            PlayerMobile pm,
            PlayerMobile.PassiveGainSource source,
            PlayerMobile.PassiveProgressionBucket bucket,
            double rawPoints,
            BaseCreature? pet,
            object? context)
        {
            if (pm == null || pm.Deleted || !pm.Alive)
                return;

            // Today, all passive points fund AnimalTaming. If you later add other skills,
            // map bucket -> SkillName here (or split to per-skill banks).
            if (bucket != PlayerMobile.PassiveProgressionBucket.AnimalTaming)
                return;

            if (rawPoints <= 0.0)
                return;

            if (pet == null || pet.Deleted)
                return;

            double chance = 0.0;
            double effectivePoints = 0;
            double gainWeight = 0;
            string sdbg = string.Empty;
            if (source == PlayerMobile.PassiveGainSource.Engagement)
            {
                if (!TryComputeEngagementChance(pm, pet, context, out chance))
                    return;

                effectivePoints = rawPoints * chance;
                if (effectivePoints <= 0.0)
                    return;

                m_Bank += effectivePoints;

                sdbg = $"[Passive] +{effectivePoints:0.###} (raw={rawPoints:0.###}, chance={chance:0.###}) bank={m_Bank:0.###}";
            }
            else
            {
                UpdateContext(context);

                if (!TryComputeSkillChance(pm, source, pet, context, out chance))
                    return;

                gainWeight = GainWeightFromChance(chance);
                effectivePoints = rawPoints * gainWeight;

                // Supportive-only policy: pre-engagement events may not outpace engagement.
                if (effectivePoints > PreEngagementMaxBank)
                    effectivePoints = PreEngagementMaxBank;

                if (effectivePoints <= 0.0)
                    return;

                m_Bank += effectivePoints;

                sdbg = $"[Passive] +{effectivePoints:0.###} (raw={rawPoints:0.###}, chance={chance:0.###}, w={gainWeight:0.###}) bank={m_Bank:0.###}";
            }

            // Optional: keep this extremely quiet in production; only useful during tuning.
            bool debug = CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.PassiveTamingEnabled) &&
                         pm.AccessLevel >= AccessLevel.GameMaster;

            if (debug)
            {
                pm.SendMessage(sdbg);

                LogHelper logger = new LogHelper("passive gains.log", false, true);
                logger.Log(LogType.Text, sdbg);
                logger.Finish();
            }
        }

        private static double GainWeightFromChance(double chance)
        {
            // Defensive clamp: treat chance outside [0,1] as 0 or 1.
            if (chance <= 0.0)
                return 0.0;
            if (chance >= 1.0)
                return 0.0; // "too easy" should not pay

            // Bell-shaped weight: peak at 0.5, zero at 0 and 1.
            // Base curve: w = 4c(1-c) in [0..1].
            double w = 4.0 * chance * (1.0 - chance);

            // Gamma tunes “how picky” we are about the mid-band.
            // Gamma = 1.0 => mild suppression
            // Gamma = 2.0 => stronger (recommended start)
            // Gamma = 3.0 => very strong
            const double Gamma = 2.0;
            if (Gamma != 1.0)
                w = Math.Pow(w, Gamma);

            return w;
        }

        /// <summary>
        /// Spend whole points as discrete gain attempts. This is intentionally conservative.
        /// </summary>
        public int ConsumeWholePointAttempts(int maxAttempts)
        {
            if (m_Bank < 1.0)
                return 0;

            int count = (int)m_Bank;

            if (count <= 0)
                return 0;

            if (maxAttempts > 0 && count > maxAttempts)
                count = maxAttempts;

            m_Bank -= count;

            // Defensive: avoid negative drift from FP precision
            if (m_Bank < 0.0)
                m_Bank = 0.0;

            return count;
        }

        internal bool HasBankedCredit(double epsilon = 1e-9)
        {
            return m_Bank > epsilon;
        }

        public void TryApply(PlayerMobile master)
        {
            DateTime now = DateTime.UtcNow;

            // Coarse apply schedule: at most once every couple seconds.
            if (now < m_NextApply)
                return;

            m_NextApply = now + TimeSpan.FromSeconds(2.0);

            master.ApplyPassivePoints(this);
        }

        #region Serialization

        internal void WriteTo(GenericWriter writer)
        {
            // PassiveGainState persistence version.
            // Production currently only knows about v1; v2 is a clean break.
            const int version = 2;
            writer.Write(version);

            // v2: single bank (expected-value points)
            writer.Write(m_Bank);
        }

        internal void ReadFrom(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        // v1: per-bucket bank array. We intentionally discard this state.
                        int count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                            reader.ReadDouble();

                        m_Bank = 0.0;
                        m_LastContext = null;
                        break;
                    }

                case 2:
                    {
                        m_Bank = reader.ReadDouble();

                        // Defensive
                        if (m_Bank < 0.0)
                            m_Bank = 0.0;

                        break;
                    }

                default:
                    {
                        // Unknown version: discard state rather than risk corruption.
                        m_Bank = 0.0;
                        m_LastContext = null;
                        break;
                    }
            }
        }

        #endregion Serialization
        // Anti-macro: if player is far above the target's tier, reduce benefit.
        // "Possible but miserable" floor is MinFactor.
        private static double SkillVsTargetFactor(double playerSkill, double targetTier)
        {
            double over = playerSkill - targetTier;

            if (over <= 0.0)
                return 1.0;

            const double OverZero = 50.0;  // how far above tier before mostly bottomed out
            const double MinFactor = 0.10; // miserable but non-zero

            double t = over / OverZero;
            t = Clamp01(t);

            // smoothstep falloff
            t = t * t * (3.0 - 2.0 * t);

            return 1.0 - t * (1.0 - MinFactor);
        }

        // Returns a "tier-equivalent" taming difficulty for *any* creature.
        // - Tamables: use MinTameSkill (authoritative)
        // - Non-tamables (MinTameSkill == 0): estimate from Power anchored to Dragon.
        //
        // NOTE: Power varies per spawn; DragonPower should be chosen as an average anchor.
        // From your avg/min/max dump, use the Dragon avg (or your preferred anchor).
        private static double GetTamingTierEstimate(BaseCreature bc)
        {
            if (bc == null)
                return 0.0;

            double mt = bc.MinTameSkill;
            if (mt > 0.0)
                return mt;

            // Dragon anchor (your shard): choose AVG dragon power from your dump.
            // Replace this with the avg value you want to lock in.
            const double DragonMinTame = 93.9;
            const double DragonPowerAvg = 281.383;

            // Gentle power-law: monotonic, stable, "feels like dragon scale".
            // Exponent is intentionally mild; we do not want huge tier swings from Power variance.
            const double Exponent = 0.45;

            double p = Math.Max(1.0, bc.Power);
            double tier = DragonMinTame * Math.Pow(p / DragonPowerAvg, Exponent);

            // Clamp to sane taming range
            if (tier < 0.0) tier = 0.0;
            if (tier > 120.0) tier = 120.0;

            return tier;
        }

        private static bool TryComputeDamageParticipation(
            BaseCreature target,
            PlayerMobile us,
            out int totalDamage,
            out int distinctPlayers,
            out int ourDamage,
            out DateTime ourLastDamageUtc)
        {
            totalDamage = 0;
            distinctPlayers = 0;
            ourDamage = 0;
            ourLastDamageUtc = DateTime.MinValue;

            if (target == null || target.Deleted)
                return false;

            if (us == null || us.Deleted || !us.Alive)
                return false;

            // Low-N in practice; keep it allocation-light and deterministic.
            List<(PlayerMobile pm, int dmg, DateTime lastUtc)> perPlayer =
                new List<(PlayerMobile pm, int dmg, DateTime lastUtc)>(8);

            for (int i = 0; i < target.DamageEntries.Count; i++)
            {
                if (target.DamageEntries[i] is not DamageEntry de)
                    continue;

                if (de.HasExpired)
                    continue;

                Mobile damager = de.Damager;
                if (damager == null || damager.Deleted)
                    continue;

                // Attribute controlled pet damage to owner (ControlMaster)
                if (damager is BaseCreature bc)
                {
                    Mobile master = bc.ControlMaster;
                    if (master != null && !master.Deleted)
                        damager = master;
                }

                if (damager is not PlayerMobile pm)
                    continue;

                if (!pm.Alive)
                    continue;

                int dmg = de.DamageGiven;
                if (dmg <= 0)
                    continue;

                DateTime whenUtc = de.LastDamage; // adjust if needed

                totalDamage += dmg;

                bool found = false;
                for (int j = 0; j < perPlayer.Count; j++)
                {
                    if (perPlayer[j].pm == pm)
                    {
                        int newDmg = perPlayer[j].dmg + dmg;
                        DateTime newLast = whenUtc > perPlayer[j].lastUtc ? whenUtc : perPlayer[j].lastUtc;
                        perPlayer[j] = (pm, newDmg, newLast);
                        found = true;
                        break;
                    }
                }

                if (!found)
                    perPlayer.Add((pm, dmg, whenUtc));
            }

            if (perPlayer.Count == 0 || totalDamage <= 0)
                return false;

            distinctPlayers = perPlayer.Count;

            // Resolve "us"
            for (int i = 0; i < perPlayer.Count; i++)
            {
                if (perPlayer[i].pm == us)
                {
                    ourDamage = perPlayer[i].dmg;
                    ourLastDamageUtc = perPlayer[i].lastUtc;
                    break;
                }
            }

            return ourDamage > 0;
        }

        private static bool TryComputeEngagementChance(
    PlayerMobile pm,
    BaseCreature pet,
    object? context,
    out double chance)
        {
            chance = 0.0;

            if (pm == null || pm.Deleted || !pm.Alive)
                return false;

            if (pet == null || pet.Deleted)
                return false;

            if (context is not BaseCreature target || target.Deleted)
                return false;

            if (target.HasRecentBenefit())
                return false; // deny engagement credit while target is actively being sustained

            // ---- Participation evidence (membership-based) ----
            if (!TryComputeDamageParticipation(
                    target,
                    pm,
                    out int totalDamage,
                    out int distinctPlayers,
                    out int ourDamage,
                    out DateTime ourLastDamageUtc))
                return false;

            // Recency gate: must be actively contributing
            const int RecentSeconds = 20;
            if ((DateTime.UtcNow - ourLastDamageUtc).TotalSeconds > RecentSeconds)
                return false;

            // Minimum contribution gate: avoid single-tag abuse
            const int MinOurDamage = 15;
            if (ourDamage < MinOurDamage)
                return false;

            double share = (double)ourDamage / totalDamage; // 0..1

            // Make group fights fairer: boost small shares gently
            const double ShareExponent = 0.65; // <1 boosts small shares
            double participation = Math.Pow(Clamp01(share), ShareExponent);

            // Optional “presence floor” once you pass MinOurDamage + recency:
            const double ParticipationFloor = 0.15;
            if (participation < ParticipationFloor)
                participation = ParticipationFloor;

            // ---- Threat (monotonic) ----
            double petPower = Math.Max(1.0, pet.Power);
            double tgtPower = Math.Max(1.0, target.Power);

            double ratio = tgtPower / petPower;

            const double R0 = 0.45; // trivial below
            const double R1 = 1.05; // full near parity+
            double threat = Clamp01((ratio - R0) / (R1 - R0));
            threat = threat * threat * (3.0 - 2.0 * threat); // smoothstep

            // ---- Engagement chance composition ----
            // 1) Relative meaningfulness: target vs pet
            double baseChance = Clamp01(threat * participation);

            // 2) Absolute progression hygiene: player vs target tier
            Skill taming = pm.Skills[SkillName.AnimalTaming];
            if (taming == null)
                return false;

            double targetTier = GetTamingTierEstimate(target);
            double skillFactor = SkillVsTargetFactor(taming.Value, targetTier);

            // Final chance
            chance = Clamp01(baseChance * skillFactor);

            return chance > 0.0;
        }
        private static double Clamp01(double v)
        {
            if (v <= 0.0)
                return 0.0;

            if (v >= 1.0)
                return 1.0;

            return v;
        }


        private static bool TryComputeSkillChance(
            PlayerMobile pm,
            PlayerMobile.PassiveGainSource source,
            BaseCreature pet,
            object? context,
            out double chance)
        {
            chance = 0;

            if (!TryComputeSkillWindow(source, pet, context, out double minSkill, out double maxSkill))
                return false;

            if (maxSkill <= minSkill)
                maxSkill = minSkill + 0.0001;

            Skill skill = pm.Skills[SkillName.AnimalTaming];
            if (skill == null)
                return false;

            chance = (skill.Value - minSkill) / (maxSkill - minSkill);

            // Canonical clamp: treat chance outside [0,1) as 0..(1-eps) for weighting
            if (chance < 0.0)
                chance = 0.0;
            else if (chance >= 1.0)
                chance = 0.999; // avoid hard "1.0 => w=0" cliff in GainWeightFromChance

            return true;
        }

        private static bool TryComputeSkillWindow(
            PlayerMobile.PassiveGainSource source,
            BaseCreature pet,
            object? context,
            out double minSkill,
            out double maxSkill)
        {
            minSkill = 0.0;
            maxSkill = 0.0;

            if (pet == null || pet.Deleted)
                return false;

            // Default window (all non-special sources for now):
            // Use a taming-tier estimate (MinTameSkill when positive; otherwise Power-based estimate).
            // This preserves the classic OSI-shaped linear window model, but avoids OSI sentinel values
            // (e.g., MinTameSkill < 0) distorting chance/weighting for modern passive progression.
            double center = GetTamingTierEstimate(pet);
            double halfWidth = 25.0;

            minSkill = center - halfWidth;
            maxSkill = center + halfWidth;

            // Optional safety: keep window in sane bounds (prevents negative mins on trivial tiers)
            if (minSkill < 0.0) minSkill = 0.0;
            if (maxSkill > 120.0) maxSkill = 120.0;

            // Defensive: avoid degenerate windows
            if (maxSkill <= minSkill)
                maxSkill = minSkill + 0.0001;

            return true;
        }
    }
    internal sealed class SourceState
    {
        private readonly PlayerMobile.PassiveGainSource m_Source;

        // Cooldown and burst tracking
        private DateTime m_NextCredit;
        private int m_BurstCount;
        private DateTime m_BurstWindowStart;

        public SourceState(PlayerMobile.PassiveGainSource src)
        {
            m_Source = src;
            m_NextCredit = DateTime.MinValue;
            m_BurstCount = 0;
            m_BurstWindowStart = DateTime.MinValue;
        }

        public bool TryConsumeCredit(out double scale)
        {
            scale = 1.0;

            DateTime now = DateTime.UtcNow;

            // Independent per-source cooldowns
            TimeSpan cd = GetCooldown(m_Source);

            if (now < m_NextCredit)
                return false;

            m_NextCredit = now + cd;

            // Simple diminishing returns in a short window
            if (m_BurstWindowStart == DateTime.MinValue || (now - m_BurstWindowStart) > TimeSpan.FromSeconds(10.0))
            {
                m_BurstWindowStart = now;
                m_BurstCount = 0;
            }

            m_BurstCount++;

            // Diminishing returns: 1.0, 0.5, 0.25, 0.125, ...
            // Clamp to avoid hitting zero.
            if (m_BurstCount > 1)
            {
                scale = 1.0 / (1 << (m_BurstCount - 1));
                if (scale < 0.1)
                    scale = 0.1;
            }

            return true;
        }

        private static TimeSpan GetCooldown(PlayerMobile.PassiveGainSource src)
        {
            // Tune per source. Start conservative.
            switch (src)
            {
                case PlayerMobile.PassiveGainSource.Command:
                    return TimeSpan.FromSeconds(2.0);

                case PlayerMobile.PassiveGainSource.Feeding:
                    return TimeSpan.FromMinutes(2.0);

                case PlayerMobile.PassiveGainSource.Heal:
                    return TimeSpan.FromSeconds(10.0);

                case PlayerMobile.PassiveGainSource.Cure:
                    return TimeSpan.FromSeconds(10.0);

                case PlayerMobile.PassiveGainSource.Training:
                    return TimeSpan.FromSeconds(5.0);

                default:
                    return TimeSpan.FromSeconds(5.0);
            }
        }
    }

    internal sealed class GlobalState
    {
        private DateTime m_WindowStart;
        private int m_AppliesInWindow;

        // Global apply budget: “how often are we willing to attempt passive gains?”
        // This is separate from per-source cooldowns.
        public bool TryConsumeApplyBudget()
        {
            DateTime now = DateTime.UtcNow;

            // 60-second window, N applies allowed
            if (m_WindowStart == DateTime.MinValue || (now - m_WindowStart) > TimeSpan.FromSeconds(60.0))
            {
                m_WindowStart = now;
                m_AppliesInWindow = 0;
            }

            const int MaxAppliesPerMinute = 20; // conservative; tune later

            if (m_AppliesInWindow >= MaxAppliesPerMinute)
                return false;

            m_AppliesInWindow++;
            return true;
        }
    }

    #endregion
}
