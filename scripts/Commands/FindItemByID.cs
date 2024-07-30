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

/* Scripts/Commands/FindItemByID.cs
 * ChangeLog
 *	5/28/2021, Adam
 *		1. Add in the resolver code to locate flippable versions of the ID you are searching for
 *			IdResolver will return a list of flipable ids (or the one if there are no other versions.)
 *		2. Add 'jump table' code. After running this command, you can use the command [next to go to the next occurance of the found items.
 *  3/26/07, Adam
 *      Convert to find an item by ItemID
 *	03/25/05, erlein
 *		Integrated with LogHelper class.
 *		Reformatted so readable (functionality left unchanged).
 *	03/23/05, erlein
 *		Moved to /Scripts/Commands/FindItemByID.cs (for Find* command normalization).
 *		Changed namespace to Server.Commands.
 *	9/15/04, Adam
 *		Added header and copyright
 */

using Server.Diagnostics;
using System;

namespace Server.Commands
{

    public class FindItemByID
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindItemByID", AccessLevel.GameMaster, new CommandEventHandler(FindItemByID_OnCommand));
        }

        [Usage("FindItemByID <ItemID>")]
        [Description("Finds an item by graphic ID.")]
        public static void FindItemByID_OnCommand(CommandEventArgs e)
        {
            // this list will hold the id plus and flipable versions of the item.
            //	for instance, 'two full jars' has two graphic ids. When you are searching for 'two full jars' you certainly want both versions
            System.Collections.ArrayList IdList = new System.Collections.ArrayList();

            try
            {
                if (e.Length == 1)
                {
                    //erl: LogHelper class handles generic logging functionality
                    LogHelper Logger = new LogHelper("FindItemByID.log", e.Mobile, false);

                    // reset jump table
                    Mobiles.PlayerMobile pm = e.Mobile as Mobiles.PlayerMobile;
                    pm.JumpIndex = 0;
                    pm.JumpList = new System.Collections.ArrayList();

                    int ItemId = 0;
                    string sx = e.GetString(0).ToLower();

                    try
                    {
                        if (sx.StartsWith("0x"))
                        {   // assume hex
                            sx = sx.Substring(2);
                            ItemId = int.Parse(sx, System.Globalization.NumberStyles.AllowHexSpecifier);
                        }
                        else
                        {   // assume decimal
                            ItemId = int.Parse(sx);
                        }
                    }
                    catch
                    {
                        e.Mobile.SendMessage("Format: FindItemByID <ItemID>");
                        return;
                    }


                    // Add in the resolver code to locate flippable versions of the ID you are searching for
                    // IdResolver will return a list of flipable ids (or the one if there are no other versions.)
                    IdList = IdResolver(ItemId);

                    // enum the world items
                    foreach (Item item in World.Items.Values)
                    {
                        if (item is Item)
                        {
                            // okay, if the IdList contains the id we are looking for, then log it!
                            if (IdList.Contains(item.ItemID))
                            {
                                Logger.Log(LogType.Item, item);
                                pm.JumpList.Add(item);
                            }
                        }
                    }
                    Logger.Finish();
                }
                else
                    e.Mobile.SendMessage("Format: FindItemByID <ItemID>");
            }
            catch (Exception err)
            {

                e.Mobile.SendMessage("Exception: " + err.Message);
            }

            e.Mobile.SendMessage("Done.");
        }

        public static System.Collections.ArrayList IdResolver(int ItemId)
        {
            System.Collections.ArrayList IdList = new System.Collections.ArrayList();
            foreach (Item item in World.Items.Values)
            {
                if (item is Item)
                {   // make sure to add the one we were originally looking for.
                    if (item.ItemID == ItemId)
                        if (!IdList.Contains(ItemId))
                            IdList.Add(ItemId);

                    Server.Items.FlipableAttribute[] attributes = (Server.Items.FlipableAttribute[])item.GetType().GetCustomAttributes(typeof(Server.Items.FlipableAttribute), false);
                    for (int ix = 0; ix < attributes.Length; ix++)
                    {
                        if (attributes[ix].ItemIDs != null)
                            foreach (int jx in attributes[ix].ItemIDs)
                                if (jx == ItemId)
                                {
                                    // copy all these IDs to our list and return.
                                    foreach (int mx in attributes[ix].ItemIDs)
                                        if (!IdList.Contains(mx))
                                            IdList.Add(mx);
                                    break;
                                }
                    }
                }
            }

            return IdList;
        }
    }
}