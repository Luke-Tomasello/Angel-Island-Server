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

/* Changelog
 *	12/21/05 Taran Kain
 *		Added 1 to initial values of LastMobile and LastItem to ensure they never assign a creature/item their respective zeroes.
 *	12/17/05 Taran Kain
 *		Added code to make sure NewItem and NewMobile play nice with reserved serials
 */

using System;

namespace Server
{
    public struct Serial : IComparable
    {
        private int m_Serial;

        private static Serial m_LastMobile = Zero + 1;
        private static Serial m_LastItem = 0x40000000 + 1;

        public static readonly Serial MinusOne = new Serial(-1);
        public static readonly Serial Zero = new Serial(0);

        public static Serial NewMobile
        {
            get
            {
                while (World.FindMobile(m_LastMobile) != null /*|| World.IsReserved(m_LastMobile)*/)
                    m_LastMobile += 1;

                return m_LastMobile;
            }
        }

        public static Serial NewItem
        {
            get
            {
                while (World.FindItem(m_LastItem) != null /*|| World.IsReserved(m_LastItem)*/)
                    m_LastItem += 1;

                return m_LastItem;
            }
        }

        private Serial(int serial)
        {
            m_Serial = serial;
        }

        public int Value
        {
            get
            {
                return m_Serial;
            }
        }

        public bool IsMobile
        {
            get
            {
                return (m_Serial > 0 && m_Serial < 0x40000000);
            }
        }

        public bool IsItem
        {
            get
            {
                return (m_Serial >= 0x40000000 && m_Serial <= 0x7FFFFFFF);
            }
        }

        public bool IsValid
        {
            get
            {
                return (m_Serial > 0);
            }
        }

        public override int GetHashCode()
        {
            return m_Serial;
        }

        public int CompareTo(object o)
        {
            if (o == null) return 1;
            else if (!(o is Serial)) throw new ArgumentException();

            int ser = ((Serial)o).m_Serial;

            if (m_Serial > ser) return 1;
            else if (m_Serial < ser) return -1;
            else return 0;
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is Serial)) return false;

            return ((Serial)o).m_Serial == m_Serial;
        }

        public static bool operator ==(Serial l, Serial r)
        {
            return l.m_Serial == r.m_Serial;
        }

        public static bool operator !=(Serial l, Serial r)
        {
            return l.m_Serial != r.m_Serial;
        }

        public static bool operator >(Serial l, Serial r)
        {
            return l.m_Serial > r.m_Serial;
        }

        public static bool operator <(Serial l, Serial r)
        {
            return l.m_Serial < r.m_Serial;
        }

        public static bool operator >=(Serial l, Serial r)
        {
            return l.m_Serial >= r.m_Serial;
        }

        public static bool operator <=(Serial l, Serial r)
        {
            return l.m_Serial <= r.m_Serial;
        }

        /*public static Serial operator ++ ( Serial l )
		{
			return new Serial( l + 1 );
		}*/

        public override string ToString()
        {
            return string.Format("0x{0:X8}", m_Serial);
        }

        public static implicit operator int(Serial a)
        {
            return a.m_Serial;
        }

        public static implicit operator Serial(int a)
        {
            return new Serial(a);
        }
    }
}