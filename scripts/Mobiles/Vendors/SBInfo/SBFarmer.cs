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

/* Scripts/Mobiles/Vendors/SBInfo/SBFarmer.cs
 * ChangeLog
 *  4/29/23, Adam
 *      Remove seedboxes and casks for Siege
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
 *  3/28/22, Adam (Casks)
 *      Add casks of water and liquor to the farmer
 *  3/12/22, Adam
 *      Add Holly and Boxwood plants.
 *      These are used in Township craft (building hedgerows)
 *      Remove Holly (for now)
 *  3/2/22, Adam (ThrowableRose)
 *      Add ThrowableRose to farmer inventory
 *  2/5/22, Adam
 *      Add "a rotten tomato" to farmer inventory
 *	5/12/05, Adam
 *		Add PlantBowl
 *		Add SeedBox
 *  3/30/05, Jade
 *      Restore carrots, pumpkins, squash, gouards, melons that were commented out
 *  3/26/05, Jade
 *      comment out carrots for easter event.
 *	10/30/04, Adam
 *		comment out squashes and gourds for halloween event: will turn back on after
 *	10/29/04, Adam
 *		comment out pumpkins for halloween event: will turn back on after
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBFarmer : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBFarmer()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if (Core.RuleSets.PlantSystem())
                {
                    Add(new BeverageBuyInfo(typeof(Cask), BeverageType.Water, BaseVendor.PlayerPays(20 * 11), 20, 0x1940, 0));   // a pitcher of water is 11gp and holds 5 measures of water, so 20*11
                    Add(new BeverageBuyInfo(typeof(Cask), BeverageType.Liquor, BaseVendor.PlayerPays(20 * 11), 20, 0x1940, 0));  // a pitcher of liquor is 11gp and holds 5 measures of liquor, so 20*11
                }
                Add(new GenericBuyInfo(typeof(Eggs), BaseVendor.PlayerPays(typeof(Eggs)), 20, 0x9B5, 0));
                Add(new GenericBuyInfo(typeof(Apple), BaseVendor.PlayerPays(typeof(Apple)), 20, 0x9D0, 0));
                Add(new GenericBuyInfo(typeof(Grapes), BaseVendor.PlayerPays(typeof(Grapes)), 20, 0x9D1, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Milk, BaseVendor.PlayerPays(7), 20, 0x9AD, 0));
                Add(new GenericBuyInfo(typeof(Watermelon), BaseVendor.PlayerPays(typeof(Watermelon)), 20, 0xC5C, 0));
                Add(new GenericBuyInfo(typeof(YellowGourd), BaseVendor.PlayerPays(typeof(YellowGourd)), 20, 0xC64, 0));
                Add(new GenericBuyInfo(typeof(GreenGourd), BaseVendor.PlayerPays(typeof(GreenGourd)), 20, 0xC66, 0));
                Add(new GenericBuyInfo(typeof(Pumpkin), BaseVendor.PlayerPays(typeof(Pumpkin)), 20, 0xC6A, 0));
                Add(new GenericBuyInfo(typeof(Onion), BaseVendor.PlayerPays(typeof(Onion)), 20, 0xC6D, 0));
                Add(new GenericBuyInfo(typeof(Lettuce), BaseVendor.PlayerPays(typeof(Lettuce)), 20, 0xC70, 0));
                Add(new GenericBuyInfo(typeof(Squash), BaseVendor.PlayerPays(typeof(Squash)), 20, 0xC72, 0));
                Add(new GenericBuyInfo(typeof(HoneydewMelon), BaseVendor.PlayerPays(typeof(HoneydewMelon)), 20, 0xC74, 0));
                Add(new GenericBuyInfo(typeof(Carrot), BaseVendor.PlayerPays(typeof(Carrot)), 20, 0xC78, 0));
                Add(new GenericBuyInfo(typeof(Cantaloupe), BaseVendor.PlayerPays(typeof(Cantaloupe)), 20, 0xC79, 0));
                Add(new GenericBuyInfo(typeof(Cabbage), BaseVendor.PlayerPays(typeof(Cabbage)), 20, 0xC7B, 0));
                //Add( new GenericBuyInfo( typeof( EarOfCorn ), 3, 20, XXXXXX, 0 ) );
                //Add( new GenericBuyInfo( typeof( Turnip ), 6, 20, XXXXXX, 0 ) );
                //Add( new GenericBuyInfo( typeof( SheafOfHay ), 2, 20, XXXXXX, 0 ) );
                Add(new GenericBuyInfo(typeof(Lemon), BaseVendor.PlayerPays(typeof(Lemon)), 20, 0x1728, 0));
                Add(new GenericBuyInfo(typeof(Lime), BaseVendor.PlayerPays(typeof(Lime)), 20, 0x172A, 0));
                Add(new GenericBuyInfo(typeof(Peach), BaseVendor.PlayerPays(typeof(Peach)), 20, 0x9D2, 0));
                Add(new GenericBuyInfo(typeof(Pear), BaseVendor.PlayerPays(typeof(Pear)), 20, 0x994, 0));
                if (Core.RuleSets.AllServerRules())
                {
                    Add(new GenericBuyInfo("a rotten tomato", typeof(RottenTomato), BaseVendor.PlayerPays(typeof(RottenTomato)), 20, 0x9D0, 0));
                    Add(new GenericBuyInfo("a rose", typeof(ThrowableRose), BaseVendor.PlayerPays(typeof(ThrowableRose)), 20, 0x18EA, 0));
                }
                if (Core.RuleSets.PlantSystem())
                    Add(new GenericBuyInfo("1060834", typeof(Engines.Plants.PlantBowl), BaseVendor.PlayerPays(2), 20, 0x15FD, 0));

                // 8/21/23, Yoar: Do these work? Either way, we don't need them since we can grow hedges using the plant system
#if false
                if (Core.RuleSets.TownshipRules())
                {
                    Add(new GenericBuyInfo(typeof(Server.Township.Boxwood), BaseVendor.PlayerPays(700), 20, Server.Township.Boxwood.YoungBoxwood, 0));
                    //Add(new GenericBuyInfo(typeof(Server.Township.Holly), BaseVendor.PlayerPays(700), 20, Server.Township.Holly.YoungHolly, 0));
                }
#endif

                if (Core.RuleSets.AllServerRules())
                {
                    Add(new GenericBuyInfo(typeof(SeedBox), BaseVendor.PlayerPays(typeof(SeedBox)), 20, 0x9A9, 0x1CE));
                    Add(new GenericBuyInfo(typeof(TreeSeedBox), BaseVendor.PlayerPays(typeof(TreeSeedBox)), 20, 0x9A9, 0x1CE));
                    Add(new GenericBuyInfo(typeof(TreeTrimmer), BaseVendor.PlayerPays(typeof(TreeTrimmer)), 20, 0xDFC, 0x47));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(Pitcher), BaseVendor.VendorPays(typeof(Pitcher)));
                    Add(typeof(Eggs), BaseVendor.VendorPays(typeof(Eggs)));
                    Add(typeof(Apple), BaseVendor.VendorPays(typeof(Apple)));
                    Add(typeof(Grapes), BaseVendor.VendorPays(typeof(Grapes)));
                    Add(typeof(Watermelon), BaseVendor.VendorPays(typeof(Watermelon)));
                    Add(typeof(YellowGourd), BaseVendor.VendorPays(typeof(YellowGourd)));
                    Add(typeof(GreenGourd), BaseVendor.VendorPays(typeof(GreenGourd)));
                    Add(typeof(Pumpkin), BaseVendor.VendorPays(typeof(Pumpkin)));
                    Add(typeof(Onion), BaseVendor.VendorPays(typeof(Onion)));
                    Add(typeof(Lettuce), BaseVendor.VendorPays(typeof(Lettuce)));
                    Add(typeof(Squash), BaseVendor.VendorPays(typeof(Squash)));
                    Add(typeof(Carrot), BaseVendor.VendorPays(typeof(Carrot)));
                    Add(typeof(HoneydewMelon), BaseVendor.VendorPays(typeof(HoneydewMelon)));
                    Add(typeof(Cantaloupe), BaseVendor.VendorPays(typeof(Cantaloupe)));
                    Add(typeof(Cabbage), BaseVendor.VendorPays(typeof(Cabbage)));
                    Add(typeof(Lemon), BaseVendor.VendorPays(typeof(Lemon)));
                    Add(typeof(Lime), BaseVendor.VendorPays(typeof(Lime)));
                    Add(typeof(Peach), BaseVendor.VendorPays(typeof(Peach)));
                    Add(typeof(Pear), BaseVendor.VendorPays(typeof(Pear)));
                }
            }
        }
    }
}