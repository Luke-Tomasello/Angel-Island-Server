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

/* Scripts/Engines/Spawner/TotalRespawnCommand.cs
 * ChangeLog
 *  8/24/22, Adam
 *      Call new Spawner.ShouldShardEnable(s) which is now used in a couple different places to determine what spawners should be respawned on which shards.
 *  8/23/22, Adam
 *      Only respawn the spawners for the configured world
 *	12/30/04 - Pixie
 *		Changed to Admin only; Added broadcast message; Only spawn Running spawners
 *	12/29/04 - Pixie
 *		Initial Version!
 */

using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Commands
{
    public class TotalRespondCommand
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("TotalRespawn", AccessLevel.Administrator, new CommandEventHandler(TotalRespawn_OnCommand));
        }

        public static void TotalRespawn_OnCommand(CommandEventArgs e)
        {
            DateTime begin = DateTime.UtcNow;
            int count = 0;
            World.Broadcast(0x35, true, string.Format("The {0} world is respawning, please wait.", Core.Server));

            List<Spawner> spawners = new();
            List<FillableContainer> fillableContainerList = new();
            foreach (Item item in World.Items.Values)
            {
                if (item is Spawner s && s.Deleted == false)
                {
                    // based on our spawner/shard config, should this spawner be respawned?
                    if (Spawner.ShouldShardEnable(s) || s is RaresSpawner)
                        spawners.Add(s);
                }
                else if (item is FillableContainer fc && fc.Deleted == false)
                    fillableContainerList.Add(fc);
            }

            foreach (Spawner sp in spawners)
            {
                if (sp.Running)
                {
                    count++;
                    sp.RemoveObjects();
                    sp.Respawn();
                }
            }

            foreach (var fc in fillableContainerList)
            {
                count++;
                fc.TotalRespawn = true;
            }

            DateTime end = DateTime.UtcNow;

            TimeSpan timeTaken = end - begin;
            World.Broadcast(0x35, true, "{1} spawn complete. The entire process took {0:00.00} seconds.", timeTaken.TotalSeconds, Core.Server);
            e.Mobile.SendMessage("Total Respawn of {0} spawners took {1:00.00} seconds", count, timeTaken.TotalSeconds);
        }
    }
}