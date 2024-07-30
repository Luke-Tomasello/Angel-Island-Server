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

/* Scripts/Gumps/Guilds/GuildMobileListServerPagedGump.cs
 * CHANGELOG:
 *	1/23/22, Pix
 *		Initial version. Replaces GuildMobileListGump where needed.
 */

using Server.Diagnostics;
using Server.Guilds;
using Server.Network;
using System;
using System.Collections;

namespace Server.Gumps
{
    public abstract class GuildMobileListServerPagedGump : Gump
    {
        protected Mobile m_Mobile;
        protected Guild m_Guild;
        protected int m_PageNumber;
        protected ArrayList m_List;

        protected const int PREV_PAGE_BUTTON = 101;
        protected const int NEXT_PAGE_BUTTON = 102;

        public GuildMobileListServerPagedGump(Mobile from, Guild guild, bool radio, ArrayList list, int pageNumber)
            : base(20, 30)
        {
            m_Mobile = from;
            m_Guild = guild;
            m_PageNumber = pageNumber;

            Dragable = false;

            AddPage(0);
            AddBackground(0, 0, 550, 440, 5054);
            AddBackground(10, 10, 530, 420, 3000);

            Design();

            m_List = new ArrayList(list);

            int start = pageNumber * 11;
            int end = start + 11;
            if (end > m_List.Count)
            {
                end = m_List.Count;
            }


            if (end < m_List.Count)
            {
                AddButton(300, 370, 4005, 4007, NEXT_PAGE_BUTTON, GumpButtonType.Reply, 0);
                AddHtmlLocalized(335, 370, 300, 35, 1011066, false, false); // Next page
            }
            if (pageNumber > 0)
            {
                AddButton(20, 370, 4014, 4016, PREV_PAGE_BUTTON, GumpButtonType.Reply, 0);
                AddHtmlLocalized(55, 370, 300, 35, 1011067, false, false); // Previous page
            }


            for (int i = start; i < end; i++)
            {
                if (radio)
                {
                    AddRadio(20, 35 + ((i % 11) * 30), 208, 209, false, i);
                }

                Mobile m = (Mobile)m_List[i];

                string name;

                if ((name = m.Name) != null && (name = name.Trim()).Length <= 0)
                    name = "(empty)";

                string title = "(no title)";
                try
                {
                    if (m.GuildTitle != null && m.GuildTitle.Trim().Length > 0)
                    {
                        title = m.GuildTitle.Trim();
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    Console.WriteLine("Send the following exception to Pixie:\n{0}\n{1}\n{2}\ntitle={3}",
                        "Exception in GuildMobileListGump",
                        ex.Message,
                        ex.StackTrace.ToString(),
                        m.GuildTitle);

                    title = "(error)";
                }

                AddLabel((radio ? 55 : 20), 35 + ((i % 11) * 30), 0, name + ", " + title);
                #region Last member Warning
                if (GuildLastMemberWarningGump.CheckTownship(from) && GuildLastMemberWarningGump.CheckLastMember(from))
                {
                    int startX = 60;
                    int startY = 75;
                    AddBackground(startX, startY + 0, 420, 280, 5054);

                    AddImageTiled(startX + 10, startY + 10, 400, 20, 2624);
                    AddAlphaRegion(startX + 10, startY + 10, 400, 20);

                    AddHtmlLocalized(startX + 10, startY + 10, 400, 20, 1060635, 30720, false, false); // <CENTER>WARNING</CENTER>

                    AddImageTiled(startX + 10, startY + 40, 400, 200, 2624);
                    AddAlphaRegion(startX + 10, startY + 40, 400, 200);

                    // The following warning is more appropriate for AI than the localized warning.
                    String WarningString =
                        "You are about to remove the last member in the guild but a township exists! " +
                        "Disbanding the guild will result in the township and all township items being deleted. " +
                        //"All items in the house will remain behind and can be freely picked up by anyone. " +
                        //"Once the house is demolished, anyone can attempt to place a new house on the vacant land. " +
                        "Are you sure you wish to continue?";

                    AddHtml(startX + 10, startY + 40, 400, 200, WarningString, false, true);

                    AddImageTiled(startX + 10, startY + 250, 400, 20, 2624);
                    AddAlphaRegion(startX + 10, startY + 250, 400, 20);
                }
                #endregion Last Member Warning
            }
        }

        public sealed override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID == PREV_PAGE_BUTTON)
            {
                SendPage(m_Mobile, m_Guild, m_PageNumber - 1);
            }
            else if (info.ButtonID == NEXT_PAGE_BUTTON)
            {
                SendPage(m_Mobile, m_Guild, m_PageNumber + 1);
            }
            else
            {
                HandleDesignedReponse(state, info);
            }
        }

        protected virtual void HandleDesignedReponse(NetState state, RelayInfo info)
        {
        }

        protected virtual void SendPage(Mobile from, Guild guild, int newpage)
        {
        }

        protected virtual void Design()
        {
        }
    }
}