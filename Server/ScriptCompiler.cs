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
            /*set
            {
                m_Assemblies = value;
            }*/
        }

        private static ArrayList m_AdditionalReferences = new ArrayList();

        public static string[] GetReferenceAssemblies()
        {
            ArrayList list = new ArrayList();

            string path = Path.Combine(Core.DataDirectory, "Assemblies.cfg");

            if (File.Exists(path))
            {
                using (StreamReader ip = new StreamReader(path))
                {
                    string line;

                    while ((line = ip.ReadLine()) != null)
                    {
                        if (line.Length > 0 && !line.StartsWith("#"))
                            list.Add(line);
                    }
                }
            }

            list.Add(Core.ExePath);

            list.AddRange(m_AdditionalReferences);

            return (string[])list.ToArray(typeof(string));
        }
#if false
        [Obsolete]
        private static CompilerResults CompileCSScripts()
        {
            return CompileCSScripts(false);
        }

        [Obsolete]
        private static CompilerResults CompileCSScripts(bool debug)
        {
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            ICodeCompiler compiler = provider.CreateCompiler();

            Console.Write("Scripts: Compiling C# scripts...");
            string[] files = GetScripts("*.cs");

            if (files.Length == 0)
            {
                Console.WriteLine("no files found.");
                return null;
            }

            string path = GetUnusedPath("Scripts.CS");

            CompilerParameters parms = new CompilerParameters(GetReferenceAssemblies(), path, debug);

            if (!debug)
                parms.CompilerOptions = "/debug- /optimize+"; // doesn't seem to have any effect

            CompilerResults results = compiler.CompileAssemblyFromFileBatch(parms, files);

            m_AdditionalReferences.Add(path);

            if (results.Errors.Count > 0)
            {
                int errorCount = 0, warningCount = 0;

                foreach (CompilerError e in results.Errors)
                {
                    if (e.IsWarning)
                        ++warningCount;
                    else
                        ++errorCount;
                }

                if (errorCount > 0)
                    Console.WriteLine("failed ({0} errors, {1} warnings)", errorCount, warningCount);
                else
                    Console.WriteLine("done ({0} errors, {1} warnings)", errorCount, warningCount);

                foreach (CompilerError e in results.Errors)
                {
                    Console.WriteLine(" - {0}: {1}: {2}: (line {3}, column {4}) {5}", e.IsWarning ? "Warning" : "Error", e.FileName, e.ErrorNumber, e.Line, e.Column, e.ErrorText);
                }
            }
            else
            {
                Console.WriteLine("done (0 errors, 0 warnings)");
            }

            return results;
        }

        private static CompilerResults CompileVBScripts()
        {
            return CompileVBScripts(false);
        }

        [Obsolete]
        private static CompilerResults CompileVBScripts(bool debug)
        {
            VBCodeProvider provider = new VBCodeProvider();
            ICodeCompiler compiler = provider.CreateCompiler();

            Console.Write("Scripts: Compiling VB.net scripts...");
            string[] files = GetScripts("*.vb");

            if (files.Length == 0)
            {
                Console.WriteLine("no files found.");
                return null;
            }

            string path = GetUnusedPath("Scripts.VB");

            CompilerResults results = compiler.CompileAssemblyFromFileBatch(new CompilerParameters(GetReferenceAssemblies(), path, true), files);

            m_AdditionalReferences.Add(path);

            if (results.Errors.Count > 0)
            {
                int errorCount = 0, warningCount = 0;

                foreach (CompilerError e in results.Errors)
                {
                    if (e.IsWarning)
                        ++warningCount;
                    else
                        ++errorCount;
                }

                if (errorCount > 0)
                    Console.WriteLine("failed ({0} errors, {1} warnings)", errorCount, warningCount);
                else
                    Console.WriteLine("done ({0} errors, {1} warnings)", errorCount, warningCount);

                foreach (CompilerError e in results.Errors)
                {
                    Console.WriteLine(" - {0}: {1}: {2}: (line {3}, column {4}) {5}", e.IsWarning ? "Warning" : "Error", e.FileName, e.ErrorNumber, e.Line, e.Column, e.ErrorText);
                }
            }
            else
            {
                Console.WriteLine("done (0 errors, 0 warnings)");
            }

            return results;
        }

        private static string GetUnusedPath(string name)
        {
            string path = Path.Combine(Core.BaseDirectory, String.Format("Scripts/Output/{0}.dll", name));

            for (int i = 2; File.Exists(path) && i <= 1000; ++i)
                path = Path.Combine(Core.BaseDirectory, String.Format("Scripts/Output/{0}.{1}.dll", name, i));

            return path;
        }

        private static void DeleteFiles(string mask)
        {
            try
            {
                string[] files = Directory.GetFiles(Path.Combine(Core.BaseDirectory, "Scripts/Output"), mask);

                foreach (string file in files)
                {
                    try { File.Delete(file); }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
#endif
#if false
        public static bool Compile()
        {
            return Compile(false);
        }

        [Obsolete]
        public static bool Compile(bool debug)
        {
            /* Pix: Here I'm letting release builds always compile the scripts as per the 'default' way
			 *  RunUO works.  In debug mode, I check for the existence of a pre-compiled 'Scripts.dll', and
			 *  if that exists, then load that, else compile normal scripts.
			 */
            //#if !DEBUG
#if !LOAD_SCRIPTS_DLL
			return CompileRawScripts(debug);
#else
            string path = Path.Combine(Core.BaseDirectory, "Scripts.dll");

            if (!File.Exists(path))
            {
                Console.WriteLine("Pre-Compiled Scripts.dll not found.");
                return CompileRawScripts(debug);
            }

            Assembly scripts = null;

            try
            {
                scripts = Assembly.LoadFile(path);
            }
            catch (Exception err)
            {
                Console.WriteLine("Impossible to load Scripts.dll. Error: {0}", err.ToString());
                return false;
            }

            m_Assemblies = new Assembly[] { scripts };
            Console.WriteLine("Scripts.dll loaded");

            Console.Write("Scripts: Verifying...");
            Core.VerifySerialization();
            Console.WriteLine("done ({0} items, {1} mobiles, and {2} Serializable objects)", Core.ScriptItems, Core.ScriptMobiles, Core.ScriptSerializableObjects);

            ArrayList invoke = new ArrayList();

            Type[] types = scripts.GetTypes();

            for (int i = 0; i < types.Length; ++i)
            {
                MethodInfo m = types[i].GetMethod("Configure", BindingFlags.Static | BindingFlags.Public);

                if (m != null)
                    invoke.Add(m);
            }

            invoke.Sort(new CallPriorityComparer());

            for (int i = 0; i < invoke.Count; ++i)
                ((MethodInfo)invoke[i]).Invoke(null, null);

            invoke.Clear();

            World.Load();

            for (int i = 0; i < types.Length; ++i)
            {
                MethodInfo m = types[i].GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public);

                if (m != null)
                    invoke.Add(m);
            }

            invoke.Sort(new CallPriorityComparer());

            for (int i = 0; i < invoke.Count; ++i)
                ((MethodInfo)invoke[i]).Invoke(null, null);

            return true;
#endif
        }

        [Obsolete]
        public static bool CompileRawScripts(bool debug)
        {
            EnsureDirectory("Scripts/");
            EnsureDirectory("Scripts/Output/");

            //m_Assemblies = new Assembly[]
            //	{ Assembly.LoadFile( Path.Combine( Core.BaseDirectory, "Scripts/Output/Scripts.CS.dll" ) ) };

            DeleteFiles("Scripts.CS*.dll");
            DeleteFiles("Scripts.VB*.dll");
            DeleteFiles("Scripts*.dll");

            if (m_AdditionalReferences.Count > 0)
                m_AdditionalReferences.Clear();

            CompilerResults csResults = null, vbResults = null;

            csResults = CompileCSScripts(debug);

            if (csResults == null || !csResults.Errors.HasErrors)
                vbResults = CompileVBScripts(debug);

            if ((csResults == null || !csResults.Errors.HasErrors) && (vbResults == null || !vbResults.Errors.HasErrors) && (vbResults != null || csResults != null))
            {
                int a = 0;
                if (csResults == null || vbResults == null)
                    m_Assemblies = new Assembly[1];
                else
                    m_Assemblies = new Assembly[2];

                if (csResults != null)
                    m_Assemblies[a++] = csResults.CompiledAssembly;

                if (vbResults != null)
                    m_Assemblies[a++] = vbResults.CompiledAssembly;

                Console.Write("Scripts: Verifying...");
                Core.VerifySerialization();
                Console.WriteLine("done ({0} items, {1} mobiles)", Core.ScriptItems, Core.ScriptMobiles);

                ArrayList invoke = new ArrayList();

                for (a = 0; a < m_Assemblies.Length; ++a)
                {
                    Type[] types = m_Assemblies[a].GetTypes();

                    for (int i = 0; i < types.Length; ++i)
                    {
                        MethodInfo m = types[i].GetMethod("Configure", BindingFlags.Static | BindingFlags.Public);

                        if (m != null)
                            invoke.Add(m);
                        //m.Invoke( null, null );
                    }
                }

                invoke.Sort(new CallPriorityComparer());

                for (int i = 0; i < invoke.Count; ++i)
                    ((MethodInfo)invoke[i]).Invoke(null, null);

                invoke.Clear();

                World.Load();

                for (a = 0; a < m_Assemblies.Length; ++a)
                {
                    Type[] types = m_Assemblies[a].GetTypes();

                    for (int i = 0; i < types.Length; ++i)
                    {
                        MethodInfo m = types[i].GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public);

                        if (m != null)
                            invoke.Add(m);
                        //m.Invoke( null, null );
                    }
                }

                invoke.Sort(new CallPriorityComparer());

                for (int i = 0; i < invoke.Count; ++i)
                    ((MethodInfo)invoke[i]).Invoke(null, null);

                return true;
            }
            else
            {
                return false;
            }
        }
#endif

        private static Hashtable m_TypeCaches = new Hashtable();
        private static TypeCache m_NullCache;

        public static TypeCache GetTypeCache(Assembly asm)
        {
            if (asm == null)
            {
                if (m_NullCache == null)
                    m_NullCache = new TypeCache(null);

                return m_NullCache;
            }

            TypeCache c = (TypeCache)m_TypeCaches[asm];

            if (c == null)
                m_TypeCaches[asm] = c = new TypeCache(asm);

            return c;
        }

        public static Type FindTypeByFullName(string fullName)
        {
            return FindTypeByFullName(fullName, true);
        }

        public static Type FindTypeByFullName(string fullName, bool ignoreCase)
        {
            Type type = null;

            for (int i = 0; type == null && i < m_Assemblies.Length; ++i)
                type = GetTypeCache(m_Assemblies[i]).GetTypeByFullName(fullName, ignoreCase);

            if (type == null)
                type = GetTypeCache(Core.Assembly).GetTypeByFullName(fullName, ignoreCase);

            return type;
        }

        public static Type FindTypeByName(string name)
        {
            return FindTypeByName(name, true);
        }

        public static Type FindTypeByName(string name, bool ignoreCase)
        {
            Type type = null;

            for (int i = 0; type == null && i < m_Assemblies.Length; ++i)
                type = GetTypeCache(m_Assemblies[i]).GetTypeByName(name, ignoreCase);

            if (type == null)
                type = GetTypeCache(Core.Assembly).GetTypeByName(name, ignoreCase);

            return type;
        }

        private static void EnsureDirectory(string dir)
        {
            string path = Path.Combine(Core.BaseDirectory, dir);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static string[] GetScripts(string type)
        {
            ArrayList list = new ArrayList();

            GetScripts(list, Path.Combine(Core.BaseDirectory, "Scripts"), type);

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

        public Type GetTypeByName(string name, bool ignoreCase)
        {
            return m_Names.Get(name, ignoreCase);
        }

        public Type GetTypeByFullName(string fullName, bool ignoreCase)
        {
            return m_FullNames.Get(fullName, ignoreCase);
        }

        public TypeCache(Assembly asm)
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

                m_Names.Add(type.Name, type);
                m_FullNames.Add(type.FullName, type);

                if (type.IsDefined(typeofTypeAliasAttribute, false))
                {
                    object[] attrs = type.GetCustomAttributes(typeofTypeAliasAttribute, false);

                    if (attrs != null && attrs.Length > 0)
                    {
                        TypeAliasAttribute attr = attrs[0] as TypeAliasAttribute;

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

        public Type Get(string key, bool ignoreCase)
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
            public new bool Equals(object x, object y)
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

            public new bool Equals(object x, object y)
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