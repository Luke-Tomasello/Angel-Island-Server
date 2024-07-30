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

/* Engines/Factions/Gumps/New Gumps/Misc/StatisticsGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

namespace Server.Factions.NewGumps.Misc
{
    public class StatisticsGump : BaseFactionGump
    {
        public StatisticsGump(Mobile m, PlayerState player)
            : base()
        {
            AddBackground(350, 215);

            AddHtml(20, 15, 310, 26, "<center><i>Statistics</i></center>", false, false);

            AddSeparator(20, 40, 310);

            AddStatistic(20, 50, 150, "<i>Name</i>", FormatName(player.Mobile));
            AddStatistic(20, 80, 150, "<i>Guild</i>", FormatGuild(player.Mobile));
            AddStatistic(20, 110, 150, "<i>Role</i>", FormatRole(player));
            AddStatistic(20, 140, 150, "<i>Score</i>", player.KillPoints.ToString());
            AddStatistic(20, 170, 150, "<i>Rank</i>", player.Rank.Rank.ToString());
        }
    }
}