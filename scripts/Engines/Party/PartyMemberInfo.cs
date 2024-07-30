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

namespace Server.Engines.PartySystem
{
    public class PartyMemberInfo
    {
        private Mobile m_Mobile;
        private bool m_CanLoot;

        public Mobile Mobile { get { return m_Mobile; } }
        public bool CanLoot { get { return m_CanLoot; } set { m_CanLoot = value; } }

        public PartyMemberInfo(Mobile m)
        {
            m_Mobile = m;
            m_CanLoot = true;
        }
    }
}