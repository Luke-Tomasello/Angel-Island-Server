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

/* Engines/AngelIsland/AIAdminGump.cs
 * ChangeLog
 *  7/24/06, Rhiannon
 *		Commented out GetHueFor(), m_AccessLevelStrings, and FormatAccessLevel()
 *  4/30/04, pixie
 *     Added "(seconds)" modifier to depot respawn frequency
 *	4/29/04, mith
 *		Hooked up remaining variables for all of our spawn intensities/respawn times to AIAdminGump.
 *  4/13/04, pixie
 *     Really fixed it this time (it was writing AND reading from the wrong place);
 *  4/13/04, pixie
 *      Fixed lighthouse passes variable to read from the right entry!
 *	4/13/04, mith
 *		Removed armor drops, added scroll drops.
 *	4/12/04 mith
 *		added buttons for Stinger Min/Max damage
 *	 4/12/04 pixie
 *		Initial Revision.
 *	 4/03/04 Created by Pixie;
 */


using System;
using System.Collections;

namespace Server.Gumps
{

    public enum AIAdminGumpPage
    {
        //Main menu items
        Items,
        CellGuards,
        PostGuards,
        CaptainGuard,
        SpiritSpawn,
        //Sub-menu items
        SpiritSpawn_Spawn,
        SpiritSpawn_Depot
    }

    public class AIAdminGump : Gump
    {
        private Mobile m_From;

        private AIAdminGumpPage m_PageType;
        private ArrayList m_List;
        private int m_ListPage;
        private object m_State;

        private const int LabelColor = 0x7FFF;
        private const int SelectedColor = 0x421F;
        private const int DisabledColor = 0x4210;

        private const int LabelColor32 = 0xFFFFFF;
        private const int SelectedColor32 = 0x8080FF;
        private const int DisabledColor32 = 0x808080;

        private const int LabelHue = 0x480;
        private const int GreenHue = 0x40;
        private const int RedHue = 0x20;

        public void AddPageButton(int x, int y, int buttonID, string text, AIAdminGumpPage page, params AIAdminGumpPage[] subPages)
        {
            bool isSelection = (m_PageType == page);

            for (int i = 0; !isSelection && i < subPages.Length; ++i)
                isSelection = (m_PageType == subPages[i]);

            AddSelectedButton(x, y, buttonID, text, isSelection);
        }

        public void AddSelectedButton(int x, int y, int buttonID, string text, bool isSelection)
        {
            AddButton(x, y - 1, isSelection ? 4006 : 4005, 4007, buttonID, GumpButtonType.Reply, 0);
            AddHtml(x + 35, y, 200, 20, Color(text, isSelection ? SelectedColor32 : LabelColor32), false, false);
        }

        public void AddButtonLabeled(int x, int y, int buttonID, string text)
        {
            AddButton(x, y - 1, 4005, 4007, buttonID, GumpButtonType.Reply, 0);
            AddHtml(x + 35, y, 240, 20, Color(text, LabelColor32), false, false);
        }

        public void AddBlackAlpha(int x, int y, int width, int height)
        {
            AddImageTiled(x, y, width, height, 2624);
            AddAlphaRegion(x, y, width, height);
        }

        public int GetButtonID(int type, int index)
        {
            return 1 + (index * 10) + type;
        }

        public static string FormatTimeSpan(TimeSpan ts)
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}", ts.Days, ts.Hours % 24, ts.Minutes % 60, ts.Seconds % 60);
        }

        public static string FormatByteAmount(long totalBytes)
        {
            if (totalBytes > 1000000000)
                return string.Format("{0:F1} GB", (double)totalBytes / 1073741824);

            if (totalBytes > 1000000)
                return string.Format("{0:F1} MB", (double)totalBytes / 1048576);

            if (totalBytes > 1000)
                return string.Format("{0:F1} KB", (double)totalBytes / 1024);

            return string.Format("{0} Bytes", totalBytes);
        }

        public static void Initialize()
        {
            CommandSystem.Register("AIAdmin", AccessLevel.Administrator, new CommandEventHandler(AIAdmin_OnCommand));
        }

        [Usage("AIAdmin")]
        [Description("Opens an interface providing access to all Angel Island settings.")]
        public static void AIAdmin_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendGump(new AIAdminGump(e.Mobile, AIAdminGumpPage.Items, 0, null, null, null));
        }

        //		public static int GetHueFor( Mobile m )
        //		{
        //			if ( m == null )
        //				return LabelHue;
        //
        //			switch ( m.AccessLevel )
        //			{
        //				case AccessLevel.Owner: return 0x35;
        //				case AccessLevel.Administrator: return 0x516;
        //				case AccessLevel.Seer: return 0x144;
        //				case AccessLevel.GameMaster: return 0x21;
        //				case AccessLevel.Counselor: return 0x2;
        //				case AccessLevel.FightBroker: return 0x8AB;
        //				case AccessLevel.Reporter: return 0x979;
        //				case AccessLevel.Player: default:
        //				{
        //					if ( m.Murderer )
        //						return 0x21;
        //					else if ( m.Criminal )
        //						return 0x3B1;
        //
        //					return 0x58;
        //				}
        //			}
        //		}

        //		public static string GetAccessLevelString (AccessLevel level)
        //		{
        //			switch ( level )
        //			{
        //				case AccessLevel.Owner: return "Owner";
        //				case AccessLevel.Administrator: return "Administrator";
        //				case AccessLevel.Seer: return "Seer";
        //				case AccessLevel.GameMaster: return "Game Master";
        //				case AccessLevel.Counselor: return "Counselor";
        //				case AccessLevel.FightBroker: return "Fight Broker";
        //				case AccessLevel.Reporter: return "Reporter";
        //				case AccessLevel.Player: default: return "Player";
        //			}
        //		}

        //		private static string[] m_AccessLevelStrings = new string[]
        //			{
        //				"Player",
        //				"Counselor",
        //				"Game Master",
        //				"Seer",
        //				"Administrator"
        //			};

        // This function doesn't seem to be used anywhere, and I can't see the purpose for it.
        //		public static string FormatAccessLevel( AccessLevel level )
        //		{
        //			int v = (int)level;
        //
        //			if ( v >= 0 && v < m_AccessLevelStrings.Length )
        //				return m_AccessLevelStrings[v];
        //
        //			return "Unknown";
        //		}

        public AIAdminGump(Mobile from, AIAdminGumpPage pageType, int listPage, ArrayList list, string notice, object state)
            : base(50, 40)
        {
            from.CloseGump(typeof(AIAdminGump));

            m_From = from;
            m_PageType = pageType;
            m_ListPage = listPage;
            m_State = state;
            m_List = list;

            AddPage(0);

            AddBackground(0, 0, 420, 440, 5054);

            AddBlackAlpha(10, 10, 170, 100);
            AddBlackAlpha(190, 10, 220, 100);
            AddBlackAlpha(10, 120, 400, 260);
            AddBlackAlpha(10, 390, 400, 40);

            AddPageButton(10, 10, GetButtonID(0, 0), "Items", AIAdminGumpPage.Items);
            AddPageButton(10, 30, GetButtonID(0, 1), "Cell Guards", AIAdminGumpPage.CellGuards);
            AddPageButton(10, 50, GetButtonID(0, 2), "Post Guards", AIAdminGumpPage.PostGuards);
            AddPageButton(10, 70, GetButtonID(0, 3), "Captain Guard", AIAdminGumpPage.CaptainGuard);
            AddPageButton(10, 90, GetButtonID(0, 4), "Spirit Spawn", AIAdminGumpPage.SpiritSpawn);

            if (notice != null)
                AddHtml(12, 392, 396, 36, Color(notice, LabelColor32), false, false);

            int paramx = 12;
            int paramwidth = 300;
            int valuex = paramx + paramwidth + 2;
            int valuewidth = 50;
            int y = 128;
            int height = 20;

            switch (pageType)
            {
                case AIAdminGumpPage.Items:
                    {
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Island Stinger Min HP");
                        AddTextField(valuex, y, valuewidth, height, 0, string.Format("{0}", CoreAI.StingerMinHP));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Island Stinger Max HP");
                        AddTextField(valuex, y, valuewidth, height, 1, string.Format("{0}", CoreAI.StingerMaxHP));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Island Stinger Min Damage");
                        AddTextField(valuex, y, valuewidth, height, 2, string.Format("{0}", CoreAI.StingerMinDamage));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Island Stinger Max Damage");
                        AddTextField(valuex, y, valuewidth, height, 3, string.Format("{0}", CoreAI.StingerMaxDamage));
                        y += (height + 2);

                        AddButtonLabeled(paramx, y - 1, GetButtonID(1, 0), "Set");

                        break;
                    }
                case AIAdminGumpPage.CellGuards:
                    {
                        //					AddLabelCropped( paramx, y, paramwidth, height, LabelHue, "Cell Guard Spawn Freq" );
                        //					AddTextField( valuex, y, valuewidth, height, 0, string.Format("{0}",CoreAI.CellGuardSpawnFreq));
                        //					y += (height+2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Cell Guard Strength");
                        AddTextField(valuex, y, valuewidth, height, 0, string.Format("{0}", CoreAI.CellGuardStrength));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Cell Guard Skill Level");
                        AddTextField(valuex, y, valuewidth, height, 1, string.Format("{0}", CoreAI.CellGuardSkillLevel));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Cell Guard Num Regs Dropped");
                        AddTextField(valuex, y, valuewidth, height, 2, string.Format("{0}", CoreAI.CellGuardNumRegDrop));
                        y += (height + 2);

                        AddButtonLabeled(paramx, y - 1, GetButtonID(2, 0), "Set");

                        break;
                    }
                case AIAdminGumpPage.PostGuards:
                    {
                        //					AddLabelCropped( paramx, y, paramwidth, height, LabelHue, "Post Guard Spawn Freq" );
                        //					AddTextField( valuex, y, valuewidth, height, 0, string.Format("{0}",CoreAI.PostGuardSpawnFreq));
                        //					y += (height+2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Guard Spawn Restart Delay (minutes)");
                        AddTextField(valuex, y, valuewidth, height, 0, string.Format("{0}", CoreAI.GuardSpawnRestartDelay));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Guard Spawn Expire Delay (minutes)");
                        AddTextField(valuex, y, valuewidth, height, 1, string.Format("{0}", CoreAI.GuardSpawnExpireDelay));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Post Guard Strength");
                        AddTextField(valuex, y, valuewidth, height, 2, string.Format("{0}", CoreAI.PostGuardStrength));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Post Guard Skill Level");
                        AddTextField(valuex, y, valuewidth, height, 3, string.Format("{0}", CoreAI.PostGuardSkillLevel));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Post Guard Regs Dropped");
                        AddTextField(valuex, y, valuewidth, height, 4, string.Format("{0}", CoreAI.PostGuardNumRegDrop));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Post Guard Bandies Dropped");
                        AddTextField(valuex, y, valuewidth, height, 5, string.Format("{0}", CoreAI.PostGuardNumBandiesDrop));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Post Guard GH Pots Dropped");
                        AddTextField(valuex, y, valuewidth, height, 6, string.Format("{0}", CoreAI.PostGuardNumGHPotDrop));
                        y += (height + 2);

                        AddButtonLabeled(paramx, y - 1, GetButtonID(3, 0), "Set");

                        break;
                    }
                case AIAdminGumpPage.CaptainGuard:
                    {
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Captain Strength");
                        AddTextField(valuex, y, valuewidth, height, 0, string.Format("{0}", CoreAI.CaptainGuardStrength));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Captain Skill Level");
                        AddTextField(valuex, y, valuewidth, height, 1, string.Format("{0}", CoreAI.CaptainGuardSkillLevel));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Captain Weapon Drop Intensity/Strength");
                        AddTextField(valuex, y, valuewidth, height, 2, string.Format("{0}", CoreAI.CaptainGuardWeapDrop));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Captain Regs Dropped");
                        AddTextField(valuex, y, valuewidth, height, 3, string.Format("{0}", CoreAI.CaptainGuardNumRegDrop));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Captain Bandies Dropped");
                        AddTextField(valuex, y, valuewidth, height, 4, string.Format("{0}", CoreAI.CaptainGuardNumBandiesDrop));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Captain GH Pots Dropped");
                        AddTextField(valuex, y, valuewidth, height, 5, string.Format("{0}", CoreAI.CaptainGuardGHPotsDrop));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Captain Scrolls Dropped");
                        AddTextField(valuex, y, valuewidth, height, 6, string.Format("{0}", CoreAI.CaptainGuardScrollDrop));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Captain Lighthouse Passes Dropped");
                        AddTextField(valuex, y, valuewidth, height, 7, string.Format("{0}", CoreAI.CaptainGuardNumLighthousePasses));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Cave Portal Availability (seconds)");
                        AddTextField(valuex, y, valuewidth, height, 8, string.Format("{0}", CoreAI.CavePortalAvailability));
                        y += (height + 2);
                        //					AddLabelCropped( paramx, y, paramwidth, height, LabelHue, "Captain Leather sets Dropped" );
                        //					AddTextField( valuex, y, valuewidth, height, 8, string.Format("{0}",CoreAI.CaptainGuardLeatherSets));
                        //					y += (height+2);
                        //					AddLabelCropped( paramx, y, paramwidth, height, LabelHue, "Captain Ringmail sets Dropped" );
                        //					AddTextField( valuex, y, valuewidth, height, 9, string.Format("{0}",CoreAI.CaptainGuardRingSets));
                        //					y += (height+2);

                        AddButtonLabeled(paramx, y - 1, GetButtonID(4, 0), "Set");

                        break;
                    }
                case AIAdminGumpPage.SpiritSpawn_Spawn:
                    {
                        //					AddLabelCropped( paramx, y, paramwidth, height, LabelHue, "respawn frequency" );
                        //					AddTextField( valuex, y, valuewidth, height, 0, string.Format("{0}",CoreAI.SpiritRespawnFreq));
                        //					y += (height+2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Spirit Restart Delay (minutes)");
                        AddTextField(valuex, y, valuewidth, height, 0, string.Format("{0}", CoreAI.SpiritRestartDelay));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Spirit Expire Delay (minutes)");
                        AddTextField(valuex, y, valuewidth, height, 1, string.Format("{0}", CoreAI.SpiritExpireDelay));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Escape Portal Availability (seconds)");
                        AddTextField(valuex, y, valuewidth, height, 2, string.Format("{0}", CoreAI.SpiritPortalAvailablity));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "First Wave: Num spirits");
                        AddTextField(valuex, y, valuewidth, height, 3, string.Format("{0}", CoreAI.SpiritFirstWaveNumber));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "First Wave: HP per spirit");
                        AddTextField(valuex, y, valuewidth, height, 4, string.Format("{0}", CoreAI.SpiritFirstWaveHP));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Second Wave: Num spirits");
                        AddTextField(valuex, y, valuewidth, height, 5, string.Format("{0}", CoreAI.SpiritSecondWaveNumber));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Second Wave: HP per spirit");
                        AddTextField(valuex, y, valuewidth, height, 6, string.Format("{0}", CoreAI.SpiritSecondWaveHP));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Third Wave: Num spirits");
                        AddTextField(valuex, y, valuewidth, height, 7, string.Format("{0}", CoreAI.SpiritThirdWaveNumber));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Third Wave: HP per spirit");
                        AddTextField(valuex, y, valuewidth, height, 8, string.Format("{0}", CoreAI.SpiritThirdWaveHP));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Boss: HP");
                        AddTextField(valuex, y, valuewidth, height, 9, string.Format("{0}", CoreAI.SpiritBossHP));
                        y += (height + 2);

                        AddButtonLabeled(paramx, y - 1, GetButtonID(5, 100), "Set");

                        goto case AIAdminGumpPage.SpiritSpawn;
                    }
                case AIAdminGumpPage.SpiritSpawn_Depot:
                    {
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Number of greater heal pots");
                        AddTextField(valuex, y, valuewidth, height, 0, string.Format("{0}", CoreAI.SpiritDepotGHPots));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Number of bandies");
                        AddTextField(valuex, y, valuewidth, height, 1, string.Format("{0}", CoreAI.SpiritDepotBandies));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Number of reagents");
                        AddTextField(valuex, y, valuewidth, height, 2, string.Format("{0}", CoreAI.SpiritDepotReagents));
                        y += (height + 2);
                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Respawn frequency (seconds)");
                        AddTextField(valuex, y, valuewidth, height, 3, string.Format("{0}", CoreAI.SpiritDepotRespawnFreq));
                        y += (height + 2);

                        AddButtonLabeled(paramx, y - 1, GetButtonID(5, 200), "Set");

                        goto case AIAdminGumpPage.SpiritSpawn;
                    }
                case AIAdminGumpPage.SpiritSpawn:
                    {
                        AddPageButton(200, 20, GetButtonID(5, 0), "Spawn Defs", AIAdminGumpPage.SpiritSpawn_Spawn);
                        AddPageButton(200, 40, GetButtonID(5, 1), "Supply Depot", AIAdminGumpPage.SpiritSpawn_Depot);
                        break;
                    }
                default:
                    {
                        //Huh?
                        break;
                    }
            }
        }

        public void AddTextField(int x, int y, int width, int height, int index)
        {
            AddBackground(x - 2, y - 2, width + 4, height + 4, 0x2486);
            AddTextEntry(x + 2, y + 2, width - 4, height - 4, 0, index, "");
        }
        public void AddTextField(int x, int y, int width, int height, int index, string initialvalue)
        {
            AddBackground(x - 2, y - 2, width + 4, height + 4, 0x2486);
            AddTextEntry(x + 2, y + 2, width - 4, height - 4, 0, index, initialvalue);
        }


        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            int val = info.ButtonID - 1;

            if (val < 0)
                return;

            Mobile from = m_From;

            if (from.AccessLevel < AccessLevel.Administrator)
                return;


            int type = val % 10;
            int index = val / 10;

            switch (type)
            {
                case 0: //main page selection
                    {
                        AIAdminGumpPage page;

                        switch (index)
                        {
                            case 0: page = AIAdminGumpPage.Items; break;
                            case 1: page = AIAdminGumpPage.CellGuards; break;
                            case 2: page = AIAdminGumpPage.PostGuards; break;
                            case 3: page = AIAdminGumpPage.CaptainGuard; break;
                            case 4: page = AIAdminGumpPage.SpiritSpawn; break;
                            default: return;
                        }

                        from.SendGump(new AIAdminGump(from, page, 0, null, null, null));
                        break;
                    }
                case 1: //Items menu
                    {
                        int MinStingerHP = GetResultValueInt(info.GetTextEntry(0), CoreAI.StingerMinHP);
                        int MaxStingerHP = GetResultValueInt(info.GetTextEntry(1), CoreAI.StingerMaxHP);
                        int MinStingerDamage = GetResultValueInt(info.GetTextEntry(2), CoreAI.StingerMinDamage);
                        int MaxStingerDamage = GetResultValueInt(info.GetTextEntry(3), CoreAI.StingerMaxDamage);
                        //Validate
                        if (MaxStingerHP >= MinStingerHP)
                        {
                            //set values
                            from.SendMessage("Setting Stinger min/max HP (" + MinStingerHP + "/" + MaxStingerHP + ")");
                            CoreAI.StingerMinHP = MinStingerHP;
                            CoreAI.StingerMaxHP = MaxStingerHP;
                        }
                        else
                        {
                            //ewwow!
                            from.SendMessage("Error: Min HP must be less than Max HP");
                        }

                        if (MaxStingerDamage >= MinStingerDamage)
                        {
                            from.SendMessage("Setting Stinger min/max Damage (" + MinStingerDamage + "/" + MaxStingerDamage + ")");
                            CoreAI.StingerMinDamage = MinStingerDamage;
                            CoreAI.StingerMaxDamage = MaxStingerDamage;
                        }
                        else
                        {
                            from.SendMessage("Error: Min Damage must be less than Max Damage");
                        }

                        //restart gump
                        from.SendGump(new AIAdminGump(from, AIAdminGumpPage.Items, 0, null, null, null));
                        break;
                    }
                case 2: //Cell Guard menu
                    {
                        //					int SpawnFreq = GetResultValueInt(info.GetTextEntry(0), CoreAI.CellGuardSpawnFreq);
                        int Strength = GetResultValueInt(info.GetTextEntry(0), CoreAI.CellGuardStrength);
                        int Skill = GetResultValueInt(info.GetTextEntry(1), CoreAI.CellGuardSkillLevel);
                        int Regs = GetResultValueInt(info.GetTextEntry(2), CoreAI.CellGuardNumRegDrop);

                        //Validate and Set
                        //					CoreAI.CellGuardSpawnFreq = SpawnFreq;
                        CoreAI.CellGuardStrength = Strength;
                        CoreAI.CellGuardSkillLevel = Skill;
                        CoreAI.CellGuardNumRegDrop = Regs;

                        from.SendGump(new AIAdminGump(from, AIAdminGumpPage.CellGuards, 0, null, null, null));
                        break;
                    }
                case 3: //Post Guard
                    {
                        //					int PostGuardSpawnFreq = GetResultValueInt(info.GetTextEntry(0), CoreAI.PostGuardSpawnFreq);
                        int RestartDelay = GetResultValueInt(info.GetTextEntry(0), CoreAI.GuardSpawnRestartDelay);
                        int ExpireDelay = GetResultValueInt(info.GetTextEntry(1), CoreAI.GuardSpawnExpireDelay);
                        int PostGuardStrength = GetResultValueInt(info.GetTextEntry(2), CoreAI.PostGuardStrength);
                        int PostGuardSkillLevel = GetResultValueInt(info.GetTextEntry(3), CoreAI.PostGuardSkillLevel);
                        int PostGuardNumRegDrop = GetResultValueInt(info.GetTextEntry(4), CoreAI.PostGuardNumRegDrop);
                        int PostGuardNumBandiesDrop = GetResultValueInt(info.GetTextEntry(5), CoreAI.PostGuardNumBandiesDrop);
                        int PostGuardNumGHPotDrop = GetResultValueInt(info.GetTextEntry(6), CoreAI.PostGuardNumGHPotDrop);

                        //Validate and Set
                        //					CoreAI.PostGuardSpawnFreq = PostGuardSpawnFreq;
                        CoreAI.GuardSpawnRestartDelay = RestartDelay;
                        CoreAI.GuardSpawnExpireDelay = ExpireDelay;
                        CoreAI.PostGuardStrength = PostGuardStrength;
                        CoreAI.PostGuardSkillLevel = PostGuardSkillLevel;
                        CoreAI.PostGuardNumRegDrop = PostGuardNumRegDrop;
                        CoreAI.PostGuardNumBandiesDrop = PostGuardNumBandiesDrop;
                        CoreAI.PostGuardNumGHPotDrop = PostGuardNumGHPotDrop;

                        from.SendGump(new AIAdminGump(from, AIAdminGumpPage.PostGuards, 0, null, null, null));
                        break;
                    }
                case 4: //Captain Guard
                    {
                        int CaptainGuardStrength = GetResultValueInt(info.GetTextEntry(0), CoreAI.CaptainGuardStrength);
                        int CaptainGuardSkillLevel = GetResultValueInt(info.GetTextEntry(1), CoreAI.CaptainGuardSkillLevel);
                        int CaptainGuardWeapDrop = GetResultValueInt(info.GetTextEntry(2), CoreAI.CaptainGuardWeapDrop);
                        int CaptainGuardNumRegDrop = GetResultValueInt(info.GetTextEntry(3), CoreAI.CaptainGuardNumRegDrop);
                        int CaptainGuardNumBandiesDrop = GetResultValueInt(info.GetTextEntry(4), CoreAI.CaptainGuardNumBandiesDrop);
                        int CaptainGuardGHPotsDrop = GetResultValueInt(info.GetTextEntry(5), CoreAI.CaptainGuardGHPotsDrop);
                        int CaptainGuardScrollDrop = GetResultValueInt(info.GetTextEntry(6), CoreAI.CaptainGuardScrollDrop);
                        int CaptainGuardNumLighthousePasses = GetResultValueInt(info.GetTextEntry(7), CoreAI.CaptainGuardNumLighthousePasses);
                        int CavePortalAvailability = GetResultValueInt(info.GetTextEntry(8), CoreAI.CavePortalAvailability);
                        //					int CaptainGuardLeatherSets = GetResultValueInt(info.GetTextEntry(8), CoreAI.CaptainGuardLeatherSets);
                        //					int CaptainGuardRingSets = GetResultValueInt(info.GetTextEntry(9), CoreAI.CaptainGuardRingSets);

                        //Validate and Set
                        CoreAI.CaptainGuardStrength = CaptainGuardStrength;
                        CoreAI.CaptainGuardSkillLevel = CaptainGuardSkillLevel;
                        CoreAI.CaptainGuardWeapDrop = CaptainGuardWeapDrop;
                        CoreAI.CaptainGuardNumRegDrop = CaptainGuardNumRegDrop;
                        CoreAI.CaptainGuardNumBandiesDrop = CaptainGuardNumBandiesDrop;
                        CoreAI.CaptainGuardGHPotsDrop = CaptainGuardGHPotsDrop;
                        CoreAI.CaptainGuardScrollDrop = CaptainGuardScrollDrop;
                        CoreAI.CaptainGuardNumLighthousePasses = CaptainGuardNumLighthousePasses;
                        CoreAI.CavePortalAvailability = CavePortalAvailability;
                        //					CoreAI.CaptainGuardLeatherSets = CaptainGuardLeatherSets;
                        //					CoreAI.CaptainGuardRingSets = CaptainGuardRingSets;

                        from.SendGump(new AIAdminGump(from, AIAdminGumpPage.CaptainGuard, 0, null, null, null));
                        break;
                    }
                case 5: //Spirit Spawn
                    {
                        AIAdminGumpPage page = AIAdminGumpPage.SpiritSpawn;
                        switch (index)
                        {
                            case 0:
                                {
                                    page = AIAdminGumpPage.SpiritSpawn_Spawn;
                                    break;
                                }
                            case 1:
                                {
                                    page = AIAdminGumpPage.SpiritSpawn_Depot;
                                    break;
                                }
                            case 100: //validate/save Spawn
                                {
                                    //							int SpiritRespawnFreq = GetResultValueInt(info.GetTextEntry(0), CoreAI.SpiritRespawnFreq);
                                    int SpiritRestartDelay = GetResultValueInt(info.GetTextEntry(0), CoreAI.SpiritRestartDelay);
                                    int SpiritExpireDelay = GetResultValueInt(info.GetTextEntry(1), CoreAI.SpiritExpireDelay);
                                    int SpiritPortalAvailablity = GetResultValueInt(info.GetTextEntry(2), CoreAI.SpiritPortalAvailablity);
                                    int SpiritFirstWaveNumber = GetResultValueInt(info.GetTextEntry(3), CoreAI.SpiritFirstWaveNumber);
                                    int SpiritFirstWaveHP = GetResultValueInt(info.GetTextEntry(4), CoreAI.SpiritFirstWaveHP);
                                    int SpiritSecondWaveNumber = GetResultValueInt(info.GetTextEntry(5), CoreAI.SpiritSecondWaveNumber);
                                    int SpiritSecondWaveHP = GetResultValueInt(info.GetTextEntry(6), CoreAI.SpiritSecondWaveHP);
                                    int SpiritThirdWaveNumber = GetResultValueInt(info.GetTextEntry(7), CoreAI.SpiritThirdWaveNumber);
                                    int SpiritThirdWaveHP = GetResultValueInt(info.GetTextEntry(8), CoreAI.SpiritThirdWaveHP);
                                    int SpiritBossHP = GetResultValueInt(info.GetTextEntry(9), CoreAI.SpiritBossHP);

                                    //verify and save
                                    //							CoreAI.SpiritRespawnFreq = SpiritRespawnFreq;
                                    CoreAI.SpiritRestartDelay = SpiritRestartDelay;
                                    CoreAI.SpiritExpireDelay = SpiritExpireDelay;
                                    CoreAI.SpiritPortalAvailablity = SpiritPortalAvailablity;
                                    CoreAI.SpiritFirstWaveNumber = SpiritFirstWaveNumber;
                                    CoreAI.SpiritFirstWaveHP = SpiritFirstWaveHP;
                                    CoreAI.SpiritSecondWaveNumber = SpiritSecondWaveNumber;
                                    CoreAI.SpiritSecondWaveHP = SpiritSecondWaveHP;
                                    CoreAI.SpiritThirdWaveNumber = SpiritThirdWaveNumber;
                                    CoreAI.SpiritThirdWaveHP = SpiritThirdWaveHP;
                                    CoreAI.SpiritBossHP = SpiritBossHP;

                                    page = AIAdminGumpPage.SpiritSpawn_Spawn;
                                    break;
                                }
                            case 200: //validate/save Depot
                                {
                                    int SpiritDepotGHPots = GetResultValueInt(info.GetTextEntry(0), CoreAI.SpiritDepotGHPots);
                                    int SpiritDepotBandies = GetResultValueInt(info.GetTextEntry(1), CoreAI.SpiritDepotBandies);
                                    int SpiritDepotReagents = GetResultValueInt(info.GetTextEntry(2), CoreAI.SpiritDepotReagents);
                                    int SpiritDepotRespawnFreq = GetResultValueInt(info.GetTextEntry(3), CoreAI.SpiritDepotRespawnFreq);

                                    //save and validate
                                    CoreAI.SpiritDepotGHPots = SpiritDepotGHPots;
                                    CoreAI.SpiritDepotBandies = SpiritDepotBandies;
                                    CoreAI.SpiritDepotReagents = SpiritDepotReagents;
                                    CoreAI.SpiritDepotRespawnFreq = SpiritDepotRespawnFreq;

                                    page = AIAdminGumpPage.SpiritSpawn_Depot;
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }

                        from.SendGump(new AIAdminGump(from, page, 0, null, null, null));
                        break;
                    }

            }
        }


        //SMD: my utility functions
        int GetResultValueInt(TextRelay relay, int defaultValue)
        {
            int iReturn = defaultValue;
            string text = (relay == null ? null : relay.Text.Trim());
            if (text != null)
            {
                if (text.Length > 0)
                {
                    iReturn = Int32.Parse(text);
                }
            }
            return iReturn;
        }

    }//end class AIAdminGump
}