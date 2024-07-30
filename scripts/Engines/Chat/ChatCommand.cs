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
    public enum ChatCommand
    {
        /// <summary>
        /// Add a channel to top list.
        /// </summary>
        AddChannel = 0x3E8,
        /// <summary>
        /// Remove channel from top list.
        /// </summary>
        RemoveChannel = 0x3E9,
        /// <summary>
        /// Queries for a new chat nickname.
        /// </summary>
        AskNewNickname = 0x3EB,
        /// <summary>
        /// Closes the chat window.
        /// </summary>
        CloseChatWindow = 0x3EC,
        /// <summary>
        /// Opens the chat window.
        /// </summary>
        OpenChatWindow = 0x3ED,
        /// <summary>
        /// Add a user to current channel.
        /// </summary>
        AddUserToChannel = 0x3EE,
        /// <summary>
        /// Remove a user from current channel.
        /// </summary>
        RemoveUserFromChannel = 0x3EF,
        /// <summary>
        /// Send a message putting generic conference name at top when player leaves a channel.
        /// </summary>
        LeaveChannel = 0x3F0,
        /// <summary>
        /// Send a message putting Channel name at top and telling player he joined the channel.
        /// </summary>
        JoinedChannel = 0x3F1,
        /// <summary>
        /// Send a message putting Channel name at top and telling player he left the channel.
        /// </summary>
        LeftChannel = 0x3F4,
    }
}