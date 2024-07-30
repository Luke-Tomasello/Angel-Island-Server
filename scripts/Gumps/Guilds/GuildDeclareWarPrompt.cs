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

/* Scripts/Gumps/Guilds/GuildDeclareWarPrompt.cs
 * Changelog:
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Guilds;
using Server.Prompts;
using System.Collections;

namespace Server.Gumps
{
    public class GuildDeclareWarPrompt : Prompt
    {
        private Mobile m_Mobile;
        private Guild m_Guild;

        public GuildDeclareWarPrompt(Mobile m, Guild g)
        {
            m_Mobile = m;
            m_Guild = g;
        }

        public override void OnCancel(Mobile from)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
                return;

            GuildGump.EnsureClosed(m_Mobile);
            m_Mobile.SendGump(new GuildWarAdminGump(m_Mobile, m_Guild));
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
                return;

            text = text.Trim();

            if (text.Length >= 3)
            {
                BaseGuild[] guilds = Guild.Search(text);

                GuildGump.EnsureClosed(m_Mobile);

                if (guilds.Length > 0)
                {
                    m_Mobile.SendGump(new GuildDeclareWarGump(m_Mobile, m_Guild, new ArrayList(guilds)));
                }
                else
                {
                    m_Mobile.SendGump(new GuildWarAdminGump(m_Mobile, m_Guild));
                    m_Mobile.SendLocalizedMessage(1018003); // No guilds found matching - try another name in the search
                }
            }
            else
            {
                m_Mobile.SendMessage("Search string must be at least three letters in length.");
            }
        }
    }
}