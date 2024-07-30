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

/* Engines/Leaderboard/LeaderboardTab.cs
 * ChangeLog:
 *	8/12/21, Craig
 *		Initial version
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Scripts.Engines.Leaderboard
{
    public class LeaderboardTab
    {
        private string m_Heading;
        private List<string> m_Lines = new List<string>();

        internal string Heading
        {
            get
            {
                return m_Heading;
            }
        }

        internal LeaderboardTab(string heading)
        {
            m_Heading = heading;
        }

        internal string GetPage(int pageNumber)
        {
            if (m_Lines.Count == 0)
                return "";

            if (pageNumber < 1 || pageNumber > GetPageCount())
                return "";

            StringBuilder sb = new StringBuilder();
            int index = (pageNumber - 1) * LeaderboardTracker.LinesPerPage;
            int stopIndex = index + LeaderboardTracker.LinesPerPage;
            for (; index < m_Lines.Count && index < stopIndex; index++)
            {
                sb.Append(m_Lines[index]);
                sb.Append("<br>");
            }
            return sb.ToString();
        }

        internal int GetPageCount()
        {
            if (m_Lines.Count == 0)
                return 0;

            return ((m_Lines.Count - 1) / LeaderboardTracker.LinesPerPage) + 1;
        }

        public void AddLine(String s)
        {
            m_Lines.Add(s);
        }

        public void Clear() { m_Lines.Clear(); }
    }
}