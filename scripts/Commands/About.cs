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

/* Scripts/Commands/About.cs
 * 	CHANGELOG:
 * 	11/13/22, Adam
 *	    Initial Version
 */

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
                string output = string.Format("{0}{5}{6} - Version {1}.{2}.{3}, Build {4}",
                Core.Server, Utility.BuildMajor(), Utility.BuildMinor(),
                Utility.BuildRevision(), Utility.BuildBuild(),
                Core.ReleasePhase < ReleasePhase.Production ? string.Format(" ({0})",
                Utility.GetCustomEnumNames(typeof(ReleasePhase))[(int)Core.ReleasePhase]) : "",
                Core.UOTC_CFG ? " (Test Center)" : "");

                e.Mobile.SendMessage(0x35, output);

                output = string.Format("{0} runs a T2A core, with OSI published enhancements through {1} ({2}).",
                    Core.Server,
                    PublishInfo.Publish, PublishInfo.PublishDate.ToString("dd-MM-yyyy"));

                e.Mobile.SendMessage(0x35, output);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
    }
}