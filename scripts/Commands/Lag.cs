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

/* Scripts/Commands/Lag.cs
 * ChangeLog
 *	3/267/08, Adam
 *		Switch from logging last 5 Heartbeat tasks to logging Scheduled tasks
 *			Heartbeat is now obsolete.
 *  11/28/06, plasma
 *      Added last 5 heartbeat tasks to log
 *	8/13/06, Pix
 *		Added console log at Adam's request.
 * 	08/3/06, weaver
 *		Initial creation.
*/
using Server.Diagnostics;
using Server.Mobiles;
using System;

namespace Server.Commands
{
    public class LagReport
    {

        public static void Initialize()
        {
            Server.CommandSystem.Register("Lag", AccessLevel.Player, new CommandEventHandler(LagReport_OnCommand));
        }

        [Usage("Lag")]
        [Description("Reports lag to the administrators")]
        private static void LagReport_OnCommand(CommandEventArgs arg)
        {
            Mobile from = arg.Mobile;

            if (from is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)from;

                // Limit to 5 minutes between lag reports
                if ((pm.LastLagTime + TimeSpan.FromMinutes(5.0)) < DateTime.UtcNow && pm.AccessLevel == AccessLevel.Player)
                {
                    // Let them log again
                    LogHelper lh = new LogHelper("lagreports.log", false, true);
                    lh.Log(LogType.Mobile, from, Server.Engines.CronScheduler.Cron.GetRecentTasks()); //adam: added schduled tasks!

                    //Requested by Adam:
                    Console.WriteLine("Lag at: {0}", DateTime.UtcNow.ToShortTimeString());

                    // Update LastLagTime on PlayerMobile
                    pm.LastLagTime = DateTime.UtcNow;

                    lh.Finish();

                    from.SendMessage("The fact that you are experiencing lag has been logged. We will review this with other data to try and determine the cause of this lag. Thank you for your help.");
                }
                else
                {

                    from.SendMessage("It has been less than five minutes since you last reported lag. Please wait five minutes between submitting lag reports.");
                }

            }

        }

    }

}