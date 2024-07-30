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

/* Scripts/Mobiles/Vendors/SBInfo/SBAlchemist.cs
 * ChangeLog
 *  7/6/2023, Adam
 *      Limit the min/max stock of empty bottles to 110/999 for non Angel Island shards
 *  7/2/23, Yoar
 *  Set the min/max stock of empty bottles to 110/999
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
 *  10/10/21, Adam
 *      Return reagents to larger quanties .. 
 *      having to shop for reagents just seems to piss-off players, and I can't think of a good justification for it atm.
 *  9/20/21, Yoar
 *      Added Runebook Dye Tubs to the buy info for 25k each.
 *  10/14/04, Froste
 *      Changed the amount argument to GenericBuyInfo from 999 to 20 for reagents, so the argument means something in GenericBuy.cs *      
 *  10/3/04, Jade
 *      Added Potion Keg Dye Tubs to the Sell List for 5k each.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBAlchemist : SBInfo
    {
        public static int BottlesStockMin { get { return !Core.RuleSets.AngelIslandRules() ? 110 : 100; } }
        public static int BottlesStockMax { get { return !Core.RuleSets.AngelIslandRules() ? 999 : 100; } }

        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBAlchemist()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(BlackPearl), BaseVendor.PlayerPays(typeof(BlackPearl)), SBMage.ReagentStockMin, SBMage.ReagentStockMin, SBMage.ReagentStockMax, 0xF7A, 0));
                Add(new GenericBuyInfo(typeof(Bloodmoss), BaseVendor.PlayerPays(typeof(Bloodmoss)), SBMage.ReagentStockMin, SBMage.ReagentStockMin, SBMage.ReagentStockMax, 0xF7B, 0));
                Add(new GenericBuyInfo(typeof(MandrakeRoot), BaseVendor.PlayerPays(typeof(MandrakeRoot)), SBMage.ReagentStockMin, SBMage.ReagentStockMin, SBMage.ReagentStockMax, 0xF86, 0));
                Add(new GenericBuyInfo(typeof(Garlic), BaseVendor.PlayerPays(typeof(Garlic)), SBMage.ReagentStockMin, SBMage.ReagentStockMin, SBMage.ReagentStockMax, 0xF84, 0));
                Add(new GenericBuyInfo(typeof(Ginseng), BaseVendor.PlayerPays(typeof(Ginseng)), SBMage.ReagentStockMin, SBMage.ReagentStockMin, SBMage.ReagentStockMax, 0xF85, 0));
                Add(new GenericBuyInfo(typeof(Nightshade), BaseVendor.PlayerPays(typeof(Nightshade)), SBMage.ReagentStockMin, SBMage.ReagentStockMin, SBMage.ReagentStockMax, 0xF88, 0));
                Add(new GenericBuyInfo(typeof(SpidersSilk), BaseVendor.PlayerPays(typeof(SpidersSilk)), SBMage.ReagentStockMin, SBMage.ReagentStockMin, SBMage.ReagentStockMax, 0xF8D, 0));
                Add(new GenericBuyInfo(typeof(SulfurousAsh), BaseVendor.PlayerPays(typeof(SulfurousAsh)), SBMage.ReagentStockMin, SBMage.ReagentStockMin, SBMage.ReagentStockMax, 0xF8C, 0));

                Add(new GenericBuyInfo(typeof(Bottle), BaseVendor.PlayerPays(typeof(Bottle)), BottlesStockMin, BottlesStockMin, BottlesStockMax, 0xF0E, 0));

                Add(new GenericBuyInfo("1041060", typeof(HairDye), BaseVendor.PlayerPays(typeof(HairDye)), 10, 0xEFF, 0));

                Add(new GenericBuyInfo(typeof(MortarPestle), BaseVendor.PlayerPays(typeof(MortarPestle)), 10, 0xE9B, 0));

                // Adam: add the appropriate exclusion rule :P
                if (Core.RuleSets.AngelIslandRules())
                {
                    //Jade: Add potion keg dye tub
                    Add(new GenericBuyInfo("Potion Keg Dye Tub", typeof(PotionKegDyeTub), BaseVendor.PlayerPays(typeof(PotionKegDyeTub)), 20, 0xFAB, 0));
                    //Yoar: Add runebook dye tub
                    Add(new GenericBuyInfo("Runebook Dye Tub", typeof(RunebookDyeTub), BaseVendor.PlayerPays(typeof(RunebookDyeTub)), 20, 0xFAB, 0));
                }

                Add(new GenericBuyInfo(typeof(NightSightPotion), BaseVendor.PlayerPays(typeof(NightSightPotion)), 10, 0xF06, 0));
                Add(new GenericBuyInfo(typeof(AgilityPotion), BaseVendor.PlayerPays(typeof(AgilityPotion)), 10, 0xF08, 0));
                Add(new GenericBuyInfo(typeof(StrengthPotion), BaseVendor.PlayerPays(typeof(StrengthPotion)), 10, 0xF09, 0));
                Add(new GenericBuyInfo(typeof(RefreshPotion), BaseVendor.PlayerPays(typeof(RefreshPotion)), 10, 0xF0B, 0));
                Add(new GenericBuyInfo(typeof(LesserCurePotion), BaseVendor.PlayerPays(typeof(LesserCurePotion)), 10, 0xF07, 0));
                Add(new GenericBuyInfo(typeof(LesserHealPotion), BaseVendor.PlayerPays(typeof(LesserHealPotion)), 10, 0xF0C, 0));
                Add(new GenericBuyInfo(typeof(LesserPoisonPotion), BaseVendor.PlayerPays(typeof(LesserPoisonPotion)), 10, 0xF0A, 0));
                Add(new GenericBuyInfo(typeof(LesserExplosionPotion), BaseVendor.PlayerPays(typeof(LesserExplosionPotion)), 10, 0xF0D, 0));

                //Add(new GenericBuyInfo(typeof(CrystallineDullCopper), new CrystallineDullCopper().Cost, 10, 0x1005, new CrystallineDullCopper().Hue));
                //Add(new GenericBuyInfo(typeof(CrystallineShadowIron), new CrystallineShadowIron().Cost, 10, 0x1005, new CrystallineShadowIron().Hue));
                //Add(new GenericBuyInfo(typeof(CrystallineCopper), new CrystallineCopper().Cost, 10, 0x1005, new CrystallineCopper().Hue));
                //Add(new GenericBuyInfo(typeof(CrystallineBronze), new CrystallineBronze().Cost, 10, 0x1005, new CrystallineBronze().Hue));
                //Add(new GenericBuyInfo(typeof(CrystallineGold), new CrystallineGold().Cost, 10, 0x1005, new CrystallineGold().Hue));
                //Add(new GenericBuyInfo(typeof(CrystallineAgapite), new CrystallineAgapite().Cost, 10, 0x1005, new CrystallineAgapite().Hue));
                //Add(new GenericBuyInfo(typeof(CrystallineVerite), new CrystallineVerite().Cost, 10, 0x1005, new CrystallineVerite().Hue));
                //Add(new GenericBuyInfo(typeof(CrystallineValorite), new CrystallineValorite().Cost, 10, 0x1005, new CrystallineValorite().Hue));

            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(BlackPearl), BaseVendor.VendorPays(typeof(BlackPearl)));
                    Add(typeof(Bloodmoss), BaseVendor.VendorPays(typeof(Bloodmoss)));
                    Add(typeof(MandrakeRoot), BaseVendor.VendorPays(typeof(MandrakeRoot)));
                    Add(typeof(Garlic), BaseVendor.VendorPays(typeof(Garlic)));
                    Add(typeof(Ginseng), BaseVendor.VendorPays(typeof(Ginseng)));
                    Add(typeof(Nightshade), BaseVendor.VendorPays(typeof(Nightshade)));
                    Add(typeof(SpidersSilk), BaseVendor.VendorPays(typeof(SpidersSilk)));
                    Add(typeof(SulfurousAsh), BaseVendor.VendorPays(typeof(SulfurousAsh)));
                    Add(typeof(Bottle), BaseVendor.VendorPays(typeof(Bottle)));
                    Add(typeof(MortarPestle), BaseVendor.VendorPays(typeof(MortarPestle)));
                    Add(typeof(HairDye), BaseVendor.VendorPays(typeof(HairDye)));

                    Add(typeof(NightSightPotion), BaseVendor.VendorPays(typeof(NightSightPotion)));
                    Add(typeof(AgilityPotion), BaseVendor.VendorPays(typeof(AgilityPotion)));
                    Add(typeof(StrengthPotion), BaseVendor.VendorPays(typeof(StrengthPotion)));
                    Add(typeof(RefreshPotion), BaseVendor.VendorPays(typeof(RefreshPotion)));
                    Add(typeof(LesserCurePotion), BaseVendor.VendorPays(typeof(LesserCurePotion)));
                    Add(typeof(LesserHealPotion), BaseVendor.VendorPays(typeof(LesserHealPotion)));
                    Add(typeof(LesserPoisonPotion), BaseVendor.VendorPays(typeof(LesserPoisonPotion)));
                    Add(typeof(LesserExplosionPotion), BaseVendor.VendorPays(typeof(LesserExplosionPotion)));
                }
            }
        }
    }
}