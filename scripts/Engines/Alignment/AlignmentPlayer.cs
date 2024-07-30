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

/* Scripts\Engines\Alignment\AlignmentPlayer.cs
 * Changelog:
 *  4/14/23, Yoar
 *      Initial version.
 */

using System;
using System.Collections.Generic;

namespace Server.Engines.Alignment
{
    [PropertyObject]
    public class AlignmentPlayer
    {
        private static readonly Dictionary<Mobile, AlignmentPlayer> m_Table = new Dictionary<Mobile, AlignmentPlayer>();

        public static Dictionary<Mobile, AlignmentPlayer> Table { get { return m_Table; } }

        public static AlignmentPlayer Find(Mobile mob, bool create = false)
        {
            if (mob == null)
                return null;

            AlignmentPlayer pl;

            if (!m_Table.TryGetValue(mob, out pl))
            {
                if (!create)
                    return null;

                m_Table[mob] = new AlignmentPlayer(mob);
            }

            return pl;
        }

        private Mobile m_Owner;
        private int m_Points;
        private int m_Rank;
        private DateTime m_NextKillAward; // not serialized

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Points
        {
            get { return m_Points; }
            set { m_Points = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Rank
        {
            get { return m_Rank; }
            set { m_Rank = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextKillAward
        {
            get { return m_NextKillAward; }
            set { m_NextKillAward = value; }
        }

        public AlignmentPlayer(Mobile m)
        {
            m_Owner = m;
            m_Points = AlignmentSystem.Elo0;
        }

        public void OnAlign(AlignmentType newType, AlignmentType oldType)
        {
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)1); // version

            writer.Write((int)m_Points);
            writer.Write((int)m_Rank);
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        m_Points = reader.ReadInt();
                        m_Rank = reader.ReadInt();

                        break;
                    }
            }

            if (version < 1)
                m_Points = AlignmentSystem.Elo0;
        }

        public bool IsEmpty()
        {
            return (m_Points == AlignmentSystem.Elo0 && m_Rank == 0);
        }

        public override string ToString()
        {
            return "...";
        }
    }
}