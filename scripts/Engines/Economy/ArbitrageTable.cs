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

/* Scripts/Engines/Economy/ArbitrageTable.cs
 * ChangeLog:
 *  10/21/2026, Adam
 *      First time checkin. Generic NPC arbitrage protection, replacing r1187's hard-coded shop-goods refusals.
 *
 * OVERVIEW
 *  Items bought from an NPC are marked StoreBought, and the mark follows them through stacking (sticky OR) and
 *  through every registered conversion. The mark only matters where an NPC-funded loop exists: buy from an NPC,
 *  convert with little or no skill, sell to an NPC at a profit. This table finds those loops from the economy
 *  itself (vendor stock and buyback, crafting recipes, conversions) instead of a list of item types:
 *
 *  - minCost(t): the cheapest NPC-funded cost of one unit of t (bought, or made from bought inputs through any number
 *    of stages). Multi-use items (a flour sack) are costed per use; water is free.
 *  - valueFrom(t): the most an NPC eventually pays for one unit of t (its own buyback, or what it can become).
 *  - t is SENSITIVE when it can be bought (directly or through conversions) and minCost(t) < valueFrom(t): it lies on
 *    a profitable route. That includes inputs that are never resold themselves (hides, flour sacks).
 *
 *  SKILL ELIGIBILITY. Skilled crafting is allowed to profit. Detection is evaluated for a crafter of exactly
 *  SkillThreshold (T, default 30, effective skill including tool bonuses): each conversion uses its real success chance
 *  at T (CraftItem formula: chance at minimum, linear to 100% at maximum), the materials a failure loses (half for
 *  ordinary recipes, a third on average for use-all recipes) and the expected exceptional payout. A route that is
 *  profitable for that crafter (break-even skill below T) is prohibited; a route that only pays above T is skilled
 *  crafting and is left alone. Conversions that cannot be attempted at T are not used.
 *
 *  SAFE PRICE. For a marked, sensitive item the buyback is capped below the best case of every route a T crafter can
 *  use (100% success, no failure losses): safe = ceil(minCostBest) - 1, per item. 0 means the NPC refuses it.
 *  An unmarked item, or a marked item that is not sensitive, is bought at the normal price.
 *
 *  STACKING. Only sensitive types keep marked and unmarked stacks apart (Item.StackableRule via Item.ProvenanceSensitive);
 *  every other merge keeps the mark if either part had it.
 *
 *  ENFORCEMENT. Separation and reduced buyback apply where Enforced (Mortalis, Seasons, Upheaval: the r1187 scope).
 *  On other rulesets the table is built and reported, and everything stacks and sells as before r1187.
 *
 *  FAILURES. A build that throws (graph or report) is never installed. The last good table stays in use; before the first
 *  good build the r1187 rules apply (marked and unmarked stacks apart, marked goods refused): never fail open. A failure is
 *  logged (console, exception log, Logs\ArbitrageTable.log) and retried after 1, 5, then every 15 minutes.
 *
 *  The table is built when the server starts, rebuilt when the pricing database, the Factions setting, the threshold or
 *  the ruleset change (checked on use; the rebuild runs on the next tick), and on [ArbitrageTable rebuild. With Factions
 *  enabled, purchase prices are taken at the lowest faction tax (-30%). Lookups are dictionary reads.
 *  Report: Logs\ArbitrageTable.log.
 *
 *  COVERAGE. Conversions outside the craft systems are declared in BuildConversions, each with its source file. A
 *  conversion that drops the mark is registered with Propagates = false; if it ever lies on a prohibited route the
 *  report lists it as a laundering error. A conversion that is not registered is invisible to the graph.
 */

using Server.Commands;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Server.Engines.Economy
{
    /// <summary>
    /// Passes the StoreBought mark from a converted input to what it becomes.
    /// </summary>
    public static class Provenance
    {
        public static void Inherit(Item? source, Item? output)
        {
            if (source != null && output != null && source.StoreBought)
                output.StoreBought = true;
        }

        public static void Inherit(bool marked, Item? output)
        {
            if (marked && output != null)
                output.StoreBought = true;
        }
    }

    public static class ArbitrageTable
    {
        public static double SkillThreshold = 30.0;
        public const int FactionMinTaxPercent = -30;     // Factions FinanceGump: lowest town tax

        // the last table that built successfully; null until one has (lookups then apply the r1187 rules: fail closed)
        private static Snapshot? m_Current;
        private static bool m_RebuildQueued;

        // failed builds: never installed; retried after 1, 5, then every 15 minutes (no rapid loop)
        private static readonly TimeSpan[] RetryDelays = { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15) };
        private static int m_ConsecutiveFailures;
        private static DateTime m_NextRetry = DateTime.MinValue;
        private static string? m_LastFailure;
        private static Timer? m_RetryTimer;
        private static bool m_FailNextBuild;        // test harness only (set by reflection)

        public static Snapshot? Current { get { return m_Current; } }
        public static int ConsecutiveFailures { get { return m_ConsecutiveFailures; } }
        public static DateTime NextRetry { get { return m_NextRetry; } }
        public static string? LastFailure { get { return m_LastFailure; } }

        public static void Initialize()
        {
            CommandSystem.Register("ArbitrageTable", AccessLevel.Administrator, new CommandEventHandler(ArbitrageTable_OnCommand));
            EventSink.ServerStarted += () => Rebuild("server start");
            Item.ProvenanceSensitive = IsSensitive;
        }

        #region Lookups (cheap)
        /// <summary>
        /// Where the protection applies (stack separation and reduced buyback): the shards whose NPCs buy back goods for
        /// cash, as r1187. Elsewhere the table is still built and reported, and every item stacks and sells normally.
        /// </summary>
        public static bool Enforced { get { return Core.RuleSets.MortalisStyleRules(); } }

        // before the first build every type keeps its provenance apart (the r1187 rule)
        public static bool IsSensitive(Type type)
        {
            if (!Enforced)
                return false;
            Snapshot? s = Get();
            return s == null || s.Sensitive.Contains(type);
        }

        private static Item Goods(Item item)
        {
            return (item is CommodityDeed cd && cd.Commodity != null) ? cd.Commodity : item;
        }

        /// <summary>
        /// The buyback cap for this item, or -1 when it is bought at the normal price.
        /// </summary>
        public static int SafePrice(Item item)
        {
            Item goods = Goods(item);
            if (!goods.StoreBought || !Enforced)
                return -1;

            Snapshot? s = Get();
            if (s == null)
                return 0;   // not built yet: refuse marked goods, as r1187 did

            if (!s.SafePrices.TryGetValue(goods.GetType(), out int safe))
                return -1;

            if (goods != item)  // a commodity deed of marked, exposed goods
                return 0;

            return safe;
        }

        public static int CapBuyback(Item item, int price)
        {
            int safe = SafePrice(item);
            return (safe >= 0 && safe < price) ? safe : price;
        }

        public static bool RefusesBuyback(Item item)
        {
            return SafePrice(item) == 0;
        }

        private static Snapshot? Get()
        {
            Snapshot? s = m_Current;
            // a stale table is rebuilt on the next tick; after a failure, not before the retry time
            if (s != null && !m_RebuildQueued && m_RetryTimer == null && s.Key != CurrentKey() && DateTime.UtcNow >= m_NextRetry)
            {
                m_RebuildQueued = true;
                Timer.DelayCall(TimeSpan.Zero, () => Rebuild("configuration changed"));
            }
            return s;
        }

        public static bool IsStale { get { return m_Current == null || m_Current.Key != CurrentKey(); } }

        private static string CurrentKey()
        {
            return string.Format("{0}|{1}|{2}|{3}", BaseVendor.PricingVersion, Core.Factions, SkillThreshold, RulesetName());
        }

        public static string RulesetName()
        {
            if (Core.RuleSets.AngelIslandRules()) return "Angel Island";
            if (Core.RuleSets.SiegeRules()) return "Siege";
            if (Core.RuleSets.MortalisRules()) return "Mortalis";
            if (Core.RuleSets.SeasonsRules()) return "Seasons";
            if (Core.RuleSets.UpheavalRules()) return "Upheaval";
            return "Other";
        }
        #endregion Lookups

        #region Build and install
        /// <summary>
        /// Builds the table for this server. A successful build is installed. A failed one never is: the last good table
        /// stays in use (or, if there has never been one, the r1187 rules: marked and unmarked stacks apart, marked goods
        /// refused), the failure is logged, and a retry is scheduled after 1, 5, then every 15 minutes. Never throws.
        /// </summary>
        public static Snapshot Rebuild(string reason)
        {
            m_RebuildQueued = false;
            Snapshot s;
            try
            {
                s = Build(new Options { Threshold = SkillThreshold, FactionsWorstCase = Core.Factions, LiveVendors = true });
            }
            catch (Exception ex)
            {   // Build catches its own failures; this is the last line of defense
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                s = new Snapshot { Key = "", Ruleset = RulesetName(), Threshold = SkillThreshold, Built = DateTime.Now, Failed = true };
                s.Errors.Add("build failed: " + ex);
                s.Report = "Arbitrage table build failed:\r\n" + ex + "\r\n";
            }

            try
            {
                if (!s.Failed)
                {
                    m_Current = s;
                    m_ConsecutiveFailures = 0;
                    m_NextRetry = DateTime.MinValue;
                    m_LastFailure = null;
                    m_RetryTimer?.Stop();
                    m_RetryTimer = null;
                    WriteLog(s.Report);
                    Utility.Monitor.WriteLine("Arbitrage table ({0}): {1} sensitive, {2} with a reduced buyback, {3} error(s) in {4:F0} ms",
                        s.Errors.Count > 0 ? ConsoleColor.Red : ConsoleColor.Yellow, reason, s.Sensitive.Count, s.SafePrices.Count, s.Errors.Count, s.BuildMs);
                }
                else
                {
                    TimeSpan delay = RetryDelays[Math.Min(m_ConsecutiveFailures, RetryDelays.Length - 1)];
                    m_ConsecutiveFailures++;
                    m_NextRetry = DateTime.UtcNow + delay;
                    m_LastFailure = string.Format("{0:yyyy-MM-dd HH:mm:ss} ({1}): {2}", DateTime.Now, reason, s.Errors.FirstOrDefault() ?? "unknown");
                    m_RetryTimer?.Stop();
                    m_RetryTimer = Timer.DelayCall(delay, () => Rebuild("retry after failure"));
                    string kept = m_Current != null ? "keeping the table built " + m_Current.Built.ToString("yyyy-MM-dd HH:mm:ss")
                                                     : "no table yet: marked goods kept apart and refused (r1187 rules)";
                    WriteLog(string.Format("Arbitrage table build FAILED {0}\r\n{1}; failure {2}, next retry in {3} minute(s)\r\n\r\n{4}",
                        m_LastFailure, kept, m_ConsecutiveFailures, delay.TotalMinutes, s.Report));
                    // a harness-injected failure is an expected diagnostic (yellow); every other failure is red
                    Utility.Monitor.WriteLine("Arbitrage table ({0}) {1}: {2}; {3}; retry in {4} minute(s). See Logs\\ArbitrageTable.log",
                        s.Injected ? ConsoleColor.Yellow : ConsoleColor.Red, reason, s.Injected ? "test-injected failure (expected)" : "FAILED",
                        FirstLine(s.Errors.FirstOrDefault()), kept, delay.TotalMinutes);
                }
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
            return s;
        }

        private static void WriteLog(string text)
        {
            try
            {
                File.WriteAllText(Path.Combine(Core.LogsDirectory, "ArbitrageTable.log"), text);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        private static string FirstLine(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return "unknown error";
            int nl = text.IndexOfAny(new[] { '\r', '\n' });
            return nl < 0 ? text : text.Substring(0, nl);
        }

        public sealed class Options
        {
            public double Threshold = 30.0;
            public bool FactionsWorstCase;          // purchase prices at the lowest faction tax
            public bool LiveVendors;                // include the live vendors' actual (taxed) prices
        }

        public sealed class Node
        {
            public Type Type = null!;
            public int Uses = 1;                    // units (uses) per item
            public double BuyUnit = double.PositiveInfinity;
            public int BuyPrice;
            public string Seller = "";
            public int Sell;                        // normal NPC buyback per item
            public int SellExceptional;             // exceptional (armor, weapons), else = Sell
            public string Buyer = "";
            public bool Free;                       // water
            // results. An item is either plain (bought, or made outside the craft systems) or crafted (PlayerCrafted):
            //  scissoring and recycling give 1 unit for a plain item and half the recipe for a crafted one.
            public double CostT = double.PositiveInfinity, ValueT, CostBest = double.PositiveInfinity;
            public double CostPlainT = double.PositiveInfinity, CostPlainBest = double.PositiveInfinity;
            public double CostCraftedT = double.PositiveInfinity, CostCraftedBest = double.PositiveInfinity, ValueCraftedT;
            public Edge? CostVia, ValueVia, BestVia;
            public double SellUnit { get { return (double)Sell / Uses; } }
        }

        public sealed class Input
        {
            public Type[] Alternatives = Array.Empty<Type>();
            public double Amount;                   // units consumed per attempt
            public double Loss;                     // units lost by a failed attempt
        }

        /// <summary>Which instances of the input a conversion applies to (scissoring and recycling differ).</summary>
        public enum Origin { Any, Plain, Crafted }

        public sealed class Edge
        {
            public Origin InputOrigin = Origin.Any;
            public string Via = "";                 // craft system or conversion
            public string Source = "";              // source file (registered conversions)
            public List<Input> Inputs = new();
            public List<(Type type, double yield)> Outputs = new();
            public bool Propagates = true;          // the output inherits the StoreBought mark
            public double MinSkill, MaxSkill, ChanceAtMin;   // success: 0 below MinSkill, linear to 1 at MaxSkill
            public double GateSkill = double.NegativeInfinity;   // cannot be attempted below this
            public bool Crafted;                    // CraftSystem recipe
            public CraftSystem? System;
            public bool CanExceptional;

            public double Chance(double skill)
            {
                if (skill < GateSkill || skill < MinSkill)
                    return 0.0;
                if (MaxSkill <= MinSkill || skill >= MaxSkill)
                    return 1.0;
                return Math.Clamp(ChanceAtMin + (skill - MinSkill) / (MaxSkill - MinSkill) * (1.0 - ChanceAtMin), 0.0, 1.0);
            }

            public double ExceptionalChance(double skill, double chance)
            {
                if (!CanExceptional || System == null)
                    return 0.0;
                double exc;
                switch (System.ECA)
                {
                    default:
                    case CraftECA.ChanceMinusSixty: exc = chance - 0.6; break;
                    case CraftECA.FiftyPercentChanceMinusTenPercent: exc = chance * 0.5 - 0.1; break;
                    case CraftECA.ChanceMinusSixtyToFourtyFive:
                        exc = chance - Math.Clamp(0.60 - (skill - 95.0) * 0.03, 0.45, 0.60);
                        break;
                }
                return Math.Clamp(exc, 0.0, chance);
            }

            public override string ToString()
            {
                string ins = string.Join(" + ", Inputs.Select(i => string.Format("{0}x {1}", Fmt(i.Amount), string.Join("|", i.Alternatives.Select(a => a.Name)))));
                string outs = string.Join(" + ", Outputs.Select(o => string.Format("{0}x {1}", Fmt(o.yield), o.type.Name)));
                return string.Format("{0}({1}{2} -> {3}{4})", Via, InputOrigin == Origin.Any ? "" : InputOrigin == Origin.Plain ? "plain " : "crafted ",
                    ins, outs, MinSkill > 0 || GateSkill > 0 ? string.Format(", skill {0}", Fmt(Math.Max(MinSkill, GateSkill))) : "");
            }
        }

        public sealed class Snapshot
        {
            public string Key = "";
            public string Ruleset = "";
            public double Threshold;
            public bool FactionsWorstCase;
            public DateTime Built;
            public double BuildMs;
            public HashSet<Type> Sensitive = new();
            public Dictionary<Type, int> SafePrices = new();      // marked goods of these types are bought for at most this (0: refused)
            public Dictionary<Type, Node> Nodes = new();
            public List<Edge> Edges = new();
            public List<string> Errors = new();                   // laundering edges, profitable cycles
            public List<string> Notes = new();
            public string Report = "";
            public bool Failed;                                   // the build or its report threw: never installed
            public bool Injected;                                 // the failure was the test harness's m_FailNextBuild (expected)

            public bool IsSensitive(Type t) { return Sensitive.Contains(t); }
        }

        public static Snapshot Build(Options opt)
        {
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            Snapshot snap = new Snapshot
            {
                Key = string.Format("{0}|{1}|{2}|{3}", BaseVendor.PricingVersion, opt.FactionsWorstCase, opt.Threshold, RulesetName()),
                Ruleset = RulesetName(),
                Threshold = opt.Threshold,
                FactionsWorstCase = opt.FactionsWorstCase,
                Built = DateTime.Now,
            };

            List<Item> probes = new();
            bool injected = false;      // true only when the failure below is the harness's deliberate one
            try
            {
                if (m_FailNextBuild)
                {   // set only by the test harness, to exercise the failure path
                    m_FailNextBuild = false;
                    injected = true;
                    throw new InvalidOperationException("forced build failure (test)");
                }

                Dictionary<Type, Node> nodes = snap.Nodes;
                Node N(Type t)
                {
                    if (!nodes.TryGetValue(t, out Node? n))
                        nodes[t] = n = new Node { Type = t };
                    return n;
                }

                // probes: a few properties are only available on an instance
                Dictionary<Type, Item?> probeCache = new();
                Item? Probe(Type t)
                {
                    if (probeCache.TryGetValue(t, out Item? p))
                        return p;
                    p = null;
                    if (!t.IsAbstract && typeof(Item).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null)
                    {
                        try
                        {
                            p = (Item?)Activator.CreateInstance(t);
                            if (p != null)
                                probes.Add(p);
                        }
                        catch { p = null; }
                    }
                    probeCache[t] = p;
                    return p;
                }
                int Uses(Type t)
                {
                    if (!typeof(IHasQuantity).IsAssignableFrom(t) || t.IsAbstract)
                        return 1;
                    Item? p = Probe(t);
                    if (p is BaseBeverage bev)
                        return Math.Max(1, bev.MaxQuantity);
                    return p is IHasQuantity hq ? Math.Max(1, hq.Quantity) : 1;
                }

                // ---------------------------------------------------------- trade tables
                void Offer(Type t, int price, string who)
                {
                    if (t == null || price <= 0)
                        return;
                    if (opt.FactionsWorstCase)
                        price = Math.Max(1, (price * (100 + FactionMinTaxPercent) + 50) / 100);
                    Node n = N(t);
                    n.Uses = Uses(t);
                    double unit = (double)price / n.Uses;
                    if (unit < n.BuyUnit)
                    {
                        n.BuyUnit = unit;
                        n.BuyPrice = price;
                        n.Seller = who;
                    }
                }
                void Buyback(Type t, int price, string who, GenericSellInfo gsi)
                {
                    if (t == null || price <= 0)
                        return;
                    int normal = price, exceptional = price;
                    if (typeof(BaseArmor).IsAssignableFrom(t) || typeof(BaseWeapon).IsAssignableFrom(t) || typeof(BaseBeverage).IsAssignableFrom(t))
                    {
                        Item? p = Probe(t);
                        if (p != null)
                        {
                            normal = gsi.GetNominalSellPriceFor(p);
                            if (p is BaseBeverage)
                                normal = Math.Max(normal, price);     // full and empty containers pay differently: the higher
                            exceptional = normal;
                            if (p is BaseArmor a) { ArmorQuality q = a.Quality; a.Quality = ArmorQuality.Exceptional; exceptional = gsi.GetNominalSellPriceFor(p); a.Quality = q; }
                            else if (p is BaseWeapon w) { WeaponQuality q = w.Quality; w.Quality = WeaponQuality.Exceptional; exceptional = gsi.GetNominalSellPriceFor(p); w.Quality = q; }
                        }
                    }
                    Node n = N(t);
                    n.Uses = Uses(t);
                    if (Math.Max(normal, exceptional) > Math.Max(n.Sell, n.SellExceptional))
                    {
                        n.Sell = normal;
                        n.SellExceptional = Math.Max(normal, exceptional);
                        n.Buyer = who;
                    }
                }

                foreach (Type sbt in typeof(SBInfo).Assembly.GetTypes().Where(t => typeof(SBInfo).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null))
                {
                    SBInfo sb;
                    try { sb = (SBInfo)Activator.CreateInstance(sbt)!; }
                    catch { continue; }
                    foreach (object e in sb.BuyInfo)
                        if (e is IBuyItemInfo bi)
                            Offer(bi.Type, bi.Price, sbt.Name);
                    if (sb.SellInfo is GenericSellInfo gsi)
                        foreach (DictionaryEntry de in gsi.Table)
                            if (de.Key is Type t && de.Value is int price)
                                Buyback(t, price, sbt.Name, gsi);
                }
                if (opt.LiveVendors)
                    foreach (Mobile m in World.Mobiles.Values.ToList())
                        if (m is BaseVendor v && !v.Deleted)
                        {
                            IBuyItemInfo[] buy;
                            try { buy = v.GetBuyInfo(); } catch { continue; }
                            foreach (IBuyItemInfo bi in buy)
                                Offer(bi.Type, bi.Price, v.GetType().Name + " (live)");
                        }

                // ---------------------------------------------------------- conversions
                List<Edge> edges = snap.Edges;
                foreach (Type t in typeof(CraftSystem).Assembly.GetTypes().Where(t => typeof(CraftSystem).IsAssignableFrom(t) && !t.IsAbstract))
                {   // the craft systems are created on first use
                    try { t.GetProperty("CraftSystem", BindingFlags.Public | BindingFlags.Static)?.GetValue(null); } catch { }
                }
                foreach (CraftSystem cs in CraftSystem.Instances.ToList())
                    for (int i = 0; i < cs.CraftItems.Count; i++)
                    {
                        CraftItem ci = cs.CraftItems.GetAt(i);
                        Edge? e = CraftEdge(cs, ci, Uses);
                        if (e != null)
                            edges.Add(e);
                    }
                // types in the NPC economy: sold by an NPC, or made by a recipe
                HashSet<Type> economy = new(nodes.Values.Where(n => !double.IsPositiveInfinity(n.BuyUnit)).Select(n => n.Type));
                foreach (Edge e in edges)
                    foreach (var o in e.Outputs)
                        economy.Add(o.type);
                edges.AddRange(BuildConversions(Probe, Uses, economy, snap.Notes));

                foreach (Edge e in edges)
                {
                    foreach (Input i in e.Inputs)
                        foreach (Type a in i.Alternatives)
                            N(a).Uses = Uses(a);
                    foreach (var o in e.Outputs)
                        N(o.type).Uses = Uses(o.type);
                }
                foreach (Node n in nodes.Values)
                    if (typeof(BaseBeverage).IsAssignableFrom(n.Type) && n.Type.IsAbstract)
                    {
                        n.Free = true;
                        n.BuyUnit = 0;
                    }

                Solve(snap, opt.Threshold);
            }
            catch (Exception ex)
            {
                snap.Failed = true;
                snap.Injected = injected;
                snap.Errors.Add("build failed: " + ex);
                if (!injected)          // an expected, harness-injected failure is not an exception to log
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
            finally
            {
                foreach (Item p in probes)
                    try { if (!p.Deleted) p.Delete(); } catch { }
            }

            snap.BuildMs = tc.Elapsed() * 1000.0;
            tc.End();
            try
            {
                snap.Report = WriteReport(snap);
            }
            catch (Exception ex)
            {
                snap.Failed = true;
                snap.Errors.Add("report failed: " + ex);
                snap.Report = "Arbitrage table report failed:\r\n" + string.Join("\r\n", snap.Errors) + "\r\n";
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
            return snap;
        }

        private static Edge? CraftEdge(CraftSystem cs, CraftItem ci, Func<Type, int> uses)
        {
            if (ci.ItemType == null || ci.Resources.Count == 0)
                return null;

            Edge e = new Edge { Via = cs.GetType().Name, Crafted = true, System = cs };
            e.Outputs.Add((ci.ItemType, 1.0));
            e.CanExceptional = !ci.ForceNonExceptional && (typeof(BaseArmor).IsAssignableFrom(ci.ItemType) || typeof(BaseWeapon).IsAssignableFrom(ci.ItemType));

            for (int r = 0; r < ci.Resources.Count; r++)
            {
                CraftRes res = ci.Resources.GetAt(r);
                if (res.ItemType == null)
                    return null;
                Type[] alts = CraftItem.GetEquivalentResources(res.ItemType) ?? new[] { res.ItemType };
                if (alts.Length == 0)
                    alts = new[] { res.ItemType };
                double amount = res.Amount;
                // a failed attempt: half of each resource (at least 1); use-all recipes lose a half, third or quarter
                double loss = ci.UseAllRes ? Math.Max(1.0, amount * (1 / 2.0 + 1 / 3.0 + 1 / 4.0) / 3) : Math.Max(1, res.Amount / 2);
                e.Inputs.Add(new Input { Alternatives = alts, Amount = amount, Loss = loss });
            }

            double gate = double.NegativeInfinity;
            for (int s = 0; s < ci.Skills.Count; s++)
            {
                CraftSkill sk = ci.Skills.GetAt(s);
                gate = Math.Max(gate, sk.MinSkill);
                if (sk.SkillToMake == cs.MainSkill)
                {
                    e.MinSkill = sk.MinSkill;
                    e.MaxSkill = sk.MaxSkill;
                }
            }
            e.GateSkill = gate;
            e.ChanceAtMin = cs.GetChanceAtMin(ci);
            return e;
        }

        /// <summary>
        /// Conversions outside the craft systems. Each entry names the code that performs it.
        /// </summary>
        /// <summary>
        /// What cooking this raw food over heat makes. False when Cook() makes nothing (a HatchableEgg) or fails; the
        /// cooked item is deleted.
        /// </summary>
        public static bool TryCookedType(CookableFood raw, out Type? cooked)
        {
            cooked = null;
            Food? food = null;
            try
            {
                food = raw.Cook();
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
            if (food == null)
                return false;
            cooked = food.GetType();
            if (!food.Deleted)
                food.Delete();
            return true;
        }

        /// <summary>
        /// What smelting this ore makes. False when GetIngot() makes nothing or fails; the ingot is deleted.
        /// </summary>
        public static bool TryIngotType(BaseOre ore, out Type? ingot)
        {
            ingot = null;
            BaseIngot? made = null;
            try
            {
                made = ore.GetIngot();
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
            if (made == null)
                return false;
            ingot = made.GetType();
            if (!made.Deleted)
                made.Delete();
            return true;
        }

        private static List<Edge> BuildConversions(Func<Type, Item?> probe, Func<Type, int> uses, HashSet<Type> economy, List<string> notes)
        {
            List<Edge> list = new();
            Edge Add(string via, string source, Type[] inputs, double[] amounts, (Type, double)[] outputs, bool propagates = true,
                double gate = double.NegativeInfinity, double min = 0, double max = 0, double chanceAtMin = 0, double lossFraction = 0)
            {
                Edge e = new Edge { Via = via, Source = source, Propagates = propagates, GateSkill = gate, MinSkill = min, MaxSkill = max, ChanceAtMin = chanceAtMin };
                for (int i = 0; i < inputs.Length; i++)
                    e.Inputs.Add(new Input { Alternatives = new[] { inputs[i] }, Amount = amounts[i], Loss = amounts[i] * lossFraction });
                e.Outputs.AddRange(outputs);
                list.Add(e);
                return e;
            }
            IEnumerable<Type> Concrete(Type baseType)
            {
                return baseType.Assembly.GetTypes().Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);
            }

            // scissors (Item.ScissorHelper passes the mark)
            Add("scissors", "Items/Resources/Tailor/Hides.cs", new[] { typeof(Hides) }, new[] { 1.0 }, new[] { (typeof(Leather), 1.0) });
            Add("scissors", "Items/Resources/Tailor/Hides.cs", new[] { typeof(SpinedHides) }, new[] { 1.0 }, new[] { (typeof(SpinedLeather), 1.0) });
            Add("scissors", "Items/Resources/Tailor/Hides.cs", new[] { typeof(HornedHides) }, new[] { 1.0 }, new[] { (typeof(HornedLeather), 1.0) });
            Add("scissors", "Items/Resources/Tailor/Hides.cs", new[] { typeof(BarbedHides) }, new[] { 1.0 }, new[] { (typeof(BarbedLeather), 1.0) });
            Add("scissors", "Items/Resources/Tailor/BoltOfCloth.cs", new[] { typeof(BoltOfCloth) }, new[] { 1.0 }, new[] { (typeof(Cloth), 50.0) });
            Add("scissors", "Items/Resources/Tailor/Cloth.cs", new[] { typeof(Cloth) }, new[] { 1.0 }, new[] { (typeof(Bandage), 1.0) });
            Add("scissors", "Items/Resources/Tailor/UncutCloth.cs", new[] { typeof(UncutCloth) }, new[] { 1.0 }, new[] { (typeof(Bandage), 1.0) });
            Add("scissors", "Items/misc/OilCloth.cs", new[] { typeof(OilCloth) }, new[] { 1.0 }, new[] { (typeof(Bandage), 1.0) });
            Add("scissors", "Items/Special/Holiday/Halloween/BurialGown.cs", new[] { typeof(BurialGown) }, new[] { 1.0 }, new[] { (typeof(Cloth), 1.0) });
            Add("scissors", "Items/Resources/Fishing/Fish.cs", new[] { typeof(Fish) }, new[] { 1.0 }, new[] { (typeof(RawFishSteak), 4.0) });
            Add("scissors", "Items/Resources/Fishing/BigFish.cs", new[] { typeof(BigFish) }, new[] { 1.0 }, new[] { (typeof(RawFishSteak), 8.0) });
            Add("scissors", "Items/Body Parts/BoneShards.cs", new[] { typeof(BoneShards) }, new[] { 1.0 }, new[] { (typeof(Bone), 3.0) });
            Add("scissors", "Items/Body Parts/PelvisBone.cs", new[] { typeof(PelvisBone) }, new[] { 1.0 }, new[] { (typeof(Bone), 1.0) });
            Add("scissors", "Items/Body Parts/SpineBone.cs", new[] { typeof(SpineBone) }, new[] { 1.0 }, new[] { (typeof(Bone), 3.0) });
            // axe on logs (BaseLog.TryCreateBoards -> ScissorHelper); special woods need Carpentry or Lumberjacking
            foreach ((Type log, Type board, double skill) in new[] {
                (typeof(Log), typeof(Board), 0.0), (typeof(OakLog), typeof(OakBoard), 65.0), (typeof(AshLog), typeof(AshBoard), 75.0),
                (typeof(YewLog), typeof(YewBoard), 80.0), (typeof(HeartwoodLog), typeof(HeartwoodBoard), 90.0),
                (typeof(BloodwoodLog), typeof(BloodwoodBoard), 95.0), (typeof(FrostwoodLog), typeof(FrostwoodBoard), 99.0) })
                Add("axe", "Items/Skill Items/Lumberjack/Log.cs", new[] { log }, new[] { 1.0 }, new[] { (board, 1.0) }, gate: skill);
            // Discovery below needs an instance. Only types an NPC sells, or that a recipe or conversion makes, are probed:
            //  anything else cannot be on an NPC-funded route, and constructing arbitrary items has side effects (a
            //  HatchableEgg joins the hatching list; its Cook() returns null).
            bool InEconomy(Type t) => economy.Contains(t) || list.Any(e => e.Outputs.Any(o => o.type == t));
            List<string> skipped = new();

            // scissoring armor and clothing back into their material, as BaseArmor.Scissor / BaseClothing.Scissor: only items with a
            //  Tailoring recipe of one resource (at least 2); a plain item gives 1 unit, a crafted one half the recipe
            CraftSystem tailoring = DefTailoring.CraftSystem;
            foreach (Type t in Concrete(typeof(BaseArmor)).Concat(Concrete(typeof(BaseClothing))))
            {
                if (!typeof(IScissorable).IsAssignableFrom(t) || !InEconomy(t))
                    continue;
                CraftItem? recipe = tailoring.CraftItems.SearchFor(t);
                if (recipe == null || recipe.Resources.Count != 1 || recipe.Resources.GetAt(0).Amount < 2)
                    continue;
                Item? p = probe(t);
                Type? material = null;
                if (p is BaseArmor a)
                {
                    CraftResourceInfo? info = CraftResources.GetInfo(a.Resource);
                    material = info != null && info.ResourceTypes.Length > 0 ? info.ResourceTypes[0] : null;
                }
                else if (p is BaseClothing c && c.Scissorable)
                {
                    if (p is BaseShoes shoes)
                    {
                        CraftResourceInfo? info = CraftResources.GetInfo(shoes.Resource);
                        material = info != null && info.ResourceTypes.Length > 0 ? info.ResourceTypes[0] : null;
                    }
                    material ??= recipe.Resources.GetAt(0).ItemType;
                }
                if (material == null)
                    continue;
                string source = p is BaseArmor ? "Items/Armor/BaseArmor.cs" : "Items/Clothing/BaseClothing.cs";
                int amount = recipe.Resources.GetAt(0).Amount;
                Add("scissors", source, new[] { t }, new[] { 1.0 }, new[] { (material, 1.0) }).InputOrigin = Origin.Plain;
                Add("scissors", source, new[] { t }, new[] { 1.0 }, new[] { (material, (double)(amount / 2)) }).InputOrigin = Origin.Crafted;
            }
            // spinning wheel
            Add("spinning wheel", "Items/Resources/Tailor/Wool.cs", new[] { typeof(Wool) }, new[] { 1.0 }, new[] { (typeof(DarkYarn), 3.0) });
            Add("spinning wheel", "Items/Resources/Tailor/Cotton.cs", new[] { typeof(Cotton) }, new[] { 1.0 }, new[] { (typeof(SpoolOfThread), 6.0) });
            Add("spinning wheel", "Items/Resources/Tailor/Flax.cs", new[] { typeof(Flax) }, new[] { 1.0 }, new[] { (typeof(SpoolOfThread), 6.0) });
            // loom: five yarns or threads -> one bolt
            foreach (Type t in Concrete(typeof(BaseClothMaterial)))
                Add("loom", "Items/Resources/Tailor/YarnsAndThreads.cs", new[] { t }, new[] { 5.0 }, new[] { (typeof(BoltOfCloth), 1.0) });
            // smelting: one ore -> two ingots; Mining, difficulty by metal (Ore.cs); a failure halves the pile
            foreach (Type t in Concrete(typeof(BaseOre)))
            {
                if (!InEconomy(t) || probe(t) is not BaseOre ore)
                    continue;
                if (!TryIngotType(ore, out Type? ingot) || ingot == null)
                {
                    skipped.Add(t.Name + " (no ingot)");
                    continue;
                }
                double d = OreDifficulty(ore.Resource);
                Add("smelting", "Items/Resources/Blacksmithing/Ore.cs", new[] { t }, new[] { 1.0 }, new[] { (ingot, 2.0) },
                    gate: d > 50.0 ? d : double.NegativeInfinity, min: d - 25.0, max: d + 25.0, lossFraction: 0.5);
            }
            // cooking over a fire (CookableFood): Cooking from the food's level to 100; a failure burns about half
            foreach (Type t in Concrete(typeof(CookableFood)))
            {
                if (!InEconomy(t) || probe(t) is not CookableFood raw)
                    continue;
                if (!TryCookedType(raw, out Type? cooked) || cooked == null)
                {
                    skipped.Add(t.Name + " (Cook() makes nothing)");
                    continue;
                }
                Add("cooking (heat)", "Items/Food/CookableFood.cs", new[] { t }, new[] { 1.0 }, new[] { (cooked, 1.0) },
                    min: raw.CookingLevel, max: raw.MaxCookingLevel, lossFraction: 0.5);
            }
            if (skipped.Count > 0)
                notes.Add("Discovery skipped (no output): " + string.Join(", ", skipped) + ".");
            notes.Add("Cooking over heat, smelting and scissoring armor/clothing are discovered only for types an NPC sells or a recipe or conversion makes.");
            // flour, dough and mixes outside the cooking craft system (Cooking.cs), water free
            Add("pour water", "Items/Food/Cooking.cs", new[] { typeof(SackFlour) }, new[] { 1.0 }, new[] { (typeof(Dough), 1.0) });
            Add("pour water", "Items/Food/Cooking.cs", new[] { typeof(BowlFlour) }, new[] { 1.0 }, new[] { (typeof(Dough), 1.0), (typeof(EmptyWoodenBowl), 1.0) });
            if (CookableFood.OldCooking)
            {
                Add("flour into a bowl", "Items/Food/Cooking.cs", new[] { typeof(SackFlour), typeof(EmptyWoodenBowl) }, new[] { 1.0, 1.0 }, new[] { (typeof(BowlFlour), 1.0) });
                Add("honey", "Items/Food/Cooking.cs", new[] { typeof(JarHoney), typeof(Dough) }, new[] { 1.0, 1.0 }, new[] { (typeof(SweetDough), 1.0) });
                Add("honey", "Items/Food/Cooking.cs", new[] { typeof(JarHoney), typeof(BowlFlour) }, new[] { 1.0, 1.0 }, new[] { (typeof(CookieMix), 1.0) });
                Add("tribal paint", "Items/Food/Cooking.cs", new[] { typeof(SackFlour), typeof(TribalBerry) }, new[] { 1.0, 1.0 }, new[] { (typeof(TribalPaint), 1.0) }, gate: 80.0);
            }
            // flour mill: two wheat sheaves -> one sack (Cooking.cs WheatSheaf, FlourMill*Addon)
            Add("flour mill", "Items/Addons/FlourMillEastAddon.cs", new[] { typeof(WheatSheaf) }, new[] { 2.0 }, new[] { (typeof(SackFlour), (double)uses(typeof(SackFlour))) });
            // recycling crafted metal and wood back into their resource (Recycle.ProcessRecycle; shop items give 1)
            foreach (CraftSystem cs in CraftSystem.Instances.ToList())
                for (int i = 0; i < cs.CraftItems.Count; i++)
                {
                    CraftItem ci = cs.CraftItems.GetAt(i);
                    if (ci.ItemType == null || ci.Resources.Count == 0)
                        continue;
                    CraftRes r0 = ci.Resources.GetAt(0);
                    if (r0.ItemType == null || r0.Amount < 2)
                        continue;
                    bool metal = typeof(BaseIngot).IsAssignableFrom(r0.ItemType), wood = typeof(BaseBoard).IsAssignableFrom(r0.ItemType) || typeof(BaseLog).IsAssignableFrom(r0.ItemType);
                    if (!metal && !wood)
                        continue;
                    if (!(typeof(BaseArmor).IsAssignableFrom(ci.ItemType) || typeof(BaseWeapon).IsAssignableFrom(ci.ItemType) || typeof(BaseInstrument).IsAssignableFrom(ci.ItemType)
                        || typeof(BaseCraftableItem).IsAssignableFrom(ci.ItemType) || typeof(BaseOtherEquipable).IsAssignableFrom(ci.ItemType)))
                        continue;
                    // a plain (bought) item gives 1, a crafted one half the first resource (Recycle.ProcessRecycle)
                    Add("recycle", "Engines/Craft/core/Recycle.cs", new[] { ci.ItemType }, new[] { 1.0 }, new[] { (r0.ItemType, 1.0) }).InputOrigin = Origin.Plain;
                    Add("recycle", "Engines/Craft/core/Recycle.cs", new[] { ci.ItemType }, new[] { 1.0 }, new[] { (r0.ItemType, (double)(r0.Amount / 2)) }).InputOrigin = Origin.Crafted;
                }

            // conversions that do NOT pass the mark (containers handed back after use): checked for laundering
            foreach (Type t in Concrete(typeof(BasePotion)))
                Add("empty bottle", "Items/Skill Items/Magical/Potions/BasePotion.cs", new[] { t }, new[] { 1.0 }, new[] { (typeof(Bottle), 1.0) }, propagates: false);
            notes.Add("Food bowls emptied by eating (Bowls.cs) and pitchers emptied by drinking return the same or an empty container; not modeled.");
            return list;
        }

        private static double OreDifficulty(CraftResource r)
        {   // mirrors Ore.cs (smelting)
            switch (r)
            {
                default: return 50.0;
                case CraftResource.DullCopper: return 65.0;
                case CraftResource.ShadowIron: return 70.0;
                case CraftResource.Copper: return 75.0;
                case CraftResource.Bronze: return 80.0;
                case CraftResource.Gold: return 85.0;
                case CraftResource.Agapite: return 90.0;
                case CraftResource.Verite: return 95.0;
                case CraftResource.Valorite: return 99.0;
            }
        }
        #endregion Build and install

        #region Graph
        private const double Eps = 1e-9;
        private const int MaxPasses = 64;

        // expected units of each input consumed per success, at this chance (best case: chance 1)
        private static double Units(Input i, double chance)
        {
            return chance >= 1.0 ? i.Amount : i.Amount + (1.0 - chance) / chance * i.Loss;
        }

        private static double GroupCost(Snapshot s, Input i, Origin origin, bool best)
        {
            double c = double.PositiveInfinity;
            foreach (Type a in i.Alternatives)
                if (s.Nodes.TryGetValue(a, out Node? n))
                    c = Math.Min(c, origin == Origin.Plain ? (best ? n.CostPlainBest : n.CostPlainT)
                        : origin == Origin.Crafted ? (best ? n.CostCraftedBest : n.CostCraftedT)
                        : (best ? n.CostBest : n.CostT));
            return c;
        }

        // value per success of one output unit: sold (expected quality at this skill) or converted further. What a recipe makes
        //  is a crafted item; what other conversions make is plain.
        private static double OutputValue(Snapshot s, Edge e, Type t, double skill, double chance, bool best)
        {
            Node n = s.Nodes[t];
            double sold = n.SellUnit;
            if (e.CanExceptional && n.SellExceptional > n.Sell)
            {
                if (best)
                    sold = (double)n.SellExceptional / n.Uses;
                else if (chance > 0)
                {
                    double exc = e.ExceptionalChance(skill, chance);
                    sold = (exc * n.SellExceptional + (chance - exc) * n.Sell) / chance / n.Uses;
                }
            }
            return Math.Max(sold, best ? 0 : e.Crafted ? n.ValueCraftedT : n.ValueT);
        }

        private static void Solve(Snapshot s, double threshold)
        {
            foreach (Node n in s.Nodes.Values)
            {
                n.CostPlainT = n.CostPlainBest = n.CostT = n.CostBest = n.Free ? 0 : n.BuyUnit;
                n.CostCraftedT = n.CostCraftedBest = double.PositiveInfinity;
                n.ValueT = n.ValueCraftedT = n.SellUnit;
            }
            List<Edge> usable = s.Edges.Where(e => e.Chance(threshold) > 0).ToList();

            // forward: cheapest NPC-funded cost at T (expected) and in the best case (100%, no losses), plain and crafted
            foreach (bool best in new[] { false, true })
            {
                bool changed = true;
                int pass = 0;
                for (; changed && pass < MaxPasses; pass++)
                {
                    changed = false;
                    foreach (Edge e in usable)
                    {
                        double chance = best ? 1.0 : e.Chance(threshold);
                        double cost = 0;
                        foreach (Input i in e.Inputs)
                            cost += GroupCost(s, i, e.InputOrigin, best) * Units(i, chance);
                        if (double.IsPositiveInfinity(cost))
                            continue;
                        foreach (var o in e.Outputs)
                        {   // other outputs of the same conversion offset the cost at their buyback
                            double others = e.Outputs.Where(x => x.type != o.type).Sum(x => x.yield * s.Nodes[x.type].SellUnit);
                            double unit = Math.Max(0, cost - others) / o.yield;
                            Node n = s.Nodes[o.type];
                            if (best)
                            {
                                if (e.Crafted ? unit < n.CostCraftedBest - Eps : unit < n.CostPlainBest - Eps)
                                {
                                    if (e.Crafted) n.CostCraftedBest = unit; else n.CostPlainBest = unit;
                                    changed = true;
                                }
                                if (unit < n.CostBest - Eps) { n.CostBest = unit; n.BestVia = e; }
                            }
                            else
                            {
                                if (e.Crafted ? unit < n.CostCraftedT - Eps : unit < n.CostPlainT - Eps)
                                {
                                    if (e.Crafted) n.CostCraftedT = unit; else n.CostPlainT = unit;
                                    changed = true;
                                }
                                if (unit < n.CostT - Eps) { n.CostT = unit; n.CostVia = e; }
                            }
                        }
                    }
                }
                if (changed)
                    s.Errors.Add(string.Format("profitable cycle: {0} costs kept falling after {1} passes (a conversion loop makes money)", best ? "best-case" : "expected", MaxPasses));
            }

            // backward: the most an NPC eventually pays per unit, at T: for a plain unit (ValueT) and a crafted one (ValueCraftedT)
            {
                bool changed = true;
                int pass = 0;
                for (; changed && pass < MaxPasses; pass++)
                {
                    changed = false;
                    foreach (Edge e in usable)
                    {
                        double chance = e.Chance(threshold);
                        double value = e.Outputs.Sum(o => o.yield * OutputValue(s, e, o.type, threshold, chance, false));
                        for (int k = 0; k < e.Inputs.Count; k++)
                        {
                            double others = 0;
                            for (int j = 0; j < e.Inputs.Count; j++)
                                if (j != k)
                                    others += GroupCost(s, e.Inputs[j], e.InputOrigin, false) * Units(e.Inputs[j], chance);
                            if (double.IsPositiveInfinity(others))
                                continue;
                            double perUnit = (value - others) / Units(e.Inputs[k], chance);
                            foreach (Type a in e.Inputs[k].Alternatives)
                            {
                                Node n = s.Nodes[a];
                                if (e.InputOrigin != Origin.Crafted && perUnit > n.ValueT + Eps)
                                {
                                    n.ValueT = perUnit;
                                    n.ValueVia = e;
                                    changed = true;
                                }
                                if (e.InputOrigin != Origin.Plain && perUnit > n.ValueCraftedT + Eps)
                                {
                                    n.ValueCraftedT = perUnit;
                                    changed = true;
                                }
                            }
                        }
                    }
                }
                if (changed)
                    s.Errors.Add(string.Format("profitable cycle: values kept rising after {0} passes (a conversion loop makes money)", MaxPasses));
            }

            // classification (plain against plain, crafted against crafted) and safe prices
            foreach (Node n in s.Nodes.Values)
            {
                if (n.Free)
                    continue;
                bool plain = !double.IsPositiveInfinity(n.CostPlainT) && n.ValueT > n.CostPlainT + Eps;
                bool crafted = !double.IsPositiveInfinity(n.CostCraftedT) && n.ValueCraftedT > n.CostCraftedT + Eps;
                if (plain || crafted)
                    s.Sensitive.Add(n.Type);
            }
            foreach (Type t in s.Sensitive)
            {
                Node n = s.Nodes[t];
                int top = Math.Max(n.Sell, n.SellExceptional);
                if (top <= 0)
                    continue;
                double itemCost = Math.Min(n.CostBest, n.CostT) * n.Uses;
                int safe = Math.Max(0, (int)Math.Ceiling(itemCost - Eps) - 1);
                if (safe < top)
                    s.SafePrices[t] = safe;
            }

            // laundering: a conversion that drops the mark between two points of a prohibited route
            foreach (Edge e in usable.Where(e => !e.Propagates))
                if (e.Inputs.Any(i => i.Alternatives.Any(s.Sensitive.Contains)) && e.Outputs.Any(o => s.Sensitive.Contains(o.type) || s.Nodes[o.type].Sell > 0))
                    s.Errors.Add("laundering: " + e + " drops the StoreBought mark on a prohibited route (" + e.Source + ")");
        }
        #endregion Graph

        #region Report
        private static string Fmt(double d)
        {
            if (double.IsPositiveInfinity(d)) return "-";
            return d == Math.Floor(d) ? d.ToString("0") : d.ToString("0.###");
        }

        private static string Route(Snapshot s, Type t, bool best, int depth = 0)
        {
            Node n = s.Nodes[t];
            Edge? e = best ? n.BestVia : n.CostVia;
            if (e == null || depth > 6)
                return n.Free ? "water" : string.Format("buy {0} {1}{2}", t.Name, n.BuyPrice, n.Uses > 1 ? "/" + n.Uses + " uses" : "") + (n.Seller.Length > 0 ? " (" + n.Seller + ")" : "");
            List<string> ins = new();
            foreach (Input i in e.Inputs)
            {
                Type? cheapest = i.Alternatives.Where(s.Nodes.ContainsKey).OrderBy(a => best ? s.Nodes[a].CostBest : s.Nodes[a].CostT).FirstOrDefault();
                if (cheapest != null)
                    ins.Add(string.Format("{0}x [{1}]", Fmt(i.Amount), Route(s, cheapest, best, depth + 1)));
            }
            return string.Format("{0} {1} <- {2}", e.Via, string.Join(" + ", e.Outputs.Select(o => Fmt(o.yield) + "x " + o.type.Name)), string.Join(" + ", ins));
        }

        private static string WriteReport(Snapshot s)
        {
            StringBuilder o = new();
            o.AppendLine(string.Format("Arbitrage table {0:yyyy-MM-dd HH:mm:ss} ({1:F0} ms)", s.Built, s.BuildMs));
            o.AppendLine(string.Format("Ruleset {0}; skill threshold T = {1}; purchase prices {2}", s.Ruleset, s.Threshold,
                s.FactionsWorstCase ? "at the lowest faction tax (" + FactionMinTaxPercent + "%)" : "as listed (Factions off)"));
            o.AppendLine(string.Format("{0} types, {1} conversions ({2} usable at T), {3} sensitive, {4} with a reduced buyback, {5} error(s)",
                s.Nodes.Count, s.Edges.Count, s.Edges.Count(e => e.Chance(s.Threshold) > 0), s.Sensitive.Count, s.SafePrices.Count, s.Errors.Count));
            o.AppendLine();
            o.AppendLine("ERRORS");
            foreach (string e in s.Errors) o.AppendLine("  ! " + e);
            if (s.Errors.Count == 0) o.AppendLine("  (none)");
            o.AppendLine();
            o.AppendLine("SENSITIVE TYPES (on a route profitable for a crafter of skill T; marked and unmarked stacks stay apart)");
            foreach (Type t in s.Sensitive.OrderBy(t => t.Name))
            {
                Node n = s.Nodes[t];
                o.AppendLine(string.Format("  {0}: cost {1} per unit (best case {2}), worth up to {3}; NPC buys at {4}{5}; marked buyback {6}",
                    t.Name, Fmt(n.CostT), Fmt(n.CostBest), Fmt(n.ValueT), n.Sell, n.SellExceptional > n.Sell ? " (exceptional " + n.SellExceptional + ")" : "",
                    s.SafePrices.TryGetValue(t, out int safe) ? (safe == 0 ? "REFUSED" : safe.ToString()) : "normal"));
                o.AppendLine("      cheapest: " + Route(s, t, false));
                if (n.ValueVia != null) o.AppendLine("      worth most as: " + n.ValueVia);
            }
            o.AppendLine();
            o.AppendLine("NOTES");
            foreach (string n in s.Notes) o.AppendLine("  " + n);
            o.AppendLine("  Not modeled: BOD rewards, player trades, magic properties on crafted goods, vendor stock limits.");
            o.AppendLine();
            o.AppendLine("CONVERSIONS OUTSIDE THE CRAFT SYSTEMS");
            foreach (var g in s.Edges.Where(e => !e.Crafted).GroupBy(e => e.Via + " (" + e.Source + ")" + (e.Propagates ? "" : " [drops the mark]")))
                o.AppendLine(string.Format("  {0}: {1}", g.Key, g.Count()));
            return o.ToString();
        }
        #endregion Report

        #region Command
        [Usage("ArbitrageTable [rebuild | threshold <skill>]")]
        [Description("Reports the NPC arbitrage table (sensitive types, reduced buybacks), rebuilds it, or sets the skill threshold.")]
        private static void ArbitrageTable_OnCommand(CommandEventArgs e)
        {
            string verb = e.Arguments.Length > 0 ? e.Arguments[0].ToLowerInvariant() : "";
            if (verb == "threshold" && e.Arguments.Length > 1 && double.TryParse(e.Arguments[1], out double t) && t >= 0 && t <= 120)
            {
                SkillThreshold = t;
                Rebuild("threshold " + t);
            }
            else if (verb == "rebuild" || m_Current == null)
                Rebuild("command");
            else if (verb.Length > 0)
            {
                e.Mobile.SendMessage("Usage: [ArbitrageTable [rebuild | threshold <skill>]");
                return;
            }

            if (m_LastFailure != null)
                e.Mobile.SendMessage(string.Format("Last build FAILED {0}; {1} consecutive failure(s), next retry {2:HH:mm:ss} UTC.",
                    FirstLine(m_LastFailure), m_ConsecutiveFailures, m_NextRetry));
            Snapshot? s = m_Current;
            if (s == null)
            {
                e.Mobile.SendMessage("No arbitrage table yet: marked goods are kept apart and refused (r1187 rules). Logs\\ArbitrageTable.log");
                return;
            }
            e.Mobile.SendMessage(string.Format("Arbitrage table ({0}, T = {1}, built {2:HH:mm:ss}): {3} sensitive, {4} reduced buybacks, {5} error(s). Logs\\ArbitrageTable.log",
                s.Ruleset, s.Threshold, s.Built, s.Sensitive.Count, s.SafePrices.Count, s.Errors.Count));
            foreach (var kv in s.SafePrices.OrderBy(kv => kv.Key.Name))
                e.Mobile.SendMessage(string.Format("  {0}: {1}", kv.Key.Name, kv.Value == 0 ? "refused" : kv.Value + " gp"));
        }
        #endregion Command
    }
}
