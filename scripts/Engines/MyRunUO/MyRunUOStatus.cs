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

namespace Server.Engines.MyRunUO
{
#if false
    public class MyRunUOStatus
    {
        public static void Initialize()
        {
            if (Config.Enabled)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(20.0), Config.StatusUpdateInterval, new TimerCallback(Begin));

                CommandSystem.Register("UpdateWebStatus", AccessLevel.Administrator, new CommandEventHandler(UpdateWebStatus_OnCommand));
            }
        }

        [Usage("UpdateWebStatus")]
        [Description("Starts the process of updating the MyRunUO online status database.")]
        public static void UpdateWebStatus_OnCommand(CommandEventArgs e)
        {
            if (m_Command == null || m_Command.HasCompleted)
            {
                Begin();
                e.Mobile.SendMessage("Web status update process has been started.");
            }
            else
            {
                e.Mobile.SendMessage("Web status database is already being updated.");
            }
        }

        private static DatabaseCommandQueue m_Command;

        public static void Begin()
        {
            if (m_Command != null && !m_Command.HasCompleted)
                return;

            DateTime start = DateTime.UtcNow;
            Console.WriteLine("MyRunUO: Updating status database");

            try
            {
                m_Command = new DatabaseCommandQueue("MyRunUO: Status database updated in {0:F1} seconds", "MyRunUO Status Database Thread");

                m_Command.Enqueue("DELETE FROM myrunuo_status");

                List<NetState> online = NetState.Instances;

                for (int i = 0; i < online.Count; ++i)
                {
                    NetState ns = online[i];
                    Mobile mob = ns.Mobile;

                    if (mob != null)
                        m_Command.Enqueue(String.Format("INSERT INTO myrunuo_status VALUES ({0})", mob.Serial.Value.ToString()));
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("MyRunUO: Error updating status database");
                Console.WriteLine(e);
            }

            if (m_Command != null)
                m_Command.Enqueue(null);
        }
    }
#endif
}