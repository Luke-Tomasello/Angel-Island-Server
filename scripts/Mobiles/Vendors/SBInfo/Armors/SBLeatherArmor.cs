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
    public class SBLeatherArmor : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBLeatherArmor()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(LeatherArms), BaseVendor.PlayerPays(typeof(LeatherArms)), 20, 0x13CD, 0));
                Add(new GenericBuyInfo(typeof(LeatherChest), BaseVendor.PlayerPays(typeof(LeatherChest)), 20, 0x13CC, 0));
                Add(new GenericBuyInfo(typeof(LeatherGloves), BaseVendor.PlayerPays(typeof(LeatherGloves)), 20, 0x13C6, 0));
                Add(new GenericBuyInfo(typeof(LeatherGorget), BaseVendor.PlayerPays(typeof(LeatherGorget)), 20, 0x13C7, 0));
                Add(new GenericBuyInfo(typeof(LeatherLegs), BaseVendor.PlayerPays(typeof(LeatherLegs)), 20, 0x13cb, 0));
                Add(new GenericBuyInfo(typeof(LeatherCap), BaseVendor.PlayerPays(typeof(LeatherCap)), 20, 0x1DB9, 0));
                Add(new GenericBuyInfo(typeof(FemaleLeatherChest), BaseVendor.PlayerPays(typeof(FemaleLeatherChest)), 20, 0x1C06, 0));
                Add(new GenericBuyInfo(typeof(LeatherBustierArms), BaseVendor.PlayerPays(typeof(LeatherBustierArms)), 20, 0x1C0A, 0));
                Add(new GenericBuyInfo(typeof(LeatherShorts), BaseVendor.PlayerPays(typeof(LeatherShorts)), 20, 0x1C00, 0));
                Add(new GenericBuyInfo(typeof(LeatherSkirt), BaseVendor.PlayerPays(typeof(LeatherSkirt)), 20, 0x1C08, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {
                    Add(typeof(LeatherArms), BaseVendor.VendorPays(typeof(LeatherArms)));
                    Add(typeof(LeatherChest), BaseVendor.VendorPays(typeof(LeatherChest)));
                    Add(typeof(LeatherGloves), BaseVendor.VendorPays(typeof(LeatherGloves)));
                    Add(typeof(LeatherGorget), BaseVendor.VendorPays(typeof(LeatherGorget))); ;
                    Add(typeof(LeatherLegs), BaseVendor.VendorPays(typeof(LeatherLegs))); ;
                    Add(typeof(LeatherCap), BaseVendor.VendorPays(typeof(LeatherCap))); ;
                    Add(typeof(FemaleLeatherChest), BaseVendor.VendorPays(typeof(FemaleLeatherChest)));
                    Add(typeof(LeatherBustierArms), BaseVendor.VendorPays(typeof(LeatherBustierArms)));
                    Add(typeof(LeatherShorts), BaseVendor.VendorPays(typeof(LeatherShorts)));
                    Add(typeof(LeatherSkirt), BaseVendor.VendorPays(typeof(LeatherSkirt)));
                }
            }
        }
    }
}