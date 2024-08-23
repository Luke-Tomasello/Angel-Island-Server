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

/* Scripts/Commands/Maintenance.cs
 * CHANGELOG
 *  1/4/22, Adam
 *      Have output to console be red
 *	12/23/08, Adam
 *		Update to use RunUO 2.0 restart model
 *	5/7/08, Adam
 *		echo countdown to the console
 *  11/13/06, Kit
 *      Added in Rebuild option, shutdown server, merge cvs, and restart server.
 *	6/2/06, Adam
 *		Initial Revision.
 *		Shuts the server down or restarts it with a minutely message of the form:
 *		"Server restarting in N minutes for maintenance..."
 */

using Server.Diagnostics;
using System;
using System.Diagnostics;

namespace Server.Misc
{
    public class Maintenance
    {
        private static bool m_Shutdown;                     // shutdown or restart?
        private static bool m_Rebuild;                      // rebuild cvs directory?
        private static bool m_Scheduled = false;            // is maintenance scheduled?
        private static int m_Countdown = 5;                 // our countdown
        private static MaintenanceTimer m_Maintenance;      // our timer
        private static int m_RebuildID;                     // process id of rebuild.exe
        private static DateTime m_When;

        public static bool Shutdown
        {
            get { return m_Shutdown; }
            set { m_Shutdown = value; }
        }

        public static bool Scheduled
        {
            get { return m_Scheduled; }
            set { m_Scheduled = value; }
        }

        public static int Countdown
        {
            get { return m_Countdown; }
            set { m_Countdown = value; }
        }

        public static bool Rebuild
        {
            get { return m_Rebuild; }
            set { m_Rebuild = value; }
        }

        public static int RebuildProcess
        {
            get { return m_RebuildID; }
            set { m_RebuildID = value; }
        }

        public static void Initialize()
        {
            CommandSystem.Register("Maintenance", AccessLevel.Administrator, new CommandEventHandler(Maintenance_OnCommand));
            m_Maintenance = new MaintenanceTimer();
        }

        public static void Maintenance_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.Length == 0)
                {
                    Usage(e);
                }
                else
                {
                    string strParam = e.GetString(0);

                    // default is 5 minutes from now
                    m_When = DateTime.UtcNow + TimeSpan.FromMinutes(5);

                    if (e.Length > 1)
                    {   // we have a date-time param
                        string sx = null;
                        for (int ix = 1; ix < e.Length; ix++)
                            sx += e.GetString(ix) + " ";
                        try { m_When = DateTime.Parse(sx); }
                        catch
                        {
                            e.Mobile.SendMessage("Bad date format.");
                            e.Mobile.SendMessage("Maintenance not initiated.");
                            Usage(e);
                            return;
                        }

                        TimeSpan diff = m_When.Subtract(DateTime.UtcNow);
                        m_Countdown = (int)diff.TotalMinutes;
                    }

                    if (strParam.ToLower().Equals("cancel"))
                    {
                        m_Scheduled = false;
                        m_Countdown = 5;
                        m_When = DateTime.MinValue;
                        if (m_Rebuild)
                        {
                            if (KillRebuild())
                            {
                                e.Mobile.SendMessage("Rebuild.exe canceled successfully.");
                                Rebuild = false;
                            }
                            else
                                e.Mobile.SendMessage("Error closing rebuild.exe!!!");
                        }
                        AutoSave.SavesEnabled = true;
                        e.Mobile.SendMessage("Maintenance has been canceled.");
                        World.Broadcast(0x482, true, "Server maintenance has been canceled.");
                        m_Maintenance.Stop();
                    }
                    else if (strParam.ToLower().Equals("rebuild"))
                    {
                        if (Rebuild)
                        {
                            e.Mobile.SendMessage("The server is already preparing for a rebuild.");
                        }
                        else
                        {
                            Rebuild = true;
                            Shutdown = true;
                            Scheduled = true;
                            AutoSave.SavesEnabled = false;
                            e.Mobile.SendMessage("You have initiated a server rebuild.");
                            m_Maintenance.Start();

                            if (!StartRebuild(Misc.TestCenter.Enabled))
                            {
                                e.Mobile.SendMessage("Rebuild.exe failed to start, canceling rebuild.");
                                Rebuild = false;
                                Scheduled = false;
                            }
                        }
                    }
                    else if (strParam.ToLower().Equals("restart") || strParam.ToLower().Equals("shutdown"))
                    {
                        if (m_Scheduled)
                        {
                            e.Mobile.SendMessage("The server is already restarting.");
                        }
                        else
                        {
                            m_Shutdown = strParam.ToLower().Equals("shutdown") ? true : false;
                            m_Scheduled = true;
                            AutoSave.SavesEnabled = false;
                            e.Mobile.SendMessage("You have initiated server {0}.", m_Shutdown ? "shutdown" : "restart");
                            m_Maintenance.Start();
                        }
                    }
                    else
                        Usage(e);
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                e.Mobile.SendMessage("There was a problem with the [Maintenance command!!  See console log");
                System.Console.WriteLine("Error with [Maintenance!");
                System.Console.WriteLine(exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }
        }

        public static bool StartRebuild(bool testcenter)
        {
            int ServerID = Core.Process.Id;

            try
            {
                Process p = new Process();
                p.StartInfo.WorkingDirectory = Core.Process.StartInfo.WorkingDirectory;
                p.StartInfo.FileName = "rebuild.exe";
                if (testcenter)
                    p.StartInfo.Arguments = ServerID.ToString() + " " + "true";
                else
                    p.StartInfo.Arguments = ServerID.ToString() + " " + "false";


                if (p.Start())
                {
                    RebuildProcess = p.Id;
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
        public static bool KillRebuild()
        {
            try
            {
                Process p = Process.GetProcessById(RebuildProcess);
                if (p != null && p.ProcessName == "rebuild")
                {
                    p.CloseMainWindow();
                    p.Close();
                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }
        public Maintenance()
        {

        }

        public class MaintenanceTimer : Timer
        {
            public MaintenanceTimer()
                : base(TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(1.0))
            {
                Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                if (Maintenance.Scheduled == false)
                    return;

                string text;
                if (Maintenance.Countdown > 5)
                {   // just tell staff
                    text = string.Format("[Staff] Server restart in {0} minutes", Maintenance.Countdown);
                    Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, text);
                }
                else
                {   // tell everyone
                    if (Maintenance.Countdown > 1)
                        text = string.Format("Server restarting in {0} minutes for maintenance...", Maintenance.Countdown);
                    else if (Maintenance.Countdown == 1)
                        text = string.Format("Server restarting in {0} minute for maintenance...", Maintenance.Countdown);
                    else
                        text = string.Format("Server restarting now...");

                    World.Broadcast(0x22, true, text);
                }
                Utility.Monitor.WriteLine(text, ConsoleColor.Red);


                if (Maintenance.Countdown == 0)
                {
                    AutoSave.Save();
                    Core.Kill(Maintenance.Shutdown == false);
                }

                Maintenance.Countdown--;
            }
        }

        private static void Usage(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Format: Maintenance <cancel|restart|shutdown|rebuild> [date time when]");
            if (m_Rebuild)
            {
                e.Mobile.SendMessage("The server is set to rebuild soon.");
            }
            else if (m_Scheduled)
            {
                e.Mobile.SendMessage("The server is set to restart soon.");
            }
            else
            {
                e.Mobile.SendMessage("The server is not set to restart.");
            }
        }
    }
}