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

namespace Server.Items
{
    public class BookContent
    {
        private string m_Title;
        private string m_Author;

        private BookPageInfo[] m_Pages;

        public string Title { get { return m_Title; } }
        public string Author { get { return m_Author; } }

        public BookPageInfo[] Pages { get { return m_Pages; } }

        public BookContent(string title, string author, params BookPageInfo[] pages)
        {
            m_Title = title;
            m_Author = author;
            m_Pages = pages;
        }

        public BookPageInfo[] Copy()
        {
            BookPageInfo[] copy = new BookPageInfo[m_Pages.Length];

            for (int i = 0; i < copy.Length; ++i)
                copy[i] = new BookPageInfo(m_Pages[i].Lines);

            return copy;
        }

        public bool IsMatch(BookPageInfo[] cmp)
        {
            if (cmp.Length != m_Pages.Length)
                return false;

            for (int i = 0; i < cmp.Length; ++i)
            {
                string[] a = m_Pages[i].Lines;
                string[] b = cmp[i].Lines;

                if (a.Length != b.Length)
                {
                    return false;
                }
                else if (a != b)
                {
                    for (int j = 0; j < a.Length; ++j)
                    {
                        if (a[j] != b[j])
                            return false;
                    }
                }
            }

            return true;
        }
    }
}