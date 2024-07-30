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

/* Scripts/Engines/Craft/Core/CraftGroup.cs
 * CHANGELOG
 *  1/8/22, Yoar
 *      Simplified input structure of CraftRes, CraftGroup constructors using TextEntry.
 */

namespace Server.Engines.Craft
{
    public class CraftGroup
    {
        private CraftItemCol m_arCraftItem;
        private TextDefinition m_Name;

        public CraftGroup(TextDefinition name)
        {
            m_Name = name;
            m_arCraftItem = new CraftItemCol();
        }

        public void AddCraftItem(CraftItem craftItem)
        {
            m_arCraftItem.Add(craftItem);
        }

        public CraftItemCol CraftItems
        {
            get { return m_arCraftItem; }
        }

        public TextDefinition Name
        {
            get { return m_Name; }
        }
    }
}