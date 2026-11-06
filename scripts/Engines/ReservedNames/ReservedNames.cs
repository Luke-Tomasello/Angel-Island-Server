
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

/* Scripts\Engines\ReservedNames\ReservedNames.cs
 * CHANGELOG:
 *  4/22/2026, Adam
 */

using Newtonsoft.Json;
using Server.Accounting;
using Server.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Engines
{
    public static class ReservedNames
    {
        public static void Initialize()
        {
            CommandSystem.Register("SeedReservedNames", AccessLevel.Administrator, new CommandEventHandler(SeedReservedNames_OnCommand));
        }
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
        }
        [Usage("SeedReservedNames")]
        [Description("Seeds the Reserved Names registry from account data and saves it to disk.")]
        public static void SeedReservedNames_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile == null)
                return;

            try
            {
                int records = ReservedNames.Seed();

                e.Mobile.SendMessage($"{records} Reserved Names added to registry.");
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage("Seeding Reserved Names failed: {0}", ex.Message);
            }
        }
        private const string FileName = "Reserved Names.json";
        private static string FilePath => Path.Combine(Core.ExeDirectory, "Saves", "Accounts", FileName);

        // Persisted form:
        //   username -> list of reserved names
        public static Dictionary<string, List<string>> Database { get; private set; } =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        // Derived lookup:
        //   normalized name -> owning username
        private static Dictionary<string, string> _nameLookup =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Optional: keep collisions visible for review
        //   normalized name -> all usernames claiming it
        public static Dictionary<string, List<string>> Collisions { get; private set; } =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public static void Add(string username, string name)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("username is required");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("name is required");

            string normalized = Normalize(name);

            // Check global ownership
            if (_nameLookup.TryGetValue(normalized, out string? existingOwner))
            {
                if (!string.Equals(existingOwner, username, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"Name '{name}' is already reserved by '{existingOwner}'");

                // Already owned by this user → no-op
                return;
            }

            // Add to Database (owner -> names)
            if (!Database.TryGetValue(username, out List<string>? names))
            {
                names = new List<string>();
                Database[username] = names;
            }

            // Prevent duplicates within same user (normalized)
            if (!ContainsNormalized(names, name))
                names.Add(name);

            // Update lookup
            _nameLookup[normalized] = username;
        }
        public static bool TryAdd(string username, string name)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(name))
                return false;

            string normalized = Normalize(name);

            if (_nameLookup.TryGetValue(normalized, out string? existingOwner))
            {
                if (!string.Equals(existingOwner, username, StringComparison.OrdinalIgnoreCase))
                    return false;

                // already owned by this user
                return true;
            }

            if (!Database.TryGetValue(username, out List<string>? names))
            {
                names = new List<string>();
                Database[username] = names;
            }

            if (!ContainsNormalized(names, name))
                names.Add(name);

            _nameLookup[normalized] = username;

            return true;
        }
        public static void ReplaceName(Account acct, string oldName, string newName)
        {
            if (acct == null)
                throw new ArgumentNullException(nameof(acct));

            if (string.IsNullOrWhiteSpace(acct.Username))
                throw new ArgumentException("Account username is required.", nameof(acct));

            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("newName is required.", nameof(newName));

            if (string.Equals(newName, CharacterCreation.DefaultName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The default character name may not be reserved.");

            string username = acct.Username;

            // Relinquish the old name, unless it is the system default placeholder.
            if (!string.IsNullOrWhiteSpace(oldName) &&
                !string.Equals(oldName, CharacterCreation.DefaultName, StringComparison.OrdinalIgnoreCase))
            {
                if (!Database.TryGetValue(username, out List<string>? names) || names == null)
                    throw new InvalidOperationException(
                        $"Account '{username}' does not own any reserved names.");

                string normalizedOld = Normalize(oldName);
                int index = -1;

                for (int i = 0; i < names.Count; i++)
                {
                    if (Normalize(names[i]) == normalizedOld)
                    {
                        index = i;
                        break;
                    }
                }

                if (index < 0)
                    throw new InvalidOperationException(
                        $"Account '{username}' does not own reserved name '{oldName}'.");

                names.RemoveAt(index);

                if (names.Count == 0)
                    Database.Remove(username);
            }

            // Acquire the new name.
            Add(username, newName);
        }
        public static void Load()
        {
            EnsureDataDirectory();

            if (!File.Exists(FilePath))
            {
                Console.WriteLine("No Reserved Names to load, initializing...");
                Database = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                RebuildLookup();
                return;
            }
            Console.WriteLine("Reserved Names loading...");
            try
            {
                string json = File.ReadAllText(FilePath);

                var db = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);

                Database = db ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReservedNames.Load failed: {0}", ex.Message);
                Database = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            }

            RebuildLookup();
        }

        public static void Save(WorldSaveEventArgs? e)
        {
            Console.WriteLine("Reserved Names saving...");
            EnsureDataDirectory();

            try
            {
                string json = JsonConvert.SerializeObject(Database, Formatting.Indented);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReservedNames.Save failed: {0}", ex.Message);
            }
        }
        public static int Seed()
        {
            var db = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (Account acct in Accounts.Table.Values)
            {
                if (acct == null)
                    continue;

                string username = acct.Username;

                if (string.IsNullOrWhiteSpace(username))
                    continue;

                List<string> reservedNames = acct.GetReservedNames();

                if (reservedNames == null || reservedNames.Count == 0)
                    continue;

                if (!db.TryGetValue(username, out List<string>? names))
                {
                    names = new List<string>();
                    db[username] = names;
                }

                foreach (string reservedName in reservedNames)
                {
                    if (string.IsNullOrWhiteSpace(reservedName))
                        continue;

                    if (string.Equals(reservedName, CharacterCreation.DefaultName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!ContainsNormalized(names, reservedName))
                        names.Add(reservedName);
                }

                if (names.Count == 0)
                    db.Remove(username);
            }

            Database = db;
            RebuildLookup();
            Save(null);

            return Database.Count;
        }

        // Simple public lookup:
        // returns the owning username for a candidate name, or null if unreserved
        public static string? LookupOwner(string candidateName)
        {
            if (string.IsNullOrWhiteSpace(candidateName))
                return null;

            string normalized = Normalize(candidateName);

            if (_nameLookup.TryGetValue(normalized, out string? owner))
                return owner;

            return null;
        }

        public static void Rebuild()
        {
            RebuildLookup();
        }
        private static void RebuildLookup()
        {
            _nameLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Collisions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in Database)
            {
                string username = kvp.Key;
                List<string> names = kvp.Value;

                if (string.IsNullOrWhiteSpace(username) || names == null)
                    continue;

                foreach (string name in names)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    string normalized = Normalize(name);

                    if (_nameLookup.TryGetValue(normalized, out string? existingOwner))
                    {
                        if (!string.Equals(existingOwner, username, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!Collisions.TryGetValue(normalized, out List<string>? owners))
                            {
                                owners = new List<string> { existingOwner };
                                Collisions[normalized] = owners;
                            }

                            if (!owners.Any(o => string.Equals(o, username, StringComparison.OrdinalIgnoreCase)))
                                owners.Add(username);
                        }

                        continue;
                    }

                    _nameLookup[normalized] = username;
                }
            }
        }

        private static bool ContainsNormalized(List<string> names, string candidate)
        {
            string normalizedCandidate = Normalize(candidate);

            foreach (string name in names)
            {
                if (Normalize(name) == normalizedCandidate)
                    return true;
            }

            return false;
        }

        public static string Normalize(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            return name.Replace(" ", "").Trim().ToLowerInvariant();
        }

        private static void EnsureDataDirectory()
        {
            string dataPath = Path.Combine(Core.ExeDirectory, "Saves", "Accounts");

            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);
        }
    }
}