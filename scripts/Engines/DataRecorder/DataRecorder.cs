/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
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

/* Scripts\Engines\DataRecorder\DataRecorder.cs
 * CHANGELOG:
 *  1/27/22, Adam (RecordMusicSales)
 *      Record data for two new leader boards for MusicSales
 *          a. total gold earned from sales
 *          b. total number of tracks sold
 *  11/6/21, Adam
 *      Check for null mobile before adding back to in-memory database
 *  10/25/21, Adam
 *      Add support for BOD Leaderboard
 *	8/17/21, Adam
 *		first time check in
 */

using Server.Engines.ChampionSpawn;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Engines.DataRecorder
{
    public static class DataRecorder
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }
        #region Prison Quest Points
        private static Dictionary<Mobile, int[]> m_PQuestPoints = new Dictionary<Mobile, int[]>();
        public static Dictionary<Mobile, int[]> GetPQuestPoints { get { return m_PQuestPoints; } }
        public static void RecordPQuestPoints(Mobile m, int escapes, int rares, int sterling)
        {
            if (m_PQuestPoints.ContainsKey(m))
            {
                m_PQuestPoints[m][0] += escapes;
                m_PQuestPoints[m][1] += rares;
                m_PQuestPoints[m][2] += sterling;
            }
            else
                m_PQuestPoints.Add(m, new int[] { escapes, rares, sterling });
        }
        #endregion Prison Quest Points
        #region Music Sales
        private static Dictionary<Mobile, int[]> m_MusicSales = new Dictionary<Mobile, int[]>();
        public static Dictionary<Mobile, int[]> GetMusicSales { get { return m_MusicSales; } }
        public static void RecordMusicSales(Mobile m, int price, int sold)
        {
            if (m_MusicSales.ContainsKey(m))
            {
                m_MusicSales[m][0] += price;
                m_MusicSales[m][1] += sold;
            }
            else
                m_MusicSales.Add(m, new int[] { price, sold });
        }
        #endregion Music Sales
        #region BOD Points
        private static Dictionary<Mobile, int> m_BODPoints = new Dictionary<Mobile, int>();
        public static Dictionary<Mobile, int> GetBODPoints { get { return m_BODPoints; } }
        private static Dictionary<Mobile, int> m_BODGold = new Dictionary<Mobile, int>();
        public static Dictionary<Mobile, int> GetBODGold { get { return m_BODGold; } }
        public static void RecordBODPoints(Mobile m, int points)
        {   // called from HandleBulkOrderDropped - BulkOrderSystem
            if (m_BODPoints.ContainsKey(m))
                m_BODPoints[m] += points;
            else
                m_BODPoints.Add(m, points);
        }
        public static void RecordBODGold(Mobile m, int gold)
        {   // called from HandleBulkOrderDropped - BulkOrderSystem
            if (m_BODGold.ContainsKey(m))
                m_BODGold[m] += gold;
            else
                m_BODGold.Add(m, gold);
        }
        #endregion BOD Points
        #region PackGold
        //private static Dictionary<Mobile, int> m_GoldDrop = new Dictionary<Mobile, int>();              // beast, amount (not serialized)
        /*public static void PackGold(Mobile beast, int amount)
        {
            m_GoldDrop.Add(beast, amount);
        }*/
        #endregion PackGold
        #region CoalesceGoldEarned
        private static Dictionary<Mobile, double> m_GoldEarned = new Dictionary<Mobile, double>();      // player, total (serialized)
        public static void LockPick(Mobile from, Item container)
        {
            LockableContainer chest = container as LockableContainer;
            if (chest == null || chest.Items == null || chest.Items.Count == 0)
                return;
            if (Server.Multis.BaseHouse.FindHouseAt(chest) != null)
                return;
            if (chest.Movable == true)
                return;
            foreach (Item item in chest.Items)
            {
                if (item is Gold)
                {
                    if (m_GoldEarned.ContainsKey(from))
                    {
                        m_GoldEarned[from] += (item as Gold).Amount;
                    }
                    else
                    {
                        m_GoldEarned.Add(from, (item as Gold).Amount);
                    }
                }
            }
        }
        public static List<KeyValuePair<Mobile, double>> GoldEarned
        {
            get
            {
                List<KeyValuePair<Mobile, double>> new_list = new List<KeyValuePair<Mobile, double>>();
                foreach (KeyValuePair<Mobile, double> kvp in m_GoldEarned)
                    new_list.Add(kvp);

                new_list.Sort((e1, e2) =>
                {
                    return e2.Value.CompareTo(e1.Value);
                });
                return new_list;
            }
        }
        public static void CoalesceGoldEarned(Mobile beast, List<DamageStore> players)
        {
            if (players != null && players.Count > 0)
            {   // we now have the players responsible for this beast's death
                // find out how much of the drop-points they are entitled to
                Dictionary<Mobile, double> goldEarnedSubTotal = PercentOfDrop(beast, players);
                // okay, now attribute those points to the players
                foreach (KeyValuePair<Mobile, double> mob_percent in goldEarnedSubTotal)
                {
                    if (m_GoldEarned.ContainsKey(mob_percent.Key))
                    {
                        m_GoldEarned[mob_percent.Key] += mob_percent.Value;
                    }
                    else
                    {
                        m_GoldEarned.Add(mob_percent.Key, mob_percent.Value);
                    }
                }
            }
        }
        private static Dictionary<Mobile, double> PercentOfDrop(Mobile beast, List<DamageStore> players)
        {   // we now have the damage stores for this beast's death
            // add up all the damage to determine the beast's damage points.
            // Note: Damage points can exceed HP of the creature due to 'healing' 
            // Then determine our percentage of the damage. That's the credited points
            //  for each player.
            Dictionary<Mobile, double> goldEarnedSubTotal = new Dictionary<Mobile, double>();    // player, subtotal

            //  calc damage points
            double beastDamagePoints = 0;
            foreach (DamageStore ds in players)
                beastDamagePoints += ds.m_Damage;

            // assign each participent a percentage of loot-points/damage-points 
            foreach (DamageStore ds in players)
            {
                double prct = (double)ds.m_Damage / beastDamagePoints;
                double loot = prct * (double)((beast as BaseCreature).PackedGold);
                goldEarnedSubTotal.Add(ds.m_Mobile, loot);
            }

            return goldEarnedSubTotal;
        }
        #endregion CoalesceGoldEarned
        #region PvM Rankings
        // pseudo dictionary that allows null keys
        private static List<KeyValuePair<Mobile, int>> m_GeneralPvMRankings = new List<KeyValuePair<Mobile, int>>();
        public static List<KeyValuePair<Mobile, int>> GeneralPvMRankings { get { return m_GeneralPvMRankings; } }
        private static List<KeyValuePair<Mobile, int>> m_ChampPvMRankings = new List<KeyValuePair<Mobile, int>>();
        public static List<KeyValuePair<Mobile, int>> ChampPvMRankings { get { return m_ChampPvMRankings; } }
        public static List<KeyValuePair<Mobile, int>> TopPvMers(List<KeyValuePair<Mobile, int>> database)
        {   // called by Cron to populate the LeaderBoard

            if (database.Count == 0)
                return null;
            List<KeyValuePair<Mobile, int>> list = new List<KeyValuePair<Mobile, int>>();
            foreach (KeyValuePair<Mobile, int> kvp in database)
                list.Add(kvp);

            list.Sort((e1, e2) =>
            {
                return e2.Value.CompareTo(e1.Value);
            });

            return list;
        }

        public static void PvMStats(Mobile beast, List<DamageStore> list, object hSpawner)
        {   // Called in BaseCreature when the creature is killed
            // list is already Sorted by damage

            // divvy up points between party members
            SortedList<Mobile, int> Results = BaseCreature.ProcessDamageStore(list);

            // anti-cheezing: Players have found that healing a monster (or allowing it to heal itself,) can yield unlimited damage points.
            //  We therefore limit the damage points to no more than the creature's HitsMax
            BaseCreature.ClipDamageStore(ref Results, beast);

            // update rankings dictionary, add new players
            foreach (var kvp in Results)
            {
                if (!Core.Debug && !Core.UOTC_CFG)
                    if (kvp.Key.AccessLevel > AccessLevel.Player)
                        continue;

                // update player damage totals
                if (hSpawner is ChampEngine ce && ce.KillDatabase)  // this champ engine is registered for separate kill accounting
                {
                    UpdateStats(kvp, m_ChampPvMRankings);
                    UpdateTownshipForPlayer(kvp);
                }

                // general accounting always applies
                UpdateStats(kvp, m_GeneralPvMRankings);              // general kill accounting
            }

            // update Gold Farmed for this player
            //  We do this by calling CoalesceGoldFarmed with only the damage to *this* beast
            //  CoalesceGoldFarmed will then calc how much gold should be attributed to this player
            CoalesceGoldEarned(beast, list);
        }
        private static void UpdateTownshipForPlayer(KeyValuePair<Mobile, int> kvp)
        {
            if (TownshipStone.GetPlayerTownship(kvp.Key) is TownshipStone ts)
            {
                ts.LBTaxSubsidy += kvp.Value;
                ts.LBFameSubsidy += kvp.Value;
            }
        }
        private static void UpdateStats(KeyValuePair<Mobile, int> kvp, List<KeyValuePair<Mobile, int>> database)
        {
            int index = 0;
            bool found = FindIndex(kvp.Key, out index, database);    // is this player in our database?

            if (found == true)                 // do they have a right to these points?
            {
                // update damage points for this player
                KeyValuePair<Mobile, int> kvp_old = database[index];
                KeyValuePair<Mobile, int> kvp_new = new KeyValuePair<Mobile, int>(kvp.Key, kvp_old.Value + kvp.Value);
                database.RemoveAt(index);
                database.Add(kvp_new);
            }
            else
            {   // add this new player to our database
                KeyValuePair<Mobile, int> new_record = new KeyValuePair<Mobile, int>(kvp.Key, kvp.Value);
                database.Add(new_record);
            }
        }
        private static bool FindIndex(Mobile m, out int index, List<KeyValuePair<Mobile, int>> database)
        {
            bool found = false;
            index = 0;
            foreach (KeyValuePair<Mobile, int> kvp in database)
            {
                if (kvp.Key == m)          // got the right player?
                {
                    found = true;               // got it, we will update totals now
                    break;
                }
                index++;                        // at this index
            }

            return found;
        }

        #endregion PvM Rankings
        #region Purchase
        public static void GoldSink(Mobile m, object obj, int price)
        {
            if (!Core.Debug && !Core.UOTC_CFG)
                if (m != null && m.AccessLevel > AccessLevel.Player)
                    return;

            if (obj is Item item)
            {
                // TODO: add player vendor upkeep
                // Rental Vendor upkeep
                Type type = item.GetType();
                if (
                    type.FullName.ToLower().Contains("deed") ||         // all deed types, filter below
                    type.FullName.ToLower().Contains("library") ||      // 
                    item is TownshipStone                               // upkeep/fees
                    )
                {
                    // toss out tents and classic 7x7 houses
                    ;
                }
            }
            else if (obj is Mobile mobile)
            {   // player vendors (daily charges)
                ;
            }
        }
        #endregion Purchase
        #region Serialization
        public static void Load()
        {
            if (!File.Exists("Saves/DataRecorder.bin"))
                return;

            Console.WriteLine("Data Recorder Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/DataRecorder.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 9: goto case 8;    // update prison quest data
                    case 8:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Mobile m = reader.ReadMobile();
                                int damage = reader.ReadInt();
                                KeyValuePair<Mobile, int> kvp = new KeyValuePair<Mobile, int>(m, damage);
                                if (m != null)
                                    m_ChampPvMRankings.Add(kvp);
                            }
                            goto case 7;
                        }
                    case 7:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Mobile m = reader.ReadMobile();
                                int gold = reader.ReadInt();
                                if (m != null)
                                    m_BODGold.Add(m, gold);
                            }
                            goto case 6;
                        }
                    case 6:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Mobile m = reader.ReadMobile();
                                int escapes = reader.ReadInt();
                                int rares = reader.ReadInt();
                                int sterling = 0;
                                if (version >= 9)
                                    sterling = reader.ReadInt();

                                if (m != null)
                                    m_PQuestPoints.Add(m, new int[] { escapes, rares, sterling });
                            }
                            goto case 5;
                        }
                    case 5:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Mobile m = reader.ReadMobile();
                                int price = reader.ReadInt();
                                int sold = reader.ReadInt();
                                if (m != null)
                                    m_MusicSales.Add(m, new int[] { price, sold });
                            }
                            goto case 4;
                        }
                    case 4:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Mobile m = reader.ReadMobile();
                                int points = reader.ReadInt();
                                if (m != null)
                                    m_BODPoints.Add(m, points);
                            }
                            goto case 3;
                        }
                    case 3:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Mobile m = reader.ReadMobile();
                                int damage = reader.ReadInt();
                                KeyValuePair<Mobile, int> kvp = new KeyValuePair<Mobile, int>(m, damage);
                                if (m != null)
                                    m_GeneralPvMRankings.Add(kvp);
                            }

                            count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Mobile m = reader.ReadMobile();
                                double score = reader.ReadDouble();
                                if (m != null)
                                    m_GoldEarned.Add(m, score);
                            }
                            break;
                        }

                    default:
                        {
                            throw new Exception("Invalid DataRecorder.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error reading DataRecorder.bin, using default values:");
                Utility.PopColor();
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Data Recorder Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/DataRecorder.bin", true);
                int version = 9;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 9: goto case 8;    // update prison quest data
                    case 8:
                        {
                            // version 8
                            writer.Write(m_ChampPvMRankings.Count);
                            foreach (KeyValuePair<Mobile, int> kvp in m_ChampPvMRankings)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value);
                            }
                            goto case 7;
                        }
                    case 7:
                        {
                            // version 7
                            writer.Write(m_BODGold.Count);
                            foreach (KeyValuePair<Mobile, int> kvp in m_BODGold)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value);
                            }
                            goto case 6;
                        }
                    case 6:
                        {
                            writer.Write(m_PQuestPoints.Count);
                            foreach (KeyValuePair<Mobile, int[]> kvp in m_PQuestPoints)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value[0]);
                                writer.Write(kvp.Value[1]);
                                writer.Write(kvp.Value[2]);
                            }
                            goto case 5;
                        }
                    case 5:
                        {
                            writer.Write(m_MusicSales.Count);
                            foreach (KeyValuePair<Mobile, int[]> kvp in m_MusicSales)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value[0]);
                                writer.Write(kvp.Value[1]);
                            }
                            goto case 4;
                        }
                    case 4:
                        {
                            // version 4
                            writer.Write(m_BODPoints.Count);
                            foreach (KeyValuePair<Mobile, int> kvp in m_BODPoints)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value);
                            }
                            goto case 3;
                        }
                    case 3:
                        {
                            // version 3
                            writer.Write(m_GeneralPvMRankings.Count);
                            foreach (KeyValuePair<Mobile, int> kvp in m_GeneralPvMRankings)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value);
                            }

                            writer.Write(m_GoldEarned.Count);
                            foreach (KeyValuePair<Mobile, double> kvp in m_GoldEarned)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value);
                            }
                            break;
                        }

                    case 2:
                        {
                            // version 2
                            /*writer.Write(m_PvMRankings.Count);
                            foreach (KeyValuePair<Mobile, DamageStore> kvp in m_PvMRankings)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value.m_Mobile);
                                writer.Write(kvp.Value.m_Damage);
                                writer.Write(kvp.Value.m_HasRight);
                            }*/
                            break;
                        }

                    case 1:
                        {
                            // version 1
                            /*writer.Write(m_PvMRankings.Count);
                            foreach (KeyValuePair<Mobile, DamageStore> kvp in m_PvMRankings)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value.m_Damage);
                                writer.Write(kvp.Value.m_HasRight);
                            }*/
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing DataRecorder.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion Serialization
    }
}