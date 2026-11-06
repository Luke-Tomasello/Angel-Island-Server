
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
 * VendorAntiBot
 *
 * Purpose
 * -------
 * VendorAntiBot is a lightweight, behavior-driven attendance and anti-automation
 * system for vendor commerce. Its goal is not to prevent selling, but to ensure
 * that prolonged or suspicious vendor activity is being performed by a present
 * human player.
 *
 * The system is intentionally layered, moving from simple, low-friction checks
 * to more sophisticated behavior analysis only when warranted.
 *
 *
 * Level 1: Attendance, not punishment
 * -----------------------------------
 * At its most basic level, VendorAntiBot occasionally asks a player to confirm
 * their presence via a benign RTT (attendance check). Passing the check allows
 * normal play to continue uninterrupted.
 *
 * This establishes a simple invariant:
 *   "Extended vendor activity should occasionally confirm a human is present."
 *
 * Casual or infrequent vendor use almost never triggers a check.
 *
 *
 * Level 2: Session awareness
 * --------------------------
 * The system is session-aware. The first vendor interaction after a period of
 * inactivity is always free and never triggers an RTT.
 *
 * This avoids annoying players who:
 *   - sell a few items
 *   - log in briefly
 *   - interact with vendors sporadically
 *
 * Only continued activity within a session can trigger further scrutiny.
 *
 *
 * Level 3: Pressure accumulation with forgiveness
 * ------------------------------------------------
 * Vendor activity generates "pressure", which represents how much verification
 * is warranted. Pressure:
 *   - accumulates with vendor transactions
 *   - decays naturally over time when activity stops
 *
 * This allows the system to remember behavior across short breaks, while still
 * forgiving inactivity. Bots that return on a fixed cadence accumulate pressure;
 * normal players cool off automatically.
 *
 *
 * Level 4: Probability, not hard thresholds
 * -----------------------------------------
 * Pressure does not trigger checks at a fixed count or time. Instead, it is
 * converted into a probability that increases as pressure grows.
 *
 * This avoids predictable patterns that bots can game, while remaining fair
 * and unobtrusive for legitimate players.
 *
 *
 * Level 5: Behavioral classification (suspicion)
 * -----------------------------------------------
 * Not all vendor activity is equal. The system distinguishes between:
 *   - benign behavior (eg. crafted goods, mixed sales)
 *   - suspicious behavior (eg. repetitive non-crafted junk loops)
 *
 * Suspicion is determined by unit-weighted dominance and crafted vs non-crafted
 * ratios. Throwing in occasional noise items does not clear suspicion; only
 * materially changing behavior does.
 *
 *
 * Level 6: Adaptive cooldowns
 * ---------------------------
 * After an attendance check:
 *   - benign behavior receives a long cooldown (rare checks)
 *   - suspicious behavior receives a short cooldown (periodic rechecks)
 *
 * This prevents the "pass once and automate forever" loophole, while still
 * respecting legitimate play.
 *
 *
 * Level 7: Differential pressure drain
 * ------------------------------------
 * Passing an attendance check does not always reset pressure completely.
 * For benign behavior, pressure is fully drained.
 * For suspicious behavior, some pressure is retained.
 *
 * This ensures that ongoing automation-like patterns continue to be monitored
 * even if the player was briefly present at the time of the first check.
 *
 *
 * Design philosophy
 * -----------------
 * - This system does not accuse or punish; it verifies presence.
 * - Normal play should rarely notice it.
 * - Suspicious loops should be continuously accountable.
 * - Behavior matters more than raw volume.
 * - Memory and forgiveness are both essential.
 *
 * VendorAntiBot is intentionally conservative, explainable, and layered.
 * Each level only activates when the simpler ones are insufficient.
 */

/* Scripts\Mobiles\Vendors\BaseVendor.VendorAntiBot.cs
 * Changelog
 *  2/17/2026, Adam
 *  first time checkin
 *  2/17/2026, GPT
 *  refactor: pressure-based probabilistic scheduling (Variant B)
 *  - session warmup (no RTT on first vendor interaction after idle)
 *  - pressure accumulates across visits + decays over time (catches 20m cadence bots)
 *  - pressure weighting uses Item.PlayerCrafted (crafted = low pressure)
 */

using Server.Accounting;
using Server.Diagnostics;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public abstract partial class BaseVendor
    {
        internal static class VendorAntiBot
        {
            // ------------------------------------------------------------
            // Live toggles (keep simple for now)
            // ------------------------------------------------------------
            public static bool Enabled = true; // when false, no scheduling/gating is active
            public static bool LogOnly = true;  // for later; state is still maintained

            // ------------------------------------------------------------
            // Pressure knobs (TODO: wire into vendor console)
            // ------------------------------------------------------------

            // TODO (Vendor console knob): Session boundary. If no vendor activity for this long,
            // treat next transaction as a new session (warmup = no RTT on that transaction).
            public static readonly TimeSpan IdleReset = TimeSpan.FromMinutes(45);

            // TODO (Vendor console knob): Pressure half-life. Larger = pressure persists longer.
            // This is the "anti-drive-by" knob that catches 20-minute cadence bots.
            public static readonly TimeSpan PressureHalfLife = TimeSpan.FromHours(3);

            // TODO (Vendor console knob): Base pressure added per vendor transaction.
            public const double BasePressurePerTxn = 8.0;

            // TODO (Vendor console knob): Crafted items contribute very little pressure.
            public const double CraftedItemPressure = 0.10;

            // TODO (Vendor console knob): Non-crafted items contribute more pressure.
            public const double NonCraftedItemPressure = 1.50;

            // TODO (Vendor console knob): If one type dominates the sold set, treat as more botty.
            public const double RepetitiveTypeMultiplier = 1.25;

            // TODO (Vendor console knob): Add some pressure based on countedGold (volume proxy).
            // Set to 0 to disable.
            public const double GoldPressureDivisor = 2000.0;

            // TODO (Vendor console knob): Probability scale factor. Higher K => fewer RTTs at same pressure.
            public const double ProbabilityScaleK = 450.0;

            // TODO (Vendor console knob): Cap probability to avoid harassment.
            public const double ProbabilityCap = 0.25; // 25%

            // TODO (Vendor console knob): Minimum time between RTT issuances.
            public static readonly TimeSpan ChallengeCooldown = TimeSpan.FromHours(6);

            // TODO (Vendor console knob): After pass, multiply remaining pressure (0 = full drain).
            public const double PassPressureMultiplier = 0.0;

            // TODO (Vendor console knob): After fail/timeout, floor pressure to this (keeps retries "hot").
            public const double FailPressureFloor = 250.0;

            // TODO (Vendor console knob): Bar duration after fail/timeout.
            public static readonly TimeSpan DefaultBarDuration = TimeSpan.FromMinutes(2);

            // TODO (Vendor console knob): cooldown after a PASS (or issuance) for benign activity.
            public static readonly TimeSpan ChallengeCooldownBenign = TimeSpan.FromHours(6);

            // TODO (Vendor console knob): cooldown after a PASS (or issuance) for suspicious activity (junk loops).
            public static readonly TimeSpan ChallengeCooldownSuspicious = TimeSpan.FromMinutes(15);

            // TODO (Vendor console knob): when suspicious, keep some pressure after PASS (0.0 = full drain, 1.0 = no drain).
            public const double PassPressureMultiplierSuspicious = 0.75;

            // Internal thresholds (keep fixed unless you have a strong reason to expose them)
            private const double SuspiciousDominanceThreshold = 0.80;   // 80%+ of units are one type
            private const double SuspiciousNonCraftedRatio = 0.90;      // 90%+ of units are non-crafted
            private const int SuspiciousMinUnits = 8;                   // ignore tiny sales

            // ------------------------------------------------------------
            // Per-player state
            // ------------------------------------------------------------
            private sealed class State
            {
                public double Pressure;

                public DateTime LastActivityUtc;     // for decay + session detection
                public DateTime LastChallengeUtc;    // for cooldown

                public bool PendingChallenge;
                public DateTime BarredUntilUtc;

                public override string ToString()
                {
                    return string.Format(
                        "Pressure={0:F2}, LastActivity={1:u}, LastChallenge={2:u}, Pending={3}, BarredUntil={4:u}",
                        Pressure, LastActivityUtc, LastChallengeUtc, PendingChallenge, BarredUntilUtc);
                }
            }

            // Keying: prefer Account GUID if available, else Mobile serial
            // NOTE: Keep key stable across sessions when possible (Account GUID).
            private static readonly Dictionary<long, State> _state = new Dictionary<long, State>();

            // NOTE: Signature changed to accept sold items so we can use Item.PlayerCrafted.
            // Call site should pass the sold list from OnSellItems().
            public static void OnSellTransaction(
    Mobile seller,
    BaseVendor vendor,
    List<SoldSummary>? sold,
    int giveGold,
    int countedGold,
    Dictionary<Type, int>? typeCounts)
            {
                if (!Enabled || seller == null || vendor == null)
                    return;

                DateTime now = DateTime.UtcNow;

                if (IsBarred(seller) || IsPending(seller))
                    return;

                State st = GetOrCreateState(seller);

                ApplyDecay(st, now);

                bool isNewSession =
                    (st.LastActivityUtc == DateTime.MinValue) ||
                    (now - st.LastActivityUtc) >= IdleReset;

                st.LastActivityUtc = now;

                double added = ComputePressureAdded(sold, countedGold, typeCounts);
                st.Pressure += added;

                // Session warmup: first interaction after idle never triggers RTT.
                if (isNewSession)
                    return;

                // NEW: classify suspicious basket, then use an effective cooldown.
                bool suspicious = IsSuspiciousBasket(sold, typeCounts);

                TimeSpan cooldown = suspicious ? ChallengeCooldownSuspicious : ChallengeCooldownBenign;

                if (st.LastChallengeUtc != DateTime.MinValue &&
                    (now - st.LastChallengeUtc) < cooldown)
                    return;

                double p = (ProbabilityScaleK > 0.0) ? (st.Pressure / ProbabilityScaleK) : 0.0;
                if (p > ProbabilityCap) p = ProbabilityCap;
                if (p < 0.0) p = 0.0;

                if (p <= 0.0 || !Roll(p))
                    return;

                st.PendingChallenge = true;
                st.LastChallengeUtc = now;

                uint acctGuid = GetAccountGuid(seller);

                // NEW: include suspicion in context
                VendorRttContext ctx = new VendorRttContext(acctGuid, (int)vendor.Serial, suspicious);

                try
                {
                    if (seller.NetState == null)
                    {
                        st.PendingChallenge = false;
                        return;
                    }

                    seller.SendGump(new Server.RTT.SmallPagedRTTGump(
                        seller,
                        "Vendor attendance check",
                        "Vendor",
                        new Server.RTT.RTTCallback(OnVendorRttResult),
                        ctx));

                    if (LogOnly)
                    {
                        LogHelper lh = new LogHelper("VendorAntiBot.log", false, true);

                        lh.Log(
                            LogType.Mobile,
                            seller,
                            string.Format(
                                "Issued vendor RTT acctGuid={0} vendor=0x{1:X8} suspicious={2} cooldown={3} p={4:F3} pressure={5:F2} +{6:F2} giveGold={7} countedGold={8} types={9}",
                                acctGuid,
                                (int)vendor.Serial,
                                suspicious,
                                Utility.FormatShortTime(cooldown),
                                p,
                                st.Pressure,
                                added,
                                giveGold,
                                countedGold,
                                typeCounts != null ? typeCounts.Count : 0));

                        lh.Finish();
                    }
                }
                catch (Exception e)
                {
                    st.PendingChallenge = false;
                    LogHelper.LogException(e);
                }
            }

            public struct SoldSummary
            {
                public Type ItemType;
                public int Amount;
                public bool PlayerCrafted;

                public SoldSummary(Type itemType, int amount, bool playerCrafted)
                {
                    ItemType = itemType;
                    Amount = amount;
                    PlayerCrafted = playerCrafted;
                }
            }
            private sealed class VendorRttContext
            {
                public uint AccountGuid;
                public int VendorSerial;
                public DateTime IssuedUtc;

                // NEW: carry suspicion across the RTT boundary so pass handling can drain correctly.
                public bool Suspicious;

                public VendorRttContext(uint accountGuid, int vendorSerial, bool suspicious)
                {
                    AccountGuid = accountGuid;
                    VendorSerial = vendorSerial;
                    IssuedUtc = DateTime.UtcNow;
                    Suspicious = suspicious;
                }
            }

            // ------------------------------------------------------------
            // Public state queries (used later by vendor pipeline)
            // ------------------------------------------------------------

            private static bool IsSuspiciousBasket(
                List<SoldSummary>? sold,
                Dictionary<Type, int>? typeCounts)
            {
                if (sold == null || sold.Count == 0 || typeCounts == null || typeCounts.Count == 0)
                    return false;

                int totalUnits = 0;
                int nonCraftedUnits = 0;

                for (int i = 0; i < sold.Count; i++)
                {
                    int amt = sold[i].Amount;
                    if (amt <= 0)
                        continue;

                    totalUnits += amt;

                    if (!sold[i].PlayerCrafted)
                        nonCraftedUnits += amt;
                }

                if (totalUnits < SuspiciousMinUnits)
                    return false;

                // Max bucket dominance by units (typeCounts is already unit-weighted via IncTypeCount(..., amount)).
                int maxBucket = 0;

                foreach (KeyValuePair<Type, int> kvp in typeCounts)
                {
                    int v = kvp.Value;
                    if (v > maxBucket)
                        maxBucket = v;
                }

                if (maxBucket <= 0)
                    return false;

                double dominance = (double)maxBucket / (double)totalUnits;
                if (dominance < SuspiciousDominanceThreshold)
                    return false;

                double nonCraftedRatio = (double)nonCraftedUnits / (double)totalUnits;
                if (nonCraftedRatio < SuspiciousNonCraftedRatio)
                    return false;

                return true;
            }
            public static bool IsBarred(Mobile m)
            {
                if (m == null)
                    return false;

                State st = GetOrCreateState(m);

                DateTime now = DateTime.UtcNow;
                return now < st.BarredUntilUtc;
            }

            public static TimeSpan GetBarredRemaining(Mobile m)
            {
                if (m == null)
                    return TimeSpan.Zero;

                State st = GetOrCreateState(m);

                DateTime now = DateTime.UtcNow;
                if (now >= st.BarredUntilUtc)
                    return TimeSpan.Zero;

                return st.BarredUntilUtc - now;
            }

            // True if an attendance challenge is currently outstanding.
            public static bool IsPending(Mobile m)
            {
                if (m == null)
                    return false;

                State st = GetOrCreateState(m);
                return st.PendingChallenge;
            }

            // ------------------------------------------------------------
            // State mutation (used later by callback + admin tooling)
            // ------------------------------------------------------------

            public static void BarFor(Mobile m, TimeSpan duration)
            {
                if (m == null)
                    return;

                if (duration <= TimeSpan.Zero)
                    duration = DefaultBarDuration;

                State st = GetOrCreateState(m);

                DateTime now = DateTime.UtcNow;

                // Extend if already barred; do not shorten.
                DateTime candidate = now + duration;
                if (candidate > st.BarredUntilUtc)
                    st.BarredUntilUtc = candidate;
            }

            // ------------------------------------------------------------
            // Internals
            // ------------------------------------------------------------
            private static State GetOrCreateState(Mobile m)
            {
                long key = GetKey(m);

                if (!_state.TryGetValue(key, out State? st) || st == null)
                {
                    st = new State
                    {
                        Pressure = 0.0,
                        LastActivityUtc = DateTime.MinValue,
                        LastChallengeUtc = DateTime.MinValue,
                        PendingChallenge = false,
                        BarredUntilUtc = DateTime.MinValue
                    };

                    _state[key] = st;
                }

                return st;
            }

            private static void ApplyDecay(State st, DateTime now)
            {
                if (st == null)
                    return;

                if (st.LastActivityUtc == DateTime.MinValue)
                    return;

                TimeSpan dt = now - st.LastActivityUtc;
                if (dt <= TimeSpan.Zero)
                    return;

                if (PressureHalfLife <= TimeSpan.Zero)
                    return;

                // Half-life decay: Pressure *= 0.5^(dt / halfLife)
                double halfLives = dt.TotalSeconds / PressureHalfLife.TotalSeconds;
                double factor = Math.Pow(0.5, halfLives);

                st.Pressure *= factor;

                if (st.Pressure < 0.01)
                    st.Pressure = 0.0;
            }

            private static double ComputePressureAdded(
                List<SoldSummary>? sold,
                int countedGold,
                Dictionary<Type, int>? typeCounts)
            {
                double pressure = BasePressurePerTxn;

                // Volume proxy (optional)
                if (GoldPressureDivisor > 0.0 && countedGold > 0)
                    pressure += (countedGold / GoldPressureDivisor);

                // Per-item pressure: crafted vs non-crafted (SoldSummary is a struct; no null checks)
                if (sold != null && sold.Count > 0)
                {
                    double itemPressure = 0.0;

                    for (int i = 0; i < sold.Count; i++)
                    {
                        SoldSummary si = sold[i];

                        int amt = si.Amount;
                        if (amt <= 0)
                            continue;

                        // Weight by units sold (important): selling 50 boots is not the same as selling 1 boot.
                        if (si.PlayerCrafted)
                            itemPressure += CraftedItemPressure * amt;
                        else
                            itemPressure += NonCraftedItemPressure * amt;
                    }

                    pressure += itemPressure;
                }

                // Repetitive-type multiplier: if most sold units are the same type, amplify.
                // NOTE: typeCounts is expected to be "units sold per type" (you already call IncTypeCount with amount).
                if (typeCounts != null && typeCounts.Count > 0)
                {
                    int totalUnits = 0;
                    int maxBucket = 0;

                    foreach (KeyValuePair<Type, int> kvp in typeCounts)
                    {
                        int v = kvp.Value;
                        if (v <= 0)
                            continue;

                        totalUnits += v;

                        if (v > maxBucket)
                            maxBucket = v;
                    }

                    if (totalUnits > 0)
                    {
                        double dominance = (double)maxBucket / (double)totalUnits;
                        if (dominance >= SuspiciousDominanceThreshold)
                            pressure *= RepetitiveTypeMultiplier;
                    }
                }

                return pressure;
            }

            private static bool Roll(double p)
            {
                // Utility.RandomDouble() returns [0,1)
                return Utility.RandomDouble() < p;
            }

            private static long GetKey(Mobile m)
            {
                // Prefer Account GUID (stable across sessions)
                if (m != null && m.Account is Account acct)
                    return (long)acct.GUID; // uint -> long, safe

                // Fallback: Mobile serial
                return m != null ? (long)(int)m.Serial : 0;
            }

            private static uint GetAccountGuid(Mobile m)
            {
                if (m != null && m.Account is Account acct)
                    return acct.GUID;

                return 0;
            }

            private static void OnVendorRttResult(Mobile from, Server.RTT.RTTResult result, object state)
            {
                try
                {
                    if (from == null)
                        return;

                    State st = GetOrCreateState(from);

                    // Always clear pending.
                    st.PendingChallenge = false;

                    VendorRttContext? ctx = state as VendorRttContext;
                    bool suspicious = (ctx != null && ctx.Suspicious);

                    if (LogOnly)
                    {
                        LogHelper lh = new LogHelper("VendorAntiBot.log", false, true);

                        lh.Log(
                            LogType.Mobile,
                            from,
                            string.Format(
                                "RTT result={0} suspicious={1} pressureNow={2:F2}",
                                result,
                                suspicious,
                                st.Pressure));

                        lh.Finish();
                    }

                    if (result == Server.RTT.RTTResult.Passed)
                    {
                        st.BarredUntilUtc = DateTime.MinValue;

                        // NEW: differential drain.
                        // Benign: full drain.
                        // Suspicious: keep pressure hot so repeated checks can happen during long loops.
                        st.Pressure *= suspicious ? PassPressureMultiplierSuspicious : PassPressureMultiplier;

                        return;
                    }

                    // Failed or timed out: bar briefly and keep pressure hot.
                    if (st.Pressure < FailPressureFloor)
                        st.Pressure = FailPressureFloor;

                    BarFor(from, DefaultBarDuration);
                }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                }
            }
        }
    }
}

