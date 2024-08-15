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

/* Scripts/Accounting/AccountsDatabase.cs
 * CHANGELOG
 * 7/29/2024, Adam
 *  Initialize database if it does not exist.
 * 8/5/22, Adam
 *      We no longer SeedAccounts
 *      Each shard is now responsible for creating only those accounts allowed.
 * 8/2/22, Yoar
 *      Rewrote my previous fix using 'DataRow.IsNull'.
 *      Fixed another instance of 'row.Field<int>(4)'.
 * 8/2/22, Yoar
 *      Wrapped a try-catch around 'hardwareHash = row.Field<int>(4)'. The hardwarehash column (index = 4)
 *      was added in post. Therefore 'row.Field(4)' may return DBNull, which cannot be casted to int.
 * 8/2/22, Yoar
 *      Added HardwareHash entry.
 * 6/20/22, Yoar
 *      Now pushing DB changes via a SQLiteTransaction.
 *      This way, I can explicitly close the transaction such that the changes are saved to disk.
 * 6/19/22, Yoar
 *      Account DB overhaul.
 *      
 *      We keep an external DB of account credentials. We abide to the following rules:
 *      1. (All servers) Whenever we change/set account credentials (e.g. via the admin gump), we push the
 *         new credentials to the accounts DB.
 *      2. (All servers) Whenever we 'get' account credentials (e.g. via the getters in 'Account'), we sync
 *         the account with the accounts DB. Therefore, DB credentials are prioritized over XML credentials.
 *      3. (DB master only) On world load, we push our XML credentials to the DB. This way, we ensure that
 *         account credentials from the XML file(s) are also present in the accounts DB.
 * 6/13/22, Yoar
 *      Renamed source file to from 'LoginDB.cs' to 'AccountsDatabase.cs'
 *      Refactored completely
 * 9/10/21: Pix
 *      Use SQLiteDataAdapter instead of SQLiteDataReader for connection improvements
 * 9/10/21: Pix
 *      Moved to pooled connection model.
 *      Added SaveAllAccounts()
 * 8/31/2021, Pix:
 *      Added SeedAccountsFromLoginDB functionality for login server
 * 8/30/2021, Pix:
 *      Initial version.
 *      Added logindb functionality.
 */

using System;
using System.Collections;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace Server.Accounting
{
    public static class AccountsDatabase
    {
        public static bool Verbose = true;

        public static string GetDatabasePath(ref bool error)
        {
            string path = Environment.GetEnvironmentVariable("AI.LOGINDB");

            if (string.IsNullOrWhiteSpace(path))
                path = @"C:\AIDB\accounts.db";

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
            const string cmdText = "CREATE TABLE IF NOT EXISTS Account (username TEXT PRIMARY KEY, cryptpassword TEXT, plainpassword TEXT, resetpassword TEXT, hardwarehash INT);";

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

            if (!ColumnExists("hardwarehash"))
                AppendColumn("hardwarehash", "INT");
        }

        private static bool ColumnExists(string columnName)
        {
            const string cmdText = "PRAGMA table_info(Account)";

            bool exists = false;

            using (SQLiteConnection conn = Connect())
            using (SQLiteCommand cmd = new SQLiteCommand(cmdText, conn))
            {
                try
                {
                    conn.Open();

                    SQLiteDataReader reader = cmd.ExecuteReader();

                    int index = reader.GetOrdinal("Name");

                    while (reader.Read())
                    {
                        if (reader.GetString(index).Equals(columnName))
                        {
                            exists = true;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                }
            }

            return exists;
        }

        private static void AppendColumn(string columnName, string dataType)
        {
            string cmdText = string.Format("ALTER TABLE Account ADD COLUMN {0} {1};", columnName, dataType);

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

        public static AccountsDBEntry? GetAccount(string username)
        {
            EnsureTable();

            DataTable table = null;

            const string cmdText = "SELECT cryptpassword, plainpassword, resetpassword, hardwarehash FROM Account WHERE username=@un;";

            using (SQLiteConnection conn = Connect())
            using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmdText, conn))
            {
                try
                {
                    conn.Open();
                    da.SelectCommand.Parameters.AddWithValue("@un", username);
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
                VerboseMessage("Warning: Failed to read accounts.db.");
                return null;
            }

            if (table.Rows.Count == 0)
                return null;

            if (table.Rows.Count != 1)
                VerboseMessage("Warning: Detected duplicate usernames (x{0}) in accounts.db for username '{1}'.", table.Rows.Count, username);

            AccountsDBEntry? result = null;

            try
            {
                DataRow row = table.Rows[0];

                string cryptPassword = row.Field<string>(0);
                string plainPassword = row.Field<string>(1);
                string resetPassword = row.Field<string>(2);

                int hardwareHash;

                if (!row.IsNull(3))
                    hardwareHash = row.Field<int>(3);
                else
                    hardwareHash = 0;

                AccountsDBEntry e = new AccountsDBEntry(username, cryptPassword, plainPassword, resetPassword, hardwareHash);

                result = e;
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            return result;
        }

        private static readonly Account[] m_SingleAccount = new Account[1];

        public static int SaveAccount(Account account)
        {
            m_SingleAccount[0] = account;

            return SaveAccountList(m_SingleAccount);
        }

        public static int SaveAllAccounts()
        {
            return SaveAccountList(Accounts.Table.Values);
        }

        public static int SaveAccountList(ICollection list)
        {
            EnsureTable();

            const string cmdText = "INSERT OR REPLACE INTO Account (username, cryptpassword, plainpassword, resetpassword, hardwarehash) VALUES (@un, @cp, @pp, @rp, @hh);";

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
                            foreach (Account account in list)
                            {
                                string cryptPassword, plainPassword, resetPassword;

                                account.GetRawCredentials(out cryptPassword, out plainPassword, out resetPassword);

                                cmd.Parameters.AddWithValue("@un", account.Username);
                                cmd.Parameters.AddWithValue("@cp", cryptPassword);
                                cmd.Parameters.AddWithValue("@pp", plainPassword);
                                cmd.Parameters.AddWithValue("@rp", resetPassword);
                                cmd.Parameters.AddWithValue("@hh", account.HardwareHashRaw);
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

        public static void SeedAccounts()
        {
            EnsureTable();

            DataTable table = null;

            const string cmdText = "SELECT username, cryptpassword, plainpassword, resetpassword, hardwarehash FROM Account;";

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
                VerboseMessage("Warning: Failed to read accounts.db.");
                return;
            }

            Console.WriteLine("Seeding accounts and syncing passwords from accounts.db.  Total accounts in accounts table: {0}.", Accounts.Table.Count);

            int count = 0;

            foreach (DataRow row in table.Rows)
            {
                try
                {
                    string username = row.Field<string>(0);
                    string cryptPassword = row.Field<string>(1);
                    string plainPassword = row.Field<string>(2);
                    string resetPassword = row.Field<string>(3);

                    int hardwareHash;

                    if (!row.IsNull(4))
                        hardwareHash = row.Field<int>(4);
                    else
                        hardwareHash = 0;

                    AccountsDBEntry dbAccount = new AccountsDBEntry(username, cryptPassword, plainPassword, resetPassword, hardwareHash);

                    Account existingAccount = Accounts.GetAccount(dbAccount.Username);

                    if (existingAccount == null)
                    {
                        Account account = new Account(dbAccount.Username);
                        account.SetRawCredentials(dbAccount.CryptPassword, dbAccount.PlainPassword, dbAccount.ResetPassword);
                        account.HardwareHashRaw = dbAccount.HardwareHash;

                        Accounts.Table[account.Username] = account;

                        count++;
                    }
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                }
            }

            Console.WriteLine("Seeded {0} accounts from accounts.db.  Total accounts in accounts table: {1}.", count, Accounts.Table.Count);
        }

        private static void VerboseMessage(string format, params object[] args)
        {
            if (Verbose)
                Console.WriteLine(format, args);
        }
    }

    public struct AccountsDBEntry
    {
        private string m_Username;
        private string m_CryptPassword;
        private string m_PlainPassword;
        private string m_ResetPassword;
        private int m_HardwareHash;

        public string Username { get { return m_Username; } }
        public string CryptPassword { get { return m_CryptPassword; } }
        public string PlainPassword { get { return m_PlainPassword; } }
        public string ResetPassword { get { return m_ResetPassword; } }
        public int HardwareHash { get { return m_HardwareHash; } }

        public AccountsDBEntry(string username, string cryptPassword, string plainPassword, string resetPassword, int hardwareHash)
        {
            m_Username = username;
            m_CryptPassword = cryptPassword;
            m_PlainPassword = plainPassword;
            m_ResetPassword = resetPassword;
            m_HardwareHash = hardwareHash;
        }
    }
}