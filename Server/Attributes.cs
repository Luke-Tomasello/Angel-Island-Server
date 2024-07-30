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

/* Server\Attributes.cs
 * ChangeLog:
 *  2/16/2024, Adam
 *		Add new CopyableAttribute
 *		This attribute should be applied to R/W properties that should not be copied during CopyProperties.
 *		Example: Class X has two properties which map to the same underlying variable. Say IEntity Y
 *		    Properties Mobile and Item both R/W Y.
 *		    Both Mobile and Item should be marked as NoCopy to prevent CopyProperties from clobbering say Mobile
 *		    when Item is read of vice versa.
 */

using System;
using System.Collections;
using System.Reflection;

namespace Server
{
    public enum CopyType
    {
        Copy,
        DoNotCopy,
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CopyableAttribute : Attribute
    {
        private CopyType m_CopyType;

        public CopyType CopyType { get { return m_CopyType; } }

        public CopyableAttribute(CopyType copyType)
        {
            m_CopyType = copyType;
        }

        public static CopyType Find(PropertyInfo propInfo)
        {
            CopyableAttribute attr = propInfo.GetCustomAttribute<CopyableAttribute>();

            if (attr != null)
                return attr.CopyType;
            else
                return CopyType.Copy; // default: copy
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class HueAttribute : Attribute
    {
        public HueAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class BodyAttribute : Attribute
    {
        public BodyAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class PropertyObjectAttribute : Attribute
    {
        public PropertyObjectAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class NoSortAttribute : Attribute
    {
        public NoSortAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CallPriorityAttribute : Attribute
    {
        private int m_Priority;

        public int Priority
        {
            get { return m_Priority; }
            set { m_Priority = value; }
        }

        public CallPriorityAttribute(int priority)
        {
            m_Priority = priority;
        }
    }

    public class CallPriorityComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            MethodInfo a = x as MethodInfo;
            MethodInfo b = y as MethodInfo;

            if (a == null && b == null)
                return 0;

            if (a == null)
                return 1;

            if (b == null)
                return -1;

            return GetPriority(a) - GetPriority(b);
        }

        private int GetPriority(MethodInfo mi)
        {
            object[] objs = mi.GetCustomAttributes(typeof(CallPriorityAttribute), true);

            if (objs == null)
                return 0;

            if (objs.Length == 0)
                return 0;

            CallPriorityAttribute attr = objs[0] as CallPriorityAttribute;

            if (attr == null)
                return 0;

            return attr.Priority;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TypeAliasAttribute : Attribute
    {
        private string[] m_Aliases;

        public string[] Aliases
        {
            get
            {
                return m_Aliases;
            }
        }

        public TypeAliasAttribute(params string[] aliases)
        {
            m_Aliases = aliases;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ParsableAttribute : Attribute
    {
        public ParsableAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public class CustomEnumAttribute : Attribute
    {
        private string[] m_Names;

        public string[] Names
        {
            get
            {
                return m_Names;
            }
        }

        public CustomEnumAttribute(string[] names)
        {
            m_Names = names;
        }
    }

    [AttributeUsage(AttributeTargets.Constructor)]
    public class ConstructableAttribute : Attribute
    {
        public ConstructableAttribute()
        {
        }
    }
#if true
    [AttributeUsage(AttributeTargets.Property)]
    public class CommandPropertyAttribute : Attribute
    {
        private AccessLevel m_ReadLevel, m_WriteLevel;
        private bool m_ReadOnly;

        public AccessLevel ReadLevel
        {
            get
            {
                return m_ReadLevel;
            }
        }

        public AccessLevel WriteLevel
        {
            get
            {
                return m_WriteLevel;
            }
        }

        public bool ReadOnly
        {
            get
            {
                return m_ReadOnly;
            }
        }

        public CommandPropertyAttribute(AccessLevel level, bool readOnly)
        {
            m_ReadLevel = level;
            m_ReadOnly = readOnly;
        }

        public CommandPropertyAttribute(AccessLevel level) : this(level, level)
        {
        }

        public CommandPropertyAttribute(AccessLevel readLevel, AccessLevel writeLevel)
        {
            m_ReadLevel = readLevel;
            m_WriteLevel = writeLevel;
        }
    }
#else

    [AttributeUsage(AttributeTargets.Property)]
    public class CommandPropertyAttribute : Attribute
    {
        private AccessLevel m_ReadLevel, m_WriteLevel;

        public AccessLevel ReadLevel
        {
            get
            {
                return m_ReadLevel;
            }
        }

        public AccessLevel WriteLevel
        {
            get
            {
                return m_WriteLevel;
            }
        }

        public CommandPropertyAttribute(AccessLevel level)
            : this(level, level)
        {
        }

        public CommandPropertyAttribute(AccessLevel readLevel, AccessLevel writeLevel)
        {
            m_ReadLevel = readLevel;
            m_WriteLevel = writeLevel;
        }
    }
#endif
}