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

/* Scripts\Engines\TerritoryControl\TerritoryRegions.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See TerritorySystem for an architectural overview
 */
using Server.Regions;
using System.Collections.Generic;

namespace Server.Engines.TerritoryControl
{
    public static class TerritoryRegions
    {
        /// <summary>
        /// Returns conquerable StaticRegions for a specific map, with additional world-area filtering.
        /// </summary>
        public static List<StaticRegion> GetConquerableRegions(Map map)
        {
            var list = new List<StaticRegion>();

            foreach (Region r in Region.Regions)
            {
                if (IsValidInvasionTarget(r, map))
                    list.Add(r as StaticRegion);
            }

            return list;
        }

        /// <summary>
        /// Centralized filter for regions eligible for invasion.
        /// Only regions fully within Britain Mainland are allowed.
        /// Excludes Lost Lands and Dungeons.
        /// </summary>
        public static bool IsValidInvasionTarget(Region region, Map map)
        {
            if (region is not StaticRegion sRegion)
                return false;

            if (sRegion.Map != map)
                return false;

            if (!sRegion.IsConquerable)
                return false;

            // Must have coords
            if (sRegion.Coords == null || sRegion.Coords.Count == 0)
                return false;

            foreach (Rectangle3D r3d in sRegion.Coords)
            {
                Rectangle2D rect = new Rectangle2D(r3d.Start, r3d.End);
                Point2D center = rect.Center;   // or new Point2D(rect.X + rect.Width/2, rect.Y + rect.Height/2)

                // If this rectangle is in an excluded area OR outside Britain Mainland → reject entire region
                if (Utility.WorldMap.LostLandsWrap.Contains(center) ||
                    Utility.WorldMap.Dungeons.Contains(center) ||
                    !Utility.WorldMap.BritMainLandWrap.Contains(center))
                {
                    return false;
                }
            }

            // All rectangles passed the filter
            return true;
        }
        /// <summary>
        /// Returns StaticRegions that should be treated as protected / illegal for invasion zones.
        /// These are regions that are inside conquerable areas but should NOT have zones created in them.
        /// </summary>
        public static List<StaticRegion> GetIllegalRegions(Map map)
        {
            var list = new List<StaticRegion>();

            foreach (Region r in Region.Regions)
            {
                if (r is StaticRegion sRegion && IsValidIllegalRegion(sRegion, map))
                    list.Add(sRegion);
            }

            return list;
        }

        /// <summary>
        /// Determines whether a region should be considered "illegal" for zone creation.
        /// </summary>
        private static bool IsValidIllegalRegion(StaticRegion region, Map map)
        {
            if (region == null || region.Map != map)
                return false;

            // Skip regions that are already marked as conquerable
            if (IsValidInvasionTarget(region, map))
                return false;

            // TODO: Add explicit protected regions here when ready for production
            /*
            string name = region.Name?.ToLowerInvariant() ?? "";

            if (name.Contains("angel island") ||
                name.Contains("prison") ||
                name.Contains("jail") ||
                name.Contains("sing sing") ||
                name.Contains("holding area"))
            {
                return true;
            }
            */

            // For now, we only exclude regions that are explicitly non-conquerable
            // but still exist inside conquerable areas.
            return true;   // ← This is the key line for initial testing
        }

    }
}