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

/* Scripts/Spells/Base/Spell.cs
 * 	ChangeLog:
 *	4/30/23, Yoar
 *	    Now conditioning tuning consoles for AI/MO
 *	3/21/10, Adam
 *		adjust damage delay - fine tuning with MagicArrowMCi
 * 	6/03/06, Kit
 * 		Added damage type define
 * 	5/15/06, Kit
 * 		Added Min/Max defines
 * 	7/4/04, Pix
 * 		Added caster to SpellHelper.Damage call so corpse wouldn't stay blue to this caster
 * 	6/5/04, Pix
 * 		Merged in 1.0RC0 code.
 * 	4/01/04 changes by merxypoo:
 * 		Changed DamageDelay to 0.65
 * 	3/25/04 changes by smerX:
 * 		Changed DamageDelay to 0.35
 * 	3/18/04 code changes by smerX:
 * 		Added 0.3 second DamageDelay
 */
using Server.Targeting;
using System;

namespace Server.Spells.First
{
    public class MagicArrowSpell : Spell
    {
        public override int MinDamage { get { return 4; } }
        public override int MaxDamage { get { return 4; } }
        public override SpellDamageType DamageType
        {
            get
            {
                return SpellDamageType.Energy;
            }
        }

        private static SpellInfo m_Info = new SpellInfo(
                "Magic Arrow", "In Por Ylem",
                SpellCircle.First,
                212,
                9041,
                Reagent.SulfurousAsh
            );

        public MagicArrowSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public override bool DelayedDamage { get { return false; } }

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
                    damage = GetNewAosDamage(10, 1, 4);
                }
                else
                {
                    damage = Utility.Random(MinDamage, MaxDamage);

                    if (CheckResisted(m))
                    {
                        damage *= GetResistScaler(0.75);

                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }

                    damage *= GetDamageScalar(m);
                }

                source.MovingParticles(m, 0x36E4, 5, 0, false, true, 3006, 4006, 0);
                source.PlaySound(0x1E5);

                TimeSpan delay = TimeSpan.Zero;

                #region Tuning Console
                if (Server.Items.Consoles.TuningConsole.Enabled)
                    delay = TimeSpan.FromSeconds(Server.Items.Consoles.MagicArrowMCi.DamageDelay);
                #endregion

                SpellHelper.Damage(delay, m, Caster, damage, 0, 100, 0, 0, 0);
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private MagicArrowSpell m_Owner;

            public InternalTarget(MagicArrowSpell owner)
                : base(12, false, TargetFlags.Harmful)
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