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

/* Scripts/Commands/FindPlayer.cs
 * Changelog
 *	9/17/2023, Adam
 *		Initial creation.
 */
using Server.Gumps;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Commands
{
    public class FindPlayer
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindPlayer", AccessLevel.GameMaster, new CommandEventHandler(FindPlayer_OnCommand));
        }

        [Usage("FindPlayer <name>")]
        [Description("Finds a player by name fast.")]
        public static void FindPlayer_OnCommand(CommandEventArgs e)
        {
            PlayerMobile caller = e.Mobile as PlayerMobile;
            caller.JumpIndex = 0;
            caller.JumpList = new System.Collections.ArrayList();
            List<PlayerMobile> list = new();
            if (e.Length == 1)
            {
                string text = e.GetString(0).ToLower();

                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is PlayerMobile pm && !pm.Deleted)
                        if (pm.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                        {
                            caller.JumpList.Add(pm);
                            list.Add(pm);
                        }

                }
                if (list.Count > 0)
                {
                    caller.SendMessage("Found {0} player(s) matching '{1}'", list.Count, text);
                    foreach (var px in list)
                        caller.SendMessage("{0}: {1}", px.Name, px.Serial);

                    PlayerMobile match = list.FirstOrDefault(p => p.Name.Equals(text, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        caller.SendMessage("player {0}: {1} is an exact match", match.Name, match.Serial);
                        if (match.NetState != null)
                            caller.SendGump(new ClientGump(caller, match.NetState));
                        else
                            caller.SendGump(new PropertiesGump(caller, match));
                    }
                }
                else
                    caller.SendMessage("Player not found");
            }
            else
            {
                caller.SendMessage("Format: FindPlayer <name>");
            }
        }
    }
}