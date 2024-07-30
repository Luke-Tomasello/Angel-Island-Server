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

/* Scripts/Spells/Third/WallOfStone.cs
 * CHANGELOG:
 *	02/01/07: Pix
 *		Now should ignore ghosts!
 */

using Server.Misc;
using Server.Targeting;
using System;

namespace Server.Spells.Third
{
    public class WallOfStoneSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Wall of Stone", "In Sanct Ylem",
                SpellCircle.Third,
                227,
                9011,
                false,
                Reagent.Bloodmoss,
                Reagent.Garlic
            );

        public WallOfStoneSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override bool CheckCast()
        {

            return base.CheckCast();
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public void Target(IPoint3D p)
        {
            if (!Caster.CanSee(p))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                SpellHelper.GetSurfaceTop(ref p);

                int dx = Caster.Location.X - p.X;
                int dy = Caster.Location.Y - p.Y;
                int rx = (dx - dy) * 44;
                int ry = (dx + dy) * 44;

                bool eastToWest;

                if (rx >= 0 && ry >= 0)
                {
                    eastToWest = false;
                }
                else if (rx >= 0)
                {
                    eastToWest = true;
                }
                else if (ry >= 0)
                {
                    eastToWest = true;
                }
                else
                {
                    eastToWest = false;
                }

                Effects.PlaySound(p, Caster.Map, 0x1F6);

                for (int i = -1; i <= 1; ++i)
                {
                    Point3D loc = new Point3D(eastToWest ? p.X + i : p.X, eastToWest ? p.Y : p.Y + i, p.Z);
                    bool canFit = SpellHelper.AdjustField(ref loc, Caster.Map, 22, true, true);

                    //Effects.SendLocationParticles( EffectItem.Create( loc, Caster.Map, EffectItem.DefaultDuration ), 0x376A, 9, 10, 5025 );

                    if (!canFit)
                        continue;

                    Item item = new InternalItem(loc, Caster.Map, Caster);

                    Effects.SendLocationParticles(item, 0x376A, 9, 10, 5025);

                    //new InternalItem( loc, Caster.Map, Caster );
                }
            }

            FinishSequence();
        }

        [DispellableField]
        private class InternalItem : Item
        {
            private Timer m_Timer;
            private DateTime m_End;

            public override bool BlocksFit { get { return true; } }

            public InternalItem(Point3D loc, Map map, Mobile caster)
                : base(0x80)
            {
                Visible = false;
                Movable = false;

                MoveToWorld(loc, map);

                if (caster.InLOS(this))
                    Visible = true;
                else
                    Delete();

                if (Deleted)
                    return;

                m_Timer = new InternalTimer(this, TimeSpan.FromSeconds(10.0));
                m_Timer.Start();

                m_End = DateTime.UtcNow + TimeSpan.FromSeconds(10.0);
            }

            public InternalItem(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)1); // version

                writer.WriteDeltaTime(m_End);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            m_End = reader.ReadDeltaTime();

                            m_Timer = new InternalTimer(this, m_End - DateTime.UtcNow);
                            m_Timer.Start();

                            break;
                        }
                    case 0:
                        {
                            TimeSpan duration = TimeSpan.FromSeconds(10.0);

                            m_Timer = new InternalTimer(this, duration);
                            m_Timer.Start();

                            m_End = DateTime.UtcNow + duration;

                            break;
                        }
                }
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                if (m_Timer != null)
                    m_Timer.Stop();
            }

            private class InternalTimer : Timer
            {
                private InternalItem m_Item;

                public InternalTimer(InternalItem item, TimeSpan duration)
                    : base(duration)
                {
                    Priority = TimerPriority.OneSecond;
                    m_Item = item;
                }

                protected override void OnTick()
                {
                    m_Item.Delete();
                }
            }
        }

        private class InternalTarget : Target
        {
            private WallOfStoneSpell m_Owner;

            public InternalTarget(WallOfStoneSpell owner)
                : base(12, true, TargetFlags.None)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is IPoint3D)
                    m_Owner.Target((IPoint3D)o);
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}