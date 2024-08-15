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

/* Scripts/Gumps/SeedBoxGump.cs
 * ChangeLog
 *  8/20/23, Yoar
 *      Complete refactor
 *	1/24/06, Adam
 *		Fix misspelling of "Prickly Pear"
 *	10/10/05, Pix
 *		Fixed Hedges and PlantHue.None.  Added "Other Mutation" display for PlantHue.None
 *  05/20/05, Kit
 *		Fixed issue with black seed color and hedge plant type both having button ID 18.
 *	05/11/05, Kit
 *	Added check for when removing seeds to update boxs item count.
 *	05/06/05, Kit
 *	Modified runuo release
 *	Added support for solen seeds, and hues.
 */

using Server;
using Server.Engines.Plants;
using Server.Gumps;
using Server.Items;
using Server.Network;
using System;

namespace Custom.Gumps
{
    public class SeedBoxGump : Gump
    {
        private const int TypeButtonOffset = 1;
        private const int HueButtonOffset = 1000;

        private SeedBox m_SeedBox;
        private int m_TypeIndex;

        public SeedBoxGump(SeedBox seedBox)
            : this(seedBox, 0)
        {
        }

        public SeedBoxGump(SeedBox seedBox, int typeIndex)
            : base(10, 10)
        {
            m_SeedBox = seedBox;
            m_TypeIndex = typeIndex;

            AddBackground(24, 24, 644, 445, 9200);

            AddBackground(40, 62, 204, 392, 83);
            AddImageTiled(48, 71, 190, 374, 1416);
            AddAlphaRegion(48, 71, 190, 374);

            AddBackground(255, 62, 394, 392, 83);
            AddImageTiled(263, 71, 380, 374, 1416);
            AddAlphaRegion(263, 71, 380, 374);

            AddLabel(106, 35, 1152, "Seed Types");
            AddLabel(423, 36, 1152, "Seed Colors");
            AddImage(290, 81, 3203);
            AddImage(82, 39, 216);
            AddImage(184, 39, 216);
            AddImage(394, 39, 216);
            AddImage(510, 39, 216);

            int x = 55;
            int y = 77;

            for (int i = 0; i < m_SeedBox.TypeTable.Length; i++)
            {
                int totalByType = 0;

                for (int j = 0; j < SeedBox.HueTable.Length; j++)
                {
                    SeedBox.SeedIndex key = new SeedBox.SeedIndex(i, j);

                    int count;

                    if (m_SeedBox.Counts.TryGetValue(key, out count))
                        totalByType += count;
                }

                if (totalByType > 0)
                    AddButton(x, y, i == m_TypeIndex ? 4006 : 4005, 4007, TypeButtonOffset + i, GumpButtonType.Reply, 0);

                AddLabel(x + 30, y, (totalByType > 0 ? 1152 : 808), m_SeedBox.TypeTable[i].Name);

                y += 20;
            }

            AddColorButton(1, 18); // plain

            AddColorButton(5, 14); // rare magenta
            AddColorButton(7, 15); // rare pink
            AddColorButton(9, 17); // rare aqua
            AddColorButton(11, 16); // rare fire red

            AddColorButton(14, 2); // red
            AddColorButton(15, 3); // bright red
            AddColorButton(16, 10); // orange
            AddColorButton(17, 11); // bright orange
            AddColorButton(18, 8); // yellow
            AddColorButton(19, 9); // bright yellow
            AddColorButton(20, 4); // green
            AddColorButton(21, 5); // bright green
            AddColorButton(22, 6); // blue
            AddColorButton(23, 7); // bright blue
            AddColorButton(24, 12); // purple
            AddColorButton(25, 13); // bright purple

            AddColorButton(28, 0); // black mutation
            AddColorButton(29, 1); // white mutation
            AddColorButton(30, 19); // solen
            AddColorButton(31, 20); // other mutation

            AddLabel(480, 420, 2100, string.Format("Total Seeds: {0}", m_SeedBox.SeedCount()));

            SeedBox.TypeInfo typeInfo = m_SeedBox.GetTypeInfo(m_TypeIndex);

            if (typeInfo != null)
            {
                AddLabel(290, 77, 1152, typeInfo.Name);
                AddItem(310, 117, typeInfo.ItemID);
            }
        }

        private void AddColorButton(int gridIndex, int hueIndex)
        {
            SeedBox.HueInfo hueInfo = SeedBox.GetHueInfo(hueIndex);

            if (hueInfo == null)
                return;

            int x = ((gridIndex % 2) == 0 ? 270 : 440);
            int y = 77 + 20 * (gridIndex / 2);

            SeedBox.SeedIndex key = new SeedBox.SeedIndex(m_TypeIndex, hueIndex);

            int count;

            if (!m_SeedBox.Counts.TryGetValue(key, out count))
                count = 0;

            if (count > 0)
                AddButton(x, y, 0xFA5, 0xFA6, HueButtonOffset + hueIndex, GumpButtonType.Reply, 0);

            AddLabel(x + 40, y + 3, count == 0 ? 808 : hueInfo.Hue, string.Format("{0} {1}", count, hueInfo.Name));
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            if (!from.InRange(m_SeedBox.GetWorldLocation(), 2) || !m_SeedBox.CheckAccess(from))
                return;

            int buttonID = info.ButtonID;

            if (buttonID >= TypeButtonOffset && buttonID < TypeButtonOffset + m_SeedBox.TypeTable.Length)
            {
                from.CloseGump(typeof(SeedBoxGump));
                from.SendGump(new SeedBoxGump(m_SeedBox, buttonID - TypeButtonOffset));
            }
            else if (buttonID >= HueButtonOffset && buttonID < HueButtonOffset + SeedBox.HueTable.Length)
            {
                int hueIndex = buttonID - HueButtonOffset;

                SeedBox.SeedIndex key = new SeedBox.SeedIndex(m_TypeIndex, hueIndex);

                int count;

                if (!m_SeedBox.Counts.TryGetValue(key, out count))
                    count = 0;

                if (count > 0)
                {
                    PlantType plantType = m_SeedBox.IndexToPlantType(m_TypeIndex);
                    PlantHue plantHue = SeedBox.IndexToPlantHue(hueIndex);

                    from.AddToBackpack(new Seed(plantType, plantHue, true));

                    m_SeedBox.RemoveSeed(plantType, plantHue);
                }
                else
                {
                    from.SendMessage("You do not have any of those seeds!");
                }

                from.CloseGump(typeof(SeedBoxGump));
                from.SendGump(new SeedBoxGump(m_SeedBox, m_TypeIndex));
            }
        }
    }
}