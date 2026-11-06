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

/* Scripts/Engines/Confinement/ConfinementOperations.cs
 * ChangeLog
 *  10/17/26, Adam
 *      Stage 2 of the confinement architecture: the jail and prison check-in/check-out state changes, pulled out of
 *      the places that did them inline (AIEntrance, AIParoleExit, AIEscapeExit, Prison.GoToPrison, Jail.DoJailing,
 *      JailExitSungate, Utility.Boot) so a later Confinement.Execute can run them. Behaviour is unchanged: each of those
 *      entry points still does its own moving, eligibility checks, messages and logging, and calls these operations at
 *      the same points, in the same order, as before.
 *      Three kinds of change are kept apart:
 *          bookkeeping   - confinement state: inmate/visitor flags, jail records, sentences, prison criminal counts,
 *                          account confinement flags, chat ban, infraction status, murder-count decay timers
 *          consequences  - gameplay results of the way someone leaves or arrives: pack cleanup, escape murder reduction,
 *                          robe/kit, prison pets, Outcast guild, escape leaderboard, mortality
 *          movement      - teleporting and region changes: NOT here, they stay with the callers
 *      None of these methods checks eligibility; callers decide whether a transition is allowed.
 *  10/17/26, Adam (admission refactor R1)
 *      Prison admission possessions (BankPossessions) under the approved policy: dismount, drop what is held, take
 *      contraband, then bank in BankEverything's two bags when everything fits under the bank's own slot rule, otherwise
 *      item by item. Involuntary admissions leave what the bank refuses at their feet (BankThese) and always continue;
 *      ArchiveEverything is not used. Voluntary entry is capacity-checked beforehand (VoluntaryBankFits, read-only, the
 *      bank's real slot accounting incl. potion stacks) and never drops anything. The result says what happened.
 *      Also the read-only voluntary quest-ticket plan, and HasDeathRobe (the entrance's robe test). Additive: nothing
 *      calls these yet except the admission runner (ConfinementAdmission.cs) and Confinement.Plan's voluntary check.
 *  10/17/26, Adam (admission refactor R6)
 *      Removed ConfiscatePossessions (bank or archive): no callers since the prison entrance moved to BankPossessions (R3).
 *  10/18/26, Adam (Stage 5: first confinement wins)
 *      ConfinementBookkeeping.IsConfined (in a jail, or in a prison as an inmate; a trespasser or visitor in a prison is not)
 *      and RecordAdditionalOffense: automated enforcement that finds someone already confined leaves them exactly as they
 *      are and records the new offense for staff (account comment, jail.log or Prison.log, Counselor+ broadcast). A
 *      continuing offense found again and again (Tor, the login infraction) is recorded once, by a stable key matched
 *      against the account comments; discrete events are always recorded.
 *      PrisonOperations.KeepLongerMinimumSentence: an inmate's minimum sentence becomes the later of the two (never
 *      shortened, nothing added); a jail has none.
 */

using Server.Accounting;
using Server.Engines.Chat;
using Server.Engines.CrownSterlingSystem;
using Server.Engines.DataRecorder;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Server.Engines.ConfinementSystem
{
    /// <summary>
    /// What a composite operation did. Applied is false when the target is not subject to it
    /// (the same "m.Player && AccessLevel.Player" guard every jail/prison path uses), in which case nothing changed.
    /// </summary>
    public sealed class ConfinementOpResult
    {
        public bool Applied { get; }
        public IReadOnlyList<string> Steps { get; }
        private ConfinementOpResult(bool applied, IReadOnlyList<string> steps) { Applied = applied; Steps = steps; }
        public static readonly ConfinementOpResult NotApplicable = new(false, Array.Empty<string>());
        internal static ConfinementOpResult Done(List<string> steps) { return new ConfinementOpResult(true, steps); }
        public override string ToString() { return Applied ? string.Join(", ", Steps) : "not applicable"; }
    }

    public enum PossessionsDisposition
    {
        Nothing,        // nothing worn or carried: no message
        Banked,         // everything banked
        PartlyBanked,   // involuntary: some banked, the bank could not hold the rest: left at their feet
        Dropped,        // involuntary: the bank could hold none of it: all left at their feet
        Refused,        // voluntary: the bank refused something after the capacity check; kept by the player, nothing dropped
    }

    /// <summary>What BankPossessions did. Top-level counts are items as banking handles them (a bag is one item).</summary>
    public sealed class PossessionsOutcome
    {
        public PossessionsDisposition Disposition { get; init; }
        public bool Bagged { get; init; }
        public int BankedTop { get; init; }
        public int BankedDeep { get; init; }
        public int RefusedTop { get; init; }
        public int RefusedDeep { get; init; }
        public Point3D Location { get; init; }
        public Map? Map { get; init; }

        public string Where => string.Format("{0}, {1}, {2} {3}", Location.X, Location.Y, Location.Z, Map);

        public override string ToString()
        {
            if (Disposition == PossessionsDisposition.Nothing)
                return "nothing to bank";
            return string.Format("{0}: banked {1} items ({2} deep){3}; {4} {5} items ({6} deep){7}",
                Disposition, BankedTop, BankedDeep, Bagged ? " in bags" : "",
                Disposition == PossessionsDisposition.Refused ? "kept by the player" : "left at their feet", RefusedTop, RefusedDeep,
                Disposition == PossessionsDisposition.Refused || RefusedTop == 0 ? "" : " at " + Where);
        }
    }

    /// <summary>Bookkeeping shared by jail and prison.</summary>
    public static class ConfinementBookkeeping
    {
        /// <summary>A player character the confinement systems apply to (staff and NPCs never are).</summary>
        public static bool Applies(Mobile? m) { return m is PlayerMobile && m.Player && m.AccessLevel == AccessLevel.Player; }

        public static void ImposeChatBan(Mobile m) { ChatHelper.SetChatBan(m, true); }
        public static void LiftChatBan(Mobile m) { ChatHelper.SetChatBan(m, false); }

        /// <summary>Account infraction status back to None (null-safe).</summary>
        public static void ClearInfraction(Mobile m)
        {
            if (m.Account is Account acct)
                acct.InfractionStatus = Account.AccountInfraction.None;
        }

        /// <summary>
        /// Leaving confinement ends permadeath, except on Mortalis, where the character stays mortal
        /// (and the parole exit hides them instead).
        /// </summary>
        public static void ReleaseMortality(PlayerMobile pm, bool hideOnMortalis)
        {
            if (Core.RuleSets.MortalisRules())
            {
                if (hideOnMortalis)
                    pm.Hidden = true;
            }
            else
                pm.Mortal = false;
        }

        #region First confinement wins
        /// <summary>
        /// Legitimately confined: in a jail, or in a prison as an inmate, on any facet. A non-inmate standing in a prison
        /// (a trespasser or a quest visitor) is not confined. A logged-out character is judged by its logout location.
        /// Automated enforcement uses this before any side effect: someone already confined is not confined, moved or
        /// penalized again by the system; the new offense is recorded for staff instead (RecordAdditionalOffense).
        /// </summary>
        public static bool IsConfined(Mobile? m)
        {
            if (m is not PlayerMobile pm || pm.Deleted)
                return false;

            Region? region = pm.Region;
            if (pm.Map == Map.Internal && pm.LogoutMap != null && pm.LogoutMap != Map.Internal)
                region = Region.Find(pm.LogoutLocation, pm.LogoutMap);

            if (region == null)
                return false;
            if (region.IsJailRules)
                return true;
            return region.IsPrisonRules && pm.PrisonInmate;
        }

        /// <summary>
        /// An automated enforcement found the character already confined (IsConfined) and leaves them exactly as they are:
        /// location, confinement, possessions, penalties and sentence. The offense is written to the account comments and
        /// to jail.log or Prison.log, and Counselor+ staff are told, so staff can decide about it.
        /// dedupKeys: for a check that keeps finding the same continuing condition (Tor/IP every two minutes, the login
        /// infraction at every login), stable texts identifying that offense: nothing is recorded when an account comment
        /// already contains one of them (the original incarceration comment, or an earlier record). Null for a discrete
        /// event, which is always recorded. Returns true when the offense was recorded (callers apply any sentence
        /// adjustment only then).
        /// </summary>
        public static bool RecordAdditionalOffense(PlayerMobile pm, string source, string reason, string[]? dedupKeys)
        {
            Account? acct = pm.Account as Account;
            if (dedupKeys != null && acct != null)
                foreach (AccountComment c in acct.Comments)
                    if (c.Content != null && dedupKeys.Any(k => c.Content.Contains(k, StringComparison.Ordinal)))
                        return false;

            Region? region = pm.Map == Map.Internal && pm.LogoutMap != null ? Region.Find(pm.LogoutLocation, pm.LogoutMap) : pm.Region;
            bool jail = region != null && region.IsJailRules;
            string where = string.Format("{0} ({1})", jail ? "jail" : "prison", pm.Map == Map.Internal ? pm.LogoutMap : pm.Map);
            string text = string.Format("Additional offense while confined in {0}; not moved: {1}: {2}", where, source, reason);

            if (acct != null)
                acct.Comments.Add(new AccountComment("SYSTEM", string.Format("{0}\n{1}", DateTime.UtcNow, text)));

            try
            {
                Server.Diagnostics.LogHelper logger = new Server.Diagnostics.LogHelper(jail ? "jail.log" : "Prison.log", false, true);
                logger.Log(Server.Diagnostics.LogType.Mobile, pm, text);
                logger.Finish();
            }
            catch (Exception ex)
            {
                Server.Diagnostics.LogHelper.LogException(ex);
            }

            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, "Staff message from SYSTEM:");
            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482,
                string.Format("{0} is already confined in {1}: additional offense recorded, not moved. ({2}: {3})", pm, where, source, reason));
            Console.WriteLine("Confinement: {0}: {1}", pm, text);
            return true;
        }

        /// <summary>
        /// The Tor offense, as either check writes it (HandleIpCheater: the IP flags; BadLogin: the TorExitNode login
        /// infraction): one offense for RecordAdditionalOffense, whichever check imprisoned them or recorded it first.
        /// </summary>
        public static readonly string[] TorOffenseKeys = { "Player blackholed with IP Flags", "Player blackholed with LoginStatus TorExitNode." };
        #endregion First confinement wins
    }

    public static class PrisonOperations
    {
        #region Bookkeeping
        /// <summary>Check-in: the inmate (or quest visitor) flag. Setting the inmate flag switches murder-count decay to the prison rate.</summary>
        public static void SetInmateFlag(PlayerMobile pm, bool visitor)
        {
            if (visitor)
                pm.PrisonVisitor = true;
            else
                pm.PrisonInmate = true;
        }

        /// <summary>Check-out: inmate and visitor flags. Clearing the inmate flag switches murder-count decay back to the normal rate.</summary>
        public static void ClearInmateFlags(PlayerMobile pm)
        {
            pm.PrisonInmate = false;
            pm.PrisonVisitor = false;
        }

        /// <summary>Account flag used by Mortalis-style rules to re-imprison a character on login.</summary>
        public static void MarkAccountImprisoned(Mobile m)
        {
            if (Core.RuleSets.MortalisStyleRules() && m.Account is Account acct)
                acct.IsImprisoned = true;
        }

        public static void ClearAccountImprisoned(Mobile m)
        {
            if (m.Account is Account acct)
                acct.IsImprisoned = false;
        }

        /// <summary>
        /// Prison sentence: short-term criminal counts are the sentence served in prison; a sentence also counts as one
        /// more trip to prison (long-term criminal counts). These are criminal counts, not murder counts.
        /// </summary>
        public static void AddPrisonSentence(PlayerMobile pm, uint sentence)
        {
            pm.ShortTermCriminalCounts += sentence;
            if (sentence > 0)
                pm.LongTermCriminalCounts++;
        }

        /// <summary>The remaining prison sentence counts (short-term criminal counts). Murder counts are untouched.</summary>
        public static void ClearPrisonCriminalCounts(PlayerMobile pm) { pm.ShortTermCriminalCounts = 0; }

        public static void ClearMinimumSentence(PlayerMobile pm) { pm.MinimumSentence = DateTime.MinValue; }

        /// <summary>
        /// First confinement wins: for an inmate in a prison, the minimum sentence becomes the later of the current one and
        /// until (never shortened, nothing added). A jail has no minimum sentence: nothing changes there. True if it changed.
        /// </summary>
        public static bool KeepLongerMinimumSentence(PlayerMobile pm, DateTime until)
        {
            if (pm.Region == null || !pm.Region.IsPrisonRules || !pm.PrisonInmate)
                return false;
            if (pm.MinimumSentence >= until)
                return false;
            pm.MinimumSentence = until;
            return true;
        }

        /// <summary>Account flag set when a login exceeded the machine-info limit (cleared on parole).</summary>
        public static void ClearMachineInfoLimitFlag(Mobile m)
        {
            if (m.Account is Account acct)
                acct.SetFlag(Account.AccountFlags.ExceedsMachineInfoLimit, false);
        }

        /// <summary>
        /// Restart murder-count decay timers at the rate for the current inmate state (MurderCountUtility.ResetKillTimers).
        /// Counts themselves are unchanged. Call after the inmate flag has been cleared.
        /// </summary>
        public static void RestartMurderDecay(PlayerMobile pm) { pm.ResetKillTime(); }

        /// <summary>
        /// [boot ex only: the account's login history and the character's long-term criminal counts.
        /// Never part of an ordinary release or transfer.
        /// </summary>
        public static void ClearAccountHistory(PlayerMobile pm, Account acct)
        {
            acct.LoginIPs = Array.Empty<IPAddress>();
            acct.ClearGAMELogin();
            acct.ClearMachines();
            pm.LongTermCriminalCounts = 0;
        }
        #endregion Bookkeeping

        #region Consequences
        /// <summary>Parole: everything in the pack goes except containers (no lighthouse pass, no souvenir).</summary>
        public static void ParolePackCleanup(Mobile m) { AITeleportHelper.EmptyPackOnExit(m, true); }

        /// <summary>Escape: rares and smuggled goods are kept, plus a souvenir.</summary>
        public static void EscapePackCleanup(Mobile m) { AITeleportHelper.EmptyPackOnExit(m, false); }

        /// <summary>
        /// Escape only: short-term murders of 10 or more are halved, 6-9 drop to 5; never below 5 by design.
        /// Returns the number removed (the player is told, as before).
        /// </summary>
        public static int ReduceEscapeMurders(PlayerMobile pm)
        {
            int oldSTC = pm.ShortTermMurders;
            int newSTC = pm.ShortTermMurders;

            if (pm.ShortTermMurders >= 10)
                newSTC = pm.ShortTermMurders / 2;
            else if (pm.ShortTermMurders > 5)
                newSTC = 5;

            pm.ShortTermMurders = newSTC;

            if (oldSTC - newSTC > 0)
                pm.SendMessage("Your short term murders have been reduced to {0}", pm.ShortTermMurders);

            return oldSTC - newSTC;
        }

        /// <summary>Remove them from The Outcast guild (the prison guild).</summary>
        public static void LeaveOutcastGuild(PlayerMobile pm)
        {
            if (pm != null && pm.Guild != null && pm.Guild.Abbreviation.Contains("ToC", StringComparison.OrdinalIgnoreCase))
                ((Guild)pm.Guild).ResignMember(pm, force: true);
        }

        /// <summary>Escape leaderboard credit.</summary>
        public static void RecordEscape(Mobile m)
        {
            DataRecorder.DataRecorder.Add(m, RecorderDomain.PQEscapes, 1);
            DataRecorder.DataRecorder.Add(m, RecorderDomain.PQRares, CountRares(m));
            DataRecorder.DataRecorder.Add(m, RecorderDomain.PQSterling, m.Backpack.GetAmount(typeof(Sterling)));
        }

        private static int CountRares(Mobile m)
        {
            if (m != null && m.Backpack != null)
            {
                List<Item> list = new(m.Backpack.GetDeepItems().Cast<Item>().ToList());
                if (m.Items != null && list.Count > 0)
                    list.AddRange(m.Items.Cast<Item>().ToList());
                int count = 0;
                foreach (Item item in list)
                    if (item.GetFlag(LootType.Rare))
                        count++;

                return count;
            }
            return 0;
        }

        /// <summary>Death robe (if alive), stinger and a blank spellbook.</summary>
        public static void IssueInmateKit(Mobile m)
        {
            // Give them a Deathrobe, Stinger dagger, and a blank spell book
            if (m.Alive)
            {
                RemoveDeathRobes(m);
                // Adam, 8/6/21: Hue the death robe orange. kind of a funky orange hue. Look for a better one
                DeathRobe robe = new Server.Items.DeathRobe();
                robe.Hue = 0x2b;
                if (!m.EquipItem(robe))
                    robe.Delete();
            }

            Item aiStinger = new AIStinger();
            if (!m.AddToBackpack(aiStinger))
                aiStinger.Delete();

            Item spellbook = new Spellbook();
            if (!m.AddToBackpack(spellbook))
                spellbook.Delete();
        }

        #region Admission possessions (approved policy D3/D4)
        /// <summary>
        /// Free slots in their bank box under TryDropItem's own rule (Container slot accounting: potions cost a slot each).
        /// Read-only: a missing bank box counts as the empty one the admission would create.
        /// </summary>
        public static int FreeBankSlots(Mobile m)
        {
            BankBox? bank = m.FindBankNoCreate();
            int max = bank != null ? bank.MaxItems : Container.GlobalMaxItems;
            if (max == 0)
                return int.MaxValue;
            return max - (bank?.SlotUsage ?? 0);
        }

        /// <summary>Slots these items take in a container (Container.GetSlotCost, as TryDropItem charges them).</summary>
        public static int SlotCost(IEnumerable<Item> items) { return items.Sum(i => Container.GetSlotCost(i)); }

        private static int DeepCount(IEnumerable<Item> items) { return items.Sum(i => 1 + i.TotalItems); }

        /// <summary>
        /// BankEverything's layout: -1 when it does not all fit; otherwise 0, 1 or 2 bags (one for the backpack contents,
        /// one for the equipment), each bag costing one more slot.
        /// </summary>
        public static int BagsThatFit(int free, int need)
        {
            if (need > free)
                return -1;
            int bags = 0;
            if (need + 1 <= free)
                bags = 1;
            if (need + 2 <= free)
                bags = 2;
            return bags;
        }

        /// <summary>
        /// Read-only (voluntary entry): which quest tickets the entrance keeps and deletes. A murderer is never a visitor
        /// and loses every quest ticket; anyone else keeps their newest valid ticket (and is a visitor) and loses the rest.
        /// </summary>
        public static bool VoluntaryTicketPlan(Mobile m, out Ticket? keep, out List<Ticket> delete)
        {
            List<Ticket> tickets = AIEntrance.GetAnyQuestTicket(m);
            keep = null;
            if (!m.IsMurderer)
                foreach (Ticket t in tickets)
                    if (t != null && !t.Deleted && !t.Expired && (keep == null || t.Created > keep.Created))
                        keep = t;
            Ticket? kept = keep;
            delete = tickets.Where(t => t != null && !t.Deleted && t != kept).ToList();
            return kept != null;
        }

        /// <summary>
        /// Read-only (voluntary entry): would the entrance's banking fit under the bank's real slot rule? Counts what will
        /// actually be banked: everything worn and in the backpack plus the item on the cursor (it goes into the pack),
        /// less the top-level lighthouse passes (deleted as contraband) and the quest tickets the entrance deletes.
        /// Conservative where stacking onto an existing pile would have saved slots. count: the item count the entrance's
        /// refusal message has always shown (worn items + everything in the backpack, the held item included).
        /// </summary>
        public static bool VoluntaryBankFits(Mobile m, IEnumerable<Item> deletedTickets, out int count)
        {
            List<Item> belongings = Utility.GetBackpackItems(m), equipment = Utility.GetEquippedItems(m);
            int need = SlotCost(belongings) + SlotCost(equipment)
                + (m.Dragging != null ? Container.GetSlotCost(m.Dragging) : 0)
                - SlotCost(belongings.Where(i => i is LightHousePass))
                - SlotCost(deletedTickets);

            count = equipment.Count + (m.Backpack?.GetDeepItems().Count ?? 0) + (m.Dragging != null ? 1 + m.Dragging.TotalItems : 0);
            return need <= FreeBankSlots(m);
        }

        /// <summary>
        /// Prison admission possessions. As the prison entrance always has: dismount, drop what is held, delete contraband (top-level
        /// lighthouse passes). Then everything in the backpack and everything worn goes to the bank: in BankEverything's
        /// bags when it all fits under the bank's slot rule, otherwise item by item.
        ///   involuntary: whatever the bank refuses is left at their feet, here, before any teleport (BankThese); never fails
        ///   voluntary (capacity checked by the caller first): nothing is dropped; if the bank still refuses something it
        ///     stays with the player and the outcome is Refused
        /// Tells the player what happened (no message when there is nothing to bank). Does not issue the inmate kit.
        /// </summary>
        public static PossessionsOutcome BankPossessions(Mobile m, bool voluntary)
        {
            IMount mt = m.Mount;
            if (mt != null)
                mt.Rider = null;

            Utility.DropHolding(m);

            foreach (Item i in new List<Item>(m.Backpack.Items))
                if (i is LightHousePass)
                    i.Delete();

            List<Item> belongings = Utility.GetBackpackItems(m), equipment = Utility.GetEquippedItems(m);
            Point3D at = m.Location;
            Map map = m.Map;
            if (belongings.Count == 0 && equipment.Count == 0)
                return new PossessionsOutcome { Disposition = PossessionsDisposition.Nothing, Location = at, Map = map };

            int deep = DeepCount(belongings) + DeepCount(equipment);
            int bags = BagsThatFit(FreeBankSlots(m), SlotCost(belongings) + SlotCost(equipment));
            List<Item> refused = new();
            int banked = 0;

            if (bags < 0 && voluntary)
                refused.AddRange(belongings.Concat(equipment));     // the capacity check should have refused them: nothing moves
            else
            {
                Teleporter.RefusedItems onRefusal = voluntary ? Teleporter.RefusedItems.KeepWithOwner : Teleporter.RefusedItems.DropAtFeet;
                // bags only when everything fits; the equipment gets its own bag only when there is room for both
                //  (BankEverything decided that after the backpack was emptied, so one bag's room could be spent twice)
                banked += Teleporter.BankThese(m, belongings, bags > 0, onRefusal, refused);
                banked += Teleporter.BankThese(m, equipment, bags > 1 || (bags > 0 && belongings.Count == 0), onRefusal, refused);
            }

            int refusedDeep = DeepCount(refused);
            PossessionsDisposition d = refused.Count == 0 ? PossessionsDisposition.Banked
                : voluntary ? PossessionsDisposition.Refused
                : banked == 0 ? PossessionsDisposition.Dropped
                : PossessionsDisposition.PartlyBanked;

            switch (d)
            {
                case PossessionsDisposition.Banked:
                    m.SendMessage("Your worldly possessions have been placed in your bank for safekeeping.");
                    break;
                case PossessionsDisposition.PartlyBanked:
                    m.SendMessage("Some of your possessions have been placed in your bank for safekeeping. Your bank box could not hold the rest ({0} items); they have been left at your feet.", refused.Count);
                    break;
                case PossessionsDisposition.Dropped:
                    m.SendMessage("Your bank box cannot hold your possessions. They have been left at your feet.");
                    break;
                case PossessionsDisposition.Refused:
                    m.SendMessage("Your items cannot be deposited in your bank box because it is full.");
                    m.SendMessage("Come back when you have emptied your bank box a bit.");
                    break;
            }

            return new PossessionsOutcome
            {
                Disposition = d,
                Bagged = bags > 0,
                BankedTop = banked,
                BankedDeep = deep - refusedDeep,
                RefusedTop = refused.Count,
                RefusedDeep = refusedDeep,
                Location = at,
                Map = map,
            };
        }

        /// <summary>The entrance's robe test: a death robe worn or carried at the top level of their items.</summary>
        public static bool HasDeathRobe(Mobile m)
        {
            if (m != null && m.Items != null)
                foreach (Item item in m.Items)
                    if (item is DeathRobe)
                        return true;

            return m?.FindItemOnLayer(Layer.OuterTorso) is DeathRobe;
        }
        #endregion Admission possessions

        private static void RemoveDeathRobes(Mobile m)
        {
            if (m != null && m.Items != null)
            {
                List<Item> list = new List<Item>(m.Items);
                foreach (Item item in list)
                {
                    if (item is DeathRobe)
                    {
                        if (item.Parent is Mobile parent)
                            parent.RemoveItem(item);
                        else if (item.Parent is Container container)
                            container.RemoveItem(item);

                        item.Delete();
                    }
                }
            }
        }
        #endregion Consequences

        #region Check-out composites (call AFTER moving them out, as the exits do)
        /// <summary>
        /// Leaving through the parole exit (also used by staff boots and the cron "nothing left to serve" kick,
        /// which run the parole exit). Same steps, same order as AIParoleExit.DoTeleport.
        /// </summary>
        public static ConfinementOpResult ParoleCheckOut(Mobile m)
        {
            if (!ConfinementBookkeeping.Applies(m))
                return ConfinementOpResult.NotApplicable;

            PlayerMobile pm = (PlayerMobile)m;
            List<string> steps = new();

            ParolePackCleanup(pm); steps.Add("parole pack cleanup");
            ClearInmateFlags(pm); steps.Add("inmate/visitor flags cleared");
            pm.MuteMessages = false; steps.Add("messages unmuted");
            ClearAccountImprisoned(pm); steps.Add("account imprisoned flag cleared");
            ConfinementBookkeeping.ReleaseMortality(pm, hideOnMortalis: true); steps.Add("mortality released");
            RestartMurderDecay(pm); steps.Add("murder decay restarted");
            ClearMinimumSentence(pm); steps.Add("minimum sentence cleared");
            ConfinementBookkeeping.ClearInfraction(pm); steps.Add("infraction cleared");
            ClearMachineInfoLimitFlag(pm); steps.Add("machine-info flag cleared");
            ConfinementBookkeeping.LiftChatBan(pm); steps.Add("chat ban lifted");
            LeaveOutcastGuild(pm); steps.Add("Outcast guild left");
            AIPUtils.DecolorizeRobe(pm); steps.Add("robe decolorized");
            AIPUtils.KillPrisonPets(pm); steps.Add("prison pets removed");

            return ConfinementOpResult.Done(steps);
        }

        /// <summary>
        /// Leaving through the escape exit. Same steps, same order as AIEscapeExit.DoTeleport. Unlike parole, an escape
        /// keeps the minimum sentence, infraction status, mute, machine-info flag and guild, and reduces murder counts.
        /// </summary>
        public static ConfinementOpResult EscapeCheckOut(Mobile m)
        {
            if (!ConfinementBookkeeping.Applies(m))
                return ConfinementOpResult.NotApplicable;

            PlayerMobile pm = (PlayerMobile)m;
            List<string> steps = new();

            EscapePackCleanup(pm); steps.Add("escape pack cleanup");
            ConfinementBookkeeping.LiftChatBan(pm); steps.Add("chat ban lifted");
            ReduceEscapeMurders(pm); steps.Add("escape murder reduction");
            ClearInmateFlags(pm); steps.Add("inmate/visitor flags cleared");
            ClearAccountImprisoned(pm); steps.Add("account imprisoned flag cleared");
            ConfinementBookkeeping.ReleaseMortality(pm, hideOnMortalis: false); steps.Add("mortality released");
            RestartMurderDecay(pm); steps.Add("murder decay restarted");
            AIPUtils.DecolorizeRobe(pm); steps.Add("robe decolorized");
            AIPUtils.Undress(pm); steps.Add("undressed");
            AIPUtils.KillPrisonPets(pm); steps.Add("prison pets removed");
            RecordEscape(pm); steps.Add("escape recorded");

            return ConfinementOpResult.Done(steps);
        }
        #endregion Check-out composites
    }

    public static class JailOperations
    {
        /// <summary>Check-in: the jail sentence (JailSystem record, release time = now + hours).</summary>
        public static void RecordSentence(Mobile m, int hours) { JailSystem.AddInmate(m, hours); }

        /// <summary>Check-out: the jail record.</summary>
        public static void RemoveRecord(Mobile m) { JailSystem.RemoveInmate(m); }

        /// <summary>Account flag used by Mortalis-style rules to re-jail a character on login.</summary>
        public static void MarkAccountJailed(Mobile m)
        {
            if (m.Account is Account acct && Core.RuleSets.MortalisStyleRules())
                acct.IsJailed = true;
        }

        public static void ClearAccountJailed(Mobile m)
        {
            if (m.Account is Account acct)
                acct.IsJailed = false;
        }
    }
}
