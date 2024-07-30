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

namespace Server.Factions
{
    public class StrongholdDefinition
    {
        private Rectangle2D[] m_Area;
        private Point3D m_JoinStone;
        private Point3D m_FactionStone;
        private Point3D[] m_Monoliths;

        public Rectangle2D[] Area { get { return m_Area; } }

        public Point3D JoinStone { get { return m_JoinStone; } }
        public Point3D FactionStone { get { return m_FactionStone; } }

        public Point3D[] Monoliths { get { return m_Monoliths; } }

        public StrongholdDefinition(Rectangle2D[] area, Point3D joinStone, Point3D factionStone, Point3D[] monoliths)
        {
            m_Area = area;
            m_JoinStone = joinStone;
            m_FactionStone = factionStone;
            m_Monoliths = monoliths;
        }
    }
}