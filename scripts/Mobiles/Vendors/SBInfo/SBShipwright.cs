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

using Server.Multis;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class SBShipwright : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBShipwright()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo("1041205", typeof(SmallBoatDeed), BaseVendor.PlayerPays(typeof(SmallBoatDeed)), 20, 0x14F2, 0));
                Add(new GenericBuyInfo("1041206", typeof(SmallDragonBoatDeed), BaseVendor.PlayerPays(typeof(SmallDragonBoatDeed)), 20, 0x14F2, 0));
                Add(new GenericBuyInfo("1041207", typeof(MediumBoatDeed), BaseVendor.PlayerPays(typeof(MediumBoatDeed)), 20, 0x14F2, 0));
                Add(new GenericBuyInfo("1041208", typeof(MediumDragonBoatDeed), BaseVendor.PlayerPays(typeof(MediumDragonBoatDeed)), 20, 0x14F2, 0));
                Add(new GenericBuyInfo("1041209", typeof(LargeBoatDeed), BaseVendor.PlayerPays(typeof(LargeBoatDeed)), 20, 0x14F2, 0));
                Add(new GenericBuyInfo("1041210", typeof(LargeDragonBoatDeed), BaseVendor.PlayerPays(typeof(LargeDragonBoatDeed)), 20, 0x14F2, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    // Comment in RunUO 2.6: "You technically CAN sell them back, *BUT* the vendors do not carry enough money to buy with"
                    // Research results: I added a ship for buyback on RunUO 2.6, and it was bought back without vendor complaint.
                    //  Then in UOGuide, it is stated "Players may sell boats as either deeds or, much more commonly, a Ship Model, which appears as a miniature ship."
                    //  https://www.uoguide.com/Boats
                    //  So it would seem RunUO's comment above is a red herring.
                    {   // ship deeds
                        Add(typeof(SmallBoatDeed), ComputePriceFor(typeof(SmallBoatDeed)));
                        Add(typeof(SmallDragonBoatDeed), ComputePriceFor(typeof(SmallDragonBoatDeed)));
                        Add(typeof(MediumBoatDeed), ComputePriceFor(typeof(MediumBoatDeed)));
                        Add(typeof(MediumDragonBoatDeed), ComputePriceFor(typeof(MediumDragonBoatDeed)));
                        Add(typeof(LargeBoatDeed), ComputePriceFor(typeof(LargeBoatDeed)));
                        Add(typeof(LargeDragonBoatDeed), ComputePriceFor(typeof(LargeDragonBoatDeed)));
                    }
                    {   // ship models
                        Add(typeof(BaseDockedBoat), ComputePriceFor(typeof(SmallBoatDeed)));
                        Add(typeof(SmallDockedBoat), ComputePriceFor(typeof(SmallBoatDeed)));
                        Add(typeof(SmallDockedDragonBoat), ComputePriceFor(typeof(SmallDragonBoatDeed)));
                        Add(typeof(MediumDockedBoat), ComputePriceFor(typeof(MediumBoatDeed)));
                        Add(typeof(MediumDockedDragonBoat), ComputePriceFor(typeof(MediumDragonBoatDeed)));
                        Add(typeof(LargeDockedBoat), ComputePriceFor(typeof(LargeBoatDeed)));
                        Add(typeof(LargeDockedDragonBoat), ComputePriceFor(typeof(LargeDragonBoatDeed)));
                    }
                }
            }

            private int ComputePriceFor(Type type)
            {   // 11/12/22, Adam: not sure if vendors buy back at 100%, so we'll apply
                //  the same markdown as for houses
                int price = BaseVendor.PlayerPays(type);                // includes Siege markup (but currently not bought back on siege)
                price = AOS.Scale(price, 80);                           // refunds 80% of the purchase price
                return price;
            }
        }
    }
}