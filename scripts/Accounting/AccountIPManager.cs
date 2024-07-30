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

/* Scripts/Accounting/AccountIPManager.cs
 * CHANGELOG
 *	8/27/2023, Adam,
 *		Initial Creation
 */

/* If an IP address is likely anonymous, the database provides the following flags for identification: 
 * Hosting Provider/Data Center, 
 * VPN, 
 * Residential Proxy, 
 * Tor Exit Node, 
 * and Public Proxy.
 */

/* Should I block Tor exit nodes?
 * With the onslaught of cyber-attacks, it is more important than ever before to block TOR (The Onion Router) nodes from communicating with your network. 
 * TOR Exit nodes can be used by anonymous attackers from around the globe to launch attacks against networks.
 * https://www.linkedin.com/pulse/blocking-tor-nodes-gary-wright/
*/
using MaxMind.GeoIP2.Responses;
using Server.Misc;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Server.Accounting
{
    public static class AccountIPManager
    {
        public class UserIPInfo
        {
            public string Ip;
            public string Username;
            public UserIPInfo(string ip, string username)
            {
                Ip = ip;
                Username = username;
            }
        }
        public static ConcurrentDictionary<string, CityResponse> UserIPProfile = new(StringComparer.OrdinalIgnoreCase);
        public static ConcurrentQueue<UserIPInfo> UserIPRequest = new();
        public static List<KeyValuePair<string, CityResponse>> UserIPList()
        {
            /* The enumerator returned from the dictionary is safe to use concurrently with reads and writes to the dictionary, 
             * however it does not represent a moment-in-time snapshot of the dictionary. 
             * The contents exposed through the enumerator may contain modifications made to the dictionary after GetEnumerator was called.
             */
            List<KeyValuePair<string, CityResponse>> list = new();
            foreach (var kvp in UserIPProfile)
                list.Add(kvp);
            // return a list that is safe to use whenever
            return list;
        }

        const int MMAccountId = 747135;
        const string MMlicenseKey = "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";
        const string MMHost = "geolite.info";
        public static void AcquireIpInfo()
        {
            List<UserIPInfo> RetryList = new List<UserIPInfo>();
            UserIPInfo userIPInfo = null;
            while (UserIPRequest.TryDequeue(out userIPInfo))
            {
                if (ServerList.IsPrivateNetwork(userIPInfo.Ip))
                {   // client.City() will throw the exception, but better to explicitly handle it here.
                    Utility.ConsoleWriteLine("Ignoring IPInfo for private network {0}", ConsoleColor.Yellow, userIPInfo.Ip);
                    continue;
                }

                using (var client = new MaxMind.GeoIP2.WebServiceClient(accountId: MMAccountId, licenseKey: MMlicenseKey, host: MMHost))
                {
                    // Do the lookup
                    CityResponse response = null;
                    try
                    {
                        if (userIPInfo != null)
                            response = client.City(userIPInfo.Ip);
                        else
                            continue; // error, should never happen

                        #region TEST CODE
#if false
                        Console.WriteLine(response.Country.IsoCode);        // 'US'
                        Console.WriteLine(response.Country.Name);           // 'United States'

                        Console.WriteLine(response.MostSpecificSubdivision.Name);    // 'Minnesota'
                        Console.WriteLine(response.MostSpecificSubdivision.IsoCode); // 'MN'

                        Console.WriteLine(response.City.Name); // 'Minneapolis'

                        Console.WriteLine(response.Postal.Code); // '55455'

                        Console.WriteLine(response.Location.Latitude);  // 44.9733
                        Console.WriteLine(response.Location.Longitude); // -93.2323
#endif
                        #endregion TEST CODE
                    }
                    catch (Exception ex)
                    {   // Sample Error: "The IP address '127.0.0.1' is a reserved IP address (private, multicast, etc.)."
                        Utility.ConsoleWriteLine(ex.Message, ConsoleColor.Red);
                        // don't retry this address
                        continue;
                    }

                    // if the key exists, replace it
                    if (UserIPProfile.ContainsKey(userIPInfo.Username))
                    {
                        CityResponse oldResponse = null;
                        bool replaced = false;
                        if (UserIPProfile.TryGetValue(userIPInfo.Username, out oldResponse))
                            if (UserIPProfile.TryRemove(userIPInfo.Username, out oldResponse))
                                if (UserIPProfile.TryAdd(userIPInfo.Username, response))
                                    replaced = true;

                        if (!replaced)
                            // retry again later
                            RetryList.Add(userIPInfo);

                        continue;
                    }

                    // try to add it to our database
                    if (!UserIPProfile.TryAdd(userIPInfo.Username, response))
                    {
                        // retry again later
                        RetryList.Add(userIPInfo);
                        continue;
                    }
                    else
                        continue;
                }
            }

            // we do this outside of the above while loop so we don't end up infinite looping
            foreach (UserIPInfo info in RetryList)
                // retry again later
                UserIPRequest.Enqueue(userIPInfo);
        }

        public static void Initialize()
        {
            Server.CommandSystem.Register("IpInfo", AccessLevel.Administrator, new CommandEventHandler(IpInfo));
        }
        [Usage("IpInfo <target>")]
        [Description("Gets the IP Info associated with this player.")]
        public static void IpInfo(CommandEventArgs e)
        {
            try
            {
                e.Mobile.SendMessage("Target the player...");
                e.Mobile.Target = new IpInfoTarget();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        private class IpInfoTarget : Target
        {

            public IpInfoTarget()
                : base(-1, false, TargetFlags.None)
            {

            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                PlayerMobile pm = targeted as Mobiles.PlayerMobile;

                if (pm != null)
                {
                    List<string> reason = null;
                    CityResponse response = GetIpInfo(pm, ref reason);
                    Accounting.Account account = pm.Account as Accounting.Account;
                    if (response != null)
                    {
                        try
                        {
                            from.SendMessage("{0}: {1}, {2}, {3}", account.Username, response.City.Name, response.MostSpecificSubdivision.Name, response.Country.Name);
                            from.SendMessage("Is Anonymous {0}", response.Traits.IsAnonymous);
                            from.SendMessage("Is Anonymous Proxy {0}", response.Traits.IsAnonymousProxy);
                            from.SendMessage("Is Anonymous Vpn {0}", response.Traits.IsAnonymousVpn);
                            from.SendMessage("Is Hosting Provider {0}", response.Traits.IsHostingProvider);
                            from.SendMessage("Is Legitimate Proxy {0}", response.Traits.IsLegitimateProxy);
                            from.SendMessage("Is Public Proxy {0}", response.Traits.IsPublicProxy);
                            from.SendMessage("Is Residential Proxy {0}", response.Traits.IsResidentialProxy);
                            from.SendMessage("Is Satellite Provider {0}", response.Traits.IsSatelliteProvider);
                            from.SendMessage("Is Tor Exit Node {0}", response.Traits.IsTorExitNode);
                        }
                        catch (Exception ex)
                        {
                            from.SendMessage("IpInfo: {0}.", ex.Message);
                        }
                    }
                    else
                    {
                        if (reason != null)
                            foreach (string s in reason)
                                from.SendMessage("{0}", s);
                    }
                }
                else
                    from.SendMessage("That is not a player.");

                return;
            }
        }
        public static CityResponse GetIpInfo(PlayerMobile pm, ref List<string> reason)
        {
            reason = new List<string>();
            if (pm != null)
            {
                Accounting.Account account = pm.Account as Accounting.Account;
                if (account != null && account.Username != null)
                {
                    if (UserIPProfile.ContainsKey(account.Username))
                    {
                        CityResponse response = null;
                        if (UserIPProfile.TryGetValue(account.Username, out response))
                            return response;
                        else
                        {
                            // the database is currently in use.
                            reason.Add(string.Format("IpInfo for {0} is currently unavailable.", account.Username));
                            return null;
                        }
                    }
                    else
                    {
                        reason.Add(string.Format("IpInfo for {0} is currently unavailable.", account.Username));
                        reason.Add(string.Format("Either the info has not been collected yet, or "));
                        reason.Add(string.Format("the IP address is a reserved IP address (private, multicast, etc.)."));
                        return null;
                    }
                }
            }
            return null;
        }
    }
}