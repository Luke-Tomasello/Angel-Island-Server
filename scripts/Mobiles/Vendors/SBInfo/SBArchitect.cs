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

/* Scripts/Mobiles/Vendors/SBInfo/SBArchitect.cs
 * ChangeLog
 *  2/3/24, Yoar
 *  Added house door addon for Siege II
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
 *  9/29/07, plasma
 *    Added DarkWoodGateHouseDoorDeed, LightWoodHouseDoorDeed, LightWoodGateHouseDoorDeed, 
 *    StrongWoodHouseDoorDeed, SmallIronGateHouseDoorDeed, SecretLightStoneHouseDoorDeed,
 *    SecretLightWoodHouseDoorDeed, SecretDarkWoodHouseDoorDeed and RattanHouseDoorDeed to the buy list     
 *  5/6/07, Adam
 *      Add StorageTaxCredits, and LockboxBuildingPermit deeds to the shopping list
 *	2/27/07, Pix
 *		Added CellHouseDoorDeed
 *  12/05/06 Taran Kain
 *      Added IronGateHouseDoorDeed.
 *	9/05/06 Taran Kain
 *		Added MetalHouseDoorDeed, DarkWoodHouseDoorDeed, SecretStoneHouseDoorDeed to vendor sell list.
 *  9/26/04, Jade
 *      Added SurveyTool to the vendor inventory.
 *	4/29/04, mith
 *		removed Core.AOS check so that Architects will sell house placement tools even if AOS is disabled.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBArchitect : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBArchitect()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if (Core.RuleSets.AngelIslandRules() || (Core.RuleSets.AnyAIShardRules() && PublishInfo.Publish >= 11))
                    Add(new GenericBuyInfo("1041280", typeof(InteriorDecorator), BaseVendor.PlayerPays(typeof(InteriorDecorator)), 20, 0xFC1, 0));

                // First mention of the 'Survey tool' was in Publish 5, Update 1 April 28, 2000.
                // However it says it was fixed, so we'll assume Publish 5 https://www.uoguide.com/Publish_5
                // RunUO doesn't have it. I think they are wrong
                if (PublishInfo.Publish >= 5 && (Core.RuleSets.StandardShardRules() || Core.RuleSets.AngelIslandRules()))
                    Add(new GenericBuyInfo("Survey Tool", typeof(SurveyTool), BaseVendor.PlayerPays(typeof(SurveyTool)), 20, 0x14F6, 0));

                // 2/3/24, Yoar: Added house door addon for Siege II
                if (Core.RuleSets.AngelIslandRules() || Core.SiegeII_CFG)
                {
                    Add(new GenericBuyInfo(typeof(MetalHouseDoorDeed), BaseVendor.PlayerPays(typeof(MetalHouseDoorDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(DarkWoodHouseDoorDeed), BaseVendor.PlayerPays(typeof(DarkWoodHouseDoorDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(DarkWoodGateHouseDoorDeed), BaseVendor.PlayerPays(typeof(DarkWoodGateHouseDoorDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(LightWoodHouseDoorDeed), BaseVendor.PlayerPays(typeof(LightWoodHouseDoorDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(LightWoodGateHouseDoorDeed), BaseVendor.PlayerPays(typeof(LightWoodGateHouseDoorDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(StrongWoodHouseDoorDeed), BaseVendor.PlayerPays(typeof(StrongWoodHouseDoorDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(IronGateHouseDoorDeed), BaseVendor.PlayerPays(typeof(IronGateHouseDoorDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(SmallIronGateHouseDoorDeed), BaseVendor.PlayerPays(typeof(SmallIronGateHouseDoorDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(CellHouseDoorDeed), BaseVendor.PlayerPays(typeof(CellHouseDoorDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(RattanHouseDoorDeed), BaseVendor.PlayerPays(typeof(RattanHouseDoorDeed)), 20, 0x14F0, 0));
                    //secret doors              
                    Add(new GenericBuyInfo(typeof(SecretStoneHouseDoorDeed), BaseVendor.PlayerPays(typeof(SecretStoneHouseDoorDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(SecretLightStoneHouseDoorDeed), BaseVendor.PlayerPays(typeof(SecretLightStoneHouseDoorDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(SecretLightWoodHouseDoorDeed), BaseVendor.PlayerPays(typeof(SecretLightWoodHouseDoorDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(SecretDarkWoodHouseDoorDeed), BaseVendor.PlayerPays(typeof(SecretDarkWoodHouseDoorDeed)), 20, 0x14F0, 0));

                }

                if (Core.RuleSets.AOSRules() || Core.RuleSets.AngelIslandRules())
                    Add(new GenericBuyInfo("1060651", typeof(HousePlacementTool), BaseVendor.PlayerPays(typeof(HousePlacementTool)), 20, 0x14F6, 0));
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
                {   // cash buyback
                    Add(typeof(HousePlacementTool), BaseVendor.VendorPays(typeof(HousePlacementTool)));
                }
            }
        }
    }
}