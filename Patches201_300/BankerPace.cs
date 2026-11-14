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

/* Patches201_300/BankerPace.cs
 * ChangeLog
 *  11/10/26, Adam
 *      Rearm: every running banker spawner (any district, or none) respawns its bankers within seconds
 *      (Patcher.ScheduleRespawn), so the bankers standing in the world are replaced by bankers born armed (Banker: on
 *      Upheaval a Brigand-class fighter with a weapon in his backpack). PatchV269 runs it after the pace.
 *  11/9/26, Adam
 *      Created (PatchV269). The Gypsy Seer takes a town's bankers' heads (Upheaval: too few players for player heads
 *      alone); the bankers' respawn is the pace. In each territory district, every running spawner of bankers (Minter
 *      is one) respawns after (the district's running banker spawners) x Pace (1 hour). A spawner brings back one
 *      creature per delay, so every town yields at most one banker head per Pace, whatever its size; a town with more
 *      bankers only starts with more. Switched-off spawners are left alone, and so are banker spawners outside every
 *      district. A spawner that also spawns others is reported: they respawn at the bankers' pace too.
 */

using Server;
using Server.Engines.TerritoryControl;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Patches201_300
{
    public static class BankerPace
    {
        /// <summary>A town yields at most one banker head per Pace.</summary>
        public static readonly TimeSpan Pace = TimeSpan.FromHours(1);

        /// <summary>A spawner that keeps bankers (Minter is one).</summary>
        public static bool SpawnsBankers(Spawner s) => s.ObjectTypes.Any(t => typeof(Banker).IsAssignableFrom(t));

        /// <summary>The running banker spawners in this district, on its map.</summary>
        public static List<Spawner> RunningIn(TerritoryDistrict district) =>
            Spawner.Instances.Where(s => s != null && !s.Deleted && s.Running && s.Map == district.Map && SpawnsBankers(s)
                && GypsySeer.DistrictAt(s.Location, s.Map) == district).ToList();

        /// <summary>Every running banker spawner in the world (any district, or none).</summary>
        public static List<Spawner> Running() =>
            Spawner.Instances.Where(s => s != null && !s.Deleted && s.Running && s.Map != null && s.Map != Map.Internal && SpawnsBankers(s)).ToList();

        /// <summary>
        /// Replaces the bankers standing in the world with new ones, born armed: every running banker spawner respawns
        /// within seconds (Patcher.ScheduleRespawn: its creatures removed, a respawn 1 to 15 seconds later). Returns the
        /// spawners scheduled.
        /// </summary>
        public static int Rearm(List<string> lines)
        {
            List<Spawner> spawners = Running();
            foreach (Spawner s in spawners)
                Patcher.ScheduleRespawn(s);
            lines.Add(string.Format("{0} running banker spawner{1} respawning within 15 seconds: their bankers are born armed",
                spawners.Count, spawners.Count == 1 ? "" : "s"));
            return spawners.Count;
        }

        /// <summary>The respawn delay for a district with this many running banker spawners.</summary>
        public static TimeSpan DelayFor(int spawners) => TimeSpan.FromTicks(Pace.Ticks * Math.Max(1, spawners));

        /// <summary>
        /// Sets the pace district by district ('apply' false: only reports). Returns a line per district (and per mixed
        /// spawner, and for banker spawners outside every district); 'changed': the spawners set.
        /// </summary>
        public static List<string> Apply(bool apply, out int changed)
        {
            List<string> lines = new List<string>();
            changed = 0;

            foreach (TerritoryDistrict district in TerritorySystem.GetDistricts().OrderBy(d => d.Name))
            {
                List<Spawner> here = RunningIn(district);
                if (here.Count == 0)
                {
                    lines.Add(string.Format("{0} ({1}): no running banker spawners", district.Name, district.Map));
                    continue;
                }

                TimeSpan delay = DelayFor(here.Count);
                foreach (Spawner s in here)
                {
                    if (apply)
                    {
                        s.MaxDelay = delay;         // first: MinDelay raises MaxDelay (to twice) when it would pass it
                        s.MinDelay = delay;
                    }
                    changed++;
                    List<string> others = s.ObjectTypes.Where(t => !typeof(Banker).IsAssignableFrom(t)).Select(t => t.Name).ToList();
                    if (others.Count > 0)
                        lines.Add(string.Format("{0}: spawner 0x{1:X8} at {2} also spawns [{3}]: they respawn at the bankers' pace too",
                            district.Name, s.Serial.Value, s.Location, string.Join(",", others)));
                }
                lines.Add(string.Format("{0} ({1}): {2} running banker spawner{3} ({4} kept), respawn {5:0.#} h{6}", district.Name, district.Map,
                    here.Count, here.Count == 1 ? "" : "s", here.Sum(s => s.Count), delay.TotalHours, apply ? "" : " (not applied)"));
            }

            int outside = Spawner.Instances.Count(s => s != null && !s.Deleted && s.Running && s.Map != null && s.Map != Map.Internal
                && SpawnsBankers(s) && GypsySeer.DistrictAt(s.Location, s.Map) == null);
            if (outside > 0)
                lines.Add(string.Format("{0} running banker spawner{1} outside every district: left alone", outside, outside == 1 ? "" : "s"));
            return lines;
        }
    }
}
