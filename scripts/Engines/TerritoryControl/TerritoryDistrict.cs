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

/* Scripts\Engines\TerritoryControl\TerritoryDistrict.cs
 * CHANGELOG:
 *   11/5/2026, Adam:
 *      - Warlord placement fallback: the first anchor (strongest first) whose own tile passes the two-axis rule, not
 *        simply the strongest; if no anchor's tile passes, the strongest anyway (a Warlord is never given up: the
 *        conquest bonus has no later retry). A boxed-in scribe spawner in Britain had received a Warlord that could not
 *        move. The journal marks a fallback tile that fails the rule: "(on the anchor's tile; fails the two-axis rule)".
 *      - TryPlaceMiniBoss takes an optional trace: every decision (ranked anchors, each drawn spot and the check it
 *        failed, the fallback and why). Null in play; used by the WarlordTour harness to explain each placement.
 *   11/4/2026, Adam:
 *      - Conquest bonus Warlord: liberating the district (OnDistrictCleared) brings one more Khartag after any earned
 *        Warlord still owed. It rewards winning the town, not kills: it is not counted in MiniBossesSpawned (spawned
 *        never exceeds MaxMiniBosses) and comes once per campaign (IsConquered latches; a recapture resets it). It
 *        announces with the "This town is won... for today" speech, which earned Warlords no longer use. If it cannot
 *        be placed it is lost (no later clear retries it), reported in red. A district marked conquered silently
 *        (PatchV257) gets none. Before this the final conquest usually brought nothing: the kills on fallen spawners
 *        reach the cap first (Jhelom's last earned Warlord came one zone early, Britain's nine).
 *      - Campaign ID and journal (TerritoryJournal, Logs/TerritoryCampaigns.log): CampaignStart (saved; TerritorySystem
 *        v4) names the current campaign, CampaignID = "<district>@<start, UTC>". BeginCampaign (a new district, a
 *        recapture) and ObserveCampaign (a campaign already running in an older save; CampaignObserved) journal its
 *        start. Journaled: zone conquered, district liberated (with the counters), each Warlord placed (earned n of max,
 *        or the conquest bonus; serial, spot, anchor) or not placed (the reason), and the recapture's credit audit,
 *        which now writes its lines under the ending campaign's ID. Placed Warlords carry the ID (Khartag
 *        TerritoryCampaign) so their deaths are journaled too.
 *      - Warlord placement: when no spot near any anchor has a walking path back to it, the Warlord is placed on the
 *        first anchor's own tile (walkable land: GetMiniBossAnchors checked it; still no unvalidated spot). An anchor
 *        inside a building found only spots outside its walls, and the Warlord waited for the next clear (the journal
 *        showed it: Jhelom zone 3059, spawner 0x400AB3A4; Britain zone 3365, spawner 0x400AB387; both in buildings).
 *      - Warlord placement: two-axis movement rule (ZoneGuardianSpawner.HasTwoAxisMovement, with the diagrams; shared
 *        with contested spawner placement). A spot must let a
 *        walker step north or south AND east or west (the game's movement check, diagonals not counted), so one-tile
 *        corridors are refused: Trinsic's invisible walkway between a wall and the moat, at (1858, 2642, -1), looks like
 *        water and left a Warlord there with no lateral movement. A refused spot is drawn again, up to
 *        MiniBossTriesPerAnchor (5) times per anchor; the anchor's own tile (the fallback) is exempt.
 *   10/27/2026, Adam:
 *      - Recapture (RestartDistrict, the Town Guardian's death) now resets everything a new campaign must win again:
 *        each web-bearing zone's conquest (IsConquered, ConqueredAt), the campaign's Warlord count (MiniBossesSpawned)
 *        and the spawners' contribution credit. Zones used to stay conquered: a re-cleared web's notification stopped at
 *        its zone and the district could never be liberated again (Vesper, Skara Brae; Britain and Jhelom were next).
 *        The credit is written to Logs/TerritoryCampaigns.log before it is cleared (every bonus earned from it was paid
 *        when its spawner, web or district was cleared; a new campaign starts from zero). If that record cannot be
 *        written, the credit is kept rather than lost (reported in red and to the exception log; the new campaign then
 *        inherits it, as before this fix).
 *      - IsComplete is public (read only) for PatchV257 (TerritoryConquestRepair, in Patches201_300).
 *   10/19/2026, Adam:
 *      - Orc Warlord (Khartag) placement: anchored on a Running, original (non-contested) spawner on walkable land
 *        in the last conquered zone, placed with the Town Guardian's mechanics
 *        (Concentric | SpawnFar | ForceZ | NoBlock | ClearPath, no AvoidPlayers, which bypassed ClearPath),
 *        and the final spot must have a clear walking path back to that spawner. Home is the anchor.
 *        Contested spawners are no longer used as anchors (some were placed in water).
 *      - Anchor choice: only spawners configured to spawn mobiles (never item-only spawners, e.g. barrels).
 *        The anchor with the highest AvgPower is tried first (creature strength, not population; guards and vendors
 *        included: Khartag appears near the town's most significant spawners), ties in random order; spawners with
 *        nothing alive come last.
 *      - No eligible anchor or spot: no spawn (no unvalidated fallback).
 *      - MiniBossesSpawned counts a mini-boss only once it is placed. A failed placement stops the loop;
 *        the mini-boss stays earned and is tried again at the next zone/district clear.
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See TerritorySystem for an architectural overview
 */

using Server.Engines.DataRecorder;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Server.Utility;

namespace Server.Engines.TerritoryControl
{
    public class TerritoryDistrict
    {
        public string Name { get; }
        public Map Map { get; }
        public List<TerritoryZone> Zones { get; } = new();
        public int MiniBossesSpawned { get; set; }
        public bool IsConquered { get; set; }

        public bool WasOriginallyGuarded { get; set; }

        /// <summary>
        /// When the current campaign began (UTC; saved). CampaignObserved: it began before the journal existed and
        /// CampaignStart is when it was first seen.
        /// </summary>
        public DateTime CampaignStart { get; set; } = DateTime.UtcNow;
        public bool CampaignObserved { get; set; }
        /// <summary>The current campaign's journal handle: "Jhelom@2026-11-04T17:02:13Z".</summary>
        public string CampaignID => string.Format("{0}@{1}", Name, CampaignStart.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>A new campaign begins now (a new district, or the Orcs recaptured it): journaled.</summary>
        public void BeginCampaign(string why)
        {
            CampaignStart = DateTime.UtcNow;
            CampaignObserved = false;
            TerritoryJournal.Write(CampaignID, string.Format("campaign begins ({0}): {1} webs, {2} kills needed; Warlords: one per {3} stable kills, {4} max, + the conquest bonus",
                why, Zones.Count(z => z?.Web != null), TotalKillsNeeded, TerritoryConfig.MiniBossKillInterval, MaxMiniBosses));
        }
        /// <summary>A campaign already running when the journal began (an older save): its state as first seen, journaled.</summary>
        public void ObserveCampaign()
        {
            CampaignStart = DateTime.UtcNow;
            CampaignObserved = true;
            TerritoryJournal.Write(CampaignID, string.Format("campaign in progress, first seen (it began before the journal): {0}",
                Status()));
        }
        // the counters a journal line reports
        private string Status()
        {
            List<TerritoryZone> webZones = Zones.Where(z => z?.Web != null).ToList();
            return string.Format("conquered {0}; webs {1}/{2} won; kills {3}/{4}, stable {5}; Warlords earned {6}, spawned {7}, max {8}",
                IsConquered, webZones.Count(z => z.IsConquered), webZones.Count, TotalKills, TotalKillsNeeded, StableMiniBossKills,
                EarnedMiniBosses, MiniBossesSpawned, MaxMiniBosses);
        }
        public int TotalKillsNeeded
        {
            get
            {
                return Zones
                    .Where(z => z?.Web != null)
                    .Sum(z => z.Web.WebKillsNeeded);
            }
        }

        public int TotalKills
        {
            get
            {
                return Zones
                    .Where(z => z?.Web != null)
                    .Sum(z => z.Web.TotalKills);
            }
        }

        public int MaxMiniBosses
        {
            get
            {
                int interval = TerritoryConfig.MiniBossKillInterval;

                if (interval <= 0)
                    return 0;

                if (TotalKillsNeeded <= 0)
                    return 0;

                return Math.Max(1, TotalKillsNeeded / interval);
            }
        }
        /// <summary>
        /// Returns kill credit that is considered "stable" for mini-boss earning.
        ///
        /// TerritoryControl distinguishes between:
        ///
        /// Random Kills:
        ///     Progress on active spawners that have not yet been deactivated.
        ///     These kills contribute to district progress, but can be lost if the
        ///     spawner times out and regains ground.
        ///
        /// Stable Kills:
        ///     Kills on deactivated/completed spawners. These kills can no longer
        ///     roll back and therefore represent permanent territorial gains.
        ///
        /// Mini-bosses are earned from Stable Kills only. This prevents players
        /// from farming partial progress across many active spawners to generate
        /// mini-bosses without actually conquering territory.
        /// </summary>
        public int StableMiniBossKills
        {
            get
            {
                return Zones
                    .Where(z => z?.Web != null)
                    .SelectMany(z => z.Web.Spawners)
                    .Where(s => s != null && !s.Deleted && s.Deactivated)
                    .Sum(s => s.SpawnerKills);
            }
        }
        public int EarnedMiniBosses
        {
            get
            {
                int interval = TerritoryConfig.MiniBossKillInterval;

                if (interval <= 0)
                    return 0;

                if (IsComplete)
                    return MaxMiniBosses;

                return Math.Min(MaxMiniBosses, StableMiniBossKills / interval);
            }
        }
        public TerritoryDistrict(string name, Map map)
        {
            Name = name;
            Map = map;
        }

        public void AddZone(TerritoryZone zone)
        {
            Zones.Add(zone);
        }

        /// <summary>
        /// Called when a web in this district is completed
        /// </summary>
        public void NotifyWebCompleted()
        {
            if (IsConquered)
                return;

            if (IsComplete)
                OnDistrictCleared();
        }

        // every GuardianWeb in the district is complete (zones without a web are not required); a district with no web
        //  is never complete, and a web with a missing or deleted spawner is never complete (GuardianWeb.IsComplete)
        public bool IsComplete
        {
            get
            {
                bool anyWeb = false;

                foreach (var zone in Zones)
                {
                    if (zone?.Web == null)
                        continue;

                    anyWeb = true;

                    if (!zone.Web.IsComplete)
                        return false;
                }

                return anyWeb;
            }
        }

        /// <summary>
        /// The Orcs have recaptured the district (the Town Guardian died): a new campaign begins. Every web, its zone and
        /// its spawners must be won again, the campaign earns its own Warlords, and contribution credit starts from zero
        /// (audited to Logs/TerritoryCampaigns.log first).
        /// </summary>
        public bool RestartDistrict()
        {
            bool restarted = false;

            // the credit is cleared only once its record is written: a campaign's credit is never lost unrecorded
            bool audited = AuditCampaignCredit("recaptured");
            if (!audited)
                Utility.Monitor.WriteLine("[TerritoryControl] {0}: the campaign credit record could not be written; the credit is kept (see the exception log).",
                    ConsoleColor.Red, Name);

            foreach (TerritoryZone zone in Zones)
            {
                GuardianWeb? web = zone?.Web;

                if (zone == null || web == null)
                    continue;

                web.IsConquered = false;

                // the zone must be conquered again: left conquered, it stopped the re-cleared web's notification
                zone.IsConquered = false;
                zone.ConqueredAt = DateTime.MinValue;

                foreach (var spawner in web.Spawners)
                {
                    if (spawner == null || spawner.Deleted)
                        continue;

                    spawner.SpawnerKills = 0;
                    if (audited)
                        spawner.RollbackContributions();    // the new campaign's credit starts from zero (recorded above)
                    spawner.Deactivated = false;
                    spawner.Running = true;
                    spawner.ScheduleRespawn = true;

                    restarted = true;
                }
            }

            IsConquered = false;
            MiniBossesSpawned = 0;                          // the new campaign earns its own Warlords

            BeginCampaign("the Orcs recaptured the district");

            return restarted;
        }
        /// <summary>
        /// Writes every spawner's contribution credit (district, zone, spawner, player, damage, finalized) to
        /// Logs/TerritoryCampaigns.log: the record of a campaign whose credit is about to be cleared. Returns false if the
        /// record could not be written (the caller then keeps the credit).
        /// </summary>
        private bool AuditCampaignCredit(string reason)
        {
            try
            {
                List<string> lines = new List<string>();
                int players = 0, damage = 0;

                foreach (TerritoryZone zone in Zones)
                {
                    GuardianWeb? web = zone?.Web;

                    if (zone == null || web == null)
                        continue;

                    foreach (ContestedSpawner spawner in web.Spawners)
                    {
                        if (spawner == null || spawner.Deleted || !spawner.HasContributions)
                            continue;

                        bool finalized = spawner.InternalContributions?.IsFinalized ?? false;

                        foreach (KeyValuePair<Serial, int> kvp in spawner.GetContributionData())
                        {
                            Mobile? m = World.FindMobile(kvp.Key);
                            lines.Add(string.Format("zone {0} spawner 0x{1:X8} player 0x{2:X8} ({3}) damage {4} finalized {5}",
                                zone.ZoneID, spawner.Serial.Value, kvp.Key.Value, m?.Name ?? "unknown", kvp.Value, finalized));
                            players++;
                            damage += kvp.Value;
                        }
                    }
                }

                lines.Insert(0, string.Format("campaign credit cleared ({0}): {1} credit entries, {2} damage; {3}",
                    reason, players, damage, Status()));

                return TerritoryJournal.Write(CampaignID, lines);  // the campaign that ends here
            }
            catch (Exception ex)
            {
                Server.Diagnostics.LogHelper.LogException(ex);
                return false;
            }
        }

        public void OnDistrictCleared()
        {
            if (IsConquered)
                return;

            IsConquered = true;

            Dictionary<Serial, int> credit = GetAllContributions();
            TerritoryJournal.Write(CampaignID, string.Format("district liberated: {0}; {1} contributors, {2} damage credit",
                Status(), credit.Count, credit.Values.Sum()));

            TerritoryMessenger.ReportDistrictConquered(Name, Map);
            AwardDistrictBonus();

            CheckMiniBossSpawn();

            // the conquest bonus Warlord: once per campaign, not counted (see the 11/4/2026 changelog)
            if (!SpawnMiniBoss(first: true, "conquest bonus", ConquestMessage))
                Utility.Monitor.WriteLine("[TerritoryControl] {0}: the conquest bonus Warlord could not be placed.", ConsoleColor.Red, Name);
        }
        public void OnZoneCleared(TerritoryZone zone)
        {
            TerritoryJournal.Write(CampaignID, string.Format("zone {0} conquered: {1}", zone.ZoneID, Status()));
            CheckMiniBossSpawn();
            NotifyWebCompleted();
        }
        public void CheckMiniBossSpawn()
        {
            bool first = true;

            while (MiniBossesSpawned < EarnedMiniBosses)
            {
                // counted only once placed; if it can't be placed, stop (it stays earned for the next clear)
                if (!SpawnMiniBoss(first, string.Format("earned {0} of {1}", MiniBossesSpawned + 1, MaxMiniBosses)))
                    break;

                MiniBossesSpawned++;

                first = false;
            }
        }
        private static readonly string[][] m_SpawnMessages =
        {
            new string[]
            {
                "*A booming voice echoes across the battlefield.*",
                "\"I am {0}, {1} of the Horde. You have driven back my warriors and earned my respect. Face me now, and let us settle this as warriors.\""
            },

            new string[]
            {
                "*Orcish drums thunder in the distance.*",
                "\"I am {0}, {1} of the Horde. My warriors watch from the hills. Show them whether humans possess courage... or only numbers.\""
            },

            new string[]
            {
                "*A harsh laugh rolls across the valley.*",
                "\"So these are the heroes of the town? I am {0}, {1} of the Horde! Leave your walls behind and face me, if courage still lives among you.\""
            },

            new string[]
            {
                "*A warhorn sounds from beyond the fighting.*",
                "\"I am {0}, {1} of the Horde. This town is won... for today. Meet me if you would dictate the terms of our retreat.\""
            },

            new string[]
            {
                "*A wounded voice calls from the darkness.*",
                "\"{0}, {1} of the Horde, is broken. My captains are dead and my warband scattered. Come to me and hear my final bargain.\""
            },

            new string[]
            {
                "*The ground trembles beneath heavy footsteps.*",
                "\"I am {0}, {1} of the Horde. Enough blood has been spilled. Face me!\""
            },

            new string[]
            {
                "*An enraged roar shakes the air.*",
                "\"You have slain my champions and scattered my clan. I am {0}, {1} of the Horde, and now you will answer for it!\""
            },

            new string[]
            {
                "*The clash of steel falls strangely silent.*",
                "\"I seek the one who led this assault. I am {0}, {1} of the Horde. Step forward and face me!\""
            }
        };
        // m_SpawnMessages: the conquest bonus Warlord's speech; earned Warlords use the others
        private const int ConquestMessage = 3;
        // kind: what the journal calls it ("earned 5 of 13", "conquest bonus")
        private bool SpawnMiniBoss(bool first, string kind, int message = -1)
        {
            TerritoryZone? zone = GetLastConqueredZone();

            if (zone == null || Map == null || Map == Map.Internal)
            {
                TerritoryJournal.Write(CampaignID, string.Format("Warlord not placed ({0}): {1}", kind,
                    zone == null ? "no conquered zone" : "the district is not on a world map"));
                return false;
            }

            List<Spawner> anchors = GetMiniBossAnchors(zone);

            if (anchors.Count == 0)
            {
                Console.WriteLine("[TerritoryControl] No miniboss spawned in district {0}: no running mobile spawner on walkable land in zone {1}.", Name, zone.Name);
                TerritoryJournal.Write(CampaignID, string.Format("Warlord not placed ({0}): no running mobile spawner on walkable land in zone {1} {2}", kind, zone.ZoneID, zone.Bounds));
                return false;
            }

            Khartag miniboss = new Khartag();

            if (!TryPlaceMiniBoss(miniboss, anchors, out Spawner? anchor, out Point3D loc) || anchor == null)
            {
                miniboss.Delete();
                Console.WriteLine("[TerritoryControl] No miniboss spawned in district {0}: no reachable spot near the {1} spawner(s) in zone {2}.", Name, anchors.Count, zone.Name);
                TerritoryJournal.Write(CampaignID, string.Format("Warlord not placed ({0}): no reachable spot near the {1} spawner(s) in zone {2} {3}: {4}", kind, anchors.Count, zone.ZoneID, zone.Bounds,
                    string.Join(", ", anchors.Take(8).Select(s => string.Format("0x{0:X8} at {1}", s.Serial.Value, s.Location))) + (anchors.Count > 8 ? ", ..." : "")));
                return false;
            }

            miniboss.TerritoryCampaign = CampaignID;    // its death is journaled under this campaign

            #region add some dyes and dyetub
            if (Core.RuleSets.SeasonsRules(2))
            {
                if (miniboss.Backpack == null)
                    Utility.AddBackpack(miniboss);

                miniboss.AddToBackpack(new Dyes());
                miniboss.AddToBackpack(new DyeTub());
            }
            #endregion add some dyes and dyetub

            #region Team

            // make sure this team setting matches our contested spawners or else our orcs will fight the mini boss
            // setting this to anythig other than 0 will cause our spawn to kill all nearby base creatures
            miniboss.Team = (Core.RuleSets.SeasonsRules(3) || Core.RuleSets.UpheavalRules()) ? 0 : 3;

            #endregion Team


            miniboss.Home = anchor.Location;
            miniboss.RangeHome = 10;

            miniboss.MoveToWorld(loc, Map);
            miniboss.OnAfterSpawn();

            if (first)
                MiniBossMessage(miniboss, loc, Map, 40, message);

            Console.WriteLine(
                "[TerritoryControl] Spawned miniboss {0} in district {1} at {2} ({3}).",
                miniboss.Name,
                Name,
                loc,
                Map?.Name ?? "Internal");
            TerritoryJournal.Write(CampaignID, string.Format("Warlord 0x{0:X8} {1} placed ({2}) at {3}, anchor spawner 0x{4:X8} at {5}{6}, zone {7}",
                miniboss.Serial.Value, miniboss.Name, kind, loc, anchor.Serial.Value, anchor.Location, loc != anchor.Location ? "" : ZoneGuardianSpawner.HasTwoAxisMovement(Map, loc) ? " (on the anchor's tile)"
                    : " (on the anchor's tile; fails the two-axis rule)", zone.ZoneID));

            return true;
        }
        /// <summary>
        /// Original (GM-placed, non-contested) spawners in the zone that can anchor the mini-boss:
        /// Running, configured to spawn mobiles (never item-only), and on walkable land for a land creature.
        /// </summary>
        private List<Spawner> GetMiniBossAnchors(TerritoryZone zone)
        {
            List<Spawner> list = new List<Spawner>();

            foreach (Spawner spawner in Spawner.Instances)
            {
                if (spawner == null || spawner.Deleted || spawner.GetType() != typeof(Spawner))
                    continue;

                if (spawner.Map != Map || !zone.Contains(spawner.Location) || !spawner.Running)
                    continue;

                if (!SpawnsMobiles(spawner))
                    continue;

                if (ZoneGuardianSpawner.IsWalkableLand(Map, spawner.Location))
                    list.Add(spawner);
            }

            return list;
        }
        /// <summary>
        /// The spawner's configured entries include a mobile (creature, guard, vendor, ...), whatever is alive right now.
        /// </summary>
        private static bool SpawnsMobiles(Spawner spawner)
        {
            foreach (Type type in spawner.ObjectTypes)
                if (typeof(Mobile).IsAssignableFrom(type))
                    return true;

            return false;
        }
        /// <summary>
        /// The order anchors are tried in: highest AvgPower first (the strength of the spawner's creatures, not their
        /// number: a guard post outranks a crowd of town animals; guards and vendors included), equal Power in random
        /// order. Spawners with nothing alive (Power 0) come last, in random order: they are still configured to spawn mobiles.
        /// </summary>
        private static List<Spawner> RankMiniBossAnchors(List<Spawner> anchors)
        {
            List<Spawner> shuffled = new List<Spawner>(anchors);
            Utility.Shuffle(shuffled);

            // OrderByDescending is a stable sort: ties keep their shuffled order
            return shuffled
                .Select(s => new { Spawner = s, Power = s.AvgPower })
                .OrderByDescending(e => e.Power)
                .Select(e => e.Spawner)
                .ToList();
        }
        /// <summary>
        /// Town Guardian placement (see GypsySeer.DoSummon) around the anchors in RankMiniBossAnchors order. The spot
        /// must have a clear walking path back to the anchor. If GetSpawnPosition finds nothing it returns the anchor's
        /// own tile, which GetMiniBossAnchors has already checked. The spot must also offer movement on both axes
        /// (ZoneGuardianSpawner.HasTwoAxisMovement, where the rule's diagrams are). Each anchor is tried up to MiniBossTriesPerAnchor times; if none gives a spot that passes,
        /// an anchor's own tile: the first one whose tile passes the rule, else the first anchor's tile anyway.
        /// trace (optional; diagnostics and harnesses): every decision, in order: the ranked anchors, each drawn spot and
        /// the check it failed, and the outcome. Null in production play: nothing extra is computed.
        /// </summary>
        private bool TryPlaceMiniBoss(Mobile m, List<Spawner> anchors, out Spawner? anchor, out Point3D loc, List<string>? trace = null)
        {
            const SpawnFlags flags = SpawnFlags.Concentric | SpawnFlags.SpawnFar | SpawnFlags.ForceZ |
                                     SpawnFlags.NoBlock | SpawnFlags.ClearPath;   // never AvoidPlayers: it bypasses ClearPath

            List<Spawner> ranked = RankMiniBossAnchors(anchors);
            trace?.Add("ranked anchors: " + string.Join(", ", ranked.Select(s => string.Format("0x{0:X8} at {1} avgPower {2:F1}", s.Serial.Value, s.Location, s.AvgPower))));
            foreach (Spawner candidate in ranked)
            {
                for (int i = 0; i < MiniBossTriesPerAnchor; i++)
                {
                    Point3D p = Spawner.GetSpawnPosition(Map, candidate.Location, 5, flags, m);

                    if (p != Point3D.Zero && ZoneGuardianSpawner.CanWalkTo(p, candidate.Location, Map) && ZoneGuardianSpawner.HasTwoAxisMovement(Map, p))
                    {
                        trace?.Add(string.Format("anchor 0x{0:X8} try {1}: {2} accepted{3}", candidate.Serial.Value, i + 1, p,
                            p == candidate.Location ? " (GetSpawnPosition returned the anchor's own tile)" : ""));
                        anchor = candidate;
                        loc = p;
                        return true;
                    }
                    trace?.Add(string.Format("anchor 0x{0:X8} try {1}: {2} rejected: {3}", candidate.Serial.Value, i + 1, p,
                        p == Point3D.Zero ? "GetSpawnPosition found nothing"
                        : !ZoneGuardianSpawner.CanWalkTo(p, candidate.Location, Map) ? "no walking path back to the anchor"
                        : "fails the two-axis rule"));
                }
            }

            // no spot with a path back to any anchor and movement on both axes (e.g. an anchor inside a building: the spots
            //  found are outside its walls): an anchor's own tile, walkable land (GetMiniBossAnchors checked it). The first
            //  anchor whose tile passes the two-axis rule; if none does, the first anchor anyway (never give up a Warlord)
            if (ranked.Count > 0)
            {
                Spawner? passing = ranked.FirstOrDefault(s => ZoneGuardianSpawner.HasTwoAxisMovement(Map, s.Location));
                anchor = passing ?? ranked[0];
                loc = anchor.Location;
                trace?.Add(string.Format("fallback: anchor 0x{0:X8}'s own tile {1}: {2}", anchor.Serial.Value, loc,
                    passing != null ? "the first ranked anchor whose tile passes the two-axis rule"
                    : "no anchor's tile passes the two-axis rule; the first ranked anchor anyway"));
                return true;
            }

            anchor = null;
            loc = Point3D.Zero;
            return false;
        }
        // a spot near an anchor is drawn again (GetSpawnPosition is random) when it fails a check, up to this many times
        private const int MiniBossTriesPerAnchor = 5;

        private void MiniBossMessage(Khartag miniboss, Point3D loc, Map map, int distance, int index = -1)
        {
            List<PlayerMobile> players = GetMiniBossMessagePlayers(loc, map, distance);

            if (players.Count == 0)
                return;

            if (index < 0)
            {   // an earned Warlord: any speech but the conquest's
                index = Utility.Random(m_SpawnMessages.Length - 1);
                if (index >= ConquestMessage)
                    index++;
            }
            string[] message = m_SpawnMessages[index];

            string name = miniboss.Name ?? "Khartag";
            string title = miniboss.Title ?? "Warlord";

            string speech = string.Format(message[1], name, title);

            foreach (PlayerMobile pm in players)
            {
                pm.SendMessage(0x22, message[0]);
                pm.SendMessage(0x22, speech);
            }
        }
        private TerritoryZone? GetLastConqueredZone()
        {
            TerritoryZone? last = null;

            foreach (TerritoryZone zone in Zones)
            {
                if (zone == null || !zone.IsConquered)
                    continue;

                if (last == null || zone.ConqueredAt > last.ConqueredAt)
                    last = zone;
            }

            return last;
        }
        private List<PlayerMobile> GetMiniBossMessagePlayers(Point3D loc, Map map, int distance)
        {
            List<PlayerMobile> players = new List<PlayerMobile>();

            if (map == null || map == Map.Internal)
                return players;

            HashSet<Mobile> seen = new HashSet<Mobile>();

            // Players physically near the miniboss spawn.
            IPooledEnumerable eable = map.GetMobilesInRange(loc, distance);

            foreach (Mobile m in eable)
            {
                if (m is PlayerMobile pm && !pm.Deleted && pm.NetState != null)
                {
                    players.Add(pm);
                    seen.Add(pm);
                }
            }

            eable.Free();

            // Players who contributed to the district fight.
            Dictionary<Serial, int> totalContributions = GetAllContributions();

            foreach (Serial serial in totalContributions.Keys)
            {
                Mobile? m = World.FindMobile(serial);

                if (m is PlayerMobile pm && !pm.Deleted && pm.NetState != null && !seen.Contains(pm))
                {
                    players.Add(pm);
                    seen.Add(pm);
                }
            }

            return players;
        }
        public void AwardDistrictBonus()
        {
            var totalContributions = GetAllContributions();
            if (totalContributions.Count == 0)
                return;

            double totalDamage = totalContributions.Values.Sum();
            int bonus = (int)(totalDamage * 1.00);   // 100% as we discussed

            // Record bonus into DataRecorder and provide township tax subsidies and fame
            foreach (var kvp in totalContributions)
            {
                Mobile? player = World.FindMobile(kvp.Key);
                if (player != null)
                {
                    DataRecorder.DataRecorder.Add(player, RecorderDomain.LaunchEventPoints, bonus);
                    TerritorySystem.UpdateTownshipForPlayer(player, bonus);
                }
            }
        }
        /// <summary>
        /// Aggregates all player damage contributions across every web (and spawner) in this district.
        /// </summary>
        /// <summary>
        /// Aggregates all player damage contributions across every zone/web in this district.
        /// </summary>
        public Dictionary<Serial, int> GetAllContributions()
        {
            var total = new Dictionary<Serial, int>();

            foreach (var zone in Zones)
            {
                if (zone?.Web == null)
                    continue;

                foreach (var kvp in zone.Web.GetAllContributions())
                {
                    if (total.ContainsKey(kvp.Key))
                        total[kvp.Key] += kvp.Value;
                    else
                        total[kvp.Key] = kvp.Value;
                }
            }

            return total;
        }

        internal static List<PlayerMobile> GetPlayersInDistrict(string districtName, Map map)
        {
            var list = new List<PlayerMobile>();

            if (string.IsNullOrEmpty(districtName) || map == null || map == Map.Internal)
                return list;

            TerritoryDistrict? district = TerritorySystem.FindDistrict(districtName);

            if (district == null)
                return list;

            System.Diagnostics.Debug.Assert(
                string.Equals(district.Name, districtName, StringComparison.OrdinalIgnoreCase));

            if (!map.Regions.TryGetValue(district.Name, out Region? region))
            {
                System.Diagnostics.Debug.Fail(
                    $"Region not found for district '{district.Name}'.");
                return list;
            }

            foreach (Mobile m in region.Mobiles.Values)
            {
                if (m is PlayerMobile pm && !pm.Deleted)
                    list.Add(pm);
            }

            return list;
        }
        public double PercentComplete()
        {
            int kills = 0;
            int killsNeeded = 0;

            foreach (TerritoryZone zone in Zones)
            {
                GuardianWeb? web = zone?.Web;

                if (web == null)
                    continue;

                foreach (var spawner in web.Spawners)
                {
                    if (spawner == null || spawner.Deleted)
                        continue;

                    kills += spawner.SpawnerKills;
                    killsNeeded += spawner.SpawnerKillsNeeded;
                }
            }

            if (killsNeeded <= 0)
                return 0.0;

            return Math.Min(100.0, kills * 100.0 / killsNeeded);
        }
        public override string ToString()
        {
            string status = IsConquered ? "[CONQUERED]" : "";
            return $"{Name} {status}";
        }
    }
}