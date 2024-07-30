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

/* misc/SpawnEngine.cs
 * CHANGELOG:
 *  3/26/2024, Adam
 *      Percolate construction errors up to the controller for display to staff.
 *      These will often be AccessLevel errors (setting props you do not have sufficient rights to set.)
 *  6/2/23, Yoar
 *      Bumped up SpawnEngine access level from GM to Seer
 * 	3/9/23, Yoar
 * 		Added short-circuit evaluation for the case in which we don't have
 * 		any set properties. This way, we don't evaluate the expensive
 * 		'type.GetProperties' operation.
 * 	3/7/23, Yoar
 * 		Initial version. Spawn items/mobiles from a single string.
 * 		Supports constructor arguments and set properties.
 */

using Server.Commands;
using System;
using System.Reflection;

namespace Server
{
    public static class SpawnEngine
    {
        private const AccessLevel m_AccessLevel = AccessLevel.Seer; // access level of the SpawnEngine

        public static bool Validate(string spawnName)
        {
            return typeof(IEntity).IsAssignableFrom(ParseType(spawnName));
        }

        public static T Build<T>(string spawnName, ref string reason) where T : IEntity
        {
            IEntity spawned = Build(spawnName, ref reason);

            if (spawned is T)
                return (T)spawned;

            if (spawned != null)
                spawned.Delete();

            return default(T);
        }

        public static IEntity Build(string spawnName, ref string reason)
        {
            return BuildInternal(spawnName, ref reason);
        }

        public static IEntity BuildInternal(string spawnName, ref string reason)
        {
            if (String.IsNullOrEmpty(spawnName))
            {
                reason = "Invalid spawn name";
                return null;
            }

            // 1. Split type name from arguments

            int argsIndex = spawnName.IndexOf(' ');

            string typeName;

            if (argsIndex != -1)
                typeName = spawnName.Substring(0, argsIndex);
            else
                typeName = spawnName;

            // 2. Parse and check type name

            Type type = ScriptCompiler.FindTypeByName(typeName);

            if (!typeof(IEntity).IsAssignableFrom(type))
            {
                reason = "Invalid type";
                return null;
            }

            // 3. Extract arguments

            string[] args;

            if (argsIndex != -1 && argsIndex + 1 < spawnName.Length)
                args = CommandSystem.Split(spawnName.Substring(argsIndex + 1));
            else
                args = new string[0];

            // 4. Count constructor arguments

            int setIndex = -1;

            for (int i = 0; i < args.Length; i++)
            {
                if (Insensitive.Equals(args[i], "set"))
                {
                    setIndex = i;
                    break;
                }
            }

            int paramCount;

            if (setIndex == -1)
                paramCount = args.Length;
            else
                paramCount = setIndex;

            // 5. Find matching constructor

            ConstructorInfo ctor = null;
            ParameterInfo[] paramList = null;

            foreach (ConstructorInfo c in type.GetConstructors())
            {
                paramList = c.GetParameters();

                if (paramList.Length == paramCount && Add.IsConstructable(c))
                {
                    ctor = c;
                    break;
                }
            }

            if (ctor == null)
            {
                reason = "No valid constructor";
                return null;
            }

            // 6. Parse constructor arguments

            string[] paramString;

            if (paramCount == args.Length)
            {
                paramString = args;
            }
            else
            {
                paramString = new string[paramCount];

                Array.Copy(args, 0, paramString, 0, paramCount);
            }

            object[] paramValues = Add.ParseValues(paramList, paramString);

            if (paramValues == null)
            {
                reason = "Failed to parse constructor arguments";
                return null;
            }

            // 7. Construct spawned object

            IEntity spawned;

            try
            {
                spawned = (IEntity)ctor.Invoke(paramValues);
            }
            catch (ApplicationException ae)
            {   // 5/13/2024, Adam: added to allow for specific construction messages
                if (ae.InnerException != null)
                    reason = ae.InnerException.Message;
                else
                    reason = ae.Message;
                return null;
            }
            catch
            {
                reason = "Failed to construct object";
                return null;
            }

            if (setIndex == -1)
            {
                reason = null;
                return spawned;
            }

            // 8. Set properties

            PropertyInfo[] allProps = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            for (int i = setIndex + 1; i < args.Length - 1; i += 2)
            {
                string pname = args[i];
                string value = args[i + 1];

                foreach (PropertyInfo prop in allProps)
                {
                    if (Insensitive.Equals(prop.Name, pname))
                    {
                        // do not allow AccessLevel to be set
                        if (prop.PropertyType == typeof(AccessLevel))
                            continue;

                        if (!prop.CanWrite)
                            continue;

                        CommandPropertyAttribute attr = Properties.GetCPA(prop);

                        if (attr == null || attr.WriteLevel > m_AccessLevel)
                        {
                            reason = string.Format("You do not have sufficient rights to set: {0}", prop.Name);
                            continue;
                        }

                        object toSet = null;
                        string result = Properties.ConstructFromString(prop.PropertyType, spawned, value, ref toSet);

                        if (result != null)
                            continue;

                        try
                        {
                            prop.SetValue(spawned, toSet, null);
                        }
                        catch
                        {
                        }

                        break;
                    }
                }
            }

            return spawned;
        }

        public static Type ParseType(string spawnName)
        {
            if (String.IsNullOrEmpty(spawnName))
                return null;

            int argsIndex = spawnName.IndexOf(' ');

            string typeName;

            if (argsIndex != -1)
                typeName = spawnName.Substring(0, argsIndex);
            else
                typeName = spawnName;

            return ScriptCompiler.FindTypeByName(typeName);
        }
    }
}