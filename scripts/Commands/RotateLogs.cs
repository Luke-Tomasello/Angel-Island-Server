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

/* Scripts/Commands/RotateLogs.cs
 * 	CHANGELOG:
  *  8/1/2026, Adam
 *      Refactored monthly log rotation to detach the complete active Logs
 *      directory into a timestamped snapshot. Logging now resumes immediately,
 *      while compression and cleanup run asynchronously against the detached
 *      snapshot. Added transient retry handling, temporary archive publication,
 *      failure-safe cleanup, and protection against overlapping rotations.
 *	11/14/06, Adam
 *		Adjust rollover tag to be seconds since 1/1/2000
 * 	11/3/06, Adam
 *		Initial Version
 */

using Server.Diagnostics;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace Server.Commands
{
    public class RotateLogs
    {
        private const int MaxRotateAttempts = 3;

        private static readonly TimeSpan RotateRetryDelay =
            TimeSpan.FromSeconds(1.0);

        // 0 = idle, 1 = rotation/archive in progress
        private static int m_Rotating;

        public static void Initialize()
        {
            Server.CommandSystem.Register(
                "RotateLogs",
                AccessLevel.Administrator,
                new CommandEventHandler(RotateLogs_OnCommand));
        }

        [Usage("RotateLogs")]
        [Description("Rotate and archive server logs.")]
        public static void RotateLogs_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (RotateNow())
                    e.Mobile.SendMessage("Log rotation started.");
                else
                    e.Mobile.SendMessage("Log rotation is already in progress.");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Console.WriteLine(ex.ToString());
                e.Mobile.SendMessage("Log rotation failed.");
            }
        }

        public static bool RotateNow()
        {
            // Atomically prevent overlapping command, cron, or retry operations.
            if (Interlocked.CompareExchange(ref m_Rotating, 1, 0) != 0)
            {
                Utility.Monitor.WriteLine(
                    "Log rotation is already in progress.",
                    ConsoleColor.Yellow);

                return false;
            }

            try
            {
                RotateTick(1);
                return true;
            }
            catch
            {
                EndRotation();
                throw;
            }
        }

        private static void RotateTick(object state)
        {
            int attempt = state is int value ? value : 1;

            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();

            bool rotated = false;
            bool retryScheduled = false;
            bool archiveQueued = false;

            string? snapshotDirectory = null;

            try
            {
                CommandLogging.Close();

                try
                {
                    snapshotDirectory = DetachLogsDirectory();
                    rotated = true;
                }
                catch (IOException ex)
                {
                    if (attempt < MaxRotateAttempts)
                    {
                        Utility.Monitor.WriteLine(
                            string.Format(
                                "Log rotation attempt {0} failed; retrying in {1} second.",
                                attempt,
                                RotateRetryDelay.TotalSeconds),
                            ConsoleColor.Yellow);

                        Timer.DelayCall(
                            RotateRetryDelay,
                            new TimerStateCallback(RotateTick),
                            attempt + 1);

                        retryScheduled = true;
                    }
                    else
                    {
                        LogHelper.LogException(ex);
                        Console.WriteLine(ex.ToString());
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogHelper.LogException(ex);
                    Console.WriteLine(ex.ToString());
                }
                finally
                {
                    // Never leave command logging offline.
                    CommandLogging.Open();
                }

                if (snapshotDirectory != null)
                {
                    QueueArchive(snapshotDirectory);
                    archiveQueued = true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                tc.End();

                if (rotated)
                {
                    Utility.Monitor.WriteLine(
                        string.Format(
                            "Rotation of logs completed in {0}.",
                            tc.TimeTaken),
                        ConsoleColor.White);
                }
                else if (!retryScheduled)
                {
                    Utility.Monitor.WriteLine(
                        string.Format(
                            "Rotation of logs failed after {0} attempt{1}.",
                            attempt,
                            attempt == 1 ? string.Empty : "s"),
                        ConsoleColor.Yellow);
                }

                /*
                 * A scheduled retry retains ownership of the rotation.
                 * A queued archive releases ownership in ArchiveSnapshot().
                 * Every other path is terminal and must release it here.
                 */
                if (!retryScheduled && !archiveQueued)
                    EndRotation();
            }
        }

        private static string GetActiveLogsDirectory()
        {
            return Path.Combine(Core.ExeDirectory, "Logs");
        }

        private static string GetArchiveQueueDirectory()
        {
            return Path.Combine(Core.ExeDirectory, "LogArchiveQueue");
        }

        private static string DetachLogsDirectory()
        {
            string activeDirectory = GetActiveLogsDirectory();
            string queueDirectory = GetArchiveQueueDirectory();

            if (!Directory.Exists(queueDirectory))
                Directory.CreateDirectory(queueDirectory);

            if (!Directory.Exists(activeDirectory))
                Directory.CreateDirectory(activeDirectory);

            string snapshotName = string.Format(
                "Logs {0}",
                Utility.GameTimeFileStamp(id: true));

            string snapshotDirectory =
                Path.Combine(queueDirectory, snapshotName);

            Directory.Move(
                activeDirectory,
                snapshotDirectory);

            // Establish the new live logging generation immediately.
            Directory.CreateDirectory(activeDirectory);

            return snapshotDirectory;
        }

        private static void QueueArchive(string snapshotDirectory)
        {
            if (string.IsNullOrEmpty(snapshotDirectory))
                throw new ArgumentNullException(nameof(snapshotDirectory));

            if (!ThreadPool.QueueUserWorkItem(
                    new WaitCallback(ArchiveSnapshot),
                    snapshotDirectory))
            {
                throw new InvalidOperationException(
                    "Unable to queue the log archive worker.");
            }
        }

        private static void ArchiveSnapshot(object? state)
        {
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();

            bool completed = false;
            string status = "failed";

            string? snapshotDirectory = state as string;
            string? archivePath = null;
            string? temporaryPath = null;

            try
            {
                if (string.IsNullOrEmpty(snapshotDirectory))
                {
                    Utility.Monitor.WriteLine(
                        "Log archive worker received an invalid snapshot path.",
                        ConsoleColor.Yellow);

                    return;
                }

                if (!Directory.Exists(snapshotDirectory))
                {
                    Utility.Monitor.WriteLine(
                        string.Format(
                            "Log snapshot {0} no longer exists.",
                            snapshotDirectory),
                        ConsoleColor.Yellow);

                    return;
                }

                archivePath = snapshotDirectory + ".zip";
                temporaryPath = archivePath + ".tmp";

                if (File.Exists(archivePath))
                {
                    status = "skipped";

                    Utility.Monitor.WriteLine(
                        string.Format(
                            "Archive {0} already exists; snapshot retained.",
                            archivePath),
                        ConsoleColor.Yellow);

                    return;
                }

                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                ZipFile.CreateFromDirectory(
                    snapshotDirectory,
                    temporaryPath);

                // Publish only a completely constructed archive.
                File.Move(
                    temporaryPath,
                    archivePath);

                // Delete the snapshot only after successful publication.
                Directory.Delete(
                    snapshotDirectory,
                    true);

                completed = true;
                status = "completed";
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);

                Utility.Monitor.WriteLine(
                    string.Format(
                        "Failed to archive log snapshot {0}.",
                        snapshotDirectory),
                    ConsoleColor.Red);

                Console.WriteLine(ex.ToString());

                try
                {
                    if (!string.IsNullOrEmpty(temporaryPath) &&
                        File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
                catch (Exception cleanupException)
                {
                    LogHelper.LogException(cleanupException);
                }
            }
            finally
            {
                tc.End();

                Utility.Monitor.WriteLine(
                    string.Format(
                        "Archive of log snapshot {0} in {1}.",
                        status,
                        tc.TimeTaken),
                    completed
                        ? ConsoleColor.White
                        : ConsoleColor.Yellow);

                EndRotation();
            }
        }

        private static void EndRotation()
        {
            Interlocked.Exchange(ref m_Rotating, 0);
        }
    }
}