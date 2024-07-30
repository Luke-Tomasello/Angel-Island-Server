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

/* Scripts/Commands/FindMobileByType.cs
 * Changelog : 
 *  6/26/2023, Adam
 *      Allow filtering for region name
 *	2/15/11, Adam
 *		Check for a null type when using reflection to find the type
 *  11/21/10, Adam
 *      1. Add reflection and IsAssignableFrom to determine if what we find is derived from what we're searching for.
 *      We're emulating the 'is' keyword for a variable Type.
 *      Eg. You want to find only ChampSpecific, then search for "ChampSpecific". But find all champ engines derived from
 *      ChampEngine, search for "ChampEngine".
 *      2. Make available to GMs for world building/cleanup if the server is launched with the -build command
 *	6/18/08, Adam
 *      first time checkin
 */

using Server.Diagnostics;
using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Commands
{
    public class FindMobileByType
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindMobileByType", AccessLevel.GameMaster, new CommandEventHandler(FindMobileByType_OnCommand));
        }

        [Usage("FindMobileByType <type> [region]")]
        [Description("Finds a mobile by type [in region].")]
        public static void FindMobileByType_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.Length <= 2 && e.Length > 0)
                {
                    if (e == null || e.Mobile == null || e.Mobile is PlayerMobile == false)
                        return;

                    // if you are a GM the world needs to be in 'Build' mode to access this comand
                    if (e.Mobile.AccessLevel < AccessLevel.Administrator && Core.Building == false)
                    {
                        e.Mobile.SendMessage("The server must be in build mode for you to access this command.");
                        return;
                    }

                    PlayerMobile pm = e.Mobile as PlayerMobile;
                    LogHelper Logger = new LogHelper("FindMobileByType.log", e.Mobile, false);
                    string name = e.GetString(0);
                    Region region = null;
                    if (e.Length > 1)
                        region = Region.FindByName(e.GetString(1), e.Mobile.Map);

                    // reset jump table
                    pm.JumpIndex = 0;
                    pm.JumpList = new ArrayList();
                    Type tx = ScriptCompiler.FindTypeByName(name);

                    if (tx != null)
                    {
                        foreach (Mobile mob in World.Mobiles.Values)
                        {
                            if (mob != null && !mob.Deleted && tx.IsAssignableFrom(mob.GetType()))
                            {
                                if (region != null)
                                {
                                    if (region.Contains(mob.Location))
                                    {
                                        pm.JumpList.Add(mob);
                                        Logger.Log(LogType.Mobile, mob);
                                    }
                                }
                                else
                                {
                                    pm.JumpList.Add(mob);
                                    Logger.Log(LogType.Mobile, mob);
                                }
                            }
                        }
                    }
                    else
                    {
                        e.Mobile.SendMessage("{0} is not a recognized type.", name);
                    }
                    Logger.Finish();

                    e.Mobile.SendMessage("{0} mobiles matching this type.", pm.JumpList.Count);
                }
                else
                {
                    e.Mobile.SendMessage("Format: FindMobileByType <type> [region]");
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
    }
}