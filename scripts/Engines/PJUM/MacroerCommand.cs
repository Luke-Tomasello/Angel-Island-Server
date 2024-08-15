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

/* Scripts/Engines/PJUM/MacroerCommand.cs
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
 *		Changes due to TCCS/PJUM separation.
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
using Server.BountySystem;
using Server.Commands;
using Server.Diagnostics;
using Server.Engines;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;

namespace Server.PJUM
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

        public static void ReportAsMacroer(Mobile from, PlayerMobile pm)
        {
            string location;
            int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;
            Map map = pm.Map;
            bool valid = Sextant.Format(pm.Location, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

            if (valid)
                location = Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
            else
                location = "????";

            if (!valid)
                location = string.Format("{0} {1}", pm.X, pm.Y);

            if (map != null)
            {
                Region reg = pm.Region;

                if (reg != map.DefaultRegion)
                {
                    location += (" in " + reg);
                }
            }

            //Output command log.
            if (from != null)
            {
                Server.Commands.CommandLogging.WriteLine(from, "{0} used [Macroer command on {1}({2}) - at {3}",
                    from.Name, pm.Name, pm.Serial, location);
            }

            if (from != null) from.SendMessage("Reporting {0} as an AFK macroer!", pm.Name);
            Account acct = pm.Account as Account;
            int count = 0;
            foreach (AccountComment comm in acct.Comments)
            {
                if (comm.Content.IndexOf(" : reported using the [macroer command") != -1)
                    count++;
            }
            if (from != null) from.SendMessage("{0} has been reported for macroing {1} times before.", pm.Name, count);

            if (PJUM.HasBeenReported(pm))
            {
                if (from != null) from.SendMessage("{0} has already been reported.", pm.Name);
                if (from == null)
                {   // the system is automatically jailing this player.
                    Jail.JailPlayer jt = new Jail.JailPlayer(pm, 3, "Caught macroing again within 8 hours by automated system.", false);
                    jt.GoToJail();
                }
            }
            else
            {
                string[] lns = new string[2];

                // make the message reflect whether or not there is an actual/reasonable bounty
                if (HasGold(pm) > 100 || BountyKeeper.CurrentLBBonusAmount > 100)
                    lns[0] = string.Format("A bounty has been placed on the head of {0} for unlawful resource gathering.", pm.Name);
                else
                    lns[0] = string.Format("{0} is an enemy of the kingdom for unlawful resource gathering.", pm.Name);

                lns[1] = string.Format("{0} was last seen at {1}.", pm.Name, location);

                // Adam: changed to 4 hours from 8
                ListEntry TCTextHandle = PJUM.AddMacroer(lns, pm, DateTime.UtcNow + TimeSpan.FromHours(4));

                //Add bounty to player
                string name = string.Format("Officer {0}", Utility.RandomBool() ? NameList.RandomName("male") : NameList.RandomName("female"));

                int bountyAmount = 0;
                Container cont = pm.BankBox;
                if (cont != null)
                {
                    int iAmountInBank = 0;

                    Item[] golds = cont.FindItemsByType(typeof(Gold), true);
                    foreach (Item g in golds)
                    {
                        iAmountInBank += g.Amount;
                    }

                    int randomAmount = Utility.RandomMinMax(iAmountInBank / 2, iAmountInBank);

                    // 5/10/23, Yoar: Let's cap this
                    if (randomAmount > CoreAI.PJUMBountyCap)
                        randomAmount = CoreAI.PJUMBountyCap;

                    if (cont.ConsumeTotal(typeof(Gold), randomAmount))
                    {
                        bountyAmount = randomAmount;
                    }
                }

                // note, from can be null (which is fine)
                Bounty bounty = null;
                bounty = new Bounty(from as PlayerMobile, pm, bountyAmount, name);
                if (bounty != null)
                {   // associate the town crier message with thie bounty so we can update the message once the bounty is collected.
                    bounty.TownCrierEntryID = TCTextHandle.EntryID;
                    BountyKeeper.Add(bounty);
                }

                //Add comment to account
                Account acc = pm.Account as Account;
                string comment = string.Format("On {0}, {1} caught {2} unattended macroing at {3} : reported using the [macroer command",
                    DateTime.UtcNow,
                    from != null ? from.Name : "auto-RTT",
                    pm.Name,
                    location);
                acc.Comments.Add(new AccountComment(from != null ? from.Name : "RTT SYSTEM", comment));
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