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

/* Scripts\Engines\ChampionSpawn\Champs\Harrower\Items\ChampionSkullType.cs
 * ChangeLog
 *  03/09/07, plasma    
 *      Removed cannedevil namespace reference
 *  1/11/07, Adam
 *      Add the 'None' type for the special champs
 *  01/05/07, plasma!
 *      Changed CannedEvil namespace to ChampionSpawn for cleanup!
 */

namespace Server.Engines.ChampionSpawn
{
    public enum ChampionSkullType
    {
        Power,
        Enlightenment,
        Venom,
        Pain,
        Greed,
        Death,
        None        // special non-champ champs 
    }
}