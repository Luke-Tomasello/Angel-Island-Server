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

using Server.Engines;
using Server.Gumps;
using System.Collections.Generic;

namespace Server.Items
{
    public class EventLootpack : Backpack
    {
        private Dictionary<Item, EventLootItem> m_Table;
        private double m_LootChance;
        private int m_LootRolls;
        private bool m_UseWeights;

        public Dictionary<Item, EventLootItem> Table
        {
            get { return m_Table; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double LootChance
        {
            get { return m_LootChance; }
            set { m_LootChance = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LootRolls
        {
            get { return m_LootRolls; }
            set { m_LootRolls = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UseWeights
        {
            get { return m_UseWeights; }
            set { m_UseWeights = value; }
        }

        [Constructable]
        public EventLootpack()
            : base()
        {
            Movable = false;
            Visible = false;

            m_Table = new Dictionary<Item, EventLootItem>();
            m_LootChance = 1.0;
            m_LootRolls = 1;
        }

        public override void OnItemAdded(Item item)
        {
            base.OnItemAdded(item);

            m_Table[item] = new EventLootItem(item);
        }

        public override void OnItemRemoved(Item item)
        {
            base.OnItemRemoved(item);

            // keep the loot item to support dragging the item within the container
#if false
            m_Table.Remove(item);
#endif
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (!base.OnDragDrop(from, dropped))
                return false;

            if (from.AccessLevel >= AccessLevel.GameMaster)
                DisplayLootItem(from, dropped);

            return true;
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (!base.OnDragDropInto(from, item, p))
                return false;

            if (from.AccessLevel >= AccessLevel.GameMaster)
                DisplayLootItem(from, item);

            return true;
        }

        public override void OnSingleClickContained(Mobile from, Item item)
        {
            base.OnSingleClickContained(from, item);

            // we need some way to edit loot items
            // since we don't have context menus on Siege, let's use single-click
            if (from.AccessLevel >= AccessLevel.GameMaster)
                DisplayLootItem(from, item);
        }

        private void DisplayLootItem(Mobile from, Item item)
        {
            EventLootItem lootItem;

            if (m_Table.TryGetValue(item, out lootItem))
                from.SendGump(new PropertiesGump(from, lootItem));
        }

        public Item[] Generate()
        {
            if (m_Table.Count == 0)
                return new Item[0];

            Defrag();

            List<Item> list = new List<Item>();

            for (int i = 0; i < m_LootRolls; i++)
            {
                if (Utility.RandomDouble() < m_LootChance)
                {
                    if (m_UseWeights)
                        GenerateWeighted(list);
                    else
                        GenerateRandom(list);
                }
            }

            return list.ToArray();
        }

        private void GenerateRandom(List<Item> list)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Item item = Items[i];

                EventLootItem lootItem;

                if (m_Table.TryGetValue(item, out lootItem) && Utility.RandomDouble() < lootItem.Chance)
                    AppendLoot(list, item);
            }
        }

        private void GenerateWeighted(List<Item> list)
        {
            int total = 0;

            for (int i = 0; i < Items.Count; i++)
            {
                Item item = Items[i];

                EventLootItem lootItem;

                if (m_Table.TryGetValue(item, out lootItem))
                    total += lootItem.Weight;
            }

            if (total <= 0)
                return;

            int rnd = Utility.Random(total);

            for (int i = 0; i < Items.Count; i++)
            {
                Item item = Items[i];

                EventLootItem lootItem;

                if (m_Table.TryGetValue(item, out lootItem))
                {
                    if (rnd < lootItem.Weight)
                    {
                        AppendLoot(list, item);
                        break;
                    }
                    else
                    {
                        rnd -= lootItem.Weight;
                    }
                }
            }
        }

        private void AppendLoot(List<Item> list, Item item)
        {
            if (item is EventLootpack)
            {
                list.AddRange(((EventLootpack)item).Generate());
            }
            else
            {
                Item loot = RareFactory.DupeItem(item);

                if (loot != null)
                    list.Add(loot);
            }
        }

        public EventLootpack(Serial serial)
            : base(serial)
        {
            m_Table = new Dictionary<Item, EventLootItem>();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((bool)m_UseWeights);

            writer.Write((double)m_LootChance);
            writer.Write((int)m_LootRolls);

            writer.Write((int)m_Table.Count);

            foreach (KeyValuePair<Item, EventLootItem> kvp in m_Table)
            {
                writer.Write((Item)kvp.Key);
                kvp.Value.Serialize(writer);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_UseWeights = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_LootChance = reader.ReadDouble();
                        m_LootRolls = reader.ReadInt();

                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            Item item = reader.ReadItem();

                            EventLootItem lootItem = new EventLootItem(item);

                            lootItem.Deserialize(reader);

                            if (item != null)
                                m_Table[item] = lootItem;
                        }

                        break;
                    }
            }

            if (version < 1)
                m_UseWeights = true;

            ValidationQueue<EventLootpack>.Enqueue(this);
        }

        public void Validate()
        {
            Defrag();
        }

        private void Defrag()
        {
            List<Item> toRemove = new List<Item>();

            foreach (Item item in m_Table.Keys)
            {
                if (!Items.Contains(item))
                    toRemove.Add(item);
            }

            foreach (Item item in toRemove)
                m_Table.Remove(item);
        }
    }

    [PropertyObject]
    public class EventLootItem
    {
        private Item m_Owner;
        private double m_Chance;
        private int m_Weight;

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Owner
        {
            get { return m_Owner; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Chance
        {
            get { return m_Chance; }
            set { m_Chance = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Weight
        {
            get { return m_Weight; }
            set { m_Weight = value; }
        }

        public EventLootItem(Item owner)
        {
            m_Owner = owner;
            m_Chance = 1.0;
            m_Weight = 1;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)1); // version

            writer.Write((double)m_Chance);

            writer.Write((int)m_Weight);
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Chance = reader.ReadDouble();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Weight = reader.ReadInt();

                        break;
                    }
            }
        }

        public override string ToString()
        {
            return "...";
        }
    }
}