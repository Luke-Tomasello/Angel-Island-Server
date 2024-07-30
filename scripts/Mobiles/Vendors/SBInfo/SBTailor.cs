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

/* Scripts/Mobiles/Vendors/SBInfo/SBTailor.cs
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
 *	4/2/8, Pix
 *		Added Leather Embroidery Book/Floss
 *  10/15/05, erlein
 *     Added color swatches + fixed embroidery book text.
 *  2/10/05, erlein
 *     Removed finished clothing pieces.
 *  09/08/05, erlein
 *     Added embroidery floss + book.
 *  04/02/05 TK
 *		Added new cloth redirects, special leather types and hides
 *  01/28/05 TK
 *		Added BoltOfCloth, UncutCloth, Hides
 *  01/23/05, Taran Kain
 *		Added cloth and leather. Removed Bolt of Cloth (not handled by ResourcePool)
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
    public class SBTailor : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();
        private static List<Type> BBSTypes = new List<Type>() { typeof(Cloth), typeof(Leather), typeof(SpinedLeather), typeof(HornedLeather), typeof(BarbedLeather), typeof(BoltOfCloth), typeof(UncutCloth), typeof(Cotton), typeof(Wool), typeof(DarkYarn), typeof(LightYarn), typeof(LightYarnUnraveled), typeof(SpoolOfThread), typeof(Hides), typeof(SpinedHides), typeof(BarbedHides), typeof(HornedHides) };

        public SBTailor()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {

                Add(new GenericBuyInfo(typeof(Scissors), BaseVendor.PlayerPays(typeof(Scissors)), 20, 0xF9F, 0));
                Add(new GenericBuyInfo(typeof(SewingKit), BaseVendor.PlayerPays(typeof(SewingKit)), 20, 0xF9D, 0));
                Add(new GenericBuyInfo(typeof(Dyes), BaseVendor.PlayerPays(typeof(Dyes)), 20, 0xFA9, 0));
                Add(new GenericBuyInfo(typeof(DyeTub), BaseVendor.PlayerPays(typeof(DyeTub)), 20, 0xFAB, 0));

                if (Core.RuleSets.AngelIslandRules())
                {   // AI specials
                    Add(new GenericBuyInfo("embroidering", typeof(EmbroideryBook), BaseVendor.PlayerPays(typeof(EmbroideryBook)), 20, 0xFF4, 0));
                    Add(new GenericBuyInfo("leather embroidering", typeof(LeatherEmbroideryBook), BaseVendor.PlayerPays(typeof(LeatherEmbroideryBook)), 20, 0xFF4, 0));
                    Add(new GenericBuyInfo("embroidery floss", typeof(Embroidery), BaseVendor.PlayerPays(typeof(Embroidery)), 20, 0xFA0, 0));
                    Add(new GenericBuyInfo("heavy embroidery floss", typeof(LeatherEmbroideryFloss), BaseVendor.PlayerPays(typeof(LeatherEmbroideryFloss)), 20, 0x1420, 0));
                    Add(new GenericBuyInfo("color swatch", typeof(ColorSwatch), BaseVendor.PlayerPays(typeof(ColorSwatch)), 20, 0x175D, 0));
                    Add(new GenericBuyInfo(typeof(ClothingBlessDeed), BaseVendor.PlayerPays(typeof(ClothingBlessDeed)), 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(TenjinsNeedle), BaseVendor.PlayerPays(typeof(TenjinsNeedle)), 20, 0xF9D, CraftResources.GetHue(CraftResource.Verite)));
                }

                if (!Core.RuleSets.AngelIslandRules())
                {
                    // erl : removed for new clothing wear system to involve tailors directly in clothing
                    Add(new GenericBuyInfo(typeof(FancyShirt), BaseVendor.PlayerPays(typeof(FancyShirt)), 20, 0x1EFD, 0));
                    Add(new GenericBuyInfo(typeof(Shirt), BaseVendor.PlayerPays(typeof(Shirt)), 20, 0x1517, 0));

                    Add(new GenericBuyInfo(typeof(ShortPants), BaseVendor.PlayerPays(typeof(ShortPants)), 20, 0x152E, 0));
                    Add(new GenericBuyInfo(typeof(LongPants), BaseVendor.PlayerPays(typeof(LongPants)), 20, 0x1539, 0));

                    Add(new GenericBuyInfo(typeof(Cloak), BaseVendor.PlayerPays(typeof(Cloak)), 20, 0x1515, 0));
                    Add(new GenericBuyInfo(typeof(FancyDress), BaseVendor.PlayerPays(typeof(FancyDress)), 20, 0x1EFF, 0));
                    Add(new GenericBuyInfo(typeof(Robe), BaseVendor.PlayerPays(typeof(Robe)), 20, 0x1F03, 0));
                    Add(new GenericBuyInfo(typeof(PlainDress), BaseVendor.PlayerPays(typeof(PlainDress)), 20, 0x1F01, 0));

                    Add(new GenericBuyInfo(typeof(Skirt), BaseVendor.PlayerPays(typeof(Skirt)), 20, 0x1516, 0));
                    Add(new GenericBuyInfo(typeof(Kilt), BaseVendor.PlayerPays(typeof(Kilt)), 20, 0x1537, 0));

                    Add(new GenericBuyInfo(typeof(FullApron), BaseVendor.PlayerPays(typeof(FullApron)), 20, 0x153d, 0));
                    Add(new GenericBuyInfo(typeof(HalfApron), BaseVendor.PlayerPays(typeof(HalfApron)), 20, 0x153b, 0));

                    Add(new GenericBuyInfo(typeof(Doublet), BaseVendor.PlayerPays(typeof(Doublet)), 20, 0x1F7B, 0));
                    Add(new GenericBuyInfo(typeof(Tunic), BaseVendor.PlayerPays(typeof(Tunic)), 20, 0x1FA1, 0));
                    Add(new GenericBuyInfo(typeof(JesterSuit), BaseVendor.PlayerPays(typeof(JesterSuit)), 20, 0x1F9F, 0));

                    Add(new GenericBuyInfo(typeof(JesterHat), BaseVendor.PlayerPays(typeof(JesterHat)), 20, 0x171C, 0));
                    Add(new GenericBuyInfo(typeof(FloppyHat), BaseVendor.PlayerPays(typeof(FloppyHat)), 20, 0x1713, 0));
                    Add(new GenericBuyInfo(typeof(WideBrimHat), BaseVendor.PlayerPays(typeof(WideBrimHat)), 20, 0x1714, 0));
                    Add(new GenericBuyInfo(typeof(Cap), BaseVendor.PlayerPays(typeof(Cap)), 20, 0x1715, 0));
                    Add(new GenericBuyInfo(typeof(SkullCap), BaseVendor.PlayerPays(typeof(SkullCap)), 20, 0x1544, 0));
                    Add(new GenericBuyInfo(typeof(Bandana), BaseVendor.PlayerPays(typeof(Bandana)), 20, 0x1540, 0));
                    Add(new GenericBuyInfo(typeof(TallStrawHat), BaseVendor.PlayerPays(typeof(TallStrawHat)), 20, 0x1716, 0));
                    Add(new GenericBuyInfo(typeof(StrawHat), BaseVendor.PlayerPays(typeof(StrawHat)), 20, 0x1717, 0));
                    Add(new GenericBuyInfo(typeof(WizardsHat), BaseVendor.PlayerPays(typeof(WizardsHat)), 20, 0x1718, 0));
                    Add(new GenericBuyInfo(typeof(Bonnet), BaseVendor.PlayerPays(typeof(Bonnet)), 20, 0x1719, 0));
                    Add(new GenericBuyInfo(typeof(FeatheredHat), BaseVendor.PlayerPays(typeof(FeatheredHat)), 20, 0x171A, 0));
                    Add(new GenericBuyInfo(typeof(TricorneHat), BaseVendor.PlayerPays(typeof(TricorneHat)), 20, 0x171B, 0));
                }

                if (Core.RuleSets.ResourcePoolRules())
                {   // balanced buyback system
                    //Add( new GenericBuyInfo( typeof( Flax ) ) );
                    foreach (Type type in BBSTypes)
                        if (ResourcePool.IsPooledResource(type))
                            Add(new GenericBuyInfo(type));
#if false
                    Add(new GenericBuyInfo(typeof(Cloth)));
                    Add(new GenericBuyInfo(typeof(Leather)));
                    Add(new GenericBuyInfo(typeof(SpinedLeather)));
                    Add(new GenericBuyInfo(typeof(HornedLeather)));
                    Add(new GenericBuyInfo(typeof(BarbedLeather)));
#endif
                }
                else if (Core.RuleSets.ShopkeepersSellResourcesRules())
                {
                    Add(new GenericBuyInfo(typeof(BoltOfCloth), BaseVendor.PlayerPays(typeof(BoltOfCloth)), 20, 0xf95, Utility.RandomDyedHue()));

                    Add(new GenericBuyInfo(typeof(Cloth), BaseVendor.PlayerPays(typeof(Cloth)), 20, 0x1766, Utility.RandomDyedHue()));
                    Add(new GenericBuyInfo(typeof(UncutCloth), BaseVendor.PlayerPays(typeof(UncutCloth)), 20, 0x1767, Utility.RandomDyedHue()));

                    Add(new GenericBuyInfo(typeof(Cotton), BaseVendor.PlayerPays(typeof(Cotton)), 20, 0xDF9, 0));
                    Add(new GenericBuyInfo(typeof(Wool), BaseVendor.PlayerPays(typeof(Wool)), 20, 0xDF8, 0));
                    Add(new GenericBuyInfo(typeof(Flax), BaseVendor.PlayerPays(typeof(Flax)), 20, 0x1A9C, 0));
                    Add(new GenericBuyInfo(typeof(SpoolOfThread), BaseVendor.PlayerPays(typeof(SpoolOfThread)), 20, 0xFA0, 0));
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
                    AddToResourcePool(typeof(Cloth));
                    AddToResourcePool(typeof(BoltOfCloth));
                    AddToResourcePool(typeof(UncutCloth));
                    AddToResourcePool(typeof(Cotton));
                    AddToResourcePool(typeof(Wool));
                    AddToResourcePool(typeof(DarkYarn));
                    AddToResourcePool(typeof(LightYarn));
                    AddToResourcePool(typeof(LightYarnUnraveled));
                    AddToResourcePool(typeof(SpoolOfThread));
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
                else if (Core.RuleSets.ShopkeepersSellResourcesRules())
                {
                    Add(typeof(BoltOfCloth), BaseVendor.VendorPays(typeof(BoltOfCloth)));
                    Add(typeof(SpoolOfThread), BaseVendor.VendorPays(typeof(SpoolOfThread)));
                    Add(typeof(Cotton), BaseVendor.VendorPays(typeof(Cotton)));
                    Add(typeof(Wool), BaseVendor.VendorPays(typeof(Wool)));
                }

                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(Scissors), BaseVendor.VendorPays(typeof(Scissors)));
                    Add(typeof(SewingKit), BaseVendor.VendorPays(typeof(SewingKit)));
                    Add(typeof(Dyes), BaseVendor.VendorPays(typeof(Dyes)));
                    Add(typeof(DyeTub), BaseVendor.VendorPays(typeof(DyeTub)));

                    Add(typeof(FancyShirt), BaseVendor.VendorPays(typeof(FancyShirt)));
                    Add(typeof(Shirt), BaseVendor.VendorPays(typeof(Shirt)));

                    Add(typeof(ShortPants), BaseVendor.VendorPays(typeof(ShortPants)));
                    Add(typeof(LongPants), BaseVendor.VendorPays(typeof(LongPants)));

                    Add(typeof(Cloak), BaseVendor.VendorPays(typeof(Cloak)));
                    Add(typeof(FancyDress), BaseVendor.VendorPays(typeof(FancyDress)));
                    Add(typeof(Robe), BaseVendor.VendorPays(typeof(Robe)));
                    Add(typeof(PlainDress), BaseVendor.VendorPays(typeof(PlainDress)));

                    Add(typeof(Skirt), BaseVendor.VendorPays(typeof(Skirt)));
                    Add(typeof(Kilt), BaseVendor.VendorPays(typeof(Kilt)));

                    Add(typeof(Doublet), BaseVendor.VendorPays(typeof(Doublet)));
                    Add(typeof(Tunic), BaseVendor.VendorPays(typeof(Tunic)));
                    Add(typeof(JesterSuit), BaseVendor.VendorPays(typeof(JesterSuit)));

                    Add(typeof(FullApron), BaseVendor.VendorPays(typeof(FullApron)));
                    Add(typeof(HalfApron), BaseVendor.VendorPays(typeof(HalfApron)));

                    Add(typeof(JesterHat), BaseVendor.VendorPays(typeof(JesterHat)));
                    Add(typeof(FloppyHat), BaseVendor.VendorPays(typeof(FloppyHat)));
                    Add(typeof(WideBrimHat), BaseVendor.VendorPays(typeof(WideBrimHat)));
                    Add(typeof(Cap), BaseVendor.VendorPays(typeof(Cap)));
                    Add(typeof(SkullCap), BaseVendor.VendorPays(typeof(SkullCap)));
                    Add(typeof(Bandana), BaseVendor.VendorPays(typeof(Bandana)));
                    Add(typeof(TallStrawHat), BaseVendor.VendorPays(typeof(TallStrawHat)));
                    Add(typeof(StrawHat), BaseVendor.VendorPays(typeof(StrawHat)));
                    Add(typeof(WizardsHat), BaseVendor.VendorPays(typeof(WizardsHat)));
                    Add(typeof(Bonnet), BaseVendor.VendorPays(typeof(Bonnet)));
                    Add(typeof(FeatheredHat), BaseVendor.VendorPays(typeof(FeatheredHat)));
                    Add(typeof(TricorneHat), BaseVendor.VendorPays(typeof(TricorneHat)));

                    Add(typeof(Flax), BaseVendor.VendorPays(typeof(Flax)));
                }
            }
        }
    }
}