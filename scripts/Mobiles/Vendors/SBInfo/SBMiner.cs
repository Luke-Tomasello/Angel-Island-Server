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

using Server.Engines.ResourcePool;
using Server.Items;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class SBMiner : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();
        private static List<Type> BBSTypes = new List<Type>() { typeof(IronIngot), typeof(DullCopperIngot), typeof(ShadowIronIngot), typeof(CopperIngot), typeof(BronzeIngot), typeof(GoldIngot), typeof(AgapiteIngot), typeof(VeriteIngot), typeof(ValoriteIngot), typeof(IronOre), typeof(DullCopperOre), typeof(ShadowIronOre), typeof(CopperOre), typeof(BronzeOre), typeof(GoldOre), typeof(AgapiteOre), typeof(VeriteOre), typeof(ValoriteOre) };

        public SBMiner()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if (Core.RuleSets.ResourcePoolRules())
                {   // balanced buyback system
                    foreach (Type type in BBSTypes)
                        if (ResourcePool.IsPooledResource(type))
                            Add(new GenericBuyInfo(type));
#if false
                    Add(new GenericBuyInfo(typeof(IronIngot)));
                    Add(new GenericBuyInfo(typeof(DullCopperIngot)));
                    Add(new GenericBuyInfo(typeof(ShadowIronIngot)));
                    Add(new GenericBuyInfo(typeof(CopperIngot)));
                    Add(new GenericBuyInfo(typeof(BronzeIngot)));
                    Add(new GenericBuyInfo(typeof(GoldIngot)));
                    Add(new GenericBuyInfo(typeof(AgapiteIngot)));
                    Add(new GenericBuyInfo(typeof(VeriteIngot)));
                    Add(new GenericBuyInfo(typeof(ValoriteIngot)));
#endif
                }

                Add(new GenericBuyInfo(typeof(Bag), BaseVendor.PlayerPays(typeof(Bag)), 20, 0xE76, 0));
                Add(new GenericBuyInfo(typeof(Candle), BaseVendor.PlayerPays(typeof(Candle)), 10, 0xA28, 0));
                Add(new GenericBuyInfo(typeof(Torch), BaseVendor.PlayerPays(typeof(Torch)), 10, 0xF6B, 0));
                Add(new GenericBuyInfo(typeof(Lantern), BaseVendor.PlayerPays(typeof(Lantern)), 10, 0xA25, 0));
                //Add( new GenericBuyInfo( typeof( OilFlask ), 8, 10, 0x####, 0 ) );
                Add(new GenericBuyInfo(typeof(Pickaxe), BaseVendor.PlayerPays(typeof(Pickaxe)), 10, 0xE86, 0));
                Add(new GenericBuyInfo(typeof(Shovel), BaseVendor.PlayerPays(typeof(Shovel)), 10, 0xF39, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ResourcePoolRules())
                {   // balanced buyback system
                    foreach (Type type in BBSTypes)
                        AddToResourcePool(type);
#if false
                    AddToResourcePool(typeof(IronIngot));
                    AddToResourcePool(typeof(DullCopperIngot));
                    AddToResourcePool(typeof(ShadowIronIngot));
                    AddToResourcePool(typeof(CopperIngot));
                    AddToResourcePool(typeof(BronzeIngot));
                    AddToResourcePool(typeof(GoldIngot));
                    AddToResourcePool(typeof(AgapiteIngot));
                    AddToResourcePool(typeof(VeriteIngot));
                    AddToResourcePool(typeof(ValoriteIngot));
                    AddToResourcePool(typeof(IronOre));
                    AddToResourcePool(typeof(DullCopperOre));
                    AddToResourcePool(typeof(ShadowIronOre));
                    AddToResourcePool(typeof(CopperOre));
                    AddToResourcePool(typeof(BronzeOre));
                    AddToResourcePool(typeof(GoldOre));
                    AddToResourcePool(typeof(AgapiteOre));
                    AddToResourcePool(typeof(VeriteOre));
                    AddToResourcePool(typeof(ValoriteOre));
#endif
                }
                else if (Core.RuleSets.ShopkeepersBuyResourcesRules())
                {   // cash buyback
                    Add(typeof(IronIngot), BaseVendor.VendorPays(typeof(IronIngot)));
                }

                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(Pickaxe), BaseVendor.VendorPays(typeof(Pickaxe)));
                    Add(typeof(Shovel), BaseVendor.VendorPays(typeof(Shovel)));
                    Add(typeof(Lantern), BaseVendor.VendorPays(typeof(Lantern)));
                    //Add( typeof( OilFlask ), 4 );
                    Add(typeof(Torch), BaseVendor.VendorPays(typeof(Torch)));
                    Add(typeof(Bag), BaseVendor.VendorPays(typeof(Bag)));
                    Add(typeof(Candle), BaseVendor.VendorPays(typeof(Candle)));
                }
            }
        }
    }
}