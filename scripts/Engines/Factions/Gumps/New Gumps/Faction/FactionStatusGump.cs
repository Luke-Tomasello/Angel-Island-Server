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

/* Engines/Factions/Gumps/New Gumps/Faction/FactionStatusGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using Server.Network;
using System;

namespace Server.Factions.NewGumps.FactionMenu
{
    public class FactionStatusGump : BaseFactionGump
    {
        protected override GumpStyle Style { get { return GumpStyle.Parchment; } }

        private Faction m_Faction;

        public FactionStatusGump(Mobile m, Faction faction)
            : base()
        {
            m_Faction = faction;

            AddBackground(632, 440);

            AddButtonLabeled(66, 40, 162, 1000, "Faction Status", true);
            AddButtonLabeled(243, 40, 162, 1001, "Faction Roster", false);
            AddButtonLabeled(420, 40, 163, 1002, "City Status", false);

            AddStatistic(65, 80, 130, "<i>Faction</i>", FormatFaction(m_Faction));
            AddStatistic(65, 114, 130, "<i>Led By</i>", FormatName(m_Faction.Commander));
            AddStatistic(65, 148, 130, "<i>Tithe Rate</i>", string.Concat(m_Faction.Tithe, '%'));

            AddSeparator(65, 196, 518);

            AddHtml(66, 216, 516, 100, m_Faction.Definition.About, true, true);
            AddButtonLabeled(66, 333, 127, 1, "Statistics");
            AddButtonLabeled(208, 333, 180, 2, "Honor Leadership");
            AddButtonLabeled(403, 333, 180, 3, "Vote For Leadership");

            if (m_Faction.IsCommander(m))
                AddButtonLabeled(66, 373, 127, 4, "Commander");

            AddButtonLabeled(208, 373, 180, 5, "Merchant Title");
            AddButtonLabeled(403, 373, 180, 6, "Leave This Faction");

            if (m.AccessLevel >= AccessLevel.GameMaster)
                AddButtonLabeled(473, 148, 110, 10, "GM Options");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!m_Faction.IsMember(from))
                return;

            int buttonID = info.ButtonID;

            switch (buttonID)
            {
                case 1: // Statistics
                    {
                        PlayerState player = PlayerState.Find(from);

                        if (player != null)
                        {
                            FactionGumps.CloseGumps(from, typeof(FactionStatusGump));
                            from.SendGump(new FactionStatusGump(from, m_Faction));

                            FactionGumps.CloseGumps(from, typeof(Misc.StatisticsGump));
                            from.SendGump(new Misc.StatisticsGump(from, player));
                        }

                        break;
                    }
                case 2: // Honor Leadership
                    {
                        Faction faction = Faction.Find(from);

                        if (faction != null)
                        {
                            faction.BeginHonorLeadership(from);

                            FactionGumps.CloseGumps(from, typeof(FactionStatusGump));
                            from.SendGump(new FactionStatusGump(from, m_Faction));
                        }

                        break;
                    }
                case 3: // Vote For Leadership
                    {
                        FactionGumps.CloseGumps(from, typeof(FactionStatusGump));
                        from.SendGump(new FactionStatusGump(from, m_Faction));

                        FactionGumps.CloseGumps(from, typeof(Misc.ElectionGump));
                        from.SendGump(new Misc.ElectionGump(from, m_Faction));

                        break;
                    }
                case 4: // Commander
                    {
                        if (m_Faction.IsCommander(from))
                        {
                            FactionGumps.CloseGumps(from, typeof(FactionStatusGump));
                            from.SendGump(new FactionStatusGump(from, m_Faction));

                            FactionGumps.CloseGumps(from, typeof(Misc.CommanderGump));
                            from.SendGump(new Misc.CommanderGump(from, m_Faction));
                        }

                        break;
                    }
                case 5: // Merchant Title
                    {
                        FactionGumps.CloseGumps(from, typeof(FactionStatusGump));
                        from.SendGump(new FactionStatusGump(from, m_Faction));

                        FactionGumps.CloseGumps(from, typeof(Misc.MerchantGump));
                        from.SendGump(new Misc.MerchantGump(from));

                        break;
                    }
                case 6: // Leave This Faction
                    {
                        FactionGumps.CloseGumps(from, typeof(FactionStatusGump));
                        from.SendGump(new FactionStatusGump(from, m_Faction));

                        FactionGumps.CloseGumps(from, typeof(Misc.LeaveGump));
                        from.SendGump(new Misc.LeaveGump(from));

                        break;
                    }
                case 10: // GM Options
                    {
                        if (from.AccessLevel >= AccessLevel.GameMaster)
                        {
                            FactionGumps.CloseGumps(from, typeof(FactionStatusGump));
                            from.SendGump(new FactionStatusGump(from, m_Faction));

                            from.SendGump(new PropertiesGump(from, m_Faction.State));
                        }

                        break;
                    }
                case 1000: // Faction Status
                    {
                        FactionGumps.CloseGumps(from, typeof(FactionStatusGump));
                        from.SendGump(new FactionStatusGump(from, m_Faction));

                        break;
                    }
                case 1001: // Faction Roster
                    {
                        FactionGumps.CloseGumps(from, typeof(FactionRosterGump));
                        from.SendGump(new FactionRosterGump(from, m_Faction));

                        break;
                    }
                case 1002: // City Status
                    {
                        FactionGumps.CloseGumps(from, typeof(CityStatusGump));
                        from.SendGump(new CityStatusGump(from, m_Faction));

                        break;
                    }
            }
        }
    }
}