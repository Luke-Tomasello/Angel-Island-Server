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

/* Scripts/Commands/Nuke.cs
 * CHANGELOG:
 *  8/17/23, Yoar
 *      Added PatchDriedHerbs
 *  7/28/23, Yoar
 *      Added PatchAlignmentRegions
 *  6/9/23, Yoar
 *      Added PatchOracles
 *  5/28/23, Yoar
 *      Added PatchTownRegions
 *  5/8/23, Yoar
 *      Added BritainFarmsPatch
 *  5/8/23, Yoar
 *      Added AnkhPatch
 *  4/15/22, Yoar
 *      Added KinMigrate
 *  4/10/22, Yoar
 *      Added DirectionalDungeonTPs
 *  9/21/22, Adam
 *      Lost of new functionality for "Analyze Teleporters"
 *          this set of functions will help me to diagnose and repair (select, delete, and cleanup) our teleporters across shards.
 *  8/30/22, Adam (PatchSpawnerAttribs)
 *      Disable PatchSpawnerAttribs() as that functionality is now covered by other patching means
 *  8/25/22, Adam
 *      Add new Nuke command: PatchSpawnerAttribs() with the support function: FindSpawnerPatch()
 *  6/7/22, Yoar
 *      Added PatchStaticHouseDoors: Patches in doors to blueprints in the static housing files.
 *  3/3/22, Adam (ResetBardPoints)
 *      Resetting NPC Bard Guild points (Composers, not UO Bards) to some reasonable value after
 *          a bug was allowing rapid skill gain.
 *  1/26/22, Adam (ReplaceAwardInstruments / AwardInstruments)
 *      Replace (and delete) all AwardHarps, AwardLapHarps, and AwardLutes with AwardInstrument
 *      I moved a bunch of stuff like Place, and the special Naming code into AwardInstrument and and removed it from RazorInstrument
 *          basically making RazorInstrument a clean base for other classes. 
 *  1/26/22, Adam (PatchRolledUpSheetMusic)
 *      New RolledUpSheetMusic has a HashCode derived from the Composition.
 *      Patch existing RolledUpSheetMusic to have this same hash so that old RolledUpSheetMusic cannot be added to the music box,
 *          and so that comparisons in general work correctly.
 *  1/12/22, Adam (BlankScroll patch / guildfealty / BreakFollow)
 *      0xE34 is the flipped blank scroll graphic (we're using this now)
 *      the old itemID 0xEF3 has magically odd behavior. 
 *      the odd behavior includes not being able to override OnDoubleClick. It's seems to be handled by the client.
 *      BreakFollow(); Try to break followers (Multibox PvP)
 *      GuildFealty(); Recalc guild fealty
 *  12/22/21, Yoar
 *      Added FixSeaCharts: Validate all existing sea charts and reset their level if needed.
 *  12/12/21, Adam (LoadDLL)
*      Add stub for dynamically loading a patch DLL
 *  12/11/21, Adam (FailoverCleanup)
 *      Add a FailoverCleanup routine to cleanup all the orphaned Failover objects
 *  11/26/21, Yoar
 *      Added KukuiNuts: Converts useless kukui nuts (of type Item) back into usable kukui nuts (of type KukuiNut)
 *  11/26/21, Yoar
 *      Added DragonMaturity: Fixes the maturity of all breedable dragons/basis that are incorrectly marked as ageless.
 *  11/18/21, Yoar
 *      Added PatchPetStats
 *  10/22/21, Adam
 *      Replace AncientSmithyHammers with Tenjin's Hammers
 *	3/15/16, Adam
 *		Set all spawner minimums and maximums if needed. Someone set them to crazy fast, like 1 minute Air Elementals
 *			And 30 second to 1 minute pirates!!
 *		While we are at it, do a complete logging of spawners and settings.
 *	2/28/11, Adam
 *		Clear all fillable containers (freezedry was making them continously refill.)
 *	2/27/11, Adam
 *		Write a nuke to locate all spawners with a respawn time over 3 hours - rares spawners.
 *		We do this because we need to remove all the ones that overlap with the FillableContainers being introduced.
 *	2/15/11, Adam
 *		Create a nuke to wipe the wandeing healers and all partol guards except for those in Skara Brae
 *		Needed for Mortalis
 *	8/27/10, adam
 *		Use Utility.BritWrap rects from boat nav to test for valid treasure map locations.
 *		Turns out that the rects defined in Utility.BritWrap are the two halves of the world excluding T2A and the dungeons
 *		which is what we want.
 *	8/14/10, adam
 *		Patch all old maps with new dynamic coordinates
 *	6/10/10, adam
 *		locate all baby rares that need patching
 *	5/2/10, adam
 *		unhue all exploited event hued clothing
 *	3/17/10, adam
 *		Patch the pm.LastDisconnect time for all players to DateTime.UtcNow.
 *	2/26/10, Adam
 *		Add end date for the birth rare drop - March 1st
 *	2/25/10, Adam
 *		Add a birth-rares generator attached to an event sinc
 *	2/25/10, Adam
 *		delete all items in containers across the world.
 *		delete all old-school home teleporters
 *		fix checker and chess boards aftrer deleting all the pieces ;-)
 *	2/22/10, Adam
 *		The logic below that tries to skip duplicating existing region fails since NAME compare	is unreliable.
 *		For example, we have a Region Controller that handles all of Jhelom, but the XML file contains 3 separate entries foir the different islands.
 *		Another example is Ocllo Island in the XML is Island Siege in the Region Controller. We will solve this by manually turning off the duplicates
 *		generated from the XML.
 *	4/25/09, plasma	
 *		Create new DRDT regions for all XML regions where they dont already exist as DRDT
 *		Creates KinFactionCity controllers on test for faction cities
 *	2/28/09, Adam
 *		Reset all skills to 80
 *	10/21/08, Adam
 *		[IntMapOrphan set all guildstones to IsIntMapStorage = false because they were not on a spawner. This was a logic error.
 *		This Nuke patches those guildstones by setting IsIntMapStorage = true.
 *	9/29/08, Adam
 *		Add a nuke to list IDOC and near IDOC houses to the console
 *	9/21/08, Adam
 *      - Create Nuke to patch all old lables to today's date on old halloween rewards
 *  7/5/08, Adam 
 *      Prove that try/catch blocks themselves do not impose significant overhead.
 *      This is not to say 'exception handling' does not impose significant overhead as we know that does.
 *	6/18/08, Adam
 *		Report on Harrower (orphaned) instances
 *	8/23/07, Adam
 *		Write a nuke to collect all lockboxes contained in a list and move them to named pouches (one for each house) into callers backpack
 *	7/3/07, Adam
 *		reset all dragons to non breeding participants
 *  6/8/07, Adam
 *      log all rare-factory rares
 *  5/23/07, Adam
 *      Retroactively making ghosts blind
 *  4/25/07, Adam
 *		(bring this back from file revision 1.20)
 *		- Retarget all castle runes, moonstones, and runebooks to the houses ban location if the holder is not a friend of the house
 *  4/20/07, Adam
 *      ImmovableItemsOnPlayers logging
 *  03/20/07, plasma
 *      Exploit test / log
 *  02/12/07, plasma
 *      Log old CannedEvil namespace items
 *  2/6/07, Adam
 *      - initialize item.IsIntMapStorage for all items as we are not sure of the bit state that was previously used in item.cs.
 *      - Call the mobiles RemoveItem and not the ArrayLists.Remove as the mobiles needs to sep parent to null.
 *  2/5/07, Adam
 *      - System to replace all GuildRestorationDeeds and move 'deeded' guildstones to the internal map.
 *  01/02/07, Kit
 *      Validate email addresses, set to null if invalid, have logging option.
 *  12/29/06, Adam
 *      Add library patcher to set all books to im.IsLockedDown = true
 *  12/21/06, Kit
 *      changed to report location of bonded non controlled ghost pets
 *	12/19/06, Pix
 *		Nuke command to fix all BaseHouseDoorAddon.
 *  11/28/06 Taran Kain
 *      Neutralized WMD.
 *  11/27/06 Taran Kain
 *      Changed nuke back to InitializeGenes
 *  11/27/06, Adam
 *      stable all pets
 *	11/20/06 Taran Kain
 *		Change nuke to generate valid gene data for all mobiles
 *	10/20/06, Adam
 *		Remove bogus comments and watchlisting 
 *	10/19/06, Adam
 *		(bring this back from file revision 1.9)
 *		- Retarget all castle runes, moonstones, and runebooks to the houses ban location if the holder is not a friend of the house
 *		- set all houses to insecure
 *  10/15/06, Adam
 *		Make email Dumper
 *  10/09/06, Kit
 *		Disabled b1 revert nuke
 *  10/07/06, Kit
 *		B1 control slot revert(Decrease all bonded pets control slots  by one
 *	9/27/06, Adam
 *		Sandal finder
 *  8/17/06, Kit
 *		Changed back to relink forges and trees!!
 *  8/16/06, weaver
 *		Re-link tent backpacks to their respective tents!
 *  8/7/06, Kit
 *		Make nuke command relink forges to house addon list, make christmas trees be added to house addon list.
 *  7/30/06, Kit
 *		Respawn all forge addons to new animated versions!
 *	7/22/06, weaver
 *		Adapted to search through all characters' bankboxes and give them a tent each.
 *	7/6/06, Adam
 *		- Retarget all castle runes, moonstones, and runebooks to the houses ban location if the holder is not a friend of the house
 *		- set all houses to insecure
 *	05/30/06, Adam
 *		Unnewbie all reagents around the world.
 *		Note: You must use [RehydrateWorld immediatly before using this command.
 *  05/15/06, Kit
 *		New warhead wipe the world of dead bonded pets that have been released but didnt delete because of B1 bug.
 *  05/14/06, Kit
 *		Disabled this WMD.
 *  05/02/06, Kit
 *		Changed to check all pets in world add them to masters stables and then increase any bonded pets control slots by +1.
 *	03/28/06 Taran Kain
 *		Changed to reset BoneContainer hues. Checks one item per game iteration. RH's and re-FD's containers.
 *  6/16/05, Kit
 *		Changed to Wipe phantom items from vendors vs clothing hue.
 *	4/22/05: Kit
 *		Initial Version
 */

using Server.Diagnostics;
using Server.Engines;
using Server.Engines.Alignment;
using Server.Engines.BulkOrders;
using Server.Engines.ChampionSpawn;
using Server.Engines.Craft;
using Server.Engines.CronScheduler;
using Server.Engines.EventResources;
using Server.Engines.ResourcePool;
using Server.Factions;
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Multis.Deeds;
using Server.Multis.StaticHousing;
using Server.Network;
using Server.Regions;
using Server.Targeting;
using Server.Township;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TextCopy;
using static Server.Commands.Decorate;
using static Server.Commands.GenTeleporter;
using static Server.Engines.SpawnerManager;
using static Server.Item;
using static Server.Items.MusicBox;
using static Server.Items.TownshipStone;
using static Server.Mobiles.Spawner;
using static Server.Multis.BaseHouse;
using static Server.Utility;
using static Server.Utility.TeleporterRuleHelpers;
using static System.Net.Mime.MediaTypeNames;

namespace Server.Commands
{
    public class Nuke
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Nuke", AccessLevel.Administrator, new CommandEventHandler(Nuke_OnCommand));
        }
        public enum operation
        {
            INFO,
            RECOVER
        }
        [Usage("Nuke [KeysOnTheOcean|PostMessage|LogElementals|StablePets|KillAES]")]
        [Description("Does whatever. Usually a one-time patch.")]
        private static void Nuke_OnCommand(CommandEventArgs e)
        {

            if (e.Arguments.Length < 1)
            {
                e.Mobile.SendMessage("Usage: Nuke <command> [arg|arg|etc.]");
                return;
            }
            try
            {

                switch (e.Arguments[0].ToLower())
                {
                    default:
                        if (LoadDll(e) == false)
                        {
                            e.Mobile.SendMessage("Usage: Nuke [KeysOnTheOcean|PostMessage|LogElementals|StablePets]");
                            e.Mobile.SendMessage(string.Format("Nuke does not implement the command: {0}", e.Arguments[0]));
                        }
                        return;
                    case "housestorage":
                        PatchHouseStorage(e);
                        break;
                    case "holidaypets":
                        HolidayPetsCleanup(e);
                        break;
                    case "xmastrees":
                        PatchXmasTrees(e);
                        break;
                    case "obsidian":
                        ObsidianStats(e);
                        break;
                    case "patchdriedherbs":
                        PatchDriedHerbs(e);
                        break;
                    case "patchalignmentregions":
                        PatchAlignmentRegions(e);
                        break;
                    case "patchoracles":
                        PatchOracles(e);
                        break;
                    case "patchregionflags":
                        PatchRegionFlags(e);
                        break;
                    case "patchtownregions":
                        PatchTownRegions(e);
                        break;
                    case "britainfarmspatch":
                        BritainFarmsPatch(e);
                        break;
                    case "ankhpatch":
                        AnkhPatch(e);
                        break;
                    case "kinmigrate":
                        KinMigrate(e);
                        break;
                    case "directionaldungeontps":
                        DirectionalDungeonTPs(e);
                        break;
                    case "renewbie":
                        Renewbie(e);
                        break;
                    case "killaes":
                        KillAES(e);
                        break;
                    case "ditchbritguards":
                        DitchBritGuards(e);
                        break;
                    case "stablepets":
                        StablePets(e);
                        break;
                    case "keysontheocean":
                        KeysOnTheOcean(e);
                        return;
                    case "postmessage":
                        Cron.PostMessage(Cron.MessageType.MSG_DUMMY, new object[3]);
                        e.Mobile.SendMessage("Message sent.");
                        return;
                    case "campplacement":
                        CampPlacement(e);
                        return;
                    case "logelementals":
                        LogElementals(e);
                        return;
                    case "ancientsmithyhammer":
                        ReplaceAncientSmithyHammers(e);
                        return;
                    case "anunusualkey":
                        UpdateAnUnusualKey(e);
                        return;
                    case "pricecheck":
                        PriceCheck(e);
                        break;
                    case "level6":
                        PatchSpawnersForLevelSixChests(e);
                        break;
                    case "patchpetstats":
                        PatchPetStats(e);
                        break;
                    case "replacesealedbows":
                        ReplaceSealedBows(e);
                        break;
                    case "dragonmaturity":
                        DragonMaturity(e);
                        break;
                    case "kukuinuts":
                        KukuiNuts(e);
                        break;
                    case "deleteawardharps":
                        DeleteAwardHarps(e);
                        break;
                    case "fixseacharts":
                        FixSeaCharts(e);
                        break;
                    case "logsharebankparticipants":
                        LogShareBankParticipants(e);
                        break;
                    case "tournamentoftheguardianpatch":
                        TournamentOfTheGuardianPatch(e);
                        break;
                    case "blankscrollpatch":
                        BlankScrollPatch(e);
                        break;
                    case "breakfollow":
                        BreakFollow(e);
                        break;
                    case "guildfealty":
                        GuildFealty(e);
                        break;
                    case "patchrolledupsheetmusic":
                        PatchRolledUpSheetMusic(e);
                        break;
                    case "replaceawardinstruments":
                        ReplaceAwardInstruments(e);
                        break;
                    case "resetbardpoints":
                        ResetBardPoints(e);
                        break;
                    case "patchstatichousedoors":
                        PatchStaticHouseDoors(e);
                        break;
                    case "patchspawnerattribs":
                        PatchSpawnerAttribs(e);
                        break;
                    case "patchregioncontrollers":
                        PatchRegionControllers(e);
                        break;
                    #region Item Tools
                    case "analyzecustomobjects":
                        AnalyzeCustomObjects(e);
                        break;
                    case "refreshteleporters":
                        RefreshTeleporters(e);
                        break;
                    case "checkaiteleporters":
                        CheckAiTeleporters(e);
                        break;
                    case "deletechampteleporters":
                        DeleteChampTeleporters(e);
                        break;
                    case "addchampteleporters":
                        AddChampTeleporters(e);
                        break;
                    case "checkspawners":
                        CheckSpawners(e);
                        break;
                    case "nospawners":
                        NoSpawners(e);
                        break;
                    case "decotrammel":
                        DecoTrammel(e);
                        break;
                    case "dedecotrammel":
                        DeDecoTrammel(e);
                        break;
                    case "findteleporter":
                        FindTeleporter(e);
                        break;
                    case "wipeillegaltrammelteleporters":
                        WipeIllegalTrammelTeleporters(e);
                        break;
                    case "wipeallitems":
                        WipeAllItems(e);
                        break;
                    case "wipeallmobiles":
                        WipeAllMobiles(e);
                        break;
                    case "killallmobiles":
                        KillAllMobiles(e);
                        break;
                    case "countallitems":
                        CountAllItems(e);
                        break;

                    case "mlmaptest":
                        MLMapTest(e);
                        break;
                    case "visittrammelitems":
                        VisitTrammelItems(e);
                        break;

                    case "legaltrammelitem":
                        LegalTrammelItem(e);
                        break;

                    case "legaltrammelitemsinreg":
                        LegalTrammelItemsInReg(e);
                        break;

                    case "deleteallillegaltrammelitems":
                        DeleteAllIllegalTrammelItems(e);
                        break;

                    case "decodedecodeltaitems":
                        DecoDedecoDeltaItems(e);
                        break;

                    case "deleteallteleporters":
                        DeleteAllTeleporters(e);
                        break;
                    case "preserveteleporter":
                        PreserveTeleporter(e);
                        break;
                    case "excludeteleporter":
                        ExcludeTeleporter(e);
                        break;
                    case "eventteleporter":
                        EventTeleporter(e);
                        break;
                    case "eventmoongate":
                        EventMoongate(e);
                        break;
                    case "legaltrammelteleporter":
                        LegalTrammelTeleporter(e);
                        break;

                    case "visitlegaltrammelitems":
                        VisitLegalTrammelItems(e);
                        break;
                    #endregion Item Tools
                    case "removeoccastle":
                        RemoveOcCastle(e);
                        break;
                    case "stretchrect":
                        StretchRect(e);
                        break;
                    case "haspatchedisfeluccateleporters":
                        HasPatchedISFeluccaTeleporters(e);
                        break;
                    case "reviewisfeluccateleporters":
                        ReviewISFeluccaTeleporters(e);
                        break;
                    case "visitangelislandteleporters":
                        VisitAngelIslandTeleporters(e);
                        break;
                    case "copyinfo":
                        CopyInfo(e);
                        break;

                    case "visit":
                        Visit(e);
                        break;

                    case "lostlandsaccessteleporters":
                        LostLandsAccessTeleporters(e);
                        break;

                    case "findransomchests":
                        FindRansomChests(e);
                        break;

                    case "findhardzbreaks":
                        FindHardZBreaks(e);
                        break;

                    case "refreshdeco":
                        RefreshDeco(e);
                        break;

                    case "calczslop":
                        CalcZSlop(e);
                        break;

                    case "analyzetemplatemobiles":
                        AnalyzeTemplateMobiles(e);
                        break;

                    case "conformityfilter":
                        ConformityFilter(e);
                        break;

                    case "purifypricingdictionary":
                        PurifyPricingDictionary(e);
                        break;

                    case "genpricedb":
                        GenPriceDB(e);
                        break;

                    case "dogtag":
                        Dogtag(e);
                        break;

                    case "displaycache":
                        DisplayCache(e);
                        break;

                    case "initializevendorinventories":
                        InitializeVendorInventories(e);
                        break;

                    case "counticommodity":
                        CountICommodity(e);
                        break;

                    case "countfists":
                        CountFists(e);
                        break;

                    case "intmapitems":
                        IntMapItems(e);
                        break;

                    case "orphanedmobiles":
                        OrphanedMobiles(e);
                        break;

                    case "analyzetownshipitems":
                        AnalyzeTownshipItems(e);
                        break;

                    case "dungeontodungeonteleporters":
                        DungeonToDungeonTeleporters(e);
                        break;

                    case "deletenerunspawners":
                        DeleteNerunSpawners(e);
                        break;

                    case "clearhelpstuckrequests":
                        ClearHelpStuckRequests(e);
                        break;

                    case "makemurderer":
                        MakeMurderer(e);
                        break;

                    case "buildwarrior":
                        BuildWarrior(e);
                        break;

                    case "buildmage":
                        BuildMage(e);
                        break;

                    case "buildadam":
                        BuildAdam(e);
                        break;

                    case "buildmobile":
                        BuildMobile(e);
                        break;

                    case "testrandomenum":
                        TestRandomEnum(e);
                        break;

                    case "testrandomgear":
                        TestRandomGear(e);
                        break;

                    case "testimbuing":
                        TestImbuing(e);
                        break;

                    case "factionorevendor":
                        FactionOreVendor(e);
                        break;

                    case "logmapregions":
                        LogMapRegions(e);
                        break;

                    case "blink":
                        Blink(e);
                        break;

                    case "testtreasuremaplocations":
                        TestTreasureMapLocations(e);
                        break;

                    case "gethardzs":
                        GetHardZs(e);
                        break;

                    case "checkblocked":
                        CheckBlocked(e);
                        break;

                    case "addtame":
                        AddTame(e);
                        break;

                    case "loadregionjump":
                        LoadRegionJump(e);
                        break;

                    case "drawline":
                        DrawLine(e);
                        break;

                    case "findfarmspawners":
                        FindFarmSpawners(e);
                        break;

                    case "orangegorgets":
                        OrangeGorgets(e);
                        break;

                    case "testspawnerclumping":
                        TestSpawnerClumping(e);
                        break;

                    case "setspawneredgegravity":
                        SetSpawnerEdgeGravity(e);
                        break;

                    case "diagnosewanderinghealer":
                        DiagnoseWanderingHealer(e);
                        break;

                    case "moongates":
                        Moongates(e);
                        break;

                    case "itemcapture":
                        ItemCapture(e);
                        break;

                    case "fillabledungeoncontainers":
                        FillableDungeonContainers(e);
                        break;

                    case "fixnerunsspwanerz":
                        FixNerunsSpwanerZ(e);
                        break;

                    case "findlootpacks":
                        FindLootPacks(e);
                        break;

                    case "finditemspawners":
                        FindItemSpawners(e);
                        break;

                    case "activemobiles":
                        ActiveMobiles(e);
                        break;

                    case "mapsolencaves":
                        MapSolenCaves(e);
                        break;

                    case "telesfromtosolencaves":
                        TelesFromToSolenCaves(e);
                        break;

                    case "disablesolencavespawners":
                        DisableSolenCaveSpawners(e);
                        break;

                    case "listaggressors":
                        ListAggressors(e);
                        break;

                    case "restoremystats":
                        RestoreMyStats(e);
                        break;

                    case "aggressiveaction":
                        AggressiveAction(e);
                        break;

                    case "decorateaddons":
                        DecorateAddons(e);
                        break;

                    case "stackableitemnames":
                        StackableItemNames(e);
                        break;

                    case "finddungeonexitteles":
                        FindDungeonExitTeles(e);
                        break;

                    case "addtrainercheststobanks":
                        AddTrainerChestsToBanks(e);
                        break;

                    case "buildnerunspawner":
                        BuildNerunSpawner(e);
                        break;

                    case "tiledata":
                        TileData(e);
                        break;

                    case "getobjectproperties":
                        GetObjectProperties(e);
                        break;

                    case "checkhome":
                        CheckHome(e);
                        break;

                    case "deleteorphanedunusualkeys":
                        DeleteOrphanedUnusualkeys(e);
                        break;

                    case "patchuospawnmap":
                        PatchUOSpawnMap(e);
                        break;

                    case "loguospawnmap":
                        LogUOSpawnMap(e);
                        break;

                    case "findlostcontainer":
                        FindLostContainer(e);
                        break;

                    case "logexteriordoors":
                        LogExteriorDoors(e);
                        break;

                    case "importrsm":
                        ImportRSM(e);
                        break;

                    case "exportrsm":
                        ExportRSM(e);
                        break;

                    case "moveboat":
                        MoveBoat(e);
                        break;

                    case "countgems":
                        CountGems(e);
                        break;

                    case "findgoldboxes":
                        FindGoldBoxes(e);
                        break;

                    case "convertgoldboxes":
                        ConvertGoldBoxes(e);
                        break;

                    case "findraregoldboxes":
                        FindRareGoldBoxes(e);
                        break;

                    case "testnerunparser":
                        TestNerunParser(e);
                        break;

                    case "rarefactoryitem":
                        RareFactoryItem(e);
                        break;

                    case "emptybank":
                        EmptyBank(e);
                        break;

                    case "patchtowerdoors":
                        PatchTowerDoors(e);
                        break;

                    case "grandfatherherding":
                        GrandfatherHerding(e);
                        break;

                    case "deletecamps":
                        DeleteCamps(e);
                        break;

                    case "loglocation":
                        LogLocation(e);
                        break;

                    case "analyzerandomminmaxscaled":
                        AnalyzeRandomMinMaxScaled(e);
                        break;

                    case "logitemtest":
                        LogItemTest(e);
                        break;

                    case "analyzeore":
                        AnalyzeOre(e);
                        break;

                    case "stromsretribution":
                        StromsRetribution(e);
                        break;

                    case "stromstownshiplockdowns":
                        StromsTownshipLockdowns(e);
                        break;

                    case "checkstromrestoration":
                        CheckStromRestoration(e);
                        break;

                    case "testcraftitem":
                        TestCraftItem(e);
                        break;

                    case "queryangryminers":
                        QueryAngryMiners(e);
                        break;

                    case "stackoverflowtest":
                        StackOverflowTest(e);
                        break;

                    case "savegridspawners":
                        SaveGridSpawners(e);
                        break;

                    case "loadgridspawners":
                        LoadGridSpawners(e);
                        break;

                    case "grab":
                        Grab(e);
                        break;

                    case "wipealltownships":
                        WipeAllTownships(e);
                        break;

                    case "getpets":
                        GetPets(e);
                        break;

                    case "findskill":
                        FindSkill(e);
                        break;

                    case "initcraftlist":
                        InitCraftList(e);
                        break;

                    case "convertlargetosmallbods":
                        ConvertLargeToSmallBODs(e);
                        break;

                    case "originreport":
                        OriginReport(e);
                        break;

                    case "getmultioffset":
                        GetMultiOffset(e);
                        break;

                    case "countplayerhousedeeds":
                        CountPlayerHouseDeeds(e);
                        break;

                    case "countsilverweapons":
                        CountSilverWeapons(e);
                        break;

                    case "treasuremapchestinventory":
                        TreasureMapChestInventory(e);
                        break;

                    case "dungeonchestinventory":
                        DungeonChestInventory(e);
                        break;

                    case "grandfatherwarhorses":
                        GrandfatherWarHorses(e);
                        break;

                    case "warhorsescrib":
                        WarHorsesCrib(e);
                        break;

                    case "profileitems":
                        ProfileItems(e);
                        break;

                    case "packuptownship":
                        PackUpTownship(e);
                        break;

                    case "packupeverything":
                        PackUpEverything(e);
                        break;

                    case "deepdupe":
                        DeepDupe(e);
                        break;

                    case "itemrestorationdeed":
                        ItemRestorationDeed(e);
                        break;

                    case "islinkedto":
                        IsLinkedTo(e);
                        break;

                    case "linkall":
                        LinkAll(e);
                        break;

                    case "uomusic":
                        UOMusic(e);
                        break;

                    case "repairspawners":
                        RepairSpawners(e);
                        break;

                    case "proactivehoming":
                        ProactiveHoming(e);
                        break;

                    case "leavesector":
                        LeaveSector(e);
                        break;

                    case "entersector":
                        EnterSector(e);
                        break;

                    case "coveinvasion":
                        CoveInvasion(e);
                        break;

                    case "musicdebug":
                        MusicDebug(e);
                        break;

                    case "spawnertriggers":
                        SpawnerTriggers(e);
                        break;

                    case "unionrect":
                        UnionRect(e);
                        break;

                    case "swapstatics":
                        SwapStatics(e);
                        break;

                    case "catalogitemspawnids":
                        CatalogItemSpawnids(e);
                        break;

                    case "patchpquestpoints":
                        PatchPQuestPoints(e);
                        break;

                    case "fixtracks":
                        FixTracks(e);
                        break;

                    case "adjustbankbox":
                        AdjustBankBox(e);
                        break;

                    case "prepworldfordistribution":
                        PrepWorldForDistribution(e);
                        break;

                    case "removeallstaff":
                        RemoveAllStaff(e);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        #region Remove All Staff
        public static void RemoveAllStaff(CommandEventArgs e)
        {
            if (e.Mobile.AccessLevel < AccessLevel.Owner)
            {
                e.Mobile.SendMessage("Not authorized.");
                ConsoleWriteLine("RemoveAllStaff: Not authorized.", ConsoleColor.Red);
                return;
            }
            int removed = 0;
            foreach (Mobile m in World.Mobiles.Values)
                if (m is PlayerMobile pm && pm.AccessLevel != AccessLevel.Player && pm.AccessLevel != AccessLevel.Owner)
                {   // kick them
                    if (pm.NetState != null)
                        pm.NetState.Dispose();

                    // report
                    string text = string.Format($"Removing {CommandLogging.Format(pm)}");
                    e.Mobile.SendMessage(text);
                    ConsoleWriteLine(text, ConsoleColor.Red);

                    Accounting.Account acct = pm.Account as Accounting.Account;
                    if (acct != null)
                        acct.Delete();
                    else
                        ;
                    removed++;
                }

            e.Mobile.SendMessage($"{removed} staff members removed.");
            ConsoleWriteLine($"{removed} staff members removed.", ConsoleColor.Red);
        }
        #endregion Remove All Staff
        #region Prep World For Distribution
        private static void PrepWorldForDistribution(CommandEventArgs e)
        {
            if (!Core.Debug && !Core.RuleSets.TestCenterRules())
            {
                e.Mobile.SendMessage("You may only run this command on a Debug Test Center");
                return;
            }
            int house_count = 0;
            int guild_count = 0;
            int township_count = 0;
            foreach (Item item in World.Items.Values)
                if (item is BaseHouse bh)
                {
                    bh.Owner = null;
                    bh.IsStaffOwned = true;
                    if (bh.Sign != null)
                        bh.Sign.FreezeDecay = true;
                    else
                    {
                        // probably a tent, we don't care
                        //System.Diagnostics.Debug.Assert(false);
                    }
                    house_count++;

                }
                else if (item is Guildstone gs)
                {
                    gs.IsStaffOwned = true;
                    guild_count++;
                }
                else if (item is TownStone ts)
                {
                    ts.IsStaffOwned = true;
                    township_count++;
                }

            Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(BlammoTick), 
                new object[] { house_count, guild_count, township_count});
        }
        private static void BlammoTick(object state)
        {
            object[] aState = (object[])state;
            int house_count = (int)aState[0];
            int guild_count = (int)aState[1];
            int township_count = (int)aState[2];

            // delete accounts 
            int account_count = OwnerTools.DeleteAccounts(null, include_owner: true);

            Console.WriteLine("{0} Accounts deleted.", account_count);
            Console.WriteLine("{0} houses preserved.", house_count);
            Console.WriteLine("{0} guilds preserved.", guild_count);
            Console.WriteLine("{0} townships preserved.", township_count);
        
            World.Save();
        }
        #endregion Prep World For Distribution
        #region Adjust Bank box
        private static void AdjustBankBox(CommandEventArgs e)
        {
            if (!Core.Debug && !Core.RuleSets.TestCenterRules())
            {
                e.Mobile.SendMessage("You may only run tis command on a Debug Test Center");
                return;
            }

            int count = e.GetInt32(1);
            List<Item> list = new();
            foreach (Item item in e.Mobile.BankBox.Items)
                if (item != null)
                    list.Add(item);

            foreach (Item item in list)
                {
                    e.Mobile.BankBox.RemoveItem(item);
                    item.Delete();
                }

            e.Mobile.BankBox.Items.Clear();

            for (int ix = 0; ix < count; ix++)
                e.Mobile.BankBox.AddItem(new Key());
            ;
            ;
            ;
            ;

        }
        #endregion Adjust Bank box
        #region FixTracks
        private static void FixTracks(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Select the area of tracks to fix...");
            BoundingBoxPicker.Begin(e.Mobile, new BoundingBoxCallback(FixTracks_Callback), 0x01);
        }
        private static void FixTracks_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            // Create rec and retrieve items within from bounding box callback result
            Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);
            IPooledEnumerable eable = map.GetItemsInBounds(rect);

            int total = 0;
            int warnings = 0;
            int trackLeft = 0x1258;
            int trackRight = 0x1259;
            int noDraw = 0x2198;
            Dictionary <Point3D, Item> table = new();
            // See what we got
            foreach (object obj in eable)
            {
                if (obj is Item item)
                {
                    if (item.ItemID == trackLeft || item.ItemID == trackRight)
                    {
                        item.Z = 1;
                        Static plate = new Static(0x2198);
                        plate.MoveToWorld(new Point3D(item.X, item.Y, 4), item.Map);

                        plate = new Static(0x2198);
                        plate.MoveToWorld(new Point3D(item.X, item.Y, 2), item.Map);
                    }
                    else
                    {
                        item.Delete();
                    }

                }
            }
            eable.Free();
            from.SendMessage("Done.");
        }
        #endregion FixTracks
        #region patch PQuestPoints
        private static void PatchPQuestPoints(CommandEventArgs e)
        {
            try
            {
                int ser = 0;
                if (!Utility.StringToInt(e.GetString(1), ref ser))
                    throw new ApplicationException();
                Serial serial = (Serial)ser;
                int escapes = e.GetInt32(2);
                int rares = e.GetInt32(3);
                int sterling = e.GetInt32(4);

                var QuestPoints = Engines.DataRecorder.DataRecorder.GetPQuestPoints;
                Mobile m = World.FindMobile(serial);
                if (QuestPoints.ContainsKey(m))
                {
                    QuestPoints[m][0] = escapes;
                    QuestPoints[m][1] = rares;
                    QuestPoints[m][2] = sterling;
                }

                e.Mobile.SendMessage($"Patched: PatchPQuestPoints {m.Name} {escapes} escapes, {rares} rares, {sterling} sterling");
            }
            catch
            {
                e.Mobile.SendMessage("Usage: PatchPQuestPoints <mobile serial> <escapes> <rares> <sterling>");
            }
        }
        #endregion patch PQuestPoints
        #region Record the graphic IDs of items that spawners spawn
        private static void CatalogItemSpawnids(CommandEventArgs e)
        {
            int count = 0;
            Utility.ConsoleWriteLine("Compiling spawner database. Slow...", ConsoleColor.Yellow);
            e.Mobile.SendMessage("Compiling spawner database. Slow...");

            List<int> list = new();
            foreach (Item item in World.Items.Values)
                if (item is Spawner spawner && spawner.ObjectNamesRaw != null)
                {
                    ArrayList objectNames = new(spawner.ObjectNamesRaw);

                    for (int i = 0; i < objectNames.Count; i++)
                    {
                        string str = objectNames[i] as string;

                        if (str.Length > 0)
                        {
                            str = str.Trim();
                            //adam 8/29/22, we now allow ':' delimited lists of creatures
                            string[] tokens = str.Split(':', StringSplitOptions.RemoveEmptyEntries);
                            for (int ix = 0; ix < tokens.Length; ix++)
                            {
                                string token = tokens[ix];

                                Type type = SpawnerType.GetType(token);

                                if (type == null)
                                {
                                    //from.SendMessage("{0} is not a valid type name.", token);
                                    tokens[ix] = string.Empty;
                                }
                                else if (HasParameterlessConstructor(type))
                                {
                                    object o = Activator.CreateInstance(type);
                                    if (o is Mobile m)
                                    {
                                        m.Delete();
                                        continue;
                                    }
                                    if (o is Item itm)
                                    {
                                        if (!list.Contains(itm.ItemID))
                                        {
                                            list.Add(itm.ItemID);
                                            count++;
                                        }
                                        itm.Delete();
                                    }
                                }
                            }
                        }
                    }
                }

            LogHelper logger = new LogHelper("spawner_spawns.log", overwrite: true, sline: true, quiet: true);
            foreach (int id in list)
                logger.Log(string.Format($"0x{id:X}, "));
            logger.Finish();

            Utility.ConsoleWriteLine($"Spawner database complete with {count} entries", ConsoleColor.Yellow);
            e.Mobile.SendMessage($"Spawner database complete with {count} entries");
        }
        #endregion Record the graphic IDs of items that spawners spawn

        #region Swap Statics
        private static void SwapStatics(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the top tile...");
            e.Mobile.Target = new SwapStaticsTarget(null);
        }
        public class SwapStaticsTarget : Target
        {
            public SwapStaticsTarget(string command)
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is Static || target is Mobile)
                {
                    IEntity top = target as IEntity;
                    List<Item> items = new();
                    OwnerTools.GetStack(top.X, top.Y, top.Map, items: items);
                    items.RemoveAll(x => Math.Abs(x.Z - top.Z) > 1);
                    items.RemoveAll(x => x is not Static);

                    if (items.Count == 2)
                    {
                        Static lower = new Static(1/*top.ItemID*/);
                        Static higher = new Static(1/*top.ItemID*/);

                        if (items[0].Serial > items[1].Serial)
                        {
                            Utility.CopyProperties(lower, items[0]);
                            Utility.CopyProperties(higher, items[1]);
                            lower.MoveToWorld(items[0].Location);
                            higher.MoveToWorld(items[1].Location);
                            items[0].Delete();
                            items[1].Delete();
                        }
                        else
                        {
                            Utility.CopyProperties(higher, items[0]);
                            Utility.CopyProperties(lower, items[1]);
                            higher.MoveToWorld(items[0].Location);
                            lower.MoveToWorld(items[1].Location);
                            items[0].MoveToIntStorage();
                            items[1].MoveToIntStorage();
                            items[0].Delete();
                            items[1].Delete();
                        }
                    }
                    else
                        from.SendMessage($"Can't swap {items.Count} static tiles.");
                }
                else
                {
                    from.SendMessage("That is not a static tile.");
                    return;
                }
            }
        }
        #endregion Swap Statics
        #region Union Rect
        private static void UnionRect(CommandEventArgs e)
        {
            // CustomRegionControl
            e.Mobile.SendMessage("Target the player...");
            e.Mobile.Target = new UnionRectTarget(null);
        }
        public class UnionRectTarget : Target
        {
            string m_Command;
            public UnionRectTarget(string command)
                : base(17, true, TargetFlags.None)
            {
                m_Command = command;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is CustomRegionControl rc)
                {
                    List<Rectangle2D> list = GetRegionControlRects(rc);
                    if (list.Count > 0)
                    {
                        Rectangle2D @base = list[0];
                        foreach (var rect in list)
                            @base.MakeHold(rect);

                        ClipboardService.SetText(@base.ToString());
                        Server.Gumps.EditAreaGump.FlashArea(from, @base, from.Map);
                        from.SendMessage("{0} has been copied to your clipboard.", @base.ToString());
                    }
                    else
                        from.SendMessage("There are no rectangles in that RegionControl.");
                }
                else
                {
                    from.SendMessage("That is not a RegionControl.");
                    return;
                }
            }
        }
        private static List<Rectangle2D> GetRegionControlRects(CustomRegionControl fdr)
        {
            List<Rectangle2D> rects = new();
            foreach (var rect in fdr.CustomRegion.Coords)
                rects.Add(new Rectangle2D(rect.Start, rect.End));

            return rects;
        }
        #endregion Union Rect
        #region Locate Spawner Triggers
        private static void SpawnerTriggers(CommandEventArgs e)
        {
            foreach (Item item in World.Items.Values)
                if (item is Spawner spawner && spawner.TriggerLinkPlayers != null)
                    ;
        }
        #endregion Locate Spawner Triggers
        #region Music Debug
        // if (Utility.SoundCanvas.ContainsKey(m) && Utility.SoundCanvas[m] != m_UOMusic)
        private static void MusicDebug(CommandEventArgs e)
        {
            if (e.Length >= 2)
                switch (e.GetString(1).ToLower())
                {
                    case "soundcanvas":
                        {
                            e.Mobile.SendMessage("Target the player...");
                            e.Mobile.Target = new MusicDebugTarget("soundcanvas");
                            break;
                        }
                }
            else
                e.Mobile.SendMessage("Usage: MusicDebug <SoundCanvas>");
        }
        public class MusicDebugTarget : Target
        {
            string m_Command;
            public MusicDebugTarget(string command)
                : base(17, true, TargetFlags.None)
            {
                m_Command = command;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile mob)
                {
                    if (Utility.SoundCanvas.ContainsKey(mob))
                        from.SendMessage("{0}", Utility.SoundCanvas[mob].ToString());
                    else
                        from.SendMessage("No SoundCanvas for {0}", mob);
                }
                else
                {
                    from.SendMessage("That is not a Player.");
                    return;
                }
            }
        }
        #endregion Music Debug
        #region Cove Invasion
        private static Rectangle2D CoveInvasionRect = new Rectangle2D();
        private static void CoveInvasion(CommandEventArgs e)
        {
            bool has_rect = false;
            try
            {
                switch (e.GetString(1))
                {
                    case "-create":
                        {// (4215, 555)+(6, 3)
                            CoveInvasionRect = new Rectangle2D(new Point2D(4215, 555), new Point2D(4222, 562));
                            //CoveInvasionRect.MakeHold(new Rectangle2D(new Point2D(2200, 1160), new Point2D(2286, 1246)));

                            //// now add in what we have so far NE corner
                            //// (2188, 1089)+(98, 157)
                            //Rectangle2D temp = new Rectangle2D(2188, 1089, 98, 157);
                            //CoveInvasionRect.MakeHold(temp);
                            //// orc fort
                            //// (2274, 1107)+(48, 25)
                            //temp = new Rectangle2D(2274, 1107,48, 25);
                            //CoveInvasionRect.MakeHold(temp);
                            //// (2273, 1105)+(39, 35)
                            //temp = new Rectangle2D(2273, 1105, 39, 35);
                            //CoveInvasionRect.MakeHold(temp);
                            //// other areas
                            //// (2286, 1256)+(20, 1)
                            //temp = new Rectangle2D(2286, 1256,20, 1);
                            //CoveInvasionRect.MakeHold(temp);
                            //// (2282, 1262)+(5, 6)
                            //temp = new Rectangle2D(2282, 1262, 5, 6);
                            //CoveInvasionRect.MakeHold(temp);
                            ////(2199, 1273)+(28, 3)
                            //temp = new Rectangle2D(2199, 1273, 28, 3);
                            //CoveInvasionRect.MakeHold(temp);
                            ////(2177, 1292)+(16, 44)
                            //temp = new Rectangle2D(2177, 1292,16, 44);
                            //CoveInvasionRect.MakeHold(temp);
                            //// (2167, 1292)+(24, 27)
                            //temp = new Rectangle2D(2167, 1292, 24, 27);
                            //CoveInvasionRect.MakeHold(temp);


                            has_rect = true;
                            break;
                        }
                    case "-grownorth":
                        {
                            int tiles = e.GetInt32(2);
                            int start_X = CoveInvasionRect.Start.X;
                            int start_Y = CoveInvasionRect.Start.Y;
                            int end_X = CoveInvasionRect.End.X;
                            int end_Y = CoveInvasionRect.End.Y;
                            CoveInvasionRect = new Rectangle2D(new Point2D(start_X, start_Y - tiles), new Point2D(end_X, end_Y));
                            has_rect = true;
                            break;
                        }

                    case "-growwest":
                        {
                            int tiles = e.GetInt32(2);
                            int start_X = CoveInvasionRect.Start.X;
                            int start_Y = CoveInvasionRect.Start.Y;
                            int end_X = CoveInvasionRect.End.X;
                            int end_Y = CoveInvasionRect.End.Y;
                            CoveInvasionRect = new Rectangle2D(new Point2D(start_X - tiles, start_Y), new Point2D(end_X, end_Y));
                            has_rect = true;
                            break;
                        }

                    case "-flash":
                        {
                            has_rect = true;
                            break;
                        }

                    default:
                        break;
                }
            }
            catch { }

            if (has_rect)
            {
                //ClipboardService.SetText(CoveInvasionRect.ToString());
                int start_X = CoveInvasionRect.Start.X;
                int start_Y = CoveInvasionRect.Start.Y;
                int end_X = CoveInvasionRect.End.X;
                int end_Y = CoveInvasionRect.End.Y;
                string text = string.Format("({0}, {1}, {2}, {3})", start_X, start_Y, end_X, end_Y);
                ClipboardService.SetText(text);
                Server.Gumps.EditAreaGump.FlashArea(e.Mobile, CoveInvasionRect, e.Mobile.Map);
                e.Mobile.SendMessage("{0} has been copied to your clipboard.", text);
            }
            else
                e.Mobile.SendMessage("Usage: CoveInvasion -create");
        }
        #endregion  
        #region Leave Sector
        private static void LeaveSector(CommandEventArgs e)
        {
            Sector sector = e.Mobile.Map.GetSector(e.Mobile);
            sector.OnLeave(e.Mobile);
        }
        private static void EnterSector(CommandEventArgs e)
        {
            Sector sector = e.Mobile.Map.GetSector(e.Mobile);
            sector.OnEnter(e.Mobile);
        }
        #endregion Leave Sector
        #region Proactive Homing
        private static void ProactiveHoming(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the object...");
            e.Mobile.Target = new ProactiveHomingTarget(); // Call our target
        }

        public class ProactiveHomingTarget : Target
        {
            public ProactiveHomingTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is BaseCreature mob)
                {
                    bool current = mob.GetCreatureBool(CreatureBoolTable.ProactiveHoming);
                    mob.SetCreatureBool(CreatureBoolTable.ProactiveHoming, !current);
                    from.SendMessage("ProactiveHoming switched {0}.", !current);
                }
                else
                {
                    from.SendMessage("That is not a BaseCreature.");
                    return;
                }
            }
        }
        #endregion Proactive Homing
        #region Repair Spawners
        private static void RepairSpawners(CommandEventArgs e)
        {
            LogHelper logger = new LogHelper("spawner mobs.log", overwrite: true, sline: true, quiet: true);
            logger.Log("(Serial serial, string[] NameList, string counts)[] tupleList = {");
            int patched = 0;
            foreach (Item item in World.Items.Values)
                if (item is Spawner spawner)
                    if (spawner.ModeMulti || spawner.ModeNeruns)
                    {
                        //(Serial serial, string[] NameList, string counts)[] tupleList = {
                        //    (1, new string[]{"cow" }, "1"),
                        //    (5, new string[]{"chickens" }, "1, 3"),
                        //    (1, new string[]{"airplane" }, "2, 4"),
                        //    (0x4004EA88, new string[]{ "paganpeasant","pagandruid", }, "2, 1"),
                        //    (0x4004EA89, new string[]{ "paganpeasant","pagandruid", }, "2, 1"),
                        //};
                        ;
                        if (spawner.ObjectNamesRaw != null)
                        {
                            string names = string.Empty;
                            foreach (string name in spawner.ObjectNamesRaw)
                                names += ('"' + name + '"' + ',');

                            string temp = string.Format("({0}, new string[]{{ {1} }}, {2}),", spawner.Serial.ToString(),
                                names, '"' + spawner.Counts + '"');

                            logger.Log(temp);
                            patched++;
                        }
                    }

            logger.Log("};");
            logger.Finish();
            e.Mobile.SendMessage("done, with {0} captured.", patched);
        }
        #endregion Repair Spawners
        #region UOMusic
        private static void UOMusic(CommandEventArgs e)
        {
            try
            {
                if (e.GetString(1).Equals("play", StringComparison.OrdinalIgnoreCase))
                {
                    e.Mobile.Send(Network.PlayMusic.GetInstance((MusicName)e.GetInt32(2)));
                }
                else if (e.GetString(1).Equals("stop", StringComparison.OrdinalIgnoreCase))
                {
                    // There is no way to simply stop the current music. Regions send this InvalidInstance, but that doesn't actually stop the music
                    //  However, ClassicUO respects music names 0-150. The UO music only goes to 50 (TokunoDungeon). If you pass ClassicUO a value greater than
                    //  50, yet less than 150, ClassicUO stops playback cleanly. (Their code supports this.)
                    e.Mobile.Send(Network.PlayMusic.GetInstance((MusicName)100));   // <= anything > 50 && < 150 stops the music
                    e.Mobile.Send(Network.PlayMusic.InvalidInstance);
                }
            }
            catch
            {
                e.Mobile.SendMessage("Usage: UOMusic Play|Stop <musicID>");
            }

        }
        #endregion UOMusic
        #region IsLinkedTo
        private static void IsLinkedTo(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the object...");
            e.Mobile.Target = new IsLinkedToTarget(); // Call our target
        }

        public class IsLinkedToTarget : Target
        {
            public IsLinkedToTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is Mobile mob)
                {
                    from.SendMessage("This mobile {0} linked to.", Utility.LinkedTo(mob) ? "is" : "is not");
                    if (Utility.LinkedTo(mob))
                        from.SendMessage("Remember, we don't cleanup 'linked to' links until server restart.");

                }
                else if (target is Item item)
                {
                    from.SendMessage("This item {0} linked to.", Utility.LinkedTo(item) ? "is" : "is not");
                    if (Utility.LinkedTo(item))
                        from.SendMessage("Remember, we don't cleanup 'linked to' links until server restart.");

                }
                else
                {
                    from.SendMessage("That is neither a mobile nor an item.");
                    return;
                }
            }
        }

        private static void LinkAll(CommandEventArgs e)
        {
            int linke = Patcher.PatchV87(0, utility: true);
            ;
            ;
            ;

        }
        #endregion IsLinkedTo
        #region Home Restoration Deed
        private static void ItemRestorationDeed(CommandEventArgs e)
        {
            HomeItemRestorationDeed deed = new HomeItemRestorationDeed(null, new Bow(), HomeItemRestorationDeed.LockdownType.None);
            e.Mobile.SendMessage("The deed is at your feet.");
            deed.MoveToWorld(e.Mobile.Location, e.Mobile.Map);
        }
        #endregion Home Restoration Deed

        #region DeepDupe
        private static void DeepDupe(CommandEventArgs e)
        {// grab itemIDs and z locations (could be enhanced to get relative locations of nearby items)

            e.Mobile.SendMessage("Target item...");
            e.Mobile.Target = new DeepDupeTarget(); // Call our target
        }

        public class DeepDupeTarget : Target
        {
            public DeepDupeTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                Item duped = null;
                if (target is Item item)
                {
                    duped = Utility.DeepDupe(item);
                }
                else
                {
                    from.SendMessage("That is not an item.");
                    return;
                }
                if (duped != null)
                {
                    duped.MoveToWorld(from.Location, from.Map);
                }
            }
        }
        #endregion DeepDupe
        private static void PackUpEverything(CommandEventArgs e)
        {

            if (!Core.Debug && !Core.RuleSets.TestCenterRules())
            {
                e.Mobile.SendMessage("You may only run tis command on a Debug Test Center");
                return;
            }

            LogHelper logger = new LogHelper("pack up everything.log", overwrite: true, sline: true);
            try
            {
                foreach (var item in World.Items.Values)
                    if (item is BaseHouse bh && bh is not Tent && bh.GetBaseHouseBool(BaseHouseBoolTable.IsPackedUp) == false)
                    {
                        bh.PackUpHouse(World.GetSystemAcct(), logger);
                        if (bh.LockDownCount > 0)
                        {
                            ; // error
                        }

                        if (ExcludeAllowedSecures(bh.Secures) > 0)
                        {
                            ; // error
                        }

                        if (bh.AddonCount > 0)
                        {
                            ; // error
                        }
                    }
                    else if (item is TownshipStone stone && stone.GetTownshipStoneBool(TownshipStoneBoolTable.IsPackedUp) == false)
                    {
                        stone.PackUpTownship(World.GetSystemAcct(), logger);
                    }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                logger.Finish();
            }
        }
        private static int ExcludeAllowedSecures(ArrayList list)
        {
            int count = list != null ? list.Count : 0;
            if (count > 0)
                foreach (object o in list)
                    if (o is SecureInfo si)
                        if (si.Item is StrongBox)
                            count--;

            return count;
        }
        #region PackUpTownship
        private static void PackUpTownship(CommandEventArgs e)
        {// grab itemIDs and z locations (could be enhanced to get relative locations of nearby items)

            e.Mobile.SendMessage("Target items...");
            e.Mobile.Target = new PackUpTownshipTarget();
        }

        public class PackUpTownshipTarget : Target
        {
            public PackUpTownshipTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is TownshipStone stone)
                {
                    LogHelper logger = new LogHelper("Pack up township.log", overwrite: false, sline: true);
                    try
                    {
                        stone.PackUpTownship(from, logger);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex);
                    }
                    finally
                    {
                        logger.Finish();
                    }
                }
                else
                {
                    from.SendMessage("That is not a TownStone.");
                    return;
                }
            }
        }
        #endregion PackUpTownship
        #region Profile Items
        private static void ProfileItems(CommandEventArgs e)
        {
            int in_felucca = 0;
            int in_trammel = 0;
            int on_internal = 0;
            int on_internal_on_player = 0;
            int on_internal_on_mobile = 0;
            int on_internal_in_container = 0;
            int on_internal_unaccounted = 0;
            int deleted = 0;

            foreach (Item item in World.Items.Values)
            {
                if (item.Deleted)
                    deleted++;
                else if (item.Map == Map.Felucca)
                    in_felucca++;
                else if (item.Map == Map.Trammel)
                    in_trammel++;
                else if (item.Map == Map.Internal)
                {
                    on_internal++;
                    if (item.RootParent is PlayerMobile)
                        on_internal_on_player++;
                    else if (item.RootParent is Mobile)
                        on_internal_on_mobile++;
                    else if (item.RootParent is Container)
                        on_internal_in_container++;
                    else
                        on_internal_unaccounted++;
                }
            }
            ;
            ;
            ;
            ;

        }
        #endregion Profile Items
        #region House Storage
        public static void PatchHouseStorage(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Patched {0} houses.", StorageInfo.PatchAll());
        }

        public class StorageInfo
        {
            public static int PatchAll()
            {
                List<BaseHouse> houses = GetAllHouses();

                int count = 0;

                if (!Core.RuleSets.SiegeStyleRules())
                    count += Patch(houses, m_AITable);

                return count;
            }

            public static List<BaseHouse> GetAllHouses()
            {
                List<BaseHouse> houses = new List<BaseHouse>();

                foreach (Item item in World.Items.Values)
                {
                    if (item is BaseHouse)
                        houses.Add((BaseHouse)item);
                }

                return houses;
            }

            public static int Patch(List<BaseHouse> houses, StorageInfo[] table)
            {
                int count = 0;

                foreach (BaseHouse bh in houses)
                {
                    Type type = bh.GetType();

                    foreach (StorageInfo info in table)
                    {
                        if (info.HouseType.IsAssignableFrom(type))
                        {
                            bh.BonusLockDowns = bh.MaxLockDowns - info.MaxLockDowns;
                            bh.MaxLockDownsRaw = info.MaxLockDowns;
                            bh.BonusSecures = bh.MaxSecures - info.MaxSecures;
                            bh.MaxSecuresRaw = info.MaxSecures;
                            bh.BonusLockboxes = bh.MaxLockboxes - info.MaxLockboxes;
                            bh.MaxLockboxesRaw = info.MaxLockboxes;
                            count++;
                            break;
                        }
                    }
                }

                return count;
            }

            private static readonly StorageInfo[] m_AITable = new StorageInfo[]
                {
                    new StorageInfo(typeof(SmallOldHouse), 270, 2, 2),
                    new StorageInfo(typeof(GuildHouse), 600, 4, 2),
                    new StorageInfo(typeof(TwoStoryHouse), 750, 5, 3),
                    new StorageInfo(typeof(Tower), 1150, 8, 4),
                    new StorageInfo(typeof(Keep), 1300, 9, 5),
                    new StorageInfo(typeof(Castle), 1950, 14, 7),
                    new StorageInfo(typeof(LargePatioHouse), 600, 4, 2),
                    new StorageInfo(typeof(LargeMarbleHouse), 750, 5, 3),
                    new StorageInfo(typeof(SmallTower), 300, 2, 2),
                    new StorageInfo(typeof(LogCabin), 600, 4, 2),
                    new StorageInfo(typeof(SandStonePatio), 450, 3, 2),
                    new StorageInfo(typeof(TwoStoryVilla), 600, 4, 2),
                    new StorageInfo(typeof(SmallShop), 275, 2, 2),
                    new StorageInfo(typeof(StaticHouse), 270, 2, 2),
                };

            private Type m_HouseType;
            private int m_MaxLockDowns;
            private int m_MaxSecures;
            private int m_MaxLockboxes;

            public Type HouseType { get { return m_HouseType; } }
            public int MaxLockDowns { get { return m_MaxLockDowns; } }
            public int MaxSecures { get { return m_MaxSecures; } }
            public int MaxLockboxes { get { return m_MaxLockboxes; } }

            public StorageInfo(Type houseType, int maxLockDowns, int maxSecures, int maxLockboxes)
            {
                m_HouseType = houseType;
                m_MaxLockDowns = maxLockDowns;
                m_MaxSecures = maxSecures;
                m_MaxLockboxes = maxLockboxes;
            }
        }
        #endregion
        #region Holiday Pets Cleanup
        public static HolidayPetsResult HolidayPetsCleanup(CommandEventArgs e)
        {
            HolidayPetsResult result = new HolidayPetsResult();

            List<BaseCreature> pets = new List<BaseCreature>();

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is BaseCreature)
                {
                    BaseCreature bc = (BaseCreature)m;

                    if (bc.IsWinterHolidayPet && !bc.IsAnyStabled && bc.Map != Map.Trammel && bc.SpawnerTempRefCount == 0)
                        pets.Add(bc);
                }
            }

            foreach (BaseCreature pet in pets)
            {
                if (pet is BaseMount)
                {
                    BaseMount mount = (BaseMount)pet;

                    if (mount.Rider != null)
                        mount.Rider = null;
                }

                Mobile master = pet.ControlMaster;

                if (master == null && pet.GetCreatureBool(CreatureBoolTable.IsTownshipLivestock))
                {
                    foreach (TownshipStone ts in TownshipStone.AllTownshipStones)
                    {
                        if (ts.Livestock.ContainsKey(pet))
                        {
                            ts.ReleaseLivestock(pet);

                            if (ts.Guild != null)
                                master = ts.Guild.Leader;

                            break;
                        }
                    }
                }

                if (master == null)
                {
                    pet.Delete();
                    result.Deleted++;
                }
                else
                {
                    StableHolidayPet(pet, master);
                    result.Stabled++;
                }
            }

            e.Mobile.SendMessage("{0} holiday pets were stabled and {1} holiday pets were deleted.", result.Stabled, result.Deleted);

            return result;
        }

        private static bool StableHolidayPet(BaseCreature pet, Mobile master)
        {
            pet.ControlTarget = null;
            pet.ControlOrder = OrderType.Stay;
            pet.Internalize();
            pet.SetControlMaster(null);
            pet.SummonMaster = null;
            pet.IsElfStabled = true;

            List<BaseCreature> stabled;

            if (!ElfStabler.Table.TryGetValue(master, out stabled))
                ElfStabler.Table[master] = stabled = new List<BaseCreature>();

            stabled.Add(pet);

            pet.LastStableChargeTime = DateTime.UtcNow;

            return true;
        }

        public class HolidayPetsResult
        {
            public int Stabled;
            public int Deleted;
        }
        #endregion
        #region XmasTrees
        public static void PatchXmasTrees(CommandEventArgs e)
        {
            int count = ReplaceLockdowns(new ReplaceEntry[]
                {
                    new ReplaceEntry(typeof(HolidayDeed), -1, typeof(ChristmasTreeAddonDeed), null),
                    new ReplaceEntry(typeof(ChristmasTreeDeed), -1, typeof(ChristmasTreeAddonDeed), null),
                    new ReplaceEntry(typeof(HolidayTree), 0x0CD7, typeof(ChristmasTreeAddon), new object[] { 0 }),
                    new ReplaceEntry(typeof(HolidayTree), 0x1B7E, typeof(ChristmasTreeAddon), new object[] { 1 }),
                });
            e.Mobile.SendMessage("Patched {0} items.", count);
        }
        #endregion
        #region Grandfather War Horses
        /*
         * Log start : 11/24/2023 2:10:32 PM
            FactionWarHorse:Loc(1362,1760,13):Internal:Mob(a war horse)(0x0000039D):Unnamed region[1]::
            FactionWarHorse:Loc(1896,2094,5):Internal:Mob(slut)(0x000003A3):Unnamed region[1]::
            FactionWarHorse:Loc(573,2130,0):Internal:Mob(a war horse)(0x000003BF):Unnamed region[1]::
            FactionWarHorse:Loc(1921,2099,0):Internal:Mob(a war horse)(0x00001F61):Unnamed region[1]::
            FactionWarHorse:Loc(1921,2098,0):Internal:Mob(a war horse)(0x00001FA9):Unnamed region[1]::
            FactionWarHorse:Loc(1916,2099,0):Internal:Mob(a war horse)(0x00003724):Unnamed region[1]::
            FactionWarHorse:Loc(1929,2100,0):Internal:Mob(house)(0x00003921):Unnamed region[1]::
            FactionWarHorse:Loc(1929,2101,0):Internal:Mob(fest)(0x00003EAA):Unnamed region[1]::
            FactionWarHorse:Loc(2079,2315,0):Internal:Mob(Atreyu)(0x000046E5):Unnamed region[1]::
            FactionWarHorse:Loc(2635,49,45):Internal:Mob(Blue)(0x00005D28):Unnamed region[1]::
        ---
            SetStr(400);
            SetDex(125);
            SetInt(51, 55);
            SetHits(240);
            SetMana(0);
            SetDamage(5, 8);
            SetSkill(SkillName.MagicResist, 25.1, 30.0);
            SetSkill(SkillName.Tactics, 29.3, 44.0);
            SetSkill(SkillName.Wrestling, 29.3, 44.0);
         */
        private static void WarHorsesCrib(CommandEventArgs e)
        {   // mounts came from [FindMobileByType FactionWarHorse
            int[] mounts = new[] { 0x0000039D, 0x000003A3, 0x000003BF, 0x00001F61, 0x00001FA9, 0x00003724, 0x00003921, 0x00003EAA, 0x000046E5, 0x00005D28 };
            // log all their stats and other important info
            LogHelper logger = new LogHelper("War Horse crib.log", true, true, true);
            foreach (int ix in mounts)
                if (World.FindMobile((Serial)ix) is FactionWarHorse fwh)
                {
                    Type type = LookupWHType(fwh);              // type of war horse
                    Mobile master = LookupControlMaster(fwh);   // owner, either ControlMaster(ridden), or part of the owners Stabled pets
                    if (master != null)                         // null would be an error
                    {
                        logger.Log(string.Format("new WHCfg(typeof({0}),{1},\"{2}\",{3},{4},{5},{6}),", type.Name, fwh.RawInt, fwh.Name, master.Serial,
                            fwh.Skills[SkillName.MagicResist].Base,
                            fwh.Skills[SkillName.Tactics].Base,
                            fwh.Skills[SkillName.Wrestling].Base));
                    }
                }
            logger.Finish();
        }

        private static Mobile LookupControlMaster(FactionWarHorse fwh)
        {
            if (fwh.ControlMaster != null)
                return fwh.ControlMaster;

            List<BaseCreature> list = null;
            foreach (Mobile m in World.Mobiles.Values)
                if (AnimalTrainer.Table.TryGetValue(m, out list))
                    if (m is PlayerMobile pm)
                        if (list.Contains(fwh))
                            return pm;

            return null;
        }
        private static Type LookupWHType(FactionWarHorse fwh)
        {
            switch (fwh.ItemID)
            {
                default: return null;
                case 0x3EB1: return typeof(CoMWarHorse);
                case 0x3EAF: return typeof(MinaxWarHorse);
                case 0x3EB0: return typeof(SLWarHorse);
                case 0x3EB2: return typeof(TBWarHorse);
            }
        }
        /*
         * type CoMWarHorse
            int 53
            name a war horse
            control master 0x4624 "Zeke"
         */
        protected class WHCfg
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
        private static void GrandfatherWarHorses(CommandEventArgs e)
        {
            // data from "War Horse crib.log" (collected above)
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
                        {
                            AnimalTrainer.Table.Add(pm, new List<BaseCreature>() { pet });
                        }

                        pet.LastStableChargeTime = DateTime.UtcNow;

                        pet.SetCreatureBool(CreatureBoolTable.StableHold, true);
                        pet.StableBackFees = AnimalTrainer.UODayChargePerPet(pm);
                    }
                }
        }
        #endregion Grandfather War Horses
        #region Treasure Map Chest Inventory
        private static void TreasureMapChestInventory(CommandEventArgs e)
        {
            PlayerMobile from = e.Mobile as PlayerMobile;
            int level = 0;
            int.TryParse(e.GetString(1), out level);
            if (level <= 0 || level > 5)
            {
                from.SendMessage("Usage: TreasureMapChestInventory <level>");
                return;
            }
            LargeCrate lc = new LargeCrate();
            lc.MaxItems = 2048;
            lc.Movable = false;
            lc.MoveToWorld(from.Location, from.Map);

            for (int ix = 0; ix < 100; ix++)
            {
                TreasureMapChest chest = new TreasureMapChest(level);
                lc.AddItem(chest);
            }

            from.SendMessage("Done: Use the [inventory command to inventory all items.");
        }
        #endregion Treasure Map Chest Inventory
        #region Dungeon Chest Inventory
        private static void DungeonChestInventory(CommandEventArgs e)
        {
            PlayerMobile from = e.Mobile as PlayerMobile;
            int level = 0;
            int.TryParse(e.GetString(1), out level);
            if (level <= 0 || level > 4)
            {
                from.SendMessage("Usage: DungeonChestInventory <level>");
                return;
            }
            LargeCrate lc = new LargeCrate();
            lc.MaxItems = 2048;
            lc.Movable = false;
            lc.MoveToWorld(from.Location, from.Map);

            for (int ix = 0; ix < 100; ix++)
            {
                DungeonTreasureChest chest = new DungeonTreasureChest(level);
                lc.AddItem(chest);
            }

            from.SendMessage("Done: Use the [inventory command to inventory all items.");
        }
        #endregion Dungeon Chest Inventory
        #region Count Silver Weapons
        private static void CountSilverWeapons(CommandEventArgs e)
        {
            int count = 0;
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted) continue;
                if (item is BaseWeapon bw)
                {
                    if (bw.Slayer != SlayerName.Silver) continue;
                    count++;
                }

            }
            return;
        }
        #endregion Count Silver Weapons
        #region Count Player House Deeds
        private static void CountPlayerHouseDeeds(CommandEventArgs e)
        {
            int count = 0;
            Dictionary<Item, string> table = new();
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted) continue;
                if (item is StaticDeed) continue;
                if (item is TentBag) continue;
                if (item is HouseDeed)
                {
                    bool case1 = false;
                    bool case2 = false;
                    bool case3 = false;
                    BaseHouse bh = null;
                    TownshipRegion itsr = null;
                    // on the player, in their bank box etc.
                    if (case1 = (item.RootParent is PlayerMobile))
                    {
                        if ((item.RootParent as PlayerMobile).AccessLevel != AccessLevel.Player)
                        {   // don't count staff owned
                            case1 = false;
                        }
                        else
                            ;
                    }
                    // in their house
                    else if (case2 = (bh = BaseHouse.FindHouseAt(item)) != null)
                    {
                        if (bh.Owner != null && bh.Owner.AccessLevel > AccessLevel.Player)
                        {   // don't count staff owned
                            case2 = false;
                        }
                        else
                            ;
                    }
                    // in their township
                    else if (case3 = (itsr = TownshipRegion.GetTownshipAt(item.GetWorldLocation(), item.Map)) != null)
                    {
                        ;
                    }
                    ;
                    ;
                    if (case1 || case2 || case3)
                    {
                        table.Add(item, String.Format("{0}:{1}", item.GetWorldLocation(), item.Map));
                        count++;
                    }
                    ;

                }
            }
            return;
        }
        #endregion Count Player House Deeds
        #region Get Multi Offset
        private static void GetMultiOffset(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target the multi you wish as the center for your offsets...");
            e.Mobile.Target = new MultiTarget();
        }

        public class MultiTarget : Target
        {
            public MultiTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;
                if (loc != Point3D.Zero)
                {
                    from.SendMessage("Select where you would like to place a component...");
                    from.Target = new NextTarget(loc);
                }
            }
        }
        public class NextTarget : Target
        {
            Point3D m_center;
            public NextTarget(Point3D center)
                : base(17, true, TargetFlags.None)
            {
                m_center = center;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;
                if (loc != Point3D.Zero)
                {
                    int x = (m_center.X > loc.X) ? -(m_center.X - loc.X) : (loc.X > m_center.X) ? (loc.X - m_center.X) : 0;
                    int y = (m_center.Y > loc.Y) ? -(m_center.Y - loc.Y) : (loc.Y > m_center.Y) ? (loc.Y - m_center.Y) : 0;
                    int z = (m_center.Z > loc.Z) ? -(m_center.Z - loc.Z) : (loc.Z > m_center.Z) ? (loc.Z - m_center.Z) : 0;
                    string text = string.Format("{0}, {1}, {2}", x, y, z);
                    from.SendMessage(text);
                    TextCopy.ClipboardService.SetText(text);
                }
            }
        }
        #endregion Get Multi Offset
        #region ObsidianStats
        private static void ObsidianStats(CommandEventArgs e)
        {
            e.Mobile.SendMessage("There are a total of {0} obsidian weapons, {1} of which are imbued.", DefObsidian.System.CountByType(typeof(BaseWeapon)), DefObsidian.System.CountImbuedByType(typeof(BaseWeapon)));
            e.Mobile.SendMessage("There are a total of {0} obsidian statues, {1} of which are imbued.", DefObsidian.System.CountByType(typeof(BaseStatue)), DefObsidian.System.CountImbuedByType(typeof(BaseStatue)));

            List<Item> items = DefObsidian.System.Registry;

            try
            {
                using (StreamWriter writer = new StreamWriter(Path.Combine(Core.LogsDirectory, "obsidianstats.log"), false))
                {
                    foreach (Item item in items)
                    {
                        writer.WriteLine("{0} ({1}), MagicEffect={2}, MagicCharges={3}, Slayer={4}",
                            item.ToString(),
                            item.GetType().Name,
                            (item is IMagicItem ? ((IMagicItem)item).MagicEffect : MagicItemEffect.None),
                            (item is IMagicItem ? ((IMagicItem)item).MagicCharges : 0),
                            (item is BaseWeapon ? ((BaseWeapon)item).Slayer : SlayerName.None));
                    }
                }
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage(ex.ToString());
            }
        }
        #endregion
        #region Origin Report
        private static void OriginReport(CommandEventArgs e)
        {
            List<Item> weapons = new();
            List<Item> armor = new();
            List<Item> jewels = new();
            List<Item> wands = new();
            List<Item> clothing = new();

            foreach (Item item in World.Items.Values)
                if (item != null && item.Deleted != true)
                //if (DateTime.UtcNow - item.Created > TimeSpan.FromMinutes(10))
                //continue ;
                //else
                {
                    bool care = false;
                    if (item is BaseWeapon bw && item is not BaseWand)
                    {
                        care = (bw.AccuracyLevel > WeaponAccuracyLevel.Regular || bw.DamageLevel > WeaponDamageLevel.Regular || bw.DurabilityLevel > WeaponDurabilityLevel.Regular) ||
                            (bw.MagicCharges > 0 || bw.Slayer != SlayerName.None);
                        if (care)
                            weapons.Add(item);
                    }
                    else if (item is BaseArmor ba)
                    {
                        care = (ba.ProtectionLevel > ArmorProtectionLevel.Regular || ba.DurabilityLevel > ArmorDurabilityLevel.Regular) ||
                            (ba.MagicCharges > 0);

                        if (care)
                            armor.Add(item);
                    }
                    else if (item is BaseJewel bj)
                    {
                        care = (bj.MagicCharges > 0);
                        if (care)
                            jewels.Add(item);
                    }
                    else if (item is BaseWand bwnd)
                    {
                        care = (bwnd.MagicCharges > 0);
                        if (care)
                            wands.Add(item);
                    }
                    else if (item is BaseClothing bc)
                    {
                        care = (bc.MagicCharges > 0);
                        if (care)
                            clothing.Add(item);
                    }
                }
            /*
            Unknown = 0x00,
            *Monster = 0x01,     // monster corpse
            *Chest   = 0x02,     // treasure chests of some type (not treasure map, or dungeon chest)
            Player  = 0x04,     // player created
            Scroll  = 0x08,     // monster loot or chest loot converted to scroll
            Legacy  = 0x10,     // legacy magic loot
            BOD     = 0x20,     // BOD reward
            *TChest = 0x40,      // treasure map chests 
            *DChest = 0x80,      // Dungeon treasure chests 
             */

            LogHelper logger = new LogHelper("Origin Report.log", overwrite: true, sline: true, quiet: true);
            logger.Log("** Magic Item Origins **");
            int total = weapons.Count() + armor.Count() + jewels.Count() + wands.Count() + clothing.Count();
            logger.Log(string.Format("{0} Total Magic Items", total));
            logger.Log("");
            DumpOut(logger, "* Weapons *", weapons);
            DumpPercent(logger, "", weapons); logger.Log("");
            DumpOut(logger, "* Armor *", armor);
            DumpPercent(logger, "", armor); logger.Log("");
            DumpOut(logger, "* Jewels *", jewels);
            DumpPercent(logger, "", jewels); logger.Log("");
            DumpOut(logger, "* Wands *", wands);
            DumpPercent(logger, "", wands); logger.Log("");
            DumpOut(logger, "* Clothing *", clothing);
            DumpPercent(logger, "", clothing); logger.Log("");
            logger.Finish();

            // summary 
            DumpOut(null, "* Weapons *", weapons, e.Mobile);
            DumpPercent(null, "", weapons, e.Mobile);
        }
        private static void DumpOut(LogHelper logger, string headding, List<Item> list, Mobile from = null)
        {
            if (logger != null)
            {
                logger.Log(headding);
                logger.Log(string.Format("Monster: {0}", list.Where(s => s.Origin.HasFlag(Genesis.Monster)).Count()));
                logger.Log(string.Format("Chest: {0}", list.Where(s => s.Origin.HasFlag(Genesis.Chest)).Count()));
                logger.Log(string.Format("Treasure Chest: {0}", list.Where(s => s.Origin.HasFlag(Genesis.TChest)).Count()));
                logger.Log(string.Format("Dungeon Chest: {0}", list.Where(s => s.Origin.HasFlag(Genesis.DChest)).Count()));
            }

            if (from != null)
            {
                from.SendMessage(headding);
                from.SendMessage(string.Format("Monster: {0}", list.Where(s => s.Origin.HasFlag(Genesis.Monster)).Count()));
                from.SendMessage(string.Format("Chest: {0}", list.Where(s => s.Origin.HasFlag(Genesis.Chest)).Count()));
                from.SendMessage(string.Format("Treasure Chest: {0}", list.Where(s => s.Origin.HasFlag(Genesis.TChest)).Count()));
                from.SendMessage(string.Format("Dungeon Chest: {0}", list.Where(s => s.Origin.HasFlag(Genesis.DChest)).Count()));
            }
        }
        private static void DumpPercent(LogHelper logger, string headding, List<Item> list, Mobile from = null)
        {
            if (logger != null)
            {
                logger.Log(headding);
                logger.Log(string.Format("Monster: {0:F2}%",
                    ((double)list.Where(s => s.Origin.HasFlag(Genesis.Monster)).Count() / list.Count()) * 100.0
                    ));
                logger.Log(string.Format("Chest: {0:F2}%",
                    ((double)list.Where(s => s.Origin.HasFlag(Genesis.Chest)).Count() / list.Count()) * 100.0
                    ));
                logger.Log(string.Format("Treasure Chest: {0:F2}%",
                    ((double)list.Where(s => s.Origin.HasFlag(Genesis.TChest)).Count() / list.Count()) * 100.0
                    ));
                logger.Log(string.Format("Dungeon Chest: {0:F2}%",
                    ((double)list.Where(s => s.Origin.HasFlag(Genesis.DChest)).Count() / list.Count()) * 100.0
                    ));
            }

            if (from != null)
            {
                from.SendMessage(headding);
                from.SendMessage(string.Format("Monster: {0:F2}%",
                    ((double)list.Where(s => s.Origin.HasFlag(Genesis.Monster)).Count() / list.Count()) * 100.0
                    ));
                from.SendMessage(string.Format("Chest: {0:F2}%",
                    ((double)list.Where(s => s.Origin.HasFlag(Genesis.Chest)).Count() / list.Count()) * 100.0
                    ));
                from.SendMessage(string.Format("Treasure Chest: {0:F2}%",
                    ((double)list.Where(s => s.Origin.HasFlag(Genesis.TChest)).Count() / list.Count()) * 100.0
                    ));
                from.SendMessage(string.Format("Dungeon Chest: {0:F2}%",
                    ((double)list.Where(s => s.Origin.HasFlag(Genesis.DChest)).Count() / list.Count()) * 100.0
                    ));
            }
        }
        /*
            10 out of 80 is what %?

            10 out of 80 is P%

            Equation: Y/X = P%

            Solving our equation for P
            P% = Y/X
            P% = 10/80
            p = 0.125

            Convert decimal to percent:
            P% = 0.125 * 100 = 12.5%
         */
        #endregion Origin Report
        #region Replace Large BODS with Small
        private static void ConvertLargeToSmallBODs(CommandEventArgs e)
        {
            int count = 0;
            foreach (Item item in World.Items.Values)
                if (item is LargeBOD lb)
                {
                    Mobile admin = World.GetAdminAcct();
                    if (admin != null)
                    {
                        SetAllSkills(admin, 70.1);
                        SmallBOD bo = lb.System.ConstructSBOD(0, false, BulkMaterialType.None, 0, null, 0, 0);
                        bo.Randomize(admin);
                        SetAllSkills(admin, 100.0);
                        Utility.ReplaceItem(new_item: bo, oldItem: item, copy_properties: false);
                        item.Delete();
                        count++;
                    }
                }

            e.Mobile.SendMessage("{0} BODs converted", count);
        }
        public static void SetAllSkills(Mobile target, double skill)
        {
            if (target is PlayerMobile player)
            {
                Server.Skills skills = player.Skills;
                for (int i = 0; i < skills.Length; ++i)
                    skills[i].Base = skill;
            }
        }
        #endregion Replace Large BODS with Small
        #region InitCraftList
        private static void InitCraftList(CommandEventArgs e)
        {
            CraftSystem temp;
            temp = DefAlchemy.CraftSystem;
            temp = DefBlacksmithy.CraftSystem;
            temp = DefBowFletching.CraftSystem;
            temp = DefCarpentry.CraftSystem;
            temp = DefCartography.CraftSystem;
            temp = DefCooking.CraftSystem;
            temp = DefGlassblowing.CraftSystem;
            temp = DefInscription.CraftSystem;
            temp = DefMasonry.CraftSystem;
            temp = DefTailoring.CraftSystem;
            temp = DefTinkering.CraftSystem;
            temp = DefTownshipCraft.CraftSystem;
            if (CraftSystem.IsCraftable(new WarHammer()))
                ;
            else
                ;

            if (CraftSystem.IsCraftable(new BoneHelm()))
                ;
            else
                ;

        }
        #endregion InitCraftList
        #region Find Skill
        private static void FindSkill(CommandEventArgs e)
        {
            LogHelper logger = new LogHelper("Find Skill.Log", true, true, true);
            try
            {
                string skillname = e.GetString(1);
                int score;
                string real_name = IntelligentDialogue.Levenshtein.BestEnumMatch(typeof(SkillName), skillname, out score);
                if (real_name != null && real_name != "" && score <= 5)
                { // found the skill you want to learn about
                    List<(Mobile m, SkillName s, double v, DateTime d)> list = new();
                    foreach (var m in World.Mobiles.Values)
                        if (m is PlayerMobile pm)
                        {
                            Server.Skills skills = pm.Skills;
                            SkillName s = (Server.SkillName)Enum.Parse(typeof(Server.SkillName), real_name, true);
                            Accounting.Account acct = (pm.Account as Accounting.Account);
                            if (acct != null && pm.AccessLevel == AccessLevel.Player && acct.AccessLevel == AccessLevel.Player)
                                list.Add((pm, s, skills[s].Base, acct.LastLogin));
                            else
                                ;
                        }

                    var sortResult = list.OrderByDescending(a => a.Item3).ThenBy(a => a.Item4).ToList();
                    foreach (var record in sortResult)
                        logger.Log(string.Format("{0}, {1}, {2}, {3}", record.m, record.s, record.v, record.d));
                }
                else
                {
                    e.Mobile.SendMessage("Skill {0} not found", e.GetString(1));
                    return;
                }
            }
            catch { }
            finally { logger.Finish(); }
        }
        #endregion Find Skill
        #region Get Pets
        private static void GetPets(CommandEventArgs e)
        {
            foreach (var m in World.Mobiles.Values)
                if (m is BaseCreature bc && bc.Deleted == false && bc.Map != Map.Internal && bc.Map != null)
                    if (bc.Controlled && bc.ControlMaster == e.Mobile)
                        bc.MoveToWorld(e.Mobile.Location, e.Mobile.Map);
        }
        #endregion Get Pets
        #region WipeAllTownships
        private static void WipeAllTownships(CommandEventArgs e)
        {
            List<TownshipStone> stones = new();
            foreach (var tss in TownshipStone.AllTownshipStones)
                if (tss is TownshipStone && tss.Deleted == false)
                    stones.Add(tss);

            foreach (TownshipStone stone in stones)
                stone.Delete();
        }
        #endregion WipeAllTownships
        #region Grab
        private static void Grab(CommandEventArgs e)
        {// grab itemIDs and z locations (could be enhanced to get relative locations of nearby items)

            e.Mobile.SendMessage("Target items...");
            e.Mobile.Target = new GrabTarget(); // Call our target
        }

        public class GrabTarget : Target
        {
            public GrabTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                string text = string.Empty;
                if (target is Item item)
                {
                    IPooledEnumerable eable = Map.Felucca.GetItemsInRange(item.Location, 0);
                    foreach (Item thing in eable)
                        if (thing != null && thing.Deleted == false)
                            text += string.Format("0x{0:X} {1}, ", thing.ItemID, thing.Z);
                    eable.Free();
                }
                else
                {
                    from.SendMessage("That is not an item.");
                    return;
                }

                from.SendMessage("'{0}' copied to the clipboard.", text);
                TextCopy.ClipboardService.SetText(text);
            }
        }
        #endregion Grab
        #region Save GridSpawners
        private static void SaveGridSpawners(CommandEventArgs e)
        {
            List<Spawner> list = new();
            foreach (Item item in World.Items.Values)
                if (item is null || item.Deleted || item.Parent != null || item.Movable)
                    continue;
                else if (item is GridSpawner gs)
                {
                    list.Add(gs);
                }
            List<string> reasons = null;
            SpawnerCapture.SaveSpawners(list, filename: "GridSpawners", ref reasons);
            e.Mobile.SendMessage("{0} grid spawners saved", list.Count);
        }
        #endregion Save GridSpawners
        #region Load GridSpawners
        private static void LoadGridSpawners(CommandEventArgs e)
        {
            List<Spawner> list = new();
            List<string> reasons = null;
            SpawnerCapture.LoadSpawners(list, filename: "GridSpawners", ref reasons);
            e.Mobile.SendMessage("{0} spawners loaded", list.Count);
            if (list.Count > 0)
            {
                e.Mobile.SendMessage("Activating {0} grid spawners...", list.Count);
                foreach (Spawner spawner in list)
                {
                    spawner.MoveToWorld(spawner.Location, map: spawner.Map);
                    spawner.ScheduleRespawn = true;
                }
            }
            e.Mobile.SendMessage("done.");
        }
        #endregion Load GridSpawners
        #region Stack Overflow Testing
        private static void StackOverflowTest(CommandEventArgs e)
        {
            ConsoleWriteLine("Begin Stack Overflow Exception Test", ConsoleColor.Red);
            //try
            {
                CrashItBaby(1234);
            }
            //catch (StackOverflowException soe)
            {
                //ConsoleOut("Stack Overflow Exception Caught {0}", ConsoleColor.Red, soe.Message);
            }

            ConsoleWriteLine("End Stack Overflow Exception Test", ConsoleColor.Red);
        }
        private static Int64 CrashItBaby(Int64 foo)
        {
            return CrashItBaby(foo++);
        }
        #endregion Stack Overflow Testing
        #region QueryAngryMiners
        private static void QueryAngryMiners(CommandEventArgs e)
        {
            int campNo = 0;
            foreach (var camp in World.Items.Values)
                if (camp is AngryMinerCampRare amc && amc.Deleted == false)
                {

                    ++campNo;
                    foreach (Item item in amc.ItemComponents)
                    {
                        if (item != null)
                        {
                            if (amc.IsIngotStack(item.GetType()))
                            {
                                int hue = (item.GetItemBool(Item.ItemBoolTable.MustSteal) == false) ? 0x40 : 0x3B2;
                                string text = string.Format("[{0}] ", campNo);
                                text += string.Format("item={0}, Movable={1}, MustSteal={2}, LootType={3},",
                                    item.GetType().Name, item.Movable, item.GetItemBool(Item.ItemBoolTable.MustSteal), item.LootType);
                                e.Mobile.SendMessage(hue, text);
                            }
                            else if (item.GetType() == typeof(MagicBox))
                            {
                                foreach (Item reward in (item as BaseContainer).Items)
                                    if ((reward is AncientSmithyHammer || reward is GargoylesPickaxe))
                                    {
                                        int hue = (reward.GetItemBool(Item.ItemBoolTable.MustSteal) == false) ? 0x40 : 0x3B2;
                                        string text = string.Format("[{0}] ", campNo);
                                        text += string.Format("item={0}, Movable={1}, MustSteal={2}, LootType={3},",
                                            reward.GetType().Name, reward.Movable, reward.GetItemBool(Item.ItemBoolTable.MustSteal), reward.LootType);
                                        e.Mobile.SendMessage(hue, text);
                                    }
                            }


                        }


                    }
                }

            e.Mobile.SendMessage("{0} camps found", campNo);
        }
        #endregion QueryAngryMiners
        #region TestCraftItem
        private static void TestCraftItem(CommandEventArgs e)
        {
            LogHelper logger = new LogHelper(Path.Combine(Core.DataDirectory, "TCItemsWhitelist.log"), false, true, true);
            List<CraftItem> craftItems = new List<CraftItem>();
            craftItems.AddRange(DefAlchemy.CraftSystem.CraftItems.Cast<CraftItem>().ToList());
            craftItems.AddRange(DefBlacksmithy.CraftSystem.CraftItems.Cast<CraftItem>().ToList());
            craftItems.AddRange(DefBowFletching.CraftSystem.CraftItems.Cast<CraftItem>().ToList());
            craftItems.AddRange(DefCarpentry.CraftSystem.CraftItems.Cast<CraftItem>().ToList());
            craftItems.AddRange(DefCartography.CraftSystem.CraftItems.Cast<CraftItem>().ToList());
            craftItems.AddRange(DefCooking.CraftSystem.CraftItems.Cast<CraftItem>().ToList());
            craftItems.AddRange(DefGlassblowing.CraftSystem.CraftItems.Cast<CraftItem>().ToList());
            craftItems.AddRange(DefInscription.CraftSystem.CraftItems.Cast<CraftItem>().ToList());
            craftItems.AddRange(DefMasonry.CraftSystem.CraftItems.Cast<CraftItem>().ToList());
            craftItems.AddRange(DefTailoring.CraftSystem.CraftItems.Cast<CraftItem>().ToList());
            craftItems.AddRange(DefTinkering.CraftSystem.CraftItems.Cast<CraftItem>().ToList());

            foreach (CraftItem ci in craftItems)
            {
                // log this type
                logger.Log(ci.ItemType.Name);
            }
            logger.Finish();
        }
        #endregion TestCraftItem
        #region Check Strom Restoration
        private static void CheckStromRestoration(CommandEventArgs e)
        {
            // our recovery container (not in anyones list)
            int root = 0x4001E408;
            // recovery chest
            List<int> chest = new List<int>
            {
                {0x4001E408},
                {0x4002E04F},
                {0x4002E56E},
                {0x4002C598},
                {0x4002DC13},
                {0x400B71B0},
                {0x4002D34B},
                {0x4002DFBA},
                {0x4002DEFE},
                {0x4002D9A8},
                {0x4004ACEB},
                {0x4002DE32},
                {0x4002D8AD},
                {0x400B70F2},
                {0x40050292},
                {0x4002D85E},
                {0x400502B1},
                {0x4007EA82},
                {0x4005C18A},
                {0x40049F6E},
                {0x40116F84},
                {0x4002A161},
                {0x40114DA8},
                {0x4009A1BA},
                {0x4002E4EF},
                {0x400B770B},
                {0x40045338},
                {0x40063E02},
                {0x4006C355},
                {0x40050587},
                {0x400B7456},
                {0x400B6FB8},
                {0x4002E0E1},
                {0x40104E83},
                {0x4010A297},
                {0x4011C77E},
                {0x4006C358},
                {0x400B7624},
                {0x4007D21B},
                {0x40071765},
                {0x4001C67A},
                {0x4003FA47},
                {0x4006E389},
                {0x4003FA4D},
                {0x400711AA},
                {0x40116283},
                {0x40074656},
                {0x40034782},
                {0x4003465A},
                {0x40071320},
                {0x402705CD},
                {0x4018BCA4},
                {0x4003FA45},
                {0x400FE320},
                {0x4003FA52},
                {0x4003FA49},
                {0x4007C81A},
                {0x4003FA48},
                {0x4006E379},
                {0x4003FA46},
                {0x400716C4},
                {0x400332BA},
                {0x4009F51B},
                {0x400A4D51},
                {0x402B543C},
                {0x400CD3B7},
                {0x40022F32},
                {0x40037F0E},
                {0x4007E944},
                {0x40037F03},
                {0x400A101B},
                {0x400A099B},
                {0x4009A93B},
                {0x4004517F},
                {0x400CCF47},
                {0x400A5143},
                {0x4009AAFE},
                {0x400ECAA5},
                {0x4002CBEF},
                {0x400FF50E},
                {0x400FE7F2},
                {0x40082B16},
                {0x400F8B75},
                {0x400F9E91},
                {0x40116325},
                {0x40083DB5},
                {0x40018593},
                {0x4002F1BC},
                {0x400B4ADD},
                {0x40019AA3},
                {0x400D2E1E},
                {0x4010AF90},
                {0x40027954},
                {0x4006DBB5},
                {0x400E203B},
                {0x40085877},
                {0x4000C057},
                {0x400E07D4},
                {0x40081CC4},
                {0x400826DB},
                {0x40085C6E},
                {0x40011888},
                {0x400118B4},
                {0x40011A3D},
                {0x40011A3B},
                {0x4008C1C9},
                {0x40085C69},
                {0x400A7E61},
                {0x400B162B},
                {0x400B6604},
                {0x40095726},
                {0x400936B9},
                {0x40051ED4},
                {0x400F6A4C},
                {0x4005587F},
                {0x4010DD03},
                {0x4004CFF5},
                {0x40093715},
                {0x40115DCC},
                {0x400936A5},
                {0x40093612},
                {0x40051E30},
                {0x4009356A},
                {0x4010AF72},
                {0x4010B49C},
                {0x400AB663},
                {0x400DD639},
                {0x40081533},
                {0x4009B289},
                {0x400ADE37},
                {0x4013CF35},
                {0x4003655B},
                {0x400E51FF},
                {0x4001EEC0},
                {0x40031A4E},
                {0x400F218B},
                {0x400B05A8},
                {0x400A3A41},
                {0x400862EF},
                {0x40046EEB},
                {0x400229C3},
                {0x4001BA5C},
                {0x400492B8},
                {0x40044737},
                {0x40049B18},
                {0x400A3DCE},
                {0x40048489},
                {0x4001BF67},
                {0x40082CEE},
                {0x4004A0CE},
                {0x40035662},
                {0x400909D9},
                {0x4002FB08},
                {0x40071043},
                {0x400E1634},
                {0x40082EE1},
                {0x400916F7},
                {0x400921E7},
                {0x40044CF9},
                {0x4005299F},
                {0x40050368},
                {0x400B7394},
                {0x4006A2D6},
                {0x40026CD5},
                {0x400D5BE2},
                {0x4004A419},
                {0x40039335},
                {0x40039F80},
                {0x40038932},
                {0x40039BE6},
                {0x400A4039},
                {0x400313CB},
                {0x40109AAC},
                {0x400EDDC3},
                {0x40081447},
                {0x40021A8F},
                {0x400219CB},
                {0x4010AF9D},
                {0x4010AFC4},
                {0x4010AF9B},
                {0x4001F367},
                {0x40058580},
                {0x400783DD},
                {0x400CCF7B},
                {0x400444B9},
                {0x4007DAF0},
                {0x40081287},
                {0x400790BA},
                {0x400B1FCD},
                {0x400B13A5},
                {0x4006BC2C},
                {0x4008D95B},
                {0x4008FBBB},
                {0x4012B2C0},
                {0x4002A783},
                {0x4002B2C9},
                {0x4002B323},
                {0x4002A835},
                {0x400231B6},
                {0x400F8B74},
                {0x400AE597},
                {0x400586F5},
                {0x40077476},
                {0x400435C6},
                {0x400AD8C6},
                {0x40043B48},
                {0x400AF2BB},
                {0x40044A1A},
                {0x400AD296},
                {0x40041B9F},
                {0x400AD563},
                {0x400DCCD7},
                {0x4004422C},
                {0x40043FB5},
                {0x400DBCC4},
                {0x400DCE0B},
                {0x40093D74},
                {0x400938CB},
                {0x400DC457},
                {0x400940D9},
                {0x400E0DAE},
                {0x4002D246},
                {0x401D0D97},
                {0x40028CF9},
                {0x4008C36F},
                {0x40087374},
                {0x40062E12},
                {0x40029E17},
                {0x40029D29},
                {0x40029C38},
                {0x40029D3B},
                {0x40029380},
                {0x40029347},
                {0x40028C4B},
                {0x400289FB},
                {0x40028C89},
                {0x40028D42},
                {0x40028A28},
                {0x40029C7B},
                {0x40028BC2},
                {0x4002095B},
                {0x4005B8F1},
                {0x4004A65E},
                {0x40060E32},
                {0x4004D1AE},
                {0x400B6777},
                {0x400B1B75},
                {0x4006C474},
                {0x4003F7BF},
                {0x40107BCE},
                {0x40066B54},
                {0x400374FF},
                {0x40093CE0},
                {0x40104F67},
                {0x40105AC5},
                {0x40105AAD},
                {0x40105AC3},
                {0x40105AB5},
                {0x40105AB4},
                {0x40105AA5},
                {0x40105ABB},
                {0x4002A9FC},
                {0x4002B18A},
                {0x4002A936},
                {0x4002B385},
                {0x4002A912},
                {0x4000C16F},
                {0x4002B1F7},
                {0x400E4DF7},
                {0x40149059},
                {0x401489CB},
                {0x401489D2},
                {0x401489C6},
                {0x401489CE},
                {0x401489C8},
                {0x40149068},
                {0x4005E2E4},
                {0x40060559},
                {0x40060930},
                {0x40060A47},
                {0x40060A81},
                {0x40060AB1},
                {0x40060CF9},
                {0x4006122E},
                {0x4005E32B},
                {0x4005E4B5},
                {0x4005E50B},
                {0x4005E578},
                {0x4005E5BB},
                {0x40060ADE},
                {0x40060AE7},
                {0x40061268},
                {0x4005E65A},
                {0x4005E6DF},
                {0x4005E9F2},
                {0x4005EA40},
                {0x4005EAB3},
                {0x4005EB43},
                {0x40060DB1},
                {0x40060DCB},
                {0x4005EBDB},
                {0x4005EC7C},
                {0x4005ED28},
                {0x4005ED83},
                {0x4005EE27},
                {0x4005EEA4},
                {0x4005EFCE},
                {0x4005F073},
                {0x4005F16E},
                {0x4005F222},
                {0x4005F2DD},
                {0x4005F3A4},
                {0x4005F41D},
                {0x4005F4B3},
                {0x4005F553},
                {0x4005F5BB},
                {0x4005F656},
                {0x4005F6AB},
                {0x4005F6FC},
                {0x4005F741},
                {0x4005F7D1},
                {0x4005F879},
                {0x4005F8D0},
                {0x40060C45},
                {0x4005F99F},
                {0x4005FA2C},
                {0x4005FA9C},
                {0x4005FB37},
                {0x4005FC17},
                {0x4005FC72},
                {0x401BE4AE},
                {0x400D31BC},
                {0x40119B7A},
                {0x401AAC78},
                {0x4010AFF0},
                {0x4006DA9D},
                {0x401FB404},
                {0x401986F8},
                {0x400BB1BC},
                {0x402B01D0},
                {0x40156755},
                {0x4000E598},
                {0x400374E8},
                {0x4029AE95},
                {0x40037196},
                {0x4029CFCE},
                {0x4003717D},
                {0x4006AA7E},
                {0x401619E4},
                {0x4014572E},
                {0x40037EBA},
                {0x40037F8D},
                {0x40037F88},
                {0x40037F91},
                {0x401FA4B5},
                {0x40068969},
                {0x4016F5B5},
                {0x40062434},
            };
            chest.Remove(root);
            // house before recovery
            List<int> house = new()
            {
                {0x4004B1D5},
                {0x4016C56A},
                {0x400E19D6},
                {0x4005E436},
                {0x40062431},
                {0x400CF503},
                {0x400832D2},
                {0x4013D510},
                {0x400A7A47},
                {0x4016CBFC},
                {0x4016CC00},
                {0x4016D917},
                {0x400BD221},
                {0x401C6951},
                {0x40032289},
                {0x40034096},
                {0x40034097},
                {0x400340BD},
                {0x400340BF},
                {0x400340C0},
                {0x400340C3},
                {0x400340C4},
                {0x400340C7},
                {0x400D9C8D},
                {0x400E5A94},
                {0x400DB0E8},
                {0x40060749},
                {0x4001E6B2},
                {0x4020F4DF},
                {0x400C8B18},
                {0x4002150C},
                {0x40021377},
                {0x40022A00},
                {0x40024923},
                {0x40018120},
                {0x40020667},
                {0x40103A83},
                {0x40127951},
                {0x400A5B7F},
                {0x4008632B},
                {0x4009CCFA},
                {0x400A998D},
                {0x400AA1E5},
                {0x40097880},
                {0x400229FF},
                {0x40047E9B},
                {0x40040F15},
                {0x40086E6E},
                {0x40041831},
                {0x4003B2A0},
                {0x4003667C},
                {0x40086E35},
                {0x4003515E},
                {0x40086E40},
                {0x4003408C},
                {0x400C8C50},
                {0x40040F1B},
                {0x400DF39F},
                {0x400DF3A4},
                {0x400DF39B},
                {0x400DF39E},
                {0x400DF3AD},
                {0x40033D46},
                {0x40146DEC},
                {0x400AB821},
                {0x4008D222},
                {0x400DF3A2},
                {0x400BDF29},
                {0x40036512},
                {0x400DF39A},
                {0x40033D41},
                {0x402BF6A2},
                {0x40132CF0},
                {0x40132D18},
                {0x40132CEE},
                {0x40132CEA},
                {0x40132CE9},
                {0x40173F19},
                {0x40029969},
                {0x400CFF33},
                {0x4002AE37},
                {0x4001B538},
                {0x40030BC2},
                {0x40085258},
                {0x400C4D63},
                {0x4003202B},
                {0x4002C726},
                {0x400983CB},
                {0x400BD96A},
                {0x4008D742},
                {0x400A0B03},
                {0x40046EA2},
                {0x402AF1D4},
                {0x40036E0F},
                {0x400E7871},
                {0x40140F58},
                {0x4001B57D},
                {0x4007DA43},
                {0x400B9960},
                {0x4006DAED},
                {0x40073C49},
                {0x40092719},
                {0x400A0B04},
                {0x4017B246},
                {0x400D8503},
                {0x4007E5E4},
                {0x4004BBCC},
                {0x40062432},
                {0x4002E88D},
                {0x4014905C},
                {0x400D38B0},
                {0x400358E4},
                {0x400CD00A},
                {0x40035E15},
                {0x4010C518},
                {0x400A005E},
                {0x4017632E},
                {0x40175E00},
                {0x400CCD27},
                {0x400210A7},
                {0x400EF1F9},
                {0x400AFFE1},
                {0x400AFFE0},
                {0x4005E182},
                {0x40135C82},
                {0x4020498E},
                {0x400F5550},
                {0x40027674},
                {0x400DC2F8},
                {0x4016C7FD},
                {0x400ADD5B},
                {0x401C9F18},
                {0x4000A155},
                {0x400D89F8},
                {0x400E0C72},
                {0x400EB0A8},
                {0x40141D43},
                {0x400C2FB8},
                {0x400C17AE},
                {0x4013B2E1},
                {0x4016E62E},
                {0x40147B99},
                {0x400957DD},
                {0x4003D5C4},
                {0x400957C4},
                {0x400957DB},
                {0x400E5112},
                {0x400F195C},
                {0x400A5443},
                {0x400E73D3},
                {0x401816DE},
                {0x400C68F2},
                {0x400957E1},
                {0x400EE6F0},
                {0x400EE734},
                {0x400EE6DF},
                {0x4001B887},
                {0x40022CBD},
                {0x40022CAB},
                {0x40022CBA},
                {0x40022C8D},
                {0x4003180A},
                {0x400EE9A1},
                {0x40022CC6},
                {0x4004EDBA},
                {0x4004F20F},
                {0x400EE70E},
                {0x400EE89E},
                {0x400EE6E4},
                {0x400EE6DC},
                {0x400EE5D9},
                {0x40032925},
                {0x400327AE},
                {0x4004EC8F},
                {0x400EE5CE},
                {0x4016FC55},
                {0x40039D10},
                {0x40095BC6},
                {0x400B44AA},
                {0x400424FE},
                {0x4003376A},
                {0x4008B56B},
                {0x40082C28},
                {0x400FB2E6},
                {0x40020902},
                {0x400F1EC2},
                {0x400F1F82},
                {0x40020D7A},
                {0x400EFC38},
                {0x400EFC49},
                {0x400EFBCD},
                {0x400EFAC4},
                {0x40095876},
                {0x400EFA92},
                {0x400EFA8A},
                {0x400EFC0F},
                {0x400EFB01},
                {0x400EFAE1},
                {0x400EFB1C},
                {0x400EFC5A},
                {0x400EFB21},
                {0x40087981},
                {0x400EF996},
                {0x4006B486},
                {0x4006E13C},
                {0x40071893},
                {0x4019BB71},
                {0x40084AAD},
                {0x4008C374},
                {0x4008C37A},
                {0x4008C37B},
                {0x4008E6AB},
                {0x40131EEE},
                {0x40132065},
                {0x40137C32},
                {0x40137CC5},
                {0x40137FFF},
                {0x40028F04},
                {0x4002914D},
                {0x40029750},
                {0x40029780},
                {0x4002A5A3},
                {0x4015C531},
                {0x40036578},
                {0x4003272D},
                {0x400D66A8},
                {0x400D66A7},
                {0x4002E8D2},
                {0x40018429},
                {0x4002D0B1},
                {0x40142038},
                {0x4006F14B},
                {0x400BD242},
                {0x40120AEC},
                {0x400BFE34},
                {0x400BD234},
                {0x4002D0D3},
                {0x40016EBE},
                {0x40165FED},
                {0x4009609D},
                {0x4002E3DC},
                {0x40049CB9},
                {0x4008B554},
                {0x400D352D},
                {0x400A6257},
                {0x40040610},
                {0x4002E595},
                {0x40093CBF},
                {0x40029C9D},
                {0x40029D0C},
                {0x40029A97},
                {0x400461A2},
                {0x40035535},
                {0x400355B5},
                {0x400EBA26},
                {0x400EB4C6},
                {0x4002E8D1},
                {0x4002E8DF},
                {0x4002E8D5},
                {0x40022B2B},
                {0x4002E8E2},
                {0x4002E8D0},
                {0x4002E8D8},
                {0x4002E8E0},
                {0x4002E8CF},
                {0x4002E8E1},
                {0x4002E8DB},
                {0x4002E8E4},
                {0x4002E8D7},
                {0x4002E8D3},
                {0x4002E8DE},
                {0x40035812},
                {0x4003549B},
                {0x40035414},
                {0x400352DB},
                {0x400352A0},
                {0x400358C0},
                {0x40035813},
                {0x40035478},
                {0x4006B485},
                {0x40284AFF},
                {0x40055510},
                {0x4013E204},
                {0x4013DD75},
                {0x4013E017},
                {0x4013DF33},
                {0x4013DD05},
                {0x4013DC8B},
                {0x400BD243},
                {0x400BD23B},
                {0x400BD247},
                {0x400BD23E},
                {0x400BD23D},
                {0x400BD249},
                {0x400BD23F},
                {0x400BD241},
                {0x400BD246},
                {0x400BD21D},
                {0x400BD245},
                {0x400ED4EA},
                {0x4007097C},
                {0x40166820},
                {0x4004D8A2},
                {0x400C06F3},
                {0x400A94EB},
                {0x4002463F},
                {0x401331C7},
                {0x400A1F7C},
                {0x4002A8A9},
                {0x4001EF33},
                {0x40028844},
                {0x400EF717},
                {0x400BD226},
                {0x400ACB2D},
                {0x4001CD3B},
                {0x400FE418},
                {0x40123BB4},
                {0x40058820},
                {0x400EBF25},
                {0x4013D5EC},
                {0x4009F495},
                {0x40032F9C},
                {0x4008C692},
                {0x400733B1},
                {0x4006242E},
                {0x40062434},
                {0x4006242F},
                {0x40062433},
                {0x4006242A},
                {0x40062435},
                {0x400335E7},
                {0x400335E3},
                {0x400335E8},
                {0x400BD229},
                {0x400BD22D},
                {0x400BD21F},
                {0x400BD223},
                {0x400BD22E},
                {0x400BD220},
                {0x400BD219},
                {0x400BD218},
                {0x400BD21B},
                {0x400BD21C},
                {0x400BD21E},
                {0x400BD228},
                {0x400BD224},
                {0x4015AB29},
                {0x4015AB3E},
                {0x400BD225},
                {0x4015AB14},
                {0x4015ABC0},
                {0x4008AA67},
                {0x400EFA10},
                {0x400EFBF1},
                {0x400EFC03},
                {0x400EF969},
                {0x400EFA41},
                {0x401325C6},
                {0x400798AB},
                {0x4005FDC7},
                {0x4005FD4C},
                {0x400EFA79},
                {0x400EFA4C},
                {0x400EFC0B},
                {0x400EFC55},
                {0x4016BF75},
                {0x40033036},
                {0x400EF9A6},
                {0x40044EF3},
                {0x40105053},
                {0x4010531B},
                {0x4010532E},
                {0x4006C9E7},
                {0x4015F5D0},
                {0x400D26C7},
                {0x4013D73F},
                {0x40094711},
                {0x4007CC2A},
                {0x4007974B},
                {0x40032AAA},
                {0x40074453},
                {0x40085F55},
                {0x40129B72},
                {0x401EE1FC},
                {0x400FD173},
                {0x40072608},
                {0x40022CC8},
                {0x400BEA53},
                {0x4013946D},
                {0x40203177},
                {0x4018154E},
                {0x4003392F},
                {0x401815BF},
                {0x4005E7B8},
                {0x40079676},
                {0x40032943},
                {0x401B3C18},
                {0x40027E1C},
                {0x4002FD2E},
                {0x4006D2DD},
                {0x400710E4},
                {0x400337D9},
                {0x4003378B},
                {0x4004F5A1},
                {0x401816C0},
                {0x40170039},
                {0x4003379E},
                {0x40181727},
                {0x40076EB8},
                {0x40082BC2},
                {0x40082C01},
                {0x4016FBCC},
                {0x40082BF2},
                {0x400D5047},
                {0x40079878},
                {0x40095868},
                {0x4012E210},
                {0x401053EC},
                {0x4008B21C},
                {0x4016AE91},
                {0x400A7894},
                {0x4017069C},
                {0x40170754},
                {0x40172817},
                {0x401707E6},
                {0x40173ED5},
                {0x40172833},
                {0x40172812},
                {0x401707E4},
                {0x4002D7B2},
                {0x4003090D},
                {0x400C0CF5},
                {0x40041DB4},
                {0x4015BE0B},
                {0x4015C191},
                {0x4015C4D7},
                {0x40046459},
                {0x4015E585},
                {0x400D290E},
                {0x400D2B70},
                {0x400D2FE0},
                {0x400D3530},
                {0x400D29F7},
                {0x4007B8A4},
                {0x400BD231},
                {0x4009A054},
                {0x4005FD3C},
                {0x400BE324},
                {0x400BD230},
                {0x400BD237},
                {0x4012E4AF},
                {0x4005007B},
                {0x4004A5A7},
                {0x400D6838},
                {0x40023AA8},
                {0x400F1CEB},
                {0x40028D35},
                {0x400291AF},
                {0x400D3930},
                {0x40033EAC},
                {0x4013DF06},
                {0x4002804C},
                {0x400B53D8},
                {0x4015C467},
                {0x4015BFFF},
                {0x400C158D},
                {0x4015C1C2},
                {0x4015C414},
                {0x40029865},
                {0x40028E85},
                {0x40094401},
                {0x4002A626},
                {0x400294BE},
                {0x4015C441},
                {0x4002CA29},
                {0x40202007},
                {0x4009868A},
                {0x40048633},
                {0x4007B8A9},
                {0x400B4568},
                {0x40036325},
                {0x40044210},
                {0x40044208},
                {0x40086FCE},
                {0x4013AB7E},
                {0x400F25E5},
                {0x40079136},
                {0x400FB323},
                {0x400E56E6},
                {0x400AC90A},
                {0x400BD7CA},
                {0x4004EFCA},
                {0x40034602},
                {0x400EDA8D},
                {0x400A845D},
                {0x400A8458},
                {0x400A846F},
                {0x400A8471},
                {0x400A8472},
                {0x400A846D},
                {0x400A846E},
                {0x400A845E},
                {0x400A846C},
                {0x400A8461},
                {0x400A8463},
                {0x400A845F},
                {0x4004114B},
                {0x400DB8AA},
                {0x40040582},
                {0x400B44CF},
                {0x400B44D1},
                {0x4004181D},
                {0x40041838},
                {0x4004181C},
                {0x40041DE4},
                {0x40045FE7},
                {0x4009A27F},
                {0x400649D7},
                {0x4006CAE7},
                {0x4006CAD6},
                {0x4006CAD7},
                {0x4006CB14},
                {0x4006CAF9},
                {0x400B4569},
                {0x4006C80F},
                {0x4006C80E},
                {0x4006C80D},
                {0x4006C817},
                {0x4006CAD4},
                {0x4006CAF3},
                {0x4006CAF4},
                {0x4006CAFB},
                {0x4006CB12},
                {0x4006CB18},
                {0x4006CB32},
                {0x4006CAF1},
                {0x4006CAF2},
                {0x400797C1},
                {0x40079A2B},
                {0x40078AC9},
                {0x400810D6},
                {0x40076DFF},
                {0x4006190F},
                {0x400BDF31},
                {0x40054B2D},
                {0x400EFBCE},
                {0x40089F4E},
                {0x40047560},
                {0x40049A5A},
            };
            //Intersection
            List<int> intersection = chest.Where(X => house.Contains(X)).ToList();
            // returns
            List<int> returns = new();
            LogHelper logger = new LogHelper("Strom returns.log", true, true, true);
            foreach (int ix in intersection)
            {
                bool inStromslist = chest.Contains(ix);
                bool inCocosList = house.Contains(ix);

                logger.Log(string.Format("item {0} exists in strom's list: {1}, exists in coco's list: {2}", ((Serial)ix), inStromslist, inCocosList));
                for (Item parent = World.FindItem(ix); parent != null; parent = World.FindItem(parent.Serial).Parent as Item)
                {
                    inStromslist = chest.Contains(parent.Serial);
                    inCocosList = house.Contains(parent.Serial);
                    logger.Log(string.Format("Parent {0} exists in strom's list: {1}, exists in coco's list: {2}", parent.Serial, inStromslist, inCocosList));

                    if (parent.Serial == root)
                        logger.Log(string.Format("Recovery Container {0} exists in strom's list: {1}, exists in coco's list: {2}", parent.Serial, inStromslist, inCocosList));
                }
                ;
                ;

            }
            logger.Finish();
            if (intersection.Count > 0)
                e.Mobile.SendMessage("Table overlap. We must return items");
            else
                e.Mobile.SendMessage("There is no table overlap. The reclamation of Strom's items is sound.");

            ;
        }
        #endregion Check Strom Restoration
        #region Make Township Lockdown
        private static void StromsTownshipLockdowns(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Select the area (of containers) to lockdown...");
            BoundingBoxPicker.Begin(e.Mobile, new BoundingBoxCallback(BoxLockDown_Callback), 0x01);
        }
        private static void BoxLockDown_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {

            // Create rec and retrieve items within from bounding box callback result
            Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);
            IPooledEnumerable eable = map.GetItemsInBounds(rect);

            int total = 0;
            int warnings = 0;
            TownshipRegion tsroot = null;
            // See what we got
            foreach (object obj in eable)
            {
                if (obj is BaseContainer bc)
                {
                    TownshipRegion tsr = TownshipRegion.GetTownshipAt(bc);
                    if (tsroot == null) tsroot = tsr;
                    if (tsr == null || (tsr != null && tsr != tsroot))
                    {
                        from.SendMessage("Error: Container {0} is not part of the township", bc);
                        return;
                    }

                    if (tsroot.TStone.IsLockedDown(bc))
                    {
                        from.SendMessage("Warning: Container {0} is already locked down", bc);
                        warnings++;
                    }
                    else
                    {
                        // Mr Strom 0x000235B9
                        int MrStrom = 0x000235B9;
                        bc.Movable = false;
                        tsroot.TStone.LockdownRegistry.Add(bc, new TownshipStone.LockDownContext(World.FindMobile(MrStrom)));
                        total++;
                    }
                }

            }
            eable.Free();
            from.SendMessage("{0} containers locked down, {1} warnings", total, warnings);
        }
        #endregion Make Township Lockdown
        #region PatchDriedHerbs
        public static void PatchDriedHerbs(CommandEventArgs e)
        {
            int count = ReplaceLockdowns(new ReplaceEntry[]
                {
                    new ReplaceEntry(typeof(Item), 0xC3C, typeof(WhiteDriedFlowers), null),
                    new ReplaceEntry(typeof(Item), 0xC3E, typeof(GreenDriedFlowers), null),
                });
            e.Mobile.SendMessage("Patched {0} items.", count);
        }
        #endregion
        #region Retribution for Strom's Theft
        private static void StromsRetribution(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Recovery in progress...");
            Dictionary<int, Type> list = new()
            {
                {0x4002E04F, typeof(Vines)},
                {0x4002E56E, typeof(Vines)},
                {0x4002C598, typeof(Vines)},
                {0x4002DC13, typeof(Vines)},
                {0x400B71B0, typeof(Vines)},
                {0x4002D34B, typeof(Vines)},
                {0x4002DFBA, typeof(Vines)},
                {0x4002DEFE, typeof(Vines)},
                {0x4002D9A8, typeof(Vines)},
                {0x4006C356, typeof(WoodenBox)},
                {0x4004ACEB, typeof(PotionKeg)},
                {0x4002DE32, typeof(Vines)},
                {0x4002D8AD, typeof(Vines)},
                {0x400B70F2, typeof(Vines)},
                {0x40050292, typeof(PotionKeg)},
                {0x4002D85E, typeof(Vines)},
                {0x400502B1, typeof(PotionKeg)},
                {0x40051ECB, typeof(ThrowableRose)},
                {0x4007EA82, typeof(PotionKeg)},
                {0x40038FBB, typeof(WoodenBox)},
                {0x4005C18A, typeof(Item)},
                {0x40049F6E, typeof(Item)},
                {0x40116F84, typeof(Rocks)},
                {0x401079C8, typeof(WheatSheaf)},
                {0x4002A161, typeof(Item)},
                {0x40114DA8, typeof(WoodenBox)},
                {0x4009A1BA, typeof(Rocks)},
                {0x4002E4EF, typeof(Vines)},
                {0x400B770B, typeof(Vines)},
                {0x40045338, typeof(Rocks)},
                {0x40063E02, typeof(ShipwreckedItem)},
                {0x4006C355, typeof(WoodenBox)},
                {0x40050587, typeof(PotionKeg)},
                {0x400B7456, typeof(Vines)},
                {0x400B6FB8, typeof(Vines)},
                {0x4002E0E1, typeof(Vines)},
                {0x401B3C99, typeof(WoodenBench)},
                {0x40104E83, typeof(Rocks)},
                {0x400495A2, typeof(WoodenBench)},
                {0x4010A297, typeof(Candelabra)},
                {0x4011C77E, typeof(PotionKeg)},
                {0x4006C358, typeof(WoodenBox)},
                {0x400B7624, typeof(Vines)},
                {0x40141077, typeof(MediumCrate)},
                {0x401A6920, typeof(QuarterStaff)},
                {0x401AA2B3, typeof(QuarterStaff)},
                {0x401AA2A9, typeof(QuarterStaff)},
                {0x400C6785, typeof(Gold)},
                {0x4003632F, typeof(WoodenBox)},
                {0x4003DC89, typeof(LargeTable)},
                {0x4003DC90, typeof(LargeTable)},
                {0x4001AECA, typeof(Scales)},
                {0x40040F93, typeof(HeatingStand)},
                {0x401D5AC1, typeof(SmallCrate)},
                {0x4003DB45, typeof(FancyArmoire)},
                {0x4003DB8D, typeof(Armoire)},
                {0x4007D21B, typeof(Head)},
                {0x4007CD86, typeof(DeadlyPoisonPotion)},
                {0x4007C707, typeof(GreaterExplosionPotion)},
                {0x4007C8B2, typeof(GreaterExplosionPotion)},
                {0x4007C8D8, typeof(GreaterExplosionPotion)},
                {0x4007CBDC, typeof(DeadlyPoisonPotion)},
                {0x4007B6A3, typeof(GreaterStrengthPotion)},
                {0x4007C7E9, typeof(TotalRefreshPotion)},
                {0x4007C8BE, typeof(GreaterExplosionPotion)},
                {0x40040003, typeof(GreaterStrengthPotion)},
                {0x4007C8D9, typeof(GreaterExplosionPotion)},
                {0x400CED3F, typeof(Scissors)},
                {0x4007C7E4, typeof(TotalRefreshPotion)},
                {0x4007C814, typeof(GreaterHealPotion)},
                {0x4007CA89, typeof(Bottle)},
                {0x4007C7EF, typeof(GreaterPoisonPotion)},
                {0x4010C84A, typeof(ThighBoots)},
                {0x40071765, typeof(StuddedGorget)},
                {0x4003F0C1, typeof(GreaterAgilityPotion)},
                {0x4007C706, typeof(GreaterExplosionPotion)},
                {0x400F5DC7, typeof(GoldNecklace)},
                {0x4001C67A, typeof(Katana)},
                {0x4007C8CA, typeof(GreaterExplosionPotion)},
                {0x4003FA47, typeof(MortarPestle)},
                {0x4007C8AF, typeof(GreaterExplosionPotion)},
                {0x4006E389, typeof(StuddedArms)},
                {0x4003FA4D, typeof(MortarPestle)},
                {0x4007B6A6, typeof(GreaterAgilityPotion)},
                {0x4007C8BF, typeof(GreaterExplosionPotion)},
                {0x4010352D, typeof(Bandage)},
                {0x400711AA, typeof(StuddedLegs)},
                {0x40116283, typeof(TribalSpear)},
                {0x4003F0B4, typeof(GreaterAgilityPotion)},
                {0x4007C7DA, typeof(TotalRefreshPotion)},
                {0x4007CD87, typeof(DeadlyPoisonPotion)},
                {0x40074656, typeof(GreaterHealPotion)},
                {0x40034782, typeof(VikingSword)},
                {0x4003465A, typeof(Bardiche)},
                {0x400C5E3E, typeof(FishSteak)},
                {0x40071320, typeof(StuddedGloves)},
                {0x402705CD, typeof(NorseHelm)},
                {0x4018BCA4, typeof(StuddedGorget)},
                {0x4003FA45, typeof(MortarPestle)},
                {0x400FE320, typeof(NorseHelm)},
                {0x4003FA52, typeof(MortarPestle)},
                {0x40040007, typeof(GreaterStrengthPotion)},
                {0x4003F46F, typeof(GreaterCurePotion)},
                {0x4003FA49, typeof(MortarPestle)},
                {0x4007C81A, typeof(GreaterHealPotion)},
                {0x4007B65C, typeof(GreaterCurePotion)},
                {0x40074659, typeof(GreaterHealPotion)},
                {0x4007B62D, typeof(GreaterHealPotion)},
                {0x4003FA48, typeof(MortarPestle)},
                {0x4003F0BB, typeof(GreaterAgilityPotion)},
                {0x40040002, typeof(GreaterStrengthPotion)},
                {0x4007DB3B, typeof(SpidersSilk)},
                {0x4007C8DA, typeof(GreaterExplosionPotion)},
                {0x4009BB09, typeof(Dagger)},
                {0x4006E379, typeof(StuddedGloves)},
                {0x4007C8CC, typeof(GreaterExplosionPotion)},
                {0x4007B650, typeof(GreaterCurePotion)},
                {0x4003FA46, typeof(MortarPestle)},
                {0x40076B98, typeof(GateTravelScroll)},
                {0x4007B5ED, typeof(GreaterCurePotion)},
                {0x400716C4, typeof(StuddedChest)},
                {0x400332BA, typeof(StuddedArms)},
                {0x402B8705, typeof(Pouch)},
                {0x4011103A, typeof(WoodenChest)},
                {0x4006341C, typeof(LargeCrate)},
                {0x4006B8F9, typeof(Cloak)},
                {0x400B00B2, typeof(Cloak)},
                {0x40041FA3, typeof(ShortPants)},
                {0x40063792, typeof(ShortPants)},
                {0x4009F51A, typeof(ShortPants)},
                {0x4009F51B, typeof(ThighBoots)},
                {0x400A4D51, typeof(Boots)},
                {0x402B543C, typeof(BearMask)},
                {0x40041FA5, typeof(BodySash)},
                {0x4004620D, typeof(BodySash)},
                {0x400CD3B7, typeof(SavageMask)},
                {0x400A101A, typeof(BodySash)},
                {0x400A1018, typeof(ShortPants)},
                {0x4009DB0B, typeof(Cloak)},
                {0x400A099D, typeof(Cloak)},
                {0x40095727, typeof(BodySash)},
                {0x40022F32, typeof(SavageMask)},
                {0x40095725, typeof(ShortPants)},
                {0x40037F0E, typeof(Basket)},
                {0x4007E944, typeof(Basket)},
                {0x40037F03, typeof(Basket)},
                {0x4009F51C, typeof(Cloak)},
                {0x4009AB00, typeof(Cloak)},
                {0x4009B532, typeof(Robe)},
                {0x400A101B, typeof(Cloak)},
                {0x400A0999, typeof(FancyShirt)},
                {0x400A4D3C, typeof(Cloak)},
                {0x400A1017, typeof(FancyShirt)},
                {0x400A099A, typeof(ShortPants)},
                {0x400A099B, typeof(Boots)},
                {0x4009B533, typeof(ShortPants)},
                {0x400951EF, typeof(ShortPants)},
                {0x40095A53, typeof(FancyDress)},
                {0x4009DB06, typeof(FancyDress)},
                {0x4007C860, typeof(Shirt)},
                {0x402153FA, typeof(Shoes)},
                {0x400FF50D, typeof(Robe)},
                {0x40067260, typeof(Kilt)},
                {0x400A36F2, typeof(Robe)},
                {0x4009A93A, typeof(ShortPants)},
                {0x4009A93B, typeof(Boots)},
                {0x400B00AE, typeof(FancyShirt)},
                {0x400A099C, typeof(BodySash)},
                {0x4009CFD3, typeof(Cloak)},
                {0x4006B8F3, typeof(FancyShirt)},
                {0x40063791, typeof(FancyShirt)},
                {0x40045182, typeof(Cloak)},
                {0x4004517D, typeof(FancyShirt)},
                {0x4009A939, typeof(FancyShirt)},
                {0x400639E7, typeof(ShortPants)},
                {0x400639E8, typeof(Boots)},
                {0x40045181, typeof(BodySash)},
                {0x4004517E, typeof(ShortPants)},
                {0x4004517F, typeof(Boots)},
                {0x400A4D4F, typeof(FancyShirt)},
                {0x4009A923, typeof(FancyShirt)},
                {0x4004CA95, typeof(Robe)},
                {0x40064CD8, typeof(Cloak)},
                {0x40064CDF, typeof(Cloak)},
                {0x400CCF47, typeof(SavageMask)},
                {0x400398E9, typeof(Robe)},
                {0x400E0DAF, typeof(BodySash)},
                {0x400E0DAD, typeof(ShortPants)},
                {0x40095A56, typeof(Cloak)},
                {0x400286F5, typeof(Boots)},
                {0x4009A925, typeof(Boots)},
                {0x40081B34, typeof(Boots)},
                {0x400A5143, typeof(Boots)},
                {0x40081B33, typeof(ShortPants)},
                {0x400A5142, typeof(ShortPants)},
                {0x4009A924, typeof(ShortPants)},
                {0x4009AAFE, typeof(Boots)},
                {0x4009AAFF, typeof(BodySash)},
                {0x40131DE4, typeof(Cloak)},
                {0x400ECAA5, typeof(Robe)},
                {0x400B00B1, typeof(BodySash)},
                {0x402F113D, typeof(ShortPants)},
                {0x400B00AF, typeof(ShortPants)},
                {0x40081B35, typeof(BodySash)},
                {0x4002CBEF, typeof(Sandals)},
                {0x4009AAFD, typeof(ShortPants)},
                {0x402F113C, typeof(Doublet)},
                {0x4003F7B0, typeof(Sandals)},
                {0x400FF50E, typeof(Sandals)},
                {0x400FE7F2, typeof(Sandals)},
                {0x400C9068, typeof(Robe)},
                {0x400A839A, typeof(Robe)},
                {0x400C9064, typeof(Robe)},
                {0x400A8632, typeof(Robe)},
                {0x400FF1BD, typeof(Robe)},
                {0x400FE87C, typeof(Robe)},
                {0x400FE4AD, typeof(Robe)},
                {0x400FE32A, typeof(Robe)},
                {0x400FEF67, typeof(Robe)},
                {0x400FF724, typeof(Robe)},
                {0x400FF415, typeof(Robe)},
                {0x400BAF4F, typeof(Robe)},
                {0x400BAA8C, typeof(Robe)},
                {0x400FE7F1, typeof(Robe)},
                {0x400FE790, typeof(Robe)},
                {0x400FF1DC, typeof(Robe)},
                {0x400A8503, typeof(Robe)},
                {0x4003A30F, typeof(Robe)},
                {0x40082B16, typeof(Robe)},
                {0x400826D1, typeof(Robe)},
                {0x4008292F, typeof(Robe)},
                {0x4008294C, typeof(Robe)},
                {0x40082CEA, typeof(Robe)},
                {0x400F8B75, typeof(DeathRobe)},
                {0x400F9E91, typeof(DeathRobe)},
                {0x4002EF83, typeof(Robe)},
                {0x40085C68, typeof(Robe)},
                {0x40116325, typeof(ShipwreckedItem)},
                {0x40083DB5, typeof(GargoylesPickaxe)},
                {0x40018593, typeof(GargoylesPickaxe)},
                {0x4002F1BC, typeof(GargoylesPickaxe)},
                {0x400B4ADD, typeof(BagOfReagents)},
                {0x40019AA3, typeof(Bag)},
                {0x400D2E1E, typeof(ScarecrowEastDeed)},
                {0x4010AF90, typeof(ShipwreckedItem)},
                {0x40027954, typeof(SeedBox)},
                {0x4006DBB5, typeof(Rock)},
                {0x400E203B, typeof(Rock)},
                {0x40085877, typeof(Rock)},
                {0x4000C057, typeof(Rock)},
                {0x400E07D4, typeof(UncutCloth)},
                {0x40081CC4, typeof(WheatSheaf)},
                {0x400826DB, typeof(Basket)},
                {0x40085C6E, typeof(Sandals)},
                {0x40011888, typeof(Sandals)},
                {0x400118B4, typeof(Sandals)},
                {0x40011A3D, typeof(Sandals)},
                {0x40011A3B, typeof(Sandals)},
                {0x4008C1C9, typeof(Sandals)},
                {0x40085C69, typeof(Sandals)},
                {0x400A72B5, typeof(FancyShirt)},
                {0x400A7E61, typeof(Shoes)},
                {0x40055033, typeof(Cloak)},
                {0x40018A37, typeof(LongPants)},
                {0x400B162B, typeof(SavageMask)},
                {0x40119674, typeof(BodySash)},
                {0x400A7E60, typeof(Robe)},
                {0x400B6604, typeof(Sandals)},
                {0x400B544C, typeof(Robe)},
                {0x4003582F, typeof(Robe)},
                {0x40095726, typeof(Boots)},
                {0x400936B9, typeof(ShipwreckedItem)},
                {0x40051ED4, typeof(ShipwreckedItem)},
                {0x400F6A4C, typeof(ShipwreckedItem)},
                {0x4005587F, typeof(ShipwreckedItem)},
                {0x4010DD03, typeof(ShipwreckedItem)},
                {0x4004CFF5, typeof(ShipwreckedItem)},
                {0x40093715, typeof(ShipwreckedItem)},
                {0x40115DCC, typeof(ShipwreckedItem)},
                {0x400936A5, typeof(ShipwreckedItem)},
                {0x40093612, typeof(ShipwreckedItem)},
                {0x40051E30, typeof(ShipwreckedItem)},
                {0x4009356A, typeof(ShipwreckedItem)},
                {0x4010AF72, typeof(ShipwreckedItem)},
                {0x4010B49C, typeof(ShipwreckedItem)},
                {0x400AB663, typeof(ShipwreckedItem)},
                {0x400DD639, typeof(ShipwreckedItem)},
                {0x40081533, typeof(ShipwreckedItem)},
                {0x4009B289, typeof(CrystallineBronze)},
                {0x400ADE37, typeof(DeathRobe)},
                {0x4013CF35, typeof(DeathRobe)},
                {0x4003655B, typeof(Item)},
                {0x4002DBE4, typeof(UncutCloth)},
                {0x40020A36, typeof(UncutCloth)},
                {0x40032FF7, typeof(UncutCloth)},
                {0x400E51FF, typeof(UncutCloth)},
                {0x4001EEC0, typeof(UncutCloth)},
                {0x40031A4E, typeof(UncutCloth)},
                {0x400A07D7, typeof(UncutCloth)},
                {0x400F218B, typeof(UncutCloth)},
                {0x400F1B13, typeof(UncutCloth)},
                {0x400B05A8, typeof(UncutCloth)},
                {0x400A3A41, typeof(UncutCloth)},
                {0x400BBC16, typeof(UncutCloth)},
                {0x400862EF, typeof(Rope)},
                {0x400C913A, typeof(Sandals)},
                {0x40046EEB, typeof(Vase)},
                {0x400A3D0A, typeof(DriedHerbs)},
                {0x400229C3, typeof(Basket)},
                {0x4001BA5C, typeof(CrystallineDullCopper)},
                {0x400492B8, typeof(CrystallineShadowIron)},
                {0x40044737, typeof(CrystallineShadowIron)},
                {0x40049B18, typeof(ScarecrowEastDeed)},
                {0x400A3DCE, typeof(CrystallineDullCopper)},
                {0x40048489, typeof(CrystallineDullCopper)},
                {0x4001BF67, typeof(CrystallineShadowIron)},
                {0x40049E08, typeof(UncutCloth)},
                {0x4006405A, typeof(UncutCloth)},
                {0x4008203E, typeof(UncutCloth)},
                {0x40082662, typeof(UncutCloth)},
                {0x40082CEE, typeof(ScarecrowEastDeed)},
                {0x4004A0CE, typeof(ScarecrowEastDeed)},
                {0x40035662, typeof(DirtySmallPot)},
                {0x400909D9, typeof(PlainDress)},
                {0x400909DA, typeof(Shoes)},
                {0x4002FB08, typeof(DirtyPot)},
                {0x40071043, typeof(SmallCrate)},
                {0x40091D27, typeof(Buckler)},
                {0x400E1634, typeof(ScarecrowEastDeed)},
                {0x400D133F, typeof(UncutCloth)},
                {0x4005007A, typeof(Bag)},
                {0x40082EE1, typeof(ScarecrowEastDeed)},
                {0x400627F4, typeof(Basket)},
                {0x400916F7, typeof(CrystallineGold)},
                {0x400921E7, typeof(CrystallineDullCopper)},
                {0x40044CF9, typeof(TwoFullJars)},
                {0x4005299F, typeof(ScarecrowEastDeed)},
                {0x40050368, typeof(ScarecrowEastDeed)},
                {0x400B7394, typeof(CrystallineBronze)},
                {0x4006A2D6, typeof(CrystallineBronze)},
                {0x40026CD5, typeof(CrystallineCopper)},
                {0x400D5BE2, typeof(CrystallineDullCopper)},
                {0x4004A419, typeof(CrystallineCopper)},
                {0x40039335, typeof(DriedOnions)},
                {0x40039F80, typeof(DriedHerbs)},
                {0x40038932, typeof(WhiteDriedFlowers)},
                {0x40039BE6, typeof(GreenDriedFlowers)},
                {0x400A4039, typeof(ScarecrowEastDeed)},
                {0x400313CB, typeof(Basket)},
                {0x40109AAC, typeof(ScarecrowEastDeed)},
                {0x400EDDC3, typeof(CrystallineShadowIron)},
                {0x40081447, typeof(DeathRobe)},
                {0x4010978F, typeof(UncutCloth)},
                {0x40021A8F, typeof(ShipwreckedItem)},
                {0x400219CB, typeof(ShipwreckedItem)},
                {0x4010AF9D, typeof(ShipwreckedItem)},
                {0x4010AFC4, typeof(ShipwreckedItem)},
                {0x4010AF9B, typeof(ShipwreckedItem)},
                {0x4001F367, typeof(Item)},
                {0x400CB6E3, typeof(UncutCloth)},
                {0x40062D6F, typeof(UncutCloth)},
                {0x40058580, typeof(DeathRobe)},
                {0x400783DD, typeof(ScarecrowEastDeed)},
                {0x400CCF7B, typeof(Rock)},
                {0x400444B9, typeof(TwoFullJars)},
                {0x40079227, typeof(UncutCloth)},
                {0x4007DAF0, typeof(DirtySmallPot)},
                {0x40081287, typeof(ScarecrowEastDeed)},
                {0x400790BA, typeof(Item)},
                {0x4007DAF1, typeof(EarOfCorn)},
                {0x4008541B, typeof(UncutCloth)},
                {0x400B1FCD, typeof(Sandals)},
                {0x400B13A5, typeof(Item)},
                {0x4006BC2C, typeof(Item)},
                {0x4008D95B, typeof(AncientSmithyHammer)},
                {0x4008FBBB, typeof(AncientSmithyHammer)},
                {0x4012B2C0, typeof(GoldWire)},
                {0x4002A783, typeof(Sandals)},
                {0x4002B2C9, typeof(Sandals)},
                {0x4002B323, typeof(Sandals)},
                {0x4002A835, typeof(Sandals)},
                {0x400231B6, typeof(TreasureMap)},
                {0x400C8F42, typeof(Cloak)},
                {0x40020D4A, typeof(WheatSheaf)},
                {0x40083B60, typeof(BloodDrenchedBandana)},
                {0x400BA496, typeof(BloodDrenchedBandana)},
                {0x400F8B74, typeof(Backpack)},
                {0x400AE597, typeof(SpecialFishingNet)},
                {0x400586F5, typeof(SpecialFishingNet)},
                {0x40077476, typeof(SpecialFishingNet)},
                {0x400435C6, typeof(SpecialFishingNet)},
                {0x400AD8C6, typeof(SpecialFishingNet)},
                {0x40043B48, typeof(SpecialFishingNet)},
                {0x400AF2BB, typeof(SpecialFishingNet)},
                {0x40044A1A, typeof(SpecialFishingNet)},
                {0x400AD296, typeof(SpecialFishingNet)},
                {0x40041B9F, typeof(SpecialFishingNet)},
                {0x400AD563, typeof(SpecialFishingNet)},
                {0x400DCCD7, typeof(SpecialFishingNet)},
                {0x4004422C, typeof(SpecialFishingNet)},
                {0x400733CF, typeof(Backpack)},
                {0x40043FB5, typeof(SpecialFishingNet)},
                {0x400DBCC4, typeof(SpecialFishingNet)},
                {0x400DCE0B, typeof(SpecialFishingNet)},
                {0x40093D74, typeof(SpecialFishingNet)},
                {0x400938CB, typeof(SpecialFishingNet)},
                {0x400DC457, typeof(SpecialFishingNet)},
                {0x400940D9, typeof(SpecialFishingNet)},
                {0x400C1993, typeof(Basket)},
                {0x4003774F, typeof(BloodDrenchedBandana)},
                {0x4008FBC0, typeof(HalfApron)},
                {0x400B1FCE, typeof(Kilt)},
                {0x4001E29F, typeof(Kilt)},
                {0x400861F1, typeof(BagOfReagents)},
                {0x400733D3, typeof(Backpack)},
                {0x4006D7AB, typeof(LongPants)},
                {0x400ED266, typeof(BodySash)},
                {0x400E0DAE, typeof(Boots)},
                {0x400B6601, typeof(Kilt)},
                {0x4002D246, typeof(Sandals)},
                {0x401D0D97, typeof(LeatherCap)},
                {0x400E6AB9, typeof(BodySash)},
                {0x401A1585, typeof(Cap)},
                {0x400A0AAF, typeof(SavageMask)},
                {0x402843C9, typeof(SavageMask)},
                {0x400B15BD, typeof(SavageMask)},
                {0x400486AD, typeof(SavageMask)},
                {0x4000B11D, typeof(SavageMask)},
                {0x40149902, typeof(FeatheredHat)},
                {0x40103E61, typeof(SavageMask)},
                {0x4004A1BA, typeof(Sandals)},
                {0x4011123E, typeof(Boots)},
                {0x40054B2F, typeof(ThighBoots)},
                {0x40041A45, typeof(ThighBoots)},
                {0x40082CEB, typeof(Sandals)},
                {0x400826D2, typeof(Sandals)},
                {0x400C9069, typeof(Sandals)},
                {0x4008294D, typeof(Sandals)},
                {0x40082B17, typeof(Sandals)},
                {0x400A8633, typeof(Sandals)},
                {0x400C9065, typeof(Shoes)},
                {0x400A839B, typeof(Sandals)},
                {0x400A4D3B, typeof(ThighBoots)},
                {0x400A4D3A, typeof(ShortPants)},
                {0x400AA771, typeof(ThighBoots)},
                {0x400AA770, typeof(ShortPants)},
                {0x400456F3, typeof(ShortPants)},
                {0x400456F4, typeof(ThighBoots)},
                {0x400ABF95, typeof(FancyShirt)},
                {0x40095724, typeof(FancyShirt)},
                {0x400DD3FB, typeof(FancyShirt)},
                {0x400DD3FC, typeof(ShortPants)},
                {0x400DD3FD, typeof(Boots)},
                {0x400D09A8, typeof(BodySash)},
                {0x400D09A5, typeof(FancyShirt)},
                {0x40081B36, typeof(Cloak)},
                {0x4009A927, typeof(Cloak)},
                {0x400951EE, typeof(Robe)},
                {0x40037EFB, typeof(FancyShirt)},
                {0x400FF032, typeof(Robe)},
                {0x4008652C, typeof(Cloak)},
                {0x4002A1C1, typeof(HalfApron)},
                {0x4003B412, typeof(Cloak)},
                {0x4003690F, typeof(Cloak)},
                {0x4002CBEB, typeof(Robe)},
                {0x40018EB2, typeof(Doublet)},
                {0x40263F24, typeof(WoodenChest)},
                {0x4007246A, typeof(Bag)},
                {0x400D127F, typeof(DyeTub)},
                {0x400D1280, typeof(Dyes)},
                {0x400D1EE5, typeof(Lantern)},
                {0x40051151, typeof(Lantern)},
                {0x4005114A, typeof(Lantern)},
                {0x40051140, typeof(Lantern)},
                {0x40089ED0, typeof(Lantern)},
                {0x40051153, typeof(Lantern)},
                {0x40051149, typeof(Lantern)},
                {0x40051148, typeof(Lantern)},
                {0x4004B98E, typeof(ShortPants)},
                {0x40071071, typeof(WoodenBench)},
                {0x40127D76, typeof(EarOfCorn)},
                {0x400B4E94, typeof(Pouch)},
                {0x40087171, typeof(GateTravelScroll)},
                {0x40029C49, typeof(Gold)},
                {0x40028CF9, typeof(BoneHelm)},
                {0x40089DB2, typeof(WheatSheaf)},
                {0x4008C36F, typeof(ScarecrowEastDeed)},
                {0x40087374, typeof(AncientSmithyHammer)},
                {0x40063794, typeof(BodySash)},
                {0x400ABF99, typeof(Cloak)},
                {0x4006A33A, typeof(Arrow)},
                {0x400743E6, typeof(Bone)},
                {0x400B8DE6, typeof(PlateArms)},
                {0x40064089, typeof(PlateGloves)},
                {0x400638F4, typeof(ChainChest)},
                {0x400C3B05, typeof(ChainLegs)},
                {0x40062E12, typeof(Helmet)},
                {0x40034CE6, typeof(PlateGorget)},
                {0x40046D85, typeof(WoodenChairCushion)},
                {0x4006165E, typeof(WoodenChairCushion)},
                {0x4008608C, typeof(Throne)},
                {0x40029D15, typeof(Robe)},
                {0x40029E17, typeof(Robe)},
                {0x40029CD2, typeof(Robe)},
                {0x400294C9, typeof(Robe)},
                {0x40029446, typeof(Robe)},
                {0x40029493, typeof(Robe)},
                {0x40029D29, typeof(Robe)},
                {0x40029C51, typeof(Robe)},
                {0x40029369, typeof(Robe)},
                {0x40029C0F, typeof(Robe)},
                {0x400289F4, typeof(Robe)},
                {0x40029333, typeof(Robe)},
                {0x40029C38, typeof(Robe)},
                {0x40028CC8, typeof(Robe)},
                {0x40028A4D, typeof(Robe)},
                {0x40028C88, typeof(Robe)},
                {0x40029DB7, typeof(Robe)},
                {0x40028A13, typeof(Robe)},
                {0x40029551, typeof(Robe)},
                {0x40028C4A, typeof(Robe)},
                {0x40029CB9, typeof(Robe)},
                {0x40029C7A, typeof(Robe)},
                {0x40029CA2, typeof(Robe)},
                {0x4002938C, typeof(Robe)},
                {0x40028A30, typeof(Robe)},
                {0x40029D3B, typeof(Robe)},
                {0x40029373, typeof(Robe)},
                {0x40029345, typeof(Robe)},
                {0x40028D41, typeof(Robe)},
                {0x40029350, typeof(Robe)},
                {0x400293C3, typeof(Robe)},
                {0x40029380, typeof(Robe)},
                {0x400289FA, typeof(Robe)},
                {0x40028BC1, typeof(Robe)},
                {0x40028A82, typeof(Robe)},
                {0x40029347, typeof(Sandals)},
                {0x40028C4B, typeof(Sandals)},
                {0x400289FB, typeof(Sandals)},
                {0x40028C89, typeof(Sandals)},
                {0x40028D42, typeof(Sandals)},
                {0x40028A28, typeof(Sandals)},
                {0x40029C7B, typeof(Sandals)},
                {0x40028BC2, typeof(Sandals)},
                {0x4002095B, typeof(CrystallineShadowIron)},
                {0x4005B8F1, typeof(CrystallineCopper)},
                {0x4004A65E, typeof(TwoFullJars)},
                {0x400649B3, typeof(CandleLarge)},
                {0x40060E32, typeof(Item)},
                {0x4004D1AE, typeof(Item)},
                {0x4005387D, typeof(DriedOnions)},
                {0x400B6777, typeof(Item)},
                {0x400BF329, typeof(DriedHerbs)},
                {0x4006F8CD, typeof(UncutCloth)},
                {0x4006DCDE, typeof(UncutCloth)},
                {0x400B1B75, typeof(Item)},
                {0x4006C474, typeof(Item)},
                {0x4003F7BF, typeof(Item)},
                {0x40107BCE, typeof(ShipwreckedItem)},
                {0x40066B54, typeof(TwoFullJars)},
                {0x400374FF, typeof(CrystallineDullCopper)},
                {0x4005F05E, typeof(WhiteDriedFlowers)},
                {0x40062191, typeof(GreenDriedFlowers)},
                {0x40096729, typeof(UncutCloth)},
                {0x40096C67, typeof(UncutCloth)},
                {0x40093CE0, typeof(DirtySmallRoundPot)},
                {0x4009529E, typeof(Backpack)},
                {0x40047B5B, typeof(Cotton)},
                {0x40047B5C, typeof(Wool)},
                {0x40104F67, typeof(Key)},
                {0x400F0373, typeof(WheatSheaf)},
                {0x4001B3BE, typeof(Rocks)},
                {0x40105AC5, typeof(ScarecrowEastDeed)},
                {0x40105AAD, typeof(ScarecrowEastDeed)},
                {0x40117A33, typeof(Rocks)},
                {0x401CE7A1, typeof(Rocks)},
                {0x40105AC3, typeof(ScarecrowEastDeed)},
                {0x40105AB5, typeof(ScarecrowEastDeed)},
                {0x40105AB4, typeof(ScarecrowEastDeed)},
                {0x40105AA5, typeof(ScarecrowEastDeed)},
                {0x4006DC97, typeof(SheafOfHay)},
                {0x4006DC95, typeof(SheafOfHay)},
                {0x400B9749, typeof(Rocks)},
                {0x40038C9E, typeof(SheafOfHay)},
                {0x4008B189, typeof(Item)},
                {0x400506AD, typeof(Rocks)},
                {0x40062673, typeof(Rocks)},
                {0x400264A9, typeof(SheafOfHay)},
                {0x400FC500, typeof(Rock)},
                {0x4005B295, typeof(Rocks)},
                {0x40062A82, typeof(Rocks)},
                {0x40105ABB, typeof(ScarecrowEastDeed)},
                {0x40046BE2, typeof(Rocks)},
                {0x401ACFB6, typeof(Rock)},
                {0x400B2D15, typeof(Rocks)},
                {0x40093638, typeof(RibCage)},
                {0x400954CD, typeof(RibCage)},
                {0x400949F1, typeof(BonePile)},
                {0x40070874, typeof(BonePile)},
                {0x40072B9F, typeof(BonePile)},
                {0x40093D14, typeof(BonePile)},
                {0x40072BBF, typeof(BonePile)},
                {0x400942A6, typeof(BonePile)},
                {0x400934E7, typeof(BonePile)},
                {0x4006FE00, typeof(BonePile)},
                {0x400734FB, typeof(BonePile)},
                {0x40072C9C, typeof(BonePile)},
                {0x40094A2F, typeof(RibCage)},
                {0x4010A05A, typeof(ShipwreckedItem)},
                {0x400814FA, typeof(ShipwreckedItem)},
                {0x40115FD5, typeof(ShipwreckedItem)},
                {0x40115025, typeof(ShipwreckedItem)},
                {0x400DD6C9, typeof(ShipwreckedItem)},
                {0x4006242B, typeof(Pouch)},
                {0x4002B299, typeof(Robe)},
                {0x4002B321, typeof(Robe)},
                {0x4002A9FC, typeof(Robe)},
                {0x4002B384, typeof(Robe)},
                {0x4002B172, typeof(Robe)},
                {0x4002B189, typeof(Robe)},
                {0x4002A8D1, typeof(Robe)},
                {0x4002A97D, typeof(Robe)},
                {0x4002A911, typeof(Robe)},
                {0x4002A7A0, typeof(Robe)},
                {0x4002B1F6, typeof(Robe)},
                {0x4002B167, typeof(Robe)},
                {0x4002B17B, typeof(Robe)},
                {0x4002B16B, typeof(Robe)},
                {0x4002A7BF, typeof(Robe)},
                {0x4002B1B5, typeof(Robe)},
                {0x4002A796, typeof(Robe)},
                {0x4002A782, typeof(Robe)},
                {0x4002B18A, typeof(Sandals)},
                {0x4002A78F, typeof(Robe)},
                {0x4002B2C8, typeof(Robe)},
                {0x4002A935, typeof(Robe)},
                {0x4002A7F8, typeof(Robe)},
                {0x4002A80A, typeof(Robe)},
                {0x4002A936, typeof(Sandals)},
                {0x4002B385, typeof(Sandals)},
                {0x4002A912, typeof(Sandals)},
                {0x4000C16F, typeof(Sandals)},
                {0x4002B1F7, typeof(Sandals)},
                {0x40063793, typeof(Boots)},
                {0x4001D814, typeof(WoodenChest)},
                {0x400E4DF7, typeof(Pouch)},
                {0x40149059, typeof(Pouch)},
                {0x401489CB, typeof(Pouch)},
                {0x401489D2, typeof(Pouch)},
                {0x401489C6, typeof(Pouch)},
                {0x401489CE, typeof(Pouch)},
                {0x401489C8, typeof(Pouch)},
                {0x40149068, typeof(Pouch)},
                {0x400833D8, typeof(Spellbook)},
                {0x4004250D, typeof(Spellbook)},
                {0x4005E2E4, typeof(ClumsyScroll)},
                {0x40060559, typeof(WeakenScroll)},
                {0x40060930, typeof(FeeblemindScroll)},
                {0x40060A47, typeof(ReactiveArmorScroll)},
                {0x40060A81, typeof(HealScroll)},
                {0x40060AB1, typeof(CreateFoodScroll)},
                {0x40060CF9, typeof(MagicArrowScroll)},
                {0x4006122E, typeof(NightSightScroll)},
                {0x4005E32B, typeof(AgilityScroll)},
                {0x4005E4B5, typeof(CunningScroll)},
                {0x4005E374, typeof(CunningScroll)},
                {0x4005E50B, typeof(CureScroll)},
                {0x4005E578, typeof(HarmScroll)},
                {0x4005E568, typeof(HarmScroll)},
                {0x4005E5BB, typeof(MagicTrapScroll)},
                {0x40060ADE, typeof(StrengthScroll)},
                {0x40060AE7, typeof(ProtectionScroll)},
                {0x40061268, typeof(MagicUnTrapScroll)},
                {0x4005E65A, typeof(BlessScroll)},
                {0x4005E6DF, typeof(FireballScroll)},
                {0x4005E9F2, typeof(MagicLockScroll)},
                {0x4005EA40, typeof(PoisonScroll)},
                {0x4005EAB3, typeof(TelekinisisScroll)},
                {0x4005EB43, typeof(TeleportScroll)},
                {0x40060DB1, typeof(UnlockScroll)},
                {0x40060DCB, typeof(WallOfStoneScroll)},
                {0x4005EBDB, typeof(ArchCureScroll)},
                {0x4005EC7C, typeof(ArchProtectionScroll)},
                {0x4005ED28, typeof(CurseScroll)},
                {0x4005ED83, typeof(FireFieldScroll)},
                {0x4005EE27, typeof(GreaterHealScroll)},
                {0x4005EEA4, typeof(LightningScroll)},
                {0x4005EFCE, typeof(ManaDrainScroll)},
                {0x4005F073, typeof(RecallScroll)},
                {0x4005F16E, typeof(BladeSpiritsScroll)},
                {0x4005F222, typeof(DispelFieldScroll)},
                {0x4005F2DD, typeof(IncognitoScroll)},
                {0x4005F3A4, typeof(MagicReflectScroll)},
                {0x4005F41D, typeof(MindBlastScroll)},
                {0x4005F4B3, typeof(ParalyzeScroll)},
                {0x4005F553, typeof(PoisonFieldScroll)},
                {0x4005F5BB, typeof(SummonCreatureScroll)},
                {0x4005F656, typeof(DispelScroll)},
                {0x4005F6AB, typeof(EnergyBoltScroll)},
                {0x4005F6FC, typeof(ExplosionScroll)},
                {0x4005F741, typeof(InvisibilityScroll)},
                {0x4005F7D1, typeof(MarkScroll)},
                {0x4005F879, typeof(ParalyzeFieldScroll)},
                {0x4005F8D0, typeof(RevealScroll)},
                {0x40060C45, typeof(MassCurseScroll)},
                {0x4005F99F, typeof(ChainLightningScroll)},
                {0x4005FA2C, typeof(EnergyFieldScroll)},
                {0x4005FA9C, typeof(FlamestrikeScroll)},
                {0x4005FB37, typeof(ManaVampireScroll)},
                {0x4005FC17, typeof(MeteorSwarmScroll)},
                {0x4005FC72, typeof(PolymorphScroll)},
            };
            int deleted = 0;
            int recovered = 0;
            LargeCrate storage = new LargeCrate();
            storage.Movable = false;
            storage.MaxItems = 1000;
            LogHelper logger = new LogHelper("Strom Recovery.log", true, true, true);
            foreach (var kvp in list)
                if (World.FindItem(kvp.Key) is Item item && item.GetType() == kvp.Value)
                {
                    recovered++;
                    if (storage.TryDropItem(e.Mobile, item, sendFullMessage: false) == false)
                    {
                        string text = string.Format("Unable to store more items in this container: {0}.", storage);
                        Console.WriteLine(text); e.Mobile.SendMessage(text); logger.Log(text);
                        text = string.Format("Deleting item {0}.", item);
                        Console.WriteLine(text); e.Mobile.SendMessage(text); logger.Log(text);
                        item.Delete();
                    }
                    else
                        logger.Log(string.Format("Item {0} has been recovered", item));
                }
                else
                {
                    logger.Log(string.Format("Item {0}:{1} had been deleted", kvp.Key, kvp.Value));
                    deleted++;
                }

            storage.MoveToWorld(e.Mobile.Location, e.Mobile.Map);
            logger.Log(string.Format("Attempted to recover {0} items. {1} items actually recovered", recovered + deleted, recovered));
            logger.Log(string.Format("{0} recovered items stored here {1}", recovered, storage));
            logger.Finish();
        }
        #endregion Retribution for Strom's Theft
        #region Analyze Ore
        private static void AnalyzeOre(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Select the area to analyze...");
            BoundingBoxPicker.Begin(e.Mobile, new BoundingBoxCallback(InvBox_Callback), 0x01);
        }
        private static void InvBox_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {

            // Create rec and retrieve items within from bounding box callback result
            Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);
            IPooledEnumerable eable = map.GetItemsInBounds(rect);

            int movable = 0;
            int mustSteal = 0;
            int total = 0;
            // See what we got
            foreach (object obj in eable)
            {
                if (obj is Item item && IsIngotStack(item.GetType()))
                {
                    total++;
                    if (item.Movable) movable++;
                    if (item.GetItemBool(Item.ItemBoolTable.MustSteal)) mustSteal++;
                }

            }
            eable.Free();
            from.SendMessage("{0} found, {1} movable, {2} stealable", total, movable, mustSteal);
        }
        private static bool IsIngotStack(Type type)
        {
            if (
                type == typeof(CopperIngots) || type == typeof(DullCopperIngots) || type == typeof(IronIngots) ||
                type == typeof(ShadowIronIngots) || type == typeof(GoldIngots) || type == typeof(SilverIngots) ||
                type == typeof(BronzeIngots) || type == typeof(AgapiteIngots) || type == typeof(VeriteIngots) ||
                type == typeof(ValoriteIngots)
                )
                return true;

            return false;
        }
        #endregion Analyze Ore
        #region Log Item Test
        private static void LogItemTest(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target item...");
            e.Mobile.Target = new LogItemTestTarget(); // Call our target
        }

        public class LogItemTestTarget : Target
        {
            public LogItemTestTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is Item item)
                {
                    LogHelper logger = new LogHelper("Log Item Test.log");
                    logger.Log(LogType.Item, item);
                    logger.Finish();
                }
                else
                {
                    from.SendMessage("That is not an item.");
                    return;
                }
            }
        }
        #endregion Log Item Test
        #region Analyze RandomMinMaxScaled
        private static void AnalyzeRandomMinMaxScaled(CommandEventArgs e)
        {
            int[] table = new int[10];
            for (int ix = 0; ix < 100 * 1000; ix++)
            {
                table[Utility.RandomMinMaxScaled(table.Length)]++;
            }
            ;
            for (int jx = 0; jx < table.Length; jx++)
                table[jx] /= 1000;
            ;

            table = new int[10];
            for (int ix = 0; ix < 100 * 1000; ix++)
            {
                table[Utility.RandomMinMaxExp(table.Length, Exponent.d0_15)]++;
            }
            ;
            for (int jx = 0; jx < table.Length; jx++)
                table[jx] /= 1000;
            ;

        }
        #endregion Analyze RandomMinMaxScaled
        #region Log Location
        private static void LogLocation(CommandEventArgs e)
        {
            LogHelper logger = new LogHelper("log location.log", false, true, true);
            logger.Log(string.Format("new Point3D{0},", e.Mobile.Location));
            logger.Finish();
        }
        #endregion Log Location
        #region Delete Camps
        private static void DeleteCamps(CommandEventArgs e)
        {
            List<BaseCamp> list = new();
            foreach (var item in World.Items.Values)
                if (item is BaseCamp bc && bc.Deleted == false)
                    list.Add(bc);

            foreach (BaseCamp bc in list)
                bc.Delete();
        }
        #endregion Delete Camps
        #region Grandfather Herding
        private static void GrandfatherHerding(CommandEventArgs e)
        {
            List<PlayerMobile> pmList = new();
            foreach (Mobile m in World.Mobiles.Values)
                if (m is PlayerMobile pm)
                    if (pm.Skills[SkillName.Herding].Base > 70.0)
                        pmList.Add(pm);

            foreach (PlayerMobile pm in pmList)
                pm.SetPlayerBool(PlayerBoolTable.GrandfatheredHerding, true);

            return;

        }
        #endregion Grandfather Herding
        #region PatchAlignmentRegions
        public static void PatchAlignmentRegions(CommandEventArgs e)
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
            patched += SetRegionAlignment("Moonglow Cemetary", Map.Felucca, AlignmentType.Undead);
            patched += SetRegionAlignment("Nujel'm Cemetery", Map.Felucca, AlignmentType.Undead);
            patched += SetRegionAlignment("Vesper Cemetery", Map.Felucca, AlignmentType.Undead);
            patched += SetRegionAlignment("Yew Cemetery", Map.Felucca, AlignmentType.Undead);
            e.Mobile.SendMessage("Number regions added: {0}. Number regions patched: {1}.", added, patched);
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
        #endregion
        #region Patch Tower doors
        private static void PatchTowerDoors(CommandEventArgs e)
        {
            foreach (Item item in World.Items.Values)
                if (item is BaseDoor bd && (bd.Z == 26 || bd.Z == 46) && bd.Facing == DoorFacing.SouthCW)
                    bd.Facing = DoorFacing.NorthCW;
        }
        #endregion Patch Tower doors
        #region Empty Bank
        private static void EmptyBank(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target player...");
            e.Mobile.Target = new EmptyBankTarget(); // Call our target
        }

        public class EmptyBankTarget : Target
        {
            public EmptyBankTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile player)
                {
                    if (player.BankBox != null)
                    {
                        List<Item> list = new List<Item>(player.BankBox.Items);
                        foreach (Item item in list)
                        {
                            player.BankBox.RemoveItem(item);
                            item.Delete();
                        }
                    }
                    else
                        from.SendMessage("They do not have a bank box.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        #endregion Empty Bank
        #region Test Rare Factory Item
        private static void RareFactoryItem(CommandEventArgs e)
        {
            int count = 0;
            for (int ix = 0; ix < 1000 * 1000; ix++)
            {
                Item item = Loot.RareFactoryItem(0.1);
                if (item != null)
                {
                    count++;
                    item.Delete();
                }
            }

            e.Mobile.SendMessage("{0} calls at 10% success generated {1} items", 1000 * 1000, count);

        }
        #endregion Test Rare Factory Item
        #region Test Nerun Parser
        private static void TestNerunParser(CommandEventArgs e)
        {
            List<string> mzLines = new() { "*|alligator:lizardman:silverserpent:snake|wisp|||||2369|3430|3|2|60|80|30|30|1|8|2|0|0|0|0" };

            foreach (string line in mzLines)
            {
                List<NerunRecord.Record> recordList = new();
                List<string> compiledTokens = new();
                string[] lineToks = line.Split(new char[] { '|' });                 // parse on '|'
                CompileLine(lineToks, compiledTokens);                              // compile this line
                NerunRecord.Parse(compiledTokens.ToArray(), recordList);            // parse into a record list
                // inspect here
                ;
            }
        }
        #endregion Test Nerun Parser
        #region Find Rare 'gold box' FillableContainer 
        private static void FindRareGoldBoxes(CommandEventArgs e)
        {   // 3712 (0xE80), and 2472 (0x9A8)
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            foreach (Item item in World.Items.Values)
            {
                if (item is FillableContainer fc && (item.ItemID == 0xE80 || item.ItemID == 0x9A8))
                {
                    if (fc.ContentType == FillableContentType.Rare)
                        pm.JumpList.Add(Spawner.GetSpawnPosition(item.Map, item.Location, 3, SpawnFlags.None, item));
                }
            }

            e.Mobile.SendMessage("Your jumplist has been initialized with {0} items.", pm.JumpList.Count);
            e.Mobile.SendMessage("{0} boxes are rare.", pm.JumpList.Count);
        }
        #endregion Find Rare 'gold box' FillableContainer 
        #region Find 'gold box' FillableContainer 
        private static void FindGoldBoxes(CommandEventArgs e)
        {   // 3712 (0xE80), and 2472 (0x9A8)
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            int notFilled = 0;
            foreach (Item item in World.Items.Values)
            {
                if (item is FillableContainer fc && (item.ItemID == 0xE80 || item.ItemID == 0x9A8))
                {
                    // look for base vendors nearby
                    IPooledEnumerable eable = Map.Felucca.GetMobilesInRange(item.Location, 10);
                    foreach (Mobile vendor in eable)
                        if (vendor is BaseVendor)
                        {
                            pm.JumpList.Add(vendor);
                            if (fc.ContentType == FillableContentType.None)
                                notFilled++;
                            break;
                        }
                    eable.Free();
                }
            }

            e.Mobile.SendMessage("Your jumplist has been initialized with {0} items.", pm.JumpList.Count);
            e.Mobile.SendMessage("{0} boxes are not filled.", notFilled);
        }
        #endregion Find 'gold box' FillableContainer 
        #region Convert 'gold box' FillableContainers to Rare
        private static void ConvertGoldBoxes(CommandEventArgs e)
        {   // 3712 (0xE80), and 2472 (0x9A8)
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            foreach (Item item in World.Items.Values)
            {
                if (item is FillableContainer fc && (item.ItemID == 0xE80 || item.ItemID == 0x9A8))
                {
                    // make sure there are no other 'Rare' gold boxes around
                    IPooledEnumerable eable = Map.Felucca.GetItemsInRange(item.Location, 20);
                    foreach (Item thing in eable)
                        if (thing is FillableContainer fct && (thing.ItemID == 0xE80 || thing.ItemID == 0x9A8))
                            if (fct.ContentType == FillableContentType.Rare)
                            {
                                eable.Free();
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
                                fc.TotalRespawn = true;
                                pm.JumpList.Add(vendor);
                            }
                            break;
                        }
                    eable.Free();

                skip:;
                }
            }

            e.Mobile.SendMessage("Your jumplist has been initialized with {0} items.", pm.JumpList.Count);
            e.Mobile.SendMessage("{0} boxes have been converted.", pm.JumpList.Count);
        }
        #endregion Convert 'gold box' FillableContainers to Rare
        #region Count Gems
        private static void CountGems(CommandEventArgs e)
        {
            int[] GemCounts = new int[Loot.GemTypes.Count()];
            foreach (Item item in World.Items.Values)
            {
                //if (Loot.GemTypes.Contains(item.GetType()))

                //  var index = Array.FindIndex(myArray, row => row.Author == "xyz");
                var index = Array.FindIndex(Loot.GemTypes, row => row == item.GetType());
                if (index != -1)
                {
                    ; // found it
                    if (item.Amount > GemCounts[index])
                    {
                        GemCounts[index] = item.Amount;
                    }
                }
                else
                    ; // not this one.
            }
        }
        #endregion Count Gems
        #region Move Boat
        private static void MoveBoat(CommandEventArgs e)
        {
            BaseBoat boat = BaseBoat.FindBoatAt(e.Mobile);
            if (boat == null)
            {
                e.Mobile.SendMessage("You are not on a boat.");
                return;
            }
            Point2D px = Point2D.Zero;
            try
            {
                if (e.ArgString.IndexOf(" N ", StringComparison.OrdinalIgnoreCase) >= 0 || e.ArgString.IndexOf(" S ", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Point3D px3 = Server.Items.Sextant.Parse(e.Mobile.Map, string.Join(" ", e.Arguments));
                    px = new Point2D(px3.X, px3.Y);
                }
                else
                    px = Point2D.Parse(string.Join(" ", e.Arguments));
            }
            catch
            {
                e.Mobile.SendMessage("Usage: MoveBoat <x y>");
                return;
            }
            boat.Location = new Point3D(px.X, px.Y, boat.Location.Z);
        }
        #endregion Move Boat
        #region Import/Export Rolled up sheet music
        public class ExpInfo
        {
            public string Username;
            public int Price;
            public ExpInfo(string username, int price)
            {
                Username = username;
                Price = price;
            }
        }
        private static void ImportRSM(CommandEventArgs e)
        {
            // from Angel Island
            Dictionary<int, ExpInfo> table = new() {
                    { 93233347, new ExpInfo("Dorundain",2000) },
                    { 662835438, new ExpInfo("Wagdog",2000) },
                    { -845254843, new ExpInfo("Wagdog",0) },
                    { -1035873931, new ExpInfo("Dorundain",5000) },
                    { -1721352356, new ExpInfo("Wagdog",500) },
                    { -638482194, new ExpInfo("Dorundain",5000) },
                    { -1733046116, new ExpInfo("random1",0) },
                    { -1308563267, new ExpInfo("Bravata",200) },
                    { 1701907019, new ExpInfo("Bravata",1000) },
                    { -1248138449, new ExpInfo("Chappelle4life3",6000) },
                    { 103000142, new ExpInfo("Wagdog",0) },
                    { 2116081743, new ExpInfo("Dorundain",1000) },
                    { 1174944509, new ExpInfo("Bravata",5000) },
                    { 681617492, new ExpInfo("Dorundain",5000) },
                    { -187482132, new ExpInfo("Chappelle4life3",500) },
                    { 992232335, new ExpInfo("Dorundain",5000) },
                    { -1366692645, new ExpInfo("Angel2",0) },
                    { 903223787, new ExpInfo("Wagdog",2000) },
                    { 220431633, new ExpInfo("Dorundain",0) },
                    { -1139706317, new ExpInfo("Dorundain",0) },
                    { 1812409723, new ExpInfo("Dorundain",2000) },
                    { -440882563, new ExpInfo("Dorundain",0) },
                    { 27770559, new ExpInfo("Dorundain",1000) },
                    { 644215459, new ExpInfo("Dorundain",2000) },
                    { 2083146414, new ExpInfo("Dorundain",1000) },
                    { 1784616072, new ExpInfo("Dorundain",2000) },
                    { -832981173, new ExpInfo("Dorundain",3000) },
                };

            LogHelper logger = new LogHelper("RolledUpSheetMusicRepair.log", true, true, true);
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicBox.GlobalMusicRepository)
            {
                if (table.ContainsKey(kvp.Key.HashCode) && kvp.Key.Owner == null || kvp.Key.Owner.Account == null)
                {
                    Mobile owner = null;
                    Accounting.Account acct = null;
                    if (GetOwnerInfo(table[kvp.Key.HashCode], out owner, out acct))
                    {
                        kvp.Key.Owner = owner;
                        kvp.Value.Price = table[kvp.Key.HashCode].Price;
                        kvp.Value.Owner = owner;
                        logger.Log(LogType.Text,
                            string.Format("music track {0} repaired.", kvp.ToString()));
                    }
                }
            }
            logger.Finish();
        }
        private static bool GetOwnerInfo(ExpInfo info, out Mobile m, out Accounting.Account acct)
        {
            m = null;
            acct = null;
            foreach (Accounting.Account current in Accounting.Accounts.Table.Values)
            {
                if (current.Username.Equals(info.Username, StringComparison.OrdinalIgnoreCase))
                {
                    ;
                    for (int ix = 0; ix < 5; ix++)
                    {
                        Mobile @char = current[ix];
                        if (@char != null)
                        {
                            m = @char;
                            acct = current;
                            return true;
                        }
                        ;
                        ;
                        ;

                    }
                    ;
                    ;
                    ;
                }
            }
            return false;
        }
        private static void ExportRSM(CommandEventArgs e)
        {
            //e.Mobile.SendMessage("Target container to store RSM...");
            //e.Mobile.Target = new ExportTarget(); // Call our target
            LogHelper logger = new LogHelper("RolledUpSheetMusic.log", true, true, true);
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicBox.GlobalMusicRepository)
            {
                /*e.Mobile.SendMessage(string.Format("Duping {0}, Owner: {1}, Serial: {2:X}, Hash Code: {3:X}",
                    kvp.Key.Name,                                       // 0
                    kvp.Key.Owner != null ? kvp.Key.Owner : "(null)",   // 1
                    kvp.Key.Serial,                                     // 2
                    kvp.Key.HashCode                                    // 3
                    ));*/

                /*
                // if the composer leaves the shard or deletes their character, all their music
                //  goes into the free bin.
                if (kvp.Value.Owner == null || kvp.Value.Owner.Deleted)
                    kvp.Value.Price = 0; 
                */
                if (kvp.Key.Owner != null && kvp.Key.Owner.Account != null)
                {
                    logger.Log(LogType.Text, string.Format("{{ {0}, new ExpInfo(\"{1}\",{2}) }},", kvp.Key.HashCode, (kvp.Key.Owner.Account as Accounting.Account).Username, kvp.Value.Price));
                }

            }
            logger.Finish();
        }

        public class ExportTarget : Target
        {
            public ExportTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is BaseContainer bc)
                {
                    bc.Movable = false;
                    bc.MaxItems = 4096;
                    int count = 0;
                    foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicBox.GlobalMusicRepository)
                    {
                        if (true)
                        {
                            /*e.Mobile.SendMessage(string.Format("Duping {0}, Owner: {1}, Serial: {2:X}, Hash Code: {3:X}",
                                kvp.Key.Name,                                       // 0
                                kvp.Key.Owner != null ? kvp.Key.Owner : "(null)",   // 1
                                kvp.Key.Serial,                                     // 2
                                kvp.Key.HashCode                                    // 3
                                ));*/
                            Item dupe = Utility.Dupe(kvp.Key);
                            bc.AddItem(dupe);
                            count++;
                        }
                    }
                }
                else
                {
                    from.SendMessage("That is not a container.");
                    return;
                }

                from.SendMessage("Duped {0} RSM.");
            }
        }
        #endregion Import/Export Rolled up sheet music
        #region Log Exterior Doors
        private static void LogExteriorDoors(CommandEventArgs e)
        {
            /*
             * new HousePlacementEntry( typeof( SmallOldHouse ),       1011303,    425,    212,    37000,      0,  4,  0,  0x0064  ),
                new HousePlacementEntry( typeof( SmallOldHouse ),       1011304,    425,    212,    37000,      0,  4,  0,  0x0066  ),
                new HousePlacementEntry( typeof( SmallOldHouse ),       1011305,    425,    212,    36750,      0,  4,  0,  0x0068  ),
                new HousePlacementEntry( typeof( SmallOldHouse ),       1011306,    425,    212,    35250,      0,  4,  0,  0x006A  ),
                new HousePlacementEntry( typeof( SmallOldHouse ),       1011307,    425,    212,    36750,      0,  4,  0,  0x006C  ),
                new HousePlacementEntry( typeof( SmallOldHouse ),       1011308,    425,    212,    36750,      0,  4,  0,  0x006E  ),
                new HousePlacementEntry( typeof( SmallShop ),           1011321,    425,    212,    50500,     -1,  4,  0,  0x00A0  ),
                new HousePlacementEntry( typeof( SmallShop ),           1011322,    425,    212,    52500,      0,  4,  0,  0x00A2  ),
                new HousePlacementEntry( typeof( SmallTower ),          1011317,    580,    290,    73500,      3,  4,  0,  0x0098  ),
                new HousePlacementEntry( typeof( TwoStoryVilla ),       1011319,    1100,   550,    113750,     3,  6,  0,  0x009E  ),
                new HousePlacementEntry( typeof( SandStonePatio ),      1011320,    850,    425,    76500,     -1,  4,  0,  0x009C  ),
                new HousePlacementEntry( typeof( LogCabin ),            1011318,    1100,   550,    81750,      1,  6,  0,  0x009A  ),
                new HousePlacementEntry( typeof( GuildHouse ),          1011309,    1370,   685,    131500,    -1,  7,  0,  0x0074  ),
                new HousePlacementEntry( typeof( TwoStoryHouse ),       1011310,    1370,   685,    162750,    -3,  7,  0,  0x0076  ),
                new HousePlacementEntry( typeof( TwoStoryHouse ),       1011311,    1370,   685,    162000,    -3,  7,  0,  0x0078  ),
                new HousePlacementEntry( typeof( LargePatioHouse ),     1011315,    1370,   685,    129250,    -4,  7,  0,  0x008C  ),
                new HousePlacementEntry( typeof( LargeMarbleHouse ),    1011316,    1370,   685,    160500,    -4,  7,  0,  0x0096  ),
                new HousePlacementEntry( typeof( Tower ),               1011312,    2119,   1059,   366500,     0,  7,  0,  0x007A  ),
                new HousePlacementEntry( typeof( Keep ),                1011313,    2625,   1312,   572750,     0, 11,  0,  0x007C  ),
                new HousePlacementEntry( typeof( Castle ),              1011314,    4076,   2038,   865250,     0, 16,  0,  0x007E  )
             */
            new SmallOldHouse(e.Mobile, new HousePlacementEntry(typeof(SmallOldHouse), 1011303, 425, 212, 37000, 0, 4, 0, 0x0064).MultiID);
            new SmallOldHouse(e.Mobile, new HousePlacementEntry(typeof(SmallOldHouse), 1011304, 425, 212, 37000, 0, 4, 0, 0x0066).MultiID);
            new SmallOldHouse(e.Mobile, new HousePlacementEntry(typeof(SmallOldHouse), 1011305, 425, 212, 36750, 0, 4, 0, 0x0068).MultiID);
            new SmallOldHouse(e.Mobile, new HousePlacementEntry(typeof(SmallOldHouse), 1011306, 425, 212, 35250, 0, 4, 0, 0x006A).MultiID);
            new SmallOldHouse(e.Mobile, new HousePlacementEntry(typeof(SmallOldHouse), 1011307, 425, 212, 36750, 0, 4, 0, 0x006C).MultiID);
            new SmallOldHouse(e.Mobile, new HousePlacementEntry(typeof(SmallOldHouse), 1011308, 425, 212, 36750, 0, 4, 0, 0x006E).MultiID);
            new SmallShop(e.Mobile, new HousePlacementEntry(typeof(SmallShop), 1011321, 425, 212, 50500, -1, 4, 0, 0x00A0).MultiID);
            new SmallShop(e.Mobile, new HousePlacementEntry(typeof(SmallShop), 1011322, 425, 212, 52500, 0, 4, 0, 0x00A2).MultiID);
            new SmallTower(e.Mobile);
            new TwoStoryVilla(e.Mobile);
            new SandStonePatio(e.Mobile);
            new LogCabin(e.Mobile);
            new GuildHouse(e.Mobile);
            new TwoStoryHouse(e.Mobile, new HousePlacementEntry(typeof(TwoStoryHouse), 1011310, 1370, 685, 162750, -3, 7, 0, 0x0076).MultiID);
            new TwoStoryHouse(e.Mobile, new HousePlacementEntry(typeof(TwoStoryHouse), 1011311, 1370, 685, 162000, -3, 7, 0, 0x0078).MultiID);
            new LargePatioHouse(e.Mobile);
            new LargeMarbleHouse(e.Mobile);
            new Tower(e.Mobile);
            new Keep(e.Mobile);
            new Castle(e.Mobile);


        }
        #endregion Log Exterior Doors
        #region PatchOracles
        public static void PatchOracles(CommandEventArgs e)
        {
            int count = 0;
            foreach (Mobile m in World.Mobiles.Values)
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
                        count++;
                    }
                }
            }
            e.Mobile.SendMessage("Patched {0} moongate wizards.", count);
        }
        #endregion
        #region FindLostContainer
        private static void FindLostContainer(CommandEventArgs e)
        {
            DateTime startDate = DateTime.UtcNow - TimeSpan.FromHours(24);
            DateTime endDate = DateTime.UtcNow + TimeSpan.FromHours(24);
            int found = 0;
            foreach (Item item in World.Items.Values)
                if (item is BaseContainer bc /*&& bc.Location == Point3D.Zero*/ && bc.Map == Map.Internal && bc.Movable /*&& bc.RootParent == null*/)
                    if (bc.GetType().Name.Contains("Crate"))
                        if (bc.LastAccessed >= startDate && bc.LastAccessed < endDate)
                        {
                            ; // breakpoint here
                            found++;
                            bc.MoveToWorld(e.Mobile.Location, e.Mobile.Map);
                            //e.Mobile.AddToBackpack(bc);
                        }

            e.Mobile.SendMessage("Found {0} crates.", found);
            return; // breakpoint here
        }
        #endregion FindLostContainer
        #region PatchRegionFlags
        private static void PatchRegionFlags(CommandEventArgs e)
        {
            int count = 0;
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
                    saveRegion.CanRessurect = dataRegion.CanRessurect;
                    saveRegion.EnableMusic = dataRegion.EnableMusic;
                    allSaveRegions.RemoveAt(indexOf);
                    count++;
                }
                else
                {
                    Console.WriteLine("Region \"{0}\" ({1}) no longer exists.", dataRegion.Name, dataRegion.Map);
                }
            }
            foreach (StaticRegion sr in allSaveRegions)
                Console.WriteLine("Unpatched region \"{0}\" ({1})", sr.Name, sr.Map);
            e.Mobile.SendMessage("Patched {0} region.", count);
        }
        #endregion
        #region PatchTownRegions
        private static void PatchTownRegions(CommandEventArgs e)
        {
            int count = 0;
            foreach (StaticRegion sr in StaticRegion.XmlDatabase)
            {
                if (Array.IndexOf(m_TownNames, sr.Name) != -1 || (sr.IsGuarded && CountVendors(sr) >= 5))
                {
                    sr.UseTownRules = true;
                    count++;
                }
            }
            e.Mobile.SendMessage("Patched {0} town regions.", count);
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
        #endregion
        #region LogUOSpawnMap
        private static void LogUOSpawnMap(CommandEventArgs e)
        {
            string area = "Area";
            List<Spawner> spawner_list = new();
            foreach (Item item in World.Items.Values)
                if (item is Spawner spawner)
                    if ((spawner.Name != null && spawner.Name.Contains(area, StringComparison.OrdinalIgnoreCase)) || spawner.Source.Contains(area, StringComparison.OrdinalIgnoreCase))
                        spawner_list.Add(spawner);

            // normalize
            Dictionary<int, List<Spawner>> spawner_area_designation = new();
            foreach (Spawner spawner in spawner_list)
            {
                int designation = 0;
                string text = spawner.Name + " " + spawner.Source;
                string[] tokens = text.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                int index = Array.FindIndex(tokens, t => t.Equals(area, StringComparison.OrdinalIgnoreCase));
                if (index >= 0 && index < tokens.Length - 1)
                {
                    designation = int.Parse(tokens[index + 1]);
                    if (spawner_area_designation.ContainsKey(designation))
                        spawner_area_designation[designation].Add(spawner);
                    else
                        spawner_area_designation.Add(designation, new List<Spawner>() { spawner });
                }
            }

            List<KeyValuePair<int, List<Spawner>>> sorted = new(spawner_area_designation);
            sorted.Sort((x, y) => x.Key.CompareTo(y.Key));

            LogHelper logger = new LogHelper("LogUOSpawnMap.log", true, true, true);
            foreach (var entry in sorted)
            {
                for (int ix = 0; ix < entry.Value.Count; ix++)
                {
                    logger.Log(LogType.Text, string.Format("{0}, {1}", entry.Value[ix].Source, entry.Value[ix].Location));
                    foreach (object o in entry.Value[ix].ObjectNamesRaw)
                        logger.Log(LogType.Text, string.Format("\t{0}", (o as string)));
                }
                logger.Log(LogType.Text, string.Format("---"));
            }
            logger.Finish();

            e.Mobile.SendMessage("{0} Areas logged", sorted.Count);

            return;
        }
        #endregion LogUOSpawnMap
        #region PatchUOSpawnMap
        private static void PatchUOSpawnMap(CommandEventArgs e)
        {
            string area = "Area";
            List<Spawner> spawner_list = new();
            foreach (Item item in World.Items.Values)
                if (item is Spawner spawner)
                    if ((spawner.Name != null && spawner.Name.Contains(area, StringComparison.OrdinalIgnoreCase)) || spawner.Source.Contains(area, StringComparison.OrdinalIgnoreCase))
                        spawner_list.Add(spawner);

            // normalize
            Dictionary<int, List<Spawner>> spawner_area_designation = new();
            foreach (Spawner spawner in spawner_list)
            {
                int designation = 0;
                string text = spawner.Name + " " + spawner.Source;
                string[] tokens = text.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                //int index = tokens.ToList().IndexOf(area, StringComparison.OrdinalIgnoreCase);
                int index = Array.FindIndex(tokens, t => t.Equals(area, StringComparison.OrdinalIgnoreCase));
                if (index >= 0 && index < tokens.Length - 1)
                {
                    designation = int.Parse(tokens[index + 1]);
                    if (spawner_area_designation.ContainsKey(designation))
                        spawner_area_designation[designation].Add(spawner);
                    else
                        spawner_area_designation.Add(designation, new List<Spawner>() { spawner });
                }
            }

            foreach (var kvp in spawner_area_designation)
            {
                int id = 1;
                foreach (Spawner target in kvp.Value)
                {
                    target.Name = null;
                    target.Source = area + " " + string.Format("{0}, {1} of {2}", kvp.Key, id++, kvp.Value.Count);
                }
            }

            return;
        }
        #endregion PatchUOSpawnMap
        #region DeleteOrphanedUnusualkeys
        private static void DeleteOrphanedUnusualkeys(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Finding all Unusual Keys...");
            List<Key> key_list = new List<Key>();
            List<KeyRing> ring_list = new List<KeyRing>();
            foreach (Item item in World.Items.Values)
            {
                if (item is Key key && !key.Deleted /*&& key.Type == KeyType.Magic*/)
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
                    orphaned_key_list.Add(key);
                }
            }

            foreach (Key key in orphaned_key_list)
                key.Delete();

            e.Mobile.SendMessage(string.Format("Deleted {0} orphaned keys", orphaned_key_list.Count));
            e.Mobile.SendMessage(string.Format("Found {0} well-loved keys", loved));
            e.Mobile.SendMessage("Done.");
        }
        #endregion DeleteOrphanedUnusualkeys
        #region CheckHome
        private static void CheckHome(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            foreach (Mobile mob in World.Mobiles.Values)
            {
                if (mob is BaseCreature bc && !bc.Deleted && bc.Spawner != null)
                    if (bc.Map != null && bc.Map != Map.Internal && bc.Spawner.Map != null && bc.Spawner.Map != Map.Internal)
                        if (bc.Spawner.Distro == SpawnerModeAttribs.ModeLegacy)
                            if (bc.Home != bc.Spawner.Location)
                            {
                                //bc.Home = bc.Spawner.Location;
                                //bc.MoveToWorld(bc.Spawner.Location, bc.Spawner.Map);
                                pm.JumpList.Add(bc);
                            }
            }

            pm.SendMessage("Jump list loaded with {0} entries.", pm.JumpList.Count);
        }
        #endregion CheckHome
        #region GetObjectProperties
        private static void GetObjectProperties(CommandEventArgs e)
        {
            #region Logging
            LogHelper logger = new LogHelper("GetObjectProperties.log", false, false);
            List<string> list = Utility.GetObjectProperties(e.Mobile);
            foreach (string s in list)
                logger.Log(LogType.Text, string.Format(s));
            logger.Finish();
            #endregion Logging
        }
        #endregion GetObjectProperties
        #region TileData
        private static void TileData(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            LandTile landTile = pm.Map.Tiles.GetLandTile(pm.X, pm.Y);
            TileFlag landFlags = Server.TileData.LandTable[landTile.ID & 0x3FFF].Flags;
            e.Mobile.SendGump(new PropertiesGump(e.Mobile, landTile));
            e.Mobile.SendGump(new PropertiesGump(e.Mobile, landFlags));
            e.Mobile.SendMessage("Done.");
        }
        #endregion TileData
        #region BuildNerunSpawner
        private static void BuildNerunSpawner(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            // build the spawner
            Spawner spawner = Activator.CreateInstance(typeof(Spawner)) as Spawner;     // create the spawner

            // add the mobs
            spawner.ObjectNamesRaw.Add("cat:dog");
            spawner.ObjectNamesRaw.Add("crow");
            spawner.ObjectNamesRaw.Add("chicken:cow:sheep");
            spawner.ObjectNamesRaw.Add("pig:goat");
            spawner.ObjectNamesRaw.Add("boar");

            spawner.MinDelay = TimeSpan.FromMinutes(1);
            spawner.MaxDelay = TimeSpan.FromMinutes(3);
            spawner.CoreSpawn = true;
            spawner.Shard = Spawner.ShardConfig.Core;
            spawner.NeedsReview = false;
            spawner.Distro = SpawnerModeAttribs.ModeNeruns;
            spawner.Source = "Nerun's Distro";

            // set the properties
            spawner.HomeRange = 10;                       // how far the spawner can fling the mobiles
            spawner.WalkRange = 10;                       // how far the mobile can walk after being spawned
                                                          // how many creatures to spawn for each entry in the spawner's creatures list
            spawner.SetSlotCount(index: 0, value: 2);        // mobile count (to spawn) for first cell in the spawner gump
            spawner.SetSlotCount(index: 1, value: 2);// these next 5 fields apply to the next 5 creatures in the spawner gump
            spawner.SetSlotCount(index: 2, value: 2);//  each of these five mobiles are capable of having it's own 'count'
            spawner.SetSlotCount(index: 3, value: 2);
            spawner.SetSlotCount(index: 4, value: 2);
            spawner.SetSlotCount(index: 5, value: 2);


            // temp look while we review
            spawner.ItemID = 0x14f0;    // deed graphic
            spawner.Hue = 70;           // green means go!
            spawner.Visible = true;     // we want to see the color

            // convert to Concentric for the wide area spawners
            if (spawner.HomeRange > 25)
                spawner.Concentric = true;

            // place it (<== seems a bit anticlimactic)
            spawner.MoveToWorld(new Point3D(pm.X, pm.Y, pm.Z), pm.Map);

            e.Mobile.SendMessage("Done.");
        }
        #endregion BuildNerunSpawner
        #region AddTrainerChestsToBanks
        private static void AddTrainerChestsToBanks(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();

            List<BaseContainer> containers = new();
            foreach (Mobile m in World.Mobiles.Values)
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
                    pm.JumpList.Add(spawner);
                }
            }

            e.Mobile.SendMessage("Your jumplist has been initialized with {0} entries", pm.JumpList.Count);
        }
        #endregion AddTrainerChestsToBanks
        #region FindDungeonExitTeles
        private static void FindDungeonExitTeles(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            foreach (Item item in World.Items.Values)
            {
                Teleporter tp = item as Teleporter;
                if (tp != null && IsDungeonTeleporter(tp) && !IsDungeon(tp.PointDest))
                {
                    pm.JumpList.Add(tp);
                }
            }
            e.Mobile.SendMessage("Done");
        }
        #endregion FindDungeonExitTeles
        #region BritainFarmsPatch
        private static void BritainFarmsPatch(CommandEventArgs e)
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
                e.Mobile.SendMessage("Failed to generate Britain Farms region.");
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
            e.Mobile.SendMessage("Successfully generated Britain Farms region.");
        }
        #endregion
        #region AnkhPatch
        private static void AnkhPatch(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Patched {0} ankhs.", Ankhs.PatchAllAnkhs());
        }
        #endregion
        #region Stackable Item Names

        private static void StackableItemNames(CommandEventArgs e)
        {
            List<Type> seen = new List<Type>();
            LogHelper logger = new LogHelper("StackableItemNames.log", true, true, true);
            foreach (Item item in World.Items.Values)
            {
                if (item != null && item.Deleted == false && item.Stackable)
                {
                    if (!seen.Contains(item.GetType()))
                    {
                        seen.Add(item.GetType());
                        Item item1 = Utility.Dupe(item);
                        Item item2 = Utility.Dupe(item);
                        item1.Amount = 1;
                        item2.Amount = 10;
                        logger.Log(LogType.Text, string.Format("{0,-25}\t{1,-25}\t{2,-25}\t", item.ItemData.Name, item1.OldSchoolName(), item2.OldSchoolName()));
                        item1.Delete();
                        item2.Delete();
                    }
                }
            }
            logger.Finish();
            e.Mobile.SendMessage("done");
        }
        #endregion Stackable Item Names
        #region Aggressive Action
        private static void AggressiveAction(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target mobile...");
            e.Mobile.Target = new AggressiveActionTarget(); // Call our target
        }

        public class AggressiveActionTarget : Target
        {
            public AggressiveActionTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is BaseCreature creature)
                {
                    creature.AggressiveAction(from);

                    from.SendMessage("done.");
                }
                else
                {
                    from.SendMessage("That is not a creature.");
                    return;
                }
            }
        }
        #endregion Aggressive Action
        #region Restore My Stats
        private static void RestoreMyStats(CommandEventArgs e)
        {
            e.Mobile.Hits = e.Mobile.HitsMax;
            e.Mobile.Stam = e.Mobile.StamMax;
            e.Mobile.Int = e.Mobile.IntMax;
            e.Mobile.SendMessage("Stat restored.");
        }
        #endregion Restore My Stats
        #region KinMigrate
        private static void KinMigrate(CommandEventArgs e)
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
            e.Mobile.SendMessage("Migrated {0} IOB guilds to the Alignment system.", count);
        }
        #endregion KinMigrate
        #region DirectionalDungeonTPs
        private static void DirectionalDungeonTPs(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
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
            e.Mobile.SendMessage("Patched {0} TPs.", patched);
            foreach (Teleporter tp in failed)
            {
                pm.JumpList.Add(tp.GetWorldLocation());
                e.Mobile.SendMessage("Failed group at {0} ({1}).", tp.Location, tp.Map);
            }

            if (pm.JumpList.Count > 0)
                e.Mobile.SendMessage("Your jumplist has been loaded with {0} failed updates).", pm.JumpList.Count);
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
        #endregion
        #region List Aggressors
        private static void ListAggressors(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target mobile to query...");
            e.Mobile.Target = new ListAggressorsTarget(); // Call our target
        }

        public class ListAggressorsTarget : Target
        {
            public ListAggressorsTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is BaseCreature creature)
                {
                    // list Aggressors
                    List<AggressorInfo> list = creature.Aggressors;

                    for (int i = 0; i < list.Count; ++i)
                    {
                        AggressorInfo ai = (AggressorInfo)list[i];

                        from.SendMessage("Aggressors: {0}", ai.Attacker);
                    }

                    from.SendMessage("Listed {0} aggressors", list.Count);

                    // now for Aggressed
                    list = creature.Aggressed;

                    for (int i = 0; i < list.Count; ++i)
                    {
                        AggressorInfo ai = (AggressorInfo)list[i];

                        from.SendMessage("Aggressed: {0}", ai.Attacker);
                    }

                    from.SendMessage("Listed {0} aggressed", list.Count);
                }
                else
                {
                    from.SendMessage("That is not a creature.");
                    return;
                }
            }
        }
        #endregion List Aggressors
        #region DisableSolenCaveSpawners
        private static void DisableSolenCaveSpawners(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted == true || item.Map == Map.Internal)
                    continue;
                if (item is Spawner spawner && spawner.Running == true)
                    if (Utility.World.SolenCaves.Contains(spawner.Location))
                    {
                        spawner.RemoveObjects();
                        spawner.Running = false;
                        pm.JumpList.Add(item);
                    }
            }

            pm.SendMessage("Your jump list has been loaded with {0} objects", pm.JumpList.Count);
        }
        #endregion DisableSolenCaveSpawners
        #region TelesFromToSolenCaves
        private static void TelesFromToSolenCaves(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted == true || item.Map == Map.Internal)
                    continue;
                if (item is Teleporter tele && tele.Running == true)
                    if (TeleporterFromTo(tele, new List<Rectangle2D>() { Utility.World.SolenCaves }, new List<Rectangle2D>() { Utility.World.SolenCaves }))
                    {

                        pm.JumpList.Add(item);
                    }
            }

            pm.SendMessage("Your jump list has been loaded with {0} objects", pm.JumpList.Count);
        }
        #endregion TelesFromToSolenCaves
        #region Map Solen Caves
        private static void MapSolenCaves(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            List<Point2D> points = new();
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted == true || item.Map == Map.Internal)
                    continue;
                if (item is RecallRune rr && rr.Description is not null && rr.Description.Contains("solen beacon"))
                {
                    points.Add(new Point2D(item.Location.X, item.Location.Y));
                    pm.JumpList.Add(item);
                }
            }
            Rectangle2D rect = new Rectangle2D();
            foreach (var point in points)
                rect.MakeHold(point);
            pm.SendMessage("Your new rectangle: {0}", rect);
            pm.SendMessage("Your jump list has been loaded with {0} objects", pm.JumpList.Count);
        }
        #endregion Map Solen Caves
        #region Find Active Mobiles
        private static void ActiveMobiles(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            LogHelper logger = new LogHelper("item spawners.log", false, true, true);
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile == null || mobile.Deleted == true || mobile.Map == Map.Internal)
                    continue;

                if (mobile is BaseCreature bc && bc.Active)
                    pm.JumpList.Add(bc);
            }
            logger.Finish();

            pm.SendMessage("Your jump list has been loaded with {0} objects", pm.JumpList.Count);
        }
        #endregion Find Active Mobiles
        #region Find Item Spawners
        private static void FindItemSpawners(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            LogHelper logger = new LogHelper("item spawners.log", false, true, true);
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted == true || item.Map != Map.Felucca)
                    continue;

                if (item is Spawner spawner && spawner.ModeNeruns == false && spawner.Running == false)
                {
                    spawner.Respawn();
                    bool found = false;
                    foreach (object o in spawner.Objects)
                    {
                        if (o is Item)
                        {
                            string name = (o as Item).GetType().Name;
                            // crap at WBB Test Center and other 'non rares'
                            bool skip = name.Contains("TreasureChest") || name.Contains("Camp") || name.Contains("Moongate") ||
                                name.Contains("GiftBox") || name.Contains("BlacksmithCooperativeDeed") || name.Contains("TenjinsHammer") ||
                                name.Contains("DeathRobe") || name.Contains("TribalPaint") || name.Contains("HousePlacementTool") ||
                                name.Contains("IronIngot") || name.Contains("TribalBerry") || name.Contains("AncientSmithyHammer") ||
                                name.Contains("KukuiNut") || name.Contains("EasterEggs");
                            if (skip == false)
                                skip = o is BaseReagent;
                            if (!skip)
                            {
                                logger.Log(LogType.Text, string.Format("{0} {1}", name, (o as Item).Location));
                                found = true;
                            }
                        }
                    }
                    if (found)
                        pm.JumpList.Add(item);
                    spawner.RemoveObjects();
                }
            }
            logger.Finish();

            pm.SendMessage("Your jump list has been loaded with {0} objects", pm.JumpList.Count);
        }
        #endregion Find Item Spawners
        #region Get Loot Packs
        private static void FindLootPacks(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            List<Container> lootPacks = new List<Container>();
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted == true)
                    continue;

                if (item is Spawner || item is ChestLootPackSpawner)
                {
                    Spawner s = item as Spawner;
                    ChestLootPackSpawner cis = item as ChestLootPackSpawner;
                    Container c = null;
                    if (s != null && s.LootPack != null)
                    {
                        lootPacks.Add(s.LootPack as Container);
                        pm.JumpList.Add(item);
                    }
                    else if (cis != null && cis.LootPack != null)
                    {
                        lootPacks.Add(cis.LootPack as Container);
                        pm.JumpList.Add(item);
                    }
                }
            }

            pm.SendMessage("Your jump list has been loaded with {0} objects", pm.JumpList.Count);
        }
        #endregion Find Loot Packs
        #region Fix Nerun's Spwaner Z 
        private static void FixNerunsSpwanerZ(CommandEventArgs e)
        {
            int found = 0;
            int @fixed = 0;
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            List<Item> garbagecan = new List<Item>();
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Map != Map.Felucca || item.Deleted) continue;

                // while we're here, get rid of all the stacked spawner consoles
                if (item is StackedSpawnerConsole) garbagecan.Add(item);
                if (item is Spawner spawner && spawner.ModeNeruns == true)
                {
                    found++;

                    // find the surface on which we should be placed
                    int ceiling = Utility.HardCeiling(spawner.Map, spawner.X, spawner.Y, spawner.Z);
                    // move the spawner up/down
                    if (Math.Abs(spawner.Z - ceiling) <= 10)
                    {
                        @fixed++;
                        spawner.Z = ceiling;
                        // now jiggle - de-stack
                        // also, sometimes we're on like a display case, avoid if we can.
                        spawner.Location = Jiggle(spawner.Map, spawner.Location, spawner);
                        spawner.Respawn();
                        pm.JumpList.Add(spawner);
                    }

                }
            }

            foreach (Item garbage in garbagecan)
                garbage.Delete();

            e.Mobile.SendMessage("{0} spawners checked, {1} fixed.", found, @fixed);
            e.Mobile.SendMessage("Jump list loaded with {0} entries.", pm.JumpList.Count);
        }
        private static Point3D Jiggle(Map map, Point3D point, object o)
        {
            for (int ix = 0; ix < 16; ix++)
            {
                point = Spawner.GetSpawnPosition(map, point, 2, SpawnFlags.None, o);
                if (SpawnerAtLocation(map, point) == false)
                    break;
                else
                    ;
            }

            return point;
        }
        private static bool SpawnerAtLocation(Map map, Point3D point)
        {
            Spawner spawner = (Spawner)FindOneItemAt(point, map, typeof(Spawner));
            if (spawner != null)
                return true;

            return false;
        }
        #endregion Fix Nerun's Spwaner Z 
        #region Find fillable containers in dungeons
        private static void FillableDungeonContainers(CommandEventArgs e)
        {
            int count = 0;
            int found = 0;
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Map != Map.Felucca || item.Deleted) continue;
                if (item is FillableContainer && item is not LibraryBookcase)
                {
                    count++;
                    Region rx;
                    if ((rx = Region.Find(item.Location, item.Map)) == null)
                        continue;
                    if (!rx.IsDungeonRules)
                        continue;

                    found++;
                }
            }

            e.Mobile.SendMessage("{0} FillableContainers checked, {1} in a dungeon.", count, found);
        }
        #endregion Find fillable containers in dungeons
        #region Item Capture
        private static void ItemCapture(CommandEventArgs e)
        {
            int radius = 10;
            List<Type> ignore = new List<Type>() { typeof(Teleporter), typeof(Spawner) };
            e.Mobile.SendMessage("Target center of the capture area...");
            e.Mobile.Target = new ItemCaptureTarget(radius, ignore);
        }
        public class ItemCaptureTarget : Target
        {
            private int m_radius;
            private List<Type> m_ignore;
            public ItemCaptureTarget(int radius, List<Type> ignore = null)
                : base(17, true, TargetFlags.None)
            {
                m_radius = radius;
                m_ignore = ignore;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;
                if (loc != Point3D.Zero)
                {
                    LogHelper logger = new LogHelper("capture.log", false, true, true);
                    IPooledEnumerable eable = from.Map.GetItemsInRange(loc, m_radius);
                    foreach (Item item in eable)
                    {
                        if (item == null) continue;
                        if (m_ignore != null && m_ignore.Contains(item.GetType()))
                            continue;

                        logger.Log(LogType.Text,

                            string.Format(@"new KeyValuePair<int,Point3D>({0}, new Point3D{1}),", item.ItemID, item.Location)

                            );

                    }
                    eable.Free();
                    logger.Finish();
                }
                else
                {

                    return;
                }
            }
        }
        #endregion Item Capture
        #region Moongates
        private static void Moongates(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            foreach (Mobile mob in World.Mobiles.Values)
            {
                if (mob != null && mob is MoonGateWizard)
                    pm.JumpList.Add(mob);
            }
            ;
            pm.SendMessage("{0} entries added to your jumplist", pm.JumpList.Count);
            ;

        }
        #endregion Moongates
        #region diagnose WanderingHealer:EvilWanderingHealer
        private static void DiagnoseWanderingHealer(CommandEventArgs e)
        {
            List<Spawner> wanderingHealer = new List<Spawner>();
            List<Spawner> evilWanderingHealer = new List<Spawner>();
            foreach (Mobile mob in World.Mobiles.Values)
            {
                if (mob == null || mob.Deleted) continue;
                if (mob.Map != Map.Felucca) continue;
                if (mob.Spawner == null) continue;
                if (mob.GetType() == typeof(WanderingHealer))
                    wanderingHealer.Add(mob.Spawner);
                if (mob.GetType() == typeof(EvilWanderingHealer))
                    evilWanderingHealer.Add(mob.Spawner);
            }

            ;
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            foreach (Spawner spawner in evilWanderingHealer)
            {
                if (!wanderingHealer.Contains(spawner))
                    pm.JumpList.Add(spawner);
            }
            ;
            pm.SendMessage("{0} entries added to your jumplist", pm.JumpList.Count);
            ;

        }
        #endregion diagnose WanderingHealer:EvilWanderingHealer
        #region test spawner clumping
        private static void SetSpawnerEdgeGravity(CommandEventArgs e)
        {
            if (e.Length != 2 || int.TryParse(e.Arguments[1], out Spawner.EdgeGravity) == false)
                e.Mobile.SendMessage("Usage SetSpawnerEdgeGravity <int>");
            else
                e.Mobile.SendMessage("Spawner Edge Gravity set to {0}", Spawner.EdgeGravity);
        }
        private static void TestSpawnerClumping(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target spawner to test...");
            e.Mobile.Target = new TestSpawnerTarget(); // Call our target
        }
        public class TestSpawnerTarget : Target
        {
            public TestSpawnerTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is Spawner spawner)
                {
                    double near = spawner.HomeRange / 3.0;
                    double medium = near * 2.0;
                    double far = near * 3.0;

                    int near_spawn = 0;
                    int medium_spawn = 0;
                    int far_spawn = 0;
                    for (int ix = 0; ix < 100; ix++)
                    {
                        spawner.Respawn();

                        foreach (object o in spawner.Objects)
                        {
                            Point3D px = new Point3D();
                            if (o is Item item)
                                px = item.Location;
                            if (o is Mobile mobile)
                                px = mobile.Location;

                            double distance = Utility.GetDistanceToSqrt(spawner.Location, px);

                            if (distance <= near)
                            {
                                near_spawn++;
                            }
                            else if (distance <= medium)
                            {
                                medium_spawn++;
                            }
                            else
                            {
                                far_spawn++;
                            }
                        }
                    }

                    int count = spawner.Objects.Count;
                    from.SendMessage("Gravity set to {0}", Spawner.EdgeGravity);
                    from.SendMessage("{0} Objects nearby. {1}", near_spawn, string.Format("{0:N2}%",
                        YisWhatPercentOfX(near_spawn, count * 100)));
                    from.SendMessage("{0} Objects medium distance. {1}", medium_spawn, string.Format("{0:N2}%",
                        YisWhatPercentOfX(medium_spawn, count * 100)));
                    from.SendMessage("{0} Objects far distance. {1}", far_spawn, string.Format("{0:N2}%",
                        YisWhatPercentOfX(far_spawn, count * 100)));

                    from.SendMessage("{0} total objects spawned", count * 100);
                }
                else
                {
                    from.SendMessage("That is not a spawner.");
                    return;
                }
            }
        }
        private static double YisWhatPercentOfX(double y, double x)
        {
            /*
             * Solving our equation for P
             * P% = Y/X
             * P% = 9.3/100
             * p = 0.093
             */
            return (y / x) * 100.0;
        }
        #endregion test spawner clumping
        #region orange gorgets
        private static void OrangeGorgets(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            int start = 1501;
            int end = start + 54;
            for (int ix = start; ix <= end; ix++)
            {
                Item item = new LeatherGorget();
                item.Hue = ix;
                pm.AddToBackpack(item);
            }
        }
        #endregion orange gorgets

        #region Find Farm Spawners
        private static void FindFarmSpawners(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            List<Type> exclude = new List<Type>() { typeof(FruitBasket) };
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted) continue;
                if (item.Map != Map.Felucca) continue;
                if (item is Spawner spawner)
                {
                    if (spawner.Running) continue;

                    if (Utility.SpawnerSpawns(spawner, "cottonplant") || Utility.SpawnerSpawns(spawner, typeof(Food), exclude))
                        pm.JumpList.Add(spawner);
                }
            }

            pm.SendMessage("Your jump list has been loaded with {0} entries.", pm.JumpList.Count);
        }
        #endregion Find Farm Spawners
        #region DrawLine
        private static void DrawLine(CommandEventArgs e)
        {
            Point2D px1 = new Point2D(622, 1496);
            Point2D px2 = new Point2D(631, 1496);

            Line line = new Line(px1, px2);
            double dist = Utility.GetDistanceToSqrt(px1, px2) + 1;
            var table = line.GetPoints((int)dist);
            foreach (var node in table)
            {
                int z = Utility.GetAverageZ(Map.Felucca, node.X, node.Y);
                LOSBlocker losb = new LOSBlocker();
                losb.MoveToWorld(new Point3D(node, 0), Map.Felucca);
            }
        }
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
        #endregion DrawLine

        #region Load Region Jump
        private static void LoadRegionJump(CommandEventArgs e)
        {
            if (e.Length < 2 || Region.FindByName(RightString(e.ArgString), e.Mobile.Map) == null)
            {
                e.Mobile.SendMessage("Usage: [LoadRegionJump <region name>");
                return;
            }
            Region rx = Region.FindByName(RightString(e.ArgString), e.Mobile.Map);
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            foreach (Rectangle3D p3d in rx.Coords)
            {
                pm.JumpList.Add(p3d.Start);                                             // top left
                pm.JumpList.Add(new Point2D(p3d.Start.X + p3d.Width, p3d.Start.Y));    // top right
                pm.JumpList.Add(new Point2D(p3d.Start.X, p3d.Start.Y + p3d.Height));    // bottom left
                pm.JumpList.Add(p3d.End);
            }

            pm.SendMessage("Your jump list has been loaded with {0} locations.", pm.JumpList.Count);
        }
        private static string RightString(string s)
        {
            int index = s.IndexOf(' ');
            if (index == -1) return s;
            else return s.Substring(index).Trim();
        }
        #endregion Load Region Jump
        #region AddTame
        private static void AddTame(CommandEventArgs e)
        {
            if (e.Length == 2)
            {
                e.Mobile.SendMessage("Target location to spawn...");
                string creature = e.Arguments[1];
                e.Mobile.Target = new AddTameTarget(creature); // Call our target
            }
            else
                e.Mobile.SendMessage("Usage: [AddTame <base creature type>");
        }

        public class AddTameTarget : Target
        {
            string m_creature;
            public AddTameTarget(string creature)
                : base(17, true, TargetFlags.None)
            {
                m_creature = creature;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;
                if (loc != Point3D.Zero)
                {
                    Type type = SpawnerType.GetType(m_creature);
                    if (type != null)
                    {
                        Mobile m = Activator.CreateInstance(type) as Mobile;
                        if (m is BaseCreature bc)
                        {
                            bc.MoveToWorld(loc, from.Map);
                            bc.Controlled = true;
                            bc.ControlMaster = from;
                            bc.ControlOrder = OrderType.Guard;
                        }
                    }
                    else
                        from.SendMessage("{0} is not a valid creature name", m_creature);
                }
                else
                {
                    from.SendMessage("Bad location");
                    return;
                }
            }
        }

        #endregion AddTame
        #region CheckBlocked
        private static void CheckBlocked(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target mobile or location to test...");
            e.Mobile.Target = new CheckBlockedTarget(); // Call our target
        }

        public class CheckBlockedTarget : Target
        {
            public CheckBlockedTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;
                if (loc != Point3D.Zero)
                {
                    from.SendMessage("Location is {0}.", Spawner.Blocked(loc, loc, map: from.Map) ? "blocked" : "not blocked");
                    from.SendMessage("Done.");
                }
                else
                {
                    from.SendMessage("Bad location");
                    return;
                }
            }
        }
        #endregion CheckBlocked
        #region Get Hard Zz
        private static void GetHardZs(CommandEventArgs e)
        {
            List<int> list = Utility.GetHardZs(e.Mobile.Map, e.Mobile.Location);
            foreach (int z in list)
                e.Mobile.SendMessage("{0}", z);
            e.Mobile.SendMessage("done.");
        }
        #endregion Get Hard Zs
        #region Find Hard ZBreaks
        private static void FindHardZBreaks(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            StaticTile[] staticTiles = from.Map.Tiles.GetStaticTiles(from.X, from.Y, true);
            Array.Reverse(staticTiles);
            if (staticTiles.Length > 0)
                foreach (StaticTile st in staticTiles)
                {
                    from.SendMessage("Static Tile breaks at {0}", st.Z);
                }
            else
            {
                from.SendMessage("There are no static tiles at {0} ({1})", from.Location, from.Map);
                LandTile lt = from.Map.Tiles.GetLandTile(from.X, from.Y);
                from.SendMessage("Land Tile break at {0}", lt.Z);
            }
        }
        #endregion Find Hard ZBreaks

        #region Test TreasureMap Locations
        private static void TestTreasureMapLocations(CommandEventArgs e)
        {
            int lcount = 0;
            int rcount = 0;
            foreach (Point2D p2d in TreasureMap.Locations)
            {
                BaseMulti bm = BaseMulti.Find(new Point3D(p2d.X, p2d.Y, Utility.GetAverageZ(e.Mobile.Map, p2d.X, p2d.Y)), e.Mobile.Map);
                if (bm != null)
                    lcount++;
            }

            for (int ix = 0; ix < 1000000; ix++)
            {   // GetRandomLocation is now filtered on base multi collisions. 
                Point2D p2d = TreasureMap.GetRandomTreasureMapLocation(map: Map.Felucca);
                BaseMulti bm = BaseMulti.Find(new Point3D(p2d.X, p2d.Y, Utility.GetAverageZ(e.Mobile.Map, p2d.X, p2d.Y)), e.Mobile.Map);
                if (bm != null)
                    rcount++;
            }

            e.Mobile.SendMessage("Out of {0} possible dynamic locations...", TreasureMap.Locations.Length);
            e.Mobile.SendMessage("There are {0} collisions with BaseMulti in TreasureMap.Locations", lcount);
            e.Mobile.SendMessage("There are {0} collisions with BaseMulti using filtered GetRandomLocation()", rcount);

            return;
        }
        #endregion Test TreasureMap Locations
        #region Blink region controllers so that they reflect (in hue) there correct 'registered' status
        private static void Blink(CommandEventArgs e)
        {
            foreach (Item item in World.Items.Values)
            {
                if (item is null || item.Deleted) continue;
                if (item.Map == null) continue;

                if (item is CustomRegionControl crc)
                {   // reset the colorization state (all shards)
                    crc.Registered = !crc.Registered;
                    crc.Registered = !crc.Registered;
                }
                if (item is StaticRegionControl src)
                {   // reset the colorization state (all shards)
                    src.Registered = !src.Registered;
                    src.Registered = !src.Registered;
                }
            }
            e.Mobile.SendMessage("Done.");
        }
        #endregion Blink region controllers so that they reflect (in hue) there correct 'registered' status
        #region Log Map Regions
        private static void LogMapRegions(CommandEventArgs e)
        {
            List<string> regions = new List<string>();
            List<Map> maps = new List<Map>() { Map.Felucca, Map.Trammel, Map.Malas, Map.Ilshenar };
            foreach (Map map in maps)
                foreach (Region rx in map.Regions.Values)
                {
                    if (!rx.Name.ToLower().Contains("dynregion"))
                        regions.Add(string.Format("{0} ({1}:{2})", rx.Name, map.Name, Controller(rx)));
                    else
                        ; // debug break point
                }

            regions.Sort();

            LogHelper logger = new LogHelper("LogMapRegions.log", true, true, true);
            foreach (string sx in regions)
            {
                logger.Log(string.Format("{0}", sx));
            }
            logger.Finish();
            e.Mobile.SendMessage("Done.");
        }
        private static string Controller(Region rx)
        {
            Point3D location = new Point3D(0, 0, 0);
            bool src = false;
            bool crc = false;
            if (rx is StaticRegion && StaticRegion.Controller(rx as StaticRegion) != null)
            {
                StaticRegionControl rc = StaticRegion.Controller(rx as StaticRegion);
                if (rc != null)
                {
                    if (rc != null)
                    {
                        location = rc.Location;
                        src = true;
                    }
                }
            }
            else if (rx is CustomRegion)
            {
                CustomRegionControl rc = ((CustomRegion)rx).Controller;
                if (rc != null)
                {
                    location = rc.Location;
                    crc = true;
                }
            }

            return string.Format("{0} {1}", location.ToString(), src ? "Static" : crc ? "Dynamic" : "Unknown");
        }
        #endregion Log Map Regions
        #region FactionOreVendor
        private static void FactionOreVendor(CommandEventArgs e)
        {
            Server.Factions.FactionOreVendor orev = new Server.Factions.FactionOreVendor(Town.Towns[0], Faction.Factions[0]);
            orev.MoveToWorld(e.Mobile.Location, e.Mobile.Map);
        }
        #endregion FactionOreVendor
        #region Test Random Enum
        private static void TestRandomGear(CommandEventArgs e)
        {   // called from Nuke: a test
            var v = Enum.GetValues(typeof(WeaponDamageLevel));
            int count = v.Length;
            double[] tab = new double[count];
            int test_level = 3;                  // change this to the level you wish to test
            double upgrade_chance = 0.0;    // change this to the upgrade chance you want applied
            for (int jx = 0; jx < 100; jx++)
                for (int ix = 0; ix <= count; ix++)
                {
                    WeaponDamageLevel level = Loot.GetGearForLevel<WeaponDamageLevel>(test_level, upgrade_chance);
                    tab[(int)level]++;
                }

            for (int ix = 0; ix < count; ix++)
                tab[ix] = tab[ix] / 100;

            double total = tab.Sum();

            e.Mobile.SendMessage("Done.");
        }
        private static void TestImbuing(CommandEventArgs e)
        {   // called from Nuke: a test
            var v = Enum.GetValues(typeof(WeaponDamageLevel));
            int count = v.Length;
            double[] tab = new double[count];
            Katana katana = new Katana();

            for (int jx = 0; jx < 100; jx++)
                for (int ix = 0; ix <= count; ix++)
                {
                    Loot.ImbueWeaponOrArmor(true, katana, (Loot.ImbueLevel)ix, 0.0, false);
                    tab[(int)katana.DamageLevel]++;
                    // reset
                    katana.DamageLevel = WeaponDamageLevel.Regular;
                }

            katana.Delete();

            for (int ix = 0; ix < count; ix++)
                tab[ix] = tab[ix] / 100;

            double total = tab.Sum();

            e.Mobile.SendMessage("Done.");
        }
        private static void TestRandomEnum(CommandEventArgs e)
        {
            for (int ix = 0; ix < 100000; ix++)
            {
                WeaponDamageLevel dl = Utility.RandomEnumMinMaxScaled<WeaponDamageLevel>(0, 6);
                int dli = (int)dl;
                if (dli < 0 || dli >= 6)
                    ;
                if (dli == 5)
                    ;
                Console.WriteLine(dli);
            }

            e.Mobile.SendMessage("Done.");

        }
        #endregion Test Random Enum
        #region Rebuild Mobile
        private static void BuildMobile(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target mobile to rebuild...");
            e.Mobile.Target = new BuildMobileTarget(); // Call our target
        }

        public class BuildMobileTarget : Target
        {
            public BuildMobileTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is Mobile mobile)
                {
                    // now for the stats
                    mobile.Stam = mobile.RawDex;
                    mobile.Mana = mobile.RawInt;
                    mobile.Hits = Math.Max(mobile.RawStr, mobile.HitsMax);

                    from.SendMessage("Mobile rebuilt.");
                }
                else
                {
                    from.SendMessage("That is not a Mobile.");
                    return;
                }
            }
        }
        #endregion Rebuild Mobile
        #region Build Player
        private static void BuildAdam(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target player to construct...");
            e.Mobile.Target = new BuildAdamTarget(); // Call our target
        }

        public class BuildAdamTarget : Target
        {
            public BuildAdamTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile player)
                {
                    // all skills 100
                    Server.Skills skills = player.Skills;
                    for (int i = 0; i < skills.Length; ++i)
                        skills[i].Base = 100.0;

                    // now for the stats
                    player.RawDex = 0;
                    player.RawInt = 0;
                    player.RawStr = 0;
                    player.Stam = player.RawDex = 30000;
                    player.Mana = player.RawInt = 30000;
                    player.Hits = player.RawStr = 30000;

                    player.Karma = 30000;
                    player.Fame = 30000;
                    from.SendMessage("Adam built.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        private static void BuildMage(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target player to construct...");
            e.Mobile.Target = new BuildMageTarget(); // Call our target
        }
        private static void NormalizePlayer(Mobile m)
        {
            if (m.Dead)
                m.Resurrect();
            //m.Blessed = false;
            //m.AccessLevel = AccessLevel.Player;
            m.BodyValue = 400;

            if (m.Poison != null)
                m.CurePoison(m);

            if (m is PlayerMobile pm)
                pm.Mortal = false;
        }
        public class BuildMageTarget : Target
        {
            public BuildMageTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile player)
                {
                    NormalizePlayer(player);

                    // first clear all the skills
                    Server.Skills skills = player.Skills;
                    for (int i = 0; i < skills.Length; ++i)
                        skills[i].Base = 0.0;

                    // build us a mage
                    skills[SkillName.Swords].Base = 100.0;
                    skills[SkillName.Anatomy].Base = 100.0;
                    skills[SkillName.Tactics].Base = 100.0;
                    skills[SkillName.Healing].Base = 100.0;
                    skills[SkillName.Magery].Base = 100.0;
                    skills[SkillName.MagicResist].Base = 100.0;
                    skills[SkillName.Meditation].Base = 100.0;

                    // now for the stats
                    player.RawDex = 0;
                    player.RawInt = 0;
                    player.RawStr = 0;
                    player.Stam = player.RawDex = 35;
                    player.Mana = player.RawInt = 100;
                    player.Hits = player.RawStr = 90;

                    from.SendMessage("Mage built.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        private static void BuildWarrior(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target player to construct...");
            e.Mobile.Target = new BuildWarriorTarget(); // Call our target
        }

        public class BuildWarriorTarget : Target
        {
            public BuildWarriorTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile player)
                {
                    NormalizePlayer(player);

                    // first clear all the skills
                    Server.Skills skills = player.Skills;
                    for (int i = 0; i < skills.Length; ++i)
                        skills[i].Base = 0.0;

                    // build us a warrior
                    skills[SkillName.Swords].Base = 100.0;
                    skills[SkillName.Anatomy].Base = 100.0;
                    skills[SkillName.Tactics].Base = 100.0;
                    skills[SkillName.Healing].Base = 100.0;
                    skills[SkillName.Magery].Base = 100.0;
                    skills[SkillName.MagicResist].Base = 100.0;
                    skills[SkillName.Meditation].Base = 100.0;

                    // now for the stats
                    player.RawDex = 0;
                    player.RawInt = 0;
                    player.RawStr = 0;
                    player.Stam = player.RawDex = 100;
                    player.Mana = player.RawInt = 25;
                    player.Hits = player.RawStr = 100;

                    from.SendMessage("Warrior built.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        #endregion Build Player
        #region Make Murderer / Innocent
        private static void MakeMurderer(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target player to make/unmake a murderer...");
            e.Mobile.Target = new MakeMurdererTarget(); // Call our target
        }
        public class MakeMurdererTarget : Target
        {
            public MakeMurdererTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile player)
                {
                    if (player.ShortTermMurders + player.LongTermMurders >= 5)
                    {
                        player.ShortTermMurders = player.LongTermMurders = 0;
                        from.SendMessage("Murder counts cleared.");
                    }
                    else
                    {
                        player.ShortTermMurders = player.LongTermMurders = 5;
                        from.SendMessage("Murder counts set.");
                    }

                    from.SendMessage("Done.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        #endregion Make Murderer / Innocent
        #region Clear Help Stuck Requests
        private static void ClearHelpStuckRequests(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target player to clear...");
            e.Mobile.Target = new PlayerTarget(); // Call our target
        }

        public class PlayerTarget : Target
        {
            public PlayerTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                Mobile followed = from;
                if (target is PlayerMobile player)
                {
                    player.ClearStuckMenu();
                    from.SendMessage("Done.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        #endregion Clear Help Stuck Requests
        #region DeleteNerunSpawners
        private static void DeleteNerunSpawners(CommandEventArgs e)
        {
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
                }
            }
        }
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
        private static bool CanParse(string line)
        {
            string[] lineToks = line.Split(new char[] { '|' });                         // parse on '|'
            if (lineToks.Length == 22)                                                  // always 22 fields
                return true;
            return false;
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
        #endregion
        #region Dungeon To Dungeon Teleporters
        private static void DungeonToDungeonTeleporters(CommandEventArgs e)
        {
            List<Rectangle3D> regionRects = new List<Rectangle3D>();
            foreach (Region rx in Region.Regions)
                if (rx != null && rx.Map == Map.Felucca && rx.IsDungeonRules)
                    foreach (Rectangle3D r in rx.Coords)
                        regionRects.Add(r);

            LogHelper logger = new LogHelper("DungeonToDungeonTeleporters.log", true, true, true);
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            foreach (Item item in World.Items.Values)
            {
                if (item is Teleporter tele && !tele.Deleted)
                {
                    if (tele.Map != Map.Felucca || tele.MapDest != Map.Felucca || !tele.Running) continue;
                    if (tele.PointDest == Point3D.Zero) continue;
                    if (Utility.World.LostLandsWrap.Contains(tele) || Utility.World.LostLandsWrap.Contains(tele.PointDest)) continue;

                    if (IsDungeonToDungeonTeleporter(regionRects, tele))
                    {
                        pm.JumpList.Add(tele);
                        logger.Log(LogType.Item);
                        ;
                    }
                }
            }
            logger.Finish();

            //e.Mobile.SendMessage("{0} items found, {1} deleted, {2} marked as IsIntMapStorage.", items, deleted, intMap);
        }
        private static bool IsDungeonToDungeonTeleporter(List<Rectangle3D> regionRects, Teleporter tele)
        {
            bool teleIsDungeon = false;
            bool destIsDungeon = false;
            Rectangle3D src = new Rectangle3D();
            foreach (Rectangle3D rx in regionRects)
            {
                if (rx.Contains(tele.Location)) { src = rx; teleIsDungeon = true; break; }
            }

            if (teleIsDungeon)
                foreach (Rectangle3D rx in regionRects)
                {
                    if (src.Start != rx.Start || src.End != rx.End)
                        if (rx.Contains(tele.PointDest)) { destIsDungeon = true; break; }
                }

            return teleIsDungeon && destIsDungeon;
        }

        private static bool SameDungeonHack(Region r1, Region r2)
        {   // we can't rely on Uid's being unique at this time.
            if (r1 == null || r2 == null || r1.Name == null || r2.Name == null)
                return false;
            string s1 = r1.Name.ToLower().Trim();
            string s2 = r2.Name.ToLower().Trim();
            string[] t1 = s1.Split(new char[] { ' ', ':' }, StringSplitOptions.TrimEntries);
            string[] t2 = s2.Split(new char[] { ' ', ':' }, StringSplitOptions.TrimEntries);

            return t1[0] == t2[0];
        }
        #endregion Dungeon To Dungeon Teleporters
        #region Analyze Township Items
        private static void AnalyzeTownshipItems(CommandEventArgs e)
        {
            List<TownshipStone> tsstones = new(AllTownshipStones);
            List<ITownshipItem> tsItems = new(TownshipItemHelper.AllTownshipItems);
            List<ITownshipItem> toDelete = new();

            foreach (var tsi in tsItems)
                if (tsi is ITownshipItem && tsi is Item && tsi.Deleted == false)
                {
                    bool orphan = true;
                    foreach (TownshipStone tsStone in tsstones)
                        if (tsStone.Contains((Item)tsi))
                        {
                            orphan = false;
                            break;
                        }
                    if (orphan == true)
                        toDelete.Add(tsi);
                }

            // 11/18/22, Adam
            //  As of today, Angel Island contains no such orphans, so we won't be adding this to the patcher.
            //  It should be noted, that when we patch with AICore 6.26, Townshipd will cleanup all their own
            //  item then the Townstone is deleted (for whatever reason.)
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpList = new ArrayList();
            pm.JumpIndex = 0;
            LogHelper logger = new LogHelper("orphaned township items.log", true, true, true);
            foreach (var tsi in toDelete)
            {
                pm.JumpList.Add(tsi);
                logger.Log(LogType.Item, tsi);
                //tsi.Delete();
                //TownshipItemHelper.AllTownshipItems.Remove(tsi);
            }
            logger.Finish();

            pm.SendMessage("Your jump list has been loaded with {0} items", toDelete.Count);

            // unfortunately, addons were never added to the AllTownshipItems, so we have no way of knowing
            //  what can be deleted and what can't. shit.
#if false
            // cleanup the township addons
            Rectangle3D bounds = GetBounds();
            Rectangle2D rect = new(bounds.Start.X, bounds.Start.Y, bounds.Width, bounds.Height);
            IPooledEnumerable eable = this.Map.GetItemsInBounds(rect);
            List<Item> addons = new List<Item>();
            foreach (Item ix in eable)
                // list of addons to delete
                if (DeleteSafetyCheck(ix, initialRegions) && ix is BaseAddon)
                    addons.Add(ix);

            eable.Free();

            foreach (Item addon in addons)
                addon.Delete();
#endif
        }
        #endregion Analyze Township Items
        #region Orphaned Mobiles
        private static void OrphanedMobiles(CommandEventArgs e)
        {
            int mobiles = 0;
            int creatures = 0;
            int creatures_no_spawner = 0;
            int raw_mobiles = 0;
            int unowned_vendor = 0;
            int homeless_basevendor = 0;
            int unknown_type = 0;
            int deleted = 0;
            int intMap = 0;

            /* World Cleanup
             * in an effort to identify all mobile creatures in the world
             * there are two legal designations, Spawner spawned, and champ spawned
             * player vendors are a special case.
             * anything just added with the [add command will be cleaned up on the next server up
             *  (admins shouldn't be doing this... everything should be on a spawner, eventSpawner if it's an event.)
             *  
             *  NOTE: We still need to handle pets, both spawned and purchased
             *      We also need to handle vendor inventory.
            */

            // first, locate all template mobiles (source templates)
            List<Mobile> templateSourceMobs = new();
            foreach (Item item in World.Items.Values)
                if (item is Spawner spawner && !spawner.Deleted && spawner.TemplateEnabled && spawner.TemplateMobile != null && !spawner.TemplateMobile.Deleted)
                    templateSourceMobs.Add(spawner.TemplateMobile);

            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();

            LogHelper logger = new LogHelper("mobiles catelog.log", true, true, true);
            foreach (Mobile mobile in World.Mobiles.Values)
                if (mobile is not PlayerMobile)
                {
                    mobiles++;
                    if (mobile is BaseCreature bc && bc.Spawner == null)
                    {
                        creatures++;
                        if (mobile is BaseVendor bv && !templateSourceMobs.Contains(mobile))
                        {   // now deleted in patcher.cs: HasDeletedHomelessBaseVendors
                            if (bv != null && !bv.Deleted)
                            {   // homeless base vendor
                                homeless_basevendor++;
                                pm.JumpList.Add(mobile);
                                logger.Log(LogType.Mobile, mobile, string.Format("homeless base vendor. Created {0}", mobile.Created));
                            }
                        }
                        else if (bc.ChampEngine == null)
                        {   // if it has no spawner and it's not spawned by a champ, it looks like a problem
                            if (!templateSourceMobs.Contains(bc as Mobile))
                            {   // if it's NOT a known template, it's a problem
                                creatures_no_spawner++;
                                logger.Log(LogType.Mobile, bc, string.Format("creature not on a spawner or champ spawned. Created {0}", bc.Created));
                            }
                        }
                    }
                    else
                    {   // player vendors fall into this category (basevendors are basecreature)
                        raw_mobiles++;

                        if (mobile is PlayerVendor pv)
                        {
                            if (pv != null && (pv.Owner == null || pv.Owner.Deleted) && !pv.IsStaffOwned)
                            {   // unowned vendor
                                unowned_vendor++;
                                logger.Log(LogType.Mobile, mobile, string.Format("unowned vendor. Created {0}", mobile.Created));
                            }
                        }
                        else
                        {
                            // not sure we have an raw mobiles on a champ spawner, but just to be sure
                            if (mobile.ChampEngine == null)
                            {   // unknown type
                                unknown_type++;
                                logger.Log(LogType.Mobile, mobile, string.Format("unknown type. Created {0}", mobile.Created));
                            }
                        }
                    }

                    deleted++;
                }
            logger.Finish();

            e.Mobile.SendMessage("{0} all mobiles found", mobiles);
            e.Mobile.SendMessage("{0} creatures found", creatures);
            e.Mobile.SendMessage("{0} creatures not on a spawner or champ spawned", creatures_no_spawner);
            e.Mobile.SendMessage("{0} raw mobiles found ", raw_mobiles);
            e.Mobile.SendMessage("{0} unowned vendor", unowned_vendor);
            e.Mobile.SendMessage("{0} homeless base vendor", homeless_basevendor);
            e.Mobile.SendMessage("{0} unknown type", unknown_type);

            if (pm.JumpList.Count > 0)
                e.Mobile.SendMessage("{0} homeless base vendors added to your jumplist", pm.JumpList.Count);

            //e.Mobile.SendMessage("{0} items found, {1} deleted, {2} marked as IsIntMapStorage.", items, deleted, intMap);
        }
        #endregion Orphaned Mobiles
        #region Int Map Items
        private static void IntMapItems(CommandEventArgs e)
        {
            int items = 0;
            int deleted = 0;
            int intMap = 0;
            foreach (Item item in World.Items.Values)
                if (item is Fists && item.Map == Map.Internal && item.Parent == null)
                {
                    items++;
                    if (item.Deleted)
                        deleted++;
                    if (item.IsIntMapStorage)
                        intMap++;
                }
            e.Mobile.SendMessage("{0} items found, {1} deleted, {2} marked as IsIntMapStorage.", items, deleted, intMap);
        }
        #endregion Int Map Items
        #region Count Fists
        private static void CountFists(CommandEventArgs e)
        {
            int fists = 0;
            int deleted = 0;
            int intMap = 0;
            foreach (Item item in World.Items.Values)
                if (item is Fists && item.Map == Map.Internal && item.Parent == null)
                {
                    fists++;
                    if (item.Deleted)
                        deleted++;
                    if (item.IsIntMapStorage)
                        intMap++;
                }
            e.Mobile.SendMessage("{0} fists found, {1} deleted, {2} marked as IsIntMapStorage.", fists, deleted, intMap);
        }
        #endregion Count Fists
        #region Count ICommodity
        private static void CountICommodity(CommandEventArgs e)
        {
            int commodities = 0;
            int notCommodity = 0;
            int deleted = 0;
            int intMap = 0;
            foreach (Item item in World.Items.Values)
                if (item is ICommodity && item.Map == Map.Internal && item.Parent == null)
                {
                    commodities++;
                    if (item.Deleted)
                        deleted++;
                    if (item.IsIntMapStorage)
                        intMap++;

                    e.Mobile.SendMessage("{0}. created {1}.", item, item.Created);
                }
                else
                    notCommodity++;
            e.Mobile.SendMessage("{0} commodities found, {1} deleted, {2} marked as IsIntMapStorage.", commodities, deleted, intMap);
            e.Mobile.SendMessage("{0} non-commodities found.", notCommodity);
        }
        #endregion Count ICommodity
        #region Initialize Vendor Inventories
        private static void InitializeVendorInventories(CommandEventArgs e)
        {
            Dictionary<string, List<Point3D>> inventoryTable = new Dictionary<string, List<Point3D>>(StringComparer.OrdinalIgnoreCase);
            e.Mobile.SendMessage("Generating Inventory table, please wait...");

            Dictionary<SBInfo, Point3D> list = new Dictionary<SBInfo, Point3D>();
            foreach (Mobile mob in World.Mobiles.Values)
            {
                if (mob == null || mob is PlayerVendor || mob is RentedVendor)
                    continue;

                if (mob.Map != Map.Felucca || mob.Title == null)
                    continue;

                if (mob is BaseVendor vendor)
                {
                    ArrayList infos = vendor.Inventory;
                    if (infos != null)
                        foreach (SBInfo info in infos)
                            if (info == null || info.BuyInfo == null || info.BuyInfo.GetType() == null)
                                continue;
                            else
                                list.Add(info, mob.Location);
                }
            }

            foreach (KeyValuePair<SBInfo, Point3D> info in list)
                foreach (GenericBuyInfo binfo in info.Key.BuyInfo)
                {   // exclude BBS items (price==0)

                    if (binfo.Type == null)
                    {   // check and make sure you are not adding something as a pooled resource without pooled resource being enabled.
                        //  this will cause an 'empty' entry in the list 'info'
                        e.Mobile.SendMessage(string.Format("binfo.Type == null: {0}. ", Utility.FileInfo()), ConsoleColor.Red);
                        continue;
                    }

                    if (ResourcePool.IsPooledResource(binfo.Type, true) && binfo.Price == 0)
                        continue;

                    string name = Utility.GetObjectDisplayName(binfo.GetDisplayObject(), binfo.Type);

                    if (inventoryTable.ContainsKey(name))
                        inventoryTable[name].Add(info.Value);
                    else
                        inventoryTable.Add(name, new List<Point3D> { info.Value });
                }

            e.Mobile.SendMessage("Inventory table generation complete with {0} items registered.", inventoryTable.Count);
        }
        #endregion Initialize Vendor Inventories
        #region DisplayCache
        private static void DisplayCache(CommandEventArgs e)
        {
            e.Mobile.SendMessage("There are {0} items in the display cache.", GenericBuyInfo.Diagnostics.Items.Count);
            e.Mobile.SendMessage("There are {0} mobiles in the display cache.", GenericBuyInfo.Diagnostics.Mobiles.Count);
            int itemsDeleted = 0;
            int itemsOnIntMap = 0;
            int mobilesDeleted = 0;
            int mobilesOnIntMap = 0;
            foreach (Item item in GenericBuyInfo.Diagnostics.Items)
            {
                if (item.Deleted)
                    itemsDeleted++;
                if (item.IsIntMapStorage)
                    itemsOnIntMap++;
            }
            foreach (Mobile mob in GenericBuyInfo.Diagnostics.Mobiles)
            {
                if (mob.Deleted)
                    mobilesDeleted++;
                if (mob.IsIntMapStorage)
                    mobilesOnIntMap++;
            }
            e.Mobile.SendMessage("{0} items are deleted, {1} are IsIntMapStorage.", itemsDeleted, itemsOnIntMap);
            e.Mobile.SendMessage("{0} mobiles are deleted, {1} are IsIntMapStorage.", mobilesDeleted, mobilesOnIntMap);
            int itemsAtDogtag = 0;
            int mobilesAtDogtag = 0;
            Point3D dogtag = Utility.Dogtag.Get("Server.Mobiles.GenericBuyInfo+DisplayCache");
            foreach (Item item in World.Items.Values)
            {
                if (item is Item && !item.Deleted && item.Map == Map.Internal)
                    if (item.Location == dogtag)
                        itemsAtDogtag++;
            }
            foreach (Mobile mob in World.Mobiles.Values)
            {
                if (mob is Mobile && !mob.Deleted && mob.Map == Map.Internal)
                    if (mob.Location == dogtag)
                        mobilesAtDogtag++;
            }
            e.Mobile.SendMessage("{0} items at dogtag {1}.", itemsAtDogtag, dogtag);
            e.Mobile.SendMessage("{0} mobiles at dogtag {1}.", mobilesAtDogtag, dogtag);

            e.Mobile.SendMessage("item display cache discrepancy: {0}.", itemsAtDogtag - itemsOnIntMap);
            e.Mobile.SendMessage("mobile display cache discrepancy: {0}.", mobilesAtDogtag - mobilesOnIntMap);
            e.Mobile.SendMessage("done.");
        }
        #endregion DisplayCache
        #region Dogtag
        private static void Dogtag(CommandEventArgs e)
        {
            Utility.Dogtag.Test();
            e.Mobile.SendMessage("done.");
        }
        #endregion Dogtag
        #region GenPriceDB
        private static void GenPriceDB(CommandEventArgs e)
        {
            seen_it.Clear();

            foreach (Mobile m in World.Mobiles.Values)
                if (m is BaseVendor bv)
                {
                    List<SBInfo> list = new List<SBInfo>(bv.Inventory.Cast<SBInfo>().ToList());
                    foreach (var sbinfo in list)
                    {
                        foreach (var buy in sbinfo.BuyInfo)
                        {
                            if (buy is PresetMapBuyInfo)
                            {
                                GenPriceLog(typeof(Server.Items.PresetMap).FullName, "buy", (buy as GenericBuyInfo).Price);
                            }
                            else
                            {
                                if ((buy as GenericBuyInfo).Type == null)
                                    ; // break point 

                                GenPriceLog((buy as GenericBuyInfo).Type.FullName, "buy", (buy as GenericBuyInfo).Price);
                            }
                        }

                        foreach (DictionaryEntry sell in (sbinfo.SellInfo as GenericSellInfo).Table)
                        {
                            if ((int)sell.Value == -1)
                                continue;

                            GenPriceLog((sell.Key as Type).FullName, "sell", (int)sell.Value);
                        }
                    }
                }

            e.Mobile.SendMessage("Price database generated with {0} entries", seen_it.Count());
            seen_it.Clear();
        }
        private static List<string> seen_it = new List<string>();
        private static void GenPriceLog(string name, string buy_sell, int price)
        {
            // 11/1/22, Adam: record all our pricing info.
            //  A similad database exists in \Data and was a capture of bot hAI's and RunUO's items and mobiles for buy/sell.
            string filename = "vendor pricing.txt";
            string output = String.Format("{0}: {1}: {2}", name, buy_sell, price);
            if (!seen_it.Contains(output))
            {
                System.IO.File.AppendAllLines(filename, new string[] { output });
                seen_it.Add(output);
            }
        }
        #endregion GenPriceDB
        #region PurifyPricingDictionary
        /// <summary>
        /// PurifyPricingDictionary cleanup and formatting algo for our new
        /// \Data\"vendor pricing.txt"
        /// "vendor pricing.txt" contains all the items and mobiles and their
        /// Buy/Sell price.
        /// We prefer RUNUO 2.6's entries since I believed their's we likely
        /// closer to truth. (dozens of engineers, contributing over as many years.)
        /// more on this later.
        /// Purpose: we no longer allow willy nilly pricing. Even pricing when the 
        /// developer or world builder is trying to do the right thing, it's too easy to 
        /// get it wrong (swapping buy/sell prices, simple typo, hunches, etc.)
        /// This database is loaded at run tome, and called each time a vendor's inventory
        /// is loaded to define the appropriate buy/sell price.
        /// See also: BaseVendor.StandardPricingDictionary where this database is used.
        /// Problems solved an issues encountered:
        /// RunUO 2.6 contained the following Buy/Sell prices for BlankScroll
        /// Server.Items.BlankScroll: sell: 3
        ///         Server.Items.BlankScroll: sell: 2
        ///         Server.Items.BlankScroll: sell: 6
        ///         Server.Items.BlankScroll: buy: 12
        ///         Server.Items.BlankScroll: buy: 5
        /// Not only are these prices inconsistent, but exploitive when you can buy for 5
        /// and sell for 6.
        /// Another example, and more dramatic is RunUO's pricing of SpellBooks
        ///         Server.Items.Spellbook: buy: 18
        ///         Server.Items.Spellbook: sell: 25
        ///         Server.Items.Spellbook: sell: 9
        /// Here you can make 7gp just by buying/selling these things.
        /// There were other inconsistencies as well.
        /// In the following algo we:
        /// 1. filter out duplicates
        /// 2. ensure buy price and sell price are > 0
        /// 3. ensure buy price is > sell price
        /// Note: while the resulting list is consistent, it doesn't guarantee
        /// that the resulting prices are the 'era accurate' price. That's a separate 
        /// research task. 
        /// See the SB* files for the vendors to see this pricing in action.
        /// </summary>
        /// <outout cleaned="vendor pricing.txt"></outout>
        private static void PurifyPricingDictionary(CommandEventArgs e)
        {

            string pathName = Path.Combine(Core.DataDirectory, "vendor pricing.txt");
            List<string> raw_list = new(File.ReadAllLines(pathName));
            List<string> clean_list = new List<string>();
            // Eliminate duplicates
            foreach (string line in raw_list)
            {
                string[] toks = line.Split(new char[] { ':', ' ' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                string substring = toks[0] + ": " + toks[1];
                bool dupe = false;
                foreach (string sx in clean_list)
                {
                    if (sx.Contains(substring))
                    {
                        dupe = true;
                        break;
                    }
                }
                if (dupe == true) continue;

                int price = Convert.ToInt32(toks[2]);
                if (price <= 0) continue;
                clean_list.Add(line);
            }

            // write the fresh copy
            clean_list.Sort();
            File.WriteAllLines(pathName, clean_list);

            // reload (we use the database for our next filtering pass.)
            BaseVendor.StandardPricingDictionary.Clear();
            BaseVendor.Load();

            // eliminate bad pricing where buy or sell < 0 or
            //  sell >= buy
            foreach (var kvp in BaseVendor.StandardPricingDictionary)
            {
                if (kvp.Value.Count == 2)
                {   // we have both a buy and sell price
                    int buyPrice = 0;
                    int sellPrice = 0;

                    if (kvp.Value[0].Key == StandardPricingType.Buy)
                    {
                        buyPrice = kvp.Value[0].Value;
                        sellPrice = kvp.Value[1].Value;
                    }
                    else
                    {
                        buyPrice = kvp.Value[1].Value;
                        sellPrice = kvp.Value[0].Value;
                    }

                    if (sellPrice <= 0 || buyPrice <= 0)
                        Utility.ConsoleWriteLine("(sellPrice <= 0 || buyPrice <= 0)", ConsoleColor.Red);
                    if (buyPrice <= sellPrice)
                        Utility.ConsoleWriteLine("(buyPrice <= sellPrice)", ConsoleColor.Red);

                    continue;
                }
            }

            e.Mobile.SendMessage("Done writing {0} entries.", clean_list.Count);
        }
        #endregion PurifyPricingDictionary
        #region Conformity filter
        private static void ConformityFilter(CommandEventArgs e)
        {
            /* Conformity filter
             * This tool may be run once in a while to determine if we are buying/selling
             * the wrong creatures/items on the wrong shard.
             * I.e., as we prepare the launch of Renascence, we needed to exclude all the Angel Island special
             *  creatures and items. Usually we were pretty good about filtering, but many slipped through.
             *  How it works: We've captured all the creatures and items known to RunUO 2.6 and use that as the standard.
             *  (Data\StandardObjects.xml). Any thing sold on a vendor that is not on the 'standards list' gets flagged
             *  and fixed.
             */
            int conformityFailures = 0;
            foreach (Mobile mx in World.Mobiles.Values)
            {
                if (mx is BaseVendor bv)
                {
                    ArrayList SBInfos = bv.Inventory;
                    if (SBInfos == null) continue;

                    XmlDocument doc = Utility.OpenStandardObjects();
                    if (doc != null)
                        for (int i = 0; i < SBInfos.Count; i++)
                        {
                            SBInfo sbInfo = (SBInfo)SBInfos[i];
                            if (sbInfo.BuyInfo != null)
                                foreach (IBuyItemInfo bii in sbInfo.BuyInfo)
                                {
                                    List<string> aliases = new();
                                    if (Core.RuleSets.StandardShardRules())
                                        if (typeof(Mobile).IsAssignableFrom(bii.Type))
                                        {
                                            if (Utility.StandardCreature(doc, bii.Type.FullName) == false)
                                            {
                                                string output = string.Format("Error: {0}: {1} {2} buying nonstandard item {3}",
                                                    GetSBClass(sbInfo.SellInfo),
                                                    bv.Name != null ? bv.Name : "no name",
                                                    bv.Title != null ? bv.Title : "no title",
                                                    bii.Type.Name);
                                                Utility.ConsoleWriteLine(output, ConsoleColor.Red);
                                                LogAberrations(output);
                                                conformityFailures++;
                                            }
                                        }
                                        else if (!Core.RuleSets.ShardAllowsItemRules(bii.Type))
                                            if (Utility.StandardItem(doc, bii.Type, aliases) == false)
                                            {
                                                string output = string.Format("Error: {0}: {1} {2} buying nonstandard item {3}",
                                                    GetSBClass(sbInfo.SellInfo),
                                                    bv.Name != null ? bv.Name : "no name",
                                                    bv.Title != null ? bv.Title : "no title",
                                                    bii.Type.Name);
                                                Utility.ConsoleWriteLine(output, ConsoleColor.Red);
                                                LogAberrations(output);
                                                conformityFailures++;
                                            }
                                }

                            if (sbInfo.SellInfo != null)
                                foreach (Type type in sbInfo.SellInfo.Types)
                                {
                                    List<string> aliases = new();
                                    if (Core.RuleSets.StandardShardRules())
                                        if (typeof(Mobile).IsAssignableFrom(type))
                                        {
                                            if (Utility.StandardCreature(doc, type.FullName) == false)
                                            {
                                                string output = string.Format("Error: {0}: {1} {2} buying nonstandard item {3}",
                                                    GetSBClass(sbInfo.SellInfo),
                                                    bv.Name != null ? bv.Name : "no name",
                                                    bv.Title != null ? bv.Title : "no title",
                                                    type.Name);
                                                Utility.ConsoleWriteLine(output, ConsoleColor.Red);
                                                LogAberrations(output);
                                                conformityFailures++;
                                            }
                                        }
                                        else if (!Core.RuleSets.ShardAllowsItemRules(type))
                                            if (Utility.StandardItem(doc, type, aliases) == false)
                                            {
                                                string output = string.Format("Error: {0}: {1} {2} selling nonstandard item {3}",
                                                    GetSBClass(sbInfo.SellInfo),
                                                    bv.Name != null ? bv.Name : "no name",
                                                    bv.Title != null ? bv.Title : "no title",
                                                    type.Name);
                                                Utility.ConsoleWriteLine(output, ConsoleColor.Red);
                                                LogAberrations(output);
                                                conformityFailures++;
                                            }
                                }
                        }
                }
            }
            Utility.ConsoleWriteLine("LoadSBInfo total failures {0}", ConsoleColor.Red, conformityFailures);
        }
        private static void LogAberrations(string text)
        {
            LogHelper logger = new LogHelper("Illegal Items For Sale.log", false, true, true);
            logger.Log(LogType.Text, text);
            logger.Finish();
        }
        private static string GetSBClass(IShopSellInfo ssi)
        {
            if (ssi == null) return "?";
            string[] toks = ssi.ToString().Split(new char[] { '.', '+' });
            if (toks.Length != 4) return "?";
            return toks[2];
        }
        #endregion Conformity filter
        #region AnalyzeTemplateMobiles
        private static void AnalyzeTemplateMobiles(CommandEventArgs e)
        {
            int match = 0;
            int nomatch = 0;
            int spawnerTempMob = 0;
            int totalOnInternal = 0;
            int totalOnNull = 0;
            int spawnerProductYes = 0;
            int spawnerProductNo = 0;
            int template = 0;
            int toDelete = 0;
            int toConsider = 0;
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m == null || m.Deleted) continue;
                if (m.Map == null) totalOnNull++;
                if (m.Map == Map.Internal) totalOnInternal++;
                if (!(m.Map == null || m.Map == Map.Internal)) continue;
                if (m is BaseCreature bc)
                {
                    // look for orphans
                    {
                        bool isSpawnerProduct = false;
                        bool isTemplate = false;

                        if (SpawnerProduct(m))
                        {
                            spawnerProductYes++;
                            isSpawnerProduct = true;
                        }
                        else
                        {
                            spawnerProductNo++;
                            isSpawnerProduct = false;
                        }

                        if (IsTemplate(m))
                        {
                            template++;
                            isTemplate = true;
                        }
                        else
                            isTemplate = false;

                        if (!isTemplate && !isSpawnerProduct)
                            toDelete++;

                        if (!isTemplate && isSpawnerProduct)
                            toConsider++;
                    }
                    if (bc.Spawner == null) continue;
                    if (bc.Spawner.TemplateMobile == null) continue;
                    if (bc.Serial == bc.Spawner.TemplateMobile.Serial)
                        match++;
                    else
                        nomatch++;
                    if (bc.SpawnerTempMob)
                        spawnerTempMob++;
                }
            }
            // m_TemplateMobile.SpawnerTempMob = true;     // reset the new
            return;
        }
        private static bool SpawnerProduct(Mobile m)
        {
            foreach (Item item in World.Items.Values)
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
        private static bool IsTemplate(Mobile m)
        {
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted) continue;
                if (item is Spawner spawner)
                    if (spawner.TemplateMobile == m)
                        return true;
            }

            return false;
        }
        #endregion AnalyzeTemplateMobiles
        #region Calc ZSlop
        private static void CalcZSlop(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            Rectangle2D rect = new Rectangle2D(10, 10, from.Location);
            int hf = Utility.HardFloor(from.Map, from.X, from.Y, from.Z);
            int hc = Utility.HardCeiling(from.Map, from.X, from.Y, from.Z);
            int zs = Utility.CalcZSlop(from.Map, rect, from.Location.Z, WipeRectFlags.Items);
            from.SendMessage("floor {0} ceiling {1} zSlop {2}\ndone.", hf, hc, zs);
        }
        #endregion Calc ZSlop
        #region Refresh Deco
        private static void RefreshDeco(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            Rectangle2D rect = new Rectangle2D(10, 10, from.Location);
            Utility.RefreshDeco(from.Map, new List<Rectangle2D> { rect },
                from.Z, lenientZ: int.MinValue);
            int hf = Utility.HardFloor(from.Map, from.X, from.Y, from.Z);
            int hc = Utility.HardCeiling(from.Map, from.X, from.Y, from.Z);
            int zs = Utility.CalcZSlop(from.Map, rect, from.Location.Z);
            Server.Gumps.EditAreaGump.FlashArea(from, rect, from.Map);
            from.SendMessage("floor {0} ceiling {1} zSlop {2}\ndone.", hf, hc, zs);
        }
        #endregion Refresh Deco
        #region Find Ransom Chests
        private static void FindRansomChests(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            PlayerMobile pm = from as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            string filename = "KinRansomChests.log";

            // find the ransom chests
            List<KinRansomChest> list = new();
            foreach (Item ix in World.Items.Values)
                if (ix is KinRansomChest krc && !krc.Deleted)
                    list.Add(krc);
            LogHelper logger = new LogHelper(filename, false, true, true);
            foreach (KinRansomChest krc in list)
            {
                logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", krc.Serial.Value, krc.X, krc.Y, krc.Z, krc.Map));
                pm.JumpList.Add(krc.GetWorldLocation());
            }
            logger.Finish();

            from.SendMessage("Done.");
            from.SendMessage("Your jump list now contains {0} entries.", pm.JumpList.Count);

            KinRansomChestWipe(Map.Felucca, list);
        }
        private static void KinRansomChestWipe(Map map, List<KinRansomChest> chests)
        {
            Type[] exclude = new Type[] { typeof(Spawner), typeof(Teleporter), typeof(AddonComponent), typeof(BaseDoor) };
            foreach (object cx in chests)
                if (cx is KinRansomChest chest)
                {
                    // 4x4x5 .. this captures all the platforms on which the chests sit. 
                    //  4x4 width and height, with a Z sweep if +- 5
                    Rectangle2D rect = new Rectangle2D(4, 4, new Point2D(chest.X, chest.Y));
                    Utility.RefreshDeco(map, new List<Rectangle2D>() { rect }, chest.Z,
                        typeExclude: exclude, lenientZ: int.MinValue/*5*/);
                }
        }
        #endregion Find Ransom Chests
        #region lost lands access teleporters
        private static void LostLandsAccessTeleporters(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            PlayerMobile pm = from as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            string filename = "LostLandsAccessTeleporters.log";
            foreach (Item item in World.Items.Values)
                if (item is Teleporter teleporter && !teleporter.Deleted && teleporter.Running)
                    if (FromBrit(teleporter) && ToLostLands(teleporter))
                    {
                        LogHelper logger = new LogHelper(filename, true, true, true);
                        logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", teleporter.Serial.Value, teleporter.X, teleporter.Y, teleporter.Z, teleporter.Map));
                        logger.Finish();
                        pm.JumpList.Add(teleporter);
                    }

            from.SendMessage("Done.");
            from.SendMessage("Your jump list now contains {0} entries.", pm.JumpList.Count);
        }
        #endregion
        #region Visit
        private static void Visit(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (e.Length < 2)
            {
                e.Mobile.SendMessage("Usage: Visit <config file> (no extension)");
                return;
            }
            string filename = e.ArgString.Substring(e.ArgString.IndexOf(' '));

            // read the list of interesting items
            string pathName = Path.Combine(Core.DataDirectory, "Patches", filename.Trim() + ".cfg");
            if (!File.Exists(pathName))
                pathName = Path.Combine(Core.LogsDirectory, filename.Trim() + ".log");
            if (File.Exists(pathName))
            {
                List<Tuple<int, int, int, int, Map>> records = new();
                foreach (string line in File.ReadAllLines(pathName))
                {
                    string[] toks = line.Split(' ');
                    records.Add(new Tuple<int, int, int, int, Map>(int.Parse(toks[0]), int.Parse(toks[1]), int.Parse(toks[2]), int.Parse(toks[3]), Map.Parse(toks[4])));
                }

                // count all null items from the list
                int isnull =
                    records.Count(item => World.FindItem(item.Item1) == null);

                // count all deleted items from the list
                int deleted =
                records.Count(item => World.FindItem(item.Item1) != null && World.FindItem(item.Item1).Deleted);

                // log the items
                (e.Mobile as PlayerMobile).JumpIndex = 0;
                (e.Mobile as PlayerMobile).JumpList = new System.Collections.ArrayList();
                foreach (var record in records)
                    (e.Mobile as PlayerMobile).JumpList.Add(new WorldLocation(record.Item2, record.Item3, record.Item4, record.Item5));

                if (deleted > 0)
                    e.Mobile.SendMessage("Info: {0} deleted items detected.", deleted);
                if (isnull > 0)
                    e.Mobile.SendMessage("Info: {0} null items deleted.", isnull);

                e.Mobile.SendMessage("Your JumpList has been loaded with {0} locations.", (e.Mobile as PlayerMobile).JumpList.Count);
            }
            else
                e.Mobile.SendMessage("Cannot find {0}", pathName);
        }
        #endregion Visit
        #region CopyInfo
        private static void CopyInfo(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the item you wish to copy.");
            e.Mobile.Target = new CopyInfoTarget(null);
        }
        public class CopyInfoTarget : Target
        {
            public CopyInfoTarget(object o)
                : base(12, true, TargetFlags.None)
            {
            }
            protected override void OnTarget(Mobile from, object targeted)
            {

                Item it = targeted as Item;
                if (it != null)
                {
                    string text = string.Format("{0} {1} {2} {3} {4}", it.Serial.Value, it.X, it.Y, it.Z, it.Map);
                    TextCopy.ClipboardService.SetText(text);
                    from.SendMessage("{0}", text);
                    from.SendMessage("Was copied to your clipboard.");
                }
                else
                    from.SendMessage("That is not an item.");
            }
        }
        #endregion CopyInfo
        #region StretchRect
        [Usage("StretchRect")]
        [Description("StretchRect")]
        private static void StretchRect(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the location you wish to check.");
            e.Mobile.Target = new StretchRectTarget(null);
        }
        public class StretchRectTarget : Target
        {
            public StretchRectTarget(object o)
                : base(12, true, TargetFlags.None)
            {

            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;

                int landZ = 0, landAvg = 0, landTop = 0;
                from.Map.GetAverageZ(loc.X, loc.Y, ref landZ, ref landAvg, ref landTop);
                if (loc != Point3D.Zero)
                {
                    Rectangle2D rect = new Rectangle2D(loc, new Point2D(loc.X + 1, loc.Y + 1));
                    rect = new SpawnableRect(rect).MaximumSpawnableRect(from.Map, 128, from.Map.GetAverageZ(rect.X, rect.Y));
                    from.SendMessage("Your max spawnable rect is {0}.", rect);
                    from.SendMessage("({0} {1}, {2}, {3})", rect.Start.X, rect.Start.Y, rect.End.X, rect.End.Y);
                    Server.Gumps.EditAreaGump.FlashArea(from, rect, from.Map);
                }
                else
                    from.SendMessage("Can't grow a rect here.");
            }
        }
        #endregion StretchRect
        #region HasPatchedISFeluccaTeleporters
        // exception: teleporter 0x400446E1 || 0x40023C72 needs a 5x9 wipe
        // 0x40004555 green acres tele added after [preserve
        // 0x40055B9C oc'nivelle. Too much custom deco. item range of zero
        // 0x400D7B02, 0x400D7B99 event tele - exclude all event teles (somehow these got added.)
        private static void HasPatchedISFeluccaTeleporters(CommandEventArgs e)
        {
            Map map = Map.Felucca;
            string toDeleteFile = "Angel Island teleporters.cfg";
            string toDeletepathName = Path.Combine(Core.DataDirectory, "Patches", toDeleteFile);
            List<Item> toDeleteList = new();
            foreach (string line in File.ReadAllLines(toDeletepathName))
            {
                string[] toks = line.Split(' ');
                //-------------------------------------------- Serial Number ------------------- X ---------------- Y ----------------- Z ----------------- Map -----------
                Tuple<int, int, int, int, Map> record = new(int.Parse(toks[0]), int.Parse(toks[1]), int.Parse(toks[2]), int.Parse(toks[3]), Map.Parse(toks[4]));
                Serial serial = record.Item1;
                Item item = World.FindItem(serial);
                if (item != null && item.Deleted == false && item.Map != null && item.Map != Map.Internal)
                    toDeleteList.Add(item);
                else
                    ;
            }

            // next, read the list of items which we wish to preserve on Siege
            // PreserveTeleporterIS.cfg
            string preserveFile = "PreserveTeleporterIS.cfg";
            string preservepathName = Path.Combine(Core.DataDirectory, "Patches", preserveFile);
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
                    else
                        ;
                }
            }

            // next, read the list of event teles which we wish to preserve on Siege
            // EventTeleporters.cfg
            string eventFile = "EventTeleporters.cfg";
            string eventpathName = Path.Combine(Core.DataDirectory, "Patches", eventFile);
            if (File.Exists(eventpathName))
            {
                foreach (String line in File.ReadAllLines(eventpathName))
                {
                    string[] toks = line.Split(' ');
                    Serial serial = int.Parse(toks[0]);
                    Item item = World.FindItem(serial);
                    if (item != null && item.Deleted == false && item.Map != null && item.Map != Map.Internal)
                        preserveLlist.Add(item);
                    else
                        ;
                }
            }

            // what's the delta?
            var delta = toDeleteList.Except(preserveLlist).ToList();

            // only unique values
            delta = delta.Distinct().ToList();


            // sort the list on distance to me
            delta.Sort((e1, e2) =>
            {
                return e2.GetDistanceToSqrt(e.Mobile).CompareTo(e1.GetDistanceToSqrt(e.Mobile));
            });

            // delta list now contains the things we want to deleted from Siege
            LogHelper review = new LogHelper("Teleporters to deleted from shard.log", true, true, true);
            foreach (Item item in delta)
                review.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", item.Serial, item.X, item.Y, item.Z, item.Map));
            review.Finish();

            // we can't load the jumplist with the items as usual, since when we get there it will have been deleted.
            //  we'll load our jumplist with Point3D+map instead.
            List<WorldLocation> jumplist = new();
            foreach (Item item in delta)
                jumplist.Add(item.GetWorldLocation());

            // lets be careful
            AutoSave.SavesEnabled = false;

            // lets get cracking!
            foreach (Item item in delta)
            {
                if (item != null && item.Deleted == false && item.Map != null && item.Map != Map.Internal)
                {
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

                    List<Item> neighborList = new();
                    UpdateWorldUnit(map, e, neighborList, rect, delta, item);
                }
            }

            (e.Mobile as PlayerMobile).JumpIndex = 0;
            (e.Mobile as PlayerMobile).JumpList = new ArrayList(jumplist);
            e.Mobile.SendMessage("Your jump list now contains {0} entries.", (e.Mobile as PlayerMobile).JumpList.Count);

        }
        private static void UpdateWorldUnit(Map map, CommandEventArgs e, List<Item> neighborList, Rectangle2D rect, List<Item> delta, Item item)
        {
#if false
            // recurse
            // If the rect we are about delete contains another teleporter to delete, take care that one first
            foreach (Item neighbor in delta)
                if (item != null && item.Deleted == false && neighbor != null && neighbor.Deleted == false && neighbor != item)
                    if (item.Map == neighbor.Map && item is Teleporter)
                        if (rect.Contains(neighbor.Location))
                            if (!neighborList.Contains(neighbor))
                            {
                                neighborList.Add(neighbor);
                                UpdateWorldUnit(e, neighborList, rect, delta, neighbor);
                            }
#endif
            // ===
            // special types and ItemIDs to protect
            // ===
            List<Type> types = new();
            List<int> ids = new();
            // cages near a teleporter
            Item si1 = World.FindItem(0x40025A85);
            Item si2 = World.FindItem(0x40025B7C);
            Item si3 = World.FindItem(0x40025C3D);
            if (si1 != null) ids.Add(si1.ItemID);
            if (si2 != null) ids.Add(si2.ItemID);
            if (si3 != null) ids.Add(si3.ItemID);
            // other types to ignore
            types.Add(typeof(Spawner));
            types.Add(typeof(BaseDoor));
            types.Add(typeof(Moongate));

            // TEST
            int startingZ = item.Z;
            types.Add(typeof(Teleporter));
            item.Delete();

            // do the patch
            Utility.RefreshDeco(map, new List<Rectangle2D>() { rect }, startingZ: startingZ,
                typeExclude: types.ToArray(), idExclude: ids.ToArray(), lenientZ: int.MinValue /*5*/);
        }
        private static void ReviewISFeluccaTeleporters(CommandEventArgs e)
        {
            string analysisFile = "Angel Island teleporters.cfg";
            string analysispathName = Path.Combine(Core.DataDirectory, "Patches", analysisFile);
            List<Item> analysisList = new();
            foreach (string line in File.ReadAllLines(analysispathName))
            {
                string[] toks = line.Split(' ');
                //--------------------------------------------- Serial Number ---------------- X ---------------- Y ----------------- Z ----------------- Map -----------
                Tuple<int, int, int, int, Map> record = new(int.Parse(toks[0]), int.Parse(toks[1]), int.Parse(toks[2]), int.Parse(toks[3]), Map.Parse(toks[4]));
                Serial serial = record.Item1;
                Item item = World.FindItem(serial);
                if (item != null)
                    analysisList.Add(item);
                else
                    ;
            }

            // next, read the list of items which we wish to preserve on Siege
            // PreserveTeleporterIS.cfg
            string preserveFile = "PreserveTeleporterIS.cfg";
            string preservepathName = Path.Combine(Core.DataDirectory, "Patches", preserveFile);
            List<Item> preserveLlist = new();
            if (File.Exists(preservepathName))
            {
                foreach (String line in File.ReadAllLines(preservepathName))
                {
                    string[] toks = line.Split(' ');
                    Serial serial = int.Parse(toks[0]);
                    Item item = World.FindItem(serial);
                    if (item != null)
                        preserveLlist.Add(item);
                    else
                        ;
                }
            }

            // what's the delta?
            var delta = analysisList.Except(preserveLlist).ToList();

            // only unique values
            delta = delta.Distinct().ToList();

            // sort the list on distance to me
            delta.Sort((e1, e2) =>
            {
                return e2.GetDistanceToSqrt(e.Mobile).CompareTo(e1.GetDistanceToSqrt(e.Mobile));
            });

            // delta list now contains the things we want to deleted from Siege
            LogHelper review = new LogHelper("Teleporters to delete from Siege.log", true, true, true);
            foreach (Item item in delta)
                review.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", item.Serial, item.X, item.Y, item.Z, item.Map));
            review.Finish();

            (e.Mobile as PlayerMobile).JumpIndex = 0;
            (e.Mobile as PlayerMobile).JumpList = new ArrayList(delta);

            e.Mobile.SendMessage("Your jump list now contains {0} entries.", (e.Mobile as PlayerMobile).JumpList.Count);
        }
        #endregion HasPatchedISFeluccaTeleporters
        #region RemoveOcCastle
        private static void RemoveOcCastle(CommandEventArgs e)
        {
            CustomRegionControl crc = (CustomRegionControl)World.FindItem(0x4004A942);
            if (crc != null)
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
            }
        }
        #endregion RemoveOcCastle
        #region Item Tools
        [Usage("LegalTrammelTeleporter")]
        [Description("Logs the targeted teleporter as being a Legal Trammel Teleporter (not to delete).")]
        private static void LegalTrammelTeleporter(CommandEventArgs e)
        {
            if (e.Mobile.Map != Map.Trammel)
            {
                e.Mobile.SendMessage("You must be in Trammel to use this command.");
                return;
            }
            if (ItemAtPoint(e.Mobile, typeof(Teleporter)) != null)
            {
                new LegalTrammelTeleporterTarget(null).DoLogAsLegalTeleporter(e.Mobile, ItemAtPoint(e.Mobile, typeof(Teleporter)));
            }
            else
            {
                e.Mobile.SendMessage("Target the teleporter you wish to log as 'Legal'.");
                e.Mobile.Target = new LegalTrammelTeleporterTarget(null);
            }
        }
        public class LegalTrammelTeleporterTarget : Target
        {
            public LegalTrammelTeleporterTarget(object o)
                : base(12, true, TargetFlags.None)
            {
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                DoLogAsLegalTeleporter(from, targeted);
            }
            public void DoLogAsLegalTeleporter(Mobile from, object targeted)
            {
                string filename = "LegalTrammelTeleporters.log";
                if (targeted is Teleporter teleporter)
                {
                    LogHelper logger = new LogHelper(filename, false, true, true);
                    logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", teleporter.Serial.Value, teleporter.X, teleporter.Y, teleporter.Z, teleporter.Map));
                    logger.Finish();
                    from.SendMessage("Legal: " + string.Format("{0} {1} {2} {3} {4}", teleporter.Serial.Value, teleporter.X, teleporter.Y, teleporter.Z, teleporter.Map));
                }
                else if (targeted is PlayerMobile pm)
                {
                    bool found = false;
                    foreach (Item item in pm.Map.GetItemsInBounds(new Rectangle2D(pm.X, pm.Y, 1, 1)))
                        if (item is Teleporter t2 && t2.X == pm.X && t2.Y == pm.Y)
                        {
                            found = true;
                            LogHelper logger = new LogHelper(filename, false, true, true);
                            logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", t2.Serial.Value, t2.X, t2.Y, t2.Z, t2.Map));
                            logger.Finish();
                            from.SendMessage("Legal: " + string.Format("{0} {1} {2} {3} {4}", t2.Serial.Value, t2.X, t2.Y, t2.Z, t2.Map));

                        }
                    if (found == false)
                        from.SendMessage("There is no teleporter here");
                }
                else
                    from.SendMessage("That is not a teleporter");
            }
        }
        #region EventTeleporter
        [Usage("EventTeleporter")]
        [Description("Tags the targeted teleporter as 'event' (not core). We will deal with this later.")]
        private static void EventTeleporter(CommandEventArgs e)
        {
            if (ItemAtPoint(e.Mobile, typeof(Teleporter)) != null)
            {
                new EventTeleporterTarget(null).DoFlagAsEvent(e.Mobile, ItemAtPoint(e.Mobile, typeof(Teleporter)));
            }
            else
            {
                e.Mobile.SendMessage("Target the teleporter you wish to tag as 'Event'.");
                e.Mobile.Target = new EventTeleporterTarget(null);
            }
        }
        public class EventTeleporterTarget : Target
        {
            public EventTeleporterTarget(object o)
                : base(12, true, TargetFlags.None)
            {
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                DoFlagAsEvent(from, targeted);
            }
            public void DoFlagAsEvent(Mobile from, object targeted)
            {
                string filename = "EventTeleporters.log";
                if (targeted is Teleporter teleporter)
                {
                    LogHelper logger = new LogHelper(filename, false, true, true);
                    logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", teleporter.Serial.Value, teleporter.X, teleporter.Y, teleporter.Z, teleporter.Map));
                    logger.Finish();
                    from.SendMessage("Event: " + string.Format("{0} {1} {2} {3} {4}", teleporter.Serial.Value, teleporter.X, teleporter.Y, teleporter.Z, teleporter.Map));
                }
                else if (targeted is PlayerMobile pm)
                {
                    bool found = false;
                    foreach (Item item in pm.Map.GetItemsInBounds(new Rectangle2D(pm.X, pm.Y, 1, 1)))
                        if (item is Teleporter t2 && t2.X == pm.X && t2.Y == pm.Y)
                        {
                            found = true;
                            LogHelper logger = new LogHelper(filename, false, true, true);
                            logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", t2.Serial.Value, t2.X, t2.Y, t2.Z, t2.Map));
                            logger.Finish();
                            from.SendMessage("Event: " + string.Format("{0} {1} {2} {3} {4}", t2.Serial.Value, t2.X, t2.Y, t2.Z, t2.Map));

                        }
                    if (found == false)
                        from.SendMessage("There is no teleporter here");
                }
                else
                    from.SendMessage("That is not a teleporter");
            }
        }
        #endregion EventTeleporter
        #region EventMoongate
        [Usage("EventMoongate")]
        [Description("Tags the targeted moongate as 'event' (not core). We will deal with this later.")]
        private static void EventMoongate(CommandEventArgs e)
        {
            if (ItemAtPoint(e.Mobile, typeof(Moongate)) != null)
            {
                new EventMoongateTarget(null).DoFlagAsEvent(e.Mobile, ItemAtPoint(e.Mobile, typeof(Moongate)));
            }
            else
            {
                e.Mobile.SendMessage("Target the moongate you wish to tag as 'Event'.");
                e.Mobile.Target = new EventMoongateTarget(null);
            }
        }
        public class EventMoongateTarget : Target
        {
            public EventMoongateTarget(object o)
                : base(12, true, TargetFlags.None)
            {
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                DoFlagAsEvent(from, targeted);
            }
            public void DoFlagAsEvent(Mobile from, object targeted)
            {
                string filename = "EventMoongates.log";
                if (targeted is Moongate moongate)
                {
                    LogHelper logger = new LogHelper(filename, false, true, true);
                    logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", moongate.Serial.Value, moongate.X, moongate.Y, moongate.Z, moongate.Map));
                    logger.Finish();
                    from.SendMessage("Event: " + string.Format("{0} {1} {2} {3} {4}", moongate.Serial.Value, moongate.X, moongate.Y, moongate.Z, moongate.Map));
                }
                else if (targeted is PlayerMobile pm)
                {
                    bool found = false;
                    foreach (Item item in pm.Map.GetItemsInBounds(new Rectangle2D(pm.X, pm.Y, 1, 1)))
                        if (item is Moongate t2 && t2.X == pm.X && t2.Y == pm.Y)
                        {
                            found = true;
                            LogHelper logger = new LogHelper(filename, false, true, true);
                            logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", t2.Serial.Value, t2.X, t2.Y, t2.Z, t2.Map));
                            logger.Finish();
                            from.SendMessage("Event: " + string.Format("{0} {1} {2} {3} {4}", t2.Serial.Value, t2.X, t2.Y, t2.Z, t2.Map));

                        }
                    if (found == false)
                        from.SendMessage("There is no moongate here");
                }
                else
                    from.SendMessage("That is not a moongate");
            }
        }
        #endregion EventMoongate
        [Usage("ExcludeTeleporter")]
        [Description("Excludes the targeted teleporter in a list which is read by the patcher when wiping all teleporters.")]
        private static void ExcludeTeleporter(CommandEventArgs e)
        {
            List<string> args = ExcludeTeleporterParseArgs(e);
            if (args == null)
            {
                e.Mobile.SendMessage("Usage: [ExcludeTeleporter <is | ai | both>");
                return;
            }
            if (ItemAtPoint(e.Mobile, typeof(Teleporter)) != null)
            {
                new ExcludeTeleporterTarget(args).DoExclude(e.Mobile, ItemAtPoint(e.Mobile, typeof(Teleporter)));
            }
            else
            {
                e.Mobile.SendMessage("Target the teleporter you wish to exclude.");
                e.Mobile.Target = new ExcludeTeleporterTarget(args);
            }
        }
        private static List<string> ExcludeTeleporterParseArgs(CommandEventArgs e)
        {
            if (string.IsNullOrEmpty(e.ArgString))
                return null;
            List<string> args = new List<string>();
            string[] strings = e.ArgString.Split(' ');

            // can only be "is" or "ai" or "both"
            if (strings.Length > 2)
                return null;

            foreach (string tok in strings)
                if (tok == "is" || tok == "ai")
                    args.Add(string.Format("ExcludeTeleporter" + tok.ToUpper() + ".log"));
                else if (tok == "both")
                {
                    args.Add("ExcludeTeleporter" + "IS" + ".log");
                    args.Add("ExcludeTeleporter" + "AI" + ".log");
                }
                else
                    continue;

            return args;
        }
        public class ExcludeTeleporterTarget : Target
        {
            List<string> m_FileNames;
            public ExcludeTeleporterTarget(object o)
                : base(12, true, TargetFlags.None)
            {
                m_FileNames = o as List<string>;
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                DoExclude(from, targeted);
            }
            public void DoExclude(Mobile from, object targeted)
            {
                if (targeted is Teleporter teleporter)
                {
                    foreach (string filename in m_FileNames)
                    {
                        LogHelper logger = new LogHelper(filename, false, true, true);
                        logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", teleporter.Serial.Value, teleporter.X, teleporter.Y, teleporter.Z, teleporter.Map));
                        logger.Finish();
                        from.SendMessage("Excluded: " + string.Format("{0} {1} {2} {3} {4}", teleporter.Serial.Value, teleporter.X, teleporter.Y, teleporter.Z, teleporter.Map));
                    }
                }
                else if (targeted is PlayerMobile pm)
                {
                    bool found = false;
                    foreach (Item item in pm.Map.GetItemsInBounds(new Rectangle2D(pm.X, pm.Y, 1, 1)))
                        if (item is Teleporter t2 && t2.X == pm.X && t2.Y == pm.Y)
                        {
                            found = true;
                            foreach (string filename in m_FileNames)
                            {
                                LogHelper logger = new LogHelper(filename, false, true, true);
                                logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", t2.Serial.Value, t2.X, t2.Y, t2.Z, t2.Map));
                                logger.Finish();
                                from.SendMessage("Excluded: " + string.Format("{0} {1} {2} {3} {4}", t2.Serial.Value, t2.X, t2.Y, t2.Z, t2.Map));
                            }
                        }
                    if (found == false)
                        from.SendMessage("There is no teleporter here");
                }
                else
                    from.SendMessage("That is not a teleporter");
            }
        }
        [Usage("PreserveTeleporter")]
        [Description("Preserves the targeted teleporter in a list which is read by the patcher when wiping all teleporters.")]
        private static void PreserveTeleporter(CommandEventArgs e)
        {
            List<string> args = PreserveTeleporterParseArgs(e);
            if (args == null || args.Count == 0)
            {
                e.Mobile.SendMessage("Usage: [PreserveTeleporter <is | ai | both>");
                return;
            }
            if (ItemAtPoint(e.Mobile, typeof(Teleporter)) != null)
            {
                new PreserveTeleporterTarget(args).DoPreserve(e.Mobile, ItemAtPoint(e.Mobile, typeof(Teleporter)));
            }
            else
            {
                e.Mobile.SendMessage("Target the teleporter you wish to preserve.");
                e.Mobile.Target = new PreserveTeleporterTarget(args);
            }
        }
        private static List<string> PreserveTeleporterParseArgs(CommandEventArgs e)
        {
            if (string.IsNullOrEmpty(e.ArgString))
                return null;
            List<string> args = new List<string>();
            string[] strings = e.ArgString.Split(' ');

            // can only be "is" or "ai" or "both"
            if (strings.Length > 2)
                return null;

            foreach (string tok in strings)
                if (tok == "is" || tok == "ai")
                    args.Add(string.Format("PreserveTeleporter" + tok.ToUpper() + ".log"));
                else if (tok == "both")
                {
                    args.Add("PreserveTeleporter" + "IS" + ".log");
                    args.Add("PreserveTeleporter" + "AI" + ".log");
                }
                else
                    continue;

            return args;
        }
        public class PreserveTeleporterTarget : Target
        {
            List<string> m_FileNames;
            public PreserveTeleporterTarget(object o)
                : base(12, true, TargetFlags.None)
            {
                m_FileNames = o as List<string>;
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                DoPreserve(from, targeted);
            }
            /// <summary>
            /// Remember, after you have a preserve a list of teleporters, move it to the Data/Patches/*.cfg
            /// This will be picked up by Nuke.RefreshTeleporters() 
            /// </summary>
            /// <param name="from"></param>
            /// <param name="targeted"></param>
            public void DoPreserve(Mobile from, object targeted)
            {
                if (targeted is Teleporter teleporter)
                {
                    foreach (string filename in m_FileNames)
                    {
                        LogHelper logger = new LogHelper(filename, false, true, true);
                        logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", teleporter.Serial.Value, teleporter.X, teleporter.Y, teleporter.Z, teleporter.Map));
                        logger.Finish();
                        from.SendMessage("Preserved: " + string.Format("{0} {1} {2} {3} {4}", teleporter.Serial.Value, teleporter.X, teleporter.Y, teleporter.Z, teleporter.Map));
                    }
                }
                else if (targeted is PlayerMobile pm)
                {
                    bool found = false;
                    foreach (Item item in pm.Map.GetItemsInBounds(new Rectangle2D(pm.X, pm.Y, 1, 1)))
                        if (item is Teleporter t2 && t2.X == pm.X && t2.Y == pm.Y)
                        {
                            found = true;
                            foreach (string filename in m_FileNames)
                            {
                                LogHelper logger = new LogHelper(filename, false, true, true);
                                logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", t2.Serial.Value, t2.X, t2.Y, t2.Z, t2.Map));
                                logger.Finish();
                                from.SendMessage("Preserved: " + string.Format("{0} {1} {2} {3} {4}", t2.Serial.Value, t2.X, t2.Y, t2.Z, t2.Map));
                            }
                        }
                    if (found == false)
                        from.SendMessage("There is no teleporter here");
                }
                else
                    from.SendMessage("That is not a teleporter");
            }
        }
        [Usage("DeleteAllTeleporters")]
        [Description("Turns off Saves, then wipes all teleporters.")]
        private static void DeleteAllTeleporters(CommandEventArgs e)
        {
            AutoSave.SavesEnabled = false;
            int count = 0;
            foreach (Item item in World.Items.Values)
                if (item is Teleporter teleporter)
                {
                    teleporter.Delete();
                    count++;
                }

            e.Mobile.SendMessage("{0} Teleporters deleted.", count);
        }
        private static void VisitAngelIslandTeleporters(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            // get a list of legal items
            string pathName = Path.Combine(Core.DataDirectory, "Patches", "Angel Island teleporters.cfg");
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
                {   // we will pack out bad items in Patcher.HasValidatedTeleporterPatchLists()
                    //  we probably captured an EffectItem or corpse(and items) or something
                    Item item = World.FindItem(record.Item1);
                    if (item == null || item.Deleted)
                        count++;
                }

                // log the items
                (e.Mobile as PlayerMobile).JumpIndex = 0;
                (e.Mobile as PlayerMobile).JumpList = new System.Collections.ArrayList();
                foreach (var record in records)
                    (e.Mobile as PlayerMobile).JumpList.Add(new WorldLocation(record.Item2, record.Item3, record.Item4, record.Item5));
                if (count > 0)
                    e.Mobile.SendMessage("Info: {0} missing items detected.", count);

                e.Mobile.SendMessage("Your JumpList has been loaded with {0} locations.", (e.Mobile as PlayerMobile).JumpList.Count);
            }
            else
                e.Mobile.SendMessage("Cannot find {0}", pathName);
        }
        private static void VisitLegalTrammelItems(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            // get a list of legal items
            string pathName = Path.Combine(Core.DataDirectory, "Patches", "LegalTrammelItems.cfg");
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
                {   // we will pack out bad items in Patcher.HasValidatedTeleporterPatchLists()
                    //  we probably captured an EffectItem or corpse(and items) or something
                    Item item = World.FindItem(record.Item1);
                    if (item == null || item.Deleted)
                        count++;
                }

                // log the items
                (e.Mobile as PlayerMobile).JumpIndex = 0;
                (e.Mobile as PlayerMobile).JumpList = new System.Collections.ArrayList();
                foreach (var record in records)
                    (e.Mobile as PlayerMobile).JumpList.Add(new WorldLocation(record.Item2, record.Item3, record.Item4, record.Item5));
                if (count > 0)
                    e.Mobile.SendMessage("Info: {0} missing items detected.", count);

                e.Mobile.SendMessage("Your JumpList has been loaded with the 'after' {0} items.", (e.Mobile as PlayerMobile).JumpList.Count);
            }
            else
                e.Mobile.SendMessage("Cannot find {0}", pathName);
        }
        private static void DecoDedecoDeltaItems(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            // get a list of legal items
            string pathName = Path.Combine(Core.DataDirectory, "Patches", "LegalTrammelItems.cfg");
            if (File.Exists(pathName))
            {

                // first Delete All Illegal Trammel Items
                DeleteAllIllegalTrammelItems(e);


                List<Tuple<int, int, int, int, Map>> records = new();
                foreach (string line in File.ReadAllLines(pathName))
                {
                    string[] toks = line.Split(' ');
                    records.Add(new Tuple<int, int, int, int, Map>(int.Parse(toks[0]), int.Parse(toks[1]), int.Parse(toks[2]), int.Parse(toks[3]), Map.Parse(toks[4])));
                }

                // log the items
                List<Item> before = new();
                foreach (Item item in World.Items.Values)
                    if (item != null && item.Deleted == false && item.Map == Map.Trammel)
                        if (item is not EffectItem)
                            if (!records.Any(t => t.Item1 == item.Serial.Value))
                                if (item is Item)
                                    before.Add(item);

                // Now Deco Trammel
                DecoTrammel(e);

                // now DeDeco Trammel
                DeDecoTrammel(e);

                // log the items
                List<Item> after = new();
                foreach (Item item in World.Items.Values)
                    if (item != null && item.Deleted == false && item.Map == Map.Trammel)
                        if (item is not EffectItem)
                            if (!records.Any(t => t.Item1 == item.Serial.Value))
                                if (item is Item)
                                    after.Add(item);

                // remove items that have a parent. I.e., in a backpack, in a library, etc.
                e.Mobile.SendMessage("Removing {0} items from 'before' list that have a parent.", before.RemoveAll(item => item.RootParent != null));
                e.Mobile.SendMessage("Removing {0} items from 'after' list that have a parent.", after.RemoveAll(item => item.RootParent != null));

                // what's the delta?
                var delta = after.Except(before).ToList();

                if (delta.Count == 0)
                {
                    // sort the list on distance to me
                    after.Sort((e1, e2) =>
                    {
                        return e2.GetDistanceToSqrt(e.Mobile).CompareTo(e1.GetDistanceToSqrt(e.Mobile));
                    });

                    (e.Mobile as PlayerMobile).JumpIndex = 0;
                    (e.Mobile as PlayerMobile).JumpList = new System.Collections.ArrayList(after);
                    e.Mobile.SendMessage("No differences detected in 'before' and 'after'.");
                    e.Mobile.SendMessage("Your JumpList has been loaded with the 'after' {0} items.", (e.Mobile as PlayerMobile).JumpList.Count);
                }
                else
                {
                    (e.Mobile as PlayerMobile).JumpIndex = 0;
                    (e.Mobile as PlayerMobile).JumpList = new System.Collections.ArrayList(delta);
                    e.Mobile.SendMessage("Trammel Item delta load complete.");
                    e.Mobile.SendMessage("Your JumpList has been loaded with {0} items.", (e.Mobile as PlayerMobile).JumpList.Count);
                }
            }
            else
                e.Mobile.SendMessage("Cannot find {0}", pathName);
        }
        [Usage("LegalTrammelItem")]
        [Description("Logs the targeted Item as being a Legal Trammel Item (not to delete).")]
        private static void LegalTrammelItem(CommandEventArgs e)
        {
            if (e.Mobile.Map != Map.Trammel)
            {
                e.Mobile.SendMessage("You must be in Trammel to use this command.");
                return;
            }
            if (ItemAtPoint(e.Mobile) != null)
            {
                new LegalTrammelItemTarget(null).DoLogAsLegalItem(e.Mobile, ItemAtPoint(e.Mobile));
            }
            else
            {
                e.Mobile.SendMessage("Target the Item you wish to log as 'Legal'.");
                e.Mobile.Target = new LegalTrammelItemTarget(null);
            }
        }
        public class LegalTrammelItemTarget : Target
        {
            public LegalTrammelItemTarget(object o)
                : base(12, true, TargetFlags.None)
            {
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                DoLogAsLegalItem(from, targeted);
            }
            public void DoLogAsLegalItem(Mobile from, object targeted)
            {
                string filename = "LegalTrammelItems.log";
                if (targeted is Item Item)
                {
                    LogHelper logger = new LogHelper(filename, false, true, true);
                    logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", Item.Serial.Value, Item.X, Item.Y, Item.Z, Item.Map));
                    logger.Finish();
                    from.SendMessage("Legal: " + string.Format("{0} {1} {2} {3} {4}", Item.Serial.Value, Item.X, Item.Y, Item.Z, Item.Map));
                }
                else if (targeted is PlayerMobile pm)
                {
                    bool found = false;
                    foreach (Item item in pm.Map.GetItemsInBounds(new Rectangle2D(pm.X, pm.Y, 1, 1)))
                        if (item is Item t2 && t2.X == pm.X && t2.Y == pm.Y)
                        {
                            found = true;
                            LogHelper logger = new LogHelper(filename, false, true, true);
                            logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", t2.Serial.Value, t2.X, t2.Y, t2.Z, t2.Map));
                            logger.Finish();
                            from.SendMessage("Legal: " + string.Format("{0} {1} {2} {3} {4}", t2.Serial.Value, t2.X, t2.Y, t2.Z, t2.Map));

                        }
                    if (found == false)
                        from.SendMessage("There is no Item here");
                }
                else
                    from.SendMessage("That is not a Item");
            }
        }
        private static Item ItemAtPoint(Mobile m, Type type = null)
        {
            IPooledEnumerable eable = m.Map.GetItemsInRange(m.Location, 0);
            Item theItem = null;
            foreach (Item item in eable)
                if (item is Item && item.Deleted == false)
                    if (type == null)
                    {
                        theItem = item;
                        break;
                    }
                    else if (type == item.GetType())
                    {
                        theItem = item;
                        break;
                    }

            eable.Free();
            return theItem;
        }
        [Usage("LegalTrammelItemsInReg")]
        [Description("Logs the Items within a region as being a Legal Trammel Items (not to delete).")]
        private static void LegalTrammelItemsInReg(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from.Map != Map.Trammel)
            {
                from.SendMessage("You must be in Trammel to use this command.");
                return;
            }

            int count = 0;
            if (from.Region != null && !from.Region.IsDefault)
            {
                foreach (Item witem in World.Items.Values)
                    if (witem.RootParent is not Mobile)
                        if (witem.RootParent == null && from.Region.Contains(witem.Location) || witem.RootParent != null && from.Region.Contains((witem.RootParent as Item).Location))
                            /// -----------------------^^^ get the item, ---------------------------------------------------^^^ even it it's in a container
                            if (witem is not EffectItem)
                            {
                                count++;
                                DoLogAsLegalItem(from, witem);
                            }

                if (count > 0)
                {
                    from.SendMessage("Captured {0} items", count);

                    // now clean the list as it's likely to have a ton of duplicates
                    string filename = "LegalTrammelItems.log";
                    string pathName = Path.Combine(Core.BaseDirectory, "Logs", filename);
                    List<string> lines = new();
                    if (File.Exists(pathName))
                    {
                        foreach (String line in File.ReadAllLines(pathName))
                            if (!lines.Contains(line))
                                lines.Add(line);

                        File.WriteAllLines(pathName, lines);
                    }

                    from.SendMessage("Log contains {0} unique entries", lines.Count);
                }
                else
                    from.SendMessage("There are no items here.");
            }
            else
                from.SendMessage("Inappropriate region for this command.");

        }
        private static void DoLogAsLegalItem(Mobile from, object o)
        {
            string filename = "LegalTrammelItems.log";
            if (o is Item i && i.Deleted == false || o is Static s && s.Deleted == false)
            {
                string output = string.Empty;
                LogHelper logger = new LogHelper(filename, false, true, true);

                if (o is Static sitem)
                    output = string.Format("{0} {1} {2} {3} {4}", sitem.Serial.Value, sitem.X, sitem.Y, sitem.Z, sitem.Map);
                else if (o is Item item)
                    output = string.Format("{0} {1} {2} {3} {4}", item.Serial.Value, item.X, item.Y, item.Z, item.Map);
                else
                    ;

                logger.Log(LogType.Text, output);

                from.SendMessage("Legal: " + output);
                logger.Finish();
            }
            else
                ;
        }
        private static void MLMapTest(CommandEventArgs e)
        {
            Teleporter teleporter = null;
            Map map = Map.Trammel;
            Point3D location;
            bool found = false;

            // first clean the locations
            MLMapClean(map, new Point3D(2666, 2073, 5));
            MLMapClean(map, new Point3D(6172, 21, 0));

            // TEST 1: This should work fine
            location = new Point3D(2666, 2073, 5);  // Buccaneer's Den underground tunnels
            if (Utility.World.InaccessibleMapLoc(location, map) == false)
                e.Mobile.SendMessage("{0} is a valid location", location);
            else
                e.Mobile.SendMessage("{0} is not a valid location", location);
            teleporter = new Teleporter();
            teleporter.MoveToWorld(location, map);
            IPooledEnumerable eable = map.GetItemsInRange(location, 0);
            foreach (Item item in eable)
                if (item is Teleporter && item.Deleted == false)
                {
                    found = true;
                    e.Mobile.SendMessage("Found teleporter {0}", teleporter);
                }
            eable.Free();
            if (found == false)
            {   // this always works
                e.Mobile.SendMessage("Teleporter {0} not found at expected location.", teleporter);
                if (World.FindItem(teleporter.Serial) != null)
                    e.Mobile.SendMessage("But World.FindItem() found teleporter at {0}, Sector is valid: {1}.", World.FindItem(teleporter.Serial).GetWorldLocation(),
                        map.GetSector(teleporter.X, teleporter.Y) != Map.Internal.InvalidSector);
            }

            // TEST 2: This should fail
            location = new Point3D(6172, 21, 0);    // Mondain's Legacy dungeons - Entrance gate
            if (Utility.World.InaccessibleMapLoc(location, map) == false)
                e.Mobile.SendMessage("{0} is a valid location", location);
            else
                e.Mobile.SendMessage("{0} is not a valid location", location);
            teleporter = new Teleporter();
            teleporter.MoveToWorld(location, map);
            eable = map.GetItemsInRange(location, 0);
            found = false;
            foreach (Item item in eable)
                if (item is Teleporter && item.Deleted == false)
                {
                    found = true;
                    e.Mobile.SendMessage("Found teleporter {0}", teleporter);
                }
            eable.Free();
            if (found == false)
            {   // this always fails - wtf?
                e.Mobile.SendMessage("Teleporter {0} not found at expected location.", teleporter);
                if (World.FindItem(teleporter.Serial) != null)
                    e.Mobile.SendMessage("But World.FindItem() found teleporter at {0}, Sector is valid: {1}.", World.FindItem(teleporter.Serial).GetWorldLocation(),
                        map.GetSector(teleporter.X, teleporter.Y) != Map.Internal.InvalidSector);
            }
        }
        private static void MLMapClean(Map map, Point3D location)
        {
            List<Item> list = new List<Item>();
            IPooledEnumerable eable = map.GetItemsInRange(location, 0);
            foreach (Item item in eable)
                if (item is Teleporter && item.Deleted == false)
                    list.Add(item);
            eable.Free();

            foreach (Item item in list)
                item.Delete();
        }
        [Usage("DeleteAllIllegalTrammelItems")]
        [Description("Deletes all Trammel items not covered by .")]
        private static int DeleteAllIllegalTrammelItems(CommandEventArgs e)
        {
            int deleted = 0;
            // first, read the list of items to exclude from the trammel search
            string pathName = Path.Combine(Core.DataDirectory, "Patches", "LegalTrammelItems.cfg");
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
                foreach (Item item in World.Items.Values)
                    if (item != null && item.Deleted == false && item.Map == Map.Trammel)
                        if (item is not EffectItem)
                            if (!records.Any(t => t.Item1 == item.Serial.Value))
                                if (item is Item)
                                    logger.Log(LogType.Item, item);

                logger.Finish();

                // delete the items
                int starting = 0;
                foreach (Item item in World.Items.Values)
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
        private static void WipeIllegalTrammelTeleporters(CommandEventArgs e)
        {
            string pathName = Path.Combine(Core.DataDirectory, "Patches", "LegalTrammelTeleporters.cfg");
            if (File.Exists(pathName))
            {
                List<Tuple<int, int, int, Map>> records = new();
                foreach (string line in File.ReadAllLines(pathName))
                {
                    string[] toks = line.Split(' ');
                    records.Add(new Tuple<int, int, int, Map>(int.Parse(toks[0]), int.Parse(toks[1]), int.Parse(toks[2]), Map.Parse(toks[3])));
                }

                // log the teleporters
                LogHelper logger = new LogHelper("OrphanedTrammelTeleporters.log", true, true, true);
                foreach (Item item in World.Items.Values)
                    if (item != null && item.Deleted == false && item.Map == Map.Trammel)
                        if (!records.Any(t => t.Item1 == item.Serial.Value))
                            if (item is Teleporter)
                                logger.Log(LogType.Item, item);
                logger.Finish();

                // count the teleporters
                int delete = 0;
                int starting = 0;
                foreach (Item item in World.Items.Values)
                    if (item != null && item.Deleted == false && item.Map == Map.Trammel)
                    {
                        starting++;
                        if (!records.Any(t => t.Item1 == item.Serial.Value))
                            if (item is Teleporter)
                            {
                                item.Delete();
                                delete++;
                            }
                    }

                e.Mobile.SendMessage("{0}/{1} Trammel teleporters deleted.", delete, starting);
            }
            else
                e.Mobile.SendMessage("Cannot find {0}", pathName);
        }
        private static void WipeAllItems(CommandEventArgs e)
        {
            if (e.Mobile.AccessLevel != AccessLevel.Owner)
            {
                e.Mobile.SendMessage("You are not authorized to run this command.");
                return;
            }
            if (Environment.MachineName.Length.ToString().ToLower() == "LUKES-AISERVER".ToLower() || !Core.Debug)
            {
                e.Mobile.SendMessage("You cannot run this command on a production machine or build.");
                return;
            }

            AutoSave.SavesEnabled = false;

            List<Item> list = new();
            foreach (Item item in World.Items.Values)
                if (item.RootParent is PlayerMobile pm && pm.AccessLevel > AccessLevel.Player)
                    continue;
                else
                    list.Add(item);

            foreach (Item item in list)
                item.Delete();

            foreach (Item item in World.Items.Values)
                e.Mobile.SendMessage("Stubborn item {0}.", item);

            e.Mobile.SendMessage("{0} items deleted.", list.Count);
        }
        private static void WipeAllMobiles(CommandEventArgs e)
        {
            if (e.Mobile.AccessLevel != AccessLevel.Owner)
            {
                e.Mobile.SendMessage("You are not authorized to run this command.");
                return;
            }
            if (Environment.MachineName.Length.ToString().ToLower() == "LUKES-AISERVER".ToLower() || !Core.Debug)
            {
                e.Mobile.SendMessage("You cannot run this command on a production machine or build.");
                return;
            }

            AutoSave.SavesEnabled = false;

            List<Mobile> list = new();
            int spawner_count = 0;
            int champ_count = 0;
            foreach (Item item in World.Items.Values)
                if (item is not null)
                    if (item is Spawner spawner && spawner.Running)
                    {
                        spawner.Running = false;
                        spawner.RemoveObjects();
                        spawner_count++;
                    }
                    else if (item is ChampEngine ce)
                    {
                        ce.Running = false;
                        ce.ClearMonsters = true;
                        champ_count++;
                    }

            // anything left?
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile is not null && mobile.Player == false)
                    list.Add(mobile);
            }
            foreach (Mobile mobile in list)
                mobile.Delete();

            e.Mobile.SendMessage("{0} spawners cleared. {1} champ engines cleared. {2} remaining mobiles deleted. ", spawner_count, champ_count, list.Count);
        }
        private static void KillAllMobiles(CommandEventArgs e)
        {
            if (e.Mobile.AccessLevel != AccessLevel.Owner)
            {
                e.Mobile.SendMessage("You are not authorized to run this command.");
                return;
            }
            if (Environment.MachineName.Length.ToString().ToLower() == "LUKES-AISERVER".ToLower() || !Core.Debug)
            {
                e.Mobile.SendMessage("You cannot run this command on a production machine or build.");
                return;
            }

            AutoSave.SavesEnabled = false;

            // sorry guys
            List<Mobile> list = new();
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile is not null && mobile.Player == false)
                    list.Add(mobile);
            }
            foreach (Mobile mobile in list)
                mobile.Kill();

            e.Mobile.SendMessage("{0} remaining mobiles killed. ", list.Count);
        }
        private static void CountAllItems(CommandEventArgs e)
        {
            List<Item> list = new();
            foreach (Item item in World.Items.Values)
                list.Add(item);

            e.Mobile.SendMessage("There are {0} items in the world.", list.Count);
        }
        private static void FindTeleporter(CommandEventArgs e)
        {
            Teleporter teleporter = ItemAtPoint(e.Mobile, typeof(Teleporter)) as Teleporter;
            if (teleporter == null)
                e.Mobile.SendMessage("There is no teleporter here.");
            else
                e.Mobile.SendMessage("Found Teleporter {0}.", teleporter);
        }
        private static void DecoTrammel(CommandEventArgs e)
        {
            // first wipe the deco (includes some teleporters)
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Britannia"), DecoMode.delete, maps: new Map[] { Map.Trammel });
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Trammel"), DecoMode.delete, maps: new Map[] { Map.Trammel });

            // wipe teleporters
            TeleportersCreator tc = new TeleportersCreator();
            tc.CreateTeleporters(listOnly: false, maps: new Map[] { Map.Trammel }, Decorate.DecoMode.delete);

            // now spawn
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Britannia"), DecoMode.add, maps: new Map[] { Map.Trammel });
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Trammel"), DecoMode.add, maps: new Map[] { Map.Trammel });

            // now add the teleporters
            tc.CreateTeleporters(listOnly: false, maps: new Map[] { Map.Trammel }, Decorate.DecoMode.add);

            // count the teleporters
            int count = 0;
            int allCount = 0;
            foreach (Item item in World.Items.Values)
                if (item.Map == Map.Trammel && item.Deleted == false)
                {
                    allCount++;
                    if (item is Teleporter)
                        count++;
                }

            e.Mobile.SendMessage("There now {0} teleporters in Trammel.", count);
            e.Mobile.SendMessage("There now {0} items in Trammel.", allCount);
        }
        private static void DeDecoTrammel(CommandEventArgs e)
        {
            // first wipe
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Britannia"), DecoMode.delete, maps: new Map[] { Map.Trammel });
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Trammel"), DecoMode.delete, maps: new Map[] { Map.Trammel });

            // wipe teleporters
            TeleportersCreator tc = new TeleportersCreator();
            tc.CreateTeleporters(listOnly: false, maps: new Map[] { Map.Trammel }, Decorate.DecoMode.delete);

            // count the teleporters
            int count = 0;
            int allCount = 0;
            foreach (Item item in World.Items.Values)
                if (item.Map == Map.Trammel && item.Deleted == false)
                {
                    allCount++;
                    if (item is Teleporter)
                        count++;
                }

            e.Mobile.SendMessage("There now {0} teleporters in Trammel.", count);
            e.Mobile.SendMessage("There now {0} items in Trammel.", allCount);

            // init the jumplist
            (e.Mobile as PlayerMobile).JumpIndex = 0;
            (e.Mobile as PlayerMobile).JumpList = new ArrayList();
            foreach (Item item in World.Items.Values)
                if (item.Map == Map.Trammel)
                    if (item is Teleporter && item.Deleted == false)
                        (e.Mobile as PlayerMobile).JumpList.Add(item.GetWorldLocation());

            e.Mobile.SendMessage("Your jump list now contains {0} entries.", (e.Mobile as PlayerMobile).JumpList.Count);
        }
        private static void VisitTrammelItems(CommandEventArgs e)
        {
            // first, read the list of items to exclude from the trammel search
            string filename = "LegalTrammelItems.log";
            string pathName = Path.Combine(Core.BaseDirectory, "Logs", filename);
            List<Tuple<int, int, int, Map>> records = new();
            if (File.Exists(pathName))
            {
                foreach (String line in File.ReadAllLines(pathName))
                {
                    string[] toks = line.Split(' ');
                    records.Add(new Tuple<int, int, int, Map>(int.Parse(toks[0]), int.Parse(toks[1]), int.Parse(toks[2]), Map.Parse(toks[3])));
                }
            }

            // count the items
            int count = 0;
            int reserved = 0;
            foreach (Item item in World.Items.Values)
                if (item != null && item.Deleted == false && item.Map == Map.Trammel)
                    if (item.RootParent is not Mobile)
                        if (item is not EffectItem)
                            if (!records.Any(t => t.Item1 == item.Serial.Value))
                                count++;
                            else
                                reserved++;

            e.Mobile.SendMessage("There now {0} items in Trammel excluding reserved.", count);

            // init the jumplist
            reserved = 0;
            List<Item> list = new();
            (e.Mobile as PlayerMobile).JumpIndex = 0;
            (e.Mobile as PlayerMobile).JumpList = new ArrayList();
            foreach (Item item in World.Items.Values)
                if (item.Map == Map.Trammel)
                    if (item is Item && item.Deleted == false)
                        if (item.RootParent is not Mobile)
                            if (item is not EffectItem)
                                if (!records.Any(t => t.Item1 == item.Serial.Value))
                                    list.Add(item);
                                else
                                    reserved++;

            // sort the list on distance to me
            list.Sort((e1, e2) =>
            {
                return e2.GetDistanceToSqrt(e.Mobile).CompareTo(e1.GetDistanceToSqrt(e.Mobile));
            });

            (e.Mobile as PlayerMobile).JumpList = new ArrayList(list);

            e.Mobile.SendMessage("Your jump list now contains {0} entries.", (e.Mobile as PlayerMobile).JumpList.Count);
        }
        private static void DeleteChampTeleporters(CommandEventArgs e)
        {
            Decorate.Generate(Path.Combine(Core.DataDirectory, "Decoration/Felucca"), Decorate.DecoMode.delete, "_champion teleporters.cfg", maps: new Map[] { Map.Felucca });
            e.Mobile.SendMessage("Champion Teleporters Deleted.");
        }
        private static void AddChampTeleporters(CommandEventArgs e)
        {
            Decorate.Generate(Path.Combine(Core.DataDirectory, "Decoration/Felucca"), Decorate.DecoMode.add, "_champion teleporters.cfg", maps: new Map[] { Map.Felucca });
            e.Mobile.SendMessage("Champion Teleporters Added.");
        }
        private static void DecorateAddons(CommandEventArgs e)
        {
            // decorate only addons
            Dictionary<Item, Tuple<Point3D, Map, bool>> tmp =
                Generate(Path.Combine(Core.DataDirectory, "Decoration/Felucca"), DecoMode.add, file: "*.cfg", listOnly: false, types: new Type[] { typeof(BaseAddon) }, maps: new Map[] { Map.Felucca });
            foreach (KeyValuePair<Item, Tuple<Point3D, Map, bool>> kvp in tmp)
            {
                if (kvp.Key is BaseAddon)
                {
                    LogHelper logger = new LogHelper("DecorateSpawners.log", false, true, true);
                    logger.Log(LogType.Text, string.Format("Decorate spawning {1} at {0}", kvp.Value.Item1, kvp.Key.GetType().Name));
                    logger.Finish();
                }
                else
                    Utility.ConsoleWriteLine("Logic Error: We should only be seeing BaseAddon.", ConsoleColor.Red);
                // only delete if we are in list only mode
                //kvp.Key.Delete();
            }

            tmp = Generate(Path.Combine(Core.DataDirectory, "Decoration/Britannia"), DecoMode.add, file: "*.cfg", listOnly: false, types: new Type[] { typeof(BaseAddon) }, maps: new Map[] { Map.Felucca });
            foreach (KeyValuePair<Item, Tuple<Point3D, Map, bool>> kvp in tmp)
            {
                if (kvp.Key is BaseAddon)
                {
                    LogHelper logger = new LogHelper("DecorateSpawners.log", false, true, true);
                    logger.Log(LogType.Text, string.Format("Decorate spawning {1} at {0}", kvp.Value.Item1, kvp.Key.GetType().Name));
                    logger.Finish();
                }
                else
                    Utility.ConsoleWriteLine("Logic Error: We should only be seeing BaseAddon.", ConsoleColor.Red);
                // only delete if we are in list only mode
                //kvp.Key.Delete();
            }

            e.Mobile.SendMessage("Done.");
        }
        private static void CheckSpawners(CommandEventArgs e)
        {
            /*
             * So Decorate spawns the following...
             * naturalist(quest), Solen hives, Grizelda(quest), Blackheart(quest)
             */
            Dictionary<Item, Tuple<Point3D, Map, bool>> tmp = Generate(Path.Combine(Core.DataDirectory, "Decoration/Felucca"), DecoMode.add, file: "*.cfg", listOnly: true, types: new Type[] { typeof(Spawner) }, maps: new Map[] { Map.Felucca });
            foreach (KeyValuePair<Item, Tuple<Point3D, Map, bool>> kvp in tmp)
            {
                if (kvp.Key is Spawner)
                {
                    LogHelper logger = new LogHelper("DecorateSpawners.log", false, true, true);
                    logger.Log(LogType.Text, string.Format("Decorate spawning spawner at {0}", kvp.Value.Item1));
                    logger.Finish();
                }
                else
                    Utility.ConsoleWriteLine("Logic Error: We should only be seeing spawners.", ConsoleColor.Red);

                kvp.Key.Delete();
            }

            tmp = Generate(Path.Combine(Core.DataDirectory, "Decoration/Britannia"), DecoMode.add, file: "*.cfg", listOnly: true, types: new Type[] { typeof(Spawner) }, maps: new Map[] { Map.Felucca });
            foreach (KeyValuePair<Item, Tuple<Point3D, Map, bool>> kvp in tmp)
            {
                if (kvp.Key is Spawner)
                {
                    LogHelper logger = new LogHelper("DecorateSpawners.log", false, true, true);
                    logger.Log(LogType.Text, string.Format("Decorate spawning spawner at {0}", kvp.Value.Item1));
                    logger.Finish();
                }
                else
                    Utility.ConsoleWriteLine("Logic Error: We should only be seeing spawners.", ConsoleColor.Red);

                kvp.Key.Delete();
            }

            e.Mobile.SendMessage("Done.");
        }
        private static void NoSpawners(CommandEventArgs e)
        {
            Dictionary<Item, Tuple<Point3D, Map, bool>> tmp = Generate(Path.Combine(Core.DataDirectory, "Decoration/Felucca"), DecoMode.add, file: "*.cfg", listOnly: true, types: null, exclude: new Type[] { typeof(Spawner) }, maps: new Map[] { Map.Felucca });
            foreach (KeyValuePair<Item, Tuple<Point3D, Map, bool>> kvp in tmp)
            {
                if (kvp.Key is Spawner)
                    Utility.ConsoleWriteLine("Logic Error: We should not be seeing spawners.", ConsoleColor.Red);
                else
                    ;

                kvp.Key.Delete();
            }

            tmp = Generate(Path.Combine(Core.DataDirectory, "Decoration/Britannia"), DecoMode.add, file: "*.cfg", listOnly: true, types: null, exclude: new Type[] { typeof(Spawner) }, maps: new Map[] { Map.Felucca });
            foreach (KeyValuePair<Item, Tuple<Point3D, Map, bool>> kvp in tmp)
            {
                if (kvp.Key is Spawner)
                    Utility.ConsoleWriteLine("Logic Error: We should not be seeing spawners.", ConsoleColor.Red);
                else
                    ;

                kvp.Key.Delete();
            }

            e.Mobile.SendMessage("Done.");
        }
        private static void CheckAiTeleporters(CommandEventArgs e)
        {
            (e.Mobile as PlayerMobile).JumpIndex = 0;
            (e.Mobile as PlayerMobile).JumpList = new System.Collections.ArrayList();
            List<Teleporter> toKeep = new();
            if (File.Exists(Path.Combine(Core.LogsDirectory, "PreserveTeleporter.log")))
            {   // load and cleanup our list of exclusions
                foreach (string line in File.ReadAllLines(Path.Combine(Core.LogsDirectory, "PreserveTeleporter.log")))
                {
                    string[] tokens = line.Split(' ');
                    Teleporter teleporter = World.FindItem(int.Parse(tokens[0])) as Teleporter;
                    if (teleporter == null)
                    {
                        Utility.ConsoleWriteLine("Logic Error: Preserved teleporter missing.", ConsoleColor.Red);
                    }
                    else
                        if (!toKeep.Contains(teleporter))
                        toKeep.Add(teleporter);
                }
            }
            else
            {
                e.Mobile.SendMessage(String.Format("{0} does not exist", Path.Combine(Core.LogsDirectory, "PreserveTeleporter.log")));
                return;
            }

            foreach (Teleporter teleporter in toKeep)
                (e.Mobile as PlayerMobile).JumpList.Add(teleporter);

            e.Mobile.SendMessage("Special AI teleporter load complete.");
            e.Mobile.SendMessage("Your JumpList has been loaded with {0} teleporters.", (e.Mobile as PlayerMobile).JumpList.Count);
        }
        private static void AnalyzeCustomObjectsUsage(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Usage: AnalyzeCustomObjects <teleporter | spawner | moongate | etc");
        }
        private static void AnalyzeCustomObjects(CommandEventArgs e)
        {
            if (e.Length != 2 ||
                e.GetString(1).ToLower() != "moongate" &&
                e.GetString(1).ToLower() != "teleporter" &&
                e.GetString(1).ToLower() != "sungate")
            {
                AnalyzeCustomObjectsUsage(e);
                return;
            }
            Type type = ScriptCompiler.FindTypeByName(e.GetString(1));
            if (type == null)
                return;

            e.Mobile.SendMessage("Analyzing {0}s, please wait...", type.Name);
            Dictionary<Item, Tuple<Point3D, Map, bool>> RunUO = new();

            // places we will query
            var facets = new List<(string, Map)>
              {
                  ("Decoration/Felucca", Map.Felucca),
                  ("Decoration/Ilshenar", Map.Ilshenar),
                  ("Decoration/Malas", Map.Malas),
                  ("Decoration/Trammel", Map.Trammel)
              };

            // ugh, need these stupid things for public moongates
            var pmls = new List<PMList>
              {
                PMList.Felucca,
                PMList.Ilshenar,
                PMList.Malas,
                PMList.Trammel
              };

            Dictionary<Item, Tuple<Point3D, Map, bool>> tmp = new();

            // Part 1 (analysis only)
            // Read all the objects of <type> that would be added (listOnly: true)
            //  locate AI objects of <type> not near any of these proposed RunUO objects of <type>
            //  we can then jump [next to see if we want to keep it. If so, we will [Preserve<object of <type>>

            // special case for teleporters
            if (type == typeof(Teleporter))
            {
                // first get the list of proposed RunUO teleporters from 'TelGen'
                TeleportersCreator tc = new TeleportersCreator();
                tc.CreateTeleporters(listOnly: true);
                foreach (KeyValuePair<Teleporter, Tuple<Point3D, Map, bool>> kvp in tc.ToAdd)
                    RunUO.Add(kvp.Key, kvp.Value);
            }

            // special case for PublicMoongate (PublicMoongate are not Moongate) 
            if (type == typeof(PublicMoongate))
            {
                // first get the list of proposed RunUO moongates from 'MoonGen'
                // All facets we care about
                for (int ix = 0; ix < facets.Count; ix++)
                {
                    PublicMoongate.Generate(pmls[ix], listOnly: true, tmp);
                    foreach (KeyValuePair<Item, Tuple<Point3D, Map, bool>> kvp in tmp)
                        if (type.IsAssignableFrom(kvp.Key.GetType()))
                            RunUO.Add(kvp.Key, kvp.Value);

                    // clear the list
                    tmp = new();
                }
            }

            // =======
            // now get the list of proposed RunUO objects of <type> from 'Decorate'
            // =======

            // All facets we care about
            foreach (var facet in facets)
            {
                tmp = Generate(Path.Combine(Core.DataDirectory, facet.Item1), DecoMode.add, file: "*.cfg",
                    listOnly: true, types: new Type[] { type }, maps: new Map[] { facet.Item2 });
                foreach (KeyValuePair<Item, Tuple<Point3D, Map, bool>> kvp in tmp)
                    if (type.IsAssignableFrom(kvp.Key.GetType()))
                        RunUO.Add(kvp.Key, kvp.Value);
            }

            // Britannia
            tmp = Generate(Path.Combine(Core.DataDirectory, "Decoration/Britannia"), DecoMode.add, file: "*.cfg", listOnly: true, types: new Type[] { type }, maps: new Map[] { Map.Felucca });
            foreach (KeyValuePair<Item, Tuple<Point3D, Map, bool>> kvp in tmp)
                if (type.IsAssignableFrom(kvp.Key.GetType()))
                    RunUO.Add(kvp.Key, kvp.Value);

            // prepare the jumplist
            (e.Mobile as PlayerMobile).JumpIndex = 0;
            (e.Mobile as PlayerMobile).JumpList = new System.Collections.ArrayList();

            // look at all AI's objects of <type> and determine which ones are not covered by RunUO.
            foreach (Item item in World.Items.Values)                                               // all world teleporters
                if (item != null && type.IsAssignableFrom(item.GetType()))
                {   // omit the temporary items contained in the RunUO list
                    if (item.Map == null || item.Map == Map.Internal || item.Deleted)
                        continue;                                                                   // likely the ones in our dictionary

                    // If Trammel has a teleporter substantially similar to our own (from some previous 'decorate')
                    //      we can ignore it, since it's a 'standard' teleporter. I.e., Don't add it to our jump/review list
                    // Update: we will use the 'Decorate' info fpr Trammel instead of a direct query
                    //if (SimilarToTrammel(type, item))
                    //continue;

                    // If RunUO is suggesting a teleporter substantially similar to our own,
                    //      we can ignore it, since it's a 'standard' teleporter. I.e., Don't add it to our jump/review list
                    if (SimilarToRunUO(type, RunUO, item))                                      // placed near loc, same map, near same PointDest, same DestMap
                        continue;

                    (e.Mobile as PlayerMobile).JumpList.Add(item);                        // add it to our jump list
                }


            // log what we found that we need to review
            LogHelper review = new LogHelper(String.Format("Angel Island {0}.log", type.Name), true, true, true);
            foreach (Item item in (e.Mobile as PlayerMobile).JumpList)
                review.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", item.Serial.Value, item.X, item.Y, item.Z, item.Map));
            review.Finish();


            // log what we found in RunUO
            //  Note: the serial numbers will be invalid since we will delete these items
            LogHelper found = new LogHelper(String.Format("All proposed RunUO {0}s.log", type.Name), true, true, true);
            foreach (KeyValuePair<Item, Tuple<Point3D, Map, bool>> kvp in RunUO)
                found.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", -1, kvp.Value.Item1.X, kvp.Value.Item1.Y, kvp.Value.Item1.Z, kvp.Value.Item2));
            found.Finish();

            // log what we found in Trammel (why?)
#if false
            found = new LogHelper(String.Format("All proposed Trammel {0}s.log", type.Name), true, true, true);
            foreach (Item item in TrammelItemsOf(type))
                found.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}", item.Serial, item.X, item.Y, item.Z, item.Map));
            found.Finish();
#endif
            // cleanup RunUO items ...
            foreach (KeyValuePair<Item, Tuple<Point3D, Map, bool>> kvp in RunUO)
                kvp.Key.Delete();

            e.Mobile.SendMessage("{0} analysis complete.", type.Name);
            e.Mobile.SendMessage("Your JumpList has been loaded with {0} {1}s.", (e.Mobile as PlayerMobile).JumpList.Count, type.Name);
        }
        private static List<Item> TrammelItemsOf(Type type)
        {
            List<Item> list = new();
            foreach (Item item in World.Items.Values)
                if (item != null && !item.Deleted && item.Map == Map.Trammel)
                    list.Add(item);

            return list;
        }
        private static bool SimilarToRunUO(Type type, Dictionary<Item, Tuple<Point3D, Map, bool>> RunUO, Item toCheck)
        {
            foreach (KeyValuePair<Item, Tuple<Point3D, Map, bool>> kvp in RunUO)

                // look to see how close RunUO's teleporter is to our teleporter
                if (Utility.GetDistanceToSqrt(kvp.Value.Item1, toCheck.Location) < 5)
                    // is RunUO's teleporter going to the same map?                                                     // Scenario: we both have teleporters in Ilshenar at the same cave exit
                    if (kvp.Value.Item2 == toCheck.Map)                                                                 // this will be true - we are at/going to the same map
                    {                                                                                               // and they have a similar destination coord
                        Teleporter tp = null;
                        Moongate mg = null;
                        if (type == typeof(Teleporter))
                        {
                            tp = kvp.Key as Teleporter;
                            if (Utility.GetDistanceToSqrt(tp.PointDest, (toCheck as Teleporter).PointDest) < 5)     // FALSE, ours and their's are > 5 tiles from one another
                                                                                                                    // and they have a similar destination map
                                if (tp.MapDest == (toCheck as Teleporter).MapDest)                                  // true, both exit to same map
                                                                                                                    // too close. we will assume RunUO's teleporter will replace ours            
                                    return true;
                        }
                        else if (type == typeof(Moongate))
                        {
                            mg = kvp.Key as Moongate;
                            if (Utility.GetDistanceToSqrt(mg.PointDest, (toCheck as Moongate).PointDest) < 5)     // FALSE, ours and their's are > 5 tiles from one another
                                                                                                                  // and they have a similar destination map
                                if (mg.MapDest == (toCheck as Moongate).MapDest)                                  // true, both exit to same map
                                                                                                                  // too close. we will assume RunUO's teleporter will replace ours            
                                    return true;
                        }
                        else
                            return false;
                    }

            // we found an AI teleporter sufficiently different from all RunUO standard teleporters.
            //   we will investigate this teleporter and see if it needs to be kept
            return false;
        }
        private static bool SimilarToTrammel(Type type, Item toCheck)
        {
            foreach (Item item in World.Items.Values)                                                               // Trammel teleporters
                if (item != null && item.Map == Map.Trammel)
                    if (item.GetType() == type)
                    {
                        Teleporter tp = null;
                        Moongate mg = null;
                        if (type == typeof(Teleporter))
                        {
                            tp = item as Teleporter;
                            // look to see how close Trammel's teleporter is to our teleporter
                            if (Utility.GetDistanceToSqrt(tp.Location, toCheck.Location) < 5)                       // are they located near the same point?
                                                                                                                    // and they have a similar destination coord
                                if (Utility.GetDistanceToSqrt(tp.PointDest, (toCheck as Moongate).PointDest) < 5)   // are, ours and their's are > 5 tiles from one another?
                                                                                                                    // looks like a match
                                    return true;
                        }
                        else if (type == typeof(Moongate))
                        {
                            mg = item as Moongate;
                            // look to see how close Trammel's teleporter is to our teleporter
                            if (Utility.GetDistanceToSqrt(mg.Location, toCheck.Location) < 5)                       // are they located near the same point?
                                                                                                                    // and they have a similar destination coord
                                if (Utility.GetDistanceToSqrt(mg.PointDest, (toCheck as Moongate).PointDest) < 5)   // are, ours and their's are > 5 tiles from one another?
                                                                                                                    // looks like a match
                                    return true;
                        }
                        else
                            return false;
                    }
            return false;
        }
        private static void RefreshTeleporters(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Refreshing teleporters, please wait...");

            // Part 2
            // Presuming you just ran (AnalyzeTeleporters) and went to each spawner with [next, and tagged the ones to preserve with [PreserveTeleporter,
            //  we should be ready 
            // 2a. load our exception list of teleporters, created with [PreserveTeleporter
            List<Teleporter> toKeep = new();
            if (File.Exists(Path.Combine(Core.LogsDirectory, "PreserveTeleporter.log")))
            {   // load and cleanup our list of exclusions
                foreach (string line in File.ReadAllLines(Path.Combine(Core.LogsDirectory, "PreserveTeleporter.log")))
                {
                    string[] tokens = line.Split(' ');
                    if (!toKeep.Contains(World.FindItem(int.Parse(tokens[0])) as Teleporter))
                        toKeep.Add(World.FindItem(int.Parse(tokens[0])) as Teleporter);
                }
            }
            else
            {
                e.Mobile.SendMessage(String.Format("{0} does not exist", Path.Combine(Core.LogsDirectory, "PreserveTeleporter.log")));
                return;
            }

            // 2a.1. Now just delete all teleporters, again, ignoring anything that may be in our exclusion list
            foreach (Item item in World.Items.Values)
                if (item is Teleporter teleporter)
                    if (!toKeep.Contains(teleporter))
                        teleporter.Delete();
                    else
                        ;

            // 2a.2
            // Create our teleporters!
            int count = new TeleportersCreator().CreateTeleporters();
            e.Mobile.SendMessage("Teleporter generating complete. {0} teleporters were generated.", count);
        }
        #endregion Item Tools
        #region Patch Region Controllers
        private static void PatchRegionControllers(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Patched {0} region controllers", Server.Regions.StaticRegion.PatchRegionControllers());
        }
        #endregion
        #region Patch Spawner Attribs
        private static void PatchSpawnerAttribs(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Disabled. Please review the need. Functionality covered by [ClassicRespawn command");
            return;
            int count = 0;
            e.Mobile.SendMessage("Patching Spawner Attribs...");
            string file = string.Empty;
            foreach (Item ix in World.Items.Values)
            {
                if (!(ix is Spawner sp)) continue;
                if (sp is EventSpawner) continue;
                count++;
                int Shard = 0x01;
                int UOSpawnMap = 0x02;
                // https://uo.stratics.com/hunters/spawn/spawnmap.jpg 
                UInt32 goodFlags = 0;
                goodFlags |= (sp.Shard != ShardConfig.AngelIsland) ? (UInt32)Shard : 0;
                goodFlags |= (sp.Source != string.Empty) ? (UInt32)UOSpawnMap : 0;
                file += sp.Serial.ToString() + Environment.NewLine;
                file += goodFlags.ToString() + Environment.NewLine;
                file += ((UInt32)sp.Shard).ToString() + Environment.NewLine;
                file += sp.Source.ToString() + Environment.NewLine;
            }
            // write the patch file
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(Core.DataDirectory, "HasPatchedSpawnerAttrib.txt")))
            {
                outputFile.WriteLine(file);
            }
            e.Mobile.SendMessage(string.Format("{0} Spawner Attribs patched.", count));
            e.Mobile.SendMessage("Done.");
        }
        private static bool FindSpawnerPatch(Serial serial, ref UInt32 goodFlags, ref UInt32 shardConfig, ref string UOSpawnMap)
        {
            string[] lines = System.IO.File.ReadAllLines(Path.Combine(Core.DataDirectory, "HasPatchedSpawnerAttrib.txt"));

            for (int ix = 0; ix < lines.Length; ix++)
            {
                if (lines[ix].ToLower().Contains(serial.ToString().ToLower()))
                {
                    string sGoodFlags = lines[++ix];
                    string sShardConfig = lines[++ix];
                    string sUOSpawnMap = lines[++ix];
                    if (UInt32.TryParse(sGoodFlags, out goodFlags) == false)
                        return false;
                    if (UInt32.TryParse(sShardConfig, out shardConfig) == false)
                        return false;
                    UOSpawnMap = sUOSpawnMap;
                    return true;
                }
            }
            return false;
        }
        #endregion Patch Spawner Attribs
        #region Patch Static House Doors
        private static void PatchStaticHouseDoors(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Patching blueprints...");
            Map housingMap = Map.Felucca;
            Rectangle2D housingArea = new Rectangle2D(new Point2D(5120, 2048), new Point2D(5423, 2232));
            HashSet<StaticHouse> housesChecked = new HashSet<StaticHouse>();
            Dictionary<string, KeyValuePair<StaticHouse, List<BaseDoor>>> houseAndDoorsByHID = new Dictionary<string, KeyValuePair<StaticHouse, List<BaseDoor>>>();
            for (int y = housingArea.Y; y <= housingArea.End.Y; y++)
            {
                for (int x = housingArea.X; x <= housingArea.End.X; x++)
                {
                    StaticHouse staticHouse = BaseHouse.FindHouseAt(new Point3D(x, y, 0), housingMap, 16) as StaticHouse;
                    if (staticHouse != null && !housesChecked.Contains(staticHouse))
                    {
                        List<BaseDoor> doors = new List<BaseDoor>();
                        MultiComponentList mcl = staticHouse.Components;
                        Rectangle2D houseBounds = new Rectangle2D(staticHouse.X + mcl.Min.X, staticHouse.Y + mcl.Min.Y, mcl.Width, mcl.Height);
                        foreach (Item item in housingMap.GetItemsInBounds(houseBounds))
                        {
                            if (item is BaseDoor)
                                doors.Add((BaseDoor)item);
                        }
                        housesChecked.Add(staticHouse);
                        if (doors.Count != 0)
                            houseAndDoorsByHID[staticHouse.HouseBlueprintID] = new KeyValuePair<StaticHouse, List<BaseDoor>>(staticHouse, doors);
                    }
                }
            }
            string[] fileNames = new string[] { StaticHouseHelper.FileNameProd, StaticHouseHelper.FileNameTest };
            int count = 0;
            foreach (string fileName in fileNames)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);
                foreach (XmlElement xmlHouseElem in xmlDoc["StaticHousing"])
                {
                    if (xmlHouseElem["Doors"] == null)
                    {
                        string houseID = xmlHouseElem["id"].InnerText;
                        KeyValuePair<StaticHouse, List<BaseDoor>> houseAndDoors;
                        if (houseAndDoorsByHID.TryGetValue(houseID, out houseAndDoors))
                        {
                            StaticHouse staticHouse = houseAndDoors.Key;
                            List<BaseDoor> doors = houseAndDoors.Value;
                            XmlElement xmlDoorsElem = xmlDoc.CreateElement("Doors");
                            foreach (BaseDoor door in doors)
                            {
                                XmlElement xmlDoorElem = xmlDoc.CreateElement("Door");
                                XmlElement xmlElem = xmlDoc.CreateElement("x");
                                xmlElem.InnerText = (door.X - staticHouse.X).ToString();
                                xmlDoorElem.AppendChild(xmlElem);
                                xmlElem = xmlDoc.CreateElement("y");
                                xmlElem.InnerText = (door.Y - staticHouse.Y).ToString();
                                xmlDoorElem.AppendChild(xmlElem);
                                xmlElem = xmlDoc.CreateElement("z");
                                xmlElem.InnerText = (door.Z - staticHouse.Z).ToString();
                                xmlDoorElem.AppendChild(xmlElem);
                                xmlElem = xmlDoc.CreateElement("doorType");
                                xmlElem.InnerText = DoorHelper.Identify(door).ToString();
                                xmlDoorElem.AppendChild(xmlElem);
                                xmlElem = xmlDoc.CreateElement("facing");
                                xmlElem.InnerText = door.Facing.ToString();
                                xmlDoorElem.AppendChild(xmlElem);
                                xmlDoorsElem.AppendChild(xmlDoorElem);
                            }
                            xmlHouseElem.AppendChild(xmlDoorsElem);
                            count++;
                        }
                    }
                }
                xmlDoc.Save(fileName);
            }
            e.Mobile.SendMessage(String.Format("Done! Patched {0} blueprints.", count));
        }
        #endregion
        #region Reset Bard Points
        /*	** OSI MODEL **
			1.  Legendary 120 
			2.  Elder 110 
			3.  Grandmaster 100 
			4.  Master 90 
			5.  Adept 80 
			6.  Expert 70 
			7.  Journeyman 60 
			8.  Apprentice 50 
			9.  Novice 40 
			10. Neophyte 30 
			No Title 29 or below 
		 */
        private static void ResetBardPoints(CommandEventArgs e)
        {
            int count = 0;
            e.Mobile.SendMessage("Resetting Bard Points...");
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m == null || m.Deleted || !(m is PlayerMobile pm) || pm.NpcGuild != NpcGuild.BardsGuild)
                    continue;

                // move anyone Master or above down to Expert.
                if (pm.NpcGuildPoints >= 900)
                    pm.NpcGuildPoints = 700;
                count++;
            }
            e.Mobile.SendMessage(string.Format("{0} Bards reset.", count));
            e.Mobile.SendMessage("Done.");
        }
        #endregion Reset Bard Points
        #region Replace Award Instruments
        private static void ReplaceAwardInstruments(CommandEventArgs e)
        {
            int count = 0;
            e.Mobile.SendMessage("Replacing Award Instruments...");
#if false
            RehydrateWorld.RehydrateWorld_OnCommand(e);
#endif
            foreach (Item i in World.Items.Values)
            {
                if (i is AwardHarp || i is AwardLapHarp || i is AwardLute)
                {
                    RazorInstrument ax = i as RazorInstrument;
                    if (ax != null)
                    {   // new award instrument. this will be the replacement
                        AwardInstrument ai = new AwardInstrument();

                        switch (ax.ItemID)
                        {   // what are we replacing?
                            case 0xEB1:
                                ai.ConfigureInstrument("harp");
                                break;
                            case 0xEB2:
                                ai.ConfigureInstrument("lapharp");
                                break;
                            case 0xEB3:
                                ai.ConfigureInstrument("lute");
                                break;
                            default:    // unknown instrument type
                                ai.Delete();
                                continue;
                        }
                        // okay, copy over the other important info
                        ai.Place = ax.Place_obsolete;
                        ai.Year = ax.Year_obsolete;
                        ai.Author = ax.Author;
                        ai.SongFile = ax.SongFile;
                        ai.SongName = ax.SongName;
                        ReplaceItem(ai, ax); // replace old with new
                        ax.Delete();            // delete the old one
                        count++;
                    }
                }
            }
            e.Mobile.SendMessage(string.Format("{0} Award Instruments replaced.", count));
            e.Mobile.SendMessage("Done.");
        }
        #endregion Replace Award Instruments
        #region Patch RolledUpSheetMusic
        private static void PatchRolledUpSheetMusic(CommandEventArgs e)
        {
            int count = 0;
            e.Mobile.SendMessage("RolledUpSheetMusic Patching...");
#if false
            RehydrateWorld.RehydrateWorld_OnCommand(e);
#endif
            foreach (Item i in World.Items.Values)
            {
                if (i is RolledUpSheetMusic rsm && rsm != null /*&& rsm.HashCode == 0*/)
                {
                    if (rsm.Composition != null)
                    {
                        ManuscriptPaper mp = new ManuscriptPaper();
                        ManuscriptPaper.LocationTarget lt = new ManuscriptPaper.LocationTarget(mp);
                        rsm.HashCode = lt.CompileHashCode(rsm.Composition);
                    }
                    else
                    {   // things that don't have a Composition have a hash of zero
                        rsm.HashCode = 0;
                    }
                    count++;
                }
            }
            e.Mobile.SendMessage(string.Format("Patched {0} RolledUpSheetMusic.", count));
            e.Mobile.SendMessage("Done.");
        }
        #endregion Patch RolledUpSheetMusic
        #region GuildFealty
        // calculate guild fealty
        private static void GuildFealty(CommandEventArgs e)
        {
            try
            {
                e.Mobile.SendMessage("Calculating guild fealty...");
                int count = 0;
                foreach (Guild g in BaseGuild.List.Values)
                {
                    count++;
                    g.CalculateGuildmaster();
                }
                e.Mobile.SendMessage(string.Format("{0} guilds updated.", count));
                e.Mobile.SendMessage("Done.");
                return;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Console.WriteLine("Exception while running GuildFealty() job:");
                Console.WriteLine(ex);
                return;
            }
        }
        #endregion GuildFealty
        #region Break Follow
        private static void BreakFollow(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target playerto Break Follow...");
            e.Mobile.Target = new BreakFollowTarget(); // Call our target
        }

        public class BreakFollowTarget : Target // Create our targeting class (which we derive from the base target class)
        {
            public BreakFollowTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                Mobile followed = from;
                if (target is PlayerMobile follower)
                {
                    Network.NetState state = follower.NetState;

                    if (state.Mobile.CanSee(followed))
                    {

                        state.Send(followed.RemovePacket);      // you can't see me
                        state.Send(new ChangeUpdateRange(0));   // your update range is now zero

                        Timer.DelayCall(TimeSpan.FromSeconds(CoreAI.MultiBoxDelay), new TimerStateCallback(Tick), new object[] { state, followed });
                    }


                    from.SendMessage("Well?");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        private static void Tick(object state)
        {
            object[] aState = (object[])state;
            Network.NetState nstate = aState[0] as Network.NetState;
            Mobile followed = aState[1] as Mobile;
            Mobile follower = nstate.Mobile;
            nstate.Send(ChangeUpdateRange.Instantiate(18)); // your update range is restored
            nstate.Mobile.SendEverything();                 // you can now see everything
        }
        #endregion Break Follow
        #region Blank Scroll Patch
        private static void BlankScrollPatch(CommandEventArgs e)
        {
            int count = 0;
            e.Mobile.SendMessage("Blank Scroll Patching...");
            foreach (Item i in World.Items.Values)
            {
                // 0xE34 is the flipped blank scroll graphic
                // the old itemID 0xEF3 has magically odd behavior. 
                //  the odd behavior includes not being able to override OnDoubleClick. It's seems to be handled by the client.
                if (i is BlankScroll bs && bs != null && bs.ItemID == 0xEF3)
                {
                    count++;
                    LogHelper logger = new LogHelper("BlankScrollPatch.log", false, true);
                    logger.Log(LogType.Item, bs, "Old ID");
                    bs.ItemID = 0xE34;
                    logger.Log(LogType.Item, bs, "New ID");
                    logger.Finish();
                }

            }
            e.Mobile.SendMessage(string.Format("Patched {0} blank scrolls.", count));
            e.Mobile.SendMessage("Done.");
        }
        #endregion Blank Scroll Patch
        #region Tournament Of The Guardian Patch
        private static void TournamentOfTheGuardianPatch(CommandEventArgs e)
        {
            int count = 0;
            e.Mobile.SendMessage("Tournament Of The Guardian reward Patching...");
            foreach (Item i in World.Items.Values)
            {
                if (i.Name != null)
                    if (i.Name.ToLower().Contains("Tournament of the Guardian - fighter Dec, 26 - Garden Grove Arena".ToLower()) == true ||
                        i.Name.ToLower().Contains("Tournament of the Guardian - fighter - Dec. 12, 2021 - Council Tower".ToLower()) == true)
                    {
                        count++;
                        i.Hue = 1194;
                        LogHelper logger = new LogHelper("TournamentOfTheGuardianPatch.log", false, true);
                        logger.Log(LogType.Item, i, i.Name);
                        logger.Finish();
                    }

            }
            e.Mobile.SendMessage(string.Format("Patched {0} rewards.", count));
            e.Mobile.SendMessage("Done.");
        }
        #endregion Tournament Of The Guardian Patch
        #region Log ShareBank Participants
        private static void LogShareBankParticipants(CommandEventArgs e)
        {
            int count = 0;
            e.Mobile.SendMessage("Logging ShareBank Participants...");
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is PlayerMobile pm && pm != null && pm.Deleted == false)
                {
                    if (pm.ShareBank == true)
                    {
                        count++;
                        LogHelper logger = new LogHelper("ShareBank.log", false, true);
                        logger.Log(LogType.Mobile, pm, "is enrolled in bankbox sharing.");
                        logger.Finish();
                    }
                }
            }
            e.Mobile.SendMessage(string.Format("Logged {0} ShareBank Participants.", count));
            e.Mobile.SendMessage("Done.");
        }
        #endregion Log ShareBank Participants
        #region Fix Sea Charts
        private static void FixSeaCharts(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Patching bugged sea charts...");
#if false
            // first rehydrate world
            RehydrateWorld.RehydrateWorld_OnCommand(e);
#endif
            int count = 0;
            ushort[,] distances = SeaTreasure.CalculateDistanceMatrix();
            foreach (Item item in World.Items.Values)
            {
                if (item is SeaChart)
                {
                    SeaChart sc = (SeaChart)item;
                    if (sc.Level != 0 && !SeaTreasure.Validate(sc.Level, sc.GetWorldPins(), distances))
                    {
                        sc.SetLevel(sc.Level);
                        count++;
                    }
                }
            }
            e.Mobile.SendMessage(string.Format("Patched {0} sea charts...", count));
            e.Mobile.SendMessage("Done.");
        }
        #endregion
        #region Delete old Award Harps
        private static void DeleteAwardHarps(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Deleting old Award Harps...");
#if false
            // first rehydrate world
            RehydrateWorld.RehydrateWorld_OnCommand(e);
#endif
            int count = 0;
            List<Item> list = new List<Item>();
            // find 'em
            foreach (Item item in World.Items.Values)
            {
                if (item is AwardHarp)
                    list.Add(item);
            }
            // delete 'em
            foreach (Item item in list)
            {
                if (item.Parent != null && item.Parent is Container cont)
                    cont.RemoveItem(item);

                item.Delete();
                count++;
            }
            e.Mobile.SendMessage(string.Format("Deleted {0} Award Harps...", count));
            e.Mobile.SendMessage("Done.");
        }
        #endregion Delete old Award Harps
        #region Load DLL
        private static bool LoadDll(CommandEventArgs e)
        {
            string path = Path.Combine(Core.BaseDirectory, "Patches", "Scripts.dll");
            if (!File.Exists(path))
            {
                Console.WriteLine("Pre-Compiled Scripts.dll not found.");

            }
            return false;
        }
        #endregion Load DLL
        #region Kukui Nuts
        private static void KukuiNuts(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Replacing Kukui Nuts...");
#if false
            // first rehydrate world
            RehydrateWorld.RehydrateWorld_OnCommand(e);
#endif
            int count = 0;
            List<Item> list = new List<Item>();
            List<Item> toDeleteList = new List<Item>();
            // find 'em
            foreach (Item item in World.Items.Values)
            {
                if (item.ItemID == 0xF8B && item.Hue == 541 && item.Name == "a kukui nut" && item.GetType() == typeof(Item))
                    list.Add(item);
            }
            // patch 'em
            foreach (Item item in list)
            {
                Item new_item = new KukuiNut(item.Amount);
                ReplaceItem(new_item, item);
                new_item.Stackable = true;
                toDeleteList.Add(item);
                count++;
            }
            // housekeeping
            foreach (Item item in toDeleteList)
                item.Delete();
            e.Mobile.SendMessage(string.Format("Patched {0} Kukui Nuts...", count));
            e.Mobile.SendMessage("Done.");
        }
        #endregion
        #region Dragon Maturity
        // Fixes the maturity of all breedable dragons/basis that are incorrectly marked as ageless
        private static void DragonMaturity(CommandEventArgs e)
        {
            int count = 0;

            LogHelper logger = new LogHelper("DragonMaturity.log", false);

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is Dragon || m is Basilisk)
                {
                    BaseCreature bc = (BaseCreature)m;

                    if (bc.KukuiNuts == 3 && bc.BreedingParticipant && bc.Maturity == Engines.Breeding.Maturity.Ageless)
                    {
                        logger.Log(LogType.Mobile, bc);

                        bc.Birthdate = DateTime.UtcNow;
                        bc.Maturity = Engines.Breeding.Maturity.Adult;

                        count++;
                    }
                }
            }

            logger.Finish();

            e.Mobile.SendMessage(String.Format("Patched the maturity of {0} pets", count));
        }
        #endregion
        #region Replace Sealed Bows
        private static void ReplaceSealedBows(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Replacing Sealed Bows...");
#if false
            // first rehydrate world
            RehydrateWorld.RehydrateWorld_OnCommand(e);
#endif
            int count = 0;
            List<BaseRanged> list = new List<BaseRanged>();
            List<BaseRanged> toDeleteList = new List<BaseRanged>();
            // find 'em
            foreach (Item item in World.Items.Values)
            {
                if (item is BaseRanged bow)
                    list.Add(bow);
            }
            // patch 'em
            foreach (BaseRanged bow in list)
            {
                if (bow is SealedBow sb)
                {
                    Bow new_item = new Bow();
                    ReplaceItem(new_item, sb);
                    new_item.Name = "an ancient " + sb.GetRawOldName();
                    new_item.WaxCharges = 4096;
                    toDeleteList.Add(sb);
                    count++;
                }
                else if (bow is SealedCrossbow sc)
                {
                    Crossbow new_item = new Crossbow();
                    ReplaceItem(new_item, sc);
                    new_item.Name = "an ancient " + sc.GetRawOldName();
                    new_item.WaxCharges = 4096;
                    toDeleteList.Add(sc);
                    count++;
                }
                else if (bow is SealedHeavyCrossbow shc)
                {
                    HeavyCrossbow new_item = new HeavyCrossbow();
                    ReplaceItem(new_item, shc);
                    new_item.Name = "an ancient " + shc.GetRawOldName();
                    new_item.WaxCharges = 4096;
                    toDeleteList.Add(shc);
                    count++;
                }
            }

            // housekeeping
            foreach (BaseRanged br in toDeleteList)
                br.Delete();

            e.Mobile.SendMessage(string.Format("Patched {0} Sealed Bows...", count));
            e.Mobile.SendMessage("Done.");
        }
#if false
        public static void ReplaceItem(Item new_item, Item oldItem, bool copy_properties = true)
        {
            bool inContainer = false;
            bool isLockedDown = false;
            bool isSecure = false;
            bool notMovable = false;
            bool isHeld = false;
            bool isOneHanded = false;
            if (oldItem.Movable == false)
            { // handle locked down and secure 
                BaseHouse bh = BaseHouse.FindHouseAt(oldItem);
                if (bh.IsLockedDown(oldItem))
                    isLockedDown = true;
                else if (bh.IsSecure(oldItem))
                    isSecure = true;
                else
                    notMovable = true;
            }
            if (oldItem.Parent is Container)
                inContainer = true;
            if (oldItem.ParentContainer == null && oldItem.ParentMobile != null)
            {   // the player has this hammer equipped
                Item thing = oldItem.ParentMobile.FindItemOnLayer(Layer.OneHanded);
                if (thing == oldItem)
                    isOneHanded = isHeld = true;
                if (isHeld == false)
                    thing = oldItem.ParentMobile.FindItemOnLayer(Layer.TwoHanded);
                if (thing == oldItem)
                    isHeld = true;
            }
            // copy properties
            Dupe.CopyProperties(new_item, oldItem);
            if (inContainer)
            {   // in a container
                new_item.Parent = null;   // addItem won't add an item it thinks it already contains.
                (oldItem.Parent as Container).DropItem(new_item);
                new_item.Location = oldItem.Location;
                new_item.Map = oldItem.Map;
                (oldItem.Parent as Container).RemoveItem(oldItem);
            }
            else if (isHeld)
            {   // held in hand
                if (isOneHanded)
                    oldItem.ParentMobile.AddToBackpack(oldItem.ParentMobile.FindItemOnLayer(Layer.OneHanded));
                else
                    oldItem.ParentMobile.AddToBackpack(oldItem.ParentMobile.FindItemOnLayer(Layer.TwoHanded));
                oldItem.ParentMobile.EquipItem(new_item);
            }
            else if (isLockedDown)
            {   // locked down in a house
                BaseHouse bh = BaseHouse.FindHouseAt(oldItem);
                if (bh != null)
                {
                    new_item.Movable = true;          // was set false in copy properties
                    bh.SetLockdown(oldItem, false);  // unlock their old item
                    bh.SetLockdown(new_item, true);   // lock down the new item
                }
            }
            else if (isSecure)
            {
                // we're cool. You can't secure a single item, the house code just 'locks it down'
            }
            else if (notMovable)
            {
                // okay as it is .. it's already movable == false from copy properties
            }
            else
                new_item.MoveToWorld(oldItem.Location, oldItem.Map);
        }
#endif
        #endregion Replace Sealed Bows
        #region Patch Pet Stats
        // Fix pets that have undergone stat drop because of the Versatility gene
        private static void PatchPetStats(CommandEventArgs e)
        {
            int count = 0;

            LogHelper logger = new LogHelper("PatchPetStats.log", false);

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is Dragon || m is Basilisk) // only these mobs can be affected by stat drop
                {
                    BaseCreature bc = (BaseCreature)m;

                    if (bc.Controlled)
                    {
                        double statFactor = 0.0;

                        statFactor += (double)bc.RawStr / bc.StrMax;
                        statFactor += (double)bc.RawDex / bc.DexMax;
                        statFactor += (double)bc.RawInt / bc.IntMax;

                        if (statFactor < 0.69) // we've undergone a stat drop
                        {
                            logger.Log(LogType.Mobile, bc, String.Format("Str={0},Dex={1},Int={2}", bc.RawStr, bc.RawDex, bc.RawInt));

                            // we don't know what the pet's stats were before the drop, let's be generous and maximize them
                            bc.RawStr = bc.StrMax;
                            bc.RawDex = bc.DexMax;
                            bc.RawInt = bc.IntMax;

                            bc.Hits = bc.HitsMax;
                            bc.Stam = bc.StamMax;
                            bc.Mana = bc.ManaMax;

                            count++;
                        }
                    }
                }
            }

            logger.Finish();

            e.Mobile.SendMessage(String.Format("Patched the stats of {0} pets", count));
        }
        #endregion
        #region Patch Spawners For Level Six Chests
        private static void PatchSpawnersForLevelSixChests(CommandEventArgs e)
        {
            LogHelper logger = new LogHelper("L6Patchspawners.log", e.Mobile, false, true);
            logger.Log("Patching spawners that spawn Level 5 Dungeon Treasure Chests...");
            foreach (Spawner sx in SpawnerCache.Spawners)
            {
                bool foundL5 = false;
                bool foundL6 = false;
                if (sx == null || !(sx is Spawner)) continue;
                foreach (string name in sx.ObjectNames)
                {
                    if (name.ToLower() == "L5TreasureChest".ToLower())
                        foundL5 = true;
                    else if (name.ToLower() == "L6TreasureChest".ToLower())
                        foundL6 = true;

                }

                if (foundL5 && !foundL6)
                {   // we should add an L6 here
                    logger.Log(string.Format("Patching spawner at {0} and respawning.", sx.Location));
                    if (sx.HomeRange > 0)   // of we are spawning on top of the spawner, don't add another chest to the count.
                        sx.Count += 1;      // one more chest
                    sx.ObjectNamesRaw.Add("L6TreasureChest");
                    sx.Respawn();
                }
                else if (foundL5 && foundL6)
                {   // we should add an L6 here
                    logger.Log(string.Format("Skipping Patching spawner at {0}. Already has a Level 6 Chest.", sx.Location));
                }
            }
            logger.Log("Done.");
            logger.Finish();
        }
        #endregion Patch Spawners For Level Six Chests
        #region Price Check
        private static void PriceCheck(CommandEventArgs e)
        {

            if (e.Length == 2)
            {
                int ItemId = 0;
                string sx = e.GetString(1).ToLower();
                try
                {
                    if (sx.StartsWith("0x"))
                    {   // assume hex
                        sx = sx.Substring(2);
                        ItemId = int.Parse(sx, System.Globalization.NumberStyles.AllowHexSpecifier);
                    }
                    else
                    {   // assume decimal
                        ItemId = int.Parse(sx);
                    }
                }
                catch
                {
                    e.Mobile.SendMessage("Format: PriceCheck <ItemID>");
                    return;
                }

                List<int> list = new List<int> { ItemId };
                List<int> found = PriceCheckWorker(e, list);
                if (found.Count > 0)
                    e.Mobile.SendMessage("Found item 0x{0:X} in players possession.", ItemId);
                else
                    e.Mobile.SendMessage("Could not find any occurances of item 0x{0:X} in players possession.", ItemId);
            }
            else
                e.Mobile.SendMessage("Format: PriceCheck <ItemID>");
        }
        private static List<int> PriceCheckWorker(CommandEventArgs e, List<int> list)
        {
            LogHelper Logger = new LogHelper("PriceCheck.log", null, false, true);
            List<int> found = new List<int>();
            if (e != null)
            {
                // reset jump table
                Mobiles.PlayerMobile pm = e.Mobile as Mobiles.PlayerMobile;
                pm.JumpIndex = 0;
                pm.JumpList = new System.Collections.ArrayList();
                e.Mobile.SendMessage("Locating matching items...");
            }
#if false
            // first rehydrate world
            RehydrateWorld.RehydrateWorld_OnCommand(e);
#endif
            // find 'em
            foreach (Item item in World.Items.Values)
            {
                if (item == null)
                    continue;

                if (!(list.Contains(item.ItemID)))
                    continue;
                if (string.IsNullOrEmpty(item.Name))
                {   // if it has a 'name' then we can probably use it as our items are unnammed
                    if (BaseHouse.FindHouseAt(item) != null)
                    {
                        Logger.Log(LogType.Item, item);
                        if (!(found.Contains(item.ItemID)))
                            found.Add(item.ItemID);
                    }

                    if (item.RootParent != null && item.RootParent is PlayerMobile)
                    {
                        Logger.Log(LogType.Item, item);
                        if (!(found.Contains(item.ItemID)))
                            found.Add(item.ItemID);
                    }
                }
            }

            Logger.Finish();
            return found;
        }
        #endregion region Price Check
        #region an unusual key
        private static void UpdateAnUnusualKey(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Updating all Unusual Keys...");
#if false
            // first rehydrate world
            RehydrateWorld.RehydrateWorld_OnCommand(e);
#endif
            int count, redcount, bluecount, yellowcount;
            count = redcount = bluecount = yellowcount = 0;
            Key ironKey = new Key(KeyType.Iron);    // from this
            Key magicKey = new Key(KeyType.Magic);  // to this
            List<Key> list = new List<Key>();
            // find 'em
            foreach (Item item in World.Items.Values)
            {
                if (item is Key key && key.ItemID == ironKey.ItemID /*&& key.KeyValue == 0xDEADBEEF*/)
                    list.Add(key);
            }

            e.Mobile.SendMessage(string.Format("Found {0} Unusual Keys...", list.Count));

            // patch 'em
            foreach (Key key in list)
            {
                if (key == null || key.Deleted)
                    continue;
                count++;                        // another patched
                key.ItemID = magicKey.ItemID;   // new look!
                if (0.03 >= Utility.RandomDouble())
                {   // red key
                    redcount++;
                    key.Hue = 0x0668;
                    key.KeyValue = Loot.RedKeyValue;
                }
                else if (0.2 >= Utility.RandomDouble())
                {   // blue key
                    bluecount++;
                    key.Hue = 0x0546;
                    key.KeyValue = Loot.BlueKeyValue;
                }
                else
                {   // yellow key
                    yellowcount++;
                    key.Hue = 0x06A5;
                    key.KeyValue = Loot.YellowKeyValue;
                }
            }

            e.Mobile.SendMessage("{0} total Unusual Keys patched.", count);
            e.Mobile.SendMessage("{0} Unusual Red Keys created.", redcount);
            e.Mobile.SendMessage("{0} Unusual Blue Keys created.", bluecount);
            e.Mobile.SendMessage("{0} Unusual Yellow Keys created.", yellowcount);
            e.Mobile.SendMessage("Done.");
        }
        #endregion an unusual key
        #region ReplaceAncientSmithyHammers
        // Must execute from the map to 'ditch'
        private static void ReplaceAncientSmithyHammers(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Replacing all Ancient Hammers with Tenjin's Hammers...");
#if false
            // first rehydrate world
            RehydrateWorld.RehydrateWorld_OnCommand(e);
#endif
            List<AncientSmithyHammer> ash_list = new List<AncientSmithyHammer>();
            foreach (Item item in World.Items.Values)
            {
                if (item is AncientSmithyHammer ash)
                    ash_list.Add(ash);
            }

            foreach (AncientSmithyHammer ash in ash_list)
            {
                bool inContainer = false;
                bool isLockedDown = false;
                bool isSecure = false;
                bool notMovable = false;
                bool isHeld = false;
                if (ash.Movable == false)
                { // handle locked down and secure 
                    BaseHouse bh = BaseHouse.FindHouseAt(ash);
                    if (bh.IsLockedDown(ash))
                        isLockedDown = true;
                    else if (bh.IsSecure(ash))
                        isSecure = true;
                    else
                        notMovable = true;
                }
                if (ash.Parent is Container)
                    inContainer = true;
                if (ash.ParentContainer == null && ash.ParentMobile != null)
                {   // the player has this hammer equipped
                    Item thing = ash.ParentMobile.FindItemOnLayer(Layer.OneHanded);
                    if (thing == ash)
                        isHeld = true;
                }
                // copy properties
                Item hammer = new TenjinsHammer();
                string save_name = hammer.Name;
                int save_hue = hammer.Hue;
                Utility.CopyProperties(hammer, ash);
                hammer.Name = save_name;
                hammer.Hue = save_hue;
                if (inContainer)
                {   // in a container
                    hammer.Parent = null;   // add item won't add an item it thinks it already contains.
                    (ash.Parent as Container).DropItem(hammer);
                    hammer.Location = ash.Location;
                    hammer.Map = ash.Map;
                }
                else if (isHeld)
                {   // held in hand
                    ash.ParentMobile.AddToBackpack(ash.ParentMobile.FindItemOnLayer(Layer.OneHanded));
                    ash.ParentMobile.EquipItem(hammer);
                }
                else if (isLockedDown)
                {   // locked down in a house
                    BaseHouse bh = BaseHouse.FindHouseAt(ash);
                    if (bh != null)
                    {
                        hammer.Movable = true;          // was set false in copy properties
                        bh.SetLockdown(ash, false);     // unlock their ash
                        bh.SetLockdown(hammer, true);   // lock down the new hammer
                    }
                }
                else if (isSecure)
                {
                    // we're cool. You can't secure a single item, the house code just 'locks it down'
                }
                else if (notMovable)
                {
                    // okay as it is .. it's already movable == false from copy properties
                }
                else
                    hammer.MoveToWorld(ash.Location, ash.Map);

                ash.Delete();
            }
        }
        #endregion ReplaceAncientSmithyHammers
        #region Renewbie
        private static void Renewbie(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Re Newbing GM clothes and daggers...");
#if false
            // first rehydrate world
            RehydrateWorld.RehydrateWorld_OnCommand(e);
#endif
            int count = 0;
            foreach (Item item in World.Items.Values)
            {
                if (item is Dagger && !(item is BasilisksFang) && !(item is AIStinger))
                {   // newbie all daggers
                    if (item.LootType == LootType.Regular)
                    {
                        item.LootType = LootType.Newbied;
                        count++;
                    }
                    continue;
                }

                if (item is AIStinger)
                {   // AIStingers are 'Rare'
                    if (item.LootType == LootType.Regular)
                    {
                        item.LootType = LootType.Rare;
                        count++;
                    }
                    continue;
                }

                if (item is Spellbook)
                {   // Return Spellbooks to being newbied
                    if (item.LootType == LootType.Regular)
                    {
                        item.LootType = LootType.Newbied;
                        count++;
                    }
                    continue;
                }

                if (item is BaseClothing bc)
                {   // newbie all GM clothes and Shrouds
                    if (bc.Quality == ClothingQuality.Exceptional /*|| bc is HoodedShroudOfShadows*/)
                        if (item.LootType == LootType.Regular)
                        {
                            item.LootType = LootType.Newbied;
                            count++;
                        }
                    continue;
                }
            }

            e.Mobile.SendMessage("{0} GM clothes and daggers re newbied.", count);
        }
        #endregion Renewbie
        #region KillAES
        private static void KillAES(CommandEventArgs e)
        {
            Console.WriteLine("Killing All AES events...");
            int count = 0;
            foreach (Item item in World.Items.Values)
            {
                if (item is AutomatedEventSystem aes)
                {   // setting this will cause the AES to cleanup and delete itself.
                    aes.ValidState = false;
                    count++;
                }
            }

            Console.WriteLine("{0} AES events killed.", count);
        }
        #endregion KillAES
        #region DitchBritGuards
        // Must execute from the map to 'ditch'
        private static void DitchBritGuards(CommandEventArgs e)
        {
            if (e.Arguments.Length < 2 || (!e.Arguments[1].ToLower().Contains("hide") && !e.Arguments[1].ToLower().Contains("unhide")))
            {
                e.Mobile.SendMessage("Usage: Nuke DitchBritGuards [hide|unhide]");
                return;
            }

            bool hide = false;
            bool unhide = false;
            if (e.Arguments[1].ToLower().Contains("unhide"))
                unhide = true;
            else if (e.Arguments[1].ToLower().Contains("hide"))
                hide = true;

            Console.WriteLine("Ditching Brit Guards...");
            List<Mobile> toTouch = new List<Mobile>();
            foreach (Mobile mob in World.Mobiles.Values)
            {
                if (mob is BaseGuard)
                {
                    if (hide && mob.Map == e.Mobile.Map)
                        if (mob.Region.Name == "Britain" || mob is BritannianRanger)
                        {
                            toTouch.Add(mob);
                            continue;
                        }

                    if (unhide && mob.Map == Map.Internal)
                    {
                        toTouch.Add(mob);
                        continue;
                    }
                }
            }
            // ditch these guards
            foreach (Mobile mob in toTouch)
            {
                mob.Map = (hide) ? Map.Internal : e.Mobile.Map;
            }

            Console.WriteLine("The Ditching of Brit Guards complete with {0} guards ditched.", toTouch.Count);
        }
        #endregion DitchBritGuards
        #region StablePets
        public class Pair
        {
            public BaseCreature m_bc;
            public Mobile m_master;
            public Pair(BaseCreature bc, Mobile master)
            {
                m_bc = bc;
                m_master = master;
            }
        }
        private static void StablePets(CommandEventArgs e)
        {
            Console.WriteLine("Stabling abandoned pets...");
            List<Pair> stable = new List<Pair>();
            foreach (Mobile mob in World.Mobiles.Values)
            {
                BaseCreature bc = mob as BaseCreature;
                if (bc != null &&
                    bc.Controlled &&
                    bc.ControlMaster != null &&
                    bc.ControlMaster.Player &&
                    (bc.ControlMaster.Map != bc.Map || !bc.ControlMaster.InRange(bc, 26)) &&
                    Utility.CanStablePet(bc))
                {
                    stable.Add(new Pair(bc, bc.ControlMaster as PlayerMobile));
                }
            }
            // stable these pets
            foreach (Pair pair in stable)
            {
                if (pair.m_bc.IsDeadBondedPet)
                    pair.m_bc.ResurrectPet();
                // stable pet and charge
                Utility.StablePet(pair.m_master, pair.m_bc);
                Utility.ChargeStableFee(pair.m_master, pair.m_bc);
            }
            Console.WriteLine("Stabling complete with {0} pets stabled.", stable.Count);
        }
        #endregion StablePets
        #region LogElementals
        private static void LogElementals(CommandEventArgs e)
        {
            LogHelper logger = new LogHelper("Elemental.log", false);
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null) return;

            // reset jump table
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            ArrayList list = new ArrayList();

            foreach (Mobile mob in World.Mobiles.Values)
            {
                Type t = mob.GetType();
                if (t.ToString().ToLower().Contains("oreelemental"))
                {
                    pm.JumpList.Add(mob);
                    list.Add(logger.Format(LogType.Mobile, mob));
                }

            }

            list.Sort();
            foreach (string s in list)
                logger.Log(LogType.Text, s);

            logger.Finish();


        }
        #endregion LogElementals
        #region CampPlacement
        private static void CampPlacement(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the miner (PlayerMobile) to begin the search...");
            e.Mobile.Target = new LocationTarget(); // Call our target
        }

        public class LocationTarget : Target // Create our targeting class (which we derive from the base target class)
        {
            public LocationTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                Point3D location;
                PlayerMobile miner = null;
                if (target is PlayerMobile)
                {
                    location = (target as PlayerMobile).Location;
                    miner = (target as PlayerMobile);
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }

                System.Console.WriteLine("CampPlacement started ... ");
                Utility.TimeCheck tc = new Utility.TimeCheck();
                tc.Start();
                CReceiveMessage.BuildAngryMinerCamp(miner, location, "AngryMinerCamp", "ValoriteOre", 4);
                tc.End();
                System.Console.WriteLine("CampPlacement finished in {0}", tc.TimeTaken);
            }
        }
        #endregion CampPlacement
        #region KeysOnTheOcean
        private static void KeysOnTheOcean(CommandEventArgs e)
        {

            if (e.Arguments.Length < 2 || (!e.Arguments[1].ToLower().Contains("info") && !e.Arguments[1].ToLower().Contains("remove")))
            {
                e.Mobile.SendMessage("Usage: Nuke KeysOnTheOcean [info|remove]");
                return;
            }

            bool info = e.Arguments[1].ToLower().Contains("info");
            bool remove = e.Arguments[1].ToLower().Contains("remove");

            int iChecked = 0;
            int iFound_felucca = 0;
            int iFound_other = 0;
            int iRepaired = 0;
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            List<Key> remove_list = new List<Key>();
            List<Key> repaired_list = new List<Key>();
            List<KeyRing> keyRings = new List<KeyRing>();
            foreach (Item item in World.Items.Values)
                if (item is KeyRing)
                    keyRings.Add(item as KeyRing);

            e.Mobile.SendMessage("cleaning up 'keys on the ocean'");
            foreach (Item item in World.Items.Values)
            {
                iChecked++;
                if (item is Key)
                {
                    Key key = item as Key;
                    if (key.Parent == null)
                    {
                        if (BaseHouse.FindHouseAt(key) == null)
                        {
                            if (key.Map == Map.Felucca)
                                iFound_felucca++;
                            else
                                iFound_other++;

                            if (info)
                                pm.JumpList.Add(key);
                            if (remove)
                                remove_list.Add(key);
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
                    }

            foreach (Key key in repaired_list)
                remove_list.Remove(key);

            if (remove)
                foreach (Key key in remove_list)
                    key.Delete();

            e.Mobile.SendMessage(string.Format("{3} {0} orphaned keys, {1} of which are in Felucca, and {2} of which are on other maps.",
                (iFound_other + iFound_felucca) - (info ? 0 : iRepaired), iFound_felucca, iFound_other, info ? "Found" : "Removed"));

            e.Mobile.SendMessage(string.Format("{0} key/keyrings repaired.", iRepaired));
        }
        #endregion KeysOnTheOcean
    }
}