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

/*
 * Scripts/Skills/Tracking.Dungeons.cs
 *	ChangeLog
 *  6/14/2026, Adam
 *
 * Dungeon Transition Tracking
 *
 * Dungeon entrances in UO are not geographically adjacent.
 * A player may step from an overworld coordinate directly into
 * a dungeon coordinate thousands of tiles away.
 *
 * When tracker and quarry are separated by a dungeon boundary,
 * tracking is redirected to the appropriate dungeon entrance or
 * exit point instead of attempting to follow raw coordinates.
 *
 * Once both parties are on the same side of the transition,
 * normal tracking resumes.
 */

using Server.Regions;
using System.Collections.Generic;

namespace Server.SkillHandlers
{


    public partial class TrackWhoGump
    {

        private static readonly DungeonTransition[] m_DungeonTransitions =
    {
    new DungeonTransition
    {
        Name = "Covetous",
        Map = Map.Felucca,
        Entrances = new Point3D[]
        {
            new Point3D(2499, 919, 0),     // Main entrance
            new Point3D(2421, 885, 0)      // Mountain pass (to be filled)
        },
        Exits = new Point3D[]
        {
            new Point3D(5456, 1863, 0),    // Main exit
            new Point3D(5393, 1957, 0)     // Mountain pass (to be filled)
        }
    },

    new DungeonTransition
    {
        Name = "Deceit",
        Map = Map.Felucca,
        Entrances = new Point3D[] { new Point3D(4111, 432, 5) },
        Exits = new Point3D[] { new Point3D(5188, 638, 0) }
    },

    new DungeonTransition
    {
        Name = "Despise",
        Map = Map.Felucca,
        Entrances = new Point3D[] { new Point3D(1298, 1080, 0) },
        Exits = new Point3D[] { new Point3D(5587, 630, 30) }
    },

    new DungeonTransition
    {
        Name = "Destard",
        Map = Map.Felucca,
        Entrances = new Point3D[] { new Point3D(1176, 2637, 0) },
        Exits = new Point3D[] { new Point3D(5243, 1005, 0) }
    },

    new DungeonTransition
    {
        Name = "Hythloth",
        Map = Map.Felucca,
        Entrances = new Point3D[] { new Point3D(4721, 3822, 0) },
        Exits = new Point3D[] { new Point3D(5905, 18, 56) }
    },

    new DungeonTransition
    {
        Name = "Shame",
        Map = Map.Felucca,
        Entrances = new Point3D[] { new Point3D(514, 1561, 0) },
        Exits = new Point3D[] { new Point3D(5396, 126, 0) }
    },

    new DungeonTransition
    {
        Name = "Wrong",
        Map = Map.Felucca,
        Entrances = new Point3D[] { new Point3D(2043, 238, 10) },
        Exits = new Point3D[] { new Point3D(5824, 629, 0) }
    },

    new DungeonTransition
    {
        Name = "Fire",
        Map = Map.Felucca,
        Entrances = new Point3D[] { new Point3D(2923, 3408, 8) },
        Exits = new Point3D[] { new Point3D(5687, 1423, 39) }
    },

    new DungeonTransition
    {
        Name = "Ice",
        Map = Map.Felucca,
        Entrances = new Point3D[] { new Point3D(2000, 81, 4) },
        Exits = new Point3D[] { new Point3D(5880, 242, 0) }
    }
};
        public static IReadOnlyList<DungeonTransition> DungeonTransitions => m_DungeonTransitions;
        // Dungeon coordinates are not geographically adjacent to their surface entrances.
        // When a suspect crosses a dungeon boundary, tracking points to the transition
        // point rather than attempting to resolve raw coordinate distance.
        private static bool IsDungeonBoundaryTransition(Mobile tracker, Mobile suspect, out DungeonTransition transition, out bool trackEntrance)
        {
            transition = null;
            trackEntrance = false;

            if (tracker == null || suspect == null || tracker.Deleted || suspect.Deleted)
                return false;

            if (tracker.Map == null || suspect.Map == null || tracker.Map != suspect.Map)
                return false;

            string? trackerDungeon = GetDungeon(tracker);
            string? suspectDungeon = GetDungeon(suspect);

            // Same space: both outside, or both in the same dungeon.
            if (Insensitive.Equals(trackerDungeon, suspectDungeon))
                return false;

            // Tracker outside, suspect inside:
            // guide tracker to the dungeon entrance.
            if (string.IsNullOrEmpty(trackerDungeon) && !string.IsNullOrEmpty(suspectDungeon))
            {
                transition = GetDungeonTransition(suspectDungeon, tracker.Map);
                trackEntrance = true;
                return transition != null;
            }

            // Tracker inside, suspect outside:
            // guide tracker to the dungeon exit.
            if (!string.IsNullOrEmpty(trackerDungeon) && string.IsNullOrEmpty(suspectDungeon))
            {
                transition = GetDungeonTransition(trackerDungeon, tracker.Map);
                trackEntrance = false;
                return transition != null;
            }

            // Different dungeons. Do not attempt chained dungeon routing.
            return false;
        }
        // TrackDungeonTransitionPoint
        private static void TrackDungeonTransitionPoint(
            Mobile from,
            Mobile suspect,
            Point3D point,
            Map map,
            TrackingRanges ranges,
            DungeonTransition transition,
            bool trackEntrance)
        {
            if (from == null || map == null || from.Map != map)
                return;

            double distance = from.GetDistanceToSqrt(point);

            if (distance <= ranges.Arrow)
            {
                if (trackEntrance)
                    from.SendMessage("Your notes suggest {0}'s trail leads toward the entrance to {1}.", suspect.Name, transition.Name);
                else
                    from.SendMessage("Your notes suggest {0}'s trail leads back toward the surface.", suspect.Name);

                from.QuestArrow = new TrackPointArrow(from, point, map, ranges.Arrow);
            }
            else if (distance <= ranges.Hint)
            {
                GiveForensicDirectionalHint(from, suspect, point);
            }
            else
            {
                from.SendMessage("The trail has gone cold.");
            }
        }
        private static bool TryGetDungeonTransitionPoint(Mobile tracker, DungeonTransition transition, bool trackEntrance, out Point3D point)
        {
            point = Point3D.Zero;

            if (tracker == null || transition == null)
                return false;

            Point3D[] points = trackEntrance ? transition.Entrances : transition.Exits;

            point = GetClosestPoint(tracker, points);

            return point != Point3D.Zero;
        }

        // etc.
        public class DungeonTransition
        {
            public string Name { get; set; }
            public Map Map { get; set; }
            public Point3D[] Entrances { get; set; }   // Overworld / surface
            public Point3D[] Exits { get; set; }       // Inside the dungeon
        }

        private static DungeonTransition? GetDungeonTransition(string dungeon, Map map)
        {
            if (string.IsNullOrEmpty(dungeon) || map == null)
                return null;

            foreach (DungeonTransition transition in m_DungeonTransitions)
            {
                if (transition == null)
                    continue;

                if (transition.Map == map && Insensitive.Equals(transition.Name, dungeon))
                    return transition;
            }

            return null;
        }
        private static Point3D GetClosestPoint(Mobile from, Point3D[] points)
        {
            Point3D best = Point3D.Zero;

            if (from == null || points == null || points.Length == 0)
                return best;

            double bestDistance = double.MaxValue;

            foreach (Point3D point in points)
            {
                double distance = from.GetDistanceToSqrt(point);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = point;
                }
            }

            return best;
        }

        /// <summary>
        /// Returns the name of the dungeon the point is in, or null if not in a dungeon.
        /// </summary>
        private static string? GetDungeon(IPoint3D p, Map map)
        {
            if (p == null || map == null)
                return null;

            // First quick check using your existing utility
            if (!Utility.WorldMap.Dungeons.Contains(p))
                return null;

            // Find the highest priority region at this location
            Region region = Region.Find(new Point3D(p), map);

            if (region is StaticRegion sr && sr.Name != null)
            {
                // Return the clean dungeon name (e.g. "Shame", "Wrong", etc.)
                return sr.Name;
            }

            // Fallback for other dungeon regions
            if (region != null && region != map.DefaultRegion && !string.IsNullOrEmpty(region.Name))
            {
                return region.Name;
            }

            return null;
        }

        /// <summary>
        /// Overload that takes an IEntity (Mobile or Item)
        /// </summary>
        private static string? GetDungeon(IEntity ent)
        {
            if (ent == null)
                return null;

            // If it's an item inside a container, use the root parent
            if (ent is Item item && item.RootParent != null)
                ent = item.RootParent as IEntity;

            if (ent == null)
                return null;

            return GetDungeon(ent.Location, ent.Map);
        }
    }
}
