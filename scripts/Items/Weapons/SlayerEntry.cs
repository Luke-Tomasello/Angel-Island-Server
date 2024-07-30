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

namespace Server.Items
{
    public class SlayerEntry
    {
        private SlayerGroup m_Group;
        private SlayerName m_Name;
        private Type[] m_Types;

        public SlayerGroup Group { get { return m_Group; } set { m_Group = value; } }
        public SlayerName Name { get { return m_Name; } }
        public Type[] Types { get { return m_Types; } }

        public SlayerEntry(SlayerName name, params Type[] types)
        {
            m_Name = name;
            m_Types = types;
        }

        public bool Slays(Mobile m)
        {
            Type t = m.GetType();

            for (int i = 0; i < m_Types.Length; ++i)
                if (m_Types[i] == t)
                    return true;

            return false;
        }
    }
}