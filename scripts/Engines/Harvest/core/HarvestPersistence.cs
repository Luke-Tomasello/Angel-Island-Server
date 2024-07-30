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

/* Engines/Harvest/Core/HarvestPersistence.cs
 * CHANGELOG:
 *  11/10/21, Yoar
 *      Added File.Exists checks to avoid errors on WorldLoad using a fresh save.
 *  11/10/21, Yoar
 *      Added HarvestPersistence: Serializes harvest banks.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Server.Engines.Harvest
{
    public static class HarvestPersistence
    {
        public static bool Enabled = true;
        public static string Folder = @"Saves\HarvestPersistence";
        public static void Configure()
        {
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
        }

        private static void Save(WorldSaveEventArgs e)
        {
            if (!Enabled)
                return;

            Console.WriteLine("HarvestPersistence Saving...");

            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);

            List<Type> types = new List<Type>();

            WriteBanks(Mining.System.OreAndStone, Map.Felucca, "ore", types);
            WriteBanks(Lumberjacking.System.Definition, Map.Felucca, "lumber", types);

            WriteTypes(types);
        }

        private static void Load()
        {
            if (!Enabled)
                return;

            Console.WriteLine("HarvestPersistence Loading...");

            Type[] types = ReadTypes();

            ReadBanks(Mining.System.OreAndStone, Map.Felucca, "ore", types);
            ReadBanks(Lumberjacking.System.Definition, Map.Felucca, "lumber", types);
        }

        private static string GetFileName(Map map, string id)
        {
            return Path.Combine(Folder, String.Format("{0}_{1}.bin", id, map));
        }

        private static void WriteBanks(HarvestDefinition def, Map map, string id, List<Type> types)
        {
            Hashtable banks = def.Banks[map] as Hashtable;

            try
            {
                BinaryFileWriter writer = new BinaryFileWriter(GetFileName(map, id), true);

                writer.Write((int)0); // version

                writer.Write((int)def.BankWidth);
                writer.Write((int)def.BankHeight);

                if (banks == null)
                {
                    writer.Write((int)0);
                }
                else
                {
                    writer.Write((int)banks.Count);

                    foreach (DictionaryEntry e in banks)
                    {
                        Point2D p = (Point2D)e.Key;
                        HarvestBank bank = (HarvestBank)e.Value;

                        writer.Write((int)p.X);
                        writer.Write((int)p.Y);

                        HarvestVein vein = bank.DefaultVein;

                        Type type = null;

                        if (vein != null && vein.PrimaryResource.Types.Length != 0)
                            type = vein.PrimaryResource.Types[0];

                        if (type != null)
                        {
                            writer.Write((bool)true);

                            WriteType(writer, type, types);
                        }
                        else
                        {
                            writer.Write((bool)false);
                        }
                    }
                }

                writer.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("HarvestPersistence Error: {0}", e);
            }
        }

        private static void ReadBanks(HarvestDefinition def, Map map, string id, Type[] types)
        {
            try
            {
                string fileName = GetFileName(map, id);

                if (!File.Exists(fileName))
                    return; // nothing to read

                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read)));

                int version = reader.ReadInt();

                int bankWidth = reader.ReadInt();
                int bankHeight = reader.ReadInt();

                if (def.BankWidth != bankWidth || def.BankHeight != bankHeight)
                {
                    throw new Exception("Bank size mismatch.");
                }

                int count = reader.ReadInt();

                for (int i = 0; i < count; i++)
                {
                    int x = reader.ReadInt();
                    int y = reader.ReadInt();

                    if (reader.ReadBool())
                    {
                        Type type = ReadType(reader, types);

                        HarvestVein vein = null;

                        for (int j = 0; j < def.Veins.Length; j++)
                        {
                            HarvestVein check = def.Veins[j];

                            if (check != null && check.PrimaryResource.Types.Length != 0 && check.PrimaryResource.Types[0] == type)
                                vein = check;
                        }

                        if (vein != null)
                        {
                            Hashtable banks = def.Banks[map] as Hashtable;

                            if (banks == null)
                                def.Banks[map] = banks = new Hashtable();

                            banks[new Point2D(x, y)] = new HarvestBank(def, vein);
                        }
                    }
                }

                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("HarvestPersistence Error: {0}", e);
            }
        }

        private static void WriteType(BinaryFileWriter writer, Type type, List<Type> types)
        {
            int index = types.IndexOf(type);

            if (index == -1)
            {
                index = types.Count;

                types.Add(type);
            }

            writer.Write((int)index);
        }

        private static Type ReadType(BinaryFileReader reader, Type[] types)
        {
            return types[reader.ReadInt()];
        }

        private static void WriteTypes(List<Type> types)
        {
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter(Path.Combine(Folder, "types.bin"), true);

                writer.Write((int)0); // version

                writer.Write((int)types.Count);

                for (int i = 0; i < types.Count; i++)
                    writer.Write((string)types[i].FullName);

                writer.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("HarvestPersistence Error: {0}", e);
            }
        }

        private static Type[] ReadTypes()
        {
            Type[] types = new Type[0];

            try
            {
                string fileName = Path.Combine(Folder, "types.bin");

                if (!File.Exists(fileName))
                    return types; // nothing to read

                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read)));

                int version = reader.ReadInt();

                types = new Type[reader.ReadInt()];

                for (int i = 0; i < types.Length; i++)
                    types[i] = ScriptCompiler.FindTypeByFullName(reader.ReadString());

                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("HarvestPersistence Error: {0}", e);
            }

            return types;
        }
    }
}