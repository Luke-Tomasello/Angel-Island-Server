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

/* Scripts/Engines/Help/PageEnableCommand.cs
 * CHANGELOG:
 *	3/5/05: Pix
 *		Initial Version.
 */

namespace Server.Commands
{
    public class PageEnableCommand
    {
        public static bool Enabled;

        public static void Initialize()
        {
            PageEnableCommand.Enabled = true;
            Server.CommandSystem.Register("PageEnable", AccessLevel.Administrator, new CommandEventHandler(PageEnable_OnCommand));
        }

        public static void PageEnable_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length > 0)
            {
                if (e.Arguments[0].ToLower() == "on")
                {
                    PageEnableCommand.Enabled = true;
                }
                else if (e.Arguments[0].ToLower() == "off")
                {
                    PageEnableCommand.Enabled = false;
                }
                else
                {
                    e.Mobile.SendMessage("[pageenable takes either 'on' or 'off' as a parameter.");
                }
            }

            e.Mobile.SendMessage("PageEnable is {0}.", PageEnableCommand.Enabled ? "ON" : "OFF");
        }
    }
}