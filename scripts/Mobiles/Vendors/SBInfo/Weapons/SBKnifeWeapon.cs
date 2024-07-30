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
    public class SBKnifeWeapon : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBKnifeWeapon()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(ButcherKnife), BaseVendor.PlayerPays(typeof(ButcherKnife)), 20, 0x13F6, 0));
                Add(new GenericBuyInfo(typeof(Cleaver), BaseVendor.PlayerPays(typeof(Cleaver)), 20, 0xEC3, 0));
                Add(new GenericBuyInfo(typeof(Dagger), BaseVendor.PlayerPays(typeof(Dagger)), 20, 0xF52, 0));
                Add(new GenericBuyInfo(typeof(SkinningKnife), BaseVendor.PlayerPays(typeof(SkinningKnife)), 20, 0xEC4, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {
                    Add(typeof(ButcherKnife), BaseVendor.VendorPays(typeof(ButcherKnife)));
                    Add(typeof(Cleaver), BaseVendor.VendorPays(typeof(Cleaver)));
                    Add(typeof(Dagger), BaseVendor.VendorPays(typeof(Dagger)));
                    Add(typeof(SkinningKnife), BaseVendor.VendorPays(typeof(SkinningKnife)));
                }
            }
        }
    }
}