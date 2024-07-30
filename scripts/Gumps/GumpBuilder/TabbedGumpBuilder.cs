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

/* Engines/Leaderboard/LeaderBoard.cs
 * ChangeLog:
 *	9/6/21  Lib
 *		Initial version
 */

using Scripts.Gumps.GumpBuilder.Backgrounds;
using Server;
using Server.Gumps;
using Server.Network;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Gumps.GumpBuilder
{
    public abstract class TabbedGumpBuilder : BaseGumpBuilder
    {
        private IList<GumpBuilderTab> Tabs { get; set; }

        public bool IsHtml { get; protected set; }

        protected int TabIndex { get; set; }

        protected int TabCount
        {
            get
            {
                return Tabs.Count;
            }
        }

        // offset tab button ButtonIDs by this much
        protected const int BaseTabIndex = 1000;

        public TabbedGumpBuilder(Mobile from, string title, int tabIndex = 0, bool isHtml = true)
            : base(from, title, GumpBackgroundType.Modern, 400, 520)
        {
            Tabs = new List<GumpBuilderTab>();

            TabIndex = tabIndex >= BaseTabIndex ? tabIndex - BaseTabIndex : tabIndex;

            IsHtml = isHtml;
        }

        protected override void InitializeUi()
        {
            base.InitializeUi();

            InitializeTabs();
        }

        protected void InitializeTabs()
        {
            var yStart = 5;
            var ySpace = 5;
            var xStart = 70;

            var buttonWidth = 20;
            var buttonHeight = 20;

            var labelOffsetX = 12;

            // draw buttons
            for (var i = 0; i < Tabs.Count; i++)
            {
                var iTab = GetTab(i);
                var yPosition = yStart + ((i + 1) * (buttonHeight + ySpace));

                // draw tab buttons
                AddButton(xStart, yPosition, 4005, 4007, iTab.Index + BaseTabIndex, GumpButtonType.Reply, 0);
                AddLabel(xStart + buttonWidth + labelOffsetX, yPosition + 2, LabelHue, iTab.DisplayName);
            }

            // draw tab content
            var contentOffsetX = 175;
            var newline = IsHtml ? "<br />" : "\r\n";
            var contentHeight = Height - ((yStart + buttonHeight) * 2);
            var selectedTab = GetTab(TabIndex);
            AddHtml(xStart + contentOffsetX, yStart + buttonHeight + ySpace, 300, contentHeight, $"{selectedTab.DisplayName}{newline}{newline}{selectedTab.Content}", true, true);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            //
        }

        public GumpBuilderTab AddTab(string displayName, int index = 0)
        {
            var numTabs = Tabs.Count();

            if (Tabs.Any(x =>
                x.DisplayName == displayName
            ))
            {
                return null;
            }

            index = (index == 0)
                ? numTabs
                : index;

            var newTab = new GumpBuilderTab(displayName, index, IsHtml);

            Tabs.Add(newTab);

            return newTab;
        }

        public GumpBuilderTab GetTab(string displayName)
        {
            return Tabs.FirstOrDefault(x => x.DisplayName == displayName);
        }

        public GumpBuilderTab GetTab(int index)
        {
            return Tabs.FirstOrDefault(x => x.Index == index);
        }
    }
}