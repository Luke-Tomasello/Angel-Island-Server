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

/* ChangeLog:
 *  10/14/06, Rhiannon
 *		File created
 */


using Server.Guilds;

namespace Server.Gumps
{
    public class ConfirmResignGump : Gump
    {
        private Mobile m_From;

        public ConfirmResignGump(Mobile from)
            : base(50, 50)
        {
            m_From = from;

            m_From.CloseGump(typeof(ConfirmResignGump));

            AddPage(0);

            AddBackground(0, 0, 215, 110, 5054);
            AddBackground(10, 10, 195, 90, 3000);

            AddHtml(20, 15, 175, 50, "Are you sure you wish to resign from your guild?", true, false); // Are you sure you want to resign from your guild?

            AddButton(20, 70, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddHtml(55, 70, 75, 20, "Yes", false, false);

            AddButton(135, 70, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtml(170, 70, 75, 20, "No", false, false);
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 2)
            {
                if (m_From.Guild != null)
                    ((Guild)m_From.Guild).RemoveMember(m_From);
            }
        }
    }
}