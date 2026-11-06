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

/* scripts\Accounting\AccountTotalIPLimiter.cs
 * CHANGELOG:
 *	8/5/22, Adam
 *		First time check in
 *		This module limits account creation based on IP of all accounts, logged in or not.
 */

using Server.Network;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Server.Misc
{
    public class AccountTotalIPLimiter
    {
        public static bool Enabled = true;

        // tell the other accounts that they are not authorized to connect another account
        public static void Notify(Accounting.Account acct, IPAddress ourAddress, int count)
        {
            if (Enabled)
                for (int i = 0; i < NetState.Instances.Count; ++i)
                {
                    NetState compState = NetState.Instances[i];
                    if (ourAddress.Equals(compState.Address) && compState.Mobile != null)
                        compState.Mobile.SendMessage(0x35, string.Format("Login: {0}: Account '{1}' blackholed, ip already has {2} account{3}.", ourAddress, acct.Username, count, count == 1 ? "" : "s"));
                }
        }

        public static bool IsOk(Accounting.Account acct, IPAddress ip, ref int count)
        {
            count = 0;

            if (!Enabled)
                return true;

            // Staff accounts are always allowed.
            if (acct.AccessLevel > AccessLevel.Player)
                return true;

            // Get all accounts associated with this IP.
            List<Accounting.Account> list = GetAccountsByIP(acct, ip);

            count = list.Count;

            // If any account on this IP is staff, exempt the entire IP.
            foreach (Accounting.Account a in list)
            {
                if (a != null && a.AccessLevel > AccessLevel.Player)
                    return true;
            }

            // No staff exemption, enforce normal limit.
            if (list.Count < CoreAI.MaxAccountsPerIP)
                return true;

            // Grab a copy of the illegal accounts sharing this IP for reporting purposes.
            List<Accounting.Account> illegal = new(list);

            // Keep only the first N legal accounts (oldest first, per GetAccountsByIP contract).
            list = list.Take(CoreAI.MaxAccountsPerIP).ToList();

            // Remove legal accounts from the illegal set.
            foreach (Accounting.Account a in list)
                illegal.Remove(a);

            // Allow login if this account is one of the legal accounts.
            if (ListContainsAccount(acct, list))
                return true;

            // Report(acct, list, illegal);

            // This account is not one of the allowed accounts for this IP.
            return false;
        }
        private static bool ListContainsAccount(Accounting.Account acct, List<Accounting.Account> list)
        {
            foreach (Accounting.Account current in list)
            {
                if (current.Username.ToLower() == acct.Username.ToLower())
                    return true;
            }

            return false;
        }
        public static List<Accounting.Account> GetAccountsByIP(Accounting.Account acct, IPAddress ip)
        {
            List<Accounting.Account> list = new(0);

            foreach (Accounting.Account a in Accounting.Accounts.Table.Values)
            {
                if (a == null)
                    continue;

                foreach (IPAddress loginIP in a.LoginIPs)
                {
                    if (IPAddress.Equals(loginIP, ip))
                    {
                        if (!list.Contains(a))
                            list.Add(a);

                        break;
                    }
                }
            }

            // sort in ascending order - first created account first, etc
            list = list.OrderBy(x => x.Created).ToList();

            return list;
        }
    }
}