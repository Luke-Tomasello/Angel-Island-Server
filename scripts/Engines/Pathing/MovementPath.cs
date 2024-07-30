/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
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

/* Scripts/Engines/Pathing/MovementPath.cs
 * CHANGELOG
 * 4/8/23, Yoar: Path command
 *      Turned display items into location effects.
 *      Fixed z location of path display
 * 12/05/05, Kit
 *		Added 2nd MovementPath() for use of navStar system.
 */
using Server.Diagnostics;
using Server.PathAlgorithms;
using Server.PathAlgorithms.FastAStar;
using Server.PathAlgorithms.NavStar;
using Server.PathAlgorithms.SlowAStar;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server
{
    public sealed class MovementPath
    {
        private Map m_Map;
        private Point3D m_Start;
        private Point3D m_Goal;
        private Direction[] m_Directions;

        public Map Map { get { return m_Map; } }
        public Point3D Start { get { return m_Start; } }
        public Point3D Goal { get { return m_Goal; } }
        public Direction[] Directions { get { return m_Directions; } }
        public bool Success { get { return (m_Directions != null && m_Directions.Length > 0); } }

        public static void Initialize()
        {
            CommandSystem.Register("Path", AccessLevel.GameMaster, new CommandEventHandler(Path_OnCommand));
            CommandSystem.Register("PathSlow", AccessLevel.GameMaster, new CommandEventHandler(PathSlow_OnCommand));
            CommandSystem.Register("PathFast", AccessLevel.GameMaster, new CommandEventHandler(PathFast_OnCommand));
        }

        public static void Path_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, true, TargetFlags.None, new TargetCallback(Path_OnTarget));
            e.Mobile.SendMessage("Target a location and a path will be drawn there.");
        }
        public static void PathSlow_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, true, TargetFlags.None, new TargetCallback(PathSlow_OnTarget));
            e.Mobile.SendMessage("Target a location and a path will be drawn there.");
        }
        public static void PathFast_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, true, TargetFlags.None, new TargetCallback(PathFast_OnTarget));
            e.Mobile.SendMessage("Target a location and a path will be drawn there.");
        }

        public static void Path(Mobile from, IPoint3D goal, PathAlgorithm alg, string name, int zOffset)
        {
            m_OverrideAlgorithm = alg;

            long start = DateTime.UtcNow.Ticks;
            Movement.MovementObject obj_start = new Movement.MovementObject(from, new Point3D(goal));
            MovementPath path = new MovementPath(obj_start);
            long end = DateTime.UtcNow.Ticks;
            double len = Math.Round((end - start) / 10000.0, 2);

            if (!path.Success)
            {
                from.SendMessage("{0} path failed: {1}ms", name, len);
            }
            else
            {
                from.SendMessage("{0} path success: {1}ms", name, len);

                int x = from.X;
                int y = from.Y;
                int z = from.Z;

                Map map = from.Map;
                //Server.Network.NetState ns = from.NetState;
                List<Item> items = new List<Item>();
                for (int i = 0; i < path.Directions.Length; ++i)
                {
                    Movement.Movement.Offset(path.Directions[i], ref x, ref y);

                    if (map != null)
                    {
                        int surfaceZ;

                        if (map.GetTopSurface(new Point3D(x, y, z + 5), out surfaceZ) != null)
                            z = surfaceZ;
                    }

                    //if (ns != null) ns.Send(new Server.Network.LocationEffect(new Point3D(x, y, z + zOffset), 0x20F6, 1, 75, 1152, 3));
                    Item marker = new Item(0x0EDE/*0x20F6*/);
                    marker.Name = alg.ToString().Contains("Fast") ? "Fast:" + i.ToString() : "Slow:" + i.ToString();
                    marker.MoveToWorld(new Point3D(x, y, z + zOffset), from.Map);
                    items.Add(marker);
                    Timer.DelayCall(TimeSpan.FromSeconds(10), new TimerStateCallback(CleanupTick), new object[] { items });

                }
            }
        }
        private static void CleanupTick(object state)
        {
            object[] aState = (object[])state;
            if (aState[0] != null && aState[0] is List<Item> items)
                foreach (Item item in items)
                    item.Delete();
        }
        public static void Path_OnTarget(Mobile from, object obj)
        {
            IPoint3D goal = obj as IPoint3D;

            if (goal == null)
                return;

            Spells.SpellHelper.GetSurfaceTop(ref goal);

            Path(from, goal, FastAStarAlgorithm.Instance, "Fast", 0);
            Path(from, goal, SlowAStarAlgorithm.Instance, "Slow", 2);
            m_OverrideAlgorithm = null;
        }
        public static void PathSlow_OnTarget(Mobile from, object obj)
        {
            IPoint3D goal = obj as IPoint3D;

            if (goal == null)
                return;

            Spells.SpellHelper.GetSurfaceTop(ref goal);

            Path(from, goal, SlowAStarAlgorithm.Instance, "Slow", 2);
            m_OverrideAlgorithm = null;
        }
        public static void PathFast_OnTarget(Mobile from, object obj)
        {
            IPoint3D goal = obj as IPoint3D;

            if (goal == null)
                return;

            Spells.SpellHelper.GetSurfaceTop(ref goal);

            Path(from, goal, FastAStarAlgorithm.Instance, "Fast", 0);
            m_OverrideAlgorithm = null;
        }

        public static bool PathTo(Movement.MovementObject obj_start)
        {

            if (obj_start.Goal == Point3D.Zero)
                return false;

            IPoint3D goal = obj_start.Goal;

            m_OverrideAlgorithm = FastAStarAlgorithm.Instance;

            // get/set the target surface
            Spells.SpellHelper.GetSurfaceTop(ref goal);
            obj_start.Goal = new Point3D(goal.X, goal.Y, goal.Z);

            MovementPath path = new MovementPath(obj_start);
            if (path != null)
            {
                if (path.Success)
                {
                    m_OverrideAlgorithm = null;
                    return true;
                }
                else
                {
                    m_OverrideAlgorithm = SlowAStarAlgorithm.Instance;
                    path = new MovementPath(obj_start);
                    if (path.Success)
                    {
                        m_OverrideAlgorithm = null;
                        return true;
                    }
                }
            }

            m_OverrideAlgorithm = null;
            return false;
        }

        public static void Pathfind(object state)
        {
            object[] states = (object[])state;
            Mobile from = (Mobile)states[0];
            Direction d = (Direction)states[1];

            try
            {
                from.Direction = d;
                from.NetState.BlockAllPackets = true;
                from.Move(d);
                from.NetState.BlockAllPackets = false;
                from.ProcessDelta();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        private static PathAlgorithm m_OverrideAlgorithm;

        public PathAlgorithm OverrideAlgorithm
        {
            get { return m_OverrideAlgorithm; }
            set { m_OverrideAlgorithm = value; }
        }

        public MovementPath(Movement.MovementObject obj_start)
        {
            m_Map = obj_start.Map;
            m_Start = obj_start.StartingLocation;
            m_Goal = obj_start.Goal;

            if (obj_start.Map == null || obj_start.Map == Map.Internal)
                return;

            if (Utility.InRange(obj_start.StartingLocation, obj_start.Goal, 1))
                return;

            try
            {
                PathAlgorithm alg = m_OverrideAlgorithm;

                if (alg == null)
                {
                    alg = FastAStarAlgorithm.Instance;

                    if (!alg.CheckCondition(obj_start))
                        alg = SlowAStarAlgorithm.Instance;
                }

                if (alg != null && alg.CheckCondition(obj_start))
                    m_Directions = alg.Find(obj_start);
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Warning: {0}: Pathing error from {1} to {2}", e.GetType().Name, obj_start.StartingLocation, obj_start.Goal);
            }
        }

        public MovementPath(Mobile m, Point3D goal, bool Nav)
        {
            Point3D start = m.Location;
            Map map = m.Map;

            m_Map = map;
            m_Start = start;
            m_Goal = goal;

            if (map == null || map == Map.Internal)
                return;

            if (Utility.InRange(start, goal, 1))
                return;

            try
            {
                PathAlgorithm alg = m_OverrideAlgorithm;
                Movement.MovementObject obj_start = new Movement.MovementObject(m, goal);
                if (alg == null)
                {
                    alg = NavStarAlgorithm.Instance;

                    if (!alg.CheckCondition(obj_start))
                        alg = SlowAStarAlgorithm.Instance;
                }

                if (alg != null && alg.CheckCondition(obj_start))
                    m_Directions = alg.Find(obj_start);
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Warning: {0}: Pathing error from {1} to {2}", e.GetType().Name, start, goal);
            }
        }
    }
}