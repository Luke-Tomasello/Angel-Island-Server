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

/* Scripts/Spells/First/NightSight.cs
 * CHANGELOG:
 *	8/18/2004 - Pixie
 *		Changed lightlevel to be higher (so it's useful at low levels of magery)
 */

using Server.Targeting;
using System;

namespace Server.Spells.First
{
    public class NightSightSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Night Sight", "In Lor",
                SpellCircle.First,
                236,
                9031,
                Reagent.SulfurousAsh,
                Reagent.SpidersSilk
            );

        public NightSightSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new NightSightTarget(this);
        }

        private class NightSightTarget : Target
        {
            private Spell m_Spell;

            public NightSightTarget(Spell spell)
                : base(10, false, TargetFlags.None)
            {
                m_Spell = spell;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile && m_Spell.CheckSequence())
                {
                    Mobile targ = (Mobile)targeted;

                    SpellHelper.Turn(m_Spell.Caster, targ);

                    if (targ.BeginAction(typeof(LightCycle)))
                    {
                        new LightCycle.NightSightTimer(targ).Start();
                        int level = (int)Math.Abs(LightCycle.DungeonLevel * (.5 + m_Spell.Caster.Skills[SkillName.Magery].Base / 200));

                        if (level > 25 || level < 0)
                            level = 25;

                        targ.LightLevel = level;

                        targ.FixedParticles(0x376A, 9, 32, 5007, EffectLayer.Waist);
                        targ.PlaySound(0x1E3);
                    }
                    else
                    {
                        from.SendMessage("{0} already have nightsight.", from == targ ? "You" : "They");
                    }
                }

                m_Spell.FinishSequence();
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Spell.FinishSequence();
            }
        }
    }
}