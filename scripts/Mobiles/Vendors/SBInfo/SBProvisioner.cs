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

/* Scripts/Mobiles/Vendors/SBInfo/SBProvisioner.cs
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
 *	4/19/07, Pix
 *		Township deed price now on a dial.
 *	5/18/05, erlein
 *		Added Tourney Stone Deed to list!
 *	5/12/05, Adam
 *		Minor cleanup
 *  01/23/05, Taran Kain
 *		Added arrows, bolts.
 *  11/14/04,Froste
 *      Moved Teleporterdeed and NameChangeDeed to SBGypsyTrader.cs
 *	10/12/04, Darva
 *		Added Flag Stone Deed to list.
 *		Moved Leg's change comment to proper place. :P
 *	9/16/04, Lego Eater
 *		added bedroll to buylist
 *	8/27/04, Adam
 *		Add the dummy name change deed to list
 *		Add the dummy teleporter deed to the list
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */
using Server.Engines.Plants;
using Server.Engines.ResourcePool;
using Server.Items;
using Server.Multis;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class SBProvisioner : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();
        private static List<Type> BBSTypes = new List<Type>() { typeof(Arrow), typeof(Bolt) };

        public SBProvisioner()
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
                    Add(new GenericBuyInfo(typeof(Arrow)));
                    Add(new GenericBuyInfo(typeof(Bolt)));
#endif
                }
                else if (Core.RuleSets.ShopkeepersSellResourcesRules())
                {
                    Add(new GenericBuyInfo(typeof(Arrow), BaseVendor.PlayerPays(typeof(Arrow)), 20, 0xF3F, 0));
                    Add(new GenericBuyInfo(typeof(Bolt), BaseVendor.PlayerPays(typeof(Bolt)), 20, 0x1BFB, 0));
                }

                Add(new GenericBuyInfo(typeof(Backpack), BaseVendor.PlayerPays(typeof(Backpack)), 20, 0x9B2, 0));
                Add(new GenericBuyInfo(typeof(Pouch), BaseVendor.PlayerPays(typeof(Pouch)), 20, 0xE79, 0));
                Add(new GenericBuyInfo(typeof(Bag), BaseVendor.PlayerPays(typeof(Bag)), 20, 0xE76, 0));
                Add(new GenericBuyInfo(typeof(Candle), BaseVendor.PlayerPays(typeof(Candle)), 20, 0xA28, 0));
                Add(new GenericBuyInfo(typeof(Torch), BaseVendor.PlayerPays(typeof(Torch)), 20, 0xF6B, 0));
                Add(new GenericBuyInfo(typeof(Lantern), BaseVendor.PlayerPays(typeof(Lantern)), 20, 0xA25, 0));
                Add(new GenericBuyInfo(typeof(Lockpick), BaseVendor.PlayerPays(typeof(Lockpick)), 20, 0x14FC, 0));
                // TODO: Array of hats, randomly colored
                Add(new GenericBuyInfo(typeof(BreadLoaf), BaseVendor.PlayerPays(typeof(BreadLoaf)), 20, 0x103B, 0));
                Add(new GenericBuyInfo(typeof(LambLeg), BaseVendor.PlayerPays(typeof(LambLeg)), 20, 0x160A, 0));
                Add(new GenericBuyInfo(typeof(ChickenLeg), BaseVendor.PlayerPays(typeof(ChickenLeg)), 20, 0x1608, 0));
                Add(new GenericBuyInfo(typeof(CookedBird), BaseVendor.PlayerPays(typeof(CookedBird)), 20, 0x9B7, 0));
                Add(new BeverageBuyInfo(typeof(BeverageBottle), BeverageType.Ale, BaseVendor.PlayerPays(7), 20, 0x99F, 0));
                Add(new BeverageBuyInfo(typeof(BeverageBottle), BeverageType.Wine, BaseVendor.PlayerPays(7), 20, 0x9C7, 0));
                Add(new BeverageBuyInfo(typeof(BeverageBottle), BeverageType.Liquor, BaseVendor.PlayerPays(7), 20, 0x99B, 0));
                Add(new BeverageBuyInfo(typeof(Jug), BeverageType.Cider, BaseVendor.PlayerPays(13), 20, 0x9C8, 0));
                Add(new GenericBuyInfo(typeof(Pear), BaseVendor.PlayerPays(typeof(Pear)), 20, 0x994, 0));
                Add(new GenericBuyInfo(typeof(Apple), BaseVendor.PlayerPays(typeof(Apple)), 20, 0x9D0, 0));
                Add(new GenericBuyInfo(typeof(Garlic), BaseVendor.PlayerPays(typeof(Garlic)), 20, 0xF84, 0));
                Add(new GenericBuyInfo(typeof(Ginseng), BaseVendor.PlayerPays(typeof(Ginseng)), 20, 0xF85, 0));
                Add(new GenericBuyInfo(typeof(Beeswax), BaseVendor.PlayerPays(typeof(Beeswax)), 20, 0x1422, 0));
                Add(new GenericBuyInfo(typeof(Bottle), BaseVendor.PlayerPays(typeof(Bottle)), 20, 0xF0E, 0));
                Add(new GenericBuyInfo(typeof(RedBook), BaseVendor.PlayerPays(typeof(RedBook)), 20, 0xFF1, 0));
                Add(new GenericBuyInfo(typeof(BlueBook), BaseVendor.PlayerPays(typeof(BlueBook)), 20, 0xFF2, 0));
                Add(new GenericBuyInfo(typeof(TanBook), BaseVendor.PlayerPays(typeof(TanBook)), 20, 0xFF0, 0));
                Add(new GenericBuyInfo(typeof(WoodenBox), BaseVendor.PlayerPays(typeof(WoodenBox)), 20, 0xE7D, 0));
                Add(new GenericBuyInfo(typeof(Bedroll), BaseVendor.PlayerPays(typeof(Bedroll)), 20, 0xA57, 0));// added bedroll for 160gp
                                                                                                               // TODO: Copper key, bedroll
                Add(new GenericBuyInfo(typeof(Kindling), BaseVendor.PlayerPays(typeof(Kindling)), 20, 0xDE1, 0));
                Add(new GenericBuyInfo("1041205", typeof(SmallBoatDeed), BaseVendor.PlayerPays(typeof(SmallBoatDeed)), 20, 0x14F2, 0));
                Add(new GenericBuyInfo("1041055", typeof(GuildDeed), BaseVendor.PlayerPays(typeof(GuildDeed)), 20, 0x14F0, 0));

                if (Core.RuleSets.TownshipRules())
                    Add(new GenericBuyInfo("Township Deed", typeof(TownshipDeed), Township.TownshipSettings.TSDeedCost, 20, 0x14F0, Township.TownshipSettings.Hue));

                Add(new GenericBuyInfo("1041060", typeof(HairDye), BaseVendor.PlayerPays(typeof(HairDye)), 20, 0xEFF, 0));
                Add(new GenericBuyInfo("1016450", typeof(Chessboard), BaseVendor.PlayerPays(typeof(Chessboard)), 20, 0xFA6, 0));
                Add(new GenericBuyInfo("1016449", typeof(CheckerBoard), BaseVendor.PlayerPays(typeof(CheckerBoard)), 20, 0xFA6, 0));
                Add(new GenericBuyInfo(typeof(Backgammon), BaseVendor.PlayerPays(typeof(Backgammon)), 20, 0xE1C, 0));
                Add(new GenericBuyInfo(typeof(Dices), BaseVendor.PlayerPays(typeof(Dices)), 20, 0xFA7, 0));

                if (Core.RuleSets.AngelIslandRules())
                {
                    Add(new GenericBuyInfo("bounty ledger", typeof(BountyLedger), BaseVendor.PlayerPays(typeof(BountyLedger)), 20, 0xEFA, 0x3C1));
                    Add(new GenericBuyInfo("Flag Stone Deed", typeof(FlagStoneAddonDeed), BaseVendor.PlayerPays(typeof(FlagStoneAddonDeed)), 20, 0x14f0, 0x0));
                    Add(new GenericBuyInfo("Tourney Stone Deed", typeof(TourneyStoneAddonDeed), BaseVendor.PlayerPays(typeof(TourneyStoneAddonDeed)), 20, 0x14f0, 0x0));
                }

                if (Engines.Plants.PlantSystem.Enabled)
                    Add(new GenericBuyInfo("1060834", typeof(Engines.Plants.PlantBowl), BaseVendor.PlayerPays(typeof(PlantBowl)), 20, 0x15FD, 0));

                // Unsure of publish.
                // Bagball first appears in UOGuide 18 November 2008:
                // Publish 56 - October 29, 2008
                //  https://www.uoguide.com/index.php?search=bagball&title=Special%3ASearch&go=Go
                // I think they were substantially before this date. We'll AOS
                if (Core.RuleSets.AOSRules())
                {
                    Add(new GenericBuyInfo(typeof(Engines.Mahjong.MahjongGame), 6, 20, 0xFAA, 0));
                    Add(new GenericBuyInfo(typeof(SmallBagBall), BaseVendor.PlayerPays(typeof(SmallBagBall)), 20, 0x2256, 0));
                    Add(new GenericBuyInfo(typeof(LargeBagBall), BaseVendor.PlayerPays(typeof(LargeBagBall)), 20, 0x2257, 0));
                }
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
                    AddToResourcePool(typeof(Arrow));
                    AddToResourcePool(typeof(Bolt));
#endif
                }
                else if (Core.RuleSets.ShopkeepersBuyResourcesRules())
                {   // cash buyback
                    Add(typeof(Arrow), BaseVendor.VendorPays(typeof(Arrow)));
                    Add(typeof(Bolt), BaseVendor.VendorPays(typeof(Bolt)));
                }

                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(Backpack), BaseVendor.VendorPays(typeof(Backpack)));
                    Add(typeof(Pouch), BaseVendor.VendorPays(typeof(Pouch)));
                    Add(typeof(Bag), BaseVendor.VendorPays(typeof(Bag)));
                    Add(typeof(Candle), BaseVendor.VendorPays(typeof(Candle)));
                    Add(typeof(Torch), BaseVendor.VendorPays(typeof(Torch)));
                    Add(typeof(Lantern), BaseVendor.VendorPays(typeof(Lantern)));
                    Add(typeof(Lockpick), BaseVendor.VendorPays(typeof(Lockpick)));
                    Add(typeof(Bottle), BaseVendor.VendorPays(typeof(Bottle)));
                    Add(typeof(RedBook), BaseVendor.VendorPays(typeof(RedBook)));
                    Add(typeof(BlueBook), BaseVendor.VendorPays(typeof(BlueBook)));
                    Add(typeof(TanBook), BaseVendor.VendorPays(typeof(TanBook)));
                    Add(typeof(WoodenBox), BaseVendor.VendorPays(typeof(WoodenBox)));
                    Add(typeof(Kindling), BaseVendor.VendorPays(typeof(Kindling)));
                    Add(typeof(HairDye), BaseVendor.VendorPays(typeof(HairDye)));
                    Add(typeof(Chessboard), BaseVendor.VendorPays(typeof(Chessboard)));
                    Add(typeof(CheckerBoard), BaseVendor.VendorPays(typeof(CheckerBoard)));
                    Add(typeof(Backgammon), BaseVendor.VendorPays(typeof(Backgammon)));
                    Add(typeof(Dices), BaseVendor.VendorPays(typeof(Dices)));
                    if (false)
                        // we don't buy these back
                        Add(typeof(GuildDeed), BaseVendor.VendorPays(typeof(GuildDeed)));
                    Add(typeof(Beeswax), BaseVendor.VendorPays(typeof(Beeswax)));
                }
            }
        }
    }
}