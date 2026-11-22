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

/* Scripts\Engines\ReservedNames\NameRegistry.cs
 * ChangeLog
 *  11/17/26, Adam (family-wide names, step 1)
 *      Created. Character names will be unique across every Angel Island shard: a name belongs to an account, on every
 *      shard, for good (a wipe, of a world or a season, never frees it). The registry is shared by all shards, beside
 *      the accounts and music databases: SQLite at AI.NAMESDB (default C:\AIDB\names.db), created on first use. A name's
 *      key ignores case, spaces and punctuation (Key: "Gal ois." is galois), so look-alikes are the same name.
 *      Step 1 (this): the registry's schema and key, and the read-only audit:
 *          [Names audit          this shard's names (its accounts' characters and its reserved names) to
 *                                C:\AIDB\names-audit\<shard>.csv; nothing in the world or the registry changes
 *          [Names audit report   every shard's audit with the player history archive (data\AIPlayerHistory: names
 *                                without accounts) to Logs\names-report.csv (one line per name: its owners, shards and
 *                                what kind of clash it is) and Logs\names-junk.csv (junk candidates, for Adam to approve)
 *      Nothing is enforced yet: seeding, the check at character creation and rename, and staff tools come next.
 *      CharacterCreation.DefaultName ("Generic Player", what a character is called when its chosen name isn't available)
 *      is a placeholder, not a name: never anyone's (Placeholder), so it's left out of the audit and the registry, and
 *      any number of characters may have it. Test Center and event shards aren't audited: they're copies of a player
 *      shard (Test Center even calls itself Upheaval, and overwrote Upheaval's audit).
 *  11/17/26, Adam (family-wide names, step 3)
 *      The registry decides (Enforced: player shards; Test Center, event shards and the Login Server keep out of it). A
 *      name is free for an account that owns it (grandfathered names keep every owner) or when nobody does; an archive
 *      name (owner unknown) is free for nobody until staff assign it (FreeFor). Claim takes a name in one transaction;
 *      a renamed character's old name stays its account's (nothing is ever freed but by staff). Unreadable: refused
 *      (fail closed). Seed (PatchV272): each player shard's names, then the archive's from the Login Server; the
 *      placeholder and the approved junk are skipped. Staff: [Names who <name>, [Names assign <account> <name>,
 *      [Names release <account|unknown|all> <name>.
 *  11/18/26, Adam (names cleanup)
 *      The registry is the only list of names: the per-shard Reserved Names (ReservedNames.cs, Saves\Accounts\Reserved
 *      Names.json) is retired (PatchV275), and [ResolveReservedName with it. [Names assign <account> <name> take makes
 *      the name that account's alone (Take: every other owner's row goes, the archive's too) and, on this shard,
 *      renames other accounts' live characters with it to the placeholder with a personal name change deed (never
 *      deleted), the Seasons leaderboard name following (RenameOthers; Names.log). Said on R:All, every shard renames.
 *      Without take, assign adds the account as one more owner, as before.
 *  11/18/26, Adam (names: a take reaches every shard)
 *      The registry remembers what staff took (Taken: the name, the account it was taken from, by whom, where, when):
 *      take records every account that loses the name, and release <account> records that account; assigning the name
 *      back (assign, take, or a claim after release all) clears its row, and release all clears the name's rows (nobody
 *      is entitled to it, so nobody is renamed for having it). Every player shard carries it out: at startup
 *      (SweepTaken: its whole roster) and at login (OnLogin), a character whose name was taken from its account is
 *      renamed 'Generic Player' with a personal name change deed, told why if online, and logged; the Seasons
 *      leaderboard name follows. Only for a Taken row (MustRename): grandfathered owners, junk names, Test Center and
 *      an unreadable registry never rename anyone. [Names who lists the Taken rows. [set name, refused because another
 *      account holds the name, offers an Owner the take (TakePreview, ConfirmTake; Commands.cs).
 *  11/18/26, Adam (names: a new file gets its tables)
 *      Open made the schema once per path, so a registry file deleted or replaced while the shard ran came back without
 *      tables (every read and claim failed, closed, until a restart; NameRegistryTests failed on a second run). Creating
 *      the file now forgets that the path's schema was made.
 *  11/18/26, Adam (one database folder)
 *      names.db lives in the one database folder (Server\DatabaseFolder.cs: AI.DBDIR, else <shard>\Databases); AI.NAMESDB
 *      is retired.
 *  11/18/26, Adam (names: online characters too)
 *      A character online when its name is taken elsewhere is renamed within 5 minutes: the cron (CNamesTaken) reads the
 *      Taken list and checks only the characters online (ApplyTakenOnline). Startup still covers the offline, login
 *      the ones arriving.
 *  11/20/26, Adam (NamesTaken database polling)
 *      The five-minute online check skips a busy or locked registry and retries on its next run.
 *      The skip is logged and [cron stats shows the database read time separately. Startup and login
 *      keep the normal registry read.
 *  11/21/26, Adam (NamesTaken on a new registry file)
 *      The five-minute check reads the normal way until this shard has made the registry's tables (a new or replaced
 *      names.db): its read-only, no-wait read can't make them, so it failed with "no such table" every run. (The busy
 *      skip goes to the console, not a log file.)
 */

using Server.Accounting;
using Server.Commands;
using Server.Diagnostics;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace Server.Engines
{
    public static class NameRegistry
    {
        #region The registry
        /// <summary>For tests: a database file other than AI.NAMESDB / the default.</summary>
        public static string? PathOverride { get; set; }

        public static string DatabasePath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PathOverride))
                    return PathOverride!;
                // the one database folder (AI.DBDIR, else <shard>\Databases): AI.NAMESDB is retired
                return DatabaseFolder.PathOf("names.db");
            }
        }

        /// <summary>Where each shard's audit goes (beside the registry): names-audit\&lt;shard&gt;.csv.</summary>
        public static string AuditFolder => Path.Combine(Path.GetDirectoryName(DatabasePath) ?? ".", "names-audit");

        private static readonly object m_SchemaLock = new object();
        private static string? m_SchemaReadyFor;

        /// <summary>
        /// The registry: one row per name and owner. key: the name's key (Key); owner: the owning account's username, or
        /// "" when it isn't known (the player history archive); name: as written; shard and source say where it was seen
        /// (live, reserved, archive, claim). A name is free for an account when no row has its key under another owner.
        /// </summary>
        internal static SQLiteConnection Open()
        {
            string path = DatabasePath;
            if (!File.Exists(path))
            {
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                SQLiteConnection.CreateFile(path);
                // a new file has no tables, even at a path whose schema was made before (the file was deleted or replaced)
                lock (m_SchemaLock)
                    m_SchemaReadyFor = null;
            }

            SQLiteConnection conn = new SQLiteConnection(string.Format(
                "Data Source={0};Pooling=True;Max Pool Size=100;Journal Mode=WAL;BusyTimeout=5000;", path));
            conn.Open();
            lock (m_SchemaLock)
            {
                if (m_SchemaReadyFor != path)
                {
                    using SQLiteCommand cmd = new SQLiteCommand(@"
CREATE TABLE IF NOT EXISTS Meta (key TEXT PRIMARY KEY, value TEXT);
CREATE TABLE IF NOT EXISTS Names (
    key TEXT NOT NULL,
    owner TEXT NOT NULL COLLATE NOCASE,
    name TEXT NOT NULL,
    shard TEXT NOT NULL,
    source TEXT NOT NULL,
    utc INTEGER NOT NULL,
    PRIMARY KEY (key, owner));
CREATE TABLE IF NOT EXISTS Taken (
    key TEXT NOT NULL,
    owner TEXT NOT NULL COLLATE NOCASE,
    name TEXT NOT NULL,
    byWhom TEXT NOT NULL,
    shard TEXT NOT NULL,
    utc INTEGER NOT NULL,
    PRIMARY KEY (key, owner));
INSERT OR IGNORE INTO Meta (key, value) VALUES ('schema', '1');", conn);
                    cmd.ExecuteNonQuery();
                    m_SchemaReadyFor = path;
                }
            }
            return conn;
        }

        /// <summary>
        /// A name's key: lowercase letters and digits only, so case, spaces and punctuation don't make a different name
        /// ("Gal ois.", "GALOIS" and "Galois" are galois). Empty when the name has no letter or digit.
        /// </summary>
        public static string Key(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;
            StringBuilder sb = new StringBuilder(name.Length);
            foreach (char ch in name)
                if (char.IsLetterOrDigit(ch))
                    sb.Append(char.ToLowerInvariant(ch));
            return sb.ToString();
        }
        #endregion The registry

        #region The audit
        public static void Initialize()
        {
            CommandSystem.Register("Names", AccessLevel.Owner, new CommandEventHandler(Names_OnCommand));
            // a name staff took from an account: its characters here are renamed now, and at login after a later take
            EventSink.Login += new LoginEventHandler(OnLogin);
            try
            {
                int renamed = SweepTaken();
                if (renamed > 0)
                    Utility.Monitor.WriteLine("Names: {0} character(s) renamed '{1}' with a name change deed: their names were taken from their accounts (Names.log).",
                        ConsoleColor.Yellow, renamed, Server.Misc.CharacterCreation.DefaultName);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "NameRegistry.SweepTaken");
            }
        }

        /// <summary>
        /// The placeholder name a character gets when its chosen name isn't available (CharacterCreation.DefaultName,
        /// "Generic Player"): never anyone's, so never audited, registered or refused.
        /// </summary>
        public static bool Placeholder(string? name) => Key(name) == Key(Server.Misc.CharacterCreation.DefaultName);

        /// <summary>A name seen on a shard: as written, its key, its account ("" unknown), and where.</summary>
        public sealed class AuditRow
        {
            public string Shard = string.Empty;
            public string Name = string.Empty;
            public string Key = string.Empty;
            public string Owner = string.Empty;         // username; "" for the archive
            public string Source = string.Empty;        // live, reserved, archive
            public int Serial;                          // a live character's (0: none)
            public DateTime LastLogin = DateTime.MinValue;  // the account's
            public string Access = string.Empty;        // the account's access level
        }

        /// <summary>This shard's names: every account's characters (live) and its reserved names (Reserved Names.json).</summary>
        public static List<AuditRow> ShardNames()
        {
            List<AuditRow> rows = new List<AuditRow>();
            Dictionary<string, Account> byName = new Dictionary<string, Account>(StringComparer.OrdinalIgnoreCase);
            foreach (Account acct in Accounts.Table.Values.OfType<Account>())
            {
                if (string.IsNullOrWhiteSpace(acct.Username))
                    continue;
                byName[acct.Username] = acct;
                for (int i = 0; i < acct.Length; i++)
                {
                    Mobile m = acct[i];
                    if (m == null || m.Deleted || string.IsNullOrWhiteSpace(m.Name))
                        continue;
                    rows.Add(new AuditRow
                    {
                        Shard = Core.Server, Name = m.Name.Trim(), Key = Key(m.Name), Owner = acct.Username, Source = "live",
                        Serial = m.Serial.Value, LastLogin = acct.LastLogin, Access = acct.AccessLevel.ToString()
                    });
                }
            }
            foreach (KeyValuePair<string, List<string>> kvp in ReservedNames.Database)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value == null)
                    continue;
                byName.TryGetValue(kvp.Key, out Account? acct);
                foreach (string name in kvp.Value)
                    if (!string.IsNullOrWhiteSpace(name))
                        rows.Add(new AuditRow
                        {
                            Shard = Core.Server, Name = name.Trim(), Key = Key(name), Owner = kvp.Key, Source = "reserved",
                            LastLogin = acct?.LastLogin ?? DateTime.MinValue, Access = acct?.AccessLevel.ToString() ?? ""
                        });
            }
            return rows.Where(r => r.Key.Length > 0 && !Placeholder(r.Name)).ToList();
        }

        /// <summary>Writes this shard's names to the audit folder (C:\AIDB\names-audit\&lt;shard&gt;.csv). Read-only otherwise.</summary>
        public static string WriteShardAudit(out int count)
        {
            List<AuditRow> rows = ShardNames();
            count = rows.Count;
            Directory.CreateDirectory(AuditFolder);
            string path = Path.Combine(AuditFolder, Core.Server + ".csv");
            List<string> lines = new List<string> { "Shard,Name,Key,Owner,Source,Serial,LastLogin,Access" };
            foreach (AuditRow r in rows)
                lines.Add(string.Join(",", Csv(r.Shard), Csv(r.Name), Csv(r.Key), Csv(r.Owner), Csv(r.Source), r.Serial.ToString(),
                    r.LastLogin == DateTime.MinValue ? "" : r.LastLogin.ToString("yyyy-MM-dd"), Csv(r.Access)));
            File.WriteAllLines(path, lines, new UTF8Encoding(false));
            return path;
        }

        /// <summary>Every shard's audit file in the audit folder.</summary>
        public static List<AuditRow> ReadAudits(string folder)
        {
            List<AuditRow> rows = new List<AuditRow>();
            if (!Directory.Exists(folder))
                return rows;
            foreach (string path in Directory.GetFiles(folder, "*.csv"))
                foreach (string line in File.ReadAllLines(path).Skip(1))
                {
                    List<string> f = SplitCsv(line);
                    if (f.Count < 8)
                        continue;
                    rows.Add(new AuditRow
                    {
                        Shard = f[0], Name = f[1], Key = f[2], Owner = f[3], Source = f[4], Serial = int.TryParse(f[5], out int s) ? s : 0,
                        LastLogin = DateTime.TryParse(f[6], out DateTime d) ? d : DateTime.MinValue, Access = f[7]
                    });
                }
            return rows;
        }

        /// <summary>The player history archive's names: no accounts (owner unknown), every shard it recorded.</summary>
        public static List<AuditRow> ArchiveNames()
        {
            List<AuditRow> rows = new List<AuditRow>();
            foreach (string path in PlayerHistory.ArchiveFiles())
                try
                {
                    foreach (PlayerHistory.Record r in PlayerHistory.ReadFile(path).Records)
                        if (!string.IsNullOrWhiteSpace(r.Name) && Key(r.Name).Length > 0 && !Placeholder(r.Name))
                            rows.Add(new AuditRow { Shard = r.Shard, Name = r.Name.Trim(), Key = Key(r.Name), Owner = "", Source = "archive", Serial = r.Serial, LastLogin = r.LastLogin });
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "NameRegistry.ArchiveNames " + path);
                }
            return rows;
        }

        /// <summary>One name across the family: its key, its spellings, its owners, and what kind of clash it is.</summary>
        public sealed class ReportLine
        {
            public string Key = string.Empty;
            public List<string> Spellings = new List<string>();
            public List<string> Owners = new List<string>();        // usernames (the archive's unknown owner isn't one)
            public List<string> Shards = new List<string>();
            public bool InArchive;
            public string Kind = string.Empty;
            public string Junk = string.Empty;                      // why it looks like junk ("" not)
        }

        /// <summary>
        /// The report: every key, with its owners and shards. Kinds: one owner; same account on several shards (fine);
        /// owners on different shards (cross-shard clash: grandfathered, every owner keeps it); owners on the same shard
        /// (a clash from before reservations); archive only (an old player's name, owner unknown: blocked); archive and
        /// owners (a living name that an old player had too).
        /// </summary>
        public static List<ReportLine> Report(IEnumerable<AuditRow> shardRows, IEnumerable<AuditRow> archiveRows)
        {
            Dictionary<string, ReportLine> lines = new Dictionary<string, ReportLine>();
            Dictionary<string, Dictionary<string, HashSet<string>>> ownersByShard = new();   // key -> shard -> owners
            foreach (AuditRow r in shardRows.Concat(archiveRows))
            {
                if (r.Key.Length == 0 || Placeholder(r.Name))
                    continue;
                if (!lines.TryGetValue(r.Key, out ReportLine? line))
                    lines[r.Key] = line = new ReportLine { Key = r.Key };
                if (!line.Spellings.Contains(r.Name))
                    line.Spellings.Add(r.Name);
                if (!line.Shards.Contains(r.Shard))
                    line.Shards.Add(r.Shard);
                if (r.Source == "archive")
                {
                    line.InArchive = true;
                    continue;
                }
                if (!line.Owners.Contains(r.Owner, StringComparer.OrdinalIgnoreCase))
                    line.Owners.Add(r.Owner);
                if (!ownersByShard.TryGetValue(r.Key, out var shards))
                    ownersByShard[r.Key] = shards = new Dictionary<string, HashSet<string>>();
                if (!shards.TryGetValue(r.Shard, out var owners))
                    shards[r.Shard] = owners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                owners.Add(r.Owner);
            }
            foreach (ReportLine line in lines.Values)
            {
                bool withinShard = ownersByShard.TryGetValue(line.Key, out var shards) && shards.Values.Any(o => o.Count > 1);
                line.Kind = line.Owners.Count == 0 ? "archive only"
                    : withinShard ? "clash on one shard"
                    : line.Owners.Count > 1 ? "clash across shards"
                    : (shards?.Count ?? 0) > 1 ? "same account, several shards"
                    : "one owner";
                if (line.InArchive && line.Owners.Count > 0)
                    line.Kind += ", in the archive";
                line.Junk = JunkReason(line.Key);
            }
            return lines.Values.OrderBy(l => l.Key, StringComparer.Ordinal).ToList();
        }

        /// <summary>Why a name looks like junk (staff test characters and the like), or "": a candidate list for Adam.</summary>
        public static string JunkReason(string key)
        {
            if (key.Length < 3)
                return "under 3 letters";
            if (key.Contains("test"))
                return "contains 'test'";
            if (key.All(char.IsDigit))
                return "digits only";
            return string.Empty;
        }

        public static (string Report, string Junk) WriteReport(List<ReportLine> lines)
        {
            string report = Path.Combine(Core.LogsDirectory, "names-report.csv");
            string junk = Path.Combine(Core.LogsDirectory, "names-junk.csv");
            List<string> r = new List<string> { "Key,Kind,Owners,Shards,Spellings,Junk" };
            foreach (ReportLine l in lines)
                r.Add(string.Join(",", Csv(l.Key), Csv(l.Kind), Csv(string.Join(" ", l.Owners)), Csv(string.Join("; ", l.Shards)),
                    Csv(string.Join(" | ", l.Spellings)), Csv(l.Junk)));
            File.WriteAllLines(report, r, new UTF8Encoding(false));
            // junk candidates: Skip is Y (skipped when seeding) until Adam says otherwise
            List<string> j = new List<string> { "Key,Skip,Reason,Kind,Owners,Spellings" };
            foreach (ReportLine l in lines.Where(l => l.Junk.Length > 0))
                j.Add(string.Join(",", Csv(l.Key), "Y", Csv(l.Junk), Csv(l.Kind), Csv(string.Join(" ", l.Owners)), Csv(string.Join(" | ", l.Spellings))));
            File.WriteAllLines(junk, j, new UTF8Encoding(false));
            return (report, junk);
        }

        /*  [Names: how staff use it (Owner)
         *
         *  The rule: a name belongs to the accounts the registry lists as its owners, on every Angel Island shard. Any owner
         *  may use it; nobody else may. A name with several owners is "grandfathered": each keeps it. A name whose only owner
         *  is unknown (an old player from the history archive) is nobody's until staff assign it. Nothing frees a name but
         *  staff.
         *
         *  [Names who <name>
         *      Who owns it (account, shard, how it was registered), and who it was taken from (by whom, where, when).
         *
         *  [Names assign <account> <name>
         *      Adds the account as one more owner; the others keep it. Nobody is renamed.
         *      - An old player's archive name: makes it usable by that account.
         *      - "Grandfather" (undoing a take): staff took flarp from dinky for morgoth, and dinky wants it back:
         *            [Names assign dinky flarp
         *        dinky and morgoth both own flarp now (grandfathered), and dinky's Taken row is cleared, so dinky's
         *        characters are no longer renamed at startup, login or by the cron. dinky's character, already renamed
         *        'Generic Player', takes flarp back with the name change deed it was given, or with staff's [set name flarp.
         *
         *  [Names assign <account> <name> take
         *      The account becomes the name's ONLY owner: every other owner loses it (the archive's unknown owner too), and
         *      each account that lost it is remembered (Taken). Their characters with the name are renamed 'Generic Player'
         *      and given a personal name change deed (never deleted): on this shard at once; on every other shard within 5
         *      minutes if online (cron CNamesTaken), at login, or at startup.
         *      - Undoing a take completely: take it back the other way ([Names assign dinky flarp take); morgoth is then the
         *        one renamed, with a deed.
         *      - [set name <name> on a player, refused because another account owns the name, offers an Owner this same
         *        take in a confirmation gump (owners, last login, who here would be renamed).
         *
         *  [Names release <account> <name>
         *      That account no longer owns it, and is remembered (Taken): its characters with the name are renamed, as above.
         *  [Names release unknown <name>
         *      Removes the archive's unknown owner (the old player's claim); account owners are untouched.
         *  [Names release all <name>
         *      Nobody owns it: anyone may take it next. Its Taken rows go too (nobody is renamed for having a free name).
         *
         *  [Names audit / [Names audit report
         *      Read-only: this shard's names to C:\AIDB\names-audit\<shard>.csv; the report combines every shard's audit with
         *      the archive into Logs\names-report.csv (owners, shards, clashes) and Logs\names-junk.csv. Run the report once,
         *      on any shard.
         *
         *  Also: no two live characters on one shard share a name (NameVerification), whatever the registry says; the
         *  placeholder 'Generic Player' is never anyone's; Test Center and event shards keep out of the registry.
         */
        [Usage("Names who <name> | assign <account> <name> [take] | release <account|unknown|all> <name> | audit [report]")]
        [Description("Family-wide names: who owns a name; assign it to an account (take: that account's alone; other accounts' characters with it on this shard are renamed 'Generic Player' with a name change deed); release it (from an account, the archive's unknown owner, or everyone). audit: this shard's names (its accounts' characters and reserved names) to C:\\AIDB\\names-audit\\<shard>.csv. audit report: every shard's audit and the player history archive to Logs\\names-report.csv (owners, shards, clashes) and Logs\\names-junk.csv (junk candidates to approve).")]
        private static void Names_OnCommand(CommandEventArgs e)
        {
            string a0 = e.Arguments.Length > 0 ? e.Arguments[0].ToLowerInvariant() : "";
            string a1 = e.Arguments.Length > 1 ? e.Arguments[1].ToLowerInvariant() : "";
            try
            {
                if (a0 == "audit" && a1 == "")
                {
                    if (Core.RuleSets.LoginServerRules() || Core.GMNTC_CFG || Core.GMNEV_CFG)
                    {
                        e.Mobile.SendMessage("Names: {0} isn't audited (no players of its own): audit each player shard ([Names audit), then [Names audit report here or anywhere.",
                            Core.RuleSets.LoginServerRules() ? "the Login Server" : Core.GMNTC_CFG ? "Test Center" : "an event shard");
                        return;
                    }
                    string path = WriteShardAudit(out int count);
                    e.Mobile.SendMessage("Names: {0} names on {1} written to {2} (nothing else changed).", count, Core.Server, path);
                }
                else if (a0 == "audit" && a1 == "report")
                {
                    List<AuditRow> shards = ReadAudits(AuditFolder);
                    List<AuditRow> archive = ArchiveNames();
                    List<ReportLine> lines = Report(shards, archive);
                    (string report, string junk) = WriteReport(lines);
                    string audited = string.Join(", ", shards.Select(r => r.Shard).Distinct().OrderBy(s => s));
                    e.Mobile.SendMessage("Names: {0} names from {1} (+ {2} archive records): {3}. Report {4}; junk candidates {5} ({6}).",
                        lines.Count, audited.Length > 0 ? audited : "no shard audits yet", archive.Count,
                        string.Join(", ", lines.GroupBy(l => l.Kind).OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count())),
                        report, junk, lines.Count(l => l.Junk.Length > 0));
                }
                else if (a0 == "who" && e.Arguments.Length > 1)
                {
                    string name = string.Join(" ", e.Arguments.Skip(1));
                    var rows = Owners(name);
                    if (rows == null)
                        e.Mobile.SendMessage("Names: the registry can't be read ({0}).", DatabasePath);
                    else if (Placeholder(name))
                        e.Mobile.SendMessage("Names: '{0}' is the placeholder: never anyone's.", name);
                    else if (rows.Count == 0)
                        e.Mobile.SendMessage("Names: '{0}' (key {1}) is free{2}.", name, Key(name), JunkReason(Key(name)).Length > 0 ? " (junk: never seeded)" : "");
                    else
                        foreach (var r in rows)
                            e.Mobile.SendMessage("Names: '{0}' (key {1}): {2}, as '{3}' on {4} ({5}).", name, Key(name),
                                r.Owner.Length > 0 ? r.Owner : "unknown owner (the archive)", r.Name, r.Shard, r.Source);
                    foreach (var t in TakenRows(name) ?? new())
                        e.Mobile.SendMessage("Names: '{0}' was taken from {1}{2} on {3}, {4:yyyy-MM-dd} (its characters lose it at startup or login).", name, t.Owner,
                            t.By.Length > 0 ? " by " + t.By : "", t.Shard, t.Utc);
                }
                else if (a0 == "assign" && e.Arguments.Length > 2)
                {
                    // assign <account> <name> [take]: without take the account is one more owner; with it, the only one
                    bool take = e.Arguments.Length > 3 && e.Arguments[^1].Equals("take", StringComparison.OrdinalIgnoreCase);
                    string account = e.Arguments[1], name = string.Join(" ", e.Arguments.Skip(2).Take(e.Arguments.Length - (take ? 3 : 2)));
                    if (Accounts.GetAccount(account) == null)
                        e.Mobile.SendMessage("Names: no account '{0}' here.", account);
                    else if (Placeholder(name) || Key(name).Length == 0)
                        e.Mobile.SendMessage("Names: '{0}' can't be anyone's.", name);
                    else if (!take)
                        e.Mobile.SendMessage(Assign(name, account) ? "Names: '{0}' is {1}'s now (other owners keep theirs; add take to make it {1}'s alone)." : "Names: the registry can't be written.", name, account);
                    else if (!Take(name, account, e.Mobile.Name))
                        e.Mobile.SendMessage("Names: the registry can't be written.");
                    else
                    {
                        TakeResult t = RenameOthers(name, account);
                        e.Mobile.SendMessage("Names: '{0}' is {1}'s alone now, on every shard. Here, on {2}: {3} character(s) of other accounts renamed '{4}' with a name change deed{5}{6}.",
                            name, account, Core.Server, t.Renamed, Server.Misc.CharacterCreation.DefaultName,
                            t.DeedFailures > 0 ? string.Format(" ({0} deed(s) couldn't be placed)", t.DeedFailures) : "",
                            t.DisplayNamesReset + t.DisplayNamesAwarded > 0 ? string.Format("; leaderboard names: {0} reset, {1} awarded", t.DisplayNamesReset, t.DisplayNamesAwarded) : "");
                        e.Mobile.SendMessage("Names: other shards rename their characters with it at startup, or when they log in.");
                    }
                }
                else if (a0 == "release" && e.Arguments.Length > 2)
                {
                    string who = e.Arguments[1], name = string.Join(" ", e.Arguments.Skip(2));
                    string? owner = who.ToLowerInvariant() == "all" ? null : who.ToLowerInvariant() == "unknown" ? "" : who;
                    int n = Release(name, owner, e.Mobile.Name);
                    e.Mobile.SendMessage(n < 0 ? "Names: the registry can't be written." : "Names: '{0}' released from {1} ({2} row(s)).", name, owner == null ? "every owner" : owner.Length == 0 ? "the unknown owner" : owner, n);
                }
                else
                    e.Mobile.SendMessage("Usage: [Names who <name> | assign <account> <name> [take] | release <account|unknown|all> <name> | audit [report]");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "NameRegistry [Names");
                e.Mobile.SendMessage("Names: failed: " + ex.Message);
            }
        }
        #endregion The audit


        #region Ownership
        /// <summary>
        /// The registry decides on player shards: not the Login Server (no players), nor Test Center or an event shard
        /// (copies of a player shard, which keep their own per-shard rules and never claim).
        /// </summary>
        public static bool Enforced => !Core.RuleSets.LoginServerRules() && !Core.GMNTC_CFG && !Core.GMNEV_CFG;

        public enum ClaimResult { Claimed, Taken, Unavailable }

        /// <summary>A name's rows: its owners ("" unknown: the archive), as written, where and from what. Null: unreadable.</summary>
        public static List<(string Owner, string Name, string Shard, string Source)>? Owners(string? name)
        {
            string key = Key(name);
            List<(string, string, string, string)> rows = new List<(string, string, string, string)>();
            if (key.Length == 0)
                return rows;
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand("SELECT owner, name, shard, source FROM Names WHERE key = @key ORDER BY utc", conn);
                cmd.Parameters.AddWithValue("@key", key);
                using SQLiteDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    rows.Add((r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3)));
                return rows;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "NameRegistry.Owners");
                return null;
            }
        }

        /// <summary>
        /// The rule, on a name's owners: free for 'username' when it owns the name (grandfathered names have several
        /// owners: each may use it), or when nobody does. A name with only an unknown owner (the archive: an old
        /// player's) is free for nobody until staff assign it.
        /// </summary>
        public static bool FreeFor(IEnumerable<string> owners, string username)
        {
            List<string> all = owners.ToList();
            List<string> known = all.Where(o => o.Length > 0).ToList();
            if (known.Count > 0)
                return known.Any(o => string.Equals(o, username, StringComparison.OrdinalIgnoreCase));
            return all.Count == 0;
        }

        /// <summary>Is the name free for this account? Null: the registry can't be read (callers refuse: fail closed).
        /// The placeholder ("Generic Player") is always free.</summary>
        public static bool? IsFree(string? name, string username)
        {
            if (Placeholder(name) || Key(name).Length == 0)
                return true;
            List<(string Owner, string Name, string Shard, string Source)>? rows = Owners(name);
            if (rows == null)
                return null;
            return FreeFor(rows.Select(r => r.Owner), username);
        }

        /// <summary>
        /// Claims the name for the account, in one transaction (two shards can't take a name at the same moment): Taken
        /// when it isn't free for the account, Unavailable when the registry can't be written. A name the account
        /// already owns is Claimed (nothing written). Never frees a name: a renamed character's old name stays its account's.
        /// </summary>
        public static ClaimResult Claim(string? name, string username, string source = "claim")
        {
            if (Placeholder(name) || string.IsNullOrWhiteSpace(username))
                return ClaimResult.Claimed;
            string key = Key(name);
            if (key.Length == 0)
                return ClaimResult.Claimed;
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteTransaction tx = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
                List<string> owners = new List<string>();
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT owner FROM Names WHERE key = @key", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@key", key);
                    using SQLiteDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                        owners.Add(r.GetString(0));
                }
                if (!FreeFor(owners, username))
                    return ClaimResult.Taken;
                Insert(conn, tx, key, username, name!.Trim(), Core.Server, source);
                ClearTaken(conn, tx, key, username);
                tx.Commit();
                return ClaimResult.Claimed;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "NameRegistry.Claim");
                return ClaimResult.Unavailable;
            }
        }

        private static int Insert(SQLiteConnection conn, SQLiteTransaction? tx, string key, string owner, string name, string shard, string source)
        {
            using SQLiteCommand cmd = new SQLiteCommand(
                "INSERT OR IGNORE INTO Names (key, owner, name, shard, source, utc) VALUES (@key, @owner, @name, @shard, @source, @utc)", conn, tx);
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@owner", owner);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@shard", shard);
            cmd.Parameters.AddWithValue("@source", source);
            cmd.Parameters.AddWithValue("@utc", DateTime.UtcNow.Ticks);
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Seeds the registry with names as they are (grandfathered: every owner keeps its name, even when several
        /// accounts have it), skipping the placeholder and the junk Adam approved (JunkReason). Rows already there are
        /// kept. Returns the rows added, or -1 when the registry can't be written.
        /// </summary>
        public static int Seed(IEnumerable<AuditRow> rows)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteTransaction tx = conn.BeginTransaction();
                int added = 0;
                foreach (AuditRow r in rows)
                    if (r.Key.Length > 0 && !Placeholder(r.Name) && JunkReason(r.Key).Length == 0)
                        added += Insert(conn, tx, r.Key, r.Owner, r.Name, r.Shard, r.Source);
                tx.Commit();
                return added;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "NameRegistry.Seed");
                return -1;
            }
        }

        /// <summary>Gives the name to the account (staff: [Names assign). Other owners keep theirs; release them to move it.</summary>
        public static bool Assign(string name, string username)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteTransaction tx = conn.BeginTransaction();
                Insert(conn, tx, Key(name), username, name.Trim(), Core.Server, "assign");
                ClearTaken(conn, tx, Key(name), username);     // the account has it again
                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "NameRegistry.Assign");
                return false;
            }
        }

        /// <summary>
        /// Makes the account the name's only owner (staff: [Names assign ... take): every other owner's row goes, the
        /// archive's unknown owner too, in one transaction. False: the registry can't be written.
        /// </summary>
        public static bool Take(string name, string username, string byWhom = "")
        {
            string key = Key(name);
            if (key.Length == 0 || Placeholder(name) || string.IsNullOrWhiteSpace(username))
                return false;
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteTransaction tx = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
                // every account that loses it is remembered (Taken): its characters with the name are renamed on every
                //  shard, at startup or login (the archive's unknown owner isn't a player: nothing to remember)
                List<string> losers = new List<string>();
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT owner FROM Names WHERE key = @key AND owner <> @owner AND owner <> ''", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@key", key);
                    cmd.Parameters.AddWithValue("@owner", username);
                    using SQLiteDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                        losers.Add(r.GetString(0));
                }
                using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Names WHERE key = @key AND owner <> @owner", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@key", key);
                    cmd.Parameters.AddWithValue("@owner", username);
                    cmd.ExecuteNonQuery();
                }
                foreach (string loser in losers)
                    RecordTaken(conn, tx, key, loser, name.Trim(), byWhom);
                Insert(conn, tx, key, username, name.Trim(), Core.Server, "assign");
                ClearTaken(conn, tx, key, username);
                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "NameRegistry.Take");
                return false;
            }
        }

        /// <summary>What taking a name did to this shard's characters.</summary>
        public sealed class TakeResult
        {
            public int Renamed, Deeds, DeedFailures, DisplayNamesReset, DisplayNamesAwarded;
        }

        /// <summary>
        /// After a take, on this shard: every other account's live character with the name (look-alikes too) is renamed
        /// to the placeholder ("Generic Player") and given a personal name change deed (backpack, else bank), never
        /// deleted. On Seasons the leaderboard name follows: an account that loses the name loses it as its DisplayName,
        /// and the taker gets it when it has none. Logged to Names.log.
        /// </summary>
        public static TakeResult RenameOthers(string name, string username)
        {
            TakeResult result = new TakeResult();
            string key = Key(name);
            using LogHelper logger = new LogHelper("Names.log", false, true);
            foreach (Server.Mobiles.PlayerMobile pm in Server.Mobiles.PlayerMobile.Instances.ToArray())
            {
                if (pm == null || pm.Deleted || pm.Account is not Account acct)
                    continue;
                bool taker = string.Equals(acct.Username, username, StringComparison.OrdinalIgnoreCase);
                SeasonalAccount? sa = Core.RuleSets.SeasonsRules() ? SeasonalAccounts.GetAccount(acct) : null;

                if (!taker && Key(pm.Name) == key)
                    RenameOne(pm, result, logger, string.Format("taken for {0}", username));
                if (!taker && sa != null && Key(sa.DisplayName) == key)
                {
                    sa.DisplayName = Server.Misc.CharacterCreation.DefaultName;
                    result.DisplayNamesReset++;
                    logger.Log(LogType.Text, string.Format("Names: {0}'s leaderboard name '{1}' reset.", acct.Username, name));
                }
                if (taker && sa != null && (string.IsNullOrWhiteSpace(sa.DisplayName) || Placeholder(sa.DisplayName)))
                {
                    sa.DisplayName = name.Trim();
                    result.DisplayNamesAwarded++;
                    logger.Log(LogType.Text, string.Format("Names: {0}'s leaderboard name is '{1}' now.", acct.Username, name));
                }
            }
            return result;
        }

        /// <summary>
        /// One character loses its name: renamed to the placeholder ("Generic Player") with a personal name change deed
        /// (backpack, else bank), never deleted; told if it's online; logged.
        /// </summary>
        private static void RenameOne(Server.Mobiles.PlayerMobile pm, TakeResult result, LogHelper logger, string why)
        {
            string oldName = pm.Name;
            string placeholder = Server.Misc.CharacterCreation.DefaultName;
            pm.Name = placeholder;
            result.Renamed++;
            Server.Items.PersonalNameChangeDeed deed = new Server.Items.PersonalNameChangeDeed();
            deed.OwnerSerial = pm.Serial;
            if ((pm.Backpack != null && pm.Backpack.TryDropItem(pm, deed, false)) || (pm.BankBox != null && pm.BankBox.TryDropItem(pm, deed, false)))
                result.Deeds++;
            else
            {
                deed.Delete();
                result.DeedFailures++;
            }
            if (pm.NetState != null)
                pm.SendMessage(0x35, "Staff have reassigned the name {0}. Use the name change deed in your pack to choose a new one.", oldName);
            logger.Log(LogType.Text, string.Format("Names: '{0}' ({1}) on {2}'s account renamed '{3}' with a name change deed ({4}).",
                oldName, pm.Serial, (pm.Account as Account)?.Username ?? "?", placeholder, why));
        }
        #endregion Ownership

        #region Taken
        // A name staff took from an account ([Names assign ... take, [Names release <account>): its characters with the
        //  name are renamed on every shard, at startup (this shard's whole roster) or when one logs in. Only for a Taken
        //  row: grandfathered owners, junk names (never registered) and an unreadable registry never rename anyone.

        private static void RecordTaken(SQLiteConnection conn, SQLiteTransaction tx, string key, string owner, string name, string byWhom)
        {
            using SQLiteCommand cmd = new SQLiteCommand(
                "INSERT OR REPLACE INTO Taken (key, owner, name, byWhom, shard, utc) VALUES (@key, @owner, @name, @by, @shard, @utc)", conn, tx);
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@owner", owner);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@by", byWhom ?? "");
            cmd.Parameters.AddWithValue("@shard", Core.Server);
            cmd.Parameters.AddWithValue("@utc", DateTime.UtcNow.Ticks);
            cmd.ExecuteNonQuery();
        }

        private static void ClearTaken(SQLiteConnection conn, SQLiteTransaction? tx, string key, string owner)
        {
            using SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Taken WHERE key = @key AND owner = @owner", conn, tx);
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@owner", owner);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Every Taken row (name key and account), or null: the registry can't be read.</summary>
        public static HashSet<(string Key, string Owner)>? LoadTaken() => LoadTaken(failFast: false);

        private static HashSet<(string Key, string Owner)>? LoadTaken(bool failFast)
        {
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            // the fail-fast read can't make the tables (read-only, no wait): until Open has made them for this path (a new
            //  or replaced file), read the normal way once
            if (failFast)
                lock (m_SchemaLock)
                    if (m_SchemaReadyFor != DatabasePath)
                        failFast = false;
            try
            {
                HashSet<(string, string)> taken = new HashSet<(string, string)>();
                using SQLiteConnection conn = failFast
                    ? new SQLiteConnection(new SQLiteConnectionStringBuilder
                    {
                        DataSource = DatabasePath,
                        ReadOnly = true,
                        FailIfMissing = true,
                        Pooling = false,
                        DefaultTimeout = 0,
                        BusyTimeout = 0
                    }.ConnectionString)
                    : Open();
                if (failFast)
                    conn.Open();
                using SQLiteCommand cmd = new SQLiteCommand("SELECT key, owner FROM Taken", conn);
                using SQLiteDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    taken.Add((r.GetString(0), r.GetString(1).ToLowerInvariant()));
                return taken;
            }
            catch (SQLiteException ex) when (failFast &&
                ((int)ex.ResultCode & 0xff) is (int)SQLiteErrorCode.Busy or (int)SQLiteErrorCode.Locked)
            {
                Console.WriteLine("NamesTaken: database busy, skipped ({0:F1} ms).",
                    System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                return null;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "NameRegistry.LoadTaken");
                return null;
            }
            finally
            {
                if (failFast)
                    Server.Engines.CronScheduler.Cron.RecordStat("NamesTaken: database read",
                        System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            }
        }

        /// <summary>The name's Taken rows: from whom, by whom, where and when. Null: unreadable.</summary>
        public static List<(string Owner, string By, string Shard, DateTime Utc)>? TakenRows(string? name)
        {
            try
            {
                List<(string, string, string, DateTime)> rows = new List<(string, string, string, DateTime)>();
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand("SELECT owner, byWhom, shard, utc FROM Taken WHERE key = @key ORDER BY utc", conn);
                cmd.Parameters.AddWithValue("@key", Key(name));
                using SQLiteDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    rows.Add((r.GetString(0), r.GetString(1), r.GetString(2), new DateTime(r.GetInt64(3), DateTimeKind.Utc)));
                return rows;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "NameRegistry.TakenRows");
                return null;
            }
        }

        /// <summary>
        /// The rule for a character: renamed only when staff took its name from its account (a Taken row for the two).
        /// The placeholder never; no row (a grandfathered owner, a junk name never registered, a Test Center) never.
        /// </summary>
        public static bool MustRename(HashSet<(string Key, string Owner)> taken, string? name, string? username)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(username) || Placeholder(name))
                return false;
            string key = Key(name);
            return key.Length > 0 && taken.Contains((key, username.ToLowerInvariant()));
        }

        /// <summary>This shard's characters whose names were taken from their accounts: renamed now (startup).</summary>
        public static int SweepTaken()
        {
            if (!Enforced)
                return 0;
            HashSet<(string Key, string Owner)>? taken = LoadTaken();
            if (taken == null || taken.Count == 0)
                return 0;       // unreadable: nobody is renamed on a guess
            TakeResult result = new TakeResult();
            using LogHelper logger = new LogHelper("Names.log", false, true);
            HashSet<Account> seen = new HashSet<Account>();
            foreach (Server.Mobiles.PlayerMobile pm in Server.Mobiles.PlayerMobile.Instances.ToArray())
            {
                if (pm == null || pm.Deleted || pm.Account is not Account acct)
                    continue;
                if (MustRename(taken, pm.Name, acct.Username))
                    RenameOne(pm, result, logger, "taken from its account (startup)");
                if (seen.Add(acct))
                    ResetDisplayName(acct, taken, logger);
            }
            return result.Renamed;
        }

        /// <summary>
        /// The cron's pass (every 5 minutes, CronTasks CNamesTaken): characters online now whose names were taken from
        /// their accounts since they logged in (on another shard, say) are renamed. Offline ones wait for startup or login.
        /// </summary>
        public static int ApplyTakenOnline()
        {
            if (!Enforced)
                return 0;
            HashSet<(string Key, string Owner)>? taken = LoadTaken(failFast: true);
            if (taken == null || taken.Count == 0)
                return 0;       // unreadable: nobody is renamed on a guess
            TakeResult result = new TakeResult();
            LogHelper? logger = null;
            try
            {
                foreach (Server.Network.NetState ns in Server.Network.NetState.Instances.ToArray())
                {
                    if (ns?.Mobile is not Server.Mobiles.PlayerMobile pm || pm.Deleted || pm.Account is not Account acct)
                        continue;
                    if (!MustRename(taken, pm.Name, acct.Username))
                        continue;
                    logger ??= new LogHelper("Names.log", false, true);
                    RenameOne(pm, result, logger, "taken from its account (online)");
                    ResetDisplayName(acct, taken, logger);
                }
            }
            finally
            {
                logger?.Finish();
            }
            return result.Renamed;
        }

        private static void OnLogin(LoginEventArgs e)
        {
            if (!Enforced || e.Mobile is not Server.Mobiles.PlayerMobile pm || pm.Account is not Account acct || Placeholder(pm.Name))
                return;
            HashSet<(string Key, string Owner)>? taken = LoadTaken();
            if (taken == null || !MustRename(taken, pm.Name, acct.Username))
                return;
            using LogHelper logger = new LogHelper("Names.log", false, true);
            RenameOne(pm, new TakeResult(), logger, "taken from its account (login)");
            ResetDisplayName(acct, taken, logger);
        }

        // Seasons: an account's leaderboard name goes with a name taken from it
        private static void ResetDisplayName(Account acct, HashSet<(string Key, string Owner)> taken, LogHelper logger)
        {
            if (!Core.RuleSets.SeasonsRules())
                return;
            SeasonalAccount? sa = SeasonalAccounts.GetAccount(acct);
            if (sa != null && MustRename(taken, sa.DisplayName, acct.Username))
            {
                logger.Log(LogType.Text, string.Format("Names: {0}'s leaderboard name '{1}' reset (taken from its account).", acct.Username, sa.DisplayName));
                sa.DisplayName = Server.Misc.CharacterCreation.DefaultName;
            }
        }
        /// <summary>
        /// For the [set name confirmation (Owner): what taking the name for this account would mean, as gump HTML: its
        /// owners (last login as this shard knows it), and this shard's characters of other accounts that would be renamed.
        /// </summary>
        public static string TakePreview(string name, string username)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("'{0}' is reserved. Make it {1}'s alone, on every Angel Island shard?<br><br>", name, username);
            var rows = Owners(name);
            if (rows == null)
                return sb.Append("The name registry can't be read.").ToString();
            foreach (var g in rows.GroupBy(r => r.Owner, StringComparer.OrdinalIgnoreCase))
            {
                if (g.Key.Length == 0)
                {
                    sb.Append("Owner: an old player (the history archive), account unknown.<br>");
                    continue;
                }
                Account? acct = Accounts.GetAccount(g.Key) as Account;
                sb.AppendFormat("Owner: {0} (seen on {1}); {2}.<br>", g.Key, string.Join(", ", g.Select(r => r.Shard).Distinct()),
                    acct == null ? "no account on this shard" : "last login here " + acct.LastLogin.ToString("yyyy-MM-dd"));
            }
            string key = Key(name);
            List<string> here = Server.Mobiles.PlayerMobile.Instances
                .Where(pm => pm != null && !pm.Deleted && Key(pm.Name) == key && pm.Account is Account a
                    && !string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase))
                .Select(pm => string.Format("{0} ({1})", pm.Name, (pm.Account as Account)?.Username)).ToList();
            sb.Append("<br>");
            sb.Append(here.Count == 0 ? "No character on this shard would be renamed."
                : string.Format("Renamed '{0}' here, with a name change deed: {1}.", Server.Misc.CharacterCreation.DefaultName, string.Join(", ", here)));
            sb.Append(" Characters elsewhere lose it at startup or when they log in.");
            return sb.ToString();
        }

        /// <summary>The [set name confirmation's Yes: take the name for the target's account, rename here, and name the target.</summary>
        public static void ConfirmTake(Mobile from, bool okay, object state)
        {
            if (state is not object[] args || args.Length < 2 || args[0] is not Mobile target || args[1] is not string name)
                return;
            if (!okay)
            {
                from.SendMessage("Names: nothing changed.");
                return;
            }
            if (target.Deleted || target.Account is not Account acct || from.AccessLevel < AccessLevel.Owner)
                return;
            if (!Take(name, acct.Username, from.Name))
            {
                from.SendMessage("Names: the registry can't be written.");
                return;
            }
            TakeResult t = RenameOthers(name, acct.Username);
            string oldName = target.Name;
            target.Name = name;
            Server.Commands.CommandLogging.WriteLine(from, "{0} {1} took the name '{2}' for {3} and set {4}'s name ({5} renamed here)",
                from.AccessLevel, Server.Commands.CommandLogging.Format(from), name, acct.Username, oldName, t.Renamed);
            from.SendMessage("Names: '{0}' is {1}'s alone now; {2} is named {0}. {3} character(s) here renamed '{4}' with a name change deed; elsewhere at startup or login.",
                name, acct.Username, oldName, t.Renamed, Server.Misc.CharacterCreation.DefaultName);
        }
        #endregion Taken

        #region Ownership (continued)

        /// <summary>
        /// Takes the name from an owner ("" the archive's unknown owner; null every owner): staff only. Rows removed, -1
        /// unwritable. From an account: remembered (Taken), so its characters with the name are renamed at startup or
        /// login, on every shard. From everyone: nobody is entitled to it any more, so nobody is renamed for having it
        /// (the name's Taken rows go too).
        /// </summary>
        public static int Release(string name, string? owner, string byWhom = "")
        {
            string key = Key(name);
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteTransaction tx = conn.BeginTransaction();
                int removed;
                using (SQLiteCommand cmd = new SQLiteCommand(owner == null ? "DELETE FROM Names WHERE key = @key"
                    : "DELETE FROM Names WHERE key = @key AND owner = @owner", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@key", key);
                    if (owner != null)
                        cmd.Parameters.AddWithValue("@owner", owner);
                    removed = cmd.ExecuteNonQuery();
                }
                if (owner == null)
                {
                    using SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Taken WHERE key = @key", conn, tx);
                    cmd.Parameters.AddWithValue("@key", key);
                    cmd.ExecuteNonQuery();
                }
                else if (owner.Length > 0 && removed > 0)
                    RecordTaken(conn, tx, key, owner, name.Trim(), byWhom);
                tx.Commit();
                return removed;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "NameRegistry.Release");
                return -1;
            }
        }
        #endregion Ownership

        #region CSV
        private static string Csv(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

        private static List<string> SplitCsv(string line)
        {
            List<string> fields = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (quoted)
                {
                    if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else if (ch == '"') quoted = false;
                    else sb.Append(ch);
                }
                else if (ch == '"') quoted = true;
                else if (ch == ',') { fields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(ch);
            }
            fields.Add(sb.ToString());
            return fields;
        }
        #endregion CSV
    }
}
