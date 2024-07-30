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

using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Factions
{
    public class FactionItemDefinition
    {
        private int m_SilverCost;
        private Type m_VendorType;

        public int SilverCost { get { return m_SilverCost; } }
        public Type VendorType { get { return m_VendorType; } }

        public FactionItemDefinition(int silverCost, Type vendorType)
        {
            m_SilverCost = silverCost;
            m_VendorType = vendorType;
        }

        private static FactionItemDefinition m_MetalArmor = new FactionItemDefinition(1000, typeof(Blacksmith));
        private static FactionItemDefinition m_Weapon = new FactionItemDefinition(1000, typeof(Blacksmith));
        private static FactionItemDefinition m_RangedWeapon = new FactionItemDefinition(1000, typeof(Bowyer));
        private static FactionItemDefinition m_LeatherArmor = new FactionItemDefinition(750, typeof(Tailor));
        private static FactionItemDefinition m_Clothing = new FactionItemDefinition(200, typeof(Tailor));
        private static FactionItemDefinition m_Scroll = new FactionItemDefinition(500, typeof(Mage));

        public static FactionItemDefinition Identify(Item item)
        {
            if (item is BaseArmor)
            {
                if (CraftResources.GetType(((BaseArmor)item).Resource) == CraftResourceType.Leather)
                    return m_LeatherArmor;

                return m_MetalArmor;
            }
            else if (item is BaseRanged)
                return m_RangedWeapon;
            else if (item is BaseWeapon)
                return m_Weapon;
            else if (item is BaseClothing)
                return m_Clothing;
            else if (item is SpellScroll)
            {
                SpellScroll scroll = (SpellScroll)item;

                MagicItemEffect effect = (MagicItemEffect)scroll.SpellID;

                if (effect == MagicItemEffect.Heal || effect == MagicItemEffect.GreaterHeal || effect == MagicItemEffect.Harm || effect == MagicItemEffect.Fireball || effect == MagicItemEffect.Lightning)
                    return m_Scroll;
            }

            return null;
        }
    }
}