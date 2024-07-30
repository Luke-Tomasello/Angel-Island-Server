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

/* Changelog :
 *	7/9/2024, Adam
 *      Created
 */

using Server.Diagnostics;
using System;
using System.Collections.Generic;

namespace Server.Commands
{
    public class FindItemBySerial
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindItemBySerial", AccessLevel.Administrator, new CommandEventHandler(FindItemBySerial_OnCommand));
        }

        [Usage("FindItemBySerial <name>")]
        [Description("Finds an item by serial.")]
        public static void FindItemBySerial_OnCommand(CommandEventArgs e)
        {
            if (e.Length > 0)
            {
                LogHelper Logger = new LogHelper("FindItemBySerial.log", e.Mobile, false);
                // reset jump table
                Mobiles.PlayerMobile pm = e.Mobile as Mobiles.PlayerMobile;
                pm.JumpIndex = 0;
                pm.JumpList = new System.Collections.ArrayList();

                Int32 value = 0;
                Utility.StringToInt(e.GetString(0), ref value);

                foreach (Item item in World.Items.Values)
                    if (item.Serial == value)
                    {
                        Logger.Log(LogType.Item, item);
                        Logger.Finish();
                        pm.JumpList.Add(item);
                        List<string> list = new();
                        Commands.CommandHandlers.Where(item, item.Map, list);
                        if (list.Count > 0)
                            list[0] = list[0].Replace("You are", "Item");
                        foreach (string s in list)
                            pm.SendMessage(s);
                        break;
                    }

                if (pm.JumpList.Count == 0)
                    Logger.Finish();
            }
            else
            {
                e.Mobile.SendMessage("Format: FindItemBySerial <name>");
            }
        }
    }
}