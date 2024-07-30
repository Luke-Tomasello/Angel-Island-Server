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

using Server.Network;
using System;
using System.Collections;
using System.Net;

namespace Server.Accounting
{
    public class AccountAttackLimiter
    {
        public static bool Enabled = true;

        public static void Initialize()
        {
            if (!Enabled)
                return;

            RegisterThrottler(0x80);
            RegisterThrottler(0x91);
            RegisterThrottler(0xCF);
        }

        public static void RegisterThrottler(int packetID)
        {
            PacketHandler ph = PacketHandlers.GetHandler(packetID);

            if (ph == null)
                return;

            ph.ThrottleCallback = new ThrottlePacketCallback(Throttle_Callback);
        }

        public static bool Throttle_Callback(NetState ns)
        {
            InvalidAccountAccessLog accessLog = FindAccessLog(ns);

            if (accessLog == null)
                return true;

            return (DateTime.UtcNow >= (accessLog.LastAccessTime + ComputeThrottle(accessLog.Counts)));
        }

        private static ArrayList m_List = new ArrayList();

        public static InvalidAccountAccessLog FindAccessLog(NetState ns)
        {
            if (ns == null)
                return null;

            IPAddress ipAddress = ns.Address;

            for (int i = 0; i < m_List.Count; ++i)
            {
                InvalidAccountAccessLog accessLog = (InvalidAccountAccessLog)m_List[i];

                if (accessLog.HasExpired)
                    m_List.RemoveAt(i--);
                else if (accessLog.Address.Equals(ipAddress))
                    return accessLog;
            }

            return null;
        }

        public static void RegisterInvalidAccess(NetState ns)
        {
            if (ns == null || !Enabled)
                return;

            InvalidAccountAccessLog accessLog = FindAccessLog(ns);

            if (accessLog == null)
                m_List.Add(accessLog = new InvalidAccountAccessLog(ns.Address));

            accessLog.Counts += 1;
            accessLog.RefreshAccessTime();
        }

        public static TimeSpan ComputeThrottle(int counts)
        {
            if (counts >= 15)
                return TimeSpan.FromMinutes(5.0);

            if (counts >= 10)
                return TimeSpan.FromMinutes(1.0);

            if (counts >= 5)
                return TimeSpan.FromSeconds(20.0);

            if (counts >= 3)
                return TimeSpan.FromSeconds(10.0);

            if (counts >= 1)
                return TimeSpan.FromSeconds(2.0);

            return TimeSpan.Zero;
        }
    }

    public class InvalidAccountAccessLog
    {
        private IPAddress m_Address;
        private DateTime m_LastAccessTime;
        private int m_Counts;

        public IPAddress Address
        {
            get { return m_Address; }
            set { m_Address = value; }
        }

        public DateTime LastAccessTime
        {
            get { return m_LastAccessTime; }
            set { m_LastAccessTime = value; }
        }

        public bool HasExpired
        {
            get { return (DateTime.UtcNow >= (m_LastAccessTime + TimeSpan.FromHours(1.0))); }
        }

        public int Counts
        {
            get { return m_Counts; }
            set { m_Counts = value; }
        }

        public void RefreshAccessTime()
        {
            m_LastAccessTime = DateTime.UtcNow;
        }

        public InvalidAccountAccessLog(IPAddress address)
        {
            m_Address = address;
            RefreshAccessTime();
        }
    }
}