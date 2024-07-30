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

using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using System;

namespace Server.Factions
{
    public class LeaveFactionGump : FactionGump
    {
        private PlayerMobile m_From;
        private Faction m_Faction;

        public LeaveFactionGump(PlayerMobile from, Faction faction)
            : base(20, 30)
        {
            m_From = from;
            m_Faction = faction;

            AddBackground(0, 0, 270, 120, 5054);
            AddBackground(10, 10, 250, 100, 3000);

            if (from.Guild is Guild && ((Guild)from.Guild).Leader == from)
                AddHtmlLocalized(20, 15, 230, 60, 1018057, true, true); // Are you sure you want your entire guild to leave this faction?
            else
                AddHtmlLocalized(20, 15, 230, 60, 1018063, true, true); // Are you sure you want to leave this faction?

            AddHtmlLocalized(55, 80, 75, 20, 1011011, false, false); // CONTINUE
            AddButton(20, 80, 4005, 4007, 1, GumpButtonType.Reply, 0);

            AddHtmlLocalized(170, 80, 75, 20, 1011012, false, false); // CANCEL
            AddButton(135, 80, 4005, 4007, 2, GumpButtonType.Reply, 0);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            switch (info.ButtonID)
            {
                case 1: // continue
                    {
                        BeginLeave(m_From);
                        break;
                    }
                case 2: // cancel
                    {
                        m_From.SendLocalizedMessage(500737); // Canceled resignation.
                        break;
                    }
            }
        }

        public static void BeginLeave(Mobile from)
        {
            Guild guild = from.Guild as Guild;

            if (guild == null)
            {
                PlayerState pl = PlayerState.Find(from);

                if (pl != null)
                {
                    pl.LeaveBegin = DateTime.UtcNow;

                    if (FactionConfig.LeavePeriod == TimeSpan.FromDays(3.0))
                        from.SendLocalizedMessage(1005065); // You will be removed from the faction in 3 days
                    else
                        from.SendMessage("You will be removed from the faction in {0} days.", FactionConfig.LeavePeriod.TotalDays);
                }
            }
            else if (guild.Leader != from)
            {
                from.SendLocalizedMessage(1005061); // You cannot quit the faction because you are not the guild master
            }
            else
            {
                from.SendLocalizedMessage(1042285); // Your guild is now quitting the faction.

                for (int i = 0; i < guild.Members.Count; ++i)
                {
                    Mobile mob = (Mobile)guild.Members[i];
                    PlayerState pl = PlayerState.Find(mob);

                    if (pl != null)
                    {
                        pl.LeaveBegin = DateTime.UtcNow;

                        if (FactionConfig.LeavePeriod == TimeSpan.FromDays(3.0))
                            mob.SendLocalizedMessage(1005060); // Your guild will quit the faction in 3 days
                        else
                            mob.SendMessage("Your guild will quit the faction in {0} days.", FactionConfig.LeavePeriod.TotalDays);
                    }
                }
            }
        }
    }
}