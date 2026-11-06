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

/* Scripts\Spells\Seventh\MeteorSwarm.cs
 * ChangeLog:
 *  8/25/21, Adam (TargetExploitCheck)
 *      We apply late binding LOS checks to thwart exploitative targeting techniques
 *      See full explanation in BaseHouse.cs
 *	7/2/10, Adam
 *		add target 'under house' exploit check
 *		if ((target is Server.Targeting.LandTarget && Server.Multis.BaseHouse.FindHouseAt(((Server.Targeting.LandTarget)(target)).Location, Caster.Map, 16) != null))
 *			target cannot be seen
 * 	6/5/04, Pix
 * 		Merged in 1.0RC0 code.
 */

using Server.Multis;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Spells.Seventh
{
    public class MeteorSwarmSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Meteor Swarm", "Flam Kal Des Ylem",
                SpellCircle.Seventh,
                233,
                9042,
                false,
                Reagent.Bloodmoss,
                Reagent.MandrakeRoot,
                Reagent.SulfurousAsh,
                Reagent.SpidersSilk
            );

        public MeteorSwarmSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public override bool DelayedDamage { get { return true; } }

        public void Target(IPoint3D p)
        {

            // Meteor Swarm is always location-centered;
            // unlike Chain Lightning, it never centers on a mobile and always uses the targeted location,
            // requiring normal LOS checks before any audio or visual effects are played.

            // NOTE: Keep all early rejects (CanSee / LOS / town / sequence) before any audio/visuals.
            Map map = Caster.Map;

            // Disallow targeting tiles that are not visible or that are inside/under a house footprint.
            if (!Caster.CanSee(p) || (p is LandTarget && BaseHouse.FindHouseAt(((LandTarget)p).Location, Caster.Map, 16) != null))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
                FinishSequence();
                return;
            }

            // Normalize to world location before any LOS / AOE center calculations.
            if (p is Item)
                p = ((Item)p).GetWorldLocation();

            Point3D origin = map != null ? new Point3D(p) : Point3D.Zero;

            // Disallow targeting tiles around corners or otherwise cannot be seen.
            // If this fails, treat it like a normal targeting failure (no sound/FX).
            if (map == null || !Utility.LineOfSight(map, Caster, origin, true))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
                FinishSequence();
                return;
            }

            // No casting in town
            if (!SpellHelper.CheckTown(p, Caster))
            {   // actually handled by region
                Caster.SendLocalizedMessage(500946); // You cannot cast this in town!
                FinishSequence();
                return;
            }

            // ensure we have enough mana/reagents/etc.
            if (!CheckSequence())
            {
                FinishSequence();
                return;
            }

            SpellHelper.Turn(Caster, p);

            double damage = Core.RuleSets.AOSRules()
                ? GetNewAosDamage(48, 1, 5)
                : Utility.Random(27, 22);

            ArrayList targets = new ArrayList();

            if (map != null)
            {
                int radius = 2;                   // Meteor Swarm radius around the impact point
                int penetrationDepth = 2;         // Allowed bleed depth through walls/house boundaries
                bool requirePlayerLOS = Core.RuleSets.AOSRules();

                // Always play the impact sound for a valid cast, even if no mobiles are affected.
                Effects.PlaySound(origin, map, 0x160);

                // 1) Gather candidate mobiles near the impact point.
                List<Mobile> candidates = new List<Mobile>();

                IPooledEnumerable eable = map.GetMobilesInRange(origin, radius);
                try
                {
                    foreach (Mobile m in eable)
                    {
                        if (m == null || m.Deleted)
                            continue;

                        if (m == Caster)
                            continue;

                        if (!SpellHelper.ValidIndirectTarget(Caster, m))
                            continue;

                        if (!Caster.CanBeHarmful(m, false))
                            continue;

                        // Per-target exploit guard: validates THIS (caster, target, origin) combination.
                        // If tripped, the caster is messaged; we skip this target and continue evaluating others.
                        if (Utility.TargetExploitCheck(this.Name, Caster, m, origin) != Utility.ExploitType.None)
                        {
#if DEBUG
                            // Debug-only: audible indicator that a filter fired (avoid breakpoints).
                            Caster.PlaySound(0xC4);
#endif
                            continue;
                        }

                        candidates.Add(m);
                    }
                }
                finally
                {
                    eable.Free();
                }

                // 2) Apply LOS + house/wall penetration filtering.
                List<Mobile> filtered = SpellHelper.AOETargets(Caster, origin, candidates, penetrationDepth, requirePlayerLOS);

                for (int i = 0; i < filtered.Count; i++)
                    targets.Add(filtered[i]);
            }

            if (targets.Count > 0)
            {
                if (Core.RuleSets.AOSRules() && targets.Count > 1)
                    damage = (damage * 2) / targets.Count;
                else if (!Core.RuleSets.AOSRules())
                    damage /= targets.Count;

                for (int i = 0; i < targets.Count; ++i)
                {
                    Mobile? m = (Mobile?)targets[i];

                    double toDeal = damage;

                    if (!Core.RuleSets.AOSRules() && CheckResisted(m))
                    {
                        toDeal *= GetResistScaler(0.5);
                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }

                    if (toDeal < 7)
                        toDeal = 7;

                    Caster.DoHarmful(m);

                    SpellHelper.Damage(
                        this,
                        m,
                        toDeal,
                        new DamageContext { Source = this, ImpactLocation = origin, TimestampUtc = DateTime.UtcNow }
                    );

                    // Visuals are per-target (projectiles from caster to affected mobiles).
                    Caster.MovingParticles(m, 0x36D4, 7, 0, false, true, 9501, 1, 0, 0x100);
                }
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private MeteorSwarmSpell m_Owner;

            public InternalTarget(MeteorSwarmSpell owner)
                : base(12, true, TargetFlags.None)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                IPoint3D? p = o as IPoint3D;

                if (p != null)
                    m_Owner.Target(p);
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}