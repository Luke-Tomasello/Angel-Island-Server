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

/* Scripts\Engines\Alignment\Alignment.cs
 * Changelog:
 *  4/14/23, Yoar
 *      Initial version.
 */

namespace Server.Engines.Alignment
{
    public abstract class Alignment
    {
        private static readonly Alignment[] m_Table = new Alignment[]
            {
                new Order(),
                new Chaos(),

                new Council(),
                new Pirate(),
                new Brigand(),
                new Orc(),
                new Savage(),
                new Undead(),
                new Militia(),

                new Outcast(),
            };

        public static Alignment[] Table { get { return m_Table; } }

        public static Alignment Get(AlignmentType type)
        {
            for (int i = 0; i < m_Table.Length; i++)
            {
                if (m_Table[i].Type == type)
                    return m_Table[i];
            }

            return null;
        }

        public abstract AlignmentType Type { get; }

        private AlignmentDefinition m_Definition;
        private AlignmentState m_State;

        public AlignmentDefinition Definition { get { return m_Definition; } }
        public AlignmentState State { get { return m_State; } }

        public Alignment(AlignmentDefinition def)
        {
            m_Definition = def;
            m_State = new AlignmentState(this);
        }

        public static void WriteReference(GenericWriter writer, Alignment alignment)
        {
            writer.Write((byte)0x1);
            writer.Write((byte)alignment.Type);
        }

        public static Alignment ReadReference(GenericReader reader)
        {
            switch (reader.ReadByte())
            {
                case 0x1:
                    {
                        return Get((AlignmentType)reader.ReadByte());
                    }
            }

            return null;
        }
    }
}