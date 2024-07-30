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

/* Misc/ItemStacker.cs
 * CHANGELOG:
 *  9/08/21, Yoar
 *		First commit. Automated item stacking.
 */

using System;
using System.Collections.Generic;

namespace Server.Misc
{
    public static class ItemStacker
    {
        private static readonly Type[] m_Single = new Type[1];
        private static readonly Item[] m_Buffer = new Item[20]; // arbitrary buffer length
        private static readonly List<StackEntry> m_StackList = new List<StackEntry>();

        private static Item[] GetStackArray(int length)
        {
            if (length > m_Buffer.Length)
                return new Item[length]; // just in case

            // clean up the buffer
            for (int i = 0; i < length; i++)
                m_Buffer[i] = null;

            return m_Buffer;
        }

        public static void StackAt(Point3D loc, Map map, Type type)
        {
            m_Single[0] = type; // no need to make a new array

            StackAt(loc, map, m_Single);
        }

        public static void StackAt(Point3D loc, Map map, Type[] types)
        {
            if (map == null || map == Map.Internal)
                return;

            Item[] stacks = GetStackArray(types.Length);

            m_StackList.Clear();

            foreach (Item item in map.GetItemsInRange(loc, 0))
            {
                if (item.Z == loc.Z)
                {
                    Type type = item.GetType();

                    for (int i = 0; i < types.Length; i++)
                    {
                        if (types[i] == type)
                        {
                            if (stacks[i] == null) // we don't have a stack yet
                                stacks[i] = item;
                            else
                                m_StackList.Add(new StackEntry(stacks[i], item)); // we cannot stack the item here, that would modify the looping collection

                            break;
                        }
                    }
                }
            }

            foreach (StackEntry e in m_StackList)
                e.Stack.StackWith(null, e.ToStack, false);
        }

        private struct StackEntry
        {
            private Item m_Stack;
            private Item m_ToStack;

            public Item Stack { get { return m_Stack; } }
            public Item ToStack { get { return m_ToStack; } }

            public StackEntry(Item stack, Item toStack)
            {
                m_Stack = stack;
                m_ToStack = toStack;
            }
        }
    }
}