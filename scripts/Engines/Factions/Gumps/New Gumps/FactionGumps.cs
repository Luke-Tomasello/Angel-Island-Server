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

/* Engines/Factions/Gumps/New Gumps/FactionGumps.cs
 * CHANGELOG:
 *  1/10/21, Yoar
 *      Now properly closing factions gumps on the server end as well.
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Factions.NewGumps
{
    public static class FactionGumps
    {
        public static bool Enabled { get { return FactionConfig.NewGumps; } }

        public static void Initialize()
        {
            CommandSystem.Register("Faction", AccessLevel.Player, new CommandEventHandler(Faction_OnCommand));
            CommandSystem.Register("Town", AccessLevel.Player, new CommandEventHandler(Town_OnCommand));
        }

        [Usage("Faction")]
        [Description("Opens the faction gump.")]
        private static void Faction_OnCommand(CommandEventArgs e)
        {
            if (!Enabled)
            {
                e.Mobile.SendMessage("This feature is currently disabled.");
                return;
            }

            PlayerState player = PlayerState.Find(e.Mobile);

            if (player == null)
            {
                e.Mobile.SendMessage("You are not in a faction!");
            }
            else
            {
                CloseGumps(e.Mobile, typeof(FactionMenu.FactionStatusGump));
                e.Mobile.SendGump(new FactionMenu.FactionStatusGump(e.Mobile, player.Faction));
            }
        }

        [Usage("Town")]
        [Description("Opens the faction town gump.")]
        private static void Town_OnCommand(CommandEventArgs e)
        {
            if (!Enabled)
            {
                e.Mobile.SendMessage("This feature is currently disabled.");
                return;
            }

            PlayerState player = PlayerState.Find(e.Mobile);

            Town town = Town.FromRegion(e.Mobile.Region);

            if (player == null)
            {
                e.Mobile.SendMessage("You are not in a faction!");
            }
            else if (town == null || town.Owner != player.Faction)
            {
                e.Mobile.SendMessage("You are not in a faction controlled town!");
            }
            else
            {
                CloseGumps(e.Mobile, typeof(TownMenu.TownGump));
                e.Mobile.SendGump(new TownMenu.TownGump(e.Mobile, town));
            }
        }

        public static void OpenJoinGump(Mobile m, Faction faction)
        {
            CloseGumps(m, typeof(FactionMenu.JoinGump));
            m.SendGump(new FactionMenu.JoinGump(m, faction));
        }

        public static void OpenFactionGump(Mobile m, Faction faction)
        {
            CloseGumps(m, typeof(FactionMenu.FactionStatusGump));
            m.SendGump(new FactionMenu.FactionStatusGump(m, faction));
        }

        public static void OpenTownGump(Mobile m, Town town)
        {
            CloseGumps(m, typeof(TownMenu.TownGump));
            m.SendGump(new TownMenu.TownGump(m, town));
        }

        public static void OpenSheriffGump(Mobile m, Town town)
        {
            CloseGumps(m, typeof(TownMenu.SheriffGump));
            m.SendGump(new TownMenu.SheriffGump(m, town));
        }

        public static void OpenFinanceGump(Mobile m, Town town)
        {
            CloseGumps(m, typeof(TownMenu.FinanceGump));
            m.SendGump(new TownMenu.FinanceGump(m, town));
        }

        public static int GetGumpID(Type type)
        {
            return type.Namespace.GetHashCode();
        }

        public static void CloseGumps(Mobile m, Type type)
        {
            NetState ns = m.NetState;

            if (ns == null)
                return;

            ns.Send(new CloseGump(GetGumpID(type), 0));

            if (ns.Gumps != null)
            {
                List<Gump> toClose = null;

                foreach (Gump gump in ns.Gumps)
                {
                    if (gump.GetType().Namespace == type.Namespace)
                    {
                        if (toClose == null)
                            toClose = new List<Gump>();

                        toClose.Add(gump);
                    }
                }

                if (toClose != null)
                {
                    foreach (Gump gump in toClose)
                        ns.RemoveGump(gump);
                }
            }
        }
    }
}