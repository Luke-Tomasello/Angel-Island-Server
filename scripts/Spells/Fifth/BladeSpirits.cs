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
 *  8/25/21, Adam (TargetExploitCheck)
 *      We apply late binding LOS checks to thwart exploitative targeting techniques
 *      See full explanation in BaseHouse.cs
 *	6/5/04, Pix
 *	    Merged in 1.0RC0 code.
 */

using Server.Mobiles;
using Server.Multis;
using Server.Targeting;
using System;

namespace Server.Spells.Fifth
{
    public class BladeSpiritsSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Blade Spirits", "In Jux Hur Ylem",
                SpellCircle.Fifth,
                266,
                9040,
                false,
                Reagent.BlackPearl,
                Reagent.MandrakeRoot,
                Reagent.Nightshade
            );

        public BladeSpiritsSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override TimeSpan GetCastDelay()
        {
            if (Core.RuleSets.AOSRules())
                return TimeSpan.FromTicks(base.GetCastDelay().Ticks * 5);

            return base.GetCastDelay() + TimeSpan.FromSeconds(6.0);
        }

        public override bool CheckCast()
        {
            if (!base.CheckCast())
                return false;

            if ((Caster.FollowerCount + 1) > Caster.FollowersMax)
            {
                Caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
                return false;
            }

            return true;
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public void Target(IPoint3D p)
        {
            Map map = Caster.Map;

            SpellHelper.GetSurfaceTop(ref p);

            if (map == null || !map.CanSpawnLandMobile(p.X, p.Y, p.Z))
            {
                Caster.SendLocalizedMessage(501942); // That location is blocked.
            }
            else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                TimeSpan duration;

                if (Core.RuleSets.AOSRules())
                    duration = TimeSpan.FromSeconds(120);
                else
                    duration = TimeSpan.FromSeconds(Utility.Random(80, 40));

                // See full explanation in BaseHouse.cs
                if (BaseHouse.TargetExploitCheck(this.Name, Caster, null, new Point3D(p)) == BaseHouse.ExploitType.None)
                    BaseCreature.Summon(new BladeSpirits(), false, Caster, new Point3D(p), 0x212, duration);
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private BladeSpiritsSpell m_Owner;

            public InternalTarget(BladeSpiritsSpell owner)
                : base(12, true, TargetFlags.None)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is IPoint3D)
                    m_Owner.Target((IPoint3D)o);
            }

            protected override void OnTargetOutOfLOS(Mobile from, object o)
            {
                from.SendLocalizedMessage(501943); // Target cannot be seen. Try again.
                from.Target = new InternalTarget(m_Owner);
                from.Target.BeginTimeout(from, TimeoutTime - DateTime.UtcNow);
                m_Owner = null;
            }

            protected override void OnTargetFinish(Mobile from)
            {
                if (m_Owner != null)
                    m_Owner.FinishSequence();
            }
        }
    }
}