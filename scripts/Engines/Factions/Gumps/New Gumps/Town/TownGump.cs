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

/* Engines/Factions/Gumps/New Gumps/Town/TownGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using Server.Network;
using System;

namespace Server.Factions.NewGumps.TownMenu
{
    public class TownGump : BaseFactionGump
    {
        private Town m_Town;

        public TownGump(Mobile m, Town town)
            : base()
        {
            m_Town = town;

            int count = 1;

            if (m_Town.IsCommander(m))
                count++;

            if (m_Town.IsSheriff(m))
                count++;

            if (m_Town.IsFinance(m))
                count++;

            if (m.AccessLevel >= AccessLevel.GameMaster)
                count++;

            AddBackground(350, 195 + 30 * ((count + 1) / 2));

            AddHtml(20, 15, 310, 26, string.Format("<center><i>{0}</i></center>", town.Definition.FriendlyName), false, false);

            AddSeparator(20, 40, 310);

            AddStatistic(20, 50, 150, "<i>Owned By</i>", FormatFaction(m_Town.Owner));
            AddStatistic(20, 80, 150, "<i>Sheriff</i>", FormatName(m_Town.Sheriff));
            AddStatistic(20, 110, 150, "<i>Finance MIN</i>", FormatName(m_Town.Finance));
            AddStatistic(20, 140, 150, "<i>Tax Rate</i>", string.Concat(m_Town.Tax.ToString("+#;-#;0"), '%'));

            AddSeparator(20, 172, 310);

            int index = 0;

            AddButtonLabeled(((index % 2) == 0) ? 20 : 180, 180 + 30 * (index / 2), 150, 1, "View Finances");
            index++;

            if (m_Town.IsCommander(m))
            {
                AddButtonLabeled(((index % 2) == 0) ? 20 : 180, 180 + 30 * (index / 2), 150, 2, "Commander");
                index++;
            }

            if (m_Town.IsSheriff(m))
            {
                AddButtonLabeled(((index % 2) == 0) ? 20 : 180, 180 + 30 * (index / 2), 150, 3, "Sheriff");
                index++;
            }

            if (m_Town.IsFinance(m))
            {
                AddButtonLabeled(((index % 2) == 0) ? 20 : 180, 180 + 30 * (index / 2), 150, 4, "Finance MIN");
                index++;
            }

            if (m.AccessLevel >= AccessLevel.GameMaster)
            {
                AddButtonLabeled(((index % 2) == 0) ? 20 : 180, 180 + 30 * (index / 2), 150, 5, "GM Options");
                index++;
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!m_Town.IsMember(from))
            {
                from.SendLocalizedMessage(1010339); // You no longer control this city
                return;
            }

            int buttonID = info.ButtonID;

            switch (buttonID)
            {
                case 1: // View Finances
                    {
                        FactionGumps.CloseGumps(from, typeof(FinancesGump));
                        from.SendGump(new FinancesGump(from, m_Town));

                        break;
                    }
                case 2: // Commander
                    {
                        if (m_Town.IsCommander(from))
                        {
                            FactionGumps.CloseGumps(from, typeof(CommanderGump));
                            from.SendGump(new CommanderGump(from, m_Town));
                        }

                        break;
                    }
                case 3: // Sheriff
                    {
                        if (m_Town.IsSheriff(from))
                        {
                            FactionGumps.CloseGumps(from, typeof(SheriffGump));
                            from.SendGump(new SheriffGump(from, m_Town));
                        }

                        break;
                    }
                case 4: // Finance MIN
                    {
                        if (m_Town.IsFinance(from))
                        {
                            FactionGumps.CloseGumps(from, typeof(FinanceGump));
                            from.SendGump(new FinanceGump(from, m_Town));
                        }

                        break;
                    }
                case 5:
                    {
                        if (from.AccessLevel >= AccessLevel.GameMaster)
                        {
                            FactionGumps.CloseGumps(from, typeof(TownGump));
                            from.SendGump(new TownGump(from, m_Town));

                            from.SendGump(new PropertiesGump(from, m_Town.State));
                        }

                        break;
                    }
            }
        }
    }
}