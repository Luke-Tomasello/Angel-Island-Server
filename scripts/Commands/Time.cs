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

/* Scripts/Commands/Time.cs
 * ChangeLog
 *	3/25/08 - Pix
 *		Changed to use new AdjustedDateTime utility class.
 *	12/06/05 - Pigpen
 *		Created.
 *		Time command works as follows. '[time' Displays Date then time.
 *	3/10/07 - Cyrun
 *		Edited message displayed to include "PST".
 */


using Server.Mobiles;

namespace Server.Commands
{
    public class TimeCommand
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Time", AccessLevel.Player, new CommandEventHandler(Time_OnCommand));
        }

        /*public static void Time_OnCommand(CommandEventArgs e)
		{
			Mobile m = e.Mobile;
			// Console.WriteLine("Scheduler: ({0} Server)/({1} Game), Priority Queue: {2}, Task Queue: {3}, Idle Queue: {4}", serverTime, gameTime, GetPriorityQueueDepth(), GetTaskQueueDepth(), GetIdleQueueDepth());
			if (m is PlayerMobile)
			{
				//m.SendMessage("Server time is: {0} PST.", DateTime.UtcNow);

				AdjustedDateTime ddt = new AdjustedDateTime(DateTime.UtcNow);
				m.SendMessage("Server time is: {0} {1}.", ddt.Value.ToShortTimeString(), ddt.TZName);
			}
		}*/

        public static void Time_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m is PlayerMobile)
            {
                m.SendMessage($"(Server time is: {AdjustedDateTime.ServerTime.ToShortTimeString()})");
                UOTime(m);
            }
        }

        public static void UOTime(Mobile from)
        {
            int genericNumber;
            string exactTime;

            Items.Clock.GetTime(from, out genericNumber, out exactTime);
            string message = Server.Text.Cliloc.Lookup[genericNumber];
            from.SendMessage(message);
            from.SendMessage(string.Format($"{exactTime} to be exact"));
        }

    }
}