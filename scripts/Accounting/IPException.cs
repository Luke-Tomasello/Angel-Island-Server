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

/* Scripts/Accounting/Account.cs
 *	CHANGELOG
 * 7/29/2024, Adam
 *  Initialize database if it does not exist.
 *	8/5/22, Adam
 *	    Instrument GetLimit() to output to the console if an IPException is overseeing logins.
 *	    Ignore IP Exceptions since they are global.
 *	    Even if we want an IP exception for someone on AI, we DON'T want an exception for them on MO/SP
 *	    The current mechanism: Login into LS, create an account, login to the server we want the exception, and create an account. This account will be 'grandfathered' in.
 * 6/20/22, Yoar
 *      Now pushing DB changes via a SQLiteTransaction.
 *      This way, I can explicitly close the transaction such that the changes are saved to disk.
 * 6/19/22, Yoar
 *      Added external IP exception DB.
 *      
 *      We keep an external DB of IP exceptions. We abide to the following rules:
 *      1. (All servers) Whenever we change/set IP exceptions (e.g. via the admin gump), we push the new IP
 *         exception to the ipexception DB.
 *      2. (All servers) Whenever we 'get' IP exceptions (via 'IPException.GetLimit'), we sync the IP exceptions
 *         with the ipexception DB. Therefore, DB IP exceptions are prioritized over XML IP exceptions.
 *      3. (DB master only) On world load, we push our XML IP exceptions to the DB. This way, we ensure that IP
 *         exceptions from the XML file are also present in the ipexception DB.
 * 6/18/22, Adam
 *      Make EventSink_OnWorldSave public so it they can be called from the Login server's special version of Save.
 * 6/14/22, Yoar
 *      Refactored completely
 *      Added IPExceptionDatabase
 * 3/15/16, Adam
 *		Reverse changes of 2/8/08
 *		Turn IPException code back on. This is because the IPException logic is per IP whereas the MaxAccountsPerIP
 *		functionality is global.
 *	2/18/08, Adam
 *		We now allow 3 accounts per household - IPException logic no longer needed
 *	8/13/06 - Pix.
 *		Added null-checking in IPException checking.
 *	6/14/05 - Pix
 *		Initial Version.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Xml;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Server.Accounting
{
    public static class IPException
    {
        private static readonly Dictionary<string, int> m_Table = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public static Dictionary<string, int> Table { get { return m_Table; } }

        public static void Configure()
        {
            EventSink.WorldLoad += EventSink_OnWorldLoad;
            EventSink.WorldSave += EventSink_OnWorldSave;
        }

        private static void EventSink_OnWorldLoad()
        {
            LoadXml();
        }

        public static void EventSink_OnWorldSave(WorldSaveEventArgs e)
        {
            SaveXml();
        }

        public static int GetLimit(string ip)
        {
            int limit = _GetLimit(ip);
            if (limit != 0)
            {
                Utility.ConsoleWriteLine(string.Format("IPException of {0} found for {1} ... Ignoring", limit, ip), ConsoleColor.Red);
                return 0;
            }

            return limit;
        }
        private static int _GetLimit(string ip)
        {
            if (Core.UseLoginDB)
            {
                int? dbLimit = IPExceptionDatabase.GetLimit(ip);

                if (dbLimit != null)
                    return (m_Table[ip] = dbLimit.Value);
            }

            int limit;

            if (m_Table.TryGetValue(ip, out limit))
                return limit;

            return 0;
        }

        public static void SetLimit(string ip, int limit)
        {
            m_Table[ip] = limit;

            if (Core.UseLoginDB)
                IPExceptionDatabase.SaveLimit(ip, limit);
        }

        public static void Remove(string ip)
        {
            m_Table.Remove(ip);

            if (Core.UseLoginDB)
                IPExceptionDatabase.Remove(ip);
        }

        public static ICollection GetAllIPs()
        {
            if (Core.UseLoginDB)
                return IPExceptionDatabase.GetAllIPs();

            return m_Table.Keys;
        }

        #region Save/Load XML

        private const string Folder = "Saves/Accounts";
        private const string FileName = "ipexception.xml";

        // TODO: Rewrite to use XmlDoc instead of XmlTextWriter
        private static void SaveXml()
        {
            try
            {
                if (!Directory.Exists(Folder))
                    Directory.CreateDirectory(Folder);

                string filePath = Path.Combine(Folder, FileName);

                using (StreamWriter sw = new StreamWriter(filePath))
                using (XmlTextWriter writer = new XmlTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.IndentChar = '\t';
                    writer.Indentation = 1;

                    writer.WriteStartDocument(true);

                    writer.WriteStartElement("root");

                    writer.WriteAttributeString("count", m_Table.Count.ToString());

                    foreach (KeyValuePair<string, int> kvp in m_Table)
                    {
                        writer.WriteStartElement("ipexception");
                        writer.WriteStartElement("ip");
                        writer.WriteString(kvp.Key);
                        writer.WriteEndElement();
                        writer.WriteStartElement("number");
                        writer.WriteString(kvp.Value.ToString());
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();

                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        private static void LoadXml()
        {
            try
            {
                Console.Write("Loading IP exceptions from '{0}'...", FileName);

                string filePath = Path.Combine(Folder, FileName);

                if (!File.Exists(filePath))
                {
                    Console.WriteLine(" Aborting, file doesn't exist.");
                    return;
                }

                int count = 0;

                XmlDocument doc = new XmlDocument();

                doc.Load(filePath);

                foreach (XmlElement xmlElem in doc["root"].GetElementsByTagName("ipexception"))
                {
                    string ip = xmlElem["ip"].InnerText;
                    int limit = Utility.ToInt32(xmlElem["number"].InnerText);

                    if (!string.IsNullOrEmpty(ip) && limit != 0)
                    {
                        m_Table[ip] = limit;
                        count++;
                    }
                }

                Console.WriteLine(" Done, loaded {0} IP exceptions.", count);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            // only if we're the DB master, push our IP exceptions to the DB
            if (Core.UseLoginDB && Core.RuleSets.DatabaseMaster)
                IPExceptionDatabase.SaveAllLimits();
        }

        #endregion
    }

    public static class IPExceptionDatabase
    {
        public static bool Verbose = true;

        public static string GetDatabasePath(ref bool error)
        {
            string path = Environment.GetEnvironmentVariable("AI.IPEXCEPTIONDB");

            if (string.IsNullOrWhiteSpace(path))
                path = @"C:\AIDB\ipexception.db";

            error = false;
            if (!File.Exists(path))
            {   // initialize database
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                // this creates a zero-byte file
                SQLiteConnection.CreateFile(path);

                if (!File.Exists(path))
                    error = true;
            }

            if (!Core.UseLoginDB)
                ;// why are we here?

            return path;
        }

        private static SQLiteConnection Connect()
        {
            bool error = false;
            return new SQLiteConnection(string.Format("Data Source={0};Pooling=True;Max Pool Size=100;", GetDatabasePath(ref error)));
        }

        private static void EnsureTable()
        {
            const string cmdText = "CREATE TABLE IF NOT EXISTS IPException (ipaddress TEXT PRIMARY KEY, accountlimit INT);";

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

        public static int? GetLimit(string ipAddress)
        {
            EnsureTable();

            DataTable table = null;

            const string cmdText = "SELECT accountlimit FROM IPException WHERE ipaddress=@ip;";

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
                VerboseMessage("Warning: Failed to read ipexception.db.");
                return null;
            }

            if (table.Rows.Count == 0)
                return null;

            if (table.Rows.Count != 1)
                VerboseMessage("Warning: Detected duplicate ip addresses (x{0}) in ipexception.db for ipaddress '{1}'.", table.Rows.Count, ipAddress);

            int accountLimit = 0;

            try
            {
                accountLimit = table.Rows[0].Field<int>(0);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            return accountLimit;
        }

        private static readonly KeyValuePair<string, int>[] m_SingleLimit = new KeyValuePair<string, int>[1];

        public static int SaveLimit(string ipAddress, int accountLimit)
        {
            m_SingleLimit[0] = new KeyValuePair<string, int>(ipAddress, accountLimit);

            return SaveLimitList(m_SingleLimit);
        }

        public static int SaveAllLimits()
        {
            return SaveLimitList(IPException.Table);
        }

        private static int SaveLimitList(ICollection<KeyValuePair<string, int>> list)
        {
            EnsureTable();

            const string cmdText = "INSERT OR REPLACE INTO IPException (ipaddress, accountlimit) VALUES (@ip, @al);";

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
                            foreach (KeyValuePair<string, int> kvp in list)
                            {
                                cmd.Parameters.AddWithValue("@ip", kvp.Key);
                                cmd.Parameters.AddWithValue("@al", kvp.Value.ToString());
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

            const string cmdText = "DELETE FROM IPException WHERE ipaddress=@ip;";

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

        public static IList GetAllIPs()
        {
            EnsureTable();

            DataTable table = null;

            const string cmdText = "SELECT ipaddress FROM IPException;";

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
                VerboseMessage("Warning: Failed to read ipexception.db.");
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