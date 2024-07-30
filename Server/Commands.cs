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
using System.Collections;
using System.Globalization;

namespace Server
{
    public delegate void CommandEventHandler(CommandEventArgs e);

    public class CommandEventArgs : EventArgs
    {
        private Mobile m_Mobile;
        private object m_Target;
        private string m_Command, m_ArgString;
        private string[] m_Arguments;
        public bool HasCommand(string command, out string argString)
        {
            for (int ix = 0; ix < m_Arguments.Length; ix++)
                if (m_Arguments[ix].Equals(command, StringComparison.OrdinalIgnoreCase))
                {
                    if (ix <= m_Arguments.Length - 2)
                        argString = m_Arguments[ix + 1];
                    else
                        argString = string.Empty;
                    return true;
                }
            argString = string.Empty;
            return false;
        }
        public Mobile Mobile
        {
            get
            {
                return m_Mobile;
            }
        }

        public object Target
        {
            get
            {
                return m_Target;
            }
        }

        public string Command
        {
            get
            {
                return m_Command;
            }
        }

        public string ArgString
        {
            get
            {
                return m_ArgString;
            }
        }

        public string[] Arguments
        {
            get
            {
                return m_Arguments;
            }
        }

        public int Length
        {
            get
            {
                return m_Arguments.Length;
            }
        }

        public string GetString(int index)
        {
            if (index < 0 || index >= m_Arguments.Length)
                return "";

            return m_Arguments[index];
        }

        public int GetInt32(int index)
        {
            if (index < 0 || index >= m_Arguments.Length)
                return 0;

            return Utility.ToInt32(m_Arguments[index]);
        }

        public bool GetBoolean(int index)
        {
            if (index < 0 || index >= m_Arguments.Length)
                return false;

            return Utility.ToBoolean(m_Arguments[index]);
        }

        public double GetDouble(int index)
        {
            if (index < 0 || index >= m_Arguments.Length)
                return 0.0;

            return Utility.ToDouble(m_Arguments[index]);
        }

        public TimeSpan GetTimeSpan(int index)
        {
            if (index < 0 || index >= m_Arguments.Length)
                return TimeSpan.Zero;

            return Utility.ToTimeSpan(m_Arguments[index]);
        }

        public CommandEventArgs()
            : this(Server.World.GetSystemAcct(), null, "", "", new string[] { "" })
        {

        }
        public CommandEventArgs(Mobile mobile, string command, string argString, string[] arguments)
            : this(mobile, null, command, argString, arguments)
        {

        }

        public CommandEventArgs(Mobile mobile, object target, string command, string argString, string[] arguments)
        {
            m_Mobile = mobile;
            m_Target = target;
            m_Command = command;
            m_ArgString = argString;
            m_Arguments = arguments;
        }
    }

    public class CommandEntry : IComparable
    {
        private string m_Command;
        private CommandEventHandler m_Handler;
        private AccessLevel m_AccessLevel;

        public string Command
        {
            get
            {
                return m_Command;
            }
        }

        public CommandEventHandler Handler
        {
            get
            {
                return m_Handler;
            }
        }

        public AccessLevel AccessLevel
        {
            get
            {
                return m_AccessLevel;
            }
        }

        public CommandEntry(string command, CommandEventHandler handler, AccessLevel accessLevel)
        {
            m_Command = command;
            m_Handler = handler;
            m_AccessLevel = accessLevel;
        }

        public int CompareTo(object obj)
        {
            if (obj == this)
                return 0;
            else if (obj == null)
                return 1;

            CommandEntry e = obj as CommandEntry;

            if (e == null)
                throw new ArgumentException();

            return m_Command.CompareTo(e.m_Command);
        }
    }

    public class CommandSystem
    {
        public static bool CheckAccess(string command, AccessLevel al)
        {
            CommandEntry entry = (CommandEntry)m_Entries[command];

            if (entry != null)
                if (al >= entry.AccessLevel)
                    return true;
            return false;
        }

        private static string m_CommandPrefix = "[";

        public static string CommandPrefix
        {
            get
            {
                return m_CommandPrefix;
            }
            set
            {
                m_CommandPrefix = value;
            }
        }

        public static string[] Split(string value)
        {
            char[] array = value.ToCharArray();
            ArrayList list = new ArrayList();

            int start = 0, end = 0;

            while (start < array.Length)
            {
                char c = array[start];

                if (c == '"')
                {
                    ++start;
                    end = start;

                    while (end < array.Length)
                    {
                        if (array[end] != '"' || array[end - 1] == '\\')
                            ++end;
                        else
                            break;
                    }

                    list.Add(value.Substring(start, end - start));

                    start = end + 2;
                }
                else if (c != ' ')
                {
                    end = start;

                    while (end < array.Length)
                    {
                        if (array[end] != ' ')
                            ++end;
                        else
                            break;
                    }

                    list.Add(value.Substring(start, end - start));

                    start = end + 1;
                }
                else
                {
                    ++start;
                }
            }

            return (string[])list.ToArray(typeof(string));
        }

        private static Hashtable m_Entries;

        public static Hashtable Entries
        {
            get
            {
                return m_Entries;
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
                if (myComparer.Compare(x, y) == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public int GetHashCode(object obj)
            {
                // Compare the hash code for the lowercase versions of the strings.
                return obj.ToString().ToLower().GetHashCode();
            }
        }

        static CommandSystem()
        {
            //m_Entries = new Hashtable(CaseInsensitiveHashCodeProvider.Default, CaseInsensitiveComparer.Default);
            m_Entries = new Hashtable(/*3, .8f, */new myCultureComparer());
        }

        public static void Register(string command, AccessLevel access, CommandEventHandler handler)
        {
            m_Entries[command] = new CommandEntry(command, handler, access);
        }

        private static AccessLevel m_BadCommandIngoreLevel = AccessLevel.Player;

        public static AccessLevel BadCommandIgnoreLevel { get { return m_BadCommandIngoreLevel; } set { m_BadCommandIngoreLevel = value; } }

        public static bool Handle(Mobile from, string text)
        {
            return Handle(from, null, text);
        }

        public static bool Handle(Mobile from, object target, string text)
        {
            if (text.StartsWith(m_CommandPrefix))
            {
                text = text.Substring(m_CommandPrefix.Length);

                int indexOf = text.IndexOf(' ');

                string command;
                string[] args;
                string argString;

                if (indexOf >= 0)
                {
                    argString = text.Substring(indexOf + 1);

                    command = text.Substring(0, indexOf);
                    args = Split(argString);
                }
                else
                {
                    argString = "";
                    command = text.ToLower();
                    args = new string[0];
                }

                CommandEntry entry = (CommandEntry)m_Entries[command];

                if (entry != null)
                {
                    if (from.AccessLevel >= entry.AccessLevel)
                    {
                        if (entry.Handler != null)
                        {
                            CommandEventArgs e = new CommandEventArgs(from, target, command, argString, args);
                            entry.Handler(e);
                            EventSink.InvokeCommand(e);
                        }
                    }
                    else
                    {
                        if (from.AccessLevel <= m_BadCommandIngoreLevel)
                            return false;

                        from.SendMessage("You do not have access to that command.");
                    }
                }
                else
                {
                    if (from.AccessLevel <= m_BadCommandIngoreLevel)
                        return false;

                    from.SendMessage("That is not a valid command.");
                }

                return true;
            }

            return false;
        }
    }
}