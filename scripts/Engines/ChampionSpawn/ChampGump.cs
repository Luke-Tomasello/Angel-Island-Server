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

/* Scripts/Engines/ChampionSpawn/ChampGump.cs
 * created 5/6/04 by mith: used to set/retrieve variables from CoreChamp.cs
 * ChangeLog
 *  03/09/07, plasma    
 *      Removed cannedevil namespace reference
 *  01/05/07, plasma!
 *      Changed CannedEvil namespace to ChampionSpawn for cleanup!
 *      Changed old ChampionSpawn to ChampEngine
 *  7/24/06, Rhiannon
 *		Commented out GetHueFor(), m_AccessLevelStrings, and FormatAccessLevel().
 *	5/9/04, mith
 *		Fixed some code in OnResponse() that was throwing a warning at compile time.
 * TODO
 */

using Server.Engines.ChampionSpawn;
using System;

namespace Server.Gumps
{

    public enum ChampGumpPage
    {
        //Main menu items
        SpawnAndLootLevels
    }

    public class ChampGump : Gump
    {
        private Mobile m_From;
        private ChampEngine m_Spawn;

        private ChampGumpPage m_PageType;
        //private ArrayList m_List;
        //private int m_ListPage;
        //private object m_State;

        private const int LabelColor = 0x7FFF;
        private const int SelectedColor = 0x421F;
        private const int DisabledColor = 0x4210;

        private const int LabelColor32 = 0xFFFFFF;
        private const int SelectedColor32 = 0x8080FF;
        private const int DisabledColor32 = 0x808080;

        private const int LabelHue = 0x480;
        private const int GreenHue = 0x40;
        private const int RedHue = 0x20;

        #region HTML Form Creation Utilities
        public void AddPageButton(int x, int y, int buttonID, string text, ChampGumpPage page, params ChampGumpPage[] subPages)
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
        #endregion

        //		public static void Initialize()
        //		{
        //			Commands.Register( "ChampAdmin", AccessLevel.GameMaster, new CommandEventHandler( ChampAdmin_OnCommand ) );
        //		}

        //		[Usage( "ChampAdmin" )]
        //		[Description( "Opens an interface providing access to all Champion Spawn settings." )]
        //		public static void ChampAdmin_OnCommand( CommandEventArgs e )
        //		{
        //			e.Mobile.SendGump( new ChampGump( e.Mobile, ChampGumpPage.SpawnLevels, 0, null, null, null ) );
        //		}

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
        //
        ////		private static string[] m_AccessLevelStrings = new string[]
        ////			{
        ////				"Player",
        ////				"Counselor",
        ////				"Game Master",
        ////				"Seer",
        ////				"Administrator"
        ////			};
        ////
        ////		public static string FormatAccessLevel( AccessLevel level )
        ////		{
        ////			int v = (int)level;
        ////
        ////			if ( v >= 0 && v < m_AccessLevelStrings.Length )
        ////				return m_AccessLevelStrings[v];
        ////
        ////			return "Unknown";
        ////		}
        //
        public ChampGump(Mobile from, ChampEngine spawn, ChampGumpPage pageType)
            : base(50, 40)
        {
            from.CloseGump(typeof(ChampGump));

            m_From = from;
            m_Spawn = spawn;
            m_PageType = pageType;
            //m_ListPage = 0;
            //m_State = null;
            //m_List = null;

            AddPage(0);

            AddBackground(0, 0, 420, 440, 5054);

            AddBlackAlpha(10, 10, 170, 100);
            AddBlackAlpha(190, 10, 220, 100);
            AddBlackAlpha(10, 120, 400, 260);
            AddBlackAlpha(10, 390, 400, 40);

            //AddPageButton( 10, 10, GetButtonID( 0, 0 ), "Spawn and Loot Levels", ChampGumpPage.SpawnAndLootLevels );
            //pla: changed this as removed spawn amounts 
            AddPageButton(10, 10, GetButtonID(0, 0), "Loot Levels", ChampGumpPage.SpawnAndLootLevels);

            //			if ( notice != null )
            //				AddHtml( 12, 392, 396, 36, Color( notice, LabelColor32 ), false, false );

            int paramwidth = 250;
            int valuewidth = 50;
            int paramx = 12;
            int valuemin = paramx + paramwidth + 2;
            int valuemax = valuemin + valuewidth + 2;
            int y = 128;
            int height = 20;

            switch (pageType)
            {
                case ChampGumpPage.SpawnAndLootLevels:
                    {
                        // Spawn Levels

                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Spawn Level");
                        y += (height + 2);

                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Amount of Spawn for Level 1");
                        AddTextField(valuemax, y, valuewidth, height, 0, string.Format("{0}", CoreChamp.Level1SpawnAmount));
                        y += (height + 2);

                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Amount of Spawn for Level 2");
                        AddTextField(valuemax, y, valuewidth, height, 1, string.Format("{0}", CoreChamp.Level2SpawnAmount));
                        y += (height + 2);

                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Amount of Spawn for Level 3");
                        AddTextField(valuemax, y, valuewidth, height, 2, string.Format("{0}", CoreChamp.Level3SpawnAmount));
                        y += (height + 2);

                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Amount of Spawn for Level 4");
                        AddTextField(valuemax, y, valuewidth, height, 3, string.Format("{0}", CoreChamp.Level4SpawnAmount));
                        y += (2 * height + 2);

                        // Loot	Levels
                        AddLabelCropped(valuemin, y, valuewidth, height, LabelHue, "Min");
                        AddLabelCropped(valuemax, y, valuewidth, height, LabelHue, "Max");
                        y += (height + 2);

                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Magic Loot Level (1=Ruin, 5=Vanq)");
                        AddTextField(valuemin, y, valuewidth, height, 4, string.Format("{0}", CoreChamp.MinChampMagicDropLevel));
                        AddTextField(valuemax, y, valuewidth, height, 5, string.Format("{0}", CoreChamp.MaxChampMagicDropLevel));
                        y += (height + 2);

                        AddLabelCropped(paramx, y, paramwidth, height, LabelHue, "Amount Of Magic Items to Drop");
                        AddTextField(valuemax, y, valuewidth, height, 6, string.Format("{0}", CoreChamp.AmountOfChampMagicItems));
                        y += (2 * height + 2);

                        AddButtonLabeled(paramx, y, GetButtonID(1, 0), "Set");

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

            if (from.AccessLevel < AccessLevel.GameMaster)
                return;


            int type = val % 10;
            int index = val / 10;

            switch (type)
            {
                case 0: //Main
                    {
                        ChampGumpPage page = ChampGumpPage.SpawnAndLootLevels;

                        switch (index)
                        {
                            case 0: page = ChampGumpPage.SpawnAndLootLevels; break;
                            default: return;
                        }

                        from.SendGump(new ChampGump(from, m_Spawn, page));
                        break;
                    }
                case 1: //Spawn Ranges
                    {

                        int Level1SpawnAmount = GetResultValueInt(info.GetTextEntry(0), CoreChamp.Level1SpawnAmount);
                        int Level2SpawnAmount = GetResultValueInt(info.GetTextEntry(1), CoreChamp.Level2SpawnAmount);
                        int Level3SpawnAmount = GetResultValueInt(info.GetTextEntry(2), CoreChamp.Level3SpawnAmount);
                        int Level4SpawnAmount = GetResultValueInt(info.GetTextEntry(3), CoreChamp.Level4SpawnAmount);

                        int MinMagicDropLevel = GetResultValueInt(info.GetTextEntry(4), CoreChamp.MinChampMagicDropLevel);
                        int MaxMagicDropLevel = GetResultValueInt(info.GetTextEntry(5), CoreChamp.MaxChampMagicDropLevel);
                        int AmountOfMagicLoot = GetResultValueInt(info.GetTextEntry(6), CoreChamp.AmountOfChampMagicItems);

                        //Validate & Set values	 \

                        CoreChamp.Level1SpawnAmount = Level1SpawnAmount;
                        CoreChamp.Level2SpawnAmount = Level2SpawnAmount;
                        CoreChamp.Level3SpawnAmount = Level3SpawnAmount;
                        CoreChamp.Level4SpawnAmount = Level4SpawnAmount;

                        //  4/16/2024, Adam: Now read-only
                        //if (MaxMagicDropLevel >= MinMagicDropLevel)
                        //{
                        //    CoreChamp.MinChampMagicDropLevel = MinMagicDropLevel;
                        //    CoreChamp.MaxChampMagicDropLevel = MaxMagicDropLevel;
                        //}
                        //else
                        //{
                        //    from.SendMessage("Error: Maximum Magic Loot Level is less than the Minimum!");
                        //}
                        // CoreChamp.AmountOfMagicItems = AmountOfMagicLoot;

                        //restart gump
                        from.SendGump(new ChampGump(from, m_Spawn, ChampGumpPage.SpawnAndLootLevels));
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
    }//end class ChampGump
}