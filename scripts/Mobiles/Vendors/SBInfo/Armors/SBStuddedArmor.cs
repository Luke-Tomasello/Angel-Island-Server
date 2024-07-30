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
    public class SBStuddedArmor : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBStuddedArmor()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(StuddedArms), BaseVendor.PlayerPays(typeof(StuddedArms)), 20, 0x13DC, 0));
                Add(new GenericBuyInfo(typeof(StuddedChest), BaseVendor.PlayerPays(typeof(StuddedChest)), 20, 0x13DB, 0));
                Add(new GenericBuyInfo(typeof(StuddedGloves), BaseVendor.PlayerPays(typeof(StuddedGloves)), 20, 0x13D5, 0));
                Add(new GenericBuyInfo(typeof(StuddedGorget), BaseVendor.PlayerPays(typeof(StuddedGorget)), 20, 0x13D6, 0));
                Add(new GenericBuyInfo(typeof(StuddedLegs), BaseVendor.PlayerPays(typeof(StuddedLegs)), 20, 0x13DA, 0));
                Add(new GenericBuyInfo(typeof(FemaleStuddedChest), BaseVendor.PlayerPays(typeof(FemaleStuddedChest)), 20, 0x1C02, 0));
                Add(new GenericBuyInfo(typeof(StuddedBustierArms), BaseVendor.PlayerPays(typeof(StuddedBustierArms)), 20, 0x1c0c, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {
                    Add(typeof(StuddedArms), BaseVendor.VendorPays(typeof(StuddedArms)));
                    Add(typeof(StuddedChest), BaseVendor.VendorPays(typeof(StuddedChest)));
                    Add(typeof(StuddedGloves), BaseVendor.VendorPays(typeof(StuddedGloves)));
                    Add(typeof(StuddedGorget), BaseVendor.VendorPays(typeof(StuddedGorget)));
                    Add(typeof(StuddedLegs), BaseVendor.VendorPays(typeof(StuddedLegs)));
                    Add(typeof(FemaleStuddedChest), BaseVendor.VendorPays(typeof(FemaleStuddedChest)));
                    Add(typeof(StuddedBustierArms), BaseVendor.VendorPays(typeof(StuddedBustierArms)));
                }
            }
        }
    }
}