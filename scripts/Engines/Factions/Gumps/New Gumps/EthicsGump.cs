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

/* Engines/Factions/Gumps/New Gumps/EthicsGump.cs
 * CHANGELOG:
 *  1/9/23, Yoar
 *      Custom ethics menu. Initial commit.
 */

using Server.Ethics;
using Server.Gumps;
using Server.Network;
using System;

namespace Server.Factions.NewGumps.Ethics
{
    public class EthicsGump : BaseFactionGump
    {
        private int m_Power;

        public EthicsGump(Player pl)
            : base()
        {
            m_Power = pl.Power;

            Power[] powers = pl.Ethic.Definition.Powers;

            AddBackground(350, 95 + 30 * ((powers.Length + 1) / 2));

            AddHtml(20, 15, 310, 26, String.Format("<center><i>{0} Powers</i></center>", pl.Ethic.Definition.Title.String), false, false);

            AddSeparator(20, 40, 310);

            AddStatistic(20, 50, 150, "<i>Life Force</i>", pl.Power.ToString());

            for (int i = 0; i < powers.Length; i++)
            {
                PowerDefinition def = powers[i].Definition;

                AddButtonLabeled((i % 2) == 0 ? 20 : 180, 80 + (i / 2) * 30, 150, 1 + i, def.Name.String);
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Player pl = Player.Find(sender.Mobile);

            if (pl == null)
                return;

            Power[] powers = pl.Ethic.Definition.Powers;

            int index = info.ButtonID - 1;

            if (index < 0 || index >= powers.Length)
                return;

            Power power = powers[index];

            if (power.CheckInvoke(pl))
                power.BeginInvoke(pl);

            FactionGumps.CloseGumps(sender.Mobile, typeof(EthicsGump));
            sender.Mobile.SendGump(new EthicsGump(pl));
        }

        public static void Update(Player pl)
        {
            EthicsGump gump = pl.Mobile.FindGump(typeof(EthicsGump)) as EthicsGump;

            if (gump != null && gump.m_Power != pl.Power)
            {
                FactionGumps.CloseGumps(pl.Mobile, typeof(EthicsGump));
                pl.Mobile.SendGump(new EthicsGump(pl));
            }
        }
    }
}