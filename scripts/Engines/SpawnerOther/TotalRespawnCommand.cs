/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

using Server.Diagnostics;
using Server.Engines.ChampionSpawn;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Commands
{
    public class TotalRespawnCommand
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("TotalRespawn", AccessLevel.Administrator, new CommandEventHandler(TotalRespawn_OnCommand));
        }

        [Usage("TotalRespawn [map]")]
        [Description("Forces a total respawn of all eligible spawners and fillable containers. Optionally limited to a map.")]
        public static void TotalRespawn_OnCommand(CommandEventArgs e)
        {
            Map? map = null;

            if (e != null && e.Length > 0)
            {
                map = Map.Parse(e.GetString(0));

                if (map == null)
                {
                    e.Mobile?.SendMessage("Unknown map: {0}", e.GetString(0));
                    return;
                }
            }

            TotalRespawn(e?.Mobile, map);
        }

        public static TotalRespawnResult TotalRespawn(Mobile? m = null, Map? map = null)
        {
            DateTime begin = DateTime.UtcNow;

            string scope = map == null ? "world" : map.Name;

            World.Broadcast(0x35, true, $"The {Core.Server} {scope} is respawning, please wait...");
            Console.WriteLine("TotalRespawn: starting ({0})...", scope);

            try
            {
                var result = TotalRespawnWorker(m, map);

                DateTime end = DateTime.UtcNow;
                TimeSpan timeTaken = end - begin;

                World.Broadcast(0x35, true,
                    $"{Core.Server} {scope} spawn complete. The entire process took {timeTaken.TotalSeconds:00.00} seconds.");

                string message =
                    $"Total respawn of {result.TotalCount} objects " +
                    $"({result.SpawnerCount} spawners, {result.FillableCount} fillables, " +
                    $"{result.ChampCount} champs, {result.OverlandCount} overland deleted) " +
                    $"on {scope} took {timeTaken.TotalSeconds:00.00} seconds.";

                Console.WriteLine("TotalRespawn: " + message);

                if (m != null)
                    m.SendMessage(message);

                return result;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);

                if (m != null)
                    m.SendMessage(0x25, "** Total Respawn Failed ** — check logs.");
                else
                    Console.WriteLine("Total Respawn FAILED: " + ex.Message);

                return new TotalRespawnResult();
            }
        }

        public static TotalRespawnResult TotalRespawnWorker(Mobile? reporter = null, Map? map = null)
        {
            var result = new TotalRespawnResult();

            NetState.FlushAll();
            NetState.Pause();

            try
            {
                var allItems = World.Items.Values.ToArray();
                var allMobiles = World.Mobiles.Values.ToArray();

                var spawners = new List<Spawner>();
                var fillables = new List<FillableContainer>();
                var champs = new List<ChampEngine>();
                var overlands = new List<BaseOverland>();

                Console.WriteLine("TotalRespawn: collecting systems...");

                foreach (Item item in allItems)
                {
                    if (item?.Deleted != false)
                        continue;

                    if (map != null && item.Map != map)
                        continue;

                    if (item is Spawner s)
                    {
                        if (s.Running)
                            spawners.Add(s);
                    }
                    else if (item is FillableContainer fc)
                    {
                        fillables.Add(fc);
                    }
                    else if (item is ChampEngine ce)
                    {
                        champs.Add(ce);
                    }
                }

                foreach (Mobile mob in allMobiles)
                {
                    if (mob?.Deleted != false)
                        continue;

                    if (map != null && mob.Map != map)
                        continue;

                    if (mob is BaseOverland bo)
                        overlands.Add(bo);
                }

                Console.WriteLine("TotalRespawn: deleting {0} overland encounter(s)...", overlands.Count);

                foreach (BaseOverland bo in overlands)
                {
                    if (!bo.Deleted)
                    {
                        bo.Delete();
                        result.OverlandCount++;
                    }
                }

                Console.WriteLine("TotalRespawn: {0} overland encounter(s) deleted.", result.OverlandCount);

                Console.WriteLine("TotalRespawn: respawning {0} spawner(s)...", spawners.Count);

                foreach (Spawner sp in spawners)
                {
                    if (sp.Running && !AllGuardsInIntStorage(sp))
                    {
                        sp.RemoveObjects();
                        sp.Respawn();
                        result.SpawnerCount++;
                    }
                }

                Console.WriteLine("TotalRespawn: {0} spawner(s) respawned.", result.SpawnerCount);

                Console.WriteLine("TotalRespawn: respawning {0} fillable container(s)...", fillables.Count);

                foreach (FillableContainer fc in fillables)
                {
                    fc.TotalRespawn = true;
                    result.FillableCount++;
                }

                Console.WriteLine("TotalRespawn: {0} fillable container(s) respawned.", result.FillableCount);

                Console.WriteLine("TotalRespawn: clearing {0} champ engine(s)...", champs.Count);

                foreach (ChampEngine ce in champs)
                {
                    if (ce.Running)
                    {
                        ce.ClearMonsters = true;
                        result.ChampCount++;
                    }
                }

                Console.WriteLine("TotalRespawn: {0} champ engine(s) cleared.", result.ChampCount);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));

                if (reporter != null)
                    reporter.SendMessage("TotalRespawn failed: {0}", ex.Message);
            }
            finally
            {
                NetState.Resume();
            }

            return result;
        }
        private static bool AllGuardsInIntStorage(Spawner spawner)
        {
            foreach (object o in spawner.Objects)
            {
                if (o is not BaseGuard bg || !bg.IsIntMapStorage)
                    return false;
            }

            return true;
        }
        public class TotalRespawnResult
        {
            public int SpawnerCount { get; set; }
            public int FillableCount { get; set; }
            public int ChampCount { get; set; }
            public int OverlandCount { get; set; }

            public int TotalCount => SpawnerCount + FillableCount + ChampCount + OverlandCount;
        }
    }
}