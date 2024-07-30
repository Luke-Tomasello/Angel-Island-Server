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

/*
    Unusual chests and crates in all dungeons: System Overview
    Throughout, I will be referring to �chests�, but this also includes the three �crate� types as well.
    �	Just walking by with no other skills will reveal the chest
    �	DH allows you to scan an area by clicking the floor nearby � gives a distance advantage over having to walk up to the chest.
    �	All player-owned �unusual keys� have been converted to three different colors: Red, Blue and Yellow. Red is the rarest, then Blue, then Yellow.
        3% of your keys will have been turned Red, 20% Blue, and the rest Yellow
    �	With the exception of some �box� types, virtually all dungeon crates, and chests are now participating in the system.
    �	Role Play Aspect: These crates were left behind many years ago by pirates, and so, the locks are rusty and will break your key. (keys are single use)
    �	Rares: Yellow chests will Yield a rare maybe 3-5% of the time. Blue 10-15%, and Red ~50%.
        Other goodies will drop as well, scaled by the above mentioned color codes. 
    �	Math facts: as of the time of this writing, there are 689 eligible containers in dungeons.  Of those 689, 136 are �unusuals�, or 19.74% of all eligible containers are �unusual�.
    �	Automatic recycling system:
        o	All unusuals have a ~4 hour decay cycle. That is, every 4 hours, these containers despawn, and are replaced with their static counterparts.
        o	When a container is opened, it immediately begins to decay. The decay for an opened container is ~15 minutes. Again, once it decays, it will be replaced with its static counterpart.
        o	This recycling system means the �look� of the dungeon never changes. we don�t add and remove chests.. there will be a constant 689 chests and any given moment.
    �	Key drops have been greatly reduced, so treasure the keys you currently own.
    �	Unusuals cannot be picked and magic does not work on them either. They are not trapped. And no skills are required to open them .. just a key.
    See Also: Scripts\Items\Containers\UnusualContainers.cs
 */

/* Scripts/Engines/Spawner/UnusualContainerSpawner.cs
 * Changelog:
 *  4/22/23, Adam (UnusualChestRules())
 *      For shards other than Angel Island, we simply convert unusual chests to generic dungeon chests.
 * 8/3/22, Adam
 *      Removed "Interesting: We found a locked container..." message
 * 1/28/22, Adam
 *  Cleanup some output statements.
 * 11/16/21, Adam (CleanupItems)
 *  Add CleanupItems(BaseContainer bc)
 *      over time, this routine will slowly and methodically cleanup all dungeon chest trash
 * 11/15/21, Adam
 *  Created
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Server.Items
{

    public static class UnusualContainerSpawner
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoadContainerCache);
        }
        public static List<BaseContainer> UnusualCache = new List<BaseContainer>();
        public static int DeFrag()
        {   // runs probably every 5 minutes (Cron Job)
            int redboxes, blueboxes, yellowboxes;
            redboxes = blueboxes = yellowboxes = 0;
            List<BaseContainer> cleanupList = new List<BaseContainer>();
            foreach (BaseContainer bc in UnusualCache)
                if (bc == null || bc.Deleted)
                {   // remove expired containers from our list
                    cleanupList.Add(bc);
                    continue;
                }
                else if (bc is LockableContainer lc)
                    if (lc.KeyValue == Loot.RedKeyValue)
                        redboxes++;
                    else if (lc.KeyValue == Loot.BlueKeyValue)
                        blueboxes++;
                    else if (lc.KeyValue == Loot.YellowKeyValue)
                        yellowboxes++;

            Console.WriteLine("UCS: Cleanup: deleting {0} UnusualContainers", cleanupList.Count);

            // remove deleted containers from our UnusualCache
            foreach (BaseContainer bc in cleanupList)
            {
                if (UnusualCache.Contains(bc))
                    UnusualCache.Remove(bc);
                if (ContainerCache.Contains(bc))
                    ContainerCache.Remove(bc);  // should never happen (debug)
            }

            // mix up our list of all candidate containers
            Utility.Shuffle(ContainerCache);

            Console.WriteLine("UCS: There are {0} UnusualContainers(before add)", UnusualCache.Count);

            Spawn(RedBoxCount - redboxes, Loot.RedKeyValue);
            Spawn(BlueBoxCount - blueboxes, Loot.BlueKeyValue);
            Spawn(YellowBoxCount - yellowboxes, Loot.YellowKeyValue);

            Console.WriteLine("UCS: There are {0} Containers in the Container Cache, {1} on the internal map", ContainerCache.Count, CountInternalMap(ContainerCache));
            if (RedBoxCount - redboxes > 0)
                Console.WriteLine("UCS: {0} red boxes added", RedBoxCount - redboxes);
            if (BlueBoxCount - blueboxes > 0)
                Console.WriteLine("UCS: {0} blue boxes added", BlueBoxCount - blueboxes);
            if (YellowBoxCount - yellowboxes > 0)
                Console.WriteLine("UCS: {0} yellow boxes added", YellowBoxCount - yellowboxes);

            Console.WriteLine("UCS: desired UnusualContainers: {0} ", (RedBoxCount) + (BlueBoxCount) + (YellowBoxCount));
            Console.WriteLine("UCS: There are {0} UnusualContainers(after add)", UnusualCache.Count);

            return (RedBoxCount - redboxes) + (BlueBoxCount - blueboxes) + (YellowBoxCount - yellowboxes);
        }
        private static int CountInternalMap(List<BaseContainer> list)
        {
            int count = 0;
            foreach (BaseContainer bc in list)
                if (bc != null && bc.Deleted == false && bc.Map == Map.Internal)
                    count++;
                else if (bc != null && bc.Deleted == true)
                {
                    Utility.PushColor(ConsoleColor.Red);
                    Console.WriteLine("Error: Container Cache item unexpectedly deleted.");
                    Utility.PopColor();
                }
            return count;
        }
        private static void Spawn(int count, uint Keyvalue)
        {
            if (count <= 0)
                return;

            int next = 0;
            for (int ix = 0; ix < count && next < ContainerCache.Count; ix++)
            {
                BaseContainer bc = ContainerCache[next++];
                if (bc.Map == Map.Internal)
                {
                    ix--;       // don't count a failed assignment
                    continue;   // skip the containers already on the internal map
                }
                else
                    // assign
                    UnusualCache.Add(MakeUnusual(bc, Keyvalue));
            }
        }
        public static int TotalRespawn()
        {   // Done once initially to seed the world with Unusual Containers, then probably every day? (Cron Job)
            if (ContainerCache.Count == 0)
                return 0;

            List<BaseContainer> list = new List<BaseContainer>();

            // first delete all existing unusuals
            foreach (BaseContainer bc in UnusualCache)
                if (IsUnusualContainer(bc))
                    list.Add(bc);

            Console.WriteLine("Deleting {0} items from the UnusualCache.", UnusualCache.Count);
            // deleting the container will restore the original
            foreach (BaseContainer bc in list)
                if (bc != null && bc.Deleted == false)
                    bc.Delete();
                else
                    ;

            // clear our list of unusuals
            UnusualCache.Clear();

            // verify our container cache is still valid
            foreach (Item item in ContainerCache)
                if (item.Deleted == true)
                {
                    // we should reload?
                    Utility.ConsoleWriteLine("Warning: Unusual Container cache no longer valid.", ConsoleColor.Red);
                }

            // mix up our list of all candidate containers
            Utility.Shuffle(ContainerCache);


            Console.WriteLine("Adding {0} unusual boxes.", RedBoxCount + BlueBoxCount + YellowBoxCount);

            int next, count, redCount, blueCount, yellowCount;
            next = count = redCount = blueCount = yellowCount = 0;

            BaseContainer bcon;
            for (int ix = 0; ix < RedBoxCount && count < ContainerCache.Count; ix++)
            {
                bcon = ContainerCache[next++];
                if (bcon.Map == Map.Internal)
                    continue;
                else
                {
                    count++;
                    redCount++;
                    UnusualCache.Add(MakeUnusual(bcon, Loot.RedKeyValue));
                }
            }

            for (int ix = 0; ix < BlueBoxCount && count < ContainerCache.Count; ix++)
            {
                bcon = ContainerCache[next++];
                if (bcon.Map == Map.Internal)
                    continue;
                else
                {
                    count++;
                    blueCount++;
                    UnusualCache.Add(MakeUnusual(bcon, Loot.BlueKeyValue));
                }
            }

            for (int ix = 0; ix < YellowBoxCount && count < ContainerCache.Count; ix++)
            {
                bcon = ContainerCache[next++];
                if (bcon.Map == Map.Internal)
                    continue;
                else
                {
                    count++;
                    yellowCount++;
                    UnusualCache.Add(MakeUnusual(bcon, Loot.YellowKeyValue));
                }
            }

            Console.WriteLine("Added {0} red boxes.", redCount);
            Console.WriteLine("Added {0} blue boxes.", blueCount);
            Console.WriteLine("Added {0} yellow boxes.", yellowCount);

            Console.WriteLine("There are now {0} items in the UnusualCache.", UnusualCache.Count);
            Console.WriteLine("There are a total of {0} items in the ContainerCache.", ContainerCache.Count);
            Console.WriteLine("There are {0} items in the on the internal map from the ContainerCache.", CountInternalMap(ContainerCache));

            return count;
        }
        // ratio of unusuals to normal
        private static int RedBoxCount => (int)((double)ContainerCache.Count * .03);    // 3% of chests will be red
        private static int BlueBoxCount => (int)((double)ContainerCache.Count * .07);    // 7% of chests will be blue
        private static int YellowBoxCount => (int)((double)ContainerCache.Count * .1);   // 10% of chests will be yellow
        private static void CleanupItems(BaseContainer bc)
        {   // over time, this routine will slowly and methodically cleanup all dungeon chest trash
            if (bc == null || bc.Deleted || bc.Items.Count == 0)
                return;

            List<Item> list = new List<Item>();
            foreach (Item item in bc.Items)
                if (item != null && item.Deleted == false)
                    list.Add(item);

            foreach (Item item in list)
            {
                bc.RemoveItem(item);
                item.Delete();
            }

        }
        private static BaseContainer MakeUnusual(BaseContainer bc, uint keyValue)
        {
            Map map = bc.Map;

            if (bc.Map == Map.Internal || IsUnusualContainer(bc))
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error: MakeUnusual() got {0}.", bc);
                Utility.PopColor();
            }

            // remove all items from this container
            CleanupItems(bc);

            if (bc is SmallCrate)
            {
                UnusualSmallCrate unusualSmallCrate = new UnusualSmallCrate(bc as SmallCrate, keyValue);
                unusualSmallCrate.MoveToWorld(bc.Location, map);
                unusualSmallCrate.Locked = true;
                return unusualSmallCrate;
            }
            else if (bc is MediumCrate)
            {
                UnusualMediumCrate unusualMediumCrate = new UnusualMediumCrate(bc as MediumCrate, keyValue);
                unusualMediumCrate.MoveToWorld(bc.Location, map);
                unusualMediumCrate.Locked = true;
                return unusualMediumCrate;
            }
            else if (bc is LargeCrate)
            {
                UnusualLargeCrate unusualLargeCrate = new UnusualLargeCrate(bc as LargeCrate, keyValue);
                unusualLargeCrate.MoveToWorld(bc.Location, map);
                unusualLargeCrate.Locked = true;
                return unusualLargeCrate;
            }
            else if (bc is WoodenChest)
            {
                UnusualWoodenChest unusualWoodenChest = new UnusualWoodenChest(bc as WoodenChest, keyValue);
                unusualWoodenChest.MoveToWorld(bc.Location, map);
                unusualWoodenChest.Locked = true;
                return unusualWoodenChest;
            }
            else if (bc is MetalGoldenChest)
            {
                UnusualMetalGoldenChest unusualMetalGoldenChest = new UnusualMetalGoldenChest(bc as MetalGoldenChest, keyValue);
                unusualMetalGoldenChest.MoveToWorld(bc.Location, map);
                unusualMetalGoldenChest.Locked = true;
                return unusualMetalGoldenChest;
            }
            else if (bc is MetalChest)
            {
                UnusualMetalChest unusualMetalChest = new UnusualMetalChest(bc as MetalChest, keyValue);
                unusualMetalChest.MoveToWorld(bc.Location, map);
                unusualMetalChest.Locked = true;
                return unusualMetalChest;
            }
            else
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error: Unsupported container type {0}.", bc);
                Utility.PopColor();
            }

            return null;
        }
        public static bool IsUnusualContainer(BaseContainer bc)
        {
            if (bc is LockableContainer lc)
            {
                if (lc.KeyValue == Loot.RedKeyValue || lc.KeyValue == Loot.BlueKeyValue || lc.KeyValue == Loot.YellowKeyValue)
                    return true;

                if (bc is UnusualSmallCrate || bc is UnusualMediumCrate || bc is UnusualLargeCrate || bc is UnusualWoodenChest || bc is UnusualMetalGoldenChest || bc is UnusualMetalChest)
                    return true;
            }

            return false;
        }
        public static bool IsTargetTypeContainer(BaseContainer bc)
        {
            if (bc is LockableContainer lc)
                if (bc is SmallCrate || bc is MediumCrate || bc is LargeCrate || bc is WoodenChest || bc is MetalGoldenChest || bc is MetalChest)
                    return true;
            return false;
        }

        private static BaseContainer ExtractInternal(BaseContainer bc)
        {
            if (bc is UnusualSmallCrate usc)
                return (BaseContainer)usc.OldContainer;
            else if (bc is UnusualMediumCrate umc)
                return (BaseContainer)umc.OldContainer;
            else if (bc is UnusualLargeCrate ulc)
                return (BaseContainer)ulc.OldContainer;
            else if (bc is UnusualWoodenChest uwc)
                return (BaseContainer)uwc.OldContainer;
            else if (bc is UnusualMetalGoldenChest umgc)
                return (BaseContainer)umgc.OldContainer;
            else if (bc is UnusualMetalChest ummc)
                return (BaseContainer)ummc.OldContainer;


            return null;
        }
        private static bool DungeonSpecial(BaseContainer bc)
        {
            // okay, now see if what we are looking at is a dungeon treasure chest
            if (bc is DungeonTreasureChest)
                return true;                    // dungeon treasure chest, don't want it
            if (!(bc is LockableContainer lc))
                return true;                    // not locakable, don't want it
            else if (!IsUnusualContainer(lc))
            {
                if (lc.Locked == true)
                {
                    //Utility.ConsoleOut("Interesting: We found a locked container at {0}.", ConsoleColor.Yellow, lc.Location);
                    return true;
                }
            }

            return false;

        }
        public static List<BaseContainer> ContainerCache = new List<BaseContainer>();
        public static void OnLoadContainerCache()
        {
            ContainerCache = LoadContainerCacheInternal();
            Console.WriteLine("done ({0} locations loaded with {1} on the internal map.)", ContainerCache.Count, CountInternalMap(ContainerCache));

            if (UnusualCache.Count != CountInternalMap(ContainerCache))
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Warning: UnusualCache.Count != CountInternalMap(ContainerCache).");
                Utility.PopColor();
            }
#if DEBUG
            // Loading dungeon chest locations...done (689 locations loaded with 126 on the internal map.)
            // Warning: UnusualCache.Count != CountInternalMap(ContainerCache).
            // count deleted
            int bxDeleted = 0;
            int bxNull = 0;
            foreach (BaseContainer bx in ContainerCache)
            {
                if (bx == null)
                {
                    bxNull++;
                    continue;
                }

                if (bx.Deleted)
                {
                    bxDeleted++;
                    continue;
                }
            }

            int byDeleted = 0;
            int byNull = 0;
            foreach (BaseContainer by in UnusualCache)
            {
                if (by == null)
                {
                    byNull++;
                    continue;
                }

                if (by.Deleted)
                {
                    byDeleted++;
                    continue;
                }
            }
#endif

#if DEBUG
            // this code will log is one of these gets deleted
            foreach (BaseContainer bc in ContainerCache)
                if (Item.DeleteMon.Contains(bc))
                    continue;
                else
                    Item.DeleteMon.Add(bc);
#endif
        }
        public static int WipeContainerCache()
        {
            DeFrag();

            List<BaseContainer> cleanupList = new List<BaseContainer>();
            foreach (BaseContainer bc in UnusualCache)
                if (bc == null)
                {   //probably don't need to do anything - OldContainer has been restored(?)
                    ;
                }
                else if (bc.Deleted)
                {   //probably don't need to do anything - OldContainer has been restored(?)
                    ;
                }
                else if (bc is LockableContainer lc)
                {   // delete this unusual, it will then restore the OldContainer
                    ;
                    cleanupList.Add(bc);
                    ;
                }

            Console.WriteLine("UCS: Cleanup: deleting {0} UnusualContainers", cleanupList.Count);

            // remove deleted containers from our UnusualCache
            foreach (BaseContainer bc in cleanupList)
            {
                if (bc is BaseUnusualContainer buc)
                {
                    if (UnusualCache.Contains(buc))
                    {
                        UnusualCache.Remove(buc);
                        buc.Delete();
                    }
                    if (ContainerCache.Contains(buc))
                        Utility.ConsoleWriteLine("Logic error: {0} still remains in the UnusualCache.", ConsoleColor.Red, buc);
                }
                else
                    Utility.ConsoleWriteLine("Logic error: {0} is not an unusual container.", ConsoleColor.Red, bc);
            }

            // remove our notion of dungeon chests. We will rebuild this in RebuildContainerCache, or on server up.
            ContainerCache.Clear();

            return cleanupList.Count;
        }
        public static int RebuildContainerCache()
        {
            DeFrag();
            if (ContainerCache.Count == 0)
            {
                ContainerCache = LoadContainerCacheInternal();
                Console.WriteLine("done ({0} locations loaded with {1} on the internal map.)", ContainerCache.Count, CountInternalMap(ContainerCache));
            }
            else
                Utility.ConsoleWriteLine("Logic error: ContainerCache still contains {0} containers.", ConsoleColor.Red, ContainerCache.Count);

            TotalRespawn();

            return ContainerCache.Count;
        }
        public static List<BaseContainer> LoadContainerCacheInternal()
        {
            List<BaseContainer> list = new List<BaseContainer>();
            Console.Write("Loading dungeon chest locations...");
            foreach (Item item in World.Items.Values)
            {
                if (item is BaseContainer bc && bc.Movable == false && bc.Deleted == false)
                {
                    if (bc.Map == Map.Felucca)
                    {
                        Region rg = Region.Find(bc.Location, bc.Map);
                        if (rg != null && rg.IsDungeonRules)
                        {
                            // while we're here, remove deco attr
                            if (bc.Deco == true)
                                bc.Deco = false;                            // should no longer be set on dungeon containers

                            if (IsTargetTypeContainer(bc) || IsUnusualContainer(bc))
                            {
                                if (DungeonSpecial(bc))                     // make sure it's lockable and and not a Dungeon Treasure Chest
                                    continue;
                                else if (!IsUnusualContainer(bc))           // want all normal, locable containers
                                    list.Add(bc);
                                else if (IsUnusualContainer(bc))            // we want the swapped-out containers on our list as well
                                    list.Add(ExtractInternal(bc));          // We will check for being on the internal map elsewhere
                            }
                            else if (!DungeonSpecial(bc))
                            {   // maybe add these later
                                //Utility.PushColor(ConsoleColor.Red);
                                //Console.WriteLine("Interesting: We found a lockable container at {0}.", bc.Location);
                                //Utility.PopColor();
                            }
                        }
                    }
                }
            }

            return list;
        }
        private static void MiniDefrag(List<BaseContainer> list)
        {
            // packing...
            while (list.Contains(null))
                list.Remove(null);

            List<BaseContainer> deleteList = new List<BaseContainer>();
            foreach (BaseContainer bc in list)
                if (bc.Deleted)
                    deleteList.Add(bc);

            foreach (BaseContainer bc in deleteList)
                list.Remove(bc);
        }
        public static void Load()
        {
            string filename = "Saves/UnusualContainers.bin";
            if (File.Exists(filename) == false)
            {   // for an established shards, this error is worthwhile, however very uncommon.
                //  But for fresh shards (no world files,) we don't want to scare the shard owner.
                //Core.LoggerShortcuts.BootError(String.Format("Error reading \"{0}\", using default values:", filename));
                return;
            }
            Console.Write("Unusual Containers Loading... ");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                                UnusualCache.Add(reader.ReadItem() as BaseContainer);

                            MiniDefrag(UnusualCache);
                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid UnusualContainers.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.ConsoleWriteLine("\nError reading \"{0}\", using default values:", ConsoleColor.Red, filename);
            }

            Console.WriteLine("{0} Unusual Containers Loaded.", UnusualCache.Count);
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Saving {0} Unusual Containers ...", UnusualCache.Count);
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/UnusualContainers.bin", true);
                int version = 0;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 0:
                        {
                            writer.Write(UnusualCache.Count);
                            foreach (BaseContainer bc in UnusualCache)
                                writer.Write(bc);
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing UnusualContainers.bin");
                Console.WriteLine(ex.ToString());
            }
        }

        public static void Initialize()
        {
            Server.CommandSystem.Register("FindUnusuals", AccessLevel.Administrator, new CommandEventHandler(FindUnusuals_OnCommand));
        }
        [Usage("FindUnusuals")]
        [Description("Finds all the unusual containers.")]
        public static void FindUnusuals_OnCommand(CommandEventArgs e)
        {
            (e.Mobile as Mobiles.PlayerMobile).JumpIndex = 0;
            (e.Mobile as Mobiles.PlayerMobile).JumpList = new ArrayList();

            foreach (Item item in World.Items.Values)
            {
                if (item is LockableContainer lc)
                {
                    if (IsUnusualContainer(lc))
                    {
                        (e.Mobile as Mobiles.PlayerMobile).JumpList.Add(lc);
                    }
                }
            }

            if ((e.Mobile as Mobiles.PlayerMobile).JumpList.Count > 0)
                e.Mobile.SendMessage("Your jump list has been initialized.");
            else
                e.Mobile.SendMessage("There are no unusual containers.");

        }
    }
}