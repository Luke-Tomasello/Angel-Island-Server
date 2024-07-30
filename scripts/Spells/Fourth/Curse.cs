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
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 2 lines removed.
	6/5/04, Pix
		Merged in 1.0RC0 code.
*/

using Server.Targeting;
using System;
using System.Collections;

namespace Server.Spells.Fourth
{
    public class CurseSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Curse", "Des Sanct",
                SpellCircle.Fourth,
                227,
                9031,
                Reagent.Nightshade,
                Reagent.Garlic,
                Reagent.SulfurousAsh
            );

        public CurseSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        private static Hashtable m_UnderEffect = new Hashtable();

        private static void RemoveEffect(object state)
        {
            Mobile m = (Mobile)state;

            m_UnderEffect.Remove(m);

        }

        public static bool UnderEffect(Mobile m)
        {
            return m_UnderEffect.Contains(m);
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

                SpellHelper.AddStatCurse(Caster, m, StatType.Str); SpellHelper.DisableSkillCheck = true;
                SpellHelper.AddStatCurse(Caster, m, StatType.Dex);
                SpellHelper.AddStatCurse(Caster, m, StatType.Int); SpellHelper.DisableSkillCheck = false;

                Timer t = (Timer)m_UnderEffect[m];

                if (Caster.Player && m.Player && Caster != m && t == null)
                {
                    TimeSpan duration = SpellHelper.GetDuration(Caster, m, true);
                    m_UnderEffect[m] = t = Timer.DelayCall(duration, new TimerStateCallback(RemoveEffect), m);
                }

                if (m.Spell != null)
                    m.Spell.OnCasterHurt();

                m.Paralyzed = false;

                m.FixedParticles(0x374A, 10, 15, 5028, EffectLayer.Waist);
                m.PlaySound(0x1EA);
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private CurseSpell m_Owner;

            public InternalTarget(CurseSpell owner)
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