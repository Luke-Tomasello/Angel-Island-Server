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

/* Scripts/Gumps/Guilds/GuildAcceptAllianceGump.cs
 * ChangeLog:
 *  01/14/08, Pix   
 *      Fixed guild alliances - now non-kin can ally with kin.
 *	12/4/07, Adam
 *		Add support for peaceful guilds (no notoriety)
 *	4/28/06, Pix
 *		Changes for Kin alignment by guild.
 *  12/14/05, Kit
 *		Initial creation
 */

using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildAcceptAllianceGump : GuildListGump
    {
        public GuildAcceptAllianceGump(Mobile from, Guild guild)
            : base(from, guild, true, guild.AllyInvitations)
        {
        }

        protected override void Design()
        {
            AddHtmlLocalized(20, 10, 400, 35, 1011147, false, false); // Select the guild to accept the invitations: 

            AddButton(20, 400, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtml(55, 400, 245, 30, "Accept alliance invitations", false, false);  // Accept alliance invitations.

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
                            if (g.IOBAlignment != IOBAlignment.None &&
                                m_Guild.IOBAlignment != IOBAlignment.None &&
                                g.IOBAlignment != m_Guild.IOBAlignment)
                            {
                                //If we're both not aligned, and aligned to different kin, then we can't ally.
                                m_Mobile.SendMessage("You cannot ally with different kin.");
                                m_Guild.AllyInvitations.Remove(g);
                                g.AllyDeclarations.Remove(m_Guild);
                            }
                            else if (m_Guild.Peaceful == true)
                            {
                                m_Mobile.SendMessage("You belong to a peaceful guild and may not do that.");
                                m_Guild.AllyInvitations.Remove(g);
                                g.AllyDeclarations.Remove(m_Guild);
                            }
                            else
                            {
                                m_Guild.AllyInvitations.Remove(g);
                                g.AllyDeclarations.Remove(m_Guild);

                                m_Guild.AddAlly(g);
                                string s = string.Format("Your guild is now allied, {0} {1}", g.Name, g.Abbreviation);
                                m_Guild.GuildMessage(s);

                                GuildGump.EnsureClosed(m_Mobile);
                            }

                            if (m_Guild.AllyInvitations.Count > 0)
                                m_Mobile.SendGump(new GuildAcceptAllianceGump(m_Mobile, m_Guild));
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