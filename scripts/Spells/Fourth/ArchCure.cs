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

/* Spells/Fourth/ArchCure.cs
 * CHANGELOG:
 *  5/15/23, Adam
 *      I think ArchCure was just broken in our Pun13.6 era as you would heal enemies and monsters
 *      whereby flagging yourself gray. We will adopt the AOS style of ArchCure under the 95% rule
 *	3/22/10, Adam
 *		Rename mantra to "An Vas Nox" and add the tag "[Greater Cure]"
 *		It all shows up now in game correctly
 *  11/07/05, Kit
 *		Restored former cure rates.
 *	10/16/05, Pix
 *		Change to chance to cure.
 *  6/4/04, Pixie
 *		Changed to Greater Cure type spell (no more area effect)
 *		with greater chance to cure than Cure.
 *		Added debugging for cure chance for people > playerlevel
 *	5/25/04, Pixie
 *		Changed formula for success curing poison
 *	5/22/04, Pixie
 *		Made it so chance to cure poison was based on the caster's magery vs the level of poison
 */


using Server.Mobiles;
using Server.Targeting;
using System.Collections.Generic;

namespace Server.Spells.Fourth
{
    public class ArchCureSpell : Spell
    {
        // Siege: traditional Arch Cure (area effect).
        private static readonly SpellInfo m_InfoSiege = new SpellInfo(
            "Arch Cure", "Vas An Nox",
            SpellCircle.Fourth,
            215,
            9061,
            Reagent.Garlic,
            Reagent.Ginseng,
            Reagent.MandrakeRoot
        );

        // Angel Island + Mortalis: this spell is treated as "Greater Cure" behavior (single target),
        // but uses OSI Arch Cure chance math for historical accuracy.
        private static readonly SpellInfo m_InfoNonSiege = new SpellInfo(
            "Greater Cure", "An Vas Nox [Greater Cure]",
            SpellCircle.Fourth,
            215,
            9061,
            Reagent.Garlic,
            Reagent.Ginseng,
            Reagent.MandrakeRoot
        );

        public ArchCureSpell(Mobile caster, Item? scroll)
            : base(caster, scroll, Core.RuleSets.SiegeRules() ? m_InfoSiege : m_InfoNonSiege)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        // --------------------------------------------------------------------
        // OSI-style Arch Cure chance:
        // chanceToCure = 10000 + (Magery * 75) - ((PoisonLevel) * 1750)
        // chanceToCure /= 100
        // chanceToCure -= 1
        //
        // PoisonLevel here is 1..5, so we map Poison.Level (0..4) -> (Level + 1).
        // This matches the commonly emulated OSI-era behavior and is used for both
        // Siege (area) and Non-Siege (single target) variants.
        // --------------------------------------------------------------------
        private static double GetArchCureChancePercent(Mobile caster, Poison poison)
        {
            if (caster == null || poison == null)
                return 0.0;

            double magery = caster.Skills[SkillName.Magery].Value;
            int poisonLevel = poison.Level + 1;

            double chance = (10000.0 + (magery * 75.0) - (poisonLevel * 1750.0)) / 100.0;
            chance -= 1.0;

            if (chance < 0.0) chance = 0.0;
            else if (chance > 100.0) chance = 100.0;

            return chance;
        }

        private static bool RollPercent(double chancePercent)
        {
            if (chancePercent <= 0.0) return false;
            if (chancePercent >= 100.0) return true;
            return Utility.RandomDouble() < (chancePercent / 100.0);
        }

        // --------------------------------------------------------------------
        // Angel Island + Mortalis: single-target behavior (Greater Cure style).
        // --------------------------------------------------------------------
        public void Target(Mobile m)
        {
            if (!Caster.CanSee(m))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
                FinishSequence();
                return;
            }

            if (!CheckBSequence(m))
            {
                FinishSequence();
                return;
            }

            SpellHelper.Turn(Caster, m);

            bool success = false;
            Poison p = m.Poison;

            if (p != null && !m.Incurable)
            {
                double chance = GetArchCureChancePercent(Caster, p);

                if (Caster.AccessLevel > AccessLevel.Player)
                    Caster.SendMessage("Chance to cure is {0:0.##}%", chance);

                if (RollPercent(chance) && m.CurePoison(Caster))
                {
                    success = true;

                    if (Caster != m)
                        Caster.SendLocalizedMessage(1010058); // You have cured the target of all poisons!

                    m.SendLocalizedMessage(1010059); // You have been cured of all poisons.
                }
            }

            if (!success)
                Caster.SendLocalizedMessage(1010060); // You have failed to cure your target!

            m.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
            m.PlaySound(0x1E0);

            FinishSequence();
        }

        // --------------------------------------------------------------------
        // Siege: traditional area Arch Cure (target point/mobile, cures in radius).
        // --------------------------------------------------------------------
        public void TargetOnIslandSiege(IPoint3D p)
        {
            if (!Caster.CanSee(p))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
                FinishSequence();
                return;
            }

            if (!CheckSequence())
            {
                FinishSequence();
                return;
            }

            SpellHelper.Turn(Caster, p);
            SpellHelper.GetSurfaceTop(ref p);

            List<Mobile> targets = new List<Mobile>();

            Map map = Caster.Map;
            Mobile? directTarget = p as Mobile;

            if (map != null)
            {
                bool feluccaRules = (map.Rules == MapRules.FeluccaRules);

                // Direct target is allowed if beneficial checks pass.
                if (directTarget != null && Caster.CanBeBeneficial(directTarget, false))
                    targets.Add(directTarget);

                IPooledEnumerable eable = map.GetMobilesInRange(new Point3D(p), 2);

                foreach (Mobile m in eable)
                {
                    if (m == directTarget)
                        continue;

                    if (AreaCanTarget(m, feluccaRules))
                        targets.Add(m);
                }

                eable.Free();
            }

            Effects.PlaySound(p, Caster.Map, 0x299);

            if (targets.Count > 0)
            {
                int cured = 0;

                for (int i = 0; i < targets.Count; ++i)
                {
                    Mobile m = targets[i];

                    if (m.Incurable)
                        continue;

                    Caster.DoBeneficial(m);

                    Poison poison = m.Poison;

                    if (poison != null)
                    {
                        double chance = GetArchCureChancePercent(Caster, poison);

                        if (Caster.AccessLevel > AccessLevel.Player)
                            Caster.SendMessage("Chance to cure ({0}) is {1:0.##}%", m.Name, chance);

                        if (RollPercent(chance) && m.CurePoison(Caster))
                            ++cured;
                    }

                    m.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
                    m.PlaySound(0x1E0);
                }

                if (cured > 0)
                    Caster.SendLocalizedMessage(1010058); // You have cured the target of all poisons!
            }

            FinishSequence();
        }

        private bool AreaCanTarget(Mobile target, bool feluccaRules)
        {
            /* Siege Arch Cure (area) restrictions:
             * - Will not cure aggressors or aggressed targets.
             * - Requires mutual innocence OR ally status.
             * - In Felucca rulesets, only cures players (no monsters/pets/summons).
             *
             * Note: Our historical note is that older implementations could allow curing
             * enemies/monsters and flagging gray; we keep these restrictions for safety.
             */

            if (!Caster.CanBeBeneficial(target, false))
                return false;

            if (target != Caster)
            {
                if (IsAggressor(target) || IsAggressed(target))
                    return false;

                if ((!IsInnocentTo(Caster, target) || !IsInnocentTo(target, Caster)) && !IsAllyTo(Caster, target))
                    return false;

                if (feluccaRules && !(target is PlayerMobile))
                    return false;
            }

            return true;
        }

        private bool IsAggressor(Mobile m)
        {
            foreach (AggressorInfo info in Caster.Aggressors)
            {
                if (m == info.Attacker && !info.Expired)
                    return true;
            }

            return false;
        }

        private bool IsAggressed(Mobile m)
        {
            foreach (AggressorInfo info in Caster.Aggressed)
            {
                if (m == info.Defender && !info.Expired)
                    return true;
            }

            return false;
        }

        private static bool IsInnocentTo(Mobile from, Mobile to)
        {
            return (Notoriety.Compute(from, to) == Notoriety.Innocent);
        }

        private static bool IsAllyTo(Mobile from, Mobile to)
        {
            return (Notoriety.Compute(from, to) == Notoriety.Ally);
        }

        public class InternalTarget : Target
        {
            private readonly ArchCureSpell m_Owner;

            public InternalTarget(ArchCureSpell owner)
                : base(12, false, TargetFlags.Beneficial)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (Core.RuleSets.SiegeRules())
                {
                    IPoint3D? p = o as IPoint3D;
                    if (p != null)
                        m_Owner.TargetOnIslandSiege(p);
                }
                else
                {
                    if (o is Mobile)
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