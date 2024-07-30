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

/* Scripts/Gumps/Guilds/GuildAllianceAdminGump.cs
 * ChangeLog:
 *	7/23/06, Pix
 *		Fixed leftover message from wargump copy.
 *	4/28/06, Pix
 *		Changes for Kin alignment by guild.
 *  12/14/05, Kit
 *		Initial creation
 */
using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildAllianceAdminGump : Gump
    {
        private Mobile m_Mobile;
        private Guild m_Guild;

        public GuildAllianceAdminGump(Mobile from, Guild guild)
            : base(20, 30)
        {
            m_Mobile = from;
            m_Guild = guild;

            Dragable = false;

            AddPage(0);
            AddBackground(0, 0, 550, 440, 5054);
            AddBackground(10, 10, 530, 420, 3000);

            AddHtml(20, 10, 510, 35, "<center>ALLY FUNCTIONS</center>", false, false); // <center>WAR FUNCTIONS</center>

            AddButton(20, 40, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtml(55, 40, 400, 30, "Declare an alliance through guild name search", false, false); // Declare war through guild name search.

            int count = 0;

            if (guild.Allies.Count > 0)
            {
                AddButton(20, 160 + (count * 30), 4005, 4007, 2, GumpButtonType.Reply, 0);
                AddHtml(55, 160 + (count++ * 30), 400, 30, "Break alliance", false, false); // Declare peace.
            }
            else
            {
                AddHtml(20, 160 + (count++ * 30), 400, 30, "No current allies", false, false); // No current wars
            }

            if (guild.AllyInvitations.Count > 0)
            {
                AddButton(20, 160 + (count * 30), 4005, 4007, 3, GumpButtonType.Reply, 0);
                AddHtml(55, 160 + (count++ * 30), 400, 30, "Accept ally invitations", false, false); // Accept ally invitations.

                AddButton(20, 160 + (count * 30), 4005, 4007, 4, GumpButtonType.Reply, 0);
                AddHtml(55, 160 + (count++ * 30), 400, 30, "Reject ally invitations", false, false); // Reject ally invitations.
            }
            else
            {
                AddHtml(20, 160 + (count++ * 30), 400, 30, "No current alliance invitations", false, false); // No current invitations received for ally.
            }

            if (guild.AllyDeclarations.Count > 0)
            {
                AddButton(20, 160 + (count * 30), 4005, 4007, 5, GumpButtonType.Reply, 0);
                AddHtml(55, 160 + (count++ * 30), 400, 30, "Rescind your alliance declarations", false, false); // Rescind your ally declarations.
            }
            else
            {
                AddHtml(20, 160 + (count++ * 30), 400, 30, "No current alliance declarations", false, false); // No current ally declarations
            }

            AddButton(20, 400, 4005, 4007, 6, GumpButtonType.Reply, 0);
            AddHtmlLocalized(55, 400, 400, 35, 1011104, false, false); // Return to the previous menu.
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
                return;

            switch (info.ButtonID)
            {
                case 1: // Declare alliance
                    {
                        m_Mobile.SendMessage("Declare alliance through search - Enter Guild Name:");
                        m_Mobile.Prompt = new GuildDeclareAlliancePrompt(m_Mobile, m_Guild);

                        break;
                    }
                case 2: // Break alliance
                    {
                        GuildGump.EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildBreakAllianceGump(m_Mobile, m_Guild));

                        break;
                    }
                case 3: // Accept alliance
                    {
                        GuildGump.EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildAcceptAllianceGump(m_Mobile, m_Guild));

                        break;
                    }
                case 4: // Reject alliance
                    {
                        GuildGump.EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildRejectAllianceGump(m_Mobile, m_Guild));

                        break;
                    }
                case 5: // Rescind declarations
                    {
                        GuildGump.EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildRescindAllianceGump(m_Mobile, m_Guild));

                        break;
                    }
                case 6: // Return
                    {
                        GuildGump.EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));

                        break;
                    }
            }
        }
    }
}