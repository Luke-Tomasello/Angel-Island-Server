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

namespace Server.Engines.Chat
{
    public delegate void OnChatAction(ChatUser from, Channel channel, string param);

    public class ChatActionHandler
    {
        private bool m_RequireModerator;
        private bool m_RequireConference;
        private OnChatAction m_Callback;

        public bool RequireModerator { get { return m_RequireModerator; } }
        public bool RequireConference { get { return m_RequireConference; } }
        public OnChatAction Callback { get { return m_Callback; } }

        public ChatActionHandler(bool requireModerator, bool requireConference, OnChatAction callback)
        {
            m_RequireModerator = requireModerator;
            m_RequireConference = requireConference;
            m_Callback = callback;
        }
    }
}