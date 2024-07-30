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

/* Scripts/Mobiles/Vendors/SBInfo/SBGeneralContractor.cs
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
 *	11/5/21, Yoar
 *      Increased the price of LockboxBuildingPermit from 15k to 25k.
 *	10/31/21, Yoar
 *	    Lockbox system cleanup. Added two switches:
 *      1. BaseHouse.LockboxSystem: enables/disables the lockbox system
 *      2. BaseHouse.TaxCreditSystem: enables/disables the tax credit system
 *  5/8/07, Adam
 *      First time checkin
 */

using Server.Items;
using Server.Multis;
using System.Collections;

namespace Server.Mobiles
{
    public class SBGeneralContractor : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBGeneralContractor()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if (Core.RuleSets.AngelIslandRules() || Core.SiegeII_CFG)
                {
                    // this book may contain irrelevant information depending on the lockbox system settings
                    Add(new GenericBuyInfo("Hello", typeof(StorageUpgradeInfo), BaseVendor.PlayerPays(typeof(StorageUpgradeInfo), siege_factor: 0.33334), 20, 0xFF2, 0));
                    // we give them the base price since this cost (the deed's ACTUAL cost) is added to the houses upgrades, which is later refunded.
                    Add(new GenericBuyInfo(typeof(ModestUpgradeContract), BaseVendor.PlayerPays(typeof(ModestUpgradeContract), siege_factor: 0.33334), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(ModerateUpgradeContract), BaseVendor.PlayerPays(typeof(ModerateUpgradeContract), siege_factor: 0.33334), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(PremiumUpgradeContract), BaseVendor.PlayerPays(typeof(PremiumUpgradeContract), siege_factor: 0.33334), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(LavishUpgradeContract), BaseVendor.PlayerPays(typeof(LavishUpgradeContract), siege_factor: 0.33334), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(DeluxeUpgradeContract), BaseVendor.PlayerPays(typeof(DeluxeUpgradeContract), siege_factor: 0.33334), 20, 0x14F0, 0));

                    if (BaseHouse.LockboxSystem)
                    {
                        if (BaseHouse.TaxCreditSystem)
                            Add(new GenericBuyInfo("30 day tax credit: Lockbox", typeof(StorageTaxCredits), BaseVendor.PlayerPays(typeof(StorageTaxCredits)), 20, 0x14F0, 0));

                        Add(new GenericBuyInfo("A building permit: Lockbox", typeof(LockboxBuildingPermit), BaseVendor.PlayerPays(typeof(LockboxBuildingPermit)), 20, 0x14F0, 0));
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
                    Add(typeof(InteriorDecorator), BaseVendor.VendorPays(typeof(InteriorDecorator)));
                }

                if (Core.RuleSets.AOSRules() || Core.RuleSets.AngelIslandRules())
                {   // cash buyback (AOS)
                    Add(typeof(HousePlacementTool), BaseVendor.VendorPays(typeof(HousePlacementTool)));
                }
            }
        }
    }
}