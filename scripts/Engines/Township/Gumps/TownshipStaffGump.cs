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

/* Scripts/Engines/Township/TownshipStaffGump.cs
 * CHANGELOG
 * 2/27/22, Yoar
 *		Changed which properties are displayed in the township listing.
 *		Removed details page. Instead, added a button that opens the props gump for the township stone.
 * 1/12/22, Yoar
 *		Township cleanups
 * 10/25/08, Pix
 *		Gump text positioning.
 * 10/19/08, Pix
 *		Added more information, center of townships, whether the ts is extended.
 *		Made Go button go to stone, not township center (they used to be the same thing).
 * 10/17/08, Pix
 *		Added percentage house ownership to gump.
 * 7/20/08, Pix
 *		De-coupled township stones from houses.
 *	5/14/07, Pix
 *		Added WeeksAtThisLevel in detail.
 *	4/30/07: Pix
 *		Enhancements for usability.
 */

using Server.Gumps;
using Server.Items;
using Server.Network;
using System;

namespace Server.Township
{
    public class TownshipStaffGump : Gump
    {
        private const int PerPage = 10;

        public static void Initialize()
        {
            CommandSystem.Register("TSList", AccessLevel.Counselor, new CommandEventHandler(TSList_OnCommand));
        }

        [Usage("TSList")]
        [Description("Opens an interface providing access to a list of townships.")]
        public static void TSList_OnCommand(CommandEventArgs e)
        {
            e.Mobile.CloseGump(typeof(TownshipStaffGump));
            e.Mobile.SendGump(new TownshipStaffGump());
        }

        private TownshipStone[] m_List;
        private int m_Page;

        public TownshipStaffGump()
            : this(null, 0)
        {
        }

        private TownshipStaffGump(TownshipStone[] list, int page)
            : base(20, 30)
        {
            try
            {
                m_List = list;
                m_Page = page;

                AddBackground(0, 0, 550, 420, 5054);
                AddBackground(10, 10, 530, 400, 3000);

                if (m_List == null)
                    m_List = TownshipStone.AllTownshipStones.ToArray();

                ShowList(page);

                AddButton(20, 380, 4005, 4007, 0, GumpButtonType.Reply, 0);
                AddHtmlLocalized(55, 380, 120, 20, 1011441, false, false); // EXIT

                if (page > 0)
                {
                    AddButton(180, 380, 4014, 4016, 2, GumpButtonType.Reply, 0);
                    AddHtmlLocalized(215, 380, 120, 20, 1043354, false, false); // Previous
                }

                if (page < (m_List.Length + PerPage - 1) / PerPage - 1)
                {
                    AddButton(330, 380, 4005, 4007, 1, GumpButtonType.Reply, 0);
                    AddHtmlLocalized(365, 380, 120, 20, 1043353, false, false); // Next
                }
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        private void ShowList(int page)
        {
            AddHtml(90, 15, 200, 20, "Guild", false, false);
            AddHtml(290, 15, 60, 20, "Abbrv.", false, false);
            AddHtml(350, 15, 60, 20, "Size", false, false);
            AddHtml(410, 15, 60, 20, "Activity", false, false);
            AddHtml(470, 15, 60, 20, "Extended", false, false);

            int y = 50;

            int start = page * PerPage;
            int end = start + PerPage;

            for (int i = start; i < end && i < m_List.Length; i++)
            {
                TownshipStone ts = m_List[i];

                AddButton(20, y, 4005, 4007, 1000 + 2 * i, GumpButtonType.Reply, 0); // go
                AddButton(55, y, 4011, 4013, 1001 + 2 * i, GumpButtonType.Reply, 0); // props

                AddHtml(90, y, 200, 20, ts.GuildName, false, false);
                AddHtml(290, y, 60, 20, ts.GuildAbbreviation, false, false);
                AddHtml(350, y, 60, 20, TownshipStone.GetTownshipSizeDesc(ts.ActivityLevel), false, false);
                AddHtml(410, y, 60, 20, TownshipStone.GetTownshipActivityDesc(ts.LastActivityLevel), false, false);
                AddHtml(470, y, 60, 20, ts.Extended.ToString(), false, false);

                y += 30;
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            try
            {
                Mobile from = sender.Mobile;

                if (from.AccessLevel < AccessLevel.Counselor)
                    return;

                int buttonID = info.ButtonID;

                switch (buttonID)
                {
                    case 1: // Next
                        {
                            from.CloseGump(typeof(TownshipStaffGump));
                            from.SendGump(new TownshipStaffGump(m_List, Math.Min(m_Page + 1, m_List.Length - 1)));
                            break;
                        }
                    case 2: // Previous
                        {
                            from.CloseGump(typeof(TownshipStaffGump));
                            from.SendGump(new TownshipStaffGump(m_List, Math.Max(m_Page - 1, 0)));
                            break;
                        }
                    case 3: // Back to Main Page
                        {
                            from.CloseGump(typeof(TownshipStaffGump));
                            from.SendGump(new TownshipStaffGump(m_List, m_Page));
                            break;
                        }
                    default:
                        {
                            if (buttonID >= 1000)
                            {
                                int index = (buttonID - 1000) / 2;
                                int type = (buttonID - 1000) % 2;

                                if (index < m_List.Length)
                                {
                                    TownshipStone ts = m_List[index];

                                    switch (type)
                                    {
                                        case 0: // Go
                                            {
                                                if (ts.Map == null || ts.Map == Map.Internal)
                                                    from.SendMessage("Cannot go there.");
                                                else
                                                    from.MoveToWorld(ts.Location, ts.Map);

                                                from.CloseGump(typeof(TownshipStaffGump));
                                                from.SendGump(new TownshipStaffGump(m_List, m_Page));
                                                break;
                                            }
                                        case 1: // Props
                                            {
                                                from.SendGump(new PropertiesGump(from, ts));
                                                from.CloseGump(typeof(TownshipStaffGump));
                                                from.SendGump(new TownshipStaffGump(m_List, m_Page));
                                                break;
                                            }
                                    }
                                }
                            }

                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }
    }
}