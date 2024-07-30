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

/* scripts\Engines\Apiculture\Gumps\BaseApicultureGump.cs
 * CHANGELOG:
 *  8/8/23, Yoar
 *      Initial commit.
 */

using Server.Gumps;
using Server.Misc;

namespace Server.Engines.Apiculture
{
    public abstract class BaseApicultureGump : Gump
    {
        public BaseApicultureGump(int x, int y)
            : base(x, y)
        {
        }

        public void AddLabelCentered(int x, int y, int width, int hue, string text)
        {
            int offset = (width - FontHelper.Width(text)) / 2;

            AddLabel(x + offset, y, hue, text);
        }

        public void AddItemCentered(int x, int y, int width, int height, int itemID, int hue = 0)
        {
            Rectangle2D bounds = ItemBounds.Table[itemID & TileData.MaxItemValue];

            int dx = (width - bounds.Start.X - bounds.End.X) / 2;
            int dy = (height - bounds.Start.Y - bounds.End.Y) / 2;

            AddItem(x + dx, y + dy, itemID, hue);
        }
    }
}