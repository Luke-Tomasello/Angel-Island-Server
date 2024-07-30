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

/* Scripts/Mobiles/Vendors/SBInfo/SBWeaver.cs
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
 *  02/04/05 TK
 *		Added new cloth redirects
 *  01/28/05 TK
 *		Added BoltOfCloth, UncutCloth
 *	01/23/05, Taran Kain
 *		Added cloth.
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
    public class SBWeaver : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();
        private static List<Type> BBSTypes = new List<Type>() { typeof(UncutCloth), typeof(BoltOfCloth), typeof(DarkYarn), typeof(LightYarn), typeof(LightYarnUnraveled), typeof(Cloth), typeof(Cotton), typeof(Wool), typeof(SpoolOfThread) };

        public SBWeaver()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Dyes), BaseVendor.PlayerPays(typeof(Dyes)), 20, 0xFA9, 0));
                Add(new GenericBuyInfo(typeof(DyeTub), BaseVendor.PlayerPays(typeof(DyeTub)), 20, 0xFAB, 0));

                if (Core.RuleSets.ResourcePoolRules())
                {   // balanced buyback system
                    foreach (Type type in BBSTypes)
                        if (ResourcePool.IsPooledResource(type))
                            Add(new GenericBuyInfo(type));
#if false
                    /*
                    Add(new GenericBuyInfo(typeof(UncutCloth)));
                    Add(new GenericBuyInfo(typeof(BoltOfCloth)));
                    Add(new GenericBuyInfo(typeof(DarkYarn)));
                    Add(new GenericBuyInfo(typeof(LightYarn)));
                    Add(new GenericBuyInfo(typeof(LightYarnUnraveled)));
                    */
                    Add(new GenericBuyInfo(typeof(Cloth)));
#endif
                }
                else if (Core.RuleSets.ShopkeepersSellResourcesRules())
                {
                    Add(new GenericBuyInfo(typeof(UncutCloth), BaseVendor.PlayerPays(typeof(UncutCloth)), 20, 0x1761, 0));
                    Add(new GenericBuyInfo(typeof(UncutCloth), BaseVendor.PlayerPays(typeof(UncutCloth)), 20, 0x1762, 0));
                    Add(new GenericBuyInfo(typeof(UncutCloth), BaseVendor.PlayerPays(typeof(UncutCloth)), 20, 0x1763, 0));
                    Add(new GenericBuyInfo(typeof(UncutCloth), BaseVendor.PlayerPays(typeof(UncutCloth)), 20, 0x1764, 0));

                    Add(new GenericBuyInfo(typeof(BoltOfCloth), BaseVendor.PlayerPays(typeof(BoltOfCloth)), 20, 0xf9B, 0));
                    Add(new GenericBuyInfo(typeof(BoltOfCloth), BaseVendor.PlayerPays(typeof(BoltOfCloth)), 20, 0xf9C, 0));
                    Add(new GenericBuyInfo(typeof(BoltOfCloth), BaseVendor.PlayerPays(typeof(BoltOfCloth)), 20, 0xf96, 0));
                    Add(new GenericBuyInfo(typeof(BoltOfCloth), BaseVendor.PlayerPays(typeof(BoltOfCloth)), 20, 0xf97, 0));

                    Add(new GenericBuyInfo(typeof(DarkYarn), BaseVendor.PlayerPays(typeof(DarkYarn)), 20, 0xE1D, 0));
                    Add(new GenericBuyInfo(typeof(LightYarn), BaseVendor.PlayerPays(typeof(LightYarn)), 20, 0xE1E, 0));
                    Add(new GenericBuyInfo(typeof(LightYarnUnraveled), BaseVendor.PlayerPays(typeof(LightYarnUnraveled)), 20, 0xE1F, 0));
                }

                Add(new GenericBuyInfo(typeof(Scissors), BaseVendor.PlayerPays(typeof(Scissors)), 20, 0xF9F, 0));
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
                    AddToResourcePool(typeof(UncutCloth));
                    AddToResourcePool(typeof(BoltOfCloth));
                    AddToResourcePool(typeof(LightYarnUnraveled));
                    AddToResourcePool(typeof(LightYarn));
                    AddToResourcePool(typeof(DarkYarn));

                    AddToResourcePool(typeof(Cloth));
                    AddToResourcePool(typeof(Cotton));
                    AddToResourcePool(typeof(Wool));
                    AddToResourcePool(typeof(SpoolOfThread));
#endif
                }
                else if (Core.RuleSets.ShopkeepersSellResourcesRules())
                {   // cash buyback
                    Add(typeof(UncutCloth), BaseVendor.VendorPays(typeof(UncutCloth)));
                    Add(typeof(BoltOfCloth), BaseVendor.VendorPays(typeof(BoltOfCloth)));
                    Add(typeof(LightYarnUnraveled), BaseVendor.VendorPays(typeof(LightYarnUnraveled)));
                    Add(typeof(LightYarn), BaseVendor.VendorPays(typeof(LightYarn)));
                    Add(typeof(DarkYarn), BaseVendor.VendorPays(typeof(DarkYarn)));
                }

                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(Scissors), BaseVendor.VendorPays(typeof(Scissors)));
                    Add(typeof(Dyes), BaseVendor.VendorPays(typeof(Dyes)));
                    Add(typeof(DyeTub), BaseVendor.VendorPays(typeof(DyeTub)));
                }
            }
        }
    }
}