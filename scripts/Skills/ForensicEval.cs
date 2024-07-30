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

/* Scripts/Skills/ForensicEval.cs
 * CHANGELOG:
 *  2/12/06, Plasma
 *      Added perma grey detection and skill delay
 *	6/26/05, Kit
		Fixed spelling error
 *	4/13/05, Adam
 *		Fixed spelling error
 *	1/24/05, Darva
 *		Made staff names only visible to a evaluator with the same, or higher access.
 *	5/26/04, Pixie
 *		Changed to use utility functions in BountyKeeper instead of iterating
 *		through the bounties arraylist manually.
 */

using Server.BountySystem;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Text;

namespace Server.SkillHandlers
{
    public class ForensicEvaluation
    {

        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.Forensics].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.Target = new ForensicTarget();
            m.RevealingAction();
            m.SendLocalizedMessage(500906); // What would you like to evaluate?

            return TimeSpan.FromSeconds(1.0); // default 1 second - 10 seconds is applied on valid target
        }

        public class ForensicTarget : Target
        {
            public ForensicTarget()
                : base(10, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {

                // Mobile from must have at least 50% of Mobile target's Stealing skill
                double targetstealing = 0;
                if (target is PlayerMobile)
                {
                    targetstealing = ((PlayerMobile)target).Skills[SkillName.Stealing].Value;
                }

                double skillvalue = from.Skills[SkillName.Forensics].Value;
                double requiredFE = targetstealing / 2;
                bool wanted = false;
                bool bonusExists = false;
                int amount = 0;

                //10 second defualt delay, reset this if invalid target
                from.NextSkillTime = Core.TickCount + (int)TimeSpan.FromSeconds(10.0).TotalMilliseconds;

                if (target is Mobile)
                {

                    // Bounties ----
                    if (target is PlayerMobile)
                    {

                        if (skillvalue > 80)
                        {
                            //foreach( Bounty b in BountyKeeper.Bounties )
                            //{
                            //	if ( b.WantedPlayer == (PlayerMobile)target )
                            //	{
                            //		wanted = true;
                            //		amount = b.Reward;
                            //		bonusExists = b.LBBonus;
                            //	}
                            //}
                            if (BountyKeeper.BountiesOnPlayer((PlayerMobile)target) > 0)
                            {
                                wanted = true;
                            }
                            amount = BountyKeeper.RewardForPlayer((PlayerMobile)target);
                            bonusExists = BountyKeeper.IsEligibleForLBBonus((PlayerMobile)target);

                            if (bonusExists)
                            {
                                amount += BountyKeeper.CurrentLBBonusAmount;
                            }

                            int infoAmount = 1;

                            if (skillvalue > 80)
                                infoAmount = 1;
                            if (skillvalue > 95)
                                infoAmount = Utility.Random(1, 2);
                            if (skillvalue > 98)
                                infoAmount = Utility.Random(2, 3);
                            if (skillvalue == 100)
                                infoAmount = 3;

                            string whoWants = bonusExists ? "Lord British" : "an Independant Party";

                            switch (infoAmount)
                            {
                                case 0:
                                    {
                                        from.SendMessage("You don't recall any bounties out on this person.");

                                        break;
                                    }
                                case 1:
                                    {
                                        if (wanted)
                                            from.SendMessage("That person is wanted.");
                                        else
                                            from.SendMessage("That person is not wanted.");

                                        break;
                                    }
                                case 2:
                                    {
                                        if (wanted)
                                            from.SendMessage("That person is wanted by {0}.", whoWants);
                                        else
                                            from.SendMessage("That person is not wanted.");

                                        break;
                                    }
                                case 3:
                                    {
                                        if (wanted)
                                        {
                                            from.SendMessage("That person is wanted by {0} for {1} GP!", whoWants, amount.ToString());
                                        }
                                        else
                                        {
                                            from.SendMessage("That person is not wanted.");
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        if (wanted)
                                        {
                                            from.SendMessage("You don't recall any bounties out on this person.");
                                        }
                                        else
                                        {
                                            from.SendMessage("That person is not wanted.");
                                        }
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            from.SendMessage("You fail to recall any bounties out on this person.");
                        }
                    }
                    else
                    {
                        from.SendMessage("You can not evaluate their status.");
                    }

                    // Theives' guild ----
                    // Plasma : Detection of perma grey in here too

                    // user FE must be at least half of target's stealing
                    if (from.CheckTargetSkill(SkillName.Forensics, target, requiredFE, 100.0, new object[2] { target, null }/*contextObj*/))
                    {
                        if (target is PlayerMobile && ((PlayerMobile)target).NpcGuild == NpcGuild.ThievesGuild)
                            from.SendMessage("That person is a scoundrel!");
                        //from.SendLocalizedMessage( 501004 );//That individual is a thief!
                        else
                        {
                            from.SendMessage("They look fairly honest..");
                            //pla: stop here as we don't do perma unless they are a thief
                            return;
                        }
                        //from.SendLocalizedMessage( 501003 );//You notice nothing unusual.

                        // plasma: detection routine for perma grey!
                        // -----------------------------------------
                        // a) Skill vs Skill check,  FE vs Staling
                        // b) Upon success, very small (scaled with FE) chance for false information
                        // c) Display their status   

                        bool bCriminal = false;         // perma?
                        Mobile targ = (Mobile)target;
                        if (targ == null)
                            return;                 // just in case

                        // Grab steal and FE values with a bit of randomess (I stole this direct from DetectHidden.cs!)
                        double ss = from.Skills[SkillName.Forensics].Value + Utility.Random(21) - 10;
                        double ts = targ.Skills[SkillName.Stealing].Value + Utility.Random(21) - 10;

                        // compare skills 
                        if (from.AccessLevel >= targ.AccessLevel && (ss >= ts))
                        {
                            // is the target perma?
                            bCriminal = (((PlayerMobile)targ).PermaFlags.Count > 0);

                            // now a small chance for false information, scaled 1% for each % from GM FE
                            if (Utility.RandomDouble() < (0.01 * (100 - from.Skills[SkillName.Forensics].Value + 1)))
                                bCriminal = !bCriminal;

                            // Display success message
                            if (bCriminal)
                                from.SendMessage("You identify them as a criminal!");
                            else
                                from.SendMessage("They appear to be a law abiding citizen");
                        }
                        else
                        {
                            //Display fail message
                            from.SendMessage("You cannot gather enough evidence to reach a reliable conclusion on their criminal status");
                        }//end perma
                    }
                    else
                    {
                        // changed from SendLocalizedMessage to be more intuative
                        from.SendMessage("You fail to determine anything useful about that individual's character.");
                    }
                }
                else if (target is Corpse)
                {

                    // Looters ----
                    if (from.CheckTargetSkill(SkillName.Forensics, target, 25.0, 100.0, new object[2] { (target as Corpse).Owner, null } /*contextObj*/))
                    {
                        Corpse c = (Corpse)target;
                        Mobile killer = c.Killer;

                        if (killer != null)
                        {
                            c.LabelTo(from, "They were killed by {0}", killer.Name);
                        }
                        else
                        {
                            c.LabelTo(from, "They were killed by no one.");
                        }

                        if (c.Looters.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder();
                            for (int i = 0; i < c.Looters.Count; i++)
                            {
                                if (((Mobile)c.Looters[i]).AccessLevel <= from.AccessLevel)
                                {
                                    if (sb.ToString() != "")
                                        sb.Append(", ");
                                    sb.Append(((Mobile)c.Looters[i]).Name);
                                }
                            }
                            if (sb.ToString() != "")
                                c.LabelTo(from, 1042752, sb.ToString());//This body has been distrubed by ~1_PLAYER_NAMES~
                            else
                                c.LabelTo(from, 501002);
                        }
                        else
                        {
                            c.LabelTo(from, 501002);//The corpse has not been desecrated.
                        }

                    }
                    else
                    {
                        from.SendLocalizedMessage(501001);//You cannot determain anything useful.
                    }
                }
                else if (target is ILockpickable)
                {
                    // Lockables ----
                    ILockpickable p = (ILockpickable)target;

                    if (p.Picker != null)
                    {
                        from.SendLocalizedMessage(1042749, p.Picker.Name);//This lock was opened by ~1_PICKER_NAME~
                    }
                    else
                    {
                        from.SendLocalizedMessage(501003);//You notice nothing unusual.
                    }
                }
                else
                {
                    from.NextSkillTime = Core.TickCount + (int)TimeSpan.FromSeconds(1.0).TotalMilliseconds;
                    from.SendMessage("You have specified an invalid target. Try again.");
                    from.Target = new ForensicTarget();
                    from.RevealingAction();
                }
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                //pla: on target cancel, reset netx skill time to 1 second
                from.NextSkillTime = Core.TickCount + (int)TimeSpan.FromSeconds(1.0).TotalMilliseconds;
            }
        }
    }
}