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

/* Scripts/Mobiles/Vendors/SBInfo/SBGypsyTrader.cs
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
 *	9/19/06, Pix
 *		Added TeleporterAddonDyeTub for sale for 15K
 *	9/13/06, Pix
 *		Removed old TeleporterDeed and replaced it with TeleporterAddonDeed
 *	3/29/06 Taran Kain
 *		Add NPCNameChangeDeed for 4000
 *	3/27/05, Kitaras
 *		Add the NpcTitleChangeDeed for sale for 1500
 *	3/15/05, Adam
 *		Add the OrcishBodyDeed for sale for 1500
 *	11/16/04, Froste
 *      Created from SBCarpenter.cs
 */

using Server.Items;
using System.Collections;


namespace Server.Mobiles
{
    public class SBGypsyTrader : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBGypsyTrader()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if (Core.RuleSets.AngelIslandRules())
                {
                    //Add(new GenericBuyInfo("Teleporter Deed", typeof(TeleporterDeed), BaseVendor.GetSellPrice(typeof(TeleporterDeed)), 20, 0x14F0, 0x0));
                    Add(new GenericBuyInfo("Teleporter Addon Deed", typeof(TeleporterAddonDeed), BaseVendor.PlayerPays(typeof(TeleporterAddonDeed)), 20, 0x14F0, 0x0));
                    Add(new GenericBuyInfo("Teleporter Dye Tub", typeof(TeleporterAddonDyeTub), BaseVendor.PlayerPays(typeof(TeleporterAddonDyeTub)), 20, 0xFAB, 0x0));
                    Add(new GenericBuyInfo("Orcish Vendor Body Deed", typeof(OrcishBodyDeed), BaseVendor.PlayerPays(typeof(OrcishBodyDeed)), 20, 0x14F0, 0x0));
                }

                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeStyleRules())
                {
                    Add(new GenericBuyInfo("Vendor Name Change Deed", typeof(NpcNameChangeDeed), BaseVendor.PlayerPays(typeof(NpcNameChangeDeed)), 20, 0x14F0, 0x0));
                    Add(new GenericBuyInfo("Vendor Title Change Deed", typeof(NpcTitleChangeDeed), BaseVendor.PlayerPays(typeof(NpcTitleChangeDeed)), 20, 0x14F0, 0x0));
                    Add(new GenericBuyInfo("Gender Change Deed", typeof(GenderChangeDeed), BaseVendor.PlayerPays(typeof(GenderChangeDeed)), 20, 0x14F0, 0x0));
                    Add(new GenericBuyInfo("Name Change Deed", typeof(NameChangeDeed), BaseVendor.PlayerPays(typeof(NameChangeDeed)), 20, 0x14F0, 0x0));
                }
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