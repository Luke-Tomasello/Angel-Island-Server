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

/* Scripts\Engines\Spawner\ChestItemspawner.cs
 * CHANGELOG
 *  7/15/2024, Adam
 *      Complete rewrite:
 *      1. Obsolete ChestLootPackSpawner. ChestItemspawner now supports loot packs
 *      2. Allow the specification of up to 6 target containers
 *      3. Select: Allow a random 'select'ion of n of those 6 target containers
 *      4. RandomItem:
 *          a. When RandomItem is true, Count represents the total number of items to spawn into each container (selected randomly.)
 *          b. When RandomItem is false, Count represents the number of items of a particular type to spawn into each container
 *      5. Loot Pack support: Loot packs are not special, they are simply added to the list of available items to spawn. Equal weighted with the 'list items'
 *          a. The function of loot packs is to allow GMs to construct custom items for distribution from the spawner
 *      6. Rare items: You spawn rare items with a combination of Min/Max delay + ChanceToSpawn
 *	3/1/11, Adam
 *		Add a ChanceToSpawn. This powerful fearure allows us to randomly spawn over time.
 *		Example: chanceToSpawn=0.1 and a spawn time of every hour will result in roughly one item spawned every 10 hours. Perfect!
 *	5/13/10, adam
 *		Change LogType.Mobile to LogType.Item when logging lootpacks (was causing blank lines in the output log)
 *	3/22/10, adam
 *		1) Restore the ability to spawn one random item from list. Keep the defalut that all items are spawned (needed for AI prison chests)
 *		2) Add the ability for spawn from a LootPack object
 *		3) add new ChestLootPackSpawner for spawning raw items usually constructed by staff (Differs from normal spawners that spawn TYPE items.)
 *  02/28/05, erlein
 *    Added logging of Count property change & deletion of ChestItemSpawner.
 *    Now logs all changes to these in /logs/spawnerchange.log
 *  4/23/04 Pulse
 * 	 Spawner no longer spawns a single random item each Spawn() attempt
 * 	   but instead spawns on of each item type listed in m_ItemsNames
 * 	 The limit of items in a chest is no longer m_Count items but is now
 *      m_Count of each item type listed in m_ItemsNames
 *  4/13/04 pixie
 *    Removed a couple unnecessary checks that were throwing warnings.
 *  4/11/04 pixie
 *    Initial Revision.
 *  4/06/04 Created by Pixie;
 */

using Server.Mobiles;
using Server.Township;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Server.Commands.FDFile.ItemDatabase;

namespace Server.Items
{
    [NoSort]
    public class ChestItemSpawner : Item
    {
        private int m_Count;                    //how many items to spawn
        private TimeSpan m_MinDelay;            //min delay to respawn
        private TimeSpan m_MaxDelay;            //max delay to respawn
        private ArrayList m_ItemsName;          //list of item names
        private ArrayList m_Items;              //list of items spawned
        private DateTime m_End;                 //time to next respawn
        private InternalTimer m_Timer;          //internaltimer
        private bool m_RandomItem;              //spawn all items in the list or only one?
        private double m_DropChance = 1.0;   //chance of spawning an item. Default to always for backwards compat
        //private double m_LootPackChance = 1.0;  //chance of spawning an item from the lootpack.
        private ushort m_Select = 0;            // select randomly this many containers to spawn into
        public ArrayList ItemsName
        {
            get
            {
                return m_ItemsName;
            }
            set
            {
                m_ItemsName = value;
                // If no itemname, no spawning 
                if (m_ItemsName.Count < 1)
                    Stop();

                InvalidateProperties();
            }
        }

        #region Loot Pack
        private Container m_LootPack;
        [CommandProperty(AccessLevel.GameMaster)]
        public Container LootPack
        {
            get { return m_LootPack; }
            set
            {   // delete the old one
                if (m_LootPack != null && m_LootPack.Deleted == false && m_LootPack != value)
                {
                    m_LootPack.SpawnerTempRefCount--;
                    if (CanDeleteTemplate(m_LootPack))
                        m_LootPack.Delete();
                }

                // remove from current container
                if (value != null && value.Parent != null && value.Parent is Container)
                    (value.Parent as Container).RemoveItem(value);

                // assign
                m_LootPack = value;

                // move to int item storage
                if (m_LootPack != null && m_LootPack.Deleted == false)
                {
                    m_LootPack.MoveToIntStorage();
                    m_LootPack.SpawnerTempRefCount++;
                }
            }
        }
        private bool CanDeleteTemplate(IEntity o)
        {
            if (o is Mobile m && m.SpawnerTempRefCount == 0) { return true; }
            else if (o is Item i && i.SpawnerTempRefCount == 0) { return true; }
            return false;
        }
        //[CommandProperty(AccessLevel.GameMaster)]
        //public double LootPackChance
        //{
        //    get { return m_LootPackChance; }
        //    set
        //    {
        //        m_LootPackChance = value;
        //        InvalidateProperties();
        //    }
        //}
        #endregion Loot Pack

        [CommandProperty(AccessLevel.GameMaster)]
        public int Count
        {
            get
            {
                return m_Count;
            }
            set
            {
                int oldCount = m_Count;

                m_Count = value;

                if (oldCount != value)
                    Respawn();

                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Running
        {
            get { return base.IsRunning; }
            set
            {
                base.IsRunning = value;
                if (base.IsRunning)
                    Start();
                else
                    Stop();

                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool RandomItem
        {
            get { return m_RandomItem; }
            set
            {
                bool respawn = m_RandomItem != value;
                m_RandomItem = value;
                if (respawn == true)
                    Respawn();
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan MinDelay { get { return m_MinDelay; } set { m_MinDelay = value; InvalidateProperties(); } }
        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan MaxDelay { get { return m_MaxDelay; } set { m_MaxDelay = value; InvalidateProperties(); } }
        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NextSpawn
        {
            get
            {
                if (Running)
                    return m_End - DateTime.UtcNow;
                else
                    return TimeSpan.FromSeconds(0);
            }
            set
            {
                Start();
                DoTimer(value);
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public double DropChance
        {
            get { return m_DropChance; }
            set
            {
                m_DropChance = value;
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public ushort Select
        {
            get { return m_Select; }
            set
            {
                ushort oldSelect = m_Select;

                m_Select = value;

                if (oldSelect != value)
                    Respawn();

                InvalidateProperties();
            }
        }
        #region Target Containers 
        public List<Container> ContainerList = new List<Container>(6) { null, null, null, null, null, null};
        [CommandProperty(AccessLevel.GameMaster)]
        public Container Container1
        {
            get
            {
                return ContainerList[0];
            }
            set
            {
                ContainerList[0] = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Container Container2
        {
            get
            {
                return ContainerList[1];
            }
            set
            {
                ContainerList[1] = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Container Container3
        {
            get
            {
                return ContainerList[2];
            }
            set
            {
                ContainerList[2] = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Container Container4
        {
            get
            {
                return ContainerList[3];
            }
            set
            {
                ContainerList[3] = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Container Container5
        {
            get
            {
                return ContainerList[4];
            }
            set
            {
                ContainerList[4] = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Container Container6
        {
            get
            {
                return ContainerList[5];
            }
            set
            {
                ContainerList[5] = value;
            }
        }
        #endregion Target Containers 
        #region Construction
        [Constructable]
        public ChestItemSpawner(int amount, int minDelay, int maxDelay, string itemName)
            : base(0x1f13)
        {
            ArrayList itemsName = new ArrayList();
            itemsName.Add(itemName.ToLower());
            InitSpawn(amount, TimeSpan.FromMinutes(minDelay), TimeSpan.FromMinutes(maxDelay), itemsName);
        }
        [Constructable]
        public ChestItemSpawner(string itemName)
            : base(0x1f13)
        {
            ArrayList itemsName = new ArrayList();
            itemsName.Add(itemName.ToLower());
            InitSpawn(1, TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(60), itemsName);
        }
        [Constructable]
        public ChestItemSpawner()
            : base(0x1f13)
        {
            ArrayList itemsName = new ArrayList();
            InitSpawn(1, TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(60), itemsName);
        }
        public ChestItemSpawner(int amount, TimeSpan minDelay, TimeSpan maxDelay, ArrayList itemsName)
            : base(0x1f13)
        {
            InitSpawn(amount, minDelay, maxDelay, itemsName);
        }
        public override string DefaultName { get { return "Chest Item Spawner"; } }
        public void InitSpawn(int amount, TimeSpan minDelay, TimeSpan maxDelay, ArrayList itemsName)
        {
            Visible = false;
            Movable = true;
            Running = true;
            m_MinDelay = minDelay;
            m_MaxDelay = maxDelay;
            m_Count = amount;
            m_ItemsName = itemsName;
            m_Items = new ArrayList();              //create new list of items
            DoTimer(TimeSpan.FromSeconds(1));       //spawn in 1 sec 
        }
        public ChestItemSpawner(Serial serial)
            : base(serial)
        {
        }
        #endregion Construction
        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFull
        {   // broken... needs rework
            get { return m_Items.Count >= m_Count; }
        }
        public override void OnDoubleClick(Mobile from)
        {
            from.CloseGump(typeof(ChestItemSpawnerGump));
            ChestItemSpawnerGump g = new ChestItemSpawnerGump(this);
            from.SendGump(g);
        }
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Running)
            {
                list.Add(1060742); // active 

                list.Add(1060656, m_Count.ToString()); // amount to make: ~1_val~ 
                list.Add(1060660, "speed\t{0} to {1}", m_MinDelay, m_MaxDelay); // ~1_val~: ~2_val~ 
            }
            else
            {
                list.Add(1060743); // inactive 
            }
        }
        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (Running)
                LabelTo(from, "[Running]");
            else
                LabelTo(from, "[Off]");
        }
        public void Start()
        {
            if (!Running)
            {
                if (m_ItemsName.Count > 0)
                {
                    Running = true;
                    DoTimer();
                }
            }
        }
        public void Stop()
        {
            if (Running)
            {
                m_Timer.Stop();
                Running = false;
            }
        }
        private bool IsInContainer(Item item)
        {
            for (int ix = 0; ix < ContainerList.Count; ix++)
                if (ContainerList[ix] != null && ContainerList[ix].Items != null)
                    if (ContainerList[ix].Items.Contains(item))
                        return true;
            return false;
        }
        public void Defrag()
        {
            // first defrag our list of containers
            for (int ix = 0; ix < ContainerList.Count; ix++)
                if (ContainerList[ix] != null && ContainerList[ix].Deleted)
                    ContainerList[ix] = null;
            
            int deleted = 0;
            if (ValidContainers().Count > 0)
                for (int i = m_Items.Count - 1; i >= 0; --i)
                    if (!IsInContainer(m_Items[i] as Item))
                    {
                        m_Items.RemoveAt(i);
                        deleted++;
                    }

            // something was deleted
            if (deleted > 0)
                InvalidateProperties();
        }
        public void OnTick()
        {
            DoTimer();
            //if (m_Select != 0)
            //    RemoveItems();
            Spawn();
        }
        #region Spawn
        public void Respawn()
        {
            try
            {
                
                //for (int i = 0; i < m_Count; i++)
                    Spawn(total: true);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
        public void Spawn(bool total = false)
        {
            List<Container> validContainers = ValidContainers();

            if (total == true)
                RemoveItems();                      // remove all items
            else
                RemoveItems(validContainers);       // remove only items that are not in 'valid containers'

            // spawn one of each item type
            for (int i = 0; i < AvailableItems(); i++)
            {
                Spawn(i, validContainers);
            }
            //if (m_RandomItem == false)
            //{
            //    // spawn one of each item type
            //    for (int i = 0; i < AvailableItems(); i++)
            //    {
            //        Spawn(i, validContainers);
            //    }
            //}
            //else
            //{
            //    //if there are item to spawn in list 
            //    if (AvailableItems() > 0)
            //    {
            //        if (total)
            //            for (int i = 0; i < AvailableItems(); i++)
            //                Spawn(GetIndexes(validContainers.Count), validContainers); //spawn all by index 
            //        else
            //            Spawn(GetIndexes(validContainers.Count), validContainers); //spawn one by index 
            //    }
            //}
        }
        public void Spawn(string itemName)
        {
            for (int i = 0; i < m_ItemsName.Count; i++)
            {
                if ((string)m_ItemsName[i] == itemName)
                {
                    Spawn(i, ValidContainers());
                    break;
                }
            }
        }
        public void Spawn(int index, List<Container> validContainers)
        {
            try
            {
                if (AvailableItems() == 0 || index >= AvailableItems() || validContainers.Count == 0)
                    return;

                Defrag();

                InitItemCache();

                foreach (var container in validContainers)
                    try
                    {
                        // See if we have reached the limit of random items
                        //  ANY display cache items match
                        if (m_RandomItem && CountContainerObjects(container) >= m_Count)
                            continue;

                        Item dcItem = null;
                        if (m_RandomItem)
                            dcItem = ItemCache[Utility.Random(ItemCache.Count)].Key;
                        else
                            dcItem = ItemCache[index].Key;

                        // See if we have reached the limit for this type of item
                        if (CountContainerObjects(container, dcItem) >= m_Count)
                            continue;

                        if (m_DropChance >= Utility.RandomDouble())
                        {
                            Item item = Utility.Dupe(dcItem);
                            m_Items.Add(item);              //add it to the list 
                            InvalidateProperties();
                            container.DropItem(item);       //spawn it in the container 
                        }
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

            }
            finally
            {
                foreach (var record in ItemCache)
                    record.Key.Delete();
            }
        }
        #endregion Spawn
        private int DisplayID(Item item)
        {
            return Utility.GetStableHashCode(item.GetType().Name + "_" + item.ItemID.ToString() + "_" + item.Hue.ToString());
        }
        private int CountContainerObjects(Container cont, Item item)
        {
            int itemID = DisplayID(item);
            int count = 0;
            if (cont != null && cont.Items != null && cont.Items.Count > 0)
                foreach (Item contained_item in cont.Items)
                    if (DisplayID(contained_item) == itemID)
                        count++;
            return count;
        }
        private int CountContainerObjects(Container cont)
        {
            int count = 0;
            foreach (var record in ItemCache)
            {
                int itemID = DisplayID(record.Key);
                if (cont != null && cont.Items != null && cont.Items.Count > 0)
                    foreach (Item contained_item in cont.Items)
                        if (DisplayID(contained_item) == itemID)
                            count++;
            }
            return count;
        }
        private List <Container> ValidContainers()
        {
            List<Container> list = new List<Container>();
            foreach (var x in ContainerList)
                if (x != null)
                    list.Add(x);

            Utility.Shuffle(list);

            if (m_Select > 0)
                list = list.Take(m_Select).ToList();

            return list;
        }
        private List<KeyValuePair<Item, int>> ItemCache = new ();
        private void InitItemCache()
        {
            //if (m_RandomItem && m_Select != 0)
            //    RemoveItems();

            ItemCache.Clear();
            if (m_ItemsName != null && m_ItemsName.Count > 0)
            {
                foreach (object itemName in m_ItemsName)
                {
                    Type type = SpawnerType.GetType((string)itemName);
                    if (type != null)
                    {
                        object o = Activator.CreateInstance(type);
                        if (o is Item item)
                            ItemCache.Add(new KeyValuePair<Item, int>(item, DisplayID(item)));
                        else if (o is IEntity ent)
                            ent.Delete();
                    }
                }
            }
            
            if (m_LootPack != null && !m_LootPack.Deleted && m_LootPack.Items != null && m_LootPack.Items.Count > 0)
            {
                List<Item> list = new List<Item>();
                foreach (Item item in m_LootPack.Items)
                    list.Add((Item)item);

                foreach (Item item in list)
                    m_LootPack.RemoveItem(item);

                foreach (Item item in list)
                    ItemCache.Add(new KeyValuePair<Item, int>(Utility.Dupe(item), DisplayID(item)));

                foreach (Item item in list)
                    m_LootPack.AddItem(item);
            }

            if (m_RandomItem)
                Utility.Shuffle(ItemCache);
        }
        private int AvailableItems()
        {
            int total=0;
            if (m_ItemsName != null)
                total += m_ItemsName.Count;

            if (m_LootPack != null && m_LootPack.Items != null)
                total +=  m_LootPack.Items.Count;
            
            return total;
        }
        public void DoTimer()
        {
            if (!Running)
                return;

            int minSeconds = (int)m_MinDelay.TotalSeconds;
            int maxSeconds = (int)m_MaxDelay.TotalSeconds;

            TimeSpan delay = TimeSpan.FromSeconds(Utility.RandomMinMax(minSeconds, maxSeconds));
            DoTimer(delay);
        }
        public void DoTimer(TimeSpan delay)
        {
            if (!Running)
                return;

            m_End = DateTime.UtcNow + delay;

            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = new InternalTimer(this, delay);
            m_Timer.Start();
        }
        private class InternalTimer : Timer
        {
            private ChestItemSpawner m_Spawner;

            public InternalTimer(ChestItemSpawner spawner, TimeSpan delay)
                : base(delay)
            {
                Priority = TimerPriority.OneSecond;
                m_Spawner = spawner;
            }

            protected override void OnTick()
            {
                if (m_Spawner != null)
                    if (!m_Spawner.Deleted)
                        m_Spawner.OnTick();
            }
        }
        public int CountItems(string itemName)
        {

            Defrag();

            int count = 0;

            for (int i = 0; i < m_Items.Count; ++i)
                if (Insensitive.Equals(itemName, m_Items[i].GetType().Name))
                    ++count;

            return count;
        }

        #region Remove Items
        public void RemoveItems(string itemName)
        {
            Defrag();

            itemName = itemName.ToLower();

            for (int i = 0; i < m_Items.Count; ++i)
            {
                object o = m_Items[i];

                if (Insensitive.Equals(itemName, o.GetType().Name))
                {
                    if (o is Item)
                        ((Item)o).Delete();

                }
            }

            InvalidateProperties();
        }
        public void RemoveItems()
        {
            Defrag();

            for (int i = 0; i < m_Items.Count; ++i)
            {
                object o = m_Items[i];

                if (o is Item item)
                {
                    if (item.Parent is Container c)
                        c.RemoveItem(item);
                    item.Delete();
                }
            }

            InvalidateProperties();
        }
        public void RemoveItems(List<Container> validContainers)
        {
            List<Container> allContainers = new(ContainerList);
            
            var result = allContainers.Except(validContainers).ToList();

            Defrag();

            foreach (var container in result)
                if (container != null)
                    RemoveItems(container);

            InvalidateProperties();
        }
        public void RemoveItems(Container cont)
        {
            for (int i = 0; i < m_Items.Count; ++i)
            {
                object o = m_Items[i];

                if (o is Item item)
                {
                    if (item.Parent == cont)
                    {
                        cont.RemoveItem(item);
                        item.Delete();
                    }
                }
            }

            InvalidateProperties();
        }
        #endregion Remove Items

        public override void OnDelete()
        {
            base.OnDelete();
            try
            {
                RemoveItems();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            if (m_Timer != null)
                m_Timer.Stop();

            if (this.LootPack != null && this.LootPack.Deleted == false)
            {
                m_LootPack.SpawnerTempRefCount--;
                if (CanDeleteTemplate(m_LootPack))
                    this.LootPack.Delete();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)6); // version 

            // version 6 - added container list
            for (int ix = 0; ix < ContainerList.Count;ix++)
                // we write the nulls too. May want to optimize this
                writer.Write (ContainerList[ix]);
            writer.Write(m_LootPack);
            writer.Write(m_Select);
            //writer.Write(m_LootPackChance);

            // version 4 - removed in version 5
            //writer.Write(m_LastChange);

            // version 2
            writer.Write(m_DropChance);

            // version 1
            writer.Write(m_RandomItem);

            // version 0
            //writer.Write(m_Container); // obsolete in version 6
            writer.Write(m_MinDelay);
            writer.Write(m_MaxDelay);
            writer.Write(m_Count);
            // obsolete in version 3
            //writer.Write(Running);

            if (Running)
                writer.Write(m_End - DateTime.UtcNow);

            writer.Write(m_ItemsName.Count);

            for (int i = 0; i < m_ItemsName.Count; ++i)
                writer.Write((string)m_ItemsName[i]);

            writer.Write(m_Items.Count);

            for (int i = 0; i < m_Items.Count; ++i)
            {
                object o = m_Items[i];

                if (o is Item)
                    writer.Write((Item)o);
                else
                    writer.Write(Serial.MinusOne);
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 6:
                    {   // read back the list - may contain nulls
                        for (int ix = 0; ix < ContainerList.Count; ix++)
                            ContainerList[ix] = reader.ReadItem() as Container;
                        m_LootPack = reader.ReadItem() as Container;
                        m_Select = reader.ReadUShort();
                        //m_LootPackChance = reader.ReadDouble();
                        goto case 5;
                    }
                case 5:
                    {
                        goto case 3; // skip version 4
                    }
                case 4:
                    {
                        //m_LastChange = reader.ReadString();
                        base.LastChange = reader.ReadString();
                        goto case 3;
                    }
                case 3:
                    {
                        goto case 2;
                    }
                case 2:
                    {
                        m_DropChance = reader.ReadDouble();
                        goto case 1;
                    }

                case 1:
                    {
                        m_RandomItem = reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 6)
                        {
                            Container c /*m_Container*/ = reader.ReadItem() as Container;
                            ContainerList[0] = c;
                        }
                        m_MinDelay = reader.ReadTimeSpan();
                        m_MaxDelay = reader.ReadTimeSpan();
                        m_Count = reader.ReadInt();
                        if (version < 3)
                            Running = reader.ReadBool();

                        if (Running)
                        {
                            TimeSpan delay = reader.ReadTimeSpan();
                            DoTimer(delay);
                        }

                        int size = reader.ReadInt();

                        m_ItemsName = new ArrayList(size);

                        for (int i = 0; i < size; ++i)
                        {
                            string typeName = reader.ReadString();

                            if (version < 6)
                            {   // convert the kooky serial string to an actual lootpack
                                int result = 0;
                                if (SpawnerType.GetType(typeName) == null)
                                    if (Utility.StringToInt(typeName, ref result))
                                        if (World.FindItem(result) is Container)
                                        {
                                            System.Diagnostics.Debug.Assert(m_LootPack == null);
                                            m_LootPack = World.FindItem(result) as Container;
                                            continue;
                                        }
                            }
                            // make sure it's a valid type
                            if (SpawnerType.GetType(typeName) != null)
                                m_ItemsName.Add(typeName);
                            else
                                ;

                            //if (ChestItemSpawnerType.GetType(typeName) == null && ChestItemSpawnerType.GetLootPack(typeName) == null)
                            //{
                            //    ; // this is a bad loot spawner. It will be picked up elsewhere (OnLoad())
                            //      // This may be a bug in loot packs getting deleted. So far the codelooks good, but I'm tracking it.
                            //      //	In any case, we are logging this and can investegate/delete the spawner.
                            //}
                        }

                        int count = reader.ReadInt();

                        m_Items = new ArrayList(count);

                        for (int i = 0; i < count; ++i)
                        {
                            IEntity e = World.FindEntity(reader.ReadInt());

                            if (e != null)
                                m_Items.Add(e);
                        }
                    }
                    break;
            }

            if (version < 6)
            {
                if (Name == "ChestItemSpawner")
                    Name = null;
            }
        }

    }

    [Obsolete("Chest LootPack Spawner is Obsolete. Please use Chest Item Spawner instead.")]
    public class ChestLootPackSpawner : ChestItemSpawner
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public override bool RandomItem
        {
            get
            {
                return base.RandomItem;
            }
        }

        public override string DefaultName { get { return "Chest LootPack Spawner"; } }
        [Constructable]
        public ChestLootPackSpawner()
            : base()
        {
            // lootpack spawners don't understand how to spawn the spawn-list understood by normal spawners
            base.RandomItem = true;

            Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(ObsoleteTick), new object[] {  });
        }
        private void ObsoleteTick(object state)
        {
            SendSystemMessage("Chest LootPack Spawner is Obsolete. Please use Chest Item Spawner instead.");
        }
        public ChestLootPackSpawner(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version 
            
            //writer.Write(m_LootPack); - eliminated in version 2
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {   // removed local lootpack, uses base class now
                        break;
                    }
                case 1:
                    {
                        LootPack = reader.ReadItem() as Container;
                        goto case 0;
                    }

                case 0:
                    {

                    }
                    break;
            }

            if (version < 2)
            {
                if (Name == "ChestItemSpawner")
                    Name = null;
            }
        }
    }
}