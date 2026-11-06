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

/* Scripts/Engines/Confinement/ConfinementExecute.cs
 * ChangeLog
 *  10/17/26, Adam
 *      Stage 3 of the confinement architecture: Confinement.Execute(), the coordinator for staff jail/prison moves.
 *      It re-plans (Confinement.Plan), checks everything that can refuse before changing anything, then runs the
 *      Stage 2 operations in a fixed order: source check-out, destination check-in, final placement.
 *          Relocate  move only; no confinement state touched, never a boot
 *          Release   staff check-out (prison: the staff boot's sentence clearing + parole check-out; jail: the jail boot's),
 *                    then placement
 *          Admit     full check-in through the existing jailing/imprisonment code (Jail.DoJailing, PrisonPlayer.GoToPrison),
 *                    then placement
 *          Transfer  jail -> prison: destination check-in first, then the jail bookkeeping;
 *                    prison -> jail/prison: prison check-out first, then the destination check-in
 *      Staff moves only (TransitionMode.Administrative). Parole, escape, the jail exit and enforcement keep their own code.
 *      Never clears account history ([boot ex stays with the [boot command) and never reduces murder counts.
 *      Facet-local: jail and prison admissions use the destination facet's own facility; releases without a destination
 *      go to the release point on the facility's own facet (prison: the parole exit point; jail: the jail exit gate's).
 *      Logged-out characters are refused (nothing changed). The destination facility's warden's office / jail cell and
 *      the staging point are checked before anything moves. A check-in that does not take is reported as a partial
 *      failure, never as success; an admission that fails puts the character back where they were. Every executed
 *      transition is logged to Confinement.log.
 *  10/17/26, Adam (Stage 4)
 *      [boot ex: an explicit ClearAccountHistory request is carried out on an authorized staff release from prison
 *      (and refused for anything else), as part of the prison check-out, in [boot ex's order.
 *      DemoteToPlayer ([jail/[prison on lower-ranked staff): the character's access level becomes Player after preflight,
 *      just before the admission; if the admission does not take, the old level is restored with the old location.
 *  10/17/26, Adam (Stage 4, forward recovery)
 *      Preflight also refuses a target that is not a player character (Mobile.Player false: the admission would not give
 *      the inmate flag) and a demotion of an Owner or System character. A prison admission, once PrisonPlayer has started,
 *      is settled forward and never undone (SettlePrisonAdmission): an inmate whose placement failed is moved to the
 *      warden's office (PartialFailure, placement corrected); without the inmate flag the character is held outside the
 *      prison (the staging point; their jail spot for jail -> prison, jail record kept) and reported UNRESOLVED with what
 *      the admission did and did not do. The demotion is kept, nothing is returned or repeated. Free -> prison,
 *      jail -> prison and prison -> prison alike. A jail admission that does not take is unchanged.
 *  10/17/26, Adam (admission refactor R5)
 *      Admit and Transfer call the shared admission runner directly (PrisonAdmission / JailAdmission) on the destination
 *      facet instead of PrisonPlayer.GoToPrison / Jail.DoJailing, with no staging at the release point (and no staging
 *      check in Preflight; the prison check is the runner's entrance point). A prison -> prison transfer no longer goes
 *      through the staff parole and a new admission: the inmate keeps their sentence and bookkeeping and is moved into the
 *      destination prison (Transfer profile). A failed prison admission is reported from the runner's result.
 *      The settlement and snapshot code is no longer used (removed in R6).
 *  10/17/26, Adam (admission refactor R6)
 *      Removed the provisional Stage 4 recovery machinery the admission runner superseded: PrisonSettlement,
 *      PrisonAdmissionSnapshot, SettlePrisonAdmission and the hold-point plumbing. CheckIn now reports only whether a
 *      failed prison admission had changed anything (unresolved: reported as it is, never undone or returned).
 *  10/17/26, Adam (D5)
 *      Rejail (the Staff jail profile on the destination facet, no check-out) and ExtendSentence (the Extension profile in
 *      place) as coordinator operations, for the [jail and [prison commands. Preflight checks the cell for Rejail.
 *  10/18/26, Adam (Stage 5)
 *      Preflight asks the registered release vetoes (ConfinementVetoes) for Release, Transfer and Rejail before anything
 *      changes; a refusal is a Vetoed denial, which the staff callers already report. The coordinator holds no rule of
 *      its own about why (the Test Center quarantine registers one: TestCenterPolicy).
 */

using Server.Accounting;
using Server.Commands;
using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Engines.ConfinementSystem
{
    public enum ConfinementOutcome
    {
        Denied,         // refused before anything changed
        Done,           // the whole transition happened
        PartialFailure, // something changed, then a step did not take: see Steps and Message (never reported as success)
    }

    public sealed class ConfinementResult
    {
        public ConfinementOutcome Outcome { get; init; }
        public ConfinementPlan Plan { get; init; } = null!;
        public IReadOnlyList<ConfinementDenial> Denials { get; init; } = Array.Empty<ConfinementDenial>();
        public IReadOnlyList<string> Steps { get; init; } = Array.Empty<string>();
        public string Message { get; init; } = "";

        public bool Succeeded => Outcome == ConfinementOutcome.Done;
        /// <summary>All denial reasons, for staff.</summary>
        public string Reason => string.Join(" ", Denials.Select(d => d.StaffText));

        public override string ToString()
        {
            return string.Format("{0} {1}: {2}{3}", Plan.Operation, Outcome,
                Outcome == ConfinementOutcome.Denied ? Reason : string.Join(", ", Steps), Message.Length > 0 ? " (" + Message + ")" : "");
        }
    }

    public static partial class Confinement
    {
        /// <summary>Where the parole exit sends prisoners (AIParoleExit.DoTeleport); staff releases from prison use it too.</summary>
        public static readonly Point3D ReleasePoint = new Point3D(818, 1087, 0);

        /// <summary>
        /// Carry out a staff jail/prison move. Plans it again first; if anything would refuse, returns Denied with nothing
        /// changed. Otherwise checks out of the source, checks in to the destination and places the character.
        /// </summary>
        public static ConfinementResult Execute(ConfinementRequest request)
        {
            // revalidate immediately before acting
            ConfinementPlan plan = Plan(request);
            List<ConfinementDenial> denials = new(plan.Denials);
            Preflight(request, plan, denials);
            if (denials.Count > 0)
                return new ConfinementResult { Outcome = ConfinementOutcome.Denied, Plan = plan, Denials = denials };

            List<string> steps = new();
            ConfinementResult result;
            try
            {
                result = Run(request, plan, steps);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                result = Partial(plan, steps, "exception: " + ex.Message);
            }

            if (plan.Operation != ConfinementOperation.None)
                Log(request, plan, result);
            return result;
        }

        #region Preflight (no changes)
        // What Execute itself cannot do, on top of what Plan refuses.
        private static void Preflight(ConfinementRequest request, ConfinementPlan plan, List<ConfinementDenial> denials)
        {
            if (request.Authority != TransitionAuthority.Staff || request.Mode != TransitionMode.Administrative)
                denials.Add(new ConfinementDenial(ConfinementDenialCode.OperationNotAllowed,
                    "Only staff moves are carried out by the confinement coordinator; parole, escape, the jail exit and enforcement use their own code."));

            // account history ([boot ex) is cleared only on an authorized staff release from prison; the planner refuses
            //  the request for anything else, and it is applied only once the release itself goes ahead (StaffPrisonCheckOut)
            if (request.ClearAccountHistory && !(plan.Operation == ConfinementOperation.Release && plan.Source.Facility == Facility.Prison))
                denials.Add(new ConfinementDenial(ConfinementDenialCode.OperationNotAllowed,
                    "Clearing account history is done only by an explicit [boot ex from prison."));

            if (plan.Operation == ConfinementOperation.None)
                return;

            // the character must be in the world: a logged-out character's spot is its logout location on the internal map,
            //  which the check-in/check-out code cannot place safely
            if (!plan.Source.InWorld)
                denials.Add(new ConfinementDenial(ConfinementDenialCode.OfflineNotSupported,
                    "They are logged out; staff jail and prison moves need them in the world."));

            // vetoes registered by other systems (ConfinementVetoes): moving someone out of confinement or between
            //  confinements, asked before anything changes. A release goes to its destination, or stays on their facet
            if (request.Target is PlayerMobile vpm && vpm.Map != null && vpm.Map != Map.Internal
                && (plan.Operation == ConfinementOperation.Release || plan.Operation == ConfinementOperation.Transfer || plan.Operation == ConfinementOperation.Rejail))
            {
                Map to = plan.Operation == ConfinementOperation.Release
                    ? (plan.DestinationLocation != null && plan.DestinationMap != null ? plan.DestinationMap : vpm.Map)
                    : plan.DestinationMap ?? vpm.Map;
                string? why = ConfinementVetoes.Check(vpm, vpm.Map, to, leaving: plan.Operation == ConfinementOperation.Release, request.Actor);
                if (why != null)
                    denials.Add(new ConfinementDenial(ConfinementDenialCode.Vetoed, why));
            }

            if (plan.Operation != ConfinementOperation.Relocate && request.Target is not PlayerMobile)
                denials.Add(new ConfinementDenial(ConfinementDenialCode.NotAPlayer, "Only players can be sent to jail or prison."));

            bool admits = plan.Operation == ConfinementOperation.Admit || plan.Operation == ConfinementOperation.Transfer
                || plan.Operation == ConfinementOperation.Rejail || plan.Operation == ConfinementOperation.ExtendSentence;
            if (admits && request.Actor == null)
                denials.Add(new ConfinementDenial(ConfinementDenialCode.NoActor, "An admission needs the staff member who makes it."));

            // what the admission code itself would silently not do: the inmate flag and the confiscation need a player
            //  character (ConfinementBookkeeping.Applies), and a demotion must be able to make them a Player
            if (admits && request.Target is PlayerMobile apm)
            {
                if (!apm.Player)
                    denials.Add(new ConfinementDenial(ConfinementDenialCode.NotAPlayer, "That is not a player character; it cannot be jailed or imprisoned."));
                if (plan.Has(ConfinementEffects.DemoteToPlayer) && apm.AccessLevel >= AccessLevel.Owner)
                    denials.Add(new ConfinementDenial(ConfinementDenialCode.OperationNotAllowed, string.Format("An {0} character cannot be demoted.", apm.AccessLevel)));
            }

            // admission resources on the destination facet, checked before anything moves
            if ((plan.Operation == ConfinementOperation.Admit || plan.Operation == ConfinementOperation.Transfer || plan.Operation == ConfinementOperation.Rejail)
                && plan.DestinationMap != null && plan.DestinationMap != Map.Internal)
            {
                Map destMap = plan.DestinationMap;
                if (plan.DestinationFacility == Facility.Prison)
                {   // the admission runner places them at the prison entrance point on the destination facet
                    if (FacilityOf(Region.Find(PrisonAdmission.EntrancePoint, destMap)) != Facility.Prison)
                        denials.Add(new ConfinementDenial(ConfinementDenialCode.DestinationNotFound,
                            string.Format("The prison entrance point {0} is not inside a prison on {1}.", PrisonAdmission.EntrancePoint, destMap)));
                }
                else if (plan.DestinationFacility == Facility.Jail)
                {
                    Point3D cell = Jail.GetCell(request.JailCell);
                    if (cell == Point3D.Zero || FacilityOf(Region.Find(cell, destMap)) != Facility.Jail)
                        denials.Add(new ConfinementDenial(ConfinementDenialCode.DestinationNotFound,
                            string.Format("Jail cell {0} is not inside a jail on {1}.", request.JailCell, destMap)));
                }
            }

            if (plan.DestinationLocation != null && (plan.DestinationMap == null || plan.DestinationMap == Map.Internal))
                denials.Add(new ConfinementDenial(ConfinementDenialCode.NoDestination, "That destination is not in the world."));
        }
        #endregion Preflight

        #region Run
        /// <summary>Where the jail exit gate releases (JailExitSungate), on the jail's own facet.</summary>
        public static readonly Point3D JailReleasePoint = new Point3D(829, 1081, 5);

        // Order and failure guarantees (everything that can refuse has already been checked by Plan + Preflight):
        //   Relocate   move only
        //   Release    check-out, with the move inside it (prison: to the destination; jail: in the jail boot's place)
        //   Admit      check-in, then placement; if a jail check-in does not take they are put back where they were
        //   Transfer   jail -> prison: check-in FIRST, then the jail bookkeeping, so an unresolved admission leaves the jail
        //              stay untouched (and they are held in their jail spot)
        //              jail -> jail (other facet): the jail bookkeeping first (one record per character), then check-in
        //              prison -> prison (another facet): no parole or release: the inmate keeps their sentence and is moved
        //              into the destination prison (admission runner, Transfer profile); no release-point hop
        //              prison -> jail: the prison check-out must come first (pack, pets, flags); if the jailing then does
        //              not take they are left at the release point, checked out, and reported loudly
        //   Admissions run through the shared admission runner (ConfinementAdmission.cs) on the destination facet, with no
        //   staging. A refusal changes nothing; a prison admission that fails part-way is reported UNRESOLVED from the
        //   runner's own result, never undone or repeated (no restored access level, no return to where they were)
        private static ConfinementResult Run(ConfinementRequest request, ConfinementPlan plan, List<string> steps)
        {
            Mobile m = request.Target;
            Point3D? there = plan.DestinationLocation;

            switch (plan.Operation)
            {
                case ConfinementOperation.None:
                case ConfinementOperation.Relocate:
                    // a plain move: no boot, no confinement state touched
                    if (there is Point3D loc)
                    {
                        m.MoveToWorld(loc, plan.DestinationMap!);
                        steps.Add("moved");
                    }
                    return Done(plan, steps, plan.Operation == ConfinementOperation.Relocate && there == null ? "already there" : "");

                case ConfinementOperation.Release:
                    {
                        PlayerMobile pm = (PlayerMobile)m;
                        if (plan.Source.Facility == Facility.Prison)
                        {   // to the destination, or the parole exit's release point on the prison's own facet
                            StaffPrisonCheckOut(request, pm, there ?? ReleasePoint, there != null ? plan.DestinationMap! : pm.Map, steps);
                            if (pm.PrisonInmate || pm.Region.IsPrisonRules)
                                return Partial(plan, steps, "the prison check-out did not take");
                        }
                        else
                        {   // to the destination, or where the jail exit gate releases, on the jail's own facet
                            JailCheckOut(pm, there ?? JailReleasePoint, there != null ? plan.DestinationMap! : pm.Map, steps);
                            if (pm.Region.IsJailRules || JailSystem.TryGetReleaseTime(pm, out _))
                                return Partial(plan, steps, "the jail check-out did not take");
                        }
                        return Done(plan, steps, "");
                    }

                case ConfinementOperation.Admit:
                    {
                        PlayerMobile pm = (PlayerMobile)m;
                        Point3D origin = pm.Location;
                        Map originMap = pm.Map;

                        // [jail/[prison on lower-ranked staff: demote (the character, not the account) only now, after
                        //  every preflight passed; a jail admission that does not take puts the old rank back, a prison
                        //  admission once started and a release never do
                        AccessLevel formerLevel = pm.AccessLevel;
                        if (plan.Has(ConfinementEffects.DemoteToPlayer))
                        {
                            pm.AccessLevel = AccessLevel.Player;
                            if (pm.AccessLevel != AccessLevel.Player)
                            {   // nothing else has changed yet
                                if (pm.AccessLevel != formerLevel)
                                    pm.AccessLevelInternal = formerLevel;
                                return new ConfinementResult { Outcome = ConfinementOutcome.Denied, Plan = plan, Steps = steps,
                                    Denials = new[] { new ConfinementDenial(ConfinementDenialCode.OperationNotAllowed, "The demotion to Player did not take; nothing changed.") } };
                            }
                            steps.Add(string.Format("demoted from {0} to Player", formerLevel));
                        }

                        if (CheckIn(request, plan, pm, steps, out string why, out bool unresolved))
                            return Done(plan, steps, why);

                        if (unresolved)
                            return Partial(plan, steps, why);   // reported as it is: still demoted, not returned

                        Return(pm, origin, originMap, steps);
                        if (plan.Has(ConfinementEffects.DemoteToPlayer) && pm.AccessLevel != formerLevel)
                        {
                            pm.AccessLevelInternal = formerLevel;
                            steps.Add(string.Format("access level restored to {0}", formerLevel));
                        }
                        return Partial(plan, steps, why + "; returned to where they were");
                    }

                case ConfinementOperation.Transfer:
                    {
                        PlayerMobile pm = (PlayerMobile)m;
                        Point3D origin = pm.Location;
                        Map originMap = pm.Map;

                        if (plan.Source.Facility == Facility.Jail && plan.DestinationFacility == Facility.Jail)
                        {   // jail -> jail (another facet): one jail record per character, so close the old stay first
                            JailCheckOut(pm, null, null, steps);
                            if (!CheckIn(request, plan, pm, steps, out string jwhy, out _))
                            {
                                Return(pm, origin, originMap, steps);
                                return Partial(plan, steps, jwhy + "; returned to their jail spot (their old jail record was already closed)");
                            }
                            return Done(plan, steps, jwhy);
                        }

                        if (plan.Source.Facility == Facility.Jail)
                        {   // jail -> prison: admission first, the jail stay is only closed once the prison has them
                            //  (an unresolved admission holds them in their jail spot, jail record kept)
                            if (!CheckIn(request, plan, pm, steps, out string why, out bool unresolved))
                            {
                                if (unresolved)
                                    return Partial(plan, steps, why + "; their jail record is kept");
                                Return(pm, origin, originMap, steps);
                                return Partial(plan, steps, why + "; returned to their jail spot, jail stay unchanged");
                            }
                            JailCheckOut(pm, null, null, steps);
                            if (JailSystem.TryGetReleaseTime(pm, out _))
                                return Partial(plan, steps, "admitted to prison, but the jail record could not be removed");
                            return Done(plan, steps, why);
                        }

                        if (plan.DestinationFacility == Facility.Prison)
                        {   // prison -> prison (another facet): no parole, no release, no new sentence. The inmate keeps their
                            //  sentence and bookkeeping and is moved straight into the destination prison (runner: Transfer)
                            AdmissionResult tr = PrisonAdmission.Admit(new AdmissionContext(pm, Reporter(request), plan.DestinationMap!)
                            { Comment = request.Comment }, PrisonProfile.Transfer, requireInmate: true);
                            steps.Add(tr.ToString());
                            if (tr.Denied)
                                return new ConfinementResult { Outcome = ConfinementOutcome.Denied, Plan = plan, Steps = steps, Denials = tr.Denials };
                            if (!tr.Completed)
                                return Unresolved(plan, steps, pm, tr);
                            return Place(plan, pm, there, steps, "transferred, sentence kept");
                        }

                        // prison -> jail: out of prison through the release point, as the staff boot always did, then jailed
                        StaffPrisonCheckOut(request, pm, ReleasePoint, pm.Map, steps);
                        if (pm.PrisonInmate || pm.Region.IsPrisonRules)
                            return Partial(plan, steps, "the prison check-out did not take; no admission was attempted");

                        if (!CheckIn(request, plan, pm, steps, out string reason, out _))
                        {
                            string held = string.Format("{0}; CHECKED OUT of {1} but NOT admitted to {2}: held at the release point {3} {4}",
                                reason, plan.Source.Region, plan.DestinationRegion, pm.Location, pm.Map);
                            Console.WriteLine("Confinement: {0}: {1}", pm, held);
                            return Partial(plan, steps, held);
                        }
                        return Done(plan, steps, reason);
                    }

                case ConfinementOperation.Rejail:
                    {   // already in a jail: the runner's Staff jail profile again on the destination facet (new cell, record,
                        //  comment), with no check-out first, exactly as [jail's DoJailing always did
                        PlayerMobile pm = (PlayerMobile)m;
                        if (!CheckIn(request, plan, pm, steps, out string why, out _))
                            return Partial(plan, steps, why);
                        return Done(plan, steps, why);
                    }

                case ConfinementOperation.ExtendSentence:
                    {   // already in this prison: the runner's Extension profile in place (sentence, chat ban, pets, logs, comment)
                        PlayerMobile pm = (PlayerMobile)m;
                        AdmissionResult er = PrisonAdmission.Admit(new AdmissionContext(pm, Reporter(request), pm.Map)
                        {
                            Comment = string.IsNullOrEmpty(request.Comment) ? "None" : request.Comment,
                            PrisonSentence = request.PrisonSentence,
                            StaffCommand = request.Actor is PlayerMobile actor && actor.AccessLevel > AccessLevel.Player,
                        }, PrisonProfile.Extension);
                        steps.Add(er.ToString());
                        if (er.Denied)
                            return new ConfinementResult { Outcome = ConfinementOutcome.Denied, Plan = plan, Steps = steps, Denials = er.Denials };
                        if (!er.Completed)
                            return Unresolved(plan, steps, pm, er);
                        return Done(plan, steps, "sentence extended");
                    }
            }

            return Partial(plan, steps, "unknown operation");
        }

        /// <summary>Put them back where they were (after an admission that did not take).</summary>
        private static void Return(PlayerMobile pm, Point3D origin, Map originMap, List<string> steps)
        {
            if (pm.Location != origin || pm.Map != originMap)
            {
                pm.MoveToWorld(origin, originMap);
                steps.Add(string.Format("returned to {0} {1}", origin, originMap));
            }
        }

        /// <summary>
        /// Staff release from prison: the staff boot's bookkeeping (Utility.Boot, without [boot ex's account history),
        /// the move, then the parole check-out. Murder counts are untouched.
        /// </summary>
        private static void StaffPrisonCheckOut(ConfinementRequest request, PlayerMobile pm, Point3D to, Map map, List<string> steps)
        {
            // [boot ex (explicit, staff release from prison only; Preflight refuses it otherwise): login IPs, game login,
            //  machines and long-term criminal counts, before the sentence counts, in [boot ex's order. Murder counts untouched.
            if (request.ClearAccountHistory && pm.Account is Account acct)
            {
                PrisonOperations.ClearAccountHistory(pm, acct);
                steps.Add("account history cleared ([boot ex)");
            }
            if (request.OverrideSentence)
            {
                PrisonOperations.ClearPrisonCriminalCounts(pm);
                steps.Add("prison criminal counts cleared");
            }
            PrisonOperations.ClearMinimumSentence(pm);
            PrisonOperations.ClearAccountImprisoned(pm);
            ConfinementBookkeeping.ClearInfraction(pm);
            steps.Add("sentence and infraction cleared");

            pm.MoveToWorld(to, map);
            steps.Add(string.Format("moved to {0} {1}", to, map));

            ConfinementOpResult r = PrisonOperations.ParoleCheckOut(pm);
            steps.Add("parole check-out (" + r.Steps.Count + " steps)");

            ConfinementBookkeeping.ReleaseMortality(pm, hideOnMortalis: false);
        }

        /// <summary>Staff release from jail: the jail boot's bookkeeping (Utility.Boot), with the move in the same place.</summary>
        private static void JailCheckOut(PlayerMobile pm, Point3D? to, Map? map, List<string> steps)
        {
            JailOperations.RemoveRecord(pm);
            steps.Add("jail record removed");
            if (to is Point3D loc && map != null)
            {
                pm.MoveToWorld(loc, map);
                steps.Add(string.Format("moved to {0} {1}", loc, map));
            }
            JailOperations.ClearAccountJailed(pm);
            ConfinementBookkeeping.ClearInfraction(pm);
            ConfinementBookkeeping.ReleaseMortality(pm, hideOnMortalis: false);
            steps.Add("jail flags cleared");
        }

        /// <summary>
        /// The admission through the shared admission runner (JailAdmission Staff / PrisonAdmission Admission) on the
        /// destination facet, then the final placement. True when it completed (why = a note); false with why = what
        /// failed. unresolved: a prison admission that changed things before failing; it is reported, never undone, and the
        /// caller must not put them back (a refusal or a jail admission that did not take is not unresolved).
        /// </summary>
        private static bool CheckIn(ConfinementRequest request, ConfinementPlan plan, PlayerMobile pm, List<string> steps, out string why,
            out bool unresolved)
        {
            unresolved = false;
            try
            {
                return CheckInCore(request, plan, pm, steps, out why, ref unresolved);
            }
            catch (Exception ex)
            {   // the runner catches inside its steps; this is the coordinator's own code around it
                LogHelper.LogException(ex);
                why = "the admission threw: " + ex.Message;
                return false;
            }
        }

        private static bool CheckInCore(ConfinementRequest request, ConfinementPlan plan, PlayerMobile pm, List<string> steps, out string why,
            ref bool unresolved)
        {
            Map destMap = plan.DestinationMap!;
            Point3D? there = plan.DestinationLocation;
            why = "";

            if (plan.DestinationFacility == Facility.Jail)
            {   // the runner's Staff jail profile (as Jail.DoJailing), on the destination facet
                AdmissionResult jr = JailAdmission.Admit(new AdmissionContext(pm, request.Actor!, destMap)
                {
                    Comment = string.IsNullOrEmpty(request.Comment) ? "None" : request.Comment,
                    JailCell = request.JailCell,
                    JailHours = request.JailHours,
                }, JailProfile.Staff);
                steps.Add(jr.ToString());
                if (!jr.Completed)
                {
                    why = jr.Denied ? "the jail admission was refused: " + jr.Reason : "the jail admission did not complete: " + jr.Failure;
                    return false;
                }

                if (there is Point3D loc && (pm.Location != loc || pm.Map != destMap))
                {
                    pm.MoveToWorld(loc, destMap);
                    steps.Add("placed");
                }
                return true;
            }

            // prison: the runner's Admission profile (as PrisonPlayer.GoToPrison), straight onto the destination facet; no
            //  staging. A refusal changes nothing; a failed step is reported as it is (the runner's result), never undone.
            AdmissionResult pr = PrisonAdmission.Admit(new AdmissionContext(pm, Reporter(request), destMap)
            {
                Comment = string.IsNullOrEmpty(request.Comment) ? "None" : request.Comment,
                PrisonSentence = request.PrisonSentence,
                StaffCommand = request.Actor is PlayerMobile actor && actor.AccessLevel > AccessLevel.Player,
            }, PrisonProfile.Admission, requireInmate: true);
            steps.Add(pr.ToString());
            if (pr.Denied)
            {
                why = "the prison admission was refused: " + pr.Reason;
                return false;
            }
            if (!pr.Completed)
            {
                unresolved = true;
                why = "UNRESOLVED: the prison admission did not complete: " + pr.Failure + (pr.NotRun.Count > 0 ? "; not run: " + string.Join(", ", pr.NotRun) : "");
                Console.WriteLine("Confinement: {0}: {1}", pm, why);
                return false;
            }
            if (pm.Location == Prison.WardensDeathTrapLocation)
                why = "sent to the warden's death trap (account farm); not placed";
            else if (there is Point3D at && (pm.Location != at || pm.Map != destMap))
            {
                pm.MoveToWorld(at, destMap);
                steps.Add("placed");
            }
            return true;
        }

        // who the admission names in its logs, messages and account comment: the staff member, as PrisonPlayer did
        private static Mobile Reporter(ConfinementRequest request)
        {
            return request.Actor as PlayerMobile ?? World.GetSystemAcct();
        }

        // the final placement after a completed transfer, if the destination named a spot
        private static ConfinementResult Place(ConfinementPlan plan, PlayerMobile pm, Point3D? there, List<string> steps, string message)
        {
            if (there is Point3D at && (pm.Location != at || pm.Map != plan.DestinationMap))
            {
                pm.MoveToWorld(at, plan.DestinationMap!);
                steps.Add("placed");
            }
            return Done(plan, steps, message);
        }

        private static ConfinementResult Unresolved(ConfinementPlan plan, List<string> steps, PlayerMobile pm, AdmissionResult r)
        {
            string why = "UNRESOLVED: " + r.Profile + " did not complete: " + r.Failure + (r.NotRun.Count > 0 ? "; not run: " + string.Join(", ", r.NotRun) : "");
            Console.WriteLine("Confinement: {0}: {1}", pm, why);
            return Partial(plan, steps, why);
        }

        #endregion Run

        #region Results and log
        private static ConfinementResult Done(ConfinementPlan plan, List<string> steps, string message)
        {
            return new ConfinementResult { Outcome = ConfinementOutcome.Done, Plan = plan, Steps = steps, Message = message };
        }

        private static ConfinementResult Partial(ConfinementPlan plan, List<string> steps, string message)
        {
            return new ConfinementResult { Outcome = ConfinementOutcome.PartialFailure, Plan = plan, Steps = steps, Message = message };
        }

        private static void Log(ConfinementRequest request, ConfinementPlan plan, ConfinementResult result)
        {
            try
            {
                LogHelper logger = new LogHelper("Confinement.log", overwrite: false, sline: true);
                logger.Log(LogType.Mobile, request.Target, string.Format("{0} by {1}: {2} {3} -> {4} [{5}] {6}",
                    plan.Operation, request.Actor?.ToString() ?? "SYSTEM",
                    plan.Source.Region?.ToString() ?? "free", plan.Source.Map,
                    plan.DestinationRegion != null ? plan.DestinationRegion + " " + plan.DestinationMap : (plan.DestinationLocation?.ToString() ?? "release point"),
                    string.Join(", ", result.Steps), result.Outcome + (result.Message.Length > 0 ? ": " + result.Message : "")));
                logger.Finish();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        #endregion Results and log
    }
}
