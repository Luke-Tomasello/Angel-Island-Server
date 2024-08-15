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

/* scripts\Accounting\AccountConcurrentIPLimiter.cs
 * CHANGELOG:
 *  8/16/22, Adam
 *      Remove CountAccountsByIP (temp fix)
 *      Issue still not solved.
 *      players must wait 1 minute before logging in after a mortal death.
 *  8/15/22, Adam (temp fix)
 *      Mortalis: Add a call to AccountLimiter.CountAccountsByIP(cacct, ourAddress)
 *      We're having trouble with characters not being able log in immediately after a Mortalis death.
 *      I think this has to do with the delay associated with NS Dispose.
 *      I'll keep looking at this.
 *  8/5/22, Adam 
 *      - Verify changed to accept an account so that AccessLevel may be checked.
 *      - remove SocketBlock mode.
 *	4/16/09, Adam
 *		Add an assert() to make sure neither account is null in IPStillHot()
 *	11/26/08, Adam
 *		- don't count staff accounts
 *			note: because this check comes before the acct for ourAddress is known, staff can only exceed these limits if they login 
 * 			Player Accounts AFTER Staff Accounts.
 * 		- tell the other accounts on this IP that they are not authorized to connect another account to this IP
 *	11/25/08, Adam
 *		- Make MaxAddresses a core command console value
 *		- Add hot-swap protection (meaningful only when MaxAddresses == 1)
 *	2/27/06, Pix
 *		Changes for Verify().
 */


using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Net;

namespace Server.Misc
{
    public class AccountConcurrentIPLimiter
    {
        public static void Initialize()
        {   // start a timer (hashtable with time value) when a player disconnects
            EventSink.Disconnected += new DisconnectedEventHandler(EventSink_Disconnected);
        }

        public class WatchDog
        {
            private DateTime m_DateTime;
            public DateTime Limit { get { return m_DateTime; } }
            private Server.Accounting.Account m_Account;
            public Server.Accounting.Account Account { get { return m_Account; } }
            public WatchDog(Mobile m)
            {
                m_DateTime = DateTime.UtcNow + m.GetLogoutDelay();
                m_Account = m.Account as Server.Accounting.Account;
            }
        }

        private static Dictionary<IPAddress, WatchDog> m_IPMRU = new Dictionary<IPAddress, WatchDog>();
        public Dictionary<IPAddress, WatchDog> IPMRU { get { return m_IPMRU; } }

        /*
		 * When a player disconnects, put their IP and the time at which they will time-out into a hashtable. 
		 *	if someone tries to login, and we are at our MAX allowable IPs, then force them to wait until the other account/character timesout.
		 *	NOTE: This only prevents day-to-day hotswaps if MaxAddresses == 1
		 */
        private static void EventSink_Disconnected(DisconnectedEventArgs e)
        {
            try
            {
                if (e.Mobile != null && e.Mobile.Account != null)
                {
                    if ((e.Mobile.Account as Server.Accounting.Account).LoginIPs != null)
                    {
                        if ((e.Mobile.Account as Server.Accounting.Account).LoginIPs[0] != null)
                        {   // delete an old record
                            if (m_IPMRU.ContainsKey((e.Mobile.Account as Server.Accounting.Account).LoginIPs[0]))
                                m_IPMRU.Remove((e.Mobile.Account as Server.Accounting.Account).LoginIPs[0]);
                            // create a new record
                            m_IPMRU[(e.Mobile.Account as Server.Accounting.Account).LoginIPs[0]] = new WatchDog(e.Mobile);
                        }
                    }
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static bool Enabled = true;

        // how many of the same IP address can be concurrently logged in which may be greater than MaxAccountsPerIP
        public static int MaxConcurrentAddresses { get { return CoreAI.MaxConcurrentAddresses; } /*set { CoreAI.MaxAddresses = value; }*/ }

        // tell the other accounts that they are not authorized to connect another account
        public static void Notify(IPAddress ourAddress)
        {
            if (Enabled)
                for (int i = 0; i < NetState.Instances.Count; ++i)
                {
                    NetState compState = NetState.Instances[i];
                    if (ourAddress.Equals(compState.Address) && compState.Mobile != null)
                        compState.Mobile.SendMessage(0x35, string.Format("You are not authorized more than {0} concurrent connection{1} from this IP address.", MaxConcurrentAddresses, MaxConcurrentAddresses == 1 ? "" : "s"));
                }
        }

        // hot-swap prevention
        public static bool IPStillHot(Server.Accounting.Account acct, IPAddress ourAddress)
        {
            if (!Enabled)
                return false;

            int count = 0;
            for (int i = 0; i < NetState.Instances.Count; ++i)
            {
                NetState compState = NetState.Instances[i];
                if (ourAddress.Equals(compState.Address))
                    ++count;
            }

            // force this login to wait until the other client timesout (the same client is not restricted.)
            if (MaxConcurrentAddresses == count && m_IPMRU.ContainsKey(ourAddress) && m_IPMRU[ourAddress].Account != acct)
            {
                // if one of these accounts is null, then this test delived a false positive whereby prohibiting the player from logging in
                Diagnostics.Assert(m_IPMRU[ourAddress].Account != null && acct != null, "Account null in IPStillHot()", Utility.FileInfo());

                if (DateTime.UtcNow < m_IPMRU[ourAddress].Limit)
                    return true;
            }

            return false;
        }

        public static bool IsOk(Server.Accounting.Account cacct, IPAddress ourAddress, bool filter_mortal_inmates = false)
        {
            if (Enabled)
            {

                // don't count staff accounts
                if (cacct.AccessLevel > AccessLevel.Player)
                    return true;

                // see if there is another logged in account with this IP address
                List<NetState> netStates = NetState.Instances;
                int count = 0;
                for (int i = 0; i < netStates.Count; ++i)
                {
                    NetState compState = netStates[i];

                    if (compState.Mobile != null && compState.Mobile.Account != null)
                    {
                        Server.Accounting.Account acct = compState.Mobile.Account as Server.Accounting.Account;

                        // don't count staff against your total
                        if (acct.GetAccessLevel() > AccessLevel.Player)
                            continue;

                        // don't count mortal inmates against your total
                        if (filter_mortal_inmates)
                            if (compState.Mobile is PlayerMobile pm && pm.PrisonInmate && pm.Mortal)
                                continue;
                    }

                    // add up matching accounts (connections to this IP address)
                    if (ourAddress.Equals(compState.Address))
                    {
                        ++count;

                        if (count > MaxConcurrentAddresses /*&& totalAccounts > 0*/)
                            return false;
                    }
                }
            }

            return true;
        }
    }
}