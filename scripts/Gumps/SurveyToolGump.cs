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

/*      changelog.
 *      
 *    9/23/04 Lego Eater.
 *            Made Survey Tool Gump From Warninggump.cs
 *
 *
 *
 *
 */

using System;

namespace Server.Gumps
{
    public delegate void SurveyToolGumpCallback(Mobile from, bool okay, object state);

    public class SurveyToolGump : Gump
    {
        private SurveyToolGumpCallback m_Callback;
        private object m_State;

        public SurveyToolGump(int header, int headerColor, object content, int contentColor, int width, int height, SurveyToolGumpCallback callback, object state)
            : base(50, 50)
        {
            m_Callback = callback;
            m_State = state;

            Closable = true;

            AddPage(0);

            AddBackground(10, 10, 190, 140, 0x242C);


            AddHtml(30, 30, 150, 75, String.Format("<div align=CENTER>{0}</div>", "This house seems to fit here."), false, false);


            AddButton(40, 85, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(40, 107, 0x81A, 0x81B, 1011036, 32767, false, false); // okay
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1 && m_Callback != null)
                m_Callback(sender.Mobile, true, m_State);
            else if (m_Callback != null)
                m_Callback(sender.Mobile, false, m_State);
        }
    }
}