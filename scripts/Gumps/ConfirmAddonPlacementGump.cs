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
 *  3/11/2024, Adam
 *   Create a robust system for addon previews:
 *   Affected files: BaseAddon.cs, BaseAddonDeed.cs, and ConfirmAddonPlacement.cs
 *   * Remove the kooky CancelPacement logic (unneeded, and wrong as well.)
 *   Auto Cancel placement and return deed if
 *   a. Player died
 *   b. Player disconnected
 *   c. Player's distance to the addon > X
 *   d. Time to place the addon has timed out.
 *   e. if the server saved the preview, and it still exists on restart, delete the addon and return the deed.
 *    Disallow placement if:
 *    the player is currently previewing another addon.
 *  1/17/22, Yoar
 *      Added support for township addons.
 *  9/17/21, Yoar
 *      Added warning to un-redeedable addons.
 *  12/21/06, Kit
 *      Made cancel happen anytime Okay not pushed.
 *	9/19/04
 *		Created by mith
 */

using Server.Items;
using Server.Multis;
using Server.Network;

namespace Server.Gumps
{
    public class ConfirmAddonPlacementGump : Gump
    {
        private BaseAddon m_Addon;
        private BaseHouse m_House;

        public ConfirmAddonPlacementGump(Mobile from, BaseAddon addon, BaseHouse house)
            : base(50, 50)
        {
            from.CloseGump(typeof(ConfirmAddonPlacementGump));

            m_Addon = addon;
            m_House = house;

            AddPage(0);

            AddBackground(10, 10, 190, 140, 0x242C);

            AddHtml(25, 30, 160, 40, "<center>Are you sure you want to place this addon here?</center>", false, false);

            if (!addon.Redeedable)
                AddHtml(25, 66, 160, 40, "<basefont color=#FFFF00><center>** You may not re-deed this addon! **</center></basefont>", false, false);

            AddButton(40, 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
            AddButton(110, 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (m_Addon.Deleted)
                return;

            Mobile from = state.Mobile;

            if (info.ButtonID == 1)
            {
                BaseAddon.RemovePreview(m_Addon, from);

                if (m_House != null)
                    m_House.Addons.Add(m_Addon);

                if (m_House == null)
                    Server.Township.TownshipItemHelper.SetOwnership(m_Addon, from);
            }

            // cancel
            if (info.ButtonID != 1)
            {
                CancelPlacement(from); // TODO: Call BaseAddon.OnChop?
                BaseAddon.RemovePreview(m_Addon, from);
            }
        }

        private void CancelPlacement(Mobile from)
        {
            Item deed = BaseAddon.Redeed(m_Addon);

            if (deed != null)
                from.AddToBackpack(deed);

            m_Addon.Delete();
        }
    }
}