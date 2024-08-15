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

/* Script/Misc/AutoSave.cs
 * CHANGELOG:
 *  8/17/22, Adam
 *      Make Backup() public so we can call it from cron
 *  2/28/22, Adam,
 *  Moving Auto save to CronTasks.cs
 *      We keep the framework 'class AutoSave' as it still has useful functionality/checks
 *  1/18/22, Adam
 *      Disable auto backup deletion of old saves
 *          Our Google backup (server-side) allows the files to get deleted, but not the directories.
 *          kinda ugly, need to find a better way. For now I'll just delete old backups manually
 *  11/25/21, Adam
 *      Replace the old RunUO backup system ("Third Backup", "Second Backup","Most Recent") with a simplier system that plays nice with 
 *      file sync-to-cloud mechanisms.
 *      The new model simply moves (or copies on error) the 'saves' folder to Backups\Automatic\<date-time-stamp>
 *      The backup system then records that file in a FIFO queue. This queue is allowed to get N deep, where N is controled by
 *      CoreAI.BackupCount. When depth > CoreAI.BackupCount, file names are dequeued and the file deleted (asynchronous file delete.)
 *	5/15/10, adam
 *		Automatically create a 1 backup each day of the form: Archive-17May10
 *	8/22/07, Adam
 *		We split the backup and save into separate catch blocks because we still want to save even if we cannot backup
 *	9/1/06, Adam
 *		Update the previous fix so that Saves are only skipped when they are the hourly 'passive' saves.
 *		Maintenance restarts, and other proactive Saves will now go through as normal.
 *	8/13/06, Adam
 *		Add a check to Save() to see if "ntbackup.exe" is running. If so, do not do a backup at this time.
 *		We added this because we will see a 24 minute backup when the Shadow Copy process is running
 *			when we would normally see a 12 second save.
 *		We also print a console message to note the fact.
 * 	12/01/05 Pix
 *		Added WorldSaveFrequency to CoreAI -- and logic to change dynamically.
 *	9/12/05, Adam
 *		Remove time-stamp from directory name so we can backup the "Most Recent"
 *			without trouble.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/2/04 smerX
 *		Changed m_Delay to 30 minutes
*/

using Server.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Misc
{
    public class AutoSave : Timer
    {
        private static TimeSpan m_Delay = TimeSpan.FromMinutes(CoreAI.WorldSaveFrequency);
        private static TimeSpan m_Warning = TimeSpan.Zero;

        public static void Initialize()
        {
            // 2/28/22, Adam: Moving Auto save to CronTasks.cs
            //  We keep the framework 'class AutoSave' as it still has useful functionality/checks
            //new AutoSave().Start();
            CommandSystem.Register("SetSaves", AccessLevel.Administrator, new CommandEventHandler(SetSaves_OnCommand));
            CommandSystem.Register("SetDBSaves", AccessLevel.Administrator, new CommandEventHandler(SetDBSaves_OnCommand));
        }

        public static void Configure()
        {   // baby file manager for Backup
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }

        private static bool m_SavesEnabled = true;
        private static bool m_DBSavesEnabled = true;
        private static bool m_FinalSave = false;        // tells us when this is the final save, will raise an event
        public static bool FinalSave
        {
            get { return m_FinalSave; }
            // gets set when this is the final save before we shutdown
            set { m_FinalSave = value; if (value == true) EventSink.InvokeShuttingDown(new ShuttingDownEventArgs()); }
        }
        public static bool SavesEnabled
        {
            get { return m_SavesEnabled; }
            set { m_SavesEnabled = value; }
        }

        public static bool DBSavesEnabled
        {
            get { return m_DBSavesEnabled; }
            set { m_DBSavesEnabled = value; }
        }

        [Usage("SetSaves <true | false>")]
        [Description("Enables or disables automatic shard saving.")]
        public static void SetSaves_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 1)
            {
                m_SavesEnabled = e.GetBoolean(0);
                e.Mobile.SendMessage("Saves have been {0}.", m_SavesEnabled ? "enabled" : "disabled");
            }
            else
            {
                e.Mobile.SendMessage("Format: SetSaves <true | false>");
            }
        }

        [Usage("SetDBSaves <true | false>")]
        [Description("Enables or disables automatic account DB saving.")]
        public static void SetDBSaves_OnCommand(CommandEventArgs e)
        {   // Note, this is processed in CronTasks.cs
            if (e.Length == 1)
            {
                m_DBSavesEnabled = e.GetBoolean(0);
                e.Mobile.SendMessage("Account database Saves have been {0}.", m_DBSavesEnabled ? "enabled" : "disabled");
            }
            else
            {
                e.Mobile.SendMessage("Format: SetDBSaves <true | false>");
            }
        }

        public AutoSave()
            : base(m_Delay - m_Warning, m_Delay)
        {
            Priority = TimerPriority.OneMinute;
        }

        private static int m_iLastSaveFrequency = CoreAI.WorldSaveFrequency;
        protected override void OnTick()
        {
            if (m_iLastSaveFrequency != CoreAI.WorldSaveFrequency)
            {
                // 2/28/22, Adam: Moving Auto save to CronTasks.cs
                //  We keep the framework 'class AutoSave' as it still has useful functionality/checks
                //this.Stop();
                //m_Delay = TimeSpan.FromMinutes(CoreAI.WorldSaveFrequency);
                //System.Console.WriteLine("Changing worldsave frequency from {0} to {1}", m_iLastSaveFrequency, CoreAI.WorldSaveFrequency);
                //new AutoSave().Start();
            }
            m_iLastSaveFrequency = CoreAI.WorldSaveFrequency;

            // don't do a save if he server is backing up
            bool ServerBusy = false;
            try
            {
                ServerBusy = System.Diagnostics.Process.GetProcessesByName("ntbackup").Length > 0;
                if (ServerBusy)
                    Console.WriteLine("World save skipped, server backup in progress.");
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine(e.ToString());
            }

            // final check to see if we should save
            if (!m_SavesEnabled || AutoRestart.Restarting || ServerBusy)
                return;

            if (m_Warning == TimeSpan.Zero)
            {
                Save();
            }
            else
            {
                int s = (int)m_Warning.TotalSeconds;
                int m = s / 60;
                s %= 60;

                if (m > 0 && s > 0)
                    World.Broadcast(0x35, true, "The world will save in {0} minute{1} and {2} second{3}.", m, m != 1 ? "s" : "", s, s != 1 ? "s" : "");
                else if (m > 0)
                    World.Broadcast(0x35, true, "The world will save in {0} minute{1}.", m, m != 1 ? "s" : "");
                else
                    World.Broadcast(0x35, true, "The world will save in {0} second{1}.", s, s != 1 ? "s" : "");

                Timer.DelayCall(m_Warning, new TimerCallback(Save));
            }
        }

        public static void Save()
        {
            if (AutoRestart.Restarting)
                return;

            if (Maintenance.Scheduled)  // final save before shutdown
                Server.Misc.AutoSave.FinalSave = true;

            // we split the backup and save into separate catch blocks because we still want to save even if we cannot backup
            try
            {
                Backup();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }

            try
            {
                World.Save();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }

        }
        public static void Backup()
        {
            string filename = Utility.GameTimeFileStamp();  // 'Game Time' Format: "August 31 2023, 06 11 34 PM"
            if (Server.Misc.AutoSave.FinalSave)             // final save before we go down
                filename += "(final)";                      // add a special tag to indicate this is the last save before the patch

            Backup(filename);
        }
        private static void Backup(string filename)
        {
            Console.WriteLine("Backing up...");

            string root = Path.Combine(Core.BaseDirectory, "Backups\\Automatic");
            string saves = Path.Combine(Core.BaseDirectory, "Saves");

            m_fileManagerDatabase.Enqueue(filename);        // save/load these for periodic cleanup
            Console.WriteLine("File Manager: Enqueueing {0}", filename);
            if (Directory.Exists(saves))
            {
                try { Directory.Move(saves, FormatDirectory(root, filename, "")); }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("We'll copy instead.");
                    try { DirectoryCopy(saves, FormatDirectory(root, filename, ""), true); }
                    catch { Console.WriteLine("Cannot copy Saves directory. Giving up."); }
                }
            }
            else
                Console.WriteLine("{0} does not exist!", saves);

            // 1/18/22, Adam: disabling this.. Our Google backup (server-side) allows the files to get deleted, but not the directories.
            //  kinda ugly, need to find a better way. For now I'll just delete old backups manually
            if (false)
                while (m_fileManagerDatabase.Count > CoreAI.BackupCount)
                {
                    string[] existing = Directory.GetDirectories(root);
                    DirectoryInfo dir;
                    string toDelete = m_fileManagerDatabase.Dequeue();
                    dir = Match(existing, toDelete);
                    Console.WriteLine("File Manager: Dequeuing {0}", toDelete);
                    if (dir != null && Directory.Exists(dir.FullName))
                    {
                        Console.WriteLine("File Manager: Deleting {0}", dir.Name);
                        // asynchronous file delete
                        //Task.Factory.StartNew(path => Directory.Delete((string)path, true), dir.FullName);
                        try { dir.Delete(true); }
                        catch { Console.WriteLine("Cannot delete backup directory {0}. Giving up.", dir.Name); }
                    }
                }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        private static DirectoryInfo Match(string[] paths, string match)
        {
            for (int i = 0; i < paths.Length; ++i)
            {
                DirectoryInfo info = new DirectoryInfo(paths[i]);

                if (info.Name.StartsWith(match))
                    return info;
            }

            return null;
        }

        private static string FormatDirectory(string root, string name, string timeStamp)
        {
            return Path.Combine(root, string.Format("{0}", name));
        }

        #region Serialization
        private static Queue<string> m_fileManagerDatabase = new Queue<string>();
        public static Queue<string> FileManagerDatabase { get { return m_fileManagerDatabase; } }
        public static void Load()
        {
            if (!File.Exists("Saves/BackupFileManager.bin"))
                return;

            Console.WriteLine("Backup file manager Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/BackupFileManager.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();
                switch (version)
                {
                    case 1:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                                m_fileManagerDatabase.Enqueue(reader.ReadString());
                            break;
                        }
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error reading Saves/BackupFileManager.bin, using default values:");
                Console.WriteLine(ex.Message);
                Utility.PopColor();
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Backup file manager Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/BackupFileManager.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            writer.Write(m_fileManagerDatabase.Count);
                            foreach (string filename in m_fileManagerDatabase)
                                writer.Write(filename);
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error writing Saves/BackupFileManager.bin");
                Console.WriteLine(ex.Message);
                Utility.PopColor();
            }
        }
        #endregion Serialization
    }
}