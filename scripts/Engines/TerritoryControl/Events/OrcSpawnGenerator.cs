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
   * OrcSpawnGenerator
   * -----------------
   *
   * PURPOSE:
   * Generates invasion spawn compositions from existing world spawners ("anchors").
   *
   * This module bridges the normal shard ecology and the TerritoryControl
   * occupation system. Rather than using fixed invasion templates, it analyzes
   * the original spawner's threat profile and generates an Orc warband using
   * a power-budget model.
   *
   * CORE CONCEPT:
   * Every anchor spawner contributes a threat budget derived from:
   *
   *     TotalPower * StyleMultiplier * InvasionIntensity
   *
   * The budget is then converted into an Orc invasion force whose composition
   * reflects both the total encounter strength and the average strength of the
   * original creatures.
   *
   * ENVIRONMENTAL SCALING:
   *
   * The generator intentionally preserves the flavor of the original world:
   *
   * - Weak wildlife areas produce modest occupation forces.
   * - Mid-tier creatures produce varied Orc patrols.
   * - Apex predators produce elite Orc warbands.
   *
   * This allows the invasion system to naturally expand beyond towns while
   * remaining believable for each environment.
   *
   * SPAWN PROFILES:
   *
   * Both Swarm and Elite use the same power-aware selection algorithm.
   * They differ only in tuning parameters.
   *
   * 1. Swarm Profile
   *
   *    Used for normal and low/mid-power anchors.
   *
   *    Goals:
   *    - Larger occupation forces
   *    - Broad creature variety
   *    - Organic compositions
   *    - Loose matching to the original encounter
   *
   *    Behavior:
   *    The generator prefers Orcs whose individual power roughly matches
   *    the original creatures while allowing considerable variation.
   *    Selection is biased toward the stronger half of the acceptable
   *    range, producing capable but diverse warbands.
   *
   * 2. Elite Profile
   *
   *    Used for dragons, drakes, daemons, and other apex encounters.
   *
   *    Goals:
   *    - Smaller but dangerous forces
   *    - High-end compositions
   *    - Reduced duplication
   *    - Elite "command groups"
   *
   *    Behavior:
   *    Uses the same power-aware matching algorithm as Swarm but with
   *    tighter power bands, stronger creature preference, and duplicate
   *    limits on High and Elite tier Orcs.
   *
   *    This produces warbands such as:
   *
   *        OrcBrute
   *        OrcishLord
   *        OrcBomber
   *
   *    rather than repetitive stacks of identical elite creatures.
   *
   * HIGH-END CLASSIFICATION:
   *
   * Two thresholds determine whether an anchor uses the Elite profile:
   *
   * - HighEndAvgPowerThreshold
   *   Detects individually powerful creatures.
   *
   * - HighEndTotalPowerThreshold
   *   Detects very small but dangerous encounters.
   *
   * This allows town guards to remain Swarm encounters while dragons,
   * daemons and similar apex threats become Elite warbands.
   *
   * MINIMUM OCCUPATION FLOOR:
   *
   * Extremely weak anchors may produce budgets below the cost of even
   * the weakest configured Orc. This is considered valid behavior.
   *
   * In these cases the generator falls back to the weakest configured
   * Orc so every occupied area still maintains an invasion presence.
   *
   * DESIGN PHILOSOPHY:
   *
   * This system prioritizes:
   *
   * - Environmental fidelity
   * - Emergent warband composition
   * - Variety without randomness
   * - Power matching over rigid templates
   * - Parameter tuning over special-case logic
   *
   * The goal is not mathematical optimization, but invasion forces that
   * look and feel as though an Orc commander assembled them to conquer
   * the original inhabitants.
   *
   * ELITE TUNING NOTES:
   *
   * Elite encounter difficulty is controlled by TWO independent systems.
   *
   * 1. OrcSpawnGenerator (this module)
   *
   *    Controls the composition of each ContestedSpawner through:
   *
   *        powerMultiplier
   *
   *    and the Elite profile tuning parameters.
   *
   *    This determines how dangerous each individual invasion group is.
   *
   * 2. ZoneGuardianSpawner
   *
   *    Controls how many ContestedSpawners are generated for an anchor.
   *
   * Total elite difficulty is therefore:
   *
   *     Individual Spawner Strength
   *         ×
   *     Number of Spawners
   *
   * The threat budget is divided across the web so these two factors can
   * be tuned independently.
   *
   * When adjusting elite difficulty:
   *
   * - Increase powerMultiplier for stronger individual warbands.
   * - Increase guardian counts for broader territorial occupation.
   *
   * Avoid changing both simultaneously unless intentionally increasing
   * the overall invasion pressure.
   * /

/* Scripts/Engines/TerritoryControl/Events/OrcSpawnGenerator.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See TerritorySystem for an architectural overview
 */

using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;
using static Server.Mobiles.Spawner;

namespace Server.Engines.TerritoryControl.Events
{
    public static class OrcSpawnGenerator
    {
        private static readonly string[] ClanNames =
        {
            "Grak'thar", "Urgash", "Krugmok", "Ghorzul", "Thrakgar",
            "Throk'mar", "Garg'zuk", "Krag'thor", "Bru'kaesh",
            "Mogruk", "Zug'mar", "Drogash", "Kharzug", "Grulmok",
            "Nargruk", "Vorgash", "Krathul", "Borgut", "Gromgar",
            "Zul'gor", "Vrag'murok", "Ur'gash",
            "Murgash", "Thrakkar", "Uzgor", "Drakmog", "Gorzhak"
        };
        private static int _clanSerial;

        public static void ResetClanSerial()
        {
            _clanSerial = 0;
        }

        public static int GenerateClanIndex()
        {
            return _clanSerial++;
        }

        public class OrcSpawnInfo
        {
            public Dictionary<string, int> CreatureCounts { get; } = new();

            public int Count { get; set; }                    // Used for legacy spawners

            public string Style { get; set; } = "Unknown";    // informational only

            // Dispersal settings
            public bool Concentric { get; set; } = true;
            public int HomeRange { get; set; } = 4;

            // Timing / Pressure (PBS style)
            public TimeSpan MinDelay { get; set; } = TerritoryConfig.GuardianSwarmMinDelay;
            public TimeSpan MaxDelay { get; set; } = TerritoryConfig.GuardianSwarmMaxDelay;
            public TimeSpan LevelRestartDelay { get; set; } = TerritoryConfig.DefaultLevelRestartDelay;

            public SpawnerModeAttribs Distro { get; set; } = SpawnerModeAttribs.ModeMulti;

            public string Clan { get; set; } = "Invader";

            public int Team { get; set; } = 3;  // 3 means to fight/kill vendors
            public double InvasionIntensity { get; set; } = 1.0;

            /// <summary>
            /// Helper to add or increment a creature type
            /// </summary>
            public void AddCreature(string typeName, int count = 1)
            {
                if (string.IsNullOrWhiteSpace(typeName))
                    typeName = "Orc";

                if (CreatureCounts.ContainsKey(typeName))
                    CreatureCounts[typeName] += count;
                else
                    CreatureCounts[typeName] = count;
            }
        }

        public static OrcSpawnInfo GenerateForAnchor(Spawner anchor, int clanIndex, double budgetScale)
        {
            if (anchor == null)
                return CreateDefaultInfo();

            double totalPower = anchor.TotalPower;
            int originalCount = anchor.GetMobiles().Count;
            double avgPowerPerCreature = totalPower / Math.Max(1, originalCount);

            // Hybrid decision:
            // High-end anchors (dragons, strong guards, etc.) use best-match (Elite)
            // Normal/weak anchors use diversity fill (Swarm)
            bool isHighEndAnchor =
                avgPowerPerCreature > TerritoryConfig.HighEndAvgPowerThreshold ||
                (originalCount <= 2 && totalPower > TerritoryConfig.HighEndTotalPowerThreshold);

            bool isSwarmStyle = !isHighEndAnchor;

            int targetOrcCount;
            double powerMultiplier;
            string styleName;

            if (isSwarmStyle)
            {
                styleName = "Swarm";
                targetOrcCount = originalCount + Utility.RandomMinMax(4, 9);
                powerMultiplier = 1.65;
            }
            else
            {
                styleName = "Elite";
                targetOrcCount = Math.Max(1, originalCount);
                powerMultiplier = 3.10;
            }

            double finalIntensity = TerritoryConfig.InvasionIntensity;
            double threatBudget = totalPower * powerMultiplier * finalIntensity * budgetScale;

            string clanName = ClanNames[clanIndex % ClanNames.Length];

            //Console.WriteLine($"[Orc Invasion] Anchor({anchor}): {totalPower:F1} power, {originalCount} creatures → {styleName} Style | Intensity: {finalIntensity:F2}x | Target: {targetOrcCount} orcs (Budget: {threatBudget:F1})");

            var info = new OrcSpawnInfo
            {
                Count = targetOrcCount,
                Style = styleName,
                Concentric = anchor.Concentric,
                HomeRange = (anchor.HomeRange <= 1) ? 4 : anchor.HomeRange,

                MinDelay = isSwarmStyle ? TerritoryConfig.GuardianSwarmMinDelay : TerritoryConfig.GuardianEliteMinDelay,
                MaxDelay = isSwarmStyle ? TerritoryConfig.GuardianSwarmMaxDelay : TerritoryConfig.GuardianEliteMaxDelay,

                LevelRestartDelay = TerritoryConfig.DefaultLevelRestartDelay,
                Distro = SpawnerModeAttribs.ModeMulti,
                Clan = clanName,
                InvasionIntensity = finalIntensity
            };

            FillCreatureTypes(info, threatBudget, avgPowerPerCreature, isSwarmStyle);

            return info;
        }
        private static void FillCreatureTypes(OrcSpawnInfo info, double threatBudget, double anchorAvgPower, bool isSwarmStyle)
        {
            if (info == null)
                return;

            info.CreatureCounts.Clear();

            var sortedCreatures = OrcInvasionTable.Creatures
                .OrderBy(c => c.Power)
                .ToList();

            if (sortedCreatures.Count == 0)
                return;

            double remainingBudget = threatBudget;

            double preferredMinFactor;
            double preferredMaxFactor;
            bool preferStrongest;
            int maxEliteDuplicates;
            int maxHighDuplicates;

            if (isSwarmStyle)
            {
                // Broad-band match: varied occupation force, but still roughly matched
                // to the original anchor's per-creature threat.
                preferredMinFactor = 0.65;
                preferredMaxFactor = 1.25;
                preferStrongest = false;

                maxEliteDuplicates = 2;
                maxHighDuplicates = 3;
            }
            else
            {
                // Elite profile: top-heavy and dangerous, but encourage
                // diverse warbands rather than repeated elite officers.
                preferredMinFactor = 0.55;
                preferredMaxFactor = 1.10;
                preferStrongest = true;

                maxEliteDuplicates = 1;
                maxHighDuplicates = 1;
            }

            double preferredMinPower = anchorAvgPower * preferredMinFactor;
            double preferredMaxPower = anchorAvgPower * preferredMaxFactor;

            int creaturesAdded = 0;

            while (remainingBudget > 0 && creaturesAdded < info.Count)
            {
                var affordable = sortedCreatures
                    .Where(c => c.Power <= remainingBudget * 1.2)
                    .Where(c => CanAddCreature(info, c.Type, c.Tier, maxEliteDuplicates, maxHighDuplicates))
                    .OrderBy(c => c.Power)
                    .ToList();

                if (affordable.Count == 0)
                    break;

                var preferred = affordable
                    .Where(c => c.Power >= preferredMinPower && c.Power <= preferredMaxPower)
                    .OrderBy(c => c.Power)
                    .ToList();

                var creature = preferred.Count > 0
                    ? (preferStrongest
                        ? preferred.Last()
                        : preferred[Utility.RandomMinMax(preferred.Count / 2, preferred.Count - 1)])
                    : (preferStrongest
                        ? affordable.Last()
                        : affordable[Utility.Random(affordable.Count)]);

                info.AddCreature(creature.Type);

                remainingBudget -= creature.Power;
                creaturesAdded++;
            }

            // Minimum floor:
            // If the computed budget cannot afford any configured orc,
            // fall back to the weakest valid orc so every occupied area
            // still produces an orc presence.
            if (info.CreatureCounts.Count == 0)
            {
                var fallback = sortedCreatures.FirstOrDefault();

                if (fallback != null)
                    info.CreatureCounts[fallback.Type] = Math.Max(1, info.Count);
            }
        }

        private static bool CanAddCreature(
            OrcSpawnInfo info,
            string typeName,
            string tier,
            int maxEliteDuplicates,
            int maxHighDuplicates)
        {
            if (info == null)
                return false;

            info.CreatureCounts.TryGetValue(typeName, out int existingCount);

            if (Insensitive.Equals(tier, "Elite"))
                return existingCount < maxEliteDuplicates;

            if (Insensitive.Equals(tier, "High"))
                return existingCount < maxHighDuplicates;

            return true;
        }
        private static OrcSpawnInfo CreateDefaultInfo()
        {
            var info = new OrcSpawnInfo
            {
                Distro = SpawnerModeAttribs.ModeMulti,
                Style = "Default",
                // intentionally not an orc to make it obvious when something is using default values
                CreatureCounts = { ["Mongbat"] = 3 },
            };

            return info;
        }
    }
}