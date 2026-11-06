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

/*
 * =====================================================================
 * Criminality Subsystem
 * =====================================================================
 *
 * This module intentionally decomposes "criminality" into three
 * orthogonal mechanics. Each mechanic has:
 *
 *   - a single responsibility
 *   - a single timer (or state source)
 *   - a single public interface for call sites
 *
 * No mechanic does double duty.
 * No flags encode temporal state.
 * Timers ARE the state machines.
 *
 * ---------------------------------------------------------------------
 * 1) Guard Whack Window (approx. 10 seconds)
 * ---------------------------------------------------------------------
 * Purpose:
 *   NPC justice ONLY. This governs whether town guards may be called
 *   to instantly kill a criminal. It is not related to player legality.
 *
 * State:
 *   m_CriminalWhackTime != null
 *
 * Public Interface:
 *   bool CanBeGuardWhacked
 *
 * Behavior:
 *   - Window opens immediately on a criminal act.
 *   - Guards may be called ONLY while the window is active.
 *   - When the window expires:
 *       - the offender is removed from all GuardCandidates
 *       - guards can no longer be called
 *
 * Messaging:
 *   - "Guards can now be called on you!" when the window opens (guarded regions)
 *   - "Guards can no longer be called on you." when the window closes
 *
 * Notes:
 *   There is deliberately NO persistent "guard-wackable" flag.
 *   Eligibility is implicit; timing is explicit.
 *
 * ---------------------------------------------------------------------
 * 2) Criminal Travel Suppression (approx. 2 minutes)
 * ---------------------------------------------------------------------
 * Purpose:
 *   Prevent instant escape (recall, gates, moongates) immediately after
 *   committing a crime. This is pressure, not punishment.
 *
 * State:
 *   m_CriminalTravelTime != null
 *
 * Public Interface:
 *   bool CanCriminalTravel
 *
 * Behavior:
 *   - Travel lock starts/refreshed on each criminal act.
 *   - Travel is allowed again once the short window expires,
 *     even if the player remains criminal for hours.
 *
 * Messaging:
 *   - Intentionally handled at call sites (spells, moongates).
 *
 * Notes:
 *   This decouples OSI's "no escape" window from Mortalis'
 *   extended gray durations.
 *
 * ---------------------------------------------------------------------
 * 3) Criminal / Gray Duration (variable; long on Mortalis)
 * ---------------------------------------------------------------------
 * Purpose:
 *   Player justice, notoriety, and long-term consequences.
 *
 * State:
 *   m_Criminal + ExpireCriminalTimer
 *
 * Public Interface:
 *   bool Criminal
 *
 * Behavior:
 *   - Governs gray status and player attack legality.
 *   - Independent of guard calls and travel suppression.
 *
 * ---------------------------------------------------------------------
 * Design Invariants
 * ---------------------------------------------------------------------
 * - No timer performs more than one job.
 * - No boolean flag encodes temporal state.
 * - Call sites never inspect timers directly.
 * - Call sites ask simple questions:
 *
 *       CanBeGuardWhacked
 *       CanCriminalTravel
 *
 * This structure is intentionally OSI-faithful in behavior,
 * while allowing Mortalis-specific tuning without cross-system bugs.
 *
 * =====================================================================
 */

/* Server\Mobile.Criminality.cs
 * CHANGELOG
 *  2/11/26, Adam
 */

using Server.Diagnostics;
using Server.Mobiles;
using Server.Regions;
using System;
namespace Server
{
    public partial class Mobile
    {
        #region Criminality

        #region Apply Criminality

        public enum CriminalitySource
        {
            Criminal, // default criminal behavior (looting blue corpse, etc.)
            Harmful,
            AFK,
            Thieving
        }

        public void ApplyCriminality(CriminalitySource src,
            CriminalityContext ctx, bool regionInform, bool message, bool writeAccountEnds = true)
        {
            TimeSpan gray;
            TimeSpan whack;
            TimeSpan travel;

            switch (src)
            {
                default:
                case CriminalitySource.Criminal:
                    gray = TimeSpan.FromMinutes(CoreAI.CriminalGrayMinutes);
                    whack = TimeSpan.FromSeconds(CoreAI.CriminalWhackSeconds);
                    travel = TimeSpan.FromMinutes(CoreAI.CriminalTravelMinutes);
                    break;

                case CriminalitySource.Harmful:
                    gray = CalcHarmfulGray(ctx);
                    whack = TimeSpan.FromSeconds(CoreAI.HarmfulWhackSeconds);
                    travel = TimeSpan.FromMinutes(CoreAI.HarmfulTravelMinutes);
                    break;

                case CriminalitySource.AFK:
                    gray = TimeSpan.FromMinutes(CoreAI.AFKGrayMinutes);
                    whack = TimeSpan.FromSeconds(CoreAI.AFKWhackSeconds);
                    travel = TimeSpan.FromMinutes(CoreAI.AFKTravelMinutes);
                    break;

                case CriminalitySource.Thieving:
                    gray = TimeSpan.FromMinutes(CoreAI.ThievingGrayMinutes);
                    whack = TimeSpan.FromSeconds(CoreAI.ThievingWhackSeconds);
                    travel = TimeSpan.FromMinutes(CoreAI.ThievingTravelMinutes);
                    break;
            }

            ApplyCriminality(src, gray, whack, travel, ctx, regionInform, message, writeAccountEnds);
        }
        /// <summary>
        /// Determines the duration of the attackable (gray) timer following a harmful action,
        /// assuming criminality has already been established.
        ///
        /// This method distinguishes between player and non-player targets:
        /// - Aggression against innocent players carries an extended gray duration to
        ///   mitigate harassment and allow meaningful retaliation.
        /// - Aggression against NPCs or other non-player entities uses a shorter duration,
        ///   as the abuse potential is significantly lower.
        ///
        /// This separation ensures proportional consequences based on the impact of the action,
        /// rather than applying a single blanket timer to all harmful interactions.
        /// </summary>
        private TimeSpan CalcHarmfulGray(CriminalityContext ctx)
        {
            if (ctx != null && ctx.Target is PlayerMobile pm)
                return TimeSpan.FromMinutes(CoreAI.HarmfulPvPGrayMinutes);
            else
                return TimeSpan.FromMinutes(CoreAI.HarmfulGrayMinutes);
        }
        public void ApplyCriminality(CriminalitySource src, TimeSpan gray, TimeSpan whack, TimeSpan travel,
            CriminalityContext? ctx, bool regionInform, bool message, bool writeAccountEnds = true)
        {
            // Mortalis note:
            // OSI could gate behavior off Mobile.Criminal because criminal timers were short-lived.
            // Mortalis tracks explicit, account-authoritative end-times for each restriction window so they survive death/relog.
            // On a new offense we refresh windows from now, but we NEVER shorten an already-active window (credit for time served).
            // On rehydration (writeAccountEnds=false), we restart runtime timers/flags ONLY and do not modify account end-times.

            if (this == null || this is not PlayerMobile pm || pm.Account == null)
                return;

            Accounting.Account? acct = pm.Account as Accounting.Account;
            if (acct == null)
                return;

            DateTime now = DateTime.UtcNow;

            if (writeAccountEnds)
            {
                static DateTime MaxEnd(DateTime existingEnds, DateTime proposedEnds)
                    => (existingEnds > proposedEnds) ? existingEnds : proposedEnds;

                static DateTime Extend(DateTime existingEnds, DateTime now, TimeSpan duration)
                {
                    // If duration is non-positive, do not modify the window.
                    if (duration <= TimeSpan.Zero)
                        return existingEnds;

                    // Preserve "forever" semantics.
                    if (duration == TimeSpan.MaxValue || existingEnds == DateTime.MaxValue)
                        return DateTime.MaxValue;

                    // Refresh from now, but never shorten an already-active window.
                    DateTime proposedEnds = now + duration;
                    return MaxEnd(existingEnds, proposedEnds);
                }

                switch (src)
                {
                    case CriminalitySource.Harmful:
                        acct.HarmfulCriminalityEnds = Extend(acct.HarmfulCriminalityEnds, now, gray);
                        acct.HarmfulGuardWhackEnds = Extend(acct.HarmfulGuardWhackEnds, now, whack);
                        acct.HarmfulTravelLockEnds = Extend(acct.HarmfulTravelLockEnds, now, travel);
                        break;

                    case CriminalitySource.AFK:
                        acct.AFKCriminalityEnds = Extend(acct.AFKCriminalityEnds, now, gray);
                        acct.AFKGuardWhackEnds = Extend(acct.AFKGuardWhackEnds, now, whack);
                        acct.AFKTravelLockEnds = Extend(acct.AFKTravelLockEnds, now, travel);
                        break;

                    case CriminalitySource.Thieving:
                        acct.ThievingCriminalityEnds = Extend(acct.ThievingCriminalityEnds, now, gray);
                        acct.ThievingGuardWhackEnds = Extend(acct.ThievingGuardWhackEnds, now, whack);
                        acct.ThievingTravelLockEnds = Extend(acct.ThievingTravelLockEnds, now, travel);
                        break;
                }
            }

            // record this Harmful Offense in their account
            if (ctx is not null && ctx.Target is PlayerMobile target)
                if (pm.InTownFirstHarmfulOffense(target))
                    pm.RecordHarmfulOffense();

            // Runtime enforcement uses durations; account fields are authoritative for persistence when writeAccountEnds==true.
            pm.MakeCriminal(gray: gray, whack: whack, travel: travel, ctx: ctx, message: message,
                regionInform: regionInform);
        }

        #endregion Apply Criminality

        // Where the crime occurred, used to prevent guards being called on unseen/off-screen crimes.
        private Point3D m_sceneOfTheCrime = Point3D.Zero;

        public Point3D SceneOfTheCrime
        {
            get { return m_sceneOfTheCrime; }
            set { m_sceneOfTheCrime = value; }
        }

        private static void StopAndClearTimer(ref Timer t)
        {
            if (t == null)
                return;

            t.Stop();
            t.Flush();
            t = null;
        }

        #region Default Criminal Timers

        // Default values (internal policy). Call sites should be explicit by using MakeCriminal(gray, whack, travel).
        public static TimeSpan DefaultCriminalGrayMinutes
        {
            get { return TimeSpan.FromMinutes(CoreAI.CriminalGrayMinutes); }
        }

        public static TimeSpan DefaultGuardWhackWindowDelay
        {
            get { return TimeSpan.FromSeconds(CoreAI.CriminalWhackSeconds); }
        }

        public static TimeSpan DefaultCriminalTravelLockDelay
        {
            get { return TimeSpan.FromMinutes(CoreAI.CriminalTravelMinutes); }
        }

        #endregion Default Criminal Timers

        #region GuardWhackWindow

        private Timer m_CriminalWhackTime;

        public bool CanBeGuardWhacked
        {
            get { return (m_CriminalWhackTime != null); }
        }

        // Represents a short-lived justice window during which civil NPC services are denied.
        public bool HasActiveJusticeRestriction => CanBeGuardWhacked;

        private void StartGuardWhackWindow(TimeSpan delay)
        {
            StopAndClearTimer(ref m_CriminalWhackTime);
            m_CriminalWhackTime = Timer.DelayCall(delay, new TimerStateCallback(EndGuardWhackWindow), null);
        }

        private void RemoveFromAllGuardCandidates()
        {
            Map map = this.Map;

            if (map == null)
                return;

            foreach (Region reg in map.RegionsSorted)
            {
                GuardedRegion? gr = reg as GuardedRegion;

                if (gr != null)
                    gr.GuardCandidates.Remove(this);
            }
        }

        // Called when the guard window expires (or if you choose to call it early).
        private void EndGuardWhackWindow(object state)
        {
            if (m_CriminalWhackTime == null)
                return;

            Region reg = this.Region;
            bool wasGuardRelevant = Utility.GuardUtility.IsGuardRelevant(this, reg, Utility.GuardRelevanceMode.Local);

            StopAndClearTimer(ref m_CriminalWhackTime);
            RemoveFromAllGuardCandidates();

            bool nowGuardRelevant = Utility.GuardUtility.IsGuardRelevant(this, reg, Utility.GuardRelevanceMode.Local);

            if (wasGuardRelevant && !nowGuardRelevant)
                this.SendLocalizedMessage(502276); // Guards can no longer be called on you.
        }
        #endregion GuardWhackWindow

        #region CriminalTravelTimer

        private Timer m_CriminalTravelTime;

        // Travel spell/moongate check:
        // true  => travel is allowed (as far as criminality is concerned)
        // false => still in the short "too hot to travel" window
        public bool CanCriminalTravel
        {
            get { return (m_CriminalTravelTime == null); }
        }

        private void StartCriminalTravelLock(TimeSpan delay)
        {
            StopAndClearTimer(ref m_CriminalTravelTime);
            m_CriminalTravelTime = Timer.DelayCall(delay, new TimerStateCallback(EndCriminalTravelLock), null);
        }

        private void EndCriminalTravelLock(object state)
        {
            StopAndClearTimer(ref m_CriminalTravelTime);

            // Intentionally no message here.
            // Travel checks should standardize messaging at the callsite (spells/moongates).
        }

        #endregion CriminalTravelTimer

        // Central entry point for opening/refreshing short criminal windows.
        // IMPORTANT: This must be safe under re-entrancy (harmful flows often call CriminalAction multiple times).
        private void BeginCriminalWindows(TimeSpan whack, TimeSpan travel, bool message)
        {
            SceneOfTheCrime = Location;

            Region reg = this.Region;

            bool wasGuardRelevant =
                Utility.GuardUtility.IsGuardRelevant(this, reg, Utility.GuardRelevanceMode.Local);

            StartGuardWhackWindow(whack);
            StartCriminalTravelLock(travel);

            bool nowGuardRelevant =
                Utility.GuardUtility.IsGuardRelevant(this, reg, Utility.GuardRelevanceMode.Local);

            // Only announce guard exposure on transition. Otherwise, fall back to the
            // generic criminal-act message if requested.
            if (!wasGuardRelevant && nowGuardRelevant)
            {
                if (reg != null && reg.IsGuarded)
                {
                    this.SendLocalizedMessage(502275); // Guards can now be called on you!
                }
                else if (message)
                {
                    this.SendLocalizedMessage(1005041); // You've committed a criminal act!!
                }
            }
            else if (message)
            {
                this.SendLocalizedMessage(1005041); // You've committed a criminal act!!
            }
        }

        // Backwards-compatible default behavior.
        // New code should prefer MakeCriminal(gray, whack, travel) for explicitness.

        public void CriminalityEnd()
        {
            SceneOfTheCrime = Point3D.Zero;

            // Once no longer criminal, criminality-based travel suppression should end.
            StopAndClearTimer(ref m_CriminalTravelTime);

            // Conservative cleanup: if criminality is forcibly cleared, ensure guard window and candidates are cleared too.
            StopAndClearTimer(ref m_CriminalWhackTime);
            RemoveFromAllGuardCandidates();
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public virtual bool Criminal
        {
            get { return GetMobileBool(MobileBoolTable.Criminal); }
            set
            {
                if (value)
                {
                    // Legacy meaning: default gray + default windows, and do NOT inform region.
                    // (Matches your current property behavior, which never calls reg.OnCriminalAction.)
                    MakeCriminal(DefaultCriminalGrayMinutes, DefaultGuardWhackWindowDelay,
                        DefaultCriminalTravelLockDelay, regionInform: false);
                }
                else
                {
                    ClearCriminality();
                }
            }
        }

        // Common-case API: apply default gray/whack/travel and record forensic context.
        // Intended replacement for legacy "Criminal = true" call sites.
        public void MakeCriminal(CriminalityContext ctx, bool message)
        {
            // Match legacy Criminal=true behavior: default durations, no region inform.
            MakeCriminal(DefaultCriminalGrayMinutes, DefaultGuardWhackWindowDelay, DefaultCriminalTravelLockDelay, ctx,
                message: message, regionInform: false);
        }

        // Legacy helper - retains existing behavior (gray duration only).
        // Prefer MakeCriminal(gray, whack, travel) in new code for explicitness.
        public void MakeCriminal(TimeSpan howLong, bool message = true, bool regionInform = true)
        {
            MakeCriminal(howLong, DefaultGuardWhackWindowDelay, DefaultCriminalTravelLockDelay,
                (CriminalityContext)null, message, regionInform);
        }

        // New explicit API: one source of truth for criminality timers.
        // This lets thieves/griefers specify custom gray/whack/travel durations without hacks elsewhere.
        public void MakeCriminal(TimeSpan gray, TimeSpan whack, TimeSpan travel, bool message = true,
            bool regionInform = true)
        {
            MakeCriminal(gray, whack, travel, (CriminalityContext)null, message, regionInform);
        }

        public void MakeCriminal(TimeSpan gray, TimeSpan whack, TimeSpan travel, CriminalityContext? ctx,
            bool message = true, bool regionInform = true)
        {
            if (!GetMobileBool(MobileBoolTable.Criminal))
            {
                SetMobileBool(MobileBoolTable.Criminal, true);
                Delta(MobileDelta.Noto);
                InvalidateProperties();
            }

            if (m_ExpireCriminal == null)
            {
                m_ExpireCriminal = new ExpireCriminalTimer(this, gray, ctx);
            }
            else
            {
                try
                {
                    // Do not shorten an existing longer timer (serialization/extended gray).
                    if (DateTime.UtcNow + gray > m_ExpireCriminal.NextTick)
                    {
                        m_ExpireCriminal.Stop();
                        m_ExpireCriminal = new ExpireCriminalTimer(this, gray, ctx);
                    }
                    else if (ctx != null)
                    {
                        // Timer not extended, but we have new forensic information.
                        // Prefer "last cause wins" for WhatHaveIDone().
                        m_ExpireCriminal.Context = ctx;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);

                    // Conservative: rebuild timer with requested duration.
                    m_ExpireCriminal.Stop();
                    m_ExpireCriminal = new ExpireCriminalTimer(this, gray, ctx);
                }
            }

            if (!m_ExpireCriminal.Running)
                m_ExpireCriminal.Start();

            // Messaging is handled by BeginCriminalWindows (CJS-owned).
            // We still notify the region for behavioral reactions (guards, etc.),
            // but suppress region messaging to avoid duplication or inconsistency.
            BeginCriminalWindows(whack, travel, message);

            if (regionInform)
            {
                Region reg = Region;
                if (reg != null)
                    reg.OnCriminalAction(this, message: false);
            }
        }

        public CriminalityContext CreateCriminalityContext(Mobile? target, object? source)
        {
            string targetName = !string.IsNullOrWhiteSpace(target?.Name)
                ? target.Name
                : "unknown";

            string text;

            if (source is string s)
            {
                switch (s)
                {
                    case "CorpseLoot":
                        text = !string.IsNullOrWhiteSpace(target?.Name)
                            ? $"looted the corpse of {targetName}"
                            : "looted a corpse";
                        break;

                    default:
                        text = !string.IsNullOrWhiteSpace(target?.Name)
                            ? $"committed a criminal act against {targetName} ({s})"
                            : $"committed a criminal act ({s})";
                        break;
                }
            }
            else
            {
                string? sourceName = source?.GetType().Name;

                if (!string.IsNullOrEmpty(sourceName))
                    text = $"attacked {targetName} ({sourceName})";
                else
                    text = $"attacked {targetName}";
            }

            return new CriminalityContext
            {
                Source = source,
                Target = target,
                Text = text
            };
        }

        private void ClearCriminality()
        {
            if (!GetMobileBool(MobileBoolTable.Criminal) && m_ExpireCriminal == null)
                return;

            if (GetMobileBool(MobileBoolTable.Criminal))
            {
                SetMobileBool(MobileBoolTable.Criminal, false);
                Delta(MobileDelta.Noto);
                InvalidateProperties();
            }

            if (m_ExpireCriminal != null)
            {
                m_ExpireCriminal.Stop();
                m_ExpireCriminal = null;
            }

            CriminalityEnd();
        }

        #endregion Criminality

        #region Murder Info

        public struct MurderStatusInfo
        {
            public bool LocalWhackable;
            public bool GlobalWhackable;
            public TimeSpan TimeToBluePrison;
            public TimeSpan TimeToBlueOverworld;
        }

        public static class MurderStatusUtility
        {
            private const int BlueThresholdLongTerms = 4;

            public static MurderStatusInfo GetStatus(PlayerMobile pm)
            {
                MurderStatusInfo info = new MurderStatusInfo();

                if (pm == null)
                    return info;

                Region reg = pm.Region;

                // In guarded regions, defer to the region's full guard-relevance rule.
                info.LocalWhackable = Utility.GuardUtility.IsGuardRelevant(pm, reg, Utility.GuardRelevanceMode.Local);
                info.GlobalWhackable = Utility.GuardUtility.IsGuardRelevant(pm, reg, Utility.GuardRelevanceMode.Global);

                int lt = pm.LongTermMurders;

                if (lt <= BlueThresholdLongTerms)
                {
                    info.TimeToBluePrison = TimeSpan.Zero;
                    info.TimeToBlueOverworld = TimeSpan.Zero;
                    return info;
                }

                int decaysNeeded = lt - BlueThresholdLongTerms;

                TimeSpan untilNext = pm.LongTermElapse - pm.GameTime;
                if (untilNext < TimeSpan.Zero)
                    untilNext = TimeSpan.Zero;

                // Prison decay period
                TimeSpan prisonPeriod = TimeSpan.FromHours(20);

                // Overworld decay period
                TimeSpan overworldPeriod = TimeSpan.FromHours(40);

                info.TimeToBluePrison =
                    untilNext + TimeSpan.FromTicks(prisonPeriod.Ticks * (decaysNeeded - 1));

                info.TimeToBlueOverworld =
                    untilNext + TimeSpan.FromTicks(overworldPeriod.Ticks * (decaysNeeded - 1));

                return info;
            }
        }

        #endregion Murder Info

        #region Murder Counts

        private int m_LongTermMurders, m_ShortTermMurders;

        public static class MurderCountUtility
        {
            // Centralized decay intervals - single source of truth to prevent drift
            public static readonly TimeSpan ShortTermNormalInterval = TimeSpan.FromHours(8);
            public static readonly TimeSpan ShortTermPrisonInterval = TimeSpan.FromHours(4);
            public static readonly TimeSpan LongTermNormalInterval = TimeSpan.FromHours(40);
            public static readonly TimeSpan LongTermPrisonInterval = TimeSpan.FromHours(20);

            public static readonly TimeSpan OfflineShortDecayThreshold = TimeSpan.FromHours(CoreAI.OfflineShortsDecayHours);

            // Murder status threshold (long-term murders needed to be "blue" eligible)
            private const int BlueThresholdLongTerms = 4;

            /// <summary>
            /// Performs a decay tick on short- and long-term murder counts.
            /// Applies prison acceleration when appropriate.
            /// </summary>
            public static void DecayKills(PlayerMobile pm)
            {
                if (pm == null) return;

                DateTime now = DateTime.UtcNow;

                // Short-term decay (murders + criminal counts)
                if (pm.ShortTermElapse < pm.GameTime ||
                    (CoreAI.OfflineShortsDecay != 0 && (now - pm.LastShortDecayed) > OfflineShortDecayThreshold))
                {
                    pm.LastShortDecayed = now;

                    TimeSpan interval = (Core.RuleSets.AngelIslandRules() && pm.PrisonInmate && pm.Alive)
                        ? ShortTermPrisonInterval
                        : ShortTermNormalInterval;

                    pm.ShortTermElapse = pm.GameTime + interval;

                    if (pm.ShortTermMurders > 0) --pm.ShortTermMurders;
                    if (pm.ShortTermCriminalCounts > 0) --pm.ShortTermCriminalCounts;
                }

                // Long-term decay
                if (pm.LongTermElapse < pm.GameTime)
                {
                    TimeSpan interval =
                        (Core.RuleSets.AngelIslandRules() || Core.RuleSets.SeasonsRules() || Core.RuleSets.UpheavalRules())
                        && pm.PrisonInmate && pm.Alive
                        ? LongTermPrisonInterval
                        : LongTermNormalInterval;

                    pm.LongTermElapse = pm.GameTime + interval;

                    if (pm.LongTermMurders > 0) --pm.LongTermMurders;
                }
            }

            /// <summary>
            /// Initializes short- and long-term elapse times based on current PrisonInmate + Alive state.
            /// Called from PlayerMobile constructor after loading or creating the mobile.
            /// </summary>
            public static void InitializeMurderElapseTimes(PlayerMobile pm)
            {
                if (pm == null) return;

                TimeSpan shortInterval = (pm.PrisonInmate && pm.Alive) ? ShortTermPrisonInterval : ShortTermNormalInterval;
                TimeSpan longInterval = (pm.PrisonInmate && pm.Alive) ? LongTermPrisonInterval : LongTermNormalInterval;

                pm.ShortTermElapse = pm.GameTime + shortInterval;
                pm.LongTermElapse = pm.GameTime + longInterval;
            }

            /// <summary>
            /// Resets elapse timers after a kill (fresh intervals + last decay reset).
            /// </summary>
            public static void ResetKillTimers(PlayerMobile pm)
            {
                if (pm == null) return;

                TimeSpan shortInterval = (pm.PrisonInmate && pm.Alive) ? ShortTermPrisonInterval : ShortTermNormalInterval;
                TimeSpan longInterval = (pm.PrisonInmate && pm.Alive) ? LongTermPrisonInterval : LongTermNormalInterval;

                pm.ShortTermElapse = pm.GameTime + shortInterval;
                pm.LongTermElapse = pm.GameTime + longInterval;

                pm.LastShortDecayed = DateTime.UtcNow;
            }

            /// <summary>
            /// Caps remaining decay time when entering prison (accelerated rates if alive).
            /// </summary>
            public static void OnBecomePrisonInmate(PlayerMobile pm)
            {
                if (pm == null) return;

                TimeSpan remShort = pm.ShortTermElapse - pm.GameTime;
                if (remShort > ShortTermPrisonInterval)
                {
                    TimeSpan newShort = pm.Alive ? ShortTermPrisonInterval : ShortTermNormalInterval;
                    pm.ShortTermElapse = pm.GameTime + newShort;
                }

                TimeSpan remLong = pm.LongTermElapse - pm.GameTime;
                if (remLong > LongTermPrisonInterval)
                {
                    TimeSpan newLong = (pm.Alive &&
                                        (Core.RuleSets.AngelIslandRules() || Core.RuleSets.SeasonsRules() || Core.RuleSets.UpheavalRules()))
                        ? LongTermPrisonInterval
                        : LongTermNormalInterval;

                    pm.LongTermElapse = pm.GameTime + newLong;
                }
            }

            /// <summary>
            /// Adds back normal decay time when leaving prison (pauses acceleration benefit).
            /// </summary>
            public static void OnReleaseFromPrison(PlayerMobile pm)
            {
                if (pm == null) return;

                pm.ShortTermElapse += (ShortTermNormalInterval - ShortTermPrisonInterval);
                pm.LongTermElapse += (LongTermNormalInterval - LongTermPrisonInterval);
            }

            /// <summary>
            /// Applies partial decay credit for time spent dead while imprisoned.
            /// Credit = half of (dead time % normal interval).
            /// </summary>
            public static void ApplyResurrectionDecayCredit(PlayerMobile pm, DateTime lastDeathTime)
            {
                if (pm == null || !pm.PrisonInmate) return;

                TimeSpan deadTime = lastDeathTime == DateTime.MinValue ? TimeSpan.Zero : DateTime.UtcNow - lastDeathTime;

                // Short-term credit
                double deadSecShort = deadTime.TotalSeconds % ShortTermNormalInterval.TotalSeconds;
                TimeSpan creditShort = TimeSpan.FromSeconds(deadSecShort / 2.0);
                TimeSpan reductionShort = ShortTermPrisonInterval - creditShort;
                if (reductionShort > TimeSpan.Zero)
                {
                    pm.ShortTermElapse -= reductionShort;
                    if (pm.ShortTermElapse < pm.GameTime) pm.ShortTermElapse = pm.GameTime;
                }

                // Long-term credit
                double deadSecLong = deadTime.TotalSeconds % LongTermNormalInterval.TotalSeconds;
                TimeSpan creditLong = TimeSpan.FromSeconds(deadSecLong / 2.0);
                TimeSpan reductionLong = LongTermPrisonInterval - creditLong;
                if (reductionLong > TimeSpan.Zero)
                {
                    pm.LongTermElapse -= reductionLong;
                    if (pm.LongTermElapse < pm.GameTime) pm.LongTermElapse = pm.GameTime;
                }
            }

            /// <summary>
            /// Handles death as inmate: records time and extends timers.
            /// </summary>
            public static void OnPrisonInmateDeath(PlayerMobile pm)
            {
                if (pm == null || !pm.PrisonInmate) return;

                pm.InmateLastDeathTime = DateTime.UtcNow;
                pm.ShortTermElapse += ShortTermPrisonInterval;
                pm.LongTermElapse += LongTermPrisonInterval;
            }

            /// <summary>
            /// Reduces elapse timers by given hours (bribe/parole effect).
            /// Clamps to GameTime and triggers decay.
            /// </summary>
            public static void ReduceElapseByHours(PlayerMobile pm, double hours)
            {
                if (pm == null || hours <= 0) return;

                TimeSpan reduction = TimeSpan.FromHours(hours);

                if (pm.ShortTermElapse > pm.GameTime)
                {
                    pm.ShortTermElapse -= reduction;
                    if (pm.ShortTermElapse < pm.GameTime) pm.ShortTermElapse = pm.GameTime;
                }

                if (pm.LongTermElapse > pm.GameTime)
                {
                    pm.LongTermElapse -= reduction;
                    if (pm.LongTermElapse < pm.GameTime) pm.LongTermElapse = pm.GameTime;
                }

                DecayKills(pm);
            }

            /// <summary>
            /// Returns remaining time until next short-term decay (min of online/offline if enabled).
            /// </summary>
            public static TimeSpan GetRemainingShortDecayTime(PlayerMobile pm)
            {
                if (pm == null) return TimeSpan.Zero;

                TimeSpan online = pm.ShortTermElapse - pm.GameTime;

                if (CoreAI.OfflineShortsDecay == 0) return online;

                TimeSpan offline = pm.LastShortDecayed.Add(OfflineShortDecayThreshold) - DateTime.UtcNow;

                return online < offline ? online : offline;
            }

            /// <summary>
            /// Returns remaining time until next long-term decay.
            /// </summary>
            public static TimeSpan GetRemainingLongDecayTime(PlayerMobile pm)
            {
                return pm == null ? TimeSpan.Zero : pm.LongTermElapse - pm.GameTime;
            }

            /// <summary>
            /// Returns the current murder status info for a player:
            ///   - Local/Global whackability (via GuardUtility)
            ///   - Estimated time to blue eligibility in prison vs overworld
            /// Uses centralized decay intervals and current elapse/GameTime.
            /// </summary>
            public static MurderStatusInfo GetMurderStatus(PlayerMobile pm)
            {
                MurderStatusInfo info = new MurderStatusInfo();

                if (pm == null)
                    return info;

                Region reg = pm.Region;

                // Guard whackability (local = region-specific, global = anywhere)
                info.LocalWhackable = Utility.GuardUtility.IsGuardRelevant(pm, reg, Utility.GuardRelevanceMode.Local);
                info.GlobalWhackable = Utility.GuardUtility.IsGuardRelevant(pm, reg, Utility.GuardRelevanceMode.Global);

                int longTermCount = pm.LongTermMurders;

                if (longTermCount <= BlueThresholdLongTerms)
                {
                    info.TimeToBluePrison = TimeSpan.Zero;
                    info.TimeToBlueOverworld = TimeSpan.Zero;
                    return info;
                }

                // Decays needed to reach blue eligibility (after threshold)
                int decaysNeeded = longTermCount - BlueThresholdLongTerms;

                // Time until next decay tick
                TimeSpan untilNext = pm.LongTermElapse - pm.GameTime;
                if (untilNext < TimeSpan.Zero)
                    untilNext = TimeSpan.Zero;

                // Full periods for remaining decays
                TimeSpan prisonPeriod = LongTermPrisonInterval;     // e.g. 20h
                TimeSpan overworldPeriod = LongTermNormalInterval;      // e.g. 40h

                // Total estimated time: time to next decay + full periods for remaining decays
                info.TimeToBluePrison = untilNext + TimeSpan.FromTicks(prisonPeriod.Ticks * (decaysNeeded - 1));
                info.TimeToBlueOverworld = untilNext + TimeSpan.FromTicks(overworldPeriod.Ticks * (decaysNeeded - 1));

                return info;
            }
        }

        #endregion Murder Counts
    }
}