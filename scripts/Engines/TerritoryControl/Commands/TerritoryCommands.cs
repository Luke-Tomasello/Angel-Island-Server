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

/* Scripts\Engines\TerritoryControl\Commands\TerritoryCommands.cs
 * CHANGELOG:
 *   11/4/2026, Adam:
 *      - [DistrictInfo: an unconquered district's MiniBosses line notes the conquest bonus Warlord (+1, not counted in
 *        MiniBossesSpawned; see TerritoryDistrict).
 *      - [DistrictInfo shows the current campaign's ID (the campaign journal's handle; TerritoryJournal).
 *      - [SetupTerritories begins every district's first campaign (journaled) once its webs are spawned.
 *   10/27/2026, Adam:
 *      - [DistrictInfo: zones without a GuardianWeb are labeled as not required for conquest (they used to read ACTIVE
 *        and looked like blockers); "active" counts web-bearing zones only; stale zones (conquered while their web is
 *        not, left by a recapture before the 10/27 fix; PatchV257 repairs them) are counted and labeled.
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See TerritorySystem for an architectural overview
 */

using Server.Diagnostics;
using Server.Engines.TerritoryControl.Events;
using Server.Gumps;
using Server.Mobiles;
using Server.Regions;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TextCopy;

namespace Server.Engines.TerritoryControl
{
    public static partial class TerritorySystem
    {
        public static void Initialize()
        {
            // configuration
            CommandSystem.Register("TerritoryConfig", AccessLevel.Administrator, TerritoryConfig_OnCommand);

            // lifecycle
            CommandSystem.Register("SetupTerritories", AccessLevel.Administrator, SetupTerritories_OnCommand);
            CommandSystem.Register("ListDistricts", AccessLevel.GameMaster, ListDistricts_OnCommand);
            CommandSystem.Register("Listzones", AccessLevel.GameMaster, ListZones_OnCommand);
            CommandSystem.Register("ZoneIDInfo", AccessLevel.GameMaster, ZoneIDInfo_OnCommand);

            CommandSystem.Register("TearDownTerritories", AccessLevel.Administrator, TearDownTerritories_OnCommand);

            // maintenence
            CommandSystem.Register("AddDistrict", AccessLevel.Administrator, AddDistrict_OnCommand);
            CommandSystem.Register("RemoveDistrict", AccessLevel.Administrator, RemoveDistrict_OnCommand);

            //CommandSystem.Register("GenerateDistricts", AccessLevel.Administrator, GenerateDistricts_OnCommand);
            //CommandSystem.Register("SpawnGuardians", AccessLevel.Administrator, SpawnGuardians_OnCommand);
            //CommandSystem.Register("DespawnGuardians", AccessLevel.Administrator, DespawnGuardians_OnCommand);
            //CommandSystem.Register("WipeDistricts", AccessLevel.Administrator, WipeDistricts_OnCommand);

            // testing
            CommandSystem.Register("CreateTestZoneWeb", AccessLevel.Administrator, CreateTestZoneWeb_OnCommand);

            CommandSystem.Register("ZoneInfo", AccessLevel.GameMaster, ZoneInfo_OnCommand);

            CommandSystem.Register("DistrictInfo", AccessLevel.GameMaster, DistrictInfo_OnCommand);

            // guard management testing - these will be used in setup/teardown but are also exposed separately for testing and manual control
            CommandSystem.Register("DitchGuards", AccessLevel.Administrator, DitchGuards_OnCommand);
            CommandSystem.Register("RestoreGuards", AccessLevel.Administrator, RestoreGuards_OnCommand);

            if (TerritoryConfig.Enabled)
            {
                Console.WriteLine("[TerritoryControl] System is ENABLED and will auto-load on world load.");
            }
        }

        #region Command Handlers (User Messaging)
        // configuration
        [Usage("TerritoryConfig")]
        [Description("Opens the TerritoryControl configuration properties.")]
        private static void TerritoryConfig_OnCommand(CommandEventArgs e)
        {
            // don't check for enabled here as we need this command to enable/disable

            e.Mobile.SendMessage("Target yourself to edit TerritoryConfig properties.");
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, (from, target) =>
            {
                if (target == from)
                    from.SendGump(new PropertiesGump(from, new TerritoryConfigTarget()));
                else
                    from.SendMessage("You must target yourself.");
            });
        }

        // lifecycle
        [Usage("SetupTerritories")]
        [Description("Full setup: Ditch guards, generate districts, and spawn guardians.")]
        private static void SetupTerritories_OnCommand(CommandEventArgs e)
        {
            SetupTerritories(e.Mobile);
        }

        [Usage("TearDownTerritories")]
        [Description("Full teardown: Despawn guardians, wipe districts, then restore guards.")]
        private static void TearDownTerritories_OnCommand(CommandEventArgs e)
        {
            TearDownTerritories(e.Mobile);
        }

        [Usage("AddDistrict <regionName>")]
        [Description("Adds a specific region as a TerritoryDistrict and spawns guardians (overrides normal rules).")]
        private static void AddDistrict_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length == 0)
            {
                e.Mobile.SendMessage("Usage: [AddDistrict <regionName>");
                e.Mobile.SendMessage("Example: [AddDistrict Jhelom");
                e.Mobile.SendMessage("Example: [AddDistrict \"Wind Dungeon\"");
                return;
            }

            string regionName = string.Join(" ", e.Arguments).Trim();

            if (AddDistrict(regionName, e.Mobile?.Map, out string failReason))
            {
                e.Mobile.SendMessage(0x40, $"Successfully added district '{regionName}'.");
            }
            else
            {
                e.Mobile.SendMessage(0x22, failReason);
            }
        }
        public static bool AddDistrict(string regionName, Map? map, out string failReason)
        {
            failReason = null;

            if (string.IsNullOrWhiteSpace(regionName))
            {
                failReason = "Region name cannot be empty.";
                return false;
            }

            if (_districts.ContainsKey(regionName))
            {
                failReason = $"A district named '{regionName}' already exists.";
                return false;
            }

            map ??= Map.Felucca;

            // Find region
            var targetRegion = Region.Regions.OfType<StaticRegion>()
                .FirstOrDefault(r => r.Map == map && Insensitive.Equals(r.Name, regionName));

            if (targetRegion == null)
            {
                failReason = $"No region named '{regionName}' found on map {map}.";
                return false;
            }

            // Create district
            var district = new TerritoryDistrict(targetRegion.Name ?? regionName, map)
            {
                WasOriginallyGuarded = targetRegion.GetFlag(RegionFlag.EnableGuards)
            };


            var illegalRegions = TerritoryRegions.GetIllegalRegions(map);
            SliceRegionIntoVerticalZones(targetRegion, district, illegalRegions, preferredWidth: 13);

            if (district.Zones.Count == 0)
            {
                failReason = $"Failed to create any zones for '{regionName}'.";
                return false;
            }

            // Make it conquerable
            targetRegion.IsConquerable = true;
            targetRegion.EnableGuards = false;

            int guardsDitched = TerritoryGuardManager.DitchGuardsForRegion(targetRegion);

            _districts[district.Name] = district;
            Initialized = true;

            // Spawn guardians
            int websSpawned = 0;
            foreach (var zone in district.Zones)
            {
                var web = ZoneGuardianSpawner.SpawnInZone(zone);
                if (web != null)
                {
                    RegisterWeb(web);
                    websSpawned++;
                }
            }

            Console.WriteLine($"[TerritoryControl] Added district '{district.Name}' with {district.Zones.Count} zones and {websSpawned} webs.");

            return true;
        }
        [Usage("RemoveDistrict <name>")]
        [Description("Removes a single TerritoryDistrict and its guardians, restoring the region's original guard state.")]
        private static void RemoveDistrict_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length == 0)
            {
                e.Mobile.SendMessage("Usage: [RemoveDistrict <name>");
                e.Mobile.SendMessage("Example: [RemoveDistrict Jhelom");
                return;
            }

            string districtName = string.Join(" ", e.Arguments).Trim();

            if (!_districts.TryGetValue(districtName, out TerritoryDistrict? district))
            {
                e.Mobile.SendMessage($"District '{districtName}' not found.");
                return;
            }

            int despawned = 0;

            foreach (var zone in district.Zones)
            {
                if (zone.Web != null)
                {
                    despawned += zone.Web.Spawners.Count;
                    zone.Web.DeleteAllSpawners();
                }
            }

            // Remove this district's webs from the active collection.
            _activeWebs.RemoveAll(w => w.Zone?.District == district);

            // Restore the underlying region to its pre-territory state.
            var region = Region.Regions.OfType<StaticRegion>()
                .FirstOrDefault(r =>
                    r.Map == district.Map &&
                    Insensitive.Equals(r.Name, district.Name));

            if (region != null)
            {
                TerritoryGuardManager.RestoreGuardsForRegion(region);

                region.EnableGuards = district.WasOriginallyGuarded;
                region.IsConquerable = false;
            }

            // The district is no longer needed once its region has been restored.
            _districts.Remove(districtName);

            e.Mobile.SendMessage(
                $"District '{districtName}' removed and original guard state restored.");

            e.Mobile.SendMessage($"Despawned {despawned} guardian spawners.");
        }

        /// <summary>
        /// Public interface for setting up the Territory system.
        /// Sends output to Mobile (if provided) and always logs to console.
        /// </summary>
        public static void SetupTerritories(Mobile? m = null)
        {
            if (!RunPreflight(m))
                return;

            Map map = m?.Map ?? Map.Felucca;

            // Establish conquerable regions without changing their guard state.
            TerritoryGuardManager.MarkConquerable(map);

            // Check for conquerable regions
            var conquerableRegions = TerritoryRegions.GetConquerableRegions(map);

            if (conquerableRegions.Count == 0)
            {
                SendMessage(m, 0x22, "ERROR: No conquerable regions found!");
                SendMessage(m, "Use [props] on StaticRegions and set IsConquerable = true.");
                SendMessage(m, "Only guarded regions with IsConquerable enabled will be invaded.");
                return;
            }

            Console.WriteLine($"[Territory Setup] Preflight passed. {conquerableRegions.Count} conquerable regions found.");

            TerritoryZone.ResetZoneSerial(); // start counting zones at 1
            OrcSpawnGenerator.ResetClanSerial();

            int districts = GenerateDistricts(map);

            // Districts now contain the original guard state.
            var guardStats = TerritoryGuardManager.InitializeConquerableState(map);

            int webs = SpawnGuardians();
            int spawners = _activeWebs.Sum(w => w.Spawners.Count);

            // every district's first campaign begins now (journaled, with its webs)
            foreach (TerritoryDistrict district in _districts.Values)
                district.BeginCampaign("the territory was set up ([SetupTerritories])");

            SendMessage(m, $"Ditched guards in {guardStats.RegionsAffected} regions ({guardStats.GuardsDitched} guards).");
            SendMessage(m, $"Generated {districts} districts.");
            SendMessage(m, $"Spawned {webs} guardian webs ({spawners} total spawners).");
            SendMessage(m, "Territory system fully set up.");
        }

        /// <summary>
        /// Public interface for tearing down the Territory system.
        /// Sends output to Mobile (if provided) and always logs to console.
        /// </summary>
        public static void TearDownTerritories(Mobile? m = null)
        {
            #region Sanity Checking
            if (!TerritoryConfig.Enabled)
            {
                SendMessage(m, "TerritoryControl system is currently disabled.");
                return;
            }

            bool hasDistricts = _districts.Count > 0;
            bool hasWebs = _activeWebs.Count > 0;
            bool hasSave = File.Exists(SavePath);

            if (!hasDistricts && !hasWebs && !hasSave)
            {
                SendMessage(m, "There are no territories to tear down.");
                SendMessage(m, "Run [SetupTerritories] first.");
                return;
            }
            #endregion Sanity Checking

            int despawned = 0;
            foreach (var web in _activeWebs)
            {
                despawned += web.Spawners.Count;
                web.DeleteAllSpawners();
            }

            int districtsCleared = _districts.Count;

            // Restore regional guard state while the districts still exist.
            var guardStats = TerritoryGuardManager.ShutdownConquerableState();

            _activeWebs.Clear();
            _districts.Clear();
            Initialized = false;

            SendMessage(m, $"Despawned {despawned} guardian spawners.");
            SendMessage(m, $"Cleared {districtsCleared} districts.");
            SendMessage(m, $"Restored {guardStats.GuardsRestored} guards in {guardStats.RegionsAffected} regions.");
            SendMessage(m, "Territory system fully torn down.");
        }

        /// <summary>
        /// Helper to send a message to both a Mobile (if provided) and the console.
        /// </summary>
        private static void SendMessage(Mobile? m, string message)
        {
            m?.SendMessage(message);
            Console.WriteLine(message);
        }

        private static void SendMessage(Mobile? m, int hue, string message)
        {
            m?.SendMessage(hue, message);
            Console.WriteLine(message);
        }

        [Usage("ListDistricts")]
        [Description("Lists all loaded Territory Districts.")]
        private static void ListDistricts_OnCommand(CommandEventArgs e)
        {
            if (!TerritoryConfig.Enabled)
            {
                e.Mobile.SendMessage("TerritoryControl system is currently disabled.");
                return;
            }

            if (_districts.Count == 0)
            {
                e.Mobile.SendMessage("No districts have been generated yet.");
                e.Mobile.SendMessage("Use [SetupTerritories] to create them.");
                return;
            }

            LogHelper logger = new LogHelper("TCDistricts.log", true);

            try
            {
                // Header
                e.Mobile.SendMessage(0x40, $"=== Territory Districts Report ({_districts.Count} total) ===");

                foreach (var district in _districts.Values)
                {
                    string line = district.ToString();   // Assuming this already gives a nice summary
                    e.Mobile.SendMessage(line);
                    logger.Log(line);
                }

                // Footer
                string footer = $"Total Districts: {_districts.Count} | Logged to TCDistricts.log";
                e.Mobile.SendMessage(0x40, footer);
                logger.Log(footer);
            }
            finally
            {
                logger.Finish();
            }
        }

        [Usage("ListZones")]
        [Description("Lists all Territory Zones sorted by ZoneID.")]
        private static void ListZones_OnCommand(CommandEventArgs e)
        {
            if (!TerritoryConfig.Enabled)
            {
                e.Mobile.SendMessage("TerritoryControl system is currently disabled.");
                return;
            }

            if (_districts.Count == 0)
            {
                e.Mobile.SendMessage("No districts have been generated yet.");
                e.Mobile.SendMessage("Use [SetupTerritories] to create them.");
                return;
            }

            // Collect and sort all zones by ZoneID
            var allZones = new List<TerritoryZone>();

            foreach (var district in _districts.Values)
                allZones.AddRange(district.Zones);

            allZones.Sort((a, b) => a.ZoneID.CompareTo(b.ZoneID));

            // Logging
            LogHelper logger = new LogHelper("TCZones.log", true);

            try
            {
                // Header
                e.Mobile.SendMessage(0x40, $"=== Territory Zones Report ({allZones.Count} total) ===");

                foreach (var zone in allZones)
                {
                    string districtName = zone.District?.Name ?? "Unknown";
                    string line = $"ID {zone.ZoneID:D4} | {zone.Bounds} | {districtName}";

                    e.Mobile.SendMessage(line);
                    logger.Log(line);
                }

                // Footer
                string footer = $"Total Zones: {allZones.Count} | Logged to TCZones.log";
                e.Mobile.SendMessage(0x40, footer);
                logger.Log(footer);
            }
            finally
            {
                logger.Finish();
            }
        }

        [Usage("ZoneIDInfo")]
        [Description("Shows live zone count, highest assigned ZoneID, and the next ZoneID to be issued.")]
        private static void ZoneIDInfo_OnCommand(CommandEventArgs e)
        {
            if (!TerritoryConfig.Enabled)
            {
                e.Mobile.SendMessage("TerritoryControl system is currently disabled.");
                return;
            }

            // Derive from the live district/zone collection, not from counts.
            var zoneIDs = _districts.Values
                .SelectMany(d => d.Zones)
                .Where(z => z != null)
                .Select(z => z.ZoneID)
                .ToList();

            uint nextID = TerritoryZone.ZoneSerial;

            e.Mobile.SendMessage(0x40, "=== Territory ZoneID Info ===");
            e.Mobile.SendMessage($"Zones: {zoneIDs.Count}");

            if (zoneIDs.Count == 0)
            {
                e.Mobile.SendMessage("Highest ZoneID: (none)");
                e.Mobile.SendMessage($"Next ZoneID: {nextID}");
                return;
            }

            uint highestID = zoneIDs.Max();
            int duplicates = zoneIDs.Count - zoneIDs.Distinct().Count();

            e.Mobile.SendMessage($"Highest ZoneID: {highestID}");
            e.Mobile.SendMessage($"Next ZoneID: {nextID}");

            if (nextID <= highestID)
                e.Mobile.SendMessage(0x22, "WARNING: Next ZoneID does not exceed the highest live ZoneID (collision risk).");

            if (duplicates > 0)
                e.Mobile.SendMessage(0x22, $"WARNING: {duplicates} duplicate ZoneID(s) among live zones.");
        }

        #region Testing
        [Usage("CreateTestZoneWeb")]
        [Description("Creates a small test zone with a full GuardianWeb for testing.")]
        private static void CreateTestZoneWeb_OnCommand(CommandEventArgs e)
        {
            if (!RunPreflight(e.Mobile))
                return;

            Point3D center = e.Mobile.Location;
            Rectangle2D bounds = new Rectangle2D(center.X - 20, center.Y - 20, 40, 40);
            Map map = e.Mobile.Map;

            // Create district first
            var district = new TerritoryDistrict("Test District", map);

            // Create zone with required Map and District
            var zone = new TerritoryZone(bounds, map, district, "Test Zone");

            district.AddZone(zone);

            System.Diagnostics.Debug.Assert(!_districts.ContainsKey(district.Name));
            _districts[district.Name] = district;

            // Spawn the web
            var web = ZoneGuardianSpawner.SpawnInZone(zone);

            if (web != null)
            {
                RegisterWeb(web);
                Initialized = true;

                e.Mobile.SendMessage($"Created test zone with {web.Spawners.Count} guardian spawners.");
                e.Mobile.SendMessage("Use [TearDownTerritories] to clean up.");
            }
            else
            {
                e.Mobile.SendMessage("Failed to create test web.");
            }
        }

        // testing - guard management
        [Usage("DitchGuards")]
        [Description("Disables guards in all conquerable guarded regions and moves them to storage.")]
        private static void DitchGuards_OnCommand(CommandEventArgs e)
        {
            var stats = TerritoryGuardManager.InitializeConquerableState(e.Mobile.Map);

            e.Mobile.SendMessage($"Ditched guards in {stats.RegionsAffected} regions.");
            e.Mobile.SendMessage($"→ {stats.GuardsDitched} guards moved to storage or deleted.");
        }

        [Usage("RestoreGuards")]
        [Description("Restores guards from storage back into their regions.")]
        private static void RestoreGuards_OnCommand(CommandEventArgs e)
        {
            var stats = TerritoryGuardManager.ShutdownConquerableState();

            e.Mobile.SendMessage($"Restored {stats.GuardsRestored} guards across {stats.RegionsAffected} regions.");
        }
        #endregion Testing

        [Usage("DistrictInfo [-flash] [-bounds] [-spawners] [-zones] [-copy] [-districts]")]
        [Description("Shows information about the TerritoryDistrict at your current location. Use -zones to show detailed zone list.")]
        private static void DistrictInfo_OnCommand(CommandEventArgs e)
        {
            if (!TerritoryConfig.Enabled)
            {
                e.Mobile.SendMessage("TerritoryControl system is currently disabled.");
                return;
            }

            Mobile m = e.Mobile;
            Point3D loc = m.Location;
            Map map = m.Map;
            TerritoryZone? currentZone = TerritoryZone.FindZoneAt(loc, map);
            TerritoryDistrict? district = currentZone?.District;

            if (district == null)
            {
                m.SendMessage("No TerritoryDistrict found at your current location.");
                return;
            }

            bool showZones = e.Arguments.Any(a => Insensitive.Equals(a, "-zones"));
            bool flash = e.Arguments.Any(a => Insensitive.Equals(a, "-flash"));
            bool bounds = e.Arguments.Any(a => Insensitive.Equals(a, "-bounds"));
            bool spawners = e.Arguments.Any(a => Insensitive.Equals(a, "-spawners"));
            bool copy = e.Arguments.Any(a => Insensitive.Equals(a, "-copy"));
            bool districts = e.Arguments.Any(a => Insensitive.Equals(a, "-districts"));

            var output = new List<string>();

            int zoneCount = district.Zones.Count;
            int zonesConquered = district.Zones.Count(z => z?.IsConquered == true);
            int zonesWebless = district.Zones.Count(z => z != null && z.Web == null);           // not required for conquest
            int zonesActive = district.Zones.Count(z => z?.Web != null && !z.IsConquered);      // web-bearing, still to win
            int zonesStale = district.Zones.Count(z => z?.Web != null && z.IsConquered && !z.Web.IsConquered);

            int webCount = district.Zones.Count(z => z?.Web != null);
            int websConquered = district.Zones.Count(z => z?.Web?.IsConquered == true);
            int websComplete = district.Zones.Count(z => z?.Web?.IsComplete == true);

            var allSpawners = district.Zones
                .Where(z => z?.Web != null)
                .SelectMany(z => z.Web.Spawners)
                .Where(s => s != null && !s.Deleted)
                .ToList();

            int spawnerCount = allSpawners.Count;
            int spawnersCleared = allSpawners.Count(s => s.Deactivated);
            int spawnersActive = spawnerCount - spawnersCleared;

            int totalKills = district.Zones.Where(z => z?.Web != null).Sum(z => z.Web.TotalKills);
            int totalKillsNeeded = district.Zones.Where(z => z?.Web != null).Sum(z => z.Web.WebKillsNeeded);

            TerritoryZone lastConqueredZone = district.Zones
                .Where(z => z != null && z.IsConquered)
                .OrderByDescending(z => z.ConqueredAt)
                .FirstOrDefault();

            output.Add($"=== District: {district.Name} ===");
            output.Add($"Map: {district.Map}");
            output.Add($"District Conquered: {district.IsConquered}");
            output.Add($"Campaign: {district.CampaignID}" + (district.CampaignObserved ? " (first seen then; it began before the journal)" : "") +
                $" (journal: Logs/{TerritoryJournal.FileName})");
            output.Add($"Zones: {zonesConquered} conquered, {zonesActive} active, {zonesWebless} without a web (not required), {zoneCount} total");
            if (zonesStale > 0)
                output.Add($"Stale zones: {zonesStale} conquered while their web is not (left by a recapture before the 10/27 fix; PatchV257)");
            if (lastConqueredZone != null)
                output.Add(
                    $"Last Zone Cleared: {lastConqueredZone.Name} [ID: {lastConqueredZone.ZoneID}] at {lastConqueredZone.ConqueredAt.ToLocalTime():g}");
            else
                output.Add("Last Zone Cleared: none");
            output.Add($"Webs: {websConquered} conquered, {websComplete} complete, {webCount} total");
            output.Add($"Spawners: {spawnersCleared} deactivated, {spawnersActive} active, {spawnerCount} total");
            output.Add($"Progress: {totalKills} / {totalKillsNeeded} kills");
            output.Add($"Stable MiniBoss Credit: {district.StableMiniBossKills} kills");
            output.Add($"MiniBoss Interval: {TerritoryConfig.MiniBossKillInterval} kills");
            output.Add($"MiniBosses: {district.MiniBossesSpawned} spawned, {district.EarnedMiniBosses} earned, {district.MaxMiniBosses} max" +
                (district.IsConquered ? "" : " (+1 conquest bonus when the district is liberated)"));

            if (showZones)
            {
                output.Add("");
                output.Add("--- Zones ---");
                foreach (TerritoryZone zone in district.Zones.OrderBy(z => z.ZoneID))
                {
                    if (zone == null) continue;

                    GuardianWeb web = zone.Web;
                    string zoneStatus = web == null ? "[NO WEB]" :
                                        zone.IsConquered && !web.IsConquered ? "[CONQUERED, STALE]" :
                                        zone.IsConquered ? "[CONQUERED]" : "[ACTIVE]";
                    string webStatus = web == null ? "not required for conquest" :
                                       web.IsConquered ? "Web conquered" :
                                       web.IsComplete ? "Web complete" :
                                       $"Web active {web.TotalKills}/{web.WebKillsNeeded}";

                    output.Add($"- Zone {zone.Name} [ID: {zone.ZoneID}] {zoneStatus}: {webStatus}");
                }
            }

            foreach (string line in output)
                m.SendMessage(line);

            // Handle visual/debug switches
            if (flash)
            {
                foreach (TerritoryZone zone in district.Zones)
                    if (zone != null)
                        Server.Gumps.EditAreaGump.FlashArea(m, zone.Bounds, zone.Map);

                m.SendMessage("District zone bounds flashed on your screen.");
            }

            if (m is PlayerMobile pm)
            {
                if (bounds)
                {
                    pm.JumpList = new ArrayList();
                    pm.JumpIndex = 0;
                    foreach (TerritoryZone zone in district.Zones)
                    {
                        if (zone == null) continue;
                        pm.JumpList.Add(new Point3D(zone.Bounds.X, zone.Bounds.Y, loc.Z));
                        pm.JumpList.Add(new Point3D(zone.Bounds.X + zone.Bounds.Width, zone.Bounds.Y, loc.Z));
                        pm.JumpList.Add(new Point3D(zone.Bounds.X + zone.Bounds.Width, zone.Bounds.Y + zone.Bounds.Height, loc.Z));
                        pm.JumpList.Add(new Point3D(zone.Bounds.X, zone.Bounds.Y + zone.Bounds.Height, loc.Z));
                    }
                    m.SendMessage($"Added {pm.JumpList.Count} district zone corners to your jump list.");
                }

                if (spawners)
                {
                    pm.JumpList = new ArrayList();
                    pm.JumpIndex = 0;

                    foreach (ContestedSpawner spawner in allSpawners
                                 .Where(s => s != null && !s.Deleted)
                                 .OrderBy(s => s.Deactivated))
                    {
                        pm.JumpList.Add(spawner);
                    }

                    m.SendMessage($"Added {pm.JumpList.Count} district spawners to your jump list.");
                }

                // ==================== NEW: -districts ====================
                if (districts)
                {
                    pm.JumpList = new ArrayList();
                    pm.JumpIndex = 0;
                    int count = 0;

                    foreach (TerritoryDistrict d in TerritorySystem.GetDistricts())
                    {
                        if (d == null || d.Map != map)
                            continue;

                        bool added = false;

                        // Primary method: Use the Region (recommended)
                        Region? r = Region.FindByName(d.Name, d.Map);

                        if (r != null && r.Coords != null && r.Coords.Count > 0)
                        {
                            foreach (Rectangle3D rect in r.Coords)
                            {
                                int centerX = rect.Start.X + (rect.Width / 2);
                                int centerY = rect.Start.Y + (rect.Height / 2);

                                // Get proper ground Z
                                Point2D node = new Point2D(centerX, centerY);
                                int centerZ = Utility.GetAverageZ(map, node.X, node.Y);   // Use current map

                                Point3D center = new Point3D(centerX, centerY, centerZ);

                                pm.JumpList.Add(center);
                                count++;
                                added = true;
                            }
                        }

                        // Fallback: If no region found, try district's own Rectangle3D(s)
                        if (!added)
                        {
                            // Try common property names - adjust if your TerritoryDistrict uses different names
                            if (d is { /* Area, Rect, RegionRect, etc. */ })
                            {
                                // Example if district has a Rectangle3D property called "Area":
                                // Rectangle3D rect = d.Area;
                                // if (!rect.IsEmpty) { ... }
                            }
                            else
                            {
                                // Ultimate fallback - you may need to customize this part
                                // For now we skip or use a simple point if you have a central location
                                m.SendMessage($"Warning: District '{d.Name}' has no region or usable area data.");
                            }
                        }
                    }

                    if (count > 0)
                        m.SendMessage($"Added {count} district center points to your jump list.");
                    else
                        m.SendMessage("No districts found to add to jump list.");
                }
            }

            if (copy)
            {
                string clipboardText = string.Join(Environment.NewLine, output);
                Clipboard clipboard = new Clipboard();
                clipboard.SetText(clipboardText);
                m.SendMessage("District information copied to clipboard.");
            }
        }

        [Usage("ZoneInfo [-flash] [-bounds] [-spawners] [-copy]")]
        [Description("Shows detailed information about the TerritoryZone at your current location. Use -flash to highlight bounds, -jump to add corners to your jump list, -spawners to add spawners to your jump list, -copy to copy output to clipboard.")]
        private static void ZoneInfo_OnCommand(CommandEventArgs e)
        {
            if (!TerritoryConfig.Enabled)
            {
                e.Mobile.SendMessage("TerritoryControl system is currently disabled.");
                return;
            }

            Mobile m = e.Mobile;
            Point3D loc = m.Location;
            Map map = m.Map;

            TerritoryZone? zone = TerritoryZone.FindZoneAt(loc, map);
            if (zone == null)
            {
                m.SendMessage("No TerritoryZone found at your current location.");
                return;
            }

            // Collect all output lines
            var output = new List<string>();

            output.Add($"=== Zone: {zone.Name} [{zone.ZoneID}] ===");
            output.Add($"Map: {zone.Map}");
            output.Add($"Bounds: {zone.Bounds}");
            output.Add($"District: {zone.District?.Name ?? "None"}");
            output.Add($"Zone Conquered: {zone.IsConquered}");

            if (zone.Web != null)
            {
                var web = zone.Web;
                string status = web.IsComplete ? "[CLEARED]" : "[ACTIVE]";
                var clans = web.Spawners
                    .Where(s => s != null)
                    .Select(s => s.Clan)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .OrderBy(c => c);

                string clanList = string.Join(", ", clans);

                output.Add(
                    $"Web Status: {status} ({web.Spawners.Count} spawners: {clanList})");
                //output.Add($"Web Status: {status} ({web.Spawners.Count} spawners)");
                output.Add($"Web Conquered: {web.IsConquered}");
                output.Add($"Progress: {web.TotalKills} / {web.WebKillsNeeded} kills");
            }
            else
            {
                output.Add("Web Status: No web assigned");
            }

            output.Add($"District Conquered: {zone.District?.IsConquered ?? false}");
            output.Add($"Anchor Spawners in zone: {ZoneGuardianSpawner.GetValidAnchorSpawners(zone).Count}");

            // Parse command switches
            bool flash = false;
            bool bounds = false;
            bool spawners = false;
            bool copy = false;

            foreach (string arg in e.Arguments)
            {
                if (Insensitive.Equals(arg, "-flash")) flash = true;
                else if (Insensitive.Equals(arg, "-bounds")) bounds = true;
                else if (Insensitive.Equals(arg, "-spawners")) spawners = true;
                else if (Insensitive.Equals(arg, "-copy")) copy = true;
            }

            // Send all queued messages
            foreach (string line in output)
                m.SendMessage(line);

            // Flash bounds
            if (flash)
            {
                Server.Gumps.EditAreaGump.FlashArea(m, zone.Bounds, zone.Map);
                m.SendMessage("Zone bounds flashed on your screen.");
            }

            if (m is PlayerMobile pm)
            {
                // Add corners to jump list
                if (bounds)
                {
                    pm.JumpList = new ArrayList();
                    pm.JumpIndex = 0;

                    pm.JumpList.Add(new Point3D(zone.Bounds.X, zone.Bounds.Y, loc.Z));
                    pm.JumpList.Add(new Point3D(zone.Bounds.X + zone.Bounds.Width, zone.Bounds.Y, loc.Z));
                    pm.JumpList.Add(new Point3D(zone.Bounds.X + zone.Bounds.Width, zone.Bounds.Y + zone.Bounds.Height,
                        loc.Z));
                    pm.JumpList.Add(new Point3D(zone.Bounds.X, zone.Bounds.Y + zone.Bounds.Height, loc.Z));

                    m.SendMessage($"Added {pm.JumpList.Count} zone corners to your jump list.");
                }

                if (spawners)
                {
                    if (zone != null && zone.Web != null && zone.Web.Spawners != null)
                    {
                        pm.JumpList = new ArrayList();
                        pm.JumpIndex = 0;

                        foreach (var spawner in zone.Web.Spawners
                                     .Where(s => s != null && !s.Deleted)
                                     .OrderBy(s => s.Deactivated))
                        {
                            pm.JumpList.Add(spawner);
                        }

                        m.SendMessage($"Added {pm.JumpList.Count} spawners to your jump list.");
                    }
                }
            }

            // Copy to clipboard
            if (copy)
            {
                string clipboardText = string.Join(Environment.NewLine, output);

                Clipboard clipboard = new Clipboard();
                clipboard.SetText(clipboardText);

                m.SendMessage("Zone information copied to clipboard.");
            }
        }
        #endregion Command Handlers (User Messaging)
        /// <summary>
        /// Runs all preflight/sanity checks before setup commands.
        /// Returns true if everything is ready, false if any check fails.
        /// </summary>
        /// <summary>
        /// Runs preflight checks before setting up territories.
        /// Sends output to the provided Mobile (if not null) and always logs to console.
        /// </summary>
        private static bool RunPreflight(Mobile? m)
        {
            if (!TerritoryConfig.Enabled)
            {
                SendMessage(m, "TerritoryControl system is currently disabled.");
                return false;
            }

            if (_districts.Count > 0 || _activeWebs.Count > 0)
            {
                SendMessage(m, "Territories are already set up.");
                SendMessage(m, "Run [TearDownTerritories] first before running Setup again.");
                return false;
            }

            // Ensure required data is loaded
            EnsureDataReady();

            // Check Orc Invasion Table
            if (!OrcInvasionTable.IsLoaded || OrcInvasionTable.CreatureCount == 0)
            {
                SendMessage(m, 0x22, "ERROR: Orc Invasion Table is missing or empty!");
                SendMessage(m, "Make sure Data/Seasons/OrcInvasionTable.json exists and is valid.");
                return false;
            }

            // Check TerritoryConfig
            if (!TerritoryConfig.IsLoaded)
            {
                SendMessage(m, 0x22, "ERROR: TerritoryConfig failed to load!");
                SendMessage(m, "This is unexpected. Check server logs for details.");
                return false;
            }

            return true;
        }
    }
}
