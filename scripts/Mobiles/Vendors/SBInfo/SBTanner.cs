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

/* ChangeLog
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
 *  04/02/05 TK
 *		Added special leathers and redirects
 *  01/28/05 TK
 *		Added hides.
 *  01/23/05, Taran Kain
 *		Added leather.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *
 *  9/14/04, Lego Eater
 *              changed price of taxidermykit from 100k to 30k
 */

using Server.Engines.ResourcePool;
using Server.Items;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class SBTanner : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();
        private static List<Type> BBSTypes = new List<Type>() { typeof(Leather), typeof(SpinedLeather), typeof(HornedLeather), typeof(BarbedLeather), typeof(Hides), typeof(SpinedHides), typeof(BarbedHides), typeof(HornedHides) };

        public SBTanner()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Bag), BaseVendor.PlayerPays(typeof(Bag)), 20, 0xE76, 0));
                Add(new GenericBuyInfo(typeof(Pouch), BaseVendor.PlayerPays(typeof(Pouch)), 20, 0xE79, 0));

                if (Core.RuleSets.ResourcePoolRules())
                {   // balanced buyback
                    foreach (Type type in BBSTypes)
                        if (ResourcePool.IsPooledResource(type))
                            Add(new GenericBuyInfo(type));
#if false
                    Add(new GenericBuyInfo(typeof(Leather)));
                    Add(new GenericBuyInfo(typeof(SpinedLeather)));
                    Add(new GenericBuyInfo(typeof(HornedLeather)));
                    Add(new GenericBuyInfo(typeof(BarbedLeather)));
#endif
                }
                else if (Core.RuleSets.ShopkeepersSellResourcesRules())
                    Add(new GenericBuyInfo(typeof(Leather), BaseVendor.PlayerPays(typeof(Leather)), 20, 0x1081, 0));

                if (Core.RuleSets.AngelIslandRules() || (Core.RuleSets.StandardShardRules() && PublishInfo.Publish >= 11))
                    Add(new GenericBuyInfo("1041279", typeof(TaxidermyKit), BaseVendor.PlayerPays(typeof(TaxidermyKit)), 20, 0x1EBA, 0));//changed price of taxidermykit from 100k to 30k Lego eater.

                Add(new GenericBuyInfo(typeof(SkinningKnife), BaseVendor.PlayerPays(typeof(SkinningKnife)), 20, 0xEC4, 0));
                Add(new GenericBuyInfo(typeof(LeatherLegs), BaseVendor.PlayerPays(typeof(LeatherLegs)), 20, 0x13CB, 0));
                Add(new GenericBuyInfo(typeof(LeatherShorts), BaseVendor.PlayerPays(typeof(LeatherShorts)), 20, 0x1C00, 0));
                Add(new GenericBuyInfo(typeof(LeatherSkirt), BaseVendor.PlayerPays(typeof(LeatherSkirt)), 20, 0x1C08, 0));
                Add(new GenericBuyInfo(typeof(LeatherCap), BaseVendor.PlayerPays(typeof(LeatherCap)), 20, 0x1DB9, 0));
                Add(new GenericBuyInfo(typeof(LeatherGloves), BaseVendor.PlayerPays(typeof(LeatherGloves)), 20, 0x13C6, 0));
                Add(new GenericBuyInfo(typeof(LeatherGorget), BaseVendor.PlayerPays(typeof(LeatherGorget)), 20, 0x13C7, 0));
                Add(new GenericBuyInfo(typeof(LeatherChest), BaseVendor.PlayerPays(typeof(LeatherChest)), 20, 0x13CC, 0));
                Add(new GenericBuyInfo(typeof(LeatherBustierArms), BaseVendor.PlayerPays(typeof(LeatherBustierArms)), 20, 0x1C0A, 0));
                Add(new GenericBuyInfo(typeof(LeatherArms), BaseVendor.PlayerPays(typeof(LeatherArms)), 20, 0x13CD, 0));
                Add(new GenericBuyInfo(typeof(StuddedLegs), BaseVendor.PlayerPays(typeof(StuddedLegs)), 20, 0x13DA, 0));
                Add(new GenericBuyInfo(typeof(StuddedGloves), BaseVendor.PlayerPays(typeof(StuddedGloves)), 20, 0x13D5, 0));
                Add(new GenericBuyInfo(typeof(StuddedGorget), BaseVendor.PlayerPays(typeof(StuddedGorget)), 20, 0x13D6, 0));
                Add(new GenericBuyInfo(typeof(StuddedChest), BaseVendor.PlayerPays(typeof(StuddedChest)), 20, 0x13DB, 0));
                Add(new GenericBuyInfo(typeof(StuddedBustierArms), BaseVendor.PlayerPays(typeof(StuddedBustierArms)), 20, 0x1C0C, 0));
                Add(new GenericBuyInfo(typeof(StuddedArms), BaseVendor.PlayerPays(typeof(StuddedArms)), 20, 0x13DC, 0));
                Add(new GenericBuyInfo(typeof(FemaleStuddedChest), BaseVendor.PlayerPays(typeof(FemaleStuddedChest)), 20, 0x1C02, 0));
                Add(new GenericBuyInfo(typeof(FemalePlateChest), BaseVendor.PlayerPays(typeof(FemalePlateChest)), 20, 0x1C04, 0));
                Add(new GenericBuyInfo(typeof(FemaleLeatherChest), BaseVendor.PlayerPays(typeof(FemaleLeatherChest)), 20, 0x1C06, 0));
                Add(new GenericBuyInfo(typeof(Backpack), BaseVendor.PlayerPays(typeof(Backpack)), 20, 0x9B2, 0));
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
                    AddToResourcePool(typeof(Leather));
                    AddToResourcePool(typeof(SpinedLeather));
                    AddToResourcePool(typeof(HornedLeather));
                    AddToResourcePool(typeof(BarbedLeather));
                    AddToResourcePool(typeof(Hides));
                    AddToResourcePool(typeof(SpinedHides));
                    AddToResourcePool(typeof(BarbedHides));
                    AddToResourcePool(typeof(HornedHides));
#endif
                }
                else if (Core.RuleSets.ShopkeepersBuyResourcesRules())
                {   // cash buyback
                    Add(typeof(Leather), BaseVendor.VendorPays(typeof(Leather)));
                }

                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(LeatherArms), BaseVendor.VendorPays(typeof(LeatherArms)));
                    Add(typeof(LeatherChest), BaseVendor.VendorPays(typeof(LeatherChest)));
                    Add(typeof(LeatherGloves), BaseVendor.VendorPays(typeof(LeatherGloves)));
                    Add(typeof(LeatherGorget), BaseVendor.VendorPays(typeof(LeatherGorget)));
                    Add(typeof(LeatherLegs), BaseVendor.VendorPays(typeof(LeatherLegs)));
                    Add(typeof(LeatherCap), BaseVendor.VendorPays(typeof(LeatherCap)));

                    Add(typeof(StuddedArms), BaseVendor.VendorPays(typeof(StuddedArms)));
                    Add(typeof(StuddedChest), BaseVendor.VendorPays(typeof(StuddedChest)));
                    Add(typeof(StuddedGloves), BaseVendor.VendorPays(typeof(StuddedGloves)));
                    Add(typeof(StuddedGorget), BaseVendor.VendorPays(typeof(StuddedGorget)));
                    Add(typeof(StuddedLegs), BaseVendor.VendorPays(typeof(StuddedLegs)));

                    Add(typeof(FemaleStuddedChest), BaseVendor.VendorPays(typeof(FemaleStuddedChest)));
                    Add(typeof(StuddedBustierArms), BaseVendor.VendorPays(typeof(StuddedBustierArms)));

                    Add(typeof(FemaleLeatherChest), BaseVendor.VendorPays(typeof(FemaleLeatherChest)));
                    Add(typeof(LeatherBustierArms), BaseVendor.VendorPays(typeof(LeatherBustierArms)));
                    Add(typeof(LeatherShorts), BaseVendor.VendorPays(typeof(LeatherShorts)));
                    Add(typeof(LeatherSkirt), BaseVendor.VendorPays(typeof(LeatherSkirt)));
                }
            }
        }
    }
}