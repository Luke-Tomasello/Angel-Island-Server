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

/* Scripts/Spells/Base/Spell.cs
	ChangeLog:
	6/03/06, Kit
		Added damage type define
	5/15/06, Kit
		Added Min/Max damage defines
	7/4/04, Pix
		Added caster to SpellHelper.Damage call so corpse wouldn't stay blue to this caster
	6/5/04, Pix
		Merged in 1.0RC0 code.
	4/26/04
		Slightly lessened base damage
	3/25/04 changes by smerX:
		Changed DamageDelay to 0.65
	3/18/04 code changes by smerX:
		Added 0.6 second DamageDelay
*/
using Server.Targeting;
using System;

namespace Server.Spells.Sixth
{
    public class EnergyBoltSpell : Spell
    {
        public override int MinDamage { get { return 17; } }
        public override int MaxDamage { get { return 21; } }
        public override SpellDamageType DamageType
        {
            get
            {
                return SpellDamageType.Energy;
            }
        }

        private static SpellInfo m_Info = new SpellInfo(
                "Energy Bolt", "Corp Por",
                SpellCircle.Sixth,
                230,
                9022,
                Reagent.BlackPearl,
                Reagent.Nightshade
            );

        public EnergyBoltSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        //		public override bool DelayedDamage{ get{ return true; } }

        public void Target(Mobile m)
        {
            if (!Caster.CanSee(m))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (CheckHSequence(m))
            {
                Mobile source = Caster;

                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)this.Circle, ref source, ref m);

                double damage;

                if (Core.RuleSets.AOSRules())
                {
                    damage = GetNewAosDamage(38, 1, 5);
                }
                else
                {
                    damage = Utility.Random(MinDamage, MaxDamage);

                    if (CheckResisted(m))
                    {
                        damage *= GetResistScaler(0.75);

                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }

                    // Scale damage based on evalint and resist
                    damage *= GetDamageScalar(m);
                }

                // Do the effects
                source.MovingParticles(m, 0x379F, 7, 0, false, true, 3043, 4043, 0x211);
                source.PlaySound(0x20A);

                // Deal the damage
                //Pixie: 7/4/04: added caster to this so corpse wouldn't stay blue to this caster
                SpellHelper.Damage(TimeSpan.FromSeconds(0.65), m, Caster, damage, 0, 0, 0, 0, 100); // originally "this"
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private EnergyBoltSpell m_Owner;

            public InternalTarget(EnergyBoltSpell owner)
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