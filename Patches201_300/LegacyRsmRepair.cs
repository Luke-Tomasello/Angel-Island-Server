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

/* Patches201_300/LegacyRsmRepair.cs
 * ChangeLog
 *  10/30/26, Adam (musicbox cleanup: tidy)
 *      Reflection names follow the music box: its per-player dictionary is Profiles (was MusicConfig) of
 *      MusicBoxProfile (was Server.Items.MusicContext). Nothing else changes.
 *  10/29/26, Adam (MusicLibrary)
 *      The music library moved out of MusicBox: MusicLibrary.Repository and MusicLibrary.MusicInfo; the review queue is
 *      MusicLibrary.ReviewQueue (it was read by reflection on MusicBox, which would have found nothing once it moved).
 *  10/25/26, Adam (moved into the patch assembly)
 *      Moved from Scripts/Engines/AIMusicSystem/Tools (namespace Server.Engines) to Patches201_300 (namespace
 *      Patches201_300): a one-time migration helper of PatchV255 belongs with its patch, not in the music system.
 *      The [RepairLegacyRSM command is removed: patch assemblies are loaded only to run patches and are never
 *      initialized. Its verify check is VerifyWorld (used by the test harness). Run, Verify, CheckProtected,
 *      SaveProtection and LoadExpected, the safeguards, logs and files are unchanged.
 *  10/25/26, Adam (protection snapshot)
 *      Every run records the protected set: each sheet to be repaired and, for each library song one of them belongs
 *      to, the library entry (owner, GUID, price, status, flags, purchasers), its master and every sheet with that
 *      composition or title (serial, title, author, price, owner, GUID, hash, instrument, composition length and
 *      SHA-256). A world apply writes it to LegacyRsmRepair.protected (BEFORE: the state found, EXPECT: the state after
 *      the repair) before anything is changed; if it cannot be written nothing is changed (Failed). After applying,
 *      the EXPECT lines and the repository (entry count, the same keys and MusicInfo objects) are checked again:
 *      a violation sets ProtectionFailed and is logged. Nothing is rolled back (the pre-migration world backup is the
 *      rollback). A world dry run writes the same snapshot to LegacyRsmRepair.protected.preview. "verify" also checks
 *      the EXPECT lines of LegacyRsmRepair.protected, e.g. after a restart.
 *  10/24/26, Adam
 *      Created. One-time repair of legacy RolledUpSheetMusic with invalid persisted state (PatchV255):
 *      - Instrument not the RSM itself: set Instrument = the RSM, the invariant the constructors and every playback
 *        path (RSM.OnDoubleClick, MusicBox, CalypsoMusicPlayer.Play) already establish. Dupes copy Instrument, so a
 *        dupe points at its source, and at null once the source is deleted; CalypsoMusicPlayer.GetMetaData returns
 *        null for those and the source's instrument type for the others. An RSM that is playing is skipped.
 *      - HashCode 0 with a composition (never hashed): set HashCode to the song hash ManuscriptPaper stores when it
 *        compiles a book (CompileHashCode, algorithm unchanged), only when that creates no unintended identity:
 *        not a GlobalMusicRepository key (rehashing a key corrupts the dictionary), not referenced by a music box
 *        list, the parse and no-parse hashes agree, the composition has notes (an empty composition hashes to a
 *        value other songs can share), every RSM and library entry with that hash has the same composition text,
 *        and a matching library entry's owner identity agrees with the RSM's. Anything else is reported as
 *        ambiguous and left unchanged. A RSM without a composition keeps HashCode 0 by design (as Nuke's
 *        PatchRolledUpSheetMusic does). A nonzero hash that disagrees with recomputation is reported, never changed.
 *      Nothing else is modified: no deletions, no repository or ownership changes, no serialization changes. If the
 *      music box lists cannot be read nothing is modified. Rerun-safe: a second run finds nothing to repair.
 *      With an equal hash a repaired copy compares equal to its library song (RSM equality is by HashCode), as copies
 *      that were hashed already do. [RepairLegacyRSM (Owner): dry run by default, "apply" to repair, "verify" after a
 *      save and restart. Every run is written to LegacyRsmRepair.log (each RSM with a defect, its reasons, and the
 *      serials eligible, skipped, ambiguous and ok); world repairs to LegacyRsmRepair.applied with previous values.
 */

using Server.Diagnostics;
using Server.Items;
using Server.Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Server;
using Server.Engines;

namespace Patches201_300
{
    public static class LegacyRsmRepair
    {
        public sealed class Result
        {
            public bool Applied;
            public bool Failed;                         // the plan could not be completed (exception, music box scan): nothing was changed
            public bool ReferenceScanFailed;            // music box lists could not be read

            public int Examined;
            public int InstrumentNull, InstrumentOther;
            public int HashOk, HashNoComposition, HashZero, HashMismatch;
            public int EligibleInstrument, EligibleHash;
            public int RepairedInstrument, RepairedHash;
            public int Ok;                              // nothing to repair
            public int Skipped;                         // a defect left unchanged (nonzero hash mismatch, RSM playing)
            public int Ambiguous;                       // hash 0, but normalization could create or break an identity

            public readonly List<string> Lines = new();
            public readonly List<string> AppliedRecords = new();   // serial <TAB> field <TAB> value <TAB> previous value
            public readonly List<string> ProtectedBefore = new();  // the protected set as found
            public readonly List<string> Protected = new();        // the protected set as it must be after the repair (CheckProtected)
            public bool ProtectionFailed;                          // after applying, a protected expectation or the repository did not hold

            public int Repaired => RepairedInstrument + RepairedHash;

            public string Summary => string.Format(
                "Legacy RSM repair {0}: {1} examined; Instrument {2} null, {3} other; hash {4} ok, {5} without composition (0 by design), " +
                "{6} zero, {7} nonzero mismatch; eligible {8} instrument, {9} hash; repaired {10} instrument, {11} hash; {12} ok, {13} skipped, {14} ambiguous; " +
                "{15} protected{16}{17}.",
                Failed ? "FAILED (nothing changed)" : Applied ? "applied" : "dry run", Examined, InstrumentNull, InstrumentOther, HashOk, HashNoComposition,
                HashZero, HashMismatch, EligibleInstrument, EligibleHash, RepairedInstrument, RepairedHash, Ok, Skipped, Ambiguous,
                Protected.Count, ProtectionFailed ? " (PROTECTION VIOLATED)" : "",
                ReferenceScanFailed ? "; music box reference scan FAILED" : "");
        }

        private const string LogFile = "LegacyRsmRepair.log";
        private const string AppliedFile = "LegacyRsmRepair.applied";
        private const string ProtectedFile = "LegacyRsmRepair.protected";

        /// <summary>
        /// The post-migration check (formerly "[RepairLegacyRSM verify"): every applied repair in LegacyRsmRepair.applied
        /// still holds, every EXPECT line of LegacyRsmRepair.protected holds, and a dry run finds nothing still eligible.
        /// Returns whether it passed; summary and problems describe the result. Logged to LegacyRsmRepair.log.
        /// </summary>
        public static bool VerifyWorld(string caller, out string summary, out List<string> problems)
        {
            List<string> records = ReadApplied();
            List<string> failures = Verify(records);
            List<string> expected = LoadExpected(Path.Combine(Core.LogsDirectory, ProtectedFile));
            List<string> violations = CheckProtected(expected);
            Result check = Run(apply: false, caller);
            bool ok = failures.Count == 0 && violations.Count == 0 && check.EligibleInstrument == 0 && check.EligibleHash == 0 && !check.ReferenceScanFailed;
            problems = failures.Concat(violations).ToList();
            summary = string.Format("Verify {0}: {1} applied repairs checked, {2} failed; {3} protected checked, {4} violated; still eligible {5} instrument, {6} hash.",
                ok ? "PASSED" : "FAILED", records.Count, failures.Count, expected.Count, violations.Count, check.EligibleInstrument, check.EligibleHash);
            AppendLog(string.Format("{0:u} {1}: {2} applied repairs checked, {3} failed; {4} protected checked, {5} violated{6}", DateTime.UtcNow, caller,
                records.Count, failures.Count, expected.Count, violations.Count,
                string.Concat(problems.Select(f => Environment.NewLine + "    " + f))));
            return ok;
        }

        /// <summary>
        /// Examines every RolledUpSheetMusic in the world (or only those in scope) and, with apply == true, repairs the eligible
        /// ones. The identity checks always look at the whole world and the whole repository. With apply == false nothing
        /// is modified; if the plan cannot be completed nothing is modified either (Result.Failed). Writes the run to
        /// LegacyRsmRepair.log, and the repairs of a world (unscoped) apply to LegacyRsmRepair.applied for Verify.
        /// Used by PatchV255.
        /// </summary>
        public static Result Run(bool apply, string caller, IEnumerable<RolledUpSheetMusic>? scope = null)
        {
            Result result = new Result { Applied = apply };
            try
            {
                ManuscriptPaper.LocationTarget compiler = new ManuscriptPaper.LocationTarget(null!);   // the hash needs no paper (no item is created)
                int emptyHash = compiler.CompileHashCode(string.Empty, false);

                List<RolledUpSheetMusic> world = World.Items.Values.OfType<RolledUpSheetMusic>().Where(r => !r.Deleted).ToList();
                Dictionary<int, List<RolledUpSheetMusic>> byStoredHash = world.Where(r => r.HashCode != 0).GroupBy(r => r.HashCode).ToDictionary(g => g.Key, g => g.ToList());

                // library keys by reference (equality is by HashCode), and entries by song hash
                HashSet<RolledUpSheetMusic> keys = new(ReferenceEqualityComparer.Instance);
                Dictionary<int, List<KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo>>> libraryByHash = new();
                foreach (KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo> kvp in MusicLibrary.Repository)
                {
                    if (kvp.Key == null)
                        continue;
                    keys.Add(kvp.Key);
                    if (!libraryByHash.TryGetValue(kvp.Key.HashCode, out var list))
                        libraryByHash[kvp.Key.HashCode] = list = new();
                    list.Add(kvp);
                }

                Dictionary<Item, List<string>>? referencedBy = MusicBoxReferences();
                result.ReferenceScanFailed = referencedBy == null;
                // a stored hash of 0 is no identity: GetHashCode then falls back to the object's runtime hash
                result.Lines.Add(string.Format("World: {0} RSMs, {1} with HashCode 0; repository: {2} entries, {3} keys with HashCode 0; {4} RSMs referenced by music box lists{5}",
                    world.Count, world.Count(r => r.HashCode == 0), keys.Count, keys.Count(k => k.HashCode == 0), referencedBy?.Count ?? 0,
                    referencedBy == null ? " (scan FAILED)" : ""));

                List<(RolledUpSheetMusic rsm, bool instrument, int hash)> plan = new();
                List<string> eligible = new(), skipped = new(), ambiguousList = new(), ok = new();
                IEnumerable<RolledUpSheetMusic> examine = scope == null ? world : scope.Where(r => r != null && !r.Deleted).Distinct(ReferenceEqualityComparer.Instance).Cast<RolledUpSheetMusic>();
                foreach (RolledUpSheetMusic rsm in examine.OrderBy(r => r.Serial.Value))
                {
                    result.Examined++;
                    List<string> notes = new();
                    bool repairInstrument = false, defectLeft = false;
                    int newHash = 0;

                    #region Instrument
                    // An RSM plays itself: the constructors and every playback path (OnDoubleClick, MusicBox, CalypsoMusicPlayer)
                    //  set Instrument = this, and nothing assigns it another item. Dupes copy Instrument and so point at their
                    //  source, which loads as null once the source is deleted. Only GetMetaData reads it without resetting it.
                    if (!ReferenceEquals(rsm.Instrument, rsm))
                    {
                        string was = rsm.Instrument == null ? "null" : "another item (" + ItemText(rsm.Instrument) + ")";
                        if (rsm.Instrument == null) result.InstrumentNull++; else result.InstrumentOther++;
                        if (rsm.IsInstancePlaying)
                        {
                            defectLeft = true;
                            notes.Add(string.Format("Instrument {0} -> self: SKIPPED, the RSM is playing (rerun later)", was));
                        }
                        else
                        {
                            result.EligibleInstrument++;
                            repairInstrument = true;
                            notes.Add(string.Format("Instrument {0} -> self: REPAIR", was));
                        }
                    }
                    #endregion Instrument

                    #region Song hash
                    int stored = rsm.HashCode;
                    string? composition = rsm.Composition;
                    bool ambiguous = false;
                    if (composition == null)
                    {
                        if (stored == 0)
                        {
                            result.HashNoComposition++;
                            notes.Add("no composition: HashCode 0 by design, not changed");
                        }
                        else
                        {
                            result.HashMismatch++;
                            defectLeft = true;
                            notes.Add(string.Format("hash {0} without a composition: SKIPPED, a nonzero hash is never rewritten", stored));
                        }
                    }
                    else
                    {
                        int parsed = compiler.CompileHashCode(composition, true);
                        int unparsed = compiler.CompileHashCode(composition, false);
                        if (stored != 0 && (stored == parsed || stored == unparsed))
                            result.HashOk++;
                        else if (stored != 0)
                        {
                            result.HashMismatch++;
                            defectLeft = true;
                            notes.Add(string.Format("hash {0} disagrees with recomputation ({1} parse, {2} no parse): SKIPPED, a nonzero hash is never rewritten",
                                stored, parsed, unparsed));
                        }
                        else
                        {
                            result.HashZero++;
                            List<string> why = new();
                            if (parsed != unparsed)
                                why.Add(string.Format("parse and no-parse hashes disagree ({0}, {1})", parsed, unparsed));
                            if (unparsed == emptyHash || string.IsNullOrWhiteSpace(composition))
                                why.Add("the composition has no notes (its hash is the empty-composition hash other songs can share)");
                            if (keys.Contains(rsm))
                                why.Add("it is a GlobalMusicRepository key (rehashing would corrupt the repository)");
                            if (referencedBy == null)
                                why.Add("music box lists could not be read");
                            else if (referencedBy.TryGetValue(rsm, out List<string>? refs))
                                why.Add("referenced by " + string.Join(", ", refs));

                            // every RSM already carrying this hash must hold the same composition text
                            List<RolledUpSheetMusic> sharing = byStoredHash.TryGetValue(unparsed, out var s) ? s.Where(r => !ReferenceEquals(r, rsm)).ToList() : new();
                            List<RolledUpSheetMusic> different = sharing.Where(r => r.Composition != composition).ToList();
                            if (different.Count > 0)
                                why.Add(string.Format("hash {0} is held by {1} RSM(s) with a different composition ({2})", unparsed, different.Count, string.Join(" ", different.Take(5).Select(r => r.Serial.ToString()))));

                            // a library entry with this hash must be this composition, and its owner identity must agree
                            string library = "no library entry";
                            if (libraryByHash.TryGetValue(unparsed, out var entries))
                            {
                                library = string.Join("; ", entries.Select(kv => string.Format("library {0} '{1}' GUID {2}", kv.Key.Serial, kv.Key.Title, kv.Value.OwnerGUID)));
                                foreach (var kv in entries)
                                {
                                    if (kv.Key.Composition != composition)
                                        why.Add(string.Format("library entry {0} '{1}' has this hash but a different composition", kv.Key.Serial, kv.Key.Title));
                                    uint libraryGuid = kv.Value.OwnerGUID;
                                    if (libraryGuid != 0 && rsm.OwnerGUID != libraryGuid)
                                        why.Add(string.Format("it would match library entry {0} '{1}' of owner GUID {2}, but carries GUID {3} (ownership is not migrated)",
                                            kv.Key.Serial, kv.Key.Title, libraryGuid, rsm.OwnerGUID));
                                }
                            }

                            string context = string.Format("hash 0 -> {0} (parse {1}); {2} other RSM(s) store it{3}; {4}", unparsed, parsed, sharing.Count,
                                sharing.Count == 0 ? "" : " (" + string.Join(" ", sharing.Take(5).Select(r => r.Serial.ToString())) + ")", library);
                            if (why.Count == 0)
                            {
                                result.EligibleHash++;
                                newHash = unparsed;
                                notes.Add(context + ": REPAIR");
                            }
                            else
                            {
                                result.Ambiguous++;
                                ambiguous = true;
                                notes.Add(context + ": AMBIGUOUS, not changed: " + string.Join("; ", why));
                            }
                        }
                    }
                    #endregion Song hash

                    // each RSM is eligible (something to repair), and/or ambiguous (hash), else skipped (defect left), else ok
                    if (repairInstrument || newHash != 0)
                    {
                        plan.Add((rsm, repairInstrument, newHash));
                        eligible.Add(rsm.Serial.ToString());
                    }
                    if (ambiguous)
                        ambiguousList.Add(rsm.Serial.ToString());
                    else if (defectLeft && !repairInstrument && newHash == 0)
                    {
                        result.Skipped++;
                        skipped.Add(rsm.Serial.ToString());
                    }
                    else if (!repairInstrument && newHash == 0)
                    {
                        result.Ok++;
                        ok.Add(rsm.Serial.ToString());
                    }

                    if (notes.Count > 0)
                    {
                        result.Lines.Add(Describe(rsm, referencedBy));
                        foreach (string n in notes)
                            result.Lines.Add("    " + n);
                    }
                }

                result.Lines.Add(string.Format("ELIGIBLE {0}: {1}", eligible.Count, string.Join(" ", eligible)));
                result.Lines.Add(string.Format("SKIPPED {0}: {1}", skipped.Count, string.Join(" ", skipped)));
                result.Lines.Add(string.Format("AMBIGUOUS {0}: {1}", ambiguousList.Count, string.Join(" ", ambiguousList)));
                result.Lines.Add(string.Format("OK {0}: {1}", ok.Count, string.Join(" ", ok)));

                #region Protection
                // every sheet to be repaired, and each library song it belongs to: the entry, its master, and every sheet
                //  with that composition or title. BEFORE is the state found, EXPECT the state the repair must leave.
                Dictionary<RolledUpSheetMusic, (bool instrument, int hash)> planned = new(ReferenceEqualityComparer.Instance);
                foreach (var (rsm, instrument, hash) in plan)
                    planned[rsm] = (instrument, hash);
                HashSet<RolledUpSheetMusic> guarded = new(ReferenceEqualityComparer.Instance);
                List<KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo>> guardedLibrary = new();
                foreach (var (rsm, _, hash) in plan)
                {
                    guarded.Add(rsm);
                    int song = hash != 0 ? hash : rsm.HashCode != 0 ? rsm.HashCode
                        : string.IsNullOrWhiteSpace(rsm.Composition) ? 0 : compiler.CompileHashCode(rsm.Composition, false);
                    if (song == 0 || !libraryByHash.TryGetValue(song, out var entries))
                        continue;
                    foreach (var kv in entries)
                    {
                        if (!guardedLibrary.Any(g => ReferenceEquals(g.Key, kv.Key)))
                            guardedLibrary.Add(kv);
                        guarded.Add(kv.Key);
                        foreach (RolledUpSheetMusic r in world)
                            if ((kv.Key.Composition != null && r.Composition == kv.Key.Composition) || (!string.IsNullOrEmpty(kv.Key.Title) && r.Title == kv.Key.Title))
                                guarded.Add(r);
                    }
                }
                foreach (RolledUpSheetMusic r in guarded.OrderBy(r => r.Serial.Value))
                {
                    result.ProtectedBefore.Add(RsmLine(r));
                    result.Protected.Add(planned.TryGetValue(r, out var p) ? RsmLine(r, p.instrument, p.hash) : RsmLine(r));
                }
                foreach (var kv in guardedLibrary.OrderBy(kv => kv.Key.Serial.Value))
                {
                    result.ProtectedBefore.Add(LibLine(kv.Key, kv.Value));
                    result.Protected.Add(LibLine(kv.Key, kv.Value));
                }
                result.Lines.Add(string.Format("PROTECTED {0}: {1} sheets, {2} library entries", result.Protected.Count, guarded.Count, guardedLibrary.Count));
                foreach (string line in result.ProtectedBefore)
                    result.Lines.Add("    BEFORE " + line);
                #endregion Protection

                // a hash repair is only as safe as the reference scan behind it: without it nothing is changed
                if (result.ReferenceScanFailed)
                {
                    result.Failed = true;
                    result.Applied = false;
                }
                else if (!apply)
                {
                    if (scope == null && result.Protected.Count > 0)
                        try { SaveProtection(Path.Combine(Core.LogsDirectory, ProtectedFile + ".preview"), result, caller + " (dry run preview)"); }
                        catch (Exception ex) { LogHelper.LogException(ex); result.Lines.Add("protection preview could not be written: " + ex.Message); }
                }
                else
                {
                    // the snapshot is persisted before anything is changed: without it nothing is changed
                    if (scope == null && result.Protected.Count > 0)
                        try { SaveProtection(Path.Combine(Core.LogsDirectory, ProtectedFile), result, caller); }
                        catch (Exception ex)
                        {
                            LogHelper.LogException(ex);
                            result.Lines.Add("PROTECTION SNAPSHOT could not be written, nothing changed: " + ex.Message);
                            result.Failed = true;
                            result.Applied = false;
                        }
                    if (!result.Failed)
                        ApplyAndCheck(result, plan);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                result.Lines.Add("EXCEPTION: " + ex.Message);
                result.Failed = true;
                result.Applied = result.Repaired > 0;           // the apply loop only sets fields; a failure there is reported with what was done
            }

            if (result.Applied && scope == null && result.AppliedRecords.Count > 0)
                WriteApplied(result.AppliedRecords, caller);

            StringBuilder sb = new();
            sb.AppendLine(string.Format("{0:u} {1} ({2}{3})", DateTime.UtcNow, caller, apply ? "apply" : "dry run", scope == null ? "" : ", scoped"));
            foreach (string line in result.Lines)
                sb.AppendLine("  " + line);
            foreach (string record in result.AppliedRecords)
                sb.AppendLine("  APPLIED " + record.Replace('\t', ' '));
            sb.AppendLine("  " + result.Summary);
            AppendLog(sb.ToString().TrimEnd());
            return result;
        }

        // applies the plan (revalidating each repair), then checks the protected expectations and the repository
        private static void ApplyAndCheck(Result result, List<(RolledUpSheetMusic rsm, bool instrument, int hash)> plan)
        {
            List<KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo>> repository = MusicLibrary.Repository.ToList();
            foreach (var (rsm, instrument, hash) in plan)
            {
                // revalidate: the state the plan was made from
                if (instrument && !ReferenceEquals(rsm.Instrument, rsm) && !rsm.IsInstancePlaying)
                {
                    string previous = rsm.Instrument == null ? "null" : string.Format("0x{0:X8}", rsm.Instrument.Serial.Value);
                    rsm.Instrument = rsm;
                    result.RepairedInstrument++;
                    result.AppliedRecords.Add(string.Format("0x{0:X8}\tInstrument\tself\t{1}", rsm.Serial.Value, previous));
                }
                if (hash != 0 && rsm.HashCode == 0)
                {
                    rsm.HashCode = hash;
                    result.RepairedHash++;
                    result.AppliedRecords.Add(string.Format("0x{0:X8}\tHashCode\t{1}\t0", rsm.Serial.Value, hash));
                }
            }

            // flagged, never rolled back: the pre-migration world backup is the rollback
            List<string> violations = CheckProtected(result.Protected);
            if (MusicLibrary.Repository.Count != repository.Count)
                violations.Add(string.Format("repository: {0} entries, expected {1}", MusicLibrary.Repository.Count, repository.Count));
            HashSet<RolledUpSheetMusic> keys = new(MusicLibrary.Repository.Keys, ReferenceEqualityComparer.Instance);
            foreach (var kv in repository)
                if (!keys.Contains(kv.Key) || !MusicLibrary.Repository.TryGetValue(kv.Key, out MusicLibrary.MusicInfo? mi) || !ReferenceEquals(mi, kv.Value))
                    violations.Add(string.Format("repository: entry {0} '{1}' is no longer found with its MusicInfo", kv.Key?.Serial, kv.Key?.Title));
            if (violations.Count > 0)
            {
                result.ProtectionFailed = true;
                foreach (string v in violations)
                    result.Lines.Add("PROTECTION VIOLATED: " + v);
            }
        }

        #region Protection
        /// <summary>
        /// Checks each protected expectation (RSM and LIB lines, see Result.Protected) against the world and the repository.
        /// Returns one failure per differing field or missing object.
        /// </summary>
        public static List<string> CheckProtected(IEnumerable<string> expected)
        {
            List<string> failures = new();
            Dictionary<int, KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo>> library = new();
            foreach (var kv in MusicLibrary.Repository)
                if (kv.Key != null)
                    library[kv.Key.Serial.Value] = kv;

            foreach (string line in expected)
            {
                string[] want = line.Split('|');
                string[] head = want[0].Split(' ');
                if (head.Length != 2 || !int.TryParse(head[1].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out int serial))
                {
                    failures.Add("unreadable protected line: " + line);
                    continue;
                }
                string? current = null;
                if (head[0] == "RSM")
                {
                    if (World.FindItem((Serial)serial) is RolledUpSheetMusic rsm && !rsm.Deleted)
                        current = RsmLine(rsm);
                    else
                        failures.Add(string.Format("{0}: no longer a rolled up sheet music", want[0]));
                }
                else if (head[0] == "LIB")
                {
                    if (library.TryGetValue(serial, out var kv))
                        current = LibLine(kv.Key, kv.Value);
                    else
                        failures.Add(string.Format("{0}: no longer in the music library", want[0]));
                }
                else
                    failures.Add("unreadable protected line: " + line);
                if (current == null)
                    continue;

                string[] have = current.Split('|');
                if (have.Length != want.Length)
                {
                    failures.Add(string.Format("{0}: is '{1}', expected '{2}'", want[0], current, line));
                    continue;
                }
                for (int i = 1; i < want.Length; i++)
                    if (have[i] != want[i])
                    {
                        int space = want[i].IndexOf(' ');
                        string field = space < 0 ? want[i] : want[i].Substring(0, space);
                        failures.Add(string.Format("{0}: {1} is {2}, expected {3}", want[0], field,
                            space < 0 ? have[i] : have[i].Substring(Math.Min(space + 1, have[i].Length)), space < 0 ? want[i] : want[i].Substring(space + 1)));
                    }
            }
            return failures;
        }

        /// <summary>
        /// Writes the protection snapshot of a run: BEFORE lines (the state found) and EXPECT lines (the state after the repair).
        /// A previous snapshot at that path is kept with a timestamp suffix. Throws if it cannot be written.
        /// </summary>
        public static void SaveProtection(string path, Result r, string caller)
        {
            Utility.EnsurePath(path);
            if (File.Exists(path))
                File.Copy(path, path + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss"), overwrite: true);
            StringBuilder sb = new();
            sb.AppendLine(string.Format("# {0:u} {1}: BEFORE is the state found, EXPECT the state the repair must leave", DateTime.UtcNow, caller));
            foreach (string line in r.ProtectedBefore)
                sb.AppendLine("BEFORE " + line);
            foreach (string line in r.Protected)
                sb.AppendLine("EXPECT " + line);
            File.WriteAllText(path, sb.ToString());
        }

        /// <summary>
        /// Reads the EXPECT lines of a protection snapshot (empty if there is none).
        /// </summary>
        public static List<string> LoadExpected(string path)
        {
            return File.Exists(path)
                ? File.ReadAllLines(path).Where(l => l.StartsWith("EXPECT ")).Select(l => l.Substring("EXPECT ".Length)).ToList()
                : new List<string>();
        }

        // instrumentSelf / hash: the planned repair, for the EXPECT line
        private static string RsmLine(RolledUpSheetMusic rsm, bool instrumentSelf = false, int hash = 0)
        {
            string instrument = instrumentSelf || ReferenceEquals(rsm.Instrument, rsm) ? "self"
                : rsm.Instrument == null ? "null" : string.Format("0x{0:X8}", rsm.Instrument.Serial.Value);
            return string.Format("RSM 0x{0:X8}|Title {1}|Author {2}|Price {3}|Napster {4}|Owner {5}|OwnerGUID {6}|HashCode {7}|Instrument {8}|Composition {9}",
                rsm.Serial.Value, Quote(rsm.Title), Quote(rsm.Author), rsm.Price, rsm.Napster, MobileText(rsm.Owner), rsm.OwnerGUID,
                hash != 0 ? hash : rsm.HashCode, instrument, Checksum(rsm.Composition));
        }

        private static string LibLine(RolledUpSheetMusic key, MusicLibrary.MusicInfo mi)
        {
            return string.Format("LIB 0x{0:X8}|HashCode {1}|Owner {2}|OwnerGUID {3}|Price {4}|Status {5}|Napster {6}|Remastered {7}|Purchasers {8}",
                key.Serial.Value, key.HashCode, MobileText(mi.Owner), mi.OwnerGUID, mi.Price, mi.Status, mi.Napster, mi.Remastered, mi.Purchasers.Count);
        }

        private static string Quote(string? s) => s == null ? "null" : "'" + s.Replace('|', '/').Replace('\r', ' ').Replace('\n', ' ') + "'";
        private static string MobileText(Mobile? m) => m == null ? "none" : string.Format("0x{0:X8}", m.Serial.Value);
        private static string Checksum(string? s)
        {
            if (s == null)
                return "null";
            using SHA256 sha = SHA256.Create();
            return s.Length + " " + Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
        }
        #endregion Protection

        /// <summary>
        /// Checks that each applied repair (serial, field, value) still holds, e.g. after a save and restart. Returns the failures.
        /// </summary>
        public static List<string> Verify(IEnumerable<string> records)
        {
            List<string> failures = new();
            foreach (string record in records)
            {
                string[] part = record.Split('\t');
                if (part.Length < 3 || !uint.TryParse(part[0].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out uint serial))
                {
                    failures.Add("unreadable record: " + record);
                    continue;
                }
                if (World.FindItem((Serial)(int)serial) is not RolledUpSheetMusic rsm || rsm.Deleted)
                    failures.Add(string.Format("{0}: no longer a rolled up sheet music", part[0]));
                else if (part[1] == "Instrument" && !ReferenceEquals(rsm.Instrument, rsm))
                    failures.Add(string.Format("{0}: Instrument is {1}, expected self", part[0], rsm.Instrument == null ? "null" : ItemText(rsm.Instrument)));
                else if (part[1] == "HashCode" && rsm.HashCode.ToString() != part[2])
                    failures.Add(string.Format("{0}: HashCode is {1}, expected {2}", part[0], rsm.HashCode, part[2]));
            }
            return failures;
        }

        #region References
        // Music boxes keep RSMs in their players' ignore lists (saved), their play context, and the library's unpublished
        //  review list (MusicLibrary.ReviewQueue). The music box fields are private, so they are read by reflection; null
        //  if any of them cannot be read.
        private static Dictionary<Item, List<string>>? MusicBoxReferences()
        {
            try
            {
                const BindingFlags inst = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo? config = typeof(MusicBox).GetField("Profiles", inst);
                FieldInfo? play = typeof(MusicBox).GetField("m_playContext", inst);
                if (config == null || play == null)
                    return null;

                Dictionary<Item, List<string>> refs = new(ReferenceEqualityComparer.Instance);
                void Add(object? o, string where)
                {
                    if (o is Item item)
                    {
                        if (!refs.TryGetValue(item, out List<string>? list))
                            refs[item] = list = new List<string>();
                        if (!list.Contains(where))
                            list.Add(where);
                    }
                }

                foreach (object o in MusicLibrary.ReviewQueue)
                    Add(o, "the unpublished review list");
                foreach (MusicBox box in World.Items.Values.OfType<MusicBox>().Where(b => !b.Deleted).ToList())
                {
                    if (config.GetValue(box) is IDictionary contexts)
                        foreach (DictionaryEntry de in contexts)
                            if (de.Value is Server.Items.MusicBoxProfile mc && mc.IgnoreList != null)
                                foreach (RolledUpSheetMusic r in mc.IgnoreList)
                                    Add(r, string.Format("music box {0} ignore list", box.Serial));
                    if (play.GetValue(box) is MusicBox.PlayContext pc)
                    {
                        foreach (RolledUpSheetMusic r in pc.Playlist)
                            Add(r, string.Format("music box {0} playlist", box.Serial));
                        foreach (RolledUpSheetMusic r in pc.IgnoreList)
                            Add(r, string.Format("music box {0} ignore list", box.Serial));
                    }
                }
                return refs;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                return null;
            }
        }
        #endregion References

        #region Reporting
        private static string Describe(RolledUpSheetMusic rsm, Dictionary<Item, List<string>>? referencedBy)
        {
            string owner = rsm.Owner == null ? "none" : string.Format("'{0}' {1}{2}", rsm.Owner.Name, rsm.Owner.Serial, rsm.Owner.Deleted ? " (deleted)" : "");
            string where;
            if (rsm.Parent == null)
                where = rsm.Map == Map.Internal || rsm.IsIntMapStorage ? "internal storage" : string.Format("{0} {1}", rsm.Map, rsm.Location);
            else
            {
                object root = rsm.RootParent;
                string rootText = root is Mobile m ? string.Format("mobile '{0}' {1}", m.Name, m.Serial) : root is Item ri ? ItemText(ri) : "?";
                Point3D at = root is IEntity ent ? ent.Location : Point3D.Zero;
                Map? map = root is IEntity e2 ? e2.Map : null;
                where = string.Format("in {0}, root {1} at {2} {3}", rsm.Parent is Item p ? ItemText(p) : "mobile", rootText, map, at);
            }
            string refs = referencedBy != null && referencedBy.TryGetValue(rsm, out List<string>? list) ? string.Join(", ", list) : "none";
            return string.Format("RSM {0} '{1}' by '{2}' (song '{3}', file '{4}', composition {5}); owner {6}, GUID {7}; {8}; music box references: {9}",
                rsm.Serial, rsm.Title, rsm.Author, rsm.SongName, rsm.SongFile, rsm.Composition == null ? "null" : rsm.Composition.Length + " chars",
                owner, rsm.OwnerGUID, where, refs);
        }

        private static string ItemText(Item i)
        {
            return string.Format("{0} {1}{2}", i.GetType().Name, i.Serial, i.Deleted ? " (deleted)" : "");
        }

        private static void AppendLog(string text)
        {
            try
            {
                string path = Path.Combine(Core.LogsDirectory, LogFile);
                Utility.EnsurePath(path);
                File.AppendAllText(path, text + Environment.NewLine);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        private static List<string> ReadApplied()
        {
            string path = Path.Combine(Core.LogsDirectory, AppliedFile);
            return File.Exists(path) ? File.ReadAllLines(path).Where(l => l.Length > 0 && !l.StartsWith("#")).ToList() : new List<string>();
        }

        // serial <TAB> field <TAB> value <TAB> previous value: what Verify checks, and what a manual rollback would restore
        private static void WriteApplied(List<string> records, string caller)
        {
            try
            {
                string path = Path.Combine(Core.LogsDirectory, AppliedFile);
                Utility.EnsurePath(path);
                File.AppendAllText(path, string.Format("# {0:u} {1}{2}{3}{2}", DateTime.UtcNow, caller, Environment.NewLine, string.Join(Environment.NewLine, records)));
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        #endregion Reporting
    }
}