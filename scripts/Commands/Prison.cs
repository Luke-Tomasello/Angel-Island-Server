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

/* Scripts/Commands/Prison.cs
 * CHANGELOG
 *	3/12/10, Adam
 *		Clone from Jail command.
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;
namespace Server.Commands
{
    public class Prison
    {
        public Prison()
        {
        }

        public static void Initialize()
        {
            Server.CommandSystem.Register("Prison", AccessLevel.Counselor, new CommandEventHandler(Prison_OnCommand));
        }

        // warden's office
        public static Point3D Location { get { return new Point3D(354, 836, 20); } }

        public static void Prison_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the player to imprison.");
            e.Mobile.Target = new PrisonTarget(Location, e.ArgString);
        }

        public static void Usage(Mobile to)
        {
            to.SendMessage("Usage: [Prison [\"Tag Message\"]");
        }

        public class PrisonPlayer
        {
            private Point3D m_Location;
            private string m_Comment;
            private PlayerMobile m_Player;
            private PlayerMobile m_Staff;
            private uint m_Sentence;

            public PrisonPlayer(PlayerMobile from, PlayerMobile pm, Point3D location, string comment, uint sentence = 3)
            {
                m_Location = location;
                m_Comment = comment;
                if (m_Comment == null || m_Comment == "")
                    m_Comment = "None";
                m_Player = pm;
                m_Staff = from != null ? from : (PlayerMobile)World.GetSystemAcct();
                m_Sentence = sentence;
            }

            public void GoToPrison()
            {
                try
                {
                    if (m_Player == null || m_Player.Deleted)
                    {
                        return;
                    }

                    Account acct = m_Player.Account as Account;

                    // stable the players pets
                    Utility.StablePets(m_Player);

                    #region Disable Global Chat
                    Server.Engines.Chat.ChatHelper.SetChatBan(m_Player, true);
                    #endregion Disable Global Chat

                    #region Before Prison Log
                    LogHelper logger = new LogHelper("Prison.log", false, false);
                    logger.Log(LogType.Mobile, m_Player, string.Format("Comment: {0}", m_Comment));
                    m_Player.LogPrisonerInfo(logger);
                    logger.Finish();
                    #endregion Before Prison Log

                    m_Player.ShortTermCriminalCounts += m_Sentence;             // how long you will stay
                    if (m_Sentence > 0)
                        m_Player.LongTermCriminalCounts++;                      // how many times you've been to prison for criminal behavior

                    Item aiEntrance = new AIEntrance(checkMurders: false, context: AIEntrance.Context.PrisonCommand);
                    aiEntrance.OnMoveOver(m_Player);
                    aiEntrance.Delete();

                    // if a hacker is just creating account after account, they will die here.
                    if (DeathSentence(m_Player))
                    {   // rotting corpse death trap
                        m_Player.MoveToWorld(new Point3D(354, 834, 0), m_Player.Map);
                    }

                    #region Notify Player/Staff
                    int sentence = 0;
                    if (m_Player.ShortTermCriminalCounts * 4 > 0)
                        sentence = (int)m_Player.ShortTermCriminalCounts * 4; // decay time in prison is 4 hours per count
                    else if ((int)(m_Player.MinimumSentence - DateTime.UtcNow).TotalHours > 0)
                        sentence = (int)(m_Player.MinimumSentence - DateTime.UtcNow).TotalHours;
                    if (sentence > 0)
                    {
                        m_Player.SendMessage("You have been imprisoned for {0} hours.", sentence);
                        m_Staff.SendMessage("{0} has been imprisoned for {1} hours.", m_Player.Name, sentence);
                    }
                    else
                    {
                        m_Player.SendMessage("You have been imprisoned for an unspecified amount of time.");
                        m_Staff.SendMessage("{0} has been imprisoned for an unspecified amount of time.", m_Player.Name);
                    }
                    #endregion Player/Staff

                    #region After Prison Log
                    logger = new LogHelper("Prison.log", false, false);
                    logger.Log(LogType.Mobile, m_Player, string.Format("{0}:{1}:{2}:{3}",
                        m_Staff.Name,
                        m_Staff.Location,
                        m_Comment,
                        sentence == 0 ? "unspecified amount of time" : sentence.ToString()));
                    m_Player.LogPrisonerInfo(logger);
                    logger.Finish();
                    #endregion After Prison Log

                    #region Notify Sender
                    Commands.CommandLogging.WriteLine(m_Staff,
                        "{0} imprisoned {1}(Username: {2}) for {4} hours with reason: {3}.",
                        m_Staff.Name, m_Player.Name, acct.Username,
                        m_Comment, sentence == 0 ? "unspecified" : sentence.ToString());
                    #endregion Notify Sender

                    #region Tag Account
                    acct.Comments.Add(new AccountComment(m_Staff.Name,
                        DateTime.UtcNow + "\nTag count: " + (acct.Comments.Count + 1) +
                        "\nImprisoned for " + (sentence == 0 ? "unspecified" : sentence.ToString()) + " hours. Reason: " +
                        m_Comment));
                    #endregion Tag Account
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
            }
            private bool DeathSentence(PlayerMobile pm)
            {   // anyone just creating account after account will find a surprise waiting for them
                return AcctNameList(pm).Count >= 8;
            }
            private List<string> AcctNameList(PlayerMobile pm)
            {
                Accounting.Account acct = pm.Account as Accounting.Account;
                // get a list of accounts associated with this machine ascending sorted by date created that match the acct in question
                List<Account> list_by_machine = Misc.AccountHardwareLimiter.GetAccountsByMachine(acct);

                List<string> nameList = new();
                foreach (Account account in list_by_machine)
                    nameList.Add(account.Username);

                NetState ns = pm.NetState;
                if (ns != null)
                {
                    List<Accounting.Account> list_by_ip = Misc.AccountTotalIPLimiter.GetAccountsByIP(acct, ns.Address);

                    foreach (Account account in list_by_ip)
                        if (!nameList.Contains(account.Username))
                            nameList.Add(account.Username);
                }

                return nameList;
            }

        }

        private class PrisonTarget : Target
        {
            private Point3D m_Location;
            private string m_Comment;

            public PrisonTarget(Point3D location, string comment)
                : base(15, false, TargetFlags.None)
            {
                m_Location = location;
                m_Comment = comment;
                if (m_Comment == null || m_Comment == "")
                    m_Comment = "None";
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                PlayerMobile pm = targ as PlayerMobile;
                if (pm == null)
                {
                    from.SendMessage("Only players can be sent to Prison.");
                    return;
                }

                PrisonPlayer prison = new PrisonPlayer(from as PlayerMobile, pm, m_Location, m_Comment);
                prison.GoToPrison();
            }
        }
    }
}