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

/* Scripts/Mobiles/Vendors/SBInfo/SBHolyMage.cs
 * ChangeLog
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
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class SBHolyMage : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBHolyMage()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Type[] types = Loot.RegularScrollTypes;

                for (int i = 0; i < types.Length && i < 8; ++i)
                {
                    int itemID = 0x1F2E + i;

                    if (i == 6)
                        itemID = 0x1F2D;
                    else if (i > 6)
                        --itemID;

                    Add(new GenericBuyInfo(types[i], BaseVendor.PlayerPays(types[i]), 20, 20, 20, itemID, 0));
                }

                Add(new GenericBuyInfo(typeof(NightSightPotion), BaseVendor.PlayerPays(typeof(NightSightPotion)), 20, 0xF06, 0));
                Add(new GenericBuyInfo(typeof(AgilityPotion), BaseVendor.PlayerPays(typeof(AgilityPotion)), 20, 0xF08, 0));
                Add(new GenericBuyInfo(typeof(StrengthPotion), BaseVendor.PlayerPays(typeof(StrengthPotion)), 20, 0xF09, 0));
                Add(new GenericBuyInfo(typeof(RefreshPotion), BaseVendor.PlayerPays(typeof(RefreshPotion)), 20, 0xF0B, 0));
                Add(new GenericBuyInfo(typeof(LesserCurePotion), BaseVendor.PlayerPays(typeof(LesserCurePotion)), 20, 0xF07, 0));
                Add(new GenericBuyInfo(typeof(LesserHealPotion), BaseVendor.PlayerPays(typeof(LesserHealPotion)), 20, 0xF0C, 0));
                Add(new GenericBuyInfo(typeof(LesserPoisonPotion), BaseVendor.PlayerPays(typeof(LesserPoisonPotion)), 20, 0xF0A, 0));

                Add(new GenericBuyInfo(typeof(BlackPearl), BaseVendor.PlayerPays(typeof(BlackPearl)), 20, 0xF7A, 0));
                Add(new GenericBuyInfo(typeof(Bloodmoss), BaseVendor.PlayerPays(typeof(Bloodmoss)), 20, 0xF7B, 0));
                Add(new GenericBuyInfo(typeof(MandrakeRoot), BaseVendor.PlayerPays(typeof(MandrakeRoot)), 20, 0xF86, 0));
                Add(new GenericBuyInfo(typeof(Garlic), BaseVendor.PlayerPays(typeof(Garlic)), 20, 0xF84, 0));
                Add(new GenericBuyInfo(typeof(Ginseng), BaseVendor.PlayerPays(typeof(Ginseng)), 20, 0xF85, 0));
                Add(new GenericBuyInfo(typeof(Nightshade), BaseVendor.PlayerPays(typeof(Nightshade)), 20, 0xF88, 0));
                Add(new GenericBuyInfo(typeof(SpidersSilk), BaseVendor.PlayerPays(typeof(SpidersSilk)), 20, 0xF8D, 0));
                Add(new GenericBuyInfo(typeof(SulfurousAsh), BaseVendor.PlayerPays(typeof(SulfurousAsh)), 20, 0xF8C, 0));

                Add(new GenericBuyInfo("1041072", typeof(MagicWizardsHat), BaseVendor.PlayerPays(typeof(MagicWizardsHat)), 10, 0x1718, 0));

                Add(new GenericBuyInfo(typeof(RecallRune), BaseVendor.PlayerPays(typeof(RecallRune)), 10, 0x1f14, 0));
                Add(new GenericBuyInfo(typeof(Spellbook), BaseVendor.PlayerPays(typeof(Spellbook)), 10, 0xEFA, 0));

                Add(new GenericBuyInfo(typeof(ScribesPen), BaseVendor.PlayerPays(typeof(ScribesPen)), 10, 0xFBF, 0));
                Add(new GenericBuyInfo(typeof(BlankScroll), BaseVendor.PlayerPays(typeof(BlankScroll)), 20, 0x0E34, 0));
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
                    Add(typeof(RecallRune), BaseVendor.VendorPays(typeof(RecallRune)));
                    Add(typeof(Spellbook), BaseVendor.VendorPays(typeof(Spellbook)));
                    Add(typeof(BlankScroll), BaseVendor.VendorPays(typeof(BlankScroll)));

                    Add(typeof(NightSightPotion), BaseVendor.VendorPays(typeof(NightSightPotion)));
                    Add(typeof(AgilityPotion), BaseVendor.VendorPays(typeof(AgilityPotion)));
                    Add(typeof(StrengthPotion), BaseVendor.VendorPays(typeof(StrengthPotion)));
                    Add(typeof(RefreshPotion), BaseVendor.VendorPays(typeof(RefreshPotion)));
                    Add(typeof(LesserCurePotion), BaseVendor.VendorPays(typeof(LesserCurePotion)));
                    Add(typeof(LesserHealPotion), BaseVendor.VendorPays(typeof(LesserHealPotion)));
                    Add(typeof(LesserPoisonPotion), BaseVendor.VendorPays(typeof(LesserPoisonPotion)));

                    Type[] types = Loot.RegularScrollTypes;

                    for (int i = 0; i < types.Length; ++i)
                        Add(types[i], BaseVendor.VendorPays(types[i]));
                }

            }
        }
    }
}