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

/* Spells\Seventh\ChainLightning.cs
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

using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Spells.Seventh
{
    public class ChainLightningSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Chain Lightning", "Vas Ort Grav",
                SpellCircle.Seventh,
                209,
                9022,
                false,
                Reagent.BlackPearl,
                Reagent.Bloodmoss,
                Reagent.MandrakeRoot,
                Reagent.SulfurousAsh
            );

        public ChainLightningSpell(Mobile caster, Item scroll)
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
            // Chain Lightning uses mobile-centered AOE when a valid target is selected;
            // otherwise it falls back to location-centered behavior with normal LOS rules.
            // Meteor Swarm, by contrast, is always location-centered.

            // NOTE: Keep all early rejects (CanSee / LOS / town / sequence) before any audio/visuals.
            Map map = Caster.Map;
            if (map == null)
            {
                FinishSequence();
                return;
            }

            // Early visibility / "under house" reject (MS model).
            if (!Caster.CanSee(p) || (p is Server.Targeting.LandTarget && Server.Multis.BaseHouse.FindHouseAt(((Server.Targeting.LandTarget)p).Location, Caster.Map, 16) != null))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
                FinishSequence();
                return;
            }

            // Resolve p to world location if it is an item.
            if (p is Item)
                p = ((Item)p).GetWorldLocation();

            // Determine the effective origin:
            // Prefer mobile-centered if the target is a legal mobile with LOS; otherwise use the targeted location.
            Point3D origin;
            Mobile? originMobile = null;

            Mobile? pMobile = p as Mobile;
            if (pMobile != null && !pMobile.Deleted && pMobile.Map == map)
            {
                // Preserve CL historical behavior: in AOS, skip self as a candidate; but still allow targeting self
                // only if your core permits it. We do not special-case targeting self here beyond the existing rules.
                if (Caster.CanSee(pMobile) &&
                    SpellHelper.ValidIndirectTarget(Caster, pMobile) &&
                    Caster.CanBeHarmful(pMobile, false) &&
                    Utility.LineOfSight(map, Caster, pMobile.Location, true))
                {
                    originMobile = pMobile;
                }
            }

            origin = (originMobile != null) ? originMobile.Location : new Point3D(p);

            // MS model: disallow targeting around corners / otherwise cannot be seen.
            // If this fails, treat it like a normal targeting failure (no sound/FX).
            if (!Utility.LineOfSight(map, Caster, origin, true))
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

            int radius = 2;           // Chain Lightning radius around the impact point
            int penetrationDepth = 2; // Allowed bleed depth through walls/house boundaries
            bool requireLOS = Core.RuleSets.AOSRules();

            // Always play the lightning sound for a valid cast.
            // - Caster.PlaySound ensures the caster hears it even if the impact point is far away.
            // - Effects.PlaySound at origin lets nearby bystanders hear it at the impact location.
            Caster.PlaySound(0x29);
            Effects.PlaySound(origin, map, 0x29);

            // 1) Gather candidate mobiles near the impact point.
            List<Mobile> candidates = new List<Mobile>();

            IPooledEnumerable eable = map.GetMobilesInRange(origin, radius);
            try
            {
                foreach (Mobile m in eable)
                {
                    if (m == null || m.Deleted)
                        continue;

                    // Preserve CL behavior: in AOS we skip the caster; pre-AOS can include caster (existing behavior).
                    if (Core.RuleSets.AOSRules() && m == Caster)
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
                        Caster.PlaySound(0xC4); // pig oink: exploit filter fired
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
            List<Mobile> filtered = SpellHelper.AOETargets(Caster, origin, candidates, penetrationDepth, requireLOS);

            for (int i = 0; i < filtered.Count; i++)
                targets.Add(filtered[i]);

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

                    // Visuals are per-target (bolts on affected mobiles).
                    m.BoltEffect(0);
                }
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private ChainLightningSpell m_Owner;

            public InternalTarget(ChainLightningSpell owner)
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