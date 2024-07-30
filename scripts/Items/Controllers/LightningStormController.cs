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

/* Items/misc/LightningStormController.cs
 * CHANGELOG:
 *  3/25/2024, Adam
 *      1. Add support for water tiles
 *      2. Turn off blessedness
 * 	1/4/24, Yoar
 * 		Initial version. Used to spawn lightning storms.
 */

using System;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Items
{
    public class LightningStormController : Item
    {
        public override string DefaultName { get { return "Lightning Storm Controller"; } }

        private int m_Range;
        private int m_MinCount;
        private int m_MaxCount;
        private TimeSpan m_MinDelay;
        private TimeSpan m_MaxDelay;
        private Timer m_Timer;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range
        {
            get { return m_Range; }
            set { m_Range = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MinCount
        {
            get { return m_MinCount; }
            set { m_MinCount = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxCount
        {
            get { return m_MaxCount; }
            set { m_MaxCount = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan MinDelay
        {
            get { return m_MinDelay; }
            set { m_MinDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan MaxDelay
        {
            get { return m_MaxDelay; }
            set { m_MaxDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Running
        {
            get { return m_Timer.Running; }
            set
            {
                if (Running != value)
                {
                    if (value)
                        StartTimer();
                    else
                        StopTimer();
                }
            }
        }

        [Constructable]
        public LightningStormController()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_Range = 30;
            m_MinCount = 2;
            m_MaxCount = 12;
            m_MinDelay = TimeSpan.FromSeconds(0.5);
            m_MinDelay = TimeSpan.FromSeconds(5.0);

            m_Timer = new InternalTimer(this);
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        private void OnTick()
        {
            int count = Utility.RandomMinMax(m_MinCount, m_MaxCount);

            DoStorm(GetWorldLocation(), Map, m_Range, count);

            StartTimer();
        }

        private void StartTimer()
        {
            StopTimer();

            m_Timer.Delay = ComputeDelay();

            m_Timer.Start();
        }

        private void StopTimer()
        {
            m_Timer.Stop();
        }

        private TimeSpan ComputeDelay()
        {
            if (m_MinDelay == m_MaxDelay)
                return m_MinDelay;

            double secondsMin = m_MinDelay.TotalSeconds;
            double secondsMax = m_MaxDelay.TotalSeconds;

            double seconds = secondsMin + Utility.RandomDouble() * (secondsMax - secondsMin);

            return TimeSpan.FromSeconds(seconds);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            StopTimer();
        }

        public LightningStormController(Serial serial)
            : base(serial)
        {
            m_Timer = new InternalTimer(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_Range);
            writer.Write((int)m_MinCount);
            writer.Write((int)m_MaxCount);
            writer.Write((TimeSpan)m_MinDelay);
            writer.Write((TimeSpan)m_MaxDelay);
            writer.Write((bool)Running);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            bool running = false;

            switch (version)
            {
                case 0:
                    {
                        m_Range = reader.ReadInt();
                        m_MinCount = reader.ReadInt();
                        m_MaxCount = reader.ReadInt();
                        m_MinDelay = reader.ReadTimeSpan();
                        m_MaxDelay = reader.ReadTimeSpan();
                        running = reader.ReadBool();
                        break;
                    }
            }

            if (running)
                StartTimer();
        }

        private class InternalTimer : Timer
        {
            private LightningStormController m_Controller;

            public InternalTimer(LightningStormController controller)
                : base(TimeSpan.Zero, TimeSpan.Zero, 1)
            {
                m_Controller = controller;

                Priority = TimerPriority.FiftyMS;
            }

            protected override void OnTick()
            {
                m_Controller.OnTick();
            }
        }

        public static void DoStorm(Point3D loc, Map map, int range, int count)
        {
            if (map == null || map == Map.Internal)
                return;

            HashSet<Point2D> struck = new HashSet<Point2D>();

            for (int i = 0; i < count; i++)
            {
                Point3D strike = GetRandomLocation(loc, map, range);

                if (strike == Point3D.Zero || !struck.Add(new Point2D(strike)))
                    continue;

                DoStrike(strike, map);
            }
        }

        private static Point3D GetRandomLocation(Point3D loc, Map map, int range)
        {
            for (int j = 0; j < 10; j++)
            {
                int x = loc.X + Utility.RandomMinMax(-range, range);
                int y = loc.Y + Utility.RandomMinMax(-range, range);
                int z = loc.Z;

                if (map.CanFit(x, y, z, 16, false, true, true))
                    return new Point3D(x, y, z);

                z = map.GetAverageZ(x, y);

                if (map.CanFit(x, y, z, 16, false, true, true))
                    return new Point3D(x, y, z);
            }

            // 3/25/2024, Adam: Add support for water tiles.
            //  We could go with this algo only, but the above might be faster for the land cases.
            Point3D px = Mobiles.Spawner.GetSpawnPosition(map, loc, homeRange: range, SpawnFlags.None /*SpawnFlags.SpawnFar*/, null);
            if (px != loc)
                return new Point3D(px.X, px.Y, px.Z);

            return Point3D.Zero;
        }

        public static void DoStrike(Point3D loc, Map map)
        {
            if (map == null || map == Map.Internal || !map.GetSector(loc).Active)
                return;

            Dummy.PlaceAt(loc, map, TimeSpan.FromSeconds(1.0)).BoltEffect(0);
        }

        private class Dummy : Mobile
        {
            private static readonly Dummy[] m_Cache = new Dummy[0x200];
            private static int m_Index = -1;

            public static Mobile PlaceAt(Point3D loc, Map map, TimeSpan duration)
            {
                Dummy dummy = null;

                for (int i = 0; i < m_Cache.Length; i++)
                {
                    if (++m_Index == m_Cache.Length)
                        m_Index = 0;

                    Dummy check = m_Cache[m_Index];

                    if (check == null || check.Deleted)
                        m_Cache[m_Index] = check = new Dummy(true);

                    if (!check.InUse)
                    {
                        dummy = check;
                        break;
                    }
                }

                if (dummy == null)
                    dummy = new Dummy(false);

                dummy.MoveToWorld(loc, map);

                dummy.StartTimer(duration);

                return dummy;
            }

            public override bool IsDeadBondedPet { get { return true; } }

            private bool m_Cached;
            private Timer m_Timer;

            private bool InUse { get { return (m_Timer != null); } }

            private Dummy(bool cached)
                : base()
            {
                BodyValue = 0;
                //Blessed = true;
                Frozen = true;

                m_Cached = cached;

                MoveToIntStorage(false);
            }

            private void OnTick()
            {
                if (m_Cached)
                    MoveToIntStorage(false);
                else
                    Delete();

                StopTimer();
            }

            private void StartTimer(TimeSpan duration)
            {
                StopTimer();

                m_Timer = Timer.DelayCall(duration, OnTick);
            }

            private void StopTimer()
            {
                if (m_Timer != null)
                {
                    m_Timer.Stop();
                    m_Timer = null;
                }
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                StopTimer();
            }

            public Dummy(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.WriteEncodedInt(0); // version
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadEncodedInt();

                Delete();
            }
        }
    }
}