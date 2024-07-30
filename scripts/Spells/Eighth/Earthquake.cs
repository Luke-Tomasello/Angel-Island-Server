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

/* 	ChangeLog:
 * 	8/27/21, Adam (MyDistanceToPerimeter)
 * 	    Rollback.. appearently EQ DID deep into a house, but damage was scaled based on remaining life.
 *  8/25/21, Adam (MyDistanceToPerimeter)
 *      We apply old-school damage mitigation to limit the range EQ can do in a house
 *      Befor this fix, EQ was traveling 5 tiles into a house, so there were zero
 *      safe spots in a 7x7 small house. This fix limits the the damage to two tiles into the house
 *      which is how I remember it from OSI.
 *      See full explanation in BaseHouse.cs
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  6/5/04, Pix
 *  Merged in 1.0RC0 code.
 */

using System;
using System.Collections;

namespace Server.Spells.Eighth
{
    public class EarthquakeSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Earthquake", "In Vas Por",
                SpellCircle.Eighth,
                233,
                9012,
                false,
                Reagent.Bloodmoss,
                Reagent.Ginseng,
                Reagent.MandrakeRoot,
                Reagent.SulfurousAsh
            );

        public EarthquakeSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override bool DelayedDamage { get { return !Core.RuleSets.AOSRules(); } }

        public override void OnCast()
        {
            if (SpellHelper.CheckTown(Caster, Caster) && CheckSequence())
            {
                ArrayList targets = new ArrayList();

                Map map = Caster.Map;

                if (map != null)
                {
                    IPooledEnumerable eable = Caster.GetMobilesInRange(1 + (int)(Caster.Skills[SkillName.Magery].Value / 15.0));
                    foreach (Mobile m in eable)
                    {
                        if (Caster != m && SpellHelper.ValidIndirectTarget(Caster, m) && Caster.CanBeHarmful(m, false) && (!Core.RuleSets.AOSRules() || Caster.InLOS(m)))
                            targets.Add(m);
                    }
                    eable.Free();
                }

                Caster.PlaySound(0x2F3);

                for (int i = 0; i < targets.Count; ++i)
                {
                    Mobile m = (Mobile)targets[i];

                    int damage;

                    if (Core.RuleSets.AOSRules())
                    {
                        damage = m.Hits / 2;

                        if (m.Player)
                            damage += Utility.RandomMinMax(0, 15);

                        if (damage < 15)
                            damage = 15;
                        else if (damage > 100)
                            damage = 100;
                    }
                    else
                    {
                        damage = (m.Hits * 6) / 10;

                        if (!m.Player && damage < 10)
                            damage = 10;
                        else if (damage > 75)
                            damage = 75;
                    }

                    Caster.DoHarmful(m);
                    SpellHelper.Damage(TimeSpan.Zero, m, Caster, damage, 100, 0, 0, 0, 0);
                }
            }

            FinishSequence();
        }
    }
}