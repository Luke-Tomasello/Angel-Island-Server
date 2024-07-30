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

/* Scripts/Commands/FindMultiByType.cs
 * Changelog : 
 *	3/9/07, Adam
 *		first time checkin
 */

using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Commands
{
    public class FindMultiByType
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindMultiByType", AccessLevel.Administrator, new CommandEventHandler(FindMultiByType_OnCommand));
        }

        [Usage("FindMultiByType <type>")]
        [Description("Finds a multi by type.")]
        public static void FindMultiByType_OnCommand(CommandEventArgs e)
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
                    LogHelper Logger = new LogHelper("FindMultiByType.log", e.Mobile, false);

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
                    Logger.Finish();
                }
                else
                {
                    e.Mobile.SendMessage("Format: FindMultiByType <type>");

#if SIMPLE_LIST // sorted by date created
                    Dictionary<DateTime, BaseHouse> list2 = new Dictionary<DateTime, BaseHouse>();
                    List<DateTime> list3 = new List<DateTime>();
                    foreach (ArrayList list in Server.Multis.BaseHouse.Multis.Values)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            BaseHouse house = list[i] as BaseHouse;
                            if (house.Owner.AccessLevel == AccessLevel.Player && !(house is Tent) && !(house is SiegeTent))
                            {
                                list2.Add(house.Created, house);
                                list3.Add(house.Created);
                            }
                        }
                    }

                    list3.Sort();
                    LogHelper Logger = new LogHelper("HousingList.log", e.Mobile, false);
                    foreach (DateTime dt in list3)
                    {
                        BaseHouse bh = list2[dt];
                        Logger.Log(LogType.Text,string.Format("Owner: {0}, Location: {1}, Built On: {2}",bh.Owner.Name, bh.Location, bh.Created.ToString()));
                    }
                    Logger.Finish();
#endif
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
    }
}