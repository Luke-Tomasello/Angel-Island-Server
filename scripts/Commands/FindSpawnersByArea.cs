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

/* scripts\Commands\FindSpawnersByArea.cs
 * ChangeLog
 *  5/30/2023
 *		Initial creation - designed to retrieve list of one or more spawners which map to the 'areas' for UOSpawnMap.jpg
 *		Each area is made up of 1 or more subareas.
 *		https://uo.stratics.com/hunters/spawn/spawnmap.zip
*/
using Server.Diagnostics;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Commands
{
    public class FindSpawnersByArea
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindSpawnersByArea", AccessLevel.GameMaster, new CommandEventHandler(FindSpawnersByArea_OnCommand));
        }

        [Usage("FindSpawnersByArea <area number>")]
        [Description("Finds locations of all spawners for this area.")]
        private static void FindSpawnersByArea_OnCommand(CommandEventArgs arg)
        {
            Mobile from = arg.Mobile;

            int AreaNumber = 0;
            if (int.TryParse(arg.GetString(0), out AreaNumber) == false || AreaNumber <= 0 || AreaNumber > 54)
            {
                arg.Mobile.SendMessage("Usage: FindSpawnersByArea <area number>");
                return;
            }

            // Perform search and retrieve list of spawner matches
            LogHelper Logger = new LogHelper("FindSpawnersByArea.log", from, true);
            List<Spawner> SpawnerList = FindSpawners(AreaNumber, from.Map);

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

        public static List<Spawner> FindSpawners(int AreaNumber, Map map)
        {
            List<Spawner> SpawnerList = new();
            foreach (Item item in World.Items.Values)
            {
                if (item is Spawner sp)
                {
                    int area = 0;
                    if (sp.Source != null)
                    {
                        string[] tokens = sp.Source.Split(new char[] { ' ', ',' });
                        if (tokens.Length >= 2 && "area".Equals(tokens[0], StringComparison.OrdinalIgnoreCase))
                            if (int.TryParse(tokens[1], out area) && area == AreaNumber)
                                SpawnerList.Add(sp);
                    }
                }
            }

            return (SpawnerList);
        }
    }
}