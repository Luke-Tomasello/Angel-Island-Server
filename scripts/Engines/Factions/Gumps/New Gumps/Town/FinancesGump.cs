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

/* Engines/Factions/Gumps/New Gumps/Town/BaseFactionGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using Server.Network;

namespace Server.Factions.NewGumps.TownMenu
{
    public class FinancesGump : BaseFactionGump
    {
        private Town m_Town;

        public FinancesGump(Mobile m, Town town)
            : base()
        {
            m_Town = town;

            FinanceEntry[] list = BuildList(town);

            AddBackground(350, 105 + 30 * list.Length);

            AddHtml(20, 15, 310, 26, "<center><i>Finance Statement</i></center>", false, false);

            AddSeparator(20, 40, 310);

            int y = 50;

            for (int i = 0; i < list.Length; i++, y += 30)
            {
                FinanceEntry e = list[i];

                int value = (e.Sign == Sign.Minus ? -e.Value : e.Value);

                AddStatisticLabel(20, y, e.Label, value.ToString(e.Sign == Sign.None ? "N0" : "+#,###;-#,###;0"), Color(value));
            }

            AddSeparator(20, y + 2, 310);

            AddButtonLabeled(180, y + 12, 150, 0, 1011393); // Back
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!m_Town.IsMember(from))
            {
                from.SendLocalizedMessage(1010339); // You no longer control this city
                return;
            }

            if (info.ButtonID == 0) // Back
            {
                FactionGumps.CloseGumps(from, typeof(TownGump));
                from.SendGump(new TownGump(from, m_Town));
            }
        }

        private static FinanceEntry[] BuildList(Town town)
        {
            int count = 5;

            if (FactionConfig.TownSqualorEnabled)
                count++;

            FinanceEntry[] list = new FinanceEntry[count];

            int i = 0;

            list[i++] = new FinanceEntry(Sign.None, town.Silver, 1011538); // Current total money for town : 
            list[i++] = new FinanceEntry(Sign.Plus, town.DailyIncome, 1011522); // Town Income : 
            list[i++] = new FinanceEntry(Sign.Minus, town.FinanceUpkeep, 1011520); // Finance Minister Upkeep : 
            list[i++] = new FinanceEntry(Sign.Minus, town.SheriffUpkeep, 1011521); // Sheriff Upkeep : 

            if (FactionConfig.TownSqualorEnabled)
                list[i++] = new FinanceEntry(Sign.Minus, town.Squalor, "Squalor : ");

            list[i++] = new FinanceEntry(Sign.Plus, town.NetCashFlow, 1011523); // Net Cash flow per day : 

            return list;
        }

        private static int Color(int value)
        {
            return (value > 0 ? 0x44 : value == 0 ? 0x37 : 0x26);
        }

        private enum Sign : byte
        {
            None,
            Plus,
            Minus,
        }

        private struct FinanceEntry
        {
            public readonly Sign Sign;
            public readonly int Value;
            public readonly TextDefinition Label;

            public FinanceEntry(Sign sign, int value, TextDefinition label)
            {
                Sign = sign;
                Value = value;
                Label = label;
            }
        }
    }
}