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

/* Scripts/Commands/Boot.cs
 * 	CHANGELOG:
 * 	5/7/23, Adam
 *	    Initial Version
 */

using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;

namespace Server.Commands
{
    public class Boot
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Boot", AccessLevel.GameMaster, new CommandEventHandler(Boot_OnCommand));
        }

        [Usage("Boot")]
        [Description("Boot the player from Prison or Jail.")]
        public static void Boot_OnCommand(CommandEventArgs e)
        {
            try
            {
                e.Mobile.SendMessage("Target the player to boot.");
                e.Mobile.Target = new BootTarget();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        private class BootTarget : Target
        {

            public BootTarget()
                : base(-1, false, TargetFlags.None)
            {

            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                PlayerMobile pm = targeted as Mobiles.PlayerMobile;
                Accounting.Account acct = pm.Account as Accounting.Account;
                if (pm == null || acct == null || pm.AccessLevel != AccessLevel.Player)
                {
                    from.SendMessage("Only players can be booted.");
                    return;
                }
                Region reg = pm.Region;
                if (reg == null)
                {
                    from.SendMessage("Error: No region information.");
                    return;
                }
                if (acct.GetFlag(Accounting.Account.IPFlags.IsTorExitNode) || acct.InfractionStatus == Accounting.Account.AccountInfraction.TorExitNode)
                {

                }
                LogHelper logger = new LogHelper("Boot Command.log", false, false, false);

                if (reg.IsAngelIslandRules)
                {
                    logger.Log(LogType.Mobile, from, string.Format("Using boot command from prison on {0}.", pm));
                    logger.Log(LogType.Text, string.Format("{0} Infraction Status is: {1}.", pm, pm.InfractionStatus));

                    // before allowing the exit, let's check a few things
                    AIParoleExit exit = new AIParoleExit();
                    bool can_exit = true;
                    if (exit.ExceedsIPLimit(pm))
                    {   // there are here with multiple accounts logged in
                        string text = "They must wait for their other account(s) to logout.";
                        from.SendMessage(text);
                        logger.Log(LogType.Text, text);
                        can_exit = false;
                    }
                    if (pm.ShortTermCriminalCounts > 0)
                    {
                        // We don't want people leaving on parole if they've not worked off their counts
                        string text = string.Format("They still have {0} criminal counts against them. They must have 0 to be paroled.", pm.ShortTermCriminalCounts);
                        from.SendMessage(text);
                        logger.Log(LogType.Text, text);
                        can_exit = false;
                    }
                    if (pm.ShortTermMurders > 4 && Core.RuleSets.AngelIslandRules())
                    {
                        // We don't want people leaving on parole if they've not worked out of stat-loss
                        string text = "They must have less than 5 short term murder counts to be paroled.";
                        from.SendMessage(text);
                        logger.Log(LogType.Text, text);
                        can_exit = false;
                    }
                    if (!pm.Alive)
                    {
                        // and we don't want them leaving as a ghost either.
                        string text = "They must be alive to exit.";
                        from.SendMessage(text);
                        logger.Log(LogType.Text, text);
                        can_exit = false;
                    }

                    // clear their MinimumSentence
                    logger.Log(LogType.Text, string.Format("clearing their MinimumSentence of {0}", pm.MinimumSentence));
                    pm.MinimumSentence = DateTime.MinValue;

                    if (can_exit)
                        // we're outta here!
                        exit.OnMoveOver(pm);

                    exit.Delete();
                }
                else if (reg.IsJailRules)
                {
                    logger.Log(LogType.Mobile, from, string.Format("Using boot command from jail on {0}.", pm));
                    string spawnTag = null;
                    Point3D spawnLoc = Point3D.Zero;
                    Map spawnMap = null;
                    Server.Misc.CharacterCreation.NewCharacterSpawnLocation(out spawnTag, out spawnLoc, out spawnMap);
                    pm.MoveToWorld(spawnLoc, spawnMap);
                }
                else
                {
                    from.SendMessage("This player is neither in prison or jail.");
                    return;
                }

                logger.Finish();

                from.SendMessage("Done.");
                return;
            }
        }
    }
}