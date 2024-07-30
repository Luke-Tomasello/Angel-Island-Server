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

/* Scripts/Engines/BulkOrders/RewardsGump.cs
 * CHANGELOG:
 *  10/14/21, Yoar
 *      Initial version.
 */

using Server.Gumps;
using Server.Network;
using System;

namespace Server.Engines.BulkOrders
{
    public class RewardsGump : Gump
    {
        private enum Page : byte
        {
            BankPoints,
            Select,
            Confirm,
        }

        private BulkOrderSystem m_System;
        private bool m_Banked;
        private Page m_Page;
        private RewardOption m_Reward;

        public RewardsGump(BulkOrderSystem system, Mobile from)
            : this(system, from, false, Page.BankPoints, null)
        {
        }

        public RewardsGump(BulkOrderSystem system, Mobile from, bool banked)
            : this(system, from, banked, Page.Select, null)
        {
        }

        private RewardsGump(BulkOrderSystem system, Mobile from, bool banked, Page page, RewardOption reward)
            : base(250, 50)
        {
            m_System = system;
            m_Banked = banked;
            m_Page = page;
            m_Reward = reward;

            BulkOrderContext context = m_System.GetContext(from, false);

            double points;

            if (context == null)
                points = 0.0;
            else if (m_Banked)
                points = context.Banked;
            else
                points = context.Pending.Points;

            int gumpHeight;

            if (m_Page == Page.Select)
                gumpHeight = 308;
            else
                gumpHeight = 144;

            AddPage(0);

            AddImage(0, 0, 0x1F40);
            AddImageTiled(20, 37, 300, gumpHeight, 0x1F42);
            AddImage(20, gumpHeight + 18, 0x1F43);

            AddImage(35, 8, 0x39);
            AddImageTiled(65, 8, 257, 10, 0x3A);
            AddImage(290, 8, 0x3B);

            AddImage(32, 33, 0x2635);

            AddHtml(70, 35, 270, 20, "Bulk Order Rewards", false, false);
            AddImageTiled(70, 55, 230, 2, 0x23C5);

            AddHtml(40, 65, 150, 20, "Your Reward Points:", false, false);
            AddLabel(230, 65, 0x64, points.ToString("F2"));
            AddImageTiled(35, 85, 270, 2, 0x23C5);

            switch (m_Page)
            {
                case Page.BankPoints:
                    {
                        if (context == null || context.Pending.Points <= 0)
                            return;

                        Closable = false;

                        double bankPoints;

                        if (context.Pending.Large)
                            bankPoints = BulkOrderSystem.LargeBankPerc * context.Pending.Points / 100.0;
                        else
                            bankPoints = BulkOrderSystem.SmallBankPerc * context.Pending.Points / 100.0;

                        AddHtml(40, 90, 270, 70, String.Format("Select <b>OKAY</b> to bank {0:F2} points. Select <b>CANCEL</b> to spend all {1} points on a reward now.", bankPoints, context.Pending.Points), false, false);

                        AddButton(40, gumpHeight + 30, 0x837, 0x838, 2, GumpButtonType.Reply, 0);
                        AddLabel(55, gumpHeight + 25, 0, "Banking Options");

                        AddButton(180, gumpHeight + 24, 0xF9, 0xF7, 1, GumpButtonType.Reply, 0); // Okay
                        AddButton(250, gumpHeight + 24, 0xF3, 0xF1, 0, GumpButtonType.Reply, 0); // Cancel

                        break;
                    }
                case Page.Select:
                    {
                        RewardOption[] rewards = m_System.GetRewardOptions();

                        AddHtml(40, 90, 270, 20, "Please Choose a Reward:", false, false);

                        AddButton(40, gumpHeight + 30, 0x837, 0x838, 2, GumpButtonType.Reply, 0);
                        AddLabel(55, gumpHeight + 25, 0, "Banking Options");

                        AddPage(1);

                        int offset = 110;
                        int pages = 1;

                        for (int i = 0; i < rewards.Length; i++)
                        {
                            RewardOption rew = rewards[i];

                            Rectangle2D bounds = ItemBounds.Table[rew.ItemID & 0x3FFF];

                            int itemHeight = Math.Max(36, bounds.Height);

                            if (offset + itemHeight > gumpHeight + 12)
                            {
                                AddButton(300, gumpHeight + 27, 0x15E1, 0x15E5, 51, GumpButtonType.Page, pages + 1);
                                AddHtmlLocalized(240, gumpHeight + 26, 60, 20, 1072854, 1, false, false); // <div align=right>Next</div>

                                AddPage(++pages);

                                AddButton(160, gumpHeight + 27, 0x15E3, 0x15E7, 52, GumpButtonType.Page, pages - 1);
                                AddHtmlLocalized(180, gumpHeight + 26, 60, 20, 1074880, 1, false, false); // Previous

                                offset = 110;
                            }

                            bool available = (rew.Points <= points);

                            int halfY = offset + (itemHeight / 2);

                            if (available)
                                AddButton(35, halfY - 6, 0x837, 0x838, 100 + i, GumpButtonType.Reply, 0);

                            AddItem(70 - (bounds.Width / 2) - bounds.X, halfY - (bounds.Height / 2) - bounds.Y, rew.ItemID, available ? rew.Hue : 995);
                            AddLabel(100, halfY - 10, available ? 0x64 : 0x21, rew.Points.ToString());
                            AddLabelCropped(150, halfY - 10, 160, 20, 0, rew.Label);

                            offset += itemHeight + 1;
                        }

                        break;
                    }
                case Page.Confirm:
                    {
                        if (m_Reward == null)
                            return;

                        Closable = false;

                        AddHtmlLocalized(40, 90, 270, 20, 1074975, false, false); // Are you sure you wish to select this?

                        Rectangle2D bounds = ItemBounds.Table[m_Reward.ItemID & 0x3FFF];

                        int itemHeight = Math.Max(36, bounds.Height);

                        int halfY = 110 + (itemHeight / 2);

                        AddItem(70 - (bounds.Width / 2) - bounds.X, halfY - (bounds.Height / 2) - bounds.Y, m_Reward.ItemID, m_Reward.Hue);
                        AddLabel(100, halfY - 10, 0x64, m_Reward.Points.ToString());
                        AddLabelCropped(150, halfY - 10, 160, 20, 0, m_Reward.Label);

                        AddButton(180, gumpHeight + 24, 0xF9, 0xF7, 1, GumpButtonType.Reply, 0); // Okay
                        AddButton(250, gumpHeight + 24, 0xF3, 0xF1, 0, GumpButtonType.Reply, 0); // Cancel

                        break;
                    }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!from.Alive)
                return;

            BulkOrderContext context = m_System.GetContext(from, false);

            switch (m_Page)
            {
                case Page.BankPoints:
                    {
                        if (info.ButtonID == 0)
                        {
                            from.CloseGump(typeof(RewardsGump));
                            from.SendGump(new RewardsGump(m_System, from, false, Page.Select, null));
                        }
                        else if (info.ButtonID == 1)
                        {
                            m_System.SavePoints(from);

                            from.CloseGump(typeof(RewardsGump));
                            from.SendGump(new RewardsGump(m_System, from, true, Page.Select, null));
                        }
                        else if (info.ButtonID == 2)
                        {
                            from.CloseGump(typeof(RewardsGump));
                            from.SendGump(new RewardsGump(m_System, from, false, Page.BankPoints, null));

                            from.CloseGump(typeof(BankingOptionsGump));
                            from.SendGump(new BankingOptionsGump(m_System, from));
                        }

                        break;
                    }
                case Page.Select:
                    {
                        if (info.ButtonID == 2)
                        {
                            from.CloseGump(typeof(RewardsGump));
                            from.SendGump(new RewardsGump(m_System, from, m_Banked, Page.Select, null));

                            from.CloseGump(typeof(BankingOptionsGump));
                            from.SendGump(new BankingOptionsGump(m_System, from));
                        }
                        else
                        {
                            int choice = info.ButtonID - 100;

                            RewardOption[] rewards = m_System.GetRewardOptions();

                            if (choice >= 0 && choice < rewards.Length && context != null && (m_Banked ? context.Banked : context.Pending.Points) >= rewards[choice].Points)
                            {
                                from.CloseGump(typeof(RewardsGump));
                                from.SendGump(new RewardsGump(m_System, from, m_Banked, Page.Confirm, rewards[choice]));
                            }
                        }

                        break;
                    }
                case Page.Confirm:
                    {
                        if (info.ButtonID == 0)
                        {
                            from.CloseGump(typeof(RewardsGump));
                            from.SendGump(new RewardsGump(m_System, from, m_Banked, Page.Select, null));
                        }
                        else if (info.ButtonID == 1)
                        {
                            if (m_Reward != null && context != null && (m_Banked ? context.Banked : context.Pending.Points) >= m_Reward.Points && BulkOrderSystem.ClaimReward(from, m_Reward))
                            {
                                if (m_Banked)
                                    context.Banked -= m_Reward.Points;
                                else
                                    context.Pending = PendingReward.Zero;

                                if (!m_Banked)
                                    return; // don't resend gump
                            }

                            from.CloseGump(typeof(RewardsGump));
                            from.SendGump(new RewardsGump(m_System, from, m_Banked, Page.Select, null));
                        }

                        break;
                    }
            }
        }
    }

    public class BankingOptionsGump : Gump
    {
        private BulkOrderSystem m_System;

        public BankingOptionsGump(BulkOrderSystem system, Mobile from)
            : base(50, 50)
        {
            m_System = system;

            BulkOrderContext context = m_System.GetContext(from, true);

            if (context == null)
                return; // sanity

            AddBackground(10, 10, 190, 140, 0x242C);

            AddHtml(30, 30, 150, 70, "When do you wish to bank your bulk order points?", false, false);

            AddButton(30, 105, 0xFA5, 0xFA6, 1, GumpButtonType.Reply, 0);

            switch (context.BankingSetting)
            {
                case BankingSetting.Enabled: AddLabel(65, 106, 0, "Ask me!"); break;
                case BankingSetting.Disabled: AddLabel(65, 106, 0, "Never"); break;
                case BankingSetting.Automatic: AddLabel(65, 106, 0, "Always"); break;
            }

            AddButton(135, 105, 0x81A, 0x81B, 0, GumpButtonType.Reply, 0); // Okay
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (info.ButtonID == 1)
            {
                BulkOrderContext context = m_System.GetContext(from, true);

                if (context == null)
                    return; // sanity

                switch (context.BankingSetting)
                {
                    case BankingSetting.Enabled: context.BankingSetting = BankingSetting.Disabled; break;
                    case BankingSetting.Disabled: context.BankingSetting = BankingSetting.Automatic; break;
                    case BankingSetting.Automatic: context.BankingSetting = BankingSetting.Enabled; break;
                }

                from.CloseGump(typeof(BankingOptionsGump));
                from.SendGump(new BankingOptionsGump(m_System, from));
            }
        }
    }
}