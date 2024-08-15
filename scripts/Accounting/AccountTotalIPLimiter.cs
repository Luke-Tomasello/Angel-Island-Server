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

/* scripts\Accounting\AccountTotalIPLimiter.cs
 * CHANGELOG:
 *	8/5/22, Adam
 *		First time check in
 *		This module limits account creation based on IP of all accounts, logged in or not.
 */

using Server.Network;
using System;
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
#if false
                        compState.Mobile.SendMessage(0x35, string.Format("Login: {0}: Account '{1}' not created, ip already has {2} account{3}.", ourAddress, acct.Username, count, count == 1 ? "" : "s"));
#else
                        compState.Mobile.SendMessage(0x35, string.Format("Login: {0}: Account '{1}' blackholed, ip already has {2} account{3}.", ourAddress, acct.Username, count, count == 1 ? "" : "s"));
#endif
                }
        }

        public static bool IsOk(Accounting.Account acct, IPAddress ip, ref int count)
        {
            if (Enabled)
            {
                // get a list of accounts associated with this machine ascending sorted by date created that match the acct in question
                List<Accounting.Account> list = GetAccountsByIP(acct, ip);

                count = list.Count;

                // no problem, create the account
                if (list.Count < CoreAI.MaxAccountsPerIP)
                    return true;
                else
                {
                    // grab a copy of the illegal accounts sharing machine info - for reporting purposes.
                    List<Accounting.Account> illegal = new(list);

                    // trim it to the first N accounts (N = MaxAccountsPerIP)
                    list = list.Skip(0).Take(CoreAI.MaxAccountsPerIP).ToList();

                    // remove legal accounts
                    foreach (Accounting.Account a in list)
                        illegal.Remove(a);

                    // they are trying to login on a legit account
                    if (ListContainsAccount(acct, list))
                        return true;

                    //Report what's going on
                    //Report(acct, list, illegal);

                    // the account they are trying to login with is not one of the N already created that have machine info
                    // where N = MaxAccountsPerMachine
                    return false;
#if false
                int limit = Math.Max(CoreAI.MaxAccountsPerIP, Accounting.IPException.GetLimit(ip.ToString()));
                count = CountAccountsByIP(acct, ip); // check the number of accounts based on IP
                if (count >= limit)
                    return false;
#endif
                }
                return true;
            }

            return true;
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

            // staff can always login
            if (acct.AccessLevel > AccessLevel.Player)
                return list;

            int count = 0;
            foreach (Accounting.Account a in Accounting.Accounts.Table.Values)
            {
                if (a.AccessLevel > AccessLevel.Player)
                    continue; // ignore staff accounts

                foreach (IPAddress loginIP in a.LoginIPs)
                {
                    if (IPAddress.Equals(loginIP, ip))
                        if (!list.Contains(a))
                            list.Add(a);
                }
            }

            // sort in ascending order - first created account first, etc
            list = list.OrderBy(x => x.Created).ToList();

            return list;
        }
#if false
        public static int CountAccountsByIP(Accounting.Account acct, IPAddress ip)
        {
            // staff can always login
            if (acct.AccessLevel > AccessLevel.Player)
                return 0;

            int count = 0;
            foreach (Accounting.Account a in Accounting.Accounts.Table.Values)
            {
                if (a.AccessLevel > AccessLevel.Player)
                    continue; // ignore staff accounts

                if (a.Username.ToLower() == acct.Username.ToLower())
                    continue; // ignore this account

                foreach (IPAddress loginIP in a.LoginIPs)
                {
                    if (IPAddress.Equals(loginIP, ip))
                        count++;
                }
            }

            return count;
        }
#endif
    }
}