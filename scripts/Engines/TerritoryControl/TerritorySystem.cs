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
   * TerritoryControl System
   *
   * PURPOSE:
   * TerritoryControl is a dynamic world occupation system that transforms normal
   * static regions into contested territories. It replaces existing spawners with
   * invading Orc forces, creating meaningful tactical friction while preserving
   * the spirit of the original world.
   *
   * Players must defeat coordinated groups of Orc guardians (webs) to clear zones
   * and eventually conquer entire districts.
   *
   * DEACTIVATION / CONQUEST LEVELS:
   * 1. Spawner Level   - Individual ContestedSpawner reaches kill quota
   * 2. Web / Zone Level - All spawners in a GuardianWeb are defeated
   * 3. District Level  - All webs in a TerritoryDistrict are cleared (full conquest)
   *
   * CORE CONCEPT:
   * - Conquerable StaticRegions are divided into Districts and smaller Zones.
   * - Each zone analyzes nearby anchor spawners and spawns themed Orc Guardian Webs.
   * - Each web consists of multiple ContestedSpawners populated via power-based generation.
   * - Progress is tracked hierarchically with strong ownership and bidirectional links.
   *
   * OWNERSHIP HIERARCHY:
   *
   * TerritorySystem (Root / Orchestrator)
   * ├── _districts : List<TerritoryDistrict>          ← Geographic structure
   * │   └── Zones : List<TerritoryZone>
   * │       └── Web : GuardianWeb (bidirectional)
   * │
   * └── _activeWebs : List<GuardianWeb>               ← Combat structure
   *     └── Spawners : List<ContestedSpawner>
   *
   * HOW IT WORKS:
   * 1. [SetupTerritories] generates districts/zones and spawns Orc guardian webs.
   * 2. ZoneGuardianSpawner + OrcSpawnGenerator analyze anchors and create themed Orcs.
   * 3. Players fight → Progress flows: Spawner → Web → Zone → District.
   * 4. Stable territorial gains (kills on deactivated spawners) earn Orc Warlord mini-boss encounters.
   *
   * KEY MECHANICS:
   * - Time pressure: Spawners have a kill deadline. Failure resets spawner progress.
   * - Stable vs Random Kills:
   *      Random Kills are progress on active spawners and may be lost if the
   *      spawner regains ground.
   *      Stable Kills are progress on deactivated spawners and are used for
   *      mini-boss earning and other permanent conquest rewards.
   * - Mini-bosses:
   *      Districts earn Orc Warlord mini-bosses as territory is reclaimed.
   *      Mini-bosses are earned from stable kills on deactivated spawners.
   *      Warlords spawn near the most recently conquered zone and provide
   *      high-value encounters and rewards during the campaign.
   * - Stragglers: Living orcs remain after a spawner/web is cleared.
   * - Power-based spawning with Swarm vs Elite styles.
   *
   * PERSISTENCE:
   * - Saved to Saves/{Core.Server}/TerritorySystem.bin (version 1)
   * - Full round-trip persistence of districts, zones, webs, spawners, links,
   *   zone conquest timestamps, and mini-boss spawn state.
   *
   * KEY FEATURES:
   * - Explicit on/off control via TerritoryConfig.Enabled
   * - Strong hierarchical ownership and bidirectional linking
   * - Proper RunUO lifecycle (Configure + Initialize)
   * - Diagnostic tools ([ZoneInfo], [DistrictInfo], [ListZones], etc.)
   * - Guard management via TerritoryGuardManager
   *
   * FUTURE EXTENSIONS:
   * - Additional invasion factions and encounter types
   * - District-specific mini-bosses and warlords
   * - Dynamic district rewards and regional effects
   * - Territory-driven world events
   * /

/* Scripts\Engines\TerritoryControl\TerritorySystem.cs
 * CHANGELOG:
 *   11/4/2026, Adam:
 *      - TerritorySystem.bin version 4: each district's current campaign start (CampaignStart, CampaignObserved;
 *        TerritoryDistrict.CampaignID, the campaign journal's handle). A version 3 save loads with every campaign
 *        journaled as first seen (it began before the journal); the next save writes version 4. A version 4 save
 *        cannot be read by an older build.
 *   5/22/2026, Adam:
 *      - Initial checkin
 */

using Server.Diagnostics;
using Server.Engines.TerritoryControl.Events;
using Server.Items;
using Server.Regions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Engines.TerritoryControl
{
    public static partial class TerritorySystem
    {
        public static bool Initialized { get; private set; }   // in-memory only

        private static Dictionary<string, TerritoryDistrict> _districts = new Dictionary<string, TerritoryDistrict>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<GuardianWeb> _activeWebs = new();

        /// <summary>
        /// Returns all current districts (read-only for external use).
        /// </summary>
        public static IEnumerable<TerritoryDistrict> GetDistricts()
        {
            return _districts.Values;
        }

        public static void Configure()
        {
            EventSink.WorldLoad += OnWorldLoad;
            EventSink.WorldSave += OnWorldSave;
        }
        private static void OnWorldLoad()
        {
            TerritoryConfig.Load();           // Always load config (persistence)

            if (!TerritoryConfig.Enabled)
                return;

            OrcInvasionTable.Load();          // Heavy data only if enabled
            Load();                           // Load system state
        }

        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            TerritoryConfig.Save();           // Always save config

            if (!TerritoryConfig.Enabled)
                return;

            if (!Initialized)
            {
                Console.WriteLine("[TerritoryControl] WARNING: Save called but system is not initialized. Run [SetupTerritories] first.");
                return;
            }

            Save();
        }
        #region Primitive Operations (Return Data, No Messaging)

        #region Persistence

        private static readonly string SavePath = $"Saves/{Core.Server}/TerritorySystem.bin";
        private static void Load()
        {
            _districts.Clear();
            _activeWebs.Clear();
            Initialized = false;

            if (!File.Exists(SavePath))
            {
                Console.WriteLine("[TerritoryControl] No save file found. Fresh start.");
                return;
            }

            Console.WriteLine("[TerritoryControl] Loading TerritorySystem...");

            BinaryFileReader? reader = null;

            try
            {
                reader = new BinaryFileReader(new BinaryReader(new FileStream(SavePath, FileMode.Open, FileAccess.Read)));

                int version = reader.ReadInt();

                switch (version)
                {
                    case 4:     // the current campaign's start (CampaignStart, CampaignObserved)
                    case 3:
                        {
                            int districtCount = reader.ReadInt();

                            for (int d = 0; d < districtCount; d++)
                            {
                                string? districtName = reader.ReadString();
                                string? mapName = reader.ReadString();
                                Map districtMap = Map.Parse(mapName) ?? Map.Felucca;
                                bool districtConquered = reader.ReadBool();
                                int miniBossesSpawned = reader.ReadInt();
                                bool wasOriginallyGuarded = reader.ReadBool();

                                var district = new TerritoryDistrict(districtName, districtMap)
                                {
                                    IsConquered = districtConquered,
                                    MiniBossesSpawned = miniBossesSpawned,
                                    WasOriginallyGuarded = wasOriginallyGuarded
                                };

                                if (version >= 4)
                                {
                                    district.CampaignStart = reader.ReadDateTime();
                                    district.CampaignObserved = reader.ReadBool();
                                }

                                int zoneCount = reader.ReadInt();

                                for (int z = 0; z < zoneCount; z++)
                                {
                                    int x = reader.ReadInt();
                                    int y = reader.ReadInt();
                                    int w = reader.ReadInt();
                                    int h = reader.ReadInt();
                                    string? zMapName = reader.ReadString();
                                    string? zoneName = reader.ReadString();
                                    uint zoneID = reader.ReadUInt();
                                    bool zoneConquered = reader.ReadBool();

                                    DateTime conqueredAt = DateTime.MinValue;

                                    if (zoneConquered)
                                        conqueredAt = reader.ReadDateTime();

                                    Map zoneMap = Map.Parse(zMapName) ?? Map.Felucca;
                                    Rectangle2D bounds = new Rectangle2D(x, y, w, h);

                                    var zone = new TerritoryZone(bounds, zoneMap, district, zoneName)
                                    {
                                        ZoneID = zoneID,
                                        IsConquered = zoneConquered,
                                        ConqueredAt = conqueredAt
                                    };

                                    district.AddZone(zone);
                                }

                                System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(district.Name));

                                if (district.Zones.Count > 0 && !string.IsNullOrWhiteSpace(district.Name))
                                    _districts[district.Name] = district;
                            }

                            // Restore the next ZoneID after zone construction,
                            // since TerritoryZone constructors advance the serial.
                            TerritoryZone.ZoneSerial = reader.ReadUInt();

                            int webCount = reader.ReadInt();

                            for (int i = 0; i < webCount; i++)
                            {
                                var web = new GuardianWeb
                                {
                                    //WebKillsNeeded = reader.ReadInt(),
                                    //TotalKills = reader.ReadInt(),
                                    IsConquered = reader.ReadBool()
                                };

                                int spawnerCount = reader.ReadInt();

                                for (int j = 0; j < spawnerCount; j++)
                                {
                                    Serial serial = reader.ReadInt();
                                    ContestedSpawner? spawner = FindSpawnerBySerial(serial);

                                    if (spawner != null && !spawner.Deleted)
                                        web.AddSpawner(spawner);
                                }

                                uint zoneID = reader.ReadUInt();

                                System.Diagnostics.Debug.Assert(zoneID != 0);

                                if (zoneID != 0)
                                {
                                    TerritoryZone? zone = TerritoryZone.FindZoneByID(zoneID);

                                    System.Diagnostics.Debug.Assert(zone != null);

                                    if (zone != null)
                                        web.SetZone(zone);
                                }

                                if (web.Spawners.Count > 0)
                                    _activeWebs.Add(web);
                            }

                            // an older save: its campaigns began before the journal; each is journaled as first seen (its
                            //  webs are loaded now). The next save keeps the ID.
                            if (version < 4)
                                foreach (TerritoryDistrict district in _districts.Values)
                                    district.ObserveCampaign();

                            break;
                        }

                    default:
                        Console.WriteLine($"[TerritoryControl] Unsupported TerritorySystem.bin version: {version}");
                        return;
                }

                Initialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[TerritoryControl] Error loading TerritorySystem.bin");
                LogHelper.LogException(ex);
            }
            finally
            {
                reader?.Close();
            }

            Console.WriteLine($"[TerritoryControl] Loaded {_districts.Count} districts, {_activeWebs.Count} active webs with {_activeWebs.Sum(w => w.Spawners.Count)} spawners.");
        }

        private static void Save()
        {
            Console.WriteLine("[TerritoryControl] Saving TerritorySystem...");

            BinaryFileWriter? writer = null;

            try
            {
                writer = new BinaryFileWriter(SavePath, true);
                writer.Write(4); // version

                writer.Write(_districts.Count);

                foreach (TerritoryDistrict district in _districts.Values)
                {
                    writer.Write(district.Name);
                    writer.Write(district.Map?.Name ?? "Felucca");
                    writer.Write(district.IsConquered);
                    writer.Write(district.MiniBossesSpawned);
                    writer.Write(district.WasOriginallyGuarded);
                    writer.Write(district.CampaignStart);           // version 4
                    writer.Write(district.CampaignObserved);        // version 4
                    writer.Write(district.Zones.Count);

                    foreach (TerritoryZone zone in district.Zones)
                    {
                        writer.Write(zone.Bounds.X);
                        writer.Write(zone.Bounds.Y);
                        writer.Write(zone.Bounds.Width);
                        writer.Write(zone.Bounds.Height);
                        writer.Write(zone.Map?.Name ?? "Felucca");
                        writer.Write(zone.Name);
                        writer.Write(zone.ZoneID);
                        writer.Write(zone.IsConquered);

                        if (zone.IsConquered)
                            writer.Write(zone.ConqueredAt);
                    }
                }

                writer.Write(TerritoryZone.ZoneSerial);

                writer.Write(_activeWebs.Count);

                foreach (GuardianWeb web in _activeWebs)
                {
                    //writer.Write(web.WebKillsNeeded);
                    //TotalKills = reader.ReadInt(),
                    writer.Write(web.IsConquered);

                    writer.Write(web.Spawners.Count);

                    foreach (ContestedSpawner spawner in web.Spawners)
                        writer.Write(spawner.Serial);

                    writer.Write(web.Zone?.ZoneID ?? 0u);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[TerritoryControl] Error saving TerritorySystem.bin");
                LogHelper.LogException(ex);
            }
            finally
            {
                writer?.Close();
            }
        }

        private static ContestedSpawner? FindSpawnerBySerial(Serial serial)
        {
            if (serial.IsItem)
            {
                Item? item = World.FindItem(serial);
                return item as ContestedSpawner;
            }
            return null;
        }

        #endregion

        public static void RegisterWeb(GuardianWeb web)
        {
            if (web != null && !_activeWebs.Contains(web))
                _activeWebs.Add(web);
        }

        /// <summary>
        /// Generates districts and zones using vertical stripe slicing.
        /// This respects protected areas (illegal regions) inside larger conquerable regions.
        /// </summary>
        private static int GenerateDistricts(Map map)
        {
            _districts.Clear();

            // Step 1: Get all conquerable (guarded) regions
            var conquerableRegions = TerritoryRegions.GetConquerableRegions(map);

            // Step 2: Get all illegal/protected regions (prisons, etc.)
            var illegalRegions = TerritoryRegions.GetIllegalRegions(map);

            // Step 2a: We no longer need to exclude these regions as we now explicitly collect only Conquerable regions
            //  Any excluded rectangles are handled in spawner placement (although the (empty) zone still exists)
            illegalRegions = new();

            foreach (StaticRegion sRegion in conquerableRegions)
            {
                var district = new TerritoryDistrict(sRegion.Name ?? $"Region_{sRegion.UId}", map)
                {
                    WasOriginallyGuarded = sRegion.GetFlag(RegionFlag.EnableGuards)
                };

                // Step 3: Slice this conquerable region into vertical zones, avoiding illegal areas
                SliceRegionIntoVerticalZones(sRegion, district, illegalRegions, preferredWidth: 13);

                if (district.Zones.Count > 0 && !string.IsNullOrWhiteSpace(district.Name))
                {
                    System.Diagnostics.Debug.Assert(!_districts.ContainsKey(district.Name));
                    _districts[district.Name] = district;
                }
            }

            Initialized = true;
            return _districts.Count;
        }
        /// <summary>
        /// Slices a single conquerable StaticRegion into multiple vertical TerritoryZones.
        /// Skips any areas that overlap with illegal/protected regions (like prisons).
        /// Uses a greedy left-to-right approach with flexible width at the end.
        /// </summary>

        /// <summary>
        /// Stage 1: Slice region into vertical stripes (respecting illegal regions)
        /// Stage 2: Cap each vertical stripe's height at max 26 tiles
        /// </summary>
        private static void SliceRegionIntoVerticalZones(StaticRegion region, TerritoryDistrict district, List<StaticRegion> illegalRegions, int preferredWidth = 13, int maxHeight = 26)
        {
            if (region == null || region.Coords == null)
                return;

            foreach (Rectangle3D r3d in region.Coords)
            {
                Rectangle2D bounds = new Rectangle2D(r3d.Start, r3d.End);

                int currentX = bounds.X;

                while (currentX < bounds.X + bounds.Width)
                {
                    int remainingWidth = bounds.X + bounds.Width - currentX;
                    int zoneWidth = Math.Min(preferredWidth, remainingWidth);

                    Rectangle2D candidate = new Rectangle2D(currentX, bounds.Y, zoneWidth, bounds.Height);

                    //if (!IntersectsAnyIllegalRegion(candidate, illegalRegions))
                    //{

                    // Stage 2: Split this tall vertical stripe into smaller height chunks
                    SplitVerticalZoneHorizontally(candidate, region.Map, district, region.Name, maxHeight);

                    //}
                    //else
                    //{
                    //    currentX += 1; // Move forward slowly when blocked
                    //    continue;
                    //}

                    currentX += zoneWidth;
                }
            }
        }
        /// <summary>
        /// Takes one tall vertical zone and splits it into smaller zones with maxHeight
        /// </summary>

        private static void SplitVerticalZoneHorizontally(Rectangle2D tallZone, Map map, TerritoryDistrict district, string name, int maxHeight)
        {
            int currentY = tallZone.Y;

            while (currentY < tallZone.Y + tallZone.Height)
            {
                int remainingHeight = tallZone.Y + tallZone.Height - currentY;
                int zoneHeight = Math.Min(maxHeight, remainingHeight);

                Rectangle2D smallZone = new Rectangle2D(tallZone.X, currentY, tallZone.Width, zoneHeight);

                var zone = new TerritoryZone(smallZone, map, district, name);
                district.AddZone(zone);

                currentY += zoneHeight;
            }
        }

        /// <summary>
        /// Returns true if the candidate rectangle overlaps with ANY illegal/protected region
        /// that should actually block zone creation.
        /// </summary>
        private static bool IntersectsAnyIllegalRegion(Rectangle2D candidate, List<StaticRegion> illegalRegions)
        {
            foreach (StaticRegion illegal in illegalRegions)
            {
                if (illegal == null || illegal.Coords == null)
                    continue;

                foreach (Rectangle3D r3d in illegal.Coords)
                {
                    Rectangle2D illegalRect = new Rectangle2D(r3d.Start, r3d.End);

                    if (candidate.Intersects(illegalRect))
                    {
                        // New logic: If the illegal region is completely inside the candidate (conquerable area),
                        // we allow it (main region wins)
                        if (candidate.Contains(illegalRect))
                            continue;   // ignore this illegal sub-region

                        return true;    // real blocking overlap
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Spawns guardians. Returns number of webs created.
        /// </summary>
        private static int SpawnGuardians()
        {
            if (_districts.Count == 0)
                return 0;

            int totalWebs = 0;

            foreach (var district in _districts.Values)
            {
                foreach (var zone in district.Zones)
                {
                    var web = ZoneGuardianSpawner.SpawnInZone(zone);
                    if (web != null)
                        totalWebs++;
                }
            }

            return totalWebs;
        }

        #endregion

        internal static TerritoryDistrict? GetDistrictFromRegion(StaticRegion region)
        {
            if (region == null || string.IsNullOrWhiteSpace(region.Name))
                return null;

            _districts.TryGetValue(region.Name, out TerritoryDistrict? district);
            return district;
        }
        /// <summary>
        /// Finds a district by name.
        /// Returns null if the district does not exist.
        /// </summary>
        public static TerritoryDistrict? FindDistrict(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            _districts.TryGetValue(name, out TerritoryDistrict? district);

            return district;
        }

        public static TerritoryDistrict? FindDistrict(Mobile m)
        {
            Region r = m.Region;
            if (r != null)
                return FindDistrict(r.Name);
            else
                return null;
        }

        private static void EnsureDataReady()
        {
            // TerritoryConfig is already loaded unconditionally in OnWorldLoad()
            // We only ensure the heavy data (Orc table) is loaded when needed
            if (!OrcInvasionTable.IsLoaded)
                OrcInvasionTable.Load();
        }
        /// <summary>
        /// Returns true if the Territory system is currently configured (districts or webs exist).
        /// This is the core check used by preflight to prevent duplicate setup.
        /// No messaging is performed.
        /// </summary>
        public static bool AreTerritoriesConfigured()
        {
            return _districts.Count > 0 || _activeWebs.Count > 0;
        }
        internal static void UpdateTownshipForPlayer(Mobile player, double points)
        {
            if (TownshipStone.GetPlayerTownship(player) is TownshipStone ts)
            {
                ts.LBTaxSubsidy += (int)points;
                ts.LBFameSubsidy += (int)points;
            }
        }
    }
}