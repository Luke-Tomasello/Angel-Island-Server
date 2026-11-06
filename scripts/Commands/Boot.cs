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

/* Scripts/Commands/Boot.cs
 * 	CHANGELOG:
 *  10/17/26, Adam (Confinement Stage 4)
 *      [boot of a player in the world who is in a prison or jail goes through the confinement coordinator
 *      (Confinement.Execute): same syntax, access, messages and Boot Command.log lines. [boot ex alone clears account
 *      history (login IPs, game login, machines, long-term criminal counts), and only when the release goes ahead.
 *      A refused parole now clears nothing (before, the parole exit could refuse after the sentence and history were
 *      cleared). The unmute and Tor watch still happen first, as before. Jail boots still release at West Britain Bank
 *      on Felucca. Logged-out players, staff and anyone not confined stay on Utility.Boot, unchanged.
 * 	5/7/23, Adam
 *	    Initial Version
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Engines.ConfinementSystem;
using Server.Mobiles;
using Server.Targeting;
using System;

namespace Server.Commands
{
    public class Boot
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Boot", AccessLevel.GameMaster, new CommandEventHandler(Boot_OnCommand));
        }

        [Usage("Boot [<serial>] [ex]")]
        [Description("Boot the player from Prison or Jail.")]
        public static void Boot_OnCommand(CommandEventArgs e)
        {
            try
            {
                bool clearCriminalCounts = false;
                PlayerMobile? directTarget = null;

                if (e.Arguments != null && e.Arguments.Length > 0)
                {
                    foreach (string token in e.Arguments)
                    {
                        if (token.Equals("ex", StringComparison.OrdinalIgnoreCase))
                        {
                            clearCriminalCounts = true;
                            continue;
                        }

                        // Try parse as hex serial
                        if (Utility.TryParseHexUInt(token, out uint raw))
                        {
                            Serial serial = (Serial)(int)raw;
                            Mobile? m = World.FindMobile(serial);

                            if (m is PlayerMobile pm)
                                directTarget = pm;
                        }
                    }
                }

                // If serial was provided and resolved
                if (directTarget != null)
                {
                    bool success = RunBoot(e.Mobile, directTarget, clearCriminalCounts);

                    if (success)
                        e.Mobile.SendMessage("Done.");

                    return;
                }

                // Otherwise fallback to targeting
                e.Mobile.SendMessage("Target the player to boot.");
                e.Mobile.Target = new BootTarget(clearCriminalCounts);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        private class BootTarget : Target
        {
            private readonly bool m_ClearCriminalCounts;

            public BootTarget(bool clearCriminalCounts)
                : base(-1, false, TargetFlags.None)
            {
                m_ClearCriminalCounts = clearCriminalCounts;
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                PlayerMobile? pm = targeted as PlayerMobile;

                if (pm == null || pm.AccessLevel != AccessLevel.Player)
                {
                    from.SendMessage("Only players can be booted.");
                    return;
                }

                bool success = RunBoot(from, pm, m_ClearCriminalCounts);

                if (!success)
                    return; // Boot already sent appropriate feedback

                from.SendMessage("Done.");
            }
        }

        /// <summary>
        /// [boot / [boot ex. A player in the world who is in a prison or jail is released through the confinement
        /// coordinator (Confinement.Execute); everything else - logged out (Felucca logout spots), staff, not confined,
        /// no account - stays on Utility.Boot, unchanged. Messages and Boot Command.log lines are those of Utility.Boot.
        /// </summary>
        internal static bool RunBoot(Mobile staff, PlayerMobile pm, bool ex)
        {
            ConfinementState state = Confinement.GetState(pm);
            if (!state.InWorld || !state.Subject || state.Facility == Facility.None || pm.Account is not Account acct)
                return Utility.Boot(staff, pm, ex);

            try
            {
                LogHelper logger = new LogHelper("Boot Command.log", false, false, false);

                // as Utility.Boot: these come before the eligibility checks, so a refused boot still does them
                if (pm.MuteMessages)
                {
                    logger.Log(LogType.Text, "restoring their ability to receive system messages");
                    pm.MuteMessages = false;
                }
                if (acct.GetFlag(Account.IPFlags.IsTorExitNode) || acct.InfractionStatus == Account.AccountInfraction.TorExitNode)
                {
                    acct.Watched = true;
                    pm.Watched = true;
                    string reason = $"{pm} came in on a Tor Exit Node.";
                    logger.Log(LogType.Mobile, staff, reason);

                    using (LogHelper wlLogger = new LogHelper("Watchlist.log", overwrite: false, sline: true))
                    {
                        wlLogger.Log(LogType.Mobile, pm);
                        wlLogger.Log(LogType.Text, $"Watch added: {reason}");
                    }
                }

                return state.Facility == Facility.Prison
                    ? BootFromPrison(staff, pm, ex, logger)
                    : BootFromJail(staff, pm, logger);
            }
            catch (Exception ex2)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex2));
                return false;
            }
        }

        // Utility.Boot's prison boot, through the coordinator: the staff boot's sentence clearing (plus account history
        //  for [boot ex), then the parole check-out at the parole exit's release point. A refusal clears nothing.
        private static bool BootFromPrison(Mobile staff, PlayerMobile pm, bool ex, LogHelper logger)
        {
            logger.Log(LogType.Mobile, staff, $"Using boot command from prison on {pm}.");
            logger.Log(LogType.Text, $"{pm} Infraction Status is: {pm.InfractionStatus}.");

            if (!ex)
                staff.SendMessage("Use [boot ex to automatically clear login IPs, game login, and machine list.");

            uint shortCounts = pm.ShortTermCriminalCounts, longCounts = pm.LongTermCriminalCounts;
            DateTime sentence = pm.MinimumSentence;
            bool bootRules = Utility.GetParoleDenials(pm, ex).Count > 0;    // false: any parole refusal is the parole exit's own

            ConfinementResult r = Confinement.Execute(new ConfinementRequest(pm, staff, TransitionAuthority.Staff, TransitionMode.Administrative,
                ConfinementDestination.Free()) { OverrideSentence = ex, ClearAccountHistory = ex });

            if (r.Outcome == ConfinementOutcome.Denied)
            {
                foreach (ConfinementDenial d in r.Denials)
                {
                    string text = !bootRules && IsParoleRule(d.Code) ? "The parole exit refused them: " + d.StaffText : d.StaffText;
                    staff.SendMessage(text);
                    logger.Log(LogType.Text, text);
                }
                staff.SendMessage("Parole denied.");
                logger.Finish();
                return false;
            }

            if (ex)
            {
                logger.Log(LogType.Text, "clearing login IPs, game login, and machine list");
                logger.Log(LogType.Text, $"clearing their LongTermCriminalCounts of {longCounts}");
                logger.Log(LogType.Text, $"clearing their ShortTermCriminalCounts of {shortCounts}");
            }
            logger.Log(LogType.Text, $"clearing their MinimumSentence of {sentence}");

            if (r.Outcome != ConfinementOutcome.Done)
            {
                string text = "The boot did not complete: " + r.Message;
                staff.SendMessage(0x22, text);
                logger.Log(LogType.Text, text);
                staff.SendMessage("Parole denied.");
                logger.Finish();
                return false;
            }

            staff.SendMessage("Paroled.");
            logger.Finish();
            return true;
        }

        // Utility.Boot's jail boot, through the coordinator: released at the new-character spawn point (West Britain Bank,
        //  Felucca), as the jail boot always did
        private static bool BootFromJail(Mobile staff, PlayerMobile pm, LogHelper logger)
        {
            logger.Log(LogType.Mobile, staff, $"Using boot command from jail on {pm}.");

            Server.Misc.CharacterCreation.NewCharacterSpawnLocation(null, pm, out _, out Point3D spawnLoc, out Map spawnMap);
            ConfinementResult r = Confinement.Execute(new ConfinementRequest(pm, staff, TransitionAuthority.Staff, TransitionMode.Administrative,
                ConfinementDestination.At(spawnLoc, spawnMap)));

            if (!r.Succeeded)
            {
                string text = r.Outcome == ConfinementOutcome.Denied ? "They cannot be booted right now: " + r.Reason : "The boot did not complete: " + r.Message;
                staff.SendMessage(r.Outcome == ConfinementOutcome.Denied ? 0x3B2 : 0x22, text);
                logger.Log(LogType.Text, text);
                logger.Finish();
                return false;
            }

            staff.SendMessage("Done.");
            logger.Finish();
            return true;
        }

        private static bool IsParoleRule(ConfinementDenialCode code)
        {
            return code is ConfinementDenialCode.ServerWars or ConfinementDenialCode.CriminalCounts or ConfinementDenialCode.MinimumSentence
                or ConfinementDenialCode.IPLimit or ConfinementDenialCode.TorExitNode or ConfinementDenialCode.MurderCounts or ConfinementDenialCode.Dead;
        }
    }


}