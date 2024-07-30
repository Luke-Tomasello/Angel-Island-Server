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

/* Scripts\Spells\Third\Poison.cs
 * ChangeLog:
 *	4/30/23, Yoar
 *	    Now conditioning tuning consoles for AI/MO
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *	5/27/10, adam
 *		Change the chance to poison from 10% chance to 18.21% per Akarius
 *	3/18/10, adam
 *		Added ability to adjust resistability via the PoisonStickMCi (console)
 *  2/12/07 Taran Kain
 *		Removed WW test and logging code.
 * 10/19/06 Taran Kain
 *		Fixed mathematical error in WW test.
 * 10/17/06 Taran Kain
 *		Added logging code and Wald-Wolfowitz test ([poisontest) to see if we do or do not have a RNG problem.
 *  7/4/04, Pix
 *		Added Damage call so that the caster will be added to the target's aggressors list.
	6/5/04, Pix
		Merged in 1.0RC0 code.
*/

using Server.Targeting;
using System;

namespace Server.Spells.Third
{
    public class PoisonSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Poison", "In Nox",
                SpellCircle.Third,
                203,
                9051,
                Reagent.Nightshade
            );

        public PoisonSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override double GetResistPercent(Mobile target)
        {
            SpellCircle circle = Circle;

            #region Tuning Console
            if (Server.Items.Consoles.TuningConsole.Enabled)
                circle = Server.Items.Consoles.PoisonStickMCi.SpellCircle;
            #endregion

            return GetResistPercentForCircle(target, circle);
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

                if (CheckResisted(m))
                {
                    m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                }
                else
                {
                    int level;

                    if (Core.RuleSets.AOSRules())
                    {
                        if (Caster.InRange(m, 2))
                        {
                            int total = (Caster.Skills.Magery.Fixed + Caster.Skills.Poisoning.Fixed) / 2;

                            if (total >= 1000)
                                level = 3;
                            else if (total > 850)
                                level = 2;
                            else if (total > 650)
                                level = 1;
                            else
                                level = 0;
                        }
                        else
                        {
                            level = 0;
                        }
                    }
                    else
                    {
                        //double total = Caster.Skills[SkillName.Magery].Value + Caster.Skills[SkillName.Poisoning].Value;

                        #region Dueling
                        double total = Caster.Skills[SkillName.Magery].Value;

                        if (Caster is Mobiles.PlayerMobile)
                        {
                            Mobiles.PlayerMobile pm = (Mobiles.PlayerMobile)Caster;

                            if (pm.DuelContext != null && pm.DuelContext.Started && !pm.DuelContext.Finished && !pm.DuelContext.Ruleset.GetOption("Skills", "Poisoning"))
                            {
                            }
                            else
                            {
                                total += Caster.Skills[SkillName.Poisoning].Value;
                            }
                        }
                        else
                        {
                            total += Caster.Skills[SkillName.Poisoning].Value;
                        }
                        #endregion

                        double dist = Caster.GetDistanceToSqrt(m);

                        if (dist >= 3.0)
                            total -= (dist - 3.0) * 10.0;
                        // adam: change from 10% to 18.21% per Akarius' recomendations
                        if (total >= 200.0 && (Core.RuleSets.AOSRules() || Utility.RandomChance(18.21)))
                            level = 3;
                        else if (total > (Core.RuleSets.AOSRules() ? 170.1 : 170.0))
                            level = 2;
                        else if (total > (Core.RuleSets.AOSRules() ? 130.1 : 130.0))
                            level = 1;
                        else
                            level = 0;
                    }

                    //Pix- 7/4/04 - this assures that the caster is added to the target's aggressors list.
                    SpellHelper.Damage(TimeSpan.FromSeconds(0.1), m, Caster, 1, 0, 0, 0, 100, 0);

                    m.ApplyPoison(Caster, Poison.GetPoison(level));
                }

                m.FixedParticles(0x374A, 10, 15, 5021, EffectLayer.Waist);
                m.PlaySound(0x474);
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private PoisonSpell m_Owner;

            public InternalTarget(PoisonSpell owner)
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