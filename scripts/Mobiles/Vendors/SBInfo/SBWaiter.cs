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

/* Scripts/Mobiles/Vendors/SBInfo/SBTavernKeeper.cs
 * ChangeLog
 *  4/3/23, Yoar:
 *  Added bowl food and apple pie.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBWaiter : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBWaiter()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new BeverageBuyInfo(typeof(BeverageBottle), BeverageType.Ale, BaseVendor.PlayerPays(7), 20, 0x99F, 0));
                Add(new BeverageBuyInfo(typeof(BeverageBottle), BeverageType.Wine, BaseVendor.PlayerPays(7), 20, 0x9C7, 0));
                Add(new BeverageBuyInfo(typeof(BeverageBottle), BeverageType.Liquor, BaseVendor.PlayerPays(7), 20, 0x99B, 0));
                Add(new BeverageBuyInfo(typeof(Jug), BeverageType.Cider, BaseVendor.PlayerPays(13), 20, 0x9C8, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Milk, BaseVendor.PlayerPays(7), 20, 0x9F0, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Ale, BaseVendor.PlayerPays(11), 20, 0x1F95, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Cider, BaseVendor.PlayerPays(11), 20, 0x1F97, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Liquor, BaseVendor.PlayerPays(11), 20, 0x1F99, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Wine, BaseVendor.PlayerPays(11), 20, 0x1F9B, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Water, BaseVendor.PlayerPays(11), 20, 0x1F9D, 0));
                Add(new GenericBuyInfo(typeof(Pitcher), BaseVendor.PlayerPays(typeof(Pitcher)), 20, 0xFF6, 0));
                Add(new GenericBuyInfo(typeof(BreadLoaf), BaseVendor.PlayerPays(typeof(BreadLoaf)), 10, 0x103B, 0));
                Add(new GenericBuyInfo(typeof(CheeseWheel), BaseVendor.PlayerPays(typeof(CheeseWheel)), 10, 0x97E, 0));
                Add(new GenericBuyInfo(typeof(CookedBird), BaseVendor.PlayerPays(typeof(CookedBird)), 20, 0x9B7, 0));
                Add(new GenericBuyInfo(typeof(LambLeg), BaseVendor.PlayerPays(typeof(LambLeg)), 20, 0x160A, 0));

                Add(new GenericBuyInfo(typeof(WoodenBowlOfCarrots), BaseVendor.PlayerPays(typeof(WoodenBowlOfCarrots)), 20, 0x15F9, 0));
                Add(new GenericBuyInfo(typeof(WoodenBowlOfCorn), BaseVendor.PlayerPays(typeof(WoodenBowlOfCorn)), 20, 0x15FA, 0));
                Add(new GenericBuyInfo(typeof(WoodenBowlOfLettuce), BaseVendor.PlayerPays(typeof(WoodenBowlOfLettuce)), 20, 0x15FB, 0));
                Add(new GenericBuyInfo(typeof(WoodenBowlOfPeas), BaseVendor.PlayerPays(typeof(WoodenBowlOfPeas)), 20, 0x15FC, 0));
                Add(new GenericBuyInfo(typeof(EmptyPewterBowl), BaseVendor.PlayerPays(typeof(EmptyPewterBowl)), 20, 0x15FD, 0));
                Add(new GenericBuyInfo(typeof(PewterBowlOfCorn), BaseVendor.PlayerPays(typeof(PewterBowlOfCorn)), 20, 0x15FE, 0));
                Add(new GenericBuyInfo(typeof(PewterBowlOfLettuce), BaseVendor.PlayerPays(typeof(PewterBowlOfLettuce)), 20, 0x15FF, 0));
                Add(new GenericBuyInfo(typeof(PewterBowlOfPeas), BaseVendor.PlayerPays(typeof(PewterBowlOfPeas)), 20, 0x1600, 0));
                Add(new GenericBuyInfo(typeof(PewterBowlOfPotatos), BaseVendor.PlayerPays(typeof(PewterBowlOfPotatos)), 20, 0x1601, 0));
                Add(new GenericBuyInfo(typeof(WoodenBowlOfStew), BaseVendor.PlayerPays(typeof(WoodenBowlOfStew)), 20, 0x1604, 0));
                Add(new GenericBuyInfo(typeof(WoodenBowlOfTomatoSoup), BaseVendor.PlayerPays(typeof(WoodenBowlOfTomatoSoup)), 20, 0x1606, 0));

                Add(new GenericBuyInfo(typeof(ApplePie), BaseVendor.PlayerPays(typeof(ApplePie)), 20, 0x1041, 0)); //OSI just has Pie, not Apple/Fruit/Meat
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
            }
        }
    }
}