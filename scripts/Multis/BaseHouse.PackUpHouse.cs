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
 *  BaseHouse.PackUpHouse.cs
 *
 *  Purpose:
 *      Implements the complete house pack-up and restoration pipeline used
 *      when converting a live house into portable restoration assets.
 *
 *  Overview:
 *      This system transforms a fully populated house into a serialized
 *      restoration package consisting of:
 *
 *          - Home restoration deed
 *          - Moving crates
 *          - Restoration deeds
 *          - Redeeded addons
 *          - NPC restoration deeds
 *          - Decorative plant deeds
 *          - Generated plant seeds
 *          - Secure/lockdown restoration entries
 *
 *      The resulting package allows a house to be reconstructed later while
 *      preserving player property, vendors, containers, and house state.
 *
 *  Important Design Notes:
 *
 *      This code is intentionally procedural and phase-oriented.
 *
 *      The pack-up process contains numerous sequencing dependencies and
 *      ownership transitions which MUST occur in the correct order.
 *
 *      Examples:
 *          - Items must be released before deeding.
 *          - Some objects are deleted after replacement deeds are created.
 *          - Ignore lists prevent duplicate processing across phases.
 *          - Addons may redeed OR move directly depending on type.
 *          - Plants may convert into seeds OR decorative deeds.
 *          - Secures/lockdowns must verify release success.
 *
 *      Because of these interdependencies, this module should be treated as
 *      a transactional state-conversion pipeline rather than generic cleanup
 *      code.
 *
 *  Architectural Intent:
 *
 *      The implementation is organized into logical phases:
 *
 *          1. Object discovery
 *          2. Cleanup/filtering
 *          3. NPC extraction
 *          4. Plant processing
 *          5. Addon processing
 *          6. Lockdown processing
 *          7. Secure processing
 *          8. Moving crate construction
 *          9. Restoration deed generation
 *         10. Delivery to player
 *
 *      Local helper functions are intentionally used to:
 *
 *          - Preserve behavioral locality
 *          - Reduce class surface pollution
 *          - Keep sequencing visible
 *          - Avoid accidental cross-module coupling
 *
 *  Maintenance Warnings:
 *
 *      Future refactors should prioritize behavioral preservation over
 *      abstraction elegance.
 *
 *      This module contains many hidden behavioral contracts involving:
 *
 *          - deletion timing
 *          - Release() semantics
 *          - Addons collection management
 *          - Secure/lockdown state transitions
 *          - container capacity assumptions
 *          - deed generation ordering
 *          - ignore-list mutation timing
 *
 *      Be especially cautious when:
 *
 *          - parallelizing logic
 *          - deduplicating passes
 *          - changing ignore-list behavior
 *          - modifying crate construction order
 *          - changing item release semantics
 *          - converting local functions into shared utilities
 *
 *      A visually cleaner implementation can easily introduce subtle
 *      restoration corruption bugs.
 *
 *  Logging:
 *
 *      Logging is intentionally verbose. These logs serve both operational
 *      diagnostics and restoration-chain auditing.
 *
 *  Historical Notes:
 *
 *      Originally extracted from BaseHouse.cs to improve maintainability and
 *      navigation of the housing subsystem.
 *
 ***************************************************************************/

/* Scripts/Multis/BaseHouse.PackupUpHouse.cs
   * CHANGELOG:
   *  5/18/2026, Adam
   *      Major structural refactor and documentation pass.
   *
   *      - Reorganized house pack-up into explicit operational phases.
   *      - Broke complex procedural logic into localized helper functions.
   *      - Improved visibility of ownership/state transitions and side effects.
   *      - Clarified restorative flows for:
   *          * lockdowns
   *          * secures
   *          * addons
   *          * plants
   *          * NPCs
   *          * moving crates
   *      - Fixed longstanding FixerAddon ignore-list typo.
   *      - Fixed secure-deed logging bug referencing lockdownDeeds.
   *      - Reduced hidden behavioral coupling and improved navigability.
   *      - Added extensive module documentation describing sequencing,
   *        restoration behavior, lifecycle constraints, and maintenance risks.
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
using Server.Items;
using Server.Mobiles;
using Server.Multis.StaticHousing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Multis
{
    public abstract partial class BaseHouse
    {
        public void PackUpHouse(Mobile from, LogHelper logger)
        {
            if (this is not BaseHouse house || IsPackedUp())
                return;

            logger.Log(string.Format("--packing up house for {0}/{1}.--", house, house.Owner));
            logger.Log(LogType.Mobile, from);

            List<Item> ignoreList = Utility.AllTownshipItems();        // should really be specific to THIS township
            List<MoveEntry> masterList = GetAllPackableObjects();      // overreaches and picks up township items

            RemoveIgnoredObjects(masterList, ignoreList);
            RemoveHouseControlledObjects(masterList);
            RemoveDeletedObjects(masterList);

            List<Mobile> npcList = GetPackableNPCs(masterList);
            List<Item> plantList = GetPackablePlants(masterList);
            List<Item> addonList = GetPackableAddons(house);

            List<HouseNPCRestorationDeed> npcDeeds = PackNPCs(npcList);
            List<HomeItemRestorationDeed> plantDeeds = PackPlants(plantList, ignoreList, out SmallCrate seedBox);
            List<HomeItemRestorationDeed> addonDeeds = PackAddons(addonList, ignoreList, out SmallCrate addonDeedBox);
            List<HomeItemRestorationDeed> lockdownDeeds = PackLockdowns(ignoreList);
            List<HomeItemRestorationDeed> secureDeeds = PackSecures(ignoreList);

            uint keyValue = Key.RandomValue();

            Key key = new Key(KeyType.Magic, keyValue)
            {
                LootType = LootType.Blessed,
                Name = string.Format("a moving crate key for {0}/{1}", house.Owner.Name, house.Sign.Name)
            };

            List<MovingCrate> movingCrates = BuildMovingCrates(
                keyValue,
                lockdownDeeds,
                secureDeeds,
                npcDeeds,
                plantDeeds,
                seedBox,
                addonDeeds,
                addonDeedBox);

            GiveRestorationDeedAndKey(movingCrates, key);

            logger.Log(LogType.Mobile, from);
            logger.Log(string.Format("--home for {0}/{1} packed up.--", house, house.Owner));

            void RemoveIgnoredObjects(List<MoveEntry> list, List<Item> ignored)
            {
                list.RemoveAll(entry => entry.Entity is Item item && ignored.Contains(item));
            }

            void RemoveHouseControlledObjects(List<MoveEntry> list)
            {
                list.RemoveAll(entry => entry.Entity is Item item && house.IsLockedDown(item));
                list.RemoveAll(entry => entry.Entity is Item item && house.IsSecure(item));
                list.RemoveAll(entry => entry.Entity is Item item && house.Addons.Contains(item));
            }

            void RemoveDeletedObjects(List<MoveEntry> list)
            {
                list.RemoveAll(entry =>
                    entry.Entity is Item item && item.Deleted ||
                    entry.Entity is Mobile mobile && mobile.Deleted);
            }

            List<Mobile> GetPackableNPCs(List<MoveEntry> list)
            {
                List<Mobile> npcs = new();

                foreach (MoveEntry entry in list)
                {
                    if ((entry.Entity is BaseVendor || entry.Entity is PlayerVendor) &&
                        entry.Entity is not ITownshipNPC &&
                        entry.Entity is Mobile mobile &&
                        !mobile.Deleted)
                    {
                        npcs.Add(mobile);
                    }
                }

                return npcs;
            }

            List<Item> GetPackablePlants(List<MoveEntry> list)
            {
                List<Item> plants = new();

                foreach (MoveEntry entry in list)
                {
                    if (entry.Entity is PlantItem plant && !plant.Deleted)
                        plants.Add(plant);
                }

                return plants;
            }

            List<Item> GetPackableAddons(BaseHouse house)
            {
                List<Item> addons = new();

                if (house.Addons == null)
                    return addons;

                foreach (object addon in house.Addons)
                {
                    if (addon is Item item && !item.Deleted)
                        addons.Add(item);
                }

                return addons;
            }

            List<HouseNPCRestorationDeed> PackNPCs(List<Mobile> npcs)
            {
                List<HouseNPCRestorationDeed> deeds = new();
                Dictionary<Mobile, Item> logging = new();

                foreach (Mobile mobile in npcs)
                {
                    if (mobile is ITownshipNPC)
                        continue;

                    HouseNPCRestorationDeed deed = new HouseNPCRestorationDeed(house, mobile);

                    logging.Add(mobile, deed);
                    mobile.MoveToIntStorage();
                    deeds.Add(deed);
                }

                foreach (KeyValuePair<Mobile, Item> unit in logging)
                    logger.Log(string.Format("Packing {0} into deed {1}", unit.Key, unit.Value));

                return deeds;
            }

            List<HomeItemRestorationDeed> PackPlants(List<Item> plants, List<Item> ignored, out SmallCrate seedBox)
            {
                List<Item> toMove = new();
                List<HomeItemRestorationDeed> deeds = new();

                seedBox = new SmallCrate
                {
                    Name = "(seeds)",
                    MaxItems = plants.Count
                };

                foreach (Item item in plants)
                {
                    if (item is not PlantItem plant || plant.Deleted || ignored.Contains(plant))
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

                        plant.Delete();
                        ignored.Add(plant);
                    }

                    if (house.IsLockedDown(plant))
                        house.Release(World.GetSystemAcct(), plant, checkWater: false);
                }

                foreach (Item item in toMove)
                {
                    if (item.Deleted)
                        throw new ApplicationException("Trying to deed an item that has already been deleted.");

                    deeds.Add(new HomeItemRestorationDeed(house, item, HomeItemRestorationDeed.LockdownType.Lockdown));
                }

                foreach (HomeItemRestorationDeed deed in deeds)
                    logger.Log(string.Format("Packing {0} into deed {1}", deed.Item, deed));

                foreach (Item seed in seedBox.Items)
                    logger.Log(string.Format("Packing seed {0} into seed box {1}", seed, seedBox));

                return deeds;
            }

            List<HomeItemRestorationDeed> PackAddons(List<Item> addons, List<Item> ignored, out SmallCrate addonDeedBox)
            {
                List<Item> toMove = new();
                List<HomeItemRestorationDeed> deeds = new();

                addonDeedBox = new SmallCrate
                {
                    Name = "(deeds)",
                    MaxItems = addons.Count
                };

                foreach (Item addon in addons)
                {
                    if (addon.Deleted || ignored.Contains(addon))
                        continue;

                    if (addon is StaticHouseHelper.FixerAddon)
                    {
                        ignored.Add(addon);
                        continue;
                    }

                    if (house.Addons.Contains(addon))
                        house.Addons.Remove(addon);

                    if (!Utility.IsRedeedableAddon(addon, out Item deed))
                    {
                        toMove.Add(addon);
                        ignored.Add(addon);
                        continue;
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

                foreach (Item item in toMove)
                {
                    if (item.Deleted)
                        throw new ApplicationException("Trying to deed an item that has already been deleted.");

                    deeds.Add(new HomeItemRestorationDeed(house, item, HomeItemRestorationDeed.LockdownType.Addon));
                }

                foreach (HomeItemRestorationDeed deed in deeds)
                    logger.Log(string.Format("Packing {0} into deed {1}", deed.Item, deed));

                foreach (Item deed in addonDeedBox.Items)
                    logger.Log(string.Format("Packing deed {0} into deed box {1}", deed, addonDeedBox));

                return deeds;
            }

            List<HomeItemRestorationDeed> PackLockdowns(List<Item> ignored)
            {
                List<Item> toMove = new();
                List<HomeItemRestorationDeed> deeds = new();
                List<Item> lockdowns = new(house.m_LockDowns.Cast<Item>().ToList());

                foreach (Item item in lockdowns)
                {
                    if (item.Deleted || ignored.Contains(item))
                        continue;

                    house.Release(World.GetSystemAcct(), item, checkWater: false);

                    if (!house.IsLockedDown(item))
                    {
                        item.SetLastMoved();
                        toMove.Add(item);
                    }
                    else
                    {
                        // investigate
                    }

                    ignored.Add(item);
                }

                foreach (Item item in toMove)
                {
                    deeds.Add(new HomeItemRestorationDeed(
                        house,
                        item,
                        IsLockbox(item) ? HomeItemRestorationDeed.LockdownType.Lockbox : HomeItemRestorationDeed.LockdownType.Lockdown));
                }

                foreach (HomeItemRestorationDeed deed in deeds)
                    logger.Log(string.Format("Packing {0} into deed {1}", deed.Item, deed));

                return deeds;
            }

            List<HomeItemRestorationDeed> PackSecures(List<Item> ignored)
            {
                List<Item> toMove = new();
                List<Item> secures = new();
                List<HomeItemRestorationDeed> deeds = new();

                foreach (object secure in house.m_Secures)
                {
                    if (secure is SecureInfo secureInfo)
                        secures.Add(secureInfo.Item);
                }

                foreach (Item item in secures)
                {
                    if (item.Deleted || ignored.Contains(item))
                        continue;

                    house.Release(World.GetSystemAcct(), item, checkWater: false);

                    if (!house.IsSecure(item))
                    {
                        item.SetLastMoved();
                        toMove.Add(item);
                    }
                    else
                    {
                        // investigate. Strongboxes may need to be released by the owner.
                    }

                    ignored.Add(item);
                }

                foreach (Item item in toMove)
                    deeds.Add(new HomeItemRestorationDeed(house, item, HomeItemRestorationDeed.LockdownType.Secure));

                foreach (HomeItemRestorationDeed deed in deeds)
                    logger.Log(string.Format("Packing {0} into deed {1}", deed.Item, deed));

                return deeds;
            }

            List<MovingCrate> BuildMovingCrates(
                uint keyValue,
                List<HomeItemRestorationDeed> lockdownDeeds,
                List<HomeItemRestorationDeed> secureDeeds,
                List<HouseNPCRestorationDeed> npcDeeds,
                List<HomeItemRestorationDeed> plantDeeds,
                SmallCrate seedBox,
                List<HomeItemRestorationDeed> addonDeeds,
                SmallCrate addonDeedBox)
            {
                List<MovingCrate> crates = new();

                AddDeedCrate(crates, keyValue, "(lockdown deeds)", "lockdownDeeds", lockdownDeeds);
                AddDeedCrate(crates, keyValue, "(secure deeds)", "secureDeeds", secureDeeds);
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

                MovingCrate crate = new MovingCrate(deeds.Count, Utility.RandomSpecialHue(hueKey), house)
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

            void AddPlantCrate(
                List<MovingCrate> crates,
                uint keyValue,
                List<HomeItemRestorationDeed> plantDeeds,
                SmallCrate seedBox)
            {
                int seedCount = seedBox.Items.Count;

                if (plantDeeds.Count == 0 && seedCount == 0)
                    return;

                int itemCount = plantDeeds.Count + seedCount + (seedCount > 0 ? 1 : 0);

                MovingCrate crate = new MovingCrate(itemCount, Utility.RandomSpecialHue("plantDeeds"), house)
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

                    logger.Log(string.Format("Adding {0} to moving crate {1} {2}", deed, crate, crate.Label));
                }

                ConfigureLock(crate, keyValue);
                crates.Add(crate);
            }

            void AddAddonCrate(
                List<MovingCrate> crates,
                uint keyValue,
                List<HomeItemRestorationDeed> addonDeeds,
                SmallCrate addonDeedBox)
            {
                int boxedDeedCount = addonDeedBox.Items.Count;

                if (addonDeeds.Count == 0 && boxedDeedCount == 0)
                    return;

                int itemCount = addonDeeds.Count + boxedDeedCount + (boxedDeedCount > 0 ? 1 : 0);

                MovingCrate crate = new MovingCrate(itemCount, Utility.RandomSpecialHue("addonDeeds"), house)
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

                    logger.Log(string.Format("Adding {0} to moving crate {1} {2}", deed, crate, crate.Label));
                }

                ConfigureLock(crate, keyValue);
                crates.Add(crate);

                logger.Log(string.Format("Adding {0} to moving crate {1}", crate.Label, crate));
            }

            void GiveRestorationDeedAndKey(List<MovingCrate> movingCrates, Key key)
            {
                if (movingCrates.Count == 0)
                {
                    from.SendMessage("Nothing to pack up.");
                    return;
                }

                HouseRestorationDeed houseRestorationDeed = new HouseRestorationDeed(house, movingCrates, key);

                switch (Utility.SecureGive(from, houseRestorationDeed))
                {
                    case Backpack:
                        from.SendMessage("A home restoration deed was placed in your backpack.");
                        logger.Log(string.Format("A home restoration deed was placed in the backpack of {0}", from));
                        break;

                    case BankBox:
                        from.SendMessage("A home restoration deed was placed in your bank box.");
                        logger.Log(string.Format("A home restoration deed was placed in the bank box of {0}", from));
                        break;

                    default:
                        from.SendMessage("A home restoration deed was dropped at your feet.");
                        logger.Log(string.Format("A home restoration deed was placed at the feet of {0} (locked down)", from));
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

                SetBaseHouseBool(BaseHouseBoolTable.IsPackedUp, true);
            }
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
            if (GetBaseHouseBool(BaseHouseBoolTable.IsPackedUp) == true)
                return true;

            // If we have crates, they must be emptied
            foreach (Item item in World.Items.Values)
                if (item.Deleted == false && item is MovingCrate mc && mc.Property == this)
                    return true;

            return false;
        }
    }
}
