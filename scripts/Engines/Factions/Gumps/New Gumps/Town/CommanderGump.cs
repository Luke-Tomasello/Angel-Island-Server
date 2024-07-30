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

/* Engines/Factions/Gumps/New Gumps/Town/CommanderGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using Server.Network;
using Server.Targeting;

namespace Server.Factions.NewGumps.TownMenu
{
    public class CommanderGump : BaseFactionGump
    {
        private Town m_Town;

        public CommanderGump(Mobile m, Town town)
            : base()
        {
            m_Town = town;

            AddBackground(350, 225);

            AddHtml(20, 15, 310, 26, "<center><i>Commander Options</i></center>", false, false);

            AddSeparator(20, 40, 310);

            AddStatistic(20, 50, 150, "<i>Sheriff</i>", FormatName(m_Town.Sheriff));
            AddStatistic(20, 80, 150, "<i>Finance Minister</i>", FormatName(m_Town.Finance));

            AddSeparator(20, 112, 310);

            AddButtonLabeled(20, 120, 150, 1, 1011557); // Hire Sheriff
            AddButtonLabeled(180, 120, 150, 2, 1011559); // Hire Finance Minister
            AddButtonLabeled(20, 150, 150, 3, 1011558); // Fire Sheriff
            AddButtonLabeled(180, 150, 150, 4, 1011560); // Fire Finance Minister
            AddButtonLabeled(180, 180, 150, 0, 1011393); // Back
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!m_Town.IsCommander(from))
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
                case 1: // Hire Sheriff
                    {
                        if (m_Town.Sheriff != null)
                        {
                            from.SendLocalizedMessage(1010342); // You must fire your Sheriff before you can elect a new one
                        }
                        else
                        {
                            from.SendLocalizedMessage(1010347); // Who shall be your new sheriff
                            from.BeginTarget(12, false, TargetFlags.None, new TargetStateCallback(TownStoneGump.HireSheriff_OnTarget), m_Town);
                        }

                        FactionGumps.CloseGumps(from, typeof(CommanderGump));
                        from.SendGump(new CommanderGump(from, m_Town));

                        break;
                    }
                case 2: // Hire Finance Minister
                    {
                        if (m_Town.Finance != null)
                        {
                            from.SendLocalizedMessage(1010345); // You must fire your finance minister before you can elect a new one
                        }
                        else
                        {
                            from.SendLocalizedMessage(1010348); // Who shall be your new Minister of Finances?
                            from.BeginTarget(12, false, TargetFlags.None, new TargetStateCallback(TownStoneGump.HireFinanceMinister_OnTarget), m_Town);
                        }

                        FactionGumps.CloseGumps(from, typeof(CommanderGump));
                        from.SendGump(new CommanderGump(from, m_Town));

                        break;
                    }
                case 3: // Fire Sheriff
                    {
                        if (m_Town.Sheriff == null)
                        {
                            from.SendLocalizedMessage(1010350); // You need to elect a sheriff before you can fire one
                        }
                        else
                        {
                            from.SendLocalizedMessage(1010349); // You have fired your sheriff
                            m_Town.Sheriff.SendLocalizedMessage(1010270); // You have been fired as Sheriff
                            m_Town.Sheriff = null;
                        }

                        FactionGumps.CloseGumps(from, typeof(CommanderGump));
                        from.SendGump(new CommanderGump(from, m_Town));

                        break;
                    }
                case 4: // Fire Finance Minister
                    {
                        if (m_Town.Finance == null)
                        {
                            from.SendLocalizedMessage(1010352); // You need to elect a financial minister before you can fire one
                        }
                        else
                        {
                            from.SendLocalizedMessage(1010351); // You have fired your financial Minister
                            m_Town.Finance.SendLocalizedMessage(1010151); // You have been fired as Finance Minister
                            m_Town.Finance = null;
                        }

                        FactionGumps.CloseGumps(from, typeof(CommanderGump));
                        from.SendGump(new CommanderGump(from, m_Town));

                        break;
                    }
            }
        }
    }
}