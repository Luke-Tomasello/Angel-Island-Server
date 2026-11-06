/***************************************************************************
 *
 *   Angel Island UO Shards
 *   Copyright (C) 2004-2026 Tomasello Software LLC.
 *
 ***************************************************************************/

/* Scripts\Engines\CourierSystem\CourierRewardInventory.cs
 * Changelog
 *  8/5/2026, Adam
 *  Created.
 */

using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Engines.CourierSystem
{
    public static class CourierRewardInventory
    {
        public static List<CourierRewardType> GetRewardTypes(
            BaseVendor vendor)
        {
            HashSet<CourierRewardType> unique =
                new HashSet<CourierRewardType>();

            if (vendor == null)
                return new List<CourierRewardType>();

            if (vendor.Deleted)
                return new List<CourierRewardType>();

            if (vendor.Inventory == null)
                return new List<CourierRewardType>();

            foreach (SBInfo sbInfo in vendor.Inventory)
            {
                if (sbInfo == null)
                    continue;

                if (sbInfo.BuyInfo == null)
                    continue;

                foreach (IBuyItemInfo buyInfo in sbInfo.BuyInfo)
                {
                    if (buyInfo == null)
                        continue;

                    Type itemType = buyInfo.Type;

                    CourierRewardType rewardType =
                        Classify(itemType);

                    if (rewardType == CourierRewardType.None)
                        continue;

                    unique.Add(rewardType);
                }
            }

            return new List<CourierRewardType>(unique);
        }

        public static CourierRewardType Classify(Type itemType)
        {
            if (itemType == null)
                return CourierRewardType.None;

            /*
             * Test derived/specialized classes before their broader parents.
             */
            if (typeof(BaseRanged).IsAssignableFrom(itemType))
                return CourierRewardType.RangedWeapon;

            if (typeof(BaseWeapon).IsAssignableFrom(itemType))
                return CourierRewardType.MeleeWeapon;

            if (typeof(BaseShield).IsAssignableFrom(itemType))
                return CourierRewardType.Shield;

            if (typeof(BaseArmor).IsAssignableFrom(itemType))
                return CourierRewardType.Armor;

            if (typeof(BasePotion).IsAssignableFrom(itemType))
                return CourierRewardType.Potion;

            if (typeof(SpellScroll).IsAssignableFrom(itemType))
                return CourierRewardType.Scroll;

            if (typeof(BaseWand).IsAssignableFrom(itemType))
                return CourierRewardType.Wand;

            if (typeof(BaseClothing).IsAssignableFrom(itemType))
                return CourierRewardType.Clothing;

            if (typeof(BaseJewel).IsAssignableFrom(itemType))
                return CourierRewardType.Jewelry;

            if (typeof(BaseReagent).IsAssignableFrom(itemType))
                return CourierRewardType.Reagent;

            /*
             * Gold may be explicitly represented by the inventory,
             * but it also remains the system fallback.
             */
            if (typeof(Gold).IsAssignableFrom(itemType))
                return CourierRewardType.Gold;

            return CourierRewardType.None;
        }

        public static CourierRewardType SelectRewardType(BaseVendor vendor)
        {
            List<CourierRewardType> types = GetRewardTypes(vendor);

            if (types.Count == 0)
                return CourierRewardType.Gold;

            return types[Utility.Random(types.Count)];
        }
    }
}