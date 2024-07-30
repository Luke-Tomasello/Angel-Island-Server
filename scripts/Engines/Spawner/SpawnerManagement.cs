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

/* Scripts\Engines\Spawner\SpawnerManagement.cs
 * CHANGELOG
 *  8/30/22, Adam
 *      The [ClassicRespawn command will 
 *          1. Respawn the world based upon Nerun's Distro specification (map files) https://github.com/nerun/runuo-nerun-distro
 *          2. disable all other spawners and set their attributes accordingly to reflect their disposition, for example "AISpecial"
 *          3. Generate a 'definitive spawner list patch.cfg' file which hold these updated spawner attributes.
 *              these updated spawner attributes will be read by all shards on next server-up, and the patch will be applied.
 *              Note: the patching based on this file, isn't to disable anything, but to simply update the spawners categorization. I.e., "AISpecial"
 *  8/27/22,Adam
 *  8/27/22 Created by Adam
 */

using Server.Diagnostics;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Server.Mobiles.Spawner;

namespace Server.Engines
{
    public static class SpawnerManager
    {
        private static List<object> recorder = new List<object>();

        public static void Initialize()
        {   // respawn the shard using Nerun's Distro
            CommandSystem.Register("ClassicRespawn", AccessLevel.Owner, new CommandEventHandler(ClassicRespawn_OnCommand));
            // allow post patching recategorization
            CommandSystem.Register("AISpecial", AccessLevel.Administrator, new CommandEventHandler(AISpecial_OnCommand));
        }

        [Usage("ClassicRespawn")]
        [Description("Removes old AI style world spawn, and replaces with Nerun's Distro spawn map.")]
        private static void ClassicRespawn_OnCommand(CommandEventArgs args)
        {
            //args.Mobile.SendMessage("Disabled. ClassicRespawn is now called by the patcher.");
            //return;

            recorder.Clear();
            (args.Mobile as PlayerMobile).JumpList = new ArrayList();

            // record stacked spawners
            List<Spawner> StackedSpawnerTable = new();

            if (!(Core.RuleSets.SiegeStyleRules()))
            {
                args.Mobile.SendMessage("This command can only be used on Siege or Mortalis.");
                return;
            }
            else if (Environment.MachineName.Length.ToString().ToLower() == "LUKES-AISERVER".ToLower())
            {
                args.Mobile.SendMessage("Because the resultant patch file needs to be checked into SVN, this must be run on a developer machine.");
                return;
            }

            // empty the "definitive spawner list patch.cfg"
            File.WriteAllText(Path.Combine(Core.LogsDirectory, "definitive spawner list patch.cfg"), String.Empty);

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
                if (spawner.GetFlag(SpawnerAttribs.ModeAI) || spawner.GetFlag(SpawnerAttribs.ModeNeruns) || spawner.Replaced == true || spawner.Kept == true)
                    continue;
                spawnerCache.Add(spawner);
            }

            if (spawnerCache.Count == 0)
            {
                Utility.ConsoleWriteLine("Nothing to do.", ConsoleColor.Magenta);
                return;
            }

            Utility.ConsoleWriteLine("Converting all core spawn to Nerun's Distro spawn...", ConsoleColor.Magenta);

            // All spawners designated as 'AllShards' in the server-up Patching, will now be prepared for
            //  merging with the new spawners from Nerun's Distro
            PrepExistingAllShardsSpawners(spawnerCache);

            /// Main Processing Loop ///
            /// For each file in the maps directory, parse them, create spawners and install them.
            /// Additionally, any existing spawners will be decommissioned unless they have an 'AllShards' designation
            string[] files = Directory.GetFiles(Path.Combine(Core.DataDirectory, "Spawners/Nerun's Distro/Spawns/felucca"));
            foreach (string file in files)
                ProcessFile(args.Mobile, file, spawnerCache, areaOverlap, StackedSpawnerTable, "spawner add list.cfg");

            // Nerun stacks spawners. It's hard for GMs to manage, but I understand his reasoning.
            //  We'll drop one of these stackedSpawnerConsole's on the stack - which lets you pick the spawner and edit it.
            foreach (Spawner spawner in StackedSpawnerTable)
            {
                Items.StackedSpawnerConsole stackedSpawnerConsole = new Items.StackedSpawnerConsole();
                stackedSpawnerConsole.Movable = false;
                stackedSpawnerConsole.Visible = false;
                stackedSpawnerConsole.MoveToWorld(new Point3D(spawner.X, spawner.Y, spawner.Z + 1), spawner.Map);
            }

            Utility.ConsoleWriteLine("Conversion to Nerun's Distro spawn complete.", ConsoleColor.Magenta);
            CoreAI.SetDynamicPatch(CoreAI.PatchIndex.HasPatchedInClasicRespawn); // patch complete

            // cleanup
            (args.Mobile as PlayerMobile).JumpList.AddRange(recorder);
            recorder.Clear();
            StackedSpawnerTable.Clear();
        }
        public static void ClassicRespawn()
        {
            recorder.Clear();

            // record stacked spawners
            List<Spawner> StackedSpawnerTable = new();

            // empty the "definitive spawner list patch.cfg"
            File.WriteAllText(Path.Combine(Core.LogsDirectory, "definitive spawner list patch.cfg"), String.Empty);

            // keep track of overlapping areas
            List<Serial> areaOverlap = new();

            // get a cache of spawners
            List<Spawner> spawnerCache = new();
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted) continue;
                if (item is not Spawner spawner) continue;
                if (spawner is EventSpawner) continue;
                if (spawner.Map != Map.Felucca) continue;
                // ignore the spawners that we have already processed.
                if (spawner.GetFlag(SpawnerAttribs.ModeAI) || spawner.GetFlag(SpawnerAttribs.ModeNeruns) || spawner.Replaced == true || spawner.Kept == true)
                    continue;
                spawnerCache.Add(spawner);
            }

            if (spawnerCache.Count == 0)
            {
                Utility.ConsoleWriteLine("Nothing to do. There are either no spawners, or all have been updated already.", ConsoleColor.Magenta);
                return;
            }

            // All spawners designated as 'AllShards' in the server-up Patching, will now be prepared for
            //  merging with the new spawners from Nerun's Distro
            PrepExistingAllShardsSpawners(spawnerCache);

            /// Main Processing Loop ///
            /// For each file in the maps directory, parse them, create spawners and install them.
            /// Additionally, any existing spawners will be decommissioned unless they have an 'AllShards' designation
            string[] files = Directory.GetFiles(Path.Combine(Core.DataDirectory, "Spawners/Nerun's Distro/Spawns/felucca"));
            foreach (string file in files)
                ProcessFile(null, file, spawnerCache, areaOverlap, StackedSpawnerTable, "spawner add list.cfg");

            // Nerun stacks spawners. It's hard for GMs to manage, but I understand his reasoning.
            //  We'll drop one of these stackedSpawnerConsole's on the stack - which lets you pick the spawner and edit it.
            foreach (Spawner spawner in StackedSpawnerTable)
            {
                Items.StackedSpawnerConsole stackedSpawnerConsole = new Items.StackedSpawnerConsole();
                stackedSpawnerConsole.Movable = false;
                stackedSpawnerConsole.Visible = false;
                stackedSpawnerConsole.MoveToWorld(new Point3D(spawner.X, spawner.Y, spawner.Z + 1), spawner.Map);
            }

            // cleanup
            recorder.Clear();
            StackedSpawnerTable.Clear();
        }
        private static void ProcessFile(Mobile from, string filename, List<Spawner> spawnerCache, List<Serial> areaOverlap, List<Spawner> StackedSpawnerTable, string outputFile)
        {
            try
            {   // compile the entire map file into a string of specially formated tokens
                string[] tokens = CompileMap(filename);

                // build a list of area starts/ends
                List<int> areas = AreaParser(tokens);

                // process areas
                ProcessAreas(tokens, areas, spawnerCache, areaOverlap, StackedSpawnerTable, new List<Spawner>(), outputFile);

                // let the developer know we're still working
                if (from != null)
                    from.SendMessage(string.Format("Finished processing {0} areas in file {1}", areas.Count, Path.GetFileName(filename)));
                Utility.ConsoleWriteLine(string.Format("Finished processing {0} areas in file {1}", areas.Count, Path.GetFileName(filename)), ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ClassicRespawn: {0}", ex);
                LogHelper.LogException(ex);
            }
        }
        public static void ProcessAreas(string[] tokens, List<int> areas, List<Spawner> spawnerCache, List<Serial> areaOverlap, List<Spawner> StackedSpawnerTable, List<Spawner> SpawnersPlaced, string outputName)
        {
            // these are the module (.map) overrides. they apply to the whole file until another override is encountered.
            List<string> ModuleOverrides = new List<string>();

            // loop over the areas, spawning mobiles at each
            for (int area = 0; area < areas.Count; area++)
            {
                List<string> tmp;

                if (area + 1 == areas.Count)
                    tmp = new List<string>(tokens.Skip(areas[area]));                                           // last/only area
                else
                    tmp = new List<string>(tokens.Skip(areas[area]).Take(areas[area + 1] - areas[area]));       // area N to area N+1

                tmp.RemoveAll(delegate (String v) { return v == "-break-"; });                                  // remove training breaks markers

                var AreaObjects = AreaObjectsToArray(tmp);                                                      // get the objects to spawn: reagents, Mobiles, etc.

                // we need to carry forward the overrides for the whole module (.map) until something changes.
                //  when something changes, we update only THAT change and carry forward all else
                List<string> overrides = new List<string>(ModuleOverridesToArray(tmp));
                ModuleOverrides = MergeOverrides(ModuleOverrides, overrides);

                ////////////////

                foreach (Spawner spawner in spawnerCache)                                                       // decommission old spawners
                    if (!areaOverlap.Contains(spawner.Serial))                                                  // because areas can overlap, we don't reprocess these spawners
                    {
                        areaOverlap.Add(spawner.Serial);                                                        // remember this spawner
                        if (spawner.Shard == Spawner.ShardConfig.AllShards)
                        {
                            spawner.Kept = true;                                                                // flag as 1) having been kept, 2) don't reprocess
                            continue;                                                                           // do nothing, we'll keep these
                        }
                        else
                        {
                            spawner.Replaced = true;                                                            // flag as 1) having been replaced, 2) don't reprocess
                            ClobberOldSpawner(spawner);                                                         // deactivate, color code, and tag as handled
                            recorder.Add(spawner);                                                              // record the decommissioning of this spawner
                        }
                    }
                // add new spawners
                InstantiateNewSpawners(AreaObjects, ModuleOverrides.ToArray(), StackedSpawnerTable, SpawnersPlaced, outputName);
            }
        }
        /// <summary>
        /// Merge the new overrides with the old
        /// </summary>
        /// <param name="ModuleOverrides"></param>
        /// <param name="ProposedOverrides"></param>
        /// <returns></returns>
        private static List<string> MergeOverrides(List<string> ModuleOverrides, List<string> ProposedOverrides)
        {
            NormalizeStrings(ModuleOverrides);                          // remove redundant whitespace
            NormalizeStrings(ProposedOverrides);                        // remove redundant whitespace
            List<string> ModuleOverridesCopy = new(ModuleOverrides);    // make a copy so we can modify the original on-the-fly
            if (ModuleOverrides.Count == 0)                             // if no overrides, accept all proposed overrides
            {
                ModuleOverrides.AddRange(ProposedOverrides);
                return ModuleOverrides;
            }
            else
                foreach (string s in ProposedOverrides)
                {                                                       // update our copy (ModuleOverridesCopy) with our proposed changes 
                    string[] pParts = s.Split(' ');
                    var match = ModuleOverrides.Where(stringToCheck => stringToCheck.Contains(pParts[0]));
                    foreach (string found in match)
                    {
                        ModuleOverridesCopy.RemoveAll(x => x.Contains(pParts[0]));
                        ModuleOverridesCopy.Add(s);
                    }

                }

            // return the new override table
            return ModuleOverridesCopy;

        }
        private static void NormalizeStrings(List<string> list)
        {
            for (int ix = 0; ix < list.Count; ix++)
            {
                string sentence = list[ix];
                RegexOptions options = RegexOptions.None;
                Regex regex = new Regex("[ ]{2,}", options);
                list[ix] = regex.Replace(sentence, " ");
            }
        }
        private static string[] ModuleOverridesToArray(List<string> tmp)
        {
            List<string> overrides = new();
            foreach (string line in tmp)
                if (line.ToLower().StartsWith("override"))
                    overrides.Add(line.ToLower());
            return overrides.ToArray();
        }
        private static string[] AreaObjectsToArray(List<string> tmp)
        {
            List<string> objects = new();
            foreach (string line in tmp)
                if (!line.ToLower().StartsWith("override"))
                    objects.Add(line);
            return objects.ToArray();
        }
        private static void PrepExistingAllShardsSpawners(List<Spawner> spawnerCache)
        {
            foreach (Spawner spawner in spawnerCache)
            {
                // outfit our 'AllShards' spawners
                if (spawner.Shard == Spawner.ShardConfig.AllShards)
                {
                    UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false, uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);
                    recorder.Add(spawner);
                }
            }
        }
        /// <summary>
        /// Update the spawners properties, and log it to a file which is checked into source control.
        /// Then on server up, our patcher will read this file and apply all patches named in this file.
        /// </summary>
        /// <param name="spawner"></param>
        /// <param name="coreSpawn"></param>
        /// <param name="uOSpawnMap"></param>
        /// <param name="shardConfig"></param>
        public static void UpdateSpawnerDesignationaAndLog(Spawner spawner, bool coreSpawn, string uOSpawnMap, ShardConfig shardConfig)
        {
            // some spawners are designated as AllShards by our patcher. we should therefore leave it alone.
            //  examples of these "AllShards" spawners are spawners in areas like Angel Island Prison, kin strongholds, etc.
            // log the change to this spawner
            if (Core.RuleSets.AngelIslandRules() == false)
            {   // don't mess with the graphics on AI
                spawner.ItemID = 0x14f0;    // deed graphic
                spawner.Visible = true;     // we want to see the color
                spawner.Hue = (int)SpawnerGraphics.AllShardsRunningHue;
            }
            spawner.CoreSpawn = coreSpawn;  // not official UO spawn
            spawner.Source = uOSpawnMap;
            spawner.Shard = shardConfig;

            string pathName = Path.Combine(Core.LogsDirectory, "definitive spawner list patch.cfg");
            // remove the existing record if it exists
            if (File.Exists(pathName))
            {
                List<Tuple<int, bool, string, string>> records = new();
                foreach (string line in File.ReadAllLines(pathName))
                {
                    string[] toks = line.Split(new char[] { ' ', ':' });
                    Serial s;
                    s = int.Parse(toks[0]);
                    if (spawner.Serial == s)
                        // eliminate this entry as we will be updating it
                        continue;
                    records.Add(new Tuple<int, bool, string, string>(int.Parse(toks[0]), bool.Parse(toks[1]), toks[2], toks[3]));
                }
                List<string> lines = new();
                foreach (var record in records)
                    lines.Add(String.Format("{0}:{1}:{2}:{3}", record.Item1, record.Item2, record.Item3, record.Item4));

                File.WriteAllLines(pathName, lines);
            }
            // now write the updated record
            LogHelper logger = new LogHelper(pathName, false, true, true);
            logger.Log(LogType.Text, string.Format("{0}:{1}:{2}:{3}", spawner.Serial.Value, spawner.CoreSpawn, spawner.Source, spawner.Shard));
            logger.Finish();
        }
        public static string[] CompileMap(string mapName)
        {
            List<string> compiledTokens = new();
            if (!File.Exists(mapName))
                return compiledTokens.ToArray();

            foreach (string line in System.IO.File.ReadLines(mapName))
            {
                if (OnIgnoreList(line)) continue;                                           // some vendors, like hiresailor, boatpainter, are not of our era. We also ignore treasure chests

                if (line.StartsWith("override"))                                            // save the override meta data
                    compiledTokens.Add(line);

                string[] lineToks = line.Split(new char[] { '|' });                         // parse on '|'
                if (lineToks.Length == 22)                                                  // always 22 fields
                    CompileLine(lineToks, compiledTokens);

                else
                {   // add 'level break' marker
                    compiledTokens.AddRange(new string[] { "-break-" });
                }
            }

            compiledTokens = PackTokenList(compiledTokens);

            return compiledTokens.ToArray();
        }
        public static void CompileLine(string[] lineToks, List<string> compiledTokens)
        {
            for (int ix = 0; ix < 7; ix++)
                if (string.IsNullOrEmpty(lineToks[ix]))                             // [1-6] are creatures on the spawner. If it's not specified, set to "-null-"
                    lineToks[ix] = "-null-";                                        // 
            lineToks[0] = string.Empty;                                             // [0] is throw away
            lineToks = lineToks.Where(x => !string.IsNullOrEmpty(x)).ToArray();     // pack the array
            compiledTokens.AddRange(lineToks);
        }
        private static List<string> PackTokenList(List<string> compiledTokens)
        {
            List<string> newTokens = new List<string>();
            bool packing = false;
            for (int ix = 0; ix < compiledTokens.Count; ix++)
            {
                if (compiledTokens[ix] == "-break-" && packing == true)
                    newTokens.Add(compiledTokens[ix] = null);
                else if (compiledTokens[ix] == "-break-" && packing == false)
                    newTokens.Add(compiledTokens[ix]);
                else if (compiledTokens[ix].StartsWith("override"))
                {
                    newTokens.Add(compiledTokens[ix]);
                    packing = true;
                }
                else
                {
                    newTokens.Add(compiledTokens[ix]);
                    packing = false;
                }
            }

            newTokens.RemoveAll(item => item == null);

            return newTokens;
        }
        public static List<int> AreaParser(string[] tokens)
        {
            // build a list of level starts
            List<int> areas = new List<int>();
            for (int ix = 0; ix < tokens.Length; ix++)
            {
                if (ScanBreaks(tokens, ref ix) == false)
                    break;

                areas.Add(ix); // next level starts here

                if (ScanLines(tokens, ref ix) == false)
                    break;
            }
            return areas;
        }
        private static void ClobberOldSpawner(Spawner spawner)
        {
            spawner.ItemID = 0x14f0;                                // deed graphic
            spawner.Visible = true;                                 // we want to see the color
            spawner.Hue = (int)SpawnerGraphics.AllShardsStoppedHue; // red means stop!
            spawner.Running = false;                                // turn off
            spawner.RemoveObjects();                                // remove spawn
            LogHelper logger = new LogHelper(Path.Combine(Core.DataDirectory, Core.Server, "spawner delete list.cfg"), false, true, true);
            logger.Log(LogType.Text, string.Format("{0}:{1}:{2}:{3}:{4}", spawner.Serial.Value, spawner.CoreSpawn, spawner.Source, spawner.Shard, spawner.Location));
            logger.Finish();
        }
        private static void InstantiateNewSpawners(string[] areaSpawnInfo, string[] areaOverrides, List<Spawner> StackedSpawnerTable, List<Spawner> SpawnersPlaced, string logname)
        {
            List<NerunRecord.Record> recordList = new List<NerunRecord.Record>();           // parser to read/convert Nerun's Distro map files
            NerunRecord.Parse(areaSpawnInfo, recordList);                                   // get the record list for this file
            for (int ix = 0; ix < recordList.Count; ix++)                                   // each spawner is represented by one record
            {
                List<string> mobTab = recordList[ix].TableOfMobiles;                        // get the mobiles for this spawner

                // build the spawner
                Spawner spawner = Activator.CreateInstance(typeof(Spawner)) as Spawner;     // create the spawner

                // add the mobs
                foreach (string mob in recordList[ix].TableOfMobiles)                       // pack-out null entries and add to new spawner
                    if (mob == "-null-")
                        continue;
                    else
                        spawner.ObjectNamesRaw.Add(mob);
                /* == Nerun's overrides ==
                 * overrideid 202
                 * overridemap 1
                 * overridemintime 0
                 * overridemaxtime 1
                 * == AI overrides ==
                 * overrideDistro AIDistro
                 * overrideCoreSpawn true
                 * overrideShard AllShards
                 * overrideNeedsReview false
                 */
                // Nerun's overrides (unsupported)
                int overrideid = ContainsOverride(areaOverrides, "overrideid") ? GetOverrideInt(areaOverrides, "overrideid") : 0;       // not supported
                int overridemap = ContainsOverride(areaOverrides, "overridemap") ? GetOverrideInt(areaOverrides, "overridemap") : 0;    // not supported
                // Nerun's overrides (supported)
                spawner.MinDelay = ContainsOverride(areaOverrides, "overridemintime") ? TimeSpan.FromMinutes(GetOverrideInt(areaOverrides, "overridemintime")) : recordList[ix].MinDelay;
                spawner.MaxDelay = ContainsOverride(areaOverrides, "overridemaxtime") ? TimeSpan.FromMinutes(GetOverrideInt(areaOverrides, "overridemaxtime")) : recordList[ix].MaxDelay;
                // AI overrides
                spawner.CoreSpawn = ContainsOverride(areaOverrides, "overrideCoreSpawn") ? GetOverrideBool(areaOverrides, "overrideCoreSpawn") : true;
                spawner.Shard = ContainsOverride(areaOverrides, "overrideShard") ? GetOverrideShardConfig(areaOverrides, "overrideShard") : Spawner.ShardConfig.Core;
                spawner.NeedsReview = ContainsOverride(areaOverrides, "overrideNeedsReview") ? GetOverrideBool(areaOverrides, "overrideNeedsReview") : false;
                spawner.Distro = ContainsOverride(areaOverrides, "overrideDistro") ? GetOverrideSpawnerAttribs(areaOverrides, "overrideDistro") : SpawnerModeAttribs.ModeNeruns;

                // flag this spawner as having already been processed
                if (spawner.GetFlag(SpawnerAttribs.ModeNeruns))
                    spawner.Source = "Nerun's Distro";
                else
                    spawner.Source = "AI Distro";

                // set the properties
                spawner.HomeRange = recordList[ix].HomeRange;                       // how far the spawner can fling the mobiles
                spawner.WalkRange = recordList[ix].WalkRange;                       // how far the mobile can walk after being spawned
                                                                                    // how many creatures to spawn for each entry in the spawner's creatures list
                spawner.SetSlotCount(index: 0, value: recordList[ix].Count);        // mobile count (to spawn) for first cell in the spawner gump
                spawner.SetSlotCount(index: 1, value: recordList[ix].EntryCount[0]);// these next 5 fields apply to the next 5 creatures in the spawner gump
                spawner.SetSlotCount(index: 2, value: recordList[ix].EntryCount[1]);//  each of these five mobiles are capable of having it's own 'count'
                spawner.SetSlotCount(index: 3, value: recordList[ix].EntryCount[2]);
                spawner.SetSlotCount(index: 4, value: recordList[ix].EntryCount[3]);
                spawner.SetSlotCount(index: 5, value: recordList[ix].EntryCount[4]);

                // temp look while we review
                spawner.ItemID = 0x14f0;                            // deed graphic
                spawner.Hue = (int)SpawnerGraphics.NerunRunningHue; // green means go!
                spawner.Visible = true;                             // we want to see the color

                // convert to Concentric for the wide area spawners
                if (spawner.HomeRange > 25)
                    spawner.Concentric = true;

                // stacked spawners - we now believe this to be intentional
                Spawner existing;
                if ((existing = AlreadySpawned(recordList[ix].X, recordList[ix].Y, recordList[ix].Z)) != null)
                {
                    LogHelper log = new LogHelper("Spawner Stacks.log", true, true, true);
                    if (SameSpawner(existing, spawner))
                        // while odd, probably legit. I assume after review, Nerun decided to double the spawn at this location
                        log.Log(LogType.Text, string.Format("Note: trying to add the same spawner at the same location: {0}", new Point3D(recordList[ix].X, recordList[ix].Y, recordList[ix].Z)));
                    else
                        // not too terribly odd, this seems to be intended to maximum mobile WalkRange. I.e., if unstacked, the mobiles walking range could be negatively impacted. 
                        //  additionally, unstacking and not changing the HomeRange could spawn the mobile on the other side of a wall (think shopkeeper.)
                        log.Log(LogType.Text, string.Format("Note: trying to add a different spawner to the same location: {0}", new Point3D(recordList[ix].X, recordList[ix].Y, recordList[ix].Z)));
                    log.Finish();

                    if (!StackedSpawnerTable.Contains(spawner))                         // keep track of these stacked spawners, as later we will place a 'stacked spawner console' atop
                        StackedSpawnerTable.Add(spawner);
                }
                // some regions (for now, only Angel Island Prison) do not want Nerun's spawn. 
                if (ExcludedRegion(new Point3D(recordList[ix].X, recordList[ix].Y, recordList[ix].Z)))
                {
                    Utility.ConsoleWriteLine("Note: blocking creation of spawn at: {0}", ConsoleColor.Yellow, new Point3D(recordList[ix].X, recordList[ix].Y, recordList[ix].Z));
                    spawner.Delete();
                    continue;
                }

                // place it (<== seems a bit anticlimactic)
                spawner.MoveToWorld(new Point3D(recordList[ix].X, recordList[ix].Y, recordList[ix].Z), Map.Felucca);

                // log it baby
                LogHelper logger = new LogHelper(Path.Combine(Core.DataDirectory, Core.Server, logname), false, true, true);
                logger.Log(LogType.Text, string.Format("{0}:{1}:{2}:{3}:{4}", spawner.Serial.Value, spawner.CoreSpawn, spawner.Source, spawner.Shard, spawner.Location));
                logger.Finish();
                recorder.Add(spawner);
                SpawnersPlaced.Add(spawner);

                if (AnyUnusedOverrides(areaOverrides))
                    Utility.ConsoleWriteLine(string.Format("Logic Error: unused override {0}", GetUnusedOverrides(areaOverrides)), ConsoleColor.Red);
            }
        }
        private static string GetUnusedOverrides(string[] overrides)
        {
            for (int ix = 0; ix < overrides.Length; ix++)
                if (overrides[ix] != null)
                    return overrides[ix];

            return null;
        }
        private static bool AnyUnusedOverrides(string[] overrides)
        {
            for (int ix = 0; ix < overrides.Length; ix++)
                if (overrides[ix] != null)
                    return true;
            return false;
        }
        private static bool ContainsOverride(string[] overrides, string match)
        {
            for (int ix = 0; ix < overrides.Length; ix++)
                if (overrides[ix] != null && overrides[ix].ToLower().StartsWith(match.ToLower()))
                    return true;

            return false;
        }
        private static bool GetOverrideBool(string[] overrides, string match)
        {
            bool o = false;
            for (int ix = 0; ix < overrides.Length; ix++)
            {
                if (overrides[ix] != null && overrides[ix].ToLower().StartsWith(match.ToLower()))
                {
                    string[] tokens = overrides[ix].Split(' ');
                    o = bool.Parse(tokens[1]);
                    overrides[ix] = null;   // we do this so we can check for unhanded overrides at the end of processing
                    return o;
                }
            }

            return o;
        }
        private static string GetOverrideString(string[] overrides, string match)
        {
            string o = String.Empty;
            for (int ix = 0; ix < overrides.Length; ix++)
            {
                if (overrides[ix] != null && overrides[ix].ToLower().StartsWith(match.ToLower()))
                {
                    string[] tokens = overrides[ix].Split(' ');
                    o = match.Substring(tokens[0].Length).Trim();
                    overrides[ix] = null;   // we do this so we can check for unhanded overrides at the end of processing
                    return o;
                }
            }

            return o;
        }
        private static int GetOverrideInt(string[] overrides, string match)
        {
            int o = 0;
            for (int ix = 0; ix < overrides.Length; ix++)
            {
                if (overrides[ix] != null && overrides[ix].ToLower().StartsWith(match.ToLower()))
                {
                    string[] tokens = overrides[ix].Split(' ');
                    o = int.Parse(tokens[1]);
                    overrides[ix] = null;   // we do this so we can check for unhanded overrides at the end of processing
                    return o;
                }
            }

            return o;
        }
        private static SpawnerModeAttribs GetOverrideSpawnerAttribs(string[] overrides, string match)
        {
            string o = String.Empty;
            for (int ix = 0; ix < overrides.Length; ix++)
            {
                if (overrides[ix] != null && overrides[ix].ToLower().StartsWith(match.ToLower()))
                {
                    string[] tokens = overrides[ix].Split(' ');
                    o = tokens[1];
                    SpawnerModeAttribs myEnum = (SpawnerModeAttribs)Enum.Parse(typeof(SpawnerModeAttribs), o, true);
                    overrides[ix] = null;   // we do this so we can check for unhanded overrides at the end of processing
                    return myEnum;
                }
            }

            return SpawnerModeAttribs.None;
        }
        private static ShardConfig GetOverrideShardConfig(string[] overrides, string match)
        {
            string o = String.Empty;
            for (int ix = 0; ix < overrides.Length; ix++)
            {
                if (overrides[ix] != null && overrides[ix].ToLower().StartsWith(match.ToLower()))
                {
                    string[] tokens = overrides[ix].Split(' ');
                    o = tokens[1];
                    ShardConfig myEnum = (ShardConfig)Enum.Parse(typeof(ShardConfig), o, true);
                    overrides[ix] = null;   // we do this so we can check for unhanded overrides at the end of processing
                    return myEnum;
                }
            }

            return ShardConfig.None;
        }
        private static bool ExcludedRegion(Point3D px)
        {
            // By Region
            ArrayList list = Region.FindAll(px, Map.Felucca);
            foreach (Region rx in list)
                if (!string.IsNullOrEmpty(rx.Name))
                    if (rx.Name.ToLower().Contains("angel island"))
                        return true;

            return false;
        }
        private static bool SameSpawner(Spawner existing, Spawner toAdd)
        {
            List<Type> existingTypes = existing.ObjectTypes;
            List<Type> toAddTypes = toAdd.ObjectTypes;
            if (existingTypes.Count != toAddTypes.Count) return false;
            if (existing.HomeRange != toAdd.HomeRange) return false;
            if (existing.WalkRange != toAdd.WalkRange) return false;
            //if (existing.EntryCount.Sum() != toAdd.EntryCount.Sum()) return false;
            if (existing.Count != toAdd.Count) return false;
            foreach (Type type in existingTypes)
                if (!toAddTypes.Contains(type)) return false;
            return true;
        }
        private static Spawner AlreadySpawned(int X, int Y, int Z)
        {
            foreach (object o in Map.Felucca.GetObjectsInBounds(new Rectangle2D(X, Y, 1, 1)))   // does 0,0 work? seems not
                if (o is Spawner spawner)
                    if (spawner.ModeNeruns == true)
                        if (spawner.Location.X == X && spawner.Location.Y == Y && spawner.Location.Z == Z)
                            return spawner;
            return null;
        }
        public static class NerunRecord
        {
            public class Record
            {
                public List<string> TableOfMobiles = new List<string>();
                public int X;
                public int Y;
                public int Z;
                public int[] EntryCount = new int[5];
                public int Count;
                public int HomeRange;
                public int WalkRange;
                public TimeSpan MinDelay;
                public TimeSpan MaxDelay;
            }
            public static void Parse(string[] spawnInfo, List<NerunRecord.Record> recordList)
            {
                for (int ix = 0; ix < spawnInfo.Length; ix++)
                {
                    Record record = new Record();

                    for (int jx = 0; jx < 6; jx++)
                        if (spawnInfo[ix] != "-null-")
                            record.TableOfMobiles.Add(spawnInfo[ix++]);
                        else
                            ix++;

                    //  , , , , , , 
                    // C:\Users\luket\Documents\Software\Development\Product\Src\Angel Island Reference Meterial\runuo-nerun-distro-master\Distro\Scripts\Customs\Nerun's Distro\New\Commands\SpawnGen.cs
                    // op.WriteLine( "*|{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}|{13}|{14}|{15}|{16}|{17}|{18}|{19}|{20}",
                    // towrite, towriteA, towriteB, towriteC, towriteD, towriteE,
                    // itemsave2.X, itemsave2.Y, itemsave2.Z, mapnumber, timer1d, timer2d,
                    // itemsave2.WalkingRange, itemsave2.HomeRange, itemsave2.SpawnID, itemsave2.Count,
                    // itemsave2.CountA, itemsave2.CountB, itemsave2.CountC, itemsave2.CountD, itemsave2.CountE );
                    //  , , , , , , 
                    string sX = spawnInfo[ix++];                    // itemsave2.X 
                    string sY = spawnInfo[ix++];                    // itemsave2.Y
                    string sZ = spawnInfo[ix++];                    // itemsave2.Z
                    string mapnumber = spawnInfo[ix++];             // mapnumber
                    string timer1d = spawnInfo[ix++];               // timer1d
                    string timer2d = spawnInfo[ix++];               // timer2d
                    string WalkingRange = spawnInfo[ix++];         //itemsave2.WalkingRange,
                    string HomeRange = spawnInfo[ix++];            //itemsave2.HomeRange, 
                    string itemsave2_SpawnID = spawnInfo[ix++];     // itemsave2.SpawnID,
                    string Count = spawnInfo[ix++];                // itemsave2.Count,
                    string itemsave2_CountA = spawnInfo[ix++];      // itemsave2.CountA,
                    string itemsave2_CountB = spawnInfo[ix++];      // itemsave2.CountB,
                    string itemsave2_CountC = spawnInfo[ix++];      // itemsave2.CountC,
                    string itemsave2_CountD = spawnInfo[ix++];      // itemsave2.CountD,
                    string itemsave2_CountE = spawnInfo[ix];        // itemsave2.CountE

                    // set the properties we care about
                    record.X = int.Parse(sX);
                    record.Y = int.Parse(sY);
                    record.Z = int.Parse(sZ);
                    record.Count = int.Parse(Count);
                    record.HomeRange = int.Parse(HomeRange);
                    record.WalkRange = int.Parse(WalkingRange);
                    record.MinDelay = TimeSpan.FromMinutes(int.Parse(timer1d));
                    record.MaxDelay = TimeSpan.FromMinutes(int.Parse(timer2d));
                    // how many creatures to spawn for each entry in the spawner's creatures list
                    record.EntryCount[0] = int.Parse(itemsave2_CountA);
                    record.EntryCount[1] = int.Parse(itemsave2_CountB);
                    record.EntryCount[2] = int.Parse(itemsave2_CountC);
                    record.EntryCount[3] = int.Parse(itemsave2_CountD);
                    record.EntryCount[4] = int.Parse(itemsave2_CountE);

                    recordList.Add(record);
                }
            }
        }
        public static Spawner SpawnerFromCompiledTokens(List<string> compiledTokens)
        {
            Spawner spawner = Activator.CreateInstance(typeof(Spawner)) as Spawner;     // create the spawner
            int ix = 6;
            spawner.Location = new Point3D(int.Parse(compiledTokens[ix++]), int.Parse(compiledTokens[ix++]), int.Parse(compiledTokens[ix++]));
            ix++;    // mapnumber
            spawner.MinDelay = TimeSpan.FromMinutes(int.Parse(compiledTokens[ix++]));
            spawner.MaxDelay = TimeSpan.FromMinutes(int.Parse(compiledTokens[ix++]));
            spawner.WalkRange = int.Parse(compiledTokens[ix++]);
            spawner.HomeRange = int.Parse(compiledTokens[ix++]);
            ix++; // itemsave2_SpawnID
            /*
            spawner.Count = int.Parse(compiledTokens[ix++]);
            spawner.EntryCount[0] = int.Parse(compiledTokens[ix++]);
            spawner.EntryCount[1] = int.Parse(compiledTokens[ix++]);
            spawner.EntryCount[2] = int.Parse(compiledTokens[ix++]);
            spawner.EntryCount[3] = int.Parse(compiledTokens[ix++]);
            spawner.EntryCount[4] = int.Parse(compiledTokens[ix++]);
            */

            spawner.SetSlotCount(index: 0, value: int.Parse(compiledTokens[ix++]));// mobile count (to spawn) for first cell in the spawner gump
            spawner.SetSlotCount(index: 1, value: int.Parse(compiledTokens[ix++]));// these next 5 fields apply to the next 5 creatures in the spawner gump
            spawner.SetSlotCount(index: 2, value: int.Parse(compiledTokens[ix++]));//  each of these five mobiles are capable of having it's own 'count'
            spawner.SetSlotCount(index: 3, value: int.Parse(compiledTokens[ix++]));
            spawner.SetSlotCount(index: 4, value: int.Parse(compiledTokens[ix++]));
            spawner.SetSlotCount(index: 5, value: int.Parse(compiledTokens[ix++]));

            return spawner;
        }
        private static bool ScanBreaks(string[] tokens, ref int ix)
        {
            for (int jx = ix; jx < tokens.Length; jx++, ix = jx)
            {
                if (tokens[jx].ToString().ToLower() == "-break-")
                    continue;

                break;
            }
            return ix < tokens.Length ? true : false;
        }
        private static bool ScanLines(string[] tokens, ref int ix)
        {
            for (int jx = ix; jx < tokens.Length; jx++, ix = jx)
            {
                if (tokens[jx].ToString().ToLower() != "-break-")
                    continue;

                break;
            }
            return ix < tokens.Length ? true : false;
        }
        private static bool OnIgnoreList(string line)
        {
            string[] ignore = {
                "treasurelevel1",
                "treasurelevel2",
                "treasurelevel3",
                "treasurelevel4",
                "acidelemental",
                "miniaturemushroom",
                "hirebard",
                "hirebardarcher",
                "hirebeggar",
                "hiremage",
                "hireranger",
                "hirerangerarcher",
                "hiresailor",
                "hirethief",
                "hirepaladin",
                "abbein",
                "taellia",
                "vicaie",
                "mallew",
                "jothan",
                "alethanian",
                "aneen",
                "aluniol",
                "rebinil",
                "athialon",
                "bolaevin",
                "alelle",
                "daelas",
                "merchant",
                "tyeelor",
                "peasant",
                "olaeni",
                "hirepeasant",
                "harbormaster",
                "bridegroom",
                "escortablewanderinghealer",
                "artist",
                "boatpainter",
                "bodysculptor",
                "treasurelevel1h",
                "dockmaster" };

            foreach (string iStr in ignore)
                if (line.ToLower().Contains(iStr)) return true;

            return false;
        }

        [Usage("AISpecial")]
        [Description("Reclassifies this spawner as a non-core AISpecial.")]
        private static void AISpecial_OnCommand(CommandEventArgs args)
        {
            if (!(Core.RuleSets.StandardShardRules()))
            {
                args.Mobile.SendMessage("This command can only be used on Siege, Mortalis or Renaissance to reclassify a spawner as AISpecial.");
                return;
            }
            else if (Environment.MachineName.Length.ToString().ToLower() == "LUKES-AISERVER".ToLower())
            {
                args.Mobile.SendMessage("Because the resultant patch file needs to be checked into SVN, this must be run on a developer machine.");
                return;
            }
            args.Mobile.SendMessage("Target the spawner to reclassify.");
            args.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(OnTarget));

        }
        private static void OnTarget(Mobile from, object target)
        {
            Spawner spawner = target as Spawner;

            if (spawner == null)
            {
                from.SendMessage("You must target a spawner.");
                return;
            }
            // turn off and remove spawn
            spawner.Running = false;
            spawner.RemoveObjects();

            // the file created by this function is read and processed by all shards, and spawners are updated accordingly
            UpdateSpawnerDesignationaAndLog(spawner: spawner, coreSpawn: false, uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AngelIsland);
        }
    }
}