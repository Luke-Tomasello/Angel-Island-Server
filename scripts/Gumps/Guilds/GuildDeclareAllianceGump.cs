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

/* Scripts/Gumps/Guilds/GuildDeclareAllianceGump.cs
 * ChangeLog:
 *	12/4/07, Adam
 *		Add support for peaceful guilds (no notoriety)
 *	11/18/06, Pix
 *		Fixed it so a non-kin guild can ally with a kin guild
 *	4/28/06, Pix
 *		Changes for Kin alignment by guild.
 *  12/14/05, Kit
 *		Initial creation
 */

using Server.Guilds;
using Server.Network;
using System.Collections;

namespace Server.Gumps
{
    public class GuildDeclareAllianceGump : GuildListGump
    {
        public GuildDeclareAllianceGump(Mobile from, Guild guild, ArrayList list)
            : base(from, guild, true, list)
        {
        }

        protected override void Design()
        {
            AddHtml(20, 10, 400, 35, "Select the guild you wish to ally with", false, false);

            AddButton(20, 400, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtml(55, 400, 245, 30, "Send the treaty", false, false);

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
                            if (g == m_Guild)
                            {
                                m_Mobile.SendMessage("You cannot declare an alliance with yourself"); // You cannot declare war against yourself!
                            }
                            else if (m_Guild.Peaceful == true)
                            {
                                m_Mobile.SendMessage("You belong to a peaceful guild and may not do this.");
                            }
                            else if ((g.AllyInvitations.Contains(m_Guild) && m_Guild.AllyDeclarations.Contains(g)) || m_Guild.IsAlly(g))
                            {
                                m_Mobile.SendMessage("You are already allied with that guild."); // You are already allyed with that guild.
                            }
                            else if (g.IOBAlignment != IOBAlignment.None &&
                                m_Guild.IOBAlignment != IOBAlignment.None &&
                                g.IOBAlignment != m_Guild.IOBAlignment)
                            {
                                m_Mobile.SendMessage("You cannot ally with different kin.");
                            }
                            else if ((g.WarInvitations.Contains(m_Guild) && m_Guild.WarDeclarations.Contains(g)) || m_Guild.IsEnemy(g, true))
                            {
                                m_Mobile.SendMessage("You cannot ally with a guild you are at war with!"); // cant allie with a guild we are at war with
                            }
                            else
                            {
                                if (!m_Guild.AllyDeclarations.Contains(g))
                                {
                                    m_Guild.AllyDeclarations.Add(g);
                                    string s = string.Format("Your guild has sent an alliance invitation, {0} {1}", g.Name, g.Abbreviation);
                                    m_Guild.GuildMessage(s);

                                }

                                if (!g.AllyInvitations.Contains(m_Guild))
                                {
                                    g.AllyInvitations.Add(m_Guild);
                                    string s = string.Format("Your guild has recieved an invitation to ally, {0} {1}", m_Guild.Name, m_Guild.Abbreviation);
                                    g.GuildMessage(s);
                                }
                            }

                            GuildGump.EnsureClosed(m_Mobile);
                            m_Mobile.SendGump(new GuildAllianceAdminGump(m_Mobile, m_Guild));
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