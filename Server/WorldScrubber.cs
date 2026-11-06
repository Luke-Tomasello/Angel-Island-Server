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

/* Server\WorldScrubber.cs
 * ChangeLog
 *  10/7/26, Claude
 *      Initial version.
 *      Standalone, cold (pre-World.Load) scrubber for serialized Item and Mobile types.
 *      Given one or more fully-qualified serialized type names, removes their records from the
 *      saved world so that an ordinary World.Load proceeds as if those objects never existed.
 *
 *      On-disk contract (see World.Load / World.SaveItems / World.SaveMobiles):
 *          <Kind>.tdb  Int32 count, then count type names (7-bit encoded byte length + UTF-8 bytes).
 *                      A record's typeID is an index into this list.
 *          <Kind>.idx  Int32 count, then count 20-byte records:
 *                      Int32 typeID, Int32 serial, Int64 position, Int32 length (little-endian).
 *          <Kind>.bin  Serialized records, located only through the index.
 *
 *      Transformation (deliberately the smallest one):
 *          .idx  records of retired types are dropped; every surviving record is kept byte-for-byte
 *                and in its original order.
 *          .tdb  retired entries are replaced in place by a resolvable placeholder (Server.Item or
 *                Server.Mobile). Type IDs are never renumbered. World.Load resolves the placeholder
 *                but never constructs it, since no record refers to it, and the next World.Save
 *                writes a fresh type table without it.
 *          .bin  never touched. Orphaned bytes are never read and the next World.Save compacts them.
 *
 *      Because type IDs are not renumbered, every .idx is consistent with both the old and new .tdb.
 *      Files are replaced .idx first, then .tdb, so an interrupted replacement can only leave the
 *      original world minus some index records, still naming the retired type (the same kind of state
 *      RunUO's own "Delete all objects of that type?" path produces). Rerunning the scrub completes it.
 *      An expected .idx or .tdb is never absent: World.Load treats a missing one as an empty object set.
 */

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Server
{
    public enum ScrubSaveKind
    {
        Items,
        Mobiles,
    }

    public static class WorldScrubber
    {
        public const string DefaultSavesDirectory = "Saves";
        public const string TempSuffix = ".scrub.tmp";
        public const string BackupFolderName = "ScrubBackups";
        public const string ReportFileName = "scrub-report.txt";

        private const int IndexRecordSize = 20;

        private static readonly UTF8Encoding m_StrictUTF8 = new UTF8Encoding(false, true);

        /// <summary>
        /// Scrubs the live save set (Saves/) before World.Load. Refuses any requested type that still
        /// exists in this executable, since the intended use is retiring types whose classes were removed.
        /// Returns normally on success, including when nothing needed to change.
        /// Throws WorldScrubException if anything is wrong; the original save is then left as it was.
        /// </summary>
        public static ScrubReport Scrub(IEnumerable<string> fullTypeNames)
        {
            return Scrub(DefaultSavesDirectory, fullTypeNames, false);
        }

        /// <summary>
        /// Scrubs the save set rooted at savesDirectory (the folder containing Items/ and Mobiles/).
        /// allowResolvableTypes: false (the normal case) refuses any requested name that World.Load would
        /// still resolve to a class in this executable. Pass true only to deliberately remove the
        /// persisted instances of a type that still exists.
        /// </summary>
        public static ScrubReport Scrub(string savesDirectory, IEnumerable<string> fullTypeNames, bool allowResolvableTypes)
        {
            return Run(savesDirectory, fullTypeNames, allowResolvableTypes, true);
        }

        /// <summary>
        /// Performs every read, validation and planning step of Scrub, and reports what Scrub would do,
        /// without writing anything.
        /// </summary>
        public static ScrubReport Preview(string savesDirectory, IEnumerable<string> fullTypeNames, bool allowResolvableTypes)
        {
            return Run(savesDirectory, fullTypeNames, allowResolvableTypes, false);
        }

        #region Run

        private static ScrubReport Run(string savesDirectory, IEnumerable<string> fullTypeNames, bool allowResolvableTypes, bool write)
        {
            if (savesDirectory == null)
                throw new ArgumentNullException(nameof(savesDirectory));

            if (fullTypeNames == null)
                throw new ArgumentNullException(nameof(fullTypeNames));

            ScrubReport report = new ScrubReport(Path.GetFullPath(savesDirectory), write);

            CheckWorldState(report);

            List<string> requested = ValidateRequest(fullTypeNames, report);

            CheckPlaceholders(report);

            if (!allowResolvableTypes)
                CheckNotResolvable(requested, report);

            SaveSet[] sets = new SaveSet[]
                {
                    new SaveSet(ScrubSaveKind.Items, report.SavesDirectory, "Items", typeof(Item).FullName),
                    new SaveSet(ScrubSaveKind.Mobiles, report.SavesDirectory, "Mobiles", typeof(Mobile).FullName),
                };

            foreach (SaveSet set in sets)
                ReadAndValidate(set, report);

            foreach (string name in requested)
                report.AddType(Match(name, sets));

            foreach (SaveSet set in sets)
                Plan(set, report);

            List<SaveSet> changed = sets.Where(s => s.Changed).ToList();

            if (changed.Count == 0)
            {
                report.Outcome = ScrubOutcome.NoChange;
                return report;
            }

            if (!write)
            {
                report.Outcome = ScrubOutcome.WouldScrub;
                return report;
            }

            Commit(sets, changed, report);

            report.Outcome = ScrubOutcome.Scrubbed;
            return report;
        }

        private static void CheckWorldState(ScrubReport report)
        {
            if (!PathsEqual(report.SavesDirectory, Path.GetFullPath(DefaultSavesDirectory)))
                return;

            if (World.Loading || World.Loaded || World.Saving)
                throw new WorldScrubException("The live save set can only be scrubbed before World.Load.", report);
        }

        private static List<string> ValidateRequest(IEnumerable<string> fullTypeNames, ScrubReport report)
        {
            List<string> requested = new List<string>();

            foreach (string name in fullTypeNames)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new WorldScrubException("A requested type name is null or blank.", report);

                if (name.Trim() != name)
                    throw new WorldScrubException(string.Format("Requested type name '{0}' has leading or trailing whitespace.", name), report);

                if (IsPlaceholderName(name))
                    throw new WorldScrubException(string.Format("'{0}' is a scrubber placeholder and cannot be scrubbed.", name), report);

                if (requested.Contains(name))
                {
                    report.AddWarning(string.Format("'{0}' was requested more than once.", name));
                    continue;
                }

                requested.Add(name);
            }

            return requested;
        }

        private static bool IsPlaceholderName(string name)
        {
            return string.Equals(name, typeof(Item).FullName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, typeof(Mobile).FullName, StringComparison.OrdinalIgnoreCase);
        }

        // The placeholders must resolve exactly as World.Load resolves type names, to a concrete type with
        // the (Serial) constructor World.Load requires of every type-table entry.
        private static void CheckPlaceholders(ScrubReport report)
        {
            foreach (Type type in new Type[] { typeof(Item), typeof(Mobile) })
            {
                if (Resolve(type.FullName) != type || type.IsAbstract || type.GetConstructor(new Type[] { typeof(Serial) }) == null)
                    throw new WorldScrubException(string.Format("Placeholder type '{0}' is not resolvable and constructible as World.Load requires.", type.FullName), report);
            }
        }

        private static void CheckNotResolvable(List<string> requested, ScrubReport report)
        {
            List<string> live = requested.Where(name => Resolve(name) != null).ToList();

            if (live.Count > 0)
                throw new WorldScrubException(string.Format("Refusing to scrub type(s) that still exist in this executable: {0}.", string.Join(", ", live)), report);
        }

        // Resolves a name exactly as World.Load does.
        private static Type Resolve(string? fullName)
        {
            _ = ScriptCompiler.Assemblies; // populates the assembly table FindTypeByFullName searches, as at startup

            return ScriptCompiler.FindTypeByFullName(fullName);
        }

        #endregion Run

        #region Read and Validate

        private static void ReadAndValidate(SaveSet set, ScrubReport report)
        {
            bool idxExists = File.Exists(set.IdxPath);
            bool tdbExists = File.Exists(set.TdbPath);

            if (!idxExists && !tdbExists)
            {
                // World.Load treats this kind as having no objects; there is nothing to scrub.
                report.AddWarning(string.Format("{0}: no save files found ({1}).", set.Kind, set.Directory));
                return;
            }

            if (idxExists != tdbExists)
                throw new WorldScrubException(string.Format("{0}: '{1}' exists but '{2}' does not.", set.Kind, idxExists ? set.IdxPath : set.TdbPath, idxExists ? set.TdbPath : set.IdxPath), report);

            set.Present = true;
            set.IdxRaw = File.ReadAllBytes(set.IdxPath);
            set.TdbRaw = File.ReadAllBytes(set.TdbPath);
            set.BinExists = File.Exists(set.BinPath);
            set.BinLength = set.BinExists ? new FileInfo(set.BinPath).Length : 0;

            try
            {
                set.Types = ParseTypeTable(set.TdbRaw);
                set.Records = ParseIndex(set.IdxRaw);
                ValidateRecords(set.Records, set.Types.Count, set.BinExists, set.BinLength);

                // The model must reproduce the original files exactly, or we must not write files with it.
                if (!BytesEqual(WriteTypeTable(set.Types), set.TdbRaw))
                    throw new InvalidDataException("the type table cannot be reproduced byte-for-byte");

                if (!BytesEqual(WriteIndex(set.Records), set.IdxRaw))
                    throw new InvalidDataException("the index cannot be reproduced byte-for-byte");
            }
            catch (InvalidDataException e)
            {
                throw new WorldScrubException(string.Format("{0}: structural validation failed: {1}.", set.Kind, e.Message), report, e);
            }

            AddSoftWarnings(set, report);

            report.AddSaveSet(new ScrubSaveSetResult(set.Kind, set.Types.Count, set.Records.Count, set.BinLength));
        }

        private static void ValidateRecords(List<IndexRecord> records, int typeCount, bool binExists, long binLength)
        {
            if (records.Count > 0 && !binExists)
                throw new InvalidDataException(string.Format("the index has {0} records but the .bin file is missing", records.Count));

            for (int i = 0; i < records.Count; i++)
            {
                IndexRecord r = records[i];

                if (r.TypeID < 0 || r.TypeID >= typeCount)
                    throw new InvalidDataException(string.Format("index record {0} (serial 0x{1:X}) has type ID {2}, but the type table has {3} entries", i, r.Serial, r.TypeID, typeCount));

                if (r.Position < 0 || r.Length < 0 || r.Position > binLength - r.Length)
                    throw new InvalidDataException(string.Format("index record {0} (serial 0x{1:X}) spans {2}+{3}, outside the {4}-byte .bin", i, r.Serial, r.Position, r.Length, binLength));
            }
        }

        // Anomalies that World.Load tolerates. Reported, not fatal, and carried through unchanged.
        private static void AddSoftWarnings(SaveSet set, ScrubReport report)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < set.Types.Count; i++)
            {
                string name = set.Types[i].Name;

                if (name.Length == 0)
                    report.AddWarning(string.Format("{0}: type table entry {1} is empty.", set.Kind, i));
                else if (!names.Add(name) && !IsPlaceholderName(name))
                    report.AddWarning(string.Format("{0}: type table entry {1} ('{2}') is a duplicate.", set.Kind, i, name));
            }

            HashSet<int> serials = new HashSet<int>();
            int duplicates = 0, wrongKind = 0;

            foreach (IndexRecord r in set.Records)
            {
                Serial serial = r.Serial;

                if (!serials.Add(r.Serial))
                    duplicates++;

                if (set.Kind == ScrubSaveKind.Items ? !serial.IsItem : !serial.IsMobile)
                    wrongKind++;
            }

            if (duplicates > 0)
                report.AddWarning(string.Format("{0}: {1} index record(s) repeat an earlier serial.", set.Kind, duplicates));

            if (wrongKind > 0)
                report.AddWarning(string.Format("{0}: {1} index record(s) have a serial outside the {0} range.", set.Kind, wrongKind));
        }

        #endregion Read and Validate

        #region Match and Plan

        private static ScrubTypeResult Match(string name, SaveSet[] sets)
        {
            ScrubTypeResult result = new ScrubTypeResult(name);

            foreach (SaveSet set in sets)
            {
                if (!set.Present)
                    continue;

                for (int i = 0; i < set.Types.Count; i++)
                {
                    string entry = set.Types[i].Name;

                    if (string.Equals(entry, name, StringComparison.Ordinal))
                    {
                        result.AddEntry(set.Kind, i);
                        set.Retired.Add(i);
                    }
                    else if (string.Equals(entry, name, StringComparison.OrdinalIgnoreCase))
                    {
                        result.AddCaseOnlyNearMiss(string.Format("{0} type table entry {1}: '{2}'", set.Kind, i, entry));
                    }
                }
            }

            return result;
        }

        private static void Plan(SaveSet set, ScrubReport report)
        {
            if (!set.Present || set.Retired.Count == 0)
                return;

            Dictionary<int, int> removedByTypeID = new Dictionary<int, int>();

            set.NewRecords = new List<IndexRecord>(set.Records.Count);

            foreach (IndexRecord r in set.Records)
            {
                if (set.Retired.Contains(r.TypeID))
                {
                    int count;
                    removedByTypeID.TryGetValue(r.TypeID, out count);
                    removedByTypeID[r.TypeID] = count + 1;
                }
                else
                {
                    set.NewRecords.Add(r);
                }
            }

            TypeEntry placeholder = new TypeEntry(set.Placeholder, EncodeTypeName(set.Placeholder));

            set.NewTypes = new List<TypeEntry>(set.Types);

            foreach (int typeID in set.Retired)
                set.NewTypes[typeID] = placeholder;

            set.NewIdx = WriteIndex(set.NewRecords);
            set.NewTdb = WriteTypeTable(set.NewTypes);

            foreach (ScrubTypeResult type in report.Types)
            {
                foreach (int typeID in type.EntriesFor(set.Kind))
                {
                    int count;
                    removedByTypeID.TryGetValue(typeID, out count);
                    type.AddRemoved(set.Kind, count);
                }
            }

            ScrubSaveSetResult result = report.SaveSets.First(s => s.Kind == set.Kind);
            result.RecordsAfter = set.NewRecords.Count;
            result.EntriesReplaced = set.Retired.Count;
        }

        #endregion Match and Plan

        #region Commit

        private static void Commit(SaveSet[] sets, List<SaveSet> changed, ScrubReport report)
        {
            List<PendingFile> pending = new List<PendingFile>();

            // .idx files first, then .tdb files: every intermediate state stays loadable (see header).
            foreach (SaveSet set in changed)
                pending.Add(new PendingFile(set.IdxPath, set.NewIdx));

            foreach (SaveSet set in changed)
                pending.Add(new PendingFile(set.TdbPath, set.NewTdb));

            string? backupDirectory = null;
            int replaced = 0;

            try
            {
                // 1. Temporary files on the same volume, flushed to disk, re-read and validated.
                foreach (PendingFile file in pending)
                {
                    DeleteIfExists(file.TempPath); // stale temp from an interrupted run; never an original
                    WriteDurable(file.TempPath, file.NewBytes);
                    file.TempWritten = true;

                    if (!BytesEqual(File.ReadAllBytes(file.TempPath), file.NewBytes))
                        throw new IOException(string.Format("'{0}' did not read back as written.", file.TempPath));
                }

                foreach (SaveSet set in changed)
                    ValidateTemps(set, pending);

                // 2. Recoverable copies of every original index and type table, verified.
                backupDirectory = CreateBackupDirectory(report.SavesDirectory);
                report.BackupDirectory = backupDirectory;

                foreach (SaveSet set in sets)
                {
                    if (!set.Present)
                        continue;

                    WriteBackup(backupDirectory, set, set.IdxPath, set.IdxRaw);
                    WriteBackup(backupDirectory, set, set.TdbPath, set.TdbRaw);
                }

                // 3. The originals must still be exactly what we validated.
                foreach (SaveSet set in sets)
                {
                    if (!set.Present)
                        continue;

                    if (!BytesEqual(File.ReadAllBytes(set.IdxPath), set.IdxRaw) || !BytesEqual(File.ReadAllBytes(set.TdbPath), set.TdbRaw))
                        throw new IOException(string.Format("{0} save files changed while the scrub was in progress.", set.Kind));
                }
            }
            catch (Exception e)
            {
                CleanupTemps(pending, report);

                if (backupDirectory != null)
                    TryDeleteDirectory(backupDirectory, report);

                report.BackupDirectory = null;

                throw new WorldScrubException("The scrub was abandoned before any save file was replaced; the original save is unchanged.", report, e);
            }

            // 4. Replace, .idx first. Each File.Move with overwrite is a single rename (MoveFileEx with
            //    MOVEFILE_REPLACE_EXISTING on Windows), so the destination is never absent.
            try
            {
                foreach (PendingFile file in pending)
                {
                    File.Move(file.TempPath, file.Path, true);
                    file.TempWritten = false;
                    replaced++;
                    report.AddReplacedFile(file.Path);

                    if (!BytesEqual(File.ReadAllBytes(file.Path), file.NewBytes))
                        throw new IOException(string.Format("'{0}' did not read back as written after replacement.", file.Path));
                }
            }
            catch (Exception e)
            {
                CleanupTemps(pending, report);

                WorldScrubException failure = new WorldScrubException(string.Format(
                    "The scrub was interrupted after replacing {0} of {1} files. Every intermediate state remains loadable, and running the scrub again completes it. The original files are in '{2}'.",
                    replaced, pending.Count, backupDirectory), report, e);

                report.AddWarning(failure.Message + " Cause: " + e.Message);
                WriteReportFile(backupDirectory, report);

                throw failure;
            }

            WriteReportFile(backupDirectory, report);
        }

        private static void ValidateTemps(SaveSet set, List<PendingFile> pending)
        {
            PendingFile idx = pending.First(p => p.Path == set.IdxPath);
            PendingFile tdb = pending.First(p => p.Path == set.TdbPath);

            List<TypeEntry> types = ParseTypeTable(File.ReadAllBytes(tdb.TempPath));
            List<IndexRecord> records = ParseIndex(File.ReadAllBytes(idx.TempPath));

            ValidateRecords(records, types.Count, set.BinExists, set.BinLength);

            // The type table keeps its size; only retired entries change, and they become the placeholder.
            if (types.Count != set.Types.Count)
                throw new InvalidDataException(string.Format("{0}: the new type table changed size.", set.Kind));

            for (int i = 0; i < types.Count; i++)
            {
                string expected = set.Retired.Contains(i) ? set.Placeholder : set.Types[i].Name;

                if (types[i].Name != expected || (!set.Retired.Contains(i) && !BytesEqual(types[i].Raw, set.Types[i].Raw)))
                    throw new InvalidDataException(string.Format("{0}: new type table entry {1} is not as intended.", set.Kind, i));
            }

            // The index keeps exactly the surviving records, unchanged and in their original order.
            int next = 0;

            foreach (IndexRecord r in set.Records)
            {
                if (set.Retired.Contains(r.TypeID))
                    continue;

                if (next >= records.Count || !records[next].Equals(r))
                    throw new InvalidDataException(string.Format("{0}: new index record {1} is not the expected surviving record.", set.Kind, next));

                next++;
            }

            if (next != records.Count)
                throw new InvalidDataException(string.Format("{0}: the new index has unexpected extra records.", set.Kind));

            foreach (IndexRecord r in records)
            {
                if (set.Retired.Contains(r.TypeID))
                    throw new InvalidDataException(string.Format("{0}: the new index still refers to a retired type.", set.Kind));
            }
        }

        private static string CreateBackupDirectory(string savesDirectory)
        {
            string root = Path.Combine(savesDirectory, BackupFolderName);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
            string path = Path.Combine(root, stamp);

            for (int i = 1; Directory.Exists(path); i++)
                path = Path.Combine(root, string.Format("{0}-{1}", stamp, i));

            Directory.CreateDirectory(path);

            return path;
        }

        private static void WriteBackup(string backupDirectory, SaveSet set, string path, byte[] bytes)
        {
            string directory = Path.Combine(backupDirectory, set.Folder);
            string target = Path.Combine(directory, Path.GetFileName(path));

            Directory.CreateDirectory(directory);
            WriteDurable(target, bytes);

            if (!BytesEqual(File.ReadAllBytes(target), bytes))
                throw new IOException(string.Format("Backup '{0}' did not read back as written.", target));
        }

        private static void WriteReportFile(string backupDirectory, ScrubReport report)
        {
            try
            {
                File.WriteAllText(Path.Combine(backupDirectory, ReportFileName), report.ToString());
            }
            catch (Exception e)
            {
                report.AddWarning(string.Format("The report file could not be written: {0}", e.Message));
            }
        }

        private static void WriteDurable(string path, byte[] bytes)
        {
            using (FileStream fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(true);
            }
        }

        private static void CleanupTemps(List<PendingFile> pending, ScrubReport report)
        {
            foreach (PendingFile file in pending)
            {
                if (!file.TempWritten)
                    continue;

                try
                {
                    DeleteIfExists(file.TempPath);
                    file.TempWritten = false;
                }
                catch (Exception e)
                {
                    report.AddWarning(string.Format("Temporary file '{0}' could not be removed: {1}", file.TempPath, e.Message));
                }
            }
        }

        private static void TryDeleteDirectory(string path, ScrubReport report)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch (Exception e)
            {
                report.AddWarning(string.Format("Unused backup folder '{0}' could not be removed: {1}", path, e.Message));
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        #endregion Commit

        #region Format

        // <Kind>.tdb: Int32 count, then count strings as written by BinaryFileWriter.Write(string) with
        // prefixStr == false and read by BinaryReader.ReadString: a 7-bit encoded byte length, then UTF-8.
        private static List<TypeEntry> ParseTypeTable(byte[] raw)
        {
            if (raw.Length < 4)
                throw new InvalidDataException(string.Format("the type table is {0} bytes, too short for its entry count", raw.Length));

            int count = BinaryPrimitives.ReadInt32LittleEndian(raw);

            if (count < 0)
                throw new InvalidDataException(string.Format("the type table declares {0} entries", count));

            List<TypeEntry> list = new List<TypeEntry>(Math.Min(count, 0x10000));
            int pos = 4;

            for (int i = 0; i < count; i++)
            {
                int start = pos;
                int length = Read7BitEncodedInt(raw, ref pos, i);

                if (length > raw.Length - pos)
                    throw new InvalidDataException(string.Format("type table entry {0} is truncated", i));

                string name;

                try
                {
                    name = m_StrictUTF8.GetString(raw, pos, length);
                }
                catch (DecoderFallbackException)
                {
                    throw new InvalidDataException(string.Format("type table entry {0} is not valid UTF-8", i));
                }

                pos += length;

                byte[] entry = new byte[pos - start];
                Buffer.BlockCopy(raw, start, entry, 0, entry.Length);

                list.Add(new TypeEntry(name, entry));
            }

            if (pos != raw.Length)
                throw new InvalidDataException(string.Format("the type table has {0} unexpected trailing bytes", raw.Length - pos));

            // Cross-check against the reader World.Load itself uses.
            using (BinaryReader reader = new BinaryReader(new MemoryStream(raw, false)))
            {
                if (reader.ReadInt32() != count)
                    throw new InvalidDataException("the type table count disagrees with BinaryReader");

                for (int i = 0; i < count; i++)
                {
                    if (reader.ReadString() != list[i].Name)
                        throw new InvalidDataException(string.Format("type table entry {0} disagrees with BinaryReader", i));
                }

                if (reader.BaseStream.Position != raw.Length)
                    throw new InvalidDataException("the type table length disagrees with BinaryReader");
            }

            return list;
        }

        // Identical to BinaryReader.Read7BitEncodedInt, including its rejection of over-long encodings.
        private static int Read7BitEncodedInt(byte[] raw, ref int pos, int entry)
        {
            uint result = 0;

            for (int shift = 0; shift < 28; shift += 7)
            {
                if (pos >= raw.Length)
                    throw new InvalidDataException(string.Format("type table entry {0} is truncated", entry));

                byte b = raw[pos++];
                result |= (uint)(b & 0x7F) << shift;

                if (b <= 0x7F)
                    return CheckLength(result, entry);
            }

            if (pos >= raw.Length)
                throw new InvalidDataException(string.Format("type table entry {0} is truncated", entry));

            byte last = raw[pos++];

            if (last > 0x0F)
                throw new InvalidDataException(string.Format("type table entry {0} has a malformed length", entry));

            result |= (uint)last << 28;

            return CheckLength(result, entry);
        }

        private static int CheckLength(uint length, int entry)
        {
            if (length > int.MaxValue)
                throw new InvalidDataException(string.Format("type table entry {0} has a negative length", entry));

            return (int)length;
        }

        private static byte[] EncodeTypeName(string name)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms, m_StrictUTF8, true))
                    writer.Write(name);

                return ms.ToArray();
            }
        }

        private static byte[] WriteTypeTable(List<TypeEntry> types)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] count = new byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(count, types.Count);
                ms.Write(count, 0, 4);

                foreach (TypeEntry entry in types)
                    ms.Write(entry.Raw, 0, entry.Raw.Length);

                return ms.ToArray();
            }
        }

        // <Kind>.idx: Int32 count, then count records of Int32 typeID, Int32 serial, Int64 position, Int32 length.
        private static List<IndexRecord> ParseIndex(byte[] raw)
        {
            if (raw.Length < 4)
                throw new InvalidDataException(string.Format("the index is {0} bytes, too short for its record count", raw.Length));

            int count = BinaryPrimitives.ReadInt32LittleEndian(raw);

            if (count < 0)
                throw new InvalidDataException(string.Format("the index declares {0} records", count));

            long expected = 4L + (long)IndexRecordSize * count;

            if (raw.Length != expected)
                throw new InvalidDataException(string.Format("the index is {0} bytes, but {1} records require exactly {2}", raw.Length, count, expected));

            List<IndexRecord> list = new List<IndexRecord>(count);

            for (int i = 0; i < count; i++)
            {
                ReadOnlySpan<byte> r = new ReadOnlySpan<byte>(raw, 4 + i * IndexRecordSize, IndexRecordSize);

                list.Add(new IndexRecord(
                    BinaryPrimitives.ReadInt32LittleEndian(r),
                    BinaryPrimitives.ReadInt32LittleEndian(r.Slice(4)),
                    BinaryPrimitives.ReadInt64LittleEndian(r.Slice(8)),
                    BinaryPrimitives.ReadInt32LittleEndian(r.Slice(16))));
            }

            return list;
        }

        private static byte[] WriteIndex(List<IndexRecord> records)
        {
            byte[] raw = new byte[4 + IndexRecordSize * records.Count];

            BinaryPrimitives.WriteInt32LittleEndian(raw, records.Count);

            for (int i = 0; i < records.Count; i++)
            {
                Span<byte> r = new Span<byte>(raw, 4 + i * IndexRecordSize, IndexRecordSize);
                IndexRecord record = records[i];

                BinaryPrimitives.WriteInt32LittleEndian(r, record.TypeID);
                BinaryPrimitives.WriteInt32LittleEndian(r.Slice(4), record.Serial);
                BinaryPrimitives.WriteInt64LittleEndian(r.Slice(8), record.Position);
                BinaryPrimitives.WriteInt32LittleEndian(r.Slice(16), record.Length);
            }

            return raw;
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            return a.AsSpan().SequenceEqual(b);
        }

        private static bool PathsEqual(string a, string b)
        {
            return string.Equals(Path.TrimEndingDirectorySeparator(a), Path.TrimEndingDirectorySeparator(b), StringComparison.OrdinalIgnoreCase);
        }

        #endregion Format

        #region Internal Types

        private readonly struct TypeEntry
        {
            public readonly string Name;
            public readonly byte[] Raw; // the entry's exact bytes: length prefix and name

            public TypeEntry(string name, byte[] raw)
            {
                Name = name;
                Raw = raw;
            }
        }

        private readonly struct IndexRecord : IEquatable<IndexRecord>
        {
            public readonly int TypeID;
            public readonly int Serial;
            public readonly long Position;
            public readonly int Length;

            public IndexRecord(int typeID, int serial, long position, int length)
            {
                TypeID = typeID;
                Serial = serial;
                Position = position;
                Length = length;
            }

            public bool Equals(IndexRecord other)
            {
                return TypeID == other.TypeID && Serial == other.Serial && Position == other.Position && Length == other.Length;
            }
        }

        private sealed class SaveSet
        {
            public readonly ScrubSaveKind Kind;
            public readonly string Folder;
            public readonly string Directory;
            public readonly string IdxPath, TdbPath, BinPath;
            public readonly string Placeholder;
            public readonly HashSet<int> Retired = new HashSet<int>();

            public bool Present;
            public byte[] IdxRaw, TdbRaw;
            public bool BinExists;
            public long BinLength;
            public List<TypeEntry> Types;
            public List<IndexRecord> Records;

            public List<TypeEntry> NewTypes;
            public List<IndexRecord> NewRecords;
            public byte[] NewIdx, NewTdb;

            public bool Changed { get { return Present && Retired.Count > 0; } }

            public SaveSet(ScrubSaveKind kind, string savesDirectory, string folder, string placeholder)
            {
                Kind = kind;
                Folder = folder;
                Directory = Path.Combine(savesDirectory, folder);
                IdxPath = Path.Combine(Directory, folder + ".idx");
                TdbPath = Path.Combine(Directory, folder + ".tdb");
                BinPath = Path.Combine(Directory, folder + ".bin");
                Placeholder = placeholder;
            }
        }

        private sealed class PendingFile
        {
            public readonly string Path;
            public readonly string TempPath;
            public readonly byte[] NewBytes;
            public bool TempWritten;

            public PendingFile(string path, byte[] newBytes)
            {
                Path = path;
                TempPath = path + TempSuffix;
                NewBytes = newBytes;
            }
        }

        #endregion Internal Types
    }

    public enum ScrubOutcome
    {
        NoChange,       // nothing requested is in the save; no file was written
        WouldScrub,     // Preview only: Scrub would change the save
        Scrubbed,       // the save was transformed
        Failed,         // a WorldScrubException was thrown; see its message and the report
    }

    public sealed class ScrubReport
    {
        private readonly List<ScrubTypeResult> m_Types = new List<ScrubTypeResult>();
        private readonly List<ScrubSaveSetResult> m_SaveSets = new List<ScrubSaveSetResult>();
        private readonly List<string> m_ReplacedFiles = new List<string>();
        private readonly List<string> m_Warnings = new List<string>();

        public string SavesDirectory { get; }
        public bool WriteRequested { get; }
        public ScrubOutcome Outcome { get; internal set; }
        public string? BackupDirectory { get; internal set; }

        public IReadOnlyList<ScrubTypeResult> Types { get { return m_Types; } }
        public IReadOnlyList<ScrubSaveSetResult> SaveSets { get { return m_SaveSets; } }
        public IReadOnlyList<string> ReplacedFiles { get { return m_ReplacedFiles; } }
        public IReadOnlyList<string> Warnings { get { return m_Warnings; } }

        internal ScrubReport(string savesDirectory, bool writeRequested)
        {
            SavesDirectory = savesDirectory;
            WriteRequested = writeRequested;
        }

        internal void AddType(ScrubTypeResult type) { m_Types.Add(type); }
        internal void AddSaveSet(ScrubSaveSetResult set) { m_SaveSets.Add(set); }
        internal void AddReplacedFile(string path) { m_ReplacedFiles.Add(path); }
        internal void AddWarning(string warning) { m_Warnings.Add(warning); }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("World scrub of '{0}': {1}", SavesDirectory, Outcome));

            foreach (ScrubSaveSetResult set in m_SaveSets)
                sb.AppendLine(string.Format("  {0}", set));

            foreach (ScrubTypeResult type in m_Types)
                sb.AppendLine(string.Format("  {0}", type));

            if (BackupDirectory != null)
                sb.AppendLine(string.Format("  Original index and type tables saved to '{0}'", BackupDirectory));

            foreach (string path in m_ReplacedFiles)
                sb.AppendLine(string.Format("  Replaced '{0}'", path));

            foreach (string warning in m_Warnings)
                sb.AppendLine(string.Format("  Warning: {0}", warning));

            return sb.ToString();
        }
    }

    public sealed class ScrubSaveSetResult
    {
        public ScrubSaveKind Kind { get; }
        public int TypeTableEntries { get; }
        public int RecordsBefore { get; }
        public int RecordsAfter { get; internal set; }
        public int EntriesReplaced { get; internal set; }
        public long BinLength { get; }

        internal ScrubSaveSetResult(ScrubSaveKind kind, int typeTableEntries, int records, long binLength)
        {
            Kind = kind;
            TypeTableEntries = typeTableEntries;
            RecordsBefore = records;
            RecordsAfter = records;
            BinLength = binLength;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1} type table entries ({2} replaced by placeholder), {3} index records -> {4}, .bin {5:N0} bytes (unchanged)",
                Kind, TypeTableEntries, EntriesReplaced, RecordsBefore, RecordsAfter, BinLength);
        }
    }

    public sealed class ScrubTypeResult
    {
        private readonly List<int> m_ItemEntries = new List<int>();
        private readonly List<int> m_MobileEntries = new List<int>();
        private readonly List<string> m_CaseOnlyNearMisses = new List<string>();

        public string TypeName { get; }
        public bool Present { get { return m_ItemEntries.Count > 0 || m_MobileEntries.Count > 0; } }
        public IReadOnlyList<int> ItemTypeTableEntries { get { return m_ItemEntries; } }
        public IReadOnlyList<int> MobileTypeTableEntries { get { return m_MobileEntries; } }
        public int ItemRecordsRemoved { get; private set; }
        public int MobileRecordsRemoved { get; private set; }
        public IReadOnlyList<string> CaseOnlyNearMisses { get { return m_CaseOnlyNearMisses; } }

        internal ScrubTypeResult(string typeName)
        {
            TypeName = typeName;
        }

        internal IReadOnlyList<int> EntriesFor(ScrubSaveKind kind)
        {
            return kind == ScrubSaveKind.Items ? m_ItemEntries : m_MobileEntries;
        }

        internal void AddEntry(ScrubSaveKind kind, int typeID)
        {
            (kind == ScrubSaveKind.Items ? m_ItemEntries : m_MobileEntries).Add(typeID);
        }

        internal void AddRemoved(ScrubSaveKind kind, int count)
        {
            if (kind == ScrubSaveKind.Items)
                ItemRecordsRemoved += count;
            else
                MobileRecordsRemoved += count;
        }

        internal void AddCaseOnlyNearMiss(string description)
        {
            m_CaseOnlyNearMisses.Add(description);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (!Present)
                sb.Append(string.Format("'{0}': not present", TypeName));
            else
                sb.Append(string.Format("'{0}': Items type table entries [{1}], {2} records removed; Mobiles type table entries [{3}], {4} records removed",
                    TypeName, string.Join(", ", m_ItemEntries), ItemRecordsRemoved, string.Join(", ", m_MobileEntries), MobileRecordsRemoved));

            foreach (string nearMiss in m_CaseOnlyNearMisses)
                sb.Append(string.Format("; differs only by case from {0} (not scrubbed)", nearMiss));

            return sb.ToString();
        }
    }

    public class WorldScrubException : Exception
    {
        public ScrubReport Report { get; }

        public WorldScrubException(string message, ScrubReport report)
            : base(message)
        {
            Report = report;
            Report.Outcome = ScrubOutcome.Failed;
        }

        public WorldScrubException(string message, ScrubReport report, Exception inner)
            : base(message, inner)
        {
            Report = report;
            Report.Outcome = ScrubOutcome.Failed;
        }
    }
}
