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

/* Engines/AngelIsland/CoreAI.cs
 * ChangeLog
 * 5/15/23, Yoar (BolaThrowLock, BolaMountLock)
 * Added configurable bola lock delays
 * 5/10/23, Yoar (PJUMBountyCap)
 * Added PJUMBountyCap
 * 5/6/23, Yoar (AreaPeaceCrowdSize, AreaPeaceSoloScalar, AreaPeaceDelayBase, AreaPeaceDelayBonus)
 * Added configurable area peace delays
 * 5/1/23, Yoar (CharDeleteUnrestricted)
 * Added CharDeleteUnrestricted: Toggle on/off char deletion restriction
 * 4/26/23, Yoar (HitPoisonChanceIncr)
 * Added HitPoisonChanceIncr - this is the probability increment for landing weapon poison at GM poisoning
 * 4/23/23, Yoar (ChatEnabled)
 * Added switch to enable the client chat system
 * 3/10/22, Yoar
 * Renamed StaminaDrainScaling to StaminaDrainByWeight
 * 9/22/22, Adam (HasPatchedCamps)
 * Add patch for camps - reset the respawn delay to like 25-40 minutes (was 3-4 hours)
 * I'm guessing here. OSI has the 'decay' time os a camp at 30 minutes, so I'm guessing they should respawn withing the next ~30 minutes.
 * 9/7/22, Adam (HasPatchedVisibleSpawners)
 * Add new patch to turn the visible spawners invisible
 * 8/26/22, Adam (SiegeBless)
 * Add a switch for SiegeBless
 * 8/25/22, Adam (HasPatchedSpawnerAttrib)
 * For Mortalis/Siege, we want to categorize all spawners as Core, Angel Island Only, etc. so that Mortalis/Siege doesn't get a bunch of weirdo mobiles in the world.
 * 8/23/22, Adam (HasPatchedSpawner)
 * 1. patch spawners to set the shard specific config. see comments in Spawner.cs
 * 2. HasPatchedMOSpawners - Turn off all spawners tagged as Angel Island 'special' (also turns off for Siege.)
 * 8/22/22, Adam
 * Change console output from yellow to green (informational)
 * 8/17/22, Adam (HasPatchedTime)
 * 1. Add HasPatchedTime to PatchBits
 * this flag is used to patch the way we read time values. (first world load only)
 * The saves of time values have already been replaced to 'save to UTC', but the reading expects local time.
 * After the world is loaded the first time, HasPatchedTime is cleared so that all subsequent reads will read UTC
 * 2. this file (CoreAI.xml) must load before the world because of the patchbits.
 * replaced EventSink.WorldLoad += WorldLoadEventHandler(OnLoad) => EventSink.PreWorldLoad += new PreWorldLoadEventHandler(OnLoad)
 * This ensures CoreAI.xml is read before any world files (mobiles, items, etc.)
 * 8/16/22, Adam (HasPatchedHouseCodes)
 * Add a new PatchBits flag HasPatchedHouseCodess.
 * We updated the AccountCode associated with a house and this patch initializes all houses with the new code.
 * 8/15/22, Adam (HasPatchedGUIDs)
 * Add a new PatchBits flag HasPatchedGUIDs.
 * When the server is launched (main.cs) the patcher will run on all mobiles and patch the GUIDS.
 * See comments in main.cs for more info.
 * 8/12/22, Adam
 * Add PatchBits. 
 * This enum specifies the one time startup patches to be applied (usually) in main.cs in Initialize
 * Once the patch is applied, the 'patched' bit is set, and it is never run again.
 * 8/9/22, Adam
 *		Make FeatureBits uint64: to 0x80000000 and beyond!
 *		Add MagicGearThrottleEnabled // do we Throttle magic gear?
 *		MagicCraftSystem // magic craft system enabled 
 *		ZoraSystem // zora enabled
 * 8/1/22, Adam
 * Add MaxAccountsPerMachine: in HardwareLimiter; controls how many accounts may be concurrently logged in on the same machine
 * defaults to MaxAccountsPerIP
 * 6/27/22: Yoar
 * Added CoreAI.BaseVendorFee: Base player vendor fee per one UO day (one UO day = 2 RL hours).
 * 4/29/22, Adam (RestockCharges)
 * Add a bool to the Vendor Management Console to determine if RestockCharges should apply.
 * 11/30/21: Yoar
 * Removed the MobsThrowSnowballs bit. Only x-mas elfs may throw snowballs.
 * Added a ShipTracking bit: Enable/disable the ship tracking system.
 * 11/26/21: Yoar
 * Added a MobsThrowSnowballs bit: Allow mobs to throw snowballs *if* they have a SnowPile in their pack.
 * 11/25/21: Yoar
 * Added a StaminaDrainScaling bit
 * 11/08/21: Yoar
 * Added DungeonChestBuryRate
 * 10/28/21, Yoar
 * Moved GSGG to SkillGainControl.
 * 10/27/21, Adam
 * 	added ValoriteSuccess, added VeriteSuccess, added AgapiteSuccess
 * 10/24/21, Yoar
 * Added a BulkOrdersEnabled bit
 * 9/10/21, Adam (MagicGearDropDowngrade)
 * Adding a variable to control the drop 'level' of magic weapons and armor globably
 * This is done to give crafters a chance to sell their magic crafted goods.
 * 9/14/21, Yoar
 * Added DetectSkillDelay for controlling the skill delay of detect hidden
 * 9/10/21, Adam (MagicGearDropChance)
 * Adding a variable to control the drop quantity of magic weapons and armor globably
 * This is done to give crafters a chance to sell their magic crafted goods.
 * 8/31/21, Adam
 * Moved DroptoBackpack() and RolledInYourSleep() to AIPUtils
 * 8/30/21 Lib
 * moved xml writing into extension method
 * added SpellManagementConsole values to CoreAI
 * 7/26/21, Pix
 * Added HarvestConstantX,, HarvestConstantY, HarvestConstantM, for controlling harvest seeding.
 * 7/20/21, Pix
 * Added ExplosionPotionThrowDelay for explosion potion 'cooldown'
 *	3/5/16, Adam
 *		Add the featurebit PlayerAccountWipe which executes in the usual account cleanup in CrobTasks.cs
 *		The bit is reset after all all accounts are wiped. Houses are also cleaned up with the wipe.
 *		This bit is accessed via the core management console and is owner access only.
 *	8/20/11, Adam
 *		Add a switch to turn on/off that account blocker when users are switching between accounts and IP addresses.
 *		I think this is preventing legitimate play so I'm turning it off until I understand the system better.
 *	8/8/10, Adam
 *		Add: OldFlee switch
 *			BaseAI seems to have a bug where they clear FocusMob in OnActionChanged().Flee
 *	6/12/10, adam
 *		Add a switch for controling the algo used for calculating armor absorb	(ArmorAbsorbClassic)
 *	3/13/10, Adam
 *		Add PetNeedsLOS
 *		in DoOrderAttack() for pets: give up if target is not in LOS (addresses the gate pet to give counts exploit)
 *	3/4/10, Adam
 *		Fix GetDouble to handle the frequent case of instring=="" instead of letting exception handling handle it.
 *	11/25/08, Adam
 *		Make MaxAddresses a console value
 *			in IPLimiter; controls how many of the same IP address can be concurrently logged in
 *	2/18/08, Adam
 *		Make MaxAccountsPerIP a console value
 *	1/19/08, Adam
 *		Add GracePeriod, ConnectionFloor, and Commission values for Player Vendor Management
 * 01/04/08, Pix
 * Added GWRChangeDelayMinutes setting.
 * 12/9/07, Adam
 * Added NewPlayerGuild feature bit.
 *	8/26/07 - Pix
 *		Added NewPlayerStartingArea feature bit.
 *	8/1/07, Pix
 *		Added RazorNegotiateFeaturesEnabled and RazorNegotiateWarnAndKick feature bits.
 * 4/3/07, Adam
 * Add a BreedingEnabled bit
 * Add a RTTNotifyEnabled bit
 *	1/08/07 Taran Kain
 *		Changed GSGG lookups to reflect new location in PlayerMobile
 *	01/02/07, Pix
 *		Added RangedCorrosionModifier.
 *		Added RangedCorrosion featurebit.
 * 08/12/06, Plasma
 * Changed AI champ restart values to 1 min
 *	10/16/06, Adam
 *		Add flag to disable tower placement
 *	10/16/06, Adam
 *		Add global override for SecurePremises
 *			i.e., CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SecurePremises)
 *	8/24/06, Adam
 *		Added TCAcctCleanupEnable to allow disabling of auto account cleanup
 * 8/19/06, Rhiannon
 *		Added PlayAccessLevel to allow dynamic control of the Play command.
 *	8/5/06, weaver
 *		Added feature bit to control disabling of help stuck command.
 *	7/23/06, Pix
 *		Added GuildKinChangeDisabled featurebit
 * 3/26/06, Pix
 *		Added IOBJoinEnabled;
 *	1/29/06, Adam
 *		TCAcctCleanupDays; trim all non-staff account and not logged in for N days - heartbeat
 *	12/28/05, Adam
 *		Add CommDeedBatch to set the number of containers processed per pass
 *	12/14/05 Pix
 *		Added DebugItemDecayOutput
 * 	12/01/05 Pix
 *		Added WorldSaveFrequency to CoreAI
 *	10/02/05, erlein
 *		Added ConfusionBaseDelay for adjustment of tamed creature confusion upon paralysis.
 *	9/20/05, Adam
 *		a. Add flag for OpposePlayers. This flag modifies the bahavior of IsOpposition()
 *		in BaseCreature such that aligned PLAYERS appear as enimies to NPCs of a different alignment.
 *		b. add flag for OpposePlayersPets
 *	9/13/05, erlein
 *		Added MeleePoisonSkillFactor bool to control poison skill factoring in
 *		OnHit() equations of melee weapons.
 *	9/03/05, Adam
 *		Add Global FreezeHouseDecay variable - World crisis mode :\
 *	09/02,05, erlein
 *		Added ReaggressIgnoreChance and xml save/load for IDOCBroadcastChance
 * 08/25/05, Taran Kain
 *		Added IDOCBroadcastChance
 *	07/13/05, erlein
 *		Added EScrollChance, EScrollSuccess.
 *	07/06/05, erlein
 *		Added CohesionLowerDelay, CohesionBaseDelay and CohesionFactor
 *		to control new res delay.
 * 06/03/05 Taran Kain
 *		Added TownCrierWordMinuteCost
 *	6/3/05, Adam
 *		Add in ExplosionPotionThreshold to control the tossers
 *		health requirement
 *	4/30/05, Pix
 *		Removed ExplosionPotionAlchemyReduction
 *	4/26/05, Pix
 *		Made explode pots targetting method toggleable based on CoreAI/ConsoleManagement setting.
 *	4/23/05, Pix
 *		Added ExplosionPotionAlchemyReduction
 *	04/18/05, Pix
 *		Added offline short term murder decay (only if it's turned on).
 *		Added potential exploding of carried explosion potions.
 *	4/18/05, Adam
 *		Add TempDouble and TempInt for testing ingame settings
 *	4/14/05, Adam
 *		Add CoreAI.TreasureMapDrop to dynamically adjust the treasuremap drop.
 *	4/8/05, Adam
 *		Add variables for the following CoreAI properties
 *		SpiritDepotTRPots, SpiritFirstWaveVirtualArmor, SpiritSecondWaveVirtualArmor,
 *		SpiritThirdWaveVirtualArmor, SpiritBossVirtualArmor
 *	3/31/05, Pix
 *		Added IsDynamicFeatureSet() utility function.
 *	3/31/05, Adam
 *		Add the new global flag for IOB'ness IOBShardWide
 *	3/7/05, Adam
 *		Add the global InmateRecallExploitCheck flag (see recall.cs)
 *	2/4/05, Adam
 *		Added PowderOfTranslocationAvail drop percentage
 *		The meaning of this file has just changed to mean: core Angel Island golbal properties.
 *		Previously this file was really only the Angel Island Prison.
 *	6/16/04, Pixie
 *		Added GSGG factor.
 *	4/29/04, mith
 *		Modified SpawnFreq variables, replaced with Restart/Expire variables (these are what the AILevelSystem uses)
 *		Added variable for CaveEntrance timer.
 *		Integrated all variables into applicable AI objects.
 *	4/13/04, mith
 *		Removed armor drops, added scroll drops.
 *	4/12/04 mith
 *		Modified initial values for Stinger Min/Max HP
 *		Added variables for Stinger Min/Max Damage
 *		Modified reg drops and starting stats for guards.
 *	4/12/04 pixie
 *		Initial Revision.
 *	4/12/04 Created by Pixie;
 */

using Scripts.Misc;
using System;
using System.IO;
using System.Xml;

namespace Server
{
    public partial class CoreAI
    {
        /// <summary>
        /// This enum is used by at least teleporters and spawners to limit the world size.
        /// When the global world size changes, certain spawners and teleporters will enable/disable.
        /// </summary>
        [Flags]
        public enum WorldSize
        {
            None = 0x00,
            Small = 0x01,
            Medium = 0x02,
            Large = 0x04,
            Full = 0x08,
        }
        public static bool WatchListShowExceptions = false; // may have a client exception for running a Mac or similar
        public static bool RevealSpellDifficulty = false;
        public static bool EnablePackUp = false;
        public static double TrackRangeBase = 10.0;
        public static double TrackRangePerSkill = 0.1;
        public static double FarmableCotton = 0.9;
        public static double FarmableFlax = 0.9;
        public static int TrackRangeMin = 10;
        public static bool TrackDifficulty = false;
        public static double BolaThrowLock = 2.0;
        public static double BolaMountLock = 3.0;
        public static int PJUMBountyCap = 25000;
        public static double AreaPeaceDelayBase = 1.0;
        public static double AreaPeaceDelayBonus = 2.0;
        public static int AreaPeaceCrowdSize = 6;
        public static double AreaPeaceSoloBonus = 0.0;
        public static double HitPoisonChanceIncr = 0.10; // incremental chance to hit weapon poison at GM poisoning
        public static int BaseVendorFee = 20; // base player vendor fee per one UO day (one UO day = 2 RL hours)
        public static double MultiBoxCensure = 60000; // how long between censures
        public static double MultiBoxPlayerStopped = 4000; // player has not moved for this long.. we consider them to have stopped.
        public static double MultiBoxDelay = 0.05; // time to reestablish connection with mobile
        public static int BackupCount = 336; // saves every 30 minutes * 7 days
        public static double ValoriteSuccess = .22; // success factors in magic craft system
        public static double VeriteSuccess = .19; // ...
        public static double AgapiteSuccess = .29; // ...
        public static int DungeonChestBuryRate = 0; // rate (in percentage) at which dungeon treasure chests are buried
        public static int DungeonChestBuryMinLevel = 6; // minimum chest level for buried treasure
        public static int MagicGearDropDowngrade = 1; // golbal magic gear (weapons and armor) downgrade (quality)
        public static int MagicGearDropChance = 5; // reducing world drops of magic weapons and armor to help out the crafters (quantity)
        public static int DetectSkillDelay = 6; // skill delay of detect hidden in seconds
        public static WorldSize CurrentWorldSize = WorldSize.Small | WorldSize.Medium | WorldSize.Large | WorldSize.Full;
        public static int StingerMinHP = 50;
        public static int StingerMaxHP = 75;
        public static int StingerMinDamage = 8;
        public static int StingerMaxDamage = 12;
        //		public static int CellGuardSpawnFreq = 20; //seconds?
        public static int CellGuardStrength = 200;
        public static int CellGuardSkillLevel = 75;
        public static int CellGuardNumRegDrop = 5;
        public static int GuardSpawnRestartDelay = 1; //minutes
        public static int GuardSpawnExpireDelay = 10; //minutes
                                                      //		public static int PostGuardSpawnFreq = 20;
        public static int PostGuardStrength = 200;
        public static int PostGuardSkillLevel = 85;
        public static int PostGuardNumRegDrop = 5;
        public static int PostGuardNumBandiesDrop = 2;
        public static int PostGuardNumGHPotDrop = 2;
        public static int CaptainGuardStrength = 600;
        public static int CaptainGuardSkillLevel = 100;
        public static int CaptainGuardWeapDrop = 2;
        public static int CaptainGuardNumRegDrop = 10;
        public static int CaptainGuardNumBandiesDrop = 10;
        public static int CaptainGuardGHPotsDrop = 4;
        public static int CaptainGuardScrollDrop = 3;
        public static int CaptainGuardNumLighthousePasses = 4;
        public static int CavePortalAvailability = 120; //seconds
                                                        //		public static int CaptainGuardLeatherSets = 1;
                                                        //		public static int CaptainGuardRingSets = 1;
                                                        //		public static int SpiritRespawnFreq = 15;
        public static int SpiritRestartDelay = 1; //minutes
        public static int SpiritExpireDelay = 7; //minutes
        public static int SpiritPortalAvailablity = 60; //seconds
        public static int SpiritFirstWaveNumber = 5;
        public static int SpiritFirstWaveHP = 25;
        public static int SpiritSecondWaveNumber = 5;
        public static int SpiritSecondWaveHP = 100;
        public static int SpiritThirdWaveNumber = 5;
        public static int SpiritThirdWaveHP = 200;
        public static int SpiritBossHP = 1000;
        public static int SpiritDepotGHPots = 10;
        public static int SpiritDepotBandies = 100;
        public static int SpiritDepotReagents = 10;
        public static int SpiritDepotRespawnFreq = 300;
        public static double PowderOfTranslocationAvail = 0.001; // drop rate in percent
        public static UInt64 DynamicFeatures = 0;
        [Obsolete]                                  // the 64bit version of dynamic patcher has been replaced with a CoreAIPatcher table.
        public static UInt64 DynamicPatches = 0;    //  this int64 can be removed at some future date, but first run, this value is converted to the new table.
        public static int SpiritDepotTRPots = 10;

        // AIP spirit spawn virtual armor
        public static int SpiritFirstWaveVirtualArmor = 100;
        public static int SpiritSecondWaveVirtualArmor = 30;
        public static int SpiritThirdWaveVirtualArmor = 50;
        public static int SpiritBossVirtualArmor = 34;

        // treasure map drop rate - usually something like 3.5% chance to appear as loot
        public static double TreasureMapDrop = 0.035; // drop rate in percent

        public static double FastwalkTrapMemory = 10.0;     // time in seconds we will remember this player
        public static int FastwalkInfractionThreshold = 3;  // number of allowed infractions within FastwalkTrapMemory seconds.
        public static double FastwalkTrapFactor = 1.2;      // speed over FastwalkThreshold
        public static bool JailFastwalkers = false;         // jail fastwalkers?
        public static bool JailLogin = true;                // jail certain login failures

        public static bool ForceGMNCUO = false;             // Require our customized version of CUO
        public static bool ForceGMNRZR = false;             // Require our customized version of Razor
        public static bool FakeBadGMNCUO = false;           // Emulate bad client (not serialized)
        public static bool FakeBadGMNRZR = false;           // Emulate bad Razor (not serialized)
        public static bool WarnBadGMNCUO = false;           // Warn of bad CUO
        public static bool WarnBadGMNRZR = false;           // Warn of bad Razor

        public static double MagicWandDropChance = 0.2;             // 20% chance at a magic wand
        public static double EnchantedScrollDropChance = 0.5;       // 50% chance at replacing the item with an enchanted scroll

        // Vorpal Bunny speed info
        public static double ActiveSpeedOverride = 0.175;       // speeds used for pets in Follow/Come/Guard mode
        public static double PassiveSpeedOverride = 0.350;
        // mounted humanoid mobiles
        public static double ActiveMountedSpeedOverride = 0.1;  // speeds used for mounted humanoids in Follow/Come/Guard mode
        public static double PassiveMountedSpeedOverride = 0.350;

        public static int SystemAccount = 0;                // Persistent System Account
        public static int AdminAccount = 0x00013237;        // Persistent Admin Account

        // purple potion explosion factors
        public static int ExplosionPotionSensitivityLevel = 100; //minimum damage before potion check happens
        public static double ExplosionPotionChance = 0.0; //percentage chance that potion will go off
        public static double ExplosionPotionThreshold = 0.95; // health of caster must be this %

        // offline count decays
        public static int OfflineShortsDecayHours = 24;
        public static int OfflineShortsDecay = 0;

        // Explosion Potion Target Method
        public enum EPTM { MobileBased, PointBased };
        public static EPTM ExplosionPotionTargetMethod = EPTM.MobileBased;

        // Town Crier cost
        public static int TownCrierWordMinuteCost = 50;

        // Spirit cohesion controls
        public static int CohesionBaseDelay = 0;
        public static int CohesionLowerDelay = 0;
        public static int CohesionFactor = 0;

        // Chance to broadcast newly IDOC houses over TCCS
        public static double IDOCBroadcastChance = 0.3;

        // Chance for creatures to ignore re-aggressive actions when not aggressing
        // already
        public static double ReaggressIgnoreChance = 0.0;

        // Base delay for pet confusion upon paralysis
        public static int ConfusionBaseDelay = 10;

        public static int WorldSaveFrequency = 30;

        public static bool DebugItemDecayOutput = false;


        // trim all non-staff account and not logged in for N days - heartbeat
        public static int TCAcctCleanupDays = 30;

        /// <summary>
        /// StandingDelay: denotes the minimum time (in seconds) an archer must stand still
        /// before being able to fire.
        /// </summary>
        public static double StandingDelay = 0.5;

        // Factor to apply to the NextMove delay for running creatures
        public static double RunningWildFactor = 0.3;
        // Factor to apply to the NextMove delay for running humanoid
        public static double RunningHumFactor = 0.7;
        // Factor to apply to the NextMove delay for running pets
        public static double RunningPetFactor = 0.01;

        // Control access level of Play command
        public static AccessLevel PlayAccessLevel = AccessLevel.Player;

        // enable account cleanup - heartbeat
        public static bool TCAcctCleanupEnable = true;

        // ranged corrosion addition reduction
        public static int RangedCorrosionModifier = 0;

        // Guild War Ring change setting delay
        public static int GWRChangeDelayMinutes = 0;

        // Player Vendor knobs
        public static bool RestockCharges = false;  // default to no vendor restock charges
        public static int GracePeriod = 60;         // 60 minute grace period to move items without restock charge
        public static int ConnectionFloor = 50;     // do not factor connections below this floor
        public static double Commission = .07;      // commission player vendors charge

        // custom mobiles 
        public static double MinstrelBasePay = 500;     // base pay for a Minstrel (3x on Siege)

        public static int MaxAccountsPerIP = 1;         // how many accounts can be created/accessed per IP Address
        public static int MaxConcurrentAddresses = 10;  // in IPLimiter; controls how many of the same IP address can be concurrently logged in. Maybe > MaxAccountsPerIP
        public static int MaxAccountsPerMachine = 1;    // in HardwareLimiter; controls how many accounts may be accessed from the same machine

        public static double SlayerWeaponDropRate = 0.05;
        public static double SlayerInstrumentDropRate = 0.05;
        public static double EnchantedEquipmentDropRate = 0.05; // currently hardcoded. 
        public static double SiegeGearDropFactor = 1.6;         // globally adjusts magic gear: PackMagicStuff() => SiegeGlobalTweakDrop()

        //Explosion Potion timing
        public static double ExplosionPotionThrowDelay = 2.1;

        //Harvest System Constants (these are the defaults for RunUO).
        // If they are changed, then the next time something is harvested on that spot (after wiping harvest memory or a server restart),
        // the ore will change to the new one.
        public static int HarvestConstantX = 17;
        public static int HarvestConstantY = 11;
        public static int HarvestConstantM = 3;

        // lib: SpellManagementConsole 8/30/21
        public static double SpellDefaultDelay = 0.5;
        public static double SpellAddlCastDelayCure = 0.02;
        public static double SpellHealRecoveryOverride = 0.95;
        public static double SpellCastDelayConstant = 0.5;
        public static double SpellCastDelayCoeff = 0.25;
        public static bool SpellUseRuoRecoveries = false;
        public static double SpellRuoRecoveryBase = 1.0;
        public static double SpellRecoveryMin = 0.2;
        public static double SpellFireballDmgDelay = 0.65;
        public static double SpellFsDmgDelay = 0.45;
        public static double SpellLightningDmgDelay = 0.35;

        public static void Configure()
        {
            // must load before the world because of the patchbits
            EventSink.PreWorldLoad += new PreWorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoadCliloc);
        }

        #region FeatureBits
        public enum FeatureBits : UInt64
        {
            InmateRecallExploitCheck = 0x01,
            IOBShardWide = 0x02,
            FreezeHouseDecay = 0x04,
            MeleePoisonSkillFactor = 0x08,
            OpposePlayers = 0x10,
            OpposePlayersPets = 0x20,
            IOBJoinEnabled = 0x40,
            GuildKinChangeDisabled = 0x80,
            HelpStuckDisabled = 0x100,
            SecurePremises = 0x200,
            TowerAllowed = 0x400,
            RangedCorrosion = 0x800,
            BreedingEnabled = 0x1000,
            RTTNotifyEnabled = 0x2000,
            RazorNegotiateFeaturesEnabled = 0x4000,
            RazorNegotiateWarnAndKick = 0x8000,
            NewPlayerStartingArea = 0x10000,
            NewPlayerGuild = 0x20000,
            PetNeedsLOS = 0x40000, // in DoOrderAttack() for pets: give up if target is not in LOS (addresses the gate pet to give counts exploit)
            LogStableCharges = 0x80000,
            ArmorAbsorbClassic = 0x100000, // use the scaled armor AR to calculate how much damage is absorbed
            SpiritSpeakUsageReport = 0x200000, // send admins system messages regarding SpirtHeal, and SlayerStrike 
            OldFlee = 0x400000, // BaseAI seems to have a bug where they clear FocusMob in OnActionChanged().Flee
            TreasureMapUsageReport = 0x800000, // send admins system messages regarding treasure map decoded 
            SeaGypsyUsageReport = 0x1000000, // send admins system messages sea gypsy escorts 
            IPBinderEnabled = 0x2000000, // This is pixie's code to block account access if some combination of account/ip actions are taken
            PlayerAccountWipe = 0x4000000, // Set this bit to wipe all Player access accounts. Owner access only, auto resets after wipe. Houses and all possessions go too
            FreezeAccountDecay = 0x8000000, // Set this bit freeze account decay
            BulkOrdersEnabled = 0x10000000, // Set this bit to enable the bulk order system
            StaminaDrainByWeight = 0x20000000, // Set this bit to enable stamina drain by weight
            ShipTracking = 0x40000000, // Set this bit to enable the ship tracking system
            MagicGearThrottleEnabled = 0x80000000, // TRUE for AI, FALSE for SP/MO
            MagicCraftSystem = 0x100000000, // magic craft system enabled 
            ZoraSystem = 0x200000000, // zora enabled
            Cooperatives = 0x400000000, // are Cooperatives enabled?
            SiegeBless = 0x800000000, // is Siege Bless enabled?
            ChatEnabled = 0x1000000000, // is the chat system enabled?
            CharDeleteUnrestricted = 0x2000000000, // is character deletion unrestricted?
        };

        public static bool IsDynamicFeatureSet(FeatureBits fb)
        {
            if ((DynamicFeatures & (UInt64)fb) > 0) return true;
            else return false;
        }

        public static void SetDynamicFeature(FeatureBits fb)
        {
            DynamicFeatures |= (UInt64)fb;
        }

        public static void ClearDynamicFeature(FeatureBits fb)
        {
            DynamicFeatures &= ~((UInt64)fb);
        }
        #endregion FeatureBits

        public static void OnSave(WorldSaveEventArgs e)
        {
            Console.WriteLine("CoreAI Saving...");
            if (!Directory.Exists("Saves/AngelIsland"))
                Directory.CreateDirectory("Saves/AngelIsland");

            string filePath = Path.Combine("Saves/AngelIsland", "CoreAI.xml");

            using (StreamWriter op = new StreamWriter(filePath))
            {
                XmlTextWriter xml = new XmlTextWriter(op)
                {
                    Formatting = Formatting.Indented,
                    IndentChar = '\t',
                    Indentation = 1
                };

                xml.WriteStartDocument(true);
                xml.WriteStartElement("CoreAI");

                xml.WriteCoreXml("RevealSpellDifficulty", RevealSpellDifficulty.ToString());
                xml.WriteCoreXml("EnablePackUp", EnablePackUp.ToString());

                xml.WriteCoreXml("WatchListShowExceptions", WatchListShowExceptions.ToString());

                //xml.WriteCoreXml("FirstAvailableSerial", FirstAvailableSerial);

                xml.WriteCoreXml("TrackRangeBase", TrackRangeBase);
                xml.WriteCoreXml("TrackRangePerSkill", TrackRangePerSkill);
                xml.WriteCoreXml("TrackRangeMin", TrackRangeMin);
                xml.WriteCoreXml("TrackDifficulty", TrackDifficulty.ToString());

                xml.WriteCoreXml("FarmableCotton", FarmableCotton);
                xml.WriteCoreXml("FarmableFlax", FarmableFlax);

                xml.WriteCoreXml("BolaThrowLock", BolaThrowLock);
                xml.WriteCoreXml("BolaMountLock", BolaMountLock);

                xml.WriteCoreXml("PJUMBountyCap", PJUMBountyCap);

                xml.WriteCoreXml("AreaPeaceCrowdSize", AreaPeaceCrowdSize);
                xml.WriteCoreXml("AreaPeaceSoloBonus", AreaPeaceSoloBonus);
                xml.WriteCoreXml("AreaPeaceDelayBase", AreaPeaceDelayBase);
                xml.WriteCoreXml("AreaPeaceDelayBonus", AreaPeaceDelayBonus);

                xml.WriteCoreXml("HitPoisonChanceIncr", HitPoisonChanceIncr);

                xml.WriteCoreXml("BaseVendorFee", BaseVendorFee);

                xml.WriteCoreXml("MultiBoxCensure", MultiBoxCensure);
                xml.WriteCoreXml("MultiBoxPlayerStopped", MultiBoxPlayerStopped);
                xml.WriteCoreXml("MultiBoxDelay", MultiBoxDelay);

                xml.WriteCoreXml("BackupCount", BackupCount); // how many backups we keep
                xml.WriteCoreXml("ValoriteSuccess", ValoriteSuccess); // success factors in magic craft system
                xml.WriteCoreXml("VeriteSuccess", VeriteSuccess); // ...
                xml.WriteCoreXml("AgapiteSuccess", AgapiteSuccess); // ...

                xml.WriteCoreXml("DungeonChestBuryRate", DungeonChestBuryRate);
                xml.WriteCoreXml("DungeonChestBuryMinLevel", DungeonChestBuryMinLevel);

                xml.WriteCoreXml("MagicGearDropDowngrade", MagicGearDropDowngrade);
                xml.WriteCoreXml("MagicGearDropChance", MagicGearDropChance);

                xml.WriteCoreXml("DetectSkillDelay", DetectSkillDelay);
                xml.WriteCoreXml("StingerMinHP", StingerMinHP);
                xml.WriteCoreXml("StingerMaxHP", StingerMaxHP);
                xml.WriteCoreXml("StingerMinDamage", StingerMinDamage);
                xml.WriteCoreXml("StingerMaxDamage", StingerMaxDamage);

                //				xml.WriteStartElement( "CellGuardSpawnFreq" );
                //				xml.WriteString( CellGuardSpawnFreq.ToString() );
                //				xml.WriteEndElement();

                xml.WriteCoreXml("CellGuardStrength", CellGuardStrength);
                xml.WriteCoreXml("CellGuardSkillLevel", CellGuardSkillLevel);
                xml.WriteCoreXml("CellGuardNumRegDrop", CellGuardNumRegDrop);
                xml.WriteCoreXml("GuardSpawnRestartDelay", GuardSpawnRestartDelay);
                xml.WriteCoreXml("GuardSpawnExpireDelay", GuardSpawnExpireDelay);

                //				xml.WriteStartElement( "PostGuardSpawnFreq" );
                //				xml.WriteString( PostGuardSpawnFreq.ToString() );
                //				xml.WriteEndElement();

                xml.WriteCoreXml("PostGuardStrength", PostGuardStrength);
                xml.WriteCoreXml("PostGuardSkillLevel", PostGuardSkillLevel);
                xml.WriteCoreXml("PostGuardNumRegDrop", PostGuardNumRegDrop);
                xml.WriteCoreXml("PostGuardNumBandiesDrop", PostGuardNumBandiesDrop);
                xml.WriteCoreXml("PostGuardNumGHPotDrop", PostGuardNumGHPotDrop);
                xml.WriteCoreXml("CaptainGuardStrength", CaptainGuardStrength);
                xml.WriteCoreXml("CaptainGuardSkillLevel", CaptainGuardSkillLevel);
                xml.WriteCoreXml("CaptainGuardWeapDrop", CaptainGuardWeapDrop);
                xml.WriteCoreXml("CaptainGuardNumRegDrop", CaptainGuardNumRegDrop);
                xml.WriteCoreXml("CaptainGuardNumBandiesDrop", CaptainGuardNumBandiesDrop);
                xml.WriteCoreXml("CaptainGuardGHPotsDrop", CaptainGuardGHPotsDrop);
                xml.WriteCoreXml("CaptainGuardScrollDrop", CaptainGuardScrollDrop);
                xml.WriteCoreXml("CaptainGuardNumLighthousePasses", CaptainGuardNumLighthousePasses);
                xml.WriteCoreXml("CavePortalAvailability", CavePortalAvailability);

                //				xml.WriteStartElement( "CaptainGuardLeatherSets" );
                //				xml.WriteString( CaptainGuardLeatherSets.ToString() );
                //				xml.WriteEndElement();
                //
                //				xml.WriteStartElement( "CaptainGuardRingSets" );
                //				xml.WriteString( CaptainGuardRingSets.ToString() );
                //				xml.WriteEndElement();

                //				xml.WriteStartElement( "SpiritRespawnFreq" );
                //				xml.WriteString( SpiritRespawnFreq.ToString() );
                //				xml.WriteEndElement();

                xml.WriteCoreXml("SpiritRestartDelay", SpiritRestartDelay);
                xml.WriteCoreXml("SpiritExpireDelay", SpiritExpireDelay);
                xml.WriteCoreXml("SpiritPortalAvailablity", SpiritPortalAvailablity);
                xml.WriteCoreXml("SpiritFirstWaveNumber", SpiritFirstWaveNumber);
                xml.WriteCoreXml("SpiritFirstWaveHP", SpiritFirstWaveHP);
                xml.WriteCoreXml("SpiritSecondWaveNumber", SpiritSecondWaveNumber);
                xml.WriteCoreXml("SpiritSecondWaveHP", SpiritSecondWaveHP);
                xml.WriteCoreXml("SpiritThirdWaveNumber", SpiritThirdWaveNumber);
                xml.WriteCoreXml("SpiritThirdWaveHP", SpiritThirdWaveHP);
                xml.WriteCoreXml("SpiritBossHP", SpiritBossHP);
                xml.WriteCoreXml("SpiritDepotGHPots", SpiritDepotGHPots);
                xml.WriteCoreXml("SpiritDepotBandies", SpiritDepotBandies);
                xml.WriteCoreXml("SpiritDepotReagents", SpiritDepotReagents);
                xml.WriteCoreXml("SpiritDepotRespawnFreq", SpiritDepotRespawnFreq);

                // GSGG values
                //xml.WriteCoreXml("GSGGTime", Server.Mobiles.PlayerMobile.GSGG);

                // PowderOfTranslocation availability
                xml.WriteCoreXml("PowderOfTranslocationAvail", PowderOfTranslocationAvail);
                xml.WriteCoreXml("DynamicFeatures", DynamicFeatures);
                #region Patch Table
                SavePatchTable(xml);
                xml.WriteCoreXml("DynamicPatches", DynamicPatches);
                #endregion Patch Table
                xml.WriteCoreXml("SpiritDepotTRPots", SpiritDepotTRPots);
                xml.WriteCoreXml("SpiritFirstWaveVirtualArmor", SpiritFirstWaveVirtualArmor);
                xml.WriteCoreXml("SpiritSecondWaveVirtualArmor", SpiritSecondWaveVirtualArmor);
                xml.WriteCoreXml("SpiritThirdWaveVirtualArmor", SpiritThirdWaveVirtualArmor);
                xml.WriteCoreXml("SpiritBossVirtualArmor", SpiritBossVirtualArmor);

                // Treasure Map Drop Rate
                xml.WriteCoreXml("TreasureMapDrop", TreasureMapDrop);

                // if they are this much faster than our FastwalkThreshold, prison!
                xml.WriteCoreXml("FastwalkTrapMemory", FastwalkTrapMemory);
                xml.WriteCoreXml("FastwalkTrapFactor", FastwalkTrapFactor);
                xml.WriteCoreXml("FastwalkInfractionThreshold", FastwalkInfractionThreshold);
                xml.WriteCoreXml("JailFastwalkers", JailFastwalkers);
                xml.WriteCoreXml("JailLogin", JailLogin);

                // require GMNCUO / GMNRazor
                xml.WriteCoreXml("ForceGMNCUO", ForceGMNCUO);
                xml.WriteCoreXml("ForceGMNRZR", ForceGMNRZR);
                xml.WriteCoreXml("WarnBadGMNCUO", WarnBadGMNCUO);
                xml.WriteCoreXml("WarnBadGMNRZR", WarnBadGMNRZR);

                xml.WriteCoreXml("MagicWandDropChance", MagicWandDropChance);

                xml.WriteCoreXml("EnchantedScrollDropChance", EnchantedScrollDropChance);

                // speeds used for pets in Follow/Come/Guard mode
                xml.WriteCoreXml("ActiveSpeedOverride", ActiveSpeedOverride);
                xml.WriteCoreXml("PassiveSpeedOverride", PassiveSpeedOverride);

                xml.WriteCoreXml("ActiveMountedSpeedOverride", ActiveMountedSpeedOverride);
                xml.WriteCoreXml("PassiveMountedSpeedOverride", PassiveMountedSpeedOverride);

                // System accounts. Used for automated tasks that require a mobile
                xml.WriteCoreXml("SystemAccount", SystemAccount);
                xml.WriteCoreXml("AdminAccount", AdminAccount);

                // purple potion explosion factors
                xml.WriteCoreXml("ExplPotSensitivity", ExplosionPotionSensitivityLevel);
                xml.WriteCoreXml("ExplPotChance", ExplosionPotionChance);
                xml.WriteCoreXml("OfflineShortsHours", OfflineShortsDecayHours);
                xml.WriteCoreXml("OfflineShortsDecay", OfflineShortsDecay);
                xml.WriteCoreXml("ExplosionPotionTargetMethod", (int)ExplosionPotionTargetMethod);
                xml.WriteCoreXml("ExplosionPotionThreshold", ExplosionPotionThreshold);
                xml.WriteCoreXml("TownCrierWordMinuteCost", TownCrierWordMinuteCost);
                xml.WriteCoreXml("CohesionBaseDelay", CohesionBaseDelay);
                xml.WriteCoreXml("CohesionLowerDelay", CohesionLowerDelay);
                xml.WriteCoreXml("CohesionFactor", CohesionFactor);
                xml.WriteCoreXml("IDOCBroadcastChance", IDOCBroadcastChance);
                xml.WriteCoreXml("ReaggressIgnoreChance", ReaggressIgnoreChance);
                xml.WriteCoreXml("ConfusionBaseDelay", ReaggressIgnoreChance);
                xml.WriteCoreXml("WorldSaveFrequency", WorldSaveFrequency);
                xml.WriteCoreXml("DebugItemDecayOutput", DebugItemDecayOutput);
                xml.WriteCoreXml("TCAcctCleanupDays", TCAcctCleanupDays);
                xml.WriteCoreXml("StandingDelay", StandingDelay);
                xml.WriteCoreXml("RunningWildFactor", RunningWildFactor);
                xml.WriteCoreXml("RunningPetFactor", RunningPetFactor);
                xml.WriteCoreXml("RunningHumFactor", RunningHumFactor);
                xml.WriteCoreXml("PlayAccessLevel", (int)PlayAccessLevel);
                xml.WriteCoreXml("TCAcctCleanupEnable", TCAcctCleanupEnable);
                xml.WriteCoreXml("RangedCorrosionModifier", RangedCorrosionModifier);
                xml.WriteCoreXml("GWRChangeDelayMinutes", GWRChangeDelayMinutes);
                xml.WriteCoreXml("RestockCharges", RestockCharges);
                xml.WriteCoreXml("GracePeriod", GracePeriod);
                xml.WriteCoreXml("ConnectionFloor", ConnectionFloor);
                xml.WriteCoreXml("MaxAccountsPerIP", MaxAccountsPerIP);
                xml.WriteCoreXml("MaxAddresses", MaxConcurrentAddresses);
                xml.WriteCoreXml("MaxAccountsPerMachine", MaxAccountsPerMachine);

                // commission vendors
                xml.WriteCoreXml("Commission", Commission);

                // custom mobiles
                xml.WriteCoreXml("MinstrelBasePay", MinstrelBasePay);

                // Slayer Weapon drop rate
                xml.WriteCoreXml("SlayerWeaponDropRate", SlayerWeaponDropRate);
                xml.WriteCoreXml("SlayerInstrumentDropRate", SlayerInstrumentDropRate);

                // enchantment imbue chance
                xml.WriteCoreXml("EnchantedEquipmentDropRate", EnchantedEquipmentDropRate);

                // Siege magic gear drop factor
                xml.WriteCoreXml("SiegeGearDropFactor", SiegeGearDropFactor);

                xml.WriteCoreXml("CurrentWorldSize", (int)CurrentWorldSize);
                xml.WriteCoreXml("ExplosionPotionThrowDelay", ExplosionPotionThrowDelay);
                xml.WriteCoreXml("HarvestConstantX", HarvestConstantX);
                xml.WriteCoreXml("HarvestConstantY", HarvestConstantY);
                xml.WriteCoreXml("HarvestConstantM", HarvestConstantM);

                // lib: SpellManagementConsole 8/30/21
                xml.WriteCoreXml("SpellDefaultDelay", SpellDefaultDelay);
                xml.WriteCoreXml("SpellAddlCastDelayCure", SpellAddlCastDelayCure);
                xml.WriteCoreXml("SpellHealRecoveryOverride", SpellHealRecoveryOverride);
                xml.WriteCoreXml("SpellCastDelayConstant", SpellCastDelayConstant);
                xml.WriteCoreXml("SpellCastDelayCoeff", SpellCastDelayCoeff);
                xml.WriteCoreXml("SpellUseRuoRecoveries", SpellUseRuoRecoveries);
                xml.WriteCoreXml("SpellRuoRecoveryBase", SpellRuoRecoveryBase);
                xml.WriteCoreXml("SpellRecoveryMin", SpellRecoveryMin);
                xml.WriteCoreXml("SpellFireballDmgDelay", SpellFireballDmgDelay);
                xml.WriteCoreXml("SpellFsDmgDelay", SpellFsDmgDelay);
                xml.WriteCoreXml("SpellLightningDmgDelay", SpellLightningDmgDelay);

                xml.WriteEndElement();
                xml.Close();
            }
        }

        public static void OnLoadCliloc()
        {
            Console.WriteLine("Cliloc Loading... ");
            string temp = Server.Text.Cliloc.Lookup[500000];
        }

        public static void OnLoad()
        {
            Console.WriteLine("CoreAI Loading...");
            string filePath = Path.Combine("Saves/AngelIsland", "CoreAI.xml");

            if (!File.Exists(filePath))
                return;

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement root = doc["CoreAI"];

            RevealSpellDifficulty = GetBool(root["RevealSpellDifficulty"], RevealSpellDifficulty);

            EnablePackUp = GetBool(root["EnablePackUp"], EnablePackUp);

            WatchListShowExceptions = GetBool(root["WatchListShowExceptions"], WatchListShowExceptions);

            //FirstAvailableSerial = GetInt32(GetText(root["FirstAvailableSerial"], ""), FirstAvailableSerial);

            TrackRangeBase = GetDouble(GetText(root["TrackRangeBase"], ""), TrackRangeBase);
            TrackRangePerSkill = GetDouble(GetText(root["TrackRangePerSkill"], ""), TrackRangePerSkill);
            TrackRangeMin = GetInt32(GetText(root["TrackRangeMin"], ""), TrackRangeMin);
            TrackDifficulty = GetBool(root["TrackDifficulty"], TrackDifficulty);

            FarmableCotton = GetDouble(GetText(root["FarmableCotton"], ""), FarmableCotton);
            FarmableFlax = GetDouble(GetText(root["FarmableFlax"], ""), FarmableFlax);

            BolaThrowLock = GetDouble(GetText(root["BolaThrowLock"], ""), BolaThrowLock);
            BolaMountLock = GetDouble(GetText(root["BolaMountLock"], ""), BolaMountLock);

            PJUMBountyCap = GetInt32(GetText(root["PJUMBountyCap"], ""), PJUMBountyCap);

            AreaPeaceCrowdSize = GetInt32(GetText(root["AreaPeaceCrowdSize"], ""), AreaPeaceCrowdSize);
            AreaPeaceSoloBonus = GetDouble(GetText(root["AreaPeaceSoloBonus"], ""), AreaPeaceSoloBonus);
            AreaPeaceDelayBase = GetDouble(GetText(root["AreaPeaceDelayBase"], ""), AreaPeaceDelayBase);
            AreaPeaceDelayBonus = GetDouble(GetText(root["AreaPeaceDelayBonus"], ""), AreaPeaceDelayBonus);

            HitPoisonChanceIncr = GetDouble(GetText(root["HitPoisonChanceIncr"], ""), HitPoisonChanceIncr);

            BaseVendorFee = GetValue(root["BaseVendorFee"], BaseVendorFee);

            MultiBoxCensure = GetDouble(GetText(root["MultiBoxCensure"], ""), MultiBoxCensure);
            MultiBoxPlayerStopped = GetDouble(GetText(root["MultiBoxPlayerStopped"], ""), MultiBoxPlayerStopped);
            MultiBoxDelay = GetDouble(GetText(root["MultiBoxDelay"], ""), MultiBoxDelay);

            BackupCount = GetValue(root["BackupCount"], BackupCount);
            ValoriteSuccess = GetDouble(GetText(root["ValoriteSuccess"], ""), ValoriteSuccess);
            VeriteSuccess = GetDouble(GetText(root["VeriteSuccess"], ""), VeriteSuccess);
            AgapiteSuccess = GetDouble(GetText(root["AgapiteSuccess"], ""), AgapiteSuccess);

            DungeonChestBuryRate = GetValue(root["DungeonChestBuryRate"], DungeonChestBuryRate);
            DungeonChestBuryMinLevel = GetValue(root["DungeonChestBuryMinLevel"], DungeonChestBuryMinLevel);

            MagicGearDropChance = GetValue(root["MagicGearDropChance"], MagicGearDropChance);
            MagicGearDropDowngrade = GetValue(root["MagicGearDropDowngrade"], MagicGearDropDowngrade);

            DetectSkillDelay = GetValue(root["DetectSkillDelay"], DetectSkillDelay);
            StingerMinHP = GetValue(root["StingerMinHP"], StingerMinHP);
            StingerMaxHP = GetValue(root["StingerMaxHP"], StingerMaxHP);
            StingerMinDamage = GetValue(root["StingerMinDamage"], StingerMinDamage);
            StingerMaxDamage = GetValue(root["StingerMaxDamage"], StingerMaxDamage);
            //			CellGuardSpawnFreq = GetValue( root["CellGuardSpawnFreq"], CellGuardSpawnFreq );
            CellGuardStrength = GetValue(root["CellGuardStrength"], CellGuardStrength);
            CellGuardSkillLevel = GetValue(root["CellGuardSkillLevel"], CellGuardSkillLevel);
            CellGuardNumRegDrop = GetValue(root["CellGuardNumRegDrop"], CellGuardNumRegDrop);
            GuardSpawnRestartDelay = GetValue(root["GuardSpawnRestartDelay"], GuardSpawnRestartDelay);
            GuardSpawnExpireDelay = GetValue(root["GuardSpawnExpireDelay"], GuardSpawnExpireDelay);
            //			PostGuardSpawnFreq = GetValue( root["PostGuardSpawnFreq"], PostGuardSpawnFreq );
            PostGuardStrength = GetValue(root["PostGuardStrength"], PostGuardStrength);
            PostGuardSkillLevel = GetValue(root["PostGuardSkillLevel"], PostGuardSkillLevel);
            PostGuardNumRegDrop = GetValue(root["PostGuardNumRegDrop"], PostGuardNumRegDrop);
            PostGuardNumBandiesDrop = GetValue(root["PostGuardNumBandiesDrop"], PostGuardNumBandiesDrop);
            PostGuardNumGHPotDrop = GetValue(root["PostGuardNumGHPotDrop"], PostGuardNumGHPotDrop);
            CaptainGuardStrength = GetValue(root["CaptainGuardStrength"], CaptainGuardStrength);
            CaptainGuardSkillLevel = GetValue(root["CaptainGuardSkillLevel"], CaptainGuardSkillLevel);
            CaptainGuardWeapDrop = GetValue(root["CaptainGuardWeapDrop"], CaptainGuardWeapDrop);
            CaptainGuardNumRegDrop = GetValue(root["CaptainGuardNumRegDrop"], CaptainGuardNumRegDrop);
            CaptainGuardNumBandiesDrop = GetValue(root["CaptainGuardNumBandiesDrop"], CaptainGuardNumBandiesDrop);
            CaptainGuardGHPotsDrop = GetValue(root["CaptainGuardGHPotsDrop"], CaptainGuardGHPotsDrop);
            CaptainGuardScrollDrop = GetValue(root["CaptainGuardScrollDrop"], CaptainGuardScrollDrop);
            CaptainGuardNumLighthousePasses = GetValue(root["CaptainGuardNumLighthousePasses"], CaptainGuardNumLighthousePasses);
            CavePortalAvailability = GetValue(root["CavePortalAvailability"], CavePortalAvailability);
            //			CaptainGuardLeatherSets = GetValue( root["CaptainGuardLeatherSets"], CaptainGuardLeatherSets );
            //			CaptainGuardRingSets = GetValue( root["CaptainGuardRingSets"], CaptainGuardRingSets );
            //			SpiritRespawnFreq = GetValue( root["SpiritRespawnFreq"], SpiritRespawnFreq );
            SpiritRestartDelay = GetValue(root["SpiritRestartDelay"], SpiritRestartDelay);
            SpiritExpireDelay = GetValue(root["SpiritExpireDelay"], SpiritExpireDelay);
            SpiritPortalAvailablity = GetValue(root["SpiritPortalAvailablity"], SpiritPortalAvailablity);
            SpiritFirstWaveNumber = GetValue(root["SpiritFirstWaveNumber"], SpiritFirstWaveNumber);
            SpiritFirstWaveHP = GetValue(root["SpiritFirstWaveHP"], SpiritFirstWaveHP);
            SpiritSecondWaveNumber = GetValue(root["SpiritSecondWaveNumber"], SpiritSecondWaveNumber);
            SpiritSecondWaveHP = GetValue(root["SpiritSecondWaveHP"], SpiritSecondWaveHP);
            SpiritThirdWaveNumber = GetValue(root["SpiritThirdWaveNumber"], SpiritThirdWaveNumber);
            SpiritThirdWaveHP = GetValue(root["SpiritThirdWaveHP"], SpiritThirdWaveHP);
            SpiritBossHP = GetValue(root["SpiritBossHP"], SpiritBossHP);
            SpiritDepotGHPots = GetValue(root["SpiritDepotGHPots"], SpiritDepotGHPots);
            SpiritDepotBandies = GetValue(root["SpiritDepotBandies"], SpiritDepotBandies);
            SpiritDepotReagents = GetValue(root["SpiritDepotReagents"], SpiritDepotReagents);
            SpiritDepotRespawnFreq = GetValue(root["SpiritDepotRespawnFreq"], SpiritDepotRespawnFreq);

            // GSGG values
            //Server.Mobiles.PlayerMobile.GSGG = GetDouble(GetText(root["GSGGTime"], "0.0"), 0.0);

            // PowderOfTranslocation availability
            PowderOfTranslocationAvail = GetDouble(GetText(root["PowderOfTranslocationAvail"], ""), PowderOfTranslocationAvail);

            DynamicFeatures = GetUInt64(GetText(root["DynamicFeatures"], "0"), DynamicFeatures);
            #region PatchTable
            // Patcher uses a new patch table unbounded by the old 64bit model
            //  we need to read it first to allow the old patch model to 'rewrite' patch states in case a repatch is needed.
            LoadPatchTable(root);
            /*Obsolete*/
            DynamicPatches = GetUInt64(GetText(root["DynamicPatches"], "0"), DynamicPatches);
            if (DynamicPatches != 0)
            {
                // populate our new patch table with old patcher values
                Populate();
                // clear DynamicPatches - will never be used again
                DynamicPatches = 0;
            }

            #endregion PatchTable
            // more supply depot supplies
            SpiritDepotTRPots = GetValue(root["SpiritDepotTRPots"], SpiritDepotTRPots);

            // AIP spirit spawn virtual armor
            SpiritFirstWaveVirtualArmor = GetValue(root["SpiritFirstWaveVirtualArmor"], SpiritFirstWaveVirtualArmor);
            SpiritSecondWaveVirtualArmor = GetValue(root["SpiritSecondWaveVirtualArmor"], SpiritSecondWaveVirtualArmor);
            SpiritThirdWaveVirtualArmor = GetValue(root["SpiritThirdWaveVirtualArmor"], SpiritThirdWaveVirtualArmor);
            SpiritBossVirtualArmor = GetValue(root["SpiritBossVirtualArmor"], SpiritBossVirtualArmor);

            // Treasure Map Drop Rate
            TreasureMapDrop = GetDouble(GetText(root["TreasureMapDrop"], ""), TreasureMapDrop);

            // if they are this much faster than our FastwalkThreshold, prison!
            FastwalkTrapMemory = GetDouble(GetText(root["FastwalkTrapMemory"], ""), FastwalkTrapMemory);
            FastwalkTrapFactor = GetDouble(GetText(root["FastwalkTrapFactor"], ""), FastwalkTrapFactor);
            FastwalkInfractionThreshold = GetValue(root["FastwalkInfractionThreshold"], FastwalkInfractionThreshold);

            JailFastwalkers = (GetInt32(GetText(root["JailFastwalkers"], "0"), 0) != 0);
            JailLogin = (GetInt32(GetText(root["JailLogin"], "0"), 0) != 0);

            ForceGMNCUO = (GetInt32(GetText(root["ForceGMNCUO"], "0"), 0) != 0);
            ForceGMNRZR = (GetInt32(GetText(root["ForceGMNRZR"], "0"), 0) != 0);
            WarnBadGMNCUO = (GetInt32(GetText(root["WarnBadGMNCUO"], "0"), 0) != 0);
            WarnBadGMNRZR = (GetInt32(GetText(root["WarnBadGMNRZR"], "0"), 0) != 0);

            MagicWandDropChance = GetDouble(GetText(root["MagicWandDropChance"], ""), MagicWandDropChance);

            EnchantedScrollDropChance = GetDouble(GetText(root["EnchantedScrollDropChance"], ""), EnchantedScrollDropChance);

            ActiveSpeedOverride = GetDouble(GetText(root["ActiveSpeedOverride"], ""), ActiveSpeedOverride);
            PassiveSpeedOverride = GetDouble(GetText(root["PassiveSpeedOverride"], ""), PassiveSpeedOverride);

            ActiveMountedSpeedOverride = GetDouble(GetText(root["ActiveMountedSpeedOverride"], ""), ActiveMountedSpeedOverride);
            PassiveMountedSpeedOverride = GetDouble(GetText(root["PassiveMountedSpeedOverride"], ""), PassiveMountedSpeedOverride);

            SystemAccount = GetValue(root["SystemAccount"], SystemAccount);
            AdminAccount = GetValue(root["AdminAccount"], AdminAccount);

            // purple potion explosion factors
            ExplosionPotionSensitivityLevel = GetValue(root["ExplPotSensitivity"], ExplosionPotionSensitivityLevel);
            ExplosionPotionChance = GetDouble(GetText(root["ExplPotChance"], ""), ExplosionPotionChance);

            // murder count vars
            OfflineShortsDecayHours = GetValue(root["OfflineShortsHours"], OfflineShortsDecayHours);
            OfflineShortsDecay = GetValue(root["OfflineShortsDecay"], 0);

            ExplosionPotionTargetMethod = (EPTM)GetValue(root["ExplosionPotionTargetMethod"], (int)ExplosionPotionTargetMethod);

            ExplosionPotionThreshold = GetDouble(GetText(root["ExplosionPotionThreshold"], ""), ExplosionPotionThreshold);

            // town crier cost
            TownCrierWordMinuteCost = GetValue(root["TownCrierWordMinuteCost"], TownCrierWordMinuteCost);

            // spirit cohesion controls
            CohesionBaseDelay = GetValue(root["CohesionBaseDelay"], CohesionBaseDelay);
            CohesionLowerDelay = GetValue(root["CohesionLowerDelay"], CohesionLowerDelay);
            CohesionFactor = GetValue(root["CohesionFactor"], CohesionFactor);

            // chance to broadcast newly IDOC houses over TCCS
            IDOCBroadcastChance = GetDouble(GetText(root["IDOCBroadcastChance"], ""), IDOCBroadcastChance);

            // chance for creatures to ignore re-aggression when not aggressing already
            ReaggressIgnoreChance = GetDouble(GetText(root["ReaggressIgnoreChance"], ""), ReaggressIgnoreChance);

            // base period of confusion for tamed creatures upon paralysis
            ReaggressIgnoreChance = GetDouble(GetText(root["ConfusionBaseDelay"], ""), ConfusionBaseDelay);

            WorldSaveFrequency = GetInt32(GetText(root["WorldSaveFrequency"], "30"), 30);

            DebugItemDecayOutput = (GetInt32(GetText(root["DebugItemDecayOutput"], "0"), 0) != 0);

            TCAcctCleanupDays = GetInt32(GetText(root["TCAcctCleanupDays"], "30"), 30);

            StandingDelay = GetDouble(GetText(root["StandingDelay"], ""), StandingDelay);

            RunningWildFactor = GetDouble(GetText(root["RunningWildFactor"], ""), RunningWildFactor);
            RunningPetFactor = GetDouble(GetText(root["RunningPetFactor"], ""), RunningPetFactor);
            RunningHumFactor = GetDouble(GetText(root["RunningHumFactor"], ""), RunningHumFactor);

            PlayAccessLevel = (AccessLevel)GetValue(root["PlayAccessLevel"], (int)PlayAccessLevel);

            TCAcctCleanupEnable = (GetInt32(GetText(root["TCAcctCleanupEnable"], "0"), 0) != 0);

            RangedCorrosionModifier = GetInt32(GetText(root["RangedCorrosionModifier"], "0"), 0);

            GWRChangeDelayMinutes = GetInt32(GetText(root["GWRChangeDelayMinutes"], "0"), 0);

            RestockCharges = (GetInt32(GetText(root["RestockCharges"], "0"), 0) != 0);
            GracePeriod = GetInt32(GetText(root["GracePeriod"], ""), GracePeriod);
            ConnectionFloor = GetInt32(GetText(root["ConnectionFloor"], ""), ConnectionFloor);
            Commission = GetDouble(GetText(root["Commission"], ""), Commission);

            MinstrelBasePay = GetDouble(GetText(root["MinstrelBasePay"], ""), MinstrelBasePay);

            MaxAccountsPerIP = GetInt32(GetText(root["MaxAccountsPerIP"], MaxAccountsPerIP.ToString()), 0);
            MaxConcurrentAddresses = GetInt32(GetText(root["MaxAddresses"], MaxConcurrentAddresses.ToString()), 0);
            MaxAccountsPerMachine = GetInt32(GetText(root["MaxAccountsPerMachine"], MaxAccountsPerMachine.ToString()), 0);

            SlayerWeaponDropRate = GetDouble(GetText(root["SlayerWeaponDropRate"], ""), SlayerWeaponDropRate);
            SlayerInstrumentDropRate = GetDouble(GetText(root["SlayerInstrumentDropRate"], ""), SlayerInstrumentDropRate);
            EnchantedEquipmentDropRate = GetDouble(GetText(root["EnchantedEquipmentDropRate"], ""), EnchantedEquipmentDropRate);

            SiegeGearDropFactor = GetDouble(GetText(root["SiegeGearDropFactor"], ""), SiegeGearDropFactor);

            CurrentWorldSize = (WorldSize)GetInt32(GetText(root["CurrentWorldSize"], ""), (int)CurrentWorldSize);

            ExplosionPotionThrowDelay = GetDouble(GetText(root["ExplosionPotionThrowDelay"], ""), ExplosionPotionThrowDelay);

            HarvestConstantX = GetInt32(GetText(root["HarvestConstantX"], HarvestConstantX.ToString()), 17);
            HarvestConstantY = GetInt32(GetText(root["HarvestConstantY"], HarvestConstantY.ToString()), 11);
            HarvestConstantM = GetInt32(GetText(root["HarvestConstantM"], HarvestConstantM.ToString()), 3);

            // lib: SpellManagementConsole 8/30/21
            void setSimpleDouble(ref double property, string elementName)
            {
                property = GetDouble(GetText(root[elementName], property.ToString()), property);
            }
            setSimpleDouble(ref SpellDefaultDelay, "SpellDefaultDelay");
            setSimpleDouble(ref SpellAddlCastDelayCure, "SpellAddlCastDelayCure");
            setSimpleDouble(ref SpellHealRecoveryOverride, "SpellHealRecoveryOverride");
            setSimpleDouble(ref SpellCastDelayConstant, "SpellCastDelayConstant");
            setSimpleDouble(ref SpellCastDelayCoeff, "SpellCastDelayCoeff");
            SpellUseRuoRecoveries = (GetInt32(GetText(root["SpellUseRuoRecoveries"], "0"), 0) != 0);
            setSimpleDouble(ref SpellRuoRecoveryBase, "SpellRuoRecoveryBase");
            setSimpleDouble(ref SpellRecoveryMin, "SpellRecoveryMin");
            setSimpleDouble(ref SpellFireballDmgDelay, "SpellFireballDmgDelay");
            setSimpleDouble(ref SpellFsDmgDelay, "SpellFsDmgDelay");
            setSimpleDouble(ref SpellLightningDmgDelay, "SpellLightningDmgDelay");

            Utility.Monitor.WriteLine("[MaxAccountsPerIP {0}.]", Core.ConsoleColorInformational(), CoreAI.MaxAccountsPerIP);
            Utility.Monitor.WriteLine("[MaxAccountsPerMachine {0}.]", Core.ConsoleColorInformational(), CoreAI.MaxAccountsPerMachine);
            // in IPLimiter; MaxAddresses controls how many of the same IP address can be concurrently logged in. Maybe > MaxAccountsPerIP
            Utility.Monitor.WriteLine("[MaxConcurrentAddresses {0}.]", Core.ConsoleColorInformational(), CoreAI.MaxConcurrentAddresses);
            Utility.Monitor.WriteLine("[Force GMN CUO {0}.]", Core.ConsoleColorInformational(), CoreAI.ForceGMNCUO);
            Utility.Monitor.WriteLine("[Warn GMN CUO {0}.]", Core.ConsoleColorInformational(), CoreAI.WarnBadGMNCUO);
            Utility.Monitor.WriteLine("[Force GMN Razor {0}.]", Core.ConsoleColorInformational(), CoreAI.ForceGMNRZR);
            Utility.Monitor.WriteLine("[Warn GMN Razor {0}.]", Core.ConsoleColorInformational(), CoreAI.WarnBadGMNRZR);
        }

        public static int GetValue(XmlElement node, int defaultValue)
        {
            return GetInt32(GetText(node, defaultValue.ToString()), defaultValue);
        }

        public static string GetText(XmlElement node, string defaultValue)
        {
            if (node == null)
                return defaultValue;

            return node.InnerText;
        }

        public static int GetInt32(string intString, int defaultValue)
        {
            try
            {
                return XmlConvert.ToInt32(intString);
            }
            catch
            {
                try
                {
                    return Convert.ToInt32(intString);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }
        public static uint GetUInt32(string intString, uint defaultValue)
        {
            try
            {
                return XmlConvert.ToUInt32(intString);
            }
            catch
            {
                try
                {
                    return Convert.ToUInt32(intString);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }
        public static UInt64 GetUInt64(string intString, UInt64 defaultValue)
        {
            try
            {
                return XmlConvert.ToUInt64(intString);
            }
            catch
            {
                try
                {
                    return Convert.ToUInt64(intString);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        public static double GetDouble(string dblString, double defaultValue)
        {
            // adam: using exceptions to handle errors is bad-bad.
            //	the intString=="" case happens a lot!
            if (dblString == null || dblString.Length == 0)
                return defaultValue;

            try
            {
                return XmlConvert.ToDouble(dblString);
            }
            catch
            {
                try
                {
                    return Convert.ToDouble(dblString);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }
    }
}