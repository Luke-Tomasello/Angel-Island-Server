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

/* Scripts\Engines\TestCenter\TestCenterPolicy.cs
 * CHANGELOG
 *	10/18/26, Adam (confinement Stage 5)
 *	    First version. One place owns the Test Center quarantine (Trammel is always Test Center, Map.TestCenter):
 *	    who may be where (Classify), the tripwire (OnMapChange, raised by both MoveToWorld and the Map setter, so every
 *	    way of crossing the boundary is seen, staff relocation included), the login checkpoint (OnLogin), the deferred
 *	    enforcement (Sweep, run by the CJailClones cron and by the tripwire), the portal checks (ValidatePortal) and the
 *	    release veto it registers with ConfinementVetoes. Test Center depends on Confinement, never the reverse.
 *	    Unchanged: staff travel as themselves (the portal never clones them), the legacy penalties
 *	    (Utility.ApplyTestCenterPenalties: cell 2 for a player, cell 1 and the clone penalties for a clone), the new
 *	    player area exemption, and TcEntryViolation / TcExitViolation as pending-enforcement flags consumed by Sweep.
 *	    First confinement wins: someone already in a jail or a prison (as an inmate) is never jailed or penalized again;
 *	    a pending violation found on them is recorded for staff (ConfinementBookkeeping.RecordAdditionalOffense).
 *	    The legacy -uotc server mode is not consulted.
 */

using Server.Diagnostics;
using Server.Engines.ConfinementSystem;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using System;
using System.Linq;

namespace Server.Engines
{
    public static class TestCenterPolicy
    {
        /// <summary>Where a character stands with respect to the Test Center quarantine, judged from its current state only.</summary>
        public enum Standing
        {
            Neutral,        // not in the world (deleted, no map, the internal map: logged out or mid session swap)
            Exempt,         // staff: they travel as themselves
            Contained,      // in a jail, or in a prison as an inmate, on any facet: first confinement wins
            Allowed,        // a clone in Test Center, a player outside it, a player in the new player area
            Intruder,       // a player in Test Center
            EscapedClone,   // a clone outside Test Center
        }

        public static void Initialize()
        {
            ConfinementVetoes.Register(ReleaseVeto);
        }

        public static Standing Classify(PlayerMobile? pm)
        {
            if (pm == null || pm.Deleted || pm.Map == null || pm.Map == Map.Internal)
                return Standing.Neutral;
            if (pm.AccessLevel > AccessLevel.Player)
                return Standing.Exempt;
            if (ConfinementBookkeeping.IsConfined(pm))
                return Standing.Contained;
            if (pm.Clone)
                return pm.Map == Map.TestCenter ? Standing.Allowed : Standing.EscapedClone;
            return Utility.WorldMap.IsIllegalInTestCenter(pm) ? Standing.Intruder : Standing.Allowed;
        }

        #region Tripwire and login
        /// <summary>
        /// PlayerMobile.OnMapChange (MoveToWorld and the Map setter). Judged a moment later, once the move (and any
        /// enforcement move, or a session swap) has finished: a character moved into confinement is Contained by then, and
        /// one moved to the internal map is Neutral, so neither is taken for an escape.
        /// </summary>
        public static void OnMapChange(PlayerMobile pm, Map oldMap)
        {
            if (pm == null || pm.Map == oldMap)
                return;
            if (pm.Clone || pm.Map == Map.TestCenter || oldMap == Map.TestCenter)
                Timer.DelayCall(TimeSpan.Zero, () => Check(pm));
        }

        /// <summary>PlayerMobile.OnLogin, after the clone cleanup: a second checkpoint, on the character's state when it fires.</summary>
        public static void OnLogin(PlayerMobile pm)
        {
            if (pm != null)
                Timer.DelayCall(TimeSpan.Zero, () => Check(pm));
        }

        // a violation raises its pending flag (once) and runs the deferred enforcement, as the legacy tripwires did
        private static void Check(PlayerMobile pm)
        {
            switch (Classify(pm))
            {
                case Standing.Intruder:
                    if (pm.TcEntryViolation)
                        return;
                    pm.TcEntryViolation = true;
                    break;
                case Standing.EscapedClone:
                    if (pm.TcExitViolation)
                        return;
                    pm.TcExitViolation = true;
                    break;
                default:
                    return;
            }
            Sweep();
        }
        #endregion Tripwire and login

        #region Enforcement
        /// <summary>
        /// The deferred enforcement (CJailClones, every five minutes, and the tripwire): every online character with a
        /// pending violation, and every clone outside Test Center. Someone already confined stays where they are: a pending
        /// violation found on them is recorded for staff and consumed. Returns how many were sent to jail.
        /// </summary>
        public static int Sweep()
        {
            int count = 0;
            foreach (NetState ns in NetState.Instances.ToArray())
            {
                if (ns == null || ns.Mobile is not PlayerMobile pm || pm.Deleted)
                    continue;

                if (ConfinementBookkeeping.IsConfined(pm))
                {
                    if (pm.TcEntryViolation || pm.TcExitViolation)
                        ConfinementBookkeeping.RecordAdditionalOffense(pm, "Test Center",
                            pm.TcEntryViolation ? "Player caught inside Test Center." : "Clone caught leaving Test Center.", dedupKeys: null);
                    pm.TcEntryViolation = false;
                    pm.TcExitViolation = false;
                    continue;
                }

                string? message = null;
                if (pm.TcEntryViolation)
                    message = "Player caught inside Test Center.";
                else if (pm.TcExitViolation)
                    message = "Clone caught leaving Test Center.";
                else if (pm.Clone && !pm.IsCloneInTestCenter && pm.Map != Map.Internal)
                    message = "Clone caught outside Test Center.";

                if (message == null)
                    continue;

                pm.TcEntryViolation = false;
                pm.TcExitViolation = false;

                if (Enforce(pm, message))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// The Test Center penalty: a player to Felucca jail cell 2 (possessions kept), a clone to cell 1 with the clone
        /// penalties (Utility.ApplyTestCenterPenalties, unchanged), each told why. First confinement wins: someone already
        /// confined is not moved or penalized; the offense is recorded for staff. True when the penalty was applied.
        /// </summary>
        public static bool Enforce(PlayerMobile pm, string message)
        {
            if (pm == null || pm.Deleted)
                return false;

            if (ConfinementBookkeeping.IsConfined(pm))
            {
                ConfinementBookkeeping.RecordAdditionalOffense(pm, "Test Center", message, dedupKeys: null);
                return false;
            }

            if (Utility.IsInJail(pm))
                return false;   // a non-inmate standing in a prison: ApplyTestCenterPenalties has always left them alone

            pm.SendMessage(0x22, pm.Clone
                ? "Test Center clones may not leave Test Center. This clone has been jailed and everything it carried destroyed."
                : "You may not enter Test Center as yourself: players visit Test Center only as clones, through the Test Center portal. You have been jailed; staff will review your case.");
            Utility.ApplyTestCenterPenalties(pm, message);
            return true;
        }
        #endregion Enforcement

        #region Portals
        /// <summary>
        /// Null when this TestCenterTeleporter may be used; otherwise why not. An entry portal (To) stands outside Test
        /// Center and puts the clone in Test Center; an exit portal (From) stands in Test Center and puts the original back
        /// outside it: its own destination when it has one, otherwise the original's logout facet (Felucca if none).
        /// original: for an exit, the character the clone returns to (null when it cannot be found: the legacy fallback).
        /// </summary>
        public static string? ValidatePortal(TestCenterTeleporter portal, PlayerMobile? original)
        {
            Map? here = portal.Map;
            switch (portal.Mode)
            {
                case TestCenterTeleporter.TestCenterMode.To:
                    {
                        if (here == null || here == Map.Internal || here == Map.TestCenter)
                            return string.Format("an entry portal must stand outside Test Center, not on {0}", here);
                        Map dest = portal.MapDest ?? Map.TestCenter;
                        if (dest != Map.TestCenter)
                            return string.Format("an entry portal must lead into Test Center, not to {0}", dest);
                        return null;
                    }
                case TestCenterTeleporter.TestCenterMode.From:
                    {
                        if (here != Map.TestCenter)
                            return string.Format("an exit portal must stand in Test Center, not on {0}", here);
                        Map? dest = portal.MapDest != null && portal.MapDest != Map.Internal
                            ? portal.MapDest
                            : original != null ? original.LogoutMap ?? Map.Felucca : Map.Felucca;
                        if (dest == null || dest == Map.Internal || dest == Map.TestCenter)
                            return string.Format("an exit portal must return the original outside Test Center, not to {0}", dest);
                        return null;
                    }
                default:
                    return null;
            }
        }

        /// <summary>A refused portal use: the character told, GameMaster+ staff told, CloneManagement.log.</summary>
        public static void ReportPortal(TestCenterTeleporter portal, PlayerMobile pm, string why)
        {
            pm.SendMessage("This portal is not working right now; staff have been told.");
            string text = string.Format("Test Center portal {0} at {1} {2} ({3}) refused {4}: {5}", portal.Serial, portal.Location, portal.Map, portal.Mode, pm, why);
            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.GameMaster, 0x482, "Staff message from SYSTEM:");
            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.GameMaster, 0x482, text);
            LogHelper logger = new LogHelper("CloneManagement.log", false, true);
            logger.Log(LogType.Mobile, pm, text);
            logger.Finish();
        }
        #endregion Portals

        #region Release veto
        // Registered with ConfinementVetoes. Test Center possessions never reach the production facets: nobody is released
        //  or moved from confinement in Test Center to another facet, and a clone is never released outside Test Center.
        //  Staff travel as themselves and are never refused.
        private static string? ReleaseVeto(PlayerMobile pm, Map fromMap, Map toMap, bool leaving, Mobile? actor)
        {
            if (pm.AccessLevel > AccessLevel.Player || toMap == Map.TestCenter)
                return null;
            if (fromMap == Map.TestCenter)
                return string.Format("{0} is confined in Test Center; Test Center possessions cannot be carried into {1}.", pm.Name, toMap);
            if (pm.Clone && leaving)
                return string.Format("{0} is a Test Center clone; a clone cannot be released into {1}.", pm.Name, toMap);
            return null;
        }
        #endregion Release veto
    }
}
