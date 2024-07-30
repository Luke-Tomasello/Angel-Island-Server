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

/* Scripts/Mobiles/Vendors/SBInfo/SBCarpenter.cs
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
 * 11/23/21, Yoar
 *      Removed Sledgehammer, DemolitionAx from buy list.
 * 11/16/21, Yoar
 *      Integrated ML wood types into the BBS.
 *  10/13/21, Adam
 *      Added Tenjin's Saw. Tenjin's Saw is used for magic crafting for carpenters. 
 *  12/25/06, adam
 *      Added the library bookcase
 *  12/22/06, adam
 *      Added the library bookcase, but left it comment out; waiting on the ability to name the library bookcase
 *  08/03/06, Rhiannon
 *		Added display cases
 *	03/20/06, weaver
 *		Added trash barrels.
 *	03/10/05, erlein
 *	    Changed to lower case letters.
 *	03/10/05, erlein
 *		Added Square Graver & Wood Engraving book.
 *  01/28/05
 *		Added Logs
 *  01/23/05, Taran Kain
 *		Added boards.
 *  11/18/04, Jade
 *      Re-enabled the fountain deeds.
 *  11/17/04, Jade
 *      Commented out the fountain deeds, because they are bugged.
 *  11/16/04, Jade
 *      Added new fountain deeds to the inventory.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
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
    public class SBCarpenter : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();
        private static List<Type> BBSTypes = new List<Type>() { typeof(Board), typeof(OakBoard), typeof(AshBoard), typeof(YewBoard), typeof(HeartwoodBoard), typeof(BloodwoodBoard), typeof(FrostwoodBoard), typeof(Log), typeof(OakLog), typeof(AshLog), typeof(YewLog), typeof(HeartwoodLog), typeof(BloodwoodLog), typeof(FrostwoodLog) };

        public SBCarpenter()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeIIRules())
                {
                    // Jade: Add new fountain deeds (currently commented out because they are bugged)
                    // Adam: waiting on the ability to name the library bookcase
                    Add(new GenericBuyInfo("a library bookcase", typeof(Library), BaseVendor.PlayerPays(typeof(Library)), 20, 0xA97, 0));
                    Add(new GenericBuyInfo("wood engraving", typeof(WoodEngravingBook), BaseVendor.PlayerPays(typeof(WoodEngravingBook)), 20, 0xFF4, 0));
                    Add(new GenericBuyInfo("a stone fountain deed", typeof(StoneFountainDeed), BaseVendor.PlayerPays(typeof(StoneFountainDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a sandstone fountain deed", typeof(SandstoneFountainDeed), BaseVendor.PlayerPays(typeof(SandstoneFountainDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("square graver", typeof(SquareGraver), BaseVendor.PlayerPays(typeof(SquareGraver)), 20, 0x10E7, 0));
                    if (Core.RuleSets.AngelIslandRules())
                        Add(new GenericBuyInfo("trash barrel", typeof(TrashBarrel), BaseVendor.PlayerPays(typeof(TrashBarrel)), 20, 0xE77, 0));
                    Add(new GenericBuyInfo("a tiny display case deed", typeof(DisplayCaseTinyAddonDeed), BaseVendor.PlayerPays(typeof(DisplayCaseTinyAddonDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a small display case deed", typeof(DisplayCaseSmallAddonDeed), BaseVendor.PlayerPays(typeof(DisplayCaseSmallAddonDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a narrow display case deed", typeof(DisplayCaseNarrowAddonDeed), BaseVendor.PlayerPays(typeof(DisplayCaseNarrowAddonDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a medium display case deed", typeof(DisplayCaseMediumAddonDeed), BaseVendor.PlayerPays(typeof(DisplayCaseMediumAddonDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a square display case deed", typeof(DisplayCaseSquareAddonDeed), BaseVendor.PlayerPays(typeof(DisplayCaseSquareAddonDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a large display case deed", typeof(DisplayCaseLargeAddonDeed), BaseVendor.PlayerPays(typeof(DisplayCaseLargeAddonDeed)), 20, 0x14F0, 0));
                }

                Add(new GenericBuyInfo(typeof(Lute), BaseVendor.PlayerPays(typeof(Lute)), 20, 0xEB3, 0));
                Add(new GenericBuyInfo(typeof(LapHarp), BaseVendor.PlayerPays(typeof(LapHarp)), 20, 0xEB2, 0));
                Add(new GenericBuyInfo(typeof(Tambourine), BaseVendor.PlayerPays(typeof(Tambourine)), 20, 0xE9D, 0));
                Add(new GenericBuyInfo(typeof(Drums), BaseVendor.PlayerPays(typeof(Drums)), 20, 0xE9C, 0));
                Add(new GenericBuyInfo(typeof(JointingPlane), BaseVendor.PlayerPays(typeof(JointingPlane)), 20, 0x1030, 0));
                Add(new GenericBuyInfo(typeof(SmoothingPlane), BaseVendor.PlayerPays(typeof(SmoothingPlane)), 20, 0x1032, 0));
                Add(new GenericBuyInfo(typeof(MouldingPlane), BaseVendor.PlayerPays(typeof(MouldingPlane)), 20, 0x102C, 0));
                Add(new GenericBuyInfo(typeof(Hammer), BaseVendor.PlayerPays(typeof(Hammer)), 20, 0x102A, 0));
                Add(new GenericBuyInfo(typeof(Saw), BaseVendor.PlayerPays(typeof(Saw)), 20, 0x1034, 0));
                if (Core.RuleSets.AngelIslandRules())
                    Add(new GenericBuyInfo(typeof(TenjinsSaw), BaseVendor.PlayerPays(typeof(TenjinsSaw)), 20, 0x1034, 0x89F));
                Add(new GenericBuyInfo(typeof(DovetailSaw), BaseVendor.PlayerPays(typeof(DovetailSaw)), 20, 0x1028, 0));
                Add(new GenericBuyInfo(typeof(Inshave), BaseVendor.PlayerPays(typeof(Inshave)), 20, 0x10E6, 0));
                Add(new GenericBuyInfo(typeof(Scorp), BaseVendor.PlayerPays(typeof(Scorp)), 20, 0x10E7, 0));
                Add(new GenericBuyInfo(typeof(Froe), BaseVendor.PlayerPays(typeof(Froe)), 20, 0x10E5, 0));
                Add(new GenericBuyInfo(typeof(DrawKnife), BaseVendor.PlayerPays(typeof(DrawKnife)), 20, 0x10E4, 0));

                if (Core.RuleSets.ResourcePoolRules())
                {   // balanced buyback
                    foreach (Type type in BBSTypes)
                        if (ResourcePool.IsPooledResource(type))
                            Add(new GenericBuyInfo(type));
#if false
                    Add(new GenericBuyInfo(typeof(Board)));
                    Add(new GenericBuyInfo(typeof(OakBoard)));
                    Add(new GenericBuyInfo(typeof(AshBoard)));
                    Add(new GenericBuyInfo(typeof(YewBoard)));
                    Add(new GenericBuyInfo(typeof(HeartwoodBoard)));
                    Add(new GenericBuyInfo(typeof(BloodwoodBoard)));
                    Add(new GenericBuyInfo(typeof(FrostwoodBoard)));
#endif
                }
                else if (Core.RuleSets.ShopkeepersSellResourcesRules())
                    Add(new GenericBuyInfo(typeof(Board), BaseVendor.PlayerPays(typeof(Board)), 20, 0x1BD7, 0));

                Add(new GenericBuyInfo(typeof(Axle), BaseVendor.PlayerPays(typeof(Axle)), 20, 0x105B, 0));
                Add(new GenericBuyInfo(typeof(Nails), BaseVendor.PlayerPays(typeof(Nails)), 20, 0x102E, 0));

#if old
                if (Core.RuleSets.AngelIslandRules()  ||Core.RuleSets.SiegeStyleRules())
                {
                    Add(new GenericBuyInfo(typeof(Server.Township.Sledgehammer), Utility.Random(230, 260), 20, 0x1439, 0));
                    Add(new GenericBuyInfo(typeof(Server.Township.DemolitionAx), Utility.Random(230, 260), 20, 0x13FB, 0));
                }
#endif
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
                    AddToResourcePool(typeof(OakBoard));
                    AddToResourcePool(typeof(AshBoard));
                    AddToResourcePool(typeof(YewBoard));
                    AddToResourcePool(typeof(HeartwoodBoard));
                    AddToResourcePool(typeof(BloodwoodBoard));
                    AddToResourcePool(typeof(FrostwoodBoard));
                    AddToResourcePool(typeof(Log));
                    AddToResourcePool(typeof(OakLog));
                    AddToResourcePool(typeof(AshLog));
                    AddToResourcePool(typeof(YewLog));
                    AddToResourcePool(typeof(HeartwoodLog));
                    AddToResourcePool(typeof(BloodwoodLog));
                    AddToResourcePool(typeof(FrostwoodLog));
#endif
                }
                else if (Core.RuleSets.ShopkeepersBuyResourcesRules())
                {
                    Add(typeof(Board), BaseVendor.VendorPays(typeof(Board)));
                    Add(typeof(Log), BaseVendor.VendorPays(typeof(Log)));

                }

                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(WoodenBox), BaseVendor.VendorPays(typeof(WoodenBox)));
                    Add(typeof(SmallCrate), BaseVendor.VendorPays(typeof(SmallCrate)));
                    Add(typeof(MediumCrate), BaseVendor.VendorPays(typeof(MediumCrate)));
                    Add(typeof(LargeCrate), BaseVendor.VendorPays(typeof(LargeCrate)));
                    Add(typeof(WoodenChest), BaseVendor.VendorPays(typeof(WoodenChest)));

                    Add(typeof(LargeTable), BaseVendor.VendorPays(typeof(LargeTable)));
                    Add(typeof(Nightstand), BaseVendor.VendorPays(typeof(Nightstand)));
                    Add(typeof(YewWoodTable), BaseVendor.VendorPays(typeof(YewWoodTable)));

                    Add(typeof(Throne), BaseVendor.VendorPays(typeof(Throne)));
                    Add(typeof(WoodenThrone), BaseVendor.VendorPays(typeof(WoodenThrone)));
                    Add(typeof(Stool), BaseVendor.VendorPays(typeof(Stool)));
                    Add(typeof(FootStool), BaseVendor.VendorPays(typeof(FootStool)));

                    Add(typeof(FancyWoodenChairCushion), BaseVendor.VendorPays(typeof(FancyWoodenChairCushion)));
                    Add(typeof(WoodenChairCushion), BaseVendor.VendorPays(typeof(WoodenChairCushion)));
                    Add(typeof(WoodenChair), BaseVendor.VendorPays(typeof(WoodenChair)));
                    Add(typeof(BambooChair), BaseVendor.VendorPays(typeof(BambooChair)));
                    Add(typeof(WoodenBench), BaseVendor.VendorPays(typeof(WoodenBench)));

                    Add(typeof(Saw), BaseVendor.VendorPays(typeof(Saw)));
                    Add(typeof(Scorp), BaseVendor.VendorPays(typeof(Scorp)));
                    Add(typeof(SmoothingPlane), BaseVendor.VendorPays(typeof(SmoothingPlane)));
                    Add(typeof(DrawKnife), BaseVendor.VendorPays(typeof(DrawKnife)));
                    Add(typeof(Froe), BaseVendor.VendorPays(typeof(Froe)));
                    Add(typeof(Hammer), BaseVendor.VendorPays(typeof(Hammer)));
                    Add(typeof(Inshave), BaseVendor.VendorPays(typeof(Inshave)));
                    Add(typeof(JointingPlane), BaseVendor.VendorPays(typeof(JointingPlane)));
                    Add(typeof(MouldingPlane), BaseVendor.VendorPays(typeof(MouldingPlane)));
                    Add(typeof(DovetailSaw), BaseVendor.VendorPays(typeof(DovetailSaw)));
                    Add(typeof(Axle), BaseVendor.VendorPays(typeof(Axle)));
                    Add(typeof(WoodenShield), BaseVendor.VendorPays(typeof(WoodenShield)));
                    Add(typeof(BlackStaff), BaseVendor.VendorPays(typeof(BlackStaff)));
                    Add(typeof(GnarledStaff), BaseVendor.VendorPays(typeof(GnarledStaff)));
                    Add(typeof(QuarterStaff), BaseVendor.VendorPays(typeof(QuarterStaff)));
                    Add(typeof(ShepherdsCrook), BaseVendor.VendorPays(typeof(ShepherdsCrook)));
                    Add(typeof(Club), BaseVendor.VendorPays(typeof(Club)));
                    Add(typeof(Lute), BaseVendor.VendorPays(typeof(Lute)));
                    Add(typeof(LapHarp), BaseVendor.VendorPays(typeof(LapHarp)));
                    Add(typeof(Tambourine), BaseVendor.VendorPays(typeof(Tambourine)));
                    Add(typeof(Drums), BaseVendor.VendorPays(typeof(Drums)));
                }
            }
        }
    }
}