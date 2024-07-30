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

namespace Server.Items
{
    public enum WeaponQuality
    {
        Low,
        Regular,
        Exceptional,
    }

    public enum WeaponType
    {
        Axe,        // Axes, Hatches, etc. These can give concussion blows
        Slashing,   // Katana, Broadsword, Longsword, etc. Slashing weapons are poisonable
        Staff,      // Staves
        Bashing,    // War Hammers, Maces, Mauls, etc. Two-handed bashing delivers crushing blows
        Piercing,   // Spears, Warforks, Daggers, etc. Two-handed piercing delivers paralyzing blows
        Polearm,    // Halberd, Bardiche
        Ranged,     // Bow, Crossbows
        Fists       // Fists
    }

    public enum WeaponDamageLevel
    {
        Regular,
        Ruin,
        Might,
        Force,
        Power,
        Vanquishing
    }

    public enum WeaponAccuracyLevel
    {
        Regular,
        Accurate,
        Surpassingly,
        Eminently,
        Exceedingly,
        Supremely
    }

    public enum WeaponDurabilityLevel
    {
        Regular,
        Durable,
        Substantial,
        Massive,
        Fortified,
        Indestructible
    }

    public enum WeaponAnimation
    {
        Slash1H = 9,
        Pierce1H = 10,
        Bash1H = 11,
        Bash2H = 12,
        Slash2H = 13,
        Pierce2H = 14,
        ShootBow = 18,
        ShootXBow = 19,
        Wrestle = 31
    }
}