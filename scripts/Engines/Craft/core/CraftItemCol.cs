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

using System;

namespace Server.Engines.Craft
{
    public class CraftItemCol : System.Collections.CollectionBase
    {
        public CraftItemCol()
        {
        }

        public int Add(CraftItem craftItem)
        {
            return List.Add(craftItem);
        }

        public void Remove(int index)
        {
            if (index > Count - 1 || index < 0)
            {
            }
            else
            {
                List.RemoveAt(index);
            }
        }

        public CraftItem GetAt(int index)
        {
            return (CraftItem)List[index];
        }

        public CraftItem SearchForSubclass(Type type)
        {
            for (int i = 0; i < List.Count; i++)
            {
                CraftItem craftItem = (CraftItem)List[i];

                if (craftItem.ItemType == type || type.IsSubclassOf(craftItem.ItemType))
                    return craftItem;
            }

            return null;
        }

        public CraftItem SearchFor(Type type)
        {
            for (int i = 0; i < List.Count; i++)
            {
                CraftItem craftItem = (CraftItem)List[i];
                if (craftItem.ItemType == type)
                {
                    return craftItem;
                }
            }
            return null;
        }
    }
}