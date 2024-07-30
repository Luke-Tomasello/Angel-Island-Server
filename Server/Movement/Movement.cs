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

/* Server\Movement\Movement.cs
 * CHANGELOG:
 * 1/4/2022, Yoar
 *      Added m_Player field to check whether the moving object is a player.
 * 1/3/2022, Adam (MovementObject)
 *      Added m_DebugOnlyMobile to the movement Object.
 *      You should never use this for calcs as it's only available in DEBUG mode.
 *      Use: Useful for setting breakpoints if say movement was caused by m_DebugOnlyMobile.Name == "Jack"
 * ??/??/2021, Adam (MovementObject)
 *  Initial creation. I did away with the notion of always having to have a Mobile for movement calcs.
 *  There was a need to do pathing calcs that didn't involve a mobile for example. Like can *a* mobile
 *  travel from point A to point B. (doing this without ever actually having the mobile in-hand.)
 */

namespace Server.Movement
{
    public class MovementObject
    {
        Point3D m_StartingLocation;
        Point3D m_Goal;
        Map m_Map;
        bool m_CanFly;
        bool m_CantWalkLand;
        bool m_CanSwim;
        bool m_Alive;
        int m_BodyID;
        bool m_IsDeadBondedPet;
        private int[] m_FlyIDs = new int[] { };
        bool m_CanOpenDoors;
        bool m_CanMoveOverObstacles;
        bool m_Player;
#if DEBUG
        Mobile m_Mobile;
#endif

        public MovementObject(Mobile m, Point3D goal)
            : this(m.Location, goal, m.Map, m.CanFlyOver,
                  m.CantWalkLand, m.CanSwim, m.Alive, m.Body,
                  m.IsDeadBondedPet, m.CanOpenDoors, m.CanMoveOverObstacles,
                  m.Player)
        {

#if DEBUG
            m_Mobile = m;
#endif
            //TO_DO: Need to handle township NPCs. They were specifically designed to disallow opeining of doors.
        }

        public MovementObject(Point3D location, Point3D goal, Map map = null, bool canFly = false,
            bool cantWalk = false, bool canSwim = false, bool alive = true, int bodyID = 0x190,
            bool isDeadBondedPet = false, bool canOpenDoors = true, bool canMoveOverObstacles = false,
            bool player = true)
        {
            m_StartingLocation = location;
            m_Goal = goal;
            m_Map = (map == null) ? Map.Felucca : map;
            m_CanFly = canFly;
            m_CantWalkLand = cantWalk;
            m_CanSwim = canSwim;
            m_Alive = alive;
            m_BodyID = bodyID;
            m_IsDeadBondedPet = isDeadBondedPet;
            m_CanOpenDoors = canOpenDoors;
            m_CanMoveOverObstacles = canMoveOverObstacles;
            m_Player = player;
        }
#if DEBUG
        public Mobile Mobile { get { return m_Mobile; } }
#endif
        public Point3D StartingLocation
        { get { return m_StartingLocation; } }
        public Point3D Goal
        { get { return m_Goal; } set { m_Goal = value; } }
        public Map Map
        { get { return m_Map; } }
        public bool CanFly
        { get { return m_CanFly; } }
        public bool CantFly
        { get { return !m_CanFly; } }
        public bool CantWalkLand
        { get { return m_CantWalkLand; } }
        public bool CanWalkLand
        { get { return !m_CantWalkLand; } }
        public bool WaterOnly
        { get { return m_CantWalkLand && CanSwim; } }
        public bool LandOnly
        { get { return CanWalkLand && !CanSwim; } }
        public bool CanSwim
        { get { return m_CanSwim; } }
        public bool CantSwim
        { get { return !m_CanSwim; } }
        public bool Alive
        { get { return m_Alive; } }
        public bool Dead
        { get { return !m_Alive; } }
        public int BodyID
        { get { return m_BodyID; } }
        public bool IsDeadBondedPet
        { get { return m_IsDeadBondedPet; } }
        public int[] FlyArray
        { get { return m_FlyIDs; } }
        public bool CanOpenDoors
        { get { return m_CanOpenDoors; } }
        public bool CanMoveOverObstacles
        { get { return m_CanMoveOverObstacles; } }
        public bool Player
        { get { return m_Player; } }
        /*private static bool CalcCanOpenDoors(Mobile m)
        {   // from baseCreature return !this.Body.IsAnimal && !this.Body.IsSea;
            if (Server.Body.GetBodyType(m.Body) != BodyType.Animal && Server.Body.GetBodyType(m.Body) != BodyType.Sea)
                return true;
            else
                return false;
        }
        private static bool CalcCanMoveOverObstacles(Mobile m)
        {
            if (Server.Body.GetBodyType(m.Body) == BodyType.Monster)
                return true;
            else
                return false;
        }*/
    }
    public class Movement
    {
        private static IMovementImpl m_Impl;

        public static IMovementImpl Impl
        {
            get { return m_Impl; }
            set { m_Impl = value; }
        }

        public static bool CheckMovement(MovementObject obj_start, Direction d, out int newZ)
        {
            if (m_Impl != null)
                return m_Impl.CheckMovement(obj_start, d, out newZ);

            newZ = obj_start.StartingLocation.Z;

            return false;
        }

        public static bool CheckMovement(MovementObject obj_start, Point3D loc, Direction d, out int newZ)
        {
            if (m_Impl != null)
                return m_Impl.CheckMovement(obj_start, loc, d, out newZ);

            newZ = obj_start.StartingLocation.Z;
            return false;
        }

        public static void Offset(Direction d, ref int x, ref int y)
        {
            switch (d & Direction.Mask)
            {
                case Direction.North: --y; break;
                case Direction.South: ++y; break;
                case Direction.West: --x; break;
                case Direction.East: ++x; break;
                case Direction.Right: ++x; --y; break;
                case Direction.Left: --x; ++y; break;
                case Direction.Down: ++x; ++y; break;
                case Direction.Up: --x; --y; break;
            }
        }
    }

    public interface IMovementImpl
    {
        bool CheckMovement(MovementObject obj_start, Direction d, out int newZ);
        bool CheckMovement(MovementObject obj_start, Point3D loc, Direction d, out int newZ);
    }
}