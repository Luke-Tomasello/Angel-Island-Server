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

/* Engines/Factions/Gumps/New Gumps/Faction/JoinGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using Server.Network;

namespace Server.Factions.NewGumps.FactionMenu
{
    public class JoinGump : BaseFactionGump
    {
        protected override GumpStyle Style { get { return GumpStyle.Parchment; } }

        private Faction m_Faction;

        public JoinGump(Mobile m, Faction faction)
            : base()
        {
            m_Faction = faction;

            AddPage(0);

            AddBackground(632, 440);

            AddStatistic(65, 80, 130, "<i>Faction</i>", m_Faction.Definition.FriendlyName);
            AddStatistic(65, 114, 130, "<i>Led By</i>", FormatName(m_Faction.Commander));
            AddStatistic(65, 148, 130, "<i>Tithe Rate</i>", m_Faction.Tithe + "%");

            AddSeparator(65, 196, 518);

            AddHtml(66, 216, 516, 100, m_Faction.Definition.About, true, true);
            AddButtonLabeled(66, 373, 150, 1, "Join This Faction");
            AddButtonLabeled(433, 373, 150, 0, "Cancel");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
                m_Faction.OnJoinAccepted(sender.Mobile);
        }
    }
}