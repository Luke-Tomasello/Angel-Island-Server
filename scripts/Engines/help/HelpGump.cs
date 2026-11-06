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

/* Engines/Help/HelpGump.cs
 * Changelog:
 *  5/20/23, Yoar (Help Stuck)
 *      Complete refactor of help-stuck. Moved all code into StuckMenu.cs.
 *  12/2/22, Adam (Help Stuck)
 *      Overhaul of help stuck system.
 *          o) Add smart checks to see if the requester is actually near a means of escape.
 *              If so, give them some directional hints.
 *          o) Move mobile checks (is criminal etc.) from ValidUseLocation() into ValidUseMobile(). 
 *              Messaging is different.
 *          o) Reorganize logical flow. For instance, if they are physically blocked, try moving them first.
 *              If they are not physically blocked, begin the gumping process (or notify staff.)
 *  3/28/22, Adam
 *      Add special case for Townships to match 'remove thyself' processing for townships.
 *  2/10/22m Adam (FindStuckExit)
 *      Add magical pathing logic to find out if they are really stuck, then use that same pathing logic
 *      To find a viable teleportTo location.
 *	6/16/10, adam
 *		Add an additional check to TryMoveStuckPlayer() for housing regions. 
 *			We now check to make sure the player is actually inside the house and not for instance standing
 *			under the overhang of a tower.
 *	3/18/07, Pix
 *		Fixed logic for all cases for help stuck.
 *  03/12/07, plasma
 *      Fixed spelling mistake and added eable.Free before eable = nothing
 *  10/03/07, plasma
 *      -Logged HelpStuck attempts
 *      -Prevent auto-move if there are nearby items/mobs and log using RecordCheater.Cheater
 *	3/2/07, Pix
 *		Implemented random selection of 3 towns and 6 shrines for help stuck.
 *	3/1/07, Pix
 *		Tweaks to last night's change.
 *	2/28/07, Pix
 *		Modifications to the help-stuck system.
 *	08/06/06, Pix
 *		Added needed check for null.  Also added try/catch around the whole OnResponse function for added safety.
 *	08/05/06, weaver
 *		Emergency patch to disable if core AI feature set bit HelpStuckDisabled is set.
 *  08/05/06 Taran Kain
 *		Modified auto-move logic to use Region type rather than BaseHouse.FindHouseAt to determine whether or not a player is
 *		in a house - takes care of castle courtyard and keep stuck-spot situations.
 *	07/30/06 Taran Kain
 *		Added auto-move logic for help stuck.
 *	05/30/06, Adam
 *		Pass the sender to ValidUseLocation() so staff can send players from jail.
 *  05/01/06 Taran Kain
 *		Moved Help-stuck validation checks into StuckMenu.ValidUseLocation()
 *  04/11/06, Kit
 *		Added check to not bring up helpstuck menu if in lost lands.
 *  10/20/04, Froste
 *      Players could use "help stuck" to get out of the GM Jail, Fixed.
 *	9/27/04, Pigpen
 *		Made it so reds now get stuck menu and prisoners of AI (inmates) get a message telling them to come up with a better plan.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *   4/27/2004, pixie
 *     Made IsYoung() always return false - so players never get the option
 *     to go to Haven,Trammel.
 */

using Server.Diagnostics;
using Server.Gumps;
using Server.Menus.Questions;
using Server.Network;
using System;

namespace Server.Engines.Help
{
    public class ContainedMenu : QuestionMenu
    {
        private Mobile m_From;

        public ContainedMenu(Mobile from)
            : base("You already have an open help request. We will have someone assist you as soon as possible.  What would you like to do?", new string[] { "Leave my old help request like it is.", "Remove my help request from the queue." })
        {
            m_From = from;
        }

        public override void OnCancel(NetState state)
        {
            m_From.SendLocalizedMessage(1005306, "", 0x35); // Help request unchanged.
        }

        public override void OnResponse(NetState state, int index)
        {
            if (index == 0)
            {
                m_From.SendLocalizedMessage(1005306, "", 0x35); // Help request unchanged.
            }
            else if (index == 1)
            {
                PageEntry entry = PageQueue.GetEntry(m_From);

                if (entry != null && entry.Handler == null)
                {
                    m_From.SendLocalizedMessage(1005307, "", 0x35); // Removed help request.
                    PageQueue.Remove(entry);
                }
                else
                {
                    m_From.SendLocalizedMessage(1005306, "", 0x35); // Help request unchanged.
                }
            }
        }
    }

    public class HelpGump : Gump
    {
        public static void Initialize()
        {
            EventSink.HelpRequest += new HelpRequestEventHandler(EventSink_HelpRequest);
        }

        private static void EventSink_HelpRequest(HelpRequestEventArgs e)
        {
            foreach (Gump g in e.Mobile.NetState.Gumps)
            {
                if (g is HelpGump)
                    return;
            }

            if (!PageQueue.CheckAllowedToPage(e.Mobile))
                return;

            //e.Mobile.SendMessage("Help currently unavailable.");
            //return;

            if (PageQueue.Contains(e.Mobile))
                e.Mobile.SendMenu(new ContainedMenu(e.Mobile));
            else
                e.Mobile.SendGump(new HelpGump(e.Mobile));
        }

        private static bool IsYoung(Mobile m)
        {
            return false;
            //Server.Accounting.Account acct = m.Account as Account;
            //return ( acct != null && (DateTime.UtcNow - acct.Created) < TimeSpan.FromHours( 24.0 ) );
        }

        public HelpGump(Mobile from)
            : base(0, 0)
        {
            from.CloseGump(typeof(HelpGump));

            bool isYoung = IsYoung(from);

            AddBackground(50, 25, 540, 430, 2600);

            AddPage(0);

            AddHtmlLocalized(150, 50, 360, 40, 1001002, false, false); // <CENTER><U>Ultima Online Help Menu</U></CENTER>
            AddButton(425, 415, 2073, 2072, 0, GumpButtonType.Reply, 0); // Close

            AddPage(1);

            if (isYoung)
            {
                AddButton(80, 75, 5540, 5541, 9, GumpButtonType.Reply, 2);
                AddHtml(110, 75, 450, 58, @"<BODY><BASEFONT COLOR=BLACK><u>Young Player Haven Transport.</u> Select this option if you want to be transported to Haven.</BODY>", true, true);

                AddButton(80, 140, 5540, 5541, 1, GumpButtonType.Reply, 2);
                AddHtml(110, 140, 450, 58, @"<u>General question about Ultima Online.</u> Select this option if you have a general gameplay question, need help learning to use a skill, or if you would like to search the UO Knowledge Base.", true, true);

                AddButton(80, 205, 5540, 5541, 2, GumpButtonType.Reply, 0);
                AddHtml(110, 205, 450, 58, @"<u>My character is physically stuck in the game.</u> This choice only covers cases where your character is physically stuck in a location they cannot move out of. This option will only work two times in 24 hours.", true, true);

                AddButton(80, 270, 5540, 5541, 0, GumpButtonType.Page, 3);
                AddHtml(110, 270, 450, 58, @"<u>Another player is harassing me.</u> Another player is verbally harassing your character. When you select this option you will be sending a text log to Origin Systems. To see what constitutes harassment please visit http://support.uo.com/gm_9.html.", true, true);

                AddButton(80, 335, 5540, 5541, 0, GumpButtonType.Page, 2);
                AddHtml(110, 335, 450, 58, @"<u>Other.</u> If you are experiencing a problem in the game that does not fall into one of the other categories or is not addressed on the Support web page (located at http://support.uo.com), please use this option.", true, true);
            }
            else
            {
                AddButton(80, 90, 5540, 5541, 1, GumpButtonType.Reply, 2);
                AddHtml(110, 90, 450, 74, @"<u>General question about Ultima Online.</u> Select this option if you have a general gameplay question, need help learning to use a skill, or if you would like to search the UO Knowledge Base.", true, true);

                AddButton(80, 170, 5540, 5541, 2, GumpButtonType.Reply, 0);
                AddHtml(110, 170, 450, 74, @"<u>My character is physically stuck in the game.</u> This choice only covers cases where your character is physically stuck in a location they cannot move out of. This option will only work two times in 24 hours.", true, true);

                AddButton(80, 250, 5540, 5541, 0, GumpButtonType.Page, 3);
                AddHtml(110, 250, 450, 74, @"<u>Another player is harassing me.</u> Another player is verbally harassing your character. When you select this option you will be sending a text log to Origin Systems. To see what constitutes harassment please visit http://support.uo.com/gm_9.html.", true, true);

                AddButton(80, 330, 5540, 5541, 0, GumpButtonType.Page, 2);
                AddHtml(110, 330, 450, 74, @"<u>Other.</u> If you are experiencing a problem in the game that does not fall into one of the other categories or is not addressed on the Support web page (located at http://support.uo.com), please use this option.", true, true);
            }

            AddPage(2);

            AddButton(80, 90, 5540, 5541, 3, GumpButtonType.Reply, 0);
            AddHtml(110, 90, 450, 74, @"<u>Report a bug or contact Origin.</u> Use this option to launch your web browser and mail in a bug report. Your report will be read by our Quality Assurance Staff. We apologize for not being able to reply to individual reports. ", true, true);

            AddButton(80, 170, 5540, 5541, 4, GumpButtonType.Reply, 0);
            AddHtml(110, 170, 450, 74, @"<u>Suggestion for the Game.</u> If you'd like to make a suggestion for the game, it should be directed to the Development Team Members who participate in the discussion forums on the UO.Com web site. Choosing this option will take you to the Discussion Forums. ", true, true);

            AddButton(80, 250, 5540, 5541, 5, GumpButtonType.Reply, 0);
            AddHtml(110, 250, 450, 74, @"<u>Account Management</u> For questions regarding your account such as forgotten passwords, payment options, account activation, and account transfer, please choose this option.", true, true);

            AddButton(80, 330, 5540, 5541, 6, GumpButtonType.Reply, 0);
            AddHtml(110, 330, 450, 74, @"<u>Other.</u> If you are experiencing a problem in the game that does not fall into one of the other categories or is not addressed on the Support web page (located at http://support.uo.com), and requires in-game assistance, use this option. ", true, true);

            /* 11/23/22, Adam (client crasher)
             * Page 3 of the HelpGump crashes CUO (0.1.10.282) but not the OSI client
             * We will disable for now until we have a fix.
             * 12/16/22, Adam. Fixed in the latest version of CUO - enabling
             * 4/23/23, Yoar: Still crashes players using older versions of CUO. Also,
             * it's irrelevant information. Disabling again.
             */
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            try
            {
                Mobile from = state.Mobile;

                PageType type = (PageType)(-1);

                switch (info.ButtonID)
                {
                    case 0: // Close/Cancel
                        {
                            from.SendLocalizedMessage(501235, "", 0x35); // Help request aborted.

                            break;
                        }
                    case 1: // General question
                        {
                            type = PageType.Question;
                            break;
                        }
                    case 2: // Stuck
                        {
                            if (!StuckMenu.DoHelpStuck(from))
                                type = PageType.Stuck;

                            break;
                        }
                    case 3: // Report bug or contact Origin
                        {
                            type = PageType.Bug;
                            break;
                        }
                    case 4: // Game suggestion
                        {
                            type = PageType.Suggestion;
                            break;
                        }
                    case 5: // Account management
                        {
                            type = PageType.Account;
                            break;
                        }
                    case 6: // Other
                        {
                            type = PageType.Other;
                            break;
                        }
                    case 7: // Harassment: verbal/exploit
                        {
                            type = PageType.Harassment;
                            break;
                        }
                    case 8: // Harassment: physical
                        {
                            type = PageType.PhysicalHarassment;
                            break;
                        }
                    case 9: // Young player transport
                        {
                            /*
							if ( IsYoung( from ) )
							{
								if ( from.Region is Regions.Jail )
								{
									from.SendLocalizedMessage( 1041530, "", 0x35 ); // You'll need a better jailbreak plan then that!
								}
								else if ( from.Region.Name == "Haven" )
								{
									from.SendLocalizedMessage( 1041529 ); // You're already in Haven
								}
								else
								{
									from.MoveToWorld( new Point3D( 3618, 2587, 0 ), Map.Trammel );
								}
							}
							*/

                            break;
                        }
                }

                if (type != (PageType)(-1) && PageQueue.CheckAllowedToPage(from))
                    from.SendGump(new PagePromptGump(from, type));
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                System.Console.WriteLine("Exception caught in HelpGump.OnResponse: " + exc.Message);
                System.Console.WriteLine(exc.StackTrace.ToString());
            }
        }
    }

}