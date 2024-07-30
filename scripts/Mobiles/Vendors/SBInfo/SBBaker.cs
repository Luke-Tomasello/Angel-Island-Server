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

/* Scripts/Mobiles/Vendors/SBInfo/SBBaker.cs
 * ChangeLog
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
    public class SBBaker : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBBaker()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(BreadLoaf), BaseVendor.PlayerPays(typeof(BreadLoaf)), 10, 0x103B, 0));
                Add(new GenericBuyInfo(typeof(CheeseWheel), BaseVendor.PlayerPays(typeof(CheeseWheel)), 10, 0x97E, 0));
                Add(new GenericBuyInfo(typeof(FrenchBread), BaseVendor.PlayerPays(typeof(FrenchBread)), 20, 0x98C, 0));
                Add(new GenericBuyInfo(typeof(FriedEggs), BaseVendor.PlayerPays(typeof(FriedEggs)), 10, 0x9B6, 0));
                Add(new GenericBuyInfo(typeof(Cake), BaseVendor.PlayerPays(typeof(Cake)), 10, 0x9E9, 0));
                Add(new GenericBuyInfo(typeof(Cookies), BaseVendor.PlayerPays(typeof(Cookies)), 20, 0x160b, 0));
                Add(new GenericBuyInfo(typeof(Muffins), BaseVendor.PlayerPays(typeof(Muffins)), 10, 0x9eb, 0));
                Add(new GenericBuyInfo(typeof(CheesePizza), BaseVendor.PlayerPays(typeof(CheesePizza)), 10, 0x1040, 0));

                Add(new GenericBuyInfo(typeof(ApplePie), BaseVendor.PlayerPays(typeof(ApplePie)), 10, 0x1041, 0));

                Add(new GenericBuyInfo(typeof(PeachCobbler), BaseVendor.PlayerPays(typeof(PeachCobbler)), 10, 0x1041, 0));

                Add(new GenericBuyInfo(typeof(Quiche), BaseVendor.PlayerPays(typeof(Quiche)), 10, 0x1041, 0));
                Add(new GenericBuyInfo(typeof(Dough), BaseVendor.PlayerPays(typeof(Dough)), 20, 0x103d, 0));
                Add(new GenericBuyInfo(typeof(JarHoney), BaseVendor.PlayerPays(typeof(JarHoney)), 20, 0x9ec, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Water, BaseVendor.PlayerPays(11), 20, 0x1F9D, 0));
                Add(new GenericBuyInfo(typeof(SackFlour), BaseVendor.PlayerPays(typeof(SackFlour)), 20, 0x1039, 0));
                Add(new GenericBuyInfo(typeof(Eggs), BaseVendor.PlayerPays(typeof(Eggs)), 20, 0x9B5, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(BreadLoaf), BaseVendor.VendorPays(typeof(BreadLoaf)));
                    Add(typeof(CheeseWheel), BaseVendor.VendorPays(typeof(CheeseWheel)));
                    Add(typeof(FrenchBread), BaseVendor.VendorPays(typeof(FrenchBread)));
                    Add(typeof(FriedEggs), BaseVendor.VendorPays(typeof(FriedEggs)));
                    Add(typeof(Cake), BaseVendor.VendorPays(typeof(Cake)));
                    Add(typeof(Cookies), BaseVendor.VendorPays(typeof(Cookies)));
                    Add(typeof(Muffins), BaseVendor.VendorPays(typeof(Muffins)));
                    Add(typeof(CheesePizza), BaseVendor.VendorPays(typeof(CheesePizza)));
                    Add(typeof(ApplePie), BaseVendor.VendorPays(typeof(ApplePie)));
                    Add(typeof(PeachCobbler), BaseVendor.VendorPays(typeof(PeachCobbler)));
                    Add(typeof(Quiche), BaseVendor.VendorPays(typeof(Quiche)));
                    Add(typeof(Dough), BaseVendor.VendorPays(typeof(Dough)));
                    Add(typeof(JarHoney), BaseVendor.VendorPays(typeof(JarHoney)));
                    Add(typeof(Pitcher), BaseVendor.VendorPays(typeof(Pitcher)));
                    Add(typeof(SackFlour), BaseVendor.VendorPays(typeof(SackFlour)));
                    Add(typeof(Eggs), BaseVendor.VendorPays(typeof(Eggs)));
                }
            }
        }
    }
}