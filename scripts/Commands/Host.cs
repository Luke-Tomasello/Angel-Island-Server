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

/* Scripts/Commands/Host.cs
 * 	CHANGELOG:
 *  12/19/06, Adam
 *      Improve output to show if this is the PROD or TC server.
 * 	2/23/06, Adam
 *	    Initial Version
 */

using System;
using System.Net;

namespace Server.Commands
{
    public class Host
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Host", AccessLevel.GameMaster, new CommandEventHandler(Host_OnCommand));
        }

        [Usage("Host")]
        [Description("Display host information.")]
        public static void Host_OnCommand(CommandEventArgs e)
        {
            try
            {
                string host = Dns.GetHostName();
                IPHostEntry iphe = Dns.Resolve(host);
                IPAddress[] ips = iphe.AddressList;

                e.Mobile.SendMessage("You are on the \"{0}\" Server.",
                    Utility.IsHostPROD(host) ? "PROD" : Utility.IsHostTC(host) ? "Test Center" : host);

                for (int i = 0; i < ips.Length; ++i)
                    e.Mobile.SendMessage("IP: {0}", ips[i]);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
    }
}