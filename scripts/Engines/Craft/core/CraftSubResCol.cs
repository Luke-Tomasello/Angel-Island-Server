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
    public class CraftSubResCol : System.Collections.CollectionBase
    {
        private Type m_Type;
        private TextDefinition m_Name;
        private bool m_Init;

        public bool Init
        {
            get { return m_Init; }
            set { m_Init = value; }
        }

        public Type ResType
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        public TextDefinition Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public CraftSubResCol()
        {
            m_Init = false;
        }

        public void Add(CraftSubRes craftSubRes)
        {
            List.Add(craftSubRes);
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

        public CraftSubRes GetAt(int index)
        {
            return (CraftSubRes)List[index];
        }

        public CraftSubRes SearchFor(Type type)
        {
            for (int i = 0; i < List.Count; i++)
            {
                CraftSubRes craftSubRes = (CraftSubRes)List[i];
                if (craftSubRes.ItemType == type)
                {
                    return craftSubRes;
                }
            }
            return null;
        }
        public int IndexOf(Type type)
        {
            for (int i = 0; i < List.Count; i++)
            {
                CraftSubRes craftSubRes = (CraftSubRes)List[i];
                if (craftSubRes.ItemType == type)
                {
                    return i;
                }
            }
            return -1;
        }


    }
}