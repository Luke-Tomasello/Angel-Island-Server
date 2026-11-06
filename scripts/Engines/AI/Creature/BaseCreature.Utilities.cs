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

/* Scripts\Engines\AI\Creature\BaseCreature.Utilities.cs
 * ChangeLog
 *  3/29/2026, Adam
 *      Created.
 */

using Server.Engines.DataRecorder;
using Server.Engines.PartySystem;
using Server.Items;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Mobiles
{
    public partial class BaseCreature
    {
        #region Death Rewards System

        /// <summary>
        /// Death Rewards System - AwardDeathRewards and supporting methods
        ///
        /// Purpose:
        /// This family of functions has exactly two responsibilities when a creature dies:
        ///
        /// 1. Award PvM Points
        ///    - Based on each player's damage contribution (clipped and party-divided).
        ///    - Used for general PvM leaderboards and progression.
        ///
        /// 2. Award LootGold
        ///    - Distributes the creature's PackedGold among players who earned looting rights,
        ///      based on their percentage of total damage.
        ///
        /// All other logic (damage clipping, party handling, champion detection, township 
        /// subsidies) exists only to support these two core outcomes.
        ///
        /// Design Notes:
        /// - DataRecorder is responsible only for recording final values.
        /// - BaseCreature owns the decomposition of the death event.
        /// - This separation keeps concerns clean and makes the system easier to maintain.
        ///
        /// Callsite should remain simple:
        ///     this.AwardDeathRewards(SpawnerHandle);
        ///
        /// </summary>
        /// <summary>
        /// Death Rewards System - AwardDeathRewards and supporting methods
        ///
        /// Purpose:
        /// This family of functions has exactly two responsibilities when a creature dies:
        ///
        /// 1. Award PvM Points
        ///    - Based on each player's damage contribution (clipped and party-divided).
        ///    - Used for general PvM leaderboards and progression.
        ///
        /// 2. Award LootGold
        ///    - Distributes the creature's PackedGold among players who earned looting rights,
        ///      based on their percentage of total damage.
        ///
        /// All other logic (damage clipping, party handling, champion detection, township 
        /// subsidies) exists only to support these two core outcomes.
        ///
        /// Callsite example:
        ///     List<DamageStore> list = GetLootingRights(this.DamageEntries, this.HitsMax);
        ///     if (list.Count > 0)
        ///         this.AwardDeathRewards(list, SpawnerHandle);
        ///
        /// </summary>
        public virtual void OnDeathLeaderBoards(List<DamageStore> list)
        {
            if (list.Count > 0)
            {
                AwardDeathRewards(
                    list,
                    awardTownship: false,
                    RecorderDomain.PvMPoints);
            }
        }
        //public void AwardDeathRewards(List<DamageStore> players, object hSpawner = null)
        //{
        //    ChampEngine ce = hSpawner as ChampEngine;

        //    bool awardTownship = false;

        //    RecorderDomain[] domains = new RecorderDomain[]
        //    {
        //        RecorderDomain.PvMPoints
        //    };

        //    if (ce != null && ce.KillDatabase)
        //    {
        //        awardTownship = true;

        //        if (ce.SpawnType == ChampLevelData.SpawnTypes.AI_Escape ||
        //            ce.SpawnType == ChampLevelData.SpawnTypes.AI_Guard)
        //        {
        //            domains = new RecorderDomain[]
        //            {
        //                RecorderDomain.PvMPoints,
        //                RecorderDomain.PQChamps
        //            };
        //        }
        //    }

        //    AwardDeathRewards(players, awardTownship, domains);
        //}

        public void AwardDeathRewards(List<DamageStore> players, bool awardTownship, params RecorderDomain[] domains)
        {
            if (players == null || players.Count == 0)
                return;

            if (HasAnyBeneficialHealing())
                return;

            if (domains == null || domains.Length == 0)
                domains = new RecorderDomain[] { RecorderDomain.PvMPoints };

            SortedList<Mobile, int> damageResults = ProcessDamageStore(players);

            ClipDamageStore(ref damageResults, this);

            foreach (var kvp in damageResults)
            {
                Mobile player = kvp.Key;
                int points = kvp.Value;

                if (player == null || player.Deleted)
                    continue;

                foreach (RecorderDomain domain in domains)
                    DataRecorder.Add(player, domain, points);

                if (awardTownship)
                    UpdateTownshipForPlayer(player, points);
            }

            CoalesceGoldEarned(players);
        }
        #region Death Reward Helpers

        private bool HasAnyBeneficialHealing()
        {
            if (BeneficialEntries == null || BeneficialEntries.Count == 0)
                return false;

            foreach (BeneficialEntry entry in BeneficialEntries)
            {
                if (entry == null || entry.HasExpired)
                    continue;

                if (entry.Benefactor is PlayerMobile)
                    return true;
            }

            return false;
        }

        public static List<DamageStore> GetLootingRights(List<DamageEntry> damageEntries, int hitsMax)
        {
            List<DamageStore> rights = new List<DamageStore>();

            for (int i = damageEntries.Count - 1; i >= 0; --i)
            {
                if (i >= damageEntries.Count)
                    continue;

                DamageEntry de = damageEntries[i];

                if (de.HasExpired)
                {
                    damageEntries.RemoveAt(i);
                    continue;
                }

                int damage = de.DamageGiven;

                List<DamageEntry> respList = de.Responsible;

                if (respList != null)
                {
                    for (int j = 0; j < respList.Count; ++j)
                    {
                        DamageEntry subEntry = respList[j];
                        Mobile master = subEntry.Damager;

                        if (master == null || master.Deleted || !master.Player)
                            continue;

                        bool needNewSubEntry = true;

                        for (int k = 0; needNewSubEntry && k < rights.Count; ++k)
                        {
                            DamageStore ds = rights[k];

                            if (ds.m_Mobile == master)
                            {
                                ds.m_Damage += subEntry.DamageGiven;
                                needNewSubEntry = false;
                            }
                        }

                        if (needNewSubEntry)
                            rights.Add(new DamageStore(master, subEntry.DamageGiven));

                        damage -= subEntry.DamageGiven;
                    }
                }

                Mobile m = de.Damager;

                if (m == null || m.Deleted || !m.Player)
                    continue;

                if (damage <= 0)
                    continue;

                bool needNewEntry = true;

                for (int j = 0; needNewEntry && j < rights.Count; ++j)
                {
                    DamageStore ds = rights[j];

                    if (ds.m_Mobile == m)
                    {
                        ds.m_Damage += damage;
                        needNewEntry = false;
                    }
                }

                if (needNewEntry)
                    rights.Add(new DamageStore(m, damage));
            }

            if (rights.Count > 0)
            {
                rights[0].m_Damage = (int)(rights[0].m_Damage * 1.25);  //This would be the first valid person attacking it.  Gets a 25% bonus.  Per 1/19/07 Five on Friday

                if (rights.Count > 1)
                    rights.Sort(); //Sort by damage

                int topDamage = rights[0].m_Damage;
                int minDamage;

                if (hitsMax >= 3000)
                    minDamage = topDamage / 16;
                else if (hitsMax >= 1000)
                    minDamage = topDamage / 8;
                else if (hitsMax >= 200)
                    minDamage = topDamage / 4;
                else
                    minDamage = topDamage / 2;

                for (int i = 0; i < rights.Count; ++i)
                {
                    DamageStore ds = rights[i];

                    ds.m_HasRight = (ds.m_Damage >= minDamage);
                }
            }

            return rights;
        }
        public static List<PlayerMobile> GetQualifiedParticipants(List<DamageEntry> damageEntries, int hitsMax)
        {
            List<PlayerMobile> players = new();

            foreach (DamageStore ds in GetLootingRights(damageEntries, hitsMax))
            {
                if (ds.m_HasRight && ds.m_Mobile is PlayerMobile pm)
                    players.Add(pm);
            }

            return players;
        }

        // Your existing ProcessDamageStore (with the fixed party logic)
        public static SortedList<Mobile, int> ProcessDamageStore(List<DamageStore> list)
        {
            SortedList<Mobile, int> results = new SortedList<Mobile, int>(
                Comparer<Mobile>.Create((x, y) => y.CompareTo(x)));

            foreach (DamageStore ds in list)
            {
                if (!ds.m_HasRight || ds.m_Mobile == null)
                    continue;

                Party? party = Engines.PartySystem.Party.Get(ds.m_Mobile);

                if (party != null)
                {
                    int dividedDamage = ds.m_Damage / party.Members.Count;

                    foreach (PartyMemberInfo info in party.Members)
                    {
                        if (info?.Mobile != null)
                        {
                            if (results.ContainsKey(info.Mobile))
                                results[info.Mobile] += dividedDamage;
                            else
                                results[info.Mobile] = dividedDamage;
                        }
                    }
                }
                else
                {
                    if (results.ContainsKey(ds.m_Mobile))
                        results[ds.m_Mobile] += ds.m_Damage;
                    else
                        results[ds.m_Mobile] = ds.m_Damage;
                }
            }

            return results;
        }

        // Your ClipDamageStore (clean version)
        public static void ClipDamageStore(ref SortedList<Mobile, int> results, Mobile beast)
        {
            if (beast == null || results == null || results.Count == 0)
                return;

            Dictionary<Mobile, int> writable = new Dictionary<Mobile, int>(results);

            foreach (var key in writable.Keys.ToList())
            {
                writable[key] = Math.Min(writable[key], beast.HitsMax);
            }

            int totalDamage = writable.Values.Sum();
            int delta = totalDamage - beast.HitsMax;

            if (delta <= 0)
                return;

            while (delta > 0)
            {
                bool reduced = false;
                foreach (var kvp in writable.OrderByDescending(k => k.Value))
                {
                    if (writable[kvp.Key] > 0)
                    {
                        writable[kvp.Key]--;
                        delta--;
                        reduced = true;
                        if (delta <= 0)
                            break;
                    }
                }
                if (!reduced)
                    break;
            }

            results = new SortedList<Mobile, int>(
                writable,
                Comparer<Mobile>.Create((x, y) => y.CompareTo(x))
            );
        }

        private void CoalesceGoldEarned(List<DamageStore> players)
        {
            if (this.GoldPacked <= 0 || players == null || players.Count == 0)
                return;

            Dictionary<Mobile, double> goldShare = PercentOfDrop(this, players);

            foreach (var kvp in goldShare)
            {
                if (kvp.Value > 0)
                    DataRecorder.Add(kvp.Key, RecorderDomain.PvMLootGold, kvp.Value);
            }
        }

        private static Dictionary<Mobile, double> PercentOfDrop(BaseCreature beast, List<DamageStore> players)
        {
            var goldSubTotal = new Dictionary<Mobile, double>();

            if (beast.GoldPacked <= 0)
                return goldSubTotal;

            double totalDamage = 0;
            foreach (var ds in players)
                totalDamage += ds.m_Damage;

            if (totalDamage <= 0)
                return goldSubTotal;

            foreach (var ds in players)
            {
                if (ds.m_Mobile == null || !ds.m_HasRight)
                    continue;

                double percentage = ds.m_Damage / totalDamage;
                double goldEarned = percentage * beast.GoldPacked;
                goldSubTotal[ds.m_Mobile] = goldEarned;
            }

            return goldSubTotal;
        }

        private void UpdateTownshipForPlayer(Mobile player, int points)
        {
            if (TownshipStone.GetPlayerTownship(player) is TownshipStone ts)
            {
                ts.LBTaxSubsidy += points;
                ts.LBFameSubsidy += points;
            }
        }

        #endregion Death Reward Helpers

        #endregion Death Rewards System
    }
}
