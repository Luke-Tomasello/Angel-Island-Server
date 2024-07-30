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

/* Scripts/Commands/FDFile.cs
 * Changelog
 *  5/5/2024, Adam (FDToIntMap, FDToWorld)
 *      Add FDToIntMap, and FDToWorld to move established setups from the world to int map storage and back again.
 *      This differs from FDBackup and FDRestore as it simply moves items to int map storage. This cannot be used to move items across shards
 *          as the serial numbers are not changed. This effectively allows you to archive one quest/event (christmas 2023 for example,) and instantiate a different one.
 *          These two functions make use of an FDRegion control to define:
 *          1. the rectangles to backup
 *          2. the map
 *          3. the name
 *  2/27/2024, Adam (MovingCrates)
 *      Disallow the capture of moving crates because of their special behaviors.
 *      MovingCrates auto delete when the last item us removed. Part of the [FDBackup create temp objects and adds/removes item from them.
 *      This behavior is problematic.
 *      Future possible solution: Just like Item has a Dupe override, we could add a DeepDupe override so that the chain of connected objects
 *          could be communicated back up to [FDBackup. 
 *          For now however, we will simply disallow the operation.
 *  2/12/2024, Adam: HOW THE SYSTM WORKS
 *  ------- Source Computer -------
 *  --- [FDBackup <name>
 *  --- Copy folder named <name>.fddb to destination computer (same folder from where it was copied.)
 *  ------- Destination Computer -------
 *  --- [FDRestore <name>
 *  * Copies items 'in whole', from one location to another, or more importantly, from one shard to another 
 *  * In Whole means that not only are the items copied, but any items they may contain (sub items), or reference (properties)
 *  1. Sub Items Example: a container that has an Enchanted Scroll in it.
 *  a. The Container 'contains' the scroll, so that is copied.
 *  2. Properties Example I:
 *  a. The scroll in the above example has a property called "Item" and it points to a weapon stashed on the internal map.
 *  aa. A copy is made of the weapon  on the internal map and relinked to the copied scroll, which is places in the copied container.
 *  2a. Properties Example II: in Example I, we made a copy of the weapon that resides on the internal map. In a second case, the 'linked'
 *      item may be part of the capture. Example Motion Controller 'links to' an Effect Controller. Both are part of the capture.
 *      In this case, both the Motion Controller and Effect Controller are copied and relinked with new serial numbers.
 *  2b. The difference, and an important aspect of the system, is that in Example I, the weapon was not part of the capture rectangle 
 *          (it was on the internal map.)
 *  3.  In Example II above, both the Motion Controller and Effect Controller were part of the capture rectangle. But what if the 
 *          Effect Controller is not part of the capture? The Motion Controller link to that object will be broken. (null)
 *  HOW THE SYSTM WORKS - INTERNALS
 *      * Old System: The old system, each shard maintained a notion of 'reserved serials'. The source shard would package up items 
 *          (not the deep copy described above, but only surface items and their subitems,) with these reserved serials.
 *          The *actual* items you are targeting on the source shard would have it's items reserialized. These items would then be serialized
 *          to disk to be read back by the destination shard. The problems here are two fold. First we are modifying our source shards 
 *          serial numbers. This is booth ugly and can cause problems for any linkages between items, or macros which may know of these serials.
 *          Secondly, the destination shard would have to be using the exact starting reserved serial. This was a major stumbling block each time
 *          we wished to do a restore. As you would imagine, the items restored on the destination would all be using these reserved serials.
 *      * New System: 
 *          1. We now share the Reserved Serial Database (one integer essentially, the starting point,) across all GMN shard.
 *          2. Reserved Serials are now temporal. That is, the only exist for the save and restore.
 *          2a. DEEP COPIES are made of the items to save on the source shard, so no *actual* in-use, items ever get changed.
 *          2b. DEEP COPIES are made of the restored items on the destination shard, so all restored items will be using that shard's natural
 *                  serializing scheme.
 *      * Magical Elements: DeepCopy, CopyItem and a family of Property functions make all this possible.
 *          There are also a handful of mapping data structures that handle for instance, linking the newly created maul to the newly created
 *              Enchanted Scroll, and placing the newly created Enchanted Scroll in the newly created container.
 *      * Take Aways: 
 *          1. Items in an Item's Items are always 'duped' and relinked
 *          2. Items referenced in an Item's properties that resides on the internal map is duped, and relinked with the new item
 *          3. Items referenced in an Item's properties that are part of the capture are duped and relinked.
 *          4. Items referenced in an Item's properties that are NOT part of the capture have their links set to null.
 *      * Notes: Addons are supported and collected and reinstantiated on the destination server as a collection of static tiles.
 *  2/12/2024, Adam: Refactor III
 *      1. We no longer care *directly* about reserved serials on the receiver side, for item construction.
 *      2. we still use reserved serials on the sender's side, bt they are used here for the FDLinkPatcher()
 *      3. we simply create a new Item on the receiver's side instead of using the whole reserved serials tap dance
 *      4. FDLinkPatcher: This is the guy that resolves item PROPERTIES that link to item. Example:
 *          music motion controller, all of the different controllers, and enchanted scrolls
 *  2/7/2024, Adam
 *      Refactor: II
 *          The old (original) model would reassign the serials of the 'in-world items' to serialize. This means that if
 *          you were to capture a treasure chest to freeze dry, the system would reassign the serial of the 
 *          treasure chest and everything within, THEN serialize that as the FDFile. (thus changing the current world.)
 *          This is both unnecessary and causes visual hiccups if the number of surface level items to capture is
 *          in the hundreds or thousands. (They are removed from view (remove packet,) while the serial is updated.)
 *          In this new model, we create duplicates of the items and sub items we wish serialize. Example:
 *          1. Target a RecallRune to freeze dry
 *          2. *Make a copy* of the RecallRune. Then ReassignSerial on that.
 *          3. Serialize the dupe
 *          4. delete the dupe
 *  2/6/2023 Adam
 *      1. Recast from an area Container capture and restore to an area Item capture and restore tool
 *      2. Allows for changing maps: backup in Felucca, restore in Trammel for example
 *      3. Future enhancement: hand back an array of the top level items so that their X/Ys may be adjusted.
 *  8/18/2023
 *      Refactor: I
 *      Purpose: To copy N containers from one shard/server/backup to another. Typically, this is a recovery operation
 *      Process: 
 *          1. To ensure the same 'reserved serial range' is available on both computers.
 *              This is done with the ReservedSerialConsole. if source and destination servers have different values for 
 *              FirstAvailableSerial, set both servers to the same value (Use the greater of the two.)
 *              Note: this model creates holes in our table. We have 0x10000000, or 268,435,456 available slots to play with
 *                  At some point, we will want some sort of smart allocator / defrag.
 *          2. Dump the containers to disk with DumpContArray. You will supply a root name for the array, "adam" for instance.
 *          3. Move the N .bin, .idx files to the destination computer LocalSerialHeap.BaseDirectory (where the program runs.)
 *          4. Load the containers using LoadContArray.
 *              Note: The containers will be loaded at the same location from which they were saved.
 *      Tips and tricks: 
 *          a. Sometimes I like to gather all the containers to restore, and place them in my house. This allows me control of the 
 *              restored containers.
 *             Other times, I want them in their starting locations. (Replacing a players stolen goods.)
 *              In this case, Be aware that if there are existing containers in the location (emptied by thieves,) you will have 
 *              duplicate containers there. One restored, and one likely empty.
 *              You can manually clean this up, or simply move/delete the empty containers before restoration.
 *	02/24/06 Taran Kain
 *		Initial version.
 */


// Ignore Spelling: Deserialize

using Server.Diagnostics;
using Server.Engines.ChampionSpawn;
using Server.Items;
using Server.Items.Triggers;
using Server.Mobiles;
using Server.Multis;
using Server.Multis.StaticHousing;
using Server.Regions;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static Server.Item;

namespace Server.Commands
{
    #region Reserved Serial Manager
    public class ReservedSerialManager
    {
        private const int FirstReservedSerial = 0x60000000;
        private const int LastReservedSerial = 0x70000000;
        private List<int> IntHeap = new();
        private int m_index = 0;
        public int FindBlock(int amount, ref string reason)
        {
            try
            {
                if (amount == 0)
                {
                    reason = string.Format("Usage: FindBlock <number>");
                    return -1;
                }
                int first = ReservedSerialManager.FirstReservedSerial;
                int last = ReservedSerialManager.LastReservedSerial;
                int count = 0;
                int alloc_start = 0;
                for (int ix = first; ix < last; ix++)
                {
                    if (World.FindItem(ix) != null)
                    {   // something here, keep looking
                        count = 0;
                        alloc_start = 0;
                    }
                    else
                    {
                        if (alloc_start == 0)
                            alloc_start = ix;

                        if (++count == amount)
                        {
                            reason = string.Format("FindBlock: Success. {0} Serials available at {1:X}", amount, alloc_start);
                            return alloc_start;
                        }
                    }
                }

                reason = string.Format("FindBlock:  Failed to find a block of size {0}", amount);

            }
            catch
            {
                reason = string.Format("Usage: FindBlock <number>");
                return -1;
            }

            return -1;
        }
        public bool VerifyBlock(int amount, int starting_serial, ref string reason)
        {
            try
            {
                if (amount == 0)
                {
                    reason = string.Format("Usage: VerifyBlock <number>");
                    return false;
                }
                int first = starting_serial;
                int last = ReservedSerialManager.LastReservedSerial;
                for (int ix = first; ix < last; ix++)
                {
                    if (World.FindItem(ix) != null)
                    {   // something here, fail
                        reason = string.Format("VerifyBlock:  Failed to find a block of size {0} at {1:X}", amount, ix);
                        return false;
                    }
                }

                reason = string.Format("VerifyBlock: Success. {0} Serials available at {1:X}", amount, starting_serial);
            }
            catch
            {
                reason = string.Format("Usage: FindBlock <number>");
                return false;
            }

            return false;
        }
        public bool Verify(List<int> list, ref string reason)
        {
            try
            {
                if (list == null || list.Count == 0)
                {
                    reason = string.Format("Usage: Verify <number>");
                    return false;
                }
                foreach (var serial in list)
                {
                    if (World.FindItem(serial) != null)
                    {   // something here, fail
                        reason = string.Format("Verify:  Failed to find {0} serials at {1:X}", list.Count, serial);
                        return false;
                    }
                }

                reason = string.Format("Verify: Success. {0} Serials available in range {1:X} - {2:X}", list.Count, list[0], list.Last());
            }
            catch
            {
                reason = string.Format("Usage: FindBlock <number>");
                return false;
            }

            return false;
        }
        public bool AllocateBlock(int amount, int starting_serial, ref string reason)
        {
            try
            {
                if (amount == 0)
                {
                    reason = string.Format("Usage: AllocateBlock <number>");
                    return false;
                }
                int first = starting_serial;
                int last = ReservedSerialManager.LastReservedSerial;
                for (int ix = first; ix < last; ix++)
                {
                    if (World.FindItem(ix) != null)
                    {   // something here, fail
                        reason = string.Format("AllocateBlock:  Failed to find a block of size {0} at {1:X}", amount, ix);
                        return false;
                    }
                    else
                        IntHeap.Add(ix);
                }

                reason = string.Format("AllocateBlock: Success. {0} Serials allocated at {1:X}", amount, starting_serial);
            }
            catch
            {
                reason = string.Format("Usage: FindBlock <number>");
                return false;
            }

            return false;
        }
        public bool Allocate(int amount, ref string reason)
        {
            try
            {
                if (amount == 0)
                {
                    reason = string.Format("Usage: AllocateBlock <number>");
                    return false;
                }
                int first = ReservedSerialManager.FirstReservedSerial;
                int last = ReservedSerialManager.LastReservedSerial;
                for (int ix = first; ix < last; ix++)
                {
                    if (World.FindItem(ix) != null)
                    {   // something here, skip it
                        continue;
                    }
                    else
                    {
                        IntHeap.Add(ix);
                        if (IntHeap.Count == amount)
                        {
                            reason = string.Format("Allocate: Success. {0} Serials allocated at {1:X}", amount, IntHeap[0]);
                            return true;
                        }
                    }
                }

                reason = string.Format("Allocate: Fail. Unable to allocate {0} Serials", amount);
                return false;

            }
            catch
            {
                reason = string.Format("Usage: Allocate <number>");
                return false;
            }

            return false;
        }
        public bool AllocateThis(int serial, ref string reason)
        {
            if (World.FindItem(serial) != null)
            {   // something here, fail
                reason = string.Format("AllocateThis: Fail. Serial {0:X} unavailable", serial);
                return false;
            }
            else
            {
                reason = string.Format("AllocateThis: Success. Serial {0:X} allocated", serial);
                IntHeap.Add(serial);
                return true;
            }
        }
        public int Floor => FirstReservedSerial;
        public ReservedSerialManager()
        {
            ;
        }
        public void Initialize()
        {
            Index = 0;
            IntHeap = new();
        }
        public int Pointer
        {
            get { return IntHeap[m_index]; }
        }
        public int Index
        {
            get { return m_index; }
            set { m_index = value; }
        }
        public int Count
        {
            get { return IntHeap.Count; }
        }
    }
    #endregion Reserved Serial Manager

    public class FDFile
    {
        private static ReservedSerialManager LocalSerialHeap = new();
        private const bool Verbose = false;
        private const bool StaticAddon = false;

        #region Commands
        public static void Initialize()
        {
#if DEBUG
            CommandSystem.Register("FDBackup", AccessLevel.Administrator, new CommandEventHandler(OnFDDumpObjArray));
            CommandSystem.Register("FDRestore", AccessLevel.Administrator, new CommandEventHandler(OnFDLoadObjArray));

            CommandSystem.Register("FDFormat", AccessLevel.Administrator, new CommandEventHandler(OnFDFormat));
            CommandSystem.Register("FDSpawner", AccessLevel.Administrator, new CommandEventHandler(OnFDSpawner));

            CommandSystem.Register("FDArchive", AccessLevel.Administrator, new CommandEventHandler(OnFDArchive));
            CommandSystem.Register("FDUnArchive", AccessLevel.Administrator, new CommandEventHandler(OnFDUnArchive));

            Server.Commands.TargetCommands.Register(new FDSaveMobileCommand());
            Server.Commands.TargetCommands.Register(new FDRestoreMobileCommand());
#else
            CommandSystem.Register("FDBackup", AccessLevel.Owner, new CommandEventHandler(OnFDDumpObjArray));
            CommandSystem.Register("FDRestore", AccessLevel.Owner, new CommandEventHandler(OnFDLoadObjArray));
            
            CommandSystem.Register("FDFormat", AccessLevel.Owner, new CommandEventHandler(OnFDFormat));
            CommandSystem.Register("FDSpawner", AccessLevel.Owner, new CommandEventHandler(OnFDSpawner));

            CommandSystem.Register("FDArchive", AccessLevel.Owner, new CommandEventHandler(OnFDArchive));
            CommandSystem.Register("FDUnArchive", AccessLevel.Owner, new CommandEventHandler(OnFDUnArchive));

            Server.Commands.TargetCommands.Register(new FDSaveMobileCommand());
            Server.Commands.TargetCommands.Register(new FDRestoreMobileCommand());
#endif
        }
        #endregion Commands

        #region Dump Object Array
        public static void OnFDDumpObjArray(CommandEventArgs e)
        {
            LocalSerialHeap.Initialize();

            if (!(e.Arguments.Length != 1 || e.Arguments.Length != 2 /*name + radius*/))
            {
                e.Mobile.SendMessage("Usage: [FDBackup <filename> [radius]");
                return;
            }
            if (e.Arguments.Length == 1)
            {
                e.Mobile.SendMessage("Select the object area to dump...");
                BoundingBoxPicker.Begin(e.Mobile, new BoundingBoxCallback(DumpArray_Callback), e.GetString(0));
            }
            else
            {   // we got a rect
                string name = e.GetString(0);
                Point3D start = new Point3D();
                Point3D end = new Point3D();
                try
                {
                    int radius = 0;
                    radius = int.Parse(e.GetString(1));
                    string sstart = string.Format("{0} {1} {2}",
                        e.Mobile.Location.X - radius, e.Mobile.Location.Y - radius,
                        e.Mobile.Location.Z);
                    string send = string.Format("{0} {1} {2}",
                        e.Mobile.Location.X + radius, e.Mobile.Location.Y + radius,
                        e.Mobile.Location.Z);
                    start = Point3D.Parse(sstart);
                    end = Point3D.Parse(send);
                }
                catch
                {
                    e.Mobile.SendMessage("Usage: [FDFileArray <filename> [radius]");
                    return;
                }
                DumpArray_Callback(e.Mobile, e.Mobile.Map, start, end, name);
            }
        }
        #region FDDumpObjArray Support
        public class ParamPack
        {
            public Dictionary<Item, Item> LinkMapper;
            public ItemDatabase ItemDatabase;
            public List<Item> AddonHandled;
            public Dictionary<int, List<AddonFields>> AddonDatabase;
            public List<Item> TempAddonComponents;
            public Dictionary<Serial, int> SpawnerLinkMapper;
            public ParamPack(
                ref Dictionary<Item, Item> LinkMapper,
                ref ItemDatabase ItemDatabase,
                ref List<Item> AddonHandled,
                ref Dictionary<int, List<AddonFields>> AddonDatabase,
                ref List<Item> TempAddonComponents,
                ref Dictionary<Serial, int> SpawnerLinkMapper
                )
            {
                this.LinkMapper = LinkMapper;
                this.ItemDatabase = ItemDatabase;
                this.AddonHandled = AddonHandled;
                this.AddonDatabase = AddonDatabase;
                this.TempAddonComponents = TempAddonComponents;
                this.SpawnerLinkMapper = SpawnerLinkMapper;
            }
        }
        private static FDRegionControl CheckFDRegionControl(Map map, Point3D start, Point3D end)
        {
            if (start != end)
                return null;

            Rectangle2D peek_rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);

            IPooledEnumerable eable = map.GetItemsInBounds(peek_rect);
            foreach (object obj in eable)
            {
                if (obj is FDRegionControl)
                {
                    eable.Free();
                    return obj as FDRegionControl;
                }
            }
            eable.Free();

            return null;
        }
        private static List<Rectangle2D> GetFDRegionControlRects(FDRegionControl fdr)
        {
            List<Rectangle2D> rects = new();
            foreach (var rect in fdr.CustomRegion.Coords)
                rects.Add(new Rectangle2D(rect.Start, rect.End));

            return rects;
        }
        private static bool IsCamp(Item item)
        {
            if (item == null) return false;
            if (item.BaseCamp is BaseCamp)
                return true;
            else return false;
        }
        private static bool IsSpawned(Item item)
        {
            if (item == null) return false;
            if (item.Spawner is Spawner)
                return true;
            else return false;
        }
        private static void UnpackMobile(Mobile template, out Backpack backpack, out EquipmentPack equipmentPack)
        {
            backpack = equipmentPack = null;
            if (template.Backpack != null)
            {
                backpack = (Backpack)Utility.DeepDupe(template.Backpack);
            }

            if (Utility.HasLayers(template) > 0)
                equipmentPack = Utility.DupeLayers(template);
        }
        #endregion FDDumpObjArray Support
        private static void DumpArray_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            string filename = state as string;
            List<Rectangle2D> rects;

            #region Rects to process
            // Create rec and retrieve items within from bounding box callback result
            Rectangle2D starting_rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);
            FDRegionControl fdr;
            if ((fdr = CheckFDRegionControl(map, start, end)) != null)
                rects = GetFDRegionControlRects(fdr);
            else
                rects = new List<Rectangle2D>() { starting_rect };
            #endregion Rects to process

            // locals
            List<Item> AddonHandled = new List<Item>();         // seen it, handled it
            List<Item> TempAddonComponents = new List<Item>();  // all components in an addon
            ItemDatabase ItemDatabase = new();                  // where we store all duped items and their reassignments
            List<Item> AllReserializedItems = new();            // condensed list to reassigned items
            Dictionary<Item, Item> LinkMapper = new();          // old item to new item
            List<int> RollbackSerials = new();                  // probably a bad name now. Holds all reserved serials we will be using
            Dictionary<int, List<AddonFields>> AddonDatabase = new();
            Dictionary<Serial, int> SpawnerLinkMapper = new();  // old spawner to template restoration code
            List<ChampEngine> ChampEngineList = new();
            try
            {
                FDBackupInProgress = true;                      // tell the world a backup is in progress
                #region Cleanup any existing save
                {
                    string lookup = FDFileNames(FDNameType.Lookup, base_name: filename);
                    string rollback = FDFileNames(FDNameType.Rollback, base_name: filename);
                    string addons = FDFileNames(FDNameType.Addons, base_name: filename);
                    string path = FDFileNames(FDNameType.BaseFolder, filename);
                    string itemsPattern = FDFileNames(FDNameType.ItemsPattern, base_name: filename);
                    string mobilesPattern = FDFileNames(FDNameType.MobileDefinitionPattern, base_name: filename);
                    string trace = FDFileNames(FDNameType.BackupTrace, base_name: filename);
                    string formatError = FDFileNames(FDNameType.FormatError, base_name: filename);
                    string unavailableError = FDFileNames(FDNameType.RestoreObjectUnavailable, base_name: filename);
                    List<string> filenames = new List<string>();
                    foreach (string fn in Directory.GetFiles(path, itemsPattern))
                    {
                        if (!filenames.Contains(fn))
                            filenames.Add(fn);
                    }
                    foreach (string fn in Directory.GetFiles(path, mobilesPattern))
                    {
                        if (!filenames.Contains(fn))
                            filenames.Add(fn);
                    }
                    if (File.Exists(lookup))
                        File.Delete(lookup);
                    if (File.Exists(rollback))
                        File.Delete(rollback);
                    if (File.Exists(addons))
                        File.Delete(addons);
                    foreach (string fn in filenames)
                        File.Delete(fn);
                    if (File.Exists(trace))
                        File.Delete(trace);
                    if (File.Exists(formatError))
                        File.Delete(formatError);
                    if (File.Exists(unavailableError))
                        File.Delete(unavailableError);
                }
                #endregion Cleanup any existing save

                #region See what we got
                List<Item> WhatWeGot = new();                   // surface items we found
                #region WhatWeGot
                Dictionary<Item, byte> QuickTable = new();

                #region Filter Champ Engines
                foreach (var rect in rects)
                {   // we need to do this before we collect the items to process since we will eliminate the champ altar and platform here
                    IPooledEnumerable eable = map.GetItemsInBounds(rect);
                    foreach (object obj in eable)
                        if (obj is ChampEngine ce)
                            ChampEngineList.Add(ce);
                    eable.Free();

                    foreach (ChampEngine ce in ChampEngineList)
                    {   // turn off graphics. These will be restored after backup completes
                        ce.RestoreGFX = ce.ChampGFX;
                        ce.ChampGFX = ChampGFX.None;
                    }
                }
                #endregion Filter Champ Engines

                foreach (var rect in rects)
                {
                    Server.Gumps.EditAreaGump.FlashArea(from, rect, from.Map);
                    IPooledEnumerable eable = map.GetItemsInBounds(rect);
                    foreach (object obj in eable)
                    {
                        if (CheckedFiltered(obj))
                            continue;

                        if (obj is Item old_item)
                        {                                               // respect the IgnoreBefore date in the FDRegionControl
                            if (fdr != null && old_item.Created < fdr.IgnoreBefore)
                                continue;
                            else if (old_item is FDRegionControl)       // ignore FDRegionControls
                                continue;
                            else if (old_item is FDRegionMarker)        // ignore FDRegionMarkers
                                continue;
                            else if (QuickTable.ContainsKey(old_item))  // since our regions may overlap, we ignore duplicate items
                                continue;
                            else if (IsCamp(old_item))                  // camps are spawned. Don't collect spawned items
                                continue;
                            else if (IsSpawned(old_item))               // Don't collect spawned items
                                continue;
                            else
                            {
                                QuickTable.Add(old_item, 0);
                            }
                        }
                    }
                    eable.Free();
                }
                WhatWeGot.AddRange(QuickTable.Keys);
                QuickTable.Clear();
                #endregion WhatWeGot
                #endregion See what we got

                #region Verify Duplicates
                System.Diagnostics.Debug.Assert(WhatWeGot.Count() - WhatWeGot.Distinct().Count() == 0);
                #endregion Verify Duplicates

                #region Disallow moving crates
                {   // see comments at the top of this file. Search "(movingcrates)"
                    foreach (Item item in WhatWeGot)
                        if (item is MovingCrate)
                        {
                            from.SendMessage("Unable to [FDBackup moving crates at this time.");
                            from.SendMessage("See comments at the top of FDFile.cs. Search \"(movingcrates)\".");
                            from.SendMessage("Use [ConvertMCToStandard to convert moving crates before [FDBackup.");
                            return;
                        }
                }
                #endregion Disallow moving crates

                #region Ignore these items
                {   // sometimes when capturing we may pick up the house we are in, etc. ignore it
                    List<Item> illegal = new();
                    foreach (Item item in WhatWeGot)
                        if (item is BaseHouse || item is StaticHouseHelper.FixerAddon)
                        {
                            illegal.Add(item);
                            break;
                        }
                    foreach (Item item in illegal)
                        WhatWeGot.Remove(item);
                }
                #endregion Ignore these items

                #region Preprocess Spawner Template Mobiles
                {
                    List<Spawner> mob_temp_spawner = new();
                    foreach (Item item in WhatWeGot)
                        if (item is Spawner spawner && spawner.TemplateMobile is Mobile m && !m.Deleted)
                            mob_temp_spawner.Add(item as Spawner);

                    foreach (Spawner spawner in mob_temp_spawner)
                    {
                        Mobile template = spawner.TemplateMobile;

                        #region get the mobiles backpack an all equipped items
                        Backpack backpack;
                        EquipmentPack equipmentPack;
                        UnpackMobile(template, out backpack, out equipmentPack);
                        #endregion get the mobiles backpack an all equipped items

                        #region create a unique code for reconstructing this template mobile on the destination
                        int code = Utility.GetGuidHandle();
                        LinkPack linkPack = new LinkPack();
                        linkPack.Link = code;                   // link the two
                        // when we dupe the new spawner, we will punch this code into the spawner
                        //  when the spawner loads up on the new server, it will reconstruct a complete template mobile from it
                        SpawnerLinkMapper.Add(spawner.Serial, code);
                        #endregion create a unique code for reconstructing this template mobile on the destination

                        #region Add the items from the template mobile to our repository if 'items we've found'
                        if (backpack != null)
                            linkPack.AddItem(backpack);
                        if (equipmentPack != null)
                            linkPack.AddItem(equipmentPack);

                        WhatWeGot.Add(linkPack);
                        #endregion Add the items from the template mobile to our repository if 'items we've found'

                        #region Compile our mobile and create set of keyword value pares to write
                        string compiled_mobile_props = Utility.CompileMobileProperties(template);
                        string compiled_mobile_skills = Utility.CompileMobileSkills(template);
                        string type_name = template.GetType().FullName;
                        Dictionary<string, string> keyValuePairs = new Dictionary<string, string>()
                        {

                            {"code", code.ToString() },
                            {"type_name", type_name },
                            {"compiled_mobile_props", compiled_mobile_props },
                            {"compiled_mobile_skills", compiled_mobile_skills },
                        };
                        #endregion Compile our mobile and create set of keyword value pares to write

                        #region Write to our mobile reconstruction database
                        string output_name = string.Format("{0}", Utility.GuidHandleToFileName(code));
                        FDMobileDefinitionWriter(filename, output_name, keyValuePairs);
                        #endregion  Write to our mobile reconstruction database
                    }
                }
                #endregion Preprocess Spawner Template Mobiles

                #region Anything to do? 
                if (WhatWeGot.Count == 0)
                {
                    from.SendMessage("Nothing to backup.");
                    return;
                }
                #endregion Anything to do? 

                #region Mutate items (currently only BaseAddon)
                {
                    // unlike surface items that are on the map, yet outside of the capture rect, internal items that are linked
                    //  by a captured item are treated as surface items and are included here.
                    List<Item> toMutate = new();
                    foreach (Item item in WhatWeGot)
                    {
                        List<Item> list = Utility.GetInternalItems(item);
                        if (list.Count > 0)
                            // we currently only support one such linked item (TODO)
                            toMutate.Add(list[0]);
                    }
                    foreach (Item item in toMutate)
                    {
                        if (item is BaseAddon ba)
                        {
                            //System.Diagnostics.Debug.Assert(item.GetItemBool(ItemBoolTable.BSPackOnly));

                            BaseAddonDeed deed = ba.Deed;
                            System.Diagnostics.Debug.Assert(deed != null);
                            bool found = false;
                            foreach (Item check_item in WhatWeGot)
                            {
                                PropertyInfo prop = FindInternalItemProperty(check_item, item);
                                if (prop != null)
                                {
                                    prop.SetValue(check_item, deed);
                                    found = true;
                                    break;
                                }
                            }
                            System.Diagnostics.Debug.Assert(found);

                            WhatWeGot.Remove(item);
                            WhatWeGot.Add(deed);
                        }
                    }
                }
                #endregion Mutate items (currently only BaseAddon)

                #region Sort on Serial                          
                // Z order is dependent on serial, lowest to highest.
                // we do this since we need to restore items in the correct z order
                WhatWeGot = WhatWeGot.OrderBy(o => o.Serial).ToList();
                #endregion Sort on Serial                                   

                #region Process and record the item
                ParamPack @params = new ParamPack(
                    ref LinkMapper,
                    ref ItemDatabase,
                    ref AddonHandled,
                    ref AddonDatabase,
                    ref TempAddonComponents,
                    ref SpawnerLinkMapper
                );
                foreach (Item item in WhatWeGot)                // recursively process and record each item
                {
                    Item old_item = item;
                    System.Diagnostics.Debug.Assert(
                        ProcessItem(old_item, @params));
                }
                #endregion Process and record the item

                #region Initialize
                string reason = string.Empty;
                bool result = false;
                #endregion Initialize

                /* Okay, this is what we have to work with
                 * 1. surface items
                 * 2. referenced items that reside on the internal map
                 * 3. links to/from both surface items and those in the internal map and in the Items of an item
                 */
                int Amount = ItemDatabase.GetDupedItems().Count;

                #region Set Backing up flag
                {   // some items like the MovingCrate have different behaviors if they are being backup/restored vs normal use.
                    //  For example MovingCrates delete themselves when the last item is removed. However removing the items and reading them is part of [FDRestore
                    // Because of this special behavior, MovingCrate needs to know not to delete itself during [FDRestore
                    foreach (Item item in ItemDatabase.GetDupedItems())
                        item.SetItemBool(ItemBoolTable.BSBackedUp, true);
                }
                #endregion Set Backing up flag

                #region Allocate the serials we will need for this session      
                result = LocalSerialHeap.Allocate(amount: Amount, ref reason);
                from.SendMessage("{0}", reason);
                if (!result)
                {
                    System.Diagnostics.Debug.Assert(false);
                    return;
                }
                #endregion Allocate the serials we will need for this session

                #region Dump a rollback database of the serials we will use
                RollbackSerials = FDCreateRollback(amount: Amount, filename, ref reason);
                from.SendMessage("{0}", reason);
                System.Diagnostics.Debug.Assert(RollbackSerials != null);
                if (RollbackSerials == null)
                    return;
                else
                    Utility.ConsoleWriteLine("DumpArray_Callback: saving at {0:X}", ConsoleColor.DarkCyan, RollbackSerials[0]);
                #endregion Dump a rollback database of the serials we will use

                #region  FDReassign old item serial to new item serial (reserved serial)
                {
                    FDBackupTraceWriter(filename, "** Backup Trace map: Original serial to new serial **", begin: true);

                    // reassign serials (frees the reserved)
                    // reassigns all duped items in the database and any in the Item list of the item (not in the database)
                    List<Item> dupedItems = ItemDatabase.GetDupedItems();
                    dupedItems = dupedItems.OrderBy(o => o.Serial).ToList();

                    // rewind our heap so that we are pointing at the beginning of reserved serials
                    LocalSerialHeap.Index = 0;
                    foreach (Item dupe in dupedItems)
                    {
                        List<Item> new_serials = new();
                        Item original = null;
                        #region Get the original item based on the dupe
                        // There are three stages in backup:
                        //  1. The original item to back up
                        //  2. the duplicate
                        //  3. the duplicate after reserialization. We want 1 & 3
                        var key = LinkMapper.Where(kvp => kvp.Value == dupe).Select(kvp => kvp.Key).FirstOrDefault();
                        if (key != null)
                            original = key;

                        #endregion Get the original item based on the dupe
                        string before = FormatItem(original, "Error, cannot find original");
                        result = FDReassign(dupe, ref reason, new_serials);
                        if (!result)
                        {
                            from.SendMessage("{0}", reason);
                            System.Diagnostics.Debug.Assert(false);
                            FDRollback(RollbackSerials);
                            return;
                        }
                        else if (Verbose)
                            from.SendMessage("{0}", reason);

                        string after = FormatItem(dupe, "Error, cannot find dupe");
                        FDBackupTraceWriter(filename, string.Format($"{before}, {after}"), begin: false);
                        System.Diagnostics.Debug.Assert(new_serials.Count == 1);
                        ItemDatabase.AddReserializedItem(root_item: dupe, reserialized_items: new_serials);
                    }
                }
                #endregion  FDReassign old item serial to new item serial (reserved serial)

                #region Patch linkMapper for internal items
                try
                {
                    /*
                     * Because Dupe() calls the items's Dupe override, and because items stored on the internal map always have this override,
                     * Our linkMapper will have a stale link for the item linked on the internal map.
                     * The following code removes the stale link, and replaces it with a fresh one.
                     */
                    List<Item> toRemove = new();
                    List<KeyValuePair<Item, Item>> toAdd = new();
                    int missing_table_entries = 0;
                    foreach (var kvp in LinkMapper)
                    {   // we only care about those items that stash stuff on the internal map
                        if (Utility.HasInternalItems(kvp.Value))
                        {   // [0] because we only support one such link atm (TODO)
                            System.Diagnostics.Debug.Assert(Utility.GetInternalItems(kvp.Key) != null);
                            System.Diagnostics.Debug.Assert(Utility.GetInternalItems(kvp.Value) != null);
                            System.Diagnostics.Debug.Assert(Utility.GetInternalItems(kvp.Key)[0] != null);
                            System.Diagnostics.Debug.Assert(Utility.GetInternalItems(kvp.Value)[0] != null);
                            toRemove.Add(Utility.GetInternalItems(kvp.Key)[0]);
                            if (!LinkMapper.ContainsKey(Utility.GetInternalItems(kvp.Key)[0]))
                                // look at FetchInternalItems() to make sure we aren't missing any internal items
                                System.Diagnostics.Debug.Assert(++missing_table_entries == 0); // why?
                            else
                                toAdd.Add(new KeyValuePair<Item, Item>(Utility.GetInternalItems(kvp.Value)[0], LinkMapper[Utility.GetInternalItems(kvp.Key)[0]]));
                        }
                    }

                    // cleanup link mapper database
                    foreach (Item item in toRemove)
                        LinkMapper.Remove(item);

                    foreach (var kvp in toAdd)
                        if (!LinkMapper.ContainsKey(kvp.Key))
                            LinkMapper.Add(kvp.Key, kvp.Value);
                        else
                            ;// probably ok. multiple template spawners that link to the same internal item will generate duplicate keys
                }
                catch
                {
                    ;
                }
                #endregion Patch linkMapper for internal items

                #region Create / Write the 'Fixup' table (fixes references to Property and Item table entries)
                // fixup any Item Properties we can. Leave those we don't understand
                //  eg. if an item has a link to another item we are capturing, remap it here
                Dictionary<int, Dictionary<string, int>> PropertyLookup = FDLinkMapper(LinkMapper);

                // now write the fixup table.
                //  We need a fixup table to patch Properties of the items when they are read back
                //      on the receiver-end because there is no other database to consult
                FDPropertyLookupWriter(filename, PropertyLookup);
                #endregion Create / Write the 'Fixup' table  (fixes references to Property and Item table entries)

                #region Dump our addon database
                {   // first we need to patch old serial with new
                    List<int> toRemove = new();
                    Dictionary<int, List<AddonFields>> toAdd = new();
                    bool inWorld = false;
                    foreach (var kvp in AddonDatabase)
                        if (LinkMapper.ContainsKey(World.FindItem(kvp.Key)))
                        {
                            inWorld = true;
                            if (LinkMapper.ContainsKey(World.FindItem(kvp.Key)))
                            {
                                toRemove.Add(kvp.Key);
                                toAdd.Add(LinkMapper[World.FindItem(kvp.Key)].Serial, kvp.Value);
                            }
                            else
                                System.Diagnostics.Debug.Assert(false);
                        }
                        else
                        {
                            toRemove.Add(kvp.Key);
                            toAdd.Add(LinkMapper[World.FindItem(kvp.Key)].Serial, kvp.Value);
                        }


                    foreach (var index in toRemove)
                        AddonDatabase.Remove(index);

                    foreach (var kvp in toAdd)
                        AddonDatabase.Add(kvp.Key, kvp.Value);

                    FDAddonWriter(filename, AddonDatabase);
                }
                #endregion Dump our addon database

                #region Build our complete list of items to dump
                /* 
                 * GetPropertyItems: patched property 'link items'
                 * GetReserializedItems: duplicates of surface items (some may be on the internal map)
                 * GetPropertyItems and GetReserializedItems may overlap, so we're careful not to 
                 *  duplicate them.
                 */
                AllReserializedItems = GetPropertyItems(PropertyLookup);
                AllReserializedItems.AddRange(ItemDatabase.GetReserializedItems().Where(p => AllReserializedItems.All(p2 => p2 != p)));
                AllReserializedItems = AllReserializedItems.OrderBy(o => o.Serial).ToList();
                #endregion Build our complete list of items to dump

                #region DEBUG
#if DEBUG
                foreach (Item item in AllReserializedItems)
                {
                    System.Diagnostics.Debug.Assert(item != null);
                    System.Diagnostics.Debug.Assert(item.Serial >= LocalSerialHeap.Floor);
                    System.Diagnostics.Debug.Assert(RollbackSerials.Contains(item.Serial));
                }
#endif
                #endregion DEBUG

                #region Dump all records to our database 
                {
                    string path = FDFileNames(FDNameType.BaseFolder, filename);
                    // now dump
                    //Parallel.For(0, AllReserializedItems.Count,
                    //       index =>
                    //       {
                    //           string output_name = string.Format("{0}{1}_Item{2}", path, filename, index);
                    //           DumpObject(from, output_name, AllReserializedItems[index]);
                    //       });

                    System.Diagnostics.Debug.Assert(AllReserializedItems.Count() - AllReserializedItems.Distinct().Count() == 0);
                    for (int ix = 0; ix < AllReserializedItems.Count; ix++)
                    {
                        string output_name = string.Format("{0}{1}_Item{2}", path, filename, ix);
                        DumpObject(from, output_name, AllReserializedItems[ix]);
                    }
                }
                #endregion Dump all records to our database 

            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                // sanity - make sure all serials have been instantiated
                foreach (var serial in RollbackSerials)
                    System.Diagnostics.Debug.Assert(World.FindItem(serial) != null);

                // clean up the temp objects
                foreach (var o in AllReserializedItems)
                    if (o is Item item)
                    {
                        if (!item.Deleted)
                            item.Delete();
                        else
                            ; /* debug, but this is often okay if say, we previously deleted a container
                                the contained items would have also been deleted */
                    }

                foreach (var o in TempAddonComponents)
                    if (o is Item item)
                    {
                        if (!item.Deleted)
                            item.Delete();
                        else
                            System.Diagnostics.Debug.Assert(false);
                    }

                foreach (ChampEngine ce in ChampEngineList)
                {   // turn on graphics. We turn them off during backup because backing up the graphics is a PITA.
                    //  easier to just flip off/on
                    ce.ChampGFX = ce.RestoreGFX;
                    ce.RestoreGFX = ChampGFX.None;
                }

                FDBackupInProgress = false;
            }
            from.SendMessage("{0} objects dumped.", AllReserializedItems.Count);
        }
        public static string FormatItem(Item item, string fail_message)
        {
            if (item == null)
                return fail_message;
            return String.Format("0x{0:X} \"{1}\"", item.Serial.Value, item.GetType().Name);
        }
        public static bool FDBackupInProgress = false;
        private static PropertyInfo FindInternalItemProperty(Item item, Item item_to_find, bool null_map = false)
        {
            List<Item> elements = new();

            // we will ignore Item properties and only focus in the derived class properties.
            Item linked_item = null;
            PropertyInfo[] props = Utility.ItemRWPropertyProperties(item, IsAssignableTo: typeof(Item));
            for (int i = 0; i < props.Length; i++)
                if ((linked_item = (Item)props[i].GetValue(item, null)) != null)
                    if ((!null_map && linked_item.Map == Map.Internal) || (null_map && linked_item.Map == null))
                        if (linked_item == item_to_find)
                            return props[i];

            return null;
        }

        public class ParentChildren
        {
            private Item m_parent;
            public Item Parent { get { return m_parent; } set { m_parent = value; } }
            List<Item> m_children = new();
            public List<Item> Children { get { return m_children; } }
            public ParentChildren(Item item)
            {
                m_parent = item;
                m_children = new();
            }
            public ParentChildren()
            { m_children = new(); }
        }
        //-----------------------------------------duped(par, child),--Reserialized(par, child)
        public class ItemDatabase : List<KeyValuePair<ParentChildren, ParentChildren>>
        {
            public enum Select
            {
                Dupes,
                Reserialized
            }
            private KeyValuePair<ParentChildren, ParentChildren>? FindRecord(Item root_item, Select mode)
            {
                if (mode == Select.Dupes)
                {
                    foreach (var temp in this)
                        if (temp.Key.Parent == root_item)
                            return temp;
                }
                else
                {
                    foreach (var temp in this)
                        if (temp.Value.Parent == root_item)
                            return temp;
                }

                return null;
            }
            public void AddNewDupeRecord(Item root_item)
            {
                KeyValuePair<ParentChildren, ParentChildren>? record;
                if ((record = FindRecord(root_item, Select.Dupes)) != null)
                {   // key already exists in the database, update it
                    record.Value.Value.Children.Add(root_item);
                    return;
                }

                base.Add(
                    new KeyValuePair<ParentChildren, ParentChildren>(
                    new ParentChildren(root_item),  // dupes
                    new ParentChildren())           // reassignments
                    );
            }
            public void AddNewDupeRecord(Item root_item, Item duped_item)
            {
                KeyValuePair<ParentChildren, ParentChildren>? record;
                if ((record = FindRecord(root_item, Select.Dupes)) == null)
                    AddNewDupeRecord(root_item);

                record = FindRecord(root_item, Select.Dupes);
                record.Value.Key.Children.Add(duped_item);
            }
            public void AddDupedItem(Item root_item, Item duped_item)
            {
                KeyValuePair<ParentChildren, ParentChildren>? record;
                if ((record = FindRecord(root_item, Select.Dupes)) == null)
                {   // key does not exist in the database, add it
                    AddNewDupeRecord(root_item, duped_item);
                    return;
                }

                record.Value.Key.Children.Add(duped_item);
            }
            public void AddNewReserializedRecord(Item root_item)
            {
                KeyValuePair<ParentChildren, ParentChildren>? record;
                if ((record = FindRecord(root_item, Select.Reserialized)) != null)
                {   // key already exists in the database, update it
                    record.Value.Value.Children.Add(root_item);
                    return;
                }

                base.Add(
                    new KeyValuePair<ParentChildren, ParentChildren>(
                    new ParentChildren(),            // dupes
                    new ParentChildren(root_item))  // reassignments
                    );
            }
            public void AddNewReserializedRecord(Item root_item, Item reserialized_item)
            {
                KeyValuePair<ParentChildren, ParentChildren>? record;
                if ((record = FindRecord(root_item, Select.Reserialized)) == null)
                    AddNewReserializedRecord(root_item);

                record = FindRecord(root_item, Select.Reserialized);
                record.Value.Value.Children.Add(reserialized_item);
            }
            public void AddReserializedItem(Item root_item, Item reserialized_item)
            {
                KeyValuePair<ParentChildren, ParentChildren>? record;
                if ((record = FindRecord(root_item, Select.Reserialized)) == null)
                {   // key does not exist in the database, add it
                    AddNewReserializedRecord(root_item, reserialized_item);
                    return;
                }

                record.Value.Value.Children.Add(reserialized_item);
            }
            public void AddReserializedItem(Item root_item, List<Item> reserialized_items)
            {
                foreach (Item item in reserialized_items)
                    AddReserializedItem(root_item: root_item, reserialized_item: item);
            }
            public List<Item> GetDupedItems()
            {
                List<Item> list = new();
                foreach (var record in this)
                    list.AddRange(record.Key.Children);
                return list;
            }
            public List<Item> GetReserializedItems()
            {
                List<Item> list = new();
                foreach (var record in this)
                    list.AddRange(record.Value.Children);
                return list;
            }
        }
        private static bool ProcessItem(Item old_item, ParamPack @parms)
        {
            #region Addons
            // Addons, like Internal items, require special handling
            if (IsAddon(old_item))
            {
                if (IsKnownAddon(old_item, ref parms.AddonHandled))
                    return true;

                RecordAddon(old_item: old_item, ref parms.AddonHandled);
                BaseAddon ba = GetAddon(old_item);

                #region StaticAddon
                //  We will simply unpack the addon's components, and treat it like any other normal item
                //  (which means it will be rendered as a standalone static.
                if (StaticAddon)
                {
                    bool result = false;
                    foreach (var component in ba.Components)
                    {
                        // create an item and copy the core items props (hue, movable, visible, etc.)
                        Item temp = new Item();
                        Utility.CopyPropertyIntersection(dest: temp, src: component as Item);
                        parms.TempAddonComponents.Add(temp);  // need to delete these when we're done
                        result = ProcessItem(temp, parms);

                        if (result == false)
                            break;
                    }

                    return result;
                }
                #endregion StaticAddon
                else
                    old_item = ba;
            }
            #endregion Addons

            return RecordItem(old_item, parms);
        }
        private static bool RecordItem(Item old_item, ParamPack @parms)
        {
            // when we deep dupe the item, we will get a list of items it contains
            List<KeyValuePair<Item, Item>> dupeMap = new();

            Item new_item = null;
            // DeepDupe dupes and item, then recurses through the items it holds duping those as well.
            //  the resultant item/container is a mirror image of the original
            // Note: items on the internal map are already duped in their Dupe() override.
            new_item = Utility.DeepDupe(old_item, raw: false, dupeMap);

            if (new_item == null)
            {
                System.Diagnostics.Debug.Assert(false);
                return false;   // need to handle
            }

            // patch mobile template spawners
            if (new_item is Spawner)
            {   // Since the FD system doesn't natively support mobiles, we use this unique code to reconstruct the template mobile
                //  Not only is the mobile reconstructed, but his backpack and equipped items will be carried over.
                if (parms.SpawnerLinkMapper.ContainsKey(old_item.Serial))
                    (new_item as Spawner).TemplateMobileDefinition = parms.SpawnerLinkMapper[old_item.Serial];
            }

            if (new_item is BaseAddon)
            {   // record it
                if (!StaticAddon)
                {
                    parms.ItemDatabase.AddDupedItem(root_item: old_item, duped_item: new_item);
                    parms.LinkMapper.Add(old_item, new_item);                             // add top-level item to our link mapper
                    parms.AddonDatabase.Add(old_item.Serial, new List<AddonFields>());    // we will patch this to new serial later

                    BaseAddon ba = old_item as BaseAddon;
                    foreach (AddonComponent component in ba.Components)
                        parms.AddonDatabase[old_item.Serial].Add(
                            new AddonFields(component.BoolTable, component.Hue, component.Name, component.Light,
                            component.ItemID, component.Offset.X, component.Offset.Y, component.Offset.Z));
                }
                else
                {

                }
            }
            else if (dupeMap.Count == 0)
            {   // it had no sub items 
                parms.ItemDatabase.AddDupedItem(root_item: old_item, duped_item: new_item);

                // add top-level item to our link mapper
                parms.LinkMapper.Add(old_item, new_item);
            }
            else
            {   // it had sub items, and the top-level item will be included in the dupe list
                foreach (var kvp2 in dupeMap)
                {
                    parms.ItemDatabase.AddDupedItem(root_item: kvp2.Key, duped_item: kvp2.Value);

                    System.Diagnostics.Debug.Assert(!parms.LinkMapper.ContainsKey(kvp2.Key));

                    // add item to our link mapper
                    parms.LinkMapper.Add(kvp2.Key, kvp2.Value);

                    System.Diagnostics.Debug.Assert(parms.LinkMapper.ContainsKey(old_item));
                }
            }

            // some guys auto-fill themselves, so we won't complain if counts don't match
            //  other guys dupe their Items when they are duped. (usually items held on the internal map, or in their item table)
            /* 5/24/2024, Adam: 
             * 
            if (!old_item.NeedsSubItemsDuped())
                foreach (Item item in new_item.PeekItems)
                    parms.ItemDatabase.AddDupedItem(root_item: item, duped_item: item);
            */

            /*  If we have an auto fill container, we didn't dupe the items it contains since duping it will regenerate the items it needs.
             *  but things like enchanted scrolls still need their 'linked' item duped..
             */
            if (new_item is Container c && c.AutoFills == true)
            {
                foreach (Item citem in new_item.PeekItems)          // container items (citem)
                    if (Utility.HasInternalItems(citem))            // container item subitems (sitem)
                    {
                        //Item item = Utility.GetFirstInternalItem(citem);
                        //Item duped_item = Utility.Dupe(item);
                        //dupeMap.Add(new KeyValuePair<Item, Item>(/*old_item*/item, /*new_item*/ duped_item));
                        //parms.ItemDatabase.AddDupedItem(root_item: item, duped_item: duped_item);

                        //// add item to our link mapper
                        //parms.LinkMapper.Add(item, duped_item);
                    }
            }

            // get and add the items referenced by other items that reside on the internal map.
            //  Things like EnchantedScrolls and House/Township restoration deeds stash stuff on the internal map.
            //  During our 'surface grab' of items (rect passed into backup,) we wouldn't see these.
            //if (old_item.NeedsSubItemsDuped())

            if (new_item is not Container || new_item is Container cont && cont.AutoFills == false)
                FetchInternalItems(dupeMap, parms);

            return true;
        }

        private static void FetchInternalItems(List<KeyValuePair<Item, Item>> dupeMap, ParamPack @parms)
        {   // note: this algorithm only supports one stashed item per item. That is, any given item will
            //  only have one item (from the internal map,) recovered. 
            //  We don't currently have any items that stash more than one, but it is this algorithm that needs to change
            //  should we ever want to support it.
            foreach (var kvp in dupeMap)
            {
                if (Utility.HasInternalItems(kvp.Key))
                    if (!parms.LinkMapper.ContainsKey(Utility.GetFirstInternalItem(kvp.Key)))
                        ProcessItem(Utility.GetFirstInternalItem(kvp.Key), parms);

                foreach (var item in kvp.Key.PeekItems)
                    if (Utility.HasInternalItems(item))
                        if (!parms.LinkMapper.ContainsKey(Utility.GetFirstInternalItem(item)))
                            ProcessItem(Utility.GetFirstInternalItem(item), parms);
            }

            return;
        }
        #region Addon Tools
        private static bool IsAddon(Item item)
        {
            /* funky rules
                ** Standard ArcheryButte **
                (item is BaseAddon) ==                  false
                (item is ArcheryButte) ==               true
                (item is AddonComponent) ==             true

                ** ArcheryButte Addon **
                (item is BaseAddon) ==                  true
                (item is ArcheryButte) ==               false
                (item is AddonComponent) ==             false
             */
            // special case for ArcheryButtes added standalone (not as an addon)
            //  Unfortunately, they are AddonComponent which messes with my addon detection.
            if ((item is BaseAddon) == false && (item is ArcheryButte) == true && (item is AddonComponent) == true)
                // nope, not a real addon
                return false;

            return (item is BaseAddon || item is AddonComponent);
        }
        private static bool IsKnownAddon(Item old_item, ref List<Item> AddonHandled)
        {
            if (old_item is BaseAddon || old_item is AddonComponent)
            {
                BaseAddon ba = null;
                if (old_item is AddonComponent ac)
                    ba = ac.Addon;
                else
                    ba = old_item as BaseAddon;

                if (AddonHandled.Contains(ba as Item))
                    return true;
                else
                    return false;
            }

            return false;
        }
        private static BaseAddon GetAddon(Item old_item)
        {
            if (IsAddon(old_item))
            {
                BaseAddon ba = null;
                if (old_item is AddonComponent ac)
                    ba = ac.Addon;
                else
                    ba = old_item as BaseAddon;

                return ba;
            }

            return null;
        }
        private static void RecordAddon(Item old_item, ref List<Item> AddonHandled)
        {
            if (IsAddon(old_item))
            {
                BaseAddon ba = null;
                if (old_item is AddonComponent ac)
                    ba = ac.Addon;
                else
                    ba = old_item as BaseAddon;

                if (AddonHandled.Contains(ba as Item))
                    return;

                AddonHandled.Add(ba as Item);
            }

            return;
        }
        /*private static Item PatchAddon(
                Item old_item, Item new_item,
                ref Dictionary<Item, Item> linkMapper,
                ref ItemDatabase ItemDatabase,
                ref Dictionary<int, List<AddonFields>> AddonDatabase
                )
        {
            ItemDatabase.AddDupedItem(root_item: old_item, duped_item: new_item);
            linkMapper.Add(old_item, new_item);                             // add top-level item to our link mapper
            AddonDatabase.Add(old_item.Serial, new List<AddonFields>());    // we will patch this to new serial later

            BaseAddon ba = old_item as BaseAddon;
            foreach (AddonComponent component in ba.Components)
                AddonDatabase[old_item.Serial].Add(
                    new AddonFields(component.BoolTable, component.Hue, component.Name, component.Light, 
                    component.ItemID, component.Offset.X, component.Offset.Y, component.Offset.Z));
            return null;
        }*/
        public class AddonFields
        {
            public Item.ItemBoolTable BoolTable;    // movable, visible, hue, etc.
            public int Hue;
            public string Name;
            public LightType Light;
            public int ItemID;
            public Point3D Offset;
            public AddonFields(Item.ItemBoolTable boolTable, int hue, string name, LightType light, int itemID, int offsetX, int offsetY, int offsetZ)
            {
                BoolTable = boolTable;
                Hue = hue;
                Name = name;
                Light = light;
                ItemID = itemID;
                Offset = new Point3D(offsetX, offsetY, offsetZ);
            }
            public AddonFields()
            {

            }
        }
        #endregion AddonTools

        private static List<Item> GetPropertyItems(Dictionary<int, Dictionary<string, int>> PropertyLookup)
        {
            List<Item> allItems = new();
            foreach (var kvp in PropertyLookup)
            {   // items can point to themselves, or point to one another causing multiple references. Not an error

                if (!allItems.Contains(World.FindItem(kvp.Key)))
                    allItems.Add(World.FindItem(kvp.Key));

                foreach (var serial in kvp.Value.Values)
                    if (!allItems.Contains(World.FindItem(serial)))
                        allItems.Add(World.FindItem(serial));
            }

            return allItems;
        }
        private static void FDLinkPatcher(
            Dictionary<int, int> fixerMap,
            Dictionary<int, Dictionary<string, int>> linkPatches)
        {
            try
            {
                foreach (var link_patch in linkPatches)
                {
                    Item surface_item;
                    // here is the item we must patch
                    if ((surface_item = World.FindItem(fixerMap[link_patch.Key])) != null)
                    {
                        PropertyInfo[] props = Utility.ItemRWPropertyProperties(surface_item, IsAssignableTo: typeof(Item));
                        for (int i = 0; i < props.Length; i++)          // look at each property for this item
                            foreach (var prop in link_patch.Value)      // see if we can match the property name
                            {
                                string name = prop.Key;                 // property name
                                int serial = fixerMap[prop.Value];      // serial will need to exist in world
                                if (props[i].Name == name)
                                {                                       // found the property
                                    Item link_item;                     // locate the real-world item
                                    if ((link_item = World.FindItem(serial)) != null)
                                        //  link to this item
                                        props[i].SetValue(surface_item, link_item, null);
                                    else
                                        throw new ApplicationException(string.Format("No link-item with serial {0} found in world.", serial));
                                }
                            }
                    }
                    else
                        throw new ApplicationException(string.Format("No surface-item with serial {0} found in world.", link_patch.Key));
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
        }

        private static Dictionary<int, Dictionary<string, int>>
            FDLinkMapper(
            Dictionary<
                Item,    // old surface item 
                Item     // new surface item 
                > linkMapper)
        {
            Dictionary
            <int,               // new top-level item (serial)
            Dictionary<string,  // property name
                int             // map to new item (serial)
                >
            > PropertyLookup = new();

            // properties that get/set an Item
            Dictionary<Item, List<PropertyInfo>> propertyInfo = new();
            foreach (var kvp in linkMapper)
            {
                Item item = kvp.Value;      // new surface level item (new serial)
                PropertyInfo[] props = Utility.ItemRWPropertyProperties(item, IsAssignableTo: typeof(Item));
                for (int i = 0; i < props.Length; i++)
                    if (props[i].GetValue(item, null) != null)
                        if (propertyInfo.ContainsKey(item))
                            // we will try to update this guy
                            propertyInfo[item].Add(props[i]);
                        else
                            // we will try to update this guy
                            propertyInfo.Add(item, new List<PropertyInfo>() { props[i] });
            }

            /*  
             *  property of an old item that holds an item
             *  We will remap that item to a new item if possible, or make it null since that item 
             *  will not exist (or be wrong) in the new world (presupposes we are changing shards)
             */
            foreach (var record in propertyInfo)
            {   // certain items don't need their items duped.
                // Treasure chests for example (because they regenerate their items on creation.)
                //if (record.Key.AutoFills())
                if (record.Key is not Container || record.Key is Container cont && cont.AutoFills == false)
                {
                    // the new item we wish to patch
                    Item new_surface_item = record.Key;
                    foreach (var prop in record.Value)
                    {   // what we are currently pointing at
                        Item old_pointer = prop.GetValue(new_surface_item, null) as Item;
                        // do we know about this old_pointer? (has it been mapped to a new item?)
                        // some items link to things that are not surface items. Instead they are items on the Internal
                        if (linkMapper.ContainsKey(old_pointer))
                        {   // for clarity
                            Item new_pointer = linkMapper[old_pointer];
                            // yes we do (know about this old_pointer,) and it now should point to new_pointer
                            prop.SetValue(new_surface_item, new_pointer, null);

                            // ---- \\

                            // Now construct the lookup record for the client of this dump
                            // we use serial numbers here...
                            /*
                                Dictionary
                                <int,               // new top-level item (serial)
                                Dictionary<string,  // property name
                                    int             // map to new item (serial)
                                    >
                                > PropertyLookup
                             */
                            if (PropertyLookup.ContainsKey(new_surface_item.Serial))
                            {   // example: motion control is the new item
                                //  It has 6 links, 3 are set. link1=> new_pointer1, link2=> new_pointer2,
                                //  the rest are null
                                PropertyLookup[new_surface_item.Serial].Add(prop.Name, new_pointer.Serial);
                            }
                            else
                            {   // create new key
                                Dictionary<string, int> temp = new();
                                temp.Add(prop.Name, new_pointer.Serial);
                                PropertyLookup.Add(new_surface_item.Serial, temp);
                            }
                        }
                        else
                        {
                            // no we do not (know about this old_pointer,) and it now should point to null since whatever it is
                            // pointing at will certainly be wrong on the other server
                            prop.SetValue(new_surface_item, null, null);
                        }
                    }
                }
            }
            return PropertyLookup;
        }
        public static void FDPropertyLookupWriter(string filename,
            Dictionary
            <int,               // new top-level item (serial)
            Dictionary<string,  // property name
                int             // map to item (serial)
                >
            > PropertyLookup
            )
        {
            string lookup = FDFileNames(FDNameType.Lookup, base_name: filename);
            Console.WriteLine("Saving {0}...", lookup);
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter(lookup, true);
                int version = 0;
                writer.Write(version);

                switch (version)
                {
                    case 0:
                        {
                            // the recipient of this database will use this to remap item properties to the correct items
                            writer.Write(PropertyLookup.Count);             // how many items need patching
                            foreach (var lookup_kvp in PropertyLookup)
                            {
                                writer.Write((int)lookup_kvp.Key);          // our surface level item
                                writer.Write(lookup_kvp.Value.Count);       // how many properties need patching
                                foreach (var value in lookup_kvp.Value)
                                {
                                    writer.Write(value.Key);                // string prop name
                                    writer.Write(value.Value);              // serial of where it should point
                                }
                            }
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing {0}", lookup);
                Console.WriteLine(ex.ToString());
            }
        }
        public static Dictionary<int, Dictionary<string, int>>
            FDPropertyLookupReader(string filename, ref string reason)
        {
            Dictionary
            <int,               // new top-level item (serial)
            Dictionary<string,  // property name
                int             // map to item (serial)
                >
            > PropertyLookup = new();
            string lookup = FDFileNames(FDNameType.Lookup, base_name: filename);
            if (!File.Exists(lookup))
            {
                reason = string.Format("{0} does not exist", lookup);
                return null;
            }
            Console.WriteLine("Loading {0}...", lookup);
            bool error = false;
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(lookup, FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {
                            int count = reader.ReadInt();               // how many items need patching
                            for (int ix = 0; ix < count; ix++)
                            {
                                int item = reader.ReadInt();            // our surface level item
                                int inner_count = reader.ReadInt();     // how many properties need patching
                                for (int jx = 0; jx < inner_count; jx++)
                                {
                                    string name = reader.ReadString();  // property name
                                    int serial = reader.ReadInt();      // serial of where it should point
                                    Dictionary<string, int> temp = new();
                                    temp.Add(name, serial);
                                    if (PropertyLookup.ContainsKey(item))
                                        PropertyLookup[item].Add(name, serial);
                                    else
                                        PropertyLookup.Add(item, temp);
                                }
                            }
                            break;
                        }
                    default:
                        {
                            reason = string.Format("Invalid {0} version.", lookup);
                            return null;
                        }
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error reading {0}", lookup);
                Utility.PopColor();
                LogHelper.LogException(ex);
                reason = ex.Message;
                error = true;
            }

            if (!error)
                reason = string.Format("PropertyLookup successfully loaded");

            return PropertyLookup;
        }
        private static void FDRollback(List<int> rollback_ids)
        {
            // No longer needed since we no longer have a global reserved database
        }
        protected static void DumpObject(Mobile from, string filename, object targeted)
        {
            Item cont = (Item)targeted;

            try
            {
                using (FileStream idxfs = new FileStream(filename + ".idx", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (FileStream binfs = new FileStream(filename + ".bin", FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        GenericWriter idx = new BinaryFileWriter(idxfs, true);
                        GenericWriter bin = new BinaryFileWriter(binfs, true);

                        ArrayList items = new ArrayList();
                        items.Add(cont);
                        items.AddRange(cont.GetDeepItems());

                        idx.Write((int)items.Count);
                        foreach (Item item in items)
                        {
                            long start = bin.Position;

                            idx.Write(item.GetType().FullName); // <--- DIFFERENT FROM WORLD SAVE FORMAT!
                            idx.Write((int)item.Serial);
                            idx.Write((long)start);

                            item.Serialize(bin);

                            idx.Write((int)(bin.Position - start));
                        }

                        idx.Close();
                        bin.Close();
                    }
                }

                if (Verbose) from.SendMessage("Object successfully dumped to {0}.", filename);
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine(e.ToString());
                from.SendMessage("Exception: {0}", e.Message);
            }
        }
        private static List<int> FDCreateRollback(int amount, string filename, ref string reason)
        {
            List<int> serials = new();
            try
            {
                if (amount == 0)
                {
                    reason = string.Format("Usage: FDCreateRollback <number>");
                    return null;
                }

                // rewind our heap so that we are pointing at the beginning of reserved serials
                LocalSerialHeap.Index = 0;
                int index = LocalSerialHeap.Pointer;
                System.Diagnostics.Debug.Assert(index != -1);

                if (index == -1)
                {   // reason already established
                    return null;
                }

                // foreach reserved serial
                for (int ix = 0; ix < LocalSerialHeap.Count; ix++)
                {   // add the reserved serial to the list
                    serials.Add(LocalSerialHeap.Pointer);
                    // increment to next serial
                    LocalSerialHeap.Index++;
                }

                FDRollbackWriter(filename, serials);
                reason = string.Format("FDCreateRollback: Success. {0} Serials saved to rollback database", amount);
            }
            catch
            {
                reason = string.Format("Usage: FDCreateRollback <number>");
                return null;
            }

            return serials;
        }
        #region Mobile Definition I/O
        public static void FDMobileDefinitionWriter(string base_name, string filename, Dictionary<string, string> keyValuePairs)
        {
            string mobileDefinition = FDFileNames(FDNameType.MobileDefinition, base_name: base_name, filename: filename);
            Console.WriteLine("Saving {0}...", mobileDefinition);
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter(mobileDefinition, true);
                int version = 0;
                writer.Write(version);

                switch (version)
                {
                    case 0:
                        {
                            writer.Write(keyValuePairs.Count);
                            foreach (var kvp in keyValuePairs)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value);
                            }
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing {0}", mobileDefinition);
                Console.WriteLine(ex.ToString());
            }
        }
        public static void FDMobileDefinitionWriterRaw(string filename, Dictionary<string, string> keyValuePairs)
        {
            string mobileDefinition = filename;
            if (!mobileDefinition.EndsWith(".mdf", StringComparison.OrdinalIgnoreCase))
                mobileDefinition = mobileDefinition + ".mdf";
            Console.WriteLine("Saving {0}...", mobileDefinition);
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter(mobileDefinition, true);
                int version = 0;
                writer.Write(version);

                switch (version)
                {
                    case 0:
                        {
                            writer.Write(keyValuePairs.Count);
                            foreach (var kvp in keyValuePairs)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value);
                            }
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing {0}", mobileDefinition);
                Console.WriteLine(ex.ToString());
            }
        }
        public static Dictionary<string, string> FDMobileDefinitionReader(string filename, ref string reason)
        {
            string mobileDefinition = filename;// FDFileNames(FDNameType.MobileDefinition, base_name: filename);
            if (!File.Exists(mobileDefinition))
            {
                reason = string.Format("{0} does not exist", mobileDefinition);
                return null;
            }
            Dictionary<string, string> keyValuePairs = new();
            Console.WriteLine("Loading {0}...", mobileDefinition);
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(mobileDefinition, FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                                keyValuePairs.Add(reader.ReadString(), reader.ReadString());
                            break;
                        }
                    default:
                        {
                            reason = string.Format("Invalid {0} version.", mobileDefinition);
                            return null;
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error reading {0}", mobileDefinition);
                Utility.PopColor();
            }

            return keyValuePairs;
        }
        #endregion Mobile Definition I/O
        public static void FDRollbackWriter(string filename, List<int> serial_list)
        {
            string rollback = FDFileNames(FDNameType.Rollback, base_name: filename);
            Console.WriteLine("Saving {0}...", rollback);
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter(rollback, true);
                int version = 0;
                writer.Write(version);

                switch (version)
                {
                    case 0:
                        {
                            writer.Write(0);    // obsolete    
                            writer.Write(serial_list.Count);
                            foreach (int serial in serial_list)
                                writer.Write(serial);
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing {0}", rollback);
                Console.WriteLine(ex.ToString());
            }
        }
        public static void FDBackupTraceWriter(string filename, string text, bool begin)
        {
            string trace = FDFileNames(FDNameType.BackupTrace, base_name: filename);
            if (begin)
                Console.WriteLine("Saving {0}...", trace);
            try
            {
                if (begin && File.Exists(trace))
                    File.Delete(trace);

                string[] lines = { text };
                File.AppendAllLines(trace, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing {0}", trace);
                Console.WriteLine(ex.ToString());
            }
        }
        public static void FDRestoreTraceWriter(string filename, string text, bool begin)
        {
            string trace = FDFileNames(FDNameType.RestoreTrace, base_name: filename);
            if (begin)
                Console.WriteLine("Saving {0}...", trace);
            try
            {
                if (begin && File.Exists(trace))
                    File.Delete(trace);

                string[] lines = { text };
                File.AppendAllLines(trace, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing {0}", trace);
                Console.WriteLine(ex.ToString());
            }
        }
        public static void FDFormatErrorWriter(string filename, string text, bool begin)
        {
            string trace = FDFileNames(FDNameType.FormatError, base_name: filename);
            if (begin)
                Console.WriteLine("Saving {0}...", trace);
            try
            {
                if (begin && File.Exists(trace))
                    File.Delete(trace);

                string[] lines = { text };
                File.AppendAllLines(trace, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing {0}", trace);
                Console.WriteLine(ex.ToString());
            }
        }
        // Unavailable
        public static void FDUnavailableErrorWriter(string filename, string text, bool begin)
        {
            string trace = FDFileNames(FDNameType.RestoreObjectUnavailable, base_name: filename);
            if (begin)
                Console.WriteLine("Saving {0}...", trace);
            try
            {
                if (begin && File.Exists(trace))
                    File.Delete(trace);

                string[] lines = { text };
                File.AppendAllLines(trace, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing {0}", trace);
                Console.WriteLine(ex.ToString());
            }
        }
        public static List<int> FDRollbackReader(string filename, ref string reason)
        {
            string rollback = FDFileNames(FDNameType.Rollback, base_name: filename);
            if (!File.Exists(rollback))
            {
                reason = string.Format("{0} does not exist", rollback);
                return null;
            }
            List<int> list = new();
            Console.WriteLine("Loading {0}...", rollback);
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(rollback, FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {
                            int obsolete = reader.ReadInt();
                            list.Add(obsolete);
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                                list.Add(reader.ReadInt());
                            break;
                        }
                    default:
                        {
                            reason = string.Format("Invalid {0} version.", rollback);
                            return null;
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error reading {0}", rollback);
                Utility.PopColor();
            }

            return list;
        }

        #region FDFormat
        private static int FDHandle()
        {   // create a security handle that represents this server/machine to prevent accidentally wiping items from the wrong server
            return Utility.GetStableHashCode(Core.Server.ToUpper() + Environment.MachineName.ToUpper());
        }
        public static void FDFormatWriter(string filename, List<int> serial_list)
        {
            string format = FDFileNames(FDNameType.Format, base_name: filename);
            Console.WriteLine("Saving {0}...", format);
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter(format, true);
                int version = 0;
                writer.Write(version);

                switch (version)
                {
                    case 0:
                        {
                            writer.Write(FDHandle());
                            writer.Write(serial_list.Count);
                            foreach (int serial in serial_list)
                                writer.Write(serial);
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing {0}", format);
                Console.WriteLine(ex.ToString());
            }
        }
        public static List<int> FDFormatReader(string filename, ref int handle, ref string reason)
        {
            string format = FDFileNames(FDNameType.Format, base_name: filename);
            if (!File.Exists(format))
            {
                reason = string.Format("{0} does not exist", format);
                return null;
            }
            List<int> list = new();
            Console.WriteLine("Loading {0}...", format);
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(format, FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {
                            handle = reader.ReadInt();
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                                list.Add(reader.ReadInt());
                            break;
                        }
                    default:
                        {
                            reason = string.Format("Invalid {0} version.", format);
                            return null;
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error reading {0}", format);
                Utility.PopColor();
            }

            if (list.Count > 0)
                reason = string.Format("Successfully loaded {0} serials", list.Count);
            else
                reason = string.Format("Database empty");

            return list;
        }

        #endregion FDFormat

        #region FDAddon
        public static void FDAddonWriter(string filename, Dictionary<int, List<AddonFields>> AddonDatabase)
        {
            string addons = FDFileNames(FDNameType.Addons, base_name: filename);
            Console.WriteLine("Saving {0}...", addons);
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter(addons, true);
                int version = 0;
                writer.Write(version);

                switch (version)
                {
                    case 0:
                        {
                            writer.Write(AddonDatabase.Count);                          // records in the database
                            foreach (var kvp1 in AddonDatabase)                         // foreach record
                            {
                                writer.Write(kvp1.Key);                                 // BaseAddon serial

                                writer.Write(kvp1.Value.Count);                         // components in the record

                                foreach (var kvp2 in kvp1.Value)
                                {
                                    writer.Write((int)kvp2.BoolTable);          // bool table (movable, hidden, etc.
                                    writer.Write(kvp2.Hue);                     // hue
                                    writer.Write(kvp2.Name);                    // name
                                    writer.Write((int)kvp2.Light);              // light
                                    writer.Write(kvp2.ItemID);                  // item ID
                                    writer.Write(kvp2.Offset.X);                // X offset
                                    writer.Write(kvp2.Offset.Y);                // Y offset
                                    writer.Write(kvp2.Offset.Z);                // Z offset
                                }
                            }
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing {0}", addons);
                Console.WriteLine(ex.ToString());
            }

            //if (AddonDatabase.Count > 0)
            //    reason = string.Format("Successfully loaded {0} serials", AddonDatabase.Count);
            //else
            //    reason = string.Format("Database empty");
        }
        public static Dictionary<int, List<AddonFields>> FDAddonReader(string filename, ref string reason)
        {
            string addons = FDFileNames(FDNameType.Addons, base_name: filename);
            if (!File.Exists(addons))
            {
                reason = string.Format("{0} does not exist", addons);
                return null;
            }
            Dictionary<int, List<AddonFields>> AddonDatabase = new();
            Console.WriteLine("Loading {0}...", addons);
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(addons, FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {
                            int count = reader.ReadInt();                               // how many records in the database
                            for (int ix = 0; ix < count; ix++)                          // for each record
                            {
                                int baSerial = reader.ReadInt();                        // BaseAddon serial
                                AddonDatabase.Add(baSerial, new List<AddonFields>());   // initialize this record

                                int listCount = reader.ReadInt();                       // how many components in the record

                                for (int jx = 0; jx < listCount; jx++)                  // add each component
                                {
                                    AddonDatabase[baSerial].Add(
                                        new AddonFields(
                                            (Item.ItemBoolTable)reader.ReadInt(),           // bool table
                                            reader.ReadInt(),                               // hue
                                            reader.ReadString(),                            // name
                                            (LightType)reader.ReadInt(),                    // light
                                            reader.ReadInt(),                               // item ID
                                            reader.ReadInt(),                               // X offset
                                            reader.ReadInt(),                               // Y offset
                                            reader.ReadInt()                                // Z offset
                                        )
                                    );
                                }
                            }

                            break;
                        }
                    default:
                        {
                            reason = string.Format("Invalid {0} version.", addons);
                            return null;
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error reading {0}", addons);
                Utility.PopColor();
            }

            if (AddonDatabase.Count > 0)
                reason = string.Format("Successfully loaded {0} serials", AddonDatabase.Count);
            else
                reason = string.Format("Database empty");

            return AddonDatabase;
        }
        #endregion FDAddon

        private static bool FDCommitAllocate(List<int> serials, ref string reason)
        {
            try
            {
                foreach (int serial in serials)
                {
                    if (!LocalSerialHeap.AllocateThis(serial: serial, ref reason))
                    {
                        return false;
                    }
                }

                reason = string.Format("FDCommitAllocate: Success. {0} Serials allocated", serials.Count);
            }
            catch
            {
                reason = string.Format("Usage: FDCheckAllocate <number>");
                return false;
            }

            return true;
        }
        protected static bool FDReassign(object targeted, ref string reason, List<Item> new_serials)
        {
            Item reassign = targeted as Item;
            if (reassign == null)
            {
                reason = string.Format("You must target an item to reassign.");
                System.Diagnostics.Debug.Assert(false);
                return false;
            }

            new_serials.Add(reassign);

            int lastSerialAssigned = -1;
            for (int i = 0; i < new_serials.Count; i++)
            {
                Item item = new_serials[i] as Item;
                item.ReassignSerial(LocalSerialHeap.Pointer);
                lastSerialAssigned = LocalSerialHeap.Pointer;
                // point at the next reserved serial
                LocalSerialHeap.Index++;
            }

            reason = string.Format("Successfully reassigned serials to {0} items. Next free reserved serial is 0x{1:X}.", new_serials.Count, lastSerialAssigned);
            return true;
        }
        #endregion Dump Object Array
        #region Load Object Array
        #region Load Object Array Tools
        private static readonly Regex SubStrKey = new(@"\d+", RegexOptions.Compiled);
        private static int KeyValue(string s)
        {   // extract the ordinal value from the string. We will sort on that since: 
            // Microsoft: (GetFiles) The order of the returned file names is not guaranteed; use the Sort method if a specific sort order is required.
            string result = SubStrKey.Match(s).Value;
            return Int32.Parse(result);
        }
        #endregion Load Object Array Tools
        public static void OnFDLoadObjArray(CommandEventArgs e)
        {
            LocalSerialHeap.Initialize();
            bool result = false;
            string reason = string.Empty;

            #region Process Arguments
            if (e.Arguments.Length != 1)
            {
                e.Mobile.SendMessage("Usage: [FDRestore <filename>");
                return;
            }
            string filename = e.Arguments[0];
            string database = FDFileNames(FDNameType.BaseFolder, filename);
            if (!Directory.Exists(database))
            {
                e.Mobile.SendMessage($"No database named {filename}");
                e.Mobile.SendMessage("Usage: [FDRestore <filename>");
                return;
            }
            #endregion  Process Arguments

            #region Rollback Reader
            // probably a bad name now. Holds all reserved serials we will be using
            Dictionary<int, int> LinkPatches = new();

            List<int> RollbackSerials = FDRollbackReader(filename, ref reason);
            if (RollbackSerials == null)
            {
                System.Diagnostics.Debug.Assert(false);
                Console.WriteLine("OnFDLoadObjArray: {0}", reason);
                return;
            }
            else
            {   // make sure we can allocate the same serials as reserved by the sender/source shard
                RollbackSerials.RemoveAt(0);    // obsolete
                LocalSerialHeap.Verify(RollbackSerials, ref reason);
                Utility.ConsoleWriteLine("FDLoadObjArray: loading at {0:X}", ConsoleColor.DarkCyan, RollbackSerials[0]);
                foreach (int serial in RollbackSerials)
                    LinkPatches.Add(serial, serial);
            }
            #endregion Rollback Reader

            #region Allocating serials
            e.Mobile.SendMessage("Allocating serials...");
            result = FDCommitAllocate(serials: RollbackSerials, ref reason);
            e.Mobile.SendMessage("{0}", reason);
            if (!result)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }
            #endregion Allocating serials

            #region Get file name array to process
            // The order of the returned file names is not guaranteed; use the Sort method if a specific sort order is required.
            // learn.microsoft.com - Directory.GetFiles Method
            // -- 
            // I'm not convinced reloading objects in the same order they were saved is absolutely necessary, but I did see funniness
            //  when restoring 17K items. The errors were subtle, like 'links' being null.
            //  This technique also injects predictability and consistency. If there are ordering dependencies, this will resolve it.
            List<string> filenames = new List<string>();
            {
                e.Mobile.SendMessage("Finding object files...");
                string pattern = FDFileNames(FDNameType.ItemsPattern, filename);
                string path = FDFileNames(FDNameType.BaseFolder, filename);
                foreach (string fn in Directory.GetFiles(path, pattern))
                {
                    string temp = Path.GetFileNameWithoutExtension(fn);
                    if (!filenames.Contains(temp))
                        filenames.Add(temp);
                }
                e.Mobile.SendMessage("Sorting object files...");
                filenames.Sort((e1, e2) =>
                {
                    return KeyValue(e1).CompareTo(KeyValue(e2));
                });
            }
            #endregion  Get file name array to process

            #region Sanity
            System.Diagnostics.Debug.Assert(filenames.Count == RollbackSerials.Count);
            #endregion Sanity

            #region Load each file
            e.Mobile.SendMessage("Loading object files...");
            try
            {
                int totalLoaded = 0;
                int totalCount = 0;
                int NumItemsWSubItems = 0;
                int hardFails = 0;
                foreach (string fn in filenames)
                    try
                    {
                        string folder = FDFileNames(FDNameType.BaseFolder, filename);
                        int loaded = 0;
                        int count = 0;
                        int subItems = 0;
                        LogHelper log = new LogHelper(fn + " LoadCont.log");
                        log.Log(LogType.Text, String.Format("Reload process initiated by {0}, with {1} as backup data.", e.Mobile, fn));

                        using (FileStream idxfs = new FileStream(folder + fn + ".idx", FileMode.Open, FileAccess.Read))
                        {
                            using (FileStream binfs = new FileStream(folder + fn + ".bin", FileMode.Open, FileAccess.Read))
                            {
                                GenericReader bin = new BinaryFileReader(new BinaryReader(binfs));
                                GenericReader idx = new BinaryFileReader(new BinaryReader(idxfs));

                                count = idx.ReadInt();
                                if (count == -1)
                                    log.Log(LogType.Text, "No item data to reload."); // do nothing
                                else
                                {
                                    ArrayList items = new ArrayList(count);
                                    log.Log(LogType.Text, String.Format("Attempting to reload {0} items.", count));

                                    Type[] ctortypes = new Type[] { typeof(Serial) };
                                    object[] ctorargs = new object[1];

                                    for (int i = 0; i < count; i++)
                                    {
                                        string type = idx.ReadString();

                                        Serial serial = idx.ReadInt();

                                        if (World.FindItem(serial) != null)
                                        {
                                            // we are reading the subitem of another item and it has already been loaded
                                            loaded++;
                                            subItems++;
                                            goto cleanup;
                                        }

                                        long position = idx.ReadLong();
                                        int length = idx.ReadInt();

                                        if (string.IsNullOrEmpty(type))
                                        {
                                            Console.WriteLine("Warning: Tried to load null type. Serial {0}. Ignoring item.", serial);
                                            log.Log(String.Format("Warning: Tried to load null type. Serial {0}. Ignoring item.", serial));
                                            System.Diagnostics.Debug.Assert(false);
                                            hardFails++;
                                            goto cleanup;
                                        }

                                        Type t = ScriptCompiler.FindTypeByFullName(type);
                                        if (t == null)
                                        {
                                            Console.WriteLine("Warning: Tried to load nonexistent type {0}. Ignoring item.", type);
                                            log.Log(String.Format("Warning: Tried to load nonexistent type {0}. Ignoring item.", type));
                                            System.Diagnostics.Debug.Assert(false);
                                            hardFails++;
                                            goto cleanup;
                                        }

                                        ConstructorInfo ctor = t.GetConstructor(ctortypes);
                                        if (ctor == null)
                                        {
                                            Console.WriteLine("Warning: Tried to load type {0} which has no serialization constructor. Ignoring item.", type);
                                            log.Log(String.Format("Warning: Tried to load type {0} which has no serialization constructor. Ignoring item.", type));
                                            System.Diagnostics.Debug.Assert(false);
                                            hardFails++;
                                            goto cleanup;
                                        }

                                        Item item = null;
                                        try
                                        {
                                            ctorargs[0] = serial;
                                            item = (Item)(ctor.Invoke(ctorargs));
                                        }
                                        catch (Exception ex)
                                        {
                                            LogHelper.LogException(ex);
                                            Console.WriteLine("An exception occurred while trying to invoke {0}'s serialization constructor.", t.FullName);
                                            Console.WriteLine(ex.ToString());
                                            log.Log(String.Format("An exception occurred while trying to invoke {0}'s serialization constructor.", t.FullName));
                                            log.Log(ex.ToString());
                                            hardFails++;
                                            goto cleanup;
                                        }

                                        if (item != null)
                                        {
                                            if (World.FindItem(item.Serial) == null)
                                                World.AddItem(item);
                                            else
                                            {
                                                hardFails++;
                                                System.Diagnostics.Debug.Assert(false);
                                            }

                                            // make sure it's there
                                            System.Diagnostics.Debug.Assert(World.FindItem(item.Serial) != null);

                                            items.Add(new object[] { item, position, length });
                                            if (Verbose) log.Log(String.Format("Successfully created item {0}", item));
                                        }
                                        else
                                        {
                                            hardFails++;
                                            System.Diagnostics.Debug.Assert(false);
                                        }
                                    }

                                    #region Parent Hierarchy
                                    for (int i = 0; i < items.Count; i++)
                                    {
                                        object[] entry = (object[])items[i];
                                        Item item = entry[0] as Item;
                                        long position = (long)entry[1];
                                        int length = (int)entry[2];

                                        if (item != null)
                                        {
                                            bin.Seek(position, SeekOrigin.Begin);

                                            try
                                            {
                                                // read the item
                                                item.Deserialize(bin);

                                                // take care of parent hierarchy
                                                object p = item.Parent;
                                                if (p is Item)
                                                {
                                                    ((Item)p).RemoveItem(item);
                                                    item.Parent = null;
                                                    ((Item)p).AddItem(item);
                                                }
                                                else if (p is Mobile)
                                                {
                                                    ((Mobile)p).RemoveItem(item);
                                                    item.Parent = null;
                                                    ((Mobile)p).AddItem(item);
                                                }
                                                else
                                                {
                                                    item.Delta(ItemDelta.Update);
                                                }

                                                item.ClearProperties();

                                                object rp = item.RootParent;
                                                if (rp is Item)
                                                    ((Item)rp).UpdateTotals();
                                                else if (rp is Mobile)
                                                    ((Mobile)rp).UpdateTotals();
                                                else
                                                    item.UpdateTotals();

                                                if (bin.Position != (position + length))
                                                    throw new Exception(String.Format("Bad serialize on {0}", item));

                                                log.Log(LogType.Item, item, "Successfully loaded.");
                                                loaded++;
                                            }
                                            catch (Exception ex)
                                            {
                                                LogHelper.LogException(ex);
                                                Console.WriteLine("Caught exception while deserializing {0}:", item);
                                                Console.WriteLine(ex.ToString());
                                                Console.WriteLine("Deleting item.");
                                                log.Log(String.Format("Caught exception while deserializing {0}:", item));
                                                log.Log(ex.ToString());
                                                log.Log("Deleting item.");
                                                item.Delete();
                                            }
                                        }
                                    }
                                    #endregion Parent Hierarchy
                                }
                            cleanup:
                                idx.Close();
                                bin.Close();
                            }
                        }

                        if (Verbose || (count - loaded) > 0)
                            Console.WriteLine("Attempted to load {0} items: {1} loaded, {2} failed.", count, loaded, count - loaded);
                        if ((count - loaded) > 0)
                            log.Log(String.Format("Attempted to load {0} items: {1} loaded, {2} failed.", count, loaded, count - loaded));
                        if (Verbose || (count - loaded) > 0)
                            e.Mobile.SendMessage("Attempted to load {0} items: {1} loaded, {2} failed.", count, loaded, count - loaded);
                        log.Finish();
                        totalCount += count;
                        totalLoaded += loaded;
                        NumItemsWSubItems += subItems;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex);
                        Console.WriteLine(ex.ToString());
                        e.Mobile.SendMessage("Exception: {0}", ex.Message);
                    }

                if (Verbose || (totalCount - totalLoaded) > 0)
                {
                    Console.WriteLine("-----");
                    e.Mobile.SendMessage("-----");
                }

                if (Verbose || (totalCount - totalLoaded) > 0)
                    Console.WriteLine("Attempted to load {0} items: {1} loaded, {2} failed, with {3} subItems and {4} hard failures.", totalCount, totalLoaded, totalCount - totalLoaded, NumItemsWSubItems, hardFails);
                //if ((totalCount - totalLoaded) > 0)
                //    log.Log(String.Format("Attempted to load {0} items: {1} loaded, {2} failed.", totalCount, totalLoaded, totalCount - totalLoaded));
                if (Verbose || (totalCount - totalLoaded) > 0)
                    e.Mobile.SendMessage("Attempted to load {0} items: {1} loaded, {2} failed, with {3} subItems and {4} hard failures.", totalCount, totalLoaded, totalCount - totalLoaded, NumItemsWSubItems, hardFails);
            }
            finally
            {

            }
            #endregion Load each file

            #region Sanity
            List<int> unavailable = new();
            foreach (var item in LinkPatches.Select((value, i) => (value, i)))
            {
                var value = item.value;
                var index = item.i;
                if (World.FindItem(value.Key) == null)
                    unavailable.Add(value.Key);
            }
            System.Diagnostics.Debug.Assert(unavailable.Count == 0);

            foreach (int bad in unavailable)
            {   // here we should write a text file showing what couldn't be loaded.
                //  You then compare that to <name>.fddb/<name>_trace.txt to determine the source item that was giving us the problem.
                LinkPatches.Remove(bad);
                RollbackSerials.Remove(bad);
            }

            if (unavailable.Count != 0)
            {
                string header = "** List of Serials for which there is no in-game item **" + Environment.NewLine +
                    "The serials listed here either represent object load failures, or an inability to create/allocate the item, " + Environment.NewLine +
                    "possibly due to a constructor failure.";
                FDUnavailableErrorWriter(filename, header, begin: true);
                foreach (int bad in unavailable)
                    FDUnavailableErrorWriter(filename, string.Format($"0x{bad:X}"), begin: false);
            }

            #endregion Sanity

            #region Patch Links (fixup Properties of loaded items to point to the newly created items)
            {
                // Properties fixer database for loaded items so that they point to the newly created items
                e.Mobile.SendMessage("Loading fixer database...");
                Dictionary<int, Dictionary<string, int>> PropertyLookup = FDPropertyLookupReader(filename, ref reason);
                e.Mobile.SendMessage("{0}", reason);
                System.Diagnostics.Debug.Assert(PropertyLookup != null);
                if (PropertyLookup == null)
                    return; // should never happen

                // fixup Properties of loaded items to point to the newly created items
                e.Mobile.SendMessage("Fixing up properties...");
                FDLinkPatcher(LinkPatches, PropertyLookup);
            }
            #endregion Patch Links (fixup Properties of loaded items to point to the newly created items)

            #region Order tiles
            // sort list so dirt tiles have the lowest serial and are placed first
            List<Item> SortedList = new();
            {
                foreach (var serial in RollbackSerials)
                    SortedList.Add(World.FindItem(serial));
                SortedList = SortedList.OrderBy(o => o.Serial).ToList();
            }
            ;
            #endregion Order tiles

            #region Recreate as this world's items (ordered)

            List<Item> Placeholders = new();
            for (int i = 0; i < RollbackSerials.Count; i++)
                Placeholders.Add(new Item());   // reserve a serial in this world

            Dictionary<int, int> linkMapper = new();
            FDBackupTraceWriter(filename, "** Restore Trace map: Original serial to new serial **", begin: true);
            foreach (var item in SortedList)
            {
                int old_serial = item.Serial;
                int new_serial = Placeholders[0].Serial;

                string before = FormatItem(item, "Error, cannot find original");
                string after = FormatItem(Placeholders[0], "Error, cannot find dupe");
                FDBackupTraceWriter(filename, string.Format($"{before}, {after}"), begin: false);

                linkMapper.Add(old_serial, new_serial);
                System.Diagnostics.Debug.Assert(World.FindItem(old_serial) != null);
                System.Diagnostics.Debug.Assert(World.FindItem(new_serial) != null);
                World.FindItem(new_serial).Delete();
                Placeholders.RemoveAt(0);
                World.FindItem(old_serial).ReassignSerial(new_serial);
                System.Diagnostics.Debug.Assert(World.FindItem(old_serial) == null);
                System.Diagnostics.Debug.Assert(World.FindItem(new_serial) != null);
                System.Diagnostics.Debug.Assert(World.FindItem(new_serial).Serial == new_serial);
            }
            #endregion Recreate as this world's items

            #region Remove hundreds of redundant dirt tiles
            //foreach (var item in SortedList)
            //{
            //    List<int> dirt = new() { 0x31F4, 0x31F5, 0x31F6, 0x31F7 };

            //}
            #endregion Remove hundreds of redundant dirt tiles

            #region Create format database
            {
                List<int> AllSerials = new();
                foreach (Item item in SortedList)
                { AllSerials.Add(item.Serial); }
                FDFormatWriter(filename, AllSerials);
            }
            ;
            #endregion Create format database

            #region Read Addon database

            Dictionary<int, List<AddonFields>> AddonDatabase = FDAddonReader(filename, ref reason);

            #endregion Read Addon database

            #region Reconstruct template mobiles
            Dictionary<int, Dictionary<string, string>> mobile_defintion_lookup = new();
            {
                #region Get file name array to process
                e.Mobile.SendMessage("Finding mobile definition files...");
                string pattern = FDFileNames(FDNameType.MobileDefinitionPattern, filename);
                string path = FDFileNames(FDNameType.BaseFolder, filename);
                foreach (string fn in Directory.GetFiles(path, pattern))
                {
                    string temp = Path.GetFileNameWithoutExtension(fn);
                    Dictionary<string, string> keyValuePairs = FDMobileDefinitionReader(fn, ref reason);
                    ;
                    int code = int.Parse(keyValuePairs["code"]);
                    mobile_defintion_lookup.Add(code, keyValuePairs);
                }
                ;
                #endregion  Get file name array to process
            }
            int num_spawners = 0;
            foreach (Item item in SortedList)
                if (item != null && !item.Deleted && item.Parent == null)
                    if (item is Spawner spawner)
                        if (spawner.TemplateMobileDefinition != 0)
                        {
                            num_spawners++;
                            System.Diagnostics.Debug.Assert(mobile_defintion_lookup.ContainsKey(spawner.TemplateMobileDefinition));
                            System.Diagnostics.Debug.Assert(mobile_defintion_lookup[spawner.TemplateMobileDefinition].ContainsKey("compiled_mobile_props"));
                            System.Diagnostics.Debug.Assert(mobile_defintion_lookup[spawner.TemplateMobileDefinition].ContainsKey("compiled_mobile_skills"));
                            System.Diagnostics.Debug.Assert(mobile_defintion_lookup[spawner.TemplateMobileDefinition].ContainsKey("type_name"));

                            Dictionary<string, string> keyValuePairs = mobile_defintion_lookup[spawner.TemplateMobileDefinition];

                            string compiled_mobile_props = keyValuePairs["compiled_mobile_props"];
                            string compiled_mobile_skills = keyValuePairs["compiled_mobile_skills"];
                            string type_name = keyValuePairs["type_name"];

                            Type type = ScriptCompiler.FindTypeByFullName(type_name);
                            System.Diagnostics.Debug.Assert(type != null);

                            object o = Activator.CreateInstance(type);
                            System.Diagnostics.Debug.Assert(o != null);

                            Utility.CopyMobileProperties(o as Mobile, compiled_mobile_props);
                            Utility.CopyMobileSkills(o as Mobile, compiled_mobile_skills);

                            // okay, the mobile of 'type' has been created and his properties set to that of the template.
                            //  Now we find his backpack (if any,) and layers (clothing, weapons, etc, (if any))
                            //foreach (Item search in World.Items.Values)
                            foreach (Item search in SortedList)
                                if (search is LinkPack linkpack)
                                {
                                    if (linkpack.Link == spawner.TemplateMobileDefinition)
                                    {
                                        #region unpack the linkpack to find the creatures backpack and any equipment (an EquipmentPack)
                                        EquipmentPack equipmentPack = null;
                                        Backpack backpack = null;
                                        foreach (Item lp_item in linkpack.Items)
                                            if (lp_item.GetType() == typeof(EquipmentPack))
                                                equipmentPack = (EquipmentPack)lp_item;
                                            else if (lp_item.GetType() == typeof(Backpack))
                                                backpack = (Backpack)lp_item;

                                        Mobile m = o as Mobile;
                                        if (backpack != null)
                                        {
                                            if (m.Backpack != null)
                                                m.Backpack.Delete();
                                            m.AddItem(backpack);
                                        }

                                        if (equipmentPack != null)
                                        {
                                            Utility.WipeLayers(m);
                                            List<Item> ep_list = new List<Item>(equipmentPack.Items);
                                            foreach (Item ep_item in ep_list)
                                                m.AddItem(ep_item);
                                            equipmentPack.Delete();
                                        }
                                        linkpack.Delete();
                                        #endregion unpack the linkpack to find the creatures backpack and any equipment (an EquipmentPack)
                                        ;
                                        // okay, creature is unpacked. Now we just
                                        //  Move him to int map storage (happens automatically)
                                        //  Set the TemplayeMobile of the spawner
                                        //  Clear the TemplateMobileDefinition flag as we are now fully reconstructed
                                        spawner.TemplateMobile = m;
                                        spawner.TemplateMobileDefinition = 0;
                                    }
                                }
                        }

            System.Diagnostics.Debug.Assert(mobile_defintion_lookup.Count == num_spawners);
            #endregion Reconstruct template mobiles

            #region Convert Event Teleporters, SunGates, etc. to permanent, except those that are triggered
            {   // assume only TriggerRelays trigger event objects.
                List<Item> TriggerRelayLinks = new();
                foreach (Item ix in SortedList)
                    if (ix is TriggerRelay tr)
                        TriggerRelayLinks.AddRange(tr.Links);

                List<Item> dupedItems = SortedList;
                foreach (Item item in dupedItems)
                {
                    if (!TriggerRelayLinks.Contains(item))  // only convert those that are NOT triggered
                    {
                        if (Utility.IsEventObject(item))
                        {   // Make Event Object Permanent (MEOP)
                            Utility.Meop(e.Mobile, item, activate: true, reason: out reason);
                            e.Mobile.SendMessage(reason);
                        }
                    }
                    else
                        ;
                }
                ;
            }
            #endregion Convert Event Teleporters, SunGates, etc. to permanent, except those that are triggered

            // I had some trouble with mass amounts of items being restored at once. I believe this was because the client was overwhelmed.
            //  This timer moves each parent item to world whereby updating the clients packet understanding in a reasonable manner.
            // Note: We allow the changing of maps here, we use the caller's map. So if we backed up in Felucca, can be restored in Trammel
            // Next: enhancement: hand back a list of top level objects so that can be offset, allowing to move this capture around.
            e.Mobile.SendMessage("Moving to world...");
            foreach (Item item in SortedList)
                if (item != null && !item.Deleted && item.Parent == null)
                    Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.05, 1.0)), new TimerStateCallback(UpdateTick), new object[] { item, e.Mobile.Map, linkMapper, AddonDatabase });
        }
        private static void UpdateTick(object state)
        {
            object[] aState = (object[])state;
            if (aState == null || aState.Length != 4) return;   // error
            Item item = aState[0] as Item;
            if (item == null || item.Deleted) return;           // error
            Map map = (aState[1] as Map) ?? item.Map;
            if (map == null) return;                            // error
            Dictionary<int, int> AddonMapper = (aState[2] as Dictionary<int, int>);
            if (AddonMapper == null) return;                    // error
            Dictionary<int, List<AddonFields>> AddonDatabase = (aState[3] as Dictionary<int, List<AddonFields>>);
            if (AddonDatabase == null) return;                  // error

            // unset the 'backed up' bit
            item.SetItemBool(ItemBoolTable.BSBackedUp, false);

            #region Special item processing
            #region CustomRegionControl - need to set region map
            if (item is CustomRegionControl)
                PatchCustomRegionControl(item, map);
            #endregion CustomRegionControl - need to set region map

            #region Addons - how do we need to build them?
            if (item is BaseAddon)
            {
                if (item is BaseAddon && !item.GetItemBool(ItemBoolTable.BSPackOnly))
                {
                    AddonBuilder(item, AddonMapper, AddonDatabase, map, pack_only: false);
                    // no further processing needed
                    return;
                }

                item = AddonBuilder(item, AddonMapper, AddonDatabase, map, pack_only: true);
            }
            #endregion Addons - how do we need to build them?
            #endregion Special item processing

            // move to world
            if (item.GetItemBool(Item.ItemBoolTable.IsIntMapStorage))
                item.MoveToIntStorage();
            else
                item.MoveToWorld(item.Location, map);
        }
        private static void PatchCustomRegionControl(Item item, Map map)
        {
            if (item is CustomRegionControl)
            {
                CustomRegionControl crc = item as CustomRegionControl;
                Server.Regions.CustomRegion cr = crc.CustomRegion;
                if (cr != null)
                {
                    cr.Registered = false;
                    cr.Map = map;
                    cr.Registered = true;
                }
            }
        }
        private static Item AddonBuilder(Item item, Dictionary<int, int> addonMapper, Dictionary<int, List<AddonFields>> addonDatabase, Map map, bool pack_only)
        {
            try
            {
                // get the serial the addon was saved with
                object o = addonMapper.FirstOrDefault(e => e.Value == item.Serial).Key;
                System.Diagnostics.Debug.Assert(addonMapper.FirstOrDefault(e => e.Value == item.Serial).Key != 0);

                int packaged_serial = (int)o;
                BaseAddon baseAddon = item as BaseAddon;

                // We don't use the supplied addon components, but rather build the addon from our addonDatabase
                System.Diagnostics.Debug.Assert(addonDatabase.ContainsKey(packaged_serial));
                #region XmlAddon Hack
                if (baseAddon.Components.Count > 0)
                    for (int i = baseAddon.Components.Count - 1; i >= 0; i--)
                    {
                        try
                        {
                            AddonComponent ac = baseAddon.Components[i] as AddonComponent;
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogException(ex);
                        }
                        baseAddon.Components.RemoveAt(i);
                    }
                #endregion XmlAddon Hack
                foreach (var record in addonDatabase[packaged_serial])
                {
                    AddonComponent ac = new AddonComponent(record.ItemID);
                    ac.BoolTable = record.BoolTable;    // capture any special flags;
                    ac.Hue = record.Hue;                // XmlAddon
                    ac.Name = record.Name;              // XmlAddon
                    ac.Light = record.Light;            // XmlAddon
                    baseAddon.AddComponent(ac, record.Offset.X, record.Offset.Y, record.Offset.Z);
                }

                if (!pack_only)
                    baseAddon.MoveToWorld(baseAddon.Location, map);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            return item;
        }
        #endregion Load Object Array

        #region Tools
        private enum FDNameType
        {
            ItemsPattern,
            BaseFolder,
            Rollback,
            Lookup,
            Format,
            FormatError,
            Addons,
            MobileDefinition,
            MobileDefinitionPattern,
            BackupTrace,
            RestoreTrace,
            RestoreObjectUnavailable
        }
        private static string FDFileNames(FDNameType mode, string base_name, string filename = null)
        {
            string DBFolder = "./" + base_name + ".fddb";
            string path_name = null;
            try
            {
                if (mode != FDNameType.BaseFolder)
                    if (!Directory.Exists(DBFolder))
                        Directory.CreateDirectory(DBFolder);

                if (mode == FDNameType.MobileDefinition)
                {
                    path_name = DBFolder + "/" + filename + "_mobile_definition.bin";
                }
                else if (mode == FDNameType.MobileDefinitionPattern)
                {
                    path_name = @"*_mobile_definition.*";
                }
                else if (mode == FDNameType.Lookup)
                {
                    path_name = DBFolder + "/" + base_name + "_property_lookup.bin";
                }
                else if (mode == FDNameType.Rollback)
                {
                    path_name = DBFolder + "/" + base_name + "_rollback.bin";
                }
                else if (mode == FDNameType.BackupTrace)
                {
                    path_name = DBFolder + "/" + base_name + "_backup_trace.txt";
                }
                else if (mode == FDNameType.RestoreTrace)
                {
                    path_name = DBFolder + "/" + base_name + "_restore_trace.txt";
                }
                else if (mode == FDNameType.FormatError)
                {
                    path_name = DBFolder + "/" + base_name + "_format_errors.txt";
                }
                else if (mode == FDNameType.RestoreObjectUnavailable)
                {
                    path_name = DBFolder + "/" + base_name + "_unavailable_errors.txt";
                }
                else if (mode == FDNameType.ItemsPattern)
                {
                    path_name = base_name + @"_Item*.*";
                }
                else if (mode == FDNameType.BaseFolder)
                {   // don't create
                    path_name = DBFolder + "/";
                }
                else if (mode == FDNameType.Format)
                {
                    path_name = DBFolder + "/" + base_name + "_format.bin"; ;
                }
                else if (mode == FDNameType.Addons)
                {
                    path_name = DBFolder + "/" + base_name + "_addons.bin"; ;
                }
            }
            catch
            {
                return null;
            }
            return path_name;
        }
        private static bool CheckedFiltered(object obj)
        {
            if (obj is Mobiles.PlayerMobile) return true;          // ignore the <target> mobile (staff member)
            if (obj is Mobile) return true;                        // for now, ignore all mobiles
            if (obj is Corpse c && !c.StaticCorpse) return true;   // ignore all but static corpses
            if (obj is Corpse) return true;                        // static corpses are on spawners, so leave them
            return false;
        }
        #endregion Tools

        public class FDRegionControl : CustomRegionControl
        {
            private DateTime m_IgnoreBefore;
            [CommandProperty(AccessLevel.GameMaster, AccessLevel.GameMaster)]
            public DateTime IgnoreBefore { get { return m_IgnoreBefore; } set { m_IgnoreBefore = value; } }
            public override string DefaultName { get { return "FD System Region Control"; } }

            [Constructable]
            public FDRegionControl()
            : base()
            {
                Movable = true;
                Visible = true;
                if (this.CustomRegion != null)
                    this.CustomRegion.Name = "FDCapture region";
            }
            public FDRegionControl(Serial serial) : base(serial) { }
            public override void OnSingleClick(Mobile m)
            {
                base.OnSingleClick(m);
                LabelTo(m, string.Format("({0})", CustomRegion.Map));
            }
            public override Item Dupe(int amount)
            {   // when duping a CustomRegionControl, we don't actually want the region itself as it's already
                //  been 'registered' with its own UId.
                // The region carries all the following info, which we will need for our dupe
                FDRegionControl new_crc = new();
                if (CustomRegion != null)
                {
                    Utility.CopyProperties(new_crc.CustomRegion, CustomRegion);
                    new_crc.CustomRegion.Coords = new(CustomRegion.Coords);
                }
                return base.Dupe(new_crc, amount);
            }
            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);
                int version = 0;
                writer.Write(0);

                // version 0
                writer.Write(m_IgnoreBefore);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        m_IgnoreBefore = reader.ReadDateTime();
                        break;
                }

            }
        }
        public class FDRegionMarker : Item
        {
            [Constructable]
            public FDRegionMarker()
                : base(0x1F14) // recall rune graphic
            {
                Movable = false;
                Hue = 33;
                Name = "FDRegionMarker";
            }
            public FDRegionMarker(Serial serial) : base(serial) { }
            public override void OnSingleClick(Mobile from)
            {
                LabelTo(from, string.Format(@"FDRegionMarker"));
                LabelTo(from, string.Format(@"{0}/{1}", this.Location, this.Map));
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);
                int version = 0;
                writer.Write(0);

            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        break;
                }

            }
        }
        #region FDFormat - Removes all instantiated objects from named database
        public static void OnFDFormat(CommandEventArgs e)
        {
            bool result = false;
            string reason = string.Empty;
            int handle = 0;

            #region Process Arguments
            if (e.Arguments.Length != 1)
            {
                e.Mobile.SendMessage("Usage: [FDFormat <filename>");
                return;
            }
            string filename = e.Arguments[0];
            string database = FDFileNames(FDNameType.BaseFolder, filename);
            if (!Directory.Exists(database))
            {
                e.Mobile.SendMessage($"No database named {filename}");
                e.Mobile.SendMessage("Usage: [FDFormat <filename>");
                return;
            }
            #endregion  Process Arguments

            List<int> FormatSerials = FDFormatReader(filename, ref handle, ref reason);
            if (handle != FDHandle())
            {
                reason = string.Format("FDFormat: Fail. These objects were instantiated on a different server.");
                Console.WriteLine(reason);
                e.Mobile.SendMessage(reason);
            }
            if (FormatSerials == null)
            {
                System.Diagnostics.Debug.Assert(false);
                Console.WriteLine("OnFDFormat: {0}", reason);
                return;
            }
            else if (FormatSerials.Count == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                Console.WriteLine("OnFDFormat: {0}", reason);
                return;
            }
            else
            {   // delete all the objects
                string header = "** List of Serials for which there is no in-game item **" + Environment.NewLine +
                    "This is often ok as some of these may be LinkPack and/or EquipmentPack. These are used to recreate template mobiles." + Environment.NewLine +
                    "Other possibilities can be rubbish laying on the ground during capture like arrows and stuff which have since decayed.";
                FDFormatErrorWriter(filename, header, begin: true);
                int count = 0;

                #region First see if they exist
                foreach (int serial in FormatSerials)
                {
                    Item item = World.FindItem(serial);
                    if (item != null)
                        count++;
                }
                if (count == FormatSerials.Count)
                {
                    string text = string.Format("OnFDFormat: Success. All {0} objects detected", count);
                    FDFormatErrorWriter(filename, text, begin: false);
                    e.Mobile.SendMessage(text);
                }
                else
                {
                    string text = string.Format("OnFDFormat: Warning. Only {0} objects found of {1}", count, FormatSerials.Count);
                    FDFormatErrorWriter(filename, text, begin: false);
                    e.Mobile.SendMessage(text);
                }
                #endregion First see if they exist

                #region Now do the actual delete
                count = 0;
                foreach (int serial in FormatSerials)
                {
                    Item item = World.FindItem(serial);
                    //System.Diagnostics.Debug.Assert(item != null);
                    if (item != null)
                    {
                        count++;
                        item.Delete();
                    }
                    else
                        FDFormatErrorWriter(filename, string.Format($"0x{serial:X}"), begin: false);
                }
                if (count == FormatSerials.Count)
                {
                    string text = string.Format("OnFDFormat: Success. All {0} objects deleted", count);
                    FDFormatErrorWriter(filename, text, begin: false);
                    e.Mobile.SendMessage(text);
                }
                else
                {
                    string text = string.Format("OnFDFormat: Warning. Only {0} objects deleted of {1}", count, FormatSerials.Count);
                    FDFormatErrorWriter(filename, text, begin: false);
                    e.Mobile.SendMessage(text);
                }
                #endregion Now do the actual delete
            }
        }
        #endregion FDFormat - Removes all instantiated objects from named database
        public static void OnFDSpawner(CommandEventArgs e)
        {
            List<string> pairs = new();
            bool result = false;
            string reason = string.Empty;
            int handle = 0;
            string filename = null;
            List<string> Arguments = new(e.Arguments);

            #region Process Arguments
            if (Arguments.Count > 0 && Arguments[0].Equals("-target", StringComparison.OrdinalIgnoreCase))
            {
                if (Arguments.Count < 3)
                    goto usage;

                for (int ix = 0; ix < Arguments.Count; ix++)
                    pairs.Add(Arguments[ix]);

                // remove -target
                pairs.RemoveAt(0);

                if (pairs.Count % 2 != 0)
                    goto usage;

                e.Mobile.SendMessage("Target the FDRegionControl.");
                e.Mobile.Target = new ProcessFDRegionControlTarget(pairs);
                return;
            }
            else
            {
                if (Arguments.Count < 3)
                    goto usage;

                for (int ix = 0; ix < Arguments.Count; ix++)
                    pairs.Add(Arguments[ix]);

                filename = pairs[0];
                pairs.RemoveAt(0);

                if (pairs.Count % 2 != 0)
                    goto usage;

                string database = FDFileNames(FDNameType.BaseFolder, filename);
                if (!Directory.Exists(database))
                {
                    e.Mobile.SendMessage($"No database named {filename}");
                    goto usage;
                    return;
                }
            }
            #endregion  Process Arguments

            List<int> TheSerials = FDFormatReader(filename, ref handle, ref reason);
            if (handle != FDHandle())
            {
                reason = string.Format("FDSpawner: Fail. These objects were instantiated on a different server.");
                Console.WriteLine(reason);
                e.Mobile.SendMessage(reason);
            }
            if (TheSerials == null)
            {
                System.Diagnostics.Debug.Assert(false);
                Console.WriteLine("FDSpawner: {0}", reason);
                return;
            }
            else if (TheSerials.Count == 0)
            {
                System.Diagnostics.Debug.Assert(false);
                Console.WriteLine("FDSpawner: {0}", reason);
                return;
            }
            else
            {
                int failed = 0;
                int succeeded = 0;
                int spawners = 0;
                foreach (Serial s in TheSerials)
                {
                    Item item = null;
                    if ((item = World.FindItem(s)) != null)
                    {
                        if (item is Spawner spawner)
                        {
                            spawners++;
                            for (int ix = 0; ix < pairs.Count; ix += 2)
                            {
                                string keyword = pairs[ix];
                                string value = pairs[ix + 1];
                                if (spawner.SetObjectProp(keyword, value, message: false))
                                    succeeded++;
                                else
                                    failed++;
                            }
                        }
                    }

                }

                e.Mobile.SendMessage("FDSpawner: Processed {0} spawners. {1} succeeded, {2} failed.", spawners, succeeded, failed);

                return;
            }
        usage:
            e.Mobile.SendMessage("Usage: [FDSpawner <filename> <keyword> <value>...");
            e.Mobile.SendMessage("Usage: [FDSpawner -target <target FDRegionControl> <keyword> <value>...");
        }
        public class ProcessFDRegionControlTarget : Targeting.Target
        {
            List<string> Pairs;
            public ProcessFDRegionControlTarget(object o)
                : base(12, true, Targeting.TargetFlags.None)
            {
                Pairs = o as List<string>;
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is FDRegionControl fdr)
                {
                    List<Rectangle2D> rects = GetFDRegionControlRects(fdr);

                    List<Item> WhatWeGot = new();                   // surface items we found
                    #region WhatWeGot
                    Dictionary<Item, byte> QuickTable = new();
                    foreach (var rect in rects)
                    {
                        IPooledEnumerable eable = fdr.Map.GetItemsInBounds(rect);
                        foreach (object obj in eable)
                        {
                            if (CheckedFiltered(obj))
                                continue;

                            if (obj is Item old_item)
                            {                                               // respect the IgnoreBefore date in the FDRegionControl
                                if (fdr != null && old_item.Created < fdr.IgnoreBefore)
                                    continue;
                                else if (old_item is FDRegionControl)       // ignore FDRegionControls
                                    continue;
                                else if (old_item is FDRegionMarker)        // ignore FDRegionMarkers
                                    continue;
                                else if (QuickTable.ContainsKey(old_item))  // since our regions may overlap, we ignore duplicate items
                                    continue;
                                else if (IsCamp(old_item))                  // camps are spawned. Don't collect spawned items
                                    continue;
                                else if (IsSpawned(old_item))               // Don't collect spawned items
                                    continue;
                                else
                                    QuickTable.Add(old_item, 0);
                            }
                        }
                        eable.Free();

                        WhatWeGot.AddRange(QuickTable.Keys);
                    }
                    #endregion WhatWeGot

                    int failed = 0;
                    int succeeded = 0;
                    int spawners = 0;
                    foreach (Item item in WhatWeGot)
                        if (item is Spawner spawner)
                        {
                            spawners++;
                            for (int ix = 0; ix < Pairs.Count; ix += 2)
                            {
                                string keyword = Pairs[ix];
                                string value = Pairs[ix + 1];
                                if (spawner.SetObjectProp(keyword, value, message: false))
                                    succeeded++;
                                else
                                    failed++;
                            }
                        }

                    from.SendMessage("FDSpawner: Processed {0} spawners. {1} succeeded, {2} failed.", spawners, succeeded, failed);
                }
                else
                    from.SendMessage("That is not an FDRegionControl");
            }
        }
        public class FDSaveMobileCommand : BaseCommand
        {
            public FDSaveMobileCommand()
            {
                AccessLevel = AccessLevel.Administrator;
                Supports = CommandSupport.AllMobiles | CommandSupport.AllNPCs;
                Commands = new string[] { "FDSaveMobile" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "FDSaveMobile <filename>";
                Description = "Dupes a targeted mobile by writing its MobileDefinition to disk.";
            }
            private string m_Filename = null;
            public override void Execute(CommandEventArgs e, object obj)
            {
                m_Filename = e.GetString(0);
                if (string.IsNullOrEmpty(m_Filename))
                {
                    LogFailure(Usage);
                    return;
                }
                DoDupe(e.Mobile, obj);
            }

            private void DoDupe(Mobile from, object targ)
            {
                bool done = false;
                if (targ is Mobile source)
                {
                    try
                    {
                        #region Compile our mobile and create set of keyword value pares to write
                        string compiled_mobile_props = Utility.CompileMobileProperties(source);
                        string compiled_mobile_skills = Utility.CompileMobileSkills(source);
                        string type_name = source.GetType().FullName;
                        Dictionary<string, string> keyValuePairs = new Dictionary<string, string>()
                        {

                            {"code", 0.ToString() },
                            {"type_name", type_name },
                            {"compiled_mobile_props", compiled_mobile_props },
                            {"compiled_mobile_skills", compiled_mobile_skills },
                        };
                        #endregion Compile our mobile and create set of keyword value pares to write

                        #region Write to our mobile reconstruction database
                        FDMobileDefinitionWriterRaw(m_Filename, keyValuePairs);
                        #endregion  Write to our mobile reconstruction database
                        done = true;
                    }
                    catch (Exception ex)
                    {
                        LogFailure(string.Format("{0}", ex.ToString()));
                        return;
                    }
                }
                else
                {
                    LogFailure("You can only dupe mobiles.");
                    return;
                }

                if (!done)
                    LogFailure("Unable to dupe.");
                else
                    AddResponse("Done.");
            }
        }
        public class FDRestoreMobileCommand : BaseCommand
        {
            public FDRestoreMobileCommand()
            {
                AccessLevel = AccessLevel.Administrator;
                Supports = CommandSupport.AllMobiles | CommandSupport.AllNPCs;
                Commands = new string[] { "FDRestoreMobile" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "FDRestoreMobile <filename>";
                Description = "Restores a targeted mobile by reading and applying a MobileDefinition file.";
            }
            private string m_Filename = null;
            public override void Execute(CommandEventArgs e, object obj)
            {
                m_Filename = e.GetString(0);
                if (string.IsNullOrEmpty(m_Filename))
                {
                    LogFailure(Usage);
                    return;
                }
                DoRestore(e.Mobile, obj);
            }

            private void DoRestore(Mobile from, object targ)
            {
                bool done = false;
                if (targ is Mobile source)
                {
                    try
                    {
                        string reason = string.Empty;
                        Dictionary<int, Dictionary<string, string>> mobile_defintion_lookup = new();
                        {
                            if (!m_Filename.EndsWith(".mdf", StringComparison.OrdinalIgnoreCase))
                                m_Filename = m_Filename + ".mdf";

                            Dictionary<string, string> keyValuePairs = FDMobileDefinitionReader(m_Filename, ref reason);
                            if (!string.IsNullOrEmpty(reason))
                            {
                                LogFailure(reason);
                                return;
                            }
                            ;
                            int code = int.Parse(keyValuePairs["code"]);
                            mobile_defintion_lookup.Add(code, keyValuePairs);
                        }
                        ;
                        {
                            Dictionary<string, string> keyValuePairs = mobile_defintion_lookup[0];

                            string compiled_mobile_props = keyValuePairs["compiled_mobile_props"];
                            string compiled_mobile_skills = keyValuePairs["compiled_mobile_skills"];
                            string type_name = keyValuePairs["type_name"];

                            Utility.CopyMobileProperties(source, compiled_mobile_props);
                            Utility.CopyMobileSkills(source, compiled_mobile_skills);
                        }

                        done = true;
                    }
                    catch (Exception ex)
                    {
                        LogFailure(string.Format("{0}", ex.ToString()));
                        return;
                    }
                }
                else
                {
                    LogFailure("You can only dupe mobiles.");
                    return;
                }

                if (!done)
                    LogFailure("Unable to dupe.");
                else
                    AddResponse("Done.");
            }
        }
        #region FDToIntMap
        public static void OnFDArchive(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the FDRegionControl you wish to archive.");
            e.Mobile.Target = new FDRegionControlTarget(mode: FDArcMode.Write);
        }
        public static void OnFDUnArchive(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the FDRegionControl you wish to unarchive.");
            e.Mobile.Target = new FDRegionControlTarget(mode: FDArcMode.Read);
        }
        public enum FDArcMode
        {
            Read,
            Write
        }
        public class FDRegionControlTarget : Target
        {
            FDArcMode m_Mode;
            public FDRegionControlTarget(FDArcMode mode)
                : base(12, true, TargetFlags.None)
            {
                m_Mode = mode;
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is FDFile.FDRegionControl rc)
                {
                    if (m_Mode == FDArcMode.Read)
                    {
                        Console.WriteLine("Reading archive {0}...", rc.CustomRegion.Name);
                        Utility.TimeCheck tc = new Utility.TimeCheck();
                        tc.Start();

                        string reason = string.Empty;
                        List<IEntity> entities = ReadArcFile(rc.CustomRegion.Name, ref reason);
                        if (entities == null)
                        {
                            Console.WriteLine("Failed to read {0} with reason {1}", rc.CustomRegion.Name, reason);
                            return;
                        }

                        int failed = 0;
                        foreach (var ent in entities)
                            if (ent == null)
                                failed++;
                            else
                            {
                                if (ent is Mobile m)
                                    m.MoveToWorld(m.Location, rc.CustomRegion.Map);
                                else if (ent is Item i)
                                {
                                    i.MoveToWorld(i.Location, rc.CustomRegion.Map);
                                    if (i is CustomRegionControl crc)
                                        crc.Registered = true;
                                }
                            }

                        tc.End();
                        Console.WriteLine($"Read {entities.Count} entities from {rc.CustomRegion.Name}, {failed} failed in {tc.TimeTaken}");
                    }
                    else if (m_Mode == FDArcMode.Write)
                    {
                        Console.WriteLine("Archiving {0}...", rc.CustomRegion.Name);
                        Utility.TimeCheck tc = new Utility.TimeCheck();
                        tc.Start();

                        List<Rectangle2D> rects = new();
                        foreach (var rect in rc.CustomRegion.Coords)
                            rects.Add(new Rectangle2D(rect.Start, rect.End));

                        List<Mobile> mobiles = new List<Mobile>();
                        List<Item> items = new List<Item>();
                        foreach (Rectangle2D rectangle in rects)
                        {
                            IPooledEnumerable eable = rc.CustomRegion.Map.GetObjectsInBounds(rectangle);
                            foreach (object o in eable)
                                if (o is Mobile m && !m.Player)
                                    mobiles.Add(o as Mobile);
                                else if (o is Item i && i is not FDRegionControl)
                                    items.Add(o as Item);
                            eable.Free();
                        }

                        // we split the lists since I'm not sure the comparer is there for IEntity
                        mobiles = mobiles.Distinct().ToList();
                        items = items.Distinct().ToList();

                        List<IEntity> entities = new List<IEntity>();
                        entities.AddRange(mobiles);
                        entities.AddRange(items);

                        WriteArcFile(rc.CustomRegion.Name, entities);

                        foreach (IEntity o in entities)
                            if (o is Mobile)
                                (o as Mobile).MoveToIntStorage(PreserveLocation: true);
                            else if (o is Item)
                            {
                                (o as Item).MoveToIntStorage(PreserveLocation: true);
                                if (o is CustomRegionControl crc)
                                    crc.Registered = false;
                            }

                        tc.End();
                        Console.WriteLine("Archived {0} entities in {1}", entities.Count, tc.TimeTaken);
                    }
                }
                else
                    from.SendMessage("That is not an FDRegionControl.");
            }

            public static List<IEntity> ReadArcFile(string filename, ref string reason)
            {
                List<IEntity> list = new();
                string folder = filename + "_IntMap";
                string archive_name = Path.Combine(folder, filename + ".arc");
                if (!File.Exists(archive_name))
                {
                    reason = string.Format($"{archive_name} does not exist.");
                    return null;
                }
                Console.WriteLine(string.Format("{0} Reading...", filename));

                try
                {
                    BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(string.Format("{0}", archive_name), FileMode.Open, FileAccess.Read)));

                    int count = reader.ReadInt();

                    for (int ix = 0; ix < count; ix++)
                        list.Add(World.FindEntity(reader.ReadInt()));

                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Error reading {0}", filename));
                    Console.WriteLine(ex.ToString());
                    return null;
                }

                Console.WriteLine(string.Format("Done reading {0}", filename));
                return list;
            }
            public static void WriteArcFile(string filename, List<IEntity> entities)
            {
                string folder = filename + "_IntMap";
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string archive_name = Path.Combine(folder, filename + ".arc");

                Console.WriteLine(string.Format("{0} Saving...", filename));
                try
                {
                    BinaryFileWriter writer = new BinaryFileWriter(string.Format("{0}", archive_name), true);

                    writer.Write(entities.Count);

                    foreach (var ent in entities)
                        writer.Write(ent.Serial);

                    writer.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Error writing {0}", filename));
                    Console.WriteLine(ex.ToString());
                    return;
                }

                Console.WriteLine(string.Format("Done writing {0}", filename));
            }
        }
        #endregion FDToIntMap
    }
}