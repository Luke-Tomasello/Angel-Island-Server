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

/* Scripts/Mobiles/Vendors/GenericSell.cs
 * Changelog
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

namespace Server.Mobiles
{
    public class GenericSellInfo : IShopSellInfo
    {
        private Hashtable m_Table = new Hashtable();
        public Hashtable Table { get { return m_Table; } }
        private ArrayList m_MyTypes = new ArrayList();
        private Type[] m_Types;

        public GenericSellInfo()
        {
        }

        public void Add(Type type, int price)
        {
#if false
            if (Core.RuleSets.SiegeStyleRules())
            {
                bool SiegeBuyOK = false;
                if (Core.RuleSets.ResourcePoolRules())
                {
                    if (ResourcePool.IsPooledResource(type, true) && price == -1)
                        SiegeBuyOK = true;
                    else if (type == typeof(CommodityDeed) && price == 0)
                        SiegeBuyOK = true;
                }
                if (SiegeBuyOK == false)
                    Utility.ConsoleOut("GenericSell: Why are we buying things back on Siege?", ConsoleColor.Red);
            }
#endif
            m_Table[type] = price;
            m_MyTypes.Add(type);
            m_Types = null;
        }

        // ResourcePool handle
        public void AddToResourcePool(Type type)
        {
            if (!Core.RuleSets.ResourcePoolRules())
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("ResourcePool Error: {0} is not a pooled resource.", (type != null) ? type.FullName : "(null)");
                Utility.PopColor();
            }

            if (ResourcePool.IsPooledResource(type, true))
            {
                Add(type, -1);

                if (m_MyTypes.IndexOf(typeof(CommodityDeed)) == -1)
                    Add(typeof(CommodityDeed), 0);
            }
            else
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("ResourcePool Error: {0} is not a pooled resource.", (type != null) ? type.FullName : "(null)");
                Utility.PopColor();
            }
        }

        public int GetSellPriceFor(Item item)
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

                if (price < 1)
                    price = 1;
            }
            else if (item is BaseBeverage)
            {
                int price1 = price, price2 = price;

                if (item is Pitcher)
                { price1 = 3; price2 = 5; }
                else if (item is BeverageBottle)
                { price1 = 3; price2 = 3; }
                else if (item is Jug)
                { price1 = 6; price2 = 6; }

                BaseBeverage bev = (BaseBeverage)item;

                if (bev.IsEmpty || bev.Content == BeverageType.Milk)
                    price = price1;
                else
                    price = price2;
            }

            return price;
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

        public bool IsSellable(Item item)
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
            Object o = m_Table[type];

            // price < 0 is ResourcePool, > 0 is regular item, and 0 is an unwanted CommodityDeed
            if (o == null || o is int price && price == 0)
                return false;
            else
                return true;
        }
    }
}