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

/* Scripts\Engines\Alignment\AlignmentDefinition.cs
 * Changelog:
 *  5/2/23, Yoar
 *      Added PrimaryHue, SecondaryHue
 *  4/30/23, Yoar
 *      Added Available property which denotes whether the alignment is available via the guild gump.
 *  4/14/23, Yoar
 *      Initial version.
 */

namespace Server.Engines.Alignment
{
    public class AlignmentDefinition
    {
        private string m_Name;
        private string m_Title;
        private string[] m_RankTitles;
        private int m_PrimaryHue;
        private int m_SecondaryHue;
        private bool m_GlobalAllegiance;
        private bool m_Available;

        public string Name { get { return m_Name; } }
        public string Title { get { return m_Title; } }
        public string[] RankTitles { get { return m_RankTitles; } }
        public int PrimaryHue { get { return m_PrimaryHue; } }
        public int SecondaryHue { get { return m_SecondaryHue; } }
        public bool GlobalAllegiance { get { return m_GlobalAllegiance; } }
        public bool Available { get { return m_Available; } }

        public AlignmentDefinition(string name, string title, string[] rankTitles, int primaryHue, int secondaryHue, bool globalAllegiance, bool available)
        {
            m_Name = name;
            m_Title = title;
            m_RankTitles = rankTitles;
            m_PrimaryHue = primaryHue;
            m_SecondaryHue = secondaryHue;
            m_GlobalAllegiance = globalAllegiance;
            m_Available = available;
        }
    }
}