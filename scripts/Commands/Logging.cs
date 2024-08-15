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

/*	Scripts/Commands/Logging.cs
 *	8/19/21, Adam,
 *	    Add the shard name into the log tree so that we know if a staff is doing something 'wrong' on the production
 *	    server vs, say, Test Center
 *	2/23/10, Adam
 *		decorate the name to avoid the "con", "prn" file-device exploit.
 *			ie., name = "_" + name;
 *	11/3/06, Adam
 *		Add Open() and Close() commands to allow remote control of the logging.
 *  10/22/06, Rhiannon
 *		Added spellcast logging (for staff spellcasting)
 *	9/22/06, Adam
 *		Add Mobile 'Type' information to output
 *	5/3/06, weaver
 *		Added LogChangeClient to log logging in and logging out.
 */

using Server.Accounting;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Commands
{
    public class CommandLogging
    {
        private static StreamWriter m_Output;
        private static bool m_Enabled = true;

        public static bool Enabled { get { return m_Enabled; } set { m_Enabled = value; } }

        public static StreamWriter Output { get { return m_Output; } }

        public static void Initialize()
        {
            EventSink.Command += new CommandEventHandler(EventSink_Command);

            string directory = "Logs/Commands/" + Core.Server;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            Open();
        }

        public static object Format(object o)
        {
            if (o is Mobile pm && pm.Player)
            {
                string where = Core.Server + ", " + ((pm.Map != null) ? pm.Map.ToString() : "null map");
                string acct = (pm.Account != null) ? ((Account)pm.Account).Username : "(no account)";
                string result = string.Format($"{where}[({pm}, acct: '{acct}') {pm.Location}]");
                return result;
            }
            else if (o is Mobile m)
            {
                string where = Core.Server + ", " + ((m.Map != null) ? m.Map.ToString() : "null map");
                string what = m.GetType().Name;
                string result = string.Format($"{where}[({m}, what: '{what}') {m.Location}]");
                return result;
            }
            else if (o is Item item)
            {
                return string.Format("0x{0:X} ({1})", item.Serial.Value, item.GetType().Name);
            }

            return o;
        }

        public static void Open()
        {
            try
            {
                if (m_Output != null)
                    Close();

                string directory = "Logs/Commands/" + Core.Server;
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                m_Output = new StreamWriter(Path.Combine(directory, string.Format("{0}.log", DateTime.UtcNow.ToLongDateString())), true);
                m_Output.AutoFlush = true;
                m_Output.WriteLine("##############################");
                m_Output.WriteLine("Log started on {0}", DateTime.UtcNow);
                m_Output.WriteLine();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static void Close()
        {
            if (m_Output == null)
                return;
            try
            {
                m_Output.Close();
                m_Output = null;
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static void WriteLine(Mobile from, string format, params object[] args)
        {
            WriteLine(from, string.Format(format, args));
        }
        public class LineInfo
        {
            public Mobile m_From;
            public string m_Text;
            public LineInfo(Mobile from, string text)
            {
                m_From = from;
                m_Text = text;
            }
        }
        private static Queue<LineInfo> LineBuffer = new();
        public static void WriteLine(Mobile from, string text)
        {
            if (!m_Enabled)
                return;

            if (from == null)
                from = World.GetSystemAcct();

            if (text != null)
                LineBuffer.Enqueue(new LineInfo(from, text));

            if (m_Output == null)
            {   // when rotating logs, there is a gap between when the stream is closed and reopened. We therefore buffer 'offline text' for later output
                //  This is passive as it's not flushed until the next WriteLine comes in
                Utility.ConsoleWriteLine(string.Format($"Logger offline, queuing: {text}"), ConsoleColor.Yellow);
                return;
            }

            if (LineBuffer.Count > 1)
                Utility.ConsoleWriteLine(string.Format($"Logger back online. Dequeuing {LineBuffer.Count} lines"), ConsoleColor.Yellow);

            while (LineBuffer.Count > 0)
            {
                LineInfo lb = LineBuffer.Dequeue();
                from = lb.m_From;
                text = lb.m_Text;

                try
                {
                    m_Output.WriteLine("{0}: {1}: {2}", DateTime.UtcNow, from.NetState != null ? from.NetState.ToString() : "(null)", text);

                    string path = Core.BaseDirectory;

                    AppendPath(ref path, "Logs");
                    AppendPath(ref path, "Commands");
                    AppendPath(ref path, from.AccessLevel.ToString());
                    path = Path.Combine(path, string.Format("{0}.log", Utility.ValidFileName(from.ToString())));

                    using (StreamWriter sw = new StreamWriter(path, true))
                    {
                        if (sw != null)
                            sw.WriteLine("{0}: {1}: {2}", DateTime.UtcNow, from.NetState, text);
                    }
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    Console.WriteLine(new System.Diagnostics.StackTrace().ToString());
                }
            }
        }

        private static char[] m_NotSafe = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        public static void AppendPath(ref string path, string toAppend)
        {
            path = Path.Combine(path, toAppend);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static string Safe(string ip)
        {
            if (ip == null)
                return "null";

            ip = ip.Trim();

            if (ip.Length == 0)
                return "empty";

            bool isSafe = true;

            for (int i = 0; isSafe && i < m_NotSafe.Length; ++i)
                isSafe = (ip.IndexOf(m_NotSafe[i]) == -1);

            if (isSafe)
                return ip;

            System.Text.StringBuilder sb = new System.Text.StringBuilder(ip);

            for (int i = 0; i < m_NotSafe.Length; ++i)
                sb.Replace(m_NotSafe[i], '_');

            return sb.ToString();
        }

        public static void EventSink_Command(CommandEventArgs e)
        {
            WriteLine(e.Mobile, "{0} {1} used command '{2} {3}'", e.Mobile.AccessLevel, Format(e.Mobile), e.Command, e.ArgString);
        }

        public static void LogChangeProperty(Mobile from, object o, string name, string value)
        {   // 5/11/2024, Adam: Add the 'was' value so we can see what it is being changed from/to
            string was = Properties.GetOnlyValue(null, o, name) ?? "??";
            WriteLine(from, "{0} {1} set property '{2}' of {3} from '{4}' to '{5}'", from.AccessLevel, Format(from), Format(o), was, name, value);

            #region Logging
            // write to the spawner's properties for quick access
            if (o is Item item)
                item.LogChange(from, name, was, value);
            #endregion Logging
        }

        // wea: added to log players logging in & out of the game
        public static void LogChangeClient(Mobile from, bool loggedin)
        {
            WriteLine(from, "{0} {1} logged " + (loggedin ? "in" : "out"), from.AccessLevel, Format(from));
        }

        // Log spellcasting (called when a staffmember casts a spell)
        public static void LogCastSpell(Mobile from, string spell)
        {
            WriteLine(from, "{0} {1} cast spell '{2}'", from.AccessLevel, Format(from), spell);
        }
    }
}