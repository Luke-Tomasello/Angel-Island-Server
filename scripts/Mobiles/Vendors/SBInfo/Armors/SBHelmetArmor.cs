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
    public class SBHelmetArmor : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBHelmetArmor()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Bascinet), BaseVendor.PlayerPays(typeof(Bascinet)), 20, 0x140C, 0));
                Add(new GenericBuyInfo(typeof(CloseHelm), BaseVendor.PlayerPays(typeof(CloseHelm)), 20, 0x1408, 0));
                Add(new GenericBuyInfo(typeof(Helmet), BaseVendor.PlayerPays(typeof(Helmet)), 20, 0x140A, 0));
                Add(new GenericBuyInfo(typeof(NorseHelm), BaseVendor.PlayerPays(typeof(NorseHelm)), 20, 0x140E, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {
                    Add(typeof(Bascinet), BaseVendor.VendorPays(typeof(Bascinet)));
                    Add(typeof(CloseHelm), BaseVendor.VendorPays(typeof(CloseHelm)));
                    Add(typeof(Helmet), BaseVendor.VendorPays(typeof(Helmet)));
                    Add(typeof(NorseHelm), BaseVendor.VendorPays(typeof(NorseHelm)));
                }
            }
        }
    }
}