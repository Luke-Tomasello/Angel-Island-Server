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

/* Scripts\Commands\OwnerTools.PlayerHistory.cs
 * CHANGELOG
 *  10/29/2026, Adam (BackpackTypes)
 *      Skip the empty piece after a backpack's last "; " (it reached ScriptCompiler.FindTypeByFullName("") and fired
 *      TypeTable's Debug assert when a Doppelganger died; Release skipped it).
 *  10/29/2026, Adam
 *      Initial version: the player history archive (Data\AIPlayerHistory), the record of the players of our shards.
 *      One file per shard. Version 1 files are self-describing: a header (version, shard, when written, count) and,
 *      per player, named fields (a reader keeps fields it does not know and writes them back). Version 0 files (the
 *      2016-2024 [Nuke DupePlayers files) are read and upgraded; the "[Angel Island 2018]" titles DupePlayers made up
 *      are dropped. Capture records every player on this shard (layers, backpack, game time, skills, stats, guild, ...)
 *      and merges into this shard's file: a player already on file is replaced, everyone else is kept.
 *      Doppelgangers are drawn from the archive (no live capture): well-played characters (game time; fame and tenure
 *      for the old records) tend to come up first, and no one repeats until everyone has had a turn.
 *      [PlayerHistory status|capture|upgrade|reload (Owner) and [PlayerInfo <target Doppelganger> (anyone).
 *      Maintain (PatchV261, once per live shard): upgrade old files, capture this shard, reload. Never on the development
 *      machine (Core.Developing: its files stay as checked in), and a shard already captured is left as is.
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.Commands
{
    public static class PlayerHistory
    {
        public const int FileVersion = 1;
        public static string Folder { get { return Path.Combine(Core.DataDirectory, "AIPlayerHistory"); } }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly TimeSpan MinGameTime = TimeSpan.FromHours(14);     // a record with hours must have played this much
        private static readonly TimeSpan MinTenure = TimeSpan.FromDays(30);        // a record without hours: created to last login

        #region Shards
        // this shard's name in the archive, and its file. null: not a player shard (Login Server)
        public static string? ThisShard
        {
            get
            {
                switch (Core.ServerID)
                {
                    case ServerID.AngelIsland: return "Angel Island 7.0";   // TODO: Angel Island 3.0 and 7.0 are both ServerID.AngelIsland
                    case ServerID.SiegePerilous: return "Siege Perilous 7.0";
                    case ServerID.Mortalis: return "Mortalis";
                    case ServerID.Seasons: return "Seasons";
                    case ServerID.Upheaval: return "Upheaval";
                    default: return null;
                }
            }
        }
        public static string? ThisShardFile
        {
            get
            {
                switch (Core.ServerID)
                {
                    case ServerID.AngelIsland: return "AI 7.0.bin";         // TODO: "AI 3.0.bin" on Angel Island 3.0
                    case ServerID.SiegePerilous: return "SP 7.0.bin";
                    case ServerID.Mortalis: return "Mortalis.bin";
                    case ServerID.Seasons: return "Seasons.bin";
                    case ServerID.Upheaval: return "Upheaval.bin";
                    default: return null;
                }
            }
        }

        // version 0 files say nothing about their shard: the file name does
        private static string LegacyShard(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (name.StartsWith("AI ", StringComparison.OrdinalIgnoreCase))
                return "Angel Island " + name.Substring(3).Trim();
            if (name.StartsWith("SP ", StringComparison.OrdinalIgnoreCase))
                return "Siege Perilous " + name.Substring(3).Trim();
            return name;
        }
        #endregion Shards

        #region Record
        public sealed class Record
        {
            public string Shard = string.Empty;         // "Angel Island 7.0"
            public DateTime Captured = DateTime.MinValue; // MinValue: unknown (version 0 records)
            public int SourceVersion = FileVersion;     // the file version this record was first written in (0: the 2016-2024 files)
            public int Serial;                          // on its shard (0: unknown)
            public string Name = string.Empty;
            public string Title = string.Empty;         // their own title
            public int Hue;
            public int Body = 400;
            public bool Female;
            public int Fame;
            public int Karma;
            public bool Murderer;
            public int Kills = -1;                      // long term murders (-1: unknown)
            public DateTime Created = DateTime.MinValue;    // account
            public DateTime LastLogin = DateTime.MinValue;  // account
            public TimeSpan GameTime = TimeSpan.Zero;   // zero: unknown
            public AccessLevel AccessLevel = AccessLevel.Player;
            public string GuildName = string.Empty;
            public string GuildAbbr = string.Empty;
            public string GuildAlignment = string.Empty;
            public int Str, Dex, Int;                   // raw (0: unknown)
            public string Skills = string.Empty;        // "Magery 100.0; EvalInt 95.5; " (base above zero)
            public string Layers = string.Empty;        // Nuke.CopyLayers: "type, hue, layer; "
            public string Backpack = string.Empty;      // "type, hue, amount; " (containers flattened)
            public Dictionary<string, string> Extra = new();    // fields this version does not know: kept on rewrite

            public bool HasGameTime { get { return GameTime > TimeSpan.Zero; } }
            public int Year { get { return Created != DateTime.MinValue ? Created.Year : LastLogin.Year; } }
            public string Label { get { return string.Format("\"{0}\" ({1} {2})", Name, Shard, Year); } }

            // best skills first: ("Magery", 100.0)
            public List<(string skill, double value)> SkillList()
            {
                List<(string, double)> list = new();
                foreach (string entry in Skills.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] parts = entry.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2 && double.TryParse(parts[1], NumberStyles.Float, Inv, out double value))
                        list.Add((parts[0], value));
                }
                return list.OrderByDescending(x => x.Item2).ToList();
            }

            // item types in the backpack (one per type)
            public List<Type> BackpackTypes()
            {
                List<Type> list = new();
                foreach (string entry in Backpack.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    string name = entry.Split(',')[0].Trim();
                    if (name.Length == 0)
                        continue;   // the piece after the last "; "
                    Type? type = ScriptCompiler.FindTypeByFullName(name);
                    if (type != null && !list.Contains(type))
                        list.Add(type);
                }
                return list;
            }
        }

        private static List<KeyValuePair<string, string>> ToFields(Record r)
        {
            List<KeyValuePair<string, string>> f = new();
            void Add(string key, string value) { f.Add(new KeyValuePair<string, string>(key, value)); }

            Add("Shard", r.Shard);
            Add("Captured", FormatDate(r.Captured));
            Add("SourceVersion", r.SourceVersion.ToString(Inv));
            Add("Serial", r.Serial.ToString(Inv));
            Add("Name", r.Name);
            Add("Title", r.Title);
            Add("Hue", r.Hue.ToString(Inv));
            Add("Body", r.Body.ToString(Inv));
            Add("Female", r.Female.ToString(Inv));
            Add("Fame", r.Fame.ToString(Inv));
            Add("Karma", r.Karma.ToString(Inv));
            Add("Murderer", r.Murderer.ToString(Inv));
            Add("Kills", r.Kills.ToString(Inv));
            Add("Created", FormatDate(r.Created));
            Add("LastLogin", FormatDate(r.LastLogin));
            Add("GameTime", r.GameTime.ToString("c", Inv));
            Add("AccessLevel", r.AccessLevel.ToString());
            Add("GuildName", r.GuildName);
            Add("GuildAbbr", r.GuildAbbr);
            Add("GuildAlignment", r.GuildAlignment);
            Add("Str", r.Str.ToString(Inv));
            Add("Dex", r.Dex.ToString(Inv));
            Add("Int", r.Int.ToString(Inv));
            Add("Skills", r.Skills);
            Add("Layers", r.Layers);
            Add("Backpack", r.Backpack);
            foreach (var kv in r.Extra)
                Add(kv.Key, kv.Value);
            return f;
        }

        private static Record FromFields(List<KeyValuePair<string, string>> fields)
        {
            Record r = new Record();
            foreach (var (key, value) in fields)
                switch (key)
                {
                    case "Shard": r.Shard = value; break;
                    case "Captured": r.Captured = ParseDate(value); break;
                    case "SourceVersion": r.SourceVersion = ParseInt(value, FileVersion); break;
                    case "Serial": r.Serial = ParseInt(value, 0); break;
                    case "Name": r.Name = value; break;
                    case "Title": r.Title = value; break;
                    case "Hue": r.Hue = ParseInt(value, 0); break;
                    case "Body": r.Body = ParseInt(value, 400); break;
                    case "Female": r.Female = bool.TryParse(value, out bool female) && female; break;
                    case "Fame": r.Fame = ParseInt(value, 0); break;
                    case "Karma": r.Karma = ParseInt(value, 0); break;
                    case "Murderer": r.Murderer = bool.TryParse(value, out bool murderer) && murderer; break;
                    case "Kills": r.Kills = ParseInt(value, -1); break;
                    case "Created": r.Created = ParseDate(value); break;
                    case "LastLogin": r.LastLogin = ParseDate(value); break;
                    case "GameTime": r.GameTime = TimeSpan.TryParse(value, Inv, out TimeSpan gt) ? gt : TimeSpan.Zero; break;
                    case "AccessLevel": r.AccessLevel = Enum.TryParse(value, out AccessLevel al) ? al : AccessLevel.Player; break;
                    case "GuildName": r.GuildName = value; break;
                    case "GuildAbbr": r.GuildAbbr = value; break;
                    case "GuildAlignment": r.GuildAlignment = value; break;
                    case "Str": r.Str = ParseInt(value, 0); break;
                    case "Dex": r.Dex = ParseInt(value, 0); break;
                    case "Int": r.Int = ParseInt(value, 0); break;
                    case "Skills": r.Skills = value; break;
                    case "Layers": r.Layers = value; break;
                    case "Backpack": r.Backpack = value; break;
                    default: r.Extra[key] = value; break;
                }
            return r;
        }

        private static string FormatDate(DateTime dt) { return dt.ToString("o", Inv); }
        private static DateTime ParseDate(string s)
        {
            return DateTime.TryParse(s, Inv, DateTimeStyles.RoundtripKind, out DateTime dt) ? dt : DateTime.MinValue;
        }
        private static int ParseInt(string s, int fallback) { return int.TryParse(s, NumberStyles.Integer, Inv, out int i) ? i : fallback; }
        #endregion Record

        #region Files
        public sealed class HistoryFile
        {
            public string Path = string.Empty;
            public int Version;
            public string Shard = string.Empty;
            public DateTime Written = DateTime.MinValue;
            public List<Record> Records = new();
        }

        // any version; throws on a damaged file
        public static HistoryFile ReadFile(string path)
        {
            HistoryFile info = new HistoryFile { Path = path };
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(fs));
                info.Version = reader.ReadInt();
                switch (info.Version)
                {
                    case 1:
                        {
                            info.Shard = reader.ReadString() ?? string.Empty;
                            info.Written = ParseDate(reader.ReadString() ?? string.Empty);
                            int count = reader.ReadInt();
                            for (int i = 0; i < count; i++)
                            {
                                int fieldCount = reader.ReadInt();
                                List<KeyValuePair<string, string>> fields = new(fieldCount);
                                for (int j = 0; j < fieldCount; j++)
                                    fields.Add(new KeyValuePair<string, string>(reader.ReadString() ?? string.Empty, reader.ReadString() ?? string.Empty));
                                info.Records.Add(FromFields(fields));
                            }
                            break;
                        }
                    case 0:
                        {   // [Nuke DupePlayers (2016-2024)
                            info.Shard = LegacyShard(path);
                            int count = reader.ReadInt();
                            for (int i = 0; i < count; i++)
                            {
                                Record r = new Record { Shard = info.Shard, SourceVersion = 0 };
                                r.Layers = reader.ReadString() ?? string.Empty;
                                r.Name = reader.ReadString() ?? string.Empty;
                                r.Title = LegacyTitle(reader.ReadString());
                                r.Hue = reader.ReadInt();
                                r.Body = reader.ReadInt();
                                r.Female = reader.ReadBool();
                                r.Fame = reader.ReadInt();
                                r.Karma = reader.ReadInt();
                                r.Murderer = reader.ReadBool();
                                r.Created = reader.ReadDateTime();
                                r.LastLogin = reader.ReadDateTime();
                                r.GuildAlignment = reader.ReadString() ?? string.Empty;
                                r.GuildName = reader.ReadString() ?? string.Empty;
                                r.GuildAbbr = reader.ReadString() ?? string.Empty;
                                info.Records.Add(r);
                            }
                            break;
                        }
                    default:
                        throw new InvalidDataException(string.Format("unknown version {0}", info.Version));
                }
            }
            return info;
        }

        // DupePlayers gave a player with no title "[Angel Island 2018]": not theirs
        private static readonly Regex m_MadeUpTitle = new Regex(@"^\[(Angel Island|Siege Perilous) \d{4}\]$");
        private static string LegacyTitle(string? title)
        {
            title = title?.Trim() ?? string.Empty;
            return m_MadeUpTitle.IsMatch(title) ? string.Empty : title;
        }

        // always the current version; written beside the file, then swapped in
        public static void WriteFile(string path, string shard, List<Record> records)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            string temp = path + ".tmp";
            BinaryFileWriter writer = new BinaryFileWriter(temp, true);
            try
            {
                writer.Write(FileVersion);
                writer.Write(shard);
                writer.Write(FormatDate(DateTime.UtcNow));
                writer.Write(records.Count);
                foreach (Record r in records)
                {
                    List<KeyValuePair<string, string>> fields = ToFields(r);
                    writer.Write(fields.Count);
                    foreach (var (key, value) in fields)
                    {
                        writer.Write(key);
                        writer.Write(value ?? string.Empty);
                    }
                }
            }
            finally
            {
                writer.Close();
            }
            File.Move(temp, path, overwrite: true);
        }

        public static string[] ArchiveFiles()
        {
            if (!Directory.Exists(Folder))
                return Array.Empty<string>();
            string[] files = Directory.GetFiles(Folder, "*.bin");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            return files;
        }
        #endregion Files

        #region Capture and Upgrade
        // every player on this shard, merged into this shard's file. message: the summary, or why not
        public static bool Capture(out string message)
        {
            string? shard = ThisShard, file = ThisShardFile;
            if (shard == null || file == null)
            {
                message = string.Format("Capture: {0} is not a player shard.", Core.Server);
                return false;
            }

            string path = Path.Combine(Folder, file);
            List<Record> records = new();
            if (File.Exists(path))
            {
                HistoryFile existing = ReadFile(path);     // a damaged file stops the capture (never overwrite history)
                records.AddRange(existing.Records);
            }

            DateTime now = DateTime.UtcNow;
            int added = 0, replaced = 0;
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is not PlayerMobile pm || pm.Deleted || string.IsNullOrWhiteSpace(pm.Name))
                    continue;

                Record r = CaptureOne(pm, shard, now);
                // the same character: same serial, or a record from the old files (no serial) with the same name
                int ix = records.FindIndex(x => x.Shard == shard &&
                    (x.Serial != 0 ? x.Serial == r.Serial : string.Equals(x.Name, r.Name, StringComparison.OrdinalIgnoreCase)));
                if (ix >= 0)
                {
                    records[ix] = r;
                    replaced++;
                }
                else
                {
                    records.Add(r);
                    added++;
                }
            }

            WriteFile(path, shard, records);
            message = string.Format("Capture: {0}: {1} players added, {2} updated, {3} in {4}.", shard, added, replaced, records.Count, file);
            return true;
        }

        private static Record CaptureOne(PlayerMobile pm, string shard, DateTime now)
        {
            Account? account = pm.Account as Account;
            Record r = new Record
            {
                Shard = shard,
                Captured = now,
                SourceVersion = FileVersion,
                Serial = pm.Serial.Value,
                Name = pm.Name.Trim(),
                Title = pm.Title ?? string.Empty,
                Hue = pm.Hue,
                Body = pm.Female ? 401 : 400,
                Female = pm.Female,
                Fame = pm.Fame,
                Karma = pm.Karma,
                Kills = pm.LongTermMurders,
                Murderer = pm.LongTermMurders >= 5,
                Created = account?.Created ?? DateTime.MinValue,
                LastLogin = account?.LastLogin ?? DateTime.MinValue,
                GameTime = pm.GameTime,
                AccessLevel = pm.AccessLevel,
                Str = pm.RawStr,
                Dex = pm.RawDex,
                Int = pm.RawInt,
                Layers = Nuke.CopyLayers(pm),
            };

            if (pm.Guild is Server.Guilds.Guild guild)
            {
                r.GuildName = guild.Name ?? string.Empty;
                r.GuildAbbr = guild.Abbreviation ?? string.Empty;
                r.GuildAlignment = Core.RuleSets.SiegeRules() ? guild.Alignment.ToString() : guild.IOBAlignment.ToString();
            }

            StringBuilder skills = new StringBuilder();
            for (int i = 0; i < pm.Skills.Length; i++)
            {
                Skill skill = pm.Skills[i];
                if (skill != null && skill.Base > 0)
                    skills.AppendFormat(Inv, "{0} {1:F1}; ", skill.SkillName, skill.Base);
            }
            r.Skills = skills.ToString();

            StringBuilder pack = new StringBuilder();
            if (pm.Backpack != null)
                AppendItems(pack, pm.Backpack);
            r.Backpack = pack.ToString();

            return r;
        }

        private static void AppendItems(StringBuilder sb, Container container)
        {
            foreach (Item item in container.Items.ToArray())
            {
                if (item == null || item.Deleted)
                    continue;
                if (item is Container child)
                    AppendItems(sb, child);
                else
                    sb.AppendFormat(Inv, "{0}, {1}, {2}; ", item.GetType().FullName, item.Hue, item.Amount);
            }
        }

        // when this shard's players were captured (its file has records captured here); MinValue: never
        private static DateTime CapturedOn()
        {
            string? shard = ThisShard, file = ThisShardFile;
            string path = file != null ? Path.Combine(Folder, file) : string.Empty;
            if (shard == null || !File.Exists(path))
                return DateTime.MinValue;

            DateTime when = DateTime.MinValue;
            foreach (Record r in ReadFile(path).Records)
                if (r.Shard == shard && r.SourceVersion >= 1 && r.Captured > when)
                    when = r.Captured;
            return when;
        }

        // rewrite every file older than the current version. Returns the number upgraded
        public static int Upgrade(List<string> report)
        {
            int upgraded = 0;
            foreach (string path in ArchiveFiles())
            {
                try
                {
                    HistoryFile info = ReadFile(path);
                    if (info.Version >= FileVersion)
                        continue;
                    WriteFile(path, info.Shard, info.Records);
                    report.Add(string.Format("Upgrade: {0}: version {1} to {2}, {3} players.", Path.GetFileName(path), info.Version, FileVersion, info.Records.Count));
                    upgraded++;
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    report.Add(string.Format("Upgrade: {0}: could not be read ({1}); left as is.", Path.GetFileName(path), ex.Message));
                }
            }
            return upgraded;
        }

        // for the patch (runs once per shard): upgrade old files, capture this shard, reload the archive.
        //  The development machine is never captured, and its files are left as checked in (its worlds are not the live
        //  shards; [PlayerHistory capture still works there for testing). A shard whose file already has its capture is
        //  left as is: the capture is the record, taken once.
        //  false: not done (this shard's file could not be read or written; the report says why)
        public static bool Maintain(List<string> report)
        {
            try
            {
                if (Core.Developing)
                {
                    report.Add(string.Format("Development machine ({0}): nothing upgraded or captured. [PlayerHistory capture to test.", Environment.MachineName));
                    return true;
                }

                Upgrade(report);

                bool captured = true;
                DateTime when = CapturedOn();
                if (when != DateTime.MinValue)
                    report.Add(string.Format("Capture: {0} was captured {1:yyyy-MM-dd}; left as is.", ThisShard, when));
                else
                {
                    captured = Capture(out string message);
                    report.Add(message);
                }

                Load();
                return captured;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                report.Add("PlayerHistory: " + ex.Message);
                return false;
            }
        }
        #endregion Capture and Upgrade

        #region Archive (Doppelgangers)
        private static readonly List<Record> m_All = new();     // every record in the archive
        private static readonly List<Record> m_Deck = new();    // the ones Doppelgangers may copy, in dealing order
        private static readonly Dictionary<Record, double> m_Weight = new();
        private static int m_Next;

        public static int Count { get { return m_Deck.Count; } }

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
        }

        public static void Initialize()
        {
            CommandSystem.Register("PlayerHistory", AccessLevel.Owner, new CommandEventHandler(PlayerHistory_OnCommand));
            CommandSystem.Register("PlayerInfo", AccessLevel.Player, new CommandEventHandler(PlayerInfo_OnCommand));
        }

        public static void Load()
        {
            m_All.Clear();
            m_Deck.Clear();
            m_Weight.Clear();
            m_Next = 0;

            foreach (string path in ArchiveFiles())
                try
                {
                    m_All.AddRange(ReadFile(path).Records);
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    Utility.Monitor.WriteLine("Player history: {0} could not be read: {1}", ConsoleColor.Red, path, ex.Message);
                }

            // names our players have (their own record from this shard is fine)
            Dictionary<string, PlayerMobile> live = new(StringComparer.OrdinalIgnoreCase);
            foreach (Mobile m in World.Mobiles.Values)
                if (m is PlayerMobile pm && !pm.Deleted && !string.IsNullOrWhiteSpace(pm.Name))
                    live[pm.Name.Trim()] = pm;

            // one record per name: the latest
            Dictionary<string, Record> best = new(StringComparer.OrdinalIgnoreCase);
            foreach (Record r in m_All)
            {
                string name = r.Name.Trim();
                if (name.Length == 0 || r.AccessLevel > AccessLevel.Player)
                    continue;
                if (live.TryGetValue(name, out PlayerMobile? pm) && LiveMatch(r) != pm)
                    continue;   // someone here has this name
                if (!r.Layers.Contains("OuterTorso"))
                    continue;   // we want clothes
                if (r.HasGameTime ? r.GameTime < MinGameTime : (r.Created == DateTime.MinValue || r.LastLogin - r.Created < MinTenure))
                    continue;   // didn't play long enough
                if (best.TryGetValue(name, out Record? have) && have.LastLogin >= r.LastLogin)
                    continue;
                best[name] = r;
            }

            m_Deck.AddRange(best.Values);
            Rank(m_Deck.Where(r => r.HasGameTime).OrderBy(r => r.GameTime).ToList());
            Rank(m_Deck.Where(r => !r.HasGameTime).OrderBy(r => r.Fame).ThenBy(r => r.LastLogin - r.Created).ToList());
            Deal();

            Console.WriteLine("Player history: {0} players on file, {1} for Doppelgangers.", m_All.Count, m_Deck.Count);
        }

        // weight 0.1 (least played) to 1.1 (most played), by rank within the group
        private static void Rank(List<Record> ascending)
        {
            for (int i = 0; i < ascending.Count; i++)
                m_Weight[ascending[i]] = 0.1 + (double)(i + 1) / ascending.Count;
        }

        // a weighted shuffle: well-played characters tend to come first; everyone once per deal
        private static void Deal()
        {
            Dictionary<Record, double> key = new();
            foreach (Record r in m_Deck)
                key[r] = -Math.Log(1.0 - Utility.RandomDouble()) / m_Weight[r];
            m_Deck.Sort((a, b) => key[a].CompareTo(key[b]));
            m_Next = 0;
        }

        // the next player for a Doppelganger (null: the archive is empty)
        public static Record? Pick()
        {
            if (m_Deck.Count == 0)
                return null;
            if (m_Next >= m_Deck.Count)
                Deal();
            return m_Deck[m_Next++];
        }

        // the record of a saved Doppelganger
        public static Record? Find(string? shard, int serial, string? name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            return m_All.FirstOrDefault(r => r.Shard == shard && (serial != 0 ? r.Serial == serial : string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
                ?? m_All.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        // the player on this shard a record is of (null: another shard, or gone)
        public static PlayerMobile? LiveMatch(Record r)
        {
            if (r.Serial == 0 || r.Shard != ThisShard)
                return null;
            return World.FindMobile(r.Serial) is PlayerMobile pm && !pm.Deleted && string.Equals(pm.Name, r.Name, StringComparison.OrdinalIgnoreCase) ? pm : null;
        }
        #endregion Archive (Doppelgangers)

        #region Commands
        [Usage("PlayerHistory status|capture|upgrade|reload")]
        [Description("The player history archive: show the files, capture this shard, upgrade old files, or reload the archive.")]
        private static void PlayerHistory_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            List<string> report = new();
            try
            {
                switch (e.Length > 0 ? e.GetString(0).ToLower() : "status")
                {
                    case "capture":
                        Capture(out string message);
                        report.Add(message);
                        Load();
                        break;
                    case "upgrade":
                        if (Upgrade(report) == 0)
                            report.Add("Upgrade: every file is current.");
                        Load();
                        break;
                    case "reload":
                        Load();
                        break;
                    case "status":
                        break;
                    default:
                        from.SendMessage("Usage: PlayerHistory status|capture|upgrade|reload");
                        return;
                }

                foreach (string path in ArchiveFiles())
                    try
                    {
                        HistoryFile info = ReadFile(path);
                        report.Add(string.Format("{0}: version {1}, {2}, {3} players{4}.", Path.GetFileName(path), info.Version, info.Shard,
                            info.Records.Count, info.Written != DateTime.MinValue ? ", written " + info.Written.ToString("yyyy-MM-dd", Inv) : string.Empty));
                    }
                    catch (Exception ex)
                    {
                        report.Add(string.Format("{0}: unreadable ({1}).", Path.GetFileName(path), ex.Message));
                    }
                report.Add(string.Format("This shard: {0} ({1}). {2} players for Doppelgangers.", ThisShard ?? "none", ThisShardFile ?? "no file", Count));
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                report.Add("PlayerHistory: " + ex.Message);
            }

            foreach (string line in report)
            {
                from.SendMessage(line);
                Console.WriteLine(line);
            }
        }

        [Usage("PlayerInfo")]
        [Description("Who a Doppelganger was: the player, their shard, and their days.")]
        private static void PlayerInfo_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target a Doppelganger.");
            e.Mobile.Target = new PlayerInfoTarget();
        }

        private class PlayerInfoTarget : Target
        {
            public PlayerInfoTarget()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is not Doppelganger doppel)
                {
                    from.SendMessage("That is not a Doppelganger.");
                    return;
                }

                Record? r = doppel.History;
                if (r == null)
                {
                    from.SendMessage("No one remembers who {0} was.", doppel.Name);
                    return;
                }

                foreach (string line in Describe(r))
                    from.SendMessage(line);
            }
        }

        public static List<string> Describe(Record r)
        {
            List<string> lines = new();
            int from = r.Year, to = r.LastLogin != DateTime.MinValue ? r.LastLogin.Year : from;
            lines.Add(from == to || from <= 1
                ? string.Format("{0} played on {1} in {2}.", r.Name, r.Shard, Math.Max(from, to))
                : string.Format("{0} played on {1} from {2} to {3}.", r.Name, r.Shard, from, to));

            if (!string.IsNullOrWhiteSpace(r.GuildName))
                lines.Add(string.IsNullOrWhiteSpace(r.GuildAbbr)
                    ? string.Format("Guild: {0}.", r.GuildName)
                    : string.Format("Guild: {0} [{1}].", r.GuildName, r.GuildAbbr));

            if (r.HasGameTime)
                lines.Add(string.Format(Inv, "Hours played: {0:N0}.", r.GameTime.TotalHours));

            lines.Add(string.Format(Inv, "Fame {0:N0}, karma {1:N0}.", r.Fame, r.Karma));

            if (r.Murderer)
                lines.Add(r.Kills >= 0 ? string.Format("A murderer, with {0} kills.", r.Kills) : "A murderer.");

            List<(string skill, double value)> skills = r.SkillList();
            if (skills.Count > 0)
                lines.Add("Best skills: " + string.Join(", ", skills.Take(3).Select(s => string.Format(Inv, "{0} {1:F1}", s.skill, s.value))) + ".");

            if (r.SourceVersion == 0)
                lines.Add("From the old records (2016-2024): hours and skills were not kept.");

            return lines;
        }
        #endregion Commands
    }
}
