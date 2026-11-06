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

/* Scripts/Commands/Jail.cs
 * CHANGELOG
 *  10/18/26, Adam (confinement Stage 5)
 *      First confinement wins: an automated (System profile) jailing of someone already confined (in a jail, or in a
 *      prison as an inmate: ConfinementBookkeeping.IsConfined) changes nothing and moves no one; the offense is recorded
 *      for staff and the result is a denial. Every JailPlayer.GoToJail caller is covered (Test Center, the macroer check,
 *      the bank check and other exploit catches, the inmate cron). A prison trespasser is still jailed. Staff jailing is
 *      unchanged.
 *  10/17/26, Adam
 *      Confinement Stage 2: the jail record, account flag and chat ban go through JailOperations /
 *      ConfinementBookkeeping (ConfinementOperations.cs). Same changes, same order; jailing behaviour is unchanged
 *      (system jailings still ban global chat, staff jailings still do not).
 *      Confinement Stage 3: the staff DoJailing takes an optional facet (each facet has its own Jail region and cells);
 *      null keeps Felucca, so every existing caller ([jail, the system jailings) is unchanged. The confinement
 *      coordinator passes the destination facet.
 *      Confinement Stage 4: [jail's Submit goes through the coordinator (CommandJailing), still to the Felucca jail with the
 *      gump's cell, sentence and comment: a prisoner is now transferred (prison check-out first, no stale prison state);
 *      someone already in jail is re-jailed by the legacy DoJailing, as before. Staff of strictly lower rank than the
 *      jailer can now be jailed: they are demoted to Player first. Equal or higher rank is refused as before.
 *  10/17/26, Adam (confinement admission refactor R4)
 *      Both DoJailing overloads (and so JailPlayer.GoToJail) delegate to the shared admission runner (JailAdmission,
 *      ConfinementAdmission.cs): the System profile (Felucca, global chat ban, "SYSTEM") and the Staff profile (the given
 *      facet, account jailed flag, jailer told, command log). Same steps, texts and logs; a cell that is not inside a jail
 *      on that facet, or a target that is not a player character, is refused before anything changes, and refusals and
 *      failures are written to jail.log and the console. MoveToJail and LogJailing are gone (the runner's steps).
 *  10/17/26, Adam (D5)
 *      [jail on someone already in a jail goes through the coordinator too (Rejail, RenewSentence): re-jailed in the
 *      Felucca jail with the gump's cell, sentence and comment, no check-out, as before; now logged to Confinement.log.
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
 *		Add command version of Jail command to be called by PlayerJusticeSystem ReportAsMacroer (JailPlayer)
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
using Server.Engines.ConfinementSystem;
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

        internal static Point3D GetCell(int cell)       // internal: the confinement coordinator validates the cell before jailing
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
                Mobile? m = targeted as Mobile;

                // staff can be jailed only by staff of strictly higher rank (they are demoted to Player on Submit);
                //  equal or higher rank is refused as before
                if (m == null || !m.Player || (m.AccessLevel != AccessLevel.Player && from.AccessLevel <= m.AccessLevel))
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

                Account? acct = toJail.Account as Account;

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

                            CommandJailing(from, m_ToJail, cell, comment, sentence, m_Trouble);
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

                            Account? acct = m_ToJail.Account as Account;

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
            private Mobile? m_ToJail;
            private int m_Cell;
            private string m_Comment;
            private bool m_Trouble;

            public JailPlayer(Mobile? toJail, int cell, string comment, bool trouble)
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

            Account? acct = toJail.Account as Account;

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

        // do jailing by system: the shared admission runner's System jail profile (JailAdmission, ConfinementAdmission.cs),
        //  always the Felucca jail: global chat banned, no account jailed flag, "SYSTEM" in the jail log, as before
        public static void DoJailing(Mobile toJail, int cell, string comment, int sentence, bool trouble)
        {
            RunJailing(JailProfile.System, World.GetSystemAcct(), toJail, cell, comment, sentence, Map.Felucca);
        }

        // [jail's Submit, through the confinement coordinator to the Felucca jail (the command's default): someone already in
        //  a jail (any facet) is re-jailed there (Rejail: new cell, sentence and comment, as before), a prisoner is
        //  transferred (prison check-out, then jailing), and lower-ranked staff are demoted first.
        private static void CommandJailing(Mobile from, Mobile toJail, int cell, string comment, int sentence, bool trouble)
        {
            ConfinementResult r = Confinement.Execute(new ConfinementRequest(toJail, from, TransitionAuthority.Staff, TransitionMode.Administrative,
                ConfinementDestination.Jail(Map.Felucca))
            { Comment = comment, JailCell = cell, JailHours = sentence, DemoteStaffTarget = toJail.AccessLevel > AccessLevel.Player, RenewSentence = true });

            if (r.Outcome == ConfinementOutcome.Denied)
                from.SendMessage("{0} cannot be jailed right now: {1}", toJail.Name, r.Reason);
            else if (r.Outcome == ConfinementOutcome.PartialFailure)
                from.SendMessage(0x22, "{0}: the jailing did not complete ({1}). See Confinement.log.", toJail.Name, r.Message);
        }

        // do jailing by staff: the runner's Staff jail profile. map: the facet whose jail they go to (each facet has its own
        //  Jail region); null = Felucca, as always. Account jailed flag, the jailer told, the command log, as before
        public static void DoJailing(Mobile jailer, Mobile toJail, int cell, string comment, int sentence, bool trouble, Map? map = null)
        {
            RunJailing(JailProfile.Staff, jailer, toJail, cell, comment, sentence, map ?? Map.Felucca);
        }

        /// <summary>
        /// The jail admission through the shared runner (JailAdmission). Every refusal is checked first and changes nothing;
        /// a refusal or a failed step is written to jail.log and the console, since the void callers cannot see the result.
        /// Never throws. Returns the runner's result (null: not a player character, nothing done).
        /// </summary>
        internal static AdmissionResult? RunJailing(JailProfile profile, Mobile reporter, Mobile toJail, int cell, string comment, int sentence, Map map)
        {
            if (toJail is not Server.Mobiles.PlayerMobile pm)
            {   // the jail record and the account comment have only ever applied to player characters
                ReportJailing(toJail, "Jail admission refused, nothing changed: not a player character");
                return null;
            }
            if (profile == JailProfile.System && ConfinementBookkeeping.IsConfined(pm))
            {   // first confinement wins: an automated jailing never moves someone already in a jail or a prison; the
                //  offense is recorded for staff (each automated jailing is a discrete event, so it is always recorded)
                ConfinementBookkeeping.RecordAdditionalOffense(pm, "system jailing", string.IsNullOrEmpty(comment) ? "None" : comment, dedupKeys: null);
                return new AdmissionResult
                {
                    Profile = "jail " + profile,
                    Denials = new[] { new ConfinementDenial(ConfinementDenialCode.OperationNotAllowed,
                        "They are already confined: the offense was recorded and they were not moved.") },
                };
            }
            AdmissionResult r = JailAdmission.Admit(new AdmissionContext(pm, reporter, map)
            {
                Comment = string.IsNullOrEmpty(comment) ? "None" : comment,
                JailCell = cell,
                JailHours = sentence,
            }, profile);
            if (!r.Completed)
                ReportJailing(pm, (r.Denied ? "Jail admission refused, nothing changed: " : "Jail admission NOT completed: ") + r);
            return r;
        }

        private static void ReportJailing(Mobile? m, string text)
        {
            Console.WriteLine("Jail: {0}: {1}", m, text);
            try
            {
                LogHelper logger = new LogHelper("jail.log", overwrite: false, sline: true);
                if (m != null && !m.Deleted)
                    logger.Log(LogType.Mobile, m, text);
                else
                    logger.Log(LogType.Text, text);
                logger.Finish();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        private static string FormatComment(int tagCount, string comment, int sentence, bool trouble)
        {
            // TODO: Simplify comment? There's no need to add the current date or the tag count. But then update GetDefaultSentence!
            return string.Format("{0}\nTag count: {1}\nJailed{2} for {3} hours. Reason: {4}",
                DateTime.UtcNow,
                tagCount,
                trouble ? "TroubleMaker" : "",
                sentence,
                comment);
        }
    }
}