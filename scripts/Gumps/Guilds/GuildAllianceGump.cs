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

/* Scripts/Gumps/Guilds/GuildAllianceGump.cs
 * ChangeLog:
 *	6/18/06, Pix
 *		Fixed one paging error, added colons to 'headers'.
 *	6/16/06, Pix
 *		Fixed the 'guild list scroll off gump' bug.
 *  12/14/05, Kit
 *		Initial creation
 */
using Server.Guilds;
using Server.Network;
using System.Collections;

namespace Server.Gumps
{
    public class GuildAllianceGump : Gump
    {
        private Mobile m_Mobile;
        private Guild m_Guild;

        public GuildAllianceGump(Mobile from, Guild guild)
            : base(20, 30)
        {
            m_Mobile = from;
            m_Guild = guild;

            Dragable = false;

            int iNextPageNumber = 0;

            AddPage(iNextPageNumber); //0
            iNextPageNumber++;

            AddBackground(0, 0, 550, 440, 5054);
            AddBackground(10, 10, 530, 420, 3000);

            AddHtml(20, 10, 500, 35, "<center>Alliance Status</center>", false, false);

            AddButton(20, 400, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(55, 400, 300, 35, 1011120, false, false); // Return to the main menu.

            AddPage(iNextPageNumber); //1
            iNextPageNumber++;

            AddButton(375, 375, 5224, 5224, 0, GumpButtonType.Page, iNextPageNumber);
            AddHtmlLocalized(410, 373, 100, 25, 1011066, false, false); // Next page

            AddHtml(20, 45, 400, 20, "We are allied with:", false, false);

            ArrayList Allies = guild.Allies;

            if (Allies.Count == 0)
            {
                AddHtml(20, 65, 400, 20, "No current alliances", false, false);
            }
            else
            {
                for (int i = 0; i < Allies.Count; ++i)
                {
                    Guild g = (Guild)Allies[i];

                    if (i != 0)
                    {
                        if (i % 15 == 0)
                        {
                            AddPage(iNextPageNumber);
                            iNextPageNumber++;

                            AddButton(375, 375, 5224, 5224, 0, GumpButtonType.Page, iNextPageNumber);
                            AddHtmlLocalized(410, 373, 100, 25, 1011066, false, false); // Next page

                            AddButton(30, 375, 5223, 5223, 0, GumpButtonType.Page, iNextPageNumber - 2);
                            AddHtmlLocalized(65, 373, 150, 25, 1011067, false, false); // Previous page

                            AddHtml(20, 45, 400, 20, "We are allied with:", false, false);
                        }
                    }

                    int y = 65 + ((i % 15) * 20);

                    AddHtml(20, y, 300, 20, g.Name, false, false);
                }
            }

            AddPage(iNextPageNumber); //2
            iNextPageNumber++;

            AddButton(375, 375, 5224, 5224, 0, GumpButtonType.Page, iNextPageNumber);
            AddHtmlLocalized(410, 373, 100, 25, 1011066, false, false); // Next page

            AddButton(30, 375, 5223, 5223, 0, GumpButtonType.Page, iNextPageNumber - 2);
            AddHtmlLocalized(65, 373, 150, 25, 1011067, false, false); // Previous page

            AddHtml(20, 45, 400, 20, "Guilds we are allied with:", false, false);

            ArrayList declared = guild.AllyDeclarations;

            if (declared.Count == 0)
            {
                AddHtml(20, 65, 400, 20, "No current invitations recieved for alliances", false, false);
            }
            else
            {
                for (int i = 0; i < declared.Count; ++i)
                {
                    Guild g = (Guild)declared[i];

                    if (i != 0)
                    {
                        if (i % 15 == 0)
                        {
                            AddPage(iNextPageNumber);
                            iNextPageNumber++;

                            AddButton(375, 375, 5224, 5224, 0, GumpButtonType.Page, iNextPageNumber);
                            AddHtmlLocalized(410, 373, 100, 25, 1011066, false, false); // Next page

                            AddButton(30, 375, 5223, 5223, 0, GumpButtonType.Page, iNextPageNumber - 2);
                            AddHtmlLocalized(65, 373, 150, 25, 1011067, false, false); // Previous page

                            AddHtml(20, 45, 400, 20, "Guilds we are allied with:", false, false);
                        }
                    }

                    int y = 65 + ((i % 15) * 20);
                    AddHtml(20, y, 300, 20, g.Name, false, false);
                }
            }

            AddPage(iNextPageNumber); //3
            iNextPageNumber++;

            AddButton(30, 375, 5223, 5223, 0, GumpButtonType.Page, iNextPageNumber - 2);
            AddHtmlLocalized(65, 373, 150, 25, 1011067, false, false); // Previous page

            AddHtml(20, 45, 400, 20, "Guilds that have allied with us:", false, false);

            ArrayList invites = guild.AllyInvitations;

            if (invites.Count > 15)
            {
                AddButton(375, 375, 5224, 5224, 0, GumpButtonType.Page, iNextPageNumber);
                AddHtmlLocalized(410, 373, 100, 25, 1011066, false, false); // Next page
            }

            if (invites.Count == 0)
            {
                AddHtml(20, 65, 400, 20, "No current alliance declarations", false, false);
            }
            else
            {
                for (int i = 0; i < invites.Count; ++i)
                {
                    Guild g = (Guild)invites[i];

                    if (i != 0)
                    {
                        if (i % 15 == 0)
                        {
                            AddPage(iNextPageNumber);
                            iNextPageNumber++;

                            if (invites.Count - i > 15)
                            {
                                AddButton(375, 375, 5224, 5224, 0, GumpButtonType.Page, iNextPageNumber);
                                AddHtmlLocalized(410, 373, 100, 25, 1011066, false, false); // Next page
                            }

                            AddButton(30, 375, 5223, 5223, 0, GumpButtonType.Page, iNextPageNumber - 2);
                            AddHtmlLocalized(65, 373, 150, 25, 1011067, false, false); // Previous page

                            AddHtml(20, 45, 400, 20, "Guilds that have allied with us:", false, false);
                        }
                    }

                    int y = 65 + ((i % 15) * 20);
                    AddHtml(20, y, 300, 20, g.Name, false, false);
                }
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (GuildGump.BadMember(m_Mobile, m_Guild))
                return;

            if (info.ButtonID == 1)
            {
                GuildGump.EnsureClosed(m_Mobile);
                m_Mobile.SendGump(new GuildGump(m_Mobile, m_Guild));
            }
        }
    }
}