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

/* Scripts/Engines/OverlandSpawner/OverlandSpawner.cs
 * Changelog:
 *	04/23/08, plasma
 *		Change to use new SpawnerCache.cs
 *	9/6/06, Adam
 *		When we load the spawner cache, make sure the spawner has something spawned on it.
 *	9/1/06, Adam
 *		Export the list of spawners via GetSpawnerList()
 *	1/15/06, Adam
 *		Make GetRandomSpawner() public
 *	1/14/06, Adam
 *		1. Add a new LoadSpawnerCache method (called on the heartbeat)t. This method
 *			reloads the cache of 'target' spawners for the Overland system
 *		2. use the spawners homerange except when it is a special 'guard post' spawner (HomeRange == 0)
 *	1/13/06, Adam
 *		Add new 'bad regions' and 'running == false' spawners to the exclusion list
 *	1/11/06, Adam
 *		Working version of the Overland Spawn System
 *	1/10/06, Adam
 *		First time checkin
 */

using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;	// for Spawner. Shouldn't a Spawner be in the 'item' namespace?
using System;

namespace Server.Engines.OverlandSpawner
{
    public class OverlandSpawner
    {
        public Mobile SpawnAt(Point2D location, Mobile mob)
        {
            Map map = Map.Felucca;
            int z = map.GetAverageZ(location.X, location.Y);

            try
            {
                BaseOverland bo = null;
                BaseCreature bc = mob as BaseCreature;
                // we will make it so this is passed in
                if (mob is BaseOverland)
                    bo = mob as BaseOverland;

                //	wander around 
                bc.RangeHome = 20;

                // could be useful ... but not today
                // c.CurrentWayPoint = spawner.WayPoint;
                // c.NavDestination = spawner.NavPoint;

                bc.Home = new Point3D(location.X, location.Y, z);
                bc.Spawner = null;

                //if we have a navdestination as soon as we spawn start on it
                //if (bc.NavDestination != NavDestinations.None)
                if (!string.IsNullOrEmpty(bc.NavDestination))
                    bc.AIObject.Think();

                /////////////////////////////
                // move it to the world
                bc.MoveToWorld(bc.Home, map);

                // Okay, indicate that this overland mob should be announced on the Town Crier
                // This must be after we are moved off the internal map because the accounce code
                //	supplies 'world location' information which would be wrong if we were still on the internal map
                if (bo != null)
                    bo.Announce = true;

                return bc;
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Server.Engines.OverlandSpawner : Exception {0}", e);
            }

            return null;
        }

        public Mobile OverlandSpawnerSpawn(Mobile mob)
        {
            try
            {   // we have many 'tested' treasure map locations, use them
                Mobile m = SpawnAt(TreasureMap.GetRandomTreasureMapLocation(map: Map.Felucca), mob);
                return m;
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Server.Engines.OverlandSpawner : Exception {0}", e);
            }

            return null;
        }

    }
}