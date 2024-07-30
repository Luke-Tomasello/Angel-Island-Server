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

/* Scripts/Mobiles/Monsters/Misc/Melee/GrotesqueBeast.cs
 * ChangeLog
 *  10/11/23, Yoar
 *      Initial commit.
 *      A plague beast with a gory death effect.
 */

using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [CorpseName("a grotesque beast corpse")]
    public class GrotesqueBeast : PlagueBeast
    {
        [Constructable]
        public GrotesqueBeast()
            : base()
        {
            Name = "a grotesque beast";
        }

        public override void OnDeath(Container c)
        {
            Explode(Location, Map, 1, 10, Utility.RandomMinMax(15, 20));

            base.OnDeath(c);
        }

        private const double TwoPi = 2 * Math.PI;

        public static void Explode(Point3D loc, Map map, int minDist, int maxDist, int count)
        {
            Explode(loc, map, minDist, maxDist, count, m_ExplodeTypes);
        }

        public static void Explode(Point3D loc, Map map, int minDist, int maxDist, int count, Type[] types)
        {
            for (int i = 0; i < count; i++)
            {
                Item item = Loot.Construct(types);

                if (item != null)
                    Throw(item, map, loc, Utility.RandomDouble() * TwoPi, Utility.RandomMinMax(minDist, maxDist));
            }
        }

        private static readonly Type[] m_ExplodeTypes = new Type[]
            {
                typeof( Blood ), typeof( Blood ), typeof( Blood ),
                typeof( Bone ), typeof( Bone ), typeof( Bone ),
                typeof( Head ),
                typeof( LeftArm ),
                typeof( LeftLeg ),
                typeof( RightArm ),
                typeof( RightLeg ),
                typeof( Skull ),
                typeof( Torso ),
            };

        private static void Throw(Item item, Map map, Point3D source, double angle, int distance)
        {
            Throw(item, map, source, GetTarget(map, source, angle, distance));
        }

        private static Point3D GetTarget(Map map, Point3D source, double angle, int distance)
        {
            if (map == null || map == Map.Internal)
                return source;

            Point2D[] line = Line2D(new Point2D(source), angle, distance);

            Point3D eyes = new Point3D(source.X, source.Y, source.Z + 14);

            for (int i = line.Length - 1; i >= 1; i--)
            {
                Point2D p = line[i];

                int z = source.Z;

                if (map.CanFit(p.X, p.Y, z, 5) && map.LineOfSight(eyes, new Point3D(p.X, p.Y, z)))
                    return new Point3D(p.X, p.Y, z);

                z = map.GetAverageZ(p.X, p.Y);

                if (map.CanFit(p.X, p.Y, z, 5) && map.LineOfSight(eyes, new Point3D(p.X, p.Y, z)))
                    return new Point3D(p.X, p.Y, z);
            }

            return source;
        }

        private static Point2D[] Line2D(Point2D source, double angle, int distance)
        {
            List<Point2D> list = new List<Point2D>();

            double dx = Math.Cos(angle);
            double dy = Math.Sin(angle);

            for (int i = 0; i < distance; i++)
            {
                int x = source.X + (int)Math.Round(i * dx);
                int y = source.Y + (int)Math.Round(i * dy);

                Point2D p = new Point2D(x, y);

                if (list.Count == 0 || list[list.Count - 1] != p)
                    list.Add(p);
            }

            return list.ToArray();
        }

        private static void Throw(Item item, Map map, Point3D source, Point3D target)
        {
            if (map == null || map == Map.Internal)
                return;

            if (source == target)
            {
                item.MoveToWorld(target, map);
                return;
            }

            item.Visible = false;
            item.MoveToWorld(target, map);

            int dx = target.X - source.X;
            int dy = target.Y - source.Y;
            int speed = 1;

            IEntity sourceEntity = EffectItem.Create(source, map, EffectItem.DefaultDuration);
            IEntity targetEntity = EffectItem.Create(target, map, EffectItem.DefaultDuration);

            Effects.SendMovingParticles(sourceEntity, targetEntity, item.ItemID, speed, 0, true, false, item.Hue, 0, 0, 0, 0, 0, 0);

            TimeSpan delay = TimeSpan.FromSeconds(0.2 * Math.Sqrt(dx * dx + dy * dy) / speed);

            new ThrowTimer(item, delay).Start();
        }

        private class ThrowTimer : Timer
        {
            private Item m_Item;

            public ThrowTimer(Item item, TimeSpan delay)
                : base(delay)
            {
                m_Item = item;
            }

            protected override void OnTick()
            {
                m_Item.Visible = true;
            }
        }

        public GrotesqueBeast(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}