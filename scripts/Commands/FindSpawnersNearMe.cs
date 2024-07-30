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

/* scripts\Commands\FindSpawnersNearMe.cs
 * ChangeLog
 *  5/29/2023
 *		Initial creation - designed to retrieve list of spawners near GM
 *		distance passed as parameter.
*/
using Server.Diagnostics;
using Server.Mobiles;
using System.Collections;
using System.Collections.Generic;

namespace Server.Commands
{
    public class FindSpawnersNearMe
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindSpawnersNearMe", AccessLevel.GameMaster, new CommandEventHandler(FindSpawnersNearMe_OnCommand));
        }

        [Usage("FindSpawnersNearMe <tile range>")]
        [Description("Finds locations of all spawners within distance.")]
        private static void FindSpawnersNearMe_OnCommand(CommandEventArgs arg)
        {
            Mobile from = arg.Mobile;

            int X = from.Location.X;
            int Y = from.Location.Y;

            int TileRange = arg.GetInt32(0);
            if (int.TryParse(arg.GetString(0), out TileRange) == false)
            {
                arg.Mobile.SendMessage("Usage: FindSpawnersNearMe <tile range>");
                return;
            }

            // Perform search and retrieve list of spawner matches
            LogHelper Logger = new LogHelper("findSpawnerNearMe.log", from, true);
            List<Spawner> SpawnerList = FindSpawners(X, Y, TileRange);

            if (SpawnerList.Count > 0)
            {
                PlayerMobile pm = arg.Mobile as PlayerMobile;

                // Have results so sort, loop and display message for each match
                SpawnerList.Sort();

                // reset jump table
                pm.JumpIndex = 0;
                pm.JumpList = new ArrayList();

                foreach (Spawner spawner in SpawnerList)
                {
                    pm.JumpList.Add(spawner);
                    Logger.Log(LogType.Item, spawner);
                }
            }

            arg.Mobile.SendMessage("Done. {0} spawners located.", SpawnerList.Count);
            Logger.Finish();
        }

        public static List<Spawner> FindSpawners(int x, int y, int tilerange)
        {
            List<Spawner> SpawnerList = new();
            foreach (Item item in World.Items.Values)
            {
                if (item is Spawner sp)
                {
                    int spX = sp.Location.X - x;
                    int spY = sp.Location.Y - y;
                    if ((tilerange == 0) || ((sqr(sp.Location.X - x) <= sqr(tilerange) && sqr(sp.Location.Y - y) <= sqr(tilerange))))
                        SpawnerList.Add(sp);
                }
            }

            return (SpawnerList);
        }

        private static int sqr(int num)
        {
            return ((num * num));
        }
    }
}