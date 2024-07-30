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
    8/29/21, Lib
        moved damage delay to CoreAI
	6/03/06, Kit
		Added damage type define
	5/15/06, Kit
		Added Min/Max damage defines.
	7/4/04, Pix
		Added caster to SpellHelper.Damage call so corpse wouldn't stay blue to this caster
	6/5/04, Pix
		Merged in 1.0RC0 code.
	3/25/04 changes by smerX:
		Changed DamageDelay to 0.45
	3/18/04 code changes by smerX:
		Added 0.3 second DamageDelay
*/
using Server.Targeting;
using System;

namespace Server.Spells.Seventh
{
    public class FlameStrikeSpell : Spell
    {
        public override int MinDamage { get { return 22; } }
        public override int MaxDamage { get { return 27; } }
        public override SpellDamageType DamageType
        {
            get
            {
                return SpellDamageType.Fire;
            }
        }

        private static SpellInfo m_Info = new SpellInfo(
                "Flame Strike", "Kal Vas Flam",
                SpellCircle.Seventh,
                245,
                9042,
                Reagent.SpidersSilk,
                Reagent.SulfurousAsh
            );

        public FlameStrikeSpell(Mobile caster, Item scroll)
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
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)this.Circle, Caster, ref m);

                double damage;

                if (Core.RuleSets.AOSRules())
                {
                    damage = GetNewAosDamage(48, 1, 5);
                }
                else
                {
                    damage = Utility.Random(MinDamage, MaxDamage);

                    if (CheckResisted(m))
                    {
                        damage *= GetResistScaler(0.6);

                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }

                    damage *= GetDamageScalar(m);
                }

                m.FixedParticles(0x3709, 10, 30, 5052, EffectLayer.LeftFoot);
                m.PlaySound(0x208);

                //Pixie: 7/4/04: added caster to this so corpse wouldn't stay blue to this caster
                //Lib: 8/29/21: moved damage delay to console
                SpellHelper.Damage(TimeSpan.FromSeconds(CoreAI.SpellFsDmgDelay), m, Caster, damage, 0, 100, 0, 0, 0);
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private FlameStrikeSpell m_Owner;

            public InternalTarget(FlameStrikeSpell owner)
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