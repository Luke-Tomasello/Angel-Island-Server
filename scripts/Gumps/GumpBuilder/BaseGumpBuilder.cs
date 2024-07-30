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

namespace Scripts.Gumps.GumpBuilder
{
    public abstract class BaseGumpBuilder : Gump
    {
        protected const int ScrollBgHex = 0x09d8;

        //protected const int GreyBgHex = 0x0a28;

        protected const int GreyBgHex = 0x23f0;

        protected const int ModernBgHex = 0x0a3c;

        protected const int LabelHue = 0x480;

        public string Title { get; protected set; }

        public Mobile From { get; protected set; }

        protected int Height { get; set; }

        protected int Width { get; set; }

        public GumpBackgroundType BackgroundType { get; protected set; }

        public BaseGumpBuilder(Mobile from, string title) : this(from, title, GumpBackgroundType.Modern, 200, 350) { }
        public BaseGumpBuilder(Mobile from, string title, GumpBackgroundType backgroundType) : this(from, title, backgroundType, 200, 350) { }
        public BaseGumpBuilder(Mobile from, string title, int height, int width) : this(from, title, GumpBackgroundType.Modern, height, width) { }
        public BaseGumpBuilder(Mobile from, string title, GumpBackgroundType backgroundType, int height, int width) : base(50, 10)
        {
            Title = title;

            From = from;

            BackgroundType = backgroundType;

            Height = height;

            Width = width;
        }

        protected virtual void InitializeUi()
        {
            var cancelBtnXOffset = 15;
            var cancelBtnYOffset = -15;

            AddPage(0);

            DrawBackground();

            DrawButton("Close", X + cancelBtnXOffset, Y + Height + cancelBtnYOffset - 23, 0);
        }

        protected void DrawBackground()
        {
            int backgroundHex;
            switch (BackgroundType)
            {
                default:
                case GumpBackgroundType.Modern:
                    backgroundHex = ModernBgHex;
                    break;
                case GumpBackgroundType.Grey:
                    backgroundHex = GreyBgHex;
                    break;
                case GumpBackgroundType.Scroll:
                    backgroundHex = ScrollBgHex;
                    break;
            }

            AddBackground(X, Y, Width, Height, backgroundHex);
        }

        protected void DrawButton(string labelText, int x, int y, int buttonId)
        {
            var labelOffsetX = 12;

            AddButton(x, y, 4005, 4007, buttonId, GumpButtonType.Reply, 0);
            AddLabel(x + 20 + labelOffsetX, y + 2, LabelHue, labelText);
        }
    }
}