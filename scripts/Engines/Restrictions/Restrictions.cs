/***************************************************************************
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

/* Scripts/Engines/Restrictions/Restrictions.cs
 * ChangeLog
 *  11/18/26, Adam (account restrictions)
 *      Created. Every staff decision that restricts or excuses an account or an address, kept in one place, shared by
 *      every shard: a row in the Restrictions table of the accounts database (accounts.db, in AI.DBDIR).
 *          ban               an account may not log in
 *          firewall          nothing from this address connects
 *          ipexception       this address may have more accounts than CoreAI.MaxAccountsPerIP (value: how many)
 *          clientexception   this account (or address) is excused from the ClassicUO/Razor code tests
 *      Each row has a scope: 'all' (every shard) or one shard (its ServerID: AngelIsland, SiegePerilous, ...), who added
 *      it, why, when, and an optional expiry. A row is never deleted: lifting it records when and by whom, so
 *      [restrictions shows the history.
 *      This replaces what each shard kept for itself when Angel Island was one shard, and the half-shared databases that
 *      followed: the Banned flag and ban tags in each shard's accounts.xml (banned on one shard, free on the others),
 *      firewall.cfg and firewalldb.db, ipexception.xml and ipexception.db (switched off in 2022: an exception could only
 *      be global), ClientException.xml (per shard, by address) and its hard-coded account list. PatchV276 moves them in.
 *      Like the rest of accounts.db these are configuration: after a world rollback the database wins.
 *      The questions every check asks: IsBanned, IsFirewalled (cached 30 seconds: it's asked at every connection),
 *      AccountLimit, ClientExempt. Changes go through Add and Lift, and are logged (Restrictions.log). Staff commands are
 *      in RestrictionsCommands.cs.
 *  11/19/26, Adam (cached ban checks)
 *      IsBanned and BanOf use the cached copy (30 s), like IsFirewalled: they read the whole table fresh on every call, so
 *      the Wealthiest leaderboard, which asks it for every account, took 4 seconds and was heard as a hiccup in music.
 *      A login asks IsBannedNow, which reads fresh, so a ban made a moment ago on another shard still counts.
 *  11/21/26, Adam (restrictions robustness)
 *      SetBanDetails lifts the old ban and adds the new one in one transaction: it lifted first, so a failed add left the
 *      account unbanned. A row whose kind this build doesn't know (a newer build shares the table) is skipped; it was read
 *      as a ban. A failed read of the table is retried after 30 seconds, not at every check (each connection asks, and a
 *      locked database can wait 5 seconds per read).
 */

using Server.Accounting;
using Server.Diagnostics;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Net;

namespace Server.Engines
{
    public static class Restrictions
    {
        public enum Kind { Ban, Firewall, IpException, ClientException }

        public const string All = "all";

        public sealed class Row
        {
            public long Id;
            public Kind Kind;
            public string Subject = "";     // an account name, or an address or address pattern ("1.2.3.*")
            public string Scope = All;      // 'all', or a shard's ServerID
            public int Value;               // an IP exception's account limit
            public string Reason = "";
            public string By = "";
            public DateTime Utc;
            public DateTime? Expires;       // null: never
            public DateTime? Lifted;        // null: in force (unless expired)
            public string LiftedBy = "";

            public bool InForce(DateTime now) => Lifted == null && (Expires == null || Expires > now);
            public bool Applies(string shard) => Scope.Equals(All, StringComparison.OrdinalIgnoreCase) || Scope.Equals(shard, StringComparison.OrdinalIgnoreCase);
            public bool IsAddress => LooksLikeAddress(Subject);

            /// <summary>One line, the same shape everywhere (staff, UOAdmin, harnesses).</summary>
            public string Describe()
            {
                string what = Kind switch
                {
                    Kind.Ban => "ban",
                    Kind.Firewall => "firewall",
                    Kind.IpException => string.Format("IP exception ({0} accounts)", Value),
                    _ => "client exception",
                };
                string state = Lifted != null ? string.Format(", LIFTED {0:yyyy-MM-dd HH:mm} by {1}", Lifted, LiftedBy.Length > 0 ? LiftedBy : "?")
                    : Expires != null && Expires <= DateTime.UtcNow ? string.Format(", EXPIRED {0:yyyy-MM-dd HH:mm}", Expires)
                    : Expires != null ? string.Format(", until {0:yyyy-MM-dd HH:mm} UTC", Expires) : "";
                return string.Format("#{0} {1}: {2} {3}, {4}{5}, by {6} {7:yyyy-MM-dd}{8}", Id, what, IsAddress ? "address" : "account", Subject,
                    Scope.Equals(All, StringComparison.OrdinalIgnoreCase) ? "every shard" : Scope, state, By.Length > 0 ? By : "?", Utc,
                    Reason.Length > 0 ? ", \"" + Reason + "\"" : "");
            }
        }

        /// <summary>This shard, as a scope: its ServerID (a Test Center or event shard shares its shard's).</summary>
        public static string ThisShard => Core.ServerID.ToString();

        /// <summary>A scope from what staff type: all/every, here/this, an alias (AI, SP, MO, SE, UP, MC, LS) or a ServerID. Null: unknown.</summary>
        public static string? ParseScope(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            string t = text.Trim().ToLowerInvariant().Replace(" ", "");
            switch (t)
            {
                case "all": case "every": case "everywhere": return All;
                case "here": case "this": return ThisShard;
                case "ai": return ServerID.AngelIsland.ToString();
                case "sp": case "siege": return ServerID.SiegePerilous.ToString();
                case "mo": return ServerID.Mortalis.ToString();
                case "se": return ServerID.Seasons.ToString();
                case "up": return ServerID.Upheaval.ToString();
                case "mc": return ServerID.MusicConservatory.ToString();
                case "ls": return ServerID.LoginServer.ToString();
            }
            foreach (ServerID id in Enum.GetValues(typeof(ServerID)))
                if (id != ServerID.Unknown && id.ToString().ToLowerInvariant() == t)
                    return id.ToString();
            return null;
        }

        public static bool LooksLikeAddress(string s) =>
            !string.IsNullOrWhiteSpace(s) && (IPAddress.TryParse(s, out _) || (s.Contains('.') && s.All(c => char.IsDigit(c) || c == '.' || c == '*' || c == '-')));

        #region The table
        private static readonly object m_Lock = new object();
        private static string? m_SchemaFor;

        private static SQLiteConnection Open()
        {
            bool error = false;
            string path = AccountsDatabase.GetDatabasePath(ref error);
            SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Pooling=True;Max Pool Size=100;BusyTimeout=5000;", path));
            conn.Open();
            lock (m_Lock)
            {
                if (m_SchemaFor != path)
                {
                    using SQLiteCommand cmd = new SQLiteCommand(@"
CREATE TABLE IF NOT EXISTS Restrictions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    kind TEXT NOT NULL,
    subject TEXT NOT NULL COLLATE NOCASE,
    scope TEXT NOT NULL COLLATE NOCASE,
    value INTEGER NOT NULL DEFAULT 0,
    reason TEXT NOT NULL DEFAULT '',
    byWhom TEXT NOT NULL DEFAULT '',
    utc INTEGER NOT NULL,
    expires INTEGER,
    lifted INTEGER,
    liftedBy TEXT NOT NULL DEFAULT '');
CREATE INDEX IF NOT EXISTS RestrictionsSubject ON Restrictions (kind, subject);", conn);
                    cmd.ExecuteNonQuery();
                    m_SchemaFor = path;
                }
            }
            return conn;
        }

        private static List<Row> Read(string where, params (string Name, object Value)[] args)
        {
            List<Row> rows = new List<Row>();
            using SQLiteConnection conn = Open();
            using SQLiteCommand cmd = new SQLiteCommand("SELECT id, kind, subject, scope, value, reason, byWhom, utc, expires, lifted, liftedBy FROM Restrictions " + where + " ORDER BY id", conn);
            foreach (var (name, value) in args)
                cmd.Parameters.AddWithValue(name, value);
            using SQLiteDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                // a kind this build doesn't know (added by a newer build sharing the table) is skipped, never guessed
                if (!Enum.TryParse(r.GetString(1), true, out Kind k) || !Enum.IsDefined(typeof(Kind), k))
                    continue;
                rows.Add(new Row
                {
                    Id = r.GetInt64(0),
                    Kind = k,
                    Subject = r.GetString(2),
                    Scope = r.GetString(3),
                    Value = Convert.ToInt32(r.GetValue(4)),
                    Reason = r.GetString(5),
                    By = r.GetString(6),
                    Utc = new DateTime(r.GetInt64(7), DateTimeKind.Utc),
                    Expires = r.IsDBNull(8) ? null : new DateTime(r.GetInt64(8), DateTimeKind.Utc),
                    Lifted = r.IsDBNull(9) ? null : new DateTime(r.GetInt64(9), DateTimeKind.Utc),
                    LiftedBy = r.GetString(10),
                });
            }
            return rows;
        }

        /// <summary>Every row, history included (null: the database can't be read).</summary>
        public static List<Row>? AllRows()
        {
            try { return Read(""); }
            catch (Exception ex) { LogHelper.LogException(ex, "Restrictions.AllRows"); return null; }
        }

        /// <summary>The rows about a subject (an account, or an address: also the patterns that match it), history included.</summary>
        public static List<Row> About(string subject)
        {
            List<Row>? all = AllRows();
            if (all == null)
                return new List<Row>();
            IPAddress? ip = IPAddress.TryParse(subject, out IPAddress? a) ? a : null;
            return all.Where(r => r.Subject.Equals(subject, StringComparison.OrdinalIgnoreCase)
                || (ip != null && r.IsAddress && Utility.IPMatch(r.Subject, ip))).ToList();
        }

        /// <summary>
        /// Adds a restriction, or renews the one in force with the same kind, subject and scope (its value, reason and
        /// expiry replaced). Returns it, or null when the database can't be written.
        /// </summary>
        public static Row? Add(Kind kind, string subject, string scope, int value, string reason, string by, DateTime? expires)
        {
            subject = subject.Trim();
            if (subject.Length == 0)
                return null;
            try
            {
                long now = DateTime.UtcNow.Ticks;
                long id;
                using (SQLiteConnection conn = Open())
                using (SQLiteTransaction tx = conn.BeginTransaction())
                {
                    long? existing = null;
                    using (SQLiteCommand find = new SQLiteCommand("SELECT id FROM Restrictions WHERE kind=@k AND subject=@s AND scope=@sc AND lifted IS NULL AND (expires IS NULL OR expires > @now) LIMIT 1", conn, tx))
                    {
                        find.Parameters.AddWithValue("@k", kind.ToString());
                        find.Parameters.AddWithValue("@s", subject);
                        find.Parameters.AddWithValue("@sc", scope);
                        find.Parameters.AddWithValue("@now", now);
                        object? o = find.ExecuteScalar();
                        if (o != null && o != DBNull.Value)
                            existing = Convert.ToInt64(o);
                    }
                    using (SQLiteCommand cmd = existing == null
                        ? new SQLiteCommand("INSERT INTO Restrictions (kind, subject, scope, value, reason, byWhom, utc, expires) VALUES (@k, @s, @sc, @v, @r, @b, @u, @e); SELECT last_insert_rowid();", conn, tx)
                        : new SQLiteCommand("UPDATE Restrictions SET value=@v, reason=@r, byWhom=@b, utc=@u, expires=@e WHERE id=@id; SELECT @id;", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@k", kind.ToString());
                        cmd.Parameters.AddWithValue("@s", subject);
                        cmd.Parameters.AddWithValue("@sc", scope);
                        cmd.Parameters.AddWithValue("@v", value);
                        cmd.Parameters.AddWithValue("@r", reason ?? "");
                        cmd.Parameters.AddWithValue("@b", by ?? "");
                        cmd.Parameters.AddWithValue("@u", now);
                        cmd.Parameters.AddWithValue("@e", expires == null ? DBNull.Value : expires.Value.Ticks);
                        if (existing != null)
                            cmd.Parameters.AddWithValue("@id", existing.Value);
                        id = Convert.ToInt64(cmd.ExecuteScalar());
                    }
                    tx.Commit();
                }
                Invalidate();
                Row? row = Read("WHERE id=@id", ("@id", id)).FirstOrDefault();
                if (row != null)
                    Log("added " + row.Describe());
                return row;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Restrictions.Add");
                return null;
            }
        }

        /// <summary>
        /// Lifts the restrictions in force of this kind on this subject (in this scope; null: every scope). The rows stay,
        /// marked lifted. Returns how many (-1: the database can't be written).
        /// </summary>
        public static int Lift(Kind kind, string subject, string? scope, string by)
        {
            try
            {
                List<Row> lifted = InForce(kind, subject.Trim(), scope);
                if (lifted.Count == 0)
                    return 0;
                using (SQLiteConnection conn = Open())
                using (SQLiteTransaction tx = conn.BeginTransaction())
                {
                    foreach (Row r in lifted)
                    {
                        using SQLiteCommand cmd = new SQLiteCommand("UPDATE Restrictions SET lifted=@l, liftedBy=@b WHERE id=@id", conn, tx);
                        cmd.Parameters.AddWithValue("@l", DateTime.UtcNow.Ticks);
                        cmd.Parameters.AddWithValue("@b", by ?? "");
                        cmd.Parameters.AddWithValue("@id", r.Id);
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
                Invalidate();
                foreach (Row r in lifted)
                    Log(string.Format("lifted #{0} ({1} {2}, {3}) by {4}", r.Id, r.Kind, r.Subject, r.Scope, by));
                return lifted.Count;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Restrictions.Lift");
                return -1;
            }
        }

        /// <summary>Sets an in-force ban's details (the ban gump: scope, expiry, reason). False: no ban in force, or unwritable.</summary>
        public static bool SetBanDetails(string account, string scope, DateTime? expires, string reason, string by)
        {
            account = account.Trim();
            List<Row> bans;
            try { bans = InForce(Kind.Ban, account, null); }
            catch (Exception ex) { LogHelper.LogException(ex, "Restrictions.SetBanDetails"); return false; }
            if (bans.Count == 0)
                return false;
            // one ban per account: the gump's scope replaces whatever the ban was added with. The old rows are lifted and
            //  the new one added in one transaction: a failure leaves the old ban in force (lifting first and then failing
            //  to add left the account unbanned).
            try
            {
                long now = DateTime.UtcNow.Ticks;
                long id;
                using (SQLiteConnection conn = Open())
                using (SQLiteTransaction tx = conn.BeginTransaction())
                {
                    foreach (Row r in bans)
                    {
                        using SQLiteCommand lift = new SQLiteCommand("UPDATE Restrictions SET lifted=@l, liftedBy=@b WHERE id=@id", conn, tx);
                        lift.Parameters.AddWithValue("@l", now);
                        lift.Parameters.AddWithValue("@b", by ?? "");
                        lift.Parameters.AddWithValue("@id", r.Id);
                        lift.ExecuteNonQuery();
                    }
                    using (SQLiteCommand add = new SQLiteCommand("INSERT INTO Restrictions (kind, subject, scope, value, reason, byWhom, utc, expires) VALUES (@k, @s, @sc, 0, @r, @b, @u, @e); SELECT last_insert_rowid();", conn, tx))
                    {
                        add.Parameters.AddWithValue("@k", Kind.Ban.ToString());
                        add.Parameters.AddWithValue("@s", account);
                        add.Parameters.AddWithValue("@sc", scope);
                        add.Parameters.AddWithValue("@r", reason ?? "");
                        add.Parameters.AddWithValue("@b", by ?? "");
                        add.Parameters.AddWithValue("@u", now);
                        add.Parameters.AddWithValue("@e", expires == null ? DBNull.Value : expires.Value.Ticks);
                        id = Convert.ToInt64(add.ExecuteScalar());
                    }
                    tx.Commit();
                }
                Invalidate();
                foreach (Row r in bans)
                    Log(string.Format("lifted #{0} ({1} {2}, {3}) by {4}", r.Id, r.Kind, r.Subject, r.Scope, by));
                Row? row = Read("WHERE id=@id", ("@id", id)).FirstOrDefault();
                if (row != null)
                    Log("added " + row.Describe());
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Restrictions.SetBanDetails");
                return false;
            }
        }

        private static List<Row> InForce(Kind kind, string subject, string? scope)
        {
            DateTime now = DateTime.UtcNow;
            return Read("WHERE kind=@k AND subject=@s AND lifted IS NULL", ("@k", kind.ToString()), ("@s", subject))
                .Where(r => r.InForce(now) && (scope == null || r.Scope.Equals(scope, StringComparison.OrdinalIgnoreCase))).ToList();
        }
        #endregion The table

        #region The questions
        // what's in force, read at most every 30 seconds (a change on this shard is seen at once; on another, within 30 s)
        private static List<Row> m_Cache = new List<Row>();
        private static DateTime m_CacheRead = DateTime.MinValue;
        public static readonly TimeSpan CacheTime = TimeSpan.FromSeconds(30);

        public static void Invalidate()
        {
            lock (m_Lock)
                m_CacheRead = DateTime.MinValue;
        }

        private static List<Row> InForceNow()
        {
            lock (m_Lock)
            {
                if (DateTime.UtcNow - m_CacheRead < CacheTime)
                    return m_Cache;
            }
            try
            {
                List<Row> rows = Read("WHERE lifted IS NULL");
                lock (m_Lock)
                {
                    m_Cache = rows;
                    m_CacheRead = DateTime.UtcNow;
                    return m_Cache;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Restrictions: reading the table");
                lock (m_Lock)
                {
                    // try again in 30 seconds, not at the next check: every connection asks, and each read of a locked
                    //  database can wait its busy timeout (5 s) on the main thread
                    m_CacheRead = DateTime.UtcNow;
                    return m_Cache;     // the last known (empty at startup: nothing is refused on a guess)
                }
            }
        }

        private static IEnumerable<Row> Applying(Kind kind)
        {
            DateTime now = DateTime.UtcNow;
            string shard = ThisShard;
            return InForceNow().Where(r => r.Kind == kind && r.InForce(now) && r.Applies(shard));
        }

        /// <summary>Is this account banned here (a ban for every shard, or this one)?</summary>
        public static bool IsBanned(string? account) =>
            BanOf(account) != null;

        /// <summary>For a login: read fresh, so a ban made a moment ago on another shard counts.</summary>
        public static bool IsBannedNow(string? account)
        {
            Invalidate();
            return IsBanned(account);
        }

        /// <summary>The ban in force on this account here, or null.</summary>
        public static Row? BanOf(string? account)
        {
            if (string.IsNullOrWhiteSpace(account))
                return null;
            return Applying(Kind.Ban).FirstOrDefault(r => r.Subject.Equals(account.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Is this address firewalled (asked at every connection: the cached copy)?</summary>
        public static bool IsFirewalled(IPAddress? ip) =>
            ip != null && Applying(Kind.Firewall).Any(r => Utility.IPMatch(r.Subject, ip));

        public static bool IsFirewalled(string ip) =>
            IPAddress.TryParse(ip, out IPAddress? a) && IsFirewalled(a);

        /// <summary>How many accounts this address may have here: its IP exception's, else CoreAI.MaxAccountsPerIP.</summary>
        public static int AccountLimit(IPAddress? ip)
        {
            int limit = CoreAI.MaxAccountsPerIP;
            if (ip == null)
                return limit;
            foreach (Row r in Applying(Kind.IpException))
                if (Utility.IPMatch(r.Subject, ip) && r.Value > limit)
                    limit = r.Value;
            return limit;
        }

        /// <summary>Is this account (or this address: the exceptions moved in from ClientException.xml) excused from the code tests?</summary>
        public static bool ClientExempt(string? account, IPAddress? ip) =>
            Applying(Kind.ClientException).Any(r => r.IsAddress
                ? ip != null && Utility.IPMatch(r.Subject, ip)
                : account != null && r.Subject.Equals(account, StringComparison.OrdinalIgnoreCase));

        /// <summary>For [restrictions report: what's in force here, by kind.</summary>
        public static string Summary()
        {
            List<Row>? all = AllRows();
            if (all == null)
                return "the accounts database can't be read";
            DateTime now = DateTime.UtcNow;
            string shard = ThisShard;
            var here = all.Where(r => r.InForce(now) && r.Applies(shard)).ToList();
            return string.Format("{0} bans, {1} firewalled addresses, {2} IP exceptions, {3} client exceptions in force here ({4} rows in all, history included)",
                here.Count(r => r.Kind == Kind.Ban), here.Count(r => r.Kind == Kind.Firewall), here.Count(r => r.Kind == Kind.IpException),
                here.Count(r => r.Kind == Kind.ClientException), all.Count);
        }
        #endregion The questions

        public static void Log(string text)
        {
            try
            {
                using LogHelper logger = new LogHelper("Restrictions.log", false, true);
                logger.Log(LogType.Text, text);
            }
            catch { }
        }
    }
}
