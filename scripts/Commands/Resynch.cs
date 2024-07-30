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

/* Scripts/Commands/Resynch.cs
 * ChangeLog
 *	9/25/04 - Pix.
 *		Added 2 minute time period between uses of the command.
 *	9/16/04 - Pixie
 *		Resurrected and re-structured this command.
 *		Attempting to see if sending a MobileUpdate packet works.
 */


using Server.Mobiles;
using Server.Network;
using System;

namespace Server.Commands
{
    public class ResynchCommand
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Resynch", AccessLevel.Player, new CommandEventHandler(Resynch_OnCommand));
        }

        public static void Resynch_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;

            if (m is PlayerMobile)
            {
                if (((PlayerMobile)m).m_LastResynchTime < (DateTime.UtcNow - TimeSpan.FromMinutes(2.0))
                    || (m.AccessLevel > AccessLevel.Player))
                {
                    m.SendMessage("Resynchronizing server and client.");
                    m.Send(new MobileUpdate(m));
                    ((PlayerMobile)m).m_LastResynchTime = DateTime.UtcNow;
                }
                else
                {
                    m.SendMessage("You must wait to use that command.");
                }
            }

        }

    }
}