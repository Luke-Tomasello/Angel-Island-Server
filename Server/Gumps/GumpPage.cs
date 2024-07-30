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

using Server.Network;
using System;

namespace Server.Gumps
{
    public class GumpPage : GumpEntry
    {
        private int m_Page;

        public GumpPage(int page)
        {
            m_Page = page;
        }

        public int Page
        {
            get
            {
                return m_Page;
            }
            set
            {
                Delta(ref m_Page, value);
            }
        }

        public override string Compile()
        {
            return String.Format("{{ page {0} }}", m_Page);
        }

        private static byte[] m_LayoutName = Gump.StringToBuffer("page");

        public override void AppendTo(DisplayGumpFast disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(m_Page);
        }
    }
}