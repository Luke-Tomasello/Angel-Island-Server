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

/* Scripts/Engines/Help/HelpStuck/HelpStuckRescue.cs
 * Changelog:
 *  10/15/2026, Adam (Help Stuck: HelpStuck.log history)
 *      HelpStuck.log now records each refusal at request, with what the player was told: Refused stuck player:
 *      "<text>" (a cliloc refusal by its text, Cliloc.Lookup; no message of its own, e.g. a region's refusal: the
 *      generic message Help Stuck sends). Normal requests only, not a test.
 *      Stranded at sea and stranded on a boat are now logged like every other move (Moved stuck player to ...),
 *      just before the move, and are moved to the shore resolved when the plan was made (plan.Destination) instead
 *      of Strandedness.ProcessStranded searching for it again. The "not moved" fallback is gone with that second
 *      search.
 *  10/14/2026, Adam (Help Stuck: stranded on a boat)
 *      StrandedOnABoat is carried out as stranded at sea is: Strandedness.ProcessStranded at once, no ticket, the
 *      player only (pets are not moved, Publish 15), one use charged only if they were moved. The trace notes where
 *      they were put ashore.
 *  10/14/2026, Adam (Help Stuck: Z correction removed, local unblock destinations)
 *      Z correction is gone (see StuckRules). When the rules note a suspicious circumstance (StuckTrace.Suspicion:
 *      blocked only by movable items), Request records the scene with RecordCheater.Cheater, possible accomplices
 *      included, as a possible auto-move exploit attempt: a record for staff, not a finding, and no sanction. Not
 *      in a test ([HelpStuckTest): the trace says it would be recorded.
 *  10/14/2026, Adam (Help Stuck: stranded at sea)
 *      Stranded at sea: one use is charged only if Strandedness.ProcessStranded actually moved them (it now says
 *      so); otherwise they are told, and nothing is charged. Pets are still not moved (Publish 15).
 *  10/13/2026, Adam (Help Stuck refactor)
 *      First time checkin. HelpStuck carries out what StuckRules decide: the request (DoHelpStuck), the
 *      delayed rescue (RescueTicket, formerly StuckMenu.TeleportTimer) and its execution. Behavior is unchanged.
 *      New: [HelpStuckTest is a dry run that reports the decision and changes nothing; [HelpStuckTest live is
 *      the previous [HelpStuckTest. Each request and each expiry writes one line to HelpStuckDecisions.log.
 *      HouseRescue: an owner, co-owner or friend of the house is moved to its ban location, pets too, with no use
 *      charged.
 *      Uses: Charge() charges a chargeable rescue exactly once, when it is carried out (stranded at sea: at once;
 *      delayed rescues: at expiry, after the move). Nothing is charged when a rescue is scheduled or cancelled.
 *      Exit hints: each distinct hint is said once (HelpStuckProbes.ExitHintLines); three teleporter tiles at a
 *      dungeon entrance no longer give "There's a teleporter nearby!" three times.
 *      Message channels documented (no change): exit hints overhead, every other Help Stuck message in the system channel.
 *      DungeonUnblock is carried out exactly as a LocalUnblock (scheduled, pets moved, one use charged on success).
 */

/* HelpStuck - overview
 *  Request:  take a StuckStatus, ask StuckRules.PlanRequest for a RescuePlan, then act on it: schedule a
 *            RescueTicket, move them ashore, describe exits, refuse, or open the town menu.
 *            Stranded at sea and stranded on a boat are carried out at once, with no ticket: the player, not their
 *            pets (Publish 15), is moved to the shore Strandedness found when the plan was made (plan.Destination).
 *  Log:      HelpStuck.log is the history: each move carried out ("Moved stuck player to ...", written just before
 *            the move, so the record's location is where they were) and each refusal at request ("Refused stuck
 *            player: ..." with what they were told), for normal requests (a [HelpStuckTest live house rescue is logged
 *            too). HelpStuckDecisions.log holds the reasoning.
 *  Ticket:   one per mobile (a new request replaces the old). Keeps them frozen until it comes due, and hidden
 *            if it is a dungeon evacuation. A ticket only counts down; it decides nothing.
 *  Expiry:   StuckRules.ValidAtExpiry on fresh facts. If valid (or forced): log, recovery chest for a dungeon
 *            evacuation, move pets and player, charge one use (if chargeable).
 *  Messages: two channels, by meaning. Spatial, world-oriented exit hints ("There's a teleporter nearby!") are
 *            local overhead messages over the player. Everything else Help Stuck says (judgments, refusals,
 *            status, scheduling and other service messages) uses the normal system channel. A dry run reports
 *            everything, hints included, as system text: it is a report for staff.
 */

using Server.Diagnostics;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Menus.Questions
{
    public static class HelpStuck
    {
        private const string NotStuck = "You do not appear to be in a valid stuck location. Use help-stuck again for more information.";
        private const string SuspicionRecord = "Possible Help Stuck auto-move exploit attempt: blocked only by movable items; local unblock not given.";

        #region Request
        // StuckMenu.DoHelpStuck. Always handled here (true): Help Stuck never falls back to a staff page.
        public static bool Request(Mobile from, StuckMode mode)
        {
            Utility.Monitor.WriteLine(string.Format("Player {0} is unstucking...{1}", from, mode == StuckMode.DryRun ? " (dry run)" : ""), ConsoleColor.Magenta);

            StuckTrace trace = new StuckTrace();
            StuckStatus status = new StuckStatus(from, mode);
            RescuePlan plan = StuckRules.PlanRequest(status, trace);
            trace.Outcome = plan.Kind.ToString();

            // a suspicious circumstance the rules noted (blocked only by movable items): record the scene and the
            //  players nearby for staff to look at later. A record, not a finding: nothing else happens to the player.
            if (trace.Suspicion != null)
            {
                if (mode == StuckMode.Normal)
                {
                    Server.Commands.RecordCheater.Cheater(from, SuspicionRecord, accomplice: true);
                    trace.Note("recorded for staff (RecordCheater), with possible accomplices");
                }
                else
                    trace.Note("would be recorded for staff (RecordCheater); not in a test");
            }

            if (mode == StuckMode.DryRun)
            {
                from.SendMessage($"Help Stuck dry run for {from.Name} (judged as a player, nothing changed):");
                foreach (string step in trace.Steps)
                    from.SendMessage(step);
                foreach (string line in StuckRules.Describe(status, plan))
                    from.SendMessage(line);
                return true;
            }

            Act(from, status, plan, mode, trace);
            StuckRules.LogDecision(from, "request", trace);
            return true;
        }

        private static void Act(Mobile from, StuckStatus status, RescuePlan plan, StuckMode mode, StuckTrace trace)
        {
            RescuePolicy policy = plan.Policy;

            switch (plan.Kind)
            {
                case RescueKind.Refused:
                    if (plan.Refusal != null)
                        plan.Refusal.Value.Send(from);
                    else
                        from.SendMessage(NotStuck);
                    if (mode == StuckMode.Normal)
                        LogStuck(from, $"Refused stuck player: \"{RefusalText(plan.Refusal)}\"");
                    Utility.Monitor.WriteLine("We do not allow them to unstuck.", ConsoleColor.Magenta);
                    break;

                case RescueKind.StrandedAtSea:
                case RescueKind.StrandedOnABoat:    // separate states (StuckRules), carried out the same way
                    // the shore Strandedness resolved when the plan was made (StuckStatus.Shore), moments ago in this
                    //  request; it is not searched for again. The player only: pets are not moved (Publish 15, by
                    //  design; no TeleportPets here).
                    if (mode == StuckMode.Normal)
                        LogMove(from, plan.Destination, plan.Map!);
                    from.MoveToWorld(plan.Destination, plan.Map!);
                    Utility.Monitor.WriteLine("We unblocked them.", ConsoleColor.Magenta);
                    trace.Note($"moved ashore to {from.Location} {from.Map}; pets not moved");
                    Charge(from, policy, mode, trace);   // carried out now
                    break;

                case RescueKind.LocalUnblock:
                case RescueKind.DungeonUnblock: // the same move; only the policy's exemptions differ
                    if (mode == StuckMode.LiveTest)
                    {   // [HelpStuckTest live: move now
                        BaseCreature.TeleportPets(from, plan.Destination, from.Map);
                        from.MoveToWorld(plan.Destination, from.Map);
                    }
                    else
                    {
                        Schedule(from, plan.Destination, plan.Map!, policy.Delay!(), policy.Force, policy);
                        from.SendMessage(policy.ScheduledMessage);
                    }
                    Utility.Monitor.WriteLine("We unblocked them.", ConsoleColor.Magenta);
                    // no use yet: charged when the move happens (a live test moves now and is not charged)
                    break;

                case RescueKind.HouseRescue:
                    if (mode == StuckMode.LiveTest)
                        RescueFromHouse(from, plan.House!);  // [HelpStuckTest live: move now
                    else
                    {
                        Schedule(from, plan.Destination, plan.Map!, policy.Delay!(), policy.Force, policy, plan.House);
                        from.SendMessage(policy.ScheduledMessage);
                    }
                    Utility.Monitor.WriteLine("We are moving them out of their house.", ConsoleColor.Magenta);
                    break;

                case RescueKind.ExitHints:
                    Utility.Monitor.WriteLine("Not hopeless, describing options.", ConsoleColor.Magenta);
                    // 5/19/23, Yoar: Let's not use up a stuck attempt here
                    // overhead, by design: an exit hint is the character noticing a way out, so it is shown where
                    //  they are looking. Every other Help Stuck message (judgments, refusals, status, scheduling) is
                    //  the service speaking and goes to the system channel (see overview).
                    foreach (string line in HelpStuckProbes.ExitHintLines(from, status.ExitOptions))
                        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, line);
                    break;

                case RescueKind.QuestArea:
                    Utility.Monitor.WriteLine("Probably a quest area. Too bad.", ConsoleColor.Magenta);
                    from.SendMessage("You appear to be in a quest area. Sorry, there is no help stuck available here.");
                    break;

                case RescueKind.TownChoice:
                    Utility.Monitor.WriteLine("Giving up, getting them some help.", ConsoleColor.Magenta);
                    // 4/4/23, Adam: just give them the menu, currently I don't trust we will have enough staff to field requests
                    StuckMenu menu = new StuckMenu(from, from, true);
                    menu.BeginClose();
                    from.SendGump(menu);
                    break;
            }
        }

        // HelpStuck.log, the history of what Help Stuck did: each move carried out ("Moved stuck player to ...",
        //  logged just before the move, so the record's location is where they were) and each refusal at request,
        //  with what the player was told. The reasoning behind a decision is in HelpStuckDecisions.log.
        private static void LogStuck(Mobile m, string text)
        {
            LogHelper log = new LogHelper("HelpStuck.log");
            log.Log(LogType.Mobile, m, text);
            log.Finish();
        }

        private static void LogMove(Mobile m, Point3D to, Map map)
        {
            LogStuck(m, string.Format("Moved stuck player to {0} ({1}) (T2A={2})",
                to, map, Utility.WorldMap.LostLandsWrap.Contains(to)));
        }

        // what a refusal said to the player: its own text, a cliloc's text, or (no message of its own, e.g. a region
        //  that refused, which says its own piece) the generic message Act sends
        private static string RefusalText(StuckRules.GateMessage? refusal)
        {
            if (refusal == null)
                return NotStuck;

            if (refusal.Value.Text != null)
                return refusal.Value.Text;

            return Server.Text.Cliloc.Lookup.TryGetValue(refusal.Value.Number, out string? text) ? text : refusal.Value.ToString();
        }
        #endregion Request

        #region Ticket
        private static readonly Dictionary<Mobile, RescueTicket> m_Table = new Dictionary<Mobile, RescueTicket>();

        public static void Schedule(Mobile m, Point3D targetLoc, Map targetMap, TimeSpan delay, bool force, RescuePolicy policy, BaseHouse? house = null)
        {
            new RescueTicket(m, targetLoc, targetMap, delay, force, policy, house).Start();
        }

        internal static void Register(RescueTicket ticket)
        {
            if (m_Table.TryGetValue(ticket.Mobile, out RescueTicket? concurrent))
                concurrent.Stop();

            m_Table[ticket.Mobile] = ticket;
        }

        internal static void Unregister(RescueTicket ticket)
        {
            m_Table.Remove(ticket.Mobile);
        }
        #endregion Ticket

        #region Expiry
        internal static void Expire(RescueTicket ticket)
        {
            Mobile m = ticket.Mobile;
            StuckTrace trace = new StuckTrace();

            bool valid = StuckRules.ValidAtExpiry(ticket, trace, out StuckRules.GateMessage? message);

            // a failing gate says why, forced or not (a forced rescue goes ahead regardless)
            message?.Send(m);

            // If we're not forcing and the move is not valid, don't teleport.
            if (!ticket.Force && !valid)
            {
                if (message == null)
                    m.SendMessage(NotStuck);

                trace.Note("uses: none charged (cancelled)");
                trace.Outcome = $"{ticket.Policy.Kind} cancelled";
                StuckRules.LogDecision(m, "expiry", trace);
                return; // IMPORTANT: bail out, do not log/teleport
            }

            if (ticket.Policy.Kind == RescueKind.HouseRescue)
            {
                RescueFromHouse(m, ticket.House!);
                Charge(m, ticket.Policy, StuckMode.Normal, trace);  // free: records that none was charged
                trace.Outcome = $"HouseRescue moved to {ticket.House!.BanLocation} {ticket.House.Map}";
                StuckRules.LogDecision(m, "expiry", trace);
                return;
            }

            // From here on we are either forcing or we have a valid move → do the teleport.
            LogHelper log = new LogHelper("HelpStuck.log");
            log.Log(LogType.Mobile, m, string.Format(
                "Moved stuck player to {0} ({1}) (T2A={2})",
                ticket.TargetLocation, ticket.TargetMap, Utility.WorldMap.LostLandsWrap.Contains(ticket.TargetLocation)));
            log.Finish();

            // Apply dungeon evac consequences and notify the player if applicable.
            if (ticket.DungeonEvacuation && m.Alive)
            {
                HelpStuckRecovery.ApplyDungeonEvacuationConsequences(m, ticket.OriginalLocation, ticket.OriginalMap);

                m.SendMessage(
                    "Your belongings have been placed in a chest near your original location, a key has been placed in your pack.");
            }

            BaseCreature.TeleportPets(m, ticket.TargetLocation, ticket.TargetMap);
            m.MoveToWorld(ticket.TargetLocation, ticket.TargetMap);
            Charge(m, ticket.Policy, StuckMode.Normal, trace);

            trace.Outcome = $"{ticket.Policy.Kind} moved to {ticket.TargetLocation} {ticket.TargetMap}{(ticket.Force && !valid ? " (forced)" : "")}";
            StuckRules.LogDecision(m, "expiry", trace);
        }

        // the authorized house rescue: an owner, co-owner or friend is moved to the house ban location, where an
        //  ejection (BaseHouse.Kick) puts people, with their pets. No use is charged.
        //  Not BaseHouse.Kick itself: 'remove thyself' deliberately cannot eject a friend of the house.
        private static void RescueFromHouse(Mobile m, BaseHouse house)
        {
            LogHelper log = new LogHelper("HelpStuck.log");
            log.Log(LogType.Mobile, m, string.Format(
                "Moved stuck player to {0} ({1}) (T2A={2}) (house rescue)",
                house.BanLocation, house.Map, Utility.WorldMap.LostLandsWrap.Contains(house.BanLocation)));
            log.Finish();

            // pets first: they are found around where the player stands
            BaseCreature.TeleportPets(m, house.BanLocation, house.Map);
            m.MoveToWorld(house.BanLocation, house.Map);
        }
        #endregion Expiry

        #region Uses
        // Mobile.UsedStuckMenu allocates two slots; a use frees up 24 hours after it was recorded
        public const int UsesPerDay = 2;
        public static readonly TimeSpan UseLifetime = TimeSpan.FromDays(1.0);

        // a chargeable rescue costs exactly one use, charged here, once, when it is carried out
        private static void Charge(Mobile m, RescuePolicy policy, StuckMode mode, StuckTrace trace)
        {
            if (!policy.Chargeable)
            {
                trace.Note("uses: none charged (free rescue)");
                return;
            }

            if (mode == StuckMode.LiveTest)
            {
                trace.Note("uses: none charged ([HelpStuckTest live)");
                return;
            }

            int before = UsesRemaining(m);
            m.UsedStuckMenu();
            trace.Note($"uses: 1 charged ({before} -> {UsesRemaining(m)} left)");
        }

        // uses left under Mobile.CanUseStuckMenu's rule: slots never used, or used more than 24 hours ago
        public static int UsesRemaining(Mobile m)
        {
            DateTime[]? uses = m.StuckMenuUses;

            if (uses == null)
                return UsesPerDay;

            int free = 0;
            foreach (DateTime used in uses)
                if (DateTime.UtcNow - used > UseLifetime)
                    free++;

            return free;
        }

        // the rescue waiting to happen for this mobile, if any
        public static RescueTicket? Pending(Mobile m)
        {
            return m_Table.TryGetValue(m, out RescueTicket? ticket) && ticket.Running ? ticket : null;
        }
        #endregion Uses
    }

    // a scheduled rescue: counts down (keeping them frozen), then hands itself to HelpStuck.Expire
    public class RescueTicket : Timer
    {
        public Mobile Mobile { get; }
        public Point3D TargetLocation { get; }
        public Map TargetMap { get; }
        public Point3D OriginalLocation { get; }
        public Map OriginalMap { get; }
        public bool Force { get; }
        public RescuePolicy Policy { get; }
        public BaseHouse? House { get; }       // HouseRescue: the house they are rescued from

        private readonly DateTime m_End;
        public DateTime Due => m_End;
        private readonly bool m_WasHidden;

        public RescueTicket(Mobile m, Point3D targetLoc, Map targetMap, TimeSpan delay, bool force, RescuePolicy policy, BaseHouse? house = null)
            : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
        {
            Priority = TimerPriority.TwoFiftyMS;

            Mobile = m;
            m_WasHidden = m.Hidden;
            TargetLocation = targetLoc;
            TargetMap = targetMap;
            m_End = DateTime.UtcNow + delay;

            Force = force;
            Policy = policy;
            House = house;

            OriginalLocation = m.Location;
            OriginalMap = m.Map;

            HelpStuck.Register(this);
        }

        // Evac + dungeon rules => we'll be creating a recovery chest and stripping gear anyway, so weight is not
        //  enforced. PARITY: judged by distance (or a map change) and the region they are in right now.
        public bool IsDungeonEvacuation =>
            HelpStuckProbes.IsEvacuationMove(OriginalMap, TargetMap, OriginalLocation, TargetLocation)
            && Mobile.Region != null && Mobile.Region.IsDungeonRules;

        // IsDungeonEvacuation as it stood on the final tick; the expiry check and the recovery chest both use it
        public bool DungeonEvacuation { get; private set; }

        protected override void OnTick()
        {
            if (!Running)
                return; // we got a queued tick after we stopped the timer .. just ignore it

            bool isDungeonEvac = IsDungeonEvacuation;

            if (DateTime.UtcNow < m_End)
            {
                Mobile.Frozen = true;

                // If we are performing a dungeon evacuation, hide them during the countdown.
                if (isDungeonEvac)
                    Mobile.Hidden = true;
            }
            else
            {
                Mobile.Frozen = false;

                // Restore their original hidden state if we hid them for a dungeon evac.
                if (isDungeonEvac)
                    Mobile.Hidden = m_WasHidden;

                Stop();
                Flush();

                HelpStuck.Unregister(this);
                DungeonEvacuation = isDungeonEvac;

                try
                {
                    HelpStuck.Expire(this);
                }
                catch (Exception exc)
                {
                    LogHelper.LogException(exc);
                    Console.WriteLine("Exception caught in RescueTicket.OnTick: " + exc.Message);
                    Console.WriteLine(exc.StackTrace?.ToString());
                }
            }
        }
    }
}
