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
using System;
using System.Text;

namespace Scripts.Gumps.GumpBuilder
{
    public abstract class MessageGumpBuilder : BaseGumpBuilder
    {
        public string Message { get; set; }

        public MessageGumpResponseLayout ResponseLayout { get; set; }

        protected int TextLineLength = 50;

        protected int TextOffsetX = 55;

        protected int TextOffsetY = 20;

        public MessageGumpBuilder(Mobile from, string title, string message) : this(from, title, message, MessageGumpResponseLayout.Confirm) { }
        public MessageGumpBuilder(Mobile from, string title, string message, MessageGumpResponseLayout responseLayout) : base(from, title, GumpBackgroundType.Grey, 200, 320)
        {
            ResponseLayout = responseLayout;

            Message = message;

            InitializeUi();
        }

        protected override void InitializeUi()
        {
            base.InitializeUi();

            StringBuilder sb = new StringBuilder(Message);
            for (var i = 0; i < Message.Length; i += TextLineLength)
            {
                sb.Insert(i, Environment.NewLine);
            }

            AddLabel(TextOffsetX, TextOffsetY, LabelHue, sb.ToString());
        }
    }
}