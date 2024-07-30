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

/* Scripts\Engines\Township\Gumps\BaseTownshipGump.cs
 * CHANGELOG:
 *  8/23/23, Yoar
 *      Initial version.
 */

using Server.Gumps;
using Server.Items;
using System;
using System.Collections;

namespace Server.Township
{
    public abstract class BaseTownshipGump : Gump
    {
        protected const int LineHeight = 30;

        protected int m_Width;
        protected int m_Height;
        protected int m_Y;

        protected TownshipStone m_Stone;

        private bool m_LineBreaks;

        public BaseTownshipGump(TownshipStone stone)
            : base(20, 30)
        {
            m_Width = 550;
            m_Height = 420;
            m_Y = 50;

            m_Stone = stone;

            m_LineBreaks = true;
        }

        protected void AddBackground()
        {
            AddBackground(0, 0, m_Width, m_Height, 5054);
            AddBackground(10, 10, m_Width - 20, m_Height - 20, 3000);
        }

        protected void AddTitle(string title)
        {
            AddHtml(20, 15, m_Width - 40, 20, Center(title), false, false);
        }

        protected void AddLine()
        {
            m_Y += LineHeight;
        }

        protected void AddLine(string format, params object[] args)
        {
            AddHtml(20, m_Y, m_Width - 40, 20, String.Format(format, args), false, false);

            if (m_LineBreaks)
                m_Y += LineHeight;
        }

        protected void AddText(int height, string format, params object[] args)
        {
            AddHtml(20, m_Y, m_Width - 40, height, String.Format(format, args), false, false);

            if (m_LineBreaks)
                m_Y += height;
        }

        protected void AddButton(int buttonID, string text, bool enabled = true)
        {
            AddButtonLabeled(20, m_Y, m_Width - 40, buttonID, text, enabled);

            if (m_LineBreaks)
                m_Y += LineHeight;
        }

        protected void AddGridText(int cols, int col, string format, params object[] args)
        {
            if (cols <= 1)
            {
                AddLine(format, args);
                return;
            }

            int width = (m_Width - 40) / cols;

            AddHtml(20 + width * col, m_Y, width, 20, String.Format(format, args), false, false);

            if (m_LineBreaks && col == cols - 1)
                m_Y += LineHeight;
        }

        protected void AddGridButton(int cols, int col, int buttonID, string text, bool enabled = true)
        {
            if (cols <= 1)
            {
                AddButton(buttonID, text, enabled);
                return;
            }

            int width = (m_Width - 40) / cols;

            AddButtonLabeled(20 + width * col, m_Y, width, buttonID, text, enabled);

            if (m_LineBreaks && col == cols - 1)
                m_Y += LineHeight;
        }

        protected void AddButtonLabeled(int x, int y, int width, int buttonID, string text, bool enabled = true)
        {
            if (enabled)
                AddButton(x, y - 1, 4005, 4007, buttonID, GumpButtonType.Reply, 0);

            AddHtml(x + 35, y, width - 35, 20, text, false, false);
        }

        protected void AddList(IList list, int entriesPerPage, Action<int> compiler)
        {
            m_LineBreaks = false;

            int xHalf = m_Width / 2;
            int y0 = m_Y;
            int yPageButtons = y0 + entriesPerPage * LineHeight;

            AddPage(1);

            for (int i = 0; i < list.Count; i++)
            {
                if (i != 0 && (i % entriesPerPage) == 0)
                {
                    m_Y = y0;

                    AddButton(xHalf, yPageButtons - 1, 4005, 4007, 0, GumpButtonType.Page, (i / entriesPerPage) + 1);
                    AddHtmlLocalized(xHalf + 35, yPageButtons, 300, 20, 1011066, false, false); // Next page

                    AddPage((i / entriesPerPage) + 1);

                    AddButton(20, yPageButtons - 1, 4014, 4016, 0, GumpButtonType.Page, (i / entriesPerPage));
                    AddHtmlLocalized(55, yPageButtons, 300, 20, 1011067, false, false); // Previous page
                }

                compiler(i);

                AddLine();
            }

            m_LineBreaks = true;
        }

        protected static int GetButtonID(int buttonID, out int index)
        {
            if (buttonID >= 1000)
            {
                index = buttonID % 1000;

                buttonID -= index;
            }
            else
            {
                index = -1;
            }

            return buttonID;
        }
    }
}