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

/* Scripts\Engines\TerritoryControl\GuardianWeb.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See TerritorySystem for an architectural overview
 */

using Server.Engines.DataRecorder;
using System.Collections.Generic;
using System.Linq;

namespace Server.Engines.TerritoryControl
{
    public class GuardianWeb
    {
        public TerritoryDistrict District { get; private set; }
        public TerritoryZone? Zone { get; private set; }        // ← Added

        public List<ContestedSpawner> Spawners { get; } = new();
        public int WebKillsNeeded
        {
            get
            {
                return Spawners
                    .Where(s => s != null && !s.Deleted)
                    .Sum(s => s.SpawnerKillsNeeded);
            }
        }
        public int TotalKills
        {
            get
            {
                return Spawners
                    .Where(s => s != null && !s.Deleted)
                    .Sum(s => s.SpawnerKills);
            }
        }

        public bool IsComplete
        {
            get
            {
                return Spawners.Count > 0 &&
                       Spawners.All(s => s != null &&
                                         !s.Deleted &&
                                         s.Deactivated);
            }
        }
        public bool IsConquered { get; set; }
        public void SetDistrict(TerritoryDistrict district)
        {
            District = district;
        }

        public void SetZone(TerritoryZone zone)
        {
            Zone = zone;
            if (zone != null)
            {
                zone.Web = this;
                // Ensure ZoneID is set
                System.Diagnostics.Debug.Assert(zone.ZoneID != 0);
                // no fallbacks - hides bug
                // zone.ZoneID = TerritoryZone.GenerateZoneID(zone.Bounds); // fallback
            }
        }

        public void AddSpawner(ContestedSpawner spawner)
        {
            if (spawner != null)
            {
                Spawners.Add(spawner);
                spawner.Web = this;
            }
        }

        ///// <summary>
        ///// Symmetric reversal - removes kills when a spawner times out
        ///// </summary>
        //public void ReverseKill(ContestedSpawner spawner, int killsToReverse)
        //{
        //    if (killsToReverse > 0)
        //    {
        //        TotalKills = Math.Max(0, TotalKills - killsToReverse);
        //    }
        //}

        /// <summary>
        /// Called on EVERY kill from any spawner in this web
        /// </summary>
        public void NotifyKill(ContestedSpawner spawner)
        {
            CheckForCompletion(spawner);
        }

        /// <summary>
        /// Checks whether this web (or the calling spawner) has reached completion.
        /// </summary>
        private void CheckForCompletion(ContestedSpawner completedSpawner)
        {
            if (IsConquered)
                return;

            if (IsComplete)
                OnWebCleared();
        }

        /// <summary>
        /// Called when the entire web has been cleared.
        /// This is the single place where full web completion logic lives.
        /// </summary>
        private void OnWebCleared()
        {
            if (IsConquered)
                return;

            IsConquered = true;

            foreach (var spawner in Spawners)
            {
                if (spawner != null && !spawner.Deleted)
                {
                    spawner.Deactivated = true;
                    spawner.FinalizeContributions();
                }
            }

            AwardWebBonus();

            SpawnMiniChampion(); // Khartag the unstopable!

            Zone?.NotifyWebCompleted();
        }

        private void SpawnMiniChampion()
        {

        }
        public void AwardWebBonus()
        {
            var totalContributions = GetAllContributions();   // aggregates from all spawners
            if (totalContributions.Count == 0)
                return;

            double totalDamage = totalContributions.Values.Sum();
            double bonus = (totalDamage * 0.50);   // 50%

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
        /// Aggregates all player damage contributions across every spawner in this web.
        /// </summary>
        public Dictionary<Serial, int> GetAllContributions()
        {
            var total = new Dictionary<Serial, int>();

            foreach (var spawner in Spawners)
            {
                if (spawner == null || !spawner.HasContributions)
                    continue;

                foreach (var kvp in spawner.GetContributionData())
                {
                    if (total.ContainsKey(kvp.Key))
                        total[kvp.Key] += kvp.Value;   // accumulate damage
                    else
                        total[kvp.Key] = kvp.Value;
                }
            }

            return total;
        }
        public void DeleteAllSpawners()
        {
            foreach (var spawner in Spawners)
            {
                if (spawner != null && !spawner.Deleted)
                    spawner.Delete();
            }
            Spawners.Clear();
        }
    }
}