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

/* Scripts\Items\Misc\Waypoint.cs
 * ChangeLog
 *	4/22/08, Adam
 *		Add fail-safe 10 minute delete timer incase the owner never OnMoveOver the waypoint (for whatever reason)
 *  4/20/08, Adam
 *		Added an auto-destruct version of the WayPoint, AutoWayPoint
 *			See Groundskeeper
 */

using Server.Targeting;
using System;

namespace Server.Items
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class WayPoint : Item
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("WayPointSeq", AccessLevel.GameMaster, new CommandEventHandler(WayPointSeq_OnCommand));
        }

        public static void WayPointSeq_OnCommand(CommandEventArgs arg)
        {
            arg.Mobile.SendMessage("Target the position of the first way point.");
            arg.Mobile.Target = new WayPointSeqTarget(null);
        }

        private WayPoint m_Next;

        [Constructable]
        public WayPoint()
            : base(0x1f14)
        {
            this.Hue = 0x498;
            this.Visible = false;
            this.Name = "AI Way Point";
            //this.Movable = false;
        }

        public WayPoint(WayPoint prev)
            : this()
        {
            if (prev != null)
                prev.NextPoint = this;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WayPoint NextPoint
        {
            get
            {
                return m_Next;
            }
            set
            {
                if (m_Next != this)
                    m_Next = value;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                from.SendMessage("Target the next way point in the sequence.");

                from.Target = new NextPointTarget(this);
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Next == null)
                LabelTo(from, "(Unlinked)");
            else
                LabelTo(from, "(Linked: {0})", m_Next.Location);
        }

        public WayPoint(Serial serial)
            : base(serial)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Next = reader.ReadItem() as WayPoint;
                        break;
                    }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

            writer.Write(m_Next);
        }
    }

    public class AutoWayPoint : WayPoint
    {
        private DeleteTimer m_Timer;

        [Constructable]
        public AutoWayPoint()
            : base()
        {
            // start the autodelete timer
            ResetTimer();
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m is Server.Mobiles.BaseCreature)
            {   // when we get to the AutoWayPoint
                if ((m as Server.Mobiles.BaseCreature).CurrentWayPoint == this)
                {   // and if the BaseCreatures CurrentWayPoint == this, delete it!
                    (m as Server.Mobiles.BaseCreature).CurrentWayPoint = null;
                    this.Delete();
                }
            }
            return base.OnMoveOver(m);
        }

        public AutoWayPoint(Serial serial)
            : base(serial)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            ResetTimer();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        private class DeleteTimer : Timer
        {
            private AutoWayPoint m_waypoint;

            public DeleteTimer(AutoWayPoint waypoint, DateTime time)
                : base(time - DateTime.UtcNow)
            {
                m_waypoint = waypoint;
                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                if (m_waypoint != null && m_waypoint.Deleted == false)
                    m_waypoint.Delete();
            }
        }

        private void ResetTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
            // auto delete after 10 minutes?
            m_Timer = new DeleteTimer(this, DateTime.UtcNow + TimeSpan.FromMinutes(10));
            m_Timer.Start();
        }
    }

    public class NextPointTarget : Target
    {
        private WayPoint m_Point;

        public NextPointTarget(WayPoint pt)
            : base(-1, false, TargetFlags.None)
        {
            m_Point = pt;
        }

        protected override void OnTarget(Mobile from, object target)
        {
            if (target is WayPoint && m_Point != null)
            {
                m_Point.NextPoint = (WayPoint)target;
            }
            else
            {
                from.SendMessage("Target a way point.");
            }
        }
    }

    public class WayPointSeqTarget : Target
    {
        private WayPoint m_Last;

        public WayPointSeqTarget(WayPoint last)
            : base(-1, true, TargetFlags.None)
        {
            m_Last = last;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is WayPoint)
            {
                if (m_Last != null)
                    m_Last.NextPoint = (WayPoint)targeted;
            }
            else if (targeted is IPoint3D)
            {
                Point3D p = new Point3D((IPoint3D)targeted);

                WayPoint point = new WayPoint(m_Last);
                point.MoveToWorld(p, from.Map);

                from.Target = new WayPointSeqTarget(point);
                from.SendMessage("Target the position of the next way point in the sequence, or target a way point link the newest way point to.");
            }
            else
            {
                from.SendMessage("Target a position, or another way point.");
            }
        }
    }
}