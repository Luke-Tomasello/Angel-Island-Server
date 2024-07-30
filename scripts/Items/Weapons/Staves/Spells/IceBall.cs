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

using Server.Targeting;
using System;

namespace Server.Spells.First
{
    public class IceBallSpell : Spell
    {
        public override bool ClearHandsOnCast { get { return false; } }

        private static SpellInfo m_Info = new SpellInfo(
                "Ice Ball", "The Staff Summons the Power for the incantation",
                SpellCircle.First,
                212,
                9041,
                Reagent.SulfurousAsh
            );

        public IceBallSpell(Mobile caster, Item scroll) : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public override bool DelayedDamage { get { return true; } }

        public void Target(Mobile m)
        {
            if (!Caster.CanSee(m))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (CheckHSequence(m))
            {
                Mobile source = Caster;

                SpellHelper.Turn(source, m);

                SpellHelper.CheckReflect((int)this.Circle, ref source, ref m);

                double damage;

                if (Core.RuleSets.AOSRules())
                {
                    damage = GetNewAosDamage(Utility.RandomMinMax(10, 15), 1, 4);
                }
                else
                {
                    damage = Utility.Random(4, 4);

                    if (CheckResisted(m))
                    {
                        damage *= GetResistScaler(0.75);

                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }

                    damage *= GetDamageScalar(m);
                }

                Effects.SendMovingEffect(source, m, 0x36E4, 7, 0, false, true, 0x480, 0);
                source.PlaySound(0x145);

                //source.MovingParticles( m, 0x36E4, 5, 0, false, true, 3006, 4006, 0 );
                //source.PlaySound( 0x1E5 );


                SpellHelper.Damage(TimeSpan.Zero, m, Caster, damage, 0, 0, 100, 0, 0);
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private IceBallSpell m_Owner;

            public InternalTarget(IceBallSpell owner) : base(12, false, TargetFlags.Harmful)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is Mobile)
                {
                    m_Owner.Target((Mobile)o);
                }
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}