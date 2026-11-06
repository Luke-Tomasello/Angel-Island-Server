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

/* Scripts/Commands/Prison.cs
 * CHANGELOG
 *	10/17/26, Adam
 *		Confinement Stage 2: the account flag, chat ban and sentence go through PrisonOperations /
 *		ConfinementBookkeeping (ConfinementOperations.cs). Same changes, same order.
 *	10/17/26, Adam (Confinement Stage 4)
 *		[prison goes through the confinement coordinator: admission as before (sentence 3, the command's comment, the
 *		target's own facet), and a jailed player is now transferred (jail check-out, no stale jail record). An inmate already
 *		in prison still gets the legacy sentence extension. Staff of strictly lower rank are demoted to Player and imprisoned;
 *		equal or higher rank is refused without any change (before, staff were moved into prison without the inmate flag).
 *	10/17/26, Adam (admission refactor R2)
 *		PrisonPlayer.GoToPrison delegates to the shared admission runner (PrisonAdmission, ConfinementAdmission.cs): the
 *		Extension profile for someone already in a prison, the Admission profile for anyone else, on the facet they stand
 *		on. The old body (and its DeathSentence/AcctNameList) is gone; the constructor no longer changes anything (the
 *		account flag is the admission's first step). Same steps, texts and logs, with the approved corrections: a missing
 *		backpack is refused before anything changes; possessions the bank cannot hold are left at their feet (no archive,
 *		no recycle bin) and the imprisoning staff member is told; a refused or unfinished admission is written to Prison.log
 *		and the console. Admit() returns the runner's result for callers that want it.
 *	10/17/26, Adam (D5)
 *		[prison on someone already in that prison goes through the coordinator too (ExtendSentence, RenewSentence): the
 *		same sentence extension as before, now a coordinator operation logged to Confinement.log.
 *	3/12/10, Adam
 *		Clone from Jail command.
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Engines.ConfinementSystem;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;
namespace Server.Commands
{
    public class Prison
    {
        public Prison()
        {
        }

        public static void Initialize()
        {
            Server.CommandSystem.Register("Prison", AccessLevel.Counselor, new CommandEventHandler(Prison_OnCommand));
        }

        // warden's office
        public static Point3D WardensOfficeLocation { get { return new Point3D(354, 836, 20); } }
        public static Point3D WardensDeathTrapLocation { get { return new Point3D(354, 834, 0); } }

        public static void Prison_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the player to imprison.");
            e.Mobile.Target = new PrisonTarget(WardensOfficeLocation, e.ArgString);
        }

        public static void Usage(Mobile to)
        {
            to.SendMessage("Usage: [Prison [\"Tag Message\"]");
        }

        public class PrisonPlayer
        {
            private Point3D m_Location;
            private string m_Comment;
            private PlayerMobile m_Player;
            private PlayerMobile m_Staff;
            private uint m_Sentence;
            private bool m_StaffCommand;      // sent by a staff member (told the possessions disposition), not the system

            // location: kept for the callers' signature; the prison entrance has always decided where they go
            public PrisonPlayer(PlayerMobile? from, PlayerMobile pm, Point3D location, string comment, uint sentence = 3)
            {
                m_Location = location;
                m_Comment = comment;
                if (m_Comment == null || m_Comment == "")
                    m_Comment = "None";

                m_Player = pm;
                m_Staff = from != null ? from : (PlayerMobile)World.GetSystemAcct();
                m_Sentence = sentence;
                m_StaffCommand = from != null && from.AccessLevel > AccessLevel.Player;
                // no changes here any more: the account flag is the admission's first step (PrisonAdmission)
            }

            /// <summary>Imprison them (or extend the sentence of someone already in a prison). See Admit.</summary>
            public void GoToPrison()
            {
                Admit();
            }

            /// <summary>
            /// The prison admission, through the shared admission runner (ConfinementAdmission.cs): someone already in a prison
            /// gets the sentence extension, anyone else the admission, on the facet they stand on, as GoToPrison always did.
            /// Returns what happened. A refusal changes nothing; a refusal or a failed step is also written to Prison.log and
            /// the console, since the void callers cannot see the result. Never throws (login and character creation call it).
            /// </summary>
            public AdmissionResult Admit()
            {
                PrisonProfile profile = m_Player?.Region != null && m_Player.Region.IsPrisonRules ? PrisonProfile.Extension : PrisonProfile.Admission;
                AdmissionResult result;
                try
                {
                    AdmissionContext ctx = new AdmissionContext(m_Player!, m_Staff, m_Player?.Map!)
                    {
                        Comment = m_Comment,
                        PrisonSentence = m_Sentence,
                        StaffCommand = m_StaffCommand,
                    };
                    result = PrisonAdmission.Admit(ctx, profile);
                }
                catch (Exception ex)
                {   // the runner catches inside its steps; this is the validation or the wrapper itself
                    LogHelper.LogException(ex);
                    result = new AdmissionResult
                    {
                        Profile = "prison " + profile,
                        Steps = new[] { new AdmissionStepOutcome { Name = "admission", Mutates = true, Status = AdmissionStepStatus.Failed,
                            Detail = string.Format("{0}: {1}", ex.GetType().Name, ex.Message), Error = ex } },
                    };
                }

                if (!result.Completed)
                {
                    string text = (result.Denied ? "Prison admission refused, nothing changed: " : "Prison admission NOT completed: ") + result;
                    Console.WriteLine("Prison: {0}: {1}", m_Player, text);
                    try
                    {
                        LogHelper logger = new LogHelper("Prison.log", false, false);
                        if (m_Player != null)
                            logger.Log(LogType.Mobile, m_Player, text);
                        else
                            logger.Log(LogType.Text, text);
                        logger.Finish();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex);
                    }
                }
                return result;
            }
        }

        private class PrisonTarget : Target
        {
            private Point3D m_Location;
            private string m_Comment;

            public PrisonTarget(Point3D location, string comment)
                : base(15, false, TargetFlags.None)
            {
                m_Location = location;
                m_Comment = comment;
                if (m_Comment == null || m_Comment == "")
                    m_Comment = "Sent to prison via [prison command";
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                PlayerMobile? pm = targ as PlayerMobile;
                if (pm == null)
                {
                    from.SendMessage("Only players can be sent to Prison.");
                    return;
                }

                // staff: only of strictly lower rank, demoted to Player first; equal or higher rank is refused, nothing changed
                //  (staff are never in a prison as inmates, so this never stands in the way of a sentence extension)
                ConfinementState state = Confinement.GetState(pm);
                if (!(state.Subject && state.Facility == Facility.Prison) && pm.AccessLevel > AccessLevel.Player && from.AccessLevel <= pm.AccessLevel)
                {
                    from.SendMessage("You cannot imprison staff of equal or higher rank.");
                    return;
                }

                // through the confinement coordinator, on the target's own facet as before: someone already in that prison
                //  has their sentence extended (ExtendSentence: counts, chat ban, pets, logs, comment, as before), a jailed
                //  player is transferred (jail check-out, no stale jail record), anyone else is admitted
                ConfinementResult r = Confinement.Execute(new ConfinementRequest(pm, from, TransitionAuthority.Staff, TransitionMode.Administrative,
                    ConfinementDestination.Prison())
                { Comment = m_Comment, PrisonSentence = 3, DemoteStaffTarget = pm.AccessLevel > AccessLevel.Player, RenewSentence = true });

                if (r.Outcome == ConfinementOutcome.Denied)
                    from.SendMessage("{0} cannot be imprisoned right now: {1}", pm.Name, r.Reason);
                else if (r.Outcome == ConfinementOutcome.PartialFailure)
                    from.SendMessage(0x22, "{0}: the imprisonment did not complete ({1}). See Confinement.log.", pm.Name, r.Message);
            }
        }
    }
}