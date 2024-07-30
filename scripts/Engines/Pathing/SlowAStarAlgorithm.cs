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

/* Scripts\Engines\Pathing\SlowAStarAlgorithm.cs
 *	ChangeLog
 * 	6/2/2021, Adam
 *		This function was disabled in CheckCondition() by always returning false. Why?
 *		I borrowed the AreaSize from FastAStar, and reenabled CheckCondition()
 *		interestingly, SlowStar is faster than FastStar in at least the few tests I ran.
 *		Lets keep it enabled. I see no ill effects in having it working.
 */

using System;
using CalcMoves = Server.Movement.Movement;
using MoveImpl = Server.Movement.MovementImpl;

namespace Server.PathAlgorithms.SlowAStar
{
    public struct PathNode
    {
        public int x, y, z;
        public int g, h;
        public int px, py, pz;
        public int dir;
    }

    public class SlowAStarAlgorithm : PathAlgorithm
    {
        public static PathAlgorithm Instance = new SlowAStarAlgorithm();

        private const int MaxDepth = 300;
        private const int MaxNodes = MaxDepth * 16;

        private static PathNode[] m_Closed = new PathNode[MaxNodes];
        private static PathNode[] m_Open = new PathNode[MaxNodes];
        private static PathNode[] m_Successors = new PathNode[8];
        private static Direction[] m_Path = new Direction[MaxNodes];

        private Point3D m_Goal;

        public int Heuristic(int x, int y, int z)
        {
            x -= m_Goal.X;
            y -= m_Goal.Y;
            z -= m_Goal.Z;

            x *= 11;
            y *= 11;

            return (x * x) + (y * y) + (z * z);
        }

        public override bool CheckCondition(Movement.MovementObject obj_start)
        {
            // this was set to return false, why? we don't want folks using it?
            // private const int AreaSize = 38; /* from FastStarAlgorithm */
            //	interestingly, SlowStar is faster than FastStar in at least the few tests I ran.
            //	Lets keep it enabled
            return Utility.InRange(obj_start.StartingLocation, obj_start.Goal, 38);
            //return false;
        }

        public override Direction[] Find(Movement.MovementObject obj_start)
        {
            m_Goal = obj_start.Goal;

            PathNode curNode;

            PathNode goalNode = new PathNode();
            goalNode.x = obj_start.Goal.X;
            goalNode.y = obj_start.Goal.Y;
            goalNode.z = obj_start.Goal.Z;

            PathNode startNode = new PathNode();
            startNode.x = obj_start.StartingLocation.X;
            startNode.y = obj_start.StartingLocation.Y;
            startNode.z = obj_start.StartingLocation.Z;
            startNode.h = Heuristic(startNode.x, startNode.y, startNode.z);

            PathNode[] closed = m_Closed, open = m_Open, successors = m_Successors;
            Direction[] path = m_Path;

            int closedCount = 0, openCount = 0, sucCount = 0, pathCount = 0;
            int popIndex, curF;
            int x, y, z;
            int depth = 0;

            int xBacktrack, yBacktrack, zBacktrack, iBacktrack = 0;

            open[openCount++] = startNode;

            while (openCount > 0)
            {
                curNode = open[0];
                curF = curNode.g + curNode.h;
                popIndex = 0;

                for (int i = 1; i < openCount; ++i)
                {
                    if ((open[i].g + open[i].h) < curF)
                    {
                        curNode = open[i];
                        curF = curNode.g + curNode.h;
                        popIndex = i;
                    }
                }

                if (curNode.x == goalNode.x && curNode.y == goalNode.y && Math.Abs(curNode.z - goalNode.z) < 16)
                {
                    if (closedCount == MaxNodes)
                        break;

                    closed[closedCount++] = curNode;

                    xBacktrack = curNode.px;
                    yBacktrack = curNode.py;
                    zBacktrack = curNode.pz;

                    if (pathCount == MaxNodes)
                        break;

                    path[pathCount++] = (Direction)curNode.dir;

                    while (xBacktrack != startNode.x || yBacktrack != startNode.y || zBacktrack != startNode.z)
                    {
                        bool found = false;

                        for (int j = 0; !found && j < closedCount; ++j)
                        {
                            if (closed[j].x == xBacktrack && closed[j].y == yBacktrack && closed[j].z == zBacktrack)
                            {
                                if (pathCount == MaxNodes)
                                    break;

                                curNode = closed[j];
                                path[pathCount++] = (Direction)curNode.dir;
                                xBacktrack = curNode.px;
                                yBacktrack = curNode.py;
                                zBacktrack = curNode.pz;
                                found = true;
                            }
                        }

                        if (!found)
                        {
                            Console.WriteLine("bugaboo..");
                            return null;
                        }

                        if (pathCount == MaxNodes)
                            break;
                    }

                    if (pathCount == MaxNodes)
                        break;

                    Direction[] dirs = new Direction[pathCount];

                    while (pathCount > 0)
                        dirs[iBacktrack++] = path[--pathCount];

                    return dirs;
                }

                --openCount;

                for (int i = popIndex; i < openCount; ++i)
                    open[i] = open[i + 1];

                sucCount = 0;

                MoveImpl.AlwaysIgnoreDoors = obj_start.CanOpenDoors;
                MoveImpl.IgnoreMovableImpassables = obj_start.CanMoveOverObstacles;

                for (int i = 0; i < 8; ++i)
                {
                    switch (i)
                    {
                        default:
                        case 0: x = 0; y = -1; break;
                        case 1: x = 1; y = -1; break;
                        case 2: x = 1; y = 0; break;
                        case 3: x = 1; y = 1; break;
                        case 4: x = 0; y = 1; break;
                        case 5: x = -1; y = 1; break;
                        case 6: x = -1; y = 0; break;
                        case 7: x = -1; y = -1; break;
                    }

                    if (CalcMoves.CheckMovement(obj_start, new Point3D(curNode.x, curNode.y, curNode.z), (Direction)i, out z))
                    {
                        successors[sucCount].x = x + curNode.x;
                        successors[sucCount].y = y + curNode.y;
                        successors[sucCount++].z = z;
                    }
                }

                MoveImpl.AlwaysIgnoreDoors = false;
                MoveImpl.IgnoreMovableImpassables = false;

                if (sucCount == 0 || ++depth > MaxDepth)
                    break;

                for (int i = 0; i < sucCount; ++i)
                {
                    x = successors[i].x;
                    y = successors[i].y;
                    z = successors[i].z;

                    successors[i].g = curNode.g + 1;

                    int openIndex = -1, closedIndex = -1;

                    for (int j = 0; openIndex == -1 && j < openCount; ++j)
                    {
                        if (open[j].x == x && open[j].y == y && open[j].z == z)
                            openIndex = j;
                    }

                    if (openIndex >= 0 && open[openIndex].g < successors[i].g)
                        continue;

                    for (int j = 0; closedIndex == -1 && j < closedCount; ++j)
                    {
                        if (closed[j].x == x && closed[j].y == y && closed[j].z == z)
                            closedIndex = j;
                    }

                    if (closedIndex >= 0 && closed[closedIndex].g < successors[i].g)
                        continue;

                    if (openIndex >= 0)
                    {
                        --openCount;

                        for (int j = openIndex; j < openCount; ++j)
                            open[j] = open[j + 1];
                    }

                    if (closedIndex >= 0)
                    {
                        --closedCount;

                        for (int j = closedIndex; j < closedCount; ++j)
                            closed[j] = closed[j + 1];
                    }

                    successors[i].px = curNode.x;
                    successors[i].py = curNode.y;
                    successors[i].pz = curNode.z;
                    successors[i].dir = (int)GetDirection(curNode.x, curNode.y, x, y);
                    successors[i].h = Heuristic(x, y, z);

                    if (openCount == MaxNodes)
                        break;

                    open[openCount++] = successors[i];
                }

                if (openCount == MaxNodes || closedCount == MaxNodes)
                    break;

                closed[closedCount++] = curNode;
            }

            return null;
        }
    }
}