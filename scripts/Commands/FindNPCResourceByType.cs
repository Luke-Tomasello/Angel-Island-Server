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

/* Scripts/Commands/FindNPCResourceByType.cs
 * Changelog : 
 *  5/30/11, Adam
 *		first time checkin
 */

using Server.Diagnostics;
using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Commands
{
    public class FindNPCResourceByType
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindNPCResourceByType", AccessLevel.GameMaster, new CommandEventHandler(FindItemByType_OnCommand));
        }

        [Usage("FindNPCResourceByType <type>")]
        [Description("Finds an resource sold by an NPC by type.")]
        public static void FindItemByType_OnCommand(CommandEventArgs e)
        {
            try
            {

                if (e == null || e.Mobile == null || e.Mobile is PlayerMobile == false)
                    return;

                string name = null;

                if (e.Length >= 1)
                {
                    name = e.GetString(0);

                    // if you are a GM the world needs to be in 'Build' mode to access this comand
                    if (e.Mobile.AccessLevel < AccessLevel.Administrator && Core.Building == false)
                    {
                        e.Mobile.SendMessage("The server must be in build mode for you to access this command.");
                        return;
                    }

                    PlayerMobile pm = e.Mobile as PlayerMobile;
                    LogHelper Logger = new LogHelper("FindNPCResourceByType.log", e.Mobile, false);

                    // reset jump table
                    pm.JumpIndex = 0;
                    pm.JumpList = new ArrayList();
                    Type tx = ScriptCompiler.FindTypeByName(name);

                    if (tx != null)
                    {
                        foreach (Mobile mob in World.Mobiles.Values)
                        {
                            if (mob is BaseVendor == false)
                                continue;

                            BaseVendor vendor = mob as BaseVendor;

                            if (vendor.Inventory == null || vendor.Inventory.Count == 0)
                                continue;

                            foreach (object ox in vendor.Inventory)
                            {
                                if (ox is SBInfo == false)
                                    continue;

                                SBInfo sbi = ox as SBInfo;

                                if (sbi.BuyInfo == null || sbi.BuyInfo.Count == 0)
                                    continue;

                                ArrayList bi = sbi.BuyInfo;

                                foreach (GenericBuyInfo gbi in bi)
                                {
                                    if (tx.IsAssignableFrom(gbi.Type))
                                    {
                                        pm.JumpList.Add(vendor);
                                        Logger.Log(LogType.Mobile, vendor);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        e.Mobile.SendMessage("{0} is not a recognized type.", name);
                    }
                    Logger.Finish();
                }
                else
                {
                    e.Mobile.SendMessage("Format: FindNPCResourceByType <type>");
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
    }
}