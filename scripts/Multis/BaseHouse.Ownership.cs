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

/* Scripts/Multis/BaseHouse.Ownership.cs
 * ChangeLog
 *  1/1/2026, Adam
 *      Created.
 *  
 */

using Server.Accounting;
using System;
using System.Collections;

namespace Server.Multis
{
    // Ownership Model
    //  - Direct: Houses directly owned by a character (mobile) and present in the per-mobile house table.
    //  - Pending Inheritance: Houses queued/claimed for inheritance (account-associated), not directly owned.
    //  - Account-level Direct: Direct houses owned by any active character on an account.
    //  - All (Aggregate): Direct ∪ Pending Inheritance. Used for placement and “any claim” gating.
    public abstract partial class BaseHouse
    {
        /// <remarks>
        /// This method intentionally aggregates both direct ownership and pending inheritance.
        /// It is appropriate for "any claim" account checks (e.g., placement blocking),
        /// but is not appropriate when callers must distinguish between direct ownership
        /// and pending inheritance. Use <c>HasHouseDirect</c>, <c>HasAccountHouseDirect</c>,
        /// or <c>HasHousePendingInheritance</c> for stricter semantics.
        /// </remarks>
        public static ArrayList GetAccountHousesAll(Mobile m)
        {
            ArrayList list = new ArrayList();

            if (m == null)
                return list;

            Account? a = m.Account as Account;

            if (a == null)
                return list;

            // loop characters
            for (int i = 0; i < 5; ++i)
            {
                if (a[i] != null)
                {
                    // loop houses
                    ArrayList? exists = (ArrayList?)m_Table[a[i]];
                    if (exists != null)
                        foreach (object o in exists)
                            list.Add(o);                    // add any found to master list
                }
            }

            list.AddRange(GetHousesInheritance(m));

            return list;
        }

        public static ArrayList GetHousesInheritance(Account acct)
        {
            ArrayList list = new ArrayList();

            if (acct == null)
                return list;

            // For each character slot…
            for (int i = 0; i < 5; ++i)
            {
                Mobile m = acct[i] as Mobile;
                if (m == null || m.Deleted)
                    continue;

                // Get inheritance houses for this character (flatten the results)
                ArrayList playerHouses = GetHousesInheritance(m);

                if (playerHouses != null && playerHouses.Count > 0)
                    list.AddRange(playerHouses);
            }

            return list;
        }

        public static ArrayList GetHousesInheritance(Mobile? owner)
        {
            // GUID-based inheritance scan:
            // Returns all player-owned houses whose OwnerGUID matches the player's
            // stable identity GUID but which are not currently assigned to this
            // Mobile as the direct Owner. This reflects “pending inheritance”
            // under the Mortalis identity model (no Account.House / Serial usage).

            ArrayList list = new ArrayList();

            if (owner == null || owner.Deleted)
                return list;

            uint guid = owner.AcctGUID;
            if (guid == 0)
                return list;

            foreach (var kvp in Instances)
            {
                Multis.BaseHouse bh = kvp.Key;
                bool playerOwned = kvp.Value;

                if (bh == null || bh.Deleted)
                    continue;

                if (!playerOwned)
                    continue;

                // House belongs to this player identity, but is not currently
                // bound to this Mobile as the live owner → treat as inheritance.
                if (bh.OwnerGUID == guid && bh.Owner != owner)
                    list.Add(bh);
            }

            if (list.Count > 0 && Core.Debug && false)
                Utility.Monitor.WriteLine("Found House Inheritance for {0}", ConsoleColor.Green, owner);

            return list;
        }

        /// <summary>
        /// Determines whether the account has at least one house directly owned
        /// by any active character on the account.
        /// Pending inherited houses are explicitly excluded.
        /// </summary>
        /// <param name="m">The mobile whose account is being checked.</param>
        /// <returns>
        /// True if any character on the account directly owns a house; otherwise, false.
        /// </returns>
        public static bool HasAccountHouseDirect(Mobile m)
        {
            Account? a = m.Account as Account;

            if (a == null)
                return false;

            for (int i = 0; i < 5; ++i)
                if (a[i] != null && HasHouseDirect(a[i]))
                    return true;

            return false;
        }

        /// <summary>
        /// Determines whether the specified mobile's account has any house
        /// associated with it, including both directly owned houses and
        /// houses pending inheritance.
        /// </summary>
        /// <param name="m">
        /// The mobile whose account is being checked for any house association.
        /// </param>
        /// <returns>
        /// True if the account has at least one house either directly owned
        /// by an active character or pending inheritance; otherwise, false.
        /// </returns>
        /// <remarks>
        /// This method represents the broadest account-level house check and
        /// intentionally combines direct ownership and pending inheritance.
        /// It is suitable for placement-blocking or eligibility checks where
        /// any form of house association should be considered.
        /// </remarks>
        public static bool HasAnyAccountHouseAll(Mobile m)
        {
            return HasAccountHouseDirect(m) || HasHousePendingInheritance(m);
        }

        /// <summary>
        /// Determines whether the specified mobile's account has a house pending inheritance.
        /// Pending inherited houses are account-associated and are not considered directly owned.
        /// </summary>
        /// <param name="m">The mobile whose account is being checked for pending inheritance.</param>
        /// <returns>
        /// True if the account has a pending inherited house; otherwise, false.
        /// </returns>
        public static bool HasHousePendingInheritance(Mobile m)
        {
            return GetHousePendingInheritance(m) != null;
        }

        /// <summary>
        /// Determines whether the specified mobile directly owns at least one
        /// non-deleted house.
        /// Pending inherited houses are explicitly excluded.
        /// </summary>
        /// <param name="m">The mobile being checked for direct house ownership.</param>
        /// <returns>
        /// True if the mobile directly owns a non-deleted house; otherwise, false.
        /// </returns>
        public static bool HasHouseDirect(Mobile m)
        {
            if (m == null)
                return false;

            ArrayList? list = (ArrayList?)m_Table[m];

            if (list == null)
                return false;

            for (int i = 0; i < list.Count; ++i)
            {
                BaseHouse? h = (BaseHouse?)list[i];

                if (!h.Deleted)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves the house pending inheritance for the specified mobile using the
        /// GUID-based identity model. A pending inherited house is one whose OwnerGUID
        /// matches the player’s stable identity GUID, but which is not currently
        /// assigned as the player's direct house ownership.
        /// 
        /// This replaces all legacy account/serial-based tracking:
        ///   - No Account.House field is consulted.
        ///   - No Serial comparisons are performed.
        ///   - Inheritance resolves solely through stable identity matching.
        /// 
        /// Returns the first matching player-owned house, or null if none exists.
        /// </summary>
        public static BaseHouse? GetHousePendingInheritance(Mobile m)
        {
            if (m == null || m.Deleted)
                return null;

            uint guid = m.AcctGUID;

            if (guid == 0)
                return null;

            // Scan all player-owned houses and locate any whose identity matches this
            // player but which are not currently assigned as the player's direct owner.
            foreach (var kvp in BaseHouse.Instances)
            {
                BaseHouse house = kvp.Key;
                bool playerOwned = kvp.Value;

                if (house.Deleted)
                    continue;

                if (!playerOwned)
                    continue;

                // House belongs to this player’s identity
                if (house.OwnerGUID == guid)
                {
                    // Pending inheritance means:
                    //   The house’s identity matches the player,
                    //   but the house is NOT currently assigned to them as Owner.
                    //
                    // This distinction allows a reincarnated player to reclaim the
                    // inherited structure via the standard “double-click to claim” path.
                    if (house.Owner != m)
                        return house;
                }
            }

            return null;
        }

        public static bool OnAfterInheritance(Mobile m)
        {
            if (m == null)
                return false;

            Accounting.Account? acct = m.Account as Accounting.Account;

            if (acct == null)
                return false;

            // TBD - maybe nothing

            return true;
        }

    }
}