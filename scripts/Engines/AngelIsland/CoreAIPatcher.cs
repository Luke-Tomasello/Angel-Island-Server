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
using System.Xml;
namespace Server
{

    public partial class CoreAI
    {
        // Patch named index table
        public enum PatchIndex
        {
            #region IMPORTED FROM PatchBits - DO NOT EDIT
            None = 0x00,    // special used for Always Run
            HasPatchedFreezeDry, // turn off freeze dry globally
            HasPatchedGUIDs, // patch the GUIDs of all players
            HasPatchedHouseCodes, // patch the unique House Codes of all houses
            HasPatchedTime, // convert to using UTC for our saved time format
            HasCategorizedSpawners, // patch spawners to set the shard specific config. see comments in Spawner.cs
            HasPatchedStandardShardSpawners, // Turn off all spawners tagged as Angel Island 'special' (also turns off for Siege.)
            HasActivatedCamps, // activate 'camps'. Orc, Lizardman, etc.
            HasTotalRespawn, // if we are touching (adding/changing) spawners, conduct a total respawn of the world
            HasPatchedBirds, // after HasClassicSpawned gets set (except for Angel Island,) this patch can run to spawn birds 
            HasPatchedMoonglowZooAI, // Angel Island: Put all the blessed creatures on a spawner
            HasPatchedVisibleSpawners, // turn off visible debug spawners
            HasPatchedCamps, // reset the respawn delay to like 25-40 minutes
            HasPatchedStaticRegions, // patch the static regions (see notes in Patcher.HasPatchedStaticRegions()
            HasPatchedNPSACoords, // patch the new player starting area to have the correct coords, and to set map to Trammel
            HasPatchedOutChamps, // remove standard OSI champ spawners.
            HasPatchedIllegalTrammelItems, // remove about 17K unneeded items.
            HasPatchedFeluccaDeco, // renew Felucca deco for Siege and Mortalis.
            HasPatchedLostLandsAccess, // turn off teleporters that implement "recdu", "recsu" and "sueacron", "doracron"
            HasPatchedFeluccaTeleporters, // Has patched Island Siege Felucca Teleporters
            HasPatchedAIFeluccaTeleporters, // Has patched Angel Island Felucca Teleporters
            HasPatchedConPvPTeleporters, // fix the ConPvPTeleporters, location, 
            HasPatchedVampChampTeleporters, // fix the vampire teleporter's Z
            HasPatchedGreenAcresTeleporters, // fix the Green Acres, location, add signs, convert moongate to teleporter
            HasInitializedNewShard, // wipe all accounts, configure Allowed IP Addresses, characters per acct, Zora, Coops, etc.
            HasRestoredPremierGems, // wipe stable, restore Premier Gems (Siege)
            HasDeletedForgeBehindWBB, // wipe the forge and deco behind WBB (Siege)
            HasFixedDoors_1133_2237_40, // fix the broken doors at 1133 2237 40
            HasRemovedOcCastle, // remove the unused event castle behind Oc'Nivelle
            HasValidatedTeleporterPatchLists, // proofread/filter and normalize patchlists
            HasPatchedTilesV1, // add/remove map tiles - version 1
            HasPatchedInClasicRespawn, // respawn Siege shards with classic spawn (disables other spawners)
            HasWipedContainerCache, // wipe the unusual container cache in anticipation of 'decoration' unexpectedly deleting these containers.
            HasRebuiltContainerCache, // rebuild the unusual container cache
            HasDisabledIOBRegions, // Siege: disable all IOB regions
            HasPatchedDestinationOverride, // While old moongates had a notion of DestinationOverride, teleporters did not. Set all existing to true
            HasPatchedMoongates, // Replace all staff placed Moongates with the new Sungates
            HasPatchedEventTeleporters, // Replace all staff placed 'event' teleporters with the new EventTeleporters
            HasPatchedAIMoongates, // MoonGen doesn't (didn't) remove old moongates before placing a new.
            HasWipedKinRansomChests, // Siege: wipe all kin ransom chests.
            HasPatchedBlessedToInvulnerable, // All shards: Replace all blessed creatures with 'Exhibit' creatures 
            HasEnabledSpecialSpawners, // Siege: Enable certain spawners for all shards: Adam and Jade for instance
            HasRemovedBlessed, // All Shards: Remove all blessed creatures not on a spawner
            HasUpdatedMoonglowZoo, // All Shards. Wipe the mobiles and set the Spawner to 'Exhibit' mode and lock the doors. For siege, remove the signs.
            HasPatchedPatchHistory, // All Shards. recored patches applied before we added the patch recorder.
            HasPatchedMoonglowZooStandardShards, // Siege: after HasClassicSpawned gets set, this patch can run to 'bless' creatures at Moonglow zoo
            HasPatchedBowyer, // Replace all spawner "Bower" with "Bowyer"
            HasPatchedTemplateSpawners, // patch template spawners that spawn a BaseVendor to be 'invulnerable'
            HasPatchedVendorFees, // turn on vendor fees across all shards
            HasCategorizedOtherSpawners, // patch spawners to set the shard specific config. see comments in Spawner.cs
            HasPatchedChickenFight, // Remove a T2A spawner left in the 'chicken fight' tunnel
            HasRepatchedMoonglowZoo, // Rerun the Moonglow 'update patch' for non AI shards
            HasDeletedNerunSpawners, // delete the spawners for the "Sea Market" out in the middle of the ocean
            HasPatchedAICamps, // Restoring AI's camps to quicker respawn. Not standard, but not as fast at AICore 4
            HasPatchedDungeonTeleporters, // The new map floor is a bit higher than the old when the teleporters were placed. We need to raise the teles
            HasPatchedDungeonTeleporters2, // The new map floor again / placement. Adjust.
            HasFixedDoorsV2, // more doors need fixing
            HasResetChamps, // Champs are in a mixed state, running, but restart timer stopped
            HasPatchedMachineInfo, // Accounts now contain a 'list-of-machine' hashes. Add the current hash to the list
            HasRefreshedStaticRegions, // redo the static region patch
            HasPatchedRegionsControllerID, // change the graphic ID
            HasRunGeneralCleanupV1, // turn on jail gate, prison spawners, moongate wizards
            HasPatchedPatrolGuardSpawners, // turn on Patrol Guards
            HasRefreshedStaticRegionsV2, // redo the static region patch again (staticregions.xml was missing from Data folder.)
            HasRefreshedStaticRegionsV3, // one more time!
            #endregion IMPORTED FROM PatchBits - DO NOT EDIT
            /* Add your patch IDs here, always add at the end, but before __last */
            PatcherTableTest,           // test the new patcher table
            UpdatedRegionControllersV1, // patch region controller name/location information
            BetaReadyV1,                // first BETA ready patch (turn off champs, hide spawners,..)
            BetaReadyV2,
            BetaReadyV3,
            BetaReadyV4,
            BetaReadyV5,
            BetaReadyV6,
            BetaReadyV7,
            BetaReadyV8,
            BetaReadyV9,
            BetaReadyV10,
            BetaReadyV11,
            BetaReadyV12,
            BetaReadyV13,
            BetaReadyV14,
            BetaReadyV15,
            BetaReadyV16,
            BetaReadyV17,
            BetaReadyV18,
            BetaReadyV19,
            BetaReadyV20,
            BetaReadyV21,
            PatchV22,
            PatchV23,
            PatchV24,
            PatchV25,
            PatchV26,
            PatchV27,
            PatchV28,
            PatchV29,
            PatchV30,
            PatchV31,
            PatchV32,
            PatchV33,
            PatchV34,
            PatchV35,
            PatchV36,
            PatchV37,
            PatchV38,
            PatchV39,
            PatchV40,
            PatchV41,
            PatchV42,
            PatchV43,
            PatchV44,
            PatchV45,
            PatchV46,
            PatchV47,
            PatchV48,
            PatchV49,
            PatchV50,
            PatchV51,
            PatchV52,
            PatchV53,
            PatchV54,
            PatchV55,
            PatchV56,
            PatchV57,
            PatchV58,
            PatchV59,
            PatchV60,
            PatchV61,
            PatchV62,
            PatchV63,
            PatchV64,
            PatchV65,
            PatchV66,
            PatchV67,
            PatchV68,
            PatchV69,
            PatchV70,
            PatchV71,
            PatchV72,
            PatchV73,
            PatchV74,
            PatchV75,
            PatchV76,
            PatchV77,
            PatchV78,
            PatchV79,
            PatchV80,
            PatchV81,
            PatchV82,
            PatchV83,
            PatchV84,
            PatchV85,
            PatchV86,
            PatchV87,
            PatchV88,
            PatchV89,
            PatchV90,
            PatchV91,
            PatchV92,
            PatchV93,
            PatchV94,
            PatchV95,
            PatchV96,
            PatchV97,
            PatchV98,
            PatchV99,
            PatchV100,
            PatchV101,
            PatchV102,
            PatchV103,
            PatchV104,
            PatchV105,
            PatchV106,
            PatchV107,
            PatchV108,
            PatchV109,
            PatchV110,
            __last
        }
        // patch table
        public static bool[] PatchTable = new bool[(int)PatchIndex.__last];

        #region Patch API
        #region NEW Patch Table model
        public static bool IsDynamicPatchSet(PatchIndex pb) { return PatchTable[(int)pb]; }
        public static void SetDynamicPatch(PatchIndex pb) { PatchTable[(int)pb] = true; }
        public static void ClearDynamicPatch(PatchIndex pb) { PatchTable[(int)pb] = false; }
        #endregion NEW Patch Table model
        #region OLD Patch Bits model
        // converter: one time patch to convert old 64bit enum values to new patch table format
        public static void Populate()
        {
            int index = 0;
            foreach (var val in Enum.GetValues(typeof(PatchBits)))
                PatchTable[index++] = IsDynamicPatchSet((PatchBits)val);
        }
        [Obsolete]
        [Flags]
        public enum PatchBits : UInt64
        {
            #region TABLE FULL - DO NOT EDIT
            // see usage in Initialize() in Server/Patcher.cs
            // since all of this will be initially false, we need to name them to indicate 'false' means the task needs doing
            None = 0x00,
            HasPatchedFreezeDry = 0x01, // turn off freeze dry globally
            HasPatchedGUIDs = 0x02, // patch the GUIDs of all players
            HasPatchedHouseCodes = 0x04, // patch the unique House Codes of all houses
            HasPatchedTime = 0x08, // convert to using UTC for our saved time format
            HasCategorizedSpawners = 0x10, // patch spawners to set the shard specific config. see comments in Spawner.cs
            HasPatchedStandardShardSpawners = 0x20, // Turn off all spawners tagged as Angel Island 'special' (also turns off for Siege.)
            HasActivatedCamps = 0x40, // activate 'camps'. Orc, Lizardman, etc.
            HasTotalRespawn = 0x80, // if we are touching (adding/changing) spawners, conduct a total respawn of the world
            HasPatchedBirds = 0x100, // after HasClassicSpawned gets set (except for Angel Island,) this patch can run to spawn birds 
            HasPatchedMoonglowZooAI = 0x200, // Angel Island: Put all the blessed creatures on a spawner
            HasPatchedVisibleSpawners = 0x400, // turn off visible debug spawners
            HasPatchedCamps = 0x800, // reset the respawn delay to like 25-40 minutes
            HasPatchedStaticRegions = 0x1000, // patch the static regions (see notes in Patcher.HasPatchedStaticRegions()
            HasPatchedNPSACoords = 0x2000, // patch the new player starting area to have the correct coords, and to set map to Trammel
            HasPatchedOutChamps = 0x4000, // remove standard OSI champ spawners.
            HasPatchedIllegalTrammelItems = 0x8000, // remove about 17K unneeded items.
            HasPatchedFeluccaDeco = 0x10000, // renew Felucca deco for Siege and Mortalis.
            HasPatchedLostLandsAccess = 0x20000, // turn off teleporters that implement "recdu", "recsu" and "sueacron", "doracron"
            HasPatchedFeluccaTeleporters = 0x40000, // Has patched Island Siege Felucca Teleporters
            HasPatchedAIFeluccaTeleporters = 0x80000, // Has patched Angel Island Felucca Teleporters
            HasPatchedConPvPTeleporters = 0x100000, // fix the ConPvPTeleporters, location, 
            HasPatchedVampChampTeleporters = 0x200000, // fix the vampire teleporter's Z
            HasPatchedGreenAcresTeleporters = 0x400000, // fix the Green Acres, location, add signs, convert moongate to teleporter
            HasInitializedNewShard = 0x800000, // wipe all accounts, configure Allowed IP Addresses, characters per acct, Zora, Coops, etc.
            HasRestoredPremierGems = 0x1000000, // wipe stable, restore Premier Gems (Siege)
            HasDeletedForgeBehindWBB = 0x2000000, // wipe the forge and deco behind WBB (Siege)
            HasFixedDoors_1133_2237_40 = 0x4000000, // fix the broken doors at 1133 2237 40
            HasRemovedOcCastle = 0x8000000, // remove the unused event castle behind Oc'Nivelle
            HasValidatedTeleporterPatchLists = 0x10000000, // proofread/filter and normalize patchlists
            HasPatchedTilesV1 = 0x20000000, // add/remove map tiles - version 1
            HasPatchedInClasicRespawn = 0x40000000, // respawn Siege shards with classic spawn (disables other spawners)
            HasWipedContainerCache = 0x80000000, // wipe the unusual container cache in anticipation of 'decoration' unexpectedly deleting these containers.
            HasRebuiltContainerCache = 0x100000000, // rebuild the unusual container cache
            HasDisabledIOBRegions = 0x200000000, // Siege: disable all IOB regions
            HasPatchedDestinationOverride = 0x400000000, // While old moongates had a notion of DestinationOverride, teleporters did not. Set all existing to true
            HasPatchedMoongates = 0x800000000, // Replace all staff placed Moongates with the new Sungates
            HasPatchedEventTeleporters = 0x1000000000, // Replace all staff placed 'event' teleporters with the new EventTeleporters
            HasPatchedAIMoongates = 0x2000000000, // MoonGen doesn't (didn't) remove old moongates before placing a new.
            HasWipedKinRansomChests = 0x4000000000, // Siege: wipe all kin ransom chests.
            HasPatchedBlessedToInvulnerable = 0x8000000000, // All shards: Replace all blessed creatures with 'Exhibit' creatures 
            HasEnabledSpecialSpawners = 0x10000000000, // Siege: Enable certain spawners for all shards: Adam and Jade for instance
            HasRemovedBlessed = 0x20000000000, // All Shards: Remove all blessed creatures not on a spawner
            HasUpdatedMoonglowZoo = 0x40000000000, // All Shards. Wipe the mobiles and set the Spawner to 'Exhibit' mode and lock the doors. For siege, remove the signs.
            HasPatchedPatchHistory = 0x80000000000, // All Shards. recored patches applied before we added the patch recorder.
            HasPatchedMoonglowZooStandardShards = 0x100000000000, // Siege: after HasClassicSpawned gets set, this patch can run to 'bless' creatures at Moonglow zoo
            HasPatchedBowyer = 0x200000000000, // Replace all spawner "Bower" with "Bowyer"
            HasPatchedTemplateSpawners = 0x400000000000, // patch template spawners that spawn a BaseVendor to be 'invulnerable'
            HasPatchedVendorFees = 0x800000000000, // turn on vendor fees across all shards
            HasCategorizedOtherSpawners = 0x1000000000000, // patch spawners to set the shard specific config. see comments in Spawner.cs
            HasPatchedChickenFight = 0x2000000000000, // Remove a T2A spawner left in the 'chicken fight' tunnel
            HasRepatchedMoonglowZoo = 0x4000000000000, // Rerun the Moonglow 'update patch' for non AI shards
            HasDeletedNerunSpawners = 0x8000000000000, // delete the spawners for the "Sea Market" out in the middle of the ocean
            HasPatchedAICamps = 0x10000000000000, // Restoring AI's camps to quicker respawn. Not standard, but not as fast at AICore 4
            HasPatchedDungeonTeleporters = 0x20000000000000, // The new map floor is a bit higher than the old when the teleporters were placed. We need to raise the teles
            HasPatchedDungeonTeleporters2 = 0x40000000000000, // The new map floor again / placement. Adjust.
            HasFixedDoorsV2 = 0x80000000000000, // more doors need fixing
            HasResetChamps = 0x100000000000000, // Champs are in a mixed state, running, but restart timer stopped
            HasPatchedMachineInfo = 0x200000000000000, // Accounts now contain a 'list-of-machine' hashes. Add the current hash to the list
            HasRefreshedStaticRegions = 0x400000000000000, // redo the static region patch
            HasPatchedRegionsControllerID = 0x800000000000000, // change the graphic ID
            HasRunGeneralCleanupV1 = 0x1000000000000000, // turn on jail gate, prison spawners, moongate wizards
            HasPatchedPatrolGuardSpawners = 0x2000000000000000, // turn on Patrol Guards
            HasRefreshedStaticRegionsV2 = 0x4000000000000000, // redo the static region patch again (staticregions.xml was missing from Data folder.)
            HasRefreshedStaticRegionsV3 = 0x8000000000000000, // one more time!
            #endregion TABLE FULL - DO NOT EDIT
        }
        [Obsolete] /* only used internally one-time during conversion to new table */
        private static bool IsDynamicPatchSet(PatchBits pb) { if ((DynamicPatches & (UInt64)pb) > 0) return true; else return false; }
        [Obsolete]
        private static void SetDynamicPatch(PatchBits pb) { DynamicPatches |= (UInt64)pb; }
        [Obsolete]
        private static void ClearDynamicPatch(PatchBits pb) { DynamicPatches &= ~((UInt64)pb); }
        #endregion OLD Patch Bits model
        #endregion Patch API
        public static void LoadPatchTable(XmlElement node)
        {
            XmlElement patchTable = node["patchTable"];
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
        public static bool GetBool(XmlElement node, bool defaultValue)
        {
            if (node == null)
                return defaultValue;

            string strVal = GetText(node, defaultValue.ToString());
            if (String.Equals(strVal, "true", StringComparison.OrdinalIgnoreCase)) return true;
            else if (String.Equals(strVal, "false", StringComparison.OrdinalIgnoreCase)) return false;
            else return defaultValue;
        }

        public static void Initialize()
        {
            Server.CommandSystem.Register("PatchLevel", AccessLevel.Administrator, new CommandEventHandler(PatchLevel));
        }
        [Usage("PatchLevel")]
        [Description("Display the current Patch Level.")]
        public static void PatchLevel(CommandEventArgs e)
        {
            try
            {
                int patchLevel = 0;
                for (int ix = 0; ix < PatchTable.Length; ix++)
                    if (PatchTable[ix])
                        patchLevel++;
                    else
                        break;

                /* Indexes explained:
                 * Our old patch table was a uint64 which housed 64 bits representing 64 patches.
                 * There was also a 'None=0x00' patch. 
                 * Our new bool table reserves those 64 bits (now bools) + 1 for for the 'None'. Added to the 64 bits gives us 65 
                 * We also had 2 initial patches under the new bool table approach, we'll call those reserved.
                 * So 65 + 2 == 67.
                 * The real patch level is therefore patchLevel - (64 + 1 + 2)
                 */

                int realPatchLevel = patchLevel - (64 + 1 + 2);
                // we add two here since the first two indices are reserved
                e.Mobile.SendMessage("The shard is at patch level {0}", realPatchLevel);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
    }
}