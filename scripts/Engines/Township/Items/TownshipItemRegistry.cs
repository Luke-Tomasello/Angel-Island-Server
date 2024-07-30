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

/* Engines/Township/Items/TownshipItemRegistry.cs
 * CHANGELOG:
 *  9/13/2023, Adam (item.IsLockedDown)
 *      Set/clear item.IsLockedDown for township lockdowns/items.
 *      This allows items to be queried on a global level, essentially, if they are a house or township lockdown
 * 1/13/21, Yoar
 *	    Initial version.
 */

using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Township
{
    [PropertyObject]
    public class TownshipItemRegistry
    {
        private TownshipStone m_TStone;
        private Dictionary<Item, TownshipItemContext> m_Table;

        [CommandProperty(AccessLevel.GameMaster)]
        public TownshipStone TStone
        {
            get { return m_TStone; }
            set { }
        }

        public Dictionary<Item, TownshipItemContext> Table
        {
            get { return m_Table; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ItemCount
        {
            get { return m_Table.Count; }
        }

        public TownshipItemRegistry(TownshipStone tstone)
        {
            m_TStone = tstone;
            m_Table = new Dictionary<Item, TownshipItemContext>();
        }

        public void Attach(Item item, TownshipItemContext c)
        {
            m_Table[item] = c;
        }

        public TownshipItemContext Lookup(Item item, bool create = false)
        {
            TownshipItemContext context;

            if (m_Table.TryGetValue(item, out context))
                return context;

            if (create)
                return m_Table[item] = new TownshipItemContext(item);

            return null;
        }

        #region FindItem

        public Item FindItem(Type type)
        {
            foreach (KeyValuePair<Item, TownshipItemContext> kvp in m_Table)
            {
                Item item = kvp.Key;

                if (type.IsAssignableFrom(item.GetType()))
                    return item;
            }

            return null;
        }

        public T FindItem<T>() where T : Item
        {
            foreach (KeyValuePair<Item, TownshipItemContext> kvp in m_Table)
            {
                Item item = kvp.Key;

                if (item is T)
                    return (T)item;
            }

            return null;
        }

        public T FindItem<T>(Predicate<T> predicate) where T : Item
        {
            foreach (KeyValuePair<Item, TownshipItemContext> kvp in m_Table)
            {
                Item item = kvp.Key;

                if (item is T && predicate((T)item))
                    return (T)item;
            }

            return null;
        }

        #endregion

        public static int DefragAll()
        {
            int total = 0;

            foreach (TownshipStone ts in TownshipStone.AllTownshipStones)
                total += ts.ItemRegistry.Defrag();

            return total;
        }

        public int Defrag()
        {
            List<Item> toRemove = new List<Item>();

            foreach (KeyValuePair<Item, TownshipItemContext> kvp in m_Table)
            {
                Item item = kvp.Key;

                if (item.Deleted)
                    toRemove.Add(item);
            }

            foreach (Item item in toRemove)
                m_Table.Remove(item);

            return toRemove.Count;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version

            writer.Write((int)m_Table.Count);

            foreach (KeyValuePair<Item, TownshipItemContext> kvp in m_Table)
            {
                writer.Write((Item)kvp.Key);
                kvp.Value.Serialize(writer);
            }
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            int count = reader.ReadInt();

            for (int i = 0; i < count; i++)
            {
                Item item = reader.ReadItem();
                TownshipItemContext context = new TownshipItemContext(reader, item, version);

                if (item != null)
                {
                    m_Table[item] = context;
                    item.IsLockedDown = true;
                }
            }
        }

        public override string ToString()
        {
            return "...";
        }
    }
}