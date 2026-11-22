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

/* Misc/GoldTracker.cs
 * Created by Pixie.
 * ChangeLog:
 *  11/19/26, Adam (bans asked only by the report)
 *      GoldTotaller.Banned is bool? (null: not checked): the constructor no longer asks every account's ban (the
 *      Wealthiest pass never uses it); the staff gold report asks when it marks banned accounts.
 *  11/15/26, Adam (wealth, a little at a time)
 *      The Wealthiest leaderboard counted every account's backpacks, banks and houses at once, every 5 minutes (190 ms
 *      on Siege, growing with the world). WealthPass counts a few accounts per slice (Budget 20 ms each Interval 100 ms)
 *      and keeps the last completed count (Last) for the board, so the board never empties mid-count. The counting is
 *      shared with the [GoldTracker command (CountAccount, AddVendors, Sorted), which still counts at once.
 *      House worth: GetDeed makes a real deed item to read its price; the price is remembered per house for an hour
 *      (HouseWorth; staff may reprice static houses) instead of a deed per house per count. DeedsMade counts them.
 *      A vendor whose owner's account isn't counted no longer stops the whole count (it threw).
 *  12/3/2025, Adam
 *      When dealing with BaseUpgradeContract, we no longer look directly at the Price field to calculate value, but instead
 *          use BaseVendor.PlayerPays() to calculate any markup (like for instance Siege 3x pricing)
 *  9/6/22, Adam
 *  ignore Shard Owner (I own all the static houses in the custom housing area)
 *  For Siege this raises an assert()
 *      if (player == World.GetAdminAcct())
 *          continue;
 *  8/22/22, Adam
 *      Fix and item leak to the internal map.
 *      gold tracker gets house deeds to determine worth. But the code was only deleting the deed if it wasn't a tent deed.
 *      Now the code deletes the deed regardless.
 *  8/13/22, Adam
 *      Added null container checks.
 *  9/6/21, Pix
 *      - Refactored somewhat
 *      - Extracted GetWealthTable method for other use
 *      - added GetSortedWealthArray to make it easy
 *      - added character name (uses character on acct with most game time)
 *      - added option to command to display character name instead of account name
 *      - removed output messaging
 *  9/2/07, Adam
 *		- Check for character access > Player (not just account access > Player.)
 *			This is because your account may be AccessLevel.Player, your you have a Seer character.
 *			This is infact the case with most of our staff .. there are good reasons for this btw.
 *		- Eliminate tent deeds from being counted, we do however continue to search tents for gold etc.
 *  9/1/07, Pix
 *      Fixed console timing output.
 *  9/1/07, Pix
 *      - Fixed -ib
 *      - Fixed -ss
 *      - Added placed houses worth
 *      - Included storage upgrade deeds/worth
 *	9/1/07, Adam
 *		- Add null check for the vendor.owner field (may be null)
 *		- Add "please wait" broadcast message
 *  8/31/07 - Pix
 *      Now counts locked down bankchecks (oversight), house deeds (+value), and has optional arguments.
 *	7/8/06 - Pix
 *		Changed the way goldtracker works.  Added sorting.
 *	7/7/06 - Pix
 *		Added PlayerVendor totals.
 *	6/06/05 - Pix
 *		Now also counts lockboxes too in count of gold.
 * 4/22/04 - pixie
 *    Initial Version
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Multis.Deeds;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Server.Misc
{
    public class GoldTracker : Timer
    {
        private static bool bInProcess = false;
        private const double SAVE_FREQUENCY = 10.0; //in minutes, with a 5-minute threshold.
        private const string OUTPUT_DIRECTORY = "web"; //directory to place file
        private const string OUTPUT_FILE = "gold.html"; //file for output

        public static void Initialize()
        {
            //uncomment the following line if you want to have it on a timer
            //new GoldTracker().Start();
            CommandSystem.Register("GoldTracker", AccessLevel.Administrator, new CommandEventHandler(GoldTracker_OnCommand));
        }

        public GoldTracker()
            : base(TimeSpan.FromMinutes(5.0), TimeSpan.FromMinutes(10.0))
        {
            Priority = TimerPriority.OneMinute;
        }

        private static string Encode(string input)
        {
            StringBuilder sb = new StringBuilder(input);

            sb.Replace("&", "&amp;");
            sb.Replace("<", "&lt;");
            sb.Replace(">", "&gt;");
            sb.Replace("\"", "&quot;");
            sb.Replace("'", "&apos;");

            return sb.ToString();
        }

        protected override void OnTick()
        {
            OnCalculateAndWrite(null);
        }

        [Usage("GoldTracker")]
        [Description("Tracks the gold in the world.")]
        private static void GoldTracker_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length == 0)
            {
                OnCalculateAndWrite(e.Mobile); //this does 100
            }
            else
            {
                int number = 100;
                //-ct:xxx // use this amount
                //-sn // supress names 
                //-ss // supress staff 
                //-ib // indicate banned 
                //-cn // use character name instead of account name
                bool bSupressNames = false;
                bool bSupressStaff = false;
                bool bIndicateBanned = false;
                bool bUseCharacterName = false;

                for (int i = 0; i < e.Arguments.Length; i++)
                {
                    try
                    {
                        string arg = e.Arguments[i];

                        if (arg.StartsWith("-ct"))
                        {
                            string num = arg.Substring(4);
                            number = Int32.Parse(num);
                        }
                        else if (arg.StartsWith("-sn"))
                        {
                            bSupressNames = true;
                        }
                        else if (arg.StartsWith("-ss"))
                        {
                            bSupressStaff = true;
                        }
                        else if (arg.StartsWith("-ib"))
                        {
                            bIndicateBanned = true;
                        }
                        else if (arg.StartsWith("-cn"))
                        {
                            bUseCharacterName = true;
                        }
                        else
                        {
                            e.Mobile.SendMessage("Ignoring unrecognized switch: " + arg);
                        }
                    }
                    catch (Exception argEx)
                    {
                        e.Mobile.SendMessage("Exception (" + argEx.Message + ") when parsing arg: " + e.Arguments[i]);
                    }
                }

                if (number <= 0)
                {
                    number = Server.Accounting.Accounts.Table.Count;
                }

                OnCalculateAndWrite(e.Mobile, number, bSupressNames, bSupressStaff, bIndicateBanned, bUseCharacterName);
            }
        }

        private static void SM(Mobile m, string message)
        {
            if (m != null && message != null && message.Length > 0)
            {
                m.SendMessage(message);
            }
        }

        private static void OnCalculateAndWrite(Mobile? cmdmob)
        {
            OnCalculateAndWrite(cmdmob, 100);
        }

        private static void OnCalculateAndWrite(Mobile? cmdmob, int maxtoprint)
        {
            OnCalculateAndWrite(cmdmob, maxtoprint, false, false, false, false);
        }

        private static void OnCalculateAndWrite(Mobile? cmdmob, int maxtoprint, bool bSupressNames, bool bSupressStaff, bool bIndicateBanned, bool bUseCharacterName)
        {
            if (bInProcess == true)
            {
                System.Console.WriteLine("Got GoldTracker call when already processing.");
                return;
            }

            bInProcess = true;

            DateTime startDateTime = DateTime.UtcNow;

            if (!Directory.Exists(OUTPUT_DIRECTORY))
                Directory.CreateDirectory(OUTPUT_DIRECTORY);

            try
            {
                GoldTotaller[] goldTotallers = GetSortedWealthArray(bSupressStaff);

                using (StreamWriter op = new StreamWriter(OUTPUT_DIRECTORY + "/" + OUTPUT_FILE))
                {
                    op.WriteLine("<HTML><BODY>");
                    op.WriteLine("<TABLE BORDER=1 WIDTH=90%><TR>");
                    if (bSupressNames)
                    {
                        op.WriteLine("<TD>=(char count)</TD>");
                    }
                    else if (bUseCharacterName)
                    {
                        op.WriteLine("<TD>Character (char count)</TD>");
                    }
                    else
                    {
                        op.WriteLine("<TD>Account (char count)</TD>");
                    }
                    op.WriteLine("<TD>Total Worth</TD>");
                    op.WriteLine("<TD>Backpack Gold</TD>");
                    op.WriteLine("<TD>Bank Gold</TD>");
                    op.WriteLine("<TD>House Gold (house count)</TD>");
                    op.WriteLine("<TD>Vendor Gold (vendor count)</TD>");
                    op.WriteLine("<TD>Returnable Deed Worth (deed count)<br>(Included house and storage upgrade deeds</TD>");
                    op.WriteLine("<TD>House(s) Worth</TD>");
                    op.WriteLine("</TR>");

                    for (int i = 0; i < goldTotallers.Length && i < maxtoprint; i++)
                    {
                        GoldTotaller gt = goldTotallers[i];
                        if (gt != null)
                        {
                            string bannedstr = "";
                            if (bIndicateBanned)
                            {
                                // asked only here, for this report (the leaderboard's pass never needs it)
                                gt.Banned ??= Server.Engines.Restrictions.IsBanned(gt.AccountName);
                                if (gt.Banned == true)
                                {
                                    bannedstr = " **banned** ";
                                }
                            }
                            if (bSupressNames)
                            {
                                op.WriteLine("<TR><TD>(" + gt.CharacterCount + ")" + bannedstr + "</TD>"); //Account
                            }
                            else if (bUseCharacterName)
                            {
                                op.WriteLine("<TR><TD><B>" + gt.CharacterName + "</B>(" + gt.CharacterCount + ")" + bannedstr + "</TD>"); //Account
                            }
                            else
                            {
                                op.WriteLine("<TR><TD><B>" + gt.AccountName + "</B>(" + gt.CharacterCount + ")" + bannedstr + "</TD>"); //Account
                            }
                            op.WriteLine("<TD>" + gt.TotalGold + "</TD>"); //Total Worth
                            op.WriteLine("<TD>" + gt.BackpackGold + "</TD>"); //Backpack
                            op.WriteLine("<TD>" + gt.BankGold + "</TD>"); //bank
                            op.WriteLine("<TD>" + gt.HouseGold + " (" + gt.HouseCount + ")</TD>"); //houses
                            op.WriteLine("<TD>" + gt.VendorGold + " (" + gt.VendorCount + ")</TD>"); //vendors
                            op.WriteLine("<TD>" + gt.HouseDeedWorth + " (" + gt.HouseDeedCount + ")</TD>"); //housedeeds
                            op.WriteLine("<TD>" + gt.HousesWorth + "</TD>"); //housedeeds

                            op.WriteLine("</TR>");
                        }
                    }

                    op.WriteLine("</TABLE></BODY></HTML>");
                    op.Flush();
                    op.Close();
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Error in GoldTracker: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
            finally
            {
                DateTime endDateTime = DateTime.UtcNow;
                TimeSpan time = endDateTime - startDateTime;
                string endmsg = "finished in " + ((double)time.TotalMilliseconds / (double)1000) + " seconds.";
                bInProcess = false;
            }
        }

        public static GoldTotaller[] GetSortedWealthArray(bool bSuppressStaff)
        {
            return Sorted(GetWealthTable(bSuppressStaff));
        }

        // the table's totals, the wealthiest first (GTComparer)
        private static GoldTotaller[] Sorted(Hashtable table)
        {
            ArrayList keys = new ArrayList(table.Keys);
            keys.Sort(new GTComparer(table));

            GoldTotaller[] goldTotallerArray = new GoldTotaller[keys.Count];

            //blah
            for (int i = 0; i < keys.Count; i++)
            {
                goldTotallerArray[i] = table[keys[i]] as GoldTotaller;
            }

            return goldTotallerArray;
        }

        // everything at once (the [GoldTracker command); the leaderboard counts a little at a time (WealthPass)
        public static Hashtable GetWealthTable(bool bSupressStaff)
        {
            Hashtable table = NewTable(bSupressStaff);

            foreach (Account acct in Accounts.Table.Values)
            {
                if (acct != null && table.ContainsKey(acct))
                    CountAccount(acct, table);
            }

            AddVendors(table, bSupressStaff);
            return table;
        }

        // a totaller for every account counted (staff suppressed when asked)
        private static Hashtable NewTable(bool bSupressStaff)
        {
            Hashtable table = new Hashtable(Accounts.Table.Values.Count);

            foreach (Account acct in Accounts.Table.Values)
            {
                if (acct == null)
                    continue;

                if ((bSupressStaff && acct.GetAccessLevel() > AccessLevel.Player) == false)
                    table.Add(acct, new GoldTotaller(acct));
            }
            return table;
        }

        // one account's characters: backpack, bank and houses
        private static void CountAccount(Account acct, Hashtable table)
        {
            TimeSpan timeSpan = TimeSpan.Zero;

            //iterate through characters:
            for (int i = 0; i < 5; ++i)
            {
                if (acct[i] is PlayerMobile player && !player.Deleted)
                {
                    Container backpack = player.Backpack;
                    BankBox bank = player.BankBox;
                    int backpackgold = 0;
                    int bankgold = 0;

                    int housedeedcount = 0;
                    int housedeedworth = 0;

                    int housesworth = 0;

                    // ignore Shard Owner (I own all the static houses in the custom housing area)
                    if (player == World.GetAdminAcct())
                        continue;

                    //BACKPACK
                    backpackgold = GetGoldInContainer(backpack);
                    housedeedworth = GetHouseDeedsInContainer(backpack, ref housedeedcount);

                    //BANK
                    bankgold = GetGoldInContainer(bank);
                    housedeedworth += GetHouseDeedsInContainer(bank, ref housedeedcount);

                    //house?
                    int housetotal = 0;
                    ArrayList list = Multis.BaseHouse.GetHouses(player);
                    for (int j = 0; j < list.Count; ++j)
                    {
                        if (list[j] is BaseHouse house)
                        {
                            housesworth += (int)house.UpgradeCosts;
                            housesworth += HouseWorth(house);
                            housetotal += GetGoldInHouse(house);
                            housedeedworth += GetHouseDeedWorthInHouse(house, ref housedeedcount);
                        }
                    }

                    if (table[acct] is not GoldTotaller acctGoldTotaller)
                        return;

                    //if the name in acctGoldTotaller is empty or this char's time is larger than the one stored.
                    if (string.IsNullOrWhiteSpace(acctGoldTotaller.CharacterName) || player.GameTime > timeSpan)
                    {
                        acctGoldTotaller.CharacterName = player.Name;
                        timeSpan = player.GameTime;
                    }

                    acctGoldTotaller.BackpackGold += backpackgold;
                    acctGoldTotaller.BankGold += bankgold;
                    acctGoldTotaller.HouseGold += housetotal;
                    acctGoldTotaller.HouseCount += list.Count;
                    acctGoldTotaller.CharacterCount += 1;
                    acctGoldTotaller.HouseDeedCount += housedeedcount;
                    acctGoldTotaller.HouseDeedWorth += housedeedworth;
                    acctGoldTotaller.HousesWorth += housesworth;
                }
            }
        }

        // the player vendors' gold, by their owners' accounts
        private static void AddVendors(Hashtable table, bool bSupressStaff)
        {
            foreach (Mobile wm in World.Mobiles.Values)
            {
                if (wm is PlayerVendor pv && pv.Owner != null && pv.Owner.Account is Account thisacct)
                {
                    if ((bSupressStaff && thisacct.GetAccessLevel() > AccessLevel.Player) == false && table[thisacct] is GoldTotaller gt)
                    {
                        gt.VendorCount += 1;
                        gt.VendorGold += pv.HoldGold;
                    }
                }
            }
        }

        #region House worth
        // What a house would sell back for: its deed's price (RealEstateBroker: Siege's markup; tents are worth nothing).
        //  The price comes from a deed, and GetDeed makes a real one (an item in the world, deleted at once), so each
        //  house's price is remembered for an hour (staff may reprice static houses) instead of a deed per house per count.
        public static readonly TimeSpan HouseWorthKept = TimeSpan.FromHours(1);
        private static readonly Dictionary<BaseHouse, (int Worth, DateTime Utc)> m_HouseWorth = new Dictionary<BaseHouse, (int, DateTime)>(ReferenceEqualityComparer.Instance);

        /// <summary>Deeds made to price houses since the server started (for harnesses: a remembered price makes none).</summary>
        public static int DeedsMade { get; private set; }

        public static int HouseWorth(BaseHouse house)
        {
            if (m_HouseWorth.TryGetValue(house, out var known) && DateTime.UtcNow - known.Utc < HouseWorthKept)
                return known.Worth;

            int worth = 0;
            HouseDeed? hd = house.GetDeed();
            DeedsMade++;
            if (hd != null && hd is SiegeTentBag == false && hd is TentBag == false)
                worth = RealEstateBroker.ComputePriceFor(hd);
            hd?.Delete();
            m_HouseWorth[house] = (worth, DateTime.UtcNow);
            return worth;
        }

        // houses that are gone, or whose price is old, are forgotten
        private static void PruneHouseWorth()
        {
            DateTime now = DateTime.UtcNow;
            foreach (BaseHouse house in m_HouseWorth.Where(kv => kv.Key.Deleted || now - kv.Value.Utc >= HouseWorthKept).Select(kv => kv.Key).ToList())
                m_HouseWorth.Remove(house);
        }
        #endregion House worth

        #region WealthPass
        // The leaderboard's count, a little at a time: a pass counts accounts for at most Budget each Interval, so no
        //  single count holds the main thread up (counting everything at once took 190 ms on Siege, every 5 minutes, and
        //  grows with the world). When every account is counted, the vendors are added and the totals sorted: that is
        //  Last, the board's numbers until the next pass ends. Gold that moves between accounts during a pass may be
        //  counted twice or missed for that pass: a leaderboard can live with that.
        public static class WealthPass
        {
            public static readonly TimeSpan Budget = TimeSpan.FromMilliseconds(20);
            public static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

            private static Hashtable? m_Table;
            private static List<Account> m_Accounts = new List<Account>();
            private static int m_Next;
            private static bool m_Suppress;
            private static Timer? m_Timer;
            private static int m_Slices;
            private static double m_WorkMs, m_LongestMs;
            private static DateTime m_StartedUtc;

            /// <summary>The last completed count, the wealthiest first (null: none yet since the server started).</summary>
            public static GoldTotaller[]? Last { get; private set; }
            public static DateTime LastUtc { get; private set; }

            /// <summary>The last completed pass: its slices and the longest one (ms).</summary>
            public static int LastSlices { get; private set; }
            public static double LastLongestSliceMs { get; private set; }

            /// <summary>Called when a pass completes (the leaderboard shows the new numbers at once).</summary>
            public static Action? Completed;

            public static bool Running => m_Table != null;

            /// <summary>Starts a pass, unless one is running. timer false: the caller steps it (harnesses).</summary>
            public static void Start(bool suppressStaff, bool timer = true)
            {
                if (Running)
                    return;
                m_Suppress = suppressStaff;
                m_Table = NewTable(suppressStaff);
                m_Accounts = Accounts.Table.Values.OfType<Account>().Where(a => m_Table.ContainsKey(a)).ToList();
                m_Next = 0;
                m_Slices = 0;
                m_WorkMs = m_LongestMs = 0;
                m_StartedUtc = DateTime.UtcNow;
                if (timer)
                    m_Timer = Timer.DelayCall(TimeSpan.Zero, Interval, () => Step(Budget.TotalMilliseconds));
            }

            /// <summary>One slice: accounts counted until 'budgetMs' is used; the pass's end in a slice of its own. True: it completed.</summary>
            public static bool Step(double budgetMs)
            {
                if (m_Table == null)
                    return true;
                long start = System.Diagnostics.Stopwatch.GetTimestamp();
                bool finishing = m_Next >= m_Accounts.Count;
                try
                {
                    if (finishing)
                    {   // every account counted (in earlier slices): the vendors, the order, the board
                        AddVendors(m_Table, m_Suppress);
                        Last = Sorted(m_Table);
                        LastUtc = DateTime.UtcNow;
                        PruneHouseWorth();
                    }
                    else
                        while (m_Next < m_Accounts.Count)
                        {
                            CountAccount(m_Accounts[m_Next++], m_Table);
                            if (Elapsed(start) >= budgetMs)
                                break;
                        }
                }
                catch (Exception ex)
                {   // a pass that fails is abandoned; the board keeps the last numbers
                    LogHelper.LogException(ex, "GoldTracker.WealthPass");
                    Stop();
                    return true;
                }
                double ms = Elapsed(start);
                m_Slices++;
                m_WorkMs += ms;
                m_LongestMs = Math.Max(m_LongestMs, ms);
                Server.Engines.CronScheduler.Cron.RecordStat("Wealth pass slice", ms);
                if (!finishing)
                    return false;

                LastSlices = m_Slices;
                LastLongestSliceMs = m_LongestMs;
                Server.Engines.CronScheduler.Cron.RecordStat("Wealth pass (all slices)", m_WorkMs);
                Console.WriteLine("Wealth pass: {0} accounts in {1} slices ({2:0} ms of work over {3:0.0} s, longest slice {4:0} ms)",
                    m_Accounts.Count, m_Slices, m_WorkMs, (DateTime.UtcNow - m_StartedUtc).TotalSeconds, m_LongestMs);
                Stop();
                try { Completed?.Invoke(); }
                catch (Exception ex) { LogHelper.LogException(ex, "GoldTracker.WealthPass.Completed"); }
                return true;
            }

            private static void Stop()
            {
                m_Timer?.Stop();
                m_Timer = null;
                m_Table = null;
                m_Accounts = new List<Account>();
            }

            private static double Elapsed(long start) => (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        }
        #endregion WealthPass

        private class GTComparer : System.Collections.IComparer
        {
            private Hashtable m_Table = null;

            public GTComparer(Hashtable ht)
            {
                m_Table = ht;
            }

            public int Compare(object? x, object? y)
            {
                if (x == y) return 0;

                GoldTotaller? xg = m_Table[x] as GoldTotaller;
                GoldTotaller? yg = m_Table[y] as GoldTotaller;

                if (xg == null || yg == null)
                {
                    return 0;
                }
                else
                {
                    if (xg.TotalGold == yg.TotalGold)
                    {
                        if (xg.BankGold > yg.BankGold)
                        {
                            return -1;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                    else if (xg.TotalGold > yg.TotalGold)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
            }

        }

        public class GoldTotaller
        {
            public string AccountName;

            public string CharacterName;

            public bool? Banned;            // null: not checked (only the staff report asks: see Report)

            public int CharacterCount = 0;

            public int BackpackGold = 0;
            public int BankGold = 0;

            public int HouseGold = 0;
            public int HouseCount = 0;

            public int VendorGold = 0;
            public int VendorCount = 0;

            public int HouseDeedCount = 0;
            public int HouseDeedWorth = 0;

            public int HousesWorth = 0;

            public int TotalGold
            {
                get { return BackpackGold + BankGold + HouseGold + VendorGold + HouseDeedWorth + HousesWorth; }
            }

            public GoldTotaller(Account acct)
            {
                this.AccountName = acct.Username;
            }
        }

        private static ArrayList GetVendors(Mobile m)
        {
            ArrayList list = new ArrayList();

            foreach (Mobile wm in World.Mobiles.Values)
            {
                if (wm is PlayerVendor)
                {
                    PlayerVendor? pv = wm as PlayerVendor;
                    if (pv != null)
                    {
                        if (pv.Owner == m)
                        {
                            list.Add(pv);
                        }
                    }
                }
            }

            return list;
        }

        private static int GetHouseDeedsInContainer(Container c, ref int count)
        {
            int iGold = 0;
            if (c != null)
            {
                Item[] deeds = c.FindItemsByType(typeof(Server.Multis.Deeds.HouseDeed), true);
                foreach (Item i in deeds)
                {
                    Server.Multis.Deeds.HouseDeed? housedeed = i as Server.Multis.Deeds.HouseDeed;
                    // don't count tents as they cannot be redeemed for cash
                    if (housedeed != null && housedeed is SiegeTentBag == false && housedeed is TentBag == false)
                    {
                        count++;
                        iGold += RealEstateBroker.ComputePriceFor(housedeed);
                    }
                }

                Item[] BUCdeeds = c.FindItemsByType(typeof(BaseUpgradeContract), true);
                foreach (Item i in BUCdeeds)
                {
                    BaseUpgradeContract? budeed = i as BaseUpgradeContract;
                    if (budeed != null)
                    {
                        count++;
                        iGold += // old code: (int)budeed.Price;
                                 // We need to calulate what the player actually pays, not the embedded deed price, since siege has 3x pricing.
                                 // the deed's cost is added to the houses upgrades, which is later refunded.
                        BaseVendor.PlayerPays(budeed.GetType());
                    }
                }
            }
            return iGold;
        }

        private static int GetGoldInContainer(Container c)
        {
            int iGold = 0;
            if (c != null)
            {
                Item[] golds = c.FindItemsByType(typeof(Gold), true);
                foreach (Item g in golds)
                {
                    iGold += g.Amount;
                }
                Item[] checks = c.FindItemsByType(typeof(BankCheck), true);
                foreach (Item i in checks)
                {
                    BankCheck bc = (BankCheck)i;
                    iGold += bc.Worth;
                }
            }
            return iGold;
        }

        private static int GetHouseDeedWorthInHouse(BaseHouse h, ref int count)
        {
            int iGold = 0;

            ArrayList lockdowns = h.LockDowns;
            foreach (Item i in lockdowns)
            {
                if (i is Container)
                {
                    iGold += GetHouseDeedsInContainer((Container)i, ref count);
                }
                else if (i is HouseDeed && i is SiegeTentBag == false && i is TentBag == false)
                {
                    count++;
                    iGold += RealEstateBroker.ComputePriceFor((HouseDeed)i);
                }
                else if (i is BaseUpgradeContract)
                {
                    count++;
                    iGold += // old code: (int)((BaseUpgradeContract)i).Price;
                             // We need to calulate what the player actually pays, not the embedded deed price, since siege has 3x pricing.
                             // the deed's cost is added to the houses upgrades, which is later refunded.
                        BaseVendor.PlayerPays(((BaseUpgradeContract)i).GetType());
                }
            }
            ArrayList secures = h.Secures;
            foreach (SecureInfo si in secures)
            {
                Container c = (Container)si.Item;
                iGold += GetHouseDeedsInContainer(c, ref count);
            }

            return iGold;
        }


        private static int GetGoldInHouse(BaseHouse h)
        {
            int iGold = 0;

            ArrayList lockdowns = h.LockDowns;
            foreach (Item i in lockdowns)
            {
                if (i is Container)
                {
                    iGold += GetGoldInContainer((Container)i);
                }
                else if (i is Gold)
                {
                    Gold gold = (Gold)i;
                    iGold += gold.Amount;
                }
                else if (i is BankCheck)
                {
                    BankCheck check = (BankCheck)i;
                    iGold += check.Worth;
                }
            }
            ArrayList secures = h.Secures;
            foreach (SecureInfo si in secures)
            {
                Container c = (Container)si.Item;
                iGold += GetGoldInContainer(c);
            }

            return iGold;
        }
    }
}