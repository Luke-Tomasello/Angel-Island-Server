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

/* Scripts\Engines\AngelIsland\Gumps\AITeleportQueryGump.cs
 * ChangeLog
 *	2/10/11, Adam
 *		make gump conditioned on Core.UOAI || Core.UOAR
 * 4/8/08, Adam
 *		Ignore gump during Server Wars.
 *		There is no murder counts, no prison, no statloss during Server Wars.
 * created 4/20/04 by mith
 * Copied from Gumps/ReportMurderer.cs
 */

using Server.Items;
using Server.Mobiles;
using Server.Network;
using System;

namespace Server.Gumps
{
    public class AITeleportQueryGump : Gump
    {
        private DateTime m_MaxResponseTime;

        public static void Initialize()
        {
            EventSink.PlayerDeath += new PlayerDeathEventHandler(EventSink_PlayerDeath);
        }

        public static void EventSink_PlayerDeath(PlayerDeathEventArgs e)
        {
            Mobile m = e.Mobile;

            // only on AI
            if (Core.RuleSets.AngelIslandRules())
                // Adam: not during Server Wars
                if (m.Player && m.AccessLevel == AccessLevel.Player && m.ShortTermMurders >= 5 && !Server.Misc.TestCenter.ServerWars())
                    if (!((PlayerMobile)m).PrisonInmate)
                        new GumpTimer(m).Start();
        }

        private class GumpTimer : Timer
        {
            private Mobile m_Player;

            public GumpTimer(Mobile m)
                : base(TimeSpan.FromSeconds(4.0))
            {
                m_Player = m;
            }

            protected override void OnTick()
            {
                m_Player.SendGump(new AITeleportQueryGump(m_Player));
            }
        }

        private class TeleportTimeoutTimer : Timer
        {
            private Mobile m_Player;

            public TeleportTimeoutTimer(Mobile m)
                : base(TimeSpan.FromMinutes(5.0))
            {
                m_Player = m;
            }

            protected override void OnTick()
            {
                m_Player.CloseGump(typeof(AITeleportQueryGump));
                Stop();
            }
        }

        private AITeleportQueryGump(Mobile m)
            : base(0, 0)
        {
            m_MaxResponseTime = DateTime.UtcNow + TimeSpan.FromMinutes(5);

            BuildGump();

            new TeleportTimeoutTimer(m).Start();

        }

        private void BuildGump()
        {
            AddBackground(265, 205, 320, 290, 5054);
            Closable = false;
            Resizable = false;

            AddPage(0);

            AddImageTiled(225, 175, 50, 45, 0xCE);   //Top left corner
            AddImageTiled(267, 175, 315, 44, 0xC9);  //Top bar
            AddImageTiled(582, 175, 43, 45, 0xCF);   //Top right corner
            AddImageTiled(225, 219, 44, 270, 0xCA);  //Left side
            AddImageTiled(582, 219, 44, 270, 0xCB);  //Right side
            AddImageTiled(225, 489, 44, 43, 0xCC);   //Lower left corner
            AddImageTiled(267, 489, 315, 43, 0xE9);  //Lower Bar
            AddImageTiled(582, 489, 43, 43, 0xCD);   //Lower right corner

            AddPage(1);

            AddHtml(260, 234, 300, 140, "You have more than 5 short-term murders, would you like to be teleported to Angel Island to serve out your sentence?", false, false);

            AddButton(260, 300, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(300, 300, 300, 50, 1046362, false, false); // Yes

            AddButton(360, 300, 0xFA5, 0xFA7, 2, GumpButtonType.Reply, 0);
            AddHtmlLocalized(400, 300, 300, 50, 1046363, false, false); // No      
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            //check if we're more than 5 minutes from the gump creation time
            //if we are, then do nothing.
            if (m_MaxResponseTime < DateTime.UtcNow)
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1:
                    {
                        Item aiEntrance = new AIEntrance();
                        aiEntrance.OnMoveOver(from);
                        aiEntrance.Delete();

                        break;
                    }
                case 2:
                    {
                        break;
                    }
            }
        }
    }
}