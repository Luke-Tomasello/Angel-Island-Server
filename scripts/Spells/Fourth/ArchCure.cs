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
using System;
using System.Collections.Generic;

namespace Server.Spells.Fourth
{
    public class ArchCureSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Greater Cure", "An Vas Nox [Greater Cure]",
                SpellCircle.Fourth,
                215,
                9061,
                Reagent.Garlic,
                Reagent.Ginseng,
                Reagent.MandrakeRoot
            );
        private static SpellInfo m_InfoIS = new SpellInfo(
                "Arch Cure", "Vas An Nox",
                SpellCircle.Fourth,
                215,
                9061,
                Reagent.Garlic,
                Reagent.Ginseng,
                Reagent.MandrakeRoot
            );

        public ArchCureSpell(Mobile caster, Item scroll)
            : base(caster, scroll, Core.RuleSets.SiegeStyleRules() ? m_InfoIS : m_Info)
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
            else if (CheckBSequence(m))
            {
                SpellHelper.Turn(Caster, m);
                //chance to cure poison is ((caster's magery/poison level) - 20%)

                double chance = 100;
                try //I threw this try-catch block in here because Poison is whacky... there'll be a tiny 
                {   //race condition if multiple people are casting cure on the same target... 
                    if (m.Poison != null)
                    {
                        //desired is: LP: 50%, DP: 90% GP-: 100%
                        double multiplier = 0.5 + 0.4 * (4 - m.Poison.Level);
                        chance = Caster.Skills[SkillName.Magery].Value * multiplier;
                    }

                    if (Caster.AccessLevel > AccessLevel.Player)
                    {
                        Caster.SendMessage("Chance to cure is " + chance + "%");
                    }
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                /*
                //new cure rates
                int chance = 100;
                Poison p = m.Poison;
                try
                {
                    if( p != null )
                    {
                        chance = 10000 
                            + (int)(Caster.Skills[SkillName.Magery].Value * 75) 
                            - ((p.Level + 1) * 1750);
                        chance /= 100;
                        if( p.Level > 3 ) //lethal poison further penalty
                        {
                            chance -= 35; //@ GM magery, chance will be 52%
                        }
                    }

                    if( Caster.AccessLevel > AccessLevel.Player )
                    {
                        Caster.SendMessage("Chance to cure is " + chance + "%");
                    }
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                */

                if (!m.Incurable && Utility.Random(0, 100) <= chance)
                {
                    if (m.CurePoison(Caster))
                    {
                        if (Caster != m)
                            Caster.SendLocalizedMessage(1010058); // You have cured the target of all poisons!

                        m.SendLocalizedMessage(1010059); // You have been cured of all poisons.
                    }
                }
                else
                {
                    Caster.SendLocalizedMessage(1010060); // You have failed to cure your target!
                }

                m.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
                m.PlaySound(0x1E0);
            }

            FinishSequence();
        }

        public void TargetOnIslandSiege(IPoint3D p)
        {
            if (!Caster.CanSee(p))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }

            #region OBSOLET AI implementation
#if false
            else if (CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                SpellHelper.GetSurfaceTop(ref p);

                List<Mobile> targets = new List<Mobile>();

                Map map = Caster.Map;
                Mobile m_directtarget = p as Mobile;

                if (map != null)
                {
                    //you can target directly someone/something and become criminal if it's a criminal action
                    if (m_directtarget != null)
                        targets.Add(m_directtarget);

                    IPooledEnumerable eable = map.GetMobilesInRange(new Point3D(p), 2);

                    foreach (Mobile m in eable)
                    {
                        //Pix - below is the current RunUO code - change it to be simpler :)
                        //                        // Archcure area effect won't cure aggressors or victims, nor murderers, criminals or monsters 
                        //                        // plus Arch Cure Area will NEVER work on summons/pets if you are in Felucca facet
                        //                        // red players can cure only themselves and guildies with arch cure area.
                        //
                        //                        if (map.Rules == MapRules.FeluccaRules)
                        //                        {
                        //                            if (Caster.CanBeBeneficial(m, false) && (!Core.AOS || !IsAggressor(m) && !IsAggressed(m) && ((IsInnocentTo(Caster, m) && IsInnocentTo(m, Caster)) || (IsAllyTo(Caster, m))) && m != m_directtarget && m is PlayerMobile || m == Caster && m != m_directtarget))
                        //                                targets.Add(m);
                        //                        }
                        //                        else if (Caster.CanBeBeneficial(m, false) && (!Core.AOS || !IsAggressor(m) && !IsAggressed(m) && ((IsInnocentTo(Caster, m) && IsInnocentTo(m, Caster)) || (IsAllyTo(Caster, m))) && m != m_directtarget || m == Caster && m != m_directtarget))
                        //                            targets.Add(m);
                        if (Caster.CanBeBeneficial(m, false))
                        {
                            targets.Add(m);
                        }
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

                        Caster.DoBeneficial(m);

                        Poison poison = m.Poison;

                        if (poison != null)
                        {
                            int chanceToCure = 10000 + (int)(Caster.Skills[SkillName.Magery].Value * 75) - ((poison.Level + 1) * 1750);
                            chanceToCure /= 100;
                            chanceToCure -= 1;

                            if (chanceToCure > Utility.Random(100) && m.CurePoison(Caster))
                                ++cured;
                        }

                        m.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
                        m.PlaySound(0x1E0);
                    }

                    if (cured > 0)
                        Caster.SendLocalizedMessage(1010058); // You have cured the target of all poisons!
                }
            }
#endif
            #endregion OBSOLET AI implementation

            else if (CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                SpellHelper.GetSurfaceTop(ref p);

                List<Mobile> targets = new List<Mobile>();

                Map map = Caster.Map;
                Mobile directTarget = p as Mobile;

                if (map != null)
                {
                    bool feluccaRules = (map.Rules == MapRules.FeluccaRules);

                    // You can target any living mobile directly, beneficial checks apply
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
                            int chanceToCure = 10000 + (int)(Caster.Skills[SkillName.Magery].Value * 75) - ((poison.Level + 1) * 1750);
                            chanceToCure /= 100;
                            chanceToCure -= 1;

                            if (chanceToCure > Utility.Random(100) && m.CurePoison(Caster))
                                ++cured;
                        }

                        m.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
                        m.PlaySound(0x1E0);
                    }

                    if (cured > 0)
                        Caster.SendLocalizedMessage(1010058); // You have cured the target of all poisons!
                }
            }

            FinishSequence();
        }
        private bool AreaCanTarget(Mobile target, bool feluccaRules)
        {
            /* Arch cure area effect won't cure aggressors, victims, murderers, criminals or monsters.
			 * In Felucca, it will also not cure summons and pets.
			 * For red players it will only cure themselves and guild members.
			 */

            if (!Caster.CanBeBeneficial(target, false))
                return false;
            // 5/15/23, Adam: I think ArchCure was just broken in our Pun13.6 era as you would heal enemies and monsters
            //  whereby flagging yourself gray. We will adopt the AOS style of ArchCure under the 95% rule
            if (/*Core.AOS &&*/ target != Caster)
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
            return (Notoriety.Compute(from, (Mobile)to) == Notoriety.Innocent);
        }

        private static bool IsAllyTo(Mobile from, Mobile to)
        {
            return (Notoriety.Compute(from, (Mobile)to) == Notoriety.Ally);
        }

        public class InternalTarget : Target
        {
            private ArchCureSpell m_Owner;

            public InternalTarget(ArchCureSpell owner)
                : base(12, false, TargetFlags.Beneficial)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (Core.RuleSets.SiegeStyleRules()) //switch targetting for Siege
                {
                    IPoint3D p = o as IPoint3D;
                    if (p != null)
                    {
                        m_Owner.TargetOnIslandSiege(p);
                    }
                }
                else
                {
                    if (o is Mobile)
                    {
                        m_Owner.Target((Mobile)o);
                    }
                }
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}