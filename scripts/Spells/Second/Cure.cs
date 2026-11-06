/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

        public CureSpell(Mobile caster, Item? scroll)
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

                // --- Cure attempt (OSI formula) ---
                Poison p = m.Poison;

                if (p != null)
                {
                    // OSI formula (commonly documented):
                    // % Chance to cure = (Magery * 0.75) + (110 - (Poison level * 33))
                    // Poison level here is conventionally 1..5, so map Poison.Level (0..4) -> 1..5.
                    double magery = Caster.Skills[SkillName.Magery].Value;
                    int poisonLevel = p.Level + 1;

                    double chance = (magery * 0.75) + (110 - (poisonLevel * 33));

                    // clamp to [0,100]
                    if (chance < 0.0) chance = 0.0;
                    else if (chance > 100.0) chance = 100.0;

                    if (Caster.AccessLevel > AccessLevel.Player)
                        Caster.SendMessage("Chance to cure is {0:0.##}%", chance);

                    if (!m.Incurable && Utility.RandomDouble() < (chance / 100.0))
                    {
                        if (m.CurePoison(Caster))
                        {
                            if (Caster != m)
                                Caster.SendLocalizedMessage(1010058); // You have cured the target of all poisons!

                            m.SendLocalizedMessage(1010059); // You have been cured of all poisons.
                        }
                        else
                        {
                            // defensive: if any shard rules override CheckCure later
                            Caster.SendLocalizedMessage(1010060); // You have failed to cure your target!
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