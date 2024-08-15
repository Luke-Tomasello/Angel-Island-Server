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

/* Scripts/Accounting/AccountHandler.cs
 * ChangeLog:
 *  8/5/22, Adam
 *      Remove IPLimiter.Verify(ip) from socket block
 *      Now only called from AccountHandlers
 *  8/1/22, Adam
 *      Yellow text for networking messages
 *	2/27/06 - Pix.
 *		Added.
 */

using System;
using System.Net;

namespace Server
{
    public class AccessRestrictions
    {
        public static void Initialize()
        {
            EventSink.SocketConnect += new SocketConnectEventHandler(EventSink_SocketConnect);
        }

        private static void EventSink_SocketConnect(SocketConnectEventArgs e)
        {
            try
            {
                IPAddress ip = ((IPEndPoint)e.Socket.RemoteEndPoint).Address;

                if (Firewall.IsBlocked(ip.ToString()))
                {
                    Diagnostics.LogHelper.LogBlockedConnection(string.Format("Client: {0}: Firewall blocked connection attempt.", ip));
                    e.AllowConnection = false;
                    return;
                }
                /*else if (IPLimiter.SocketBlock && !IPLimiter.Verify(ip))
                {
                    Diagnostics.LogHelper.LogBlockedConnection(string.Format("Client: {0}: Past IP limit threshold", ip));
                    e.AllowConnection = false;
                    return;
                }*/
            }
            catch
            {
                e.AllowConnection = false;
            }
        }
    }
}