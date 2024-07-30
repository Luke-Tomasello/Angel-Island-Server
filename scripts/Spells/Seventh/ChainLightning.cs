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

using Server.Multis;
using Server.Targeting;
using System.Collections;

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
            // adam: add target 'under house' exploit check
            if (!Caster.CanSee(p) || (p is Server.Targeting.LandTarget && Server.Multis.BaseHouse.FindHouseAt(((Server.Targeting.LandTarget)(p)).Location, Caster.Map, 16) != null))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                if (p is Item)
                    p = ((Item)p).GetWorldLocation();

                double damage;

                if (Core.RuleSets.AOSRules())
                    damage = GetNewAosDamage(48, 1, 5);
                else
                    damage = Utility.Random(27, 22);

                ArrayList targets = new ArrayList();

                Map map = Caster.Map;

                if (map != null)
                {
                    IPooledEnumerable eable = map.GetMobilesInRange(new Point3D(p), 2);

                    foreach (Mobile m in eable)
                    {
                        if (Core.RuleSets.AOSRules() && m == Caster)
                            continue;

                        // See full explanation in BaseHouse.cs
                        if (BaseHouse.TargetExploitCheck(this.Name, Caster, m, new Point3D(p)) != BaseHouse.ExploitType.None)
                        {   // we've already sent the caster a message
                            FinishSequence();
                            return;
                        }
                        else if (BaseHouse.ProximityToDamage(Caster, m) <= 2)
                            if (SpellHelper.ValidIndirectTarget(Caster, m) && Caster.CanBeHarmful(m, false))
                                targets.Add(m);
                    }
                    eable.Free();
                }

                if (targets.Count > 0)
                {
                    if (Core.RuleSets.AOSRules() && targets.Count > 1)
                        damage = (damage * 2) / targets.Count;
                    else if (!Core.RuleSets.AOSRules())
                        damage /= targets.Count;

                    for (int i = 0; i < targets.Count; ++i)
                    {
                        Mobile m = (Mobile)targets[i];

                        double toDeal = damage;

                        if (!Core.RuleSets.AOSRules() && CheckResisted(m))
                        {
                            toDeal *= GetResistScaler(0.5);

                            m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                        }

                        if (toDeal < 7)
                            toDeal = 7;

                        Caster.DoHarmful(m);
                        SpellHelper.Damage(this, m, toDeal, 0, 0, 0, 0, 100);

                        m.BoltEffect(0);
                    }
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
                IPoint3D p = o as IPoint3D;

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