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

/* Scripts\Engines\ConPVP\ArenaGump.cs
 * Changelog:
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *      This file serves as the main control class.
 */

namespace Server.Engines.ConPVP
{
    public static class ConPVPSystem
    {
        public static bool Enabled
        {
            get
            {
#if true
                return true;
#else
                return Core.UOTC_CFG;
#endif
            }
        }
    }
}