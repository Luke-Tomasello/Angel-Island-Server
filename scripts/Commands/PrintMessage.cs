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

/* Scripts/Commands/PrintMessage.cs
 * ChangeLog
 *	3/17/05, Adam
 *		First time checkin.
 */

namespace Server.Commands
{
    public class PrintMessage
    {

        public static void Initialize()
        {
            Server.CommandSystem.Register("PrintMessage", AccessLevel.GameMaster, new CommandEventHandler(PrintMessage_OnCommand));
        }

        [Usage("PrintMessage <msg_number>")]
        [Description("Print the localized message associated with msg_number.")]
        private static void PrintMessage_OnCommand(CommandEventArgs arg)
        {
            Mobile from = arg.Mobile;

            if (arg.Length <= 0)
            {
                from.SendMessage("Usage: PrintMessage <msg_number>");
                return;
            }

            // What message do we print
            int message = arg.GetInt32(0);
            from.SendLocalizedMessage(message);
            from.SendMessage("Done.");
        }

    }

}