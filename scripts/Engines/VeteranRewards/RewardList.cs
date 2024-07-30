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

namespace Server.Engines.VeteranRewards
{
    public class RewardList
    {
        private TimeSpan m_Age;
        private RewardEntry[] m_Entries;

        public TimeSpan Age { get { return m_Age; } }
        public RewardEntry[] Entries { get { return m_Entries; } }

        public RewardList(TimeSpan interval, int index, RewardEntry[] entries)
        {
            m_Age = TimeSpan.FromDays(interval.TotalDays * index);
            m_Entries = entries;

            for (int i = 0; i < entries.Length; ++i)
                entries[i].List = this;
        }
    }
}