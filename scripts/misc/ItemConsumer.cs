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

/* Misc/ItemConsumer.cs
 * CHANGELOG:
 *	1/8/21, Yoar
 *	    Initial version.
 */

using System;
using System.Collections.Generic;

namespace Server.Items
{
    public static class ItemConsumer
    {
        public delegate int GroupItem(Item a, Item b);

        public static int ConsumeGrouped(Container cont, Type[][] types, int[] amounts, bool consume = true, bool recurse = true, Predicate<Item> predicate = null, GroupItem grouper = null, OnItemConsumed callback = null)
        {
            Item[][] groups = new Item[types.Length][];

            for (int i = 0; i < types.Length; i++)
            {
                Item[] group;

                if (Group(cont, types[i], amounts[i], out group, recurse, predicate, grouper))
                    groups[i] = group;
                else
                    return i;
            }

            if (consume)
            {
                for (int i = 0; i < types.Length; i++)
                    Consume(groups[i], amounts[i], callback);
            }

            return -1;
        }

        private static readonly Item[] m_EmptyGroup = new Item[0];
        private static readonly List<Item> m_Group = new List<Item>();

        private enum GroupingType : byte { First, Best };

        public static bool HasTotal(Container cont, Type[] types, int needs, bool recurse = true, Predicate<Item> predicate = null, GroupItem grouper = null)
        {
            Item[] group;
            return Group(cont, types, needs, out group, GroupingType.First, recurse, predicate, grouper) >= needs;
        }

        public static bool Group(Container cont, Type[] types, int needs, out Item[] group, bool recurse = true, Predicate<Item> predicate = null, GroupItem grouper = null)
        {
            return Group(cont, types, needs, out group, GroupingType.First, recurse, predicate, grouper) >= needs;
        }

        public static int GetBestGroupAmount(Container cont, Type[] types, bool recurse = true, Predicate<Item> predicate = null, GroupItem grouper = null)
        {
            Item[] group;
            return Group(cont, types, 0, out group, GroupingType.Best, recurse, predicate, grouper);
        }

        public static int GetBestGroup(Container cont, Type[] types, out Item[] group, bool recurse = true, Predicate<Item> predicate = null, GroupItem grouper = null)
        {
            return Group(cont, types, 0, out group, GroupingType.Best, recurse, predicate, grouper);
        }

        private static int Group(Container cont, Type[] types, int needs, out Item[] group, GroupingType type, bool recurse = true, Predicate<Item> predicate = null, GroupItem grouper = null)
        {
            if (predicate == null)
                predicate = DefaultPredicate;

            if (grouper == null)
                grouper = DefaultGrouper;

            Item[] found = cont.FindItemsByType(types, recurse);

            int[] totals = new int[found.Length]; // how much quantity does each item hold?

            for (int i = 0; i < found.Length; i++)
            {
                Item item = found[i];

                if (predicate(item))
                {
                    int quantity;

                    if (item is IHasQuantity)
                        quantity = ((IHasQuantity)item).Quantity;
                    else
                        quantity = item.Amount;

                    totals[i] = quantity;
                }
            }

            group = m_EmptyGroup;
            int total = 0;

            for (int i = 0; i < found.Length; i++)
            {
                if (totals[i] <= 0)
                    continue;

                Item a = found[i];

                m_Group.Clear();
                m_Group.Add(a); // make a new group with "a"

                int totalCur = totals[i];

                for (int j = i + 1; j < found.Length; j++)
                {
                    if (totals[j] <= 0)
                        continue;

                    Item b = found[j];

                    if (grouper(a, b) != 0)
                        continue;

                    m_Group.Add(b); // add "b" to the group

                    totalCur += totals[j];

                    totals[j] = -1; // we have already grouped this item, no need to make a new group with it

                    if (type == GroupingType.First && totalCur >= needs)
                        break;
                }

                if (totalCur >= total)
                {
                    group = m_Group.ToArray();
                    total = totalCur;

                    if (type == GroupingType.First && total >= needs)
                        break;
                }
            }

            return total;
        }

        public static void Consume(Item[] group, int needs, OnItemConsumed callback = null)
        {
            for (int i = 0; i < group.Length && needs > 0; i++)
            {
                Item item = group[i];

                int toConsume;

                if (item is IHasQuantity)
                {
                    IHasQuantity hq = (IHasQuantity)item;

                    toConsume = Math.Min(hq.Quantity, needs);

                    hq.Quantity -= toConsume;
                }
                else
                {
                    toConsume = Math.Min(item.Amount, needs);

                    item.Consume(toConsume);
                }

                needs -= toConsume;

                if (callback != null)
                    callback(item, toConsume);
            }
        }

        public static bool DefaultPredicate(Item item)
        {
            return true;
        }

        public static int DefaultGrouper(Item a, Item b)
        {
            return b.Hue.CompareTo(a.Hue);
        }
    }
}