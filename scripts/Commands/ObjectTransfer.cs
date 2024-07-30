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

/* Scripts/Commands/ObjectTransfer.cs
 * ChangeLog
 *    1/22/24, Yoar
 *        Saves/Loads entities to/from file.
 *        Does not adjust exported/imported serial refs!
 */

using Server.Diagnostics;
using System;
using System.IO;
using System.Reflection;

namespace Server.Commands
{
    public static class ObjectTransfer
    {
        public static void Export(string filename, IEntity[] ents, out string message)
        {
            message = null;

            LogHelper logger = new LogHelper("ObjectTransfer.log");

            logger.Log(LogType.Text, String.Format("Exporting objects to {0}.", filename));

            FileStream idxStream = null;
            FileStream binStream = null;
            GenericWriter idxWriter = null;
            GenericWriter binWriter = null;

            try
            {
                using (idxStream = new FileStream(GetIdxFilename(filename), FileMode.Create, FileAccess.Write, FileShare.None))
                using (binStream = new FileStream(GetBinFilename(filename), FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    idxWriter = new BinaryFileWriter(idxStream, true);
                    binWriter = new BinaryFileWriter(binStream, true);

                    idxWriter.Write((int)ents.Length);

                    foreach (IEntity ent in ents)
                    {
                        logger.Log(LogType.Text, String.Format("Writing {0:X6} of type {1}.", ent.Serial, ent.GetType().FullName));

                        long start = binWriter.Position;

                        if (ent is Mobile)
                            ((Mobile)ent).Serialize(binWriter);
                        else if (ent is Item)
                            ((Item)ent).Serialize(binWriter);

                        int length = (int)(binWriter.Position - start);

                        idxWriter.Write((string)ent.GetType().FullName);
                        idxWriter.Write((int)ent.Serial);
                        idxWriter.Write((long)start);
                        idxWriter.Write((int)length);
                    }

                    idxWriter.Close();
                    binWriter.Close();
                }

                message = String.Format("Successfully exported {0} objects to {1}.", ents.Length, filename);
            }
            catch (Exception ex)
            {
                message = String.Format("An error occurred: {0}", ex.ToString());
            }
            finally
            {
                if (idxWriter != null)
                    idxWriter.Close();

                if (binWriter != null)
                    binWriter.Close();

                if (idxStream != null)
                    idxStream.Close();

                if (binStream != null)
                    binStream.Close();
            }

            if (message != null)
                logger.Log(LogType.Text, message);

            logger.Finish();
        }

        public static ImportResult[] Import(string filename, out string message)
        {
            message = null;

            LogHelper logger = new LogHelper("ObjectTransfer.log");

            logger.Log(LogType.Text, String.Format("Importing objects from {0}.", filename));

            ImportResult[] imported = new ImportResult[0];
            int count = 0;

            FileStream idxStream = null;
            FileStream binStream = null;
            BinaryReader idxFile = null;
            BinaryReader binFile = null;
            GenericReader idxReader = null;
            GenericReader binReader = null;

            try
            {
                using (idxStream = new FileStream(GetIdxFilename(filename), FileMode.Open, FileAccess.Read))
                using (binStream = new FileStream(GetBinFilename(filename), FileMode.Open, FileAccess.Read))
                using (idxFile = new BinaryReader(idxStream))
                using (binFile = new BinaryReader(binStream))
                {
                    idxReader = new BinaryFileReader(idxFile);
                    binReader = new BinaryFileReader(binFile);

                    imported = new ImportResult[idxReader.ReadInt()];

                    for (int i = 0; i < imported.Length; i++)
                    {
                        string typeName = idxReader.ReadString();
                        Serial oldSerial = idxReader.ReadInt();
                        long start = idxReader.ReadLong();
                        int length = idxReader.ReadInt();

                        Type type = ScriptCompiler.FindTypeByFullName(typeName);

                        Serial serial;

                        if (typeof(Mobile).IsAssignableFrom(type))
                        {
                            serial = Serial.NewMobile;
                        }
                        else if (typeof(Item).IsAssignableFrom(type))
                        {
                            serial = Serial.NewItem;
                        }
                        else
                        {
                            logger.Log(LogType.Text, String.Format("Invalid type: {0}.", typeName));
                            continue;
                        }

                        ConstructorInfo ctor = type.GetConstructor(new Type[] { typeof(Serial) });

                        if (ctor == null)
                        {
                            logger.Log(LogType.Text, String.Format("Type {0} has no serialization constructor.", typeName));
                            continue;
                        }

                        IEntity ent = (IEntity)ctor.Invoke(new object[] { serial });

                        if (ent == null)
                        {
                            logger.Log(LogType.Text, String.Format("Failed to construct object of type: {0}.", typeName));
                            continue;
                        }

                        logger.Log(LogType.Text, String.Format("Constructed {0:X6} of type {1} (old serial: {2:X6}).", serial, typeName, oldSerial));

                        if (ent is Mobile)
                            World.AddMobile((Mobile)ent);
                        else if (ent is Item)
                            World.AddItem((Item)ent);

                        imported[i] = new ImportResult(ent, oldSerial);
                    }

                    idxStream.Seek(4, SeekOrigin.Begin);

                    for (int i = 0; i < imported.Length; i++)
                    {
                        string typeName = idxReader.ReadString();
                        idxReader.ReadInt(); // old serial
                        long start = idxReader.ReadLong();
                        int length = idxReader.ReadInt();

                        IEntity ent = imported[i].Entity;

                        if (ent == null)
                            continue;

                        logger.Log(LogType.Text, String.Format("Deserializing {0:X6}.", ent.Serial));

                        binStream.Seek(start, SeekOrigin.Begin);

                        if (ent is Mobile)
                            ((Mobile)ent).Deserialize(binReader);
                        else if (ent is Item)
                            ((Item)ent).Deserialize(binReader);

                        count++;
                    }
                }

                message = String.Format("Successfully imported {0} objects from {1}.", count, filename);
            }
            catch (Exception ex)
            {
                message = String.Format("An error occurred: {0}", ex.ToString());
            }
            finally
            {
                if (idxFile != null)
                    idxFile.Close();

                if (binFile != null)
                    binFile.Close();

                if (idxStream != null)
                    idxStream.Close();

                if (binStream != null)
                    binStream.Close();
            }

            foreach (ImportResult res in imported)
            {
                if (res.Entity is Mobile)
                {
                    Mobile m = (Mobile)res.Entity;

                    m.UpdateRegion();
                    m.UpdateTotals();

                    m.ClearProperties();
                }
                else if (res.Entity is Item)
                {
                    Item item = (Item)res.Entity;

                    if (item.Parent == null)
                        item.UpdateTotals();

                    item.ClearProperties();
                }
            }

            if (message != null)
                logger.Log(LogType.Text, message);

            logger.Finish();

            return imported;
        }

        private static string GetIdxFilename(string name)
        {
            return String.Concat(name, ".idx");
        }

        private static string GetBinFilename(string name)
        {
            return String.Concat(name, ".bin");
        }

        public struct ImportResult
        {
            public readonly IEntity Entity;
            public readonly Serial OldSerial;

            public ImportResult(IEntity entity, Serial oldSerial)
            {
                Entity = entity;
                OldSerial = oldSerial;
            }
        }
    }
}