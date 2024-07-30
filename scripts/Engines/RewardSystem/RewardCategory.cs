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

/* Scripts/Engines/Reward System/RewardCategory.cs
 * Created 5/23/04 by mith
 * ChangeLog
 */

using System.Collections;

namespace Server.Engines.RewardSystem
{
    public class RewardCategory
    {
        private int m_Name;
        private string m_NameString;
        private string m_XmlString;
        private ArrayList m_Entries;

        public int Name { get { return m_Name; } }
        public string NameString { get { return m_NameString; } }
        public string XmlString { get { return m_XmlString; } }
        public ArrayList Entries { get { return m_Entries; } }

        public RewardCategory(int name, string xmlString)
        {
            m_Name = name;
            m_XmlString = xmlString;
            m_Entries = new ArrayList();
        }

        public RewardCategory(string name, string xmlString)
        {
            m_NameString = name;
            m_XmlString = xmlString;
            m_Entries = new ArrayList();
        }
    }
}