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

/* Scripts\Engines\WealthTracker\WealthTracker.cs
 * CHANGELOG:
 *  1/12/08, Adam
 *      Make command Counselor access
 *  1/10/08, Adam
 *      Add switches and switch parsing logic
 *  1/9/08, Adam
 *      - Major cleanup � add external access to report writer
 *		- reverse x and y in IComparer.Compare() to get a descending sort (largest to smallest) 
 *		- Add a ScenarioFilter() to narrow the number of cases we are tracking.
 *  1/8/08, Adam
 *		- Initial Version
 *		- add tables
 */

using Server.Diagnostics;			// log helper
using Server.Network;
using System;
using System.Collections;

namespace Server.Engines
{
    /// <summary>
    /// provides methods to audit wealth acquisition
    /// </summary>
    static public class WealthTracker
    {
        static Hashtable m_IPList = new Hashtable();

        public static void Initialize()
        {
            Server.CommandSystem.Register("WealthTracker", AccessLevel.Counselor, new CommandEventHandler(WealthTracker_OnCommand));
        }

        public static void Configure()
        {
            EventSink.WealthTracker += new WealthTrackerEventHandler(OnWealthTracker);
        }

        #region utils
        public class WealthDomainComparer : IComparer
        {
            int IComparer.Compare(Object x, Object y)
            {   // reverse x and y to get a descending sort (largest to smallest)
                IPDomain mx = y as IPDomain;
                IPDomain my = x as IPDomain;
                if (mx == null || my == null)
                    return 0;

                if (mx.gold == my.gold)
                    return 0;
                if (mx.gold > my.gold)
                    return 1;
                else
                    return -1;
            }
        }

        public static object GetFirst(Hashtable ht, bool mustBeOnline)
        {
            foreach (DictionaryEntry dx in ht)
            {   // just return the first thing found
                if (mustBeOnline == false)
                    return dx.Value;
                // else
                if (dx.Value is AccountDomain)
                {
                    AccountDomain ad = dx.Value as AccountDomain;
                    foreach (DictionaryEntry mx in ad.mobileList)
                        if (mx.Value is Mobile)
                            if ((mx.Value as Mobile).NetState != null)
                                return dx.Value;
                }
                else if (dx.Value is Mobile)
                {
                    if ((dx.Value as Mobile).NetState != null)
                        return dx.Value;
                }
            }
            return null;
        }

        public static object GetFirst(Hashtable ht)
        {
            return GetFirst(ht, false);
        }

        private static int GetArgInt(Mobile from, string pattern, string args, AccessLevel accessLevel, int defaultValue)
        {   // sanity
            if (from == null || pattern == null)
                return 0;

            // check access
            if (from.AccessLevel < accessLevel)
            {
                from.SendMessage("You must have {0} access to use the {1} switch.", accessLevel.ToString(), pattern);
                return defaultValue;
            }

            // all int switches MUST be exactly of the form "-x=nn" where nn is the int portion. no spaces allowed
            try
            {   // extract the integer argument from the arg-string.
                string argID = string.Format("{0}=", pattern);
                int startIndex = args.IndexOf(argID);
                if (startIndex == -1) throw new ApplicationException();
                int whiteIndex = args.IndexOf(' ', startIndex);
                int intStart = startIndex + pattern.Length + 1;
                string argVal = (whiteIndex == -1) ? args.Substring(intStart) : args.Substring(intStart, whiteIndex - argID.Length);
                int param;
                if (int.TryParse(argVal, out param) == false)
                    throw new ApplicationException();
                else
                    return param;
            }
            catch // no logging here
            {
                from.SendMessage("Poorly formed {0} switch.", pattern);
                return defaultValue;
            }
        }
        #endregion

        #region ReportCompiler
        public static IPDomain[] ReportCompiler(int limit, int timeout)
        {   // allocate an array to hold the output
            IPDomain[] list = new IPDomain[WealthTracker.m_IPList.Count];

            // remove expired nodes
            int jx = 0;
            foreach (IPDomain node in WealthTracker.m_IPList.Values)
            {   // filter out old farming activity
                if (node == null) continue;
                if (((TimeSpan)(DateTime.UtcNow - node.lastTick)).Minutes <= timeout)
                    list[jx++] = node;
            }

            // now sort on gold held
            Array.Sort(list, new WealthDomainComparer());

            // now trim the array
            if (list.Length > limit)
                Array.Resize(ref list, limit);

            return list;
        }
        #endregion

        #region WealthTracker_OnCommand
        // WealthTracker_OnCommand
        [Usage("WealthTracker")]
        [Description("Provides client gold farming statistics.")]
        public static void WealthTracker_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.ArgString.Contains("-h"))
                {
                    e.Mobile.SendMessage("Usage: WealthTracker -h|-l=NN|-m=NN|-o");
                    e.Mobile.SendMessage("Where: -h ... help - this screen");
                    e.Mobile.SendMessage("Where: -l=NN ... list - top NN gold farming domains");
                    e.Mobile.SendMessage("Where: -m=NN ... mins - within last NN minutes");
                    e.Mobile.SendMessage("Where: -o ... online clients only");
                    //e.Mobile.SendMessage("Where: -hi ... clients that have hardware info");
                    return;
                }

                // default to top 10
                int limit = 10;
                if (e.ArgString.Contains("-t"))
                    limit = GetArgInt(e.Mobile, "-t", e.ArgString, AccessLevel.Owner, limit);

                // default to last hour
                int minutes = 60;
                if (e.ArgString.Contains("-m"))
                    minutes = GetArgInt(e.Mobile, "-m", e.ArgString, AccessLevel.Counselor, minutes);

                bool mustBeOnline = false;
                if (e.ArgString.Contains("-o"))
                    mustBeOnline = true;

                // compile the constrained list
                IPDomain[] list = ReportCompiler(limit, minutes);

                // show a super minimal report
                for (int ix = 0; ix < list.Length; ix++)
                {
                    IPDomain node = list[ix] as IPDomain;
                    AccountDomain ad = GetFirst(node.accountList, mustBeOnline) as AccountDomain;       // just first (online) account
                    if (ad != null)
                    {
                        Mobile m = GetFirst(ad.mobileList, mustBeOnline) as Mobile;                     // just first (online) mobile
                        if (m != null)
                            e.Mobile.SendMessage(string.Format("mob:{2}, gold:{0}, loc:{1}", node.gold, node.location, m));
                    }
                }

            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            e.Mobile.SendMessage("done.");
        }
        #endregion

        public static void OnWealthTracker(WealthTrackerEventArgs e)
        {
            // sanity
            if (e == null || e.item == null || e.item.Deleted == true)
                return;

            try
            {   // if we've not audited this item before
                if (e.item.Audited == false)
                {
                    e.item.Audited = true;
                    switch (e.auditType)
                    {
                        default: return;
                        case AuditType.GoldLifted: GoldLifted(e); break;
                        case AuditType.GoldDropBackpack: GoldDropBackpack(e); break;
                        case AuditType.GoldDropBank: GoldDropBank(e); break;
                        case AuditType.CheckDropBackpack: CheckDropBackpack(e); break;
                        case AuditType.CheckDropBank: CheckDropBank(e); break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        #region Domain definitions
        // node that describes the wealth coming into this IP Address
        public class WealthDomain
        {
            private Hashtable m_childList = new Hashtable();                        // list of accounts associated with this IP
            protected Hashtable childList { get { return m_childList; } }
            private int m_gold;
            public int gold { get { return m_gold; } set { m_gold = value; } }
            DateTime m_lastTick = DateTime.UtcNow;
            public DateTime lastTick { get { return m_lastTick; } set { m_lastTick = value; } }
            private Point3D m_location;
            public Point3D location { get { return m_location; } set { m_location = value; } }
        }

        public class IPDomain : WealthDomain
        {
            public Hashtable accountList { get { return base.childList; } }
        }

        public class AccountDomain : WealthDomain
        {
            public Hashtable mobileList { get { return base.childList; } }
        }
        #endregion

        #region ScenarioFilter
        private static bool ScenarioFilter(WealthTrackerEventArgs e)
        {
            switch (e.auditType)
            {
                // At this time, we only support the looting a corpse scenario and looting a dungeon chest scenario
                // we don't expect picking up harrower loot or looting treasure map chests will be exploitable
                case AuditType.GoldLifted:
                    if (e.parent != null && (e.parent is Items.Corpse || e.parent is Items.DecayedCorpse || e.parent is Items.DungeonTreasureChest))
                        return true;
                    return false;
                case AuditType.GoldDropBackpack:
                    break;
                case AuditType.GoldDropBank:
                    break;
                case AuditType.CheckDropBackpack:
                    break;
                case AuditType.CheckDropBank:
                    break;
            }

            return false;
        }
        #endregion

        private static void GoldLifted(WealthTrackerEventArgs e)
        {
            // sanity
            if (e == null || e.item == null || e.from == null || e.from.NetState == null)
                return;

            // eliminate tracking of all but approved scenarios 
            if (ScenarioFilter(e) == false)
                return;

            NetState state = e.from.NetState;
            int iphc = state.ToString().GetHashCode();                  // hash code of IP address
            IPDomain ipd = null;
            AccountDomain ad = null;
            if (m_IPList.Contains(iphc))                                // if we have it already
            {
                ipd = m_IPList[iphc] as IPDomain;                       // get the IPDomain at this IP address
                if (ipd.accountList.Contains(e.from.Account) == false)  // if we don't have an account domain, create it
                    ipd.accountList.Add(e.from.Account, new AccountDomain());
                ad = ipd.accountList[e.from.Account] as AccountDomain;  // get the account domain
            }
            else
            {
                m_IPList[iphc] = new IPDomain();                            // start a new list of clients at this IP address
                ipd = m_IPList[iphc] as IPDomain;                           // get the IPDomain at this IP address
                ipd.accountList.Add(e.from.Account, new AccountDomain());   // add another client at this IP + location
                ad = ipd.accountList[e.from.Account] as AccountDomain;      // get the account domain
            }

            // update domain information
            if (ad != null && ipd != null)
            {
                ad.mobileList[e.from.Serial] = e.from;              // record mobile (not sure what the key should be)
                ad.gold += e.item.Amount;                           // update gold for this IPDomain
                ad.lastTick = DateTime.UtcNow;                         // last gold pickup
                ad.location = e.from.Location;                      // last gold pickup location
                ipd.gold += e.item.Amount;                          // update gold for this IPDomain
                ipd.lastTick = DateTime.UtcNow;                        // last gold pickup
                ipd.location = e.from.Location;                     // last gold pickup location
            }
        }

        // tbd
        private static void GoldDropBackpack(WealthTrackerEventArgs e)
        {
        }

        // tbd
        private static void GoldDropBank(WealthTrackerEventArgs e)
        {
        }

        // tbd
        private static void CheckDropBackpack(WealthTrackerEventArgs e)
        {
        }

        // tbd
        private static void CheckDropBank(WealthTrackerEventArgs e)
        {
        }

    }
}