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

/* Scripts/Misc/CrashGuard.cs
 * CHANGELOG:
 *  3/10/07, Adam
 *      Replace old hardcoded email list with new distribution list: devnotify@game-master.net
 *  12/19/06, Adam
 *      Add a check to prevent sending Crash emails if this is a private server.
 *  11/18/06, Adam
 *      Update to new .NET 2.0 email services
 *	9/3/06, Adam
 *		Add zenroth52@gmail.com to email list
 *	5/6/06, Pix
 *		Added "(Test Center)" to email sent if the crash happens on Test.
 *	2/2/06, Adam
 *		call our new SmtpDirect email engine 
 */

using Server.Accounting;
using Server.Network;
using Server.SMTP;					// new SMTP engine
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
//using System.Web.Mail;
using System.Net.Mail;

namespace Server.Misc
{
    public class CrashGuard
    {
        private static bool Enabled = true;
        private static bool SaveBackup = true;
        private static bool RestartServer = true;
        private static bool GenerateReport = true;

        // To add reporting emailing, fill in EmailServer and Emails:
        // Example:
        //  private const string Emails = "first@email.here;second@email.here;third@email.here";
        private static string Emails = Environment.GetEnvironmentVariable("AI.EMAIL.DEVNOTIFY");

        public static void Initialize()
        {
            if (Enabled) // If enabled, register our crash event handler
                EventSink.Crashed += new CrashedEventHandler(CrashGuard_OnCrash);
#if DEBUG
            //register a command to simulate a crash
            CommandSystem.Register("Crash", AccessLevel.Administrator, new CommandEventHandler(CrashGuard_OnCrash));
#endif
        }
#if DEBUG
        public static void CrashGuard_OnCrash(CommandEventArgs e)
        {
            CrashGuard_OnCrash(new CrashedEventArgs(new Exception()));
        }
#endif
        public static void CrashGuard_OnCrash(CrashedEventArgs e)
        {
            if (SaveBackup)
                Backup();

            if (GenerateReport)
                GenerateCrashReport(e);

            if (Core.Service)
                e.Close = true;
            else if (RestartServer)
                Restart(e);
        }

        private static void SendEmail(string filePath)
        {
            Console.Write("Crash: Sending email...");

            try
            {
                MailMessage message = new MailMessage();
                message.Subject = "Automated RunUO Crash Report";
                if (Server.Misc.TestCenter.Enabled)
                {
                    message.Subject += " (Test Center)";
                }
                message.From = new MailAddress(SmtpDirect.FromEmailAddress);
                message.To.Add(SmtpDirect.ClassicList(Emails));
                message.Body = "Automated RunUO Crash Report. See attachment for details.";
                message.Attachments.Add(SmtpDirect.MailAttachment(filePath));

                bool result = new SmtpDirect().SendEmail(message);
                Console.WriteLine("done: {0}", result.ToString());
            }
            catch
            {
                Console.WriteLine("failed");
            }
        }

        private static string GetRoot()
        {
            try
            {
                return Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            }
            catch
            {
                return "";
            }
        }

        private static string Combine(string path1, string path2)
        {
            if (path1 == "")
                return path2;

            return Path.Combine(path1, path2);
        }

        private static void Restart(CrashedEventArgs e)
        {
            string root = GetRoot();

            Console.Write("Crash: Restarting...");

            try
            {
                Process.Start(Core.ExePath);
                Console.WriteLine("done");

                e.Close = true;
            }
            catch
            {
                Console.WriteLine("failed");
            }
        }

        private static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static void CreateDirectory(string path1, string path2)
        {
            CreateDirectory(Combine(path1, path2));
        }

        private static void CopyFile(string rootOrigin, string rootBackup, string path)
        {
            string originPath = Combine(rootOrigin, path);
            string backupPath = Combine(rootBackup, path);

            try
            {
                if (File.Exists(originPath))
                    File.Copy(originPath, backupPath);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        private static void Backup()
        {
            Console.Write("Crash: Backing up...");

            try
            {
                string timeStamp = GetTimeStamp();

                string root = GetRoot();
                string rootBackup = Combine(root, string.Format("Backups/Crashed/{0}/", timeStamp));
                string rootOrigin = Combine(root, string.Format("Saves/"));

                // Create new directories
                CreateDirectory(rootBackup);
                CreateDirectory(rootBackup, "Accounts/");
                CreateDirectory(rootBackup, "Items/");
                CreateDirectory(rootBackup, "Mobiles/");
                CreateDirectory(rootBackup, "Guilds/");
                CreateDirectory(rootBackup, "Regions/");

                // Copy files
                CopyFile(rootOrigin, rootBackup, "Accounts/Accounts.xml");

                CopyFile(rootOrigin, rootBackup, "Items/Items.bin");
                CopyFile(rootOrigin, rootBackup, "Items/Items.idx");
                CopyFile(rootOrigin, rootBackup, "Items/Items.tdb");

                CopyFile(rootOrigin, rootBackup, "Mobiles/Mobiles.bin");
                CopyFile(rootOrigin, rootBackup, "Mobiles/Mobiles.idx");
                CopyFile(rootOrigin, rootBackup, "Mobiles/Mobiles.tdb");

                CopyFile(rootOrigin, rootBackup, "Guilds/Guilds.bin");
                CopyFile(rootOrigin, rootBackup, "Guilds/Guilds.idx");

                CopyFile(rootOrigin, rootBackup, "Regions/Regions.bin");
                CopyFile(rootOrigin, rootBackup, "Regions/Regions.idx");

                Console.WriteLine("done");
            }
            catch
            {
                Console.WriteLine("failed");
            }
        }

        private static void GenerateCrashReport(CrashedEventArgs e)
        {
            Console.Write("Crash: Generating report...");

            try
            {
                string timeStamp = GetTimeStamp();
                string fileName = string.Format("Crash {0}.log", timeStamp);

                string root = GetRoot();
                string filePath = Combine(root, fileName);

                using (StreamWriter op = new StreamWriter(filePath))
                {
                    op.WriteLine("Server Crash Report");
                    op.WriteLine("===================");
                    op.WriteLine();
                    op.WriteLine("Operating System: {0}", Environment.OSVersion);
                    op.WriteLine(".NET Framework: {0}", Environment.Version);
                    op.WriteLine("Time: {0}", DateTime.UtcNow);

                    try { op.WriteLine("Mobiles: {0}", World.Mobiles.Count); }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                    try { op.WriteLine("Items: {0}", World.Items.Count); }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                    op.WriteLine("Clients:");

                    try
                    {
                        //ArrayList states = NetState.Instances;
                        List<NetState> states = NetState.Instances;

                        op.WriteLine("- Count: {0}", states.Count);

                        for (int i = 0; i < states.Count; ++i)
                        {
                            NetState state = states[i];

                            op.Write("+ {0}:", state);

                            Account a = state.Account as Account;

                            if (a != null)
                                op.Write(" (account = {0})", a.Username);

                            Mobile m = state.Mobile;

                            if (m != null)
                                op.Write(" (mobile = 0x{0:X} '{1}')", m.Serial.Value, m.Name);

                            op.WriteLine();
                        }
                    }
                    catch
                    {
                        op.WriteLine("- Failed");
                    }

                    op.WriteLine();

                    op.WriteLine("Exception:");
                    op.WriteLine(e.Exception);
                }

                Console.WriteLine("done");

                // don't send email from some random developer's computer
                if (Emails != null && Utility.IsHostPrivate(Utility.GetHost()) == false)
                    SendEmail(filePath);
            }
            catch
            {
                Console.WriteLine("failed");
            }
        }

        private static string GetTimeStamp()
        {
            DateTime now = DateTime.UtcNow;

            return string.Format("{0}-{1}-{2}-{3}-{4}-{5}",
                    now.Day,
                    now.Month,
                    now.Year,
                    now.Hour,
                    now.Minute,
                    now.Second
                );
        }
    }
}