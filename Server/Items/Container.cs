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

/* Server/Items/Container.cs
 * CHANGELOG:
 *  10/21/2026, Adam (StoreBought provenance)
 *      OnDragDrop: a dropped item placed beside a stack it may not join only because of starter binding or merchant
 *      provenance tells the player why (Item.NotifyStackBlocked). StackItem: the merged stack keeps the StoreBought mark.
 *  10/18/2026, Adam (DropItem overflow, MaxItems 0)
 *      DropItem's overflow rule no longer treats MaxItems 0 as full: like CheckHold, CheckHoldStackDelta and the weight
 *      rule beside it (MaxWeight > 0), 0 means no item limit. Before, Items.Count >= 0 diverted every DropItem into a
 *      RecycleBin for such a container. Positive limits, the weight rule and the exclusions are unchanged.
 *  10/18/2026, Adam (ItemCountText)
 *      Capacity display: ItemCountText reads "109 items" as before, or "109 items (125/125 slots)" when the capacity slots
 *      in use (SlotUsage, as CheckHold counts them: potions one slot each, at any depth) differ from TotalItems and the
 *      container has an item limit. Used by the single-click label and the tooltip (which keeps cliloc 1050044 when the
 *      counts agree). Display only: TotalItems, capacity enforcement and visibility (DisplaysContent,
 *      CheckContentDisplay) are unchanged.
 *  10/17/2026, Adam (SlotUsage)
 *      Public read-only SlotUsage (ComputeContainerSlotUsage), so a capacity check can use the same slot accounting
 *      CheckHold/TryDropItem enforce. No rule changed.
 *  10/16/2026, Adam (OnStackAttempt)
 *      Enforce the virtual slot delta (Item.GetStackingContainerSlotDelta / CheckHoldStackDelta) before StackWith,
 *      matching TryDropItem. Dropping onto an existing stack no longer bypasses MaxItems for slot-scaled items (potions).
 *  10/16/2026, Adam (GetSlotCost, ComputeContainerSlotUsage, CheckHold)
 *      Potions now consume one slot per potion at every ancestor level, regardless of nesting depth.
 *      GetSlotCost = GetContainerSlotCount() + TotalItems + the extra cost of potions nested at any depth; it is used for
 *      container usage and for incoming items in both CheckHold overloads. Items holding no potions cost exactly what
 *      they did before (including TotalItems overrides such as SeedBox).
 *      Refusals from a container that is already over capacity now say so (SendOverCapacityMessage).
 *  6/4/2024, Adam (OnDragDropInto)
 *      trap and move to world 'no draw tiles'
 *      this happens at least when 'Normalize On Lift' items are set to NODRAW because they are not legal, or an intended drop.
 *  11/5/2023, Adam (CheckHold(List<Item> itemList))
 *      Add a new CheckHold function that accepts a list of items to check (may be containers.)
 *  9/25/2023, Adam (Item.AddItem(ref Item item))
 *      Add an override to Item.AddItem() that accepts a reference to the dropped item.
 *      This version is called by Container DropItem since it's the AddItem code that can replace an item with an Enchanted Scroll.
 *      We need this 'ref to' since DropItem does the positioning of the dropped item within the container.
 *  9/5/2023, Adam (ContainerHash) 
 *      we need to sort here in case one of the items has been moved, but still exists
 *      I.e., removing an item then returning it will change the list order resulting
 *      in a different hash
 *  9/4/2023, Adam (ContainerHash)
 *      Useful for comparing containers across saves.
 *      If the hash is the same, the container + all contents are the same.
 *  2/6/22, Adam (DropOverflow)
 *      Allow players to overload a container (backpack on death) if the item is newbied or blessed
 *  11/7/21, Yoar
 *      Implemented ContainerData (RunUO 2.0)
 *      Fixes incorrect container GumpIDs and Bounds
 *  10/27/21, Adam
 * 	DropItem(Item dropped): Check for null item
 *  10/1/21, Adam
 *      Obsolete FailoverList, replaced with Scripts\Engines\RecycleBin\RecycleBin.cs
 *      If you are loading a version 4 database, you will get a prompt asking if you want to delete all of these objects.
 *          Just say YES, and you will be fine. 
 *  9/8/21, Adam
 *      Add a new rule (3) to the failover logic. (PlayerOwned)
 *      This rule excludes stuff like dungeon chests, tmap chests, ransom chests, etc.
 *  7/20/21, Adam (DropItem)
 *      Add a new Failover item to handle container overloading.
 *          This failover item holds a list of items that were placed into a container that was not caught by the higher level functions.
 *          certain functions bypass the usual checks, and this failover 'list' captures thos items and tucks them away on the internal map
 *          where they can be recovered, or will expire and be automaticalll deleted (probably an exploit.)
 *          Also, GM's can do this with commands like [dupinbag... no more, there is no overloading (item or weight wise) allowed on AI.
 *  11/7/10, Adam
 *      Remove separate frequency variable and instead reuse timer.delay value setup at timer creation.
 *      Add more variance to initial timer delay and frequency to smooth dispersal (CPU usage) over time
 *	4/17/09, Adam
 *		Add a new 'factory' type of container to the spawner lootpack processing.
 *		This 'factory' type allows us to select one random item from the pack instead of giving the chance for each item.
 * 7/8/08, Adam
 *		Convert exception logging to use EventSink.InvokeLogException()
 * 2/11/08, Pix
 *      - Removed merged "IsDecoContainer"
 *  6/7/07, Adam
 *      - Add ScheduleFreeze() override called from heartbeat only
 *      nobody should be scheduling freezes before the heartbeat timer (called after startup.)
 *      - Remove the unconditional creation of an Freeze Dry timer in the serialize ctor
 *      - Move a FreezeDry 'after startup' timer to Heartbeat
 *  4/4/06, Adam
 *      Add FindAllItems() to recursively locate and return the list of contained items.
 *  08/05/06 Taran Kain
 *		Changed TryDropItem to at first not check items, then search for a stack to add to, *then* check items.
 *		Previously, if you had 125 items in a container, you could add items to an existing stack manually but not drop it on the container. Now you can.
 *  05/01/06 Taran Kain
 *		Virtualized MaxItems so that it may be overridden.
 *  3/28/06 Taran Kain
 *		Added "(decorative)" tags to property list and single click, disabled DisplaysContent if Deco == true
 *	12/22/05, Adam
 *		make sure we are not FD'ing a player vendor!
 *	12/20/05 TK
 *		Added ability for items with Parent is Mobile to FD (bankboxes, backpacks etc)
 *	12/19/05 Taran Kain
 *		CheckFreezeDry returns true now only if the number of Mobiles with netstates (ie players) within 8 tiles is 0, otherwise falls to base
 *	12/18/05 Taran Kain
 *		Implemented new CanFreezeDry property and CheckFreezeDry() function, changed FreezeTimer to check these - moves logic into OO design
 *	12/14/05 Taran Kain
 *		Merged FreezeDry functionality in, added support for BankBox
 *	5/9/05, Adam
 *		Push the Deco flag down here to the Container level
 */

using Scripts.Misc;
using Server.Diagnostics;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Items
{
    public delegate void OnItemConsumed(Item item, int amount);
    public delegate int CheckItemGroup(Item a, Item b);

    public delegate void ContainerSnoopHandler(Container cont, Mobile from);


    public class Container : Item
    {
        public override Item Dupe(int amount)
        {
            if (this is DungeonTreasureChest)
                ;
            Container? copy = Activator.CreateInstance(GetType()) as Container;

            if (copy == null)
                return null;

            Utility.CopyProperties(this, copy);

            return base.Dupe(copy, amount);
        }
        private static ContainerSnoopHandler m_SnoopHandler;
        public virtual bool AutoFills { get { return false; } } // Hint for duping. When we dupe, don't copy the items as this container will auto-fill
        public bool Empty { get { return Items != null && Items.Count == 0; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public uint ContainerHash
        {
            get
            {
                string hash = string.Empty;
                hash += DeepHash(this, hash);
                return (uint)Utility.GetStableHashCode(hash);
            }
        }
        private string DeepHash(Item item, string hash)
        {
            if (item == null || item.Deleted)
                return hash;

            hash += item.Serial.ToString();
            if (item is Container cont && cont.Items != null && cont.Items.Count > 0)
            {   // we need to sort here in case one of the items has been moved, but still exists
                //  I.e., removing an item then returning it will change the list order resulting
                //  in a different hash
                List<Item> list = new List<Item>(cont.Items);
                list.Sort((p, q) => p.Serial.CompareTo(q.Serial));
                foreach (var inner in list)
                    if (inner is Container cont2)
                        hash += DeepHash(cont2, hash);
                    else
                        hash += inner.Serial.ToString();
            }

            return Utility.GetStableHashCode(hash).ToString();
        }
        public void CloseContainerGumpsForOpeners()
        {
            List<Mobile> openers = this.Openers;

            if (openers == null || openers.Count == 0)
                return;

            lock (openers)
            {
                for (int i = openers.Count - 1; i >= 0; --i)
                {
                    Mobile mob = openers[i];

                    if (mob == null || mob.Deleted || mob.NetState == null)
                    {
                        openers.RemoveAt(i);
                        continue;
                    }

                    // Close the container UI so players cannot "preview" a newly respawned rare.
                    // Container windows are gumps (RunUO: ContainerGump).
                    //mob.CloseGump(typeof(ContainerGump));
                    mob.CloseGump(this.GetType());
                }

                if (openers.Count == 0)
                    this.Openers = null;
            }
        }


        public static ContainerSnoopHandler SnoopHandler
        {
            get { return m_SnoopHandler; }
            set { m_SnoopHandler = value; }
        }

        // Adam: New extended container attributes
        private ExtAttrib m_ExtAttrib = ExtAttrib.None;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Factory
        {
            get
            {
                if (GetAttribFlag(m_ExtAttrib, ExtAttrib.Factory))
                    return true;
                else
                    return false;
            }
            set
            {
                SetAttribFlag(ref m_ExtAttrib, ExtAttrib.Factory, value);
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Deco
        {
            get
            {
                if (GetAttribFlag(m_ExtAttrib, ExtAttrib.Deco))
                    return true;
                else
                    return false;
            }
            set
            {
                SetAttribFlag(ref m_ExtAttrib, ExtAttrib.Deco, value);
            }
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            if (Deco)
            {
                list.Add("decorative");
            }
        }

        private ContainerData m_ContainerData;

        private int m_DropSound;
        private int m_GumpID;
        private int m_MaxItems;

        public ContainerData ContainerData
        {
            get
            {
                if (m_ContainerData == null)
                    UpdateContainerData();

                return m_ContainerData;
            }
            set { m_ContainerData = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int ItemID
        {
            get { return base.ItemID; }
            set
            {
                int oldID = this.ItemID;

                base.ItemID = value;

                if (this.ItemID != oldID)
                    UpdateContainerData();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int GumpID
        {
            get { return (m_GumpID == -1 ? DefaultGumpID : m_GumpID); }
            set { m_GumpID = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DropSound
        {
            get { return (m_DropSound == -1 ? DefaultDropSound : m_DropSound); }
            set { m_DropSound = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int MaxItems
        {
            get { return (m_MaxItems == -1 ? DefaultMaxItems : m_MaxItems); }
            set { m_MaxItems = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int MaxWeight
        {
            get
            {
                int maxWeight;

                if (Parent is Container)
                {
                    maxWeight = ((Container)Parent).MaxWeight;

                    if (maxWeight > 0)
                        maxWeight = Math.Max(maxWeight, DefaultMaxWeight);
                }
                else
                {
                    maxWeight = DefaultMaxWeight;
                }

                return maxWeight;
            }
        }
        public virtual void UpdateContainerData()
        {
            this.ContainerData = ContainerData.GetData(this.ItemID);
        }

        public virtual Rectangle2D Bounds { get { return ContainerData.Bounds; } }
        public virtual int DefaultGumpID { get { return ContainerData.GumpID; } }
        public virtual int DefaultDropSound { get { return ContainerData.DropSound; } }
        public virtual int DefaultMaxItems { get { return m_GlobalMaxItems; } }
        public virtual int DefaultMaxWeight { get { return m_GlobalMaxWeight; } }
        /// <summary>
        /// Policy hook for whether <paramref name="m"/> is allowed to store <paramref name="item"/> in this container.
        /// Default policy preserves legacy behavior via CanStore().
        /// </summary>
        public virtual bool AllowStore(Mobile m, Item item)
        {
            return CanStore(m);
        }

        public virtual bool CanStore(Mobile m)
        {
            return Movable || IsLockedDown || IsSecure || m == Parent;
        }

        public virtual int GetDroppedSound(Item item)
        {
            int dropSound = item.GetDropSound();

            return dropSound != -1 ? dropSound : DropSound;
        }

        public override void OnSnoop(Mobile from)
        {
            if (m_SnoopHandler != null)
                m_SnoopHandler(this, from);
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            if (item.GetItemBool(ItemBoolTable.MustSteal))
            {   // items can now be flagged as must steal
                reject = LRReason.TryToSteal;
                return false;
            }
            return base.CheckLift(from, item, ref reject);
        }

        public bool CheckHold(Mobile m, Item item, bool message)
        {
            return CheckHold(m, item, message, true, 0, 0);

        }

        public bool CheckHold(Mobile m, Item item, bool message, bool checkItems)
        {
            return CheckHold(m, item, message, checkItems, 0, 0);
        }
        public virtual bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            // lets not allow staff to bypass all the checks, GM+ used to be able to overload containers. Not sure I want that
            //  But GM+ should be able to store in containers (backpacks) that are not their own
            if (m.AccessLevel < AccessLevel.GameMaster)
                if (!AllowStore(m, item))
                {
                    if (message)
                        SendCantStoreMessage(m, item);

                    return false;
                }

            // Item-count capacity (MaxItems) check.
            // IMPORTANT:
            // TotalItems is the legacy physical-object count and must not be used as the baseline
            // for our virtual slot model. Use computed slot usage instead.
            int maxItems = MaxItems;

            if (checkItems && maxItems != 0)
            {
                // Own cost plus contents, including potions nested at any depth (0 for virtual items).
                int incomingSlots = GetSlotCost(item);

                int slotUsage = ComputeContainerSlotUsage();
                int totalSlots = slotUsage + plusItems + incomingSlots;

                if (totalSlots > maxItems)
                {
                    if (message)
                    {
                        if (slotUsage > maxItems)
                            SendOverCapacityMessage(m, slotUsage, maxItems);
                        else
                            SendFullItemsMessage(m, item);
                    }

                    return false;
                }
            }

            // Weight capacity (MaxWeight) check.
            int maxWeight = MaxWeight;

            if (maxWeight != 0)
            {
                int totalWeight = TotalWeight + plusWeight + item.TotalWeight + item.PileWeight;

                if (totalWeight > maxWeight)
                {
                    if (message)
                        SendFullWeightMessage(m, item);

                    return false;
                }
            }

            // Propagate the check up the containment chain (nested containers).
            object parent = Parent;

            while (parent != null)
            {
                if (parent is Container c)
                    return c.CheckHold(m, item, message, checkItems, plusItems, plusWeight);

                if (parent is Item parentItem)
                    parent = parentItem.Parent;
                else
                    break;
            }

            return true;
        }

        public bool CheckHold(List<Item> itemList)
        {
            int maxItems = MaxItems;
            int maxWeight = MaxWeight;

            // Baselines must use virtual slot usage, not TotalItems.
            int totalSlots = ComputeContainerSlotUsage();
            int totalWeight = TotalWeight;

            foreach (Item item in itemList)
            {
                // ---- Item-count capacity (MaxItems) ----
                if (maxItems != 0)
                {
                    // Own cost plus contents, including potions nested at any depth (0 for virtual items).
                    int incomingSlots = GetSlotCost(item);

                    if ((totalSlots + incomingSlots) > maxItems)
                        return false;

                    // Update running virtual slot total
                    totalSlots += incomingSlots;
                }

                // ---- Weight capacity (MaxWeight) ----
                if (maxWeight != 0)
                {
                    int incomingWeight = item.TotalWeight + item.PileWeight;

                    if ((totalWeight + incomingWeight) > maxWeight)
                        return false;

                    // Update running virtual total weight
                    totalWeight += incomingWeight;
                }
            }

            // Propagate up containment chain (nested containers)
            object parent = Parent;

            while (parent != null)
            {
                if (parent is Container cont)
                {
                    if (!cont.CheckHold(itemList))
                        return false;

                    break;
                }
                else if (parent is Item parentItem)
                {
                    parent = parentItem.Parent;
                }
                else
                {
                    break;
                }
            }

            return true;
        }

        public override void AddItem(Item item)
        {
            if (item == null || item.Deleted || item.Parent == this)
            { return; }
            base.AddItem(item);
            ScheduleRefresh();
        }

        public virtual void SendFullItemsMessage(Mobile to, Item item)
        {
            to.SendMessage("That container cannot hold more items.");
        }

        public virtual void SendFullWeightMessage(Mobile to, Item item)
        {
            to.SendMessage("That container cannot hold more weight.");
        }

        public virtual void SendCantStoreMessage(Mobile to, Item item)
        {
            to.SendLocalizedMessage(500176); // That is not your container, you can't store things here.
        }
        private bool CheckPublicContainer(Item item)
        {
            if (this.RootParent == null && this is not Corpse && Multis.BaseHouse.FindHouseAt(item) == null)
                return true;
            return false;
        }
        public virtual bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (!CheckHold(from, item, true, true))
                return false;

            if (from.DenyAccessPublicContainer && CheckPublicContainer(item))
            {
                return false;
            }

            if (item.ItemID == 1 /*NO DRAW*/)
            {   // this happens at least when 'Normalize On Lift' items are set to NODRAW because they are not legal, or an intended drop.
                item.ClearBounce();
                item.MoveToWorld(from.Location, from.Map);
                return false;
            }

            item.Location = new Point3D(p.m_X, p.m_Y, 0);
            AddItem(item);

            from.SendSound(GetDroppedSound(item), GetWorldLocation());

            return true;
        }
        public int ConsumeTotal(Type[] types, int[] amounts)
        {
            return ConsumeTotal(types, amounts, true, null);
        }

        public int ConsumeTotal(Type[] types, int[] amounts, bool recurse, OnItemConsumed? callback)
        {
            try
            {
                if (types.Length != amounts.Length)
                    throw new ArgumentException();

                Item[][] items = new Item[types.Length][];
                int[] totals = new int[types.Length];
                for (int i = 0; i < types.Length; ++i)
                {
                    items[i] = FindItemsByType(types[i], recurse);

                    for (int j = 0; j < items[i].Length; ++j)
                        totals[i] += items[i][j].Amount;

                    if (totals[i] < amounts[i])
                        return i;
                }

                for (int i = 0; i < types.Length; ++i)
                {
                    int need = amounts[i];

                    for (int j = 0; j < items[i].Length; ++j)
                    {
                        Item item = items[i][j];

                        int theirAmount = item.Amount;

                        if (theirAmount < need)
                        {
                            if (callback != null)
                                callback(item, theirAmount);

                            item.Delete();
                            need -= theirAmount;
                        }
                        else
                        {
                            if (callback != null)
                                callback(item, need);

                            item.Consume(need);
                            break;
                        }
                    }
                }
            }
            finally
            {
                ScheduleRefresh();
            }

            return -1;
        }
        public int ConsumeTotal(Type[] types, int[] amounts, bool recurse, OnItemConsumed? callback, Func<Item, bool>? canConsume)
        {
            try
            {
                if (types.Length != amounts.Length)
                    throw new ArgumentException();

                Item[][] items = new Item[types.Length][];
                int[] totals = new int[types.Length];

                for (int i = 0; i < types.Length; ++i)
                {
                    var all = FindItemsByType(types[i], recurse);
                    items[i] = canConsume == null ? all : all.Where(canConsume).ToArray();

                    for (int j = 0; j < items[i].Length; ++j)
                        totals[i] += items[i][j].Amount;

                    if (totals[i] < amounts[i])
                        return i;
                }

                for (int i = 0; i < types.Length; ++i)
                {
                    int need = amounts[i];

                    for (int j = 0; j < items[i].Length; ++j)
                    {
                        var it = items[i][j];
                        int have = it.Amount;

                        if (have < need)
                        {
                            callback?.Invoke(it, have);
                            it.Delete();
                            need -= have;
                        }
                        else
                        {
                            callback?.Invoke(it, need);
                            it.Consume(need);
                            break;
                        }
                    }
                }
            }
            finally
            {
                ScheduleRefresh();
            }

            return -1;
        }

        public enum ConsumeCheckStatus
        {
            Error,          // Pack null or someother error
            Ok,             // sufficient owned items found
            NotEnough,      // shortfall with no blocked (filtered) items
            Filtered        // shortfall because some items were blocked by ownership
        }

        public ConsumeCheckStatus CheckConsumeTotal(
            Type[] types,
            int[] amounts,
            PlayerMobile? actor,              // who we're checking ownership against
            bool recurse,
            out int failingIndex,            // index in 'types' that failed, or -1
            out int missingAmount,           // how many short for that failing type
            out int filteredCount            // how many items were filtered for that type
        )
        {
            if (types == null || amounts == null || types.Length != amounts.Length)
                throw new ArgumentException();

            failingIndex = -1;
            missingAmount = 0;
            filteredCount = 0;

            for (int i = 0; i < types.Length; ++i)
            {
                // Ownership-enforced search; we also get how many were filtered out
                var items = FindItemsByType(types[i], recurse, actor, enforceOwnership: true, out var filtered);
                var total = 0;
                for (int j = 0; j < items.Length; ++j)
                    total += items[j].Amount;

                if (total < amounts[i])
                {
                    failingIndex = i;
                    missingAmount = amounts[i] - total;
                    filteredCount = filtered;

                    return filtered > 0 ? ConsumeCheckStatus.Filtered
                        : ConsumeCheckStatus.NotEnough;
                }
            }

            return ConsumeCheckStatus.Ok;
        }
        public ConsumeCheckStatus CheckConsumeTotal(
            Type type,
            int amount,
            PlayerMobile actor,
            bool recurse,
            out int missingAmount,
            out int filteredCount
        )
        {
            var status = CheckConsumeTotal(
                types: new[] { type },
                amounts: new[] { amount },
                actor: actor,
                recurse: recurse,
                out var failingIndex,
                out missingAmount,
                out filteredCount
            );

            // normalize for single-type call
            if (status == ConsumeCheckStatus.Ok)
                missingAmount = 0;

            return status;
        }

        public int GetAmount(Type type)
        {
            return GetAmount(type, true);
        }

        public int GetAmount(Type type, bool recurse)
        {
            Item[] items = FindItemsByType(type, recurse);

            int amount = 0;

            for (int i = 0; i < items.Length; ++i)
                amount += items[i].Amount;

            return amount;
        }

        public int GetAmount(Type[] types)
        {
            return GetAmount(types, true);
        }

        public int GetAmount(Type[] types, bool recurse)
        {
            Item[] items = FindItemsByType(types, recurse);

            int amount = 0;

            for (int i = 0; i < items.Length; ++i)
                amount += items[i].Amount;

            return amount;
        }

        public bool ConsumeTotal(Type type, int amount)
        {
            return ConsumeTotal(type, amount, true, null);
        }

        public bool ConsumeTotal(Type type, int amount, bool recurse)
        {
            return ConsumeTotal(type, amount, recurse, null);
        }

        public bool ConsumeTotal(Type type, int amount, bool recurse, OnItemConsumed? callback)
        {
            try
            {
                Item[] items = FindItemsByType(type, recurse);

                int total = 0;

                for (int i = 0; i < items.Length; ++i)
                    total += items[i].Amount;

                if (total >= amount)
                {
                    // We've enough, so consume it

                    int need = amount;

                    for (int i = 0; i < items.Length; ++i)
                    {
                        Item item = items[i];

                        int theirAmount = item.Amount;

                        if (theirAmount < need)
                        {
                            if (callback != null)
                                callback(item, theirAmount);

                            item.Delete();
                            need -= theirAmount;
                        }
                        else
                        {
                            if (callback != null)
                                callback(item, need);

                            item.Consume(need);

                            return true;
                        }
                    }
                }
            }
            finally
            {
                ScheduleRefresh();
            }

            return false;
        }

        public int ConsumeUpTo(Type type, int amount)
        {
            return ConsumeUpTo(type, amount, true);
        }

        public int ConsumeUpTo(Type type, int amount, bool recurse)
        {
            try
            {
                int consumed = 0;

                Queue toDelete = new Queue();

                RecurseConsumeUpTo(this, type, amount, recurse, ref consumed, toDelete);

                while (toDelete.Count > 0)
                    ((Item)toDelete.Dequeue()).Delete();


                return consumed;
            }
            finally { ScheduleRefresh(); }
        }

        private static void RecurseConsumeUpTo(Item current, Type type, int amount, bool recurse, ref int consumed, Queue toDelete)
        {
            try
            {
                if (current != null && current.Items.Count > 0)
                {
                    List<Item> list = current.Items;

                    for (int i = 0; i < list.Count; ++i)
                    {
                        Item item = (Item)list[i];

                        if (type.IsAssignableFrom(item.GetType()))
                        {
                            int need = amount - consumed;
                            int theirAmount = item.Amount;

                            if (theirAmount <= need)
                            {
                                toDelete.Enqueue(item);
                                consumed += theirAmount;
                            }
                            else
                            {
                                item.Amount -= need;
                                consumed += need;

                                return;
                            }
                        }
                        else if (recurse && item is Container)
                        {
                            RecurseConsumeUpTo(item, type, amount, recurse, ref consumed, toDelete);
                        }
                    }
                }
            }
            finally
            {
                if (current is Container c)
                    c.ScheduleRefresh();
            }
        }

        // return a list of all contained items and from all sub containers
        public ArrayList FindAllItems()
        {
            ArrayList list = new ArrayList();
            FindAllItems(this, list);
            return list;
        }

        public void FindAllItems(Item current, ArrayList list)
        {
            if (current == null || current.Deleted == true)
                return;

            list.Add(current);

            foreach (Item item in current.Items)
            {
                if (item == null || item.Deleted == true)
                    continue;

                if (item is Container)
                    FindAllItems(item, list);
                else
                    list.Add(item);
            }

            return;
        }


        // ===================== FindItem[s] by Type (Unified, Ownership-aware) =====================
#nullable enable

        [Flags]
        public enum FindFlags
        {
            None = 0,
            Recurse = 1,
            EnforceOwnership = 2
        }

        // -------- Core traversal (single source of truth) --------
        private static IEnumerable<Item> EnumerateItems(Item root, bool recurse)
        {
            if (root == null || root.Items.Count == 0)
                yield break;

            var items = root.Items;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                yield return it;

                if (recurse && it is Container)
                {
                    foreach (var sub in EnumerateItems(it, true))
                        yield return sub;
                }
            }
        }

        private static bool MatchesType(Item it, Type type)
            => type != null && type.IsAssignableFrom(it.GetType());

        private static bool MatchesAnyType(Item it, Type[]? types)
        {
            if (types == null || types.Length == 0)
                return false;

            var t = it.GetType();
            for (int i = 0; i < types.Length; i++)
            {
                var want = types[i];
                if (want != null && want.IsAssignableFrom(t))
                    return true;
            }
            return false;
        }

        // Ownership filter is ONLY applied if actor != null and enforceOwnership == true.
        private static List<Item> CoreFindItems(
            Item root,
            Func<Item, bool> typeMatch,
            bool recurse,
            PlayerMobile? actor,
            bool enforceOwnership,
            out int filtered)
        {
            filtered = 0;
            var result = new List<Item>();

            foreach (var it in EnumerateItems(root, recurse))
            {
                if (!typeMatch(it))
                    continue;

                if (enforceOwnership && actor != null && !StartingLootMonitor.CanUse(actor, it))
                {
                    filtered++;
                    continue;
                }

                result.Add(it);
            }

            return result;
        }

        // ===================== Non-Generic FindItemsByType (Type) =====================
        // Legacy: unchanged behavior
        public Item[] FindItemsByType(Type type) => FindItemsByType(type, true);

        public Item[] FindItemsByType(Type type, bool recurse)
        {
            var list = CoreFindItems(this, it => MatchesType(it, type), recurse, actor: null, enforceOwnership: false, out _);
            return list.ToArray();
        }

        // New: with ownership filtering (caller ignores count)
        public Item[] FindItemsByType(Type type, bool recurse, PlayerMobile actor, bool enforceOwnership)
        {
            var list = CoreFindItems(this, it => MatchesType(it, type), recurse, actor, enforceOwnership, out _);
            return list.ToArray();
        }

        // New: with ownership filtering + filtered count
        public Item[] FindItemsByType(Type type, bool recurse, PlayerMobile? actor, bool enforceOwnership, out int filtered)
        {
            var list = CoreFindItems(this, it => MatchesType(it, type), recurse, actor, enforceOwnership, out filtered);
            return list.ToArray();
        }

        // ===================== Non-Generic FindItemsByType (Type[]) =====================
        // Legacy: unchanged behavior
        public Item[] FindItemsByType(Type[] types) => FindItemsByType(types, true);

        public Item[] FindItemsByType(Type[] types, bool recurse)
        {
            var list = CoreFindItems(this, it => MatchesAnyType(it, types), recurse, actor: null, enforceOwnership: false, out _);
            return list.ToArray();
        }

        // New
        public Item[] FindItemsByType(Type[] types, bool recurse, PlayerMobile actor, bool enforceOwnership)
        {
            var list = CoreFindItems(this, it => MatchesAnyType(it, types), recurse, actor, enforceOwnership, out _);
            return list.ToArray();
        }

        public Item[] FindItemsByType(Type[] types, bool recurse, PlayerMobile? actor, bool enforceOwnership, out int filtered)
        {
            var list = CoreFindItems(this, it => MatchesAnyType(it, types), recurse, actor, enforceOwnership, out filtered);
            return list.ToArray();
        }

        // ===================== Non-Generic FindItemByType (single) =====================
        // Type
        public Item? FindItemByType(Type type) => FindItemByType(type, true);

        public Item? FindItemByType(Type type, bool recurse)
        {
            foreach (var it in EnumerateItems(this, recurse))
                if (MatchesType(it, type))
                    return it;
            return null;
        }

        public Item? FindItemByType(Type type, bool recurse, PlayerMobile actor, bool enforceOwnership)
        {
            foreach (var it in EnumerateItems(this, recurse))
            {
                if (!MatchesType(it, type))
                    continue;

                if (enforceOwnership && actor != null && !StartingLootMonitor.CanUse(actor, it))
                    continue;

                return it;
            }
            return null;
        }

        // Type[]
        public Item? FindItemByType(Type[] types) => FindItemByType(types, true);

        public Item? FindItemByType(Type[] types, bool recurse)
        {
            foreach (var it in EnumerateItems(this, recurse))
                if (MatchesAnyType(it, types))
                    return it;
            return null;
        }

        public Item? FindItemByType(Type[] types, bool recurse, PlayerMobile actor, bool enforceOwnership)
        {
            foreach (var it in EnumerateItems(this, recurse))
            {
                if (!MatchesAnyType(it, types))
                    continue;

                if (enforceOwnership && actor != null && !StartingLootMonitor.CanUse(actor, it))
                    continue;

                return it;
            }
            return null;
        }

        // ===================== Generic FindItemsByType<T> =====================
        public List<T> FindItemsByType<T>() where T : Item => FindItemsByType<T>(true, null);

        public List<T> FindItemsByType<T>(bool recurse) where T : Item => FindItemsByType<T>(recurse, null);

        public List<T> FindItemsByType<T>(Predicate<T>? predicate) where T : Item => FindItemsByType<T>(true, predicate);

        public List<T> FindItemsByType<T>(bool recurse, Predicate<T>? predicate) where T : Item
        {
            var t = typeof(T);
            var list = new List<T>();
            foreach (var it in EnumerateItems(this, recurse))
            {
                if (!t.IsAssignableFrom(it.GetType()))
                    continue;

                var typed = (T)it;
                if (predicate == null || predicate(typed))
                    list.Add(typed);
            }
            return list;
        }

        // New: Generic with ownership filtering
        public List<T> FindItemsByType<T>(bool recurse, Predicate<T>? predicate, PlayerMobile actor, bool enforceOwnership) where T : Item
        {
            var t = typeof(T);
            var list = new List<T>();
            foreach (var it in EnumerateItems(this, recurse))
            {
                if (!t.IsAssignableFrom(it.GetType()))
                    continue;

                if (enforceOwnership && actor != null && !StartingLootMonitor.CanUse(actor, it))
                    continue;

                var typed = (T)it;
                if (predicate == null || predicate(typed))
                    list.Add(typed);
            }
            return list;
        }

        // ===================== Generic FindItemByType<T> (single) =====================
        public T? FindItemByType<T>() where T : Item => FindItemByType<T>(true, null);

        public T? FindItemByType<T>(bool recurse) where T : Item => FindItemByType<T>(recurse, null);

        public T? FindItemByType<T>(Predicate<T>? predicate) where T : Item => FindItemByType<T>(true, predicate);

        public T? FindItemByType<T>(bool recurse, Predicate<T>? predicate) where T : Item
        {
            var t = typeof(T);
            foreach (var it in EnumerateItems(this, recurse))
            {
                if (!t.IsAssignableFrom(it.GetType()))
                    continue;

                var typed = (T)it;
                if (predicate == null || predicate(typed))
                    return typed;
            }
            return null;
        }

        // New: Generic single with ownership filtering
        public T? FindItemByType<T>(bool recurse, Predicate<T>? predicate, PlayerMobile actor, bool enforceOwnership) where T : Item
        {
            var t = typeof(T);
            foreach (var it in EnumerateItems(this, recurse))
            {
                if (!t.IsAssignableFrom(it.GetType()))
                    continue;

                if (enforceOwnership && actor != null && !StartingLootMonitor.CanUse(actor, it))
                    continue;

                var typed = (T)it;
                if (predicate == null || predicate(typed))
                    return typed;
            }
            return null;
        }
#nullable disable


        private static bool InTypeList(Item item, Type[] types)
        {
            Type t = item.GetType();

            for (int i = 0; i < types.Length; ++i)
                if (types[i].IsAssignableFrom(t))
                    return true;

            return false;
        }

        private static void SetSaveFlag(ref SaveFlag flags, SaveFlag toSet, bool setIf)
        {
            if (setIf)
                flags |= toSet;
        }

        private static bool GetSaveFlag(SaveFlag flags, SaveFlag toGet)
        {
            return ((flags & toGet) != 0);
        }

        [Flags]
        private enum SaveFlag : byte
        {
            None = 0x00000000,
            MaxItems = 0x00000001,
            GumpID = 0x00000002,
            DropSound = 0x00000004,
        }

        private static void SetAttribFlag(ref ExtAttrib flags, ExtAttrib toSet, bool setIf)
        {
            if (setIf)
                flags |= toSet;
            else
                ClearAttribFlag(ref flags, toSet);
        }

        private static bool GetAttribFlag(ExtAttrib flags, ExtAttrib toGet)
        {
            return ((flags & toGet) != 0);
        }

        private static void ClearAttribFlag(ref ExtAttrib flags, ExtAttrib toClear)
        {
            flags &= ~toClear;
        }

        [Flags]
        private enum ExtAttrib : byte
        {
            None = 0x0000,
            Deco = 0x0001,          // can items be stored here?
            Factory = 0x0002,       // used in the spawner lootpack code
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 5;
            writer.Write(version); // version

            switch (version)
            {
                case 5:
                    {
                        goto default;
                    }
                case 4:
                    {
                        // obeolete
                        break;
                    }
                default:
                    {
                        writer.Write((byte)m_ExtAttrib);    // version 3

                        SaveFlag flags = SaveFlag.None;
                        SetSaveFlag(ref flags, SaveFlag.MaxItems, m_MaxItems != -1);
                        SetSaveFlag(ref flags, SaveFlag.GumpID, m_GumpID != -1);
                        SetSaveFlag(ref flags, SaveFlag.DropSound, m_DropSound != -1);

                        writer.Write((byte)flags);

                        if (GetSaveFlag(flags, SaveFlag.MaxItems))
                            writer.WriteEncodedInt((int)m_MaxItems);

                        if (GetSaveFlag(flags, SaveFlag.GumpID))
                            writer.WriteEncodedInt((int)m_GumpID);

                        if (GetSaveFlag(flags, SaveFlag.DropSound))
                            writer.WriteEncodedInt((int)m_DropSound);
                        break;
                    }
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 5:
                    {   /// skip version 4 - obsolete
                        goto case 3;
                    }
                case 4:
                    {   // obsolete
                        // FailoverList only kept around for backwards compatibility
                        // If you are loading a version 4 database, you will get a prompt asking if you want to delete all of these objects.
                        //      Just say YES, and you will be fine. 
                        reader.ReadItem();
                        goto case 3;
                    }
                case 3:
                    {
                        m_ExtAttrib = (ExtAttrib)reader.ReadByte();
                        goto case 2;
                    }
                case 2:
                    {
                        if (version < 3)
                            m_ExtAttrib = ExtAttrib.None;

                        SaveFlag flags = (SaveFlag)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.MaxItems))
                            m_MaxItems = reader.ReadEncodedInt();
                        else
                            m_MaxItems = -1;

                        if (GetSaveFlag(flags, SaveFlag.GumpID))
                            m_GumpID = reader.ReadEncodedInt();
                        else
                            m_GumpID = -1;

                        if (GetSaveFlag(flags, SaveFlag.DropSound))
                            m_DropSound = reader.ReadEncodedInt();
                        else
                            m_DropSound = -1;

                        break;
                    }
                case 1:
                    {
                        m_MaxItems = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 1)
                            m_MaxItems = m_GlobalMaxItems;

                        m_GumpID = reader.ReadInt();
                        m_DropSound = reader.ReadInt();

                        if (m_GumpID == DefaultGumpID)
                            m_GumpID = -1;

                        if (m_DropSound == DefaultDropSound)
                            m_DropSound = -1;

                        if (m_MaxItems == DefaultMaxItems)
                            m_MaxItems = -1;

                        //m_Bounds = new Rectangle2D( reader.ReadPoint2D(), reader.ReadPoint2D() );
                        reader.ReadPoint2D();
                        reader.ReadPoint2D();

                        break;
                    }
            }

            UpdateContainerData();
        }

        private static int m_GlobalMaxItems = 125;
        private static int m_GlobalMaxWeight = 400;

        public static int GlobalMaxItems { get { return m_GlobalMaxItems; } set { m_GlobalMaxItems = value; } }
        public static int GlobalMaxWeight { get { return m_GlobalMaxWeight; } set { m_GlobalMaxWeight = value; } }

        public Container(int itemID)
            : base(itemID)
        {
            m_GumpID = -1;
            m_DropSound = -1;
            m_MaxItems = -1;

            UpdateContainerData();
        }

        public Container(Serial serial)
            : base(serial)
        {
        }

        public virtual bool OnStackAttempt(Mobile from, Item stack, Item dropped)
        {
            if (!CheckHold(from, dropped, true, false))
                return false;

            // Stacking may not add an item object, but it can add virtual slots (e.g., potions cost Amount).
            // Enforce the slot delta BEFORE committing the merge, exactly as TryDropItem does.
            int deltaSlots = Item.GetStackingContainerSlotDelta(stack, dropped);

            if (deltaSlots > 0 && !CheckHoldStackDelta(from, dropped, true, deltaSlots))
                return false;

            return stack.StackWith(from, dropped);
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (TryDropItem(from, dropped, true))
            {
                if (from.DenyAccessPublicContainer && CheckPublicContainer(dropped))
                {
                    return false;
                }

                // placed beside a stack it may not join (starter binding, merchant provenance): say why
                if (!dropped.Deleted && dropped.Parent == this)
                    foreach (Item item in Items)
                        if (item != dropped && Item.GetStackBlock(item, dropped) != StackBlock.None)
                        {
                            Item.NotifyStackBlocked(from, item, dropped);
                            break;
                        }

                from.SendSound(GetDroppedSound(dropped), GetWorldLocation());

                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Attempts to drop an item into this container.
        /// 
        /// IMPORTANT:
        /// Container "item count" (TotalItems) reflects physical item objects only.
        /// It does NOT reflect virtual slot usage introduced by stacking rules
        /// (e.g., potions consuming slots equal to Amount).
        ///
        /// Stacking may not create a new item object, but it can increase the
        /// container's virtual slot usage. Therefore, stacking operations must
        /// enforce slot-capacity via GetContainerSlotCount / delta-slot checks,
        /// rather than relying solely on TotalItems.
        /// </summary>
        public virtual bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            // First pass: ignore item-count limits so we can attempt stacking, but still enforce
            // "hard" constraints (CanStore, weight, parent-chain rules, etc).
            if (!CheckHold(from, dropped, sendFullMessage, checkItems: false))
                return false;

            List<Item> list = Items;

            for (int i = 0; i < list.Count; ++i)
            {
                Item target = list[i];

                if (target is Container)
                    continue;

                // If this would stack, enforce virtual slot delta BEFORE committing the merge.
                if (Item.StackableRule(target, dropped))
                {
                    int deltaSlots = Item.GetStackingContainerSlotDelta(target, dropped);

                    // If stacking would increase virtual slot usage beyond capacity, reject (all-or-nothing).
                    if (deltaSlots > 0 && !CheckHoldStackDelta(from, dropped, sendFullMessage, deltaSlots))
                        return false;

                    if (target.StackWith(from, dropped, false))
                        return true;

                    // If StackableRule said yes but StackWith failed (rare), keep scanning.
                }
            }

            // No stacking target: now enforce item-count limits normally.
            if (!CheckHold(from, dropped, sendFullMessage))
                return false;

            DropItem(dropped);
            return true;
        }

        /// <summary>
        /// Like CheckHold(..., checkItems:true) but for a stack-merge: the container will not gain a new
        /// top-level item object; instead it gains "virtual slots" equal to deltaSlots.
        /// Weight still increases by the dropped item's weight.
        /// </summary>
        protected virtual bool CheckHoldStackDelta(Mobile m, Item dropped, bool message, int deltaSlots)
        {
            // Treat virtual items as consuming no slots.
            if (dropped.IsVirtualItem)
                deltaSlots = 0;

            // Enforce this container's MaxItems using virtual slot usage (not TotalItems).
            int maxItems = MaxItems;

            if (maxItems != 0)
            {
                int slotUsage = ComputeContainerSlotUsage();

                if ((slotUsage + deltaSlots) > maxItems)
                {
                    if (message)
                    {
                        if (slotUsage > maxItems)
                            SendOverCapacityMessage(m, slotUsage, maxItems);
                        else
                            SendFullItemsMessage(m, dropped);
                    }

                    return false;
                }
            }

            // Enforce weight normally (stacking still adds weight).
            int maxWeight = MaxWeight;

            if (maxWeight != 0 && (TotalWeight + dropped.TotalWeight + dropped.PileWeight) > maxWeight)
            {
                if (message)
                    SendFullWeightMessage(m, dropped);

                return false;
            }

            // Propagate the same delta check up the containment chain.
            object parent = Parent;

            while (parent != null)
            {
                if (parent is Container c)
                    return c.CheckHoldStackDelta(m, dropped, message, deltaSlots);

                if (parent is Item parentItem)
                    parent = parentItem.Parent;
                else
                    break;
            }

            return true;
        }
        // Compute current "virtual slot usage" for this container.
        // This is intentionally computed on demand (baby step) to validate behavior before caching.
        /// <summary>Slots in use, as CheckHold and TryDropItem count them (read-only).</summary>
        public int SlotUsage { get { return ComputeContainerSlotUsage(); } }

        /// <summary>
        /// The item count as players see it, "109 items". TotalItems counts a potion stack once while capacity (CheckHold)
        /// counts each potion, at any depth; when the two differ, and the container has an item limit, the slots in use
        /// are appended: "109 items (125/125 slots)". Display only: nothing is counted or enforced differently.
        /// </summary>
        public string ItemCountText
        {
            get
            {
                int maxItems = MaxItems;    // 0: no item limit, so the slot count does not matter
                int slots = maxItems == 0 ? TotalItems : SlotUsage;
                return slots == TotalItems
                    ? string.Format("{0} items", TotalItems)
                    : string.Format("{0} items ({1}/{2} slots)", TotalItems, slots, maxItems);
            }
        }

        protected int ComputeContainerSlotUsage()
        {
            int total = 0;

            List<Item> list = Items;

            for (int i = 0; i < list.Count; ++i)
            {
                Item it = list[i];

                if (it == null || it.Deleted)
                    continue;

                // Virtual items do not consume slots.
                if (it.IsVirtualItem)
                    continue;

                total += GetSlotCost(it);
            }

            return total;
        }

        /// <summary>
        /// The slots 'item' occupies in any container that holds it, directly or at any depth:
        /// its own cost (default 1; potions cost Amount), plus the legacy physical count of its
        /// contents (TotalItems), plus the extra cost of slot-scaled items (potions) nested inside it.
        /// For items that contain no potions this is exactly the legacy GetContainerSlotCount() + TotalItems,
        /// so ordinary items, non-potion stacks, and TotalItems overrides (e.g., SeedBox) are unchanged.
        /// </summary>
        public static int GetSlotCost(Item item)
        {
            if (item == null || item.Deleted || item.IsVirtualItem)
                return 0;

            return item.GetContainerSlotCount() + item.TotalItems + NestedSlotSurcharge(item);
        }

        // TotalItems counts each nested object as one slot. Add what nested slot-scaled items cost beyond that,
        // so every potion consumes one slot at every ancestor level regardless of nesting depth.
        private static int NestedSlotSurcharge(Item parent)
        {
            int extra = 0;

            List<Item> list = parent.Items;

            for (int i = 0; i < list.Count; ++i)
            {
                Item it = list[i];

                if (it == null || it.Deleted || it.IsVirtualItem)
                    continue;

                extra += Math.Max(0, it.GetContainerSlotCount() - 1);

                if (it.Items.Count > 0)
                    extra += NestedSlotSurcharge(it);
            }

            return extra;
        }

        public virtual void SendOverCapacityMessage(Mobile to, int slotUsage, int maxItems)
        {
            to.SendMessage("That container is over capacity ({0} of {1} slots). Remove items before adding to or rearranging it.", slotUsage, maxItems);
        }
        public virtual void Destroy()
        {
            Point3D loc = GetWorldLocation();
            Map map = Map;

            for (int i = Items.Count - 1; i >= 0; --i)
            {
                if (i < Items.Count)
                {
                    ((Item)Items[i]).SetLastMoved();
                    ((Item)Items[i]).MoveToWorld(loc, map);
                }
            }

            Delete();
        }

        public void UpdateAllTotals()
        {
            // tell the container to update it's notion of what it has
            UpdateTotals();
            object parent = this;
            while (parent != null)
            {
                if (parent is Item)
                {
                    (parent as Item).UpdateTotals();
                    parent = (parent as Item).Parent;
                }
                else if (parent is Mobile)
                {
                    (parent as Mobile).UpdateTotals();
                    parent = null;  // mobiles don't have parents
                }
            }
        }

        #region Refresh Totals
        private bool m_PendingRefresh;
        private static readonly TimeSpan RefreshDelay = TimeSpan.FromMilliseconds(200);  // tune: 100-500ms

        private void ScheduleRefresh()
        {
            if (!m_PendingRefresh)
            {
                m_PendingRefresh = true;
                Timer.DelayCall(RefreshDelay, RefreshTotals);
            }
        }

        private void RefreshTotals()
        {
            m_PendingRefresh = false;

            if (Deleted || RootParent is not Mobile from || from.Deleted || from.NetState == null)
                return;

            UpdateAllTotals();
        }
        #endregion Refresh Totals
        public virtual bool PlayerOwned()
        {
            Item item = this;
            Mobile mobile = null;
            if (this.RootParent != null && this.RootParent is Item)
                item = this.RootParent as Item;
            else if (this.RootParent != null && this.RootParent is Mobile)
                mobile = this.RootParent as Mobile;

            if (item != null)
                if (BaseHouse.FindHouseAt(item) != null)
                    return true;

            if (mobile != null)
                return true;

            if (item != null)
                if (BaseBoat.FindBoatAt(item) != null)
                    return true;

            return false;
        }

        public virtual void DropOverflow(Item dropped, string logdata)
        {
            Engines.RecycleBin.RecycleBin.Add(dropped, this, logdata);
        }
        private bool SpecialLoot(Item dropped)
        {
            if (this.Parent is Mobile m && m != null && m.Player)
                if (m.Items != null && m.Items.Contains(dropped))
                    return dropped.GetFlag(LootType.Newbied) || dropped.GetFlag(LootType.Blessed);
            return false;
        }
        /// <summary>
        /// Drop an item into a stack, or create a new stack if possible.
        /// </summary>
        /// <param name="dropped"></param>
        public void StackItem(Item dropped)
        {
            bool found = false;
            foreach (Item item in this.Items)
            {
                if (Item.StackableRule(item, dropped))
                {
                    item.LootType = (item.LootType != dropped.LootType ? LootType.Regular : item.LootType);
                    if (dropped.StoreBought)
                        item.StoreBought = true;    // merchant provenance is sticky
                    item.Amount += dropped.Amount;
                    found = true;
                    break;
                }
            }

            if (found)
                dropped.Delete();
            else
                DropItem(dropped);
        }
        public void DropRare(Item dropped)
        {
            if (dropped != null)
            {
                dropped.LootType = LootType.Rare;
                DropItem(dropped);
            }
        }
        private Memory OverflowMessageGiven = new Memory();
        public virtual void DropItem(Item dropped)
        {
            if (dropped == null)
                return;

            #region DropOverflow
            // 7/20/21, Adam:
            // Certain functions can call this path directly, bypassing the usual container overload checks
            // (weight and item count). GMs can do this with [dupeinbag], and certain exploits have historically
            // been able to achieve the same result.
            //
            // This is no longer allowed. Each container now has an associated, expiring RecycleBin.
            // Items that would otherwise overload a container are diverted into this RecycleBin instead.
            // Unchecked overloads can lead to serious problems, including client disconnects, lag, and
            // general instability.
            //
            // RecycleBin contents are stored on the internal map. In the case of legitimate player mistakes,
            // these items can be recovered. In the case of GM error or exploit-driven overloads, the
            // RecycleBin will automatically self-destruct after 10 days. All activity is logged.

            // Inclusion rules: conditions that qualify an item for recycle-bin handling
            bool incl1 = this.MaxItems > 0 && this.Items.Count >= this.MaxItems;   // item count overflow (0: no item limit, as in CheckHold)
            bool incl2 = this.MaxWeight > 0 && this.TotalWeight >= this.MaxWeight; // weight overflow

            // Exclusion rules: conditions that disqualify an item from recycle-bin handling
            bool excl1 = (this.Movable == false && !PlayerOwned())                 // system / event containers
                         || this.Map == Map.Internal;

            bool excl2 = SpecialLoot(dropped);                                     // preserve special loot
            bool excl3 = this is RecycleBin;                                       // recycle bins are terminal

            bool shouldRecycle = (incl1 || incl2) && !(excl1 || excl2 || excl3);

            if (shouldRecycle)
            {
                #region log it
                string addtl = "";
                if (incl1) addtl += "(items)";
                if (incl2) addtl += "(weight)";

                Mobile root = this.RootParent as Mobile;

                string actorText;
                string guidText;

                if (root != null)
                {
                    actorText = root.ToString();
                    guidText = string.Format(" GUID:{0}", root.AcctGUID);
                }
                else
                {
                    actorText = "Unknown";
                    guidText = "";
                }

                string text = string.Format(
                    "{0}{1} Trying to drop: {2} into container: {3} error: {4}",
                    actorText,
                    guidText,
                    dropped,
                    this,
                    addtl
                );
                #endregion log it

                // save it in a safe 'recoverable' place
                // see Scripts\Engines\RecycleBin\RecycleBin.cs
                DropOverflow(dropped, text);

                // messaging, anti-spam
                if (OverflowMessageGiven.Recall("overflow") == null)
                {
                    OverflowMessageGiven.Remember("overflow", 4);
                    // tell the owner what happened
                    if (this.RootParent != null && this.RootParent is Mobile m && m.Player)
                        m.SendMessage("Your container overflow items have been archived.");

                    // tell staff what's going on, but don't spam
                    Point3D location = this.GetWorldLocation();
                    LogHelper.Broadcaster.BroadcastMessage(
                        AccessLevel.GameMaster,
                        52,
                        string.Format("Possible container overflow exploit at: {0}", location),
                        spamDelay: TimeSpan.FromMinutes(5));
                }

                // one of the problems with these lower level functions is that they don't properly account for items added and removed.
                //  therefore we climb back up the container stack (to the parent mobile) and update totals.
                UpdateAllTotals();

                return;
            }
            #endregion DropOverflow

            AddItem(ref dropped);

            Rectangle2D bounds = dropped.GetGraphicBounds();
            Rectangle2D ourBounds = this.Bounds;

            int x, y;

            if (bounds.Width >= ourBounds.Width)
                x = (ourBounds.Width - bounds.Width) / 2;
            else
                x = Utility.Random(ourBounds.Width - bounds.Width);

            if (bounds.Height >= ourBounds.Height)
                y = (ourBounds.Height - bounds.Height) / 2;
            else
                y = Utility.Random(ourBounds.Height - bounds.Height);

            x += ourBounds.X;
            x -= bounds.X;

            y += ourBounds.Y;
            y -= bounds.Y;

            dropped.Location = new Point3D(x, y, 0);
        }

        public override void OnDoubleClickSecureTrade(Mobile from)
        {
            if (from.InRange(GetWorldLocation(), 2))
            {
                DisplayTo(from);

                SecureTradeContainer cont = GetSecureTradeCont();

                if (cont != null)
                {
                    SecureTrade trade = cont.Trade;

                    if (trade != null && trade.From.Mobile == from)
                        DisplayTo(trade.To.Mobile);
                    else if (trade != null && trade.To.Mobile == from)
                        DisplayTo(trade.From.Mobile);
                }
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public virtual bool DisplaysContent
        {
            get
            {
                if (Deco)
                    return false;

                return true;
            }
        }

        public virtual bool CheckContentDisplay(Mobile from)
        {
            if (!DisplaysContent)
                return false;

            object root = this.RootParent;

            if (root == null || root is Item || root == from || from.AccessLevel > AccessLevel.Player)
                return true;

            return false;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (Deco)
                LabelTo(from, "(decorative)");

            if (CheckContentDisplay(from))
            {

                if (from.AccessLevel >= AccessLevel.GameMaster)
                    LabelTo(from, $"({ItemCountText}, {TotalWeight} stones, {TotalGold}) gold");
                else
                    LabelTo(from, $"({ItemCountText}, {TotalWeight} stones)");

            }
        }

        private List<Mobile> m_Openers;

        public List<Mobile> Openers
        {
            get { return m_Openers; }
            set { m_Openers = value; }
        }

        public virtual bool IsPublicContainer { get { return false; } }

        public override void OnDelete()
        {
            base.OnDelete();

            m_Openers = null;
        }

        public virtual void DisplayTo(Mobile to)
        {
            if (!IsPublicContainer)
            {
                bool contains = false;

                if (m_Openers != null)
                {
                    Point3D worldLoc = GetWorldLocation();
                    Map map = this.Map;

                    for (int i = 0; i < m_Openers.Count; ++i)
                    {
                        Mobile mob = (Mobile)m_Openers[i];

                        if (mob == to)
                        {
                            contains = true;
                        }
                        else
                        {
                            int range = GetUpdateRange(mob);

                            if (mob.Map != map || !mob.InRange(worldLoc, range))
                                m_Openers.RemoveAt(i--);
                        }
                    }
                }

                if (!contains)
                {
                    if (m_Openers == null)
                        m_Openers = new List<Mobile>(4);

                    m_Openers.Add(to);
                }
                else if (m_Openers != null && m_Openers.Count == 0)
                {
                    m_Openers = null;
                }
            }

            to.Send(new ContainerDisplay(this));

            if (to.NetState != null && to.NetState.ContainerGridLines/*IsPost6017*/)
            {
                to.Send(new ContainerContent6017(to, this));
            }
            else
            {
                to.Send(new ContainerContent(to, this));
            }

            if (ObjectPropertyList.Enabled)
            {
                List<Item> items = this.Items;

                for (int i = 0; i < items.Count; ++i)
                    to.Send(((Item)items[i]).OPLPacket);
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (DisplaysContent)//CheckContentDisplay( from ) )
            {
                string items = ItemCountText;
                if (items == string.Format("{0} items", TotalItems))
                    list.Add(1050044, "{0}\t{1}", TotalItems, TotalWeight);  // ~1_COUNT~ items, ~2_WEIGHT~ stones
                else
                    list.Add(string.Format("{0}, {1} stones", items, TotalWeight));    // the slots in use differ from the count
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player || from.InRange(this.GetWorldLocation(), 2))
                DisplayTo(from);
            else
                from.SendLocalizedMessage(500446); // That is too far away.
        }
    }

    public class ContainerData
    {
        static ContainerData()
        {
            m_Table = new Dictionary<int, ContainerData>();

            string path = Path.Combine(Core.DataDirectory, "containers.cfg");

            if (!File.Exists(path))
            {
                m_Default = new ContainerData(0x3C, new Rectangle2D(44, 65, 142, 94), 0x48);
                return;
            }

            using (StreamReader reader = new StreamReader(path))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.Length == 0 || line.StartsWith("#"))
                        continue;

                    try
                    {
                        string[] split = line.Split('\t');

                        if (split.Length >= 3)
                        {
                            int gumpID = Utility.ToInt32(split[0]);

                            string[] aRect = split[1].Split(' ');
                            if (aRect.Length < 4)
                                continue;

                            int x = Utility.ToInt32(aRect[0]);
                            int y = Utility.ToInt32(aRect[1]);
                            int width = Utility.ToInt32(aRect[2]);
                            int height = Utility.ToInt32(aRect[3]);

                            Rectangle2D bounds = new Rectangle2D(x, y, width, height);

                            int dropSound = Utility.ToInt32(split[2]);

                            ContainerData data = new ContainerData(gumpID, bounds, dropSound);

                            if (m_Default == null)
                                m_Default = data;

                            if (split.Length >= 4)
                            {
                                string[] aIDs = split[3].Split(',');

                                for (int i = 0; i < aIDs.Length; i++)
                                {
                                    int id = Utility.ToInt32(aIDs[i]);

                                    if (m_Table.ContainsKey(id))
                                    {
                                        Console.WriteLine(@"Warning: double ItemID entry in Data\containers.cfg");
                                    }
                                    else
                                    {
                                        m_Table[id] = data;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            if (m_Default == null)
                m_Default = new ContainerData(0x3C, new Rectangle2D(44, 65, 142, 94), 0x48);
        }

        private static ContainerData m_Default;
        private static Dictionary<int, ContainerData> m_Table;

        public static ContainerData Default
        {
            get { return m_Default; }
            set { m_Default = value; }
        }

        public static ContainerData GetData(int itemID)
        {
            ContainerData data = null;
            m_Table.TryGetValue(itemID, out data);

            if (data != null)
                return data;
            else
                return m_Default;
        }

        private int m_GumpID;
        private Rectangle2D m_Bounds;
        private int m_DropSound;

        public int GumpID { get { return m_GumpID; } }
        public Rectangle2D Bounds { get { return m_Bounds; } }
        public int DropSound { get { return m_DropSound; } }

        public ContainerData(int gumpID, Rectangle2D bounds, int dropSound)
        {
            m_GumpID = gumpID;
            m_Bounds = bounds;
            m_DropSound = dropSound;
        }
    }
}