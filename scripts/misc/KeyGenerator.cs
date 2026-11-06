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

/* Scripts\misc\KeyGenerator.cs
 * ChangeLog
 *  12/10/2025, Adam
 *  First time check in.
 *  Purpose : Monitor and allocate unique IDs for starting loot packages.
 */

using Server;
using System;
using System.IO;

namespace Scripts.Misc
{
    internal class KeyGenerator
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
        }

        private static int _next = 0;  // starts at 0 so first allocation is 1
        private static readonly object _lock = new object();

        public static int Alloc()
        {
            lock (_lock)
            {
                _next++;

                if (_next <= 0) // overflow or explicit reset
                    _next = 1;

                return _next;
            }
        }

        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Starting Key Generator Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/KeyGenerator.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            writer.Write(_next);
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Saves/KeyGenerator.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        public static void Load()
        {
            if (File.Exists("Saves/KeyGenerator.bin"))
                Console.WriteLine("Starting Key Generator Loading...");
            else
            {
                Console.WriteLine("No Key Generator _next to Load.");
                return;
            }
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/KeyGenerator.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            _next = reader.ReadInt();
                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid KeyGenerator.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading Saves/KeyGenerator.bin, using default values:", ConsoleColor.Red);
            }
        }
    }
}
