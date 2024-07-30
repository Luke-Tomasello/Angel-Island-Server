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

/* Scripts/Mobiles/Vendors/SBInfo/SBHouseDeed.cs
 * CHANGELOG
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
 *   etc. In this way we centrally adjust who sell/buys what when. And using the SPD above, for what price.:
 *  9/17/21, Yoar
 *      Static housing revamp
 *  6/12/07, adam
 *      add check for TestCenter.Enabled == true before adding Static houses for sale.
 *      We don't want this on until we have checked in a valid StaticHousing*.xml
 *	6/11/07 - Pix
 *		Added our static house deeds!
 *	11/22/06 - Pix
 *		Added missing TwoStoryStonePlasterHouseDeed
 */

using Server.Multis.Deeds;
using Server.Multis.StaticHousing;
using System.Collections;

namespace Server.Mobiles
{
    public class SBHouseDeed : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBHouseDeed()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo("deed to a stone and plaster house", typeof(StonePlasterHouseDeed), StonePlasterHouseDeed.m_price, 20, 0x14F0, 0));
                Add(new GenericBuyInfo("deed to a field stone house", typeof(FieldStoneHouseDeed), FieldStoneHouseDeed.m_price, 20, 0x14F0, 0));
                Add(new GenericBuyInfo("deed to a wooden house", typeof(WoodHouseDeed), WoodHouseDeed.m_price, 20, 0x14F0, 0));
                Add(new GenericBuyInfo("deed to a wood and plaster house", typeof(WoodPlasterHouseDeed), WoodPlasterHouseDeed.m_price, 20, 0x14F0, 0));
                Add(new GenericBuyInfo("deed to a thatched roof cottage", typeof(ThatchedRoofCottageDeed), ThatchedRoofCottageDeed.m_price, 20, 0x14F0, 0));

                Add(new GenericBuyInfo("deed to a small brick house", typeof(SmallBrickHouseDeed), SmallBrickHouseDeed.m_price, 20, 0x14F0, 0));
                Add(new GenericBuyInfo("deed to a small stone workshop", typeof(StoneWorkshopDeed), StoneWorkshopDeed.m_price, 20, 0x14F0, 0));
                Add(new GenericBuyInfo("deed to a small marble workshop", typeof(MarbleWorkshopDeed), MarbleWorkshopDeed.m_price, 20, 0x14F0, 0));
                Add(new GenericBuyInfo("deed to a small stone tower", typeof(SmallTowerDeed), SmallTowerDeed.m_price, 20, 0x14F0, 0));

                Add(new GenericBuyInfo("deed to a sandstone house with patio", typeof(SandstonePatioDeed), SandstonePatioDeed.m_price, 20, 0x14F0, 0));
                Add(new GenericBuyInfo("deed to a large house with patio", typeof(LargePatioDeed), LargePatioDeed.m_price, 20, 0x14F0, 0));
                Add(new GenericBuyInfo("deed to a marble house with patio", typeof(LargeMarbleDeed), LargeMarbleDeed.m_price, 20, 0x14F0, 0));

                Add(new GenericBuyInfo("deed to a brick house", typeof(BrickHouseDeed), BrickHouseDeed.m_price, 20, 0x14F0, 0));

                Add(new GenericBuyInfo("deed to a two-story log cabin", typeof(LogCabinDeed), LogCabinDeed.m_price, 20, 0x14F0, 0));
                Add(new GenericBuyInfo("deed to a two-story wood and plaster house", typeof(TwoStoryWoodPlasterHouseDeed), TwoStoryWoodPlasterHouseDeed.m_price, 20, 0x14F0, 0));
                Add(new GenericBuyInfo("deed to a two-story stone and plaster house", typeof(TwoStoryStonePlasterHouseDeed), TwoStoryStonePlasterHouseDeed.m_price, 20, 0x14F0, 0));
                Add(new GenericBuyInfo("deed to a two-story villa", typeof(VillaDeed), VillaDeed.m_price, 20, 0x14F0, 0));

                Add(new GenericBuyInfo("deed to a tower", typeof(TowerDeed), TowerDeed.m_price, 20, 0x14F0, 0));
                Add(new GenericBuyInfo("deed to a small stone keep", typeof(KeepDeed), KeepDeed.m_price, 20, 0x14F0, 0));
                Add(new GenericBuyInfo("deed to a castle", typeof(CastleDeed), CastleDeed.m_price, 20, 0x14F0, 0));

                // m_map[1041217] = "deed to a blue tent";
                if (Core.RuleSets.SiegeRules())
                    Add(new GenericBuyInfo("a rolled up tent", typeof(TentBag), TentBag.m_price, 20, 0xA58, 0));

                if (Core.RuleSets.StaticHousingRules())
                {
                    foreach (StaticHouseHelper.StaticHouseDeedInfo info in StaticHouseHelper.HouseDeedInfo)
                    {
                        Add(new GenericBuyInfo("deed to a " + info.Description,
                                typeof(StaticDeed),
                                info.Price,
                                20,
                                0,
                                0,
                                0x14F0,
                                0,
                                new object[] { info.ID }
                            ));
                    }
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(StonePlasterHouseDeed), StonePlasterHouseDeed.m_price);
                    Add(typeof(FieldStoneHouseDeed), FieldStoneHouseDeed.m_price);
                    Add(typeof(SmallBrickHouseDeed), SmallBrickHouseDeed.m_price);
                    Add(typeof(WoodHouseDeed), WoodHouseDeed.m_price);
                    Add(typeof(WoodPlasterHouseDeed), WoodPlasterHouseDeed.m_price);
                    Add(typeof(ThatchedRoofCottageDeed), ThatchedRoofCottageDeed.m_price);
                    Add(typeof(BrickHouseDeed), BrickHouseDeed.m_price);
                    Add(typeof(TwoStoryWoodPlasterHouseDeed), TwoStoryWoodPlasterHouseDeed.m_price);
                    Add(typeof(TowerDeed), TowerDeed.m_price);
                    Add(typeof(KeepDeed), KeepDeed.m_price);
                    Add(typeof(CastleDeed), CastleDeed.m_price);
                    Add(typeof(LargePatioDeed), LargePatioDeed.m_price);
                    Add(typeof(LargeMarbleDeed), LargeMarbleDeed.m_price);
                    Add(typeof(SmallTowerDeed), SmallTowerDeed.m_price);
                    Add(typeof(LogCabinDeed), LogCabinDeed.m_price);
                    Add(typeof(SandstonePatioDeed), SandstonePatioDeed.m_price);
                    Add(typeof(VillaDeed), VillaDeed.m_price);
                    Add(typeof(StoneWorkshopDeed), StoneWorkshopDeed.m_price);
                    Add(typeof(MarbleWorkshopDeed), MarbleWorkshopDeed.m_price);
                    Add(typeof(SmallBrickHouseDeed), SmallBrickHouseDeed.m_price);
                }
            }
        }
    }
}