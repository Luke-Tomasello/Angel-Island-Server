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

using Server.Diagnostics;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Engines
{
    public static class IDSRegressionTest
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Ids", AccessLevel.Administrator, new CommandEventHandler(IDS_OnCommand));
        }
        private static readonly object mutex = new object();
        private static Queue<string> m_regressionText = new Queue<string>();
        private static bool m_regressionRecorderEnabled = false;
        public static bool RegressionRecorderEnabled
        {
            get
            {
                lock (mutex)
                {
                    return m_regressionRecorderEnabled;
                }
            }
        }
        private static LogHelper m_logger = null;
        public static LogHelper Logger
        {
            get
            {
                lock (mutex)
                {
                    return m_logger;
                }
            }
        }
        private static ProcessTimer m_ProcessTimer = null;
        private class ProcessTimer : Timer
        {
            Mobile m_mobile;
            Mobile m_target;
            public ProcessTimer(Mobile m, Mobile target)
                : base(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(5.0))
            {
                m_mobile = m;
                m_target = target;
                Priority = TimerPriority.FiftyMS;
                System.Console.WriteLine("Regression timer started.");
                Start();
            }

            protected override void OnTick()
            {
                try
                {
                    if (m_regressionText.Count == 0)
                    {
                        m_mobile.SendMessage("done.");
                        // cleanup timer
                        m_ProcessTimer.Stop();
                        m_ProcessTimer.Running = false;
                        m_ProcessTimer = null;
                        // cleanup log file
                        lock (mutex)
                        {
                            m_logger.Finish();
                        }
                        // cleanup state
                        lock (mutex)
                        {
                            m_regressionRecorderEnabled = false;
                        }
                        return;
                    }

                    while (m_regressionText.Count > 0)
                    {
                        string text = m_regressionText.Dequeue();
                        text = text.Trim();

                        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
                        {
                            lock (mutex)
                            {
                                m_logger.Log(Environment.NewLine);
                            }
                            //Console.WriteLine();
                            Utility.ConsoleWriteLine("", ConsoleColor.White);
                        }
                        else if (text[0] == '#')
                        {   // just output the comment
                            lock (mutex)
                            {
                                m_logger.Log(text);
                            }
                            //Console.WriteLine(text);
                            Utility.ConsoleWriteLine(text, ConsoleColor.White);
                        }
                        else
                        {   // ok! We've got a command string

                            // remove comments
                            int index = text.IndexOf("//");
                            string head = text;
                            if (index >= 0) head = head.Substring(0, index).Trim();
                            // substitute this placeholder for the NPCs name.
                            head = head.Replace("<npc name>", m_target.Name);
                            m_mobile.Say(head);    // this is just for me, the mobile does not process this
                            m_target.OnSpeech(new SpeechEventArgs(m_mobile, head, Server.Network.MessageType.Regular, 0x3B2, new int[0], true));
                            lock (mutex)
                            {
                                m_logger.Log(text);
                            }
                            //Console.WriteLine(text);
                            Utility.ConsoleWriteLine(text, ConsoleColor.White);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    System.Console.WriteLine("Exception Caught in Regression timer: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                }
            }
        }
        [Usage("IDS")]
        [Description("Intelligent Dialogue System regression tests.")]
        public static void IDS_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 1 && e.Arguments[0].ToLower() == "clear")
            {
                IntelligentDialogue.Memory.Clear();
                e.Mobile.SendMessage("IDS Memory cleared.");
                return;
            }

            if (m_ProcessTimer != null)
            {
                e.Mobile.SendMessage("Stopping IDS regression tests.");
                m_regressionText.Clear();
                m_ProcessTimer.Stop();
                m_ProcessTimer.Running = false;
                m_ProcessTimer = null;
                // cleanup log file
                lock (mutex)
                {
                    m_logger.Finish();
                }
                // cleanup state
                lock (mutex)
                {
                    m_regressionRecorderEnabled = false;
                }
            }
            else
            {
                e.Mobile.SendMessage("Starting IDS regression tests...");
                Mobile target = null;
                if (NearBy(e.Mobile, ref target, typeof(BaseGuard)))
                {
                    string filename = Path.Combine(Core.DataDirectory, "Intelligent Dialogue Regression Tests.txt");
                    foreach (string line in File.ReadAllLines(filename))
                        m_regressionText.Enqueue(line);

                    lock (mutex)
                    {
                        m_logger = new LogHelper("ids regression results.log", true);
                    }
                    m_ProcessTimer = new ProcessTimer(e.Mobile, target);
                    lock (mutex)
                    {
                        m_regressionRecorderEnabled = true;
                    }
                }
                else
                {
                    e.Mobile.SendMessage("There are no guards nearby.");
                }
            }
        }
        private static bool NearBy(Mobile m, ref Mobile target, Type t)
        {
            int range = 13;
            if (m is BaseCreature bc)
                range = bc.RangePerception;

            IPooledEnumerable eable = m.Map.GetMobilesInRange(m.Location, range);
            foreach (Mobile found in eable)
                if (found != null)
                    if (t.IsAssignableFrom(found.GetType()))
                    {
                        eable.Free();
                        target = found;
                        return true;
                    }

            eable.Free();
            target = null;
            return false;
        }
    }
}