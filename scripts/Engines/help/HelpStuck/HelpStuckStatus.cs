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

/* Scripts/Engines/Help/HelpStuck/HelpStuckStatus.cs
 * Changelog:
 *  10/14/2026, Adam (Help Stuck: stranded on a boat)
 *      Boat facts: Boat (OnABoat is now Boat != null), BoatExit, BoatAuthority (staff judged as players in a test),
 *      TillerObstruction, BoatDecaying, ReachableShipKey and Captains. ShipKey is gone with the ShipKey gate:
 *      a key in the backpack is BoatAuthority, a key they could pick up is ReachableShipKey.
 *  10/14/2026, Adam (Help Stuck: Z correction removed, local unblock destinations)
 *      Removed BadZ, CheatingZ and GoodZ (Z correction is gone). Blocked: Mobile.IsBlocked. BlockedByMovables:
 *      blocked, but free once movable impassable items are ignored (HelpStuckProbes.IsBlockedIgnoringMovables).
 *  10/14/2026, Adam (Help Stuck: stranded at sea)
 *      Shore: where Strandedness would put a stranded player ashore. OnABoat: a boat at their location (a fact only;
 *      StrandedOnABoat is a separate state, not decided yet).
 *  10/13/2026, Adam (Help Stuck refactor)
 *      First time checkin. StuckStatus: the facts Help Stuck rules are decided on.
 *      House and HouseFriend: the house they are inside, and whether BaseHouse.IsFriend says they are its friend.
 */

/* StuckStatus - overview
 *  A StuckStatus is what is true about a player and their surroundings at one moment: the moment they ask
 *  for help, or the moment the delayed rescue comes due. It holds facts, not decisions:
 *      o raw values, not judgements: TotalWeight (not 'overweight'), LastAggression (not 'in combat').
 *          The thresholds belong to the rules (StuckRules).
 *      o cheap facts are read when the status is taken; expensive ones (pathfinding, area scans, item
 *          searches) are computed the first time a rule reads them, then remembered. A rule that is never
 *          reached never pays for its facts, and no fact is computed twice in one evaluation.
 *      o no side effects, with one exception: RegionAllowsStuck asks the region, and a region may message the
 *          player itself when it says no (Region.CanUseStuckMenu). It is the last gate, so it is only asked
 *          when everything else has passed, and its refusal is then the one the player hears. A dry run asks
 *          quietly.
 *  A new StuckStatus is taken when a delayed rescue comes due; it is never carried over from the request.
 */

using Server.Engines;
using Server.Mobiles;
using Server.Multis;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Server.Menus.Questions
{
    public enum StuckMode
    {
        Normal,     // a player's Help Stuck request
        LiveTest,   // [HelpStuckTest live: staff are judged as players, a local unblock moves at once and is not charged
        DryRun,     // [HelpStuckTest: staff are judged as players, nothing is changed, the decision is reported
    }

    public sealed class StuckStatus
    {
        public Mobile Mobile { get; }
        public StuckMode Mode { get; }
        public Point3D Location { get; }
        public Map Map { get; }
        public Region? Region { get; }

        public StuckStatus(Mobile m, StuckMode mode)
        {
            Mobile = m;
            Mode = mode;
            Location = m.Location;
            Map = m.Map;
            Region = m.Region;
            TotalWeight = m.TotalWeight;

            m_HasUses = Fact(() => m.CanUseStuckMenu());
            m_CanMoveFreely = Fact(() => HelpStuckProbes.CanMoveFreely(m));
            m_LastAggression = Fact(() => HelpStuckProbes.LastAggression(m));
            m_CarryingSigil = Fact(() => Factions.Sigil.ExistsOn(m));
            m_CarryingFlag = Fact(() => Engines.Alignment.TheFlag.ExistsOn(m));
            m_StrandedAtSea = Fact(() => HelpStuckProbes.StrandedAtSea(m));
            m_Shore = Fact(() => HelpStuckProbes.FindShore(m));
            m_Boat = Fact(() => HelpStuckProbes.FindBoat(m));
            m_BoatExit = Fact(() => Boat == null ? null : HelpStuckProbes.FindBoatExit(m, Boat));
            m_BoatAuthority = Fact(() => Boat == null ? CommandAuthority.None : Boat.GetCommandAuthority(m, JudgedAsPlayer));
            m_TillerObstruction = Fact(() => Boat?.FindTillerObstruction());
            m_BoatDecaying = Fact(() => Boat != null && Boat.DecayDue);
            m_ReachableShipKey = Fact(() => Boat == null ? ShipKeyLocation.None : HelpStuckProbes.FindReachableShipKey(m, Boat));
            m_Captains = Fact(() => Boat == null ? new List<BoatCaptain>() : HelpStuckProbes.FindOtherCaptains(m, Boat));
            m_HasLoadedPackAnimal = Fact(() => HelpStuckProbes.HasPackie(m));
            m_RegionAllowsStuck = Fact(() => Region == null || Region.CanUseStuckMenu(m, quiet: mode == StuckMode.DryRun));
            m_Blocked = Fact(() => m.IsBlocked(HelpStuckProbes.UnblockRange));
            m_BlockedByMovables = Fact(() => Blocked && !HelpStuckProbes.IsBlockedIgnoringMovables(m, HelpStuckProbes.UnblockRange));
            m_House = Fact(() => HelpStuckProbes.FindHouseInside(m));
            m_HouseFriend = Fact(() => House != null && House.IsFriend(m));
            m_ExitOptions = Fact(() =>
            {
                List<object> list = new List<object>();
                HelpStuckProbes.GetExitOptions(m, list);
                return list;
            });
        }

        private static Lazy<T> Fact<T>(Func<T> probe) => new Lazy<T>(probe, LazyThreadSafetyMode.None);

        #region Who
        public bool IsStaff => Mobile.AccessLevel > AccessLevel.Player;
        // [HelpStuckTest (either mode) judges staff by the player rules
        public bool JudgedAsPlayer => Mode != StuckMode.Normal;
        public bool Alive => Mobile.Alive;
        public bool HasUses => m_HasUses.Value;     // a Help Stuck use is left in the last 24 hours
        public bool TestCenterClone => Mobile.Clone && Mobile.IsCloneInTestCenter;
        public bool CanCriminalTravel => Mobile is not PlayerMobile pm || pm.CanCriminalTravel;
        public DateTime LastAggression => m_LastAggression.Value;
        #endregion Who

        #region Where
        public bool HelpStuckDisabled => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.HelpStuckDisabled);
        public bool PrisonRules => Region != null && Region.IsPrisonRules;
        public bool JailRules => Region != null && Region.IsJailRules;
        public bool DungeonRules => Region != null && Region.IsDungeonRules;
        public bool CanMoveFreely => m_CanMoveFreely.Value;         // pathfinding (dungeons only)
        public bool StrandedAtSea => m_StrandedAtSea.Value;     // in the water without a boat (Strandedness.IsStranded)
        public Point3D Shore => m_Shore.Value;                  // where Strandedness would put them ashore (Point3D.Zero: nowhere)
        // Aboard a boat. Facts only, asked of the boat itself (BaseBoat, Plank); StuckRules decides what they mean.
        //  StrandedOnABoat is a separate state from StrandedAtSea.
        public BaseBoat? Boat => m_Boat.Value;                  // the boat at their location (its footprint), or null
        public bool OnABoat => Boat != null;
        public BoatExit? BoatExit => m_BoatExit.Value;          // where walking over either plank puts them, or null
        // their claim to command it (BaseBoat.GetCommandAuthority; staff judged as players when JudgedAsPlayer)
        public CommandAuthority BoatAuthority => m_BoatAuthority.Value;
        public Item? TillerObstruction => m_TillerObstruction.Value;   // what keeps the tillerman from taking orders, or null
        public bool BoatDecaying => m_BoatDecaying.Value;       // BaseBoat.DecayDue: no orders are taken
        public ShipKeyLocation ReachableShipKey => m_ReachableShipKey.Value;   // a key they could pick up (alive only)
        public List<BoatCaptain> Captains => m_Captains.Value;  // other non-staff players aboard with a claim to command
        public bool RegionAllowsStuck => m_RegionAllowsStuck.Value; // may message the player (see overview)
        public bool Blocked => m_Blocked.Value;     // cannot walk UnblockRange tiles out and back in any direction (Mobile.IsBlocked)
        // blocked, but not once movable impassable items (crates, kegs, ...) are ignored: those items are what block them
        public bool BlockedByMovables => m_BlockedByMovables.Value;
        public List<object> ExitOptions => m_ExitOptions.Value;     // reachable exits within 64 tiles (pathfinding)
        public BaseHouse? House => m_House.Value;               // the house they are inside (its region, and on its tiles)
        public bool HouseFriend => m_HouseFriend.Value;         // BaseHouse.IsFriend: friend, co-owner, owner (and GameMaster+)
        #endregion Where

        #region Carrying
        public int TotalWeight { get; }
        public bool CarryingSigil => m_CarryingSigil.Value;
        public bool CarryingFlag => m_CarryingFlag.Value;
        public bool HasLoadedPackAnimal => m_HasLoadedPackAnimal.Value;
        #endregion Carrying

        private readonly Lazy<bool> m_HasUses;
        private readonly Lazy<bool> m_CanMoveFreely;
        private readonly Lazy<DateTime> m_LastAggression;
        private readonly Lazy<bool> m_CarryingSigil;
        private readonly Lazy<bool> m_CarryingFlag;
        private readonly Lazy<bool> m_StrandedAtSea;
        private readonly Lazy<Point3D> m_Shore;
        private readonly Lazy<BaseBoat?> m_Boat;
        private readonly Lazy<BoatExit?> m_BoatExit;
        private readonly Lazy<CommandAuthority> m_BoatAuthority;
        private readonly Lazy<Item?> m_TillerObstruction;
        private readonly Lazy<bool> m_BoatDecaying;
        private readonly Lazy<ShipKeyLocation> m_ReachableShipKey;
        private readonly Lazy<List<BoatCaptain>> m_Captains;
        private readonly Lazy<bool> m_HasLoadedPackAnimal;
        private readonly Lazy<bool> m_RegionAllowsStuck;
        private readonly Lazy<bool> m_Blocked;
        private readonly Lazy<bool> m_BlockedByMovables;
        private readonly Lazy<List<object>> m_ExitOptions;
        private readonly Lazy<BaseHouse?> m_House;
        private readonly Lazy<bool> m_HouseFriend;
    }
}
