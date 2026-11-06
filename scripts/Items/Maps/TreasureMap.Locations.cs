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

/* Scripts\Items\Maps\TreasureMap.Locations.cs
 * ChangeLog
 *  10/21/2026, Adam
 *  ValidateLocation accepts only clearly natural, open ground (NotNaturalGround, replaces IsRoofed): a natural land
 *  tile (forest, grass, snow, jungle, dirt, furrows, sand, swamp), no man-made surface at standing level, and nothing
 *  overhead (a Roof static at or above the standing Z, or a Surface/Wall/Impassable/Bridge static 10+ above it).
 *  Questionable tiles, unnamed land included, are rejected.
 *  10/21/2026, Adam
 *  Generated locations are validated where consumers use them: walkable, non-water land at GetAverageZ
 *  (ZoneGuardianSpawner.IsWalkableLand) plus the existing treasure-map restrictions.
 *  - A spawner whose anchor fails is rejected entirely (no anchor, no nearby candidates). Spawners standing
 *    over water, on docks or on furniture had been added as locations without any terrain check.
 *  - Nearby candidates are validated inside GetSpawnPosition (validator) before its clear-path test; a
 *    candidate found only at another Z (dock plank, basement, rooftop) is rejected, since Point2D cannot keep it.
 *  - GetSpawnPosition's give-up result (the anchor itself) is never counted as a candidate.
 *  - Generate() and ParseLocations() split out of LoadLocations; output format unchanged.
 *  - SpawnerQuery is asked for SpawnContent.Any: content classification depends on live spawned objects, so
 *    spawners with nothing spawned (e.g. Running == false) were never selected. All Felucca spawners outside
 *    Town and Guarded regions are now anchors, validated identically (aquatic spawners fail on terrain).
 *  - IsValidTreasureLoc (anchors and candidates): the Moongates region is excluded by name, whatever its guard
 *    state; Moongate Houseblockers and outdoor dungeon entrances stay eligible.
 *  - TEMPORARY (TODO, remove when fixed): the Felucca Jhelom town area is excluded directly, working around a
 *    Region.Find anomaly that reports some of it as non-town.
 *  - Regenerate(caller) (PatchV253): generates, writes Angel Island Treasure.json.new, reads it back, swaps it in
 *    with File.Replace (old file kept as .pre-<caller>), then updates TreasureMap.Locations. A failed or implausible
 *    run (no locations, or fewer than half of those loaded) leaves the file and cache unchanged. TreasureLocations.log.
 *  - [JumpTreasureMapLocations and [GenTreasureMapLocations load the jump list through OwnerTools.LoadTreasureJump
 *    (shared with [LoadJump treasure): random order, each location once, Felucca WorldLocations at GetAverageZ.
 *  - Generate() prints the one progress message ("Generating Treasure Map Locations. This may take a while...") for
 *    every generation: startup with no database, [GenTreasureMapLocations, and PatchV253. Callers no longer print their own.
 *  1/31/2026, Adam
 *  First time checkin
 * TreasureMap: refactor Locations generation/persistence (spawner-driven)
 * Body:
 * Refactors TreasureMap.Locations into a clean, data-driven model backed by a JSON cache ("Angel Island Treasure.json").
 * On startup, load cached Point2D locations if present; otherwise generate a new pool by querying Felucca spawners
 * (filtered by domain/content), extracting anchor points and fanning out additional points via GetSpawnPosition(ClearPath),
 * rejecting houses/regions/rect exclusions, deduping globally, shuffling, then persisting the result to JSON for deterministic reuse.
 * This replaces the prior Angel Island locations implementation (functionally OK, but ugly/brittle) with a maintainable,
 * extensible foundation.
 * Model overview:
 * Locations are now a cached set of Point2D coordinates. The system prefers loading the cached JSON; if missing, it rebuilds the pool
 * from live world data (spawners) and writes the cache. Generation is world-aware: it uses spawner anchors plus a small fan-out
 * to increase density, enforces accessibility via ClearPath, excludes invalid regions (town/guarded/etc), excludes houses (current snapshot),
 * dedupes, and randomizes ordering to avoid predictable clustering. The resulting list is large enough to resist rune-booking and resilient
 * to future dynamic housing (placement-time validation can retry against the pool).
 */
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Prompts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Server.Commands
{

    public static class TreasureMapLocations
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("GenTreasureMapLocations", AccessLevel.Owner, new CommandEventHandler(GenTreasureMapLocations_OnCommand));
            Server.CommandSystem.Register("JumpTreasureMapLocations", AccessLevel.GameMaster, new CommandEventHandler(JumpTreasureMapLocations_OnCommand));
        }

        [Usage("GenTreasureMapLocations")]
        [Description("Rebuilds custom AI treasure map locations from spawners and writes Angel Island Treasure.json.")]
        public static void GenTreasureMapLocations_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null)
                return;

            from.Prompt = new ConfirmGenTreasureLocationsPrompt();
            from.SendMessage("This command will regenerate treasure map locations and may take about 1 minute.");
            from.SendMessage("During this time the server may hitch briefly at startup-style load.");

            if (Core.ReleasePhase >= ReleasePhase.Production)
            {
                from.SendMessage("WARNING: You are on a production server.");
                from.SendMessage("This will overwrite the existing Angel Island Treasure.json file.");
            }

            from.SendMessage("Type 'yes' to confirm.");
        }

        private class ConfirmGenTreasureLocationsPrompt : Prompt
        {
            public ConfirmGenTreasureLocationsPrompt()
            {
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (from == null)
                    return;

                if (text == null)
                    text = string.Empty;

                text = text.Trim();

                if (text.Length > 40)
                    text = text.Substring(0, 40);

                text = text.ToLower();

                if (text != "y" && text != "yes")
                {
                    from.SendMessage("Canceled.");
                    return;
                }

                // Run the command
                try
                {
                    string AIFileName = Path.Combine(Core.DataDirectory, "Angel Island Treasure.json");

                    try
                    {
                        if (File.Exists(AIFileName))
                            File.Delete(AIFileName);
                    }
                    catch (Exception ex)
                    {
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                        from.SendMessage("Warning: Could not delete \"{0}\". Attempting to continue.", AIFileName);
                    }

                    from.SendMessage("Generating treasure map locations...");   // the console message comes from Generate

                    Utility.TimeCheck tc = new Utility.TimeCheck();
                    tc.Start();

                    int lcount = LoadLocations();

                    OwnerTools.LoadTreasureJump(from);

                    tc.End();
                    string s = string.Format("Done. {0} locations loaded in: {1}", lcount, tc.TimeTaken);
                    Console.WriteLine(s);
                    from.SendMessage(s);
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    from.SendMessage("Error: exception while generating treasure map locations. See server logs.");
                }
            }

            public override void OnCancel(Mobile from)
            {
                if (from != null)
                    from.SendMessage("Canceled.");
            }
        }
        [Usage("JumpTreasureMapLocations")]
        [Description("Loads every treasure map location into your jump list, once each in random order (same as [LoadJump treasure).")]
        public static void JumpTreasureMapLocations_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile != null)
                OwnerTools.LoadTreasureJump(e.Mobile);
        }

        public static int LoadLocations()
        {
            string AIFileName = Path.Combine(Core.DataDirectory, "Angel Island Treasure.json");
            int count = 0;

            if (File.Exists(AIFileName))
            {
                try
                {
                    List<Point2D> points = ParseLocations(File.ReadAllText(AIFileName));

                    if (points.Count == 0)
                    {
                        Core.LoggerShortcuts.BootError(
                            string.Format("\nTreasure locations file \"{0}\" exists but could not be parsed or was empty.", AIFileName));
                        return 0;
                    }

                    TreasureMap.Locations = points.ToArray();
                    count = TreasureMap.Locations.Length;

                    // handled by caller
                    //Utility.Monitor.WriteLine(
                    //    "TreasureMap: Loaded {0} locations from \"{1}\".",
                    //    ConsoleColor.Green,
                    //    count,
                    //    AIFileName);
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    Core.LoggerShortcuts.BootError(
                        string.Format("\nError while reading Treasure locations from \"{0}\".", AIFileName));
                    return 0;
                }
            }
            else // generate and write new locations file
            {
                List<Point2D> locations = Generate(out _);

                // Assign
                TreasureMap.Locations = locations.ToArray();
                count = TreasureMap.Locations.Length;

                // Persist to Angel Island Treasure.json
                try
                {
                    WriteLocations(AIFileName, locations);
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    Core.LoggerShortcuts.BootError(
                        string.Format("\nError while writing Treasure locations to \"{0}\".", AIFileName));
                }
            }


            return count;
        }

        // Angel Island Treasure.json format: [ [x,y], ... ]
        private static void WriteLocations(string path, List<Point2D> locations)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                locations.Select(p => new int[] { p.X, p.Y }).ToList(),
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Regenerates the location database from the loaded world and replaces Angel Island Treasure.json and
        /// TreasureMap.Locations. The new file is written beside the old one, read back, and only then swapped in
        /// (the old file is kept as Angel Island Treasure.json.pre-[caller]; an existing backup is never overwritten). On any failure the existing file and
        /// cache are left as they were and false is returned. Every run is appended to TreasureLocations.log.
        /// </summary>
        public static bool Regenerate(string caller, out string summary)
        {
            string path = Path.Combine(Core.DataDirectory, "Angel Island Treasure.json");
            string temp = path + ".new";
            string backup = path + ".pre-" + caller;
            int oldCount = TreasureMap.Locations?.Length ?? 0;
            StringBuilder log = new StringBuilder();
            log.AppendFormat("{0} {1} on {2}: regenerate treasure map locations\r\n", DateTime.Now, caller, Core.Server);
            bool ok = false;
            summary = "";

            try
            {
                Utility.TimeCheck tc = new Utility.TimeCheck();
                tc.Start();
                List<Point2D> locations = Generate(out GenerationReport report);
                tc.End();

                log.AppendFormat("  spawners {0}, anchors accepted {1}, anchors rejected {2}, candidates accepted {3}, fallbacks rejected {4}, searches exhausted {5}, in {6}\r\n",
                    report.Spawners, report.AnchorsAccepted, report.AnchorsRejected.Values.Sum(), report.CandidatesAccepted, report.FallbacksRejected, report.SearchesExhausted, tc.TimeTaken);
                foreach (var kv in report.AnchorsRejected.OrderByDescending(kv => kv.Value))
                    log.AppendFormat("    anchor rejected, {0}: {1}\r\n", kv.Key, kv.Value);
                foreach (var kv in report.CandidatesRejected.OrderByDescending(kv => kv.Value))
                    log.AppendFormat("    candidate check rejected, {0}: {1}\r\n", kv.Key, kv.Value);

                // A world that failed to load (no spawners, no regions) must not replace a good database
                if (locations.Count == 0 || locations.Count < oldCount / 2)
                {
                    summary = string.Format("Treasure map locations NOT regenerated: {0} generated against {1} loaded.", locations.Count, oldCount);
                    return false;
                }

                WriteLocations(temp, locations);
                if (ParseLocations(File.ReadAllText(temp)).Count != locations.Count)
                {
                    summary = string.Format("Treasure map locations NOT regenerated: \"{0}\" did not read back.", temp);
                    return false;
                }

                // Keep the first backup: if the world is not saved after the patch, it runs again at the next
                //  startup, and that run must not overwrite the original database with the previous run's output.
                if (File.Exists(path))
                    File.Replace(temp, path, File.Exists(backup) ? null : backup);
                else
                    File.Move(temp, path);

                TreasureMap.Locations = locations.ToArray();
                ok = true;

                // Decoded maps keep the chest location they were given; count those the new rules would reject
                int maps = 0, invalid = 0;
                foreach (Item item in World.Items.Values)
                    if (item is TreasureMap tm && !tm.Deleted && !tm.Completed && tm.ChestMap == Map.Felucca)
                    {
                        maps++;
                        if (ValidateLocation(Map.Felucca, tm.ChestLocation.X, tm.ChestLocation.Y) != null)
                            invalid++;
                    }

                summary = string.Format("Treasure map locations regenerated: {0} (was {1}); {2} anchors accepted, {3} rejected; {4} of {5} uncompleted treasure maps point at locations the generator now rejects (unchanged).",
                    locations.Count, oldCount, report.AnchorsAccepted, report.AnchorsRejected.Values.Sum(), invalid, maps);
                return true;
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                summary = (ok ? "Treasure map locations regenerated, then failed: " : "Treasure map locations NOT regenerated: ") + ex.Message;
                return ok;
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                log.AppendFormat("  {0}\r\n", summary);
                try { File.AppendAllText(Path.Combine(Core.LogsDirectory, "TreasureLocations.log"), log.ToString()); }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            }
        }

        // Target density: ~5 points per spawner (1 anchor + alternates)
        private const int TargetPerSpawner = 5;
        private const int HomeRange = 10;
        private const int RetriesPerAlternate = 20;
        private const int HouseHeight = 16;

        /// <summary>
        /// What Generate() accepted and rejected, by reason.
        /// </summary>
        public class GenerationReport
        {
            public int Spawners;
            public int AnchorsAccepted;
            public int CandidatesAccepted;
            public int FallbacksRejected;           // GetSpawnPosition gave up and returned the anchor
            public int SearchesExhausted;           // no new location after RetriesPerAlternate attempts
            public Dictionary<string, int> AnchorsRejected = new();
            public Dictionary<string, int> CandidatesRejected = new();
            public List<Spawner> RejectedSpawners = new();

            internal static void Count(Dictionary<string, int> d, string reason) =>
                d[reason] = d.TryGetValue(reason, out int n) ? n + 1 : 1;
        }

        /// <summary>
        /// A treasure map location as its consumers use it: (x, y, GetAverageZ) is walkable, non-water land
        /// and passes the treasure map restrictions. Returns null when valid, else the reason.
        /// </summary>
        public static string? ValidateLocation(Map map, int x, int y)
        {
            Point3D p = new Point3D(x, y, map.GetAverageZ(x, y));

            if (!IsValidTreasureLoc(p, map, HouseHeight))
                return "treasure map restrictions";

            if (!Server.Engines.TerritoryControl.ZoneGuardianSpawner.IsWalkableLand(map, p))
                return "not walkable land at GetAverageZ";

            string? outdoor = NotNaturalGround(map, x, y, p.Z);
            if (outdoor != null)
                return outdoor;

            return null;
        }

        // Conservative outdoor filter: only clearly natural, uncovered ground is a location. Anything questionable
        //  (unnamed land tiles included) is rejected; the pool stays large enough without it.
        private static readonly HashSet<string> NaturalLand = new(StringComparer.OrdinalIgnoreCase)
            { "forest", "grass", "snow", "jungle", "dirt", "furrows", "sand", "swamp" };
        private const TileFlag ManMade = TileFlag.NoShoot | TileFlag.ArticleA | TileFlag.ArticleAn | TileFlag.Bridge |
            TileFlag.Impassable | TileFlag.Wall | TileFlag.Roof | TileFlag.Door | TileFlag.Window;
        private const TileFlag Cover = TileFlag.Surface | TileFlag.Roof | TileFlag.Wall | TileFlag.Impassable | TileFlag.Bridge;

        /// <summary>
        /// Null when (x, y) at standing z is clearly natural, open ground; else the reason. Rejects:
        ///  - a land tile that is not natural terrain (floors, flagstone, cobblestones, paved stone, NODRAW, unnamed);
        ///  - a man-made surface at standing level (pavers, flagstones, carpet, boards, bridges, graves, teleporters);
        ///    natural decoration (flowers, grasses) carries none of the ManMade flags;
        ///  - anything overhead: a Roof static at or above z, or a Surface/Wall/Impassable/Bridge static 10 or more
        ///    above z (an upper floor, flat roof or ceiling). Tree canopies are Foliage and do not count.
        /// Player houses are excluded separately.
        /// </summary>
        public static string? NotNaturalGround(Map map, int x, int y, int z)
        {
            LandTile land = map.Tiles.GetLandTile(x, y);
            string name = TileData.LandTable[land.ID & TileData.MaxLandValue].Name ?? "";
            if (!NaturalLand.Contains(name))
                return "not natural ground (" + (name.Length > 0 ? name : "unnamed") + ")";

            foreach (StaticTile t in map.Tiles.GetStaticTiles(x, y))
            {
                TileFlag f = TileData.ItemTable[t.ID & TileData.MaxItemValue].Flags;
                if ((f & TileFlag.Surface) != 0 && t.Z >= z - 3 && t.Z < z + 10 && (f & ManMade) != 0)
                    return "man-made floor";
                if (t.Z >= z && (f & TileFlag.Roof) != 0)
                    return "building floor (roofed)";
                if (t.Z >= z + 10 && (f & TileFlag.Foliage) == 0 && (f & Cover) != 0)
                    return "covered";
            }
            return null;
        }

        /// <summary>
        /// Builds a new location pool from Felucca spawners (not persisted). Each spawner is an anchor: if the
        /// anchor itself is not a valid location the spawner is skipped; otherwise the anchor is added and
        /// GetSpawnPosition (ClearPath) finds nearby locations reachable on foot from it.
        /// </summary>
        public static List<Point2D> Generate(out GenerationReport report)
        {
            // The one progress message for every generation (startup with no database, [GenTreasureMapLocations,
            //  PatchV253): generation takes about a minute and the console is otherwise silent.
            Utility.Monitor.WriteLine("Generating Treasure Map Locations. This may take a while...", ConsoleColor.Yellow);

            report = new GenerationReport();
            GenerationReport rep = report;
            List<Point2D> locations = new();

            List<Spawner> spawners =
                SpawnerQuery.GetSpawners(
                    maps: MapScope.Felucca,
                    domain: SpawnDomain.Any & ~(SpawnDomain.Town | SpawnDomain.Guarded),
                    content: SpawnContent.Any);     // any content, running or not: terrain is validated per anchor below

            // Dedupe key for Point2D
            HashSet<long> seen = new HashSet<long>();

            // Candidates are validated inside GetSpawnPosition, before its clear-path test. A candidate is only
            // usable at GetAverageZ: the stored Point2D cannot keep a dock plank, basement or rooftop Z.
            Spawner.LocationValidator validator = (loc, map) =>
            {
                string? reason = loc.Z != map.GetAverageZ(loc.X, loc.Y) ? "not at GetAverageZ" : ValidateLocation(map, loc.X, loc.Y);
                if (reason != null)
                    GenerationReport.Count(rep.CandidatesRejected, reason);
                return reason == null;
            };

            // Dummy item used by GetSpawnPosition
            MetalChest dummy = new MetalChest();
            try
            {
                foreach (Spawner s in spawners)
                {
                    if (s == null)
                        continue;

                    Map map = s.Map;
                    if (map != Map.Felucca)
                        continue;

                    report.Spawners++;

                    // The anchor must itself be a valid location, or the spawner is not used at all
                    string? anchorReason = ValidateLocation(map, s.X, s.Y);
                    if (anchorReason != null)
                    {
                        GenerationReport.Count(report.AnchorsRejected, anchorReason);
                        report.RejectedSpawners.Add(s);
                        continue;
                    }

                    report.AnchorsAccepted++;
                    Point3D anchor3 = new Point3D(s.X, s.Y, map.GetAverageZ(s.X, s.Y));

                    int added = 0;

                    // Add anchor (if not a global duplicate)
                    if (TryAdd(new Point2D(anchor3.X, anchor3.Y), locations, seen))
                        added++;

                    // Fan out until we reach TargetPerSpawner or GetSpawnPosition reports a hard fail
                    while (added < TargetPerSpawner)
                    {
                        bool gotOne = false;

                        for (int attempt = 0; attempt < RetriesPerAlternate; attempt++)
                        {
                            Point3D p = Spawner.GetSpawnPosition(
                                map,
                                anchor3,
                                homeRange: HomeRange,
                                sflags: Utility.SpawnFlags.ClearPath,
                                dummy,
                                validator);

                            // Strong failure: internal SpawnPointTries exhausted, GetSpawnPosition returned the anchor
                            if (p == anchor3)
                            {
                                report.FallbacksRejected++;
                                break;
                            }

                            // GetSpawnPosition ran the validator; check again rather than trust it
                            string? reason = p.Z != map.GetAverageZ(p.X, p.Y) ? "not at GetAverageZ" : ValidateLocation(map, p.X, p.Y);
                            if (reason != null)
                            {
                                GenerationReport.Count(report.CandidatesRejected, reason);
                                continue;
                            }

                            if (!TryAdd(new Point2D(p.X, p.Y), locations, seen))
                            {
                                GenerationReport.Count(report.CandidatesRejected, "duplicate");
                                continue;
                            }

                            report.CandidatesAccepted++;
                            added++;
                            gotOne = true;
                            break;
                        }

                        if (!gotOne)
                        {
                            report.SearchesExhausted++;
                            break; // stop fanning out this spawner
                        }
                    }
                }
            }
            finally
            {
                dummy.Delete();
            }

            // Randomize final pool
            Utility.Shuffle(locations);

            return locations;
        }

        /// <summary>
        /// Reads a locations file: [ { "X":n, "Y":n }, ... ] or [ [x,y], ... ]. Negative points are dropped, duplicates removed.
        /// </summary>
        public static List<Point2D> ParseLocations(string json)
        {
            var points = new List<Point2D>();

            // Format A: [ { "X":n, "Y":n }, ... ]
            try
            {
                var arr = System.Text.Json.JsonSerializer.Deserialize<List<Point2DJson>>(json);
                if (arr != null && arr.Count > 0)
                {
                    foreach (var p in arr)
                    {
                        if (p.X < 0 || p.Y < 0)
                            continue;

                        points.Add(new Point2D(p.X, p.Y));
                    }
                }
            }
            catch
            {
            }

            // Format B: [ [x,y], ... ]
            if (points.Count == 0)
            {
                try
                {
                    var arr = System.Text.Json.JsonSerializer.Deserialize<List<int[]>>(json);
                    if (arr != null && arr.Count > 0)
                    {
                        foreach (var pair in arr)
                        {
                            if (pair == null || pair.Length < 2)
                                continue;

                            int x = pair[0];
                            int y = pair[1];

                            if (x < 0 || y < 0)
                                continue;

                            points.Add(new Point2D(x, y));
                        }
                    }
                }
                catch
                {
                }
            }

            return Dedupe(points);
        }

        private static bool TryAdd(Point2D p, List<Point2D> list, HashSet<long> seen)
        {
            long key = ((long)p.X << 32) ^ (uint)p.Y;

            if (!seen.Add(key))
                return false;

            list.Add(p);
            return true;
        }

        private static bool IsValidTreasureLoc(Point3D loc, Map map, int houseHeight)
        {
            if (loc.X < 0 || loc.Y < 0)
                return false;

            // Map sanity (you can tighten this if needed)
            if (map != Map.Felucca)
                return false;

            Point2D p2 = new Point2D(loc.X, loc.Y);

            // Must be on Britannia main land wrap
            //if (!Utility.World.BritMainLandWrap.Contains(p2))
            //return false;

            if (!Utility.WorldMap.BritWrap[0].Contains(p2) && !Utility.WorldMap.BritWrap[1].Contains(p2))
                return false;

            // Must NOT be in excluded rectangles
            if (Utility.WorldMap.AngelIsland.Contains(p2))
            {
                Rectangle2D aiRect = Utility.WorldMap.AngelIsland;

                //Utility.Monitor.WriteLine(
                //    "TreasureLoc reject: AIRect TL({0},{1}) WH({2},{3}) BR({4},{5}) hit by point ({6},{7})",
                //    ConsoleColor.Red,
                //    aiRect.X,
                //    aiRect.Y,
                //    aiRect.Width,
                //    aiRect.Height,
                //    aiRect.X + aiRect.Width,
                //    aiRect.Y + aiRect.Height,
                //    p2.X,
                //    p2.Y);

                return false;
            }


            if (Utility.WorldMap.LostLandsWrap.Contains(p2))
                return false;

            if (Utility.WorldMap.SolenCaves.Contains(p2))
                return false;

            if (Utility.WorldMap.Dungeons.Contains(p2))
                return false;

            // Reject houses (strongest signal)
            BaseHouse? house = BaseHouse.FindHouseAt(loc, map, houseHeight);
            if (house != null)
                return false;

            // Reject region-based exclusions
            Region? r = null;
            try { r = Region.Find(loc, map); }
            catch { r = null; }

            if (r != null)
            {
                if (r.IsTownRules) return false;
                if (r.IsGuarded) return false;
                if (r.IsPrisonRules) return false;
                if (r.IsGreenAcresRules) return false;
                if (r.IsHouseRules) return false;
                if (r.IsJailRules) return false;
                if (r.IsDungeonRules) return false;

                // Name-based belt-and-suspenders
                // (keep as-is if you want explicit denylist)
                if (r.Name == "Broken ML Map") return false;
                if (r.Name == "Britannia Royal Zoo") return false;
                if (r.Name == "Restricted Area") return false;
                if (r.Name == "Misc Dungeons") return false;
                if (r.Name == "Terathan Keep") return false;

                // The moongate pads (guarded in the base region data). Excluded by name, not by guard state:
                // guards are mutable (Upheaval territory control). The surrounding "Moongate Houseblockers"
                // region only prevents housing and stays eligible.
                if (r.Name == "Moongates") return false;
            }

            // TODO: TEMPORARY WORKAROUND -- REMOVE when the Jhelom region issue is fixed.
            //  Some locations inside the Felucca Jhelom town region's own area (around 1376-1419, 3732-3749) are not
            //  reported as Jhelom by Region.Find, apparently a region-priority/classification anomaly that needs separate
            //  investigation, so the town check above lets them through. This is not a geographic rule: exclude the town's
            //  own area directly until the anomaly is fixed, then return to this generator and REMOVE this block.
            Region? jhelom = Region.FindByName("Jhelom", map);
            if (jhelom != null && jhelom.IsTownRules && jhelom.Contains(loc))
                return false;

            return true;
        }

        private static List<Point2D> Dedupe(List<Point2D> points)
        {
            // Do not rely on Point2D equality semantics; dedupe by coordinate key.
            var set = new HashSet<long>();
            var list = new List<Point2D>(points.Count);

            for (int i = 0; i < points.Count; i++)
            {
                Point2D p = points[i];

                long key = ((long)p.X << 32) ^ (uint)p.Y;
                if (set.Add(key))
                    list.Add(p);
            }

            return list;
        }

        private struct Point2DJson
        {
            public int X { get; set; }
            public int Y { get; set; }
        }


    }

}

#region TOOLS


#endregion TOOLS