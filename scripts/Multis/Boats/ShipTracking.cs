/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
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

/* Scripts/Multis/Boats/ShipTracking.cs
 * ChangeLog
 *  11/30/21, Yoar
 *      Initial version.
 */

using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Multis
{
    public static class ShipTracking
    {
        public static bool Enabled { get { return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.ShipTracking); } }

        public static bool NeedsSpyglass = true; // do we need a spyglass for ship tracking?
        public static bool NeedsPackMap = true; // do we need a map in our backpack for ship tracking? (if false, instances of TransientMapItem are spawned)
        public static bool CheckEuclidianDistance = false; // should we check the Euclidian distances when tracking for ships?
        public static int MaxCount = 5; // what is the maximum number of boats we can track? ('-1' means unlimited)
        public static int MapRange = 400; // range of the transient maps
        public static int MapSize = 240; // size of the transient maps
        public static int MapPool = 100; // size of the transient map pool
        public static TimeSpan TrackInterval = TimeSpan.FromSeconds(5.0); // track timer tick interval

        private static readonly Type[] m_ValidMapTypes = new Type[]
            {
                // store-bought maps
                typeof( PresetMap ),

                // player crafted maps
                typeof( LocalMap ),
                typeof( CityMap ),
                typeof( SeaChart ),
                typeof( WorldMap ),
            };

        private static bool IsValidMapType(Type type)
        {
            return Array.IndexOf(m_ValidMapTypes, type) != -1;
        }

        private static readonly Dictionary<BaseBoat, TrackTimer> m_Timers = new Dictionary<BaseBoat, TrackTimer>();

        public static void Initialize()
        {
            EventSink.Speech += EventSink_OnSpeech;
        }

        private static void EventSink_OnSpeech(SpeechEventArgs e)
        {
            Mobile m = e.Mobile;

            if (!Enabled || (m.X < 5120 && m.Map != Map.Felucca && m.Map != Map.Trammel))
                return;

            if (Insensitive.Equals(e.Speech, "start tracking"))
            {
                BaseBoat boat = BaseBoat.FindBoatAt(m);

                if (boat != null && !IsTracking(boat, m) && ValidateTracker(boat, m))
                    AddTracker(boat, m);
            }
            else if (Insensitive.Equals(e.Speech, "stop tracking"))
            {
                BaseBoat boat = BaseBoat.FindBoatAt(m);

                if (boat != null)
                    BeginRemoveTracker(boat, m);
            }
        }

        private static bool ValidateTracker(BaseBoat boat, Mobile m)
        {
            return m.Alive && m.Backpack != null && boat.Contains(m); // TODO: Do we need the ship key?
        }

        private static int GetTrackingRange(BaseBoat boat, Mobile m)
        {
            // pick the player's highest skill value between cartopgraphy and tracking
            double skill = Math.Min(120.0, Math.Max(m.Skills[SkillName.Cartography].Value, m.Skills[SkillName.Tracking].Value));

            return 150 + 5 * ((int)skill / 10); // for every 10 skill points, 5 extra tiles - 200 tiles at GM
        }

        private static bool IsTracking(BaseBoat boat, Mobile m)
        {
            TrackTimer timer;

            return m_Timers.TryGetValue(boat, out timer) && timer.Trackers.ContainsKey(m);
        }

        private static bool AddTracker(BaseBoat boat, Mobile m)
        {
            if (NeedsSpyglass && m.Backpack.FindItemByType(typeof(Spyglass)) == null)
            {
                m.SendMessage("You need a spyglass.");
                return false;
            }

            MapItem mapItem = null;

            if (NeedsPackMap)
            {
                foreach (Item item in m.Backpack.Items)
                {
                    if (item is MapItem)
                    {
                        MapItem check = (MapItem)item;

                        if (!check.Protected && !check.ReadOnly && check.Bounds.Width != 0 && check.Bounds.Height != 0 && IsValidMapType(check.GetType()))
                        {
                            mapItem = check;
                            break;
                        }
                    }
                }

                if (mapItem == null)
                {
                    m.SendMessage("You need a map.");
                    return false;
                }
            }

            RemoveTracker(boat, m); // sanity

            TrackTimer timer;

            if (!m_Timers.TryGetValue(boat, out timer))
                (m_Timers[boat] = timer = new TrackTimer(boat)).Start();

            timer.Trackers[m] = RegisterMapItem(m, mapItem);

            m.SendMessage("You begin tracking ships.");
            return true;
        }

        private static void BeginRemoveTracker(BaseBoat boat, Mobile m)
        {
            TrackTimer timer;

            if (!m_Timers.TryGetValue(boat, out timer) || !timer.Trackers.ContainsKey(m))
                return;

            if (!timer.ToRemove.Contains(m))
                timer.ToRemove.Add(m); // remove the tracker on the next timer tick
        }

        private static bool RemoveTracker(BaseBoat boat, Mobile m)
        {
            TrackTimer timer;

            if (!m_Timers.TryGetValue(boat, out timer))
                return false;

            MapItem mapItem;

            if (!timer.Trackers.TryGetValue(m, out mapItem))
                return false;

            UnregisterMapItem(mapItem);

            timer.Trackers.Remove(m);

            if (timer.Trackers.Count == 0)
            {
                timer.Stop();

                m_Timers.Remove(boat);
            }

            m.SendMessage("You stop tracking ships.");
            return true;
        }

        private class TrackTimer : Timer
        {
            private BaseBoat m_Boat;
            private Dictionary<Mobile, MapItem> m_Trackers;
            private List<Mobile> m_ToRemove;

            public Dictionary<Mobile, MapItem> Trackers { get { return m_Trackers; } }
            public List<Mobile> ToRemove { get { return m_ToRemove; } }

            public TrackTimer(BaseBoat boat)
                : base(TimeSpan.Zero, TrackInterval)
            {
                m_Boat = boat;
                m_Trackers = new Dictionary<Mobile, MapItem>();
                m_ToRemove = new List<Mobile>();
            }

            protected override void OnTick()
            {
                bool valid = Enabled && !m_Boat.Deleted && (m_Boat.Map == Map.Felucca || m_Boat.Map == Map.Trammel);

                int range = 0; // maximum tracking range

                foreach (KeyValuePair<Mobile, MapItem> kvp in m_Trackers)
                {
                    if (!m_ToRemove.Contains(kvp.Key) && (!valid || !ValidateTracker(m_Boat, kvp.Key) || !kvp.Value.IsChildOf(kvp.Key.Backpack) || !kvp.Value.Validate(kvp.Key)))
                    {
                        m_ToRemove.Add(kvp.Key);
                        continue;
                    }

                    int rangeCur = GetTrackingRange(m_Boat, kvp.Key);

                    if (range < rangeCur)
                        range = rangeCur;
                }

                foreach (Mobile m in m_ToRemove)
                    RemoveTracker(m_Boat, m);

                m_ToRemove.Clear();

                if (range == 0)
                    return;

                BaseBoat[] toTrack = FindBoatsInRange(m_Boat, range, MaxCount);

                foreach (KeyValuePair<Mobile, MapItem> kvp in m_Trackers)
                    DisplayMap(kvp.Key, kvp.Value, toTrack);

                if (m_Boat.TillerMan != null)
                    m_Boat.TillerMan.OnShipTracking(toTrack);
            }

            private void DisplayMap(Mobile m, MapItem mapItem, BaseBoat[] toTrack)
            {
                // remember the position of the map item
                Point3D p = mapItem.Location; // remember the location of the map inside the player's backpack

                // close all displays for this map
                mapItem.Internalize();

                mapItem.Pins.Clear();

                // center the map on our boat
                CartographersSextant.CenterMap(mapItem, m_Boat.Location);

                // add a pin for our boat
                if (mapItem.Bounds.Contains(m_Boat.Location))
                    mapItem.AddWorldPin(m_Boat.X, m_Boat.Y);

                // add a pin for every tracked boat
                for (int i = 0; i < toTrack.Length; i++)
                {
                    if (mapItem.Bounds.Contains(toTrack[i].Location))
                        mapItem.AddWorldPin(toTrack[i].X, toTrack[i].Y);
                }

                // move the map item back to its original position
                m.Backpack.AddItem(mapItem);
                mapItem.Location = p;

                // send the necessary item update packets
                mapItem.ProcessDelta();

                // open the map display for the tracker
                mapItem.DisplayTo(m);
            }
        }

        private static BaseBoat[] FindBoatsInRange(BaseBoat source, int range, int maxCount)
        {
            List<TrackResult> list = new List<TrackResult>();

            foreach (BaseBoat target in BaseBoat.Boats)
            {
                if (source == target || source.Map != target.Map || !Utility.InRange(source.Location, target.Location, range))
                    continue;

                int dx = target.X - source.X;
                int dy = target.Y - source.Y;

                int distSquared = dx * dx + dy * dy;

                if (CheckEuclidianDistance && distSquared > range * range)
                    continue;

                list.Add(new TrackResult(target, distSquared));
            }

            list.Sort(InternalComparer.Instance); // sort in ascending order by squared distance

            int count;

            if (maxCount == -1)
                count = list.Count; // unlimited
            else
                count = Math.Min(maxCount, list.Count);

            BaseBoat[] array = new BaseBoat[count];

            for (int i = 0; i < count; i++) // take the first 'maxCount' targets
                array[i] = list[i].Target;

            return array;
        }

        private struct TrackResult
        {
            private BaseBoat m_Target;
            private int m_DistSquared;

            public BaseBoat Target { get { return m_Target; } }
            public int DistSquared { get { return m_DistSquared; } }

            public TrackResult(BaseBoat target, int distSquared)
            {
                m_Target = target;
                m_DistSquared = distSquared;
            }
        }

        private class InternalComparer : IComparer<TrackResult>
        {
            public static readonly InternalComparer Instance = new InternalComparer();

            public InternalComparer()
            {
            }

            public int Compare(TrackResult x, TrackResult y)
            {
                return x.DistSquared.CompareTo(y.DistSquared);
            }
        }

        private static MapItem RegisterMapItem(Mobile from, MapItem mapItem)
        {
            if (mapItem == null)
            {
                mapItem = TransientMapItem.Get();

                from.Backpack.DropItem(mapItem);
            }

            return mapItem;
        }

        private static void UnregisterMapItem(MapItem mapItem)
        {
            if (mapItem is TransientMapItem)
                ((TransientMapItem)mapItem).Dispose();
        }

        private class TransientMapItem : MapItem
        {
            private static TransientMapItem[] m_Pool;
            private static int m_Index = -1;

            public static MapItem Get()
            {
                if (m_Pool == null)
                    m_Pool = new TransientMapItem[MapPool];

                for (int i = 0; i < m_Pool.Length; i++)
                {
                    if (++m_Index >= m_Pool.Length)
                        m_Index = 0;

                    TransientMapItem mapItem = m_Pool[m_Index];

                    if (mapItem == null || mapItem.Deleted)
                        return m_Pool[m_Index] = new TransientMapItem(true);

                    if (mapItem.Map == Map.Internal)
                        return mapItem;
                }

                return new TransientMapItem(false);
            }

            public override int LabelNumber { get { return 1015232; } } // sea chart

            private bool m_Pooled;

            public TransientMapItem(bool pooled)
                : base()
            {
                m_Pooled = pooled;

                Movable = false;

                SetDisplay(0, 0, MapRange, MapRange, MapSize, MapSize);
            }

            public void Dispose()
            {
                if (m_Pooled)
                    Internalize();
                else
                    Delete();
            }

            public TransientMapItem(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0); // version
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                Delete();
            }
        }
    }
}