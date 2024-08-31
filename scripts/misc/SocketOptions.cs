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

/* Scripts/Misc/SocketOptions.cs
 * ChangeLog
 *	8/13/21, Adam
 *	    Add support for a dedicated LoginServer.
 *	    The only publically known listening port is the standard 2593. Should we come under a DoS attack, I will publish other ports.
 *	    The *server* ports are to always remain secret. 
 *	2/15/11, Adam
 *		Core EventShard can be turned on with all other server configurations and overrides the default port for that shard.
 *		For example, one can turn on -uoev -uomo yielding an event shard with the Mortalis rule set, but a port number that
 *		won't collide with the production Mortalis.
 *		Note you can even have a Test Center version of an event shard, i.e., -uoev -uomo -uotc
 *  11/12/10, Adam
 *      Add in Core.TestCenter listening port
 *	2/27/06, Pix
 *		Changes for IPLimiter.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Network;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Newtonsoft;
using Newtonsoft.Json;
namespace Server
{
    public class SocketOptions
    {
        private const bool NagleEnabled = false; // Should the Nagle algorithm be enabled? This may reduce performance
        private const int CoalesceBufferSize = 512; // MSS that the core will use when buffering packets
        private const int PooledSockets = 32; // The number of sockets to initially pool. Ideal value is expected client count. 

        public static int AngelIslandPort
        {
            get
            {
                string json = File.ReadAllText("Ports.json");
                Dictionary<string, int> portsTable = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                return portsTable["AngelIslandPort"];
            }
        }
        public static int TestCenterPort
        {
            get
            {
                string json = File.ReadAllText("Ports.json");
                Dictionary<string, int> portsTable = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                return portsTable["TestCenterPort"];
            }
        }
        public static int SiegePerilousPort
        {
            get
            {
                string json = File.ReadAllText("Ports.json");
                Dictionary<string, int> portsTable = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                return portsTable["SiegePerilousPort"];
            }
        }
        public static int MortalisPort
        {
            get
            {
                string json = File.ReadAllText("Ports.json");
                Dictionary<string, int> portsTable = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                return portsTable["MortalisPort"];
            }
        }
        public static int RenaissancePort
        {
            get
            {
                string json = File.ReadAllText("Ports.json");
                Dictionary<string, int> portsTable = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                return portsTable["RenaissancePort"];
            }
        }
        public static int EventShardPort
        {
            get
            {
                string json = File.ReadAllText("Ports.json");
                Dictionary<string, int> portsTable = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                return portsTable["EventShardPort"];
            }
        }
        public static int LoginServerPortBase
        {
            get
            {
                string json = File.ReadAllText("Ports.json");
                Dictionary<string, int> portsTable = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                return portsTable["LoginServerPortBase"];
            }
        }

        private static IPEndPoint[] m_ListenerEndPoints;
        //        = new IPEndPoint[] {
        //			new IPEndPoint( IPAddress.Any, Port ), // Default: Listen on port 2593 on all IP addresses
        //			
        //			// Examples:
        //			// new IPEndPoint( IPAddress.Any, 80 ), // Listen on port 80 on all IP addresses
        //			// new IPEndPoint( IPAddress.Parse( "1.2.3.4" ), 2593 ), // Listen on port 2593 on IP address 1.2.3.4
        //		};

        public static void Initialize()
        {
            if (Core.RuleSets.LoginServerRules())
                m_ListenerEndPoints = new IPEndPoint[6];
            else
                m_ListenerEndPoints = new IPEndPoint[1];

            if (Core.RuleSets.LoginServerRules())
            {
                int portnext = 0;
                m_ListenerEndPoints[portnext] = new IPEndPoint(IPAddress.Any, LoginServerPortBase + portnext++ + 0);    // basic login at 2593
                m_ListenerEndPoints[portnext] = new IPEndPoint(IPAddress.Any, LoginServerPortBase + portnext++ + 50);   // login at 2593 + 1 + 50 = 2644
                m_ListenerEndPoints[portnext] = new IPEndPoint(IPAddress.Any, LoginServerPortBase + portnext++ + 60);   // etc.
                m_ListenerEndPoints[portnext] = new IPEndPoint(IPAddress.Any, LoginServerPortBase + portnext++ + 70);
                m_ListenerEndPoints[portnext] = new IPEndPoint(IPAddress.Any, LoginServerPortBase + portnext++ + 80);
                m_ListenerEndPoints[portnext] = new IPEndPoint(IPAddress.Any, LoginServerPortBase + portnext++ + 90);
            }
            else if (Core.UOEV_CFG)
            {
                // Core EventShard can be turned on with all other server configurations and overrides the default port for that shard.
                //  For example, one can turn on -uoev -uomo yielding an event shard with the Mortalis rule set, but a port number that
                //	won't collide with the production Mortalis.
                //	Note you can even have a Test Center version of an event shard, i.e., -uoev -uomo -uotc
                m_ListenerEndPoints[0] = new IPEndPoint(IPAddress.Any, EventShardPort);
            }
            else
            {
                // Core TestCenter can be turned on with Core.UOSP giving us a SP TC
                //  otherwise it is the usual AI TC
                //  but whatever the test center mode, it always gets its own port
                if (Core.UOTC_CFG)
                {
#if CORE_UOBETA
                    m_ListenerEndPoints[0] = new IPEndPoint(IPAddress.Any, AngelIslandPort);
#else
                    m_ListenerEndPoints[0] = new IPEndPoint(IPAddress.Any, TestCenterPort);
#endif
                }
                else
                {
                    if (Core.RuleSets.SiegeRules())
                        m_ListenerEndPoints[0] = new IPEndPoint(IPAddress.Any, SiegePerilousPort);
                    else if (Core.RuleSets.MortalisRules())
                        m_ListenerEndPoints[0] = new IPEndPoint(IPAddress.Any, MortalisPort);
                    else if (Core.RuleSets.RenaissanceRules())
                        m_ListenerEndPoints[0] = new IPEndPoint(IPAddress.Any, RenaissancePort);
                    else
                        m_ListenerEndPoints[0] = new IPEndPoint(IPAddress.Any, AngelIslandPort);
                }
            }

            SendQueue.CoalesceBufferSize = CoalesceBufferSize;
            SocketPool.InitialCapacity = PooledSockets;

            EventSink.SocketConnect += new SocketConnectEventHandler(EventSink_SocketConnect);

            Listener.EndPoints = m_ListenerEndPoints;
        }

        private static void EventSink_SocketConnect(SocketConnectEventArgs e)
        {
            if (!e.AllowConnection)
                return;

            if (!NagleEnabled)
                e.Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1); // RunUO uses its own algorithm
        }
    }
}

//using System;
//using System.IO;
//using System.Net;
//using System.Net.Sockets;
//using Server;
//using Server.Misc;
//using Server.Network;

//namespace Server
//{
//    public class SocketOptions
//    {
//        private const int CoalesceBufferSize = 512; // MSS that the core will use when buffering packets, a value of 0 will turn this buffering off and Nagle on

//        private static int[] m_AdditionalPorts = new int[0];
//        //private static int[] m_AdditionalPorts = new int[]{ 2594 };

//        public static void Initialize()
//        {
//            NetState.CreatedCallback = new NetStateCreatedCallback( NetState_Created );
//            SendQueue.CoalesceBufferSize = CoalesceBufferSize;

//            if ( m_AdditionalPorts.Length > 0 )
//                EventSink.ServerStarted += new ServerStartedEventHandler( EventSink_ServerStarted );
//        }

//        public static void EventSink_ServerStarted()
//        {
//            //for (int i = 0; i < m_AdditionalPorts.Length; ++i)
//            //{
//            //    Core.MessagePump.AddListener(new Listener(m_AdditionalPorts[i]));
//            //}
//        }

//        public static void NetState_Created( NetState ns )
//        {
//            if ( IPLimiter.SocketBlock && !IPLimiter.Verify( ns.Address ) )
//            {
//                Console.WriteLine( "Login: {0}: Past IP limit threshold", ns );

//                using ( StreamWriter op = new StreamWriter( "ipLimits.log", true ) )
//                    op.WriteLine( "{0}\tPast IP limit threshold\t{1}", ns, DateTime.UtcNow );

//                ns.Dispose();
//                return;
//            }

//            Socket s = ns.Socket;

//            s.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 15000 );
//            s.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 15000 );

//            if ( CoalesceBufferSize > 0 )
//                s.SetSocketOption( SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1 ); // RunUO uses its own algorithm
//        }
//    }
//}