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

/* Spells/Second/Cure.cs
 * CHANGELOG:
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *  11/07/05, Kit
 *		Restored former cure rates.
 *	10/16/05, Pix
 *		Change to chance to cure.
	6/5/04, Pix
		Merged in 1.0RC0 code.
 *  6/4/04, Pixie
 *		Added debugging for cure chance for people > playerlevel
 *	5/25/04, Pixie
 *		Changed formula for success curing poison
 *	5/22/04, Pixie
 *		Made it so chance to cure poison was based on the caster's magery vs the level of poison
 */

using Server.Targeting;
using System;

namespace Server.Spells.Second
{
    public class CureSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Cure", "An Nox",
                SpellCircle.Second,
                212,
                9061,
                Reagent.Garlic,
                Reagent.Ginseng
            );

        public CureSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override bool CheckCast()
        {
            #region Dueling
            if (Engines.ConPVP.DuelContext.CheckSuddenDeath(Caster))
            {
                Caster.SendMessage(0x22, "You cannot cast this spell when in sudden death.");
                return false;
            }
            #endregion

            return base.CheckCast();
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
            else if (CheckBSequence(m))
            {
                SpellHelper.Turn(Caster, m);
                //chance to cure poison is ((caster's magery/poison level) - 20%)

                if (m.Poison != null)
                {
                    double chance = 100;
                    try //I threw this try-catch block in here because Poison is whacky... there'll be a tiny 
                    {   //race condition if multiple people are casting cure on the same target... 
                        chance = ((Caster.Skills[SkillName.Magery].Value / (m.Poison.Level + 1)) - 20) * 7.5;
                        //this gives 0 chance for a mage to cure lethal
                        // 37.5% chance at GM to cure deadly ( needs > 80 for a chance )
                        // 100% chance at GM to cure greater ( needs > 60 for a chance )
                        // 100% chance at GM to cure regular ( needs > 40 for a chance )
                        // 100% chance at GM to cure lesser ( needs > 20 for a chance )

                        if (Caster.AccessLevel > AccessLevel.Player)
                        {
                            Caster.SendMessage("Chance to cure is " + chance + "%");
                        }
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                    //new cure rate code
                    /*
                    Poison p = m.Poison;
                    if( p != null )
                    {
                        int chance = 100;
                        try
                        {
                            chance = 10000 
                                         + (int)(Caster.Skills[SkillName.Magery].Value * 75) 
                                         - ((p.Level + 1) * 1750);
                            chance /= 100;
                            if( p.Level == 3 ) //DP tweak
                            {
                                chance -= 15; //@GM Magery, chance will be 90%
                            }
                            if( p.Level > 3 ) //lethal poison further penalty
                            {
                                chance -= 50; //@GM Magery, chance will be 37%
                            }

                            if( Caster.AccessLevel > AccessLevel.Player )
                            {
                                Caster.SendMessage("Chance to cure is " + chance + "%");
                            }
                        }
                        catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                        */
                    if (!m.Incurable && Utility.Random(0, 100) <= chance)
                    {
                        if (m.CurePoison(Caster))
                        {
                            if (Caster != m)
                                Caster.SendLocalizedMessage(1010058); // You have cured the target of all poisons!

                            m.SendLocalizedMessage(1010059); // You have been cured of all poisons.
                        }
                    }
                    else
                    {
                        Caster.SendLocalizedMessage(1010060); // You have failed to cure your target!
                    }
                }

                m.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
                m.PlaySound(0x1E0);
            }

            FinishSequence();
        }

        public class InternalTarget : Target
        {
            private CureSpell m_Owner;

            public InternalTarget(CureSpell owner)
                : base(12, false, TargetFlags.Beneficial)
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