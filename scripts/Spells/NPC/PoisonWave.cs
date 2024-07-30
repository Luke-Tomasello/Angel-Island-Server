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

/* ChangeLog:
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 * 8/22/06, Pix
 *		Removed IOBEquipped requirement.
 *	7/13/04 Created by smerX
 */

using Server.Items;
using Server.Misc;
using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Spells.NPC
{
    public class PoisonWaveSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Poison Wave", "Ara In Nox",
                SpellCircle.Third,
                230,
                9052,
                false,
                Reagent.BlackPearl,
                Reagent.Nightshade,
                Reagent.SpidersSilk
            );

        private TimeSpan GetDelay
        {
            get
            {
                if (Utility.RandomDouble() >= 0.33)
                    return TimeSpan.FromSeconds(0.25);
                else if (Utility.RandomDouble() >= 50)
                    return TimeSpan.FromSeconds(0.50);
                else
                    return TimeSpan.FromSeconds(0.75);
            }
        }

        private int GetID
        {
            get
            {
                if (Utility.RandomDouble() >= 0.50)
                    return 0x3915;
                else
                    return 0x3922;
            }
        }

        public PoisonWaveSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Point3D p = new Point3D(Caster.X, Caster.Y, Caster.Z);

            if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                DoEffect();
                FinishSequence();
            }
        }

        private void DoEffect()
        {
            IPooledEnumerable eable = Caster.GetMobilesInRange(4);
            foreach (Mobile m in eable)
            {
                if (m != null && m is PlayerMobile && m.Alive && m.AccessLevel == AccessLevel.Player)
                {
                    PlayerMobile pm = (PlayerMobile)m;
                    if ( /*pm.IOBEquipped &&*/ pm.IOBAlignment == IOBAlignment.Council)
                        continue;

                    if (!m.Poisoned && !m.Hidden && Utility.RandomDouble() <= 0.80)
                    {
                        if (Utility.RandomDouble() <= 0.03)
                        {
                            m.Poison = Poison.Deadly;
                        }
                        else
                        {
                            m.Poison = Poison.Greater;
                        }

                        //Caster.DoHarmful( m );

                        m.FixedParticles(0x374A, 10, 15, 5021, EffectLayer.Waist);
                        m.PlaySound(0x474);

                    }
                }
            }
            eable.Free();
        }

        [DispellableField]
        private class InternalItem : Item
        {
            private Timer m_Timer;
            private DateTime m_End;
            private Mobile m_Caster;

            public override bool BlocksFit { get { return true; } }

            public InternalItem(int itemID, Point3D loc, Mobile caster, Map map, TimeSpan duration, int val)
                : base(itemID)
            {
                bool canFit = SpellHelper.AdjustField(ref loc, map, 12, false);

                Visible = false;
                Movable = false;
                Light = LightType.Circle300;

                MoveToWorld(loc, map);

                m_Caster = caster;

                m_End = DateTime.UtcNow + duration;

                m_Timer = new InternalTimer(this, duration, caster.InLOS(this), canFit);
                m_Timer.Start();
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                if (m_Timer != null)
                    m_Timer.Stop();
            }

            public InternalItem(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)1); // version

                writer.Write(m_Caster);
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
                            m_Caster = reader.ReadMobile();

                            goto case 0;
                        }
                    case 0:
                        {
                            m_End = reader.ReadDeltaTime();

                            m_Timer = new InternalTimer(this, TimeSpan.Zero, true, true);
                            m_Timer.Start();

                            break;
                        }
                }
            }

            private class InternalTimer : Timer
            {
                private InternalItem m_Item;
                private bool m_InLOS, m_CanFit;

                private static Queue m_Queue = new Queue();

                public InternalTimer(InternalItem item, TimeSpan delay, bool inLOS, bool canFit)
                    : base(delay, TimeSpan.FromSeconds(1.5))
                {
                    m_Item = item;
                    m_InLOS = inLOS;
                    m_CanFit = canFit;

                    Priority = TimerPriority.TwentyFiveMS;
                }

                protected override void OnTick()
                {
                    if (m_Item.Deleted)
                        return;

                    if (!m_Item.Visible)
                    {
                        if (m_InLOS && m_CanFit)
                            m_Item.Visible = true;
                        else
                            m_Item.Delete();

                        if (!m_Item.Deleted)
                        {
                            m_Item.ProcessDelta();
                            Effects.SendLocationParticles(EffectItem.Create(m_Item.Location, m_Item.Map, EffectItem.DefaultDuration), 0x376A, 9, 10, 5040);
                        }
                    }
                    else if (DateTime.UtcNow > m_Item.m_End)
                    {
                        m_Item.Delete();
                        Stop();
                    }
                }
            }
        }
    }
}