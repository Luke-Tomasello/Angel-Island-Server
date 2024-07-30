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

namespace Server.PathAlgorithms
{
    public abstract class PathAlgorithm
    {
        public abstract bool CheckCondition(Movement.MovementObject obj_start);
        public abstract Direction[] Find(Movement.MovementObject obj_start);

        private static Direction[] m_CalcDirections = new Direction[9]
            {
                Direction.Up,
                Direction.North,
                Direction.Right,
                Direction.West,
                Direction.North,
                Direction.East,
                Direction.Left,
                Direction.South,
                Direction.Down
            };

        public Direction GetDirection(int xSource, int ySource, int xDest, int yDest)
        {
            int x = xDest + 1 - xSource;
            int y = yDest + 1 - ySource;
            int v = (y * 3) + x;

            if (v < 0 || v >= 9)
                return Direction.North;

            return m_CalcDirections[v];
        }
    }
}