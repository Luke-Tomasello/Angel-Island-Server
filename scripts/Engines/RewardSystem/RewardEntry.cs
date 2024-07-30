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

/* Scripts/Engines/Reward System/RewardEntry.cs
 * Created 5/23/04 by mith
 * ChangeLog
 */

using System;

namespace Server.Engines.RewardSystem
{
    public class RewardEntry
    {
        private RewardList m_List;
        private RewardCategory m_Category;
        private Type m_ItemType;
        private int m_Name;
        private string m_NameString;
        private object[] m_Args;

        public RewardList List { get { return m_List; } set { m_List = value; } }
        public RewardCategory Category { get { return m_Category; } }
        public Type ItemType { get { return m_ItemType; } }
        public int Name { get { return m_Name; } }
        public string NameString { get { return m_NameString; } }
        public object[] Args { get { return m_Args; } }

        public Item Construct()
        {
            try
            {
                Item item = Activator.CreateInstance(m_ItemType, m_Args) as Item;

                if (item is IRewardItem)
                    ((IRewardItem)item).IsRewardItem = true;

                return item;
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

            return null;
        }

        public RewardEntry(RewardCategory category, int name, Type itemType, params object[] args)
        {
            m_Category = category;
            m_ItemType = itemType;
            m_Name = name;
            m_Args = args;
            category.Entries.Add(this);
        }

        public RewardEntry(RewardCategory category, string name, Type itemType, params object[] args)
        {
            m_Category = category;
            m_ItemType = itemType;
            m_NameString = name;
            m_Args = args;
            category.Entries.Add(this);
        }
    }
}