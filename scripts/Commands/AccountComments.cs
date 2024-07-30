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

/* Commands/AccountComments.cs
 * CHANGELOG:
 *  9/09/21, Yoar
 *		First commit. View/Add/Remove account comments.
 */

using Server.Accounting;
using Server.Gumps;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.Commands
{
    public static class AccountComments
    {
        public const AccessLevel ReadAccess = AccessLevel.Counselor; // cannot edit this dynamically
        public static AccessLevel WriteAccess = AccessLevel.GameMaster;
        public static AccessLevel RemoveAccess = AccessLevel.Seer;
        public static int CommentsPerPage = 4;

        public static void Initialize()
        {
            CommandSystem.Register("AccountComments", ReadAccess, new CommandEventHandler(AccountComments_OnCommand));
            CommandSystem.Register("Comments", ReadAccess, new CommandEventHandler(AccountComments_OnCommand));
        }

        [Usage("AccountComments")]
        [Aliases("Comments")]
        [Description("View/Add/Remove account comments.")]
        private static void AccountComments_OnCommand(CommandEventArgs args)
        {
            if (args.Length == 0)
            {
                args.Mobile.SendMessage("Select the player to view account comments.");
                args.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(OnTarget));
            }
            else
            {
                if (args.Mobile.AccessLevel < AccessLevel.Administrator)
                {
                    args.Mobile.SendMessage("That is not accessible.");
                    return;
                }

                Account acct = Accounts.GetAccount(args.GetString(0));

                if (acct == null)
                    args.Mobile.SendMessage("That account does not exist.");
                else
                    args.Mobile.SendGump(new AccountCommentsGump(args.Mobile, null, acct));
            }
        }

        private static void OnTarget(Mobile from, object target)
        {
            Mobile m = target as Mobile;

            if (m == null || !m.Player)
            {
                from.SendMessage("You must target a player.");
                return;
            }
            else if (from.AccessLevel < WriteAccess && m.AccessLevel >= from.AccessLevel) // if we don't have write access, do an access level check
            {
                from.SendMessage("That is not accessible.");
                return;
            }

            if (m.Account == null)
                from.SendMessage("That player does not have an account.");
            else
                from.SendGump(new AccountCommentsGump(from, m, (Account)m.Account));
        }

        private class AccountCommentsGump : Gump
        {
            private enum Page
            {
                View,
                Add,
                Remove
            }
            private Mobile m_Target;
            private Account m_Account;
            private int m_Index;
            private bool m_FilterSystem;

            private Page m_Page;
            private AccountComment m_Selected;

            private int m_Height;

            private ArrayList m_List;

            public AccountCommentsGump(Mobile from, Mobile target, Account acct)
                : this(from, target, acct, 0, false, Page.View, null, null)
            {
            }

            private AccountCommentsGump(Mobile from, Mobile target, Account acct, int index, bool filterSystem, Page page, AccountComment selected, string notice)
                : base(120, 50)
            {
                m_Target = target;
                m_Account = acct;
                m_Index = index;
                m_FilterSystem = filterSystem;

                m_Page = page;
                m_Selected = selected;

                if (from.AccessLevel < ReadAccess)
                    return; // sanity

                m_Height = (m_Page == Page.Add ? 1 : CommentsPerPage) * 90;

                AddBackground(0, 0, 420, 155 + m_Height, 5054);

                AddImageTiled(10, 10, 400, 85 + m_Height, 2624);
                AddAlphaRegion(10, 10, 400, 85 + m_Height);

                AddImageTiled(10, 105 + m_Height, 400, 40, 2624);
                AddAlphaRegion(10, 105 + m_Height, 400, 40);

                string title;

                if (from.AccessLevel >= AccessLevel.GameMaster) // need GM access to see usernames
                    title = String.Format("Comments for {0}", m_Account.Username);
                else if (m_Target != null)
                    title = String.Format("Comments for Acct. of {0}", m_Target.Name);
                else
                    title = "Comments";

                AddHtml(10, 15, 400, 20, Color(Center(title)), false, false);

                switch (m_Page)
                {
                    case Page.View:
                        {
                            if (from.AccessLevel >= WriteAccess)
                            {
                                AddButton(20, 39, 4005, 4007, 1, GumpButtonType.Reply, 0);
                                AddHtml(55, 40, 90, 20, Color("Add Comment"), false, false);
                            }

                            if (from.AccessLevel >= RemoveAccess)
                            {
                                AddButton(145, 39, 4005, 4007, 2, GumpButtonType.Reply, 0);
                                AddHtml(180, 40, 90, 20, Color("Remove..."), false, false);
                            }

                            AddButton(275, 39, m_FilterSystem ? 4006 : 4005, 4007, 3, GumpButtonType.Reply, 0);
                            AddHtml(310, 40, 90, 20, Color("Filter System"), false, false);

                            AddComments(false, ref notice);

                            break;
                        }
                    case Page.Add:
                        {
                            if (from.AccessLevel < WriteAccess)
                                return; // sanity

                            AddButton(20, 39, 4005, 4007, 1, GumpButtonType.Reply, 0);
                            AddHtml(55, 40, 90, 20, Color("Submit"), false, false);

                            AddButton(275, 39, 4017, 4019, 0, GumpButtonType.Reply, 0);
                            AddHtml(310, 40, 90, 20, Color("Cancel"), false, false);

                            AddBackground(20, 64, 380, 100, 3000);
                            AddTextEntry(23, 67, 376, 94, 0, 0, "");

                            break;
                        }
                    case Page.Remove:
                        {
                            if (from.AccessLevel < RemoveAccess)
                                return; // sanity

                            AddButton(20, 39, 4005, 4007, 1, GumpButtonType.Reply, 0);
                            AddHtml(55, 40, 120, 20, Color("Remove Selected"), false, false);

                            AddButton(275, 39, 4017, 4019, 0, GumpButtonType.Reply, 0);
                            AddHtml(310, 40, 90, 20, Color("Cancel"), false, false);

                            if (notice == null)
                                notice = "Select the comment you wish to remove.";

                            AddComments(true, ref notice);

                            break;
                        }
                }

                if (notice != null)
                    AddHtml(12, 107 + m_Height, 396, 36, Color(notice), false, false);
            }

            private void AddComments(bool select, ref string notice)
            {
                m_List = new ArrayList(m_Account.Comments); // copy the list in case it gets modified

                int count = 0;
                int added = 0;

                if (m_List.Count == 0)
                {
                    if (notice == null)
                        notice = "There are no comments for this account.";
                }
                else
                {
                    for (int i = 0; i < m_List.Count; i++)
                    {
                        AccountComment c = (AccountComment)m_Account.Comments[i];

                        if (!m_FilterSystem || Array.IndexOf(m_SystemComments, c.AddedBy) == -1)
                        {
                            if (i >= m_Index && added < CommentsPerPage)
                            {
                                string html = String.Format("[Added By: {0} on {1}]<br>{2}", c.AddedBy, c.LastModified.ToString("H:mm M/d/yy"), c.Content);
                                AddHtml(20, 64 + (added * 90), 380, 80, html, true, true);

                                if (select)
                                    AddButton(0, 67 + (added * 90), m_Selected == c ? 211 : 210, m_Selected == c ? 210 : 211, 100 + i, GumpButtonType.Reply, 0);

                                added++;
                            }

                            count++;
                        }
                    }
                }

                int currentPage = m_Index / CommentsPerPage + 1;
                int totalPages = Math.Max(1, (count + CommentsPerPage - 1) / CommentsPerPage);

                AddHtml(145, 64 + m_Height, 130, 20, Color(Center(String.Format("Page {0} of {1}", currentPage, totalPages))), false, false);

                if (currentPage > 1)
                {
                    AddButton(20, 64 + m_Height, 4014, 4016, 4, GumpButtonType.Reply, 0);
                    AddHtml(55, 65 + m_Height, 90, 20, Color("Previous Page"), false, false);
                }

                if (currentPage < totalPages)
                {
                    AddButton(275, 64 + m_Height, 4005, 4007, 5, GumpButtonType.Reply, 0);
                    AddHtml(310, 65 + m_Height, 90, 20, Color("Next Page"), false, false);
                }
            }

            private static readonly string[] m_SystemComments = new string[]
                {
                    "System",
                    "SYSTEM",
                    "RTT SYSTEM",
                };

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                Mobile from = sender.Mobile;

                if (from.AccessLevel < ReadAccess)
                    return; // sanity

                switch (m_Page)
                {
                    case Page.View:
                        {
                            switch (info.ButtonID)
                            {
                                case 1: // add comment
                                    {
                                        if (from.AccessLevel < WriteAccess)
                                            return; // sanity

                                        from.SendGump(new AccountCommentsGump(from, m_Target, m_Account, m_Index, m_FilterSystem, Page.Add, null, "Enter the new comment."));
                                        break;
                                    }
                                case 2: // remove...
                                    {
                                        if (from.AccessLevel < RemoveAccess)
                                            return; // sanity

                                        from.SendGump(new AccountCommentsGump(from, m_Target, m_Account, m_Index, m_FilterSystem, Page.Remove, null, null));
                                        break;
                                    }
                                case 3: // filter system comments
                                    {
                                        bool filterSystem = !m_FilterSystem;

                                        from.SendGump(new AccountCommentsGump(from, m_Target, m_Account, m_Index, filterSystem, Page.View, null, filterSystem ? "Now filtering system comments." : "Now showing all comments."));
                                        break;
                                    }
                                case 4: // previous page
                                    {
                                        from.SendGump(new AccountCommentsGump(from, m_Target, m_Account, CommentsPerPage * (m_Index / CommentsPerPage - 1), m_FilterSystem, Page.View, null, null));
                                        break;
                                    }
                                case 5: // next page
                                    {
                                        from.SendGump(new AccountCommentsGump(from, m_Target, m_Account, CommentsPerPage * (m_Index / CommentsPerPage + 1), m_FilterSystem, Page.View, null, null));
                                        break;
                                    }
                            }

                            break;
                        }
                    case Page.Add:
                        {
                            if (from.AccessLevel < WriteAccess)
                                return; // sanity

                            switch (info.ButtonID)
                            {
                                case 0: // cancel
                                    {
                                        from.SendGump(new AccountCommentsGump(from, m_Target, m_Account, m_Index, m_FilterSystem, Page.View, null, "Canceled add."));
                                        break;
                                    }
                                case 1: // submit
                                    {
                                        string text = info.GetTextEntry(0).Text;

                                        if (String.IsNullOrEmpty(text))
                                        {
                                            from.SendGump(new AccountCommentsGump(from, m_Target, m_Account, m_Index, m_FilterSystem, Page.Add, null, "Text field is empty - enter the new comment."));
                                        }
                                        else
                                        {
                                            m_Account.Comments.Add(new AccountComment(from.Name, text));

                                            from.SendGump(new AccountCommentsGump(from, m_Target, m_Account, m_Index, m_FilterSystem, Page.View, null, "Comment added."));
                                        }

                                        break;
                                    }
                            }

                            break;
                        }
                    case Page.Remove:
                        {
                            if (from.AccessLevel < RemoveAccess)
                                return; // sanity

                            switch (info.ButtonID)
                            {
                                case 0: // cancel
                                    {
                                        from.SendGump(new AccountCommentsGump(from, m_Target, m_Account, m_Index, m_FilterSystem, Page.View, null, "Canceled remove."));
                                        break;
                                    }
                                case 1: // remove selected
                                    {
                                        int index = m_Selected == null ? -1 : m_Account.Comments.IndexOf(m_Selected);

                                        if (index == -1)
                                        {
                                            from.SendGump(new AccountCommentsGump(from, m_Target, m_Account, m_Index, m_FilterSystem, Page.Remove, null, "No comment was selected - select the comment you wish to remove."));
                                        }
                                        else
                                        {
                                            m_Account.Comments.RemoveAt(index);

                                            from.SendGump(new AccountCommentsGump(from, m_Target, m_Account, m_Index, m_FilterSystem, Page.View, null, "Comment removed."));
                                        }

                                        break;
                                    }
                                default:
                                    {
                                        if (m_List != null && info.ButtonID >= 100 && info.ButtonID < 100 + m_List.Count)
                                        {
                                            AccountComment selected = (AccountComment)m_List[info.ButtonID - 100];

                                            from.SendGump(new AccountCommentsGump(from, m_Target, m_Account, m_Index, m_FilterSystem, Page.Remove, selected, null));
                                        }

                                        break;
                                    }
                            }

                            break;
                        }
                }
            }
        }
    }
}