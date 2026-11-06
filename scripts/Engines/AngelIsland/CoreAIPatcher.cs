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

/* Engines/AngelIsland/CoreAIPatcher.cs
 * ChangeLog
 *	1/17/23 Created by Adam;
 *	    You will only add patch IDs here. Nothing else to do.
 *	    Append your patches to the PatchIndex enum, after the last patch id, but just before __last.
 *	    Ordinality is important here as the order defines the patch id.
 *	    don't shuffle, add, or delete any enum fields as this would disrupt the patch-state of all patches that follow.
 *	    Example:
 *	    public enum PatchIndex
 *      {
 *          #region IMPORTED FROM PatchBits - DO NOT EDIT
 *          PatcherTableTest,           // test the new patcher table
 *          UpdatedRegionControllersV1, // Add static region controllers for Wrong
 *          / * ADD YOUR PATCH ID HERE * /
 *          __last
 *      }
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml;
using static Server.Patcher;

namespace Server
{

    public partial class CoreAI
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("PatchLevel", AccessLevel.Administrator, new CommandEventHandler(PatchLevel));
            Server.CommandSystem.Register("RunPatch", AccessLevel.Administrator, new CommandEventHandler(RunPatch));
        }

        // Patch named index table
        public enum PatchIndex
        {
            None = 0,    // special used for Always Run
            PatchV1,     // retired: one-time patch history conversion (code commented out)
            PatchV2,     // clear the PatchV3 flag so the static region patch reruns
            PatchV3,     // clear the PatchV4 flag so the static region patch reruns
            PatchV4,     // reload static regions from Data and rerun PatchV19
            PatchV5,     // set Green Acres region controller graphics
            PatchV6,     // general cleanup: jail exit gate, Warden, prison spawners, moongate wizards
            PatchV7,     // Standard shards: validate teleporter patch lists
            PatchV8,     // turn off FreezeDry
            PatchV9,     // Standard shards: disable IOB regions
            PatchV10,    // retired: wipe Kin Ransom chests
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
            PatchV101,   // tower doors, carpet store, bog creatures
            PatchV102,   // refresh camps, camp spawner placement, alignment regions, boat lockdowns, herding
            PatchV103,   // refresh camps, angry miner camps, plows
            PatchV104,   // refresh camps
            PatchV105,   // system character, recycle bin cleanup, refresh camps, respawn aligned creatures
            PatchV106,   // make the system character persist and lead the guild
            PatchV107,   // Siege-style: update moongate wizard spawners
            PatchV108,   // refresh camps
            PatchV109,   // refresh camps
            PatchV110,   // remove spawned rares, stealable fruit baskets, client enforcement (warning only)
            PatchV111,   // remove spawned rares, fruit baskets, client enforcement, spawner item and camp cleanup
            PatchV112,   // fix spinning township NPCs
            PatchV113,   // farm produce spawners, watch list, safer treasure chest spawners
            PatchV114,   // remove Adam from the watch list
            PatchV115,   // replace barrels, tubs and buckets with fillable versions
            PatchV116,   // Siege-style: enable lizardman camps
            PatchV117,   // Siege-style: apply missed murder reprieves
            PatchV118,   // Siege-style: enable breeding, Tree of Knowledge and kukui nut spawners
            PatchV119,   // Siege-style: disable breeding, Tree of Knowledge and kukui nut spawners
            PatchV120,   // Siege-style: replace Pub15 lizardman camps
            PatchV121,   // Siege-style: unbond pets
            PatchV122,   // mark player-crafted items as Origin.Player, Siege-style respawn
            PatchV123,   // tag legacy magic loot
            PatchV124,   // Test Center: enable breeding, Tree of Knowledge and kukui nut spawners
            PatchV125,   // farmables (guard dogs), Siege-style: enable BODs
            PatchV126,   // Siege-style: replace large BODs with small
            PatchV127,   // Siege-style: bring town criers home
            PatchV128,   // Siege-style: start decay on angry miner camps
            PatchV129,   // Siege-style: respawn real estate brokers
            PatchV130,   // Siege-style: wipe factions, respawn vendors
            PatchV131,   // Siege-style: restore grandfathered war horses
            PatchV132,   // respawn the world to fix underwater spawn
            PatchV133,   // fix spawner template reference counts
            PatchV134,   // Siege-style: replace xmas trees and deeds with XML versions with music
            PatchV135,   // replace xmas trees and deeds with XML versions with music
            PatchV136,   // set xmas tree and motion controller RangeExit to -1
            PatchV137,   // unbless selected items, set township minstrel charge
            PatchV138,   // enable static housing, set township minstrel charge
            PatchV139,   // mark hire fighter gear DeleteOnLift
            PatchV140,   // respawn hire fighters for their new titles
            PatchV141,   // Siege: refund houses bought at the old 10x price
            PatchV142,   // fix ControlMasterGUID for stabled pets
            PatchV143,   // Siege/Angel Island: recreate lost pets
            PatchV144,   // recalculate house deed prices on vendors and for-sale signs
            PatchV145,   // fix mount ItemIDs and remount riders
            PatchV146,   // Siege: allow mini-boss armor; clear the legacy IsStabled bit
            PatchV147,   // empty placeholder
            PatchV148,   // reassign legacy reserved serials
            PatchV149,   // set stable masters to breeders only, no claim tickets
            PatchV150,   // disable spawners that spawn restricted objects
            PatchV151,   // rebuild the item/mobile link database
            PatchV152,   // find and fix enchanted scrolls
            PatchV153,   // rerun the link database fix (PatchV151)
            PatchV154,   // patch invasion beacons
            PatchV155,   // delete old invasion beacons
            PatchV156,   // fix stable masters (breeders only, no checks)
            PatchV157,   // turn on all beacons
            PatchV158,   // port broadcaster controllers to the spawner model
            PatchV159,   // Siege: re-add missing mobiles to multi/Nerun's spawners
            PatchV160,   // Test Center: re-add missing mobiles to multi/Nerun's spawners
            PatchV161,   // Siege-style: wounded soldiers give no rewards
            PatchV162,   // establish player reward hues, Siege anniversary shroud
            PatchV163,   // fix motion controllers
            PatchV164,   // Siege: reset champ PvM rankings
            PatchV165,   // Siege-style: no skill gain waits, record GM skills in history
            PatchV166,   // force a township subsidy update
            PatchV167,   // unregister custom region controls on the internal map
            PatchV168,   // Siege champ, township and pirate updates, clean up spawner lists
            PatchV169,   // Siege: replace island serpents with gila monsters, remove sterling from animal spawners
            PatchV170,   // Siege: reset champ engines to initialize pirate naming
            PatchV171,   // patch fire pits
            PatchV172,   // remove all staff accounts
            PatchV173,   // Event shards: despawn for the Tribute shard
            PatchV174,   // turn off FreezeAccountDecay
            PatchV175,   // turn off FreezeHouseDecay and FreezeTownshipDecay
            PatchV176,   // recalculate house deed prices on vendors
            PatchV177,   // allow pets through Moonglow teleporters
            PatchV178,   // migrate house and bank box ownership to GUIDs
            PatchV179,   // Mortalis: reset repeat murderous offenses to 1
            PatchV180,   // Mortalis: adjust stat gain factor, remove self perma flags
            PatchV181,   // set the BOD gold reward model (GoldCraftValue, scalar 2.333)
            PatchV182,   // send farmers' dogs home when players leave
            PatchV183,   // link persistent GUID identity, backfill music ownership
            PatchV184,   // Mortalis: relink music ownership from the music database
            PatchV185,   // mark travel objects (gates, teleporters)
            PatchV186,   // kill angry miners, Mortalis-style: repair launch reward clothes
            PatchV187,   // fix door facing in small marble workshops
            PatchV188,   // Mortalis: give each player a song
            PatchV189,   // Mortalis: fix granite hues
            PatchV190,   // Angel Island: reset GlobalGoldScalar to 1
            PatchV191,   // initialize region defaults, allow reds in the Custom Housing Area
            PatchV192,   // initialize criminality defaults, configure Seasons
            PatchV193,   // make template mobs staff owned
            PatchV194,   // set the BOD gold reward model, no GlobalGoldScalar
            PatchV195,   // respawn running pirate champs
            PatchV196,   // set harmful gray durations (2 min NPC, 10 min PvP)
            PatchV197,   // respawn the world to regenerate corrected gold
            PatchV198,   // delete the double door in Minoc
            PatchV199,   // clean up Felucca, create the per-shard Test Center, new player area region
            PatchV200,   // Upheaval: initialize (account limits, no new player guild or area)
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
            PatchV234,   // retired: unused slot
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
            __last
        }
        // patch table
        public static bool[] PatchTable = new bool[(int)PatchIndex.__last];

        #region Patch API
        public static bool IsDynamicPatchSet(PatchIndex pb) { return PatchTable[(int)pb]; }
        public static void SetDynamicPatch(PatchIndex pb) { PatchTable[(int)pb] = true; }
        public static void ClearDynamicPatch(PatchIndex pb) { PatchTable[(int)pb] = false; }

        #endregion Patch API
        public static void LoadPatchTable(XmlElement node)
        {
            XmlElement? patchTable = node["patchTable"];
            int index = 0;
            if (patchTable != null)
            {
                foreach (XmlElement value in patchTable.GetElementsByTagName("value"))
                {
                    try { CoreAI.PatchTable[index++] = GetBool(value, false); }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }
        }
        public static void SavePatchTable(XmlTextWriter xml)
        {
            xml.WriteStartElement("patchTable");

            foreach (bool b in PatchTable)
                Save(xml, b);

            xml.WriteEndElement();
        }
        public static void Save(XmlTextWriter xml, bool b)
        {
            xml.WriteStartElement("value");
            xml.WriteString(b.ToString());
            xml.WriteEndElement();
        }
        public static bool GetBool(XmlElement? node, bool defaultValue)
        {
            if (node == null)
                return defaultValue;

            string strVal = GetText(node, defaultValue.ToString());
            if (string.Equals(strVal, "true", StringComparison.OrdinalIgnoreCase)) return true;
            else if (string.Equals(strVal, "false", StringComparison.OrdinalIgnoreCase)) return false;
            else return defaultValue;
        }

        #region Commands
        [Usage("PatchLevel")]
        [Description("Display the current Patch Level.")]
        public static void PatchLevel(CommandEventArgs e)
        {
            try
            {
                var patchNames = Enum.GetNames(typeof(PatchIndex));

                // Second to last = the last real patch (before __last)
                string lastPatch = patchNames[patchNames.Length - 2];

                string displayName = lastPatch.Replace("PatchV", "v");

                e.Mobile.SendMessage($"The shard is at patch level {displayName}");
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }
        [Usage("RunPatch <patchNumber>")]
        [Description("Explicitly runs the specified patch.")]
        public static void RunPatch(CommandEventArgs e)
        {
            try
            {
                if (e.Length != 1)
                {
                    e.Mobile.SendMessage("Usage: RunPatch <patchNumber>");
                    return;
                }

                if (!int.TryParse(e.GetString(0), out int patchNumber))
                {
                    e.Mobile.SendMessage("Patch number must be an integer.");
                    return;
                }

                if (!Patcher.ValidPatch(patchNumber))
                {
                    e.Mobile.SendMessage("Patch {0} is not a valid patch number.", patchNumber);
                    return;
                }

                int? patched = Patcher.RunPatch(patchNumber);

                if (!patched.HasValue)
                {
                    e.Mobile.SendMessage(
                        $"PatchV{patchNumber} was not found or was not applicable.");
                    return;
                }

                e.Mobile.SendMessage(
                    $"PatchV{patchNumber} complete with {patched.Value} objects updated.");
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }
        #endregion Commands

        #region Reporting
        public static bool ReportStatus()
        {
            bool ok = ReportLibraries();

            ReportPatchStatus();

            return ok;
        }
        public static void ReportPatchStatus()
        {
            bool[] patchTable = LoadPatchTableForReport();

            if (patchTable == null)
            {
                Console.WriteLine("Patch history unavailable.");
                return;
            }

            int lastApplied = 0;
            List<int> missing = new List<int>();

            for (int i = 1; i < patchTable.Length; i++)
            {
                if (patchTable[i])
                    lastApplied = i;
            }

            if (lastApplied == 0)
            {
                Console.WriteLine("No patches applied.");
                return;
            }

            for (int i = 1; i <= lastApplied; i++)
            {
                if (!patchTable[i])
                    missing.Add(i);
            }

            if (missing.Count == 0)
            {
                Console.WriteLine($"Patches 1-{lastApplied} applied.");
            }
            else
            {
                // Gaps are not necessarily errors. A patch may be retired or simply
                // not applicable to this shard, leaving its PatchTable entry unset.
                Console.WriteLine($"Patch history through V{lastApplied}.");
                Console.WriteLine(
                    $"Not applied: {string.Join(", ", missing.Select(i => $"V{i}"))}");
            }
        }
        public static bool ReportLibraries()
        {
            int ready = 0;
            bool ok = true;

            foreach (PatchLibraryInfo lib in PatchLibraries)
            {
                string path = Path.Combine(AppContext.BaseDirectory, lib.AssemblyName);

                if (!File.Exists(path))
                {
                    Console.WriteLine($"{lib.AssemblyName}  range {lib.FirstPatch}-{lib.LastPatch}  MISSING");
                    ok = false;
                    continue;
                }

                if (!TryValidateContract(path, lib, out string detail))
                {
                    Console.WriteLine($"{lib.AssemblyName}  range {lib.FirstPatch}-{lib.LastPatch}  INVALID  {detail}");
                    ok = false;
                    continue;
                }

                Console.WriteLine($"{lib.AssemblyName}  range {lib.FirstPatch}-{lib.LastPatch}  ready to load");
                ready++;
            }

            Console.WriteLine($"Patch libraries: {ready}/{PatchLibraries.Length} ready to load");
            return ok;
        }
        private static bool TryValidateContract(string path, PatchLibraryInfo lib, out string detail)
        {
            detail = null;

            try
            {
                using FileStream stream = File.OpenRead(path);
                using var peReader = new PEReader(stream);

                if (!peReader.HasMetadata)
                {
                    detail = "not a managed assembly";
                    return false;
                }

                MetadataReader md = peReader.GetMetadataReader();

                if (!TryFindType(md, lib.TypeName, out TypeDefinition typeDef))
                {
                    detail = $"type not found: {lib.TypeName}";
                    return false;
                }

                if (!typeDef.Attributes.HasFlag(TypeAttributes.Public) ||
                    !typeDef.Attributes.HasFlag(TypeAttributes.Abstract) ||
                    !typeDef.Attributes.HasFlag(TypeAttributes.Sealed))
                {
                    // public static class == public abstract sealed
                    detail = $"{lib.TypeName} is not a public static class";
                    return false;
                }

                bool sawGetLast = false;
                bool sawRun = false;

                foreach (MethodDefinitionHandle methodHandle in typeDef.GetMethods())
                {
                    MethodDefinition method = md.GetMethodDefinition(methodHandle);
                    string name = md.GetString(method.Name);

                    if (name == "GetLastPatch")
                    {
                        if (!IsPublicStatic(method))
                        {
                            detail = "GetLastPatch is not public static";
                            return false;
                        }

                        if (!SignatureMatches(md, method, paramCount: 0, returnsInt: true, returnsNullableInt: false))
                        {
                            detail = "GetLastPatch signature mismatch (expected int GetLastPatch())";
                            return false;
                        }

                        sawGetLast = true;
                    }
                    else if (name == "RunPatch")
                    {
                        if (!IsPublicStatic(method))
                        {
                            detail = "RunPatch is not public static";
                            return false;
                        }

                        if (!SignatureMatches(md, method, paramCount: 1, returnsInt: false, returnsNullableInt: true))
                        {
                            detail = "RunPatch signature mismatch (expected int? RunPatch(int))";
                            return false;
                        }

                        sawRun = true;
                    }
                }

                if (!sawGetLast)
                {
                    detail = "GetLastPatch not found";
                    return false;
                }

                if (!sawRun)
                {
                    detail = "RunPatch not found";
                    return false;
                }

                if (!TryReadPatchOffset(md, typeDef, out int offset))
                {
                    detail = "PatchOffset const not found";
                    return false;
                }

                // Your libraries use offset 0/100/200 for ranges 1-100 / 101-200 / 201-300
                if (offset != lib.FirstPatch - 1 && offset != lib.FirstPatch)
                {
                    detail = $"PatchOffset={offset} does not match catalog range {lib.FirstPatch}-{lib.LastPatch}";
                    return false;
                }

                return true;
            }
            catch (BadImageFormatException ex)
            {
                detail = "bad image: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                detail = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static bool IsPublicStatic(MethodDefinition method)
        {
            MethodAttributes attrs = method.Attributes;
            return attrs.HasFlag(MethodAttributes.Public) &&
                   attrs.HasFlag(MethodAttributes.Static);
        }

        private static bool TryFindType(MetadataReader md, string fullName, out TypeDefinition typeDef)
        {
            typeDef = default;

            int lastDot = fullName.LastIndexOf('.');
            string ns = lastDot >= 0 ? fullName.Substring(0, lastDot) : "";
            string name = lastDot >= 0 ? fullName.Substring(lastDot + 1) : fullName;

            foreach (TypeDefinitionHandle handle in md.TypeDefinitions)
            {
                TypeDefinition td = md.GetTypeDefinition(handle);
                if (md.GetString(td.Name) != name)
                    continue;
                if (md.GetString(td.Namespace) != ns)
                    continue;

                typeDef = td;
                return true;
            }

            return false;
        }

        private static bool TryReadPatchOffset(MetadataReader md, TypeDefinition typeDef, out int offset)
        {
            offset = 0;

            foreach (FieldDefinitionHandle fieldHandle in typeDef.GetFields())
            {
                FieldDefinition field = md.GetFieldDefinition(fieldHandle);
                if (md.GetString(field.Name) != "PatchOffset")
                    continue;

                if (!field.Attributes.HasFlag(FieldAttributes.Literal) ||
                    !field.Attributes.HasFlag(FieldAttributes.Static))
                    return false;

                ConstantHandle constantHandle = field.GetDefaultValue();
                if (constantHandle.IsNil)
                    return false;

                Constant constant = md.GetConstant(constantHandle);
                if (constant.TypeCode != ConstantTypeCode.Int32)
                    return false;

                BlobReader blob = md.GetBlobReader(constant.Value);
                offset = blob.ReadInt32();
                return true;
            }

            return false;
        }

        private static bool SignatureMatches(MetadataReader md, MethodDefinition method, int paramCount, bool returnsInt, bool returnsNullableInt)
        {
            BlobReader blob = md.GetBlobReader(method.Signature);

            SignatureHeader header = blob.ReadSignatureHeader();
            if (header.Kind != SignatureKind.Method)
                return false;
            if (header.IsInstance)
                return false;

            int actualParams = blob.ReadCompressedInteger();
            if (actualParams != paramCount)
                return false;

            if (returnsInt)
            {
                if (blob.ReadSignatureTypeCode() != SignatureTypeCode.Int32)
                    return false;
            }
            else if (returnsNullableInt)
            {
                if (!IsNullableInt32(md, ref blob))
                    return false;
            }

            if (paramCount == 1)
            {
                if (blob.ReadSignatureTypeCode() != SignatureTypeCode.Int32)
                    return false;
            }

            return true;
        }

        private static bool IsNullableInt32(MetadataReader md, ref BlobReader blob)
        {
            // int? == GenericInst System.Nullable`1<int>

            if (blob.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance)
                return false;

            if (blob.ReadSignatureTypeCode() != SignatureTypeCode.TypeHandle)
                return false;

            EntityHandle typeHandle = blob.ReadTypeHandle();

            if (!IsNullableOfT(md, typeHandle))
                return false;

            int genericArgCount = blob.ReadCompressedInteger();

            if (genericArgCount != 1)
                return false;

            return blob.ReadSignatureTypeCode() == SignatureTypeCode.Int32;
        }

        private static bool IsNullableOfT(MetadataReader md, EntityHandle handle)
        {
            string ns;
            string name;

            if (handle.Kind == HandleKind.TypeReference)
            {
                TypeReference tr = md.GetTypeReference((TypeReferenceHandle)handle);
                ns = md.GetString(tr.Namespace);
                name = md.GetString(tr.Name);
            }
            else if (handle.Kind == HandleKind.TypeDefinition)
            {
                TypeDefinition td = md.GetTypeDefinition((TypeDefinitionHandle)handle);
                ns = md.GetString(td.Namespace);
                name = md.GetString(td.Name);
            }
            else
            {
                return false;
            }

            return ns == "System" && name == "Nullable`1";
        }

        private static bool[] LoadPatchTableForReport()
        {
            string filePath = Path.Combine(
                Core.SavesDirectory,
                Core.Server,
                "CoreAI.xml");

            if (!File.Exists(filePath))
                return null;

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement? root = doc["CoreAI"];

            if (root == null)
                return null;

            XmlElement? patchTable = root["patchTable"];

            if (patchTable == null)
                return null;

            bool[] table = new bool[PatchTable.Length];

            int index = 0;

            foreach (XmlElement value in patchTable.GetElementsByTagName("value"))
            {
                if (index >= table.Length)
                    break;

                table[index++] = GetBool(value, false);
            }

            return table;
        }
        #endregion Reporting
    }
}