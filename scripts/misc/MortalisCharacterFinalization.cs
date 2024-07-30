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

/* Adam: Mortalis; bugiting code
 *  We don't want to keep resupplying new characters with newbie items they already have (farming exploit)
 *  Note: We handle containers of stuff in a special way. For instance, a bag-of-reagents is exploded into 'the' list as individual reagents.
 *  Subsequently, the container is ignored.
 *  Later, we look at what was intended for the character, 'a-bag-of-reagents', and repackage that for them.
 *  Why?: it's far easier to match stacks of reagents than a a-bag-of-reagents which may not contain the correct quanties.
 */

/* Scripts\Misc\MortalisCharacterFinalizations
 * ChangeLog
 *  8/17/22, Adam
 *      -Add support for 'shared accounts'
 *          Under certain circumstances, players can temporarily create a second and third account on mortalis (different ip, no machine info.)
 *          under this circumstances, we want to include those accounts in our stash gatherer.
 *      -Better diagnostic messages
 *  8/16/22, Adam
 *      Finished implementation.
 *      Should be good to go save bug fixes.
 *  8/15/22, Adam
 *  First time checkin
 *  Character cleanup (MortalCharacterFinalize())
 *      1. [all shards] save a list of things we want on our character (CharacterCreation)
 *      2. [all shards] remove all duplicate items from character (CharacterCreation)
 *      3. [mortalis] transfer account bankbox to new character
 *      4. [mortalis] remove items that exceed their budget (they have them stashed elsewhere.)
 *      5. [mortalis] reequip them from their stashed items from the 'wants' list (again, ignoring duplicates.)
 */

using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
namespace Server.Misc
{
    public class MortalisGear
    {
        public static void MortalCharacterFinalize(CharacterCreatedEventArgs args, Mobile newChar)
        {
            List<Item> EquippedThingsWeWant = Utility.GetEquippedItems(newChar);                  // all equipped items
            EquippedThingsWeWant.RemoveAll(item => item == null);                                   // having nulls here is normal, empty layers
            List<Item> BackpackThingsWeWantDupe = DeepDupe(Utility.GetBackpackItems(newChar));   // a dupe of everything: includes containers - used for repackaging
            List<Item> BackpackThingsWeWant = ExplodedList(Utility.GetBackpackItems(newChar));   // only items, no containers
            List<Item> AllThingsWeWant = new List<Item>(EquippedThingsWeWant);                      // all items, all layers and backpack
            AllThingsWeWant.AddRange(BackpackThingsWeWant);

            List<Item> stash = new List<Item>();
            #region BANKBOX
            //Adam: Mortalis allows the player's bankbox to be transfered to the next character created on the account after death
            // do we have a carryover bankbox?
            if (args.Account != null && args.Account is Account acct && acct.BankBox != Serial.Zero)
            {
                // can we load the carryover bankbox?
                Item item = World.FindItem(acct.BankBox);
                if (item != null && item is StrongBackpack bbox)
                {
                    List<Item> list = new List<Item>();
                    if (newChar.BankBox != null)
                    {
                        // temp list of items
                        foreach (Item thing in bbox.Items)
                            list.Add(thing);

                        // move all items to the players bankbox
                        foreach (Item thing in list)
                        {
                            bbox.RemoveItem(thing);
                            newChar.BankBox.DropItem(thing);
                        }

                        bbox.Delete();                  // cleanup old bankbox
                        acct.BankBox = Serial.Zero;     // clear the notion of a carryover bankbox
                    }
                }
                else
                    // couldn't load.
                    acct.BankBox = Serial.Zero;         // clear the notion of a carryover bankbox
            }
            #endregion BANKBOX

            stash = GetStashedItems(args.State.Address, newChar);   // get the list of stashed items
            RemoveAllGear(newChar);             // remove all layers, empty backpack (nothing is deleted)

            // okay, now that we have deleted all items on the player, reequip her from their stash (bankbox etc.)
            // restore wanted items from stash if possible, otherwise the original item is given.
            // Note: Stacks are handled here :(
            foreach (Item oItem in AllThingsWeWant)
            {
                if (oItem.Stackable)
                {   // first get a list of all of this type of item and amounts
                    var haveList = new List<Item>();
                    foreach (Item vitem in stash)
                        if (vitem.GetType() == oItem.GetType())
                            haveList.Add(vitem);

                    // sort the list so smallest quanties are first
                    haveList.Sort((x, y) => y.Amount.CompareTo(x.Amount));

                    if (haveList.Count > 0)
                    {   // first create one of these
                        int need = oItem.Amount;
                        Item tmpItem = Utility.Dupe(oItem);
                        tmpItem.Amount = 0;

                        foreach (var stashedItem in haveList)
                        {   // done
                            if (need == 0)
                                break;

                            DebugFoundItem(stashedItem);                    // console output about where the stash was found

                            // consume stash first
                            if (stashedItem.Amount > need)
                            {
                                tmpItem.Amount += need;
                                stashedItem.Amount -= need;                   // debit the user's stash
                                need = oItem.Amount - tmpItem.Amount;       // the new amount we need
                                break;
                            }
                            else if (stashedItem.Amount <= need)
                            {
                                tmpItem.Amount += stashedItem.Amount;
                                stashedItem.Amount -= stashedItem.Amount;        // debit the user's stash
                                need = oItem.Amount - tmpItem.Amount;           // the new amount we need
                                if (stashedItem.RootParent is Container cont)   // if we're in a container
                                    cont.RemoveItem(stashedItem);               // remnove from container
                                else
                                    Unlock(stashedItem);                        // if it was locked down or secure
                                stashedItem.Delete();                           // all used up. sorry!
                            }
                        }
                        // top it off if there wan't enough
                        if (tmpItem.Amount < oItem.Amount)
                            tmpItem.Amount = oItem.Amount;
                        else if (tmpItem.Amount > oItem.Amount)
                        {
                            Utility.ConsoleWriteLine(string.Format("Logic Error: {0} acct:{1}. ", Utility.FileInfo(), args.Account as Account), ConsoleColor.Red);
                            tmpItem.Amount = oItem.Amount; // cap it
                        }

                        if (!newChar.EquipItem(tmpItem))       // equip it if you can (original item)
                            newChar.Backpack.AddItem(tmpItem); // otherwise drop to backpack (original item)
                    }
                    else
                    {   // they have nothing in their stash. We'll just give them the one allocated
                        if (!newChar.EquipItem(oItem))       // equip it if you can (original item)
                            newChar.Backpack.AddItem(oItem); // otherwise drop to backpack (original item)
                    }
                }
                else
                {
                    // see is we have an equivalent item in the stash
                    Item item = FindStashedItems(stash, oItem, orEquivalent: true);
                    if (item != null && item.Deleted == false)
                    {   // good, we will recycle this item - no need to give them a new one
                        DebugFoundItem(item);                   // console output about where the stash was found
                        if (item.RootParent is Container cont)  // if we're in a container
                            cont.RemoveItem(item);              // remove from container
                        else
                            Unlock(item);                       // if it was locked down or secure
                        if (!newChar.EquipItem(item))           // equip it if you can (stash item)
                            newChar.Backpack.AddItem(item);     // otherwise drop to backpack (stash item)
                        stash.Remove(item);                     // remove it from our stash
                    }
                    else
                    {   // they have nothing in their stash. We'll just give them the one allocated
                        if (!newChar.EquipItem(oItem))       // equip it if you can (original item)
                            newChar.Backpack.AddItem(oItem); // otherwise drop to backpack (original item)
                    }
                }
            }

            // okay, player has been requipped. Now we repackage loose things should be in containers (a bag of regents)
            // BackpackThingsWeWantDupe contains the original packaging of items. we use that as a reference.
            foreach (Item item in BackpackThingsWeWantDupe)
            {
                if (item is Container cont)
                {
                    object o;
                    List<Type> types = new List<Type>();
                    if (cont.Items != null && cont.Items.Count > 0)
                        foreach (Item citem in cont.Items)
                            // get the types of things in this container
                            types.Add(citem.GetType());

                    // now we know what should go into the players container.

                    // first we will create the container
                    o = null;
                    try { o = Activator.CreateInstance(cont.GetType()); }
                    catch { Utility.ConsoleWriteLine(string.Format("Logic Error: {0} acct:{1}. ", Utility.FileInfo(), args.Account as Account), ConsoleColor.Red); }

                    // whacky but true: if we were just creating a BagOfReagents, it's already full!
                    //  we need to delete those contents (free), and refill it with the stuff from our stash. (already in our backpack)
                    if (o != null && o is Container bag)
                    {
                        List<Item> toDelete = new List<Item>();
                        if (bag.Items != null && bag.Items.Count > 0)
                            foreach (Item bitem in bag.Items)
                                toDelete.Add(bitem);
                        foreach (Item citem in toDelete)
                        {
                            bag.RemoveItem(citem);
                            citem.Delete();
                        }
                    }

                    // lets fill it with stuff
                    if (o != null && o is Container fbag)
                    {
                        List<Item> toMove = new List<Item>();
                        foreach (Type type in types)
                        {
                            foreach (Item bpItem in newChar.Backpack.Items)
                            {
                                if (bpItem.GetType() == type)
                                {   // we found what we're looking for
                                    toMove.Add(bpItem);
                                    break;
                                }
                            }
                        }
                        foreach (Item tm in toMove)
                        {
                            newChar.Backpack.RemoveItem(tm);
                            fbag.AddItem(tm);
                        }
                        // all items matching 'type' have been moved from the players backpack to the contanier (bag)
                        //  drop the bag into the player's backpack.
                        newChar.Backpack.AddItem(fbag);
                    }
                }
            }
        }

        private static List<Item> DeepDupe(List<Item> list)
        {
            List<Item> dupe = new List<Item>();
            Item new_item = null;
            foreach (Item item in list)
            {
                new_item = Utility.Dupe(item);
                dupe.Add(new_item);
            }
            return dupe;
        }
        /// <summary>
        /// We remove all gear from the player, but we don't delete.
        /// We've already saved all items in another list
        /// </summary>
        /// <param name="m"></param>
        public static void RemoveAllGear(Mobile m)
        {
            // unequip all items
            List<Item> list = new List<Item>(Utility.GetEquippedItems(m));
            list.RemoveAll(item => item == null);
            foreach (Item item in list)
            {
                // Unequip any items being worn
                if (!m.Backpack.Items.Contains(item))
                    m.Backpack.DropItem(item);
            }

            // get a list of what's in the backpack now
            List<Item> bplist = new List<Item>();
            foreach (Item item in m.Backpack.Items)
                bplist.Add(item);

            // okay. backpack now has all player items. remove (not delete) them.
            foreach (Item item in bplist)
            {
                if (m.Backpack.Items.Contains(item))
                    m.Backpack.RemoveItem(item);
                else
                    Utility.ConsoleWriteLine(string.Format("Logic Error: {0} acct:{1}. ", Utility.FileInfo(), m.Account as Account), ConsoleColor.Red);
            }
        }
        private static void DebugFoundItem(Item item)
        {
#if DEBUG
            Account acct = null;
            if (item.RootParent is Corpse)
            {
                acct = (item.RootParent as Corpse).Owner.Account as Account;
                Utility.ConsoleWriteLine(String.Format("Found {0} in a house:{3} on {1} at {2} acct:{4}", item, item.RootParent as Corpse, GetLocation(item.RootParent), InAHouse(item.RootParent) ? true : false, acct), ConsoleColor.Red);
            }
            else if (item.RootParent is Item)
                Utility.ConsoleWriteLine(String.Format("Found {0} in a house:{2} at {1} acct:{3}", item, GetLocation(item.RootParent), InAHouse(item.RootParent) ? true : false, acct), ConsoleColor.Red);
            else if (item.RootParent is Mobile)
            {
                acct = (item.RootParent as Mobile).Account as Account;
                Utility.ConsoleWriteLine(String.Format("Found {0} in a bankbox in a house:{2} at {1} acct:{3}", item, GetLocation(item.RootParent), InAHouse(item.RootParent) ? true : false, acct), ConsoleColor.Red);
            }
            else if (item.RootParent == null)
                Utility.ConsoleWriteLine(String.Format("Found {0} in a house:{2} at {1} acct:(null)", item, GetLocation(item), InAHouse(item) ? true : false), ConsoleColor.Red);
            else
                Utility.ConsoleWriteLine(string.Format("Logic Error: {0} acct:{1}. ", Utility.FileInfo(), acct), ConsoleColor.Red);
#endif
        }
        private static bool InAHouse(object o)
        {
            Account acct = null;
            if (o is Item)
                return Multis.BaseHouse.FindHouseAt(o as Item) != null;
            if (o is Mobile)
                return Multis.BaseHouse.FindHouseAt(o as Mobile) != null;
            else
                Utility.ConsoleWriteLine(string.Format("Logic Error: {0} acct:{1}. ", Utility.FileInfo(), acct), ConsoleColor.Red);

            return false;
        }
        /// <summary>
        /// explodes all containers in 'list' and returns a new list of the contents of those containers. (and any other items it found.)
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static List<Item> ExplodedList(List<Item> list)
        {
            List<Item> tmpList = new List<Item>();
            foreach (Item item in list)
                if (item is Container cont)
                    tmpList.AddRange(ExplodeContainers(cont));
                else
                    tmpList.Add(item);

            return tmpList;
        }
        private static Point3D GetLocation(object o)
        {
            if (o == null) return Point3D.Zero;
            if (o is Item item) return item.Location;
            if (o is Mobile m) return m.Location;
            return Point3D.Zero;
        }
        private static bool Unlock(Item item)
        {
            Multis.BaseHouse bh = Multis.BaseHouse.FindHouseAt(item);
            if (bh == null) return false;

            if (bh.IsLockedDown(item))
            {
                bh.SetLockdown(item, false);
                return true;
            }
            else if (bh.IsSecure(item))
            {
                for (int i = 0; i < bh.Secures.Count; ++i)
                {
                    Multis.SecureInfo info = (Multis.SecureInfo)bh.Secures[i];

                    if (info.Item == item)
                    {
                        item.IsLockedDown = false;
                        item.IsSecure = false;
                        item.Movable = true;
                        item.SetLastMoved();
                        item.PublicOverheadMessage(Server.Network.MessageType.Label, 0x3B2, 501656);//[no longer secure]
                        bh.Secures.RemoveAt(i);
                        return true;
                    }
                }
            }

            return false;
        }
        private static Item FindStashedItems(List<Item> list, Item wantItem, bool orEquivalent)
        {
            if (wantItem == null) return null;
            //if (wantItem.Deleted) is okay, since we recently deleted these items, but they have not been cleaned up yet.
            foreach (Item item in list)
            {
                if (item == null) continue;
                if (item.Deleted) continue; // should never happen (logic error)
                if (wantItem.GetType() == item.GetType()/*here we could check SameItem(), but I don't think that is necessary*/)
                {   // okay, we found a wanted item in their stash of items, we'll use it
                    return item;
                }
            }
            if (orEquivalent == true)
            {   // find something of the same baseclass that goes on the same layer
                Layer wantLayer = wantItem.Layer;
                foreach (Item item in list)
                {
                    if (item.Layer != wantLayer) continue; // nope!
                    // there's probably a better fit, but we'll go with this.
                    if (item is BaseClothing && wantItem is BaseClothing)
                        return item;
                    else if (item is BaseArmor && wantItem is BaseArmor)
                        return item;
                    else if (item is BaseWeapon bw1 && wantItem is BaseWeapon bw2 && bw1.Type == bw2.Type)
                        return item;
                }
            }
            return null;
        }
        static uint GetAccountCode(Mobile m)
        {
            uint accountCode = 0;
            Account acct = null;
            if (m.Account != null && m.Account is Account)
            {
                acct = m.Account as Account;
                accountCode = acct.AccountCode;
            }
            return accountCode;
        }
        static uint GetAccountCode(Account acct)
        {
            uint accountCode = 0;
            if (acct != null)
                accountCode = acct.AccountCode;
            return accountCode;
        }
        private static List<Item> GetStashedItems(IPAddress address, Mobile mobileToBeCreated)
        {
            // list of stashed items
            List<Item> list = new List<Item>();
            ArrayList accounts = Utility.GetSharedAccounts(address);
            uint creatingAcctCode = GetAccountCode(mobileToBeCreated);
            List<ChestItemSpawner> cis = GetChestItemSpawners();
            foreach (Account acct in accounts)
            {
                for (int i = 0; i < acct.Length; i++)
                {
                    PlayerMobile player = acct[i] as PlayerMobile;
                    if (player is PlayerMobile)
                        list.AddRange(GetStashedItems(creating: GetAccountCode(acct) == creatingAcctCode, player, cis));
                }
            }
            return list;
        }
        /// <summary>
        /// Gets the stashed items from this account:
        /// Locations checked are bankbox, followers, houses, and public 'world' locations 
        ///     (like the spawned items in a container managed by a ChestItemSpawner)
        /// </summary>
        /// <param name="creating"></param>
        /// 'creating' indicates the player is being created after subsequently dying and having their character deleted.
        /// 'creating' is special since their account (and not their mobile) will contain certain things the new character is entitled to or has, like a bankbox, followers, and a house.
        /// players accounts that are not 'creating' have all this info embedded in the mobile itself. This is why we must distinguish between 'creating' and not.
        /// Note: even though Mortalis only allows one account, a player can get a second account (illegally) if they 1) use an alternate IP address, and 2) have not yet had their machine info collected.
        /// Until the machine info is collected, these other accounts will be able to login and collect items. It is for this reason we check all accounts associated with this IP and/or machine info.
        /// /// <param name="m"></param>
        /// The mobile to collect items from.
        /// <param name="cis"></param>
        /// An unfortunate optimization parameter. We pass it as a parameter only to save processing time.
        /// <returns></returns>
        private static List<Item> GetStashedItems(bool creating, Mobile m, List<ChestItemSpawner> cis)
        {
            #region setup
            if (m == null) return new List<Item>();
            Account acct = null;
            if (m.Account != null && m.Account is Account)
                acct = m.Account as Account;
            if (acct == null) return new List<Item>();
            if (m.AccessLevel > AccessLevel.Player) return new List<Item>();
            #endregion setup

            // list of stashed items
            List<Item> stashedItemsList = new List<Item>();

            #region bankbox
            foreach (Item item in m.BankBox.Items)
                if (item is Container cont && cont.Items != null && cont.Items.Count > 0)
                    stashedItemsList.AddRange(ExplodeContainers(cont));
                else
                {   // it's just an item
                    if (!stashedItemsList.Contains(item))               // sanity - make sure we're not double dipping
                        stashedItemsList.Add(item);
                    else
                        Utility.ConsoleWriteLine(string.Format("Logic Error: {0} acct:{1}. ", Utility.FileInfo(), acct), ConsoleColor.Red);
                }
            #endregion bankbox

            #region followers
            List<Mobile> followers = new List<Mobile>();
            if (acct.Followers > 0 || !creating)
            {
                foreach (Mobile mob in World.Mobiles.Values)
                {
                    if (mob is BaseCreature bc)
                    {
                        if (creating)
                        {
                            if (bc.ControlMasterGUID != acct.GUID)
                                continue;
                        }
                        else
                        {
                            if (bc.ControlMaster != m)
                                continue;
                        }

                        if (!(bc is PackHorse || bc is PackLlama || bc is Beetle || bc is HordeMinion))
                            continue;

                        followers.Add(bc);
                    }
                }

            }
            foreach (Mobile mob in followers)
            {
                if (mob is BaseCreature bc)
                {
                    Container bp = bc.Backpack;
                    if (bp != null && bp.Items != null && bp.Items.Count > 0)
                    {
                        foreach (Item item in bp.Items)
                            if (item is Container cont && cont.Items != null && cont.Items.Count > 0)
                                stashedItemsList.AddRange(ExplodeContainers(cont));
                            else
                            {   // it's just an item
                                if (!stashedItemsList.Contains(item))               // sanity - make sure we're not double dipping
                                    stashedItemsList.Add(item);
                                else
                                    Utility.ConsoleWriteLine(string.Format("Logic Error: {0} acct:{1}. ", Utility.FileInfo(), acct), ConsoleColor.Red);
                            }
                    }
                }
            }
            #endregion followers

            #region house
            if (acct.House != Serial.Zero || !creating)
            {
                List<Multis.BaseHouse> bhl = new List<Multis.BaseHouse>();
                if (creating)
                    bhl.Add(World.FindItem(acct.House) as Multis.BaseHouse);
                else
                    bhl.AddRange(Multis.BaseHouse.GetHouses(m).Cast<Multis.BaseHouse>().ToList());
                foreach (Multis.BaseHouse bh in bhl)
                    if (bh != null && (bh.CheckInheritance(m) || !creating))
                    {
                        Item[] itemList = bh.FindAllItems();
                        if (itemList != null && itemList.Length > 0)
                        {   // we found items in his house. 
                            foreach (Item item in itemList)
                            {
                                if (item is null) continue;
                                if (item is Multis.BaseHouse) continue;
                                if (item is BaseHouseDoor) continue;
                                if (item is Multis.HouseSign) continue;
                                if (item is StrongBox) continue;
                                if (item is BaseAddon) continue;
                                if (item is Container cont && cont.Items != null && cont.Items.Count > 0)
                                    stashedItemsList.AddRange(ExplodeContainers(cont));
                                else
                                {   // it's just an item
                                    if (!stashedItemsList.Contains(item))               // sanity - make sure we're not double dipping
                                        stashedItemsList.Add(item);
                                    else
                                        Utility.ConsoleWriteLine(string.Format("Logic Error: {0} acct:{1}. ", Utility.FileInfo(), acct), ConsoleColor.Red);
                                }
                            }
                        }
                    }
            }
            #endregion house

            #region shared accounts
            if (!creating)
            {   // check all shared account mobile's equipment, and backpack (bank and house handled elsewhere)

                // BACKPACK
                if (m.Backpack != null)
                    foreach (Item item in m.Backpack.Items)
                        if (item is Container cont && cont.Items != null && cont.Items.Count > 0)
                            stashedItemsList.AddRange(ExplodeContainers(cont));
                        else
                        {   // it's just an item
                            if (!stashedItemsList.Contains(item))               // sanity - make sure we're not double dipping
                                stashedItemsList.Add(item);
                            else
                                Utility.ConsoleWriteLine(string.Format("Logic Error: {0} acct:{1}. ", Utility.FileInfo(), acct), ConsoleColor.Red);
                        }

                // EQUIPPED ITEMS
                List<Item> faei = Utility.GetEquippedItems(m);
                faei.RemoveAll(item => item == null);
                foreach (Item item in faei)
                    if (!stashedItemsList.Contains(item))               // sanity - make sure we're not double dipping
                        stashedItemsList.Add(item);
                    else
                        Utility.ConsoleWriteLine(string.Format("Logic Error: {0} acct:{1}. ", Utility.FileInfo(), acct), ConsoleColor.Red);
            }
            #endregion shared accounts

            // TODO: add checks for another player with 2 or more of these items (friend helping them farm)

            /* Public locations
             * 1. in a container in a public place, not on a mobile
             * 2. on the ground, movable/visable
             */
            if (creating)                                                           // only look here once, for creating creating account
                foreach (Item item in World.Items.Values)
                {
                    if (item == null) continue;                                     // yeah
                    if (item.Deleted == true) continue;                             // yeah
                    if (item.Map != Map.Felucca) continue;                          // yeah
                    if (item is Container) continue;                                // no containers (owners bankbox handled above)
                    if (ExcludedParent(item)) continue;                             // filter for certain parents
                    if (ExcludedHouse(item)) continue;                              // no house loot (owners house handled above)
                    else
                    {   // just an item
                        if (item.Movable && item.Visible)                           // just an item please
                            if (!ExcludedCorpse(item))                              // some NPC corpses are invalid
                                if (!ExcludedSpawnedItem(item))                     // no spawned items please
                                    if (!ExcludedCampComponent(item))               // camps have real 'filled' chests and stuff. We don't want to include them
                                        if (!ExcludedItemSpawnerManaged(item, cis)) // some items are stuffed into containers via a chest item spawner. no want
                                            if (!stashedItemsList.Contains(item))               // sanity - make sure we're not double dipping
                                                stashedItemsList.Add(item);
                                            else
                                                Utility.ConsoleWriteLine(string.Format("Logic Error: {0} acct:{1}. ", Utility.FileInfo(), acct), ConsoleColor.Red);
                    }
                }

            return stashedItemsList;
        }
        private static List<ChestItemSpawner> GetChestItemSpawners()
        {
            // we use this to exclude items in chests controlled by a ChestItemSpawner
            List<ChestItemSpawner> cis = new List<ChestItemSpawner>();
            foreach (Item item in World.Items.Values)
                if (item != null && item.Deleted == false)
                    if (item is ChestItemSpawner spawner && spawner.Running == true)
                        cis.Add(spawner);
            return cis;
        }
        private static bool ExcludedHouse(Item item)
        {
            return Multis.BaseHouse.FindHouseAt(item) != null;
        }
        private static bool ExcludedCorpse(Item item)
        {
            if (item.RootParent is Corpse corpse)
                if (corpse.Owner == null || corpse.StaticCorpse)           // avoid NPC corpses (dead miners etc.)
                    return true;
            return false;
        }
        private static bool ExcludedParent(Item item)
        {
            if (ExcludedType(item, typeof(FillableContainer))) return true;    // avoid (auto-refill container)
            if (ExcludedType(item, typeof(BaseTreasureChest))) return true;    // avoid 
            if (ExcludedType(item, typeof(DungeonTreasureChest))) return true; // avoid 
            if (ExcludedType(item, typeof(BaseUnusualContainer))) return true; // avoid 
            if (ExcludedType(item, typeof(SupplyDepotChest))) return true;     // avoid 
            if (ExcludedType(item, typeof(LibraryBookcase))) return true;      // avoid 
            if (ExcludedType(item, typeof(BankBox))) return true;              // avoid 
            if (ExcludedType(item, typeof(Mobile))) return true;         // avoid 
            if (ExcludedType(item, typeof(Hair))) return true;                 // avoid 
            return false;
        }
        private static bool ExcludedCampComponent(Item item)
        {
            if (item.RootParent is Container cont)
                //if (cont.IsDynamicFeatureSet(Item.FeatureBits_obsolete.CampComponent))
                if (cont.CampComponent)
                    return true;

            return false;
        }
        private static bool ExcludedItemSpawnerManaged(Item item, List<ChestItemSpawner> cis)
        {
            //if (item.RootParent is Container cont)
            //    foreach (ChestItemSpawner spawner in cis)
            //        if (spawner.SpawnContainer == cont)
            //            return true;

            return false;
        }
        private static bool ExcludedSpawnedItem(Item item)
        {
            return Utility.IsSpawnedItem(item);
        }
        private static bool ExcludedType(Item item, Type t)
        {
            if (t.IsAssignableFrom(item.GetType()) || item.RootParent != null && t.IsAssignableFrom(item.RootParent.GetType()))
                return true;
            else return false;
        }
        public static bool SameItem(Item item1, Item item2, bool orEquivalent)
        {
            // looks to be the same newbie gear orEquivalent
            if (item1.GetType() == item2.GetType() || (orEquivalent && IsGear(item1, item2) && SameBase(item1, item2) && SameLayer(item1, item2)))
                if (item1.LootType == item2.LootType || orEquivalent)
                    if (item1.Layer == item2.Layer)
                        if (item1.PlayerCrafted == item2.PlayerCrafted || orEquivalent)
                        {
                            if (item1 is BaseClothing bc1 && item2 is BaseClothing bc2)
                            {
                                if (bc1.Quality == bc2.Quality && bc1.Crafter == bc2.Crafter || orEquivalent)
                                    if (NearSamePowerAttributes(item1, item2))
                                        return true;
                            }
                            else if (item1 is BaseArmor ba1 && item2 is BaseArmor ba2)
                            {
                                if (ba1.Quality == ba2.Quality && ba1.Crafter == ba2.Crafter || orEquivalent)
                                    if (NearSamePowerAttributes(item1, item2))
                                        return true;
                            }
                            else if (item1 is BaseWeapon bw1 && item2 is BaseWeapon bw2)
                            {
                                if (bw1.Quality == bw2.Quality && bw1.Crafter == bw2.Crafter || orEquivalent)
                                    if (NearSamePowerAttributes(item1, item2))
                                        return true;
                            }
                            else
                            {   // just an item: bandage, cheese, etc.
                                //  note: orEquivalent does not apply to items like cheese, bandages, etc because IsGear == false
                                return true;
                            }
                        }

            // sufficiently different
            return false;
        }
        private static bool IsGear(Item item1, Item item2)
        {
            if (item1 is BaseClothing bc1 && item2 is BaseClothing bc2)
            {
                return true;
            }
            else if (item1 is BaseArmor ba1 && item2 is BaseArmor ba2)
            {
                return true;
            }
            else if (item1 is BaseWeapon bw1 && item2 is BaseWeapon bw2)
            {
                return true;
            }

            return false;
        }
        private static bool NearSamePowerAttributes(Item item1, Item item2)
        {   // we don't want to grab the players best gear and drop it on this new player.
            //  these tests should provide at best one level +- the current level
            if (item1 is BaseClothing bc1 && item2 is BaseClothing bc2)
            {
                if (bc1.MagicEffect == bc2.MagicEffect)
                    if (bc1.IOBAlignment == bc2.IOBAlignment)
                        return true;
            }
            else if (item1 is BaseArmor ba1 && item2 is BaseArmor ba2)
            {
                if (Math.Abs((int)ba1.ProtectionLevel - (int)ba2.ProtectionLevel) <= 1)
                    if (Math.Abs((int)ba1.DurabilityLevel - (int)ba2.DurabilityLevel) <= 1)
                        if (Math.Abs((int)ba1.ProtectionLevel - (int)ba2.ProtectionLevel) <= 1)
                            if (ba1.IOBAlignment == ba2.IOBAlignment)
                                return true;

            }
            else if (item1 is BaseWeapon bw1 && item2 is BaseWeapon bw2)
            {
                if (Math.Abs((int)bw1.DamageLevel - (int)bw2.DamageLevel) <= 1)
                    if (Math.Abs((int)bw1.AccuracyLevel - (int)bw2.AccuracyLevel) <= 1)
                        if (Math.Abs((int)bw1.DurabilityLevel - (int)bw2.DurabilityLevel) <= 1)
                            return true;
            }
            return false;
        }
        private static bool SameBase(Item item1, Item item2)
        {
            if (item1 is BaseClothing && item2 is BaseClothing)
                return true;
            else if (item1 is BaseArmor && item2 is BaseArmor)
                return true;
            else if (item1 is BaseWeapon && item2 is BaseWeapon)
                return true;

            return false;
        }
        private static bool SameLayer(Item item1, Item item2)
        {
            if (item1 == null || item2 == null)
                return false;
            return item1.Layer == item2.Layer;
        }
        private static List<Item> ExplodeContainers(Container cont)
        {
            List<Item> list = new List<Item>();
            if (cont.Items != null && cont.Items.Count > 0)
                for (int ix = 0; ix < cont.Items.Count; ix++)
                    if (cont.Items[ix] is Item)
                    {
                        if (cont.Items[ix] is Container contN)
                            list.AddRange(ExplodeContainers(contN));
                        else
                            list.Add(cont.Items[ix] as Item);
                    }

            return list;
        }
    }
}