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

/* Scripts/Engines/AI/Creature/BaseCreature.Power.cs
 * ChangeLog
 *  12/16/25, Adam
 *      Created.
 */

using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Mobiles
{
    public partial class BaseCreature
    {
        private bool _powerDirty = true;
        private double _power;
        private PowerDetail _powerDetail = new(0, 0, 0, 0, 0, 0);

        [CommandProperty(AccessLevel.GameMaster)]
        public double Power
        {
            get { EnsurePower(); return _power; }
        }

        public PowerDetail PowerBreakdown
        {
            get { EnsurePower(); return _powerDetail; }
        }

        private void EnsurePower()
        {
            if (_powerDirty)
            {
                _power = PowerModel.ComputePower(this, out _powerDetail);
                _powerDirty = false;
            }
        }
        public void InvalidatePower() => _powerDirty = true;

        /* Mark dirty when relevant characteristics change
            base creature OnAfterSpawn()
            base creature Deserialize()
         */
    }

    public static class PowerModel
    {
        // Tunables
        public static double W_HP = 0.45, W_ARM = 0.15, W_MELEE = 0.20, W_MAGIC = 0.18, W_SPEC = 0.02;
        public static double MinPower = 10, MaxPower = 10000;

        public static double ComputePower(BaseCreature bc, out PowerDetail d)
        {
            // Durability & mitigation (prefer template/base metrics)
            double hp = Math.Max(1, bc.HitsMax);           // template cap; OK if your core keeps it intrinsic
            double arm = Math.Max(0, bc.VirtualArmor);      // swap to SumBaseResists(bc) if you use AOS resists

            // Offense (melee proxy): use DamageMin/Max + base skill scaling
            double avgDmg = (bc.DamageMin + bc.DamageMax) * 0.5;
            double tac = GetBaseSkill(bc, SkillName.Tactics);
            double wrest = GetBaseSkill(bc, SkillName.Wrestling);
            double meleeScale = 0.5 + 0.5 * ((tac + wrest) / 200.0); // ~0.5–1.5
            double melee = avgDmg * meleeScale;

            // Offense (magic proxy): base Magery/EvalInt only
            double magery = GetBaseSkill(bc, SkillName.Magery);
            double evalInt = GetBaseSkill(bc, SkillName.EvalInt);
            double magic = 0.10 * (magery + evalInt);

            // Specials
            double specials = 0.0;
            if (bc.HasBreath) specials += 5.0;
            // add more flags as needed (AoE, life drain, poison tiers, etc.)

            double total = W_HP * hp + W_ARM * arm + W_MELEE * melee + W_MAGIC * magic + W_SPEC * specials;
            total = Clamp(total, MinPower, MaxPower);

            d = new PowerDetail(hp, arm, melee, magic, specials, total);
            return total;
        }

        private static double GetBaseSkill(BaseCreature bc, SkillName name)
        {
            var s = bc.Skills?[name];
            return s?.Base ?? 0.0; // BASE, not Value (ignores buffs/debuffs)
        }

        private static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);
    }

    public sealed record PowerDetail(
        double Hp,
        double ArmorOrResists,
        double Melee,
        double Magic,
        double Specials,
        double Total
    );

    public static class PowerInfo
    {
        public static void Initialize()
        {
            CommandSystem.Register("PowerInfo", AccessLevel.Counselor, e =>
            {
                e.Mobile.Target = new TargetImpl();
                e.Mobile.SendMessage("Target a creature.");
            });
        }

        private sealed class TargetImpl : Target
        {
            public TargetImpl() : base(12, false, TargetFlags.None) { }
            protected override void OnTarget(Mobile from, object o)
            {
                if (o is BaseCreature bc)
                {
                    var d = bc.PowerBreakdown;
                    from.SendMessage($"Power {d.Total:F1} | HP {d.Hp:F0}, ARM {d.ArmorOrResists:F0}, MELEE {d.Melee:F1}, MAGIC {d.Magic:F1}, SPEC {d.Specials:F1}");
                }
                else from.SendMessage("Not a creature.");
            }
        }
    }
}

namespace Server.Commands
{
    public static class DumpPowers
    {
        public static void Initialize()
        {
            CommandSystem.Register("DumpPowers", AccessLevel.Administrator, OnCommand);
        }

        // Usage:
        //   [DumpPowers
        //   [DumpPowers Data\\power_world.csv
        //   [DumpPowers Data\\power_world.csv includeAll
        //
        // By default excludes: vendors, controlled, summoned, invulnerable/blessed, deleted.
        // Pass "includeAll" to include everything.
        [Usage("DumpPowers [path] [includeAll]")]
        [Description("Dumps Power for all in-world BaseCreature instances. Also writes a per-type median summary.")]
        private static void OnCommand(CommandEventArgs e)
        {
            string path = e.Length >= 1 && !IsFlag(e.GetString(0))
                ? e.GetString(0)
                : Path.Combine(Core.ExeDirectory, "power_world.csv");

            bool includeAll = false;
            for (int i = 0; i < e.Length; i++)
            {
                var a = e.GetString(i);
                if (IsFlag(a) && a.Equals("includeAll", StringComparison.OrdinalIgnoreCase))
                    includeAll = true;
            }

            try
            {
                var rows = CollectRows(includeAll);
                if (rows.Count == 0)
                {
                    e.Mobile.SendMessage(0x22, "[DumpPowers] No eligible creatures found.");
                    return;
                }

                WritePerInstanceCsv(path, rows);

                // Also write per-type medians next to it
                string typePath = Path.ChangeExtension(path, ".types.csv");
                WritePerTypeCsv(typePath, rows);

                e.Mobile.SendMessage(0x44, $"[DumpPowers] Wrote {rows.Count} rows → {path}");
                e.Mobile.SendMessage(0x44, $"[DumpPowers] Wrote per-type medians → {typePath}");
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage(0x22, $"[DumpPowers] ERROR: {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        private static bool IsFlag(string s)
            => s != null && (s.Equals("includeAll", StringComparison.OrdinalIgnoreCase));

        private sealed class Row
        {
            public string? TypeName;
            public int Serial;
            public string? Map;
            public int X, Y, Z;
            public string? Region;
            public double Power, Hp, Armor, Melee, Magic, Specials;
            public int HitsMax, DmgMin, DmgMax;
            public double MageryBase, EvalIntBase, TacticsBase, WrestlingBase;
            public bool Vendor, Controlled, Summoned, Invuln;
        }

        private static List<Row> CollectRows(bool includeAll)
        {
            var rows = new List<Row>(2048);

            foreach (var m in World.Mobiles.Values)
            {
                if (m is not BaseCreature bc)
                    continue;

                if (!includeAll && !IsEligible(bc, requireSpawner: true))
                    continue;

                try
                {
                    var p = bc.Power;               // computed via your EnsurePower()
                    var d = bc.PowerBreakdown;

                    rows.Add(new Row
                    {
                        TypeName = bc.GetType().FullName,
                        Serial = bc.Serial,
                        Map = bc.Map?.ToString() ?? "None",
                        X = bc.X,
                        Y = bc.Y,
                        Z = bc.Z,
                        Region = bc.Region?.Name ?? "None",

                        Power = d.Total,
                        Hp = d.Hp,
                        Armor = d.ArmorOrResists,
                        Melee = d.Melee,
                        Magic = d.Magic,
                        Specials = d.Specials,

                        HitsMax = bc.HitsMax,
                        DmgMin = bc.DamageMin,
                        DmgMax = bc.DamageMax,

                        MageryBase = GetBaseSkill(bc, SkillName.Magery),
                        EvalIntBase = GetBaseSkill(bc, SkillName.EvalInt),
                        TacticsBase = GetBaseSkill(bc, SkillName.Tactics),
                        WrestlingBase = GetBaseSkill(bc, SkillName.Wrestling),

                        Vendor = bc is BaseVendor,
                        Controlled = bc.Controlled,
                        Summoned = bc.Summoned,
                        Invuln = m.Blessed
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DumpPowers] Skipped {m} ({m.GetType().Name}): {ex.Message}");
                }
            }

            // Sort by power desc for readability
            rows.Sort((a, b) => b.Power.CompareTo(a.Power));
            return rows;
        }

        private static readonly string[] GmNameHints = { "cherub", "test", "debug", "gm", "play", "toy" };

        private static bool IsEligible(BaseCreature bc, bool requireSpawner = false, Map? allowedMap = null)
        {
            // --- Hard excludes / fast outs ---
            if (bc == null || bc.Deleted) return false;
            if (!bc.Alive) return false;
            if (bc.Blessed) return false;
            if (bc.IsInvulnerable) return false;
            if (bc is BaseVendor) return false;
            if (bc is BaseOverland) return false; // escorts/bandits/treasure hunters
            if (bc is BaseShieldGuard) return false; // order/chaos, escort defenders
            if (bc is BaseGuard) return false; // guards
            if (bc.Controlled || bc.Summoned) return false;
            if (bc.Map == null || bc.Map == Map.Internal) return false;

            // Map filter: default to Felucca if not specified, else allow caller to set
            var targetMap = allowedMap ?? Map.Felucca;
            if (bc.Map != targetMap) return false;

            // Decorative / placeholder corpses
            var t = bc.GetType();
            if (t.Name.StartsWith("Dead", StringComparison.OrdinalIgnoreCase)) return false;

            // --- Provenance gate ---
            if (requireSpawner)
            {
                if (bc.SpawnerHandle is not Spawner s) return false;  // excludes champ engines / null spawner
                if (s.Running == false) return false;
                if (s.CoreSpawn == false) return false;

                // probably never trus since we are filtering for 'core spawn', but let's be sure
                if (s.CustomMobile() == true) return false;

                return true; // provenance is authoritative
            }

            // Optional: keep for now, but it's safe to remove later if it blocks legit spawns
            if (IsLikelyGmPlaything(bc)) return false;

            // Non-spawner path: you can add more heuristics here later if needed.
            return true;
        }

        private static bool IsLikelyGmPlaything(BaseCreature bc)
        {
            string displayName = $"{bc.Name ?? ""} {bc.Title ?? ""}".ToLowerInvariant().Trim();
            if (string.IsNullOrWhiteSpace(displayName))
                return false;

            // Quick soft GM-name/title hints
            foreach (var hint in GmNameHints) if (displayName.Contains(hint)) return true;

            // CamelCase typename token match (weak tie-breaker)
            string typeName = bc.GetType().Name;
            var tokens = System.Text.RegularExpressions.Regex
                .Split(typeName, @"(?<!^)(?=[A-Z])")
                .Select(t => t.ToLowerInvariant())
                .Where(t => t.Length > 1);

            bool match = tokens.Any(tok => displayName.Contains(tok));
            return !match;
        }
        private static double GetBaseSkill(BaseCreature bc, SkillName name)
        {
            var s = bc.Skills?[name];
            return s?.Base ?? 0.0; // BASE, not Value (ignore buffs/debuffs)
        }

        private static void WritePerInstanceCsv(string path, List<Row> rows)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Core.ExeDirectory);

            using var sw = new StreamWriter(path, false);
            sw.WriteLine("Type,Serial,Map,X,Y,Z,Region,Power,HP,ArmorOrResists,Melee,Magic,Specials,HitsMax,DamageMin,DamageMax,MageryBase,EvalIntBase,TacticsBase,WrestlingBase,IsVendor,Controlled,Summoned,Invuln");

            foreach (var r in rows)
            {
                var typeName = r.TypeName?.Replace(",", " ");
                var region = (r.Region ?? "").Replace(",", " ");
                sw.WriteLine(string.Join(",",
                    typeName, r.Serial,
                    r.Map, r.X, r.Y, r.Z, region,
                    r.Power.ToString("F4"),
                    r.Hp.ToString("F2"),
                    r.Armor.ToString("F2"),
                    r.Melee.ToString("F4"),
                    r.Magic.ToString("F4"),
                    r.Specials.ToString("F4"),
                    r.HitsMax, r.DmgMin, r.DmgMax,
                    r.MageryBase.ToString("F1"),
                    r.EvalIntBase.ToString("F1"),
                    r.TacticsBase.ToString("F1"),
                    r.WrestlingBase.ToString("F1"),
                    r.Vendor ? 1 : 0,
                    r.Controlled ? 1 : 0,
                    r.Summoned ? 1 : 0,
                    r.Invuln ? 1 : 0
                ));
            }
        }

        private static void WritePerTypeCsv(string path, List<Row> rows)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Core.ExeDirectory);

            var groups = rows.GroupBy(r => r.TypeName)
                             .Select(g => new
                             {
                                 TypeName = g.Key,
                                 Count = g.Count(),
                                 MedianPower = Median(g.Select(x => x.Power)),
                                 MedianHP = Median(g.Select(x => x.Hp)),
                                 MedianArmor = Median(g.Select(x => x.Armor)),
                                 MedianMelee = Median(g.Select(x => x.Melee)),
                                 MedianMagic = Median(g.Select(x => x.Magic))
                             })
                             .OrderByDescending(x => x.MedianPower)
                             .ToList();

            using var sw = new StreamWriter(path, false);
            sw.WriteLine("Type,Count,MedianPower,MedianHP,MedianArmor,MedianMelee,MedianMagic");
            foreach (var g in groups)
            {
                var typeName = g.TypeName?.Replace(",", " ");
                sw.WriteLine(string.Join(",",
                    typeName,
                    g.Count,
                    g.MedianPower.ToString("F4"),
                    g.MedianHP.ToString("F2"),
                    g.MedianArmor.ToString("F2"),
                    g.MedianMelee.ToString("F4"),
                    g.MedianMagic.ToString("F4")
                ));
            }
        }

        private static double Median(IEnumerable<double> seq)
        {
            var arr = seq.Where(x => !double.IsNaN(x) && !double.IsInfinity(x)).OrderBy(x => x).ToArray();
            if (arr.Length == 0) return 0;
            int mid = arr.Length / 2;
            return (arr.Length % 2 == 1) ? arr[mid] : (arr[mid - 1] + arr[mid]) * 0.5;
        }
    }
}



