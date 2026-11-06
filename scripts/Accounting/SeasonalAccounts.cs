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

/* Scripts\Accounting\SeasonalAccounts.cs
 * ChangeLog
 *  3/27/2026, Adam
 *      First time checkin.
 */

using Server.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Server.Accounting
{
    public class SeasonalAccounts
    {
        private static Dictionary<uint, SeasonalAccount> m_Accounts = new Dictionary<uint, SeasonalAccount>();
        private static bool Enabled => Core.RuleSets.SeasonsRules();
        public static void Configure()
        {
            if (Enabled)
            {
                EventSink.WorldLoad += Load;
                EventSink.WorldSave += Save;
            }
        }
        public static void Initialize()
        {
            if (Enabled)
            {
                TargetCommands.Register(new SeasonalAccountCommand());
            }
        }
        public static SeasonalAccount GetAccount(Mobile m)
        {
            if (!Enabled)
            {
                System.Diagnostics.Debug.Assert(false, "SeasonalAccounts accessed while SeasonsRules is disabled.");
                return null;
            }

            if (m == null || m.Account == null)
            {
                System.Diagnostics.Debug.Assert(false, "GetAccount called with null Mobile or Account.");
                return null;
            }

            return GetAccount(m.Account as Accounting.Account);
        }

        public static SeasonalAccount GetAccount(Account? acct)
        {
            if (!Enabled)
            {
                System.Diagnostics.Debug.Assert(false, "SeasonalAccounts accessed while SeasonsRules is disabled.");
                return null;
            }

            if (acct == null)
            {
                System.Diagnostics.Debug.Assert(false, "GetAccount called with null Account.");
                return null;
            }

            uint guid = acct.GUID;

            if (!m_Accounts.TryGetValue(guid, out SeasonalAccount? sa))
            {
                sa = new SeasonalAccount(guid);
                m_Accounts[guid] = sa;
            }

            // Legacy backfill:
            // DisplayName was added after some SeasonalAccount records already existed.
            // If missing, seed it from the first live reserved name on the owning account.
            if (string.IsNullOrWhiteSpace(sa.DisplayName))
            {
                string? first = acct.GetReservedNames()?.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(first))
                    sa.DisplayName = first.Trim();
            }

            return sa;
        }

        public static SeasonalAccount GetAccount(uint guid)
        {
            if (!Enabled)
            {
                System.Diagnostics.Debug.Assert(false, "SeasonalAccounts accessed while SeasonsRules is disabled.");
                return null;
            }

            Account? acct = Accounts.GetAccount(guid);

            if (acct == null)
            {
                System.Diagnostics.Debug.Assert(false, "GetAccount(uint guid) could not resolve Account.");
                return null;
            }

            return GetAccount(acct);
        }
        /// <summary>
        /// Removes the SeasonalAccount record associated with the given GUID.
        /// 
        /// WARNING:
        /// Do NOT call this as part of normal Account deletion flows.
        /// SeasonalAccount data is intentionally persistent across shard wipes (Seasons),
        /// as it tracks lifetime progression (e.g., accumulated points) beyond a single season.
        /// </summary>
        public static bool DeleteAccount(uint guid)
        {
            if (!Enabled)
            {
                System.Diagnostics.Debug.Assert(false, "SeasonalAccounts accessed while SeasonsRules is disabled.");
                return false;
            }

            return m_Accounts.Remove(guid);
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("SeasonalAccounts Saving...");

            if (!Directory.Exists("Saves/Accounts"))
                Directory.CreateDirectory("Saves/Accounts");

            string filePath = Path.Combine("Saves/Accounts", "SeasonalAccounts.xml");

            using (StreamWriter op = new StreamWriter(filePath))
            using (XmlTextWriter xml = new XmlTextWriter(op))
            {
                xml.Formatting = Formatting.Indented;
                xml.IndentChar = '\t';
                xml.Indentation = 1;

                xml.WriteStartDocument(true);
                xml.WriteStartElement("seasonalAccounts");

                xml.WriteAttributeString("count", m_Accounts.Count.ToString());

                foreach (SeasonalAccount a in m_Accounts.Values)
                    a.Save(xml);

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }

        public static void Load()
        {
            string filePath = Path.Combine("Saves/Accounts", "SeasonalAccounts.xml");

            if (!File.Exists(filePath))
            {
                Console.WriteLine("No SeasonalAccounts database. Creating...");
                Directory.CreateDirectory("Saves/Accounts");

                using (StreamWriter sw = new StreamWriter(filePath))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    sw.WriteLine("<seasonalAccounts count=\"0\"></seasonalAccounts>");
                }
            }

            m_Accounts.Clear();

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement? root = doc["seasonalAccounts"];

            if (root == null)
            {
                Console.WriteLine("Warning: seasonalAccounts root not found.");
                return;
            }

            foreach (XmlElement node in root.GetElementsByTagName("account"))
            {
                try
                {
                    SeasonalAccount sa = new SeasonalAccount(node);
                    m_Accounts[sa.AccountGUID] = sa;
                }
                catch
                {
                    Console.WriteLine("Warning: SeasonalAccount load failed");
                }
            }

            Console.WriteLine($"Loaded {m_Accounts.Count} seasonal accounts");
        }

        private class SeasonalAccountCommand : BaseCommand
        {
            public SeasonalAccountCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "SeasonalAccount" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "SeasonalAccount <player>";
                Description = "Opens the SeasonalAccount properties for the target player.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is Mobile m)
                {
                    var acct = m.Account as Server.Accounting.Account;

                    if (acct == null)
                    {
                        LogFailure("Target has no valid account.");
                        return;
                    }

                    var sa = Server.Accounting.SeasonalAccounts.GetAccount(acct);

                    if (sa == null)
                    {
                        LogFailure("SeasonalAccount unavailable (check gating).");
                        return;
                    }

                    e.Mobile.SendMessage("Opening SeasonalAccount properties...");
                    e.Mobile.SendGump(new Server.Gumps.PropertiesGump(e.Mobile, sa));
                }
                else
                {
                    LogFailure("That is not a player.");
                }
            }
        }

        public static int ClearAll()
        {
            int count = m_Accounts.Count;
            m_Accounts.Clear();
            return count;
        }

        public static IEnumerable<SeasonalAccount> GetAllAccounts()
        {
            return m_Accounts.Values;
        }

        public static void RemoveStaffEntries()
        {
            if (!Enabled)
            {
                System.Diagnostics.Debug.Assert(false, "SeasonalAccounts accessed while SeasonsRules is disabled.");
                return;
            }

            List<uint> remove = new List<uint>();

            foreach (KeyValuePair<uint, SeasonalAccount> kvp in m_Accounts)
            {
                SeasonalAccount sa = kvp.Value;

                if (sa == null)
                    continue;

                Account? acct = Accounts.GetAccount(sa.AccountGUID);

                if (acct == null)
                    continue;

                // Account-level check
                if (acct.AccessLevel > AccessLevel.Player)
                {
                    remove.Add(kvp.Key);
                    continue;
                }

                // Mobile-level check (any mobile on account disqualifies)
                for (int i = 0; i < acct.Length; ++i)
                {
                    Mobile m = acct[i];

                    if (m != null && m.AccessLevel > AccessLevel.Player)
                    {
                        remove.Add(kvp.Key);
                        break;
                    }
                }
            }

            foreach (uint guid in remove)
                m_Accounts.Remove(guid);
        }
    }
}