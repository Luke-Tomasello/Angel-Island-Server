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

/* Scripts\Engines\Seasons\SeasonManager.cs
 * ChangeLog
 *  3/27/2026, Adam
 *      First time checkin.
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Server.Engines.Seasons
{
    public static class SeasonManager
    {
        private const string Folder = "Saves/Seasons";
        private const string StateFile = "SeasonState.xml";

        private static int m_Season = 1;

        public static void Initialize()
        {
            CommandSystem.Register("SeasonEnding", AccessLevel.Administrator, new CommandEventHandler(SeasonEnding_OnCommand));
            CommandSystem.Register("WipeSeasonData", AccessLevel.Administrator, new CommandEventHandler(WipeSeasonData_OnCommand));
        }
        public static void Configure()
        {
            if (Core.RuleSets.SeasonsRules())
            {
                EventSink.WorldLoad += new WorldLoadEventHandler(Load);
                EventSink.WorldSave += new WorldSaveEventHandler(Save);
            }
        }

        [Usage("WipeSeasonData")]
        [Description("Clears all in-memory seasonal data (accounts and baselines). Does not touch underlying data files.")]
        public static void WipeSeasonData_OnCommand(CommandEventArgs e)
        {
            try
            {
                int accountsCleared = SeasonalAccounts.ClearAll();
                int baselinesCleared = SeasonalEarningBaselineState.ClearAll();

                string msg = $"Cleared {accountsCleared} seasonal accounts and {baselinesCleared} baseline entries.";

                if (e.Mobile != null)
                    e.Mobile.SendMessage(0x3B2, msg);        // same green color as WipeLeaderboards
                else
                    Console.WriteLine(msg);

                // Optional: You can add a refresh or display command here if desired
                // new CLeaderBoard().LeaderBoard();   // only if it makes sense for seasonal data
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);

                string errorMsg = "Failed to wipe seasonal data. Check logs.";

                if (e.Mobile != null)
                    e.Mobile.SendMessage(0x25, errorMsg);    // red color for error
                else
                    Console.WriteLine(errorMsg);
            }
        }

        [Usage("SeasonEnding")]
        [Description("Performs end-of-season rollover logic.")]
        private static void SeasonEnding_OnCommand(CommandEventArgs e)
        {
            if (!Core.RuleSets.SeasonsRules())
            {
                e.Mobile.SendMessage("Season logic is only available on Seasons.");
                return;
            }

            Console.WriteLine("SeasonEnding: begin");

            int oldSeason = Season;
            int accountsReset = SeasonEnding();

            Console.WriteLine($"SeasonEnding: advanced Season {oldSeason} -> {Season}.");
            Console.WriteLine($"SeasonEnding: reset {accountsReset} seasonal accounts.");
            Console.WriteLine("SeasonEnding: reset all seasonal earning baselines.");
            Console.WriteLine("SeasonEnding: complete");

            e.Mobile.SendMessage(
                $"Season rollover complete. " +
                $"Advanced from Season {oldSeason} to Season {Season}. " +
                $"{accountsReset} accounts reset.");
        }

        /// <summary>
        /// Performs end-of-season cleanup.
        /// Resets seasonal points and spent points for all accounts.
        /// Returns the number of accounts that were processed.
        /// </summary>
        public static int SeasonEnding()
        {
            if (!Core.RuleSets.SeasonsRules())
                return 0;

            int accountCount = 0;

            foreach (SeasonalAccount account in SeasonalAccounts.GetAllAccounts())
            {
                if (account == null)
                    continue;

                account.SeasonPoints = 0;
                account.SeasonSpentPoints = 0;     // Note: you had SeasonSpentPoints in the gump, assuming this is the correct property
                accountCount++;
            }

            SeasonalEarningBaselineState.ResetAll();

            // next season!
            m_Season++;

            return accountCount;
        }
        public static SeasonalAccount? EnsureSeasonalAccount(uint guid, string displayName)
        {
            SeasonalAccount account = SeasonalAccounts.GetAccount(guid);

            if (account == null)
                return null;

            if (string.IsNullOrWhiteSpace(account.DisplayName) &&
                !string.IsNullOrWhiteSpace(displayName) &&
                !string.Equals(displayName, CharacterCreation.DefaultName, StringComparison.OrdinalIgnoreCase))
            {
                account.DisplayName = displayName.Trim();
            }

            return account;
        }
        /// <summary>
        /// Updates the DisplayName in the SeasonalAccount when a player changes their name.
        /// Should be called whenever a name change is committed on a Seasons shard.
        /// </summary>
        public static void ReplaceName(uint guid, string oldName, string newName)
        {
            if (!Core.RuleSets.SeasonsRules())
                return;

            if (string.IsNullOrWhiteSpace(newName))
                return;

            SeasonalAccount sa = SeasonalAccounts.GetAccount(guid);

            if (sa != null)
            {
                // Only update if we don't already have a DisplayName, or if this is the current one
                if (string.IsNullOrWhiteSpace(sa.DisplayName) ||
                    string.Equals(sa.DisplayName, oldName, StringComparison.OrdinalIgnoreCase))
                {
                    sa.DisplayName = newName.Trim();
                }
            }
        }

        public static int Season
        {
            get { return m_Season; }
        }

        private static int ReadSeasonCore()
        {
            string filePath = Path.Combine(Folder, StateFile);

            if (!File.Exists(filePath))
                return 1;

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement? root = doc["seasonState"];

            if (root == null)
                return 1;

            int season = Utility.ToInt32(root.GetAttribute("season"));

            return season > 0 ? season : 1;
        }

        public static int ReadSeason()
        {
            try
            {
                return ReadSeasonCore();
            }
            catch
            {
                return 1;
            }
        }

        private static void Load()
        {
            Console.WriteLine("Season State Loading...");

            string filePath = Path.Combine(Folder, StateFile);

            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(Folder);

                using (StreamWriter sw = new StreamWriter(filePath))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    sw.WriteLine("<seasonState season=\"1\"></seasonState>");
                }
            }

            m_Season = ReadSeason();
        }
        private static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Season State Saving...");

            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);

            string filePath = Path.Combine(Folder, StateFile);

            using (StreamWriter op = new StreamWriter(filePath))
            using (XmlTextWriter xml = new XmlTextWriter(op))
            {
                xml.Formatting = Formatting.Indented;
                xml.IndentChar = '\t';
                xml.Indentation = 1;

                xml.WriteStartDocument(true);

                xml.WriteStartElement("seasonState");
                xml.WriteAttributeString("season", m_Season.ToString());
                xml.WriteEndElement();

                xml.WriteEndDocument();
            }
        }
    }
}