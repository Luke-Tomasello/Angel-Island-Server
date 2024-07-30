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

/* Scripts/Gumps/Guilds/GuildBreakAllianceGump.cs
 * ChangeLog:
 *	7/23/06, Pix
 *		Cleaned up.
 *  12/14/05, Kit
 *		Initial creation
 */
using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildBreakAllianceGump : GuildListGump
    {
        public GuildBreakAllianceGump(Mobile from, Guild guild)
            : base(from, guild, true, guild.Allies)
        {
        }

        protected override void Design()
        {
            AddHtml(20, 10, 400, 35, "Select the guild you wish to break your alliance with", false, false);

            AddButton(20, 400, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtml(55, 400, 245, 30, "Break Alliance", false, false);

            AddButton(300, 400, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddHtmlLocalized(335, 400, 100, 35, 1011012, false, false); // CANCEL
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
                return;

            if (info.ButtonID == 1)
            {
                int[] switches = info.Switches;

                if (switches.Length > 0)
                {
                    int index = switches[0];

                    if (index >= 0 && index < m_List.Count)
                    {
                        Guild g = (Guild)m_List[index];

                        if (g != null)
                        {
                            m_Guild.RemoveAlly(g);
                            string s = string.Format("You break your alliance with, {0} {1}", g.Name, g.Abbreviation);
                            m_Guild.GuildMessage(s);

                            GuildGump.EnsureClosed(m_Mobile);

                            if (m_Guild.Allies.Count > 0)
                                m_Mobile.SendGump(new GuildBreakAllianceGump(m_Mobile, m_Guild));
                            else
                                m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
                        }
                    }
                }
            }
            else if (info.ButtonID == 2)
            {
                GuildGump.EnsureClosed(m_Mobile);
                m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
            }
        }
    }
}