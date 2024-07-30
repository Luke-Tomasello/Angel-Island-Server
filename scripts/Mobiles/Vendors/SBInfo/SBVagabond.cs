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

/* Scripts/Mobiles/Vendors/SBInfo/SBVagabond.cs
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

using Server.Engines.ResourcePool;
using Server.Items;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class SBVagabond : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();
        private static List<Type> BBSTypes = new List<Type>() { typeof(Board), typeof(IronIngot) };

        public SBVagabond()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Amber), BaseVendor.PlayerPays(typeof(Amber)), 20, 0xF25, 0));
                Add(new GenericBuyInfo(typeof(Amethyst), BaseVendor.PlayerPays(typeof(Amethyst)), 20, 0xF16, 0));
                Add(new GenericBuyInfo(typeof(Citrine), BaseVendor.PlayerPays(typeof(Citrine)), 20, 0xF15, 0));
                Add(new GenericBuyInfo(typeof(Diamond), BaseVendor.PlayerPays(typeof(Diamond)), 20, 0xF26, 0));
                Add(new GenericBuyInfo(typeof(Emerald), BaseVendor.PlayerPays(typeof(Emerald)), 20, 0xF10, 0));
                Add(new GenericBuyInfo(typeof(Ruby), BaseVendor.PlayerPays(typeof(Ruby)), 20, 0xF13, 0));
                Add(new GenericBuyInfo(typeof(Sapphire), BaseVendor.PlayerPays(typeof(Sapphire)), 20, 0xF19, 0));
                Add(new GenericBuyInfo(typeof(StarSapphire), BaseVendor.PlayerPays(typeof(StarSapphire)), 20, 0xF21, 0));
                Add(new GenericBuyInfo(typeof(Tourmaline), BaseVendor.PlayerPays(typeof(Tourmaline)), 20, 0xF2D, 0));

                if (Core.RuleSets.ResourcePoolRules())
                {   // balanced buyback system
                    foreach (Type type in BBSTypes)
                        if (ResourcePool.IsPooledResource(type))
                            Add(new GenericBuyInfo(type));
#if false
                    Add(new GenericBuyInfo(typeof(Board)));
                    Add(new GenericBuyInfo(typeof(IronIngot)));
#endif
                }
                else if (Core.RuleSets.ShopkeepersSellResourcesRules())
                {
                    Add(new GenericBuyInfo(typeof(Board), BaseVendor.PlayerPays(typeof(Board)), 20, 0x1BD7, 0));
                    Add(new GenericBuyInfo(typeof(IronIngot), BaseVendor.PlayerPays(typeof(IronIngot)), 16, 0x1BF2, 0));
                }

                Add(new GenericBuyInfo(typeof(Necklace), BaseVendor.PlayerPays(typeof(Necklace)), 20, 0x1085, 0));
                Add(new GenericBuyInfo(typeof(GoldRing), BaseVendor.PlayerPays(typeof(GoldRing)), 20, 0x108A, 0));
                Add(new GenericBuyInfo(typeof(GoldNecklace), BaseVendor.PlayerPays(typeof(GoldNecklace)), 20, 0x1088, 0));
                Add(new GenericBuyInfo(typeof(GoldBeadNecklace), BaseVendor.PlayerPays(typeof(GoldBeadNecklace)), 20, 0x1089, 0));
                Add(new GenericBuyInfo(typeof(GoldBracelet), BaseVendor.PlayerPays(typeof(GoldBracelet)), 20, 0x1086, 0));
                Add(new GenericBuyInfo(typeof(GoldEarrings), BaseVendor.PlayerPays(typeof(GoldEarrings)), 20, 0x1087, 0));
                Add(new GenericBuyInfo(typeof(Beads), BaseVendor.PlayerPays(typeof(Beads)), 20, 0x108B, 0));
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
                    AddToResourcePool(typeof(Board));
                    AddToResourcePool(typeof(IronIngot));
#endif
                }
                else if (Core.RuleSets.ShopkeepersBuyResourcesRules())
                {   // cash buyback
                    Add(typeof(Board), BaseVendor.VendorPays(typeof(Board)));
                    Add(typeof(IronIngot), BaseVendor.VendorPays(typeof(IronIngot)));
                }

                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(Amber), BaseVendor.VendorPays(typeof(Amber)));
                    Add(typeof(Amethyst), BaseVendor.VendorPays(typeof(Amethyst)));
                    Add(typeof(Citrine), BaseVendor.VendorPays(typeof(Citrine)));
                    Add(typeof(Diamond), BaseVendor.VendorPays(typeof(Diamond)));
                    Add(typeof(Emerald), BaseVendor.VendorPays(typeof(Emerald)));
                    Add(typeof(Ruby), BaseVendor.VendorPays(typeof(Ruby)));
                    Add(typeof(Sapphire), BaseVendor.VendorPays(typeof(Sapphire)));
                    Add(typeof(StarSapphire), BaseVendor.VendorPays(typeof(StarSapphire)));
                    Add(typeof(Tourmaline), BaseVendor.VendorPays(typeof(Tourmaline)));

                    Add(typeof(Necklace), BaseVendor.VendorPays(typeof(Necklace)));
                    Add(typeof(GoldRing), BaseVendor.VendorPays(typeof(GoldRing)));
                    Add(typeof(GoldNecklace), BaseVendor.VendorPays(typeof(GoldNecklace)));
                    Add(typeof(GoldBeadNecklace), BaseVendor.VendorPays(typeof(GoldBeadNecklace)));
                    Add(typeof(GoldBracelet), BaseVendor.VendorPays(typeof(GoldBracelet)));
                    Add(typeof(GoldEarrings), BaseVendor.VendorPays(typeof(GoldEarrings)));
                    Add(typeof(Beads), BaseVendor.VendorPays(typeof(Beads)));
                }
            }
        }
    }
}