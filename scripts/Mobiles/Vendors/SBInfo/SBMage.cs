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

/* Scripts/Mobiles/Vendors/SBInfo/SBMage.cs
 * ChangeLog
 *  7/6/2023, Adam
 *      Limit the min/max stock of regs to 110/999 for non Angel Island shards
 *  7/2/23, Yoar
 *      Set the min/max stock of regs to 110/999
 *  2/15/23, Adam
 *      Update to use the StandardPricingDictionary for scrolls.
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
 *  10/1/21, Adam
 *      Return reagents to larger quanties .. 
 *      having to shop for reagents just seems to piss-off players, and I can't think of a good justification for it atm.
 *  10/14/04, Froste
 *      Changed the amount argument to GenericBuyInfo from 999 to 20 for reagents, so the argument means something in GenericBuy.cs
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/22/04, mith
 *		Modified so that Mages only sell up to level 3 scrolls.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class SBMage : SBInfo
    {
        public static int ReagentStockMin { get { return !Core.RuleSets.AngelIslandRules() ? 110 : 110; } }
        public static int ReagentStockMax { get { return !Core.RuleSets.AngelIslandRules() ? 999 : 150; } }

        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBMage()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Type[] types = Loot.RegularScrollTypes;

                int circles = 3;

                for (int i = 0; i < circles * 8 && i < types.Length; ++i)
                {
                    int itemID = 0x1F2E + i;

                    if (i == 6)
                        itemID = 0x1F2D;
                    else if (i > 6)
                        --itemID;

                    Add(new GenericBuyInfo(types[i], BaseVendor.PlayerPays(types[i]), 20, 20, 20, itemID, 0));
                }

                Add(new GenericBuyInfo(typeof(BlackPearl), BaseVendor.PlayerPays(typeof(BlackPearl)), ReagentStockMin, ReagentStockMin, ReagentStockMax, 0xF7A, 0));
                Add(new GenericBuyInfo(typeof(Bloodmoss), BaseVendor.PlayerPays(typeof(Bloodmoss)), ReagentStockMin, ReagentStockMin, ReagentStockMax, 0xF7B, 0));
                Add(new GenericBuyInfo(typeof(MandrakeRoot), BaseVendor.PlayerPays(typeof(MandrakeRoot)), ReagentStockMin, ReagentStockMin, ReagentStockMax, 0xF86, 0));
                Add(new GenericBuyInfo(typeof(Garlic), BaseVendor.PlayerPays(typeof(Garlic)), ReagentStockMin, ReagentStockMin, ReagentStockMax, 0xF84, 0));
                Add(new GenericBuyInfo(typeof(Ginseng), BaseVendor.PlayerPays(typeof(Ginseng)), ReagentStockMin, ReagentStockMin, ReagentStockMax, 0xF85, 0));
                Add(new GenericBuyInfo(typeof(Nightshade), BaseVendor.PlayerPays(typeof(Nightshade)), ReagentStockMin, ReagentStockMin, ReagentStockMax, 0xF88, 0));
                Add(new GenericBuyInfo(typeof(SpidersSilk), BaseVendor.PlayerPays(typeof(SpidersSilk)), ReagentStockMin, ReagentStockMin, ReagentStockMax, 0xF8D, 0));
                Add(new GenericBuyInfo(typeof(SulfurousAsh), BaseVendor.PlayerPays(typeof(SulfurousAsh)), ReagentStockMin, ReagentStockMin, ReagentStockMax, 0xF8C, 0));

                if (Core.RuleSets.AOSRules())
                {
                    Add(new GenericBuyInfo(typeof(BatWing), BaseVendor.PlayerPays(typeof(BatWing)), 20, 0xF78, 0));
                    Add(new GenericBuyInfo(typeof(GraveDust), BaseVendor.PlayerPays(typeof(GraveDust)), 20, 0xF8F, 0));
                    Add(new GenericBuyInfo(typeof(DaemonBlood), BaseVendor.PlayerPays(typeof(DaemonBlood)), 20, 0xF7D, 0));
                    Add(new GenericBuyInfo(typeof(NoxCrystal), BaseVendor.PlayerPays(typeof(NoxCrystal)), 20, 0xF8E, 0));
                    Add(new GenericBuyInfo(typeof(PigIron), BaseVendor.PlayerPays(typeof(PigIron)), 20, 0xF8A, 0));

                    Add(new GenericBuyInfo(typeof(NecromancerSpellbook), BaseVendor.PlayerPays(typeof(NecromancerSpellbook)), 10, 0x2253, 0));
                }

                Add(new GenericBuyInfo("1041072", typeof(MagicWizardsHat), BaseVendor.PlayerPays(typeof(MagicWizardsHat)), 10, 0x1718, 0));

                // sold by players 
                if (Core.RuleSets.StandardShardRules() && false)
                    Add(new GenericBuyInfo("1041267", typeof(Runebook), BaseVendor.PlayerPays(typeof(Runebook)), 10, 0xEFA, 0x461));

                Add(new GenericBuyInfo(typeof(RecallRune), BaseVendor.PlayerPays(typeof(RecallRune)), 10, 0x1F14, 0));

                if (Core.RuleSets.AngelIslandRules())
                {
                    // the text "An Unmarked Moonstone" does not show. Figure out how Taran did the BBS Types (see GetBunchType in BaseVendor.cs)
                    Add(new GenericBuyInfo("An Unmarked Moonstone", typeof(Moonstone), BaseVendor.PlayerPays(typeof(Moonstone)), 100, 0xF8B, 0));
                    Add(new GenericBuyInfo(typeof(PowderOfTranslocation), BaseVendor.PlayerPays(typeof(PowderOfTranslocation)), 100, 0x26B8, 0));
                    Add(new GenericBuyInfo(typeof(Stonebag), BaseVendor.PlayerPays(typeof(Stonebag)), 10, 0xE76, 0));
                }

                Add(new GenericBuyInfo(typeof(Spellbook), BaseVendor.PlayerPays(typeof(Spellbook)), 10, 0xEFA, 0));

                Add(new GenericBuyInfo(typeof(ScribesPen), BaseVendor.PlayerPays(typeof(ScribesPen)), 10, 0xFBF, 0));
                Add(new GenericBuyInfo(typeof(BlankScroll), BaseVendor.PlayerPays(typeof(BlankScroll)), SBScribe.ScrollStockMin, SBScribe.ScrollStockMin, SBScribe.ScrollStockMax, 0x0E34, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    if (false)
                        // sold by players
                        Add(typeof(Runebook), BaseVendor.VendorPays(typeof(Runebook)));
                    Add(typeof(BlackPearl), BaseVendor.VendorPays(typeof(BlackPearl)));
                    Add(typeof(Bloodmoss), BaseVendor.VendorPays(typeof(Bloodmoss)));
                    Add(typeof(MandrakeRoot), BaseVendor.VendorPays(typeof(MandrakeRoot)));
                    Add(typeof(Garlic), BaseVendor.VendorPays(typeof(Garlic)));
                    Add(typeof(Ginseng), BaseVendor.VendorPays(typeof(Ginseng)));
                    Add(typeof(Nightshade), BaseVendor.VendorPays(typeof(Nightshade)));
                    Add(typeof(SpidersSilk), BaseVendor.VendorPays(typeof(SpidersSilk)));
                    Add(typeof(SulfurousAsh), BaseVendor.VendorPays(typeof(SulfurousAsh)));
                    Add(typeof(RecallRune), BaseVendor.VendorPays(typeof(RecallRune)));
                    Add(typeof(Spellbook), BaseVendor.VendorPays(typeof(Spellbook)));
                    Add(typeof(BlankScroll), BaseVendor.VendorPays(typeof(BlankScroll)));
                }

                if (Core.RuleSets.AOSRules())
                {   // cash buyback
                    Add(typeof(BatWing), BaseVendor.VendorPays(typeof(BatWing)));
                    Add(typeof(GraveDust), BaseVendor.VendorPays(typeof(GraveDust)));
                    Add(typeof(DaemonBlood), BaseVendor.VendorPays(typeof(DaemonBlood)));
                    Add(typeof(NoxCrystal), BaseVendor.VendorPays(typeof(NoxCrystal)));
                    Add(typeof(PigIron), BaseVendor.VendorPays(typeof(PigIron)));
                }

                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Type[] types = Loot.RegularScrollTypes;

                    for (int i = 0; i < types.Length; ++i)
                        Add(types[i], BaseVendor.VendorPays(types[i]));
                }
            }
        }
    }
}