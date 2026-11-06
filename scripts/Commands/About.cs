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

/* Scripts/Commands/About.cs
 * 	CHANGELOG:
 * 	11/13/22, Adam
 *	    Initial Version
 */

using Server.Misc;
using System;

namespace Server.Commands
{
    public class About
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("About", AccessLevel.Player, new CommandEventHandler(About_OnCommand));
        }

        [Usage("About")]
        [Description("Display shard information.")]
        public static void About_OnCommand(CommandEventArgs e)
        {
            try
            {

                string serverName = Core.Server;

                if (Core.ServerID == ServerID.Seasons)
                    serverName = string.Format("{0} S{1}", serverName, Core.SeasonVariant);

                string phase = Core.ReleasePhase < ReleasePhase.Production
                    ? string.Format(" ({0})", Utility.GetCustomEnumNames(typeof(ReleasePhase))[(int)Core.ReleasePhase])
                    : "";

                string testCenter = Core.GMNTC_CFG ? " (Test Center)" : "";

                string output = string.Format("{0}{1}{2} - Version {3}.{4}.{5}, Build {6}",
                    serverName,
                    phase,
                    testCenter,
                    Utility.BuildMajor(),
                    Utility.BuildMinor(),
                    Utility.BuildRevision(),
                    Utility.BuildBuild());

                e.Mobile.SendMessage(0x35, output);

                output = string.Format("{0} runs a T2A core, with OSI published enhancements through {1} ({2}).",
                    Core.Server,
                    PublishInfo.Publish, PublishInfo.PublishDate.ToString("dd-MM-yyyy"));

                e.Mobile.SendMessage(0x35, output);

                if (Core.ServerID == ServerID.Seasons)
                {
                    string next = SeasonsHelper.SeasonInfo(SeasonsHelper.SeasonRequest.Reset, "MMM d, yyyy HH:mm");
                    string countdown = SeasonsHelper.SeasonInfo(SeasonsHelper.SeasonRequest.Countdown, "{days}d {hours2}h {minutes2}m");
                    //e.Mobile.SendMessage(0x3C, $"Seasonal wipes • Next: {next} • {countdown} remaining");
                    e.Mobile.SendMessage(0x3C, "This is a seasonal wipe shard.");
                    e.Mobile.SendMessage(0x3C, $"Next wipe: {next}");
                    e.Mobile.SendMessage(0x3E, $"{countdown}");
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
    }
}