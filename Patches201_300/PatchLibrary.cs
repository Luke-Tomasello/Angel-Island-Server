/***************************************************************************
 *
 *                         Patches201_300
 *
 *  GMN World Patch Library - Patches 201 through 300
 *
 *  OVERVIEW
 *  --------
 *  This library contains the implementation of world patches assigned to
 *  patch numbers 201 through 300.
 *
 *  Patches are invoked by the main Patcher through PatchLibrary.RunPatch().
 *  The library does not manage persistent patch history; that responsibility
 *  belongs to the main Patcher.
 *
 *  PatchLibrary maintains a dispatch table whose array position corresponds
 *  to the patch number within this library's range. Empty or retired patch
 *  positions are preserved as null.
 *
 *  GetLastPatch() reports the highest patch slot currently defined by this
 *  library. The full numeric range does not need to be populated.
 *
 *  ADDING A PATCH
 *  --------------
 *  1. Add the patch implementation using the standard signature:
 *
 *         private static int? PatchV100(int patchid)
 *
 *  2. Add the method to m_Patches in its permanent numeric position:
 *
 *         PatchV98,
 *         PatchV99,
 *         PatchV100,
 *
 *  3. Add the corresponding PatchIndex value in the main Patcher.
 *
 *  Return the number of objects updated when the patch applies.
 *  Return null when the patch does not apply to the current shard/server.
 *
 *  Released patch numbers are permanent. Never reorder, renumber, reuse,
 *  or collapse existing slots. Preserve retired positions as null.
 *
 ***************************************************************************/

/* Patches201_300/PatchLibrary.cs
 * ChangeLog
 *  11/5/26, Adam (PatchV264)
 *      PatchV264 also moves three hemmed-in spawners, Felucca and Trammel (a Warlord placed on the tile could not move,
 *      or barely): Britain scribe (1493, 1713, 6) -> (1493, 1715, 6), Ocllo scribe (3614, 2467, 0) -> (3611, 2466, 0) and
 *      Jhelom butcher (1448, 4024, 0) -> (1450, 4027, 0). Runs whether or not TerritoryControl is on; not Login Server.
 *  11/4/26, Adam (PatchV264)
 *      Add PatchV264: Warlord counts carried over from an earlier campaign (a recapture before r1220 kept
 *      MiniBossesSpawned; PatchV257 repaired only the conquest flags) are set to the earned count, with
 *      TerritoryConquestRepair.RepairWarlordCounts (Patches201_300/TerritoryConquestRepair.cs). Upheaval 2026-11-04:
 *      Britain 45 -> 34. TerritoryControl is enabled and loaded; not Login Server.
 *  11/4/26, Adam (PatchV263)
 *      Add PatchV263: Upheaval breeds creatures. Turns on the BreedingEnabled dynamic feature (as the Core Management
 *      Console's BreedingEnabled does); already on, nothing changes. Upheaval only; not Login Server.
 *  10/29/26, Adam (PatchV262)
 *      Add PatchV262: the tracks remasters replaced. Approving a remaster removed the replaced track from the music library
 *      and left it in internal storage; RemasterOrphanCleanup deletes each one nothing references. Not Login Server. Not done
 *      (retried next start) if the reference scan cannot be completed.
 *  10/29/26, Adam (PatchV261)
 *      Add PatchV261: player history. Upgrades the old Data/AIPlayerHistory files and captures every player on this shard
 *      into its file (PlayerHistory.Maintain; merged, nothing on file is lost), then reloads the Doppelganger pool.
 *      Not Login Server, Test Center or event shards. Not done (retried next start) if this shard's file cannot be read or written.
 *  10/28/26, Adam (PatchV260)
 *      Add PatchV260: The Pumpkin King, retuned. The ChampEngine at 1434 1389 0 (Felucca) re-applies the ThePumpkinKing
 *      definition (Data/ChampSpawns.xml: fewer kills, fewer alive at once) and gets MaxLevels 5. A running champ restarts with the new levels.
 *      Not done (retried next start) while the old XML is installed. Not Login Server.
 *  10/28/26, Adam (PatchV259)
 *      Add PatchV259: The Pumpkin King pumpkin patch. An 8x8 PumpkinPatchVine GridSpawner (sparsity 0.8, MustSteal off,
 *      running) at 1436 1395 0 on Felucca and Test Center; a map that already has a grid spawner there is left alone.
 *      Not Login Server. The new spawner gets ScheduleRespawn, so the patch fills right away.
 *  10/28/26, Adam (PatchV258)
 *      Add PatchV258: The Pumpkin King (Halloween 2026). The ChampEngine at 1434 1389 0 (Felucca) gets MaxLevels 8: a run
 *      plays 8 of its spawn levels, spread evenly, then the boss. Not Login Server; a missing champ is reported, not an error.
 *  10/27/26, Adam (PatchV257)
 *      Add PatchV257: repair TerritoryControl conquest flags left by Orc recaptures before the 10/27 fix (stale zones,
 *      districts fully re-cleared but not conquered) with TerritoryConquestRepair (Patches201_300/TerritoryConquestRepair.cs).
 *      Silent: no announcement, bonus or Warlord; no spawner, kill, credit or ownership is changed. Only where
 *      TerritoryControl is enabled and loaded; not Login Server.
 *  10/26/26, Adam
 *      MusicOwnershipReconciler (PatchV252) moved here from Scripts/Engines/AIMusicSystem/MusicBox: one-time migration
 *      helpers live with their patches. PatchV252 itself is unchanged (it has already run).
 *  10/25/26, Adam (PatchV256)
 *      Add PatchV256: delete orphaned music virtual instrument sets (complete, unreferenced harp/lapharp/lute sets in
 *      internal storage) with MusicOrphanCleanup (Patches201_300/MusicOrphanCleanup.cs). Not Login Server.
 *      LegacyRsmRepair (PatchV255) moved here from Scripts/Engines/AIMusicSystem/Tools: one-time migration helpers live
 *      with their patches. PatchV255 itself is unchanged (it has already run).
 *  10/25/26, Adam
 *      PatchV255: a protection violation after the repair (LegacyRsmRepair.Result.ProtectionFailed) is reported in red
 *      and leaves the patch incomplete (returns null).
 *  10/24/26, Adam
 *      Add PatchV255: repair legacy rolled up sheet music (Instrument self-reference, never-hashed songs) with
 *      LegacyRsmRepair (Scripts/Engines/AIMusicSystem/Tools/LegacyRsmRepair.cs). Not Login Server.
 */

using Server;
using Server.Accounting;
using Server.Commands;
using Server.Engines;
using Server.Engines.Alignment;
using Server.Engines.ChampionSpawn;
using Server.Engines.TerritoryControl;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Multis.StaticHousing;
using Server.Regions;
using Server.SkillHandlers;
using System.Collections;
using System.Xml;
using static Server.CoreAI;
using static Server.Patcher;
using static Server.Utility;

namespace Patches201_300
{
    public static class PatchLibrary
    {
        private const int PatchOffset = 200;

        private delegate int? PatchHandler(int patchid);

        private static readonly PatchHandler[] m_Patches =
        {
            null,        // index 0 - reserved/run-always convention
            PatchV201,   // Seasons 2: initialization placeholder (empty)
            PatchV202,   // township decay, champ subsidies, Wrong level 3 champ spawner
            PatchV203,   // Seasons 2: turn off guards
            PatchV204,   // turn on PackUp (not Mortalis)
            PatchV205,   // Seasons 2: respawn to remove magic scrolls
            PatchV206,   // Seasons 2 (Debug): make all regions conquerable
            PatchV207,   // fix the Harrower summoning room
            PatchV208,   // Seasons 2: enable WaxPot on beekeepers
            PatchV209,   // Seasons 2: respawn for balanced weapon drops
            PatchV210,   // Seasons 2: turn on guards
            PatchV211,   // turn on guards
            PatchV212,   // repair custom region controller names
            PatchV213,   // Seasons 2: repair custom region controllers, respawn for limited magic
            PatchV214,   // Seasons 2: turn off event champ spawners
            PatchV215,   // Seasons: patch static housing default storage
            PatchV216,   // Seasons: increase tracking ranges
            PatchV217,   // Seasons 2: add a stable boy at each dungeon
            PatchV218,   // set the mount endurance factor to 2.0
            PatchV219,   // spawn pack animal followers for NPCs (not Siege)
            PatchV220,   // Seasons 2: respawn to replace treasure chest magic scrolls
            PatchV221,   // remove abandoned small forges, move a pack llama follower
            PatchV222,   // Seasons: replace the skill teleporter at 1361 883 with a temporary teleporter
            PatchV223,   // Seasons: restore the skill teleporter at 1361 883 after Season 2
            PatchV224,   // Seasons: total respawn of fillable containers
            PatchV225,   // Seasons: respawn to replace treasure chest magic scrolls
            PatchV226,   // Seasons 2: newbie all Orcish Kin Masks
            PatchV227,   // Seasons 2: territory districts, orc mini-bosses, township activity, creature allegiance
            PatchV228,   // Seasons 2: set orc alignment, clear orc aggression, add Ocllo and Magincia districts
            PatchV229,   // Seasons 2: enable vendor supplies
            PatchV230,   // Seasons 2: spawn gypsy seers near Felucca town criers
            PatchV231,   // Seasons 2: enable vendor supplies, disable some prison spawn
            PatchV232,   // Seasons 3/Upheaval: creature allegiance, Orcish Invasion, gypsy seers
            PatchV233,   // turn off the Terathan Keep teleporter
            null,        // PatchV234 - retired: unused slot
            PatchV235,   // Seasons 3/Upheaval: creature allegiance, Orcish Invasion, gypsy seers (redo)
            PatchV236,   // PreWorldLoad: rename StartingLootMonitor.bin to KeyGenerator.bin
            PatchV237,   // ServerStarted: convert saved DateTimes to UTC
            PatchV238,   // ServerStarted: total world respawn
            PatchV239,   // ServerStarted: seed reserved names
            PatchV240,   // migrate legacy patch history
            PatchV241,   // PreWorldLoad: migrate saves from Saves/AngelIsland to Saves/<server>
            PatchV242,   // configure login server visibility
            PatchV243,   // Upheaval: creature allegiance, respawn Orcish Invasion, gypsy seers
            PatchV244,   // restore town guardedness from Data/StaticRegions.xml
            PatchV245,   // PreWorldLoad: remove retired Invasion types
            PatchV246,   // PreWorldLoad: remove retired Wild Hunt types
            PatchV247,   // remove retired Wild Hunt spawn entries from spawners
            PatchV248,   // PreWorldLoad: remove retired Kin Ransom types
            PatchV249,   // Upheaval: fortify and newbie existing Orcish Kin Masks
            PatchV250,   // Wrong: unlock the SecretDungeonDoor at 5679 520, retarget the PortKey pad (not Angel Island)
            PatchV251,   // synchronize custom house deeds on staff-owned player vendors with their blueprints (not Login Server)
            PatchV252,   // Upheaval: reconcile music ownership and library prices from account GUIDs
            PatchV253,   // regenerate treasure map locations with the validated generator (not Login Server)
            PatchV254,   // relocate overland NPCs with invalid or uncertain placements to treasure map locations (not Login Server)
            PatchV255,   // repair legacy rolled up sheet music: Instrument self-reference, never-hashed songs (not Login Server)
            PatchV256,   // delete orphaned music virtual instrument sets (harp/lapharp/lute) from internal storage (not Login Server)
            PatchV257,   // repair territory conquest flags left by Orc recaptures (silent; not Login Server)
            PatchV258,   // The Pumpkin King: champ at 1434 1389 0 runs 8 spawn levels (MaxLevels; not Login Server)
            PatchV259,   // The Pumpkin King: pumpkin patch grid spawner at 1436 1395 0, Felucca and Test Center (not Login Server)
            PatchV260,   // The Pumpkin King: re-apply the retuned ChampSpawns.xml levels, MaxLevels 5 (restarts a running champ; not Login Server)
            PatchV261,   // player history: upgrade Data/AIPlayerHistory files, capture this shard's players (not Login Server, Test Center or event shards)
            PatchV262,   // music library: delete the tracks remasters replaced, left in internal storage (not Login Server)
            PatchV263,   // Upheaval: enable breeding (the BreedingEnabled dynamic feature; not Login Server)
            PatchV264,   // territory: set Warlord counts carried over from an earlier campaign to the earned count (not Login Server)
        };

        public static int GetLastPatch()
        {
            return PatchOffset + m_Patches.Length - 1;
        }
        public static int? RunPatch(int patchNumber)
        {
            int index = patchNumber - PatchOffset;

            if (index <= 0 ||
                index >= m_Patches.Length ||
                m_Patches[index] == null)
            {
                return null;
            }

            return m_Patches[index](patchNumber);
        }

        #region PatchV201

        private static int? PatchV201(int patchid)
        {
            if (Seasons(2) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Initialize Seasons season 2

                //whatever we want to do for Seasons season 2 initialization

                #endregion Initialize Seasons season 2

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV201

        #region PatchV202

        private static int? PatchV202(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {

                int patched = 0;

                #region Begin Implementation

                #region Patch townships
                // Season 2 startup normalization:
                // TownshipSettings values are persisted/deserialized at runtime, so the declared
                // field initializers act only as defaults for new worlds/settings. We patch the
                // live value here instead of baking Season 2 tuning directly into the initializer
                // to preserve consistency/readability with the existing TownshipSettings model.
                Server.Township.TownshipSettings.LawGuardedFee = 750;
                patched++;

                // turn on township decay
                CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.FreezeTownshipDecay);

                foreach (var tss in TownshipStone.AllTownshipStones)
                {
                    tss.RecordFameAndTaxBankInfo();
                    patched++;
                }

                #endregion Patch townships

                #region All Champ Engines now pay LB Subsidies and Fame

                foreach (ChampEngine ce in ChampEngine.Instances)
                {
                    ce.KillDatabase = true;
                    patched++;
                }
                #endregion All Champ Engines now pay LB Subsidies and Fame

                #region Patch champ spawner in Wrong level 3 (Random)

                Point3D px = new Point3D(5574, 194, 0);

                Map[] maps =
                {
                    Map.Felucca,
                    Map.TestCenter
                };

                foreach (Map map in maps)
                {
                    List<Item> items = FindItemsAt(px, map, 2);

                    foreach (Item item in items)
                    {
                        if (item is ChampEngine ce)
                        {
                            // Prevent normal wave spawns from appearing inside the tower.
                            ce.SpawnExcludeStart = new Point3D(5591, 207, -5);
                            ce.SpawnExcludeEnd = new Point3D(5569, 185, 44);

                            // Widen normal wave distribution around the tower.
                            // Final champ placement still respects level-defined range.
                            ce.HomeRange = 30;

                            patched++;
                        }
                    }
                }

                #endregion Patch champ spawner in Wrong level 3 (Random)

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV202

        #region PatchV203

        private static int? PatchV203(int patchid)
        {
            if (Seasons(2) && !LoginServer(quiet: true))
            {

                int patched = 0;

                #region Begin Implementation

                #region Turn off guards
                foreach (Region region in Region.Regions)
                {
                    if (region is StaticRegion sRegion)
                    {
                        if (sRegion.EnableGuards && sRegion is not TownshipRegion)
                        {
                            sRegion.GuardsDisabledUntil = SeasonsHelper.GetNextSeasonReset(DateTime.UtcNow);
                            patched++;
                        }
                    }
                }
                #endregion Turn off guards

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV203

        #region PatchV204

        private static int? PatchV204(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true) && !Mortalis())
            {

                int patched = 0;

                #region Begin Implementation

                #region Turn on Packup

                CoreAI.EnablePackUp = true;
                patched++;

                #endregion Turn on Packup

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV204

        #region PatchV205

        private static int? PatchV205(int patchid)
        {
            if (Seasons(2) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region  Respawn to eliminate magic scrolls

                var result = TotalRespawnCommand.TotalRespawn(null);
                patched = result.TotalCount;

                #endregion Respawn to eliminate magic scrolls

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV205

        #region PatchV206

        private static int? PatchV206(int patchid)
        {
            if (Seasons(2) && Core.Debug && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region  Set all regions to IsConquerable

                foreach (Region region in Region.Regions)
                {
                    if (region is StaticRegion sRegion)
                    {
                        if (sRegion is not TownshipRegion)
                        {
                            sRegion.IsConquerable = true;
                            patched++;
                        }
                    }
                }

                #endregion Set all regions to IsConquerable

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV206

        #region PatchV207

        private static int? PatchV207(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Patch Harrower Summoning Room

                /*
                 * One-time cleanup/build patch for the Harrower summoning room.
                 * Removes legacy misplaced decorative items and rebuilds the teleporter
                 * appearance using static tiles.
                 */

                DeleteItem(0x40015105, typeof(Item)); patched++; // sheet music 0x40015105
                DeleteItem(0x40015101, typeof(Item)); patched++; // music stand 0x40015101
                DeleteItem(0x40015106, typeof(Item)); patched++; // moongate appearance
                DeleteItem(0x40015100, typeof(Item)); patched++; // other music stand

                // teleporter appearance
                AddStatic(0x17EA, new Point3D(5151, 1759, 0), Map.Felucca); patched++;
                AddStatic(0x17EB, new Point3D(5151, 1760, 0), Map.Felucca); patched++;
                AddStatic(0x17ED, new Point3D(5151, 1761, 0), Map.Felucca); patched++;
                AddStatic(0x17F0, new Point3D(5152, 1761, 0), Map.Felucca); patched++;
                AddStatic(0x17F2, new Point3D(5153, 1761, 0), Map.Felucca); patched++;
                AddStatic(0x17F1, new Point3D(5153, 1760, 0), Map.Felucca); patched++;
                AddStatic(0x17EF, new Point3D(5153, 1759, 0), Map.Felucca); patched++;
                AddStatic(0x17EC, new Point3D(5152, 1759, 0), Map.Felucca); patched++;
                AddStatic(0x17EE, new Point3D(5152, 1760, 0), Map.Felucca); patched++;

                EnableTeleporter(0x4001849B); patched++;
                EnableTeleporter(0x400184A5); patched++;
                EnableTeleporter(0x400184CB); patched++;
                EnableTeleporter(0x400184EB); patched++;

                ChangeName(0x40119CAF, "Enlightenment"); patched++; // Oaks is the skull of Enlightenment

                #endregion Patch Harrower Summoning Room

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV207

        #region PatchV208

        private static int? PatchV208(int patchid)
        {
            CoreAI.PatchIndex bits = CoreAI.PatchIndex.PatchV208;
            if (Seasons(2) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Enable WaxPot on Beekeepers

                if (!OwnerTools.AddVendorInventoryEntry("WaxPot", out string reason))
                    ErrorOut(bits, "Problem adding WaxPot to vendor inventory. Reason: {0}", ConsoleColor.Red, reason);
                else
                    patched++;
                // now respawn the beekeepers
                patched += OwnerTools.RespawnMobileType(typeof(Beekeeper), Map.Felucca);

                #endregion Enable WaxPot on Beekeepers

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV208

        #region PatchV209

        private static int? PatchV209(int patchid)
        {
            if (Seasons(2) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Respawn to ensure new 'balanced' weapon drops

                var result = TotalRespawnCommand.TotalRespawn(null);
                patched = result.TotalCount;

                #endregion Respawn to ensure new 'balanced' weapon drops

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV209

        #region PatchV210

        private static int? PatchV210(int patchid)
        {
            if (Seasons(2) && !LoginServer(quiet: true))
            {

                int patched = 0;

                #region Begin Implementation

                #region Turn on guards
                // we are no longer using this GuardsDisabledUntil mechanism for Territory control
                foreach (Region region in Region.Regions)
                {
                    if (region is StaticRegion sRegion)
                    {
                        sRegion.GuardsDisabledUntil = DateTime.MinValue;
                        patched++;
                    }
                }
                #endregion Turn on guards

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV210

        #region PatchV211

        private static int? PatchV211(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Turn on guards
                // we are no longer using this GuardsDisabledUntil mechanism for Territory control
                foreach (Region region in Region.Regions)
                {
                    if (region is StaticRegion sRegion)
                    {
                        sRegion.GuardsDisabledUntil = DateTime.MinValue;
                        patched++;
                    }
                }
                #endregion Turn on guards

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV211

        #region PatchV212

        private static int? PatchV212(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Repair custom region controllers

                foreach (Region region in Region.Regions)
                {
                    if (region is not CustomRegion cRegion || cRegion.Name == null)
                        continue;

                    CustomRegionControl controller = cRegion.Controller;
                    if (controller == null)
                        continue;

                    if (HasLeadingOrTrailingWhitespace(controller.CustomRegion.Name))
                    {
                        controller.CustomRegion.Name = controller.CustomRegion.Name.Trim();
                        patched++;
                    }

                    if (controller.CustomRegion.Name == "Moonglow Cemetary")
                        controller.CustomRegion.Name = "Moonglow Cemetery";
                }

                #endregion Repair custom region controllers

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV212

        #region PatchV213

        private static int? PatchV213(int patchid)
        {
            if (Seasons(2) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Repair custom region controllers

                // Previous versions of TerritorySystem may have changed the notion of EnableGuards.
                // We tear down, repair, then restore the system.
                bool territoriesConfigured = TerritorySystem.AreTerritoriesConfigured();
                if (territoriesConfigured)
                {
                    TerritorySystem.TearDownTerritories(null);
                }

                // Define how each protected region should be handled
                var regionActions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Broken ML Map",          "Disable"}, // ML client map errors
                    { "Minoc Safe Haven",       "Keep" },   // old AI event spot? Seems ok to keep 
                    { "Invasion Spawn",         "Disable" },// accidentally set to guarded
                    { "Vesper Cemetery",        "Disable" },// accidentally set to guarded
                    { "Baron's Office",         "Remove" }, // Cove event region (normally covered by cove guards anyway)
                    { "Oc'Nivelle",             "Disable" },// AI special, never guarded
                    { "Nujel'm Odd Spots",      "Keep" },   // Odd spots not covered by the greater guard protection
                    { "The Palm Grove",         "Disable" },// old AI dueling arena
                    { "Memorial of the fallen", "Keep" },   // player memorial
                    { "Fire Temple",            "Disable" },// accidentally set to guarded
                    { "Eiffel Island",          "Disable" },// accidentally set to guarded
                    { "Britannia Royal Zoo",    "Keep" },
                    { "Moonglow",               "Keep" },
                    { "Moonglow Cemetery",      "Disable" },// accidentally set to guarded
                    { "Nujel'm",                "Keep" },
                    { "Skara Brae",             "Keep" },
                    { "Yew",                    "Keep" },
                    { "Vesper",                 "Keep" },
                    { "Trinsic",                "Keep" },
                    { "Minoc",                  "Keep" },
                    { "Britain",                "Keep" },
                    { "Cove",                   "Keep" }
                };

                foreach (Region region in Region.Regions)
                {
                    if (region is not CustomRegion cRegion || cRegion.Name == null)
                        continue;

                    if (!regionActions.TryGetValue(cRegion.Name, out string action))
                        continue;

                    CustomRegionControl controller = cRegion.Controller;
                    if (controller == null)
                        continue;

                    switch (action.ToLowerInvariant())
                    {
                        case "keep":
                            // Do nothing
                            break;

                        case "disable":
                            if (controller.CustomRegion.EnableGuards)
                            {
                                controller.CustomRegion.EnableGuards = false;
                                patched++;
                            }
                            break;

                        case "remove":
                            controller.CustomRegion.Registered = false;
                            controller.Delete();           // Delete the controller
                            patched++;
                            break;
                    }
                }

                Console.WriteLine($"Repaired {patched} custom region controllers.");

                if (territoriesConfigured)
                {
                    TerritorySystem.SetupTerritories(null);
                }

                #endregion Repair custom region controllers

                #region Respawn to ensure limited magical enhancements

                var result = TotalRespawnCommand.TotalRespawn(null);
                patched = result.TotalCount;

                #endregion Respawn to ensure limited magical enhancements

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV213

        #region PatchV214

        private static int? PatchV214(int patchid)
        {
            if (Seasons(2) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Turn off event champ spawners
                // Disable specific Champion Engines
                int[] serialsToDisable =
                {
                    0x400BAC15, // Gargoyle Queen
                    0x401B091B  // Ocllo Pirates
                };

                foreach (int serial in serialsToDisable)
                {
                    if (World.FindItem(serial) is ChampEngine engine)
                    {
                        engine.Running = false;
                        engine.ClearMonsters = true;
                        patched++;
                        Console.WriteLine($"Disabled champion spawn with serial 0x{serial:X}");
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Could not find ChampEngine with serial 0x{serial:X}");
                    }
                }

                #endregion Turn off event champ spawners

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV214

        #region PatchV215

        private static int? PatchV215(int patchid)
        {
            if (Seasons() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Patch Static Housing Default Storage

                if (Core.RuleSets.SeasonsRules())
                {
                    const string regionName = "Custom Housing Area";

                    StaticRegion trammelRegion = Region.FindByName(regionName, Map.Trammel) as StaticRegion;
                    StaticRegion feluccaRegion = Region.FindByName(regionName, Map.Felucca) as StaticRegion;

                    foreach (BaseHouse house in BaseHouse.Instances.Keys)
                    {
                        if (!(house is StaticHouse staticHouse))
                            continue;

                        bool inCustomHousingArea =
                            (trammelRegion != null && staticHouse.Map == Map.Trammel && trammelRegion.Contains(staticHouse.Location)) ||
                            (feluccaRegion != null && staticHouse.Map == Map.Felucca && feluccaRegion.Contains(staticHouse.Location));

                        if (!inCustomHousingArea)
                            continue;

                        StaticHouseHelper.HouseBlueprint blueprint = StaticHouseHelper.GetBlueprint(staticHouse.HouseBlueprintID);

                        if (blueprint == null)
                            continue;

                        var storage = StaticHouse.GetClassicStorage(blueprint.Width, blueprint.Height);

                        staticHouse.MaxLockDownsRaw = storage.Lockdowns;
                        staticHouse.MaxSecureContainersRaw = storage.Secures;
                        staticHouse.MaxLockboxesRaw = storage.Lockboxes;
                    }
                }

                #endregion Patch Static Housing Default Storage

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV215

        #region PatchV216

        private static int? PatchV216(int patchid)
        {
            if (Seasons() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Give the detective some teeth - we will scale back if need be

                CoreAI.TrackArrowRangeMultiplier = 10; patched++;
                CoreAI.TrackHintRangeMultiplier = 20; patched++;

                #endregion Give the detective some teeth - we will scale back if need be

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV216

        #region PatchV217

        private static int? PatchV217(int patchid)
        {
            if (Seasons(2) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Put a stable boy at each dungeon

                Utility.SpawnFlags flags = Utility.SpawnFlags.Concentric | Utility.SpawnFlags.SpawnFar | Utility.SpawnFlags.ForceZ | Utility.SpawnFlags.NoBlock | Utility.SpawnFlags.ClearPath;
                foreach (var dt in TrackWhoGump.DungeonTransitions)
                {
                    foreach (var entrance in dt.Entrances.Where(p => p != Point3D.Zero))
                    {
                        Spawner s = new Spawner();
                        Point3D spawnerLoc = Spawner.GetSpawnPosition(dt.Map, entrance, 20, flags, s);

                        s.ObjectNamesRaw = new ArrayList() { "Stablehand" };
                        s.SpawnerFlags = flags;
                        s.MoveToWorld(spawnerLoc, Map.Felucca);
                    }
                }

                #endregion Put a stable boy at each dungeon

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV217

        #region PatchV218

        private static int? PatchV218(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Set a more reasonable endurance factor for mounts

                CoreAI.MountEnduranceFactor = 2.0; patched++;

                #endregion Set a more reasonable endurance factor for mounts

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV218

        #region PatchV219

        private static int? PatchV219(int patchid)
        {

            if (AllShards() && !Siege() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Spawn some pack horses and pack llamas as followers to other NPCs

                // Format: (x, y, z) Type - comment
                var spawnList = new[]
                {
                    new { Loc = new Point3D(2541, 665, 0),   Type = "Gypsy",      Extras = true },
                    new { Loc = new Point3D(2575, 525, 15),  Type = "Miner",      Extras = true },
                    new { Loc = new Point3D(1420, 854, 0),   Type = "Miner",      Extras = false },
                    new { Loc = new Point3D(854, 1687, 0),   Type = "Brigand",    Extras = false },
                    new { Loc = new Point3D(715, 2234, -3),  Type = "Fisherman",  Extras = false },
                    new { Loc = new Point3D(4420, 1027, -2), Type = "Fisherman",  Extras = false }
                };

                Utility.SpawnFlags extraFlags = Utility.SpawnFlags.Concentric | Utility.SpawnFlags.SpawnFar | Utility.SpawnFlags.ForceZ | Utility.SpawnFlags.NoBlock | Utility.SpawnFlags.ClearPath;

                foreach (var entry in spawnList)
                {
                    // Create the main spawner
                    Spawner main = new Spawner();
                    main.ObjectNamesRaw = new ArrayList { entry.Type };
                    main.Followers = "PackLlama|PackHorse";           // Only on main spawners
                    main.HomeRange = 10;

                    main.MoveToWorld(entry.Loc, Map.Felucca);
                    patched++;

                    // Spawn extras around Gypsy and the first Miner
                    if (entry.Extras)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            Spawner extra = new Spawner();
                            Point3D extraLoc = Spawner.GetSpawnPosition(Map.Felucca, entry.Loc, 20, extraFlags, extra);

                            extra.ObjectNamesRaw = new ArrayList { entry.Type };  // same NPC type
                            extra.HomeRange = 12;

                            extra.MoveToWorld(extraLoc, Map.Felucca);
                            patched++;
                        }
                    }
                }

                #endregion Spawn some pack horses and pack llamas as followers to other NPCs

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV219

        #region PatchV220

        private static int? PatchV220(int patchid)
        {

            if (Seasons(2) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Respawn to replace magic scrolls in treasure chests

                var result = TotalRespawnCommand.TotalRespawn(null);
                patched = result.TotalCount;

                #endregion Respawn to replace magic scrolls in treasure chests

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV220

        #region PatchV221

        private static int? PatchV221(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation
                #region Comments
                #endregion Comments

                #region Cleanup abandoned small forges across all shards

                // Illegal / Extra SmallForgeAddon locations (should be removed)
                Point3D[] IllegalSmallForges = new Point3D[]
                        {
                    //new Point3D(1048, 1390, 0),   //  - orc fort
                    new Point3D(1417, 872, 1),      // illegal
                    new Point3D(1431, 1838, -2),    // illegal
                    new Point3D(2348, 837, 0),      // illegal
                    new Point3D(1461, 839, 0),      // illegal
                    new Point3D(1461, 814, 0),      // illegal
                    new Point3D(1484, 902, 0),
                    new Point3D(1418, 891, 1),
                    new Point3D(1424, 854, 0),
                    new Point3D(1904, 2155, -39),
                    new Point3D(1209, 1942, 0),     // illegal
                    //new Point3D(2530, 642, 0),    // - gypsy camp
                    //new Point3D(5269, 3972, 37),  // - delucia, seems ok
                    new Point3D(2384, 212, 2),      // - house 
                    //new Point3D(959, 859, 1),     // - Yew Shrine
                    new Point3D(666, 1323, 2),      // - House
                    new Point3D(1968, 2066, -40),
                            //new Point3D(1206, 2655, 6)    // - House
                        };


                Map[] maps = { Map.Felucca, Map.Trammel };

                foreach (Point3D loc in IllegalSmallForges)
                {
                    foreach (Map map in maps)
                    {
                        // Find any SmallForgeAddon at this exact location
                        foreach (Item item in map.GetItemsInRange(loc, 0))
                        {
                            if (item is SmallForgeAddon && item.Location == loc && !item.Deleted)
                            {
                                string logMsg = $"[Cleanup] Deleted illegal SmallForgeAddon at {loc} on {map.Name}";
                                Console.WriteLine(logMsg);

                                item.Delete();
                                patched++;
                                break; // only one per location
                            }
                        }
                    }
                }

                string summary = $"Removed {patched} illegal SmallForgeAddons.";
                Console.WriteLine(summary);

                #endregion Cleanup abandoned small forges across all shards

                #region Move one pack llama follower
                // from (1420, 854, 0)
                // to (1181, 1475, 0)
                // Find any spawner at this exact location
                var from = new Point3D(1420, 854, 0);
                var to = new Point3D(1181, 1475, 0);
                foreach (Item item in Map.Felucca.GetItemsInRange(from, 0))
                {
                    if (item is Spawner && item.Location == from && !item.Deleted)
                    {
                        string logMsg = $"[Cleanup] Moving spawner at {from} to {to}";
                        Console.WriteLine(logMsg);
                        (item as Spawner).ScheduleRespawn = true;
                        item.MoveToWorld(to, Map.Felucca);
                        patched++;
                        break; // only one per location
                    }
                }
                #endregion Move one pack llama follower

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV221

        #region PatchV222

        private static int? PatchV222(int patchid)
        {

            if (Seasons() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                Point3D loc = new Point3D(1361, 883, 0);
                Map map = Map.Felucca;

                // Find the existing SkillTeleporter at exact location
                SkillTeleporter skillTP = null;
                foreach (Item item in map.GetItemsInRange(loc, 1))
                {
                    if (item is SkillTeleporter st && !st.Deleted && st.Location == loc)
                    {
                        skillTP = st;
                        break;
                    }
                }

                if (skillTP != null)
                {
                    skillTP.Running = false;
                    patched++;
                    Console.WriteLine($"[Patch V160] Disabled SkillTeleporter at {skillTP.Location}");
                }

                // Create new temporary Teleporter at exact location
                Teleporter newTP = new Teleporter();
                newTP.Location = loc;
                newTP.Map = map;
                newTP.Running = true;

                // Copy destination from the SkillTeleporter
                if (skillTP != null)
                {
                    newTP.MapDest = skillTP.MapDest;
                    newTP.PointDest = skillTP.PointDest;
                }
                else
                {
                    Console.WriteLine("[Patch V160] Warning: SkillTeleporter not found at target location!");
                }

                newTP.MoveToWorld(loc, map);
                patched++;

                Console.WriteLine($"[Patch V160] Added temporary Teleporter at {loc}");

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV222

        #region PatchV223

        private static int? PatchV223(int patchid)
        {
            DateTime Season2End = new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc);
            if (Seasons() && HasExpired(Season2End) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                Point3D loc = new Point3D(1361, 883, 0);
                Map map = Map.Felucca;

                // Restore the original SkillTeleporter
                SkillTeleporter skillTP = null;
                foreach (Item item in map.GetItemsInRange(loc, 1))   // very tight search
                {
                    if (item is SkillTeleporter st && !st.Deleted && st.Location == loc)
                    {
                        skillTP = st;
                        break;
                    }
                }

                if (skillTP != null)
                {
                    skillTP.Running = true;
                    patched++;
                    Console.WriteLine($"[Patch V161] Restored SkillTeleporter at {skillTP.Location}");
                }

                // Delete the temporary Teleporter
                foreach (Item item in map.GetItemsInRange(loc, 1))
                {
                    if (item is Teleporter tp && !tp.Deleted && tp.Location == loc)
                    {
                        tp.Delete();
                        patched++;
                        Console.WriteLine($"[Patch V161] Removed temporary Teleporter at {loc}");
                        break;
                    }
                }

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV223

        #region PatchV224

        private static int? PatchV224(int patchid)
        {

            if (Seasons() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                foreach (Item item in World.Items.Values)
                {
                    if (item is FillableContainer fc)
                    {
                        fc.TotalRespawn = true;
                        patched++;
                    }
                }

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV224

        #region PatchV225

        private static int? PatchV225(int patchid)
        {

            if (Seasons() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Respawn to replace magic scrolls in treasure chests

                var result = TotalRespawnCommand.TotalRespawn(null);
                patched = result.TotalCount;

                #endregion Respawn to replace magic scrolls in treasure chests

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV225

        #region PatchV226

        private static int? PatchV226(int patchid)
        {

            if (Seasons(2) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Newbie all orcish kin masks

                foreach (Item item in World.Items.Values)
                {
                    if (item is OrcishKinMask mask)
                    {
                        mask.LootType = LootType.Newbied;
                        patched++;
                    }
                }

                #endregion Newbie all orcish kin masks

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV226

        #region PatchV227

        private static int? PatchV227(int patchid)
        {

            if (Seasons(2) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Patch all districts to set WasOriginallyGuarded

                foreach (var district in TerritorySystem.GetDistricts())
                {
                    if (!district.WasOriginallyGuarded)  // only set if not already set
                    {
                        district.WasOriginallyGuarded = true;
                        patched++;
                    }
                }

                #endregion Patch all districts to set WasOriginallyGuarded

                #region Patch all orc brutes and miniboses (Khartag)

                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is Khartag khartag)
                    {
                        khartag.BardImmune = false;  // set BardImmune to false for all Khartag instances
                        patched++;
                    }
                    else if (m is OrcBrute orcBrute)
                    {
                        orcBrute.BardImmune = false; // set BardImmune to false for all OrcBrute instances
                        patched++;
                    }
                }
                #endregion Patch all orc brutes and miniboses (Khartag)

                #region Bump all townships to at least ActivityLevel.MEDIUM

                foreach (var candidate in TownshipStone.AllTownshipStones)
                {
                    if (candidate is TownshipStone township)
                    {
                        if (township.ActivityLevel < Server.Township.ActivityLevel.MEDIUM)
                        {
                            township.ActivityLevel = Server.Township.ActivityLevel.MEDIUM;
                            township.WeeksAtThisLevel++;
                            patched++;
                        }
                    }
                }

                #endregion Bump all townships to at least ActivityLevel.MEDIUM

                #region reduce visit costs

                // sanity check
                if (Server.Township.TownshipSettings.NoneToLow == 18)
                {
                    Server.Township.TownshipSettings.NoneToLow /= 2;
                    Server.Township.TownshipSettings.LowToMedium /= 2;
                    Server.Township.TownshipSettings.MediumToHigh /= 2;
                    Server.Township.TownshipSettings.HighToBooming /= 2;
                    patched++;
                }

                #endregion reduce visit costs

                #region Add Jhelom and Wind dungeon as conquerable districts

                Console.WriteLine("Adding districts...");

                // Jhelom - force guarded
                var jhelomRegion = Region.Regions.OfType<StaticRegion>()
                    .FirstOrDefault(r => r.Map == Map.Felucca && Insensitive.Equals(r.Name, "Jhelom"));

                string reason;
                if (jhelomRegion != null)
                {
                    jhelomRegion.EnableGuards = true;

                    if (TerritorySystem.AddDistrict("Jhelom", Map.Felucca, out reason))
                    {
                        Console.WriteLine("✓ Jhelom district added successfully (guarded).");
                        patched++;
                    }
                    else
                        Console.WriteLine($"✗ Failed to add Jhelom: {reason}");
                }
                else
                {
                    Console.WriteLine("✗ Jhelom region not found.");
                }

                // Wind Dungeon (no guards)
                if (TerritorySystem.AddDistrict("Wind Dungeon", Map.Felucca, out reason))
                {
                    Console.WriteLine("✓ Wind Dungeon district added successfully.");
                    patched++;
                }
                else
                    Console.WriteLine($"✗ Failed to add Wind Dungeon: {reason}");

                #endregion Add Jhelom and Wind dungeon as conquerable districts

                #region Configure CreatureAllegiance (Global)

                AlignmentConfig.CreatureAllegiance = CreatureAllegiance.Global;
                patched++;

                #endregion Configure CreatureAllegiance (Global)

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV227

        #region PatchV228

        private static int? PatchV228(int patchid)
        {

            if (Seasons(2) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Patch all existing orcs to set their alignment

                foreach (Mobile mob in World.Mobiles.Values)
                {
                    if (mob is Server.Mobiles.Orc ||
                        mob is OrcCaptain ||
                        mob is OrcishMage ||
                        mob is OrcBomber ||
                        mob is OrcishLord ||
                        mob is OrcBrute ||
                        mob is Khartag)
                    {
                        BaseCreature bc = mob as BaseCreature;

                        if (bc != null && bc.GuildAlignment != AlignmentType.Orc)
                        {
                            bc.GuildAlignment = AlignmentType.Orc;
                            patched++;
                        }
                    }
                }

                #endregion Patch all existing orcs to set their alignment

                #region Clear player aggrression against orcs

                foreach (var pm in PlayerMobile.Instances)
                {
                    if (pm is PlayerMobile && pm.Account is Account acct)
                        acct.ClearOrcEnemy();
                }

                #endregion Clear player aggrression against orcs

                #region Add Ocllo and Magincia as conquerable districts
                Console.WriteLine("Adding districts...");

                string reason;

                // Ocllo (no guards)
                if (TerritorySystem.AddDistrict("Ocllo", Map.Felucca, out reason))
                {
                    Console.WriteLine("✓ Ocllo district added successfully.");
                    patched++;
                }
                else
                    Console.WriteLine($"✗ Failed to add Ocllo: {reason}");

                // Magincia - force guarded
                var maginciaRegion = Region.Regions.OfType<StaticRegion>()
                    .FirstOrDefault(r => r.Map == Map.Felucca && Insensitive.Equals(r.Name, "Magincia"));

                if (maginciaRegion != null)
                {
                    maginciaRegion.EnableGuards = true;

                    if (TerritorySystem.AddDistrict("Magincia", Map.Felucca, out reason))
                    {
                        Console.WriteLine("✓ Magincia district added successfully (guarded).");
                        patched++;
                    }
                    else
                        Console.WriteLine($"✗ Failed to add Magincia: {reason}");
                }
                else
                {
                    Console.WriteLine("✗ Magincia region not found.");
                }

                Console.WriteLine($"Added {patched} districts.");
                #endregion

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV228

        #region PatchV229

        private static int? PatchV229(int patchid)
        {
            CoreAI.PatchIndex bits = CoreAI.PatchIndex.PatchV229;

            if (Seasons(2) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region enable various vendor supplies

                string[] itemsToAdd = new string[]
                {
                    "InteriorDecorator",
                    "TaxidermyKit",
                    "EtchingBook",
                    "EtchingKit",
                    "GlassblowingBook",
                    "SandMiningBook",
                    "Blowpipe",
                    "PotionKegDyeTub",
                    "MasonryBook",
                    "StoneMiningBook",
                    "MalletAndChisel",
                    "RockHammer",
                    "StoneGraver",
                    "WoodEngravingBook",
                    "SquareGraver",
                    "ModestUpgradeContract",
                    "ModerateUpgradeContract",
                    "PremiumUpgradeContract",
                    "LavishUpgradeContract",
                    "DeluxeUpgradeContract",
                    "StorageUpgradeInfo",
                };

                foreach (string item in itemsToAdd)
                {
                    if (!OwnerTools.AddVendorInventoryEntry(item, out string reason))
                        ErrorOut(bits, $"Problem adding {item} to vendor inventory. Reason: {{0}}", ConsoleColor.Red, reason);
                    else
                        patched++;
                }

                #endregion enable various vendor supplies

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV229

        #region PatchV230

        private static int? PatchV230(int patchid)
        {

            if (Seasons(2) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Spawn GypsySeers near existing TownCrier spawners on Felucca

                List<Spawner> townCrierSpawners = new List<Spawner>();

                foreach (Item item in World.Items.Values)
                {
                    if (item is Spawner spawner &&
                        !spawner.Deleted &&
                        spawner.Running &&
                        spawner.Map == Map.Felucca &&
                        spawner.Spawns("TownCrier"))
                    {
                        if (TerritorySystem.FindDistrict(spawner.Region.Name) != null)
                            townCrierSpawners.Add(spawner);
                    }
                }

                foreach (Spawner townCrierSpawner in townCrierSpawners)
                {
                    // Make the patch safe to run more than once.
                    if (HasNearbyGypsySeerSpawner(townCrierSpawner.Location, townCrierSpawner.Map, 5))
                        continue;

                    Point3D location = FindNearbySpawnLocation(
                        townCrierSpawner.Location,
                        townCrierSpawner.Map,
                        2);

                    Spawner seerSpawner = new Spawner
                    {
                        ObjectNamesRaw = new ArrayList { "GypsySeer" },
                        HomeRange = 5,
                        MinDelay = TimeSpan.FromMinutes(30),
                        MaxDelay = TimeSpan.FromMinutes(60),
                        Running = true,
                        ScheduleRespawn = true
                    };

                    seerSpawner.MoveToWorld(location, townCrierSpawner.Map);
                    patched++;
                }

                #endregion Spawn GypsySeers near existing TownCrier spawners on Felucca

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV230

        #region PatchV231

        private static int? PatchV231(int patchid)
        {
            CoreAI.PatchIndex bits = CoreAI.PatchIndex.PatchV231;

            if (Seasons(2) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region enable various vendor supplies

                string[] itemsToAdd = new string[]
                {
                    "PrisonVisitorsTicket",
                };

                foreach (string item in itemsToAdd)
                {
                    if (!OwnerTools.AddVendorInventoryEntry(item, out string reason))
                        ErrorOut(bits, $"Problem adding {item} to vendor inventory. Reason: {{0}}", ConsoleColor.Red, reason);
                    else
                        patched++;
                }

                #endregion enable various vendor supplies

                #region Disable some prison spawn
                // Your prison spawn serial numbers / IDs to disable
                var prisonSpawnIds = new HashSet<Serial>
                {
                    0x40011D38,
                    0x4001B82A,
                    0x4001B838
                    // Add more IDs here as needed
                };

                // Step 2: Loop over instances again (or combine loops) and handle matches
                foreach (var spawner in Spawner.Instances)
                {
                    if (spawner == null) continue;

                    // Check if serial number / ID matches (adjust property if it's .Serial or .Id etc.)
                    if (prisonSpawnIds.Contains(spawner.Serial))  // or spawner.Id, etc.
                    {
                        spawner.RemoveObjects();
                        spawner.Running = false;
                        patched++;
                        Console.WriteLine($"Disabled prison spawner: {spawner.Serial:X8}");
                    }
                }
                #endregion Disable some prison spawn

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV231

        #region PatchV232

        private static int? PatchV232(int patchid)
        {

            if ((Seasons(3) || Upheaval()) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Configure CreatureAllegiance (Global)

                if (AlignmentConfig.CreatureAllegiance != CreatureAllegiance.Global)
                {   // should already be established for season 3
                    AlignmentConfig.CreatureAllegiance = CreatureAllegiance.Global;
                    patched++;
                }

                #endregion Configure CreatureAllegiance (Global)

                #region (Re)Establish Orcish Invasion 
                bool territoriesConfigured = TerritorySystem.AreTerritoriesConfigured();
                if (territoriesConfigured)
                {
                    TerritorySystem.TearDownTerritories(null);
                }
                else
                    TerritoryConfig.Enabled = true;

                TerritorySystem.SetupTerritories(null);

                #endregion (Re)Establish Orcish Invasion 

                #region Spawn GypsySeers near existing TownCrier spawners on Felucca
                if (!DoAnySpawnersSpawn("GypsySeer", Map.Felucca))
                {
                    List<Spawner> townCrierSpawners = new List<Spawner>();

                    foreach (Item item in World.Items.Values)
                    {
                        if (item is Spawner spawner &&
                            !spawner.Deleted &&
                            spawner.Running &&
                            spawner.Map == Map.Felucca &&
                            spawner.Spawns("TownCrier"))
                        {
                            if (TerritorySystem.FindDistrict(spawner.Region.Name) != null)
                                townCrierSpawners.Add(spawner);
                        }
                    }

                    foreach (Spawner townCrierSpawner in townCrierSpawners)
                    {
                        // Make the patch safe to run more than once.
                        if (HasNearbyGypsySeerSpawner(townCrierSpawner.Location, townCrierSpawner.Map, 5))
                            continue;

                        Point3D location = FindNearbySpawnLocation(
                            townCrierSpawner.Location,
                            townCrierSpawner.Map,
                            2);

                        Spawner seerSpawner = new Spawner
                        {
                            ObjectNamesRaw = new ArrayList { "GypsySeer" },
                            HomeRange = 5,
                            MinDelay = TimeSpan.FromMinutes(30),
                            MaxDelay = TimeSpan.FromMinutes(60),
                            Running = true,
                            ScheduleRespawn = true
                        };

                        seerSpawner.MoveToWorld(location, townCrierSpawner.Map);
                        patched++;
                    }
                }
                #endregion Spawn GypsySeers near existing TownCrier spawners on Felucca

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV232

        #region PatchV233

        private static int? PatchV233(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Turn off teleporter to Terathan Keep

                List<Item> items = FindItemsAt(new Point3D(5140, 1773, 0), Map.Felucca);

                foreach (Item item in items)
                {
                    if (item is Teleporter ce && ce.Running)
                    {
                        ce.Running = false;
                        patched++;
                    }
                }

                #endregion Turn off teleporter to Terathan Keep

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV233

        #region PatchV234

        // null

        #endregion PatchV234

        #region PatchV235

        private static int? PatchV235(int patchid)
        {

            if ((Seasons(3) || Upheaval()) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Configure CreatureAllegiance (Global)

                if (AlignmentConfig.CreatureAllegiance != CreatureAllegiance.Global)
                {   // should already be established for season 3
                    AlignmentConfig.CreatureAllegiance = CreatureAllegiance.Global;
                    patched++;
                }

                #endregion Configure CreatureAllegiance (Global)

                #region Configure Orcish Invasion 
                bool territoriesConfigured = TerritorySystem.AreTerritoriesConfigured();

                // Tear Down Orcish Invasion 
                if (territoriesConfigured)
                {
                    TerritorySystem.TearDownTerritories(null);
                    TerritoryConfig.Enabled = false;
                }

                // Establish Orcish Invasion for Upheaval
                if (Upheaval())
                {
                    TerritoryConfig.Enabled = true;
                    TerritorySystem.SetupTerritories(null);
                }

                #endregion Configure Orcish Invasion 

                #region Remove GypsySeer spawners from Felucca
                foreach (Item item in World.Items.Values)
                {
                    if (item is Spawner spawner &&
                        !spawner.Deleted &&
                        spawner.Map == Map.Felucca &&
                        spawner.Spawns("GypsySeer"))
                    {
                        spawner.Delete();
                        patched++;
                    }
                }
                #endregion Remove GypsySeer spawners from Felucca

                #region Spawn GypsySeers near existing TownCrier spawners on Felucca

                if (Upheaval())
                {
                    if (!DoAnySpawnersSpawn("GypsySeer", Map.Felucca))
                    {
                        List<Spawner> townCrierSpawners = new List<Spawner>();

                        foreach (Item item in World.Items.Values)
                        {
                            if (item is Spawner spawner &&
                                !spawner.Deleted &&
                                spawner.Running &&
                                spawner.Map == Map.Felucca &&
                                spawner.Spawns("TownCrier"))
                            {
                                if (TerritorySystem.FindDistrict(spawner.Region.Name) != null)
                                    townCrierSpawners.Add(spawner);
                            }
                        }

                        foreach (Spawner townCrierSpawner in townCrierSpawners)
                        {
                            // Make the patch safe to run more than once.
                            if (HasNearbyGypsySeerSpawner(townCrierSpawner.Location, townCrierSpawner.Map, 5))
                                continue;

                            Point3D location = FindNearbySpawnLocation(
                                townCrierSpawner.Location,
                                townCrierSpawner.Map,
                                2);

                            Spawner seerSpawner = new Spawner
                            {
                                ObjectNamesRaw = new ArrayList { "GypsySeer" },
                                HomeRange = 5,
                                MinDelay = TimeSpan.FromMinutes(30),
                                MaxDelay = TimeSpan.FromMinutes(60),
                                Running = true,
                                ScheduleRespawn = true
                            };

                            seerSpawner.MoveToWorld(location, townCrierSpawner.Map);
                            patched++;
                        }
                    }
                }

                #endregion Spawn GypsySeers near existing TownCrier spawners on Felucca

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV235

        #region PatchV236

        private static int? PatchV236(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                EchoOut("One-time to patch KeyGenerator.bin (rename).", ConsoleColor.Magenta);

                string newPath = "Saves/KeyGenerator.bin";
                string oldPath = "Saves/StartingLootMonitor.bin";

                try
                {
                    if (File.Exists(oldPath))
                    {
                        // If a new-format file already exists, prefer it and just drop the legacy one.
                        if (File.Exists(newPath))
                        {
                            EchoOut("KeyGenerator.bin already exists; deleting legacy StartingLootMonitor.bin.", ConsoleColor.Magenta);
                            File.Delete(oldPath);
                        }
                        else
                        {
                            EchoOut("Renaming StartingLootMonitor.bin -> KeyGenerator.bin.", ConsoleColor.Magenta);
                            File.Move(oldPath, newPath);
                            patched++;
                        }
                    }
                    else
                    {
                        EchoOut("No legacy StartingLootMonitor.bin found; nothing to patch.", ConsoleColor.Magenta);
                    }
                }
                catch (Exception ex)
                {
                    EchoOut($"Error while renaming StartingLootMonitor.bin: {ex.Message}", ConsoleColor.Red);
                }

                EchoOut(string.Format("{0} file renamed.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }

        #endregion PatchV236

        #region PatchV237

        private static int? PatchV237(int patchid)
        {
            int patches = 0;
            CoreAI.PatchIndex bits = CoreAI.PatchIndex.PatchV237;
            // adam, 8/18/22
            // Summary: (PatchV237)
            //  1. Converted all DateTime saved to UTC.
            //  2. Converted all DateTime read to Local
            //  Problem solved: 
            //  The server runs on a computer set as UTC
            //  Our local development machines us Local time
            //  If you move a Saves folder from the server to your local development machine, all saved DateTimes would be in UTC (would mess with debugging DateTime issues, e.g, expirations, etc.)
            //  By explicitly converting to/from UTC, we can safely move Saves from our server to local, and even from local to server. (should we even need to patch something locally.)
            //  PatchV237:
            //  The first time the server is run, this bit will not be set, and all reads will read as Local time (the old default.)
            //  After the first run, (and a save to UTC) this bit will have been set (below). Once set, all future reads will be UTC
            //  When can we remove this code. After we have sufficient backups, we can remove the check in all DateTime *readers* after 9/15/22
            if (AllShards())
            {
                EchoOut(string.Format("Patching all DateTime saves during this run session."), ConsoleColor.Magenta);
                patches++;

                SetDynamicPatch(bits);
                return patches;
            }
            return null;
        }

        #endregion PatchV237

        #region PatchV238

        private static int? PatchV238(int patchid)
        {   // respawn the world now
            CoreAI.PatchIndex bits = CoreAI.PatchIndex.PatchV238;

            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to respawn the world. Respawning...", ConsoleColor.Magenta);

                int patched = 0;

                List<Spawner> spawners = new();
                double nextspawn = 0.5;

                foreach (Item item in World.Items.Values)
                {
                    if (item is Spawner s && s.Deleted == false && s.Running)
                    {
                        // based on our spawner/shard config, should this spawner be respawned?
                        if (Spawner.ShouldShardEnable(s))
                        {
                            patched++;
                            s.ScheduleRespawn = true;
                        }
                    }
                }

                EchoOut(
                    string.Format(
                        "{0} Spawners scheduled. Spawning will complete in {1:0.00} seconds.",
                        patched,
                        nextspawn),
                    ConsoleColor.Magenta);

                return patched;
            }

            return null;
        }

        #endregion PatchV238

        #region PatchV239

        private static int? PatchV239(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                patched = ReservedNames.Seed();

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV239

        #region PatchV240

        private static int? PatchV240(int patchid)
        {

            if (Core.MigratePatchHistory && !LoginServer(quiet: true))
            {
                int patched = 0;

                PatchComplete(CoreAI.PatchIndex.PatchV126, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV187, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV191, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV192, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV193, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV204, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV207, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV212, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV214, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV215, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV216, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV217, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV218, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV219, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV220, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV221, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV222, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV224, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV225, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV226, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV227, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV228, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV229, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV230, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV231, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV233, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV237, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV238, -1); patched++;
                PatchComplete(CoreAI.PatchIndex.PatchV239, -1); patched++;

                ClearDynamicPatch(CoreAI.PatchIndex.PatchV235);
                patched++;

                EchoOut(
                    "Patch history migration complete.",
                    ConsoleColor.Magenta);

                return patched;
            }

            return null;
        }

        #endregion PatchV240

        #region PatchV241

        private static int? PatchV241(int patchid)
        {

            if (AllShards())
            {
                int patched = 0;

                string source = Path.Combine(Core.SavesDirectory, "AngelIsland");
                string destination = Path.Combine(Core.SavesDirectory, Core.Server);

                if (Directory.Exists(source))
                {
                    string[] files = Directory.GetFiles(source);
                    List<string> conflicts = new List<string>();

                    //
                    // Preflight
                    //
                    // Make sure none of the files we're about to migrate
                    // already exist in the destination.
                    //
                    foreach (string file in files)
                    {
                        string target = Path.Combine(
                            destination,
                            Path.GetFileName(file));

                        if (File.Exists(target))
                            conflicts.Add(target);
                    }

                    if (conflicts.Count > 0)
                    {
                        EchoOut(
                            "PatchV241: Save migration aborted. Destination file conflict(s) detected:",
                            ConsoleColor.Red);

                        foreach (string conflict in conflicts)
                            EchoOut("  " + conflict, ConsoleColor.Red);

                        return patched;
                    }

                    //
                    // Migration
                    //
                    Directory.CreateDirectory(destination);

                    bool moveFailed = false;

                    foreach (string file in files)
                    {
                        string target = Path.Combine(
                            destination,
                            Path.GetFileName(file));

                        try
                        {
                            File.Move(file, target);
                            patched++;
                        }
                        catch (Exception ex)
                        {
                            moveFailed = true;

                            EchoOut(
                                string.Format(
                                    "PatchV241: Unable to move '{0}' to '{1}': {2}",
                                    file,
                                    target,
                                    ex.Message),
                                ConsoleColor.Red);
                        }
                    }

                    //
                    // Only remove the old directory if every file moved.
                    //
                    if (!moveFailed)
                    {
                        try
                        {
                            Directory.Delete(source);
                        }
                        catch (Exception ex)
                        {
                            EchoOut(
                                string.Format(
                                    "PatchV241: Files migrated, but unable to remove '{0}': {1}",
                                    source,
                                    ex.Message),
                                ConsoleColor.Yellow);
                        }
                    }
                    else
                    {
                        EchoOut(
                            string.Format(
                                "PatchV241: Migration incomplete. {0} has been left in place.",
                                source),
                            ConsoleColor.Red);
                    }
                }

                EchoOut(
                    string.Format(
                        "Data migration complete. {0} file{1} moved from Saves\\AngelIsland to Saves\\{2}.",
                        patched,
                        patched == 1 ? "" : "s",
                        Core.Server),
                    ConsoleColor.Magenta);

                return patched;
            }

            return null;
        }

        #endregion PatchV241

        #region PatchV242

        private static int? PatchV242(int patchid)
        {

            if (AllShards())
            {
                int patched = 0;

                // Establish the default login-server visibility for all game shards.
                //
                // Administrative is intentionally not represented by a visibility
                // feature bit. It remains Owner-IP-only by design.

                // Public shards
                CoreAI.SetDynamicFeature(CoreAI.FeatureBits.AngelIslandVisible);
                patched++;

                CoreAI.SetDynamicFeature(CoreAI.FeatureBits.SiegePerilousVisible);
                patched++;

                CoreAI.SetDynamicFeature(CoreAI.FeatureBits.MortalisVisible);
                patched++;

                CoreAI.SetDynamicFeature(CoreAI.FeatureBits.SeasonsVisible);
                patched++;

                // Non-public shards
                CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.TestCenterVisible);
                patched++;

                CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.UpheavalVisible);
                patched++;

                EchoOut(
                    "Shard visibility initialized: Angel Island, Siege Perilous, Mortalis, " +
                    "and UO Seasons visible; Test Center and Upheaval hidden; " +
                    "Administrative remains Owner-only.",
                    ConsoleColor.Magenta);

                return patched;
            }

            return null;
        }

        #endregion PatchV242

        #region PatchV243

        private static int? PatchV243(int patchid)
        {

            if (Upheaval() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Configure CreatureAllegiance (Global)

                if (AlignmentConfig.CreatureAllegiance != CreatureAllegiance.Global)
                {
                    AlignmentConfig.CreatureAllegiance = CreatureAllegiance.Global;
                    patched++;
                }

                #endregion Configure CreatureAllegiance (Global)

                #region Configure Orcish Invasion

                bool territoriesConfigured = TerritorySystem.AreTerritoriesConfigured();

                // Tear down existing Orcish Invasion.
                if (territoriesConfigured)
                {
                    TerritorySystem.TearDownTerritories(null);
                    TerritoryConfig.Enabled = false;
                }

                // Re-establish Orcish Invasion for Upheaval.
                TerritoryConfig.Enabled = true;
                TerritorySystem.SetupTerritories(null);

                #endregion Configure Orcish Invasion

                #region Remove GypsySeer spawners from Felucca

                foreach (Item item in World.Items.Values)
                {
                    if (item is Spawner spawner &&
                        !spawner.Deleted &&
                        spawner.Map == Map.Felucca &&
                        spawner.Spawns("GypsySeer"))
                    {
                        spawner.Delete();
                        patched++;
                    }
                }

                #endregion Remove GypsySeer spawners from Felucca

                #region Spawn GypsySeers near existing TownCrier spawners on Felucca

                if (!DoAnySpawnersSpawn("GypsySeer", Map.Felucca))
                {
                    List<Spawner> townCrierSpawners = new List<Spawner>();

                    foreach (Item item in World.Items.Values)
                    {
                        if (item is Spawner spawner &&
                            !spawner.Deleted &&
                            spawner.Running &&
                            spawner.Map == Map.Felucca &&
                            spawner.Spawns("TownCrier"))
                        {
                            if (TerritorySystem.FindDistrict(spawner.Region.Name) != null)
                                townCrierSpawners.Add(spawner);
                        }
                    }

                    foreach (Spawner townCrierSpawner in townCrierSpawners)
                    {
                        // Make the patch safe to run more than once.
                        if (HasNearbyGypsySeerSpawner(
                            townCrierSpawner.Location,
                            townCrierSpawner.Map,
                            5))
                        {
                            continue;
                        }

                        Point3D location = FindNearbySpawnLocation(
                            townCrierSpawner.Location,
                            townCrierSpawner.Map,
                            2);

                        Spawner seerSpawner = new Spawner
                        {
                            ObjectNamesRaw = new ArrayList { "GypsySeer" },
                            HomeRange = 5,
                            MinDelay = TimeSpan.FromMinutes(30),
                            MaxDelay = TimeSpan.FromMinutes(60),
                            Running = true,
                            ScheduleRespawn = true
                        };

                        seerSpawner.MoveToWorld(location, townCrierSpawner.Map);
                        patched++;
                    }
                }

                #endregion Spawn GypsySeers near existing TownCrier spawners on Felucca

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV243

        #region PatchV244
        private static int? PatchV244(int patchid)
        {

            if ((Seasons() || Upheaval()) && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                // Restore authoritative guardedness from Data/StaticRegions.xml.
                // TerritoryControl previously disabled guards without preserving
                // the original RegionFlag.EnableGuards state.
                List<StaticRegion> liveRegions = new List<StaticRegion>(StaticRegion.XmlDatabase);

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(StaticRegion.DataFileName);

                XmlElement xmlRoot = xmlDoc["StaticRegions"];

                if (xmlRoot == null)
                {
                    Console.WriteLine("[PatchV244] StaticRegions root not found.");
                    return patched;
                }

                foreach (XmlElement xmlElem in xmlRoot.GetElementsByTagName("region"))
                {
                    if (xmlElem.GetAttribute("type") != "StaticRegion")
                        continue;

                    // Load the authoritative Data definition into a detached region.
                    StaticRegion dataRegion = new StaticRegion();
                    dataRegion.Load(xmlElem);

                    int index = liveRegions.FindIndex(r =>
                        r.Map == dataRegion.Map &&
                        Insensitive.Equals(r.Name, dataRegion.Name));

                    if (index == -1)
                        continue;

                    StaticRegion liveRegion = liveRegions[index];

                    bool dataGuarded =
                        dataRegion.GetFlag(RegionFlag.EnableGuards);

                    bool liveGuarded =
                        liveRegion.GetFlag(RegionFlag.EnableGuards);

                    if (liveGuarded != dataGuarded)
                    {
                        liveRegion.SetFlag(
                            RegionFlag.EnableGuards,
                            dataGuarded);

                        patched++;

                        Console.WriteLine(
                            "[PatchV244] Restored guardedness: {0} | {1} -> {2}",
                            liveRegion.Map,
                            liveRegion.Name,
                            dataGuarded ? "Guarded" : "Unguarded");
                    }

                    // Prevent a duplicate Map+Name definition from matching
                    // this same live region a second time.
                    liveRegions.RemoveAt(index);
                }

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV244

        #region PatchV245
        private static int? PatchV245(int patchid)
        {

            if (!LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                ScrubReport report = WorldScrubber.Scrub(
                    new string[]
                    {
                        "Server.Engines.Invasion.InvasionPersistence",
                        "Server.Engines.Invasion.InvasionSpawner",
                        "Server.Engines.Invasion.UndeadInvasionConsole",
                        "Server.Engines.Invasion.TheGuardianConsole",

                        "Server.Engines.Invasion.TheGuardian",
                        "Server.Engines.Invasion.TheGuardianTentacles",
                        "Server.Engines.Invasion.WraithRiderMage",
                        "Server.Engines.Invasion.WraithRiderWarrior"
                    });

                foreach (ScrubTypeResult type in report.Types)
                    patched += type.ItemRecordsRemoved + type.MobileRecordsRemoved;

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV245

        #region PatchV246
        private static int? PatchV246(int patchid)
        {

            if (!LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                // PreWorldLoad: remove the persisted objects of the retired Wild Hunt system from the cold world save.
                ScrubReport report = WorldScrubber.Scrub(
                    new string[]
                    {
                        "Server.Mobiles.WildHunt.Astrid",
                        "Server.Mobiles.WildHunt.Bjorn",
                        "Server.Mobiles.WildHunt.Brynhild",
                        "Server.Mobiles.WildHunt.Odin",
                        "Server.Mobiles.WildHunt.WildHuntWarrior",
                        "Server.Mobiles.WildHunt.WildHuntMage",
                        "Server.Mobiles.WildHunt.WildHuntHorse",
                        "Server.Mobiles.WildHunt.WildHuntCharger",
                        "Server.Mobiles.WildHunt.Sleipnir",
                        "Server.Mobiles.WildHunt.OdinRaven",

                        "Server.Mobiles.WildHunt.FallenSoldier"
                    });

                foreach (ScrubTypeResult type in report.Types)
                    patched += type.ItemRecordsRemoved + type.MobileRecordsRemoved;

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV246

        #region PatchV247
        private static int? PatchV247(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Remove retired Wild Hunt spawn entries
                // PatchV246 scrubbed the persisted Wild Hunt objects before World.Load, but surviving spawners
                //  (including EventSpawners) may still name those types in their spawn lists.
                // Only the retired names are removed: a whole entry when all of its ':' alternatives are retired
                //  (with its count slot in multi/Nerun's mode), otherwise just the retired alternatives.
                //  Spawners are never deleted; one left with no entries is reported (and simply spawns nothing).
                // Names are matched as spawners have resolved them: short or fully-qualified, ignoring case.
                //  A name that still resolves (SpawnerType.GetType) is never removed.
                HashSet<string> retired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string name in new string[] { "Astrid", "Bjorn", "Brynhild", "Odin", "OdinRaven", "Sleipnir",
                    "WildHuntCharger", "WildHuntHorse", "WildHuntMage", "WildHuntWarrior", "FallenSoldier" })
                {
                    retired.Add(name);
                    retired.Add("Server.Mobiles.WildHunt." + name);
                }

                int spawners = 0, emptied = 0;
                foreach (Item item in World.Items.Values)
                    if (item is Spawner spawner && spawner.ObjectNamesRaw != null && spawner.ObjectNamesRaw.Count > 0)
                    {
                        ArrayList names = new ArrayList();
                        List<int> removedSlots = new List<int>();
                        int removed = 0;

                        for (int ix = 0; ix < spawner.ObjectNamesRaw.Count; ix++)
                        {
                            if (spawner.ObjectNamesRaw[ix] is not string entry)
                            {
                                names.Add(spawner.ObjectNamesRaw[ix]);
                                continue;
                            }

                            string[] tokens = entry.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            List<string> keep = new List<string>();
                            foreach (string token in tokens)
                                if (retired.Contains(token) && SpawnerType.GetType(token) == null)
                                    removed++;
                                else
                                    keep.Add(token);

                            if (keep.Count == tokens.Length)
                                names.Add(entry);                           // nothing retired: keep the entry verbatim
                            else if (keep.Count > 0)
                                names.Add(string.Join(":", keep));          // keep the surviving alternatives
                            else
                                removedSlots.Add(ix);                       // the entire entry was Wild Hunt
                        }

                        if (removed == 0)
                            continue;

                        string before = string.Format("[{0}] counts [{1}]", string.Join(", ", spawner.ObjectNamesRaw.Cast<object>()), spawner.Counts);

                        // multi/Nerun's spawners keep one count slot per entry
                        if (removedSlots.Count > 0 && (spawner.ModeMulti || spawner.ModeNeruns))
                        {
                            List<string> slots = spawner.Counts.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                            for (int i = removedSlots.Count - 1; i >= 0; i--)
                                if (removedSlots[i] < slots.Count)
                                    slots.RemoveAt(removedSlots[i]);

                            spawner.WipeSlots();
                            if (slots.Count > 0)
                                spawner.Counts = string.Join(", ", slots);
                        }

                        spawner.ObjectNamesRaw = names;                     // setter stops the timer if no entries remain

                        spawners++;
                        patched += removed;
                        if (names.Count == 0)
                            emptied++;

                        EchoOut(string.Format("{0} at {1} ({2}): {3} => [{4}] counts [{5}]{6}",
                            spawner, spawner.Location, spawner.Map, before, string.Join(", ", names.Cast<object>()), spawner.Counts,
                            names.Count == 0 ? " (no entries left; spawner kept)" : ""), ConsoleColor.Magenta);
                    }

                EchoOut(string.Format("{0} retired Wild Hunt spawn name(s) removed from {1} spawner(s); {2} spawner(s) left with no entries.",
                    patched, spawners, emptied), ConsoleColor.Magenta);
                #endregion Remove retired Wild Hunt spawn entries

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV247

        #region PatchV248
        private static int? PatchV248(int patchid)
        {

            if (!LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                // PreWorldLoad: remove the persisted objects of the retired Kin Ransom feature from the cold world save.
                ScrubReport report = WorldScrubber.Scrub(
                    new string[]
                    {
                        "Server.KinRansomAES",
                        "Server.Items.KinRansomChest"
                    });

                foreach (ScrubTypeResult type in report.Types)
                    patched += type.ItemRecordsRemoved + type.MobileRecordsRemoved;

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV248

        #region PatchV249
        private static int? PatchV249(int patchid)
        {

            if (Upheaval() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Fortify and newbie Orcish Kin Masks
                // Since r1162, Upheaval character creation gives the starting mask the same x10 durability
                //  boost as the rest of the starting equipment (400-500 HP). Bring all existing masks to parity:
                //  boost the ones that need it, and newbie the ones that are not already protected.
                // Masks still held by uncontrolled creatures (NPC spawn loot) are left alone so they still drop.
                // A mask is unboosted when MaxHitPoints <= InitMaxHits (constructed at 40-50, wear only lowers it).
                //  A boosted mask starts at 400+, and its MaxHitPoints only falls after its HitPoints reach 0
                //  (masks cannot be repaired), so an unboosted mask must also have HitPoints > 0.
                //  Anything else (boosted, worn out, or staff-edited) is not boosted, so a rerun changes nothing.
                // Both values are scaled by 10, which is the starting-equipment boost for an unworn mask and keeps
                //  the wear proportion on a worn one (no free repair).
                // A mask needs newbie unless it is already both unstealable and unlootable (Newbied, Blessed,
                //  SiegeBlessed), so stronger protection is never downgraded.
                const int multiplier = 10;
                const LootType protectedFlags = LootType.UnStealable | LootType.UnLootable;
                int boosted = 0, newbied = 0, wornOut = 0;

                foreach (Item item in World.Items.Values)
                {
                    if (item is not OrcishKinMask mask || mask.Deleted)
                        continue;

                    if (mask.RootParent is BaseCreature bc && !bc.Controlled)
                        continue;

                    bool changed = false;

                    if (mask.MaxHitPoints > 0 && mask.MaxHitPoints <= mask.InitMaxHits)
                    {
                        if (mask.HitPoints > 0)
                        {
                            int hits = Math.Min(mask.HitPoints, mask.MaxHitPoints);
                            mask.MaxHitPoints = mask.MaxHitPoints * multiplier;
                            mask.HitPoints = hits * multiplier;
                            boosted++;
                            changed = true;
                        }
                        else
                            wornOut++;      // worn out: cannot prove it was never boosted
                    }

                    if ((mask.LootType & protectedFlags) != protectedFlags)
                    {
                        mask.LootType = LootType.Newbied;
                        newbied++;
                        changed = true;
                    }

                    if (changed)
                        patched++;
                }

                EchoOut(string.Format("{0} Orcish Kin Mask(s) updated: {1} fortified, {2} newbied; {3} worn-out mask(s) not fortified.",
                    patched, boosted, newbied, wornOut), ConsoleColor.Magenta);
                #endregion Fortify and newbie Orcish Kin Masks

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV249

        #region PatchV250
        private static int? PatchV250(int patchid)
        {

            if (!AngelIsland(quiet: true) && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Unlock the Wrong SecretDungeonDoor at 5679 520
                // The old BasePrisonerCamp.ManageLockTick() locked every BaseDoor within 6 tiles of the guard, so the
                //  WrongExecuteNoble camp at 5680 523 also locked this unrelated secret door (on both facets).
                //  Fixed in BasePrisonerCamp (only the camp's own BarredMetalDoors). Angel Island, which does not
                //  have the prisoner camps, shows the intended state: Locked == false. Only Locked is restored.
                Point3D location = new Point3D(5679, 520, 0);
                foreach (Map map in new Map[] { Map.Felucca, Map.TestCenter /*Trammel*/ })
                    foreach (Item item in FindItemsAt(location, map))
                        if (item is SecretDungeonDoor door && door.Locked)
                        {
                            door.Locked = false;
                            patched++;
                            EchoOut(string.Format("Unlocked {0} at {1} ({2}).", door, door.Location, door.Map), ConsoleColor.Magenta);
                        }
                #endregion Unlock the Wrong SecretDungeonDoor at 5679 520

                #region Retarget the Wrong PortKey pad
                // The nine teleporters of the PortKey pad around the PortalManager at 5868 527 15 now all go to
                //  5700 628 0. Their old destinations differ by shard, so they are identified by the pad itself:
                //  a plain Teleporter (not a subclass) with a single destination on one of the nine pad tiles, on a
                //  facet that has the PortalManager. Only PointDest changes: Running belongs to the PortalManager,
                //  and MapDest is kept (null on Felucca, meaning the traveler's facet; Trammel on Trammel).
                Point3D padCenter = new Point3D(5868, 527, 15);
                Point3D padDest = new Point3D(5700, 628, 0);
                foreach (Map map in new Map[] { Map.Felucca, Map.TestCenter /*Trammel*/ })
                {
                    if (!FindItemsAt(padCenter, map).Any(item => item is PortalManager))
                        continue;

                    for (int x = padCenter.X - 1; x <= padCenter.X + 1; x++)
                        for (int y = padCenter.Y - 1; y <= padCenter.Y + 1; y++)
                        {
                            // the center tile's teleporter sits at z 16 (under the PortalManager), the rest at z 15
                            Point3D tile = new Point3D(x, y, (x == padCenter.X && y == padCenter.Y) ? 16 : 15);
                            foreach (Item item in FindItemsAt(tile, map))
                                if (item.GetType() == typeof(Teleporter) && item is Teleporter tp && tp.PointDest != padDest
                                    && tp.PointDest2 == Point3D.Zero && tp.PointDest3 == Point3D.Zero
                                    && tp.PointDest4 == Point3D.Zero && tp.PointDest5 == Point3D.Zero)
                                {
                                    Point3D old = tp.PointDest;
                                    tp.PointDest = padDest;
                                    patched++;
                                    EchoOut(string.Format("Retargeted {0} at {1} ({2}) from {3} to {4}.", tp, tp.Location, tp.Map, old, padDest), ConsoleColor.Magenta);
                                }
                        }
                }
                #endregion Retarget the Wrong PortKey pad

                #region Reset the Wrong North SecretDungeonDoors
                // The secret doors into the east wing (5722 553) and the west wing (5652 552) carry stale key values
                //  (and the west one is locked) that no saved key matches. Restore them to the Angel Island state:
                //  unlocked, no key value. The barred prisoner gates and their camp keys are not touched.
                foreach (Point3D secretDoor in new Point3D[] { new Point3D(5722, 553, 20), new Point3D(5652, 552, 20) })
                    foreach (Map map in new Map[] { Map.Felucca, Map.TestCenter /*Trammel*/ })
                        foreach (Item item in FindItemsAt(secretDoor, map))
                            if (item is SecretDungeonDoor door && (door.Locked || door.KeyValue != 0))
                            {
                                string before = string.Format("Locked={0} KeyValue=0x{1:X8}", door.Locked, door.KeyValue);
                                door.Locked = false;
                                door.KeyValue = 0;
                                patched++;
                                EchoOut(string.Format("Reset {0} at {1} ({2}): {3} => Locked=False KeyValue=0x00000000.", door, door.Location, door.Map, before), ConsoleColor.Magenta);
                            }
                #endregion Reset the Wrong North SecretDungeonDoors

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV250

        #region PatchV251
        private static int? PatchV251(int patchid)
        {

            if (!LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Synchronize custom house deeds on staff-owned player vendors
                // A staff vendor's StaticDeed listing holds the price and description it was stocked with, while the
                //  deed's own Price is read from its blueprint, so listings can go stale (e.g., deeds stocked on another
                //  shard). OwnerTools.RepriceCustomHousingDeeds (shared with [RepriceHousingVendors and [PreLaunch)
                //  updates each listing in place, never touches classic house deeds, and logs and deletes deeds whose
                //  blueprint is missing (PriceError). The run, with its summary, is always written to HousingVendorSync.log.
                int repriced = OwnerTools.RepriceCustomHousingDeeds("PatchV251",
                    out int vendors, out int examined, out int invalidDeleted);
                patched = repriced + invalidDeleted;
                EchoOut(string.Format("Scanned {0} staff-owned player vendors, examined {1} custom house deeds: {2} repriced, {3} invalid deeds deleted.",
                    vendors, examined, repriced, invalidDeleted), ConsoleColor.Magenta);
                #endregion Synchronize custom house deeds on staff-owned player vendors

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV251

        #region PatchV252
        private static int? PatchV252(int patchid)
        {

            if (Upheaval() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Reconcile music ownership from account GUIDs
                // Upheaval's music library came from another shard: its entries carry the composers' account GUIDs
                //  (MusicInfo.OwnerGUID) but no local owner, so MusicBoxMaintenance has zeroed their prices.
                //  PatchV184 (Mortalis only) does not apply here, and its copy scan skips every copy of a library song
                //  (RolledUpSheetMusic equality is by HashCode). MusicOwnershipReconciler binds library entries, their
                //  masters, and copies to the local character of the account with that GUID, recovers the GUID of legacy
                //  copies from a unique library match, and restores library prices from their masters. Character
                //  serials are never used as identity; unresolved and ambiguous objects are left unchanged.
                //  Rerun-safe: a second run changes nothing. Every run is written to MusicOwnership.log.
                //  If validation fails nothing is changed and the patch is left incomplete (returns null) for review.
                MusicOwnershipReconciler.Result result = MusicOwnershipReconciler.Reconcile(apply: true, "PatchV252");
                if (result.Failed)
                {
                    EchoOut(result.Summary + " See MusicOwnership.log.", ConsoleColor.Red);
                    return null;
                }
                patched = result.Changes;
                EchoOut(result.Summary, ConsoleColor.Magenta);
                #endregion Reconcile music ownership from account GUIDs

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV252

        #region PatchV253
        private static int? PatchV253(int patchid)
        {

            if (!LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Regenerate treasure map locations
                // The cached locations (Data\Angel Island Treasure.json) were generated with spawner anchors added without
                //  any terrain check and nearby locations validated at a Z the stored Point2D does not keep, so overland
                //  encounters and treasure chests could land in the sea, under docks, or inside buildings. The corrected
                //  generator validates every anchor and location as walkable, non-water land at GetAverageZ, uses all
                //  Felucca spawners outside Town and Guarded regions (running or not), and excludes the Moongates
                //  (and, temporarily, the Jhelom town area). Runs after the world, spawners, houses and regions are loaded.
                //  TreasureMapLocations.Regenerate writes the new file beside the old, reads it back, swaps it in (the old
                //  file is kept as .pre-PatchV253), and updates TreasureMap.Locations. Decoded treasure maps keep their
                //  chest locations. On failure nothing changes and the patch stays incomplete (returns null) to retry at
                //  the next startup. Every run is written to TreasureLocations.log.
                if (!TreasureMapLocations.Regenerate("PatchV253", out string summary))     // Generate prints the progress message
                {
                    EchoOut(summary + " See TreasureLocations.log.", ConsoleColor.Red);
                    return null;
                }
                patched = TreasureMap.Locations.Length;
                EchoOut(summary, ConsoleColor.Magenta);
                #endregion Regenerate treasure map locations

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV253

        #region PatchV254
        private static int? PatchV254(int patchid)
        {

            if (!LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Relocate misplaced overland NPCs
                // Overland bandits, merchants and treasure hunters spawned before PatchV253 may stand in the sea, inside
                //  buildings, or in confined spots. OverlandRelocator moves every one whose placement is invalid or
                //  uncertain to the nearest treasure map location that is itself valid (standable land, open ground
                //  reachable on foot or through a door), and moves its Home with it. Spawner-controlled and escorted NPCs,
                //  and treasure hunters whose quest has started, are exempt. Valid placements (including buildings with a
                //  reachable door) are kept. Runs after PatchV253 has regenerated the locations. Rerun-safe: a second run
                //  changes nothing. Every run is written to OverlandRelocation.log.
                Server.Engines.OverlandSpawner.OverlandRelocator.Result result = Server.Engines.OverlandSpawner.OverlandRelocator.Relocate("PatchV254");
                patched = result.Repositioned;
                EchoOut(result.Summary, result.Unresolved > 0 ? ConsoleColor.Yellow : ConsoleColor.Magenta);
                #endregion Relocate misplaced overland NPCs

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV254

        #region PatchV255
        private static int? PatchV255(int patchid)
        {

            if (!LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Repair legacy rolled up sheet music
                // Rolled up sheet music must play itself (Instrument == the RSM); dupes copied Instrument from their source
                //  and load with null once it is deleted. RSMs saved before songs were hashed carry HashCode 0. LegacyRsmRepair
                //  (Patches201_300/LegacyRsmRepair.cs) sets Instrument = the RSM, and stores the song hash ManuscriptPaper computes
                //  (algorithm unchanged) only when no repository key, music box list, ownership GUID or hash collision is
                //  involved. Compositionless RSMs keep hash 0; nonzero hashes, repository keys and ownership are never
                //  changed; ambiguous RSMs are reported and left. Rerun-safe: a second run changes nothing. Every run is
                //  written to LegacyRsmRepair.log, the repairs with their previous values to LegacyRsmRepair.applied.
                //  If the plan cannot be completed nothing is changed and the patch is left incomplete (returns null).
                //  The protected set (sheets repaired, their library songs, masters and copies) is written to
                //  LegacyRsmRepair.protected before anything changes and checked after; a violation is flagged (red,
                //  returns null), never rolled back: the pre-migration world backup is the rollback.
                LegacyRsmRepair.Result result = LegacyRsmRepair.Run(apply: true, "PatchV255");
                if (result.Failed || result.ProtectionFailed)
                {
                    EchoOut(result.Summary + " See LegacyRsmRepair.log.", ConsoleColor.Red);
                    return null;
                }
                patched = result.Repaired;
                EchoOut(result.Summary, result.Ambiguous + result.Skipped > 0 ? ConsoleColor.Yellow : ConsoleColor.Magenta);
                #endregion Repair legacy rolled up sheet music

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV255

        #region PatchV256
        private static int? PatchV256(int patchid)
        {

            if (!LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Delete orphaned music virtual instruments
                // Every MusicContext creates a harp, lapharp and lute RazorInstrument in internal storage. Contexts dropped
                //  without MusicContext.Finish() (CMusicConfigCleanup before Phase 3 Stage 1, CalypsoMusicPlayer.GetMetaData)
                //  left those sets behind (Upheaval 2026-10-23: 11,002 sets). MusicOrphanCleanup deletes only complete,
                //  unreferenced sets: live session instruments are protected, and the world and the server's static fields are
                //  scanned for references before anything is deleted. If the plan cannot be completed nothing is deleted and
                //  the patch is left incomplete (returns null). The run is written to MusicOrphanCleanup.log, the deleted sets
                //  to MusicOrphanCleanup.deleted.
                MusicOrphanCleanup.Result result = MusicOrphanCleanup.Run(apply: true, "PatchV256");
                if (result.Failed)
                {
                    EchoOut(result.Summary + " See MusicOrphanCleanup.log.", ConsoleColor.Red);
                    return null;
                }
                patched = result.Deleted;
                EchoOut(result.Summary, result.ReferencedSets + result.Incomplete > 0 ? ConsoleColor.Yellow : ConsoleColor.Magenta);
                #endregion Delete orphaned music virtual instruments

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV256

        #region PatchV257
        private static int? PatchV257(int patchid)
        {

            if (!LoginServer(quiet: true) && TerritoryConfig.Enabled && TerritorySystem.Initialized)
            {
                int patched = 0;
                #region Begin Implementation

                #region Repair territory conquest flags
                // Before the 10/27 fix an Orc recapture reset a district's webs and spawners but left its zones conquered, and a
                //  conquered zone swallowed the re-cleared web's notification: the district could never be liberated again
                //  (Upheaval 2026-10-27: Vesper and Skara Brae fully re-cleared but not conquered; Britain and Jhelom with 60
                //  and 30 stale zones). TerritoryConquestRepair brings the flags into line with what has been won, silently,
                //  touching no spawner, kill, credit or ownership. The plan is recorded before anything changes (if it cannot
                //  be, nothing changes and the patch is left incomplete); after the apply a second evaluation must find
                //  nothing to change. The run is written to TerritoryConquestRepair.log.
                TerritoryConquestRepair.Result result = TerritoryConquestRepair.Run(apply: true, "PatchV257");
                if (result.Failed)
                {
                    EchoOut(result.Summary + " See TerritoryConquestRepair.log.", ConsoleColor.Red);
                    return null;
                }
                patched = result.Changes;
                EchoOut(result.Summary, result.DamagedWebs + result.Inconsistent > 0 ? ConsoleColor.Yellow : ConsoleColor.Magenta);
                #endregion Repair territory conquest flags

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV257

        #region PatchV258
        private static int? PatchV258(int patchid)
        {

            if (!LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region The Pumpkin King: 8 spawn levels
                // Halloween 2026: the champ at 1434 1389 0 (Felucca) plays 8 of its spawn levels, spread evenly; the boss
                //  level still runs last. MaxLevels stays GM-editable with [props.
                Point3D location = new Point3D(1434, 1389, 0);
                foreach (ChampEngine ce in ChampEngine.Instances)
                    if (!ce.Deleted && ce.Map == Map.Felucca && ce.Location == location)
                    {
                        ce.MaxLevels = 8;
                        patched++;
                    }

                if (patched == 0)
                    EchoOut(string.Format("PatchV258: no ChampEngine at {0} on Felucca; nothing changed.", location), ConsoleColor.Yellow);
                else
                    EchoOut(string.Format("PatchV258: {0} ChampEngine(s) at {1} set to MaxLevels 8.", patched, location), ConsoleColor.Magenta);
                #endregion The Pumpkin King: 8 spawn levels

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV258

        #region PatchV259
        private static int? PatchV259(int patchid)
        {

            if (!LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region The Pumpkin King: pumpkin patch
                // Halloween 2026: an 8x8 PumpkinPatchVine grid spawner beside the Pumpkin King champ, on Felucca and Test Center.
                //  A map that already has a grid spawner at the spot is left alone. Staff turn it off (Running) after the event.
                Point3D location = new Point3D(1436, 1395, 0);
                foreach (Map map in new Map[] { Map.Felucca, Map.TestCenter })
                {
                    if (map == null || map == Map.Internal)
                        continue;

                    bool exists = false;
                    IPooledEnumerable eable = map.GetItemsInRange(location, 0);
                    foreach (Item item in eable)
                        if (item is GridSpawner && !item.Deleted && item.X == location.X && item.Y == location.Y)
                            exists = true;
                    eable.Free();

                    if (exists)
                    {
                        EchoOut(string.Format("PatchV259: {0} already has a grid spawner at {1}; left alone.", map, location), ConsoleColor.Yellow);
                        continue;
                    }

                    GridSpawner spawner = new GridSpawner();
                    spawner.MoveToWorld(location, map);
                    spawner.ObjectNamesRaw = new ArrayList { "PumpkinPatchVine" };   // a vine with a real pumpkin on it
                    spawner.MustSteal = false;
                    spawner.GridWidth = 8;
                    spawner.GridHeight = 8;
                    spawner.Sparsity = 0.8;
                    spawner.Running = true;
                    spawner.ScheduleRespawn = true;                                 // fill the patch now (1-15 seconds)
                    patched++;
                    EchoOut(string.Format("PatchV259: pumpkin patch grid spawner placed at {0} on {1}.", location, map), ConsoleColor.Magenta);
                }
                #endregion The Pumpkin King: pumpkin patch

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV259

        #region PatchV260
        private static int? PatchV260(int patchid)
        {

            if (!LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region The Pumpkin King: retuned levels
                // Halloween 2026: ThePumpkinKing levels retuned in Data/ChampSpawns.xml (fewer kills, fewer alive at once). A
                //  ChampEngine keeps its own copy of the level table, so the champ at 1434 1389 0 (Felucca) re-applies the
                //  definition, and plays 5 spawn levels (0, 4, 8, 12, 15, then the boss).
                // A running champ restarts with the new levels (re-applying the definition restarts the spawn). Not done (null:
                //  runs again next start) if the shard still has the old XML.
                const int NewFirstLevelKills = 25;
                ChampDefinition? definition = ChampDefinitions.Find("ThePumpkinKing");
                ArrayList? levels = definition?.CreateLevels();
                if (definition == null || levels == null || levels.Count == 0 || ((ChampLevelData)levels[0]).m_MaxKills != NewFirstLevelKills)
                {
                    EchoOut("PatchV260: Data/ChampSpawns.xml does not have the retuned ThePumpkinKing levels; install it. Will retry next start.", ConsoleColor.Red);
                    return null;
                }

                Point3D location = new Point3D(1434, 1389, 0);
                foreach (ChampEngine ce in ChampEngine.Instances)
                    if (!ce.Deleted && ce.Map == Map.Felucca && ce.Location == location)
                    {
                        if (ce.Running)
                            EchoOut(string.Format("PatchV260: ChampEngine at {0} is running; restarting it with the new levels.", location), ConsoleColor.Yellow);

                        ce.MaxLevels = 5;                       // before the restart below, so the new run plays 5 levels
                        ce.DefinitionName = definition.Name;    // rebuild the level table from the XML (restarts a running spawn)
                        patched++;
                    }

                if (patched == 0)
                    EchoOut(string.Format("PatchV260: no ChampEngine at {0} on Felucca; nothing changed.", location), ConsoleColor.Yellow);
                else
                    EchoOut(string.Format("PatchV260: {0} ChampEngine(s) at {1}: retuned levels, MaxLevels 5.", patched, location), ConsoleColor.Magenta);
                #endregion The Pumpkin King: retuned levels

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV260
        #region PatchV261
        private static int? PatchV261(int patchid)
        {

            if (!LoginServer(quiet: true) && !Core.RuleSets.TestCenterRules() && !Core.RuleSets.EventShardRules())
            {
                int patched = 0;
                #region Begin Implementation

                #region Player history: the capture
                // The player history archive (Data/AIPlayerHistory, see OwnerTools.PlayerHistory.cs) is the record of our
                //  players. Once per live shard: upgrade the old (version 0) files, then capture every player here into this
                //  shard's file (merged: a player on file is replaced, everyone else kept), and reload the Doppelganger pool.
                //  Test Center and event shards are not captured (their players are not the shard's). Not done (null: runs
                //  again next start) if this shard's file cannot be read or written.
                List<string> report = new List<string>();
                bool done = PlayerHistory.Maintain(report);
                foreach (string line in report)
                    EchoOut("PatchV261: " + line, done ? ConsoleColor.Magenta : ConsoleColor.Red);

                if (!done)
                {
                    EchoOut("PatchV261: player history not captured. Will retry next start.", ConsoleColor.Red);
                    return null;
                }
                patched++;
                #endregion Player history: the capture

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV261
        #region PatchV262
        private static int? PatchV262(int patchid)
        {

            if (!LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Delete the tracks remasters replaced
                // Until 10/29/26, approving a remaster removed the track it replaced from the music library and left the
                //  item in internal storage. RemasterOrphanCleanup deletes those tracks (on Map.Internal, no parent, not
                //  temporary, not in the library or the review queue, the TrackID of a remastered library track or, for a
                //  track that lost its owner, its title and author; different music), each only if nothing in the world or
                //  the server's static fields references it. If the plan cannot
                //  be completed nothing is deleted and the patch is left incomplete (returns null). The run is written to
                //  RemasterOrphanCleanup.log, the deleted tracks to RemasterOrphanCleanup.deleted.
                RemasterOrphanCleanup.Result result = RemasterOrphanCleanup.Run(apply: true, "PatchV262");
                if (result.Failed)
                {
                    EchoOut(result.Summary + " See RemasterOrphanCleanup.log.", ConsoleColor.Red);
                    return null;
                }
                patched = result.Deleted;
                EchoOut(result.Summary, result.Referenced > 0 ? ConsoleColor.Yellow : ConsoleColor.Magenta);
                #endregion Delete the tracks remasters replaced

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV262
        #region PatchV263
        private static int? PatchV263(int patchid)
        {

            if (Upheaval() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Enable breeding
                // Upheaval breeds creatures (11/4/26): the BreedingEnabled dynamic feature, as the Core Management Console's
                //  BreedingEnabled property sets it. Dragons breed only in the Fire dungeon region and white wyrms in Ice
                //  (Dragon.CanBreed, WhiteWyrm.CanBreed). Already on (set by hand): nothing to do.
                if (!CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.BreedingEnabled))
                {
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.BreedingEnabled);
                    patched++;
                }
                EchoOut(string.Format("PatchV263: breeding is enabled{0}.", patched == 0 ? " (it already was)" : ""), ConsoleColor.Magenta);
                #endregion Enable breeding

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV263

        #region PatchV264
        private static int? PatchV264(int patchid)
        {
            if (!LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Repair carried-over Warlord counts
                // Before r1220 an Orc recapture kept the district's Warlord count (MiniBossesSpawned) and PatchV257 repaired
                //  only the conquest flags: the new campaign inherited the old count and spawned no earned Warlord
                //  (Upheaval 2026-11-04: Britain 45 spawned of 34 earned). A count above what has been earned can only be
                //  carried over; it is set to the earned count, and the rest of the campaign spawns its Warlords as they are
                //  earned. Nothing is spawned here. The plan is recorded to TerritoryConquestRepair.log first (if it cannot
                //  be, nothing changes and the patch is left incomplete).
                if (TerritoryConfig.Enabled && TerritorySystem.Initialized)
                {
                    List<string> lines = new List<string>();
                    int changed = TerritoryConquestRepair.RepairWarlordCounts(apply: true, "PatchV264", lines);
                    if (changed < 0)
                    {
                        EchoOut("PatchV264: the Warlord count repair could not be recorded; nothing changed. See the exception log.", ConsoleColor.Red);
                        return null;
                    }
                    foreach (string line in lines)
                        EchoOut("PatchV264: " + line, ConsoleColor.Magenta);
                    patched = changed;
                }
                #endregion Repair carried-over Warlord counts

                #region Move hemmed-in spawners
                // Spawners hemmed in by furniture and walls: a Warlord placed on one as the fallback (TryPlaceMiniBoss)
                //  could not move, or barely (WarlordSpawnTests, 2026-11-05). Moved to open floor, on Felucca and Trammel.
                //  Already moved (nothing at the old tile): nothing to do.
                //  Britain scribe (1493, 1713, 6): boxed in (bookshelf, potted plant, wall, writing desk), no step possible.
                //  Ocllo scribe (3614, 2467, 0): north and west only.
                //  Jhelom butcher (1448, 4024, 0): south only; the zone's only anchor, so every fallback there failed the rule.
                (Point3D From, Point3D To)[] moves =
                {
                    (new Point3D(1493, 1713, 6), new Point3D(1493, 1715, 6)),
                    (new Point3D(3614, 2467, 0), new Point3D(3611, 2466, 0)),
                    (new Point3D(1448, 4024, 0), new Point3D(1450, 4027, 0)),
                };
                foreach ((Point3D from, Point3D to) in moves)
                    foreach (Map map in new[] { Map.Felucca, Map.Trammel })
                        foreach (Spawner spawner in Spawner.Instances.Where(s => s != null && !s.Deleted && s.Map == map && s.Location == from).ToList())
                        {
                            spawner.MoveToWorld(to, map);
                            patched++;
                            EchoOut(string.Format("PatchV264: spawner 0x{0:X8} [{1}] on {2} moved from {3} to {4}", spawner.Serial.Value,
                                string.Join(",", spawner.ObjectNamesRaw.Cast<object>()), map, from, to), ConsoleColor.Magenta);
                        }
                #endregion Move hemmed-in spawners

                #endregion End Implementation
                return patched;
            }

            return null;
        }
        #endregion PatchV264
        #region Helpers
        #region PatchV212
        private static bool HasLeadingOrTrailingWhitespace(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;

            return char.IsWhiteSpace(s[0]) || char.IsWhiteSpace(s[^1]);
        }
        #endregion PatchV212
        #region PatchV207
        private static bool ChangeName(Serial serial, string newName)
        {
            Item item = World.FindItem(serial);

            if (item == null || item.Deleted)
                return false;

            item.Name = newName;
            return true;
        }
        #endregion PatchV207
        private static bool HasNearbyGypsySeerSpawner(Point3D location, Map map, int range)
        {
            IPooledEnumerable eable = map.GetItemsInRange(location, range);

            try
            {
                foreach (Item item in eable)
                {
                    if (item is Spawner spawner &&
                        !spawner.Deleted &&
                        spawner.Spawns("GypsySeer"))
                    {
                        return true;
                    }
                }
            }
            finally
            {
                eable.Free();
            }

            return false;
        }
        private static Point3D FindNearbySpawnLocation(Point3D center, Map map, int range)
        {
            for (int i = 0; i < 25; i++)
            {
                int x = center.X + Utility.RandomMinMax(-range, range);
                int y = center.Y + Utility.RandomMinMax(-range, range);
                int z = map.GetAverageZ(x, y);

                if (map.CanSpawnLandMobile(x, y, z))
                    return new Point3D(x, y, z);
            }

            // Safe fallback: place it directly on the existing TownCrier spawner.
            return center;
        }
        #endregion Helpers

    }
}