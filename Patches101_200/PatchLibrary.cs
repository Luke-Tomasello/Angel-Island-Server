/***************************************************************************
 *
 *                         Patches101_200
 *
 *  GMN World Patch Library - Patches 101 through 200
 *
 *  OVERVIEW
 *  --------
 *  This library contains the implementation of world patches assigned to
 *  patch numbers 101 through 200.
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

/* Patches101_200/PatchLibrary.cs
 * ChangeLog
 *  10/29/26, Adam (MusicLibrary)
 *      Type names only: the music library moved out of MusicBox (MusicBox.GlobalMusicRepository is MusicLibrary.Repository,
 *      MusicBox.MusicInfo is MusicLibrary.MusicInfo; MusicBox.FindSong is unchanged). No patch changes what it does.
 *  10/27/26, Adam
 *      SpawnTypes enum removed (champion definitions are now Data/ChampSpawns.xml): PatchV168, PatchV170 and PatchV195 use
 *      ChampEngine.DefinitionName (ChampNames) instead of SpawnType. Same champs, same effect; all three have already run.
 */

using Server;
using Server.Accounting;
using Server.Commands;
using Server.Diagnostics;
using Server.Engines;
using Server.Engines.Alignment;
using Server.Engines.BulkOrders;
using Server.Engines.ChampionSpawn;
using Server.Engines.Consoles;
using Server.Guilds;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Multis.Deeds;
using Server.Regions;
using System.Collections;
using System.Xml;
using static Server.CoreAI;
using static Server.Mobiles.Spawner;
using static Server.Patcher;
using static Server.Utility;

namespace Patches101_200
{
    public static class PatchLibrary
    {
        private const int PatchOffset = 100;

        private delegate int? PatchHandler(int patchid);

        private static readonly PatchHandler[] m_Patches =
        {
            null,        // index 0 - reserved/run-always convention
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

        #region PatchV101

        private static int? PatchV101(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Patch Tower doors
                if (AllShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is BaseDoor bd && (bd.Z == 26 || bd.Z == 46) && bd.Facing == DoorFacing.SouthCW)
                        {
                            bd.Facing = DoorFacing.NorthCW;
                            patched++;
                        }
                }
                #endregion Patch Tower doors

                #region Enable Carpet Store
                if (SiegeStyleShards())
                {
                    Point3D px = new Point3D(2222, 1150, 0);
                    Spawner spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                    if (spawner != null)
                        if (EnableSpawner(ref spawner))
                            patched++;
                }
                #endregion  Enable Carpet Store

                #region Spawn Bog creatures (for plant system)
                if (SiegeStyleShards())
                {
                    Point3D px = new Point3D(1163, 2885, 0);
                    Spawner spawner = null;
                    if (AddSpawner(px, new List<string>() { "Bogthing", "Bogling" }, coreSpawn: false, ref spawner, count: 1, homeRange: 30, force: true))
                    {
                        spawner.Distro = SpawnerModeAttribs.ModeMulti;
                        spawner.Counts = "2,5";
                        spawner.Concentric = true;
                        patched++;
                    }
                }
                #endregion Spawn Bog creatures (for plant system)

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV101

        #region PatchV102

        private static int? PatchV102(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Cleanup & Refresh Camps
                if (AllShards())
                {
                    List<BaseCamp> list = new();
                    foreach (var item in World.Items.Values)
                        if (item is BaseCamp bc && bc.Deleted == false)
                            list.Add(bc);

                    foreach (BaseCamp bc in list)
                        bc.Delete();

                    patched += list.Count;

                    // now respawn
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.Running == true && spawner.Spawns("camp"))
                        {
                            spawner.Respawn();
                            patched++;
                        }
                }
                #endregion Cleanup & Refresh Camps

                #region Set certain camp spawners to home range 0
                //  we will then manually move them manually to a location where they spawn correctly (not in trees etc.)
                if (SiegeStyleShards())
                {
                    foreach (var item in World.Items.Values)
                        if (item is BaseCamp bc && bc.Deleted == false)
                            if (bc is OrcCamp || bc is RatCamp || bc is LizardmanCamp)
                                if (bc.Spawner != null && bc.Spawner.Deleted == false)
                                {
                                    if (bc.Spawner.HomeRange != 0)
                                    {
                                        bc.Spawner.HomeRange = 0;
                                        patched++;
                                    }
                                }

                    // now move them to better locations that show all components
                    List<Point3D> list = new()
                    {
                        new Point3D (4480, 1439, 8),
                        new Point3D (4488, 1429, 8),
                        new Point3D (2103, 3371, 4),
                        new Point3D (2093, 3357, 0),
                        new Point3D (1152, 3471, 0),
                        new Point3D (1153, 3474, 0),
                        new Point3D (2349, 722, 0),
                        new Point3D (2337, 731, 0),
                        new Point3D (2548, 1173, 5),
                        new Point3D (2549, 1174, 5),
                        new Point3D (826, 662, 0),
                        new Point3D (824, 662, 0),
                        new Point3D (1064, 2488, 0),
                        new Point3D (1060, 2483, 0),
                        new Point3D (1858, 1082, 0),
                        new Point3D (1856, 1081, 0),
                        new Point3D (1530, 999, 0),
                        new Point3D (1544, 1005, 0),
                        new Point3D (788, 1894, 0),
                        new Point3D (789, 1892, 0),
                        new Point3D (1939, 1310, 0),
                        new Point3D (1936, 1309, 0),
                        new Point3D (1033, 1403, 0),
                        new Point3D (1023, 1410, 0)
                    };

                    for (int ix = 0; ix < list.Count; ix += 2)
                    {
                        Spawner spawner = (Spawner)FindOneItemAt(list[ix], Map.Felucca, typeof(Spawner), 2, false);
                        if (spawner != null)
                        {
                            spawner.MoveToWorld(list[ix + 1]);
                            spawner.Respawn();
                            patched++;
                        }
                    }

                    list = new()
                    {
                        new Point3D (3602, 2815, 29),
                        new Point3D (3610, 2813, 24),
                        new Point3D (3170, 672, 8),
                        new Point3D (3156, 678, 5)

                    };

                    for (int ix = 0; ix < list.Count; ix += 2)
                    {
                        Spawner spawner = (Spawner)FindOneItemAt(list[ix], Map.Felucca, typeof(Spawner), 2, false);
                        if (spawner != null)
                        {
                            spawner.MoveToWorld(list[ix + 1]);
                            spawner.Respawn();
                            patched++;
                        }
                    }
                }
                #endregion Set certain camp spawners to home range 0

                #region PatchAlignmentRegions
                if (AllShards())
                {
                    patched += PatchAlignmentRegions();

                    // now respawn lich
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.Running == true && spawner.Spawns("lich"))
                        {
                            spawner.Respawn();
                            patched++;
                        }
                }
                #endregion PatchAlignmentRegions

                #region Release items locked down on boats
                if (AllShards())
                {
                    Dictionary<Item, TownshipStone> items = new();
                    foreach (var tss in TownshipStone.AllTownshipStones)
                        if (tss is TownshipStone && tss.LockdownRegistry != null)
                            foreach (var kvp in tss.LockdownRegistry)
                                if (BaseBoat.FindBoatAt(kvp.Key) != null && tss.IsLockedDown(kvp.Key))
                                    items.Add(kvp.Key, tss);

                    foreach (var kvp in items)
                    {
                        kvp.Value.LockdownRegistry.Remove(kvp.Key);
                        kvp.Key.Movable = true;
                    }

                    patched += items.Count;
                }
                #endregion Release items locked down on boats

                #region Grandfather Herding
                if (SiegeStyleShards())
                {
                    List<PlayerMobile> pmList = new();
                    foreach (Mobile m in World.Mobiles.Values)
                        if (m is PlayerMobile pm)
                            if (pm.Skills[SkillName.Herding].Base > 70.0)
                                pmList.Add(pm);

                    foreach (PlayerMobile pm in pmList)
                    {
                        pm.SetPlayerBool(PlayerBoolTable.GrandfatheredHerding, true);
                        patched++;
                    }

                }
                #endregion Grandfather Herding

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV102

        #region PatchV103

        private static int? PatchV103(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Cleanup & Refresh Camps
                if (AllShards())
                {
                    List<BaseCamp> list = new();
                    foreach (var item in World.Items.Values)
                        if (item is BaseCamp bc && bc.Deleted == false)
                            list.Add(bc);

                    foreach (BaseCamp bc in list)
                        bc.Delete();

                    patched += list.Count;

                    // now respawn
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.Running == true && spawner.Spawns("camp"))
                        {
                            spawner.Respawn();
                            patched++;
                        }
                }
                #endregion Cleanup & Refresh Camps

                #region Angry Miner Camps
                if (SiegeStyleShards())
                {
                    List<Point3D> list = new() {
                        new Point3D(2667, 103, 0), new Point3D(1268, 2648, 0), new Point3D(1831, 1021, 0)};

                    foreach (var px in list)
                    {
                        Spawner spawner = new Spawner();
                        spawner.ObjectNamesRaw.Add("AngryMinerCampRare");
                        spawner.MinDelay = TimeSpan.FromMinutes(45);
                        spawner.MaxDelay = TimeSpan.FromHours(1);
                        spawner.HomeRange = 0;
                        spawner.MoveToWorld(px, Map.Felucca);
                        spawner.ScheduleRespawn = true;
                        patched++;
                    }
                }
                #endregion Angry Miner Camps

                #region Plows
                if (AllShards())
                {
                    List<Point3D> list = new() {
                        new Point3D(1223, 1723, 0), new Point3D(3711, 2616, 20), new Point3D(676, 1009, 0)};

                    LogHelper logger = new LogHelper(LogHelper.DataFolder("Rares Spawners.log"), false, true);
                    foreach (var px in list)
                    {
                        RaresSpawner spawner = new RaresSpawner();
                        spawner.MustSteal = true;
                        spawner.RareType = (px == new Point3D(676, 1009, 0)) ? Loot.RareType.PlowEast : Loot.RareType.PlowSouth;
                        spawner.MinDelay = TimeSpan.FromHours(48);
                        spawner.MaxDelay = TimeSpan.FromHours(96);
                        spawner.HomeRange = (px == new Point3D(676, 1009, 0)) ? 0 : 10;
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
                }
                #endregion Plows

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV103

        #region PatchV104

        private static int? PatchV104(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Cleanup & Refresh Camps
                if (AllShards())
                {
                    List<BaseCamp> list = new();
                    foreach (var item in World.Items.Values)
                        if (item is BaseCamp bc && bc.Deleted == false)
                            list.Add(bc);

                    foreach (BaseCamp bc in list)
                        bc.Delete();

                    patched += list.Count;

                    // now respawn
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.Running == true && spawner.Spawns("camp"))
                        {
                            spawner.Respawn();
                            patched++;
                        }
                }
                #endregion Cleanup & Refresh Camps

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV104

        #region PatchV105

        private static int? PatchV105(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Make System Character to Persist and Own stuff
                if (AllShards())
                {
                    PlayerMobile system = World.GetSystemAcct() as PlayerMobile;
                    Mobile AdamAnt = World.GetAdminAcct();
                    Guildstone prison = (Guildstone)FindOneItemAt(new Point3D(352, 838, 20), Map.Felucca, typeof(Guildstone));

                    // remove adam from guild
                    if (AdamAnt != null && AdamAnt.Guild != null)
                        ((Guild)AdamAnt.Guild).ResignMember(AdamAnt, force: true);
                    // make new system account guild master
                    if (prison.Guild != null)
                    {
                        ((Guild)prison.Guild).AddMember(system);
                        prison.Guildmaster = system;
                    }
                    patched++;
                }
                #endregion Make System Character to Persist and Own stuff

                #region Cleanup Recycle Bin
                // we had a bug where after 10 days we were just orphaning the container on the internal map.
                if (AllShards())
                {
                    List<Item> list = new();
                    foreach (Item item in World.Items.Values)
                        if (item != null && item.Map == Map.Internal && !item.Movable && item.IsIntMapStorage && item.Parent == null)
                            if (item is StrongBackpack sbp)
                                if (DateTime.UtcNow > sbp.Created + TimeSpan.FromDays(10))
                                    // more than 10 days old, delete it
                                    list.Add(sbp);

                    foreach (Item item in list)
                        item.Delete();

                    patched += list.Count;

                }
                #endregion Cleanup Recycle Bin

                #region Cleanup & Refresh Camps
                if (AllShards())
                {   // Delete all Siege Tents (will be replaced with addons/multis)
                    List<SiegeTent> list = new();
                    foreach (var item in World.Items.Values)
                        if (item is BaseHouse bh && bh.Deleted == false && bh is SiegeTent st)
                            list.Add(st);

                    foreach (SiegeTent st in list)
                        st.Delete();

                    patched += list.Count;
                }

                if (SiegeStyleShards())
                {   // delete angry miner camp spawners (will be replaced with dynamic camps)
                    List<Spawner> list = new();
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.Running == true && spawner.Spawns("AngryMinerCampRare"))
                        {
                            list.Add(spawner);
                            patched++;
                        }

                    foreach (Spawner spawner in list)
                        spawner.Delete();
                }

                if (AllShards())
                {   // delete all camps and respawn
                    List<BaseCamp> list = new();
                    foreach (var item in World.Items.Values)
                        if (item is BaseCamp bc && bc.Deleted == false)
                            list.Add(bc);

                    foreach (BaseCamp bc in list)
                        bc.Delete();

                    patched += list.Count;

                    // now respawn
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.Running == true && spawner.Spawns("camp"))
                        {
                            spawner.Respawn();
                            patched++;
                        }
                }
                #endregion Cleanup & Refresh Camps

                #region Respawn Aligned Creatures
                if (AllShards())
                {
                    List<Spawner> seen = new();
                    foreach (Mobile m in World.Mobiles.Values)
                        if (m is BaseCreature bc && !bc.Deleted && bc.GuildAlignment != AlignmentType.None)
                            if (bc.Spawner != null && !bc.Spawner.Deleted && bc.Spawner.Running)
                                if (!seen.Contains(bc.Spawner))
                                {
                                    bc.Spawner.RemoveObjects();
                                    bc.Spawner.ScheduleRespawn = true;
                                    seen.Add(bc.Spawner);
                                    patched++;
                                }
                }
                #endregion Respawn Aligned Creatures

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV105

        #region PatchV106

        private static int? PatchV106(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Make System Character to Persist and Own stuff
                if (AllShards())
                {
                    PlayerMobile system = World.GetSystemAcct() as PlayerMobile;
                    Mobile AdamAnt = World.GetAdminAcct();
                    Guildstone prison = (Guildstone)FindOneItemAt(new Point3D(352, 838, 20), Map.Felucca, typeof(Guildstone));

                    // remove adam from guild
                    if (AdamAnt != null && AdamAnt.Guild != null)
                        ((Guild)AdamAnt.Guild).ResignMember(AdamAnt, force: true);
                    // make new system account guild master
                    if (prison.Guild != null)
                    {
                        ((Guild)prison.Guild).AddMember(system);
                        prison.Guildmaster = system;
                    }
                    patched++;
                }
                #endregion Make System Character to Persist and Own stuff

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV106

        #region PatchV107

        private static int? PatchV107(int patchid)
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
                            spawner.SetProp = "DestinationFlags MoongateTownship";
                            spawner.Respawn();
                            patched++;
                        }
                }
                #endregion Update Moongate Wizard's Spawners

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV107

        #region PatchV108

        private static int? PatchV108(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Cleanup & Refresh Camps
                if (AllShards())
                {   // delete all camps and respawn
                    List<BaseCamp> list = new();
                    foreach (var item in World.Items.Values)
                        if (item is BaseCamp bc && bc.Deleted == false)
                            list.Add(bc);

                    foreach (BaseCamp bc in list)
                        bc.Delete();

                    patched += list.Count;

                    // now respawn
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.Running == true && spawner.Spawns(typeof(BaseCamp)))
                        {
                            spawner.ScheduleRespawn = true;
                            patched++;
                        }
                }
                #endregion Cleanup & Refresh Camps

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV108

        #region PatchV109

        private static int? PatchV109(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Cleanup & Refresh Camps
                if (AllShards())
                {
                    patched += RefreshCamps();
                }
                #endregion Cleanup & Refresh Camps

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV109

        #region PatchV110

        private static int? PatchV110(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Remove all spawned rares
                if (AllShards())
                {
                    List<Item> ToDelete = new();
                    foreach (Item item in World.Items.Values)
                        if (item is RaresSpawner raresSpawner && raresSpawner.Running)
                        {
                            IPooledEnumerable eable = Map.Felucca.GetItemsInRange(raresSpawner.Location, raresSpawner.HomeRange);
                            foreach (Item thing in eable)
                                if (thing != null && thing.Deleted == false)
                                    if (thing.GetFlag(LootType.Rare) || thing.GetItemBool(Item.ItemBoolTable.MustSteal))
                                        ToDelete.Add(thing);

                            eable.Free();
                        }
                        else if (item is Spawner spawner && spawner.Running && spawner.Name != null && spawner.Name.Contains("rares", StringComparison.OrdinalIgnoreCase))
                        {
                            Point3D px = spawner.Location;
                            foreach (Item thing in FindItemsAt(px, Map.Felucca, lenientZ: 5))
                                if (thing != null && thing.Deleted == false)
                                    if (spawner.Spawns(thing.GetType()))
                                        ToDelete.Add(thing);
                        }

                    patched += ToDelete.Count;
                    foreach (Item item in ToDelete)
                        item.Delete();
                }
                #endregion Remove all spawned rares

                #region Add stealable fruit baskets
                if (AllShards())
                {
                    List<Item> ToDelete = new();
                    List<Point3D> list = new() {
                        new Point3D(3692, 1261, 24), new Point3D(1141, 1816, 6), new Point3D(1515, 1422, 21)};

                    // first delete the existing fruit baskets                    
                    foreach (var px in list)
                        foreach (Item thing in FindItemsAt(px, Map.Felucca, lenientZ: 2))
                            if (thing is FruitBasket)
                                ToDelete.Add(thing);

                    foreach (var thing in ToDelete)
                    {
                        thing.Delete();
                        patched++;
                    }

                    LogHelper logger = new LogHelper(LogHelper.DataFolder("Rares Spawners.log"), false, true);
                    foreach (var px in list)
                    {
                        RaresSpawner spawner = new RaresSpawner();
                        spawner.MustSteal = true;
                        spawner.RareType = Loot.RareType.FruitBasket;
                        spawner.MinDelay = TimeSpan.FromHours(48);
                        spawner.MaxDelay = TimeSpan.FromHours(96);
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
                }
                #endregion Add stealable fruit baskets

                #region Cleanup
                // delete pots here (pots and fruit baskets at same location.)
                if (AllShards())
                {
                    List<Item> ToDelete = new();
                    List<Point3D> list = new() {
                        new Point3D(1512, 1428, 21),new Point3D(1515, 1422, 21),
                    };
                    foreach (var px in list)
                        foreach (Item thing in FindItemsAt(px, Map.Felucca, lenientZ: 2))
                            if (thing != null && thing.Deleted == false && thing.ItemID == 0x9E3)
                                ToDelete.Add(thing);

                    foreach (var thing in ToDelete)
                    {
                        thing.Delete();
                        patched++;
                    }
                }
                #endregion Cleanup

                #region Enable Client Enforcement (Warning only)
                if (AllShards())
                {
                    CoreAI.ForceGMNCUO = true;
                    CoreAI.ForceGMNRZR = true;
                    CoreAI.WarnBadGMNCUO = true;
                    CoreAI.WarnBadGMNRZR = true;
                    patched += 4;
                }
                #endregion Enable Client Enforcement (Warning only)

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV110

        #region PatchV111

        private static int? PatchV111(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Remove all spawned rares
                if (SiegeStyleShards())
                {
                    List<Item> ToDelete = new();
                    foreach (Item item in World.Items.Values)
                        if (item is RaresSpawner raresSpawner && raresSpawner.Running)
                        {
                            IPooledEnumerable eable = Map.Felucca.GetItemsInRange(raresSpawner.Location, raresSpawner.HomeRange);
                            foreach (Item thing in eable)
                                if (thing != null && thing.Deleted == false)
                                    if (thing.GetFlag(LootType.Rare) || thing.GetItemBool(Item.ItemBoolTable.MustSteal))
                                        ToDelete.Add(thing);

                            eable.Free();
                        }
                        else if (item is Spawner spawner && spawner.Running && spawner.Name != null && spawner.Name.Contains("rares", StringComparison.OrdinalIgnoreCase))
                        {
                            Point3D px = spawner.Location;
                            foreach (Item thing in FindItemsAt(px, Map.Felucca, lenientZ: 5))
                                if (thing != null && thing.Deleted == false)
                                    if (spawner.Spawns(thing.GetType()))
                                        ToDelete.Add(thing);
                        }

                    patched += ToDelete.Count;
                    foreach (Item item in ToDelete)
                        item.Delete();
                }
                #endregion Remove all spawned rares

                #region Add stealable fruit baskets
                if (SiegeStyleShards())
                {
                    List<Item> ToDelete = new();
                    List<Point3D> list = new() {
                        new Point3D(3692, 1261, 24), new Point3D(1141, 1816, 6), new Point3D(1515, 1422, 21)};

                    // first delete the existing fruit baskets                    
                    foreach (var px in list)
                        foreach (Item thing in FindItemsAt(px, Map.Felucca, lenientZ: 2))
                            if (thing is FruitBasket)
                                ToDelete.Add(thing);

                    foreach (var thing in ToDelete)
                    {
                        thing.Delete();
                        patched++;
                    }

                    LogHelper logger = new LogHelper(LogHelper.DataFolder("Rares Spawners.log"), false, true);
                    foreach (var px in list)
                    {
                        RaresSpawner spawner = new RaresSpawner();
                        spawner.MustSteal = true;
                        spawner.RareType = Loot.RareType.FruitBasket;
                        spawner.MinDelay = TimeSpan.FromHours(48);
                        spawner.MaxDelay = TimeSpan.FromHours(96);
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
                }
                #endregion Add stealable fruit baskets

                #region Cleanup
                // delete pots here (pots and fruit baskets at same location.)
                if (SiegeStyleShards())
                {
                    List<Item> ToDelete = new();
                    List<Point3D> list = new() {
                        new Point3D(1512, 1428, 21),new Point3D(1515, 1422, 21),
                    };
                    foreach (var px in list)
                        foreach (Item thing in FindItemsAt(px, Map.Felucca, lenientZ: 2))
                            if (thing != null && thing.Deleted == false && thing.ItemID == 0x9E3)
                                ToDelete.Add(thing);

                    foreach (var thing in ToDelete)
                    {
                        thing.Delete();
                        patched++;
                    }
                }
                #endregion Cleanup

                #region Enable Client Enforcement (Warning only)
                if (AllShards())
                {
                    CoreAI.ForceGMNCUO = true;
                    CoreAI.ForceGMNRZR = true;
                    CoreAI.WarnBadGMNCUO = true;
                    CoreAI.WarnBadGMNRZR = true;
                    patched += 4;
                }
                #endregion Enable Client Enforcement (Warning only)

                #region Cleanup OnSpawner items (will force decay)
                if (AllShards())
                {
                    foreach (Item item in World.Items.Values)
                    {
                        if (item != null && item.GetItemBool(Item.ItemBoolTable.OnSpawner))
                        {
                            item.SetItemBool(Item.ItemBoolTable.OnSpawner, false);
                            patched++;
                        }
                    }
                }
                #endregion Cleanup OnSpawner items (will force decay)

                #region Cleanup & Refresh Camps
                if (AllShards())
                {
                    patched += RefreshCamps();
                }
                #endregion Cleanup & Refresh Camps

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV111

        #region PatchV112

        private static int? PatchV112(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Resolve spinning Township NPCs
                if (AllShards())
                {
                    foreach (Mobile m in World.Mobiles.Values)
                        if (m != null && !m.Deleted && m is ITownshipNPC)
                            if (m is BaseCreature bc && bc.CantWalkLand)
                                bc.RangeHome = 0;
                }
                #endregion Resolve spinning Township NPCs

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV112

        #region PatchV113

        private static int? PatchV113(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Remove all normal cotton plants and carrots from spawners
                if (AllShards())
                {
                    List<string> toRemove = new() { "carrot", "cottonplant" };
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.Running)
                            foreach (var ele in toRemove)
                                if (spawner.Spawns(ele))
                                {
                                    int index = spawner.IndexOf(ele);

                                    if (index != -1)
                                    {
                                        spawner.RemoveObjects();
                                        spawner.ScheduleRespawn = true;
                                        spawner.RemoveAtIndex(index);
                                        patched++;
                                    }
                                }
                }
                #endregion Remove all normal cotton plants and carrots from spawners

                #region Load Farm Produce
                if (AllShards())
                {
                    patched += LoadGridSpawners();
                }
                #endregion Load Farm Produce

                #region Remove Adam from Watch List
                if (AllShards())
                    if (World.GetAdminAcct() != null)
                    {
                        Server.Accounting.Account acct = World.GetAdminAcct().Account as Server.Accounting.Account;
                        if (acct != null)
                        {
                            acct.Watched = false;
                            patched++;
                        }
                    }
                #endregion Remove Adam from Watch List

                #region Update treasure chest spawners that seem too safe
                if (AllShards())
                {
                    List<Spawner> spawners = new();
                    List<string> chestTypes = new() { "L2TreasureChest", "L3TreasureChest", "L4TreasureChest", "L5TreasureChest", "L6TreasureChest", };
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.Map == Map.Felucca && spawner.Running)
                            foreach (string type in chestTypes)
                                if (spawner.Spawns(type))
                                    if (!spawners.Contains(spawner))
                                        spawners.Add(spawner);

                    // okay, now we have a list of spawners that spawn higher level chests
                    //  first, reset the respawn times
                    foreach (Spawner spawner in spawners)
                    {
                        spawner.MinDelay = TimeSpan.FromMinutes(60);
                        spawner.MaxDelay = TimeSpan.FromMinutes(80);
                        patched++;
                    }

                    // Now fag for review those that seem too safe
                    List<Spawner> dangerous = new();
                    foreach (Spawner test in spawners)
                    {
                        IPooledEnumerable eable = Map.Felucca.GetMobilesInRange(test.Location, 13);
                        foreach (Mobile m in eable)
                        {
                            if (m is BaseCreature bc && !bc.Controlled)
                                if (bc.FightMode.HasFlag(FightMode.All))
                                {   // seems aggressive
                                    dangerous.Add(test);
                                    break;
                                }
                        }
                        eable.Free();
                    }

                    //LogHelper logger = new LogHelper("safespawners.log", true, true, true);
                    //PlayerMobile pm = (PlayerMobile)World.GetAdminAcct();
                    //pm.JumpList = new ArrayList();
                    //pm.JumpIndex = 0;
                    var safe = spawners.Where(p => dangerous.All(p2 => p2 != p));
                    foreach (var spawner in safe)
                    {   // less risk, more delay
                        spawner.MinDelay = TimeSpan.FromDays(1.0);
                        spawner.MaxDelay = TimeSpan.FromDays(1.5);
                        //logger.Log("{" + "new Point3D" + spawner.Location.ToString() + "},");
                        //pm.JumpList.Add(spawner.Location);
                    }
                    //logger.Finish();

                    // less risk, add some fun!
                    Dictionary<Point3D, SpawnerAction> locations = new() {
                        {new Point3D(3130, 3409, -1), SpawnerAction.Sea},
                        {new Point3D(5715, 59, 5), SpawnerAction.MushroomTrap},
                        {new Point3D(5698, 60, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(5724, 75, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(5731, 91, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(5702, 80, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(5399, 1859, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(5186, 598, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(5413, 1932, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(5325, 960, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(5951, 216, 29), SpawnerAction.MushroomTrap},
                        {new Point3D(5228, 832, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(5326, 785, 1), SpawnerAction.MushroomTrap},
                        {new Point3D(5151, 743, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(5526, 45, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(5448, 159, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(5752, 101, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(5833, 244, -3), SpawnerAction.MushroomTrap},
                        {new Point3D(5497, 626, 25), SpawnerAction.MushroomTrap},
                        {new Point3D(5498, 692, 20), SpawnerAction.MushroomTrap},
                        {new Point3D(5390, 963, 16), SpawnerAction.MushroomTrap},
                        {new Point3D(5757, 1332, -3), SpawnerAction.MushroomTrap},
                        {new Point3D(5457, 925, 20), SpawnerAction.MushroomTrap},
                        {new Point3D(5398, 982, 5), SpawnerAction.MushroomTrap},
                        {new Point3D(5957, 216, 22), SpawnerAction.MushroomTrap},
                        {new Point3D(5958, 216, 22), SpawnerAction.MushroomTrap},
                        {new Point3D(5319, 103, 15), SpawnerAction.GasTrap},
                        {new Point3D(5320, 103, 15), SpawnerAction.GasTrap},
                        {new Point3D(5154, 694, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(5245, 173, 15), SpawnerAction.GasTrap},
                        {new Point3D(5244, 173, 15), SpawnerAction.GasTrap},
                        {new Point3D(5243, 173, 15), SpawnerAction.GasTrap},
                        {new Point3D(5614, 1837, 0), SpawnerAction.MushroomTrap},
                        {new Point3D(1245, 852, 16), SpawnerAction.MushroomTrap},
                        {new Point3D(1246, 852, 16), SpawnerAction.MushroomTrap},
                    };

                    foreach (var kvp in locations)
                    {
                        Spawner spawner = (Spawner)FindOneItemAt(kvp.Key, Map.Felucca, typeof(Spawner), 2, false);
                        if (spawner != null)
                        {
                            Spawner trapSpawner = new Spawner();
                            if (spawner.HomeRange == 0)
                                trapSpawner.HomeRange = 2; // if the chest is a fixed location, put the trap near it
                            else
                                trapSpawner.HomeRange = Math.Max(3, spawner.HomeRange);

                            // have the number of traps reflect the number of things we are protecting
                            trapSpawner.Count = (kvp.Value == SpawnerAction.MushroomTrap ? 3 : 1) * spawner.ObjectCount;

                            trapSpawner.SpawnerFlags = SpawnFlags.ClearPath;

                            if (kvp.Value == SpawnerAction.MushroomTrap)
                            {
                                trapSpawner.ObjectNamesRaw = new ArrayList() { "MushroomTrap" };
                                trapSpawner.SetProp = "PopOnce true";
                            }
                            else if (kvp.Value == SpawnerAction.GasTrap)
                            {
                                trapSpawner.ObjectNamesRaw = new ArrayList() { "GasTrap" };
                                trapSpawner.SetProp = "PopOnce true";
                            }
                            else if (kvp.Value == SpawnerAction.Sea)
                                trapSpawner.ObjectNamesRaw = new ArrayList() { "SeaSerpent" };

                            Point3D px = Spawner.GetSpawnPosition(Map.Felucca, kvp.Key, homeRange: trapSpawner.HomeRange, sflags: SpawnFlags.ClearPath, trapSpawner);
                            trapSpawner.MoveToWorld(px, Map.Felucca);
                            trapSpawner.ScheduleRespawn = true;
                            patched++;
                        }
                    }

                }
                #endregion Update treasure chest spawners that seem too safe

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV113

        #region PatchV114

        private static int? PatchV114(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Remove Adam from Watch List
                if (AllShards())
                    if (World.GetAdminAcct() is PlayerMobile pm)
                    {

                        pm.SetPlayerBool(PlayerBoolTable.Watched, false);
                        Server.Accounting.Account acct = pm.Account as Server.Accounting.Account;
                        if (acct != null)
                        {
                            acct.WatchExpire = DateTime.MinValue;
                            acct.Watched = false;
                            patched++;
                        }
                    }
                #endregion Remove Adam from Watch List

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV114

        #region PatchV115

        private static int? PatchV115(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Replace Barrels, Tubs, and Buckets with fillable ones
                List<KeyValuePair<int, Point3D>> placement = new();
                List<Spawner> spawners = new();
                List<int> interest = new()
                {
                    // barrels: empty, full
                    0xE77, 0x154D,
                    // tubs: empty, full
                    0xE83, 0xE7B,
                    // buckets: empty, full, static
                    0x14E0, 0x2004, 0xFFA
                };

                foreach (Item item in World.Items.Values)
                {
                    if (InWorld(item))
                    {
                        List<Item> items = FindItemsAt(item.Location, item.Map);
                        foreach (Item test_item in items)
                            if (test_item.Parent == null && !test_item.IsLockedDown && !test_item.IsSecure)
                                switch (test_item.ItemID)
                                {
                                    // barrels: full
                                    case 0x154D:
                                        FixDeco(interest, items);
                                        Spawner spawner = null;
                                        AddSpawner(test_item.Location, new List<string>() { "barrel" }, coreSpawn: false, ref spawner, count: 1, homeRange: 0);
                                        if (spawner != null)
                                        {
                                            Barrel barrel = new();
                                            if (barrel != null)
                                            {
                                                int quantity = Utility.RandomMinMax(5, barrel.MaxQuantity);
                                                spawner.SetProp = string.Format("Z {0};Quantity {1}", test_item.Z, quantity);
                                                spawner.MinDelay = TimeSpan.FromDays(1); spawner.MaxDelay = TimeSpan.FromDays(1.5);
                                                patched++;
                                                barrel.Delete();
                                            }
                                        }
                                        break;

                                    // tub: full
                                    case 0xE7B:
                                        FixDeco(interest, items);
                                        spawner = null;
                                        AddSpawner(test_item.Location, new List<string>() { "tub" }, coreSpawn: false, ref spawner, count: 1, homeRange: 0);
                                        if (spawner != null)
                                        {
                                            Tub tub = new();
                                            if (tub != null)
                                            {
                                                int quantity = Utility.RandomMinMax(5, tub.MaxQuantity);
                                                spawner.SetProp = string.Format("Z {0};Quantity {1}", test_item.Z, quantity);
                                                spawner.MinDelay = TimeSpan.FromDays(1); spawner.MaxDelay = TimeSpan.FromDays(1.5);
                                                patched++;
                                                tub.Delete();
                                            }
                                        }
                                        break;

                                    // bucket: full
                                    case 0x2004:
                                    case 0xFFA:
                                        FixDeco(interest, items);
                                        spawner = null;
                                        AddSpawner(test_item.Location, new List<string>() { "bucket" }, coreSpawn: false, ref spawner, count: 1, homeRange: 0);
                                        if (spawner != null)
                                        {
                                            Bucket bucket = new();
                                            if (bucket != null)
                                            {
                                                int quantity = Utility.RandomMinMax(5, bucket.MaxQuantity);
                                                // Z+1 here to lift the bucket out of the bushes
                                                spawner.SetProp = string.Format("Z {0};Quantity {1}", test_item.Z + 1, quantity);
                                                spawner.MinDelay = TimeSpan.FromDays(2); spawner.MaxDelay = TimeSpan.FromDays(2.5);
                                                patched++;
                                                bucket.Delete();
                                            }
                                        }
                                        break;

                                }
                    }
                }
                #endregion Replace Barrels, Tubs, and Buckets with fillable ones

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV115

        #region PatchV116

        private static int? PatchV116(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Enable Lizardman camps
                if (SiegeStyleShards())
                {
                    List<Point3D> list = new() { new Point3D(3000, 3590, 15), new Point3D(1847, 3088, 0) };
                    foreach (var px in list)
                    {
                        Spawner spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                        if (spawner != null)
                            if (EnableSpawner(ref spawner))
                                patched++;
                    }
                }
                #endregion Enable Lizardman camps

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV116

        #region PatchV117

        private static int? PatchV117(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Patch players that missed out of the Murder Reprieve, also log
                if (SiegeStyleShards())
                {
                    foreach (Mobile m in World.Mobiles.Values)
                        if (m is PlayerMobile pm && !pm.Deleted && pm.FactionPlayerState != null)
                            if ((pm.ShortTermMurders > 4 || pm.LongTermMurders > 4) && pm.GetPlayerBool(PlayerBoolTable.MurderReprieve) == false)
                            {
                                Server.Factions.Faction.MurderReprieve(World.GetSystemAcct(), pm);
                                patched++;
                            }

                    LogHelper logger = new LogHelper("Murder Reprieve.log", false, true);
                    foreach (Mobile m in World.Mobiles.Values)
                        if (m is PlayerMobile pm && !pm.Deleted && pm.FactionPlayerState != null && pm.GetPlayerBool(PlayerBoolTable.MurderReprieve) == true)
                        {
                            if (pm.GetPlayerBool(PlayerBoolTable.MurderReprieveRescinded) == false)
                            {
                                logger.Log(string.Format("{0} : Joining this faction grants you a one-time murder reprieve.", pm));
                                logger.Log(string.Format("{0} : old STM:{1}, new STM:{2}", pm, pm.ReprieveShortTermMurders, pm.ShortTermMurders));
                                logger.Log(string.Format("{0} : old LTM:{1}, new LTM:{2}", pm, pm.ReprieveLongTermMurders, pm.LongTermMurders));
                            }
                            else
                            {
                                logger.Log(string.Format("{0} : Your reprieve has been lost for returning to your murderous ways", pm));
                                logger.Log(string.Format("{0} : current STM:{1}", pm, pm.ShortTermMurders));
                                logger.Log(string.Format("{0} : current LTM:{1}", pm, pm.LongTermMurders));
                            }
                        }
                    logger.Finish();
                }
                #endregion Patch players that missed out of the Murder Reprieve, also log

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV117

        #region PatchV118

        private static int? PatchV118(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Enable Breeding / Followers Max
                if (SiegeStyleShards())
                {
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.BreedingEnabled);
                    patched++;

                    foreach (Mobile m in World.Mobiles.Values)
                        if (m is PlayerMobile playerMobile && !playerMobile.Deleted)
                            playerMobile.FollowersMax = 5;
                }
                #endregion Enable Breeding / Followers Max

                #region Enable Tree of Knowledge and kukui nut spawners
                if (SiegeStyleShards())
                {
                    List<Point3D> list = new() { new Point3D(1069, 2260, 2), new Point3D(1070, 2262, 2) };
                    foreach (var px in list)
                    {
                        Spawner spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                        if (spawner != null)
                            if (EnableSpawner(ref spawner))
                                patched++;
                    }

                }
                #endregion Enable Tree of Knowledge and kukui nut spawners

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV118

        #region PatchV119

        private static int? PatchV119(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Disable Breeding / Followers Max
                if (SiegeStyleShards())
                {
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.BreedingEnabled);
                    patched++;

                    foreach (Mobile m in World.Mobiles.Values)
                        if (m is PlayerMobile playerMobile && !playerMobile.Deleted)
                            playerMobile.FollowersMax = 8;
                }
                #endregion Disable Breeding / Followers Max

                #region Disable Tree of Knowledge and kukui nut spawners
                if (SiegeStyleShards())
                {
                    List<Point3D> list = new() { new Point3D(1069, 2260, 2), new Point3D(1070, 2262, 2) };
                    foreach (var px in list)
                        if (DisableSpawner(px))
                            patched++;
                }
                #endregion Disable Tree of Knowledge and kukui nut spawners

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV119

        #region PatchV120

        private static int? PatchV120(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Disable Pub15 Lizardman camps
                if (SiegeStyleShards())
                {
                    List<Point3D> list = new() { new Point3D(3000, 3590, 15), new Point3D(1847, 3088, 0) };
                    foreach (var px in list)
                        if (DisableSpawner(px))
                            patched++;
                }
                #endregion Disable Pub15 Lizardman camps

                #region Add Lizardman Camps
                if (SiegeStyleShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && (spawner.Spawns("OrcCamp") || spawner.Spawns("RatCamp")))
                        {
                            List<string> list = new List<string>(spawner.ObjectNamesRaw.Cast<string>().ToList());
                            list = list.Select(s => s.Replace("Orccamp:Ratcamp", "OrcCamp:RatCamp:LizardmanCamp")).ToList();
                            list = list.Select(s => s.Replace("RatCamp:OrcCamp", "OrcCamp:RatCamp:LizardmanCamp")).ToList();
                            spawner.ObjectNamesRaw = new ArrayList(list);
                            spawner.RemoveObjects();
                            spawner.ScheduleRespawn = true;
                            patched++;
                        }
                }
                #endregion Add Lizardman Camps

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV120

        #region PatchV121

        private static int? PatchV121(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Unbond pets
                if (SiegeStyleShards())
                {
                    foreach (Mobile m in World.Mobiles.Values)
                        if (m is BaseCreature bc && bc.Deleted == false && bc.IsBonded == true)
                        {
                            bc.IsBonded = false;
                            patched++;
                        }
                }
                #endregion Unbond pets


                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV121

        #region PatchV122

        private static int? PatchV122(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Mark Player Crafted items as Origin.Player
                if (AllShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item != null && item.Deleted == false)
                            if (item.PlayerCrafted)
                            {
                                item.Origin = Item.Genesis.Player;
                                patched++;
                            }
                }
                #endregion Mark Player Crafted items as Origin.Player

                #region Total Respawn (pack enchanted scrolls)
                if (SiegeStyleShards())
                    patched += ScheduleRespawn();
                #endregion Total Respawn (pack enchanted scrolls)

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV122

        #region PatchV123

        private static int? PatchV123(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Tag Legacy Magic Loot
                if (AllShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item != null && item.Deleted == false)
                            if (!item.PlayerCrafted && IsMagicLoot(item))
                            {
                                item.Origin = Item.Genesis.Legacy;
                                patched++;
                            }
                }
                #endregion Tag Legacy Magic Loot

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV123

        #region PatchV124

        private static int? PatchV124(int patchid)
        {

            if (TestCenterShards(quiet: true) && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Enable Tree of Knowledge and kukui nut spawners
                if (TestCenterShards(quiet: true))
                {
                    EchoOut("Patching Test Center.", ConsoleColor.Green);

                    List<Point3D> list = new() { new Point3D(1069, 2260, 2), new Point3D(1070, 2262, 2) };
                    foreach (var px in list)
                    {
                        Spawner spawner = (Spawner)FindOneItemAt(px, Map.Felucca, typeof(Spawner), 2, false);
                        if (spawner != null)
                            if (EnableSpawner(ref spawner))
                                patched++;
                    }
                    // turn on breeding
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.BreedingEnabled);
                }
                #endregion Enable Tree of Knowledge and kukui nut spawners

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV124

        #region PatchV125

        private static int? PatchV125(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Farmables
                if (AllShards())
                {   // globally reduce these
                    CropManagementConsole.FarmableCotton = 0.1;
                    patched += CropManagementConsole.CottonPlots;
                    CropManagementConsole.FarmableFlax = 0.1;
                    patched += CropManagementConsole.FlaxPlots;

                    // add guard dogs
                    Spawner spawner = null;
                    foreach (Item item in World.Items.Values)
                        if (item is GridSpawner gs)
                            if (gs.Spawns("FarmableCotton") || gs.Spawns("FarmableFlax"))
                                if (AddSpawner(new Point3D(gs.X + 1, gs.Y + 1, gs.Z), new List<string>() { "FarmDog" }, coreSpawn: false, ref spawner, count: 1, homeRange: 10))
                                {
                                    spawner.MinDelay = TimeSpan.FromSeconds(18);
                                    spawner.MaxDelay = TimeSpan.FromSeconds(30);
                                    patched++;
                                }
                }
                #endregion Farmables

                #region Enable BODs
                if (SiegeStyleShards())
                {
                    BulkOrderControl.SystemEnabled = true; patched++;
                    BulkOrderControl.EnabledFlags |= BulkOrderFlags.Smith; patched++;
                    BulkOrderControl.EnabledFlags |= BulkOrderFlags.Tailor; patched++;
                    BulkOrderControl.LargeEnabled = false; patched++;
                    BulkOrderControl.GoldRewards = true; patched++;
                    BulkOrderControl.FameRewards = true; patched++;
                    BulkOrderControl.RewardsGump = false; patched++;
                    BulkOrderControl.OfferOnTurnIn = true; patched++;
                }
                #endregion Enable BODs

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV125

        #region PatchV126

        private static int? PatchV126(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Replace Large BODS with Small
                if (SiegeStyleShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is LargeBOD lb)
                        {
                            Mobile sysact = World.GetSystemAcct();
                            if (sysact != null)
                            {
                                Nuke.SetAllSkills(sysact, 70.1);
                                SmallBOD bo = lb.System.ConstructSBOD(0, false, BulkMaterialType.None, 0, null, 0, 0);
                                bo.Randomize(sysact);
                                Nuke.SetAllSkills(sysact, 100.0);
                                Utility.ReplaceItem(newItem: bo, oldItem: item, copyProperties: false);
                                item.Delete();
                                patched++;
                            }
                        }
                }
                #endregion Replace Large BODS with Small

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV126

        #region PatchV127

        private static int? PatchV127(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Bring Town Criers home
                if (SiegeStyleShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.Running)
                            if (spawner.Spawns("TownCrier"))
                            {
                                spawner.RemoveObjects();
                                spawner.ScheduleRespawn = true;
                                patched++;
                            }

                }
                #endregion Bring Town Criers home

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV127

        #region PatchV128

        private static int? PatchV128(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Start decay on all angry miner camps
                if (SiegeStyleShards())
                {
                    foreach (Item item in World.Items.Values)
                    {
                        if (item != null)
                        {
                            if (item.GetType() == typeof(MagicBox) && item is LockableContainer lc)
                            {
                                // no houses
                                if (BaseMulti.Find(item.Location, item.Map) != null || item.Parent != null)
                                    continue;

                                foreach (Item reward in lc.Items)
                                    if ((reward is AncientSmithyHammer || reward is GargoylesPickaxe) && Utility.RandomBool())
                                    {   // kick off decay
                                        lc.Locked = false;
                                        patched++;
                                    }
                            }
                        }
                    }
                }
                #endregion Start decay on all angry miner camps

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV128

        #region PatchV129

        private static int? PatchV129(int patchid)
        {

            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Respawn Real Estate Brokers
                if (SiegeStyleShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.Running)
                            if (spawner.Spawns("RealEstateBroker"))
                            {
                                spawner.RemoveObjects();
                                spawner.ScheduleRespawn = true;
                                patched++;
                            }
                }
                #endregion Respawn Real Estate Brokers

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV129

        #region PatchV130

        private static int? PatchV130(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Wipe Factions and respawn all BaseVendor
                if (SiegeStyleShards())
                {
                    Server.Factions.Generator.DegenerateResult result = Server.Factions.Generator.DegenerateFactions();
                    patched += result.TotalCount();

                    foreach (Mobile m in World.Mobiles.Values)
                        if (m is BaseVendor bv && bv.Spawner is Spawner spawner)
                            if (spawner.Running)
                            {
                                spawner.RemoveObjects();
                                spawner.ScheduleRespawn = true;
                                patched++;
                            }
                }
                #endregion Wipe Factions and respawn all BaseVendor

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV130

        #region PatchV131

        private static int? PatchV131(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Restore grandfathered war horses
                if (SiegeStyleShards())
                {
                    // data from "War Horse crib.log" (see nuke)
                    List<WHCfg> list = new() {
                        new WHCfg(typeof(CoMWarHorse),53,"a war horse",0x00004624,26.8,39.2,39.5),
                        new WHCfg(typeof(CoMWarHorse),51,"slut",0x000081CE,28.2,100,100),
                        new WHCfg(typeof(CoMWarHorse),53,"a war horse",0x00004624,26.6,35.9,39),
                        new WHCfg(typeof(CoMWarHorse),54,"a war horse",0x00000E03,25.7,33.4,36.8),
                        new WHCfg(typeof(CoMWarHorse),55,"a war horse",0x00000E03,26.9,33.7,32.5),
                        new WHCfg(typeof(CoMWarHorse),55,"a war horse",0x000235B9,29,36.7,33.3),
                        new WHCfg(typeof(CoMWarHorse),54,"house",0x000081CE,30.7,77.5,100),
                        new WHCfg(typeof(CoMWarHorse),53,"fest",0x000081CE,27.1,98.8,100),
                        new WHCfg(typeof(CoMWarHorse),54,"Atreyu",0x00004C50,31.1,100,90.8),
                        new WHCfg(typeof(CoMWarHorse),55,"Blue",0x0000BD48,27.3,31.4,41.3),
                    };

                    // find the owner, create the war horse, and configure, then force-stable
                    foreach (var cfg in list)
                        if (World.FindMobile((Serial)cfg.m_control_master) is PlayerMobile pm)
                        {
                            object o = Activator.CreateInstance(cfg.m_type);
                            if (o is CoMWarHorse pet)
                            {
                                // basics
                                pet.Controlled = true;
                                pet.ControlMaster = pm;
                                pet.Name = cfg.m_name;

                                // stats
                                pet.RawInt = cfg.m_int;

                                // skills
                                pet.SetSkill(SkillName.MagicResist, cfg.m_magic_resist);
                                pet.SetSkill(SkillName.Tactics, cfg.m_tactics);
                                pet.SetSkill(SkillName.Wrestling, cfg.m_wrestling);

                                // stable!
                                pet.ControlTarget = null;
                                pet.ControlOrder = OrderType.Stay;
                                pet.Internalize();

                                pet.SetControlMaster(null);
                                pet.SummonMaster = null;

                                pet.IsAnimalTrainerStabled = true;

                                if (AnimalTrainer.Table.ContainsKey(pm))
                                {
                                    if (!AnimalTrainer.Table[pm].Contains(pet))
                                        AnimalTrainer.Table[pm].Add(pet);
                                }
                                else
                                    AnimalTrainer.Table.Add(pm, new List<BaseCreature>() { pet });

                                pet.LastStableChargeTime = DateTime.UtcNow;

                                pet.SetCreatureBool(CreatureBoolTable.StableHold, true);
                                pet.StableBackFees = AnimalTrainer.UODayChargePerPet(pm);

                                patched++;
                            }
                        }
                }
                #endregion Restore grandfathered war horses

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV131

        #region PatchV132

        private static int? PatchV132(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Respawn world to fix creatures spawning underwater
                if (AllShards())
                    patched += ScheduleRespawn();
                #endregion Respawn world to fix creatures spawning underwater

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV132

        #region PatchV133

        private static int? PatchV133(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Patch template mobile and item reference counts
                if (AllShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && !spawner.Deleted && spawner.TemplateEnabled)
                            if (spawner.TemplateMobile is Mobile m && !m.Deleted)
                            {
                                m.SpawnerTempRefCount = 1;
                                if (m.SpawnerTempMob == false)
                                    m.SpawnerTempMob = true;
                                patched++;
                            }
                            else if (spawner.TemplateItem is Item i && !i.Deleted)
                            {
                                i.SpawnerTempRefCount = 1;
                                if (i.SpawnerTempItem == false)
                                    i.SpawnerTempItem = true;
                                patched++;
                            }

                }
                #endregion Patch template mobile and item reference counts

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV133

        #region PatchV134

        private static int? PatchV134(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Replace xmas trees and deeds with xml versions, and add music to them
                if (SiegeStyleShards())
                {
                    patched += ReplaceLockdowns(new ReplaceEntry[]
                    {
                        new ReplaceEntry(typeof(HolidayDeed), -1, typeof(ChristmasTreeAddonDeed), null),
                        new ReplaceEntry(typeof(ChristmasTreeDeed), -1, typeof(ChristmasTreeAddonDeed), null),
                        new ReplaceEntry(typeof(HolidayTree), 0x0CD7, typeof(ChristmasTreeAddon), new object[] { 0 }),
                        new ReplaceEntry(typeof(HolidayTree), 0x1B7E, typeof(ChristmasTreeAddon), new object[] { 1 }),
                    });

                    List<Item> music = new();
                    object info = null;
                    List<string> names = new() {
                        ". . It's Beginning to Look a Lot Like Christmas",
                        ". . O Tannembaum",
                        ". . Deck the Halls",
                        ". . Christmas Time is Here",
                    };
                    foreach (var name in names)
                        if (MusicBox.FindSong(name.Split(), out info))
                            music.Add(((KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo>)info).Key);

                    foreach (Item item in World.Items.Values)
                    {
                        if (item is ChristmasTreeAddonDeed deed)
                        {
                            deed.SheetMusic = music[Utility.Random(music.Count)];
                            deed.DelayBetween = 3600 * 1000; // every hour
                            deed.RangeEnter = 5;
                            deed.RangeExit = 15;
                            patched++;
                        }
                        else if (item is ChristmasTreeAddon addon)
                        {
                            addon.SheetMusic = music[Utility.Random(music.Count)];
                            addon.DelayBetween = 3600 * 1000; // every hour
                            addon.RangeEnter = 5;
                            addon.RangeExit = 15;
                            patched++;
                        }
                    }
                }
                #endregion Replace xmas trees and deeds with xml versions, and add music to them

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV134

        #region PatchV135

        private static int? PatchV135(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Replace xmas trees and deeds with xml versions, and add music to them
                if (AllShards())
                {
                    patched += ReplaceLockdowns(new ReplaceEntry[]
                    {
                        new ReplaceEntry(typeof(HolidayDeed), -1, typeof(ChristmasTreeAddonDeed), null),
                        new ReplaceEntry(typeof(ChristmasTreeDeed), -1, typeof(ChristmasTreeAddonDeed), null),
                        new ReplaceEntry(typeof(HolidayTree), 0x0CD7, typeof(ChristmasTreeAddon), new object[] { 0 }),
                        new ReplaceEntry(typeof(HolidayTree), 0x1B7E, typeof(ChristmasTreeAddon), new object[] { 1 }),
                    });

                    // this exact music only available on Siege
                    List<Item> music = new();
                    object info = null;
                    List<string> names = new() {
                        ". . It's Beginning to Look a Lot Like Christmas",
                        ". . O Tannembaum",
                        ". . Deck the Halls",
                        ". . Christmas Time is Here",
                    };
                    foreach (var name in names)
                        if (MusicBox.FindSong(name.Split(), out info))
                            music.Add(((KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo>)info).Key);

                    if (music.Count > 0)
                        foreach (Item item in World.Items.Values)
                        {
                            if (item is ChristmasTreeAddonDeed deed)
                            {
                                if (deed.SheetMusic == null)    // don't change existing music
                                    deed.SheetMusic = music[Utility.Random(music.Count)];
                                deed.DelayBetween = 3600 * 1000; // every hour
                                deed.DelayBetween += Utility.RandomList(Utility.Random(20), -Utility.Random(20)); // random offset from other trees in the area
                                deed.RangeEnter = 5;
                                deed.RangeExit = 15;
                                patched++;
                            }
                            else if (item is ChristmasTreeAddon addon)
                            {
                                if (addon.SheetMusic == null)   // don't change existing music
                                    addon.SheetMusic = music[Utility.Random(music.Count)];
                                addon.DelayBetween = 3600 * 1000; // every hour
                                addon.DelayBetween += Utility.RandomList(Utility.Random(20), -Utility.Random(20)); // random offset from other trees in the area
                                addon.RangeEnter = 5;
                                addon.RangeExit = 15;
                                patched++;
                            }
                        }
                }
                #endregion Replace xmas trees and deeds with xml versions, and add music to them

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV135

        #region PatchV136

        private static int? PatchV136(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Set all xmas trees, deeds, and motion controllers to RangeExit -1 (no stop on exit)
                if (AllShards())
                {
                    foreach (Item item in World.Items.Values)
                    {
                        if (item is MusicMotionController mmc)
                        {
                            mmc.RangeExit = -1;
                            patched++;
                        }
                        else if (item is ChristmasTreeAddonDeed deed)
                        {
                            deed.RangeExit = -1;
                            patched++;
                        }
                        else if (item is ChristmasTreeAddon addon)
                        {
                            addon.RangeExit = -1;
                            patched++;
                        }
                    }
                }
                #endregion Set all xmas trees, deeds, and motion controllers to RangeExit -1 (no stop on exit)

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV136

        #region PatchV137

        private static int? PatchV137(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Unbless all the crap I asked to be unblessed
                if (AllShards())
                {
                    foreach (Mobile m in World.Mobiles.Values)
                    {
                        if (m is Mobile && m.Blessed)
                        {
                            m.Blessed = false;
                            patched++;

                            if (m.Spawner is Spawner spawner)
                            {
                                if (spawner.Invulnerable == false)
                                {
                                    spawner.Invulnerable = true;
                                    patched++;
                                }
                            }
                        }
                    }


                }
                #endregion Unbless all the crap I asked to be unblessed

                #region Township 
                if (AllShards())
                {
                    Server.Township.TownshipSettings.MinstrelCharge = 100000;
                    patched++;
                }
                #endregion Township 

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV137

        #region PatchV138

        private static int? PatchV138(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Enable Static Housing
                if (AllShards())
                {
                    List<Point3D> list = new List<Point3D>() { new Point3D(1433, 1593, 20), new Point3D(1432, 1593, 20) };
                    foreach (var px in list)
                    {
                        // delete Sparkle 0x375A
                        List<Item> items = FindItemsAt(px, Map.Felucca, 2);
                        foreach (Item ix in items)
                            if (ix.ItemID == 0x375A)
                            {
                                ix.Delete();
                                patched++;
                            }
                        // enable teleporter and turn on sparkle
                        Teleporter tele = (Teleporter)FindOneItemAt(px, Map.Felucca, typeof(Teleporter), 2, false);
                        if (tele != null)
                        {
                            tele.Running = true;
                            tele.Sparkle = true;
                            patched++;
                        }
                    }
                }
                #endregion Enable Static Housing

                #region Township 
                if (AllShards())
                {
                    Server.Township.TownshipSettings.MinstrelCharge = 100000;
                    patched++;
                }
                #endregion Township 

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV138

        #region PatchV139

        private static int? PatchV139(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Set all hire fighters clothes and item to Item.BoolTable.DeleteOnLift
                if (AllShards())
                {
                    foreach (Mobile m in World.Mobiles.Values)
                        if (m is HireFighter hf)
                            foreach (Item item in hf.Items)
                            {
                                item.SetItemBool(Item.ItemBoolTable.DeleteOnLift, true);
                                patched++;
                            }
                }
                #endregion Set all hire fighters clothes and item to Item.BoolTable.DeleteOnLift

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV139

        #region PatchV140

        private static int? PatchV140(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Respawn hire fighters so they get their new title
                if (AllShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.Running)
                            if (spawner.Spawns("HireFighter"))
                            {
                                spawner.RemoveObjects();
                                spawner.ScheduleRespawn = true;
                                patched++;
                            }
                }
                #endregion Respawn hire fighters so they get their new title

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV140

        #region PatchV141

        private static int? PatchV141(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Issue refunds to players that purchased houses at 10x (houses are now 3x)
                if (Siege())
                {
                    {   // HOUSES
                        int hash_counter = 2048;
                        LogHelper logger = new LogHelper("Housing refund.log", overwrite: false, sline: true);
                        foreach (Item item in World.Items.Values)
                            if (item is HouseSign sign && sign.Structure != null && sign.Structure.Owner != null)
                            {
                                if (sign.Structure.Owner.AccessLevel > AccessLevel.Player)
                                {
                                    logger.Log(string.Format("Skipping staff owned house for {0}, located at {1}", sign.Structure.Owner, sign.Structure.Location));
                                    continue;
                                }

                                HouseDeed hd = sign.Structure.GetDeed();
                                logger.Log(string.Format("Beginning refund for {0} located at {1}, Serial {2}.", sign.Structure.GetType().Name, sign.Structure.Location, sign.Structure.Serial));
                                logger.Log(string.Format("The Owner is {0}", sign.Structure.Owner));
                                if (hd == null)
                                {
                                    logger.Log(string.Format("** Unable to acquire a deed for this house, skipping. **".ToUpper()));
                                    continue;
                                }
                                logger.Log(string.Format("Base Price: {0}", hd.Price));
                                logger.Log(string.Format("Owner paid: {0}", hd.Price * 10));
                                logger.Log(string.Format("New price: {0}", hd.Price * 3));
                                int refund = (hd.Price * 10) - (hd.Price * 3);  // new 3x pricing
                                logger.Log(string.Format("Owner gets a refund of {0}", refund));

                                /*
                                 * Make Hue
                                 */
                                int hue = RandomSpecialHue(hd.Price + (hash_counter += 16));

                                /*
                                 * Make the checks
                                 * +16 to make the hue quite different
                                 */
                                int[] amounts = Utility.RandomNSumM(4, refund);     // amount for each of the 4 checks
                                DateTime[] valid_dates = new DateTime[4]            // dates these checks become valid
                                {
                                DateTime.UtcNow,
                                DateTime.UtcNow + TimeSpan.FromDays(7),
                                DateTime.UtcNow + TimeSpan.FromDays(14),
                                DateTime.UtcNow + TimeSpan.FromDays(21),
                                };
                                RefundCheck[] checks = new RefundCheck[4]           // construct the table of checks
                                {
                                new RefundCheck(amounts[0],valid_dates[0], hue, "for a house"),
                                new RefundCheck(amounts[1],valid_dates[1], hue, "for a house"),
                                new RefundCheck(amounts[2],valid_dates[2], hue, "for a house"),
                                new RefundCheck(amounts[3],valid_dates[3], hue, "for a house"),
                                };

                                System.Diagnostics.Debug.Assert(amounts.Sum() == refund);   // sanity

                                /*
                                 * Distribute the checks
                                 */
                                foreach (var check in checks)
                                {
                                    if (sign.Structure.Owner.BankBox != null)
                                        if (sign.Structure.Owner.BankBox.TryDropItem(World.GetSystemAcct(), check, sendFullMessage: false))
                                        {
                                            logger.Log(string.Format("A check worth {0} was deposited into {1}'s bank box", check.Worth, sign.Structure.Owner));
                                            patched++;
                                            continue;
                                        }
                                    if (sign.Structure.Owner.Backpack != null)
                                        if (sign.Structure.Owner.Backpack.TryDropItem(World.GetSystemAcct(), check, sendFullMessage: false))
                                        {
                                            logger.Log(string.Format("A check worth {0} was deposited into {1}'s backpack", check.Worth, sign.Structure.Owner));
                                            patched++;
                                            continue;
                                        }

                                    logger.Log(string.Format("Unable to deposit a check worth {0} for {1}", check.Worth, sign.Structure.Owner));
                                    ;
                                }
                            }
                        logger.Finish();
                    }
                    {   // HOUSE DEEDS
                        int hash_counter = 1024;
                        LogHelper logger = new LogHelper("House deed refund.log", overwrite: false, sline: true);
                        foreach (Item item in World.Items.Values)
                            if (item != null && !item.Deleted && item.GetType().IsAssignableTo(typeof(HouseDeed)))
                            {
                                Mobile parent_mobile = item.RootParent as Mobile;
                                Container parent_container = item.Parent as Container;

                                if (parent_mobile == null && parent_container == null)
                                {
                                    /*  We have a ton of this crap, ignore for now, but we need to clean all this stuff up
                                     * 1/21/2024 10:18:57 AM: Unknown condition: deed 0x4001D3A4 "LogCabinDeed", located at (0, 0, 0 Internal)
                                     * 1/21/2024 10:18:57 AM: Unknown condition: Created:1/21/2024 6:18:57 PM, LastMoved:1/21/2024 6:18:57 PM, LastAccessed:1/21/2024 6:18:57 PM, Map:Internal
                                     */
                                    if (item.LastAccessed.Year > 2024)
                                    {
                                        logger.Log(string.Format("Unknown condition: deed {0}, located at {1}", item, item.GetWorldLocation()));
                                        logger.Log(string.Format("Unknown condition: Created:{0}, LastMoved:{1}, LastAccessed:{2}, Map:{3}",
                                            item.Created, item.LastMoved, item.LastAccessed, item.Map));
                                    }
                                    continue;
                                }

                                if (parent_mobile != null && parent_mobile.AccessLevel > AccessLevel.Player)
                                {
                                    logger.Log(string.Format("Skipping staff owned house deed for {0}, located at {1}", parent_mobile, parent_mobile.Location));
                                    continue;
                                }

                                if (parent_mobile is PlayerVendor pv && (pv.IsStaffOwned || pv.Owner != null && pv.Owner.AccessLevel > AccessLevel.Player))
                                {   // too spammy
                                    //logger.Log(string.Format("Skipping deeds on a staff owned player vendor {0}, located at {1}", parent_mobile, parent_mobile.Location));
                                    continue;
                                }

                                if (parent_container is BankBox bx && bx.Parent == null)
                                {   // orphaned BB, Adam has some of there, ignore
                                    continue;
                                }

                                BaseMulti bm = null;
                                if (parent_container != null && (bm = BaseHouse.Find(parent_container.Location, parent_container.Map)) != null && bm is BaseHouse bh)
                                {   // ignore containers in staff houses
                                    if (bh.Owner != null && bh.Owner.AccessLevel > AccessLevel.Player)
                                        continue;
                                }

                                if (parent_mobile is BaseVendor bv)
                                {
                                    logger.Log(string.Format("Skipping deeds on a staff owned BaseVendor {0}, located at {1}", parent_mobile, parent_mobile.Location));
                                    continue;
                                }

                                HouseDeed hd = item as HouseDeed;
                                System.Diagnostics.Debug.Assert(hd != null);   // sanity
                                if (hd == null)
                                {
                                    logger.Log(string.Format("** Unable to cast deed to HouseDeed , skipping. **".ToUpper()));
                                    continue;
                                }

                                if (hd is TentBag || hd is SiegeTentBag)
                                {   // not doing these
                                    continue;
                                }

                                logger.Log(string.Format("Beginning refund for {0} located at {1}, Serial {2}.", hd.GetType().Name, hd.GetWorldLocation(), hd.Serial));

                                if (parent_container is BankBox bb)
                                    logger.Log(string.Format("The Owner is {0}'s {1}", bb.Parent, bb));
                                else
                                    logger.Log(string.Format("The Owner is {0}", (IEntity)parent_mobile ?? (IEntity)parent_container));

                                logger.Log(string.Format("Base Price: {0}", hd.Price));
                                logger.Log(string.Format("Owner paid: {0}", hd.Price * 10));
                                int refund = (hd.Price * 10);
                                logger.Log(string.Format("Owner gets a full refund of {0}", refund));

                                /*
                                 * Make Hue
                                 * +16 to make the hue quite different
                                 */
                                int hue = RandomSpecialHue(hd.Price + (hash_counter += 16));

                                /*
                                 * Make the checks
                                 */
                                int[] amounts = Utility.RandomNSumM(4, refund);     // amount for each of the 4 checks
                                DateTime[] valid_dates = new DateTime[4]            // dates these checks become valid
                                {
                                DateTime.UtcNow,
                                DateTime.UtcNow + TimeSpan.FromDays(7),
                                DateTime.UtcNow + TimeSpan.FromDays(14),
                                DateTime.UtcNow + TimeSpan.FromDays(21),
                                };
                                RefundCheck[] checks = new RefundCheck[4]           // construct the table of checks
                                {
                                new RefundCheck(amounts[0],valid_dates[0], hue, "for a house deed"),
                                new RefundCheck(amounts[1],valid_dates[1], hue, "for a house deed"),
                                new RefundCheck(amounts[2],valid_dates[2], hue, "for a house deed"),
                                new RefundCheck(amounts[3],valid_dates[3], hue, "for a house deed"),
                                };

                                System.Diagnostics.Debug.Assert(amounts.Sum() == refund);   // sanity

                                /*
                                 * Distribute the checks
                                 */
                                foreach (var check in checks)
                                {
                                    if (parent_mobile != null)
                                        if (parent_mobile.BankBox != null)
                                            if (parent_mobile.BankBox.TryDropItem(World.GetSystemAcct(), check, sendFullMessage: false))
                                            {
                                                logger.Log(string.Format("A check worth {0} was deposited into {1}'s bank box", check.Worth, parent_mobile));
                                                patched++;
                                                continue;
                                            }
                                    if (parent_container != null)
                                        if (true)
                                            if (parent_container.TryDropItem(World.GetSystemAcct(), check, sendFullMessage: false))
                                            {
                                                logger.Log(string.Format("A check worth {0} was deposited into container {1}, location {2}", check.Worth, parent_container, parent_container.GetWorldLocation()));
                                                patched++;
                                                continue;
                                            }

                                    logger.Log(string.Format("Unable to deposit a check worth {0} for {1}".ToUpper(), check.Worth, (IEntity)parent_mobile ?? (IEntity)parent_container));
                                }

                                hd.Delete();
                            }
                        logger.Finish();
                    }
                }
                #endregion Issue refunds to players that purchased houses at 10x (houses are now 3x)

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV141

        #region PatchV142

        private static int? PatchV142(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Patch ControlMasterGUID for stabled pets
                if (AllShards())
                {
                    {   // verify all stabled pets have a valid GUID
                        foreach (Mobile m in World.Mobiles.Values)
                            if (m is BaseCreature bc && bc.IsAnyStabled) // ControlMaster will be null if stabled
                                if (bc.ControlMasterGUID == 0)
                                    ;
                    }


                    {   // verify all players have a GUID
                        foreach (Mobile m in World.Mobiles.Values)
                            if (m is PlayerMobile pm)
                                if (pm.AcctGUID == 0)
                                {
                                    ;
                                }
                    }

                    {   // patch normal stabled pets GUIDS
                        foreach (Mobile m in World.Mobiles.Values)
                            if (m is PlayerMobile pm)
                                if (AnimalTrainer.Table.ContainsKey(pm))
                                    if (AnimalTrainer.Table[pm] != null)
                                        foreach (var pet in AnimalTrainer.Table[pm])
                                            if (pet is BaseCreature bc)
                                                if (bc.ControlMasterGUID != pm.AcctGUID)
                                                {
                                                    bc.ControlMasterGUID = pm.AcctGUID;
                                                    patched++;
                                                }
                    }

                    {   // patch stable master stabled pets GUIDS
                        foreach (var kvp in StableMaster.Table)
                            if (kvp.Key is PlayerMobile pm)
                                if (kvp.Value != null)
                                    foreach (var pet in kvp.Value)
                                        if (pet is BaseCreature bc)
                                            if (bc.ControlMasterGUID != pm.AcctGUID)
                                            {
                                                bc.ControlMasterGUID = pm.AcctGUID;
                                                patched++;
                                            }
                    }

                    {   // patch stable master stabled pets FLAGS
                        foreach (var kvp in StableMaster.Table)
                            if (kvp.Key is PlayerMobile pm)
                                if (kvp.Value != null)
                                    foreach (var pet in kvp.Value)
                                        if (pet is BaseCreature bc)
                                            if (bc.IsStableMasterStabled == false)
                                            {
                                                bc.IsStableMasterStabled = true;
                                                patched++;
                                            }
                    }

                    {   // patch elf stabler stabled pets GUIDS
                        foreach (var kvp in ElfStabler.Table)
                            if (kvp.Key is PlayerMobile pm)
                                if (kvp.Value != null)
                                    foreach (var pet in kvp.Value)
                                        if (pet is BaseCreature bc)
                                            if (bc.ControlMasterGUID != pm.AcctGUID)
                                            {
                                                bc.ControlMasterGUID = pm.AcctGUID;
                                                patched++;
                                            }
                    }

                    {   // patch elf stabler stabled pets FLAGS
                        foreach (var kvp in ElfStabler.Table)
                            if (kvp.Key is PlayerMobile pm)
                                if (kvp.Value != null)
                                    foreach (var pet in kvp.Value)
                                        if (pet is BaseCreature bc)
                                            if (bc.IsElfStabled == false)
                                            {
                                                bc.IsElfStabled = true;
                                                patched++;
                                            }
                    }

                }
                #endregion Patch ControlMasterGUID for stabled pets

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV142

        #region PatchV143

        private static int? PatchV143(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                string exportfile = AngelIsland() ? "pets.ai.export" : "pets.siege.export";
                #region Begin Implementation

                #region Recreate lost pets                
                if (Siege() || AngelIsland())
                {
                    /* This bit of code synchronizes both the players notion of his pets, the global pet cache
                     * 1. removes pets that are not stabled from players pet list
                     * 2. removes pets that are not stabled from the global pet cache
                     * 3. StableImport all previously known 'good pets'
                     * 4. verify the configuration of the pet and add him to the global pet cache
                     */
                    // remove pets that are not stabled from the pet cache
                    {
                        foreach (Mobile mob in World.Mobiles.Values)
                            if (mob is PlayerMobile pm)
                            {
                                List<BaseCreature> list = new();
                                if (AnimalTrainer.Table.ContainsKey(pm))
                                {
                                    foreach (Mobile mob2 in AnimalTrainer.Table[pm])
                                        if (mob2 is BaseCreature pet)
                                            if (pet.IsAnyStabled == false)
                                                list.Add(pet);

                                    foreach (BaseCreature pet in list)
                                    {
                                        AnimalTrainer.Table[pm].Remove(pet);
                                        pet.ClearStabled();
                                        pet.ControlMasterGUID = 0;

                                        if (Mobile.PetCache.Contains(pet))
                                            Mobile.PetCache.Remove(pet);
                                    }
                                }
                            }
                    }

                    // cleanup the global pet cache directly
                    {
                        List<Mobile> removeList = new List<Mobile>();
                        foreach (Mobile mob3 in Mobile.PetCache)
                            if (mob3 is BaseCreature bc)
                                if (!bc.IsAnyStabled)
                                    removeList.Add(bc);

                        foreach (Mobile mob4 in removeList)
                            Mobile.PetCache.Remove(mob4);
                    }

                    // now add back all the previously known stabled pets
                    Server.Commands.StableTransfer.StableImport_OnCommand(
                        new CommandEventArgs(World.GetSystemAcct(),
                        command: "StableImport",
                        argString: exportfile,
                        arguments: new string[] { exportfile }));

                    // Verify their configuration and add to global pet cache
                    {
                        int num_deleted = 0;
                        foreach (Mobile mob in World.Mobiles.Values)
                            if (mob is PlayerMobile pm)
                                if (AnimalTrainer.Table.ContainsKey(pm))
                                    if (AnimalTrainer.Table[pm] != null)
                                        foreach (Mobile mob2 in AnimalTrainer.Table[pm])
                                            if (mob2 is BaseCreature pet)
                                                if (pet.Deleted)
                                                {
                                                    num_deleted++; // error
                                                }
                                                else if (pet.IsAnyStabled)
                                                {
                                                    pet.ClearStabled();
                                                    pet.IsStableMasterStabled = true;
                                                    pet.ControlMasterGUID = pm.AcctGUID;

                                                    if (!Mobile.PetCache.Contains(pet))
                                                        Mobile.PetCache.Add(pet);
                                                }
                        EchoOut(string.Format("** {0} deleted pets **".ToUpper(), num_deleted), ConsoleColor.Magenta);
                    }
                }

                #endregion Recreate lost pets

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV143

        #region PatchV144

        private static int? PatchV144(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Recalc house deed prices for deeds on PlayerVendors (custom housing area)
                foreach (Mobile m in World.Mobiles.Values)
                    if (m is PlayerVendor pv && pv.IsStaffOwned)
                        patched += pv.RefreshDefaultPrices();
                #endregion Recalc house deed prices for deeds on PlayerVendors (custom housing area)

                #region Update pricing on 'for sale' signs

                // first grab all the static house signs
                List<HouseSign> signs = new List<HouseSign>();
                foreach (Item item in World.Items.Values)
                    if (item is HouseSign hs && hs.Structure is Server.Multis.StaticHousing.StaticHouse sh)
                        signs.Add(hs);

                const int ForSaleSign = 0xBD1;
                List<Item> toDelete = new();
                foreach (var sign in signs)
                {
                    List<Item> items = new List<Item>();
                    IPooledEnumerable eable = sign.Map.GetItemsInRange(sign.Location, range: 13);
                    foreach (Item thing in eable)
                        if (thing != null && thing.ItemID == ForSaleSign)
                            if (thing.Name is string name && name.Contains("price", StringComparison.OrdinalIgnoreCase))
                                items.Add(thing);
                    eable.Free();

                    // get the closest for sale sign
                    items.Sort((x, y) => x.GetDistanceToSqrt(sign).CompareTo(y.GetDistanceToSqrt(sign)));

                    foreach (Item thing in items)
                        if (thing != null && thing.ItemID == ForSaleSign)
                            if (thing.Name is string name && name.Contains("price", StringComparison.OrdinalIgnoreCase))
                            {
                                toDelete.Add(thing);
                                ForSaleSign fss = new();
                                fss.MoveToWorld(thing.Location, thing.Map);
                                Server.Multis.StaticHousing.StaticDeed temp = (Server.Multis.StaticHousing.StaticDeed)sign.Structure.GetDeed();
                                fss.Deed = temp;    // these signs dupe the deed, so we need to delete the temp one
                                temp.Delete();
                                patched++;
                            }
                }

                foreach (Item item in toDelete)
                    item.Delete();

                #endregion Update pricing on 'for sale' signs

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV144

        #region PatchV145

        private static int? PatchV145(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Patch BaseMount ItemIDs

                Dictionary<Mobile, Mobile> mounted = new();
                int m_count = 0;
                int broken_ids = 0;
                int base_mounts = 0;
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is BaseMount)
                        base_mounts++;                          // how many we are checking

                    if (m.Mount != null && m.Mount is Mobile)   // m.Mount is not Mobile == EtherealLlama for instance
                    {   // dismount, and add to remount dictionary
                        m_count++;
                        mounted.Add(m, m.Mount as Mobile);
                        Mobile mount = (Mobile)m.Mount;
                        Dismount(m);
                        patched++;
                    }
                }

                // everyone has been dismounted
                // Clear the notion of InternamItem, but don't delete
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is BaseMount bm)
                    {
                        base_mounts++;                                              // how many we are checking
                        if (bm.InternalItem is MountItem)
                            bm.InternalItem.SetItemBool(Item.ItemBoolTable.Orphaned, true); // allows Cron Cleanup to delete this (MountItems are explicitly ignored.)
                        bm.InternalItem = null;                                     // clear the internal Item, but don't delete
                        int goodID = BaseMount.LookupItemID(bm.BodyValue);
                        if (bm.ItemID != goodID && goodID != 0)
                        {   // got a broken ID, hmm, patch!
                            bm.ItemID = goodID;
                            broken_ids++;
                            patched++;
                        }
                        patched++;
                    }
                }

                // mount up all the riders
                foreach (var kvp in mounted)
                {
                    // mount up!
                    (kvp.Value as IMount).Rider = kvp.Key;
                    patched++;
                }

                #endregion Patch BaseMount ItemIDs

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV145

        #region PatchV146

        private static int? PatchV146(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Allow mini-boss armor
                if (Core.RuleSets.SiegeRules(2))
                {
                    // turn off these spawners as the rare loot is part of the 'mini-boss' loot
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.Running)
                            if (
                                spawner.Spawns("FleshRenderer")
                                || spawner.Spawns("AncientLich")
                                || spawner.Spawns("ArcticOgreLord")
                                || spawner.Spawns("AncientWyrm")
                                || spawner.Spawns("BoneDemon")
                                || spawner.Spawns("Balron")
                                )
                            {
                                spawner.RemoveObjects();
                                spawner.Running = false;
                                patched++;
                            }
                }
                #endregion Allow mini-boss armor

                #region Patch out old IsStabled bit
                if (AllShards())
                {
                    // clear the legacy notion of purely being 'stabled'
                    //foreach (Mobile m in World.Mobiles.Values)
                    //    if (m is BaseCreature bc)
                    //        if (bc.GetStable == CreatureBoolTable.None && bc.GetCreatureBool(CreatureBoolTable.__open3))
                    //        {   // CreatureBoolTable.IsAnyStabled => __open3
                    //            bc.SetCreatureBool(CreatureBoolTable.__open3, false);
                    //            patched++;
                    //        }
                }
                #endregion Patch out old IsStabled bit

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV146

        #region PatchV147

        private static int? PatchV147(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation


                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV147

        #region PatchV148

        private static int? PatchV148(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Get rid of all legacy Reserved serials
                {
                    List<Item> reserved_serial_list = new();
                    List<Item> new_items_list = new();
                    foreach (Item item in World.Items.Values)
                        if (item.Serial >= 0x60000000)
                        {
                            reserved_serial_list.Add(item);
                            new_items_list.Add(new Item());
                        }

                    foreach (Item item in reserved_serial_list)
                    {
                        int serial = new_items_list[0].Serial;
                        new_items_list[0].Delete();
                        new_items_list.RemoveAt(0);
                        item.ReassignSerial(serial);
                        patched++;
                    }
                }
                #endregion Get rid of all legacy Reserved serials

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV148

        #region PatchV149

        private static int? PatchV149(int patchid)
        {
            return Patcher.PatchV149(patchid, utility: false);
        }

        #endregion PatchV149

        #region PatchV150

        private static int? PatchV150(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Wipe spawner objects that are trying to spawn restricted objects (things from Angel Island Prison for instance)
                //  Don't need to do this on each restart since the spawner now disallows these objects
                if (AllShards())
                {
                    string bad_object = string.Empty;
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner)
                            if (!SpawnerGump.CheckRegion(spawner, spawner.ObjectNamesRaw, ref bad_object))
                            {
                                EchoOut(string.Format("Disabling spawner ({0}) spawning illicit object ({1}).", spawner, bad_object), ConsoleColor.Yellow);
                                spawner.ObjectNamesRaw = new ArrayList();
                                spawner.RemoveObjects();
                                spawner.Running = false;
                                patched++;
                            }
                }
                #endregion Wipe spawner objects that are trying to spawn restricted objects (things from Angel Island Prison for instance)

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV150

        #region PatchV151

        private static int? PatchV151(int patchid)
        {
            return Patcher.PatchV151(patchid, utility: false);
        }

        #endregion PatchV151

        #region PatchV152

        private static int? PatchV152(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Find/Fix Enchanted Scrolls 
                if (AllShards())
                {
                    List<EnchantedScroll> needs_patch = new();
                    foreach (Item item in World.Items.Values)
                        if (item is EnchantedScroll es)
                            if (es.Item == null || es.Item is EnchantedScroll)
                            {
                                needs_patch.Add(es);
                            }
                            else if (es.Item.IsIntMapStorage != true)
                            {
                                es.Item.IsIntMapStorage = true;
                            }

                    List<Type> junk_types = new() { typeof(Dagger), typeof(SkinningKnife), typeof(Cleaver), typeof(HammerPick),
                        typeof(Hatchet), typeof(Maul), typeof(ButcherKnife), typeof(Club), typeof(Pitchfork), typeof(ShepherdsCrook)};
                    List<Item> got = new();
                    foreach (EnchantedScroll es in needs_patch)
                    {
                        Item item = null;
                        while ((item = ChestLoot(level: 5)) == null || junk_types.Contains(item.GetType()))
                            ;
                        es.Item = item;
                        got.Add(item);
                    }

                    ;
                }
                #endregion Find/Fix Enchanted Scrolls 

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV152

        #region PatchV153

        private static int? PatchV153(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Find/Fix links
                if (AllShards())
                {   // rerun patch to correct mobile/item linkage
                    patched += Patcher.PatchV151(patchid: 0, utility: true);
                }
                #endregion Find/Fix links

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV153

        #region PatchV154

        private static int? PatchV154(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Patch invasion beacons
                if (AllShards())
                {
                    List<NavBeacon> to_delete = new();
                    foreach (Item item in World.Items.Values)
                        if (item is NavBeacon nb)
                        {
                            nb.Journey = "britain_invasion";
                            nb.RegisterBeacon();
                            patched++;
                            if (nb.Map != Map.Felucca)
                            {
                                to_delete.Add(nb);
                                patched++;
                            }
                        }

                    foreach (NavBeacon nb in to_delete)
                        nb.Delete();
                }
                #endregion Patch invasion beacons

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV154

        #region PatchV155

        private static int? PatchV155(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Delete old invasion beacons
                if (AllShards())
                {
                    List<NavBeacon> to_delete = new();
                    foreach (Item item in World.Items.Values)
                        if (item is NavBeacon nb)
                        {
                            to_delete.Add(nb);
                            patched++;
                        }

                    foreach (NavBeacon nb in to_delete)
                        nb.Delete();
                }
                #endregion Delete old invasion beacons

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV155

        #region PatchV156

        private static int? PatchV156(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Fix Stable Masters  - Breeders only / No Checks
                if (AllShards())
                {   // rerun patch to correct mobile/item linkage
                    patched += Patcher.PatchV149(patchid: 0, utility: true);
                }
                #endregion Fix Stable Masters  - Breeders only / No Checks

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV156

        #region PatchV157

        private static int? PatchV157(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Turn on all beacons
                if (AllShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is NavBeacon nb)
                            nb.Active = true;
                }
                #endregion Turn on all beacons

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV157

        #region PatchV158

        private static int? PatchV158(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Port all Broadcaster Controllers to new 'Spawner' model
                if (AllShards())
                {
                    #region Set the spawners of all linked items and mobiles as linked
                    foreach (Item item in World.Items.Values)
                        if (item.GetItemBool(Item.ItemBoolTable.IsLinked))
                            if (item.Spawner != null)
                                item.Spawner.SetItemBool(Item.ItemBoolTable.IsLinked, true);

                    foreach (Mobile mobile in World.Mobiles.Values)
                        if (mobile.GetMobileBool(Mobile.MobileBoolTable.IsLinked))
                            if (mobile.Spawner != null)
                                mobile.Spawner.SetItemBool(Item.ItemBoolTable.IsLinked, true);
                    #endregion Set the spawners of all linked items and mobiles as linked

                    #region now, move all broadcasters to 'spawner model'
                    List<IEntity> entities = new List<IEntity>();
                    foreach (Item item in World.Items.Values)
                        if (item is Server.Items.Triggers.Broadcaster bc)
                            if (bc.TargetMobile != null)
                            {
                                entities.Add(bc.TargetMobile);
                                if (bc.TargetMobile.Spawner != null)
                                {
                                    bc.TargetMobile.Spawner.SetItemBool(Item.ItemBoolTable.IsLinked, true);
                                    bc.TargetSpawner = bc.TargetMobile.Spawner;
                                    bc.TargetMobile = null;
                                    patched++;
                                }

                            }
                            else if (bc.TargetItem != null)
                            {
                                entities.Add(bc.TargetItem);
                                if (bc.TargetItem.Spawner != null)
                                {
                                    bc.TargetSpawner.Spawner.SetItemBool(Item.ItemBoolTable.IsLinked, true);
                                    bc.TargetSpawner = bc.TargetItem.Spawner;
                                    // don't null the bc.TargetItem here since a spawner is an item
                                    //  and they will share the IEntity m_Target
                                    patched++;
                                }
                            }
                    #endregion now, move all broadcasters to 'spawner model'

                    // resetting the mobile or item above will clear it's 'linked' flag, reset it here
                    foreach (var ent in entities)
                        if (ent is Item item)
                            item.SetItemBool(Item.ItemBoolTable.IsLinked, true);
                        else if (ent is Mobile mobile)
                            mobile.SetMobileBool(Mobile.MobileBoolTable.IsLinked, true);
                }
                #endregion Port all Broadcaster Controllers to new 'Spawner' model

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV158

        #region PatchV159

        private static int? PatchV159(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation
                int missing = 0;
                #region Re-add missing mobiles from ModeNeruns and ModeMulti spawners
                if (Siege())
                {
                    (Serial serial, string[] NameList, string counts)[] tupleList = {
                            (0x4004EA87, new string[]{ "paganpeasant","pagandruid", }, "2, 1"),
                            (0x4004EA88, new string[]{ "paganpeasant","pagandruid", }, "2, 1"),
                            (0x4004EA89, new string[]{ "paganpeasant","pagandruid", }, "2, 1"),
                            (0x4004F18A, new string[]{ "paganpeasant","pagandruid", }, "0, 1"),
                            (0x4000B151, new string[]{ "EvilWanderingHealer", }, "1, 1"),
                            (0x4000B1A9, new string[]{ "lich","lichlord", }, "2, 2"),
                            (0x4000DAD3, new string[]{ "Bogthing","Bogling", }, "2, 5"),
                            (0x40019DA5, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DA6, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DA7, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DA8, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DA9, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DAA, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DAB, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DAC, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DAD, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DAE, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DAF, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DB0, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DB1, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DB2, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DB3, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DB4, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DB5, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DB6, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DB7, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DB8, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DB9, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DBA, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DBB, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DBD, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DBE, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DBF, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DC0, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DC1, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DC2, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DC3, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DC4, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DC5, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DC6, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DC7, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DC8, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DC9, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DCA, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DCB, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DCC, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DCD, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DCE, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DCF, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DD0, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DD1, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DD2, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DD3, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DD4, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DD5, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DD6, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DD7, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DD8, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DD9, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DDA, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DDB, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DDC, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DDD, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DDE, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DDF, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DE1, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DE2, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DE3, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DE4, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DE5, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DE6, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DE7, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DE8, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DE9, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DEB, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DEC, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DED, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DEE, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DEF, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DF0, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DF1, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DF6, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DF9, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DFC, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019DFE, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E09, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E0B, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E0E, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E0F, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E11, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E13, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E14, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E15, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E16, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E17, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E18, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E19, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E1A, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E1B, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E1C, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E20, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E26, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E28, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E29, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E2D, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E2F, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E30, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E32, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E3B, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E3F, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E42, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E48, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E52, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E53, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E54, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E55, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E56, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E58, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E59, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E5F, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E60, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E61, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E62, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E63, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E64, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E66, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E67, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E68, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E69, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E6B, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E6C, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E6D, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E70, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E71, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E72, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E73, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E74, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E75, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E76, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E77, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E78, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E79, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E7A, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E7B, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E7C, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E7D, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E7E, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E7F, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E80, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E81, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E82, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E83, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E84, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E86, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E8A, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E8C, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E8E, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E91, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E92, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E93, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E94, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E95, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E96, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E98, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E99, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E9A, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E9B, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E9C, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E9D, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E9E, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019E9F, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EA0, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EA1, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EA2, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EA3, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EA4, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EA5, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EA6, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EA7, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EA8, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EA9, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EAA, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EAB, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EAC, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EAD, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EAE, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x40019EB7, new string[]{ "TerathanDrone", }, "1"),
                            (0x40019EB8, new string[]{ "IceSerpent","WhiteWyrm", }, "1, 1, 0, 0, 0, 0"),
                            (0x40019EB9, new string[]{ "IceSerpent","WhiteWyrm", }, "1, 1, 0, 0, 0, 0"),
                            (0x40019EBA, new string[]{ "GiantSpider","DreadSpider", }, "1, 1, 0, 0, 0, 0"),
                            (0x40019EBB, new string[]{ "BogThing","Quagmire", }, "1, 1, 0, 0, 0, 0"),
                            (0x40019EBC, new string[]{ "Basilisk", }, "1"),
                            (0x40019EBE, new string[]{ "Basilisk", }, "1"),
                            (0x40019EBF, new string[]{ "Centaur","Kirin","Unicorn", }, "1, 1, 1, 0, 0, 0"),
                            (0x40019EC5, new string[]{ "ForestOstard","FrenziedOstard","FrenziedOstard", }, "1, 1, 1, 0, 0, 0"),
                            (0x40019EC9, new string[]{ "Wyvern","OphidianWarrior", }, "1, 1, 0, 0, 0, 0"),
                            (0x40019ECA, new string[]{ "FireSteed","HellCat","FireSteed", }, "1, 1, 1, 0, 0, 0"),
                            (0x40019ECC, new string[]{  }, "1"),
                            (0x400AADB0, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADB1, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADB3, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADB4, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADB5, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADB6, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADB7, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADB8, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADB9, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADBB, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADBC, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADBD, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADBF, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADC0, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADC1, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADC2, new string[]{ "alligator", }, "8"),
                            (0x400AADC3, new string[]{ "alligator", }, "2"),
                            (0x400AADC4, new string[]{ "alligator", }, "2"),
                            (0x400AADC5, new string[]{ "alligator", }, "2"),
                            (0x400AADC6, new string[]{ "alligator", }, "8"),
                            (0x400AADC7, new string[]{ "alligator", }, "2"),
                            (0x400AADC8, new string[]{ "Harpy", }, "6"),
                            (0x400AADC9, new string[]{ "Headlessone", }, "4"),
                            (0x400AADCB, new string[]{ "Stoneharpy", }, "2"),
                            (0x400AADCC, new string[]{ "Gazer","Gazerlarva", }, "4, 2, 0, 0, 0, 0"),
                            (0x400AADCD, new string[]{ "Gazer","Gazerlarva", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AADCE, new string[]{ "Corpser", }, "3"),
                            (0x400AADCF, new string[]{ "Corpser", }, "3"),
                            (0x400AADD0, new string[]{ "Headlessone", }, "2"),
                            (0x400AADD1, new string[]{ "Headlessone", }, "2"),
                            (0x400AADD2, new string[]{ "Headlessone", }, "2"),
                            (0x400AADD3, new string[]{ "Harpy","Stoneharpy", }, "8, 1, 0, 0, 0, 0"),
                            (0x400AADD4, new string[]{ "Giantspider","Dreadspider", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADD5, new string[]{ "Slime", }, "5"),
                            (0x400AADD6, new string[]{ "Waterelemental", }, "2"),
                            (0x400AADD7, new string[]{ "Giantspider","Dreadspider", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AADD8, new string[]{ "Giantspider","Dreadspider", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AADD9, new string[]{ "Gazer","Gazerlarva","Eldergazer", }, "4, 2, 1, 0, 0, 0"),
                            (0x400AADDA, new string[]{ "Corpser", }, "3"),
                            (0x400AADDB, new string[]{ "Corpser", }, "3"),
                            (0x400AADDC, new string[]{ "Gazer","Gazerlarva", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AADDD, new string[]{ "Corpser", }, "2"),
                            (0x400AADDE, new string[]{ "Gazer", }, "2"),
                            (0x400AADDF, new string[]{ "Skeleton", }, "4"),
                            (0x400AADE0, new string[]{ "Skeleton", }, "4"),
                            (0x400AADE1, new string[]{ "Skeleton", }, "4"),
                            (0x400AADE2, new string[]{ "Skeleton", }, "4"),
                            (0x400AADE3, new string[]{ "Mummy","Shade","Skeletalknight","Skeleton:Zombie","Spectre","Wraith", }, "1, 1, 1, 2, 1, 1"),
                            (0x400AADE4, new string[]{ "Mummy","Shade","Skeletalknight","Skeleton:Zombie","Spectre","Wraith", }, "1, 1, 1, 2, 1, 1"),
                            (0x400AADE5, new string[]{ "Mummy","Shade","Skeletalknight","Skeleton:Zombie","Spectre","Wraith", }, "1, 1, 1, 2, 1, 1"),
                            (0x400AADE6, new string[]{ "Boneknight","Skeleton","Zombie","Shade", }, "3, 1, 1, 1, 0, 0"),
                            (0x400AADE7, new string[]{ "Boneknight","Skeleton","Zombie","Shade", }, "3, 1, 1, 1, 0, 0"),
                            (0x400AADE8, new string[]{ "Lich","Spectre","Skeleton", }, "7, 3, 1, 0, 0, 0"),
                            (0x400AADE9, new string[]{ "Mummy","Shade","Skeletalknight","Skeleton:Zombie","Spectre","Wraith", }, "1, 1, 1, 2, 1, 1"),
                            (0x400AADEA, new string[]{ "Mummy","Shade","Skeletalknight","Skeleton:Zombie","Spectre","Wraith", }, "1, 1, 1, 3, 1, 1"),
                            (0x400AADEB, new string[]{ "Dragon","Drake", }, "1, 2, 0, 0, 0, 0"),
                            (0x400AADEC, new string[]{ "Mummy","Skeleton","Zombie", }, "1, 3, 1, 0, 0, 0"),
                            (0x400AADED, new string[]{ "Waterelemental", }, "2"),
                            (0x400AADEF, new string[]{ "Skeleton", }, "2"),
                            (0x400AADF0, new string[]{ "Mummy","Skeleton","Zombie", }, "1, 3, 1, 0, 0, 0"),
                            (0x400AADF1, new string[]{ "Skeleton","Zombie", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AADF2, new string[]{ "Skeleton","Zombie", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AADF3, new string[]{ "Skeleton","Zombie", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AADF4, new string[]{ "Skeleton","Zombie", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AADF5, new string[]{ "Skeleton","Spectre", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADF6, new string[]{ "Ghoul","Shade","Skeleton","Zombie", }, "3, 1, 1, 1, 0, 0"),
                            (0x400AADF8, new string[]{ "Ghoul","Shade","Skeleton","Zombie", }, "3, 1, 1, 1, 0, 0"),
                            (0x400AADF9, new string[]{ "Ghoul","Skeleton","Wraith","Zombie", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AADFA, new string[]{ "Ghoul","Skeleton","Wraith","Zombie", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AADFB, new string[]{ "Ghoul","Skeleton","Wraith","Zombie", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AADFD, new string[]{ "Ghoul","Wraith", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AADFE, new string[]{ "Ghoul","Mummy","Shade","Wraith","Zombie", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AADFF, new string[]{ "Boneknight","Skeletalknight", }, "4, 4, 0, 0, 0, 0"),
                            (0x400AAE00, new string[]{ "Ghoul","Shade","Skeleton","Wraith", }, "1, 1, 2, 1, 0, 0"),
                            (0x400AAE01, new string[]{ "Bonemagi", }, "3"),
                            (0x400AAE02, new string[]{ "Ghoul","Shade","Skeleton","Wraith","Zombie", }, "1, 1, 2, 1, 1, 0"),
                            (0x400AAE03, new string[]{ "Ghoul","Shade","Spectre","Wraith", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AAE04, new string[]{ "Lich", }, "2"),
                            (0x400AAE05, new string[]{ "Ghoul","Shade","Spectre","Wraith", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AAE06, new string[]{ "Ghoul","Lich","Spectre","Wraith", }, "1, 3, 1, 1, 0, 0"),
                            (0x400AAE07, new string[]{ "Ghoul","Poisonelemental", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AAE08, new string[]{ "Ghoul","Poisonelemental", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AAE09, new string[]{ "Ghoul","Shade","Spectre","Wraith", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AAE0A, new string[]{ "Lich", }, "4"),
                            (0x400AAE0B, new string[]{ "Ghoul","Lich", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AAE0C, new string[]{ "Fireelemental", }, "2"),
                            (0x400AAE0D, new string[]{ "Lich", }, "2"),
                            (0x400AAE0F, new string[]{ "Lizardman", }, "3"),
                            (0x400AAE10, new string[]{ "Lizardman", }, "5"),
                            (0x400AAE11, new string[]{ "Lizardman", }, "3"),
                            (0x400AAE12, new string[]{ "Lizardman", }, "5"),
                            (0x400AAE13, new string[]{ "Lizardman", }, "3"),
                            (0x400AAE14, new string[]{ "Lizardman", }, "3"),
                            (0x400AAE15, new string[]{ "Lizardman", }, "3"),
                            (0x400AAE16, new string[]{ "Ettin", }, "2"),
                            (0x400AAE17, new string[]{ "Ettin", }, "4"),
                            (0x400AAE18, new string[]{ "Earthelemental","Ettin", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AAE19, new string[]{ "Earthelemental","Ettin", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AAE1A, new string[]{ "Earthelemental","Ettin", }, "1, 2, 0, 0, 0, 0"),
                            (0x400AAE1B, new string[]{ "Ettin", }, "2"),
                            (0x400AAE1C, new string[]{ "Ettin", }, "2"),
                            (0x400AAE1D, new string[]{ "Earthelemental", }, "3"),
                            (0x400AAE1E, new string[]{ "Troll", }, "3"),
                            (0x400AAE1F, new string[]{ "Troll", }, "3"),
                            (0x400AAE20, new string[]{ "Troll", }, "3"),
                            (0x400AAE21, new string[]{ "Troll", }, "3"),
                            (0x400AAE22, new string[]{ "Ogre", }, "3"),
                            (0x400AAE24, new string[]{ "Ogre", }, "3"),
                            (0x400AAE25, new string[]{ "Ogrelord", }, "5"),
                            (0x400AAE26, new string[]{ "Ogre", }, "3"),
                            (0x400AAE2A, new string[]{ "Ogre", }, "3"),
                            (0x400AAE2B, new string[]{ "Ettin", }, "4"),
                            (0x400AAE2C, new string[]{ "Ettin", }, "4"),
                            (0x400AAE2D, new string[]{ "Cyclops", }, "3"),
                            (0x400AAE2E, new string[]{ "Ettin", }, "4"),
                            (0x400AAE2F, new string[]{ "Ettin", }, "4"),
                            (0x400AAE30, new string[]{ "Dragon","Drake","Wyvern", }, "1, 2, 1, 0, 0, 0"),
                            (0x400AAE32, new string[]{ "Dragon","Drake", }, "1, 2, 0, 0, 0, 0"),
                            (0x400AAE34, new string[]{ "Dragon","Drake","Wyvern", }, "1, 2, 1, 0, 0, 0"),
                            (0x400AAE39, new string[]{ "Dragon","Drake","Wyvern", }, "1, 2, 1, 0, 0, 0"),
                            (0x400AAE3A, new string[]{ "Dragon","Drake","Wyvern", }, "1, 2, 1, 0, 0, 0"),
                            (0x400AAE3C, new string[]{ "Giantserpent", }, "6"),
                            (0x400AAE47, new string[]{ "Wyvern","Dragon","Drake", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AAE48, new string[]{ "Waterelemental", }, "4"),
                            (0x400AAE49, new string[]{ "Giantserpent", }, "6"),
                            (0x400AAE4B, new string[]{ "Dragon","Drake","Wyvern", }, "1, 2, 1, 0, 0, 0"),
                            (0x400AAE4C, new string[]{ "Dragon","Drake","Wyvern", }, "1, 2, 1, 0, 0, 0"),
                            (0x400AAE4D, new string[]{ "Drake","Wyvern", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AAE4E, new string[]{ "Waterelemental", }, "4"),
                            (0x400AAE4F, new string[]{ "Waterelemental", }, "4"),
                            (0x400AAE50, new string[]{ "Drake","Shadowwyrm","Wyvern", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AAE51, new string[]{ "Daemon","Evilmage", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AAE52, new string[]{ "Drake","Shadowwyrm","Wyvern", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AAE53, new string[]{ "Fireelemental", }, "1"),
                            (0x400AAE54, new string[]{ "Wyvern:Wyvern:Giantserpent", }, "3"),
                            (0x400AAE55, new string[]{ "Ancientwyrm", }, "1"),
                            (0x400AAE56, new string[]{ "Slime","Giantrat","Giantserpent", }, "1, 2, 1, 0, 0, 0"),
                            (0x400AAE57, new string[]{ "Slime", }, "1"),
                            (0x400AAE58, new string[]{ "Evilmagelord", }, "2"),
                            (0x400AAE59, new string[]{ "Giantrat","Lavasnake","Evilmage", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AAE5A, new string[]{ "Lavalizard", }, "1"),
                            (0x400AAE5B, new string[]{ "Fireelemental", }, "3"),
                            (0x400AAE5C, new string[]{ "Fireelemental","Lavasnake","Hellcat","Hellhound", }, "3, 1, 1, 1, 0, 0"),
                            (0x400AAE5D, new string[]{ "Efreet","Lavaserpent","Hellhound","Fireelemental", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AAE5E, new string[]{ "Lich","Bonemagi:Skeletalmage","Hellcat","Hellhound","Fireelemental","Lavalizard", }, "4, 6, 1, 3, 2, 1"),
                            (0x400AAE5F, new string[]{ "Lavasnake", }, "1"),
                            (0x400AAE60, new string[]{ "Lavalizard", }, "1"),
                            (0x400AAE61, new string[]{ "Hellcat", }, "1"),
                            (0x400AAE63, new string[]{ "Fireelemental", }, "1"),
                            (0x400AAE64, new string[]{ "Lavasnake", }, "1"),
                            (0x400AAE65, new string[]{ "Lavasnake", }, "1"),
                            (0x400AAE66, new string[]{ "Fireelemental", }, "1"),
                            (0x400AAE67, new string[]{ "Fireelemental","Efreet","Lavaserpent", }, "4, 1, 1, 0, 0, 0"),
                            (0x400AAE68, new string[]{ "Hellcat","Fireelemental", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AAE69, new string[]{ "Fireelemental", }, "1"),
                            (0x400AAE6A, new string[]{ "Fireelemental", }, "2"),
                            (0x400AAE6B, new string[]{ "Hellhound", }, "1"),
                            (0x400AAE6C, new string[]{ "Efreet","Lavalizard", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AAE6D, new string[]{ "Fireelemental","Lavaserpent", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AAE6E, new string[]{ "Lavalizard","Fireelemental", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AAE6F, new string[]{ "Lich","Lichlord", }, "3, 1, 0, 0, 0, 0"),
                            (0x400AAE71, new string[]{ "Hellhound", }, "6"),
                            (0x400AAE72, new string[]{ "Daemon", }, "2"),
                            (0x400AAE73, new string[]{ "Hellhound","Hellcat", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AAE75, new string[]{ "Evilmage","Hellhound","Lavasnake","Hellcat", }, "1, 5, 1, 1, 0, 0"),
                            (0x400AAE76, new string[]{ "Evilmage", }, "1"),
                            (0x400AAE77, new string[]{ "Evilmagelord", }, "1"),
                            (0x400AAE78, new string[]{ "Evilmagelord", }, "1"),
                            (0x400AAE79, new string[]{ "Evilmage", }, "1"),
                            (0x400AAE7A, new string[]{ "Evilmage", }, "1"),
                            (0x400AAE7B, new string[]{ "Hellhound", }, "1"),
                            (0x400AAE7C, new string[]{ "Evilmage","Evilmagelord", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AAE7D, new string[]{ "Evilmage", }, "1"),
                            (0x400AAE7E, new string[]{ "Evilmage", }, "1"),
                            (0x400AAE7F, new string[]{ "Evilmage", }, "1"),
                            (0x400AAE80, new string[]{ "Evilmage", }, "1"),
                            (0x400AAE81, new string[]{ "Evilmage", }, "1"),
                            (0x400AAE82, new string[]{ "Evilmagelord", }, "1"),
                            (0x400AAE83, new string[]{ "Spectre:Wraith","Skeleton","Zombie", }, "2, 3, 4, 0, 0, 0"),
                            (0x400AAE84, new string[]{ "Spectre:Wraith:Shade","Skeleton","Zombie", }, "2, 3, 4, 0, 0, 0"),
                            (0x400AAE85, new string[]{ "Spectre:Shade:Wraith","Lich","Skeleton","Zombie", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AAE86, new string[]{ "Spectre:Shade:Wraith","Skeleton","Zombie", }, "1, 1, 2, 0, 0, 0"),
                            (0x400AAE87, new string[]{ "Lich", }, "1"),
                            (0x400AAE89, new string[]{ "Spectre:Wraith","Skeleton","Zombie", }, "2, 3, 4, 0, 0, 0"),
                            (0x400AAE8A, new string[]{ "Skeleton", }, "10"),
                            (0x400AAE8B, new string[]{ "zombie:skeleton", }, "4"),
                            (0x400AAE8C, new string[]{ "Imp","Hellhound", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AAE8D, new string[]{ "Imp","Hellhound", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AAE8E, new string[]{ "Imp","Hellhound", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AAE8F, new string[]{ "Imp","Hellhound", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AAE90, new string[]{ "Firegargoyle", }, "2"),
                            (0x400AAE91, new string[]{ "Imp","Hellhound", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AAE92, new string[]{ "Gargoyle","Hellhound", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AAE93, new string[]{ "Imp","Hellhound", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AAE94, new string[]{ "Gargoyle", }, "2"),
                            (0x400AAE95, new string[]{ "Imp","Hellhound", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AAE96, new string[]{ "Imp","Hellhound", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AAE98, new string[]{ "Daemon", }, "2"),
                            (0x400AAE9A, new string[]{ "Imp","Hellhound", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AAE9B, new string[]{ "Gargoyle", }, "2"),
                            (0x400AAE9C, new string[]{ "Gargoyle", }, "2"),
                            (0x400AAE9D, new string[]{ "Imp","Hellhound", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AAE9F, new string[]{ "Gargoyle", }, "4"),
                            (0x400AAEA1, new string[]{ "Gazer","Gazerlarva", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AAEA2, new string[]{ "Imp","Hellhound","Gargoyle", }, "1, 1, 2, 0, 0, 0"),
                            (0x400AAEA4, new string[]{ "Eldergazer","Gazer","Gazerlarva", }, "1, 3, 2, 0, 0, 0"),
                            (0x400AAEA5, new string[]{ "Balron","Gargoyle","Gazer", }, "1, 2, 2, 0, 0, 0"),
                            (0x400AAEA6, new string[]{ "Daemon","Gargoyle","Gazer", }, "2, 2, 2, 0, 0, 0"),
                            (0x400AAEA7, new string[]{ "Gargoyle", }, "2"),
                            (0x400AAEA8, new string[]{ "Daemon","Stonegargoyle", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AAEA9, new string[]{ "Daemon","Gazer","Gazerlarva", }, "2, 3, 2, 0, 0, 0"),
                            (0x400AAEAA, new string[]{ "Daemon","Hellhound","Imp", }, "2, 1, 1, 0, 0, 0"),
                            (0x400AAEAB, new string[]{ "Gargoyle", }, "2"),
                            (0x400AAEAC, new string[]{ "Gargoyle", }, "2"),
                            (0x400AAEAD, new string[]{ "Gargoyle", }, "2"),
                            (0x400AAEAE, new string[]{ "Firegargoyle", }, "2"),
                            (0x400AAEAF, new string[]{ "Gargoyle", }, "2"),
                            (0x400AAEB0, new string[]{ "Balron", }, "1"),
                            (0x400AAEB1, new string[]{ "Gazer","Imp","Stonegargoyle", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AAEB2, new string[]{ "Arcticogrelord", }, "1"),
                            (0x400AAEB3, new string[]{ "Iceserpent","Snowelemental","Ratman","Frosttroll","Frostspider", }, "2, 1, 1, 1, 1, 0"),
                            (0x400AAEB4, new string[]{ "Frostspider","Frostooze","Icesnake", }, "1, 2, 1, 0, 0, 0"),
                            (0x400AAEB5, new string[]{ "Ratmanmage", }, "1"),
                            (0x400AAEB6, new string[]{ "Arcticogrelord", }, "1"),
                            (0x400AAEB7, new string[]{ "Whitewyrm", }, "1"),
                            (0x400AAEB8, new string[]{ "Frostooze","Frostspider","Iceelemental:Snowelemental","Icesnake","Frosttroll", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AAEB9, new string[]{ "Frostooze","Frostspider","Iceelemental:Snowelemental","Icesnake","Frosttroll", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AAEBB, new string[]{ "Iceserpent","Ratman","Snowelemental:Iceelemental","Frosttroll", }, "3, 1, 4, 2, 0, 0"),
                            (0x400AAEBC, new string[]{ "Iceserpent","Snowelemental:Iceelemental","Frosttroll","Ratman", }, "3, 4, 1, 1, 0, 0"),
                            (0x400AAEBD, new string[]{ "Iceserpent","Snowelemental:Iceelemental", }, "9, 9, 0, 0, 0, 0"),
                            (0x400AAEBE, new string[]{ "Ratman","Ratmanarcher","Ratmanmage", }, "12, 7, 5, 0, 0, 0"),
                            (0x400AAEBF, new string[]{ "Icefiend", }, "1"),
                            (0x400AAEC0, new string[]{ "Icefiend", }, "1"),
                            (0x400AAEC1, new string[]{ "Swampdragon", }, "7"),
                            (0x400AAEC2, new string[]{ "Imp","Scorpion","Desertostard","Orc","Orcishlord","Stoneharpy", }, "15, 15, 6, 9, 6, 3"),
                            (0x400AAEC3, new string[]{ "Stonegargoyle","Wyvern","Mummy","Ettin", }, "3, 6, 4, 6, 0, 0"),
                            (0x400AAEC4, new string[]{ "Imp","Scorpion","Desertostard","Orc","Orcishlord","Stoneharpy", }, "10, 10, 4, 6, 4, 3"),
                            (0x400AAEC5, new string[]{ "Stonegargoyle","Wyvern","Mummy","Ettin", }, "3, 4, 2, 4, 0, 0"),
                            (0x400AAEC6, new string[]{ "Gazer","Reaper","Scorpion","Giantserpent","Troll", }, "3, 2, 3, 2, 2, 0"),
                            (0x400AAEC7, new string[]{ "Corpser","Giantspider","Troll","Harpy","Ettin","Gazer", }, "3, 3, 3, 6, 3, 3"),
                            (0x400AAEC8, new string[]{ "Cyclops","Titan", }, "5, 3, 0, 0, 0, 0"),
                            (0x400AAEC9, new string[]{ "Imp","Scorpion","Desertostard","Orc","Orcishlord","Wyvern", }, "3, 3, 1, 3, 2, 1"),
                            (0x400AAECA, new string[]{ "Ettin", }, "2"),
                            (0x400AAECB, new string[]{ "Ophidianarchmage","Ophidianmage","Ophidianmatriarch","Ophidianknight","Ophidianwarrior", }, "3, 6, 3, 4, 10, 0"),
                            (0x400AAECC, new string[]{ "Wyvern","Drake","Imp","Mongbat", }, "2, 4, 1, 2, 0, 0"),
                            (0x400AAECD, new string[]{ "Imp","Scorpion","Desertostard","Orc","Orcishlord","Stoneharpy", }, "8, 8, 3, 5, 3, 3"),
                            (0x400AAECE, new string[]{ "Stonegargoyle","Wyvern","Mummy","Ettin", }, "3, 3, 3, 5, 0, 0"),
                            (0x400AAECF, new string[]{ "Hellhound","Lavalizard","Fireelemental","Efreet","Hellcat","Lavaserpent", }, "8, 8, 10, 4, 6, 8"),
                            (0x400AAED0, new string[]{ "Lavasnake", }, "15"),
                            (0x400AAED1, new string[]{ "Wyvern", }, "6"),
                            (0x400AAED2, new string[]{ "Iceserpent","Icesnake","Frosttroll","Snowelemental","Whitewolf","Walrus", }, "4, 8, 4, 4, 5, 5"),
                            (0x400AAED3, new string[]{ "Polarbear", }, "5"),
                            (0x400AAED4, new string[]{ "Iceserpent","Icesnake","Frosttroll","Snowelemental","Whitewolf","Walrus", }, "4, 7, 4, 4, 5, 5"),
                            (0x400AAED5, new string[]{ "Polarbear", }, "5"),
                            (0x400AAED6, new string[]{ "Iceserpent","Icesnake","Frosttroll","Snowelemental","Whitewolf","Walrus", }, "4, 10, 4, 4, 3, 3"),
                            (0x400AAED8, new string[]{ "Polarbear", }, "3"),
                            (0x400AAED9, new string[]{ "Iceserpent","Icesnake","Snowelemental","Whitewolf","Walrus","Polarbear", }, "6, 10, 3, 3, 3, 3"),
                            (0x400AAEDA, new string[]{ "Iceserpent","Icesnake","Frosttroll","Snowelemental","Whitewolf","Walrus", }, "6, 4, 4, 4, 4, 4"),
                            (0x400AAEDB, new string[]{ "Polarbear", }, "4"),
                            (0x400AAEDC, new string[]{ "Iceserpent","Icesnake","Frosttroll","Snowelemental","Whitewolf","Walrus", }, "6, 3, 3, 3, 4, 4"),
                            (0x400AAEDD, new string[]{ "Polarbear", }, "4"),
                            (0x400AAEDE, new string[]{ "Gorilla","Panther","Giantserpent","Silverserpent","Mongbat", }, "10, 10, 10, 2, 10, 0"),
                            (0x400AAEDF, new string[]{ "Gianttoad","Swampdragon","Giantrat","Alligator","Sewerrat","Plaguebeast", }, "8, 5, 12, 12, 11, 2"),
                            (0x400AAEE0, new string[]{ "Bullfrog","Swamptentacle","Bogling","Bogthing", }, "15, 5, 13, 3, 0, 0"),
                            (0x400AAEE1, new string[]{ "Boneknight","Skeletalknight","Bonemagi","Skeleton","Wraith","Ghoul", }, "3, 3, 3, 6, 4, 4"),
                            (0x400AAEE2, new string[]{ "Savagerider","Savage","Savageshaman", }, "3, 9, 3, 0, 0, 0"),
                            (0x400AAEE3, new string[]{ "Horse","Orc","Orcishlord","Greathart","Hind","Ridablellama", }, "4, 8, 4, 4, 16, 4"),
                            (0x400AAEE4, new string[]{ "Horse","Orc","Orcishlord","Greathart","Hind","Ridablellama", }, "4, 8, 4, 4, 16, 4"),
                            (0x400AAEE5, new string[]{ "Horse","Orc","Orcishlord","Greathart","Hind","Ridablellama", }, "4, 8, 4, 4, 16, 4"),
                            (0x400AAEE6, new string[]{ "Horse","Orc","Orcishlord","Greathart","Hind","Ridablellama", }, "4, 8, 4, 4, 16, 4"),
                            (0x400AAEE7, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "6, 4, 4, 2, 2, 2"),
                            (0x400AAEE8, new string[]{ "Hind","Llama","Timberwolf", }, "2, 2, 2, 0, 0, 0"),
                            (0x400AAEE9, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "6, 4, 4, 2, 2, 2"),
                            (0x400AAEEA, new string[]{ "Hind","Llama","Timberwolf", }, "2, 2, 2, 0, 0, 0"),
                            (0x400AAEEB, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "6, 4, 4, 2, 2, 2"),
                            (0x400AAEEC, new string[]{ "Hind","Llama","Timberwolf", }, "2, 2, 2, 0, 0, 0"),
                            (0x400AAEED, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "6, 4, 4, 2, 2, 2"),
                            (0x400AAEEE, new string[]{ "Hind","Llama","Timberwolf", }, "2, 2, 2, 0, 0, 0"),
                            (0x400AAEEF, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAEF0, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAEF1, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAEF3, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAEF4, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAEF5, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAEF6, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAEF7, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAEF8, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAEFA, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAEFB, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAEFC, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAEFE, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAEFF, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF00, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF01, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF02, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF03, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF04, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF05, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF06, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF07, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF08, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF09, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF0A, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF0B, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF0C, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF0D, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF0E, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF0F, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF10, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF11, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF12, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF13, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF14, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF15, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF16, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF17, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF18, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF19, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF1A, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF1B, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF1C, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF1D, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF1E, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF1F, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF20, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF21, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF22, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF23, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF24, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF25, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF26, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF27, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF28, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF29, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF2A, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF2B, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF2C, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF2D, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF2E, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF2F, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF30, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF31, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF33, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF34, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF35, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF36, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF37, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF38, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF39, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF3A, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF3B, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF3C, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF3D, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF3E, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF3F, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF40, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF41, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF42, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF43, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF44, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF45, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF46, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF47, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF48, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF49, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF4A, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF4B, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF4D, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF4E, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF4F, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF51, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400AAF52, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400AAF53, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400AAF54, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400AAF55, new string[]{ "Forestostard","Ridablellama", }, "3, 3, 0, 0, 0, 0"),
                            (0x400AAF56, new string[]{ "Frenziedostard", }, "8"),
                            (0x400AAF57, new string[]{ "Imp","Mongbat", }, "2, 5, 0, 0, 0, 0"),
                            (0x400AAF58, new string[]{ "Imp","Harpy","Headlessone","Scorpion","Stoneharpy","Wyvern", }, "1, 1, 1, 1, 1, 1"),
                            (0x400AAF59, new string[]{ "Rabbit:JackRabbit","Bird","Rat","Hind","Llama","Horse", }, "20, 15, 5, 4, 3, 3"),
                            (0x400AAF5A, new string[]{ "Mongbat","Greathart", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AAF5B, new string[]{ "Rabbit:JackRabbit","Bird","Rat","Hind","Llama","Horse", }, "20, 15, 5, 4, 3, 3"),
                            (0x400AAF5C, new string[]{ "Mongbat","Greathart", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AAF5D, new string[]{ "Rabbit:JackRabbit","Bird","Rat","Hind","Llama","Horse", }, "20, 15, 5, 4, 3, 3"),
                            (0x400AAF5E, new string[]{ "Mongbat","Greathart", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AAF5F, new string[]{ "Rabbit:JackRabbit","Bird","Rat","Hind","Llama","Horse", }, "20, 15, 5, 4, 3, 3"),
                            (0x400AAF60, new string[]{ "Mongbat","Greathart", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AAF61, new string[]{ "Rabbit:JackRabbit","Bird","Rat","Hind","Llama","Horse", }, "20, 15, 5, 4, 3, 3"),
                            (0x400AAF63, new string[]{ "Mongbat","Greathart", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AAF64, new string[]{ "Bull","Greathart","Cow", }, "10, 6, 6, 0, 0, 0"),
                            (0x400AAF65, new string[]{ "Bull","Greathart","Cow", }, "2, 2, 2, 0, 0, 0"),
                            (0x400AAF66, new string[]{ "Gorilla","Panther","Giantserpent","Silverserpent","Mongbat", }, "6, 6, 6, 1, 6, 0"),
                            (0x400AAF67, new string[]{ "Gorilla","Panther","Giantserpent","Mongbat","Silverserpent", }, "5, 5, 5, 5, 1, 0"),
                            (0x400AAF69, new string[]{ "Gorilla","Panther","Giantserpent","Mongbat","Silverserpent", }, "5, 5, 5, 5, 1, 0"),
                            (0x400AAF6A, new string[]{ "Terathanmatriarch","Terathanavenger","Terathandrone","Terathanwarrior", }, "2, 4, 10, 10, 0, 0"),
                            (0x400AAF6B, new string[]{ "Ophidianmatriarch:Ophidianarchmage","Ophidianmage","Ophidianknight:Ophidianwarrior", }, "1, 1, 2, 0, 0, 0"),
                            (0x400AAF6C, new string[]{ "Ophidianmatriarch:Ophidianarchmage","Ophidianmage","Ophidianknight:Ophidianwarrior", }, "1, 1, 2, 0, 0, 0"),
                            (0x400AAF6D, new string[]{ "Ophidianmatriarch:Ophidianarchmage","Ophidianmage","Ophidianknight:Ophidianwarrior", }, "1, 1, 2, 0, 0, 0"),
                            (0x400AAF6E, new string[]{ "Ophidianmatriarch:Ophidianarchmage","Ophidianmage","Ophidianknight:Ophidianwarrior", }, "1, 1, 2, 0, 0, 0"),
                            (0x400AAF6F, new string[]{ "Ophidianmatriarch:Ophidianarchmage","Ophidianmage","Ophidianknight:Ophidianwarrior", }, "1, 1, 2, 0, 0, 0"),
                            (0x400AAF70, new string[]{ "Orc","Orccaptain","Orcishlord", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AAF71, new string[]{ "Orc", }, "5"),
                            (0x400AAF72, new string[]{ "Direwolf", }, "2"),
                            (0x400AAF73, new string[]{ "Orcishlord", }, "3"),
                            (0x400AAF74, new string[]{ "Orccaptain", }, "3"),
                            (0x400AAF75, new string[]{ "Orc", }, "3"),
                            (0x400AAF76, new string[]{ "Noble", }, "1"),
                            (0x400AAF77, new string[]{ "Orcishlord","Orc", }, "1, 3, 0, 0, 0, 0"),
                            (0x400AAF78, new string[]{ "Giantrat", }, "3"),
                            (0x400AAF79, new string[]{ "Direwolf", }, "3"),
                            (0x400AAF7A, new string[]{ "Orccaptain","Orcishlord","Orcishmage","Orcbomber", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AAF7B, new string[]{ "Orcbomber","Orccaptain","Orc", }, "1, 1, 2, 0, 0, 0"),
                            (0x400AAF7C, new string[]{ "Orcishlord","Orcishmage","Orcbomber","Orc", }, "1, 2, 1, 2, 0, 0"),
                            (0x400AAF7D, new string[]{ "Orc","Orcishmage","Orcishlord", }, "3, 1, 2, 0, 0, 0"),
                            (0x400AAF7E, new string[]{ "Corpser", }, "2"),
                            (0x400AAF7F, new string[]{ "Orc", }, "3"),
                            (0x400AAF80, new string[]{ "Orcbrute","Orc", }, "1, 4, 0, 0, 0, 0"),
                            (0x400AAF81, new string[]{ "Orcbomber","Orc", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AAF82, new string[]{ "Orcbomber","Earthelemental", }, "1, 4, 0, 0, 0, 0"),
                            (0x400AAF83, new string[]{ "hordeminion", }, "17"),
                            (0x400AAF84, new string[]{ "mongbat", }, "17"),
                            (0x400AAF85, new string[]{ "lizardman:ratman", }, "8"),
                            (0x400AAF86, new string[]{ "ettin:giantspider","wisp","mongbat:orc:troll", }, "2, 1, 6, 0, 0, 0"),
                            (0x400AAF87, new string[]{ "ettin:giantspider","wisp","mongbat:orc:troll", }, "2, 1, 6, 0, 0, 0"),
                            (0x400AAF88, new string[]{ "ettin:giantspider","wisp","mongbat:orc:troll", }, "2, 1, 6, 0, 0, 0"),
                            (0x400AAF89, new string[]{ "ettin:giantspider","wisp","mongbat:orc:troll", }, "2, 1, 6, 0, 0, 0"),
                            (0x400AAF8A, new string[]{ "ettin:giantspider","wisp","mongbat:orc:troll", }, "2, 1, 6, 0, 0, 0"),
                            (0x400AAF8B, new string[]{ "ettin:giantspider","wisp","mongbat:orc:troll", }, "2, 1, 6, 0, 0, 0"),
                            (0x400AAF8C, new string[]{ "ettin:harpy:ogre:troll", }, "8"),
                            (0x400AAF8D, new string[]{ "ettin:harpy:ogre:troll", }, "8"),
                            (0x400AAF8E, new string[]{ "ettin:harpy:ogre:troll", }, "8"),
                            (0x400AAF8F, new string[]{ "ettin:harpy:ogre:troll", }, "8"),
                            (0x400AAF90, new string[]{ "ettin:harpy:ogre:troll", }, "8"),
                            (0x400AAF91, new string[]{ "ettin:harpy:ogre:troll", }, "8"),
                            (0x400AAF92, new string[]{ "ettin:harpy:ogre:troll", }, "8"),
                            (0x400AAF93, new string[]{ "ettin:ogre", }, "6"),
                            (0x400AAF94, new string[]{ "orcishmage", }, "5"),
                            (0x400AAF95, new string[]{ "orc", }, "10"),
                            (0x400AAF96, new string[]{ "orc", }, "10"),
                            (0x400AAF97, new string[]{ "orc", }, "10"),
                            (0x400AAF98, new string[]{ "orc", }, "10"),
                            (0x400AAF99, new string[]{ "orc", }, "10"),
                            (0x400AAF9A, new string[]{ "orc", }, "10"),
                            (0x400AAF9C, new string[]{ "brigand:harpy:lizardman:orcishmage:orc:ratman:spectre:troll", }, "3"),
                            (0x400AAF9D, new string[]{ "brigand:harpy:lizardman:orcishmage:orc:ratman:spectre:troll", }, "7"),
                            (0x400AAF9E, new string[]{ "brigand:harpy:lizardman:orcishmage:orc:ratman:spectre:troll", }, "3"),
                            (0x400AAF9F, new string[]{ "brigand:harpy:lizardman:orcishmage:orc:ratman:spectre:troll", }, "3"),
                            (0x400AAFA0, new string[]{ "brigand:harpy:lizardman:orcishmage:orc:ratman:spectre:troll", }, "6"),
                            (0x400AAFA1, new string[]{ "brigand:harpy:lizardman:orcishmage:orc:ratman:spectre:troll", }, "7"),
                            (0x400AAFA2, new string[]{ "airelemental:ettin:ogre:waterelemental","orc:troll:lizardman", }, "5, 5, 0, 0, 0, 0"),
                            (0x400AAFA3, new string[]{ "airelemental:ettin:ogre:waterelemental","orc:troll:lizardman", }, "5, 5, 0, 0, 0, 0"),
                            (0x400AAFA4, new string[]{ "airelemental:ettin:ogre:waterelemental","orc:troll:lizardman", }, "5, 5, 0, 0, 0, 0"),
                            (0x400AAFA5, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFA6, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFA7, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFA9, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFAF, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFBC, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFBD, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFBF, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFC0, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFC2, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFC3, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFC4, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFC6, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFC7, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFC8, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400AAFC9, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400AAFCA, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400AAFCB, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400AAFCC, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400AAFCD, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400AAFCE, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400AAFCF, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400AAFD0, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400AAFD1, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400AAFD2, new string[]{ "earthelemental:ratman", }, "4"),
                            (0x400AAFD3, new string[]{ "earthelemental:ratman", }, "4"),
                            (0x400AAFD4, new string[]{ "earthelemental:ratman", }, "4"),
                            (0x400AAFD5, new string[]{ "ghoul:lich:lichlord","skeleton:zombie", }, "3, 5, 0, 0, 0, 0"),
                            (0x400AAFD6, new string[]{ "ghoul:lich:lichlord","skeleton:zombie", }, "3, 5, 0, 0, 0, 0"),
                            (0x400AAFD7, new string[]{ "gargoyle:gazer", }, "4"),
                            (0x400AAFD8, new string[]{ "gargoyle:gazer", }, "4"),
                            (0x400AAFD9, new string[]{ "gargoyle:gazer", }, "4"),
                            (0x400AAFDA, new string[]{ "gargoyle:gazer", }, "4"),
                            (0x400AAFDB, new string[]{ "gargoyle:gazer", }, "4"),
                            (0x400AAFDC, new string[]{ "troll", }, "3"),
                            (0x400AAFDE, new string[]{ "alligator:lizardman:silverserpent:snake","wisp", }, "8, 2, 0, 0, 0, 0"),
                            (0x400AAFDF, new string[]{ "alligator:lizardman:silverserpent:snake","wisp", }, "8, 2, 0, 0, 0, 0"),
                            (0x400AAFE0, new string[]{ "alligator:lizardman:silverserpent:snake","wisp", }, "8, 2, 0, 0, 0, 0"),
                            (0x400AAFE1, new string[]{ "alligator:lizardman:silverserpent:snake","wisp", }, "8, 2, 0, 0, 0, 0"),
                            (0x400AAFE2, new string[]{ "alligator:lizardman:silverserpent:snake","wisp", }, "8, 2, 0, 0, 0, 0"),
                            (0x400AAFE3, new string[]{ "alligator:lizardman:silverserpent:snake","wisp", }, "8, 2, 0, 0, 0, 0"),
                            (0x400AAFE4, new string[]{ "airelemental:gargoyle", }, "2"),
                            (0x400AAFE5, new string[]{ "airelemental:gargoyle", }, "2"),
                            (0x400AAFE6, new string[]{ "airelemental:gargoyle", }, "2"),
                            (0x400AAFE7, new string[]{ "airelemental:gargoyle", }, "2"),
                            (0x400AAFE8, new string[]{ "lizardman:ratman", }, "8"),
                            (0x400AAFE9, new string[]{ "lizardman:ratman", }, "8"),
                            (0x400AAFEA, new string[]{ "lizardman:ratman", }, "8"),
                            (0x400AAFEB, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AAFEC, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AAFED, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AAFEE, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AAFEF, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AAFF0, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AAFF1, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AAFF2, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AAFF3, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AAFF4, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AAFF5, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AAFF6, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400AAFF7, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400AAFF8, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400AAFF9, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400AAFFA, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400AAFFB, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400AAFFC, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400AAFFD, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400AAFFE, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400AAFFF, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400AB000, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB001, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB002, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB003, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB004, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB005, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB006, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB007, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB008, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB009, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB00A, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400AB00B, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400AB00C, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400AB00D, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400AB00E, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400AB00F, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400AB010, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400AB011, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400AB012, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400AB013, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400AB014, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400AB015, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400AB016, new string[]{ "ettin:gargoyle:gazer:orc:troll", }, "5"),
                            (0x400AB017, new string[]{ "ettin:gargoyle:gazer:orc:troll", }, "5"),
                            (0x400AB018, new string[]{ "ettin:gargoyle:gazer:orc:troll", }, "5"),
                            (0x400AB019, new string[]{ "ettin:troll", }, "2"),
                            (0x400AB01A, new string[]{ "ettin:troll", }, "2"),
                            (0x400AB01C, new string[]{ "ettin:troll", }, "2"),
                            (0x400AB01D, new string[]{ "ettin:troll", }, "2"),
                            (0x400AB01E, new string[]{ "ettin:troll", }, "2"),
                            (0x400AB01F, new string[]{ "ettin:troll", }, "2"),
                            (0x400AB020, new string[]{ "ettin:troll", }, "2"),
                            (0x400AB021, new string[]{ "ettin:troll", }, "2"),
                            (0x400AB022, new string[]{ "ettin:troll", }, "2"),
                            (0x400AB023, new string[]{ "ettin:troll", }, "2"),
                            (0x400AB024, new string[]{ "ettin:troll", }, "2"),
                            (0x400AB025, new string[]{ "ettin:troll", }, "2"),
                            (0x400AB026, new string[]{ "airelemental:waterelemental","direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:skeleton:troll", }, "2, 11, 0, 0, 0, 0"),
                            (0x400AB027, new string[]{ "airelemental:waterelemental","direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:skeleton:troll", }, "2, 11, 0, 0, 0, 0"),
                            (0x400AB028, new string[]{ "airelemental:waterelemental","direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:skeleton:troll", }, "2, 11, 0, 0, 0, 0"),
                            (0x400AB02B, new string[]{ "airelemental:waterelemental","direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:skeleton:troll", }, "2, 11, 0, 0, 0, 0"),
                            (0x400AB02C, new string[]{ "airelemental:waterelemental","direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:skeleton:troll", }, "2, 11, 0, 0, 0, 0"),
                            (0x400AB02D, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400AB02E, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400AB02F, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400AB030, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400AB031, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400AB033, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400AB034, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400AB035, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400AB036, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400AB037, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400AB038, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400AB039, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400AB03A, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400AB03B, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400AB03C, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400AB03D, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400AB03E, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400AB03F, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400AB040, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400AB041, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400AB042, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400AB043, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400AB044, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400AB045, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400AB046, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400AB047, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400AB048, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400AB049, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400AB04B, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400AB04C, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400AB04D, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400AB04E, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400AB04F, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400AB050, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400AB051, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400AB052, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400AB053, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400AB054, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400AB055, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400AB056, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400AB057, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400AB058, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400AB059, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400AB05A, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400AB05B, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400AB05D, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400AB05E, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400AB05F, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400AB060, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400AB061, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400AB063, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400AB065, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400AB066, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400AB068, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400AB06B, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400AB06C, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400AB06E, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400AB071, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400AB076, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400AB078, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400AB079, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400AB07D, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB07F, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB080, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB082, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB083, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB084, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB085, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB086, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB087, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB088, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB089, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB08B, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB08C, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB08E, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB08F, new string[]{ "corpser:direwolf:ettin:harpy:lizardman:orc:ratman:reaper", }, "8"),
                            (0x400AB090, new string[]{ "corpser:direwolf:ettin:harpy:lizardman:orc:ratman:reaper", }, "8"),
                            (0x400AB091, new string[]{ "corpser:direwolf:ettin:harpy:lizardman:orc:ratman:reaper", }, "8"),
                            (0x400AB092, new string[]{ "ettin:giantspider:harpy:lizardman:ogre:orc:skeleton:spectre:troll", }, "9"),
                            (0x400AB093, new string[]{ "ettin:giantspider:harpy:lizardman:ogre:orc:skeleton:spectre:troll", }, "9"),
                            (0x400AB094, new string[]{ "ettin:giantspider:harpy:lizardman:ogre:orc:skeleton:spectre:troll", }, "9"),
                            (0x400AB095, new string[]{ "ghoul:giantspider:headlessone:lizardman:orc:ratman:zombie","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB096, new string[]{ "ghoul:giantspider:headlessone:lizardman:orc:ratman:zombie","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB097, new string[]{ "ghoul:giantspider:headlessone:lizardman:orc:ratman:zombie","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB098, new string[]{ "ghoul:giantspider:headlessone:lizardman:orc:ratman:zombie","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB099, new string[]{ "ghoul:giantspider:headlessone:lizardman:orc:ratman:zombie","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB09A, new string[]{ "ghoul:giantspider:headlessone:lizardman:orc:ratman:zombie","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400AB09B, new string[]{ "giantspider:orc:ratman:spectre:zombie", }, "5"),
                            (0x400AB09C, new string[]{ "ettin:gazer:harpy:headlessone:ogre:troll", }, "6"),
                            (0x400AB09D, new string[]{ "ettin:gazer:harpy:headlessone:ogre:troll", }, "6"),
                            (0x400AB0A3, new string[]{ "ettin:gazer:harpy:headlessone:ogre:troll", }, "6"),
                            (0x400AB0A4, new string[]{ "ettin:gazer:harpy:headlessone:ogre:troll", }, "6"),
                            (0x400AB0A5, new string[]{ "ettin:gazer:harpy:headlessone:ogre:troll", }, "6"),
                            (0x400AB0A6, new string[]{ "ettin:gazer:harpy:headlessone:ogre:troll", }, "6"),
                            (0x400AB0A7, new string[]{ "airelemental:waterelemental","corpser:Direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:ratman:reaper", }, "2, 10, 0, 0, 0, 0"),
                            (0x400AB0A8, new string[]{ "airelemental:waterelemental","corpser:Direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:ratman:reaper", }, "2, 10, 0, 0, 0, 0"),
                            (0x400AB0A9, new string[]{ "airelemental:waterelemental","corpser:Direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:ratman:reaper", }, "2, 10, 0, 0, 0, 0"),
                            (0x400AB0AA, new string[]{ "airelemental:waterelemental","corpser:Direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:ratman:reaper", }, "2, 10, 0, 0, 0, 0"),
                            (0x400AB0AB, new string[]{ "corpser:direwolf:harpy:reaper:skeleton", }, "5"),
                            (0x400AB0AC, new string[]{ "corpser:direwolf:harpy:reaper:skeleton", }, "5"),
                            (0x400AB0AD, new string[]{ "corpser:direwolf:harpy:reaper:skeleton", }, "5"),
                            (0x400AB0AE, new string[]{ "corpser:direwolf:harpy:reaper:skeleton", }, "5"),
                            (0x400AB0AF, new string[]{ "corpser:direwolf:harpy:reaper:skeleton", }, "5"),
                            (0x400AB0B0, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400AB0B1, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400AB0B2, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400AB0B3, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400AB0B4, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400AB0B5, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400AB0B6, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400AB0B7, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400AB0B8, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400AB0B9, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400AB0BA, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400AB0BB, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400AB0BC, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400AB0BD, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400AB0C0, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400AB0C1, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400AB0C3, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400AB0C4, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400AB0C6, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400AB0C7, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "8"),
                            (0x400AB0C8, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400AB0C9, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400AB0CA, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400AB0CB, new string[]{ "alligator:giantserpent:lizardman:mongbat:ratman:silverserpent:snake", }, "7"),
                            (0x400AB0CC, new string[]{ "alligator:giantserpent:lizardman:mongbat:ratman:silverserpent:snake", }, "7"),
                            (0x400AB0CD, new string[]{ "alligator:giantserpent:lizardman:mongbat:ratman:silverserpent:snake", }, "7"),
                            (0x400AB0CE, new string[]{ "alligator:giantserpent:lizardman:mongbat:ratman:silverserpent:snake", }, "7"),
                            (0x400AB0CF, new string[]{ "alligator:giantserpent:lizardman:mongbat:ratman:silverserpent:snake", }, "7"),
                            (0x400AB0D0, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400AB0D1, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400AB0D2, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400AB0D3, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400AB0D4, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400AB0D5, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400AB0D6, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400AB0D7, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400AB0D8, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0DA, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0DB, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0DC, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0DD, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0DE, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0DF, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0E0, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0E1, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0E2, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0E3, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0E4, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0E5, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0E6, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0E7, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0E8, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0E9, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0EA, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0EB, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400AB0EC, new string[]{ "airelemental:ettin:gargoyle:gazer:orc:troll:waterelemental", }, "7"),
                            (0x400AB0ED, new string[]{ "airelemental:ettin:gargoyle:gazer:orc:troll:waterelemental", }, "7"),
                            (0x400AB0EE, new string[]{ "airelemental:ettin:gargoyle:gazer:orc:troll:waterelemental", }, "7"),
                            (0x400AB0EF, new string[]{ "airelemental:ettin:gargoyle:gazer:orc:troll:waterelemental", }, "7"),
                            (0x400AB0F0, new string[]{ "airelemental:ettin:gargoyle:gazer:orc:troll:waterelemental", }, "7"),
                            (0x400AB0F1, new string[]{ "timberwolf", }, "7"),
                            (0x400AB0F2, new string[]{ "timberwolf", }, "7"),
                            (0x400AB0F3, new string[]{ "timberwolf", }, "7"),
                            (0x400AB0F4, new string[]{ "timberwolf", }, "7"),
                            (0x400AB0F5, new string[]{ "timberwolf", }, "7"),
                            (0x400AB0F6, new string[]{ "greathart","hind", }, "2, 5, 0, 0, 0, 0"),
                            (0x400AB0F7, new string[]{ "greathart","hind", }, "2, 5, 0, 0, 0, 0"),
                            (0x400AB0F8, new string[]{ "greathart","hind", }, "2, 5, 0, 0, 0, 0"),
                            (0x400AB0F9, new string[]{ "greathart","hind", }, "2, 5, 0, 0, 0, 0"),
                            (0x400AB0FA, new string[]{ "greathart","hind", }, "2, 5, 0, 0, 0, 0"),
                            (0x400AB0FB, new string[]{ "greathart","hind", }, "2, 5, 0, 0, 0, 0"),
                            (0x400AB0FC, new string[]{ "ettin:harpy:headlessone:lizardman:ogre:orc:skeleton:troll:zombie","wisp", }, "9, 1, 0, 0, 0, 0"),
                            (0x400AB0FD, new string[]{ "ettin:harpy:headlessone:lizardman:ogre:orc:skeleton:troll:zombie","wisp", }, "9, 1, 0, 0, 0, 0"),
                            (0x400AB0FE, new string[]{ "ettin:harpy:headlessone:lizardman:ogre:orc:skeleton:troll:zombie","wisp", }, "9, 1, 0, 0, 0, 0"),
                            (0x400AB0FF, new string[]{ "ettin:harpy:headlessone:lizardman:ogre:orc:skeleton:troll:zombie","wisp", }, "9, 1, 0, 0, 0, 0"),
                            (0x400AB100, new string[]{ "ettin:harpy:headlessone:lizardman:ogre:orc:skeleton:troll:zombie","wisp", }, "9, 1, 0, 0, 0, 0"),
                            (0x400AB101, new string[]{ "ettin:ogre:orc:ratman:skeleton:spectre:zombie", }, "7"),
                            (0x400AB102, new string[]{ "ettin:ogre:orc:ratman:skeleton:spectre:zombie", }, "7"),
                            (0x400AB103, new string[]{ "ettin:gargoyle:gazer:ogre:orc:troll:waterelemental", }, "7"),
                            (0x400AB104, new string[]{ "ettin:gargoyle:gazer:ogre:orc:troll:waterelemental", }, "7"),
                            (0x400AB105, new string[]{ "ettin:gargoyle:gazer:ogre:orc:troll:waterelemental", }, "7"),
                            (0x400AB107, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AB108, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AB109, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AB10A, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AB10B, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AB10C, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AB10D, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AB10E, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AB10F, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AB110, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AB111, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AB112, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AB113, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AB114, new string[]{ "orc:ratman", }, "6"),
                            (0x400AB115, new string[]{ "orc:ratman", }, "6"),
                            (0x400AB116, new string[]{ "orc:ratman", }, "6"),
                            (0x400AB117, new string[]{ "orc:ratman", }, "6"),
                            (0x400AB118, new string[]{ "orc:ratman", }, "6"),
                            (0x400AB119, new string[]{ "orc:ratman", }, "6"),
                            (0x400AB11A, new string[]{ "Orc","Orcishlord","Orcishmage", }, "1, 3, 2, 5, 3, 2"),
                            (0x400AB11B, new string[]{ "Orc","Orcishlord","Orcishmage", }, "1, 3, 2, 5, 3, 2"),
                            (0x400AB11C, new string[]{ "orc","ettin","ratman","giantspider","reaper", }, "10, 10, 10, 10, 10, 0"),
                            (0x400AB11E, new string[]{ "orc","ettin","ratman","giantspider","reaper", }, "10, 10, 10, 10, 10, 0"),
                            (0x400AB11F, new string[]{ "orc","ettin","ratman","giantspider","reaper", }, "10, 10, 10, 10, 10, 0"),
                            (0x400AB120, new string[]{ "orc","ettin","ratman","giantspider","reaper", }, "10, 10, 10, 10, 10, 0"),
                            (0x400AB121, new string[]{ "orc","ettin","ratman","giantspider","reaper", }, "10, 10, 10, 10, 10, 0"),
                            (0x400AB123, new string[]{ "orc","ettin","ratman","giantspider","reaper", }, "10, 10, 10, 10, 10, 0"),
                            (0x400AB124, new string[]{ "Daemon", }, "4"),
                            (0x400AB125, new string[]{ "Imp", }, "4"),
                            (0x400AB126, new string[]{ "Imp", }, "4"),
                            (0x400AB128, new string[]{ "Imp", }, "4"),
                            (0x400AB129, new string[]{ "Imp", }, "4"),
                            (0x400AB12A, new string[]{ "orc","giantspider","giantserpent","gorilla","tropicalbird","whippingvine", }, "5, 5, 5, 5, 5, 5"),
                            (0x400AB12B, new string[]{ "orc","giantspider","giantserpent","gorilla","tropicalbird","whippingvine", }, "10, 10, 10, 10, 10, 10"),
                            (0x400AB12C, new string[]{ "orc","giantspider","giantserpent","gorilla","tropicalbird","whippingvine", }, "10, 10, 10, 10, 10, 10"),
                            (0x400AB12D, new string[]{ "orc","giantspider","giantserpent","gorilla","tropicalbird","whippingvine", }, "10, 10, 10, 10, 10, 10"),
                            (0x400AB12E, new string[]{ "orc","giantspider","giantserpent","gorilla","tropicalbird","whippingvine", }, "10, 10, 10, 10, 10, 10"),
                            (0x400AB12F, new string[]{ "orc","giantspider","giantserpent","gorilla","tropicalbird","whippingvine", }, "10, 10, 10, 10, 10, 10"),
                            (0x400AB130, new string[]{ "Headlessone","Orc","Orccaptain","Orcishlord","Orcishmage", }, "3, 6, 1, 2, 1, 0"),
                            (0x400AB131, new string[]{ "Savage","Savagerider","Savageshaman", }, "2, 1, 1, 0, 0, 0"),
                            (0x400AB132, new string[]{ "Savage","Savagerider","Savageshaman", }, "2, 1, 1, 0, 0, 0"),
                            (0x400AB133, new string[]{ "Brigand", }, "7"),
                            (0x400AB134, new string[]{ "Lich", }, "8"),
                            (0x400AB135, new string[]{ "Orc","Orcishlord","Orcishmage", }, "3, 1, 1, 0, 0, 0"),
                            (0x400AB136, new string[]{ "Orc","Orcishlord","Orcishmage", }, "3, 1, 1, 0, 0, 0"),
                            (0x400AB138, new string[]{ "orc","orcBomber","OrcCaptain","OrcishLord","OrcishMage", }, "8, 1, 1, 2, 2, 0"),
                            (0x400AB139, new string[]{ "Skeleton", }, "2"),
                            (0x400AB13A, new string[]{ "Wraith:Shade:Spectre","Ghoul", }, "4, 4, 0, 0, 0, 0"),
                            (0x400AB13B, new string[]{ "Wraith:Shade:Spectre", }, "1"),
                            (0x400AB13C, new string[]{ "Wraith:Shade:Spectre", }, "1"),
                            (0x400AB13D, new string[]{ "Lich", }, "1"),
                            (0x400AB13E, new string[]{ "Daemon","Drake", }, "1, 2, 0, 0, 0, 0"),
                            (0x400AB13F, new string[]{ "Dragon","Drake", }, "1, 2, 0, 0, 0, 0"),
                            (0x400AB140, new string[]{ "Daemon", }, "3"),
                            (0x400AB142, new string[]{ "Dragon","Drake", }, "1, 2, 0, 0, 0, 0"),
                            (0x400AB144, new string[]{ "Wisp", }, "8"),
                            (0x400AB145, new string[]{ "Ghoul","Giantrat","Giantserpent","Headlessone","Jwilson", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB146, new string[]{ "Iceserpent","Icesnake","Polarbear","Snowleopard","Whitewolf","Frostooze", }, "1, 1, 1, 1, 1, 1"),
                            (0x400AB147, new string[]{ "Frostspider","Frosttroll", }, "1, 4, 0, 0, 0, 0"),
                            (0x400AB148, new string[]{ "Brownbear:Grizzlybear:Blackbear","Direwolf:Timberwolf:Greywolf","Rabbit:JackRabbit","Cougar:Panther","Bird:Eagle", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB14A, new string[]{ "Alligator","Bullfrog","Corpser","Swamptentacle", }, "2, 3, 1, 1, 0, 0"),
                            (0x400AB14B, new string[]{ "Giantserpent","Giantspider","Boar","Greathart","Hind","Llama", }, "1, 1, 1, 1, 1, 1"),
                            (0x400AB14C, new string[]{ "Horse","Rat", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB14D, new string[]{ "Brownbear:Grizzlybear:Blackbear","Direwolf:Timberwolf:Greywolf","Cougar:Panther","Giantserpent","Greathart", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB14E, new string[]{ "Alligator","Bullfrog","Gorilla","Giantserpent","Gianttoad", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB14F, new string[]{ "Mummy","Ophidianwarrior","Scorpion","Giantserpent", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB150, new string[]{ "Rottingcorpse","Skeleton","Slime","Snake","Strongmongbat", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB151, new string[]{ "Ghoul","Giantserpent","Headlessone","Jwilson","Zombie", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB152, new string[]{ "Cat","Chicken","Dog","Pig","Sheep", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB153, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB154, new string[]{ "Orccamp", }, "1"),
                            (0x400AB155, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB156, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB157, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB158, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB159, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB15A, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB15C, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB15E, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB165, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB167, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB168, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB169, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB16A, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB16C, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400AB16D, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB16E, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB16F, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB170, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB171, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB172, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB174, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB178, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB179, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB17A, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB17F, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB180, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB181, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB182, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB183, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB184, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB185, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB186, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB187, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB188, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB189, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB18A, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB18B, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB18C, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB18D, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB18E, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB18F, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB190, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB191, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB192, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB193, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB194, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB195, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB196, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB197, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB198, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB199, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB19A, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB19B, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB19E, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB19F, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1A0, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1A2, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1A3, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1A4, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1A5, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1A6, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1A7, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1A8, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1A9, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1AA, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1AB, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1AC, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1AD, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1AE, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1AF, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1B0, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1B1, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1B2, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1B3, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400AB1B4, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1B5, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1B6, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","deepseaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1B7, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1B8, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1B9, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1BA, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1BB, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1BD, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1BE, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1BF, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1C0, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1C1, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1C2, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1C4, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1C5, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1C6, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1C7, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1C8, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1DD, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1DE, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1DF, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1E0, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1E1, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1E2, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1E3, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1E4, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1E5, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1E6, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1E7, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1E8, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1E9, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1EA, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1EB, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","deepseaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1EC, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1ED, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1EE, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1EF, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1F0, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1F1, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1F2, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1F3, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1F4, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1F5, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1F6, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1F7, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1F8, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1F9, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","kraken", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1FA, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1FB, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1FC, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1FD, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB1FF, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB200, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB201, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB202, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","deepseaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB203, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB204, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","deepseaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB205, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB206, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB207, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB209, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB20A, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB20B, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB20D, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","deepseaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB20E, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB20F, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB210, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB211, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400AB212, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "13, 4, 1, 0, 0, 0"),
                            (0x400AB214, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "13, 4, 1, 0, 0, 0"),
                            (0x400AB215, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "13, 4, 1, 0, 0, 0"),
                            (0x400AB216, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "13, 4, 1, 0, 0, 0"),
                            (0x400AB217, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB218, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB219, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB21A, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB21B, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","deepseaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB21C, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","deepseaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB21D, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB21E, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB21F, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB220, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB221, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB222, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB223, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB225, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB226, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB228, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB229, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB22A, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB22B, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB22C, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB22D, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB22E, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB22F, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB230, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB231, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB232, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB233, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB234, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB235, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB237, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","deepseaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB238, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB239, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB23A, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB23B, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB23D, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB23E, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB242, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB244, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB245, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB246, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB247, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","deepseaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB248, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400AB249, new string[]{ "Earthelemental","Scorpion", }, "4, 2, 0, 0, 0, 0"),
                            (0x400AB24A, new string[]{ "Earthelemental","Scorpion", }, "7, 3, 0, 0, 0, 0"),
                            (0x400AB24B, new string[]{ "Earthelemental","Scorpion", }, "4, 1, 0, 0, 0, 0"),
                            (0x400AB24C, new string[]{ "Earthelemental", }, "4"),
                            (0x400AB24D, new string[]{ "Earthelemental", }, "4"),
                            (0x400AB24E, new string[]{ "Earthelemental","Scorpion", }, "4, 2, 0, 0, 0, 0"),
                            (0x400AB24F, new string[]{ "Earthelemental","Scorpion", }, "6, 20, 0, 0, 0, 0"),
                            (0x400AB250, new string[]{ "Airelemental", }, "3"),
                            (0x400AB252, new string[]{ "Dullcopperelemental", }, "6"),
                            (0x400AB253, new string[]{ "Airelemental", }, "3"),
                            (0x400AB254, new string[]{ "Airelemental","Waterelemental", }, "3, 2, 0, 0, 0, 0"),
                            (0x400AB255, new string[]{ "Kraken","Waterelemental", }, "1, 3, 0, 0, 0, 0"),
                            (0x400AB256, new string[]{ "Kraken","Seaserpent", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB257, new string[]{ "Dullcopperelemental", }, "2"),
                            (0x400AB258, new string[]{ "Dullcopperelemental", }, "4"),
                            (0x400AB259, new string[]{ "Earthelemental", }, "2"),
                            (0x400AB25A, new string[]{ "Fireelemental","Poisonelemental", }, "1, 4, 0, 0, 0, 0"),
                            (0x400AB25B, new string[]{ "Kraken","Seaserpent","Waterelemental", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB25C, new string[]{ "Evilmage","Evilmagelord", }, "5, 5, 0, 0, 0, 0"),
                            (0x400AB25D, new string[]{ "Airelemental","Earthelemental", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AB25E, new string[]{ "Fireelemental","Poisonelemental","Scorpion", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB25F, new string[]{ "Airelemental","Earthelemental", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AB260, new string[]{ "Fireelemental","Poisonelemental","Scorpion", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB261, new string[]{ "Airelemental", }, "2"),
                            (0x400AB262, new string[]{ "Eldergazer", }, "2"),
                            (0x400AB263, new string[]{ "Dullcopperelemental","Fireelemental", }, "4, 2, 0, 0, 0, 0"),
                            (0x400AB264, new string[]{ "Kraken","Seaserpent","Waterelemental", }, "2, 2, 3, 0, 0, 0"),
                            (0x400AB265, new string[]{ "Evilmage","Evilmagelord", }, "5, 5, 0, 0, 0, 0"),
                            (0x400AB267, new string[]{ "Kraken","Seaserpent","Waterelemental", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB269, new string[]{ "Earthelemental","Scorpion", }, "4, 2, 0, 0, 0, 0"),
                            (0x400AB26A, new string[]{ "Earthelemental","Fireelemental", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AB26B, new string[]{ "Airelemental", }, "2"),
                            (0x400AB26C, new string[]{ "Fireelemental", }, "2"),
                            (0x400AB26D, new string[]{ "Airelemental", }, "2"),
                            (0x400AB26E, new string[]{ "Kraken","Seaserpent", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB26F, new string[]{ "Evilmage","Evilmagelord", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB270, new string[]{ "Eldergazer", }, "2"),
                            (0x400AB271, new string[]{ "Airelemental", }, "5"),
                            (0x400AB272, new string[]{ "Bloodelemental", }, "8"),
                            (0x400AB273, new string[]{ "Fireelemental", }, "2"),
                            (0x400AB274, new string[]{ "Earthelemental", }, "4"),
                            (0x400AB275, new string[]{ "Bloodelemental", }, "2"),
                            (0x400AB276, new string[]{ "Earthelemental","Fireelemental", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AB277, new string[]{ "Kraken","Seaserpent","Waterelemental", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB278, new string[]{ "Kraken","Seaserpent","Waterelemental", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB279, new string[]{ "Kraken","Waterelemental", }, "1, 3, 0, 0, 0, 0"),
                            (0x400AB27A, new string[]{ "Blacksolenworker", }, "2"),
                            (0x400AB27C, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400AB27D, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400AB27E, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400AB280, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400AB281, new string[]{ "Blacksolenwarrior", }, "1"),
                            (0x400AB283, new string[]{ "Blacksolenwarrior", }, "1"),
                            (0x400AB284, new string[]{ "Blacksolenwarrior", }, "1"),
                            (0x400AB285, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400AB286, new string[]{ "Blacksolenworker", }, "2"),
                            (0x400AB287, new string[]{ "Antlion", }, "1"),
                            (0x400AB288, new string[]{ "Blacksolenworker", }, "2"),
                            (0x400AB289, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400AB28A, new string[]{ "Blacksolenworker","Blacksolenqueen", }, "2, 1, 0, 0, 0, 0"),
                            (0x400AB28B, new string[]{ "Blacksolenwarrior", }, "1"),
                            (0x400AB28C, new string[]{ "Blacksolenwarrior", }, "2"),
                            (0x400AB28D, new string[]{ "Blacksolenwarrior", }, "1"),
                            (0x400AB28E, new string[]{ "Beetle", }, "2"),
                            (0x400AB28F, new string[]{ "Blacksolenworker", }, "2"),
                            (0x400AB290, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400AB291, new string[]{ "Antlion", }, "1"),
                            (0x400AB292, new string[]{ "Dreadspider", }, "2"),
                            (0x400AB293, new string[]{ "Blacksolenwarrior", }, "1"),
                            (0x400AB294, new string[]{ "Blacksolenwarrior", }, "2"),
                            (0x400AB295, new string[]{ "Blacksolenwarrior", }, "2"),
                            (0x400AB296, new string[]{ "Blacksolenwarrior", }, "1"),
                            (0x400AB297, new string[]{ "Blacksolenqueen","Blacksolenworker", }, "1, 2, 0, 0, 0, 0"),
                            (0x400AB298, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400AB299, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400AB29A, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400AB29B, new string[]{ "Blacksolenworker", }, "3"),
                            (0x400AB29C, new string[]{ "Beetle", }, "2"),
                            (0x400AB29D, new string[]{ "Blacksolenwarrior", }, "2"),
                            (0x400AB29E, new string[]{ "Antlion", }, "1"),
                            (0x400AB29F, new string[]{ "Blacksolenworker", }, "2"),
                            (0x400AB2A1, new string[]{ "Blacksolenwarrior", }, "2"),
                            (0x400AB2A2, new string[]{ "Blacksolenworker", }, "2"),
                            (0x400AB2A3, new string[]{ "Antlion", }, "3"),
                            (0x400AB2A4, new string[]{ "Antlion", }, "1"),
                            (0x400AB2A5, new string[]{ "redsoleninfiltratorqueen","redsoleninfiltratorwarrior", }, "1, 2, 0, 0, 0, 0"),
                            (0x400AB2A7, new string[]{ "redsoleninfiltratorqueen","redsoleninfiltratorwarrior", }, "1, 2, 0, 0, 0, 0"),
                            (0x400AB2A9, new string[]{ "redsoleninfiltratorqueen","redsoleninfiltratorwarrior", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB2AA, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2AB, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB2AC, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2AD, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2AE, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB2AF, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2B0, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2B1, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB2B2, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2B4, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2B6, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB2B7, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2B8, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2B9, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB2BA, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2BB, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2BC, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB2BE, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2BF, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2C0, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB2C1, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2C2, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2C3, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB2C4, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2C5, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2C6, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB2C7, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2C8, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2C9, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400AB2CA, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB2CB, new string[]{ "dragon","drake", }, "1, 2, 0, 0, 0, 0"),
                            (0x400AB2CC, new string[]{ "lichlord","lich", }, "1, 2, 0, 0, 0, 0"),
                            (0x400AB2CD, new string[]{ "daemon", }, "3"),
                            (0x400AB2CE, new string[]{ "Bird","Cat","Dog","Rat", }, "15, 15, 15, 15, 0, 0"),
                            (0x400AB2CF, new string[]{ "Cow", }, "7"),
                            (0x400AB2D0, new string[]{ "Cow", }, "7"),
                            (0x400AB2D1, new string[]{ "Bird","Cat","Dog","Rat", }, "5, 5, 5, 5, 0, 0"),
                            (0x400AB2D2, new string[]{ "Chicken", }, "5"),
                            (0x400AB2D4, new string[]{ "Bird","Cat","Dog","Rat", }, "5, 5, 5, 5, 0, 0"),
                            (0x400AB2E0, new string[]{ "Chicken", }, "5"),
                            (0x400AB2E1, new string[]{ "Bird","Cat","Dog","Rat", }, "7, 7, 7, 7, 0, 0"),
                            (0x400AB2E4, new string[]{ "Chicken", }, "5"),
                            (0x400AB2E6, new string[]{ "Bird","Cat","Dog","Rat", }, "12, 12, 12, 12, 0, 0"),
                            (0x400AB2E7, new string[]{ "Bird","Cat","Dog","Rat", }, "8, 8, 8, 8, 0, 0"),
                            (0x400AB2E8, new string[]{ "Chicken:Sheep:Cow", }, "3"),
                            (0x400AB2EA, new string[]{ "Chicken:Sheep:Cow", }, "3"),
                            (0x400AB2EB, new string[]{ "Chicken:Sheep:Cow", }, "3"),
                            (0x400AB2ED, new string[]{ "Bird","Cat","Dog","Rat", }, "20, 20, 20, 20, 0, 0"),
                            (0x400AB2EE, new string[]{ "Sheep", }, "15"),
                            (0x400AB2F1, new string[]{ "Sheep", }, "15"),
                            (0x400AB2F2, new string[]{ "Sheep", }, "15"),
                            (0x400AB2F3, new string[]{ "Chicken", }, "2"),
                            (0x400AB2F4, new string[]{ "Chicken", }, "2"),
                            (0x400AB2F5, new string[]{ "Chicken", }, "2"),
                            (0x400AB2F6, new string[]{ "Bird","Cat","Dog","Rat", }, "4, 4, 4, 4, 0, 0"),
                            (0x400AB2F7, new string[]{ "Chicken", }, "1"),
                            (0x400AB2F8, new string[]{ "Bird","Cat","Dog","Rat", }, "10, 10, 10, 10, 0, 0"),
                            (0x400AB2F9, new string[]{ "Bird","Cat","Dog","Rat", }, "10, 10, 10, 10, 0, 0"),
                            (0x400AB2FA, new string[]{ "Bird","Cat","Dog","Rat", }, "5, 5, 5, 5, 0, 0"),
                            (0x400AB2FB, new string[]{ "Bird","Cat","Dog","Rat", }, "5, 5, 5, 5, 0, 0"),
                            (0x400AB2FC, new string[]{ "Bird","Cat","Dog","Rat", }, "3, 3, 3, 3, 0, 0"),
                            (0x400AB2FD, new string[]{ "Bird","Cat","Dog","Rat", }, "4, 4, 4, 4, 0, 0"),
                            (0x400AB2FE, new string[]{ "Chicken:Cow:Sheep", }, "5"),
                            (0x400AB2FF, new string[]{ "Chicken:Cow:Sheep", }, "5"),
                            (0x400AB300, new string[]{ "Bird","Cat","Dog","Rat", }, "10, 10, 10, 10, 0, 0"),
                            (0x400AB301, new string[]{ "Bird","Cat","Dog","Rat", }, "5, 5, 5, 5, 0, 0"),
                            (0x400AB302, new string[]{ "Bird","Cat","Dog","Rat", }, "12, 12, 12, 12, 0, 0"),
                            (0x400AB304, new string[]{ "Bird","Cat","Dog","Rat", }, "10, 10, 10, 10, 0, 0"),
                            (0x400AB308, new string[]{ "Chicken", }, "10"),
                            (0x400AB30B, new string[]{ "Bird","Cat","Dog","Rat", }, "4, 4, 4, 4, 0, 0"),
                            (0x400AB30E, new string[]{ "Bird","Cat","Dog","Rat", }, "6, 6, 6, 6, 0, 0"),
                            (0x400AB30F, new string[]{ "Bird","Cat","Dog","Rat", }, "6, 6, 6, 6, 0, 0"),
                            (0x400AB310, new string[]{ "Bird","Cat","Dog","Rat", }, "5, 5, 5, 5, 0, 0"),
                            (0x400AB311, new string[]{ "Bird","Cat","Dog","Rat", }, "4, 4, 4, 4, 0, 0"),
                            (0x400AB312, new string[]{ "Bird","Cat","Dog","Rat", }, "18, 18, 18, 18, 0, 0"),
                            (0x400AB313, new string[]{ "Bird","Cat","Dog","Rat", }, "12, 12, 12, 12, 0, 0"),
                            (0x400AB314, new string[]{ "Chicken:Cow:Sheep", }, "5"),
                            (0x400AB317, new string[]{ "Chicken", }, "2"),
                            (0x400AB318, new string[]{ "Chicken", }, "1"),
                            (0x400AB319, new string[]{ "bird", }, "3"),
                            (0x400AB31B, new string[]{ "bird", }, "5"),
                            (0x400AB31C, new string[]{ "bird", }, "3"),
                            (0x400AB31D, new string[]{ "bird", }, "5"),
                            (0x400AB31E, new string[]{ "bird", }, "3"),
                            (0x400AB31F, new string[]{ "bird", }, "5"),
                            (0x400AB320, new string[]{ "Bird", }, "10"),
                            (0x400AB321, new string[]{ "Bird","Cat","Dog","Rat", }, "12, 12, 12, 12, 0, 0"),
                            (0x400AB322, new string[]{ "Bird","Cat","Dog","Rat", }, "6, 6, 6, 6, 0, 0"),
                            (0x400AB323, new string[]{ "Chicken:Sheep:Cow:Bull", }, "5"),
                            (0x400AB324, new string[]{ "Orderguard", }, "1"),
                            (0x400AB325, new string[]{ "Orderguard", }, "1"),
                            (0x400AB326, new string[]{ "Chaosguard", }, "1"),
                            (0x400AB327, new string[]{ "Chaosguard", }, "1"),
                            (0x400AB328, new string[]{ "Towncrier", }, "1"),
                            (0x400AB329, new string[]{ "Towncrier", }, "1"),
                            (0x400AB32A, new string[]{ "Towncrier", }, "1"),
                            (0x400AB32B, new string[]{ "Towncrier", }, "1"),
                            (0x400AB32C, new string[]{ "Towncrier", }, "1"),
                            (0x400AB32D, new string[]{ "Towncrier", }, "1"),
                            (0x400AB32E, new string[]{ "Towncrier", }, "1"),
                            (0x400AB330, new string[]{ "Towncrier", }, "1"),
                            (0x400AB331, new string[]{ "Towncrier", }, "1"),
                            (0x400AB332, new string[]{ "Towncrier", }, "1"),
                            (0x400AB333, new string[]{ "Towncrier", }, "1"),
                            (0x400AB334, new string[]{ "Towncrier", }, "1"),
                            (0x400AB335, new string[]{ "Towncrier", }, "1"),
                            (0x400AB336, new string[]{ "Towncrier", }, "1"),
                            (0x400AB338, new string[]{ "Towncrier", }, "1"),
                            (0x400AB339, new string[]{ "Towncrier", }, "1"),
                            (0x400AB33A, new string[]{ "Towncrier", }, "1"),
                            (0x400AB33B, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400AB33C, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400AB33D, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400AB33E, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400AB33F, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400AB341, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400AB342, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400AB343, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400AB344, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400AB345, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400AB346, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400AB347, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400AB349, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400AB34A, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400AB34C, new string[]{ "Dreadspider","Fireelemental","Hellcat","Lavalizard","Lavasnake", }, "2, 1, 1, 1, 1, 0"),
                            (0x400AB34D, new string[]{ "Dreadspider","Fireelemental","Hellcat","Lavalizard","Lavasnake", }, "2, 1, 1, 1, 1, 0"),
                            (0x400AB34E, new string[]{ "Dreadspider","Fireelemental","Hellcat","Lavalizard","Lavasnake", }, "2, 1, 1, 1, 1, 0"),
                            (0x400AB34F, new string[]{ "Dreadspider","Fireelemental","Hellcat","Lavalizard","Lavasnake", }, "2, 1, 1, 1, 1, 0"),
                            (0x400AB351, new string[]{ "Dreadspider","Fireelemental","Hellcat","Lavalizard","Lavasnake", }, "2, 1, 1, 1, 1, 0"),
                            (0x400AB352, new string[]{ "Herbalist","Mage", }, "2, 2, 0, 0, 0, 0"),
                            (0x400AB353, new string[]{ "jeweler", }, "1"),
                            (0x400AB355, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB357, new string[]{ "bowyer", }, "1"),
                            (0x400AB358, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB359, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB35A, new string[]{ "carpenter","architect","realestatebroker", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB35B, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB35C, new string[]{ "animaltrainer", }, "1"),
                            (0x400AB35D, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB35E, new string[]{ "innkeeper", }, "1"),
                            (0x400AB360, new string[]{ "tinkerguildmaster", }, "1"),
                            (0x400AB361, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB363, new string[]{ "warriorguildmaster","hirefighter", }, "1, 2, 0, 0, 0, 0"),
                            (0x400AB364, new string[]{ "innkeeper", }, "1"),
                            (0x400AB365, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB366, new string[]{ "tanner","furtrader", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB367, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB368, new string[]{ "fisherman", }, "1"),
                            (0x400AB369, new string[]{ "scribe", }, "1"),
                            (0x400AB36A, new string[]{ "blacksmithguildmaster","blacksmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB36B, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB36C, new string[]{ "animaltrainer", }, "1"),
                            (0x400AB36D, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB36E, new string[]{ "butcher", }, "1"),
                            (0x400AB36F, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB370, new string[]{ "herbalist","alchemist","customhairstylist", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB371, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB372, new string[]{ "scribe", }, "1"),
                            (0x400AB373, new string[]{ "jeweler", }, "1"),
                            (0x400AB374, new string[]{ "baker", }, "1"),
                            (0x400AB375, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB376, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB377, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB378, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB379, new string[]{ "bard","bardguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB37A, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB37B, new string[]{ "minerguildmaster", }, "1"),
                            (0x400AB37C, new string[]{ "innkeeper", }, "1"),
                            (0x400AB37D, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB37E, new string[]{ "mage", }, "1"),
                            (0x400AB37F, new string[]{ "towncrier", }, "1"),
                            (0x400AB380, new string[]{ "orderguard","hirefighter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB381, new string[]{ "farmer","rancher", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB382, new string[]{ "escortablemage", }, "1"),
                            (0x400AB383, new string[]{ "orderguard", }, "1"),
                            (0x400AB384, new string[]{ "hirefighter", }, "3"),
                            (0x400AB385, new string[]{ "animaltrainer", }, "1"),
                            (0x400AB386, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB387, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB388, new string[]{ "innkeeper", }, "1"),
                            (0x400AB389, new string[]{ "thiefguildmaster", }, "1"),
                            (0x400AB38A, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB38B, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB38C, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB38D, new string[]{ "carpenter","architect","realestatebroker", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB38E, new string[]{ "fisherman", }, "1"),
                            (0x400AB38F, new string[]{ "tanner","furtrader", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB390, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB391, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB392, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB393, new string[]{ "fisherman", }, "1"),
                            (0x400AB394, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB396, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB397, new string[]{ "fisherman", }, "1"),
                            (0x400AB398, new string[]{ "fisherman", }, "1"),
                            (0x400AB399, new string[]{ "fisherman", }, "1"),
                            (0x400AB39A, new string[]{ "jeweler", }, "1"),
                            (0x400AB39B, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB39C, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB39E, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB39F, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB3A0, new string[]{ "baker", }, "1"),
                            (0x400AB3A1, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3A2, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3A3, new string[]{ "butcher","farmer", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3A4, new string[]{ "butcher", }, "1"),
                            (0x400AB3A6, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3A7, new string[]{ "carpenter","architect","realestatebroker", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB3A8, new string[]{ "tinker","tinkerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3A9, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB3AA, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3AB, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3AC, new string[]{ "fisherman", }, "1"),
                            (0x400AB3AD, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3AE, new string[]{ "scribe", }, "1"),
                            (0x400AB3AF, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB3B0, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB3B1, new string[]{ "innkeeper", }, "1"),
                            (0x400AB3B2, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3B3, new string[]{ "fisherguildmaster", }, "1"),
                            (0x400AB3B4, new string[]{ "innkeeper", }, "1"),
                            (0x400AB3B5, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB3B6, new string[]{ "tinker","tinkerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3B7, new string[]{ "baker", }, "1"),
                            (0x400AB3B8, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3B9, new string[]{ "baker", }, "1"),
                            (0x400AB3BA, new string[]{ "jeweler", }, "1"),
                            (0x400AB3BB, new string[]{ "fisherguildmaster", }, "1"),
                            (0x400AB3BC, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3BD, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB3BF, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3C0, new string[]{ "fisherman", }, "1"),
                            (0x400AB3C1, new string[]{ "minerguildmaster", }, "1"),
                            (0x400AB3C2, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB3C3, new string[]{ "provisioner", }, "1"),
                            (0x400AB3C5, new string[]{ "cobbler", }, "1"),
                            (0x400AB3C6, new string[]{ "butcher", }, "1"),
                            (0x400AB3C7, new string[]{ "animaltrainer", }, "1"),
                            (0x400AB3C8, new string[]{ "tanner","furtrader", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3CA, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3CB, new string[]{ "architect", }, "1"),
                            (0x400AB3CC, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3CD, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3CF, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB3D0, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3D1, new string[]{ "tinker","tinkerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3D2, new string[]{ "minerguildmaster", }, "1"),
                            (0x400AB3D3, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3D4, new string[]{ "minerguildmaster", }, "1"),
                            (0x400AB3D5, new string[]{ "bard","bardguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3D6, new string[]{ "warriorguildmaster", }, "1"),
                            (0x400AB3D7, new string[]{ "carpenter","realestatebroker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3D8, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB3D9, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3DA, new string[]{ "innkeeper", }, "1"),
                            (0x400AB3DB, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3DC, new string[]{ "mageguildmaster", }, "1"),
                            (0x400AB3DE, new string[]{ "butcher","farmer", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3DF, new string[]{ "butcher", }, "1"),
                            (0x400AB3E0, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3E1, new string[]{ "herbalist","alchemist","customhairstylist", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB3E2, new string[]{ "baker", }, "1"),
                            (0x400AB3E3, new string[]{ "carpenter","architect","realestatebroker", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB3E4, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3E5, new string[]{ "fisherman", }, "1"),
                            (0x400AB3E6, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3E7, new string[]{ "herbalist","alchemist","customhairstylist", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB3E8, new string[]{ "innkeeper", }, "1"),
                            (0x400AB3E9, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB3EA, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3EB, new string[]{ "innkeeper", }, "1"),
                            (0x400AB3EC, new string[]{ "shipwright","mapmaker", }, "1, 1"),
                            (0x400AB3ED, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB3EE, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB3EF, new string[]{ "tanner", }, "1"),
                            (0x400AB3F1, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3F2, new string[]{ "bardguildmaster", }, "1"),
                            (0x400AB3F3, new string[]{ "jeweler", }, "1"),
                            (0x400AB3F4, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB3F5, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB3F7, new string[]{ "bowyer", }, "1"),
                            (0x400AB3F9, new string[]{ "butcher", }, "1"),
                            (0x400AB3FA, new string[]{ "furtrader", }, "1"),
                            (0x400AB3FB, new string[]{ "fisherman", }, "1"),
                            (0x400AB3FC, new string[]{ "scribe", }, "1"),
                            (0x400AB3FD, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB3FE, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB3FF, new string[]{ "butcher", }, "1"),
                            (0x400AB400, new string[]{ "innkeeper", }, "1"),
                            (0x400AB401, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB402, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB403, new string[]{ "bardguildmaster", }, "1"),
                            (0x400AB404, new string[]{ "tanner","furtrader", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB405, new string[]{ "innkeeper", }, "1"),
                            (0x400AB406, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB407, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB408, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB409, new string[]{ "baker", }, "1"),
                            (0x400AB40A, new string[]{ "baker", }, "1"),
                            (0x400AB40B, new string[]{ "butcher", }, "1"),
                            (0x400AB40D, new string[]{ "warriorguildmaster", }, "1"),
                            (0x400AB40E, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB410, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB411, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB413, new string[]{ "animaltrainer", }, "1"),
                            (0x400AB414, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB415, new string[]{ "animaltrainer", }, "1"),
                            (0x400AB416, new string[]{ "bowyer", }, "1"),
                            (0x400AB417, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB418, new string[]{ "fisherman", }, "1"),
                            (0x400AB419, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB41A, new string[]{ "tinker","tinkerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB41C, new string[]{ "warriorguildmaster", }, "1"),
                            (0x400AB41D, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB41E, new string[]{ "herbalist","alchemist","customhairstylist", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB41F, new string[]{ "innkeeper", }, "1"),
                            (0x400AB420, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB421, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB423, new string[]{ "fisherman", }, "1"),
                            (0x400AB424, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB425, new string[]{ "rangerguildmaster", }, "1"),
                            (0x400AB426, new string[]{ "innkeeper", }, "1"),
                            (0x400AB427, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB428, new string[]{ "fisherman", }, "1"),
                            (0x400AB429, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB42A, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB42B, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB42C, new string[]{ "animaltrainer", }, "1"),
                            (0x400AB42E, new string[]{ "bowyer", }, "1"),
                            (0x400AB42F, new string[]{ "butcher", }, "1"),
                            (0x400AB430, new string[]{ "carpenter","architect","realestatebroker", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB431, new string[]{ "innkeeper", }, "1"),
                            (0x400AB433, new string[]{ "butcher","farmer", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB434, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB435, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB436, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB437, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB438, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB439, new string[]{ "scribe", }, "1"),
                            (0x400AB43A, new string[]{ "jeweler", }, "1"),
                            (0x400AB43B, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB43C, new string[]{ "innkeeper", }, "1"),
                            (0x400AB43D, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB43E, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB43F, new string[]{ "tinkerguildmaster", }, "1"),
                            (0x400AB440, new string[]{ "butcher", }, "1"),
                            (0x400AB441, new string[]{ "fisherman", }, "1"),
                            (0x400AB442, new string[]{ "tanner","furtrader", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB443, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB444, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB445, new string[]{ "animaltrainer", }, "1"),
                            (0x400AB447, new string[]{ "warriorguildmaster", }, "1"),
                            (0x400AB448, new string[]{ "innkeeper", }, "1"),
                            (0x400AB449, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB44A, new string[]{ "baker", }, "1"),
                            (0x400AB44B, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB44C, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB44D, new string[]{ "warriorguildmaster", }, "1"),
                            (0x400AB44E, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB44F, new string[]{ "animaltrainer", }, "1"),
                            (0x400AB450, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB451, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB452, new string[]{ "minerguildmaster", }, "1"),
                            (0x400AB453, new string[]{ "bowyer", }, "1"),
                            (0x400AB454, new string[]{ "jeweler", }, "1"),
                            (0x400AB456, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB457, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB458, new string[]{ "tanner","furtrader", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB459, new string[]{ "baker", }, "1"),
                            (0x400AB45A, new string[]{ "fisherman", }, "1"),
                            (0x400AB45B, new string[]{ "innkeeper", }, "1"),
                            (0x400AB45C, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB45D, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB45E, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB460, new string[]{ "beekeeper", }, "1"),
                            (0x400AB461, new string[]{ "butcher", }, "1"),
                            (0x400AB462, new string[]{ "warriorguildmaster", }, "1"),
                            (0x400AB464, new string[]{ "butcher","farmer", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB465, new string[]{ "fisherguildmaster", }, "1"),
                            (0x400AB466, new string[]{ "butcher","farmer", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB468, new string[]{ "tinker","tinkerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB469, new string[]{ "carpenter","architect","realestatebroker", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB46B, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB46C, new string[]{ "innkeeper", }, "1"),
                            (0x400AB46E, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB470, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB473, new string[]{ "herbalist","alchemist","customhairstylist", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB474, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB477, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400AB479, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB47A, new string[]{ "scribe", }, "1"),
                            (0x400AB47B, new string[]{ "innkeeper", }, "1"),
                            (0x400AB47C, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB47D, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB47E, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB47F, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB480, new string[]{ "innkeeper", }, "1"),
                            (0x400AB481, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB482, new string[]{ "innkeeper", }, "1"),
                            (0x400AB485, new string[]{ "scribe", }, "1"),
                            (0x400AB487, new string[]{ "baker", }, "1"),
                            (0x400AB488, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB489, new string[]{ "herbalist","alchemist","customhairstylist", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB48B, new string[]{ "bowyer", }, "1"),
                            (0x400AB48C, new string[]{ "tanner","furtrader", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB48D, new string[]{ "butcher", }, "1"),
                            (0x400AB48E, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB48F, new string[]{ "innkeeper", }, "1"),
                            (0x400AB490, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB491, new string[]{ "carpenter","architect","realestatebroker", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB492, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB493, new string[]{ "bowyer", }, "1"),
                            (0x400AB494, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB495, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB496, new string[]{ "baker", }, "1"),
                            (0x400AB497, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB498, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB499, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB49A, new string[]{ "innkeeper", }, "1"),
                            (0x400AB49C, new string[]{ "animaltrainer", }, "1"),
                            (0x400AB49D, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB49E, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB4A0, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB4A1, new string[]{ "minter","banker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB4A2, new string[]{ "mageguildmaster","alchemist","mage", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB4A3, new string[]{ "tinkerguildmaster","tinker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB4A4, new string[]{ "jeweler", }, "1"),
                            (0x400AB4A5, new string[]{ "animaltrainer", }, "1"),
                            (0x400AB4A6, new string[]{ "butcher", }, "1"),
                            (0x400AB4A7, new string[]{ "weaponsmith","armorer", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB4A8, new string[]{ "customhairstylist","alchemist","herbalist", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB4A9, new string[]{ "mapmaker","shipwright", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB4AA, new string[]{ "tailorguildmaster","weaver","tailor", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB4AB, new string[]{ "fisherman", }, "1"),
                            (0x400AB4AC, new string[]{ "baker", }, "1"),
                            (0x400AB4AD, new string[]{ "innkeeper", }, "1"),
                            (0x400AB4AE, new string[]{ "healerguildmaster","healer", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB4AF, new string[]{ "realestatebroker","architect","carpenter", }, "1, 1, 1, 0, 0, 0"),
                            (0x400AB4B0, new string[]{ "cobbler","provisioner", }, "1, 1, 0, 0, 0, 0"),
                            (0x400AB4CC, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4CD, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4CE, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4D0, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4D1, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4D2, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4D3, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4D4, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4D5, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4D6, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4D7, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4D8, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4D9, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4DA, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4DB, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4DC, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4DD, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4DE, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4DF, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4E0, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4E1, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4E2, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4E3, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4E5, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4E6, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4E7, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4E8, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4E9, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4EA, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4EB, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4EC, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4ED, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4EE, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4EF, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4F0, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4F1, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4F2, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4F3, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4F4, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4F5, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4F7, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4F8, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4FA, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4FB, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4FC, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB4FD, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB501, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB502, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB503, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB505, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB50B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB50C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB50E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB50F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB510, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB511, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB512, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB514, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB515, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB516, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB517, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB518, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB519, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB51A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB51B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB51C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB51D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB51E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB51F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB520, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB521, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB522, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB523, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB525, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB526, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB527, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB528, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB529, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB52A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB52B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB52C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB52D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB52F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB530, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB531, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB532, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB533, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB534, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB535, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB536, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB537, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB538, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB539, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB53A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB53B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB53C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB53D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB53E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB53F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB540, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB541, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB542, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB543, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB544, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB545, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB546, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB547, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB548, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB54A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB54B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB54C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB54E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB54F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB550, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB552, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB553, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB555, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB556, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB557, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB558, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB559, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB55A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB55B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB55C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB55E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB55F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB560, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB561, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB562, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB563, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB564, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB565, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB566, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB567, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB568, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB569, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB56A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB56B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB56C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB56D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB56E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB56F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB570, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB571, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB572, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB573, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB574, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB575, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB576, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB578, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB57A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB57B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB57D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB57F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB580, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB582, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB583, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB584, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB585, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB586, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB587, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB588, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB589, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB58C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB58D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB58E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB58F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB590, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB591, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB592, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB593, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB594, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB595, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB596, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB598, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB599, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB59A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB59B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB59C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400AB59E, new string[]{ "Jukawarrior", }, "2"),
                            (0x400AB59F, new string[]{ "Jukalord","Jukamage","Jukawarrior", }, "1, 1, 3, 0, 0, 0"),
                            (0x400AB5A1, new string[]{ "Golemcontroller","Golem", }, "4, 4, 0, 0, 0, 0"),
                            (0x400AB5A2, new string[]{ "Jukalord","Jukamage","Jukawarrior", }, "1, 1, 3, 0, 0, 0"),
                            (0x400AB5A3, new string[]{ "Jukalord","Jukamage","Jukawarrior", }, "1, 1, 3, 0, 0, 0"),
                            (0x400AB5A5, new string[]{ "Jukalord","Jukamage","Jukawarrior", }, "1, 1, 3, 0, 0, 0"),
                            (0x400AB5A6, new string[]{ "Brigand","Jukalord","Jukamage","Jukawarrior", }, "4, 1, 1, 3, 0, 0"),
                            (0x400AB5A8, new string[]{ "Golem","Jukalord","Jukamage","Jukawarrior", }, "1, 1, 1, 3, 0, 0"),
                            (0x400AB5A9, new string[]{ "Jukalord","Jukamage","Jukawarrior", }, "1, 1, 3, 0, 0, 0"),
                            (0x400231DE, new string[]{ "EvilWanderingHealer","sheep", }, "1, 2"),
                            (0x4002EE36, new string[]{ "wildhuntwarrior","wildhuntmage", }, "1, 2, 3, 4"),
                            (0x40036BA1, new string[]{ "Shadowwyrm", }, "1, 1, 1, 1"),
                            };

                    foreach (var obj in tupleList)
                    {
                        if (World.FindItem(obj.serial) is Spawner spawner)
                        {
                            if (spawner.ModeMulti || spawner.ModeNeruns)
                            {
                                spawner.ObjectNamesRaw = new ArrayList(obj.NameList);
                                spawner.Counts = obj.counts;
                                patched++;
                            }
                        }
                        else
                            missing++; // only one missing in my test, and I believe it was one of Boz's misconfigured spawners 
                    }
                }
                #endregion Re-add missing mobiles from ModeNeruns and ModeMulti spawners

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV159

        #region PatchV160

        private static int? PatchV160(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation
                int missing = 0;
                #region Re-add missing mobiles from ModeNeruns and ModeMulti spawners
                if (TestCenterShards())
                {
                    (Serial serial, string[] NameList, string counts)[] tupleList = {
                            (0x401745CB, new string[]{ "wyvern","wyvern","wyvern", }, "1"),
                            (0x4005D9F5, new string[]{ "Shadowwyrm", }, "1, 1, 1, 1"),
                            (0x40107283, new string[]{ "Drake","Drake","Drake", }, "1"),
                            (0x40006DD7, new string[]{ "lich","lichlord", }, "2, 2"),
                            (0x4000A7CA, new string[]{ "EvilWanderingHealer","WanderingHealer", }, "1, 1"),
                            (0x4000DCD4, new string[]{ "Bogthing","Bogling", }, "2, 5"),
                            (0x4009AE64, new string[]{ "drake", }, "2"),
                            (0x400A08EF, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08F0, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08F1, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08F2, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08F3, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08F4, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08F5, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08F6, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08F7, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08F8, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08F9, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08FA, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08FB, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08FC, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08FD, new string[]{ "sewerrat:Rat","Bullfrog", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A08FE, new string[]{ "alligator", }, "8"),
                            (0x400A08FF, new string[]{ "alligator", }, "2"),
                            (0x400A0900, new string[]{ "alligator", }, "2"),
                            (0x400A0901, new string[]{ "alligator", }, "2"),
                            (0x400A0902, new string[]{ "alligator", }, "8"),
                            (0x400A0903, new string[]{ "alligator", }, "2"),
                            (0x400A0904, new string[]{ "Harpy", }, "6"),
                            (0x400A0905, new string[]{ "Headlessone", }, "4"),
                            (0x400A0906, new string[]{ "Stoneharpy", }, "2"),
                            (0x400A0907, new string[]{ "Gazer","Gazerlarva", }, "4, 2, 0, 0, 0, 0"),
                            (0x400A0908, new string[]{ "Gazer","Gazerlarva", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A0909, new string[]{ "Corpser", }, "3"),
                            (0x400A090A, new string[]{ "Corpser", }, "3"),
                            (0x400A090B, new string[]{ "Headlessone", }, "2"),
                            (0x400A090C, new string[]{ "Headlessone", }, "2"),
                            (0x400A090D, new string[]{ "Headlessone", }, "2"),
                            (0x400A090E, new string[]{ "Harpy","Stoneharpy", }, "8, 1, 0, 0, 0, 0"),
                            (0x400A090F, new string[]{ "Giantspider","Dreadspider", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A0910, new string[]{ "Slime", }, "5"),
                            (0x400A0911, new string[]{ "Waterelemental", }, "2"),
                            (0x400A0912, new string[]{ "Giantspider","Dreadspider", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A0913, new string[]{ "Giantspider","Dreadspider", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A0914, new string[]{ "Gazer","Gazerlarva","Eldergazer", }, "4, 2, 1, 0, 0, 0"),
                            (0x400A0915, new string[]{ "Corpser", }, "3"),
                            (0x400A0916, new string[]{ "Corpser", }, "3"),
                            (0x400A0917, new string[]{ "Gazer","Gazerlarva", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A0918, new string[]{ "Corpser", }, "2"),
                            (0x400A0919, new string[]{ "Gazer", }, "2"),
                            (0x400A091A, new string[]{ "Skeleton", }, "4"),
                            (0x400A091B, new string[]{ "Skeleton", }, "4"),
                            (0x400A091C, new string[]{ "Skeleton", }, "4"),
                            (0x400A091D, new string[]{ "Skeleton", }, "4"),
                            (0x400A091E, new string[]{ "Mummy","Shade","Skeletalknight","Skeleton:Zombie","Spectre","Wraith", }, "1, 1, 1, 2, 1, 1"),
                            (0x400A091F, new string[]{ "Mummy","Shade","Skeletalknight","Skeleton:Zombie","Spectre","Wraith", }, "1, 1, 1, 2, 1, 1"),
                            (0x400A0920, new string[]{ "Mummy","Shade","Skeletalknight","Skeleton:Zombie","Spectre","Wraith", }, "1, 1, 1, 2, 1, 1"),
                            (0x400A0921, new string[]{ "Boneknight","Skeleton","Zombie","Shade", }, "3, 1, 1, 1, 0, 0"),
                            (0x400A0922, new string[]{ "Boneknight","Skeleton","Zombie","Shade", }, "3, 1, 1, 1, 0, 0"),
                            (0x400A0923, new string[]{ "Lich","Spectre","Skeleton", }, "7, 3, 1, 0, 0, 0"),
                            (0x400A0924, new string[]{ "Mummy","Shade","Skeletalknight","Skeleton:Zombie","Spectre","Wraith", }, "1, 1, 1, 2, 1, 1"),
                            (0x400A0925, new string[]{ "Mummy","Shade","Skeletalknight","Skeleton:Zombie","Spectre","Wraith", }, "1, 1, 1, 3, 1, 1"),
                            (0x400A0926, new string[]{ "Dragon","Drake", }, "1, 2, 0, 0, 0, 0"),
                            (0x400A0927, new string[]{ "Mummy","Skeleton","Zombie", }, "1, 3, 1, 0, 0, 0"),
                            (0x400A0928, new string[]{ "Waterelemental", }, "2"),
                            (0x400A0929, new string[]{ "Skeleton", }, "2"),
                            (0x400A092A, new string[]{ "Mummy","Skeleton","Zombie", }, "1, 3, 1, 0, 0, 0"),
                            (0x400A092B, new string[]{ "Skeleton","Zombie", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A092C, new string[]{ "Skeleton","Zombie", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A092D, new string[]{ "Skeleton","Zombie", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A092E, new string[]{ "Skeleton","Zombie", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A092F, new string[]{ "Skeleton","Spectre", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A0930, new string[]{ "Ghoul","Shade","Skeleton","Zombie", }, "3, 1, 1, 1, 0, 0"),
                            (0x400A0931, new string[]{ "Ghoul","Shade","Skeleton","Zombie", }, "3, 1, 1, 1, 0, 0"),
                            (0x400A0932, new string[]{ "Ghoul","Skeleton","Wraith","Zombie", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0933, new string[]{ "Ghoul","Skeleton","Wraith","Zombie", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0934, new string[]{ "Ghoul","Skeleton","Wraith","Zombie", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0935, new string[]{ "Ghoul","Wraith", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A0936, new string[]{ "Ghoul","Mummy","Shade","Wraith","Zombie", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0937, new string[]{ "Boneknight","Skeletalknight", }, "4, 4, 0, 0, 0, 0"),
                            (0x400A0938, new string[]{ "Ghoul","Shade","Skeleton","Wraith", }, "1, 1, 2, 1, 0, 0"),
                            (0x400A0939, new string[]{ "Bonemagi", }, "3"),
                            (0x400A093A, new string[]{ "Ghoul","Shade","Skeleton","Wraith","Zombie", }, "1, 1, 2, 1, 1, 0"),
                            (0x400A093B, new string[]{ "Ghoul","Shade","Spectre","Wraith", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A093C, new string[]{ "Lich", }, "2"),
                            (0x400A093D, new string[]{ "Ghoul","Shade","Spectre","Wraith", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A093E, new string[]{ "Ghoul","Lich","Spectre","Wraith", }, "1, 3, 1, 1, 0, 0"),
                            (0x400A093F, new string[]{ "Ghoul","Poisonelemental", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0940, new string[]{ "Ghoul","Poisonelemental", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0941, new string[]{ "Ghoul","Shade","Spectre","Wraith", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0942, new string[]{ "Lich", }, "4"),
                            (0x400A0943, new string[]{ "Ghoul","Lich", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A0944, new string[]{ "Fireelemental", }, "2"),
                            (0x400A0945, new string[]{ "Lich", }, "2"),
                            (0x400A0946, new string[]{ "Lizardman", }, "5"),
                            (0x400A0947, new string[]{ "Lizardman", }, "5"),
                            (0x400A0948, new string[]{ "Lizardman", }, "5"),
                            (0x400A0949, new string[]{ "Lizardman", }, "5"),
                            (0x400A094A, new string[]{ "Lizardman", }, "5"),
                            (0x400A094B, new string[]{ "Lizardman", }, "5"),
                            (0x400A094C, new string[]{ "Lizardman", }, "5"),
                            (0x400A094D, new string[]{ "Ettin", }, "4"),
                            (0x400A094E, new string[]{ "Ettin", }, "4"),
                            (0x400A094F, new string[]{ "Earthelemental","Ettin", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0950, new string[]{ "Earthelemental","Ettin", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0951, new string[]{ "Earthelemental","Ettin", }, "4, 6, 0, 0, 0, 0"),
                            (0x400A0952, new string[]{ "Ettin", }, "4"),
                            (0x400A0953, new string[]{ "Ettin", }, "4"),
                            (0x400A0954, new string[]{ "Earthelemental", }, "6"),
                            (0x400A0955, new string[]{ "Troll", }, "3"),
                            (0x400A0956, new string[]{ "Troll", }, "3"),
                            (0x400A0957, new string[]{ "Troll", }, "3"),
                            (0x400A0958, new string[]{ "Troll", }, "3"),
                            (0x400A0959, new string[]{ "Ogre", }, "3"),
                            (0x400A095A, new string[]{ "Ogre", }, "3"),
                            (0x400A095B, new string[]{ "Ogrelord", }, "5"),
                            (0x400A095C, new string[]{ "Ogre", }, "3"),
                            (0x400A095D, new string[]{ "Ogre", }, "3"),
                            (0x400A095E, new string[]{ "Ettin", }, "4"),
                            (0x400A095F, new string[]{ "Ettin", }, "4"),
                            (0x400A0960, new string[]{ "Cyclops", }, "3"),
                            (0x400A0961, new string[]{ "Ettin", }, "4"),
                            (0x400A0962, new string[]{ "Ettin", }, "4"),
                            (0x400A0963, new string[]{ "Dragon","Drake","Wyvern", }, "1, 2, 1, 0, 0, 0"),
                            (0x400A0964, new string[]{ "Dragon","Drake", }, "1, 2, 0, 0, 0, 0"),
                            (0x400A0965, new string[]{ "Dragon","Drake","Wyvern", }, "1, 2, 1, 0, 0, 0"),
                            (0x400A0966, new string[]{ "Dragon","Drake","Wyvern", }, "1, 2, 1, 0, 0, 0"),
                            (0x400A0967, new string[]{ "Dragon","Drake","Wyvern", }, "1, 2, 1, 0, 0, 0"),
                            (0x400A0968, new string[]{ "Giantserpent", }, "6"),
                            (0x400A0969, new string[]{ "Wyvern","Dragon","Drake", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A096A, new string[]{ "Waterelemental", }, "4"),
                            (0x400A096B, new string[]{ "Giantserpent", }, "6"),
                            (0x400A096C, new string[]{ "Dragon","Drake","Wyvern", }, "1, 2, 1, 0, 0, 0"),
                            (0x400A096D, new string[]{ "Dragon","Drake","Wyvern", }, "1, 2, 1, 0, 0, 0"),
                            (0x400A096E, new string[]{ "Drake","Wyvern", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A096F, new string[]{ "Waterelemental", }, "4"),
                            (0x400A0970, new string[]{ "Waterelemental", }, "4"),
                            (0x400A0971, new string[]{ "Drake","Shadowwyrm","Wyvern", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0972, new string[]{ "Daemon","Evilmage", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A0973, new string[]{ "Drake","Shadowwyrm","Wyvern", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0974, new string[]{ "Fireelemental", }, "1"),
                            (0x400A0975, new string[]{ "Wyvern:Wyvern:Giantserpent", }, "3"),
                            (0x400A0976, new string[]{ "Ancientwyrm", }, "1"),
                            (0x400A0977, new string[]{ "Slime","Giantrat","Giantserpent", }, "1, 2, 1, 0, 0, 0"),
                            (0x400A0978, new string[]{ "Slime", }, "1"),
                            (0x400A0979, new string[]{ "Evilmagelord", }, "2"),
                            (0x400A097A, new string[]{ "Giantrat","Lavasnake","Evilmage", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A097B, new string[]{ "Lavalizard", }, "1"),
                            (0x400A097C, new string[]{ "Fireelemental", }, "3"),
                            (0x400A097D, new string[]{ "Fireelemental","Lavasnake","Hellcat","Hellhound", }, "3, 1, 1, 1, 0, 0"),
                            (0x400A097E, new string[]{ "Efreet","Lavaserpent","Hellhound","Fireelemental", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A097F, new string[]{ "Lich","Bonemagi:Skeletalmage","Hellcat","Hellhound","Fireelemental","Lavalizard", }, "4, 6, 1, 3, 2, 1"),
                            (0x400A0980, new string[]{ "Lavasnake", }, "1"),
                            (0x400A0981, new string[]{ "Lavalizard", }, "1"),
                            (0x400A0982, new string[]{ "Hellcat", }, "1"),
                            (0x400A0983, new string[]{ "Fireelemental", }, "1"),
                            (0x400A0984, new string[]{ "Lavasnake", }, "1"),
                            (0x400A0985, new string[]{ "Lavasnake", }, "1"),
                            (0x400A0986, new string[]{ "Fireelemental", }, "1"),
                            (0x400A0987, new string[]{ "Fireelemental","Efreet","Lavaserpent", }, "4, 1, 1, 0, 0, 0"),
                            (0x400A0988, new string[]{ "Hellcat","Fireelemental", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0989, new string[]{ "Fireelemental", }, "1"),
                            (0x400A098A, new string[]{ "Fireelemental", }, "2"),
                            (0x400A098B, new string[]{ "Hellhound", }, "1"),
                            (0x400A098C, new string[]{ "Efreet","Lavalizard", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A098D, new string[]{ "Fireelemental","Lavaserpent", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A098E, new string[]{ "Lavalizard","Fireelemental", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A098F, new string[]{ "Lich","Lichlord", }, "3, 1, 0, 0, 0, 0"),
                            (0x400A0990, new string[]{ "Hellhound", }, "6"),
                            (0x400A0991, new string[]{ "Daemon", }, "2"),
                            (0x400A0992, new string[]{ "Hellhound","Hellcat", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0993, new string[]{ "Evilmage","Hellhound","Lavasnake","Hellcat", }, "1, 5, 1, 1, 0, 0"),
                            (0x400A0994, new string[]{ "Evilmage", }, "1"),
                            (0x400A0995, new string[]{ "Evilmagelord", }, "1"),
                            (0x400A0996, new string[]{ "Evilmagelord", }, "1"),
                            (0x400A0997, new string[]{ "Evilmage", }, "1"),
                            (0x400A0998, new string[]{ "Evilmage", }, "1"),
                            (0x400A0999, new string[]{ "Hellhound", }, "1"),
                            (0x400A099A, new string[]{ "Evilmage","Evilmagelord", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A099B, new string[]{ "Evilmage", }, "1"),
                            (0x400A099C, new string[]{ "Evilmage", }, "1"),
                            (0x400A099D, new string[]{ "Evilmage", }, "1"),
                            (0x400A099E, new string[]{ "Evilmage", }, "1"),
                            (0x400A099F, new string[]{ "Evilmage", }, "1"),
                            (0x400A09A0, new string[]{ "Evilmagelord", }, "1"),
                            (0x400A09A1, new string[]{ "Spectre:Wraith","Skeleton","Zombie", }, "2, 3, 4, 0, 0, 0"),
                            (0x400A09A2, new string[]{ "Spectre:Wraith:Shade","Skeleton","Zombie", }, "2, 3, 4, 0, 0, 0"),
                            (0x400A09A3, new string[]{ "Spectre:Shade:Wraith","Lich","Skeleton","Zombie", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A09A4, new string[]{ "Spectre:Shade:Wraith","Skeleton","Zombie", }, "1, 1, 2, 0, 0, 0"),
                            (0x400A09A5, new string[]{ "Lich", }, "1"),
                            (0x400A09A6, new string[]{ "Spectre:Wraith","Skeleton","Zombie", }, "2, 3, 4, 0, 0, 0"),
                            (0x400A09A7, new string[]{ "Skeleton", }, "10"),
                            (0x400A09A8, new string[]{ "zombie:skeleton", }, "4"),
                            (0x400A09A9, new string[]{ "Imp","Hellhound", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A09AA, new string[]{ "Imp","Hellhound", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A09AB, new string[]{ "Imp","Hellhound", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A09AC, new string[]{ "Imp","Hellhound", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A09AD, new string[]{ "Firegargoyle", }, "2"),
                            (0x400A09AE, new string[]{ "Imp","Hellhound", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A09AF, new string[]{ "Gargoyle","Hellhound", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A09B0, new string[]{ "Imp","Hellhound", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A09B1, new string[]{ "Gargoyle", }, "2"),
                            (0x400A09B2, new string[]{ "Imp","Hellhound", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A09B3, new string[]{ "Imp","Hellhound", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A09B4, new string[]{ "Daemon", }, "2"),
                            (0x400A09B5, new string[]{ "Imp","Hellhound", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A09B6, new string[]{ "Gargoyle", }, "2"),
                            (0x400A09B7, new string[]{ "Gargoyle", }, "2"),
                            (0x400A09B8, new string[]{ "Imp","Hellhound", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A09B9, new string[]{ "Gargoyle", }, "4"),
                            (0x400A09BA, new string[]{ "Gazer","Gazerlarva", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A09BB, new string[]{ "Imp","Hellhound","Gargoyle", }, "1, 1, 2, 0, 0, 0"),
                            (0x400A09BC, new string[]{ "Eldergazer","Gazer","Gazerlarva", }, "1, 3, 2, 0, 0, 0"),
                            (0x400A09BD, new string[]{ "Balron","Gargoyle","Gazer", }, "1, 2, 2, 0, 0, 0"),
                            (0x400A09BE, new string[]{ "Daemon","Gargoyle","Gazer", }, "2, 2, 2, 0, 0, 0"),
                            (0x400A09BF, new string[]{ "Gargoyle", }, "2"),
                            (0x400A09C0, new string[]{ "Daemon","Stonegargoyle", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A09C1, new string[]{ "Daemon","Gazer","Gazerlarva", }, "2, 3, 2, 0, 0, 0"),
                            (0x400A09C2, new string[]{ "Daemon","Hellhound","Imp", }, "2, 1, 1, 0, 0, 0"),
                            (0x400A09C3, new string[]{ "Gargoyle", }, "2"),
                            (0x400A09C4, new string[]{ "Gargoyle", }, "2"),
                            (0x400A09C5, new string[]{ "Gargoyle", }, "2"),
                            (0x400A09C6, new string[]{ "Firegargoyle", }, "2"),
                            (0x400A09C7, new string[]{ "Gargoyle", }, "2"),
                            (0x400A09C9, new string[]{ "Balron", }, "1"),
                            (0x400A09CB, new string[]{ "Gazer","Imp","Stonegargoyle", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A09CC, new string[]{ "Arcticogrelord", }, "1"),
                            (0x400A09CD, new string[]{ "Iceserpent","Snowelemental","Ratman","Frosttroll","Frostspider", }, "2, 1, 1, 1, 1, 0"),
                            (0x400A09CE, new string[]{ "Frostspider","Frostooze","Icesnake", }, "1, 2, 1, 0, 0, 0"),
                            (0x400A09CF, new string[]{ "Ratmanmage", }, "1"),
                            (0x400A09D0, new string[]{ "Arcticogrelord", }, "1"),
                            (0x400A09D1, new string[]{ "Whitewyrm", }, "1"),
                            (0x400A09D2, new string[]{ "Frostooze","Frostspider","Iceelemental:Snowelemental","Icesnake","Frosttroll", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A09D4, new string[]{ "Frostooze","Frostspider","Iceelemental:Snowelemental","Icesnake","Frosttroll", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A09D5, new string[]{ "Iceserpent","Ratman","Snowelemental:Iceelemental","Frosttroll", }, "3, 1, 4, 2, 0, 0"),
                            (0x400A09D6, new string[]{ "Iceserpent","Snowelemental:Iceelemental","Frosttroll","Ratman", }, "3, 4, 1, 1, 0, 0"),
                            (0x400A09D7, new string[]{ "Iceserpent","Snowelemental:Iceelemental", }, "9, 9, 0, 0, 0, 0"),
                            (0x400A09D8, new string[]{ "Ratman","Ratmanarcher","Ratmanmage", }, "12, 7, 5, 0, 0, 0"),
                            (0x400A09D9, new string[]{ "Icefiend", }, "1"),
                            (0x400A09DA, new string[]{ "Icefiend", }, "1"),
                            (0x400A09DB, new string[]{ "Swampdragon", }, "7"),
                            (0x400A09DC, new string[]{ "Imp","Scorpion","Desertostard","Orc","Orcishlord","Stoneharpy", }, "15, 15, 6, 9, 6, 3"),
                            (0x400A09DD, new string[]{ "Stonegargoyle","Wyvern","Mummy","Ettin", }, "3, 6, 4, 6, 0, 0"),
                            (0x400A09DE, new string[]{ "Imp","Scorpion","Desertostard","Orc","Orcishlord","Stoneharpy", }, "10, 10, 4, 6, 4, 3"),
                            (0x400A09DF, new string[]{ "Stonegargoyle","Wyvern","Mummy","Ettin", }, "3, 4, 2, 4, 0, 0"),
                            (0x400A09E0, new string[]{ "Gazer","Reaper","Scorpion","Giantserpent","Troll", }, "3, 2, 3, 2, 2, 0"),
                            (0x400A09E1, new string[]{ "Corpser","Giantspider","Troll","Harpy","Ettin","Gazer", }, "3, 3, 3, 6, 3, 3"),
                            (0x400A09E2, new string[]{ "Cyclops","Titan", }, "5, 3, 0, 0, 0, 0"),
                            (0x400A09E3, new string[]{ "Imp","Scorpion","Desertostard","Orc","Orcishlord","Wyvern", }, "3, 3, 1, 3, 2, 1"),
                            (0x400A09E4, new string[]{ "Ettin", }, "2"),
                            (0x400A09E5, new string[]{ "Ophidianarchmage","Ophidianmage","Ophidianmatriarch","Ophidianknight","Ophidianwarrior", }, "3, 6, 3, 4, 10, 0"),
                            (0x400A09E6, new string[]{ "Wyvern","Drake","Imp","Mongbat", }, "2, 4, 1, 2, 0, 0"),
                            (0x400A09E7, new string[]{ "Imp","Scorpion","Desertostard","Orc","Orcishlord","Stoneharpy", }, "8, 8, 3, 5, 3, 3"),
                            (0x400A09E8, new string[]{ "Stonegargoyle","Wyvern","Mummy","Ettin", }, "3, 3, 3, 5, 0, 0"),
                            (0x400A09E9, new string[]{ "Hellhound","Lavalizard","Fireelemental","Efreet","Hellcat","Lavaserpent", }, "8, 8, 10, 4, 6, 8"),
                            (0x400A09EA, new string[]{ "Lavasnake", }, "15"),
                            (0x400A09EB, new string[]{ "Wyvern", }, "6"),
                            (0x400A09EC, new string[]{ "Iceserpent","Icesnake","Frosttroll","Snowelemental","Whitewolf","Walrus", }, "4, 8, 4, 4, 5, 5"),
                            (0x400A09ED, new string[]{ "Polarbear", }, "5"),
                            (0x400A09EE, new string[]{ "Iceserpent","Icesnake","Frosttroll","Snowelemental","Whitewolf","Walrus", }, "4, 7, 4, 4, 5, 5"),
                            (0x400A09EF, new string[]{ "Polarbear", }, "5"),
                            (0x400A09F0, new string[]{ "Iceserpent","Icesnake","Frosttroll","Snowelemental","Whitewolf","Walrus", }, "4, 10, 4, 4, 3, 3"),
                            (0x400A09F1, new string[]{ "Polarbear", }, "3"),
                            (0x400A09F2, new string[]{ "Iceserpent","Icesnake","Snowelemental","Whitewolf","Walrus","Polarbear", }, "6, 10, 3, 3, 3, 3"),
                            (0x400A09F3, new string[]{ "Iceserpent","Icesnake","Frosttroll","Snowelemental","Whitewolf","Walrus", }, "6, 4, 4, 4, 4, 4"),
                            (0x400A09F4, new string[]{ "Polarbear", }, "4"),
                            (0x400A09F5, new string[]{ "Iceserpent","Icesnake","Frosttroll","Snowelemental","Whitewolf","Walrus", }, "6, 3, 3, 3, 4, 4"),
                            (0x400A09F6, new string[]{ "Polarbear", }, "4"),
                            (0x400A09F7, new string[]{ "Gorilla","Panther","Giantserpent","Silverserpent","Mongbat", }, "10, 10, 10, 2, 10, 0"),
                            (0x400A09F8, new string[]{ "Gianttoad","Swampdragon","Giantrat","Alligator","Sewerrat","Plaguebeast", }, "8, 5, 12, 12, 11, 2"),
                            (0x400A09F9, new string[]{ "Bullfrog","Swamptentacle","Bogling","Bogthing", }, "15, 5, 13, 3, 0, 0"),
                            (0x400A09FA, new string[]{ "Boneknight","Skeletalknight","Bonemagi","Skeleton","Wraith","Ghoul", }, "3, 3, 3, 6, 4, 4"),
                            (0x400A09FB, new string[]{ "Savagerider","Savage","Savageshaman", }, "3, 9, 3, 0, 0, 0"),
                            (0x400A09FC, new string[]{ "Horse","Orc","Orcishlord","Greathart","Hind","Ridablellama", }, "4, 8, 4, 4, 16, 4"),
                            (0x400A09FD, new string[]{ "Horse","Orc","Orcishlord","Greathart","Hind","Ridablellama", }, "4, 8, 4, 4, 16, 4"),
                            (0x400A09FE, new string[]{ "Horse","Orc","Orcishlord","Greathart","Hind","Ridablellama", }, "4, 8, 4, 4, 16, 4"),
                            (0x400A09FF, new string[]{ "Horse","Orc","Orcishlord","Greathart","Hind","Ridablellama", }, "4, 8, 4, 4, 16, 4"),
                            (0x400A0A00, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "6, 4, 4, 2, 2, 2"),
                            (0x400A0A01, new string[]{ "Hind","Llama","Timberwolf", }, "2, 2, 2, 0, 0, 0"),
                            (0x400A0A02, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "6, 4, 4, 2, 2, 2"),
                            (0x400A0A03, new string[]{ "Hind","Llama","Timberwolf", }, "2, 2, 2, 0, 0, 0"),
                            (0x400A0A04, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "6, 4, 4, 2, 2, 2"),
                            (0x400A0A05, new string[]{ "Hind","Llama","Timberwolf", }, "2, 2, 2, 0, 0, 0"),
                            (0x400A0A06, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "6, 4, 4, 2, 2, 2"),
                            (0x400A0A07, new string[]{ "Hind","Llama","Timberwolf", }, "2, 2, 2, 0, 0, 0"),
                            (0x400A0A08, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A09, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A0A, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A0B, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A0C, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A0D, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A0E, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A0F, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A10, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A11, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A12, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A13, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A14, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A15, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A16, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A17, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A18, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A19, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A1A, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A1B, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A1C, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A1D, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A1E, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A1F, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A20, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A21, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A22, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A23, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A24, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A25, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A26, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A27, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A28, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A29, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A2A, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A2B, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A2C, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A2D, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A2E, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A2F, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A30, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A31, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A32, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A33, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A34, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A35, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A36, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A37, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A38, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A39, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A3A, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A3B, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A3C, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A3D, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A3E, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A3F, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A40, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A41, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A42, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A43, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A45, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A46, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A47, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A48, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A49, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A4A, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A4B, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A4C, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A4D, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A4E, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A4F, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A50, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A51, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A52, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A53, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A54, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A55, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A56, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A57, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A58, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A59, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A5A, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A5B, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A5C, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A5D, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A5E, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A5F, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A60, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A61, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A63, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A64, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A65, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A66, new string[]{ "Brownbear:Grizzlybear:Blackbear","Horse:Ridablellama","Rabbit:JackRabbit","Bird","Cougar","Greathart", }, "3, 2, 2, 1, 1, 1"),
                            (0x400A0A67, new string[]{ "Hind","Llama","Timberwolf","Corpser","Ettin","Forestostard", }, "1, 1, 1, 1, 1, 2"),
                            (0x400A0A68, new string[]{ "Gazer","Giantspider","Harpy","Headlessone","Lizardman","Mongbat", }, "1, 1, 1, 1, 3, 1"),
                            (0x400A0A69, new string[]{ "Rat","Ratman","Reaper","Troll", }, "1, 1, 1, 2, 0, 0"),
                            (0x400A0A6A, new string[]{ "Forestostard","Ridablellama", }, "3, 3, 0, 0, 0, 0"),
                            (0x400A0A6B, new string[]{ "Frenziedostard", }, "8"),
                            (0x400A0A6C, new string[]{ "Imp","Mongbat", }, "2, 5, 0, 0, 0, 0"),
                            (0x400A0A6D, new string[]{ "Imp","Harpy","Headlessone","Scorpion","Stoneharpy","Wyvern", }, "1, 1, 1, 1, 1, 1"),
                            (0x400A0A6E, new string[]{ "Rabbit:JackRabbit","Bird","Rat","Hind","Llama","Horse", }, "20, 15, 5, 4, 3, 3"),
                            (0x400A0A6F, new string[]{ "Mongbat","Greathart", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0A70, new string[]{ "Rabbit:JackRabbit","Bird","Rat","Hind","Llama","Horse", }, "20, 15, 5, 4, 3, 3"),
                            (0x400A0A71, new string[]{ "Mongbat","Greathart", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0A72, new string[]{ "Rabbit:JackRabbit","Bird","Rat","Hind","Llama","Horse", }, "20, 15, 5, 4, 3, 3"),
                            (0x400A0A73, new string[]{ "Mongbat","Greathart", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0A74, new string[]{ "Rabbit:JackRabbit","Bird","Rat","Hind","Llama","Horse", }, "20, 15, 5, 4, 3, 3"),
                            (0x400A0A75, new string[]{ "Mongbat","Greathart", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0A76, new string[]{ "Rabbit:JackRabbit","Bird","Rat","Hind","Llama","Horse", }, "20, 15, 5, 4, 3, 3"),
                            (0x400A0A77, new string[]{ "Mongbat","Greathart", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0A78, new string[]{ "Bull","Greathart","Cow", }, "10, 6, 6, 0, 0, 0"),
                            (0x400A0A79, new string[]{ "Bull","Greathart","Cow", }, "2, 2, 2, 0, 0, 0"),
                            (0x400A0A7A, new string[]{ "Gorilla","Panther","Giantserpent","Silverserpent","Mongbat", }, "6, 6, 6, 1, 6, 0"),
                            (0x400A0A7B, new string[]{ "Gorilla","Panther","Giantserpent","Mongbat","Silverserpent", }, "5, 5, 5, 5, 1, 0"),
                            (0x400A0A7C, new string[]{ "Gorilla","Panther","Giantserpent","Mongbat","Silverserpent", }, "5, 5, 5, 5, 1, 0"),
                            (0x400A0A7D, new string[]{ "Terathanmatriarch","Terathanavenger","Terathandrone","Terathanwarrior", }, "2, 4, 10, 10, 0, 0"),
                            (0x400A0A7E, new string[]{ "Ophidianmatriarch:Ophidianarchmage","Ophidianmage","Ophidianknight:Ophidianwarrior", }, "1, 1, 2, 0, 0, 0"),
                            (0x400A0A7F, new string[]{ "Ophidianmatriarch:Ophidianarchmage","Ophidianmage","Ophidianknight:Ophidianwarrior", }, "1, 1, 2, 0, 0, 0"),
                            (0x400A0A80, new string[]{ "Ophidianmatriarch:Ophidianarchmage","Ophidianmage","Ophidianknight:Ophidianwarrior", }, "1, 1, 2, 0, 0, 0"),
                            (0x400A0A81, new string[]{ "Ophidianmatriarch:Ophidianarchmage","Ophidianmage","Ophidianknight:Ophidianwarrior", }, "1, 1, 2, 0, 0, 0"),
                            (0x400A0A82, new string[]{ "Ophidianmatriarch:Ophidianarchmage","Ophidianmage","Ophidianknight:Ophidianwarrior", }, "1, 1, 2, 0, 0, 0"),
                            (0x400A0A83, new string[]{ "Orc","Orccaptain","Orcishlord", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0A84, new string[]{ "Orc", }, "5"),
                            (0x400A0A85, new string[]{ "Direwolf", }, "2"),
                            (0x400A0A86, new string[]{ "Orcishlord", }, "3"),
                            (0x400A0A87, new string[]{ "Orccaptain", }, "3"),
                            (0x400A0A88, new string[]{ "Orc", }, "3"),
                            (0x400A0A89, new string[]{ "Noble", }, "1"),
                            (0x400A0A8A, new string[]{ "Orcishlord","Orc", }, "1, 3, 0, 0, 0, 0"),
                            (0x400A0A8B, new string[]{ "Giantrat", }, "3"),
                            (0x400A0A8C, new string[]{ "Direwolf", }, "3"),
                            (0x400A0A8D, new string[]{ "Orccaptain","Orcishlord","Orcishmage","Orcbomber", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0A8E, new string[]{ "Orcbomber","Orccaptain","Orc", }, "1, 1, 2, 0, 0, 0"),
                            (0x400A0A8F, new string[]{ "Orcishlord","Orcishmage","Orcbomber","Orc", }, "1, 2, 1, 2, 0, 0"),
                            (0x400A0A90, new string[]{ "Orc","Orcishmage","Orcishlord", }, "3, 1, 2, 0, 0, 0"),
                            (0x400A0A91, new string[]{ "Corpser", }, "2"),
                            (0x400A0A92, new string[]{ "Orc", }, "3"),
                            (0x400A0A93, new string[]{ "Orcbrute","Orc", }, "1, 4, 0, 0, 0, 0"),
                            (0x400A0A94, new string[]{ "Orcbomber","Orc", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A0A95, new string[]{ "Orcbomber","Earthelemental", }, "1, 4, 0, 0, 0, 0"),
                            (0x400A0A96, new string[]{ "hordeminion", }, "17"),
                            (0x400A0A97, new string[]{ "mongbat", }, "17"),
                            (0x400A0A98, new string[]{ "lizardman:ratman", }, "8"),
                            (0x400A0A99, new string[]{ "ettin:giantspider","wisp","mongbat:orc:troll", }, "2, 1, 6, 0, 0, 0"),
                            (0x400A0A9A, new string[]{ "ettin:giantspider","wisp","mongbat:orc:troll", }, "2, 1, 6, 0, 0, 0"),
                            (0x400A0A9B, new string[]{ "ettin:giantspider","wisp","mongbat:orc:troll", }, "2, 1, 6, 0, 0, 0"),
                            (0x400A0A9C, new string[]{ "ettin:giantspider","wisp","mongbat:orc:troll", }, "2, 1, 6, 0, 0, 0"),
                            (0x400A0A9D, new string[]{ "ettin:giantspider","wisp","mongbat:orc:troll", }, "2, 1, 6, 0, 0, 0"),
                            (0x400A0A9E, new string[]{ "ettin:giantspider","wisp","mongbat:orc:troll", }, "2, 1, 6, 0, 0, 0"),
                            (0x400A0A9F, new string[]{ "ettin:harpy:ogre:troll", }, "8"),
                            (0x400A0AA0, new string[]{ "ettin:harpy:ogre:troll", }, "8"),
                            (0x400A0AA1, new string[]{ "ettin:harpy:ogre:troll", }, "8"),
                            (0x400A0AA2, new string[]{ "ettin:harpy:ogre:troll", }, "8"),
                            (0x400A0AA3, new string[]{ "ettin:harpy:ogre:troll", }, "8"),
                            (0x400A0AA4, new string[]{ "ettin:harpy:ogre:troll", }, "8"),
                            (0x400A0AA5, new string[]{ "ettin:harpy:ogre:troll", }, "8"),
                            (0x400A0AA6, new string[]{ "ettin:ogre", }, "6"),
                            (0x400A0AA7, new string[]{ "orcishmage", }, "5"),
                            (0x400A0AA8, new string[]{ "orc", }, "10"),
                            (0x400A0AA9, new string[]{ "orc", }, "10"),
                            (0x400A0AAA, new string[]{ "orc", }, "10"),
                            (0x400A0AAB, new string[]{ "orc", }, "10"),
                            (0x400A0AAC, new string[]{ "orc", }, "10"),
                            (0x400A0AAD, new string[]{ "orc", }, "10"),
                            (0x400A0AAE, new string[]{ "brigand:harpy:lizardman:orcishmage:orc:ratman:spectre:troll", }, "3"),
                            (0x400A0AAF, new string[]{ "brigand:harpy:lizardman:orcishmage:orc:ratman:spectre:troll", }, "7"),
                            (0x400A0AB0, new string[]{ "brigand:harpy:lizardman:orcishmage:orc:ratman:spectre:troll", }, "3"),
                            (0x400A0AB1, new string[]{ "brigand:harpy:lizardman:orcishmage:orc:ratman:spectre:troll", }, "3"),
                            (0x400A0AB2, new string[]{ "brigand:harpy:lizardman:orcishmage:orc:ratman:spectre:troll", }, "6"),
                            (0x400A0AB3, new string[]{ "brigand:harpy:lizardman:orcishmage:orc:ratman:spectre:troll", }, "7"),
                            (0x400A0AB4, new string[]{ "airelemental:ettin:ogre:waterelemental","orc:troll:lizardman", }, "5, 5, 0, 0, 0, 0"),
                            (0x400A0AB5, new string[]{ "airelemental:ettin:ogre:waterelemental","orc:troll:lizardman", }, "5, 5, 0, 0, 0, 0"),
                            (0x400A0AB6, new string[]{ "airelemental:ettin:ogre:waterelemental","orc:troll:lizardman", }, "5, 5, 0, 0, 0, 0"),
                            (0x400A0AB7, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0AB8, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0AB9, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0ABA, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0ABB, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0ABC, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0ABD, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0ABE, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0ABF, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0AC0, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0AC1, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0AC2, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0AC3, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0AC4, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0AC5, new string[]{ "airelemental:gargoyle:gazer", }, "3"),
                            (0x400A0AC6, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400A0AC7, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400A0AC8, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400A0AC9, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400A0ACA, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400A0ACB, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400A0ACC, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400A0ACD, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400A0ACE, new string[]{ "airelemental","ettin:harpy:orc", }, "1, 8, 0, 0, 0, 0"),
                            (0x400A0ACF, new string[]{ "earthelemental:ratman", }, "4"),
                            (0x400A0AD0, new string[]{ "earthelemental:ratman", }, "4"),
                            (0x400A0AD1, new string[]{ "earthelemental:ratman", }, "4"),
                            (0x400A0AD2, new string[]{ "ghoul:lich:lichlord","skeleton:zombie", }, "3, 5, 0, 0, 0, 0"),
                            (0x400A0AD3, new string[]{ "ghoul:lich:lichlord","skeleton:zombie", }, "3, 5, 0, 0, 0, 0"),
                            (0x400A0AD4, new string[]{ "gargoyle:gazer", }, "4"),
                            (0x400A0AD5, new string[]{ "gargoyle:gazer", }, "4"),
                            (0x400A0AD6, new string[]{ "gargoyle:gazer", }, "4"),
                            (0x400A0AD7, new string[]{ "gargoyle:gazer", }, "4"),
                            (0x400A0AD8, new string[]{ "gargoyle:gazer", }, "4"),
                            (0x400A0AD9, new string[]{ "troll", }, "10"),
                            (0x400A0ADA, new string[]{ "alligator:lizardman:silverserpent:snake","wisp", }, "8, 2, 0, 0, 0, 0"),
                            (0x400A0ADB, new string[]{ "alligator:lizardman:silverserpent:snake","wisp", }, "8, 2, 0, 0, 0, 0"),
                            (0x400A0ADC, new string[]{ "alligator:lizardman:silverserpent:snake","wisp", }, "8, 2, 0, 0, 0, 0"),
                            (0x400A0ADD, new string[]{ "alligator:lizardman:silverserpent:snake","wisp", }, "8, 2, 0, 0, 0, 0"),
                            (0x400A0ADE, new string[]{ "alligator:lizardman:silverserpent:snake","wisp", }, "8, 2, 0, 0, 0, 0"),
                            (0x400A0ADF, new string[]{ "alligator:lizardman:silverserpent:snake","wisp", }, "8, 2, 0, 0, 0, 0"),
                            (0x400A0AE0, new string[]{ "airelemental:gargoyle", }, "2"),
                            (0x400A0AE1, new string[]{ "airelemental:gargoyle", }, "2"),
                            (0x400A0AE2, new string[]{ "airelemental:gargoyle", }, "2"),
                            (0x400A0AE3, new string[]{ "airelemental:gargoyle", }, "2"),
                            (0x400A0AE4, new string[]{ "lizardman:ratman", }, "8"),
                            (0x400A0AE5, new string[]{ "lizardman:ratman", }, "8"),
                            (0x400A0AE6, new string[]{ "lizardman:ratman", }, "8"),
                            (0x400A0AE7, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0AE8, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0AE9, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0AEA, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0AEB, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0AEC, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0AED, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0AEE, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0AEF, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0AF0, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0AF1, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0AF2, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400A0AF3, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400A0AF4, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400A0AF5, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400A0AF6, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400A0AF7, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400A0AF8, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400A0AF9, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400A0AFA, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400A0AFB, new string[]{ "ettin:lizardman:ogre:orc:troll", }, "5"),
                            (0x400A0AFC, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0AFD, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0AFE, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0AFF, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0B00, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0B01, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0B02, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0B03, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0B04, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0B05, new string[]{ "ettin:headlessone:lizardman:ogre:orc:troll:waterelemental","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0B06, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400A0B07, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400A0B08, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400A0B09, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400A0B0A, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400A0B0B, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400A0B0C, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400A0B0D, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400A0B0E, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400A0B0F, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400A0B10, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400A0B11, new string[]{ "airelemental:ettin:orc", }, "3"),
                            (0x400A0B12, new string[]{ "ettin:gargoyle:gazer:orc:troll", }, "5"),
                            (0x400A0B13, new string[]{ "ettin:gargoyle:gazer:orc:troll", }, "5"),
                            (0x400A0B14, new string[]{ "ettin:gargoyle:gazer:orc:troll", }, "5"),
                            (0x400A0B15, new string[]{ "ettin:troll", }, "2"),
                            (0x400A0B16, new string[]{ "ettin:troll", }, "2"),
                            (0x400A0B17, new string[]{ "ettin:troll", }, "2"),
                            (0x400A0B18, new string[]{ "ettin:troll", }, "2"),
                            (0x400A0B19, new string[]{ "ettin:troll", }, "2"),
                            (0x400A0B1A, new string[]{ "ettin:troll", }, "2"),
                            (0x400A0B1B, new string[]{ "ettin:troll", }, "2"),
                            (0x400A0B1C, new string[]{ "ettin:troll", }, "2"),
                            (0x400A0B1D, new string[]{ "ettin:troll", }, "2"),
                            (0x400A0B1E, new string[]{ "ettin:troll", }, "2"),
                            (0x400A0B1F, new string[]{ "ettin:troll", }, "2"),
                            (0x400A0B20, new string[]{ "ettin:troll", }, "2"),
                            (0x400A0B21, new string[]{ "airelemental:waterelemental","direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:skeleton:troll", }, "2, 11, 0, 0, 0, 0"),
                            (0x400A0B22, new string[]{ "airelemental:waterelemental","direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:skeleton:troll", }, "2, 11, 0, 0, 0, 0"),
                            (0x400A0B23, new string[]{ "airelemental:waterelemental","direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:skeleton:troll", }, "2, 11, 0, 0, 0, 0"),
                            (0x400A0B24, new string[]{ "airelemental:waterelemental","direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:skeleton:troll", }, "2, 11, 0, 0, 0, 0"),
                            (0x400A0B25, new string[]{ "airelemental:waterelemental","direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:skeleton:troll", }, "2, 11, 0, 0, 0, 0"),
                            (0x400A0B26, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400A0B27, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400A0B28, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400A0B29, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400A0B2A, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400A0B2B, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400A0B2C, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400A0B2D, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400A0B2E, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400A0B2F, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400A0B30, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400A0B31, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400A0B32, new string[]{ "giantspider:scorpion", }, "2"),
                            (0x400A0B33, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400A0B34, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400A0B35, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400A0B36, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400A0B37, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400A0B38, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400A0B39, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400A0B3A, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400A0B3B, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400A0B3C, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400A0B3D, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400A0B3E, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400A0B3F, new string[]{ "corpser:direwolf:ettin:ogre:reaper:skeleton:zombie", }, "7"),
                            (0x400A0B40, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400A0B41, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400A0B42, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400A0B43, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400A0B44, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400A0B45, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400A0B46, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400A0B47, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400A0B48, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400A0B49, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400A0B4A, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400A0B4B, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400A0B4C, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400A0B4D, new string[]{ "harpy:lizardman:orc:ratman:troll", }, "5"),
                            (0x400A0B4E, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400A0B4F, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400A0B50, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400A0B51, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400A0B52, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400A0B53, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400A0B54, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400A0B55, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400A0B56, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400A0B57, new string[]{ "ettin:mongbat:orc:ratman:troll", }, "5"),
                            (0x400A0B58, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400A0B59, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400A0B5A, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400A0B5B, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400A0B5C, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400A0B5D, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400A0B5E, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400A0B5F, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400A0B60, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400A0B61, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400A0B62, new string[]{ "alligator:giantserpent:lizardman:ratman:slime:snake", }, "6"),
                            (0x400A0B63, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0B64, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0B65, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0B66, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0B67, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0B68, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0B69, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0B6A, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0B6B, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0B6C, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0B6D, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0B6E, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0B6F, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0B70, new string[]{ "ettin:ghoul:ogre:orc","wisp", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0B71, new string[]{ "corpser:direwolf:ettin:harpy:lizardman:orc:ratman:reaper", }, "8"),
                            (0x400A0B72, new string[]{ "corpser:direwolf:ettin:harpy:lizardman:orc:ratman:reaper", }, "8"),
                            (0x400A0B73, new string[]{ "corpser:direwolf:ettin:harpy:lizardman:orc:ratman:reaper", }, "8"),
                            (0x400A0B74, new string[]{ "ettin:giantspider:harpy:lizardman:ogre:orc:skeleton:spectre:troll", }, "9"),
                            (0x400A0B75, new string[]{ "ettin:giantspider:harpy:lizardman:ogre:orc:skeleton:spectre:troll", }, "9"),
                            (0x400A0B76, new string[]{ "ettin:giantspider:harpy:lizardman:ogre:orc:skeleton:spectre:troll", }, "9"),
                            (0x400A0B77, new string[]{ "ghoul:giantspider:headlessone:lizardman:orc:ratman:zombie","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0B78, new string[]{ "ghoul:giantspider:headlessone:lizardman:orc:ratman:zombie","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0B79, new string[]{ "ghoul:giantspider:headlessone:lizardman:orc:ratman:zombie","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0B7A, new string[]{ "ghoul:giantspider:headlessone:lizardman:orc:ratman:zombie","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0B7B, new string[]{ "ghoul:giantspider:headlessone:lizardman:orc:ratman:zombie","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0B7C, new string[]{ "ghoul:giantspider:headlessone:lizardman:orc:ratman:zombie","wisp", }, "7, 1, 0, 0, 0, 0"),
                            (0x400A0B7D, new string[]{ "giantspider:orc:ratman:spectre:zombie", }, "5"),
                            (0x400A0B7E, new string[]{ "ettin:gazer:harpy:headlessone:ogre:troll", }, "6"),
                            (0x400A0B7F, new string[]{ "ettin:gazer:harpy:headlessone:ogre:troll", }, "6"),
                            (0x400A0B80, new string[]{ "ettin:gazer:harpy:headlessone:ogre:troll", }, "6"),
                            (0x400A0B81, new string[]{ "ettin:gazer:harpy:headlessone:ogre:troll", }, "6"),
                            (0x400A0B82, new string[]{ "ettin:gazer:harpy:headlessone:ogre:troll", }, "6"),
                            (0x400A0B83, new string[]{ "ettin:gazer:harpy:headlessone:ogre:troll", }, "6"),
                            (0x400A0B84, new string[]{ "airelemental:waterelemental","corpser:Direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:ratman:reaper", }, "2, 10, 0, 0, 0, 0"),
                            (0x400A0B85, new string[]{ "airelemental:waterelemental","corpser:Direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:ratman:reaper", }, "2, 10, 0, 0, 0, 0"),
                            (0x400A0B86, new string[]{ "airelemental:waterelemental","corpser:Direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:ratman:reaper", }, "2, 10, 0, 0, 0, 0"),
                            (0x400A0B87, new string[]{ "airelemental:waterelemental","corpser:Direwolf:ettin:gargoyle:gazer:harpy:lizardman:orc:ratman:reaper", }, "2, 10, 0, 0, 0, 0"),
                            (0x400A0B88, new string[]{ "corpser:direwolf:harpy:reaper:skeleton", }, "5"),
                            (0x400A0B89, new string[]{ "corpser:direwolf:harpy:reaper:skeleton", }, "5"),
                            (0x400A0B8A, new string[]{ "corpser:direwolf:harpy:reaper:skeleton", }, "5"),
                            (0x400A0B8B, new string[]{ "corpser:direwolf:harpy:reaper:skeleton", }, "5"),
                            (0x400A0B8C, new string[]{ "corpser:direwolf:harpy:reaper:skeleton", }, "5"),
                            (0x400A0B8D, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400A0B8E, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400A0B8F, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400A0B90, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400A0B91, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400A0B92, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400A0B93, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400A0B94, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400A0B95, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400A0B96, new string[]{ "airelemental:waterelemental","corpser:direwolf:ettin:gargoyle:gazer:harpy:orc:reaper:troll", }, "2, 9, 0, 0, 0, 0"),
                            (0x400A0B97, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400A0B98, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400A0B99, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400A0B9A, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400A0B9B, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400A0B9C, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400A0B9D, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400A0B9E, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400A0B9F, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400A0BA0, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400A0BA1, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "8"),
                            (0x400A0BA2, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400A0BA3, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400A0BA4, new string[]{ "polarbear:snowleopard:walrus:whitewolf", }, "4"),
                            (0x400A0BA5, new string[]{ "alligator:giantserpent:lizardman:mongbat:ratman:silverserpent:snake", }, "7"),
                            (0x400A0BA6, new string[]{ "alligator:giantserpent:lizardman:mongbat:ratman:silverserpent:snake", }, "7"),
                            (0x400A0BA7, new string[]{ "alligator:giantserpent:lizardman:mongbat:ratman:silverserpent:snake", }, "7"),
                            (0x400A0BA8, new string[]{ "alligator:giantserpent:lizardman:mongbat:ratman:silverserpent:snake", }, "7"),
                            (0x400A0BA9, new string[]{ "alligator:giantserpent:lizardman:mongbat:ratman:silverserpent:snake", }, "7"),
                            (0x400A0BAA, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400A0BAB, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400A0BAC, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400A0BAD, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400A0BAE, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400A0BAF, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400A0BB0, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400A0BB1, new string[]{ "ettin:ghoul:headlessone:ogre:orc:orcishmage:skeleton:troll", }, "8"),
                            (0x400A0BB2, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BB3, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BB4, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BB5, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BB6, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BB7, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BB8, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BB9, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BBA, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BBB, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BBC, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BBD, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BBE, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BBF, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BC0, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BC1, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BC2, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BC3, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BC4, new string[]{ "corpser:ettin:ghoul:giantspider:lizardman:ogre:orc:reaper:skeleton:troll:zombie","wisp", }, "11, 1, 0, 0, 0, 0"),
                            (0x400A0BC5, new string[]{ "airelemental:ettin:gargoyle:gazer:orc:troll:waterelemental", }, "7"),
                            (0x400A0BC6, new string[]{ "airelemental:ettin:gargoyle:gazer:orc:troll:waterelemental", }, "7"),
                            (0x400A0BC7, new string[]{ "airelemental:ettin:gargoyle:gazer:orc:troll:waterelemental", }, "7"),
                            (0x400A0BC8, new string[]{ "airelemental:ettin:gargoyle:gazer:orc:troll:waterelemental", }, "7"),
                            (0x400A0BC9, new string[]{ "airelemental:ettin:gargoyle:gazer:orc:troll:waterelemental", }, "7"),
                            (0x400A0BCA, new string[]{ "timberwolf", }, "7"),
                            (0x400A0BCB, new string[]{ "timberwolf", }, "7"),
                            (0x400A0BCC, new string[]{ "timberwolf", }, "7"),
                            (0x400A0BCD, new string[]{ "timberwolf", }, "7"),
                            (0x400A0BCE, new string[]{ "timberwolf", }, "7"),
                            (0x400A0BCF, new string[]{ "greathart","hind", }, "2, 5, 0, 0, 0, 0"),
                            (0x400A0BD0, new string[]{ "greathart","hind", }, "2, 5, 0, 0, 0, 0"),
                            (0x400A0BD1, new string[]{ "greathart","hind", }, "2, 5, 0, 0, 0, 0"),
                            (0x400A0BD2, new string[]{ "greathart","hind", }, "2, 5, 0, 0, 0, 0"),
                            (0x400A0BD3, new string[]{ "greathart","hind", }, "2, 5, 0, 0, 0, 0"),
                            (0x400A0BD4, new string[]{ "greathart","hind", }, "2, 5, 0, 0, 0, 0"),
                            (0x400A0BD5, new string[]{ "ettin:harpy:headlessone:lizardman:ogre:orc:skeleton:troll:zombie","wisp", }, "9, 1, 0, 0, 0, 0"),
                            (0x400A0BD6, new string[]{ "ettin:harpy:headlessone:lizardman:ogre:orc:skeleton:troll:zombie","wisp", }, "9, 1, 0, 0, 0, 0"),
                            (0x400A0BD7, new string[]{ "ettin:harpy:headlessone:lizardman:ogre:orc:skeleton:troll:zombie","wisp", }, "9, 1, 0, 0, 0, 0"),
                            (0x400A0BD8, new string[]{ "ettin:harpy:headlessone:lizardman:ogre:orc:skeleton:troll:zombie","wisp", }, "9, 1, 0, 0, 0, 0"),
                            (0x400A0BD9, new string[]{ "ettin:harpy:headlessone:lizardman:ogre:orc:skeleton:troll:zombie","wisp", }, "9, 1, 0, 0, 0, 0"),
                            (0x400A0BDA, new string[]{ "ettin:ogre:orc:ratman:skeleton:spectre:zombie", }, "7"),
                            (0x400A0BDB, new string[]{ "ettin:ogre:orc:ratman:skeleton:spectre:zombie", }, "7"),
                            (0x400A0BDC, new string[]{ "ettin:gargoyle:gazer:ogre:orc:troll:waterelemental", }, "7"),
                            (0x400A0BDD, new string[]{ "ettin:gargoyle:gazer:ogre:orc:troll:waterelemental", }, "7"),
                            (0x400A0BDE, new string[]{ "ettin:gargoyle:gazer:ogre:orc:troll:waterelemental", }, "7"),
                            (0x400A0BDF, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0BE0, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0BE1, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0BE2, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0BE3, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0BE4, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0BE5, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0BE6, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0BE7, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0BE8, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0BE9, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0BEA, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0BEB, new string[]{ "mongbat","orc", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0BEC, new string[]{ "orc:ratman", }, "6"),
                            (0x400A0BED, new string[]{ "orc:ratman", }, "6"),
                            (0x400A0BEE, new string[]{ "orc:ratman", }, "6"),
                            (0x400A0BEF, new string[]{ "orc:ratman", }, "6"),
                            (0x400A0BF0, new string[]{ "orc:ratman", }, "6"),
                            (0x400A0BF1, new string[]{ "orc:ratman", }, "6"),
                            (0x400A0BF2, new string[]{ "Orc","Orcishlord","Orcishmage", }, "1, 3, 2, 5, 3, 2"),
                            (0x400A0BF3, new string[]{ "Orc","Orcishlord","Orcishmage", }, "1, 3, 2, 5, 3, 2"),
                            (0x400A0BF4, new string[]{ "orc","ettin","ratman","giantspider","reaper", }, "10, 10, 10, 10, 10, 0"),
                            (0x400A0BF5, new string[]{ "orc","ettin","ratman","giantspider","reaper", }, "10, 10, 10, 10, 10, 0"),
                            (0x400A0BF6, new string[]{ "orc","ettin","ratman","giantspider","reaper", }, "10, 10, 10, 10, 10, 0"),
                            (0x400A0BF7, new string[]{ "orc","ettin","ratman","giantspider","reaper", }, "10, 10, 10, 10, 10, 0"),
                            (0x400A0BF8, new string[]{ "orc","ettin","ratman","giantspider","reaper", }, "10, 10, 10, 10, 10, 0"),
                            (0x400A0BF9, new string[]{ "orc","ettin","ratman","giantspider","reaper", }, "10, 10, 10, 10, 10, 0"),
                            (0x400A0BFA, new string[]{ "Daemon", }, "4"),
                            (0x400A0BFB, new string[]{ "Imp", }, "4"),
                            (0x400A0BFC, new string[]{ "Imp", }, "4"),
                            (0x400A0BFD, new string[]{ "Imp", }, "4"),
                            (0x400A0BFE, new string[]{ "Imp", }, "4"),
                            (0x400A0BFF, new string[]{ "orc","giantspider","giantserpent","gorilla","tropicalbird","whippingvine", }, "5, 5, 5, 5, 5, 5"),
                            (0x400A0C00, new string[]{ "orc","giantspider","giantserpent","gorilla","tropicalbird","whippingvine", }, "10, 10, 10, 10, 10, 10"),
                            (0x400A0C01, new string[]{ "orc","giantspider","giantserpent","gorilla","tropicalbird","whippingvine", }, "10, 10, 10, 10, 10, 10"),
                            (0x400A0C02, new string[]{ "orc","giantspider","giantserpent","gorilla","tropicalbird","whippingvine", }, "10, 10, 10, 10, 10, 10"),
                            (0x400A0C03, new string[]{ "orc","giantspider","giantserpent","gorilla","tropicalbird","whippingvine", }, "10, 10, 10, 10, 10, 10"),
                            (0x400A0C04, new string[]{ "orc","giantspider","giantserpent","gorilla","tropicalbird","whippingvine", }, "10, 10, 10, 10, 10, 10"),
                            (0x400A0C05, new string[]{ "Headlessone","Orc","Orccaptain","Orcishlord","Orcishmage", }, "3, 6, 1, 2, 1, 0"),
                            (0x400A0C06, new string[]{ "Savage","Savagerider","Savageshaman", }, "2, 1, 1, 0, 0, 0"),
                            (0x400A0C07, new string[]{ "Savage","Savagerider","Savageshaman", }, "2, 1, 1, 0, 0, 0"),
                            (0x400A0C08, new string[]{ "Brigand", }, "7"),
                            (0x400A0C09, new string[]{ "Lich", }, "8"),
                            (0x400A0C0A, new string[]{ "Orc","Orcishlord","Orcishmage", }, "3, 1, 1, 0, 0, 0"),
                            (0x400A0C0B, new string[]{ "Orc","Orcishlord","Orcishmage", }, "3, 1, 1, 0, 0, 0"),
                            (0x400A0C0C, new string[]{ "orc","orcBomber","OrcCaptain","OrcishLord","OrcishMage", }, "8, 1, 1, 2, 2, 0"),
                            (0x400A0C0D, new string[]{ "Skeleton", }, "2"),
                            (0x400A0C0E, new string[]{ "Wraith:Shade:Spectre","Ghoul", }, "4, 4, 0, 0, 0, 0"),
                            (0x400A0C0F, new string[]{ "Wraith:Shade:Spectre", }, "1"),
                            (0x400A0C10, new string[]{ "Wraith:Shade:Spectre", }, "1"),
                            (0x400A0C11, new string[]{ "Lich", }, "1"),
                            (0x400A0C12, new string[]{ "Daemon","Drake", }, "1, 2, 0, 0, 0, 0"),
                            (0x400A0C13, new string[]{ "Dragon","Drake", }, "1, 2, 0, 0, 0, 0"),
                            (0x400A0C14, new string[]{ "Daemon", }, "3"),
                            (0x400A0C15, new string[]{ "Dragon","Drake", }, "1, 2, 0, 0, 0, 0"),
                            (0x400A0C16, new string[]{ "Wisp", }, "8"),
                            (0x400A0C17, new string[]{ "Ghoul","Giantrat","Giantserpent","Headlessone","Jwilson", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0C18, new string[]{ "Iceserpent","Icesnake","Polarbear","Snowleopard","Whitewolf","Frostooze", }, "1, 1, 1, 1, 1, 1"),
                            (0x400A0C19, new string[]{ "Frostspider","Frosttroll", }, "1, 4, 0, 0, 0, 0"),
                            (0x400A0C1A, new string[]{ "Brownbear:Grizzlybear:Blackbear","Direwolf:Timberwolf:Greywolf","Rabbit:JackRabbit","Cougar:Panther","Bird:Eagle", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0C1B, new string[]{ "Alligator","Bullfrog","Corpser","Swamptentacle", }, "2, 3, 1, 1, 0, 0"),
                            (0x400A0C1C, new string[]{ "Giantserpent","Giantspider","Boar","Greathart","Hind","Llama", }, "1, 1, 1, 1, 1, 1"),
                            (0x400A0C1D, new string[]{ "Horse","Rat", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0C1E, new string[]{ "Brownbear:Grizzlybear:Blackbear","Direwolf:Timberwolf:Greywolf","Cougar:Panther","Giantserpent","Greathart", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0C1F, new string[]{ "Alligator","Bullfrog","Gorilla","Giantserpent","Gianttoad", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0C20, new string[]{ "Mummy","Ophidianwarrior","Scorpion","Giantserpent", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0C21, new string[]{ "Rottingcorpse","Skeleton","Slime","Snake","Strongmongbat", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0C22, new string[]{ "Ghoul","Giantserpent","Headlessone","Jwilson","Zombie", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0C23, new string[]{ "Cat","Chicken","Dog","Pig","Sheep", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0C24, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C25, new string[]{ "OrcCamp", }, "1"),
                            (0x400A0C26, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C27, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C28, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C29, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C2A, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C2B, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C2C, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C2D, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C2E, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C2F, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C30, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C31, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C32, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C33, new string[]{ "OrcCamp:RatCamp:LizardmanCamp", }, "1"),
                            (0x400A0C34, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C35, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C36, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C37, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C38, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C39, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C3A, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C3B, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C3C, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C3D, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C3E, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C3F, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C40, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C41, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C42, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C43, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C44, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C45, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C46, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C47, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C48, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C49, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C4A, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C4B, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C4C, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C4D, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C4E, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C4F, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C50, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C51, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C52, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C53, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C54, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C55, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C56, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C57, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C58, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C59, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C5A, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C5B, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C5C, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C5D, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C5E, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C5F, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C60, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C61, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C62, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C63, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C64, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C65, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C66, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C67, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C68, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C69, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C6A, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C6B, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C6C, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C6D, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C6E, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C6F, new string[]{ "BlackPearl:Bloodmoss:SpidersSilk:SulfurousAsh:Garlic:Ginseng:Nightshade:MandrakeRoot", }, "160"),
                            (0x400A0C70, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C71, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C72, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C73, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C74, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C75, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C76, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","deepseaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C77, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C78, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C79, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C7A, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C7B, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C7C, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","kraken", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C7D, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C7E, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C7F, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C80, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C81, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C82, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C83, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C84, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C85, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C86, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C87, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C88, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C89, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C8A, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C8B, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C8C, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C8D, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C8E, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C8F, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C90, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C91, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C92, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C93, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C94, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C95, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C96, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C97, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C98, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C99, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C9A, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","deepseaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C9B, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","kraken", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C9C, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C9D, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C9E, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0C9F, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CA1, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CA2, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CA4, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CA5, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CA6, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","kraken", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CA7, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CA8, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CA9, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CAA, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CAB, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CAC, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CAD, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CAE, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CAF, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CB0, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CB1, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CB2, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CB3, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","kraken", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CB4, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CB5, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CB6, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "15, 6, 1, 0, 0, 0"),
                            (0x400A0CB7, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "13, 4, 1, 0, 0, 0"),
                            (0x400A0CB8, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "13, 4, 1, 0, 0, 0"),
                            (0x400A0CB9, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "13, 4, 1, 0, 0, 0"),
                            (0x400A0CBA, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "13, 4, 1, 0, 0, 0"),
                            (0x400A0CBB, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CBC, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CBD, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CBE, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CBF, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CC0, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CC1, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CC2, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CC3, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","deepseaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CC4, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CC5, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CC6, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CC7, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","kraken", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CC8, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","kraken", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CC9, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","deepseaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CCA, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CCB, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","kraken", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CCC, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CCD, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CCE, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CCF, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","kraken", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CD0, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CD1, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CD2, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CD3, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CD4, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CD5, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CD6, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CD7, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CD8, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CD9, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CDA, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CDB, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CDC, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CDD, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CDE, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CDF, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CE0, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","seaserpent", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CE1, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CE2, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CE3, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","waterelemental", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CE4, new string[]{ "DeepSeaSerpent:SeaSerpent:WaterElemental:Kraken","Dolphin","dolphin", }, "10, 2, 1, 0, 0, 0"),
                            (0x400A0CE5, new string[]{ "Earthelemental","Scorpion", }, "4, 2, 0, 0, 0, 0"),
                            (0x400A0CE6, new string[]{ "Earthelemental","Scorpion", }, "7, 3, 0, 0, 0, 0"),
                            (0x400A0CE7, new string[]{ "Earthelemental","Scorpion", }, "4, 1, 0, 0, 0, 0"),
                            (0x400A0CE8, new string[]{ "Earthelemental", }, "4"),
                            (0x400A0CE9, new string[]{ "Earthelemental", }, "4"),
                            (0x400A0CEA, new string[]{ "Earthelemental","Scorpion", }, "4, 2, 0, 0, 0, 0"),
                            (0x400A0CEB, new string[]{ "Earthelemental","Scorpion", }, "6, 20, 0, 0, 0, 0"),
                            (0x400A0CEC, new string[]{ "Airelemental", }, "3"),
                            (0x400A0CED, new string[]{ "Dullcopperelemental", }, "6"),
                            (0x400A0CEE, new string[]{ "Airelemental", }, "3"),
                            (0x400A0CEF, new string[]{ "Airelemental","Waterelemental", }, "3, 2, 0, 0, 0, 0"),
                            (0x400A0CF0, new string[]{ "Kraken","Waterelemental", }, "1, 3, 0, 0, 0, 0"),
                            (0x400A0CF1, new string[]{ "Kraken","Seaserpent", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0CF2, new string[]{ "Dullcopperelemental", }, "2"),
                            (0x400A0CF3, new string[]{ "Dullcopperelemental", }, "4"),
                            (0x400A0CF4, new string[]{ "Earthelemental", }, "2"),
                            (0x400A0CF5, new string[]{ "Fireelemental","Poisonelemental", }, "1, 4, 0, 0, 0, 0"),
                            (0x400A0CF6, new string[]{ "Kraken","Seaserpent","Waterelemental", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0CF7, new string[]{ "Evilmage","Evilmagelord", }, "5, 5, 0, 0, 0, 0"),
                            (0x400A0CF8, new string[]{ "Airelemental","Earthelemental", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A0CFA, new string[]{ "Fireelemental","Poisonelemental","Scorpion", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0CFB, new string[]{ "Airelemental","Earthelemental", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A0CFC, new string[]{ "Fireelemental","Poisonelemental","Scorpion", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0CFD, new string[]{ "Airelemental", }, "2"),
                            (0x400A0CFF, new string[]{ "Eldergazer", }, "2"),
                            (0x400A0D00, new string[]{ "Dullcopperelemental","Fireelemental", }, "4, 2, 0, 0, 0, 0"),
                            (0x400A0D01, new string[]{ "Kraken","Seaserpent","Waterelemental", }, "2, 2, 3, 0, 0, 0"),
                            (0x400A0D02, new string[]{ "Evilmage","Evilmagelord", }, "5, 5, 0, 0, 0, 0"),
                            (0x400A0D03, new string[]{ "Kraken","Seaserpent","Waterelemental", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0D04, new string[]{ "Earthelemental","Scorpion", }, "4, 2, 0, 0, 0, 0"),
                            (0x400A0D05, new string[]{ "Earthelemental","Fireelemental", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A0D06, new string[]{ "Airelemental", }, "2"),
                            (0x400A0D07, new string[]{ "Fireelemental", }, "2"),
                            (0x400A0D08, new string[]{ "Airelemental", }, "2"),
                            (0x400A0D09, new string[]{ "Kraken","Seaserpent", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0D0A, new string[]{ "Evilmage","Evilmagelord", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0D0B, new string[]{ "Eldergazer", }, "2"),
                            (0x400A0D0C, new string[]{ "Airelemental", }, "5"),
                            (0x400A0D0D, new string[]{ "Bloodelemental", }, "8"),
                            (0x400A0D0E, new string[]{ "Fireelemental", }, "2"),
                            (0x400A0D0F, new string[]{ "Earthelemental", }, "4"),
                            (0x400A0D10, new string[]{ "Bloodelemental", }, "2"),
                            (0x400A0D11, new string[]{ "Earthelemental","Fireelemental", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A0D12, new string[]{ "Kraken","Seaserpent","Waterelemental", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0D13, new string[]{ "Kraken","Seaserpent","Waterelemental", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0D14, new string[]{ "Kraken","Waterelemental", }, "1, 3, 0, 0, 0, 0"),
                            (0x400A0D15, new string[]{ "Blacksolenworker", }, "2"),
                            (0x400A0D16, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400A0D17, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400A0D18, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400A0D19, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400A0D1A, new string[]{ "Blacksolenwarrior", }, "1"),
                            (0x400A0D1B, new string[]{ "Blacksolenwarrior", }, "1"),
                            (0x400A0D1C, new string[]{ "Blacksolenwarrior", }, "1"),
                            (0x400A0D1D, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400A0D1E, new string[]{ "Blacksolenworker", }, "2"),
                            (0x400A0D1F, new string[]{ "Antlion", }, "1"),
                            (0x400A0D20, new string[]{ "Blacksolenworker", }, "2"),
                            (0x400A0D21, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400A0D22, new string[]{ "Blacksolenworker","Blacksolenqueen", }, "2, 1, 0, 0, 0, 0"),
                            (0x400A0D23, new string[]{ "Blacksolenwarrior", }, "1"),
                            (0x400A0D24, new string[]{ "Blacksolenwarrior", }, "2"),
                            (0x400A0D25, new string[]{ "Blacksolenwarrior", }, "1"),
                            (0x400A0D26, new string[]{ "Beetle", }, "2"),
                            (0x400A0D27, new string[]{ "Blacksolenworker", }, "2"),
                            (0x400A0D28, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400A0D29, new string[]{ "Antlion", }, "1"),
                            (0x400A0D2A, new string[]{ "Dreadspider", }, "2"),
                            (0x400A0D2B, new string[]{ "Blacksolenwarrior", }, "1"),
                            (0x400A0D2C, new string[]{ "Blacksolenwarrior", }, "2"),
                            (0x400A0D2D, new string[]{ "Blacksolenwarrior", }, "2"),
                            (0x400A0D2E, new string[]{ "Blacksolenwarrior", }, "1"),
                            (0x400A0D2F, new string[]{ "Blacksolenqueen","Blacksolenworker", }, "1, 2, 0, 0, 0, 0"),
                            (0x400A0D30, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400A0D31, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400A0D32, new string[]{ "Blacksolenworker", }, "1"),
                            (0x400A0D33, new string[]{ "Blacksolenworker", }, "3"),
                            (0x400A0D34, new string[]{ "Beetle", }, "2"),
                            (0x400A0D35, new string[]{ "Blacksolenwarrior", }, "2"),
                            (0x400A0D36, new string[]{ "Antlion", }, "1"),
                            (0x400A0D37, new string[]{ "Blacksolenworker", }, "2"),
                            (0x400A0D38, new string[]{ "Blacksolenwarrior", }, "2"),
                            (0x400A0D39, new string[]{ "Blacksolenworker", }, "2"),
                            (0x400A0D3A, new string[]{ "Antlion", }, "3"),
                            (0x400A0D3B, new string[]{ "Antlion", }, "1"),
                            (0x400A0D3C, new string[]{ "redsoleninfiltratorqueen","redsoleninfiltratorwarrior", }, "1, 2, 0, 0, 0, 0"),
                            (0x400A0D3D, new string[]{ "redsoleninfiltratorqueen","redsoleninfiltratorwarrior", }, "1, 2, 0, 0, 0, 0"),
                            (0x400A0D3E, new string[]{ "redsoleninfiltratorqueen","redsoleninfiltratorwarrior", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0D3F, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D40, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0D41, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D42, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D43, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0D44, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D45, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D46, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0D47, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D48, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D49, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0D4A, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D4B, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D4C, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0D4D, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D4E, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D4F, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0D50, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D51, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D52, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0D53, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D54, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D55, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0D56, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D57, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D58, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0D59, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D5A, new string[]{ "Balron","Dragon","Drake","Nightmare", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D5B, new string[]{ "Ophidianarchmage","Ophidianknight","Ophidianmage","Ophidianwarrior","Ophidianmatriarch", }, "1, 1, 1, 1, 1, 0"),
                            (0x400A0D5C, new string[]{ "Terathanavenger","Terathandrone","Terathanmatriarch","Terathanwarrior", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0D5D, new string[]{ "dragon","drake", }, "1, 2, 0, 0, 0, 0"),
                            (0x400A0D5E, new string[]{ "lichlord","lich", }, "1, 2, 0, 0, 0, 0"),
                            (0x400A0D5F, new string[]{ "daemon", }, "3"),
                            (0x400A0D60, new string[]{ "Bird","Cat","Dog","Rat", }, "15, 15, 15, 15, 0, 0"),
                            (0x400A0D61, new string[]{ "Cow", }, "7"),
                            (0x400A0D62, new string[]{ "Cow", }, "7"),
                            (0x400A0D63, new string[]{ "Bird","Cat","Dog","Rat", }, "5, 5, 5, 5, 0, 0"),
                            (0x400A0D64, new string[]{ "Chicken", }, "5"),
                            (0x400A0D65, new string[]{ "Bird","Cat","Dog","Rat", }, "5, 5, 5, 5, 0, 0"),
                            (0x400A0D66, new string[]{ "Chicken", }, "5"),
                            (0x400A0D67, new string[]{ "Bird","Cat","Dog","Rat", }, "7, 7, 7, 7, 0, 0"),
                            (0x400A0D68, new string[]{ "Chicken", }, "5"),
                            (0x400A0D69, new string[]{ "Bird","Cat","Dog","Rat", }, "12, 12, 12, 12, 0, 0"),
                            (0x400A0D6A, new string[]{ "Bird","Cat","Dog","Rat", }, "8, 8, 8, 8, 0, 0"),
                            (0x400A0D6B, new string[]{ "Chicken:Sheep:Cow", }, "3"),
                            (0x400A0D6C, new string[]{ "Chicken:Sheep:Cow", }, "3"),
                            (0x400A0D6D, new string[]{ "Chicken:Sheep:Cow", }, "3"),
                            (0x400A0D6E, new string[]{ "Bird","Cat","Dog","Rat", }, "20, 20, 20, 20, 0, 0"),
                            (0x400A0D6F, new string[]{ "Sheep", }, "15"),
                            (0x400A0D70, new string[]{ "Sheep", }, "15"),
                            (0x400A0D71, new string[]{ "Sheep", }, "15"),
                            (0x400A0D72, new string[]{ "Chicken", }, "2"),
                            (0x400A0D73, new string[]{ "Chicken", }, "2"),
                            (0x400A0D74, new string[]{ "Chicken", }, "2"),
                            (0x400A0D75, new string[]{ "Bird","Cat","Dog","Rat", }, "4, 4, 4, 4, 0, 0"),
                            (0x400A0D77, new string[]{ "Bird","Cat","Dog","Rat", }, "10, 10, 10, 10, 0, 0"),
                            (0x400A0D78, new string[]{ "Bird","Cat","Dog","Rat", }, "10, 10, 10, 10, 0, 0"),
                            (0x400A0D79, new string[]{ "Bird","Cat","Dog","Rat", }, "5, 5, 5, 5, 0, 0"),
                            (0x400A0D7A, new string[]{ "Bird","Cat","Dog","Rat", }, "5, 5, 5, 5, 0, 0"),
                            (0x400A0D7B, new string[]{ "Bird","Cat","Dog","Rat", }, "3, 3, 3, 3, 0, 0"),
                            (0x400A0D7C, new string[]{ "Bird","Cat","Dog","Rat", }, "4, 4, 4, 4, 0, 0"),
                            (0x400A0D7D, new string[]{ "Chicken:Cow:Sheep", }, "5"),
                            (0x400A0D7E, new string[]{ "Chicken:Cow:Sheep", }, "5"),
                            (0x400A0D7F, new string[]{ "Bird","Cat","Dog","Rat", }, "10, 10, 10, 10, 0, 0"),
                            (0x400A0D80, new string[]{ "Bird","Cat","Dog","Rat", }, "5, 5, 5, 5, 0, 0"),
                            (0x400A0D81, new string[]{ "Bird","Cat","Dog","Rat", }, "12, 12, 12, 12, 0, 0"),
                            (0x400A0D82, new string[]{ "Bird","Cat","Dog","Rat", }, "10, 10, 10, 10, 0, 0"),
                            (0x400A0D83, new string[]{ "Chicken", }, "10"),
                            (0x400A0D84, new string[]{ "Bird","Cat","Dog","Rat", }, "4, 4, 4, 4, 0, 0"),
                            (0x400A0D85, new string[]{ "Bird","Cat","Dog","Rat", }, "6, 6, 6, 6, 0, 0"),
                            (0x400A0D86, new string[]{ "Bird","Cat","Dog","Rat", }, "6, 6, 6, 6, 0, 0"),
                            (0x400A0D87, new string[]{ "Bird","Cat","Dog","Rat", }, "5, 5, 5, 5, 0, 0"),
                            (0x400A0D88, new string[]{ "Bird","Cat","Dog","Rat", }, "4, 4, 4, 4, 0, 0"),
                            (0x400A0D89, new string[]{ "Bird","Cat","Dog","Rat", }, "18, 18, 18, 18, 0, 0"),
                            (0x400A0D8A, new string[]{ "Bird","Cat","Dog","Rat", }, "12, 12, 12, 12, 0, 0"),
                            (0x400A0D8B, new string[]{ "Chicken:Cow:Sheep", }, "5"),
                            (0x400A0D8C, new string[]{ "Chicken", }, "2"),
                            (0x400A0D8D, new string[]{ "Chicken", }, "1"),
                            (0x400A0D8E, new string[]{ "bird", }, "3"),
                            (0x400A0D8F, new string[]{ "bird", }, "5"),
                            (0x400A0D90, new string[]{ "bird", }, "3"),
                            (0x400A0D91, new string[]{ "bird", }, "5"),
                            (0x400A0D92, new string[]{ "bird", }, "3"),
                            (0x400A0D93, new string[]{ "bird", }, "5"),
                            (0x400A0D94, new string[]{ "Bird", }, "10"),
                            (0x400A0D95, new string[]{ "Bird","Cat","Dog","Rat", }, "12, 12, 12, 12, 0, 0"),
                            (0x400A0D96, new string[]{ "Bird","Cat","Dog","Rat", }, "6, 6, 6, 6, 0, 0"),
                            (0x400A0D97, new string[]{ "Chicken:Sheep:Cow:Bull", }, "5"),
                            (0x400A0D98, new string[]{ "Orderguard", }, "1"),
                            (0x400A0D99, new string[]{ "Orderguard", }, "1"),
                            (0x400A0D9A, new string[]{ "Chaosguard", }, "1"),
                            (0x400A0D9B, new string[]{ "Chaosguard", }, "1"),
                            (0x400A0D9C, new string[]{ "Towncrier", }, "1"),
                            (0x400A0D9D, new string[]{ "Towncrier", }, "1"),
                            (0x400A0D9E, new string[]{ "Towncrier", }, "1"),
                            (0x400A0D9F, new string[]{ "Towncrier", }, "1"),
                            (0x400A0DA0, new string[]{ "Towncrier", }, "1"),
                            (0x400A0DA1, new string[]{ "Towncrier", }, "1"),
                            (0x400A0DA2, new string[]{ "Towncrier", }, "1"),
                            (0x400A0DA3, new string[]{ "Towncrier", }, "1"),
                            (0x400A0DA4, new string[]{ "Towncrier", }, "1"),
                            (0x400A0DA5, new string[]{ "Towncrier", }, "1"),
                            (0x400A0DA6, new string[]{ "Towncrier", }, "1"),
                            (0x400A0DA7, new string[]{ "Towncrier", }, "1"),
                            (0x400A0DA8, new string[]{ "Towncrier", }, "1"),
                            (0x400A0DAA, new string[]{ "Towncrier", }, "1"),
                            (0x400A0DAB, new string[]{ "Towncrier", }, "1"),
                            (0x400A0DAC, new string[]{ "Towncrier", }, "1"),
                            (0x400A0DAD, new string[]{ "Towncrier", }, "1"),
                            (0x400A0DAE, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400A0DAF, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400A0DB0, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400A0DB1, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400A0DB2, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400A0DB3, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400A0DB4, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400A0DB5, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400A0DB6, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400A0DB7, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400A0DB8, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400A0DB9, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400A0DBA, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400A0DBB, new string[]{ "Escortablemage:Seekerofadventure:Noble", }, "3"),
                            (0x400A0DBC, new string[]{ "Dreadspider","Fireelemental","Hellcat","Lavalizard","Lavasnake", }, "2, 1, 1, 1, 1, 0"),
                            (0x400A0DBD, new string[]{ "Dreadspider","Fireelemental","Hellcat","Lavalizard","Lavasnake", }, "2, 1, 1, 1, 1, 0"),
                            (0x400A0DBE, new string[]{ "Dreadspider","Fireelemental","Hellcat","Lavalizard","Lavasnake", }, "2, 1, 1, 1, 1, 0"),
                            (0x400A0DBF, new string[]{ "Dreadspider","Fireelemental","Hellcat","Lavalizard","Lavasnake", }, "2, 1, 1, 1, 1, 0"),
                            (0x400A0DC0, new string[]{ "Dreadspider","Fireelemental","Hellcat","Lavalizard","Lavasnake", }, "2, 1, 1, 1, 1, 0"),
                            (0x400A0DC1, new string[]{ "Herbalist","Mage", }, "2, 2, 0, 0, 0, 0"),
                            (0x400A0DC2, new string[]{ "jeweler", }, "1"),
                            (0x400A0DC3, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DC4, new string[]{ "bowyer", }, "1"),
                            (0x400A0DC5, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0DC6, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0DC7, new string[]{ "carpenter","architect","realestatebroker", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0DC8, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0DC9, new string[]{ "animaltrainer", }, "1"),
                            (0x400A0DCA, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DCB, new string[]{ "innkeeper", }, "1"),
                            (0x400A0DCC, new string[]{ "tinkerguildmaster", }, "1"),
                            (0x400A0DCD, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0DCE, new string[]{ "warriorguildmaster","hirefighter", }, "1, 2, 0, 0, 0, 0"),
                            (0x400A0DCF, new string[]{ "innkeeper", }, "1"),
                            (0x400A0DD0, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DD1, new string[]{ "tanner","furtrader", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DD2, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0DD3, new string[]{ "fisherman", }, "1"),
                            (0x400A0DD4, new string[]{ "scribe", }, "1"),
                            (0x400A0DD5, new string[]{ "blacksmithguildmaster","blacksmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DD6, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DD7, new string[]{ "animaltrainer", }, "1"),
                            (0x400A0DD8, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0DD9, new string[]{ "butcher", }, "1"),
                            (0x400A0DDA, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DDB, new string[]{ "herbalist","alchemist","customhairstylist", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0DDC, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DDD, new string[]{ "scribe", }, "1"),
                            (0x400A0DDE, new string[]{ "jeweler", }, "1"),
                            (0x400A0DDF, new string[]{ "baker", }, "1"),
                            (0x400A0DE0, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DE1, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DE2, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DE3, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0DE4, new string[]{ "bard","bardguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DE5, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0DE6, new string[]{ "minerguildmaster", }, "1"),
                            (0x400A0DE7, new string[]{ "innkeeper", }, "1"),
                            (0x400A0DE8, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DE9, new string[]{ "mage", }, "1"),
                            (0x400A0DEA, new string[]{ "towncrier", }, "1"),
                            (0x400A0DEB, new string[]{ "orderguard","hirefighter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DEC, new string[]{ "farmer","rancher", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DED, new string[]{ "escortablemage", }, "1"),
                            (0x400A0DEE, new string[]{ "orderguard", }, "1"),
                            (0x400A0DEF, new string[]{ "hirefighter", }, "3"),
                            (0x400A0DF0, new string[]{ "animaltrainer", }, "1"),
                            (0x400A0DF1, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DF2, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0DF3, new string[]{ "innkeeper", }, "1"),
                            (0x400A0DF5, new string[]{ "thiefguildmaster", }, "1"),
                            (0x400A0DF6, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0DF7, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DF8, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DF9, new string[]{ "carpenter","architect","realestatebroker", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0DFA, new string[]{ "fisherman", }, "1"),
                            (0x400A0DFB, new string[]{ "tanner","furtrader", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DFC, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DFD, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DFE, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0DFF, new string[]{ "fisherman", }, "1"),
                            (0x400A0E00, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E01, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E02, new string[]{ "fisherman", }, "1"),
                            (0x400A0E03, new string[]{ "fisherman", }, "1"),
                            (0x400A0E04, new string[]{ "fisherman", }, "1"),
                            (0x400A0E05, new string[]{ "jeweler", }, "1"),
                            (0x400A0E06, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E07, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E08, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E09, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E0A, new string[]{ "baker", }, "1"),
                            (0x400A0E0B, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E0D, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E0E, new string[]{ "butcher","farmer", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E0F, new string[]{ "butcher", }, "1"),
                            (0x400A0E10, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E12, new string[]{ "carpenter","architect","realestatebroker", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E13, new string[]{ "tinker","tinkerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E14, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0E15, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E16, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E17, new string[]{ "fisherman", }, "1"),
                            (0x400A0E18, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E19, new string[]{ "scribe", }, "1"),
                            (0x400A0E1A, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E1B, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E1C, new string[]{ "innkeeper", }, "1"),
                            (0x400A0E1D, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E1E, new string[]{ "fisherguildmaster", }, "1"),
                            (0x400A0E1F, new string[]{ "innkeeper", }, "1"),
                            (0x400A0E20, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E21, new string[]{ "tinker","tinkerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E22, new string[]{ "baker", }, "1"),
                            (0x400A0E23, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E24, new string[]{ "baker", }, "1"),
                            (0x400A0E25, new string[]{ "jeweler", }, "1"),
                            (0x400A0E26, new string[]{ "fisherguildmaster", }, "1"),
                            (0x400A0E28, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E29, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0E2A, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E2B, new string[]{ "fisherman", }, "1"),
                            (0x400A0E2C, new string[]{ "minerguildmaster", }, "1"),
                            (0x400A0E2D, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E2E, new string[]{ "provisioner", }, "1"),
                            (0x400A0E2F, new string[]{ "cobbler", }, "1"),
                            (0x400A0E30, new string[]{ "butcher", }, "1"),
                            (0x400A0E31, new string[]{ "animaltrainer", }, "1"),
                            (0x400A0E32, new string[]{ "tanner","furtrader", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E33, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E34, new string[]{ "architect", }, "1"),
                            (0x400A0E35, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E36, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E37, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0E38, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E39, new string[]{ "tinker","tinkerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E3B, new string[]{ "minerguildmaster", }, "1"),
                            (0x400A0E3C, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E3D, new string[]{ "minerguildmaster", }, "1"),
                            (0x400A0E3E, new string[]{ "bard","bardguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E3F, new string[]{ "warriorguildmaster", }, "1"),
                            (0x400A0E40, new string[]{ "carpenter","realestatebroker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E41, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E42, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E43, new string[]{ "innkeeper", }, "1"),
                            (0x400A0E44, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E45, new string[]{ "mageguildmaster", }, "1"),
                            (0x400A0E46, new string[]{ "butcher","farmer", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E47, new string[]{ "butcher", }, "1"),
                            (0x400A0E48, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E49, new string[]{ "herbalist","alchemist","customhairstylist", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E4A, new string[]{ "baker", }, "1"),
                            (0x400A0E4B, new string[]{ "carpenter","architect","realestatebroker", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E4C, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E4D, new string[]{ "fisherman", }, "1"),
                            (0x400A0E4E, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E4F, new string[]{ "herbalist","alchemist","customhairstylist", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E50, new string[]{ "innkeeper", }, "1"),
                            (0x400A0E51, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E52, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E53, new string[]{ "innkeeper", }, "1"),
                            (0x400A0E54, new string[]{ "shipwright","mapmaker", }, "1, 1"),
                            (0x400A0E55, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0E56, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E57, new string[]{ "tanner", }, "1"),
                            (0x400A0E58, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E59, new string[]{ "bardguildmaster", }, "1"),
                            (0x400A0E5A, new string[]{ "jeweler", }, "1"),
                            (0x400A0E5B, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0E5C, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E5D, new string[]{ "bowyer", }, "1"),
                            (0x400A0E5E, new string[]{ "butcher", }, "1"),
                            (0x400A0E5F, new string[]{ "furtrader", }, "1"),
                            (0x400A0E60, new string[]{ "fisherman", }, "1"),
                            (0x400A0E61, new string[]{ "scribe", }, "1"),
                            (0x400A0E62, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E63, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E64, new string[]{ "butcher", }, "1"),
                            (0x400A0E65, new string[]{ "innkeeper", }, "1"),
                            (0x400A0E66, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E67, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E68, new string[]{ "bardguildmaster", }, "1"),
                            (0x400A0E69, new string[]{ "tanner","furtrader", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E6A, new string[]{ "innkeeper", }, "1"),
                            (0x400A0E6B, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E6C, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E6D, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E6E, new string[]{ "baker", }, "1"),
                            (0x400A0E6F, new string[]{ "baker", }, "1"),
                            (0x400A0E70, new string[]{ "butcher", }, "1"),
                            (0x400A0E71, new string[]{ "warriorguildmaster", }, "1"),
                            (0x400A0E72, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E73, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E74, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E75, new string[]{ "animaltrainer", }, "1"),
                            (0x400A0E76, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E77, new string[]{ "animaltrainer", }, "1"),
                            (0x400A0E78, new string[]{ "bowyer", }, "1"),
                            (0x400A0E79, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E7B, new string[]{ "fisherman", }, "1"),
                            (0x400A0E7C, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E7D, new string[]{ "tinker","tinkerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E7E, new string[]{ "warriorguildmaster", }, "1"),
                            (0x400A0E7F, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E80, new string[]{ "herbalist","alchemist","customhairstylist", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E81, new string[]{ "innkeeper", }, "1"),
                            (0x400A0E83, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0E84, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0E85, new string[]{ "fisherman", }, "1"),
                            (0x400A0E87, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E88, new string[]{ "rangerguildmaster", }, "1"),
                            (0x400A0E89, new string[]{ "innkeeper", }, "1"),
                            (0x400A0E8A, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E8B, new string[]{ "fisherman", }, "1"),
                            (0x400A0E8C, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E8D, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E8E, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E8F, new string[]{ "animaltrainer", }, "1"),
                            (0x400A0E90, new string[]{ "bowyer", }, "1"),
                            (0x400A0E91, new string[]{ "butcher", }, "1"),
                            (0x400A0E92, new string[]{ "carpenter","architect","realestatebroker", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E93, new string[]{ "innkeeper", }, "1"),
                            (0x400A0E94, new string[]{ "butcher","farmer", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E95, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E96, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E97, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E98, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E99, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0E9A, new string[]{ "scribe", }, "1"),
                            (0x400A0E9B, new string[]{ "jeweler", }, "1"),
                            (0x400A0E9C, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E9D, new string[]{ "innkeeper", }, "1"),
                            (0x400A0E9E, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0E9F, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EA0, new string[]{ "tinkerguildmaster", }, "1"),
                            (0x400A0EA1, new string[]{ "butcher", }, "1"),
                            (0x400A0EA2, new string[]{ "fisherman", }, "1"),
                            (0x400A0EA3, new string[]{ "tanner","furtrader", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EA4, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EA5, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EA6, new string[]{ "animaltrainer", }, "1"),
                            (0x400A0EA7, new string[]{ "warriorguildmaster", }, "1"),
                            (0x400A0EA8, new string[]{ "innkeeper", }, "1"),
                            (0x400A0EA9, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0EAA, new string[]{ "baker", }, "1"),
                            (0x400A0EAB, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EAC, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EAD, new string[]{ "warriorguildmaster", }, "1"),
                            (0x400A0EAE, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0EAF, new string[]{ "animaltrainer", }, "1"),
                            (0x400A0EB0, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EB1, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0EB2, new string[]{ "minerguildmaster", }, "1"),
                            (0x400A0EB3, new string[]{ "bowyer", }, "1"),
                            (0x400A0EB4, new string[]{ "jeweler", }, "1"),
                            (0x400A0EB5, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0EB6, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0EB7, new string[]{ "tanner","furtrader", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EB8, new string[]{ "baker", }, "1"),
                            (0x400A0EB9, new string[]{ "fisherman", }, "1"),
                            (0x400A0EBA, new string[]{ "innkeeper", }, "1"),
                            (0x400A0EBB, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EBC, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EBD, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0EBE, new string[]{ "beekeeper", }, "1"),
                            (0x400A0EBF, new string[]{ "butcher", }, "1"),
                            (0x400A0EC0, new string[]{ "warriorguildmaster", }, "1"),
                            (0x400A0EC1, new string[]{ "butcher","farmer", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EC2, new string[]{ "fisherguildmaster", }, "1"),
                            (0x400A0EC3, new string[]{ "butcher","farmer", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EC4, new string[]{ "tinker","tinkerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EC5, new string[]{ "carpenter","architect","realestatebroker", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0EC6, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EC8, new string[]{ "innkeeper", }, "1"),
                            (0x400A0EC9, new string[]{ "armorer","weaponsmith", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0ECA, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0ECB, new string[]{ "herbalist","alchemist","customhairstylist", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0ECC, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0ECD, new string[]{ "tavernkeeper","waiter","cook","barkeeper", }, "1, 1, 1, 1, 0, 0"),
                            (0x400A0ECE, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0ECF, new string[]{ "scribe", }, "1"),
                            (0x400A0ED0, new string[]{ "innkeeper", }, "1"),
                            (0x400A0ED1, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0ED2, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0ED3, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0ED4, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0ED5, new string[]{ "innkeeper", }, "1"),
                            (0x400A0ED6, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0ED7, new string[]{ "innkeeper", }, "1"),
                            (0x400A0ED8, new string[]{ "scribe", }, "1"),
                            (0x400A0ED9, new string[]{ "baker", }, "1"),
                            (0x400A0EDA, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EDB, new string[]{ "herbalist","alchemist","customhairstylist", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0EDC, new string[]{ "bowyer", }, "1"),
                            (0x400A0EDD, new string[]{ "tanner","furtrader", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EDE, new string[]{ "butcher", }, "1"),
                            (0x400A0EDF, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EE0, new string[]{ "innkeeper", }, "1"),
                            (0x400A0EE1, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EE2, new string[]{ "carpenter","architect","realestatebroker", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0EE3, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EE4, new string[]{ "bowyer", }, "1"),
                            (0x400A0EE5, new string[]{ "shipwright","mapmaker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EE6, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EE7, new string[]{ "baker", }, "1"),
                            (0x400A0EE8, new string[]{ "provisioner","cobbler", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EE9, new string[]{ "banker","minter", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EEA, new string[]{ "blacksmith","blacksmithguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EEB, new string[]{ "innkeeper", }, "1"),
                            (0x400A0EEC, new string[]{ "animaltrainer", }, "1"),
                            (0x400A0EED, new string[]{ "tailor","weaver","tailorguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0EEE, new string[]{ "healer","healerguildmaster", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EEF, new string[]{ "mage","alchemist","mageguildmaster", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0EF0, new string[]{ "minter","banker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EF1, new string[]{ "mageguildmaster","alchemist","mage", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0EF2, new string[]{ "tinkerguildmaster","tinker", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EF3, new string[]{ "jeweler", }, "1"),
                            (0x400A0EF4, new string[]{ "animaltrainer", }, "1"),
                            (0x400A0EF5, new string[]{ "butcher", }, "1"),
                            (0x400A0EF6, new string[]{ "weaponsmith","armorer", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EF7, new string[]{ "customhairstylist","alchemist","herbalist", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0EF8, new string[]{ "mapmaker","shipwright", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EF9, new string[]{ "tailorguildmaster","weaver","tailor", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0EFA, new string[]{ "fisherman", }, "1"),
                            (0x400A0EFB, new string[]{ "baker", }, "1"),
                            (0x400A0EFC, new string[]{ "innkeeper", }, "1"),
                            (0x400A0EFD, new string[]{ "healerguildmaster","healer", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0EFE, new string[]{ "realestatebroker","architect","carpenter", }, "1, 1, 1, 0, 0, 0"),
                            (0x400A0EFF, new string[]{ "cobbler","provisioner", }, "1, 1, 0, 0, 0, 0"),
                            (0x400A0F1A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F1B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F1C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F1D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F1E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F1F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F20, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F21, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F22, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F23, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F24, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F25, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F26, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F27, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F28, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F29, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F2A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F2B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F2C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F2D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F2E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F2F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F30, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F31, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F32, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F33, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F34, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F35, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F36, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F37, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F38, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F39, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F3A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F3B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F3C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F3D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F3E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F3F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F40, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F41, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F42, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F43, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F44, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F45, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F46, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F47, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F48, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F49, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F4A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F4B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F4C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F4D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F4E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F4F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F50, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F51, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F52, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F53, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F54, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F55, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F56, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F57, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F58, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F59, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F5A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F5B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F5C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F5D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F5E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F5F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F60, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F61, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F62, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F63, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F64, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F65, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F66, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F67, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F68, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F69, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F6A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F6B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F6C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F6D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F6E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F6F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F70, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F71, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F72, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F73, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F74, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F75, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F76, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F77, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F78, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F79, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F7A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F7B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F7C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F7D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F7E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F7F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F80, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F81, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F82, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F83, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F84, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F85, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F86, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F88, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F89, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F8A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F8B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F8C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F8D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F8E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F8F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F90, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F91, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F92, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F93, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F94, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F95, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F96, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F97, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F98, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F99, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F9A, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F9B, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F9C, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F9D, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F9E, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0F9F, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FA0, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FA1, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FA2, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FA3, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FA4, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FA5, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FA6, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FA7, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FA8, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FA9, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FAA, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FAB, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FAC, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FAD, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FAE, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FAF, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FB0, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FB1, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FB2, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FB3, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FB4, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FB5, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FB6, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FB7, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FB8, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FB9, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FBA, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FBB, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FBC, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FBD, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FBE, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FBF, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FC0, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FC1, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FC2, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FC3, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FC4, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FC5, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FC6, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FC7, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FC8, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FC9, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FCA, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FCC, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FCD, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FCE, new string[]{ "Boar:Cougar:Goat:Horse:Panther:Pig:Sheep","BlackBear:GrizzlyBear:BrownBear:TimberWolf:GreyWolf","WanderingHealer:EvilWanderingHealer","Bird:Eagle","GreatHart:Hind","Bull:Cow", }, "7, 5, 2, 2, 2, 2"),
                            (0x400A0FCF, new string[]{ "Jukawarrior", }, "2"),
                            (0x400A0FD0, new string[]{ "Jukalord","Jukamage","Jukawarrior", }, "1, 1, 3, 0, 0, 0"),
                            (0x400A0FD1, new string[]{ "Golemcontroller","Golem", }, "4, 4, 0, 0, 0, 0"),
                            (0x400A0FD2, new string[]{ "Jukalord","Jukamage","Jukawarrior", }, "1, 1, 3, 0, 0, 0"),
                            (0x400A0FD3, new string[]{ "Jukalord","Jukamage","Jukawarrior", }, "1, 1, 3, 0, 0, 0"),
                            (0x400A0FD4, new string[]{ "Jukalord","Jukamage","Jukawarrior", }, "1, 1, 3, 0, 0, 0"),
                            (0x400A0FD5, new string[]{ "Brigand","Jukalord","Jukamage","Jukawarrior", }, "4, 1, 1, 3, 0, 0"),
                            (0x400A0FD6, new string[]{ "Golem","Jukalord","Jukamage","Jukawarrior", }, "1, 1, 1, 3, 0, 0"),
                            (0x400A0FD7, new string[]{ "Jukalord","Jukamage","Jukawarrior", }, "1, 1, 3, 0, 0, 0"),
                            (0x4001CF8C, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF8D, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF8E, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF8F, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF90, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF91, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF92, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF93, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF94, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF95, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF96, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF97, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF98, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF99, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF9A, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF9B, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF9C, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF9D, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF9E, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CF9F, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFA0, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFA1, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFA2, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFA3, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFA4, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFA5, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFA6, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFA7, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFA8, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFA9, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFAA, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFAB, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFAC, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFAD, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFAE, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFAF, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFB0, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFB1, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFB2, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFB3, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFB4, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFB5, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFB6, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFB7, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFB8, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFB9, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFBA, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFBB, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFBC, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFBD, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFBE, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFBF, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFC0, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFC1, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFC2, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFC4, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFC5, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFC6, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFC7, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFC8, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFC9, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFCA, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFCB, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFCC, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFCD, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFCE, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFCF, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFD0, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFD1, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFD3, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFD4, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFD5, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFD6, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFD7, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFD8, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFD9, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFDA, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFDB, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFDC, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFDE, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFE0, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFE2, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFE4, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFE7, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFE9, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFEB, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFED, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFEF, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFF2, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFF4, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFF6, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFF8, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFFA, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFFD, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001CFFF, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D002, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D004, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D006, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D008, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D00A, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D00C, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D00E, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D010, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D012, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D014, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D016, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D018, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D01A, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D01C, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D01E, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D020, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D022, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D024, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D027, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D029, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D02B, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D02D, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D02F, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D031, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D032, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D033, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D034, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D036, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D038, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D03A, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D03C, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D03E, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D040, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D042, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D044, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D046, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D048, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D04A, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D04C, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D04E, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D050, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D052, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D054, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D060, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D11E, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D147, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D14B, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D14F, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D157, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D15A, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D15D, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D166, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D170, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D179, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D17C, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D17E, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D180, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D18E, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D192, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D195, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D198, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D19B, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1A0, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1A4, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1A7, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1AC, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1B1, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1B4, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1B8, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1BB, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1BD, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1BF, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1C1, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1C3, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1C5, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1C7, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1CA, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1CC, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1CD, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1CF, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1D1, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1D3, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1D5, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1D7, new string[]{ "Crow:Magpie:Raven", }, "2"),
                            (0x4001D1E7, new string[]{ "RedSolenWorker", }, "1"),
                            (0x4001D1E9, new string[]{ "WhiteWyrm","IceSerpent", }, "1, 1, 0, 0, 0, 0"),
                            (0x4001D1EC, new string[]{ "WhiteWyrm","IceSerpent", }, "1, 1, 0, 0, 0, 0"),
                            (0x4001D1EE, new string[]{ "GiantSpider","DreadSpider", }, "1, 1, 0, 0, 0, 0"),
                            (0x4001D1F0, new string[]{ "BogThing","SwampDragon", }, "1, 1, 0, 0, 0, 0"),
                            (0x4001D1F4, new string[]{ "Wisp","Wisp", }, "1, 1, 0, 0, 0, 0"),
                            (0x4001D1F9, new string[]{ "Wisp", }, "1"),
                            (0x4001D1FD, new string[]{ "Centaur","Kirin","Unicorn","Pixie", }, "1, 1, 1, 1, 0, 0"),
                            (0x4001D20A, new string[]{ "ForestOstard","FrenziedOstard","FrenziedOstard", }, "1, 1, 1, 0, 0, 0"),
                            (0x4001D210, new string[]{ "Wyvern","OphidianWarrior", }, "1, 1, 0, 0, 0, 0"),
                            (0x4001D213, new string[]{ "FireSteed","FireSteed","HellCat", }, "1, 1, 1, 0, 0, 0"),
                            (0x4001D21B, new string[]{ "HellSteed", }, "1"),
                            (0x4002452F, new string[]{ "Drake","Drake","Drake", }, "1"),
                            (0x40024540, new string[]{ "Wyvern", }, "1"),
                            (0x40024545, new string[]{ "wyvern", }, "1"),
                            (0x4014E9F1, new string[]{ "wyvern", }, "1"),
                            (0x4014E9F2, new string[]{ "orcishmage","orcishmage","orcishmage", }, "1"),
                            (0x4014E9F4, new string[]{ "Troll","Troll", }, "1"),
                            (0x4015D982, new string[]{ "orcishmage","orcishmage","orcishmage", }, "1"),
                            (0x4015D988, new string[]{ "troll","troll", }, "1"),
                            (0x4015D9AE, new string[]{ "drake", }, "1"),
                            (0x4015D9BB, new string[]{ "orcishmage","orcishmage","orcishmage", }, "1"),
                            (0x4015D9C0, new string[]{ "Troll", }, "1"),
                            (0x4015D9D1, new string[]{ "Troll","Troll","Troll", }, "1"),
                            (0x4015D9D2, new string[]{ "Ogre","Ogre","Ogre", }, "1"),
                            };

                    foreach (var obj in tupleList)
                    {
                        if (World.FindItem(obj.serial) is Spawner spawner)
                        {
                            if (spawner.ModeMulti || spawner.ModeNeruns)
                            {
                                spawner.ObjectNamesRaw = new ArrayList(obj.NameList);
                                spawner.Counts = obj.counts;
                                patched++;
                            }
                        }
                        else
                            missing++; // only one missing in my test, and I believe it was one of Boz's misconfigured spawners 
                    }
                }
                #endregion Re-add missing mobiles from ModeNeruns and ModeMulti spawners

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV160

        #region PatchV161

        private static int? PatchV161(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Set 'a wounded soldier' to not give rewards
                if (SiegeStyleShards())
                {
                    foreach (var m in World.Mobiles.Values)
                        if (m is BaseEscortable be && be.Name == "a wounded soldier")
                            be.GiveReward = false;
                }
                #endregion Set 'a wounded soldier' to not give rewards

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV161

        #region PatchV162

        private static int? PatchV162(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Establish all player reward hues
                if (AllShards())
                {
                    List<PlayerMobile> list = new();
                    foreach (var m in World.Mobiles.Values)
                        if (m is PlayerMobile pm && pm.AccessLevel == AccessLevel.Player && (pm.Account as Server.Accounting.Account).AccessLevel == AccessLevel.Player)
                            list.Add(pm);

                    // let the most recent players get the most unique hues
                    list.Sort((e1, e2) =>
                    {
                        return (e2.Account as Server.Accounting.Account).LastLogin.CompareTo((e1.Account as Server.Accounting.Account).LastLogin);
                    });

                    foreach (var pm in list)
                        pm.RewardHue = Utility.RewardHue(pm);
                }
                #endregion Establish all player reward hues

                #region 1 year Siege anniversary  - hued shroud
                if (Siege())
                {
                    LogHelper logger = new LogHelper("1 year Siege anniversary.log", overwrite: false, sline: true);
                    foreach (var m in World.Mobiles.Values)
                        if (m is PlayerMobile pm && pm.AccessLevel == AccessLevel.Player)
                            if (pm.BankBox != null)
                                try
                                {
                                    SPOneYearAnniversary shroud = new SPOneYearAnniversary(pm.RewardHue);
                                    if (!pm.BankBox.TryDropItem(World.GetAdminAcct(), shroud, sendFullMessage: false))
                                        pm.BankBox.AddItem(shroud);

                                    logger.Log(string.Format("{0} received {1}", pm, shroud));
                                }
                                catch { }

                    logger.Finish();
                }
                #endregion 1 year Siege anniversary  - hued shroud

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV162

        #region PatchV163

        private static int? PatchV163(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Fixup Motion Controllers
                if (AllShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is Server.Items.Triggers.MotionController mc)
                            if (mc.Target is Spawner spawner)
                                if (mc.TreatSpawnerAsItem)
                                    mc.TargetItem = spawner;
                                else
                                    mc.TargetSpawner = spawner;

                }
                #endregion Fixup Motion Controllers

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV163

        #region PatchV164

        private static int? PatchV164(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Reset GOT ChampPvMRankings
                if (Siege())
                {   // table deleted
                    //Engines.DataRecorder.DataRecorder.ChampPvMRankings.Clear();
                    patched++;
                }
                #endregion Reset GOT ChampPvMRankings

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV164

        #region PatchV165

        private static int? PatchV165(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Allow non ROT gains for any skills already GM'ed
                if (SiegeStyleShards())
                {
                    // Skill on Siege will be Return Over Time (ROT)
                    //  ROT has it's own 'wait' system
                    SkillGainSystem.GainWaits = false;
                    patched++;

                    foreach (Mobile m in World.Mobiles.Values)
                        if (m is PlayerMobile pm)
                        {
                            Server.Skills skills = pm.Skills;
                            for (int i = 0; i < skills.Length; ++i)
                                if (skills[i].Base >= 100.0)
                                {
                                    pm.GMHistoryTable |= SkillNameFlagsHelper.GetFlag(skills[i].SkillName);
                                    patched++;
                                }
                        }
                }
                #endregion Allow non ROT gains for any skills already GM'ed

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV165

        #region PatchV166

        private static int? PatchV166(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Force an update of township subsidies
                if (AllShards())
                {
                    foreach (var tss in TownshipStone.AllTownshipStones)
                        tss.RecordFameAndTaxBankInfo();
                }
                #endregion Force an update of township subsidies

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV166

        #region PatchV167

        private static int? PatchV167(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Unregister all Custom Region Controls that reside on the internal map
                if (AllShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is CustomRegionControl crc)
                            if (crc.Map == Map.Internal && crc.Registered == true)
                            {
                                crc.Registered = false;
                                patched++;
                            }
                }
                #endregion Unregister all Custom Region Controls that reside on the internal map

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV167

        #region PatchV168

        private static int? PatchV168(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Reset GOT ChampPvMRankings
                if (Siege())
                {   //table deleted
                    //Engines.DataRecorder.DataRecorder.ChampPvMRankings.Clear();
                    patched++;
                }
                #endregion Reset GOT ChampPvMRankings

                #region Enable Township benefits from Prison mini champs - Add Sterling rewards
                if (Siege())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is ChampEngine ce)
                        {
                            if (ce.DefinitionName == ChampNames.AI_Escape)
                            {
                                ce.BossSterlingMin = 1000;
                                ce.BossSterlingMax = 2500;

                                ce.SterlingMin = 40;
                                ce.SterlingMax = 85;
                                ce.KillDatabase = true;
                                ce.ClearMonsters = true;
                                patched++;
                            }
                            else if (ce.DefinitionName == ChampNames.AI_Guard)
                            {
                                ce.BossSterlingMin = 95;
                                ce.BossSterlingMax = 125;

                                ce.SterlingMin = 25;
                                ce.SterlingMax = 75;
                                ce.KillDatabase = true;
                                ce.ClearMonsters = true;
                                patched++;
                            }
                        }
                        else if (item is Spawner spawner && Region.Find(spawner.Location, spawner.Map) is Region region && region.Name == "Angel Island")
                        {
                            spawner.SterlingMin = 10;
                            spawner.SterlingMax = 18;
                            spawner.ScheduleRespawn = true;
                            patched++;
                        }
                    ;
                }
                #endregion Enable Township benefits from Prison mini champs - Add Sterling rewards

                #region Switch Pirate_Mini for Pirate_Micro
                if (Siege())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is ChampEngine ce && ce.DefinitionName == ChampNames.Pirate_Mini)
                        {
                            ce.DefinitionName = ChampNames.Pirate_Micro;
                            patched++;
                        }
                }
                #endregion Switch Pirate_Mini for Pirate_Micro

                #region Spawner Beautifier
                if (AllShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && spawner.ObjectNamesRaw != null)
                        {
                            ArrayList objectNames = new(spawner.ObjectNamesRaw);
                            ArrayList objectNamesFixed = new ArrayList();

                            for (int i = 0; i < objectNames.Count; i++)
                            {
                                string str = objectNames[i] as string;

                                if (true)
                                {
                                    if (str.Length > 0)
                                    {
                                        str = str.Trim();
                                        //adam 8/29/22, we now allow ':' delimited lists of creatures
                                        string[] tokens = str.Split(':', StringSplitOptions.RemoveEmptyEntries);
                                        List<string> valid = new List<string>();
                                        for (int ix = 0; ix < tokens.Length; ix++)
                                        {
                                            string token = tokens[ix];

                                            Type type = SpawnerType.GetType(token);

                                            if (type == null)
                                            {
                                                //from.SendMessage("{0} is not a valid type name.", token);
                                                tokens[ix] = string.Empty;
                                            }
                                            else
                                            {
                                                valid.Add(type.Name);   // 7/3/2024, Adam: beautifier
                                                patched++;
                                            }
                                        }
                                        // rebuild the entry
                                        str = string.Empty;
                                        foreach (string obj in valid)
                                            str += obj + ':';
                                        str = str.TrimEnd(':');
                                        // add it
                                        objectNamesFixed.Add(str);
                                    }
                                }
                            }
                            spawner.ObjectNamesRaw = new ArrayList(objectNamesFixed);
                        }
                }
                #endregion Spawner Beautifier

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV168

        #region PatchV169

        private static int? PatchV169(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Replace Island  Serpents with Gila Monsters
                if (Siege())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner)
                            if (spawner.Spawns("IslandSerpentModerate"))
                            {
                                spawner.ObjectNamesRaw = new ArrayList() { "GilaMonsterModerate" };
                                spawner.SetProp = null;
                                spawner.SterlingMin = 15;
                                spawner.SterlingMax = 45;
                                spawner.ScheduleRespawn = true;
                                patched++;
                            }
                            else if (spawner.Spawns("IslandSerpentStrong"))
                            {
                                spawner.ObjectNamesRaw = new ArrayList() { "GilaMonsterStrong" };
                                spawner.SetProp = null;
                                spawner.SterlingMin = 25;
                                spawner.SterlingMax = 75;
                                spawner.ScheduleRespawn = true;
                                patched++;
                            }
                }
                #endregion Replace Island  Serpents with Gila Monsters

                #region Remove sterling from animal spawners
                if (Siege())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner && Region.Find(spawner.Location, spawner.Map) is Region region && region.Name == "Angel Island")
                        {
                            foreach (object o in spawner.Objects)
                            {
                                if (o is BaseCreature bc && bc.Body.IsAnimal)
                                {
                                    spawner.SterlingMin = 0;
                                    spawner.SterlingMax = 0;
                                    spawner.ScheduleRespawn = true;
                                    patched++;
                                    break;
                                }
                            }
                        }
                    ;
                }
                #endregion Remove sterling from animal spawners

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV169

        #region PatchV170

        private static int? PatchV170(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Initialize pirate naming by resetting the champ engines
                if (Siege())
                {
                    foreach (ChampEngine item in ChampEngine.Instances)
                        if (item is ChampEngine ce)
                        {
                            ce.DefinitionName = ce.DefinitionName;
                            patched++;
                        }
                }
                #endregion Initialize pirate naming by resetting the champ engines

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV170

        #region PatchV171

        private static int? PatchV171(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Patch fire pits
                if (AllShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item != null && (item.ItemID == 0x0FAC || item.ItemID == 0x0DE3))
                        {
                            item.Light = LightType.Circle225;
                            patched++;
                        }
                }
                #endregion Patch fire pits

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV171

        #region PatchV172

        private static int? PatchV172(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Remove all Staff Accounts
                if (AllShards())
                {
                    Nuke.RemoveAllStaff(new CommandEventArgs(World.GetSystemAcct(),
                        command: "RemoveAllStaff",
                        argString: string.Empty,
                        arguments: new string[] { string.Empty }));
                }
                #endregion Remove all Staff Accounts

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV172

        #region PatchV173

        private static int? PatchV173(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Despawn in prep for Tribute Shard
                if (EventShards())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is Spawner spawner)
                            if (spawner.Running == true)
                            {   // disable
                                spawner.ScheduleDespawn = true;
                                spawner.Running = false;
                                patched++;
                            }
                }
                #endregion Despawn in prep for Tribute Shard

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV173

        #region PatchV174

        private static int? PatchV174(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Make sure FreezeAccountDecay is off for all shards
                if (AllShards())
                {
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.FreezeAccountDecay);
                    patched++;
                }
                #endregion Make sure FreezeAccountDecay is off for all shards

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV174

        #region PatchV175

        private static int? PatchV175(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Make sure FreezeHouseDecay && FreezeTownshipDecay is off for all shards
                if (AllShards())
                {
                    if (!Mortalis())
                    {
                        CoreAI.SetDynamicFeature(CoreAI.FeatureBits.FreezeHouseDecay);
                        CoreAI.SetDynamicFeature(CoreAI.FeatureBits.FreezeTownshipDecay);
                        patched += 2;
                    }
                }
                #endregion Make sure FreezeHouseDecay && FreezeTownshipDecay is off for all shards

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV175

        #region PatchV176

        private static int? PatchV176(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Recalc house deed prices for deeds on PlayerVendors (custom housing area)
                foreach (Mobile m in World.Mobiles.Values)
                    if (m is PlayerVendor pv && pv.IsStaffOwned)
                        patched += pv.RefreshDefaultPrices();
                #endregion Recalc house deed prices for deeds on PlayerVendors (custom housing area)

                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV176

        #region PatchV177

        private static int? PatchV177(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Enable pet traversal for Moonglow teleporters
                foreach (Item item in World.Items.Values)
                {
                    if (item is Teleporter teleporter)
                    {
                        Region region = Region.Find(teleporter.Location, teleporter.Map);

                        if (region != null && region.Name == "Moonglow")
                        {
                            if (!teleporter.Pets)
                            {
                                teleporter.Pets = true;
                                patched++;
                            }
                        }
                    }
                }
                #endregion Enable pet traversal for Moonglow teleporters



                #endregion End Implementation
                return patched;
            }
            return null;
        }

        #endregion PatchV177

        #region PatchV178

        private static int? PatchV178(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Set BaseHouse OwnerGUID to Owner.GUID (live owner → house)

                foreach (Item item in World.Items.Values)
                {
                    if (item is BaseHouse bh && !bh.Deleted)
                    {
                        if (bh.Owner is PlayerMobile pm && !pm.Deleted)
                        {
                            // Direct house ownership: bind the house to the player’s identity GUID.
                            if (bh.OwnerGUID == 0)
                                bh.OwnerGUID = pm.AcctGUID;

                            patched++;
                        }
                    }
                }

                #endregion Set BaseHouse OwnerGUID to Owner.GUID (live owner → house)

                #region Migrate Account.House / Account.BankBox to GUID-based ownership

                // Some houses and bankboxes are tied to the account identity rather than a live
                // PlayerMobile (e.g., inheritance, pre-reincarnation state). These were historically
                // stored as Serials on the Account (House, BankBox). We now migrate those to the
                // GUID/OwnerGUID model by:
                //   1) Deriving the account GUID via Utility.MakeGuid(Username, Username)
                //   2) Finding the referenced items in the world
                //   3) Setting their OwnerGUID to the account GUID

                foreach (Server.Accounting.Account acct in Accounts.Table.Values)
                {
                    if (acct == null)
                        continue;

                    // Stable identity for this account; matches the model used elsewhere:
                    //   Player.GUID and Account GUID are derived consistently from username.
                    uint guid = Utility.MakeGuid(acct.Username, acct.Username);

                    if (guid == 0)
                        continue;

                    // ----- House inheritance / account-bound house -----
                    if (acct.House != Serial.MinusOne)
                    {
                        if (World.FindItem(acct.House) is BaseHouse bh && !bh.Deleted)
                        {
                            // Only assign if not already bound; avoid clobbering any data already
                            // migrated via the live-owner pass above.
                            if (bh.OwnerGUID == 0)
                            {
                                bh.OwnerGUID = guid;
                                patched++;
                            }
                        }
                        else
                        {
                            // Optional: clear invalid legacy reference
                            // acct.House = Serial.MinusOne;
                        }
                    }

                    // ----- Bankbox inheritance / account-bound strongbox -----
                    if (acct.BankBox != Serial.MinusOne)
                    {
                        if (World.FindItem(acct.BankBox) is StrongBackpack box && !box.Deleted)
                        {
                            // StrongBackpack (bank box) inherits OwnerGUID from Item.
                            if (box.OwnerGUID == 0)
                            {
                                box.OwnerGUID = guid;
                                patched++;
                            }
                        }
                        else
                        {
                            // Optional: clear invalid legacy reference
                            // acct.BankBox = Serial.MinusOne;
                        }
                    }
                }

                #endregion Migrate Account.House / Account.BankBox to GUID-based ownership


                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV178

        #region PatchV179

        private static int? PatchV179(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Reset all players with multiple MurderousOffenses to 1

                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m == null || m.Deleted)
                        continue;

                    PlayerMobile pm = m as PlayerMobile;
                    if (pm == null)
                        continue;

                    Account acct = pm.Account as Account;
                    if (acct == null)
                        continue;
                    // persisted across lives
                    if (Core.RuleSets.MortalisRules())
                        if (acct.HarmfulOffenses > 1)
                        {
                            acct.HarmfulOffenses = 1;
                            patched++;
                        }
                }

                #endregion Reset all players with multiple MurderousOffenses to 1

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV179

        #region PatchV180

        private static int? PatchV180(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Adjust stat gain factor for Mortalis

                if (Mortalis())
                {
                    CoreAI.StatGainFactor = 2.0;
                    patched++;
                }

                #endregion Adjust stat gain factor for Mortalis

                #region Remove self from Perma Flags
                foreach (Mobile m in World.Mobiles.Values)
                {
                    PlayerMobile pm = m as PlayerMobile;
                    if (pm == null || pm.Deleted)
                        continue;

                    if (pm.PermaFlags != null && pm.ContainsPermaFlag(pm))
                    {
                        pm.RemovePermaFlag(pm);
                        pm.Delta(MobileDelta.Noto);
                        patched++;
                    }
                }
                #endregion Remove self from Perma Flags

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV180

        #region PatchV181

        private static int? PatchV181(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Force all shards to GoldRewardModel.GoldCraftValue
                BulkOrderSystem.GoldRewardModel = GoldRewardModel.GoldCraftValue;
                BulkOrderSystem.GlobalGoldScalar = 2.333;
                patched++;
                #endregion Force all shards to GoldRewardModel.GoldCraftValue

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV181

        #region PatchV182

        private static int? PatchV182(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Bring farmer's dogs home when all players have left their region
                foreach (Item item in World.Items.Values)
                    if (item is Spawner spawner)
                        if (spawner.Spawns("FarmDog"))
                        {
                            spawner.AutoHome = true;    // go home when all players have left the building
                            spawner.GuardIgnore = true; // guards should not be whacking these guys
                            patched++;
                        }

                #endregion Bring farmer's dogs home when all players have left their region

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV182

        #region PatchV183

        private static int? PatchV183(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                // Phase I: Persistent GUID identity linking
                // Purpose:
                // - Establish account-rooted persistent identity
                // - Bind PlayerMobile identity to account identity
                // - Embed portable identity metadata into assets where needed
                //
                // Notes:
                // - Shard-local serials are NOT removed or replaced
                // - This adds a persistent identity layer used for:
                //   * cross-shard asset migration
                //   * attribution relinking
                //   * ownership continuity
                //   * Mortalis deletion model support

                #region Phase I: Persistent GUID Identity Linking

                // Normalize account GUIDs (canonical identity anchor)
                foreach (Account acct in Accounts.Table.Values)
                {
                    if (acct == null)
                        continue;

                    // Deterministic GUID generation ensures stability across shards
                    uint guid = Utility.MakeGuid(acct.Username, acct.Username);
                    acct.GUID = guid;
                    patched++;
                }

                // Bind PlayerMobile GUIDs to owning account identity
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is not PlayerMobile pm)
                        continue;

                    if (pm.Deleted)
                        continue;

                    Account acct = pm.Account as Account;
                    if (acct == null)
                        continue;

                    // PlayerMobile identity is a projection of account identity
                    pm.AcctGUID = acct.GUID;
                    patched++;
                }

                // Embed portable identity into authored assets (RSM ownership metadata)
                int skipped = 0;
                int assigned = 0;
                foreach (Item item in World.Items.Values)
                {
                    if (item == null || item.Deleted)
                        continue;

                    if (item is not RolledUpSheetMusic rsm)
                        continue;

                    // Must have a concrete PlayerMobile owner to backfill identity
                    if (rsm.Owner is not PlayerMobile pm)
                    {
                        skipped++;
                        continue;
                    }

                    // Store portable identity for cross-shard relinking and attribution recovery
                    rsm.OwnerGUID = pm.AcctGUID;

                    assigned++;
                    patched++;
                }

                #region SP Phase I: Backfill MusicInfo.OwnerGUID from RSM.OwnerGUID
                if (Siege())
                {
                    int scanned = 0;
                    int patchedMI = 0;
                    int skippedMI = 0;
                    int unresolved = 0;

                    foreach (KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo> kvp in MusicLibrary.Repository)
                    {
                        RolledUpSheetMusic rsm = kvp.Key;
                        MusicLibrary.MusicInfo mi = kvp.Value;

                        if (rsm == null || mi == null)
                            continue;

                        if (rsm.Deleted)
                            continue;

                        scanned++;

                        // If already populated, do not stomp.
                        if (mi.OwnerGUID != 0)
                        {
                            skippedMI++;
                            continue;
                        }

                        // Prefer the asset-carried GUID: it survives owner resolution failures and migrations.
                        if (rsm.OwnerGUID == 0)
                        {
                            unresolved++;
                            continue;
                        }

                        mi.OwnerGUID = rsm.OwnerGUID;
                        patchedMI++;
                        patched++;
                    }

                    Console.WriteLine(
                        $"SP MusicInfo OwnerGUID backfill: scanned={scanned}, patched={patchedMI}, skipped(already set)={skippedMI}, unresolved(no RSM guid)={unresolved}");
                }
                #endregion



                #endregion Phase I: Persistent GUID Identity Linking

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV183

        #region PatchV184

        private static int? PatchV184(int patchid)
        {
            if (Mortalis() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Mortalis: Relink music ownership from MusicInfo.OwnerGUID (database-driven)

                int scanned = 0;
                int linked = 0;
                int skipped = 0;
                int unresolved = 0;

                // Database is authoritative now. Do NOT depend on RSM.OwnerGUID here.
                foreach (KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo> kvp in MusicLibrary.Repository)
                {
                    RolledUpSheetMusic rsm = kvp.Key;
                    MusicLibrary.MusicInfo mi = kvp.Value;

                    if (rsm == null || mi == null)
                        continue;

                    if (rsm.Deleted)
                        continue;

                    scanned++;

                    // Nothing to relink if the database does not carry identity metadata
                    if (mi.OwnerGUID == 0)
                    {
                        skipped++;
                        continue;
                    }

                    // Resolve the owning player via Account.GUID -> first PlayerMobile slot
                    PlayerMobile pm = ResolveFirstPlayerByAccountGuid(mi.OwnerGUID);

                    if (pm == null)
                    {
                        unresolved++;
                        continue;
                    }

                    // Sanity check: persistent identity must match resolved runtime identity
                    if (mi.OwnerGUID != pm.AcctGUID)
                    {
                        Console.WriteLine(
                            $"[MUSIC RELINK ERROR] GUID mismatch: DB={mi.OwnerGUID}, PM={pm.AcctGUID}, Player={pm.Name}");
                    }

                    // Bind runtime owner (reasserts identity invariant)
                    mi.Owner = pm;


                    // This will ALSO initialize rsm.OwnerGUID via the Owner property setter.
                    rsm.Owner = pm;

                    linked++;
                    patched++;
                }

                Console.WriteLine(
                    $"Mortalis music relink (db-driven): scanned={scanned}, linked={linked}, skipped(no db guid)={skipped}, unresolved(no account/player)={unresolved}");

                #endregion Mortalis: Relink music ownership from MusicInfo.OwnerGUID (database-driven)

                #region Mortalis: Backfill wild RSM OwnerGUID from music database (skip library masters)

                int dbScanned = 0;
                int dbMissingOwner = 0;
                int dbHashCollisions = 0;

                scanned = 0;
                int skippedLibrary = 0;
                int skippedAlreadySet = 0;
                int patchedOwnerGuid = 0;
                int patchedOwnerMobile = 0;
                int unresolvedNoDbMatch = 0;

                // Build DB lookup: HashCode -> OwnerGUID (authoritative)
                Dictionary<int, uint> ownerByHash = new Dictionary<int, uint>();

                foreach (KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo> kvp in MusicLibrary.Repository)
                {
                    RolledUpSheetMusic libRsm = kvp.Key;
                    MusicLibrary.MusicInfo mi = kvp.Value;

                    if (libRsm == null || mi == null)
                        continue;

                    dbScanned++;

                    if (mi.OwnerGUID == 0)
                    {
                        dbMissingOwner++;
                        continue;
                    }

                    int hash = libRsm.HashCode;

                    if (ownerByHash.TryGetValue(hash, out uint existing) && existing != mi.OwnerGUID)
                    {
                        // Same hash maps to different owners: ambiguous
                        dbHashCollisions++;
                        continue;
                    }

                    ownerByHash[hash] = mi.OwnerGUID;
                }

                // Patch world RSM (excluding library masters)
                foreach (Item item in World.Items.Values)
                {
                    if (item == null || item.Deleted)
                        continue;

                    if (item is not RolledUpSheetMusic rsm)
                        continue;

                    scanned++;

                    // Skip the library master objects (dictionary keys)
                    if (MusicLibrary.Repository.ContainsKey(rsm))
                    {
                        skippedLibrary++;
                        continue;
                    }

                    // If already set, leave it alone
                    if (rsm.OwnerGUID != 0)
                    {
                        skippedAlreadySet++;
                        continue;
                    }

                    if (!ownerByHash.TryGetValue(rsm.HashCode, out uint ownerGuid) || ownerGuid == 0)
                    {
                        unresolvedNoDbMatch++;
                        continue;
                    }

                    // Backfill portable identity
                    rsm.OwnerGUID = ownerGuid;
                    patchedOwnerGuid++;

                    // Optional runtime owner binding (never clear OwnerGUID if this fails)
                    PlayerMobile pm = ResolveFirstPlayerByAccountGuid(ownerGuid);
                    if (pm != null)
                    {
                        rsm.Owner = pm;
                        patchedOwnerMobile++;
                    }
                }

                Console.WriteLine(
                    $"Mortalis wild RSM backfill: dbScanned={dbScanned}, dbMissingOwner={dbMissingOwner}, dbHashCollisions={dbHashCollisions}");
                Console.WriteLine(
                    $"Mortalis wild RSM backfill: scanned={scanned}, skippedLibrary={skippedLibrary}, skippedAlreadySet={skippedAlreadySet}, patchedOwnerGuid={patchedOwnerGuid}, patchedOwnerMobile={patchedOwnerMobile}, unresolvedNoDbMatch={unresolvedNoDbMatch}");

                #endregion Mortalis: Backfill wild RSM OwnerGUID from music database (skip library masters)


                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV184

        #region PatchV185

        private static int? PatchV185(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Patch all travel objects

                foreach (Item item in World.Items.Values)
                {
                    if (item == null)
                        continue;

                    if (item is PublicMoongate ||
                        item is Moongate ||
                        item is Sungate ||
                        item is Teleporter ||
                        item is HouseTeleporter)
                    {
                        if (!item.TravelObject)
                        {
                            item.TravelObject = true;
                            patched++;
                        }
                    }
                }
                #endregion Patch all travel objects

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV185

        #region PatchV186

        private static int? PatchV186(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Kill all AngryMiners

                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m == null || m.Deleted)
                        continue;

                    if (m is AngryMiner)
                    {
                        // Choose ONE of these depending on intent:

                        // proper death handling (corpse, OnDeath, etc.)
                        m.Kill();

                        patched++;
                    }
                }

                #endregion Kill all AngryMiners

                #region Repair and make indestructible all launch reward clothes

                if (Core.RuleSets.MortalisStyleRules())
                {
                    foreach (Item item in World.Items.Values)
                    {
                        if (item is BaseClothing bc)
                            if (bc.LootType == LootType.Blessed && bc.Hue != 0)
                            {
                                bc.Reward = true;
                                bc.HitPoints = 1024;
                                patched++;
                            }
                    }
                }

                #endregion Repair and make indestructible all launch reward clothes

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV186

        #region PatchV187

        private static int? PatchV187(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Patch doors for small marble workshop

                foreach (Item item in World.Items.Values)
                {
                    if (item is not BaseDoor door)
                        continue;

                    if (door.Facing != DoorFacing.SouthCW)
                        continue;

                    if (door.Z != 27)
                        continue;

                    Region r = Region.Find(door.Location, door.Map);
                    if (r is not HouseRegion hr)
                        continue;

                    // HouseRegion.House is BaseHouse; we need StaticHouse for blueprint ID
                    if (hr.House is not Server.Multis.StaticHousing.StaticHouse sh)
                        continue;

                    if (!string.Equals(sh.HouseBlueprintID, "HIDC35B4", StringComparison.OrdinalIgnoreCase))
                        continue;

                    door.Facing = DoorFacing.SouthCCW;
                    patched++;
                }

                Console.WriteLine("Patched {0} doors for StaticHouse blueprint HIDC35B4 (SouthCW -> SouthCCW).", patched);

                #endregion Patch doors for small marble workshop

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV187

        #region PatchV188

        private static int? PatchV188(int patchid)
        {
            if (Mortalis() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Distribute a song to each player

                RolledUpSheetMusic template = null;
                object info = null;

                string song = ". . I'm still Standing)";

                if (MusicBox.FindSong(song.Split(), out info))
                {
                    template = ((KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo>)info).Key;
                }

                if (template != null && !template.Deleted)
                {
                    int given = 0;
                    int backpack = 0;
                    int bank = 0;
                    int failed = 0;

                    foreach (Mobile m in World.Mobiles.Values)
                    {
                        if (m == null || m.Deleted)
                            continue;

                        if (m is not PlayerMobile pm)
                            continue;

                        // Create a per-player copy
                        RolledUpSheetMusic tune = Utility.Dupe(template) as RolledUpSheetMusic;
                        if (tune == null)
                        {
                            failed++;
                            continue;
                        }

                        tune.LootType = LootType.Blessed;
                        tune.Hue = pm.RewardHue;

                        bool placed = false;

                        // 1) Try backpack
                        Container pack = pm.Backpack;
                        if (pack != null && pack.TryDropItem(pm, tune, sendFullMessage: false))
                        {
                            placed = true;
                            backpack++;
                            patched++;
                        }
                        else
                        {
                            // 2) Try bank box
                            BankBox box = pm.BankBox;
                            if (box != null && box.TryDropItem(pm, tune, sendFullMessage: false))
                            {
                                placed = true;
                                bank++;
                                patched++;
                            }
                        }

                        // 3) Give up
                        if (!placed)
                        {
                            tune.Delete();
                            failed++;
                        }
                        else
                        {
                            given++;
                        }
                    }

                    Console.WriteLine($"Song Distribution: '{song}' | given={given}, backpack={backpack}, bank={bank}, failed={failed}");
                }
                else
                {
                    Console.WriteLine($"Song Distribution: template not found for '{song}'");
                }

                #endregion Distribute a song to each player


                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV188

        #region PatchV189

        private static int? PatchV189(int patchid)
        {
            if (Mortalis() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region patch BaseGranite
                foreach (Item item in World.Items.Values)
                {
                    if (item is BaseGranite baseGranite)
                    {
                        baseGranite.Hue = CraftResources.GetHue(baseGranite.Resource);
                        patched++;
                    }
                }
                #endregion patch BaseGranite

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV189

        #region PatchV190

        private static int? PatchV190(int patchid)
        {
            if (AngelIsland() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Reset GlobalGoldScalar to 1 for Angel Island only
                BulkOrderSystem.GlobalGoldScalar = 1.0;
                patched++;
                #endregion Reset GlobalGoldScalar to 1 for Angel Island only

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV190

        #region PatchV191

        private static int? PatchV191(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Init region defaults
                foreach (Region region in Region.Regions)
                {
                    if (region is StaticRegion sRegion)
                    {
                        sRegion.AllowBeneficialActions = true;
                        sRegion.AllowHarmfulActions = true;
                        patched++;
                    }
                }
                #endregion Init region defaults

                #region Configure Custom Housing Area to allow reds
                Region rx = Region.FindByUId(uid: 25);
                if (rx is StaticRegion sr)
                {
                    sr.Name = "Custom Housing Area";
                    sr.AllowReds = true;
                    sr.AllowBeneficialActions = false;
                    sr.AllowHarmfulActions = false;
                }
                patched++;
                #endregion Configure Custom Housing Area to allow reds

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV191

        #region PatchV192

        private static int? PatchV192(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Init criminality defaults
                CoreAI.CriminalGrayMinutes = 2;      // 2 minutes
                CoreAI.CriminalWhackSeconds = 10;    // 10 seconds
                CoreAI.CriminalTravelMinutes = 2;    // 2 minutes

                CoreAI.HarmfulGrayMinutes = 10;    // 10 minutes
                CoreAI.HarmfulWhackSeconds = 10;   // 10 seconds
                CoreAI.HarmfulTravelMinutes = 2;   // 2 minutes

                CoreAI.AFKGrayMinutes = 240;         // 4 hours
                CoreAI.AFKWhackSeconds = 10;         // 10 seconds
                CoreAI.AFKTravelMinutes = 2;         // 2 minutes

                CoreAI.ThievingGrayMinutes = 10;     // 10 minutes
                CoreAI.ThievingWhackSeconds = 10;    // 10 seconds
                CoreAI.ThievingTravelMinutes = 2;    // 2 minutes
                patched++;
                #endregion Init criminality defaults

                #region Configure Seasons

                if (Seasons())
                {
                    // enable passive taming
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.PassiveTamingEnabled);
                    patched++;

                    // config allowed accounts/connections (Angel Island style)
                    CoreAI.MaxAccountsPerIP = 3; // how many accounts can be created/accessed per IP Address
                    CoreAI.MaxAccountsPerMachine = 3; // in HardwareLimiter; controls how many accounts may be accessed from the same machine
                    CoreAI.MaxConcurrentAddresses = 3; // in IPLimiter; controls how many of the same IP address can be concurrently logged in. Maybe > MaxAccountsPerIP
                    patched += 3;
                }

                #endregion Configure Seasons

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV192

        #region PatchV193

        private static int? PatchV193(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Make template mobs staff owned

                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m == null || m.Deleted)
                        continue;
                    if (m.SpawnerTempMob)
                    {
                        m.IsStaffOwned = true;
                        patched++;
                    }
                }

                #endregion Make template mobs staff owned

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV193

        #region PatchV194

        private static int? PatchV194(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Force all shards to GoldRewardModel.GoldCraftValue and no GlobalGoldScalar
                BulkOrderSystem.GoldRewardModel = GoldRewardModel.GoldCraftValue;
                BulkOrderSystem.GlobalGoldScalar = 1.0;
                patched++;
                #endregion Force all shards to GoldRewardModel.GoldCraftValue and no GlobalGoldScalar

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV194

        #region PatchV195

        private static int? PatchV195(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Respawn all running pirate champs

                foreach (Item item in World.Items.Values)
                {
                    if (item is not ChampEngine ce)
                        continue;

                    if (!ce.Running)
                        continue;

                    // Skip if this is not a Pirate champion spawn (case-insensitive check)
                    if (!ce.DefinitionName.Contains("pirate", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // force regeneration of this level
                    ce.Level = ce.Level;

                    patched++;
                }
                #endregion Respawn all running pirate champs

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV195

        #region PatchV196

        private static int? PatchV196(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Update both HarmfulPvPGrayMinutes and HarmfulGrayMinutes
                CoreAI.HarmfulGrayMinutes = 2;      // normal blue NPC
                CoreAI.HarmfulPvPGrayMinutes = 10;  // player against another player - criminal
                #endregion Update both HarmfulPvPGrayMinutes and HarmfulGrayMinutes

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV196

        #region PatchV197

        private static int? PatchV197(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Respawn world to force regeneration of corrected gold

                var result = TotalRespawnCommand.TotalRespawn(null);
                patched = result.TotalCount;

                #endregion Respawn world to force regeneration of corrected gold

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV197

        #region PatchV198

        private static int? PatchV198(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Delete the double door in Minoc

                BaseDoor bd = World.FindItem(0x4000BE9C) as BaseDoor;
                if (bd != null)
                {
                    bd.Delete();
                    patched++;
                }

                #endregion Delete the double door in Minoc

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV198

        #region PatchV199

        private static int? PatchV199(int patchid)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region First Cleanup Felucca
                foreach (Item item in World.Items.Values)
                    if (item is CustomRegionControl crc)
                        if (crc.CustomRegion is StaticRegion sr && sr.Name == "Custom Region")
                        {
                            crc.Registered = false;
                            crc.Delete();
                            patched++;
                        }

                foreach (Region region in Region.Regions)
                {
                    if (region is StaticRegion sRegion)
                    {
                        if (sRegion.Name == "Custom Region")
                        {
                            sRegion.Registered = false;
                            patched++;
                        }
                    }
                }
                #endregion First Cleanup Felucca

                #region Create the per-shard Test Center
                // before the actual move, update the signage from "Dueling Arena" to Test Center
                Item sign = World.FindItem(0x4004118A);
                if (sign != null)
                    sign.Name = "Test Center";

                // mirror the felucca world onto Trammel (Test Center)
                patched += MirrorFelucca.MirrorToTrammel(new CommandEventArgs(null, null, null, null));

                // drop a test center stone (giving stone) at WBB (1426, 1693, 0)
                var tcs = new TestCenterStone();
                tcs.MoveToWorld(new Point3D(1426, 1693, 0), Map.TestCenter);

                // configure teleporters to test center
                // There are existing teleporters there, we will delete them and replace them with our Test Center teleporter
                List<Point3D> list = new List<Point3D>() { new Point3D(1173, 3625, 5), new Point3D(1172, 3625, 5) };
                {
                    // Felucca side -> Trammel
                    foreach (var px in list)
                    {
                        List<Item> items = FindItemsAt(px, Map.Felucca, 2);
                        foreach (Item ix in items)
                            if (ix is Teleporter tp)
                            {
                                // delete the old one
                                tp.Delete();
                                patched++;
                                // install the new one
                                var tct = new TestCenterTeleporter();
                                tct.MoveToWorld(px, Map.Felucca);
                                tct.Mode = TestCenterTeleporter.TestCenterMode.To;
                                tct.MapDest = Map.Trammel;
                                tct.PointDest = new Point3D(1420, 1698, 0); // WBB Trammel
                                tct.NoGhosts = true;
                                patched++;
                            }
                    }
                }

                {   // Trammel side -> Felucca
                    foreach (var px in list)
                    {
                        List<Item> items = FindItemsAt(px, Map.Trammel, 2);
                        foreach (Item ix in items)
                            if (ix is Teleporter tp)
                            {
                                // delete the old one
                                tp.Delete();
                                patched++;
                                // install the new one
                                var tct = new TestCenterTeleporter();
                                tct.MoveToWorld(px, Map.Trammel);
                                tct.Mode = TestCenterTeleporter.TestCenterMode.From;
                                tct.MapDest = Map.Felucca;
                                tct.PointDest = new Point3D(1171, 3627, 0); // Near the teleporter in Felucca they left from
                                tct.NoGhosts = true;
                                patched++;
                            }
                    }
                }

                // finally, update the newly created sign in Trammel that says "Test Center" to say "Felucca"
                if (sign != null)
                {
                    List<Item> items = FindItemsAt(sign.Location, Map.Trammel, 2);
                    foreach (Item ix in items)
                        if (ix is Item item)
                            if (item.Name == "Test Center")
                            {
                                item.Name = "Felucca";
                                break;
                            }
                }

                #region New Player Starting Area

                const string regionName = "New Player Starting Zone";

                XmlElement elem = FindStaticRegionTemplate(regionName);

                if (elem == null)
                {
                    Console.WriteLine("Static region patch failed: template region not found.");
                }
                else if (StaticRegion.XmlDatabase.Any(r => r.Name == regionName))
                {
                    Console.WriteLine("Static region patch skipped: \"{0}\" already exists.", regionName);

                    var region = StaticRegion.XmlDatabase
                        .FirstOrDefault(r => string.Equals(r.Name, regionName, StringComparison.OrdinalIgnoreCase));

                    if (region != null)
                    {
                        if (!region.Registered)
                        {
                            region.Registered = true;
                            Console.WriteLine("Static region patch: \"{0}\" re-registered.", regionName);
                        }
                        else
                        {
                            Console.WriteLine("Static region patch: \"{0}\" already registered.", regionName);
                        }
                    }
                }
                else
                {
                    StaticRegion region = new StaticRegion();

                    region.Load(elem);

                    region.ShardConfig = ShardConfig.Core;
                    region.Registered = true;

                    StaticRegion.XmlDatabase.Add(region);

                    Console.WriteLine("Static region patch applied: \"{0}\" added as Core region.", regionName);
                }

                #endregion New Player Starting Area

                #endregion Create the per-shard Test Center

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV199

        #region PatchV200

        private static int? PatchV200(int patchid)
        {
            if (Upheaval() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Initialize Upheaval

                CoreAI.MaxAccountsPerIP = 1;
                CoreAI.MaxAccountsPerMachine = 1;
                CoreAI.MaxConcurrentAddresses = 1;
                CoreAI.PostTeleportMoveLockMs = 1;
                CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerGuild);
                CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);

                #endregion Initialize Upheaval

                #endregion End Implementation

                return patched;
            }

            return null;
        }

        #endregion PatchV200

        #region Helpers
        #region PatchV123
        public static bool IsMagicLoot(Item item)
        {
            if (EnchantedScroll.IsValidType(item))
            {
                if (item is BaseWeapon bw && item is not BaseWand)
                {
                    if (bw.AccuracyLevel > WeaponAccuracyLevel.Regular || bw.DamageLevel > WeaponDamageLevel.Regular || bw.DurabilityLevel > WeaponDurabilityLevel.Regular)
                        return true;

                    if (bw.MagicCharges > 0 || bw.Slayer != SlayerName.None)
                        return true;
                }
                else if (item is BaseArmor ba)
                {
                    if (ba.ProtectionLevel > ArmorProtectionLevel.Regular || ba.DurabilityLevel > ArmorDurabilityLevel.Regular)
                        return true;

                    if (ba.MagicCharges > 0)
                        return true;
                }
                else if (item is BaseJewel bj)
                {
                    if (bj.MagicCharges > 0)
                        return true;
                }
                else if (item is BaseWand bwnd)
                {
                    if (bwnd.MagicCharges > 0)
                        return true;
                }
                else if (item is BaseClothing bc)
                {
                    if (bc.MagicCharges > 0)
                        return true;
                }
            }
            return false;
        }
        #endregion PatchV123
        #region PatchV131
        public class WHCfg
        {
            public Type m_type;
            public int m_int;
            public string m_name;
            public int m_control_master;
            public double m_magic_resist;
            public double m_tactics;
            public double m_wrestling;
            public WHCfg(Type type, int @int, string name, int master, double magic_resist, double tactics, double wrestling)
            {
                m_type = type;
                m_int = @int;
                m_name = name;
                m_control_master = master;
                m_magic_resist = magic_resist;
                m_tactics = tactics;
                m_wrestling = wrestling;
            }
        }
        #endregion PatchV131
        #region PatchV199
        private static XmlElement FindStaticRegionTemplate(string regionName)
        {
            string filePath = Path.Combine(Core.DataDirectory, "staticRegions.xml");

            if (!File.Exists(filePath))
                return null;

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement root = doc["StaticRegions"];

            if (root == null)
                return null;

            foreach (XmlElement elem in root.GetElementsByTagName("region"))
            {
                if (elem.GetAttribute("type") != "StaticRegion")
                    continue;

                if (elem.GetAttribute("name") == regionName)
                    return elem;
            }

            return null;
        }
        #endregion PatchV199
        #region PatchV152
        private static Item ChestLoot(int level)
        {
            Loot.ImbueLevel imbueLevel = Loot.TreasureMapLevelToImbueLevel(level);
            int[] minmax = Loot.ImbueLevelToMagicEnchantment(imbueLevel);
            Item item = null;

            if (Utility.Chance(.6))
            {
                item = Loot.RandomWeapon();
                if (item != null)
                {
                    // give the best loot with possible upgrade and hueing
                    item = Loot.ImbueWeaponOrArmor(noThrottle: true, item, Loot.ImbueLevel.Level5, upgrade_chance: 1, hueing: true);
                    (item as BaseWeapon).SetRandomMagicEffect(minmax[0], minmax[1]);
                }
            }
            else if (Utility.Chance(.4))
            {
                item = Loot.RandomArmorOrShield();
                if (item != null)
                {
                    // give the best loot with possible upgrade and hueing
                    item = Loot.ImbueWeaponOrArmor(noThrottle: true, item, Loot.ImbueLevel.Level5, upgrade_chance: 1, hueing: true);
                    (item as BaseArmor).SetRandomMagicEffect(minmax[0], minmax[1]);
                }
            }
            else if (Utility.Chance(.2))
            {
                if ((item = Loot.RandomClothingOrJewelry(must_support_magic: true)) != null)
                {
                    if (item is BaseClothing)
                        ((BaseClothing)item).SetRandomMagicEffect(minmax[0], minmax[1]);
                    else if (item is BaseJewel)
                        ((BaseJewel)item).SetRandomMagicEffect(minmax[0], minmax[1]);
                }
            }
            else if (Utility.Chance(.2))
            {
                if ((item = new Wand()) != null)
                    ((Wand)item).SetRandomMagicEffect(minmax[0], minmax[1]);
            }

            return item;
        }
        #endregion PatchV152
        #region PatchV115
        private static void FixDeco(List<int> interest, List<Item> items)
        {
            foreach (Item item in items)
                if (interest.Contains(item.ItemID))
                    item.Delete();
        }
        #endregion PatchV115
        #region  PatchV113
        private static int LoadGridSpawners()
        {
            List<Spawner> list = new();
            List<string> reasons = null;
            SpawnerCapture.LoadSpawners(list, filename: "GridSpawners", ref reasons);
            if (list.Count > 0)
            {
                foreach (Spawner spawner in list)
                {
                    spawner.MoveToWorld(spawner.Location, map: spawner.Map);
                    spawner.ScheduleRespawn = true;
                }
            }
            return list.Count;
        }
        public enum SpawnerAction
        {
            None,
            Sea,
            MushroomTrap,
            GasTrap
        }
        #endregion PatchV113
        #region PatchV102
        private static int PatchAlignmentRegions()
        {
            int added = 0;
            added += EnsureRegion("The Undead Crypts", Map.Felucca, RegionRuleset.Standard, new Rectangle2D(918, 664, 123, 185));
            added += EnsureRegion("The Orc Fort", Map.Felucca, RegionRuleset.Standard, new Rectangle2D(557, 1424, 179, 129));
            added += EnsureRegion("Wind Dungeon", Map.Felucca, RegionRuleset.Dungeon, new Rectangle2D(5120, 7, 248, 247));
            added += EnsureRegion("Yew Cemetery", Map.Felucca, RegionRuleset.Standard, new Rectangle2D(709, 1101, 30, 38));
            int patched = 0;
            patched += SetRegionAlignment("The Undead Crypts", Map.Felucca, AlignmentType.Undead);
            patched += SetRegionAlignment("The Orc Fort", Map.Felucca, AlignmentType.Orc);
            patched += SetRegionAlignment("Wind Dungeon", Map.Felucca, AlignmentType.Council);
            patched += SetRegionAlignment("Britain Graveyard", Map.Felucca, AlignmentType.Undead);
            patched += SetRegionAlignment("Moonglow Cemetery", Map.Felucca, AlignmentType.Undead);
            patched += SetRegionAlignment("Nujel'm Cemetery", Map.Felucca, AlignmentType.Undead);
            patched += SetRegionAlignment("Vesper Cemetery", Map.Felucca, AlignmentType.Undead);
            patched += SetRegionAlignment("Yew Cemetery", Map.Felucca, AlignmentType.Undead);
            return added + patched;
        }
        private static int EnsureRegion(string name, Map map, RegionRuleset ruleset, params Rectangle2D[] bounds)
        {
            Region region = Region.FindByName(name, map);
            if (region != null)
                return 0;
            StaticRegion sr = new StaticRegion("", name, map, typeof(WarriorGuard));
            for (int i = 0; i < bounds.Length; i++)
                sr.Coords.Add(new Rectangle3D(bounds[i].X, bounds[i].Y, sbyte.MinValue, bounds[i].Width, bounds[i].Height, byte.MaxValue));
            sr.Ruleset = ruleset;
            sr.PriorityType = RegionPriorityType.Medium;
            sr.Registered = true;
            StaticRegion.XmlDatabase.Add(sr);
            return 1;
        }
        private static int SetRegionAlignment(string name, Map map, AlignmentType alignment)
        {
            StaticRegion sr = Region.FindByName(name, map) as StaticRegion;
            if (sr == null)
                return 0;
            sr.GuildAlignment = alignment;
            return 1;
        }
        #endregion PatchV102
        #endregion Helpers
    }
}