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

/* Patches201_300/MusicOwnershipReconciler.cs
 * ChangeLog
 *  10/29/26, Adam (MusicLibrary)
 *      Type names only: the music library moved out of MusicBox (MusicLibrary.Repository, MusicLibrary.MusicInfo).
 *  10/26/26, Adam (moved into the patch assembly)
 *      Moved from Scripts/Engines/AIMusicSystem/MusicBox (namespace Server.Items) to Patches201_300 (namespace
 *      Patches201_300): a one-time migration helper of PatchV252 belongs with its patch, not in the music system.
 *      Reconcile, its planning, validation, safeguards and MusicOwnership.log are unchanged.
 *  10/25/26, Adam
 *      A legacy copy with no composition is never matched by HashCode (CopiesNoMatch): its hash can collide with a
 *      real library song (the empty-composition hash).
 *  10/20/26, Adam
 *      Created. Reconciles music ownership on a shard whose music library came from another shard, using the
 *      account GUID (MusicInfo.OwnerGUID / Item.OwnerGUID) as the only source of identity:
 *      - Library: MusicInfo.Owner and its master RolledUpSheetMusic.Owner are bound to the local character of the
 *        account with that GUID.
 *      - Copies outside the library: a copy carrying an OwnerGUID is bound to that account's character; a legacy copy
 *        with OwnerGUID 0 takes its GUID (and owner) from the one library composition with the same HashCode.
 *      - Prices: a library entry with Price 0 whose owner is bound takes its master's positive Price (the price it was
 *        submitted with; MusicBoxMaintenance zeroes the price of ownerless entries).
 *      Character serials are never used to infer identity (they may come from another shard). Every change is
 *      planned and validated before anything is modified; with apply == false nothing is modified (dry run).
 *      Used by PatchV252 (Upheaval). The run is written to MusicOwnership.log.
 *      Note: RolledUpSheetMusic equality is by composition HashCode, so GlobalMusicRepository.ContainsKey() is also
 *      true for copies of a library song. Library masters are identified here by reference.
 */

using Server;
using Server.Accounting;
using Server.Diagnostics;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Patches201_300
{
    public static class MusicOwnershipReconciler
    {
        public sealed class Result
        {
            public bool Applied;
            public bool Failed;                     // validation failed: nothing was changed

            // library entries
            public int LibraryExamined, LibraryRelinked, LibraryAlreadyCorrect, LibraryUnresolved, LibraryAmbiguous, LibraryNoGuid;

            // copies outside the library
            public int CopiesExamined, CopiesRelinked, CopiesGuidRecovered, CopiesAlreadyCorrect, CopiesUnresolved, CopiesAmbiguous, CopiesNoMatch;

            // library prices
            public int PricesRestored, PricesAlreadySet, PricesUnrecoverable, PricesNoOwner;

            public readonly List<string> Lines = new();

            public int Changes => LibraryRelinked + CopiesRelinked + CopiesGuidRecovered + PricesRestored;

            public string Summary => string.Format(
                "Music ownership {0}: library {1} examined, {2} relinked, {3} already correct, {4} unresolved, {5} ambiguous, {6} without GUID; " +
                "copies {7} examined, {8} relinked, {9} GUID recovered, {10} already correct, {11} unresolved, {12} ambiguous, {13} without library match; " +
                "prices {14} restored, {15} already set, {16} unrecoverable (master price 0), {17} left (no owner); {18} changes.",
                Failed ? "NOT applied (validation failed), planned" : Applied ? "reconciled" : "dry run",
                LibraryExamined, LibraryRelinked, LibraryAlreadyCorrect, LibraryUnresolved, LibraryAmbiguous, LibraryNoGuid,
                CopiesExamined, CopiesRelinked, CopiesGuidRecovered, CopiesAlreadyCorrect, CopiesUnresolved, CopiesAmbiguous, CopiesNoMatch,
                PricesRestored, PricesAlreadySet, PricesUnrecoverable, PricesNoOwner, Changes);
        }

        private enum Resolution { Resolved, NoAccount, Ambiguous, NoCharacter }

        // One planned change. Applied only after the whole plan has been built and validated.
        private sealed class Change
        {
            public uint Guid;                         // the identity the change asserts
            public PlayerMobile? Owner;               // the resolved local character
            public MusicLibrary.MusicInfo? InfoOwner;     // library entry whose Owner is set to Owner
            public RolledUpSheetMusic? RsmOwner;      // master or copy whose Owner is set to Owner
            public RolledUpSheetMusic? RsmGuid;       // legacy copy that only receives OwnerGUID = Guid
            public MusicLibrary.MusicInfo? InfoPrice;     // library entry whose Price is restored
            public int Price;
        }

        /// <summary>
        /// Reconciles music ownership from account GUIDs. With apply == false nothing is modified and the result
        /// reports what would change. Writes the run to MusicOwnership.log.
        /// </summary>
        public static Result Reconcile(bool apply, string caller)
        {
            Result result = new Result { Applied = apply };
            List<Change> plan = new();

            Dictionary<uint, List<Account>> accountsByGuid = AccountsByGuid();
            Dictionary<uint, (Resolution, PlayerMobile?)> resolved = new();

            (Resolution, PlayerMobile?) Resolve(uint guid)
            {
                if (!resolved.TryGetValue(guid, out var r))
                    resolved[guid] = r = ResolveCharacter(guid, accountsByGuid);
                return r;
            }

            #region Library entries
            // Masters by reference (RolledUpSheetMusic equality is by composition HashCode).
            HashSet<RolledUpSheetMusic> masters = new(ReferenceEqualityComparer.Instance);
            // Composition HashCode -> library OwnerGUIDs (0 included), for legacy copies.
            Dictionary<int, HashSet<uint>> guidsByHash = new();

            foreach (KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo> kvp in MusicLibrary.Repository)
            {
                RolledUpSheetMusic rsm = kvp.Key;
                MusicLibrary.MusicInfo info = kvp.Value;

                if (rsm == null || info == null)
                    continue;

                masters.Add(rsm);

                if (rsm.Deleted)
                    continue;

                if (rsm.HashCode != 0)
                {
                    if (!guidsByHash.TryGetValue(rsm.HashCode, out HashSet<uint>? set))
                        guidsByHash[rsm.HashCode] = set = new HashSet<uint>();
                    set.Add(info.OwnerGUID);
                }

                result.LibraryExamined++;
                uint guid = info.OwnerGUID;
                string what = Describe(rsm, guid);

                if (guid == 0)
                {
                    result.LibraryNoGuid++;
                    result.PricesNoOwner += info.Price == 0 ? 1 : 0;
                    continue;
                }

                // The master must not carry a different identity than its database record.
                if (rsm.OwnerGUID != 0 && rsm.OwnerGUID != guid)
                {
                    result.LibraryAmbiguous++;
                    result.Lines.Add(string.Format("AMBIGUOUS library {0}: master carries OwnerGUID {1}", what, rsm.OwnerGUID));
                    result.PricesNoOwner += info.Price == 0 ? 1 : 0;
                    continue;
                }

                (Resolution res, PlayerMobile? pm) = Resolve(guid);
                if (res != Resolution.Resolved || pm == null)
                {
                    if (res == Resolution.Ambiguous)
                    {
                        result.LibraryAmbiguous++;
                        result.Lines.Add(string.Format("AMBIGUOUS library {0}: GUID shared by several accounts", what));
                    }
                    else
                    {
                        result.LibraryUnresolved++;
                        if (res == Resolution.NoCharacter)
                            result.Lines.Add(string.Format("UNRESOLVED library {0}: account has no character with this GUID", what));
                    }
                    result.PricesNoOwner += info.Price == 0 ? 1 : 0;
                    continue;
                }

                // An existing owner is kept when it is a character of the same account identity.
                bool infoOk = Owns(info.Owner, guid);
                bool rsmOk = Owns(rsm.Owner, guid) && rsm.OwnerGUID == guid;
                int price = info.Price == 0 && rsm.Price > 0 ? rsm.Price : 0;

                if (infoOk && rsmOk)
                    result.LibraryAlreadyCorrect++;
                else
                {
                    result.LibraryRelinked++;
                    result.Lines.Add(string.Format("RELINK library {0}: {1} -> {2}", what, OwnerText(info.Owner, rsm.Owner), CharText(pm)));
                }

                if (info.Price > 0)
                    result.PricesAlreadySet++;
                else if (price > 0)
                {
                    result.PricesRestored++;
                    result.Lines.Add(string.Format("PRICE library {0}: 0 -> {1}", what, price));
                }
                else
                    result.PricesUnrecoverable++;

                if (!(infoOk && rsmOk) || price > 0)
                    plan.Add(new Change
                    {
                        Guid = guid,
                        Owner = pm,
                        InfoOwner = infoOk ? null : info,
                        RsmOwner = rsmOk ? null : rsm,
                        InfoPrice = price > 0 ? info : null,
                        Price = price,
                    });
            }
            #endregion Library entries

            #region Copies outside the library
            foreach (Item item in World.Items.Values)
            {
                if (item is not RolledUpSheetMusic rsm || rsm.Deleted || masters.Contains(rsm))
                    continue;

                result.CopiesExamined++;
                uint guid = rsm.OwnerGUID;
                bool recovered = false;

                if (guid == 0)
                {
                    // Legacy copy: identity only from the one library composition with this HashCode.
                    //  A songless copy has no identity: its hash may collide with a real song (empty composition).
                    if (rsm.HashCode == 0 || !rsm.HasComposition || !guidsByHash.TryGetValue(rsm.HashCode, out HashSet<uint>? set))
                    {
                        result.CopiesNoMatch++;
                        continue;
                    }
                    if (set.Count != 1)
                    {
                        result.CopiesAmbiguous++;
                        result.Lines.Add(string.Format("AMBIGUOUS copy {0}: composition matches several owners", Describe(rsm, 0)));
                        continue;
                    }
                    guid = set.First();
                    if (guid == 0)
                    {
                        result.CopiesNoMatch++;
                        continue;
                    }
                    // A copy that already has a live owner of another identity is not overruled by a hash match.
                    if (rsm.Owner is Mobile current && !current.Deleted && !Owns(current, guid))
                    {
                        result.CopiesAmbiguous++;
                        result.Lines.Add(string.Format("AMBIGUOUS copy {0}: owned by {1}, composition belongs to GUID {2}", Describe(rsm, 0), CharText(current), guid));
                        continue;
                    }
                    recovered = true;
                }

                (Resolution res, PlayerMobile? pm) = Resolve(guid);
                if (res == Resolution.Ambiguous)
                {
                    result.CopiesAmbiguous++;
                    result.Lines.Add(string.Format("AMBIGUOUS copy {0}: GUID shared by several accounts", Describe(rsm, guid)));
                    continue;
                }

                if (res != Resolution.Resolved || pm == null)
                {
                    if (recovered)
                    {
                        // Record the identity even though the composer has no character here yet.
                        result.CopiesGuidRecovered++;
                        result.Lines.Add(string.Format("GUID copy {0}: OwnerGUID 0 -> {1} (no local character)", Describe(rsm, 0), guid));
                        plan.Add(new Change { Guid = guid, RsmGuid = rsm });
                    }
                    else
                        result.CopiesUnresolved++;
                    continue;
                }

                if (!recovered && Owns(rsm.Owner, guid))
                {
                    result.CopiesAlreadyCorrect++;
                    continue;
                }

                if (recovered)
                    result.CopiesGuidRecovered++;
                else
                    result.CopiesRelinked++;
                result.Lines.Add(string.Format("{0} copy {1}: {2} -> {3}", recovered ? "GUID+RELINK" : "RELINK", Describe(rsm, guid), OwnerText(null, rsm.Owner), CharText(pm)));
                plan.Add(new Change { Guid = guid, Owner = pm, RsmOwner = rsm });
            }
            #endregion Copies outside the library

            #region Validate, then apply
            // Every owner assignment must leave the identity unchanged: the character carries exactly the GUID the
            // record asserts, and an object that already carries a GUID carries this one.
            foreach (Change c in plan)
            {
                bool ownerSet = c.InfoOwner != null || c.RsmOwner != null;
                if (c.Guid == 0 ||
                    (ownerSet && (c.Owner == null || c.Owner.Deleted || c.Owner.AcctGUID != c.Guid)) ||
                    (c.InfoOwner != null && c.InfoOwner.OwnerGUID != c.Guid) ||
                    (c.RsmOwner != null && c.RsmOwner.OwnerGUID != 0 && c.RsmOwner.OwnerGUID != c.Guid) ||
                    (c.RsmGuid != null && c.RsmGuid.OwnerGUID != 0) ||
                    (c.InfoPrice != null && (c.InfoPrice.Price != 0 || c.Price <= 0)))
                {
                    result.Applied = false;
                    result.Failed = true;
                    result.Lines.Add(string.Format("VALIDATION FAILED for GUID {0}; nothing was changed.", c.Guid));
                    Write(result, caller);
                    return result;
                }
            }

            if (apply)
                foreach (Change c in plan)
                {
                    // The Owner setters stamp OwnerGUID = Owner.AcctGUID, which validation has shown equals c.Guid.
                    if (c.InfoOwner != null)
                        c.InfoOwner.Owner = c.Owner;
                    if (c.RsmOwner != null)
                        c.RsmOwner.Owner = c.Owner;
                    if (c.RsmGuid != null)
                        c.RsmGuid.OwnerGUID = c.Guid;       // identity only; Owner is left as it is
                    if (c.InfoPrice != null)
                        c.InfoPrice.Price = c.Price;
                }
            #endregion Validate, then apply

            Write(result, caller);
            return result;
        }

        #region Identity
        private static Dictionary<uint, List<Account>> AccountsByGuid()
        {
            Dictionary<uint, List<Account>> table = new();
            foreach (object o in Accounts.Table.Values)
                if (o is Account acct && acct.GUID != 0)
                {
                    if (!table.TryGetValue(acct.GUID, out List<Account>? list))
                        table[acct.GUID] = list = new List<Account>();
                    list.Add(acct);
                }
            return table;
        }

        // GUID -> the one account with it -> its first character (lowest slot) carrying the same GUID.
        private static (Resolution, PlayerMobile?) ResolveCharacter(uint guid, Dictionary<uint, List<Account>> accountsByGuid)
        {
            if (!accountsByGuid.TryGetValue(guid, out List<Account>? list))
                return (Resolution.NoAccount, null);
            if (list.Count > 1)
                return (Resolution.Ambiguous, null);

            Account acct = list[0];
            for (int i = 0; i < acct.Length; i++)
                if (acct[i] is PlayerMobile pm && !pm.Deleted && pm.AcctGUID == guid && pm.Account == acct)
                    return (Resolution.Resolved, pm);

            return (Resolution.NoCharacter, null);
        }

        // An owner is correct when it is a live character of the account identity.
        private static bool Owns(Mobile? m, uint guid)
        {
            return m is PlayerMobile pm && !pm.Deleted && guid != 0 && pm.AcctGUID == guid;
        }
        #endregion Identity

        #region Reporting
        private static string Describe(RolledUpSheetMusic rsm, uint guid)
        {
            return string.Format("'{0}' {1} (GUID {2})", rsm.Title, rsm.Serial, guid);
        }

        private static string CharText(Mobile? m)
        {
            return m == null ? "(none)" : string.Format("'{0}' {1}", m.Name, m.Serial);
        }

        private static string OwnerText(Mobile? info, Mobile? rsm)
        {
            return info == null || info == rsm ? CharText(rsm) : string.Format("{0} / master {1}", CharText(info), CharText(rsm));
        }

        private static void Write(Result result, string caller)
        {
            try
            {
                StringBuilder sb = new();
                sb.AppendLine(string.Format("{0:u} {1} ({2})", DateTime.UtcNow, caller, result.Applied ? "applied" : "dry run"));
                foreach (string line in result.Lines)
                    sb.AppendLine("    " + line);
                sb.AppendLine("    " + result.Summary);
                string path = Path.Combine(Core.LogsDirectory, "MusicOwnership.log");
                Utility.EnsurePath(path);
                File.AppendAllText(path, sb.ToString());
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        #endregion Reporting
    }
}
