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
    public class SBAxeWeapon : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBAxeWeapon()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(BattleAxe), BaseVendor.PlayerPays(typeof(BattleAxe)), 20, 0xF47, 0));
                Add(new GenericBuyInfo(typeof(DoubleAxe), BaseVendor.PlayerPays(typeof(DoubleAxe)), 20, 0xF4B, 0));
                Add(new GenericBuyInfo(typeof(ExecutionersAxe), BaseVendor.PlayerPays(typeof(ExecutionersAxe)), 20, 0xF45, 0));
                Add(new GenericBuyInfo(typeof(LargeBattleAxe), BaseVendor.PlayerPays(typeof(LargeBattleAxe)), 20, 0x13FB, 0));
                Add(new GenericBuyInfo(typeof(Pickaxe), BaseVendor.PlayerPays(typeof(Pickaxe)), 20, 0xE86, 0));
                Add(new GenericBuyInfo(typeof(TwoHandedAxe), BaseVendor.PlayerPays(typeof(TwoHandedAxe)), 20, 0x1443, 0));
                Add(new GenericBuyInfo(typeof(WarAxe), BaseVendor.PlayerPays(typeof(WarAxe)), 20, 0x13B0, 0));
                Add(new GenericBuyInfo(typeof(Axe), BaseVendor.PlayerPays(typeof(Axe)), 20, 0xF49, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {
                    Add(typeof(BattleAxe), BaseVendor.VendorPays(typeof(BattleAxe)));
                    Add(typeof(DoubleAxe), BaseVendor.VendorPays(typeof(DoubleAxe)));
                    Add(typeof(ExecutionersAxe), BaseVendor.VendorPays(typeof(ExecutionersAxe)));
                    Add(typeof(LargeBattleAxe), BaseVendor.VendorPays(typeof(LargeBattleAxe)));
                    Add(typeof(Pickaxe), BaseVendor.VendorPays(typeof(Pickaxe)));
                    Add(typeof(TwoHandedAxe), BaseVendor.VendorPays(typeof(TwoHandedAxe)));
                    Add(typeof(WarAxe), BaseVendor.VendorPays(typeof(WarAxe)));
                    Add(typeof(Axe), BaseVendor.VendorPays(typeof(Axe)));
                }
            }
        }
    }
}