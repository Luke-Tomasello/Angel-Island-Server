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

/* Engines/Factions/Gumps/New Gumps/Misc/LeaveGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using Server.Network;
using System;

namespace Server.Factions.NewGumps.Misc
{
    public class LeaveGump : BaseFactionGump
    {
        public LeaveGump(Mobile m)
            : base()
        {
            AddBackground(350, 175);

            AddHtml(20, 15, 310, 26, "<center><i>Resignation</i></center>", false, false);

            AddSeparator(20, 40, 310);

            PlayerState player = PlayerState.Find(m);

            if (player != null && player.IsLeaving)
            {
                TimeSpan remaining = (player.LeaveBegin + FactionConfig.LeavePeriod) - DateTime.UtcNow;

                if (remaining.TotalDays >= 1)
                    AddHtmlLocalized(20, 48, 310, 64, 1042743, remaining.TotalDays.ToString("N0"), 0, true, true);// Your term of service will come to an end in ~1_DAYS~ days.
                else if (remaining.TotalHours >= 1)
                    AddHtmlLocalized(20, 48, 310, 64, 1042741, remaining.TotalHours.ToString("N0"), 0, true, true); // Your term of service will come to an end in ~1_HOURS~ hours.
                else
                    AddHtmlLocalized(20, 48, 310, 64, 1042742, true, true); // Your term of service will come to an end in less than one hour.
            }
            else
            {
                AddHtmlLocalized(20, 48, 310, 64, 1042233, true, true); // You are not in the process of quitting the faction.
            }

            AddSeparator(20, 122, 310);

            if (player.IsLeaving)
                AddButtonLabeled(180, 130, 150, 0, "Okay");
            else
                AddButtonLabeled(20, 130, 150, 1, 3006115); // Resign
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (info.ButtonID == 1) // Resign
            {
                LeaveFactionGump.BeginLeave(from);

                FactionGumps.CloseGumps(from, typeof(LeaveGump));
                from.SendGump(new LeaveGump(from));
            }
        }
    }
}