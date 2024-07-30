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

/* Engines/Factions/Gumps/New Gumps/Town/SheriffGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using Server.Network;

namespace Server.Factions.NewGumps.TownMenu
{
    public class SheriffGump : BaseFactionGump
    {
        private Town m_Town;

        public SheriffGump(Mobile m, Town town)
            : base()
        {
            m_Town = town;

            AddPage(0);

            AddBackground(350, 335);

            #region General

            AddPage(1);

            AddHtml(20, 15, 310, 26, "<center><i>Sheriff</i></center>", false, false);

            AddSeparator(20, 40, 310);

            AddPageButtonLabeled(20, 50, 150, 2, "Hire Guards");
            AddButtonLabeled(180, 50, 150, 1, "Fire Guard");

            AddSeparator(20, 82, 310);

            AddButtonLabeled(180, 90, 150, 0, 1011393); // Back

            #endregion

            #region Hire Guards

            AddPage(2);

            AddHtml(20, 15, 310, 26, "<center><i>Hire Guards</i></center>", false, false);
            AddSeparator(20, 40, 310);

            int y = 40;

            for (int i = 0; i < town.GuardLists.Count; i++, y += 60)
            {
                GuardList list = town.GuardLists[i];

                AddButton(20, y + 23, 0x845, 0x846, 0, GumpButtonType.Page, 3 + i);
                AddItemCentered(40, y, 70, 60, list.Definition.ItemID);
                AddHtmlText(120, y + 20, 200, 20, list.Definition.Header, false, false);
            }

            AddSeparator(20, 282, 310);

            AddPageButtonLabeled(230, 290, 100, 1, 1011393); // Back

            #endregion

            #region Guard Pages

            for (int i = 0; i < town.GuardLists.Count; i++)
            {
                GuardList list = town.GuardLists[i];

                AddPage(3 + i);

                AddHtml(20, 15, 310, 26, "<center><i>Hire Guards</i></center>", false, false);
                AddSeparator(20, 40, 310);

                AddStatisticLabel(20, 50, 1011514, list.Guards.Count.ToString(), 0x26); // You have : 
                AddStatisticLabel(20, 80, 1011515, list.Definition.Maximum.ToString(), 0x12A); // Maximum : 
                AddStatisticLabel(20, 110, 1011516, list.Definition.Price.ToString("N0"), 0x44); // Cost : 
                AddStatisticLabel(20, 140, 1011517, list.Definition.Upkeep.ToString("N0"), 0x37); // Daily Pay :
                AddStatisticLabel(20, 170, 1011518, town.Silver.ToString("N0"), 0x44); // Current Silver : 
                AddStatisticLabel(20, 200, 1011519, town.SheriffUpkeep.ToString("N0"), 0x44); // Current Payroll : 

                AddSeparator(20, 282, 310);

                AddButtonLabeled(20, 290, 200, 100 + i, list.Definition.Label); // Hire [...]
                AddPageButtonLabeled(230, 290, 100, 2, 1011393); // Back
            }

            #endregion
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!m_Town.IsSheriff(from))
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
                case 1: // Fire Guard
                    {
                        m_Town.BeginOrderFiring(from);

                        FactionGumps.CloseGumps(from, typeof(SheriffGump));
                        from.SendGump(new SheriffGump(from, m_Town));

                        break;
                    }
                default:
                    {
                        int index = buttonID - 100;

                        if (index >= 0 && index < m_Town.GuardLists.Count)
                        {
                            Factions.SheriffGump.PlaceGuard(from, m_Town.Owner, m_Town, m_Town.GuardLists[index]);

                            FactionGumps.CloseGumps(from, typeof(SheriffGump));
                            from.SendGump(new SheriffGump(from, m_Town));
                        }

                        break;
                    }
            }
        }
    }
}