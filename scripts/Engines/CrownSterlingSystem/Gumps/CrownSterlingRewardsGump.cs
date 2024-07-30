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

/* Scripts\Engines\CrownSterlingSystem\Gumps\CrownSterlingRewardsGump.cs
 * CHANGELOG:
 *  6/28,2024, Adam
 *      Allow Sterling consumption from both backpack and bank box
 *      Add new RareSets
 *  6/25/2024, Adam
 *      Initial version.
 */

using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Engines.CrownSterlingSystem
{
    public class CrownSterlingRewardsGump : Gump
    {
        private enum Page : byte
        {
            Select,
            Confirm,
        }

        private CrownSterlingSystem m_System;
        private Page m_Page;
        private CrownSterlingReward m_Reward;
        List<CrownSterlingReward> m_AddRewardsDatabase = new();
        List<CrownSterlingReward> m_RemoveRewardsDatabase = new();
        public CrownSterlingRewardsGump(CrownSterlingSystem system, Mobile from, object[] args)
            : this(system, from, Page.Select, new object[] { args[0], args[1], null })
        {
        }

        private CrownSterlingRewardsGump(CrownSterlingSystem system, Mobile buyer, Page page, object[] args)
            : base(250, 50)
        {
            m_System = system;
            m_Page = page;
            m_AddRewardsDatabase = args[0] as List<CrownSterlingReward>;
            m_RemoveRewardsDatabase = args[1] as List<CrownSterlingReward>;
            m_Reward = args[2] as CrownSterlingReward;

            if (!(buyer is PlayerMobile pm && pm.Backpack != null && pm.BankBox != null))
                return;

            int sterling = buyer.Backpack.GetAmount(typeof(Sterling)) + buyer.BankBox.GetAmount(typeof(Sterling));

            int gumpHeight;

            if (m_Page == Page.Select)
                gumpHeight = 308;
            else
                gumpHeight = 144;

            // grow the select gump for enormous items
            if (m_Page == Page.Confirm)
            {
                int itemID = 0;
                if (m_Reward != null && m_Reward.ItemO is int id)
                    itemID = id;
                else
                    itemID = (m_Reward.ItemO as Item).ItemID;

                if (ItemBounds.Table[itemID & 0x3FFF].Height >= 70)
                    gumpHeight += (int)(ItemBounds.Table[itemID & 0x3FFF].Height * .55);
            }

            AddPage(0);

            AddImage(0, 0, 0x1F40);
            AddImageTiled(20, 37, 300, gumpHeight, 0x1F42);
            AddImage(20, gumpHeight + 18, 0x1F43);

            AddImage(35, 8, 0x39);
            AddImageTiled(65, 8, 257, 10, 0x3A);
            AddImage(290, 8, 0x3B);

            AddImage(32, 33, 0x2635);

            string title = system.Vendor is CrownSterlingVendor csv ? csv.RewardSet.ToString() : "My goods";
            AddHtml(70, 35, 270, 20, Utility.SplitCamelCase(title), false, false);
            AddImageTiled(70, 55, 230, 2, 0x23C5);

            AddHtml(40, 65, 150, 20, "Your available sterling:", false, false);
            AddLabel(230, 65, 0x64, sterling.ToString());
            AddImageTiled(35, 85, 270, 2, 0x23C5);

            switch (m_Page)
            {
                case Page.Select:
                    {
                        CrownSterlingReward[] rewards = m_System.GetCrownRewardOptions(m_AddRewardsDatabase, m_RemoveRewardsDatabase);

                        AddHtml(40, 90, 270, 20, "Please Make Your Selection:", false, false);

                        AddPage(1);

                        int offset = 110;
                        int pages = 1;

                        for (int i = 0; i < rewards.Length; i++)
                        {
                            CrownSterlingReward rew = rewards[i];

                            int itemId = rew.ItemO is Item ? (rew.ItemO as Item).ItemID : (int)rew.ItemO;
                            Rectangle2D bounds = ItemBounds.Table[itemId & 0x3FFF];
                            System.Diagnostics.Debug.Assert(bounds.Height > 0); // if you're running the wrong binary/bounds.bin
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

                            bool available = (rew.Cost <= sterling);

                            int halfY = offset + (itemHeight / 2);

                            if (available)
                                AddButton(35, halfY - 6, 0x837, 0x838, 100 + i, GumpButtonType.Reply, 0);

                            AddItem(70 + 10 - (bounds.Width / 2) - bounds.X, halfY - (bounds.Height / 2) - bounds.Y, itemId, available ? rew.Hue : 995);
                            AddLabel(100 + 20, halfY - 10, available ? LabelHue(rew) : 0x21, rew.Cost.ToString());
                            AddLabelCropped(150 + 20, halfY - 10, 160 - 10, 20, 0, rew.Label);

                            offset += itemHeight + 1;
                        }

                        break;
                    }
                case Page.Confirm:
                    {
                        if (m_Reward == null)
                            return;

                        Closable = false;

                        AddHtml(40, 90, 270, 20, "Are you sure you wish to purchase this?", false, false);

                        int itemId = m_Reward.ItemO is Item ? (m_Reward.ItemO as Item).ItemID : (int)m_Reward.ItemO;
                        Rectangle2D bounds = ItemBounds.Table[itemId & 0x3FFF];
                        System.Diagnostics.Debug.Assert(bounds.Height > 0); // if you're running the wrong binary/bounds.bin
                        int itemHeight = Math.Max(36, bounds.Height);

                        int halfY = 110 + (itemHeight / 2);

                        AddItem(70 + 10 - (bounds.Width / 2) - bounds.X, halfY - (bounds.Height / 2) - bounds.Y, itemId, m_Reward.Hue);
                        AddLabel(100 + 20, halfY - 10, 0x64, m_Reward.Cost.ToString());
                        AddLabelCropped(150 + 20, halfY - 10, 160 - 10, 20, 0, m_Reward.Label);

                        AddButton(180, gumpHeight + 24, 0xF9, 0xF7, 1, GumpButtonType.Reply, 0); // Okay
                        AddButton(250, gumpHeight + 24, 0xF3, 0xF1, 0, GumpButtonType.Reply, 0); // Cancel

                        break;
                    }
            }
        }
        private int LabelHue(CrownSterlingReward reward)
        {
            int blue = 0x64;
            int green = 0x84E;
            if (reward.ItemO is CrownSterlingSystem.AddonFactory)
                return green;
            return blue;
        }
        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!from.Alive)
                return;

            switch (m_Page)
            {
                case Page.Select:
                    {
                        if (info.ButtonID == 2)
                        {
                            from.CloseGump(typeof(CrownSterlingRewardsGump));
                            from.SendGump(new CrownSterlingRewardsGump(m_System, from, Page.Select, new object[] { m_AddRewardsDatabase, m_RemoveRewardsDatabase, null }));
                        }
                        else
                        {
                            int choice = info.ButtonID - 100;

                            CrownSterlingReward[] rewards = m_System.GetCrownRewardOptions(m_AddRewardsDatabase, m_RemoveRewardsDatabase);

                            if (choice >= 0 && choice < rewards.Length)
                            {
                                from.CloseGump(typeof(CrownSterlingRewardsGump));
                                from.SendGump(new CrownSterlingRewardsGump(m_System, from, Page.Confirm, new object[] { m_AddRewardsDatabase, m_RemoveRewardsDatabase, rewards[choice] }));
                            }
                        }

                        break;
                    }
                case Page.Confirm:
                    {
                        if (info.ButtonID == 0)
                        {
                            from.CloseGump(typeof(CrownSterlingRewardsGump));
                            from.SendGump(new CrownSterlingRewardsGump(m_System, from, Page.Select, new object[] { m_AddRewardsDatabase, m_RemoveRewardsDatabase, null }));
                        }
                        else if (info.ButtonID == 1)
                        {
                            if (m_Reward != null && m_System.ClaimReward(from, m_Reward))
                            {
                                return; // don't resend gump
                            }

                            from.CloseGump(typeof(CrownSterlingRewardsGump));
                            from.SendGump(new CrownSterlingRewardsGump(m_System, from, Page.Select, new object[] { m_AddRewardsDatabase, m_RemoveRewardsDatabase, null }));
                        }

                        break;
                    }
            }
        }
    }
}