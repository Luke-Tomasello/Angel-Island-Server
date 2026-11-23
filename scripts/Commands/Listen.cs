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

/* Scripts/Commands/Listen.cs
 * CHANGELOG:
 *  11/22/26, Adam (Listener music boxes)
 *      [listen by an Administrator also turns on (or off, the second time) listening to the Listener music boxes serving
 *      that player: near such a box they hear its notes and see its lyrics and replies, as the player does (AIMS asks
 *      AimsHost.Overhears). It follows the player, not the box; the player isn't told; nothing is saved. Party and guild
 *      listening are unchanged (GameMaster).
 *	7/11/05 - Pix
 *		Initial Version
 */

using Server.Mobiles;
using Server.Targeting;
using System.Collections.Generic;

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

                if (from.AccessLevel >= AccessLevel.Administrator && obj is PlayerMobile player && player != from)
                    from.SendMessage(ToggleMusic(from, player)
                        ? "You are now listening to their Listener music boxes (stay near the box to hear it)."
                        : "You are no longer listening to their Listener music boxes.");
            }
        }

        #region Listener music boxes
        // staff -> the players whose Listener music boxes they hear; not saved
        private static readonly Dictionary<Mobile, HashSet<Mobile>> m_Music = new();

        /// <summary>Turns listening to the player's Listener music boxes on or off; true when it's on now.</summary>
        public static bool ToggleMusic(Mobile staff, Mobile player)
        {
            // forget the deleted (staff and players)
            foreach (Mobile gone in new List<Mobile>(m_Music.Keys))
                if (gone.Deleted)
                    m_Music.Remove(gone);

            if (!m_Music.TryGetValue(staff, out HashSet<Mobile>? players))
                m_Music[staff] = players = new HashSet<Mobile>();
            players.RemoveWhere(m => m.Deleted);

            bool on = players.Add(player);
            if (!on)
                players.Remove(player);
            if (players.Count == 0)
                m_Music.Remove(staff);
            return on;
        }

        /// <summary>Does staff hear a Listener music box serving the player? Asked for every note: nothing to look up unless someone is listening.</summary>
        public static bool Overhears(Mobile staff, Mobile? player)
        {
            return m_Music.Count > 0 && player != null && staff.AccessLevel >= AccessLevel.Administrator
                && m_Music.TryGetValue(staff, out HashSet<Mobile>? players) && players.Contains(player);
        }
        #endregion Listener music boxes

    }
}