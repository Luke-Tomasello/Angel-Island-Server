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

/* Engines/Leaderboard/LeaderboardGump.cs
 * ChangeLog:
 *  4/9/2024, Adam
 *      increase the header height to support a break into 2 lines.
 *      Introduce header_height variable, and base page content off that.
 *	8/12/21, Craig
 *		Initial version
 */

using Server;
using Server.Gumps;
using Server.Network;

namespace Scripts.Engines.Leaderboard
{
    public class LeaderboardGump : Gump
    {
        private const int Width = 416;
        private const int Height = 300;

        // How many tabs will show per "tab page". The tab layout scales dynamically based on this value.
        // A "tab page" is the group of tabs that display on one page. The left and right arrows change pages.
        private const int TabsPerPage = 3;

        private int m_TabIndex;
        private int m_PageNumber;
        private Mobile m_From;

        private LeaderboardTab m_Tab;

        private int m_StartingTabCount;

        // The current page number in the selected tab (1 base)
        private int CurrentPageNumber { get { return m_PageNumber; } }

        // The currently selected tab's index (0 base)
        private int CurrentTabIndex { get { return m_TabIndex; } }
        private LeaderboardTab CurrentTab { get { return m_Tab; } }

        // The index of the current tab page (0 base)
        private int CurrentTabPage { get { return CurrentTabIndex / TabsPerPage; } }
        // The index of the last tab page (0 base)
        private int LastTabPage { get { return (LeaderboardTracker.TabCount - 1) / TabsPerPage; } }

        public LeaderboardGump(Mobile from)
            : this(from, 0, 1)
        {
            // Public constructor to display initial leaderboard when opened by the player
            // Hides tab and page implenentation from outside classes
        }

        public LeaderboardGump(Mobile from, int tabIndex, int pageNumber)
            : base(50, 10)
        {
            from.CloseGump(typeof(LeaderboardGump));
            m_From = from;
            m_TabIndex = tabIndex;
            m_PageNumber = pageNumber;

            AddPage(0);
            // Top of scroll
            AddImage(0, 0, 5170);
            AddImage(38, 0, 5151);
            AddImage(208, 0, 5151);
            AddImage(378, 0, 5172);
            // Scroll body
            AddImage(0, 38, 5153);
            AddImage(38, 38, 5154);
            AddImage(208, 38, 5154);
            AddImage(378, 38, 5155);
            AddImage(0, 150, 5153);
            AddImage(38, 150, 5154);
            AddImage(208, 150, 5154);
            AddImage(378, 150, 5155);
            // Scroll bottom
            AddImage(0, 262, 5176);
            AddImage(38, 262, 5177);
            AddImage(208, 262, 5177);
            AddImage(378, 262, 5178);

            string title = "<center>Leaderboards</center>";
            AddHtml(0, 3, Width, 30, title, false, false);

            // Display tab list
            m_StartingTabCount = LeaderboardTracker.TabCount;
            if (m_StartingTabCount == 0)
            {
                AddHtml(0, Height / 2, Width, 30, "<center>No leaderboards to display</center>", false, false);
                return;
            }

            m_Tab = LeaderboardTracker.GetTab(CurrentTabIndex);
            if (m_Tab == null)
                return;

            int margin = 20;        // Left and right margin for dead space at edge of scroll
            int buttonwidth = 25;   // Width of left and right buttons for changing tab pages

            // Adjust tab width depending on how many tabs will be displayed
            int tabWidth = (Width - ((margin + buttonwidth) * 2)) / GetTabCountForPage(CurrentTabPage);

            // Place left and right buttons, if needed
            if (m_StartingTabCount > TabsPerPage)
            {
                AddButton(margin, 42, 5607, 5603, 3, GumpButtonType.Reply, 0); // Left
                AddButton((Width - margin - buttonwidth + 3), 42, 5605, 5601, 4, GumpButtonType.Reply, 0); // Right
            }

            int firstTabIndex = GetFirstTabIndex(CurrentTabPage);
            int stopIndex = GetTabCountForPage(CurrentTabPage);
            int xPos = margin + buttonwidth;
            int header_height = 40; // 30
            for (int i = 0; i < stopIndex; i++)
            {
                LeaderboardTab nextTab = LeaderboardTracker.GetTab(firstTabIndex + i);
                if (nextTab != null)
                {
                    // Currently selected tab gets a highlighted non-clickable button and highlighted text
                    if (i == CurrentTabIndex % TabsPerPage)
                    {
                        AddImage(xPos, 40, 4012);
                        AddHtml(xPos + 35, 43, tabWidth - 35, header_height, string.Format("<basefont color=#0000CC>{0}</font>", nextTab.Heading), false, false);
                    }
                    else
                    {
                        AddButton(xPos, 40, 4011, 4011, i + 5, GumpButtonType.Reply, 0);
                        AddHtml(xPos + 35, 43, tabWidth - 35, header_height, nextTab.Heading, false, false);
                    }
                }
                xPos += tabWidth;
            }

            // Display page content
            int pageCount = m_Tab.GetPageCount();
            if ((pageNumber > 0) && (pageNumber <= pageCount))
            {
                AddHtml(30, /*75*/header_height + 40, Width, 200, m_Tab.GetPage(pageNumber), false, false);

                // Place page up and down buttons, if needed
                if (pageCount > 1)
                {
                    // Scroll buttons
                    //2084
                    AddButton(370, 5, 2435, 2436, 1, GumpButtonType.Reply, 0); // Up
                    AddButton(370, 286, 2437, 2437, 2, GumpButtonType.Reply, 0); // Down

                    // Current page count  x/y
                    string pagetext = string.Format("<center>Page {0} / {1}<center>", pageNumber, pageCount);
                    AddHtml(0, 286, Width, 30, pagetext, false, false);
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            // If a tab was deleted while the gump was open, reload the leaderboard
            if (CurrentTab == null)
                return;

            if (m_StartingTabCount != LeaderboardTracker.TabCount)
            {
                m_From.SendGump(new LeaderboardGump(m_From, 0, 1));
                return;
            }


            switch (info.ButtonID)
            {
                case 1: // Scroll up
                    {
                        int previousPage = CurrentPageNumber - 1;
                        if (previousPage < 1)
                            previousPage = CurrentTab.GetPageCount();

                        m_From.SendGump(new LeaderboardGump(m_From, CurrentTabIndex, previousPage));
                        break;
                    }
                case 2: // Scroll down
                    {
                        int nextPage = CurrentPageNumber + 1;
                        if (nextPage > CurrentTab.GetPageCount())
                            nextPage = 1;

                        m_From.SendGump(new LeaderboardGump(m_From, CurrentTabIndex, nextPage));
                        break;
                    }
                case 3: // Left
                    {
                        int previousTabPage = CurrentTabPage - 1;
                        if (previousTabPage < 0)
                            previousTabPage = LastTabPage;

                        m_From.SendGump(new LeaderboardGump(m_From, GetFirstTabIndex(previousTabPage), 1));
                        break;
                    }
                case 4: // Right
                    {
                        int nextTabPage = CurrentTabPage + 1;
                        if (nextTabPage > LastTabPage)
                            nextTabPage = 0;

                        m_From.SendGump(new LeaderboardGump(m_From, GetFirstTabIndex(nextTabPage), 1));
                        break;
                    }
                default: // Tab buttons
                    {
                        // Closing the gump triggers this branch, so return if the button ID is not
                        // in the range for a tab button. Otherwise a new blank gump gets created.
                        if (info.ButtonID < 5 || info.ButtonID > TabsPerPage + 4)
                            return;

                        int tabIndex = (info.ButtonID - 5) + (GetFirstTabIndex(CurrentTabPage));
                        m_From.SendGump(new LeaderboardGump(m_From, tabIndex, 1));
                        break;
                    }
            }
        }

        // Get the index of the first tab on this "tab page"
        private int GetFirstTabIndex(int tabPage)
        {
            return tabPage * TabsPerPage;
        }

        // Get the total number of tabs on this page.
        // Usually its just TabsPerLine, but the last page will vary.
        private int GetTabCountForPage(int tabPageIndex)
        {
            int tabCount = LeaderboardTracker.TabCount % TabsPerPage;
            if (tabCount == 0 || tabPageIndex != LastTabPage)
                return TabsPerPage;
            else
                return tabCount;
        }
    }
}