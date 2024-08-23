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

/* Scripts\Engines\RecycleBin\RecycleBin.cs
 * CHANGELOG:
 *  8/12/2023, Adam
 *      Handle reading back a null container
 *      Actually deleted the container if older than 10 days
 *  2/18/22, Adam
 *      Log and Delete items that cannot be recycled.
 *	10/2/21, Adam
 *		first time check in
 */

using Server.Diagnostics;
using Server.Items;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Items
{
    public class RecycleBin : StrongBackpack
    {
        public RecycleBin()
            : base()
        {
            this.MaxItems = 0;                              // unlimited
            this.Movable = false;                           // don't decay / cleanup
            this.IsIntMapStorage = true;                    // don't decay / cleanup
        }
        public RecycleBin(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}

namespace Server.Engines.RecycleBin
{
    public static class RecycleBin
    {
        private static Dictionary<Serial, Container> m_database = new Dictionary<Serial, Container>();
        public static Dictionary<Serial, Container> Database { get { return m_database; } }
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }

        public static void Add(Item item, Container from, string logdata)
        {
            if (item == null) return;
            if (m_database.ContainsKey(from.Serial))
            {
                LogHelper logger = null;
                if (TryDropItem(item, m_database[from.Serial]) == false)
                {
                    string text = string.Format("Unable to store more items in this recycle bin: {0}. Too bad.", m_database[from.Serial].Serial);
                    Console.WriteLine(text);
                    logger = new LogHelper("RecycleBin.log", false);
                    logger.Log(LogType.Text, logdata);
                    logger.Log(LogType.Text, text);
                    logger.Finish();
                    item.Delete();
                    return;
                }

                // Log it
                logger = new LogHelper("RecycleBin.log", false);
                logger.Log(LogType.Text, logdata);
                logger.Finish();
            }
            else
            {
                Items.RecycleBin cont = new Items.RecycleBin();     // RecycleBin holds 1600 stones. If the user overflows that much, too bad.
                m_database.Add(from.Serial, cont);
                Add(item, from, logdata);
            }
        }

        public static bool TryDropItem(Item item, Container to)
        {
            if (to.TotalWeight + item.Weight >= to.MaxWeight)
                return false;

            to.AddItem(item);
            return true;
        }

        #region Serialization
        public static void Load()
        {
            if (!File.Exists("Saves/RecycleBin.bin"))
                return;

            Console.WriteLine("Recycle Bin Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/RecycleBin.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 2:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Serial s = (Serial)reader.ReadInt();

                                StrongBackpack cont = (StrongBackpack)reader.ReadItem();
                                // we will delete this container after 10 days since creation.
                                //  we understand they may have added things since then. 
                                if (cont != null && DateTime.UtcNow < cont.Created + TimeSpan.FromDays(10))
                                    m_database.Add(s, cont);
                                else
                                {
                                    if (cont != null)
                                    {
                                        string logdata = string.Format("Emptying recycle bin: {0} for container: {1}", cont.Serial, s);
                                        Utility.Monitor.WriteLine(logdata, ConsoleColor.Yellow);
                                        LogHelper logger = new LogHelper("RecycleBin.log", false);
                                        logger.Log(LogType.Text, logdata);
                                        logger.Finish();
                                        cont.Delete();
                                    }
                                    else
                                    {
                                        string logdata = string.Format("Null bin entry: for container: {1}", s);
                                        Utility.Monitor.WriteLine(logdata, ConsoleColor.Red);
                                        LogHelper logger = new LogHelper("RecycleBin.log", false);
                                        logger.Log(LogType.Text, logdata);
                                        logger.Finish();
                                    }
                                }
                            }
                            break;
                        }
                    case 1:
                        {
                            // initial empty database
                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid RecycleBin.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading Saves/RecycleBin.bin, using default values:", ConsoleColor.Red);
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("RecycleBin Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/RecycleBin.bin", true);
                int version = 2;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 2:
                        {
                            writer.Write(m_database.Count);
                            foreach (KeyValuePair<Serial, Container> kvp in m_database)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value);
                            }
                            break;
                        }
                    case 1:
                        {
                            // initial empty database
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Saves/RecycleBin.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion Serialization
    }
}