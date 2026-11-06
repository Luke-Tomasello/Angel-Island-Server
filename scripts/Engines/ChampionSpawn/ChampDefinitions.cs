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

/* Scripts/Engines/ChampionSpawn/ChampDefinitions.cs
 * ChangeLog:
 *  10/27/26, Adam
 *      Created. Champion definitions now live in Data/ChampSpawns.xml (generated from ChampLevelData.CreateSpawn(), which
 *      was then removed with the SpawnTypes enum). A champ engine names its definition (ChampEngine.DefinitionName) and
 *      keeps the levels it was given as its own serialized snapshot: changing or removing a definition never touches a
 *      placed champ until a definition is applied to it again.
 *      The file is validated strictly when loaded: an unknown element or attribute, a bad number, a monster or captain that
 *      is not a Mobile type, unpaired names, a duplicate name, rejects that definition (reported in red and in
 *      Logs/ChampDefinitions.log); every name C# relies on (ChampNames) must be present.
 *      A numeric attribute is a literal or a reference to a public static integral field of an approved class
 *      (CoreAI.SpiritExpireDelay); a reference is checked when the file loads and read when the definition is applied.
 *      [ChampDefs lists the definitions; [ChampDefs reload (Administrator) reloads the file. Names gives [props its pick list
 *      for ChampEngine.DefinitionName ([StringList]).
 */

using Server.Commands;
using Server.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using static Server.Utility;

namespace Server.Engines.ChampionSpawn
{
    // Definition names that C# behavior depends on. Every one must exist in ChampSpawns.xml (checked at load).
    public static class ChampNames
    {
        public const string Abyss = "Abyss";
        public const string Arachnid = "Arachnid";
        public const string ColdBlood = "ColdBlood";
        public const string UnholyTerror = "UnholyTerror";
        public const string VerminHorde = "VerminHorde";
        public const string Doppelganger = "Doppelganger";
        public const string FrozenHost = "FrozenHost";
        public const string Bob = "Bob";
        public const string Vampire = "Vampire";
        public const string Pirate_Full_Sea = "Pirate_Full_Sea";
        public const string Pirate_Mini = "Pirate_Mini";
        public const string Pirate_Micro = "Pirate_Micro";
        public const string Deceit_Mini = "Deceit_Mini";
        public const string Wrong_Mini = "Wrong_Mini";
        public const string AI_Escape = "AI_Escape";
        public const string AI_Guard = "AI_Guard";
    }

    public sealed class ChampDefinition
    {
        public string Name { get; }
        public int LevelCount => m_Levels.Count;
        private readonly List<LevelDefinition> m_Levels;

        internal ChampDefinition(string name, List<LevelDefinition> levels)
        {
            Name = name;
            m_Levels = levels;
        }

        // a new set of levels for one champ engine; references are read now
        public ArrayList CreateLevels()
        {
            ArrayList list = new ArrayList();
            foreach (LevelDefinition level in m_Levels)
                list.Add(level.Create());
            return list;
        }

        internal sealed class LevelDefinition
        {
            public ChampValue MaxKills, MaxMobs, MaxSpawn, MaxRange, SpawnDelaySeconds, ExpireMinutes;
            public SpawnFlags Flags;
            public string[] Monsters = Array.Empty<string>();
            public string? Captain;
            public string[]? Names;
            public string?[]? Titles;

            public ChampLevelData Create()
            {
                return new ChampLevelData(MaxKills.Value, MaxMobs.Value, MaxSpawn.Value, MaxRange.Value,
                    TimeSpan.FromSeconds(SpawnDelaySeconds.Value), TimeSpan.FromMinutes(ExpireMinutes.Value), Flags,
                    (string[])Monsters.Clone(), Captain, (string[]?)Names?.Clone(), (string[]?)Titles?.Clone());
            }
        }
    }

    // a literal, or an approved public static field read when the definition is applied
    internal readonly struct ChampValue
    {
        private readonly int m_Literal;
        private readonly FieldInfo? m_Field;

        public ChampValue(int literal) { m_Literal = literal; m_Field = null; }
        public ChampValue(FieldInfo field) { m_Literal = 0; m_Field = field; }

        public int Value => m_Field == null ? m_Literal : Convert.ToInt32(m_Field.GetValue(null), CultureInfo.InvariantCulture);
    }

    public static class ChampDefinitions
    {
        public const string FileName = "ChampSpawns.xml";
        private const string LogFile = "ChampDefinitions.log";

        // the only classes a definition may read from, by the name used in the XML
        private static readonly Dictionary<string, Type> ApprovedSources = new(StringComparer.Ordinal)
        {
            ["CoreAI"] = typeof(CoreAI),
        };

        // field types that always convert to int
        private static readonly HashSet<Type> IntegralTypes = new() { typeof(int), typeof(short), typeof(ushort), typeof(byte), typeof(sbyte) };

        private static Dictionary<string, ChampDefinition> m_Definitions = new(StringComparer.OrdinalIgnoreCase);
        private static List<ChampDefinition> m_Ordered = new();

        public static IReadOnlyList<ChampDefinition> All => m_Ordered;

        // the definition names in file order ([props pick list for ChampEngine.DefinitionName)
        public static string[] Names => m_Ordered.Select(d => d.Name).ToArray();

        public static string FilePath => Path.Combine(Core.DataDirectory, FileName);

        public static void Configure()
        {
            // before the world loads: champ engines resolve their definition names while deserializing
            List<string> errors = new List<string>();
            if (!Load(FilePath, errors))
                Report(errors, "Champion definitions: not loaded");
            else
                Report(errors, string.Format("Champion definitions: {0} loaded from {1}", m_Ordered.Count, FileName));
        }

        public static void Initialize()
        {
            CommandSystem.Register("ChampDefs", AccessLevel.GameMaster, new CommandEventHandler(ChampDefs_OnCommand));
        }

        public static ChampDefinition? Find(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            m_Definitions.TryGetValue(name, out ChampDefinition? def);
            return def;
        }

        // a champ engine was asked for a definition that does not exist
        public static void ReportUnknown(string? name, Item engine)
        {
            Report(new List<string> { string.Format("{0} (0x{1:X8}): no champion definition named '{2}'; the champ is unchanged", engine.GetType().Name, engine.Serial.Value, name) }, null);
        }

        /// <summary>
        /// Loads the file. False if it cannot be read at all (the current definitions are kept); otherwise the valid
        /// definitions replace the current ones and every rejected definition is listed in errors.
        /// </summary>
        public static bool Load(string path, List<string> errors)
        {
            List<ChampDefinition>? ordered = Parse(path, errors);
            if (ordered == null)
                return false;

            Dictionary<string, ChampDefinition> definitions = new(StringComparer.OrdinalIgnoreCase);
            foreach (ChampDefinition def in ordered)
                definitions.Add(def.Name, def);

            m_Definitions = definitions;
            m_Ordered = ordered;
            return true;
        }

        /// <summary>
        /// Reads and validates the file without installing it: the valid definitions in file order, or null if the file
        /// cannot be read at all. Every rejected definition (and every missing ChampNames definition) is listed in errors.
        /// </summary>
        public static List<ChampDefinition>? Parse(string path, List<string> errors)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(path);
            }
            catch (Exception ex)
            {
                errors.Add(string.Format("{0}: {1}", path, ex.Message));
                return null;
            }

            XmlElement? root = doc.DocumentElement;
            if (root == null || root.Name != "ChampSpawns" || root.GetAttribute("version") != "1")
            {
                errors.Add(string.Format("{0}: expected <ChampSpawns version=\"1\">", path));
                return null;
            }

            Dictionary<string, ChampDefinition> definitions = new(StringComparer.OrdinalIgnoreCase);
            List<ChampDefinition> ordered = new List<ChampDefinition>();

            foreach (XmlNode node in root.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Comment || node.NodeType == XmlNodeType.Whitespace)
                    continue;
                if (node is not XmlElement champ || champ.Name != "Champ")
                {
                    errors.Add(string.Format("<ChampSpawns>: unexpected {0}", Describe(node)));
                    continue;
                }

                string name = champ.GetAttribute("name");
                List<string> problems = new List<string>();
                ChampDefinition? def = ParseChamp(champ, name, problems);

                if (def != null && definitions.ContainsKey(name))
                    problems.Add("duplicate name");

                if (def == null || problems.Count > 0)
                {
                    errors.Add(string.Format("Champ '{0}' rejected: {1}", name, string.Join("; ", problems)));
                    continue;
                }

                definitions.Add(name, def);
                ordered.Add(def);
            }

            foreach (FieldInfo required in typeof(ChampNames).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                string name = (string)required.GetValue(null)!;
                if (!definitions.TryGetValue(name, out ChampDefinition? def) || def.Name != name)
                    errors.Add(string.Format("Champ '{0}' is missing: C# behavior depends on it (ChampNames.{1})", name, required.Name));
            }

            return ordered;
        }

        private static ChampDefinition? ParseChamp(XmlElement champ, string name, List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim() != name)
                problems.Add("a champ needs a name without leading or trailing spaces");
            CheckAttributes(champ, problems, "name");

            List<ChampDefinition.LevelDefinition> levels = new List<ChampDefinition.LevelDefinition>();
            foreach (XmlNode node in champ.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Comment || node.NodeType == XmlNodeType.Whitespace)
                    continue;
                if (node is not XmlElement level || level.Name != "Level")
                {
                    problems.Add(string.Format("unexpected {0}", Describe(node)));
                    continue;
                }

                string where = string.Format("level {0}", levels.Count + 1);
                int repeat = level.HasAttribute("repeat") ? ParseLiteral(level, "repeat", 1, 100, where, problems) : 1;
                ChampDefinition.LevelDefinition? def = ParseLevel(level, where, problems);
                if (def != null)
                    for (int i = 0; i < repeat; i++)
                        levels.Add(def);
            }

            if (levels.Count == 0)
                problems.Add("no levels");

            return problems.Count == 0 ? new ChampDefinition(name, levels) : null;
        }

        private static ChampDefinition.LevelDefinition? ParseLevel(XmlElement level, string where, List<string> problems)
        {
            int before = problems.Count;
            CheckAttributes(level, problems, "repeat", "maxKills", "maxMobs", "maxSpawn", "maxRange", "spawnDelaySeconds", "expireMinutes", "flags", "captain");

            ChampDefinition.LevelDefinition def = new ChampDefinition.LevelDefinition
            {
                MaxKills = ParseValue(level, "maxKills", required: true, 1, where, problems),
                MaxMobs = ParseValue(level, "maxMobs", required: true, 1, where, problems),
                MaxSpawn = ParseValue(level, "maxSpawn", required: true, 1, where, problems),
                MaxRange = ParseValue(level, "maxRange", required: true, 0, where, problems),
                SpawnDelaySeconds = ParseValue(level, "spawnDelaySeconds", required: false, 0, where, problems),
                ExpireMinutes = ParseValue(level, "expireMinutes", required: true, 0, where, problems),
            };

            if (level.HasAttribute("flags"))
            {
                string text = level.GetAttribute("flags");
                if (text.Length == 0 || char.IsDigit(text[0]) || text[0] == '-' || !Enum.TryParse(text, ignoreCase: false, out SpawnFlags flags))
                    problems.Add(string.Format("{0}: flags '{1}' is not a SpawnFlags name", where, text));
                else
                    def.Flags = flags;
            }

            if (level.HasAttribute("captain"))
            {
                def.Captain = level.GetAttribute("captain");
                CheckMobileType(def.Captain, where + " captain", problems);
            }

            List<string> monsters = new List<string>();
            List<string> names = new List<string>();
            List<string?> titles = new List<string?>();
            foreach (XmlNode node in level.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Comment || node.NodeType == XmlNodeType.Whitespace)
                    continue;
                if (node is XmlElement monster && monster.Name == "Monster")
                {
                    CheckAttributes(monster, problems, "type");
                    string type = monster.GetAttribute("type");
                    CheckMobileType(type, where + " monster", problems);
                    monsters.Add(type);
                }
                else if (node is XmlElement named && named.Name == "Name")
                {
                    CheckAttributes(named, problems, "name", "title");
                    string text = named.GetAttribute("name");
                    if (string.IsNullOrWhiteSpace(text))
                        problems.Add(string.Format("{0}: a <Name> needs a name", where));
                    names.Add(text);
                    titles.Add(named.HasAttribute("title") ? named.GetAttribute("title") : null);
                }
                else
                    problems.Add(string.Format("{0}: unexpected {1}", where, Describe(node)));
            }

            if (monsters.Count == 0)
                problems.Add(string.Format("{0}: no <Monster>", where));

            def.Monsters = monsters.ToArray();
            if (names.Count > 0)
            {
                def.Names = names.ToArray();
                def.Titles = titles.ToArray();
            }

            return problems.Count == before ? def : null;
        }

        private static ChampValue ParseValue(XmlElement element, string attribute, bool required, int min, string where, List<string> problems)
        {
            if (!element.HasAttribute(attribute))
            {
                if (required)
                    problems.Add(string.Format("{0}: {1} is required", where, attribute));
                return new ChampValue(0);
            }

            string text = element.GetAttribute(attribute);
            if (text.Length > 0 && (char.IsDigit(text[0]) || text[0] == '-'))
                return new ChampValue(ParseLiteral(element, attribute, min, int.MaxValue, where, problems));

            // Alias.Field: an approved class's public static integral field, read when the definition is applied
            string[] parts = text.Split('.');
            if (parts.Length != 2 || !ApprovedSources.TryGetValue(parts[0], out Type? source))
            {
                problems.Add(string.Format("{0}: {1} '{2}' is neither a number nor an approved reference ({3}.Field)", where, attribute, text, string.Join(", ", ApprovedSources.Keys)));
                return new ChampValue(0);
            }

            FieldInfo? field = source.GetField(parts[1], BindingFlags.Public | BindingFlags.Static);
            if (field == null || !IntegralTypes.Contains(field.FieldType))
            {
                problems.Add(string.Format("{0}: {1} '{2}' is not a public static integral field", where, attribute, text));
                return new ChampValue(0);
            }

            return new ChampValue(field);
        }

        private static int ParseLiteral(XmlElement element, string attribute, int min, int max, string where, List<string> problems)
        {
            string text = element.GetAttribute(attribute);
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) || value < min || value > max)
            {
                problems.Add(string.Format("{0}: {1} '{2}' must be a whole number from {3} to {4}", where, attribute, text, min, max));
                return min;
            }
            return value;
        }

        private static void CheckMobileType(string name, string what, List<string> problems)
        {
            Type? type = ScriptCompiler.FindTypeByName(name);
            if (type == null || !typeof(Mobile).IsAssignableFrom(type) || type.IsAbstract)
                problems.Add(string.Format("{0} '{1}' is not a Mobile type", what, name));
        }

        private static void CheckAttributes(XmlElement element, List<string> problems, params string[] allowed)
        {
            foreach (XmlAttribute attribute in element.Attributes)
                if (Array.IndexOf(allowed, attribute.Name) < 0)
                    problems.Add(string.Format("<{0}>: unknown attribute '{1}'", element.Name, attribute.Name));
        }

        private static string Describe(XmlNode node)
        {
            return node is XmlElement e ? string.Format("<{0}>", e.Name) : node.NodeType.ToString();
        }

        // errors in red on the console and in Logs/ChampDefinitions.log; the summary (if any) in the normal color
        private static void Report(List<string> errors, string? summary)
        {
            foreach (string error in errors)
                Utility.Monitor.WriteLine("Champion definitions: " + error, ConsoleColor.Red);
            if (summary != null)
                Utility.Monitor.WriteLine(errors.Count == 0 ? summary : string.Format("{0} ({1} error(s))", summary, errors.Count),
                    errors.Count == 0 ? ConsoleColor.Gray : ConsoleColor.Red);

            if (errors.Count == 0)
                return;
            try
            {
                string path = Path.Combine(Core.LogsDirectory, LogFile);
                Utility.EnsurePath(path);
                string stamp = DateTime.UtcNow.ToString("u");
                File.AppendAllLines(path, errors.Select(e => stamp + " " + e));
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        [Usage("ChampDefs [reload]")]
        [Description("Lists the champion definitions (Data/ChampSpawns.xml); reload (Administrator) reads the file again.")]
        private static void ChampDefs_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (e.Length > 0 && e.GetString(0).Equals("reload", StringComparison.OrdinalIgnoreCase))
            {
                if (from.AccessLevel < AccessLevel.Administrator)
                {
                    from.SendMessage("Only an Administrator may reload the champion definitions.");
                    return;
                }

                List<string> errors = new List<string>();
                bool loaded = Load(FilePath, errors);
                Report(errors, loaded ? string.Format("Champion definitions: {0} reloaded from {1} by {2}", m_Ordered.Count, FileName, from) : null);
                foreach (string error in errors)
                    from.SendMessage(0x22, error);
                from.SendMessage(loaded
                    ? string.Format("{0} champion definitions loaded ({1} error(s)). Placed champs keep their levels until a definition is applied again.", m_Ordered.Count, errors.Count)
                    : "The file could not be read; the current definitions are unchanged.");
                return;
            }

            from.SendMessage("{0} champion definitions (set DefinitionName on a champ engine):", m_Ordered.Count);
            from.SendMessage(string.Join(", ", m_Ordered.Select(d => string.Format("{0} ({1})", d.Name, d.LevelCount))));
        }
    }
}
