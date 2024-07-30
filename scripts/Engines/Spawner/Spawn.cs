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

/* Scripts\Engines\Spawner\Spawn.cs
 * CHANGELOG
 *  9/3/22, Adam
 *  9/3/22 Created by Adam
 *  Create spawners from a database of spawner descriptions stored in:
 *  \Data\Spawners\Angel Island\Spawns\felucca\
 *  For example: \Data\Spawners\Angel Island\Spawns\felucca\Graveyards.map
 */


using Server.Diagnostics;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Server.Engines
{
    public static class Spawn
    {
        private static List<object> recorder = new List<object>();
        private static List<Spawner> StackedSpawnerTable = new List<Spawner>();
        public static void Initialize()
        {
            CommandSystem.Register("Spawn", AccessLevel.Administrator, new CommandEventHandler(Spawn_OnCommand));
        }

        [Usage("Spawn")]
        [Description("Create spawners from a database of spawner descriptions stored (.map) files.")]
        private static void Spawn_OnCommand(CommandEventArgs args)
        {
            recorder.Clear();
            (args.Mobile as PlayerMobile).JumpList = new ArrayList();

            if (!(Core.RuleSets.StandardShardRules()))
            {
                args.Mobile.SendMessage("This command can only be used on a standard.");
                return;
            }
            else if (Environment.MachineName.Length.ToString().ToLower() == "LUKES-AISERVER".ToLower())
            {
                args.Mobile.SendMessage("Because the resultant patch file needs to be checked into SVN, this must be run on a developer machine.");
                return;
            }

            string filename = args.ArgString;
            string fullFileName = Path.Combine(Core.DataDirectory, "Spawners/Angel Island/Spawns/felucca", filename);
            string outputName = Path.Combine(Core.DataDirectory, String.Format("{0} spawner patch {1}.cfg", args.ArgString, DateTime.UtcNow.ToString("MM-dd-yyyy")));
            UInt32 patchID = (UInt32)Utility.GetStableHashCode(outputName, version: 1);
            if (File.Exists(fullFileName) == false)
            {
                args.Mobile.SendMessage("{0} not found.", filename);
                return;
            }

            // empty the "'shard' spawner list patch.cfg"
            File.WriteAllText(outputName, String.Empty);

            // keep track of overlapping areas
            List<Serial> areaOverlap = new();

            // get a cache of spawners
            List<Spawner> spawnerCache = new();
            foreach (Item item in World.Items.Values)
            {
                if (item is not Spawner spawner) continue;
                if (spawner is EventSpawner) continue;
                if (spawner.Map != Map.Felucca) continue;
                // ignore the spawners that we have already processed.
                if (spawner.PatchID == patchID)
                    spawnerCache.Add(spawner);
            }

            // first, remove all spawners with this patch id
            if (spawnerCache.Count > 0)
            {
                Utility.ConsoleWriteLine("Located {0} spawners with this patch id. Removing...", ConsoleColor.Magenta, spawnerCache.Count);
                foreach (Spawner spawner in spawnerCache)
                    spawner.Delete();
                Utility.ConsoleWriteLine("{0} spawners deleted.", ConsoleColor.Magenta, spawnerCache.Count);
            }

            Utility.ConsoleWriteLine("Spawning from {0}...", ConsoleColor.Magenta, args.ArgString);

            // run
            ProcessFile(args.Mobile, fullFileName, patchID, outputName);

            // Nerun stacks spawners. It's hard for GMs to manage, but I understand his reasoning.
            //  We'll drop one of these stackedSpawnerConsole's on the stack - which lets you pick the spawner and edit it.
            foreach (Spawner spawner in StackedSpawnerTable)
            {
                if (!Patcher.AlreadySpawned(spawner.X, spawner.Y, spawner.Z, spawner.Map))
                {
                    Items.StackedSpawnerConsole stackedSpawnerConsole = new Items.StackedSpawnerConsole();
                    stackedSpawnerConsole.Movable = false;
                    stackedSpawnerConsole.Visible = false;
                    stackedSpawnerConsole.MoveToWorld(new Point3D(spawner.X, spawner.Y, spawner.Z + 1), spawner.Map);
                }
            }

            Utility.ConsoleWriteLine("Spawn from {0} complete.", ConsoleColor.Magenta, filename);

            // cleanup
            (args.Mobile as PlayerMobile).JumpList.AddRange(recorder);
            recorder.Clear();
            StackedSpawnerTable.Clear();
        }

        private static void ProcessFile(Mobile from, string filename, UInt32 patchID, string outputName)
        {
            try
            {
                // compile the entire map file into a string of specially formated tokens
                string[] tokens = SpawnerManager.CompileMap(filename);

                // build a list of area starts/ends
                List<int> areas = SpawnerManager.AreaParser(tokens);

                // record stacked spawners
                List<Spawner> StackedSpawnerTable = new();
                // keep track of overlapping areas
                List<Serial> areaOverlap = new();
                // spawner cache
                List<Spawner> spawnerCache = new();
                // newly placed spawners
                List<Spawner> spawnersPlaced = new();

                // process areas
                SpawnerManager.ProcessAreas(tokens, areas, spawnerCache, areaOverlap, StackedSpawnerTable, spawnersPlaced, outputName);

                // Nerun(and us now) stacks spawners. It's hard for GMs to manage, but I understand his reasoning.
                //  We'll drop one of these stackedSpawnerConsole's on the stack - which lets you pick the spawner and edit it.
                foreach (Spawner spawner in StackedSpawnerTable)
                {
                    Items.StackedSpawnerConsole stackedSpawnerConsole = new Items.StackedSpawnerConsole();
                    stackedSpawnerConsole.Movable = false;
                    stackedSpawnerConsole.Visible = false;
                    stackedSpawnerConsole.MoveToWorld(new Point3D(spawner.X, spawner.Y, spawner.Z + 1), spawner.Map);
                }
                // give this spawner a patch id
                //  The patch id is used for removing previously placed spawners with the same patchID before adding the new spawner with the same patchID
                foreach (Spawner spawner in spawnersPlaced)
                    spawner.PatchID = patchID;

                // let the developer know we're still working
                from.SendMessage(string.Format("Finished processing {0} areas in file {1}", areas.Count, Path.GetFileName(filename)));
                Utility.ConsoleWriteLine(string.Format("Finished processing {0} areas in file {1}", areas.Count, Path.GetFileName(filename)), ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Spawn: {0}", ex);
                LogHelper.LogException(ex);
            }
        }

    }
}