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

using System.Text;

namespace Scripts.Gumps.GumpBuilder
{
    public class GumpBuilderTab
    {
        public string DisplayName { get; protected set; }

        public StringBuilder Content { get; protected set; }

        public int Index { get; protected set; }

        public bool IsHtml { get; protected set; }

        public GumpBuilderTab(string displayName, int index = 0, bool isHtml = true)
        {
            Content = new StringBuilder();

            DisplayName = displayName;

            Index = index;

            IsHtml = isHtml;
        }

        public void AddLine(string content)
        {
            Content.Append($"{content}");
            Content.Append(IsHtml ? "<br />" : "\r\n");
        }

        public void Clear()
        {
            Content.Clear();
        }
    }
}