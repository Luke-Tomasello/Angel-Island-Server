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

/* Scripts\Engines\QuestCodes\QuestCodes.cs
 * CHANGELOG:
 *	2/27/23, Adam
 *		first time check in
 */

using Server.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Engines.QuestCodes
{
    public static class QuestCodes
    {
        #region Global Interfaces
        public static void Initialize()
        {
            CommandSystem.Register("QuestCodeAlloc", AccessLevel.Seer, new CommandEventHandler(QuestCodeAlloc_OnCommand));
            CommandSystem.Register("QuestCodeLookup", AccessLevel.Seer, new CommandEventHandler(QuestCodeLookup_OnCommand));
        }
        public static void QuestCodeAlloc_OnCommand(CommandEventArgs e)
        {
            if (string.IsNullOrEmpty(e.ArgString))
            {
                e.Mobile.SendMessage("Usage: [QuestCodeAlloc <quest name>");
                return;
            }
            Alloc(e.Mobile, e.ArgString);
        }
        public static void QuestCodeLookup_OnCommand(CommandEventArgs e)
        {
            if (string.IsNullOrEmpty(e.ArgString))
            {
                e.Mobile.SendMessage("Usage: [QuestCodeLookup <quest name>");
                return;
            }

            ushort code = QuestCode(e.Mobile, e.ArgString);
            if (code < QuestCodeMinValue)
            {
                string text = string.Format("QuestCode for {0} does not exist.", QuestName(e.ArgString));
                e.Mobile.SendMessage(text);
            }
            else
            {
                string text = string.Format("The QuestCode for '{0}' is {1}.", QuestName(e.ArgString), code);
                e.Mobile.SendMessage(text);
            }
        }
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }
        #endregion Global Interfaces
        private static Dictionary<string, ushort> m_database = new();
        public static Dictionary<string, ushort> Database { get { return m_database; } }
        public const ushort QuestCodeMinValue = 101;
        private static string QuestName(string questName)
        {
            if (string.IsNullOrEmpty(questName)) return string.Empty;
            return questName.ToLower().Replace(" ", "");
        }
        public static ushort QuestCode(Mobile from, string questName)
        {   // values < QuestCodeMinValue are an error
            if (string.IsNullOrEmpty(questName)) return 0;
            questName = QuestName(questName);
            if (m_database.ContainsKey(questName))
                return m_database[questName];

            return 0;
        }
        public static bool CheckQuestCode(ushort code)
        {
            foreach (var kvp in m_database)
                if (code == kvp.Value)
                    return true;

            return false;
        }
        public static ushort Alloc(Mobile from, string questName)
        {
            if (string.IsNullOrEmpty(questName)) return 0;
            questName = QuestName(questName);
            LogHelper logger = null;
            string text = string.Empty;
            if (m_database.ContainsKey(questName))
            {
                text = string.Format("Error: QuestCode for {0} already allocated.", questName);
                from.SendMessage(text);
                logger = new LogHelper("QuestCodes.log", false);
                logger.Log(LogType.Text, text);
                logger.Finish();
                return 0;
            }
            else
            {
                bool found = false;
                ushort check = 0;
                for (int ix = 1; ix < 100 && found == false; ix++)
                {
                    check = (ushort)Utility.RandomMinMax((int)QuestCodeMinValue, (int)ushort.MaxValue);
                    foreach (var kvp in m_database)
                        if (kvp.Value == check)
                        {
                            found = true;
                            continue;
                        }
                }

                if (found == true)
                {
                    text = string.Format("Error: Unable to allocate a QuestCode for {0}.", questName);
                    from.SendMessage(text);
                    logger = new LogHelper("QuestCodes.log", false);
                    logger.Log(LogType.Text, text);
                    logger.Finish();
                    return 0;
                }

                m_database.Add(questName, check);
                text = string.Format("New QuestCode {0} allocated for {1}.", check, questName);
                from.SendMessage(text);
                logger = new LogHelper("QuestCodes.log", false);
                logger.Log(LogType.Text, text);
                logger.Finish();
                return check;
            }
        }

        #region Serialization
        public static void Load()
        {
            if (!File.Exists("Saves/QuestCodes.bin"))
                return;

            Console.WriteLine("QuestCodes Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/QuestCodes.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                m_database.Add(reader.ReadString(), reader.ReadUShort());
                            }
                            break;
                        }
                    default:
                        {
                            reader.Close();
                            throw new Exception("Invalid QuestCodes.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error reading Saves/QuestCodes.bin, using default values:");
                Utility.PopColor();
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("QuestCodes Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/QuestCodes.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            writer.Write(m_database.Count);
                            foreach (var kvp in m_database)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value);
                            }
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Saves/QuestCodes.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion Serialization
    }
}