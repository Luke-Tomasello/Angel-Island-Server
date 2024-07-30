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

/* Changelog
 * 02/25/05 TK
 *		Temporarily changed resist to 100% for to merge without letting spells work -
 *		more tweaking is needed.
 * 02/24/05 TK
 *		Fixed mana drain damage and resist calculations.
 */

using Server.Targeting;
using System;
using System.Collections;

namespace Server.Spells.Fourth
{
    public class ManaDrainSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Mana Drain", "Ort Rel",
                SpellCircle.Fourth,
                215,
                9031,
                Reagent.BlackPearl,
                Reagent.MandrakeRoot,
                Reagent.SpidersSilk
            );

        public ManaDrainSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        private Hashtable m_Table = new Hashtable();

        private void AosDelay_Callback(object state)
        {
            object[] states = (object[])state;

            Mobile m = (Mobile)states[0];
            int mana = (int)states[1];

            if (m.Alive && !m.IsDeadBondedPet)
            {
                m.Mana += mana;

                m.FixedEffect(0x3779, 10, 25);
                m.PlaySound(0x28E);
            }

            m_Table.Remove(m);
        }

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

                if (m.Spell != null)
                    m.Spell.OnCasterHurt();

                m.Paralyzed = false;

                if (Core.RuleSets.AOSRules())
                {
                    int toDrain = 40 + (int)(GetDamageSkill(Caster) - GetResistSkill(m));

                    if (toDrain < 0)
                        toDrain = 0;
                    else if (toDrain > m.Mana)
                        toDrain = m.Mana;

                    if (m_Table.Contains(m))
                        toDrain = 0;

                    m.FixedParticles(0x3789, 10, 25, 5032, EffectLayer.Head);
                    m.PlaySound(0x1F8);

                    if (toDrain > 0)
                    {
                        m.Mana -= toDrain;

                        m_Table[m] = Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerStateCallback(AosDelay_Callback), new object[] { m, toDrain });
                    }
                }
                else
                {
                    if (CheckResisted(m))
                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    else
                    {
                        // int damage = 30 * (Caster.Magery / 100) + 10 * ((Caster.Eval - Target.Resist) / 100) + Random(-5, 5)
                        int damage = (int)(.3 * Caster.Skills[SkillName.Magery].Value + .1 * (Caster.Skills[SkillName.EvalInt].Value - m.Skills[SkillName.MagicResist].Value) + Utility.Random(-5, 10));
                        //m.PublicOverheadMessage(MessageType.System, 0, true, damage.ToString() + " dmg");
                        m.Paralyzed = false;
                        m.Mana -= damage;
                    }

                    m.FixedParticles(0x374A, 10, 15, 5032, EffectLayer.Head);
                    m.PlaySound(0x1F8);
                }
            }

            FinishSequence();
        }

        public override double GetResistPercent(Mobile target)
        {
            // Target.Resist * (100 - ((8 / Spell.Circle) / 10) * (Caster.Magery + Caster.Eval))
            double chance = target.Skills[SkillName.MagicResist].Value * .01 * (100 - .2 * (Caster.Skills[SkillName.Magery].Value + Caster.Skills[SkillName.EvalInt].Value));
            //target.PublicOverheadMessage(MessageType.Label, 0, true, chance.ToString() + "% to resist");
            return 100.0;//chance;
        }

        private class InternalTarget : Target
        {
            private ManaDrainSpell m_Owner;

            public InternalTarget(ManaDrainSpell owner)
                : base(12, false, TargetFlags.Harmful)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is Mobile)
                    m_Owner.Target((Mobile)o);
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}