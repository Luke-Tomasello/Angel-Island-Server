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

/* Patches201_300/RemasterOrphanCleanup.cs
 * ChangeLog
 *  10/29/26, Adam (MusicLibrary)
 *      The library is MusicLibrary (moved out of MusicBox); the review queue is MusicLibrary.ReviewQueue (it was read
 *      by reflection, which would have found nothing once the field moved).
 *  10/29/26, Adam
 *      Created. One-time removal of the tracks remasters replaced (PatchV262). Until 10/29/26, approving a remaster
 *      removed the track it replaced from the music library (MusicBox.GlobalMusicRepository) and left the item in
 *      internal storage. A candidate is exactly such a track: a RolledUpSheetMusic on Map.Internal with no parent, not a
 *      temporary object (the music box's playback copies), not a library track and not waiting for review, whose
 *      TrackID (title, author, owner) is that of a library track marked Remastered, with different music (a remaster's
 *      song hash differs from the track it replaces). A track that has lost its owner (on Upheaval, the 2023 originals of
 *      the two tracks Galois remastered in 2024) matches by title and author, if it has no owner GUID or the remaster's. Before anything is deleted the whole plan is made: every item and
 *      mobile in the world, and every static field of the server (except the world's own registries, World.Items and
 *      World.Mobiles), is scanned for references, and a referenced candidate is left. A music box's per-player lists (its
 *      ignore list, sale notices) are not references: they drop deleted tracks when the box loads. If the scan cannot be
 *      completed nothing is deleted (Result.Failed). With apply == false nothing is deleted (dry run). The run is written
 *      to RemasterOrphanCleanup.log, the deleted tracks to RemasterOrphanCleanup.deleted.
 */

using Server;
using Server.Items;
using Server.Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Patches201_300
{
    public static class RemasterOrphanCleanup
    {
        public sealed class Result
        {
            public bool Applied;
            public bool Failed;                         // the plan could not be completed: nothing was deleted
            public int RemasteredTracks;                // library tracks marked Remastered
            public int Candidates;                      // replaced tracks left in internal storage
            public int Referenced;                      // candidates something else references (left)
            public int ExternalReferences;              // references found by the scan (holder, field) to candidates
            public int ScanErrors;                      // holders that could not be read: the scan is incomplete, nothing is deleted
            public int OutOfScope;                      // unreferenced candidates outside the scope (left)
            public int Deleted;                         // tracks deleted (apply)
            public readonly List<string> Lines = new();
            public readonly List<RolledUpSheetMusic> Planned = new();

            public string Summary => string.Format(
                "Remaster orphan cleanup {0}: {1} remastered library tracks; {2} replaced tracks left in internal storage; " +
                "{3} referenced (left); {4} external references; {5} scan errors; {6} out of scope; {7} deleted.",
                Failed ? (Deleted > 0 ? "FAILED" : "FAILED (nothing deleted)") : Applied ? "applied" : "dry run",
                RemasteredTracks, Candidates, Referenced, ExternalReferences, ScanErrors, OutOfScope, Deleted);
        }

        private const string LogFile = "RemasterOrphanCleanup.log";
        private const string DeletedFile = "RemasterOrphanCleanup.deleted";
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Plans the removal of the tracks remasters replaced and, with apply == true, deletes them. The plan (candidates
        /// and the reference scan of the world and of static fields) is complete before anything is deleted; if it cannot
        /// be completed nothing is deleted (Result.Failed). With a scope only candidates inside it are deleted (the
        /// candidates and the reference scan still cover the whole world). Used by PatchV262 (no scope).
        /// </summary>
        public static Result Run(bool apply, string caller, IEnumerable<Item>? scope = null)
        {
            Result result = new Result { Applied = apply };
            try
            {
                // the remastered library tracks, by TrackID, and by title and author (for tracks that lost their owner)
                Dictionary<int, List<RolledUpSheetMusic>> remastered = new();
                Dictionary<string, List<RolledUpSheetMusic>> byTitle = new();
                foreach (KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo> kvp in MusicLibrary.Repository)
                    if (kvp.Key != null && !kvp.Key.Deleted && kvp.Value != null && kvp.Value.Remastered)
                    {
                        result.RemasteredTracks++;
                        if (!remastered.TryGetValue(kvp.Key.TrackID, out List<RolledUpSheetMusic>? list))
                            remastered[kvp.Key.TrackID] = list = new();
                        list.Add(kvp.Key);
                        if (!byTitle.TryGetValue(TitleAuthor(kvp.Key), out List<RolledUpSheetMusic>? titled))
                            byTitle[TitleAuthor(kvp.Key)] = titled = new();
                        titled.Add(kvp.Key);
                    }

                HashSet<Item> library = new(MusicLibrary.Repository.Keys.Where(k => k != null), ReferenceEqualityComparer.Instance);
                HashSet<Item> review = new(MusicLibrary.ReviewQueue.Where(r => r != null), ReferenceEqualityComparer.Instance);

                HashSet<Item> candidates = new(ReferenceEqualityComparer.Instance);
                foreach (Item item in World.Items.Values.ToList())
                {
                    if (item is not RolledUpSheetMusic r || r.Deleted || r.Map != Map.Internal || r.Parent != null)
                        continue;
                    if (r.GetItemBool(Item.ItemBoolTable.TempObject) || library.Contains(r) || review.Contains(r))
                        continue;
                    // the same TrackID (title, author, owner); a track that has lost its owner: the same title and author,
                    //  and no owner GUID or the remaster's
                    bool ownerless = false;
                    if (!remastered.TryGetValue(r.TrackID, out List<RolledUpSheetMusic>? remasters))
                    {
                        if (r.Owner != null || !byTitle.TryGetValue(TitleAuthor(r), out List<RolledUpSheetMusic>? titled))
                            continue;
                        remasters = titled.Where(k => r.OwnerGUID == 0 || r.OwnerGUID == k.OwnerGUID || (k.Owner != null && r.OwnerGUID == k.Owner.AcctGUID)).ToList();
                        ownerless = true;
                    }
                    RolledUpSheetMusic? by = remasters.FirstOrDefault(k => k.HashCode != r.HashCode);
                    if (by == null)
                        continue;                               // the same music as the library track: not a replaced version
                    candidates.Add(r);
                    result.Lines.Add(string.Format("CANDIDATE {0} '{1}' by {2}, created {3:u}, replaced by {4} (remastered {5:u}){6}",
                        r.Serial, r.Title, r.Author, r.Created, by.Serial, MusicLibrary.Repository[by].DateRemastered,
                        ownerless ? string.Format("; no owner (owner GUID 0x{0:X}): matched by title and author", r.OwnerGUID) : ""));
                }
                result.Candidates = candidates.Count;

                // reference scan: every item and mobile, and every static field, before anything is deleted
                Dictionary<Item, string> referenced = new(ReferenceEqualityComparer.Instance);
                if (candidates.Count > 0)
                    ReferenceScan(candidates, referenced, result);
                if (result.ScanErrors > 0)
                    throw new InvalidOperationException(string.Format("the reference scan could not read {0} holder(s)", result.ScanErrors));

                HashSet<Item>? inScope = scope == null ? null : new HashSet<Item>(scope, ReferenceEqualityComparer.Instance);
                foreach (Item c in candidates)
                {
                    if (referenced.TryGetValue(c, out string? why))
                    {
                        result.Referenced++;
                        result.Lines.Add(string.Format("REFERENCED (left) {0}: {1}", c.Serial, why));
                    }
                    else if (inScope != null && !inScope.Contains(c))
                        result.OutOfScope++;
                    else
                        result.Planned.Add((RolledUpSheetMusic)c);
                }

                if (apply)
                {
                    List<string> deleted = new();
                    foreach (RolledUpSheetMusic r in result.Planned)
                    {
                        deleted.Add(string.Format("{0}\t{1}\t{2}\t{3}\t{4:u}", r.Serial, r.TrackID, r.HashCode, r.Title, r.Created));
                        r.Delete();
                        result.Deleted++;
                    }
                    if (deleted.Count > 0)
                        File.AppendAllLines(Path.Combine(Core.LogsDirectory, DeletedFile), deleted);
                }
            }
            catch (Exception ex)
            {
                result.Failed = true;
                result.Lines.Add("EXCEPTION (nothing deleted): " + ex.Message);
            }

            try
            {
                List<string> log = new() { string.Format("{0:u} {1}: {2}", DateTime.UtcNow, caller, result.Summary) };
                log.AddRange(result.Lines.Select(l => "    " + l));
                File.AppendAllLines(Path.Combine(Core.LogsDirectory, LogFile), log);
            }
            catch (Exception ex) { Server.Diagnostics.LogHelper.LogException(ex); }

            return result;
        }

        // TrackID's text without the owner
        private static string TitleAuthor(RolledUpSheetMusic r) => ((r.Title ?? "") + (r.Author ?? "")).Replace(" ", "").ToLowerInvariant();

        #region Reference scan
        // Records, for every candidate, the first holder found that references it (a field of an item or a mobile, or a
        //  collection held by one, or a static field). A candidate referencing itself, or another candidate, is not a
        //  reference (they go together).
        private static void ReferenceScan(HashSet<Item> candidates, Dictionary<Item, string> referenced, Result result)
        {
            Dictionary<Type, FieldInfo[]> cache = new();
            void Note(object? value, object holder, string field)
            {
                if (value is Item i && candidates.Contains(i) && !ReferenceEquals(holder, i) && !(holder is Item h && candidates.Contains(h)))
                {
                    result.ExternalReferences++;
                    if (!referenced.ContainsKey(i))
                        referenced[i] = string.Format("referenced by {0}.{1}", Describe(holder), field);
                }
            }
            void Scan(object holder)
            {
                foreach (FieldInfo f in Fields(holder.GetType(), Instance, cache))
                    try
                    {
                        Value(f.GetValue(holder), holder, f.Name, 0);
                    }
                    catch (Exception ex)
                    {
                        result.ScanErrors++;
                        result.Lines.Add(string.Format("SCAN ERROR: {0}.{1}: {2}", Describe(holder), f.Name, ex.Message));
                    }
            }
            void Value(object? v, object holder, string field, int depth)
            {
                if (v == null)
                    return;
                if (v is Item)
                    Note(v, holder, field);
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
                    Scan(item);
            foreach (Mobile m in World.Mobiles.Values.ToList())
                if (!m.Deleted)
                    Scan(m);

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

        // fields that can hold an item or a collection of them (a music box's per-player MusicContext is not one: it holds
        //  the box's ignore list and sale notices, which drop deleted tracks when the box loads)
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
            if (ft == typeof(object) || typeof(Item).IsAssignableFrom(ft) || ft.IsAssignableFrom(typeof(RolledUpSheetMusic)))
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
                _ => holder.GetType().Name,
            };
        }
        #endregion Reference scan
    }
}
