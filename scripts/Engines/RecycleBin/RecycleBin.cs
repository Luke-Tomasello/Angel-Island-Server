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

/* Scripts\Engines\RecycleBin\RecycleBin.cs
 * CHANGELOG:
 *  8/12/2023, Adam
 *      Handle reading back a null container
 *      Actually deleted the container if older than 10 days
 *  2/18/22, Adam
 *      Log and Delete items that cannot be recycled.
 *	10/2/21, Adam
 *		first time check in
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Items;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Items
{
    public class RecycleBin : StrongBackpack
    {
        public RecycleBin()
            : base()
        {
            this.MaxItems = 0;                              // unlimited
            this.Movable = false;                           // don't decay / cleanup
            this.IsIntMapStorage = true;                    // don't decay / cleanup
        }
        public RecycleBin(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}

namespace Server.Engines.RecycleBin
{
    public static class RecycleBin
    {
        private static Dictionary<Serial, Container> m_database = new Dictionary<Serial, Container>();
        public static Dictionary<Serial, Container> Database { get { return m_database; } }
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }
        public static void Initialize()
        {
            CommandSystem.Register("RecycleBin", AccessLevel.Owner, new CommandEventHandler(RecycleBin_OnCommand));
        }
        public static void Add(Item item, Container container, string logdata)
        {
            if (item == null) return;
            if (item.Deleted) return;
            if (item.Parent is Container c)
            {   // ghost gold midigation
                c.RemoveItem(item);
                c.UpdateAllTotals();
                item.UpdateTotals();
            }
            if (m_database.ContainsKey(container.Serial))
            {
                LogHelper? logger = null;
                if (TryDropItem(item, m_database[container.Serial]) == false)
                {
                    // Terminal condition:
                    // The recycle bin itself is full. At this point we have exhausted all safe recovery options.
                    // Dropping to the world would re-open the overload exploit (client crash / lag vector),
                    // so the only remaining safe action is to destroy the incoming item.
                    //
                    // In other words: too bad. This is either deliberate exploitation or irrecoverable misuse.
                    string text = string.Format(
                        "RecycleBin FULL: source=0x{0:X8} bin=0x{1:X8}. Deleting item=0x{2:X8} {3} amt={4} wt={5}.",
                        (int)container.Serial,
                        (int)m_database[container.Serial].Serial,
                        (int)item.Serial,
                        item.GetType().Name,
                        item.Amount,
                        item.Weight);
                    Console.WriteLine(text);
                    logger = new LogHelper("RecycleBin.log", overwrite: false, sline: false, rolloverMb: 10);
                    logger.Log(LogType.Text, logdata);
                    logger.Log(LogType.Text, text);
                    logger.Finish();
                    item.Delete();
                    return;
                }

                // Log it
                logger = new LogHelper("RecycleBin.log", overwrite: false, sline: false, rolloverMb: 10);
                logger.Log(LogType.Text, logdata);
                logger.Finish();
            }
            else
            {
                Items.RecycleBin cont = new Items.RecycleBin();     // RecycleBin holds 1600 stones. If the user overflows that much, too bad.
                m_database.Add(container.Serial, cont);
                Add(item, container, logdata);
            }
        }

        public static bool TryDropItem(Item item, Container to)
        {
            if (to.TotalWeight + item.Weight >= to.MaxWeight)
                return false;

            to.DropItem(item);
            return true;
        }

        #region Serialization
        public static void Load()
        {
            if (!File.Exists("Saves/RecycleBin.bin"))
                return;

            Console.WriteLine("Recycle Bin Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/RecycleBin.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 2:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Serial s = (Serial)reader.ReadInt();

                                StrongBackpack? cont = (StrongBackpack?)reader.ReadItem();
                                // we will delete this container after 10 days since creation.
                                //  we understand they may have added things since then. 
                                if (cont != null && DateTime.UtcNow < cont.Created + TimeSpan.FromDays(10))
                                    m_database.Add(s, cont);
                                else
                                {
                                    if (cont != null)
                                    {
                                        string logdata = string.Format("Emptying recycle bin: {0} for container: {1}", cont.Serial, s);
                                        Utility.Monitor.WriteLine(logdata, ConsoleColor.Yellow);
                                        LogHelper logger = new LogHelper("RecycleBin.log", overwrite: false, sline: false, rolloverMb: 10);
                                        logger.Log(LogType.Text, logdata);
                                        logger.Finish();
                                        cont.Delete();
                                    }
                                    else
                                    {
                                        string logdata = string.Format("Null bin entry: for container: {1}", s);
                                        Utility.Monitor.WriteLine(logdata, ConsoleColor.Red);
                                        LogHelper logger = new LogHelper("RecycleBin.log", overwrite: false, sline: false, rolloverMb: 10);
                                        logger.Log(LogType.Text, logdata);
                                        logger.Finish();
                                    }
                                }
                            }
                            break;
                        }
                    case 1:
                        {
                            // initial empty database
                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid RecycleBin.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading Saves/RecycleBin.bin, using default values:", ConsoleColor.Red);
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("RecycleBin Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/RecycleBin.bin", true);
                int version = 2;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 2:
                        {
                            writer.Write(m_database.Count);
                            foreach (KeyValuePair<Serial, Container> kvp in m_database)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value);
                            }
                            break;
                        }
                    case 1:
                        {
                            // initial empty database
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Saves/RecycleBin.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion Serialization

        public enum Operation
        {
            INFO,
            RECOVER
        }
        // RecycleBin [info|recover|list] [serial]
        // - If serial is provided (hex, with or without 0x), no target is requested.
        // - Otherwise, prompts to target a container.
        //
        // Examples:
        //   RecycleBin info
        //   RecycleBin recover
        //   RecycleBin info 0x40012345
        //   RecycleBin recover 40012345

        // RecycleBin [info|recover|list] [serial]
        // list: shows entries and expiration (10 days from Created)

        // Updated to support lookup by either serial OR name token.
        // Name token matching is case-insensitive and checks:
        // 1) RootParent Mobile.Name (most useful: player name)
        // 2) RootParent Item.Name (if any)
        // 3) The container item Name (if any)

        [Usage("RecycleBin info|recover|list [serialOrName]")]
        [Description("Dumps info about a container's overloaded items, optionally recovers them, or lists recycle-bin entries. Optionally takes a container serial (hex) or a name token to avoid targeting.")]
        public static void RecycleBin_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e == null || e.Mobile == null)
                    return;

                Operation op;
                bool isList;

                if (!TryParseRecycleBinArgs(e,
                    out op,
                    out Serial containerSerial,
                    out bool hasSerial,
                    out string nameToken,
                    out bool hasName,
                    out isList))
                {
                    e.Mobile.SendMessage("Usage: RecycleBin [recover|info|list] [serialHex|nameToken]");
                    e.Mobile.SendMessage("Examples: RecycleBin list");
                    e.Mobile.SendMessage("          RecycleBin list 0x40012345");
                    e.Mobile.SendMessage("          RecycleBin list Morgoth");
                    e.Mobile.SendMessage("          RecycleBin recover Morgoth");
                    return;
                }

                if (isList)
                {
                    // If serial provided, list only that entry; else if name provided, list matches; else list all.
                    ListRecycleBins(e.Mobile,
                        hasSerial ? (Serial?)containerSerial : null,
                        hasName ? nameToken : null);
                    return;
                }

                // Direct path: serial or name
                if (hasSerial)
                {
                    ProcessRecycleBin(e.Mobile, op, containerSerial);
                    return;
                }

                if (hasName)
                {
                    ProcessRecycleBinByName(e.Mobile, op, nameToken);
                    return;
                }

                // Target path: ask user to target a container.
                e.Mobile.SendMessage("Target the container you wish to process.");
                e.Mobile.Target = new ContainerTarget(op);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        private static bool TryParseRecycleBinArgs(CommandEventArgs e, out Operation op, out Serial containerSerial, out bool hasSerial, out string nameToken, out bool hasName, out bool isList)
        {
            op = Operation.INFO;
            containerSerial = Serial.MinusOne;
            hasSerial = false;
            nameToken = null;
            hasName = false;
            isList = false;

            if (e.Arguments == null || e.Length < 1 || e.Length > 2)
                return false;

            string opToken = (e.Arguments[0] ?? string.Empty).Trim().ToLowerInvariant();

            if (opToken == "list")
            {
                isList = true;
            }
            else if (opToken == "info")
            {
                op = Operation.INFO;
            }
            else if (opToken == "recover")
            {
                op = Operation.RECOVER;
            }
            else
            {
                return false;
            }

            if (e.Length == 2)
            {
                string token = (e.Arguments[1] ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(token))
                    return false;

                // Prefer serial parsing if it looks like hex or parses as hex.
                if (LooksLikeHexSerial(token) && Utility.TryParseHexUInt(token, out uint raw))
                {
                    containerSerial = (int)raw;
                    hasSerial = true;
                    return true;
                }

                // Otherwise treat as name token.
                nameToken = token;
                hasName = true;
            }

            return true;
        }

        private static bool LooksLikeHexSerial(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return true;

            // If it contains any A-F, it's probably hex.
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if ((c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))
                    return true;
            }

            return false;
        }

        private static void ProcessRecycleBinByName(Mobile from, Operation op, string nameToken)
        {
            if (from == null)
                return;

            var matches = FindRecycleBinMatchesByName(nameToken);

            if (matches == null || matches.Count == 0)
            {
                from.SendMessage("No RecycleBin units found matching that name token.");
                return;
            }

            if (matches.Count > 1)
            {
                from.SendMessage("Multiple recycle bins match that name token. Please disambiguate with serial:");
                for (int i = 0; i < matches.Count; i++)
                {
                    var m = matches[i];
                    from.SendMessage(string.Format("  0x{0:X8} Parent: {1}", (int)m.Serial, m.ParentText));
                }
                return;
            }

            // Exactly one match
            ProcessRecycleBin(from, op, matches[0].Serial);
        }

        private sealed class RecycleBinMatch
        {
            public Serial Serial;
            public string ParentText;
        }

        private static List<RecycleBinMatch> FindRecycleBinMatchesByName(string nameToken)
        {
            var db = Engines.RecycleBin.RecycleBin.Database;
            if (db == null || db.Count == 0)
                return new List<RecycleBinMatch>();

            if (string.IsNullOrWhiteSpace(nameToken))
                return new List<RecycleBinMatch>();

            string needle = nameToken.Trim();
            string needleLower = needle.ToLowerInvariant();

            List<RecycleBinMatch> results = new List<RecycleBinMatch>();

            foreach (var kvp in db)
            {
                Serial containerSerial = kvp.Key;
                Container cont = kvp.Value;

                if (cont == null)
                    continue;

                Item? item = World.FindItem(containerSerial);
                string parentText = BuildOverflowContextDescriptor(item);

                // Candidate strings to match against
                string contName = cont.Name ?? string.Empty;

                string parentMobileName = string.Empty;
                string parentItemName = string.Empty;

                if (item != null)
                {
                    object root = item.RootParent;

                    Mobile? m = root as Mobile;
                    if (m != null && !string.IsNullOrEmpty(m.Name))
                        parentMobileName = m.Name;

                    Item? i = root as Item;
                    if (i != null && !string.IsNullOrEmpty(i.Name))
                        parentItemName = i.Name;

                    // House/world case: RootParent is null, so fall back to immediate parent container if present.
                    if (root == null)
                    {
                        Item? p = item.Parent as Item;
                        if (p != null && !string.IsNullOrEmpty(p.Name))
                            parentItemName = p.Name;
                    }
                }

                if (StringContains(parentMobileName, needleLower) ||
                    StringContains(parentItemName, needleLower) ||
                    StringContains(contName, needleLower))
                {
                    results.Add(new RecycleBinMatch
                    {
                        Serial = containerSerial,
                        ParentText = parentText
                    });
                }
            }

            return results;
        }
        private static string BuildOverflowContextDescriptor(Item? item)
        {
            if (item == null)
                return "null";

            object root = item.RootParent;

            // If its ultimately held by a player/mob, that's the best context.
            if (root is Mobile rm)
                return string.Format("{0} 0x{1:X8}", rm.Name, (int)rm.Serial);

            // If its ultimately inside another item (pack, bankbox, etc).
            if (root is Item ri)
                return DescribeItem(ri);

            // RootParent is null when its in the world (common for houses/lockdowns).
            // Fall back to immediate parent container if any.
            if (item.Parent is Item p)
                return string.Format("World in {0}", DescribeItem(p));

            return "World";
        }

        private static string DescribeItem(Item i)
        {
            if (i == null)
                return "null";

            string name = string.IsNullOrEmpty(i.Name) ? i.GetType().Name : i.Name;
            return string.Format("{0} 0x{1:X8}", name, (int)i.Serial);
        }
        private static bool StringContains(string haystack, string needleLower)
        {
            if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needleLower))
                return false;

            return haystack.ToLowerInvariant().Contains(needleLower);
        }

        private static void ProcessRecycleBin(Mobile from, Operation op, Serial containerSerial)
        {
            if (from == null)
                return;

            if (Engines.RecycleBin.RecycleBin.Database == null ||
                !Engines.RecycleBin.RecycleBin.Database.TryGetValue(containerSerial, out Container? unit) ||
                unit == null)
            {
                from.SendMessage("No RecycleBin units found for that container serial.");
                return;
            }

            int archived = unit.Items != null ? unit.Items.Count : 0;

            if (op == Operation.INFO)
            {
                from.SendMessage(string.Format("Recycle bin: {0} with: {1} items archived.", unit.Serial, archived));
                from.SendMessage("There is 1 matching recycle bin.");
                from.SendMessage(string.Format("{0} total items archived.", archived));
                return;
            }

            // RECOVER
            if (archived <= 0)
            {
                from.SendMessage("There are no archived items to recover.");
                return;
            }

            unit.MoveToWorld(from.Location, from.Map);
            unit.Visible = true;
            unit.Movable = true;

            Engines.RecycleBin.RecycleBin.Database.Remove(containerSerial);

            from.SendMessage("Done.");
        }

        private static void ListRecycleBins(Mobile from, Serial? filterSerial, string? filterNameToken)
        {
            if (from == null)
                return;

            var db = Engines.RecycleBin.RecycleBin.Database;
            if (db == null || db.Count == 0)
            {
                from.SendMessage("RecycleBin database is empty.");
                return;
            }

            const int retentionDays = 10;
            TimeSpan retention = TimeSpan.FromDays(retentionDays);

            int shown = 0;

            string? nameNeedleLower = null;
            if (!string.IsNullOrWhiteSpace(filterNameToken))
                nameNeedleLower = filterNameToken.Trim().ToLowerInvariant();

            LogHelper logger = new LogHelper("RecycleBinDatabase.log", true, true);

            foreach (var kvp in db)
            {
                Serial containerSerial = kvp.Key;
                Container cont = kvp.Value;

                if (filterSerial.HasValue && containerSerial != filterSerial.Value)
                    continue;

                if (cont == null)
                    continue;

                Item? parentItem = World.FindItem(containerSerial);

                // Name filter path
                if (nameNeedleLower != null)
                {
                    string contName = cont.Name ?? string.Empty;

                    string parentMobileName = string.Empty;
                    string parentItemName = string.Empty;

                    if (parentItem != null)
                    {
                        object root = parentItem.RootParent;
                        Mobile? m = root as Mobile;
                        if (m != null && !string.IsNullOrEmpty(m.Name))
                            parentMobileName = m.Name;

                        Item? i = root as Item;
                        if (i != null && !string.IsNullOrEmpty(i.Name))
                            parentItemName = i.Name;
                    }

                    if (!StringContains(parentMobileName, nameNeedleLower) &&
                        !StringContains(parentItemName, nameNeedleLower) &&
                        !StringContains(contName, nameNeedleLower))
                    {
                        continue;
                    }
                }

                // Parent info (source-of-truth container)
                string parentText = BuildParentDescriptor(parentItem);

                // Expiration: Created + retention
                DateTime deleteAt = cont.Created + retention;

                // Only show non-expired entries
                if (DateTime.UtcNow >= deleteAt)
                    continue;

                TimeSpan remaining = deleteAt - DateTime.UtcNow;
                string remainingText = FormatTimeRemaining(remaining);

                string line = string.Format(
                    "Recycle Bin: 0x{0:X8}, Owner: {1} Will be deleted on: {2:u} ({3} remaining)",
                    (int)containerSerial,
                    parentText,
                    deleteAt,
                    remainingText);

                from.SendMessage(line);
                logger.Log(LogType.Text, line);

                shown++;
            }

            logger.Finish();

            if (filterSerial.HasValue)
                from.SendMessage(string.Format("Listed {0} matching entry(ies).", shown));
            else if (!string.IsNullOrWhiteSpace(filterNameToken))
                from.SendMessage(string.Format("Listed {0} matching entry(ies).", shown));
            else
                from.SendMessage(string.Format("Listed {0} entry(ies).", shown));

            from.SendMessage("Full listing written to Logs/RecycleBinDatabase.log");
        }

        private static string BuildParentDescriptor(Item? item)
        {
            if (item == null)
                return "null"; // item not found

            object root = item.RootParent;

            // World-placed item (house lockdown, floor, etc.)
            if (root == null)
                return item.ToString();

            Mobile? m = root as Mobile;
            if (m != null)
            {
                string? acct = null;

                if (m.Account is Account a && !string.IsNullOrEmpty(a.Username))
                    acct = a.Username;

                if (!string.IsNullOrEmpty(acct))
                    return string.Format("{0} ({1}) 0x{2:X8}", m.Name, acct, (int)m.Serial);

                return string.Format("{0} 0x{1:X8}", m.Name, (int)m.Serial);
            }

            Item? i = root as Item;
            if (i != null)
            {
                string name = string.IsNullOrEmpty(i.Name) ? i.GetType().Name : i.Name;
                return string.Format("{0} 0x{1:X8}", name, (int)i.Serial);
            }

            return "unknown";
        }

        private static string FormatTimeRemaining(TimeSpan ts)
        {
            if (ts.TotalSeconds <= 0)
                return "0s";

            if (ts.TotalDays >= 1)
                return string.Format("{0}d {1}h", (int)ts.TotalDays, ts.Hours);

            if (ts.TotalHours >= 1)
                return string.Format("{0}h {1}m", (int)ts.TotalHours, ts.Minutes);

            if (ts.TotalMinutes >= 1)
                return string.Format("{0}m {1}s", (int)ts.TotalMinutes, ts.Seconds);

            return string.Format("{0}s", (int)ts.TotalSeconds);
        }

        public class ContainerTarget : Target
        {
            private readonly Operation m_op;

            public ContainerTarget(Operation op)
                : base(12, false, TargetFlags.None)
            {
                m_op = op;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Container? container = targeted as Container;
                if (container == null)
                {
                    from.SendMessage("That is not a container.");
                    return;
                }

                ProcessRecycleBin(from, m_op, container.Serial);
            }
        }

    }
}