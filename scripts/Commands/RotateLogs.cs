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

/* Scripts/Commands/RotateLogs.cs
 * 	CHANGELOG:
 *	11/14/06, Adam
 *		Adjust rollover tag to be seconds since 1/1/2000
 * 	11/3/06, Adam
 *		Initial Version
 */

using Server.Diagnostics;
using System;
using System.IO;

namespace Server.Commands
{
    public class RotateLogs
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("RotateLogs", AccessLevel.Administrator, new CommandEventHandler(RotateLogs_OnCommand));
        }

        [Usage("RotateLogs")]
        [Description("Rotate player command logs.")]
        public static void RotateLogs_OnCommand(CommandEventArgs e)
        {
            try
            {
                RotateNow();
                e.Mobile.SendMessage("Log rotation complete.");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Console.WriteLine(ex.ToString());
                e.Mobile.SendMessage("Log rotation failed.");
            }
        }

        public static void RotateNow()
        {
            // close the open logfile
            CommandLogging.Close();

            // 5/12/2024, Adam: We need to allow the CLR time to cleanup finalizers (I think,) before we actually move the file or else it fails. 
            //  I added queuing for the command logger to buffer output until the stream is reopened.
            Timer.DelayCall(TimeSpan.FromSeconds(7.0), new TimerStateCallback(RotateTick), new object[] { null, null });
        }

        private static void RotateTick(object state)
        {
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            bool ok = true;
            try
            {
                string root = Path.Combine(Core.BaseDirectory, "Logs");

                if (!Directory.Exists(root))
                    Directory.CreateDirectory(root);

                string[] existing = Directory.GetDirectories(root);

                DirectoryInfo dir;

                // rename the commands directory with a date-time stamp
                dir = Match(existing, "Commands");
                if (dir != null)
                {
                    string ToName = Utility.GameTimeFileStamp(id: true);  // 'Game Time' Format: "August 31 2023, 06 11 34 PM (0x01)"
                    try { dir.MoveTo(FormatDirectory(root, ToName, "")); }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex);
                        Console.WriteLine("Failed to move to {0}", FormatDirectory(root, ToName, ""));
                        Console.WriteLine(ex.ToString());
                        ok = false;
                    }
                }

                // reopen the logfile
                CommandLogging.Open();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                tc.End();
                System.Console.WriteLine("Rotation of command logs {0} in {1}.",
                        ok ? "completed" : "failed", tc.TimeTaken);
            }
        }

        #region Archive

        public static void ArchiveNow()
        {
            // close the open logfile
            CommandLogging.Close();

            // 5/13/2024, Adam: We need to allow the CLR time to cleanup finalizers (I think,) before we actually archive the files or else it fails. 
            //  (I added queuing for the command logger to buffer output until the stream is reopened.)
            Timer.DelayCall(TimeSpan.FromSeconds(7.0), new TimerStateCallback(ArchiveTick), new object[] { null, null });
        }

        private static void ArchiveTick(object state)
        {
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            bool compress_ok = true;
            try
            {
                string root = Path.Combine(Core.BaseDirectory, "Logs");

                if (Directory.Exists(root))
                {
                    DateTime dt = AdjustedDateTime.GameTime;
                    string ToName = string.Format($"Logs {dt.Month}-{dt.Day}-{dt.Year}.zip");
                    if (!File.Exists(ToName))
                    {
                        try { System.IO.Compression.ZipFile.CreateFromDirectory(root, ToName); }
                        catch (Exception ex)
                        {
                            LogHelper.LogException(ex);
                            Utility.ConsoleWriteLine(string.Format("Failed to archive to {0}", FormatDirectory(root, ToName, "")), ConsoleColor.Red);
                            Console.WriteLine(ex.ToString());
                            compress_ok = false;
                            goto done;
                        }
                        // now we do our best to delete the folder. Failure is routine. See comments in delete routine.
                        Timer.DelayCall(TimeSpan.FromSeconds(7.0), new TimerStateCallback(DeleteTick), new object[] { null, null });
                    }
                    else
                        Utility.ConsoleWriteLine(string.Format("Archive {0} already exists", ToName), ConsoleColor.Yellow);
                }

            done:;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                tc.End();
                Utility.ConsoleWriteLine(string.Format("Archive of command logs {0} in {1}.",
                        compress_ok ? "completed" : "failed", tc.TimeTaken), compress_ok ? ConsoleColor.White : ConsoleColor.Yellow);
            }
        }
        private static void DeleteTick(object state)
        {
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            bool remove_ok = true;
            int count = 0;
            try
            {
                string root = Path.Combine(Core.BaseDirectory, "Logs");

                if (Directory.Exists(root))
                {
                    DateTime dt = AdjustedDateTime.GameTime;
                    string ToName = string.Format($"Logs {dt.Month}-{dt.Day}-{dt.Year}.zip");
                    if (File.Exists(ToName))
                    {
                        try { DeleteDirectory(root, ref count); }
                        catch (Exception ex)
                        {
                            Utility.ConsoleWriteLine(string.Format("Failed to completely remove to {0} (non critical)", root), ConsoleColor.Yellow);
                            remove_ok = false;
                            goto done;
                        }
                    }
                    else
                        Utility.ConsoleWriteLine(string.Format("Archive {0} doesn't exist. Unsafe to delete", ToName), ConsoleColor.Yellow);
                }

            done:
                // reopen the logfile
                CommandLogging.Open();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                tc.End();
                Utility.ConsoleWriteLine(string.Format("Removal of logs directory {0} in {1}.\n{2} files/folders actually removed.",
                        remove_ok ? "completed" : "failed", tc.TimeTaken, count), remove_ok ? ConsoleColor.White : ConsoleColor.Yellow);
                if (remove_ok == false)
                    Utility.ConsoleWriteLine(string.Format("A failure here is acceptable since backup, explorer, anti-virus etc. can all have files/folders open."), ConsoleColor.Yellow);
            }
        }
        public static void DeleteDirectory(string target_dir, ref int count)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
                count++;
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir, ref count);
            }

            Directory.Delete(target_dir, true);
            count++;
        }
        #endregion Archive

        private static string FormatDirectory(string root, string name, string timeStamp)
        {
            return Path.Combine(root, String.Format("{0}", name));
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
    }
}