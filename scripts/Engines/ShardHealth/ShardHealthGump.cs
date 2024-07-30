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

using Scripts.Gumps.GumpBuilder;
using Server;
using Server.Gumps;
using Server.Network;
using System.Collections.Generic;

namespace Scripts.Engines.ShardHealth
{
    public class ShardHealthGump : TabbedGumpBuilder
    {
        public ShardHealthGump(Mobile from) : this(from, 0) { }
        public ShardHealthGump(Mobile from, int tabIndex) : base(from, "Shard Health", tabIndex)
        {
            from.CloseGump(typeof(ShardHealthGump));

            SetUpTabs();

            InitializeUi();
        }

        private void SetUpTabs()
        {
            var testTabs = new List<string>()
            {
                "Trammies",
                "Toxic crybullies",
                "Loot hoarders"
            };

            for (var j = 0; j < testTabs.Count; j++)
            {
                var thisTab = AddTab(testTabs[j]);

                if (thisTab == null)
                {
                    continue;
                }

                for (var i = 0; i < 50; i++)
                {
                    thisTab.AddLine($"Player {i + 1}");
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID >= BaseTabIndex && info.ButtonID < BaseTabIndex + TabCount)
            {
                From.CloseGumps(typeof(ShardHealthGump));

                From.SendGump(new ShardHealthGump(From, info.ButtonID));

                return;
            }

            switch (info.ButtonID)
            {
                default:
                    break;
            }
        }

        protected override void InitializeUi()
        {
            base.InitializeUi();
        }
    }
}