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

using Server.Prompts;

namespace Server.Engines.Help
{
    public class PagePrompt : Prompt
    {
        private PageType m_Type;

        public PagePrompt(PageType type)
        {
            m_Type = type;
        }

        public override void OnCancel(Mobile from)
        {
            from.SendLocalizedMessage(501235, "", 0x35); // Help request aborted.
        }

        public override void OnResponse(Mobile from, string text)
        {
            from.SendLocalizedMessage(501234, "", 0x35); /* The next available Counselor/Game Master will respond as soon as possible.
															* Please check your Journal for messages every few minutes.
															*/

            PageQueue.Enqueue(new PageEntry(from, text, m_Type));
        }
    }
}