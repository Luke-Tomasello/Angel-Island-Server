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

/* Scripts/Commands/Listen.cs
 * CHANGELOG:
 *	7/11/05 - Pix
 *		Initial Version
 */

using Server.Targeting;

namespace Server.Commands
{
    /// <summary>
    /// Summary description for Listen.
    /// </summary>
    public class Listen
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Listen", AccessLevel.GameMaster, new CommandEventHandler(Listen_OnCommand));
        }

        public static void Listen_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(Listen_OnTarget));
            e.Mobile.SendMessage("Target a player.");
        }

        public static void Listen_OnTarget(Mobile from, object obj)
        {
            if (obj is Mobile)
            {
                Server.Engines.PartySystem.Party.ListenToParty_OnTarget(from, obj);
                Server.Guilds.Guild.ListenToGuild_OnTarget(from, obj);
            }
        }

    }
}