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

/***************************************************************************
 * We use GeoLite2 Free Geolocation Data from MaxMind
 * https://dev.maxmind.com/geoip/geolite2-free-geolocation-data
 * EditionIDs GeoLite2-ASN GeoLite2-City GeoLite2-Country
 * 
 * We use this web service to lookup login IP Addresses and tell us the following:
 * 1. Hosting Provider/Data Center, 
 * 2. VPN, 
 * 3. Residential Proxy, 
 * 4. Tor Exit Node, 
 * 5. and Public Proxy.
 * 
 * With this knowledge, and other tools, we can reasonably determine if a user is using some sort of proxy 
 * to get around our MAX CONNECTIONS limits.
 * 
 * You'll want to visit the URL above and sign up for the free GeoLite2.
 * You will get an AccountID and LicenseKey. 
 * 
 * See usage in this file: AcquireIpInfo()
 ***************************************************************************/

/* Scripts/Accounting/AccountHandler.cs
 * ChangeLog:
 *  8/15/2024, Adam (GetBestUNMatch)
 *      unfortunately, SQLite database is case-sensitive.
 *      Example: Create the "Adam Ant" account. Next login is with "adam ant". SQLite won't recognize "adam ant"
 *      We will look for the best match here
 *  9/29/2023, Adam (Delete Character and Tor Exit Node)
 *      If a player entered our shard via a Tor Exit Node, the likelihood that they are a bad actor is very high.
 *      Don't know what they are up to, but we have sent them to prison, and disallow them from deleting their character
 *      (so they can start again.)
 *  8/27/2023, Adam (MaxMind.GeoIP2)
 *      Implement IP lookup/analysis to determine many aspects of an IP address. Like for instance, is it a VPN
 *      See also: AccountIPManager.cs
 *  5/4/23, Adam (encrypted credentials)
 *      Our version of CUO (GMNCUO) now user encrypts credentials for EventSink_GameLogin only.
 *      If EventSink_GameLogin detects encrypted credentials, it marks the account as using a valid client,
 *          otherwise it marks the client as using an invalid client.
 *      Currently CoreAI.ForceGMNCUO is set to false. So it's acceptable to use an invalid client. But in
 *          future server versions, we will warn/reject such logins from an invalid client.
 *  5/1/23, Yoar (CharDeleteUnrestricted)
 *      Added CoreAI.CharDeleteUnrestricted: Toggle on/off char deletion restriction
 *  4/30/23: Adam (EventSink_GameLogin)
 *      We no longer block players from entering the shard if they have break a login rule, like too many IP addresses.
 *      We now take them out of society by tossing their cheating asses in Prison.
 *      See also: EventSink_Connected in PlayerMobile.cs
 * 12/16/22, Adam (EventSink_AccountLogin)
 *  We no longer make IP checks or hardware checks if this is the LoginServer, that responsibility is passed to
 *      the game-server.
 *  Also, add a [AccountAccess command for selectively turning off/on one or more of the following for testing:
 *      AccountConcurrentIPLimiter, AccountTotalIPLimiter, and AccountHardwareLimiter
 *      PS. the only way to test the AccountHardwareLimiter is to turn off the AccountTotalIPLimiter.
 * 9/6/22, Yoar
 *      Added publish-based Starting City patch in Initialize.
 *      But, I've disabled the publish checks so that all starting cities are in Felucca.
 * 9/4/22, Yoar (WorldZone)
 *      Added support for world zone starting cities.
 * 8/5/22, Adam
 *      When AccountLimiter declines a login, show the connected accounts via DisplayAccountsByIP()
 *      Add the notion of GrandfatheredAccount(bool newAccount). This is because we don't support IPExceptioms any longer
 *      and an existing account was likely allowed via IPException, or admin created.
 * 8/5/22, Adam
 *      Overhaul of EventSink_AccountLogin and EventSink_GameLogin
 *      - EventSink_AccountLogin and EventSink_GameLogin are now nearly identical.
 *      - CreateAccount() no longer adds the account. the temp account must pass all tests before it is added
 *          a. Remove all access checks, those are now post (temp) account creation.
 *      - New: CreateAccountFromDB() Works like CreateAccount(), but creates an account from database knowledge
 *      - New: AccountLimiter.Verify() checks number if accounts associated with the IP
 *      - Fix e.Accepted usage (was setting accepted to true if an account was created, but before any of the validity checks.)
 *      - Add SyncAccountToDB(). When an new account is created via CreateAccount, it is immediately synced to the database
 * 8/4/22, Adam (HardwareLimiter.Verify(acct))
 *  The problem with our system is that via seeding, we create more accounts than what should be allowed. 1 on Mortalis/SP for instance.
 *  Until I can figure this out, I ban the second account attempting to login when the HardwareLimiter detects as second account.
 *  The banning is necessary since HardwareLimiter only blocks concurrent connections, but doesn't stop players from simply switching accounts.
 *  I.e., they will have 3 accounts on Mortalis to chose from.
 *  This is still in development.
 * 8/1/22, Yoar
 *      Now ignoring staff accounts when checking the IP-based account limit.
 * 8/1/22, Adam
 *      Yellow text for networking messages
 * 7/30/22, Adam
 *      Weave in new HardwareLimiter checks. 
 *      Prevents players from connecting more than one account to a particular machine.
 *      (Wobbly. The client only randomly sends Spy On Client (packet 0xD9), so we don't always have HardwareInfo. I.e., won't always catch it.)
 *      New logging
 * 1/13/22, Pix
 *  Now set acct.LastLogin during game login.
 * 8/16/21, adam
 *  New implementation moves back to the far simpler RunUO model with some of our IPNotify logic for telling 2nd, 3rd, etc accounts why Nth account couldn't connect.
 *      New model in a nutshell: 
 *      1) Each shard manages it's own account database independently.
 *      2) There is no attempt to sync passwords across shards.
 *      3) The LoginServer does only that - field requests for a game-server, do some core tests of name and password fitness, then passes the real work to the game-server.
 * 3/15/16, Adam
 *		Reverse changes of 2/8/08
 *		Turn IPException code back on. This is because the IPException logic is per IP whereas the MaxAccountsPerIP
 *			functionality is global.
 *	12/15/10, adam
 *		Add a new LoginServer variable to the core used in determining when we are a login server and when we are not.
 *		This saves us from trying to explicitly name all the servers we may be running (see AccountHandler.cs)
 *		History: I was getting complaints that auto creation on Test Center wasn't working. This was because we were incorrectly
 *		enumerating the non-primary (login) servers. This new varible should alleviate that problem.
 *  11/13/10, Adam
 *      Re allow AutoAccountCreation untill we enter ALPHA
 *	11/26/08, Adam
 *		During login, call IPLimiter.Notify() to tell other accounts on this IP why they cannot connect.
 *	11/25/08, Adam
 *		In Account logon and game logon call IPLimiter.IPStillTooHot() to see if the player is attempting a hot-swap of accounts.
 *		Account hot-swap prevention is only really effective if IPLimiter.MaxAddresses == 1
 *	2/18/08, Adam
 *		- We now allow 3 accounts per household - IPException logic no longer needed
 *		- Make MaxAccountsPerIP a console value (move to CoreAI)
 *  2/26/07, Adam
 *      Check for parameters > 1 passed to [password reset and handle with usage message
 *	11/06/06, Pix
 *		Removed AccountActivation stuff, because now it is handled without changing passwords
 *		purely with the profile gump.
 *	2/27/06, Pix
 *		Changes for IPLimiter.
 *	9/15/05, Adam
 *		convert MaxAccountsPerIP to a property so that it can be set
 *		in TestCenter.cs
 *	7/25/05, Pix
 *		Fixed to check ALL the IPs an account has logged in from, not just the first one.
 *	7/7/05, Pix
 *		Added Audit Email for Auto-account creation.
 * 6/28/05, Pix
 *	Reworked Password_OnCommand to use 'reset password' functionality gump.
 * 6/15/05, Pix
 *	Enabled AutoAccount, which now uses new IPException class.
 *  Removed functionality of [password command - now just directs player to [profile
 * 2/23/05, Pix
 *	Now if you delete a houseowner, the house will transfer to the first available 
 *	character on the account.  If you try to delete a houseowner and it is the last
 *	character on the account, the delete will fail.
 * 7/15/04, Pix
 *	Removed IP check from password changing with [password
 *	Logged IP when changing password.
 * 6/5/04, Pix
 * Merged in 1.0RC0 code.
 * 5/17/04, Pixie 
 *	Enabled password changing.
 *	Added use of new PasswordGump.
 *	User must enter current password.
 * 4/5/04 code changes by Pixie
 *	Changed the number of accounts per IP to 2 for ALPHA testing.
 * 4/24/04 code changes by adam
 *	Change AutoAccountCreation from true to false
 */

using Server.Accounting;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using static Server.Accounting.AccountIPManager;

namespace Server.Misc
{
    public class AccountHandler
    {
        private static bool AutoAccountCreation = true;
        public static bool RestrictDeletion
        {
            get { return !CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.CharDeleteUnrestricted); }
        }
        private static TimeSpan DeleteDelay = TimeSpan.FromDays(7.0);
        public static bool ProtectPasswords = true;
        private static AccessLevel m_LockdownLevel;
        //private static Dictionary<IPAddress,string> AccountLoginLookup = new Dictionary<IPAddress,string>();
        public static AccessLevel LockdownLevel
        {
            get { return m_LockdownLevel; }
            set { m_LockdownLevel = value; }
        }
        public static int MaxAccountsPerIP
        {
            get { return CoreAI.MaxAccountsPerIP; }
            set { CoreAI.MaxAccountsPerIP = value; }
        }
        private static CityInfo[] StartingCities = new CityInfo[]
            {
                new CityInfo( "Yew",        "The Empath Abbey",         633,    858,    0  ),
                new CityInfo( "Minoc",      "The Barnacle",             2476,   413,    15 ),
                new CityInfo( "Britain",    "Sweet Dreams Inn",         1496,   1628,   10 ),
                new CityInfo( "Moonglow",   "The Scholars Inn",         4408,   1168,   0  ),
                new CityInfo( "Trinsic",    "The Traveler's Inn",       1845,   2745,   0  ),
                new CityInfo( "Magincia",   "The Great Horns Tavern",   3734,   2222,   20 ),
                new CityInfo( "Jhelom",     "The Mercenary Inn",        1374,   3826,   0  ),
                new CityInfo( "Skara Brae", "The Falconer's Inn",       618,    2234,   0  ),
                new CityInfo( "Vesper",     "The Ironwood Inn",         2771,   976,    0  )
            };
        private static readonly CityInfo m_OldHaven = new CityInfo("Haven", "Buckler's Hideaway", 3667, 2625, 0);
        private static readonly CityInfo m_NewHaven = new CityInfo("Haven", "Uzeraan's Mansion", 3618, 2591, 0);
        public static CityInfo[] GetStartingCities()
        {
            #region World Zone
            if (WorldZone.ActiveZone != null)
                return WorldZone.ActiveZone.StartingCities;
            #endregion

            return StartingCities;
        }
        private static bool PasswordCommandEnabled = true;
        public static void Initialize()
        {
            EventSink.DeleteRequest += new DeleteRequestEventHandler(EventSink_DeleteRequest);
            EventSink.AccountLogin += new AccountLoginEventHandler(EventSink_AccountLogin);
            EventSink.GameLogin += new GameLoginEventHandler(EventSink_GameLogin);

            if (PasswordCommandEnabled)
                Server.CommandSystem.Register("Password", AccessLevel.Player, new CommandEventHandler(Password_OnCommand));

            #region Patch Starting Cities
            /*
             * Changes to starting locations:
             * - Publish 44 (ML) / CC 6.0.0.0: Always use New Haven, removed old starting quests
             * - Publish 70 (HS) / CC 7.0.13.0: Re-introduced starting city selection
             *
             * Notes:
             * - Clients prior to 6.0.0.0 have a hardcoded CityInfo[] that may not match ours
             * - Clients between 6.0.0.0 and 7.0.12.1 always send cityIndex 0
             * - Client version is not always available at this point (for example in 6.0.0.0), but in newer clients (7.0?) it is
             */
#if false
            if (PublishInfo.Publish >= 5) // introduction of trammel
            {
                if (PublishInfo.Publish >= 44 && PublishInfo.Publish < 70)
                {
                    // between ML and HS everyone started in New Haven
                    StartingCities = new CityInfo[] { m_NewHaven };
                }
                else
                {
                    System.Collections.Generic.List<CityInfo> startingCities = new System.Collections.Generic.List<CityInfo>(StartingCities);

                    if (PublishInfo.Publish >= 43) // emergence of New Haven
                        startingCities.Insert(0, m_NewHaven);
                    else
                        startingCities.Insert(0, m_OldHaven);

                    if (PublishInfo.Publish >= 60) // destruction of Magincia
                    {
                        for (int i = startingCities.Count - 1; i >= 0; i--)
                        {
                            if (startingCities[i].City == "Magincia")
                            {
                                startingCities.RemoveAt(i);
                                break;
                            }
                        }
                    }

                    StartingCities = startingCities.ToArray();
                }
            }
            else
#endif
            {
                for (int i = 0; i < StartingCities.Length; i++)
                    StartingCities[i].Map = Map.Felucca;
            }
            #endregion

            /* When we enter ALPHA we will lock the server down, but for now we want players to log in and help us identify
			 * those odd bits we need to fix.
			if (Core.UOSP) //Until Siege gets out of the closed phase, keep AutoAccountCreation false
			{
				AutoAccountCreation = false;
			}*/

            Server.CommandSystem.Register("AccountAccess", AccessLevel.Administrator, new CommandEventHandler(AccountAccess_OnCommand));
        }
        [Usage("AccountAccess AccountLimiter|IPLimiter true|false")]
        [Description("Disable certain login checks to check others.")]
        public static void AccountAccess_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            bool bSendUsage = false;
            bool result = false;
            if (e.Length != 2)
                bSendUsage = true;
            else
            {
                if (e.Arguments[0].ToLower() == "AccountConcurrentIPLimiter".ToLower() && GetBool(e.Arguments[1].ToLower(), out result))
                {
                    AccountConcurrentIPLimiter.Enabled = result;
                    from.SendMessage("AccountConcurrentIPLimiter {0}", AccountConcurrentIPLimiter.Enabled ? "enabled" : "disabled");
                }
                else if (e.Arguments[0].ToLower() == "AccountTotalIPLimiter".ToLower() && GetBool(e.Arguments[1].ToLower(), out result))
                {
                    AccountTotalIPLimiter.Enabled = result;
                    from.SendMessage("AccountTotalIPLimiter {0}", AccountTotalIPLimiter.Enabled ? "enabled" : "disabled");
                }
                else if (e.Arguments[0].ToLower() == "AccountHardwareLimiter".ToLower() && GetBool(e.Arguments[1].ToLower(), out result))
                {
                    AccountHardwareLimiter.Enabled = result;
                    from.SendMessage("AccountHardwareLimiter {0}", AccountHardwareLimiter.Enabled ? "enabled" : "disabled");
                }
                else if (e.Arguments[0].ToLower() == "All".ToLower() && GetBool(e.Arguments[1].ToLower(), out result))
                {
                    AccountConcurrentIPLimiter.Enabled = result;
                    from.SendMessage("AccountConcurrentIPLimiter {0}", AccountConcurrentIPLimiter.Enabled ? "enabled" : "disabled");
                    AccountTotalIPLimiter.Enabled = result;
                    from.SendMessage("AccountTotalIPLimiter {0}", AccountTotalIPLimiter.Enabled ? "enabled" : "disabled");
                    AccountHardwareLimiter.Enabled = result;
                    from.SendMessage("AccountHardwareLimiter {0}", AccountHardwareLimiter.Enabled ? "enabled" : "disabled");
                }
                else
                    bSendUsage = true;
            }

            if (bSendUsage)
            {
                from.SendMessage("AccountAccess AccountConcurrentIPLimiter|AccountTotalIPLimiter true|false");
                from.SendMessage("AccountAccess AccountHardwareLimiter true|false");
            }
        }
        private static bool GetBool(string input, out bool result)
        {
            result = false;
            return bool.TryParse(input, out result);
        }
        [Usage("Password reset")]
        [Description("Brings up a gump for reset password.")]
        public static void Password_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            bool bSendUsage = false;

            if (e.Length == 0)
            {
                bSendUsage = true;
            }
            else if (e.Length == 1)
            {
                string keyword = e.GetString(0);

                if (keyword.Length > 0)
                {
                    if (keyword.ToLower() == "reset")
                    {
                        from.SendGump(new Server.Gumps.PasswordGump(from));
                    }
                    else
                    {
                        bSendUsage = true;
                    }
                }
                else
                {
                    bSendUsage = true;
                }
            }
            else
                bSendUsage = true;

            if (bSendUsage)
            {
                from.SendMessage("To change your password, use the {0}profile command.", Server.CommandSystem.CommandPrefix);
                from.SendMessage("To reset the password for a friend's account, use {0}password reset", Server.CommandSystem.CommandPrefix);
            }
        }
        private static void EventSink_DeleteRequest(DeleteRequestEventArgs e)
        {
            NetState state = e.State;
            int index = e.Index;

            Account acct = state.Account as Account;

            if (acct == null)
            {
                state.Dispose();
            }
            else if (index < 0 || index >= 5)
            {
                state.Send(new DeleteResult(DeleteResultType.BadRequest));
                state.Send(new CharacterListUpdate(acct));
            }
            else if (acct.GetFlag(Account.IPFlags.IsTorExitNode, Account.IPFlagsState.Permanent))
            {   // cheaters that try to enter our shard via a Tor Exit Node don't get to delete their character and start over.
                state.Send(new DeleteResult(DeleteResultType.BadRequest));
                state.Send(new CharacterListUpdate(acct));
            }
            else
            {
                Mobile m = acct[index];

                if (m == null)
                {
                    state.Send(new DeleteResult(DeleteResultType.CharNotExist));
                    state.Send(new CharacterListUpdate(acct));
                }
                else if (m.NetState != null)
                {
                    state.Send(new DeleteResult(DeleteResultType.CharBeingPlayed));
                    state.Send(new CharacterListUpdate(acct));
                }
                else if (RestrictDeletion && DateTime.UtcNow < (m.Created + DeleteDelay))
                {
                    state.Send(new DeleteResult(DeleteResultType.CharTooYoung));
                    state.Send(new CharacterListUpdate(acct));
                }
                else
                {
                    bool bDelete = true;

                    if (m is Server.Mobiles.PlayerMobile)
                    {
                        Server.Mobiles.PlayerMobile pm = (Server.Mobiles.PlayerMobile)m;
                        System.Collections.ArrayList houses = Multis.BaseHouse.GetHouses(pm);
                        if (houses.Count > 0)
                        {
                            if (acct.Count > 1)
                            {
                                Mobile newOwner = null;
                                //find a non-deleted, non-null character on the account
                                for (int i = 0; i < acct.Count; i++)
                                {
                                    if (index != i)
                                    {
                                        if (acct[i] != null)
                                        {
                                            if (!acct[i].Deleted)
                                            {
                                                newOwner = acct[i];
                                            }
                                        }
                                    }
                                }

                                if (newOwner == null) //sanity check, should never happen
                                {
                                    System.Console.WriteLine("Sanity check failed: newOwner == null!");
                                    bDelete = false;
                                    state.Send(new DeleteResult(DeleteResultType.BadRequest));
                                }
                                else
                                {
                                    for (int i = 0; i < houses.Count; ++i)
                                    {
                                        if (houses[i] is Server.Multis.BaseHouse)
                                        {
                                            Server.Multis.BaseHouse house = (Server.Multis.BaseHouse)houses[i];
                                            if (house != null)
                                            {
                                                if (house.Owner == m) //another sanity check - house.Owner should always be m at this point!
                                                {
                                                    //swap to new owner
                                                    house.Owner = newOwner;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //If account only has one character, then refuse to delete the houseowner
                                bDelete = false;
                                state.Send(new DeleteResult(DeleteResultType.BadRequest));
                            }
                        }
                    }

                    if (bDelete)
                    {
                        Utility.Monitor.WriteLine(string.Format("Client: {0}: Deleting character {1} (name:{3}) (0x{2:X})", state, index, m.Serial.Value, m.Name), ConsoleColor.Yellow);
                        m.Delete();
                    }

                    state.Send(new CharacterListUpdate(acct));
                }
            }
        }
        private static Account CreateAccount(NetState state, string un, string pw)
        {
            if (un.Length == 0 || pw.Length == 0)
                return null;

            bool isSafe = true;

            for (int i = 0; isSafe && i < un.Length; ++i)
                isSafe = (un[i] >= 0x20 && un[i] < 0x80);

            for (int i = 0; isSafe && i < pw.Length; ++i)
                isSafe = (pw[i] >= 0x20 && pw[i] < 0x80);

            if (!isSafe)
                return null;

            Account acct = new Account(un, pw);
            if (Core.UseLoginDB)
            {
                AccountsDatabase.SaveAccount(acct);
                AccountsDBEntry? dbAccount = AccountsDatabase.GetAccount(un);
                if (dbAccount != null)
                {
                    acct.SetRawCredentials(dbAccount.Value.CryptPassword, dbAccount.Value.PlainPassword, dbAccount.Value.ResetPassword);
                    acct.HardwareHashRaw = dbAccount.Value.HardwareHash;
                }
            }

            return acct;
        }
        private static Account CreateAccountFromDB(string un)
        {
            if (Core.UseLoginDB)
            {
                AccountsDBEntry? dbAccount = AccountsDatabase.GetAccount(un);
                if (dbAccount != null)
                {
                    Account acct = new Account(un);
                    acct.SetRawCredentials(dbAccount.Value.CryptPassword, dbAccount.Value.PlainPassword, dbAccount.Value.ResetPassword);
                    acct.HardwareHashRaw = dbAccount.Value.HardwareHash;
                    return acct;
                }
            }

            return null;
        }
        public static void SyncAccountToFromDB(Account acct)
        {
            SyncAccountToFromDB(null, acct);
        }
        private static void SyncAccountToFromDB(IPAddress ip, Account acct)
        {
            if (acct != null && Core.UseLoginDB)
            {
                AccountsDBEntry? dbAccount = AccountsDatabase.GetAccount(acct.Username);
                if (dbAccount == null)
                    AccountsDatabase.SaveAccount(acct);

                dbAccount = AccountsDatabase.GetAccount(acct.Username);
                if (dbAccount != null)
                {
                    acct.SetRawCredentials(dbAccount.Value.CryptPassword, dbAccount.Value.PlainPassword, dbAccount.Value.ResetPassword);
                    acct.HardwareHashRaw = dbAccount.Value.HardwareHash;
                    acct.SetHardwareHashAcquired(acct.HardwareHashRaw);
                }
                else
                {
                    if (ip != null)
                        Utility.Monitor.WriteLine("Login: {0}: Unable to sync local account '{1}' to database", ConsoleColor.Red, ip, acct.Username);
                    else
                        Utility.Monitor.WriteLine("Login: Unable to sync local account '{0}' to database", ConsoleColor.Red, acct.Username);
                }

                // sanity check for corrupt or missing, or outdated database.
                if (dbAccount != null)
                {
                    if (acct.HardwareHashRaw != 0)              // we have a local shard HardwareHash for this account
                        if (dbAccount.Value.HardwareHash == 0)  // but our database is unaware of this hash
                        {
                            Utility.Monitor.WriteLine(string.Format("Error: corrupt, missing, or outdated database."), ConsoleColor.Red);
                            Utility.Monitor.WriteLine(string.Format("we have a local shard HardwareHash for account {0}", acct), ConsoleColor.Red);
                            Utility.Monitor.WriteLine(string.Format("but our database is unaware of this hash"), ConsoleColor.Red);
                            Utility.Monitor.WriteLine(string.Format("The database will need to be rebuilt from the local accounts.xml"), ConsoleColor.Red);
                            // Seeding won't work, we will need a new reverse seeding mechanism: accounts.xml ==> database
                        }
                }
            }
        }
        private static void DisplayAccountsByIP(Accounting.Account acct, IPAddress ip)
        {
            foreach (Accounting.Account a in Accounting.Accounts.Table.Values)
            {
                foreach (IPAddress loginIP in a.LoginIPs)
                {
                    if (IPAddress.Equals(loginIP, ip))
                        Utility.Monitor.WriteLine(string.Format(
                            "{0}: {1} ({2})", Core.RuleSets.LoginServerRules() ? "LoginServer" : Core.Server,
                            a.Username,
                            a.AccessLevel > AccessLevel.Player ? "staff" : "player"
                            ), ConsoleColor.Red);
                }
            }

            return;
        }
        private static bool GrandfatheredAccount(bool grandfathered)
        {
            if (Core.RuleSets.SiegeRules() || Core.RuleSets.MortalisRules())
            {
                if (grandfathered)
                    Utility.Monitor.WriteLine(string.Format("no grandfathering on Siege or Mortalis"), ConsoleColor.Red);
                return false;
            }

            // since we can't use IPExceptions, and until we resolve this, we will grandfather in all existing accounts
            //  since they were probably previously allowed via IP exception or Admin created.
            if (grandfathered)
                Utility.Monitor.WriteLine(string.Format("Grandfathered Account detected"), ConsoleColor.Red);

            return grandfathered;
        }
        private static string GetBestUNMatch(Account acct)
        {   // unfortunately, SQLite database is case-sensitive.
            // Example: Create the "Adam Ant" account. Next login is with "adam ant". SQLite won't recognize "adam ant"
            // We will look for the best match here
            string cryptPassword = acct.CryptPassword;

            // first see if the remote database matches what's in our local database
            AccountsDBEntry? dbAccount = AccountsDatabase.GetAccount(acct.Username);
            if (dbAccount != null && dbAccount.Value.CryptPassword == cryptPassword)
                return acct.Username;

            // okay, try lowercase
            dbAccount = AccountsDatabase.GetAccount(acct.Username.ToLower());
            if (dbAccount != null && dbAccount.Value.CryptPassword == cryptPassword)
                return acct.Username.ToLower();

            // okay, try uppercase (unusual)
            dbAccount = AccountsDatabase.GetAccount(acct.Username.ToUpper());
            if (dbAccount != null && dbAccount.Value.CryptPassword == cryptPassword)
                return acct.Username.ToUpper();

            // okay, try camelcase 
            dbAccount = AccountsDatabase.GetAccount(Utility.CamelCase(acct.Username.ToLower()));
            if (dbAccount != null && dbAccount.Value.CryptPassword == cryptPassword)
                return Utility.CamelCase(acct.Username.ToLower());

            return acct.Username;
        }
        public static void EventSink_AccountLogin(AccountLoginEventArgs e)
        {
            string un = e.Username;
            string pw = e.Password;
            e.Accepted = false;
            bool newShardAcct = false;
            IPAddress ip = e.State.Address;
            e.Accepted = false;
            int count = 0;
            bool grandfathered = false;
            bool loginServer = Core.RuleSets.LoginServer;

            // disallow the name "system" for any account since we use this to distinguish actions by the SYSTEM and a player account
            //  This account will transparently become "metsys"
            if (un.Equals("system", StringComparison.OrdinalIgnoreCase))
                un = Utility.Reverse(un);

            // does accounts.xml have an entry for this acct?
            Account acct = Accounts.GetAccount(un);

            // unfortunately, SQLite database is case-sensitive. We will look for the best match
            if (acct != null && Core.UseLoginDB)
                un = GetBestUNMatch(acct);

            // if we have a preexisting SHARD account, and this is not MO or SP, allow it since we don't have working
            //  shard specific IPExceptions
            grandfathered = GrandfatheredAccount(acct != null);

            if (acct != null && Core.UseLoginDB)
                SyncAccountToFromDB(ip, acct);

            // see if we can fetch an account from the login DB
            if (acct == null && Core.UseLoginDB)
            {
                e.State.Account = acct = CreateAccountFromDB(un);
                bool ok = acct == null ? false : acct.CheckAccess(e.State);
                newShardAcct = ok;

                if (acct != null)
                {
                    if (!ok)
                    {
                        Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Bad Comm '{1}'", e.State, un));
                        e.RejectReason = ALRReason.BadComm;
                        return;
                    }
                }
            }

            // no account in login DB, see if we can create one locally
            if (acct == null)
            {
                if (AutoAccountCreation && un.Trim().Length > 0)
                {
                    e.State.Account = acct = CreateAccount(e.State, un, pw);
                    bool ok = acct == null ? false : acct.CheckAccess(e.State);
                    newShardAcct = ok;

                    if (!ok)
                    {
                        Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Bad Comm '{1}'", e.State, un));
                        e.RejectReason = ALRReason.BadComm;
                    }
                }
                else
                {
                    Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Invalid username '{1}'", e.State, un));
                    e.RejectReason = ALRReason.Invalid;
                }
            }

            if (acct == null)
            {
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Failed '{1}': {2}", e.State, un, e.RejectReason));
            }
            else if (!AccountConcurrentIPLimiter.IsOk(acct, ip))
            {
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Past IP limit threshold", e.State));
                // tell other accounts on this IP what's going on
                AccountConcurrentIPLimiter.Notify(e.State.Address);
                e.RejectReason = ALRReason.InUse;
            }
            // don't check login server here as it's likely to have more accounts than allowed on any one shard
            else if (!loginServer && !grandfathered && !AccountTotalIPLimiter.IsOk(acct, ip, ref count))
            {
#if false
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Account '{1}' not created, ip already has {2} account{3}.", e.State, un, count, count == 1 ? "" : "s"));
#else
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Account '{1}' blackholed, ip already has {2} account{3}.", e.State, un, count, count == 1 ? "" : "s"));
#endif
                // tell other accounts on this IP what's going on
                AccountTotalIPLimiter.Notify(acct, ip, count);
                // write the accounts to the console
                DisplayAccountsByIP(acct, ip);
                e.RejectReason = ALRReason.Blocked;
            }
            // don't check login server here as it's likely to have more accounts than allowed on any one shard
            else if (!loginServer && !grandfathered && !AccountHardwareLimiter.IsOk(acct))
            {   // full report already issued (IsOk()). See BlockedConnection.log
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Past machine limit threshold", e.State));

                // tell other accounts on this machine what's going on
                AccountHardwareLimiter.Notify(acct);
                e.RejectReason = ALRReason.Blocked;
            }
            // don't check login server here as it needs to allow access to several shards
            else if (!loginServer && AccountConcurrentIPLimiter.IPStillHot(acct, e.State.Address))
            {
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Access denied for '{1}'. IP too hot", e.State, un));
                e.RejectReason = ALRReason.InUse;
            }
            else if (!acct.HasAccess(e.State))
            {
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Access denied for '{1}'", e.State, un));
                e.RejectReason = (m_LockdownLevel > AccessLevel.Player ? ALRReason.BadComm : ALRReason.BadPass);
            }
            else if (!acct.CheckPassword(pw))
            {
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Invalid password for '{1}'", e.State, un));
                e.RejectReason = ALRReason.BadPass;
            }
            else
            {   // all good. Setup the account, add the account to the Shard, and send new account email if appropriate
                Utility.Monitor.WriteLine(string.Format("Login: {0}: Valid credentials for '{1}'", e.State, un), ConsoleColor.Yellow);
                e.State.Account = acct;
                e.Accepted = true;

                acct.LogAccess(e.State);
                acct.LastLogin = DateTime.UtcNow;

                if (newShardAcct)
                    Accounts.Table[un] = acct;

                if (Core.RuleSets.LoginServerRules())
                {
                    Utility.Monitor.WriteLine(string.Format("Login: {0}: LoginServer passing account '{1}' to game-server.", e.State, un), ConsoleColor.Yellow);
                }
                else if (newShardAcct)
                {
                    //Audit email
                    try
                    {
                        string regSubject = "Account Created Dynamically";
                        string regBody = "Account created dynamically, auto-account.\n";
                        regBody += "Info: \n";
                        regBody += "\n";
                        regBody += "Account: " + un + "\n";
                        regBody += "IP: " + ip + "\n";
                        regBody += "Password: " + pw + "\n";
                        regBody += "\n";
                        Emailer mail = new Emailer();
                        mail.SendEmail(Environment.GetEnvironmentVariable("AI.EMAIL.ACCOUNTING"), regSubject, regBody, false);
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                    Console.WriteLine("Login: {0}: Creating new account '{1}'", e.State, un);
                }
            }

            if (!e.Accepted)
                AccountAttackLimiter.RegisterInvalidAccess(e.State);
        }

        public static void EventSink_GameLogin(GameLoginEventArgs e)
        {
            // cleanup encrypted username and password from an early version of client security
            UnFuck(e);

            if (!World.IsSystemAcct(e.State.Address) && Core.GeoIPCheck())
            {
                AccountIPManager.UserIPRequest.Enqueue(new UserIPInfo(e.State.Address.ToString(), e.Username));

                #region Queuing Test
                //UserIPRequest.Enqueue(new UserIPInfo("99.45.119.245"/*e.State.Address.ToString()*/, e.Username));
                //UserIPRequest.Enqueue(new UserIPInfo("99.45.119.245"/*e.State.Address.ToString()*/, e.Username));
                //UserIPRequest.Enqueue(new UserIPInfo("99.45.119.245"/*e.State.Address.ToString()*/, e.Username));
                #endregion Queuing Test

                // database access is slow, so we'll do it in the background
                Task.Factory.StartNew(() => AcquireIpInfo());

                // white list this client if need be
                ClientException.ClientExceptionWhiteList(e.Username, e.State);
            }

            // we handle "valid_client" in a new way - we no longer block login, but rather disconnect them after login
            Account acct = null;
            bool valid_client = true;
            GameLogin_Internal(e, ref acct, valid_client);
            if (acct != null)
                acct.SetFlag(Account.AccountFlag.InvalidClient, false);
        }
        private static void GameLogin_Internal(GameLoginEventArgs e, ref Account acct, bool valid_client)
        {
            string un = e.Username;
            string pw = e.Password;
            // disallow the name "system" for any account since we use this to distinguish actions by the SYSTEM and a player account
            //  This account will transparently become "metsys"
            if (un.Equals("system", StringComparison.OrdinalIgnoreCase))
                un = Utility.Reverse(un);
            e.Accepted = true;
            bool newShardAcct = false;
            IPAddress ip = e.State.Address;
            int count = 0;
            bool grandfathered = false;
            bool loginServer = Core.RuleSets.LoginServer;

            // does accounts.xml have an entry for this acct?
            acct = Accounts.GetAccount(un);

            // unfortunately, SQLite database is case-sensitive. We will look for the best match
            if (acct != null && Core.UseLoginDB)
                un = GetBestUNMatch(acct);

            // if we have a preexisting SHARD account, and this is NOT MO or SP, allow it since we don't have working
            //  shard specific IPExceptions
            grandfathered = GrandfatheredAccount(acct != null);

            if (acct != null && Core.UseLoginDB)
                SyncAccountToFromDB(ip, acct);

            // see if we can fetch an account from the login DB
            if (acct == null && Core.UseLoginDB)
            {
                e.State.Account = acct = CreateAccountFromDB(un);
                bool ok = acct == null ? false : acct.CheckAccess(e.State);
                newShardAcct = ok;

                if (acct != null)
                {
                    if (!ok)
                    {
                        Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Bad Comm '{1}'", e.State, un));
                        e.RejectReason = ALRReason.BadComm;
                        return;
                    }
                }
            }

            // no account in login DB, see if we can create one locally
            if (acct == null)
            {
                if (AutoAccountCreation && un.Trim().Length > 0)
                {
                    e.State.Account = acct = CreateAccount(e.State, un, pw);
                    e.Accepted = acct == null ? false : acct.CheckAccess(e.State);
                    newShardAcct = e.Accepted;

                    if (!e.Accepted)
                    {
                        Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Bad Comm '{1}'", e.State, un));
                        e.RejectReason = ALRReason.BadComm;
                    }
                }
                else
                {
                    Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Invalid username '{1}'", e.State, un));
                    e.RejectReason = ALRReason.Invalid;
                }
            }

            bool invalidClient = false;
            bool accountConcurrentIPLimiter = false;
            bool accountTotalIPLimiter = false;
            bool accountHardwareLimiter = false;
            bool IPStillHot = false;
            bool hasAccess = false;
            bool checkPassword = false;
            bool banned = false;
            List<Account.AccountInfraction> offenses = new();
            if (acct != null)
            {
                acct.InfractionStatus = Account.AccountInfraction.none;
                #region Obsolete pre-login checks, now checked after login
                // 8/26/2023, Adam: We no longer use the invalid client check at login, but rather later after login
                //  during a packet exchange
                // TorExitNode is no longer processed here, but will be determined after login
                invalidClient = !valid_client && CoreAI.ForceGMNCUO;
                if (invalidClient) offenses.Add(Account.AccountInfraction.TorExitNode);
                #endregion Obsolete pre-login checks, now checked after login
                accountConcurrentIPLimiter = AccountConcurrentIPLimiter.IsOk(acct, ip, filter_mortal_inmates: true);
                if (!accountConcurrentIPLimiter) offenses.Add(Account.AccountInfraction.concurrentIPLimiter);
                accountTotalIPLimiter = AccountTotalIPLimiter.IsOk(acct, ip, ref count);
                if (!accountTotalIPLimiter) offenses.Add(Account.AccountInfraction.totalIPLimiter);
                accountHardwareLimiter = AccountHardwareLimiter.IsOk(acct);
                if (!accountHardwareLimiter) offenses.Add(Account.AccountInfraction.totalHardwareLimiter);
                IPStillHot = AccountConcurrentIPLimiter.IPStillHot(acct, e.State.Address);
                if (IPStillHot) offenses.Add(Account.AccountInfraction.IPStillHot);
                hasAccess = acct.HasAccess(e.State);
                if (!hasAccess) offenses.Add(Account.AccountInfraction.hasAccess);
                checkPassword = acct.CheckPassword(pw);
                if (!checkPassword) offenses.Add(Account.AccountInfraction.checkPassword);
                banned = acct.Banned;
                if (banned) offenses.Add(Account.AccountInfraction.banned);
            }

            if (acct == null)
            {
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Failed '{1}': {2}", e.State, un, e.RejectReason));
            }
            #region Obsolete pre-login checks, now checked after login
            // 8/26/2023, Adam: We no longer use the invalid client check at login, but rather later after login
            //  during a packet exchange
            // TorExitNode is no longer processed here, but will be determined after login
            else if (invalidClient)
            {
                acct.InfractionStatus = Account.AccountInfraction.TorExitNode;
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Access denied for '{1}'. Invalid Client", e.State, un));
                //e.Accepted = false;
            }
            #endregion Obsolete pre-login checks, now checked after login
            else if (!accountConcurrentIPLimiter)
            {
                acct.InfractionStatus = Account.AccountInfraction.concurrentIPLimiter;
                e.RejectReason = ALRReason.InUse;
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Past IP limit threshold", e.State));
                // tell other accounts on this IP what's going on
                AccountConcurrentIPLimiter.Notify(e.State.Address);
            }
            else if (!grandfathered && !accountTotalIPLimiter)
            {
                acct.InfractionStatus = Account.AccountInfraction.totalIPLimiter;
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Account '{1}' blackholed, ip already has {2} account{3}.", e.State, un, count, count == 1 ? "" : "s"));
                // tell other accounts on this IP what's going on
                AccountTotalIPLimiter.Notify(acct, ip, count);
                e.RejectReason = ALRReason.Blocked;
            }
            else if (!grandfathered && !accountHardwareLimiter)
            {
                acct.InfractionStatus = Account.AccountInfraction.totalHardwareLimiter;
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Past machine limit threshold", e.State));
                // tell other accounts on this machine what's going on
                AccountHardwareLimiter.Notify(acct);
                e.RejectReason = ALRReason.Blocked;
                e.Accepted = false;
            }
            else if (IPStillHot)
            {
                acct.InfractionStatus = Account.AccountInfraction.IPStillHot;
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Access denied for '{1}'. IP too hot", e.State, un));
                //e.Accepted = false;
            }
            else if (!hasAccess)
            {
                acct.InfractionStatus = Account.AccountInfraction.hasAccess;
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Access denied for '{1}'", e.State, un));
                e.RejectReason = (m_LockdownLevel > AccessLevel.Player ? ALRReason.BadComm : ALRReason.BadPass);
                e.Accepted = false;
            }
            else if (!checkPassword)
            {
                acct.InfractionStatus = Account.AccountInfraction.checkPassword;
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Invalid password for '{1}'", e.State, un));
                e.RejectReason = ALRReason.BadPass;
                e.Accepted = false;
            }
            else if (banned)
            {
                acct.InfractionStatus = Account.AccountInfraction.banned;
                Server.Diagnostics.LogHelper.LogBlockedConnection(string.Format("Login: {0}: Banned account '{1}'", e.State, un));
                e.RejectReason = ALRReason.Blocked;
                e.Accepted = false;
            }

            // OffensePrioritization
            acct.InfractionStatus = OffensePrioritization(acct, offenses);

            // these cases here are the exceptions. We allow the player to hang at "Creating Character" screen
            bool BlockedLogin =
                acct.InfractionStatus == Account.AccountInfraction.hasAccess &&
                acct.InfractionStatus == Account.AccountInfraction.checkPassword &&
                acct.InfractionStatus == Account.AccountInfraction.banned;

            bool ContinueLogin = !CoreAI.JailLogin ? e.Accepted == true : !BlockedLogin;

            // If we are the login server, then we will process failures during game login differently
            //  This is because failures at this level just hang the client at "Connection"
            //  Instead we will send them on a Prison adventure as Mortals.
            //  Their failed state will be picked up in PlayerMobile EventSink_Connected()
            if (acct != null && ContinueLogin)
            {
                Utility.Monitor.WriteLine(string.Format("Login: {0}: Account '{1}' at character list", e.State, un), ConsoleColor.Yellow);
                e.State.Account = acct;
                acct.LogAccess(e.State);
                acct.LastLogin = DateTime.UtcNow;
                acct.LogGAMELogin(e.State);             // specific for GAME login
                e.CityInfo = GetStartingCities();       // specific for GAME login
                acct.GUID = Utility.MakeGuid(un, un);   // create/refresh the GUID for this account

                if (newShardAcct)
                    Accounts.Table[un] = acct;

                if (newShardAcct)
                {
                    //Audit email
                    try
                    {
                        string regSubject = "Account Created Dynamically";
                        string regBody = "Account created dynamically, auto-account.\n";
                        regBody += "Info: \n";
                        regBody += "\n";
                        regBody += "Account: " + un + "\n";
                        regBody += "IP: " + ip + "\n";
                        regBody += "Password: " + pw + "\n";
                        regBody += "\n";
                        Emailer mail = new Emailer();
                        mail.SendEmail(Environment.GetEnvironmentVariable("AI.EMAIL.ACCOUNTING"), regSubject, regBody, false);
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                    Console.WriteLine("Login: {0}: Creating new account '{1}'", e.State, un);
                }
            }

            if (!e.Accepted)
                AccountAttackLimiter.RegisterInvalidAccess(e.State);
        }
        private static Account.AccountInfraction OffensePrioritization(Account acct, List<Account.AccountInfraction> offenses)
        {
            bool BlockedLogin =
                acct.InfractionStatus == Account.AccountInfraction.hasAccess &&
                acct.InfractionStatus == Account.AccountInfraction.checkPassword &&
                acct.InfractionStatus == Account.AccountInfraction.banned;

            if (!BlockedLogin)
            {
                // the following three infractions take priority over the others.
                #region Obsolete pre-login checks, now checked after login
                // 8/26/2023, Adam: We no longer use the invalid client check at login, but rather later after login
                //  during a packet exchange
                // TorExitNode is no longer processed here, but will be determined after login
                if (offenses.Contains(Account.AccountInfraction.TorExitNode))
                    return Account.AccountInfraction.TorExitNode;
                #endregion Obsolete pre-login checks, now checked after login

                if (offenses.Contains(Account.AccountInfraction.totalIPLimiter))
                    return Account.AccountInfraction.totalIPLimiter;

                if (offenses.Contains(Account.AccountInfraction.totalHardwareLimiter))
                    return Account.AccountInfraction.totalHardwareLimiter;
            }

            return acct.InfractionStatus;
        }
        private static bool IsGMNUO(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            if (text.EndsWith("$g", StringComparison.OrdinalIgnoreCase))
                return true;
            else
                return false;
        }
        private static void UnFuck(GameLoginEventArgs e)
        {
#if false
            // Version 1 of ClassicUO 'binding' discontinued
            string clear_text_username = ShuffleExtensions.DeShuffle(e.Username, e.State.m_Seed);
            string clear_text_password = ShuffleExtensions.DeShuffle(e.Password, e.State.m_Seed);
            bool valid_client = false;
            if (!IsGMNUO(clear_text_username) || !IsGMNUO(clear_text_password))
            {   
                // accept the old client, it will be enforced elsewhere
                valid_client = false;
            }
            else
            {
                // Remove last 2 characters from text ("$g")
                clear_text_username = clear_text_username.Remove(clear_text_username.Length - 2, 2);
                clear_text_password = clear_text_password.Remove(clear_text_password.Length - 2, 2);
                e.Username = clear_text_username;
                e.Password = clear_text_password;
                valid_client = true;
            }
            
            Account acct = null;
            GameLogin_Internal(e, ref acct, valid_client);
            if (acct != null)
                acct.ValidClient = valid_client;
#endif
            // Version 2 of ClassicUO 'binding' patch to support older scheme (above)
            // we will remove this after a couple weeks. We need to unfuck their name, although
            //  If we have ForceGMNCUO turned on, they will be disconnected some time after logging in
            string clear_text_username = ShuffleExtensions.DeShuffle(e.Username, e.State.m_Seed);
            string clear_text_password = ShuffleExtensions.DeShuffle(e.Password, e.State.m_Seed);
            bool valid_client = false;
            if (!IsGMNUO(clear_text_username) || !IsGMNUO(clear_text_password))
            {
                valid_client = false;
            }
            else
            {
                // Remove last 2 characters from text ("$g")
                clear_text_username = clear_text_username.Remove(clear_text_username.Length - 2, 2);
                clear_text_password = clear_text_password.Remove(clear_text_password.Length - 2, 2);
                e.Username = clear_text_username;
                e.Password = clear_text_password;
                valid_client = true;
                Utility.Monitor.WriteLine(string.Format("Login: {0}: Account '{1}' using older ClassicUO", e.State, e.Username), ConsoleColor.Red);
            }

        }
    }
}