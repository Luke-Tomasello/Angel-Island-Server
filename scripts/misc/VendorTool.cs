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

/* CHANGELOG
 * Scripts/Misc/VendorTool.cs
 *  06/12/05 TK
 *		Fixed UI issues with page #'s >1
 *  06/09/05 TK
 *		Modified to allow deletion of items for sale.
 *	06/09/05 Taran Kain
 *		First version.
 */

using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.Tools
{
    /// <summary>
    /// Summary description for VendorTool.
    /// </summary>
    public class VendorTool : Item
    {
        [Constructable]
        public VendorTool()
            : base(0x1EB8)
        {
            //
            // TODO: Add constructor logic here
            //
            this.Weight = 1.0;
            this.Name = "vendor tool";
        }

        public VendorTool(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendMessage("This item is for GameMasters only. It will now self-destruct.");
                this.Delete();
            }
            from.SendMessage("Target the vendor to list the inventory of.");
            from.Target = new VendorToolTarget();
        }
    }

    class VendorToolTarget : Target
    {
        public VendorToolTarget()
            : base(8, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object o)
        {
            if (!(o is PlayerVendor))
            {
                from.SendMessage("This tool is for use on player vendors.");
                return;
            }

            from.SendGump(new VendorToolGump(from, (PlayerVendor)o));
        }
    }

    class VendorToolGump : Gump
    {
        private PlayerVendor m_Vendor;
        private int m_Page;

        public VendorToolGump(Mobile from, PlayerVendor pv)
            : this(from, pv, 0)
        {
        }

        public VendorToolGump(Mobile from, PlayerVendor pv, int page)
            : base(50, 40)
        {
            // make sure we don't crash shit..
            try
            {
                from.CloseGump(typeof(VendorToolGump));

                m_Vendor = pv;
                m_Page = page;
                ArrayList values = new ArrayList(m_Vendor.SellItems.Values);

                AddPage(0);

                AddImageTiled(0, 0, 430, 508, 0xA40);
                AddAlphaRegion(1, 1, 428, 506);

                AddHtml(0, 0, 428, 22, "<basefont color=#FFFFFF><center>Vendor Inventory</center></basefont>", false, false);
                AddHtml(2, 22, 200, 16, "<basefont color=#ffffff>Description</basefont>", false, false);
                AddHtml(203, 22, 50, 16, "<basefont color=#ffffff>Price</basefont>", false, false);
                AddHtml(254, 22, 110, 16, "<basefont color=#ffffff>Item</basefont>", false, false);
                for (int i = page * 20; i < page * 20 + 20 && i < m_Vendor.SellItems.Count; i++)
                {
                    VendorItem vi = values[i] as VendorItem;
                    if (vi == null)
                        continue;
                    AddHtml(2, 44 + 22 * (i % 20), 200, 16, String.Format("<basefont color=#ffffff>{0}</basefont>", vi.Description), false, false);
                    AddHtml(203, 44 + 22 * (i % 20), 50, 16, String.Format("<basefont color=#ffffff>{0}</basefont>", (vi.IsForSale ? vi.Price.ToString() : "NFS")), false, false);
                    string typename;
                    if (vi.Item == null)
                        typename = "null";
                    else
                    {
                        typename = vi.Item.GetType().ToString();
                        typename = typename.Substring(typename.LastIndexOf(".") + 1);
                    }
                    AddHtml(254, 44 + 22 * (i % 20), 110, 16, String.Format("<basefont color=#ffffff>{0}</basefont>", typename), false, false);
                    AddButton(369, 44 + 22 * (i % 20), 0xFB1, 0xFB3, 4 + 2 * i, GumpButtonType.Reply, 0);
                    AddButton(399, 44 + 22 * (i % 20), 0xFA5, 0xFA7, 5 + 2 * i, GumpButtonType.Reply, 0);
                }
                AddButton(1, 485, 0xFB1, 0xFB3, 3, GumpButtonType.Reply, 0);
                AddHtml(35, 485, 100, 22, "<basefont color=#ffffff>Clear Vendor</basefont>", false, false);
                AddButton(200, 485, 0xFB7, 0xFB9, 0, GumpButtonType.Reply, 0); // ok
                if (m_Page > 0)
                    AddButton(339, 485, 0xFAE, 0xFB0, 1, GumpButtonType.Reply, 0); // back
                if (m_Vendor.SellItems.Count >= 20 * m_Page)
                    AddButton(369, 485, 0xFA5, 0xFA7, 2, GumpButtonType.Reply, 0); // next
            }
            catch
            {
                from.SendMessage("An error occurred while listing the vendor contents.");
            }
        }

        public override void OnResponse(NetState ns, RelayInfo ri)
        {
            if (ri.ButtonID == 0)
                return;
            if (ri.ButtonID == 1)
            {
                ns.Mobile.SendGump(new VendorToolGump(ns.Mobile, m_Vendor, m_Page - 1));
                return;
            }
            if (ri.ButtonID == 2)
            {
                ns.Mobile.SendGump(new VendorToolGump(ns.Mobile, m_Vendor, m_Page + 1));
                return;
            }
            if (ri.ButtonID == 3)
            {
                // clear vendor
                if (m_Vendor.Backpack.Items.Count != m_Vendor.SellItems.Count)
                    ns.Mobile.SendMessage("Discrepancy between Backpack and SellItem list. Backpack: {0} items. SellItem list: {1} items.", m_Vendor.Backpack.Items.Count, m_Vendor.SellItems.Count);
                ns.Mobile.SendMessage("Clearing vendor of all items. Leaving bank intact.");
                m_Vendor.Backpack.Items.Clear();
                m_Vendor.SellItems.Clear();
                ns.Mobile.SendGump(new VendorToolGump(ns.Mobile, m_Vendor));
                return;
            }

            if (ri.ButtonID % 2 == 1)
            {
                try
                {
                    ArrayList values = new ArrayList(m_Vendor.SellItems.Values);
                    ns.Mobile.SendGump(new VendorToolGump(ns.Mobile, m_Vendor, m_Page));
                    ns.Mobile.SendGump(new PropertiesGump(ns.Mobile, ((VendorItem)values[(ri.ButtonID - 5) / 2]).Item));
                }
                catch
                {
                    ns.Mobile.SendMessage("An error occurred while opening the properties gump for that item.");
                }
            }
            else
            {
                try
                {
                    ArrayList keys = new ArrayList(m_Vendor.SellItems.Keys);
                    Item key = keys[(ri.ButtonID - 4) / 2] as Item;
                    m_Vendor.SellItems.Remove(key);
                    key.Delete();
                    ns.Mobile.SendGump(new VendorToolGump(ns.Mobile, m_Vendor, m_Page));
                    ns.Mobile.SendMessage("Item deleted.");
                }
                catch
                {
                    ns.Mobile.SendMessage("An error occurred while trying to delete the item.");
                }
            }
        }
    }
}