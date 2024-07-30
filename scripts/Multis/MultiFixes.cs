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

/* Scripts/Multis/MultiFixes.cs
 * ChangeLog
 *  10/25/21 Yoar
 *      Initial version. Use this class to add server-side fixes to multis.
 *      
 *      Fix for large marble patio house: Added hidden surface tile to fill
 *      in a gap in the floor (in the southern-most flower bed).
 */

using Server;

namespace Scripts.Multis
{
    public static class MultiFixes
    {
        public static void Initialize()
        {
            MultiComponentList mcl;

            // large marble patio house
            mcl = MultiData.GetComponents(0x96);
            mcl.Add(0x2198, 1, 2, 4); // add hidden surface tile
        }
    }
}