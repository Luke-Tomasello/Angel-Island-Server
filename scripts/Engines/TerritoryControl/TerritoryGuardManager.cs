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

/* Scripts\Engines\TerritoryControl\TerritoryGuardManager.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See TerritorySystem for an architectural overview
 */


using Server.Diagnostics;
using Server.Mobiles;
using Server.Regions;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Engines.TerritoryControl
{
    public static class TerritoryGuardManager
    {
        private static readonly List<Serial> _storedGuardSpawners = new();
        private static readonly string SavePath = $"Saves/{Core.Server}/TerritoryGuards.bin";

        public class GuardStats
        {
            public int RegionsAffected { get; set; }
            public int GuardsDitched { get; set; }
            public int GuardsRestored { get; set; }
        }

        public static void Configure()
        {
            EventSink.WorldLoad += OnWorldLoad;
            EventSink.WorldSave += OnWorldSave;
        }

        private static void OnWorldLoad()
        {
            Load();
        }

        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            Save();
        }

        public static GuardStats InitializeConquerableState(Map map)
        {
            // disable the guarded regions
            MakeConquerable(map);

            var stats = new GuardStats();
            var processedSpawners = new HashSet<Serial>();

            foreach (Region r in Region.Regions)
            {
                if (r is not StaticRegion sRegion || !TerritoryRegions.IsValidInvasionTarget(sRegion, map))
                    continue;

                stats.RegionsAffected++;

                // Snapshot the mobiles first to avoid modification-during-enumeration issues
                List<Mobile> mobilesSnapshot = new List<Mobile>(r.Mobiles.Values);

                foreach (Mobile m in mobilesSnapshot)
                {
                    if (m is BaseGuard guard && !guard.Deleted)
                    {
                        if (guard.Spawner is Spawner spawner && !spawner.Deleted)
                        {
                            if (processedSpawners.Add(spawner.Serial))
                            {
                                _storedGuardSpawners.Add(spawner.Serial);
                            }

                            guard.MoveToIntStorage();
                            stats.GuardsDitched++;
                        }
                        else
                        {
                            guard.Delete();
                            stats.GuardsDitched++;
                        }
                    }
                }
            }

            if (stats.GuardsDitched == 0)
            {
                Console.WriteLine("[TerritoryGuardManager] No guards found to ditch in conquerable regions.");
            }

            return stats;
        }

        public static GuardStats ShutdownConquerableState()
        {
            // enable the guarded regions
            ClearConquerable();

            var stats = new GuardStats();

            foreach (Serial serial in _storedGuardSpawners)
            {
                if (!serial.IsItem) continue;

                Item? item = World.FindItem(serial);
                if (item is Spawner spawner && !spawner.Deleted)
                {
                    foreach (object o in spawner.Objects)
                    {
                        if (o is Mobile m && m.Map == Map.Internal && !m.Deleted)
                        {
                            m.RetrieveMobileFromIntStorage(spawner.Location, spawner.Map);
                            stats.GuardsRestored++;
                        }
                    }
                }
            }

            _storedGuardSpawners.Clear();

            return stats;
        }
        private static readonly List<string> _townNames = new List<string>
        {
            "Britain",
            "Buccaneer's Den",
            "Cove",
            "Jhelom",
            "Magincia",
            "Minoc",
            "Moonglow",
            "Nujel'm",
            "Ocllo",
            "Serpent's Hold",
            "Skara Brae",
            "Trinsic",
            "Vesper",
            "Wind",
            "Yew",
        };
        /// <summary>
        /// Marks the configured town regions conquerable without changing guard state,
        /// so districts can be generated before guards are disabled.
        /// </summary>
        public static void MarkConquerable(Map map)
        {
            foreach (string regionName in _townNames)
            {
                if (Region.FindByName(regionName, map) is StaticRegion region)
                    region.IsConquerable = true;
            }
        }
        private static void MakeConquerable(Map map)
        {
            int count = 0;

            foreach (string regionName in _townNames)
            {
                StaticRegion region =
                    Region.FindByName(regionName, map) as StaticRegion;

                if (region == null)
                {
                    Console.WriteLine(
                        $"[TerritoryGuardManager] Unable to find StaticRegion '{regionName}' on {map}.");

                    continue;
                }

                region.IsConquerable = true;
                region.EnableGuards = false;
                count++;
            }

            Console.WriteLine(
                $"[TerritoryGuardManager] Guards disabled in {count} regions. " +
                "These regions are now conquerable.");
        }

        private static void ClearConquerable()
        {
            int count = 0;

            foreach (Region region in Region.Regions)
            {
                if (region is not StaticRegion sRegion || !sRegion.IsConquerable)
                    continue;

                var district = TerritorySystem.GetDistrictFromRegion(sRegion);

                if (district != null)
                    sRegion.EnableGuards = district.WasOriginallyGuarded;

                sRegion.IsConquerable = false;
                count++;
            }

            Console.WriteLine(
                $"[TerritoryGuardManager] Restored {count} conquerable regions to their original guard state.");
        }
        public static int DitchGuardsForRegion(StaticRegion region)
        {
            if (region == null) return 0;

            int count = 0;
            List<Mobile> mobilesSnapshot = new List<Mobile>(region.Mobiles.Values);

            foreach (Mobile m in mobilesSnapshot)
            {
                if (m is BaseGuard guard && !guard.Deleted)
                {
                    if (guard.Spawner is Spawner spawner && !spawner.Deleted)
                    {
                        if (!_storedGuardSpawners.Contains(spawner.Serial))
                            _storedGuardSpawners.Add(spawner.Serial);

                        guard.MoveToIntStorage();
                        count++;
                    }
                    else
                    {
                        guard.Delete();
                        count++;
                    }
                }
            }

            return count;
        }
        public static void RestoreGuardsForRegion(StaticRegion region)
        {
            if (region == null) return;

            // Find and restore guards for this specific region
            foreach (Serial serial in _storedGuardSpawners.ToArray()) // copy to avoid modification issues
            {
                if (!serial.IsItem) continue;

                Item? item = World.FindItem(serial);
                if (item is Spawner spawner && !spawner.Deleted && spawner.Map == region.Map)
                {
                    // Simple check: if spawner is inside the region
                    if (region.Contains(spawner.Location))
                    {
                        foreach (object o in spawner.Objects)
                        {
                            if (o is Mobile m && m.Map == Map.Internal && !m.Deleted)
                            {
                                m.RetrieveMobileFromIntStorage(spawner.Location, spawner.Map);
                            }
                        }

                        _storedGuardSpawners.Remove(serial);
                    }
                }
            }
        }
        private static void Save()
        {
            Utility.EnsurePath(SavePath);

            BinaryFileWriter? writer = null;
            try
            {
                writer = new BinaryFileWriter(SavePath, true);
                writer.Write(1); // version
                writer.Write(_storedGuardSpawners.Count);
                foreach (Serial s in _storedGuardSpawners)
                    writer.Write(s);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                writer?.Close();
            }
        }

        private static void Load()
        {
            _storedGuardSpawners.Clear();

            if (!File.Exists(SavePath))
                return;

            BinaryFileReader? reader = null;
            try
            {
                reader = new BinaryFileReader(new BinaryReader(new FileStream(SavePath, FileMode.Open)));
                int version = reader.ReadInt();

                if (version == 1)
                {
                    int count = reader.ReadInt();
                    for (int i = 0; i < count; i++)
                        _storedGuardSpawners.Add(reader.ReadInt());
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                reader?.Close();
            }
        }
    }
}