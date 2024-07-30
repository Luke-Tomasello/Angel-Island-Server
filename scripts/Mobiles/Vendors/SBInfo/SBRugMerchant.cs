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

/* Scripts/Mobiles/Vendors/SBInfo/SBRugMerchant.cs
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
 *  11/16/21 Yoar
 *		Removed BBS support.
 *  04/02/05 TK
 *		Added yarn
 *	9/19/04, Pigpen
 *		Fixed price on Delucia Medium East Carpet.
 *	9/17/04, Pigpen
 *		Created SBRugMerchant.cs
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBRugMerchant : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBRugMerchant()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if (Core.RuleSets.CarpetStore())
                {
                    Add(new GenericBuyInfo("Britain Small Carpet [east]", typeof(BritainSmallEastAddonDeed), BaseVendor.PlayerPays(typeof(BritainSmallEastAddonDeed)), 20, 0xAC6, 0x0));
                    Add(new GenericBuyInfo("Britain Small Carpet [south]", typeof(BritainSmallSouthAddonDeed), BaseVendor.PlayerPays(typeof(BritainSmallSouthAddonDeed)), 20, 0xAC6, 0x0));
                    Add(new GenericBuyInfo("Britain Medium Carpet [East]", typeof(BritainMediumEastAddonDeed), BaseVendor.PlayerPays(typeof(BritainMediumEastAddonDeed)), 20, 0xAC6, 0x0));
                    Add(new GenericBuyInfo("Britain Medium Carpet [south]", typeof(BritainMediumSouthAddonDeed), BaseVendor.PlayerPays(typeof(BritainMediumSouthAddonDeed)), 20, 0xAC6, 0x0));
                    Add(new GenericBuyInfo("Britain Large Carpet", typeof(BritainLargeAddonDeed), BaseVendor.PlayerPays(typeof(BritainLargeAddonDeed)), 20, 0xAC6, 0x0));
                    Add(new GenericBuyInfo("Delucia Small Carpet [east]", typeof(DeluciaSmallEastAddonDeed), BaseVendor.PlayerPays(typeof(DeluciaSmallEastAddonDeed)), 20, 0xAEB, 0x0));
                    Add(new GenericBuyInfo("Delucia Small Carpet [south]", typeof(DeluciaSmallSouthAddonDeed), BaseVendor.PlayerPays(typeof(DeluciaSmallSouthAddonDeed)), 20, 0xAEB, 0x0));
                    Add(new GenericBuyInfo("Delucia Medium Carpet [East]", typeof(DeluciaMediumEastAddonDeed), BaseVendor.PlayerPays(typeof(DeluciaMediumEastAddonDeed)), 20, 0xAEB, 0x0));
                    Add(new GenericBuyInfo("Delucia Medium Carpet [south]", typeof(DeluciaMediumSouthAddonDeed), BaseVendor.PlayerPays(typeof(DeluciaMediumSouthAddonDeed)), 20, 0xAEB, 0x0));
                    Add(new GenericBuyInfo("Delucia Large Carpet", typeof(DeluciaLargeAddonDeed), BaseVendor.PlayerPays(typeof(DeluciaLargeAddonDeed)), 20, 0xAEB, 0x0));
                    Add(new GenericBuyInfo("Magincia Small Carpet [east]", typeof(MaginciaSmallEastAddonDeed), BaseVendor.PlayerPays(typeof(MaginciaSmallEastAddonDeed)), 20, 0xAD1, 0x0));
                    Add(new GenericBuyInfo("Magincia Small Carpet [south]", typeof(MaginciaSmallSouthAddonDeed), BaseVendor.PlayerPays(typeof(MaginciaSmallSouthAddonDeed)), 20, 0xAD1, 0x0));
                    Add(new GenericBuyInfo("Magincia Medium Carpet [East]", typeof(MaginciaMediumEastAddonDeed), BaseVendor.PlayerPays(typeof(MaginciaMediumEastAddonDeed)), 20, 0xAD1, 0x0));
                    Add(new GenericBuyInfo("Magincia Medium Carpet [south]", typeof(MaginciaMediumSouthAddonDeed), BaseVendor.PlayerPays(typeof(MaginciaMediumSouthAddonDeed)), 20, 0xAD1, 0x0));
                    Add(new GenericBuyInfo("Magincia Large Carpet", typeof(MaginciaLargeAddonDeed), BaseVendor.PlayerPays(typeof(MaginciaLargeAddonDeed)), 20, 0xAD1, 0x0));
                    Add(new GenericBuyInfo("Minoc Small Carpet [east]", typeof(MinocSmallEastAddonDeed), BaseVendor.PlayerPays(typeof(MinocSmallEastAddonDeed)), 20, 0x109D, 0x0));
                    Add(new GenericBuyInfo("Minoc Small Carpet [south]", typeof(MinocSmallSouthAddonDeed), BaseVendor.PlayerPays(typeof(MinocSmallSouthAddonDeed)), 20, 0x109D, 0x0));
                    Add(new GenericBuyInfo("Minoc Medium Carpet [East]", typeof(MinocMediumEastAddonDeed), BaseVendor.PlayerPays(typeof(MinocMediumEastAddonDeed)), 20, 0x109D, 0x0));
                    Add(new GenericBuyInfo("Minoc Medium Carpet [south]", typeof(MinocMediumSouthAddonDeed), BaseVendor.PlayerPays(typeof(MinocMediumSouthAddonDeed)), 20, 0x109D, 0x0));
                    Add(new GenericBuyInfo("Minoc Large Carpet", typeof(MinocLargeAddonDeed), BaseVendor.PlayerPays(typeof(MinocLargeAddonDeed)), 20, 0x109D, 0x0));
                    Add(new GenericBuyInfo("Nujelm Small Carpet [east]", typeof(NujelmSmallEastAddonDeed), BaseVendor.PlayerPays(typeof(NujelmSmallEastAddonDeed)), 20, 0xABD, 0x0));
                    Add(new GenericBuyInfo("Nujelm Small Carpet [south]", typeof(NujelmSmallSouthAddonDeed), BaseVendor.PlayerPays(typeof(NujelmSmallSouthAddonDeed)), 20, 0xABD, 0x0));
                    Add(new GenericBuyInfo("Nujelm Medium Carpet [East]", typeof(NujelmMediumEastAddonDeed), BaseVendor.PlayerPays(typeof(NujelmMediumEastAddonDeed)), 20, 0xABD, 0x0));
                    Add(new GenericBuyInfo("Nujelm Medium Carpet [south]", typeof(NujelmMediumSouthAddonDeed), BaseVendor.PlayerPays(typeof(NujelmMediumSouthAddonDeed)), 20, 0xABD, 0x0));
                    Add(new GenericBuyInfo("Nujelm Large Carpet", typeof(NujelmLargeAddonDeed), BaseVendor.PlayerPays(typeof(NujelmLargeAddonDeed)), 20, 0xABD, 0x0));
                    Add(new GenericBuyInfo("Occlo Small Carpet [east]", typeof(OccloSmallEastAddonDeed), BaseVendor.PlayerPays(typeof(OccloSmallEastAddonDeed)), 20, 0xAED, 0x0));
                    Add(new GenericBuyInfo("Occlo Small Carpet [south]", typeof(OccloSmallSouthAddonDeed), BaseVendor.PlayerPays(typeof(OccloSmallSouthAddonDeed)), 20, 0xAED, 0x0));
                    Add(new GenericBuyInfo("Occlo Medium Carpet [East]", typeof(OccloMediumEastAddonDeed), BaseVendor.PlayerPays(typeof(OccloMediumEastAddonDeed)), 20, 0xAED, 0x0));
                    Add(new GenericBuyInfo("Occlo Medium Carpet [south]", typeof(OccloMediumSouthAddonDeed), BaseVendor.PlayerPays(typeof(OccloMediumSouthAddonDeed)), 20, 0xAED, 0x0));
                    Add(new GenericBuyInfo("Occlo Large Carpet", typeof(OccloLargeAddonDeed), BaseVendor.PlayerPays(typeof(OccloLargeAddonDeed)), 20, 0xAED, 0x0));
                    Add(new GenericBuyInfo("Trinsic Small Carpet [east]", typeof(TrinsicSmallEastAddonDeed), BaseVendor.PlayerPays(typeof(TrinsicSmallEastAddonDeed)), 20, 0xADA, 0x0));
                    Add(new GenericBuyInfo("Trinsic Small Carpet [south]", typeof(TrinsicSmallSouthAddonDeed), BaseVendor.PlayerPays(typeof(TrinsicSmallSouthAddonDeed)), 20, 0xADA, 0x0));
                    Add(new GenericBuyInfo("Trinsic Medium Carpet [East]", typeof(TrinsicMediumEastAddonDeed), BaseVendor.PlayerPays(typeof(TrinsicMediumEastAddonDeed)), 20, 0xADA, 0x0));
                    Add(new GenericBuyInfo("Trinsic Medium Carpet [south]", typeof(TrinsicMediumSouthAddonDeed), BaseVendor.PlayerPays(typeof(TrinsicMediumSouthAddonDeed)), 20, 0xADA, 0x0));
                    Add(new GenericBuyInfo("Trinsic Large Carpet", typeof(TrinsicLargeAddonDeed), BaseVendor.PlayerPays(typeof(TrinsicLargeAddonDeed)), 20, 0xADA, 0x0));
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