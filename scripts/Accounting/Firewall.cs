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

/* Accounting/Firewall.cs
 * CHANGELOG:
 * 7/29/2024, Adam
 *  Initialize database if it does not exist.
 *  6/23/22, Yoar
 *      Added loading messages
 *  6/21/22, Yoar
 *      Added FirewallDatabase
 *	6/20/22, Yoar
 *		Refactored completely
 *	2/15/05 - Pix
 *		Initial version for 1.0.0
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace Server
{
    public static class Firewall
    {
        private static readonly List<string> m_List = new List<string>();

        public static List<string> List { get { return m_List; } }

        public static void Configure()
        {
            EventSink.WorldLoad += EventSink_OnWorldLoad;
            EventSink.WorldSave += EventSink_OnWorldSave;
        }

        private static void EventSink_OnWorldLoad()
        {
            LoadCfg();
        }

        public static void EventSink_OnWorldSave(WorldSaveEventArgs e)
        {
            SaveCfg();
        }

        public static bool IsBlocked(string ip)
        {
            if (Core.UseLoginDB && FirewallDatabase.Contains(ip))
                return true;

            return m_List.Contains(ip);
        }

        public static void Add(string ip)
        {
            if (!m_List.Contains(ip))
                m_List.Add(ip);

            if (Core.UseLoginDB)
                FirewallDatabase.Save(ip);
        }

        public static void Remove(string ip)
        {
            m_List.Remove(ip);

            if (Core.UseLoginDB)
                FirewallDatabase.Remove(ip);
        }

        public static IList GetAllIPs()
        {
            if (Core.UseLoginDB)
                return FirewallDatabase.GetAll();

            return m_List;
        }

        #region Save/Load CFG

        private const string m_FileName = "firewall.cfg";

        private static void SaveCfg()
        {
            if (m_List.Count == 0)
                return;

            try
            {
                using (StreamWriter op = new StreamWriter(m_FileName))
                {
                    for (int i = 0; i < m_List.Count; ++i)
                        op.WriteLine(m_List[i]);
                }
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        private static void LoadCfg()
        {
            try
            {
                Console.Write("Loading firewalled IPs from '{0}'...", m_FileName);

                if (!File.Exists(m_FileName))
                {
                    Console.WriteLine(" Aborting, file doesn't exist.");
                    return;
                }

                int count = 0;

                using (StreamReader ip = new StreamReader(m_FileName))
                {
                    string line;

                    while ((line = ip.ReadLine()) != null)
                    {
                        line = line.Trim();

                        if (line.Length != 0)
                        {
                            m_List.Add(line);
                            count++;
                        }
                    }
                }

                Console.WriteLine(" Done, loaded {0} firewalled IPs.", count);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            // only if we're the DB master, push our firewalled IPs to the DB
            if (Core.UseLoginDB && Core.RuleSets.DatabaseMaster)
                FirewallDatabase.SaveAll();
        }

        #endregion
    }

    public static class FirewallDatabase
    {
        public static bool Verbose = true;

        private static string GetDatabasePath()
        {
            string path = Environment.GetEnvironmentVariable("AI.FIREWALLDB");

            if (String.IsNullOrWhiteSpace(path))
                path = @"C:\AIDB\firewalldb.db";

            if (!File.Exists(path))
            {   // initialize database
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                // this creates a zero-byte file
                SQLiteConnection.CreateFile(path);
            }

            return path;
        }

        private static SQLiteConnection Connect()
        {
            return new SQLiteConnection(String.Format("Data Source={0};Pooling=True;Max Pool Size=100;", GetDatabasePath()));
        }

        private static void EnsureTable()
        {
            const string cmdText = "CREATE TABLE IF NOT EXISTS Firewall (ipaddress TEXT PRIMARY KEY);";

            using (SQLiteConnection conn = Connect())
            using (SQLiteCommand cmd = new SQLiteCommand(cmdText, conn))
            {
                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                }
            }
        }

        public static bool Contains(string ipAddress)
        {
            EnsureTable();

            DataTable table = null;

            const string cmdText = "SELECT ipaddress FROM Firewall WHERE ipaddress=@ip;";

            using (SQLiteConnection conn = Connect())
            using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmdText, conn))
            {
                try
                {
                    conn.Open();
                    da.SelectCommand.Parameters.AddWithValue("@ip", ipAddress);
                    table = new DataTable();
                    da.Fill(table);
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                }
            }

            if (table == null)
            {
                VerboseMessage("Warning: Failed to read firewall.db.");
                return false;
            }

            if (table.Rows.Count == 0)
                return false;

            if (table.Rows.Count != 1)
                VerboseMessage("Warning: Detected duplicate ip addresses (x{0}) in firewall.db for ipaddress '{1}'.", table.Rows.Count, ipAddress);

            return true;
        }

        private static readonly string[] m_SingleIP = new string[1];

        public static int Save(string ipAddress)
        {
            m_SingleIP[0] = ipAddress;

            return SaveList(m_SingleIP);
        }

        public static int SaveAll()
        {
            return SaveList(Firewall.List);
        }

        private static int SaveList(ICollection<string> list)
        {
            EnsureTable();

            const string cmdText = "INSERT OR REPLACE INTO Firewall (ipaddress) VALUES (@ip);";

            int count = 0;

            using (SQLiteConnection conn = Connect())
            using (SQLiteCommand cmd = new SQLiteCommand(cmdText, conn))
            {
                try
                {
                    conn.Open();

                    using (SQLiteTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (string ipAddress in list)
                            {
                                cmd.Parameters.AddWithValue("@ip", ipAddress);
                                cmd.ExecuteNonQuery();

                                count++;
                            }

                            trans.Commit();
                        }
                        catch (Exception ex)
                        {
                            EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                        }
                    }
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                }
            }

            return count;
        }

        public static void Remove(string ipAddress)
        {
            EnsureTable();

            const string cmdText = "DELETE FROM Firewall WHERE ipaddress=@ip;";

            using (SQLiteConnection conn = Connect())
            using (SQLiteCommand cmd = new SQLiteCommand(cmdText, conn))
            {
                try
                {
                    conn.Open();

                    using (SQLiteTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            cmd.Parameters.AddWithValue("@ip", ipAddress);
                            cmd.ExecuteNonQuery();

                            trans.Commit();
                        }
                        catch (Exception ex)
                        {
                            EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                        }
                    }
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                }
            }
        }

        private static readonly string[] m_EmptyIPs = new string[0];

        public static IList GetAll()
        {
            EnsureTable();

            DataTable table = null;

            const string cmdText = "SELECT ipaddress FROM Firewall;";

            using (SQLiteConnection conn = Connect())
            using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmdText, conn))
            {
                try
                {
                    conn.Open();
                    table = new DataTable();
                    da.Fill(table);
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                }
            }

            if (table == null)
            {
                VerboseMessage("Warning: Failed to read firewall.db.");
                return m_EmptyIPs;
            }

            if (table.Rows.Count == 0)
                return m_EmptyIPs;

            List<string> list = new List<string>();

            for (int i = 0; i < table.Rows.Count; i++)
            {
                string ipAddress = null;

                try
                {
                    ipAddress = table.Rows[i].Field<string>(0);
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                }

                if (ipAddress != null)
                    list.Add(ipAddress);
            }

            return list;
        }

        private static void VerboseMessage(string format, params object[] args)
        {
            if (Verbose)
                Console.WriteLine(format, args);
        }
    }
}