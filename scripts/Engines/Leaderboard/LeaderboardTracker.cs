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

/* Engines/Leaderboard/LeaderboardTracker.cs
 * ChangeLog:
 *	8/12/21, Craig
 *		Initial version
 */

using System.Collections.Generic;

namespace Scripts.Engines.Leaderboard
{
    public class LeaderboardTracker
    {
        // We need two dictionaries so we can retrieve the page by its name and its index number
        private static Dictionary<string, LeaderboardTab> m_Tabs = new Dictionary<string, LeaderboardTab>();
        private static Dictionary<int, string> m_TabIndex = new Dictionary<int, string>();

        internal static int LinesPerPage { get { return 10; } }

        internal static int TabCount
        {
            get
            {
                return m_Tabs.Count;
            }
        }

        public static LeaderboardTab AddTab(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if (m_Tabs.ContainsKey(name))
            {
                return m_Tabs[name];
            }
            else
            {
                LeaderboardTab page = new LeaderboardTab(name);
                m_TabIndex.Add(m_Tabs.Count, name);
                m_Tabs.Add(name, page);
                return page;
            }
        }

        public static void DeleteTab(string name)
        {
            int toDelete = int.MinValue;
            foreach (KeyValuePair<int, string> kvp in m_TabIndex)
            {
                if (kvp.Value.Equals(name))
                {
                    toDelete = kvp.Key;
                    break;
                }
            }

            if (toDelete >= 0)
            {
                m_TabIndex.Remove(toDelete);
                m_Tabs.Remove(name);
                Dictionary<int, string> temp = new Dictionary<int, string>();
                foreach (KeyValuePair<int, string> kvp in m_TabIndex)
                {
                    if (kvp.Key > toDelete)
                        temp.Add(kvp.Key - 1, kvp.Value);
                    else
                        temp.Add(kvp.Key, kvp.Value);
                }
                m_TabIndex = temp;
            }
        }

        public static LeaderboardTab GetTab(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if (m_Tabs.ContainsKey(name))
                return m_Tabs[name];

            return null;
        }

        internal static LeaderboardTab GetTab(int tabNumber)
        {
            if (m_TabIndex.ContainsKey(tabNumber))
                return GetTab(m_TabIndex[tabNumber]);

            return null;
        }
    }
}