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

/* Patches201_300/MusicOrphanCleanup.cs
 * ChangeLog
 *  10/25/26, Adam
 *      Created. One-time removal of orphaned virtual instruments (PatchV256). Every MusicContext creates three
 *      RazorInstruments (harp, lapharp, lute) in internal storage; before Phase 3 Stage 1 the hourly
 *      CMusicConfigCleanup dropped contexts without deleting them, and CalypsoMusicPlayer.GetMetaData leaked a set per
 *      call (D1). A candidate is exactly a RazorInstrument, on Map.Internal with no parent, in internal storage, named
 *      harp, lapharp or lute. Only complete sets are removed: a harp, lapharp and lute next to each other in serial order
 *      (their serials may skip numbers held by other items) and created together, as the MusicContext constructor makes
 *      them; then, among the unreferenced candidates that first pass could not pair (older saves reused serials, so a
 *      set's members can interleave with other items'), those created in the same second form k sets when they are
 *      exactly k harps, k lapharps and k lutes. Every other candidate is left and reported. Before anything is deleted the whole plan is made: the instruments of every live session
 *      (SystemMusicPlayer.MusicConfig) are protected, and every item and mobile in the world, and every static field of
 *      the server (except the world's own registries, World.Items and World.Mobiles), is scanned for references; a set
 *      with any referenced member is excluded. Incomplete and referenced candidates are reported and left. With apply == false nothing is
 *      deleted (dry run). The run is written to MusicOrphanCleanup.log, the deleted sets to MusicOrphanCleanup.deleted.
 */

using Server.Commands;
using Server.Diagnostics;
using Server.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MusicContext = Server.Commands.MusicContext;   // the engine's session (Server.Items has a MusicBox MusicContext too)
using Server;

namespace Patches201_300
{
    public static class MusicOrphanCleanup
    {
        public sealed class Result
        {
            public bool Applied;
            public bool Failed;                         // the plan could not be completed: nothing was deleted
            public int Candidates;                      // RazorInstruments matching the candidate criteria
            public int Sessions, SessionInstruments;    // live sessions and the candidates they hold (protected)
            public int CompleteSets;                    // unreferenced complete harp/lapharp/lute sets (to delete)
            public int Incomplete;                      // candidates not in a complete set (left)
            public int ReferencedSets;                  // complete sets with a referenced member, other than a live session (left)
            public int ExternalReferences;              // references found by the scan (holder, field) to candidates, other than live sessions
            public int ScanErrors;                      // holders that could not be read: the scan is incomplete, nothing is deleted
            public int Individuals;                     // individual orphan candidates: candidates no live session or other holder references
            public int ValidatedTriplets;               // complete unreferenced sets in the world (before any scope)
            public int SecondPassTriplets;              // of those, sets found by creation second (interleaved serials)
            public int IndividualsInReferencedSets;     // individuals whose set has a referenced member (left)
            public int UngroupedIndividuals;            // individuals in no complete set (left, reported for review)
            // every individual is accounted for: 3 per validated triplet, or left as an exclusion
            public bool Accounted => Individuals == ValidatedTriplets * 3 + IndividualsInReferencedSets + UngroupedIndividuals;
            public int Deleted;                         // items deleted (apply)
            public readonly List<string> Lines = new();
            public readonly List<Item[]> Planned = new();     // the sets to delete (harp, lapharp, lute), as planned

            public string Summary => string.Format(
                "Music orphan cleanup {0}: {1} candidate instruments; {2} live sessions holding {3}; {11} individual orphan candidates, {12} validated triplets " +
                "({13} items, {15} of the sets by creation second; {14}); {4} complete unreferenced sets to delete ({5} items); " +
                "{6} referenced sets and {7} incomplete candidates left; {8} external references; {9} scan errors; {10} items deleted.",
                Failed ? (Deleted > 0 ? "FAILED" : "FAILED (nothing deleted)") : Applied ? "applied" : "dry run", Candidates, Sessions, SessionInstruments,
                CompleteSets, CompleteSets * 3, ReferencedSets, Incomplete, ExternalReferences, ScanErrors, Deleted,
                Individuals, ValidatedTriplets, ValidatedTriplets * 3, Accounted ? "all individuals accounted for" : "INDIVIDUALS NOT ACCOUNTED FOR",
                SecondPassTriplets);
        }

        private const string LogFile = "MusicOrphanCleanup.log";
        private const string DeletedFile = "MusicOrphanCleanup.deleted";
        private static readonly string[] SetNames = { "harp", "lapharp", "lute" };   // MusicContext constructor order
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Plans the removal of orphaned virtual instrument sets and, with apply == true, deletes them. The plan (candidates,
        /// live sessions, the reference scan of the world and of static fields) is complete before anything is deleted; if it
        /// cannot be completed nothing is deleted (Result.Failed). With a scope only sets entirely inside it are deleted (the
        /// candidates, sessions and reference scan still cover the whole world). Used by PatchV256 (no scope).
        /// </summary>
        public static Result Run(bool apply, string caller, IEnumerable<Item>? scope = null)
        {
            Result result = new Result { Applied = apply };
            List<Item[]> sets = new();
            try
            {
                // candidates
                HashSet<Item> candidates = new(ReferenceEqualityComparer.Instance);
                foreach (Item i in World.Items.Values)
                    if (IsCandidate(i))
                        candidates.Add(i);
                result.Candidates = candidates.Count;

                // live sessions: their instruments are never candidates for deletion
                Dictionary<Item, string> referenced = new(ReferenceEqualityComparer.Instance);
                foreach (KeyValuePair<Mobile, MusicContext> kvp in SystemMusicPlayer.MusicConfig.ToList())
                {
                    if (kvp.Value == null)
                        continue;
                    result.Sessions++;
                    List<string> held = new();
                    foreach (Item i in ItemsIn(kvp.Value))
                    {
                        held.Add(string.Format("{0} {1}", i.Serial, i.Name));
                        if (candidates.Contains(i) && !referenced.ContainsKey(i))
                        {
                            result.SessionInstruments++;
                            referenced[i] = string.Format("live session of {0} 0x{1:X8} '{2}'", kvp.Key?.GetType().Name, kvp.Key?.Serial.Value, kvp.Key?.Name);
                        }
                    }
                    result.Lines.Add(string.Format("SESSION {0} 0x{1:X8} '{2}': {3}", kvp.Key?.GetType().Name, kvp.Key?.Serial.Value, kvp.Key?.Name, string.Join(", ", held)));
                }

                // reference scan: every item and mobile, and every static field, before anything is deleted
                ReferenceScan(candidates, referenced, result);
                if (result.ScanErrors > 0)
                    throw new InvalidOperationException(string.Format("the reference scan could not read {0} holder(s)", result.ScanErrors));

                // complete sets: a harp, lapharp and lute next to each other in serial order (no other candidate between them;
                //  their serials may skip numbers other items hold), created together
                List<Item> ordered = candidates.OrderBy(i => i.Serial.Value).ToList();
                HashSet<Item> inSet = new(ReferenceEqualityComparer.Instance);
                for (int ix = 0; ix + 2 < ordered.Count; ix++)
                {
                    Item a = ordered[ix], b = ordered[ix + 1], c = ordered[ix + 2];
                    if (a.Name != SetNames[0] || b.Name != SetNames[1] || c.Name != SetNames[2])
                        continue;
                    if ((c.Created - a.Created).Duration() > TimeSpan.FromSeconds(5) || (b.Created - a.Created).Duration() > TimeSpan.FromSeconds(5))
                        continue;
                    Item[] set = { a, b, c };
                    inSet.Add(a); inSet.Add(b); inSet.Add(c);
                    ix += 2;
                    List<string> why = set.Where(referenced.ContainsKey).Select(i => string.Format("{0} {1}: {2}", i.Serial, i.Name, referenced[i])).ToList();
                    if (why.Count == 0)
                        sets.Add(set);
                    else if (!set.All(i => referenced.ContainsKey(i) && referenced[i].StartsWith("live session")))
                    {
                        result.ReferencedSets++;
                        result.IndividualsInReferencedSets += set.Count(i => !referenced.ContainsKey(i));
                        result.Lines.Add("REFERENCED SET (left): " + string.Join("; ", why));
                    }
                }
                // second pass, over the unreferenced candidates the first could not pair (older saves reused serials, so a
                //  set's members can be interleaved with other items'): candidates created in the same second form k sets when
                //  they are exactly k harps, k lapharps and k lutes (paired by serial order); any other group is left
                foreach (var second in ordered.Where(i => !inSet.Contains(i) && !referenced.ContainsKey(i))
                    .GroupBy(i => i.Created.Ticks / TimeSpan.TicksPerSecond).OrderBy(g => g.Key))
                {
                    List<Item>[] byName = SetNames.Select(n => second.Where(i => i.Name == n).OrderBy(i => i.Serial.Value).ToList()).ToArray();
                    int k = byName[0].Count;
                    if (k == 0 || byName[1].Count != k || byName[2].Count != k)
                        continue;
                    for (int j = 0; j < k; j++)
                    {
                        Item[] set = { byName[0][j], byName[1][j], byName[2][j] };
                        foreach (Item i in set)
                            inSet.Add(i);
                        sets.Add(set);
                        result.SecondPassTriplets++;
                    }
                }

                // individual orphan candidates versus validated triplets: every individual must be accounted for
                result.Individuals = candidates.Count(i => !referenced.ContainsKey(i));
                result.ValidatedTriplets = sets.Count;
                result.UngroupedIndividuals = ordered.Count(i => !inSet.Contains(i) && !referenced.ContainsKey(i));
                result.Lines.Add(string.Format("INDIVIDUALS {0} = {1} validated triplets x 3 ({2}; {6} by serial order, {7} by creation second) + {3} in referenced sets + {4} ungrouped: {5}",
                    result.Individuals, result.ValidatedTriplets, result.ValidatedTriplets * 3, result.IndividualsInReferencedSets, result.UngroupedIndividuals,
                    result.Accounted ? "accounted" : "NOT ACCOUNTED", result.ValidatedTriplets - result.SecondPassTriplets, result.SecondPassTriplets));

                if (scope != null)
                {
                    HashSet<Item> inScope = new(scope.Where(i => i != null), ReferenceEqualityComparer.Instance);
                    result.Lines.Add(string.Format("SCOPED: {0} of {1} complete unreferenced sets are inside the scope", sets.Count(s => s.All(inScope.Contains)), sets.Count));
                    sets = sets.Where(s => s.All(inScope.Contains)).ToList();
                }
                result.CompleteSets = sets.Count;
                result.Planned.AddRange(sets);
                foreach (Item i in ordered.Where(i => !inSet.Contains(i)))
                {
                    result.Incomplete++;
                    result.Lines.Add(string.Format("INCOMPLETE (left): {0} {1} created {2:u}{3}", i.Serial, i.Name, i.Created,
                        referenced.TryGetValue(i, out string? r) ? "; " + r : ""));
                }

                // what is to be deleted, by creation month
                foreach (var g in sets.GroupBy(s => s[0].Created.ToString("yyyy-MM")).OrderBy(g => g.Key))
                    result.Lines.Add(string.Format("SETS created {0}: {1}", g.Key, g.Count()));
                foreach (Item[] s in sets.Take(10))
                    result.Lines.Add(string.Format("SAMPLE SET: {0} {1} {2} created {3:u}", s[0].Serial, s[1].Serial, s[2].Serial, s[0].Created));
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                result.Lines.Add("EXCEPTION (nothing deleted): " + ex.Message);
                result.Failed = true;
                result.Applied = false;
                sets.Clear();
            }

            // delete only after the whole plan is made; each item is checked again
            if (apply && !result.Failed)
            {
                List<string> deleted = new();
                try
                {
                    foreach (Item[] set in sets)
                    {
                        if (!set.All(IsCandidate))
                        {
                            result.Lines.Add(string.Format("CHANGED since planning (left): {0}", string.Join(" ", set.Select(i => i.Serial.ToString()))));
                            continue;
                        }
                        foreach (Item i in set)
                        {
                            i.Delete();
                            result.Deleted++;
                        }
                        deleted.Add(string.Format("{0}\t{1}\t{2}\t{3:u}", set[0].Serial, set[1].Serial, set[2].Serial, set[0].Created));
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    result.Lines.Add(string.Format("EXCEPTION while deleting, after {0} items: {1}", result.Deleted, ex.Message));
                    result.Failed = true;
                }
                if (scope == null)
                    WriteFile(DeletedFile, string.Format("# {0:u} {1}: harp lapharp lute created{2}{3}{2}", DateTime.UtcNow, caller, Environment.NewLine,
                    string.Join(Environment.NewLine, deleted)));
            }

            StringBuilder sb = new();
            sb.AppendLine(string.Format("{0:u} {1} ({2}{3})", DateTime.UtcNow, caller, apply ? "apply" : "dry run", scope == null ? "" : ", scoped"));
            foreach (string line in result.Lines)
                sb.AppendLine("  " + line);
            sb.AppendLine("  " + result.Summary);
            WriteFile(LogFile, sb.ToString());
            return result;
        }

        public static bool IsCandidate(Item i)
        {
            return i != null && !i.Deleted && i.GetType() == typeof(RazorInstrument) && i.Map == Map.Internal && i.Parent == null
                && i.IsIntMapStorage && Array.IndexOf(SetNames, i.Name) >= 0;
        }

        #region Reference scan
        // Records, for every candidate, the first holder found that references it (a field of an item, a mobile, a
        //  MusicContext or a collection held by one, or a static field). A candidate referencing itself, or another
        //  candidate, is not a reference (both go together).
        private static void ReferenceScan(HashSet<Item> candidates, Dictionary<Item, string> referenced, Result result)
        {
            Dictionary<Type, FieldInfo[]> cache = new();
            void Note(object? value, object holder, string field)
            {
                if (value is Item i && candidates.Contains(i) && !ReferenceEquals(holder, i) && !(holder is Item h && candidates.Contains(h)))
                {
                    if (referenced.TryGetValue(i, out string? already) && already.StartsWith("live session"))
                        return;                                         // the live session itself (reached again through MusicConfig)
                    result.ExternalReferences++;
                    if (!referenced.ContainsKey(i))
                        referenced[i] = string.Format("referenced by {0}.{1}", Describe(holder), field);
                }
            }
            void Scan(object holder, string prefix, int depth)
            {
                foreach (FieldInfo f in Fields(holder.GetType(), Instance, cache))
                    try
                    {
                        Value(f.GetValue(holder), holder, prefix + f.Name, depth);
                    }
                    catch (Exception ex)
                    {
                        result.ScanErrors++;
                        result.Lines.Add(string.Format("SCAN ERROR: {0}.{1}{2}: {3}", Describe(holder), prefix, f.Name, ex.Message));
                    }
            }
            void Value(object? v, object holder, string field, int depth)
            {
                if (v == null)
                    return;
                if (v is Item)
                    Note(v, holder, field);
                else if (v is MusicContext mc && depth < 2)
                    Scan(mc, field + ".", depth + 1);
                else if (v is IDictionary d)
                {
                    foreach (DictionaryEntry de in d)
                    {
                        Value(de.Key, holder, field + "[key]", depth + 1);
                        Value(de.Value, holder, field + "[value]", depth + 1);
                    }
                }
                else if (v is IEnumerable e && v is not string && depth < 2)
                    foreach (object? o in e)
                        Value(o, holder, field + "[]", depth + 1);
            }

            foreach (Item item in World.Items.Values.ToList())
                if (!item.Deleted)
                    Scan(item, "", 0);
            foreach (Mobile m in World.Mobiles.Values.ToList())
                if (!m.Deleted)
                    Scan(m, "", 0);

            // static fields (registries held by types); generic type definitions have no values
            foreach (Type t in typeof(Item).Assembly.GetTypes())
            {
                if (t.ContainsGenericParameters)
                    continue;
                foreach (FieldInfo f in t.GetFields(Static))
                {
                    if (f.IsLiteral || !MayHold(f.FieldType))
                        continue;
                    try
                    {
                        object? v = f.GetValue(null);
                        // the world's own registries (World.Items / World.Mobiles) hold every entity: not a reference
                        if (ReferenceEquals(v, World.Items) || ReferenceEquals(v, World.Mobiles))
                            continue;
                        Value(v, t, f.Name, 0);
                    }
                    catch (TypeInitializationException)
                    {   // a type whose initializer failed holds no values
                        result.Lines.Add(string.Format("static {0}.{1}: type not initialized (skipped)", t.FullName, f.Name));
                    }
                    catch (Exception ex)
                    {
                        result.ScanErrors++;
                        result.Lines.Add(string.Format("SCAN ERROR: static {0}.{1}: {2}", t.FullName, f.Name, ex.Message));
                    }
                }
            }
        }

        // fields that can hold an item, a MusicContext or a collection of them
        private static FieldInfo[] Fields(Type t, BindingFlags flags, Dictionary<Type, FieldInfo[]> cache)
        {
            if (cache.TryGetValue(t, out FieldInfo[]? fields))
                return fields;
            List<FieldInfo> list = new();
            for (Type? x = t; x != null && x != typeof(object); x = x.BaseType)
                foreach (FieldInfo f in x.GetFields(flags | BindingFlags.DeclaredOnly))
                    if (MayHold(f.FieldType))
                        list.Add(f);
            return cache[t] = list.ToArray();
        }

        private static bool MayHold(Type ft)
        {
            if (ft.IsPrimitive || ft.IsEnum || ft == typeof(string) || ft.IsPointer || typeof(Delegate).IsAssignableFrom(ft))
                return false;
            if (ft == typeof(object) || typeof(Item).IsAssignableFrom(ft) || ft.IsAssignableFrom(typeof(RazorInstrument)) || ft == typeof(MusicContext))
                return true;
            if (typeof(IEnumerable).IsAssignableFrom(ft))
            {
                if (ft.IsArray)
                    return MayHold(ft.GetElementType()!);
                Type[] args = ft.IsGenericType ? ft.GetGenericArguments() : Type.EmptyTypes;
                return args.Length == 0 || args.Any(MayHold);         // non-generic collections may hold anything
            }
            return false;
        }

        private static string Describe(object holder)
        {
            return holder switch
            {
                Item i => string.Format("{0} {1}", i.GetType().Name, i.Serial),
                Mobile m => string.Format("{0} {1} '{2}'", m.GetType().Name, m.Serial, m.Name),
                Type t => "static " + t.FullName,
                _ => holder.GetType().Name
            };
        }

        private static IEnumerable<Item> ItemsIn(MusicContext mc)
        {
            foreach (FieldInfo f in typeof(MusicContext).GetFields(Instance))
                if (f.GetValue(mc) is Item i)
                    yield return i;
        }
        #endregion Reference scan

        private static void WriteFile(string name, string text)
        {
            try
            {
                string path = Path.Combine(Core.LogsDirectory, name);
                Utility.EnsurePath(path);
                File.AppendAllText(path, text.TrimEnd() + Environment.NewLine);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
    }
}
