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

/* Skills/RemoveTrap.cs
 * CHANGELOG:
 *  6/7/23, Yoar
 *      In RunUO, it's impossible to disarm level 5 chests. Is the RunUO implementation wrong?
 *      Let's instead use {-30, +10} as our difficulty offset, similar to how lockpicking works.
 *      Also, adjusted the "oops" rate to be more similar to that of poisoning.
 *  5/21/23, Yoar
 *      Added era-accurate Remove Trap
 *      Conditioned all customizations for AI/MO
 *	3/6/11, add in faction trap stuffs
 *  11/23/06, Plasma
 *       Modified to only apply skill delay OnTarget()
 *  8/8/04, Pixie
 *		Added functionality for tripping the trap if you fail to disarm it.
 *  7/10/04, Old Salty
 * 		Removed debug message for remove trap unless you are GM or higher accesslevel.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/20/2004, Pixie
 *		Changed so that someone with removetrap+detecthidden > 150 
 *		can disarm a level 5 treasure map.
 */

using Server.Factions;
using Server.Items;
using Server.Network;
using Server.Targeting;
using System;

namespace Server.SkillHandlers
{
    public class RemoveTrap
    {
        /* AI/MO customizations to RT
         * This makes DH/RT essential for treasure hunters
         */
        public static bool EraAI { get { return (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules()); } }

        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.RemoveTrap].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            if (m.Skills[SkillName.Lockpicking].Value < 50)
            {
                m.SendLocalizedMessage(502366); // You do not know enough about locks.  Become better at picking locks.
            }
            else if (m.Skills[SkillName.DetectHidden].Value < 50)
            {
                m.SendLocalizedMessage(502367); // You are not perceptive enough.  Become better at detect hidden.
            }
            else
            {
                m.Target = new InternalTarget();

                m.SendLocalizedMessage(502368); // Wich trap will you attempt to disarm?
            }

            return TimeSpan.FromSeconds(1.0); // pla: changed this to 1 second.  10 seconds applied OnTarget()
        }

        private class InternalTarget : Target
        {
            public InternalTarget()
                : base(2, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                bool resetSkillTime = false;

                if (targeted is Mobile)
                {
                    from.SendLocalizedMessage(502816); // You feel that such an action would be inappropriate
                }
                else if (targeted is TrapableContainer)
                {
                    resetSkillTime = true;

                    TrapableContainer targ = (TrapableContainer)targeted;

                    from.Direction = from.GetDirectionTo(targ);

                    if (targ.TrapType == TrapType.None)
                    {
                        from.SendLocalizedMessage(502373); // That doesn't appear to be trapped
                        return;
                    }

                    from.PlaySound(0x241);

                    double minSkill, maxSkill;

                    if (EraAI)
                    {
                        double bonus = 0.0;

                        /* AI/MO: Modify minskill with detect hidden skill. What this means is that
                         * with a trappower of 125 (level 5 tmap) and GM DH, min RT skill to unlock
                         * is 75 and max is 100, OR, rather, DH and RT need to be above a combined
                         * 175 to have a chance at disarming the trap. Level 4 power (100), they
                         * need to have above 150 total to have a chance.
                         */
                        if (from.Skills[SkillName.DetectHidden].Base > 50.0)
                            bonus = (from.Skills[SkillName.DetectHidden].Base - 50.0);

                        minSkill = targ.TrapPower - bonus;
                        maxSkill = targ.TrapPower - bonus + 50.0;
                    }
                    else
                    {
                        /* 6/7/23, Yoar: In RunUO, it's impossible to disarm level 5 chests. Is
                         * the RunUO implementation wrong? Let's instead use {-30, +10} as our
                         * difficulty offset, similar to how lockpicking works.
                         */
#if RunUO
                        minSkill = targ.TrapPower;
                        maxSkill = targ.TrapPower + 30.0;
#else
                        minSkill = targ.TrapPower - 30.0;
                        maxSkill = targ.TrapPower + 10.0;
#endif
                    }

                    if (from.AccessLevel >= AccessLevel.GameMaster)
                        from.SendMessage("minskill is {0}, maxskill is {1}", minSkill, maxSkill);

                    if (from.CheckTargetSkill(SkillName.RemoveTrap, targ, minSkill, maxSkill, new object[2] /*contextObj*/))
                    {
                        targ.TrapPower = 0;
                        targ.TrapLevel = 0;
                        targ.TrapType = TrapType.None;
                        from.SendLocalizedMessage(502377); // You successfully render the trap harmless

                        // 5/12/23, Adam: 
                        if (targeted is DungeonTreasureChest dtc)
                            dtc.DecayChest(from);
                    }
                    else if (EraAI)
                    {
                        double skillValue = from.Skills[SkillName.RemoveTrap].Value;

                        // minimum of 1/200 trip, maximum of 199/200 trip
                        double triggerChance = Math.Max(0.005, Math.Min(0.995, (targ.TrapPower - skillValue) / 800.0));

                        triggerChance *= targ.TrapSensitivity;

                        if (from.AccessLevel > AccessLevel.Player)
                            from.SendMessage("Chance to trip trap: {0}", triggerChance);

                        if (Utility.RandomDouble() < triggerChance)
                        {
                            targ.ExecuteTrap(from);
                            from.RevealingAction();
                            from.SendLocalizedMessage(502370); // Oops.
                        }
                        else
                        {
                            from.SendLocalizedMessage(502371); // You breathe a sigh of relief as you fail to disarm the trap but don't set it off.
                        }
                    }
                    else if (from.Skills[SkillName.RemoveTrap].Base < 80.0 && Utility.Random(20) == 0) // just a guess
                    {
                        targ.ExecuteTrap(from);
                        from.RevealingAction(); // 5/21/23, Yoar: Only OSI, it seems that you are revealed when you take damage
                        from.SendLocalizedMessage(502370); // Oops.
                    }
                    else
                    {
                        from.SendLocalizedMessage(502371); // You breathe a sigh of relief as you fail to disarm the trap but don't set it off.
                    }
                }
                #region Factions
                else if (targeted is BaseFactionTrap)
                {
                    resetSkillTime = true;

                    BaseFactionTrap trap = (BaseFactionTrap)targeted;
                    Faction faction = Faction.Find(from);

                    FactionTrapRemovalKit kit = (from.Backpack == null ? null : from.Backpack.FindItemByType(typeof(FactionTrapRemovalKit)) as FactionTrapRemovalKit);

                    bool isOwner = (trap.Placer == from || (trap.Faction != null && trap.Faction.IsCommander(from)));

                    if (faction == null)
                    {
                        from.SendLocalizedMessage(1010538); // You may not disarm faction traps unless you are in an opposing faction
                    }
                    else if (faction == trap.Faction && trap.Faction != null && !isOwner)
                    {
                        from.SendLocalizedMessage(1010537); // You may not disarm traps set by your own faction!
                    }
                    else if (!isOwner && kit == null)
                    {
                        from.SendLocalizedMessage(1042530); // You must have a trap removal kit at the base level of your pack to disarm a faction trap.
                    }
                    else
                    {
                        if (from.CheckTargetSkill(SkillName.RemoveTrap, trap, 80.0, 100.0, new object[2] { trap.RootParent, null } /*contextObj*/) &&
                            from.CheckTargetSkill(SkillName.Tinkering, trap, 80.0, 100.0, new object[2] { trap.RootParent, null } /*contextObj*/))
                        {
                            from.PrivateOverheadMessage(MessageType.Regular, trap.MessageHue, trap.DisarmMessage, from.NetState);

                            if (!isOwner)
                            {
                                int silver = faction.AwardSilver(from, trap.SilverFromDisarm);

                                if (silver > 0)
                                    from.SendLocalizedMessage(1008113, true, silver.ToString("N0")); // You have been granted faction silver for removing the enemy trap :
                            }

                            trap.Delete();
                        }
                        else
                        {
                            from.SendLocalizedMessage(502372); // You fail to disarm the trap... but you don't set it off
                        }

                        if (!isOwner && kit != null)
                            kit.ConsumeCharge(from);
                    }
                }
                #endregion
                else
                {
                    from.SendLocalizedMessage(502373); // That does'nt appear to be trapped                    
                }

                if (resetSkillTime)
                {
                    //pla: set 10 second delay before next skill use
                    from.NextSkillTime = Core.TickCount + (int)TimeSpan.FromSeconds(10.0).TotalMilliseconds;
                }
            }
        }
    }
}