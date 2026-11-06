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

/* Scripts/Misc/CharacterCreation.Utilities.cs
 * ChangeLog
 *  1/12/2026, Adam
  */

using Server.Accounting;
using Server.Engines;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using System;
using System.Collections;
using System.Collections.Generic;
using static Server.Commands.Prison;

namespace Server.Misc
{
    public partial class CharacterCreation
    {
        private static void OnDeleteCharacter(object state)
        {
            if (state is Mobile m)
            {
                if (m.NetState != null && m.NetState.Running)
                    m.NetState.Dispose();

                m.Delete();

                Console.WriteLine("Player {0} deleted (too many characters)", m);
            }
        }
        private static bool CharacterCreationExceededAccountLimit(Account a, PlayerMobile created)
        {
            if (a == null || created == null || created.Deleted)
                return false;

            int max = CoreAI.MaxCharactersPerAccount;

            if (max <= 0)
                return false;

            int count = 0;

            for (int i = 0; i < a.Length; ++i)
            {
                Mobile m = a[i] as Mobile;

                if (m == null || m.Deleted)
                    continue;

                count++;
            }

            return count > max;
        }

        #region Seasons Season 2 - durable starting equipment

        private static bool BoostStartingDurability(Item item, int multiplier)
        {
            if (item == null || item.Deleted || multiplier <= 1)
                return false;

            switch (item)
            {
                case BaseWeapon weapon:
                    BoostHitPoints(weapon, multiplier);

                    if (weapon is IUsesRemaining uses)
                        BoostUses(uses, multiplier);

                    return true;

                case BaseArmor armor:
                    BoostHitPoints(armor, multiplier);
                    return true;

                case BaseClothing clothing:
                    BoostHitPoints(clothing, multiplier);
                    return true;

                case BaseTool tool:
                    BoostUses(tool, multiplier);
                    return true;
            }

            return false;
        }

        private static void BoostHitPoints(BaseWeapon item, int multiplier)
        {
            item.MaxHitPoints = Math.Max(item.MaxHitPoints, item.HitPoints);
            item.MaxHitPoints *= multiplier;
            item.HitPoints = item.MaxHitPoints;
        }

        private static void BoostHitPoints(BaseArmor item, int multiplier)
        {
            item.MaxHitPoints = Math.Max(item.MaxHitPoints, item.HitPoints);
            item.MaxHitPoints *= multiplier;
            item.HitPoints = item.MaxHitPoints;
        }

        private static void BoostHitPoints(BaseClothing item, int multiplier)
        {
            item.MaxHitPoints = Math.Max(item.MaxHitPoints, item.HitPoints);
            item.MaxHitPoints *= multiplier;
            item.HitPoints = item.MaxHitPoints;
        }

        private static void BoostUses(IUsesRemaining item, int multiplier)
        {
            item.UsesRemaining *= multiplier;
        }

        private static void BoostUses(BaseTool item, int multiplier)
        {
            item.UsesRemaining *= multiplier;
        }

        #endregion Seasons Season 2 - durable starting equipment

        public static void QueueWelcomeMessage(Mobile m, string line)
        {
            if (m == null || string.IsNullOrWhiteSpace(line))
                return;

            WelcomeTimer.AppendFor(m, new[] { line.Trim() }, hue: 0x35);
        }
        public static void RemoveDuplicateFromBackpackIfBanked<T>(Mobile m) where T : Item
        {
            if (m == null)
                return;

            Container bank = m.BankBox;
            Container pack = m.Backpack;

            if (bank == null || pack == null)
                return;

            // If the item type already exists in the bank, remove it from the backpack
            if (bank.FindItemByType(typeof(T), false) != null)
            {
                Item? item = pack.FindItemByType(typeof(T), false);

                if (item != null)
                {
                    pack.RemoveItem(item);
                    item.Delete();
                }
            }
        }

        public static bool IsJailOrPrison(Mobile m)
        {
            if (m == null)
                return false;

            Account? acct = m.Account as Account;

            if (acct == null)
                return false;

            if (acct.IsImprisoned)
                return true;

            if (acct.IsJailed)
                return true;

            return false;
        }

        public static void ApplyJailOrPrison(Mobile m)
        {
            if (m == null)
                return;

            Account? acct = m.Account as Account;

            if (acct == null)
                return;

            if (acct.IsImprisoned)
            {
                var aip = new PrisonPlayer(from: (PlayerMobile)World.GetAdminAcct(),
                    (PlayerMobile)m,
                    Point3D.Zero, "Character imprisoned on login",
                    sentence: 3);
                aip.GoToPrison();
                return;
            }

            if (acct.IsJailed)
            {
                Server.Commands.Jail.DoJailing(jailer: World.GetAdminAcct(),
                    toJail: m,
                    cell: 3,
                    comment: "Character jailed on login",
                    sentence: 24,
                    trouble: false);
                return;
            }
        }

        public static void ApplyAccountHouseBans(PlayerMobile pm)
        {
            if (pm == null)
                return;

            if (!Core.RuleSets.MortalisRules())
                return;

            if (pm.Account is not Accounting.Account acc)
                return;

            List<int> list = acc.BannedHouseSerials;
            if (list == null || list.Count == 0)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                Serial houseSerial = (Serial)list[i];

                // Find the house by serial; exact API may differ on your shard.
                BaseHouse? house = World.FindItem(houseSerial) as BaseHouse;

                if (house == null || house.Deleted)
                    continue;

                // Re-apply the ban to the new incarnation.
                house.Bans.Add(pm);
            }
        }

        public static Dictionary<uint, DateTime> GrieferHotList = new();
        // Mortalis Town-Justice System (Account-Level Escalation)
        //
        // This system exists to stop the classic suicide-by-guards grief loop where a
        // player repeatedly creates throwaway characters, runs into a guarded town,
        // attacks an innocent (often with poison), gets guard-whacked immediately,
        // deletes the character, and repeats. The goal is to make the behavior costly
        // at the account level so that new characters cannot escape the consequences.
        //
        // The pipeline works in three stages:
        //
        // 1. Patrol Guard (detection):
        //    Patrol guards forward qualifying aggression into the guard system without
        //    poofing. They trigger guard response when a player is (a) on the griefer
        //    list with a fresh criminal flag, or (b) actively committing an in-town
        //    murderous offense. See patrol guard code for details.
        //
        // 2. PlayerMobile (recording):
        //    Whenever a harmful action occurs, PlayerMobile determines whether it
        //    qualifies as an in-town murderous offense (innocent target, guarded
        //    region, active Aggressed entry). If so, the account's MurderousOffense
        //    counter is incremented. Death is irrelevant; the act itself is enough.
        //    See DoHarmful and InTownMurderousOffense for detection logic.
        //
        // 3. Account-Level Escalation (this method):
        //    When a new character is created, the account's MurderousOffense value is
        //    checked. Any value greater than zero triggers escalating punitive measures
        //    applied to the new character.
        //
        //    Tier 1 (1st offense):
        //       Spawn at Buccaneers Den (handled in NewCharacterSpawnLocation).
        //
        //    Tier 2 (2nd offense):
        //       Buccs spawn.
        //       4 hour criminal timer.
        //       Town crier announcement.
        //       Add player to GrieferShitList.
        //
        //    Tier 3 (3rd offense):
        //       Buccs spawn.
        //       4 hour criminal timer.
        //       +3 murder counts (short and long).
        //       Town crier announcement.
        //       Add player to GrieferShitList.
        //
        //    Tier 4+ (4th and all subsequent offenses):
        //       Buccs spawn.
        //       4 hour criminal timer.
        //       +1 murder count.
        //       Town crier announcement.
        //       Add player to GrieferShitList.
        //
        // The result is that griefing in town carries persistent, unavoidable,
        // account-wide consequences. Disposable characters no longer provide a free
        // avenue for repeated grief attempts.
        public static void GrieferEscalation(Mobile m)
        {
            if (m == null)
                return;

            Account? acct = m.Account as Account;
            if (acct == null)
                return;

            int offenseCount = acct.HarmfulOffenses;

            // All tiers start with Buccaneers Den respawn.
            // already done in NewCharacterSpawnLocation()

            // Optional hygiene: prune expired hot entries opportunistically
            DateTime now = DateTime.UtcNow;
            if (GrieferHotList.TryGetValue(acct.GUID, out DateTime hotUntil) && hotUntil <= now)
                GrieferHotList.Remove(acct.GUID);

            // Tier 1: Buc's respawn only (no additional action)
            if (offenseCount <= 1)
            {
                BroadcastGrieferEscalation(m, offenseCount);
                QueueAdminJump(m);
                RehydrateCriminalityFromAccount(m);
                return;
            }

            // Tier 2+: apply the common punishment package
            TownJustice.AddMurderousOffenseAnnouncement(m);

            // Guards should treat as active target for 1 hour
            GrieferHotList[acct.GUID] = now + TimeSpan.FromHours(1);

            // Tier-specific murder count adjustments
            if (offenseCount == 3)
            {
                m.ShortTermMurders += 3;
                m.LongTermMurders += 3;
            }
            else if (offenseCount >= 4)
            {
                m.ShortTermMurders += 1;
                m.LongTermMurders += 1;
            }

            BroadcastGrieferEscalation(m, offenseCount);
            QueueAdminJump(m);
        }

        private static void BroadcastGrieferEscalation(Mobile m, int offenseCount)
        {
            Commands.CommandHandlers.BroadcastMessage(
                AccessLevel.Administrator,
                0x22,
                string.Format("{0} ({1}) is on {2} murderous offense.",
                    m.Name,
                    m.Serial,
                    Utility.ToOrdinal(offenseCount)));
        }

        private static void QueueAdminJump(Mobile m)
        {
            if (World.GetAdminAcct() is PlayerMobile admin && admin.NetState != null)
            {
                admin.JumpIndex = 0;
                admin.JumpList = new ArrayList();
                admin.JumpList.Add(m);
            }
        }

        private static TimeSpan RemainingUntil(DateTime endsAtUtc, DateTime nowUtc)
        {
            if (endsAtUtc == DateTime.MaxValue)
                return TimeSpan.MaxValue;

            TimeSpan rem = endsAtUtc - nowUtc;
            return (rem > TimeSpan.Zero) ? rem : TimeSpan.Zero;
        }
        public static void RehydrateCriminalityFromAccount(Mobile m)
        {
            if (m is not PlayerMobile pm)
                return;

            Accounting.Account? acct = pm.Account as Accounting.Account;
            if (acct == null)
                return;

            DateTime now = DateTime.UtcNow;

            // Prefer a single active "source" block, mirroring "what have i done" order.
            // If you ever allow stacking, you can change this to merge maxima instead.
            Mobile.CriminalitySource? source = null;

            DateTime criminalEnds = DateTime.MinValue;
            DateTime whackEnds = DateTime.MinValue;
            DateTime travelEnds = DateTime.MinValue;

            if (acct.HarmfulCriminalityEnds > now || acct.HarmfulGuardWhackEnds > now || acct.HarmfulTravelLockEnds > now)
            {
                source = Mobile.CriminalitySource.Harmful;
                criminalEnds = acct.HarmfulCriminalityEnds;
                whackEnds = acct.HarmfulGuardWhackEnds;
                travelEnds = acct.HarmfulTravelLockEnds;
            }
            else if (acct.AFKCriminalityEnds > now || acct.AFKGuardWhackEnds > now || acct.AFKTravelLockEnds > now)
            {
                source = Mobile.CriminalitySource.AFK;
                criminalEnds = acct.AFKCriminalityEnds;
                whackEnds = acct.AFKGuardWhackEnds;
                travelEnds = acct.AFKTravelLockEnds;
            }
            else if (acct.ThievingCriminalityEnds > now || acct.ThievingGuardWhackEnds > now || acct.ThievingTravelLockEnds > now)
            {
                source = Mobile.CriminalitySource.Thieving;
                criminalEnds = acct.ThievingCriminalityEnds;
                whackEnds = acct.ThievingGuardWhackEnds;
                travelEnds = acct.ThievingTravelLockEnds;
            }

            if (source == null)
                return;

            // Convert account end-times into remaining durations and restart runtime enforcement.
            TimeSpan grayRem = RemainingUntil(criminalEnds, now);
            TimeSpan whackRem = RemainingUntil(whackEnds, now);
            TimeSpan travelRem = RemainingUntil(travelEnds, now);

            // NOTE: Rehydrate runtime criminality from account-authoritative end-times.
            // This restarts in-memory timers/flags ONLY and must not modify account end-times
            // (credit for time served is preserved).
            pm.ApplyCriminality(
                source.Value,
                gray: grayRem,
                whack: whackRem,
                travel: travelRem,
                ctx: null,
                regionInform: false,
                message: false,
                writeAccountEnds: false);
        }

        private static void CleanupDuplicateItems(Mobile newChar, bool orEquivalent)
        {
            List<Item> BackPacklist = Utility.GetBackpackItems(newChar);
            List<Item> Equipedlist = Utility.GetEquippedItems(newChar);

            // first, remove 'same items' from backpack that may equipped.
            foreach (Item eitem in Equipedlist)
            {
                if (eitem == null || eitem.Deleted) continue;
                foreach (Item bitem in BackPacklist)
                {
                    if (bitem == null || bitem.Deleted) continue;
                    // allow a dagger along with another weapon
                    bool oneIsDagger = bitem.GetType() == typeof(Dagger) || eitem.GetType() == typeof(Dagger);
                    if (SameItem(eitem, bitem, orEquivalent: oneIsDagger ? false : true))
                    {
                        newChar.Backpack.RemoveItem(bitem);
                        // example: They selected Arms Lore and got a club. They also selected Mace Fighting and got another club
                        bitem.Delete();
                    }
                }
            }
            // next remove duplicate items from backpack
            foreach (Item item1 in BackPacklist)
            {
                if (item1 == null || item1.Deleted) continue;
                foreach (Item item2 in BackPacklist)
                {
                    if (item2 == null || item2.Deleted) continue;
                    if (item1 == item2) continue;
                    // allow a dagger along with another weapon
                    bool oneIsDagger = item1.GetType() == typeof(Dagger) || item2.GetType() == typeof(Dagger);
                    if (SameItem(item1, item2, orEquivalent: oneIsDagger ? false : true))
                    {
                        newChar.Backpack.RemoveItem(item2);
                        item2.Delete();
                    }
                }
            }
        }
        public static bool SameItem(Item item1, Item item2, bool orEquivalent)
        {
            // looks to be the same newbie gear orEquivalent
            if (item1.GetType() == item2.GetType() || (orEquivalent && IsGear(item1, item2) && SameBase(item1, item2) && SameLayer(item1, item2)))
                if (item1.LootType == item2.LootType || orEquivalent)
                    if (item1.Layer == item2.Layer)
                        if (item1.PlayerCrafted == item2.PlayerCrafted || orEquivalent)
                        {
                            if (item1 is BaseClothing bc1 && item2 is BaseClothing bc2)
                            {
                                if (bc1.Quality == bc2.Quality && bc1.Crafter == bc2.Crafter || orEquivalent)
                                    if (NearSamePowerAttributes(item1, item2))
                                        return true;
                            }
                            else if (item1 is BaseArmor ba1 && item2 is BaseArmor ba2)
                            {
                                if (ba1.Quality == ba2.Quality && ba1.Crafter == ba2.Crafter || orEquivalent)
                                    if (NearSamePowerAttributes(item1, item2))
                                        return true;
                            }
                            else if (item1 is BaseWeapon bw1 && item2 is BaseWeapon bw2)
                            {
                                if (bw1.Quality == bw2.Quality && bw1.Crafter == bw2.Crafter || orEquivalent)
                                    if (NearSamePowerAttributes(item1, item2))
                                        return true;
                            }
                            else
                            {   // just an item: bandage, cheese, etc.
                                //  note: orEquivalent does not apply to items like cheese, bandages, etc because IsGear == false
                                return true;
                            }
                        }

            // sufficiently different
            return false;
        }
        private static bool SameLayer(Item item1, Item item2)
        {
            if (item1 == null || item2 == null)
                return false;
            return item1.Layer == item2.Layer;
        }
        private static bool IsGear(Item item1, Item item2)
        {
            if (item1 is BaseClothing bc1 && item2 is BaseClothing bc2)
            {
                return true;
            }
            else if (item1 is BaseArmor ba1 && item2 is BaseArmor ba2)
            {
                return true;
            }
            else if (item1 is BaseWeapon bw1 && item2 is BaseWeapon bw2)
            {
                return true;
            }

            return false;
        }
        private static bool NearSamePowerAttributes(Item item1, Item item2)
        {   // we don't want to grab the players best gear and drop it on this new player.
            //  these tests should provide at best one level +- the current level
            if (item1 is BaseClothing bc1 && item2 is BaseClothing bc2)
            {
                if (bc1.MagicEffect == bc2.MagicEffect)
                    if (bc1.IOBAlignment == bc2.IOBAlignment)
                        return true;
            }
            else if (item1 is BaseArmor ba1 && item2 is BaseArmor ba2)
            {
                if (Math.Abs((int)ba1.ProtectionLevel - (int)ba2.ProtectionLevel) <= 1)
                    if (Math.Abs((int)ba1.DurabilityLevel - (int)ba2.DurabilityLevel) <= 1)
                        if (Math.Abs((int)ba1.ProtectionLevel - (int)ba2.ProtectionLevel) <= 1)
                            if (ba1.IOBAlignment == ba2.IOBAlignment)
                                return true;

            }
            else if (item1 is BaseWeapon bw1 && item2 is BaseWeapon bw2)
            {
                if (Math.Abs((int)bw1.DamageLevel - (int)bw2.DamageLevel) <= 1)
                    if (Math.Abs((int)bw1.AccuracyLevel - (int)bw2.AccuracyLevel) <= 1)
                        if (Math.Abs((int)bw1.DurabilityLevel - (int)bw2.DurabilityLevel) <= 1)
                            return true;
            }
            return false;
        }
        private static bool SameBase(Item item1, Item item2)
        {
            if (item1 is BaseClothing && item2 is BaseClothing)
                return true;
            else if (item1 is BaseArmor && item2 is BaseArmor)
                return true;
            else if (item1 is BaseWeapon && item2 is BaseWeapon)
                return true;

            return false;
        }
    }
}