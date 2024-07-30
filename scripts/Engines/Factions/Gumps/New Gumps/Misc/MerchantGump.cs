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

/* Engines/Factions/Gumps/New Gumps/Misc/MerchantGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using Server.Network;

namespace Server.Factions.NewGumps.Misc
{
    public class MerchantGump : BaseFactionGump
    {
        public MerchantGump(Mobile m)
            : base()
        {
            PlayerState player = PlayerState.Find(m);

            MerchantTitle currentTitle = (player == null ? MerchantTitle.None : player.MerchantTitle);

            AddBackground(350, 205);

            AddHtml(20, 15, 310, 26, "<center><i>Merchant Options</i></center>", false, false);

            AddSeparator(20, 40, 310);

            AddHtmlLocalized(20, 50, 310, 20, 1011473, false, false); // Select the title you wish to display

            AddButtonLabeled(20, 70, 150, 1, 1011051, currentTitle == MerchantTitle.None); // None

            int k = 0;

            for (int i = 0; i < MerchantTitles.Info.Length; i++)
            {
                MerchantTitleInfo mti = MerchantTitles.Info[i];

                if (MerchantTitles.IsQualified(m, mti))
                {
                    AddButtonLabeled((k % 2) == 0 ? 20 : 180, 100 + (k / 2) * 30, 150, 2 + i, mti.Title, currentTitle == (MerchantTitle)(i + 1));

                    k++;
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            int titleIndex = (info.ButtonID - 1);
            int tableIndex = (titleIndex - 1);

            if (titleIndex == 0 || tableIndex >= 0 && tableIndex < MerchantTitles.Info.Length)
            {
                FactionStoneGump.SetMerchantTitle(from, (MerchantTitle)titleIndex);

                FactionGumps.CloseGumps(from, typeof(MerchantGump));
                from.SendGump(new MerchantGump(from));
            }
        }
    }
}