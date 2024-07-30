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
    public class SBChainmailArmor : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBChainmailArmor()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(ChainChest), BaseVendor.PlayerPays(typeof(ChainChest)), 20, 0x13BF, 0));
                Add(new GenericBuyInfo(typeof(ChainLegs), BaseVendor.PlayerPays(typeof(ChainLegs)), 20, 0x13BE, 0));
                Add(new GenericBuyInfo(typeof(ChainCoif), BaseVendor.PlayerPays(typeof(ChainCoif)), 20, 0x13BB, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {
                    Add(typeof(ChainChest), BaseVendor.VendorPays(typeof(ChainChest)));
                    Add(typeof(ChainLegs), BaseVendor.VendorPays(typeof(ChainLegs)));
                    Add(typeof(ChainCoif), BaseVendor.VendorPays(typeof(ChainCoif)));
                }
            }
        }
    }
}