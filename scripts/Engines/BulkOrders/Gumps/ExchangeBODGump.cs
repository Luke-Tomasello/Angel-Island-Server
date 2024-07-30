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

/* Scripts/Engines/BulkOrders/ExchangeBODGump.cs
 * CHANGELOG:
 *  11/7/21, Yoar
 *      Initial version.
 */

using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.BulkOrders
{
    public class ExchangeBODGump : Gump
    {
        private BaseVendor m_Vendor;
        private LargeBOD m_Deed;

        public ExchangeBODGump(BaseVendor vendor, LargeBOD deed)
            : base(250, 50)
        {
            Closable = false;
            m_Vendor = vendor;
            m_Deed = deed;

            AddPage(0);

            AddImage(0, 0, 0x1F40);
            AddImageTiled(20, 37, 300, 129, 0x1F42);
            AddImage(20, 147, 0x1F43);

            AddImage(35, 8, 0x39);
            AddImageTiled(65, 8, 257, 10, 0x3A);
            AddImage(290, 8, 0x3B);

            AddImage(32, 33, 0x2635);

            AddHtml(70, 35, 270, 20, "Exchange Bulk Order Deed", false, false);
            AddImageTiled(70, 55, 230, 2, 0x23C5);

            AddHtml(40, 65, 270, 70, "Select <b>OKAY</b> to exhange the large bulk order for a <i>random</i> small bulk order. Select <b>CANCEL</b> to keep the large bulk order.", false, false);

            AddButton(180, 153, 0xF9, 0xF7, 1, GumpButtonType.Reply, 0); // Okay
            AddButton(250, 153, 0xF3, 0xF1, 0, GumpButtonType.Reply, 0); // Cancel
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!from.Alive || m_Vendor.BulkOrderSystem == null || m_Vendor.BulkOrderSystem != m_Deed.System || !m_Deed.IsEmpty())
                return;

            if (info.ButtonID == 1)
                m_Vendor.BulkOrderSystem.Exchange(from, m_Vendor, m_Deed);
        }
    }
}