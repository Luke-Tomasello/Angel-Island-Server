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

/* Scripts\Engines\DataRecorder\DataRecorder.cs
 * CHANGELOG:
 *  4/5/2026, Adam
 *      Renamed LootGold → PvMLootGold for clarity (now clearly represents gold earned via PvM activities).
 *  1/27/22, Adam
 *      Record data for two new leader boards for MusicSales
 *  11/6/21, Adam
 *      Check for null mobile before adding back to in-memory database
 *  10/25/21, Adam
 *      Add support for BOD Leaderboard
 *	8/17/21, Adam
 *		first time check in
 */

using Server.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Engines.DataRecorder
{
    /// <summary>
    /// DataRecorder - Central activity and economy tracking system for the shard.
    ///
    /// This static class records various player activities and earnings used for:
    /// - Leaderboards (PvM, Prison Quest, BOD, Music, Earned Gold, etc.)
    /// - Seasonal rewards and point systems
    /// - Township subsidies and internal statistics
    ///
    /// All data is stored in a single unified structure using RecorderDomain.
    /// Old separate dictionaries have been retired in favor of this model.
    ///
    /// Serialization:
    /// Current version is 10 (unified dynamic format). Adding new domains requires
    /// no changes to Load/Save.
    /// </summary>
    public enum RecorderDomain
    {
        PvMLootGold,    // Gold earned from killing monsters and looting them
        ChestGold,      // Gold earned from lockpicking chests
        PvMPoints,      // Damage contribution points for PvM leaderboards
        PQChamps,       // Prison Quest Champion damage points

        BODPoints,

        MusicGold,
        MusicPoints,

        PQEscapes,
        PQRares,
        PQSterling,

        SmithBODGold,
        TailorBODGold,
        CarpenterBODGold,

        TheftGold,      // Gold earned through stealing

        // New: Generic bucket for major launch / seasonal event participation points
        // Reusable across any future launch or big event without creating dead domains.
        LaunchEventPoints
    }

    public static class DataRecorder
    {
        public static void Initialize()
        {
            CommandSystem.Register("WipeLeaderboards", AccessLevel.Owner, new CommandEventHandler(WipeLeaderboards_OnCommand));
        }

        public static void Configure()
        {
            EventSink.WorldLoad += Load;
            EventSink.WorldSave += Save;
        }

        #region Core Data Storage

        private static readonly Dictionary<Mobile, Dictionary<RecorderDomain, double>> m_Database
            = new Dictionary<Mobile, Dictionary<RecorderDomain, double>>();

        /// <summary>
        /// Read-only access to the internal database.
        /// Use Add() and Get() for normal operations.
        /// </summary>
        public static IReadOnlyDictionary<Mobile, Dictionary<RecorderDomain, double>> Database
            => m_Database;

        /// <summary>
        /// Add (or subtract) a value for a specific domain for a player.
        /// Negative values are supported and will clean up zero/negative entries.
        /// </summary>
        public static void Add(Mobile m, RecorderDomain domain, double value)
        {
            if (m == null || value == 0)
                return;

            if (!m_Database.TryGetValue(m, out var table))
            {
                table = new Dictionary<RecorderDomain, double>();
                m_Database[m] = table;
            }

            table.TryGetValue(domain, out double current);
            double newValue = current + value;

            if (newValue <= 0)
            {
                table.Remove(domain);
                if (table.Count == 0)
                    m_Database.Remove(m);
            }
            else
            {
                table[domain] = newValue;
            }
        }

        /// <summary>
        /// Get the value for a specific domain for a player. Returns 0 if not present.
        /// </summary>
        public static double Get(Mobile m, RecorderDomain domain)
        {
            if (m == null)
                return 0;

            if (m_Database.TryGetValue(m, out var table) &&
                table.TryGetValue(domain, out var value))
                return value;

            return 0;
        }

        /// <summary>
        /// Delete all recorded data for a mobile.
        /// </summary>
        public static void Delete(Mobile m)
        {
            if (m == null)
                return;

            m_Database.Remove(m);
        }

        /// <summary>
        /// Replace one mobile key with another, preserving recorded domain values.
        /// Used when a semantic player identity is transferred to a Substitute.
        /// </summary>
        public static void Replace(Mobile from, Mobile to)
        {
            if (from == null || to == null || from == to)
                return;

            if (!m_Database.TryGetValue(from, out var fromTable))
                return;

            if (!m_Database.TryGetValue(to, out var toTable))
            {
                toTable = new Dictionary<RecorderDomain, double>();
                m_Database[to] = toTable;
            }

            foreach (var kvp in fromTable)
            {
                toTable.TryGetValue(kvp.Key, out double current);

                double newValue = current + kvp.Value;

                if (newValue <= 0)
                    toTable.Remove(kvp.Key);
                else
                    toTable[kvp.Key] = newValue;
            }

            m_Database.Remove(from);

            if (toTable.Count == 0)
                m_Database.Remove(to);
        }
        public static void RemoveDeletedEntries()
        {
            List<Mobile> toDelete = new List<Mobile>();

            foreach (Mobile m in m_Database.Keys)
            {
                if (m == null || m.Deleted)
                    toDelete.Add(m);
            }

            foreach (Mobile m in toDelete)
                Delete(m);
        }
        #endregion

        #region Leaderboards

        /// <summary>
        /// Returns the top players by total earned gold (PvMLootGold + ChestGold + TheftGold combined).
        /// This is what players see on the main "Earned Gold" leaderboard.
        /// </summary>
        public static List<KeyValuePair<Mobile, int>> GetEarnedGoldLeaderboard(int maxCount = 0)
        {
            var combined = new Dictionary<Mobile, double>();

            foreach (var entry in Database)
            {
                Mobile m = entry.Key;
                if (m == null) continue;

                double total = Get(m, RecorderDomain.PvMLootGold) +
                               Get(m, RecorderDomain.ChestGold) +
                               Get(m, RecorderDomain.TheftGold);

                if (total > 0)
                    combined[m] = total;
            }

            var list = new List<KeyValuePair<Mobile, int>>(
                combined.Select(k => new KeyValuePair<Mobile, int>(k.Key, (int)k.Value))
            );

            list.Sort((a, b) => b.Value.CompareTo(a.Value));

            if (maxCount > 0 && list.Count > maxCount)
                list.RemoveRange(maxCount, list.Count - maxCount);

            return list;
        }

        /// <summary>
        /// Returns the top players for any single domain, sorted descending.
        /// </summary>
        public static List<KeyValuePair<Mobile, int>> GetTopPlayers(RecorderDomain domain, int maxCount = 0)
        {
            var list = new List<KeyValuePair<Mobile, int>>();

            foreach (var entry in Database)
            {
                Mobile m = entry.Key;
                if (m == null) continue;

                double value = Get(m, domain);
                if (value > 0)
                    list.Add(new KeyValuePair<Mobile, int>(m, (int)value));
            }

            list.Sort((a, b) => b.Value.CompareTo(a.Value));

            if (maxCount > 0 && list.Count > maxCount)
                list.RemoveRange(maxCount, list.Count - maxCount);

            return list;
        }

        /// <summary>
        /// Returns the top players by total BOD gold earned (Smith + Tailor + Carpenter combined).
        /// The leaderboard does not distinguish between professions — it shows the total.
        /// </summary>
        public static List<KeyValuePair<Mobile, int>> GetBODGoldLeaderboard(int maxCount = 0)
        {
            var combined = new Dictionary<Mobile, double>();

            foreach (var entry in Database)
            {
                Mobile m = entry.Key;
                if (m == null) continue;

                double totalBODGold =
                    Get(m, RecorderDomain.SmithBODGold) +
                    Get(m, RecorderDomain.TailorBODGold) +
                    Get(m, RecorderDomain.CarpenterBODGold);

                if (totalBODGold > 0)
                    combined[m] = totalBODGold;
            }

            var list = new List<KeyValuePair<Mobile, int>>(
                combined.Select(k => new KeyValuePair<Mobile, int>(k.Key, (int)k.Value))
            );

            list.Sort((a, b) => b.Value.CompareTo(a.Value));

            if (maxCount > 0 && list.Count > maxCount)
                list.RemoveRange(maxCount, list.Count - maxCount);

            return list;
        }

        #endregion

        #region Recording Helpers
        // Put this method in DataRecorder.cs
        public static void RemoveStaffEntries()
        {
            var staffToRemove = m_Database.Keys
                .Where(m => m != null && m.AccessLevel > AccessLevel.Player)
                .ToList();

            foreach (var m in staffToRemove)
                m_Database.Remove(m);
        }
        public static void GoldSink(Mobile m, object obj, int price)
        {
            if (!Core.Debug && !Core.GMNTC_CFG)
                if (m != null && m.AccessLevel > AccessLevel.Player)
                    return;

            // TODO: Expand with player vendor upkeep, rental charges, etc.
        }

        #endregion


        #region Serialization (Version 10 - Unified Dynamic Format)

        public static void Load()
        {
            if (!File.Exists("Saves/DataRecorder.bin"))
                return;

            Console.WriteLine("Data Recorder Loading...");

            BinaryFileReader? reader = null;

            try
            {
                reader = new BinaryFileReader(
                    new BinaryReader(new FileStream("Saves/DataRecorder.bin", FileMode.Open, FileAccess.Read)));

                int version = reader.ReadInt();

                switch (version)
                {
                    case 10:
                        // Unified dynamic format
                        int playerCount = reader.ReadInt();
                        for (int i = 0; i < playerCount; i++)
                        {
                            Mobile? m = reader.ReadMobile();
                            if (m == null)
                            {
                                int skip = reader.ReadInt();
                                for (int j = 0; j < skip; j++)
                                {
                                    reader.ReadInt();   // domain
                                    reader.ReadDouble(); // value
                                }
                                continue;
                            }

                            int domainCount = reader.ReadInt();
                            for (int j = 0; j < domainCount; j++)
                            {
                                int domainId = reader.ReadInt();
                                double value = reader.ReadDouble();
                                if (value != 0)
                                    Add(m, (RecorderDomain)domainId, value);
                            }
                        }
                        break;

                    // Migration from legacy versions (9 and below)
                    case 9:
                        goto case 8;
                    case 8:
                        // Prison Quest Champ PvM Rankings → PQChamps
                        int count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile? m = reader.ReadMobile();
                            int damage = reader.ReadInt();
                            if (m != null && damage > 0)
                                Add(m, RecorderDomain.PQChamps, damage);
                        }
                        goto case 7;

                    case 7: // Legacy BOD Gold → split evenly across professions
                        count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile? m = reader.ReadMobile();
                            int gold = reader.ReadInt();

                            if (m != null && gold > 0)
                            {
                                int goldPerProfession = gold / 3;
                                int remainder = gold % 3;

                                if (goldPerProfession > 0)
                                {
                                    Add(m, RecorderDomain.SmithBODGold, goldPerProfession);
                                    Add(m, RecorderDomain.TailorBODGold, goldPerProfession);
                                    Add(m, RecorderDomain.CarpenterBODGold, goldPerProfession);
                                }

                                if (remainder > 0)
                                    Add(m, RecorderDomain.SmithBODGold, remainder);
                            }
                        }
                        goto case 6;

                    case 6: // Prison Quest Points
                        count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile? m = reader.ReadMobile();
                            int escapes = reader.ReadInt();
                            int rares = reader.ReadInt();
                            int sterling = reader.ReadInt();
                            if (m != null)
                            {
                                if (escapes > 0) Add(m, RecorderDomain.PQEscapes, escapes);
                                if (rares > 0) Add(m, RecorderDomain.PQRares, rares);
                                if (sterling > 0) Add(m, RecorderDomain.PQSterling, sterling);
                            }
                        }
                        goto case 5;

                    case 5: // Music Sales
                        count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile? m = reader.ReadMobile();
                            int price = reader.ReadInt();
                            int sold = reader.ReadInt();
                            if (m != null)
                            {
                                if (price > 0) Add(m, RecorderDomain.MusicGold, price);
                                if (sold > 0) Add(m, RecorderDomain.MusicPoints, sold);
                            }
                        }
                        goto case 4;

                    case 4: // BOD Points
                        count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile? m = reader.ReadMobile();
                            int points = reader.ReadInt();
                            if (m != null && points > 0)
                                Add(m, RecorderDomain.BODPoints, points);
                        }
                        goto case 3;

                    case 3: // General PvM Rankings + PvMLootGold (was LootGold)
                        count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile? m = reader.ReadMobile();
                            int damage = reader.ReadInt();
                            if (m != null && damage > 0)
                                Add(m, RecorderDomain.PvMPoints, damage);
                        }

                        count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile? m = reader.ReadMobile();
                            double gold = reader.ReadDouble();
                            if (m != null && gold > 0)
                                Add(m, RecorderDomain.PvMLootGold, gold);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Utility.Monitor.WriteLine("Error loading DataRecorder.bin", ConsoleColor.Red);
                LogHelper.LogException(ex);
            }
            finally
            {
                reader?.Close();
            }
        }

        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Data Recorder Saving...");

            BinaryFileWriter? writer = null;

            try
            {
                writer = new BinaryFileWriter("Saves/DataRecorder.bin", true);
                writer.Write(10); // unified format

                writer.Write(m_Database.Count);

                foreach (var playerEntry in m_Database)
                {
                    writer.Write(playerEntry.Key);

                    var domains = playerEntry.Value;
                    writer.Write(domains.Count);

                    foreach (var d in domains)
                    {
                        writer.Write((int)d.Key);
                        writer.Write(d.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing DataRecorder.bin");
                LogHelper.LogException(ex);
            }
            finally
            {
                writer?.Close();
            }
        }

        #endregion


        #region Wipe Command

        [Usage("WipeLeaderboards")]
        [Description("Clears all leaderboard data. Does not delete the associated persistent storage.")]
        public static void WipeLeaderboards_OnCommand(CommandEventArgs e)
        {
            try
            {
                int clearedCount = ClearAllDataRecorder();

                string message = $"{clearedCount} leaderboards cleared.";

                if (e.Mobile != null)
                    e.Mobile.SendMessage(0x3B2, message);
                else
                    Console.WriteLine(message);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);

                if (e.Mobile != null)
                    e.Mobile.SendMessage(0x25, "Failed to wipe leaderboards. Check logs.");
                else
                    Console.WriteLine("Failed to wipe leaderboards. Check logs.");
            }
        }

        public static int ClearAllDataRecorder()
        {
            int count = m_Database.Count;
            m_Database.Clear();
            return count;
        }

        #endregion
    }
}