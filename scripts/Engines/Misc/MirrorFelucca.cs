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

/* Scripts\Engines\Misc\MirrorFelucca.cs
 * CHANGELOG
 *  4/23/2026, Adam
 *      Initial version
 */

using Server.Diagnostics;
using Server.Engines.ChampionSpawn;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Multis.StaticHousing;
using Server.Network;
using Server.Regions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using static Server.Multis.StaticHousing.StaticHouseHelper;

namespace Server.Engines
{
    public static class MirrorFelucca
    {
        public static string DataFileName { get { return Path.Combine(Core.DataDirectory, "StaticRegions.xml"); } }
        public static string SaveFileName = "Saves/StaticRegions.xml";

        public static int MirrorToTrammel(CommandEventArgs e)
        {
            Mobile? m = e?.Mobile;
            Console.WriteLine("MirrorFelucca: starting Felucca -> Trammel mirror...");

            // --- Setup Phase ---
            Console.WriteLine("MirrorFelucca: capturing Custom Housing Area...");
            CustomHousingAreaSnapshot cha = CaptureCustomHousingArea();
            Console.WriteLine("MirrorFelucca: captured {0} custom house(s).", cha.Houses.Count);

            Console.WriteLine("MirrorFelucca: stashing New Player Starting Area...");
            m_NewPlayerStartingAreaStash = StashNewPlayerStartingArea();
            Console.WriteLine("MirrorFelucca: stashed {0} object(s) from the New Player Starting Area.", m_NewPlayerStartingAreaStash.Count);

            Console.WriteLine("MirrorFelucca: calculating ineligible items...");
            HashSet<Item> ineligibleItems = CalcIneligibleItems();
            Console.WriteLine("MirrorFelucca: {0} ineligible item(s) found.", ineligibleItems.Count);

            bool newPlayerStartingAreaDisabled = false;
            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.NewPlayerStartingArea))
            {
                Console.WriteLine("MirrorFelucca: disabling Trammel new player starting area...");
                newPlayerStartingAreaDisabled = true;
                CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);
            }

            Console.WriteLine("MirrorFelucca: disabling known Trammel controllers...");
            int controllersUnregistered = DisableKnownTrammelControllers();
            Console.WriteLine("MirrorFelucca: {0} controller(s) disabled.", controllersUnregistered);

            Console.WriteLine("MirrorFelucca: unregistering Trammel regions...");
            int regionsUnregistered = UnregisterTrammelRegions();
            Console.WriteLine("MirrorFelucca: {0} region(s) unregistered.", regionsUnregistered);

            Console.WriteLine("MirrorFelucca: wiping Trammel items...");
            int trammelItemsDeleted = WipeTrammel();
            Console.WriteLine("MirrorFelucca: {0} Trammel item(s) deleted.", trammelItemsDeleted);

            Console.WriteLine("MirrorFelucca: loading mirrored Trammel static regions...");
            int regionsBefore = Region.Regions.Count;
            LoadRegions();
            int regionsLoaded = Region.Regions.Count - regionsBefore;
            Console.WriteLine("MirrorFelucca: {0} Trammel static region(s) loaded.", regionsLoaded);

            // --- Setup Summary (before long mirror operation) ---
            MirrorSetupResult setup = new MirrorSetupResult
            {
                CustomHousesCaptured = cha.Houses.Count,
                StartingAreaStashed = m_NewPlayerStartingAreaStash.Count,
                IneligibleItems = ineligibleItems.Count,
                NewPlayerStartingAreaDisabled = newPlayerStartingAreaDisabled,
                ControllersUnregistered = controllersUnregistered,
                RegionsUnregistered = regionsUnregistered,
                RegionsLoaded = regionsLoaded,
                TrammelItemsDeleted = trammelItemsDeleted
            };

            Console.WriteLine("MirrorFelucca: setup phase complete. Beginning item mirror...");

            // --- Long Mirror Operation ---
            Console.WriteLine("MirrorFelucca: copying Felucca items to Trammel...");
            NetState.FlushAll();
            NetState.Pause();

            MirrorResult mirrorResult;
            try
            {
                mirrorResult = FeluccaToTrammel(cha, ineligibleItems);
            }
            finally
            {
                NetState.Resume();
            }

            // --- Final Report ---
            MirrorFeluccaReport.Output(m, setup, mirrorResult);

            return mirrorResult.TotalUpdated;
        }
        public static class MirrorFeluccaReport
        {
            public static void Output(Mobile? m, MirrorSetupResult setup, MirrorResult mirror)
            {
                foreach (string line in BuildLines(setup, mirror))
                {
                    Console.WriteLine("MirrorFelucca: {0}", line);

                    if (m != null)
                        m.SendMessage(line);
                }
            }

            private static IEnumerable<string> BuildLines(MirrorSetupResult setup, MirrorResult mirror)
            {
                yield return "Trammel conversion complete.";
                yield return string.Format("new player starting area disabled: {0}", setup.NewPlayerStartingAreaDisabled);
                yield return string.Format("ineligible items skipped: {0}", setup.IneligibleItems);
                yield return string.Format("controllers disabled: {0}", setup.ControllersUnregistered);
                yield return string.Format("regions unregistered: {0}", setup.RegionsUnregistered);
                yield return string.Format("Trammel static regions loaded: {0}", setup.RegionsLoaded);
                yield return string.Format("items deleted from Trammel: {0}", setup.TrammelItemsDeleted);

                yield return string.Format("items considered: {0}", mirror.ItemsConsidered);
                yield return string.Format("items copied from Felucca: {0}", mirror.ItemsCopied);
                yield return string.Format("items failed: {0}", mirror.ItemsFailed);

                yield return string.Format("custom houses captured: {0}", setup.CustomHousesCaptured);
                yield return string.Format("custom houses rebuilt: {0}", mirror.CustomHousesRebuilt);
                yield return string.Format("starting area objects restored: {0}", mirror.StartingAreaObjectsRestored);

                yield return string.Format("general item references relinked: {0}", mirror.GeneralRelinked);
                yield return string.Format("spawners defragged: {0}", mirror.SpawnersDefragged);
                yield return string.Format("total updated: {0}", mirror.TotalUpdated);
            }
        }
        private static HashSet<Item> CalcIneligibleItems()
        {
            HashSet<Item> list = new();

            // spawned items
            foreach (Spawner s in Spawner.Instances)
            {
                if (s == null)
                    continue;

                var items = s.GetItems();
                if (items != null)
                {
                    foreach (Item item in items)
                        list.Add(item);
                }
            }

            // Champ Engines
            foreach (ChampEngine engine in ChampEngine.Instances)
            {   // these are the alter, platform, skulls, etc.
                DecomposeItems(engine.GetItems(), list);
            }

            // Tents
            foreach (KeyValuePair<BaseHouse, bool> bh in BaseHouse.Instances)
            {   // roof, walls, backpack, bed roll, etc.
                if (bh.Key is Tent tent)
                    DecomposeItems(tent.GetItems(), list);

                if (bh.Key is SiegeTent siegeTent)
                    DecomposeItems(siegeTent.GetItems(), list);
            }

            return list;
        }
        private static void DecomposeItems(IEnumerable<Item> items, HashSet<Item> list)
        {
            if (items == null || list == null)
                return;

            foreach (Item item in items)
                DecomposeItemGraph(item, list);
        }
        private static void DecomposeItemGraph(Item item, HashSet<Item> list)
        {
            if (item == null || item.Deleted)
                return;

            // Always add the item itself: backpack, rug, addon, component, etc.
            if (!list.Add(item))
                return;

            // If this item is not an addon, there is nothing deeper to decompose.
            if (item is not BaseAddon addon || addon.Components == null)
                return;

            foreach (AddonComponent component in addon.Components)
            {
                if (component == null || component.Deleted)
                    continue;

                // Add the component itself.
                DecomposeItemGraph(component, list);

                // Some components point to another addon; decompose that addon too.
                if (component.Addon != null && component.Addon != addon)
                    DecomposeItemGraph(component.Addon, list);
            }
        }
        public static void LoadRegions()
        {
            if (LoadTrammelRegions(SaveFileName, false) == 0)
                LoadTrammelRegions(DataFileName, true);
        }
        public static int LoadTrammelRegions(string fileName, bool initRegistration)
        {
            int count = 0;

            try
            {
                if (!File.Exists(fileName))
                    return 0;

                Console.Write("Loading Trammel static regions from \"{0}\"...\n", Utility.GetShortPath(fileName));

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);

                XmlElement? xmlRoot = xmlDoc["StaticRegions"];

                if (xmlRoot == null)
                    return 0;

                foreach (XmlElement xmlElem in xmlRoot.GetElementsByTagName("region"))
                {
                    if (xmlElem.GetAttribute("type") != "StaticRegion")
                        continue;

                    if (!string.Equals(xmlElem.GetAttribute("map"), "Felucca", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string name = xmlElem.GetAttribute("name");

                    if (ShouldSkipMirroredRegion(name))
                        continue;

                    xmlElem.SetAttribute("map", "Trammel");

                    StaticRegion region = new StaticRegion();
                    region.Load(xmlElem);

                    if (initRegistration)
                        region.Registered = Spawner.ShouldShardEnable(region.ShardConfig);
                    else
                        region.Registered = region.WasRegistered;

                    StaticRegion.XmlDatabase.Add(region);
                    count++;
                }

                Console.WriteLine("Done! Loaded {0} Trammel static region(s).", count);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            return count;
        }
        private static bool ShouldSkipMirroredRegion(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            //if (string.Equals(name, "Jail", StringComparison.OrdinalIgnoreCase))
            //    return true;

            //if (string.Equals(name, "Angel Island", StringComparison.OrdinalIgnoreCase))
            //    return true;

            return false;
        }
        private static int DisableKnownTrammelControllers()
        {
            int count = 0;
            CustomRegionControl? crc;

            crc = World.FindItem(0x401003D1) as CustomRegionControl;
            if (crc != null)
            {
                Debug.Assert(crc.CustomRegion == null || crc.CustomRegion.Map == Map.Trammel);
                //crc.Registered = false;
                Region.RemoveRegion(crc.CustomRegion);
                count++;
            }

            crc = World.FindItem(0x40100E18) as CustomRegionControl;
            if (crc != null)
            {
                Debug.Assert(crc.CustomRegion == null || crc.CustomRegion.Map == Map.Trammel);
                //crc.Registered = false;
                Region.RemoveRegion(crc.CustomRegion);
                count++;
            }

            return count;
        }
        private static int UnregisterTrammelRegions()
        {
            int count = 0;

            Region[] regions = new Region[Region.Regions.Count];
            Region.Regions.CopyTo(regions, 0);

            foreach (Region rx in regions)
            {
                if (rx == null)
                    continue;

                if (rx.Map != Map.Trammel)
                    continue;

                //rx.Unregister();
                Region.RemoveRegion(rx);
                count++;
            }

            return count;
        }
        private static MirrorResult FeluccaToTrammel(CustomHousingAreaSnapshot cha, HashSet<Item> ineligibleItems)
        {
            // Result object capturing metrics for this mirror operation
            MirrorResult result = new MirrorResult();

            // Mapping of original Felucca item serial -> duplicated Trammel item
            // This is the backbone of all post-duplication fixups
            Dictionary<Serial, Item> itemMap = new Dictionary<Serial, Item>();

            HashSet<Type> seenTypeMismatches = new HashSet<Type>();
            HashSet<Type> seenNullDupes = new HashSet<Type>();

            // Snapshot the world items to avoid enumeration issues during mutation
            Item[] items = new Item[World.Items.Values.Count];
            World.Items.Values.CopyTo(items, 0);

            // --- Phase 1: Duplicate eligible items and build remap table ---
            foreach (Item item in items)
            {
                // Skip items that should not be duplicated (ownership, spawners, etc.)
                if (!ShouldCopy(item, ineligibleItems))
                    continue;

                // Track all items we attempt to process
                result.ItemsConsidered++;

                // Temporarily unregister region controllers to avoid duplication side-effects
                bool wasRegistered = UnregisterRegionController(item);

                // Perform the duplication
                Item newItem = Dupe(item);

                // Handle failure case (rare but important for diagnostics)
                if (newItem == null)
                {
                    result.ItemsFailed++;

                    Type t = item.GetType();

                    if (seenNullDupes.Add(t))
                    {
                        Console.WriteLine(
                            "MirrorFelucca ERROR: Dupe returned null for {0} (Serial: {1}, Map: {2}, Location: {3})",
                            item.GetType().FullName,
                            item.Serial,
                            item.Map,
                            item.Location);
                    }

                    RestoreSourceRegistration(item, wasRegistered);
                    continue;
                }

                // Type integrity check: did we get the same type back?
                // Static results are expected for intentional flattening paths, such as
                // AddonComponent -> Static and final visual fallback -> Static.
                if (newItem.GetType() != item.GetType() && newItem is not Static)
                {
                    Type t = item.GetType();

                    if (seenTypeMismatches.Add(t))
                    {
                        Console.WriteLine(
                            "MirrorFelucca WARNING: Type mismatch during Dupe. Source: {0}, Result: {1}",
                            t.FullName,
                            newItem.GetType().FullName);
                    }
                }

                // Successful duplication
                result.ItemsCopied++;

                // Record mapping for later fixup phase
                itemMap[item.Serial] = newItem;

                // Remap item to Trammel and restore region controller state
                RemapItemToTrammel(newItem);
                RestoreRegionControllerRegistration(item, newItem, wasRegistered);
            }

            // --- Phase 2a: Rebuild Custom Housing Area ---
            Console.WriteLine("MirrorFelucca: rebuilding Custom Housing Area...");
            result.CustomHousesRebuilt = RebuildCustomHousingArea(cha);
            Console.WriteLine(
                "MirrorFelucca: rebuilt {0} custom house display(s).",
                result.CustomHousesRebuilt);

            // --- Phase 2b: Restore New Player Starting Area ---
            Console.WriteLine("MirrorFelucca: restoring New Player Starting Area...");
            result.StartingAreaObjectsRestored = RestoreNewPlayerStartingArea(m_NewPlayerStartingAreaStash);
            Console.WriteLine(
                "MirrorFelucca: restored {0} object(s) to the New Player Starting Area.",
                result.StartingAreaObjectsRestored);

            // --- Phase 2c: Fix up logical relationships using remap table ---
            result.GeneralRelinked = RelinkItemReferences(itemMap);

            #region Validate Dupe
            LogHelper logger = new LogHelper("Link Failures.log", overwrite: true, sline: true, quiet: true);
            HashSet<Item> jumpList = new HashSet<Item>();
            foreach (var kvp in itemMap)
            {
                Item? source = World.FindItem(kvp.Key);   // original serial
                Item dest = kvp.Value; // cloned item

                if (!ValidateDupe(source, dest, logger, jumpList))
                {
                    ; // log / break / debug
                }
            }
            PlayerMobile? admin = World.GetAdminAcct() as PlayerMobile;
            admin.JumpIndex = 0;
            admin.JumpList = new ArrayList();
            foreach (var item in jumpList)
                admin.JumpList.Add(item);
            logger.Finish();
            #endregion Validate Dupe

            // --- Phase 2d: Defrag spawners to clean up any fragmentation from item moves during relinking ---
            result.SpawnersDefragged = DefragSpawners(itemMap);

            return result;
        }

        private static Item Dupe(Item item)
        {
            Item? newItem = null;
            if (item is AddonComponent ac)
            {
                newItem = new Static(ac.ItemID); // MirrorFelucca-specific flattening policy
                Utility.CopyProperties(item, newItem);
            }
            else
                newItem = DupeUtility.Dupe(item);

            return newItem;
        }
        private static int RebuildCustomHousingArea(CustomHousingAreaSnapshot cha)
        {
            if (cha == null || cha.Houses == null)
                return 0;

            int rebuilt = 0;

            foreach (CustomHousingHouseSnapshot h in cha.Houses)
            {
                if (string.IsNullOrEmpty(h.HouseBlueprintID))
                    continue;

                // 1. Create StaticHouse from h.HouseBlueprintID
                HouseBlueprint blueprint = StaticHouseHelper.GetBlueprint(h.HouseBlueprintID);
                if (blueprint == null)
                    continue;

                StaticHouse house = new StaticHouse(World.GetAdminAcct(), blueprint);
                if (house == null)
                    continue;

                // 2. Move it to h.HouseLocation on Map.Trammel
                house.MoveToWorld(h.HouseLocation, Map.Trammel);

                // 3. Create PlayerVendor at h.VendorLocation on 
                PlayerVendor pv = new PlayerVendor(World.GetAdminAcct(), house);
                pv.MoveToWorld(h.VendorLocation, Map.Trammel);

                // 4. Set vendor owner/admin/staff-owned/facing south
                pv.IsStaffOwned = true;
                pv.Direction = h.VendorDirection;

                // 5. Add 10 deeds from house.GetDeed() to vendor backpack
                for (int ix = 0; ix < 10; ix++)
                    pv.AddToBackpack(house.GetDeed());

                rebuilt++;
            }

            return rebuilt;
        }
        private static int RelinkItemReferences(Dictionary<Serial, Item> itemMap)
        {
            int count = 0;

            foreach (KeyValuePair<Serial, Item> kvp in itemMap)
            {
                Item? source = World.FindItem(kvp.Key);
                Item dest = kvp.Value;

                if (source == null || source.Deleted || dest == null || dest.Deleted)
                    continue;

                count += RelinkItemReferences(source, dest, itemMap);
            }

            return count;
        }
        private static int RelinkItemReferences(Item source, Item dest, Dictionary<Serial, Item> itemMap)
        {
            int count = 0;

            if (source == null || source.Deleted || dest == null || dest.Deleted)
                return 0;

            PropertyInfo[] props = Utility.ItemRWProperties(source);

            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo prop = props[i];

                try
                {
                    if (prop.GetIndexParameters().Length != 0)
                        continue;

                    CopyableAttribute ca = Utility.GetCA(source.GetType(), prop.Name);

                    if (ca != null && (ca.CopyType == CopyType.DoNotCopy || ca.CopyType == CopyType.DoNotRelink))
                        continue;

                    bool itemProperty = typeof(Item).IsAssignableFrom(prop.PropertyType);
                    bool entityProperty = typeof(IEntity).IsAssignableFrom(prop.PropertyType);

                    if (!itemProperty && !entityProperty)
                        continue;

                    object? sourceValue = prop.GetValue(source, null);

                    if (sourceValue == null)
                        continue;

                    Item? oldTarget = sourceValue as Item;

                    if (oldTarget == null && sourceValue is IEntity entity)
                        oldTarget = entity as Item;

                    if (oldTarget == null || oldTarget.Deleted)
                        continue;

                    if (!itemMap.TryGetValue(oldTarget.Serial, out Item? newTarget))
                        continue;

                    if (newTarget == null || newTarget.Deleted)
                        continue;

                    if (newTarget.Map != dest.Map)
                        continue;

                    if (!prop.PropertyType.IsAssignableFrom(newTarget.GetType()))
                        continue;

                    prop.SetValue(dest, newTarget, null);
                    count++;
                }
                catch
                {
                    // Ignore reflection/property side effects for now.
                }
            }

            return count;
        }
        private static int DefragSpawners(Dictionary<Serial, Item> itemMap)
        {
            int defragged = 0;

            if (itemMap == null)
                return 0;

            foreach (Item item in itemMap.Values)
            {
                if (item == null || item.Deleted)
                    continue;

                if (item is ChestItemSpawner cis)
                {
                    cis.Defrag();
                    defragged++;
                }
                else if (item is Spawner spawner)
                {
                    spawner.Defrag();
                    defragged++;
                }
            }

            return defragged;
        }
        private static bool ShouldCopy(Item item, HashSet<Item> ineligibleItems)
        {
            if (item == null || item.Deleted)
                return false;

            if (ineligibleItems.Contains(item))
                return false;

            if (item.Map != Map.Felucca)
                return false;

            if (item.CampComponent)
                return false;

            if (item.RootParent is Mobile)
                return false;

            if (item is BaseHouse || BaseHouse.FindHouseAt(item, parcel: true) != null)
                return false;

            if (item is BaseBoat || BaseBoat.FindBoatAt(item) != null)
                return false;

            // Static region controllers remain authoritative on Felucca only.
            if (item is StaticRegionControl)
                return false;

            if (item is Corpse)
                return false;

            if (item is TentBackpack)
                return false;

            if (item is BaseUnusualContainer)
                return false;

            if (item is HouseSign hs && hs.Structure is BaseHouse)
                return false;

            // these are generated dynamically at the end of a quest. we don't want to accidentially pick one up.
            if (item is AIEscapeExit)
                return false;

            return true;
        }
        private static bool UnregisterRegionController(Item item)
        {
            switch (item)
            {
                case CustomRegionControl crc:
                    bool crcWasRegistered = crc.Registered;
                    crc.Registered = false;
                    return crcWasRegistered;

                case StaticRegionControl src:
                    bool srcWasRegistered = src.Registered;
                    src.Registered = false;
                    return srcWasRegistered;

                default:
                    return false;
            }
        }
        private static void RestoreSourceRegistration(Item source, bool wasRegistered)
        {
            switch (source)
            {
                case CustomRegionControl crc:
                    crc.Registered = wasRegistered;
                    break;

                case StaticRegionControl src:
                    src.Registered = wasRegistered;
                    break;
            }
        }
        private static void RemapItemToTrammel(Item item)
        {
            item.Map = Map.Trammel;

            if (item is Teleporter tele)
                tele.MapDest = Map.Trammel;

            switch (item)
            {
                case ChampEngine engine:
                    if (engine.Running)
                    {   // kickstart
                        engine.ClearMonsters = true;
                        engine.Running = false;
                        engine.Running = true;
                    }
                    break;
                case CustomRegionControl crc:
                    if (crc.CustomRegion != null)
                        crc.CustomRegion.Map = Map.Trammel;
                    break;

                case StaticRegionControl src:
                    if (src.StaticRegion != null)
                        src.StaticRegion.Map = Map.Trammel;
                    break;
            }
        }
        private static void RestoreRegionControllerRegistration(Item source, Item copy, bool wasRegistered)
        {
            switch (copy)
            {
                case CustomRegionControl crc:
                    crc.Registered = wasRegistered;
                    break;

                case StaticRegionControl src:
                    src.Registered = wasRegistered;
                    break;
            }

            switch (source)
            {
                case CustomRegionControl crc:
                    crc.Registered = wasRegistered;
                    break;

                case StaticRegionControl src:
                    src.Registered = wasRegistered;
                    break;
            }
        }
        private static int WipeTrammel()
        {
            int count = 0;

            // --- Items ---
            Item[] items = new Item[World.Items.Values.Count];
            World.Items.Values.CopyTo(items, 0);

            foreach (Item item in items)
            {
                if (item == null || item.Deleted)
                    continue;

                if (item.Map != Map.Trammel)
                    continue;

                if (item.RootParent is Mobile)
                    continue;

                item.Delete();
                count++;
            }

            // --- Mobiles ---
            Mobile[] mobiles = new Mobile[World.Mobiles.Values.Count];
            World.Mobiles.Values.CopyTo(mobiles, 0);

            foreach (Mobile m in mobiles)
            {
                if (m == null || m.Deleted)
                    continue;

                if (m.Map != Map.Trammel)
                    continue;

                if (m is PlayerMobile)
                    continue;

                m.Delete();
                count++;
            }

            return count;
        }
        private static bool ValidateDupe(Item? source, Item dest, LogHelper logger, HashSet<Item> jumpList)
        {
            if (source == null || dest == null)
            {
                logger.Log($"ValidateDupe failed: source={(source == null ? "null" : source.ToString())}, dest={(dest == null ? "null" : dest.ToString())}");

                if (source != null)
                    jumpList.Add(source);

                return false;
            }

            // certain things like addons we remap to statics, so ignore those conflicts
            if (source.GetType() != dest.GetType())
                return true;

            bool valid = true;

            PropertyInfo[] props = Utility.ItemRWProperties(source);

            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo prop = props[i];

                try
                {
                    CopyableAttribute ca = Utility.GetCA(source.GetType(), prop.Name);
                    if (ca != null && (ca.CopyType == CopyType.DoNotCopy || ca.CopyType == CopyType.DoNotRelink))
                        continue;

                    if (!typeof(Item).IsAssignableFrom(prop.PropertyType))
                        continue;

                    Item? sourceValue = prop.GetValue(source, null) as Item;

                    if (sourceValue == null)
                        continue;

                    Item? destValue = prop.GetValue(dest, null) as Item;

                    if (destValue == null)
                    {
                        logger.Log($"ValidateDupe failed: destination property is null. Source: {source}, SourceValue: {sourceValue}, Dest: {dest}, Prop: {prop.Name}");
                        jumpList.Add(source);
                        valid = false;
                        continue;
                    }

                    // If the destination still points to the exact same instance as the source,
                    // this indicates we intentionally preserved a shared reference (e.g., internal
                    // map objects, GM test artifacts, or other non-mirrored items).
                    //
                    // These are not remapped by design, so treat this as valid rather than a failure.
                    // Without this check, the validator would incorrectly flag legitimate shared
                    // references as errors.
                    if (destValue == sourceValue)
                    {
                        logger.Log($"ValidateDupe note: shared reference preserved (not relinked). Source: {source}, SourceValue: {sourceValue}, Dest: {dest}, Prop: {prop.Name}, Map: {destValue.Map}");
                        continue;
                    }

                    if (destValue.Map != Map.Trammel)
                    {
                        logger.Log($"ValidateDupe failed: destination property is not on Trammel. Source: {source}, SourceValue: {sourceValue}, Dest: {dest}, DestValue: {destValue}, DestMap: {destValue.Map}, Prop: {prop.Name}");
                        jumpList.Add(source);
                        valid = false;
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    logger.Log($"ValidateDupe failed: exception reading property. Source: {source}, Dest: {dest}, Prop: {prop.Name}, Exception: {ex.Message}");

                    jumpList.Add(source);
                    valid = false;
                }
            }

            return valid;
        }
        #region New Player starting Area
        private class NewPlayerStartingAreaStashEntry
        {
            public IEntity? Entity;
            public Point3D Location;
            public Map Map;
        }

        private static List<NewPlayerStartingAreaStashEntry> m_NewPlayerStartingAreaStash;
        private static List<NewPlayerStartingAreaStashEntry> StashNewPlayerStartingArea()
        {
            List<NewPlayerStartingAreaStashEntry> stashed = new List<NewPlayerStartingAreaStashEntry>();

            Rectangle2D rect = new Rectangle2D(5700, 940, 65, 60);
            Map map = Map.Trammel;

            // Items
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted)
                    continue;

                if (item.Map != map)
                    continue;

                if (!rect.Contains(item.Location))
                    continue;

                stashed.Add(new NewPlayerStartingAreaStashEntry
                {
                    Entity = item,
                    Location = item.Location,
                    Map = item.Map
                });

                item.MoveToIntStorage(PreserveLocation: true);

                if (item is CustomRegionControl crc)
                    crc.Registered = false;
            }

            // Mobiles
            foreach (Mobile mob in World.Mobiles.Values)
            {
                if (mob == null || mob.Deleted)
                    continue;

                if (mob.Map != map)
                    continue;

                if (!rect.Contains(mob.Location))
                    continue;

                stashed.Add(new NewPlayerStartingAreaStashEntry
                {
                    Entity = mob,
                    Location = mob.Location,
                    Map = mob.Map
                });

                mob.MoveToIntStorage(PreserveLocation: true);
            }

            return stashed;
        }
        private static int RestoreNewPlayerStartingArea(List<NewPlayerStartingAreaStashEntry> stashed)
        {
            if (stashed == null)
                return 0;

            int restored = 0;

            foreach (NewPlayerStartingAreaStashEntry entry in stashed)
            {
                if (entry == null || entry.Entity == null)
                    continue;

                if (entry.Entity is Item item)
                {
                    if (item.Deleted)
                        continue;

                    item.MoveToWorld(entry.Location, entry.Map);

                    if (item is CustomRegionControl crc)
                        crc.Registered = true;

                    restored++;
                }
                else if (entry.Entity is Mobile mob)
                {
                    if (mob.Deleted)
                        continue;

                    mob.MoveToWorld(entry.Location, entry.Map);
                    restored++;
                }
            }

            return restored;
        }
        #endregion New Player starting Area

        #region Custom Housing

        private sealed class CustomHousingAreaSnapshot
        {
            public List<CustomHousingHouseSnapshot> Houses { get; } = new();
        }
        private sealed class CustomHousingHouseSnapshot
        {
            public string HouseBlueprintID;
            public Point3D HouseLocation;
            public Map HouseMap;

            public Point3D VendorLocation;
            public Direction VendorDirection;
            public Map VendorMap;
        }
        private static CustomHousingAreaSnapshot CaptureCustomHousingArea()
        {
            CustomHousingAreaSnapshot snapshot = new CustomHousingAreaSnapshot();
            Dictionary<BaseHouse, CustomHousingHouseSnapshot> houseMap = new();

            Region? chaRegion = Region.FindByName("Custom Housing Area", Map.Felucca);

            if (chaRegion == null)
                return snapshot;

            // 1. Capture StaticHouse entries
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted)
                    continue;

                if (item is not StaticHouse sh)
                    continue;

                if (item.Map != Map.Felucca || !chaRegion.Contains(item.Location))
                    continue;

                CustomHousingHouseSnapshot entry = new CustomHousingHouseSnapshot
                {
                    HouseBlueprintID = sh.HouseBlueprintID,
                    HouseLocation = sh.Location,
                    HouseMap = sh.Map
                };

                snapshot.Houses.Add(entry);
                houseMap[sh] = entry;
            }

            // 2. Backfill vendors by exact containing house
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m == null || m.Deleted)
                    continue;

                if (m is not PlayerVendor pv)
                    continue;

                if (pv.Map != Map.Felucca || !chaRegion.Contains(pv.Location))
                    continue;

                BaseHouse house = BaseHouse.GetHouseAtOrNear(pv.Location, pv.Map, radius: 1, parcel: true);

                if (house != null && houseMap.TryGetValue(house, out CustomHousingHouseSnapshot? entry))
                {
                    entry.VendorLocation = pv.Location;
                    entry.VendorDirection = pv.Direction;
                    entry.VendorMap = pv.Map;
                }
            }
            // sanity
            var missing = snapshot.Houses
                .Where(h => h.VendorLocation == Point3D.Zero)
                .ToList();

            if (missing.Count > 0)
            {
                Console.WriteLine(
                    "MirrorFelucca WARNING: {0} house(s) missing vendor location.",
                    missing.Count);
            }

            return snapshot;
        }

        #endregion Custom Housing
        public class MirrorResult
        {
            public int ItemsConsidered { get; set; }
            public int ItemsCopied { get; set; }
            public int ItemsFailed { get; set; }

            // custom houses
            public int CustomHousesRebuilt { get; set; }

            // historical preservation
            public int StartingAreaObjectsRestored { get; set; }

            // spawner  defrag
            public int SpawnersDefragged { get; set; }

            // everything else
            public int GeneralRelinked { get; set; }

            public int TotalFixups =>
                GeneralRelinked;

            public int TotalUpdated =>
                ItemsCopied +
                CustomHousesRebuilt +
                StartingAreaObjectsRestored +
                TotalFixups +
                SpawnersDefragged;
        }

        public class MirrorSetupResult
        {
            public int CustomHousesCaptured { get; set; }
            public int StartingAreaStashed { get; set; }
            public int IneligibleItems { get; set; }
            public bool NewPlayerStartingAreaDisabled { get; set; }
            public int ControllersUnregistered { get; set; }
            public int RegionsUnregistered { get; set; }
            public int RegionsLoaded { get; set; }
            public int TrammelItemsDeleted { get; set; }
        }
    }
}