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
	6/5/04, Pix
		Merged in 1.0RC0 code.
*/

using Server.Mobiles;
using Server.Targeting;
using System;

namespace Server.Spells.Fifth
{
    public class ParalyzeSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Paralyze", "An Ex Por",
                SpellCircle.Fifth,
                218,
                9012,
                Reagent.Garlic,
                Reagent.MandrakeRoot,
                Reagent.SpidersSilk
            );

        public ParalyzeSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public void Target(Mobile m)
        {
            if (m is BaseCreature bc && !bc.Deleted && bc.BlockDamage == true)
            {
                string text = $"* {bc.SafeName} shrugs off your feeble attempt *";
                if (bc.CheckString(text))
                    bc.Emote(text);
            }
            else if (!Caster.CanSee(m))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (Core.RuleSets.AOSRules() && (m.Frozen || m.Paralyzed || (m.Spell != null && m.Spell.IsCasting)))
            {
                Caster.SendLocalizedMessage(1061923); // The target is already frozen.
            }
            else if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)this.Circle, Caster, ref m);

                double duration;

                if (Core.RuleSets.AOSRules())
                {
                    int secs = 2 + (GetDamageFixed(Caster) / 100) - (GetResistFixed(m) / 100);

                    if (!m.Player)
                        secs *= 3;

                    if (secs < 0)
                        secs = 0;

                    duration = secs;
                }
                else
                {
                    // Algorithm: ((20% of magery) + 7) seconds [- 50% if resisted]

                    duration = 7.0 + (Caster.Skills[SkillName.Magery].Value * 0.2);

                    if (CheckResisted(m))
                        duration *= GetResistScaler(0.75);
                }

                m.Paralyze(TimeSpan.FromSeconds(duration));

                m.PlaySound(0x204);
                m.FixedEffect(0x376A, 6, 1);
            }

            FinishSequence();
        }

        public class InternalTarget : Target
        {
            private ParalyzeSpell m_Owner;

            public InternalTarget(ParalyzeSpell owner)
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