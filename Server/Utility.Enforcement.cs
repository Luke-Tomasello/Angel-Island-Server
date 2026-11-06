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


/* Server/Utility.Enforcement.cs
 * ChangeLog:
 *  10/17/26, Adam (CanBoot)
 *      Added CanBoot(): a side-effect-free preflight for Boot(). Boot() and CanBoot() share the target checks
 *      (CheckBootTarget), the prison/jail decision (GetBootKind) and the parole restrictions (GetParoleDenials);
 *      CanBoot also asks the parole exit (AIParoleExit.CheckParole) what it would still refuse after the boot.
 *      Boot() now returns false when the parole exit refuses to move them (it used to report "Paroled.").
 *      GetParoleDenials/GetStaffReleaseDenials return AIParoleExit.ParoleDenial codes (same messages via DenialText)
 *      and are shared with the confinement planner (Scripts/Engines/Confinement/Confinement.cs).
 *      Confinement Stage 2: HandlePrisonBoot/HandleJailBoot make their state changes through PrisonOperations,
 *      JailOperations and ConfinementBookkeeping (ConfinementOperations.cs): same changes, same order, same log lines.
 *      [boot ex is split into ClearAccountHistory (login IPs, game login, machines, long-term criminal counts) and
 *      ClearPrisonCriminalCounts (short-term criminal counts); together they do exactly what ex did.
 *  10/6/2026
 *      Moved from Utility.cs as part of a structural cleanup; no behavior change.
 *      Jail/prison/boot handling, Test Center clone penalties, Mortalis thief extraction and guard rules.
 */

using Server.Accounting;
using Server.Commands;
using Server.Diagnostics;
using Server.Engines.ConfinementSystem;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
namespace Server
{
    public partial class Utility
    {
        #region Jail, Prison, Boot & Test Center
        #region Incarceration Checks
        public static bool IsIncarcerated(Mobile m)
        {
            if (m is PlayerMobile pm && pm.Account is Account acct && pm.Region is Region r)
            {
                if (r.IsJailRules || r.IsPrisonRules)
                    return true;

                return false;
            }

            return false;
        }

        public static bool IsInJail(Mobile m)
        {
            if (m == null)
                return false;

            if (m.Region is not Region region)
                return false;

            if (region.IsPrisonRules || region.IsJailRules)
                return true;

            return false;
        }
        #endregion Incarceration Checks
        #region Test Center Clone Penalties

        /// <summary>
        /// Applies penalties when a clone or player violates Test Center rules.
        /// This method is called from three places:
        ///   1. Cron job detects a clone outside the Test Center.
        ///   2. A clone uses the TestCenterTeleporter in "To" mode (entering).
        ///   3. A non-clone player uses the TestCenterTeleporter in "From" mode (exiting).
        /// 
        /// Behavior:
        ///   - Drops held items
        ///   - Jails the mobile (cell 1 for clones, cell 2 for non-clone players)
        ///   - Makes clones mortal (no resurrection)
        ///   - Deletes backpack and bankbox (prevents economy contamination)
        ///   - Notifies staff and logs the incident
        /// </summary>
        public static void ApplyTestCenterPenalties(Mobile m, string message)
        {
            if (!(m is PlayerMobile pm))
                return;

            // Ignore if already in jail
            if (IsInJail(m))
                return;

            // Drop anything they are currently holding/dragging
            pm.DropHeldItems();

            // Choose jail cell: 
            //   Cell 1 = No exit gate (used for clones)
            //   Cell 2 = No exit gate (used for non-clone players abusing From teleporter)
            int cell = pm.Clone ? 1 : 2;

            // Jail the player/clone
            Jail.JailPlayer jt = new Jail.JailPlayer(pm, cell, message, false);
            jt.GoToJail();

            // Clones are permanently mortal
            if (pm.Clone)
            {
                pm.Mortal = true;

                // Delete inventory to prevent economy contamination
                #region Delete their stuff

                // turn them faction purple. Will alert staff that this 'clone' may not be released into the world
                Utility.WipeLayersHue(pm, 14);
                pm.Hue = 14;

                if (pm.Backpack != null && !pm.Backpack.Deleted)
                    pm.Backpack.Delete();

                if (pm.BankBox != null && !pm.BankBox.Deleted)
                    pm.BankBox.Delete();

                #endregion Delete their stuff
            }

            // Notify staff
            CommandHandlers.BroadcastMessage(AccessLevel.GameMaster, 0x482, "Staff message from SYSTEM:");
            CommandHandlers.BroadcastMessage(AccessLevel.GameMaster, 0x482,
                $"{pm} was sent to jail. ({message})");

            // Log the incident
            LogHelper logger = new LogHelper("CloneManagement.log", false, true);
            logger.Log(LogType.Mobile, pm, message);
            logger.Finish();
        }

        #endregion Test Center Clone Penalties
        #region Mortalis Thief Extraction & Griefers

        public static void MortalisTransportThiefToBucs(PlayerMobile pm)
        {
            if (pm == null || pm.Deleted)
                return;

            PlayMortalisThiefExtractionEffects(pm);

            Map spawnMap = Map.Felucca;

            // Buccaneer's Den arrival zone (loose scatter, fixed fallback)
            Rectangle2D spawnRect = new Rectangle2D(
                new Point2D(2714, 2129),  // 2727 - 13, 2133 - 4
                new Point2D(2740, 2137)); // 2727 + 13, 2133 + 4

            Point3D fallback = new Point3D(2727, 2133, 5);

            Point3D dest = fallback;

            // Try a few random points in the rect; if none are valid, use fallback.
            for (int i = 0; i < 3; i++)
            {
                int x = Utility.RandomMinMax(spawnRect.Start.X, spawnRect.End.X);
                int y = Utility.RandomMinMax(spawnRect.Start.Y, spawnRect.End.Y);

                int z = spawnMap.GetAverageZ(x, y);

                // Use the computed Z at this location; do not use a banded search here.
                // If CanSpawnLandMobile needs a zBand, keep it tight (0) since we already chose Z.
                bool canSpawn = Utility.CanSpawnLandMobile(
                    spawnMap,
                    new Point2D(x, y),
                    z: z,
                    flags: CanFitFlags.requireSurface | CanFitFlags.canSwim);

                if (canSpawn)
                {
                    dest = new Point3D(x, y, z);
                    break;
                }
            }

            // Clear combat state to avoid immediate re-engagement
            pm.Combatant = null;
            pm.Warmode = false;

            // Move to world
            pm.MoveToWorld(dest, spawnMap);

            pm.SendMessage("You have been escorted to Buccaneer's Den.");
        }
        private static void PlayMortalisThiefExtractionEffects(PlayerMobile pm)
        {
            if (pm == null || pm.Deleted)
                return;

            Point3D loc = pm.Location;
            Map map = pm.Map;

            if (map == null || map == Map.Internal)
                return;

            // White column at the ORIGINAL location (world-anchored)
            // itemID 0x3709 is the column animation; hue 0 reads as white/light.
            Effects.SendLocationEffect(loc, map, 0x3709, 30, 10, 0x480, 0);

            // Gendered scream at the ORIGINAL location (so nearby mobiles hear it)
            int screamSound = pm.Female ? 0x15E : 0x15D;
            Effects.PlaySound(loc, map, screamSound);
        }
        public static bool InGrieferHotList(Mobile m)
        {
            if (m == null)
                return false;

            if (m.Account is not Accounting.Account acct)
                return false;

            DateTime now = DateTime.UtcNow;

            if (CharacterCreation.GrieferHotList.TryGetValue(acct.GUID, out DateTime hotUntil))
            {
                if (hotUntil > now)
                    return true;

                // Optional hygiene: expired entry
                CharacterCreation.GrieferHotList.Remove(acct.GUID);
            }

            return false;
        }
        #endregion Mortalis Thief Extraction & Griefers
        #region Jail, Prison & Boot
        public static void AddIncarcerationComment(
            Mobile? reporter,
            Mobile target,
            string reason,
            TimeSpan duration,
            string location,
            string? destination)
        {
            if (target is not PlayerMobile pm)
                return;

            if (pm.Account is not Account acct)
                return;

            // Reporter name: null means SYSTEM.
            string reporterName =
                (reporter == null || string.IsNullOrWhiteSpace(reporter.Name))
                    ? "SYSTEM"
                    : reporter.Name;

            int tagCount = acct.Comments.Count + 1;

            string durationText =
                duration == TimeSpan.Zero
                    ? "unspecified"
                    : ((int)Math.Ceiling(duration.TotalHours)).ToString();

            if (string.IsNullOrWhiteSpace(location))
                location = "unknown";

            if (string.IsNullOrWhiteSpace(destination))
                destination = "unknown";

            if (string.IsNullOrWhiteSpace(reason))
                reason = "unspecified";

            acct.Comments.Add(
                new AccountComment(
                    reporterName,
                    string.Format(
                        "{0}\nTag count: {1}\nTagged for {2} hours\nLocation: {3}\nDestination: {4}\nReason: {5}",
                        DateTime.UtcNow,
                        tagCount,
                        durationText,
                        location,
                        destination,
                        reason)));
        }


        public static bool IsPlayerJailed(Mobile m)
        {
            if (m == null)
                return false;

            Region reg = m.Region;

            if (reg == null)
                return false;

            return reg.IsJailRules;
        }

        public static bool IsPlayerImprisoned(Mobile m)
        {
            if (m == null)
                return false;

            Region reg = m.Region;

            if (reg == null)
                return false;

            return reg.IsPrisonRules;
        }

        public static bool Boot(Mobile m, bool wipeBreadcrumbs = false)
        {
            return Boot(null, m, wipeBreadcrumbs);
        }

        private enum BootKind { None, Prison, Jail, OfflineJail, OfflinePrison }

        // who can be booted at all (shared by Boot and CanBoot)
        private static bool CheckBootTarget(Mobile m, out PlayerMobile pm, out Account acct, out Region reg, out string reason)
        {
            pm = null!; acct = null!; reg = null!;

            if (m is not PlayerMobile p || p.AccessLevel != AccessLevel.Player)
            {
                reason = "Only players can be booted.";
                return false;
            }
            if (p.Account is not Account a)
            {
                reason = "That player has no valid account.";
                return false;
            }
            if (p.Region is not Region r)
            {
                reason = "Error: No region information.";
                return false;
            }

            pm = p; acct = a; reg = r;
            reason = string.Empty;
            return true;
        }

        // where they are being booted from (shared by Boot and CanBoot)
        private static BootKind GetBootKind(PlayerMobile pm, Region reg)
        {
            if (reg.IsPrisonRules)
                return BootKind.Prison;
            if (reg.IsJailRules)
                return BootKind.Jail;
            // Check if player is logged out but was in jail/prison
            if (WasInRegionWhenLoggedOut(pm, "Jail", Map.Felucca))
                return BootKind.OfflineJail;
            if (WasInRegionWhenLoggedOut(pm, "Angel Island", Map.Felucca))
                return BootKind.OfflinePrison;
            return BootKind.None;
        }

        // the boot's own parole restrictions (shared by HandlePrisonBoot, CanBoot and Confinement.Plan)
        internal static List<AIParoleExit.ParoleDenial> GetParoleDenials(PlayerMobile pm, bool clearCCounts)
        {
            List<AIParoleExit.ParoleDenial> denials = new();

            if (AIParoleExit.ExceedsIPLimit(pm))
                denials.Add(AIParoleExit.ParoleDenial.IPLimit);

            if (!clearCCounts && pm.ShortTermCriminalCounts > 0)
                denials.Add(AIParoleExit.ParoleDenial.CriminalCounts);

            if (pm.ShortTermMurders > 4 && !Core.RuleSets.SiegeRules())
                denials.Add(AIParoleExit.ParoleDenial.MurderCounts);

            if (!pm.Alive)
                denials.Add(AIParoleExit.ParoleDenial.Dead);

            return denials;
        }

        // everything that stops a staff boot from prison: the boot's own restrictions, then what the parole exit
        //  would still refuse after the boot has cleared counts and sentence (shared by CanBoot and Confinement.Plan)
        internal static List<AIParoleExit.ParoleDenial> GetStaffReleaseDenials(PlayerMobile pm, bool clearCCounts)
        {
            List<AIParoleExit.ParoleDenial> denials = GetParoleDenials(pm, clearCCounts);
            if (denials.Count == 0)
            {
                AIParoleExit.ParoleDenial denial = AIParoleExit.CheckParole(pm, afterBoot: true);
                if (denial != AIParoleExit.ParoleDenial.None)
                    denials.Add(denial);
            }
            return denials;
        }

        /// <summary>
        /// Preflight for Boot(): would booting m succeed right now? No side effects (nothing is moved,
        /// cleared, logged or sent). Uses the same checks as Boot(), plus the parole exit's own checks
        /// (AIParoleExit.CheckParole) that Boot cannot clear. reason explains a false result.
        /// </summary>
        public static bool CanBoot(Mobile m, bool clearCCounts, out string reason)
        {
            if (!CheckBootTarget(m, out PlayerMobile pm, out Account acct, out Region reg, out reason))
                return false;

            switch (GetBootKind(pm, reg))
            {
                case BootKind.Prison:
                case BootKind.OfflinePrison:
                    {
                        List<AIParoleExit.ParoleDenial> denials = GetStaffReleaseDenials(pm, clearCCounts);
                        reason = string.Join(" ", denials.Select(d => AIParoleExit.DenialText(pm, d, toStaff: true)));
                        return denials.Count == 0;
                    }
                case BootKind.Jail:
                case BootKind.OfflineJail:
                    return true;    // jail boots have no eligibility rules
                default:
                    reason = "This player is neither in prison or jail.";
                    return false;
            }
        }

        public static bool Boot(Mobile? staff, Mobile m, bool clearCCounts = false)
        {
            if (!CheckBootTarget(m, out PlayerMobile pm, out Account acct, out Region reg, out string targetReason))
            {
                staff?.SendMessage(targetReason);
                return false;
            }

            try
            {
                LogHelper logger = new LogHelper("Boot Command.log", false, false, false);
                Mobile actor = staff ?? (Mobile)Server.World.GetSystemAcct();

                if (pm.MuteMessages)
                {
                    logger.Log(LogType.Text, "restoring their ability to receive system messages");
                    pm.MuteMessages = false;
                }

                // Tor exit node handling
                if (acct.GetFlag(Accounting.Account.IPFlags.IsTorExitNode) ||
                    acct.InfractionStatus == Accounting.Account.AccountInfraction.TorExitNode)
                {
                    acct.Watched = true;
                    pm.Watched = true;
                    string reason = $"{pm} came in on a Tor Exit Node.";
                    logger.Log(LogType.Mobile, actor, reason);

                    using (LogHelper wlLogger = new LogHelper("Watchlist.log", overwrite: false, sline: true))
                    {
                        wlLogger.Log(LogType.Mobile, pm);
                        wlLogger.Log(LogType.Text, $"Watch added: {reason}");
                    }
                }

                switch (GetBootKind(pm, reg))
                {
                    case BootKind.Prison:
                        return HandlePrisonBoot(staff, pm, acct, logger, clearCCounts);

                    case BootKind.Jail:
                        return HandleJailBoot(staff, pm, acct, logger);

                    case BootKind.OfflineJail:
                        {
                            // handle offline jail boot
                            bool result = HandleJailBoot(staff, pm, acct, logger);
                            // synchronize logout location and hide
                            pm.Hidden = true;
                            pm.LogoutLocation = pm.Location;
                            pm.LogoutMap = pm.Map;
                            pm.MoveToWorld(pm.LogoutLocation, Map.Internal);
                            return result;
                        }

                    case BootKind.OfflinePrison:
                        {
                            bool result = HandlePrisonBoot(staff, pm, acct, logger, clearCCounts);
                            // synchronize logout location and hide
                            pm.Hidden = true;
                            pm.LogoutLocation = pm.Location;
                            pm.LogoutMap = pm.Map;
                            pm.MoveToWorld(pm.LogoutLocation, Map.Internal);
                            return result;
                        }

                    default:
                        staff?.SendMessage("This player is neither in prison or jail.");
                        logger.Finish();
                        return false;
                }
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                return false;
            }
        }
        private static bool WasInRegionWhenLoggedOut(PlayerMobile pm, string regionName, Map map)
        {
            if (pm == null || pm.LogoutMap == null || pm.LogoutMap != map)
                return false;

            var region = Region.Regions.OfType<StaticRegion>()
                .FirstOrDefault(r => r.Map == map && Insensitive.Equals(r.Name, regionName));

            return region != null && region.Contains(pm.LogoutLocation);
        }
        private static bool HandlePrisonBoot(Mobile? staff, PlayerMobile pm, Account acct, LogHelper logger, bool clearCCounts)
        {
            Mobile actor = staff ?? (Mobile)Server.World.GetSystemAcct(); // already captured in caller, but kept for clarity

            logger.Log(LogType.Mobile, actor, $"Using boot command from prison on {pm}.");
            logger.Log(LogType.Text, $"{pm} Infraction Status is: {pm.InfractionStatus}.");

            if (!clearCCounts)
                staff?.SendMessage("Use [boot ex to automatically clear login IPs, game login, and machine list.");

            AIParoleExit exit = new AIParoleExit();

            List<AIParoleExit.ParoleDenial> denials = GetParoleDenials(pm, clearCCounts);
            foreach (AIParoleExit.ParoleDenial denial in denials)
            {
                string text = AIParoleExit.DenialText(pm, denial, toStaff: true);
                staff?.SendMessage(text);
                logger.Log(LogType.Text, text);
            }
            bool canExit = denials.Count == 0;

            if (canExit)
            {
                if (clearCCounts)
                {   // [boot ex: account login history and long-term criminal counts, then the remaining prison sentence counts
                    logger.Log(LogType.Text, "clearing login IPs, game login, and machine list");
                    logger.Log(LogType.Text, $"clearing their LongTermCriminalCounts of {pm.LongTermCriminalCounts}");
                    PrisonOperations.ClearAccountHistory(pm, acct);

                    logger.Log(LogType.Text, $"clearing their ShortTermCriminalCounts of {pm.ShortTermCriminalCounts}");
                    PrisonOperations.ClearPrisonCriminalCounts(pm);
                }

                // prison check-out bookkeeping before the parole exit runs
                logger.Log(LogType.Text, $"clearing their MinimumSentence of {pm.MinimumSentence}");
                PrisonOperations.ClearMinimumSentence(pm);

                PrisonOperations.ClearAccountImprisoned(pm);
                ConfinementBookkeeping.ClearInfraction(pm);

                // the exit does the move and the parole check-out (PrisonOperations.ParoleCheckOut), and can still refuse
                //  (see AIParoleExit.CheckParole); false = moved
                bool moved = !exit.OnMoveOver(pm);

                // Clear Mortal flag unless on Mortalis (permadeath)
                ConfinementBookkeeping.ReleaseMortality(pm, hideOnMortalis: false);

                if (!moved)
                {
                    string text = "The parole exit refused them: " + AIParoleExit.DenialText(pm, AIParoleExit.CheckParole(pm, afterBoot: true), toStaff: true);
                    staff?.SendMessage(text);
                    logger.Log(LogType.Text, text);
                    canExit = false;
                }
            }

            exit.Delete();

            if (canExit)
                staff?.SendMessage("Paroled.");
            else
                staff?.SendMessage("Parole denied.");

            logger.Finish();
            return canExit;
        }

        private static bool HandleJailBoot(Mobile? staff, PlayerMobile pm, Account acct, LogHelper logger)
        {
            logger.Log(LogType.Mobile, staff ?? (Mobile)Server.World.GetSystemAcct(), $"Using boot command from jail on {pm}.");

            // jail check-out: the record (before the move, as before)
            JailOperations.RemoveRecord(pm);

            string spawnTag;
            Point3D spawnLoc;
            Map spawnMap;
            Server.Misc.CharacterCreation.NewCharacterSpawnLocation(null, pm, out spawnTag, out spawnLoc, out spawnMap);
            pm.MoveToWorld(spawnLoc, spawnMap);

            // jail check-out: account flag and infraction (after the move, as before)
            JailOperations.ClearAccountJailed(pm);
            ConfinementBookkeeping.ClearInfraction(pm);

            // Clear Mortal flag unless on Mortalis (permadeath)
            ConfinementBookkeeping.ReleaseMortality(pm, hideOnMortalis: false);

            staff?.SendMessage("Done.");
            logger.Finish();
            return true;
        }
        #endregion Jail, Prison & Boot
        #endregion Jail, Prison, Boot & Test Center

        #region Guard Rules
        public enum GuardRelevanceMode
        {
            Local,   // Region-specific: used when deciding to spawn/attack guards in this region
            Global   // Broad query: "am I whackable anywhere?" (UI, tooltips, status, logs, etc.)
        }

        public static class GuardUtility
        {
            /// <summary>
            /// Single source-of-truth method to determine if a mobile is guard-relevant.
            /// Behavior changes based on mode (Local vs Global).
            /// The region must be passed explicitly because m.Region can lag in transition hooks.
            /// </summary>
            public static bool IsGuardRelevant(Mobile? m, Region? r, GuardRelevanceMode mode)
            {
                if (m == null || !m.Alive)
                    return false;

                // Common early outs (independent of region/mode)
                if (m is BaseGuard || m is PlayerVendor || m.Blessed)
                    return false;

                GuardedRegion? region = r as GuardedRegion;

                // Staff note (kept for reference; usually commented out now)
                // if (m.AccessLevel > AccessLevel.Player) return false;

                // ────────────────────────────────────────────────────────────────
                // Region null safety
                // ────────────────────────────────────────────────────────────────
                if (region == null)
                {
                    // In almost all cases, no region = no guarded behavior
                    // But we allow global queries to fall back gracefully if possible
                    if (mode == GuardRelevanceMode.Local)
                        return false;

                    // For global mode: we can still answer based on mobile properties alone
                    // (criminal flag, timers, hotlist, etc.) but region-dependent rules will be skipped or defaulted
                    return HasGuardWhackConditions(m, null);  // pass null → internal defaults
                }

                bool isAlreadyCandidate = region.AlreadyGuardCandidate(m);

                // ────────────────────────────────────────────────────────────────
                // Mode-specific logic
                // ────────────────────────────────────────────────────────────────
                if (mode == GuardRelevanceMode.Local)
                {
                    // Local mode: must be in a guarded region
                    if (!region.IsGuarded)
                        return false;

                    // In local mode: already being chased → no new guard call (prevents spam)
                    if (isAlreadyCandidate)
                        return false;

                    return HasGuardWhackConditions(m, region);
                }
                else // Global mode
                {
                    // Global mode: ignore IsGuarded

                    // In global mode: already a candidate in *this* region context is a POSITIVE signal
                    if (isAlreadyCandidate)
                        return true;

                    return HasGuardWhackConditions(m, region);
                }
            }

            /// <summary>
            /// Shared core conditions that make someone guard-whackable.
            /// Handles null region gracefully (skips or defaults region-dependent checks).
            /// </summary>
            private static bool HasGuardWhackConditions(Mobile m, Region? contextRegion)
            {
                // Default to allowing reds if no region context (conservative for global queries)
                bool allowReds = contextRegion != null
                    ? AreRedsAllowed(contextRegion)
                    : true;  // or false — depending on your desired fallback; true is safer for "anywhere"

                return
                    // Short-timed criminal flag (region-independent)
                    (m.Criminal && m.CanBeGuardWhacked) ||

                    // Murderer/red rules — only if we have region context
                    (m.Red && !allowReds) ||

                    // Any active guard-whack timer (region-independent)
                    m.CanBeGuardWhacked ||

                    // Special / permanent overrides (region-independent)
                    Utility.InGrieferHotList(m) ||

                    // Region-specific monster evil rule — skip if no region
                    (contextRegion != null && GuardAI.GuardedRegionEvilMonsterRule(null, contextRegion, m));
            }

            // Convenience overloads

            public static bool IsGuardRelevantLocal(Mobile m, Region region)
                => IsGuardRelevant(m, region, GuardRelevanceMode.Local);

            public static bool IsGuardRelevantGlobal(Mobile m, Region region)
                => IsGuardRelevant(m, region, GuardRelevanceMode.Global);

        }
        public static bool AreRedsAllowed(Mobile m)
        {
            if (m == null)
                return Core.DefaultRedsInTown;

            return AreRedsAllowed(m.Region);
        }

        public static bool AreRedsAllowed(Region r)
        {
            if (r is StaticRegion sr && !Core.RuleSets.SiegeRules())
                return sr.AllowReds;

            return Core.DefaultRedsInTown;
        }
        //public static bool AreRedsEffectivelyAllowed(Region r)
        //{
        //    if (r is StaticRegion sr)
        //    {
        //        if (sr.GuardLocked())
        //            return true;
        //    }

        //    return AreRedsAllowed(r);
        #endregion Guard Rules
    }
}
