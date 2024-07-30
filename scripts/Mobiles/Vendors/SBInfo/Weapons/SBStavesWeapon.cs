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
    public class SBStavesWeapon : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBStavesWeapon()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(BlackStaff), BaseVendor.PlayerPays(typeof(BlackStaff)), 20, 0xDF1, 0));
                Add(new GenericBuyInfo(typeof(GnarledStaff), BaseVendor.PlayerPays(typeof(GnarledStaff)), 20, 0x13F8, 0));
                Add(new GenericBuyInfo(typeof(QuarterStaff), BaseVendor.PlayerPays(typeof(QuarterStaff)), 20, 0xE89, 0));
                Add(new GenericBuyInfo(typeof(ShepherdsCrook), BaseVendor.PlayerPays(typeof(ShepherdsCrook)), 20, 0xE81, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {
                    Add(typeof(BlackStaff), BaseVendor.VendorPays(typeof(BlackStaff)));
                    Add(typeof(GnarledStaff), BaseVendor.VendorPays(typeof(GnarledStaff)));
                    Add(typeof(QuarterStaff), BaseVendor.VendorPays(typeof(QuarterStaff)));
                    Add(typeof(ShepherdsCrook), BaseVendor.VendorPays(typeof(ShepherdsCrook)));
                }
            }
        }
    }
}