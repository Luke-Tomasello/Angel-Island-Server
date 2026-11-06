/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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
 *  10/27/2026, Adam
 *		Add StringListAttribute: [props offers a string property's values as a pick list, read from a public static
 *		string[] property or field each time the list opens (e.g. names loaded from a data file, which CustomEnum's
 *		compile-time names cannot be). First use: ChampEngine.DefinitionName.
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
    /// <summary>
    /// Controls how a property participates in duplication and relinking.
    /// </summary>
    public enum CopyType
    {
        /// <summary>
        /// Default behavior: copy the value and allow relinking.
        /// </summary>
        Copy,

        /// <summary>
        /// Do not copy this property at all.
        /// </summary>
        DoNotCopy,

        /// <summary>
        /// Copy the property value, but do not relink it.
        /// Useful for shared or cross-map references (e.g., LootPacks on Internal map).
        /// </summary>
        DoNotRelink
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CopyableAttribute : Attribute
    {
        public CopyType CopyType { get; }

        public CopyableAttribute(CopyType copyType)
        {
            CopyType = copyType;
        }

        /// <summary>
        /// Returns the effective CopyType for a property.
        /// Defaults to Copy if no attribute is present.
        /// </summary>
        public static CopyType Find(PropertyInfo propInfo)
        {
            var attr = propInfo.GetCustomAttribute<CopyableAttribute>();
            return attr?.CopyType ?? CopyType.Copy;
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
        public int Compare(object? x, object? y)
        {
            MethodInfo? a = x as MethodInfo;
            MethodInfo? b = y as MethodInfo;

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

            CallPriorityAttribute? attr = objs[0] as CallPriorityAttribute;

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

    // [props offers these strings as a pick list for a string property. The names are read from the provider's public
    // static string[] property or field each time the list opens; an empty list falls back to ordinary text entry.
    [AttributeUsage(AttributeTargets.Property)]
    public class StringListAttribute : Attribute
    {
        private readonly Type m_Provider;
        private readonly string m_Member;

        public StringListAttribute(Type provider, string member)
        {
            m_Provider = provider;
            m_Member = member;
        }

        public string[] GetNames()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            object? value = m_Provider.GetProperty(m_Member, flags)?.GetValue(null) ?? m_Provider.GetField(m_Member, flags)?.GetValue(null);
            return value as string[] ?? new string[0];
        }
    }

    [AttributeUsage(AttributeTargets.Constructor)]
    public class ConstructableAttribute : Attribute
    {
        public ConstructableAttribute()
        {
        }
    }

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

}