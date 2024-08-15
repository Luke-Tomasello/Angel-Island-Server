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

using System;

namespace Server.Gumps
{
    public delegate void NoticeGumpCallback(Mobile from, object state);

    public class NoticeGump : Gump
    {
        private NoticeGumpCallback m_Callback;
        private object m_State;

        public NoticeGump(int header, int headerColor, object content, int contentColor, int width, int height, NoticeGumpCallback callback, object state)
            : base((640 - width) / 2, (480 - height) / 2)
        {
            m_Callback = callback;
            m_State = state;

            Closable = false;

            AddPage(0);

            AddBackground(0, 0, width, height, 5054);

            AddImageTiled(10, 10, width - 20, 20, 2624);
            AddAlphaRegion(10, 10, width - 20, 20);
            AddHtmlLocalized(10, 10, width - 20, 20, header, headerColor, false, false);

            AddImageTiled(10, 40, width - 20, height - 80, 2624);
            AddAlphaRegion(10, 40, width - 20, height - 80);

            if (content is int)
                AddHtmlLocalized(10, 40, width - 20, height - 80, (int)content, contentColor, false, true);
            else if (content is string)
                AddHtml(10, 40, width - 20, height - 80, string.Format("<BASEFONT COLOR=#{0:X6}>{1}</BASEFONT>", contentColor, content), false, true);

            AddImageTiled(10, height - 30, width - 20, 20, 2624);
            AddAlphaRegion(10, height - 30, width - 20, 20);
            AddButton(10, height - 30, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(40, height - 30, 120, 20, 1011036, 32767, false, false); // OKAY
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1 && m_Callback != null)
                m_Callback(sender.Mobile, m_State);
        }
    }
}