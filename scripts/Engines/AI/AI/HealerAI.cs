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

/* Scripts/Engines/AI/AI/HealerAI.cs
 * CHANGELOG
 *	1/1/09, Adam
 *		Add new Serialization model for creature AI.
 *		BaseCreature will already be serializing this data when I finish the upgrade, so all you need to do is add your data to serialize. 
 *		Make sure to use the SaveFlags to optimize saves by not writing stuff that is non default.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Second;
using Server.Targeting;
using System;

namespace Server.Mobiles
{
    public class HealerAI : BaseAI
    {
        private static NeedDelegate m_Cure = new NeedDelegate(NeedCure);
        private static NeedDelegate m_GHeal = new NeedDelegate(NeedGHeal);
        private static NeedDelegate m_LHeal = new NeedDelegate(NeedLHeal);
        private static NeedDelegate[] m_ACure = new NeedDelegate[] { m_Cure };
        private static NeedDelegate[] m_AGHeal = new NeedDelegate[] { m_GHeal };
        private static NeedDelegate[] m_ALHeal = new NeedDelegate[] { m_LHeal };
        private static NeedDelegate[] m_All = new NeedDelegate[] { m_Cure, m_GHeal, m_LHeal };

        public HealerAI(BaseCreature m)
            : base(m)
        {
        }

        public override bool Think()
        {
            Target targ = m_Mobile.Target;

            if (targ != null)
            {
                if (targ is CureSpell.InternalTarget)
                {
                    ProcessTarget(targ, m_ACure);
                }
                else if (targ is GreaterHealSpell.InternalTarget)
                {
                    ProcessTarget(targ, m_AGHeal);
                }
                else if (targ is HealSpell.InternalTarget)
                {
                    ProcessTarget(targ, m_ALHeal);
                }
                else
                {
                    targ.Cancel(m_Mobile, TargetCancelType.Canceled);
                }
            }
            else
            {
                Mobile toHelp = Find(m_All);

                if (toHelp != null)
                {
                    if (NeedCure(toHelp))
                    {

                        m_Mobile.DebugSay(DebugFlags.AI, "{0} needs a cure", toHelp.Name);

                        if (!(new CureSpell(m_Mobile, null)).Cast())
                            new CureSpell(m_Mobile, null).Cast();
                    }
                    else if (NeedGHeal(toHelp))
                    {

                        m_Mobile.DebugSay(DebugFlags.AI, "{0} needs a greater heal", toHelp.Name);

                        if (!(new GreaterHealSpell(m_Mobile, null)).Cast())
                            new HealSpell(m_Mobile, null).Cast();
                    }
                    else if (NeedLHeal(toHelp))
                    {

                        m_Mobile.DebugSay(DebugFlags.AI, "{0} needs a lesser heal", toHelp.Name);

                        new HealSpell(m_Mobile, null).Cast();
                    }
                }
                else
                {
                    if (AcquireFocusMob(m_Mobile.RangePerception, FightMode.All | FightMode.Weakest, false, true, false))
                    {
                        WalkMobileRange(m_Mobile.FocusMob, 1, false, 4, 7);
                    }
                    else
                    {
                        WalkRandomInHome(3, 2, 1);
                    }
                }
            }

            return true;
        }

        private delegate bool NeedDelegate(Mobile m);

        private void ProcessTarget(Target targ, NeedDelegate[] func)
        {
            Mobile toHelp = Find(func);

            if (toHelp != null)
            {
                if (targ.Range != -1 && !m_Mobile.InRange(toHelp, targ.Range))
                {
                    DoMove(m_Mobile.GetDirectionTo(toHelp) | Direction.Running);
                }
                else
                {
                    targ.Invoke(m_Mobile, toHelp);
                }
            }
            else
            {
                targ.Cancel(m_Mobile, TargetCancelType.Canceled);
            }
        }

        private Mobile Find(params NeedDelegate[] funcs)
        {
            if (m_Mobile.Deleted)
                return null;

            Map map = m_Mobile.Map;

            if (map != null)
            {
                double prio = 0.0;
                Mobile found = null;

                IPooledEnumerable eable = m_Mobile.GetMobilesInRange(m_Mobile.RangePerception);
                foreach (Mobile m in eable)
                {
                    if (!m_Mobile.CanSee(m) || !(m is BaseCreature) || ((BaseCreature)m).Team != m_Mobile.Team)
                        continue;

                    for (int i = 0; i < funcs.Length; ++i)
                    {
                        if (funcs[i](m))
                        {
                            double val = -m_Mobile.GetDistanceToSqrt(m);

                            if (found == null || val > prio)
                            {
                                prio = val;
                                found = m;
                            }

                            break;
                        }
                    }
                }
                eable.Free();

                return found;
            }

            return null;
        }

        private static bool NeedCure(Mobile m)
        {
            return m.Poisoned;
        }

        private static bool NeedGHeal(Mobile m)
        {
            return m.Hits < m.HitsMax - 40;
        }

        private static bool NeedLHeal(Mobile m)
        {
            return m.Hits < m.HitsMax - 10;
        }

        #region Serialize
        private SaveFlags m_flags;

        [Flags]
        private enum SaveFlags
        {   // 0x00 - 0x800 reserved for version
            unused = 0x1000
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;                                // current version (up to 4095)
            m_flags = m_flags | (SaveFlags)version;         // save the version and flags
            writer.Write((int)m_flags);

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            m_flags = (SaveFlags)reader.ReadInt();              // grab the version an flags
            int version = (int)(m_flags & (SaveFlags)0xFFF);    // maskout the version

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
            switch (version)
            {
                default: break;
            }

        }
        #endregion Serialize
    }
}