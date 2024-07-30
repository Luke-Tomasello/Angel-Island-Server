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

/* Scripts/Gumps/Guilds/MoveGuildstoneGump.cs
 * ChangeLog
 *	3/10/05, mith
 *		Script Created. Called from Guildstone.OnDoubleClick()
 */

using Server.Items;
using Server.Network;
using System;

namespace Server.Gumps
{
    public class MoveGuildstoneGump : Gump
    {
        private Guildstone m_Stone;

        public MoveGuildstoneGump(Mobile from, Guildstone stone)
            : base(50, 50)
        {
            m_Stone = stone;

            AddPage(0);

            AddBackground(10, 10, 190, 140, 0x242C);

            AddHtml(30, 30, 150, 75, String.Format("<div align=CENTER>{0}</div>", "Are you sure you want to re-deed this guildstone?"), false, false);

            AddButton(40, 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
            AddButton(110, 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (m_Stone.Deleted)
                return;

            Mobile from = state.Mobile;

            if (info.ButtonID == 1)
                m_Stone.OnPrepareMove(from);
        }
    }
}