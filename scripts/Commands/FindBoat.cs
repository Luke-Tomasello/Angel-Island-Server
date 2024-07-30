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

/* Scripts/Commands/FindBoat.cs
 * Changelog
 *	12/18/2023, Adam
 *		Initial creation.
 */
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Commands
{
    public class FindBoat
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindBoat", AccessLevel.GameMaster, new CommandEventHandler(FindBoat_OnCommand));
        }

        [Usage("FindBoat <owner name>")]
        [Description("Finds a boat by player name by name fast.")]
        public static void FindBoat_OnCommand(CommandEventArgs e)
        {
            PlayerMobile caller = e.Mobile as PlayerMobile;
            caller.JumpIndex = 0;
            caller.JumpList = new System.Collections.ArrayList();
            List<BaseBoat> list = new();
            if (e.Length == 1)
            {
                string text = e.GetString(0).ToLower();

                foreach (Item i in World.Items.Values)
                {
                    if (i is BaseBoat bb && !bb.Deleted)
                        if (bb.Owner != null && bb.Owner.Name != null && bb.Owner.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                        {
                            caller.JumpList.Add(bb);
                            list.Add(bb);
                        }

                }
                if (list.Count > 0)
                {
                    caller.SendMessage("Found {0} boats(s) matching '{1}'", list.Count, text);
                    foreach (var px in list)
                        caller.SendMessage("{0}: {1}", px.Name, px.Serial);

                    BaseBoat bmatch = list.FirstOrDefault(p => p.Owner.Name.Equals(text, StringComparison.OrdinalIgnoreCase));
                    PlayerMobile match = bmatch.Owner != null ? bmatch.Owner as PlayerMobile : null;
                    if (match != null)
                    {
                        caller.SendMessage("player {0}: {1} is an exact match", match.Name, match.Serial);
                        caller.SendGump(new PropertiesGump(caller, bmatch));
                    }
                }
                else
                    caller.SendMessage("Player not found");
            }
            else
            {
                caller.SendMessage("Format: FindBoat <name>");
            }
        }
    }
}