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

/* Scripts/Commands/StableTransfer.cs
 * ChangeLog
 *    1/22/24, Yoar
 *        Imports/Exports stable data from/to file
 */

using Server.Diagnostics;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Commands
{
    public static class StableTransfer
    {
        public static void Initialize()
        {
            CommandSystem.Register("StableExport", AccessLevel.Administrator, new CommandEventHandler(StableExport_OnCommand));
            CommandSystem.Register("StableImport", AccessLevel.Administrator, new CommandEventHandler(StableImport_OnCommand));
        }

        [Usage("StableExport <filename>")]
        private static void StableExport_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length < 1)
            {
                e.Mobile.SendMessage("Must specify a filename");
                return;
            }

            string filename = e.GetString(0);

            string message = null;

            LogHelper logger = new LogHelper("StableTransfer.log");

            logger.Log(LogType.Text, string.Format("Exporting stables to {0}.", filename));

            Dictionary<Serial, Serial> ownership = new Dictionary<Serial, Serial>();
            List<IEntity> toExport = new List<IEntity>();

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (AnimalTrainer.Table.ContainsKey(m))
                    foreach (Mobile pet in AnimalTrainer.Table[m])
                    {
                        ownership[pet.Serial] = m.Serial;
                        toExport.Add(pet);
                    }
            }

            FileStream stbStream = null;
            GenericWriter stbWriter = null;

            try
            {
                using (stbStream = new FileStream(GetStbFilename(filename), FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stbWriter = new BinaryFileWriter(stbStream, true);

                    stbWriter.Write((int)ownership.Count);

                    foreach (KeyValuePair<Serial, Serial> kvp in ownership)
                    {
                        stbWriter.Write((int)kvp.Key); // pet serial
                        stbWriter.Write((int)kvp.Value); // owner serial
                    }

                    stbWriter.Close();
                }

                message = string.Format("Successfully exported stable table containing {0} pets to {1}.", ownership.Count, filename);
            }
            catch (Exception ex)
            {
                message = string.Format("An error occurred: {0}", ex.ToString());
            }
            finally
            {
                if (stbWriter != null)
                    stbWriter.Close();

                if (stbStream != null)
                    stbStream.Close();
            }

            if (message != null)
                logger.Log(LogType.Text, message);

            ObjectTransfer.Export(filename, toExport.ToArray(), out message);

            if (message != null)
                logger.Log(LogType.Text, message);

            logger.Finish();
        }

        [Usage("StableImport <filename>")]
        public static void StableImport_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length < 1)
            {
                e.Mobile.SendMessage("Must specify a filename");
                return;
            }

            string filename = e.GetString(0);

            string message = null;

            LogHelper logger = new LogHelper("StableTransfer.log");

            logger.Log(LogType.Text, string.Format("Importing stables from {0}.", filename));

            Dictionary<Serial, Serial> ownership = new Dictionary<Serial, Serial>();

            FileStream stbStream = null;
            BinaryReader stbFile = null;
            GenericReader stbReader = null;

            try
            {
                using (stbStream = new FileStream(GetStbFilename(filename), FileMode.Open, FileAccess.Read))
                using (stbFile = new BinaryReader(stbStream))
                {
                    stbReader = new BinaryFileReader(stbFile);

                    int count = stbReader.ReadInt();

                    for (int i = 0; i < count; i++)
                    {
                        Serial petSerial = stbReader.ReadInt();
                        Serial ownerSerial = stbReader.ReadInt();

                        ownership[petSerial] = ownerSerial;
                    }
                }

                message = string.Format("Successfully imported stable table containing {0} pets to {1}.", ownership.Count, filename);
            }
            catch (Exception ex)
            {
                message = string.Format("An error occurred: {0}", ex.ToString());
            }
            finally
            {
                if (stbFile != null)
                    stbFile.Close();

                if (stbStream != null)
                    stbStream.Close();
            }

            if (message != null)
                logger.Log(LogType.Text, message);

            ObjectTransfer.ImportResult[] imported = ObjectTransfer.Import(filename, out message);

            if (message != null)
                logger.Log(LogType.Text, message);

            foreach (ObjectTransfer.ImportResult res in imported)
            {
                Serial ownerSerial;
                ownership.TryGetValue(res.OldSerial, out ownerSerial);

                Mobile owner = World.FindMobile(ownerSerial);

                if (owner == null)
                {
                    logger.Log(LogType.Text, string.Format("Failed to import stabled pet {0}. Old owner ({1:X6}) is gone.", res.Entity, ownerSerial));
                    res.Entity.Delete();
                    continue;
                }

                if (HasPetWithSerial(owner, res.OldSerial))
                {
                    logger.Log(LogType.Text, string.Format("Failed to import stabled pet {0}. Owner ({1}) already has a pet with this serial ({2:X6}).", res.Entity, owner, res.OldSerial));
                    res.Entity.Delete();
                    continue;
                }

                BaseCreature pet = res.Entity as BaseCreature;

                if (pet == null)
                {
                    logger.Log(LogType.Text, string.Format("Failed to import stabled pet {0}. We are not a creature?!", res.Entity));
                    res.Entity.Delete();
                    continue;
                }

                pet.ControlTarget = null;
                pet.ControlOrder = OrderType.Stay;
                pet.Internalize();
                pet.SetControlMaster(null);
                pet.SummonMaster = null;
                pet.IsAnimalTrainerStabled = true;
                if (AnimalTrainer.Table.ContainsKey(owner))
                {
                    if (!AnimalTrainer.Table[owner].Contains(pet))
                        AnimalTrainer.Table[owner].Add(pet);
                }
                else
                    AnimalTrainer.Table.Add(owner, new List<BaseCreature>() { pet });

                pet.LastStableChargeTime = DateTime.UtcNow;

                logger.Log(LogType.Text, string.Format("Successfully imported stabled pet {0} of owner {1}.", res.Entity, owner));
            }

            logger.Finish();
        }

        private static string GetStbFilename(string name)
        {
            return string.Concat(name, ".stb");
        }

        private static bool HasPetWithSerial(Mobile owner, Serial petSerial)
        {
            foreach (Mobile pet in owner.Followers)
            {
                if (pet.Serial == petSerial)
                    return true;
            }

            if (AnimalTrainer.Table.ContainsKey(owner))
                foreach (Mobile pet in AnimalTrainer.Table[owner])
                {
                    if (pet.Serial == petSerial)
                        return true;
                }

            return false;
        }
    }
}