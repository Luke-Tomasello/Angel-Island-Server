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

/* Scripts\Engines\NavStar\BeaconManager.cs
 * CHANGELOG
 * 3/10/2026, Adam,
 * 	Initial Creation
 */

using System;
using System.Collections.Generic;

namespace Server.Items
{
    public static class BeaconManagerCommand
    {
        private sealed class BeaconPlacement
        {
            public int Ring;
            public string? Journey;
            public Map? Map;
            public List<NavBeacon> Beacons = new List<NavBeacon>();
            public DateTime CreatedUtc;
        }

        private sealed class BeaconManagerState
        {
            public string Journey;
            public int Width;
            public int Count;
            public int StartRing;
            public int RingStep = 10;

            // This is the last surviving placed ring in history.
            // The next implicit placement is derived from this.
            public int? LastPlacedRing;

            public List<BeaconPlacement> History = new List<BeaconPlacement>();

            public int GetNextRingToPlace()
            {
                return LastPlacedRing.HasValue ? (LastPlacedRing.Value + RingStep) : StartRing;
            }

            public void RecalculateLastPlacedRing()
            {
                if (History.Count > 0)
                    LastPlacedRing = History[History.Count - 1].Ring;
                else
                    LastPlacedRing = null;
            }
        }

        private static readonly Dictionary<Serial, BeaconManagerState> m_States = new Dictionary<Serial, BeaconManagerState>();

        public static void Initialize()
        {
            CommandSystem.Register(
                "BeaconManager",
                AccessLevel.GameMaster,
                new CommandEventHandler(BeaconManager_OnCommand));
        }

        /*
            BeaconManager

            Arguments:
            -journey <name>      : Journey name used for created beacons
            -width <n>           : Width of the row laid directly in front of you
            -count <n>           : Maximum number of beacons to place
            -ring <n>            : Ring number to place immediately
            -ringstep <n>        : Ring increment for implicit continuation
            -undo [ring]         : Delete last placed batch, or all tracked batches for a specific ring
            -status              : Show current BeaconManager session state
            -clear               : Clear current BeaconManager session state and history

            Behavior:
            - No arguments continues the current session and places the next ring.
            - Supplying placement arguments updates the current session.
            - Supplying -ring places that ring immediately.
            - Supplying -undo removes only beacons created by this BeaconManager session.
            - Placement is a straight row one tile in front of you, centered on your facing.
            - Up to Count beacons are distributed across Width tiles.
            - If Width is smaller than Count, only Width beacons can be placed.

            Example:
            [BeaconManager -journey britain_invasion -width 20 -count 10 -ring 10 -ringstep 10
        */
        private static void BeaconManager_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null || from.Deleted)
                return;

            BeaconManagerState? state = GetState(from, false);

            if (e.Arguments == null || e.Arguments.Length == 0)
            {
                if (!HasRunnableState(state))
                {
                    SendUsage(from);
                    return;
                }

                PlaceNextRing(from, state);
                return;
            }

            ParsedArgs args = ParseArgs(e.Arguments);

            if (!string.IsNullOrEmpty(args.Error))
            {
                from.SendMessage(args.Error);
                SendUsage(from);
                return;
            }

            if (args.Clear)
            {
                ClearState(from);
                from.SendMessage("BeaconManager session cleared.");
                return;
            }

            if (args.Status)
            {
                ShowStatus(from, state);
                return;
            }

            if (args.Undo)
            {
                if (state == null)
                {
                    from.SendMessage("No BeaconManager session exists.");
                    return;
                }

                if (args.UndoRing.HasValue)
                    UndoRing(from, state, args.UndoRing.Value);
                else
                    UndoLast(from, state);

                return;
            }

            if (state == null)
                state = GetState(from, true);

            bool changed = false;

            if (args.Journey != null)
            {
                // Treat journey change as a new authoring session boundary.
                bool journeyChanged = !string.Equals(state.Journey, args.Journey, StringComparison.OrdinalIgnoreCase);

                state.Journey = args.Journey;
                changed = true;

                if (journeyChanged)
                {
                    state.History.Clear();
                    state.LastPlacedRing = null;
                }
            }

            if (args.Width.HasValue)
            {
                if (args.Width.Value <= 0)
                {
                    from.SendMessage("Width must be greater than zero.");
                    return;
                }

                state.Width = args.Width.Value;
                changed = true;
            }

            if (args.Count.HasValue)
            {
                if (args.Count.Value <= 0)
                {
                    from.SendMessage("Count must be greater than zero.");
                    return;
                }

                state.Count = args.Count.Value;
                changed = true;
            }

            if (args.RingStep.HasValue)
            {
                if (args.RingStep.Value <= 0)
                {
                    from.SendMessage("RingStep must be greater than zero.");
                    return;
                }

                state.RingStep = args.RingStep.Value;
                changed = true;
            }

            if (args.Ring.HasValue)
            {
                if (args.Ring.Value <= 0)
                {
                    from.SendMessage("Ring must be greater than zero.");
                    return;
                }

                state.StartRing = args.Ring.Value;

                // Explicit ring placement should place that exact ring immediately.
                if (!HasPlacementSettings(state))
                {
                    from.SendMessage("Journey, Width, and Count must be set before placing beacons.");
                    SendUsage(from);
                    return;
                }

                PlaceSpecificRing(from, state, args.Ring.Value);
                return;
            }

            if (changed)
            {
                from.SendMessage("BeaconManager state updated.");
                ShowStatus(from, state);
                return;
            }

            from.SendMessage("Nothing to do.");
            SendUsage(from);
        }

        private static BeaconManagerState? GetState(Mobile from, bool create)
        {
            BeaconManagerState? state;

            if (m_States.TryGetValue(from.Serial, out state))
                return state;

            if (!create)
                return null;

            state = new BeaconManagerState();
            m_States[from.Serial] = state;
            return state;
        }

        private static void ClearState(Mobile from)
        {
            m_States.Remove(from.Serial);
        }

        private static bool HasPlacementSettings(BeaconManagerState? state)
        {
            return state != null
                && !string.IsNullOrWhiteSpace(state.Journey)
                && state.Width > 0
                && state.Count > 0;
        }

        private static bool HasRunnableState(BeaconManagerState state)
        {
            return HasPlacementSettings(state) && state.StartRing > 0;
        }

        private static void PlaceNextRing(Mobile from, BeaconManagerState state)
        {
            int ring = state.GetNextRingToPlace();
            PlaceSpecificRing(from, state, ring);
        }

        private static void PlaceSpecificRing(Mobile from, BeaconManagerState state, int ring)
        {
            if (from.Map == null || from.Map == Map.Internal)
            {
                from.SendMessage("You must be in the world to place beacons.");
                return;
            }

            BeaconPlacement placement = new BeaconPlacement
            {
                Journey = state.Journey,
                Ring = ring,
                Map = from.Map,
                CreatedUtc = DateTime.UtcNow
            };

            List<Point3D> row = FindRowLocations(from, state.Width, state.Count);

            if (row.Count <= 0)
            {
                from.SendMessage("No valid beacon locations were found.");
                return;
            }

            for (int i = 0; i < row.Count; i++)
            {
                NavBeacon beacon = new NavBeacon(state.Journey, ring);
                beacon.MoveToWorld(row[i], from.Map);
                placement.Beacons.Add(beacon);
            }

            state.History.Add(placement);
            state.LastPlacedRing = ring;

            from.SendMessage("Placed {0} beacon{1} for Journey '{2}', Ring {3}.",
                placement.Beacons.Count,
                placement.Beacons.Count == 1 ? "" : "s",
                state.Journey,
                ring);

            int requested = Math.Min(state.Count, state.Width);

            if (placement.Beacons.Count < requested)
            {
                from.SendMessage("Requested up to {0}, but only {1} valid location{2} found.",
                    requested,
                    placement.Beacons.Count,
                    placement.Beacons.Count == 1 ? " was" : "s were");
            }
        }

        private static void UndoLast(Mobile from, BeaconManagerState state)
        {
            if (state.History.Count == 0)
            {
                from.SendMessage("Nothing to undo.");
                return;
            }

            BeaconPlacement placement = state.History[state.History.Count - 1];
            state.History.RemoveAt(state.History.Count - 1);

            int deleted = DeletePlacementBeacons(placement);

            state.RecalculateLastPlacedRing();

            from.SendMessage("Undo removed {0} beacon{1} from Journey '{2}', Ring {3}.",
                deleted,
                deleted == 1 ? "" : "s",
                placement.Journey,
                placement.Ring);
        }

        private static void UndoRing(Mobile from, BeaconManagerState state, int ring)
        {
            List<BeaconPlacement> matches = new List<BeaconPlacement>();

            for (int i = 0; i < state.History.Count; i++)
            {
                BeaconPlacement p = state.History[i];

                if (p.Ring == ring)
                    matches.Add(p);
            }

            if (matches.Count == 0)
            {
                from.SendMessage("No tracked placements found for Ring {0}.", ring);
                return;
            }

            int deleted = 0;

            foreach (BeaconPlacement p in matches)
                deleted += DeletePlacementBeacons(p);

            state.History.RemoveAll(p => p.Ring == ring);
            state.RecalculateLastPlacedRing();

            from.SendMessage("Undo removed {0} tracked placement set{1} for Ring {2}, deleting {3} beacon{4}.",
                matches.Count,
                matches.Count == 1 ? "" : "s",
                ring,
                deleted,
                deleted == 1 ? "" : "s");
        }

        private static int DeletePlacementBeacons(BeaconPlacement placement)
        {
            int deleted = 0;

            if (placement == null || placement.Beacons == null)
                return deleted;

            for (int i = 0; i < placement.Beacons.Count; i++)
            {
                NavBeacon beacon = placement.Beacons[i];

                if (beacon != null && !beacon.Deleted)
                {
                    beacon.Delete();
                    deleted++;
                }
            }

            return deleted;
        }

        private static List<Point3D> FindRowLocations(Mobile from, int width, int count)
        {
            List<Point3D> result = new List<Point3D>();

            Map map = from.Map;

            if (map == null || map == Map.Internal || width <= 0 || count <= 0)
                return result;

            int dx, dy;
            GetDirectionOffset(from.Direction & Direction.Mask, out dx, out dy);

            int lateralX = -dy;
            int lateralY = dx;

            int baseX = from.X + dx;
            int baseY = from.Y + dy;

            List<int> slots = BuildDistributedSlots(width, count);

            for (int i = 0; i < slots.Count; i++)
            {
                int offset = slots[i];
                int x = baseX + (lateralX * offset);
                int y = baseY + (lateralY * offset);
                int z = map.GetAverageZ(x, y);

                // Adjust this helper if your shard uses a different spawn validation call.
                if (!map.CanSpawnLandMobile(x, y, z))
                    continue;

                result.Add(new Point3D(x, y, z));
            }

            return result;
        }

        private static List<int> BuildDistributedSlots(int width, int count)
        {
            List<int> slots = new List<int>();

            if (width <= 0 || count <= 0)
                return slots;

            int toPlace = Math.Min(width, count);

            if (toPlace <= 0)
                return slots;

            if (toPlace == width)
            {
                int start = -(width / 2);

                for (int i = 0; i < width; i++)
                    slots.Add(start + i);

                return slots;
            }

            HashSet<int> used = new HashSet<int>();
            int startOffset = -(width / 2);

            for (int i = 0; i < toPlace; i++)
            {
                int index;

                if (toPlace == 1)
                    index = width / 2;
                else
                    index = (int)Math.Round((double)i * (width - 1) / (toPlace - 1));

                if (index < 0)
                    index = 0;
                else if (index >= width)
                    index = width - 1;

                while (used.Contains(index) && index < (width - 1))
                    index++;

                while (used.Contains(index) && index > 0)
                    index--;

                if (used.Contains(index))
                    continue;

                used.Add(index);
                slots.Add(startOffset + index);
            }

            slots.Sort();
            return slots;
        }

        private static void GetDirectionOffset(Direction dir, out int dx, out int dy)
        {
            dx = 0;
            dy = 0;

            switch (dir)
            {
                case Direction.North: dx = 0; dy = -1; break;
                case Direction.Right: dx = 1; dy = -1; break;
                case Direction.East: dx = 1; dy = 0; break;
                case Direction.Down: dx = 1; dy = 1; break;
                case Direction.South: dx = 0; dy = 1; break;
                case Direction.Left: dx = -1; dy = 1; break;
                case Direction.West: dx = -1; dy = 0; break;
                case Direction.Up: dx = -1; dy = -1; break;
                default: dx = 0; dy = -1; break;
            }
        }

        private static void ShowStatus(Mobile from, BeaconManagerState? state)
        {
            if (state == null)
            {
                from.SendMessage("No BeaconManager session exists.");
                return;
            }

            from.SendMessage("BeaconManager status:");
            from.SendMessage("  Journey: {0}", string.IsNullOrWhiteSpace(state.Journey) ? "(unset)" : state.Journey);
            from.SendMessage("  Width: {0}", state.Width);
            from.SendMessage("  Count: {0}", state.Count);
            from.SendMessage("  StartRing: {0}", state.StartRing);
            from.SendMessage("  RingStep: {0}", state.RingStep);
            from.SendMessage("  LastPlacedRing: {0}", state.LastPlacedRing.HasValue ? state.LastPlacedRing.Value.ToString() : "(none)");
            from.SendMessage("  NextRingToPlace: {0}", HasRunnableState(state) ? state.GetNextRingToPlace().ToString() : "(n/a)");
            from.SendMessage("  TrackedPlacementSets: {0}", state.History.Count);
        }

        private static void SendUsage(Mobile from)
        {
            from.SendMessage("Usage:");
            from.SendMessage("  [BeaconManager");
            from.SendMessage("  [BeaconManager -journey <name> -width <n> -count <n> -ring <n> [-ringstep <n>]");
            from.SendMessage("  [BeaconManager -width <n>");
            from.SendMessage("  [BeaconManager -count <n>");
            from.SendMessage("  [BeaconManager -ringstep <n>");
            from.SendMessage("  [BeaconManager -undo [ring]");
            from.SendMessage("  [BeaconManager -status");
            from.SendMessage("  [BeaconManager -clear");
        }

        private sealed class ParsedArgs
        {
            public string? Journey;
            public int? Width;
            public int? Count;
            public int? Ring;
            public int? RingStep;
            public bool Undo;
            public int? UndoRing;
            public bool Status;
            public bool Clear;
            public string Error;
        }

        private static ParsedArgs ParseArgs(string[] args)
        {
            ParsedArgs p = new ParsedArgs();

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];

                if (InsensitiveEquals(a, "-journey"))
                {
                    if (!TryGetNext(args, ref i, out p.Journey))
                    {
                        p.Error = "Missing value for -journey.";
                        return p;
                    }
                }
                else if (InsensitiveEquals(a, "-width"))
                {
                    int value;
                    if (!TryGetNextInt(args, ref i, out value))
                    {
                        p.Error = "Missing or invalid value for -width.";
                        return p;
                    }

                    p.Width = value;
                }
                else if (InsensitiveEquals(a, "-count"))
                {
                    int value;
                    if (!TryGetNextInt(args, ref i, out value))
                    {
                        p.Error = "Missing or invalid value for -count.";
                        return p;
                    }

                    p.Count = value;
                }
                else if (InsensitiveEquals(a, "-ring"))
                {
                    int value;
                    if (!TryGetNextInt(args, ref i, out value))
                    {
                        p.Error = "Missing or invalid value for -ring.";
                        return p;
                    }

                    p.Ring = value;
                }
                else if (InsensitiveEquals(a, "-ringstep"))
                {
                    int value;
                    if (!TryGetNextInt(args, ref i, out value))
                    {
                        p.Error = "Missing or invalid value for -ringstep.";
                        return p;
                    }

                    p.RingStep = value;
                }
                else if (InsensitiveEquals(a, "-undo"))
                {
                    p.Undo = true;

                    if ((i + 1) < args.Length)
                    {
                        int value;
                        if (int.TryParse(args[i + 1], out value))
                        {
                            p.UndoRing = value;
                            i++;
                        }
                    }
                }
                else if (InsensitiveEquals(a, "-status"))
                {
                    p.Status = true;
                }
                else if (InsensitiveEquals(a, "-clear"))
                {
                    p.Clear = true;
                }
                else
                {
                    p.Error = string.Format("Unknown argument '{0}'.", a);
                    return p;
                }
            }

            return p;
        }

        private static bool TryGetNext(string[] args, ref int i, out string value)
        {
            value = null;

            if ((i + 1) >= args.Length)
                return false;

            value = args[++i];
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool TryGetNextInt(string[] args, ref int i, out int value)
        {
            value = 0;

            if ((i + 1) >= args.Length)
                return false;

            return int.TryParse(args[++i], out value);
        }

        private static bool InsensitiveEquals(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}