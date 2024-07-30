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

/* Scripts/Mobiles/Vendors/SBInfo/SBMarketStall.cs
 * ChangeLog
 *  1/14/24, Yoar
 *      Initial version.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBMarketStall : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBMarketStall()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo("Small Vesper Market Stall", typeof(SmallVesperMarketStallDeed), BaseVendor.PlayerPays(typeof(SmallVesperMarketStallDeed)), 20, 0x14F0, 0x0));
                Add(new GenericBuyInfo("Large Vesper Market Stall", typeof(LargeVesperMarketStallDeed), BaseVendor.PlayerPays(typeof(LargeVesperMarketStallDeed)), 20, 0x14F0, 0x0));
                Add(new GenericBuyInfo("Small Nujel'm Market Stall", typeof(SmallNujelmMarketStallDeed), BaseVendor.PlayerPays(typeof(SmallNujelmMarketStallDeed)), 20, 0x14F0, 0x0));
                Add(new GenericBuyInfo("Large Nujel'm Market Stall", typeof(LargeNujelmMarketStallDeed), BaseVendor.PlayerPays(typeof(LargeNujelmMarketStallDeed)), 20, 0x14F0, 0x0));
                Add(new GenericBuyInfo("Small Britain Market Stall", typeof(SmallBritainMarketStallDeed), BaseVendor.PlayerPays(typeof(SmallBritainMarketStallDeed)), 20, 0x14F0, 0x0));
                Add(new GenericBuyInfo("Large Britain Market Stall", typeof(LargeBritainMarketStallDeed), BaseVendor.PlayerPays(typeof(LargeBritainMarketStallDeed)), 20, 0x14F0, 0x0));
                Add(new GenericBuyInfo("Small Minoc Market Stall", typeof(SmallMinocMarketStallDeed), BaseVendor.PlayerPays(typeof(SmallMinocMarketStallDeed)), 20, 0x14F0, 0x0));
                Add(new GenericBuyInfo("Large Minoc Market Stall", typeof(LargeMinocMarketStallDeed), BaseVendor.PlayerPays(typeof(LargeMinocMarketStallDeed)), 20, 0x14F0, 0x0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
            }
        }
    }
}