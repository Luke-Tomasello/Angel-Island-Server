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

/* Scripts/Commands/DeleteMultiByType.cs
 * Changelog : 
 *	8/21/2023, Adam
 *		first time checkin
 */

using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Commands
{
    public class DeleteMultiByType
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("DeleteMultiByType", AccessLevel.Administrator, new CommandEventHandler(DeleteMultiByType_OnCommand));
        }

        [Usage("DeleteMultiByType <type>")]
        [Description("Deletes all multi by type.")]
        public static void DeleteMultiByType_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.Length == 1)
                {

                    Type t = ScriptCompiler.FindTypeByName(e.GetString(0));
                    if (t == null)
                    {
                        e.Mobile.SendMessage("No type with that name was found.");
                        return;
                    }
                    PlayerMobile pm = (PlayerMobile)e.Mobile;
                    pm.JumpIndex = 0;
                    pm.JumpList = new ArrayList();
                    LogHelper Logger = new LogHelper("DeleteMultiByType.log", e.Mobile, false);

                    foreach (Item item in World.Items.Values)
                    {
                        if (item != null && item is BaseMulti)
                        {
                            if (t.IsAssignableFrom(item.GetType()))
                            {
                                Logger.Log(LogType.Item, item);
                                pm.JumpList.Add(item);
                            }
                        }
                    }
                    foreach (object o in pm.JumpList)
                        if (o is BaseMulti bm)
                            bm.Delete();
                    Logger.Finish();

                    e.Mobile.SendMessage("{0} multis deleted.", pm.JumpList.Count);
                }
                else
                {
                    e.Mobile.SendMessage("Format: DeleteMultiByType <type>");
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
    }
}