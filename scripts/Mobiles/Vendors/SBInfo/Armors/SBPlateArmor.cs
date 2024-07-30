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
    public class SBPlateArmor : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBPlateArmor()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(PlateArms), BaseVendor.PlayerPays(typeof(PlateArms)), 20, 0x1410, 0));
                Add(new GenericBuyInfo(typeof(PlateChest), BaseVendor.PlayerPays(typeof(PlateChest)), 20, 0x1415, 0));
                Add(new GenericBuyInfo(typeof(PlateGloves), BaseVendor.PlayerPays(typeof(PlateGloves)), 20, 0x1414, 0));
                Add(new GenericBuyInfo(typeof(PlateGorget), BaseVendor.PlayerPays(typeof(PlateGorget)), 20, 0x1413, 0));
                Add(new GenericBuyInfo(typeof(PlateLegs), BaseVendor.PlayerPays(typeof(PlateLegs)), 20, 0x1411, 0));

                Add(new GenericBuyInfo(typeof(PlateHelm), BaseVendor.PlayerPays(typeof(PlateHelm)), 20, 0x1412, 0));
                Add(new GenericBuyInfo(typeof(FemalePlateChest), BaseVendor.PlayerPays(typeof(FemalePlateChest)), 20, 0x1C04, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {
                    Add(typeof(PlateArms), BaseVendor.VendorPays(typeof(PlateArms)));
                    Add(typeof(PlateChest), BaseVendor.VendorPays(typeof(PlateChest)));
                    Add(typeof(PlateGloves), BaseVendor.VendorPays(typeof(PlateGloves)));
                    Add(typeof(PlateGorget), BaseVendor.VendorPays(typeof(PlateGorget)));
                    Add(typeof(PlateLegs), BaseVendor.VendorPays(typeof(PlateLegs)));
                    Add(typeof(PlateHelm), BaseVendor.VendorPays(typeof(PlateHelm)));
                    Add(typeof(FemalePlateChest), BaseVendor.VendorPays(typeof(FemalePlateChest)));
                }
            }
        }
    }
}