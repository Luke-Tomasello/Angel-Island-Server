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

/* Scripts/Engines/Help/HelpStuck/HelpStuckRules.cs
 * Changelog:
 *  10/15/2026, Adam (Help Stuck: HelpStuck.log history)
 *      Comments and the dry run's description only: stranded at sea and stranded on a boat are moved to the shore
 *      found when the plan was made (HelpStuck no longer has Strandedness.ProcessStranded search for it again), so
 *      the "none if Strandedness cannot move them" case is gone.
 *  10/14/2026, Adam (Help Stuck: stranded on a boat)
 *      StrandedOnABoat has a policy (PlanStrandedOnABoat, where its design is documented). Aboard a boat it decides,
 *      and nothing after it applies: a player on a boat no longer gets a local unblock or the town menu. Refused
 *      (free) if they can leave by a plank, can command the boat (BaseBoat.GetCommandAuthority; a ghost silenced by
 *      tiller clutter cannot), can pick up its key, or an associated captain aboard can sail it; otherwise moved at
 *      once to the Strandedness shore, pets not moved, one use charged only if moved; no shore: refused, page staff.
 *      The ShipKey gate is removed: it only ever applied aboard a boat, and the boat rules now answer it, with the
 *      tillerman's semantics (key rings, corpse keys, Siege), once.
 *      RescuePolicy.Immediate: a rescue carried out at request (stranded at sea, stranded on a boat). It has no
 *      expiry, so the weight gate is applied at request on its exemptions; StrandedOnABoat is not exempt. Stranded
 *      at sea is unchanged (the Weight gate excepts it).
 *      StuckTrace.Decide: a deciding step with its own verdict (NOT STRANDED).
 *  10/14/2026, Adam (Help Stuck: Z correction removed, local unblock destinations)
 *      Removed ZCorrection. It moved a player to the highest item top on their X/Y, up or down, before every gate
 *      and forced: a vertical teleport the Z-stack protections (ExploitTracking, the Teleport/Mark iStack checks)
 *      never see. A player who is genuinely stuck still gets the local unblock, house rescue or town menu.
 *      Local unblock: a designated exit (house ban location, township edge) first, as before. Otherwise, physically
 *      blocked: if only movable impassable items block them (StuckStatus.BlockedByMovables: blocked, but free once
 *      movable items are ignored) there is no local unblock, the scene is recorded as a possible auto-move exploit
 *      attempt (StuckTrace.Suspect, recorded by HelpStuck), and the rescues below still apply. Otherwise every
 *      blocked-move candidate must be in line of sight and in the same region (HelpStuckProbes.FindBlockedDestination).
 *  10/14/2026, Adam (Help Stuck: stranded at sea)
 *      StrandedAtSea is planned only when Strandedness finds a shore location (StuckStatus.Shore); the plan carries
 *      it, so the dry run shows where they would go. With no shore, the usual rescues apply. Detection itself moved
 *      to Strandedness (see HelpStuckProbes); a player in the water without a boat now gets it.
 *      StrandedOnABoat: a separate state from StrandedAtSea, reserved and traced (on a boat or not) but with no
 *      policy yet. Boat behavior is unchanged.
 *  10/13/2026, Adam (Help Stuck refactor)
 *      First time checkin. The Help Stuck rules, previously spread through DoHelpStuck, AllowUnstuck, TryUnblock,
 *      IsHopeless and TeleportTimer.OnTick, now in one place:
 *          o RescuePolicies: what each kind of rescue does (delay, force, use accounting, checks at expiry)
 *          o the eligibility gates, in their original order and with their original messages
 *          o the request decision tree (which rescue to plan) and the expiry decision (is it still valid)
 *          o StuckTrace: the record of which rules were checked, against which facts, and why they decided
 *      Behavior is unchanged. The quirks are kept and marked PARITY.
 *      One change: the DailyUses gate now says the player is out of uses (it sent no message of its own, so the
 *      player got the generic 'not a valid stuck location' message).
 *      New: HouseRescue. An owner, co-owner or friend of the house they are inside (BaseHouse.IsFriend) is moved
 *      to the house ban location (where an ejection puts people), pets too: no use is charged,
 *      weight and the daily use limit do not apply, a reachable exit does not cancel it, and at expiry they must still
 *      be a friend inside the same house. Anyone else inside a house still gets the ordinary LocalUnblock.
 *      Uses: a chargeable rescue now costs exactly one use, when it is carried out (RescuePolicy.Chargeable replaces
 *      ChargeAtRequest/ChargeOnSuccess). A local unblock used to be charged when scheduled and again when it
 *      happened (two uses), and kept the first charge if it was cancelled.
 *      Dungeon exit hints: when DungeonNotStuck would refuse a player who can walk around a dungeon, and a
 *      legitimate exit (ExitOptions) is in reach, they get the ExitHints plan instead of "But, you're not stuck."
 *      Uses are required only by an outcome that needs one: the outcome is decided first, then, if it needs a use
 *      (RescuePolicy.NeedsUse: it is chargeable, or leads to a chargeable rescue, as the town menu leads to TownMove),
 *      one must be left; otherwise it is delivered unchanged. So with no use left a player still gets exit hints,
 *      "But, you're not stuck.", the quest-area message, a house rescue, and any other refusal with its own reason;
 *      only a chargeable rescue is refused as out of uses. At expiry the DailyUses gate applies to the same rule.
 *      Eligible no longer sends messages itself; the caller sends the deciding refusal.
 *      Exit hints are shown once per distinct line (HelpStuckProbes.ExitHintLines), live and in the dry run alike;
 *      the trace reports both counts, e.g. "3 reachable exit(s), 1 distinct hint(s)".
 *      New: DungeonUnblock. A physically blocked local unblock in a dungeon is its own policy: a LocalUnblock in
 *      every respect (delay, chargeable, not-moved check, gates, no reachable exits at expiry) except that weight
 *      is not enforced. The move is at most a few tiles and stays in the dungeon, so nothing is carried out, and
 *      an overweight player embedded in dungeon geometry often cannot drop anything. House, township and overworld
 *      blocked unblocks remain LocalUnblock, weight enforced.
 */

/* StuckRules - overview
 *  Rules decide; they read facts from a StuckStatus and never query the world themselves. They also never
 *  message, charge, schedule or move: they return a RescuePlan (at request) or a verdict (at expiry), and
 *  HelpStuck (HelpStuckRescue.cs) carries it out. A refusal is part of that answer (RescuePlan.Refusal, or
 *  ValidAtExpiry's message) and HelpStuck sends it; a dry run only reports it. The one exception is the region
 *  hook, which messages the player itself when it says no (see StuckStatus).
 *
 *  Request: eligibility -> house rescue -> stranded at sea -> [aboard a boat: the boat rules, terminal]
 *           -> local unblock -> exit hints -> quest area -> town menu
 *           (stranded at sea: in the water without a boat, and only when Strandedness finds a shore)
 *           (aboard a boat: PlanStrandedOnABoat decides, and nothing after it applies: refused if they can leave
 *           by a plank, sail it, pick up its key, or an associated captain aboard can sail it; otherwise
 *           StrandedOnABoat to the Strandedness shore, or refused with no shore)
 *           (in a dungeon they can walk around: exit hints if an exit is in reach, else "not stuck")
 *           (local unblock: a designated exit (house, township), else physically blocked: a short move to a spot in
 *           line of sight, in the same region, that they cannot walk to; in a dungeon a DungeonUnblock, weight exempt.
 *           Blocked only by movable items: no local unblock, recorded as a possible auto-move exploit attempt)
 *           -> the use check, on the outcome
 *  Expiry:  not moved -> eligibility again (fresh facts, the use check included) -> no reachable exits
 *
 *  Uses: decide the outcome first. If that outcome needs a Help Stuck use (RescuePolicy.NeedsUse, the only place
 *      this is decided), enforce DailyUses; otherwise deliver the outcome unchanged. A chargeable rescue
 *      (RescuePolicy.Chargeable) costs exactly one use, charged once, when it is carried out: at once for an
 *      immediate rescue (stranded at sea or on a boat), at expiry for a delayed rescue. Scheduling costs nothing,
 *      and a cancelled rescue costs nothing.
 *  Immediate rescues (RescuePolicy.Immediate) have no ticket and no expiry, so the gate a delayed rescue meets at
 *      expiry (weight) is applied at request, on the policy's exemptions.
 *      Refusals, exit hints, the quest-area message and the house rescue are free. A use must be left at request
 *      and again at expiry (DailyUses gate); nothing is charged in between, so the charging itself cannot fail it.
 *
 *  PARITY notes (kept deliberately, to be revisited one at a time):
 *      o every delayed rescue requires 'no reachable exits' at expiry, local unblocks included
 *          (RescuePolicy.RequireNoExitsAtExpiry)
 *      o weight is not checked at request; at expiry it is, except for a dungeon evacuation (and for a policy that
 *          exempts it: HouseRescue, DungeonUnblock). Whether a move is
 *          a dungeon evacuation is decided at expiry from the move's distance and the region the player is in.
 *      o a forced rescue (a staff town move) still runs the expiry gates, and they still message, but cannot
 *          stop it; it also ignores having moved (RescuePolicy.RevalidateWhenForced)
 *      o destinations other than the blocked local unblock's are not validated, and none has a fallback
 */

using Server.Diagnostics;
using Server.Mobiles;
using Server.Multis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Menus.Questions
{
    #region Trace
    public sealed class StuckTrace
    {
        private readonly List<string> m_Steps = new();
        public IReadOnlyList<string> Steps => m_Steps;
        public string Outcome { get; set; } = "";
        public string? Deciding { get; private set; }   // the step that decided the outcome

        public void Pass(string rule, string observed) => m_Steps.Add($"{rule}: {observed} -> pass");
        public void Fail(string rule, string observed) => m_Steps.Add(Deciding = $"{rule}: {observed} -> FAIL");
        public void Take(string rule, string observed) => m_Steps.Add(Deciding = $"{rule}: {observed} -> TAKE");
        public void Skip(string rule, string observed) => m_Steps.Add($"{rule}: {observed} -> no");
        public void Note(string text) => m_Steps.Add("  " + text);
        public void Step(string rule, string observed, string verdict) => m_Steps.Add($"{rule}: {observed} -> {verdict}");
        // a step that decides the outcome, with its own verdict (e.g. NOT STRANDED)
        public void Decide(string rule, string observed, string verdict) => m_Steps.Add(Deciding = $"{rule}: {observed} -> {verdict}");

        // a circumstance worth recording for staff to look at later: not a finding that anyone cheated. The rules
        //  only note it; HelpStuck records it (RecordCheater), never in a test
        public string? Suspicion { get; private set; }
        public void Suspect(string rule, string observed) => m_Steps.Add(Suspicion = $"{rule}: {observed} -> SUSPICIOUS");

        // one line for the decisions log
        public string Summary => Deciding == null ? Outcome : $"{Outcome} [{Deciding}]";
    }
    #endregion Trace

    #region Plans and policies
    public enum RescueKind
    {
        Refused,        // a gate said no at request
        // StrandedAtSea and StrandedOnABoat are deliberately separate states: their detection, policy and rescue
        //  considerations differ, and neither is defined in terms of the other. Both are carried out by moving the
        //  player, not their pets (Publish 15), to where Strandedness puts a stranded player ashore.
        StrandedAtSea,  // in the water without a boat: Strandedness moves them ashore
        StrandedOnABoat,    // aboard a boat with no way off it and no associated captain (PlanStrandedOnABoat)
        LocalUnblock,   // physically blocked, in a house or in a township: a short move
        DungeonUnblock, // physically blocked in a dungeon: the same short move, weight not enforced
        HouseRescue,  // inside a house they own, co-own or are a friend of: moved to its ban location
        ExitHints,      // not hopeless: point them at a reachable exit
        QuestArea,      // off Felucca with no way out: nothing we can do
        TownChoice,     // hopeless: the town menu
        TownMove,       // the move chosen from the town menu
    }

    // gates a kind of rescue is not subject to
    [Flags]
    public enum GateExemptions
    {
        None = 0x0,
        Weight = 0x1,
        DailyUses = 0x2,    // not declared by a policy: set by Eligible when the outcome needs no use (RescuePolicy.NeedsUse)
    }

    public sealed class RescuePolicy
    {
        public RescueKind Kind { get; init; }
        public GateExemptions Exemptions { get; init; } // at request and at expiry
        public Func<TimeSpan>? Delay { get; init; }     // null: no delayed move, or the delay is given by the caller
        public string DelayText { get; init; } = "";
        public bool Force { get; init; }                // the move happens whatever the expiry checks say
        public bool Chargeable { get; init; }           // costs one use, charged once, when the rescue is carried out
        // carried out at request, with no ticket and so no expiry: the gates a delayed rescue meets at expiry
        //  (weight) are applied at request instead (PlanRequest), on this policy's exemptions
        public bool Immediate { get; init; }
        public RescuePolicy? LeadsTo { get; init; }     // the rescue this outcome hands on to (the town menu: TownMove)
        public bool RevalidateWhenForced { get; init; } // a forced move still runs (and messages) the expiry checks
        public bool RequireNoExitsAtExpiry { get; init; }
        public string? ScheduledMessage { get; init; }

        // the one answer to 'does this outcome need a Help Stuck use?': it is charged, or it leads to one that is.
        //  Anything else (a refusal, exit hints, the quest-area message, a house rescue) is free and is delivered
        //  whatever the use count.
        public bool NeedsUse => Chargeable || LeadsTo?.NeedsUse == true;
    }

    public static class RescuePolicies
    {
        private const string MovedSoon = "You will be moved sometime in the next minute.";

        public static readonly RescuePolicy Refused = new() { Kind = RescueKind.Refused };

        // carried out at once, no ticket, to the Strandedness shore found when planned. Weight does not apply (the
        //  Weight gate excepts stranded at sea). Pets are not moved: Help Stuck moves the player, not their pets
        //  (Publish 15).
        public static readonly RescuePolicy StrandedAtSea = new()
        {
            Kind = RescueKind.StrandedAtSea,
            Chargeable = true,              // charged at once, when they are moved
            Immediate = true,
        };

        // carried out at once, to the Strandedness shore found when planned (the shore a login or a dry dock uses), no
        //  ticket: a delay would only let the boat sail on, and a moved player fails the not-moved check. Weight
        //  applies, at request: unlike open water, a boat carries cargo, and this must not become a way to ship it
        //  ashore. Pets are not moved (Publish 15).
        public static readonly RescuePolicy StrandedOnABoat = new()
        {
            Kind = RescueKind.StrandedOnABoat,
            Chargeable = true,              // charged at once, when they are moved
            Immediate = true,
        };

        public static readonly RescuePolicy LocalUnblock = new()
        {
            Kind = RescueKind.LocalUnblock,
            Delay = () => TimeSpan.FromSeconds(Utility.RandomMinMax(10, 60)),
            DelayText = "10-60 s",
            Chargeable = true,              // charged when the move happens, not when it is scheduled
            RevalidateWhenForced = true,
            RequireNoExitsAtExpiry = true,  // PARITY: a reachable exit cancels a local unblock
            ScheduledMessage = MovedSoon,
        };

        // a local unblock of the physically blocked kind, in a dungeon: at most UnblockRange tiles, and they stay in
        //  the dungeon, so nothing is carried out of it and weight is not enforced (an overweight player embedded in
        //  dungeon geometry often cannot drop anything). Otherwise exactly a LocalUnblock. House, township and
        //  overworld blocked unblocks stay LocalUnblock, weight enforced.
        public static readonly RescuePolicy DungeonUnblock = new()
        {
            Kind = RescueKind.DungeonUnblock,
            Exemptions = GateExemptions.Weight,
            Delay = LocalUnblock.Delay,
            DelayText = LocalUnblock.DelayText,
            Chargeable = true,              // charged when the move happens, not when it is scheduled
            RevalidateWhenForced = true,
            RequireNoExitsAtExpiry = true,  // PARITY: as LocalUnblock
            ScheduledMessage = MovedSoon,
        };

        // an owner, co-owner or friend of the house (BaseHouse.IsFriend) is moved to its ban location, where an
        //  ejection would put them. It is their house, so it is not chargeable (needs no use) and ignores weight;
        //  everything else (not moved, combat, criminal travel, sigil, ...) still applies, and at expiry they must
        //  still be a friend inside the same house. A non-friend in a house gets the ordinary LocalUnblock.
        public static readonly RescuePolicy HouseRescue = new()
        {
            Kind = RescueKind.HouseRescue,
            Exemptions = GateExemptions.Weight,
            Delay = () => TimeSpan.FromSeconds(Utility.RandomMinMax(10, 60)),
            DelayText = "10-60 s",
            RevalidateWhenForced = true,
            RequireNoExitsAtExpiry = false, // a reachable moongate is no reason to leave them stuck in their own house
            ScheduledMessage = MovedSoon,
        };

        public static readonly RescuePolicy ExitHints = new() { Kind = RescueKind.ExitHints };
        public static readonly RescuePolicy QuestArea = new() { Kind = RescueKind.QuestArea };

        // the delay and force come from the town menu: 10-120 s for a player's own request, at once and
        //  forced when staff send the menu
        public static readonly RescuePolicy TownMove = new()
        {
            Kind = RescueKind.TownMove,
            DelayText = "10-120 s (immediate from staff)",
            Chargeable = true,
            RevalidateWhenForced = true,
            RequireNoExitsAtExpiry = true,
        };

        // opening the menu costs nothing, but it leads to a TownMove, so it needs a use (after TownMove: static
        //  fields are initialized in order)
        public static readonly RescuePolicy TownChoice = new() { Kind = RescueKind.TownChoice, LeadsTo = TownMove };
    }

    public sealed class RescuePlan
    {
        public RescuePolicy Policy { get; }
        public RescueKind Kind => Policy.Kind;
        public Point3D Destination { get; }
        public Map? Map { get; }
        public string Source { get; }       // what produced the destination (local unblock)
        public StuckRules.GateMessage? Refusal { get; } // what a refusal says (null: the generic message)
        public BaseHouse? House { get; }    // the house they are rescued from (HouseRescue)

        public RescuePlan(RescuePolicy policy, Point3D destination = default, Map? map = null, string source = "", StuckRules.GateMessage? refusal = null, BaseHouse? house = null)
        {
            Policy = policy;
            Destination = destination;
            Map = map;
            Source = source;
            Refusal = refusal;
            House = house;
        }
    }
    #endregion Plans and policies

    public static class StuckRules
    {
        public const int MaxWeight = 101;   // StuckMenu.MAXHELPSTUCKALIVEWEIGHT
        public static readonly TimeSpan CombatWindow = TimeSpan.FromSeconds(30.0);

        #region Gates
        public readonly struct GateMessage
        {
            public readonly string? Text;
            public readonly int Number;
            public readonly int Hue;    // -1: the default hue
            public GateMessage(string text, int hue = -1) { Text = text; Number = 0; Hue = hue; }
            public GateMessage(int number, int hue = -1) { Text = null; Number = number; Hue = hue; }

            public void Send(Mobile m)
            {
                if (Text != null)
                {
                    if (Hue >= 0)
                        m.SendMessage(Hue, Text);
                    else
                        m.SendMessage(Text);
                }
                else if (Hue >= 0)
                    m.SendLocalizedMessage(Number, "", Hue);
                else
                    m.SendLocalizedMessage(Number);
            }
            public override string ToString() => Text ?? $"#{Number}";
        }

        private sealed class Gate
        {
            public readonly string Name;
            public readonly Func<StuckStatus, GateExemptions, bool> Fails;  // (status, exempt)
            public readonly Func<StuckStatus, GateExemptions, string> Observed;     // only reads facts Fails already read
            public readonly Func<StuckStatus, GateMessage?> Message;        // null: the gate sends nothing itself

            public Gate(string name, Func<StuckStatus, GateExemptions, bool> fails, Func<StuckStatus, GateExemptions, string> observed, Func<StuckStatus, GateMessage?> message)
            {
                Name = name;
                Fails = fails;
                Observed = observed;
                Message = message;
            }
        }

        private static GateMessage? None(StuckStatus s) => null;

        private const string OutOfUsesText = "You have already used help stuck twice in the last 24 hours. Please try again later.";
        public static readonly GateMessage OutOfUses = new GateMessage(OutOfUsesText);

        // the order is behavior: the first failing gate is the one the player hears about
        private static readonly Gate[] Gates = new Gate[]
        {
            new("DailyUses", (s, x) => !x.HasFlag(GateExemptions.DailyUses) && !s.HasUses,
                (s, x) => x.HasFlag(GateExemptions.DailyUses) ? "not checked here (only an outcome that needs a use is)" : $"use available={s.HasUses}",
                s => new GateMessage(OutOfUsesText)),
            new("Disabled", (s, x) => s.HelpStuckDisabled,
                (s, x) => $"HelpStuckDisabled={s.HelpStuckDisabled}",
                s => new GateMessage("Sorry, help stuck is unavailable at this time. Please page for staff assistance.")),
            new("TestCenterClone", (s, x) => s.TestCenterClone,
                (s, x) => $"test center clone={s.TestCenterClone}",
                s => new GateMessage("You cannot use Help Stuck while in the Test Center.")),
            new("PrisonOrJail", (s, x) => s.PrisonRules || s.JailRules,
                (s, x) => $"prison={s.PrisonRules} jail={s.JailRules}",
                s => s.PrisonRules ? new GateMessage("Nice try Louie.", 0x23) : new GateMessage("You'll need a better jailbreak plan than that!", 0x23)), // cliloc 1041530 reads "then that"
            new("DungeonNotStuck", (s, x) => s.DungeonRules && s.CanMoveFreely,
                (s, x) => s.DungeonRules ? $"dungeon, can walk 8+ tiles={s.CanMoveFreely}" : "not a dungeon",
                s => new GateMessage("But, you're not stuck.")),
            // 5/19/23, Yoar: no longer refused for being criminal, only while criminal travel is locked
            new("CriminalTravel", (s, x) => !s.CanCriminalTravel,
                (s, x) => $"can criminal travel={s.CanCriminalTravel}",
                s => new GateMessage(1005270, 0x23)),   // Thou'rt a criminal and cannot escape so easily...
            new("Combat", (s, x) => DateTime.UtcNow - s.LastAggression < CombatWindow,
                (s, x) => s.LastAggression == DateTime.MinValue ? "never attacked" : $"last attacked {(int)(DateTime.UtcNow - s.LastAggression).TotalSeconds} s ago (window {CombatWindow.TotalSeconds} s)",
                s => new GateMessage(1005271, 0x23)),   // Wouldst thou flee during the heat of battle??
            new("Sigil", (s, x) => s.CarryingSigil,
                (s, x) => $"carrying sigil={s.CarryingSigil}",
                s => new GateMessage(1061632)),         // You can't do that while carrying the sigil.
            new("Flag", (s, x) => s.CarryingFlag,
                (s, x) => $"carrying flag={s.CarryingFlag}",
                s => new GateMessage("You can't do that while carrying the flag.")),
            new("Weight", (s, x) => !x.HasFlag(GateExemptions.Weight) && s.TotalWeight >= MaxWeight && !s.StrandedAtSea,
                (s, x) => x.HasFlag(GateExemptions.Weight) ? "exempt" : s.TotalWeight < MaxWeight ? $"{s.TotalWeight} < {MaxWeight}" : $"{s.TotalWeight} >= {MaxWeight}, stranded at sea={s.StrandedAtSea}",
                s => new GateMessage("You are too encumbered to be moved, drop some of your stuff and try again.")),
            new("PackAnimal", (s, x) => s.HasLoadedPackAnimal,
                (s, x) => $"loaded pack animal={s.HasLoadedPackAnimal}",
                s => new GateMessage("You cannot be transported because you have loaded pack animal!")),
            // (the ShipKey gate is gone: whether someone aboard a boat can command it, key or otherwise, is decided
            //  once, by PlanStrandedOnABoat. Off a boat it never applied.)
            // last: the region may message the player itself when it says no
            new("RegionVeto", (s, x) => !s.RegionAllowsStuck,
                (s, x) => $"region allows={s.RegionAllowsStuck}",
                None),
        };

        // AllowUnstuck. On failure: failedGate is the deciding gate, and message what it says (null: the caller sends
        //  the generic message). Eligible sends nothing itself; the caller decides what the player hears.
        // outcome: the outcome being checked. The DailyUses gate applies only when it needs a use; at request the
        //  outcome is not decided yet (null), so PlanRequest checks uses once it is.
        public static bool Eligible(StuckStatus s, GateExemptions exempt, RescuePolicy? outcome, StuckTrace trace, out string? failedGate, out GateMessage? message)
        {
            failedGate = null;
            message = null;

            if (outcome == null || !outcome.NeedsUse)
                exempt |= GateExemptions.DailyUses;

            if (s.IsStaff && !s.JudgedAsPlayer)
            {
                trace.Pass("Staff", $"{s.Mobile.AccessLevel}, gates bypassed");
                return true;
            }

            foreach (Gate gate in Gates)
            {
                if (!gate.Fails(s, exempt))
                {
                    trace.Pass(gate.Name, gate.Observed(s, exempt));
                    continue;
                }

                trace.Fail(gate.Name, gate.Observed(s, exempt));
                failedGate = gate.Name;
                message = gate.Message(s);
                return false;
            }

            return true;
        }

        private static RescuePlan Refuse(StuckTrace trace, GateMessage? message)
        {
            trace.Note(message != null ? $"says: {message}" : "says: the generic 'not a valid stuck location' message");
            return new RescuePlan(RescuePolicies.Refused, refusal: message);
        }
        #endregion Gates

        #region Request
        // DoHelpStuck: decide which rescue applies. No side effects beyond a refusing gate's message.
        public static RescuePlan PlanRequest(StuckStatus s, StuckTrace trace)
        {
            // an owner, co-owner or friend of the house they are inside (BaseHouse.IsFriend) gets the authorized house
            //  rescue. Anyone else in a house gets none of its exemptions: the ordinary LocalUnblock, weight and uses.
            BaseHouse? house = s.House;
            bool houseFriend = house != null && s.HouseFriend;
            if (house == null)
                trace.Skip("HouseAuthorization", "not inside a house");
            else
                trace.Step("HouseAuthorization",
                    $"inside {house.GetType().Name} at {house.Location}, owner/co-owner/friend={s.HouseFriend}"
                        + (s.IsStaff && s.HouseFriend ? " (GameMaster+ counts as owner)" : ""),
                    houseFriend ? "AUTHORIZED HOUSE RESCUE (HouseRescue policy: not chargeable, so it needs no use; weight exempt)" : "NOT AUTHORIZED: ordinary LocalUnblock rules (weight and uses apply)");

            // Decide the outcome first, without looking at uses. If that outcome needs a use (RescuePolicy.NeedsUse),
            //  one must be left; otherwise it is delivered as decided. A refusal is free: the player hears its own
            //  reason whatever their use count.
            // weight is left to the expiry check: a dungeon evacuation does not care, everything else does.
            //  A house friend's request already carries the house rescue's exemptions (it is the rescue they get).
            GateExemptions exempt = GateExemptions.Weight | (houseFriend ? RescuePolicies.HouseRescue.Exemptions : GateExemptions.None);
            RescuePlan plan;
            if (!Eligible(s, exempt, null, trace, out string? failedGate, out GateMessage? message))
            {
                // in place of the refusal: mobile in a dungeon, but a legitimate exit is in reach
                if (failedGate == "DungeonNotStuck" && s.ExitOptions.Count != 0)
                {
                    trace.Take("ExitHints", $"DungeonNotStuck found them mobile, but {ExitsSeen(s)}: the hint instead of 'not stuck'");
                    plan = new RescuePlan(RescuePolicies.ExitHints);
                }
                else
                {
                    if (failedGate == "DungeonNotStuck")
                        trace.Skip("ExitHints", "no reachable exits: 'But, you're not stuck.'");
                    trace.Note($"a refusal needs no use: its own reason is given (use available={s.HasUses})");
                    return Refuse(trace, message);
                }
            }
            else
                plan = PlanRescue(s, house, houseFriend, trace);

            // the use check, on the outcome
            if (!plan.Policy.NeedsUse)
                trace.Pass("DailyUses", $"{plan.Kind} needs no use (use available={s.HasUses})");
            else if (s.IsStaff && !s.JudgedAsPlayer)
                trace.Pass("DailyUses", $"{plan.Kind} needs a use; staff, not checked");
            else if (!s.HasUses)
            {
                trace.Fail("DailyUses", $"{plan.Kind} needs a use; none left");
                return Refuse(trace, OutOfUses);
            }
            else
                trace.Pass("DailyUses", $"{plan.Kind} needs a use; use available");

            // an immediate rescue has no expiry, so the gate deferred to it (weight) applies now, on its exemptions
            if (plan.Policy.Immediate && !(s.IsStaff && !s.JudgedAsPlayer)
                && !PassesGate("Weight", s, plan.Policy.Exemptions, "immediate: checked at request", trace, out GateMessage? weightMessage))
                return Refuse(trace, weightMessage);

            return plan;
        }

        // one gate, by name, outside Eligible's sequence (an immediate rescue's deferred gate)
        private static bool PassesGate(string name, StuckStatus s, GateExemptions exempt, string when, StuckTrace trace, out GateMessage? message)
        {
            Gate gate = Gates.First(g => g.Name == name);
            message = null;

            if (!gate.Fails(s, exempt))
            {
                trace.Pass(gate.Name, $"{gate.Observed(s, exempt)} ({when})");
                return true;
            }

            trace.Fail(gate.Name, $"{gate.Observed(s, exempt)} ({when})");
            message = gate.Message(s);
            return false;
        }

        // for the trace: the raw exits found, and the distinct hints the player will see
        private static string ExitsSeen(StuckStatus s) =>
            $"{s.ExitOptions.Count} reachable exit(s), {HelpStuckProbes.ExitHintLines(s.Mobile, s.ExitOptions).Count} distinct hint(s)";

        // eligible (uses aside): which rescue applies
        private static RescuePlan PlanRescue(StuckStatus s, BaseHouse? house, bool houseFriend, StuckTrace trace)
        {
            // before stranded at sea, so its exemptions apply only to this rescue (a house tile is not open water)
            if (houseFriend)
            {
                trace.Take("HouseRescue", $"ban location {house!.BanLocation} {house.Map}");
                return new RescuePlan(RescuePolicies.HouseRescue, house.BanLocation, house.Map, "house", house: house);
            }

            // planned only when Strandedness can find them a shore; otherwise the rescues below apply
            if (!s.StrandedAtSea)
                trace.Skip("StrandedAtSea", "not in the water without a boat");
            else if (s.Shore == Point3D.Zero)
                trace.Skip("StrandedAtSea", "in the water without a boat, but Strandedness finds no shore location");
            else
            {
                trace.Take("StrandedAtSea", $"in the water without a boat -> shore {s.Shore}");
                return new RescuePlan(RescuePolicies.StrandedAtSea, s.Shore, s.Map);
            }

            // aboard a boat: the boat rules decide, and nothing below applies
            if (s.OnABoat)
                return PlanStrandedOnABoat(s, trace);
            trace.Skip("StrandedOnABoat", "not on a boat");

            // local unblock: a designated exit first (a house they are inside: its ban location; a township: an edge
            //  point). These leave the region by design.
            Point3D loc = HelpStuckProbes.FindDesignatedExit(s.Mobile, out string source);
            if (loc == Point3D.Zero && s.Blocked)
            {
                if (s.BlockedByMovables)
                    // manufactured: only movable impassable items (crates, kegs, ...) keep them from walking out. No
                    //  local unblock; HelpStuck records the scene, and the rescues below still apply.
                    trace.Suspect("MovableBlockage", "blocked, but free once movable items are ignored: no local unblock (possible auto-move exploit attempt)");
                else
                {
                    // physically blocked: a short move past the obstruction, to a spot in line of sight and in the
                    //  same region (HelpStuckProbes.FindBlockedDestination)
                    loc = HelpStuckProbes.FindBlockedDestination(s.Mobile);
                    if (loc != Point3D.Zero)
                        source = HelpStuckProbes.BlockedSource;
                }
            }
            if (loc != Point3D.Zero)
            {
                // physically blocked in a dungeon: the dungeon variant (weight exempt); every other source stays LocalUnblock
                if (s.DungeonRules && source == HelpStuckProbes.BlockedSource)
                {
                    trace.Take("DungeonUnblock", $"{source} in a dungeon -> {loc} (weight not enforced)");
                    return new RescuePlan(RescuePolicies.DungeonUnblock, loc, s.Map, source);
                }
                trace.Take("LocalUnblock", $"{source} -> {loc}");
                return new RescuePlan(RescuePolicies.LocalUnblock, loc, s.Map, source);
            }
            trace.Skip("LocalUnblock", !s.Blocked ? "no house or township exit, and not physically blocked"
                : s.BlockedByMovables ? "blocked only by movable items"
                : "no house or township exit, and no blocked-move destination in line of sight and in the same region");

            if (s.ExitOptions.Count != 0)
            {
                trace.Take("ExitHints", ExitsSeen(s));
                return new RescuePlan(RescuePolicies.ExitHints);
            }
            trace.Skip("ExitHints", "no reachable exits");

            // we're probably in a quest zone
            if (s.Map != Map.Felucca)
            {
                trace.Take("QuestArea", $"map {s.Map}");
                return new RescuePlan(RescuePolicies.QuestArea);
            }
            trace.Skip("QuestArea", "on Felucca");

            trace.Take("TownChoice", "hopeless");
            return new RescuePlan(RescuePolicies.TownChoice);
        }

        /* StrandedOnABoat: does this player have a reasonable way off this boat without Help Stuck?
         *
         * The determination is social as well as mechanical, which is why it is not simply !CanCommand.
         *
         * Mechanical: a player is not stranded if they can walk off over a plank (anyone aboard may open one,
         *  key or no key, and a ghost too: Plank.OnDoubleClickDead), or if they can sail the boat themselves.
         *  "Can sail" is the tillerman's own rule (BaseBoat.GetCommandAuthority: a key in the backpack or on a key
         *  ring, their own corpse aboard with the key, Siege's closest-player rule) and, for a living player, a key
         *  they could pick up (cursor, unlocked hold, deck). It is not a navigation solver: having authority is
         *  enough, so an anchor, a course or a living player's tiller clutter (they can lift it) does not make them
         *  stranded. Two things do make authority useless: a boat that is decaying takes no orders from anyone,
         *  and a ghost cannot lift the clutter that silences the tillerman.
         *
         * Social: another player aboard who can sail the boat counts as help only when the server has affirmative
         *  evidence that they and the requester are associated (FindAssociation). Merely having another captain
         *  aboard is not enough: that player may be a stranger, a pirate, or the requester's captor (the murderer
         *  who took the key). Lack of hostility is never evidence of friendship, so an unknown captain is no help.
         *  Conversely, associated players must not be able to use Help Stuck as a boat-to-shore shuttle, one
         *  staying aboard to sail while the other asks to be put ashore. So:
         *      o same account or same hardware: common control. Help, whatever aggression they stage.
         *      o same guild, allied guild or same party: softer. Help, unless there is affirmative hostility
         *        (FindHostility: they struck first in a current fight, or the requester's corpse aboard names them
         *        as its killer).
         *  One associated captain who can sail is enough; any others aboard do not change it. Whether that captain
         *  is at the keyboard, or willing, is for the two of them: the point is that they are associated.
         *  Shared login IPs do not decide anything (too loose); the trace records them for staff.
         *
         * Once they are aboard, this decision is terminal: a refusal here must not fall through to LocalUnblock or
         *  the town menu (that would be the shuttle), and a rescue is only ever to the shore Strandedness finds.
         *  The weight gate applies (RescuePolicies.StrandedOnABoat). What the Help Stuck module promises in general
         *  is in StuckMenu.cs; the boat mechanics are in BaseBoat (GetCommandAuthority, FindTillerObstruction,
         *  DecayDue) and Plank (FindDisembarkLocation), and only asked here.
         */
        private static RescuePlan PlanStrandedOnABoat(StuckStatus s, StuckTrace trace)
        {
            BaseBoat boat = s.Boat!;
            trace.Step("OnABoat", $"{boat.GetType().Name}{(boat.ShipName != null ? " '" + boat.ShipName + "'" : "")} at {boat.Location} {boat.Map}, anchored={boat.Anchored}, moving={boat.IsMoving}",
                "the boat rules decide (nothing else applies aboard)");

            // a plank puts them ashore
            if (s.BoatExit != null)
            {
                trace.Decide("Disembark", $"{s.BoatExit.Plank.Side} plank -> {s.BoatExit.Location}", "NOT STRANDED");
                return Refuse(trace, new GateMessage("You can leave the ship by its plank."));
            }
            trace.Skip("Disembark", "neither plank puts them ashore");

            // they can sail it themselves
            string sinking = "the boat is decaying: the tillerman takes no orders";
            string? obstruction = s.TillerObstruction == null ? null : $"{s.TillerObstruction.GetType().Name} beside the tillerman";
            CommandAuthority authority = s.BoatAuthority;
            if (authority != CommandAuthority.None && authority != CommandAuthority.StaffBoatOnly)
            {
                string claim = $"{authority} ({AuthorityText(authority)})";

                if (s.BoatDecaying)
                    trace.Skip("Command", $"{claim}, but {sinking}");
                else if (obstruction != null && !s.Alive)
                    trace.Skip("Command", $"{claim}, but {obstruction} silences him, and a ghost cannot lift it");
                else
                {
                    trace.Decide("Command", obstruction == null ? claim : $"{claim}; {obstruction}, which they can lift", "NOT STRANDED");
                    return Refuse(trace, obstruction != null
                        ? new GateMessage("The tillerman needs room: clear the deck beside him, and he will take your orders.")
                        : authority switch
                        {
                            CommandAuthority.Key => new GateMessage("But the key is in your backpack!"),
                            CommandAuthority.CorpseKey => new GateMessage("Your corpse still carries the ship's key: the tillerman will take your orders."),
                            CommandAuthority.ClosestPlayer => new GateMessage("No one on deck holds the key: the tillerman will take your orders."),
                            _ => new GateMessage("You can command this ship."),
                        });
                }
            }
            else
                trace.Skip("Command", authority == CommandAuthority.StaffBoatOnly ? "a staff boat: GameMaster+ only" : "no claim (no key, no corpse key" + (Core.RuleSets.SiegeRules() ? ", not the closest player with no key on deck)" : ")"));

            // a key they could pick up (alive: a ghost lifts nothing)
            if (!s.BoatDecaying && s.ReachableShipKey != ShipKeyLocation.None)
            {
                trace.Decide("ShipKey", $"a key to this boat: {s.ReachableShipKey}", "NOT STRANDED");
                return Refuse(trace, s.ReachableShipKey switch
                {
                    ShipKeyLocation.Hand => new GateMessage("But the key is in your hands!"),
                    ShipKeyLocation.Hold => new GateMessage("But the key is in the hold!"),
                    _ => new GateMessage("But the key is right there!"),
                });
            }
            trace.Skip("ShipKey", s.BoatDecaying ? "not considered: " + sinking : s.Alive ? "no key to pick up" : "a ghost cannot pick one up");

            // an associated captain aboard can sail it
            BoatCaptain? helper = null;
            foreach (BoatCaptain c in s.Captains)
            {
                string verdict;
                string? unusable = s.BoatDecaying ? sinking : obstruction != null && !c.Mobile.Alive ? $"{obstruction}, and they are a ghost" : null;

                if (unusable != null)
                    verdict = $"cannot sail it ({unusable})";
                else if (c.Association == Association.Account || c.Association == Association.Hardware)
                    verdict = "HELP (common control: hostility does not count)";
                else if (c.Association == Association.None)
                    verdict = "no help (no evidence of association)";
                else if (c.Hostility != null)
                    verdict = "no help (hostile)";
                else
                    verdict = "HELP";

                trace.Note($"captain {c.Mobile.Name} ({(c.Mobile.Alive ? "alive" : "ghost")}): {c.Authority}; association {c.Association}"
                    + $"; hostility {c.Hostility ?? "none recorded"}; shared login IP {(c.SharedLoginIP ? "yes" : "no")} (forensic only) -> {verdict}");

                if (helper == null && verdict.StartsWith("HELP"))
                    helper = c;
            }

            if (helper != null)
            {
                trace.Decide("Assistance", $"{helper.Mobile.Name} ({helper.Association}) can sail it", "NOT STRANDED");
                return Refuse(trace, new GateMessage("Your companion aboard can sail this ship."));
            }
            trace.Skip("Assistance", s.Captains.Count == 0 ? "no other captain aboard" : $"{s.Captains.Count} other captain(s), none associated and able");

            // stranded: ashore where Strandedness puts a stranded player, or nowhere
            if (s.Shore == Point3D.Zero)
            {
                trace.Fail("StrandedOnABoat", "stranded, but Strandedness finds no shore location");
                return Refuse(trace, new GateMessage("Help Stuck cannot find a shore for you here. Please page for staff assistance."));
            }

            trace.Take("StrandedOnABoat", $"no way off and no associated captain -> shore {s.Shore}");
            return new RescuePlan(RescuePolicies.StrandedOnABoat, s.Shore, s.Map);
        }

        private static string AuthorityText(CommandAuthority authority) => authority switch
        {
            CommandAuthority.Staff => "staff",
            CommandAuthority.Key => "a key or key ring in their backpack",
            CommandAuthority.CorpseKey => "their corpse aboard holds the key",
            CommandAuthority.ClosestPlayer => "Siege: closest to the tillerman, no key on deck",
            _ => authority.ToString(),
        };
        #endregion Request

        #region Expiry
        // TeleportTimer.OnTick: is the delayed rescue still valid? (A forced one goes ahead regardless.)
        // message: what a failing gate says (null: the generic message)
        public static bool ValidAtExpiry(RescueTicket ticket, StuckTrace trace, out GateMessage? message)
        {
            message = null;
            Mobile m = ticket.Mobile;

            if (m.Location != ticket.OriginalLocation || m.Map != ticket.OriginalMap)
            {
                trace.Fail("NotMoved", $"was {ticket.OriginalLocation} {ticket.OriginalMap}, now {m.Location} {m.Map}");
                return false;
            }
            trace.Pass("NotMoved", $"{m.Location} {m.Map}");

            if (ticket.Force && !ticket.Policy.RevalidateWhenForced)
                return true;

            StuckStatus now = new StuckStatus(m, StuckMode.Normal);

            // the house rescue's exemptions hold only while they are still a friend inside the same house
            if (ticket.Policy.Kind == RescueKind.HouseRescue)
            {
                bool authorized = now.House != null && now.House == ticket.House && now.HouseFriend;
                string observed = now.House == null ? "not inside a house" : now.House != ticket.House ? "inside a different house" : $"owner/co-owner/friend={now.HouseFriend}";
                if (!authorized)
                {
                    trace.Fail("HouseAuthorization", observed);
                    return false;
                }
                trace.Pass("HouseAuthorization", observed);
            }

            GateExemptions exempt = ticket.Policy.Exemptions;

            // PARITY: decided here, from the move's distance and the region they are in now
            if (ticket.DungeonEvacuation)
            {
                exempt |= GateExemptions.Weight;
                trace.Note("dungeon evacuation: weight not enforced (belongings stay behind in a recovery chest)");
            }

            // the DailyUses gate applies when the ticket's rescue needs a use (RescuePolicy.NeedsUse)
            if (!Eligible(now, exempt, ticket.Policy, trace, out _, out message))
                return false;

            if (ticket.Policy.RequireNoExitsAtExpiry)
            {
                if (now.ExitOptions.Count != 0)
                {
                    trace.Fail("NoReachableExits", $"{now.ExitOptions.Count} reachable exit(s)");
                    return false;
                }
                trace.Pass("NoReachableExits", "none reachable");
            }

            return true;
        }
        #endregion Expiry

        #region Reporting
        // a dry run's report: the trace and what the plan would do
        public static IEnumerable<string> Describe(StuckStatus s, RescuePlan plan)
        {
            RescuePolicy p = plan.Policy;
            switch (plan.Kind)
            {
                case RescueKind.Refused:
                    yield return plan.Refusal != null ? $"Refused: \"{plan.Refusal}\"" : "Refused: \"You do not appear to be in a valid stuck location. Use help-stuck again for more information.\"";
                    yield break;
                case RescueKind.StrandedAtSea:
                    yield return $"Stranded at sea: moved ashore, at once, to {plan.Destination} {plan.Map} (the Strandedness shore); pets are not moved";
                    yield return "  uses: 1, now";
                    yield break;
                case RescueKind.StrandedOnABoat:
                    yield return $"Stranded on a boat: moved ashore, at once, to {plan.Destination} {plan.Map} (the Strandedness shore); pets are not moved";
                    yield return "  weight enforced (checked at request: no ticket)";
                    yield return "  uses: 1, now";
                    yield break;
                case RescueKind.ExitHints:
                    yield return "Exit hints, no move, no use charged:";
                    foreach (string line in HelpStuckProbes.ExitHintLines(s.Mobile, s.ExitOptions))
                        yield return "  " + line;
                    yield break;
                case RescueKind.QuestArea:
                    yield return "Quest area: \"Sorry, there is no help stuck available here.\" No use charged.";
                    yield break;
                case RescueKind.TownChoice:
                    yield return "Town menu (frozen while it is open, up to 3 minutes). A chosen town is a TownMove:";
                    p = RescuePolicies.TownMove;
                    break;
                default:
                    yield return $"{plan.Kind}{(plan.Source != "" ? " (" + plan.Source + ")" : "")} to {plan.Destination} {plan.Map}{(p.Force ? ", forced" : "")}";
                    break;
            }

            if (p.DelayText != "")
                yield return $"  delay {p.DelayText}, frozen meanwhile";
            if (p.Chargeable)
                yield return p.Delay != null || p.Kind == RescueKind.TownMove
                    ? "  uses: none now; 1 when the move happens; none if it is cancelled"
                    : "  uses: 1, now";
            else
                yield return "  uses: none (free rescue)";
            if (p.Exemptions != GateExemptions.None)
                yield return $"  exempt from gates: {p.Exemptions} (at request and at expiry)";
            if (!p.Immediate)
                yield return "  at expiry: must not have moved" + (p.Force ? " (ignored: forced)" : "")
                    + (p.Kind == RescueKind.HouseRescue ? "; still a friend inside the same house" : "")
                    + (p.Exemptions.HasFlag(GateExemptions.Weight) ? "; gates again" : "; gates again, weight enforced unless a dungeon evacuation")
                    + (p.RequireNoExitsAtExpiry ? "; no reachable exits" : "");
            if (p.Kind == RescueKind.HouseRescue)
                yield return "  then: pets and player moved to the house ban location (BaseHouse.BanLocation)";
        }

        public static void LogDecision(Mobile m, string phase, StuckTrace trace)
        {
            LogHelper log = new LogHelper("HelpStuckDecisions.log", overwrite: false, sline: true, quiet: true);
            log.Log(LogType.Mobile, m, $"{phase}: {trace.Summary}");
            log.Finish();
        }
        #endregion Reporting
    }
}
