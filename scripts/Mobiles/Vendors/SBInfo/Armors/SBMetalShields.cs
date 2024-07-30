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

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBMetalShields : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBMetalShields()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Buckler), BaseVendor.PlayerPays(typeof(Buckler)), 20, 0x1B73, 0));
                Add(new GenericBuyInfo(typeof(BronzeShield), BaseVendor.PlayerPays(typeof(BronzeShield)), 20, 0x1B72, 0));
                Add(new GenericBuyInfo(typeof(MetalShield), BaseVendor.PlayerPays(typeof(MetalShield)), 20, 0x1B7B, 0));
                Add(new GenericBuyInfo(typeof(MetalKiteShield), BaseVendor.PlayerPays(typeof(MetalKiteShield)), 20, 0x1B74, 0));
                Add(new GenericBuyInfo(typeof(HeaterShield), BaseVendor.PlayerPays(typeof(HeaterShield)), 20, 0x1B76, 0));
                Add(new GenericBuyInfo(typeof(WoodenKiteShield), BaseVendor.PlayerPays(typeof(WoodenKiteShield)), 20, 0x1B78, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {
                    Add(typeof(Buckler), BaseVendor.VendorPays(typeof(Buckler)));
                    Add(typeof(BronzeShield), BaseVendor.VendorPays(typeof(BronzeShield)));
                    Add(typeof(MetalShield), BaseVendor.VendorPays(typeof(MetalShield)));
                    Add(typeof(MetalKiteShield), BaseVendor.VendorPays(typeof(MetalKiteShield)));
                    Add(typeof(HeaterShield), BaseVendor.VendorPays(typeof(HeaterShield)));
                    Add(typeof(WoodenKiteShield), BaseVendor.VendorPays(typeof(WoodenKiteShield)));
                }
            }
        }
    }
}