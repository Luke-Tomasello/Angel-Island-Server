/***************************************************************************
 *
 *                         Patches001_100
 *
 *  GMN World Patch Library - Patches 1 through 100
 *
 *  OVERVIEW
 *  --------
 *  This library contains the implementation of world patches assigned to
 *  patch numbers 1 through 100.
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

/* Patches001_100/PatchLibrary.cs
 * ChangeLog
 *  10/29/26, Adam (MusicLibrary)
 *      Type names only: the music library moved out of MusicBox (MusicBox.GlobalMusicRepository is MusicLibrary.Repository,
 *      MusicBox.MusicInfo is MusicLibrary.MusicInfo). No patch changes what it does.
 */

using Server;
using Server.Commands;
using Server.Diagnostics;
using Server.Engines;
using Server.Engines.Alignment;
using Server.Engines.BulkOrders;
using Server.Engines.ChampionSpawn;
using Server.Engines.Consoles;
using Server.Engines.CronScheduler;
using Server.Engines.ResourcePool;
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Multis.Deeds;
using Server.Regions;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using static Server.Commands.Decorate;
using static Server.Commands.GenTeleporter;
using static Server.CoreAI;
using static Server.Engines.SpawnerManager;
using static Server.Mobiles.Spawner;
using static Server.Patcher;
using static Server.Utility;

namespace Patches001_100
{
    public static class PatchLibrary
    {
        private const int PatchOffset = 0;

        private delegate int? PatchHandler(int patchid);

        private static readonly PatchHandler[] m_Patches =
        {
            null,        // index 0 - reserved/run-always convention
            PatchV1,     // retired: one-time patch history conversion (code commented out)
            PatchV2,     // clear the PatchV3 flag so the static region patch reruns
            PatchV3,     // clear the PatchV4 flag so the static region patch reruns
            PatchV4,     // reload static regions from Data and rerun PatchV19
            PatchV5,     // set Green Acres region controller graphics
            PatchV6,     // general cleanup: jail exit gate, Warden, prison spawners, moongate wizards
            PatchV7,     // Standard shards: validate teleporter patch lists
            PatchV8,     // turn off FreezeDry
            PatchV9,     // Standard shards: disable IOB regions
            null,        // PatchV10 - retired: wipe Kin Ransom chests
            PatchV11,    // assign GUIDs to players and their pets
            PatchV12,    // refresh house account codes
            PatchV13,    // categorize spawners (core vs. Angel Island special)
            PatchV14,    // Angel Island: activate camps
            PatchV15,    // Standard shards: disable Angel Island spawners
            PatchV16,    // set Felucca camp spawner respawn delays to 45-60 minutes
            PatchV17,    // move the New Player Starting Zone region controllers to Trammel
            PatchV18,    // Standard shards: remove the castle behind Oc'Nivelle
            PatchV19,    // rebuild static regions and patch region controllers
            PatchV20,    // Standard shards: initialize Felucca deco (moongates, teleporters, signs, doors)
            PatchV21,    // Standard shards: wipe the unusual container cache
            PatchV22,    // Standard shards: remove some Felucca teleporters and surrounding deco
            PatchV23,    // Standard shards: rebuild the unusual container cache
            PatchV24,    // Angel Island: remove some Felucca teleporters
            PatchV25,    // remove champion teleporters
            PatchV26,    // remove illegal Trammel items
            PatchV27,    // fix ConPvP teleporters
            PatchV28,    // fix vampire champ teleporter Z
            PatchV29,    // fix Green Acres teleporters
            PatchV30,    // Standard shards: restore Premier Gems to Britain
            PatchV31,    // Standard shards: remove the forge and deco behind West Britain Bank
            PatchV32,    // fix broken doors at 1133 2237 40
            PatchV33,    // Standard shards: respawn the world with classic UO spawn
            PatchV34,    // spawn birds, and crows at graveyards
            PatchV35,    // Angel Island: rebuild Moonglow Zoo spawners with protected creatures
            PatchV36,    // Standard shards: make Moonglow Zoo creatures protected
            PatchV37,    // convert Moonglow Zoo to exhibit creatures, lock doors, remove signs
            PatchV38,    // Standard shards: hide visible Felucca spawners (after classic respawn)
            PatchV39,    // enable DestinationOverride on all teleporters
            PatchV40,    // replace moongates with sungates
            PatchV41,    // disable Britain teleporters to the Lost Lands
            PatchV42,    // replace event teleporters with EventTeleporters
            PatchV43,    // Standard shards: initialize a new shard (wipe accounts/items, configure systems)
            PatchV44,    // map fixes: Buc's den, new player area, Britain sewer orc fort, gates, teleporters
            PatchV45,    // remove redundant Felucca public moongates
            PatchV46,    // convert blessed mobiles to Exhibit (invulnerable)
            PatchV47,    // remove blessed creatures not on spawners
            PatchV48,    // enable special spawners
            PatchV49,    // Angel Island: fix 'bower' spawn entries to 'bowyer'
            PatchV50,    // make template vendors invulnerable
            PatchV51,    // restore vendor daily fees
            PatchV52,    // categorize 'other' spawners
            PatchV53,    // remove the T2A teleporter from the chicken fight tunnel
            PatchV54,    // Standard shards: repatch Moonglow Zoo
            PatchV55,    // Standard shards: remove Sea Market spawners
            PatchV56,    // Angel Island: faster camp respawn rates
            PatchV57,    // adjust dungeon exit teleporters for the new map floor
            PatchV58,    // Standard shards: adjust dungeon exit teleporters (pass 2)
            PatchV59,    // fix doors at the Fire Temple and 1680 2986 20, remove tiles
            PatchV60,    // reset champ spawns, enable the Autumn champ
            PatchV61,    // add each account's hardware hash to its hash list
            PatchV62,    // Siege-style: enable Patrol Guards, add Vesper bank spawners
            PatchV63,    // patcher table test (no-op)
            PatchV64,    // fix static region controllers
            PatchV65,    // beta prep: disable Siege seasonal champs, fix the Green Acres region
            PatchV66,    // rework Yew Orc Fort, Wind, farms, T2A teleporters, pet teleporting, Britain tinker
            PatchV67,    // verify patcher fix, fix Wind teleporter and lich spawner, respawn
            PatchV68,    // Hythloth and dungeon map fixes, T2A teleporters, Evil Wandering Healer, enable Kin
            PatchV69,    // T2A and Fire dungeon blockers, Fire/Ice moongate wizards, spawner cleanup
            PatchV70,    // Siege-style rares, remove control slots, spawn trainer and dungeon chests
            PatchV71,    // Solen caves, WBB library books, Siege orc goodies, mobile activation
            PatchV72,    // teleporter maintenance (Covetous, Ice dungeon)
            PatchV73,    // wipe factions, migrate Kin, spawner and addon fixes, Covetous teleporter
            PatchV74,    // empty placeholder (applied empty by accident)
            PatchV75,    // Siege-style: Wind solen spawner, orc goodies, evil orcs, mount speed
            PatchV76,    // Siege-style: new player area and guild, convert unusual chests to dungeon chests
            PatchV77,    // Siege-style: champ, moongate wizard, Cove banker, Juka and Nightmare changes, respawn
            PatchV78,    // Outcasts guild stone, death trap, explosion potions, orc fort trees, respawn
            PatchV79,    // Siege-style: skill gain bonus, respawn
            PatchV80,    // fix Covetous tiles, revert Hythloth directional teleporters
            PatchV81,    // feather deck tile, Angel Island stinger souvenirs, ankhs, Britain farms
            PatchV82,    // reset MachineInfo, area peace, map and teleporter fixes, respawn dungeon chests
            PatchV83,    // Jhelom hire fighters, solens, keyword teleporters, sea horses, bank trainer chests
            PatchV84,    // Fire Temple fixes, spawner graphics, ClearPath guard spawners, Yew Orc Fort blocker
            PatchV85,    // Deceit and Buc's spawners, Wind pet teleporters, township stones, Ilshenar sungates
            PatchV86,    // spawn, T2A teleporter, wand drop, Pub15 spawner, region and orphaned key fixes
            PatchV87,    // moongate wizards, T2A exits, Dungeon Wrong rework, orc fort, region flags
            PatchV88,    // Siege-style: patch oracles, port key
            PatchV89,    // respawn camp spawners, Siege: Destard healers
            PatchV90,    // Wrong bone piles, ostards, ridable llamas, savage riders, spawner updates
            PatchV91,    // Siege-style: Dungeon Despise spawn, stone crafter
            PatchV92,    // port music library to Siege, permanent spawners/teleporters, savage paint, map fixes
            PatchV93,    // dungeon chest levels, pet-friendly spawners, remove keys on the ocean
            PatchV94,    // add the Prison quest (liaison officer, rare chest)
            PatchV95,    // wipe machine hashes for the new hash algorithm
            PatchV96,    // Siege-style: enable Sea Gypsies
            PatchV97,    // goodwill crates, fruit baskets, total respawn for new decay
            PatchV98,    // Siege-style rares, crystalline powder, scarecrows, cloth
            PatchV99,    // add rare fillable containers, respawn rares
            PatchV100,   // moongate wizard spawners, Nerun's override values, gold box spawn delay
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
        #region PatchV1
        private static int? PatchV1(int patchid)
        {
            // this ancient batch converted from our old patch history file to the new patch history file. It is no longer needed, but is left here for reference.
            //int patched = 0;
            //PatchIndex bits = PatchIndex.PatchV1;
            //// cannot call AllShards here as it will initialize the file we are checking!
            //if (true)
            //{
            //    // cannot call LogPatch here as it will initialize the file we are checking!
            //    if (!File.Exists(m_pathName))
            //        foreach (PatchIndex patchBits in Enum.GetValues(typeof(PatchIndex)))
            //            if (Patched(patchBits))
            //            {   // record that this patch has been applied.
            //                LogPatch(patchBits);
            //                PatchComplete(patchBits, patchid, timeUnknown: true);
            //                patched++;
            //            }

            //    LogPatch(bits);
            //    EchoOut("One-time patch to retroactively update patch history...", ConsoleColor.Magenta);
            //    EchoOut(string.Format("{0} Patch history updated.", patched), ConsoleColor.Magenta);
            //    return patched;
            //}
            return null;
        }
        #endregion PatchV1
        #region PatchV2
        private static int? PatchV2(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV2;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("Redo of static regions patch V3...", ConsoleColor.Magenta);
                ClearDynamicPatch(PatchIndex.PatchV3);   // clear the previous patch flag
                EchoOut(string.Format("Ready to apply V2..."), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV2
        #region PatchV3
        private static int? PatchV3(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV3;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("Redo of static regions patch V2...", ConsoleColor.Magenta);
                ClearDynamicPatch(PatchIndex.PatchV4);   // clear the previous patch flag
                EchoOut(string.Format("Ready to apply Static Region Patch....", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV3
        #region PatchV4
        private static int? PatchV4(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV4;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("Redo of static regions patch V1...", ConsoleColor.Magenta);
                object regionRegions = SaveRegionRegions();             // save the current regions, and clear region database
                object mapRegions = SaveMapRegions();                   // save the current regions, and clear map region database
                DeleteStaticRegionsFromSaves();                         // force a reload from Data/
                StaticRegion.XmlDatabase.Clear();                       // empty database
                StaticRegion.LoadRegions(StaticRegion.DataFileName, initRegistration: true);    // okay, load from Data/
                ClearDynamicPatch(PatchIndex.PatchV19);   // clear the previous patch flag

                int? result = PatchV19(patchid);          // redo the patch
                if (!result.HasValue)
                    return null;

                patched = result.Value;

                RestoreRegionRegions(regionRegions);
                RestoreMapRegions(mapRegions);

                EchoOut(string.Format("{0} static regions repatched.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        private static bool DeleteStaticRegionsFromSaves()
        {
            string filename = Path.Combine("Saves/", "StaticRegions.xml");
            bool complete = false;
            if (System.IO.File.Exists(filename))
            {
                try { System.IO.File.Delete(filename); }
                catch { }
                if (!System.IO.File.Exists(filename))
                    complete = true;
            }
            else
                complete = true;

            return complete;
        }
        private static object SaveRegionRegions()
        {
            Dictionary<int, Region> regions = new Dictionary<int, Region>();
            foreach (Region rx in Region.Regions)
                regions.Add(rx.UId, rx);
            foreach (Region rx in regions.Values)
                Region.RemoveRegion(rx);
            return regions;
        }
        private static void RestoreRegionRegions(object o)
        {
            Dictionary<int, Region> regions = o as Dictionary<int, Region>;
            foreach (Region rx in regions.Values)

                /* Can't filter here by Region.FindByUId(rx.UId) as these are guaranteed 
                 *  to be different that the regions loaded from Data/StaticRegions.xml
                 * We allow null or empty names as this are house regions. As long as they are not in 
                 *  Data/StaticRegions.xml, then we should be cool
                 * Finally, we allow all uniquely named regions. This might a problem for Snafu's 
                 *  "Last Man Standing" regions if they made there way into Data/StaticRegions.xml
                 */
                if (string.IsNullOrEmpty(rx.Name) || (Region.FindByName(rx.Name, rx.Map) == null))
                    // add it!
                    Region.AddRegion(rx);
            return;
        }
        private static object SaveMapRegions()
        {
            Dictionary<string, Region> regions = new Dictionary<string, Region>();
            foreach (Region rx in Map.Felucca.Regions.Values)
                regions.Add(rx.Name, rx);
            foreach (Region rx in regions.Values)
                Map.Felucca.UnregisterRegion(rx);
            return regions;
        }
        private static void RestoreMapRegions(object o)
        {
            Dictionary<string, Region> regions = o as Dictionary<string, Region>;
            foreach (Region rx in regions.Values)
                if (!Map.Felucca.Regions.ContainsKey(rx.Name))
                    //Map.Felucca.Regions.Add(rx.Name, rx);
                    rx.Map.RegisterRegion(rx);
            return;
        }
        #endregion PatchV4
        #region PatchV5
        private static int? PatchV5(int patchid)
        {

            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV5;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch of region controllers graphic ID...", ConsoleColor.Magenta);

                Region ga = Region.FindByName("Green Acres", Map.Felucca);
                if (ga != null)
                {
                    for (int i = CustomRegionControl.Instances.Count - 1; i >= 0; i--)
                    {
                        CustomRegionControl crc = CustomRegionControl.Instances[i];
                        if (crc.Parent == null && ga.Contains(crc.Location))
                        {
                            crc.ItemID = 0x0BF6; // Sailor's guild sign
                            patched++;
                        }
                    }

                    for (int i = StaticRegionControl.Instances.Count - 1; i >= 0; i--)
                    {
                        StaticRegionControl src = StaticRegionControl.Instances[i];
                        if (src.Parent == null && ga.Contains(src.Location))
                        {
                            src.ItemID = 0x0BAC; // healer sign (looks different that region controllers)
                            patched++;
                        }
                    }
                }

                EchoOut(string.Format("{0} region controllers patched.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV5
        #region PatchV6
        private static int? PatchV6(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV6;
            if (AllShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time patch to perform some general cleanup...", ConsoleColor.Magenta);

                Region prison = Region.FindByName("Angel Island", Map.Felucca);
                Region greenacres = Region.FindByName("Green Acres", Map.Felucca);
                bool ai = Core.RuleSets.AngelIslandRules();
                bool siege = Core.RuleSets.SiegeRules();

                foreach (Item item in Server.World.Items.Values)
                {
                    if (item is null || item.Deleted) continue;
                    if (item.Map == null) continue;

                    // turn jail exit gate (all shards)
                    if (IsFelucca(item) && item is JailExitSungate gate)
                    {
                        gate.Running = true;
                        gate.Visible = true;
                        patched++;
                    }
                    // add Warden (all shards)
                    if (IsFelucca(item) && item is Spawner s0 && prison != null && prison.Contains(s0.Location) && s0.Location == new Point3D(355, 833, 20))
                    {
                        s0.ObjectNamesRaw = new() { "AIWarden" };

                        UpdateSpawnerDesignationaAndLog(s0, coreSpawn: false,
                            uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                        s0.HomeRange = 0;
                        s0.MobileDirection = Direction.South;
                        s0.Running = true;
                        patched++;
                    }
                    // turn prison spawners (all shards but AI)
                    if (!ai && IsFelucca(item) && item is Spawner s1 && prison != null && prison.Contains(s1.Location))
                    {
                        // reclassify here as 'all shards'
                        UpdateSpawnerDesignationaAndLog(s1, coreSpawn: false,
                            uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                        s1.Running = true;
                        patched++;
                    }
                    // turn on MoonGateWizards (all shards but AI)
                    if (!ai && IsFelucca(item) && item is Spawner s2)
                    {
                        if (s2.Spawns("MoonGateWizard"))
                        {
                            // reclassify here as 'all shards'
                            UpdateSpawnerDesignationaAndLog(s2, coreSpawn: false,
                                uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                            s2.Running = true;
                            patched++;
                        }
                    }
                    if (item is CustomRegionControl crc)
                    {   // reset the colorization state (all shards)
                        crc.Registered = !crc.Registered;
                        crc.Registered = !crc.Registered;
                        patched++;

                        if (crc.CustomRegion != null && !string.IsNullOrEmpty(crc.CustomRegion.Name))
                        {
                            if (!ai) // all shards but AI
                                if (crc.CustomRegion.Name == "Ocllo Island" || crc.CustomRegion.Name == "Island Siege")
                                {
                                    crc.Registered = false;
                                    patched++;
                                }

                            if (ai) // AI only
                                if (crc.CustomRegion.Name == "Ocllo Island" || crc.CustomRegion.Name == "Ocllo")
                                {
                                    crc.Registered = false;
                                    patched++;
                                }
                        }
                    }
                    if (item is StaticRegionControl src)
                    {   // reset the colorization state (all shards)
                        src.Registered = !src.Registered;
                        src.Registered = !src.Registered;
                        patched++;

                        if (!ai) // all shards but AI
                            if (src.StaticRegion.Name == "Ocllo Island" || src.StaticRegion.Name == "Island Siege")
                            {
                                src.Registered = false;
                                patched++;
                            }

                        if (ai) // AI only
                            if (src.StaticRegion.Name == "Ocllo Island" || src.StaticRegion.Name == "Ocllo")
                            {
                                src.Registered = false;
                                patched++;
                            }
                    }
                }

                EchoOut(string.Format("{0} elements patched", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        private static bool IsFelucca(Item item)
        {
            return item != null && !item.Deleted && item.Map == Map.Felucca;
        }
        #endregion PatchV6
        #region PatchV7
        private static int? PatchV7(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV7;
            if (StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to validated teleporter patchLists...", ConsoleColor.Magenta);

                List<Tuple<int, int, int, int, Map>> records = new List<Tuple<int, int, int, int, Map>>();
                string folder = Path.Combine(Core.DataDirectory, "Patches");

                if (!Directory.Exists(folder))
                {
                    ErrorOut(bits, string.Format("Error: unable to locate {0}.", folder), ConsoleColor.Red);
                    return 0;
                }

                string[] files = Directory.GetFiles(folder, "*.cfg");
                for (int j = 0; j < files.Length; ++j)
                {
                    int oldPatched = patched;
                    foreach (string line in File.ReadAllLines(files[j]))
                    {
                        Serial serial;
                        string[] toks = line.Split(' ');
                        if (toks.Length != 5) continue; // different .cfg format
                                                        // normalize hex => decimal
                        if (toks[0].StartsWith("0X", StringComparison.OrdinalIgnoreCase))
                        {
                            serial = int.Parse(toks[0].Substring(2), NumberStyles.HexNumber);
                            patched++;              // had to convert
                        }
                        else
                            serial = int.Parse(toks[0]);
                        //------------------------------------- Serial Number ------------ X ---------------- Y ----------------- Z ----------------- Map --
                        Tuple<int, int, int, int, Map> record = new(serial, int.Parse(toks[1]), int.Parse(toks[2]), int.Parse(toks[3]), Map.Parse(toks[4]));
                        serial = record.Item1;
                        Item item = Server.World.FindItem(serial);
                        if (item != null && item.Deleted == false && item.Map != null && item.Map != Map.Internal)
                            records.Add(record);    // used for writing
                        else
                            patched++;              // can't find teleporter
                    }

                    // rewrite the list if changed.
                    if (oldPatched != patched)
                        using (StreamWriter file = new(files[j]))
                        {
                            foreach (var toWrite in records)
                            {
                                string s = string.Format("{0} {1} {2} {3} {4}", toWrite.Item1, toWrite.Item2, toWrite.Item3, toWrite.Item4, toWrite.Item5);
                                file.WriteLine(s);
                            }
                        }
                    records.Clear();
                }


                EchoOut(string.Format("{0} teleporter patchLists checked, {1} modifications.", files.Length, patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV7
        #region PatchV8
        private static int? PatchV8(int patchid)
        {
            int patches = 0;
            PatchIndex bits = PatchIndex.PatchV8;
            //adam: Because Gold Tracker and Leaderboard frequently rehydrate the world, it obviates the need for freeze drying somewhat.
            //  I mean, what good is having 10,000 freeze dried items, if every 5 minutes we rehydrate them?
            //  If our population ever skyrockets, we can revisit this.
            //  Mortalis: Another concern is with new player tracking of items. This becomes much more complex with freeze drying as it effectly hides them from the world.
            if (Server.World.FreezeDryEnabled == true && AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch of FreezeDry system. Turning off...", ConsoleColor.Magenta);
                CommandEventArgs e = new CommandEventArgs();
                Server.World.FreezeDryEnabled = false; // no more FreezeDry :(
                EchoOut("One-time patch of FreezeDry system. complete.", ConsoleColor.Magenta);
                return patches + 1;
            }
            return null;
        }
        #endregion PatchV8
        #region PatchV9
        private static int? PatchV9(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV9;
            if (StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to disabled IOB regions...", ConsoleColor.Magenta);
                foreach (Item item in Server.World.Items.Values)
                    if (item is CustomRegionControl crc)
                        if (crc.CustomRegion.IOBAlignment != IOBAlignment.None)
                        {
                            crc.Registered = false;
                            patched++;
                        }
                if (patched > 0)
                {

                    EchoOut(string.Format("{0} IOB regions disabled.", patched), ConsoleColor.Magenta);
                }
                else
                    ErrorOut(bits, "Did not find IOB regions as expected.", ConsoleColor.Red);
                return patched;
            }
            return null;
        }
        #endregion PatchV9
        #region PatchV11
        private static int? PatchV11(int patchid)
        {
            int patches = 0;
            PatchIndex bits = PatchIndex.PatchV11;
            // patch the players GUIDs
            //  we need to do players first, then pets
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch of player GUIDs. Setting...", ConsoleColor.Magenta);
                int seen = 0;
                int patched = 0;
                foreach (Mobile m in Server.World.Mobiles.Values)
                {
                    if (m == null || m.Deleted) continue;
                    if (m is PlayerMobile pm)
                    {
                        seen++;
                        // players that don't yet have a GUID, get one now.
                        //  a GUID uniquely identifies a player, even after the mobile has been deleted
                        // (Needed by by Mortalis)
                        if (pm.AcctGUID == 0)
                        {
                            patched++;
                            // no longer used in this way.
                            //  GUIDs are created and assigned at login
                            //pm.GUID = pm.MakeGuid();
                        }
                    }
                }
                EchoOut(string.Format("{0} players checked, {1} patched.", seen, patched), ConsoleColor.Magenta);
                EchoOut("One-time patch of player GUIDs complete.", ConsoleColor.Magenta);
                patches = patched;

                // patch the player's pets GUIDs
                //  we need to do players first, then pets
                if (AllShards())
                {
                    EchoOut("One-time patch of player's pets GUIDs. Setting...", ConsoleColor.Magenta);
                    seen = 0;
                    patched = 0;
                    foreach (Mobile m in Server.World.Mobiles.Values)
                    {
                        if (m == null || m.Deleted) continue;
                        if (m is BaseCreature bc && bc.ControlMaster != null)
                        {
                            seen++;
                            // player's pet's that don't yet have a GUID, get one now.
                            //  a GUID uniquely identifies a player, even after the mobile has been deleted
                            // (Needed by by Mortalis)
                            if (bc.ControlMasterGUID == 0)
                            {
                                patched++;
                                bc.ControlMasterGUID = bc.ControlMaster.AcctGUID;
                            }
                        }
                    }
                    EchoOut(string.Format("{0} pets checked, {1} patched.", seen, patched), ConsoleColor.Magenta);
                    EchoOut("One-time patch of player's pet's GUIDs complete.", ConsoleColor.Magenta);
                    patches += patched;
                }

                return patches;
            }
            return null;
        }
        #endregion PatchV11
        #region PatchV12
        private static int? PatchV12(int patchid)
        {
            PatchIndex bits = PatchIndex.PatchV12;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch of housing account codes. Patching...", ConsoleColor.Magenta);
                int patched = 0;
                foreach (Item item in Server.World.Items.Values)
                {
                    if (item is Server.Multis.BaseHouse bh)
                    {
                        patched++;
                        bh.RefreshAccountCode();
                    }
                }
                EchoOut(string.Format("{0} houses patched.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV12
        #region PatchV13
        private static int? PatchV13(int patchid)
        {
            XmlDocument doc = Utility.OpenStandardObjects();
            PatchIndex bits = PatchIndex.PatchV13;
            if (AllShards())
            {
                EchoOut("One-time patch for spawner categorization. Patching...", ConsoleColor.Magenta);
                int patched = 0;
                List<Serial> orcCaves = new List<Serial>() { 0x40052374, 0x40052EF4, 0x40053A47, 0x40055561, 0x400558F9, 0x40055C42, 0x400562D6, 0x40056626, 0x40056B6C, 0x40056F13, 0x40057224, 0x400574D1, 0x40058011, 0x400583AA };
                List<Serial> area37 = new List<Serial>() { 0x40033C4C };
                List<Serial> area40 = new List<Serial>() { 0x40132932 };

                // Sanity. Disallow running patch on already patched spawners
                foreach (Item item in Server.World.Items.Values)
                {
                    if (item is EventSpawner) continue;
                    if (item.Map != Map.Felucca) continue;

                    if (item is Spawner spawner)
                    {
                        if (spawner.Distro != SpawnerModeAttribs.None)
                        {
                            ErrorOut(bits, "Error: These spawners have already been patched. Aborting...", ConsoleColor.Red);
                            return 0;
                        }
                    }
                }

                LogHelper logger = new LogHelper(Path.Combine(Core.DataDirectory, Core.Server, "definitive spawner list.cfg"), true, true);
                foreach (Item item in Server.World.Items.Values)
                {
                    if (item is EventSpawner) continue;
                    if (item.Map != Map.Felucca) continue;

                    if (item is Spawner spawner)
                    {
                        bool allStandardCreature = AllStandardCreatures(doc, spawner);
                        bool excludeFromCore = ExcludeFromCore(spawner);

                        patched++;
                        if (orcCaves.Contains(spawner.Serial))
                        {
                            spawner.CoreSpawn = true;
                            spawner.Source = "OrcCaves";
                            spawner.Shard = Spawner.ShardConfig.Core;
                            spawner.NeedsReview = false;
                            spawner.Distro = SpawnerModeAttribs.ModeLegacy;
                        }
                        else if (area37.Contains(spawner.Serial))
                        {
                            spawner.CoreSpawn = true;
                            spawner.Source = "Area 37";
                            spawner.Shard = Spawner.ShardConfig.Core;
                            spawner.NeedsReview = false;
                            spawner.Distro = SpawnerModeAttribs.ModeLegacy;
                        }
                        else if (area40.Contains(spawner.Serial))
                        {
                            spawner.CoreSpawn = true;
                            spawner.Source = "Area 40";
                            spawner.Shard = Spawner.ShardConfig.Core;
                            spawner.NeedsReview = false;
                            spawner.Distro = SpawnerModeAttribs.ModeLegacy;
                        }
                        else if (spawner.CoreSpawn)                                                         // was manually designated by AI staff
                        {   // determine if this spawner needs review
                            if (spawner.Name.ToLower() == "spawner")
                            {   // it was marked as core, but had no 'Area' qualifier 
                                spawner.NeedsReview = true;
                            }
                            else
                                spawner.NeedsReview = false;
                            spawner.CoreSpawn = true;
                            spawner.Source = PatchSpawnerData(spawner);
                            spawner.Shard = Spawner.ShardConfig.Core;
                            spawner.Distro = SpawnerModeAttribs.ModeLegacy;
                        }
                        else if (SpecialDisposition(spawner) != null)                                   // AI prison
                        {   // needs to come before AllStandardCreatures.
                            spawner.Source = SpecialDisposition(spawner);                          // default to another naming, like IOB Alignment
                                                                                                   // An Angel Island special spawn. I.e., non-core that will be allowed on all shards.
                                                                                                   //  example: Angel Island Prison, treasure chests, hire fighters, etc
                            spawner.CoreSpawn = false;
                            spawner.Shard = Spawner.ShardConfig.AllShards;
                            spawner.NeedsReview = false;
                            spawner.Distro = SpawnerModeAttribs.ModeLegacy;
                        }
                        else if (PromoteToCore(spawner) == true && excludeFromCore == false)            // shopkeepers and such
                        {   // certain mobiles like BaseVendor and Guards should be flagged as being 'core' spawners as they are not 'special' (or usually not)
                            spawner.Source = "Pub15";                                              // denotes this as pub 15 standard spawn. 
                            spawner.CoreSpawn = true;
                            spawner.Shard = Spawner.ShardConfig.Core;
                            spawner.NeedsReview = false;
                            spawner.Distro = SpawnerModeAttribs.ModeLegacy;
                        }
                        else if (allStandardCreature && excludeFromCore == false)                       // standard creatures get promoted to core
                        {   // here we check to see if all creatures on this spawner are standard OSI circa Publish 15
                            //  in the early days, we placed spawners without a 'core' marker. We will promote them now.
                            spawner.Source = "Pub15";                                              // denotes this as pub 15 standard spawn. 
                            spawner.CoreSpawn = true;
                            spawner.Shard = Spawner.ShardConfig.Core;
                            spawner.NeedsReview = false;
                            spawner.Distro = SpawnerModeAttribs.ModeLegacy;
                        }
                        else
                        {
                            // An Angel Island special spawn. I.e., non-core
                            spawner.Source = "AISpecial";  // denotes this as an Angel Island Special - non standard. 
                            spawner.CoreSpawn = false;
                            spawner.Shard = Spawner.ShardConfig.AngelIsland;
                            spawner.NeedsReview = false;
                            spawner.Distro = SpawnerModeAttribs.ModeLegacy;
                        }

                        logger.Log(LogType.Text, string.Format("Serial: {0}, Location: {1}, Name: {2}, UOSpawnMap: {3}, NeedsReview: {4}", spawner.Serial, spawner.Location, spawner.Name, spawner.Source, spawner.NeedsReview));
                    }
                }
                logger.Finish();
                EchoOut(string.Format("{0} spawners categorized.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        private static bool AllStandardCreatures(XmlDocument doc, Spawner spawner)
        {
            if (spawner.Objects.Count == 0)
                return false;

            foreach (object o in spawner.Objects)
                if (o is BaseCreature)
                    if (Utility.StandardCreature(doc, o.GetType().FullName) == false)
                        return false;

            // we have objects, they are all baseCreature, and they are all standard.
            return true;
        }
        private static string SpecialDisposition(Spawner spawner)
        {
            // By Region
            ArrayList list = Region.FindAll(spawner.Location, spawner.Map);
            foreach (Region rx in list)
                if (!string.IsNullOrEmpty(rx.Name))
                    if (rx.Name.ToLower().Contains("angel island"))
                        return "AIPrison";

            // I've opted to disallow a special disposition for Kin, meaning, not on Siege
            if (false)
            {
                // By IOB Alignment
                //  We're keeping the kin and their strongholds.
                foreach (object o in spawner.Objects)
                {
                    if (o is BaseCreature bc)
                    {
                        switch (bc.IOBAlignment)
                        {
                            case IOBAlignment.Council:
                            case IOBAlignment.Pirate:
                            case IOBAlignment.Brigand:
                            case IOBAlignment.Orcish:
                            case IOBAlignment.Savage:
                            case IOBAlignment.Undead:
                            case IOBAlignment.Good:
                                return "Kin";
                        }
                    }
                }
            }

            // special spawners that we want to keep on all shards
            if (spawner.Spawns("treasure")) return "AISpecial";
            if (spawner.Spawns("hirefighter")) return "AISpecial";
            if (spawner.Spawns("deadminer")) return "AISpecial";
            if (spawner.Spawns("deadinmate")) return "AISpecial";
            if (spawner.Spawns("moongatewizard")) return "AISpecial";

            return null;
        }
        private static bool ExcludeFromCore(Spawner spawner)
        {
            foreach (object o in spawner.ObjectTypes)
            {   // all/most of these guys should NOT be marked as core

                if (typeof(FightBroker).IsAssignableFrom((o as Type))) return true;         // AI Special
                if (typeof(Auctioneer).IsAssignableFrom((o as Type))) return true;          // AI Special
                if (typeof(Ranger).IsAssignableFrom((o as Type))) return true;              // AI Special: RunUO's StandardCreatures contains a 'Ranger' too. Don't accept it as a match (will accept elsewhere)
                if (typeof(BritannianRanger).IsAssignableFrom((o as Type))) return true;    // AI Special
                if (typeof(Server.Mobiles.Pirate).IsAssignableFrom((o as Type))) return true;              // AI Special: RunUO's StandardCreatures contains a 'Pirate' too. Don't accept it as a match (will accept elsewhere)
            }

            return false;
        }
        private static bool PromoteToCore(Spawner spawner)
        {
            foreach (object o in spawner.ObjectTypes)
            {   // all/most of these guys should be marked as core

                if (typeof(BaseGuard).IsAssignableFrom((o as Type))) return true;
                if (typeof(BaseVendor).IsAssignableFrom((o as Type))) return true;      // includes BaseHealer
                if (typeof(BaseOverland).IsAssignableFrom((o as Type))) return true;    // all your Escortables (are belong to us)
                if (typeof(Gypsy).IsAssignableFrom((o as Type))) return true;
                if (typeof(Thief).IsAssignableFrom((o as Type))) return true;           // I guess. Don't know I've ever seen one of these

                // promote spawners with town critters to 'Core'
                if (typeof(BaseCreature).IsAssignableFrom((o as Type)))
                {
                    int nonCritters = 0;
                    foreach (object c in spawner.Objects)
                    {   // see if there is anything but critters on this spawner. If so, we won't promote
                        if (!(c is BaseCreature bc && bc.AIObject is AnimalAI))
                            // not a town critter
                            nonCritters++;
                    }
                    if (nonCritters == 0 && spawner.Objects.Count > 0)
                    {
                        // critters in town are also 'core'
                        if (Utility.IsTownRegion(spawner.Location, spawner.Map))
                            return true;
                    }
                }
            }

            return false;
        }
        private static string PatchSpawnerData(Spawner spawner)
        {
            if (spawner.Name == "Spawner")
                return "Area ??";
            else if (spawner.Name.ToLower().Contains("area"))
            {
                spawner.Source = spawner.Name.Replace("Spawner", "").Replace("  ", " ").Trim();
                spawner.Name = "Spawner";
                return spawner.Source;
            }
            else
                return "??";
        }
        #endregion PatchV13
        #region PatchV14
        private static int? PatchV14(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV14;
            if (AngelIsland())
            {
                LogPatch(bits);
                EchoOut("One-time patch of Camp system. Activating...", ConsoleColor.Magenta);
                foreach (Item item in Server.World.Items.Values)
                {
                    if (item is EventSpawner) continue;
                    if (item.Map != Map.Felucca) continue;

                    if (item is Spawner spawner)
                        if (spawner.Spawns("camp"))
                        {
                            if (spawner.Running == false)
                            {
                                patched++;
                                spawner.Running = true;
                                spawner.MinDelay = TimeSpan.FromHours(1);
                                spawner.MaxDelay = TimeSpan.FromHours(3);
                            }
                            else
                            {
                                patched++;
                                spawner.MinDelay = TimeSpan.FromHours(1);
                                spawner.MaxDelay = TimeSpan.FromHours(3);
                            }
                        }
                }

                EchoOut(string.Format("{0} Camps Activated.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV14
        #region PatchV15
        private static int? PatchV15(int patchid)
        {   // Turn off all spawners tagged as Angel Island 'special'
            PatchIndex bits = PatchIndex.PatchV15;
            if (StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to disable Angel Island spawners on standard shards. Patching...", ConsoleColor.Magenta);
                int patched = 0;
                foreach (Item item in Server.World.Items.Values)
                {
                    if (item.Map != Map.Felucca) continue;
                    if (item is EventSpawner) continue;
                    if (item is Spawner spawner)
                    {
                        // based on our spawner/shard config, should this spawner be killed?
                        if (Spawner.ShouldShardEnable(spawner) == false)
                        {
                            patched++;
                            spawner.Running = false;
                            spawner.RemoveObjects();
                        }
                    }
                }
                EchoOut(string.Format("{0} spawners disabled.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV15
        #region PatchV16
        private static int? PatchV16(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV16;
            // we really only should run this on AngelIsland(), but I inadvertently ran it on 
            //  Siege, so now we need to undo the patch there.
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch of Camp respawn. Patching...", ConsoleColor.Magenta);
                foreach (Item item in Server.World.Items.Values)
                {
                    if (item is EventSpawner) continue;
                    if (item.Map != Map.Felucca) continue;

                    if (item is Spawner spawner)
                        if (spawner.Spawns("camp"))
                        {
                            patched++;
                            spawner.MinDelay = TimeSpan.FromMinutes(45);    // from Nerun's Outdoors.map
                            spawner.MaxDelay = TimeSpan.FromMinutes(60);
                        }
                }

                EchoOut(string.Format("{0} Camps Patched.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV16
        #region PatchV17
        private static int? PatchV17(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV17;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch of New Player Starting Zone...", ConsoleColor.Magenta);

                for (int i = CustomRegionControl.Instances.Count - 1; i >= 0; i--)
                {
                    CustomRegionControl crc = CustomRegionControl.Instances[i];

                    if (crc.Parent == null)
                    {
                        CustomRegion cr = crc.CustomRegion;

                        for (int j = 0; j < StaticRegion.XmlDatabase.Count; j++)
                        {
                            StaticRegion sr = StaticRegion.XmlDatabase[j];

                            if (cr.Name == sr.Name && cr.Map != sr.Map)
                            {
                                // only static-ify custom region controllers if the static region in question is registered
                                //if (sr.Registered)
                                {
                                    patched++;
                                    cr.Map = Map.Trammel;
                                    cr.Coords.Clear();
                                    cr.Coords.AddRange(new List<Rectangle3D>() { new Rectangle3D(new Point3D(5700, 940, -128), new Point3D(5765, 1000, 128)) });
                                }

                                goto Done;
                            }
                        }
                    }
                }
            Done:


                EchoOut(string.Format("{0} New Player Starting Zone patched.", patched), ConsoleColor.Magenta);
                return patched;

            }
            return null;
        }
        #endregion PatchV17
        #region PatchV18
        private static int? PatchV18(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV18;
            if (StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to remove castle behind Oc'Nivelle...", ConsoleColor.Magenta);
                if (Server.World.FindItem(0x4004A942) is CustomRegionControl crc)
                {
                    List<Rectangle3D> list = new(crc.CustomRegion.Coords);
                    crc.Registered = false;
                    Rectangle2D area = new Rectangle2D();
                    foreach (Rectangle3D r3d in list)
                    {
                        area.MakeHold(new Point2D(r3d.Start.X, r3d.Start.Y));
                        area.MakeHold(new Point2D(r3d.End.X, r3d.End.Y));
                    }
                    int landZ = 0, landAvg = 0, landTop = 0;
                    crc.Map.GetAverageZ(crc.X, crc.Y, ref landZ, ref landAvg, ref landTop);
                    BaseHouse bh = BaseHouse.FindHouseAt(crc);
                    if (bh != null)
                    {
                        int lenientZ = bh.Components.Height;
                        List<Rectangle2D> bigrect = new() { area };
                        Type[] exclude = new Type[] { typeof(Spawner) };
                        Utility.RefreshDeco(crc.Map, bigrect, startingZ: landAvg,
                            typeExclude: exclude, lenientZ: int.MinValue/*lenientZ*/);
                    }


                    EchoOut(string.Format("Castle behind Oc'Nivelle removed. {0} items updated", patched), ConsoleColor.Magenta);
                    return patched;
                }
                else
                {
                    ErrorOut(bits, "Did not find castle at expected location.", ConsoleColor.Red);
                }
            }
            return null;
        }
        #endregion PatchV18
        #region PatchV19
        private static int? PatchV19(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV19;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch of static regions...", ConsoleColor.Magenta);
                /*
                 * If you want to check out the current regioning, do the following:
                 * 1. Pull my changes
                 * 2. Delete Saves/StaticRegions.xml
                 * 3. Boot server
                 * 4. Run [nuke PatchRegionControllers
                 */
                string filename = Path.Combine("Saves/", "StaticRegions.xml");
                bool complete = false;
                if (System.IO.File.Exists(filename))
                {
                    try { System.IO.File.Delete(filename); }
                    catch { ErrorOut(bits, string.Format("Error:PatchV19: Cannot delete : {0}", filename), ConsoleColor.Red); }
                    if (!System.IO.File.Exists(filename))
                    {   // it was deleted
                        patched = Server.Regions.StaticRegion.PatchRegionControllers();
                        complete = true;
                    }
                }
                else
                {
                    patched = Server.Regions.StaticRegion.PatchRegionControllers();
                    complete = true;
                }

                if (complete == true)
                {

                    EchoOut(string.Format("{0} static regions patched.", patched), ConsoleColor.Magenta);
                    return patched;
                }
                else
                    ErrorOut(bits, "Logic Error: Unable to patch static regions '{0}'.", ConsoleColor.Red, "StaticRegions.xml");
            }
            return null;
        }
        #endregion PatchV19
        #region PatchV20
        private static int? PatchV20(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV20;

            if (StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to initialize Felucca deco...", ConsoleColor.Magenta);

                List<Item> before = Before();

                // Moongates
                PublicMoongate.Generate(PMList.Felucca);

                // add the teleporters
                TeleportersCreator tc = new TeleportersCreator();
                tc.CreateTeleporters(listOnly: false, new Map[] { Map.Felucca });

                // add the signs
                SignParser.Parse(Server.World.GetSystemAcct(), null, new Map[] { Map.Felucca });

                // now the doors
                DoorGenerator.Generate(null, new Map[] { Map.Felucca });

                // Felucca
                Generate(
                    Path.Combine(Core.DataDirectory, "Decoration/Felucca"),
                    DecoMode.add,
                    file: "*.cfg",
                    listOnly: false,
                    types: null,
                    boundingRect: null,
                    exclude: null,
                    maps: new Map[] { Map.Felucca });

                // Britannia
                Generate(
                    Path.Combine(Core.DataDirectory, "Decoration/Britannia"),
                    DecoMode.add,
                    file: "*.cfg",
                    listOnly: false,
                    types: null,
                    boundingRect: null,
                    exclude: null,
                    maps: new Map[] { Map.Felucca });

                List<Item> after = After();

                // what's the delta?
                patched = Delta(before, after).Count;

                return patched;
            }

            return null;
        }
        #endregion PatchV20
        #region PatchV21
        private static int? PatchV21(int patchid)
        {   // wipe the unusual container cache in anticipation of 'decoration' unexpectedly deleting these containers.
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV21;
            if (StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to wipe the unusual container cache.", ConsoleColor.Magenta);
                Mute();
                try
                {
                    patched = UnusualContainerSpawner.WipeContainerCache();
                }
                finally
                {
                    Unmute();
                }

                EchoOut(string.Format("{0} unusual container cache cleared.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV21
        #region PatchV22
        private static int? PatchV22(int patchid)
        {
            int patched = 0;
            if (StandardShards())
            {
                EchoOut("One-time patch to remove some Felucca teleporters and surrounding deco.", ConsoleColor.Magenta);
                EchoOut("This patch will run about 5 minutes...", ConsoleColor.Yellow);
                Mute();
                try
                {
                    List<Item> before = Before(Map.Felucca);

                    HasPatchedFeluccaTeleportersWorker();

                    List<Item> after = After(Map.Felucca);

                    // what's the delta?
                    patched = Delta(before, after).Count;
                }
                finally
                {
                    Unmute();
                }
                EchoOut(string.Format("{0} Felucca teleporters removed or updated.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV22
        #region PatchV23
        private static int? PatchV23(int patchid)
        {   // rebuild the unusual container cache
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV23;
            if (StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to rebuild the unusual container cache.", ConsoleColor.Magenta);
                Mute();
                try
                {
                    patched = UnusualContainerSpawner.RebuildContainerCache();
                }
                finally
                {
                    Unmute();
                }

                EchoOut(string.Format("{0} unusual container cache rebuild.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV23
        #region PatchV24
        private static int? PatchV24(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV24;
            if (AngelIsland())
            {
                LogPatch(bits);
                EchoOut("One-time patch to remove some Angel Island Felucca teleporters...", ConsoleColor.Magenta);

                // get a list of legal items
                string pathName = Path.Combine(Core.DataDirectory, "Patches", "ExcludeTeleporterAI.cfg");
                Utility.EnsurePath(pathName);
                if (File.Exists(pathName))
                {

                    List<Tuple<int, int, int, int, Map>> records = new();
                    foreach (string line in File.ReadAllLines(pathName))
                    {
                        string[] toks = line.Split(' ');
                        records.Add(new Tuple<int, int, int, int, Map>(int.Parse(toks[0]), int.Parse(toks[1]), int.Parse(toks[2]), int.Parse(toks[3]), Map.Parse(toks[4])));
                    }

                    // sanity
                    int count = 0;
                    foreach (var record in records)
                    {   // we will pack out bad items in Patcher.PatchV7()
                        //  we probably captured an EffectItem or corpse(and items) or something
                        Item item = Server.World.FindItem(record.Item1);
                        if (item == null || item.Deleted)
                            count++;
                        else if (item is Teleporter && item.Location == new Point3D(record.Item2, record.Item3, record.Item4))
                        {
                            item.Delete();
                            patched++;
                        }
                    }


                    EchoOut(string.Format("{0} Angel Island Felucca teleporters removed.", patched), ConsoleColor.Magenta);
                    return patched;
                }
                else
                    ErrorOut(bits, string.Format("Error: unable to locate {0}.", pathName), ConsoleColor.Red);
            }
            return null;
        }
        #endregion PatchV24
        #region PatchV25
        private static int? PatchV25(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV25;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to remove Champion Teleporters...", ConsoleColor.Magenta);

                int index = ConsoleCaptureOn();

                Dictionary<Item, Tuple<Point3D, Map, bool>> ChangeLog =
                    Decorate.Generate(Path.Combine(Core.DataDirectory, "Decoration/Felucca"), Decorate.DecoMode.delete, "_champion teleporters.cfg", maps: new Map[] { Map.Felucca });
                int tmp = ChangeLog.Count;

                patched = ConsoleCaptureOff(index);


                EchoOut(string.Format("{0} Champion Teleporters removed.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV25
        #region PatchV26
        private static int? PatchV26(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV26;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to remove illegal Trammel items...", ConsoleColor.Magenta);

                patched = DeleteAllIllegalTrammelItems(new CommandEventArgs());


                EchoOut(string.Format("{0} illegal Trammel items removed.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        private static int DeleteAllIllegalTrammelItems(CommandEventArgs e)
        {
            int deleted = 0;
            // first, read the list of items to exclude from the trammel search
            string pathName = Path.Combine(Core.DataDirectory, "Patches", "LegalTrammelItems.cfg");
            Utility.EnsurePath(pathName);
            if (File.Exists(pathName))
            {
                List<Tuple<int, int, int, int, Map>> records = new();
                foreach (string line in File.ReadAllLines(pathName))
                {
                    string[] toks = line.Split(' ');
                    records.Add(new Tuple<int, int, int, int, Map>(int.Parse(toks[0]), int.Parse(toks[1]), int.Parse(toks[2]), int.Parse(toks[3]), Map.Parse(toks[4])));
                }

                // log the items
                LogHelper logger = new LogHelper("OrphanedTrammelItems.log", true, true, true);
                foreach (Item item in Server.World.Items.Values)
                    if (item != null && item.Deleted == false && item.Map == Map.Trammel)
                        if (item is not EffectItem)
                            if (!records.Any(t => t.Item1 == item.Serial.Value))
                                if (item is Item)
                                    logger.Log(LogType.Item, item);

                logger.Finish();

                // delete the items
                int starting = 0;
                foreach (Item item in Server.World.Items.Values)
                    if (item != null && item.Deleted == false && item.Map == Map.Trammel)
                        if (item.RootParent is not Mobile)
                            if (item is not EffectItem)
                            {
                                starting++;
                                if (!records.Any(t => t.Item1 == item.Serial.Value))
                                    if (item is Item)
                                    {
                                        item.Delete();
                                        deleted++;
                                    }
                            }

                if (e.Mobile != null)
                    e.Mobile.SendMessage("{0}/{1} Trammel items deleted.", deleted, starting);
            }
            else if (e.Mobile != null)
                e.Mobile.SendMessage("Cannot find {0}", pathName);

            return deleted;
        }
        #endregion PatchV26
        #region PatchV27
        private static int? PatchV27(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV27;
            // test center doesn't have these teleporters
            if (AllShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time patch to fix ConPvP teleporters...", ConsoleColor.Magenta);
                if (!Core.GMNTC_CFG)
                {
                    #region comments
                    /* Destination locations: (367, 904, 0), (365, 906, 0), (367, 908, 0), (371, 905, 0)
                     * These four spawners get one of the above destinations: 0x40059DF7, 0x40044B1A, 0x40041922, 0x4004190A

                     *  - Vesper Bank -
                     * sign at ((2875, 673, 17)) placard: 7977 (0x1F29), Name Dueling Arena (movable = false)
                     * post at {(2874, 672, -3)} post 9 (0x09) (movable = false)

                     * large crate (0x4000FC7A), move X+1, Y-2
                     * large crate (0x4000FC78) move to (2872, 676, 2)
                     * large create 0x4000FC75, move to (2875, 672, 3)
                     * 
                     * tele 0x40059DF7, move to (2874, 674, 0)
                     * tele 0x40044B1A, move to (2875, 674, 0)
                    */
                    #endregion comments
                    Teleporter tp1 = Patcher.FindItem(typeof(Teleporter), Map.Felucca, new Point3D(2874, 673, 0)) as Teleporter;
                    Teleporter tp2 = Patcher.FindItem(typeof(Teleporter), Map.Felucca, new Point3D(2875, 673, 0)) as Teleporter;
                    Teleporter tp3 = Patcher.FindItem(typeof(Teleporter), Map.Felucca, new Point3D(1173, 3625, 6)) as Teleporter;
                    Teleporter tp4 = Patcher.FindItem(typeof(Teleporter), Map.Felucca, new Point3D(1172, 3625, 5)) as Teleporter;

                    if (tp1 != null && tp2 != null && tp3 != null && tp4 != null)
                    {   // got our Teleporters
                        tp1.PointDest = new Point3D(367, 904, 0); patched++;
                        tp2.PointDest = new Point3D(365, 906, 0); patched++;
                        tp3.PointDest = new Point3D(367, 908, 0); patched++;
                        tp4.PointDest = new Point3D(371, 905, 0); patched++;

                        Item placard = new Static(0x1F29); patched++;
                        Item post = new Static(0x09); patched++;
                        placard.Movable = false; patched++;
                        placard.Location = new Point3D(2875, 673, 17); patched++;
                        placard.Name = "Dueling Arena"; patched++;
                        placard.Map = Map.Felucca; patched++;
                        post.Movable = false; patched++;
                        post.Location = new Point3D(2874, 672, -3); patched++;
                        post.Map = Map.Felucca; patched++;

                        if (Server.World.FindItem(0x4000FC7A) is LargeCrate lc1 && Server.World.FindItem(0x4000FC78) is LargeCrate lc2 && Server.World.FindItem(0x4000FC75) is LargeCrate lc3)
                        {
                            lc1.Location = new Point3D(lc1.X + 1, lc1.Y - 2, lc1.Z); patched++;
                            lc2.Location = new Point3D(2872, 676, 2); patched++;
                            lc3.Location = new Point3D(2875, 672, 3); patched++;
                        }


                        EchoOut(string.Format("{0} ConPvP teleporter elements fixed.", patched), ConsoleColor.Magenta);
                        return patched;
                    }
                    else
                    {
                        ErrorOut(bits, string.Format("One or more teleporters missing from expected locations."), ConsoleColor.Red);
                    }
                }
                else
                {
                    EchoOut(string.Format("{0} ConPvP teleporter elements fixed. Nothing to do.", patched), ConsoleColor.Magenta);
                    return patched;
                }
            }
            return null;
        }
        #endregion PatchV27
        #region PatchV28
        private static int? PatchV28(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV28;
            if (AllShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time patch to fix vampire champ teleporters...", ConsoleColor.Magenta);
                // patch vampire teleporters by setting them to Z 1
                // They were at Z 0, and OnMoveOver fails if the teleporter Z < than the mobiles Z
                // 0x401C2190
                // 0x401C23DC
                ////
                if (Server.World.FindItem(0x401C2190) is Teleporter t1 && Server.World.FindItem(0x401C23DC) is Teleporter t2)
                {
                    t1.Z = t2.Z = 1;
                    patched += 2;

                    EchoOut(string.Format("{0} vampire champ teleporters fixed.", patched), ConsoleColor.Magenta);
                    return patched;
                }
                else
                {
                    ErrorOut(bits, string.Format("Error: unable to locate one or more of the vampire champ teleporters.", patched), ConsoleColor.Red);
                }
            }
            return null;
        }
        #endregion PatchV28
        #region PatchV29
        private static int? PatchV29(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV29;
            if (AllShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time patch to fix Green Acres teleporters...", ConsoleColor.Magenta);

                Teleporter tp1 = Patcher.FindItem(typeof(Teleporter), Map.Felucca, new Point3D(5440, 1153, 0)) as Teleporter;
                Teleporter tp2 = Patcher.FindItem(typeof(Teleporter), Map.Felucca, new Point3D(5440, 1151, 0)) as Teleporter;
                Moongate mg1 = Patcher.FindItem(typeof(Moongate), Map.Felucca, new Point3D(5440, 1149, 0)) as Moongate;
                Teleporter tp3 = Patcher.FindItem(typeof(Teleporter), Map.Felucca, new Point3D(5440, 1147, 0)) as Teleporter;

                if (tp1 != null && tp2 != null && mg1 != null && tp3 != null)
                {
                    tp1.Location = new Point3D(5437, 1150, 0);
                    tp2.Location = new Point3D(5437, 1148, 0);
                    tp3.Location = new Point3D(5437, 1144, 0);

                    Teleporter tp4 = new Teleporter(mg1.PointDest, mg1.MapDest);
                    tp4.MoveToWorld(new Point3D(5437, 1146, 0), Map.Felucca);
                    mg1.Delete();

                    List<Point3D> points = new() { new Point3D(5436, 1150, 0), new Point3D(5436, 1148, 0), new Point3D(5436, 1146, 0), new Point3D(5436, 1144, 0) };
                    List<string> texts = new() { "West Britain Bank", "Last Man Standing", "New Player Starting Area", "Capture The Flag" };
                    for (int ix = 0; ix < 4; ix++)
                    {
                        Item post = new Static(0xA9); patched++;
                        post.Movable = false; patched++;
                        post.Map = Map.Felucca; patched++;
                        post.Location = points[ix]; patched++;

                        Item placard = new Static(0x1F28); patched++;
                        placard.Movable = false; patched++;
                        placard.Map = Map.Felucca; patched++;
                        placard.Location = points[ix]; patched++;
                        placard.Location = new Point3D(points[ix].X, points[ix].Y, points[ix].Z + 12); patched++;
                        placard.Name = texts[ix]; patched++;
                    }

                    // clean up old sign and sparkles at and near old tp1 location
                    Point3D garbage = new Point3D(5440, 1153, 0);
                    List<Item> garbagecan = new();
                    IPooledEnumerable eable = Map.Felucca.GetItemsInRange(garbage, 1);
                    foreach (Item item in eable)
                        if (item.ItemID == 0x375A)          // sparkles
                        {
                            garbagecan.Add(item); patched++;
                        }
                        else if (item.ItemID == 0xA9)       // post
                        {
                            garbagecan.Add(item); patched++;
                        }
                        else if (item.ItemID == 0x1F28)     // placard
                        {
                            garbagecan.Add(item); patched++;
                        }
                    eable.Free();

                    foreach (Item item in garbagecan)
                        item.Delete();


                    EchoOut(string.Format("{0} Green Acres teleporter elements fixed.", patched), ConsoleColor.Magenta);
                    return patched;
                }
                else
                {
                    if (Core.GMNTC_CFG)
                    {
                        EchoOut(string.Format("{0} Green Acres teleporter elements fixed. Nothing to do.", patched), ConsoleColor.Magenta);
                        return patched;
                    }
                    else
                        ErrorOut(bits, string.Format("One or more teleporters missing from expected locations."), ConsoleColor.Red);
                }
            }
            return null;
        }
        #endregion PatchV29
        #region PatchV30
        private static int? PatchV30(int patchid)
        {  // wipe stable, restore Premier Gems (Standard Shards)
            // WBB Jewler
            //  Premier Gems,
            // (1448, 1676)+(11, 23)
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV30;
            if (StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to restore Premier Gems to Britain...", ConsoleColor.Magenta);

                // our rect around the stable
                Rectangle2D rect = new Rectangle2D(1448, 1676, 13, 23);
                List<Rectangle2D> rects = new List<Rectangle2D>() { rect };

                // mini deco
                // we will wipe everything except spawners (they weren't put there by RunUO)
                patched = Utility.RefreshDeco(Map.Felucca, rects, startingZ: 0,
                    typeExclude: new Type[] { typeof(Spawner) }, lenientZ: int.MinValue/*lenientZ: 5*/);

                // now add the sign
                SignParser.Parse(World.GetSystemAcct(), rects, new Map[] { Map.Felucca });

                // now the doors
                DoorGenerator.Generate(rects, new Map[] { Map.Felucca });


                EchoOut(string.Format("Premier Gems restored to Britain. {0} items updated", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV30
        #region PatchV31
        private static int? PatchV31(int patchid)
        {  // wipe the forge and deco behind WBB
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV31;
            if (StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to remove the forge and deco behind West Britain Bank...", ConsoleColor.Magenta);
                // serial of the anvil 0x4020EE97
                Item anvil = World.FindItem(0x4020EE97);
                if (anvil != null)
                {
                    List<Rectangle2D> rects = new List<Rectangle2D>() { new Rectangle2D(anvil.X, anvil.Y, 2, 3) };
                    patched = Utility.RefreshDeco(anvil.Map, rects, startingZ: anvil.Z,
                        typeExclude: new Type[] { typeof(Spawner) }, lenientZ: int.MinValue/*0*/);


                    EchoOut(string.Format("Removal of the forge and deco behind West Britain ({0} items updated) Bank complete.", patched), ConsoleColor.Magenta);
                    return patched;
                }
                else
                {
                    ErrorOut(bits, "Did not find anvil at expected location.", ConsoleColor.Red);
                }
            }
            return null;
        }
        #endregion PatchV31
        #region PatchV32
        private static int? PatchV32(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV32;
            if (AllShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time patch to fix broken doors at 1133 2237 40...", ConsoleColor.Magenta);
                Item door1 = World.FindItem(0x40010E34);
                Item door2 = World.FindItem(0x40010E3D);
                Item door3 = World.FindItem(0x40010E33);
                Item door4 = World.FindItem(0x40010E3C);
                if (door1 != null && door2 != null && door3 != null && door4 != null)
                {
                    door1.X++; patched++;
                    door2.X++; patched++;
                    door3.X++; patched++;
                    door4.X++; patched++;

                    EchoOut(string.Format("Doors fixed. {0} items updated", patched), ConsoleColor.Magenta);
                    return patched;
                }
                else
                {
                    ErrorOut(bits, "Did not find door(s) at expected location.", ConsoleColor.Red);
                }
            }
            return null;
        }
        #endregion PatchV32
        #region PatchV33
        private static int? PatchV33(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV33;
            if (StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to respawn the world with classic UO spawn.", ConsoleColor.Magenta);
                EchoOut("This patch will take several minutes...", ConsoleColor.Yellow);

                var before = Before();
                Mute();
                try
                {
                    SpawnerManager.ClassicRespawn();
                }
                finally
                {
                    Unmute();
                }
                var after = After();

                patched = Delta(before, after).Count;


                EchoOut(string.Format("{0} spawners updated", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV33
        #region PatchV34
        private static int? PatchV34(int patchid)
        {
            int patched = 0;
            // this patch can only run after a [classicRespawn
            if (
                (Patched(PatchIndex.PatchV33) && StandardShards()) ||
                (AngelIsland())
                )
            {
                EchoOut("One-time patch of bird spawn. spawning...", ConsoleColor.Magenta);

                var before = Before();

                // spawn our new birds where ever birds are spawned in WildLife.map
                string[] files = new string[] { Path.Combine(Core.DataDirectory, "Spawners/Nerun's Distro/Spawns/felucca/WildLife.map") };
                foreach (string file in files)
                    HasPatchedBirdsWorker(file, "bird", "Crow:Magpie:Raven");

                // spawn crows at the Graveyards
                files = new string[] { Path.Combine(Core.DataDirectory, "Spawners/Nerun's Distro/Spawns/felucca/Graveyards.map") };
                foreach (string file in files)
                    HasPatchedBirdsWorker(file, "skeleton", "Crow");

                var after = After();

                patched = Delta(before, after).Count;

                EchoOut(string.Format("{0} birds spawned.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        private static void HasPatchedBirdsWorker(string file, string match, string replacement)
        {
            foreach (string line in System.IO.File.ReadLines(file))
            {
                // find matching lines
                if (Regex.Match(line, @"\b(" + match + @")\b", RegexOptions.IgnoreCase).Success)
                {
                    List<NerunRecord.Record> recordList = new();
                    List<string> compiledTokens = new();
                    string[] lineToks = line.Split(new char[] { '|' });                     // parse on '|'
                    if (lineToks.Length == 22)                                              // always 22 fields
                    {
                        CompileLine(lineToks, compiledTokens);                              // compile this line
                        NerunRecord.Parse(compiledTokens.ToArray(), recordList);            // parse into a record list
                        Spawner spawner = SpawnerFromCompiledTokens(compiledTokens);        // create the spawner based on the specs

                        for (int ix = 0; ix < compiledTokens.Count; ix++)
                        {
                            if (compiledTokens[ix].ToLower().Contains(match))
                            {
                                int count;
                                /*
                                if (ix == 0)
                                    count = spawner.Count;                                  // we found the bird in the first cell, this cell is controlled by 'Count'
                                else
                                    count = spawner.EntryCount[ix - 1];                     // we found 'bird' in a cell > 0, so use that entry count
                                */
                                count = spawner.GetSlotCount(ix);
                                //Array.Fill(spawner.EntryCount, 0);                          // wipe the existing notion of EntryCounts
                                spawner.WipeSlots();
                                spawner.Count = count;                                      // we're only adding one entry, so we use the 'Count' field
                                spawner.ObjectNamesRaw.Add(replacement);                    // Add our birds!
                                break;
                            }
                        }
                        // else: it will always be '22' (a valid record) since we are only passing it a valid record.

                        // the file created by this function is read and processed by all shards, and spawners are updated accordingly
                        UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false, uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                        // set the spawner mode
                        if (replacement.Contains(':'))                                      // important:
                            spawner.ModeMulti = true;                                       // if the replacement we added contains colon delimited fields, it must be a ModeMulti
                        else                                                                // otherwise,
                            spawner.ModeAI = true;                                          // it's a classic AI spawner

                        // convert to Concentric for the wide area spawners
                        if (spawner.HomeRange > 25)                                         // better distribution
                            spawner.Concentric = true;

                        // place it (<== seems a bit anticlimactic)
                        spawner.MoveToWorld(spawner.Location, Map.Felucca);

                        // now drop a control console on top of the pile if there are more than one stacked spawners, and there isn't already
                        //  a console there.
                        if (NeedsConsole(spawner.X, spawner.Y, spawner.Z, spawner.Map) && !AlreadySpawned(spawner.X, spawner.Y, spawner.Z, spawner.Map))
                        {
                            Server.Items.StackedSpawnerConsole stackedSpawnerConsole = new Server.Items.StackedSpawnerConsole();
                            stackedSpawnerConsole.Movable = false;
                            stackedSpawnerConsole.Visible = false;
                            stackedSpawnerConsole.MoveToWorld(new Point3D(spawner.X, spawner.Y, spawner.Z + 1), spawner.Map);
                        }

                        // as usual, log it
                        string output = string.Format("Add {0} spawner at {1}", replacement, spawner.Location);
                        LogHelper logger = new LogHelper("adding birds.log", false, true);
                        logger.Log(LogType.Text, output);
                        logger.Finish();
                    }
                }
            }
        }
        public static bool AlreadySpawned(int X, int Y, int Z, Map map)
        {
            foreach (object o in map.GetObjectsInBounds(new Rectangle2D(X, Y, 1, 1)))   // does 0,0 work? seems not
                if (o is Server.Items.StackedSpawnerConsole)
                    return true;
            return false;
        }
        public static bool NeedsConsole(int X, int Y, int Z, Map map)
        {
            int count = 0;
            foreach (object o in map.GetObjectsInBounds(new Rectangle2D(X, Y, 1, 1)))   // does 0,0 work? seems not
                if (o is Spawner spawner)
                {
                    if (spawner.X == X && spawner.Y == Y)
                        count++;
                }
            return count > 1;
        }
        #endregion PatchV34
        #region PatchV35
        private static int? PatchV35(int patchid)
        {
            int patched = 0;
            if (AngelIsland())
            {
                EchoOut("One-time patch of Angel Island's Moonglow Zoo running...", ConsoleColor.Magenta);

                // get Moonglow Zoo spawners
                List<string> mzLines = GetMoonglowSpawnerEntries();

                // create spawners at moonglow zoo and bless the creatures so they can't be tamed, don't attack, can't be barded, etc.
                foreach (string line in mzLines)
                {
                    List<NerunRecord.Record> recordList = new();
                    List<string> compiledTokens = new();
                    string[] lineToks = line.Split(new char[] { '|' });                 // parse on '|'
                    CompileLine(lineToks, compiledTokens);                              // compile this line
                    NerunRecord.Parse(compiledTokens.ToArray(), recordList);            // parse into a record list

                    foreach (NerunRecord.Record record in recordList)
                    {
                        // record the mobiles found
                        List<Mobile> mobList = new();

                        // get the area rect of these mobiles
                        Rectangle2D rect = new Rectangle2D(record.X, record.Y, 1, 1);
                        // grow the rect to the 'room size' of the mobiles
                        rect = new Utility.SpawnableRect(rect).MaximumSpawnableRect(Map.Felucca, 30, Map.Felucca.GetAverageZ(rect.X, rect.Y));

                        foreach (object o in Map.Felucca.GetObjectsInBounds(rect))
                            if (o is Mobile mob && mob.Blessed == true)
                                mobList.Add(mob);

                        // create the spawner
                        Spawner spawner = Activator.CreateInstance(typeof(Spawner)) as Spawner;
                        spawner.MoveToWorld(new Point3D(record.X, record.Y, record.Z), Map.Felucca);
                        patched++;

                        // add the mobiles
                        foreach (Mobile m in mobList)
                            spawner.ObjectNamesRaw.Add(m.GetType().Name);

                        // set up the spawn count for each of these creatures
                        for (int jx = 0; jx < spawner.ObjectNamesRaw.Count; jx++)
                            spawner.SetSlotCount(index: jx, 1);
                        /*if (jx == 0)
                            spawner.Count = 1;
                        else
                            spawner.EntryCount[jx - 1] = 1;*/

                        // special case for our reaper
                        if (spawner.ObjectNamesRaw.Count == 1 && (spawner.ObjectNamesRaw[0] as string).ToLower() == "reaper")
                        {
                            spawner.HomeRange = 0;
                        }
                        else
                        {
                            spawner.ModeMulti = true;                                   // tell the spawner we're a multi spawner
                            spawner.HomeRange = 1;                                      // just a seed, will be thrown away
                            spawner.WalkRangeCalc = true;                               // automatically calculate the best walking range
                        }
                        spawner.Invulnerable = true;

                        // delete the old mobiles
                        foreach (Mobile m in mobList)
                            m.Delete();

                        // respawn the new
                        spawner.ScheduleRespawn = true;
                    }

                }

                EchoOut(string.Format("{0} Angel Island's Moonglow Zoo elements patched.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }

        #endregion PatchV35
        #region PatchV36
        private static int? PatchV36(int patchid)
        {
            int patched = 0;
            PatchIndex bits = CoreAI.PatchIndex.PatchV36;
            if (StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch of standard shard's Moonglow Zoo running...", ConsoleColor.Magenta);

                // get Moonglow Zoo spawners
                List<string> mzLines = GetMoonglowSpawnerEntries();

                // set spawners at moonglow zoo to bless the creatures so they can't be tamed, don't attack, can't be barded, etc.
                foreach (string line in mzLines)
                {
                    List<NerunRecord.Record> recordList = new();
                    List<string> compiledTokens = new();
                    string[] lineToks = line.Split(new char[] { '|' });                 // parse on '|'
                    CompileLine(lineToks, compiledTokens);                              // compile this line
                    NerunRecord.Parse(compiledTokens.ToArray(), recordList);            // parse into a record list
                    List<Spawner> allSpawnersAt = new List<Spawner>(AllModeNerunsSpawnersAt(recordList[0].X, recordList[0].Y, Map.Felucca));
                    foreach (Spawner spawner in allSpawnersAt)
                    {
                        spawner.Invulnerable = true;
                        spawner.RemoveObjects();
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                }

                EchoOut(string.Format("{0} standard shard's Moonglow Zoo elements patched.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV36
        #region PatchV37
        private static int? PatchV37(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV37;
            if (AllShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time patch of Moonglow Zoo on all shards running...", ConsoleColor.Magenta);

                // first, wipe the area of mobiles (likely blessed from AI)
                Region reg = Region.FindByName("Britannia Royal Zoo", Map.Felucca);
                if (reg != null)
                {
                    foreach (Rectangle3D rect in reg.Coords)
                    {
                        Rectangle2D rect2D = new(rect.Start.X, rect.Start.Y, rect.Width, rect.Height);
                        patched += WipeRect(Map.Felucca, rect2D,
                            Utility.GetAverageZ(Map.Felucca, rect2D.Center.X, rect2D.Center.Y),
                            flags: WipeRectFlags.Mobiles);
                    }
                }
                else
                {
                    ErrorOut(bits, "Logic Error: Unable to find Moonglow zoo to wipe.", ConsoleColor.Red);
                    return 0;
                }

                // next, set all the spawners to 'Exhibit' creatures
                foreach (Rectangle3D rect in reg.Coords)
                {
                    IPooledEnumerable eable = Map.Felucca.GetItemsInBounds(new Rectangle2D(rect.Start.X, rect.Start.Y, rect.Width, rect.Height));

                    foreach (Item item in eable)
                    {
                        if (item is Spawner spawner && !spawner.Deleted)
                        {
                            foreach (object o in spawner.Objects)
                            {
                                if (o is BaseCreature bc)
                                {   // leave guards and escortables alone
                                    if (!bc.Body.IsHuman)
                                    {   // we don't mix monsters and humans at this location
                                        spawner.Exhibit = true;
                                        spawner.RemoveObjects();
                                        patched++;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    eable.Free();
                }

                // next lock all the doors and remove signs (standard shards only)
                foreach (Rectangle3D rect in reg.Coords)
                {
                    List<Item> list = new();
                    IPooledEnumerable eable = Map.Felucca.GetItemsInBounds(new Rectangle2D(rect.Start.X, rect.Start.Y, rect.Width, rect.Height));
                    foreach (Item item in eable)
                    {
                        if (item is LocalizedSign)
                            continue;
                        else if (item is CustomRegionControl)
                            continue;
                        else if (item is BaseDoor bd)
                        {
                            bd.Locked = true;
                            patched++;
                        }
                        // south facing sign, north facing sign
                        //  only delete for standard shards
                        else if (StandardShards(quiet: true) && item.ItemID == 0xBD1 || item.ItemID == 0xBD2)
                        {
                            list.Add(item);
                            patched++;
                        }

                    }
                    eable.Free();

                    // delete all the items
                    foreach (Item item in list)
                        item.Delete();
                }


                EchoOut(string.Format("{0} Moonglow Zoo on all shards patched.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV37
        #region PatchV38
        private static int? PatchV38(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV38;
            if (Patched(PatchIndex.PatchV33) == true &&
                StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch of visible spawners, hiding...", ConsoleColor.Magenta);

                foreach (Item item in Server.World.Items.Values)
                {
                    if (item is EventSpawner) continue;
                    if (item.Map != Map.Felucca) continue;

                    if (item is Spawner spawner && spawner.Visible == true)
                    {
                        spawner.Visible = false;
                        patched++;
                    }
                }


                EchoOut(string.Format("{0} visible spawners hidden.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV38
        #region PatchV39
        private static int? PatchV39(int patchid)
        {   // While old moongates had a notion of DestinationOverride, teleporters did not.
            //  The result is that teleporters that send you to, say another map, fail because it's is against the travel rules.
            // Set all existing to true
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV39;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to set DestinationOverride to true for all teleporters.", ConsoleColor.Magenta);

                foreach (Item item in Server.World.Items.Values)
                    if (item is Teleporter teleporter && !teleporter.Deleted)
                    {
                        patched++;
                        teleporter.DestinationOverride = true;
                    }


                EchoOut(string.Format("{0} DestinationOverride patched.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV39
        #region PatchV40
        private static int? PatchV40(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV40;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to replace all moongates with sungates.", ConsoleColor.Magenta);
                Mute();
                try
                {
                    // get the list of moongates
                    string pathName = Path.Combine(Core.DataDirectory, "Patches", "Angel Island Moongate.cfg");
                    EnsurePath(pathName);
                    if (File.Exists(pathName))
                    {

                        List<Tuple<int, int, int, int, Map>> records = new();
                        foreach (string line in File.ReadAllLines(pathName))
                        {
                            string[] toks = line.Split(' ');
                            records.Add(new Tuple<int, int, int, int, Map>(int.Parse(toks[0]), int.Parse(toks[1]), int.Parse(toks[2]), int.Parse(toks[3]), Map.Parse(toks[4])));
                        }

                        int count = 0;
                        foreach (var record in records)
                        {
                            Item item = Server.World.FindItem(record.Item1);
                            if (item == null || item.Deleted)
                                count++;
                            else if (item is Moongate && item.Location == new Point3D(record.Item2, record.Item3, record.Item4))
                            {
                                if (item.Serial == 0x400040A1)
                                {   // special New Player Starting area
                                    Moongate moongate;
                                    moongate = (Moongate)item;
                                    Sungate sungate = new Sungate();
                                    CopyProperties(moongate, sungate);
                                }
                                else if (item.GetType() == typeof(ConfirmationMoongate))
                                {
                                    ConfirmationMoongate moongate;
                                    moongate = (ConfirmationMoongate)item;
                                    EventConfirmationSungate sungate = new EventConfirmationSungate();
                                    CopyProperties(moongate, sungate);
                                    sungate.Running = false;
                                    sungate.Visible = false;
                                }
                                // JailExitGates are no more
                                //else if (item.GetType() == typeof(JailExitGate))
                                //{
                                //    JailExitGate moongate;
                                //    moongate = (JailExitGate)item;
                                //    JailExitSungate sungate = new JailExitSungate();
                                //    CopyProperties(moongate, sungate);
                                //    sungate.Running = false;
                                //    sungate.Visible = false;
                                //}
                                else if (item.GetType() == typeof(Moongate))
                                {
                                    Moongate moongate;
                                    moongate = (Moongate)item;
                                    EventSungate sungate = new EventSungate();
                                    CopyProperties(moongate, sungate);
                                    sungate.Running = false;
                                    sungate.Visible = false;
                                }
                                else
                                    ErrorOut(bits, "Logic Error: Unknown moongate type {0}.", ConsoleColor.Red, item);

                                item.Delete();
                                patched++;
                            }
                        }
                        Unmute();

                        EchoOut(string.Format("{0} moongates replaced.", patched), ConsoleColor.Magenta);
                    }
                    else
                        ErrorOut(bits, string.Format("Error: unable to locate {0}.", pathName), ConsoleColor.Red);
                }
                finally { Unmute(); }
                return patched;
            }
            return null;
        }
        #endregion PatchV40
        #region PatchV41
        private static int? PatchV41(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV41;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to remove Lost Lands access...", ConsoleColor.Magenta);
                string filename = "LostLandsAccessTeleporters.log";
                LogHelper logger = new LogHelper(filename, true, true, true);
                foreach (Item item in Server.World.Items.Values)
                    if (item is Teleporter teleporter && !teleporter.Deleted && teleporter.Running)
                        if (TeleporterRuleHelpers.FromBrit(teleporter) && TeleporterRuleHelpers.ToLostLands(teleporter))
                        {
                            logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", teleporter.Serial.Value, teleporter.X, teleporter.Y, teleporter.Z, teleporter.Map));
                            patched++;
                            teleporter.Running = false;
                        }
                logger.Finish();

                EchoOut(string.Format("{0} Lost Lands access removed.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV41
        #region PatchV42
        private static int? PatchV42(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV42;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to replace all event teleporters with EventTeleporters.", ConsoleColor.Magenta);
                Mute();
                try
                {
                    // get the list of moongates
                    string pathName = Path.Combine(Core.DataDirectory, "Patches", "EventTeleporters.cfg");
                    EnsurePath(pathName);
                    if (File.Exists(pathName))
                    {

                        List<Tuple<int, int, int, int, Map>> records = new();
                        foreach (string line in File.ReadAllLines(pathName))
                        {
                            string[] toks = line.Split(' ');
                            records.Add(new Tuple<int, int, int, int, Map>(int.Parse(toks[0]), int.Parse(toks[1]), int.Parse(toks[2]), int.Parse(toks[3]), Map.Parse(toks[4])));
                        }

                        int count = 0;
                        foreach (var record in records)
                        {
                            Item item = Server.World.FindItem(record.Item1);
                            if (item == null || item.Deleted)
                                count++;
                            else if (item is Teleporter && item.Location == new Point3D(record.Item2, record.Item3, record.Item4))
                            {
                                if (item.GetType() == typeof(Teleporter))
                                {
                                    Teleporter teleporter;
                                    teleporter = (Teleporter)item;
                                    EventTeleporter etp = new EventTeleporter();
                                    CopyProperties(teleporter, etp);
                                    etp.Running = false;
                                    etp.Visible = false;
                                }
                                else if (item.GetType() == typeof(KeywordTeleporter))
                                {
                                    KeywordTeleporter teleporter;
                                    teleporter = (KeywordTeleporter)item;
                                    EventKeywordTeleporter etp = new EventKeywordTeleporter();
                                    CopyProperties(teleporter, etp);
                                    etp.Running = false;
                                    etp.Visible = false;
                                }
                                else
                                    ErrorOut(bits, "Logic Error: Unknown teleporter type {0}.", ConsoleColor.Red, item);

                                item.Delete();
                                patched++;
                            }
                        }
                        Unmute();

                        EchoOut(string.Format("{0} teleporters replaced.", patched), ConsoleColor.Magenta);
                    }
                    else
                        ErrorOut(bits, string.Format("Error: unable to locate {0}.", pathName), ConsoleColor.Red);
                }
                finally
                {
                    Unmute();
                }
                return patched;
            }
            return null;
        }
        #endregion PatchV42
        #region PatchV43
        private static int? PatchV43(int patchid)
        {  // wipe all accounts, configure Allowed IP Addresses, characters per acct, Zora, Coops, etc.
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV43;
            if (StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to initialize a new shard...", ConsoleColor.Magenta);
                Mute();
                try
                {
                    // ====================
                    int count = 0;

                    // delete accounts 
                    count = OwnerTools.DeleteAccounts();
                    EchoOut(string.Format("{0} Accounts deleted.", count), ConsoleColor.Magenta);
                    patched += count;

                    // delete contents of all containers
                    count = OwnerTools.EmptyContainers();
                    EchoOut(string.Format("{0} Items deleted.", count), ConsoleColor.Magenta);
                    patched += count;

                    // ====================
                    // configure item management for standard shards
                    {
                        CoreManagementConsole cmc = new CoreManagementConsole();
                        GameSystemsConsole gsc = new GameSystemsConsole();
                        cmc.Cooperatives = true; cmc.Cooperatives = false; patched++;
                        gsc.MagicCraftSystem = true; gsc.MagicCraftSystem = false; patched++;
                        gsc.MagicGearDropChance = 50; patched++;
                        gsc.MagicGearDropDowngrade = 0; patched++;
                        gsc.MagicGearThrottleEnabled = false; patched++;
                        gsc.SlayerInstrumentDropRate = 0.05; patched++;
                        gsc.SlayerWeaponDropRate = 0.05; patched++;
                        gsc.ZoraSystem = true; gsc.ZoraSystem = false; patched++;
                        cmc.Delete();
                        gsc.Delete();
                    }

                    // Bulk Orders
                    {
                        if (Core.RuleSets.BulkOrderSystemRules())
                        {   // turn on
                            BulkOrderSystem.EnabledFlags = BulkOrderFlags.Smith; // tailor hasn't been tested (?)
                            CoreAI.SetDynamicFeature(CoreAI.FeatureBits.BulkOrdersEnabled);
                        }
                        else
                        {   // turn off
                            BulkOrderSystem.EnabledFlags = BulkOrderFlags.None;
                            CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.BulkOrdersEnabled);
                        }
                    }

                    // skill Gain
                    {
                        if (SiegeStyleShards(quiet: true))
                        {
                            // Skill on Siege will be Return Over Time (ROT)
                            SkillGainSystem.GainWaits = true; patched++;
                        }
                        else
                        {
                            // other shards skills are difficulty based.
                            SkillGainSystem.GainWaits = false; patched++;
                        }
                    }

                    // configure the server for all shards
                    {
                        CoreManagementConsole cmc = new CoreManagementConsole();
                        if (Siege(quiet: true))
                        { CoreAI.MaxAccountsPerIP = 1; patched++; }
                        else
                        { CoreAI.MaxAccountsPerIP = 3; patched++; }
                        CoreAI.MaxConcurrentAddresses = 1; patched++;
                        cmc.NewPlayerGuild = true; patched++;
                        // For Mortalis, we may want to have the exit from the NPSZ randomized.
                        cmc.NewPlayerStartingArea = true; patched++;
                        cmc.Delete();
                    }

                    // remove all guilds
                    {
                        List<Guild> glist = new();
                        foreach (Guild g in BaseGuild.List.Values)
                            glist.Add(g);

                        foreach (Guild g in glist)
                        {
                            patched++;
                            g.Disband();
                        }
                        foreach (Item item in Server.World.Items.Values)
                        {
                            if (item is Guildstone guildstone && !guildstone.Deleted)
                            {
                                patched++;
                                guildstone.Delete();
                            }
                        }
                    }

                    // Reprice all the static houses by simply lifting the item and placing it back in the vendor's backpack.
                    // (The vendor already 1. has auto pricing of deeds, and 2. already takes into account Siege Pricing Rules.
                    {
                        foreach (Mobile playerVendor in Server.World.Mobiles.Values)
                        {
                            if (playerVendor is PlayerVendor && playerVendor.Backpack != null)
                            {
                                if (playerVendor.IsStaffOwned && playerVendor.Backpack.Items != null)
                                {
                                    List<Item> list = new();
                                    List<Item> itemList = new(playerVendor.Backpack.Items);
                                    foreach (Item item in itemList)
                                    {
                                        if (item is HouseDeed hd)
                                        {
                                            list.Add(hd);
                                            playerVendor.Backpack.RemoveItem(hd);
                                        }
                                    }
                                    foreach (Item item in list)
                                    {
                                        playerVendor.Backpack.AddItem(item);
                                        patched++;
                                    }
                                }
                            }
                        }
                    }

                    // Initialize the ResourcePool
                    {
                        string saves = Path.Combine(Core.ExeDirectory, "Saves");
                        string resourcePool = Path.Combine(Core.ExeDirectory, "Saves", "ResourcePool");
                        string transactionHistories = Path.Combine(Core.ExeDirectory, "Saves", "TransactionHistories");
                        if (Directory.Exists(resourcePool))
                        {
                            DirectoryInfo di = new DirectoryInfo(resourcePool);
                            RecursiveDelete(di);
                        }
                        if (!Directory.Exists(resourcePool))
                            Directory.CreateDirectory(resourcePool);
                        if (!Directory.Exists(transactionHistories))
                            Directory.CreateDirectory(transactionHistories);
                        if (ResourcePool.Consignments != null)
                        {
                            int rpc = ResourcePool.Consignments.Count;
                            EchoOut(string.Format("{0} resource pool consignments detected.", rpc), ConsoleColor.Magenta);
                            ResourcePool.Consignments.Clear();
                            EchoOut(string.Format("{0} resource pool consignments deleted.", rpc), ConsoleColor.Magenta);
                        }
                        else
                            EchoOut(string.Format("No resource pool consignments to deleted."), ConsoleColor.Magenta);
                    }
                }
                finally
                {
                    // Done
                    Unmute();
                }
                EchoOut(string.Format("{0} shard initialization steps complete.", patched), ConsoleColor.Magenta);
                return patched;
            }
            else if (AngelIsland())
            {   // nothing to do here - AI was set up long ago
                LogPatch(bits);
                EchoOut(string.Format("{0} shard initialization steps complete.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV43
        #region PatchV44
        private static int? PatchV44(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV44;
            if (AllShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time patch to fix up map elements...", ConsoleColor.Magenta);

                // record Items before the patch
                List<Item> before = Before();

                // apply some patches.
                #region Buc's den (All shards)
                {   // Buc's den, missing floor tile
                    Static fix = new Static(0x53E);
                    fix.MoveToWorld(new Point3D(2647, 2195, -20), Map.Felucca); patched++;
                }
                #endregion Buc's den (All shards)
                #region new player starting area (All shards)
                {   // remove random marble slab from new player starting area
                    Item slab = Server.World.FindItem(0x40052C36);
                    if (slab != null && slab.Deleted == false && slab.Location == new Point3D(5735, 975, 3) && slab.Map == Map.Trammel)
                    {
                        slab.Delete(); patched++;
                    }
                    else
                        EchoOut(
                            string.Format("Warning: Did not find slab where expected {0}. ", new Point3D(5735, 975, 3)),
                            ConsoleColor.Yellow);
                }
                #endregion new player starting area (All shards)
                #region brit sewer => new orc fort (Standard Shards only)
                if (StandardShards())
                {
                    EchoOut("\tTunnels from brit sewers to a new orc fort...", ConsoleColor.Magenta);
                    Map map = Map.Felucca;

                    // fix teleporter and sign to tunnel in brit sewer
                    {
                        // first for the teleporter at (6140, 1431, 4)
                        Item tele1 = FindOneItemAt(new Point3D(6140, 1431, 4), Map.Felucca, typeof(Teleporter));
                        if (tele1 is Teleporter teleporter1)
                        {   // adjust position
                            teleporter1.MoveToWorld(new Point3D(6140, 1430, 4), tele1.Map);
                            patched++;
                            // add a second teleporter
                            Item tele2 = Dupe(teleporter1);
                            if (tele2 is Teleporter teleporter2)
                            {
                                teleporter1.MoveToWorld(new Point3D(6141, 1430, 4), tele1.Map);
                                patched++;
                            }
                        }
                        else
                            ErrorOut(bits, string.Format("Logic Error: Did not find teleporter where expected {0}. ", new Point3D(6140, 1431, 4)), ConsoleColor.Red);

                        // now for the teleporter signage at (6142, 1430, 5)
                        Item sign1 = Server.World.FindItem(0x40004AC3);
                        if (sign1 != null)
                        {
                            sign1.ItemID = 0x1F29;
                            sign1.Name = "Caution: Orcs!";
                            sign1.MoveToWorld(new Point3D(6143, 1431, 26), map);
                            patched++;
                            Item post1 = new Static(0x09);
                            post1.MoveToWorld(new Point3D(6142, 1430, 4), map);
                            patched++;
                        }
                        else
                            ErrorOut(bits, string.Format("Logic Error: Did not find sign where expected {0}. ", 0x40004AC3), ConsoleColor.Red);

                        // tunnel entrance from orc area at 2108 1228 0 Felucca
                        {
                            tele1 = FindOneItemAt(new Point3D(2108, 1228, 0), Map.Felucca, typeof(Teleporter));
                            if (tele1 is Teleporter teleporter)
                            {   // set the PointDest to just inside the cave
                                teleporter.PointDest = new Point3D(6126, 1411, 15);
                                patched++;
                                // okay, now for the cave exit teleporters to this orc camp
                                Item tele2 = FindOneItemAt(new Point3D(6125, 1411, 15), Map.Felucca, typeof(Teleporter));
                                Item tele3 = FindOneItemAt(new Point3D(6126, 1411, 15), Map.Felucca, typeof(Teleporter));
                                Item tele4 = FindOneItemAt(new Point3D(6127, 1411, 15), Map.Felucca, typeof(Teleporter));
                                if (tele2 is Teleporter teleporter2 && tele3 is Teleporter teleporter3 && tele4 is Teleporter teleporter4)
                                {
                                    teleporter2.PointDest = teleporter3.PointDest = teleporter4.PointDest = new Point3D(2108, 1228, 7); // (2108, 1228, 0)
                                    patched++;
                                }
                                else
                                    ErrorOut(bits,
                                        string.Format("Logic Error: Did not find teleporter where expected {0}. ", new Point3D(6125, 1411, 15)),
                                        ConsoleColor.Red);

                            }
                            else
                                ErrorOut(bits,
                                    string.Format("Logic Error: Did not find teleporter where expected {0}. ", new Point3D(2108, 1228, 0)),
                                    ConsoleColor.Red);
                        }

                        // Convert this rect (2101, 1226)+(4, 17) to static (from someone's old Township deco)
                        //  Looks good for an Orc Camp
                        {
                            MakeStatic(new Rectangle2D(2101, 1226, 4, 17), Map.Felucca);
                            patched++;
                        }

                        // add an orc fort
                        {
                            object o = Activator.CreateInstance(typeof(BludchokAddon));
                            (o as Item).MoveToWorld(new Point3D(2069, 1243, 0), Map.Felucca);
                            patched++;

                            // add some doors to the orc fort
                            //  just steal the door info from the other fort
                            Item door1 = Server.World.FindItem(0x40016C8D);
                            Item door2 = Server.World.FindItem(0x40016C25);
                            Item door3 = Server.World.FindItem(0x400167F9);
                            Item door4 = Server.World.FindItem(0x40015CC1);
                            Item door5 = Server.World.FindItem(0x40013581);
                            Item door6 = Server.World.FindItem(0x40013589);
                            Item door7 = Server.World.FindItem(0x4001424A);
                            Item door8 = Server.World.FindItem(0x40012578);
                            Item door9 = Server.World.FindItem(0x400124EB);
                            if (door1 is BaseDoor baseDoor1 && door2 is BaseDoor baseDoor2 &&
                                door3 is BaseDoor baseDoor3 && door4 is BaseDoor baseDoor4 &&
                                door5 is BaseDoor baseDoor5 && door6 is BaseDoor baseDoor6 &&
                                door7 is BaseDoor baseDoor7 && door8 is BaseDoor baseDoor8 &&
                                door9 is BaseDoor baseDoor9)
                            {
                                Item newDoor1 = Dupe(door1);
                                Item newDoor2 = Dupe(door2);
                                Item newDoor3 = Dupe(door3);
                                Item newDoor4 = Dupe(door4);
                                Item newDoor5 = Dupe(door5);
                                Item newDoor6 = Dupe(door6);
                                Item newDoor7 = Dupe(door7);
                                Item newDoor8 = Dupe(door8);
                                Item newDoor9 = Dupe(door9);

                                newDoor1.MoveToWorld(new Point3D(2062, 1239, 0), map); patched++;
                                newDoor2.MoveToWorld(new Point3D(2062, 1238, 0), map); patched++;
                                newDoor3.MoveToWorld(new Point3D(2066, 1235, 5), map); patched++;
                                newDoor4.MoveToWorld(new Point3D(2068, 1231, 5), map); patched++;
                                newDoor5.MoveToWorld(new Point3D(2065, 1251, 5), map); patched++;
                                newDoor6.MoveToWorld(new Point3D(2065, 1250, 5), map); patched++;
                                newDoor7.MoveToWorld(new Point3D(2056, 1251, 5), map); patched++;
                                newDoor8.MoveToWorld(new Point3D(2077, 1234, 0), map); patched++;
                                newDoor9.MoveToWorld(new Point3D(2078, 1234, 0), map); patched++;
                            }
                        }

                        // add an orc camp outside fort
                        {
                            object o = Activator.CreateInstance(typeof(Spawner));
                            (o as Item).MoveToWorld(new Point3D(2094, 1234, 0), map);
                            Spawner spawner1 = o as Spawner;
                            spawner1.ObjectNamesRaw.Add("orccamp");
                            patched++;
                        }
                    }
                }
                #endregion brit sewer => new orc fort (Standard Shards only)
                #region Moongates (All shards)
                if (AllShards() && !LoginServer(quiet: true))
                {
                    EchoOut("\tFix floating moongate in Brit's Castle, and remove one from Oc'Nivelle...", ConsoleColor.Magenta);
                    // floating moongate in brit's castle
                    Point3D px = new Point3D(1322, 1622, 71);
                    Item item = FindOneItemAt(px, Map.Felucca, typeof(EventSungate));
                    if (item != null)
                        item.Location = new Point3D(1322, 1622, 55);
                    else
                        ErrorOut(bits,
                            string.Format("Logic Error: Found too many sungates at {0}. ", px),
                            ConsoleColor.Red);

                    // floating moongate in Oc'Nivelle arena
                    px = new Point3D(815, 1052, 21);
                    item = FindOneItemAt(px, Map.Felucca, typeof(EventSungate));
                    if (item != null)
                        // we'll delete is since there is already a teleporter at that location with sparkles
                        item.Delete();
                    else
                        ErrorOut(bits,
                            string.Format("Logic Error: Found too many sungates at {0}. ", px),
                            ConsoleColor.Red);
                }
                #endregion Moongates (All shards)
                #region Moongates (standard shards only)
                // exit from Solen tunnels 
                //  we didn't use these tunnels on AI, but we will on the standard shards
                if (StandardShards())
                {
                    EchoOut("\tRemove solen moongate and sign (\"tunnels not in use\")...", ConsoleColor.Magenta);

                    // wipe rect instead of trying to locate the three different components (5726, 1852)+(6, 3)
                    Rectangle2D rect = Rectangle2D.Parse("(5726, 1852)+(6, 3)");
                    if (Utility.CountItems(Map.Felucca, rect) == 3)
                        WipeRect(Map.Felucca, rect, flags: WipeRectFlags.Items);
                    else
                        ErrorOut(bits,
                                string.Format("Logic Error: Did not find solen items where expected at {0}. ", rect),
                                ConsoleColor.Red);
                }
                #endregion Moongates (Standard Shards only)
                #region Teleporters (All shards)
                if (AllShards() && !LoginServer(quiet: true))
                {
                    EchoOut("\tfix the teleporters that lead from Vesper Inn to/from brit (and chicken arena)...", ConsoleColor.Magenta);
                    // first, the gate that leads from the tunnel, to the next tunnel TO brit
                    {
                        Point3D px = new Point3D(5686, 385, 2);
                        Item item = FindOneItemAt(px, Map.Felucca, typeof(Teleporter));
                        if (item != null)
                        {   // runUO placed this one wrong (2 instead of 3). Probably a difference in maps
                            int lowZ = 0, avgZ = 0, topZ = 0;
                            Map.Felucca.GetAverageZ(px.X, px.Y, ref lowZ, ref avgZ, ref topZ);
                            item.Z = avgZ;
                            patched++;
                            // fix up the destination, should be just the other side of the cave
                            (item as Teleporter).PointDest = new Point3D(2776, 894, -23);
                        }
                        else
                            ErrorOut(bits,
                                string.Format("Logic Error: Did not find teleporter where expected {0}. ", px),
                                ConsoleColor.Red);
                    }

                    // Next, RunUO placed two additional teleporters here. Again, probably for map comparability.
                    {
                        Point3D px = new Point3D(5686, 386, 2);
                        Item item = FindOneItemAt(px, Map.Felucca, typeof(Teleporter));
                        if (item != null)
                        {
                            item.Delete();
                            patched++;
                        }
                        else
                            ErrorOut(bits,
                                string.Format("Logic Error: Did not find teleporter where expected {0}. ", px),
                                ConsoleColor.Red);


                        // And this one.
                        px = new Point3D(5686, 387, 2);
                        item = FindOneItemAt(px, Map.Felucca, typeof(Teleporter));
                        if (item != null)
                        {
                            item.Delete();
                            patched++;
                        }
                        else
                            ErrorOut(bits,
                                string.Format("Logic Error: Did not find teleporter where expected {0}. ", px),
                                ConsoleColor.Red);
                    }

                    // Now.. on the other side of this cave entrance/exit is a single teleporter, we will add another
                    //  to cover the entrance/exit
                    {
                        Point3D px = new Point3D(2776, 895, -23);
                        Item item = FindOneItemAt(px, Map.Felucca, typeof(Teleporter));
                        if (item != null)
                        {   // activate this teleporter
                            Teleporter teleporter1 = item as Teleporter;
                            teleporter1.PointDest = new Point3D(5686, 385, 3);
                            teleporter1.Running = true;
                            // let's add a second teleporter
                            Teleporter teleporter2 = new Teleporter();
                            CopyProperties(teleporter1, teleporter2);
                            teleporter2.Location = new Point3D(2776, 894, -23);
                            patched++;
                        }
                        else
                            ErrorOut(bits,
                                string.Format("Logic Error: Did not find teleporter where expected {0}. ", px),
                                ConsoleColor.Red);
                    }

                    EchoOut("\treplace the 'alchemical symbol' with a 'hole' on the lighthouse roof (escape).", ConsoleColor.Magenta);
                    {
                        Point3D px = new Point3D(5672, 2406, 79);
                        Item item = FindOneItemAt(px, Map.Felucca, 0x1822);
                        if (item != null)
                        {
                            // 'alchemical symbol' (0x1822) to 'hole' (0x11C5)
                            item.ItemID = 0x11C5;
                            patched++;
                        }
                        else
                            ErrorOut(bits,
                                    string.Format("Logic Error: Did not find 'alchemical symbol' at {0}. ", px), ConsoleColor.Red);
                    }
                    #region EventTeleporters (All shards)
                    // patch staff made sparkles into the spawner
                    if (AllShards())
                    {
                        EchoOut("\tpatch staff made sparkles into the spawner...", ConsoleColor.Magenta);
                        foreach (Item item in Server.World.Items.Values)
                        {
                            if (item != null && item is EventTeleporter etp && !etp.Deleted)
                            {
                                // copy the sparkles if they exist (some items have more than one)
                                //  we will prefer the special sparkle '0x375A'
                                List<Item> list = FindItemAt(etp.Location, etp.Map, 0x375A);
                                list.AddRange(FindItemAt(etp.Location, etp.Map, 0x373A));
                                if (list.Count > 0)
                                {   // there be sparkles here
                                    for (int ix = 0; ix < list.Count; ix++)
                                    {
                                        if (ix == 0)
                                        {   // just get the first sparkle, delete the rest
                                            etp.SparkleItem = list[0];
                                            etp.SparkleItem.MoveToWorld(etp.Location, etp.Map);
                                            etp.SparkleItem.Visible = etp.Running;
                                            etp.SparkleHue = list[0].Hue;
                                            patched++;
                                        }
                                        else
                                        {
                                            list[ix].Delete();
                                            patched++;
                                        }
                                    }
                                }
                                else
                                {   // No sparkles here, lets add some
                                    // lets add a sparkle if it's not over a "crumbling floor"
                                    // Note: this "crumbling floor" started out at Z=-1, later it will
                                    //  be moved to Z=0
                                    Item tmp = FindOneItemAt(etp.Location, etp.Map, 0x11C5, lenientZ: 3);
                                    if (tmp == null)
                                    {
                                        etp.SparkleItem = new Item(0x373A);
                                        etp.SparkleItem.MoveToWorld(etp.Location, etp.Map);
                                        etp.SparkleItem.Visible = etp.Running;
                                    }
                                }
                            }
                        }
                    }
                    #endregion EventTeleporters (All shards)
                }
                #endregion Teleporters
                #region Chicken arena (Multi shard)
                if (AllShards() && !LoginServer(quiet: true))
                {
                    EchoOut("\twipe the area in the tunnel of all staff added deco (chicken arena tunnel)...", ConsoleColor.Magenta);
                    // wipe the area in the tunnel of all staff added deco.
                    //  for AllShards, this has already happened, but no need to special case that here
                    {
                        Rectangle2D rect = Rectangle2D.Parse("(5657, 421)+(12, 10)");
                        WipeRect(Map.Felucca, rect, flags: WipeRectFlags.Items);
                    }

                    // patch the tele from Vesper inn into the tunnel
                    {
                        Point3D px = new Point3D(2782, 981, 2);
                        Item item = FindOneItemAt(px, Map.Felucca, typeof(EventTeleporter));
                        if (item != null)
                        {
                            (item as EventTeleporter).PointDest = new Point3D(5659, 424, -3);
                            patched++;
                        }
                        else
                        {
                            ErrorOut(bits,
                                string.Format("Logic Error: Did not find teleporter where expected. "), ConsoleColor.Red);
                            return 0;
                        }

                        // patch the tele and hole's Z from Vesper inn to the tunnel
                        // first the tele
                        int lowZ = 0, avgZ = 0, topZ = 0;
                        Map.Felucca.GetAverageZ(px.X, px.Y, ref lowZ, ref avgZ, ref topZ);
                        item.Z = avgZ;

                        // now for the hole (0x11C5)
                        px.Z = -1;
                        Item hole = FindOneItemAt(px, Map.Felucca, 0x11C5);
                        if (hole != null)
                        {
                            hole.Z = avgZ;
                            patched++;
                        }
                        else
                            ErrorOut(bits,
                                string.Format("Logic Error: Did not find 'hole' where expected. "), ConsoleColor.Red);
                    }

                    // add the tele back to Vesper Inn from the tunnel
                    {
                        Mute();
                        try
                        {
                            EventTeleporter etp = new();
                            etp.Map = Map.Felucca;
                            etp.Location = new Point3D(5658, 423, 8);
                            etp.PointDest = new Point3D(2782, 981, 0);
                            etp.MapDest = Map.Felucca;
                            patched++;
                        }
                        finally
                        {
                            Unmute();
                        }

                    }

                    // add the 'hole' that drops you to the arena
                    {
                        Static hole = new Static(0x11C5);
                        hole.MoveToWorld(new Point3D(5660, 422, -2), Map.Felucca);
                        patched++;
                        // and now the teleporter
                        Mute();
                        try
                        {
                            EventTeleporter etp = new();
                            etp.Map = Map.Felucca;
                            etp.Location = new Point3D(5660, 422, -2);
                            etp.PointDest = new Point3D(1674, 966, 78);
                            etp.MapDest = Map.Ilshenar;
                            patched++;
                        }
                        finally
                        {
                            Unmute();
                        }
                    }
                    // update the sign info
                    {
                        Item sign = Server.World.FindItem(0x4000A5B2);
                        if (sign != null)
                        {
                            sign.Name = "There is no Chicken Arena down the hole. *winks*";
                            patched++;
                        }
                        else
                            ErrorOut(bits,
                                string.Format("Logic Error: Did not find sign where expected {0}. ", new Point3D(2782, 978, 0)),
                                ConsoleColor.Red);
                    }
                }
                #endregion Chicken arena (Multi shard)
                #region funky 'room' in Wrong with a teleporter to Shame
                if (StandardShards())
                {   // (5686, 573)+(11, 15)
                    EchoOut("\twipe funky 'room' in Wrong with a teleporter to Shame...", ConsoleColor.Magenta);
                    //  We could get away with a simple wipe, but just in case RunUO has/will put something there.
                    Utility.RefreshDeco(Map.Felucca, new List<Rectangle2D>() { new Rectangle2D(5686, 573, 11, 15) },
                        startingZ: 0, lenientZ: int.MinValue);
                }
                #endregion funky 'room' in Wrong with a teleporter to Shame
                #region replace missing dungeon wall in Deceit
                if (StandardShards())
                {   // (5259, 671, 0)
                    EchoOut("\tReplace missing dungeon wall in Deceit...", ConsoleColor.Magenta);
                    Static wall = new Static(0x243); // dungeon wall
                    wall.MoveToWorld(new Point3D(5259, 671, 0), Map.Felucca);
                }
                #endregion replace missing dungeon wall in Deceit
                #region Platform cleanup in Wrong
                if (StandardShards())
                {   // (5643, 575)+(5, 4)
                    EchoOut("\tPlatform cleanup in Wrong...", ConsoleColor.Magenta);
                    //  We could get away with a simple wipe, but just in case RunUO has/will put something there.
                    Utility.RefreshDeco(Map.Felucca, new List<Rectangle2D>() { new Rectangle2D(5643, 575, 5, 4) },
                        startingZ: Utility.GetAverageZ(Map.Felucca, 5643, 575), lenientZ: int.MinValue);
                }
                #endregion Platform cleanup in Wrong
                #region Wind memories
                // there appears to be a but in either RunUO, or the client
                // if you add this static, it changes 'direction' on the next server restart.
                // I can reproduce this in RunUO 2.6
                #endregion Wind memories
                List<Item> after = After();
                patched = Delta(before, after).Count;

                EchoOut(string.Format("{0} map tiles updated", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV44
        #region PatchV45
        private static int? PatchV45(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV45;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to cleanup redundant public moongates.", ConsoleColor.Magenta);

                var before = Before();

                PublicMoongate.Generate(PMList.Felucca);

                var after = After();

                patched = Delta(before, after).Count;


                EchoOut(string.Format("{0} Redundant public moongates deleted.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV45
        #region PatchV46
        private static int? PatchV46(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV46;
            if (AllShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time patch to convert 'blessed' mobiles to 'Exhibit' (Invulnerable+)...", ConsoleColor.Magenta);
                foreach (Item item in Server.World.Items.Values)
                    if (item is Spawner spawner)
                        foreach (object o in spawner.Objects)
                        {
                            if (o is Mobile m)
                            {
                                if (m.Blessed)
                                {
                                    spawner.Exhibit = true;
                                    if (spawner.Running)
                                        spawner.ScheduleRespawn = true;
                                    patched++;

                                    // now for the template
                                    if (spawner.TemplateMobile != null && !spawner.TemplateMobile.Deleted)
                                    {
                                        if (spawner.TemplateMobile is BaseCreature bc)
                                        {
                                            Spawner.MakeExhibit(bc);
                                            patched++;
                                        }
                                    }
                                }
                            }
                        }

                if (patched > 0)
                {

                    EchoOut(string.Format("{0} 'blessed' mobiles converted to 'exhibit'.", patched), ConsoleColor.Magenta);
                }
                else
                    ErrorOut(bits, "Did not find 'blessed' mobiles as expected.", ConsoleColor.Red);
                return patched;
            }
            return null;
        }
        #endregion PatchV46
        #region PatchV47
        private static int? PatchV47(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV47;
            if (AllShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time patch to remove blessed creatures...", ConsoleColor.Magenta);

                // look for blessed creatures not on a spawner - there should be zero
                LogHelper logger = new LogHelper("OrphanedBlessedDeleted.log", true, true, true);
                foreach (Mobile m in Server.World.Mobiles.Values)
                {
                    if (m == null || m.Deleted)
                        continue;

                    if (m is PlayerMobile)
                        continue;

                    if (m.Blessed && !IsSpawnerProduct(m))
                    {
                        logger.Log(LogType.Mobile, m);
                        m.Delete();
                        patched++;
                    }
                }
                logger.Finish();
                if (patched > 0)
                {

                    EchoOut(string.Format("{0} 'blessed' mobiles removed.", patched), ConsoleColor.Magenta);
                }
                else
                    ErrorOut(bits, "Did not find 'blessed' mobiles as expected.", ConsoleColor.Red);
                return patched;
            }
            return null;
        }

        #endregion PatchV47
        #region PatchV48
        private static int? PatchV48(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV48;
            if (AllShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time patch to enable special spawners...", ConsoleColor.Magenta);


                List<Item> list = new();
                /*LordGuardian */
                list.Add(FindOneItemAt(new Point3D(1521, 1442, 15), Map.Felucca, typeof(Spawner)));
                /* LadyGuardian */
                list.Add(FindOneItemAt(new Point3D(1521, 1443, 15), Map.Felucca, typeof(Spawner)));
                /* Smaghed */
                list.Add(FindOneItemAt(new Point3D(1521, 1446, 15), Map.Felucca, typeof(Spawner)));
                /* CrapFactory */
                list.Add(FindOneItemAt(new Point3D(1521, 1448, 15), Map.Felucca, typeof(Spawner)));
                /* Mashiara */
                list.Add(FindOneItemAt(new Point3D(1505, 1441, 15), Map.Felucca, typeof(Spawner)));

                foreach (Item item in list)
                    if (item is Spawner spawner && !spawner.Deleted)
                    {
                        spawner.Running = true;
                        // reclassify here as 'all shards'
                        UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false,
                            uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);
                        patched++;
                    }

                // move Mashiara
                if (list.Count >= 5)
                    if (list[4] is Spawner spawner)
                    {
                        spawner.Location = new Point3D(1526, 1442, 15);
                        spawner.MobileDirection = Direction.West;
                        patched++;
                    }
                // test center doesn't have all these spawners
                if (patched == 6 || Core.GMNTC_CFG)
                {
                    EchoOut(string.Format("{0} spawners enabled.", patched), ConsoleColor.Magenta);
                }
                else
                    ErrorOut(bits, "Did not find the one or more spawners as expected.", ConsoleColor.Red);
                return patched;
            }
            return null;
        }
        #endregion PatchV48
        #region PatchV49
        private static int? PatchV49(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV49;
            if (AngelIsland())
            {
                LogPatch(bits);
                EchoOut("One-time patch of 'bowyer' spawners. Patching...", ConsoleColor.Magenta);
                foreach (Item item in Server.World.Items.Values)
                {
                    if (item is EventSpawner) continue;
                    if (item.Map != Map.Felucca) continue;

                    if (item is Spawner spawner)
                        if (spawner.Spawns("bower"))
                        {
                            patched++;
                            for (int ix = 0; ix < spawner.ObjectNamesRaw.Count; ix++)
                                if ((spawner.ObjectNamesRaw[ix] as string).ToLower().Contains("bower"))
                                    spawner.ObjectNamesRaw[ix] = new string("bowyer");
                        }
                }

                EchoOut(string.Format("{0} 'bowyer' spawners patched.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV49
        #region PatchV50
        private static int? PatchV50(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV50;
            if (AllShards())
            {   // special 'template' 
                LogPatch(bits);
                EchoOut("One-time patch to set template BaseVendor to invulnerable...", ConsoleColor.Magenta);

                foreach (Item item in Server.World.Items.Values)
                {
                    if (item == null || item.Deleted)
                        continue;

                    if (item is Spawner spawner)
                    {
                        if (spawner.TemplateMobile is BaseVendor vendor)
                        {
                            if (vendor.IsInvulnerable == false)
                            {   // make clear our intentions
                                spawner.Exhibit = true;         // anything this spawner spawns will be an Exhibit
                                vendor.IsInvulnerable = true;   // update the template mobile to also be Invulnerable
                                patched++;
                            }
                        }
                    }
                }

                EchoOut(string.Format("{0} template vendors patched.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV50
        #region PatchV51
        private static int? PatchV51(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV51;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to turn on vendor daily fees...", ConsoleColor.Magenta);

                // restore original fees. 
                // We had set this to zero causing ill behavior when calculating days the vendor can work for you (divide by zero)
                CoreAI.BaseVendorFee = 20;
                patched++;

                EchoOut(string.Format("{0} vendor daily fees restored.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV51
        #region PatchV52
        private static int? PatchV52(int patchid)
        {
            if (AllShards())
            {
                EchoOut("One-time patch for 'other' spawner categorization. Patching...", ConsoleColor.Magenta);
                int patched = 0;

                // build spawner list
                List<Spawner> spawnerList = new();
                foreach (Item item in World.Items.Values)
                {
                    if (item is Spawner spawner && !spawner.Deleted)
                    {   // all the spawners missed on the first patch
                        if (spawner.Distro == SpawnerModeAttribs.None)
                        {
                            if (item is EventSpawner)
                                spawnerList.Add(spawner);
                            else if (item.Map != Map.Felucca)
                                spawnerList.Add(spawner);
                        }
                    }
                }

                foreach (Spawner spawner in spawnerList)
                {
                    patched++;
                    spawner.Distro = SpawnerModeAttribs.ModeLegacy;
                    UpdateSpawnerDesignationaAndLog(
                        spawner,
                        coreSpawn: false,
                        uOSpawnMap: "AISpecial",
                        shardConfig: ShardConfig.AllShards);
                }
                EchoOut(string.Format("{0} 'other' spawners categorized.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV52
        #region PatchV53
        private static int? PatchV53(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV53;
            if (AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to remove T2A spawner from 'chicken fight' tunnel...", ConsoleColor.Magenta);

                List<Item> items = FindItemsAt(new Point3D(5658, 423, 8), Map.Felucca, lenientZ: 1);
                // delete the T2A teleporter and stalagmite
                foreach (Item item in items)
                    if (item.GetType() == typeof(EventTeleporter))
                        continue;
                    else
                    {
                        patched++;
                        item.Delete();
                    }

                EchoOut(string.Format("{0} items removed.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV53
        #region PatchV54
        private static int? PatchV54(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV54;
            if (StandardShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time repatch of Moonglow Zoo on all standard shards running...", ConsoleColor.Magenta);

                // first, wipe the area of mobiles (likely blessed from AI)
                Region reg = Region.FindByName("Britannia Royal Zoo", Map.Felucca);
                if (reg != null)
                {
                    foreach (Rectangle3D rect in reg.Coords)
                    {
                        Rectangle2D rect2D = new(rect.Start.X, rect.Start.Y, rect.Width, rect.Height);
                        patched += WipeRect(Map.Felucca, rect2D,
                            Utility.GetAverageZ(Map.Felucca, rect2D.Center.X, rect2D.Center.Y),
                            flags: WipeRectFlags.Mobiles);
                    }
                }
                else
                {
                    ErrorOut(bits, "Logic Error: Unable to find Moonglow zoo to wipe.", ConsoleColor.Red);
                    return 0;
                }

                // next, set all the spawners to 'Exhibit' creatures
                foreach (Rectangle3D rect in reg.Coords)
                {
                    IPooledEnumerable eable = Map.Felucca.GetItemsInBounds(new Rectangle2D(rect.Start.X, rect.Start.Y, rect.Width, rect.Height));

                    foreach (Item item in eable)
                    {
                        if (item is Spawner spawner && !spawner.Deleted)
                        {
                            foreach (object o in spawner.Objects)
                            {
                                if (o is BaseCreature bc)
                                {   // leave guards and escortables alone
                                    if (!bc.Body.IsHuman)
                                    {   // we don't mix monsters and humans at this location
                                        spawner.Exhibit = true;
                                        spawner.RemoveObjects();
                                        patched++;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    eable.Free();
                }

                // next lock all the doors and remove signs (standard shards only)
                foreach (Rectangle3D rect in reg.Coords)
                {
                    List<Item> list = new();
                    IPooledEnumerable eable = Map.Felucca.GetItemsInBounds(new Rectangle2D(rect.Start.X, rect.Start.Y, rect.Width, rect.Height));
                    foreach (Item item in eable)
                    {
                        if (item is LocalizedSign)
                            continue;
                        else if (item is CustomRegionControl)
                            continue;
                        else if (item is BaseDoor bd)
                        {
                            bd.Locked = true;
                            patched++;
                        }
                        // south facing sign, north facing sign
                        //  only delete for standard shards
                        else if (StandardShards(quiet: true) && item.ItemID == 0xBD1 || item.ItemID == 0xBD2)
                        {
                            list.Add(item);
                            patched++;
                        }

                    }
                    eable.Free();

                    // delete all the items
                    foreach (Item item in list)
                        item.Delete();
                }

                EchoOut(string.Format("{0} elements of Moonglow Zoo on all standard shards repatched.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV54
        #region PatchV55
        private static int? PatchV55(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV55;
            if (StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to remove the \"Sea Market\" spawners....", ConsoleColor.Magenta);

                // get "Sea Market" spawners
                List<string> mzLines = GetNerunSpawnerEntries();

                // delete spawners at "Sea Market"
                foreach (string line in mzLines)
                {
                    List<NerunRecord.Record> recordList = new();
                    List<string> compiledTokens = new();
                    string[] lineToks = line.Split(new char[] { '|' });                 // parse on '|'
                    CompileLine(lineToks, compiledTokens);                              // compile this line
                    NerunRecord.Parse(compiledTokens.ToArray(), recordList);            // parse into a record list
                    List<Spawner> allSpawnersAt = new List<Spawner>(AllModeNerunsSpawnersAt(recordList[0].X, recordList[0].Y, Map.Felucca));
                    foreach (Spawner spawner in allSpawnersAt)
                    {
                        spawner.RemoveObjects();
                        spawner.Delete();
                        patched++;
                    }
                }

                EchoOut(string.Format("{0} \"Sea Market\" spawners removed.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV55
        #region PatchV56
        private static int? PatchV56(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV56;
            // There were some complaints that the new Nerun respawn rates for camps was too slow. (Outdoors.map)
            //  I'll revert to faster respawn, but only for Angel Island
            if (AngelIsland())
            {
                LogPatch(bits);
                EchoOut("One-time patch for custom respawn rates for Angel Island \"Camps\". Patching...", ConsoleColor.Magenta);
                foreach (Item item in World.Items.Values)
                {
                    if (item is EventSpawner) continue;
                    if (item.Map != Map.Felucca) continue;

                    if (item is Spawner spawner)
                        if (spawner.Spawns("camp"))
                        {
                            patched++;
                            spawner.MinDelay = TimeSpan.FromMinutes(10);    // ignoring Nerun's Outdoors.map, custom for Angel Island
                            spawner.MaxDelay = TimeSpan.FromMinutes(20);
                        }
                }

                EchoOut(string.Format("{0} Angel Island Camps Patched.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV56
        #region PatchV57
        private static int? PatchV57(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV57;
            // The new map floor is a bit higher than the old when the teleporters were placed.
            // We need to raise the teles
            if (!LoginServer() && AllShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to adjust dungeon exit teleporters. Patching...", ConsoleColor.Magenta);
                List<Tuple<Point3D, Point3D>> list = new()
                {
                    new Tuple<Point3D, Point3D> (new Point3D(5457, 1864, 0), new Point3D(5457, 1864, 3)),

                    new Tuple<Point3D, Point3D> (new Point3D(5242, 1007, 0), new Point3D(5242, 1007, 4)),
                    new Tuple<Point3D, Point3D> (new Point3D(5243, 1007, 0), new Point3D(5243, 1007, 4)),
                    new Tuple<Point3D, Point3D> (new Point3D(5244, 1007, 0), new Point3D(5244, 1007, 4)),
                };

                foreach (var points in list)
                {
                    Item item = Utility.FindOneItemAt(points.Item1, Map.Felucca, typeof(Teleporter));
                    if (item is Teleporter tele)
                    {
                        tele.Location = points.Item2;    // adjust for map changes
                        patched++;
                    }
                    else
                        ErrorOut(bits, "Did not find teleporter at {0}.", ConsoleColor.Red, points.Item1);
                }

                // dupe teleporter at (1997, 81, 7)
                //  add dupes at (1997, 82, 6),and (1997, 80, 7)
                {
                    Item item = Utility.FindOneItemAt(new Point3D(1997, 81, 7), Map.Felucca, typeof(Teleporter));
                    if (item is Teleporter tele)
                    {
                        Item tele2 = Utility.Dupe(tele);
                        Item tele3 = Utility.Dupe(tele);
                        if (tele2 != null)
                        {
                            tele2.Location = new Point3D(1997, 82, 6);
                            patched++;
                        }
                        if (tele3 != null)
                        {
                            tele3.Location = new Point3D(1997, 80, 7);
                            patched++;
                        }
                    }
                    else
                        ErrorOut(bits, "Did not find teleporter at expected location .", ConsoleColor.Red);
                }


                // patch pointdest to (5878, 147, 25) for tele at (5203, 2327, 27)
                {
                    Item item = Utility.FindOneItemAt(new Point3D(5203, 2327, 27), Map.Felucca, typeof(Teleporter));
                    if (item is Teleporter tele)
                    {
                        tele.PointDest = new Point3D(5878, 147, 25);
                        patched++;
                    }
                }

                EchoOut(string.Format("{0} Dungeon exit teleporters adjusted.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV57
        #region PatchV58
        private static int? PatchV58(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV58;
            // The new map floor again / placement. Adjust.
            if (!LoginServer() && StandardShards())
            {
                LogPatch(bits);
                EchoOut("One-time patch to adjust dungeon exit teleporters II. Patching...", ConsoleColor.Magenta);
                List<Tuple<Point3D, Point3D>> list = new()
                {
                    new Tuple<Point3D, Point3D> (new Point3D(5556, 1826, -10), new Point3D(5556, 1827, -13)),
                    new Tuple<Point3D, Point3D> (new Point3D(5557, 1826, -10), new Point3D(5557, 1827, -13)),
                };

                foreach (var points in list)
                {
                    Item item = Utility.FindOneItemAt(points.Item1, Map.Felucca, typeof(Teleporter));
                    if (item is Teleporter tele)
                    {
                        tele.Location = points.Item2;    // adjust for map changes
                        patched++;
                    }
                    else
                        ErrorOut(bits, "Did not find teleporter at {0}.", ConsoleColor.Red, points.Item1);
                }

                // delete random moongate
                {
                    Item item = Utility.FindOneItemAt(new Point3D(5545, 189, 0), Map.Felucca, typeof(Moongate));
                    if (item is Moongate moongate)
                    {
                        patched++;
                        moongate.Delete();
                    }
                }

                // adjust tele dest
                {
                    Item item = Utility.FindOneItemAt(new Point3D(5538, 170, 5), Map.Felucca, typeof(Teleporter));
                    if (item is Teleporter tele)
                    {
                        patched++;
                        tele.PointDest = new Point3D(5513, 176, 5);
                    }
                    else
                        ErrorOut(bits, "Did not find teleporter at expected location .", ConsoleColor.Red);
                }

                // and the reverse
                {
                    Item item = Utility.FindOneItemAt(new Point3D(5513, 176, 5), Map.Felucca, typeof(Teleporter));
                    if (item is Teleporter tele)
                    {
                        patched++;
                        tele.PointDest = new Point3D(5538, 170, 5);
                    }
                    else
                        ErrorOut(bits, "Did not find teleporter at expected location .", ConsoleColor.Red);
                }

                EchoOut(string.Format("{0} Dungeon exit teleporters adjusted.", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV58
        #region PatchV59
        private static int? PatchV59(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV59;
            if (AllShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time patch to fix broken doors at Fire Temple and 1680 2986 20...", ConsoleColor.Magenta);
                BaseDoor door1 = (BaseDoor)FindOneItemAt(new Point3D(1680, 2986, 20), Map.Felucca, typeof(BaseDoor));
                BaseDoor door2 = (BaseDoor)FindOneItemAt(new Point3D(1680, 2985, 20), Map.Felucca, typeof(BaseDoor));
                if (door1 != null && door2 != null)
                {
                    door1.Facing = DoorFacing.SouthCW;
                    door2.Facing = DoorFacing.NorthCCW;
                    patched += 2;
                    EchoOut(string.Format("Doors fixed. {0} items updated", patched), ConsoleColor.Magenta);
                    if (StandardShards())
                    {   // wipe tiles from (4595, 3625, 30) - (4599, 3625, 30)
                        List<Item> items = new();
                        for (int X = 4595; X <= 4599; X++)
                            items.Add(FindOneItemAt(new Point3D(X, 3625, 30), Map.Felucca, typeof(Item)));
                        foreach (Item item in items)
                        {
                            item?.Delete();
                            patched++;
                        }
                    }
                    EchoOut(string.Format("Tiles removed. {0} items updated", patched - 2), ConsoleColor.Magenta);
                }
                else
                {
                    ErrorOut(bits, "Did not find door(s) at expected location.", ConsoleColor.Red);
                }
                return patched;
            }
            return null;
        }
        #endregion PatchV59
        #region PatchV60
        private static int? PatchV60(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV60;
            if (AllShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time patch to fix ill-configured champ spawns...", ConsoleColor.Magenta);

                // set them all off and clear the monsters
                ResetChampsWorker(bits, ref patched);

                // turn on autumn champ
                EchoOut(string.Format("Turning on Autumn Bob champ"), ConsoleColor.Magenta);
                ChampHelpers.ToggleChamp(ChampHelpers.Autumn_Bob, true);

                EchoOut(string.Format("{0} champ engines updated", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV60
        #region PatchV61
        private static int? PatchV61(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV61;
            if (AllShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time patch to add each account's current hardware hash to new hash list...", ConsoleColor.Magenta);

                foreach (Server.Accounting.Account current in Server.Accounting.Accounts.Table.Values)
                {
                    // they have no hash
                    if (current.HardwareHashRaw == 0)
                        continue;

                    // add this hash to the hash list
                    if (!current.Machines.Contains(current.HardwareHashRaw))
                    {
                        current.Machines.Add(current.HardwareHashRaw);
                        patched++;
                    }
                }

                EchoOut(string.Format("{0} accounts patched", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV61
        #region PatchV62
        private static int? PatchV62(int patchid)
        {
            int patched = 0;
            PatchIndex bits = PatchIndex.PatchV62;
            if (SiegeStyleShards() && !LoginServer(quiet: true))
            {
                LogPatch(bits);
                EchoOut("One-time patch to enable Patrol Guards...", ConsoleColor.Magenta);
                Spawner template = null;
                foreach (Item item in Server.World.Items.Values)
                {
                    if (item is Spawner spawner && spawner.Map == Map.Felucca && !spawner.Deleted)
                    {
                        if (IsTownRegion(spawner.Location, spawner.Map))
                            if (spawner.Spawns("PatrolGuard"))
                            {
                                // reclassify here as 'all shards'
                                UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false,
                                    uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                                if (template == null)
                                    template = spawner;

                                spawner.Running = true;
                                patched++;
                            }
                    }
                }
                #region Add two Patrol Guard spawners to Vesper bank
                // add two Patrol Guard spawners to Vesper (previously Kin spawners)
                Spawner s1 = new Spawner();
                Spawner s2 = new Spawner();
                Utility.CopyProperties(template, s1);
                Utility.CopyProperties(template, s2);
                s1.ObjectNamesRaw = new ArrayList(s1.ObjectNamesRaw);
                s2.ObjectNamesRaw = new ArrayList(s2.ObjectNamesRaw);
                s1.Location = new Point3D(2896, 677, 0);
                s2.Location = new Point3D(2896, 674, 0);
                UpdateSpawnerDesignationaAndLog(s1, coreSpawn: false, uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);
                UpdateSpawnerDesignationaAndLog(s2, coreSpawn: false, uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);
                // now drop a control console on top of the pile if there are more than one stacked spawners, and there isn't already
                //  a console there.
                if (NeedsConsole(s1.X, s1.Y, s1.Z, s1.Map) && !AlreadySpawned(s1.X, s1.Y, s1.Z, s1.Map))
                {
                    Server.Items.StackedSpawnerConsole stackedSpawnerConsole = new Server.Items.StackedSpawnerConsole();
                    stackedSpawnerConsole.Movable = false;
                    stackedSpawnerConsole.Visible = false;
                    stackedSpawnerConsole.MoveToWorld(new Point3D(s1.X, s1.Y, s1.Z + 1), s1.Map);
                }
                if (NeedsConsole(s2.X, s2.Y, s2.Z, s2.Map) && !AlreadySpawned(s2.X, s2.Y, s2.Z, s2.Map))
                {
                    Server.Items.StackedSpawnerConsole stackedSpawnerConsole = new Server.Items.StackedSpawnerConsole();
                    stackedSpawnerConsole.Movable = false;
                    stackedSpawnerConsole.Visible = false;
                    stackedSpawnerConsole.MoveToWorld(new Point3D(s2.X, s2.Y, s2.Z + 1), s2.Map);
                }
                patched += 2;
                #endregion  Add two Patrol Guard spawners to Vesper bank
                EchoOut(string.Format("{0} Patrol Guards enabled", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV62
        #region PatchV63
        private static int? PatchV63(int patchid)
        {
            if (AllShards())
            {
                EchoOut("PatchV63: running...", ConsoleColor.Yellow);
                int patched = 333;

                EchoOut(string.Format("{0} Patcher Table test complete", patched), ConsoleColor.Yellow);
                return patched;
            }
            return null;
        }
        #endregion PatchV63
        #region PatchV64
        private static int? PatchV64(int patchid)
        {
            if (AllShards())
            {
                EchoOut("Update region controllers V1 running...", ConsoleColor.Magenta);
                int patched = 0;

                patched = StaticRegionControl.FixControllers();

                EchoOut(string.Format("{0} Region controllers updated", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV64
        #region PatchV65
        private static int? PatchV65(int patchid)
        {
            PatchIndex bits = PatchIndex.PatchV65;
            if (AllShards())
            {
                EchoOut("Beta Ready V1 running...", ConsoleColor.Magenta);
                int patched = 0;
                #region Begin Implementation
                #region Champs
                if (Siege())
                {
                    Serial[] toCheck = new[] { ChampHelpers.Summer_Pirate, ChampHelpers.Autumn_Bob, ChampHelpers.Winter_FrozenHost, ChampHelpers.Spring_Vampire };
                    try
                    {
                        foreach (Serial serial in toCheck)
                        {
                            if (Server.World.FindItem(serial) as ChampEngine != null)
                            {   // turn off all seasonal champs
                                ChampHelpers.ToggleChamp(serial, false);
                                EchoOut(string.Format("{0} seasonal champ has been disabled.", ChampHelpers.ChampName(serial)), ConsoleColor.Magenta);
                                patched++;
                            }
                            else
                                ErrorOut(bits, "Error: seasonal champ {0} not found", ConsoleColor.Red, serial);
                        }
                    }
                    catch (Exception e)
                    {
                        LogHelper.LogException(e);
                        ErrorOut(bits, "Beta Ready V1: {0}", ConsoleColor.Red, e.Message);
                        ErrorOut(bits, "{0}", ConsoleColor.Red, e.StackTrace);
                    }
                }
                #endregion Champs
                if (AllShards())
                {
                    #region Fix Green Acres
                    //  The Green Acres rectangle has been wrong since forever, and it's still wrong in RunUO 2.6
                    //  It's one giant rectangle that includes at least Dungeon Wrong and maybe a couple others.
                    //  This patch breaks GA into sort of a backwards 'L' by creating two rectangles.
                    Region rx = Region.FindByName("Green Acres", Map.Felucca);
                    if (rx != null)
                    {
                        // old broken RunUO GA rect = <rect x="5376" y="512" width="767" height="767" />
                        bool broken = rx.Coords.Count == 1 && rx.Coords[0].Start == new Point3D(5376, 512, -128) && rx.Coords[0].End == new Point3D(6143, 1279, 128);
                        if (broken)
                        {   // (5887, 1023, 0) is the corner of the backwards 'L' shape where we will split out the rectangles.
                            Rectangle3D core = rx.Coords[0];
                            Rectangle3D unt1 = new Rectangle3D(new Point3D(core.Start.X, 1023, -128), new Point3D(core.End.X, core.End.Y, 128));
                            Rectangle3D unt2 = new Rectangle3D(new Point3D(core.Start.X + (5887 - core.Start.X), core.Start.Y, -128), new Point3D(core.End.X, 1023, 128));
                            rx.Coords = new List<Rectangle3D> { unt1, unt2 };
                            patched += 2;
                        }
                    }
                }
                #endregion Fix Green Acres
                #endregion End Implementation
                EchoOut(string.Format("{0} Beta Ready objects updated", patched), ConsoleColor.Magenta);
                return patched;
            }
            return null;
        }
        #endregion PatchV65
        #region PatchV66
        private static int? PatchV66(int patchid)
        {
            if (AllShards())
            {
                int patched = 0;
                #region Begin Implementation
                if (SiegeStyleShards())
                {
                    #region Yew Orc Fort
                    EchoOut("Patching Yew Orc Fort...", ConsoleColor.Magenta);
                    // add backdoor tele (inside fort) (628, 1478, 2)
                    Teleporter tp1 = new Teleporter();
                    tp1.Direction = Direction.North;
                    tp1.Directional = true;
                    tp1.Creatures = true;
                    tp1.Pets = true;
                    tp1.PointDest = new Point3D(628, 1476, 0);
                    tp1.MapDest = Map.Felucca;
                    tp1.MoveToWorld(new Point3D(628, 1478, 2), Map.Felucca);
                    tp1.Running = false;    // orcs now want it off
                    patched++;

                    // add backdoor tele (outside fort) (628, 1476, 0)
                    Teleporter tp2 = new Teleporter();
                    tp2.Direction = Direction.South;
                    tp2.Directional = true;
                    tp2.Creatures = true;
                    tp2.Pets = true;
                    tp2.PointDest = new Point3D(628, 1478, 2);
                    tp2.MapDest = Map.Felucca;
                    tp2.MoveToWorld(new Point3D(628, 1476, 0), Map.Felucca);
                    tp2.Running = false;    // orcs now want it off
                    patched++;

                    // wipe the tower (637, 1486, 0) x (645, 1494, 0)
                    //  plus some side objects, plus some roof tiles
                    Rectangle2D rx = new Rectangle2D(new Point2D(637, 1486), new Point2D(645, 1494));
                    rx.MakeHold(new Point2D(640, 1493));
                    rx.MakeHold(new Point2D(645, 1494));
                    // roof tiles                
                    rx.MakeHold(new Point2D(646, 1489));
                    rx.MakeHold(new Point2D(646, 1490));
                    rx.MakeHold(new Point2D(646, 1491));
                    rx.MakeHold(new Point2D(646, 1492));
                    rx.MakeHold(new Point2D(646, 1493));
                    patched += Utility.WipeRect(Map.Felucca, rx, 0, WipeRectFlags.All, new Type[] { typeof(Spawner) });

                    // wipe the outer east expansion (647, 1481, 0) x (679, 1509, 0)
                    patched += Utility.WipeRect(Map.Felucca, new Rectangle2D(new Point2D(647, 1481), new Point2D(679, 1509)), 0, WipeRectFlags.All, new Type[] { typeof(Spawner) });

                    // wipe some dangling palisades
                    Rectangle2D rect = new Rectangle2D(654, 1509, 658 - 654 + 1, 1509 - 1509 + 1);
                    patched += Utility.WipeRect(Map.Felucca, rect, 0, WipeRectFlags.All, new Type[] { typeof(Spawner) });

                    // spread out the orc fort spawn (too many inside fort)
                    // Old/new Values: 
                    //  HomeRange 10 => 60
                    //  Concentric false => true
                    Spawner spawner = (Spawner)FindOneItemAt(new Point3D(633, 1484, 0), Map.Felucca, typeof(Spawner));
                    if (spawner != null)
                    {
                        spawner.HomeRange = 60;
                        spawner.Concentric = true;
                        spawner.Respawn();
                        patched++;
                    }

                    // North-West corner
                    LOSBlocker losa = new LOSBlocker();
                    losa.MoveToWorld(new Point3D(622, 1477, 0), Map.Felucca); // (622, 1478, 20) // (622, 1477, 0)
                    patched++;

                    losa = new LOSBlocker();
                    losa.MoveToWorld(new Point3D(622, 1478, 0), Map.Felucca); // (622, 1479, 20) // (622, 1478, 0)
                    patched++;

                    losa = new LOSBlocker();
                    losa.MoveToWorld(new Point3D(623, 1477, 0), Map.Felucca); // (624, 1477, 20) // (623, 1477, 0)
                    patched++;

                    // South wall, west
                    Point2D px1 = new Point2D(622, 1496);
                    Point2D px2 = new Point2D(631, 1496);
                    Line line = new Line(px1, px2);
                    foreach (var node in line.GetPoints((int)Utility.GetDistanceToSqrt(px1, px2) + 1))
                    {
                        LOSBlocker losb = new LOSBlocker();
                        int z = Utility.GetAverageZ(Map.Felucca, node.X, node.Y);
                        losb.MoveToWorld(new Point3D(node, z), Map.Felucca);
                        patched++;
                    }

                    // Walkway West
                    px1 = new Point2D(631, 1495);
                    px2 = new Point2D(631, 1489);
                    line = new Line(px1, px2);
                    foreach (var node in line.GetPoints((int)Utility.GetDistanceToSqrt(px1, px2) + 1))
                    {
                        LOSBlocker losb = new LOSBlocker();
                        int z = Utility.GetAverageZ(Map.Felucca, node.X, node.Y);
                        losb.MoveToWorld(new Point3D(node, z), Map.Felucca);
                        patched++;
                    }

                    // Walkway East
                    //px1 = new Point2D(635, 1494);
                    px1 = new Point2D(635, 1495);
                    px2 = new Point2D(635, 1489);
                    line = new Line(px1, px2);
                    foreach (var node in line.GetPoints((int)Utility.GetDistanceToSqrt(px1, px2) + 1))
                    {
                        LOSBlocker losb = new LOSBlocker();
                        int z = Utility.GetAverageZ(Map.Felucca, node.X, node.Y);
                        losb.MoveToWorld(new Point3D(node, z), Map.Felucca);
                        patched++;
                    }

                    // South wall West
                    px1 = new Point2D(635, 1496);
                    px2 = new Point2D(646, 1496);
                    line = new Line(px1, px2);
                    foreach (var node in line.GetPoints((int)Utility.GetDistanceToSqrt(px1, px2) + 1))
                    {
                        LOSBlocker losb = new LOSBlocker();
                        int z = Utility.GetAverageZ(Map.Felucca, node.X, node.Y);
                        losb.MoveToWorld(new Point3D(node, z), Map.Felucca);
                        patched++;
                    }

                    // East wall, center
                    px1 = new Point2D(646, 1483);
                    px2 = new Point2D(646, 1488);
                    line = new Line(px1, px2);
                    foreach (var node in line.GetPoints((int)Utility.GetDistanceToSqrt(px1, px2) + 1))
                    {
                        LOSBlocker losb = new LOSBlocker();
                        int z = Utility.GetAverageZ(Map.Felucca, node.X, node.Y);
                        losb.MoveToWorld(new Point3D(node, z), Map.Felucca);
                        patched++;
                    }

                    {
                        // South wall West planks
                        // steps 1829 (0x725) at (625, 1495, 2) and (626, 1495, 8)
                        Static steps;
                        steps = new Static(0x725);
                        steps.MoveToWorld(new Point3D(625, 1495, 2), Map.Felucca);
                        patched++;

                        steps = new Static(0x725);
                        steps.MoveToWorld(new Point3D(626, 1495, 8), Map.Felucca);
                        patched++;

                        // planks location start (627, 1495, 13) => (630, 1495, 13) (running east-west)
                        px1 = new Point2D(627, 1495);
                        px2 = new Point2D(630, 1495);
                        line = new Line(px1, px2);
                        foreach (var node in line.GetPoints((int)Utility.GetDistanceToSqrt(px1, px2) + 1))
                        {
                            int itemid;
                            switch (node.Y)
                            {
                                case 627:
                                case 628:
                                case 629:
                                    itemid = 0x4DC;
                                    break;
                                default:
                                    itemid = 0x4D5;
                                    break;
                            }
                            Static plank = new Static(itemid);
                            int z = 13;
                            plank.MoveToWorld(new Point3D(node, z), Map.Felucca);
                            patched++;
                        }

                        // planks location start (630, 1495, 13) => (630, 1493, 13) (running north-south)
                        {
                            Static plank = new Static(0x4C8);
                            plank.MoveToWorld(new Point3D(630, 1494, 13), Map.Felucca);
                            patched++;
                            plank = new Static(0x4C8);
                            plank.MoveToWorld(new Point3D(630, 1493, 13), Map.Felucca);
                            patched++;
                        }

                        // add steps (item id 1828 (0x724))
                        steps = new Static(0x724);
                        steps.MoveToWorld(new Point3D(630, 1490, -1), Map.Felucca);
                        patched++;

                        steps = new Static(0x724);
                        steps.MoveToWorld(new Point3D(630, 1491, 3), Map.Felucca);
                        patched++;

                        steps = new Static(0x724);
                        steps.MoveToWorld(new Point3D(630, 1492, 8), Map.Felucca);
                        patched++;

                        // add a non-usable steps for aesthetics. (matching map statics)
                        steps = new Static(0x724);
                        steps.MoveToWorld(new Point3D(631, 1491, 3), Map.Felucca);
                        patched++;

                        steps = new Static(0x724);
                        steps.MoveToWorld(new Point3D(631, 1492, 8), Map.Felucca);
                        patched++;
                    }

                    // add pigs and boar spawner
                    {
                        Spawner pigs = new Spawner();
                        pigs.MoveToWorld(new Point3D(642, 1490, 0), Map.Felucca);
                        pigs.ObjectNamesRaw.Add("pig");
                        pigs.ObjectNamesRaw.Add("boar");
                        pigs.Count = 3;
                        pigs.HomeRange = 2;
                        pigs.Respawn();
                        patched++;
                    }

                    // walkways: cleanup old deco
                    {
                        int[] items = new[] { 0x40085B41, 0x400856EF, 0x40085B4B, 0x40085894, 0x40085A48, 0x40085A3D,
                            0x400859C9, 0x40085A7A, 0x40085A5D, 0x40085A84,0x40085BFF,0x40085841,0x4008586D,0x4008585F,};
                        foreach (int ix in items)
                        {
                            Item item = Server.World.FindItem(ix);
                            if (item != null)
                            {
                                item.Delete();
                                patched++;
                            }
                        }
                    }
                    // add stairs (1826 (0x722)) @ (645, 1484, 5) and (645, 1485, 0)
                    {
                        Static stairs = new Static(0x722);
                        stairs.MoveToWorld(new Point3D(645, 1484, 5), Map.Felucca);
                        stairs.Movable = false;
                        patched++;

                        stairs = new Static(0x722);
                        stairs.MoveToWorld(new Point3D(645, 1485, 0), Map.Felucca);
                        stairs.Movable = false;
                        patched++;

                        // remove odd thing that seems to be blocking
                        // top of stairs [area interface
                        //  target yourself twice so you have that exact tile
                        Item odd = Server.World.FindItem(0x40085D13);
                        if (odd != null)
                        {
                            odd.Delete();
                            patched++;
                        }

                        // add some planks
                        px1 = new Point2D(645, 1481);
                        px2 = new Point2D(645, 1483);
                        line = new Line(px1, px2);
                        foreach (var node in line.GetPoints((int)Utility.GetDistanceToSqrt(px1, px2) + 1))
                        {
                            Static plank = new Static(0x4D5);
                            int z = 10;
                            plank.MoveToWorld(new Point3D(node, z), Map.Felucca);
                            plank.Movable = false;
                            patched++;
                        }
                    }

                    // entrance walkway, east side
                    {   // corner plank missing
                        Static plank = new Static(0x4D5);
                        plank.MoveToWorld(new Point3D(636, 1496, 11), Map.Felucca);
                        plank.Movable = false;
                        patched++;

                        // more planks. double wide walkway
                        px1 = new Point2D(636, 1495);
                        px2 = new Point2D(640, 1495);
                        line = new Line(px1, px2);
                        bool toggle = false;
                        int itemid = 0x4D7;
                        int z = 0;
                        foreach (var node in line.GetPoints((int)Utility.GetDistanceToSqrt(px1, px2) + 1))
                        {
                            if (toggle == true)
                                itemid = 0x4D7;
                            else
                                itemid = 0x4D5;

                            toggle = !toggle;

                            plank = new Static(itemid);
                            z = node.X != 640 && node.X != 637 ? 12 : 11; // (f'ing odd ball zs)
                            plank.MoveToWorld(new Point3D(node, z), Map.Felucca);
                            plank.Movable = false;
                            patched++;
                        }

                        // add some stairs
                        Static stair = new Static(0x723);
                        stair.MoveToWorld(new Point3D(642, 1495, 2), Map.Felucca);
                        patched++;

                        stair = new Static(0x723);
                        stair.MoveToWorld(new Point3D(641, 1495, 6), Map.Felucca);
                        patched++;

                        // remove this extra plank (0x40006483)
                        if (Server.World.FindItem(0x40006483) != null)
                        {
                            Server.World.FindItem(0x40006483).Delete();
                            patched++;
                        }
                    }

                    // metal chest
                    {
                        MetalChest chest = new MetalChest();
                        chest.MoveToWorld(new Point3D(635, 1475, 16), Map.Felucca);
                        chest.Movable = false;
                        patched++;
                    }

                    #endregion Yew Orc Fort

                    #region Wind
                    EchoOut("Patching Wind...", ConsoleColor.Magenta);
                    EchoOut("\tAdd a permanent ogre corpse in Wind like was on Napa...", ConsoleColor.Magenta);
                    Static ogre = new Static(0x2006);
                    ogre.Movable = false;
                    ogre.Direction = Direction.Down;
                    ogre.MoveToWorld(new Point3D(5194, 233, 25), Map.Felucca);
                    patched++;

                    // turn on lich spawner at wind entrance
                    Spawner lichSpawner = (Spawner)FindOneItemAt(new Point3D(5186, 234, 25), Map.Felucca, typeof(Spawner));
                    if (lichSpawner != null)
                    {
                        // reclassify here as 'all shards'
                        UpdateSpawnerDesignationaAndLog(lichSpawner, coreSpawn: false,
                            uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                        lichSpawner.Running = true;
                        patched++;
                    }

                    #endregion Wind

                    #region Farm Cotton and Food
                    EchoOut("Patching farmable produce and cotton...", ConsoleColor.Magenta);
                    List<Type> exclude = new List<Type>() { typeof(FruitBasket) };
                    foreach (Item item in Server.World.Items.Values)
                    {
                        if (item == null || item.Deleted) continue;
                        if (item.Map != Map.Felucca) continue;
                        if (item is Spawner spnr)
                        {
                            if (spnr.Running) continue;
                            if (spnr.Spawns("cottonplant") || spnr.Spawns(typeof(Food), exclude))
                            {
                                // reclassify here as 'all shards'
                                UpdateSpawnerDesignationaAndLog(spnr, coreSpawn: false,
                                    uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                                spnr.Running = true;
                                spnr.Respawn();
                                patched++;
                            }
                        }
                    }
                    #endregion Farm Cotton and Food
                }
                #region disable T2A teles
                if (AllShards())
                {
                    EchoOut("Disabling T2A teleporters...", ConsoleColor.Magenta);
                    Teleporter tp = (Teleporter)FindOneItemAt(new Point3D(2399, 198, 0), Map.Felucca, typeof(Teleporter));
                    if (tp != null)
                    {
                        tp.Running = false;
                        patched++;
                    }

                    tp = (Teleporter)FindOneItemAt(new Point3D(2400, 198, 0), Map.Felucca, typeof(Teleporter));
                    if (tp != null)
                    {
                        tp.Running = false;
                        patched++;
                    }

                    tp = (Teleporter)FindOneItemAt(new Point3D(6005, 1378, 0), Map.Felucca, typeof(Teleporter));
                    if (tp != null)
                    {
                        tp.Running = false;
                        patched++;
                    }

                    tp = (Teleporter)FindOneItemAt(new Point3D(6005, 1379, 2), Map.Felucca, typeof(Teleporter));
                    if (tp != null)
                    {
                        tp.Running = false;
                        patched++;
                    }

                    tp = (Teleporter)FindOneItemAt(new Point3D(6005, 1380, 1), Map.Felucca, typeof(Teleporter));
                    if (tp != null)
                    {
                        tp.Running = false;
                        patched++;
                    }

                    tp = (Teleporter)FindOneItemAt(new Point3D(766, 1645, 0), Map.Felucca, typeof(Teleporter));
                    if (tp != null)
                    {
                        tp.Running = false;
                        patched++;
                    }

                    tp = (Teleporter)FindOneItemAt(new Point3D(766, 1646, 0), Map.Felucca, typeof(Teleporter));
                    if (tp != null)
                    {
                        tp.Running = false;
                        patched++;
                    }

                    tp = (Teleporter)FindOneItemAt(new Point3D(766, 1647, 0), Map.Felucca, typeof(Teleporter));
                    if (tp != null)
                    {
                        tp.Running = false;
                        patched++;
                    }
                }

                #endregion disable T2A teles

                #region disable teles to bugged cave (Trinsic)
                if (AllShards())
                {
                    EchoOut("Disable teles to bugged cave (Trinsic)...", ConsoleColor.Magenta);
                    Teleporter tp = (Teleporter)FindOneItemAt(new Point3D(1714, 2996, 0), Map.Felucca, typeof(Teleporter));
                    if (tp != null)
                    {
                        tp.Running = false;
                        patched++;
                    }

                    tp = (Teleporter)FindOneItemAt(new Point3D(1714, 2997, 0), Map.Felucca, typeof(Teleporter));
                    if (tp != null)
                    {
                        tp.Running = false;
                        patched++;
                    }
                }
                #endregion disable teles to bugged cave (Trinsic)

                #region allow pets through dungeon teleporters
                {
                    EchoOut("Allow pets through dungeon teleporters...", ConsoleColor.Magenta);
                    foreach (Item item in Server.World.Items.Values)
                    {
                        if (item is Teleporter tp && IsDungeonTeleporter(tp))
                        {
                            tp.Pets = true;
                            patched++;
                        }
                    }
                }
                #endregion allow pets through dungeon teleporters

                #region Add tinker to Britain
                if (SiegeStyleShards())
                {
                    EchoOut("Add tinker to Britain...", ConsoleColor.Magenta);
                    // re enable the tinker spawner we have in brit
                    List<Item> items = FindItemsAt(new Point3D(1426, 1657, 10), Map.Felucca, lenientZ: 1);
                    foreach (Item item in items)
                        if (item is Spawner sx)
                            if (sx.Distro == SpawnerModeAttribs.ModeLegacy)
                            {
                                // reclassify here as 'all shards'
                                UpdateSpawnerDesignationaAndLog(sx, coreSpawn: false,
                                    uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                                sx.Running = true;
                                patched++;
                            }


                }
                #endregion Add tinker to britain

                #endregion End Implementation

                return patched;
            }
            return null;
        }
        #endregion PatchV66
        #region PatchV67
        private static int? PatchV67(int patchid)
        {
            if (AllShards())
            {
                int patched = 0;
                #region Begin Implementation

                #region Patch Check
                // we had a bug in out patcher logic, so this 'patch check' just verifies it is fixed
                if (SiegeStyleShards())
                    EchoOut("SiegeStyleShards Patch Check Okay.", ConsoleColor.Magenta);

                if (AngelIsland())
                    EchoOut("AngelIsland Patch Check Okay.", ConsoleColor.Magenta);
                #endregion Patch Check

                if (SiegeStyleShards())
                {   // no slayer instruments on Siege
                    CoreAI.SlayerInstrumentDropRate = 0.0;
                    patched++;
                    /* [GOM] Od � Today at 11:57 AM
                     * @Adam Ant all the normal slayers we�re definitely on Seige
                     * Hard to find though
                     * Lvl 4/5 maps
                     * Silver was prevalent
                     */
                    CoreAI.SlayerWeaponDropRate = 0.05;
                    patched++;

                    // fix skill level for Wind teleporter
                    SkillTeleporter skillTeleporter = (SkillTeleporter)FindOneItemAt(new Point3D(1361, 883, 0), Map.Felucca, typeof(SkillTeleporter));
                    if (skillTeleporter != null)
                    {
                        skillTeleporter.Required = 72;
                        patched++;
                    }

                    // fix homerange on lich spawner at wind entrance
                    Spawner lichSpawner = (Spawner)FindOneItemAt(new Point3D(5186, 234, 25), Map.Felucca, typeof(Spawner));
                    if (lichSpawner != null)
                    {   // hang out near entrance
                        lichSpawner.HomeRange = 5;
                        patched++;
                    }
                }

                if (AllShards())
                {
                    // we've updated our spawn calcs for concentric spawners to reduce 'clumping'
                    //  To see this, we will need to do a total respawn.
                    patched += ScheduleRespawn();
                }

                #endregion End Implementation

                return patched;
            }
            return null;
        }
        #endregion PatchV67
        #region PatchV68
        private static int? PatchV68(int patchid)
        {
            if (AllShards())
            {
                int patched = 0;
                #region Begin Implementation

                #region broken tile
                Static tile = new Static(0x53D);
                tile.MoveToWorld(new Point3D(5924, 41, 44), Map.Felucca);
                patched++;
                #endregion broken tile

                #region broken stairs in Hythloth level 3

                #region add missing floor tiles
                tile = new Static(0x53F);
                tile.MoveToWorld(new Point3D(5926, 171, 22), Map.Felucca);
                patched++;

                tile = new Static(0x53F);
                tile.MoveToWorld(new Point3D(5926, 172, 22), Map.Felucca);
                patched++;

                tile = new Static(0x53F);
                tile.MoveToWorld(new Point3D(5926, 170, 22), Map.Felucca);
                patched++;

                tile = new Static(0x53F);
                tile.MoveToWorld(new Point3D(5926, 169, 22), Map.Felucca);
                patched++;

                tile = new Static(0x53F);
                tile.MoveToWorld(new Point3D(5926, 168, 22), Map.Felucca);
                patched++;
                #endregion add missing floor tiles

                #region remove blockers
                IPooledEnumerable eable = Map.Felucca.GetItemsInRange(new Point3D(5926, 172, 22), 5);
                List<Item> garbagecan = new List<Item>();
                foreach (Item item in eable)
                    if (item.ItemID == 0x21A4)          // blocker
                    {
                        garbagecan.Add(item);
                        patched++;
                    }
                eable.Free();

                eable = Map.Felucca.GetItemsInRange(new Point3D(5919, 171, 0), 2);
                foreach (Item item in eable)
                    if (item.ItemID == 0x21A4)          // blocker
                    {
                        garbagecan.Add(item);
                        patched++;
                    }
                eable.Free();

                foreach (Item item in garbagecan)
                    item.Delete();
                #endregion remove blockers

                #region add blockers
                List<Point3D> blockers = new List<Point3D>() {
                    new Point3D(5920, 171, 22),
                    new Point3D(5921, 171, 22),
                    new Point3D(5922, 171, 22),
                    new Point3D(5923, 171, 22),
                    new Point3D(5924, 171, 22),
                    new Point3D(5925, 171, 22),
                    new Point3D(5926, 168, 22),
                    new Point3D(5926, 169, 22),
                    new Point3D(5926, 170, 22),
                    new Point3D(5926, 171, 22),
                    new Point3D(5923, 168, 2),
                    new Point3D(5923, 169, 2),
                    new Point3D(5923, 170, 2),
                };

                foreach (Point3D bp in blockers)
                {
                    new Blocker().MoveToWorld(bp, Map.Felucca);
                    patched++;
                }
                #endregion add blockers

                #region wipe 6 teleporters 
                List<Point3D> teles = new List<Point3D>() {
                new Point3D(5923, 169, 1),
                new Point3D(5920, 170, 16),
                new Point3D(5920, 168, 16),
                new Point3D(5920, 169, 16),
                new Point3D(5921, 170, 14),
                new Point3D(5921, 169, 13),
                new Point3D(5921, 168, 12),
                };

                foreach (Point3D px in teles)
                {
                    Teleporter tele = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter));
                    if (tele != null)
                    {
                        tele.Delete();
                        patched++;
                    }
                }
                #endregion  wipe 6 teleporters 

                #region Add / Modify teleporters
                {
                    // add some teleporters
                    Teleporter tele;
                    // t1 at (5920, 168, 16), mapdest fel, pointdest (5919, 169, 22), direction West, directional true
                    tele = new Teleporter();
                    tele.Pets = true;
                    tele.MapDest = Map.Felucca;
                    tele.Direction = Direction.West;
                    tele.Directional = true;
                    tele.PointDest = new Point3D(5919, 168, 22);
                    tele.MoveToWorld(new Point3D(5920, 168, 16), Map.Felucca);
                    // (5920, 169, 17)
                    tele = new Teleporter();
                    tele.Pets = true;
                    tele.MapDest = Map.Felucca;
                    tele.Direction = Direction.West;
                    tele.Directional = true;
                    tele.PointDest = new Point3D(5919, 169, 22);
                    tele.MoveToWorld(new Point3D(5920, 169, 17), Map.Felucca);
                    // (5920, 170, 16)
                    tele = new Teleporter();
                    tele.Pets = true;
                    tele.MapDest = Map.Felucca;
                    tele.Direction = Direction.West;
                    tele.Directional = true;
                    tele.PointDest = new Point3D(5919, 170, 22);
                    tele.MoveToWorld(new Point3D(5920, 170, 16), Map.Felucca);
                    // t2 at (5921, 169, 12), mapdest fel, pointdest (5919, 169, 22), direction West, directional true
                    tele = new Teleporter();
                    tele.Pets = true;
                    tele.MapDest = Map.Felucca;
                    tele.Direction = Direction.West;
                    tele.Directional = true;
                    tele.PointDest = new Point3D(5919, 169, 22);
                    tele.MoveToWorld(new Point3D(5921, 169, 12), Map.Felucca);

                    // tp3 at (5922, 168, 6) set delay 0
                    tele = (Teleporter)FindOneItemAt(new Point3D(5922, 168, 6), Map.Felucca, typeof(Teleporter));
                    if (tele != null)
                    {
                        tele.Delay = TimeSpan.Zero;
                        tele.PointDest = new Point3D(6084, 144, -22);
                        patched++;
                    }
                    // tp4 at (5922, 169, 6) set delay 0
                    tele = (Teleporter)FindOneItemAt(new Point3D(5922, 169, 6), Map.Felucca, typeof(Teleporter));
                    if (tele != null)
                    {
                        tele.Delay = TimeSpan.Zero;
                        patched++;
                    }
                    // tp5 at (5922, 170, 6) set delay 0
                    tele = (Teleporter)FindOneItemAt(new Point3D(5922, 170, 6), Map.Felucca, typeof(Teleporter));
                    if (tele != null)
                    {
                        tele.Delay = TimeSpan.Zero;
                        tele.PointDest = new Point3D(6084, 146, -22);
                        patched++;
                    }
                }
                #endregion Add / Modify teleporters
                #endregion broken stairs in Hythloth level 3

                if (SiegeStyleShards())
                {
                    #region diable these T2A teles
                    eable = Map.Felucca.GetItemsInRange(new Point3D(5850, 432, 0), 5);
                    foreach (Item item in eable)
                        if (item is Teleporter t2at)
                        {
                            t2at.Running = false;
                            patched++;
                        }
                    eable.Free();
                    #endregion diable these T2A teles

                    #region Add EvilWanderingHealer
                    foreach (Item item in Server.World.Items.Values)
                    {
                        if (item == null || item.Map != Map.Felucca)
                            continue;
                        if (item is Spawner spawner && spawner.Running && spawner.ModeNeruns == true)
                        {
                            int index = 0;
                            if ((index = SpawnerIndexOf(spawner, "WanderingHealer")) != -1)
                            {
                                spawner.ObjectNamesRaw[index] =
                                    (spawner.ObjectNamesRaw[index] as string).Replace("WanderingHealer", "WanderingHealer:EvilWanderingHealer");
                                // index - 1: see comments in Spawner.Spawn
                                //spawner.EntryCount[index - 1] += 1; // bump up the number of creatures for this slot (because we added one)
                                spawner.SetSlotCount(index: index, spawner.GetSlotCount(index) + 1);
                                // don't need to respawn, it's done below in "Enable Kin"
                                //spawner.ScheduleRespawn = true;
                                patched++;
                            }
                        }
                    }
                    #endregion Add EvilWanderingHealer

                    #region Enable Kin
                    // we're enabling kin on Siege, a total respawn is required to reset alignment of all creatures
                    ScheduleRespawn();
                    #endregion Enable Kin
                }

                #endregion End Implementation

                return patched;
            }
            return null;
        }
        #endregion PatchV68
        #region PatchV69
        private static int? PatchV69(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Siege
                if (SiegeStyleShards())
                {
                    #region T2A blocker deco
                    List<KeyValuePair<int, Point3D>> patch_list = new()
                    {
                        new KeyValuePair<int,Point3D>(2121, new Point3D(5767, 2906, 15)),
                        new KeyValuePair<int,Point3D>(1, new Point3D(5767, 2907, 15)),
                        new KeyValuePair<int,Point3D>(2094, new Point3D(5767, 2907, 15)),
                        new KeyValuePair<int,Point3D>(2121, new Point3D(5767, 2908, 15)),
                        new KeyValuePair<int,Point3D>(2121, new Point3D(5767, 2905, 15)),
                        new KeyValuePair<int,Point3D>(2121, new Point3D(5767, 2909, 15)),
                        new KeyValuePair<int,Point3D>(2121, new Point3D(5767, 2904, 15)),
                        new KeyValuePair<int,Point3D>(2121, new Point3D(5767, 2910, 15)),
                        new KeyValuePair<int,Point3D>(2121, new Point3D(5767, 2911, 13)),
                        new KeyValuePair<int,Point3D>(8612, new Point3D(5761, 2911, 23)),
                        new KeyValuePair<int,Point3D>(8612, new Point3D(5760, 2912, 33)),
                        new KeyValuePair<int,Point3D>(8612, new Point3D(5761, 2912, 33)),
                        new KeyValuePair<int,Point3D>(8612, new Point3D(5762, 2912, 33)),
                        new KeyValuePair<int,Point3D>(8612, new Point3D(5763, 2912, 33)),
                        new KeyValuePair<int,Point3D>(8612, new Point3D(5764, 2912, 33)),
                        new KeyValuePair<int,Point3D>(8612, new Point3D(5765, 2912, 33)),
                        new KeyValuePair<int,Point3D>(8612, new Point3D(5766, 2912, 33)),
                        new KeyValuePair<int,Point3D>(8612, new Point3D(5767, 2912, 33)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5218, 2331, 30)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5216, 2333, 29)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5216, 2333, 29)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5218, 2333, 29)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5218, 2333, 29)),
                        new KeyValuePair<int,Point3D>(7038, new Point3D(5219, 2332, 29)),
                        new KeyValuePair<int,Point3D>(7038, new Point3D(5222, 2323, 28)),
                        new KeyValuePair<int,Point3D>(7038, new Point3D(5223, 2325, 30)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5218, 2329, 30)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5218, 2329, 30)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5221, 2332, 30)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5221, 2330, 30)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5221, 2330, 30)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5221, 2328, 30)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5221, 2328, 30)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5221, 2327, 28)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5221, 2327, 28)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5221, 2325, 28)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5221, 2325, 28)),
                        new KeyValuePair<int,Point3D>(4962, new Point3D(5222, 2325, 29)),
                        new KeyValuePair<int,Point3D>(4963, new Point3D(5221, 2331, 29)),
                        new KeyValuePair<int,Point3D>(4964, new Point3D(5220, 2333, 28)),
                        new KeyValuePair<int,Point3D>(4965, new Point3D(5219, 2330, 30)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5218, 2327, 30)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5218, 2327, 30)),
                        new KeyValuePair<int,Point3D>(4962, new Point3D(5219, 2333, 27)),
                        new KeyValuePair<int,Point3D>(4967, new Point3D(5221, 2329, 31)),
                        new KeyValuePair<int,Point3D>(4967, new Point3D(5224, 2325, 31)),
                        new KeyValuePair<int,Point3D>(4964, new Point3D(5224, 2328, 28)),
                        new KeyValuePair<int,Point3D>(7038, new Point3D(5221, 2326, 28)),
                        new KeyValuePair<int,Point3D>(4967, new Point3D(5222, 2324, 31)),
                        new KeyValuePair<int,Point3D>(4967, new Point3D(5221, 2324, 30)),
                        new KeyValuePair<int,Point3D>(8612, new Point3D(5221, 2322, 29)),
                        new KeyValuePair<int,Point3D>(8612, new Point3D(5221, 2320, 46)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5222, 2334, 30)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5222, 2334, 30)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5199, 2335, 32)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5199, 2335, 32)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5199, 2337, 27)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5198, 2336, 30)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5198, 2336, 30)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5204, 2333, 31)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5205, 2334, 32)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5205, 2334, 32)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5204, 2335, 30)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5204, 2335, 30)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5206, 2335, 29)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5200, 2334, 33)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5205, 2336, 28)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5205, 2336, 28)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5204, 2337, 28)),
                        new KeyValuePair<int,Point3D>(3287, new Point3D(5204, 2337, 28)),
                        new KeyValuePair<int,Point3D>(3286, new Point3D(5200, 2338, 27)),


                    };

                    foreach (var unit in patch_list)
                    {
                        if (unit.Key < 2) continue;
                        Static @static = new Static(unit.Key);
                        if (@static == null) continue;
                        @static.MoveToWorld(unit.Value, Map.Felucca);
                    }
                    #endregion T2A blocker deco

                    #region Fire dungeon blockers
                    Blocker blocker;
                    blocker = new Blocker();
                    blocker.MoveToWorld(new Point3D(5759, 2911, 23), Map.Felucca);
                    blocker = new Blocker();
                    blocker.MoveToWorld(new Point3D(5760, 2911, 23), Map.Felucca);
                    for (int ix = 5762; ix <= 5767; ix++)
                    {
                        blocker = new Blocker();
                        blocker.MoveToWorld(new Point3D(ix, 2911, 25), Map.Felucca);
                    }
                    #endregion Fire dungeon blockers
                }
                #endregion  Siege

                #region all shards
                #region Add moongate wizards to Fire and Ice dungeon entrances
                if (AllShards())
                {   // fire
                    Spawner spawner = new Spawner();
                    spawner.ObjectNamesRaw.Add("moongatewizard");
                    spawner.MoveToWorld(new Point3D(5761, 2908, 15), Map.Felucca);
                    spawner.Respawn();
                    // ice
                    spawner = new Spawner();
                    spawner.ObjectNamesRaw.Add("moongatewizard");
                    spawner.MoveToWorld(new Point3D(5213, 2321, 29), Map.Felucca);
                    spawner.Respawn();
                }
                #endregion Add moongate wizards to Fire and Ice dungeon entrances
                #endregion  all shards

                #region Siege
                #region spawner cleanup
                if (SiegeStyleShards())
                {
                    int found = 0;
                    List<Item> garbagecan = new List<Item>();
                    foreach (Item item in Server.World.Items.Values)
                    {
                        if (item == null || item.Map != Map.Felucca || item.Deleted) continue;

                        // while we're here, get rid of all the stacked spawner consoles
                        if (item is StackedSpawnerConsole)
                        {
                            garbagecan.Add(item);
                            patched++;
                        }
                        else if (item is Spawner spawner && spawner.ModeNeruns == true)
                        {
                            found++;

                            // find the surface on which we should be placed
                            int ceiling = Utility.HardCeiling(spawner.Map, spawner.X, spawner.Y, spawner.Z);
                            // move the spawner up/down
                            if (Math.Abs(spawner.Z - ceiling) <= 10)
                            {
                                patched++;
                                spawner.Z = ceiling;
                                // now jiggle - de-stack
                                // also, sometimes we're on like a display case, avoid if we can.
                                if (!spawner.Spawns("camp")) // don't jiggle camps. These seem to be specifically placed to 'fit'
                                    spawner.Location = Jiggle(spawner.Map, spawner.Location, spawner);
                                spawner.Respawn();
                            }
                            else
                            {   // seems like the z is fine. In that case, we just jiggle it
                                if (!spawner.Spawns("camp"))
                                {
                                    patched++;
                                    spawner.Location = Jiggle(spawner.Map, spawner.Location, spawner);
                                }
                            }

                        }
                    }

                    // delete consoles
                    foreach (Item garbage in garbagecan)
                        garbage.Delete();
                }
                #endregion spawner cleanup
                #endregion Siege

                #endregion End Implementation

                return patched;
            }
            return null;
        }
        #endregion PatchV69
        #region PatchV70
        private static int? PatchV70(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation
                #region Rares
                if (SiegeStyleShards())
                {
                    List<Point3D> rare_spawners = new()
                        {
                            new Point3D(5192, 584, 0),
                            new Point3D(5301, 593, 0),
                            new Point3D(286, 986, 6),
                            new Point3D(1134, 2220, 46),
                            new Point3D(1133, 2220, 66),
                            new Point3D(3788, 1121, 30),
                            new Point3D(2636, 2081, 16),
                            new Point3D(2685, 2065, 35),
                            new Point3D(5949, 220, 34),
                            new Point3D(3656, 2510, 0),
                            new Point3D(5946, 216, 29),
                            new Point3D(5649, 1404, 22),
                            new Point3D(1397, 3809, 0), // Toolkit 
                            new Point3D(554, 2187, 6), // WhiteChessmen 
                            new Point3D(1931, 2795, 3), // BlackChessmen 
                            new Point3D(5804, 141, -5), // Rocks 
                            new Point3D(5168, 566, 6), // CandleLarge 
                            new Point3D(955, 709, 20), // FruitBasket 
                            new Point3D(5575, 1829, 6), // CandleLarge 
                            new Point3D(5582, 1829, 6), // CandleLarge
                        };

                    foreach (Point3D px in rare_spawners)
                    {
                        Spawner spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 0, false);
                        if (spawner != null)
                        {
                            // reclassify here as 'all shards'
                            UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false,
                                uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                            // while we're here, call it a "Rares Spawner"
                            spawner.Name = "Rares Spawner";
                            spawner.Running = true;
                            spawner.Respawn();
                            patched++;
                        }
                    }
                }
                #endregion Rares
                #region Control Slots
                if (SiegeStyleShards())
                {
                    foreach (Mobile m in Server.World.Mobiles.Values)
                    {
                        if (m is null || m is not PlayerMobile) continue;
                        // remove control slots
                        (m as PlayerMobile).FollowersMax = 16;
                        patched++;
                    }
                }
                #endregion Control Slots
                #region Spawn trainer chests in banks, and all other dungeon chests
                if (SiegeStyleShards())
                {
                    foreach (Item item in Server.World.Items.Values)
                    {
                        if (item == null || item.Map != Map.Felucca) continue;
                        if (item is Spawner spawner && spawner.ModeNeruns == false && spawner.Running == false)
                        {
                            if (spawner.Spawns("TreasureChest"))
                            {
                                UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false,
                                uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                                spawner.Running = true;
                                spawner.Respawn();
                                patched++;
                            }
                        }
                    }
                }
                #endregion Spawn trainer chests in banks, and all other dungeon chests
                #endregion End Implementation

                return patched;
            }
            return null;
        }
        #endregion PatchV70
        #region PatchV71
        private static int? PatchV71(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation
                #region Map stuffs
                if (SiegeStyleShards())
                {
                    Teleporter teleporter = (Teleporter)FindOneItemAt(new Point3D(5882, 243, 1), Map.Felucca, typeof(Teleporter), 2, false);
                    if (teleporter != null)
                    {   // z was off .. fix now
                        teleporter.Location = new Point3D(5882, 243, 1);
                        patched++;
                    }
                }
                if (AllShards())
                {   // disable trinsic passage to T2A
                    Teleporter teleporter = (Teleporter)FindOneItemAt(new Point3D(1629, 3320, 0), Map.Felucca, typeof(Teleporter), 2, false);
                    if (teleporter != null)
                    {
                        teleporter.Running = false;
                        patched++;
                    }
                }
                #endregion Map stuffs
                #region Solen Caves
                // note: green thorns were handled in code
                if (AllShards())
                {
                    patched += DisableSolenCaveSpawners();
                    patched += DisableTelesFromToSolenCaves();
                }
                #endregion Solen Caves
                #region Library books at WBB (AllShards)
                {
                    WebBook webBook = null;
                    if (AllShards())
                    {
                        webBook = (WebBook)FindOneItemAt(new Point3D(1415, 1688, 6), Map.Felucca, typeof(WebBook), 2, false);
                        if (webBook != null)
                        {
                            webBook.Description = "News";
                            webBook.URL = "https://game-master.net/news.html";
                            webBook.Name = Core.Server;
                            patched++;
                        }
                        webBook = (WebBook)FindOneItemAt(new Point3D(1414, 1688, 6), Map.Felucca, typeof(WebBook), 2, false);
                        if (webBook != null)
                        {
                            webBook.Description = "Discord";
                            webBook.URL = "https://discord.gg/GwRuSV9vAb";
                            webBook.Name = Core.Server;
                            patched++;
                        }
                        webBook = (WebBook)FindOneItemAt(new Point3D(1414, 1687, 6), Map.Felucca, typeof(WebBook), 2, false);
                        if (webBook != null)
                        {
                            webBook.Description = "Wiki";
                            webBook.URL = "https://game-master.net/aiwiki/index.php?title=Main_Page";
                            webBook.Name = Core.Server;
                            patched++;
                        }
                        webBook = (WebBook)FindOneItemAt(new Point3D(1414, 1686, 6), Map.Felucca, typeof(WebBook), 2, false);
                        if (webBook != null)
                        {
                            webBook.Description = "Beginner Info";
                            webBook.URL = "https://game-master.net/aiwiki/index.php?title=Beginners_Guide";
                            webBook.Name = Core.Server;
                            patched++;
                        }
                        webBook = (WebBook)FindOneItemAt(new Point3D(1414, 1685, 6), Map.Felucca, typeof(WebBook), 2, false);
                        if (webBook != null)
                        {
                            webBook.Description = "FAQ";
                            webBook.URL = "https://game-master.net/aiwiki/index.php?title=FAQ";
                            webBook.Name = Core.Server;
                            patched++;
                        }
                        webBook = (WebBook)FindOneItemAt(new Point3D(1415, 1685, 6), Map.Felucca, typeof(WebBook), 2, false);
                        if (webBook != null)
                        {
                            webBook.Description = "Tutorials";
                            webBook.URL = "https://game-master.net/aiwiki/index.php?title=Video_Tutorials";
                            webBook.Name = Core.Server;
                            patched++;
                        }
                    }
                }
                #endregion Library books at WBB
                #region Orc goodies (Siege Only)
                if (SiegeStyleShards())
                {
                    // spinning wheel south addon (674, 1200, 0)
                    SpinningwheelSouthAddon swsa = new();
                    swsa.MoveToWorld(new Point3D(674, 1200, 0), Map.Felucca);
                    // loom south addon (677, 1200, 1)
                    LoomSouthAddon lsa = new();
                    lsa.MoveToWorld(new Point3D(677, 1200, 1), Map.Felucca);
                    // provisioner spawner (682, 1203, 0)
                    Spawner provisioner = new();
                    provisioner.ObjectNamesRaw.Add("Provisioner");
                    provisioner.MoveToWorld(new Point3D(682, 1203, 0), Map.Felucca);
                    // reclassify here as 'all shards'
                    UpdateSpawnerDesignationaAndLog(provisioner, coreSpawn: false,
                        uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                }
                #endregion Orc goodies (Siege Only)
                // do last since it respawns
                #region Patch Mobile Activation
                if (AllShards())
                {   // We changed the way mobiles get deactivated. 
                    //  A respawn is needed to deactivate all mobiles that should be so
                    ScheduleRespawn();
                }
                #endregion Patch Mobile Activation
                #endregion End Implementation

                return patched;
            }
            return null;
        }
        #endregion PatchV71
        #region PatchV72
        private static int? PatchV72(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation
                #region Teleporter maintenance (All Shards)
                if (AllShards())
                {   // make all dungeon exits/entrances directional
                    patched += DirectionalDungeonTPs();

                    // remove these covetous teleporters
                    List<Point3D> list = new() { new Point3D(2544, 850, 0), new Point3D(2545, 850, 0), new Point3D(2546, 850, 0) };
                    foreach (Point3D px in list)
                    {
                        Teleporter teleporter = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
                        if (teleporter != null)
                        {
                            teleporter.Delete();
                            patched++;
                        }
                    }

                    // manually fix these Ice dungeon teleporters
                    list.Clear();
                    list.AddRange(new List<Point3D>() { new Point3D(5874, 146, 27), new Point3D(5875, 146, 27), new Point3D(5876, 146, 27), new Point3D(5877, 146, 27) });
                    foreach (Point3D px in list)
                    {
                        Teleporter teleporter = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
                        if (teleporter != null)
                        {
                            teleporter.Direction = Direction.North;
                            teleporter.Directional = true;
                            patched++;
                        }
                    }
                }
                if (AngelIsland())
                {
                    // bad z on this guy
                    Teleporter teleporter = (Teleporter)FindOneItemAt(new Point3D(5882, 243, 0), Map.Felucca, typeof(Teleporter), 2, false);
                    if (teleporter != null)
                    {
                        teleporter.Z++;
                        patched++;
                    }

                    teleporter = (Teleporter)FindOneItemAt(new Point3D(5187, 639, 0), Map.Felucca, typeof(Teleporter), 2, false);
                    if (teleporter != null)
                    {
                        teleporter = (Teleporter)Utility.Dupe(teleporter);
                        teleporter.MoveToWorld(new Point3D(5186, 639, 0), Map.Felucca);
                        patched++;
                    }
                }
                #endregion Teleporter maintenance (All Shards)
                #endregion End Implementation

                return patched;
            }
            return null;
        }
        #endregion PatchV72
        #region PatchV73
        private static int? PatchV73(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation
                #region Wipe Factions
                if (SiegeStyleShards())
                {
                    /*patched += unknown*/
                    patched += Server.Factions.Generator.DegenerateFactions().TotalCount();
                }
                #endregion Wipe Factions
                #region KinMigrate
                if (AllShards())
                {
                    patched += KinMigrate();
                }
                #endregion KinMigrate
                #region Spawners
                if (SiegeStyleShards())
                {
                    // set this spawner to 'Dummy' .. can't be killed
                    Spawner spawner = (Spawner)FindOneItemAt(new Point3D(1416, 1700, 1), Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner != null)
                    {
                        spawner.Dummy = true;
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                }
                #endregion Spawners
                #region Addons
                if (AllShards())
                {   // wipe the old tree farm signs from brit farms
                    List<Rectangle2D> list = new() {
                        new Rectangle2D(1185, 1809,31, 31),
                        new Rectangle2D(1176, 1672,32, 24),
                        new Rectangle2D(1208, 1712,32, 24),
                    };
                    List<Item> garbage = new();
                    foreach (var rct in list)
                    {
                        IPooledEnumerable eable = Map.Felucca.GetItemsInBounds(rct);
                        foreach (Item item in eable)
                        {
                            if (item is AddonComponent)
                                garbage.Add(item);
                        }
                        eable.Free();
                    }

                    foreach (var item in garbage)
                    {
                        item.Delete();
                        patched++;
                    }
                }

                #endregion Addons
                #region Covetous teleporter
                if (AllShards())
                {
                    // missing on tp here
                    Teleporter teleporter = (Teleporter)FindOneItemAt(new Point3D(5615, 1998, 0), Map.Felucca, typeof(Teleporter), 2, false);
                    if (teleporter != null)
                    {
                        teleporter = (Teleporter)Utility.Dupe(teleporter);
                        teleporter.Y += 1;
                        teleporter.Z = 4;
                        teleporter.MoveToWorld(teleporter.Location, teleporter.Map);
                        patched++;
                    }
                }
                #endregion Covetous teleporter
                #endregion End Implementation

                return patched;
            }
            return null;
        }
        #endregion PatchV73
        #region PatchV74
        private static int? PatchV74(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation
                /*
                 * We accidentally patched with this empty.
                 * Dummy patch
                 */
                #endregion End Implementation

                return patched;
            }
            return null;
        }
        #endregion PatchV74
        #region PatchV75
        private static int? PatchV75(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation
                #region Wind Solen Spawners
                if (SiegeStyleShards())
                {
                    // turn off this spawner. Siege folks complain it wasn't there 'back in the day'
                    Spawner spawner = (Spawner)FindOneItemAt(new Point3D(1359, 893, 0), Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner != null)
                    {
                        spawner.Running = false;
                        spawner.RemoveObjects();
                        patched++;
                    }
                }
                #endregion Wind Solen Spawners
                #region Orc goodies (Siege Only)
                if (SiegeStyleShards())
                {
                    // spinning wheel south addon (674, 1200, 0)
                    SpinningwheelSouthAddon swsa = (SpinningwheelSouthAddon)FindOneItemAt(new Point3D(674, 1200, 0), Map.Felucca, typeof(SpinningwheelSouthAddon), 2, false);
                    if (swsa == null)
                    {
                        swsa = new();
                        swsa.MoveToWorld(new Point3D(674, 1200, 0), Map.Felucca);
                        patched++;
                    }

                    // loom south addon (677, 1200, 1)
                    LoomSouthAddon lsa = (LoomSouthAddon)FindOneItemAt(new Point3D(677, 1200, 1), Map.Felucca, typeof(LoomSouthAddon), 2, false);
                    if (lsa == null)
                    {
                        lsa = new();
                        lsa.MoveToWorld(new Point3D(677, 1200, 1), Map.Felucca);
                        patched++;
                    }
                }
                #endregion Orc goodies (Siege Only)
                #region Evil Orcs! (Siege Only)
                if (SiegeStyleShards())
                {
                    // record items before
                    List<Item> before = Before(Map.Felucca);
                    // stuff

                    Generate(Path.Combine(Core.DataDirectory, "Decoration/Felucca"),
                        DecoMode.add, file: "*.cfg", listOnly: false, types: new Type[] { typeof(BaseAddon) }, maps: new Map[] { Map.Felucca });
                    Generate(Path.Combine(Core.DataDirectory, "Decoration/Britannia"),
                        DecoMode.add, file: "*.cfg", listOnly: false, types: new Type[] { typeof(BaseAddon) }, maps: new Map[] { Map.Felucca });

                    // record items after
                    List<Item> after = After(Map.Felucca);

                    // what's the delta?
                    patched += Delta(before, after).Count;
                }
                #endregion Evil Orcs! (Siege Only)
                #region Mount Speed
                if (SiegeStyleShards())
                {
                    //  On AI this is 2 because there are no mounts.
                    //  On Siege, we need this to be 4 for mounted players (twice as fast.)
                    SecurityManagementConsole.FwdMaxStepsMount = 4;
                    patched++;
                }
                #endregion MountSpeed
                #endregion End Implementation

                return patched;
            }
            return null;
        }
        #endregion PatchV75
        #region PatchV76
        private static int? PatchV76(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation
                #region New Player Starting Area and New Guild (Siege)
                if (SiegeStyleShards())
                {
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);
                    // New guild is now on
                    //CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerGuild);
                    patched += 1;
                }
                #endregion New Player Starting Area and New Guild (Siege)
                #region Convert unusual chests to dungeon chests (Siege)
                if (SiegeStyleShards())
                {
                    try
                    {
                        Mute();
                        patched += UnusualContainerSpawner.WipeContainerCache();
                        patched += UnusualContainerSpawner.RebuildContainerCache();
                    }
                    finally
                    {
                        Unmute();
                    }
                }
                #endregion Convert unusual chests to dungeon chests (Siege)
                #endregion End Implementation

                return patched;
            }
            return null;
        }
        #endregion PatchV76
        #region PatchV77
        private static int? PatchV77(int patchid)
        {
            PatchIndex bits = PatchIndex.PatchV77;
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation
                #region Hide Bob Champ (and other champ maint.) (Siege)
                if (SiegeStyleShards())
                {   // (1392, 3785)+(7, 8)
                    IPooledEnumerable eable = Map.Felucca.GetItemsInBounds(new Rectangle2D(1392, 3785, 7, 8));

                    foreach (Item item in eable)

                        if (item is not null && item.Deleted == false)
                        {
                            item.Visible = false;
                            item.Z -= 5;
                        }
                    // disable all seasonal champs
                    ResetChampsWorker(bits, ref patched);
                }
                #endregion Hide Bob Champ (and other champ maint.) (Siege)
                #region  CoreManagementConsole, we need to set MeleePoisonSkillFactor false
                if (SiegeStyleShards())
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.MeleePoisonSkillFactor);
                #endregion  CoreManagementConsole, we need to set MeleePoisonSkillFactor false
                #region Patch New member names (Siege)
                if (SiegeStyleShards())
                {
                    foreach (BaseGuild g in BaseGuild.List.Values)
                    {
                        Guild guild = g as Guild;
                        if (guild != null)
                        {
                            if (guild.Abbreviation.Contains("Cob", StringComparison.OrdinalIgnoreCase))
                            {   // the current guild master cannot be voted out
                                guild.FixedGuildmaster = true;
                                foreach (var mem in guild.Members)
                                {   // guildmaster can-change her own title later as she sees fit.
                                    (mem as Server.Mobiles.PlayerMobile).GuildTitle = "Citizen";
                                    patched++;
                                }
                            }
                        }
                    }
                }
                #endregion Patch New member names  (Siege)
                #region Turn off Moongate Wizards (Siege)
                if (SiegeStyleShards())
                {
                    foreach (Item item in Server.World.Items.Values)
                        if (IsFelucca(item) && item is Spawner s)
                        {
                            if (s.Spawns("MoonGateWizard"))
                            {
                                s.Running = false;
                                s.RemoveObjects();
                                patched++;
                            }
                        }
                }
                #endregion Turn off Moongate Wizards (Siege)
                #region Turn on banker in Cove (Siege)
                if (SiegeStyleShards())
                {
                    // (2227, 1199, 0)
                    // (2227, 1198, 0)
                    List<Point3D> list = new() { new Point3D(2227, 1199, 0), new Point3D(2227, 1198, 0) };
                    foreach (var pt in list)
                    {
                        Spawner spawner = (Spawner)FindOneItemAt(pt, Map.Felucca, typeof(Spawner), 2, false);
                        if (spawner != null)
                        {
                            // reclassify here as 'all shards'
                            UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false,
                                uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                            spawner.Running = true;
                            spawner.ScheduleRespawn = true;
                            patched++;
                        }
                    }
                }
                #endregion Turn on banker in Cove (Siege)
                #region Turn off Jukas
                // Jukas appeared in Ultima Online's third expansion. Electronic Arts sponsored a launch party for the expansion on April 6, 2001. 
                //  So they are 'in era' However, the Siege folks say they weren't on Siege, or at least in Wrong.
                //  https://www.uoguide.com/Ultima_Online:_Third_Dawn
                //  https://www.uoguide.com/Juka
                if (SiegeStyleShards())
                {
                    foreach (Item item in Server.World.Items.Values)
                        if (IsFelucca(item) && item is Spawner s)
                        {
                            if (s.Spawns("juka"))
                            {
                                s.Running = false;
                                s.RemoveObjects();
                                patched++;
                            }
                        }
                }
                #endregion Turn off Jukas
                #region Add Nightmare in Covetous level 2
                if (SiegeStyleShards())
                {
                    // (5584, 2012, 0)
                    List<Point3D> list = new() { new Point3D(5584, 2012, 0) };
                    foreach (var pt in list)
                    {
                        Spawner spawner = (Spawner)FindOneItemAt(pt, Map.Felucca, typeof(Spawner), 2, false);
                        if (spawner != null)
                        {
                            // reclassify here as 'all shards'
                            UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false,
                                uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                            spawner.Running = true;
                            spawner.ScheduleRespawn = true;
                            patched++;
                        }
                    }
                }
                #endregion Add Nightmare in Covetous level 2
                #region Respawn World
                if (AllShards())
                {
                    ScheduleRespawn();
                }
                #endregion Respawn World
                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV77
        #region PatchV78
        private static int? PatchV78(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Outcasts guild stone
                // we have a new model for 'too many accounts' on our shards
                // Blocking on EventSink_GameLogin just hangs the client (Both CUO and OSI.)You can only gracefully
                //  We instead log in the abhorrent account into prison, make the mortal, and add them to this outcast guild
                if (AllShards())
                {
                    Mobile gm = Server.World.GetAdminAcct();
                    Guild guild = new Guild(gm, "The Outcasts", "ToC");

                    if (gm.Guild != null)
                        ((Guild)gm.Guild).ResignMember(gm);

                    gm.Guild = guild;
                    gm.GuildTitle = "Guildmaster";
                    Guildstone gs = new Guildstone(guild);
                    guild.Guildstone = gs;
                    gs.FixedGuildmaster = true; // guild master does not change
                    gs.GuildAlignment = AlignmentType.Outcast;
                    gs.GuildWarRing = true;     // at war with everyone
                    gs.NoResign = true;         // you cannot resign
                    gs.IsStaffOwned = true;     // so cron does not delete this guild stone not in a house
                    gs.IsNoCountingGuild = true;// cannot report murder
                    gs.MoveToWorld(new Point3D(352, 838, 20), Map.Felucca);
                    patched++;
                }
                #endregion Outcasts guild stone
                #region Death Trap
                if (AllShards())
                {
                    List<Point3D> list = new() { new Point3D(354, 834, 0) };
                    foreach (var pt in list)
                    {
                        Spawner spawner = (Spawner)FindOneItemAt(pt, Map.Felucca, typeof(Spawner), 2, false);
                        if (spawner == null)
                        {
                            spawner = new Spawner();
                            spawner.ObjectNamesRaw.Add("rottingcorpse");
                            spawner.Count = 3;
                            spawner.HomeRange = 2;
                            spawner.MoveToWorld(pt, Map.Felucca);
                            // reclassify here as 'all shards'
                            UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false,
                                uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                            spawner.Running = true;
                            spawner.ScheduleRespawn = true;
                            patched++;
                        }
                    }
                }
                #endregion Death Trap
                #region explosion potions
                if (SiegeStyleShards())
                {   // point based vs mobile based
                    CoreAI.ExplosionPotionTargetMethod = EPTM.PointBased;
                }
                #endregion explosion potions
                #region Trees to protect orc fort
                if (SiegeStyleShards())
                {
                    Server.Engines.Plants.PlantAddon tree = new Server.Engines.Plants.PlantAddon("tree1");
                    tree.MoveToWorld(new Point3D(615, 1464, 0), Map.Felucca);
                    tree = new Server.Engines.Plants.PlantAddon("tree2");
                    tree.MoveToWorld(new Point3D(613, 1486, 0), Map.Felucca);
                    tree = new Server.Engines.Plants.PlantAddon("ohiitree");
                    tree.MoveToWorld(new Point3D(613, 1477, 0), Map.Felucca);
                }
                #endregion Trees to protect orc fort
                #region Respawn World
                if (AllShards())
                {
                    patched += ScheduleRespawn();
                }
                #endregion Respawn World
                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV78
        #region PatchV79
        private static int? PatchV79(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation
                #region Skill Gain Bonus
                if (SiegeStyleShards())
                {
                    SkillGainSystem.Bonus = 0.0;
                    patched++;
                }
                #endregion
                #region Respawn World
                // changed SBInfo for Architect to remove custom houses
                if (SiegeStyleShards())
                {
                    patched += ScheduleRespawn();
                }
                #endregion Respawn World
                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV79
        #region PatchV80
        private static int? PatchV80(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation
                #region Fix missing tiles in Covetous
                if (AllShards())
                {
                    List<Point3D> list = new() { new Point3D(5592, 1840, -3), new Point3D(5592, 1841, -3), new Point3D(5592, 1842, -3) };
                    foreach (var pt in list)
                    {
                        Static tile = new(0x7A3);
                        tile.MoveToWorld(pt, Map.Felucca);
                        patched++;
                    }
                }
                #endregion Fix missing tiles in Covetous
                #region Revert Hythloth 'directional' teleporters
                #region More Info
                /*
                 * Don't really understand the problem here, it's not obvious. But I suspect it has to do
                 * with both teleporters having the same directionality, and perhaps how the mobile is facing.
                 * We will punt for now, for launch.
                 */
                #endregion More Info
                if (AllShards())
                {
                    List<Point3D> list = new() { new Point3D(4723, 3813, 0), new Point3D(4722, 3813, 0) , new Point3D(4721, 3813, 0) ,
                        new Point3D(5906, 16, 64), new Point3D(5905, 16, 64), new Point3D(5904, 16, 64)};
                    foreach (var pt in list)
                    {
                        Teleporter tele = (Teleporter)FindOneItemAt(pt, Map.Felucca, typeof(Teleporter), 2, false);
                        if (tele != null)
                        {
                            tele.Directional = false;
                            patched++;
                        }
                    }
                }
                #endregion Revert Hythloth 'directional' teleporters
                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV80
        #region PatchV81
        private static int? PatchV81(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation
                #region Replace 'deck tile' with feather
                if (Core.RuleSets.SiegeRules())
                {
                    List<Item> list = new();
                    foreach (Item item in Server.World.Items.Values)
                    {
                        if (item is null || item.Deleted)
                            continue;
                        if (item is MountItem && item.Parent is Backpack)
                        {   // prison wasn't dismounting players, so it dropped the MountItem in their backpack.
                            list.Add(item);
                        }
                    }

                    foreach (Item item in list)
                    {
                        ReplaceItem(new Feather(), item, copyProperties: false);
                        item.Delete();
                    }
                }
                #endregion Replace 'deck tile' with feather
                #region Replace AI Stingers with an AISouvenir
                if (Core.RuleSets.SiegeRules())
                {
                    List<Item> list = new();
                    foreach (Item item in Server.World.Items.Values)
                    {
                        if (item is null || item.Deleted)
                            continue;
                        if (item is AIStinger)
                        {   // see if they are still in prison
                            if (item.RootParent is PlayerMobile pm && pm.PrisonInmate == true)
                                continue;
                            // otherwise, it shouldn't be out there
                            list.Add(item);
                        }
                    }

                    foreach (Item item in list)
                    {
                        ReplaceItem(AITeleportHelper.AISouvenir(), item, copyProperties: false);
                        item.Delete();
                    }
                }
                #endregion Replace AI Stingers with an AISouvenir
                #region Patch All Ankhs
                if (AllShards())
                {
                    patched += Ankhs.PatchAllAnkhs();
                }
                #endregion Patch All Ankhs
                #region Britain Farms Patch
                if (SiegeStyleShards())
                {
                    BritainFarmsPatch();
                    patched++;
                }
                #endregion Britain Farms Patch
                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV81
        #region PatchV82
        private static int? PatchV82(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation
                #region Reset all MachineInfo
                #endregion Reset all MachineInfo
                #region Establish Area Peace Settings
                if (SiegeStyleShards())
                {   // 12s max
                    /*CoreAI.AreaPeaceCrowdSize = 6;
                    CoreAI.AreaPeaceDelayBase = 1.0;
                    CoreAI.AreaPeaceDelayBonus = 3.0;
                    CoreAI.AreaPeaceSoloBonus = 2.0;*/
                    // 6s max, one creature, 100 skill
                    CoreAI.AreaPeaceDelayBase = 1.0;
                    CoreAI.AreaPeaceDelayBonus = 2.0;
                    CoreAI.AreaPeaceCrowdSize = 6;
                    CoreAI.AreaPeaceSoloBonus = 1.0;
                    patched++;
                }
                //(1276, 1486, 7)
                #endregion Establish Area Peace Settings
                #region Remove orphaned oven
                if (SiegeStyleShards())
                {
                    AddonComponent addon = (AddonComponent)FindOneItemAt(new Point3D(1276, 1486, 7), Map.Felucca, typeof(AddonComponent), 2, false);
                    if (addon != null)
                    {
                        addon.Delete();
                        patched++;
                    }
                }
                #endregion Remove orphaned oven
                #region Covetous Mountain Pass Teleporters
                if (SiegeStyleShards())
                {
                    Teleporter tele = (Teleporter)FindOneItemAt(new Point3D(5393, 1959, 0/*1*/), Map.Felucca, typeof(Teleporter), 2, false);
                    if (tele != null)
                    {
                        tele.Z = 1;
                        patched++;
                    }
                    tele = (Teleporter)FindOneItemAt(new Point3D(5394, 1959, 0/*3*/), Map.Felucca, typeof(Teleporter), 2, false);
                    if (tele != null)
                    {
                        tele.Z = 3;
                        patched++;
                    }
                }
                #endregion Covetous Mountain Pass Teleporters
                #region Respawn dungeon chests
                if (AllShards())
                {
                    foreach (Item item in Server.World.Items.Values)
                        if (item is Spawner sp && sp.Spawns("TreasureChest"))
                        {
                            sp.ScheduleRespawn = true;
                            patched++;
                        }
                }
                #endregion Respawn dungeon chests
                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV82
        #region PatchV83
        private static int? PatchV83(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Turn on Hire Fighters in Jhelom fighting pit
                if (SiegeStyleShards())
                {
                    Spawner spawner = (Spawner)FindOneItemAt(new Point3D(1399, 3742, -21), Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner != null)
                    {
                        // reclassify here as 'all shards'
                        UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false,
                            uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                        spawner.Running = true;
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                }
                #endregion Turn on Hire Fighters in Jhelom fighting pit

                #region Solens
                if (SiegeStyleShards())
                {
                    List<Point3D> list = new List<Point3D>() { new Point3D(1942, 3261, 2), new Point3D(3217, 500, 0) };
                    foreach (Point3D px in list)
                    {
                        Spawner spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                        if (spawner != null)
                        {
                            spawner.Running = false;
                            spawner.RemoveObjects();
                            patched++;
                        }
                    }
                }
                #endregion Solens
                #region Disable Keyword Teles
                if (SiegeStyleShards())
                {
                    List<Point3D> list = new() { new Point3D(5736, 3196, 10), new Point3D(5974, 2697, 35),
                        new Point3D(5974, 2697, 35), new Point3D(5267, 2757, 35)};
                    foreach (var pt in list)
                    {
                        Teleporter tele = (Teleporter)FindOneItemAt(pt, Map.Felucca, typeof(Teleporter), 2, false);
                        if (tele != null)
                        {
                            tele.Running = false;
                            patched++;
                        }
                    }
                }
                #endregion Disable Keyword Teles
                #region Replace Sea Horses
                if (SiegeStyleShards())
                {
                    foreach (Item item in Server.World.Items.Values)
                        if (item is Spawner spawner && spawner.Spawns(type: typeof(SeaHorse)))
                        {
                            int index = SpawnerIndexOf(spawner, "seahorse");
                            List<string> names = spawner.ObjectNamesParsed;
                            names = names.ConvertAll(d => d.ToLower());
                            List<string> raw = spawner.ObjectNamesRaw.Cast<string>().ToList();
                            List<string> core = new() { "dolphin", "seaserpent", "waterelemental", "deepseaserpent", "kraken" };
                            var result = core.Where(p => !names.Any(p2 => p2.Equals(p)));
                            if (result.Count() > 0)
                                // give them what's not in the list
                                foreach (string name in result)
                                {
                                    string temp = spawner.ObjectNamesRaw[index] as string;
                                    temp = temp.Replace("seahorse", name, StringComparison.OrdinalIgnoreCase);
                                    spawner.ObjectNamesRaw[index] = temp;
                                    spawner.RemoveObjects();
                                    spawner.ScheduleRespawn = true;
                                    patched++;
                                    break;
                                }
                            else
                            {
                                // select something at random
                                string temp = spawner.ObjectNamesRaw[index] as string;
                                int str_index = Utility.RandomMinMaxScaled(0, core.Count - 1);
                                temp = temp.Replace("seahorse", core[str_index], StringComparison.OrdinalIgnoreCase);
                                spawner.ObjectNamesRaw[index] = temp;
                                spawner.RemoveObjects();
                                spawner.ScheduleRespawn = true;
                                patched++;
                            }
                        }
                }
                #endregion Replace Sea Horses
                #region Add Trainer Chests to all Banks
                if (SiegeStyleShards())
                {
                    List<BaseContainer> containers = new();
                    foreach (Mobile m in Server.World.Mobiles.Values)
                    {
                        if (m is Banker || m is Minter && m.Map == Map.Felucca)
                        {
                            IPooledEnumerable eable = m.GetItemsInRange(20);
                            foreach (Item item in eable)
                                if (item is BaseContainer bc && !containers.Contains(bc))
                                    containers.Add(item as BaseContainer);
                            eable.Free();
                        }
                    }

                    foreach (BaseContainer bc in containers)
                    {
                        if (!bc.Movable && bc is not DungeonTreasureChest && bc is MetalChest || bc is WoodenChest)
                        {
                            Spawner spawner = new Spawner();
                            spawner.GraphicID = bc.ItemID;  // need to get the correct 'flip' direction
                            spawner.ObjectNamesRaw.Add("L0TreasureChest");
                            spawner.HomeRange = 0;
                            spawner.MaxDelay = TimeSpan.FromMinutes(10);
                            spawner.MinDelay = TimeSpan.FromMinutes(5);
                            spawner.MoveToWorld(bc.Location, Map.Felucca);
                            spawner.ScheduleRespawn = true;
                            // reclassify here as 'all shards'
                            UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false,
                                uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);
                            bc.Delete();
                            patched++;
                        }
                    }
                }
                #endregion Add Trainer Chests to all Banks

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV83
        #region PatchV84
        private static int? PatchV84(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Fixes to Fire Temple
                if (SiegeStyleShards())
                {
                    // enable area 48 spawner                        
                    Spawner spawner = (Spawner)FindOneItemAt(new Point3D(4595, 3572, 75), Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner != null)
                    {
                        // reclassify here as 'all shards'
                        UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false,
                            uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                        spawner.Running = true;
                        spawner.HomeRange = 9;
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }

                    // disable Nerun's spawner
                    List<Item> spawners = (List<Item>)FindItemsAt(new Point3D(4595, 3576, 30), Map.Felucca);
                    foreach (Item it in spawners)
                        if (it is Spawner s && s.Distro == SpawnerModeAttribs.ModeNeruns)
                        {
                            s.Running = false;
                            s.RemoveObjects();
                            patched++;
                        }

                    // set Nerun's to ClearPath
                    spawners = (List<Item>)FindItemsAt(new Point3D(4569, 3572, 30), Map.Felucca);
                    foreach (Item it in spawners)
                        if (it is Spawner s && s.Distro == SpawnerModeAttribs.ModeNeruns)
                        {
                            s.SpawnerFlags = spawner.SpawnerFlags | SpawnFlags.ClearPath;
                            s.RemoveObjects();
                            s.ScheduleRespawn = true;
                            patched++;
                        }

                    // disable Nerun's spawner
                    spawners = (List<Item>)FindItemsAt(new Point3D(4620, 3572, 30), Map.Felucca);
                    foreach (Item it in spawners)
                        if (it is Spawner s && s.Distro == SpawnerModeAttribs.ModeNeruns)
                        {
                            s.Running = false;
                            s.RemoveObjects();
                            patched++;
                        }

                    // enable area 48 spawner
                    spawner = (Spawner)FindOneItemAt(new Point3D(4592, 3570, 30), Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner != null)
                    {
                        // reclassify here as 'all shards'
                        UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false,
                            uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                        spawner.Running = true;
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }

                    // enable pub15 spawner
                    spawner = (Spawner)FindOneItemAt(new Point3D(4620, 3567, 30), Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner != null)
                    {
                        // reclassify here as 'all shards'
                        UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false,
                            uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                        spawner.Running = true;
                        spawner.SpawnerFlags = spawner.SpawnerFlags | SpawnFlags.ClearPath;
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }

                    // set Nerun's spawner to ClearPath 
                    spawners = (List<Item>)FindItemsAt(new Point3D(4597, 3608, 30), Map.Felucca);
                    foreach (Item it in spawners)
                        if (it is Spawner s && s.Distro == SpawnerModeAttribs.ModeNeruns)
                        {
                            s.SpawnerFlags = spawner.SpawnerFlags | SpawnFlags.ClearPath;
                            s.RemoveObjects();
                            s.ScheduleRespawn = true;
                            patched++;
                        }
                }
                #endregion Fixes to Fire Temple

                #region Update All Spawner Graphics (on/off)
                if (SiegeStyleShards())
                {
                    foreach (Item item in Server.World.Items.Values)
                        if (item is Spawner spawner)
                        {
                            spawner.UpdateDisplay();
                            patched++;
                        }
                }
                #endregion Update All Spawner Graphics (on/off)

                #region All existing guards' spawners should be ClearPath
                if (SiegeStyleShards())
                {
                    foreach (Item item in Server.World.Items.Values)
                        if (item is Spawner spawner && spawner.Running && spawner.Spawns(typeof(BaseGuard)))
                        {
                            spawner.SpawnerFlags = spawner.SpawnerFlags | SpawnFlags.ClearPath;
                            spawner.RemoveObjects();
                            spawner.ScheduleRespawn = true;
                            patched++;
                        }
                }
                #endregion Update All Spawner Graphics (on/off)

                #region Add Item Blocker at Yew Orc Fort
                if (AllShards())
                {
                    NoDropTarget ndt = (NoDropTarget)FindOneItemAt(new Point3D(634, 1492, 0), Map.Felucca, typeof(NoDropTarget), 2, false);
                    if (ndt == null)
                    {
                        ndt = new NoDropTarget();
                        ndt.MoveToWorld(new Point3D(634, 1492, 0), Map.Felucca);
                        patched++;
                    }
                }
                #endregion Add Item Blocker at Yew Orc Fort

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV84
        #region PatchV85
        private static int? PatchV85(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Bring to home
                if (SiegeStyleShards())
                {
                    foreach (Mobile mob in Server.World.Mobiles.Values)
                        if (mob is BaseCreature bc && !bc.Deleted && bc.Spawner != null)
                            if (bc.Map != null && bc.Map != Map.Internal && bc.Spawner.Map != null && bc.Spawner.Map != Map.Internal)
                                if (bc.Spawner.Distro == SpawnerModeAttribs.ModeLegacy)
                                    if (bc.Home != bc.Spawner.Location)
                                    {
                                        bc.Home = bc.Spawner.Location;
                                        bc.MoveToWorld(bc.Spawner.Location, bc.Spawner.Map);
                                        patched++;
                                    }
                }
                #endregion Bring to home

                #region Add Spawners To Deceit
                if (SiegeStyleShards())
                {
                    // two lich two lichlord here
                    Point3D px = new Point3D(5311, 748, -20);
                    Spawner spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner == null)
                    {
                        // do we need classification here?
                        // UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false, uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);
                        spawner = new();
                        spawner.Distro = SpawnerModeAttribs.ModeMulti;
                        spawner.ObjectNamesRaw.Add("lich");
                        spawner.ObjectNamesRaw.Add("lichlord");
                        spawner.Counts = "2,2";
                        spawner.HomeRange = 8;
                        spawner.MoveToWorld(px, Map.Felucca);
                        spawner.Running = true;
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }

                    // one lichlord here
                    px = new Point3D(5267, 691, 0);
                    spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner == null)
                    {
                        // do we need classification here?
                        // UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false, uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);
                        spawner = new();
                        spawner.Distro = SpawnerModeAttribs.ModeLegacy;
                        spawner.ObjectNamesRaw.Add("lichlord");
                        spawner.Count = 1;
                        spawner.HomeRange = 4;
                        spawner.MoveToWorld(px, Map.Felucca);
                        spawner.Running = true;
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }

                    // two lich here
                    px = new Point3D(5264, 656, 0);
                    spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner == null)
                    {
                        // do we need classification here?
                        // UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: false, uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);
                        spawner = new();
                        spawner.Distro = SpawnerModeAttribs.ModeLegacy;
                        spawner.ObjectNamesRaw.Add("lich");
                        spawner.Count = 2;
                        spawner.HomeRange = 5;
                        spawner.WalkRange = 8;
                        spawner.MoveToWorld(px, Map.Felucca);
                        spawner.Running = true;
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                }
                #endregion Add Spawners To Deceit

                #region Update Buc's Spawners
                if (SiegeStyleShards())
                {
                    Spawner spawner = null;
                    // mages
                    Point3D px = new Point3D(2747, 2116, 0);
                    if (DeleteSpawner(px))
                        patched++;
                    if (AddSpawner(px, new List<string>() { "mage" }, coreSpawn: true, spawner: ref spawner, count: 2, homeRange: 4))
                        patched++;

                    // evil healers
                    px = new Point3D(2707, 2147, 0);
                    if (DeleteSpawner(px))
                        patched++;
                    if (AddSpawner(px, new List<string>() { "evilHealer" }, coreSpawn: true, spawner: ref spawner, count: 2, homeRange: 4))
                        patched++;

                    // Alchemists
                    px = new Point3D(2779, 2131, 0);
                    if (DeleteSpawner(px))
                        patched++;
                    if (AddSpawner(px, new List<string>() { "alchemist" }, coreSpawn: true, spawner: ref spawner, count: 2, homeRange: 4))
                        patched++;

                    // Animal trainer 
                    px = new Point3D(2715, 2211, 0);
                    if (DeleteSpawner(px))
                        patched++;
                    if (AddSpawner(px, new List<string>() { "animalTrainer" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 4))
                        patched++;

                    // Tailor 
                    px = new Point3D(2755, 2219, 0);
                    if (DeleteSpawner(px))
                        patched++;
                    if (AddSpawner(px, new List<string>() { "tailor" }, coreSpawn: true, spawner: ref spawner, count: 2, homeRange: 4))
                        patched++;
                }

                #endregion Update Buc's Spawners

                #region Allow Pets on Wind Teleporters
                if (SiegeStyleShards())
                {
                    Point3D px = new Point3D(5191, 152, 0);
                    Teleporter tele = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
                    if (tele != null)
                        tele.Pets = true;

                    px = new Point3D(5200, 71, 17);
                    tele = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
                    if (tele != null)
                        tele.Pets = true;

                    px = new Point3D(5217, 18, 15);
                    tele = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
                    if (tele != null)
                        tele.Pets = true;
                }
                #endregion Allow Pets on Wind Teleporters

                #region Patch TownshipStone Registration
                if (SiegeStyleShards())
                {
                    foreach (Item item in Server.World.Items.Values)
                        if (item is TownshipStone ts)
                        {
                            ts.Registered = true;
                            patched++;
                        }
                }
                #endregion Patch TownshipStone Registration

                #region Make Ilshenar EventConfirmationSungate's permanent
                if (SiegeStyleShards())
                {
                    List<Point3D> list = new() { new Point3D(1362, 1105, -26), new Point3D(1365, 1105, -26) };
                    foreach (Point3D px in list)
                    {
                        EventConfirmationSungate sungate = (EventConfirmationSungate)FindOneItemAt(px, Map.Ilshenar, typeof(EventConfirmationSungate), 2, false);
                        if (sungate != null)
                        {
                            sungate.DurationOverride = true;
                            sungate.Duration = TimeSpan.FromHours(8000);
                            sungate.Countdown = TimeSpan.FromSeconds(5);
                        }
                    }
                }
                #endregion Make Ilshenar EventConfirmationSungate's permanent

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV85
        #region PatchV86
        private static int? PatchV86(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region NOT AT THIS TIME
                #endregion NOT AT THIS TIME

                #region Add random spawn to this island
                if (SiegeStyleShards())
                {
                    Spawner spawner = null;
                    if (AddSpawner(new Point3D(1283, 528, 30), new List<string>() { "bird", "pig", "rabbit" }, coreSpawn: false, spawner: ref spawner, count: 8, homeRange: 20))
                        patched++;
                }
                #endregion Add random spawn to this island

                #region Add Phoenix
                if (SiegeStyleShards())
                {
                    var px = new Point3D(5950, 1316, 9);
                    Spawner spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner == null)
                    {
                        spawner = new Spawner();
                        spawner.ObjectNamesRaw.Add("Phoenix");
                        spawner.Running = true;
                        spawner.ScheduleRespawn = true;
                        spawner.MoveToWorld(px, Map.Felucca);
                        patched++;
                    }
                }
                #endregion Add Phoenix

                #region Disable more T2A teleporters
                // (6025, 1344, -26), (6025, 1345, -26), (6025, 1346, -26)
                if (AllShards())
                {
                    List<Point3D> list = new() { new Point3D(6025, 1344, -26), new Point3D(6025, 1345, -26), new Point3D(6025, 1346, -26) };
                    foreach (var px in list)
                    {
                        DisbleTeleporter(px);
                        patched++;
                    }
                }
                #endregion Disable more T2A teleporters

                #region Magic Wand Drop Chance
                if (SiegeStyleShards())
                    CoreAI.MagicWandDropChance = 0.02;
                else
                    CoreAI.MagicWandDropChance = 0.00;
                patched++;
                #endregion Magic Wand Drop Chance

                #region Enable various Pub15 Spawners
                if (SiegeStyleShards())
                {
                    Spawner spawner = null;
                    // (1891, 1085, 2), concentric, allshards
                    if (EnableSpawner(new Point3D(1891, 1085, 2), ref spawner))
                    {
                        spawner.Concentric = true;
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                    // (1856, 1076, 0) enable only
                    if (EnableSpawner(new Point3D(1856, 1076, 0), ref spawner))
                    {
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                    // (1871, 1076, 0) move to (1858, 1082, 0)
                    if (MoveSpawner(old_location: new Point3D(1871, 1076, 0), new_location: new Point3D(1858, 1082, 0), ref spawner))
                    {
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                    // (2073, 864, 0) allshards
                    if (EnableSpawner(new Point3D(2073, 864, 0), ref spawner))
                    {
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                    // (2109, 865, 0) concentric, allshards
                    if (EnableSpawner(new Point3D(2109, 865, 0), ref spawner))
                    {
                        spawner.Concentric = true;
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                    // (2110, 864, 0) concentric, allshards
                    if (EnableSpawner(new Point3D(2110, 864, 0), ref spawner))
                    {
                        spawner.Concentric = true;
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                    // (2111, 862, 0) enable
                    if (EnableSpawner(new Point3D(2111, 862, 0), ref spawner))
                    {
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                    // Dupe (2109, 865, 0), move to (2169, 869, 0)
                    if (DupeSpawner(at: new Point3D(2109, 865, 0), to: new Point3D(2169, 869, 0), ref spawner))
                    {
                        if (EnableSpawner(new Point3D(2169, 869, 0), ref spawner, "Supplemental"))
                            patched++;
                    }
                    // Dupe (2110, 864, 0) (2169, 867, 0)
                    if (DupeSpawner(at: new Point3D(2110, 864, 0), to: new Point3D(2169, 867, 0), ref spawner))
                    {
                        if (EnableSpawner(new Point3D(2169, 867, 0), ref spawner, "Supplemental"))
                            patched++;
                    }
                }
                #endregion

                #region Horde Minions
                //  turn off spawner, remove objects
                if (DisableSpawner(new Point3D(3621, 2814, 21)))
                    patched++;
                #endregion Horde Minions

                #region Move buc's spawner
                if (SiegeStyleShards())
                {
                    // from 2816 3499 0
                    // to (2817, 3504, 0)
                    Spawner spawner = null;
                    if (MoveSpawner(old_location: new Point3D(2816, 3499, 0), new_location: new Point3D(2817, 3504, 0), ref spawner))
                        patched++;
                }
                #endregion Move buc's spawner

                #region Patch all UOSpawnMap Spawners
                if (AllShards())
                {
                    string area = "Area";
                    List<Spawner> spawner_list = new();
                    foreach (Item item in World.Items.Values)
                    {
                        if (item is Spawner spnr)
                        {
                            if ((spnr.Name != null &&
                                 spnr.Name.Contains(area, StringComparison.OrdinalIgnoreCase)) ||
                                (spnr.Source != null &&
                                 spnr.Source.Contains(area, StringComparison.OrdinalIgnoreCase)))
                            {
                                spawner_list.Add(spnr);
                            }
                        }
                    }

                    // normalize
                    Dictionary<int, List<Spawner>> spawner_area_designation = new();
                    foreach (Spawner spnr in spawner_list)
                    {
                        int designation = 0;
                        string text = spnr.Name + " " + spnr.Source;
                        string[] tokens = text.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        int index = Array.FindIndex(tokens, t => t.Equals(area, StringComparison.OrdinalIgnoreCase));
                        if (index >= 0 && index < tokens.Length - 1)
                        {
                            designation = int.Parse(tokens[index + 1]);
                            if (spawner_area_designation.ContainsKey(designation))
                                spawner_area_designation[designation].Add(spnr);
                            else
                                spawner_area_designation.Add(designation, new List<Spawner>() { spnr });
                        }
                    }

                    foreach (var kvp in spawner_area_designation)
                    {
                        int id = 1;
                        foreach (Spawner target in kvp.Value)
                        {
                            target.Name = null;
                            target.Source = area + " " + string.Format("{0}, {1} of {2}", kvp.Key, id++, kvp.Value.Count);
                            target.CoreSpawn = true;
                            target.Distro = SpawnerModeAttribs.ModeLegacy;
                            target.ModeAI = false;
                            target.ModeNeruns = false;
                            target.Shard = ShardConfig.Core;
                            target.SpawnerFlags = target.SpawnerFlags | SpawnFlags.Concentric;
                            patched++;
                        }
                    }
                }
                #endregion Patch all UOSpawnMap Spawners

                #region Area 30 Spawn
                if (SiegeStyleShards())
                {   // remove manually placed spawners (and we will update our real Area 30 spawner to do the right thing)
                    List<Point3D> list = new() {
                        new Point3D(2019, 947, 0),
                        new Point3D(2004, 977, 0),
                        new Point3D(2034, 977, 0),
                        new Point3D(2064, 977, 0),
                        new Point3D(2004, 1007, 0),
                        new Point3D(2034, 1007, 0),
                        new Point3D(2064, 1007, 0),
                        new Point3D(1974, 1022, 0),
                        new Point3D(2004, 1037, 0),
                        new Point3D(2034, 1037, 0),
                        new Point3D(2064, 1037, 0),
                        new Point3D(2049, 1067, 0),
                     };
                    // delete temporary spawners
                    foreach (Point3D px in list)
                    {
                        DeleteSpawner(px);
                        patched++;
                    }

                    // okay, now fix our Area 30 spawner
                    // 
                    Spawner spawner = (Spawner)FindOneItemAt(new Point3D(2034, 1012, 0), Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner != null)
                    {
                        spawner.RemoveObjects();
                        spawner.ObjectNamesRaw.Clear();
                        spawner.ObjectNamesRaw.Add("lizardman");
                        spawner.ObjectNamesRaw.Add("alligator");
                        spawner.ObjectNamesRaw.Add("snake");
                        spawner.Count = 24; // was 12
                        spawner.Running = true;
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                }
                #endregion Area 30 Spawn

                #region Region Fix for IsTownRegion
                if (AllShards())
                {
                    foreach (StaticRegion sr in StaticRegion.XmlDatabase)
                    {
                        if (Array.IndexOf(m_TownNames, sr.Name) != -1 || (sr.IsGuarded && CountVendors(sr) >= 5))
                        {
                            sr.UseTownRules = true;
                            patched++;
                        }
                    }
                }
                #endregion Region Fix for IsTownRegion

                #region Region Fix enter/exit messages
                if (AllShards())
                {
                    foreach (StaticRegion sr in StaticRegion.XmlDatabase)
                    {
                        if (sr.UseTownRules || sr.IsDungeonRules || sr.IsJailRules || sr.IsPrisonRules || sr.IOBZone)
                        {
                            sr.ShowEnterMessage = true;
                            sr.ShowExitMessage = true;
                            patched++;
                        }
                    }
                }
                #endregion Region Fix enter/exit messages

                #region More Buc's Spawners
                if (SiegeStyleShards())
                {
                    Spawner spawner = null;
                    // Scribe
                    Point3D px = new Point3D(2746, 2116, 0);
                    if (DeleteSpawner(px))
                        patched++;
                    if (AddSpawner(px, new List<string>() { "Scribe" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 4))
                        patched++;

                    // Shipwright
                    px = new Point3D(2734, 2252, 0);
                    if (DeleteSpawner(px))
                        patched++;
                    if (AddSpawner(px, new List<string>() { "Shipwright" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 4))
                        patched++;

                    // Tinker
                    px = new Point3D(2635, 2082, 10);
                    if (DeleteSpawner(px))
                        patched++;
                    if (AddSpawner(px, new List<string>() { "Tinker" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 4))
                        patched++;

                    // Herbalist
                    px = new Point3D(2778, 2131, 0);
                    if (DeleteSpawner(px))
                        patched++;
                    if (AddSpawner(px, new List<string>() { "Herbalist" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 4))
                        patched++;
                }
                #endregion  More Buc's Spawners

                #region Delete orphaned keys
                if (SiegeStyleShards())
                {
                    List<Key> key_list = new List<Key>();
                    List<KeyRing> ring_list = new List<KeyRing>();
                    foreach (Item item in Server.World.Items.Values)
                    {
                        if (item is Key key && !key.Deleted)
                        {
                            if (key.Map == Map.Internal && key.RootParent == null)
                                if (key.IsIntMapStorage == true)
                                {
                                    key_list.Add(key);
                                }
                        }

                        if (item is KeyRing keyring && !keyring.Deleted)
                        {
                            ring_list.Add(keyring);
                        }
                    }

                    int orphans = 0;
                    int loved = 0;
                    bool found = false;
                    List<Key> orphaned_key_list = new List<Key>();
                    foreach (Key key in key_list)
                    {
                        found = false;
                        foreach (KeyRing keyring in ring_list)
                        {
                            if (keyring.IsKeyOnRing(key.Serial))
                            {   // not an orphan
                                loved++;
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            orphans++;
                            patched++;
                            orphaned_key_list.Add(key);
                        }
                    }

                    foreach (Key key in orphaned_key_list)
                        key.Delete();
                }
                #endregion Delete orphaned keys

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV86
        #region PatchV87
        private static int? PatchV87(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region turn on Moongate Wizards
                if (SiegeStyleShards())
                {
                    int before = CountMoonGateWizards();
                    MoonGateWizard.OracleGen_OnCommand(new CommandEventArgs());
                    int after = CountMoonGateWizards();
                    patched += after - before;
                }
                #endregion turn on Moongate Wizards

                #region Turn off Ice/Fire T2A exist
                if (AllShards())
                {
                    List<Point3D> list = new()
                    {   // ice
                        new Point3D(5203, 2327, 27),  new Point3D(5207, 2322, 27), new Point3D(5207, 2323, 26),
                        new Point3D(5874, 146, 27), new Point3D(5875, 146, 27), new Point3D(5876, 146, 27), new Point3D(5877, 146, 27),
                        // fire
                        new Point3D(5792, 1415, 41), new Point3D(5792, 1416, 41), new Point3D(5792, 1417, 41),
                        new Point3D(5757, 2909, 15), new Point3D(5757, 2908, 14), new Point3D(5757, 2907, 15) ,
                        };

                    foreach (var px in list)
                        if (DisableTeleporter(px))
                            patched++;
                }
                #endregion Turn off Ice/Fire T2A exist

                #region Map Cleanup
                if (AllShards())
                {
                    // teleportes in spirit spawn under stones
                    List<Point3D> list = new() { new Point3D(5748, 362, 2), new Point3D(5749, 362, 0), new Point3D(5750, 362, 3) };
                    foreach (var px in list)
                        if (DeleteTeleporter(px))
                            patched++;

                    // stone under floor (5750, 363, -1)
                    list = new() { new Point3D(5751, 363, 0), new Point3D(5750, 363, -1) };
                    foreach (var px in list)
                        if (DeleteItem(px))
                            patched++;
                }
                #endregion Map Cleanup

                #region Info on Wrong
                // https://www.uoguide.com/Wrong
                // https://web.archive.org/web/20010721225156if_/http://uo.stratics.com:80/
                // https://web.archive.org/web/20020606030919/http://uo.stratics.com/content/atlas/wrong.shtml
                #endregion

                #region Wrong Jail Cells
                if (SiegeStyleShards())
                {   // Imprisoned in the West Wing cells are inmates desperate for help escaping.
                    // https://www.uoguide.com/Wrong

                    // move this oddly placed teleporter
                    if (MoveItem(old_location: new Point3D(5733, 554, 20), new_location: new Point3D(5733 - 1, 554, 20)))
                        patched++;

                    // first disable treasure chests
                    List<Point3D> list = new() { 
                        // west wing
                        new Point3D(5650, 553, 20), new Point3D(5651, 558, 21), new Point3D(5660, 553, 20), 
                        // east wing
                        new Point3D(5725, 560, 20), new Point3D(5703, 521, 0) };
                    foreach (var px in list)
                        if (DisableSpawner(px))
                            patched++;

                    // disable these teleporters
                    list = new() { new Point3D(5741, 549, 12), new Point3D(5741, 550, 12),
                        new Point3D(5872, 532, 24), new Point3D(5867, 528, 15), new Point3D(5871, 531, 15), new Point3D(5870, 530, 15),
                        new Point3D(5871, 530, 15), new Point3D(5870, 531, 15), new Point3D(5867, 528, 15), new Point3D(5868, 528, 15),
                        new Point3D(5868, 529, 15), new Point3D(5867, 529, 15), new Point3D(5873, 533, 15), new Point3D(5872, 532, 24)};
                    foreach (var px in list)
                        if (DisableTeleporter(px))
                            patched++;

                    Spawner spawner = null;
                    list = new() { new Point3D(5852, 584, 15), new Point3D(5852, 570, 15) };
                    foreach (var px in list)
                        if (AddSpawner(px, new List<string>() { "DeadBrigandGuard" }, coreSpawn: true, spawner: ref spawner, count: 3, homeRange: 4))
                            patched++;

                    list = new() { new Point3D(5854, 553, 15) };
                    foreach (var px in list)
                        if (AddSpawner(px, new List<string>() { "DeadBrigandGuard" }, coreSpawn: true, spawner: ref spawner, count: 4, homeRange: 8))
                            patched++;

                    list = new() { new Point3D(5860, 542, 15) };
                    foreach (var px in list)
                        if (AddSpawner(px, new List<string>() { "WrongPrisonerLizardman" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 4))
                            patched++;

                    list = new() { new Point3D(5861, 546, 15) };
                    foreach (var px in list)
                        if (AddSpawner(px, new List<string>() { "WrongPrisonerLizardman" }, coreSpawn: true, spawner: ref spawner, count: 2, homeRange: 6))
                            patched++;

                    list = new() { new Point3D(5854, 551, 15), new Point3D(5852, 565, 15) };
                    foreach (var px in list)
                        if (AddSpawner(px, new List<string>() { "Lizardman" }, coreSpawn: true, spawner: ref spawner, count: 3, homeRange: 8))
                            patched++;

                    // Add 'Wrong Prisoners' (these are 'camps' that include a treasure chest)
                    list = new() { 
                        // west wing
                        new Point3D(5666, 560, 20), new Point3D(5659, 560, 20), new Point3D(5652, 560, 20),
                        // east wing
                        new Point3D(5726, 561, 20), new Point3D(5722, 561, 20), new Point3D(5718, 561, 20)
                    };
                    foreach (var px in list)
                        if (AddSpawner(px, new List<string>() { "WrongPrisonerNorth" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 0))
                            patched++;

                    // Add 'Wrong Prisoners' last meal
                    list = new() { 
                        // last meal
                        new Point3D(5691, 528, 0), new Point3D(5689, 525, 0),
                    };
                    foreach (var px in list)
                        if (AddSpawner(px, new List<string>() { "WrongPrisonerLastMeal" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 0))
                            patched++;

                    // Add 'Wrong Prisoners' time to die
                    list = new() { 
                        // last meal
                        new Point3D(5680, 523, 0), new Point3D(5675, 525, 0), new Point3D(5704, 523, 0), new Point3D(5705, 527, 0)
                    };
                    foreach (var px in list)
                        if (AddSpawner(px, new List<string>() { "WrongExecuteNoble" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 0))
                            patched++;

                    if (AddSpawner(new Point3D(5792, 544, 10), new List<string>() { "WrongExecuteController" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 0))
                        patched++;

                    list = new() { new Point3D(5825, 526, 0), new Point3D(5831, 523, 0) };
                    foreach (var px in list)
                        if (AddSpawner(px, new List<string>() { "WrongExecuteController" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 0))
                            patched++;

                    list = new() { new Point3D(5869, 554, 15), };
                    foreach (var px in list)
                        if (AddSpawner(px, new List<string>() { "DeadExecutioner" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 0))
                            patched++;

                    // disable these treasure chests
                    list = new() { new Point3D(5852, 593, 16), new Point3D(5864, 578, 15), new Point3D(5863, 545, 15) };
                    foreach (var px in list)
                        if (DisableSpawner(px))
                            patched++;

                    // delete these deco chests
                    list = new() { new Point3D(5786, 557, 10), new Point3D(5786, 556, 10),
                        new Point3D(5786, 555, 10), new Point3D(5786, 554, 10), new Point3D(5678, 529, 0),
                        new Point3D(5723, 559, 20), new Point3D(5727, 554, 20), new Point3D(5734, 549, 20), new Point3D(5718, 549, 20),
                        new Point3D(5669, 520, 0), new Point3D(5667, 549, 22), new Point3D(5648, 549, 22)};
                    foreach (var px in list)
                        while (DeleteItem(px))
                            patched++;

                    // hide these sparkles
                    list = new() { new Point3D(5867, 528, 15), new Point3D(5872, 532, 24) };
                    foreach (var px in list)
                        while (HideItem(px))
                            patched++;

                }
                #endregion

                #region Dungeon Wrong Spawners
                if (SiegeStyleShards())
                {
                    // enable P15 spawnerd
                    List<Point3D> list = new()
                    {
                        new Point3D(5825, 600, 1),  // giant rat
                        //new Point3D(5791, 545, 10),
                        //new Point3D(5790, 571, 10),
                        //new Point3D(5790, 579, 10),
                        //new Point3D(5789, 593, 10),
                    };
                    Spawner spawner = null;
                    foreach (var px in list)
                    {
                        if (EnableSpawner(px, ref spawner))
                        {
                            spawner.ScheduleRespawn = true;
                            SetSpawnerFlags(px, SpawnFlags.ClearPath);
                            patched++;
                        }
                    }

                    {   // disable treasure chest(s)
                        list = new List<Point3D>() {
                            new Point3D(5791, 579, 11), new Point3D(5791, 544, 10),new Point3D(5821, 529, 0),
                        };
                        foreach (var px in list)
                        {
                            if (DisableSpawner(px))
                                patched++;
                        }
                    }

                    // add prisoners (Wrong Prisoner West)
                    { // 
                        list = new List<Point3D>() {
                            new Point3D(5790, 555, 10), new Point3D(5790, 563, 10),new Point3D(5790, 571, 10),
                            new Point3D(5790, 579, 10),new Point3D(5790, 587, 10),new Point3D(5790, 594, 10)
                        };
                        foreach (var px in list)
                        {
                            // first disable, move x-1 of an existing spawner
                            if (DisableSpawner(px))
                            {
                                patched++;
                                if (MoveSpawner(old_location: px, new_location: new Point3D(px.X - 1, px.Y, px.Z), ref spawner))
                                    patched++;
                            }

                            // now Add
                            if (AddSpawner(px, new List<string>() { "WrongPrisonerWest" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 0))
                            {
                                patched++;
                            }
                        }
                    }

                    // add executioners
                    {
                        //Point3D px = new Point3D(5793, 544, 10);
                        //AddSpawner(px, new List<string>() { "Executioner" }, coreSpawn: true, count: 2, homeRange: 7);
                        //SetSpawnerFlags(px, SpawnFlags.ClearPath);
                        //patched++;

                        // enable these guys
                        /*Point3D px = new Point3D(5828, 529, 0);
                        if (EnableSpawner(px, ref spawner))
                        {
                            spawner.ScheduleRespawn = true;
                            SetSpawnerFlags(px, SpawnFlags.ClearPath);
                            patched++;
                        }*/
                    }

                    // rebuild golems and golem controllers
                    {
                        {   // disable Nerun's spawner at (5790, 544, 10)
                            Point3D px = new Point3D(5790, 544, 10);
                            foreach (Item item in FindItemsAt(px, Map.Felucca, lenientZ: 2))
                            {
                                if (item is Spawner s && s.Distro == SpawnerModeAttribs.ModeNeruns)
                                {
                                    DisableSpawner(s);
                                    patched++;
                                }
                            }
                        }

                        // add lizardmen
                        {
                            Point3D px = new Point3D(5822, 577, 0);
                            AddSpawner(px, new List<string>() { "LizardMan" }, coreSpawn: false, spawner: ref spawner, count: 2, homeRange: 6);
                        }
                    }
                }
                #endregion Dungeon Wrong Spawners

                #region RazorInstrument
                if (AllShards())
                {   // Make RazorInstrument(LootType.UnStealable | LootType.UnLootable) so that is newbied for reds too
                    foreach (Item item in Server.World.Items.Values)
                        if (item is RazorInstrument ri)
                        {
                            ri.LootType = LootType.UnStealable | LootType.UnLootable;
                            patched++;
                        }
                }
                #endregion RazorInstrument

                #region Orc fort stuffs
                if (SiegeStyleShards())
                {
                    #region Delete extra Orc fort Addon
                    List<Point3D> list = new List<Point3D>() {
                        new Point3D(2075, 1243, 0), // the fort itself
                        new Point3D(2077, 1234, 0), // door 1
                        new Point3D(2078, 1234, 0), // door 2
                        new Point3D(2056, 1251, 5), // door 3
                        new Point3D(2065, 1251, 5), // door 4
                        new Point3D(2065, 1250, 5), // door 5
                        new Point3D(2062, 1239, 0), // door 6
                        new Point3D(2062, 1238, 0), // door 7
                        new Point3D(2066, 1235, 5), // door 8
                        new Point3D(2068, 1231, 5), // door 9
                        new Point3D(2094, 1234, 0), // orc camp
                        new Point3D(2108, 1227, 7), // cave left 1
                        new Point3D(2108, 1227, 7), // cave left 2
                        new Point3D(2109, 1227, 7),  // cave right
                        new Point3D(2108, 1228, 25), // torch
                        new Point3D(2108, 1228, 0),  // teleporter
                    };
                    foreach (var px in list)
                    {
                        Item fort = (Item)FindOneItemAt(px, Map.Felucca, typeof(Item), 2, false);
                        if (fort != null)
                        {
                            fort.Delete();
                            patched++;
                        }
                    }
                    #endregion Delete extra Orc fort Addon

                    {
                        // (5334, 2004, 0) // Add teleporter 
                        Point3D px = new Point3D(5334, 2004, 0);
                        Teleporter tele = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
                        if (tele == null)
                        {
                            tele = new Teleporter();
                            tele.PointDest = new Point3D(6126, 1411, 15);
                            tele.MoveToWorld(px, Map.Felucca);
                            patched++;
                        }
                    }

                    {
                        list = new()
                        {
                        new Point3D(6125, 1411, 15), // teleporter
                        new Point3D(6126, 1411, 15), // teleporter
                        new Point3D(6127, 1411, 15), // teleporter
                        };
                        foreach (var px in list)
                        {
                            Teleporter tele = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
                            if (tele != null)
                            {
                                tele.PointDest = new Point3D(5334, 2004, 0);
                                patched++;
                            }
                        }
                    }

                    {
                        // (6141, 1430, 4) // repoint teleporter 
                        Point3D px = new Point3D(6141, 1430, 4);
                        Teleporter tele = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
                        if (tele != null)
                        {
                            tele.PointDest = new Point3D(6138, 1409, 2);
                            patched++;
                        }
                    }

                    {
                        // (6140, 1430, 4) // repoint teleporter 
                        Point3D px = new Point3D(6140, 1430, 4);
                        Teleporter tele = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
                        if (tele != null)
                        {
                            tele.PointDest = new Point3D(6137, 1409, 2);
                            patched++;
                        }
                    }

                    {
                        // (6137, 1409, 2) // repoint teleporter 
                        Point3D px = new Point3D(6137, 1409, 2);
                        Teleporter tele = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
                        if (tele != null)
                        {
                            tele.PointDest = new Point3D(6140, 1430, 4);
                            patched++;
                        }
                    }

                    {
                        // (6138, 1409, 2) // repoint teleporter 
                        Point3D px = new Point3D(6138, 1409, 2);
                        Teleporter tele = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
                        if (tele != null)
                        {
                            tele.PointDest = new Point3D(6141, 1430, 4);
                            patched++;
                        }
                    }
                }
                #endregion Orc fort stuffs

                #region PatchRegionFlags
                if (AllShards())
                {
                    List<StaticRegion> allSaveRegions = new List<StaticRegion>(StaticRegion.XmlDatabase);
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(StaticRegion.DataFileName);
                    XmlElement xmlRoot = xmlDoc["StaticRegions"];
                    foreach (XmlElement xmlElem in xmlRoot.GetElementsByTagName("region"))
                    {
                        if (xmlElem.GetAttribute("type") != "StaticRegion")
                            continue;
                        StaticRegion dataRegion = new StaticRegion();
                        dataRegion.Load(xmlElem);
                        int indexOf = allSaveRegions.FindIndex(r => r.Map == dataRegion.Map && r.Name.ToLower() == dataRegion.Name.ToLower());
                        if (indexOf != -1)
                        {
                            StaticRegion saveRegion = allSaveRegions[indexOf];
                            saveRegion.EnableHousing = dataRegion.EnableHousing;
                            saveRegion.EnableStuckMenu = dataRegion.EnableStuckMenu;
                            saveRegion.CanUsePotions = dataRegion.CanUsePotions;
                            saveRegion.CanResurrect = dataRegion.CanResurrect;
                            saveRegion.EnableMusic = dataRegion.EnableMusic;
                            allSaveRegions.RemoveAt(indexOf);
                            patched++;
                        }
                    }

                    /*      Yoar � Today at 2:18 PM
                            Oh and Vesper cemetery is missing a region altogether, But I remember it was possible to place there 
                            Might be a good idea to add a region there
                     */
                }
                #endregion PatchRegionFlags

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV87
        #region PatchV88
        private static int? PatchV88(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region PatchOracles
                if (SiegeStyleShards())
                {
                    foreach (Mobile m in Server.World.Mobiles.Values)
                    {
                        if (m is MoonGateWizard)
                        {
                            MoonGateWizard mgw = (MoonGateWizard)m;
                            bool hasPublicMoongate = false;
                            foreach (Item item in mgw.GetItemsInRange(7))
                            {
                                if (item is PublicMoongate)
                                {
                                    hasPublicMoongate = true;
                                    break;
                                }
                            }
                            if (hasPublicMoongate)
                            {
                                mgw.DestinationFlags = OracleFlag.Moongate; // we'll only gate to public moongates
                                mgw.UsePublicMoongates = true; // we'll put our gate on top of the public moongate
                                mgw.Cost = 150; // it costs 150 gp to purchase a gate
                                mgw.DangerCost = 0; // we're not going to dangerous locations, so this value is not used
                                mgw.RequiresFunds = true; // if they can't pay, we won't cast a gate
                                mgw.TwoWay = false; // our gate doesn't work from both directions
                                patched++;
                            }
                        }
                    }
                }
                #endregion PatchOracles
                #region Port Key 
                if (SiegeStyleShards())
                {
                    Point3D px = new Point3D(5863, 533, 15);
                    PortalManager pm = (PortalManager)FindOneItemAt(px, Map.Felucca, typeof(PortalManager), 2, false);
                    if (pm == null)
                    {
                        pm = new();
                        pm.Running = true;
                        pm.MoveToWorld(px, Map.Felucca);
                        patched++;
                    }
                }
                #endregion Port Key 

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV88
        #region PatchV89
        private static int? PatchV89(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Respawn camp spawners
                if (AllShards())
                {
                    foreach (Item item in Server.World.Items.Values)
                        if (item is Spawner sp && sp.Running)
                            if (sp.Spawns(typeof(BaseCamp)))
                            {
                                sp.RemoveObjects();
                                sp.Respawn();
                                patched++;
                            }
                }
                #endregion Respawn camp spawners

                #region Destard Healer(s)
                if (Core.RuleSets.SiegeRules())
                {
                    Spawner spawner = null;
                    Point3D px = new Point3D(1183, 2662, 5);
                    spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner == null)
                    {
                        spawner = new();
                        spawner.Distro = SpawnerModeAttribs.ModeMulti;
                        spawner.ObjectNamesRaw.Add("EvilWanderingHealer");
                        spawner.ObjectNamesRaw.Add("WanderingHealer");
                        spawner.Counts = "1,1";
                        spawner.HomeRange = 40;
                        spawner.WalkRange = -1;
                        spawner.Concentric = true;
                        spawner.MoveToWorld(px, Map.Felucca);
                        spawner.Running = true;
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                }
                #endregion

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV89
        #region PatchV90
        private static int? PatchV90(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Patch 'blocking' bone piles at Dungeon Wrong entrance
                if (AllShards())
                {
                    List<Point3D> list = new() { new Point3D(2041, 217, 16), new Point3D(2040, 237, 12) };
                    foreach (var px in list)
                    {
                        Static @static = (Static)FindOneItemAt(px, Map.Felucca, ItemID: 8600, 2, false);
                        if (@static == null)
                        {
                            @static = new(8600);
                            @static.MoveToWorld(px, Map.Felucca);
                            patched++;
                        }
                    }
                }
                #endregion Patch 'blocking' bone piles at Dungeon Wrong entrance

                #region Add Ostards
                if (SiegeStyleShards())
                {   // Desert Ostards
                    Point3D px = new Point3D(1929, 866, -1);
                    Spawner spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner == null)
                    {
                        AddSpawner(px, new List<string>() { "DesertOstard" }, coreSpawn: true, spawner: ref spawner, count: 12, homeRange: 60);
                        spawner.Concentric = true;
                        patched++;
                    }

                    // Forest Ostards
                    px = new Point3D(704, 1057, 0);
                    spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner == null)
                    {
                        AddSpawner(px, new List<string>() { "ForestOstard" }, coreSpawn: true, spawner: ref spawner, count: 12, homeRange: 60);
                        spawner.Concentric = true;
                        patched++;
                    }

                    // Frenzied Ostards
                    px = new Point3D(2648, 2267, 15);
                    spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner == null)
                    {
                        AddSpawner(px, new List<string>() { "FrenziedOstard" }, coreSpawn: true, spawner: ref spawner, count: 12, homeRange: 60);
                        spawner.Concentric = true;
                        patched++;
                    }
                }
                #endregion Add Ostards

                #region Ridable Llamas!
                if (SiegeStyleShards())
                {
                    Point3D px = new Point3D(4418, 3683, 0);
                    Spawner spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner == null)
                    {
                        AddSpawner(px, new List<string>() { "RidableLlama" }, coreSpawn: true, spawner: ref spawner, count: 6, homeRange: 50);
                        spawner.Concentric = true;
                        patched++;
                    }
                }
                #endregion Ridable Llamas!

                #region Respawn SavageRiders
                if (SiegeStyleShards())
                {
                    foreach (Item item in Server.World.Items.Values)
                        if (item is Spawner spawner && spawner.Running && spawner.Spawns(typeof(SavageRider)))
                            if (ScheduleRespawn(spawner))
                                patched++;

                }
                #endregion Respawn SavageRiders

                #region Yoar's Spawner Updates
                if (SiegeStyleShards())
                {
                    /*
                     * Moved from (1042, 1394, 0) to (1033, 1403, 0)
                     * Changed spawn name from "orccamp:ratmancamp" to "orccamp"
                     * Changed HomeRange from 1 to 0
                     */
                    Spawner spawner = null;
                    if (MoveSpawner(old_location: new Point3D(1042, 1394, 0), new_location: new Point3D(1033, 1403, 0), ref spawner))
                    {
                        spawner.ObjectNamesRaw = new ArrayList() { "OrcCamp" };
                        spawner.HomeRange = 0;
                        patched++;
                    }

                    /*
                     * 1058, 1375, 0 - replace with an orc spawner in the center of the fort
                     * 1059, 1376, 0 - this is a misc. harmful mob spawner inside the orc fort
                     */
                    List<Point3D> list = new() { new Point3D(1058, 1375, 0), new Point3D(1059, 1376, 0) };
                    foreach (var px in list)
                        if (DisableSpawner(px))
                            patched++;


                }
                #endregion Yoar's Spawner Updates

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV90
        #region PatchV91
        private static int? PatchV91(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Dungeon Despise
                if (SiegeStyleShards())
                {
                    Spawner spawner = null;
                    List<Point3D> list = null;

                    // enable these Pub15 spawners
                    list = new()
                    {   
                        // level 1
                        new Point3D(5452, 548, 56),
                        // level 2
                        new Point3D(5498, 672, 20), new Point3D(5473, 668, 20), new Point3D(5458, 685, 20), new Point3D(5441, 680, 20),
                        new Point3D(5397, 661, 20), new Point3D(5405, 691, 20), new Point3D(5388, 703, 15), new Point3D(5386, 689, 20),
                        new Point3D(5408, 724, 15), new Point3D(5422, 747, 5), new Point3D(5399, 753, 5), new Point3D(5456, 709, 15),
                        new Point3D(5442, 626, 30), new Point3D(5465, 611, 45), new Point3D(5499, 611, 47), new Point3D(5500, 563, 65),
                        new Point3D(5468, 531, 60), new Point3D(5454, 525, 61), new Point3D(5427, 531, 60), new Point3D(5473, 741, 5),
                        new Point3D(5504, 750, 5),new Point3D(5506, 714, 15), 
                        // level 3
                        new Point3D(5397, 859, 45), new Point3D(5396, 849, 45), new Point3D(5388, 851, 55), new Point3D(5395, 820, 60),
                        new Point3D(5399, 786, 65), new Point3D(5428, 781, 60), new Point3D(5456, 787, 60), new Point3D(5481, 802, 60),
                        new Point3D(5517, 815, 60), new Point3D(5518, 815, 60),new Point3D(5567, 787, 60), new Point3D(5610, 824, 60),
                        new Point3D(5518, 851, 45), new Point3D(5537, 887, 30), new Point3D(5561, 906, 30), new Point3D(5581, 893, 30),

                    };
                    foreach (var px in list)
                        if (EnableSpawner(px, ref spawner))
                            patched++;

                    // add a dead miners
                    list = new()
                    {
                        new Point3D(5421, 616, 45), new Point3D(5423, 545, 60), new Point3D(5402, 570, 60),
                    };
                    foreach (var px in list)
                        if (AddSpawner(px, new List<string>() { "DeadMiner" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 0))
                            patched++;

                    // cleanup these deco corpses, level 2 entrance .. just too much
                    list = new()
                    {
                        new Point3D(5511, 673, 20), new Point3D(5517, 673, 20), new Point3D(5519, 673, 20)
                    };
                    foreach (var px in list)
                    {
                        Corpse corpse = (Corpse)FindOneItemAt(px, Map.Felucca, typeof(Corpse), 2, false);
                        if (corpse != null)
                        {
                            corpse.Delete();
                            patched++;
                        }
                    }
                }
                #endregion Dungeon Despise

                #region Stone Crafter
                if (SiegeStyleShards())
                {
                    Point3D px = new Point3D(2499, 431, 15);
                    Spawner spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner == null)
                    {
                        AddSpawner(px, new List<string>() { "StoneCrafter" }, coreSpawn: true, spawner: ref spawner, count: 1, homeRange: 4);
                        patched++;
                    }
                }
                #endregion Stone Crafter

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV91
        #region PatchV92
        private static int? PatchV92(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Port Music Library from Angel Island to Siege
                if (SiegeStyleShards())
                {
                    // from Angel Island
                    Dictionary<int, Nuke.ExpInfo> table = new() {
                    { 93233347, new Nuke.ExpInfo("Dorundain",2000) },
                    { 662835438, new Nuke.ExpInfo("Wagdog",2000) },
                    { -845254843, new Nuke.ExpInfo("Wagdog",0) },
                    { -1035873931, new Nuke.ExpInfo("Dorundain",5000) },
                    { -1721352356, new Nuke.ExpInfo("Wagdog",500) },
                    { -638482194, new Nuke.ExpInfo("Dorundain",5000) },
                    { -1733046116, new Nuke.ExpInfo("random1",0) },
                    { -1308563267, new Nuke.ExpInfo("Bravata",200) },
                    { 1701907019, new Nuke.ExpInfo("Bravata",1000) },
                    { -1248138449, new Nuke.ExpInfo("Chappelle4life3",6000) },
                    { 103000142, new Nuke.ExpInfo("Wagdog",0) },
                    { 2116081743, new Nuke.ExpInfo("Dorundain",1000) },
                    { 1174944509, new Nuke.ExpInfo("Bravata",5000) },
                    { 681617492, new Nuke.ExpInfo("Dorundain",5000) },
                    { -187482132, new Nuke.ExpInfo("Chappelle4life3",500) },
                    { 992232335, new Nuke.ExpInfo("Dorundain",5000) },
                    { -1366692645, new Nuke.ExpInfo("Angel2",0) },
                    { 903223787, new Nuke.ExpInfo("Wagdog",2000) },
                    { 220431633, new Nuke.ExpInfo("Dorundain",0) },
                    { -1139706317, new Nuke.ExpInfo("Dorundain",0) },
                    { 1812409723, new Nuke.ExpInfo("Dorundain",2000) },
                    { -440882563, new Nuke.ExpInfo("Dorundain",0) },
                    { 27770559, new Nuke.ExpInfo("Dorundain",1000) },
                    { 644215459, new Nuke.ExpInfo("Dorundain",2000) },
                    { 2083146414, new Nuke.ExpInfo("Dorundain",1000) },
                    { 1784616072, new Nuke.ExpInfo("Dorundain",2000) },
                    { -832981173, new Nuke.ExpInfo("Dorundain",3000) },
                };

                    LogHelper logger = new LogHelper("RolledUpSheetMusicRepair.log", true, true, true);
                    foreach (KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo> kvp in MusicLibrary.Repository)
                    {
                        if (table.ContainsKey(kvp.Key.HashCode) && (kvp.Key.Owner == null || kvp.Key.Owner.Account == null))
                        {
                            Mobile owner = null;
                            Server.Accounting.Account acct = null;
                            if (GetOwnerInfo(table[kvp.Key.HashCode], out owner, out acct))
                            {
                                kvp.Key.Owner = owner;
                                kvp.Value.Price = table[kvp.Key.HashCode].Price;
                                kvp.Value.Owner = owner;
                                logger.Log(LogType.Text,
                                string.Format("music track {0} repaired.", kvp.ToString()));
                                patched++;
                            }
                        }
                    }
                    logger.Finish();
                }
                #endregion Port Music Library from Angel Island to Siege

                #region Make spawners permanent
                if (SiegeStyleShards())
                {
                    Point3D px = new Point3D(1168, 2959, 0);
                    Spawner spawner = null;
                    if (MakeSpawnerPermanent(px, ref spawner))
                    {
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                }
                #endregion Make spawners permanent

                #region Make teleporters permanent
                if (SiegeStyleShards())
                {
                    List<Point3D> list = new() {new Point3D(6027, 1452, 0),new Point3D(6027, 1453, 0),
                            new Point3D(5171, 1987, 2), new Point3D(5171, 1988, 2)};
                    Teleporter tele = null;
                    foreach (var px in list)
                        if (MakeTeleporterPermanent(px, ref tele))
                            patched++;
                }
                #endregion Make teleporters permanent

                #region Savage Paint
                if (SiegeStyleShards())
                {
                    // Remove old Cauldron
                    Point3D px = new Point3D(1175, 2960, 0);
                    foreach (Item item in FindItemsAt(px, Map.Felucca, lenientZ: 8))
                    {
                        if (item.ItemID == 0x975 /*Cauldron*/ || item.ItemID == 0x970 /*bubbling liquid*/)
                        {
                            item.Delete();
                            patched++;
                        }
                    }

                    // TribalPaintCauldron
                    new TribalPaintCauldron().MoveToWorld(px, Map.Felucca);
                    patched++;
                }
                #endregion Savage Paint

                #region Remove extra pentagram
                if (AllShards())
                {

                    IPooledEnumerable eable = Map.Felucca.GetItemsInRange(new Point3D(5692, 1346, 0), 5);
                    foreach (Item item in eable)
                        if (item is Spawner || item is BaseAddon || item is BaseContainer ||
                            item is StackedSpawnerConsole || item is Teleporter || item is BaseDoor ||
                            item is AddonComponent)
                            continue;
                        else
                        {
                            item.Delete();
                            patched++;
                        }
                    eable.Free();
                }
                #endregion Remove extra pentagram

                #region Moonglow Cemetery light level
                if (AllShards())
                {
                    for (int i = CustomRegionControl.Instances.Count - 1; i >= 0; i--)
                    {
                        CustomRegionControl crc = CustomRegionControl.Instances[i];
                        CustomRegion cr = crc.CustomRegion;
                        // note: Our spelling of Cemetery is incorrect
                        if (cr.Name.Equals("Moonglow Cemetary"))
                        {
                            cr.LightLevel = -1;
                            patched++;
                        }
                    }
                }
                #endregion Moonglow Cemetery light level

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV92
        #region PatchV93
        private static int? PatchV93(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Patch Dungeon Chest Levels
                if (SiegeStyleShards())
                {
                    foreach (Item item in Server.World.Items.Values)
                        if (item is Spawner spawner && (spawner.Spawns("L5TreasureChest") || spawner.Spawns("L6TreasureChest")))
                        {
                            List<string> list = new List<string>(spawner.ObjectNamesRaw.Cast<string>().ToList());
                            list = list.Select(s => s.Replace("L5TreasureChest", "L4TreasureChest")).ToList();
                            list = list.Select(s => s.Replace("L6TreasureChest", "L4TreasureChest")).ToList();
                            spawner.ObjectNamesRaw = new ArrayList(list.Select(s => s.Replace("L6TreasureChest", "L4TreasureChest")).ToList());
                            patched++;
                        }
                }
                #endregion Patch Dungeon Chest Levels

                #region Patch Spawners, allow pets
                if (SiegeStyleShards())
                {
                    List<Point3D> list = new() { new Point3D(1987, 2062, -40), new Point3D(1988, 2062, -40), new Point3D(1989, 2062, -40) };
                    foreach (var px in list)
                    {
                        Teleporter tele = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
                        if (tele != null)
                        {
                            tele.Pets = true;
                            patched++;
                        }
                    }

                }
                #endregion Patch Spawners, allow pets

                #region Keys On The Ocean
                if (AllShards())
                {
                    bool info = false;
                    bool remove = true;

                    int iChecked = 0;
                    int iFound_felucca = 0;
                    int iFound_other = 0;
                    int iRepaired = 0;
                    List<Key> remove_list = new List<Key>();
                    List<Key> repaired_list = new List<Key>();
                    List<KeyRing> keyRings = new List<KeyRing>();
                    foreach (Item item in Server.World.Items.Values)
                        if (item is KeyRing)
                            keyRings.Add(item as KeyRing);

                    foreach (Item item in Server.World.Items.Values)
                    {
                        iChecked++;
                        if (item is Key)
                        {
                            Key key = item as Key;
                            if (key.Parent == null)
                            {
                                if (BaseHouse.FindHouseAt(key) == null)
                                {
                                    /*if (key.Map == Map.Internal && key.IsIntMapStorage)
                                    {
                                        ; // ignore, already on a keyring
                                    }
                                    else */
                                    {
                                        if (key.Map == Map.Felucca)
                                            iFound_felucca++;
                                        else
                                            iFound_other++;

                                        if (remove)
                                            remove_list.Add(key);
                                    }
                                }
                                // else, in a house. ignore
                            }
                            // else, has a parent. ignore
                        }
                        // else not a key
                    }

                    // repair
                    foreach (Key key in remove_list)
                        foreach (var keyring in keyRings)
                            if (keyring.IsKeyOnRing(key.KeyValue))
                            {
                                keyring.RemoveKey(key.KeyValue);
                                keyring.AddKey(key);
                                repaired_list.Add(key);
                                iRepaired++;
                                patched++;
                                break;
                            }

                    foreach (Key key in repaired_list)
                        remove_list.Remove(key);

                    if (remove)
                        foreach (Key key in remove_list)
                        {
                            key.Delete();
                            patched++;
                        }

                    LogHelper logger = new LogHelper("Keys On The Ocean.log", false, true);
                    logger.Log(LogType.Text, string.Format("{3} {0} orphaned keys, {1} of which are in Felucca, and {2} of which are on other maps.",
                        (iFound_other + iFound_felucca) - (info ? 0 : iRepaired), iFound_felucca, iFound_other, info ? "Found" : "Removed"));

                    logger.Log(LogType.Text, string.Format("{0} key/keyrings repaired.", iRepaired));
                    logger.Finish();
                }
                #endregion

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV93
        #region PatchV94
        private static int? PatchV94(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Prison Quest
                var px = new Point3D(311, 786, 0);
                Teleporter tele = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
                if (tele != null)
                {
                    tele.Delete();
                    patched++;
                }

                // add the PrisonLiaisonOfficer
                px = new Point3D(802, 1059, 0);
                Spawner spawner = null;
                if (AddSpawner(px, new List<string>() { "PrisonLiaisonOfficer" }, coreSpawn: false, ref spawner, count: 1, homeRange: 0, force: true))
                {
                    spawner.MobileDirection = Direction.East;
                    patched++;
                }

                // add the rare treasure chest
                px = new Point3D(222, 775, 5);
                TriggerSpawner trigger_spawner = null;
                if (AddTriggerSpawner(px, 0xCE1EB734, new List<string>() { "RareTreasureChest" }, coreSpawn: false, ref trigger_spawner, count: 1, homeRange: 0, force: true))
                {
                    patched++;
                }
                #endregion Prison Quest

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV94
        #region PatchV95
        private static int? PatchV95(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region New Machine Hash Algo - wipe all existing
                if (AllShards())
                {
                    EchoOut("Updating Global Account Database...", ConsoleColor.Yellow);
                    foreach (Server.Accounting.Account acct in Server.Accounting.Accounts.Table.Values)
                    {
                        if (acct == null)
                            continue;

                        patched += acct.Machines.Count;
                        acct.ClearMachines();
                    }
                    EchoOut("Updating of Global Account Database complete", ConsoleColor.Yellow);
                }
                #endregion New Machine Hash Algo - wipe all existing

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV95
        #region PatchV96
        private static int? PatchV96(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Enable Sea Gypsies
                if (SiegeStyleShards())
                {
                    foreach (Item item in Server.World.Items.Values)
                        if (item is Spawner spawner && spawner.Running == false && spawner.Spawns("SeaGypsy"))
                            if (EnableSpawner(ref spawner))
                                patched++;
                }
                #endregion Enable Sea Gypsies

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV96
        #region PatchV97
        private static int? PatchV97(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Update Goodwill Crates
                if (AllShards())
                {
                    List<BaseContainer> list = new();
                    IPooledEnumerable eable = Map.Felucca.GetItemsInRange(new Point3D(1424, 1703, 7), 3);
                    foreach (Item item in eable)
                        if (item is BaseContainer bc)
                            list.Add(bc);

                    eable.Free();

                    foreach (var cont in list)
                        if (cont is SmallCrate sc)
                        {
                            GoodwillAsshat.SmallGoodwillCrate sbtc = new();
                            List<Item> items = new();
                            foreach (Item item in sc.Items)
                                items.Add(item);

                            foreach (Item item in items)
                            {
                                sc.RemoveItem(item);
                                sbtc.AddItem(item);
                            }

                            sbtc.ItemID = sc.ItemID;    // get the facing right
                            sbtc.Location = sc.Location;
                            sbtc.Map = sc.Map;
                            sbtc.Movable = sc.Movable;
                            sbtc.Name = sc.Name;
                            sc.Delete();
                            patched++;
                        }
                        else if (cont is MediumCrate mc)
                        {
                            GoodwillAsshat.SmallGoodwillCrate mbtc = new();
                            List<Item> items = new();
                            foreach (Item item in mc.Items)
                                items.Add(item);

                            foreach (Item item in items)
                            {
                                mc.RemoveItem(item);
                                mbtc.AddItem(item);
                            }

                            mbtc.ItemID = mc.ItemID;    // get the facing right
                            mbtc.Location = mc.Location;
                            mbtc.Map = mc.Map;
                            mbtc.Movable = mc.Movable;
                            mbtc.Name = mc.Name;
                            mc.Delete();
                            patched++;
                        }
                        else if (cont is LargeCrate lc)
                        {
                            GoodwillAsshat.SmallGoodwillCrate lbtc = new();
                            List<Item> items = new();
                            foreach (Item item in lc.Items)
                                items.Add(item);

                            foreach (Item item in items)
                            {
                                lc.RemoveItem(item);
                                lbtc.AddItem(item);
                            }

                            lbtc.ItemID = lc.ItemID;    // get the facing right
                            lbtc.Location = lc.Location;
                            lbtc.Map = lc.Map;
                            lbtc.Movable = lc.Movable;
                            lbtc.Name = lc.Name;
                            lc.Delete();
                            patched++;
                        }
                }
                #endregion Update Goodwill Crates

                #region Fruit Baskets
                if (AllShards())
                {
                    Point3D Yew = new Point3D(286, 986, 6);
                    foreach (Item item in Server.World.Items.Values)
                        if (item is Spawner spawner && spawner.Running == true && spawner.Spawns("FruitBasket"))
                        {
                            if (spawner.Location != Yew)
                                spawner.SetProp = "movable false";

                            spawner.Respawn();
                            patched++;
                        }
                }
                #endregion Fruit Baskets

                #region Total Respawn (new decay strategy)
                if (AllShards())
                    patched += ScheduleRespawn();
                #endregion Total Respawn (new decay strategy)

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV97
        #region PatchV98
        private static int? PatchV98(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Move Spawner
                if (SiegeStyleShards())
                {// (2990, 845, 0) => (2994, 839, 0)
                    Spawner spawner = null;
                    if (MoveSpawner(new Point3D(2990, 845, 0), new Point3D(2994, 839, 0), ref spawner))
                    {
                        spawner.RemoveObjects();
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                }
                #endregion Move Spawner

                #region Add Rares
                if (AllShards())
                {
                    #region delete existing deco
                    List<Point3D> list = new() {
                        new Point3D(1847, 2714, 16),new Point3D(3010, 3352, 21),
                    };

                    foreach (var px in list)
                        if (DeleteItem(px))
                            patched++;

                    #endregion delete existing deco

                    #region Add Crystalline Powder
                    list = new() {
                        new Point3D(5305, 87, 21), new Point3D(3010, 3352, 21), new Point3D(1847, 2714, 16)
                    };

                    LogHelper logger = new LogHelper(LogHelper.DataFolder("Rares Spawners.log"), false, true);
                    foreach (var px in list)
                    {
                        RaresSpawner spawner = new RaresSpawner();
                        spawner.MustSteal = true;
                        spawner.RareType = Loot.RareType.CrystallinePowder;
                        spawner.MinDelay = TimeSpan.FromHours(12);
                        spawner.MaxDelay = TimeSpan.FromHours(24);
                        spawner.HomeRange = 0;
                        spawner.MoveToWorld(px, Map.Felucca);
                        spawner.ScheduleRespawn = true;
                        patched++;

                        logger.Log(LogType.Item, spawner);
                        logger.Log(string.Format("MustSteal:{0}, RareType:{1}, MinDelay:{2}, MaxDelay:{3}, HomeRange:{4}",
                            spawner.MustSteal,
                            spawner.RareType,
                            spawner.MinDelay,
                            spawner.MaxDelay,
                            spawner.HomeRange
                            ));
                    }
                    logger.Finish();

                    #endregion Add Crystalline Powder

                    #region Add Scarecrows
                    list = new() {
                        new Point3D(1222, 1722, 0), new Point3D(813, 2163, 0)
                    };

                    logger = new LogHelper(LogHelper.DataFolder("Rares Spawners.log"), false, true);
                    foreach (var px in list)
                    {
                        RaresSpawner spawner = new RaresSpawner();
                        spawner.MustSteal = true;
                        spawner.RareType = Loot.RareType.ScarecrowEast;
                        spawner.MinDelay = TimeSpan.FromHours(12);
                        spawner.MaxDelay = TimeSpan.FromHours(24);
                        spawner.HomeRange = 0;
                        spawner.MoveToWorld(px, Map.Felucca);
                        spawner.ScheduleRespawn = true;
                        patched++;

                        logger.Log(LogType.Item, spawner);
                        logger.Log(string.Format("MustSteal:{0}, RareType:{1}, MinDelay:{2}, MaxDelay:{3}, HomeRange:{4}",
                            spawner.MustSteal,
                            spawner.RareType,
                            spawner.MinDelay,
                            spawner.MaxDelay,
                            spawner.HomeRange
                            ));
                    }
                    logger.Finish();

                    // add some crows
                    foreach (var px in list)
                    {
                        Spawner spawner = new Spawner();
                        spawner.ObjectNamesRaw.Add("crow");
                        spawner.MinDelay = TimeSpan.FromMinutes(5);
                        spawner.MaxDelay = TimeSpan.FromMinutes(15);
                        spawner.Count = 3;
                        spawner.HomeRange = 10;
                        spawner.MoveToWorld(new Point3D(px.X, px.Y + 1, px.Z), Map.Felucca);
                        spawner.ScheduleRespawn = true;
                        patched++;

                        // add some vegetables if needed
                        bool foundFood = false;
                        IPooledEnumerable eable = Map.Felucca.GetItemsInRange(px, 20);
                        foreach (Item item in eable)
                            if (item is Food food)
                            {
                                foundFood = true;
                                break;
                            }
                        eable.Free();

                        if (foundFood == false)
                        {
                            spawner = new Spawner();
                            spawner.ObjectNamesRaw.Add("pumpkin");
                            spawner.ObjectNamesRaw.Add("carrot");
                            spawner.MinDelay = TimeSpan.FromMinutes(5);
                            spawner.MaxDelay = TimeSpan.FromMinutes(10);
                            spawner.Count = 12;
                            spawner.HomeRange = 10;
                            spawner.MoveToWorld(new Point3D(px.X + 1, px.Y, px.Z), Map.Felucca);
                            spawner.ScheduleRespawn = true;
                            patched++;
                        }
                    }
                    #endregion Add Scarecrows

                    #region Add Uncut Cloth
                    list = new() {
                        new Point3D(3762, 1268, 6), new Point3D(3665, 2576, 6)
                    };

                    logger = new LogHelper(LogHelper.DataFolder("Rares Spawners.log"), false, true);
                    foreach (var px in list)
                    {
                        RaresSpawner spawner = new RaresSpawner();
                        spawner.MustSteal = true;
                        spawner.RareType = Loot.RareType.RareUncutCloth;
                        spawner.MinDelay = TimeSpan.FromHours(12);
                        spawner.MaxDelay = TimeSpan.FromHours(24);
                        spawner.HomeRange = 0;
                        spawner.MoveToWorld(px, Map.Felucca);
                        spawner.ScheduleRespawn = true;
                        patched++;

                        logger.Log(LogType.Item, spawner);
                        logger.Log(string.Format("MustSteal:{0}, RareType:{1}, MinDelay:{2}, MaxDelay:{3}, HomeRange:{4}",
                            spawner.MustSteal,
                            spawner.RareType,
                            spawner.MinDelay,
                            spawner.MaxDelay,
                            spawner.HomeRange
                            ));
                    }
                    logger.Finish();

                    #endregion Add Uncut Cloth

                    #region Add Bolt of Cloth
                    list = new() {
                        new Point3D(4459, 1068, 0), new Point3D(2953, 619, 0)
                    };

                    logger = new LogHelper(LogHelper.DataFolder("Rares Spawners.log"), false, true);
                    foreach (var px in list)
                    {
                        RaresSpawner spawner = new RaresSpawner();
                        spawner.MustSteal = true;
                        spawner.RareType = Loot.RareType.RareBoltOfCloth;
                        spawner.MinDelay = TimeSpan.FromHours(12);
                        spawner.MaxDelay = TimeSpan.FromHours(24);
                        spawner.HomeRange = 0;
                        spawner.MoveToWorld(px, Map.Felucca);
                        spawner.ScheduleRespawn = true;
                        patched++;

                        logger.Log(LogType.Item, spawner);
                        logger.Log(string.Format("MustSteal:{0}, RareType:{1}, MinDelay:{2}, MaxDelay:{3}, HomeRange:{4}",
                            spawner.MustSteal,
                            spawner.RareType,
                            spawner.MinDelay,
                            spawner.MaxDelay,
                            spawner.HomeRange
                            ));
                    }
                    logger.Finish();

                    #endregion Add Bolt of Cloth
                }
                #endregion Add Rares

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV98
        #region PatchV99
        private static int? PatchV99(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Add Rare FillableContainers
                if (AllShards())
                {
                    LogHelper logger = new LogHelper(LogHelper.DataFolder("Rares Spawners.log"), false, true);
                    foreach (Item item in Server.World.Items.Values)
                    {
                        if (item is FillableContainer fc)
                        {   // if it is a 'gold box'
                            if (item.ItemID == 0xE80 || item.ItemID == 0x9A8)
                            {
                                // make sure there are no other 'Rare' gold boxes around
                                IPooledEnumerable eable = Map.Felucca.GetItemsInRange(item.Location, 20);
                                foreach (Item thing in eable)
                                    if (thing is FillableContainer fct && (thing.ItemID == 0xE80 || thing.ItemID == 0x9A8))
                                        if (fct.ContentType == FillableContentType.Rare)
                                        {
                                            eable.Free();
                                            // we found another FillableContainer spawning a rare, skip to the next item
                                            goto skip;
                                        }
                                eable.Free();

                                // look for base vendors nearby
                                eable = Map.Felucca.GetMobilesInRange(item.Location, 10);
                                foreach (Mobile vendor in eable)
                                    // needs to be around a vendor (to call guards)
                                    if (vendor is BaseVendor)
                                    {
                                        if (fc.ContentType == FillableContentType.None)
                                        {
                                            fc.ContentType = FillableContentType.Rare;
                                            fc.MinDelay = TimeSpan.FromHours(12);
                                            fc.MaxDelay = TimeSpan.FromHours(24);
                                            fc.MustSteal = true;
                                            logger.Log(LogType.Item, fc);
                                            logger.Log(string.Format("MustSteal:{0}, ContentType:{1}, MinDelay:{2}, MaxDelay:{3}",
                                                fc.MustSteal,
                                                fc.ContentType,
                                                fc.MinDelay,
                                                fc.MaxDelay
                                                ));
                                            patched++;
                                        }
                                        break;
                                    }
                                eable.Free();

                            skip:
                                continue;
                            }
                        }
                    }
                    logger.Finish();
                }
                #endregion Add Rare FillableContainers

                #region Respawn all Rares
                if (AllShards())
                {
                    patched += RespawnRares();
                }
                #endregion Respawn all Rares

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV99
        #region PatchV100
        private static int? PatchV100(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Update Moongate Wizard's Spawners
                if (SiegeStyleShards())
                {
                    // DestinationFlags Moongate
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.Running == true && spawner.Spawns("MoonGateWizard"))
                        {
                            spawner.SetProp = "DestinationFlags Moongate";
                            spawner.Respawn();
                            patched++;
                        }
                }
                #endregion Update Moongate Wizard's Spawners

                #region Nerun's Distro spawners to have correct override values
                if (SiegeStyleShards())
                {
                    List<Spawner> list = new();
                    foreach (Item item in Server.World.Items.Values)
                        if (item is Spawner spawner && spawner.Running && spawner.Distro == SpawnerModeAttribs.ModeNeruns)
                            if (spawner.MinDelay.TotalMinutes == 60 && spawner.MaxDelay.TotalMinutes == 80)
                                list.Add(spawner);

                    // Nerun's Distro file "Outdoors.map" has exactly 79 spawners that need this change.
                    //  Our simple enumeration above turned up exactly 79 such spawners. (No worry here about sucking in the wrong spawners)
                    foreach (Spawner spawner in list)
                        spawner.MaxDelay = TimeSpan.FromMinutes(60);

                    patched += list.Count;
                }
                #endregion Nerun's Distro spawners to have correct override values

                #region Increase Spawn Delay for our 'Gold Boxes'
                if (AllShards())
                {
                    foreach (Item item in Server.World.Items.Values)
                    {
                        if (item is FillableContainer fc && fc.ContentType == FillableContentType.Rare)
                        {   // double old delay
                            fc.MinDelay = TimeSpan.FromHours(12 * 2);
                            // triple old delay
                            fc.MaxDelay = TimeSpan.FromHours(24 * 3);
                            patched += 1;
                        }
                    }
                }
                #endregion Increase Spawn Delay for our 'Gold Boxes'

                #endregion End Implementation
                return patched;
            }
            return null;
        }
        #endregion PatchV100

        #region Helpers
        #region PatchV99
        public static int RespawnRares()
        {
            int count = 0;

            List<RaresSpawner> spawners = new();
            List<FillableContainer> fillableContainerList = new();
            foreach (Item item in Server.World.Items.Values)
                if (item is RaresSpawner s && s.Deleted == false && s.Running == true)
                    spawners.Add(s);
                else if (item is FillableContainer fc && fc.Deleted == false)
                    fillableContainerList.Add(fc);

            foreach (RaresSpawner sp in spawners)
            {
                if (sp.Running)
                {
                    count++;
                    sp.RemoveObjects();
                    sp.Respawn();
                }
            }

            foreach (var fc in fillableContainerList)
            {
                count++;
                fc.TotalRespawn = true;
            }

            return count;
        }
        #endregion PatchV99
        #region PatchV94
        private static bool AddTriggerSpawner(Point3D px, uint key, List<string> creatures, bool coreSpawn, ref TriggerSpawner spawner, int count = 1, int homeRange = 4, bool force = false)
        {
            spawner = (TriggerSpawner)FindOneItemAt(px, Map.Felucca, typeof(TriggerSpawner), 2, false);
            if (spawner == null || force == true)
            {
                spawner = new(key);

                // reclassify here as 'all shards'
                if (coreSpawn)
                    UpdateSpawnerDesignationaAndLog(spawner, coreSpawn: coreSpawn,
                        uOSpawnMap: "AISpecial", shardConfig: ShardConfig.AllShards);

                spawner.Count = count;
                spawner.HomeRange = homeRange;
                foreach (string mob in creatures)
                    spawner.ObjectNamesRaw.Add(mob);
                spawner.MoveToWorld(px, Map.Felucca);
            }

            return (spawner != null);
        }

        #endregion PatchV94
        #region PatchV92
        private static bool GetOwnerInfo(Nuke.ExpInfo info, out Mobile m, out Server.Accounting.Account acct)
        {
            m = null;
            acct = null;
            foreach (Server.Accounting.Account current in Server.Accounting.Accounts.Table.Values)
                if (current.Username.Equals(info.Username, StringComparison.OrdinalIgnoreCase))
                    for (int ix = 0; ix < 5; ix++)
                    {
                        Mobile @char = current[ix];
                        if (@char != null)
                        {
                            m = @char;
                            acct = current;
                            return true;
                        }
                    }

            return false;
        }
        #endregion PatchV92
        #region PatchV87
        private static int CountMoonGateWizards()
        {
            int count = 0;

            foreach (Item item in Server.World.Items.Values)
                if (item is Spawner spawner)
                    if (spawner.Spawns("MoonGateWizard") && spawner.Running)
                        count++;

            //foreach (Mobile m in World.Mobiles.Values)
            //if (m is MoonGateWizard)
            //count++;
            return count;
        }
        #endregion PatchV87
        #region PatchV86
        private static bool DisbleTeleporter(Point3D px)
        {
            Teleporter tele = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
            if (tele != null)
                tele.Running = false;

            return (tele != null);
        }
        private static int CountVendors(Region region)
        {
            if (region.Map == null)
                return 0;
            int count = 0;
            foreach (Rectangle3D rect in region.Coords)
            {
                Rectangle2D rect2D = new Rectangle2D(rect.Start, rect.End);
                foreach (Mobile m in region.Map.GetMobilesInBounds(rect2D))
                {
                    if (m is BaseVendor)
                    {
                        BaseVendor vendor = (BaseVendor)m;
                        if (vendor.IsActiveSeller && vendor.Spawner != null && region.Contains(vendor.Spawner.Location))
                            count++;
                    }
                }
            }
            return count;
        }
        private static readonly string[] m_TownNames = new string[]
        {
            "Britain",
            "Britannia Royal Zoo",
            "Cove",
            "Delucia",
            "Jhelom",
            "Magincia",
            "Minoc",
            "Moonglow",
            "Nujel'm",
            "Ocllo",
            "Papua",
            "Serpent's Hold",
            "Skara Brae",
            "Trinsic",
            "Vesper",
            "Wind",
            "Yew",
        };
        #endregion PatchV86
        #region PatchV81
        #region BritainFarmsPatch
        private static void BritainFarmsPatch()
        {
            StaticRegion britain = Region.FindByName("Britain", Map.Felucca) as StaticRegion;
            int indexOf = -1;
            for (int i = 0; i < britain.Coords.Count && indexOf == -1; i++)
            {
                Rectangle3D rect = britain.Coords[i];
                if (rect.Start.X == 1093 && rect.Start.Y == 1538 && rect.Width == 292 && rect.Height == 369)
                    indexOf = i;
            }
            if (indexOf == -1)
            {
                //e.Mobile.SendMessage("Failed to generate Britain Farms region.");
                return;
            }
            // save Britain region as XML
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement xmlElem = xmlDoc.CreateElement("region");
            xmlElem.SetAttribute("type", britain.GetType().Name);
            britain.Save(xmlElem);
            // create new Britain Farms region
            StaticRegion britainFarms = new StaticRegion("", "Britain Farms", Map.Felucca, typeof(WarriorGuard));
            // load Britain Farms region from XML
            britainFarms.Load(xmlElem);
            StaticRegion.XmlDatabase.Add(britainFarms);
            // adjust props
            britainFarms.ShowEnterMessage = false;
            britainFarms.ShowExitMessage = false;
            britainFarms.GoLocation = new Point3D(1321, 1751, 10);
            // adjust areas
            britain.Registered = false;
            britain.Coords.RemoveAt(indexOf);
            britain.Coords.Add(new Rectangle3D(1296, 1560, sbyte.MinValue, 89, 136, byte.MaxValue));
            britain.Coords.Add(new Rectangle3D(1284, 1706, sbyte.MinValue, 101, 211, byte.MaxValue));
            britain.Registered = true;
            britainFarms.Registered = false;
            britainFarms.Coords.Clear();
            britainFarms.Coords.Add(new Rectangle3D(1093, 1538, sbyte.MinValue, 203, 168, byte.MaxValue));
            britainFarms.Coords.Add(new Rectangle3D(1093, 1706, sbyte.MinValue, 191, 201, byte.MaxValue));
            britainFarms.Registered = true;
            // add region controller
            bool hasController = false;
            foreach (Item item in Map.Felucca.GetItemsInRange(new Point3D(5429, 1167, 0), 0))
            {
                if (item is StaticRegionControl)
                    hasController = true;
                break;
            }
            if (!hasController)
            {
                StaticRegionControl src = new StaticRegionControl();
                src.StaticRegion = britainFarms;
                src.MoveToWorld(new Point3D(5429, 1167, 0), Map.Felucca);
            }
            //e.Mobile.SendMessage("Successfully generated Britain Farms region.");
        }
        #endregion
        #endregion PatchV81
        #region PatchV73
        #region KinMigrate
        private static int KinMigrate()
        {
            int count = 0;
            foreach (BaseGuild g in BaseGuild.List.Values)
            {
                Guild guild = g as Guild;
                if (guild != null && guild.IOBAlignment != IOBAlignment.None)
                {
                    AlignmentType alignment = AlignmentType.None;
                    switch (guild.IOBAlignment)
                    {
                        case IOBAlignment.Council: alignment = AlignmentType.Council; break;
                        case IOBAlignment.Pirate: alignment = AlignmentType.Pirate; break;
                        case IOBAlignment.Brigand: alignment = AlignmentType.Brigand; break;
                        case IOBAlignment.Orcish: alignment = AlignmentType.Orc; break;
                        case IOBAlignment.Savage: alignment = AlignmentType.Savage; break;
                        case IOBAlignment.Undead: alignment = AlignmentType.Undead; break;
                        case IOBAlignment.Good: alignment = AlignmentType.Militia; break;
                    }
                    if (alignment != AlignmentType.None)
                    {
                        DateTime nextChangeTime = guild.NextTypeChange;
                        guild.IOBAlignment = IOBAlignment.None;
                        guild.Alignment = alignment;
                        guild.NextTypeChange = nextChangeTime;
                        count++;
                    }
                }
            }
            return count;
        }
        #endregion KinMigrate
        #endregion PatchV73
        #region PatchV72
        #region DirectionalDungeonTPs
        private static int DirectionalDungeonTPs()
        {
            HashSet<Teleporter> grouped = new HashSet<Teleporter>();
            List<List<Teleporter>> groups = new List<List<Teleporter>>();
            foreach (Item item in World.Items.Values)
            {
                Teleporter tp = item as Teleporter;
                if (tp != null && IsDungeonEnterExitTeleporter(tp) && !grouped.Contains(tp))
                {
                    List<Teleporter> group = new List<Teleporter>();
                    List<Teleporter> toProcess = new List<Teleporter>();
                    toProcess.Add(tp);
                    while (toProcess.Count != 0)
                    {
                        Teleporter cur = toProcess[0];
                        group.Add(cur);
                        grouped.Add(cur);
                        FindNeighbors(cur, toProcess, grouped);
                        toProcess.RemoveAt(0);
                    }
                    if (group.Count > 1)
                        groups.Add(group);
                }
            }
            int patched = 0;
            List<Teleporter> failed = new List<Teleporter>();
            foreach (List<Teleporter> group in groups)
            {
                DirectionalScore[] dirScores = new DirectionalScore[4];
                dirScores[0].Dir = Direction.North;
                dirScores[1].Dir = Direction.East;
                dirScores[2].Dir = Direction.South;
                dirScores[3].Dir = Direction.West;
                foreach (Teleporter tp in group)
                {
                    if (IsBlocked(new Point3D(tp.X, tp.Y - 1, tp.Z), tp.Map))
                        dirScores[0].Score++;
                    if (IsBlocked(new Point3D(tp.X + 1, tp.Y, tp.Z), tp.Map))
                        dirScores[1].Score++;
                    if (IsBlocked(new Point3D(tp.X, tp.Y + 1, tp.Z), tp.Map))
                        dirScores[2].Score++;
                    if (IsBlocked(new Point3D(tp.X - 1, tp.Y, tp.Z), tp.Map))
                        dirScores[3].Score++;
                }
                Array.Sort(dirScores);
                if (dirScores[0].Score == dirScores[1].Score)
                {
                    // we can't decide in which direction the TPs should work
                    failed.Add(group[0]);
                    foreach (Teleporter tp in group)
                    {
                        tp.Direction = (Direction)0;
                        tp.Directional = false;
                    }
                    continue;
                }
                Direction dir = dirScores[0].Dir;
                foreach (Teleporter tp in group)
                {
                    tp.Direction = dir;
                    tp.Directional = true;
                    patched++;
                }
            }

            return patched;
        }
        private static void FindNeighbors(Teleporter tp, List<Teleporter> list, HashSet<Teleporter> ignore)
        {
            FindTeleportersAt(new Point3D(tp.X, tp.Y - 1, tp.Z), tp.Map, list, ignore);
            FindTeleportersAt(new Point3D(tp.X + 1, tp.Y, tp.Z), tp.Map, list, ignore);
            FindTeleportersAt(new Point3D(tp.X, tp.Y + 1, tp.Z), tp.Map, list, ignore);
            FindTeleportersAt(new Point3D(tp.X - 1, tp.Y, tp.Z), tp.Map, list, ignore);
        }
        private static void FindTeleportersAt(Point3D loc, Map map, List<Teleporter> list, HashSet<Teleporter> ignore)
        {
            foreach (Item item in map.GetItemsInRange(loc, 0))
            {
                Teleporter tp = item as Teleporter;
                if (!ignore.Contains(tp) && tp != null && Utility.IsDungeonTeleporter(tp) && Math.Abs(loc.Z - tp.Z) <= 7)
                    list.Add(tp);
            }
        }
        private static bool IsBlocked(Point3D loc, Map map)
        {
            int surfaceZ;
            if (map.GetTopSurface(new Point3D(loc.X, loc.Y, loc.Z + 7), out surfaceZ) == null)
                return true;
            if (surfaceZ < loc.Z - 7)
                return true;
            if (!map.CanFit(loc.X, loc.Y, surfaceZ, 16, false, false, true))
                return true;
            return false;
        }
        private struct DirectionalScore : IComparable<DirectionalScore>
        {
            public Direction Dir;
            public int Score;
            public int CompareTo(DirectionalScore other)
            {
                return other.Score.CompareTo(Score);
            }
        }
        #endregion DirectionalDungeonTPs
        #endregion PatchV72
        #region PatchV71
        private static int DisableSolenCaveSpawners()
        {
            int count = 0;
            foreach (Item item in Server.World.Items.Values)
            {
                if (item == null || item.Deleted == true || item.Map == Map.Internal)
                    continue;
                if (item is Spawner spawner && spawner.Running == true)
                    if (Utility.WorldMap.SolenCaves.Contains(spawner.Location))
                    {
                        spawner.RemoveObjects();
                        spawner.Running = false;
                        count++;
                    }
            }
            return count;
        }
        private static int DisableTelesFromToSolenCaves()
        {
            int count = 0;
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted == true || item.Map == Map.Internal)
                    continue;
                if (item is Teleporter tele && tele.Running == true)
                    if (TeleporterFromTo(tele, new List<Rectangle2D>() { Utility.WorldMap.SolenCaves }, new List<Rectangle2D>() { Utility.WorldMap.SolenCaves }))
                    {
                        tele.Running = false;
                        count++;
                    }
            }

            return count;
        }
        #endregion PatchV71
        #region PatchV69
        private static Point3D Jiggle(Map map, Point3D point, object o)
        {
            for (int ix = 0; ix < 16; ix++)
            {
                point = Spawner.GetSpawnPosition(map, point, 2, SpawnFlags.None, o);
                if (SpawnerAtLocation(map, point) == false)
                    break;
            }

            return point;
        }
        private static bool SpawnerAtLocation(Map map, Point3D point)
        {
            Spawner spawner = (Spawner)FindOneItemAt(point, map, typeof(Spawner), 0, false);
            if (spawner != null)
                return true;

            return false;
        }
        #endregion PatchV69
        #region PatchV66
        public class Line
        {
            public Point2D p1, p2;

            public Line(Point2D p1, Point2D p2)
            {
                this.p1 = p1;
                this.p2 = p2;
            }

            public Point2D[] GetPoints(int quantity)
            {
                var points = new Point2D[quantity];
                int ydiff = p2.Y - p1.Y, xdiff = p2.X - p1.X;
                double slope = (double)(p2.Y - p1.Y) / (p2.X - p1.X);
                double x, y;

                --quantity;

                for (double i = 0; i < quantity; i++)
                {
                    y = slope == 0 ? 0 : ydiff * (i / quantity);
                    x = slope == 0 ? xdiff * (i / quantity) : y / slope;
                    //points[(int)i] = new Point2D((int)Math.Round(x) + p1.X, (int)Math.Round(y) + p1.Y);
                    points[(int)i] = new Point2D((int)Math.Ceiling(x) + p1.X, (int)Math.Ceiling(y) + p1.Y);
                }

                points[quantity] = p2;
                return points;
            }
        }
        #endregion PatchV66
        #region PatchV60
        private static void ResetChampsWorker(PatchIndex bits, ref int patched)
        {
            string[] names = new[] { "Summer Pirate", "Autumn Bob", "Winter FrozenHost", "Spring Vampire" };
            Serial[] ids = new[] { ChampHelpers.Summer_Pirate, ChampHelpers.Autumn_Bob, ChampHelpers.Winter_FrozenHost, ChampHelpers.Spring_Vampire };
            try
            {
                int index = 0;
                foreach (Serial serial in ids)
                {
                    if (Server.World.FindItem(serial) as ChampEngine != null)
                    {
                        ChampEngine champ = Server.World.FindItem(serial) as ChampEngine;
                        champ.Name = names[index++];
                        EchoOut(string.Format("Resetting seasonal champ {0}.", champ.Name), ConsoleColor.Magenta);
                        ChampHelpers.ToggleChamp(serial, false);
                        patched++;
                    }
                    else
                    {
                        ErrorOut(bits, string.Format("{0} seasonal not found", serial), ConsoleColor.Red);
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Seasonal champ ResetChamps code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
        #endregion PatchV60
        #region PatchV55
        private static List<string> GetNerunSpawnerEntries()
        {
            // get Vendor spawner entries in "Sea Market"
            string[] files = new string[] { Path.Combine(Core.DataDirectory, "Spawners/Nerun's Distro/Spawns/felucca/Vendors.map") };
            List<string> mzLines = new();
            bool recording = false;
            bool start = false;
            foreach (string file in files)
            {
                foreach (string line in System.IO.File.ReadLines(file))
                {   //first scan looking for "Sea Market"
                    if (line.ToLower().Contains("sea market"))
                    {   // found it
                        // now collect all lines up until the next comment into a buffer
                        start = true;
                        continue;
                    }
                    if (start == true && CanParse(line))
                    {
                        recording = true;
                        mzLines.Add(line);
                        continue;
                    }
                    else if (recording == true && CanParse(line) == false)
                    {
                        recording = false;
                        break;
                    }
                }
            }

            return mzLines;
        }
        #endregion PatchV55
        #region PatchV47
        private static bool IsSpawnerProduct(Mobile m)
        {
            foreach (Item item in Server.World.Items.Values)
            {
                if (item == null || item.Deleted) continue;
                if (item is Spawner spawner)
                {
                    if (spawner.Objects != null)
                        foreach (object o in spawner.Objects)
                            if (o is Mobile sm)
                                if (sm.Serial == m.Serial)
                                    return true;
                }
            }

            return false;
        }
        #endregion PatchV47
        #region PatchV43
        public static void RecursiveDelete(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
                return;

            foreach (var dir in baseDir.EnumerateDirectories())
            {
                RecursiveDelete(dir);
            }
            baseDir.Delete(true);
        }
        #endregion PatchV43
        #region PatchV22
        public static void HasPatchedFeluccaTeleportersWorker()
        {
            Map map = null;
            string toDeleteFile = "Angel Island teleporters.cfg";
            string toDeletepathName = Path.Combine(Core.DataDirectory, "Patches", toDeleteFile);
            EnsurePath(toDeletepathName);
            List<Item> toDeleteList = new();
            foreach (string line in File.ReadAllLines(toDeletepathName))
            {
                string[] toks = line.Split(' ');
                //-------------------------------------------------------------- Serial Number ------------------------------------------------- X ---------------- Y ----------------- Z ----------------- Map -----------
                Tuple<int, int, int, int, Map> record = new(int.Parse(toks[0]), int.Parse(toks[1]), int.Parse(toks[2]), int.Parse(toks[3]), Map.Parse(toks[4]));
                Serial serial = record.Item1;
                Item item = World.FindItem(serial);
                if (item != null && item.Deleted == false && item.Map != null && item.Map != Map.Internal)
                    toDeleteList.Add(item);
            }

            // next, read the list of items which we wish to preserve for standard shards
            // PreserveTeleporterIS.cfg
            string preserveFile = "PreserveTeleporterIS.cfg";
            string preservepathName = Path.Combine(Core.DataDirectory, "Patches", preserveFile);
            EnsurePath(preservepathName);
            List<Item> preserveLlist = new();
            if (File.Exists(preservepathName))
            {
                foreach (String line in File.ReadAllLines(preservepathName))
                {
                    string[] toks = line.Split(' ');
                    Serial serial = int.Parse(toks[0]);
                    Item item = World.FindItem(serial);
                    if (item != null && item.Deleted == false && item.Map != null && item.Map != Map.Internal)
                        preserveLlist.Add(item);
                }
            }

            // next, read the list of event teles which we wish to preserve for standard shards
            // EventTeleporters.cfg
            string eventFile = "EventTeleporters.cfg";
            string eventpathName = Path.Combine(Core.DataDirectory, "Patches", eventFile);
            EnsurePath(eventpathName);
            if (File.Exists(eventpathName))
            {
                foreach (String line in File.ReadAllLines(eventpathName))
                {
                    string[] toks = line.Split(' ');
                    Serial serial = int.Parse(toks[0]);
                    Item item = World.FindItem(serial);
                    if (item != null && item.Deleted == false && item.Map != null && item.Map != Map.Internal)
                        preserveLlist.Add(item);
                }
            }

            // what's the delta?
            var delta = toDeleteList.Except(preserveLlist).ToList();

            // only unique values
            delta = delta.Distinct().ToList();

            // delta list now contains the things we want to deleted from our standard shard
            LogHelper review = new LogHelper("Teleporters to deleted from shard.log", true, true, true);
            foreach (Item item in delta)
                review.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", item.Serial.Value, item.X, item.Y, item.Z, item.Map));
            review.Finish();

            // we can't load the jumplist with the items as usual, since when we get there it will have been deleted.
            //  we'll load our jumplist with Point3D+map instead.
            List<Item.WorldLocation> jumplist = new();
            foreach (Item item in delta)
                jumplist.Add(item.GetWorldLocation());

            // lets be careful
            AutoSave.SavesEnabled = false;

            // lets get cracking!
            foreach (Item item in delta)
            {
                if (item != null && item.Deleted == false && item.Map != null && item.Map != Map.Internal)
                {
                    map = item.Map;
                    Rectangle2D rect = new();
                    int width = 6;
                    int height = 6;

                    if (item.Serial == 0x400446E1 || item.Serial == 0x40023C72)
                    {   // exception: teleporter 0x400446E1 || 0x40023C72 needs a 5x9 wipe
                        // special room in wrong
                        width = 6;
                        height = 10;
                    }
                    else if (item.Serial == 0x40055B9C || item.Serial == 0x400C91AE)
                    {   // exception: 0x40055B9C || 0x400C91AE needs a 1x1 wipe. (Oc'nivelle deco)
                        // oc'nivelle teles with surrounding deco
                        width = 1;
                        height = 1;
                    }
                    else if (item.Serial == 0x400162AA || item.Serial == 0x40027043)
                    {   // destard level 3
                        width = 6;
                        height = 6;
                    }
                    else
                    {
                        width = 6;
                        height = 6;
                    }

                    rect.X = item.X;
                    rect.Y = item.Y;
                    rect.Width = 1;
                    rect.Height = 1;

                    // now grow the rect to hold the items within the given width and height
                    int[] dimentions = new int[] { width, height };
                    foreach (int dimention in dimentions)
                    {   // some deletions are for the tele only, don't suck in any other tiles.
                        if (dimention > 1)
                        {
                            IPooledEnumerable eable = item.Map.GetItemsInRange(item.Location, dimention);
                            foreach (Item nearItem in eable)
                                if (nearItem != null && nearItem.Deleted == false && nearItem.Map != null && nearItem.Map != Map.Internal)
                                    rect.MakeHold(new Point2D(nearItem.X, nearItem.Y));
                            eable.Free();
                        }
                    }

                    UpdateWorldUnit(map, rect, item);
                }
            }
        }
        private static void UpdateWorldUnit(Map map, Rectangle2D rect, Item item)
        {
            // ===
            // special types and ItemIDs to protect
            // ===

            // cages near a teleporter
            List<int> ids = new() { 0x000006f6, 0x000006f5, 0x00000687 };

            // other types to ignore
            List<Type> types = new();
            types.Add(typeof(Spawner));
            types.Add(typeof(BaseDoor));
            types.Add(typeof(Moongate));
            types.Add(typeof(Teleporter));

            int startingZ = item.Z;
            item.Delete();

            // do the patch
            RefreshDeco(map, new List<Rectangle2D>() { rect }, startingZ: startingZ,
                typeExclude: types.ToArray(), idExclude: ids.ToArray(), lenientZ: int.MinValue /*5*/);
        }
        #endregion PatchV22
        #region HasPatchedZoo   
        private static List<string> GetMoonglowSpawnerEntries()
        {
            // get Moonglow Zoo spawners
            string[] files = new string[] { Path.Combine(Core.DataDirectory, "Spawners/Nerun's Distro/Spawns/felucca/Outdoors.map") };
            List<string> mzLines = new();
            bool recording = false;
            bool start = false;
            foreach (string file in files)
            {
                foreach (string line in System.IO.File.ReadLines(file))
                {   //first scan looking for moonglow zoo
                    if (line.ToLower().Contains("moonglow zoo"))
                    {   // found it
                        // now collect all lines up until the next comment into a buffer
                        start = true;
                        continue;
                    }
                    if (start == true && CanParse(line))
                    {
                        recording = true;
                        mzLines.Add(line);
                        continue;
                    }
                    else if (recording == true && CanParse(line) == false)
                    {
                        recording = false;
                        break;
                    }
                }
            }

            return mzLines;
        }
        private static List<Spawner> AllModeNerunsSpawnersAt(int X, int Y, Map map)
        {
            List<Spawner> list = new();

            foreach (object o in map.GetObjectsInBounds(new Rectangle2D(X, Y, 1, 1)))   // does 0,0 work? seems not
                if (o is Spawner spawner)
                    if (spawner.ModeNeruns)
                        list.Add(spawner);

            return list;
        }
        private static bool CanParse(string line)
        {
            string[] lineToks = line.Split(new char[] { '|' });                         // parse on '|'
            if (lineToks.Length == 22)                                                  // always 22 fields
                return true;
            return false;
        }
        #endregion HasPatchedZoo
        #endregion Helpers
    }
}