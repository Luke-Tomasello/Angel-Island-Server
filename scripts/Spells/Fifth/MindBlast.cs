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

/*
 * Mind blast checks the targets stats and determines which stat is the highest and lowest then determines the difference between the two. 
 *  This number is halved and checked to make sure it is not more than 40 damage. Should the target player pass a resist check the damage would be halved.
 * 
 * Example 1: Str: 100: Dex:25 Int:100, Highest Stat 100, Lowest Stat 25, Difference:75
 * Result 1: Damage:37, Resisted Damage:18
 * 
 * Example 2: Str: 100: Dex:25 Int:110, Highest Stat 110, Lowest Stat 25, Difference:85
 * Result 2: Damage:40, Resisted Damage:20
 * 
 * Example 3: Str: 90: Dex:45 Int:90, Highest Stat 90, Lowest Stat 45, Difference:45
 * Result3: Damage:22, Resisted Damage:11
 * 
 * As you can see balanced stats are critical when mitigating the damage from the mind blast spell.
 * 
 * Formula: (Highest Stat - Lowest Stat) / 2
 * http://www.uorenaissance.com/info/Mind_Blast
 */
/* Scripts/Spells/Base/Spell.cs
 *	ChangeLog:
 *  7/23/2023, Adam: (Siege)
 *      I had a long discussion with a seasoned player, and while they admit damage is 'era accurate', they also say
 *      Insta-hit and MB destroyed PvP in that era. He suggested a cap of 25.
 *      We're reverting to modified Angel Island style MB with the damageMod. This tamps down damage to reasonable levels.
 *      For now, this is only on TestCenter for review.
 *	4/30/23, Yoar
 *	    Now conditioning for AI/MO: Tuning consoles, smerX damageMod, damage caps
 *	3/19/10, Adam
 *		scale damage between lightning and ebolt - fine tuning with MindBlastMCi
 * 	7/4/04, Pix
 * 		Added caster to SpellHelper.Damage call so corpse wouldn't stay blue to this caster
 * 	5/5/04 changes by smerX:
 * 		scaled the lower end of damages
 * 	3/25/04 changes by smerX:
 * 		Added DamageDelay of 0.65 seconds
 * 	3/18/04 code changes by smerX:
 * 		Removed DamageDelay
 * 		Amended damage calc to ("int damage = (highestStat - lowestStat) / 2")
 * 			This produces normal p15 damage 
 */

using Server.Targeting;
using System;
using System.Linq;

namespace Server.Spells.Fifth
{
    public class MindBlastSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Mind Blast", "Por Corp Wis",
                SpellCircle.Fifth,
                218,
                Core.RuleSets.AOSRules() ? 9002 : 9032,
                Reagent.BlackPearl,
                Reagent.MandrakeRoot,
                Reagent.Nightshade,
                Reagent.SulfurousAsh
            );

        public MindBlastSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
            if (Core.RuleSets.AOSRules())
                m_Info.LeftHandEffect = m_Info.RightHandEffect = 9002;
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        private void AosDelay_Callback(object state)
        {
            object[] states = (object[])state;
            Mobile caster = (Mobile)states[0];
            Mobile target = (Mobile)states[1];
            Mobile defender = (Mobile)states[2];
            int damage = (int)states[3];

            if (caster.HarmfulCheck(defender))
            {
                SpellHelper.Damage(TimeSpan.FromSeconds(0.65), target, Utility.RandomMinMax(damage, damage + 4), 0, 0, 100, 0, 0);

                target.FixedParticles(0x374A, 10, 15, 5038, 1181, 2, EffectLayer.Head);
                target.PlaySound(0x213);
            }
        }

        //		public override bool DelayedDamage{ get{ return !Core.AOS; } }
        // DamageDelay was above ^ Commented out by smerX

        public void Target(Mobile m)
        {
            if (!Caster.CanSee(m))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (Core.RuleSets.AOSRules())
            {
                if (Caster.CanBeHarmful(m) && CheckSequence())
                {
                    Mobile from = Caster, target = m;

                    SpellHelper.Turn(from, target);

                    SpellHelper.CheckReflect((int)this.Circle, ref from, ref target);

                    int damage = (int)((Caster.Skills[SkillName.Magery].Value + Caster.Int) / 5);

                    if (damage > 60)
                        damage = 60;

                    Timer.DelayCall(TimeSpan.FromSeconds(1.0),
                        new TimerStateCallback(AosDelay_Callback),
                        new object[] { Caster, target, m, damage });
                }
            }
            else if (CheckHSequence(m))
            {
                Mobile from = Caster, target = m;

                SpellHelper.Turn(from, target);

                SpellHelper.CheckReflect((int)this.Circle, ref from, ref target);

                // Algorithm: (highestStat - lowestStat) / 2 [- 50% if resisted]

                double damageMod = 0;

                int[] rawStats = new[] { target.RawStr, target.RawDex, target.RawInt };
                int[] stats = new[] { target.Str, target.Dex, target.Int };

                #region smerX Damage Mod (AI/MO)
                if (Core.RuleSets.ModifiedMindBlast())
                {
                    int rawHighStat = rawStats.Max();
                    int rawLowStat = rawStats.Min();
                    rawHighStat = Math.Min(rawHighStat, 150);
                    rawLowStat = Math.Min(rawLowStat, 150);

                    // round up
                    double rawFormula = Math.Ceiling((rawHighStat - rawLowStat) / 2.0);

#if true
                    // going for a flat reduction.
                    //  The table below still delivers regular 37 damage hits.
                    // This damageMod: ToMage 11/21, ToWarrior 13/24
                    damageMod = -(rawFormula * .34);
#else
                    if (rawFormula == 0)
                        damageMod = 1;
                    else if (rawFormula >= 38)
                        damageMod = 0;
                    else if (rawFormula >= 36)
                        damageMod = -5;
                    else if (rawFormula >= 33)
                        damageMod = -10;
                    else if (rawFormula >= 30)
                        damageMod = -15;
                    else if (rawFormula >= 28)
                        damageMod = -18;
                    else
                        damageMod = -20;
#endif

                }
                #endregion

                int highestStat = stats.Max();
                int lowestStat = stats.Min();
                highestStat = Math.Min(highestStat, 150);
                lowestStat = Math.Min(lowestStat, 150);

                double damage = 0;
                if (Core.RuleSets.ModifiedMindBlast())
                    // round up
                    damage = Math.Ceiling(((highestStat - lowestStat) / 2.0) + damageMod);
                else
                    damage = ((highestStat - lowestStat) / 2.0) + damageMod;

                #region Damage Caps (AI/MO/SP)
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules() || Core.RuleSets.SiegeStyleRules())
                {
                    if (damage > 45)
                        damage = 45;
                    else if (damage <= 0)
                        damage = 1;
                }
                #endregion

                #region Tuning Console
#if false
                if (Server.Items.Consoles.TuningConsole.Enabled && Server.Items.Consoles.MindBlastMCi.UseNewScale == true)
                    damage = (int)(((double)damage / ((45.0 - 1.0) / (Server.Items.Consoles.MindBlastMCi.DamageRangeHigh - Server.Items.Consoles.MindBlastMCi.DamageRangeLow))) + Server.Items.Consoles.MindBlastMCi.DamageRangeLow);
#endif
                #endregion

                if (CheckResisted(target))
                {
                    if (Core.RuleSets.ModifiedMindBlast())
                        // round up
                        damage = Math.Ceiling(damage * (100.0 * GetResistScaler(0.5)) / 100.0);
                    else
                        damage = damage * (100.0 * GetResistScaler(0.5)) / 100.0;
                    target.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                }

                from.FixedParticles(0x374A, 10, 15, 2038, EffectLayer.Head);

                target.FixedParticles(0x374A, 10, 15, 5038, EffectLayer.Head);
                target.PlaySound(0x213);

                TimeSpan delay = TimeSpan.Zero;

                #region Tuning Console
#if false
                if (Server.Items.Consoles.TuningConsole.Enabled)
                    delay = TimeSpan.FromSeconds(Server.Items.Consoles.MindBlastMCi.DamageDelay);
#endif
                #endregion

                //Pixie: 7/4/04: added caster to this so corpse wouldn't stay blue to this caster
                SpellHelper.Damage(delay, target, Caster, damage, 0, 0, 100, 0, 0);
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private MindBlastSpell m_Owner;

            public InternalTarget(MindBlastSpell owner)
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