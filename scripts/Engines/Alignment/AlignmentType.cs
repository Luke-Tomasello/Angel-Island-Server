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

/* Scripts\Engines\Alignment\AlignmentType.cs
 * Changelog:
 *  4/30/23, Yoar
 *      Added non-selectable Outcast alignment
 *  4/14/23, Yoar
 *      Initial version.
 */

namespace Server.Engines.Alignment
{
    public enum AlignmentType : byte
    {
        None,

        Council,
        Pirate,
        Brigand,
        Orc,
        Savage,
        Undead,
        Militia,

        Order,
        Chaos,

        Outcast,
    }
}