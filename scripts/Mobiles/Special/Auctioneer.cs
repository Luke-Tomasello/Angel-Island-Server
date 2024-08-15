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

/* Scripts\Mobiles\Special\Auctioneer.cs
 * ChangeLog
 * 7/23/10, Pix
 *      Set many other functions to protected.
 *      Reworked the Running proerties and how auctions are started. (which affected how auctions stop in the timer)
 * 7/15/10, Pix
 *      Set GetAuctionItem to protected so township auctioneers can access the method.
 *	8/28/07, Adam
 *		fix the 'time 'till the auction ends' display
 *	8/22/07, Adam
 *		Ready for live
 *  8/16/07, Adam
 *		All features but serialization in
 *  8/14/07, Adam
 *      Initial checkin
 */

using Server.Diagnostics;
using Server.Items;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class Auctioneer : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }
        private enum Bids { BID_MAX = 50000000 }
        private AuctionTimer m_Timer;

        private int m_HighBid = 0;
        [CommandProperty(AccessLevel.Administrator)]
        public int HighBid
        {
            get { return m_HighBid; }
        }

        private int m_BidIncrement = 1000;
        [CommandProperty(AccessLevel.Administrator)]
        public int BidIncrement
        {
            get { return m_BidIncrement; }
            set { m_BidIncrement = value; }
        }

        private int m_StartingBid = 1000;
        [CommandProperty(AccessLevel.Administrator)]
        public int StartingBid
        {
            get { return m_StartingBid; }
            set { m_StartingBid = value; }
        }

        private Serial m_HighBidMobile = 0x0; // 1st form of ID
        [CommandProperty(AccessLevel.Administrator)]
        public Serial HighBidMobile
        {
            get { return m_HighBidMobile; }
        }

        private Serial m_HighBidMobileHC = 0; // 2nd form of ID
        [CommandProperty(AccessLevel.Administrator)]
        public Serial HighBidMobileHC
        {
            get { return m_HighBidMobileHC; }
        }

        private TimeSpan m_AuctionEnds;
        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan AuctionEnds
        {
            get { return m_AuctionEnds; }
            set { m_AuctionEnds = value; }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool Running
        {
            get
            {
                return AuctionStartFlag;
            }
            set
            {
                //we only do something in set if we're going from false to true.
                //To cancel an auction, use AuctionControl
                if (value == true && AuctionStartFlag == false)
                {
                    bool result = m_AuctionEnds.TotalSeconds > 0;
                    result = result && Backpack != null && Backpack.Items.Count == 1;
                    if (result == true)
                        if (AuctionStartFlag == false)
                            OnAuctionStart();
                }
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool AdminStartAuction
        {
            get { return false; }
            set
            {
                bool result = m_AuctionEnds.TotalSeconds > 0;
                result = result && Backpack != null && Backpack.Items.Count == 1;
                if (result == true)
                    if (AuctionStartFlag == false)
                        OnAuctionStart();
            }
        }

        private bool m_AuctionStartFlag = false;
        [CommandProperty(AccessLevel.Administrator)]
        public bool AuctionStartFlag
        {
            get
            {
                return m_AuctionStartFlag;
            }
        }

        private int m_GoldDeleted = 0;
        [CommandProperty(AccessLevel.Administrator)]
        public int GoldDeleted
        {
            get
            {
                return m_GoldDeleted;
            }
        }

        public enum AuctionState { Normal, Cancel_NoRefund, Cancel_Refund }
        [CommandProperty(AccessLevel.Administrator)]
        public AuctionState AuctionControl
        {
            get { return AuctionState.Normal; }

            set
            {
                if (Running)
                    switch (value)
                    {
                        case AuctionState.Normal:
                            break;
                        case AuctionState.Cancel_NoRefund:
                            LogEvent("Auction canceled without a refund");
                            this.Say("The auction has been canceled.");
                            ResetAuctionState();
                            break;
                        case AuctionState.Cancel_Refund:
                            LogEvent("Auction canceled with a refund");
                            this.Say("The auction has been canceled.");
                            RefundBid();
                            ResetAuctionState();
                            break;
                    }
            }
        }

        [Constructable]
        public Auctioneer()
            : base("the auctioneer")
        {
            m_Timer = new AuctionTimer(this);
            m_Timer.Start();
        }

        public Auctioneer(Serial serial)
            : base(serial)
        {
            m_Timer = new AuctionTimer(this);
            m_Timer.Start();
        }

        public override void InitSBInfo()
        {
            // we sell nothing
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            if (Backpack == null)
            {
                Container pack = new Backpack();
                pack.Movable = false;
                AddItem(pack);
            }

            Item hair = this.FindItemOnLayer(Layer.Hair);
            if (hair != null)
                hair.Delete();

            Item FacialHair = this.FindItemOnLayer(Layer.FacialHair);
            if (FacialHair != null)
                FacialHair.Delete();

            if (Utility.RandomBool())
                AddItem(new PonyTail(GetHairHue()));
            else
                AddItem(new LongHair(GetHairHue()));
        }

        public override void InitLoot()
        {   /* no loot */
        }

        public override void InitBody()
        {
            base.InitBody();
            SpeechHue = 33;
            Hue = 33770;
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (from.AccessLevel >= AccessLevel.Seer)
            {
                Container cont = this.Backpack;
                if (cont != null)
                {
                    if (Backpack.Items.Count == 0)
                    {
                        cont.DropItem(dropped);
                        return true;
                    }
                    else
                    {
                        this.SayTo(from, "I can only handle one auction item at a time.");
                    }
                }
            }

            return base.OnDragDrop(from, dropped);
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(this.Location, 2))
                return true;

            return base.HandlesOnSpeech(from);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (e.Mobile == null || e.Speech == null)
                return;

            int value = 0;
            switch (psParse(e.Mobile, e.Speech.ToLower(), out value))
            {
                case "help": psHelp(); break;
                case "i wish to bid": psBid(e.Mobile, value); break;
                case "current bid":
                case "high bid":
                case "current price":
                case "price": psHighBid(); break;
                case "item":
                case "auction": psItem(); break;
                case "time": psTime(); break;
                case "%%no auction": psNoAuction(); break;
                case "%%done": break;
                default: psDefault(); break;
            }
        }

        public void OnAuctionStart()
        {   // called once at the start of each auction
            m_AuctionStartFlag = true;
            Item item = GetAuctionItem();

            // it is an error to start an auction without an auction item
            if (Misc.Diagnostics.Assert(item != null, "item == null", Utility.FileInfo()))
            {
                LogHelper lh = new LogHelper(GetLogName(), false, true);
                string ItemName = null;
                if (item != null)
                    if (item.Name != null)
                        ItemName = item.Name;
                    else
                        ItemName = item.GetType().Name;

                lh.Log(LogType.Text, string.Format("** New Auction started for item {0}**", ItemName));
                lh.Log(LogType.Item, item);
                lh.Finish();
            }
        }

        protected void LogEvent(string text)
        {
            Item item = GetAuctionItem();
            LogHelper lh = new LogHelper(GetLogName(), false, true);
            if (item != null)
                lh.Log(LogType.Item, item, text);
            else
                lh.Log(LogType.Text, text);
            lh.Finish();
        }

        private string GetLogName()
        {
            Item item = GetAuctionItem();
            if (Running == true && item != null)
                return string.Format("Auction({0}).log", item.Serial.ToString());
            else
                return string.Format("Auction.log");
        }

        public virtual void OnAuctionFinish()
        {
            this.Say(string.Format("The auction has finished."));
            PlayerMobile pm = World.FindMobile(m_HighBidMobile) as PlayerMobile;
            if (CheckID() && pm != null)
            {
                this.Say(string.Format("The winning bid was placed by {0} for {1} gold.", pm.Name, m_HighBid));
                this.Say("Congratulations!");

                BankBox box = pm.BankBox;
                if (Misc.Diagnostics.Assert(box != null, "box == null", Utility.FileInfo()))
                {
                    Item item = GetAuctionItem();
                    if (item != null)
                    {
                        LogEvent(string.Format("OnAuctionFinish() : winner: {0}, amount: {1} gold.", pm, m_HighBid));
                        Backpack.RemoveItem(item);
                        box.AddItem(item);
                        m_GoldDeleted += m_HighBid;
                    }
                }
            }
            else
            {
                LogEvent(string.Format("OnAuctionFinish() : There were no bidders in this auction."));
                this.Say("There were no bidders in this auction.");
            }

            // reset auction state
            ResetAuctionState();
        }

        protected Item GetAuctionItem()
        {
            Item item = null;
            if (Misc.Diagnostics.Assert(Backpack != null, "Backpack == null", Utility.FileInfo()))
                if (Backpack.Items.Count == 1)
                    item = Backpack.Items[0] as Item;

            return item;
        }

        protected void ResetAuctionState()
        {
            // reset auction state
            m_HighBidMobileHC = 0;
            m_HighBid = 0;
            m_HighBidMobile = 0;
            m_AuctionStartFlag = false;
            m_AuctionEnds = TimeSpan.Zero;
        }

        private void psTime()
        {
            if (m_AuctionEnds.TotalHours >= 24)
                this.Say(string.Format("The auction will end in about {0} day{1} and {2} hour{3}.",
                    m_AuctionEnds.Days, m_AuctionEnds.Days == 1 ? "" : "s",
                    m_AuctionEnds.Hours, m_AuctionEnds.Hours == 1 ? "" : "s"));
            else if (m_AuctionEnds.TotalMinutes >= 60)
                this.Say(string.Format("The auction will end in about {0} hour{1} and {2} minute{3}.",
                    m_AuctionEnds.Hours, m_AuctionEnds.Hours == 1 ? "" : "s",
                    m_AuctionEnds.Minutes, m_AuctionEnds.Minutes == 1 ? "" : "s"));
            else
                this.Say(string.Format("The auction will end in about {0} minute{1}.",
                    m_AuctionEnds.Minutes, m_AuctionEnds.Minutes == 1 ? "" : "s"));
        }

        private void psItem()
        {
            this.Say(string.Format("The goods are im my backpack, please have a look."));
        }

        private void psNoAuction()
        {
            this.Say(string.Format("I am not currently running any auctions."));
        }

        private void psHighBid()
        {
            if (m_HighBid == 0 || World.FindMobile(m_HighBidMobile) == null)
            {
                this.Say(string.Format("The starting bid is at least {0} gold.", m_StartingBid));
            }
            else
            {
                this.Say(string.Format("The current high bid is {0} gold by {1}.", m_HighBid, World.FindMobile(m_HighBidMobile).Name));
            }
        }

        private string psParse(Mobile m, string text, out int value)
        {
            string IWishToBid = "i wish to bid";
            string Bid = "bid";
            string Help = "help";
            value = 0;

            // no auction at this time .. ignore commands but 'help'
            if (!text.StartsWith(Help) && Running == false)
            {
                return "%%no auction";
            }

            // I wish to bid
            if (text.StartsWith(IWishToBid))
            {
                bool result = text.Length > IWishToBid.Length &&
                    Int32.TryParse(text.Substring(IWishToBid.Length + 1), out value);
                if (result == false)
                    return "help";

                return IWishToBid;

            }
            // Bid
            if (text.StartsWith(Bid))
            {
                bool result = text.Length > Bid.Length &&
                    Int32.TryParse(text.Substring(Bid.Length + 1), out value);
                if (result == false)
                    return "help";

                return IWishToBid;

            }
            else
                return text;
        }

        private void RefundBid()
        {
            Mobile m = World.FindMobile(m_HighBidMobile);
            if (CheckID() && m != null)
            {
                // deposit the refund
                BankBox box = m.BankBox;
                //Item item = (m_HighBid >= 1000 ? (Item)new BankCheck(m_HighBid) : (Item)new Gold(m_HighBid));
                if (Misc.Diagnostics.Assert(box != null, "box == null", Utility.FileInfo()))
                {
                    //box.AddItem(item);
                    Banker.Deposit(box, m_HighBid);
                    LogEvent(string.Format("RefundBid() : player: {0}, amount: {1} gold", m, m_HighBid));
                }
            }
        }

        private bool ValidateBidder(Mobile bidder)
        {
            if (bidder.Serial == m_HighBidMobile)
            {
                SayTo(bidder, "You cannot bit against yourself silly!");
                return false;
            }

            return true;
        }

        private bool PlaceBid(Mobile bidder, int totalCost)
        {
            bool bought = false;

            if (ValidateBidder(bidder) == false)
                return false;

            Container cont = bidder.Backpack;
            if (cont != null)
            {
                if (cont.ConsumeTotal(typeof(Gold), totalCost))
                {
                    bought = true;
                }
            }

            if (!bought)
            {
                if (Banker.CombinedWithdrawFromAllEnrolled(bidder, totalCost))
                {
                    bought = true;
                }
                else
                {
                    SayTo(bidder, 500191); //Begging thy pardon, but thy bank account lacks these funds.
                }
            }

            if (bought)
            {
                bidder.PlaySound(0x32);
            }

            return bought;
        }

        private void psHelp()
        {
            this.Say("just say 'i wish to bid <amount>'");
            this.Say("or 'high bid' if you want to see the current high bid");
            this.Say("or 'item' or 'auction' if you would like to see what's for sale");
        }

        private void psBid(Mobile m, int value)
        {
            // bid too high
            if (value > (int)Bids.BID_MAX)
            {
                this.Say(string.Format("You may not bid more than {0} gold.", (int)Bids.BID_MAX));
                return;
            }

            // bid too low
            if (value < m_BidIncrement)
            {
                this.Say(string.Format("You must bid at least {0} gold.", m_BidIncrement));
                return;
            }

            // first bid
            if (m_HighBid == 0)
            {   // first bid too low
                if (value < m_StartingBid)
                {
                    this.Say(string.Format("You must start the bidding at {0} gold.", m_StartingBid));
                    return;
                }

                // okay, we have a good first bid. check/get gold
                if (PlaceBid(m, value))
                {
                    m_HighBid = value;
                    m_HighBidMobile = m.Serial;
                    m_HighBidMobileHC = m.Account != null ? m.Account.ToString().GetHashCode() : 0;
                    LogEvent(string.Format("PlaceBid() : bidder: {0}, amount: {1} gold.", m, m_HighBid));
                    this.Say(string.Format("Thank you {1} for our starting bid of {0} gold.", m_HighBid, m.Name));
                    UpdateTimer();
                    return;
                }

                return;
            }

            // bid too low
            if (value < m_HighBid + m_BidIncrement)
            {
                this.Say(string.Format("You must bid at least {0} gold.", m_HighBid + m_BidIncrement));
                return;
            }

            // okay, we have a good  bid. check/get gold
            if (PlaceBid(m, value))
            {
                RefundBid();
                m_HighBid = value;
                m_HighBidMobile = m.Serial;
                m_HighBidMobileHC = m.Account != null ? m.Account.ToString().GetHashCode() : 0;
                LogEvent(string.Format("PlaceBid() : bidder: {0}, amount: {1} gold.", m, m_HighBid));
                this.Say(string.Format("Thank you {1} for our new high bid of {0} gold.", m_HighBid, m.Name));
                UpdateTimer();
                return;
            }

            // they did not have the funds
            return;
        }

        private void psDefault()
        {
            this.Say("Hmm?");
            this.Say("If you need help, just say so."); ;
        }

        private void UpdateTimer()
        {   // Add five minutes to the timer if we get sniped
            if (Running && m_AuctionEnds.TotalMinutes <= 5)
            {
                m_AuctionEnds += TimeSpan.FromMinutes(5);
                this.Say(string.Format("I'll extend the bidding by 5 minutes for a total of {0} minutes.", m_AuctionEnds.TotalMinutes));
            }
        }

        protected bool CheckID()
        {
            PlayerMobile pm = World.FindMobile(m_HighBidMobile) as PlayerMobile;
            if (Misc.Diagnostics.Assert(pm != null && pm.Account != null && pm.Account.ToString().GetHashCode() == m_HighBidMobileHC, "Hash Code mismatch", Utility.FileInfo()))
                return true;

            return false;
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] is ContextMenus.VendorBuyEntry)
                    list.RemoveAt(i--);
                else if (list[i] is ContextMenus.VendorSellEntry)
                    list.RemoveAt(i--);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write(m_HighBid);
            writer.Write(m_BidIncrement);
            writer.Write(m_StartingBid);
            writer.Write(m_HighBidMobile);
            writer.Write(m_HighBidMobileHC);
            writer.Write(m_AuctionEnds);
            writer.Write(m_AuctionStartFlag);
            writer.Write(m_GoldDeleted);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    m_HighBid = reader.ReadInt();
                    m_BidIncrement = reader.ReadInt();
                    m_StartingBid = reader.ReadInt();
                    m_HighBidMobile = reader.ReadInt();
                    m_HighBidMobileHC = reader.ReadInt();
                    m_AuctionEnds = reader.ReadTimeSpan();
                    m_AuctionStartFlag = reader.ReadBool();
                    m_GoldDeleted = reader.ReadInt();
                    goto case 0;
                case 0:
                    // no data in version 0
                    goto default;
                default:
                    break;
            }

        }

        private class AuctionTimer : Timer
        {
            private Auctioneer m_mobile;

            public AuctionTimer(Auctioneer m)
                : base(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1))
            {
                m_mobile = m;
                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                if (m_mobile == null || m_mobile.Deleted == true)
                    return;

                if (m_mobile.Running == true)
                {
                    m_mobile.AuctionEnds -= TimeSpan.FromMinutes(1);
                    //if (m_mobile.Running == false)
                    if (m_mobile.AuctionEnds.TotalSeconds <= 0)
                        m_mobile.OnAuctionFinish();

                    if (m_mobile.AuctionEnds.TotalMinutes == 1)
                    {
                        m_mobile.Say("Only 1 minute left in the auction!");
                        m_mobile.Say("If you're going to bid, you had better do it quickly!");
                    }
                }
            }
        }
    }
}