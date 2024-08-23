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

/* ChangeLog 
 *  8/14/2023, Adam (MatchingRegions)
 *      MatchingRegions verifies all regions match between the player and the banker.
 *      A good example is standing in Wind Park, and yelling over the cave wall there to town to bank.
 *          this.Say("I will not do business with an exploiter!");
 * 1/5/22, Adam (all)
 *      Finished a complete rewrite of the banking system.
 *      Now gold and checks on deposit are treated as 'gold' in the account (just like a real bank.)
 * 12/29/2021, Adam (GetBalanceFromAllEnrolled, GetAccountBalance, ConsumeTotal, GetAccountBalanceBreakdown)
 *      Add new [sharebank command to enroll/disenroll them in the account-wide bankbox sharing.
 *      Implemented: Balance, Check, Withdraw
 *	01/03/07, plasma
 *		Remove all duel challenge system code
 *  10/15/07, Pix
 *      Fixed expired AggressorInfos from preventing banking.
 *  9/1/07, Pix
 *      Fixed aggressor-banking.
 *	8/26/07, Pix
 *		Prevent duelers from accessing their bankbox.
 *	8/22/07, Adam
 *		Add new CashCheck() function to be called from Withdraw() if the resulting check would be less than 5000 gold.
 *	04/16/05, Kitaras
 * 		Added DepositUpTo() function, added ability to deposit from a player or a container
 *	10/16/04, Darva
 *		Added code to prevent players from opening their bank box for 2 minutes after
 * 		Any successful steal.
 */

using Server.ContextMenus;
using Server.Diagnostics;
using Server.Items;
using Server.Misc;
using Server.Network;
using Server.Regions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Server.Mobiles
{
    public class Banker : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }
        public override NpcGuild NpcGuild { get { return NpcGuild.MerchantsGuild; } }
        [Constructable]
        public Banker()
            : base("the banker")
        {
        }
        #region New Banking System
        public static bool CombinedWithdrawFromAllEnrolled(Mobile from, int amount)
        {
            if (from is PlayerMobile pm && pm != null)
            {
                if (pm.ShareBank)
                {
                    int total = 0;
                    List<KeyValuePair<Mobile, List<Item>>> goldTab = new List<KeyValuePair<Mobile, List<Item>>>();
                    List<KeyValuePair<Mobile, List<Item>>> checksTab = new List<KeyValuePair<Mobile, List<Item>>>();

                    CallBankRT(BankRT.START, string.Format("{1} requesting withdrawal of: {0}", amount, from), amount);
                    total = GetCombinedBalanceFromAllEnrolled(from, out goldTab, out checksTab);
                    CallBankRT(BankRT.BALANCE, string.Format("GetBalanceFromAllEnrolled: {0}", total), total);

                    if (total < amount)
                    {
                        CallBankRT(BankRT.ISF, string.Format("Amount requested: {0}", amount), amount);
                        return false;
                    }

                    CallBankRT(BankRT.RECORDING, string.Format("Requesting {0} from {1}", amount, from), amount);

                    int withdrawn = 0;                                                      // logging support

                    ///////////////////////////////////
                    /// Get all that the requestor has first
                    int cb = GetCombinedBalance(from);
                    cb = Math.Min(cb, amount);
                    if (cb > 0)
                    {
                        CallBankRT(BankRT.RECORDING, string.Format("Max withdrawal of {0} from {1}", cb, from), amount - cb);
                        if (CombinedWithdraw(from, cb) == false)
                        {   // logic error - we probably owe this player a refund for cb amount
                            BankingRecorder(string.Format("Error Debiting {0} from {1}", cb, from), ConsoleColor.Red);
                            return false;
                        }
                        amount -= cb;
                        withdrawn += cb;
                    }
                    else
                        BankingRecorder(string.Format("Requestor {0} is broke!", from), ConsoleColor.Green);

                    // Okay, now loop over the shared accounts and make the withdrawals
                    // 'suggested' is what each will try to pay initially
                    int dontcare = 0;                                                       // quotient/suggested is what we will *try* to get
                    int suggested = Math.DivRem(amount, goldTab.Count, out dontcare);       // number of participants
                    if (suggested == 0) suggested = amount;                                 // if they ask for an amount < count - 1, suggested will be 0

                    while (amount > 0)
                    {
                        // first process gold payments
                        foreach (KeyValuePair<Mobile, List<Item>> kvp in goldTab)
                        {
                            if (amount == 0)
                                break;                                                          // all done!

                            for (int i = 0; amount > 0 && i < kvp.Value.Count; ++i)
                            {
                                if (amount == 0)
                                    break;                                                      // all done!

                                if (kvp.Value[i].Deleted)                                       // deleted
                                    continue;

                                int balance = GetAccountGoldBalance(kvp.Key);                   // how much gold they got left in the bank

                                if (balance == 0)
                                {
                                    CallBankRT(BankRT.RECORDING, string.Format("{0} is 'gold' broke!", kvp.Key), amount);
                                    break;                                                      // they're out of gold
                                }

                                int grab = Math.Min(suggested, balance);                        // this is what we will try for
                                grab = Math.Min(grab, kvp.Value[i].Amount);                     // find the minimum to take
                                grab = Math.Min(grab, amount);                                  // <==

                                if (kvp.Value[i].Amount == grab)
                                {
                                    CallBankRT(BankRT.RECORDING, string.Format("Withdrawing {0} from {1}", grab, kvp.Key), amount);
                                    amount -= grab;
                                    withdrawn += grab;
                                    kvp.Value[i].Delete();                                      // delete it
                                }
                                else
                                {
                                    CallBankRT(BankRT.RECORDING, string.Format("Withdrawing {0} from {1}", grab, kvp.Key), amount);
                                    kvp.Value[i].Amount -= grab;
                                    amount -= grab;
                                    withdrawn += grab;
                                }
                            }
                        }

                        // next, process check payments
                        foreach (KeyValuePair<Mobile, List<Item>> kvp in checksTab)
                        {
                            if (amount == 0)
                                break;                                                          // all done!

                            for (int i = 0; amount > 0 && i < kvp.Value.Count; ++i)
                            {
                                BankCheck check = (BankCheck)kvp.Value[i];

                                if (amount == 0)
                                    break;                                                      // all done!

                                if (check.Deleted)                                              // check worth 0
                                    continue;

                                int balance = GetAccountCheckBalance(kvp.Key);                   // how much in checks they got left in the bank

                                if (balance == 0)
                                {
                                    CallBankRT(BankRT.RECORDING, string.Format("{0} is 'check' broke!", kvp.Key), amount);
                                    break;                                                      // they're out of checks
                                }

                                int grab = Math.Min(suggested, balance);                        // this is what we will try for
                                grab = Math.Min(grab, check.Worth);                             // find the minimum to take
                                grab = Math.Min(grab, amount);                                  // <==

                                if (check.Worth == grab)
                                {
                                    CallBankRT(BankRT.RECORDING, string.Format("Cashing {0} from {1}", grab, kvp.Key), amount);
                                    amount -= grab;
                                    withdrawn += grab;
                                    kvp.Value[i].Delete();                                      // delete it
                                }
                                else
                                {
                                    CallBankRT(BankRT.RECORDING, string.Format("Cashing {0} from {1}", grab, kvp.Key), amount);
                                    check.Worth -= grab;
                                    amount -= grab;
                                    withdrawn += grab;
                                }
                            }
                        }
                    }

                    // cleanup 'small' checks
                    foreach (KeyValuePair<Mobile, List<Item>> kvp in checksTab)
                    {
                        for (int i = 0; i < kvp.Value.Count; ++i)
                        {
                            BankCheck check = (BankCheck)kvp.Value[i];
                            // cash this check - don't leave tiny checks in the users bank
                            if (check.Worth < 5000 && check.Deleted == false)
                                CashCheck(kvp.Key, kvp.Value[i] as BankCheck);
                        }
                    }

                    CallBankRT(BankRT.END, string.Format("total withdrawn: {0}", withdrawn), withdrawn);
                }
                else
                    return CombinedWithdraw(from, amount);
            }

            return true;
        }
        private static bool CombinedWithdraw(Mobile from, int amount)
        {
            Item[] gold, checks;
            int balance = 0;
            if (from is PlayerMobile pm && pm != null)
            {
                balance = GetCombinedBalance(from, out gold, out checks);

                if (balance < amount)
                    return false;

                for (int i = 0; amount > 0 && i < gold.Length; ++i)
                {
                    if (gold[i].Deleted == true)
                        continue;

                    if (gold[i].Amount <= amount)
                    {
                        amount -= gold[i].Amount;
                        gold[i].Delete();
                    }
                    else
                    {
                        gold[i].Amount -= amount;
                        amount = 0;
                    }
                }

                for (int i = 0; amount > 0 && i < checks.Length; ++i)
                {
                    if (checks[i].Deleted == true)
                        continue;

                    BankCheck check = (BankCheck)checks[i];

                    if (check.Worth <= amount)
                    {
                        amount -= check.Worth;
                        check.Delete();
                    }
                    else
                    {
                        check.Worth -= amount;
                        amount = 0;

                        // cash this check - don't leave tiny checks in the users bank
                        if (check.Worth < 5000)
                            CashCheck(from, check);
                    }
                }

                return true;
            }
            else
                return false;
        }
        public static int GetAccessibleBalance(Mobile from)
        {
            if (from is PlayerMobile pm && pm != null)
            {
                if (pm.ShareBank)
                {
                    return GetCombinedBalanceFromAllEnrolled(from);
                }
                else
                {
                    Item[] gold, checks;
                    return GetCombinedBalance(from, out gold, out checks);
                }
            }

            return 0;
        }
        public static int GetCombinedBalance(Mobile from)
        {
            Item[] gold, checks;
            return GetCombinedBalance(from, out gold, out checks);
        }
        public static int GetCombinedBalance(Mobile from, out Item[] gold, out Item[] checks)
        {
            int balance = 0;

            Container bank = from.BankBox;

            if (bank != null)
            {
                gold = bank.FindItemsByType(typeof(Gold));
                checks = bank.FindItemsByType(typeof(BankCheck));

                for (int i = 0; i < gold.Length; ++i)
                    if (gold[i].Deleted == false)
                        balance += gold[i].Amount;

                for (int i = 0; i < checks.Length; ++i)
                    if (checks[i].Deleted == false)
                        balance += ((BankCheck)checks[i]).Worth;
            }
            else
            {
                gold = checks = new Item[0];
            }

            return balance;
        }
        public static int GetGoldBalanceFromAllEnrolled(Mobile from)
        {   // Get Balance From players on this account that have enrolled in bankbox sharing
            int total = 0;
            if (from is PlayerMobile pm && pm != null)
            {
                // you cannot share other's bank if you yourself are not sharing
                if (pm.ShareBank == false)
                    return 0;

                Server.Accounting.Account acct = (Server.Accounting.Account)pm.Account;
                for (int i = 0; i < 5; ++i)
                {
                    if (acct[i] is PlayerMobile target)
                    {
                        if (target != null)
                        {
                            if (target.ShareBank)
                            {
                                if (target.BankBox != null)
                                {
                                    Item[] items = target.BankBox.FindItemsByType(typeof(Gold), true);

                                    for (int jx = 0; jx < items.Length; ++jx)
                                        if (items[jx].Deleted == false)
                                            total += items[jx].Amount;
                                }
                            }
                        }
                    }
                }
            }

            return total;
        }
        public static int GetCombinedBalanceFromAllEnrolled(Mobile from)
        {
            List<KeyValuePair<Mobile, List<Item>>> gold = new List<KeyValuePair<Mobile, List<Item>>>();
            List<KeyValuePair<Mobile, List<Item>>> checks = new List<KeyValuePair<Mobile, List<Item>>>();
            return GetCombinedBalanceFromAllEnrolled(from, out gold, out checks);
        }
        public static int GetCombinedBalanceFromAllEnrolled(Mobile from, out List<KeyValuePair<Mobile, List<Item>>> gold, out List<KeyValuePair<Mobile, List<Item>>> checks)
        {   // Get Balance From players on this account that have enrolled in bankbox sharing
            int balance = 0;
            gold = new List<KeyValuePair<Mobile, List<Item>>>();
            checks = new List<KeyValuePair<Mobile, List<Item>>>();
            if (from is PlayerMobile pm && pm != null)
            {
                // you cannot share other's bank if you yourself are not sharing
                if (pm.ShareBank == false)
                    return 0;

                Server.Accounting.Account acct = (Server.Accounting.Account)pm.Account;
                for (int i = 0; i < 5; ++i)
                {
                    if (acct[i] is PlayerMobile target)
                    {
                        if (target != null)
                        {
                            if (target.ShareBank)
                            {
                                if (target.BankBox != null)
                                {
                                    Item[] theGold, theChecks;
                                    balance += GetCombinedBalance(target, out theGold, out theChecks);
                                    gold.Add(new KeyValuePair<Mobile, List<Item>>(target, theGold.ToList()));
                                    checks.Add(new KeyValuePair<Mobile, List<Item>>(target, theChecks.ToList()));
                                }
                            }
                        }
                    }
                }
            }

            return balance;
        }
        public static int GetAccountGoldBalance(Mobile from)
        {
            int total = 0;
            if (from is PlayerMobile pm && pm != null)
            {
                if (pm.BankBox != null)
                {
                    Item[] items = pm.BankBox.FindItemsByType(typeof(Gold), true);

                    for (int jx = 0; jx < items.Length; ++jx)
                        if (items[jx].Deleted == false)
                            total += items[jx].Amount;
                }
            }

            return total;
        }
        public static int GetAccountCheckBalance(Mobile from)
        {
            int total = 0;
            if (from is PlayerMobile pm && pm != null)
            {
                if (pm.BankBox != null)
                {
                    Item[] items = pm.BankBox.FindItemsByType(typeof(BankCheck), true);

                    for (int jx = 0; jx < items.Length; ++jx)
                    {
                        BankCheck check = (BankCheck)items[jx];
                        if (check.Deleted == false)
                            total += check.Worth;
                    }
                }
            }

            return total;
        }
        #endregion New Banking System
        public static bool Check(Mobile banker, Mobile m, int amount)
        {
            BankBox box = m.BankBox;
            if (box != null)
            {
                BankingRecorder(string.Format("Check Request for {0} from {1}", amount, m), ConsoleColor.Yellow, amount);

                // dummy to check box capacity
                BankCheck check = new BankCheck(1);

                if (!box.CheckHold(m, check, false))
                {
                    BankingRecorder(string.Format("Check Request Failed: Bank Box full"), ConsoleColor.Red);
                    if (banker != null)
                        banker.Say(500386); // There's not enough room in your bankbox for the check!
                    check.Delete();
                    return false;
                }

                // delete the dummy check so it's not counted
                check.Delete();

                if (!CombinedWithdrawFromAllEnrolled(m, amount))
                {
                    BankingRecorder(string.Format("Check Request Failed: Insufficient Funds"), ConsoleColor.Yellow);
                    if (banker != null)
                        banker.Say(500384); // Ah, art thou trying to fool me? Thou hast not so much gold!
                    return false;
                }
                else
                {
                    BankingRecorder(string.Format("Check Request for {0} Complete for {1}", amount, m), ConsoleColor.Yellow, amount);
                    box.DropItem(new BankCheck(amount));
                    if (banker != null)
                        banker.Say(1042673, AffixType.Append, amount.ToString(), ""); // Into your bank box I have placed a check in the amount of:
                    return true;
                }
            }

            return false;
        }
        public static void CashCheck(Mobile from, BankCheck check)
        {
            if (check.Deleted == true)
                return;

            BankBox box = from.BankBox;

            if (box != null)
            {
                int deposited = 0;
                int toAdd = check.Worth;

                check.Delete();
                Gold gold;

                while (toAdd > 60000)
                {
                    gold = new Gold(60000);

                    if (box.TryDropItem(from, gold, false))
                    {
                        toAdd -= 60000;
                        deposited += 60000;
                    }
                    else
                    {
                        gold.Delete();

                        from.AddToBackpack(new BankCheck(toAdd));
                        toAdd = 0;

                        break;
                    }
                }

                if (toAdd > 0)
                {
                    gold = new Gold(toAdd);

                    if (box.TryDropItem(from, gold, false))
                    {
                        deposited += toAdd;
                    }
                    else
                    {
                        gold.Delete();

                        from.AddToBackpack(new BankCheck(toAdd));
                    }
                }
            }
        }
        public static bool Deposit(Mobile from, int amount)
        {
            if (from != null)
            {
                BankBox box = from.BankBox;

                Item item = (amount >= 1000 ? (Item)new BankCheck(amount) : (Item)new Gold(amount));

                if (box != null && box.TryDropItem(from, item, false))
                    return true;

                item.Delete();
            }
            return false;
        }
        public static int DepositUpTo(Mobile from, int amount)
        {
            BankBox box = from.BankBox;
            if (box == null)
                return 0;

            int amountLeft = amount;
            while (amountLeft > 0)
            {
                Item item;
                int amountGiven;

                if (amountLeft < 5000)
                {
                    item = new Gold(amountLeft);
                    amountGiven = amountLeft;
                }
                else if (amountLeft <= 1000000)
                {
                    item = new BankCheck(amountLeft);
                    amountGiven = amountLeft;
                }
                else
                {
                    item = new BankCheck(1000000);
                    amountGiven = 1000000;
                }

                if (box.TryDropItem(from, item, false))
                {
                    amountLeft -= amountGiven;
                }
                else
                {
                    item.Delete();
                    break;
                }
            }

            return amount - amountLeft;
        }
        public static void Deposit(Container cont, int amount)
        {
            while (amount > 0)
            {
                Item item;

                if (amount < 5000)
                {
                    item = new Gold(amount);
                    amount = 0;
                }
                else if (amount <= 1000000)
                {
                    item = new BankCheck(amount);
                    amount = 0;
                }
                else
                {
                    item = new BankCheck(1000000);
                    amount -= 1000000;
                }

                cont.DropItem(item);
            }
        }
        public Banker(Serial serial)
            : base(serial)
        {
        }
        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(this.Location, 12))
                return true;

            return base.HandlesOnSpeech(from);
        }
        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && e.Mobile.InRange(this.Location, 12))
            {   // 8/14/2023, Adam: Avoid certain exploits with MatchingRegions (ignore township bankers)
                //  Exploit: player in Wind Park shouts over rocks to Wind Town to bank.
                if (TownshipRegion.GetTownshipAt(e.Mobile) == null && !Region.MatchingRegions(this, e.Mobile))
                {
                    this.Say("I will not do business with an exploiter!");
                    return;
                }
                for (int i = 0; i < e.Keywords.Length; ++i)
                {
                    int keyword = e.Keywords[i];

                    switch (keyword)
                    {
                        case 0x0000: // *withdraw*
                            {
                                e.Handled = true;

                                if (e.Mobile.Criminal)
                                {
                                    this.Say(500389); // I will not do business with a criminal!
                                    break;
                                }

                                string[] split = e.Speech.Split(' ');

                                if (split.Length >= 2)
                                {
                                    int amount;

                                    try
                                    {
                                        amount = Convert.ToInt32(split[1]);
                                    }
                                    catch
                                    {
                                        break;
                                    }

                                    if (amount > 5000)
                                    {
                                        this.Say(500381); // Thou canst not withdraw so much at one time!
                                    }
                                    else if (amount > 0)
                                    {
                                        // New shared bankbox implementation
                                        if (!CombinedWithdrawFromAllEnrolled(e.Mobile, amount))
                                        {
                                            this.Say(500384); // Ah, art thou trying to fool me? Thou hast not so much gold!
                                        }
                                        else
                                        {
                                            e.Mobile.AddToBackpack(new Gold(amount));
                                            this.Say(1010005); // Thou hast withdrawn gold from thy account.
                                        }
                                    }
                                }

                                break;
                            }   // *withdraw*
                        case 0x0001: // *balance*
                            {
                                e.Handled = true;

                                if (e.Mobile.Criminal)
                                {
                                    this.Say(500389); // I will not do business with a criminal!
                                    break;
                                }

                                BankBox box = e.Mobile.BankBox;

                                if (box != null)
                                {
                                    // publicly announce this character's total gold balance (includes checks as gold)
                                    int balance = GetAccountGoldBalance(e.Mobile);
                                    balance += GetAccountCheckBalance(e.Mobile);
                                    this.Say(1042759, balance.ToString("N0") /*balance.ToString()*/); // Thy current bank balance is ~1_AMOUNT~ gold.
                                }
                                if (e.Mobile is PlayerMobile pm)
                                {
                                    if (pm != null)
                                    {
                                        if (pm.ShareBank)
                                        {
                                            // privately 'SayTo' this account's total gold balance (includes checks as gold)
                                            //  for all enrolled characters.
                                            int balance = GetCombinedBalanceFromAllEnrolled(e.Mobile);
                                            this.SayTo(e.Mobile, string.Format("Thy balance for all players on this account is {0} gold.", balance));
                                        }
                                    }
                                }

                                break;
                            }   // *balance*
                        case 0x0002: // *bank*
                            {
                                e.Handled = true;

                                if (e.Mobile.Criminal)
                                {
                                    this.Say(500378); // Thou art a criminal and cannot access thy bank box.
                                    break;
                                }
                                else if (Server.SkillHandlers.Stealing.HasHotItem(e.Mobile))
                                {
                                    this.Say("Thou'rt a thief and cannot access thy bank box.");
                                    break;
                                }
                                else if (IsAggressor(e.Mobile, player: true))
                                {
                                    this.Say("Thou seemest too busy fighting and cannot access thy bank box");
                                    break;
                                }

                                BankBox box = e.Mobile.BankBox;

                                if (box != null)
                                    box.Open();

                                break;
                            }   // *bank*
                        case 0x0003: // *check*
                            {
                                e.Handled = true;

                                if (e.Mobile.Criminal)
                                {
                                    this.Say(500389); // I will not do business with a criminal!
                                    break;
                                }

                                string[] split = e.Speech.Split(' ');

                                if (split.Length >= 2)
                                {
                                    int amount;

                                    try
                                    {
                                        amount = Convert.ToInt32(split[1]);
                                    }
                                    catch
                                    {
                                        break;
                                    }

                                    if (amount < 5000)
                                    {
                                        this.Say(1010006); // We cannot create checks for such a paltry amount of gold!
                                    }
                                    else if (amount > 1000000)
                                    {
                                        this.Say(1010007); // Our policies prevent us from creating checks worth that much!
                                    }
                                    else
                                    {   // try to make the check
                                        Check(this, e.Mobile, amount);
                                    }
                                }

                                break;
                            }   // *check*
                    }
                }
            }

            base.OnSpeech(e);
        }
        public override bool IsAggressor(Mobile m, bool player = false)
        {
            foreach (AggressorInfo info in m.Aggressed)
            {
                if (!info.Expired && info.Attacker == m && (player ? info.Defender.Player : true))
                    return true;
            }
            return false;
        }
        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            if (from.Alive)
                list.Add(new OpenBankEntry(from, this));

            base.AddCustomContextEntries(from, list);
        }
        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBBanker());
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        #region Banking Regression Tests
        public enum BankRT
        {
            ISF,            // insufficient funds
            BALANCE,
            START,
            RECORDING,
            END
        }
        private static int m_requested_value;
        private static int m_value_withdrawn;
        private static int m_balance;
        private static readonly object mutex = new object();
        public static LogHelper Logger
        {
            get
            {
                lock (mutex)
                {
                    return m_logger;
                }
            }
        }
        private static void BankingRecorderStop()
        {
            lock (mutex)
            {
                m_logger.Finish();
                m_logger = null;
            }
        }
        private static void BankingRecorderStart()
        {
            lock (mutex) { m_logger = new LogHelper("bank regression results.log", true); }
        }
        private static void BankingRecorder(string text, ConsoleColor color)
        {
            Console.Out.Flush();
            Utility.Monitor.WriteLine(text, color);
            lock (mutex)
            {   // regression test log
                if (Logger != null)
                    Logger.Log(text);
                else
                {   // normal transaction logger
                    LogHelper logger = new LogHelper("bank transactions.log", false, true);
                    logger.Log(text);
                    logger.Finish();
                }
            }
        }
        private static void BankingRecorder(string format, ConsoleColor color, params object[] args)
        {
            BankingRecorder(string.Format(format, args), color);
        }
        private static void CallBankRT(BankRT state, string text, int data)
        {
            if (state == BankRT.START)
            {   // banner
                string msg = "Job start.";
                BankingRecorder(msg, ConsoleColor.White);
            }

            // output
            BankingRecorder(text, ConsoleColor.Green);

            ////////
            /// Process messages
            ////////
            if (state == BankRT.START)
            {
                m_requested_value = data;
            }
            if (state == BankRT.BALANCE)
            {
                m_balance = data;
            }
            if (state == BankRT.ISF)
            {
                if (m_balance >= m_requested_value)
                {
                    string complaint = string.Format("Error: We have funds({0}) > requested amount({1}).", m_balance, m_requested_value);
                    BankingRecorder(complaint, ConsoleColor.Red);
                    string message = "Job complete with errors.";
                    BankingRecorder(message, ConsoleColor.White);
                }
                else
                {
                    string stringOut = "Insufficient funds correctly detected.";
                    BankingRecorder(stringOut, ConsoleColor.Green);
                    string message = "Job complete.";
                    BankingRecorder(message, ConsoleColor.White);
                }
                m_running = false;
            }
            else if (state == BankRT.RECORDING)
            {

            }
            else if (state == BankRT.END)
            {
                m_value_withdrawn = data;
                if (m_requested_value != m_value_withdrawn)
                {
                    string complaint = "Error: Requested amount != amount returned.";
                    BankingRecorder(complaint, ConsoleColor.Red);
                    string message = "Job complete with errors.";
                    BankingRecorder(message, ConsoleColor.White);
                }
                else
                {
                    string message = "Job complete.";
                    BankingRecorder(message, ConsoleColor.White);
                }
                m_running = false;
            }
        }

        private static ProcessTimer m_ProcessTimer = null;
        private static LogHelper m_logger = null;
        private static bool m_running = false;
        private static List<PlayerMobile> m_mobiles = new List<PlayerMobile>();

        private class ProcessTimer : Timer
        {
            Mobile m_mobile;
            int m_regressionCount;
            public ProcessTimer(Mobile m, int loops)
                : base(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(1.0))
            {
                m_mobile = m;
                m_regressionCount = loops;

                Priority = TimerPriority.FiftyMS;
                System.Console.WriteLine("Regression timer started.");
            }
            protected override void OnTick()
            {
                try
                {
                    // m_running == false prevents us from closing down the system when there 
                    //  may still be a delay-call in the queue
                    if (m_running == false)
                    {
                        if (m_regressionCount-- <= 0)
                        {
                            Console.WriteLine("m_regressionCount:{0}", m_regressionCount);
                            string text = "BankRT complete.";
                            m_mobile.SendMessage(text);
                            // stop timer, cleanup dummy account
                            AllStop();
                            // cleanup log file
                            BankingRecorder(text, ConsoleColor.White);
                            BankingRecorderStop();
                            return;
                        }
                    }
                    else
                        Console.WriteLine("m_regressionCount:{0}", m_regressionCount);

                    if (m_running == false)
                    {
                        SetupJob();
                        m_running = true;
                    }
                    //else
                    //BankingRecorder("Ignoring duplicate request to start job.", ConsoleColor.Red);

                }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    System.Console.WriteLine("Exception Caught in Regression timer: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                }
            }
            protected void SetupJob()
            {
                string text = "New job starting...";
                BankingRecorder(text, ConsoleColor.White);

                // clean thier bank box
                for (int ix = 0; ix < m_mobiles.Count; ix++)
                {
                    m_mobiles[ix].BankBox.Delete();
                    m_mobiles[ix].BankBox = new BankBox(m_mobiles[ix]);
                }

                // fill their bankbox
                int funds = 0;
                for (int ix = 0; ix < m_mobiles.Count; ix++)
                {
                    int amount = Utility.RandomMinMax(10000, 50000);
                    funds += (amount / 2) * 2;
                    if ((amount / 2) * 2 == 0)
                    {
                        ix--;
                        continue;
                    }
                    BankCheck bc = new BankCheck(amount / 2);
                    Gold gld = new Gold(amount / 2);
                    m_mobiles[ix].BankBox.AddItem(bc);
                    m_mobiles[ix].BankBox.AddItem(gld);
                }

                if (Banker.GetAccessibleBalance(m_mobiles[0]) == funds)
                {
                    text = string.Format("Our calculated funds are {0}", funds);
                    BankingRecorder(text, ConsoleColor.White);
                }
                else
                {
                    text = string.Format("Error: funding calculation mismatch {0}/{1}", funds, Banker.GetAccessibleBalance(m_mobiles[0]));
                    BankingRecorder(text, ConsoleColor.Red);
                }

                // okay, account created, characters created, backpacks filled, ... launch!
                Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(Tick), new object[] { null });
            }
        }
        private static void Tick(object state)
        {
            // RandomMinMax(10000, 50000) is what we gave each of the 4 PlayerMobiles.
            //  so RandomMinMax(10000*4, 50000*4) will give is a reasonable likihood of both
            //  withdrawl success and failure.
            if (m_running)
            {
                if (Utility.RandomBool())
                    Banker.Check(null, m_mobiles[0], Utility.RandomMinMax(10000 * 4, 50000 * 4));
                Banker.CombinedWithdrawFromAllEnrolled(m_mobiles[0], Utility.RandomMinMax(10000 * 4, 50000 * 4));
            }
        }
        private static void AllStop()
        {
            m_running = false;
            m_ProcessTimer.Stop();
            m_ProcessTimer.Running = false;
            m_ProcessTimer = null;
            m_dummyAccount.Delete();
            m_mobiles.Clear();
        }
        public new static void Initialize()
        {
            Server.CommandSystem.Register("BankRT", AccessLevel.Administrator, new CommandEventHandler(BankRT_OnCommand));
        }
        private static Accounting.Account m_dummyAccount = null;
        [Usage("BankRT")]
        [Description("Banking regression tests for Shared Bankboxes.")]
        public static void BankRT_OnCommand(CommandEventArgs e)
        {
            if (m_ProcessTimer != null)
            {
                string text = "Stopping banking regression tests.";
                e.Mobile.SendMessage(text);
                // stop timer, cleanup dummy account
                AllStop();
                // cleanup log file
                BankingRecorder(text, ConsoleColor.White);
                BankingRecorderStop();
            }
            else
            {
                string text = "Starting banking regression tests...";
                e.Mobile.SendMessage(text);
                // create the dummy account
                m_dummyAccount = new Accounting.Account("username", "password");
                for (int ix = 0; ix < 4; ix++)
                {   // this is our canned list of participants
                    PlayerMobile m = CharacterCreation.CreateMobile(m_dummyAccount) as PlayerMobile;
                    m.BankBox = new BankBox(m);
                    m.ShareBank = true;
                    m.Name = "Jimmy " + (ix + 1).ToString();
                    m_mobiles.Add(m);
                }
                BankingRecorderStart();
                BankingRecorder(text, ConsoleColor.White);

                m_ProcessTimer = new ProcessTimer(e.Mobile, 100);
                m_ProcessTimer.Start();
            }
        }
        #endregion Banking Regression Tests
    }
}