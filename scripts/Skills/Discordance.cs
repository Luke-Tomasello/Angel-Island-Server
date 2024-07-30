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

/* ChangeLog
 * 12/13/23, Yoar
 * 		Added ApplyDiscordance static method
 *  5/5/23, Yoar
 *      Implemented Enticement skill, which was in Pub16 replaced by the Discordance skill.
 *      Loosely based on the following sources:
 *      https://github.com/Delphi79/UO-Demo-Decompiled/blob/c5aed3fb712ecfdb2b082f8ef6285c3f395bd2bb/scripts.uosl/entice.uosl.q
 *      https://wiki.uosecondage.com/Enticement
 *	7/26/05, Adam
 *		Massive AOS cleanout
 *	11/10/04, Pix
 *		Added call to DoHarmful so that discordance is an aggressive action.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.SkillHandlers
{
    public class Discordance
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.Discordance].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.RevealingAction();

            BaseInstrument.PickInstrument(m, new InstrumentPickedCallback(OnPickedInstrument));

            return TimeSpan.FromSeconds(1.0); // Cannot use another skill for 1 second
        }

        public static void OnPickedInstrument(Mobile from, BaseInstrument instrument)
        {
            from.RevealingAction();

            if (PublishInfo.Publish >= 16.0 || Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules())
            {
                from.SendLocalizedMessage(1049541); // Choose the target for your song of discordance.
                from.Target = new DiscordanceTarget(from, instrument);
            }
            else
            {
                from.SendLocalizedMessage(500873); // Whom do you wish to entice?
                from.Target = new EnticeTarget(from, instrument);
            }
        }

        private class DiscordanceInfo
        {
            public Mobile m_From;
            public Mobile m_Creature;
            public DateTime m_EndTime;
            public Timer m_Timer;
            public double m_Scalar;
            public ArrayList m_Mods;

            public DiscordanceInfo(Mobile from, Mobile creature, TimeSpan duration, double scalar, ArrayList mods)
            {
                m_From = from;
                m_Creature = creature;
                m_EndTime = DateTime.UtcNow + duration;
                m_Scalar = scalar;
                m_Mods = mods;

                Apply();
            }

            public void Apply()
            {
                for (int i = 0; i < m_Mods.Count; ++i)
                {
                    object mod = m_Mods[i];

                    /*if ( mod is ResistanceMod )
						m_Creature.AddResistanceMod( (ResistanceMod) mod );
					else*/
                    if (mod is StatMod)
                        m_Creature.AddStatMod((StatMod)mod);
                    else if (mod is SkillMod)
                        m_Creature.AddSkillMod((SkillMod)mod);
                }
            }

            public void Clear()
            {
                for (int i = 0; i < m_Mods.Count; ++i)
                {
                    object mod = m_Mods[i];

                    /*if ( mod is ResistanceMod )
						m_Creature.RemoveResistanceMod( (ResistanceMod) mod );
					else*/
                    if (mod is StatMod)
                        m_Creature.RemoveStatMod(((StatMod)mod).Name);
                    else if (mod is SkillMod)
                        m_Creature.RemoveSkillMod((SkillMod)mod);
                }
            }
        }

        private static Hashtable m_Table = new Hashtable();

        public static Hashtable Table { get { return m_Table; } }

        public static bool GetScalar(Mobile targ, ref double scalar)
        {
            DiscordanceInfo info = m_Table[targ] as DiscordanceInfo;

            if (info == null)
                return false;

            scalar = info.m_Scalar;
            return true;
        }

        private static void ProcessDiscordance(object state)
        {
            DiscordanceInfo info = (DiscordanceInfo)state;
            Mobile from = info.m_From;
            Mobile targ = info.m_Creature;

            if (DateTime.UtcNow >= info.m_EndTime || targ.Deleted || from.Map != targ.Map || targ.GetDistanceToSqrt(from) > 16)
            {
                if (info.m_Timer != null)
                    info.m_Timer.Stop();

                info.Clear();
                m_Table.Remove(targ);
            }
            else
            {
                targ.FixedEffect(0x376A, 1, 32);
            }
        }

        public class DiscordanceTarget : Target
        {
            private BaseInstrument m_Instrument;

            public DiscordanceTarget(Mobile from, BaseInstrument inst)
                : base(BaseInstrument.GetBardRange(from, SkillName.Discordance), false, TargetFlags.Harmful)
            {
                m_Instrument = inst;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                from.RevealingAction();

                if (target is Mobile)
                {
                    Mobile targ = (Mobile)target;

                    // Adam - 10/24/22, Only make this an aggressive action if not BardImmune
                    if (!targ.IsInvulnerable && !(targ is BaseCreature && ((BaseCreature)targ).BardImmune))
                        //Pixie - 11/10/04 - added this so discordance is an aggressive action.
                        from.DoHarmful(targ, true, this);

                    if (targ.IsInvulnerable || targ is BaseCreature && ((BaseCreature)targ).BardImmune)
                    {
                        from.SendLocalizedMessage(1049535); // A song of discord would have no effect on that.
                    }
                    else if (!targ.Player)
                    {
                        double diff = m_Instrument.GetDifficultyFor(from, targ) - 10.0;
                        double music = from.Skills[SkillName.Musicianship].Value;

                        if (music > 100.0)
                            diff -= (music - 100.0) * 0.5;

                        if (!BaseInstrument.CheckMusicianship(from))
                        {
                            from.SendLocalizedMessage(500612); // You play poorly, and there is no effect.
                            m_Instrument.PlayInstrumentBadly(from);
                            m_Instrument.ConsumeUse(from);
                        }
                        else if (from.CheckTargetSkill(SkillName.Discordance, target, diff - 25.0, diff + 25.0, new object[2] { target, null } /*contextObj*/))
                        {
                            if (!m_Table.Contains(targ))
                            {
                                from.SendLocalizedMessage(1049539); // You play the song surpressing your targets strength
                                m_Instrument.PlayInstrumentWell(from);
                                m_Instrument.ConsumeUse(from);

                                ApplyDiscordance(from, targ);
                            }
                            else
                            {
                                from.SendLocalizedMessage(1049537);// Your target is already in discord.
                            }
                        }
                        else
                        {
                            from.SendLocalizedMessage(1049540);// You fail to disrupt your target
                            m_Instrument.PlayInstrumentBadly(from);
                            m_Instrument.ConsumeUse(from);
                        }
                    }
                    else
                    {
                        m_Instrument.PlayInstrumentBadly(from);
                    }
                }
            }
        }

        public static void ApplyDiscordance(Mobile from, Mobile targ)
        {
            ArrayList mods = new ArrayList();
            double scalar;

            if (Core.RuleSets.AOSRules())
            {
                double discord = from.Skills[SkillName.Discordance].Value;
                int effect;

                if (discord > 100.0)
                    effect = -20 + (int)((discord - 100.0) / -2.5);
                else
                    effect = (int)(discord / -5.0);

                scalar = effect * 0.01;

                mods.Add(new ResistanceMod(ResistanceType.Physical, effect));
                mods.Add(new ResistanceMod(ResistanceType.Fire, effect));
                mods.Add(new ResistanceMod(ResistanceType.Cold, effect));
                mods.Add(new ResistanceMod(ResistanceType.Poison, effect));
                mods.Add(new ResistanceMod(ResistanceType.Energy, effect));

                for (int i = 0; i < targ.Skills.Length; ++i)
                {
                    if (targ.Skills[i].Value > 0)
                        mods.Add(new DefaultSkillMod((SkillName)i, true, targ.Skills[i].Value * scalar));
                }
            }
            else
            {
                scalar = (from.Skills[SkillName.Discordance].Value / -5.0) / 100.0;

                mods.Add(new StatMod(StatType.Str, "DiscordanceStr", (int)(targ.RawStr * scalar), TimeSpan.Zero));
                mods.Add(new StatMod(StatType.Int, "DiscordanceInt", (int)(targ.RawInt * scalar), TimeSpan.Zero));
                mods.Add(new StatMod(StatType.Dex, "DiscordanceDex", (int)(targ.RawDex * scalar), TimeSpan.Zero));

                for (int i = 0; i < targ.Skills.Length; ++i)
                {
                    if (targ.Skills[i].Value > 0)
                        mods.Add(new DefaultSkillMod((SkillName)i, true, targ.Skills[i].Value * scalar));
                }
            }

            TimeSpan len = TimeSpan.FromSeconds(from.Skills[SkillName.Discordance].Value * 2);

            DiscordanceInfo info = new DiscordanceInfo(from, targ, len, scalar, mods);
            info.m_Timer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(1.25), new TimerStateCallback(ProcessDiscordance), info);

            m_Table[targ] = info;
        }

        #region Enticement

        private class EnticeTarget : Target
        {
            private BaseInstrument m_Instrument;
            private bool m_SetSkillTime = true;

            public EnticeTarget(Mobile from, BaseInstrument instrument)
                : base(BaseInstrument.GetBardRange(from, SkillName.Discordance), false, TargetFlags.None)
            {
                m_Instrument = instrument;
            }

            protected override void OnTargetFinish(Mobile from)
            {
                if (m_SetSkillTime)
                    from.NextSkillTime = Core.TickCount;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Mobile targ = targeted as Mobile;

                if (targ == null || targ.IsInvulnerable || (targ is BaseCreature && (((BaseCreature)targ).BardImmune || ((BaseCreature)targ).IsDeadPet)))
                {
                    from.SendLocalizedMessage(500879); // You cannot entice that!
                }
                else if (targ == from)
                {
                    from.SendLocalizedMessage(500880); // You cannot entice yourself!
                }
                else if (targ is BaseGuard || targ is PlayerVendor || targ is Barkeeper)
                {
                    from.SendLocalizedMessage(500887); // They look too dedicated to their job to be lured away.
                }
                else if (targ.Player)
                {
                    from.SendLocalizedMessage(500889); // You might have better luck with sweet words.
                }
                else
                {
                    m_SetSkillTime = false;

                    from.NextSkillTime = Core.TickCount + (int)TimeSpan.FromSeconds(10.0).TotalMilliseconds;

                    if (!BaseInstrument.CheckMusicianship(from))
                    {
                        from.SendLocalizedMessage(500612); // You play poorly, and there is no effect.
                        m_Instrument.PlayInstrumentBadly(from);
                        m_Instrument.ConsumeUse(from);
                    }
                    else
                    {
                        bool success;

                        // all bard skills are difficulty-based, let's not make an exception here
#if false
                        double totalSkill = 0.0;

                        totalSkill += Math.Min(100.0, from.Skills[SkillName.Musicianship].Value);
                        totalSkill += Math.Min(100.0, from.Skills[SkillName.Discordance].Value);

                        double chance = totalSkill / 200.0;

                        success = from.CheckTargetSkill(SkillName.Discordance, targ, chance);
#else
                        double diff = m_Instrument.GetDifficultyFor(from, targ) - 10.0;
                        double music = from.Skills[SkillName.Musicianship].Value;

                        if (music > 100.0)
                            diff -= (music - 100.0) * 0.5;

                        success = from.CheckTargetSkill(SkillName.Discordance, targ, diff - 25.0, diff + 25.0, new object[2] { targ, null } /*contextObj*/);
#endif

                        if (!success)
                        {
                            from.SendLocalizedMessage(500883); // Your music fails to attract them.
                            m_Instrument.PlayInstrumentBadly(from);
                            m_Instrument.ConsumeUse(from);
                        }
                        else
                        {
                            if (targ.Body.IsHuman)
                                targ.Say(500885); // What am I hearing?

                            from.SendLocalizedMessage(500886); // You play your hypnotic music, luring them near.
                            m_Instrument.PlayInstrumentWell(from);
                            m_Instrument.ConsumeUse(from);

                            targ.Direction = targ.GetDirectionTo(from);

                            if (targ is BaseCreature)
                            {
                                BaseCreature bc = (BaseCreature)targ;

                                bc.CurrentSpeed = bc.PassiveSpeed;

                                bc.Combatant = null;
                                bc.Warmode = false;

                                bc.Enticer = from;
                                bc.EnticeExpire = DateTime.UtcNow + TimeSpan.FromSeconds(10.0);
                            }
                        }
                    }
                }
            }
        }

        public static bool DoActionEntice(BaseCreature bc)
        {
            Mobile enticer = bc.Enticer;

            if (enticer == null || enticer.Deleted || !enticer.Alive || bc.Map != enticer.Map || bc.GetDistanceToSqrt(enticer) > 16 || !bc.CanSee(enticer) || DateTime.UtcNow >= bc.EnticeExpire)
            {
            }
            else if (bc is BaseVendor)
            {
                if (bc.Body.IsHuman)
                    bc.Say(500890); // Oh, but I cannot wander too far from my shop!
            }
            else if (bc.InRange(enticer, 1))
            {
                if (bc.Body.IsHuman)
                    bc.Say(500893); // It was you playing that lovely music!
            }
            else if (bc.AIObject != null && !bc.AIObject.DoMove(bc.GetDirectionTo(enticer), true))
            {
                if (bc.Body.IsHuman)
                    bc.Say(500891); // Hmm, I can't find that lovely music...

                enticer.SendLocalizedMessage(500892); // They seem unable to reach you.
            }
            else
            {
                return true;
            }

            bc.Enticer = null;

            return false;
        }

        #endregion
    }
}