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

/* ***********************************
 * ** CAUTIONARY NOTE ON TIME USAGE **
 * This module uses AdjustedDateTime.GameTime which returns a DST adjusted version of time independent
 * of the underlying system. For instance our production server DOES NOT change time for DST, but this module
 * along with a select few others surfaces functionality to the players which require a standard (DST adjusted) notion of time.
 * The DST adjusted modules include: AutomatedEventSystem.cs, AutoRestart.cs, CronScheduler.cs.
 * ***********************************
 */

/* Scripts/Misc/AutoRestart.cs
 * CHANGELOG
 *	12/23/08, Adam
 *		Update to use RunUO 2.0 restart model
 *	4/14/08, Adam
 *		Replace all explicit use of DateTime.UtcNow with the new: public static DateTime AdjustedDateTime.GameTime
 *		Since the AES system uses AutoRestart, then AutoRestart must also respect 'game time' and can no longer use DateTime.UtcNow
 *		files that need to opperate in this special time mode: CronScheduler.cs, AutoRestart.cs, AutomatedEventSystem.cs
 *	4/6/08, Adam
 *		if World.SaveOption.NoSaves is on, don't call AutoSave.Save() as it will still move the directories around
 *  3/5/07, Adam
 *      Add ServerWars property to see if ServerWars are running
 *      Add ServerWars command to see if ServerWars are running
 *	6/2/06, Adam
 *		Export a few variables so that I can control Server Wars programatically
 *	4/29/05, Adam
 *		Change the ServerWarsMinutes property to display TotalMinutes
 *	4/28/05, Adam
 *		Many updates to support Server Wars
 *		Disable saves at the beginning of "Server Wars"
 *		Add special message when wars are on
 *		Add ServerWarsMinutes property called by Command Console
 *	12/21/05, Adam
 *		Change from TimeSpan.Zero to 5 minutes
 *		Set RestartDelay = TimeSpan.Zero if we are [restart now
 *	5/02/2004, Pixie
 *		Initial Revision.
 *		Commands [Restart and [RestartTime
 */

using Server.Diagnostics;
using System;

namespace Server.Misc
{
    public class AutoRestart : Timer
    {
        private static bool m_enabled = false;              // is the script enabled?
        private static double ServerWarsDefault = 5.0;      // how long the server should remain active before restart (period of 'server wars')
        private static double ShutdownWarningDefault = 1.0; // at what interval should the shutdown message be displayed?
        private static double WarsWarningDefault = 10.0;    // at what interval should the Server Wars message be displayed?
        private static TimeSpan m_RestartOffset = TimeSpan.FromHours(2.0); // time of day at which to restart
        private static TimeSpan RestartDelay = TimeSpan.FromMinutes(ServerWarsDefault);
        private static TimeSpan ShutdownWarningInterval = TimeSpan.FromMinutes(ShutdownWarningDefault);
        private static TimeSpan WarsWarningInterval = TimeSpan.FromMinutes(WarsWarningDefault);

        private static bool m_Restarting;
        private static DateTime m_RestartTime;

        public static bool Restarting
        {
            get { return m_Restarting; }
        }

        public static bool Enabled
        {
            get { return m_enabled; }
            set { m_enabled = value; }
        }

        public static DateTime RestartTime
        {
            get { return m_RestartTime; }
            set { m_RestartTime = value; }
        }

        public static int ServerWarsMinutes
        {
            get { return (int)RestartDelay.TotalMinutes; }
            set { RestartDelay = TimeSpan.FromMinutes(value); }
        }

        public static void Initialize()
        {
            CommandSystem.Register("ServerWars", AccessLevel.GameMaster, new CommandEventHandler(ServerWars_OnCommand));
            CommandSystem.Register("Restart", AccessLevel.Administrator, new CommandEventHandler(Restart_OnCommand));
            CommandSystem.Register("RestartTime", AccessLevel.Administrator, new CommandEventHandler(RestartTime_OnCommand));
            new AutoRestart().Start();
        }

        public static bool ServerWars
        {
            get { return World.SaveType == World.SaveOption.NoSaves && m_enabled && RestartDelay.TotalMinutes > ServerWarsDefault; }
        }

        public static void ServerWars_OnCommand(CommandEventArgs e)
        {
            if (ServerWars == true)
                e.Mobile.SendMessage("Server wars are in progress.");
            else
                e.Mobile.SendMessage("Server wars are not in progress.");
        }

        public static void RestartTime_OnCommand(CommandEventArgs e)
        {
            if (m_enabled)
            {
                e.Mobile.SendMessage("The server is set to restart at " + m_RestartTime);
            }
            else
            {
                e.Mobile.SendMessage("The server is not set to restart.");
            }
        }

        public static void Restart_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.Length == 0)
                {
                    e.Mobile.SendMessage("Format: Restart <off|now|24hrtime(xx:xx:xx)>");
                    if (m_enabled)
                    {
                        e.Mobile.SendMessage("The server is set to restart at " + m_RestartTime);
                    }
                    else
                    {
                        e.Mobile.SendMessage("The server is not set to restart.");
                    }
                }
                else
                {
                    string strParam = e.GetString(0);

                    if (strParam.ToLower().Equals("off"))
                    {
                        m_enabled = false;
                        e.Mobile.SendMessage("Restart set to off.");
                    }
                    else if (strParam.ToLower().Equals("now"))
                    {
                        if (m_Restarting)
                        {
                            e.Mobile.SendMessage("The server is already restarting.");
                        }
                        else
                        {
                            e.Mobile.SendMessage("You have initiated server shutdown.");
                            m_enabled = true;
                            m_RestartTime = AdjustedDateTime.GameTime;
                            RestartDelay = TimeSpan.Zero;
                        }
                    }
                    else
                    {
                        //should be a time format
                        char[] delimiter = { ':' };
                        string[] fields = strParam.Split(delimiter, 3);
                        int iHour = Int32.Parse(fields[0]);
                        int iMinute = 0;
                        int iSecond = 0;
                        if (fields.Length > 1)
                        {
                            iMinute = Int32.Parse(fields[1]);
                        }
                        if (fields.Length > 2)
                        {
                            iSecond = Int32.Parse(fields[2]);
                        }

                        m_RestartOffset = TimeSpan.FromHours(iHour) + TimeSpan.FromMinutes(iMinute) + TimeSpan.FromSeconds(iSecond);
                        m_RestartTime = AdjustedDateTime.GameTime.Date + m_RestartOffset;

                        if (m_RestartTime < AdjustedDateTime.GameTime)
                        {
                            m_RestartTime += TimeSpan.FromDays(1.0);
                        }

                        e.Mobile.SendMessage("Restarting server at " + m_RestartTime);
                        m_enabled = true;
                    }
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                m_enabled = false;
                e.Mobile.SendMessage("There was a problem with the [restart command!!  See console log");
                System.Console.WriteLine("Error with [RESTART!");
                System.Console.WriteLine(exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }
        }

        public AutoRestart()
            : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
        {
            Priority = TimerPriority.FiveSeconds;

            m_RestartTime = AdjustedDateTime.GameTime.Date + m_RestartOffset;

            if (m_RestartTime < AdjustedDateTime.GameTime)
                m_RestartTime += TimeSpan.FromDays(1.0);
        }

        private void ShutdownWarning_Callback()
        {
            World.Broadcast(0x22, true, "The server is going down shortly.");
        }

        private void WarWarning_Callback()
        {
            World.Broadcast(0x22, true, "Server Wars in progress...");
        }

        private void Restart_Callback()
        {
            Core.Kill(true);
        }

        protected override void OnTick()
        {
            if (m_Restarting || !m_enabled)
                return;

            if (AdjustedDateTime.GameTime < m_RestartTime)
                return;

            // warn the user 5 minutes before the server goes down
            if (ShutdownWarningInterval > TimeSpan.Zero)
            {
                TimeSpan dx = (m_RestartTime + RestartDelay) - AdjustedDateTime.GameTime;
                dx = TimeSpan.FromMinutes(dx.TotalMinutes - 5);
                Timer.DelayCall(dx, ShutdownWarningInterval, new TimerCallback(ShutdownWarning_Callback));
            }

            // if we're doing server wars, lets tell the players
            if (RestartDelay != TimeSpan.Zero && RestartDelay != TimeSpan.FromMinutes(ServerWarsDefault))
                Timer.DelayCall(TimeSpan.FromMinutes(0), WarsWarningInterval, new TimerCallback(WarWarning_Callback));

            // if World.SaveOption.NoSaves is on, don't call AutoSave.Save() as it will still move the directories around
            if (World.SaveType == World.SaveOption.NoSaves)
                Console.WriteLine("Note: AutoSave.Save() skipped because: World.SaveOption.NoSaves");
            else
                AutoSave.Save();            // last save before "Server Wars"
            AutoSave.SavesEnabled = false;  // disable saves now
            m_Restarting = true;            // okay, all set

            Timer.DelayCall(RestartDelay, new TimerCallback(Restart_Callback));
        }
    }
}