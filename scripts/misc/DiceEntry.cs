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

/* Misc/DiceEntry.cs
 * CHANGELOG:
 *  12/4/21, Yoar
 *      Initial version
 */

using System;

namespace Server
{
    [Parsable]
    public struct DiceEntry : IEquatable<DiceEntry>
    {
        public static readonly DiceEntry Empty = new DiceEntry();

        private int m_Count;
        private int m_Sides;
        private int m_Bonus;

        public int Count { get { return m_Count; } }
        public int Sides { get { return m_Sides; } }
        public int Bonus { get { return m_Bonus; } }

        public static DiceEntry Parse(string value)
        {
            return new DiceEntry(value);
        }

        public DiceEntry(string str)
        {
            Parse(str, out m_Count, out m_Sides, out m_Bonus);
        }

        public static void Parse(string str, out int count, out int sides, out int bonus)
        {
            count = sides = bonus = 0;

            int start = 0;
            int index = str.IndexOf('d', start);

            if (index < start)
                return;

            count = Utility.ToInt32(str.Substring(start, index - start));

            bool negative;

            start = index + 1;
            index = str.IndexOf('+', start);

            if (negative = (index < start))
                index = str.IndexOf('-', start);

            if (index < start)
                index = str.Length;

            sides = Utility.ToInt32(str.Substring(start, index - start));

            if (index == str.Length)
                return;

            start = index + 1;
            index = str.Length;

            bonus = Utility.ToInt32(str.Substring(start, index - start));

            if (negative)
                bonus = -bonus;
        }

        public DiceEntry(int count, int sides)
            : this(count, sides, 0)
        {
        }

        public DiceEntry(int count, int sides, int bonus)
        {
            m_Count = count;
            m_Sides = sides;
            m_Bonus = bonus;
        }

        public DiceEntry(DiceEntry copy)
        {
            m_Count = copy.m_Count;
            m_Sides = copy.m_Sides;
            m_Bonus = copy.m_Bonus;
        }

        public static bool operator ==(DiceEntry a, DiceEntry b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(DiceEntry a, DiceEntry b)
        {
            return !a.Equals(b);
        }

        public override bool Equals(object obj)
        {
            return obj is DiceEntry && this.Equals((DiceEntry)obj);
        }

        public bool Equals(DiceEntry other)
        {
            return m_Count == other.m_Count && m_Sides == other.m_Sides && m_Bonus == other.m_Bonus;
        }

        public override int GetHashCode()
        {
            return m_Count.GetHashCode() ^ m_Sides.GetHashCode() ^ m_Bonus.GetHashCode();
        }

        public int Roll()
        {
            int v = m_Bonus;

            for (int i = 0; i < m_Count; ++i)
                v += Utility.Random(1, m_Sides);

            return v;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)m_Count);
            writer.Write((int)m_Sides);
            writer.Write((int)m_Bonus);
        }

        public DiceEntry(GenericReader reader)
        {
            m_Count = reader.ReadInt();
            m_Sides = reader.ReadInt();
            m_Bonus = reader.ReadInt();
        }

        public override string ToString()
        {
            if (m_Bonus > 0)
                return string.Format("{0}d{1}+{2}", m_Count, m_Sides, m_Bonus);
            else if (m_Bonus < 0)
                return string.Format("{0}d{1}-{2}", m_Count, m_Sides, m_Bonus);
            else
                return string.Format("{0}d{1}", m_Count, m_Sides);
        }
    }
}