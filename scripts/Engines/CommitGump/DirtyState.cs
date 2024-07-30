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

/* Scripts/Engines/CommitGump/DirtyState.cs
 * CHANGELOG:
 *	06/07/09, plasma
 *		Added dirty reference
 *	01/26/09 - Plasma,
 *		Initial creation
 */
using System;

namespace Server.Engines.CommitGump
{

    public class DirtyState
    {
        private sealed class DirtyValue<T> where T : struct
        {
            public T m_Value = default(T);
            private T m_OriginalValue = default(T);

            public bool IsDirty()
            {
                return !(m_Value.Equals(OriginalValue));
            }

            public DirtyValue(T initialValue)
            {
                m_OriginalValue = m_Value = initialValue;
            }

            public void SetValue(T newValue)
            {
                m_Value = newValue;
            }

            public T OriginalValue
            {
                get { return m_OriginalValue; }
            }
        }

        private sealed class DirtyReference<T> where T : class, IEquatable<T>, ICloneable
        {
            public T m_Value = default(T);
            private T m_OriginalValue = default(T);

            public bool IsDirty()
            {
                return !(m_Value.Equals(OriginalValue));
            }

            public DirtyReference(T initialValue)
            {
                m_Value = initialValue;
                //clone here so we aren't comparing whatever is required with ourselves :p
                m_OriginalValue = (T)m_Value.Clone();
            }

            public void SetValue(T newValue)
            {
                m_Value = newValue;
            }

            public T OriginalValue
            {
                get { return m_OriginalValue; }
            }
        }

        private CommitGumpBase.GumpSession m_DirtyFields = new CommitGumpBase.GumpSession();

        //value

        public void SetValue<T>(string key, T value) where T : struct
        {
            if (m_DirtyFields[key] == null)
                m_DirtyFields[key] = new DirtyValue<T>(value);
            ((DirtyValue<T>)m_DirtyFields[key]).SetValue(value);
        }

        public T GetValue<T>(string key) where T : struct
        {
            if (m_DirtyFields[key] == null)
                m_DirtyFields[key] = new DirtyValue<T>(default(T));
            return ((DirtyValue<T>)m_DirtyFields[key]).m_Value;
        }

        public T GetOriginalValue<T>(string key) where T : struct
        {
            if (m_DirtyFields[key] == null)
                m_DirtyFields[key] = new DirtyValue<T>(default(T));
            return ((DirtyValue<T>)m_DirtyFields[key]).OriginalValue;
        }

        public bool IsValueDirty<T>(string key) where T : struct
        {
            if (m_DirtyFields[key] == null)
                m_DirtyFields[key] = new DirtyValue<T>(default(T));
            return ((DirtyValue<T>)m_DirtyFields[key]).IsDirty();
        }

        //reference

        public void SetReference<T>(string key, T value) where T : class, IEquatable<T>, ICloneable
        {
            if (m_DirtyFields[key] == null)
                m_DirtyFields[key] = new DirtyReference<T>(value);
            ((DirtyReference<T>)m_DirtyFields[key]).SetValue(value);
        }

        public T GetReference<T>(string key) where T : class, IEquatable<T>, ICloneable
        {
            if (m_DirtyFields[key] == null)
                m_DirtyFields[key] = new DirtyReference<T>(default(T));
            return ((DirtyReference<T>)m_DirtyFields[key]).m_Value;
        }

        public T GetOriginalReference<T>(string key) where T : class, IEquatable<T>, ICloneable
        {
            if (m_DirtyFields[key] == null)
                m_DirtyFields[key] = new DirtyReference<T>(default(T));
            return ((DirtyReference<T>)m_DirtyFields[key]).OriginalValue;
        }

        public bool IsReferenceDirty<T>(string key) where T : class, IEquatable<T>, ICloneable
        {
            if (m_DirtyFields[key] == null)
                m_DirtyFields[key] = new DirtyReference<T>(default(T));
            return ((DirtyReference<T>)m_DirtyFields[key]).IsDirty();
        }


    }

}