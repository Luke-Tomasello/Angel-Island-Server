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

/* ChangeLog
 * 5/30/05, Kit
 *		Changed check that only allowed spells effect to work if casted from a player
 *		Mobs can now cast weaken that works.
 *	4/27/05 smerX
 *		Low level debuffs always interrupt targets' spells, circa OSI15
 */

using Server.Network;
using Server.Targeting;

namespace Server.Spells.First
{
    public class WeakenSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Weaken", "Des Mani",
                SpellCircle.First,
                212,
                9031,
                Reagent.Garlic,
                Reagent.Nightshade
            );

        public WeakenSpell(Mobile caster, Item scroll)
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

                if (m.Spell != null)
                    m.Spell.OnCasterHurt();

                SpellHelper.CheckReflect((int)this.Circle, Caster, ref m);

                if (SpellHelper.AddStatCurse(Caster, m, StatType.Str))
                {
                    m.FixedParticles(0x3779, 10, 15, 5009, EffectLayer.Waist);
                    m.PlaySound(0x1E6);
                }
                else
                {
                    Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502632); // The spell fizzles.
                    Caster.FixedEffect(0x3735, 6, 30);
                    Caster.PlaySound(0x5C);
                }

                m.Paralyzed = false;

            }

            FinishSequence();
        }

        public class InternalTarget : Target
        {
            private WeakenSpell m_Owner;

            public InternalTarget(WeakenSpell owner)
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