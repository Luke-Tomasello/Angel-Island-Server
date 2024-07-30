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

using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildCandidatesGump : GuildMobileListGump
    {
        public GuildCandidatesGump(Mobile from, Guild guild)
            : base(from, guild, false, guild.Candidates)
        {
        }

        protected override void Design()
        {
            AddHtmlLocalized(20, 10, 500, 35, 1013030, false, false); // <center> Candidates </center>

            AddButton(20, 400, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(55, 400, 300, 35, 1011120, false, false); // Return to the main menu.
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