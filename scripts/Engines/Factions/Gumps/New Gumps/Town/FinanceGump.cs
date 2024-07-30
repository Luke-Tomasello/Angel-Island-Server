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

/* Engines/Factions/Gumps/New Gumps/Town/FinanceGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using Server.Network;
using System;

namespace Server.Factions.NewGumps.TownMenu
{
    public class FinanceGump : BaseFactionGump
    {
        private Town m_Town;

        public FinanceGump(Mobile m, Town town)
            : base()
        {
            m_Town = town;

            AddPage(0);

            AddBackground(350, 315);

            #region General

            AddPage(1);

            AddHtml(20, 15, 310, 26, "<center><i>Finance Minister</i></center>", false, false);

            AddSeparator(20, 40, 310);

            AddPageButtonLabeled(20, 50, 150, 3, "Buy Shopkeepers");
            AddButtonLabeled(180, 50, 150, 2, "Fire Shopkeeper");
            AddPageButtonLabeled(20, 80, 150, 2, "Change Prices");

            AddSeparator(20, 112, 310);

            AddButtonLabeled(180, 120, 150, 0, 1011393); // Back

            #endregion

            #region Change Prices

            AddPage(2);

            AddHtml(20, 15, 310, 26, "<center><i>Change Prices</i></center>", false, false);

            AddSeparator(20, 40, 310);

            int y = 50;

            for (int i = 0; i < Factions.FinanceGump.PriceOffsets.Length; i++)
            {
                int tax = Factions.FinanceGump.PriceOffsets[i];

                bool left = (i >= 6);

                if (i != 0 && (i % 6) == 0)
                    y = 50;

                AddRadio(left ? 20 : 180, y, 208, 209, town.Tax == tax, i + 1);
                AddLabel(left ? 55 : 215, y, tax < 0 ? 0x26 : 0x12A, String.Concat(tax.ToString("+0;-#"), '%'));

                y += 30;
            }

            AddRadio(20, 230, 208, 209, town.Tax == 0, 0);
            AddHtmlLocalized(55, 230, 90, 25, 1011542, false, false); // normal

            AddSeparator(20, 262, 310);

            AddButtonLabeled(20, 270, 150, 1, 1011509); // Set Prices
            AddPageButtonLabeled(180, 270, 150, 1, 1011393); // Back

            #endregion

            #region Buy Shopkeepers

            AddPage(3);

            AddHtml(20, 15, 310, 26, "<center><i>Buy Shopkeepers</i></center>", false, false);
            AddSeparator(20, 40, 310);

            y = 40;

            for (int i = 0; i < m_Town.VendorLists.Count; i++, y += 40)
            {
                VendorList list = m_Town.VendorLists[i];

                AddButton(20, y + 13, 0x845, 0x846, 0, GumpButtonType.Page, 4 + i);
                AddItemCentered(50, y, 40, 40, list.Definition.ItemID);
                AddHtmlText(100, y + 10, 200, 20, list.Definition.Header, false, false);
            }

            AddSeparator(20, 262, 310);

            AddButtonLabeled(230, 270, 100, 1, 1011393); // Back

            #endregion

            #region Shopkeeper Pages

            for (int i = 0; i < m_Town.VendorLists.Count; i++)
            {
                VendorList list = m_Town.VendorLists[i];

                AddPage(4 + i);

                AddHtml(20, 15, 310, 26, "<center><i>Buy Shopkeepers</i></center>", false, false);
                AddSeparator(20, 40, 310);

                AddStatisticLabel(20, 50, 1011514, list.Vendors.Count.ToString(), 0x26); // You have : 
                AddStatisticLabel(20, 80, 1011515, list.Definition.Maximum.ToString(), 0x12A); // Maximum : 
                AddStatisticLabel(20, 110, 1011516, list.Definition.Price.ToString("N0"), 0x44); // Cost : 
                AddStatisticLabel(20, 140, 1011517, list.Definition.Upkeep.ToString("N0"), 0x37); // Daily Pay :
                AddStatisticLabel(20, 170, 1011518, town.Silver.ToString("N0"), 0x44); // Current Silver : 
                AddStatisticLabel(20, 200, 1011519, town.FinanceUpkeep.ToString("N0"), 0x44); // Current Payroll : 

                AddSeparator(20, 262, 310);

                AddButtonLabeled(20, 270, 200, 100 + i, list.Definition.Label); // Buy [...]
                AddPageButtonLabeled(230, 270, 100, 3, 1011393); // Back
            }

            #endregion
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!m_Town.IsFinance(from))
            {
                from.SendLocalizedMessage(1010339); // You no longer control this city
                return;
            }

            int buttonID = info.ButtonID;

            switch (info.ButtonID)
            {
                case 0: // Back
                    {
                        FactionGumps.CloseGumps(from, typeof(TownGump));
                        from.SendGump(new TownGump(from, m_Town));

                        break;
                    }
                case 1: // Set Prices
                    {
                        int index = (info.Switches.Length == 0 ? -1 : info.Switches[0]);

                        if (index >= 0 && index <= Factions.FinanceGump.PriceOffsets.Length)
                        {
                            Factions.FinanceGump.SetTax(from, m_Town, index == 0 ? 0 : Factions.FinanceGump.PriceOffsets[index - 1]);

                            FactionGumps.CloseGumps(from, typeof(FinanceGump));
                            from.SendGump(new FinanceGump(from, m_Town));
                        }

                        break;
                    }
                case 2: // Fire Shopkeeper
                    {
                        m_Town.BeginOrderFiring(from);

                        FactionGumps.CloseGumps(from, typeof(FinanceGump));
                        from.SendGump(new FinanceGump(from, m_Town));

                        break;
                    }
                default:
                    {
                        int index = buttonID - 100;

                        if (index >= 0 && index < m_Town.VendorLists.Count)
                        {
                            Factions.FinanceGump.PlaceVendor(from, m_Town.Owner, m_Town, m_Town.VendorLists[index]);

                            FactionGumps.CloseGumps(from, typeof(FinanceGump));
                            from.SendGump(new FinanceGump(from, m_Town));
                        }

                        break;
                    }
            }
        }
    }
}