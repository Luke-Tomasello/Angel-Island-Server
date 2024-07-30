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

/* Scripts/Engines/Invasion/GoodiesTimer.cs
 * ChangeLog
 *  10/28/23, Yoar
 *		Initial Version.
 */

using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Engines.Invasion
{
    public class GoodiesTimer : Timer
    {
        public static int DropGoodies(Point3D loc, Map map, int radius, int minTotal, int maxTotal, int randPerc = 10)
        {
            if (map == null)
                return 0;

            Point2D[] points = Disk(new Point2D(loc), radius);

            if (points.Length <= 0)
                return 0;

            int[] values = RandomValues(points.Length, minTotal, maxTotal, randPerc);

            int total = 0;

            for (int i = 0; i < points.Length; i++)
            {
                Point2D p = points[i];
                int value = values[i];

                if (value > 0)
                {
                    new GoodiesTimer(map, p.X, p.Y, value).Start();

                    total += value;
                }
            }

            return total;
        }

        private static Point2D[] Disk(Point2D center, int radius)
        {
            List<Point2D> list = new List<Point2D>();

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int dist = (int)Math.Round(Math.Sqrt(dx * dx + dy * dy));

                    if (dist <= radius)
                        list.Add(new Point2D(center.X + dx, center.Y + dy));
                }
            }

            return list.ToArray();
        }

        private static int[] RandomValues(int count, int minTotal, int maxTotal, int randPerc)
        {
            if (count <= 0)
                return new int[0];

            int total = Utility.RandomMinMax(minTotal, maxTotal);

            int randPart = randPerc * total / 100;
            int evenPart = total - randPart;

            int div = evenPart / count;
            int rem = evenPart % count;

            int[] sum = RandomSum(count, randPart);

            int[] result = new int[count];

            for (int i = 0; i < count; i++)
                result[i] = div + sum[i];

            result[Utility.Random(count)] += rem;

            return result;
        }

        private static int[] RandomSum(int count, int sum)
        {
            int[] intervals = new int[count + 1];

            intervals[0] = 0;
            intervals[count] = sum;

            for (int i = 1; i < count; i++)
                intervals[i] = Utility.RandomMinMax(0, sum);

            Array.Sort(intervals);

            int[] result = new int[count];

            for (int i = 0; i < count; i++)
                result[i] = intervals[i + 1] - intervals[i];

            return result;
        }

        private Map m_Map;
        private int m_X, m_Y;
        private int m_Value;

        public GoodiesTimer(Map map, int x, int y, int value)
            : base(TimeSpan.FromSeconds(Utility.RandomDouble() * 10.0))
        {
            m_Map = map;
            m_X = x;
            m_Y = y;
            m_Value = value;
        }

        protected override void OnTick()
        {
            int z = m_Map.GetAverageZ(m_X, m_Y);

            bool canFit = Utility.CanFit(m_Map, m_X, m_Y, z, 6, Utility.CanFitFlags.requireSurface);

            for (int dz = -5; !canFit && dz <= 5; dz++)
            {
                canFit = Utility.CanFit(m_Map, m_X, m_Y, z + dz, 6, Utility.CanFitFlags.requireSurface);

                if (canFit)
                    z += dz;
            }

            if (!canFit)
                return;

            Gold g = new Gold(m_Value);

            g.MoveToWorld(new Point3D(m_X, m_Y, z), m_Map);

            if (Utility.RandomBool())
            {
                switch (Utility.Random(3))
                {
                    case 0: // Fire column
                        {
                            Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
                            Effects.PlaySound(g, g.Map, 0x208);

                            break;
                        }
                    case 1: // Explosion
                        {
                            Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration), 0x36BD, 20, 10, 5044);
                            Effects.PlaySound(g, g.Map, 0x307);

                            break;
                        }
                    case 2: // Ball of fire
                        {
                            Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration), 0x36FE, 10, 10, 5052);

                            break;
                        }
                }
            }
        }
    }
}