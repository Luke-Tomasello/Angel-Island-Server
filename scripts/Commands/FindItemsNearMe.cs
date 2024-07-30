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

/* Scripts\Commands\FindItemsNearMe.cs
 * ChangeLog
 *  5/29/2023
 *		Initial creation - designed to retrieve list of items near GM
 *		distance passed as parameter.
*/
using Server.Diagnostics;
using Server.Mobiles;
using System.Collections;
using System.Collections.Generic;

namespace Server.Commands
{
    public class FindItemsNearMe
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindItemsNearMe", AccessLevel.GameMaster, new CommandEventHandler(FindItemsNearMe_OnCommand));
        }

        [Usage("FindItemsNearMe <tile range>")]
        [Description("Finds locations of all items within distance.")]
        private static void FindItemsNearMe_OnCommand(CommandEventArgs arg)
        {
            Mobile from = arg.Mobile;

            int X = from.Location.X;
            int Y = from.Location.Y;

            int TileRange = arg.GetInt32(0);
            if (int.TryParse(arg.GetString(0), out TileRange) == false)
            {
                arg.Mobile.SendMessage("Usage: FindItemsNearMe <tile range>");
                return;
            }

            // Perform search and retrieve list of spawner matches
            LogHelper Logger = new LogHelper("FindItemsNearMe.log", from, true);
            List<Item> ItemList = FindItems(X, Y, TileRange);

            if (ItemList.Count > 0)
            {
                PlayerMobile pm = arg.Mobile as PlayerMobile;

                // Have results so sort, loop and display message for each match
                ItemList.Sort();

                // reset jump table
                pm.JumpIndex = 0;
                pm.JumpList = new ArrayList();

                foreach (Item item in ItemList)
                {
                    pm.JumpList.Add(item);
                    Logger.Log(LogType.Item, item);
                }
            }

            arg.Mobile.SendMessage("Done. {0} items located.", ItemList.Count);
            Logger.Finish();
        }

        public static List<Item> FindItems(int x, int y, int tilerange)
        {
            List<Item> ItemList = new();
            foreach (Item item in World.Items.Values)
            {
                if (item is Item it)
                {
                    int spX = it.Location.X - x;
                    int spY = it.Location.Y - y;
                    if ((tilerange == 0) || ((sqr(it.Location.X - x) <= sqr(tilerange) && sqr(it.Location.Y - y) <= sqr(tilerange))))
                        ItemList.Add(it);
                }
            }

            return (ItemList);
        }

        private static int sqr(int num)
        {
            return ((num * num));
        }
    }
}