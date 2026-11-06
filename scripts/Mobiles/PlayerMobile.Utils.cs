/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Mobiles\PlayerMobile.Utils.cs
 * ChangeLog:
 *  6/9/2026, Adam
 *      first check-in
 */

using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public partial class PlayerMobile
    {
        #region Login Messages

        private List<string> m_LoginMessages;
        private Timer? m_LoginMessageTimer;

        /// <summary>
        /// Queue a message to be shown to the player shortly after login.
        /// </summary>
        public void QueueLoginMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || Deleted)
                return;

            // Right? they're Incarcerated. No login blabber
            if (Utility.IsIncarcerated(this))
                return;

            m_LoginMessages ??= new List<string>();
            m_LoginMessages.Add(message);

            StartLoginMessageTimer();
        }

        private void StartLoginMessageTimer()
        {
            if (m_LoginMessageTimer != null)
                return;

            m_LoginMessageTimer = Timer.DelayCall(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), SendNextLoginMessage);
        }

        private void SendNextLoginMessage()
        {
            if (m_LoginMessages == null || m_LoginMessages.Count == 0 || NetState == null)
            {
                StopLoginMessageTimer();
                return;
            }

            // Send one message
            string message = m_LoginMessages[0];
            m_LoginMessages.RemoveAt(0);

            if (!string.IsNullOrWhiteSpace(message))
                SendMessage(0x35, message); // Yellow text

            // If queue is now empty, stop the timer
            if (m_LoginMessages.Count == 0)
                StopLoginMessageTimer();
        }

        private void StopLoginMessageTimer()
        {
            m_LoginMessageTimer?.Stop();
            m_LoginMessageTimer = null;
            m_LoginMessages?.Clear();
        }

        #endregion Login Messages
    }
}