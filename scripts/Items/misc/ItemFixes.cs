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

/* Scripts/Item/ItemFixes.cs
 * ChangeLog
 *  4/26/23, Yoar
 *      Patched name of executioner's caps
 *  4/21/23, Yoar
 *      Added reagent, spell scroll patches
 *  4/20/23, Yoar
 *      Initial version. Use this class to add server-side fixes to item data.
 *      
 *      Fixed the name of bola balls.
 */

using System;

namespace Server.Items
{
    public static class ItemFixes
    {
        public static void Initialize()
        {
            PatchItemNames();
        }

        private static void PatchItemNames()
        {
            PatchItemName(0x0E73, "bola ball%s", Article.A);
            PatchItemName(0x0F83, "Executioner's Cap%s", Article.None);

            // patch reagents
            PatchLowerCase(0x0F78, 0x0F91);

            PatchSpellScrolls();
        }

        private static void PatchItemName(int itemID, string name, Article article)
        {
            ItemData id = TileData.ItemTable[itemID & TileData.MaxItemValue];

            id.Name = name;

            if (article == Article.A)
                id.Flags |= TileFlag.ArticleA;
            else
                id.Flags &= ~TileFlag.ArticleA;

            if (article == Article.An)
                id.Flags |= TileFlag.ArticleAn;
            else
                id.Flags &= ~TileFlag.ArticleAn;

            TileData.ItemTable[itemID & TileData.MaxItemValue] = id;
        }

        private static void PatchLowerCase(int start, int end)
        {
            for (int itemID = start; itemID <= end; itemID++)
            {
                ItemData id = TileData.ItemTable[itemID & TileData.MaxItemValue];

                id.Name = id.Name.ToLower();

                TileData.ItemTable[itemID & TileData.MaxItemValue] = id;
            }
        }

        private static void PatchSpellScrolls()
        {
            for (int i = 0; i < 64; i++)
            {
                int offset;

                // why must OSI be so cruel
                if (i < 6)
                    offset = i + 1;
                else if (i == 6)
                    offset = 0;
                else
                    offset = i;

                int itemID = 0x1F2D + offset;
                int cliloc = 1027981 + offset;

                string clilocStr;

                if (Server.Text.Cliloc.Lookup.TryGetValue(cliloc, out clilocStr))
                {
                    ItemData id = TileData.ItemTable[itemID & TileData.MaxItemValue];

                    id.Name = string.Concat(clilocStr.ToLower(), " scroll%s");

                    TileData.ItemTable[itemID & TileData.MaxItemValue] = id;
                }
            }
        }
    }
}