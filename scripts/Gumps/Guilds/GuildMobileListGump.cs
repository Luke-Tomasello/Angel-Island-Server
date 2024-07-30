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

/* Scripts/Gumps/Guilds/GuildMobileListGump.cs
 * CHANGELOG:
 *	6/16/06, Pix
 *		Added guildmember's title to listing.
 */

using Server.Diagnostics;
using Server.Guilds;
using System;
using System.Collections;

namespace Server.Gumps
{

    /*
     * NOTE: The client has problems with this class when sending list with large number AND radio button
     *   Use GuildMobileListPagedGump as an alternative. The difference is this class sends the entire list
     *   and has the client handle paging, whereas GuildMobileListPagedGump sends only one page at a time.
     */
    public abstract class GuildMobileListGump : Gump
    {
        protected Mobile m_Mobile;
        protected Guild m_Guild;
        protected ArrayList m_List;

        public GuildMobileListGump(Mobile from, Guild guild, bool radio, ArrayList list)
            : base(20, 30)
        {
            m_Mobile = from;
            m_Guild = guild;

            Dragable = false;

            AddPage(0);
            AddBackground(0, 0, 550, 440, 5054);
            AddBackground(10, 10, 530, 420, 3000);

            Design();

            m_List = new ArrayList(list);

            for (int i = 0; i < m_List.Count; ++i)
            {
                if ((i % 11) == 0)
                {
                    if (i != 0)
                    {
                        AddButton(300, 370, 4005, 4007, 0, GumpButtonType.Page, (i / 11) + 1);
                        AddHtmlLocalized(335, 370, 300, 35, 1011066, false, false); // Next page
                    }

                    AddPage((i / 11) + 1);

                    if (i != 0)
                    {
                        AddButton(20, 370, 4014, 4016, 0, GumpButtonType.Page, (i / 11));
                        AddHtmlLocalized(55, 370, 300, 35, 1011067, false, false); // Previous page
                    }
                }

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
            }

            return;
        }

        protected virtual void Design()
        {
        }
    }
}