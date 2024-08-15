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

/* Scripts/Engines/Chat/ChatHelper.cs
 * ChangeLog:
 *  5/1/23, Yoar
 *      Added ChatBan account tag
 *	4/23/23, Yoar
 *	    Initial version.
 *	    
 *	    Enables GMs to enable/disable the chat system.
 *	    Enables players to enter/leave global chat.
 *	    Automatically adds players to global chat.
 */

using Server.Accounting;
using System;

namespace Server.Engines.Chat
{
    public static class ChatHelper
    {
        public static void Initialize()
        {
            CommandSystem.Register("SetChat", AccessLevel.GameMaster, new CommandEventHandler(SetChat_OnCommand));

            CommandSystem.Register("ToggleChat", AccessLevel.Player, new CommandEventHandler(ToggleChat_OnCommand));
            CommandSystem.Register("Chat", AccessLevel.Player, new CommandEventHandler(ToggleChat_OnCommand));

            EventSink.Connected += new ConnectedEventHandler(EventSink_OnConnected);
            EventSink.Disconnected += new DisconnectedEventHandler(EventSink_OnDisconnected);
        }

        [Usage("SetChat [<true|false>]")]
        [Description("Toggles on/off chat.")]
        private static void SetChat_OnCommand(CommandEventArgs e)
        {
            bool enabled;

            if (e.Length == 0)
                enabled = !ChatSystem.Enabled;
            else
                enabled = e.GetBoolean(0);

            if (ChatSystem.Enabled != enabled)
            {
                if (enabled)
                {
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.ChatEnabled);

                    e.Mobile.SendMessage("The chat system has been enabled.");
                }
                else
                {
                    for (int i = ChatUser.Users.Count - 1; i >= 0; i--)
                    {
                        if (i < ChatUser.Users.Count)
                            ChatUser.RemoveChatUser((ChatUser)ChatUser.Users[i]);
                    }

                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.ChatEnabled);

                    e.Mobile.SendMessage("The chat system has been disabled.");
                }
            }
        }

        [Usage("ToggleChat")]
        [Aliases("Chat")]
        [Description("Enter/leave global chat.")]
        private static void ToggleChat_OnCommand(CommandEventArgs e)
        {
            if (!ChatSystem.Enabled || GetChatBan(e.Mobile))
                return;

            bool enabled = (ChatUser.GetChatUser(e.Mobile) == null);

            if (enabled)
                AddUser(e.Mobile);
            else
                RemoveUser(e.Mobile);

            SetChatOff(e.Mobile, !enabled);
        }

        private static void EventSink_OnConnected(ConnectedEventArgs e)
        {
            if (ChatSystem.Enabled && !GetChatOff(e.Mobile) && !GetChatBan(e.Mobile))
                Timer.DelayCall(TimeSpan.FromSeconds(10.0), OnTick, e.Mobile);
        }

        private static void OnTick(object state)
        {
            Mobile from = (Mobile)state;

            if (ChatSystem.Enabled && !GetChatOff(from) && !GetChatBan(from) && from.NetState != null)
                AddUser(from);
        }

        private static void EventSink_OnDisconnected(DisconnectedEventArgs e)
        {
            RemoveUser(e.Mobile);
        }

        public static void AddUser(Mobile from)
        {
            ChatUser user = ChatUser.GetChatUser(from);

            if (user != null)
                return;

            user = ChatUser.AddChatUser(from);

            SendSystemMessage(user, "Type \",\" followed by your message. To disable, say \"[chat\".");
        }

        public static void RemoveUser(Mobile from)
        {
            ChatUser user = ChatUser.GetChatUser(from);

            if (user == null)
                return;

            ChatUser.RemoveChatUser(user);
        }

        public static void SendSystemMessage(ChatUser to, string format, params object[] args)
        {
            to.SendMessage(57, to.Mobile, "0System", string.Format(format, args));
        }

        #region Account Tags

        public static bool GetChatOff(Mobile from)
        {
            Account acct = from.Account as Account;

            if (acct == null)
                return false;

            string tag = acct.GetTag("ChatOff");

            return (tag != null && Utility.ToBoolean(tag));
        }

        public static void SetChatOff(Mobile from, bool value)
        {
            Account acct = from.Account as Account;

            if (acct == null)
                return;

            if (value)
                acct.SetTag("ChatOff", value.ToString());
            else
                acct.RemoveTag("ChatOff");
        }

        public static bool GetChatBan(Mobile from)
        {
            Account acct = from.Account as Account;

            if (acct == null)
                return false;

            string tag = acct.GetTag("ChatBan");

            return (tag != null && Utility.ToBoolean(tag));
        }

        public static void SetChatBan(Mobile from, bool value)
        {
            Account acct = from.Account as Account;

            if (acct == null)
                return;

            if (value)
                acct.SetTag("ChatBan", value.ToString());
            else
                acct.RemoveTag("ChatBan");
        }

        #endregion
    }
}