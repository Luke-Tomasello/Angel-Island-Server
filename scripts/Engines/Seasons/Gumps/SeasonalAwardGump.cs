
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

/* Scripts\Engines\Seasons\Gumps\SeasonalAwardGump.cs
 * ChangeLog
 *  3/27/2026, Adam
 *      First time checkin.
 */

using Server.Accounting;
using Server.Gumps;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Engines.Seasons
{
    public class SeasonalAwardGump : Gump
    {
        public static void Initialize()
        {
            CommandSystem.Register("Awards", AccessLevel.Player, new CommandEventHandler(Awards_OnCommand));
        }

        private enum Page : byte
        {
            Select,
            Confirm
        }

        private Mobile m_From;
        private SeasonalAccount m_Account;
        private Page m_Page;
        private SeasonalAwardDefinition? m_Award;

        public SeasonalAwardGump(Mobile from)
            : this(from, Page.Select, null)
        {
        }

        private SeasonalAwardGump(Mobile from, Page page, SeasonalAwardDefinition? award)
            : base(250, 50)
        {
            m_From = from;
            m_Account = SeasonalAccounts.GetAccount(from);
            m_Page = page;
            m_Award = award;

            int points = GetSpendablePoints(m_Account);

            int gumpHeight = (m_Page == Page.Select) ? 308 : 144;

            AddPage(0);

            AddImage(0, 0, 0x1F40);
            AddImageTiled(20, 37, 300, gumpHeight, 0x1F42);
            AddImage(20, gumpHeight + 18, 0x1F43);

            AddImage(35, 8, 0x39);
            AddImageTiled(65, 8, 257, 10, 0x3A);
            AddImage(290, 8, 0x3B);

            AddImage(32, 33, 0x2635);

            AddHtml(70, 35, 270, 20, "Seasonal Awards", false, false);
            AddImageTiled(70, 55, 230, 2, 0x23C5);

            AddHtml(40, 65, 150, 20, "Available Points:", false, false);
            AddLabel(230, 65, 0x64, points.ToString());
            AddImageTiled(35, 85, 270, 2, 0x23C5);

            switch (m_Page)
            {
                case Page.Select:
                    {
                        List<SeasonalAwardDefinition> awards = SeasonalAwardDefinitions.GetAll();

                        AddHtml(40, 90, 270, 20, "Choose an Award:", false, false);

                        AddPage(1);

                        int offset = 110;
                        int pages = 1;

                        for (int i = 0; i < awards.Count; i++)
                        {
                            SeasonalAwardDefinition awardDef = awards[i];

                            int itemID = awardDef.GetDisplayItemID();
                            int hue = awardDef.GetDisplayHue();
                            string name = awardDef.GetDisplayName();
                            if (itemID == 0)
                                continue;

                            Rectangle2D bounds = ItemBounds.Table[itemID & 0x3FFF];
                            int itemHeight = Math.Max(36, bounds.Height);

                            if (offset + itemHeight > gumpHeight + 12)
                            {
                                AddButton(300, gumpHeight + 27, 0x15E1, 0x15E5, 51, GumpButtonType.Page, pages + 1);
                                AddHtmlLocalized(240, gumpHeight + 26, 60, 20, 1072854, 1, false, false);

                                AddPage(++pages);

                                AddButton(160, gumpHeight + 27, 0x15E3, 0x15E7, 52, GumpButtonType.Page, pages - 1);
                                AddHtmlLocalized(180, gumpHeight + 26, 60, 20, 1074880, 1, false, false);

                                offset = 110;
                            }

                            bool available = (awardDef.Cost <= points);

                            int halfY = offset + (itemHeight / 2);

                            if (available)
                                AddButton(35, halfY - 6, 0x837, 0x838, 100 + i, GumpButtonType.Reply, 0);

                            AddItem(
                                70 - (bounds.Width / 2) - bounds.X,
                                halfY - (bounds.Height / 2) - bounds.Y,
                                itemID,
                                available ? hue : 995);

                            AddLabel(100, halfY - 10, available ? 0x64 : 0x21, awardDef.Cost.ToString());
                            AddLabelCropped(150, halfY - 10, 150, 20, 0, name);

                            offset += itemHeight + 1;
                        }

                        break;
                    }

                case Page.Confirm:
                    {
                        if (m_Award == null)
                            return;

                        AddHtml(40, 90, 270, 20, "Confirm this award?", false, false);

                        int itemID = m_Award.GetDisplayItemID();
                        int hue = m_Award.GetDisplayHue();
                        string name = m_Award.GetDisplayName();

                        Rectangle2D bounds = ItemBounds.Table[itemID & 0x3FFF];
                        int itemHeight = Math.Max(36, bounds.Height);
                        int halfY = 110 + (itemHeight / 2);

                        AddItem(
                            70 - (bounds.Width / 2) - bounds.X,
                            halfY - (bounds.Height / 2) - bounds.Y,
                            itemID,
                            hue);

                        AddLabel(100, halfY - 10, 0x64, m_Award.Cost.ToString());
                        AddLabelCropped(150, halfY - 10, 150, 20, 0, name);

                        AddButton(180, gumpHeight + 24, 0xF9, 0xF7, 1, GumpButtonType.Reply, 0);
                        AddButton(250, gumpHeight + 24, 0xF3, 0xF1, 0, GumpButtonType.Reply, 0);

                        break;
                    }
            }
        }

        private static int GetSpendablePoints(SeasonalAccount account)
        {
            if (account == null)
                return 0;

            int spendable = account.LifetimeEarnedPoints - account.SeasonSpentPoints;

            if (spendable < 0)
                spendable = 0;

            return spendable;
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;
            if (!from.Alive)
                return;

            SeasonalAccount account = SeasonalAccounts.GetAccount(from);

            switch (m_Page)
            {
                case Page.Select:
                    {
                        int choice = info.ButtonID - 100;
                        List<SeasonalAwardDefinition> awards = SeasonalAwardDefinitions.GetAll();

                        if (choice >= 0 && choice < awards.Count)
                        {
                            SeasonalAwardDefinition selected = awards[choice];

                            if (account != null && GetSpendablePoints(account) >= selected.Cost)
                            {
                                from.SendGump(new SeasonalAwardGump(from, Page.Confirm, selected));
                            }
                        }
                        break;
                    }

                case Page.Confirm:
                    {
                        if (info.ButtonID == 1 && m_Award != null && account != null)
                        {
                            if (GetSpendablePoints(account) >= m_Award.Cost)
                            {
                                // Deduct the points
                                account.SeasonSpentPoints += m_Award.Cost;

                                // Create and deliver the award
                                Item? item = m_Award.Construct();

                                if (item != null)
                                {
                                    // Try to give to backpack first
                                    if (!from.AddToBackpack(item))
                                    {
                                        // Fallback: Try bank box
                                        if (from.BankBox != null && from.BankBox.TryDropItem(from, item, false))
                                        {
                                            from.SendMessage("The award has been placed in your bank box.");
                                        }
                                        else
                                        {
                                            // Final fallback: drop at feet
                                            item.MoveToWorld(from.Location, from.Map);
                                            from.SendMessage("The award has been placed at your feet.");
                                        }
                                    }
                                    else
                                    {
                                        from.SendMessage($"You have received: {m_Award.GetDisplayName()}.");
                                    }
                                }
                                else
                                {
                                    from.SendMessage("Failed to create the award item. Please contact staff.");
                                }
                            }
                            else
                            {
                                from.SendMessage("You no longer have enough points for this award.");
                            }
                        }

                        // Return to the main awards list
                        from.SendGump(new SeasonalAwardGump(from));
                        break;
                    }
            }
        }

        [Usage("Awards")]
        [Description("Opens the Seasonal Awards gump.")]
        private static void Awards_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null)
                return;

            if (!Core.RuleSets.SeasonsRules())
            {
                from.SendMessage("Seasonal awards are only available on Seasons.");
                return;
            }

            from.CloseGump(typeof(SeasonalAwardGump));
            from.SendGump(new SeasonalAwardGump(from));
        }
    }
}
