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

/* Engines/Factions/Gumps/New Gumps/BaseFactionGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using System;

namespace Server.Factions.NewGumps
{
    public enum GumpStyle : byte
    {
        Parchment,
        StoneSlab,
    }

    public abstract class BaseFactionGump : Gump
    {
        protected virtual GumpStyle Style { get { return GumpStyle.StoneSlab; } }

        protected BaseFactionGump()
            : base(20, 30)
        {
        }

        public override int GetGumpID()
        {
            return FactionGumps.GetGumpID(this.GetType());
        }

        protected void AddBackground(int width, int height)
        {
            if (Style == GumpStyle.Parchment)
                AddBackground(0, 0, width, height, 0x24AE);
            else
                AddBackground(0, 0, width, height, 0x242C);
        }

        protected void AddSeparator(int x, int y, int width)
        {
            if (Style == GumpStyle.Parchment)
                AddImageTiled(x, y, width, 4, 0x238D);
            else
                AddImageTiled(x, y, width, 2, 0x2711);
        }

        protected void AddTextBox(int x, int y, int width)
        {
            if (Style == GumpStyle.Parchment)
            {
                AddImageTiled(x, y, width, 26, 0xA40);
                AddImageTiled(x + 2, y + 2, width - 4, 22, 0xBBC);
            }
            else
            {
                AddBackground(x, y, width, 26, 0x2486);
            }
        }

        protected void AddHtmlText(int x, int y, int width, int height, TextDefinition def, bool back, bool scroll)
        {
            TextDefinition.AddHtmlText(this, x, y, width, height, def, back, scroll);
        }

        protected void AddHtmlText(int x, int y, int width, int height, TextDefinition def, bool back, bool scroll, int numberColor, int stringColor)
        {
            TextDefinition.AddHtmlText(this, x, y, width, height, def, back, scroll, numberColor, stringColor);
        }

        protected void AddStatistic(int x, int y, int width, TextDefinition key, TextDefinition value)
        {
            AddTextBox(x, y, width);

            AddHtmlText(x + 5, y + 3, width - 10, 20, key, false, false);

            AddHtmlText(x + 8 + width, y + 3, 200, 20, value, false, false);
        }

        protected void AddStatisticLabel(int x, int y, TextDefinition key, string value, int color)
        {
            AddHtmlText(x, y, 200, 25, key, false, false);

            AddLabel(x + 210, y, color, value);
        }

        protected void AddButtonLabeled(int x, int y, int width, int buttonID, TextDefinition text, bool selected = false)
        {
            AddBackground(x, y, width, 26, 0x2486);

            AddButton(x + 5, y + 5, 0x845, 0x846, buttonID, GumpButtonType.Reply, 0);

            AddHtmlText(x + 30, y + 3, width - 35, 20, text, false, false, selected ? 0x0F : -1, selected ? 0x7B : -1);
        }

        protected void AddPageButtonLabeled(int x, int y, int width, int pageID, TextDefinition text)
        {
            AddBackground(x, y, width, 26, 0x2486);

            AddButton(x + 5, y + 5, 0x845, 0x846, 0, GumpButtonType.Page, pageID);

            AddHtmlText(x + 30, y + 3, width - 35, 20, text, false, false);
        }

        protected void AddItemCentered(int x, int y, int width, int height, int itemID)
        {
            Rectangle2D bounds = ItemBounds.Table[itemID & TileData.MaxItemValue];

            int dx = (width - bounds.Start.X - bounds.End.X) / 2;
            int dy = (height - bounds.Start.Y - bounds.End.Y) / 2;

            AddItem(x + dx, y + dy, itemID);
        }

        protected static string FormatName(Mobile m)
        {
            return (m == null ? null : m.Name);
        }

        protected static string FormatFaction(Faction f)
        {
            return (f == null ? null : f.Definition.FriendlyName);
        }

        protected static string FormatGuild(Mobile m)
        {
            return ((m == null || m.Guild == null ? null : string.Concat('(', m.Guild.Abbreviation, ')')));
        }

        protected static string FormatRole(PlayerState pl)
        {
            if (pl.Mobile == pl.Faction.Commander)
                return "Commander";

            if (pl.Sheriff != null)
                return string.Concat("Sheriff of ", pl.Sheriff.Definition.FriendlyName);

            if (pl.Finance != null)
                return string.Concat("Finance MIN of ", pl.Finance.Definition.FriendlyName);

            int titleIndex = (int)pl.MerchantTitle - 1;

            if (titleIndex >= 0 && titleIndex < MerchantTitles.Info.Length)
                return MerchantTitles.Info[titleIndex].Label.String;

            return "Member";
        }
    }
}