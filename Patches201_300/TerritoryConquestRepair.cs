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

/* Patches201_300/TerritoryConquestRepair.cs
 * ChangeLog
 *  11/4/26, Adam
 *      Add RepairWarlordCounts (PatchV264). Before r1220 a recapture kept the district's Warlord count
 *      (MiniBossesSpawned), and PatchV257 repaired only the conquest flags: the new campaign started with the old
 *      campaign's count and spawned no earned Warlord (Upheaval 2026-11-04: Britain 45 spawned with 34 earned; Jhelom
 *      13 of 13, all earned by the new campaign, none received). A count above what the district has earned
 *      (EarnedMiniBosses) can only be carried over: it is set to the earned count, so the rest of the campaign spawns
 *      its Warlords as they are earned. Jhelom's count equals its earned count and is left (its players get the
 *      conquest bonus Warlord). The plan is written to TerritoryConquestRepair.log before anything changes; if it
 *      cannot be, nothing changes. With apply == false nothing changes (dry run).
 *  10/27/26, Adam
 *      Created. One-time repair of TerritoryControl conquest flags (PatchV257). Before the 10/27 fix an Orc recapture
 *      (TerritoryDistrict.RestartDistrict) reset the webs and spawners but left the zones conquered, and an already
 *      conquered zone swallowed its re-cleared web's notification: the district could never be liberated again
 *      (Upheaval 2026-10-27: Vesper and Skara Brae fully re-cleared but not conquered; Britain 60 and Jhelom 30 stale
 *      zones). The repair brings the flags into line with what has actually been won, silently (no announcement, bonus
 *      or Warlord) and touching no spawner, kill, contribution credit or ownership:
 *        - a web that is complete (every spawner present and deactivated) but not marked conquered is marked;
 *        - a zone conquered while its web is not (stale) is cleared (flag and time); a zone whose web is conquered and
 *          complete but which is not is marked (now);
 *        - a district whose webs are all complete but which is not conquered is marked.
 *      Never: a damaged web (a missing or deleted spawner) is complete (reported as a configuration problem); a district
 *      with no web is conquered; a conquered district is un-conquered (an inconsistency is reported and left). Missed
 *      rewards are reported, not paid. The plan is written to TerritoryConquestRepair.log before anything changes; if it
 *      cannot be written nothing changes (Result.Failed). After an apply the repair is evaluated again and must find
 *      nothing left to change (a second run changes nothing). With apply == false nothing changes (dry run).
 */

using Server;
using Server.Diagnostics;
using Server.Engines.TerritoryControl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Patches201_300
{
    public static class TerritoryConquestRepair
    {
        public sealed class Result
        {
            public bool Applied;
            public bool Failed;                                         // the plan could not be recorded, or an apply left changes behind
            public int WebsMarked, ZonesMarked, ZonesCleared, DistrictsMarked;  // changes (made, or that a dry run would make)
            public int DamagedWebs, WeblessDistricts, Inconsistent;    // reported only
            public int Remaining = -1;                                  // after an apply: changes a second evaluation still finds (must be 0)
            public int Changes => WebsMarked + ZonesMarked + ZonesCleared + DistrictsMarked;
            public readonly List<string> Lines = new();

            public string Summary => string.Format(
                "Territory conquest repair {0}: {1} change(s): webs marked {2}, zones marked {3}, stale zones cleared {4}, districts marked {5}; " +
                "damaged webs {6}, districts without webs {7}, inconsistencies left {8}{9}.",
                Failed ? "FAILED" : Applied ? "applied" : "dry run", Changes, WebsMarked, ZonesMarked, ZonesCleared, DistrictsMarked,
                DamagedWebs, WeblessDistricts, Inconsistent, Remaining < 0 ? "" : string.Format("; a second evaluation finds {0}", Remaining));
        }

        private const string LogFile = "TerritoryConquestRepair.log";

        /// <summary>
        /// Plans the repair and, with apply == true, makes it. The plan is recorded (TerritoryConquestRepair.log) before
        /// anything changes; if it cannot be, nothing changes (Result.Failed). With a scope only those districts are
        /// evaluated and nothing is logged (tests). Used by PatchV257 (no scope).
        /// </summary>
        public static Result Run(bool apply, string caller, IEnumerable<TerritoryDistrict>? scope = null)
        {
            List<TerritoryDistrict> districts = (scope ?? TerritorySystem.GetDistricts()).Where(d => d != null).OrderBy(d => d.Name).ToList();
            bool log = scope == null;

            Result plan = Evaluate(apply: false, districts);
            if (log && !Record(caller, "plan", plan))
            {
                plan.Failed = true;                                     // the plan is not on record: change nothing
                return plan;
            }
            if (!apply)
                return plan;

            Result applied = Evaluate(apply: true, districts);
            applied.Remaining = Evaluate(apply: false, districts).Changes;
            applied.Failed = applied.Remaining != 0;
            if (log)
                Record(caller, "applied", applied);
            return applied;
        }

        private static Result Evaluate(bool apply, List<TerritoryDistrict> districts)
        {
            Result result = new Result { Applied = apply };
            string act = apply ? "" : "would ";

            foreach (TerritoryDistrict district in districts)
            {
                List<string> lines = new List<string>();
                List<TerritoryZone> webZones = district.Zones.Where(z => z?.Web != null).OrderBy(z => z.ZoneID).ToList();

                if (webZones.Count == 0)
                {
                    result.WeblessDistricts++;
                    lines.Add("  no GuardianWeb: never conquered by this repair");
                }

                foreach (TerritoryZone zone in webZones)
                {
                    GuardianWeb web = zone.Web;
                    int missing = web.Spawners.Count(s => s == null || s.Deleted);
                    bool damaged = web.Spawners.Count == 0 || missing > 0;
                    bool complete = !damaged && web.IsComplete;

                    if (damaged)
                    {
                        result.DamagedWebs++;
                        lines.Add(string.Format("  zone {0}: damaged web ({1} spawners, {2} missing or deleted): a configuration problem, never complete",
                            zone.ZoneID, web.Spawners.Count, missing));
                    }

                    // a complete web not marked conquered (its last kill's notification never ran)
                    bool webConquered = web.IsConquered;
                    if (!webConquered && complete)
                    {
                        result.WebsMarked++;
                        Dictionary<Serial, int> credit = web.GetAllContributions();
                        lines.Add(string.Format("  zone {0}: {1}mark its complete web conquered (missed web bonus, not paid: {2} contributors, {3} damage credit, bonus 50%)",
                            zone.ZoneID, act, credit.Count, credit.Values.Sum()));
                        if (apply)
                            web.IsConquered = true;
                        webConquered = true;
                    }
                    else if (webConquered && !complete)
                    {
                        result.Inconsistent++;
                        lines.Add(string.Format("  zone {0}: web marked conquered but not complete: left as it is", zone.ZoneID));
                    }

                    // the zone follows its web
                    if (zone.IsConquered && !webConquered)
                    {
                        result.ZonesCleared++;
                        lines.Add(string.Format("  zone {0}: {1}clear the stale conquest (conquered {2:u}; its web is being fought for again)",
                            zone.ZoneID, act, zone.ConqueredAt));
                        if (apply)
                        {
                            zone.IsConquered = false;
                            zone.ConqueredAt = DateTime.MinValue;
                        }
                    }
                    else if (!zone.IsConquered && webConquered && complete)
                    {
                        result.ZonesMarked++;
                        lines.Add(string.Format("  zone {0}: {1}mark conquered (its web is)", zone.ZoneID, act));
                        if (apply)
                        {
                            zone.IsConquered = true;
                            zone.ConqueredAt = DateTime.UtcNow;
                        }
                    }
                }

                // the district: at least one web, every web complete (TerritoryDistrict.IsComplete; damaged webs never are)
                bool districtComplete = webZones.Count > 0 && district.IsComplete;
                if (!district.IsConquered && districtComplete)
                {
                    result.DistrictsMarked++;
                    Dictionary<Serial, int> credit = district.GetAllContributions();
                    lines.Add(string.Format("  {0}mark the district conquered (silent)", act));
                    lines.Add(string.Format("  missed district bonus, not paid: {0} contributors, {1} damage credit, bonus 100% (an upper bound: credit kept from an earlier campaign may already have been rewarded)",
                        credit.Count, credit.Values.Sum()));
                    if (apply)
                        district.IsConquered = true;
                }
                else if (district.IsConquered && !districtComplete)
                {
                    result.Inconsistent++;
                    lines.Add("  district marked conquered but not complete: left as it is (an accomplishment is never undone)");
                }

                result.Lines.Add(string.Format("{0}: {1}", district.Name, lines.Count == 0 ? "no change" : ""));
                result.Lines.AddRange(lines);
            }

            return result;
        }

        /// <summary>
        /// A Warlord count carried over from an earlier campaign (MiniBossesSpawned above EarnedMiniBosses) is set to
        /// the earned count (PatchV264). Returns the number of districts changed (or that a dry run would change), -1 if
        /// the plan could not be recorded (nothing changed). With a scope only those districts are evaluated and nothing
        /// is logged (tests). The changes are listed in lines.
        /// </summary>
        public static int RepairWarlordCounts(bool apply, string caller, List<string> lines, IEnumerable<TerritoryDistrict>? scope = null)
        {
            List<TerritoryDistrict> districts = (scope ?? TerritorySystem.GetDistricts()).Where(d => d != null).OrderBy(d => d.Name).ToList();
            List<TerritoryDistrict> carried = districts.Where(d => d.MiniBossesSpawned > d.EarnedMiniBosses).ToList();
            string act = apply ? "set" : "would set";

            foreach (TerritoryDistrict d in carried)
                lines.Add(string.Format("{0}: Warlords spawned {1} of {2} earned ({3} max, conquered {4}): {5} to {2} (carried over from an earlier campaign)",
                    d.Name, d.MiniBossesSpawned, d.EarnedMiniBosses, d.MaxMiniBosses, d.IsConquered, act));

            if (scope == null)
            {
                Result plan = new Result();
                plan.Lines.AddRange(lines);
                if (!Record(caller, apply ? "Warlord counts" : "Warlord counts (dry run)", plan,
                    string.Format("{0} carried-over Warlord count(s) of {1} districts", carried.Count, districts.Count)))
                    return -1;                                          // the plan is not on record: change nothing
            }

            if (apply)
                foreach (TerritoryDistrict d in carried)
                    d.MiniBossesSpawned = d.EarnedMiniBosses;

            return carried.Count;
        }

        // appends the run to TerritoryConquestRepair.log; false if it could not be written
        private static bool Record(string caller, string stage, Result result, string? summary = null)
        {
            try
            {
                string path = Path.Combine(Core.LogsDirectory, LogFile);
                Utility.EnsurePath(path);
                List<string> text = new List<string> { string.Format("{0:u} {1} {2}: {3}", DateTime.UtcNow, caller, stage, summary ?? result.Summary) };
                text.AddRange(result.Lines);
                text.Add("");
                File.AppendAllLines(path, text);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                return false;
            }
        }
    }
}
