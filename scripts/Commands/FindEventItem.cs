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

/* scripts\Commands\AccessLevel\FindEventItem.cs
 * Changelog
 *	10/16/22, Adam
 *		Initial creation.
 *		This version of findItem is for GMs and above.
 *		It's restricted EventMoongates(EventSungagtes), EventSpawners, and EventTeleporters
 */

using Server.Diagnostics;
using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Commands
{
    public class FindEventItem
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindEventItem", AccessLevel.GameMaster, new CommandEventHandler(FindEventItem_OnCommand));
        }

        [Usage("FindEventItem [type] ")]
        [Description("Finds all named event items. If no parameters are given, FindEventItem finds all event items.")]
        public static void FindEventItem_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 1 && Utility.ItemRemap(e.Arguments))
            {
                LogHelper Logger = new LogHelper("findEventItem.log", null, false);

                // reset jump table
                PlayerMobile pm = e.Mobile as PlayerMobile;
                pm.JumpIndex = 0;
                pm.JumpList = new ArrayList();

                // what are we looking for?
                string name = e.GetString(0);
                Type type = ScriptCompiler.FindTypeByName(name);

                if (type == null)
                {
                    if (e.Mobile != null)
                        e.Mobile.SendMessage("No type with that name was found.");
                    return;
                }

                foreach (Item item in World.Items.Values)
                {
                    if (item != null && item.Map != null && item.Map != Map.Internal && item.Deleted == false)
                        if (type.IsAssignableFrom(item.GetType()))
                            pm.JumpList.Add(item);
                }
                Logger.Log(LogType.Text, string.Format("{2} JumpList was loaded with {0} {1}(s).",
                    (e.Mobile as PlayerMobile).JumpList.Count, type.Name, e.Mobile));
                Logger.Finish();

                e.Mobile.SendMessage("Your JumpList has been loaded with {0} {1}(s).", (e.Mobile as PlayerMobile).JumpList.Count, type.Name);
            }
            else
            {
                // Badly formatted
                e.Mobile.SendMessage("Format: FindEventItem [spawner | moongate | teleporter]");
            }
        }
    }
}