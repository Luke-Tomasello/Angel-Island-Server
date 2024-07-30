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

/* Scripts/Mobiles/Vendors/SBInfo/AnimalTrainer.cs
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
 *	5/6/05: Pix
 *		Updated "ItemID" field for mobiles that the animal trainer sells
 */

using System.Collections;

namespace Server.Mobiles
{
    public class SBAnimalTrainer : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBAnimalTrainer()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new AnimalBuyInfo(1, typeof(Eagle), BaseVendor.PlayerPays(typeof(Eagle)), 10, 5, 0));
                Add(new AnimalBuyInfo(1, typeof(Cat), BaseVendor.PlayerPays(typeof(Cat)), 10, 201, 0));
                Add(new AnimalBuyInfo(1, typeof(Horse), BaseVendor.PlayerPays(typeof(Horse)), 10, 204, 0));
                Add(new AnimalBuyInfo(1, typeof(Rabbit), BaseVendor.PlayerPays(typeof(Rabbit)), 10, 205, 0));
                Add(new AnimalBuyInfo(1, typeof(BrownBear), BaseVendor.PlayerPays(typeof(BrownBear)), 10, 167, 0));
                Add(new AnimalBuyInfo(1, typeof(GrizzlyBear), BaseVendor.PlayerPays(typeof(GrizzlyBear)), 10, 212, 0));
                Add(new AnimalBuyInfo(1, typeof(Panther), BaseVendor.PlayerPays(typeof(Panther)), 10, 214, 0));
                Add(new AnimalBuyInfo(1, typeof(Dog), BaseVendor.PlayerPays(typeof(Dog)), 10, 217, 0));
                Add(new AnimalBuyInfo(1, typeof(TimberWolf), BaseVendor.PlayerPays(typeof(TimberWolf)), 10, 225, 0));
                Add(new AnimalBuyInfo(1, typeof(PackHorse), BaseVendor.PlayerPays(typeof(PackHorse)), 10, 291, 0));
                Add(new AnimalBuyInfo(1, typeof(PackLlama), BaseVendor.PlayerPays(typeof(PackLlama)), 10, 292, 0));
                Add(new AnimalBuyInfo(1, typeof(Rat), BaseVendor.PlayerPays(typeof(Rat)), 10, 238, 0));
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