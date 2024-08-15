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

/* Scripts/Commands/Jail.cs
 * CHANGELOG
 *  7/09/21, Yoar
 *      Refactored:
 *          Added array of jail cells
 *          Added more robust argument handling for the [jail command
 *          Added two DoJailing methods, one for system jailings and one for staff jailings
 *          Reduced the amount of copy pasta by adding the methods: GetDefaultSentence, StablePets, MoveToJail, LogJailing, FormatComment
 *          (*) Jailing behavior remains unchanged!
 *      JailGump:
 *          Targeting a player with the [jail command now opens the JailGump to review/edit the parameters of the jailing
 *	3/25/09, Adam
 *		Add command version of Jail command to be called by PJUM ReportAsMacroer (JailPlayer)
 *		When auto stabling, 
 *			ignore pets over stable limit (you're going to jail .. too bad)
 *			ignore pets that have something in their pack (you're going to jail .. too bad)
 *  1/12/08, Adam
 *      Stable pets if the jailed player has any
 *	6/14/06, Adam
 *		If the player is offline when they are jailed, set their map to internal
 *	5/30/06, Pix
 *		Changed LogoutLocation to jail when player is not online.
 *  04/11/06, Kit
 *		Added [JailTroublemaker command addition.
 *  11/06/05 Taran Kain
 *		Changed jail sentence to 12 + 12*prev infractions (jail or macroer)
 *  11/06/05 Taran Kain
 *		Changed jail sentence from 24 hours to 24hr + 24 * prev [jail sentences + 4 * [macroer convictions
 *	10/18/05, erlein
 *		Added logging of jailed players.
 *	09/12/05, Adam
 *		Make access Counselor
 *		Counselors can already 'pull' players to jail, and this is more of a 
 *		problem as they must then let them out, or tell the rest of staff to
 *		do so. Using [jail automates the exit process.
 *	09/01/05 Taran Kain
 *		First version. Jails a player and tags their account.
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Targeting;
using System;

namespace Server.Commands
{
    public static class Jail
    {
        public static void Initialize()
        {
            CommandSystem.Register("Jail", AccessLevel.Counselor, new CommandEventHandler(Jail_OnCommand));
        }

        private static readonly Point3D[] m_Cells = new Point3D[]
            {
                new Point3D(5276, 1164, 0),
                new Point3D(5286, 1164, 0),
                new Point3D(5296, 1164, 0),
                new Point3D(5306, 1164, 0),
                new Point3D(5276, 1174, 0),
                new Point3D(5286, 1174, 0),
                new Point3D(5296, 1174, 0),
                new Point3D(5306, 1174, 0),
                new Point3D(5283, 1184, 0),
                new Point3D(5304, 1184, 0),
            };

        private static Point3D GetCell(int cell)
        {
            int index = cell - 1;

            if (index >= 0 && index < m_Cells.Length)
                return m_Cells[index];

            return Point3D.Zero;
        }

        private static void Jail_OnCommand(CommandEventArgs e)
        {
            bool trouble = false;
            int cell = 3;
            string comment = "";

            int index = 0;

            while (index < e.Arguments.Length)
            {
                string key = e.GetString(index);

                switch (key)
                {
                    case "-troublemaker":
                        {
                            trouble = true;
                            comment = "were disrupting a staff event";

                            index++;
                            break;
                        }
                    case "-cell":
                        {
                            if (index >= e.Arguments.Length - 1)
                            {
                                Usage(e.Mobile);
                                return;
                            }
                            else
                            {
                                cell = e.GetInt32(index + 1);

                                if (cell < 1 || cell > m_Cells.Length)
                                {
                                    e.Mobile.SendMessage("Cells range from 1 to {0}.", m_Cells.Length);
                                    return;
                                }
                            }

                            index += 2;
                            break;
                        }
                    default: // if no keyword, the remainder of the input forms the comment
                        {
                            int count = e.Arguments.Length - index;

                            comment = string.Join(" ", e.Arguments, index, count);

                            index += count;
                            break;
                        }
                }
            }

            e.Mobile.SendMessage("Target the player to jail.");
            e.Mobile.Target = new JailTarget(cell, comment, trouble);
        }

        private static void Usage(Mobile to)
        {
            to.SendMessage("Usage: [jail [-troublemaker] [-cell <num>] [\"Tag Message\"]");
        }

        private class JailTarget : Target
        {
            private int m_Cell;
            private string m_Comment;
            private bool m_Trouble;

            public JailTarget(int cell, string comment, bool trouble)
                : base(-1, false, TargetFlags.None)
            {
                m_Cell = cell;
                m_Comment = comment;
                m_Trouble = trouble;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Mobile m = targeted as Mobile;

                if (m == null || !m.Player || m.AccessLevel != AccessLevel.Player)
                {
                    from.SendMessage("Only players can be sent to jail.");
                    return;
                }

                from.SendGump(new JailGump(from, m, m_Cell, m_Comment, m_Trouble));
            }
        }

        private class JailGump : Gump
        {
            private Mobile m_ToJail;
            private bool m_Trouble;

            public JailGump(Mobile jailer, Mobile toJail, int cell, string comment, bool trouble)
               : this(jailer, toJail, cell, comment, trouble, GetDefaultSentence(toJail, trouble))
            {
            }

            public JailGump(Mobile jailer, Mobile toJail, int cell, string comment, bool trouble, int sentence)
                : base(120, 50)
            {
                m_ToJail = toJail;
                m_Trouble = trouble;

                Account acct = toJail.Account as Account;

                string username;

                if (acct == null)
                    username = "-";
                else if (jailer.AccessLevel >= AccessLevel.GameMaster) // need GM access to see usernames
                    username = acct.Username;
                else
                    username = string.Format("Acct. of {0}", toJail.Name);

                AddBackground(0, 0, 420, 288, 9200);

                AddImageTiled(10, 10, 400, 268, 2624);
                AddAlphaRegion(10, 10, 400, 268);

                AddHtml(20, 11, 60, 20, Color("To Jail:"), false, false);
                AddButton(110, 10, 4011, 4013, 2, GumpButtonType.Reply, 0); // view player props
                AddHtml(145, 11, 240, 20, Color(toJail.Name), false, false);

                AddHtml(20, 35, 60, 20, Color("Account:"), false, false);
                AddButton(110, 34, 4011, 4013, 3, GumpButtonType.Reply, 0); // view account
                AddHtml(145, 35, 240, 20, Color(username), false, false);

                AddHtml(20, 59, 120, 20, Color(string.Format("Cell (1-{0}):", m_Cells.Length)), false, false);
                AddTextEntry(145, 59, 90, 20, 0x481, 0, cell.ToString());

                AddHtml(20, 83, 90, 20, Color("Sentence (hrs):"), false, false);
                AddTextEntry(145, 83, 90, 20, 0x481, 1, sentence.ToString());

                AddHtml(20, 107, 120, 20, Color("Comment:"), false, false);
                AddBackground(20, 128, 380, 100, 3000);
                AddTextEntry(23, 131, 376, 94, 0, 2, comment);

                AddHtml(20, 231, 120, 20, Color("Troublemaker:"), false, false);
                AddButton(110, 230, trouble ? 4006 : 4005, 4007, 4, GumpButtonType.Reply, 0); // toggle troublemaker
                AddHtml(145, 231, 150, 20, Color(trouble ? "Yes" : "No"), false, false);

                AddButton(20, 254, 4005, 4007, 1, GumpButtonType.Reply, 0);
                AddHtml(55, 255, 90, 20, Color("Submit"), false, false);

                AddButton(265, 254, 4017, 4019, 0, GumpButtonType.Reply, 0);
                AddHtml(300, 255, 90, 20, Color("Cancel"), false, false);
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (info.ButtonID == 0)
                    return;

                int cell = Utility.ToInt32(info.GetTextEntry(0).Text);
                int sentence = Utility.ToInt32(info.GetTextEntry(1).Text);
                string comment = info.GetTextEntry(2).Text;

                Mobile from = sender.Mobile;

                switch (info.ButtonID)
                {
                    case 1: // okay
                        {
                            if (cell < 1 || cell > m_Cells.Length)
                            {
                                from.SendMessage("Cells range from 1 to {0}.", m_Cells.Length);
                                from.SendGump(new JailGump(from, m_ToJail, cell, comment, m_Trouble, sentence));
                                return;
                            }
                            else if (sentence <= 0)
                            {
                                from.SendMessage("The sentence duration must be at least 1 hour.");
                                from.SendGump(new JailGump(from, m_ToJail, cell, comment, m_Trouble, sentence));
                                return;
                            }

                            DoJailing(from, m_ToJail, cell, comment, sentence, m_Trouble);
                            break;
                        }
                    case 2: // view player props
                        {
                            from.SendGump(new JailGump(from, m_ToJail, cell, comment, m_Trouble, sentence));
                            from.SendGump(new PropertiesGump(from, m_ToJail));
                            break;
                        }
                    case 3: // view account
                        {
                            from.SendGump(new JailGump(from, m_ToJail, cell, comment, m_Trouble, sentence));

                            if (from.AccessLevel < AccessLevel.Administrator)
                            {
                                from.SendMessage("You cannot access that.");
                                break;
                            }

                            Account acct = m_ToJail.Account as Account;

                            if (acct != null)
                                from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Information, 0, null, null, acct));

                            break;
                        }
                    case 4: // toggle troublemaker
                        {
                            from.SendGump(new JailGump(from, m_ToJail, cell, comment, !m_Trouble, sentence));
                            break;
                        }
                }
            }
        }

        public class JailPlayer
        {
            private Mobile m_ToJail;
            private int m_Cell;
            private string m_Comment;
            private bool m_Trouble;

            public JailPlayer(Mobile toJail, int cell, string comment, bool trouble)
            {
                m_ToJail = toJail;
                m_Cell = cell;
                m_Comment = comment;
                m_Trouble = trouble;
            }

            public void GoToJail()
            {
                if (m_ToJail == null)
                    return; // sanity

                int sentence = GetDefaultSentence(m_ToJail, m_Trouble);

                DoJailing(m_ToJail, m_Cell, m_Comment, sentence, m_Trouble);
            }
        }

        private static int GetDefaultSentence(Mobile toJail, bool trouble)
        {
            if (trouble)
                return 2; // two hour sentance for troublemakers

            int sentence = 12; // 12 hour minimum sentence

            Account acct = toJail.Account as Account;

            if (acct != null)
            {
                foreach (AccountComment comm in acct.Comments)
                {
                    if (comm.Content.IndexOf("Jailed for ") != -1 && comm.Content.IndexOf("Tag count: ") != -1)
                        sentence += 12; // 12 hours for each previous [jail'ing
                    else if (comm.Content.IndexOf(" : reported using the [macroer command") != -1)
                        sentence += 12; // 12 hours for every time they were caught resource botting
                }
            }

            return sentence;
        }

        // do jailing by system
        public static void DoJailing(Mobile toJail, int cell, string comment, int sentence, bool trouble)
        {
            Account acct = toJail.Account as Account;

            if (string.IsNullOrEmpty(comment))
                comment = "None";

            // stable the players pets
            Utility.StablePets(toJail);

            #region Disable Global Chat
            Server.Engines.Chat.ChatHelper.SetChatBan(toJail, true);
            #endregion Disable Global Chat

            // move to jail
            MoveToJail(toJail, cell);

            // register inmate
            JailExitSungate.AddInmate(toJail, sentence);

            toJail.SendMessage("You have been jailed for {0} hours.", sentence);

            // log the jailing to file
            LogJailing("SYSTEM", toJail, cell, comment, sentence);

            // add account comment
            if (acct != null)
                acct.Comments.Add(new AccountComment("SYSTEM", FormatComment(acct.Comments.Count + 1, comment, sentence, trouble)));
        }

        // do jailing by staff
        public static void DoJailing(Mobile jailer, Mobile toJail, int cell, string comment, int sentence, bool trouble)
        {
            Account acct = toJail.Account as Account;

            if (string.IsNullOrEmpty(comment))
                comment = "None";

            // stable the players pets
            int count = Utility.StablePets(toJail);
            if (count != 0)
                jailer.SendMessage("{0} pets have been stabled", count);

            // move to jail
            MoveToJail(toJail, cell);

            // register inmate
            JailExitSungate.AddInmate(toJail, sentence);

            toJail.SendMessage("You have been jailed for {0} hours.", sentence);
            jailer.SendMessage("{0} has been jailed for {1} hours.", toJail.Name, sentence);

            // log the jailing to file
            LogJailing(jailer.Name, toJail, cell, comment, sentence);

            // log the usage of the jail system
            CommandLogging.WriteLine(jailer, "{0} jailed {1}(Username: {2}) into cell {3} for {4} hours with reason: {5}.", jailer.Name, toJail.Name, acct == null ? "-" : acct.Username, cell, sentence, comment);

            // add account comment
            if (acct != null)
                acct.Comments.Add(new AccountComment(jailer.Name, FormatComment(acct.Comments.Count + 1, comment, sentence, trouble)));
        }
        private static void MoveToJail(Mobile m, int cell)
        {
            Point3D destPoint = Jail.GetCell(cell);

            m.MoveToWorld(destPoint, Map.Felucca);

            if (m.NetState == null)
            {
                m.LogoutLocation = destPoint;
                m.LogoutMap = Map.Felucca;
                m.Map = Map.Internal;
            }
        }

        private static void LogJailing(string jailer, Mobile toJail, int cell, string comment, int sentence)
        {
            LogHelper Logger = new LogHelper("jail.log", false, true);

            // TODO: More readable format?
            Logger.Log(LogType.Mobile, toJail, string.Format("{0}:{1}:{2}:{3}", jailer, cell, comment, sentence));

            Logger.Finish();
        }

        private static string FormatComment(int tagCount, string comment, int sentence, bool trouble)
        {
            // TODO: Simplify comment? There's no need to add the current date or the tag count. But then update GetDefaultSentence!
            return string.Format("{0}\nTag count: {1}\nJailed{2} for {3} hours. Reason: {4}", DateTime.UtcNow, tagCount, trouble ? "TroubleMaker" : "", sentence, comment);
        }
    }
}