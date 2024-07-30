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

/* Engines/Factions/Gumps/New Gumps/Misc/CommanderGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using Server.Network;
using System;

namespace Server.Factions.NewGumps.Misc
{
    public class CommanderGump : BaseFactionGump
    {
        private Faction m_Faction;

        public CommanderGump(Mobile m, Faction faction)
            : base()
        {
            m_Faction = faction;

            AddPage(0);

            AddBackground(350, 325);

            #region Commander Options

            AddPage(1);

            AddHtml(20, 15, 310, 26, "<center><i>Commander Options</i></center>", false, false);

            AddSeparator(20, 40, 310);

            AddStatistic(20, 50, 150, "<i>Tithe Rate</i>", String.Concat(m_Faction.Tithe, "%"));
            AddStatistic(20, 80, 150, "<i>Silver Available</i>", m_Faction.Silver.ToString("N0"));

            AddSeparator(20, 112, 310);

            AddPageButtonLabeled(20, 120, 150, 2, "Change Tithe Rate");
            AddPageButtonLabeled(180, 120, 150, 3, "Transfer Silver");
            AddButtonLabeled(20, 150, 150, 1, "Message Faction");

            #endregion

            #region Change Tithe Rate

            AddPage(2);

            AddHtml(20, 15, 310, 26, "<center><i>Change Tithe Rate</i></center>", false, false);

            AddSeparator(20, 40, 310);

            AddHtmlLocalized(20, 50, 280, 20, 1011479, false, false); // Select the % for the new tithe rate

            int y = 70;
            bool left = true;

            for (int i = 0; i <= 10; i++)
            {
                bool full = ((i % 5) == 0);

                AddButtonLabeled(left ? 20 : 180, y, full ? 310 : 150, 100 + i, 1011480 + i, m_Faction.Tithe == 10 * i);

                if (full || (left = !left))
                    y += 30;
            }

            AddPageButtonLabeled(180, 280, 150, 1, 1011393); // Back

            #endregion

            #region Transfer Silver

            AddPage(3);

            AddHtml(20, 15, 310, 26, "<center><i>Transfer Silver</i></center>", false, false);

            AddSeparator(20, 40, 310);

            AddHtml(20, 50, 280, 20, "Select the amount of silver to transfer", false, false);

            y = 70;
            left = true;

            for (int i = 0; i < m_Transferable.Length; i++)
            {
                AddRadio(left ? 23 : 183, y + 5, 208, 209, i == 0, i + 1);
                AddLabel(left ? 58 : 218, y + 5, 0, m_Transferable[i].ToString("N0"));

                if (left = !left)
                    y += 30;
            }

            AddHtml(20, y, 280, 20, "Select a town to transfer the silver to", false, false);

            y += 20;
            left = true;

            for (int i = 0; i < Town.Towns.Count; i++)
            {
                Town town = Town.Towns[i];

                if (town.Owner == m_Faction)
                {
                    AddButtonLabeled(left ? 20 : 180, y, 150, 200 + i, town.Definition.FriendlyName);

                    if (left = !left)
                        y += 30;
                }
            }

            AddPageButtonLabeled(180, 280, 150, 1, 1011393); // Back

            #endregion
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!m_Faction.IsCommander(from))
                return;

            int buttonID = info.ButtonID;

            int group = (buttonID / 100);
            int index = (buttonID % 100);

            switch (group)
            {
                default:
                    {
                        switch (buttonID)
                        {
                            case 1: // Message Faction
                                {
                                    if (from.AccessLevel == AccessLevel.Player && !m_Faction.FactionMessageReady)
                                        from.SendLocalizedMessage(1010264); // The required time has not yet passed since the last message was sent
                                    else
                                        m_Faction.BeginBroadcast(from);

                                    FactionGumps.CloseGumps(from, typeof(CommanderGump));
                                    from.SendGump(new CommanderGump(from, m_Faction));

                                    break;
                                }
                        }

                        break;
                    }
                case 1:
                    {
                        if (index >= 0 && index <= 10)
                        {
                            m_Faction.Tithe = index * 10;

                            FactionGumps.CloseGumps(from, typeof(CommanderGump));
                            from.SendGump(new CommanderGump(from, m_Faction));
                        }

                        break;
                    }
                case 2:
                    {
                        if (index >= 0 && index < Town.Towns.Count)
                        {
                            int transferIndex = (info.Switches.Length == 0 ? -1 : info.Switches[0] - 1);

                            if (transferIndex >= 0 && transferIndex < m_Transferable.Length)
                            {
                                FactionStoneGump.TransferSilver(from, m_Faction, Town.Towns[index], m_Transferable[transferIndex]);

                                FactionGumps.CloseGumps(from, typeof(CommanderGump));
                                from.SendGump(new CommanderGump(from, m_Faction));
                            }
                        }

                        break;
                    }
            }
        }

        private static readonly int[] m_Transferable = new int[]
            {
                10000, 20000, 50000, 100000
            };
    }
}