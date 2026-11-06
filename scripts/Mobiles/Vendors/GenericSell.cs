/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts/Mobiles/Vendors/GenericSell.cs
 * Changelog
 *  10/21/2026, Adam (StoreBought provenance, generic)
 *      The Mortalis-style shop-goods refusal and its Dough/BreadLoaf exception list are replaced by the derived
 *      ArbitrageTable: GetSellPriceFor caps marked goods on an NPC arbitrage route at a safe price (normal price
 *      otherwise; GetNominalSellPriceFor ignores provenance), and IsSellable refuses them when that price is 0.
 *  10/19/2026, Adam (StoreBought provenance)
 *      IsSellable: on Mortalis, Seasons and Upheaval, NPCs no longer buy back shop goods or what was merely cut
 *      from them; crafted goods are bought at full price, except Dough and BreadLoaf made from shop flour.
 *      See IsRestrictedShopGoods.
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
 * 11/16/21, Yoar
 *      BBS overhaul. Moved most code related to the BBS to ResourcePool.cs.
 *	01/23/05	Taran Kain
 *		Added logic to support Resource Pool.
 */

using Server.Engines.ResourcePool;
using Server.Items;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class GenericSellInfo : IShopSellInfo
    {
        private Hashtable m_Table = new Hashtable();
        public Hashtable Table { get { return m_Table; } }
        private ArrayList m_MyTypes = new ArrayList();
        private Type[]? m_Types;

        public GenericSellInfo()
        {
        }

        public void Add(Type type, int price)
        {
            //if (BaseVendor.VendorPays(type) != 0)
            //{
            //    Debug.Assert(price == BaseVendor.VendorPays(type));
            //    Debug.Assert(price < BaseVendor.PlayerPays(type));
            //}

            m_Table[type] = price;
            m_MyTypes.Add(type);
            m_Types = null;
        }

        // ResourcePool handle
        public void AddToResourcePool(Type type)
        {
            if (!Core.RuleSets.ResourcePoolRules())
            {
                Utility.Monitor.WriteLine("ResourcePool Error: {0} is not a pooled resource.", ConsoleColor.Red, (type != null) ? type.FullName : "(null)");
            }

            if (ResourcePool.IsPooledResource(type, true))
            {
                Add(type, -1);

                if (m_MyTypes.IndexOf(typeof(CommodityDeed)) == -1)
                    Add(typeof(CommodityDeed), 0);
            }
            else
            {
                Utility.Monitor.WriteLine("ResourcePool Error: {0} is not a pooled resource.", ConsoleColor.Red, (type != null) ? type.FullName : "(null)");
            }
        }
        // NOTE:
        // This sell-price logic is not strict OSI Publish 15 behavior.
        // It intentionally rewards attributed/tiered weapons and armor.
        //
        // Design rationale:
        // - Attributed items are intentionally rare (no runics, no mass-production paths).
        // - Acquisition requires both skill progression and active adventuring.
        // - Risk, time, and gameplay are the gating factors, not automation or crafting loops.
        // - Vendor payout is treated as a reward for playing the game, not a primary gold faucet.
        //
        // Economic control is enforced via scarcity and drop distribution, not via vendor math.
        //
        // What the vendor pays: the normal price, or less for marked (StoreBought) goods on an NPC arbitrage route
        //  (ArbitrageTable.CapBuyback). Use GetNominalSellPriceFor for the price regardless of provenance.
        public int GetSellPriceFor(Item item)
        {
            return Server.Engines.Economy.ArbitrageTable.CapBuyback(item, GetNominalSellPriceFor(item));
        }

        public int GetNominalSellPriceFor(Item item)
        {
            int price = (int)m_Table[item.GetType()];

            if (item is BaseArmor)
            {
                BaseArmor armor = (BaseArmor)item;

                if (armor.Quality == ArmorQuality.Low)
                    price = (int)(price * 0.60);
                else if (armor.Quality == ArmorQuality.Exceptional)
                    price = (int)(price * 1.25);

                price += 100 * (int)armor.DurabilityLevel;
                price += 100 * (int)armor.ProtectionLevel;

                price = ApplyConditionMultiplier(price, armor.HitPoints, armor.MaxHitPoints);

                if (price < 1)
                    price = 1;
            }
            else if (item is BaseWeapon)
            {
                BaseWeapon weapon = (BaseWeapon)item;

                if (weapon.Quality == WeaponQuality.Low)
                    price = (int)(price * 0.60);
                else if (weapon.Quality == WeaponQuality.Exceptional)
                    price = (int)(price * 1.25);

                price += 100 * (int)weapon.DurabilityLevel;
                price += 100 * (int)weapon.DamageLevel;

                price = ApplyConditionMultiplier(price, weapon.HitPoints, weapon.MaxHitPoints);

                if (price < 1)
                    price = 1;
            }
            else if (item is BaseBeverage)
            {
                int price1 = price, price2 = price;

                if (item is Pitcher)
                {
                    price1 = 3;
                    price2 = 5;
                }
                else if (item is BeverageBottle)
                {
                    price1 = 3;
                    price2 = 3;
                }
                else if (item is Jug)
                {
                    price1 = 6;
                    price2 = 6;
                }

                BaseBeverage bev = (BaseBeverage)item;

                if (bev.IsEmpty || bev.Content == BeverageType.Milk)
                    price = price1;
                else
                    price = price2;
            }

            return price;
        }

        private static int ApplyConditionMultiplier(int price, int hitPoints, int maxHitPoints)
        {
            if (price <= 0)
                return 0;

            if (maxHitPoints <= 0)
                return price;

            if (hitPoints < 0)
                hitPoints = 0;
            else if (hitPoints > maxHitPoints)
                hitPoints = maxHitPoints;

            double ratio = (double)hitPoints / maxHitPoints;

            return (int)(price * ratio);
        }

        public int GetBuyPriceFor(Item item)
        {
            return (int)(1.90 * GetSellPriceFor(item));
        }

        public Type[] Types
        {
            get
            {
                if (m_Types == null)
                    m_Types = (Type[])m_MyTypes.ToArray(typeof(Type));

                return m_Types;
            }
        }

        public string GetNameFor(Item item)
        {
            if (item.Name != null)
                return item.Name;
            else
                return item.LabelNumber.ToString();
        }

        #region Shop goods (StoreBought provenance)
        /* NPC-funded arbitrage: a player buys from an NPC, converts with little or no skill, and sells to an NPC at a
         *  profit. Items bought from an NPC are marked StoreBought and the mark follows them through stacking and every
         *  registered conversion. Server.Engines.Economy.ArbitrageTable derives, from vendor stock and buyback, recipes
         *  and conversions, which types lie on such a route and the most an NPC may pay for marked goods of those types
         *  without the route paying (0: refused). Unmarked goods, and marked goods of other types, sell at full price.
         *  Prices are unchanged. Applies where ArbitrageTable.Enforced (Mortalis, Seasons, Upheaval).
         */
        public static bool IsRefusedShopGoods(Item item)
        {
            return item != null && Server.Engines.Economy.ArbitrageTable.RefusesBuyback(item);
        }

        /// <summary>
        /// Marked goods this vendor would pay less for than the normal price.
        /// </summary>
        public bool IsReducedShopGoods(Item item)
        {
            return item != null && !IsRefusedShopGoods(item) && GetSellPriceFor(item) < GetNominalSellPriceFor(item);
        }
        #endregion Shop goods (StoreBought provenance)

        public bool IsSellable(Item item)
        {
            //if ( item.Hue != 0 )
            //return false;

            if (IsRefusedShopGoods(item))
                return false;

            if (item is CommodityDeed cd)
            {
                if (Core.RuleSets.ResourcePoolRules())
                {
                    if (cd.Commodity != null)
                        return IsInList(cd.Commodity.GetType());
                }
                // if it's not acceptable to the ResourcePool, we don't want it
                return false;
            }

            return IsInList(item.GetType());
        }

        public bool IsResellable(Item item)
        {
            //if ( item.Hue != 0 )
            //return false;

            if (item is CommodityDeed cd)
            {
                if (Core.RuleSets.ResourcePoolRules())
                {
                    if (cd.Commodity != null)
                        return IsInList(cd.Commodity.GetType());
                }
                // if it's not acceptable to the ResourcePool, we don't want it
                return false;
            }

            return IsInList(item.GetType());
        }

        public bool IsInList(Type type)
        {
            Object? o = m_Table[type];

            // price < 0 is ResourcePool, > 0 is regular item, and 0 is an unwanted CommodityDeed
            if (o == null || o is int price && price == 0)
                return false;
            else
                return true;
        }
    }
}