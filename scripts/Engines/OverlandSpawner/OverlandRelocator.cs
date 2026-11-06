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

/* Scripts\Engines\OverlandSpawner\OverlandRelocator.cs
 * ChangeLog
 *  10/21/26, Adam
 *      Created. One-time relocation of existing overland NPCs (OverlandBandit, OverlandMerchant,
 *      OverlandTreasureHunter) placed before the treasure map locations were validated. Used by PatchV254.
 *      - Exempt: spawner-controlled NPCs (intentionally placed), escorted NPCs, and treasure hunters whose quest has
 *        started (past Ready); they are counted but never moved.
 *      - Valid: standable, non-water ground in the world, not inside a multi or a jail/prison/house/dungeon region
 *        (or a guarded/town region for a bandit), with open ground reachable on foot or through a door.
 *      - Anything else (invalid or uncertain) is moved to the nearest treasure map location that is itself valid;
 *        Home moves with it, and an announced NPC's Town Crier entry is refreshed with the new location.
 *        Identity, inventory, quest state and behavior are unchanged.
 *      - Rerun-safe: a relocated NPC is valid, so a second run changes nothing.
 *      The run is written to OverlandRelocation.log.
 */

using Server.Diagnostics;
using Server.Engines.TerritoryControl;
using Server.Items;
using Server.Mobiles;
using Server.Movement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Server.Engines.OverlandSpawner
{
    public static class OverlandRelocator
    {
        private const int ReachCap = 400;           // fewer reachable tiles than this = confined
        private const int Candidates = 50;          // nearest treasure map locations tried per NPC

        public sealed class Result
        {
            public int Checked, Exempt, Valid, Repositioned, Unresolved;
            public string Summary => string.Format("Checked {0} overland NPCs, repositioned {1}, unresolved {2}.", Checked, Repositioned, Unresolved);
        }

        /// <summary>
        /// Relocates overland NPCs whose placement is not valid to verified treasure map locations.
        /// Writes the run to OverlandRelocation.log.
        /// </summary>
        public static Result Relocate(string caller)
        {
            Result r = new Result();
            StringBuilder log = new StringBuilder();
            log.AppendFormat("{0} {1} on {2}\r\n", caller, DateTime.Now, Core.Server);

            Point2D[] pool = TreasureMap.Locations ?? new Point2D[0];
            List<BaseCreature> npcs = World.Mobiles.Values.OfType<BaseCreature>()
                .Where(m => !m.Deleted && (m is OverlandBandit || m is OverlandMerchant || m is OverlandTreasureHunter))
                .OrderBy(m => m.Serial.Value).ToList();

            foreach (BaseCreature bc in npcs)
            {
                r.Checked++;
                string id = string.Format("{0} {1} \"{2}\"", bc.GetType().Name, bc.Serial, bc.Name);

                string? exempt = bc.Spawner != null ? "spawner-controlled"
                    : bc.ControlMaster != null ? "escorted"
                    : bc is OverlandTreasureHunter th && th.State > OverlandTreasureHunter.QuestState.Ready ? "treasure hunt in progress (" + th.State + ")"
                    : null;
                if (exempt != null)
                {
                    r.Exempt++;
                    log.AppendFormat("exempt: {0} at {1} {2}: {3}\r\n", id, bc.Location, bc.Map, exempt);
                    continue;
                }

                List<string> problems = Problems(bc.Map, bc.Location, bc is OverlandBandit);
                if (problems.Count == 0)
                {
                    r.Valid++;
                    continue;
                }

                Point3D? dest = Candidate(bc.Map == Map.Felucca ? bc.Location : Point3D.Zero, pool, bc is OverlandBandit);
                if (dest == null)
                {
                    r.Unresolved++;
                    log.AppendFormat("unresolved: {0} at {1} {2}: {3}; no valid treasure map location found\r\n", id, bc.Location, bc.Map, string.Join("; ", problems));
                    continue;
                }

                Point3D from = bc.Location;
                Map? fromMap = bc.Map;
                bc.Home = dest.Value;
                bc.MoveToWorld(dest.Value, Map.Felucca);
                if (bc is BaseOverland bo && bo.Announce && bo.LastMsg == BaseOverland.MsgState.InitialMsg)
                {   // refresh the Town Crier entry so it describes the new location (as the message timer does)
                    bo.OverlandSystemMessage(BaseOverland.MsgState.None);
                    bo.OverlandSystemMessage(BaseOverland.MsgState.InitialMsg);
                }
                r.Repositioned++;
                log.AppendFormat("repositioned: {0} from {1} {2} to {3} Felucca: {4}\r\n", id, from, fromMap, dest.Value, string.Join("; ", problems));
            }

            log.AppendFormat("{0} Exempt {1}, valid {2}. Treasure map locations: {3}.\r\n", r.Summary, r.Exempt, r.Valid, pool.Length);
            try
            {
                File.AppendAllText(Path.Combine(Core.LogsDirectory, "OverlandRelocation.log"), log.ToString() + "\r\n");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            return r;
        }

        // Why a standing position is not valid (empty = valid): invalid (not in the world, at 0,0, water, not standable)
        //  or uncertain (multi, restricted region, not on Felucca, confined with no door to open ground).
        private static List<string> Problems(Map? map, Point3D p, bool hostile)
        {
            List<string> r = new List<string>();
            if (map == null || map == Map.Internal)
            {
                r.Add("not in the world");
                return r;
            }
            if (p.X == 0 && p.Y == 0) r.Add("at 0,0");
            if (Utility.IsWater(map, p.X, p.Y, p.Z)) r.Add("standing in water");
            else if (!ZoneGuardianSpawner.IsWalkableLand(map, p)) r.Add("not standable ground");
            if (map != Map.Felucca) r.Add("not on Felucca");
            if (p.X >= 5120) r.Add("dungeon/T2A coordinates");
            if (BaseMulti.Find(p, map) != null) r.Add("inside a multi");
            foreach (object o in Region.FindAll(p, map))
            {
                if (o is not Region reg || reg == map.DefaultRegion)
                    continue;
                string n = string.IsNullOrEmpty(reg.Name) ? reg.GetType().Name : reg.Name;
                if (reg.IsJailRules || reg.IsPrisonRules) r.Add("jail/prison region " + n);
                if (reg.IsHouseRules) r.Add("house region " + n);
                if (reg.IsDungeonRules) r.Add("dungeon region " + n);
                if (hostile && (reg.IsGuarded || reg.IsTownRules)) r.Add("bandit in guarded/town region " + n);
            }
            if (r.Count == 0 && Reach(map, p, false) < ReachCap && Reach(map, p, true) < ReachCap)
                r.Add("confined, no door to open ground");
            return r;
        }

        // 8-way flood fill using the server's movement rules for a human NPC; stops at ReachCap.
        //  With doors, a door tile is passable (NPCs open doors).
        private static int Reach(Map map, Point3D start, bool doors)
        {
            HashSet<long> seen = new HashSet<long>() { Key(start.X, start.Y) };
            Queue<Point3D> q = new Queue<Point3D>();
            q.Enqueue(start);
            while (q.Count > 0 && seen.Count < ReachCap)
            {
                Point3D p = q.Dequeue();
                for (int d = 0; d < 8; d++)
                {
                    int x = p.X, y = p.Y;
                    Movement.Movement.Offset((Direction)d, ref x, ref y);
                    if (seen.Contains(Key(x, y)))
                        continue;
                    MovementObject mo = new MovementObject(p, p, map, player: false);
                    if (!Movement.Movement.CheckMovement(mo, p, (Direction)d, out int z))
                    {
                        if (!doors || (d & 1) != 0 || !DoorAt(map, x, y, p.Z))   // doors are entered orthogonally
                            continue;
                        z = p.Z;
                    }
                    seen.Add(Key(x, y));
                    q.Enqueue(new Point3D(x, y, z));
                }
            }
            return seen.Count;
        }

        private static bool DoorAt(Map map, int x, int y, int z)
        {
            IPooledEnumerable eable = map.GetItemsInRange(new Point3D(x, y, z), 0);
            try
            {
                foreach (Item i in eable)
                    if (i is BaseDoor d && Math.Abs(d.Z - z) <= 10)
                        return true;
                return false;
            }
            finally { eable.Free(); }
        }

        // The nearest treasure map location (to near, or the first valid one if near is Zero) that is valid itself.
        private static Point3D? Candidate(Point3D near, Point2D[] pool, bool hostile)
        {
            Map map = Map.Felucca;
            IEnumerable<Point2D> order = near == Point3D.Zero ? pool
                : pool.OrderBy(c => (long)(c.X - near.X) * (c.X - near.X) + (long)(c.Y - near.Y) * (c.Y - near.Y));
            foreach (Point2D c in order.Take(Candidates))
            {
                Point3D p = new Point3D(c.X, c.Y, map.GetAverageZ(c.X, c.Y));
                if (Problems(map, p, hostile).Count == 0)
                    return p;
            }
            return null;
        }

        private static long Key(int x, int y) => ((long)x << 32) ^ (uint)y;
    }
}
