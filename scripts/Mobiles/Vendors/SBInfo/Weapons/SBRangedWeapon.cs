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
 *  05/02/05 TK
 *		Removed arrow, bolt, shaft, feather from list - they're covered in Bowyer
 *		Bowyer was selling arrows twice
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.ResourcePool;
using Server.Items;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class SBRangedWeapon : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();
        private static List<Type> BBSTypes = new List<Type>() { typeof(Arrow), typeof(Bolt), typeof(Shaft), typeof(Feather) };
        public SBRangedWeapon()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Crossbow), BaseVendor.PlayerPays(typeof(Crossbow)), 20, 0xF50, 0));
                Add(new GenericBuyInfo(typeof(HeavyCrossbow), BaseVendor.PlayerPays(typeof(HeavyCrossbow)), 20, 0x13FD, 0));
                Add(new GenericBuyInfo(typeof(Bow), BaseVendor.PlayerPays(typeof(Bow)), 20, 0x13B2, 0));
                if (Core.RuleSets.AOSRules())
                {
                    Add(new GenericBuyInfo(typeof(RepeatingCrossbow), BaseVendor.PlayerPays(typeof(RepeatingCrossbow)), 20, 0x26C3, 0));
                    Add(new GenericBuyInfo(typeof(CompositeBow), BaseVendor.PlayerPays(typeof(CompositeBow)), 20, 0x26C2, 0));
                }

                if (Core.RuleSets.ResourcePoolRules())
                {   // balanced buyback system
                    foreach (Type type in BBSTypes)
                        if (ResourcePool.IsPooledResource(type))
                            Add(new GenericBuyInfo(type));
                }
                else if (Core.RuleSets.ShopkeepersSellResourcesRules())
                {
                    Add(new GenericBuyInfo(typeof(Bolt), 2, BaseVendor.PlayerPays(typeof(Bolt)), 0x1BFB, 0));
                    Add(new GenericBuyInfo(typeof(Arrow), 2, BaseVendor.PlayerPays(typeof(Arrow)), 0xF3F, 0));
                    Add(new GenericBuyInfo(typeof(Feather), 2, BaseVendor.PlayerPays(typeof(Feather)), 0x1BD1, 0));
                    Add(new GenericBuyInfo(typeof(Shaft), 3, BaseVendor.PlayerPays(typeof(Shaft)), 0x1BD4, 0));
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
                }
                else if (Core.RuleSets.ShopkeepersBuyResourcesRules())
                {   // cash buyback
                    Add(typeof(Bolt), BaseVendor.PlayerPays(typeof(Bolt)));
                    Add(typeof(Arrow), BaseVendor.PlayerPays(typeof(Arrow)));
                    Add(typeof(Shaft), BaseVendor.PlayerPays(typeof(Shaft)));
                    Add(typeof(Feather), BaseVendor.PlayerPays(typeof(Feather)));
                }

                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(HeavyCrossbow), BaseVendor.PlayerPays(typeof(HeavyCrossbow)));
                    Add(typeof(Bow), BaseVendor.PlayerPays(typeof(Bow)));
                    Add(typeof(Crossbow), BaseVendor.PlayerPays(typeof(Crossbow)));

                    if (Core.RuleSets.AOSRules())
                    {
                        Add(typeof(CompositeBow), BaseVendor.PlayerPays(typeof(CompositeBow)));
                        Add(typeof(RepeatingCrossbow), BaseVendor.PlayerPays(typeof(RepeatingCrossbow)));
                    }
                }
            }
        }
    }
}