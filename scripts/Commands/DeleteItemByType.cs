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

/* Scripts/Commands/DeleteItemByType.cs
 * Changelog : 
 *  2/27/11, Adam
 *		initial checkin
 */

using Server.Diagnostics;
using Server.Mobiles;
using System;
using System.Collections;
using System.Reflection;

namespace Server.Commands
{
    public class DeleteItemByType
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("DeleteItemByType", AccessLevel.GameMaster, new CommandEventHandler(DeleteItemByType_OnCommand));
        }

        [Usage("DeleteItemByType <type> [<property> <value>]")]
        [Description("Finds an item by type with property x set to y.")]
        public static void DeleteItemByType_OnCommand(CommandEventArgs e)
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

                    PlayerMobile pm = e.Mobile as PlayerMobile;
                    LogHelper Logger = new LogHelper("DeleteItemByType.log", e.Mobile, false);

                    // reset jump table
                    pm.JumpIndex = 0;
                    pm.JumpList = new ArrayList();
                    Type tx = ScriptCompiler.FindTypeByName(name);

                    if (tx != null)
                    {
                        foreach (Item item in World.Items.Values)
                        {
                            if (item != null && !item.Deleted && item.GetType() == tx /* tx.IsAssignableFrom(item.GetType())*/)
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

                    // since we already have the items in our jump list, just reuse it for the to-delete list
                    if (pm.JumpList.Count > 0)
                    {
                        foreach (Item ix in pm.JumpList)
                        {
                            if (ix != null)
                            {
                                Console.WriteLine("Deleting object {0}.", ix);
                                ix.Delete();
                            }
                        }
                    }

                    Logger.Finish();
                }
                else
                {
                    e.Mobile.SendMessage("Format: DeleteItemByType <type>");
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
    }
}