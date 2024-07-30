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

/* Gumps/ConfirmAddonPlacement.cs
 * ChangeLog
 *	10/12/04
 *		Created by Darva
 */

using Server.Items;
using Server.Network;
using System;

namespace Server.Gumps
{
    public class ConfirmFlagstoneUseGump : Gump
    {
        private AddonComponent m_Addon;

        public ConfirmFlagstoneUseGump(Mobile from, AddonComponent addon)
            : base(50, 50)
        {
            from.CloseGump(typeof(ConfirmFlagstoneUseGump));

            AddPage(0);

            AddBackground(10, 10, 190, 140, 0x242C);

            AddHtml(30, 30, 150, 75, String.Format("<div align=CENTER>{0}</div>", "Do you wish to open yourself to attacks?"), false, false);

            AddButton(40, 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
            AddButton(110, 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
            m_Addon = addon;
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            if (info.ButtonID == 1)
            {
                if (from.InRange(m_Addon, 2))
                {
                    from.Criminal = true;
                    from.Say("*Prepares for combat*");
                }
                else
                {
                    from.SendMessage("You must remain closer to the stone.");
                }
            }
        }
    }
}