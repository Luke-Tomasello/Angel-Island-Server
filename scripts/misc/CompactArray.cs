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

/* Misc/CompactArray.cs
 * CHANGELOG:
 *  10/28/21, Yoar
 *		Initial version.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Server
{
    /// <summary>
    /// Based on <see cref="BaseAttributes"/>. Efficient storage of integer values.
    /// Only non-zero integers are kept in memory. Index the object using uint flags.
    /// </summary>
    public class CompactArray : IEquatable<CompactArray>, IEnumerable<uint>
    {
        private static readonly int[] m_EmptyValues = new int[0];

        private uint m_Flags;
        private int[] m_Values;

        public uint Flags { get { return m_Flags; } }
        public int[] Values { get { return m_Values; } }

        public int Count { get { return m_Values.Length; } }

        public int this[uint flag]
        {
            get { return GetValue(flag); }
            set { SetValue(flag, value); }
        }

        public CompactArray()
        {
            m_Values = m_EmptyValues;
        }

        private int GetValue(uint flag)
        {
            if ((m_Flags & flag) == 0)
                return 0;

            int index = GetIndex(flag);

            if (index >= 0 && index < m_Values.Length)
                return m_Values[index];

            return 0;
        }

        private void SetValue(uint flag, int value)
        {
            if (value != 0)
            {
                int index = GetIndex(flag);

                if ((m_Flags & flag) != 0)
                {
                    if (index >= 0 && index < m_Values.Length)
                        m_Values[index] = value;
                }
                else if (index >= 0 && index <= m_Values.Length)
                {
                    m_Flags |= flag;

                    m_Values = InsertAt(m_Values, index, value);
                }
            }
            else if ((m_Flags & flag) != 0)
            {
                int index = GetIndex(flag);

                if (index >= 0 && index < m_Values.Length)
                {
                    m_Flags &= ~flag;

                    if (m_Values.Length == 1)
                        m_Values = m_EmptyValues;
                    else
                        m_Values = RemoveAt(m_Values, index);
                }
            }
        }

        public static T[] InsertAt<T>(T[] array, int index, T value)
        {
            T[] temp = array;

            array = new T[temp.Length + 1];

            Array.Copy(temp, 0, array, 0, index);
            Array.Copy(temp, index, array, index + 1, temp.Length - index);

            array[index] = value;

            return array;
        }

        public static T[] RemoveAt<T>(T[] array, int index)
        {
            T[] temp = array;

            array = new T[temp.Length - 1];

            Array.Copy(temp, 0, array, 0, index);
            Array.Copy(temp, index + 1, array, index, temp.Length - index - 1);

            return array;
        }

        private int GetIndex(uint flag)
        {
            uint curr = 0x1;

            int index = 0;

            for (int i = 0; i < 32; i++, curr <<= 1)
            {
                if (curr == flag)
                    return index;

                if ((m_Flags & curr) != 0)
                    index++;
            }

            return -1;
        }

        public void Clear()
        {
            m_Flags = 0;
            m_Values = m_EmptyValues;
        }

        #region IEquatable

        public bool Equals(CompactArray other)
        {
            if (other == null || m_Flags != other.m_Flags || m_Values.Length != other.m_Values.Length)
                return false;

            for (int i = 0; i < m_Values.Length; i++)
            {
                if (m_Values[i] != other.m_Values[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is CompactArray && Equals((CompactArray)obj);
        }

        public override int GetHashCode()
        {
            int hash = m_Flags.GetHashCode();

            for (int i = 0; i < m_Values.Length; i++)
                hash ^= m_Values[i];

            return hash;
        }

        public static bool operator ==(CompactArray a, CompactArray b)
        {
            if ((object)a == null || (object)b == null)
                return Object.Equals(a, b);
            else
                return a.Equals(b);
        }

        public static bool operator !=(CompactArray a, CompactArray b)
        {
            if ((object)a == null || (object)b == null)
                return !Object.Equals(a, b);
            else
                return !a.Equals(b);
        }

        #endregion

        #region IEnumerable

        IEnumerator<uint> IEnumerable<uint>.GetEnumerator()
        {
            return (IEnumerator<uint>)this.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return new InternalEnumerator(this);
        }

        public class InternalEnumerator : IEnumerator<uint>
        {
            private CompactArray m_Array;
            private uint m_Mask;
            private uint m_Curr;
            private bool m_Disposed;

            public InternalEnumerator(CompactArray array)
            {
                m_Array = array;
                Reset();
            }

            uint IEnumerator<uint>.Current
            {
                get { return (uint)this.Current; }
            }

            public object Current
            {
                get { return m_Curr; }
            }

            public bool MoveNext()
            {
                if (m_Curr == 0)
                {
                    m_Curr = 0x1;

                    if ((m_Mask & 0x1) != 0)
                        return true;
                }

                while (m_Mask != 0)
                {
                    m_Mask >>= 1;
                    m_Curr <<= 1;

                    if ((m_Mask & 0x1) != 0)
                        return true;
                }

                return false;
            }

            public void Reset()
            {
                m_Mask = m_Array.Flags;
                m_Curr = 0;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;

                m_Array = null;
                m_Disposed = true;
            }
        }

        #endregion

        public virtual void Serialize(GenericWriter writer)
        {
            writer.Write((byte)0); // version

            writer.Write((uint)m_Flags);
            writer.WriteEncodedInt((int)m_Values.Length);

            foreach (int value in m_Values)
                writer.WriteEncodedInt((int)value);
        }

        public virtual void Deserialize(GenericReader reader)
        {
            byte version = reader.ReadByte();

            m_Flags = reader.ReadUInt();
            m_Values = new int[reader.ReadEncodedInt()];

            for (int i = 0; i < m_Values.Length; i++)
                m_Values[i] = reader.ReadEncodedInt();
        }

        public virtual bool IsEmpty()
        {
            return this.Count == 0;
        }
    }

    /// <summary>
    /// Based on <see cref="BaseAttributes"/>. Efficient storage of integer values.
    /// Only non-zero integers are kept in memory. Index the object using ulong flags.
    /// </summary>
    public class CompactArray64 : IEquatable<CompactArray64>, IEnumerable<ulong>
    {
        private static readonly int[] m_EmptyValues = new int[0];

        private ulong m_Flags;
        private int[] m_Values;

        public ulong Flags { get { return m_Flags; } }
        public int[] Values { get { return m_Values; } }

        public int Count { get { return m_Values.Length; } }

        public int this[ulong flag]
        {
            get { return GetValue(flag); }
            set { SetValue(flag, value); }
        }

        public CompactArray64()
        {
            m_Values = m_EmptyValues;
        }

        private int GetValue(ulong flag)
        {
            if ((m_Flags & flag) == 0)
                return 0;

            int index = GetIndex(flag);

            if (index >= 0 && index < m_Values.Length)
                return m_Values[index];

            return 0;
        }

        private void SetValue(ulong flag, int value)
        {
            if (value != 0)
            {
                int index = GetIndex(flag);

                if ((m_Flags & flag) != 0)
                {
                    if (index >= 0 && index < m_Values.Length)
                        m_Values[index] = value;
                }
                else if (index >= 0 && index <= m_Values.Length)
                {
                    m_Flags |= flag;

                    m_Values = CompactArray.InsertAt(m_Values, index, value);
                }
            }
            else if ((m_Flags & flag) != 0)
            {
                int index = GetIndex(flag);

                if (index >= 0 && index < m_Values.Length)
                {
                    m_Flags &= ~flag;

                    if (m_Values.Length == 1)
                        m_Values = m_EmptyValues;
                    else
                        m_Values = CompactArray.RemoveAt(m_Values, index);
                }
            }
        }

        private int GetIndex(ulong flag)
        {
            ulong curr = 0x1;

            int index = 0;

            for (int i = 0; i < 64; i++, curr <<= 1)
            {
                if (curr == flag)
                    return index;

                if ((m_Flags & curr) != 0)
                    index++;
            }

            return -1;
        }

        public void Clear()
        {
            m_Flags = 0;
            m_Values = m_EmptyValues;
        }

        #region IEquatable

        public bool Equals(CompactArray64 other)
        {
            if (other == null || m_Flags != other.m_Flags || m_Values.Length != other.m_Values.Length)
                return false;

            for (int i = 0; i < m_Values.Length; i++)
            {
                if (m_Values[i] != other.m_Values[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is CompactArray64 && Equals((CompactArray64)obj);
        }

        public override int GetHashCode()
        {
            int hash = m_Flags.GetHashCode();

            for (int i = 0; i < m_Values.Length; i++)
                hash ^= m_Values[i];

            return hash;
        }

        public static bool operator ==(CompactArray64 a, CompactArray64 b)
        {
            if ((object)a == null || (object)b == null)
                return Object.Equals(a, b);
            else
                return a.Equals(b);
        }

        public static bool operator !=(CompactArray64 a, CompactArray64 b)
        {
            if ((object)a == null || (object)b == null)
                return !Object.Equals(a, b);
            else
                return !a.Equals(b);
        }

        #endregion

        #region IEnumerable

        IEnumerator<ulong> IEnumerable<ulong>.GetEnumerator()
        {
            return (IEnumerator<ulong>)this.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return new InternalEnumerator(this);
        }

        public class InternalEnumerator : IEnumerator<ulong>
        {
            private CompactArray64 m_Array;
            private ulong m_Mask;
            private ulong m_Curr;
            private bool m_Disposed;

            public InternalEnumerator(CompactArray64 array)
            {
                m_Array = array;
                Reset();
            }

            ulong IEnumerator<ulong>.Current
            {
                get { return (ulong)this.Current; }
            }

            public object Current
            {
                get { return m_Curr; }
            }

            public bool MoveNext()
            {
                if (m_Curr == 0)
                {
                    m_Curr = 0x1;

                    if ((m_Mask & 0x1) != 0)
                        return true;
                }

                while (m_Mask != 0)
                {
                    m_Mask >>= 1;
                    m_Curr <<= 1;

                    if ((m_Mask & 0x1) != 0)
                        return true;
                }

                return false;
            }

            public void Reset()
            {
                m_Mask = m_Array.Flags;
                m_Curr = 0;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;

                m_Array = null;
                m_Disposed = true;
            }
        }

        #endregion

        public virtual void Serialize(GenericWriter writer)
        {
            writer.Write((byte)0); // version

            writer.Write((ulong)m_Flags);
            writer.WriteEncodedInt((int)m_Values.Length);

            foreach (int value in m_Values)
                writer.WriteEncodedInt((int)value);
        }

        public virtual void Deserialize(GenericReader reader)
        {
            byte version = reader.ReadByte();

            m_Flags = reader.ReadULong();
            m_Values = new int[reader.ReadEncodedInt()];

            for (int i = 0; i < m_Values.Length; i++)
                m_Values[i] = reader.ReadEncodedInt();
        }

        public virtual bool IsEmpty()
        {
            return this.Count == 0;
        }
    }
}