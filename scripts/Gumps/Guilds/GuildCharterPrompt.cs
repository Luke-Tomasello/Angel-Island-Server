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
using Server.Prompts;

namespace Server.Gumps
{
    public class GuildCharterPrompt : Prompt
    {
        private Mobile m_Mobile;
        private Guild m_Guild;

        public GuildCharterPrompt(Mobile m, Guild g)
        {
            m_Mobile = m;
            m_Guild = g;
        }

        public override void OnCancel(Mobile from)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
                return;

            GuildGump.EnsureClosed(m_Mobile);
            m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
                return;

            text = text.Trim();

            if (text.Length > 50)
                text = text.Substring(0, 50);

            if (text.Length > 0)
                m_Guild.Charter = text;

            m_Mobile.SendLocalizedMessage(1013072); // Enter the new website for the guild (50 characters max):
            m_Mobile.Prompt = new GuildWebsitePrompt(m_Mobile, m_Guild);

            GuildGump.EnsureClosed(m_Mobile);
            m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
        }
    }
}