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

/* Engines/Township/Items/TownshipItemRegistry.cs
 * CHANGELOG:
 * 1/13/21, Yoar
 *	    Initial version.
 */

using System;

namespace Server.Township
{
    [PropertyObject]
    public class TownshipItemContext
    {
        private Item m_Item;
        private Mobile m_Owner;
        private DateTime m_Placed;

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Item
        {
            get { return m_Item; }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public DateTime Placed
        {
            get { return m_Placed; }
            set { m_Placed = value; }
        }

        public TownshipItemContext(Item item)
        {
            m_Item = item;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((Mobile)m_Owner);
            writer.Write((DateTime)m_Placed);
        }

        public TownshipItemContext(GenericReader reader, Item item, int systemVersion)
        {
            m_Item = item;

            m_Owner = reader.ReadMobile();
            m_Placed = reader.ReadDateTime();
        }

        public override string ToString()
        {
            return "...";
        }
    }
}