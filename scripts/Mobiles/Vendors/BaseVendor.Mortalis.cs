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

/* Scripts\Mobiles\Vendors\BaseVendor.Mortalis.cs
 * Changelog
 *  12/3/2025, Adam
 *  Created.
 *  Purpose: Imposes daily sell limits on players selling items to vendors.
 */
using Server.Accounting;
using System;

namespace Server.Mobiles
{
    public abstract partial class BaseVendor : BaseCreature, IVendor
    {
        // Optional: MO-specific per-transaction cap if you ever want it separate from MaxSell
        private const int MoMaxSell = 500;

        private static readonly TimeSpan DailySellWindow = TimeSpan.FromHours(24.0);

        private static bool CanSellerUseDailyLimit(Mobile seller)
        {
            // Only enforce on Mortalis; ignore for other rule sets and staff
            if (!Core.RuleSets.MortalisRules())
                return false;

            if (seller == null || !seller.Player)
                return false;

            //// Staff should not be limited
            //if (seller.AccessLevel > AccessLevel.Player)
            //    return false;

            return true;
        }

        /// <summary>
        /// Checks whether paying the given gold amount would exceed the player's daily limit.
        /// Returns true if allowed, false if this transaction should be blocked.
        /// </summary>
        private static bool CheckDailySellLimit(Mobile seller, int transactionGold, out int remainingGold)
        {
            remainingGold = CoreAI.DailySellGoldLimit;

            if (!CanSellerUseDailyLimit(seller) || transactionGold <= 0)
                return true;

            Account? account = seller.Account as Account;
            if (account == null)
                return true;

            // rollover window
            if (DateTime.UtcNow - account.VendorWindowStart >= DailySellWindow)
            {
                account.VendorDailyGold = 0;
                account.VendorWindowStart = DateTime.UtcNow;
            }

            int already = account.VendorDailyGold;
            remainingGold = CoreAI.DailySellGoldLimit - already;

            if (remainingGold <= 0)
                return false;

            return (already + transactionGold) <= CoreAI.DailySellGoldLimit;
        }

        /// <summary>
        /// Records that the given gold amount has been paid to the player in the current window.
        /// </summary>
        private static void RecordDailySale(Mobile seller, int goldPaid)
        {
            if (!CanSellerUseDailyLimit(seller) || goldPaid <= 0)
                return;

            Account? account = seller.Account as Account;
            if (account == null)
                return;

            // rollover guard here if you want, or rely on Check to have run first
            if (DateTime.UtcNow - account.VendorWindowStart >= DailySellWindow)
            {
                account.VendorDailyGold = 0;
                account.VendorWindowStart = DateTime.UtcNow;
            }

            account.VendorDailyGold += goldPaid;
        }
    }
}
