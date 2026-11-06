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

/* Server/ScriptCompiler.cs
 * CHANGELOG:
 *	1/2/09, Adam
 *		Add new SerializableObject for classes you want Serialized.
 *		This SerializableObject is checked in the ScriptCompiler to insure there are both
 * 			Serialize() and Deserialize() methods
 *	4/21/05 Pix
 *		Modified to allow for loading and debugging of scripts.dll.
 */

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Server
{
    public class ScriptCompiler
    {
        private static Assembly[] m_Assemblies = { null };
        public static Assembly[] Assemblies
        {
            get
            {
                // we just hardcode the assembly to the one-and-only
                //  later we will add assemblies here if we so desire
                Type t = typeof(Core);
                Assembly assemFromType = t.Assembly;
                m_Assemblies[0] = assemFromType;
                return m_Assemblies;
            }
        }

        private static Hashtable m_TypeCaches = new Hashtable();
        private static TypeCache? m_NullCache;

        public static TypeCache GetTypeCache(Assembly asm)
        {
            if (asm == null)
            {
                if (m_NullCache == null)
                    m_NullCache = new TypeCache(null);

                return m_NullCache;
            }

            TypeCache? c = (TypeCache?)m_TypeCaches[asm];

            if (c == null)
                m_TypeCaches[asm] = c = new TypeCache(asm);

            return c;
        }

        public static Type FindTypeByFullName(string? fullName)
        {
            return FindTypeByFullName(fullName, true);
        }

        public static Type FindTypeByFullName(string? fullName, bool ignoreCase)
        {
            Type? type = null;

            for (int i = 0; type == null && i < m_Assemblies.Length; ++i)
                type = GetTypeCache(m_Assemblies[i]).GetTypeByFullName(fullName, ignoreCase);

            if (type == null)
                type = GetTypeCache(Core.Assembly).GetTypeByFullName(fullName, ignoreCase);

            return type;
        }

        public static Type? FindTypeByName(string? name)
        {
            return FindTypeByName(name, true);
        }

        public static Type? FindTypeByName(string? name, bool ignoreCase)
        {
            Type? type = null;

            for (int i = 0; type == null && i < m_Assemblies.Length; ++i)
                type = GetTypeCache(m_Assemblies[i]).GetTypeByName(name, ignoreCase);

            if (type == null)
                type = GetTypeCache(Core.Assembly).GetTypeByName(name, ignoreCase);

            return type;
        }

        private static void EnsureDirectory(string dir)
        {
            string path = Path.Combine(Core.ExeDirectory, dir);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static string[] GetScripts(string type)
        {
            ArrayList list = new ArrayList();

            GetScripts(list, Path.Combine(Core.ExeDirectory, "Scripts"), type);

            return (string[])list.ToArray(typeof(string));
        }

        private static void GetScripts(ArrayList list, string path, string type)
        {
            foreach (string dir in Directory.GetDirectories(path))
                GetScripts(list, dir, type);

            list.AddRange(Directory.GetFiles(path, type));
        }
    }

    public class TypeCache
    {
        private Type[] m_Types;
        private TypeTable m_Names, m_FullNames;

        public Type[] Types { get { return m_Types; } }
        public TypeTable Names { get { return m_Names; } }
        public TypeTable FullNames { get { return m_FullNames; } }

        public Type? GetTypeByName(string? name, bool ignoreCase)
        {
            return m_Names.Get(name, ignoreCase);
        }

        public Type? GetTypeByFullName(string? fullName, bool ignoreCase)
        {
            return m_FullNames.Get(fullName, ignoreCase);
        }

        public TypeCache(Assembly? asm)
        {
            if (asm == null)
                m_Types = Type.EmptyTypes;
            else
                m_Types = asm.GetTypes();

            m_Names = new TypeTable(m_Types.Length);
            m_FullNames = new TypeTable(m_Types.Length);

            Type typeofTypeAliasAttribute = typeof(TypeAliasAttribute);

            for (int i = 0; i < m_Types.Length; ++i)
            {
                Type type = m_Types[i];

                // Simple-name map: on collisions, prefer non-Alignment over Alignment.
                Type? existing = m_Names.Get(type.Name, true);

                if (existing == null)
                {
                    m_Names.Add(type.Name, type);
                }
                else
                {
                    bool existingIsAlignment = existing.FullName != null &&
                                               existing.FullName.StartsWith("Server.Engines.Alignment.", StringComparison.Ordinal);

                    bool candidateIsAlignment = type.FullName != null &&
                                                type.FullName.StartsWith("Server.Engines.Alignment.", StringComparison.Ordinal);

                    if (existingIsAlignment && !candidateIsAlignment)
                        m_Names.Add(type.Name, type);
                }

                // Full-name map: always add (no meaningful collisions expected here).
                m_FullNames.Add(type.FullName, type);

                if (type.IsDefined(typeofTypeAliasAttribute, false))
                {
                    object[] attrs = type.GetCustomAttributes(typeofTypeAliasAttribute, false);

                    if (attrs != null && attrs.Length > 0)
                    {
                        TypeAliasAttribute? attr = attrs[0] as TypeAliasAttribute;

                        if (attr != null)
                        {
                            for (int j = 0; j < attr.Aliases.Length; ++j)
                                m_FullNames.Add(attr.Aliases[j], type);
                        }
                    }
                }
            }
        }
    }

    public class TypeTable
    {
        private Hashtable m_Sensitive, m_Insensitive;

        public void Add(string key, Type type)
        {
            m_Sensitive[key] = type;
            m_Insensitive[key] = type;
        }

        public Type? Get(string? key, bool ignoreCase)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(key));

            if (string.IsNullOrEmpty(key))
                return null;

            if (ignoreCase)
                return (Type)m_Insensitive[key];

            return (Type)m_Sensitive[key];
        }

        class myComparer : IEqualityComparer
        {
            public new bool Equals(object? x, object? y)
            {
                return x.Equals(y);
            }

            public int GetHashCode(object obj)
            {
                return obj.ToString().ToLower().GetHashCode();
            }
        }

        class myCultureComparer : IEqualityComparer
        {
            public CaseInsensitiveComparer myComparer;

            public myCultureComparer()
            {
                myComparer = CaseInsensitiveComparer.DefaultInvariant;
            }

            public myCultureComparer(CultureInfo myCulture)
            {
                myComparer = new CaseInsensitiveComparer(myCulture);
            }

            public new bool Equals(object? x, object? y)
            {
                return myComparer.Compare(x, y) == 0;
            }

            public int GetHashCode(object obj)
            {
                return obj.ToString().ToLower().GetHashCode();
            }
        }

        public TypeTable(int capacity)
        {
            m_Sensitive = new Hashtable(capacity);
            m_Insensitive = new Hashtable(capacity, new myCultureComparer());
        }
    }
}