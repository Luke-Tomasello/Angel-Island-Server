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

using Server.Items;

namespace Server.ContextMenus
{
    public class EatEntry : ContextMenuEntry
    {
        private Mobile m_From;
        private Food m_Food;

        public EatEntry(Mobile from, Food food)
            : base(6135, 1)
        {
            m_From = from;
            m_Food = food;
        }

        public override void OnClick()
        {
            if (m_Food.Deleted || !m_Food.Movable || !m_From.CheckAlive())
                return;

            m_Food.Eat(m_From);
        }
    }
}