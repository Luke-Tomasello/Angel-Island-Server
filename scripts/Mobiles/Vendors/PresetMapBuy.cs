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

/* Scripts/Mobiles/Vendors/PresetMapBuy.cs
 * Changelog
 *  07/02/05 Taran Kain
 *		Made constructor correctly set type, was causing crashes
 */

using Server.Items;

namespace Server.Mobiles
{
    public class PresetMapBuyInfo : GenericBuyInfo
    {
        private PresetMapEntry m_Entry;

        public PresetMapBuyInfo(PresetMapEntry entry, int price, int amount)
            : base(entry.Name.ToString(), typeof(Server.Items.PresetMap), price, amount, 0x14EC, 0)
        {
            m_Entry = entry;
        }

        public override object GetObject()
        {
            return new PresetMap(m_Entry);
        }
    }
}