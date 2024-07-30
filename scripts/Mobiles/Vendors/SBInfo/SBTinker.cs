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

/* Scripts/Mobiles/Vendors/SBInfo/SBTinker.cs
 * ChangeLog
 *  2/1/24, Yoar
 *  Enabled rekeying contracts for SiegeII.
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
 *  5/23/07, Adam
 *      Add the DoorRekeyingContract. This Contract allows the player to rekey a single door.
 *      (minor gold sink)
 *  09/08/05, erlein
 *     Added EtchingKit, EtchingBook.
 *  01/28/05 TK
 *		Added ore colors, logs.
 *  01/23/05, Taran Kain
 *		Added boards, all nine ingots.
 *	01/18/05, Pigpen
 *		Added hatchet to list of items for sale.
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
    public class SBTinker : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();
        private static List<Type> BBSTypes = new List<Type>() { typeof(Board), typeof(Log), typeof(IronIngot), typeof(DullCopperIngot), typeof(ShadowIronIngot), typeof(CopperIngot), typeof(BronzeIngot), typeof(GoldIngot), typeof(AgapiteIngot), typeof(VeriteIngot), typeof(ValoriteIngot), typeof(IronOre), typeof(DullCopperOre), typeof(ShadowIronOre), typeof(CopperOre), typeof(BronzeOre), typeof(GoldOre), typeof(AgapiteOre), typeof(VeriteOre), typeof(ValoriteOre) };
        public SBTinker()
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
                    Add(new GenericBuyInfo("metal etching", typeof(EtchingBook), BaseVendor.PlayerPays(typeof(EtchingBook)), 20, 0xFF4, 0));
                    Add(new GenericBuyInfo("metal etching kit", typeof(EtchingKit), BaseVendor.PlayerPays(typeof(EtchingKit)), 20, 0x1EB8, 0));
                }

                if (Core.RuleSets.AngelIslandRules() || Core.SiegeII_CFG)
                {
                    Add(new GenericBuyInfo(typeof(DoorRekeyingContract), BaseVendor.PlayerPays(typeof(DoorRekeyingContract)), 20, 0x14F0, 0));  // minor gold sink
                }

                Add(new GenericBuyInfo(typeof(Drums), BaseVendor.PlayerPays(typeof(Drums)), 20, 0x0E9C, 0));
                Add(new GenericBuyInfo(typeof(Tambourine), BaseVendor.PlayerPays(typeof(Tambourine)), 20, 0x0E9E, 0));
                Add(new GenericBuyInfo(typeof(LapHarp), BaseVendor.PlayerPays(typeof(LapHarp)), 20, 0x0EB2, 0));
                Add(new GenericBuyInfo(typeof(Lute), BaseVendor.PlayerPays(typeof(Lute)), 20, 0x0EB3, 0));

                Add(new GenericBuyInfo(typeof(Shovel), BaseVendor.PlayerPays(typeof(Shovel)), 20, 0xF39, 0));
                Add(new GenericBuyInfo(typeof(SewingKit), BaseVendor.PlayerPays(typeof(SewingKit)), 20, 0xF9D, 0));
                Add(new GenericBuyInfo(typeof(Scissors), BaseVendor.PlayerPays(typeof(Scissors)), 20, 0xF9F, 0));
                Add(new GenericBuyInfo(typeof(Tongs), BaseVendor.PlayerPays(typeof(Tongs)), 20, 0xFBB, 0));
                Add(new GenericBuyInfo(typeof(Key), BaseVendor.PlayerPays(typeof(Key)), 20, 0x100E, 0));

                Add(new GenericBuyInfo(typeof(DovetailSaw), BaseVendor.PlayerPays(typeof(DovetailSaw)), 20, 0x1028, 0));
                Add(new GenericBuyInfo(typeof(MouldingPlane), BaseVendor.PlayerPays(typeof(MouldingPlane)), 20, 0x102C, 0));
                Add(new GenericBuyInfo(typeof(Nails), BaseVendor.PlayerPays(typeof(Nails)), 20, 0x102E, 0));
                Add(new GenericBuyInfo(typeof(JointingPlane), BaseVendor.PlayerPays(typeof(JointingPlane)), 20, 0x1030, 0));
                Add(new GenericBuyInfo(typeof(SmoothingPlane), BaseVendor.PlayerPays(typeof(SmoothingPlane)), 20, 0x1032, 0));
                Add(new GenericBuyInfo(typeof(Saw), BaseVendor.PlayerPays(typeof(Saw)), 20, 0x1034, 0));

                Add(new GenericBuyInfo(typeof(Clock), BaseVendor.PlayerPays(typeof(Clock)), 20, 0x104B, 0));
                Add(new GenericBuyInfo(typeof(ClockParts), BaseVendor.PlayerPays(typeof(ClockParts)), 20, 0x104F, 0));
                Add(new GenericBuyInfo(typeof(AxleGears), BaseVendor.PlayerPays(typeof(AxleGears)), 20, 0x1051, 0));
                Add(new GenericBuyInfo(typeof(Gears), BaseVendor.PlayerPays(typeof(Gears)), 20, 0x1053, 0));
                Add(new GenericBuyInfo(typeof(Hinge), BaseVendor.PlayerPays(typeof(Hinge)), 20, 0x1055, 0));
                Add(new GenericBuyInfo(typeof(Sextant), BaseVendor.PlayerPays(typeof(Sextant)), 20, 0x1057, 0));

                if (Core.RuleSets.AnyAIShardRules())
                    Add(new GenericBuyInfo(typeof(CartographersSextant), BaseVendor.PlayerPays(typeof(CartographersSextant)), 20, 0x1057, 0));

                Add(new GenericBuyInfo(typeof(SextantParts), BaseVendor.PlayerPays(typeof(SextantParts)), 20, 0x1059, 0));
                Add(new GenericBuyInfo(typeof(Axle), BaseVendor.PlayerPays(typeof(Axle)), 20, 0x105B, 0));
                Add(new GenericBuyInfo(typeof(Springs), BaseVendor.PlayerPays(typeof(Springs)), 20, 0x105D, 0));

                Add(new GenericBuyInfo(typeof(DrawKnife), BaseVendor.PlayerPays(typeof(DrawKnife)), 20, 0x10E4, 0));
                Add(new GenericBuyInfo(typeof(Froe), BaseVendor.PlayerPays(typeof(Froe)), 20, 0x10E5, 0));
                Add(new GenericBuyInfo(typeof(Inshave), BaseVendor.PlayerPays(typeof(Inshave)), 20, 0x10E6, 0));
                Add(new GenericBuyInfo(typeof(Scorp), BaseVendor.PlayerPays(typeof(Scorp)), 20, 0x10E7, 0));

                Add(new GenericBuyInfo(typeof(Lockpick), BaseVendor.PlayerPays(typeof(Lockpick)), 20, 0x14FC, 0));
                Add(new GenericBuyInfo(typeof(TinkerTools), BaseVendor.PlayerPays(typeof(TinkerTools)), 20, 0x1EB8, 0));

                Add(new GenericBuyInfo(typeof(Pickaxe), BaseVendor.PlayerPays(typeof(Pickaxe)), 20, 0xE86, 0));
                // TODO: Sledgehammer
                Add(new GenericBuyInfo(typeof(Hammer), BaseVendor.PlayerPays(typeof(Hammer)), 20, 0x102A, 0));
                Add(new GenericBuyInfo(typeof(SmithHammer), BaseVendor.PlayerPays(typeof(SmithHammer)), 20, 0x13E3, 0));
                Add(new GenericBuyInfo(typeof(Hatchet), BaseVendor.PlayerPays(typeof(Hatchet)), 20, 0xF43, 0));  //added 1/18/05 by pigpen
                Add(new GenericBuyInfo(typeof(ButcherKnife), BaseVendor.PlayerPays(typeof(ButcherKnife)), 20, 0x13F6, 0));

                if (Core.RuleSets.ResourcePoolRules())
                {   // balanced buyback system
                    foreach (Type type in BBSTypes)
                        if (ResourcePool.IsPooledResource(type))
                            Add(new GenericBuyInfo(type));
#if false
                    Add(new GenericBuyInfo(typeof(Board)));
                    Add(new GenericBuyInfo(typeof(IronIngot)));
                    Add(new GenericBuyInfo(typeof(DullCopperIngot)));
                    Add(new GenericBuyInfo(typeof(ShadowIronIngot)));
                    Add(new GenericBuyInfo(typeof(CopperIngot)));
                    Add(new GenericBuyInfo(typeof(BronzeIngot)));
                    Add(new GenericBuyInfo(typeof(GoldIngot)));
                    Add(new GenericBuyInfo(typeof(AgapiteIngot)));
                    Add(new GenericBuyInfo(typeof(VeriteIngot)));
                    Add(new GenericBuyInfo(typeof(ValoriteIngot)));
#endif
                }
                else if (Core.RuleSets.ShopkeepersSellResourcesRules())
                {
                    Add(new GenericBuyInfo(typeof(Board), BaseVendor.PlayerPays(typeof(Board)), 20, 0x1BD7, 0));
                    Add(new GenericBuyInfo(typeof(IronIngot), BaseVendor.PlayerPays(typeof(IronIngot)), 16, 0x1BF2, 0));
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
#if false
                    AddToResourcePool(typeof(Board));
                    AddToResourcePool(typeof(Log));
                    AddToResourcePool(typeof(IronIngot));
                    AddToResourcePool(typeof(DullCopperIngot));
                    AddToResourcePool(typeof(ShadowIronIngot));
                    AddToResourcePool(typeof(CopperIngot));
                    AddToResourcePool(typeof(BronzeIngot));
                    AddToResourcePool(typeof(GoldIngot));
                    AddToResourcePool(typeof(AgapiteIngot));
                    AddToResourcePool(typeof(VeriteIngot));
                    AddToResourcePool(typeof(ValoriteIngot));
                    AddToResourcePool(typeof(IronOre));
                    AddToResourcePool(typeof(DullCopperOre));
                    AddToResourcePool(typeof(ShadowIronOre));
                    AddToResourcePool(typeof(CopperOre));
                    AddToResourcePool(typeof(BronzeOre));
                    AddToResourcePool(typeof(GoldOre));
                    AddToResourcePool(typeof(AgapiteOre));
                    AddToResourcePool(typeof(VeriteOre));
                    AddToResourcePool(typeof(ValoriteOre));
#endif
                }
                else if (Core.RuleSets.ShopkeepersBuyResourcesRules())
                {   // cash buyback
                    Add(typeof(Board), BaseVendor.VendorPays(typeof(Board)));
                    Add(typeof(Log), BaseVendor.VendorPays(typeof(Log)));
                    // according to RunUO 2.6, Tinkers don't buy IronIngot(s)
                }

                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(Drums), BaseVendor.VendorPays(typeof(Drums)));
                    Add(typeof(Tambourine), BaseVendor.VendorPays(typeof(Tambourine)));
                    Add(typeof(LapHarp), BaseVendor.VendorPays(typeof(LapHarp)));
                    Add(typeof(Lute), BaseVendor.VendorPays(typeof(Lute)));

                    Add(typeof(Shovel), BaseVendor.VendorPays(typeof(Shovel)));
                    Add(typeof(SewingKit), BaseVendor.VendorPays(typeof(SewingKit)));
                    Add(typeof(Scissors), BaseVendor.VendorPays(typeof(Scissors)));
                    Add(typeof(Tongs), BaseVendor.VendorPays(typeof(Tongs)));
                    Add(typeof(Key), BaseVendor.VendorPays(typeof(Key)));

                    Add(typeof(DovetailSaw), BaseVendor.VendorPays(typeof(DovetailSaw)));
                    Add(typeof(MouldingPlane), BaseVendor.VendorPays(typeof(MouldingPlane)));
                    Add(typeof(Nails), BaseVendor.VendorPays(typeof(Nails)));
                    Add(typeof(JointingPlane), BaseVendor.VendorPays(typeof(JointingPlane)));
                    Add(typeof(SmoothingPlane), BaseVendor.VendorPays(typeof(SmoothingPlane)));
                    Add(typeof(Saw), BaseVendor.VendorPays(typeof(Saw)));

                    Add(typeof(Clock), BaseVendor.VendorPays(typeof(Clock)));
                    Add(typeof(ClockParts), BaseVendor.VendorPays(typeof(ClockParts)));
                    Add(typeof(AxleGears), BaseVendor.VendorPays(typeof(AxleGears)));
                    Add(typeof(Gears), BaseVendor.VendorPays(typeof(Gears)));
                    Add(typeof(Hinge), BaseVendor.VendorPays(typeof(Hinge)));
                    Add(typeof(Sextant), BaseVendor.VendorPays(typeof(Sextant)));
                    Add(typeof(SextantParts), BaseVendor.VendorPays(typeof(SextantParts)));
                    Add(typeof(Axle), BaseVendor.VendorPays(typeof(Axle)));
                    Add(typeof(Springs), BaseVendor.VendorPays(typeof(Springs)));

                    Add(typeof(DrawKnife), BaseVendor.VendorPays(typeof(DrawKnife)));
                    Add(typeof(Froe), BaseVendor.VendorPays(typeof(Froe)));
                    Add(typeof(Inshave), BaseVendor.VendorPays(typeof(Inshave)));
                    Add(typeof(Scorp), BaseVendor.VendorPays(typeof(Scorp)));

                    Add(typeof(Lockpick), BaseVendor.VendorPays(typeof(Lockpick)));
                    Add(typeof(TinkerTools), BaseVendor.VendorPays(typeof(TinkerTools)));

                    Add(typeof(Pickaxe), BaseVendor.VendorPays(typeof(Pickaxe)));
                    Add(typeof(Hammer), BaseVendor.VendorPays(typeof(Hammer)));
                    Add(typeof(SmithHammer), BaseVendor.VendorPays(typeof(SmithHammer)));
                    Add(typeof(ButcherKnife), BaseVendor.VendorPays(typeof(ButcherKnife)));
                }
            }
        }
    }
}