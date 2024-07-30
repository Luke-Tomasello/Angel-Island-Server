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
 * 5/12/23, Yoar
 *      Conditioned reverse mechanics for AI/MO
 * 02/25/05 TK
 *		Temporarily changed resist to 100% for to merge without letting spells work -
 *		more tweaking is needed.
 * 02/24/05 TK
 *		Fixed mana vampire damage and resist calculations. 
	6/5/04, Pix
		Merged in 1.0RC0 code.
*/

using Server.Targeting;

namespace Server.Spells.Seventh
{
    public class ManaVampireSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Mana Vampire", "Ort Sanct",
                SpellCircle.Seventh,
                221,
                9032,
                Reagent.BlackPearl,
                Reagent.Bloodmoss,
                Reagent.MandrakeRoot,
                Reagent.SpidersSilk
            );

        public ManaVampireSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
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

                int toDrain = 0;
                bool reverse = false;

                if (Core.RuleSets.AOSRules())
                {
                    toDrain = (int)(GetDamageSkill(Caster) - GetResistSkill(m));

                    if (!m.Player)
                        toDrain /= 2;

                    if (toDrain < 0)
                        toDrain = 0;
                    else if (toDrain > m.Mana)
                        toDrain = m.Mana;
                }
                else
                {
                    if (CheckResisted(m))
                    {
                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.

                        if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules())
                        {
                            // double reversechance = 12.5 * ((Target.Magery + Target.Eval - Caster.Magery - Caster.Eval) / 100) + 12.5;
                            double reversechance = (.125 * .01 * (m.Skills[SkillName.Magery].Value + m.Skills[SkillName.EvalInt].Value - Caster.Skills[SkillName.Magery].Value - Caster.Skills[SkillName.EvalInt].Value) + .125);
                            double rand = Utility.RandomDouble();
                            //m.PublicOverheadMessage(MessageType.Label, 0, true, (reversechance * 100).ToString() + "% to reverse, rolled " + rand.ToString("P"));
                            if (rand < reversechance)
                            {
                                reverse = true;
                                // int damage = 40 * (Caster.Magery / 100) + 22.5 * ((Caster.Eval - Target.Resist) / 100) - 7.5 + Random(-5, 5)
                                // damage += 30
                                //toDrain = (int)(30 + .4 * m.Skills[SkillName.Magery].Value + .225 * (m.Skills[SkillName.EvalInt].Value - Caster.Skills[SkillName.MagicResist].Value) - 7.5 + Utility.Random(-5, 10));
                            }
                        }
                    }
                    else
                        // int damage = 40 * (Caster.Magery / 100) + 22.5 * ((Caster.Eval - Target.Resist) / 100) - 7.5 + Random(-5, 5)
                        // damage += 30
                        toDrain = (int)(30 + .4 * Caster.Skills[SkillName.Magery].Value + .225 * (Caster.Skills[SkillName.EvalInt].Value - m.Skills[SkillName.MagicResist].Value) - 7.5 + Utility.Random(-5, 10));
                }

                Mobile cast = Caster, targ = m;
                if (reverse)
                {
                    cast = m;
                    targ = Caster;
                }

                if (toDrain > (cast.ManaMax - cast.Mana))
                    toDrain = cast.ManaMax - cast.Mana;
                if (reverse && toDrain < 20)
                    toDrain = 20;
                if (toDrain > targ.Mana)
                    toDrain = targ.Mana;

                //targ.PublicOverheadMessage(MessageType.Label, 0, true, toDrain.ToString() + " dmg");

                targ.Mana -= toDrain;
                cast.Mana += toDrain;

                if (Core.RuleSets.AOSRules())
                {
                    m.FixedParticles(0x374A, 1, 15, 5054, 23, 7, EffectLayer.Head);
                    m.PlaySound(0x1F9);

                    Caster.FixedParticles(0x0000, 10, 5, 2054, EffectLayer.Head);
                }
                else
                {
                    m.FixedParticles(0x374A, 10, 15, 5054, EffectLayer.Head);
                    m.PlaySound(0x1F9);
                }
            }

            FinishSequence();
        }

        public override double GetResistPercent(Mobile target)
        {
            // Target.Resist * (100 - ((8 / Spell.Circle) / 10) * (Caster.Magery + Caster.Eval))
            double chance = target.Skills[SkillName.MagicResist].Value * .01 * (100 - .11 * (Caster.Skills[SkillName.Magery].Value + Caster.Skills[SkillName.EvalInt].Value));
            //target.PublicOverheadMessage(MessageType.Label, 0, true, chance.ToString() + "% to resist");
            return 100.0;//chance;
        }

        private class InternalTarget : Target
        {
            private ManaVampireSpell m_Owner;

            public InternalTarget(ManaVampireSpell owner)
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