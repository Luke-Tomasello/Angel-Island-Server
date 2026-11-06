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

/* Scripts/Engines/Confinement/ConfinementAdmission.cs
 * ChangeLog
 *  10/17/26, Adam (admission refactor R1)
 *      Jail and prison admission as explicit, observable steps run by one runner, replacing the workflows owned by
 *      Jail.DoJailing (staff and system), PrisonPlayer.GoToPrison and AIEntrance.OnMoveOver/DoTeleport.
 *          Validate  every expected refusal, read-only, before anything changes (a denied admission changes nothing)
 *          Run       the profile's steps in order; each reports Done, Skipped (with the reason) or Failed. The runner
 *                    catches per step, stops at the first failure and returns what completed, what was skipped, what
 *                    failed and what was not run. Nothing is retried, repeated or undone.
 *      Profiles reproduce the legacy workflows step for step (texts, logs, order), with the approved possessions policy
 *      (PrisonOperations.BankPossessions) and the voluntary-entry refusals moved ahead of any change:
 *          prison Admission   GoToPrison (staff [prison, system/login/Tor, character creation; a staff target is moved
 *                             in without the inmate flag or confiscation, as before)
 *          prison Extension   GoToPrison on someone already in a prison (sentence, logs, comment; no move)
 *          prison EntranceSent       AIEntrance with the PrisonCommand context (guards, the murderer prompt)
 *          prison EntranceVoluntary  the world entrance tile
 *          jail Staff / System       the two DoJailing overloads (their differences are kept: the staff jailing sets the
 *                                    account jailed flag, the system jailing bans global chat)
 *      R1: not wired. The legacy methods still run the game; they are the oracle for the parity tests.
 *  10/17/26, Adam (admission refactor R2)
 *      PrisonPlayer.GoToPrison now delegates here (prison Admission / Extension); AIEntrance and DoJailing are not yet wired.
 *  10/17/26, Adam (admission refactor R3)
 *      AIEntrance.OnMoveOver delegates here (EntranceSent / EntranceVoluntary). The prompt is closed when the teleport
 *      starts and the transport (pets in range, robe, flag, move) runs then or, for a tile with a delay, when its timer
 *      fires (RunDeferredTransport): on the facet they are on at that moment, failing visibly if the character is gone or
 *      out of the world. Unfinished entrance admissions are written to Prison.log and the console (ReportNotCompleted).
 *      DoJailing is not yet wired.
 *  10/17/26, Adam (admission refactor R4)
 *      Jail.DoJailing (staff and system, and so JailPlayer.GoToJail) delegates here (jail Staff / System).
 *  10/17/26, Adam (admission refactor R6)
 *      Removed AdmissionContext.InPlaceTransfer: prison -> prison transfers use the Transfer profile (R5).
 */

using Server.Accounting;
using Server.Commands;
using Server.Diagnostics;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Engines.ConfinementSystem
{
    #region Result contracts
    public enum AdmissionStepStatus
    {
        Done,       // the step did its work (Detail says what)
        Skipped,    // the step did not apply (Detail says why); nothing changed
        Failed,     // the step could not do its work or threw (Detail says why); the runner stopped here
    }

    /// <summary>What one step reports.</summary>
    public readonly struct StepReport
    {
        public AdmissionStepStatus Status { get; }
        public string Detail { get; }
        private StepReport(AdmissionStepStatus status, string detail) { Status = status; Detail = detail ?? ""; }
        public static StepReport Done(string detail = "") { return new StepReport(AdmissionStepStatus.Done, detail); }
        public static StepReport Skipped(string reason) { return new StepReport(AdmissionStepStatus.Skipped, reason); }
        public static StepReport Failed(string reason) { return new StepReport(AdmissionStepStatus.Failed, reason); }
    }

    /// <summary>
    /// One admission step. Mutates: whether it changes game state (false for logs, messages and other reads), so a result
    /// can say whether anything changed before a failure.
    /// </summary>
    public sealed class AdmissionStep
    {
        public string Name { get; }
        public bool Mutates { get; }
        public Func<AdmissionContext, StepReport> Run { get; }
        public AdmissionStep(string name, bool mutates, Func<AdmissionContext, StepReport> run)
        {
            Name = name;
            Mutates = mutates;
            Run = run;
        }
    }

    public sealed class AdmissionStepOutcome
    {
        public string Name { get; init; } = "";
        public bool Mutates { get; init; }
        public AdmissionStepStatus Status { get; init; }
        public string Detail { get; init; } = "";
        public Exception? Error { get; init; }

        public override string ToString()
        {
            string s = Status == AdmissionStepStatus.Done ? Name : string.Format("{0} {1}", Name, Status.ToString().ToLowerInvariant());
            return Detail.Length > 0 ? string.Format("{0} ({1})", s, Detail) : s;
        }
    }

    public sealed class AdmissionResult
    {
        public string Profile { get; init; } = "";
        public IReadOnlyList<ConfinementDenial> Denials { get; init; } = Array.Empty<ConfinementDenial>();
        public IReadOnlyList<AdmissionStepOutcome> Steps { get; init; } = Array.Empty<AdmissionStepOutcome>();
        public IReadOnlyList<string> NotRun { get; init; } = Array.Empty<string>();

        /// <summary>Refused by validation: nothing ran, nothing changed.</summary>
        public bool Denied => Denials.Count > 0;
        /// <summary>The step that failed (the runner stopped there), if any.</summary>
        public AdmissionStepOutcome? Failure => Steps.FirstOrDefault(s => s.Status == AdmissionStepStatus.Failed);
        /// <summary>Every step ran and none failed.</summary>
        public bool Completed => !Denied && Failure == null;
        /// <summary>Some state-changing step did (or attempted) its work: a failed result is a partial admission, never a clean denial.</summary>
        public bool Changed => Steps.Any(s => s.Mutates && s.Status != AdmissionStepStatus.Skipped);
        public string Reason => string.Join(" ", Denials.Select(d => d.StaffText));
        public PossessionsOutcome? Possessions { get; init; }

        public override string ToString()
        {
            if (Denied)
                return string.Format("{0}: denied: {1}", Profile, Reason);
            string s = string.Format("{0}: {1}: {2}", Profile, Completed ? "completed" : "FAILED", string.Join(", ", Steps));
            if (NotRun.Count > 0)
                s += "; not run: " + string.Join(", ", NotRun);
            return s;
        }
    }

    /// <summary>What an admission works on: its inputs, plus what earlier steps leave for later ones.</summary>
    public sealed class AdmissionContext
    {
        public PlayerMobile Target { get; }
        /// <summary>The staff member, or the system/admin account: named in logs, messages and the account comment.</summary>
        public Mobile Reporter { get; }
        /// <summary>The facet whose facility they are admitted to.</summary>
        public Map Map { get; }

        public string Comment { get; init; } = "None";
        public uint PrisonSentence { get; init; } = 3;
        public int JailCell { get; init; } = 3;
        public int JailHours { get; init; } = 24;
        /// <summary>A staff command: the staff member is also told what became of the possessions.</summary>
        public bool StaffCommand { get; init; }
        // the entrance tile's settings (voluntary entry)
        public bool CheckMurders { get; init; } = true;
        public bool SourceEffect { get; init; }
        public bool DestEffect { get; init; }
        public int SoundID { get; init; }
        public TimeSpan Delay { get; init; } = TimeSpan.Zero;

        // left by steps for later steps
        public string PriorLocation { get; set; } = "";
        public bool Visitor { get; set; }
        public Ticket? KeptTicket { get; set; }
        public List<Ticket> TicketsToDelete { get; set; } = new();
        public PossessionsOutcome? Possessions { get; set; }
        public int Hours { get; set; }

        public AdmissionContext(PlayerMobile target, Mobile reporter, Map map)
        {
            Target = target;
            Reporter = reporter;
            Map = map;
        }
    }
    #endregion Result contracts

    /// <summary>Runs admission steps in order and reports each one. Stops at the first failure; never repeats or undoes a step.</summary>
    public static class AdmissionRunner
    {
        public static AdmissionResult Run(string profile, AdmissionContext ctx, IReadOnlyList<AdmissionStep> steps, IReadOnlyList<ConfinementDenial> denials)
        {
            if (denials.Count > 0)
                return new AdmissionResult { Profile = profile, Denials = denials, NotRun = steps.Select(s => s.Name).ToList() };

            List<AdmissionStepOutcome> outcomes = new();
            List<string> notRun = new();
            for (int i = 0; i < steps.Count; i++)
            {
                AdmissionStep step = steps[i];
                StepReport r;
                Exception? error = null;
                try
                {
                    r = step.Run(ctx);
                }
                catch (Exception ex)
                {   // visible: the exception log, the result, and whoever reports the result
                    LogHelper.LogException(ex);
                    error = ex;
                    r = StepReport.Failed(string.Format("{0}: {1}", ex.GetType().Name, ex.Message));
                }

                outcomes.Add(new AdmissionStepOutcome { Name = step.Name, Mutates = step.Mutates, Status = r.Status, Detail = r.Detail, Error = error });
                if (r.Status == AdmissionStepStatus.Failed)
                {
                    notRun.AddRange(steps.Skip(i + 1).Select(s => s.Name));
                    break;
                }
            }

            return new AdmissionResult { Profile = profile, Steps = outcomes, NotRun = notRun, Possessions = ctx.Possessions };
        }
    }

    public enum PrisonProfile
    {
        Admission,          // PrisonPlayer.GoToPrison on someone not in a prison
        Extension,          // PrisonPlayer.GoToPrison on someone already in a prison
        EntranceSent,       // AIEntrance, PrisonCommand context (guards, the murderer prompt)
        EntranceVoluntary,  // AIEntrance, the world tile
        Transfer,           // the coordinator's prison -> prison transfer: an inmate moved to another prison, sentence and bookkeeping kept
    }

    public static class PrisonAdmission
    {
        /// <summary>Where the prison entrance puts everyone (AIEntrance.DoTeleport).</summary>
        public static readonly Point3D EntrancePoint = new Point3D(355, 836, 20);

        /// <summary>Validate, then run the profile. A denial changes nothing.</summary>
        public static AdmissionResult Admit(AdmissionContext ctx, PrisonProfile profile, bool requireInmate = false)
        {
            return AdmissionRunner.Run("prison " + profile, ctx, Steps(profile, ctx), Validate(ctx, profile, requireInmate));
        }

        #region Validation (read-only)
        /// <summary>
        /// Every expected refusal, before anything changes. requireInmate: the admission must end with the inmate flag
        /// (the coordinator); the legacy callers allow staff targets, who are moved in without it. The voluntary profile
        /// also sends the entrance's own refusal messages (messages only; nothing else changes).
        /// </summary>
        public static List<ConfinementDenial> Validate(AdmissionContext ctx, PrisonProfile profile, bool requireInmate)
        {
            List<ConfinementDenial> denials = new();
            PlayerMobile pm = ctx.Target;

            if (pm == null || pm.Deleted)
            {
                denials.Add(new ConfinementDenial(ConfinementDenialCode.Deleted, "That character no longer exists."));
                return denials;
            }
            if (ctx.Map == null || ctx.Map == Map.Internal)
                denials.Add(new ConfinementDenial(ConfinementDenialCode.NoDestination, "There is no facet to admit them on."));
            if (requireInmate && !ConfinementBookkeeping.Applies(pm))
                denials.Add(new ConfinementDenial(ConfinementDenialCode.NotAPlayer,
                    "That is not a player character at player level; the admission would not make them an inmate."));

            bool inPrison = pm.Region != null && pm.Region.IsPrisonRules;
            switch (profile)
            {
                case PrisonProfile.Extension:
                    if (!inPrison)
                        denials.Add(new ConfinementDenial(ConfinementDenialCode.OperationNotAllowed, "They are not in a prison; there is no sentence to extend."));
                    break;

                case PrisonProfile.Transfer:
                    // an inmate, in a prison, going to a prison on the given facet
                    if (!inPrison || !pm.PrisonInmate || !ConfinementBookkeeping.Applies(pm))
                        denials.Add(new ConfinementDenial(ConfinementDenialCode.OperationNotAllowed, "Only an inmate in a prison can be transferred to another prison."));
                    if (ctx.Map != null && ctx.Map != Map.Internal && Confinement.FacilityOf(Region.Find(EntrancePoint, ctx.Map)) != Facility.Prison)
                        denials.Add(new ConfinementDenial(ConfinementDenialCode.DestinationNotFound,
                            string.Format("The prison entrance point {0} is not inside a prison on {1}.", EntrancePoint, ctx.Map)));
                    break;

                case PrisonProfile.Admission:
                case PrisonProfile.EntranceSent:
                case PrisonProfile.EntranceVoluntary:
                    // the entrance needs a backpack and turns away anyone already in a prison (silently, as before)
                    if (pm.Backpack == null)
                        denials.Add(new ConfinementDenial(ConfinementDenialCode.NoBackpackOrBank, "They have no backpack."));
                    if (inPrison && profile != PrisonProfile.Admission)
                        denials.Add(new ConfinementDenial(ConfinementDenialCode.OperationNotAllowed, "They are already in a prison."));
                    if (inPrison && profile == PrisonProfile.Admission)
                        denials.Add(new ConfinementDenial(ConfinementDenialCode.OperationNotAllowed, "They are already in a prison: that is a sentence extension."));
                    break;
            }

            if (profile == PrisonProfile.EntranceVoluntary && denials.Count == 0)
                ValidateVoluntary(ctx, denials);

            return denials;
        }

        // The entrance tile's own rules, in its order, with its messages, before anything changes: a quest ticket for
        //  a non-murderer, room in the bank (the bank's real slot rule), five short-term murders for a non-visitor.
        private static void ValidateVoluntary(AdmissionContext ctx, List<ConfinementDenial> denials)
        {
            PlayerMobile pm = ctx.Target;

            ctx.Visitor = PrisonOperations.VoluntaryTicketPlan(pm, out Ticket? keep, out List<Ticket> delete);
            ctx.KeptTicket = keep;
            ctx.TicketsToDelete = delete;

            if (!pm.IsMurderer && !ctx.Visitor)
            {
                const string text = "You must have a valid ticket to attend this quest, you cannot pass.";
                pm.SendMessage(text);
                denials.Add(new ConfinementDenial(ConfinementDenialCode.NeedsMurdersOrTicket, "They need a valid quest ticket to enter.", text));
                return;
            }

            if (!PrisonOperations.VoluntaryBankFits(pm, delete, out int count))
            {
                pm.SendMessage("Your bank box cannot store the {0} items you are trying to bring in.", count);
                pm.SendMessage("Come back when you have emptied your bank box a bit or carrying fewer items.");
                denials.Add(new ConfinementDenial(ConfinementDenialCode.BankFull, "Their bank box cannot hold their belongings.",
                    "Your bank box cannot store the items you are trying to bring in."));
                return;
            }

            if (pm.AccessLevel == AccessLevel.Player && !ctx.Visitor && !pm.PrisonInmate && ctx.CheckMurders && pm.ShortTermMurders < 5)
            {
                const string text = "You must have at least five short term murder counts to use this.";
                pm.SendMessage(text);
                denials.Add(new ConfinementDenial(ConfinementDenialCode.NeedsMurdersOrTicket, "They need at least five short term murder counts to enter.", text));
            }
        }
        #endregion Validation

        #region Profiles
        public static IReadOnlyList<AdmissionStep> Steps(PrisonProfile profile, AdmissionContext ctx)
        {
            switch (profile)
            {
                case PrisonProfile.Admission:
                    return Before().Concat(EntranceSent(ctx)).Concat(After()).ToList();
                case PrisonProfile.Extension:
                    return Before().Concat(After()).ToList();
                case PrisonProfile.EntranceSent:
                    return EntranceSent(ctx).ToList();
                case PrisonProfile.Transfer:
                    return TransferSteps().ToList();
                default:
                    return EntranceVoluntary(ctx).ToList();
            }
        }

        // prison -> prison: no parole, no release, no new sentence, confiscation, robe, kit or account comment. Prison.log records
        //  the transfer; then the entrance's transport without the robe (pets in range, the inmate flag they already have, the
        //  move into the destination prison)
        private static IEnumerable<AdmissionStep> TransferSteps()
        {
            yield return new AdmissionStep("prison log", false, c =>
            {
                LogHelper logger = new LogHelper("Prison.log", false, false);
                logger.Log(LogType.Mobile, c.Target, string.Format("Transferred to the prison on {0} by {1}: {2}", c.Map, c.Reporter?.Name ?? "SYSTEM", c.Comment));
                logger.Finish();
                return StepReport.Done();
            });
            // the entrance's transport without its robe: a transfer issues nothing and takes nothing (no robe, kit or
            //  confiscation); the inmate keeps their equipment and possessions exactly
            foreach (AdmissionStep s in Transport().Where(s => s.Name != "robe"))
                yield return s;
        }

        // GoToPrison up to the entrance: account flag (its constructor), location, pets, chat ban, log, sentence
        private static IEnumerable<AdmissionStep> Before()
        {
            yield return new AdmissionStep("account flag", true, c =>
            {
                if (c.Target.Account is not Account)
                    return StepReport.Skipped("no account");
                if (!Core.RuleSets.MortalisStyleRules())
                    return StepReport.Skipped("not used by this rule set");
                PrisonOperations.MarkAccountImprisoned(c.Target);
                return StepReport.Done("imprisoned");
            });
            yield return new AdmissionStep("location", false, c =>
            {
                c.PriorLocation = Utility.GetSextantLocation(c.Target);
                return StepReport.Done(c.PriorLocation);
            });
            yield return new AdmissionStep("pets", true, c => StepReport.Done(Utility.StablePets(c.Target) + " stabled"));
            yield return new AdmissionStep("chat ban", true, c =>
            {
                if (c.Target.Account is not Account)
                    return StepReport.Skipped("no account");
                ConfinementBookkeeping.ImposeChatBan(c.Target);
                return StepReport.Done();
            });
            yield return new AdmissionStep("prison log", false, c =>
            {
                LogHelper logger = new LogHelper("Prison.log", false, false);
                logger.Log(LogType.Mobile, c.Target, string.Format("Comment: {0}", c.Comment));
                c.Target.LogMortalPrisonInfo(logger);
                logger.Finish();
                return StepReport.Done();
            });
            yield return new AdmissionStep("sentence", true, c =>
            {
                uint s = c.Target.ShortTermCriminalCounts, l = c.Target.LongTermCriminalCounts;
                PrisonOperations.AddPrisonSentence(c.Target, c.PrisonSentence);
                return StepReport.Done(string.Format("short-term {0} -> {1}, long-term {2} -> {3}", s, c.Target.ShortTermCriminalCounts, l, c.Target.LongTermCriminalCounts));
            });
        }

        // AIEntrance with the PrisonCommand context
        private static IEnumerable<AdmissionStep> EntranceSent(AdmissionContext ctx)
        {
            yield return HeldItem();
            yield return new AdmissionStep("quest tickets", true, c =>
            {   // sent to prison: never a visitor, and no ticket may become a way out
                List<Ticket> list = AIEntrance.GetAnyQuestTicket(c.Target);
                c.Visitor = false;
                if (list.Count == 0)
                    return StepReport.Skipped("none");
                foreach (Ticket t in list)
                    if (t != null && !t.Deleted)
                        t.Delete();
                return StepReport.Done(list.Count + " deleted");
            });
            yield return Possessions(voluntary: false);
            yield return Kit();
            yield return ClosePrompt();
            foreach (AdmissionStep s in Transport())
                yield return s;
            yield return ResurrectGump();
        }

        // AIEntrance, the world tile: Validate has already applied its refusals and planned the tickets
        private static IEnumerable<AdmissionStep> EntranceVoluntary(AdmissionContext ctx)
        {
            yield return HeldItem();
            yield return new AdmissionStep("quest tickets", true, c =>
            {
                if (c.Target.IsMurderer && c.TicketsToDelete.Count > 0)
                    c.Target.SendMessage("Your ticket is of no value here, murderer!");
                foreach (Ticket t in c.TicketsToDelete)
                    if (!t.Deleted)
                        t.Delete();
                // the kept ticket ends up loose in the backpack, as the entrance has always left it
                if (c.KeptTicket != null && !c.KeptTicket.Deleted && c.KeptTicket.Parent != c.Target.Backpack)
                {
                    if (c.KeptTicket.Parent is Container from)
                        from.RemoveItem(c.KeptTicket);
                    c.Target.AddToBackpack(c.KeptTicket);
                }
                if (c.TicketsToDelete.Count == 0 && c.KeptTicket == null)
                    return StepReport.Skipped("none");
                return StepReport.Done(string.Format("{0} deleted{1}", c.TicketsToDelete.Count, c.KeptTicket != null ? ", one kept" : ""));
            });
            yield return new AdmissionStep("welcome", false, c =>
            {
                if (c.Target.AccessLevel != AccessLevel.Player || !c.Visitor)
                    return StepReport.Skipped(c.Visitor ? "staff" : "not a visitor");
                c.Target.SendMessage("You may enter.");
                return StepReport.Done();
            });
            yield return Possessions(voluntary: true);
            yield return Kit();
            yield return ClosePrompt();
            if (ctx.Delay == TimeSpan.Zero)
                foreach (AdmissionStep s in Transport())
                    yield return s;
            else
                yield return DeferredTransport();
            yield return ResurrectGump();
        }

        // GoToPrison after the entrance: death trap, messages, log, command log, account comment
        private static IEnumerable<AdmissionStep> After()
        {
            yield return new AdmissionStep("death trap", true, c =>
            {
                int accounts = AccountFarmSize(c.Target);
                if (accounts < 8)
                    return StepReport.Skipped(accounts + " accounts on this machine/IP");
                c.Target.MoveToWorld(Prison.WardensDeathTrapLocation, c.Target.Map);
                return StepReport.Done(accounts + " accounts on this machine/IP");
            });
            yield return new AdmissionStep("notify", false, c =>
            {
                PlayerMobile pm = c.Target;
                int sentence = 0;
                if (pm.ShortTermCriminalCounts * 4 > 0)
                    sentence = (int)pm.ShortTermCriminalCounts * 4; // decay time in prison is 4 hours per count
                else if ((int)(pm.MinimumSentence - DateTime.UtcNow).TotalHours > 0)
                    sentence = (int)(pm.MinimumSentence - DateTime.UtcNow).TotalHours;
                c.Hours = sentence;
                if (sentence > 0)
                {
                    pm.SendMessage("You have been imprisoned for {0} hours.", sentence);
                    c.Reporter.SendMessage("{0} has been imprisoned for {1} hours.", pm.Name, sentence);
                }
                else
                {
                    pm.SendMessage("You have been imprisoned for an unspecified amount of time.");
                    c.Reporter.SendMessage("{0} has been imprisoned for an unspecified amount of time.", pm.Name);
                }
                return StepReport.Done(sentence + " hours");
            });
            yield return new AdmissionStep("prison log", false, c =>
            {
                LogHelper logger = new LogHelper("Prison.log", false, false);
                logger.Log(LogType.Mobile, c.Target, string.Format("{0}:{1}:{2}:{3}",
                    c.Reporter.Name, c.Reporter.Location, c.Comment, c.Hours == 0 ? "unspecified amount of time" : c.Hours.ToString()));
                c.Target.LogMortalPrisonInfo(logger);
                logger.Finish();
                return StepReport.Done();
            });
            yield return new AdmissionStep("command log", false, c =>
            {
                if (c.Target.Account is not Account acct)
                    return StepReport.Skipped("no account");
                CommandLogging.WriteLine(c.Reporter, "{0} imprisoned {1}(Username: {2}) for {4} hours with reason: {3}.",
                    c.Reporter.Name, c.Target.Name, acct.Username, c.Comment, c.Hours == 0 ? "unspecified" : c.Hours.ToString());
                return StepReport.Done();
            });
            yield return new AdmissionStep("account comment", true, c =>
            {
                if (c.Target.Account is not Account)
                    return StepReport.Skipped("no account");
                Utility.AddIncarcerationComment(c.Reporter, c.Target, c.Comment, TimeSpan.FromHours(c.Hours), c.PriorLocation, destination: "Prison");
                return StepReport.Done();
            });
        }
        #endregion Profiles

        #region Entrance steps
        private static AdmissionStep HeldItem()
        {
            return new AdmissionStep("held item", true, c =>
            {
                Item? held = c.Target.Dragging;
                Utility.DropHolding(c.Target);
                return held == null ? StepReport.Skipped("nothing held") : StepReport.Done("into the backpack");
            });
        }

        // the approved possessions policy (D3 involuntary, D4 voluntary); a staff target keeps everything, as before
        private static AdmissionStep Possessions(bool voluntary)
        {
            return new AdmissionStep("possessions", true, c =>
            {
                PlayerMobile pm = c.Target;
                if (pm.AccessLevel != AccessLevel.Player)
                    return StepReport.Skipped("staff: possessions are not taken");

                PossessionsOutcome o = PrisonOperations.BankPossessions(pm, voluntary);
                c.Possessions = o;
                if (o.Disposition != PossessionsDisposition.Nothing)
                {
                    LogHelper logger = new LogHelper("Prison.log", false, false);
                    logger.Log(LogType.Mobile, pm, "Possessions: " + o);
                    logger.Finish();
                }
                if (c.StaffCommand)
                    TellStaff(c.Reporter, pm, o);

                if (o.Disposition == PossessionsDisposition.Refused)    // voluntary only, after the capacity check said it fits
                    return StepReport.Failed("the bank refused possessions after the capacity check: " + o);
                return StepReport.Done(o.ToString());
            });
        }

        private static void TellStaff(Mobile staff, PlayerMobile pm, PossessionsOutcome o)
        {
            switch (o.Disposition)
            {
                case PossessionsDisposition.Banked:
                    staff.SendMessage("{0}: possessions banked ({1} items).", pm.Name, o.BankedTop);
                    break;
                case PossessionsDisposition.PartlyBanked:
                    staff.SendMessage("{0}: {1} items banked; the bank could not hold {2} items, left at their feet ({3}).", pm.Name, o.BankedTop, o.RefusedTop, o.Where);
                    break;
                case PossessionsDisposition.Dropped:
                    staff.SendMessage("{0}: bank full; {1} items left at their feet ({2}).", pm.Name, o.RefusedTop, o.Where);
                    break;
            }
        }

        private static AdmissionStep Kit()
        {
            return new AdmissionStep("inmate kit", true, c =>
            {
                if (c.Target.AccessLevel != AccessLevel.Player)
                    return StepReport.Skipped("staff");
                PrisonOperations.IssueInmateKit(c.Target);
                return StepReport.Done(c.Target.Alive ? "robe, stinger, spellbook" : "stinger, spellbook");
            });
        }

        // the entrance's start of the teleport (immediately, even when the teleport itself is delayed)
        private static AdmissionStep ClosePrompt()
        {
            return new AdmissionStep("close prompt", false, c => { c.Target.CloseGumps(typeof(AITeleportQueryGump)); return StepReport.Done(); });
        }

        // the entrance's teleport: pets in range, robe, the inmate (or visitor) flag, then the move
        private static IEnumerable<AdmissionStep> Transport()
        {
            yield return new AdmissionStep("pets in range", true, c => StepReport.Done(Utility.StablePets(c.Target, 13) + " stabled"));
            yield return new AdmissionStep("robe", true, c =>
            {
                if (PrisonOperations.HasDeathRobe(c.Target))
                    return StepReport.Skipped("has one");
                DeathRobe robe = new DeathRobe();
                robe.Hue = 0x2b;
                if (!c.Target.AddToBackpack(robe))
                {
                    robe.Delete();
                    return StepReport.Skipped("no room in the backpack");
                }
                return StepReport.Done("into the backpack");
            });
            yield return new AdmissionStep("inmate flag", true, c =>
            {   // before the move: the prison turns away any player without it (StaticRegion.OnMobileAdd)
                if (!ConfinementBookkeeping.Applies(c.Target))
                    return StepReport.Skipped("not a player character at player level");
                PrisonOperations.SetInmateFlag(c.Target, visitor: c.Visitor);
                return StepReport.Done(c.Visitor ? "visitor" : "inmate");
            });
            yield return new AdmissionStep("move", true, c =>
            {
                PlayerMobile pm = c.Target;
                if (c.SourceEffect)
                    Effects.SendLocationEffect(pm.Location, pm.Map, 0x3728, 10, 10);
                pm.MoveToWorld(EntrancePoint, c.Map);
                if (c.DestEffect)
                    Effects.SendLocationEffect(pm.Location, pm.Map, 0x3728, 10, 10);
                if (c.SoundID > 0)
                    Effects.PlaySound(pm.Location, pm.Map, c.SoundID);

                if (pm.Map != c.Map || pm.Region == null || !pm.Region.IsPrisonRules)
                    return StepReport.Failed(string.Format("not inside a prison after the move: at {0} {1} ({2})", pm.Location, pm.Map, pm.Region?.Name ?? "no region"));
                return StepReport.Done(string.Format("{0} {1}", pm.Location, pm.Map));
            });
        }

        // the world tile with a delay: the teleport happens later, on a timer, as AIEntrance has always done it
        private static AdmissionStep DeferredTransport()
        {
            return new AdmissionStep("teleport", false, c =>
            {
                Timer.DelayCall(c.Delay, () => RunDeferredTransport(c));
                return StepReport.Done("scheduled in " + c.Delay);
            });
        }

        /// <summary>
        /// The delayed teleport when its timer fires: on the facet they are on then (as AIEntrance always did), if they are
        /// still a character in the world. Its result is returned and, unless it completed, written to Prison.log and the
        /// console (the admission that scheduled it has already returned).
        /// </summary>
        public static AdmissionResult RunDeferredTransport(AdmissionContext scheduled)
        {
            PlayerMobile pm = scheduled.Target;
            AdmissionContext c = new AdmissionContext(pm, scheduled.Reporter, pm?.Map!)
            {
                SourceEffect = scheduled.SourceEffect,
                DestEffect = scheduled.DestEffect,
                SoundID = scheduled.SoundID,
                Visitor = scheduled.Visitor,
            };
            List<AdmissionStep> steps = new()
            {
                new AdmissionStep("still here", false, x =>
                {
                    if (x.Target == null || x.Target.Deleted)
                        return StepReport.Failed("the character was deleted before the delayed teleport");
                    if (x.Map == null || x.Map == Map.Internal)
                        return StepReport.Failed("the character was not in the world when the delayed teleport fired");
                    return StepReport.Done(string.Format("{0} {1}", x.Target.Location, x.Map));
                }),
            };
            steps.AddRange(Transport());

            AdmissionResult r = AdmissionRunner.Run("prison entrance teleport (delayed)", c, steps, Array.Empty<ConfinementDenial>());
            if (!r.Completed)
                ReportNotCompleted(pm, r);
            return r;
        }

        /// <summary>An entrance admission or delayed teleport that did not complete: to the console and Prison.log.</summary>
        public static void ReportNotCompleted(PlayerMobile? pm, AdmissionResult r)
        {
            string text = "Prison entrance NOT completed: " + r;
            Console.WriteLine("Prison: {0}: {1}", pm, text);
            try
            {
                LogHelper logger = new LogHelper("Prison.log", false, false);
                if (pm != null && !pm.Deleted)
                    logger.Log(LogType.Mobile, pm, text);
                else
                    logger.Log(LogType.Text, string.Format("{0}: {1}", pm?.ToString() ?? "(no character)", text));
                logger.Finish();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        private static AdmissionStep ResurrectGump()
        {
            return new AdmissionStep("resurrect prompt", false, c =>
            {
                if (c.Target.Alive || c.Target.NetState == null)
                    return StepReport.Skipped(c.Target.Alive ? "alive" : "offline");
                c.Target.CloseGump(typeof(ResurrectGump));
                c.Target.SendGump(new ResurrectGump(c.Target, ResurrectMessage.Healer));
                return StepReport.Done();
            });
        }
        #endregion Entrance steps

        /// <summary>How many accounts share their machine or IP (GoToPrison's death trap sends 8 or more to the trap).</summary>
        public static int AccountFarmSize(PlayerMobile pm)
        {
            Account? acct = pm.Account as Account;
            List<string> names = new();
            foreach (Account account in Server.Misc.AccountHardwareLimiter.GetAccountsByMachine(acct))
                names.Add(account.Username);

            NetState ns = pm.NetState;
            if (ns != null)
                foreach (Account account in Server.Misc.AccountTotalIPLimiter.GetAccountsByIP(acct, ns.Address))
                    if (!names.Contains(account.Username))
                        names.Add(account.Username);

            return names.Count;
        }
    }

    public enum JailProfile
    {
        Staff,      // Jail.DoJailing(jailer, ...): account jailed flag, staff told, command log
        System,     // Jail.DoJailing(toJail, ...) / JailPlayer.GoToJail: global chat ban, reporter SYSTEM, Felucca
    }

    public static class JailAdmission
    {
        public static AdmissionResult Admit(AdmissionContext ctx, JailProfile profile)
        {
            return AdmissionRunner.Run("jail " + profile, ctx, Steps(profile).ToList(), Validate(ctx));
        }

        /// <summary>Every expected refusal, read-only: a player character, a real cell inside a jail on the facet.</summary>
        public static List<ConfinementDenial> Validate(AdmissionContext ctx)
        {
            List<ConfinementDenial> denials = new();
            if (ctx.Target == null || ctx.Target.Deleted)
            {
                denials.Add(new ConfinementDenial(ConfinementDenialCode.Deleted, "That character no longer exists."));
                return denials;
            }
            if (ctx.Map == null || ctx.Map == Map.Internal)
                denials.Add(new ConfinementDenial(ConfinementDenialCode.NoDestination, "There is no facet to jail them on."));
            else
            {
                Point3D cell = Jail.GetCell(ctx.JailCell);
                Region? r = cell == Point3D.Zero ? null : Region.Find(cell, ctx.Map);
                if (r == null || !r.IsJailRules)
                    denials.Add(new ConfinementDenial(ConfinementDenialCode.DestinationNotFound,
                        string.Format("Jail cell {0} is not inside a jail on {1}.", ctx.JailCell, ctx.Map)));
            }
            return denials;
        }

        public static IEnumerable<AdmissionStep> Steps(JailProfile profile)
        {
            bool staff = profile == JailProfile.Staff;

            yield return new AdmissionStep("location", false, c =>
            {
                c.PriorLocation = Utility.GetSextantLocation(c.Target);
                return StepReport.Done(c.PriorLocation);
            });
            if (staff)
                yield return new AdmissionStep("account flag", true, c =>
                {
                    if (c.Target.Account is not Account)
                        return StepReport.Skipped("no account");
                    if (!Core.RuleSets.MortalisStyleRules())
                        return StepReport.Skipped("not used by this rule set");
                    JailOperations.MarkAccountJailed(c.Target);
                    return StepReport.Done("jailed");
                });
            yield return new AdmissionStep("pets", true, c =>
            {
                int count = Utility.StablePets(c.Target);
                if (staff && count != 0)
                    c.Reporter.SendMessage("{0} pets have been stabled", count);
                return StepReport.Done(count + " stabled");
            });
            if (!staff)
                yield return new AdmissionStep("chat ban", true, c =>
                {
                    if (c.Target.Account is not Account)
                        return StepReport.Skipped("no account");
                    ConfinementBookkeeping.ImposeChatBan(c.Target);
                    return StepReport.Done();
                });
            yield return new AdmissionStep("move", true, c =>
            {   // Jail.MoveToJail: a logged-out character is placed by its logout location
                PlayerMobile pm = c.Target;
                Point3D cell = Jail.GetCell(c.JailCell);
                pm.MoveToWorld(cell, c.Map);
                if (pm.NetState == null)
                {
                    pm.LogoutLocation = cell;
                    pm.LogoutMap = c.Map;
                    pm.Map = Map.Internal;
                    return StepReport.Done(string.Format("logged out: logout location {0} {1}", cell, c.Map));
                }
                if (pm.Map != c.Map || pm.Region == null || !pm.Region.IsJailRules)
                    return StepReport.Failed(string.Format("not inside a jail after the move: at {0} {1}", pm.Location, pm.Map));
                return StepReport.Done(string.Format("cell {0}: {1} {2}", c.JailCell, cell, c.Map));
            });
            yield return new AdmissionStep("jail record", true, c =>
            {
                JailOperations.RecordSentence(c.Target, c.JailHours);
                return StepReport.Done(c.JailHours + " hours");
            });
            yield return new AdmissionStep("notify", false, c =>
            {
                c.Target.SendMessage("You have been jailed for {0} hours.", c.JailHours);
                if (staff)
                    c.Reporter.SendMessage("{0} has been jailed for {1} hours.", c.Target.Name, c.JailHours);
                return StepReport.Done();
            });
            yield return new AdmissionStep("jail log", false, c =>
            {
                LogHelper logger = new LogHelper("jail.log", overwrite: false, sline: true);
                logger.Log(LogType.Mobile, c.Target, string.Format("{0}:{1}:{2}:{3}", staff ? c.Reporter.Name : "SYSTEM", c.JailCell, c.Comment, c.JailHours));
                logger.Finish();
                return StepReport.Done();
            });
            if (staff)
                yield return new AdmissionStep("command log", false, c =>
                {
                    Account? acct = c.Target.Account as Account;
                    CommandLogging.WriteLine(c.Reporter, "{0} jailed {1}(Username: {2}) into cell {3} for {4} hours with reason: {5}.",
                        c.Reporter.Name, c.Target.Name, acct == null ? "-" : acct.Username, c.JailCell, c.JailHours, c.Comment);
                    return StepReport.Done();
                });
            yield return new AdmissionStep("account comment", true, c =>
            {
                if (c.Target.Account is not Account)
                    return StepReport.Skipped("no account");
                Utility.AddIncarcerationComment(staff ? c.Reporter : World.GetSystemAcct(), c.Target, c.Comment,
                    TimeSpan.FromHours(c.JailHours), c.PriorLocation, destination: "Jail");
                return StepReport.Done();
            });
        }
    }
}
