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
 *
 *  TownshipStone.PackUpTownship.cs
 *
 *  Purpose:
 *      Implements the township pack-up and restoration pipeline used to
 *      convert an active township into portable restoration assets and
 *      recoverable township resources.
 *
 *  Overview:
 *      This system transforms a live township into a serialized restoration
 *      package consisting of:
 *
 *          - Township restoration deed
 *          - Moving crates
 *          - Township item restoration deeds
 *          - Township NPC deeds
 *          - Livestock restoration deeds
 *          - Decorative plant restoration deeds
 *          - Generated plant seeds
 *          - Redeeded township addons
 *          - Township resource credit recovery
 *
 *      Unlike house pack-up, township pack-up is a hybrid process:
 *
 *          - Some township assets are preserved and restored later.
 *          - Other township assets are intentionally demolished and
 *            converted back into township credit/resources.
 *
 *  Important Design Notes:
 *
 *      This module is intentionally phase-oriented and procedural.
 *
 *      The process contains many sequencing dependencies and ownership/state
 *      transitions which MUST remain visible and predictable.
 *
 *      Examples:
 *
 *          - Township addons may redeed OR move directly.
 *          - Decorative plants become restoration deeds.
 *          - Functional plants become seeds.
 *          - Township statics are credited and deleted.
 *          - Scaffolds are credited and deleted.
 *          - NPCs and livestock are moved into internal storage.
 *          - Ignore lists prevent duplicate processing across phases.
 *
 *      Because township pack-up performs both preservation and demolition,
 *      the distinction between these operations is intentionally explicit in
 *      the naming of the processing phases.
 *
 *  Architectural Intent:
 *
 *      The implementation is organized into explicit operational phases:
 *
 *          1. Validation and guildstone checks
 *          2. Township item registration cleanup
 *          3. Livestock extraction
 *          4. Township lockdown extraction
 *          5. Township NPC extraction
 *          6. Plant processing
 *          7. Township addon preservation
 *          8. Township static credit/deletion
 *          9. Township scaffold credit/deletion
 *         10. Moving crate construction
 *         11. Restoration deed generation
 *         12. Delivery to player
 *
 *      The naming convention is intentional:
 *
 *          - Pack*                  => preservation/restoration
 *          - CreditAndDelete*      => demolition/resource recovery
 *
 *      This distinction is important when navigating and maintaining the
 *      township lifecycle code.
 *
 *  Township Statics:
 *
 *      Township statics are NOT restored as individual restoration deeds.
 *
 *      Instead, they are processed through township credit recovery using:
 *
 *          DefTownshipCraft.CreditTownship(...)
 *
 *      This behavior is intentional and separate from addon preservation.
 *
 *      Historically this behavior was embedded inside addon processing,
 *      which made township static handling difficult to discover and reason
 *      about. The current structure exposes this destructive pass
 *      explicitly.
 *
 *  Local Functions:
 *
 *      Local helper functions are intentionally used to:
 *
 *          - Preserve behavioral locality
 *          - Reduce class-level surface area
 *          - Keep sequencing obvious
 *          - Avoid accidental subsystem coupling
 *          - Make destructive vs restorative phases explicit
 *
 *  Maintenance Warnings:
 *
 *      Future refactors should prioritize behavioral preservation over
 *      abstraction elegance.
 *
 *      This module contains numerous hidden behavioral contracts involving:
 *
 *          - Township item registration
 *          - Ignore-list mutation timing
 *          - Addon redeeding behavior
 *          - Township credit semantics
 *          - Scaffold lifecycle behavior
 *          - Internal storage transitions
 *          - Deletion timing
 *          - Container capacity assumptions
 *          - Restoration ordering
 *
 *      Be especially cautious when:
 *
 *          - Combining processing phases
 *          - Parallelizing passes
 *          - Deduplicating registry scans
 *          - Reordering destructive operations
 *          - Modifying township credit behavior
 *          - Altering ignore-list semantics
 *          - Generalizing addon/static processing
 *
 *      A visually cleaner implementation can easily introduce subtle
 *      restoration corruption or township credit inconsistencies.
 *
 *  Logging:
 *
 *      Logging is intentionally verbose. These logs provide operational
 *      diagnostics and restoration-chain auditing.
 *
 *  Historical Notes:
 *
 *      Originally extracted from TownshipStone.cs to improve maintainability
 *      and navigation of the township subsystem.
 *
 ***************************************************************************/

/* Scripts/Engines/Township/TownshipStone.PackUpTownship.cs
   * CHANGELOG:
   *  5/18/2026, Adam
   *      Major structural refactor and documentation pass.
   *
   *      - Reorganized township pack-up into explicit operational phases.
   *      - Separated restorative flows from destructive credit/delete flows.
   *      - Split township addon preservation from township static recovery.
   *      - Added explicit CreditAndDeleteTownshipStatics() pass.
   *      - Renamed scaffold cleanup to CreditAndDeleteTownshipScaffolds().
   *      - Reduced hidden side effects and improved navigability.
   *      - Added extensive module documentation describing architecture,
   *        sequencing constraints, lifecycle behavior, and maintenance risks.
   *      - Preserved original pack-up semantics and execution ordering.
   *
   *      See module header for detailed architectural notes and warnings.
   *
   *  5/17/2026, Adam
   *      Broke out to its own module to reduce the size of the main file
   *      and improve subsystem navigation.
   */

using Server.Diagnostics;
using Server.Engines.Plants;
using Server.Mobiles;
using Server.Multis;
using Server.Multis.StaticHousing;
using Server.Regions;
using Server.Township;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public partial class TownshipStone
    {
        public bool PackUpTownship(Mobile from, LogHelper logger)
        {
            if (this is not TownshipStone stone || IsPackedUp())
                return true;

            logger.Log(string.Format("--packing up township for {0}.--", stone.Guild));
            logger.Log(LogType.Mobile, from);

            if (!CanPackUpTownship(from))
                return false;

            List<IEntity> ignoreList = new();

            RegisterUnregisteredChristmasTrees(ignoreList);

            List<TownshipLivestockRestorationDeed> livestockDeeds = PackLivestock(ignoreList);
            List<TownshipItemRestorationDeed> lockdownDeeds = PackLockdowns(ignoreList);
            List<TownshipNPCDeed> npcDeeds = PackNPCs(ignoreList);
            List<TownshipItemRestorationDeed> plantDeeds = PackPlants(ignoreList, out SmallCrate seedBox);
            List<TownshipItemRestorationDeed> addonDeeds = PackTownshipAddons(ignoreList, out SmallCrate addonDeedBox);

            CreditAndDeleteTownshipScaffolds(ignoreList);

            CreditAndDeleteTownshipStatics(ignoreList);

            uint keyValue = Key.RandomValue();

            Key key = new Key(KeyType.Magic, keyValue)
            {
                LootType = LootType.Blessed,
                Name = string.Format("a moving crate key for {0}", stone.Guild.Abbreviation ?? stone.Guild.Name)
            };

            List<MovingCrate> movingCrates = BuildMovingCrates(
                keyValue,
                livestockDeeds,
                lockdownDeeds,
                npcDeeds,
                plantDeeds,
                seedBox,
                addonDeeds,
                addonDeedBox);

            if (!GiveRestorationDeedAndKey(movingCrates, key))
                return false;

            logger.Log(LogType.Mobile, from);
            logger.Log(string.Format("--township for {0} packed up.--", stone.Guild));

            return true;

            bool CanPackUpTownship(Mobile from)
            {
                if (stone.Guild != null && stone.Guild.Guildstone != null && stone.Guild.Guildstone.Map != Map.Internal)
                    return true;

                from.SendMessage("You cannot pack up your township while your guild stone is being moved.");
                from.SendMessage("Place your guild stone and try again.");

                return false;
            }

            void RegisterUnregisteredChristmasTrees(List<IEntity> ignored)
            {
                TownshipRegion? townshipRegion = TownshipRegion.GetTownshipAt(stone);

                if (townshipRegion == null)
                {
                    // error
                    return;
                }

                foreach (Rectangle3D rect in townshipRegion.Coords)
                {
                    IPooledEnumerable eable = Map.Felucca.GetItemsInBounds(new Rectangle2D(rect.Start, rect.End));

                    foreach (Item item in eable)
                    {
                        if (ignored.Contains(item) || item.Deleted)
                            continue;

                        if (item is not ChristmasTreeAddon)
                            continue;

                        if (IsInHouse(item))
                        {
                            // just want to know
                            continue;
                        }

                        if (stone.ItemRegistry.Table.Keys.Contains(item))
                            continue;

                        TownshipItemHelper.SetOwnership(item, from);

                        if (!stone.ItemRegistry.Table.Keys.Contains(item))
                        {
                            // error
                        }
                    }

                    eable.Free();
                }
            }

            List<TownshipLivestockRestorationDeed> PackLivestock(List<IEntity> ignored)
            {
                List<TownshipLivestockRestorationDeed> deeds = new();

                if (stone.Livestock == null)
                    return deeds;

                foreach (BaseCreature creature in stone.Livestock.Keys)
                {
                    if (ignored.Contains(creature) || creature.Deleted)
                        continue;

                    deeds.Add(new TownshipLivestockRestorationDeed(stone, creature));
                }

                foreach (TownshipLivestockRestorationDeed deed in deeds)
                    logger.Log(string.Format("Packing {0} into deed {1}", deed.Mobile, deed));

                return deeds;
            }

            List<TownshipItemRestorationDeed> PackLockdowns(List<IEntity> ignored)
            {
                List<Item> toMove = new();
                List<TownshipItemRestorationDeed> deeds = new();

                foreach (Item item in stone.LockdownRegistry.Keys)
                {
                    if (ignored.Contains(item) || item.Deleted)
                        continue;

                    item.SetLastMoved();
                    toMove.Add(item);
                    ignored.Add(item);
                }

                foreach (Item item in toMove)
                    deeds.Add(new TownshipItemRestorationDeed(stone, item));

                foreach (TownshipItemRestorationDeed deed in deeds)
                    logger.Log(string.Format("Packing {0} into deed {1}", deed.Item, deed));

                return deeds;
            }

            List<TownshipNPCDeed> PackNPCs(List<IEntity> ignored)
            {
                List<TownshipNPCDeed> deeds = new();
                Dictionary<Mobile, Item> logging = new();

                if (stone.TownshipNPCs == null)
                    return deeds;

                foreach (Mobile mobile in stone.TownshipNPCs)
                {
                    if (ignored.Contains(mobile) || mobile.Deleted)
                        continue;

                    TownshipNPCDeed? deed = ConfirmRedeedNPCGump.GetDeed(from, mobile.GetType());

                    deed.RestorationMobile = mobile.Serial;

                    logging.Add(mobile, deed);
                    mobile.MoveToIntStorage();
                    deeds.Add(deed);
                }

                foreach (KeyValuePair<Mobile, Item> unit in logging)
                    logger.Log(string.Format("Packing {0} into deed {1}", unit.Key, unit.Value));

                return deeds;
            }

            List<TownshipItemRestorationDeed> PackPlants(List<IEntity> ignored, out SmallCrate seedBox)
            {
                List<Item> toMove = new();
                List<ITownshipItem> plants = new();
                List<TownshipItemRestorationDeed> deeds = new();

                seedBox = new SmallCrate
                {
                    Name = "(seeds)"
                };

                foreach (ITownshipItem townshipItem in Utility.TownshipItems(stone, new List<Type> { typeof(TownshipPlantItem) }))
                {
                    if (townshipItem is Item item && ignored.Contains(item))
                        continue;

                    if (townshipItem.Deleted)
                        continue;

                    plants.Add(townshipItem);
                }

                seedBox.MaxItems = plants.Count;

                foreach (ITownshipItem townshipItem in plants)
                {
                    if (townshipItem is not StaticPlantItem plant || plant.Deleted)
                        continue;

                    if (plant.PlantStatus == PlantStatus.DecorativePlant)
                    {
                        toMove.Add(plant);
                        ignored.Add(plant);
                    }
                    else if (plant.PlantStatus != PlantStatus.DeadTwigs)
                    {
                        Seed seed = new Seed(plant.PlantType, plant.PlantHue, showType: true);
                        seed.Name = string.Format("{0} {1} seed", seed.PlantHue, seed.PlantType);

                        logger.Log(string.Format("Generating seed {0} from {1}", seed, plant));

                        if (!seedBox.TryDropItem(World.GetSystemAcct(), seed, sendFullMessage: false))
                            throw new ApplicationException(string.Format("Max items:{0} insufficient to store seeds in seed box:{1}.", seedBox.MaxItems, seedBox));

                        logger.Log(string.Format("Dropping seed {0} into {1}", seed, seedBox));

                        if (TownshipItemHelper.AllTownshipItems.Contains(townshipItem))
                            TownshipItemHelper.Unregister(townshipItem);

                        plant.Delete();
                        ignored.Add(plant);
                    }
                }

                foreach (Item item in toMove)
                {
                    if (item.Deleted)
                        throw new ApplicationException("Trying to deed an item that has already been deleted.");

                    deeds.Add(new TownshipItemRestorationDeed(stone, item));
                }

                foreach (TownshipItemRestorationDeed deed in deeds)
                    logger.Log(string.Format("Packing {0} into deed {1}", deed.Item, deed));

                foreach (Item seed in seedBox.Items)
                    logger.Log(string.Format("Packing deed {0} into deed box {1}", seed, seedBox));

                return deeds;
            }

            List<TownshipItemRestorationDeed> PackTownshipAddons(List<IEntity> ignored, out SmallCrate addonDeedBox)
            {
                List<Item> toMove = new();
                List<TownshipItemRestorationDeed> deeds = new();
                List<Item> registeredItems = new(stone.ItemRegistry.Table.Keys);

                addonDeedBox = new SmallCrate
                {
                    Name = "(deeds)",
                    MaxItems = registeredItems.Count
                };

                foreach (Item townshipItem in registeredItems)
                {
                    if (ignored.Contains(townshipItem) || townshipItem.Deleted)
                        continue;

                    if (townshipItem is not BaseAddon addon)
                        continue;

                    if (townshipItem is PlantAddon plantAddon && ignored.Contains(plantAddon.PlantItem))
                        continue;

                    if (addon.GetItemBool(ItemBoolTable.Preview))
                    {
                        // Exploit prevention. We still want to know if this is possible.
                        System.Diagnostics.Debug.Assert(false);
                        continue;
                    }

                    if (IsInHouse(townshipItem))
                    {
                        // error
                        continue;
                    }

                    if (townshipItem is StaticHouseHelper.FixerAddon)
                    {
                        ignored.Add(townshipItem);
                        continue;
                    }

                    PackTownshipAddon(addon, toMove, ignored, addonDeedBox);
                }

                foreach (Item item in toMove)
                {
                    if (item.Deleted)
                        throw new ApplicationException("Trying to deed an item that has already been deleted.");

                    deeds.Add(new TownshipItemRestorationDeed(stone, item));
                }

                foreach (TownshipItemRestorationDeed deed in deeds)
                    logger.Log(string.Format("Packing {0} into deed {1}", deed.Item, deed));

                foreach (Item deed in addonDeedBox.Items)
                    logger.Log(string.Format("Packing deed {0} into deed box {1}", deed, addonDeedBox));

                return deeds;
            }

            void PackTownshipAddon(BaseAddon baseAddon, List<Item> toMove, List<IEntity> ignored, SmallCrate addonDeedBox)
            {
                Item addon = baseAddon;

                if (!Utility.IsRedeedableAddon(addon, out Item deed))
                {
                    addon.SetLastMoved();
                    toMove.Add(addon);
                    ignored.Add(addon);
                    return;
                }

                if (deed is BaseAddonDeed)
                    deed.Hue = addon.Hue;

                logger.Log(string.Format("Generating deed {0} from {1}", deed, addon));

                if (!addonDeedBox.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                    throw new ApplicationException(string.Format("Max items:{0} insufficient to store deeds in deed box:{1}.", addonDeedBox.MaxItems, addonDeedBox));

                logger.Log(string.Format("Dropping deed {0} into {1}", deed, addonDeedBox));

                addon.Delete();
                ignored.Add(addon);
            }

            void CreditAndDeleteTownshipStatics(List<IEntity> ignored)
            {
                List<Item> registeredItems = new(stone.ItemRegistry.Table.Keys);

                foreach (Item townshipItem in registeredItems)
                {
                    if (ignored.Contains(townshipItem) || townshipItem.Deleted)
                        continue;

                    if (townshipItem is BaseAddon)
                        continue;

                    if (townshipItem is PlantAddon plantAddon && ignored.Contains(plantAddon.PlantItem))
                        continue;

                    if (IsInHouse(townshipItem))
                    {
                        // error
                        continue;
                    }

                    CreditAndDeleteTownshipStatic(townshipItem, ignored);
                }
            }

            void CreditAndDeleteTownshipStatic(Item item, List<IEntity> ignored)
            {
                if (ignored.Contains(item))
                    return;

                switch (DefTownshipCraft.CreditTownship(stone, item))
                {
                    case DefTownshipCraft.CreditResult.Failed:
                        logger.Log(string.Format("Failed to credit township for {0}", item));
                        item.Delete();
                        ignored.Add(item);
                        break;

                    case DefTownshipCraft.CreditResult.Partial:
                        logger.Log(string.Format("Partial to credit township for {0}", item));
                        item.Delete();
                        ignored.Add(item);
                        break;

                    case DefTownshipCraft.CreditResult.Full:
                        logger.Log(string.Format("Full to credit township for {0}", item));
                        item.Delete();
                        ignored.Add(item);
                        break;
                }
            }

            void CreditAndDeleteTownshipScaffolds(List<IEntity> ignored)
            {
                List<Item> scaffolds = new(stone.Scaffolds);

                foreach (Item item in scaffolds)
                {
                    if (ignored.Contains(item) || item.Deleted)
                        continue;

                    if (item is not Scaffold scaffold)
                        throw new ApplicationException(string.Format("Found {0} while looking for scaffolds.", item));

                    switch (DefTownshipCraft.CreditTownship(stone, scaffold.ToBuild))
                    {
                        case DefTownshipCraft.CreditResult.Failed:
                            logger.Log(string.Format("Failed to credit township for {0}", item));
                            item.Delete();
                            ignored.Add(item);
                            break;

                        case DefTownshipCraft.CreditResult.Partial:
                            logger.Log(string.Format("Partial to credit township for {0}", item));
                            item.Delete();
                            ignored.Add(item);
                            break;

                        case DefTownshipCraft.CreditResult.Full:
                            logger.Log(string.Format("Full to credit township for {0}", item));
                            item.Delete();
                            ignored.Add(item);
                            break;
                    }
                }
            }

            List<MovingCrate> BuildMovingCrates(
                uint keyValue,
                List<TownshipLivestockRestorationDeed> livestockDeeds,
                List<TownshipItemRestorationDeed> lockdownDeeds,
                List<TownshipNPCDeed> npcDeeds,
                List<TownshipItemRestorationDeed> plantDeeds,
                SmallCrate seedBox,
                List<TownshipItemRestorationDeed> addonDeeds,
                SmallCrate addonDeedBox)
            {
                List<MovingCrate> crates = new();

                AddDeedCrate(crates, keyValue, "(livestock deeds)", "livestockDeeds", livestockDeeds);
                AddDeedCrate(crates, keyValue, "(lockdown deeds)", "lockdownDeeds", lockdownDeeds);
                AddDeedCrate(crates, keyValue, "(NPC deeds)", "NPCDeeds", npcDeeds);
                AddPlantCrate(crates, keyValue, plantDeeds, seedBox);
                AddAddonCrate(crates, keyValue, addonDeeds, addonDeedBox);

                return crates;
            }

            void AddDeedCrate<T>(List<MovingCrate> crates, uint keyValue, string label, string hueKey, List<T> deeds)
                where T : Item
            {
                if (deeds.Count == 0)
                    return;

                MovingCrate crate = new MovingCrate(deeds.Count, Utility.RandomSpecialHue(hueKey), stone)
                {
                    Label = label
                };

                foreach (Item deed in deeds)
                {
                    if (!crate.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                        throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", deeds.Count));
                }

                ConfigureLock(crate, keyValue);
                crates.Add(crate);

                logger.Log(string.Format("Adding {0} to moving crate {1}", crate.Label, crate));
            }

            void AddPlantCrate(List<MovingCrate> crates, uint keyValue, List<TownshipItemRestorationDeed> plantDeeds, SmallCrate seedBox)
            {
                int seedCount = seedBox.Items.Count;

                if (plantDeeds.Count == 0 && seedCount == 0)
                    return;

                int itemCount = plantDeeds.Count + seedCount + (seedCount > 0 ? 1 : 0);

                MovingCrate crate = new MovingCrate(itemCount, Utility.RandomSpecialHue("plantDeeds"), stone)
                {
                    Label = "(seeds & decorative plants)"
                };

                if (seedCount > 0)
                {
                    if (!crate.TryDropItem(World.GetSystemAcct(), seedBox, sendFullMessage: false))
                        throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", itemCount));

                    logger.Log(string.Format("Adding {0} to moving crate {1} {2}", seedBox, crate, crate.Label));
                }

                foreach (Item deed in plantDeeds)
                {
                    if (!crate.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                        throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", itemCount));
                }

                logger.Log(string.Format("Adding {0} to moving crate {1}", crate.Label, crate));

                ConfigureLock(crate, keyValue);
                crates.Add(crate);
            }

            void AddAddonCrate(List<MovingCrate> crates, uint keyValue, List<TownshipItemRestorationDeed> addonDeeds, SmallCrate addonDeedBox)
            {
                int boxedDeedCount = addonDeedBox.Items.Count;

                if (addonDeeds.Count == 0 && boxedDeedCount == 0)
                    return;

                int itemCount = addonDeeds.Count + boxedDeedCount + (boxedDeedCount > 0 ? 1 : 0);

                MovingCrate crate = new MovingCrate(itemCount, Utility.RandomSpecialHue("addonDeeds"), stone)
                {
                    Label = "(addon deeds)"
                };

                if (boxedDeedCount > 0)
                {
                    if (!crate.TryDropItem(World.GetSystemAcct(), addonDeedBox, sendFullMessage: false))
                        throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", itemCount));

                    logger.Log(string.Format("Adding {0} to moving crate {1} {2}", addonDeedBox, crate, crate.Label));
                }

                foreach (Item deed in addonDeeds)
                {
                    if (!crate.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                        throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", itemCount));
                }

                ConfigureLock(crate, keyValue);
                crates.Add(crate);

                logger.Log(string.Format("Adding {0} to moving crate {1}", crate.Label, crate));
            }

            bool GiveRestorationDeedAndKey(List<MovingCrate> movingCrates, Key key)
            {
                if (movingCrates.Count == 0)
                {
                    from.SendMessage("Nothing to pack up.");
                    return false;
                }

                TownshipRestorationDeed townshipDeed = new TownshipRestorationDeed(stone, movingCrates, key);

                switch (Utility.SecureGive(from, townshipDeed))
                {
                    case Backpack:
                        from.SendMessage("A township restoration deed was placed in your backpack.");
                        logger.Log(string.Format("A township restoration deed was placed in the backpack of {0}", from));
                        break;

                    case BankBox:
                        from.SendMessage("A township restoration deed was placed in your bank box.");
                        logger.Log(string.Format("A township restoration deed was placed in the bank box of {0}", from));
                        break;

                    default:
                        from.SendMessage("A township restoration deed was dropped at your feet.");
                        logger.Log(string.Format("A township restoration deed was placed at the feet of {0} (locked down)", from));
                        break;
                }

                switch (Utility.SecureGive(from, key))
                {
                    case Backpack:
                        from.SendMessage("The key to your moving crates was placed in your backpack.");
                        logger.Log(string.Format("A moving crate key was placed in the backpack of {0}", from));
                        break;

                    case BankBox:
                        from.SendMessage("The key to your moving crates was placed in your bank box.");
                        logger.Log(string.Format("A moving crate key was placed in the bank box of {0}", from));
                        break;

                    default:
                        from.SendMessage("The key to your moving crates dropped at your feet.");
                        logger.Log(string.Format("A moving crate key was placed at the feet of {0} (locked down)", from));
                        break;
                }

                SetTownshipStoneBool(TownshipStoneBoolTable.IsPackedUp, true);

                return true;
            }
        }

        private static bool IsInHouse(Item item)
        {
            return BaseHouse.Find(item.Location, item.Map) != null;
        }

        private static void ConfigureLock(LockableContainer c, uint keyValue)
        {
            // LockLevel of 0 means that the door can't be picklocked
            // LockLevel of -255 means it's magic locked
            c.Locked = true;
            c.MaxLockLevel = 0; // ?
            c.LockLevel = 0;
            c.KeyValue = keyValue;
        }

        public bool IsPackedUp()
        {
            // packed up, but crates have not yet been created
            if (GetTownshipStoneBool(TownshipStoneBoolTable.IsPackedUp))
                return true;

            // If we have crates, they must be emptied
            foreach (Item item in World.Items.Values)
                if (item.Deleted == false && item is MovingCrate mc && mc.Property == this)
                    return true;
            return false;
        }
    }
}
