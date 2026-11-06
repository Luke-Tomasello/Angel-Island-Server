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

/***************************************************************************
 *                              Patcher.cs
 *
 *  GMN World Patch / Migration System
 *
 *  OVERVIEW
 *  --------
 *  The Patcher applies one-time, ordered changes to an existing shard world.
 *  Patches run sequentially during server startup. Each patch has a permanent
 *  PatchIndex identity whose completion state is persisted. Completed patches
 *  are skipped on later startups.
 *
 *  Implementations live in external patch libraries that own fixed ranges:
 *
 *      Patches001_100.dll
 *      Patches101_200.dll
 *      Patches201_300.dll
 *
 *  Each library reports the highest patch defined in its range. The Patcher
 *  walks those ranges in order, skips already-completed patches, and invokes
 *  the owning library. Empty or retired slots return null and are ignored.
 *  A live patch may also return null when it does not apply to the current
 *  shard or server configuration (e.g. LoginServer, or a ruleset check).
 *
 *  Individual patches may contain ruleset gates (Angel Island, Siege,
 *  Mortalis, Seasons, Upheaval, etc.) so one sequence can be shared
 *  across shards.
 *
 *  Patch methods are named by permanent position, not by what they do:
 *
 *      PatchV1, PatchV2, PatchV3, ...
 *
 *  Document the work inside the method (named regions and comments).
 *
 *  ADDING A PATCH
 *  --------------
 *  1. Add the next PatchIndex enum value.
 *
 *         PatchV241,
 *
 *  2. Add the matching method in the library that owns that numeric range,
 *     using the standard pattern:
 *
 *         private static int? PatchV241(int patchid)
 *         {
 *             int patched = 0;
 *             PatchIndex bits = PatchIndex.PatchV241;
 *
 *             if (!Patched(bits) && AllShards() && !LoginServer(quiet: true))
 *             {
 *                 #region Begin Implementation
 *
 *                 // World / configuration changes here.
 *
 *                 #endregion End Implementation
 *
 *                 EchoOut(
 *                     string.Format("{1} patched with {0} objects updated",
 *                         patched, GetPatchName(bits)),
 *                     ConsoleColor.Magenta);
 *
 *                 PatchComplete(bits, patchid);
 *                 return patched;
 *             }
 *
 *             return null;   // already done, or does not apply
 *         }
 *
 *  3. Register the method in that library's dispatch table at its permanent
 *     numeric position.
 *
 *         PatchV239,
 *         PatchV240,
 *         PatchV241,
 *
 *     Retired slots must not be collapsed or reused. Keep the hole as null:
 *
 *         PatchV171,
 *         null,       // PatchV172 retired
 *         PatchV173,
 *
 *  IMPORTANT
 *  ---------
 *  Patch numbers are persistent identities. Once released, do not reorder,
 *  renumber, reuse, or collapse slots. Live shards have completion state
 *  bound to those identities.
 *
 *  Prefer rerun-safe implementations. Administrators can force a patch at
 *  runtime via Patcher.RunPatch(patchNumber), which clears that patch's
 *  persisted completion state and then invokes it. A safe patch can be
 *  reapplied for debugging or shard transitions without duplicating or
 *  corrupting world state.
 *
 ***************************************************************************/

using Server.Diagnostics;
using Server.Engines.Craft;
using Server.Engines.CronScheduler;
using Server.Items;
using Server.Items.Triggers;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static Server.CoreAI;
using static Server.Engines.SpawnerManager;
using static Server.Item;
using static Server.Mobiles.Spawner;
using static Server.Utility;

namespace Server
{
    public static class Patcher
    {
        static int m_PatchID = 0;

        [CallPriority(10)]  // must come after coreai.xml is loaded
        public static void Configure()
        {
            // executes before the world loads
            EventSink.PreWorldLoad += new PreWorldLoadEventHandler(EventSink_PreWorldLoad);
        }

        private delegate int? PatchHandler(int patchid);

        // executes after world load but BEFORE EventSink.ServerStarted
        public static void Initialize()
        {
            if (Core.Patching == false)
            {
                Utility.Monitor.WriteLine("Patching disabled with the -nopatch startup switch", ConsoleColor.Red);
                return;
            }

            // executes after world load AFTER EventSink.Initialize
            EventSink.ServerStarted += new ServerStartedEventHandler(EventSink_ServerStarted);

            /*  adam: This is the startup patcher.
             *  You'll usually put onetime patching logic here.
             */
            string pathName = Path.Combine(Core.DataDirectory, "Patches", "ConsoleHistory.log");
            EnsurePath(pathName);
            Utility.Monitor.ConsoleOutEcho = pathName;
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();

            // Migrate legacy patch history before normal patch processing.
            MigratePatchHistory();

            try
            {
                int patches = 0;
                bool patchRan = false;

                patches += AlwaysRunFirst(m_PatchID++);

                foreach (PatchLibraryInfo library in m_PatchLibraries)
                {
                    int lastPatch = GetLastExternalPatch(library);

                    for (int i = library.FirstPatch; i <= lastPatch; i++)
                    {
                        PatchIndex bits = (PatchIndex)i;

                        // Patch history is owned by the dispatcher.
                        if (Patched(bits))
                            continue;

                        int? result = RunExternalPatch(i);

                        // null means an empty/retired slot or a patch
                        // that is not applicable to this shard/server.
                        if (!result.HasValue)
                            continue;

                        int patched = result.Value;

                        EchoOut(
                            string.Format(
                                "{1} patched with {0} objects updated",
                                patched,
                                GetPatchName(bits)),
                            ConsoleColor.Magenta);

                        PatchComplete(bits, i);

                        patches += patched;
                        patchRan = true;
                    }
                }

                // apply last
                patches += AlwaysRunLast(m_PatchID++);

                if (!patchRan && patches == 0)
                    EchoOut("No Initialize patching required.", ConsoleColor.Magenta);
            }
            #region Finally
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                Utility.Monitor.ConsoleOutEcho = null;
            }

            tc.End();
            System.Console.WriteLine("Patcher1 completed in {0}", tc.TimeTaken);
            #endregion Finally
        }
        private static void MigratePatchHistory()
        {
            if (Core.MigratePatchHistory && !Patched(PatchIndex.PatchV240))
                RunPatchNormally((int)PatchIndex.PatchV240);
        }
        private static void EventSink_PreWorldLoad()
        {
            Utility.Monitor.ConsoleOutEcho = Path.Combine(Core.LogsDirectory, "ConsoleHistory.log");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();

            try
            {
                int patches = 0;
                bool patchRan = false;

                PatchIndex[] preWorldLoadPatches =
                {
                    PatchIndex.PatchV236,
                    PatchIndex.PatchV241,
                    PatchIndex.PatchV245,
                    PatchIndex.PatchV246,
                    PatchIndex.PatchV248,
                };

                foreach (PatchIndex bits in preWorldLoadPatches)
                {
                    if (Patched(bits))
                        continue;

                    int? result = RunPatchNormally((int)bits);

                    if (result.HasValue)
                    {
                        patches += result.Value;
                        patchRan = true;
                    }
                }

                if (!patchRan)
                    EchoOut("No special 'PreWorldLoad' patching required.", ConsoleColor.Magenta);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                Utility.Monitor.ConsoleOutEcho = null;
            }

            tc.End();
            System.Console.WriteLine("Patcher3 completed in {0}", tc.TimeTaken);
        }

        public static void EventSink_ServerStarted()
        {
            Utility.Monitor.ConsoleOutEcho = Path.Combine(Core.LogsDirectory, "ConsoleHistory.log");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();

            try
            {
                int patches = 0;
                bool patchRan = false;

                PatchIndex bits;
                int? result;

                bits = PatchIndex.PatchV237;
                if (!Patched(bits))
                {
                    result = RunPatchNormally((int)bits);

                    if (result.HasValue)
                    {
                        patches += result.Value;
                        patchRan = true;
                    }
                }

                bits = PatchIndex.PatchV238;
                if (!Patched(bits))
                {
                    result = RunPatchNormally((int)bits);

                    if (result.HasValue)
                    {
                        patches += result.Value;
                        patchRan = true;
                    }
                }

                bits = PatchIndex.PatchV239;
                if (!Patched(bits))
                {
                    result = RunPatchNormally((int)bits);

                    if (result.HasValue)
                    {
                        patches += result.Value;
                        patchRan = true;
                    }
                }

                if (!patchRan)
                    EchoOut("No special 'ServerStarted' patching required.", ConsoleColor.Magenta);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                Utility.Monitor.ConsoleOutEcho = null;
            }

            tc.End();
            System.Console.WriteLine("Patcher2 completed in {0}", tc.TimeTaken);
        }
        #region AlwaysRun
        private static int AlwaysRunFirst(int patchid)
        {
            int patches = 0;
            PatchIndex bits = PatchIndex.None;
            int patched = 0;

            #region Begin Implementation

            #region Ensure IP/Account info is configured for DEBUG vs PROD
            if (Mortalis())
            {
                //EchoOut("Run Always: Configure login restrictions for Mortalis...", ConsoleColor.Yellow);
                //#if DEBUG
                //                CoreAI.MaxConcurrentAddresses = Math.Max(CoreAI.MaxConcurrentAddresses, 3);
                //                CoreAI.MaxAccountsPerIP = Math.Max(CoreAI.MaxAccountsPerIP, 3);
                //                CoreAI.MaxAccountsPerMachine = Math.Max(CoreAI.MaxAccountsPerMachine, 3);
                //#else
                //                CoreAI.MaxConcurrentAddresses = CoreAI.MaxAccountsPerIP = CoreAI.MaxAccountsPerMachine = 1;
                //#endif
                //                patched += 3;
            }
            #endregion Ensure IP/Account info is configured for DEBUG vs PROD

            #region Delete all DeleteOnRestart items
            if (AllShards())
            {
                EchoOut("Run Always: Deleting all DeleteOnRestart items...", ConsoleColor.Yellow);
                foreach (Item item in World.Items.Values)
                    if (item.GetItemBool(ItemBoolTable.DeleteOnRestart))
                    {
                        item.Delete();
                        patched++;
                    }
            }
            #endregion Delete all DeleteOnRestart items

            #region Initialize Craftables Table
            if (AllShards())
            {
                EchoOut("Run Always: Initializing 'craftables' table...", ConsoleColor.Yellow);
                patched += CraftSystem.InitCraftTable();
            }
            #endregion Initialize Craftables Table

            #region Update Enable/disable temporarily modified spawners 
            if (Siege())
            {   // for this particular township, we disable all the Nerun's Distro spawners (full of monsters,) and enable friendly forest creatures.
                //  Should the township go away, this will be reversed.
                //  This mechanism can be expanded to have sets of spawners associated with a particular township. Currently it's hard-coded to a single township.
                #region Is the township still here?
                bool TownshipEstablished = false;
                Item? witem = World.FindItem(0x400AB0FD); // just a Nerun's Distro Spawner near township
                if (witem is Spawner wspawner)
                {
                    Region tsr = TownshipRegion.Find(wspawner.Location, wspawner.Map);
                    if (tsr is TownshipRegion)
                        TownshipEstablished = true;
                    else
                        TownshipEstablished = false;
                }
                #endregion Is the township still here?

                #region Standard Shard Spawners
                // set these standard spawners to make this township a happy place
                List<Serial> standard_spawners = new() { 0x400AB0FD, 0x400AB0FC, 0x400AB0FE, 0x400AB0FF, 0x400AB100 };
                foreach (var serial in standard_spawners)
                {
                    Item? item = World.FindItem(serial);
                    if (item is Spawner spawner)
                    {
                        System.Diagnostics.Debug.Assert(spawner.ModeNeruns == true);
                        if (spawner.Running == false && TownshipEstablished == false)
                        {   // enable
                            spawner.Running = true;
                            spawner.ScheduleRespawn = true;
                        }
                        else if (spawner.Running == true && TownshipEstablished == true)
                        {   // disable
                            spawner.ScheduleDespawn = true;
                            spawner.Running = false;
                        }
                    }
                }
                #endregion Standard Shard Spawners

                #region Custom Spawners
                // set these custom spawners to return this area to standard OSI
                List<Serial> custom_spawners = new() { 0x40041E7D, 0x40042327, 0x400422D6 };
                foreach (var serial in custom_spawners)
                {
                    Item? item = World.FindItem(serial);
                    if (item is Spawner spawner)
                    {
                        System.Diagnostics.Debug.Assert(spawner.ModeNeruns == false);
                        if (spawner.Running == false && TownshipEstablished == true)
                        {   // enable
                            spawner.Running = true;
                            spawner.ScheduleRespawn = true;
                        }
                        else if (spawner.Running == true && TownshipEstablished == false)
                        {   // disable
                            spawner.ScheduleDespawn = true;
                            spawner.Running = false;
                        }
                    }
                }
                #endregion Custom Spawners
            }
            #endregion Update Enable/disable temporarily modified spawners 

            #region Allow 5 accounts on TC in debug mode
            if (AllShards() && Core.GMNTC_CFG && Core.Debug)
            {
                CoreAI.MaxAccountsPerIP = 5;
                CoreAI.MaxConcurrentAddresses = 5;
                CoreAI.MaxAccountsPerMachine = 5;
                patched += 3;
            }
            #endregion Allow 5 accounts on TC in debug mode

            #endregion End Implementation

            EchoOut(string.Format("Run Always: {0} objects updated", patched), ConsoleColor.Magenta);
            //PatchComplete(bits, patchid);
            patches = patched;
            return patches;
        }
        private static int AlwaysRunLast(int patchid)
        {
            int patches = 0;
            PatchIndex bits = PatchIndex.None;
            int patched = 0;

            #region Begin Implementation

            #region Enforce TestCenterStone only exists on Test Center
            if (!TestCenter())
            {
                List<Item> stones = new();

                foreach (Item item in World.Items.Values)
                    if (item is TestCenterStone)
                        stones.Add(item);

                foreach (Item item in stones)
                {
                    item.Delete();
                    patched++;
                }
            }
            #endregion Enforce TestCenterStone only exists on Test Center

            #endregion End Implementation

            EchoOut(string.Format("Run Always: {0} objects updated", patched), ConsoleColor.Magenta);
            patches = patched;
            return patches;
        }
        #endregion

        #region Wrappers
        public static int PatchV151(int patchid, bool utility = false)
        {
            int patches = 0;
            PatchIndex bits = utility ? PatchIndex.None : PatchIndex.PatchV151;
            if ((!Patched(bits) || utility) && AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Patch link database
                if (AllShards())
                {
                    List<Mobile> mobiles_that_are_linked = new();
                    List<Item> items_that_are_linked = new();
                    List<IEntity> objects_that_are_linked = new();
                    // build lists of all the items and mobiles that are linked to
                    foreach (Item item in World.Items.Values)
                    {
                        if (item is ITrigger || item is ITriggerable)
                        {
                            mobiles_that_are_linked.AddRange(Utility.GetLinksFrom(item, typeof(Mobile)).Cast<Mobile>().ToList());
                            items_that_are_linked.AddRange(Utility.GetLinksFrom(item, typeof(Item)).Cast<Item>().ToList());
                            objects_that_are_linked.AddRange(Utility.GetLinksFrom(item, typeof(IEntity)).Cast<IEntity>().ToList());
                        }
                    }

                    mobiles_that_are_linked = mobiles_that_are_linked.Distinct().ToList();
                    items_that_are_linked = items_that_are_linked.Distinct().ToList();

                    foreach (var ent in objects_that_are_linked)
                        if (ent is Item)
                        {
                            if (!items_that_are_linked.Contains(ent as Item))
                                items_that_are_linked.Add(ent as Item);
                        }
                        else if (ent is Mobile)
                        {
                            if (!mobiles_that_are_linked.Contains(ent as Mobile))
                                mobiles_that_are_linked.Add(ent as Mobile);
                        }

                    // now, loop through items and mobiles and set their 'linked' bit if they are linked to
                    int unlinked_items = 0;
                    int unlinked_mobiles = 0;
                    foreach (Item item in items_that_are_linked)
                    {
                        if (!item.GetItemBool(ItemBoolTable.IsLinked))
                        {
                            item.SetItemBool(ItemBoolTable.IsLinked, true);
                            unlinked_items++;
                        }
                        else
                            patched++;
                    }

                    foreach (Mobile mob in mobiles_that_are_linked)
                    {
                        if (!mob.GetMobileBool(Mobile.MobileBoolTable.IsLinked))
                        {
                            unlinked_mobiles++;
                            mob.SetMobileBool(Mobile.MobileBoolTable.IsLinked, true);
                        }
                        else
                            patched++;
                    }
                }
                #endregion Patch link database

                #endregion End Implementation
                EchoOut(string.Format("{1} patched with {0} objects updated", patched, GetPatchName(bits)), ConsoleColor.Magenta);
                PatchComplete(bits, patchid);
                patches = patched;
            }

            return patches;
        }
        public static int PatchV149(int patchid, bool utility = false)
        {
            int patches = 0;
            PatchIndex bits = utility ? PatchIndex.None : PatchIndex.PatchV149;
            if (!Patched(bits) && AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Set Stable Masters to Breeders only, and no claim tickets
                if (AllShards())
                {
                    foreach (Mobile m in World.Mobiles.Values)
                        if (m is StableMaster sm)
                        {
                            sm.BreedersOnly = true;
                            sm.UseChecks = false;
                            patched++;
                        }
                }
                #endregion Set Stable Masters to Breeders only, and no claim tickets

                #endregion End Implementation
                EchoOut(string.Format("{1} patched with {0} objects updated", patched, GetPatchName(bits)), ConsoleColor.Magenta);
                PatchComplete(bits, patchid);
                patches = patched;
            }

            return patches;
        }
        #endregion Wrappers

        #region Tools
        public static bool DoAnySpawnersSpawn(string spawnName, Map map)
        {
            foreach (Item item in World.Items.Values)
            {
                if (item is Spawner spawner &&
                    !spawner.Deleted &&
                    spawner.Running &&
                    spawner.Map == map &&
                    spawner.Spawns(spawnName))
                {
                    return true;
                }
            }

            return false;
        }
        public static bool EnableTeleporter(Serial serial)
        {
            Item? item = World.FindItem(serial);

            if (item is not Teleporter teleporter || teleporter.Deleted)
                return false;

            teleporter.Running = true;
            return true;
        }
        public static Static? AddStatic(int itemID, Point3D location, Map map)
        {
            if (map == null)
                return null;

            IPooledEnumerable eable = map.GetItemsInRange(location, 0);

            foreach (Item item in eable)
            {
                if (item is Static existing &&
                    existing.ItemID == itemID &&
                    existing.Location == location)
                {
                    eable.Free();
                    return existing;
                }
            }

            eable.Free();

            Static added = new Static(itemID);
            added.MoveToWorld(location, map);

            return added;
        }
        public static bool SetSpawnerFlags(Point3D px, SpawnFlags flags)
        {
            Spawner? spawner = (Spawner?)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
            if (spawner != null)
            {
                spawner.SpawnerFlags |= flags;
            }

            return (spawner != null);
        }
        private static bool AddTriggerSpawner(Point3D px, uint key, List<string> creatures, bool coreSpawn, ref TriggerSpawner spawner, int count = 1, int homeRange = 4, bool force = false)
        {
            spawner = (TriggerSpawner)FindOneItemAt(px, Map.Felucca, typeof(TriggerSpawner), 2, false);
            if (spawner == null || force == true)
            {
                spawner = new(key);

                // reclassify here as 'all shards'
                if (coreSpawn)
                    UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: coreSpawn,
                        uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                spawner.Count = count;
                spawner.HomeRange = homeRange;
                foreach (string mob in creatures)
                    spawner.ObjectNamesRaw.Add(mob);
                spawner.MoveToWorld(px, Map.Felucca);
            }

            return (spawner != null);
        }
        public static bool AddSpawner(Point3D px, List<string> creatures, bool coreSpawn, ref Spawner spawner, int count = 1, int homeRange = 4, bool force = false)
        {
            spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
            if (spawner == null || force == true)
            {
                spawner = new();

                // reclassify here as 'all shards'
                if (coreSpawn)
                    UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: coreSpawn,
                        uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                spawner.Count = count;
                spawner.HomeRange = homeRange;
                foreach (string mob in creatures)
                    spawner.ObjectNamesRaw.Add(mob);
                spawner.MoveToWorld(px, Map.Felucca);
                spawner.Running = true;
                spawner.ScheduleRespawn = true;
            }

            return (spawner != null);
        }
        public static bool MakeSpawnerPermanent(Point3D px, ref Spawner new_spawner)
        {
            EventSpawner? event_spawner = (EventSpawner?)FindOneItemAt(px, Map.Felucca, typeof(EventSpawner), 2, false);
            new_spawner = null;
            if (event_spawner != null)
            {
                new_spawner = new Spawner();
                CopyPropertyIntersection(dest: new_spawner, src: event_spawner);
                if (new_spawner != null)
                {
                    new_spawner.MoveToWorld(px, event_spawner.Map);
                    event_spawner.Delete();
                }
            }

            return (event_spawner != null && new_spawner != null);
        }
        public static bool MakeTeleporterPermanent(Point3D px, ref Teleporter new_tele)
        {
            EventTeleporter? event_tele = (EventTeleporter?)FindOneItemAt(px, Map.Felucca, typeof(EventTeleporter), 2, false);
            new_tele = null;
            if (event_tele != null)
            {
                new_tele = new Teleporter();
                CopyPropertyIntersection(dest: new_tele, src: event_tele);
                if (new_tele != null)
                {
                    new_tele.MoveToWorld(px, event_tele.Map);
                    event_tele.Delete();
                }
            }

            return (event_tele != null && new_tele != null);
        }
        public static bool DeleteSpawner(Point3D px)
        {
            Spawner? spawner = (Spawner?)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
            if (spawner != null)
            {
                spawner.RemoveObjects();
                spawner.Delete();
            }

            return (spawner != null);
        }
        public static bool ScheduleRespawn(Spawner spawner)
        {
            if (spawner != null)
            {
                spawner.RemoveObjects();
                spawner.ScheduleRespawn = true;
            }

            return (spawner != null);
        }
        public static bool DeleteTeleporter(Point3D px)
        {
            Teleporter? tele = (Teleporter?)FindOneItemAt(px, Map.Felucca, typeof(Teleporter));
            if (tele != null)
            {
                tele.Delete();
            }

            return (tele != null);
        }
        public static bool DeleteItem(Point3D px)
        {
            Item? item = (Item?)FindOneItemAt(px, Map.Felucca, typeof(Item));
            if (item != null)
            {
                item.Delete();
            }

            return (item != null);
        }
        public static bool DeleteItem(Serial serial, Type type)
        {
            Item? item = World.FindItem(serial);

            if (item == null || item.Deleted)
                return false;

            if (!type.IsAssignableFrom(item.GetType()))
                return false;

            item.Delete();
            return true;
        }
        public static bool HideItem(Point3D px)
        {
            Item? item = (Item?)FindOneItemAt(px, Map.Felucca, typeof(Item), 2, false);
            bool bWasVisible = false;
            if (item != null && item.Visible)
            {
                bWasVisible = true;
                item.Visible = false;
            }

            return (item != null && bWasVisible);
        }
        public static bool DupeSpawner(Point3D at, Point3D to, ref Spawner sr)
        {
            Spawner? spawner = (Spawner?)FindOneItemAt(at, Map.Felucca, typeof(Spawner), 2, false);
            Spawner? new_spawner = null;
            if (spawner != null)
            {
                new_spawner = (Spawner)Utility.Dupe(spawner);
                if (new_spawner != null)
                {
                    new_spawner.MoveToWorld(to, spawner.Map);
                    sr = new_spawner;
                }
            }

            return (spawner != null && new_spawner != null);
        }
        public static bool EnableSpawner(Point3D px, ref Spawner spawner, string? designation = null)
        {
            spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
            return EnableSpawner(ref spawner, designation);
        }
        public static bool EnableSpawner(ref Spawner spawner, string? designation = null)
        {
            if (spawner != null)
            {
                // reclassify here as 'all shards'
                UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false,
                    uOSpawnMap: designation != null ? designation : spawner.Source, shardConfig: ShardConfig.AllShards);

                spawner.Running = true;
                spawner.ScheduleRespawn = true;
            }

            return (spawner != null);
        }
        public static bool DisableTeleporter(Point3D px)
        {
            Teleporter? tele = (Teleporter?)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
            if (tele is Teleporter)
                tele.Running = false;
            return (tele != null);
        }
        public static bool DisableSpawner(Point3D px)
        {
            Spawner? spawner = (Spawner?)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
            return DisableSpawner(spawner);
        }
        public static bool DisableSpawner(Spawner? spawner)
        {
            if (spawner != null)
            {
                spawner.Running = false;
                spawner.RemoveObjects();
            }

            return (spawner != null);
        }
        public static bool MoveSpawner(Point3D old_location, Point3D new_location, ref Spawner spawner)
        {
            spawner = (Spawner)FindOneItemAt(old_location, Map.Felucca, typeof(Spawner), 2, false);
            if (spawner != null)
            {
                spawner.Location = new_location;
            }

            return (spawner != null);
        }
        public static bool MoveItem(Point3D old_location, Point3D new_location)
        {
            Item? item = (Item?)FindOneItemAt(old_location, Map.Felucca, typeof(Item), 2, false);
            if (item != null)
            {
                item.Location = new_location;
            }

            return (item != null);
        }

        #endregion Tools

        #region Utility functions

        public static bool Patched(PatchIndex bits)
        {
            // PatchIndex.None is reserved for repeated patching
            return bits == PatchIndex.None ? false : CoreAI.IsDynamicPatchSet(bits);
        }

        /// <summary>
        /// Returns true once the specified UTC date/time has passed.
        /// </summary>
        public static bool HasExpired(DateTime dateUtc)
        {
            return DateTime.UtcNow >= dateUtc;
        }
        private static bool TestCenter(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.TestCenterRules()); else return Core.RuleSets.TestCenterRules(); }
        private static bool EventShard(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.EventShardRules()); else return Core.RuleSets.EventShardRules(); }
        public static bool Siege(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.SiegeRules()); else return Core.RuleSets.SiegeRules(); }
        public static bool AngelIsland(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.AngelIslandRules()); else return Core.RuleSets.AngelIslandRules(); }
        public static bool Mortalis(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.MortalisRules()); else return Core.RuleSets.MortalisRules(); }
        public static bool Seasons(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.SeasonsRules()); else return Core.RuleSets.SeasonsRules(); }
        public static bool Seasons(int season, bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.SeasonsRules(season)); else return Core.RuleSets.SeasonsRules(season); }
        public static bool Upheaval(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.UpheavalRules()); else return Core.RuleSets.UpheavalRules(); }
        public static bool LoginServer(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.LoginServerRules()); else return Core.RuleSets.LoginServerRules(); }
        public static bool TestCenterShards(bool quiet = false)
        {
            if (Core.RuleSets.TestCenterRules())
                return TestCenter(quiet);
            else
                return false;
        }
        public static bool EventShards(bool quiet = false)
        {
            if (Core.RuleSets.EventShardRules())
                return EventShard(quiet);
            else
                return false;
        }

        public static bool SiegeStyleShards(bool quiet = false)
        {
            if (Core.RuleSets.SiegeRules())
                return Siege(quiet);
            else if (Core.RuleSets.MortalisStyleRules())
                return Mortalis(quiet);
            else
                return false;
        }

        public static bool StandardShards(bool quiet = false)
        {
            if (Core.RuleSets.SiegeRules())
                return Siege(quiet);
            else if (Core.RuleSets.MortalisStyleRules())
                return Mortalis(quiet);
            if (Core.RuleSets.SeasonsRules())
                return Seasons(quiet);
            else
                return false;
        }

        public static bool AllShards() { EchoOut("Patching all shards.", ConsoleColor.Green); return true; }

        public static void LogPatch(PatchIndex bits)
        {
            // record the patch
            using (StreamWriter sw = File.AppendText(m_pathName))
            {
                sw.WriteLine(string.Format("{0}: patching...",
                    Enum.GetName(typeof(PatchIndex), bits)));
            }
        }

        public static void PatchComplete(PatchIndex bits, int patchid, bool timeUnknown = false)
        {
            // PatchIndex.None is a special flag that runs each and every server restart. Usually 
            //  to cleanup map (0,0) and other common errors
            if (bits != PatchIndex.None)
                SetDynamicPatch(bits);
            string l1 = string.Format(string.Format("Patch {0} complete. {1}", patchid,
                timeUnknown ? DateTime.MinValue : AdjustedDateTime.GameTime));
            string l2 = string.Format("===========================");
            EchoOut(l1, ConsoleColor.Green, echo: false);
            EchoOut(l2, ConsoleColor.Green, echo: false);
            using (StreamWriter sw = File.AppendText(m_pathName))
            {
                sw.WriteLine(string.Format("{0}: Complete on {1}",
                    GetPatchName(bits),
                    timeUnknown ? DateTime.MinValue : AdjustedDateTime.GameTime));
                sw.WriteLine(l2);
            }
        }
        private static string? GetPatchName(PatchIndex bits)
        {
            return Enum.GetName(typeof(PatchIndex), bits);
        }
        private static bool Patching(bool shards)
        {
            // format for the screen
            {
                bool wasmuted = m_muted;
                if (wasmuted) Unmute();

                if (shards)
                    EchoOut($"Patching {Core.Server}.", ConsoleColor.Green);

                if (wasmuted) Mute();
            }

            return shards;
        }

        public static void EchoOut(string text, ConsoleColor color, bool echo = true)
        {
            Monitor.WriteLine(text, color);
            if (echo)
                using (StreamWriter sw = File.AppendText(m_pathName))
                {
                    sw.WriteLine(string.Format("{0}", text));
                }
        }

        public static void ErrorOut(PatchIndex bits, string text, ConsoleColor color, object? o = null)
        {
            Utility.Monitor.ErrorOut(text, color, o);
            using (StreamWriter sw = File.AppendText(m_pathName))
            {
                sw.WriteLine(string.Format("{0}: {1} {2}", Enum.GetName(typeof(PatchIndex), bits),
                    text, o));
            }
        }
        private static bool m_muted = false;
        private static string m_pathName = Path.Combine(Core.LogsDirectory, "PatchHistory.log");
        public static void Mute()
        {
            m_muted = true;
            Console.SetOut(StreamWriter.Null);
        }

        public static void Unmute()
        {
            m_muted = false;
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);
        }
        public static List<Item> Delta(List<Item> before, List<Item> after)
        {
            //List<Item> list1 = after.Except(before).ToList();
            //List<Item> list2 = before.Except(after).ToList();
            return before.Except(after).ToList();
        }
        private static List<Item> CountItems(Map? map = null)
        {
            List<Item> results = new();
            foreach (Item item in World.Items.Values)
                if (item != null && item.Deleted == false && (map != null) ? (item.Map == map) : true)
                    results.Add(item);

            return results;
        }

        public static List<Item> Before(Map? map = null)
        { return CountItems(map); }

        public static List<Item> After(Map? map = null)
        { return CountItems(map); }
        public static int ConsoleCaptureOn()
        {
            List<string> lines = new();
            string filename = "deco_delete.txt";
            string pathName = Path.Combine(Core.ExeDirectory, "Logs", filename);
            if (System.IO.File.Exists(pathName))
                foreach (String line in System.IO.File.ReadAllLines(pathName))
                    lines.Add(line);
            return lines.Count;
        }

        public static int ConsoleCaptureOff(int index)
        {
            List<string> lines = new();
            string filename = "deco_delete.txt";
            string pathName = Path.Combine(Core.ExeDirectory, "Logs", filename);
            if (System.IO.File.Exists(pathName))
                foreach (String line in System.IO.File.ReadAllLines(pathName))
                    lines.Add(line);

            int patched = 0;
            if (lines.Count > index)
                for (int ix = index; ix < lines.Count; ix++)
                    if (lines[ix].Contains("Deleting a 0x") && lines[ix].Contains("(teleporter)"))
                        patched++;

            return patched;
        }
        public static Item? FindItem(Type t, Map map, Point3D p)
        {
            Item? found = null;

            IPooledEnumerable eable = map.GetItemsInRange(p, 0);
            foreach (Item item in eable)
                if (item.GetType().IsAssignableFrom(t))
                {
                    found = item;
                    break;
                }

            eable.Free();

            return found;
        }
        public static int ScheduleRespawn()
        {   // respawn the world now
            int patches = 0;

            EchoOut("Scheduling world Respawn...", ConsoleColor.Magenta);
            int patched = 0;

            List<Spawner> spawners = new();
            double nextspawn = 0.5;
            foreach (Item item in World.Items.Values)
            {
                if (item is Spawner s && s.Deleted == false && s.Running)
                {
                    // based on our spawner/shard config, should this spawner be respawned?
                    patched++;
                    s.ScheduleRespawn = true;
                }
            }

            EchoOut(string.Format("{0} Spawners scheduled. Spawning will complete in {1:0.00} seconds.", patched, nextspawn), ConsoleColor.Magenta);
            patches = patched;
            return patches;
        }
        public static int RefreshCamps()
        {
            int patched = 0;
            // delete all camps and respawn
            List<BaseCamp> list = new();
            foreach (var item in World.Items.Values)
                if (item is BaseCamp bc && bc.Deleted == false)
                    list.Add(bc);

            foreach (BaseCamp bc in list)
                bc.Delete();

            patched += list.Count;

            // now respawn
            foreach (Item item in World.Items.Values)
                if (item is Spawner spawner && spawner.Running == true && spawner.Spawns(typeof(BaseCamp)))
                {
                    spawner.ScheduleRespawn = true;
                    patched++;
                }

            // ask the Cron scheduler to respawn dynamic camps
            Timer.DelayCall(TimeSpan.FromSeconds(3.0), new TimerStateCallback(DoCronTick), new object?[] { "DynamicCampSystem", null });
            return patched;
        }
        private static void DoCronTick(object state)
        {
            object[] aState = (object[])state;
            string? command = (aState[0] != null) ? aState[0] as string : "";
            CommandEventArgs e = new(World.GetSystemAcct(), null, command, new string[] { command });
            Cron.Run_OnCommand(e);
        }
        #endregion Utility functions

        #region Commands

        internal sealed class PatchLibraryInfo
        {
            public int FirstPatch { get; }
            public int LastPatch { get; }
            public string AssemblyName { get; }
            public string TypeName { get; }
            public MethodInfo GetLastPatchMethod { get; set; }

            public MethodInfo RunPatchMethod { get; set; }

            public PatchLibraryInfo(
                int firstPatch,
                int lastPatch,
                string assemblyName,
                string typeName)
            {
                FirstPatch = firstPatch;
                LastPatch = lastPatch;
                AssemblyName = assemblyName;
                TypeName = typeName;
            }
        }

        private static readonly PatchLibraryInfo[] m_PatchLibraries =
        {
            new PatchLibraryInfo(
                1,
                100,
                "Patches001_100.dll",
                "Patches001_100.PatchLibrary"),

            new PatchLibraryInfo(
                101,
                200,
                "Patches101_200.dll",
                "Patches101_200.PatchLibrary"),

            new PatchLibraryInfo(
                201,
                300,
                "Patches201_300.dll",
                "Patches201_300.PatchLibrary"),
        };
        internal static PatchLibraryInfo[] PatchLibraries { get { return m_PatchLibraries; } }
        private static PatchLibraryInfo? GetPatchLibrary(int patchNumber)
        {
            foreach (PatchLibraryInfo library in m_PatchLibraries)
            {
                if (patchNumber >= library.FirstPatch &&
                    patchNumber <= library.LastPatch)
                {
                    return library;
                }
            }

            return null;
        }
        private static void EnsurePatchLibraryLoaded(PatchLibraryInfo library)
        {
            if (library.RunPatchMethod != null &&
                library.GetLastPatchMethod != null)
            {
                return;
            }

            string path = Path.Combine(
                Core.ExeDirectory,
                library.AssemblyName);

            Assembly assembly = Assembly.LoadFrom(path);

            Type type = assembly.GetType(
                library.TypeName,
                throwOnError: true);

            MethodInfo runPatchMethod = type.GetMethod(
                "RunPatch",
                BindingFlags.Public | BindingFlags.Static);

            if (runPatchMethod == null)
                throw new MissingMethodException(
                    library.TypeName,
                    "RunPatch");

            MethodInfo getLastPatchMethod = type.GetMethod(
                "GetLastPatch",
                BindingFlags.Public | BindingFlags.Static);

            if (getLastPatchMethod == null)
                throw new MissingMethodException(
                    library.TypeName,
                    "GetLastPatch");

            library.RunPatchMethod = runPatchMethod;
            library.GetLastPatchMethod = getLastPatchMethod;
        }
        private static int? RunExternalPatch(int patchNumber)
        {
            PatchLibraryInfo? library = GetPatchLibrary(patchNumber);

            if (library == null)
                return null;

            EnsurePatchLibraryLoaded(library);

            object value = library.RunPatchMethod.Invoke(
                null,
                new object[] { patchNumber });

            // Nullable<int> boxes as either null or int.
            return value == null ? null : (int)value;
        }
        private static int GetLastExternalPatch(PatchLibraryInfo library)
        {
            EnsurePatchLibraryLoaded(library);

            object value = library.GetLastPatchMethod.Invoke(
                null,
                null);

            return (int)value;
        }
        private static int? RunPatchNormally(int patchNumber)
        {
            if (patchNumber <= 0)
                return null;

            PatchIndex bits = (PatchIndex)patchNumber;

            if (Patched(bits))
                return null;

            int? result = RunExternalPatch(patchNumber);

            if (!result.HasValue)
                return null;

            EchoOut(
                string.Format(
                    "{1} patched with {0} objects updated",
                    result.Value,
                    GetPatchName(bits)),
                ConsoleColor.Magenta);

            PatchComplete(bits, patchNumber);

            return result.Value;
        }
        public static int? RunPatch(int patchNumber)
        {
            if (!ValidPatch(patchNumber))
                return null;

            PatchIndex bits = (PatchIndex)patchNumber;

            // RunPatch is explicitly a forced re-run command.
            ClearDynamicPatch(bits);

            return RunPatchNormally(patchNumber);
        }
        public static bool ValidPatch(int patchNumber)
        {
            return Enum.IsDefined(typeof(PatchIndex), patchNumber) &&
                   patchNumber != (int)PatchIndex.None;
        }
        #endregion Commands
    }
}