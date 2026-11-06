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

/*
 * ZoneGuardianSpawner
 * -------------------
 *
 * PURPOSE:
 * Converts the static ecology of a TerritoryZone into an occupied Orc
 * warband structure.
 *
 * This module is responsible for discovering eligible anchor spawners,
 * creating GuardianWebs, assigning clan ownership, and distributing
 * ContestedSpawners throughout the zone.
 *
 * CORE CONCEPT:
 *
 * Each qualifying anchor spawner becomes a clan-controlled occupation
 * force. The force is represented as a GuardianWeb containing one or
 * more ContestedSpawners.
 *
 * Players do not conquer individual spawners in isolation.
 * They defeat enough members of a clan to collapse the entire web,
 * eventually reclaiming the zone and district.
 *
 * WEB GENERATION:
 *
 * Anchor
 *    ↓
 * GuardianWeb (Clan)
 *    ↓
 * ContestedSpawners
 *    ↓
 * Orc Forces
 *
 * Multiple ContestedSpawners are intentionally used to:
 *
 * - Spread encounters throughout the zone
 * - Create territorial control rather than a single battle
 * - Encourage movement and exploration
 * - Give clan ownership a physical presence in the world
 *
 * WEB WIDTH TUNING:
 *
 * The primary tuning knob in this module is:
 *
 *     CalculateGuardianCount()
 *
 * This controls how many ContestedSpawners are created from a single
 * anchor and therefore determines the physical footprint of a clan's
 * occupation force.
 *
 * Increasing guardian count:
 *
 * - Creates more locations to clear
 * - Increases web complexity
 * - Increases territorial coverage
 * - Does NOT increase total threat budget
 *
 * IMPORTANT:
 *
 * Guardian count is intentionally decoupled from total invasion power.
 *
 * The anchor's threat budget is divided across the generated web:
 *
 *     budgetScale = 1.0 / guardianCount
 *
 * This prevents powerful anchors (dragons, drakes, etc.) from
 * multiplying their threat simply because they generate multiple
 * ContestedSpawners.
 *
 * HISTORICAL NOTE:
 *
 * Earlier versions effectively gave every ContestedSpawner the full
 * anchor budget, causing elite anchors to scale both horizontally
 * (more spawners) and vertically (stronger spawners) at the same time.
 *
 * The current design treats the anchor's threat budget as a shared
 * resource distributed across the web.
 *
 * RELATIONSHIP TO OrcSpawnGenerator:
 *
 * Elite difficulty is controlled by two independent systems:
 *
 * ZoneGuardianSpawner:
 *     Controls web width.
 *
 * OrcSpawnGenerator:
 *     Controls individual spawner strength and composition.
 *
 * Think of the relationship as:
 *
 *     Total Difficulty
 *         =
 *     Web Width
 *         ×
 *     Spawner Strength
 *
 * If elite encounters feel too spread out, adjust guardian counts here.
 * If elite encounters feel too weak or too strong, adjust the power
 * multipliers in OrcSpawnGenerator.
 *
 * DESIGN PHILOSOPHY:
 *
 * This module controls WHERE players fight.
 *
 * OrcSpawnGenerator controls WHAT they fight.
 */

/* Scripts\Engines\TerritoryControl\ZoneGuardianSpawner.cs
 * CHANGELOG:
 *   11/4/2026, Adam:
 *      - Two-axis movement rule (HasTwoAxisMovement, with the diagrams that explain it): a placement must let a walker
 *        step north or south AND east or west (the game's movement check; diagonals do not count). Contested spawners
 *        skip candidates that fail it (TryFindGoodSpawnLocation searches on); the fallback to the anchor's own tile is
 *        exempt. Shared with Orc Warlord placement (TerritoryDistrict.TryPlaceMiniBoss). Why: Trinsic's invisible
 *        one-tile walkway between a wall and the moat, (1858, 2642, -1), passed every other check. Applies to new
 *        placements only ([SetupTerritories); contested spawners already in the world are not moved.
 *   10/19/2026, Adam:
 *      - Contested spawners are placed only on walkable land (no longer canSwim: water tiles were accepted)
 *        with a clear walking path back to their anchor (ClearPathLand; one movement step when adjacent).
 *      - The fallback to the anchor's own tile now requires walkable land; otherwise that guardian is skipped.
 *      - IsWalkableLand / CanWalkTo shared with TerritoryDistrict (Orc Warlord placement).
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See TerritorySystem for an architectural overview
 */

using Server.Engines.TerritoryControl.Events;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Engines.TerritoryControl
{
    public static class ZoneGuardianSpawner
    {
        public static GuardianWeb? SpawnInZone(TerritoryZone zone)
        {
            if (zone == null)
                return null;

            var anchorSpawners = GetValidAnchorSpawners(zone);

            if (anchorSpawners.Count == 0)
                return null;

            var web = new GuardianWeb();

            // Link web to district for conquest tracking
            if (zone.District != null)
                web.SetDistrict(zone.District);

            // Proper bidirectional zone linking
            web.SetZone(zone);

            // One clan per anchor (logical warband grouping)
            foreach (Spawner anchor in anchorSpawners)
            {
                int clanIndex = OrcSpawnGenerator.GenerateClanIndex();

                int guardianCount = CalculateGuardianCount(anchor);
                double budgetScale = 1.0 / guardianCount;

                for (int i = 0; i < guardianCount; i++)
                {
                    Point3D loc;

                    if (!TryGetPlacement(anchor, zone, out loc))
                        continue;

                    // Pass clanIndex so all guardians from the same anchor share a clan
                    var spawnInfo = OrcSpawnGenerator.GenerateForAnchor(
                        anchor,
                        clanIndex,
                        budgetScale);

                    var placement = new ContestedSpawner(spawnInfo);

                    placement.MoveToWorld(loc, zone.Map);
                    web.AddSpawner(placement);
                }
            }

            // Register the web for persistence and tracking
            if (web.Spawners.Count > 0)
            {
                TerritorySystem.RegisterWeb(web);
                return web;
            }

            return null;
        }

        public static List<Spawner> GetValidAnchorSpawners(TerritoryZone zone)
        {
            var list = new List<Spawner>();

            foreach (Spawner spawner in Spawner.Instances)
            {
                if (spawner == null || spawner.Deleted)
                    continue;

                if (spawner.GetType() != typeof(Spawner))
                    continue;

                if (!spawner.Running || spawner.TemplateEnabled)
                    continue;

                if (spawner.GetMobiles().Count < 1 || spawner.GetItems().Count > 0)
                    continue;

                if (spawner.Invulnerable)
                    continue;

                // Filter by both zone bounds AND map
                if (spawner.Map != zone.Map)
                    continue;

                if (zone.Contains(spawner.Location))
                    list.Add(spawner);
            }

            return list;
        }

        /// <summary>
        /// Where a contested spawner for this anchor goes: the nearest walkable spot connected to the anchor,
        /// else the anchor's own tile if it is walkable land. False: no valid spot, the guardian is skipped.
        /// </summary>
        private static bool TryGetPlacement(Spawner anchor, TerritoryZone zone, out Point3D loc)
        {
            if (TryFindGoodSpawnLocation(anchor, zone, out loc))
                return true;

            loc = anchor.Location;

            return !IsIllegalSpawnLocation(loc, zone.Map) && IsWalkableLand(zone.Map, loc);
        }

        /// <summary>
        /// A spot a land-walking creature can stand on: a surface, no blocking statics/items, not water,
        /// and a region that allows spawning.
        /// </summary>
        internal static bool IsWalkableLand(Map map, Point3D p)
        {
            if (map == null || map == Map.Internal)
                return false;

            return Utility.CanSpawnLandMobile(map, p, Utility.CanFitFlags.requireSurface) && !Utility.IsWater(map, p.X, p.Y, p.Z);
        }

        /// <summary>
        /// A human-style walker can get from 'from' to 'to'. ClearPathLand cannot test adjacent tiles
        /// (there are no path steps), so those are tested with a single movement step.
        /// </summary>
        internal static bool CanWalkTo(Point3D from, Point3D to, Map map)
        {
            int distance = Math.Max(Math.Abs(from.X - to.X), Math.Abs(from.Y - to.Y));

            if (distance == 0)
                return true;

            if (distance == 1)
            {
                var mo = new Server.Movement.MovementObject(from, to, map);
                return Server.Movement.Movement.CheckMovement(mo, from, Utility.GetDirection(from, to), out int newZ) &&
                       Math.Abs(newZ - to.Z) <= 16;
            }

            return Spawner.ClearPathLand(from, to, map);
        }

        /*
         * Mobile Placement — Two-Axis Movement Requirement
         *
         * Legend:
         *   C = candidate placement
         *   . = traversable neighbor
         *   # = blocked (wall, obstacle, etc.)
         *   ~ = water / impassable terrain
         *   ? = irrelevant
         *
         * ACCEPT — North and East open:
         *
         *     ?  .  ?
         *     #  C  .
         *     ?  #  ?
         *
         * ACCEPT — North/South and West open:
         *
         *     ?  .  ?
         *     .  C  #
         *     ?  .  ?
         *
         * REJECT — Trinsic walkway (moat and wall):
         *
         *     ?  .  ?
         *     ~  C  #
         *     ?  .  ?
         *
         * REJECT — East/West corridor:
         *
         *     ?  #  ?
         *     .  C  .
         *     ?  #  ?
         *
         * Rule:
         *   (CanMoveNorth || CanMoveSouth)
         *       &&
         *   (CanMoveEast || CanMoveWest)
         *
         * A valid placement must offer movement on BOTH axes.
         * Diagonal tiles do not satisfy either requirement.
         *
         * Why (11/4/2026): Trinsic has a one-tile-wide walkway running north/south between a wall and the moat, with no
         * floor art: it looks like water, but it is walkable, so IsWalkableLand and the path-back check accept it, and a
         * Warlord placed there has no lateral movement. "Traversable" is the game's own movement check from the candidate
         * (Movement.CheckMovement: elevation, statics, items, walls, water), never a test of the neighbor tile alone: the
         * walkway's east neighbor is the building's floor, walkable land, but behind the wall.
         * Used by Warlord placement (TerritoryDistrict.TryPlaceMiniBoss) and contested spawner placement (TryFindGoodSpawnLocation);
         * neither applies it to the fallback, the anchor's own tile. Not used by movement, pathfinding or Spawner.GetSpawnPosition.
         */
        internal static bool HasTwoAxisMovement(Map map, Point3D p)
        {
            return (CanStep(map, p, Direction.North) || CanStep(map, p, Direction.South))
                && (CanStep(map, p, Direction.East) || CanStep(map, p, Direction.West));
        }
        // a land walker standing at p can take one step in direction d (the movement check a creature's own step uses)
        private static bool CanStep(Map map, Point3D p, Direction d)
        {
            int x = p.X, y = p.Y;
            Server.Movement.Movement.Offset(d, ref x, ref y);
            Point3D to = new Point3D(x, y, p.Z);
            var mo = new Server.Movement.MovementObject(p, to, map);
            return Server.Movement.Movement.CheckMovement(mo, p, d, out int newZ);
        }
        private static bool TryFindGoodSpawnLocation(
            Spawner source,
            TerritoryZone zone,
            out Point3D location)
        {
            Map map = zone.Map;
            Point3D center = source.Location;
            const int maxRadius = 8;

            HashSet<Point2D> occupied = new HashSet<Point2D>();
            IPooledEnumerable eable = map.GetItemsInRange(center, maxRadius);

            foreach (Item item in eable)
            {
                if (item is Spawner spawner && !spawner.Deleted)
                    occupied.Add(new Point2D(spawner.X, spawner.Y));
            }

            eable.Free();

            for (int radius = 1; radius <= maxRadius; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                            continue;

                        int x = center.X + dx;
                        int y = center.Y + dy;

                        if (occupied.Contains(new Point2D(x, y)))
                            continue;

                        Point3D candidate = new Point3D(x, y, center.Z);

                        if (IsIllegalSpawnLocation(candidate, map))
                            continue;

                        int z = center.Z;

                        // land walkers only (the default CanSpawnMobile flags include canSwim), connected to the anchor
                        if (Utility.CanSpawnMobile(map, x, y, ref z, Utility.CanFitFlags.requireSurface))
                        {
                            Point3D spot = new Point3D(x, y, z);

                            // and room to move on both axes (never a one-tile corridor; HasTwoAxisMovement)
                            if (!Utility.IsWater(map, x, y, z) && CanWalkTo(spot, center, map) && HasTwoAxisMovement(map, spot))
                            {
                                location = spot;
                                return true;
                            }
                        }
                    }
                }
            }

            location = Point3D.Zero;
            return false;
        }

        private static bool IsIllegalSpawnLocation(Point3D loc, Map map)
        {
            if (map != Map.Felucca)
                return false;

            Point2D point = new Point2D(loc.X, loc.Y);

            if (Utility.WorldMap.AngelIsland.Contains(point))
                return true;

            return false;
        }

        private static int CalculateGuardianCount(Spawner anchor)
        {
            if (anchor.AvgPower <= 0)
                return 2;

            int count = (int)(anchor.AvgPower / 70.0);
            return Math.Clamp(count, 1, 6);
        }
    }
}