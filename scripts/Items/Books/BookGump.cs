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

/* Scripts/Gumps/BookGump.cs
 * ChangeLog
 *  12/23/06, Rhiannon
 *      Changed book text color to match that of regular books.
 *  11/24/06, Rhiannon
 *      Initial creation (simulates book functionality for use with Library and LibraryGump)
 */

using Server.Items;

namespace Server.Gumps
{
    public class BookGump : Gump
    {
        private BaseBook m_Book;

        public BaseBook Book { get { return m_Book; } }

        private void AddBackground()
        {
            AddPage(0);

            // Background image
            AddImage(100, 10, 500);
        }

        private void AddTitlePage()
        {
            string title = m_Book.Title;
            string author = m_Book.Author;

            AddPage(1);

            AddLabelCropped(140, 75, 150, 30, 0, title);
            AddHtml(140, 140, 50, 30, "<body text = #404040 </body>by", false, false);
            AddLabelCropped(140, 170, 150, 30, 0, author);
            AddButton(456, 10, 502, 502, 0, GumpButtonType.Page, 2);

        }

        private void AddText()
        {
            BookPageInfo[] pages = m_Book.Pages;
            int totalGumpPages = m_Book.Pages.Length / 2 + 1;
            int gumpPageNo = 1;

            for (int i = 0; i < m_Book.Pages.Length; i++)
            {
                // The first book page, like all odd-numbered pages, is written on the 
                // right side of the first gump page.
                int bookPageNo = i + 1;
                int firstCol = 325;

                if (bookPageNo % 2 == 0)
                {
                    // Even numbered pages are written on the left side of the gump page.
                    // Book page 2 is on gump page 2; 4 is on 3, 6 is on 4, etc.
                    gumpPageNo++;
                    AddPage(gumpPageNo);
                    firstCol = 140;

                    // Turn page buttons
                    // After writing gump page 1, whenever a new gump page is created, add a back button.
                    AddButton(100, 10, 501, 501, 0, GumpButtonType.Page, gumpPageNo - 1);

                    // On all new pages, except the last one, add a forward button.
                    if (gumpPageNo < totalGumpPages) // not the last gump page
                        AddButton(456, 10, 502, 502, 0, GumpButtonType.Page, gumpPageNo + 1);
                }

                BookPageInfo page = pages[i];

                for (int j = 0; j < page.Lines.Length; j++)
                {
                    // Write each line of the current book page to the gump in the appropriate place.
                    AddHtml(firstCol, 38 + (j * 18), 155, 20, "<body text = #943131 </body>" + page.Lines[j], false, false);
                }

                // Add the book page number to the middle of the bottom line
                AddHtml(firstCol + 70, 210, 50, 30, "<body text = #404040 </body>" + bookPageNo.ToString(), false, false);
            }
        }

        public BookGump(Mobile from, BaseBook book)
            : base(150, 200)
        {
            if (book != null)
            {
                m_Book = book;
                AddBackground();
                AddTitlePage();
                AddText();
            }
        }
    }
}