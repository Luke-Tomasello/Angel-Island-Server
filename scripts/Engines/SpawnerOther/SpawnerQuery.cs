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

/* Scripts\Engines\Spawner\SpawnerQuery.cs
 * ChangeLog
 *  1/31/2026, Adam
 * Add SpawnerQuery helper for domain/content-based spawner filtering
 * Body:
 * Introduces SpawnerQuery, a reusable helper that operates over Spawner.Instances and returns filtered subsets based on
 * map scope, domain classification, and content classification. This centralizes spawner filtering logic that was previously ad-hoc,
 * duplicated, or implicit, and provides a consistent way for systems to reason about where spawners exist and what they represent.
 * SpawnerQuery classifies spawners along orthogonal axes (map, domain, content) and applies caller-specified inclusion/exclusion masks.
 * The default model is inclusive: all spawners are considered valid unless explicitly excluded by the query.
 * Terrain-backed checks (e.g. water) are used where necessary to avoid relying solely on spawn metadata.
 * Usage example:
 * Treasure map generation uses SpawnerQuery to obtain a prefiltered list of candidate spawners
 * (e.g., Felucca, non-town, non-guarded, non-sea) before deriving potential treasure locations.
 * This keeps treasure logic focused on location selection rather than spawner interpretation, and allows the same filtering mechanism
 * to be reused by other systems without coupling or special cases.
 */

using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Server.Misc
{
    [Flags]
    public enum MapScope : uint
    {
        None = 0,
        Felucca = 1u << 0,
        Internal = 1u << 1,

        Any = 0xFFFFFFFFu
    }

    [Flags]
    public enum SpawnDomain : uint
    {
        None = 0,

        // Ownership / intent
        Staff = 1u << 0,

        // Region-derived "rules"
        House = 1u << 1,
        Jail = 1u << 2,
        GreenAcres = 1u << 3,
        AngelIsland = 1u << 4,
        Dungeon = 1u << 5,
        Town = 1u << 6,
        Guarded = 1u << 7,
        World = 1u << 8,

        Any = 0xFFFFFFFFu
    }

    [Flags]
    public enum SpawnContent : uint
    {
        None = 0,

        Item = 1u << 0,
        Vendor = 1u << 1,
        Humanoid = 1u << 2,
        Animal = 1u << 3,
        Monster = 1u << 4,
        Sea = 1u << 5,

        Any = 0xFFFFFFFFu
    }

    public static class SpawnerQuery
    {
        // Main query entrypoint.
        public static List<Spawner> GetSpawners(
            MapScope maps = MapScope.Any,
            SpawnDomain domain = SpawnDomain.Any,
            SpawnContent content = SpawnContent.Any)
        {
            var list = new List<Spawner>();

            foreach (Spawner s in Spawner.Instances)
            {
                if (s == null)
                    continue;

                if (!MapMatches(s, maps))
                    continue;

                SpawnDomain d = ClassifyDomain(s);
                if (!Matches(domain, d))
                    continue;

                SpawnContent c = ClassifyContent(s);

                // First: normal content matching
                if (!Matches(content, c))
                    continue;

                // Second: terrain backstop for Sea exclusion
                // If the caller does NOT want Sea content, and this spawner is physically in water, reject it.
                if ((content & SpawnContent.Sea) == 0)
                {
                    Point3D p = s.Location;

                    if (Utility.IsWater(s.Map, p.X, p.Y, p.Z))
                        continue;
                }

                list.Add(s);
            }

            return list;
        }

        private static bool Matches(SpawnDomain filter, SpawnDomain value)
        {
            if (filter == SpawnDomain.Any)
                return true;

            return (value & filter) != 0;
        }

        private static bool Matches(SpawnContent filter, SpawnContent value)
        {
            if (filter == SpawnContent.Any)
                return true;

            return (value & filter) != 0;
        }

        private static bool MapMatches(Spawner s, MapScope maps)
        {
            if (maps == MapScope.Any)
                return true;

            Map m = s.Map;

            if (m == Map.Internal)
                return (maps & MapScope.Internal) != 0;

            if (m == Map.Felucca)
                return (maps & MapScope.Felucca) != 0;

            // If you truly only care about Internal/Felucca, treat others as non-matching.
            return false;
        }

        // Domain classifier: driven by StaffOwned + Region rule flags.
        private static SpawnDomain ClassifyDomain(Spawner s)
        {
            SpawnDomain d = SpawnDomain.None;

            if (s.IsStaffOwned)
                d |= SpawnDomain.Staff;

            Region? r = null;
            try { r = Region.Find(s.Location, s.Map); } catch { r = null; }

            if (r != null)
            {
                if (r.IsHouseRules) d |= SpawnDomain.House;
                if (r.IsJailRules) d |= SpawnDomain.Jail;
                if (r.IsGreenAcresRules) d |= SpawnDomain.GreenAcres;
                if (r.IsPrisonRules) d |= SpawnDomain.AngelIsland;
                if (r.IsDungeonRules) d |= SpawnDomain.Dungeon;
                if (r.IsTownRules) d |= SpawnDomain.Town;
                if (r.IsGuarded) d |= SpawnDomain.Guarded;
            }

            // Baseline world membership: if no region-derived rules were set,
            // it is considered part of the normal world.
            if ((d & (SpawnDomain.House | SpawnDomain.Jail | SpawnDomain.GreenAcres | SpawnDomain.AngelIsland |
                      SpawnDomain.Dungeon | SpawnDomain.Town | SpawnDomain.Guarded)) == 0)
            {
                d |= SpawnDomain.World;
            }

            return d;
        }


        // Content classifier:
        // - Prefer instance-based classification (Spawner.Objects) so we can use Body flags.
        // - Fall back to type-only classification if Objects list is empty/unavailable.
        public static SpawnContent ClassifyContent(Spawner s)
        {
            SpawnContent c = SpawnContent.None;

            bool sawAnyObject = false;

            foreach (object o in EnumerateSpawnObjects(s))
            {
                if (o == null)
                    continue;

                sawAnyObject = true;

                // Items
                Item? item = o as Item;
                if (item != null)
                {
                    c |= SpawnContent.Item;
                    continue;
                }

                // Mobiles
                Mobile? m = o as Mobile;
                if (m == null)
                    continue;

                // Vendors
                if (m is BaseVendor)
                {
                    c |= SpawnContent.Vendor;
                    continue;
                }

                // Creatures (Body-based taxonomy)
                BaseCreature? bc = m as BaseCreature;
                if (bc != null)
                {
                    // NOTE: these are not necessarily mutually exclusive in your implementation,
                    // so we OR everything that applies.
                    if (bc.Body.IsHuman)
                        c |= SpawnContent.Humanoid;

                    if (bc.Body.IsAnimal)
                        c |= SpawnContent.Animal;

                    if (bc.Body.IsMonster)
                        c |= SpawnContent.Monster;

                    if (bc.Body.IsSea)
                        c |= SpawnContent.Sea;

                    // Safety net: if none of the above fired, decide a default.
                    if ((c & (SpawnContent.Humanoid | SpawnContent.Animal | SpawnContent.Monster | SpawnContent.Sea)) == 0)
                        c |= SpawnContent.Monster;

                    continue;
                }

                // Non-creature mobiles (town NPCs, escorts, hirelings, etc.)
                c |= SpawnContent.Humanoid;
            }

            if (sawAnyObject)
                return c;

            // Fallback: type-only classification (less accurate, but better than empty).
            foreach (Type t in EnumerateSpawnTypes(s))
            {
                if (t == null)
                    continue;

                if (typeof(Item).IsAssignableFrom(t))
                {
                    c |= SpawnContent.Item;
                    continue;
                }

                if (!typeof(Mobile).IsAssignableFrom(t))
                    continue;

                if (typeof(BaseVendor).IsAssignableFrom(t))
                {
                    c |= SpawnContent.Vendor;
                    continue;
                }

                if (typeof(BaseCreature).IsAssignableFrom(t))
                {
                    // TODO(backfill): if you have a static creature taxonomy by Type, do it here.
                    // Otherwise leave as Monster in fallback mode.
                    c |= SpawnContent.Monster;
                    continue;
                }

                c |= SpawnContent.Humanoid;
            }

            return c;
        }

        // Enumerate the spawn templates/definitions stored on the spawner.
        // This is your preferred path because it lets us read Body flags from actual Mobile instances.
        private static IEnumerable<object> EnumerateSpawnObjects(Spawner s)
        {
            // TODO(backfill): if your field/property is not named Objects, change it here.
            // The user stated: "We will have a list of Objects (that is what's currently spawner...)"
            ArrayList list = s.Objects;

            if (list == null || list.Count == 0)
                yield break;

            for (int i = 0; i < list.Count; i++)
                yield return list[i];
        }

        // Optional fallback if you can enumerate types without instances.
        // If you do not have such a mechanism, returning empty is fine.
        private static IEnumerable<Type> EnumerateSpawnTypes(Spawner s)
        {
            // TODO(backfill): implement only if you have an authoritative list of types.
            // Example patterns:
            // - s.SpawnNames (string) -> ScriptCompiler.FindTypeByName(...)
            // - s.Entries with a TypeName
            // - etc.

            return Enumerable.Empty<Type>();
        }
    }
}
