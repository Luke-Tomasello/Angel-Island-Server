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

using System;

namespace Server.Spells
{
    public class SpellRegistry
    {
        private static Type[] m_Types = new Type[300];
        private static int m_Count;

        public static Type[] Types
        {
            get
            {
                m_Count = -1;
                return m_Types;
            }
        }

        public static int Count
        {
            get
            {
                if (m_Count == -1)
                {
                    m_Count = 0;

                    for (int i = 0; i < 64; ++i)
                    {
                        if (m_Types[i] != null)
                            ++m_Count;
                    }
                }

                return m_Count;
            }
        }

        public static void Register(int spellID, Type type)
        {
            if (spellID < 0 || spellID >= m_Types.Length)
                return;

            if (m_Types[spellID] == null)
                ++m_Count;

            m_Types[spellID] = type;
        }

        private static object[] m_Params = new object[2];

        public static Spell NewSpell(int spellID, Mobile caster, Item scroll)
        {
            if (spellID < 0 || spellID >= m_Types.Length)
                return null;

            Type t = m_Types[spellID];

            if (t == null)
                return null;

            m_Params[0] = caster;
            m_Params[1] = scroll;

            return (Spell)Activator.CreateInstance(t, m_Params);
        }

        private static string[] m_CircleNames = new string[]
            {
                "First",
                "Second",
                "Third",
                "Fourth",
                "Fifth",
                "Sixth",
                "Seventh",
                "Eighth",
                "Necromancy",
                "Chivalry"
            };

        public static Spell NewSpell(string name, Mobile caster, Item scroll)
        {
            for (int i = 0; i < m_CircleNames.Length; ++i)
            {
                Type t = ScriptCompiler.FindTypeByFullName(String.Format("Server.Spells.{0}.{1}", m_CircleNames[i], name));

                if (t != null)
                {
                    m_Params[0] = caster;
                    m_Params[1] = scroll;

                    try
                    {
                        return (Spell)Activator.CreateInstance(t, m_Params);
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }

            return null;
        }
    }
}