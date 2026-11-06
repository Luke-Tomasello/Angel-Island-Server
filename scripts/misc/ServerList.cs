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

/* Scripts/Misc/ServerList.cs
 * Changelog:
 *  9/23/26, Adam
*      Refactor server list into a table-driven shard configuration.
*      Add dynamic per-shard visibility with persistent FeatureBits.
*      Owner IPs always see all configured shards regardless of visibility.
*      Administrative remains restricted to Owner IPs.
*      Expose shard configuration for runtime inspection and visibility management.
 *  8/5/22, Adam
 *      Enable "Administrative" shard access. This is the login server and is only accessible by developers/owner
 *  7/27/22, Adam
 *      Enable "Mortalis"
 *  5/3/13, Adam
 *      Remove all servers but Angel Island
 *	2/28/11, Adam
 *		Remove "Mortalis Event" from the server list
 *	2/15/11, Adam
 *		Add the EventShardPort EndPoint
 *		This endpoint is used for temporary event shards.
 *		Only 1 at a time is supported
 *		See Comments in SocketOptions.cs
 *	02/11/11, Adam
 *		Setup the MortalisPort EndPoint
 *  11/17,06, Adam
 *      Add: #pragma warning disable 429
 *      The Unreachable code complaints in this file are acceptable
 *      C:\Program Files\RunUO\Scripts\Misc\ServerList.cs(65,38): warning CS0429: Unreachable expression code detected
 *      C:\Program Files\RunUO\Scripts\Misc\ServerList.cs(65,67): warning CS0429: Unreachable expression code detected
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *  4/5/04 code changes by Pixie
 *		Changed servername to "Angel Island"
 */

using Server.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Server.Misc
{
    public class ServerList
    {
        /* 
           * The default setting for Address, a value of 'null', will use your local IP address. If all of your local IP addresses
           * are private network addresses and AutoDetect is 'true' then RunUO will attempt to discover your public IP address
           * for you automatically.
           *
           * If you do not plan on allowing clients outside of your LAN to connect, you can set AutoDetect to 'false' and leave
           * Address set to 'null'.
           * 
           * If your public IP address cannot be determined, you must change the value of Address to your public IP address
           * manually to allow clients outside of your LAN to connect to your server. Address can be either an IP address or
           * a hostname that will be resolved when RunUO starts.
           * 
           * If you want players outside your LAN to be able to connect to your server and you are behind a router, you must also
           * forward TCP port 2593 to your private IP address. The procedure for doing this varies by manufacturer but generally
           * involves configuration of the router through your web browser.
           *
           * ServerList will direct connecting clients depending on both the address they are connecting from and the address and
           * port they are connecting to. If it is determined that both ends of a connection are private IP addresses, ServerList
           * will direct the client to the local private IP address. If a client is connecting to a local public IP address, they
           * will be directed to whichever address and port they initially connected to. This allows multihomed servers to function
           * properly and fully supports listening on multiple ports. If a client with a public IP address is connecting to a
           * locally private address, the server will direct the client to either the AutoDetected IP address or the manually entered
           * IP address or hostname, whichever is applicable. Loopback clients will be directed to loopback.
           * 
           * If you would like to listen on additional ports (i.e. 22, 23, 80, for clients behind highly restrictive egress
           * firewalls) or specific IP adddresses you can do so by modifying the file SocketOptions.cs found in this directory.
           */

        public static readonly string? Address = null;
        public static readonly bool AutoDetect = false;
        private static bool m_PlayerView;
        private static IPAddress? m_PublicAddress;

        /// <summary>
        /// Defines a game server advertised by the login server.
        /// Visibility is backed by CoreAI dynamic feature bits.
        /// </summary>
        public class ShardEntry
        {
            public string Name { get; }
            public int Port { get; }
            public CoreAI.FeatureBits VisibilityFeature { get; }

            public bool Visible
            {
                get => CoreAI.IsDynamicFeatureSet(VisibilityFeature);
                set
                {
                    if (value)
                        CoreAI.SetDynamicFeature(VisibilityFeature);
                    else
                        CoreAI.ClearDynamicFeature(VisibilityFeature);
                }
            }

            public ShardEntry(string name, int port, CoreAI.FeatureBits visibilityFeature)
            {
                Name = name;
                Port = port;
                VisibilityFeature = visibilityFeature;
            }
        }

        /*
         * This table is the authoritative definition of game servers advertised
         * by the login server.
         *
         * The Name is both the client-visible shard name and the canonical name
         * used by administrative tools.
         *
         * Owner IPs always see every shard regardless of its visibility setting.
         *
         * Administrative is intentionally not included here. It remains an
         * Owner-IP-only endpoint and is never publicly advertised.
         */
        private static readonly List<ShardEntry> m_Shards = new()
        {
            new("Angel Island",     SocketOptions.AngelIslandPort,     CoreAI.FeatureBits.AngelIslandVisible),
            new("Siege Perilous",   SocketOptions.SiegePerilousPort,   CoreAI.FeatureBits.SiegePerilousVisible),
            new("Mortalis",         SocketOptions.MortalisPort,         CoreAI.FeatureBits.MortalisVisible),
            new("UO Seasons",       SocketOptions.SeasonsPort,          CoreAI.FeatureBits.SeasonsVisible),
            new("Upheaval Test",    SocketOptions.TestCenterPort,       CoreAI.FeatureBits.TestCenterVisible),
            new("Upheaval",         SocketOptions.UpheavalPort,         CoreAI.FeatureBits.UpheavalVisible)
        };

        /// <summary>
        /// Game servers known to the login server.
        /// </summary>
        public static IReadOnlyList<ShardEntry> Shards => m_Shards;

        public static void Initialize()
        {
            if (Address == null)
            {
                if (AutoDetect)
                    AutoDetection();
            }
            else
            {
                Resolve(Address, out m_PublicAddress);
            }

            EventSink.ServerList += new ServerListEventHandler(EventSink_ServerList);

            CommandSystem.Register(
                "ServerList",
                AccessLevel.Owner,
                new CommandEventHandler(ServerList_OnCommand));
        }

        private static void EventSink_ServerList(ServerListEventArgs e)
        {
            try
            {
                NetState ns = e.State;
                Socket socket = ns.Socket;

                IPEndPoint? localEndPoint = (IPEndPoint?)socket.LocalEndPoint;

                IPAddress localAddress = localEndPoint.Address;

                /*
                 * Determine the address that should be advertised back to this
                 * particular client.
                 */
                if (IsPrivateNetwork(localAddress))
                {
                    IPEndPoint? remoteEndPoint = (IPEndPoint?)socket.RemoteEndPoint;

                    if (!IsPrivateNetwork(remoteEndPoint.Address) && m_PublicAddress != null)
                        localAddress = m_PublicAddress;
                }

                bool ownerIP =
                    !m_PlayerView &&
                    ns != null &&
                    ns.Address != null &&
                    Server.Commands.OwnerTools.IsOwnerIP(ns.Address);

                /*
                 * Owner IPs always see every game server.
                 *
                 * Everyone else sees only shards currently marked visible.
                 * Visibility affects advertisement only; individual game-server
                 * lockdown remains authoritative for actual access.
                 */
                foreach (ShardEntry shard in m_Shards)
                {
                    if (ownerIP || shard.Visible)
                    {
                        e.AddServer(
                            shard.Name,
                            new IPEndPoint(localAddress, shard.Port));
                    }
                }

                /*
                 * Administrative is deliberately outside normal shard visibility.
                 * It is always restricted to Owner IPs.
                 */
                if (ownerIP)
                {
                    e.AddServer(
                        "Administrative",
                        new IPEndPoint(localAddress, SocketOptions.LoginServerPortBase));
                }
            }
            catch
            {
                e.Rejected = true;
            }
        }

        /// <summary>
        /// Finds a shard by its canonical/display name.
        /// Matching is case-insensitive.
        /// </summary>
        public static ShardEntry? FindShard(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            foreach (ShardEntry shard in m_Shards)
            {
                if (string.Equals(shard.Name, name, StringComparison.OrdinalIgnoreCase))
                    return shard;
            }

            return null;
        }

        private static void AutoDetection()
        {
            if (!HasPublicIPAddress())
            {
                Console.Write("ServerList: Auto-detecting public IP address...");
                m_PublicAddress = FindPublicAddress();

                if (m_PublicAddress != null)
                    Console.WriteLine("done ({0})", m_PublicAddress);
                else
                    Console.WriteLine("failed");
            }
        }

        private static void Resolve(string addr, out IPAddress outValue)
        {
            if (IPAddress.TryParse(addr, out outValue))
                return;

            try
            {
                IPHostEntry iphe = Dns.GetHostEntry(addr);

                if (iphe.AddressList.Length > 0)
                    outValue = iphe.AddressList[iphe.AddressList.Length - 1];
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        private static bool HasPublicIPAddress()
        {
            IPHostEntry iphe = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress[] ips = iphe.AddressList;

            for (int i = 0; i < ips.Length; ++i)
            {
                if (!IsPrivateNetwork(ips[i]))
                    return true;
            }

            return false;
        }

        public static bool IsPrivateNetwork(string sip)
        {
            if (!IPAddress.TryParse(sip, out IPAddress? ip))
                return false;

            return IsPrivateNetwork(ip);
        }

        public static bool IsPrivateNetwork(IPAddress ip)
        {
            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                return false;

            // Loopback
            if (Utility.IPMatch("127.0.0.*", ip))
                return true;

            // RFC 1918
            if (Utility.IPMatch("192.168.*", ip))
                return true;

            if (Utility.IPMatch("10.*", ip))
                return true;

            if (Utility.IPMatch("172.16-31.*", ip))
                return true;

            // Link-local
            if (Utility.IPMatch("169.254.*", ip))
                return true;

            // RFC 6598 carrier-grade NAT
            if (Utility.IPMatch("100.64-127.*", ip))
                return true;

            return false;
        }

        private static IPAddress? FindPublicAddress()
        {
            try
            {
                WebRequest req = HttpWebRequest.Create("http://uo.cx/ip.php");
                req.Timeout = 15000;

                WebResponse res = req.GetResponse();
                Stream stream = res.GetResponseStream();
                StreamReader reader = new StreamReader(stream);

                IPAddress ip = IPAddress.Parse(reader.ReadLine());

                reader.Close();
                stream.Close();
                res.Close();

                return ip;
            }
            catch
            {
                return null;
            }
        }

        #region Commands

        [Usage("ServerList [PlayerView <true|false>]")]
        [Description("Controls diagnostic server-list behavior.")]
        public static void ServerList_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length == 0)
            {
                e.Mobile.SendMessage(
                    "ServerList PlayerView is {0}.",
                    m_PlayerView ? "enabled" : "disabled");

                return;
            }

            if (e.Arguments.Length == 2 &&
                e.Arguments[0].Equals("PlayerView", StringComparison.OrdinalIgnoreCase) &&
                bool.TryParse(e.Arguments[1], out bool value))
            {
                m_PlayerView = value;

                if (m_PlayerView)
                {
                    e.Mobile.SendMessage(
                        "WARNING: ServerList PlayerView enabled. Owner IPs will receive the public player server list.");

                    Console.WriteLine(
                        "WARNING: ServerList PlayerView enabled. Owner IP server-list exceptions are temporarily disabled.");
                }
                else
                {
                    e.Mobile.SendMessage(
                        "ServerList PlayerView disabled. Owner IPs will receive the normal Owner server list.");

                    Console.WriteLine(
                        "ServerList PlayerView disabled. Normal Owner IP server-list behavior restored.");
                }

                return;
            }

            e.Mobile.SendMessage("Usage: [ServerList [PlayerView <true|false>]");
        }
        #endregion Commands
    }
}
