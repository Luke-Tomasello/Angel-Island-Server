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

/* Scripts/Engines/Pathing/NavStarAlgorithem.cs
 * * ChangeLog
 * 	12/05/05, Kit
 *		Initial Creation
 */

using System.Collections;
using CalcMoves = Server.Movement.Movement;
using MoveImpl = Server.Movement.MovementImpl;

namespace Server.PathAlgorithms.NavStar
{
    public struct PathNode
    {
        public int cost, total;
        public int parent, next, prev;
        public int z;
    }

    public class NavStarAlgorithm : PathAlgorithm
    {
        public static PathAlgorithm Instance = new NavStarAlgorithm();

        private const int MaxDepth = 30000;
        private const int AreaSize = 256;

        private const int NodeCount = AreaSize * AreaSize * PlaneCount;

        private const int PlaneOffset = 128;
        private const int PlaneCount = 13;
        private const int PlaneHeight = 20;

        private static Direction[] m_Path = new Direction[AreaSize * AreaSize];
        private static PathNode[] m_Nodes = new PathNode[NodeCount];
        private static BitArray m_Touched = new BitArray(NodeCount);
        private static BitArray m_OnOpen = new BitArray(NodeCount);
        private static int[] m_Successors = new int[8];

        private static int m_xOffset, m_yOffset;
        private static int m_OpenList;

        private Point3D m_Goal;

        public int Heuristic(int x, int y, int z)
        {
            x -= m_Goal.X - m_xOffset;
            y -= m_Goal.Y - m_yOffset;
            z -= m_Goal.Z;

            x *= 11;
            y *= 11;

            return (x * x) + (y * y) + (z * z);
        }

        public override bool CheckCondition(Movement.MovementObject obj_start)
        {
            return Utility.InRange(obj_start.StartingLocation, obj_start.Goal, AreaSize);
        }

        private void RemoveFromChain(int node)
        {
            if (node < 0 || node >= NodeCount)
                return;

            if (!m_Touched[node] || !m_OnOpen[node])
                return;

            int prev = m_Nodes[node].prev;
            int next = m_Nodes[node].next;

            if (m_OpenList == node)
                m_OpenList = next;

            if (prev != -1)
                m_Nodes[prev].next = next;

            if (next != -1)
                m_Nodes[next].prev = prev;

            m_Nodes[node].prev = -1;
            m_Nodes[node].next = -1;
        }

        private void AddToChain(int node)
        {
            if (node < 0 || node >= NodeCount)
                return;

            RemoveFromChain(node);

            if (m_OpenList != -1)
                m_Nodes[m_OpenList].prev = node;

            m_Nodes[node].next = m_OpenList;
            m_Nodes[node].prev = -1;

            m_OpenList = node;

            m_Touched[node] = true;
            m_OnOpen[node] = true;
        }

        public override Direction[] Find(Movement.MovementObject obj_start)
        {
            if (!Utility.InRange(obj_start.StartingLocation, obj_start.Goal, AreaSize))
                return null;

            m_Touched.SetAll(false);

            m_Goal = obj_start.Goal;

            m_xOffset = (obj_start.StartingLocation.X + obj_start.Goal.X - AreaSize) / 2;
            m_yOffset = (obj_start.StartingLocation.Y + obj_start.Goal.Y - AreaSize) / 2;

            int fromNode = GetIndex(obj_start.StartingLocation.X, obj_start.StartingLocation.Y, obj_start.StartingLocation.Z);
            int destNode = GetIndex(obj_start.Goal.X, obj_start.Goal.Y, obj_start.Goal.Z);

            m_OpenList = fromNode;

            m_Nodes[m_OpenList].cost = 0;
            m_Nodes[m_OpenList].total = Heuristic(obj_start.StartingLocation.X - m_xOffset, obj_start.StartingLocation.Y - m_yOffset, obj_start.StartingLocation.Z);
            m_Nodes[m_OpenList].parent = -1;
            m_Nodes[m_OpenList].next = -1;
            m_Nodes[m_OpenList].prev = -1;
            m_Nodes[m_OpenList].z = obj_start.StartingLocation.Z;

            m_OnOpen[m_OpenList] = true;
            m_Touched[m_OpenList] = true;

            int pathCount, parent;
            int backtrack = 0, depth = 0;

            Direction[] path = m_Path;

            while (m_OpenList != -1)
            {
                int bestNode = FindBest(m_OpenList);

                if (++depth > MaxDepth)
                    break;


                MoveImpl.AlwaysIgnoreDoors = obj_start.CanOpenDoors;
                MoveImpl.IgnoreMovableImpassables = obj_start.CanMoveOverObstacles;

                int[] vals = m_Successors;
                int count = GetSuccessors(obj_start, bestNode);

                MoveImpl.AlwaysIgnoreDoors = false;
                MoveImpl.IgnoreMovableImpassables = false;

                if (count == 0)
                    break;

                for (int i = 0; i < count; ++i)
                {
                    int newNode = vals[i];

                    bool wasTouched = m_Touched[newNode];

                    if (!wasTouched)
                    {
                        int newCost = m_Nodes[bestNode].cost + 1;
                        int newTotal = newCost + Heuristic(newNode % AreaSize, (newNode / AreaSize) % AreaSize, m_Nodes[newNode].z);

                        if (!wasTouched || m_Nodes[newNode].total > newTotal)
                        {
                            m_Nodes[newNode].parent = bestNode;
                            m_Nodes[newNode].cost = newCost;
                            m_Nodes[newNode].total = newTotal;

                            if (!wasTouched || !m_OnOpen[newNode])
                            {
                                AddToChain(newNode);

                                if (newNode == destNode)
                                {
                                    pathCount = 0;
                                    parent = m_Nodes[newNode].parent;

                                    while (parent != -1)
                                    {
                                        path[pathCount++] = GetDirection(parent % AreaSize, (parent / AreaSize) % AreaSize, newNode % AreaSize, (newNode / AreaSize) % AreaSize);
                                        newNode = parent;
                                        parent = m_Nodes[newNode].parent;

                                        if (newNode == fromNode)
                                            break;
                                    }

                                    Direction[] dirs = new Direction[pathCount];

                                    while (pathCount > 0)
                                        dirs[backtrack++] = path[--pathCount];

                                    return dirs;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        private int GetIndex(int x, int y, int z)
        {
            x -= m_xOffset;
            y -= m_yOffset;
            z += PlaneOffset;
            z /= PlaneHeight;

            return x + (y * AreaSize) + (z * AreaSize * AreaSize);
        }

        private int FindBest(int node)
        {
            int least = m_Nodes[node].total;
            int leastNode = node;

            while (node != -1)
            {
                if (m_Nodes[node].total < least)
                {
                    least = m_Nodes[node].total;
                    leastNode = node;
                }

                node = m_Nodes[node].next;
            }

            RemoveFromChain(leastNode);

            m_Touched[leastNode] = true;
            m_OnOpen[leastNode] = false;

            return leastNode;
        }

        public int GetSuccessors(Movement.MovementObject object_start, int p)
        {
            int px = p % AreaSize;
            int py = (p / AreaSize) % AreaSize;
            int pz = m_Nodes[p].z;
            int x, y, z;

            Point3D p3D = new Point3D(px + m_xOffset, py + m_yOffset, pz);

            int[] vals = m_Successors;
            int count = 0;

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

                x += px;
                y += py;

                if (x < 0 || x >= AreaSize || y < 0 || y >= AreaSize)
                    continue;

                if (CalcMoves.CheckMovement(object_start, p3D, (Direction)i, out z))
                {
                    int idx = GetIndex(x + m_xOffset, y + m_yOffset, z);

                    if (idx >= 0 && idx < NodeCount)
                    {
                        m_Nodes[idx].z = z;
                        vals[count++] = idx;
                    }
                }
            }

            return count;
        }
    }
}