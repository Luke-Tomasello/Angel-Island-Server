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
 * BaseBoat.Movement.cs
 *
 * This file contains the boat movement pipeline (forward/back/sideways, turning) and the supporting
 * spatial machinery used to make movement decisions safely and consistently.
 *
 * Key design change:
 *   All boat spatial queries (occupied footprint, bounds, deck checks) are normalized through a single
 *   source of truth: BoatGeometry.
 *
 * Why BoatGeometry exists:
 *   Boats can have "holes" in their MultiComponentList where Tiles[x][y].Length == 0.
 *   These holes are visually empty but still fall inside the multi's bounding box, and legacy code paths
 *   could disagree on whether such cells were "part of the boat". That disagreement caused asymmetric
 *   movement behavior, CanFit inconsistencies, and edge/overlap cases where sea creatures could be
 *   scooped or carried onto the deck.
 *
 * BoatGeometry rules:
 *   - For collision, CanFit, and shove logic, we treat the ENTIRE multi bounds as occupied.
 *     (Dense footprint: holes are still considered blocked/owned by the boat for spatial reasoning.)
 *   - Deck detection remains surface-based and can be sparse.
 *     (OnDeck is about "walkable deck surface", not "within the boat footprint".)
 *
 * Shove model (driver for this refactor):
 *   Prior movement logic could resolve overlaps after the boat moved, allowing mobiles to end up inside
 *   the hull footprint and be "carried" by overlap resolution (notably sea creatures during sideways motion).
 *   The new model pre-computes the proposed next boat position, identifies mobiles inside the NEW occupied
 *   footprint, and proactively shoves eligible non-riders out of the way before committing the move.
 *
 * Rider preservation:
 *   Mobiles already within the OLD occupied footprint that are legitimate riders (players, pets/summons,
 *   hirelings, and true deck occupants) are never shoved and continue to ride with the boat as expected.
 *
 * Net result:
 *   Symmetric movement/turn behavior across all hull sizes and facings, consistent CanFit/deck semantics,
 *   and reliable pre-move shove that prevents sea-creature ramming/scooping without breaking passengers.
 */


/* Scripts/Multis/Boats/BaseBoat.Movement.cs
 * ChangeLog
 *  2/7/2026, Adam (Created)
 *      See above comment
 */
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Multis
{
    public abstract partial class BaseBoat
    {
        // Single source of truth for boat spatial queries (footprint/occupied/deck).
        // IMPORTANT: Boats can have "holes" in their MultiComponentList where Tiles[x][y].Length == 0.
        // For collision, shove, and CanFit logic we treat the ENTIRE multi bounds as occupied.
        // Deck detection remains surface-based and can still be sparse.
        private sealed class BoatGeometry
        {
            public readonly MultiComponentList Mcl;
            public readonly Point3D BoatLoc;
            public readonly Direction Facing;

            public readonly Rectangle2D BoundsWorld;

            // Dense footprint: all cells inside MCL bounds (includes holes).
            public readonly HashSet<Point2D> FootprintCellsWorld;

            // Occupied cells: footprint + component anchor cells (hold/tiller/planks).
            public readonly HashSet<Point2D> OccupiedCellsWorld;

            // Best surface offset Z per cell (relative to boat origin). Used by IsOnDeck.
            private readonly Dictionary<Point2D, int> _bestSurfaceOffsetZWorld;

            // Strict deck cells: surface, not impassable, not roof, component Z == 0.
            public readonly HashSet<Point2D> StrictDeckCellsWorld;

            // Strict interior deck cells: strict deck + 4-neighbor strict deck.
            public readonly HashSet<Point2D> StrictInteriorDeckCellsWorld;

            // Center used for sidedness tests (world coords).
            public readonly Point2D CenterWorld;

            public BoatGeometry(BaseBoat boat, Point3D boatLoc, Direction facing)
            {
                if (boat == null)
                    throw new ArgumentNullException("boat");

                BoatLoc = boatLoc;
                Facing = (Direction)((int)facing & 0x7);

                Mcl = boat.GetComponents(Facing);

                if (Mcl == null)
                    throw new ArgumentNullException("Mcl");

                BoundsWorld = new Rectangle2D(
                    boatLoc.X + Mcl.Min.X,
                    boatLoc.Y + Mcl.Min.Y,
                    Mcl.Width,
                    Mcl.Height);

                CenterWorld = new Point2D(
                    boatLoc.X + Mcl.Min.X + Mcl.Center.X,
                    boatLoc.Y + Mcl.Min.Y + Mcl.Center.Y);

                FootprintCellsWorld = new HashSet<Point2D>();
                OccupiedCellsWorld = new HashSet<Point2D>();

                _bestSurfaceOffsetZWorld = new Dictionary<Point2D, int>();
                StrictDeckCellsWorld = new HashSet<Point2D>();
                StrictInteriorDeckCellsWorld = new HashSet<Point2D>();

                BuildDenseFootprint();
                BuildOccupiedWithAnchors(boat);
                BuildDeckSurfaces();
                BuildInteriorDeck();
            }

            private void BuildDenseFootprint()
            {
                for (int x = 0; x < Mcl.Width; x++)
                {
                    for (int y = 0; y < Mcl.Height; y++)
                    {
                        int wx = BoatLoc.X + Mcl.Min.X + x;
                        int wy = BoatLoc.Y + Mcl.Min.Y + y;

                        Point2D p = new Point2D(wx, wy);

                        FootprintCellsWorld.Add(p);
                        OccupiedCellsWorld.Add(p);
                    }
                }
            }

            private void BuildOccupiedWithAnchors(BaseBoat boat)
            {
                // Hold / tiller / planks should behave like part of the boat for shove/collision.
                Point2D hold = boat.GetHoldCellAt(BoatLoc, Facing);
                Point2D tiller = boat.GetTillerCellAt(BoatLoc, Facing);

                OccupiedCellsWorld.Add(hold);
                OccupiedCellsWorld.Add(tiller);

                boat.GetPlankCellsAt(BoatLoc, Facing, out Point2D port, out Point2D star);
                OccupiedCellsWorld.Add(port);
                OccupiedCellsWorld.Add(star);
            }

            private void BuildDeckSurfaces()
            {
                // Build best-surface-z map and strict deck cells in one pass.
                for (int relX = 0; relX < Mcl.Width; relX++)
                {
                    for (int relY = 0; relY < Mcl.Height; relY++)
                    {
                        StaticTile[] tiles = Mcl.Tiles[relX][relY];

                        if (tiles == null || tiles.Length == 0)
                            continue;

                        bool foundSurface = false;
                        int bestSurfaceOffsetZ = int.MinValue;

                        bool strictDeck = false;

                        for (int i = 0; i < tiles.Length; i++)
                        {
                            StaticTile t = tiles[i];

                            int itemID = t.ID & 0x3FFF;
                            ItemData id = TileData.ItemTable[itemID];

                            if ((id.Flags & TileFlag.Impassable) != 0)
                                continue;

                            if ((id.Flags & TileFlag.Roof) != 0)
                                continue;

                            if ((id.Flags & TileFlag.Surface) == 0)
                                continue;

                            // Best surface for IsOnDeck uses max Z among valid surfaces.
                            if (t.Z > bestSurfaceOffsetZ)
                            {
                                bestSurfaceOffsetZ = t.Z;
                                foundSurface = true;
                            }

                            // Strict deck requires deck-level components (Z == 0).
                            if (t.Z == 0)
                                strictDeck = true;
                        }

                        int wx = BoatLoc.X + Mcl.Min.X + relX;
                        int wy = BoatLoc.Y + Mcl.Min.Y + relY;
                        Point2D w = new Point2D(wx, wy);

                        if (foundSurface)
                            _bestSurfaceOffsetZWorld[w] = bestSurfaceOffsetZ;

                        if (strictDeck)
                            StrictDeckCellsWorld.Add(w);
                    }
                }
            }

            private void BuildInteriorDeck()
            {
                foreach (Point2D p in StrictDeckCellsWorld)
                {
                    if (StrictDeckCellsWorld.Contains(new Point2D(p.X - 1, p.Y)) &&
                        StrictDeckCellsWorld.Contains(new Point2D(p.X + 1, p.Y)) &&
                        StrictDeckCellsWorld.Contains(new Point2D(p.X, p.Y - 1)) &&
                        StrictDeckCellsWorld.Contains(new Point2D(p.X, p.Y + 1)))
                    {
                        StrictInteriorDeckCellsWorld.Add(p);
                    }
                }
            }

            public bool IsFootprintCell(int x, int y)
            {
                return FootprintCellsWorld.Contains(new Point2D(x, y));
            }

            public bool IsOccupiedCell(int x, int y)
            {
                return OccupiedCellsWorld.Contains(new Point2D(x, y));
            }

            public bool IsStrictDeckCell(int x, int y)
            {
                return StrictDeckCellsWorld.Contains(new Point2D(x, y));
            }

            public bool IsStrictInteriorDeckCell(int x, int y)
            {
                return StrictInteriorDeckCellsWorld.Contains(new Point2D(x, y));
            }

            public bool TryGetBestSurfaceOffsetZ(int x, int y, out int offsetZ)
            {
                return _bestSurfaceOffsetZWorld.TryGetValue(new Point2D(x, y), out offsetZ);
            }

            public bool IsOnDeck(Point3D loc, int boatZ)
            {
                if (!TryGetBestSurfaceOffsetZ(loc.X, loc.Y, out int offsetZ))
                    return false;

                int deckZWorld = boatZ + offsetZ;

                // IMPORTANT: tolerate small Z drift for riders (step logic can be +/- 2)
                return (loc.Z >= deckZWorld - 2 && loc.Z <= deckZWorld + 2);
            }

            // Sidedness relative to movement direction (lx,ly = left perp unit vector).
            public bool IsOnLeftSide(Mobile m, int lx, int ly)
            {
                if (m == null)
                    return false;

                int vx = m.X - CenterWorld.X;
                int vy = m.Y - CenterWorld.Y;

                int dot = (vx * lx) + (vy * ly);

                // >= 0 means left side relative to movement direction
                return (dot >= 0);
            }
        }

        private BoatGeometry BuildGeometry(Point3D boatLoc, Direction facing)
        {
            return new BoatGeometry(this, boatLoc, facing);
        }

        // Helpers that compute anchor cells for an arbitrary boat location.
        private Point2D GetHoldCellAt(Point3D boatLoc, Direction facing)
        {
            GetFacingOffset(facing, out int fx, out int fy);

            int hx = boatLoc.X + (fx * HoldDistance);
            int hy = boatLoc.Y + (fy * HoldDistance);

            return new Point2D(hx, hy);
        }

        private Point2D GetTillerCellAt(Point3D boatLoc, Direction facing)
        {
            GetFacingOffset(facing, out int fx, out int fy);

            int tx = boatLoc.X + (fx * TillerManDistance);
            int ty = boatLoc.Y + (fy * TillerManDistance);

            // Preserve your North special-case (+1 X)
            if (((Direction)((int)facing & 0x7)) == Direction.North)
                tx += 1;

            return new Point2D(tx, ty);
        }

        private void GetPlankCellsAt(Point3D boatLoc, Direction facing, out Point2D portCell, out Point2D starboardCell)
        {
            // Normalize to pure cardinal (strip Running, etc.)
            Direction dir = (Direction)((int)facing & 0x7);

            // Convert normalized facing to quarter turns from North.
            int count = FacingToQuarterTurnsFromNorth(dir);

            // Rotate per-hull local offsets into world-facing local offsets.
            Point2D port = RotateOffset(PortOffset, count);
            Point2D star = RotateOffset(StarboardOffset, count);

            // NOTE (TEMP / Large+LargeDragon fix):
            // Large hulls exhibit an East/West origin bias relative to (boatLoc.X, boatLoc.Y) that shifts both planks
            // inboard by 2 tiles. Apply a symmetric X compensation for E/W only.
            //
            // WARNING: Intentionally narrow in scope until we anchor plank cells from multi/component geometry.
            bool isLargeHull = (this is LargeBoat) || (this is LargeDragonBoat);

            if (isLargeHull)
            {
                if (dir == Direction.West)
                {
                    port = new Point2D(port.X - 2, port.Y);
                    star = new Point2D(star.X - 2, star.Y);
                }
                else if (dir == Direction.East)
                {
                    port = new Point2D(port.X + 2, port.Y);
                    star = new Point2D(star.X + 2, star.Y);
                }
            }

            // Translate local offsets to world cells.
            portCell = new Point2D(boatLoc.X + port.X, boatLoc.Y + port.Y);
            starboardCell = new Point2D(boatLoc.X + star.X, boatLoc.Y + star.Y);
        }

        public bool SetFacing(Direction facing)
        {
            if (CheckDecay())
                return false;

            if (Map != null && Map != Map.Internal)
            {
                if (!CanFit(Location, Map, facing))
                    return false;

                if (!ClearTurnPath(facing, Map))
                    return false;
            }

            Direction oldFacing = m_Facing;

            // Decisions about who "rides" the turn must be made using the OLD geometry.
            BoatGeometry oldGeo = BuildGeometry(this.Location, oldFacing);

            m_Facing = facing;

            ArrayList toMove = new ArrayList();

            IPooledEnumerable eable = Map.GetObjectsInBounds(oldGeo.BoundsWorld);

            foreach (object o in eable)
            {
                if (o == this || o is TillerMan || o is Hold || o is Plank)
                    continue;

                if (o is Item)
                {
                    Item item = (Item)o;

                    // 1/16/24, Yoar: Allow boat movement over static water items
                    if (!item.Movable && item.Z == Z && item.ItemID >= 0x1796 && item.ItemID <= 0x17B2)
                        continue;

                    if (item.Visible && item.Z >= Z && item.Z < Z + Depth && oldGeo.IsOccupiedCell(item.X, item.Y))
                        toMove.Add(item);
                }
                else if (o is Mobile)
                {
                    Mobile m = (Mobile)o;

                    // Players (and owned/operated mobiles) ride if within occupied space.
                    if (m.Player || IsPlayerOwnedOrOperated(m))
                    {
                        if (oldGeo.IsOccupiedCell(m.X, m.Y))
                        {
                            toMove.Add(m);
                            m.Direction = (Direction)((int)m.Direction - (int)oldFacing + (int)facing);
                        }

                        continue;
                    }

                    // Wild/non-owned mobiles only ride if truly on interior deck.
                    if (oldGeo.IsStrictInteriorDeckCell(m.X, m.Y))
                    {
                        toMove.Add(m);
                        m.Direction = (Direction)((int)m.Direction - (int)oldFacing + (int)facing);
                    }
                }
            }

            eable.Free();

            // Rotate all riders around the boat origin.
            int oldQ = FacingToQuarterTurnsFromNorth(oldFacing);
            int newQ = FacingToQuarterTurnsFromNorth(facing);
            int turns = (newQ - oldQ) & 0x3;

            for (int i = 0; i < toMove.Count; ++i)
            {
                object? o = toMove[i];

                if (o is Item)
                {
                    Item item = (Item)o;

                    // 1/16/24, Yoar: Allow boat movement over static water items
                    if (!item.Movable && item.Z == Z && item.ItemID >= 0x1796 && item.ItemID <= 0x17B2)
                        continue;

                    Point2D rel = new Point2D(item.X - X, item.Y - Y);
                    Point2D rot = RotateOffset(rel, turns);

                    item.Location = new Point3D(X + rot.X, Y + rot.Y, item.Z);
                }
                else if (o is Mobile)
                {
                    Mobile m = (Mobile)o;

                    Point2D rel = new Point2D(m.X - X, m.Y - Y);
                    Point2D rot = RotateOffset(rel, turns);

                    m.Location = new Point3D(X + rot.X, Y + rot.Y, m.Z);
                }
            }

            switch (facing)
            {
                case Direction.North: ItemID = NorthID; break;
                case Direction.East: ItemID = EastID; break;
                case Direction.South: ItemID = SouthID; break;
                case Direction.West: ItemID = WestID; break;
            }

            RefreshComponents();

            OnAfterSetFacing(oldFacing);
            return true;
        }
        public virtual void OnAfterSetFacing(Direction oldFacing)
        {
            UpdateComponents();
        }
        public bool Move(Direction dir, int speed, bool message)
        {
            Map map = Map;

            if (map == null || Deleted || CheckDecay())
                return false;

            if (m_Anchored)
            {
                if (message && m_TillerMan != null)
                    m_TillerMan.Say(501419); // Ar, the anchor is down sir!

                return false;
            }

            int rx = 0, ry = 0;
            Direction moveDir = (Direction)(((int)m_Facing + (int)dir) & 0x7);
            Movement.Movement.Offset(moveDir, ref rx, ref ry);

            for (int i = 1; i <= speed; ++i)
            {
                Point3D testLoc = new Point3D(X + (i * rx), Y + (i * ry), Z);

                // NEW: shove mobs out of the delta footprint for this step
                if (!ClearPath(testLoc, moveDir, map))
                {
                    if (message && m_TillerMan != null)
                        m_TillerMan.Say(501424); // Ar, we've stopped sir.

                    return false;
                }

                if (!CanFit(new Point3D(X + (i * rx), Y + (i * ry), Z), Map))
                {
                    if (i == 1)
                    {
                        ProcessSinkables();

                        if (message && m_TillerMan != null)
                        {
                            if (m_Sinkables.Count != 0)
                                m_TillerMan.Say(false, "Ar, we've hit something cap'n!");
                            else
                                m_TillerMan.Say(501424); // Ar, we've stopped sir.
                        }

                        return false;
                    }

                    speed = i - 1;
                    break;
                }
            }

            int xOffset = speed * rx;
            int yOffset = speed * ry;

            int newX = X + xOffset;
            int newY = Y + yOffset;

            if (Core.RuleSets.AnyAIShardRules() && Utility.WorldMap.AngelIsland.Contains(new Point2D(newX, newY)))
            {
                if (message && m_TillerMan != null)
                    m_TillerMan.Say(false, "Ar, I'll not go any nearer to that Angel Island.");

                return false;
            }

            Rectangle2D[] wrap = BaseBoat.GetWrapFor(this.Location, map);

            for (int i = 0; i < wrap.Length; ++i)
            {
                Rectangle2D rect = wrap[i];

                if (rect.Contains(new Point2D(X, Y)) && !rect.Contains(new Point2D(newX, newY)))
                {
                    if (newX < rect.X)
                        newX = rect.X + rect.Width - 1;
                    else if (newX >= rect.X + rect.Width)
                        newX = rect.X;

                    if (newY < rect.Y)
                        newY = rect.Y + rect.Height - 1;
                    else if (newY >= rect.Y + rect.Height)
                        newY = rect.Y;

                    for (int j = 1; j <= speed; ++j)
                    {
                        Point3D testLoc = new Point3D(newX + (j * rx), newY + (j * ry), Z);

                        if (!ClearPath(testLoc, moveDir, map))
                        {
                            if (message && m_TillerMan != null)
                                m_TillerMan.Say(501424); // Ar, we've stopped sir.

                            return false;
                        }

                        if (!CanFit(new Point3D(newX + (j * rx), newY + (j * ry), Z), Map))
                        {
                            if (message && m_TillerMan != null)
                                m_TillerMan.Say(501424); // Ar, we've stopped sir.

                            return false;
                        }
                    }

                    xOffset = newX - X;
                    yOffset = newY - Y;
                }
            }

            MultiComponentList mcl = GetComponents();

            ArrayList toMove = new ArrayList();

            IPooledEnumerable eable = map.GetObjectsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));

            foreach (object o in eable)
            {
                if (o != this && !(o is TillerMan || o is Hold || o is Plank))
                    toMove.Add(o);
            }

            eable.Free();

            HashSet<Point3D> stackLocs = new HashSet<Point3D>(); // stack items at these locations

            for (int i = 0; i < toMove.Count; ++i)
            {
                object? o = toMove[i];

                if (o is Item)
                {
                    Item item = (Item)o;

                    // 1/16/24, Yoar: Allow boat movement over static water items
                    if (!item.Movable && item.Z == Z && item.ItemID >= 0x1796 && item.ItemID <= 0x17B2)
                        continue;

                    if (Contains(item) && item.Visible && item.Z >= Z && item.Z < Z + Depth)
                    {
                        Point3D newLoc = new Point3D(item.X + xOffset, item.Y + yOffset, item.Z);

                        item.Location = newLoc;

                        if (Array.IndexOf(m_AutoStack, item.GetType()) != -1)
                            stackLocs.Add(newLoc);
                    }
                }
                else if (o is Mobile)
                {
                    Mobile m = (Mobile)o;

                    // Players and owned/operated mobiles ride by footprint.
                    if (ShouldRideByFootprint(this, m))
                    {
                        m.Location = new Point3D(m.X + xOffset, m.Y + yOffset, m.Z);
                        continue;
                    }

                    // Wild/non-owned mobiles only ride if truly on deck (prevents sea creatures being carried)
                    if (IsOnDeck(m.Location))
                        m.Location = new Point3D(m.X + xOffset, m.Y + yOffset, m.Z);
                }
            }

            Location = new Point3D(X + xOffset, Y + yOffset, Z);

            foreach (Point3D loc in stackLocs)
                ItemStacker.StackAt(loc, map, m_AutoStack);

            return true;
        }

        private bool ClearTurnPath(Direction newFacing, Map map)
        {
            bool debug = DebugShove;
            Action<string> log = (s) => { if (debug) Console.WriteLine("[ClearTurnPath] " + s); };

            Direction oldFacing = m_Facing;

            BoatGeometry oldGeo = BuildGeometry(this.Location, oldFacing);
            BoatGeometry newGeo = BuildGeometry(this.Location, newFacing);

            // delta = new occupied - old occupied
            HashSet<Point2D> deltaSet = new HashSet<Point2D>();

            foreach (Point2D p in newGeo.OccupiedCellsWorld)
            {
                if (!oldGeo.OccupiedCellsWorld.Contains(p))
                    deltaSet.Add(p);
            }

            if (deltaSet.Count == 0)
                return true;

            Rectangle2D bounds = GetBounds(deltaSet);

            log(string.Format(
                "old={0} new={1} delta={2} bounds={3} loc=({4},{5},{6})",
                oldFacing, newFacing, deltaSet.Count, bounds, X, Y, Z));

            IPooledEnumerable eable = map.GetMobilesInBounds(bounds);

            try
            {
                foreach (Mobile m in eable)
                {
                    if (m == null || m.Deleted)
                        continue;

                    Point2D mp = new Point2D(m.X, m.Y);

                    if (!deltaSet.Contains(mp))
                        continue;

                    log(string.Format(
                        "delta-hit: mob={0}({1}) serial=0x{2:X} loc=({3},{4},{5}) player={6}",
                        m.Name, m.GetType().Name, m.Serial.Value, m.X, m.Y, m.Z, m.Player));

                    // Do not shove anything already in the boat occupied space as currently defined
                    if (oldGeo.IsOccupiedCell(m.X, m.Y))
                    {
                        log("skip: already in old occupied");
                        continue;
                    }

                    // Players: hard stop
                    if (m.Player)
                    {
                        log("FAIL: player in delta => hard stop");
                        return false;
                    }

                    if (!ShouldShove(m))
                    {
                        log("skip: ShouldShove=false");
                        continue;
                    }

                    // For turning, choose a deterministic "push axis".
                    // Using the proposed newFacing works well in practice.
                    if (!TryShoveAside(m, newFacing, this.Location, newGeo, map, maxRadius: 6, out string reason))
                    {
                        log("FAIL: shove failed");
                        return false;
                    }

                    if (debug)
                    {
                        bool stillInNew = newGeo.IsOccupiedCell(m.X, m.Y);
                        bool stillInOld = oldGeo.IsOccupiedCell(m.X, m.Y);
                        bool nowOnDeck = IsOnDeck(m.Location);

                        log(string.Format(
                            "post-shove: loc=({0},{1},{2}) inNew={3} inOld={4} IsOnDeck={5}",
                            m.X, m.Y, m.Z, stillInNew, stillInOld, nowOnDeck));
                    }

                    log("shove: success");
                }
            }
            finally
            {
                eable.Free();
            }

            return true;
        }
        // Helper: player-owned/operated mobiles should ride even if deck detection is imperfect.
        // This fixes pets/summons being left behind while still preventing wild/sea creatures from being carried.
        private static bool IsPlayerOwnedOrOperated(Mobile m)
        {
            if (m == null || m.Player)
                return false;

            BaseCreature? bc = m as BaseCreature;
            if (bc == null)
                return false;

            // Pets (controlled) and summons (summoned) are "owned/operated"
            if (bc.Controlled && bc.ControlMaster != null)
                return true;

            if (bc.Summoned && bc.SummonMaster != null)
                return true;

            return false;
        }

        // Helper: derive a readable rider type for logs/debug if you ever need it
        private static bool ShouldRideByFootprint(BaseBoat boat, Mobile m)
        {
            if (boat == null || m == null)
                return false;

            // Players always ride if within boat footprint
            if (m.Player)
                return boat.Contains(m);

            // Player-owned/operated non-players (pets/summons) also ride by footprint
            if (IsPlayerOwnedOrOperated(m))
                return boat.Contains(m);

            return false;
        }

        private bool TryShoveAside(Mobile m, Direction moveDir, Point3D newBoatLoc, BoatGeometry newGeo, Map map, int maxRadius, out string reason)
        {
            int tried = 0;
            int failFootprint = 0;
            int failWater = 0;
            int failSpawn = 0;

            GetForwardOffset(moveDir, out int fx, out int fy);
            GetPerpOffsets(moveDir, out int lx, out int ly, out int rx, out int ry);

            int startX = m.X;
            int startY = m.Y;

            int z = m.Z;

            // Determine which side the mob is on, and only shove along THAT perp.
            int px, py;

            if (newGeo.IsOnLeftSide(m, lx, ly))
            {
                px = lx; py = ly; // shove stays on left side
            }
            else
            {
                px = rx; py = ry; // shove stays on right side
            }

            // Forward-only search:
            // Try positions in front of the mob (same direction the boat is moving),
            // with small same-side lateral offsets to find water corridors near shore.
            for (int f = 1; f <= maxRadius; f++)
            {
                // Try straight forward first
                if (TryPlaceAt(m, startX + (fx * f), startY + (fy * f), z, newBoatLoc, newGeo, map,
                    ref tried, ref failFootprint, ref failWater, ref failSpawn))
                {
                    reason = "placed via forward shove";
                    return true;
                }

                // Then forward + same-side lateral fan-out
                for (int s = 1; s <= maxRadius; s++)
                {
                    if (TryPlaceAt(m,
                        startX + (fx * f) + (px * s),
                        startY + (fy * f) + (py * s),
                        z, newBoatLoc, newGeo, map,
                        ref tried, ref failFootprint, ref failWater, ref failSpawn))
                    {
                        reason = "placed via forward+same-side shove";
                        return true;
                    }
                }
            }

            reason = string.Format(
                "no legal forward placement (tried={0}, footprint={1}, water={2}, spawn={3})",
                tried, failFootprint, failWater, failSpawn);

            return false;
        }


        private bool TryPlaceAt(Mobile m, int x, int y, int z, Point3D newBoatLoc, BoatGeometry newGeo, Map map, ref int tried, ref int failFootprint, ref int failWater, ref int failSpawn)
        {
            tried++;

            if (newGeo.IsOccupiedCell(x, y))
            {
                failFootprint++;
                return false;
            }

            // Prefer the caller-provided Z first (usually the mob's current Z, e.g. -5 for deep water).
            // Fallback to map average Z if that fails (useful near shore where water Z may differ).
            int preferredZ = z;
            int avgZ = map.GetAverageZ(x, y);

            // Try preferred Z
            if (Utility.IsWater(map, x, y, preferredZ) &&
                Utility.CanSpawnWaterMobile(map, new Point3D(x, y, preferredZ)))
            {
                m.Location = new Point3D(x, y, preferredZ);
                return true;
            }

            // Try average Z (only if different, to avoid redundant work)
            if (avgZ != preferredZ)
            {
                if (!Utility.IsWater(map, x, y, avgZ))
                {
                    failWater++;
                    return false;
                }

                if (!Utility.CanSpawnWaterMobile(map, new Point3D(x, y, avgZ)))
                {
                    failSpawn++;
                    return false;
                }

                m.Location = new Point3D(x, y, avgZ);
                return true;
            }

            // If avgZ == preferredZ, we already tried it and failed.
            failSpawn++;
            return false;
        }

        private bool ClearPath(Point3D newBoatLoc, Direction moveDir, Map map)
        {
            bool debug = DebugShove;

            Action<string> log = (s) => { if (debug) Console.WriteLine("[ClearPath] " + s); };

            Direction dir = (Direction)((int)moveDir & 0x7);

            BoatGeometry oldGeo = BuildGeometry(this.Location, this.Facing);
            BoatGeometry newGeo = BuildGeometry(newBoatLoc, this.Facing);

            Rectangle2D bounds = newGeo.BoundsWorld;

            if (debug)
                log(string.Format(
                    "newBoatLoc={0} moveDir={1} oldOcc={2} newOcc={3} bounds={4}",
                    newBoatLoc, dir, oldGeo.OccupiedCellsWorld.Count, newGeo.OccupiedCellsWorld.Count, bounds));

            IPooledEnumerable eable = map.GetMobilesInBounds(bounds);

            try
            {
                foreach (Mobile m in eable)
                {
                    if (m == null || m.Deleted)
                        continue;

                    // Only care if they are in the NEW occupied space.
                    if (!newGeo.IsOccupiedCell(m.X, m.Y))
                        continue;

                    if (debug)
                        log(string.Format(
                            "new-occ-hit: mob={0}({1}) serial=0x{2:X} loc=({3},{4},{5}) player={6} type={7}",
                            m.Name, m.GetType().Name, m.Serial.Value, m.X, m.Y, m.Z, m.Player, m.GetType().FullName));

                    bool inOldOcc = oldGeo.IsOccupiedCell(m.X, m.Y);

                    // Rider exemption (prevents leaving pets behind):
                    // If they were already within the old footprint, treat players and controlled mobs as riders.
                    // Wild mobs only count as riders if truly on deck.
                    if (inOldOcc)
                    {
                        if (m.Player)
                        {
                            if (debug) log("skip: rider (player in old occupied)");
                            continue;
                        }

                        if (IsPlayerOwnedOrOperated(m))
                        {
                            if (debug) log("skip: rider (owned/controlled in old occupied)");
                            continue;
                        }

                        if (IsOnDeck(m.Location))
                        {
                            if (debug) log("skip: rider (ondeck in old occupied)");
                            continue;
                        }

                        if (debug) log("note: in old occupied but not rider; will attempt shove");
                    }

                    // Players not already riding are a hard stop.
                    if (m.Player)
                    {
                        if (debug) log("FAIL: player within new occupied (not rider) => hard stop");
                        return false;
                    }

                    if (!ShouldShove(m))
                    {
                        if (debug) log("skip: ShouldShove=false");
                        continue;
                    }

                    if (!TryShoveAside(m, moveDir, newBoatLoc, newGeo, map, 6, out string shoveReason))
                    {
                        if (debug) log("FAIL: shove failed => " + shoveReason);
                        return false;
                    }

                    if (debug)
                    {
                        bool stillInNew = newGeo.IsOccupiedCell(m.X, m.Y);
                        bool stillInOld = oldGeo.IsOccupiedCell(m.X, m.Y);
                        bool nowOnDeck = IsOnDeck(m.Location);

                        log(string.Format(
                            "post-shove: loc=({0},{1},{2}) inNew={3} inOld={4} IsOnDeck={5}",
                            m.X, m.Y, m.Z, stillInNew, stillInOld, nowOnDeck));
                    }

                    // Do not allow the move if we failed to clear NEW occupancy.
                    if (newGeo.IsOccupiedCell(m.X, m.Y))
                    {
                        if (debug) log("FAIL: mob still within new occupied after shove => hard stop");
                        return false;
                    }

                    if (debug) log("shove: success");
                }
            }
            finally
            {
                eable.Free();
            }

            return true;
        }

        private bool ShouldShove(Mobile m)
        {
            return true;
        }
        private static void GetForwardOffset(Direction moveDir, out int fx, out int fy)
        {
            fx = fy = 0;

            // Normalize to 0-7
            Direction dir = (Direction)((int)moveDir & 0x7);

            Movement.Movement.Offset(dir, ref fx, ref fy);
        }
        private static void GetPerpOffsets(Direction moveDir, out int lx, out int ly, out int rx, out int ry)
        {
            // Normalize to pure direction (strip running / flags)
            Direction dir = (Direction)((int)moveDir & 0x7);

            // Left is 90 degrees counter-clockwise, right is clockwise
            Direction leftDir = (Direction)(((int)dir + 6) & 0x7);
            Direction rightDir = (Direction)(((int)dir + 2) & 0x7);

            lx = ly = rx = ry = 0;

            Movement.Movement.Offset(leftDir, ref lx, ref ly);
            Movement.Movement.Offset(rightDir, ref rx, ref ry);
        }

        public bool CanFit(Point3D p, Map map)
        {
            return CanFit(p, map, m_Facing);
        }

        public bool CanFit(Point3D p, Map map, Direction facing)
        {
            if (map == null || map == Map.Internal || Deleted || CheckDecay())
                return false;

            // Geometry at proposed position/facing (dense footprint + anchors).
            BoatGeometry newGeo = BuildGeometry(p, facing);

            // Geometry for current boat state (used to ignore self-overlap).
            BoatGeometry selfGeo = BuildGeometry(this.Location, m_Facing);

            // Primary water/solid checks across occupied cells (includes holes and anchors).
            foreach (Point2D cell in newGeo.OccupiedCellsWorld)
            {
                int tx = cell.X;
                int ty = cell.Y;

                // Ignore cells already occupied by our current placement (self overlap).
                if (selfGeo.IsOccupiedCell(tx, ty))
                    continue;

                #region World Zone
                if (WorldZone.IsInside(this.Location, this.Map) && WorldZone.IsOutside(new Point3D(tx, ty, 0), map))
                    return false;
                #endregion

                LandTile landTile = map.Tiles.GetLandTile(tx, ty);
                StaticTile[] tiles = map.Tiles.GetStaticTiles(tx, ty, true);

                bool hasWater = false;

                if (landTile.Z == p.Z && ((landTile.ID >= 168 && landTile.ID <= 171) || (landTile.ID >= 310 && landTile.ID <= 311)))
                    hasWater = true;

                for (int i = 0; i < tiles.Length; ++i)
                {
                    StaticTile tile = tiles[i];

                    // 11/12/22, Adam: RunUO2.6 changed the notion of what a water tile is.
                    bool isWater = (tile.ID >= 0x1796 && tile.ID <= 0x17B2);

                    if (tile.Z == p.Z && isWater)
                        hasWater = true;
                    else if (tile.Z >= p.Z && tile.Z < p.Z + Depth && !isWater)
                        return false;
                }

                // 1/16/24, Yoar: Allow boat movement over static water items
                foreach (Item item in map.GetItemsInRange(new Point3D(tx, ty, p.Z), 0))
                {
                    if (item.Movable)
                        continue;

                    // 3/23/2024, Adam: Allow boats to move over these objects (spawners, and controllers)
                    if (boat_ignore_types.Contains(item.GetType()))
                        continue;

                    bool isWater = (item.ItemID >= 0x1796 && item.ItemID <= 0x17B2);

                    if (item.Z == p.Z && isWater)
                        hasWater = true;
                    else if (item.Z >= p.Z && item.Z < p.Z + Depth && !isWater)
                        return false;
                }

                if (!hasWater)
                    return false;
            }

            m_Sinkables.Clear();

            // Secondary: block on non-water, non-sinkable visible items within occupied cells and z range.
            IPooledEnumerable eable = map.GetItemsInBounds(newGeo.BoundsWorld);

            foreach (Item item in eable)
            {
                if (item.ItemID >= 0x4000 || item.Z + (IsSinkable(item) ? GetClearance(item) - 1 : 0) < p.Z || item.Z >= p.Z + Depth || !item.Visible)
                    continue;

                // 1/16/24, Yoar: Allow boat movement over static water items
                if (!item.Movable && item.Z == p.Z && item.ItemID >= 0x1796 && item.ItemID <= 0x17B2)
                    continue;

                // Ignore items that are not in our occupied cells.
                if (!newGeo.IsOccupiedCell(item.X, item.Y))
                    continue;

                // Ignore self overlap.
                if (selfGeo.IsOccupiedCell(item.X, item.Y))
                    continue;

                if (item is Arrow || item is Bolt || item is Blood)
                    continue;

                if (IsSinkable(item))
                {
                    m_Sinkables.Add(item);
                    continue;
                }

                eable.Free();
                return false;
            }

            eable.Free();

            if (!CheckSinkables())
                return false;

            return true;
        }

        public MultiComponentList GetComponents()
        {
            return GetComponents(m_Facing);
        }

        public virtual MultiComponentList GetComponents(Direction facing)
        {
            return MultiData.GetComponents(GetItemID(facing));
        }

        public override bool Contains(int x, int y)
        {
            if (Deleted)
                return false;

            BoatGeometry geo = BuildGeometry(this.Location, m_Facing);

            return geo.IsOccupiedCell(x, y);
        }

        // Returns the world cell of the hold anchor for a given facing
        private Point2D GetHoldCell(Direction facing)
        {
            return GetHoldCellAt(this.Location, facing);
        }

        // Returns the world cell of the tillerman anchor for a given facing
        private Point2D GetTillerCell(Direction facing)
        {
            return GetTillerCellAt(this.Location, facing);
        }

        // Returns the world cells for planks anchors for a given facing
        private void GetPlankCells(Direction facing, out Point2D portCell, out Point2D starboardCell)
        {
            GetPlankCellsAt(this.Location, facing, out portCell, out starboardCell);
        }
        /// <summary>
        /// Checks if a given mobile is on the deck of this boat
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public bool IsOnDeck(Mobile m)
        {
            if (m == null || m.Deleted || m.Map != this.Map)
                return false;
            return IsOnDeck(m.Location);
        }

        /// <summary>
        /// Checks if a given item is on the deck of this boat
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsOnDeck(Item item)
        {
            if (item == null || item.Deleted || item.Map != this.Map || item.Parent != null)
                return false;
            return IsOnDeck(item.Location);
        }

        /// <summary>
        /// Checks if a given location is on the deck of this boat
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public bool IsOnDeck(Point3D loc)
        {
            if (Deleted || Map == null || Map == Map.Internal)
                return false;

            BoatGeometry geo = BuildGeometry(this.Location, m_Facing);

            return geo.IsOnDeck(loc, this.Z);
        }


        // "Interior deck" means this cell and its 4-neighbors are deck.
        // This intentionally excludes the outer ring, which players cannot truly occupy
        // due to collision and edge geometry. Use this for "NPC rides during turn".
        /// <summary>
        /// Retuns ArrayList of Moblies currently on the deck
        /// </summary>
        /// <returns></returns>
        public ArrayList GetMobilesOnDeck()
        {
            ArrayList results = new ArrayList();

            if (Deleted || Map == null || Map == Map.Internal)
                return results;

            BoatGeometry geo = BuildGeometry(this.Location, m_Facing);

            foreach (Mobile m in Map.GetMobilesInBounds(geo.BoundsWorld))
            {
                if (IsOnDeck(m))
                    results.Add(m);
            }

            return results;
        }
        private static int FacingToQuarterTurnsFromNorth(Direction facing)
        {
            // Normalize and map cardinal facings only.
            // If diagonals ever appear here, clamp to nearest cardinal or handle explicitly.
            Direction dir = (Direction)((int)facing & 0x7);

            switch (dir)
            {

                case Direction.North: return 0;
                case Direction.East: return 1;
                case Direction.South: return 2;
                case Direction.West: return 3;

                default:
                    // Fallback: approximate by shifting to nearest cardinal
                    // East(0), South(2), West(4), North(6) in RunUO direction space
                    int v = (int)dir;
                    int nearest = ((v + 1) / 2) * 2; // round to even
                    nearest &= 0x7;
                    return FacingToQuarterTurnsFromNorth((Direction)nearest);
            }
        }
    }
}
