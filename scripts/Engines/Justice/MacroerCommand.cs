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

/* Scripts/Engines/PlayerJusticeSystem/MacroerCommand.cs
 * ChangeLog
 *  5/10/23, Yoar (PJUMBountyCap)
 *      Capped macroer bounty
 *	5/17/10, adam
 *		make the Town Crier message reflect whether or not there is an actual/reasonable bounty
 *			this includes the macroer's gold + LB bonus
 *	3/25/09, Adam
 *		Auto jail player if caught macroing a second time (via the automatic system) within the 8 criminal phase.
 *		Players manually targeted by staff are not auto jailed.
 *	03/27/07, Pix
 *		Implemented RTT for AFK resource gathering thwarting.
 *	02/11/06, Adam
 *		Make common the formatting of sextant coords.
 *	01/13/06, Pix
 *		Changes due to TownCrierBulletinSystem/PlayerJusticeSystem separation.
 *  11/13/05 Taran Kain
 *		Tells the reporting staffmember how many times the player has been reported before.
 *	10/17/05, Adam
 *		Reduced access to AccessLevel.Counselor
 *	3/29/05, Adam
 *		changed sentense to 4 hours from 8
 *  3/28/05 - Pix
 *		Fixed random amount to depend on amount in macroers bank.
 *	3/28/05 - Pix
 *		Now doesn't add a macroer to the list if he's already on the list.
 *	3/27/05 - Pix
 *		Now tries to consume 1-3K from the macroer's bank account
 *	1/27/05 - Pix
 *		Initial Version.
 */

using Server.Accounting;
using Server.Commands;
using Server.Diagnostics;
using Server.Items;
using Server.Targeting;
using System;
using PlayerMobile = Server.Mobiles.PlayerMobile;

namespace Server.Engines
{
    public class MacroerCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("Macroer", AccessLevel.Counselor, new CommandEventHandler(Macroer_OnCommand));
        }

        public MacroerCommand()//Mobile m, DateTime dt, Location loc)
        {
        }

        public static void Macroer_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new MacroerTarget();
            e.Mobile.SendMessage("Who do you wish to report as a macroer?");
        }

        public static int HasGold(Mobile m)
        {
            int iAmountInBank = 0;
            Container cont = m.BankBox;
            if (cont != null)
            {
                Item[] golds = cont.FindItemsByType(typeof(Gold), true);
                foreach (Item g in golds)
                {
                    iAmountInBank += g.Amount;
                }
            }

            return iAmountInBank;
        }

        public static void ReportAsMacroer(Mobile reporter, PlayerMobile macroer)
        {
            string reason = "unattended resource gathering";
            //int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
            //bool xEast = false, ySouth = false;
            Map map = macroer.Map;

            if (macroer is not PlayerMobile pm)
                return;

            if (macroer.Account is not Account acct)
                return;

            string location = Utility.GetSextantLocation(macroer);

            if (map != null)
            {
                Region reg = macroer.Region;

                if (reg != map.DefaultRegion)
                {
                    location += (" in " + reg);
                }
            }

            //Output command log.
            if (reporter != null)
            {
                Server.Commands.CommandLogging.WriteLine(reporter, "{0} used [Macroer command on {1}({2}) - at {3}",
                    reporter.Name, macroer.Name, macroer.Serial, location);
            }

            if (reporter != null) reporter.SendMessage("Reporting {0} as an AFK macroer!", macroer.Name);

            int count = 0;
            foreach (AccountComment comm in acct.Comments)
            {
                if (comm.Content.IndexOf(reason) != -1)
                    count++;
            }
            if (reporter != null) reporter.SendMessage("{0} has been reported for macroing {1} times before.", macroer.Name, count);

            if (PlayerJusticeSystem.HasBeenReported(macroer))
            {
                if (reporter != null) reporter.SendMessage("{0} has already been reported.", macroer.Name);
                if (reporter == null)
                {   // the system is automatically jailing this player.
                    Jail.JailPlayer jt = new Jail.JailPlayer(macroer, 3, "Caught macroing again within 8 hours by automated system.", false);
                    jt.GoToJail();
                }
            }
            else
            {
                string[] lns = new string[2];

                // make the message reflect whether or not there is an actual/reasonable bounty
                if (HasGold(macroer) > 100 || BountyKeeper.CurrentLBBonusAmount > 100)
                    lns[0] = string.Format("A bounty has been placed on the head of {0} for unlawful resource gathering.", macroer.Name);
                else
                    lns[0] = string.Format("{0} is an enemy of the kingdom for unlawful resource gathering.", macroer.Name);

                lns[1] = string.Format("{0} was last seen at {1}.", macroer.Name, location);

                // Adam: AFK macroing sentencing (reduced from 8h to 4h)
                TimeSpan duration = TimeSpan.FromMinutes(CoreAI.AFKGrayMinutes);

                // Apply criminality + record authoritative account state
                pm.CriminalAction(message: false, ctx: null, Mobile.CriminalitySource.AFK, regionInform: false);

                // Record justice-system commentary using the authoritative sentence end
                DateTime sentenceEnds = DateTime.UtcNow + duration;
                ListEntry TCTextHandle =
                    PlayerJusticeSystem.AddMacroer(lns, macroer); // account comment

                //Add bounty to player
                BountyUtility.CreateAndRegisterBounty(reporter, macroer, TCTextHandle.EntryID);

                // Add comment to account
                Utility.AddIncarcerationComment(
                    reporter,
                    macroer,
                    reason,
                    duration,
                    location,                                   // location of mobile before
                    destination: "Jail");                       // where they are going
            }
        }

        private class MacroerTarget : Target
        {
            public MacroerTarget()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (targ is PlayerMobile)
                {
                    try
                    {
                        PlayerMobile pm = (PlayerMobile)targ;
                        MacroerCommand.ReportAsMacroer(from, pm);
                    }
                    catch (Exception except)
                    {
                        LogHelper.LogException(except);
                        System.Console.WriteLine("Caught exception in [macroer command: {0}", except.Message);
                        System.Console.WriteLine(except.StackTrace);
                    }
                }
                else
                {
                    from.SendMessage("Only players are macroers.");
                }
            }
        }

    }
}