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

/* Server/Geometry.cs
 * CHANGELOG
 *  3/6/2024, Adam Rect3d => Rect2d
 *      Add a rect2d constructor that takes two point3ds:
 *      Rectangle2D(IPoint3D start, IPoint3D end)
 *  9/5/2023, Adam (PointsInRect2D/PointsInRect3D)
 *      Add functionality to return all points within a rect
 *  9/20/22, Adam (MaximumSpawnableRect)
 *      Moved to utilities
 *  9/8/22, Adam (Rectangle2D.Center)
 *      Add Rectangle2D.Center
 *	9/4/22, Yoar
 *		Added + and - operators to Point2D
 *  8/28/22, Adam (MaximumSpawnableRect/MakeHold)
 *      - Add MakeHold(Point2D): if the rect is empty, Creates 1x1 tile rectangle. Otherwise, calls MakeHold(Rectangle2D) with a 1x1 rectangle.
 *      - MaximumSpawnableRect: this beast is sort of a geometrical aberration in that it crosses the line between pure geometry and knowing whether a mobile can be spawned within.
 *          Grow the given rectangle in all directions in which a mobile may be spawned.
 *          Use caution: You should only call this in a closed area, otherwise your rectangle can grow quite large.
 *	8/31/10, Adam
 *		make Point2D serializable
 *	9/01/06 Taran Kain
 *		Added + and - operators to Point3D
 */

using System;
using System.Collections.Generic;

namespace Server
{
    [Serializable]
    [Parsable]
    public struct Point2D : IPoint2D
    {
        internal int m_X;
        internal int m_Y;

        public static readonly Point2D Zero = new Point2D(0, 0);

        public Point2D(int x, int y)
        {
            m_X = x;
            m_Y = y;
        }

        public Point2D(IPoint2D p)
            : this(p.X, p.Y)
        {
        }

        public Point2D(IPoint3D p)
            : this(p.X, p.Y)
        {
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int X
        {
            get
            {
                return m_X;
            }
            set
            {
                m_X = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int Y
        {
            get
            {
                return m_Y;
            }
            set
            {
                m_Y = value;
            }
        }

        public override string ToString()
        {
            return String.Format("({0}, {1})", m_X, m_Y);
        }

        public static Point2D Parse(string value)
        {
            int[] tab = Utility.IntParser(value);
            if (tab.Length == 2)
                return new Point2D(tab[0], tab[1]);
            else
                return new Point2D();
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is IPoint2D)) return false;

            IPoint2D p = (IPoint2D)o;

            return m_X == p.X && m_Y == p.Y;
        }

        public override int GetHashCode()
        {
            return m_X ^ m_Y;
        }

        public static bool operator ==(Point2D l, Point2D r)
        {
            return l.m_X == r.m_X && l.m_Y == r.m_Y;
        }

        public static bool operator !=(Point2D l, Point2D r)
        {
            return l.m_X != r.m_X || l.m_Y != r.m_Y;
        }

        public static bool operator ==(Point2D l, IPoint2D r)
        {
            return l.m_X == r.X && l.m_Y == r.Y;
        }

        public static bool operator !=(Point2D l, IPoint2D r)
        {
            return l.m_X != r.X || l.m_Y != r.Y;
        }

        public static bool operator >(Point2D l, Point2D r)
        {
            return l.m_X > r.m_X && l.m_Y > r.m_Y;
        }

        public static bool operator >(Point2D l, Point3D r)
        {
            return l.m_X > r.m_X && l.m_Y > r.m_Y;
        }

        public static bool operator >(Point2D l, IPoint2D r)
        {
            return l.m_X > r.X && l.m_Y > r.Y;
        }

        public static bool operator <(Point2D l, Point2D r)
        {
            return l.m_X < r.m_X && l.m_Y < r.m_Y;
        }

        public static bool operator <(Point2D l, Point3D r)
        {
            return l.m_X < r.m_X && l.m_Y < r.m_Y;
        }

        public static bool operator <(Point2D l, IPoint2D r)
        {
            return l.m_X < r.X && l.m_Y < r.Y;
        }

        public static bool operator >=(Point2D l, Point2D r)
        {
            return l.m_X >= r.m_X && l.m_Y >= r.m_Y;
        }

        public static bool operator >=(Point2D l, Point3D r)
        {
            return l.m_X >= r.m_X && l.m_Y >= r.m_Y;
        }

        public static bool operator >=(Point2D l, IPoint2D r)
        {
            return l.m_X >= r.X && l.m_Y >= r.Y;
        }

        public static bool operator <=(Point2D l, Point2D r)
        {
            return l.m_X <= r.m_X && l.m_Y <= r.m_Y;
        }

        public static bool operator <=(Point2D l, Point3D r)
        {
            return l.m_X <= r.m_X && l.m_Y <= r.m_Y;
        }

        public static bool operator <=(Point2D l, IPoint2D r)
        {
            return l.m_X <= r.X && l.m_Y <= r.Y;
        }

        public static Point2D operator +(Point2D l, Point2D r)
        {
            return new Point2D(l.m_X + r.m_X, l.m_Y + r.m_Y);
        }

        public static Point2D operator -(Point2D l, Point2D r)
        {
            return new Point2D(l.m_X - r.m_X, l.m_Y - r.m_Y);
        }
    }

    [Parsable]
    public struct Point3D : IPoint3D
    {
        internal int m_X;
        internal int m_Y;
        internal int m_Z;

        public static readonly Point3D Zero = new Point3D(0, 0, 0);

        public Point3D(int x, int y, int z)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
        }

        public Point3D(IPoint3D p)
            : this(p.X, p.Y, p.Z)
        {
        }

        public Point3D(IPoint2D p, int z)
            : this(p.X, p.Y, z)
        {
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int X
        {
            get
            {
                return m_X;
            }
            set
            {
                m_X = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int Y
        {
            get
            {
                return m_Y;
            }
            set
            {
                m_Y = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int Z
        {
            get
            {
                return m_Z;
            }
            set
            {
                m_Z = value;
            }
        }

        public override string ToString()
        {
            return String.Format("({0}, {1}, {2})", m_X, m_Y, m_Z);
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is IPoint3D)) return false;

            IPoint3D p = (IPoint3D)o;

            return m_X == p.X && m_Y == p.Y && m_Z == p.Z;
        }

        public override int GetHashCode()
        {
            return m_X ^ m_Y ^ m_Z;
        }

        public static Point3D Parse(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                int[] tab = Utility.IntParser(value);
                if (tab.Length == 3)
                    return new Point3D(Convert.ToInt32(tab[0]), Convert.ToInt32(tab[1]), Convert.ToInt32(tab[2]));
                else
                    return new Point3D();
            }
            else
            {
                //Utility.ConsoleOut("Unable to parse Point3D().", ConsoleColor.Red);
                return new Point3D();
            }
        }

        public static bool operator ==(Point3D l, Point3D r)
        {
            return l.m_X == r.m_X && l.m_Y == r.m_Y && l.m_Z == r.m_Z;
        }

        public static bool operator !=(Point3D l, Point3D r)
        {
            return l.m_X != r.m_X || l.m_Y != r.m_Y || l.m_Z != r.m_Z;
        }

        public static bool operator ==(Point3D l, IPoint3D r)
        {
            return l.m_X == r.X && l.m_Y == r.Y && l.m_Z == r.Z;
        }

        public static bool operator !=(Point3D l, IPoint3D r)
        {
            return l.m_X != r.X || l.m_Y != r.Y || l.m_Z != r.Z;
        }

        public static Point3D operator +(Point3D l, Point3D r)
        {
            return new Point3D(l.X + r.X, l.Y + r.Y, l.Z + r.Z);
        }

        public static Point3D operator -(Point3D l, Point3D r)
        {
            return new Point3D(l.X - r.X, l.Y - r.Y, l.Z - r.Z);
        }
    }

    [NoSort]
    [Parsable]
    [PropertyObject]
    public struct Rectangle2D
    {
        private Point2D m_Start;
        private Point2D m_End;

        public Rectangle2D(int width, int height, IPoint2D center)
        {
            int x = center.X - width / 2;
            int y = center.Y - height / 2;
            m_Start = new Point2D(x, y);
            m_End = new Point2D(x + width, y + height);
        }

        public List<Point3D> PointsInRect3D(Map map)
        {
            List<Point3D> list3D = new();
            List<Point2D> list2D = PointsInRect2D(new Rectangle2D(this.Start, this.End));
            foreach (Point2D p2D in list2D)
                list3D.Add(new Point3D(p2D.X, p2D.Y, map.GetAverageZ(p2D.X, p2D.Y)));

            return list3D;
        }
        public List<Point2D> PointsInRect2D()
        {
            return PointsInRect2D(this);
        }
        public List<Point2D> PointsInRect2D(Rectangle2D rect)
        {
            List<Point2D> list = new();
            Point2D from = new Point2D(rect.Start.X, rect.Start.Y);
            for (int y = from.Y; y < rect.End.Y; y++)
            {
                if (y < rect.Start.Y || y > rect.End.Y)
                    continue;

                for (int x = from.X; x < rect.End.X; x++)
                {
                    if (x < rect.Start.X || x > rect.End.X)
                        continue;

                    list.Add(new Point2D(x, y));
                }
            }
            return list;
        }
        public Rectangle2D(Rectangle3D rect)
            : this(rect.Start, rect.End)
        {
        }
        public Rectangle2D(IPoint2D start, IPoint2D end)
        {
            m_Start = new Point2D(start);
            m_End = new Point2D(end);
        }
        public Rectangle2D(IPoint3D start, IPoint3D end)
            : this(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1)
        {

        }
        public Rectangle2D(int x, int y, int width, int height)
        {
            m_Start = new Point2D(x, y);
            m_End = new Point2D(x + width, y + height);
        }

        public void Set(int x, int y, int width, int height)
        {
            m_Start = new Point2D(x, y);
            m_End = new Point2D(x + width, y + height);
        }

        public static Rectangle2D Parse(string value)
        {
            // example: "(5657, 421)+(12, 10)"
            int[] tab = Utility.IntParser(value);
            if (tab.Length == 4)
                return new Rectangle2D(tab[0], tab[1], tab[2], tab[3]);
            else
                return new Rectangle2D();
        }
        [CommandProperty(AccessLevel.Counselor)]
        public Point2D Start
        {
            get
            {
                return m_Start;
            }
            set
            {
                m_Start = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public Point2D End
        {
            get
            {
                return m_End;
            }
            set
            {
                m_End = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int X
        {
            get
            {
                return m_Start.m_X;
            }
            set
            {
                m_Start.m_X = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int Y
        {
            get
            {
                return m_Start.m_Y;
            }
            set
            {
                m_Start.m_Y = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int Width
        {
            get
            {
                return m_End.m_X - m_Start.m_X;
            }
            set
            {
                m_End.m_X = m_Start.m_X + value;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int Height
        {
            get
            {
                return m_End.m_Y - m_Start.m_Y;
            }
            set
            {
                m_End.m_Y = m_Start.m_Y + value;
            }
        }
        public void MakeHold(Point2D p)
        {
            if (m_Start.m_X == 0 && m_Start.m_Y == 0)
            {
                m_Start.m_X = p.X;
                m_Start.m_Y = p.Y;
                Width = 1;
                Height = 1;
            }
            else
            {
                // add a new rect, with a one tile width and one tile height
                MakeHold(new Rectangle2D(p.X, p.Y, 1, 1));
            }
        }
        public void MakeHold(Rectangle2D r)
        {
            if (r.m_Start.m_X < m_Start.m_X)
                m_Start.m_X = r.m_Start.m_X;

            if (r.m_Start.m_Y < m_Start.m_Y)
                m_Start.m_Y = r.m_Start.m_Y;

            if (r.m_End.m_X > m_End.m_X)
                m_End.m_X = r.m_End.m_X;

            if (r.m_End.m_Y > m_End.m_Y)
                m_End.m_Y = r.m_End.m_Y;
        }

        public Point2D Center
        {
            get { return new Point2D((m_Start.m_X + Width / 2), (m_Start.m_Y + Height / 2)); }
        }

        public bool Contains(Point3D p)
        {
            return (m_Start.m_X <= p.m_X && m_Start.m_Y <= p.m_Y && m_End.m_X > p.m_X && m_End.m_Y > p.m_Y);
            //return ( m_Start <= p && m_End > p );
        }

        public bool Contains(Point2D p)
        {
            return (m_Start.m_X <= p.m_X && m_Start.m_Y <= p.m_Y && m_End.m_X > p.m_X && m_End.m_Y > p.m_Y);
            //return ( m_Start <= p && m_End > p );
        }

        public bool Contains(IPoint2D p)
        {
            return (m_Start <= p && m_End > p);
        }

        public override string ToString()
        {
            return String.Format("({0}, {1})+({2}, {3})", X, Y, Width, Height);
        }
    }

    [NoSort]
    [PropertyObject]
    public struct Rectangle3D
    {
        private Point3D m_Start;
        private Point3D m_End;

        public Rectangle3D(Point3D start, Point3D end)
        {
            m_Start = start;
            m_End = end;
        }

        public Rectangle3D(int x, int y, int z, int width, int height, int depth)
        {
            m_Start = new Point3D(x, y, z);
            m_End = new Point3D(x + width, y + height, z + depth);
        }

        [CommandProperty(AccessLevel.Counselor)]
        public Point3D Start
        {
            get
            {
                return m_Start;
            }
            set
            {
                m_Start = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public Point3D End
        {
            get
            {
                return m_End;
            }
            set
            {
                m_End = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int Width
        {
            get
            {
                return m_End.X - m_Start.X;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int Height
        {
            get
            {
                return m_End.Y - m_Start.Y;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int Depth
        {
            get
            {
                return m_End.Z - m_Start.Z;
            }
        }

        public bool Contains(Point3D p)
        {
            return (p.m_X >= m_Start.m_X)
                && (p.m_X < m_End.m_X)
                && (p.m_Y >= m_Start.m_Y)
                && (p.m_Y < m_End.m_Y)
                && (p.m_Z >= m_Start.m_Z)
                && (p.m_Z < m_End.m_Z);
        }

        public bool Contains(IPoint3D p)
        {
            return (p.X >= m_Start.m_X)
                && (p.X < m_End.m_X)
                && (p.Y >= m_Start.m_Y)
                && (p.Y < m_End.m_Y)
                && (p.Z >= m_Start.m_Z)
                && (p.Z < m_End.m_Z);
        }

        public override string ToString()
        {
            return String.Format("({0}, {1})+({2}, {3})", m_Start.m_X, m_Start.m_Y, Width, Height);
        }
    }
}