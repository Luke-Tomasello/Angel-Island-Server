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

/* scripts\Accounting\AccountHardwareLimiter.cs
 * CHANGELOG:
 *  8/9/22, Adam
 *      Switch to account HardwareHashAcquired from account Created.
 *      Add AuthorizedUsers_OnCommand so staff can dump the Authorized Users for an account (that has a hardware hash)
 *  8/8/22, Adam
 *      HardwareLimiter redesign.
 *      Rather than trapping concurrent accounts logged in, we instead 'bind' to the first account created when it attains a hardware hash.
 *      The interesting aspect of this is that: (assuming different IPs and IPs < CoreAI.MaxAccountsPerIP - trapped elsewhere)
 *      1. you can login multiple accounts if 0-1 accounts have a machine account.
 *      2. Once the first account created for this shard has machine info, this algo 'binds' to that account.
 *      3. The user can keep creating new accounts that have not yet machine info
 *      4. the accounts created in #3 above are orphaned as soon as the account in #2 'binds'
 *          Maybe not the best user experience, but there is no other way.
 *  8/1/22, Adam
 *      Use new CoreAI.MaxAccountsPerMachine instead of hard coded 1
 *	7/30/22, Adam
 *		First time check in
 *		Use machine info (Spy On Client, packet 0xD9) to determine if this same machine is connected to more than one account.
 *		1. Spy On Client packet seems to be randomly sent - we'll have to make this work.
 *		2. Some shards like Siege Perilous and Mortalis only allow one account per IP Address. 
 *		    we use machine info (in addition to IP address) to limit access. HardwareLimiter is especially useful if the user is using a VPN to skate around this 
 *		    restriction.
 */


using Server.Accounting;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Linq;
using static Server.Diagnostics.LogHelper;

namespace Server.Misc
{
    public class AccountHardwareLimiter
    {
        public static bool Enabled = true;

        // tell the other accounts that they are not authorized to connect another account
        public static void Notify(Account acct)
        {
            if (Enabled)
                for (int i = 0; i < NetState.Instances.Count; ++i)
                {
                    NetState compState = NetState.Instances[i];
                    // sanity
                    if (compState.Mobile == null || compState.Mobile.Account == null)
                        continue;
                    Server.Accounting.Account current = compState.Mobile.Account as Server.Accounting.Account;
                    if (HardwareOk(acct) && HardwareOk(current) && compState.Mobile != null)
                        if (acct.HardwareHashRaw == current.HardwareHashRaw)
                            compState.Mobile.SendMessage(0x35, string.Format("You are only allowed {0} account(s) on this server.", CoreAI.MaxAccountsPerMachine));
                }
        }

        static bool HardwareOk(Account acct)
        {
#if false
            // See if at some point in time we got a good HardwareInfo
            if (acct.HardwareHashRaw == 0)
                return false;
            // okay, we have good hardware info
            return true;
#endif
            // See if at some point in time we got a good HardwareInfo
            var newList = acct.Machines.Where(i => i != 0).ToList();
            return newList.Count > 0;
        }
        public static List<Account> GetAccountsByMachine(Accounting.Account acct)
        {
            List<Account> list = new List<Account>();

            // staff can always login
            if (acct.AccessLevel > AccessLevel.Player)
                return list;

            // can't check what we don't know
            if (HardwareOk(acct) == false)
                // i.e., if they have no hardware info, they can login
                //  however, as soon as their hardware info IS known, they will only be able to log into the known acct(s)
                return list;

            // TODO: HACK: 5/11/2023, Adam: Don't know how this value is getting salted away.
            //  Needs deeper research
            //  Two accounts have this value, but the guy swears he has nothing to do with the other account.
            if (acct.Machines.Contains(-1934357564))
                return list;

            foreach (Accounting.Account current in Accounting.Accounts.Table.Values)
            {
                if (current.AccessLevel > AccessLevel.Player)
                    continue; // ignore staff accounts

                // can't check what we don't know
                if (HardwareOk(current) == false)
                    continue;

                // add up matching accounts (connections by this machine)
                if (acct.Machines.Intersect(current.Machines).Any())
                    if (!list.Contains(current))
                        list.Add(current);
            }

            // sort in ascending order - first created account first, etc
            list = list.OrderBy(x => x.Created).ToList();

            return list;
        }
        private static bool ListContainsAccount(Account acct, List<Account> list)
        {
            foreach (Account current in list)
            {
                if (current.Username.ToLower() == acct.Username.ToLower())
                    return true;
            }

            return false;
        }
        public static bool IsOk(Account acct)
        {
            if (Enabled)
            {
                // get a list of accounts associated with this machine ascending sorted by date created that match the acct in question
                List<Account> list = GetAccountsByMachine(acct);

                // no problem, create the account
                if (list.Count < CoreAI.MaxAccountsPerMachine)
                    return true;
                else
                {
                    // grab a copy of the illegal accounts sharing machine info - for reporting purposes.
                    List<Account> illegal = new(list);

                    // trim it to the first N accounts (N = MaxAccountsPerMachine)
                    // Accounts after MaxAccountsPerMachine were likely created without machine info,
                    //  then the account's machine info was updated sometime after creation
                    list = list.Skip(0).Take(CoreAI.MaxAccountsPerMachine).ToList();

                    // remove legal accounts
                    foreach (Account a in list)
                        illegal.Remove(a);

                    // they are trying to login on a legit account
                    if (ListContainsAccount(acct, list))
                        return true;

                    //Report what's going on
                    Report(acct, list, illegal);

                    // the account they are trying to login with is not one of the N already created that have machine info
                    // where N = MaxAccountsPerMachine
                    return false;
                }
            }

            return true;
        }
        private static void Report(Account acct, List<Account> legal, List<Account> illegal)
        {
            LogBlockedConnection(string.Format("--- Login Report ---"));
            LogBlockedConnection(string.Format("Login: {0}: Past machine limit threshold", acct));
            LogBlockedConnection(string.Format("Legal accounts:"));
            foreach (Account a in legal)
                LogBlockedConnection(string.Format("\t{0}", a));
            LogBlockedConnection(string.Format("Illegal accounts:"));
            foreach (Account a in illegal)
                LogBlockedConnection(string.Format("\t{0}", a));
            LogBlockedConnection(string.Format("--- End Report ---"));
        }
        public static void Initialize()
        {
            Server.CommandSystem.Register("AuthorizedUsers", AccessLevel.Administrator, new CommandEventHandler(AuthorizedUsers_OnCommand));
        }

        [Usage("AuthorizedUsers <target player>")]
        [Description("Dumps the users with a hardware hash that matches this user.")]
        public static void AuthorizedUsers_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target player to analyze...");
            e.Mobile.Target = new PickPlayerTarget(); // Call our target
        }

        public class PickPlayerTarget : Target // Create our targeting class (which we derive from the base target class)
        {
            public PickPlayerTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile m)
                {
                    Account acct = m.Account != null ? m.Account as Account : null;
                    if (acct != null && acct.HardwareHashRaw != 0)
                    {
                        if (acct.AccessLevel > AccessLevel.Player)
                        {
                            from.SendMessage("You cannot analyze staff accounts.");
                            return;
                        }

                        // get a list of accounts associated with this machine ascending sorted by date created that match the acct in question
                        List<Account> list = GetAccountsByMachine(acct);

                        // no authorized accounts to list
                        if (list.Count < CoreAI.MaxAccountsPerMachine)
                        {
                            from.SendMessage("No authorized accounts.");
                            return;
                        }

                        // DUMP1
                        Utility.Monitor.WriteLine(string.Format("all matching users"), ConsoleColor.Red);
                        foreach (Account ax in list)
                            Utility.Monitor.WriteLine(string.Format("{0} hash {1}", ax, ax.HardwareHashRaw), ConsoleColor.Red);

                        // trim it to the first N accounts (N = MaxAccountsPerMachine)
                        // Accounts after MaxAccountsPerMachine were likely created without machine info,
                        //  then the account's machine info was updated sometime after creation
                        list = list.Skip(0).Take(CoreAI.MaxAccountsPerMachine).ToList();

                        // DUMP2
                        Utility.Monitor.WriteLine(string.Format("{0} Authorized users", CoreAI.MaxAccountsPerMachine), ConsoleColor.Red);
                        foreach (Account ax in list)
                            Utility.Monitor.WriteLine(string.Format("{0} hash {1}", ax, ax.HardwareHashRaw), ConsoleColor.Red);

                        from.SendMessage("Authorized dumped to console.");
                    }
                    else
                        from.SendMessage("Nothing to analyze.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
    }
}