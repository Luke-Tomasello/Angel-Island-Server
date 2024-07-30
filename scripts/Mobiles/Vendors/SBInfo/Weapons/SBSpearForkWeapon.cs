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
    public class SBSpearForkWeapon : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBSpearForkWeapon()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Spear), BaseVendor.PlayerPays(typeof(Spear)), 20, 0xF62, 0));
                Add(new GenericBuyInfo(typeof(Pitchfork), BaseVendor.PlayerPays(typeof(Pitchfork)), 20, 0xE87, 0));
                Add(new GenericBuyInfo(typeof(ShortSpear), BaseVendor.PlayerPays(typeof(ShortSpear)), 20, 0x1403, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {
                    Add(typeof(Spear), BaseVendor.VendorPays(typeof(Spear)));
                    Add(typeof(Pitchfork), BaseVendor.VendorPays(typeof(Pitchfork)));
                    Add(typeof(ShortSpear), BaseVendor.VendorPays(typeof(ShortSpear)));
                }
            }
        }
    }
}