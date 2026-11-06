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
    *************************************************************************** /

   /***************************************************************************
    *
    *   This program is free software; you can redistribute it and/or modify
    *   it under the terms of the GNU General Public License as published by
    *   the Free Software Foundation; either version 2 of the License, or
    *   (at your option) any later version.
    *
    *************************************************************************** /

/* Scripts\Engines\CourierSystem\CourierRewardType.cs
 * Changelog
 *  8/5/2026, Adam
 *  Created.
 */

namespace Server.Engines.CourierSystem
{
    public enum CourierRewardType
    {
        None,

        MeleeWeapon,
        RangedWeapon,
        Armor,
        Shield,

        Reagent,
        Potion,
        Scroll,
        Wand,

        Clothing,
        Jewelry,

        Gold
    }
}