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

/* Scripts/Mobiles/Vendors/SBInfo/SBStoneCrafter.cs
 * ChangeLog
 *  6/16/23, Yoar
 *      Added RockHammer for pre-Pub 14 shards, in case we want to enable masonry
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
 *  3/24/22, Yoar
 *      Added marble, sandstone stockpile deeds.
 *  3/23/22, Adam
 *      Add rock commodity deeds
 *	4/10/05, Kitaras	
 *		Added in stone graver item.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using Server.Township;
using System.Collections;

namespace Server.Mobiles
{
    public class SBStoneCrafter : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBStoneCrafter()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo("Making Valuables With Stonecrafting", typeof(MasonryBook), BaseVendor.PlayerPays(typeof(MasonryBook)), 10, 0xFBE, 0));
                Add(new GenericBuyInfo("Mining For Quality Stone", typeof(StoneMiningBook), BaseVendor.PlayerPays(typeof(StoneMiningBook)), 10, 0xFBE, 0));
                Add(new GenericBuyInfo("1044515", typeof(MalletAndChisel), BaseVendor.PlayerPays(typeof(MalletAndChisel)), 50, 0x12B3, 0));

                // 6/16/23, Yoar: In case we don't have context menus nor bulk orders, simply sell a rock hammer so that we can mine for granite
                if (PublishInfo.Publish < 14.0)
                    Add(new GenericBuyInfo("Rock Hammer", typeof(RockHammer), BaseVendor.PlayerPays(typeof(RockHammer)), 20, 0xFB5, 0x973));

                // 8/22/23, Yoar: Enabled for townships
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.TownshipRules())
                    Add(new GenericBuyInfo("Stone Graver", typeof(StoneGraver), BaseVendor.PlayerPays(typeof(StoneGraver)), 20, 4135, 0));

                Add(new GenericBuyInfo(typeof(StockpileDeed), 6000, 20, 0x14F0, 0, new object[] { TownshipStockpile.StockFlag.Marble, 1000 }));
                Add(new GenericBuyInfo(typeof(StockpileDeed), 6000, 20, 0x14F0, 0, new object[] { TownshipStockpile.StockFlag.Sandstone, 1000 }));

                Add(new GenericBuyInfo(typeof(BasaltDeed), BaseVendor.PlayerPays(typeof(BasaltDeed)), 20, 0x1363, 0));
                Add(new GenericBuyInfo(typeof(DaciteDeed), BaseVendor.PlayerPays(typeof(DaciteDeed)), 20, 0x1364, 0));
                Add(new GenericBuyInfo(typeof(DiabaseDeed), BaseVendor.PlayerPays(typeof(DiabaseDeed)), 20, 0x136B, 0));
                Add(new GenericBuyInfo(typeof(DioriteDeed), BaseVendor.PlayerPays(typeof(DioriteDeed)), 20, 0x1772, 0));
                Add(new GenericBuyInfo(typeof(GabbroDeed), BaseVendor.PlayerPays(typeof(GabbroDeed)), 20, 0x1773, 0));
                Add(new GenericBuyInfo(typeof(PegmatiteDeed), BaseVendor.PlayerPays(typeof(PegmatiteDeed)), 20, 0x1775, 0));
                Add(new GenericBuyInfo(typeof(PeridotiteDeed), BaseVendor.PlayerPays(typeof(PeridotiteDeed)), 20, 0x1777, 0));
                Add(new GenericBuyInfo(typeof(RhyoliteDeed), BaseVendor.PlayerPays(typeof(RhyoliteDeed)), 20, 0x1367, 0));
                Add(new GenericBuyInfo(typeof(GneissDeed), BaseVendor.PlayerPays(typeof(GneissDeed)), 20, 0x1774, 0));
                Add(new GenericBuyInfo(typeof(QuartziteDeed), BaseVendor.PlayerPays(typeof(QuartziteDeed)), 20, 0x177C, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(MasonryBook), BaseVendor.VendorPays(typeof(MasonryBook)));
                    Add(typeof(StoneMiningBook), BaseVendor.VendorPays(typeof(StoneMiningBook)));
                    Add(typeof(MalletAndChisel), BaseVendor.VendorPays(typeof(MalletAndChisel)));
                }
            }
        }
    }
}