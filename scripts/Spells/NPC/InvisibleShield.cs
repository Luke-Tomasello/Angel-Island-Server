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

/* Scripts\Spells\NPC\InvisibleShield.cs
 * ChangeLog:
 * 	6/29/10, adam
 * 		Initial creation
 *		This spell is used by guards on Suicide Bombers to contain the blast
 */

using Server.Targeting;
using System;

namespace Server.Spells.Fifth
{
    public class InvisibleShieldSpell : Spell
    {
        // Kal (summon)
        // Sanct (Protection)
        // Grav (field)
        private static SpellInfo m_Info = new SpellInfo(
                "InvisibleShield", "Kal Sanct Grav",
                SpellCircle.Fifth,
                221,
                9022,
                true,
                Reagent.BlackPearl,
                Reagent.MandrakeRoot,
                Reagent.SpidersSilk,
                Reagent.SulfurousAsh
            );

        public InvisibleShieldSpell(Mobile caster, Item scroll)
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
            else if (m.Spell != null && m.Spell.IsCasting)
            {
                // too busy casting
            }
            else if (m.InvisibleShield)
            {
                Caster.SendMessage("They are already covered with an invisible shield.");
            }
            else if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);
                m.Shield(TimeSpan.FromMinutes(2.0));

                InvisibleShieldInfo info = new InvisibleShieldInfo(Caster, m, TimeSpan.FromMinutes(2.0));
                info.m_Timer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(1.25), new TimerStateCallback(ProcessInvisibleShieldInfo), info);

                // wall of stone sound: point, map, sound
                Effects.PlaySound(m, Caster.Map, 0x1F6);
            }

            FinishSequence();
        }

        public class InternalTarget : Target
        {
            private InvisibleShieldSpell m_Owner;

            public InternalTarget(InvisibleShieldSpell owner)
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

        private class InvisibleShieldInfo
        {
            public Mobile m_From;
            public Mobile m_Target;
            public DateTime m_EndTime;
            public Timer m_Timer;

            public InvisibleShieldInfo(Mobile from, Mobile target, TimeSpan duration)
            {
                m_From = from;
                m_Target = target;
                m_EndTime = DateTime.UtcNow + duration;
            }
        }

        private static void ProcessInvisibleShieldInfo(object state)
        {
            InvisibleShieldInfo info = (InvisibleShieldInfo)state;
            Mobile from = info.m_From;
            Mobile targ = info.m_Target;

            if (DateTime.UtcNow >= info.m_EndTime || targ.Deleted || from.Map != targ.Map ||
                targ.GetDistanceToSqrt(from) > 16 || targ.InvisibleShield == false)
            {
                if (info.m_Timer != null)
                    info.m_Timer.Stop();

                targ.InvisibleShield = false;
            }
            else
            {
                targ.FixedEffect(0x376A, 1, 32, 51, 0);
            }
        }
    }
}