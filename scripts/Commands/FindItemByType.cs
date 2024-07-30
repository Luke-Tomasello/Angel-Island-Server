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

/* Scripts/Commands/FindItemByType.cs
 * Changelog : 
 *  12/22/10, Adam
 *		Add 'property' and 'value' processing so that you can search for and item type with value X
 *		Example:
 *		[FindItemByType Spawner Running False
 *  11/21/10, Adam
 *      1. Add reflection and IsAssignableFrom to determine if what we find is derived from what we're searching for.
 *      We're emulating the 'is' keyword for a variable Type.
 *      Eg. You want to find only ChampSpecific, then search for "ChampSpecific". But find all champ engines derived from
 *      ChampEngine, search for "ChampEngine".
 *      2. Make available to GMs for world building/cleanup if the server is launched with the -build command
 *	3/10/10, Adam
 *		Add Jump List processing
 *	3/9/07, Adam
 *      Convert to a "find item by type" command
 *	9/7/06, Adam
 *		Remove the hack and make into: Find(multi)ByType 
 *	06/28/06, Adam
 *		Find Mobile by Type (currently hacked to find PlayerBarkeeper only)
 */

using Server.Diagnostics;
using Server.Mobiles;
using System;
using System.Collections;
using System.Reflection;

namespace Server.Commands
{
    public class FindItemByType
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindItemByType", AccessLevel.GameMaster, new CommandEventHandler(FindItemByType_OnCommand));
        }

        [Usage("FindItemByType <type> [<property> <value>]")]
        [Description("Finds an item by type with property x set to y.")]
        public static void FindItemByType_OnCommand(CommandEventArgs e)
        {
            try
            {

                if (e == null || e.Mobile == null || e.Mobile is PlayerMobile == false)
                    return;

                string sProp = null;
                string sVal = null;
                string name = null;

                if (e.Length >= 1)
                {
                    name = e.GetString(0);

                    if (e.Length >= 2)
                        sProp = e.GetString(1);

                    if (e.Length >= 3)
                        sVal = e.GetString(2);

                    // if you are a GM the world needs to be in 'Build' mode to access this comand
                    if (e.Mobile.AccessLevel < AccessLevel.Administrator && Core.Building == false)
                    {
                        e.Mobile.SendMessage("The server must be in build mode for you to access this command.");
                        return;
                    }

                    LogHelper Logger = new LogHelper("FindItemByType.log", e.Mobile, false);

                    // reset jump table
                    PlayerMobile pm = e.Mobile as PlayerMobile;
                    pm.JumpIndex = 0;
                    pm.JumpList = new ArrayList();
                    Type tx = ScriptCompiler.FindTypeByName(name);

                    if (tx != null)
                    {
                        foreach (Item item in World.Items.Values)
                        {
                            if (item != null && !item.Deleted && tx.IsAssignableFrom(item.GetType()))
                            {
                                // read the properties
                                PropertyInfo[] allProps = item.GetType().GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

                                if (sProp != null)
                                {
                                    foreach (PropertyInfo prop in allProps)
                                    {
                                        if (prop.Name.ToLower() == sProp.ToLower())
                                        {
                                            bool ok = false;
                                            string val = Properties.GetValue(e.Mobile, item, sProp);

                                            // match a null value
                                            if ((val == null || val.Length == 0 || val.EndsWith(Properties.PropNull, StringComparison.CurrentCultureIgnoreCase)) && (sVal == null || sVal.Length == 0))
                                                ok = true;

                                            // see if the property matches
                                            else if (val != null && sVal != null)
                                            {
                                                string[] toks = val.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                                if (toks.Length >= 3 && toks[2].Equals(sVal, StringComparison.CurrentCultureIgnoreCase))
                                                    ok = true;
                                                else
                                                    break;
                                            }

                                            if (ok)
                                            {
                                                pm.JumpList.Add(item);
                                                Logger.Log(LogType.Item, item);
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {   // no prop to check, everything matches
                                    pm.JumpList.Add(item);
                                    Logger.Log(LogType.Item, item);
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
                    e.Mobile.SendMessage("Format: FindItemByType <type>");
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
    }
}