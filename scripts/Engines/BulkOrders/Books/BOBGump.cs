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

/* Scripts/Engines/BulkOrders/Books/BOBGump.cs
 * CHANGELOG:
 *  2/10/24, Yoar
 *      Reworked page logic. In order to land on the correct page,
 *      we iteratively turn the book's pages 'page' number of times.
 */

using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Prompts;
using System;
using System.Collections;

namespace Server.Engines.BulkOrders
{
    public class BOBGump : Gump
    {
        private PlayerMobile m_From;
        private BulkOrderBook m_Book;
        private ArrayList m_List;

        private int m_Page;
        private bool m_Next;

        private const int LabelColor = 0x7FFF;

        public Item Reconstruct(object obj)
        {
            Item item = null;

            if (obj is BOBLargeEntry)
                item = ((BOBLargeEntry)obj).Reconstruct();
            else if (obj is BOBSmallEntry)
                item = ((BOBSmallEntry)obj).Reconstruct();

            return item;
        }

        public bool CheckFilter(object obj)
        {
            if (obj is BOBLargeEntry)
            {
                BOBLargeEntry e = (BOBLargeEntry)obj;

                return CheckFilter(e.Material, e.AmountMax, true, e.RequireExceptional, e.DeedType, (e.Entries.Length > 0 ? e.Entries[0].ItemType : null));
            }
            else if (obj is BOBSmallEntry)
            {
                BOBSmallEntry e = (BOBSmallEntry)obj;

                return CheckFilter(e.Material, e.AmountMax, false, e.RequireExceptional, e.DeedType, e.ItemType);
            }

            return false;
        }

        public bool CheckFilter(BulkMaterialType mat, int amountMax, bool isLarge, bool reqExc, BODType deedType, Type itemType)
        {
            BOBFilter f = (m_From.UseOwnFilter ? m_From.BOBFilter : m_Book.Filter);

            if (f.IsDefault)
                return true;

            if (f.Quality == 1 && reqExc)
                return false;
            else if (f.Quality == 2 && !reqExc)
                return false;

            if (f.Quantity == 1 && amountMax != 10)
                return false;
            else if (f.Quantity == 2 && amountMax != 15)
                return false;
            else if (f.Quantity == 3 && amountMax != 20)
                return false;

            if (f.Type == 1 && isLarge)
                return false;
            else if (f.Type == 2 && !isLarge)
                return false;

            switch (f.Material)
            {
                default:
                case 0: return true;
                case 1: return (deedType == BODType.Smith);
                case 2: return (deedType == BODType.Tailor);

                case 3: return (mat == BulkMaterialType.None && BGTClassifier.Classify(deedType, itemType) == BulkGenericType.Iron);
                case 4: return (mat == BulkMaterialType.DullCopper);
                case 5: return (mat == BulkMaterialType.ShadowIron);
                case 6: return (mat == BulkMaterialType.Copper);
                case 7: return (mat == BulkMaterialType.Bronze);
                case 8: return (mat == BulkMaterialType.Gold);
                case 9: return (mat == BulkMaterialType.Agapite);
                case 10: return (mat == BulkMaterialType.Verite);
                case 11: return (mat == BulkMaterialType.Valorite);

                case 12: return (mat == BulkMaterialType.None && BGTClassifier.Classify(deedType, itemType) == BulkGenericType.Cloth);
                case 13: return (mat == BulkMaterialType.None && BGTClassifier.Classify(deedType, itemType) == BulkGenericType.Leather);
                case 14: return (mat == BulkMaterialType.Spined);
                case 15: return (mat == BulkMaterialType.Horned);
                case 16: return (mat == BulkMaterialType.Barbed);

                case 17: return (deedType == BODType.Carpenter);
                case 18: return (mat == BulkMaterialType.None && BGTClassifier.Classify(deedType, itemType) == BulkGenericType.Wood);
                case 19: return (mat == BulkMaterialType.Oak);
                case 20: return (mat == BulkMaterialType.Ash);
                case 21: return (mat == BulkMaterialType.Yew);
                case 22: return (mat == BulkMaterialType.Heartwood);
                case 23: return (mat == BulkMaterialType.Bloodwood);
                case 24: return (mat == BulkMaterialType.Frostwood);
            }
        }

        public const int LinesPerPage = 10;

        private void TurnPage(int count)
        {
            ArrayList list = new ArrayList();
            int page = 0;
            int line = 0;
            bool next = false;

            for (int i = 0; i < m_Book.Entries.Count; i++)
            {
                object obj = m_Book.Entries[i];

                if (!CheckFilter(obj))
                    continue;

                int size;

                if (obj is BOBLargeEntry)
                    size = ((BOBLargeEntry)obj).Entries.Length;
                else
                    size = 1;

                if (line != 0 && line + size > LinesPerPage)
                {
                    if (page == count)
                    {
                        next = true;
                        break;
                    }

                    list.Clear();

                    page++;
                    line = 0;
                }

                line += size;

                list.Add(obj);
            }

            m_List = list;
            m_Page = page;
            m_Next = next;
        }

        public object GetMaterialName(BulkMaterialType mat, BODType type, Type itemType)
        {
            switch (type)
            {
                case BODType.Smith:
                case BODType.Tinker:
                    {
                        switch (mat)
                        {
                            case BulkMaterialType.None: return 1062226;
                            case BulkMaterialType.DullCopper: return 1018332;
                            case BulkMaterialType.ShadowIron: return 1018333;
                            case BulkMaterialType.Copper: return 1018334;
                            case BulkMaterialType.Bronze: return 1018335;
                            case BulkMaterialType.Gold: return 1018336;
                            case BulkMaterialType.Agapite: return 1018337;
                            case BulkMaterialType.Verite: return 1018338;
                            case BulkMaterialType.Valorite: return 1018339;
                        }

                        break;
                    }
                case BODType.Tailor:
                    {
                        switch (mat)
                        {
                            case BulkMaterialType.None:
                                {
                                    if (itemType.IsSubclassOf(typeof(BaseArmor)) || itemType.IsSubclassOf(typeof(BaseShoes)))
                                        return 1062235;

                                    return 1044286;
                                }
                            case BulkMaterialType.Spined: return 1062236;
                            case BulkMaterialType.Horned: return 1062237;
                            case BulkMaterialType.Barbed: return 1062238;
                        }

                        break;
                    }
                case BODType.Carpenter:
                case BODType.Fletcher:
                    {
                        switch (mat)
                        {
                            case BulkMaterialType.None: return "Wood";
                            case BulkMaterialType.Oak: return "Oak";
                            case BulkMaterialType.Ash: return "Ash";
                            case BulkMaterialType.Yew: return "Yew";
                            case BulkMaterialType.Heartwood: return "Heartwood";
                            case BulkMaterialType.Bloodwood: return "Bloodwood";
                            case BulkMaterialType.Frostwood: return "Frostwood";
                        }

                        break;
                    }
            }

            return "Invalid";
        }

        public BOBGump(PlayerMobile from, BulkOrderBook book)
            : this(from, book, 0)
        {
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            int index = info.ButtonID;

            switch (index)
            {
                case 0: // EXIT
                    {
                        break;
                    }
                case 1: // Set Filter
                    {
                        m_From.SendGump(new BOBFilterGump(m_From, m_Book));

                        break;
                    }
                case 2: // Previous page
                    {
                        if (m_Page > 0)
                            m_From.SendGump(new BOBGump(m_From, m_Book, m_Page - 1));

                        return;
                    }
                case 3: // Next page
                    {
                        if (m_Next)
                            m_From.SendGump(new BOBGump(m_From, m_Book, m_Page + 1));

                        break;
                    }
                default:
                    {
                        bool canDrop = m_Book.IsChildOf(m_From.Backpack);
                        bool canPrice = canDrop || (m_Book.RootParent is PlayerVendor);

                        index -= 4;

                        int type = index % 2;
                        index /= 2;

                        if (index < 0 || index >= m_List.Count)
                            break;

                        object obj = m_List[index];

                        if (!m_Book.Entries.Contains(obj))
                        {
                            m_From.SendLocalizedMessage(1062382); // The deed selected is not available.
                            break;
                        }

                        if (type == 0) // Drop
                        {
                            if (m_Book.IsChildOf(m_From.Backpack))
                            {
                                Item item = Reconstruct(obj);

                                if (item != null)
                                {
                                    m_From.AddToBackpack(item);
                                    m_From.SendLocalizedMessage(1045152); // The bulk order deed has been placed in your backpack.

                                    m_Book.Entries.Remove(obj);
                                    m_Book.InvalidateProperties();

                                    if (m_Book.Entries.Count > 0)
                                        m_From.SendGump(new BOBGump(m_From, m_Book, m_Page));
                                    else
                                        m_From.SendLocalizedMessage(1062381); // The book is empty.
                                }
                                else
                                {
                                    m_From.SendMessage("Internal error. The bulk order deed could not be reconstructed.");
                                }
                            }
                        }
                        else // Set Price | Buy
                        {
                            if (m_Book.IsChildOf(m_From.Backpack))
                            {
                                m_From.Prompt = new SetPricePrompt(m_Book, obj, m_Page, m_List);
                                m_From.SendLocalizedMessage(1062383); // Type in a price for the deed:
                            }
                            else if (m_Book.RootParent is PlayerVendor)
                            {
                                PlayerVendor pv = (PlayerVendor)m_Book.RootParent;

                                VendorItem vi = pv.SellItems[m_Book] as VendorItem;

                                int price = 0;

                                if (vi != null && !vi.IsForSale)
                                {
                                    if (obj is BOBLargeEntry)
                                        price = ((BOBLargeEntry)obj).Price;
                                    else if (obj is BOBSmallEntry)
                                        price = ((BOBSmallEntry)obj).Price;
                                }

                                if (price == 0)
                                    m_From.SendLocalizedMessage(1062382); // The deed selected is not available.
                                else
                                    m_From.SendGump(new BODBuyGump(m_From, m_Book, obj, price));
                            }
                        }

                        break;
                    }
            }
        }

        private class SetPricePrompt : Prompt
        {
            private BulkOrderBook m_Book;
            private object m_Object;
            private int m_Page;
            private ArrayList m_List;

            public SetPricePrompt(BulkOrderBook book, object obj, int page, ArrayList list)
            {
                m_Book = book;
                m_Object = obj;
                m_Page = page;
                m_List = list;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (!m_Book.Entries.Contains(m_Object))
                {
                    from.SendLocalizedMessage(1062382); // The deed selected is not available.
                    return;
                }

                int price = Utility.ToInt32(text);

                if (price < 0 || price > 250000000)
                {
                    from.SendLocalizedMessage(1062390); // The price you requested is outrageous!
                }
                else if (m_Object is BOBLargeEntry)
                {
                    ((BOBLargeEntry)m_Object).Price = price;

                    from.SendLocalizedMessage(1062384); // Deed price set.

                    if (from is PlayerMobile)
                        from.SendGump(new BOBGump((PlayerMobile)from, m_Book, m_Page));
                }
                else if (m_Object is BOBSmallEntry)
                {
                    ((BOBSmallEntry)m_Object).Price = price;

                    from.SendLocalizedMessage(1062384); // Deed price set.

                    if (from is PlayerMobile)
                        from.SendGump(new BOBGump((PlayerMobile)from, m_Book, m_Page));
                }
            }
        }

        public BOBGump(PlayerMobile from, BulkOrderBook book, int page)
            : base(12, 24)
        {
            from.CloseGump(typeof(BOBGump));
            from.CloseGump(typeof(BOBFilterGump));

            m_From = from;
            m_Book = book;

            TurnPage(page);

            PlayerVendor pv = book.RootParent as PlayerVendor;

            bool canDrop = book.IsChildOf(from.Backpack);
            bool canBuy = (pv != null);
            bool canPrice = (canDrop || canBuy);

            if (canBuy)
            {
                VendorItem vi = pv.SellItems[book] as VendorItem;

                canBuy = (vi != null && !vi.IsForSale);
            }

            int width = 600;

            if (!canPrice)
                width = 516;

            X = (624 - width) / 2;

            AddPage(0);

            AddBackground(10, 10, width, 439, 5054);
            AddImageTiled(18, 20, width - 17, 420, 2624);

            if (canPrice)
            {
                AddImageTiled(573, 64, 24, 352, 200);
                AddImageTiled(493, 64, 78, 352, 1416);
            }

            if (canDrop)
                AddImageTiled(24, 64, 32, 352, 1416);

            AddImageTiled(58, 64, 36, 352, 200);
            AddImageTiled(96, 64, 133, 352, 1416);
            AddImageTiled(231, 64, 80, 352, 200);
            AddImageTiled(313, 64, 100, 352, 1416);
            AddImageTiled(415, 64, 76, 352, 200);

            AddAlphaRegion(18, 20, width - 17, 420);
            AddImage(5, 5, 10460);
            AddImage(width - 15, 5, 10460);
            AddImage(5, 424, 10460);
            AddImage(width - 15, 424, 10460);

            AddHtmlLocalized(canPrice ? 266 : 224, 32, 200, 32, 1062220, LabelColor, false, false); // Bulk Order Book
            AddHtmlLocalized(63, 64, 200, 32, 1062213, LabelColor, false, false); // Type
            AddHtmlLocalized(147, 64, 200, 32, 1062214, LabelColor, false, false); // Item
            AddHtmlLocalized(246, 64, 200, 32, 1062215, LabelColor, false, false); // Quality
            AddHtmlLocalized(336, 64, 200, 32, 1062216, LabelColor, false, false); // Material
            AddHtmlLocalized(429, 64, 200, 32, 1062217, LabelColor, false, false); // Amount

            AddButton(35, 32, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(70, 32, 200, 32, 1062476, LabelColor, false, false); // Set Filter

            BOBFilter f = (from.UseOwnFilter ? from.BOBFilter : book.Filter);

            if (f.IsDefault)
                AddHtmlLocalized(canPrice ? 470 : 386, 32, 120, 32, 1062475, 16927, false, false); // Using No Filter
            else if (from.UseOwnFilter)
                AddHtmlLocalized(canPrice ? 470 : 386, 32, 120, 32, 1062451, 16927, false, false); // Using Your Filter
            else
                AddHtmlLocalized(canPrice ? 470 : 386, 32, 120, 32, 1062230, 16927, false, false); // Using Book Filter

            AddButton(375, 416, 4017, 4018, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(410, 416, 120, 20, 1011441, LabelColor, false, false); // EXIT

            if (canDrop)
                AddHtmlLocalized(26, 64, 50, 32, 1062212, LabelColor, false, false); // Drop

            if (canPrice)
            {
                AddHtmlLocalized(516, 64, 200, 32, 1062218, LabelColor, false, false); // Price

                if (canBuy)
                    AddHtmlLocalized(576, 64, 200, 32, 1062219, LabelColor, false, false); // Buy
                else
                    AddHtmlLocalized(576, 64, 200, 32, 1062227, LabelColor, false, false); // Set
            }

            if (page > 0)
            {
                AddButton(75, 416, 4014, 4016, 2, GumpButtonType.Reply, 0);
                AddHtmlLocalized(110, 416, 150, 20, 1011067, LabelColor, false, false); // Previous page
            }

            if (m_Next)
            {
                AddButton(225, 416, 4005, 4007, 3, GumpButtonType.Reply, 0);
                AddHtmlLocalized(260, 416, 150, 20, 1011066, LabelColor, false, false); // Next page
            }

            int y = 96;

            for (int i = 0; i < m_List.Count; i++)
            {
                object obj = m_List[i];

                AddImageTiled(24, y - 2, canPrice ? 573 : 489, 2, 2624);

                if (obj is BOBLargeEntry)
                {
                    BOBLargeEntry e = (BOBLargeEntry)obj;

                    if (canDrop)
                        AddButton(35, y + 2, 5602, 5606, 4 + (i * 2), GumpButtonType.Reply, 0);

                    if (canDrop || (canBuy && e.Price > 0))
                    {
                        AddButton(579, y + 2, 2117, 2118, 5 + (i * 2), GumpButtonType.Reply, 0);
                        AddLabel(495, y, 1152, e.Price.ToString());
                    }

                    AddHtmlLocalized(61, y, 50, 32, 1062225, LabelColor, false, false); // Large

                    for (int j = 0; j < e.Entries.Length; ++j)
                    {
                        BOBLargeSubEntry sub = e.Entries[j];

                        AddHtmlLocalized(103, y, 130, 32, sub.Number, LabelColor, false, false);

                        if (e.RequireExceptional)
                            AddHtmlLocalized(235, y, 80, 20, 1060636, LabelColor, false, false); // exceptional
                        else
                            AddHtmlLocalized(235, y, 80, 20, 1011542, LabelColor, false, false); // normal

                        object name = GetMaterialName(e.Material, e.DeedType, sub.ItemType);

                        if (name is int)
                            AddHtmlLocalized(316, y, 100, 20, (int)name, LabelColor, false, false);
                        else if (name is string)
                            AddLabel(316, y, 1152, (string)name);

                        AddLabel(421, y, 1152, String.Format("{0} / {1}", sub.AmountCur, e.AmountMax));

                        y += 32;
                    }
                }
                else if (obj is BOBSmallEntry)
                {
                    BOBSmallEntry e = (BOBSmallEntry)obj;

                    if (canDrop)
                        AddButton(35, y + 2, 5602, 5606, 4 + (i * 2), GumpButtonType.Reply, 0);

                    if (canDrop || (canBuy && e.Price > 0))
                    {
                        AddButton(579, y + 2, 2117, 2118, 5 + (i * 2), GumpButtonType.Reply, 0);
                        AddLabel(495, y, 1152, e.Price.ToString());
                    }

                    AddHtmlLocalized(61, y, 50, 32, 1062224, LabelColor, false, false); // Small

                    AddHtmlLocalized(103, y, 130, 32, e.Number, LabelColor, false, false);

                    if (e.RequireExceptional)
                        AddHtmlLocalized(235, y, 80, 20, 1060636, LabelColor, false, false); // exceptional
                    else
                        AddHtmlLocalized(235, y, 80, 20, 1011542, LabelColor, false, false); // normal

                    object name = GetMaterialName(e.Material, e.DeedType, e.ItemType);

                    if (name is int)
                        AddHtmlLocalized(316, y, 100, 20, (int)name, LabelColor, false, false);
                    else if (name is string)
                        AddLabel(316, y, 1152, (string)name);

                    AddLabel(421, y, 1152, String.Format("{0} / {1}", e.AmountCur, e.AmountMax));

                    y += 32;
                }
            }
        }
    }
}