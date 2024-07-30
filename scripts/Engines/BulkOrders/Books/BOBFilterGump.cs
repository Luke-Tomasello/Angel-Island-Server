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

/* Scripts/Engines/BulkOrders/Books/BOBFilterGump.cs
 * CHANGELOG:
 *  2/10/24, Yoar
 *      Added filters for carpentry BODs.
 */

using Server.Gumps;
using Server.Mobiles;

namespace Server.Engines.BulkOrders
{
    public class BOBFilterGump : Gump
    {
        private PlayerMobile m_From;
        private BulkOrderBook m_Book;

        private const int LabelColor = 0x7FFF;

        private static FilterEntry[] m_MaterialFilters = new FilterEntry[]
            {
                new FilterEntry(1044067,  1), // Blacksmithy
				new FilterEntry(1062226,  3), // Iron
				new FilterEntry(1018332,  4), // Dull Copper
				new FilterEntry(1018333,  5), // Shadow Iron
				new FilterEntry(1018334,  6), // Copper
				new FilterEntry(1018335,  7), // Bronze

				new FilterEntry(      0,  0), // --Blank--
				new FilterEntry(1018336,  8), // Golden
				new FilterEntry(1018337,  9), // Agapite
				new FilterEntry(1018338, 10), // Verite
				new FilterEntry(1018339, 11), // Valorite
				new FilterEntry(      0,  0), // --Blank--

				new FilterEntry(1044094,  2), // Tailoring
				new FilterEntry(1044286, 12), // Cloth
				new FilterEntry(1062235, 13), // Leather
				new FilterEntry(1062236, 14), // Spined
				new FilterEntry(1062237, 15), // Horned
				new FilterEntry(1062238, 16), // Barbed
                
                new FilterEntry(1044071, 17), // Carpentry
                new FilterEntry(1079435, 18), // Wood
                new FilterEntry("Oak", 19),
                new FilterEntry("Ash", 20),
                new FilterEntry("Yew", 21),
                new FilterEntry(0, 0), // --Blank--

                new FilterEntry(0, 0), // --Blank--
                new FilterEntry("Heartwood", 22),
                new FilterEntry("Bloodwood", 23),
                new FilterEntry("Frostwood", 24),
                new FilterEntry(0, 0), // --Blank--
                new FilterEntry(0, 0), // --Blank--
			};

        private static FilterEntry[] m_TypeFilters = new FilterEntry[]
            {
                new FilterEntry(1062229, 0), // All
				new FilterEntry(1062224, 1), // Small
				new FilterEntry(1062225, 2)  // Large
			};

        private static FilterEntry[] m_QualityFilters = new FilterEntry[]
            {
                new FilterEntry(1062229, 0), // All
				new FilterEntry(1011542, 1), // Normal
				new FilterEntry(1060636, 2)  // Exceptional
			};

        private static FilterEntry[] m_AmountFilters = new FilterEntry[]
            {
                new FilterEntry(1062229, 0), // All
				new FilterEntry(1049706, 1), // 10
				new FilterEntry(1016007, 2), // 15
				new FilterEntry(1062239, 3)  // 20
			};

        private static FilterEntry[][] m_Filters = new FilterEntry[][]
            {
                m_TypeFilters,
                m_QualityFilters,
                m_MaterialFilters,
                m_AmountFilters
            };

        private static int[] m_XOffsets_Type = new int[] { 0, 75, 170 };
        private static int[] m_XOffsets_Quality = new int[] { 0, 75, 170 };
        private static int[] m_XOffsets_Amount = new int[] { 0, 75, 180, 275 };
        private static int[] m_XOffsets_Material = new int[] { 0, 105, 210, 305, 390, 485 };

        private static int[] m_XWidths_Small = new int[] { 50, 50, 70, 50 };
        private static int[] m_XWidths_Large = new int[] { 80, 50, 50, 50, 50, 50 };

        private void AddFilterList(int x, int y, int[] xOffsets, int yOffset, FilterEntry[] filters, int[] xWidths, int filterValue, int filterIndex)
        {
            for (int i = 0; i < filters.GetLength(0); ++i)
            {
                TextEntry label = filters[i].Label;

                if (label == TextEntry.Empty)
                    continue;

                bool isSelected = (filters[i].Value == filterValue);

                if (!isSelected && (i % xOffsets.Length) == 0)
                    isSelected = (filterValue == 0);

                TextEntry.AddHtmlText(this, x + 35 + xOffsets[i % xOffsets.Length], y + ((i / xOffsets.Length) * yOffset), xWidths[i % xOffsets.Length], 32, label, false, false, isSelected ? 16927 : LabelColor, isSelected ? 0x8484FF : 0xFFFFFF);
                AddButton(x + xOffsets[i % xOffsets.Length], y + ((i / xOffsets.Length) * yOffset), 4005, 4007, 4 + filterIndex + (i * 4), GumpButtonType.Reply, 0);
            }
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            BOBFilter f = (m_From.UseOwnFilter ? m_From.BOBFilter : m_Book.Filter);

            int index = info.ButtonID;

            switch (index)
            {
                case 0: // Apply
                    {
                        m_From.SendGump(new BOBGump(m_From, m_Book));

                        break;
                    }
                case 1: // Set Book Filter
                    {
                        m_From.UseOwnFilter = false;
                        m_From.SendGump(new BOBFilterGump(m_From, m_Book));

                        break;
                    }
                case 2: // Set Your Filter
                    {
                        m_From.UseOwnFilter = true;
                        m_From.SendGump(new BOBFilterGump(m_From, m_Book));

                        break;
                    }
                case 3: // Clear Filter
                    {
                        f.Clear();
                        m_From.SendGump(new BOBFilterGump(m_From, m_Book));

                        break;
                    }
                default:
                    {
                        index -= 4;

                        int type = index % 4;
                        index /= 4;

                        if (type >= 0 && type < m_Filters.Length)
                        {
                            FilterEntry[] filters = m_Filters[type];

                            if (index >= 0 && index < filters.GetLength(0))
                            {
                                if (filters[index].Label == TextEntry.Empty)
                                    break;

                                switch (type)
                                {
                                    case 0: f.Type = filters[index].Value; break;
                                    case 1: f.Quality = filters[index].Value; break;
                                    case 2: f.Material = filters[index].Value; break;
                                    case 3: f.Quantity = filters[index].Value; break;
                                }

                                m_From.SendGump(new BOBFilterGump(m_From, m_Book));
                            }
                        }

                        break;
                    }
            }
        }

        public BOBFilterGump(PlayerMobile from, BulkOrderBook book)
            : base(12, 24)
        {
            from.CloseGump(typeof(BOBGump));
            from.CloseGump(typeof(BOBFilterGump));

            m_From = from;
            m_Book = book;

            BOBFilter f = (from.UseOwnFilter ? from.BOBFilter : book.Filter);

            AddPage(0);

            AddBackground(10, 10, 600, 519, 5054);

            AddImageTiled(18, 20, 583, 500, 2624);
            AddAlphaRegion(18, 20, 583, 500);

            AddImage(5, 5, 10460);
            AddImage(585, 5, 10460);
            AddImage(5, 504, 10460);
            AddImage(585, 504, 10460);

            AddHtmlLocalized(270, 32, 200, 32, 1062223, LabelColor, false, false); // Filter Preference

            AddHtmlLocalized(26, 64, 120, 32, 1062228, LabelColor, false, false); // Bulk Order Type
            AddFilterList(25, 96, m_XOffsets_Type, 40, m_TypeFilters, m_XWidths_Small, f.Type, 0);

            AddHtmlLocalized(320, 64, 50, 32, 1062215, LabelColor, false, false); // Quality
            AddFilterList(320, 96, m_XOffsets_Quality, 40, m_QualityFilters, m_XWidths_Small, f.Quality, 1);

            AddHtmlLocalized(26, 160, 120, 32, 1062232, LabelColor, false, false); // Material Type
            AddFilterList(25, 192, m_XOffsets_Material, 40, m_MaterialFilters, m_XWidths_Large, f.Material, 2);

            AddHtmlLocalized(26, 400, 120, 32, 1062217, LabelColor, false, false); // Amount
            AddFilterList(25, 432, m_XOffsets_Amount, 40, m_AmountFilters, m_XWidths_Small, f.Quantity, 3);

            AddHtmlLocalized(75, 496, 120, 32, 1062477, (from.UseOwnFilter ? LabelColor : 16927), false, false); // Set Book Filter
            AddButton(40, 496, 4005, 4007, 1, GumpButtonType.Reply, 0);

            AddHtmlLocalized(235, 496, 120, 32, 1062478, (from.UseOwnFilter ? 16927 : LabelColor), false, false); // Set Your Filter
            AddButton(200, 496, 4005, 4007, 2, GumpButtonType.Reply, 0);

            AddHtmlLocalized(405, 496, 120, 32, 1062231, LabelColor, false, false); // Clear Filter
            AddButton(370, 496, 4005, 4007, 3, GumpButtonType.Reply, 0);

            AddHtmlLocalized(540, 496, 50, 32, 1011046, LabelColor, false, false); // APPLY
            AddButton(505, 496, 4017, 4018, 0, GumpButtonType.Reply, 0);
        }

        private struct FilterEntry
        {
            public readonly TextEntry Label;
            public readonly int Value;

            public FilterEntry(TextEntry label, int index)
            {
                Label = label;
                Value = index;
            }
        }
    }
}