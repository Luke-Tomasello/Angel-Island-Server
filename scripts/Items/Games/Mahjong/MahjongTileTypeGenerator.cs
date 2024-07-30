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

using System.Collections;

namespace Server.Engines.Mahjong
{
    public class MahjongTileTypeGenerator
    {
        private ArrayList m_LeftTileTypes;

        public ArrayList LeftTileTypes { get { return m_LeftTileTypes; } }

        public MahjongTileTypeGenerator(int count)
        {
            m_LeftTileTypes = new ArrayList(34 * count);

            for (int i = 1; i <= 34; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    m_LeftTileTypes.Add((MahjongTileType)i);
                }
            }
        }

        public MahjongTileType Next()
        {
            int random = Utility.Random(m_LeftTileTypes.Count);
            MahjongTileType next = (MahjongTileType)m_LeftTileTypes[random];
            m_LeftTileTypes.RemoveAt(random);

            return next;
        }
    }
}