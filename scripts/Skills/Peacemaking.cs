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

/* Scripts/Skills/PeaceMaking.cs
 * ChangeLog
 *  5/6/23, Yoar
 *      Added configurable skill-dependent area peace delay
 *  5/5/23, Yoar
 *      Implemented pre-Pub16 non-targetable peacemaking behavior.
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  2/1/08, Pix
 *      Protected RTT test from ever being called, even on TC, but left the code.
 *	8/31/07, Adam
 *		Change CheckTargetSkill() to check against a max skill of 100 instead of 120
 *	8/30/07, Adam
 *		Revert change below and move the logic to m_Instrument.GetDifficultyFor(creature)
 *	8/28/07, Adam
 *		Fail peace on Paragon creatures (with no message)
 *	8/26/07, Pix.
 *		Added RTT to peacemaking.
 *  1/26/07, Adam
 *      - new dynamic property system
 *      - Invoke the new OnPeace() which allows NPCs to speak out if someone tries to peace them
 *	1/5/06, weaver
 *		Made targetted peacing an aggressive action.
 *	6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.SkillHandlers
{
    using System;
    using System.Collections.Generic;

    public class Peacemaking
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.Peacemaking].Callback = new SkillUseCallback(OnUse);
        }

        private static TimeSpan m_SkillDelay; // hacky

        public static TimeSpan OnUse(Mobile m)
        {
            m.RevealingAction();

            m_SkillDelay = TimeSpan.FromSeconds(1.0); // Cannot use another skill for 1 second

            BaseInstrument.PickInstrument(m, new InstrumentPickedCallback(OnPickedInstrument));

            return m_SkillDelay;
        }

        public static void OnPickedInstrument(Mobile from, BaseInstrument instrument)
        {
            from.RevealingAction();

            if (PublishInfo.Publish >= 16.0 || Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisStyleRules())
            {
                from.SendLocalizedMessage(1049525); // Whom do you wish to calm?
                from.Target = new InternalTarget(from, instrument);
                from.NextSkillTime = Core.TickCount + (int)TimeSpan.FromHours(6.0).TotalMilliseconds;
            }
            else
            {
                bool success = PeaceArea(from, instrument);

                TimeSpan delay = ((PublishInfo.Publish >= 4.0 && success) ? TimeSpan.FromSeconds(5.0) : TimeSpan.FromSeconds(10.0));

                m_SkillDelay = delay; // ensure that 'OnUse' returns the correct skill delay

                from.NextSkillTime = Core.TickCount + (int)m_SkillDelay.TotalMilliseconds;
            }
        }

        private class InternalTarget : Target
        {
            private BaseInstrument m_Instrument;
            private bool m_SetSkillTime = true;

            public InternalTarget(Mobile from, BaseInstrument instrument)
                : base(BaseInstrument.GetBardRange(from, SkillName.Peacemaking), false, GetTargetFlags(from))
            {
                m_Instrument = instrument;
            }

            private static TargetFlags GetTargetFlags(Mobile from)
            {
                // Only modify behavior for Mortalis
                if (!Core.RuleSets.MortalisStyleRules())
                    return TargetFlags.None;

                if (from == null || from.Map == null || from.Region == null)
                    return TargetFlags.None;

                // If they are standing in a guarded town region,
                // we want the client to give the standard OSI criminal warning.
                if (from.Region.IsGuarded)
                    return TargetFlags.Harmful;

                return TargetFlags.None;
            }

            protected override void OnTargetFinish(Mobile from)
            {
                if (m_SetSkillTime)
                    from.NextSkillTime = Core.TickCount;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                from.RevealingAction();

                if (targeted is not Mobile)
                {
                    from.SendLocalizedMessage(1049528); // You cannot calm that!
                }
                /*else if (targeted is BaseCreature bc && bc.Uncalmable)
                {
                    from.SendLocalizedMessage(1049526); // You have no chance of calming that creature.
                }*/
                #region Dueling
                else if (from.Region.IsPartOf(typeof(Engines.ConPVP.SafeZone)))
                {
                    from.SendMessage("You may not peacemake in this area.");
                }
                else if (((Mobile)targeted).Region.IsPartOf(typeof(Engines.ConPVP.SafeZone)))
                {
                    from.SendMessage("You may not peacemake there.");
                }
                #endregion
                else
                {
                    m_SetSkillTime = false;

                    from.NextSkillTime = Core.TickCount + (int)TimeSpan.FromSeconds(10.0).TotalMilliseconds;

                    if (targeted == from)
                    {
                        // Standard mode : reset combatants for everyone in the area

                        // TODO: 5s skill delay if successful?
                        PeaceArea(from, m_Instrument);
                    }
                    else
                    {
                        // Target mode : pacify a single target for a longer duration
                        PeaceTarget(from, m_Instrument, (Mobile)targeted, ref m_SetSkillTime);
                    }
                }
            }
        }

        public static bool PeaceArea(Mobile from, BaseInstrument instrument)
        {
            if (!BaseInstrument.CheckMusicianship(from))
            {
                from.SendLocalizedMessage(500612); // You play poorly, and there is no effect.
                instrument.PlayInstrumentBadly(from);
                instrument.ConsumeUse(from);
            }
            else if (!from.CheckSkill(SkillName.Peacemaking, 0.0, 100.0, contextObj: new object[2]))
            {
                from.SendLocalizedMessage(500613); // You attempt to calm everyone, but fail.
                instrument.PlayInstrumentBadly(from);
                instrument.ConsumeUse(from);
            }
            else
            {
                instrument.PlayInstrumentWell(from);
                instrument.ConsumeUse(from);

                Map map = from.Map;

                if (map != null)
                {
                    int range = BaseInstrument.GetBardRange(from, SkillName.Peacemaking);

                    List<Mobile> targets = new List<Mobile>();

                    foreach (Mobile m in from.GetMobilesInRange(range))
                    {
                        // execute the new Peace Action, and allow the NPC to speak out!
                        if (m is BaseCreature)
                            ((BaseCreature)m).OnPeace();

                        if (m.AccessLevel > AccessLevel.Player)
                            continue;

                        if ((m is BaseCreature && ((BaseCreature)m).Uncalmable) || m == from || !from.CanBeHarmful(m, false))
                            continue;

                        targets.Add(m);
                    }

                    foreach (Mobile m in targets)
                    {
                        m.SendLocalizedMessage(500616); // You hear lovely music, and forget to continue battling!
                        m.Combatant = null;
                        m.Warmode = false;

                        BaseCreature? bc = m as BaseCreature;

                        if (bc != null && !bc.BardPacified)
                        {
                            // 5/6/23, Yoar: Added configurable skill-dependent area peace delay
                            double delay = 0.0;

                            delay += CoreAI.AreaPeaceDelayBase;
                            delay += CoreAI.AreaPeaceDelayBonus * (from.Skills[SkillName.Peacemaking].Value / 100.0);

                            if (targets.Count < CoreAI.AreaPeaceCrowdSize && CoreAI.AreaPeaceSoloBonus != 0.0)
                            {
                                double soloFactor = (double)(CoreAI.AreaPeaceCrowdSize - targets.Count) / (CoreAI.AreaPeaceCrowdSize - 1);
                                double soloBonus = soloFactor * CoreAI.AreaPeaceSoloBonus;

                                delay *= (1.0 + soloBonus);
                            }

                            // Contest inputs (used for break-on-damage / weakening logic)
                            // Be careful: match PeaceTarget difficulty math: instrument difficulty - 10, plus music>100 adjustment.
                            double music = from.Skills[SkillName.Musicianship].Value;
                            double peace = from.Skills[SkillName.Peacemaking].Value;

                            double diff = instrument.GetDifficultyFor(from, bc) - 10.0;

                            if (music > 100.0)
                                diff -= (music - 100.0) * 0.5;

                            bc.DebugSay(DebugFlags.Barding, "I have been pacified for {0:F2} seconds.", delay);
                            bc.Pacify(from, DateTime.UtcNow + TimeSpan.FromSeconds(delay), diff, music, peace);
                        }
                    }

                    if (targets.Count == 0)
                        from.SendLocalizedMessage(1049648); // You play hypnotic music, but there is nothing in range for you to calm.
                    else
                        from.SendLocalizedMessage(500615); // You play your hypnotic music, stopping the battle.
                }

                return true; // success
            }

            return false; // failure
        }

        public static void PeaceTarget(Mobile from, BaseInstrument instrument, Mobile targ, ref bool setSkillTime)
        {
            // adam: don't make it aggressive unless you have a chance to peace
            if (from.CanBeHarmful(targ, false) && !(targ is BaseCreature && ((BaseCreature)targ).Uncalmable))
                // wea: made this an aggressive action
                from.DoHarmful(targ);

            // execute the new Peace Action, and allow the NPC to speak out!
            if (targ is BaseCreature)
                ((BaseCreature)targ).OnPeace();

            if (!from.CanBeHarmful(targ, false))
            {
                from.SendLocalizedMessage(1049528);
                setSkillTime = true;
            }
            else if (targ is BaseCreature && ((BaseCreature)targ).Uncalmable)
            {
                from.SendLocalizedMessage(1049526); // You have no chance of calming that creature.
                setSkillTime = true;
            }
            else if (targ is BaseCreature && ((BaseCreature)targ).BardPacified)
            {
                from.SendLocalizedMessage(1049527); // That creature is already being calmed.
                setSkillTime = true;
            }
            else if (!BaseInstrument.CheckMusicianship(from))
            {
                from.SendLocalizedMessage(500612); // You play poorly, and there is no effect.
                instrument.PlayInstrumentBadly(from);
                instrument.ConsumeUse(from);
            }
            else
            {
                // Difficulty / player skill (Publish 15 targeted peace model)
                double diff = instrument.GetDifficultyFor(from, targ) - 10.0;
                double music = from.Skills[SkillName.Musicianship].Value;
                double peace = from.Skills[SkillName.Peacemaking].Value;

                if (music > 100.0)
                    diff -= (music - 100.0) * 0.5;

                if (!from.CheckTargetSkill(SkillName.Peacemaking, targ, diff - 25.0, diff + 25.0, new object?[2] { targ, null }/*contextObj*/))
                {
                    from.SendLocalizedMessage(1049531); // You attempt to calm your target, but fail.
                    instrument.PlayInstrumentBadly(from);
                    instrument.ConsumeUse(from);
                }
                else
                {
                    instrument.PlayInstrumentWell(from);
                    instrument.ConsumeUse(from);

                    if (targ is BaseCreature)
                    {
                        BaseCreature bc = (BaseCreature)targ;
                        from.SendLocalizedMessage(1049532); // You play hypnotic music, calming your target.

                        targ.Combatant = null;
                        targ.Warmode = false;

                        double seconds = 100 - (diff / 1.5);

                        if (seconds > 120)
                            seconds = 120;
                        else if (seconds < 10)
                            seconds = 10;

                        bc.Pacify(from, DateTime.UtcNow + TimeSpan.FromSeconds(seconds), diff, music, peace);
                    }
                    else
                    {
                        from.SendLocalizedMessage(1049532); // You play hypnotic music, calming your target.

                        targ.SendLocalizedMessage(500616); // You hear lovely music, and forget to continue battling!
                        targ.Combatant = null;
                        targ.Warmode = false;
                    }
                }
            }
        }
    }

}