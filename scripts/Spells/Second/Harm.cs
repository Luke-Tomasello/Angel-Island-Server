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

/* Scripts\Spells\Second\Harm.cs
 * ChangeLog:
 *	4/30/23, Yoar
 *	    Now conditioning tuning consoles for AI/MO
 *	3/21/10, Adam
 *		scale min/max damage between - fine tuning with HarmMCi
 * 6/03/06, Kit
		Added damage type define
 *  5/15/06, Kit
 *		Added Min/Max damage define
	6/5/04, Pix
		Merged in 1.0RC0 code.
*/

using Server.Targeting;

namespace Server.Spells.Second
{
    public class HarmSpell : Spell
    {
        public override int MinDamage { get { return 1; } }
        public override int MaxDamage { get { return 15; } }
        public override SpellDamageType DamageType
        {
            get
            {
                return SpellDamageType.Energy;
            }
        }

        private static SpellInfo m_Info = new SpellInfo(
                "Harm", "An Mani",
                SpellCircle.Second,
                212,
                Core.RuleSets.AOSRules() ? 9001 : 9041,
                Reagent.Nightshade,
                Reagent.SpidersSilk
            );

        public HarmSpell(Mobile caster, Item scroll)
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
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)this.Circle, Caster, ref m);

                double damage;

                if (Core.RuleSets.AOSRules())
                {
                    damage = GetNewAosDamage(17, 1, 5);
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

                if (!m.InRange(Caster, 2))
                    damage *= 0.25; // 1/4 damage at > 2 tile range
                else if (!m.InRange(Caster, 1))
                    damage *= 0.50; // 1/2 damage at 2 tile range

                #region Tuning Console
                if (Server.Items.Consoles.TuningConsole.Enabled && Server.Items.Consoles.HarmMCi.UseNewScale == true)
                    damage = Utility.RescaleNumber(damage, 1, 15, Server.Items.Consoles.HarmMCi.DamageRangeLow, Server.Items.Consoles.HarmMCi.DamageRangeHigh);
                #endregion

                if (Core.RuleSets.AOSRules())
                {
                    m.FixedParticles(0x374A, 10, 30, 5013, 1153, 2, EffectLayer.Waist);
                    m.PlaySound(0x0FC);
                }
                else
                {
                    m.FixedParticles(0x374A, 10, 15, 5013, EffectLayer.Waist);
                    m.PlaySound(0x1F1);
                }

                SpellHelper.Damage(this, m, damage, 0, 0, 100, 0, 0);
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private HarmSpell m_Owner;

            public InternalTarget(HarmSpell owner)
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