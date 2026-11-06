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

/* Scripts\Engines\TerritoryControl\TerritoryZone.cs
 * CHANGELOG:
 *   10/27/2026, Adam:
 *      - NotifyWebCompleted: the zone's conquest still happens once, but the district always re-evaluates its own
 *        completion. A zone that was already conquered used to swallow the notification, so the district never heard
 *        that its last web had been cleared.
 *      - IsConquered and ConqueredAt have public setters (as the district's and web's flags do) for PatchV257
 *        (TerritoryConquestRepair, in Patches201_300).
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See TerritorySystem for an architectural overview
 */

using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Engines.TerritoryControl
{
    public class TerritoryZone
    {
        public Rectangle2D Bounds { get; }

        private static uint _zoneSerial = 1;   // global counter
        public uint ZoneID { get; set; }
        public string Name { get; }
        public Map Map { get; }
        public bool IsConquered { get; set; }
        public DateTime ConqueredAt { get; set; }
        public GuardianWeb Web { get; internal set; }

        public TerritoryDistrict District { get; }

        public static void ResetZoneSerial()
        {
            _zoneSerial = 1;
        }
        internal static uint ZoneSerial
        {
            get { return _zoneSerial; }
            set { _zoneSerial = value; }
        }
        public static uint GenerateZoneID()
        {
            return _zoneSerial++;
        }

        public TerritoryZone(Rectangle2D bounds, Map map, TerritoryDistrict district, string? name = null)
        {
            Bounds = bounds;
            Map = map;
            District = district;
            ZoneID = GenerateZoneID();
            Name = name ?? $"{map} Zone({ZoneID})";
        }



        public bool Contains(Point3D loc)
        {
            // Only check coordinates - map is checked separately
            return Bounds.Contains(new Point2D(loc.X, loc.Y));
        }

        internal static TerritoryZone? FindZone(IEntity entity)
        {
            if (entity == null)
                return null;

            return FindZoneAt(entity.Location, entity.Map);
        }
        internal static TerritoryZone? FindZoneAt(IPoint3D p, Map map)
        {
            if (p == null || map == null)
                return null;

            Point3D loc = new Point3D(p.X, p.Y, p.Z);

            Region region = Region.Find(loc, map);

            if (region == null)
                return null;

            TerritoryDistrict? district = TerritorySystem.FindDistrict(region.Name);

            if (district == null)
                return null;

            foreach (TerritoryZone zone in district.Zones)
            {
                if (zone.Contains(loc))
                    return zone;
            }

            return null;
        }
        internal static List<PlayerMobile> GetPlayersInZone(IEntity entity)
        {
            var list = new List<PlayerMobile>();

            if (entity == null || entity.Map == null || entity.Map == Map.Internal)
                return list;

            TerritoryZone? zone = FindZone(entity);

            IPooledEnumerable eable = zone == null
                ? entity.Map.GetMobilesInRange(entity.Location, 30)
                : entity.Map.GetMobilesInBounds(zone.Bounds);

            try
            {
                foreach (Mobile m in eable)
                {
                    if (m is PlayerMobile pm && !pm.Deleted)
                        list.Add(pm);
                }
            }
            finally
            {
                eable.Free();
            }

            return list;
        }

        internal static TerritoryZone? FindZoneByID(uint zoneID)
        {
            foreach (TerritoryDistrict district in TerritorySystem.GetDistricts())
                foreach (var zone in district.Zones)
                    if (zone.ZoneID == zoneID)
                        return zone;
            return null;
        }

        public void NotifyWebCompleted()
        {
            // the zone is conquered once (OnZoneCleared notifies the district)
            if (!IsConquered && IsComplete)
            {
                OnZoneCleared();
                return;
            }

            // otherwise the district still re-evaluates its own completion (it latches once itself)
            District?.NotifyWebCompleted();
        }

        private bool IsComplete
        {
            get
            {
                return Web != null && Web.IsComplete;
            }
        }

        public void OnZoneCleared()
        {
            if (IsConquered)
                return;

            IsConquered = true;
            ConqueredAt = DateTime.UtcNow;

            District?.OnZoneCleared(this);
        }
        public override string ToString() => $"{Name} ({Bounds}) on {Map.Name}";
    }
}