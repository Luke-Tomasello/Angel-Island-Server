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

/* Scripts/Mobiles/Vendors/SBInfo/SBCook.cs
 * ChangeLog
 *  4/3/23, Yoar:
 * - Added muffins, bowl food and apple pie.
 *  11/8/22, Adam: (Vendor System)
 *  Complete overhaul of the vendor system:
 * - DisplayCache:
 *   Display cache objects are now strongly typed and there is a separate list for each.
 *   I still dislike the fact it is a �Container�, but we can live with that.
 *   Display cache objects are serialized and are deleted on each server restart as the vendors rebuild the list.
 *   Display cache objects are marked as �int map storage� so Cron doesn�t delete them.
 * - ResourcePool:
 *   Properly exclude all ResourcePool interactions when resource pool is not being used. (Buy/Sell now works correctly without ResourcePool.)
 *   Normalize/automate all ResourcePool resources for purchase/sale within a vendor. If a vendor Sells X, he will Buy X. 
 *   At the top of each SB<merchant> there is a list of ResourcePool types. This list is uniformly looped over for creating all ResourcePool buy/sell entries.
 * - Standard Pricing Database
 *   No longer do we hardcode in what we believe the buy/sell price of X is. We now use a Standard Pricing Database for assigning these prices.
 *   I.e., BaseVendor.PlayerPays(typeof(Drums)), or BaseVendor.VendorPays(typeof(Drums))
 *   This database was derived from RunUO 2.6 first and added items not covered from Angel Island 5.
 *   The database was then filtered, checked, sorted and committed. 
 * - Make use of Rule Sets as opposed to hardcoding shard configurations everywhere.
 *   Exampes:
 *   if (Core.UOAI_SVR) => if (Core.RuleSets.AngelIslandRules())
 *   if (Server.Engines.ResourcePool.ResourcePool.Enabled) => if (Core.RuleSets.ResourcePoolRules())
 *   etc. In this way we centrally adjust who sell/buys what when. And using the SPD above, for what price.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBCook : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBCook()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(CheeseWheel), BaseVendor.PlayerPays(typeof(CheeseWheel)), 20, 0x97E, 0));
                Add(new GenericBuyInfo("1044567", typeof(Skillet), BaseVendor.PlayerPays(typeof(Skillet)), 20, 0x97F, 0));
                Add(new GenericBuyInfo(typeof(CookedBird), BaseVendor.PlayerPays(typeof(CookedBird)), 20, 0x9B7, 0));
                Add(new GenericBuyInfo(typeof(RoastPig), BaseVendor.PlayerPays(typeof(RoastPig)), 20, 0x9BB, 0));

                Add(new GenericBuyInfo(typeof(ApplePie), BaseVendor.PlayerPays(typeof(ApplePie)), 20, 0x1041, 0)); //OSI just has Pie, not Apple/Fruit/Meat
                Add(new GenericBuyInfo(typeof(Cake), BaseVendor.PlayerPays(typeof(Cake)), 20, 0x9E9, 0));
                Add(new GenericBuyInfo(typeof(Muffins), BaseVendor.PlayerPays(typeof(Muffins)), 20, 0x9EA, 0));

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

                Add(new GenericBuyInfo(typeof(JarHoney), BaseVendor.PlayerPays(typeof(JarHoney)), 20, 0x9EC, 0));
                Add(new GenericBuyInfo(typeof(SackFlour), BaseVendor.PlayerPays(typeof(SackFlour)), 20, 0x1039, 0));
                Add(new GenericBuyInfo(typeof(BreadLoaf), BaseVendor.PlayerPays(typeof(BreadLoaf)), 20, 0x103B, 0));
                Add(new GenericBuyInfo(typeof(FlourSifter), BaseVendor.PlayerPays(typeof(FlourSifter)), 20, 0x103E, 0));
                Add(new GenericBuyInfo(typeof(RollingPin), BaseVendor.PlayerPays(typeof(RollingPin)), 20, 0x1043, 0));
                Add(new GenericBuyInfo(typeof(ChickenLeg), BaseVendor.PlayerPays(typeof(ChickenLeg)), 20, 0x1608, 0));
                Add(new GenericBuyInfo(typeof(LambLeg), BaseVendor.PlayerPays(typeof(LambLeg)), 20, 0x1609, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(CheeseWheel), BaseVendor.VendorPays(typeof(CheeseWheel)));
                    Add(typeof(CookedBird), BaseVendor.VendorPays(typeof(CookedBird)));
                    Add(typeof(RoastPig), BaseVendor.VendorPays(typeof(RoastPig)));
                    Add(typeof(Cake), BaseVendor.VendorPays(typeof(Cake)));
                    Add(typeof(JarHoney), BaseVendor.VendorPays(typeof(JarHoney)));
                    Add(typeof(SackFlour), BaseVendor.VendorPays(typeof(SackFlour)));
                    Add(typeof(BreadLoaf), BaseVendor.VendorPays(typeof(BreadLoaf)));
                    Add(typeof(ChickenLeg), BaseVendor.VendorPays(typeof(ChickenLeg)));
                    Add(typeof(LambLeg), BaseVendor.VendorPays(typeof(LambLeg)));
                    Add(typeof(Skillet), BaseVendor.VendorPays(typeof(Skillet)));
                    Add(typeof(FlourSifter), BaseVendor.VendorPays(typeof(FlourSifter)));
                    Add(typeof(RollingPin), BaseVendor.VendorPays(typeof(RollingPin)));
                    Add(typeof(Muffins), BaseVendor.VendorPays(typeof(Muffins)));
                    Add(typeof(ApplePie), BaseVendor.VendorPays(typeof(ApplePie)));

                    Add(typeof(WoodenBowlOfCarrots), BaseVendor.VendorPays(typeof(WoodenBowlOfCarrots)));
                    Add(typeof(WoodenBowlOfCorn), BaseVendor.VendorPays(typeof(WoodenBowlOfCorn)));
                    Add(typeof(WoodenBowlOfLettuce), BaseVendor.VendorPays(typeof(WoodenBowlOfLettuce)));
                    Add(typeof(WoodenBowlOfPeas), BaseVendor.VendorPays(typeof(WoodenBowlOfPeas)));
                    Add(typeof(EmptyPewterBowl), BaseVendor.VendorPays(typeof(EmptyPewterBowl)));
                    Add(typeof(PewterBowlOfCorn), BaseVendor.VendorPays(typeof(PewterBowlOfCorn)));
                    Add(typeof(PewterBowlOfLettuce), BaseVendor.VendorPays(typeof(PewterBowlOfLettuce)));
                    Add(typeof(PewterBowlOfPeas), BaseVendor.VendorPays(typeof(PewterBowlOfPeas)));
                    Add(typeof(PewterBowlOfPotatos), BaseVendor.VendorPays(typeof(PewterBowlOfPotatos)));
                    Add(typeof(WoodenBowlOfStew), BaseVendor.VendorPays(typeof(WoodenBowlOfStew)));
                    Add(typeof(WoodenBowlOfTomatoSoup), BaseVendor.VendorPays(typeof(WoodenBowlOfTomatoSoup)));
                }
            }
        }
    }
}