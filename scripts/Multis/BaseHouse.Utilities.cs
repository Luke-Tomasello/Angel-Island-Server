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

/* Scripts/Multis/BaseHouse.Utilities.cs
 * ChangeLog
 *  3/7/2026, Adam
 *      Created.
 *  
 */

using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using Server.Multis.Deeds;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Multis
{
    // Ownership Model
    //  - Direct: Houses directly owned by a character (mobile) and present in the per-mobile house table.
    //  - Pending Inheritance: Houses queued/claimed for inheritance (account-associated), not directly owned.
    //  - Account-level Direct: Direct houses owned by any active character on an account.
    //  - All (Aggregate): Direct ∪ Pending Inheritance. Used for placement and “any claim” gating.

    public abstract partial class BaseHouse
    {
        public sealed class RuneTargetHit
        {
            public RecallRune Rune;
            public Runebook Book;
            public int EntryIndex = -1;
            public RunebookEntry? Entry;

            public bool IsRune
            {
                get { return Rune != null; }
            }

            public bool IsRunebookEntry
            {
                get { return Book != null && EntryIndex >= 0 && Entry != null; }
            }

            public Item DisplayItem
            {
                get { return (Item)Rune ?? (Item)Book; }
            }

            public string Describe()
            {
                if (Rune != null)
                {
                    return String.Format(
                        "RecallRune 0x{0:X}: '{1}' -> {2} ({3})",
                        (int)Rune.Serial,
                        String.IsNullOrWhiteSpace(Rune.Description) ? "(no description)" : Rune.Description,
                        Rune.Target,
                        Rune.TargetMap);
                }

                if (Book != null && Entry != null)
                {
                    return String.Format(
                        "Runebook 0x{0:X}, entry {1}: '{2}' -> {3} ({4})",
                        (int)Book.Serial,
                        EntryIndex,
                        String.IsNullOrWhiteSpace(Entry.Description) ? "(no description)" : Entry.Description,
                        Entry.Location,
                        Entry.Map);
                }

                return "(unknown rune target)";
            }
        }

        public static void Initialize()
        {
            Server.CommandSystem.Register("GetDeed", AccessLevel.Administrator, new CommandEventHandler(OnGetDeed));
            Server.CommandSystem.Register("BanRunes", AccessLevel.GameMaster, new CommandEventHandler(OnBanRunes));
            Server.CommandSystem.Register("FindRunes", AccessLevel.GameMaster, new CommandEventHandler(OnFindRunes));
        }

        [Usage("FindRunes <friends|strangers>")]
        [Description("Finds marked recall runes and runebook entries for a house and reports them without retargeting.")]
        private static void OnFindRunes(CommandEventArgs e)
        {
            ExecuteRuneCommand(
                e,
                commandName: "FindRunes",
                prompt: "Find runes for which house?",
                logFile: "FoundRunes.log",
                postAction: null,
                summary: count => String.Format(
                    "{0} matching rune target{1} found.",
                    count,
                    count == 1 ? "" : "s"));
        }

        [Usage("BanRunes <friends|strangers>")]
        [Description("Retargets marked recall runes and runebook entries for a house to the house ban location.")]
        private static void OnBanRunes(CommandEventArgs e)
        {
            ExecuteRuneCommand(
                e,
                commandName: "BanRunes",
                prompt: "Ban runes for which house?",
                logFile: "BannedRunes.log",
                postAction: (bh, list) => BanRuneTargets(bh, list),
                summary: count => String.Format(
                    "{0} rune target{1} retargeted to the house ban location.",
                    count,
                    count == 1 ? "" : "s"));
        }

        private static void ExecuteRuneCommand(CommandEventArgs e, string commandName, string prompt, string logFile, Action<BaseHouse, List<RuneTargetHit>>? postAction, Func<int, string> summary)
        {
            if (!TryParseRuneMode(e, commandName, out bool includeFriends))
                return;

            ResetJumpList(e.Mobile);

            e.Mobile.SendMessage(prompt);
            e.Mobile.SendMessage("Please target the house sign.");
            e.Mobile.Target = new RuneCommandTarget(includeFriends, logFile, postAction, summary);
        }

        private static bool TryParseRuneMode(CommandEventArgs e, string commandName, out bool includeFriends)
        {
            includeFriends = false;

            if (e.Length != 1)
            {
                e.Mobile.SendMessage("Format: {0} <friends|strangers>", commandName);
                return false;
            }

            string mode = e.GetString(0);

            if (Insensitive.Equals(mode, "friends"))
            {
                includeFriends = true;
                return true;
            }

            if (Insensitive.Equals(mode, "strangers"))
            {
                includeFriends = false;
                return true;
            }

            e.Mobile.SendMessage("You must specify either friends or strangers.");
            return false;
        }

        private static void ResetJumpList(Mobile from)
        {
            PlayerMobile? pm = from as PlayerMobile;

            if (pm != null)
            {
                pm.JumpIndex = 0;
                pm.JumpList = new ArrayList();
            }
        }

        private static BaseHouse? GetHouseFromTarget(Mobile from, object target)
        {
            HouseSign? sign = target as HouseSign;

            if (sign == null || sign.Structure == null)
            {
                from.SendMessage("That is not a house sign.");
                return null;
            }

            BaseHouse bh = sign.Structure as BaseHouse;

            if (bh == null)
            {
                from.SendMessage("That is not a valid house.");
                return null;
            }

            return bh;
        }

        private static List<RuneTargetHit> CollectRuneTargets(Mobile from, BaseHouse bh, bool includeFriends, string logFile)
        {
            List<RuneTargetHit> candidates = FindRuneTargets(bh);
            List<RuneTargetHit> list = new List<RuneTargetHit>();

            PlayerMobile? pm = from as PlayerMobile;
            LogHelper logger = new LogHelper(logFile, null, overwrite: false);

            try
            {
                foreach (RuneTargetHit hit in candidates)
                {
                    if (hit == null || hit.DisplayItem == null || hit.DisplayItem.Deleted)
                        continue;

                    bool friendOwned = IsFriendOwnedRuneTarget(bh, hit);

                    if (includeFriends)
                    {
                        if (!friendOwned)
                            continue;
                    }
                    else
                    {
                        if (friendOwned)
                            continue;
                    }

                    if (pm != null)
                        pm.JumpList.Add(hit.DisplayItem);

                    logger.Log(LogType.Item, hit.DisplayItem);
                    list.Add(hit);

                    from.SendMessage(hit.Describe());
                }
            }
            finally
            {
                logger.Finish();
            }

            return list;
        }

        private class RuneCommandTarget : Target
        {
            private readonly bool m_IncludeFriends;
            private readonly string m_LogFile;
            private readonly Action<BaseHouse, List<RuneTargetHit>>? m_PostAction;
            private readonly Func<int, string> m_Summary;

            public RuneCommandTarget(
                bool includeFriends,
                string logFile,
                Action<BaseHouse, List<RuneTargetHit>>? postAction,
                Func<int, string> summary)
                : base(8, false, TargetFlags.None)
            {
                m_IncludeFriends = includeFriends;
                m_LogFile = logFile;
                m_PostAction = postAction;
                m_Summary = summary;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                BaseHouse? bh = GetHouseFromTarget(from, target);

                if (bh == null)
                    return;

                List<RuneTargetHit> list = CollectRuneTargets(from, bh, m_IncludeFriends, m_LogFile);

                if (list.Count == 0)
                {
                    from.SendMessage("No matching rune targets were found.");
                    return;
                }

                if (m_PostAction != null)
                    m_PostAction(bh, list);

                if (m_Summary != null)
                    from.SendMessage(m_Summary(list.Count));
            }
        }

        private static bool IsFriendOwnedRuneTarget(BaseHouse bh, RuneTargetHit hit)
        {
            if (bh == null || hit == null)
                return false;

            Item item = hit.DisplayItem;

            if (item == null || item.Deleted)
                return false;

            Mobile? owner = item.RootParent as Mobile;

            if (owner == null || owner.Deleted)
                return false;

            return bh.IsFriend(owner);
        }

        public static List<RuneTargetHit> FindRuneTargets(BaseHouse bh)
        {
            List<RuneTargetHit> list = new List<RuneTargetHit>();

            if (bh == null || bh.Deleted || bh.Map == null || bh.Map == Map.Internal)
                return list;

            foreach (Item item in Server.World.Items.Values)
            {
                if (item == null || item.Deleted || item.Map != bh.Map)
                    continue;

                RecallRune? rune = item as RecallRune;

                if (rune != null)
                {
                    if (!rune.Marked)
                        continue;

                    if (rune.TargetMap != bh.Map)
                        continue;

                    BaseHouse? targetHouse = BaseHouse.FindHouseAt(rune.Target, rune.TargetMap, 16, parcel: true);

                    if (targetHouse != bh)
                        continue;

                    list.Add(new RuneTargetHit
                    {
                        Rune = rune
                    });

                    continue;
                }

                Runebook? book = item as Runebook;

                if (book != null && book.Entries != null)
                {
                    for (int i = 0; i < book.Entries.Count; ++i)
                    {
                        RunebookEntry? entry = book.Entries[i] as RunebookEntry;

                        if (entry == null || entry.Map == null || entry.Map != bh.Map)
                            continue;

                        BaseHouse? targetHouse = BaseHouse.FindHouseAt(entry.Location, entry.Map, 16, parcel: true);

                        if (targetHouse != bh)
                            continue;

                        list.Add(new RuneTargetHit
                        {
                            Book = book,
                            EntryIndex = i,
                            Entry = entry
                        });
                    }
                }
            }

            return list;
        }

        public static List<RecallRune> FindRecallRunes(BaseHouse bh, bool allowFriends = false)
        {
            List<RecallRune> list = new List<RecallRune>();

            if (bh == null || bh.Deleted || bh.Map == null || bh.Map == Map.Internal)
                return list;

            foreach (RuneTargetHit hit in FindRuneTargets(bh))
            {
                if (hit != null && hit.Rune != null)
                    list.Add(hit.Rune);
            }

            return list;
        }

        public static void BanRuneTargets(BaseHouse bh, List<RuneTargetHit> targets)
        {
            if (bh == null || bh.Deleted || targets == null || bh.Map == null || bh.Map == Map.Internal)
                return;

            int x = bh.BanLocation.X;
            int y = bh.BanLocation.Y;
            int z = bh.BanLocation.Z;

            Map map = bh.Map;

            if (map != null && !Utility.CanFit(map, x, y, z, 16, Utility.CanFitFlags.requireSurface))
                z = map.GetAverageZ(x, y);

            Point3D banTarget = new Point3D(x, y, z);

            foreach (RuneTargetHit hit in targets)
            {
                if (hit == null)
                    continue;

                if (hit.Rune != null)
                {
                    RecallRune rune = hit.Rune;

                    if (rune.Deleted || !rune.Marked)
                        continue;

                    rune.Target = banTarget;
                    rune.TargetMap = map;
                    rune.Description = "defunct";
                    rune.House = null;
                    rune.InvalidateProperties();
                }
                else if (hit.Book != null && hit.Entry != null && hit.EntryIndex >= 0 && hit.EntryIndex < hit.Book.Entries.Count)
                {
                    RunebookEntry entry = hit.Entry;

                    entry.Location = banTarget;
                    entry.Map = map;
                    entry.Description = "defunct";
                    entry.House = null;

                    hit.Book.InvalidateProperties();
                }
            }
        }

        public static void BanRecallRunes(BaseHouse bh, List<RecallRune> runes)
        {
            if (bh == null || bh.Deleted || runes == null || bh.Map == null || bh.Map == Map.Internal)
                return;

            int x = bh.BanLocation.X;
            int y = bh.BanLocation.Y;
            int z = bh.BanLocation.Z;

            Map map = bh.Map;

            if (map != null && !Utility.CanFit(map, x, y, z, 16, Utility.CanFitFlags.requireSurface))
                z = map.GetAverageZ(x, y);

            Point3D banTarget = new Point3D(x, y, z);

            foreach (RecallRune rune in runes)
            {
                if (rune == null || rune.Deleted)
                    continue;

                if (!rune.Marked)
                    continue;

                rune.Target = banTarget;
                rune.TargetMap = map;
                rune.Description = "defunct";
                rune.House = null;

                rune.InvalidateProperties();
            }
        }
    }

    public abstract partial class BaseHouse
    {

        [Usage("GetDeed")]
        [Description("Gets the deed for this house.")]

        private static void OnGetDeed(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Get a deed for which house?.");
            e.Mobile.SendMessage("Please target the house sign.");
            e.Mobile.Target = new GetDeedTarget();
        }

        public class GetDeedTarget : Target
        {
            public GetDeedTarget()
                : base(8, false, TargetFlags.None)
            {

            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is HouseSign && (target as HouseSign).Structure != null)
                {
                    HouseSign? sign = target as HouseSign;
                    /*
                    if (sign.Owner is StaticHouse == false)
                    {
                        from.SendMessage("You may only wipe regions on a StaticHouse.");
                        return;
                    }*/

                    BaseHouse bh = sign.Structure as BaseHouse;
                    if (bh != null)
                    {
                        HouseDeed? hd = bh.GetDeed();
                        if (hd == null)
                        {
                            from.SendMessage("There is no deed for this house.");
                            return;
                        }
                        from.AddToBackpack(hd);
                        from.SendMessage("The deed has been added to your backpack.");
                        return;
                    }
                    from.SendMessage("Error getting house deed.");
                }
                else
                {
                    from.SendMessage("That is not a house sign.");
                }
            }
        }
    }


}