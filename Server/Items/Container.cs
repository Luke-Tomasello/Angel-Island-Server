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

/* Server/Items/Container.cs
 * CHANGELOG:
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

using Server.Diagnostics;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Server.Items
{
    public delegate void OnItemConsumed(Item item, int amount);
    public delegate int CheckItemGroup(Item a, Item b);

    public delegate void ContainerSnoopHandler(Container cont, Mobile from);


    public class Container : Item
    {
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
#if false
        // overriding Parent causes issues when duping since Custom Attributes don't work on derived properties.
        //  Since Freeze dry is gone, no need to override these
        public override bool IsSecure
        {
            get
            {
                return base.IsSecure;
            }
            set
            {
                /*if (value && (m_Freeze == null || !m_Freeze.Running))
                {
                    m_Freeze = new FreezeTimer(this);
                    m_Freeze.Start();
                }
                else if (!value && m_Freeze != null)
                {
                    m_Freeze.Stop();
                    m_Freeze = null;
                }*/

                base.IsSecure = value;
            }
        }

        public override bool IsLockedDown
        {
            get
            {
                return base.IsLockedDown;
            }
            set
            {
                /*if (value && (m_Freeze == null || !m_Freeze.Running))
                {
                    m_Freeze = new FreezeTimer(this);
                    m_Freeze.Start();
                }
                else if (!value && m_Freeze != null)
                {
                    m_Freeze.Stop();
                    m_Freeze = null;
                }*/

                base.IsLockedDown = value;
            }
        }

        public override object Parent
        {
            get
            {
                return base.Parent;
            }
            set
            {
                base.Parent = value;

                /*if (CanFreezeDry)
                {
                    if (m_Freeze != null && m_Freeze.Running)
                        return;

                    m_Freeze = new FreezeTimer(this);
                    m_Freeze.Start();
                }*/
            }
        }
#endif
        public virtual void UpdateContainerData()
        {
            this.ContainerData = ContainerData.GetData(this.ItemID);
        }

        public virtual Rectangle2D Bounds { get { return ContainerData.Bounds; } }
        public virtual int DefaultGumpID { get { return ContainerData.GumpID; } }
        public virtual int DefaultDropSound { get { return ContainerData.DropSound; } }
        public virtual int DefaultMaxItems { get { return m_GlobalMaxItems; } }
        public virtual int DefaultMaxWeight { get { return m_GlobalMaxWeight; } }

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
            if (m.AccessLevel < AccessLevel.GameMaster)
            {
                if (!CanStore(m))
                {
                    if (message)
                        SendCantStoreMessage(m, item);

                    return false;
                }

                int maxItems = this.MaxItems;

                if (checkItems && maxItems != 0 && (this.TotalItems + plusItems + item.TotalItems + (item.IsVirtualItem ? 0 : 1)) > maxItems)
                {
                    if (message)
                        SendFullItemsMessage(m, item);

                    return false;
                }
                else
                {
                    int maxWeight = this.MaxWeight;

                    if (maxWeight != 0 && (this.TotalWeight + plusWeight + item.TotalWeight + item.PileWeight) > maxWeight)
                    {
                        if (message)
                            SendFullWeightMessage(m, item);

                        return false;
                    }
                }
            }

            object parent = this.Parent;

            while (parent != null)
            {
                if (parent is Container)
                    return ((Container)parent).CheckHold(m, item, message, checkItems, plusItems, plusWeight);
                else if (parent is Item)
                    parent = ((Item)parent).Parent;
                else
                    break;
            }

            return true;
        }

        public bool CheckHold(List<Item> itemList)
        {
            int maxItems = this.MaxItems;
            int maxWeight = this.MaxWeight;
            int totalItems = this.TotalItems;
            int totalWeight = this.TotalWeight;
            foreach (Item item in itemList)
            {
                if (maxItems != 0)
                {
                    if ((totalItems + item.TotalItems + (item.IsVirtualItem ? 0 : 1)) > maxItems)
                        return false;
                    else
                    {   // update virtual total items
                        totalItems += item.TotalItems + (item.IsVirtualItem ? 0 : 1);
                    }
                }

                if (maxWeight != 0)
                {
                    if ((totalWeight + item.TotalWeight + item.PileWeight) > maxWeight)
                        return false;
                    else
                    {   // update virtual total weight
                        totalWeight += item.TotalWeight + item.PileWeight;
                    }
                }
            }

            object parent = this.Parent;

            while (parent != null)
            {
                if (parent is Container cont && cont.CheckHold(itemList) == false)
                    return false;
                else if (parent is Item)
                    parent = ((Item)parent).Parent;
                else
                    break;
            }

            return true;
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

        private class GroupComparer : IComparer
        {
            private CheckItemGroup m_Grouper;

            public GroupComparer(CheckItemGroup grouper)
            {
                m_Grouper = grouper;
            }

            public int Compare(object x, object y)
            {
                Item a = (Item)x;
                Item b = (Item)y;

                return m_Grouper(a, b);
            }
        }

        public bool ConsumeTotalGrouped(Type type, int amount, bool recurse, OnItemConsumed callback, CheckItemGroup grouper)
        {
            if (grouper == null)
                throw new ArgumentNullException();

            Item[] typedItems = FindItemsByType(type, recurse);

            ArrayList groups = new ArrayList();
            int idx = 0;

            while (idx < typedItems.Length)
            {
                Item a = typedItems[idx++];
                ArrayList group = new ArrayList();

                group.Add(a);

                while (idx < typedItems.Length)
                {
                    Item b = typedItems[idx];
                    int v = grouper(a, b);

                    if (v == 0)
                        group.Add(b);
                    else
                        break;

                    ++idx;
                }

                groups.Add(group);
            }

            Item[][] items = new Item[groups.Count][];
            int[] totals = new int[groups.Count];

            bool hasEnough = false;

            for (int i = 0; i < groups.Count; ++i)
            {
                items[i] = (Item[])(((ArrayList)groups[i]).ToArray(typeof(Item)));

                for (int j = 0; j < items[i].Length; ++j)
                    totals[i] += items[i][j].Amount;

                if (totals[i] >= amount)
                    hasEnough = true;
            }

            if (!hasEnough)
                return false;

            for (int i = 0; i < items.Length; ++i)
            {
                if (totals[i] >= amount)
                {
                    int need = amount;

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

                    break;
                }
            }

            return true;
        }

        public int ConsumeTotalGrouped(Type[] types, int[] amounts, bool recurse, OnItemConsumed callback, CheckItemGroup grouper)
        {
            if (types.Length != amounts.Length)
                throw new ArgumentException();
            else if (grouper == null)
                throw new ArgumentNullException();

            Item[][][] items = new Item[types.Length][][];
            int[][] totals = new int[types.Length][];

            for (int i = 0; i < types.Length; ++i)
            {
                Item[] typedItems = FindItemsByType(types[i], recurse);

                ArrayList groups = new ArrayList();
                int idx = 0;

                while (idx < typedItems.Length)
                {
                    Item a = typedItems[idx++];
                    ArrayList group = new ArrayList();

                    group.Add(a);

                    while (idx < typedItems.Length)
                    {
                        Item b = typedItems[idx];
                        int v = grouper(a, b);

                        if (v == 0)
                            group.Add(b);
                        else
                            break;

                        ++idx;
                    }

                    groups.Add(group);
                }

                items[i] = new Item[groups.Count][];
                totals[i] = new int[groups.Count];

                bool hasEnough = false;

                for (int j = 0; j < groups.Count; ++j)
                {
                    items[i][j] = (Item[])(((ArrayList)groups[j]).ToArray(typeof(Item)));

                    for (int k = 0; k < items[i][j].Length; ++k)
                        totals[i][j] += items[i][j][k].Amount;

                    if (totals[i][j] >= amounts[i])
                        hasEnough = true;
                }

                if (!hasEnough)
                    return i;
            }

            for (int i = 0; i < items.Length; ++i)
            {
                for (int j = 0; j < items[i].Length; ++j)
                {
                    if (totals[i][j] >= amounts[i])
                    {
                        int need = amounts[i];

                        for (int k = 0; k < items[i][j].Length; ++k)
                        {
                            Item item = items[i][j][k];

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

                        break;
                    }
                }
            }

            return -1;
        }

        public int GetBestGroupAmount(Type type, bool recurse, CheckItemGroup grouper)
        {
            if (grouper == null)
                throw new ArgumentNullException();

            int best = 0;

            Item[] typedItems = FindItemsByType(type, recurse);

            ArrayList groups = new ArrayList();
            int idx = 0;

            while (idx < typedItems.Length)
            {
                Item a = typedItems[idx++];
                ArrayList group = new ArrayList();

                group.Add(a);

                while (idx < typedItems.Length)
                {
                    Item b = typedItems[idx];
                    int v = grouper(a, b);

                    if (v == 0)
                        group.Add(b);
                    else
                        break;

                    ++idx;
                }

                groups.Add(group);
            }

            for (int i = 0; i < groups.Count; ++i)
            {
                Item[] items = (Item[])(((ArrayList)groups[i]).ToArray(typeof(Item)));
                int total = 0;

                for (int j = 0; j < items.Length; ++j)
                    total += items[j].Amount;

                if (total >= best)
                    best = total;
            }

            return best;
        }

        public int GetBestGroupAmount(Type[] types, bool recurse, CheckItemGroup grouper)
        {
            if (grouper == null)
                throw new ArgumentNullException();

            int best = 0;

            Item[] typedItems = FindItemsByType(types, recurse);

            ArrayList groups = new ArrayList();
            int idx = 0;

            while (idx < typedItems.Length)
            {
                Item a = typedItems[idx++];
                ArrayList group = new ArrayList();

                group.Add(a);

                while (idx < typedItems.Length)
                {
                    Item b = typedItems[idx];
                    int v = grouper(a, b);

                    if (v == 0)
                        group.Add(b);
                    else
                        break;

                    ++idx;
                }

                groups.Add(group);
            }

            for (int j = 0; j < groups.Count; ++j)
            {
                Item[] items = (Item[])(((ArrayList)groups[j]).ToArray(typeof(Item)));
                int total = 0;

                for (int k = 0; k < items.Length; ++k)
                    total += items[k].Amount;

                if (total >= best)
                    best = total;
            }

            return best;
        }

        public int GetBestGroupAmount(Type[][] types, bool recurse, CheckItemGroup grouper)
        {
            if (grouper == null)
                throw new ArgumentNullException();

            int best = 0;

            for (int i = 0; i < types.Length; ++i)
            {
                Item[] typedItems = FindItemsByType(types[i], recurse);

                ArrayList groups = new ArrayList();
                int idx = 0;

                while (idx < typedItems.Length)
                {
                    Item a = typedItems[idx++];
                    ArrayList group = new ArrayList();

                    group.Add(a);

                    while (idx < typedItems.Length)
                    {
                        Item b = typedItems[idx];
                        int v = grouper(a, b);

                        if (v == 0)
                            group.Add(b);
                        else
                            break;

                        ++idx;
                    }

                    groups.Add(group);
                }

                for (int j = 0; j < groups.Count; ++j)
                {
                    Item[] items = (Item[])(((ArrayList)groups[j]).ToArray(typeof(Item)));
                    int total = 0;

                    for (int k = 0; k < items.Length; ++k)
                        total += items[k].Amount;

                    if (total >= best)
                        best = total;
                }
            }

            return best;
        }

        public int ConsumeTotalGrouped(Type[][] types, int[] amounts, bool recurse, OnItemConsumed callback, CheckItemGroup grouper)
        {
            if (types.Length != amounts.Length)
                throw new ArgumentException();
            else if (grouper == null)
                throw new ArgumentNullException();

            Item[][][] items = new Item[types.Length][][];
            int[][] totals = new int[types.Length][];

            for (int i = 0; i < types.Length; ++i)
            {
                Item[] typedItems = FindItemsByType(types[i], recurse);

                ArrayList groups = new ArrayList();
                int idx = 0;

                while (idx < typedItems.Length)
                {
                    Item a = typedItems[idx++];
                    ArrayList group = new ArrayList();

                    group.Add(a);

                    while (idx < typedItems.Length)
                    {
                        Item b = typedItems[idx];
                        int v = grouper(a, b);

                        if (v == 0)
                            group.Add(b);
                        else
                            break;

                        ++idx;
                    }

                    groups.Add(group);
                }

                items[i] = new Item[groups.Count][];
                totals[i] = new int[groups.Count];

                bool hasEnough = false;

                for (int j = 0; j < groups.Count; ++j)
                {
                    items[i][j] = (Item[])(((ArrayList)groups[j]).ToArray(typeof(Item)));

                    for (int k = 0; k < items[i][j].Length; ++k)
                        totals[i][j] += items[i][j][k].Amount;

                    if (totals[i][j] >= amounts[i])
                        hasEnough = true;
                }

                if (!hasEnough)
                    return i;
            }

            for (int i = 0; i < items.Length; ++i)
            {
                for (int j = 0; j < items[i].Length; ++j)
                {
                    if (totals[i][j] >= amounts[i])
                    {
                        int need = amounts[i];

                        for (int k = 0; k < items[i][j].Length; ++k)
                        {
                            Item item = items[i][j][k];

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

                        break;
                    }
                }
            }

            return -1;
        }

        public int ConsumeTotal(Type[][] types, int[] amounts)
        {
            return ConsumeTotal(types, amounts, true, null);
        }

        public int ConsumeTotal(Type[][] types, int[] amounts, bool recurse)
        {
            return ConsumeTotal(types, amounts, recurse, null);
        }

        public int ConsumeTotal(Type[][] types, int[] amounts, bool recurse, OnItemConsumed callback)
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

            return -1;
        }

        public int ConsumeTotal(Type[] types, int[] amounts)
        {
            return ConsumeTotal(types, amounts, true, null);
        }

        public int ConsumeTotal(Type[] types, int[] amounts, bool recurse)
        {
            return ConsumeTotal(types, amounts, recurse, null);
        }

        public int ConsumeTotal(Type[] types, int[] amounts, bool recurse, OnItemConsumed callback)
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

            return -1;
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

        public bool ConsumeTotal(Type type, int amount, bool recurse, OnItemConsumed callback)
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

            return false;
        }

        public int ConsumeUpTo(Type type, int amount)
        {
            return ConsumeUpTo(type, amount, true);
        }

        public int ConsumeUpTo(Type type, int amount, bool recurse)
        {
            int consumed = 0;

            Queue toDelete = new Queue();

            RecurseConsumeUpTo(this, type, amount, recurse, ref consumed, toDelete);

            while (toDelete.Count > 0)
                ((Item)toDelete.Dequeue()).Delete();

            return consumed;
        }

        private static void RecurseConsumeUpTo(Item current, Type type, int amount, bool recurse, ref int consumed, Queue toDelete)
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

#if old
		private static ArrayList m_FindItemsList = new ArrayList();

		public Item[] FindItemsByType(Type type)
		{
			return FindItemsByType(type, true);
		}

		public Item[] FindItemsByType(Type type, bool recurse)
		{
			if (m_FindItemsList.Count > 0)
				m_FindItemsList.Clear();

			RecurseFindItemsByType(this, type, recurse, m_FindItemsList);

			return (Item[])m_FindItemsList.ToArray(typeof(Item));
		}

		private static void RecurseFindItemsByType(Item current, Type type, bool recurse, ArrayList list)
		{
			if (current != null && current.Items.Count > 0)
			{
				ArrayList items = current.Items;

				for (int i = 0; i < items.Count; ++i)
				{
					Item item = (Item)items[i];

					if (type.IsAssignableFrom(item.GetType()))// item.GetType().IsAssignableFrom( type ) )
						list.Add(item);

					if (recurse && item is Container)
						RecurseFindItemsByType(item, type, recurse, list);
				}
			}
		}

		public Item[] FindItemsByType(Type[] types)
		{
			return FindItemsByType(types, true);
		}

		public Item[] FindItemsByType(Type[] types, bool recurse)
		{
			if (m_FindItemsList.Count > 0)
				m_FindItemsList.Clear();

			RecurseFindItemsByType(this, types, recurse, m_FindItemsList);

			return (Item[])m_FindItemsList.ToArray(typeof(Item));
		}

		private static void RecurseFindItemsByType(Item current, Type[] types, bool recurse, ArrayList list)
		{
			if (current != null && current.Items.Count > 0)
			{
				ArrayList items = current.Items;

				for (int i = 0; i < items.Count; ++i)
				{
					Item item = (Item)items[i];

					if (InTypeList(item, types))
						list.Add(item);

					if (recurse && item is Container)
						RecurseFindItemsByType(item, types, recurse, list);
				}
			}
		}

		public Item FindItemByType(Type type)
		{
			return FindItemByType(type, true);
		}

		public Item FindItemByType(Type type, bool recurse)
		{
			return RecurseFindItemByType(this, type, recurse);
		}

		private static Item RecurseFindItemByType(Item current, Type type, bool recurse)
		{
			if (current != null && current.Items.Count > 0)
			{
				ArrayList list = current.Items;

				for (int i = 0; i < list.Count; ++i)
				{
					Item item = (Item)list[i];

					if (type.IsAssignableFrom(item.GetType()))
					{
						return item;
					}
					else if (recurse && item is Container)
					{
						Item check = RecurseFindItemByType(item, type, recurse);

						if (check != null)
							return check;
					}
				}
			}

			return null;
		}

		public Item FindItemByType(Type[] types)
		{
			return FindItemByType(types, true);
		}

		public Item FindItemByType(Type[] types, bool recurse)
		{
			return RecurseFindItemByType(this, types, recurse);
		}

		private static Item RecurseFindItemByType(Item current, Type[] types, bool recurse)
		{
			if (current != null && current.Items.Count > 0)
			{
				ArrayList list = current.Items;

				for (int i = 0; i < list.Count; ++i)
				{
					Item item = (Item)list[i];

					if (InTypeList(item, types))
					{
						return item;
					}
					else if (recurse && item is Container)
					{
						Item check = RecurseFindItemByType(item, types, recurse);

						if (check != null)
							return check;
					}
				}
			}

			return null;
		}
#endif
        private static List<Item> m_FindItemsList = new List<Item>();

        #region Non-Generic FindItem[s] by Type
        public Item[] FindItemsByType(Type type)
        {
            return FindItemsByType(type, true);
        }

        public Item[] FindItemsByType(Type type, bool recurse)
        {
            if (m_FindItemsList.Count > 0)
                m_FindItemsList.Clear();

            RecurseFindItemsByType(this, type, recurse, m_FindItemsList);

            return m_FindItemsList.ToArray();
        }

        private static void RecurseFindItemsByType(Item current, Type type, bool recurse, List<Item> list)
        {
            if (current != null && current.Items.Count > 0)
            {
                List<Item> items = current.Items;

                for (int i = 0; i < items.Count; ++i)
                {
                    Item item = items[i];

                    if (type.IsAssignableFrom(item.GetType()))// item.GetType().IsAssignableFrom( type ) )
                        list.Add(item);

                    if (recurse && item is Container)
                        RecurseFindItemsByType(item, type, recurse, list);
                }
            }
        }

        public Item[] FindItemsByType(Type[] types)
        {
            return FindItemsByType(types, true);
        }

        public Item[] FindItemsByType(Type[] types, bool recurse)
        {
            if (m_FindItemsList.Count > 0)
                m_FindItemsList.Clear();

            RecurseFindItemsByType(this, types, recurse, m_FindItemsList);

            return m_FindItemsList.ToArray();
        }

        private static void RecurseFindItemsByType(Item current, Type[] types, bool recurse, List<Item> list)
        {
            if (current != null && current.Items.Count > 0)
            {
                List<Item> items = current.Items;

                for (int i = 0; i < items.Count; ++i)
                {
                    Item item = items[i];

                    if (InTypeList(item, types))
                        list.Add(item);

                    if (recurse && item is Container)
                        RecurseFindItemsByType(item, types, recurse, list);
                }
            }
        }

        public Item FindItemByType(Type type)
        {
            return FindItemByType(type, true);
        }

        public Item FindItemByType(Type type, bool recurse)
        {
            return RecurseFindItemByType(this, type, recurse);
        }

        private static Item RecurseFindItemByType(Item current, Type type, bool recurse)
        {
            if (type != null && current != null && current.Items.Count > 0)
            {
                List<Item> list = current.Items;

                for (int i = 0; i < list.Count; ++i)
                {
                    Item item = list[i];

                    if (type.IsAssignableFrom(item.GetType()))
                    {
                        return item;
                    }
                    else if (recurse && item is Container)
                    {
                        Item check = RecurseFindItemByType(item, type, recurse);

                        if (check != null)
                            return check;
                    }
                }
            }

            return null;
        }

        public Item FindItemByType(Type[] types)
        {
            return FindItemByType(types, true);
        }

        public Item FindItemByType(Type[] types, bool recurse)
        {
            return RecurseFindItemByType(this, types, recurse);
        }

        private static Item RecurseFindItemByType(Item current, Type[] types, bool recurse)
        {
            if (current != null && current.Items.Count > 0)
            {
                List<Item> list = current.Items;

                for (int i = 0; i < list.Count; ++i)
                {
                    Item item = list[i];

                    if (InTypeList(item, types))
                    {
                        return item;
                    }
                    else if (recurse && item is Container)
                    {
                        Item check = RecurseFindItemByType(item, types, recurse);

                        if (check != null)
                            return check;
                    }
                }
            }

            return null;
        }

        #endregion

        #region Generic FindItem[s] by Type
        public List<T> FindItemsByType<T>() where T : Item
        {
            return FindItemsByType<T>(true, null);
        }

        public List<T> FindItemsByType<T>(bool recurse) where T : Item
        {
            return FindItemsByType<T>(recurse, null);
        }

        public List<T> FindItemsByType<T>(Predicate<T> predicate) where T : Item
        {
            return FindItemsByType<T>(true, predicate);
        }

        public List<T> FindItemsByType<T>(bool recurse, Predicate<T> predicate) where T : Item
        {
            if (m_FindItemsList.Count > 0)
                m_FindItemsList.Clear();

            List<T> list = new List<T>();

            RecurseFindItemsByType<T>(this, recurse, list, predicate);

            return list;
        }

        private static void RecurseFindItemsByType<T>(Item current, bool recurse, List<T> list, Predicate<T> predicate) where T : Item
        {
            if (current != null && current.Items.Count > 0)
            {
                List<Item> items = current.Items;

                for (int i = 0; i < items.Count; ++i)
                {
                    Item item = items[i];

                    if (typeof(T).IsAssignableFrom(item.GetType()))
                    {
                        T typedItem = (T)item;

                        if (predicate == null || predicate(typedItem))
                            list.Add(typedItem);
                    }

                    if (recurse && item is Container)
                        RecurseFindItemsByType<T>(item, recurse, list, predicate);
                }
            }
        }

        public T FindItemByType<T>() where T : Item
        {
            return FindItemByType<T>(true);
        }


        public T FindItemByType<T>(Predicate<T> predicate) where T : Item
        {
            return FindItemByType<T>(true, predicate);
        }

        public T FindItemByType<T>(bool recurse) where T : Item
        {
            return FindItemByType<T>(recurse, null);
        }

        public T FindItemByType<T>(bool recurse, Predicate<T> predicate) where T : Item
        {
            return RecurseFindItemByType<T>(this, recurse, predicate);
        }

        private static T RecurseFindItemByType<T>(Item current, bool recurse, Predicate<T> predicate) where T : Item
        {
            if (current != null && current.Items.Count > 0)
            {
                List<Item> list = current.Items;

                for (int i = 0; i < list.Count; ++i)
                {
                    Item item = list[i];

                    if (typeof(T).IsAssignableFrom(item.GetType()))
                    {
                        T typedItem = (T)item;

                        if (predicate == null || predicate(typedItem))
                            return typedItem;
                    }
                    else if (recurse && item is Container)
                    {
                        T check = RecurseFindItemByType<T>(item, recurse, predicate);

                        if (check != null)
                            return check;
                    }
                }
            }

            return null;
        }
        #endregion

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
        {   // Adam: no longer forced for all containers. See Deserialize
            //m_Freeze = new FreezeTimer(this);
            //m_Freeze.Start();
        }

        public virtual bool OnStackAttempt(Mobile from, Item stack, Item dropped)
        {
            if (!CheckHold(from, dropped, true, false))
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

                from.SendSound(GetDroppedSound(dropped), GetWorldLocation());

                return true;
            }
            else
            {
                return false;
            }
        }
        /*public bool CanStackWith(Mobile from, Item dropped)
        {
            ArrayList list = this.Items;
            for (int i = 0; i < list.Count; ++i)
            {
                Item item = (Item)list[i];

                if (!(item is Container) && item.CanStackWith(from, dropped))
                    return true;
            }

            return false;
        }*/
        public virtual bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            if (!CheckHold(from, dropped, sendFullMessage, false)) // do one check ignoring item count, so we can check for stacked items
                return false;

            List<Item> list = this.Items;

            for (int i = 0; i < list.Count; ++i)
            {
                Item item = (Item)list[i];

                if (!(item is Container) && item.StackWith(from, dropped, false))
                    return true;
            }

            if (!CheckHold(from, dropped, sendFullMessage)) // if there's nothing to stack with, now we check item count
                return false;

            DropItem(dropped);

            return true;
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
        public virtual bool PlayerOwned()
        {
            return false;
        }

        public virtual void DropOverflow(Item dropped, string logdata)
        {   // the derived class BaseContainer, will call our recycling logic to archive these 
            //  for 10 days. After that time, they are deleted.
            // BaseContainer now calls Scripts\Engines\RecycleBin\RecycleBin.cs
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
        public virtual void DropItem(Item dropped)
        {
            if (dropped == null)
                return;

            #region DropOverflow
            // 7/20/21, Adam: Certain functions call this directly bypassing the usual overload checks (weight/items)
            //  GMs can do this with [dupeinbag, and I believe certain exploits can accomplish this as well.
            //  We no longer will allow this. Each container now has an expiring RecycleBin. This container stores items that
            //  would otherwise overload a container which leads to numerous problems, and in extreme cases: client disconnects, lag, etc.
            //  Our RecycleBin stores these items on the internal map, and in the case of a legitimate player oversight, we can recover these items.
            //  in the case of a GM goof-up, or an exploit overload exploit, the RecycleBin object will self-destruct after 10 days (all logged.)
            bool rule1 = this.Items.Count >= this.MaxItems;                                         // overloading items
            bool rule2 = this.MaxWeight > 0 && this.TotalWeight >= this.MaxWeight;                  // overloading weight
            bool rule3 = (this.Movable == false && !PlayerOwned()) || this.Map == Map.Internal;     // event containers like ransom chests, treasure chests
            bool rule4 = SpecialLoot(dropped);                                                      // make sure players keep their special loot
            if ((rule1 || rule2) && !rule3 && !rule4)
            {
                // log it
                string addtl = "";
                if (rule1 == true) addtl += "(items)";
                if (rule2 == true) addtl += "(weight)";
                string text = string.Format(string.Format("{1} Trying to drop: {0} into container: {2}", dropped, addtl, this.Serial));

                // save it in a safe 'recoverable' place
                // see Scripts\Engines\RecycleBin\RecycleBin.cs
                DropOverflow(dropped, text);

                // tell the owner what happened
                if (this.RootParent != null && this.RootParent is Mobile)
                    (this.RootParent as Mobile).SendMessage("Your container overflow items have been archived.");

                // tell staff what's going on
                Point3D location = this.GetWorldLocation();
                LogHelper.BroadcastMessage(AccessLevel.GameMaster, 52, String.Format("Possible container overflow exploit at: {0}", location));

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
                LabelTo(from, "({0} items, {1} stones)", TotalItems, TotalWeight);
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
                list.Add(1050044, "{0}\t{1}", TotalItems, TotalWeight);
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