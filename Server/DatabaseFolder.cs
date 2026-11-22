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

/* Server/DatabaseFolder.cs
 * ChangeLog
 *  11/18/26, Adam (one database folder)
 *      Created. Where the shard's databases live, decided in one place.
 *
 *      THE FOLDER
 *          AI.DBDIR set      that folder: it must exist and be writable, or the server stops (Check).
 *          AI.DBDIR unset    <shard>\Databases, created at first start. Not inside Saves: AutoSave moves the whole Saves
 *                            folder to Backups before each save, and open databases can't go with it.
 *      Every database asks here (PathOf): accounts.db (logins and passwords), ipexception.db, firewalldb.db, names.db
 *      (family-wide character names), music.db (the shared music library). Shards that share a folder share them: the
 *      Angel Island family sets AI.DBDIR=C:\AIDB on each of its machines; a shard on its own needs nothing.
 *      They're always on: -usedb and -sharemusic are retired (the server stops if it's given one), and so are the old
 *      per-database variables AI.LOGINDB, AI.MUSICDB, AI.NAMESDB, AI.IPEXCEPTIONDB, AI.FIREWALLDB (the same).
 *      The guard: a machine whose C:\AIDB holds the family's databases, with AI.DBDIR unset or elsewhere, stops: its
 *      shards would otherwise start quietly on empty databases of their own, apart from the rest of the family.
 *
 *      THE RULE: ON A ROLLBACK, THE DATABASES WIN
 *      The databases are kept apart from the world (Saves), and a world can be rolled back to an older save while they
 *      can't. So they hold only what may survive a rollback as it is: identity, configuration and the shared music
 *      catalog, never world value (items, mobiles, skills, gold). Each one:
 *          accounts.db     a newer password or machine after a rollback is still the right one
 *          names.db        a name claimed after the save point stays its account's (its character is gone, the name
 *                          isn't); a take after it still renames at startup
 *          firewall, ipexception   configuration
 *          music.db        songs, decisions and renames: a home world missing a song rebuilds it; a synced decision
 *                          is final
 *      Anything a database hands INTO the world follows one of two patterns:
 *          applying it twice is harmless (music decisions and renames: the second does nothing), or
 *          it is acknowledged on save, never on apply: the world applies it and remembers the ids in a file saved with
 *          the world, and only after a world save completes is the database told (music sales: MusicShare, Recognized.txt).
 *          A world that never saved it (a crash, a rollback) still finds it open, and applies it again.
 */

using System;
using System.IO;
using System.Linq;

namespace Server
{
    public static class DatabaseFolder
    {
        /// <summary>The environment variable naming the folder (the family's: C:\AIDB).</summary>
        public const string Variable = "AI.DBDIR";

        /// <summary>The Angel Island family's folder: the guard looks for its databases there.</summary>
        public const string FamilyFolder = @"C:\AIDB";

        /// <summary>The databases that mark a family folder.</summary>
        public static readonly string[] FamilyFiles = { "accounts.db", "names.db", "music.db" };

        /// <summary>Retired: each database had its own variable; AI.DBDIR replaces them all.</summary>
        public static readonly string[] RetiredVariables = { "AI.LOGINDB", "AI.MUSICDB", "AI.NAMESDB", "AI.IPEXCEPTIONDB", "AI.FIREWALLDB" };

        /// <summary>Retired startup switches: the databases are always on.</summary>
        public static readonly string[] RetiredSwitches = { "-usedb", "-sharemusic" };

        /// <summary>The folder: AI.DBDIR, else &lt;shard&gt;\Databases.</summary>
        public static string Path => Resolve(Environment.GetEnvironmentVariable(Variable), Core.ExeDirectory);

        /// <summary>A database file in the folder (created with the folder when it's the default).</summary>
        public static string PathOf(string file)
        {
            string folder = Path;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return System.IO.Path.Combine(folder, file);
        }

        public static string Resolve(string? variable, string shardRoot) =>
            string.IsNullOrWhiteSpace(variable) ? System.IO.Path.Combine(shardRoot, "Databases") : variable.Trim().Trim('"');

        /// <summary>Why this server must not start with these settings, or null. (Main halts on it.)</summary>
        public static string? Check() =>
            Check(Environment.GetEnvironmentVariable, Core.ExeDirectory, FamilyFolder);

        /// <summary>The checks, with the environment, the shard's folder and the family folder given (for tests).</summary>
        public static string? Check(Func<string, string?> env, string shardRoot, string familyFolder)
        {
            string[] retired = RetiredVariables.Where(v => !string.IsNullOrWhiteSpace(env(v))).ToArray();
            if (retired.Length > 0)
                return string.Format("{0} {1} retired: every database now lives in one folder, {2}. Remove {3} and set {2} instead " +
                    "(or leave it unset: {4}).", string.Join(", ", retired), retired.Length == 1 ? "is" : "are", Variable,
                    retired.Length == 1 ? "it" : "them", System.IO.Path.Combine(shardRoot, "Databases"));

            string? variable = env(Variable);
            string folder = Resolve(variable, shardRoot);
            if (!string.IsNullOrWhiteSpace(variable))
            {
                if (!Directory.Exists(folder))
                    return string.Format("{0} is {1}, which doesn't exist. Create it, or set {0} to the folder that holds the databases.", Variable, folder);
                if (!Writable(folder))
                    return string.Format("{0} is {1}, which this server can't write to.", Variable, folder);
            }

            // the guard: this machine keeps the family's databases in C:\AIDB, and this shard would use others
            bool family = Directory.Exists(familyFolder) && FamilyFiles.Any(f => File.Exists(System.IO.Path.Combine(familyFolder, f)));
            if (family && !SameFolder(folder, familyFolder))
                return string.Format("This machine's databases are in {0}, but this shard would use {1}. Set {2}={0} (a system " +
                    "environment variable), then start the shard again.", familyFolder, folder, Variable);

            if (string.IsNullOrWhiteSpace(variable))
            {
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (Exception ex)
                {
                    return string.Format("The database folder {0} can't be created ({1}). Set {2} to a folder this server can write to.", folder, ex.Message, Variable);
                }
                if (!Writable(folder))
                    return string.Format("The database folder {0} can't be written to. Set {1} to a folder this server can write to.", folder, Variable);
            }
            return null;
        }

        /// <summary>The retired switches on this command line, as given ("" none).</summary>
        public static string RetiredSwitchesIn(string[] args) =>
            string.Join(" ", args.Where(a => RetiredSwitches.Contains(a.Trim().ToLowerInvariant())));

        public static bool SameFolder(string a, string b)
        {
            try
            {
                return string.Equals(System.IO.Path.GetFullPath(a).TrimEnd('\\', '/'), System.IO.Path.GetFullPath(b).TrimEnd('\\', '/'),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool Writable(string folder)
        {
            try
            {
                string probe = System.IO.Path.Combine(folder, ".write-test-" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
