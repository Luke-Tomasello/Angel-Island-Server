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

/* Engines/Township/ConfirmDestroyItemGump.cs
 * CHANGELOG:
 * 2/19/22, Yoar
 *	    Added 'is Scaffold' or 'CanDestroy' checks.
 * 11/23/21, Yoar
 *	    Initial version.
 */

using Server.Gumps;
using Server.Network;

namespace Server.Township
{
    public class ConfirmDestroyItemGump : Gump
    {
        private Item m_Item;

        public ConfirmDestroyItemGump(Item item)
            : base(50, 50)
        {
            m_Item = item;

            AddPage(0);

            AddBackground(10, 10, 190, 140, 0x242C);

            AddHtml(25, 30, 160, 80, "<center>Are you sure you wish to destroy this item?</center>", false, false);

            AddButton(40, 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
            AddButton(110, 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (m_Item.Deleted || !from.InRange(m_Item, 2) || (!(m_Item is Scaffold) && (!(m_Item is ITownshipItem) || !((ITownshipItem)m_Item).CanDestroy(from))))
                return;

            switch (info.ButtonID)
            {
                case 0x1: // okay
                    {
                        from.SendLocalizedMessage(500461); // You destroy the item.

                        Effects.PlaySound(from.Location, from.Map, 0x11C);

                        m_Item.Delete();

                        break;
                    }
            }
        }
    }
}