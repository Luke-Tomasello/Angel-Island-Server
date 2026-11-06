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

/* Scripts/Engines/Help/HelpStuck/HelpStuckProbes.cs
 * Changelog:
 *  10/14/2026, Adam (Help Stuck: stranded on a boat)
 *      Boat facts for StrandedOnABoat, asked of the boat's own side-effect-free queries: FindBoat; FindBoatExit
 *      (Plank.FindDisembarkLocation, either plank); FindOtherCaptains (other non-staff players aboard with
 *      BaseBoat.GetCommandAuthority, and their relationship to the requester).
 *      FindShipKey is now FindReachableShipKey: a key to the boat a living player aboard could pick up (cursor, unlocked
 *      hold, deck). It matches by KeyValue (BaseBoat.CheckKey), as the tillerman does, not by Key.Link, and finds key
 *      rings too. A key in the backpack is command authority now, not a ShipKeyLocation.
 *      FindAssociation: same account, same nonzero hardware hash, same guild, allied guilds, same party.
 *      FindHostility: positive evidence only, an aggression record (they struck first) or the victim's corpse aboard
 *      naming them, or their creature, as its killer. Shared login IPs are recorded for the trace only.
 *  10/14/2026, Adam (Help Stuck: local unblock destinations)
 *      FindUnblockLocation is split: FindDesignatedExit (the house ban location, a township edge: unchanged) and
 *      FindBlockedDestination (physically blocked). Every blocked-move candidate, directional and the Spawner
 *      fallback, must now be in line of sight and in the same region (IsSafeUnblockDestination); the 2007 auto-move
 *      required both, and they were lost in the 2022 rewrite.
 *      IsBlockedIgnoringMovables: Mobile.IsBlocked with movable impassable items treated as passable, so the rules
 *      can tell a blockage made of movable items from real geometry.
 *  10/14/2026, Adam (Help Stuck: stranded at sea)
 *      StrandedAtSea now asks Strandedness.IsStranded, with no boat at their location. It used
 *      Utility.CanSpawnWaterMobile, whose fit check counts the player as blocking their own tile, so it was never
 *      true for a player.
 *      FindShore: the shore location Strandedness would move them to (Strandedness.FindShoreLocation), or none.
 *      OnABoat: a boat at their location. StrandedOnABoat is a separate state from StrandedAtSea; its policy is not
 *      decided yet.
 *  10/13/2026, Adam (Help Stuck refactor)
 *      Moved here unchanged from StuckMenu.cs: the world probes (free movement, stranded at sea, pack animals,
 *      ship keys), the local-unblock destination search and the exit-option scan.
 *      FindUnblockLocation now also reports which source produced the destination (house, township, blocked),
 *      for the decision trace only.
 *      FindHouseInside: the house check FindUnblockLocation already made, shared with the house rescue.
 *      Exit hint wording: "There's {0} nearby!" (was "There's {0} is nearby!") and "... from here." at a fair
 *      distance (was "from there", unlike the others).
 *      ExitHintLines replaces DescribeExitOption: the distinct exit hint lines, keyed on the rendered text, so
 *      several exits that read the same (a strip of teleporter tiles) say it once. ExitOptions itself is unchanged.
 *      The live response and the dry run both use it; HelpStuck sends the lines (probes no longer message).
 *      BlockedSource: the name of FindUnblockLocation's physically blocked source, which the rules use to choose
 *      DungeonUnblock.
 */

/* HelpStuckProbes - overview
 *  These are the questions Help Stuck asks of the world. They observe and search; they never message the
 *  player, charge a use, start a timer or move anything. StuckStatus calls them lazily, so the expensive ones
 *  (pathfinding, area scans) run only when a rule actually reads the answer.
 */

using Server.Engines;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;
using System;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Menus.Questions
{
    // a key to the boat a living player aboard could pick up (FindReachableShipKey). In the backpack it is command
    //  authority instead (BaseBoat.GetCommandAuthority).
    public enum ShipKeyLocation : byte
    {
        None,
        Hand,   // on their cursor
        Hold,   // in the boat's hold, unlocked
        Deck,   // lying on the deck
    }

    // FindAssociation: affirmative evidence of association, strongest first
    public enum Association
    {
        None,       // no evidence either way (not evidence that they are strangers)
        Account,    // the same account: one player
        Hardware,   // the same nonzero hardware hash (Account.HardwareHashRaw): the same machine
        Guild,      // the same guild
        Alliance,   // allied guilds (Guild.IsAlly)
        Party,      // the same party, now
    }

    // FindBoatExit: the plank and where walking over it puts them
    public sealed class BoatExit
    {
        public Plank Plank { get; }
        public Point3D Location { get; }
        public BoatExit(Plank plank, Point3D location) { Plank = plank; Location = location; }
    }

    // FindOtherCaptains: another player aboard with a claim to command the boat, and what is known of their
    //  relationship to the requester. Facts, not a verdict.
    public sealed class BoatCaptain
    {
        public Mobile Mobile { get; }
        public CommandAuthority Authority { get; }
        public Association Association { get; }     // FindAssociation(captain, requester)
        public string? Hostility { get; }           // FindHostility(captain, requester): null, none
        // a login IP in common (BountyKeeper.SharedAccount: any of the last 32 per account, no time window).
        //  Forensic only: households, carrier NAT and VPNs make it too loose to decide anything.
        public bool SharedLoginIP { get; }

        public BoatCaptain(Mobile mobile, CommandAuthority authority, Association association, string? hostility, bool sharedLoginIP)
        {
            Mobile = mobile;
            Authority = authority;
            Association = association;
            Hostility = hostility;
            SharedLoginIP = sharedLoginIP;
        }
    }

    public static class HelpStuckProbes
    {
        public const int UnblockRange = 4;
        public const string BlockedSource = "blocked";   // FindBlockedDestination: physically blocked, a short move past the obstruction

        public static bool IsEvacuationMove(Map fromMap, Map toMap, Point3D fromLoc, Point3D toLoc)
        {
            if (fromMap == null || toMap == null)
                return false;

            if (fromMap != toMap)
                return true; // cross-map is evac by definition

            // Distance check: local unblocks are small; town teleports are huge.
            // Pick threshold safely above your unblock radius / spawn range.
            const int EvacDistanceTiles = 30;

            return Utility.InRange(fromLoc, toLoc, EvacDistanceTiles) == false;
        }

        // StrandedAtSea and StrandedOnABoat are deliberately separate states: being in the water without a boat and
        //  being aboard one differ in how they are detected, what policy applies and how a rescue is carried out.
        //  Neither is defined in terms of the other.

        // the boat at their location (the boat's footprint; Z is not considered), or null
        public static BaseBoat? FindBoat(Mobile m)
        {
            if (m.Map == null || m.Map == Map.Internal)
                return null;

            return BaseBoat.FindBoatAt(m, m.Map);
        }

        // a boat at their location. A fact, not a judgement: whether they are stranded on it is StuckRules' question
        public static bool OnABoat(Mobile m)
        {
            return FindBoat(m) != null;
        }

        // in the water without a boat. Strandedness owns what 'stranded' means (no surface where they stand, over
        //  water); a boat under them is excluded explicitly
        public static bool StrandedAtSea(Mobile m)
        {
            return m.Map != null && m.Map != Map.Internal && !OnABoat(m) && Strandedness.IsStranded(m, m.Map);
        }

        // where Strandedness would put a stranded player ashore (Point3D.Zero: nowhere). Searches only, moves nothing.
        public static Point3D FindShore(Mobile m)
        {
            if (m.Map == null || m.Map == Map.Internal)
                return Point3D.Zero;

            return Strandedness.FindShoreLocation(m, m.Map, out Point3D shore) ? shore : Point3D.Zero;
        }
        public static bool CanMoveFreely(Mobile m)
        {   // all dungeons have exit teles.
            if (m != null && m.Region != null && m.Region.IsDungeonRules)
                for (int ix = 0; ix < 10; ix++)
                {   // can they walk 8-10 tiles? If so, they are looking for a free ride
                    Point3D newLocation = Spawner.GetSpawnPosition(m.Map, m.Location, 10, SpawnFlags.SpawnFar, m);
                    if (newLocation != m.Location && m.GetDistanceToSqrt(newLocation) >= 8)
                        if (Spawner.ClearPathLand(m.Location, newLocation, map: m.Map, true))
                            return true;
                }
            return false;
        }

        // the most recent time this mobile attacked anyone (DateTime.MinValue if never)
        public static DateTime LastAggression(Mobile m)
        {
            DateTime last = DateTime.MinValue;

            for (int i = 0; i < m.Aggressed.Count; i++)
            {
                AggressorInfo info = m.Aggressed[i];

                if (info.LastCombatTime > last)
                    last = info.LastCombatTime;
            }

            return last;
        }

        public static bool HasPackie(Mobile m)
        {
            foreach (Mobile check in m.GetMobilesInRange(3))
            {
                BaseCreature? bc = check as BaseCreature;

                if (bc == null)
                    continue;

                if (!bc.Controlled || bc.ControlMaster != m)
                    continue;

                if (bc.ControlOrder != OrderType.Come && bc.ControlOrder != OrderType.Follow && bc.ControlOrder != OrderType.Guard)
                    continue;

                if (!IsPackie(bc))
                    continue;

                if (bc.Backpack == null || bc.Backpack.Items.Count == 0)
                    continue;

                return true;
            }

            return false;
        }

        private static bool IsPackie(Mobile m)
        {
            return (m is PackHorse || m is PackLlama || m is Beetle || m is HordeMinion);
        }

        #region Boat
        // the boat's footprint rectangle (its multi bounds)
        private static Rectangle2D BoatBounds(BaseBoat boat)
        {
            MultiComponentList mcl = boat.Components;

            return new Rectangle2D(boat.X + mcl.Min.X, boat.Y + mcl.Min.Y, mcl.Width, mcl.Height);
        }

        // a key, or a key ring holding one, that opens this boat's planks: what BaseBoat.CarriesKey looks for
        private static bool IsBoatKey(Item item, BaseBoat boat)
        {
            if (item is Key key)
                return key.KeyValue != 0 && boat.CheckKey(key.KeyValue);

            if (item is KeyRing ring)
                return (boat.SPlank != null && ring.IsKeyOnRing(boat.SPlank.KeyValue))
                    || (boat.PPlank != null && ring.IsKeyOnRing(boat.PPlank.KeyValue));

            return false;
        }

        // a key to this boat that a living player aboard could pick up and command with: on their cursor, in the
        //  hold (unlocked), or lying on the deck. One in their backpack is not this: that is command authority
        //  already (BaseBoat.GetCommandAuthority). A ghost can lift nothing, so for a ghost there is none.
        public static ShipKeyLocation FindReachableShipKey(Mobile from, BaseBoat boat)
        {
            if (!from.Alive)
                return ShipKeyLocation.None;

            if (from.Dragging != null && IsBoatKey(from.Dragging, boat))
                return ShipKeyLocation.Hand;

            if (boat.Hold != null && !boat.Hold.Locked)
            {
                foreach (Item item in boat.Hold.FindItemsByType<Item>())
                {
                    if (IsBoatKey(item, boat))
                        return ShipKeyLocation.Hold;
                }
            }

            if (boat.Map != null)
            {
                IPooledEnumerable eable = boat.Map.GetItemsInBounds(BoatBounds(boat));

                try
                {
                    foreach (Item item in eable)
                    {
                        if (boat.Contains(item) && IsBoatKey(item, boat))
                            return ShipKeyLocation.Deck;
                    }
                }
                finally
                {
                    eable.Free();
                }
            }

            return ShipKeyLocation.None;
        }

        // where walking over either plank would put them (Plank.FindDisembarkLocation: the plank's own search, so
        //  a plank they can open counts as open), or null if neither puts them ashore
        public static BoatExit? FindBoatExit(Mobile from, BaseBoat boat)
        {
            foreach (Plank? plank in new[] { boat.PPlank, boat.SPlank })
            {
                if (plank != null && !plank.Deleted && plank.FindDisembarkLocation(from, out Point3D loc))
                    return new BoatExit(plank, loc);
            }

            return null;
        }

        // every other player aboard, not staff, with a claim to command the boat (BaseBoat.GetCommandAuthority),
        //  with what is known of their relationship to the requester. Facts only: StuckRules decides who counts.
        public static List<BoatCaptain> FindOtherCaptains(Mobile requester, BaseBoat boat)
        {
            List<BoatCaptain> captains = new List<BoatCaptain>();

            if (boat.Map == null || boat.Map == Map.Internal)
                return captains;

            List<Mobile> aboard = new List<Mobile>();
            IPooledEnumerable eable = boat.Map.GetMobilesInBounds(BoatBounds(boat));

            try
            {
                foreach (Mobile m in eable)
                    if (m != requester && m.Player && m.AccessLevel == AccessLevel.Player && boat.Contains(m))
                        aboard.Add(m);
            }
            finally
            {
                eable.Free();
            }

            foreach (Mobile m in aboard)
            {
                CommandAuthority authority = boat.GetCommandAuthority(m);

                if (authority == CommandAuthority.None || authority == CommandAuthority.StaffBoatOnly)
                    continue;

                captains.Add(new BoatCaptain(m, authority, FindAssociation(m, requester), FindHostility(m, requester, boat),
                    BountyKeeper.SharedAccount(requester, m)));
            }

            return captains;
        }
        #endregion Boat

        #region Association and hostility
        // affirmative evidence that a and b are associated: common control (same account; same hardware, the last
        //  machine each account logged in from) or a standing relationship (same guild, allied guilds, same party).
        //  The first that holds, strongest first. None means no evidence either way: it is not evidence that they
        //  are strangers. Shared login IPs are deliberately not here (see BoatCaptain.SharedLoginIP).
        public static Association FindAssociation(Mobile a, Mobile b)
        {
            if (a.Account != null && a.Account == b.Account)
                return Association.Account;

            if (a.Account is Accounting.Account aa && b.Account is Accounting.Account ba
                && aa.HardwareHashRaw != 0 && aa.HardwareHashRaw == ba.HardwareHashRaw)
                return Association.Hardware;

            if (a.Guild != null && a.Guild == b.Guild)
                return Association.Guild;

            if (a.Guild is Guilds.Guild ga && b.Guild is Guilds.Guild gb && ga.IsAlly(gb))
                return Association.Alliance;

            Engines.PartySystem.Party? party = Engines.PartySystem.Party.Get(a);
            if (party != null && party == Engines.PartySystem.Party.Get(b))
                return Association.Party;

            return Association.None;
        }

        // affirmative evidence that attacker attacked or killed victim (null: none). Positive evidence only: no
        //  record is not evidence of goodwill.
        //  o an aggression record: attacker struck first in their current fight (Aggressed/Aggressors keep who
        //      started it; either side's blows keep it alive; it lapses 2 minutes after the last one). Their pets
        //      starting a fight count: the master is recorded too (BaseCreature.DoHarmful).
        //  o the victim's corpse, aboard this boat, names attacker (or a creature attacker controls or summoned)
        //      as its Killer: the last one they fought. It lasts as long as the corpse.
        public static string? FindHostility(Mobile attacker, Mobile victim, BaseBoat boat)
        {
            if (attacker.CheckAggressed(victim) || victim.CheckAggressor(attacker))
                return "struck first in a fight with them (aggression record)";

            if (victim.Corpse is Corpse corpse && !corpse.Deleted && boat.Contains(corpse))
            {
                Mobile? killer = corpse.Killer;

                if (killer != null && (killer == attacker
                    || (killer is BaseCreature bc && (bc.ControlMaster == attacker || bc.SummonMaster == attacker))))
                    return killer == attacker ? "killed them (their corpse aboard names the killer)" : $"their {killer.Name} killed them (their corpse aboard names it)";
            }

            return null;
        }
        #endregion Association and hostility

        #region Local unblock destination
        // the house they are inside: in its region and on its tiles (BaseHouse.IsInside)
        public static BaseHouse? FindHouseInside(Mobile from)
        {
            HouseRegion? hr = from.Region.GetRegion(typeof(HouseRegion)) as HouseRegion;

            if (hr != null && hr.House != null && hr.House.IsInside(from))
                return hr.House;

            return null;
        }

        // a designated way out of where they are, the first that yields a location: the house they are inside (its
        //  ban location), then a township (a point on its edge). These leave the region by design, so the blocked
        //  search's line of sight and same-region checks do not apply to them.
        public static Point3D FindDesignatedExit(Mobile from, out string source)
        {
            source = "none";
            Map map = from.Map;

            if (map == null)
                return Point3D.Zero; // sanity

            BaseHouse? house = FindHouseInside(from);

            if (house != null)
            {
                source = "house";
                return house.BanLocation;   // HouseRegion.GoLocation
            }

            TownshipRegion? tsr = from.Region.GetRegion(typeof(TownshipRegion)) as TownshipRegion;

            if (tsr != null)
            {
                List<Point3D> edgeLocations = new List<Point3D>();

                if (tsr.Coords.Count != 0)
                    Utility.CalcRegionBounds(tsr, ref edgeLocations);

                Utility.Shuffle(edgeLocations);

                for (int i = 0; i < edgeLocations.Count; i++)
                {
                    Point3D edgeLocation = edgeLocations[i];

                    if (tsr.Map.CanSpawnLandMobile(edgeLocation))
                    {
                        source = "township";
                        return edgeLocation;
                    }
                }
            }

            return Point3D.Zero;
        }

        // physically blocked (IsBlocked): a short move past the obstruction. Every candidate, directional and
        //  fallback, must be a safe destination (IsSafeUnblockDestination).
        public static Point3D FindBlockedDestination(Mobile from)
        {
            Map map = from.Map;

            if (map == null)
                return Point3D.Zero; // sanity

            for (int i = 0; i < 8; i++)
            {
                Point3D edgeLoc = GetPointInDirection(from.Location, (Direction)i, UnblockRange);

                bool isValid = Utility.CanSpawnLandMobile(map, edgeLoc);

                if (!isValid)
                {
                    edgeLoc.Z = map.GetAverageZ(edgeLoc.X, edgeLoc.Y);

                    isValid = Utility.CanSpawnLandMobile(map, edgeLoc);
                }

                // Yoar: make sure we *cannot* path there, else there would be no point in TPing us there
                if (isValid && IsSafeUnblockDestination(from, edgeLoc) && !CanGetThere(from.Location, edgeLoc, from))
                    return edgeLoc;
            }

            Point3D spawnLoc = Spawner.GetSpawnPosition(map, from.Location, UnblockRange, SpawnFlags.None, from,
                (p, m) => IsSafeUnblockDestination(from, p));

            // Yoar: make sure we *cannot* path there, else there would be no point in TPing us there
            if (spawnLoc != Point3D.Zero && spawnLoc != from.Location && IsSafeUnblockDestination(from, spawnLoc) && !CanGetThere(from.Location, spawnLoc, from))
                return spawnLoc;

            return Point3D.Zero;
        }

        // a blocked-move destination must be in line of sight from where they stand and in the same region: a short
        //  move never goes through a wall or into another region
        public static bool IsSafeUnblockDestination(Mobile from, Point3D loc)
        {
            return from.InLOS(loc) && Region.Find(loc, from.Map) == from.Region;
        }

        // Mobile.IsBlocked, but with movable impassable items (crates, kegs, ...) treated as passable: the
        //  pathfinder's canMoveOverObstacles option. Blocked normally but not here: the movable items are what block them.
        public static bool IsBlockedIgnoringMovables(Mobile m, int numTiles)
        {
            Point3D here = m.Location;
            Point3D[] targets = new Point3D[]
            {
                new Point3D(here.X, here.Y - numTiles, here.Z),
                new Point3D(here.X, here.Y + numTiles, here.Z),
                new Point3D(here.X - numTiles, here.Y, here.Z),
                new Point3D(here.X + numTiles, here.Y, here.Z),
                new Point3D(here.X + numTiles, here.Y - numTiles, here.Z),
                new Point3D(here.X - numTiles, here.Y + numTiles, here.Z),
                new Point3D(here.X + numTiles, here.Y + numTiles, here.Z),
                new Point3D(here.X - numTiles, here.Y - numTiles, here.Z),
            };

            foreach (Point3D there in targets)
                if (MovementPath.PathTo(new Movement.MovementObject(here, there, m.Map, canMoveOverObstacles: true))
                    && MovementPath.PathTo(new Movement.MovementObject(there, here, m.Map, canMoveOverObstacles: true)))
                    return false;   // can get there and back

            return true;
        }

        private static Point3D GetPointInDirection(Point3D loc, Direction dir, int range)
        {
            int dx = 0;
            int dy = 0;
            Movement.Movement.Offset(dir, ref dx, ref dy);

            return new Point3D(loc.X + dx * range, loc.Y + dy * range, loc.Z);
        }
        #endregion Local unblock destination

        #region Exit options
        public static void GetExitOptions(Mobile from, List<object> exitOptions)
        {
            Map map = from.Map;

            if (map == null || map == Map.Internal)
                return;

            Rectangle2D rect = new Rectangle2D(from.X - 128 / 2, from.Y - 128 / 2, 128, 128);

            foreach (object o in from.Map.GetObjectsInBounds(rect))
            {
                if (!from.Alive && !from.Red && o is BaseHealer healer)
                {
                    if (CanGetThere(from.Location, healer.Location, from))
                        exitOptions.Add(healer);
                }
                else if (!from.Alive && o is EvilWanderingHealer evilHealer)
                {
                    if (CanGetThere(from.Location, evilHealer.Location, from))
                        exitOptions.Add(evilHealer);
                }
                else if (o is PublicMoongate public_moongate)
                {
                    if (CanGetThere(from.Location, public_moongate.Location, from) && public_moongate.Visible)
                        exitOptions.Add(public_moongate);
                }
                else if (o is Moongate moongate)
                {
                    if (CanGetThere(from.Location, moongate.Location, from) && moongate.Visible && !Utility.IsDungeon(moongate.PointDest))
                        exitOptions.Add(moongate);
                }
                else if (o is Sungate sungate && sungate.Running && !Utility.IsDungeon(sungate.PointDest))
                {
                    if (CanGetThere(from.Location, sungate.Location, from))
                        exitOptions.Add(sungate);
                }
                else if (o is Teleporter tele && !(o is HouseTeleporter) && tele.Running && !Utility.IsDungeon(tele.PointDest))
                {
                    if (CanGetThere(from.Location, tele.Location, from))
                        exitOptions.Add(tele);
                }
            }
        }

        // the exit hints the player is shown: one line per distinct rendered text, in ExitOptions order.
        //  The text is the key because it is exactly type, direction and distance band; several exits that read the
        //  same (a strip of teleporter tiles) say it once. Shared by the live response and the dry run.
        public static List<string> ExitHintLines(Mobile from, IEnumerable<object> exitOptions)
        {
            List<string> lines = new List<string>();
            HashSet<string> seen = new HashSet<string>();

            foreach (object o in exitOptions)
            {
                string? text = ExitOptionText(from, o);

                if (text != null && seen.Add(text))
                    lines.Add(text);
            }

            return lines;
        }

        public static string? ExitOptionText(Mobile from, object o)
        {
            IPoint3D? p = o as IPoint3D;

            if (p == null)
                return null; // sanity

            string name;

            if (o is BaseHealer)
                name = "a healer";
            else if (o is Moongate || o is Sungate)
                name = "a moongate";
            else if (o is Teleporter)
                name = "a teleporter";
            else
                name = "an exit";

            Direction dir = from.GetDirectionTo(p);

            string dirStr = dir.ToString().ToLower();

            switch (dir)
            {
                case Direction.Right: dirStr = "north-east"; break;
                case Direction.Down: dirStr = "south-east"; break;
                case Direction.Left: dirStr = "south-west"; break;
                case Direction.Up: dirStr = "north-west"; break;
            }

            double distance = from.GetDistanceToSqrt(p);

            string format;

            if (distance > 200)
                format = "There's {0} a long journey {1} from here.";
            else if (distance > 150)
                format = "There's {0} quite a long {1} distance from here.";
            else if (distance > 100)
                format = "There's {0} a long way {1} from here.";
            else if (distance > 75)
                format = "There's {0} a fair distance {1} from here.";
            else if (distance > 50)
                format = "There's {0} quite a ways {1} from here.";
            else if (distance > 40)
                format = "There's {0} just a short way {1} from here.";
            else if (distance > 20)
                format = "There's {0} but a few steps {1} from here.";
            else
                format = "There's {0} nearby!";

            return string.Format(format, name, dirStr);
        }

        private static bool CanGetThere(Point3D here, Point3D there, Mobile from)
        {
            Movement.MovementObject obj_start = new Movement.MovementObject(here, there, from.Map);

            return MovementPath.PathTo(obj_start);
        }
        #endregion Exit options
    }
}
