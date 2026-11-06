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

/* Scripts/Engines/Help/StuckMenu.cs
 * Changelog:
 *  10/15/2026, Adam (Help Stuck: HelpStuck.log history)
 *      Overview only: HelpStuck.log now records every move carried out (immediate ones too) and each refusal at
 *      request, with what the player was told (see HelpStuck).
 *  10/14/2026, Adam (Help Stuck: stranded on a boat; module overview)
 *      Aboard a boat, Help Stuck now decides StrandedOnABoat (see StuckRules.PlanStrandedOnABoat) and nothing else:
 *      no local unblock or town menu aboard. The HELP-STUCK SYSTEM overview below is rewritten for the system as it
 *      now is (the pipeline, immediate and delayed rescues, revalidation, uses, the rescue kinds, dungeons, the
 *      refusals, the layout, diagnostics); the pre-refactor operational summary is gone.
 *  10/13/2026, Adam (Help Stuck refactor)
 *      Behavior-preserving refactor. The Help Stuck engine moved out of StuckMenu.cs into HelpStuck/:
 *          HelpStuckStatus.cs      StuckStatus: the facts (cheap ones read at once, expensive ones on demand)
 *          HelpStuckRules.cs       the policies, gates and decision trees, and the decision trace
 *          HelpStuckRescue.cs      carrying out the decision: the request, the delayed rescue (RescueTicket,
 *                                  formerly TeleportTimer) and its execution
 *          HelpStuckProbes.cs      world probes and searches (unblock destination, exit options, ...)
 *          HelpStuckRecovery.cs    the dungeon evacuation recovery chest
 *      StuckMenu keeps the town menu, the commands and DoHelpStuck (now HelpStuck.Request).
 *      [HelpStuckTest is now a dry run: it reports the facts, rules and plan and changes nothing.
 *      [HelpStuckTest live is the previous [HelpStuckTest.
 *      When Core.Debug, any access level may use [HelpStuckTest, [HelpStuckReset and [HelpStuckUses.
 *      Out of daily uses now says so, instead of the generic 'not a valid stuck location' message.
 *      Inside a house they own, co-own or are a friend of, Help Stuck moves them (and their pets) to the house ban
 *      location as an authorized house rescue: no use charged, weight ignored. Anyone else inside a house is unchanged.
 *      A chargeable rescue now costs exactly one use, when it is carried out; scheduling or a cancelled rescue costs none.
 *      [HelpStuckUses: shows a player's recorded uses, uses left and any pending rescue, read only.
 *      In a dungeon they can walk around, a reachable exit is now pointed out instead of "But, you're not stuck.",
 *      and exit hints are given even with no Help Stuck use left (they move nobody and cost nothing).
 *      Only an outcome that needs a use is refused for having none left: with no use left, exit hints, "But, you're
 *      not stuck.", the quest-area message, a house rescue and any other refusal (with its own reason) still come through.
 *      Physically blocked in a dungeon, the short local move (DungeonUnblock) no longer requires dropping weight; it still
 *      costs one use on success. House, township and overworld local unblocks still enforce weight.
 *  1/2/2026, Adam
 *      Help-Stuck: fix dungeon evacuation logic; replace kill with recovery chest; refactor TeleportTimer
 *      Fixed a logic bug where players could be killed (or penalized) even when Help-Stuck successfully performed a local unblock (short-range move).
 *      Introduced an explicit evacuation check (isEvac) so dungeon consequences apply only when the player is being extracted (e.g., town relocation), not for nearby remediation.
 *      Replaced dungeon “kill on evac” behavior with a recovery-chest consequence on all shards:
 *      Cursor-held items are dropped first to prevent exploits.
 *      Non-essential carried items are moved to a locked, immovable chest spawned near the original stuck location.
 *      Chest is re-keyed, key is given to the player at teleport time, and chest decays after 1 hour.
 *      Prevents free loot transport without killing the player.
 *      Refactored consequence handling into a dedicated routine (ApplyDungeonEvacuationConsequences) and cleaned up TeleportTimer.OnTick() flow for clarity.
 *      General cleanup and comment improvements around Help-Stuck relocation paths (local unblock vs. evac).
 *      Help-Stuck: finalize recovery chest behavior (decay, keying, lockpick)
 *      Moved recovery chest decay logic into the chest itself; expiry time is now owned, serialized, and enforced across restarts.
 *      Added explicit expiration timestamp and self-managed decay timer to prevent indefinite recovery chests after shard reboot.
 *      Recovery chest is now re-keyed per instance and provides the evac’d player with a Blessed key at teleport time.
 *      Enabled lockpicking on recovery chests with high-tier dungeon difficulty (level 4), allowing opportunistic recovery by skilled pickers.
 *      Preserves anti–loot-transport guarantees while enabling emergent dungeon gameplay.
 *  6/7/2023, Adam (exit options)
 *      Add public moongates to the list of exit options.
 *      Also exclude teleporters and moongates as exit options if they lead to a dungeon.
 *  5/24/2023, Adam (StrandedAtSea())
 *      Check if they are StrandedAtSea without a boat (maybe the boat decayed.)
 *      If so, when we try to unstuck them, use the standard Strandedness.ProcessStranded()
 *  5/20/2023, Adam (AllowUnstuck())
 *      Disallow help stuck if:
 *          1. They are in a dungeon (all dungeons have teleporters)
 *          2. They can walk 8-10 tiles in some direction
 *          3. If T2A is not allowed, you get the Brit menu
 *  5/20/23, Yoar (Help Stuck)
 *      Complete refactor of help-stuck. Moved all code into StuckMenu.cs.
 *  12/2/22, Adam (Help Stuck)
 *      Overhaul of help stuck system.
 *          o) Add smart checks to see if the requester is actually near a means of escape.
 *              If so, give them some directional hints.
 *          o) Move mobile checks (is criminal etc.) from ValidUseLocation() into ValidUseMobile(). 
 *              Messaging is different.
 *          o) Reorganize logical flow. For instance, if they are physically bocked, try moving them first.
 *              If they are not physically blocked, begin the gumping process (or notify staff.)
 *  9/4/22, Yoar (WorldZone)
 *      Added support for world zone unstuck locations.
 *  8/28/22, Yoar
 *      Can no longer unstuck while holding a sigil.
 *  2/10/22, Adam (IsBlocked())
 *      Use the new mobile IsBlocked() function to see if they are truly stuck
 *      Add a check in TeleportTimer to see if they moved (like they had precast Teleport before hitting Help)
 *      If they are not in the same location as when they asked for help, ignore the request.
 *	6/18/10, Adam
 *		Update region logic to reflect shift from static to new dynamic regions
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	3/18/07, Pix
 *		Fixed logic for all cases for help stuck.
 *	3/2/07, Pix
 *		Implemented random selection of 3 towns and 6 shrines for help stuck.
 *	3/1/07, Pix
 *		Tweaks to last night's change.
 *	2/28/07, Pix
 *		Modifications to the help-stuck system.
 *  08/03/06 Taran Kain
 *		Re-added OnTick() reentry blocking, after further investigation. Added explanatory comment.
 *	07/30/06 Taran Kain
 *		Removed OnTick() reentry blocking, could prevent it from running at all
 *	05/30/06, Adam
 *		- Make ValidUseLocation() succeed for staff
 *		- Also add comments and some cleanup.
 *		- Block OnTick() reentry (Open separate SR)
 *  05/01/06 Taran Kain
 *		Consolidated validation checks into ValidUseLocation() to help checking at more than one point in the process.
 *  04/11/06, Kit
 *		Made check IsInSecondAgeArea public for use with testing at help menu to prvent helpstuck in lostlands.
 *	06/04/05, Pix
 *		Force mobile to drop whatever they're holding on HelpStuck teleport.
 *  03/14/05, Lego
 *           added Oc'Nivelle to help stuck menu
 *  11/09/04, Lego
 *           fixed problem with delucia teleporting
 *  9/26/04, Pigpen
 *		Added Buc's Den to Old Lands Locations and added Location in South East T2A to T2A Locations
 *	9/17/04, Pigpen
 *		Changed Cove teleport location, fixing problem of stuck players teleporting into wall of new Rug Shop.
 *  7/12/04, Pix
 *		Fixed problem with help-stuck option where if the person instalogs while
 *		waiting for teleport timer, they get teleported to trammel.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

/*
===============================================================
                     HELP-STUCK SYSTEM
===============================================================

    Purpose
    -------
    Help Stuck gets a player out of a place they genuinely cannot leave
    (world geometry, a dungeon corner, open water, a boat they cannot
    sail) without staff, and without becoming a free teleport, a way to
    carry goods out, or a way to escape a fight or the law. Two
    principles, in this order:

        1. A player who is genuinely stuck gets out.
        2. A player who is not stuck, or who arranged to look stuck,
           gets advice or a refusal, never a free ride.

    So Help Stuck prefers the least it can do: point out a way out, make
    a short local move, and only when nothing else works, a move ashore
    or to a town. Help Stuck never falls back to a staff page.

    Pipeline
    --------
        StuckStatus (facts) -> gates (eligibility) -> rescue planning
            -> use check (and an immediate rescue's weight check) -> execution

    o Facts (StuckStatus): what is true about the player and their
      surroundings at one moment. Cheap facts are read at once, expensive
      ones (pathfinding, area scans) when a rule first reads them. Facts are
      side-effect free (the region's own refusal message aside).
    o Gates (StuckRules.Eligible): the refusals, in a fixed order; the first
      that fails is the one the player hears. Staff bypass them, except
      under [HelpStuckTest, which judges staff as players.
    o Planning (StuckRules.PlanRequest): exactly one RescuePlan per request.
      The rules decide; they never message, charge, schedule or move.
    o Use check: decided on the outcome. Only an outcome that needs a use
      (RescuePolicy.NeedsUse) is refused for having none left.
    o Execution (HelpStuck): refuse, hint, open the town menu, move now, or
      schedule a RescueTicket. Rescue kinds are policies (RescuePolicies):
      delay, exemptions, use accounting, checks at expiry.

    Immediate and delayed rescues
    -----------------------------
    Immediate (stranded at sea, stranded on a boat): carried out at
    request, no ticket. With no expiry, the weight gate is applied at
    request (stranded at sea is exempt from it; stranded on a boat is not).
    Delayed (local unblock, dungeon unblock, house rescue: 10-60 s; the
    town move: 10-120 s, at once and forced when staff send the menu): the
    player is frozen until it comes due (hidden too, for a dungeon
    evacuation).

    Revalidation at expiry (StuckRules.ValidAtExpiry), on fresh facts:
    the player must not have moved; the gates run again (uses included,
    weight unless exempt or a dungeon evacuation); a house rescue needs them
    to still be a friend inside the same house; and most rescues are
    cancelled by a reachable exit. A forced staff move goes ahead anyway.

    Uses
    ----
    Two per character per 24 hours. A chargeable rescue costs exactly one
    use, charged once, when it is carried out (a move that actually
    happened). Scheduling, a cancelled rescue, and a rescue that could not
    move them cost nothing. Free, whatever the use count: refusals with
    their reasons, exit hints, the quest-area message, the house rescue,
    and every "you are not stranded" answer aboard a boat.

    Rescue kinds
    ------------
    o HouseRescue: an owner, co-owner or friend of the house they are
      inside is moved, with pets, to its ban location. Free, weight exempt;
      a reachable exit does not cancel it. Anyone else in a house gets the
      ordinary local unblock.
    o StrandedAtSea: in the water without a boat. Moved ashore where
      Strandedness puts stranded players; pets stay. Weight does not apply.
    o StrandedOnABoat: aboard a boat with no way off it. A boat is special
      because getting off it depends on who controls it: a player is not
      stranded if they can leave by a plank or sail it, or an associated
      captain aboard can, and another captain aboard is not help merely
      because they could sail (they may be a stranger or a captor). Once
      aboard, the boat rules are terminal: no local unblock or town menu
      (that would be a boat-to-shore shuttle). Moved ashore as for
      stranded at sea; pets stay; weight applies. The policy and its
      reasoning: StuckRules.PlanStrandedOnABoat. The boat mechanics it asks:
      BaseBoat (GetCommandAuthority, FindTillerObstruction, DecayDue) and
      Plank (FindDisembarkLocation). The two sea states are deliberately
      separate; neither is a special case of the other.
    o LocalUnblock: a short move. First a designated way out (a house's
      ban location, a township edge), which leaves the region by design.
      Otherwise, only when physically blocked: a spot within 4 tiles that
      they cannot walk to, in line of sight and in the same region. A move
      never goes through a wall or into another region, and never straight
      up or down (Z correction was removed: it was an unchecked vertical
      teleport). Blocked only by movable items (a manufactured blockage of
      crates, kegs, ...): no local unblock, and the scene is recorded for
      staff (RecordCheater, with possible accomplices; a record, not a
      sanction). The other rescues still apply.
    o DungeonUnblock: the physically blocked local unblock, in a dungeon.
      Weight not enforced (the move stays in the dungeon).
    o ExitHints: a reachable exit (moongate, teleporter, healer for a
      ghost) is pointed out instead of a move. Free.
    o QuestArea: off Felucca with no way out: nothing Help Stuck can do.
    o TownChoice / TownMove: hopeless: the town menu, then a delayed move.

    Dungeons
    --------
    Dungeons all have exits. A player who can walk 8+ tiles in a dungeon is
    not stuck (they are shown a reachable exit if there is one). A town move
    out of a dungeon is an evacuation: their belongings are placed in a
    locked, decaying recovery chest at the spot they left (key given to
    them), so nothing is carried out and weight is not enforced
    (HelpStuckRecovery).

    Where Help Stuck is refused
    ---------------------------
    Disabled (feature bit), Test Center clones, prison and jail, a dungeon
    they can walk around, criminal travel locked, recent combat (a fight
    they started, within 30 s), carrying a sigil or the flag, overweight (101+
    stones; see above for when), a loaded pack animal, and any region that
    refuses it (Region.CanUseStuckMenu: Angel Island, duel safe zones, ...).

    Code layout (HelpStuck/)
    ------------------------
        StuckMenu.cs            this overview; the town menu; the commands; DoHelpStuck
        HelpStuckStatus.cs      StuckStatus: the facts
        HelpStuckProbes.cs      the world questions behind the facts (no side effects)
        HelpStuckRules.cs       policies, gates, the request and expiry decisions, StuckTrace
        HelpStuckRescue.cs      HelpStuck: carrying out a plan; RescueTicket; use charging
        HelpStuckRecovery.cs    the dungeon evacuation recovery chest
    Boat and sea mechanics stay in their own code (BaseBoat, Plank,
    Strandedness); Help Stuck asks them side-effect-free questions.

    Diagnostics and logs
    --------------------
    [HelpStuckTest: a dry run. Judges the caller as a player, changes
    nothing, and reports every rule checked, the facts it read, and what
    the plan would do. [HelpStuckTest live: carries it out (a local unblock
    moves at once; a test is never charged). [HelpStuckUses: a player's
    uses and any pending rescue. [HelpStuckReset: clears uses (not on
    production).
    HelpStuckDecisions.log: one line per request and per expiry, with the
    deciding rule. HelpStuck.log: the history, not the reasoning: each move
    carried out (delayed, immediate, house rescue: "Moved stuck player to
    ...", the record's location being where they were) and each refusal at
    request ("Refused stuck player: ..." with what they were told).
    Cheater.log / Watchlist.log: recorded suspicious circumstances
    (RecordCheater).
    Messages: exit hints overhead (the character noticing a way out);
    everything else in the system channel.
    ===============================================================
*/


using Server.Diagnostics;
using Server.Engines;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.Targeting;
using System;
using System.Collections.Generic;
using static Server.Utility;
namespace Server.Menus.Questions
{
    public class StuckMenu : Gump
    {
        public const int MAXHELPSTUCKALIVEWEIGHT = StuckRules.MaxWeight;

        private static readonly Point3D m_DefaultLocation = new Point3D(1420, 1698, 0); // WBB

        private static Point3D[] m_Locations = new Point3D[]
            {
                new Point3D( 1522, 1757, 28 ), //0: Britain
				new Point3D( 2005, 2754, 30 ), //1: Trinsic
				new Point3D( 2973,  891,  0 ), //2: Vesper
				new Point3D( 2498,  392,  0 ), //3: Minoc
				new Point3D(  490, 1166,  0 ), //4: Yew
				new Point3D( 2249, 1192,  0 ), //5: Cove
				new Point3D( 2716, 2182,  0 ), //6: Buccaneer's Den
                new Point3D(  825, 1072,  0 ), //7: Oc'Nivelle
				new Point3D( 5720, 3109, -1 ), //8: Papua
				new Point3D( 5216, 4033, 37 ), //9: Delucia
				new Point3D( 5884, 3596,  1 ), //10: South East Lost Lands

				//Added for AutoMove option:
                new Point3D( 1458, 843, 7 ), //11: chaos shrine
				new Point3D( 1858, 873, -1 ), //12: compassion shrine
				new Point3D( 1728, 3528, 3 ), //13: honor shrine
				new Point3D( 1301, 635, 16 ), //14: justice shrine
				new Point3D( 3355, 292, 4 ), //15: sacrifice shrine
				new Point3D( 1600, 2490, 12 ), //16: spirituality shrine
			};

        public static Point3D[] GetUnstuckLocations(Mobile m)
        {
            #region World Zone
            if (WorldZone.IsInside(m.Location, m.Map))
                return WorldZone.ActiveZone.UnstuckLocations;
            #endregion

            return m_Locations;
        }

        public static Point3D GetUnstuckLocation(Mobile m, int index)
        {
            Point3D[] locations = GetUnstuckLocations(m);

            if (index >= 0 && index < locations.Length)
                return locations[index];

            return m_DefaultLocation;
        }

        public static bool IsInSecondAgeArea(Mobile m)
        {
            if (m.Map != Map.Trammel && m.Map != Map.Felucca)
                return false;

            if (m.X >= 5120 && m.Y >= 2304)
                return true;

            if (m.Region.IsPartOf("Terathan Keep"))
                return true;

            return false;
        }

        private Mobile m_Mobile, m_Sender;
        private bool m_MarkUse;

        private Timer? m_Timer;

        public StuckMenu(Mobile beholder, Mobile beheld, bool markUse)
            : base(150, 50)
        {
            m_Sender = beholder;
            m_Mobile = beheld;
            m_MarkUse = markUse;

            Closable = false;
            Dragable = false;

            AddPage(0);

            AddBackground(0, 0, 280, 400, 2600);

            AddHtmlLocalized(50, 25, 170, 40, 1011027, false, false); //Chose a town:
            // adam: if they are in T2A, and Core.RuleSets.AllowLostLandsAccess() == false, they should not be here
            if (!IsInSecondAgeArea(beheld) || !Core.RuleSets.AllowLostLandsAccess())
            {
                AddButton(50, 60, 208, 209, 1, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 60, 335, 40, 1011028, false, false); // Britain

                AddButton(50, 95, 208, 209, 2, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 95, 335, 40, 1011029, false, false); // Trinsic

                AddButton(50, 130, 208, 209, 3, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 130, 335, 40, 1011030, false, false); // Vesper

                AddButton(50, 165, 208, 209, 4, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 165, 335, 40, 1011031, false, false); // Minoc

                AddButton(50, 200, 208, 209, 5, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 200, 335, 40, 1011032, false, false); // Yew

                AddButton(50, 235, 208, 209, 6, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 235, 335, 40, 1011033, false, false); // Cove

                AddButton(50, 270, 208, 209, 7, GumpButtonType.Reply, 0);
                AddHtml(75, 270, 335, 40, "Buccaneer's Den", false, false); // Buccaneer's Den

                AddButton(50, 305, 208, 209, 8, GumpButtonType.Reply, 0);
                AddHtml(75, 305, 335, 40, "Oc'Nivelle", false, false); //Oc'Nivelle
            }
            else
            {

                AddButton(50, 60, 208, 209, 9, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 60, 335, 40, 1011057, false, false); // Papua

                AddButton(50, 95, 208, 209, 10, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 95, 335, 40, 1011058, false, false); // Delucia

                AddButton(50, 120, 208, 209, 11, GumpButtonType.Reply, 0);
                AddHtml(75, 130, 335, 40, "South East Lost Lands", false, false); // South East Lost Lands

            }

            AddButton(55, 340, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(90, 340, 320, 40, 1011012, false, false); // CANCEL
        }

        public void BeginClose()
        {
            StopClose();

            m_Timer = new CloseTimer(m_Mobile);
            m_Timer.Start();

            m_Mobile.Frozen = true;
        }

        public void StopClose()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            m_Mobile.Frozen = false;
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            StopClose();

            if (info.ButtonID == 0)
            {
                if (m_Mobile == m_Sender)
                    m_Mobile.SendLocalizedMessage(1010588); // You choose not to go to any city.
            }
            else if (!IsInSecondAgeArea(m_Mobile) || !Core.RuleSets.AllowLostLandsAccess())
            {
                if (info.ButtonID >= 1 && info.ButtonID <= 8)
                    Teleport(info.ButtonID - 1);
            }
            else if (info.ButtonID == 9 || info.ButtonID == 10 || info.ButtonID == 11)
            {
                Teleport(info.ButtonID - 1);
            }
        }

        private void Teleport(int index)
        {
            Point3D loc = GetUnstuckLocation(m_Mobile, index);

            if (m_MarkUse)
            {
                m_Mobile.SendLocalizedMessage(1010589); // You will be teleported within the next two minutes.

                HelpStuck.Schedule(m_Mobile, loc, Map.Felucca, TimeSpan.FromSeconds(10.0 + (Utility.RandomDouble() * 110.0)), m_Sender.AccessLevel > AccessLevel.Player, RescuePolicies.TownMove);
            }
            else
            {
                HelpStuck.Schedule(m_Mobile, loc, Map.Felucca, TimeSpan.Zero, m_Sender.AccessLevel > AccessLevel.Player, RescuePolicies.TownMove);
            }
        }

        public static void Initialize()
        {
            // Core.Debug: anyone may use the Help Stuck test commands
            CommandSystem.Register("HelpStuckTest", Core.Debug ? AccessLevel.Player : AccessLevel.Counselor, new CommandEventHandler(HelpStuckTest_OnCommand));
            if (Core.Debug || TestCenterServer.Enabled == true || Core.ReleasePhase < ReleasePhase.Production)
                CommandSystem.Register("HelpStuckReset", AccessLevel.Player, new CommandEventHandler(HelpStuckReset_OnCommand));
            CommandSystem.Register("HelpStuckUses", Core.Debug ? AccessLevel.Player : AccessLevel.Counselor, new CommandEventHandler(HelpStuckUses_OnCommand));
        }

        #region Commands

        [Usage("HelpStuckTest [live]")]
        [Description("Judges you as a player would be judged by Help Stuck. Without 'live': reports the facts, rules and plan, changing nothing. With 'live': carries it out (a local unblock moves you at once and is not charged).")]
        private static void HelpStuckTest_OnCommand(CommandEventArgs e)
        {
            bool live = e.Arguments.Length > 0 && Insensitive.Equals(e.Arguments[0], "live");

            HelpStuck.Request(e.Mobile, live ? StuckMode.LiveTest : StuckMode.DryRun);
        }

        private static void HelpStuckReset_OnCommand(CommandEventArgs e)
        {
            if (Core.ReleasePhase == ReleasePhase.Production && !Core.Debug)
            {
                e.Mobile.SendMessage("You cannot reset help stuck on this shard.");
            }
            else
            {
                if (e.Mobile is PlayerMobile)
                    ((PlayerMobile)e.Mobile).ClearStuckMenu();

                e.Mobile.SendMessage("Help Stuck reset.");
            }
        }

        [Usage("HelpStuckUses")]
        [Description("Shows a player's Help Stuck uses: each recorded use, how many are left, and any pending rescue. Changes nothing.")]
        private static void HelpStuckUses_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the player whose Help Stuck uses you wish to see.");
            e.Mobile.Target = new HelpStuckUsesTarget();
        }

        private class HelpStuckUsesTarget : Target
        {
            public HelpStuckUsesTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is not PlayerMobile pm)
                {
                    from.SendMessage("That is not a player.");
                    return;
                }

                // read only: Mobile.StuckMenuUses is a copy
                DateTime now = DateTime.UtcNow;
                DateTime[]? uses = pm.StuckMenuUses;
                int left = HelpStuck.UsesRemaining(pm);

                from.SendMessage($"Help Stuck uses for {pm.Name} at {now:yyyy-MM-dd HH:mm:ss} UTC:");
                from.SendMessage($"  {left} of {HelpStuck.UsesPerDay} left, can use now={pm.CanUseStuckMenu()}");

                if (uses == null)
                    from.SendMessage("  no uses recorded (never used, or reset)");
                else
                    for (int i = 0; i < uses.Length; i++)
                    {
                        if (uses[i] == DateTime.MinValue)
                            from.SendMessage($"  slot {i}: never used");
                        else if (now - uses[i] > HelpStuck.UseLifetime)
                            from.SendMessage($"  slot {i}: used {uses[i]:yyyy-MM-dd HH:mm:ss} UTC, free again ({(now - uses[i]).TotalHours:F1} h ago)");
                        else
                            from.SendMessage($"  slot {i}: used {uses[i]:yyyy-MM-dd HH:mm:ss} UTC, ACTIVE, frees at {uses[i] + HelpStuck.UseLifetime:yyyy-MM-dd HH:mm:ss} UTC");
                    }

                RescueTicket? pending = HelpStuck.Pending(pm);
                if (pending == null)
                    from.SendMessage("  no rescue pending");
                else
                    from.SendMessage($"  pending: {pending.Policy.Kind} to {pending.TargetLocation} {pending.TargetMap}, due in {Math.Max(0, (pending.Due - now).TotalSeconds):F0} s, "
                        + (pending.Policy.Chargeable ? "will charge 1 use if it happens" : "free"));
            }
        }

        #endregion

        private class CloseTimer : Timer
        {
            private Mobile m_Mobile;
            private DateTime m_End;

            public CloseTimer(Mobile m)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {
                m_Mobile = m;
                m_End = DateTime.UtcNow + TimeSpan.FromMinutes(3.0);
            }

            protected override void OnTick()
            {
                if (m_Mobile.NetState == null || DateTime.UtcNow > m_End)
                {
                    m_Mobile.Frozen = false;
                    m_Mobile.CloseGump(typeof(StuckMenu));

                    Stop();
                }
                else
                {
                    m_Mobile.Frozen = true;
                }
            }
        }

        // HelpGump: Help -> Stuck. See HelpStuck (HelpStuck/HelpStuckRescue.cs).
        public static bool DoHelpStuck(Mobile from)
        {
            return HelpStuck.Request(from, StuckMode.Normal);
        }

        public const int UnblockRange = HelpStuckProbes.UnblockRange;
    }
}
