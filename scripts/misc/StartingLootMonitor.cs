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

/* Scripts\misc\StartingLootMonitor.cs
 * ChangeLog
 *  12/10/2025, Adam
 *  First time check in.
 *  Purpose : Monitor and allocate unique IDs for starting loot packages.
 */

using Server;
using Server.Accounting;

namespace Scripts.Misc
{
    internal class StartingLootMonitor
    {
        public static bool CanUse(Mobile m, Item? item)
        {
            if (item == null || m == null) return false;
            if (item.FamilyID == 0) return true; // world item OK

            var acct = m.Account as Account;
            return acct != null && acct.HasStarterFamilyId(item.FamilyID);
        }

        public static bool DenyWithMsg(Mobile m)
        {
            m?.SendMessage("This provision is bound to another soul.");
            return false;
        }
    }
}
