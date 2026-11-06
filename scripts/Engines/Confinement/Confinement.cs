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

/* Scripts/Engines/Confinement/Confinement.cs
 * ChangeLog
 *  10/17/26, Adam
 *      Stage 1 of the confinement architecture: the read-only state and policy layer.
 *      Confinement.GetState() says where a player is confined (jail, prison, or nowhere; online or offline).
 *      Confinement.Plan() classifies a requested move as Relocate, Release, Admit or Transfer, decides who may do it
 *      (staff, the player through a gameplay exit, or the system), lists every reason it would be refused, and
 *      describes the state changes it implies. Planning never moves, clears, deletes, logs or messages anything.
 *      Nothing calls this yet; the existing jail/prison/boot/parole/escape code is unchanged.
 *  10/17/26, Adam (Stage 3)
 *      Confinement is partial; Execute() lives in ConfinementExecute.cs. Requests carry the admission details
 *      (comment, jail cell/hours, prison sentence). A staff transfer out of prison now declares ParoleConsequences:
 *      its prison check-out is the staff boot's parole check-out, as Jail now has always done.
 *      Facet-local incarceration: Jail() defaults to the target's own facet when it has a jail (as Prison() always did),
 *      instead of always Felucca.
 *      Read-only fix: prison admission planning no longer reads Mobile.BankBox, which creates a missing bank box
 *      (found by ConfinementExecTests O3). The bank box is never a refusal reason (the entrance creates it); the
 *      voluntary-entry capacity check runs read-only when there is no bank box yet.
 *  10/17/26, Adam (Stage 4)
 *      DemoteStaffTarget ([jail/[prison on staff): a staff target of strictly lower rank than the staff member is planned
 *      as the free player they will be after demotion (an Admit with the DemoteToPlayer effect); equal or higher rank is
 *      denied ("staff of equal or higher rank").
 *  10/17/26, Adam (admission refactor R1)
 *      Voluntary entry's bank check uses the bank's real slot accounting (PrisonOperations.VoluntaryBankFits: potion
 *      stacks, contraband and deleted quest tickets counted as the entrance banks them) instead of Teleporter.CheckBank's
 *      object count. Still read-only.
 *  10/17/26, Adam (admission refactor R5)
 *      A prison -> prison transfer plans as PrisonTransfer (the inmate moved, sentence and bookkeeping kept), not as a
 *      prison check-out (parole) plus a new check-in.
 *  10/17/26, Adam (D5)
 *      Rejail and ExtendSentence: with RenewSentence (the [jail/[prison commands), someone already in a jail (any facet) is
 *      re-jailed and someone already in that prison has their sentence extended, instead of a no-op move or a transfer.
 *  10/18/26, Adam (Stage 5)
 *      ConfinementVetoes: a small, generic pre-operation veto on moving someone out of confinement (or between
 *      confinements), registered by other systems and asked by Execute's Preflight (ConfinementDenialCode.Vetoed) and by
 *      the gameplay exits (RefuseAutomated: the character told, GameMaster+ staff told, Confinement.log). Confinement
 *      itself contains no rule about who may leave where; with nothing registered nothing is refused.
 */

using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Engines.ConfinementSystem
{
    /// <summary>The kind of confinement a region imposes.</summary>
    public enum Facility
    {
        None,       // not confined
        Jail,       // IsJailRules
        Prison,     // IsPrisonRules (Angel Island)
    }

    /// <summary>What a move means for confinement. Decided by comparing the source and destination facility regions.</summary>
    public enum ConfinementOperation
    {
        None,       // no confinement change: unconfined to unconfined, or the target is not subject to confinement (staff)
        Relocate,   // within the same facility region: plain move, confinement state untouched
        Release,    // confined to free: complete checkout
        Admit,      // free to confined: complete check-in
        Transfer,   // one facility region to another (jail <-> prison, Felucca prison <-> Trammel prison): checkout then check-in
        Rejail,         // [jail on someone already in a jail (any facet): jailed again (new cell, record, comment), no check-out
        ExtendSentence, // [prison on someone already in that prison: the sentence extended in place
    }

    /// <summary>Who is making the move happen.</summary>
    public enum TransitionAuthority
    {
        System,     // automatic enforcement (cron inmate management, Tor imprisonment, Test Center clone penalties)
        Player,     // the player, through a gameplay exit or entrance
        Staff,      // a staff member, through an administrative tool
    }

    /// <summary>The path the move comes through. Selects the policy that applies.</summary>
    public enum TransitionMode
    {
        Administrative, // Bring Here, Jail now/Prison now, [jail, [prison, [boot              (Staff)
        Parole,         // AIParoleExit                                                     (Player)
        Escape,         // AIEscapeExit                                                     (Player)
        JailExit,       // JailExitSungate                                                  (Player)
        VoluntaryEntry, // walking into AIEntrance (murderers, quest visitors)              (Player)
        Enforcement,    // cron inmate management, Tor imprisonment, clone penalties        (System)
    }

    /// <summary>The state changes a plan implies. Stage 2 implements them; Stage 1 only reports them.</summary>
    [Flags]
    public enum ConfinementEffects
    {
        None = 0x000,
        Move = 0x001,                   // the target is moved
        // Stage 2 operations behind each effect are in ConfinementOperations.cs
        CheckOutJail = 0x002,           // JailOperations: record removed, account jailed flag cleared (the jail exit gate also lifts the chat ban; a staff boot does not)
        CheckOutPrison = 0x004,         // PrisonOperations.ParoleCheckOut / EscapeCheckOut: inmate/visitor flags, account flag, pack, robe, prison pets
        CheckInJail = 0x008,            // JailOperations.RecordSentence (+ account flag on Mortalis-style rules); pets stabled, account comment
        CheckInPrison = 0x010,          // PrisonOperations: AddPrisonSentence, BankPossessions, IssueInmateKit, SetInmateFlag; account comment
        ClearSentence = 0x020,          // confinement bookkeeping only: PrisonOperations.ClearPrisonCriminalCounts + ClearMinimumSentence, or the jail record
        ParoleConsequences = 0x040,     // parole (ParoleCheckOut): pack emptied, sentence/infraction/mute/machine flag cleared, Outcast guild left
        EscapeConsequences = 0x080,     // escape (EscapeCheckOut): rares and smuggled goods kept, souvenir, undressed, leaderboard credit
        ReduceMurderCounts = 0x100,     // escape only (PrisonOperations.ReduceEscapeMurders): short-term murders above 5 are reduced
        PrisonVisitor = 0x200,          // admitted as a quest visitor rather than an inmate
        ClearAccountHistory = 0x400,    // only on explicit request ([boot ex, PrisonOperations.ClearAccountHistory): login IPs, game login, machines, long-term criminal counts
        DemoteToPlayer = 0x800,         // [jail/[prison on staff of lower rank: the character's access level becomes Player, then a normal admission
        PrisonTransfer = 0x1000,        // prison -> prison (another facet): the inmate is moved, sentence and bookkeeping kept (no parole, no new admission)
        RenewSentence = 0x2000,         // Rejail / ExtendSentence: the jail admission again, or the prison sentence extension, with no check-out
    }

    public enum ConfinementDenialCode
    {
        // the target
        Deleted,
        NotAPlayer,
        NoAccount,
        NoLocation,
        // authority
        NoActor,
        InsufficientAccess,
        ModeNotAllowed,
        OperationNotAllowed,
        // destination
        NoDestination,
        DestinationNotFound,
        NoBackpackOrBank,
        BankFull,
        OfflineNotSupported,
        NeedsMurdersOrTicket,
        // parole (AIParoleExit.ParoleDenial)
        ServerWars,
        CriminalCounts,
        MinimumSentence,
        IPLimit,
        TorExitNode,
        MurderCounts,
        Dead,
        // jail
        JailSentence,
        // a veto registered by another system (ConfinementVetoes)
        Vetoed,
    }

    public sealed class ConfinementDenial
    {
        public ConfinementDenialCode Code { get; }
        public string StaffText { get; }
        public string PlayerText { get; }
        public ConfinementDenial(ConfinementDenialCode code, string staffText, string? playerText = null)
        {
            Code = code;
            StaffText = staffText;
            PlayerText = playerText ?? staffText;
        }
        public override string ToString() { return string.Format("{0}: {1}", Code, StaffText); }
    }

    /// <summary>
    /// Pre-operation vetoes on moving someone out of confinement (or between confinements), registered by other systems.
    /// Confinement does not know why a veto refuses; it only asks, before anything changes: the staff operations in
    /// Execute's Preflight (a Vetoed denial: Release, Transfer, Rejail), and the gameplay exits (AIParoleExit, AIEscapeExit,
    /// JailExitSungate) and the prison's parole kick through RefuseAutomated. With no veto registered nothing is refused.
    /// </summary>
    public static class ConfinementVetoes
    {
        /// <summary>
        /// The refusal text (for staff and the logs), or null to allow. fromMap: the facet they are confined on; toMap: where
        /// the operation puts them; leaving: they leave confinement (a release or a gameplay exit), false for a move between
        /// confinements (transfer, re-jail); actor: the staff member, null for a gameplay exit.
        /// </summary>
        public delegate string? Veto(PlayerMobile target, Map fromMap, Map toMap, bool leaving, Mobile? actor);

        private static readonly List<Veto> m_Vetoes = new();

        public static void Register(Veto veto)
        {
            if (veto != null && !m_Vetoes.Contains(veto))
                m_Vetoes.Add(veto);
        }

        /// <summary>The first refusal, or null. A veto that throws refuses (fail closed).</summary>
        public static string? Check(PlayerMobile target, Map fromMap, Map toMap, bool leaving, Mobile? actor)
        {
            foreach (Veto veto in m_Vetoes)
            {
                string? why;
                try
                {
                    why = veto(target, fromMap, toMap, leaving, actor);
                }
                catch (Exception ex)
                {
                    Server.Diagnostics.LogHelper.LogException(ex);
                    why = "the release check failed: " + ex.Message;
                }
                if (why != null)
                    return why;
            }
            return null;
        }

        /// <summary>
        /// A gameplay exit asks before it moves or checks out anyone (leaving, no actor). On a refusal (true): unless report
        /// is false, the character is told, GameMaster+ staff get a broadcast and Confinement.log a line. The exit must then
        /// neither move them nor check them out.
        /// </summary>
        public static bool RefuseAutomated(Mobile m, Map toMap, string exit, bool report = true)
        {
            if (m is not PlayerMobile pm || pm.Map == null || toMap == null)
                return false;

            string? why = Check(pm, pm.Map, toMap, leaving: true, actor: null);
            if (why == null)
                return false;

            if (report)
            {
                pm.SendMessage("You cannot leave this way: {0}", why);
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.GameMaster, 0x482, "Staff message from SYSTEM:");
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.GameMaster, 0x482, string.Format("{0}: {1} refused: {2}", pm, exit, why));
                try
                {
                    Server.Diagnostics.LogHelper logger = new Server.Diagnostics.LogHelper("Confinement.log", false, true);
                    logger.Log(Server.Diagnostics.LogType.Mobile, pm, string.Format("{0} refused (veto): {1}", exit, why));
                    logger.Finish();
                }
                catch (Exception ex)
                {
                    Server.Diagnostics.LogHelper.LogException(ex);
                }
            }
            return true;
        }
    }

    /// <summary>Where a mobile is confined right now. A snapshot; holds no references that planning could change.</summary>
    public sealed class ConfinementState
    {
        public Mobile Mobile { get; init; } = null!;
        public bool Online { get; init; }               // has a NetState
        public bool InWorld { get; init; }              // false: logged out (Map.Internal); Location/Map are the logout location
        public Point3D Location { get; init; }
        public Map? Map { get; init; }
        public Region? Region { get; init; }            // the facility region (identity distinguishes Felucca from Trammel prison)
        public Facility Facility { get; init; }
        public bool Subject { get; init; }              // a player character (Mobile.Player) at AccessLevel.Player: confinement applies
        public bool PrisonInmate { get; init; }
        public bool PrisonVisitor { get; init; }
        public bool JailRecord { get; init; }           // JailSystem has an unexpired sentence
        public DateTime JailRelease { get; init; }
        public bool AccountImprisoned { get; init; }
        public bool AccountJailed { get; init; }
        public IReadOnlyList<string> Anomalies { get; init; } = Array.Empty<string>();

        public bool IsConfined => Facility != Facility.None;

        public override string ToString()
        {
            return string.Format("{0} {1} {2}@{3} region={4} inmate={5} visitor={6} jailRecord={7}{8}",
                Facility, InWorld ? "in world" : "offline", Location, Map, Region?.ToString() ?? "-", PrisonInmate, PrisonVisitor, JailRecord,
                Anomalies.Count > 0 ? " anomalies: " + string.Join("; ", Anomalies) : "");
        }
    }

    /// <summary>Where the target should end up.</summary>
    public sealed class ConfinementDestination
    {
        public Facility? RequestedFacility { get; private init; }   // null = take it from Location
        public Point3D? Location { get; private init; }
        public Map? Map { get; private init; }

        /// <summary>A specific spot (e.g. Bring Here: the staff member's location). Its region decides the facility.</summary>
        public static ConfinementDestination At(Point3D location, Map map) { return new ConfinementDestination { Location = location, Map = map }; }
        /// <summary>Released, to the facility's normal release point.</summary>
        public static ConfinementDestination Free() { return new ConfinementDestination { RequestedFacility = Facility.None }; }
        /// <summary>The jail on a facet; null = the target's own facet if it has a jail, else Felucca (each facet manages its own jail).</summary>
        public static ConfinementDestination Jail(Map? facet = null) { return new ConfinementDestination { RequestedFacility = Facility.Jail, Map = facet }; }
        /// <summary>Angel Island prison on a facet; null = the target's own facet if it has a prison, else Felucca.</summary>
        public static ConfinementDestination Prison(Map? facet = null) { return new ConfinementDestination { RequestedFacility = Facility.Prison, Map = facet }; }

        public override string ToString()
        {
            return Location != null ? string.Format("{0}@{1}", Location, Map) : string.Format("{0}{1}", RequestedFacility, Map != null ? "@" + Map : "");
        }
    }

    public sealed class ConfinementRequest
    {
        public Mobile Target { get; }
        public Mobile? Actor { get; }                       // the staff member, or the player themselves
        public TransitionAuthority Authority { get; }
        public TransitionMode Mode { get; }
        public ConfinementDestination Destination { get; }
        /// <summary>Staff release: clear the rest of the sentence. False = the sentence must already be served ([boot without "ex").</summary>
        public bool OverrideSentence { get; init; } = true;
        /// <summary>Explicit [boot ex only: also clear login IPs, game login, machines and long-term criminal counts.</summary>
        public bool ClearAccountHistory { get; init; }
        /// <summary>Admission details (Execute): the account comment and log reason, and the jail cell/hours or prison sentence.
        /// Defaults are those of the User Information gump's Jail now / Prison now.</summary>
        public string Comment { get; init; } = "Sent by staff";
        public int JailCell { get; init; } = 3;
        public int JailHours { get; init; } = 24;
        public uint PrisonSentence { get; init; } = 3;
        /// <summary>[jail/[prison: a staff target of strictly lower rank than the staff member is demoted to Player (the
        /// character only, not the account) and admitted normally. Never restored on release.</summary>
        public bool DemoteStaffTarget { get; init; }
        /// <summary>[jail/[prison (staff): someone already in a jail is re-jailed (Rejail), someone already in that prison has
        /// their sentence extended (ExtendSentence), instead of a no-op move. Off for every other caller.</summary>
        public bool RenewSentence { get; init; }

        public ConfinementRequest(Mobile target, Mobile? actor, TransitionAuthority authority, TransitionMode mode, ConfinementDestination destination)
        {
            Target = target;
            Actor = actor;
            Authority = authority;
            Mode = mode;
            Destination = destination;
        }

        /// <summary>A staff action (Bring Here, Jail now, Prison now, [jail, [prison, [boot).</summary>
        public static ConfinementRequest Staff(Mobile staff, Mobile target, ConfinementDestination destination)
        {
            return new ConfinementRequest(target, staff, TransitionAuthority.Staff, TransitionMode.Administrative, destination);
        }
        /// <summary>The player through a gameplay exit or entrance.</summary>
        public static ConfinementRequest Player(Mobile player, TransitionMode mode, ConfinementDestination destination)
        {
            return new ConfinementRequest(player, player, TransitionAuthority.Player, mode, destination);
        }
        /// <summary>Automatic enforcement.</summary>
        public static ConfinementRequest Enforcement(Mobile target, ConfinementDestination destination)
        {
            return new ConfinementRequest(target, null, TransitionAuthority.System, TransitionMode.Enforcement, destination);
        }
    }

    public sealed class ConfinementPlan
    {
        public ConfinementRequest Request { get; init; } = null!;
        public ConfinementState Source { get; init; } = null!;
        public Facility DestinationFacility { get; init; }
        public Region? DestinationRegion { get; init; }
        public Map? DestinationMap { get; init; }
        public Point3D? DestinationLocation { get; init; }
        public ConfinementOperation Operation { get; init; }
        public ConfinementEffects Effects { get; init; }
        public IReadOnlyList<ConfinementDenial> Denials { get; init; } = Array.Empty<ConfinementDenial>();

        public bool Allowed => Denials.Count == 0;
        /// <summary>All denial reasons, for staff.</summary>
        public string Reason => string.Join(" ", Denials.Select(d => d.StaffText));
        public bool Has(ConfinementEffects effect) => (Effects & effect) == effect;
        public bool DeniedFor(ConfinementDenialCode code) => Denials.Any(d => d.Code == code);

        public override string ToString()
        {
            return string.Format("{0} {1}/{2} {3} -> {4}{5} [{6}]{7}",
                Operation, Request.Authority, Request.Mode, Source.Facility, DestinationFacility,
                DestinationRegion != null ? "(" + DestinationRegion + "@" + DestinationMap + ")" : "",
                Effects, Allowed ? " allowed" : " DENIED: " + string.Join("; ", Denials));
        }
    }

    /// <summary>
    /// The confinement policy: one place that decides what a jail/prison move is, who may make it, and why it would be refused.
    /// Stage 1: read only. Every method here is free of side effects.
    /// </summary>
    public static partial class Confinement     // Execute: ConfinementExecute.cs
    {
        #region State
        public static Facility FacilityOf(Region? region)
        {
            if (region == null)
                return Facility.None;
            if (region.IsPrisonRules)
                return Facility.Prison;
            if (region.IsJailRules)
                return Facility.Jail;
            return Facility.None;
        }

        /// <summary>
        /// The facility region at a spot, or null if the spot is not in a jail or prison.
        /// Region.Find returns the most specific region; region parents are never set in this codebase,
        /// so the region itself is the facility's identity (each facet has its own Angel Island and Jail region).
        /// </summary>
        public static Region? FacilityRegionAt(Point3D location, Map? map)
        {
            if (map == null || map == Map.Internal)
                return null;
            Region region = Region.Find(location, map);
            return FacilityOf(region) != Facility.None ? region : null;
        }

        /// <summary>The (first) region of a facility on a facet, e.g. Angel Island on Trammel.</summary>
        public static Region? FindFacilityRegion(Facility facility, Map? map)
        {
            if (facility == Facility.None || map == null || map == Map.Internal)
                return null;
            return Region.Regions.FirstOrDefault(r => r != null && r.Map == map && FacilityOf(r) == facility);
        }

        public static ConfinementState GetState(Mobile m)
        {
            PlayerMobile? pm = m as PlayerMobile;
            Account? acct = m.Account as Account;

            // logged out players sit on the internal map; their logout location says where they are confined
            bool inWorld = m.Map != null && m.Map != Map.Internal;
            Point3D location = inWorld ? m.Location : m.LogoutLocation;
            Map? map = inWorld ? m.Map : m.LogoutMap;

            Region? region = FacilityRegionAt(location, map);
            Facility facility = FacilityOf(region);

            bool jailRecord = JailSystem.TryGetReleaseTime(m, out DateTime release) && release > DateTime.UtcNow;
            bool inmate = pm != null && pm.PrisonInmate;
            bool visitor = pm != null && pm.PrisonVisitor;
            // a player character, as the jail/prison code decides it (m.Player && AccessLevel.Player: AIParoleExit, AIEntrance, [jail)
            bool subject = pm != null && m.Player && m.AccessLevel == AccessLevel.Player;

            List<string> anomalies = new();
            if (subject)
            {
                if (inmate && facility != Facility.Prison)
                    anomalies.Add("inmate flag set outside prison");
                if (facility == Facility.Prison && !inmate && !visitor)
                    anomalies.Add("in prison without the inmate or visitor flag");
                if (jailRecord && facility != Facility.Jail)
                    anomalies.Add("jail sentence recorded outside jail");
            }

            return new ConfinementState
            {
                Mobile = m,
                Online = m.NetState != null,
                InWorld = inWorld,
                Location = location,
                Map = map,
                Region = region,
                Facility = facility,
                Subject = subject,
                PrisonInmate = inmate,
                PrisonVisitor = visitor,
                JailRecord = jailRecord,
                JailRelease = jailRecord ? release : DateTime.MinValue,
                AccountImprisoned = acct != null && acct.IsImprisoned,
                AccountJailed = acct != null && acct.IsJailed,
                Anomalies = anomalies,
            };
        }
        #endregion State

        #region Plan
        public static ConfinementPlan Plan(ConfinementRequest request)
        {
            Mobile target = request.Target;
            ConfinementState source = GetState(target);
            List<ConfinementDenial> denials = new();

            // [jail/[prison on a staff character: staff are never confined, so plan them as the free player they will be
            //  once demoted (at the same spot); authority (strictly higher rank) is checked below like any staff move
            bool demote = request.DemoteStaffTarget && request.Authority == TransitionAuthority.Staff
                && target is PlayerMobile && target.AccessLevel > AccessLevel.Player;
            if (demote)
                source = AsDemoted(source);

            // where to
            ResolveDestination(request.Destination, source, out Facility destFacility, out Region? destRegion, out Map? destMap, denials);

            // what kind of move it is
            ConfinementOperation op;
            if (!source.Subject)
                op = ConfinementOperation.None;                     // staff and non-players are never confined
            else if (source.Region == null && destRegion == null)
                op = ConfinementOperation.None;
            else if (source.Region != null && source.Region == destRegion)
                op = ConfinementOperation.Relocate;
            else if (source.Region != null && destRegion == null)
                op = ConfinementOperation.Release;
            else if (source.Region == null)
                op = ConfinementOperation.Admit;
            else
                op = ConfinementOperation.Transfer;

            // [jail on someone already in a jail (any facet: the command's jail, as DoJailing always did), [prison on someone
            //  already in that prison: renew the sentence, as the commands always have, rather than move or transfer them
            if (request.RenewSentence && source.Subject && request.Destination.Location == null)
            {
                if (source.Facility == Facility.Jail && destFacility == Facility.Jail)
                    op = ConfinementOperation.Rejail;
                else if (source.Facility == Facility.Prison && destFacility == Facility.Prison && source.Region == destRegion)
                    op = ConfinementOperation.ExtendSentence;
            }

            // asking for a facility (rather than a spot) only makes sense for someone who can be confined
            if (!source.Subject && request.Destination.RequestedFacility is Facility f && f != Facility.None)
                denials.Add(Deny(ConfinementDenialCode.NotAPlayer, "Only players can be sent to jail or prison."));

            ConfinementEffects effects = op == ConfinementOperation.None || op == ConfinementOperation.Relocate
                ? (request.Destination.Location != null ? ConfinementEffects.Move : ConfinementEffects.None)
                : ConfinementEffects.Move;

            if (op == ConfinementOperation.Rejail || op == ConfinementOperation.ExtendSentence)
            {   // staff only (the [jail/[prison commands); no check-out or check-in policy applies
                CheckTarget(target, source, denials);
                CheckAuthority(request, source, op, destFacility, denials);
                if (request.Authority != TransitionAuthority.Staff)
                    denials.Add(Deny(ConfinementDenialCode.OperationNotAllowed, "Only staff can re-jail or extend a prison sentence."));
                effects = (op == ConfinementOperation.Rejail ? ConfinementEffects.Move | ConfinementEffects.CheckInJail : ConfinementEffects.None)
                    | ConfinementEffects.RenewSentence;
            }
            else if (op != ConfinementOperation.None && op != ConfinementOperation.Relocate)
            {
                CheckTarget(target, source, denials);
                CheckAuthority(request, source, op, destFacility, denials);
                effects |= CheckPolicy(request, source, op, destFacility, denials);
            }
            else if (op == ConfinementOperation.Relocate || op == ConfinementOperation.None)
                CheckRelocate(request, source, op, denials);

            if (demote)
            {
                if (op == ConfinementOperation.Admit)
                    effects |= ConfinementEffects.DemoteToPlayer;
                else
                    denials.Add(Deny(ConfinementDenialCode.OperationNotAllowed, "A staff character can only be demoted to be jailed or imprisoned."));
            }

            return new ConfinementPlan
            {
                Request = request,
                Source = source,
                DestinationFacility = destFacility,
                DestinationRegion = destRegion,
                DestinationMap = destMap,
                DestinationLocation = request.Destination.Location,
                Operation = op,
                Effects = effects,
                Denials = denials,
            };
        }

        // a staff character planned as the free player they would be after demotion: same spot, not confined
        private static ConfinementState AsDemoted(ConfinementState s)
        {
            return new ConfinementState
            {
                Mobile = s.Mobile, Online = s.Online, InWorld = s.InWorld, Location = s.Location, Map = s.Map,
                Region = null, Facility = Facility.None, Subject = true,
                PrisonInmate = s.PrisonInmate, PrisonVisitor = s.PrisonVisitor, JailRecord = s.JailRecord, JailRelease = s.JailRelease,
                AccountImprisoned = s.AccountImprisoned, AccountJailed = s.AccountJailed,
                Anomalies = new[] { "staff character, planned as a player after demotion" },
            };
        }

        private static void ResolveDestination(ConfinementDestination dest, ConfinementState source,
            out Facility facility, out Region? region, out Map? map, List<ConfinementDenial> denials)
        {
            if (dest.Location is Point3D loc)
            {
                map = dest.Map;
                if (map == null || map == Map.Internal)
                    denials.Add(Deny(ConfinementDenialCode.NoDestination, "That destination is not in the world."));
                region = FacilityRegionAt(loc, map);
                facility = FacilityOf(region);
                return;
            }

            facility = dest.RequestedFacility ?? Facility.None;
            switch (facility)
            {
                case Facility.Jail:
                    // facet-local: the target's own facet if it has a jail (as for prison), else Felucca
                    map = dest.Map ?? (FindFacilityRegion(Facility.Jail, source.Map) != null ? source.Map : Map.Felucca);
                    region = source.Facility == Facility.Jail && source.Map == map ? source.Region : FindFacilityRegion(Facility.Jail, map);
                    break;
                case Facility.Prison:
                    // the target's own facet if it has a prison (as AIEntrance does), else Felucca
                    map = dest.Map ?? (FindFacilityRegion(Facility.Prison, source.Map) != null ? source.Map : Map.Felucca);
                    region = source.Facility == Facility.Prison && source.Map == map ? source.Region : FindFacilityRegion(Facility.Prison, map);
                    break;
                default:
                    map = null;
                    region = null;
                    return;
            }

            if (region == null)
                denials.Add(Deny(ConfinementDenialCode.DestinationNotFound, string.Format("There is no {0} on {1}.", facility.ToString().ToLower(), map)));
        }

        private static void CheckTarget(Mobile target, ConfinementState source, List<ConfinementDenial> denials)
        {
            if (target.Deleted)
                denials.Add(Deny(ConfinementDenialCode.Deleted, "That character no longer exists."));
            if (target.Account is not Account)
                denials.Add(Deny(ConfinementDenialCode.NoAccount, "That player has no valid account."));
            if (source.Map == null || source.Map == Map.Internal)
                denials.Add(Deny(ConfinementDenialCode.NoLocation, "That player has no known location."));
        }

        private static void CheckAuthority(ConfinementRequest request, ConfinementState source, ConfinementOperation op, Facility destFacility, List<ConfinementDenial> denials)
        {
            switch (request.Authority)
            {
                case TransitionAuthority.Staff:
                    if (request.Mode != TransitionMode.Administrative)
                        denials.Add(Deny(ConfinementDenialCode.ModeNotAllowed, string.Format("Staff moves are administrative, not {0}.", request.Mode)));
                    if (request.Actor == null)
                        denials.Add(Deny(ConfinementDenialCode.NoActor, "A staff move needs a staff member."));
                    else if (request.Actor.AccessLevel < AccessLevel.Counselor || request.Actor.AccessLevel <= request.Target.AccessLevel)
                        denials.Add(Deny(ConfinementDenialCode.InsufficientAccess, request.Target.AccessLevel > AccessLevel.Player
                            ? "You cannot jail or imprison staff of equal or higher rank."
                            : "You do not have the authority to do that."));
                    break;

                case TransitionAuthority.System:
                    if (request.Mode != TransitionMode.Enforcement)
                        denials.Add(Deny(ConfinementDenialCode.ModeNotAllowed, string.Format("System moves are enforcement, not {0}.", request.Mode)));
                    break;

                case TransitionAuthority.Player:
                    {
                        // a player can only move themselves, and only through the exit or entrance that matches the move
                        bool fits = request.Mode switch
                        {
                            TransitionMode.Parole => op == ConfinementOperation.Release && source.Facility == Facility.Prison,
                            TransitionMode.Escape => op == ConfinementOperation.Release && source.Facility == Facility.Prison,
                            TransitionMode.JailExit => op == ConfinementOperation.Release && source.Facility == Facility.Jail,
                            TransitionMode.VoluntaryEntry => op == ConfinementOperation.Admit && destFacility == Facility.Prison,
                            _ => false,
                        };
                        if (request.Actor != null && request.Actor != request.Target)
                            denials.Add(Deny(ConfinementDenialCode.NoActor, "A player can only move themselves."));
                        if (!fits)
                            denials.Add(Deny(ConfinementDenialCode.OperationNotAllowed,
                                string.Format("A player cannot {0} {1} -> {2} through {3}.", op, source.Facility, destFacility, request.Mode)));
                        break;
                    }
            }
        }

        private static void CheckRelocate(ConfinementRequest request, ConfinementState source, ConfinementOperation op, List<ConfinementDenial> denials)
        {   // moving within a facility (or outside the system) changes no confinement state; staff still need authority over the target
            if (request.Authority == TransitionAuthority.Staff && request.Actor != null && source.Subject
                && (request.Actor.AccessLevel < AccessLevel.Counselor || request.Actor.AccessLevel <= request.Target.AccessLevel))
                denials.Add(Deny(ConfinementDenialCode.InsufficientAccess, "You do not have the authority to do that."));
        }

        /// <summary>The eligibility rules and implied state changes for a Release, Admit or Transfer.</summary>
        private static ConfinementEffects CheckPolicy(ConfinementRequest request, ConfinementState source, ConfinementOperation op, Facility destFacility, List<ConfinementDenial> denials)
        {
            ConfinementEffects effects = ConfinementEffects.None;
            PlayerMobile? pm = request.Target as PlayerMobile;
            if (pm == null)
                return effects;

            if (request.ClearAccountHistory)
            {
                if (request.Authority == TransitionAuthority.Staff && op == ConfinementOperation.Release && source.Facility == Facility.Prison)
                    effects |= ConfinementEffects.ClearAccountHistory;
                else
                    denials.Add(Deny(ConfinementDenialCode.OperationNotAllowed, "Account history is only cleared by an explicit staff boot from prison ([boot ex)."));
            }

            // prison -> prison (another facet): a transfer of the inmate, not a release and a new admission (R5)
            if (op == ConfinementOperation.Transfer && source.Facility == Facility.Prison && destFacility == Facility.Prison)
                return effects | ConfinementEffects.PrisonTransfer;

            // checkout from the source
            if (op == ConfinementOperation.Release || op == ConfinementOperation.Transfer)
            {
                if (source.Facility == Facility.Prison)
                {
                    effects |= ConfinementEffects.CheckOutPrison;
                    effects |= CheckLeavePrison(request, pm, op, denials);
                }
                else if (source.Facility == Facility.Jail)
                {
                    effects |= ConfinementEffects.CheckOutJail;
                    effects |= CheckLeaveJail(request, source, op, denials);
                }
            }

            // check-in to the destination
            if (op == ConfinementOperation.Admit || op == ConfinementOperation.Transfer)
            {
                if (destFacility == Facility.Prison)
                {
                    effects |= ConfinementEffects.CheckInPrison;
                    effects |= CheckEnterPrison(request, source, pm, denials);
                }
                else if (destFacility == Facility.Jail)
                    effects |= ConfinementEffects.CheckInJail;
            }

            return effects;
        }

        private static ConfinementEffects CheckLeavePrison(ConfinementRequest request, PlayerMobile pm, ConfinementOperation op, List<ConfinementDenial> denials)
        {
            switch (request.Mode)
            {
                case TransitionMode.Parole:
                    {   // the parole exit's own rules, exactly as AIParoleExit.OnMoveOver applies them
                        AIParoleExit.ParoleDenial d = AIParoleExit.CheckParole(pm, afterBoot: false);
                        if (d != AIParoleExit.ParoleDenial.None)
                            denials.Add(Deny(d, pm));
                        return ConfinementEffects.ParoleConsequences;
                    }

                case TransitionMode.Escape:
                    // AIEscapeExit.OnMoveOver: alive, nothing else (the lighthouse pass is checked earlier, at the cave entrance)
                    if (!pm.Alive)
                        denials.Add(new ConfinementDenial(ConfinementDenialCode.Dead, "They must be alive to escape.", "You must be alive to use this."));
                    return ConfinementEffects.EscapeConsequences | ConfinementEffects.ReduceMurderCounts;

                case TransitionMode.Administrative:
                    if (op == ConfinementOperation.Transfer)
                        // staff transfers need no parole eligibility; the prison check-out is the staff boot's
                        //  (parole check-out), as Jail now always did (Utility.Boot then Jail.DoJailing)
                        return ConfinementEffects.ParoleConsequences | ConfinementEffects.ClearSentence;
                    // staff release: the boot's rules (Utility.Boot), then what the parole exit would still refuse
                    foreach (AIParoleExit.ParoleDenial d in Utility.GetStaffReleaseDenials(pm, clearCCounts: request.OverrideSentence))
                        denials.Add(Deny(d, pm));
                    return ConfinementEffects.ParoleConsequences | (request.OverrideSentence ? ConfinementEffects.ClearSentence : ConfinementEffects.None);

                case TransitionMode.Enforcement:
                    // automatic enforcement (e.g. the cron kick of inmates with nothing left to serve) is not subject to parole rules
                    return ConfinementEffects.ParoleConsequences;

                default:
                    return ConfinementEffects.None;
            }
        }

        private static ConfinementEffects CheckLeaveJail(ConfinementRequest request, ConfinementState source, ConfinementOperation op, List<ConfinementDenial> denials)
        {
            switch (request.Mode)
            {
                case TransitionMode.JailExit:
                    // JailExitSungate.ValidateUse: the sentence must be served
                    if (source.JailRecord)
                    {
                        TimeSpan left = source.JailRelease - DateTime.UtcNow;
                        denials.Add(new ConfinementDenial(ConfinementDenialCode.JailSentence,
                            string.Format("They have {0:0.#} hours of their jail sentence left.", left.TotalHours),
                            "There is still time left in your sentence."));
                    }
                    return ConfinementEffects.None;

                case TransitionMode.Administrative:
                case TransitionMode.Enforcement:
                    return ConfinementEffects.ClearSentence;            // staff and system: no jail eligibility rules

                default:
                    return ConfinementEffects.None;
            }
        }

        private static ConfinementEffects CheckEnterPrison(ConfinementRequest request, ConfinementState source, PlayerMobile pm, List<ConfinementDenial> denials)
        {
            ConfinementEffects effects = ConfinementEffects.None;

            // AIEntrance.OnMoveOver needs both containers. Mobile.BankBox creates a missing bank box on first use (AIEntrance
            //  reads it that way too), so only the backpack can really be missing; the planner must not create the bank box.
            if (pm.Backpack == null)
                denials.Add(Deny(ConfinementDenialCode.NoBackpackOrBank, "They have no backpack."));

            // prison check-in (AIEntrance) places the player in the world; it cannot do that for a logged out player yet
            if (!source.InWorld)
                denials.Add(Deny(ConfinementDenialCode.OfflineNotSupported, "They are offline; prison admission needs them online."));

            if (request.Mode == TransitionMode.VoluntaryEntry)
            {   // AIEntrance.OnMoveOver, voluntary entry: murderers enter as inmates, non-murderers need a quest ticket and enter as visitors
                bool visitor = !pm.IsMurderer && AIEntrance.HasValidQuestTicket(pm);
                if (visitor)
                    effects |= ConfinementEffects.PrisonVisitor;
                else if (!pm.IsMurderer)
                    denials.Add(new ConfinementDenial(ConfinementDenialCode.NeedsMurdersOrTicket, "They need a valid quest ticket to enter.",
                        "You must have a valid ticket to attend this quest, you cannot pass."));
                else if (!pm.PrisonInmate && pm.ShortTermMurders < 5)
                    denials.Add(new ConfinementDenial(ConfinementDenialCode.NeedsMurdersOrTicket, "They need at least five short term murder counts to enter.",
                        "You must have at least five short term murder counts to use this."));

                // room in the bank under the bank's own slot rule (potion stacks included), counting what the entrance would
                //  bank: read-only, a missing bank box counts as the empty one the entrance would create
                PrisonOperations.VoluntaryTicketPlan(pm, out _, out List<Ticket> deleted);
                bool fits = pm.Backpack == null || PrisonOperations.VoluntaryBankFits(pm, deleted, out _);
                if (pm.Backpack != null && !fits)
                    denials.Add(new ConfinementDenial(ConfinementDenialCode.BankFull, "Their bank box cannot hold their belongings.",
                        "Your bank box cannot store the items you are trying to bring in."));
            }
            // staff and system admissions: never refused for a full bank (what it cannot hold is left at their feet)

            return effects;
        }

        private static ConfinementDenial Deny(ConfinementDenialCode code, string text) { return new ConfinementDenial(code, text); }

        private static ConfinementDenial Deny(AIParoleExit.ParoleDenial d, PlayerMobile pm)
        {
            ConfinementDenialCode code = d switch
            {
                AIParoleExit.ParoleDenial.ServerWars => ConfinementDenialCode.ServerWars,
                AIParoleExit.ParoleDenial.CriminalCounts => ConfinementDenialCode.CriminalCounts,
                AIParoleExit.ParoleDenial.MinimumSentence => ConfinementDenialCode.MinimumSentence,
                AIParoleExit.ParoleDenial.IPLimit => ConfinementDenialCode.IPLimit,
                AIParoleExit.ParoleDenial.TorExitNode => ConfinementDenialCode.TorExitNode,
                AIParoleExit.ParoleDenial.MurderCounts => ConfinementDenialCode.MurderCounts,
                _ => ConfinementDenialCode.Dead,
            };
            return new ConfinementDenial(code, AIParoleExit.DenialText(pm, d, toStaff: true), AIParoleExit.DenialText(pm, d, toStaff: false));
        }
        #endregion Plan
    }
}
