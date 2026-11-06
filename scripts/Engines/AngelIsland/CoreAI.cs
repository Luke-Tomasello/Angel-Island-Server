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

/* Scripts/Engines/AngelIsland/CoreAI.cs
 * ChangeLog
 *
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
using Server.Misc;
using System;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Server
{
    public partial class CoreAI
    {
        public const int ConfigVersion = 1;

        public static AccessLevel LockdownLevel = AccessLevel.Player;

        // not serialized - only used for console commands and such
        public const string OwnerIP = "192.168.1.67";
        public const string OwnerLocalMachine = "LUKES-MAIN";

        #region Seasons

        /// <summary>
        /// Seasonal Accounting System - Gold to Points Conversion
        ///
        /// Purpose:
        /// Converts cumulative gameplay earnings recorded in DataRecorder into
        /// seasonal point progression using a delta-based, per-category model.
        ///
        /// Core Philosophy:
        /// - Real profit and progression should come from gameplay, not vendor arbitrage.
        /// - PvM remains the baseline calibration target because it defines the highest and most
        ///   consistent and broadly accessible gold-per-hour reference.
        /// - Different professions and activities are normalized, not artificially equalized.
        /// - High-effort play is rewarded, but runaway leaders are gently compressed near the cap.
        /// - Categories are processed independently so each playstyle can be tuned on its own merits.
        ///
        /// Accounting Model:
        /// - DataRecorder is the source of truth for raw cumulative earnings.
        /// - This module builds per-account snapshots from DataRecorder.
        /// - Each category is compared against its credited baseline to determine newly earned value.
        /// - Only newly earned value is eligible for conversion into seasonal points.
        /// - Gold is processed once. Any value suppressed by diminishing returns or caps is not banked
        ///   for later recovery.
        ///
        /// Separation of Responsibilities:
        /// - DataRecorder:
        ///   Tracks raw cumulative gold by earning source.
        ///
        /// - Seasonal accounting (this module):
        ///   Converts recorder totals into seasonal points using delta-based bookkeeping.
        ///
        /// - Leaderboard system:
        ///   Independently reads DataRecorder for rankings and display. It does not depend on
        ///   this module's internal snapshot or baseline structures.
        ///
        /// Calibration Reference:
        /// ~117,815 gold (about 90 dragons over a 90-day season) ~= 600 seasonal points
        /// -> 200 gold per point (anchor)
        ///
        /// SmallBOD Sampling (2000 random deeds per profession):
        /// - Blacksmith: ~1,151 gp  -> ~5.75 points per BOD at baseline
        /// - Tailor:       ~626 gp  -> ~3.13 points per BOD at baseline
        /// - Carpenter:    ~380 gp  -> ~1.90 points per BOD at baseline
        ///
        /// Category Rationales:
        ///
        /// PvM Loot:
        /// - Baseline category.
        /// - Represents the most consistent, visible, and broadly accessible earning model.
        /// - Other multipliers are judged relative to this.
        ///
        /// Music:
        /// - Currently treated at baseline.
        /// - Real-world observation from a top-tier composer indicates approximately
        ///   150,000 gold earned over a 90-day season.
        /// - At the baseline conversion rate (200 gold per point), this corresponds to
        ///   roughly 750 seasonal points, placing high-performing composers above PvM
        ///   baseline but below the seasonal cap.
        /// - This validates the current 1.0 multiplier as appropriately positioned:
        ///   music can be competitive when played at a high level without dominating
        ///   higher-risk or more specialized professions.
        /// - No adjustment is currently required; additional live data will continue
        ///   to inform future tuning if necessary.
        ///
        /// Chest Gold:
        /// - Chesting has meaningful upfront friction due to lockpicking and remove trap training,
        ///   which can be slower and less direct than reaching baseline PvM viability.
        /// - Early progression contains more dead time before chesting becomes truly profitable.
        /// - Strong chest targets (level 4-5, roughly 4000-5000 gp) are commonly located near
        ///   very dangerous monsters such as dragons and similar top-end threats.
        /// - Even when chesters avoid fighting the surrounding mobs directly, the playstyle still
        ///   operates in dangerous spaces and requires specialized skill investment.
        /// - Lower tier starter chests (level 2-3, roughly 2000-3000 gp) provide earlier access,
        ///   but the overall path remains narrower and more specialized than ordinary PvM.
        /// - For these reasons, chest gold is weighted above baseline PvM, but slightly below theft.
        ///
        /// Theft Gold:
        /// - Test thief steals about 340 gp per successful criminal theft of gold.
        /// - Assuming about 3-4 legitimate guarded-region criminal gold steals per hour,
        ///   theft yields roughly 91,800 to 122,400 gold over a 90-day season at about
        ///   1 active hour per day.
        /// - In practice, qualifying theft opportunities are limited:
        ///   players do not often carry large gold stacks in guarded regions,
        ///   and thieves more commonly target equipment than raw gold.
        /// - Because theft credit applies only to legitimate criminal gold theft,
        ///   the category is intentionally overweighted relative to raw gold yield so
        ///   successful thieves remain competitive within the seasonal system.
        ///
        /// Multipliers:
        /// These normalize categories relative to PvM baseline without forcing artificial parity.
        /// </summary>

        public static double TownshipWallCostFactor = 0.5;

        // Base conversion rate: gold -> seasonal points
        public static double SeasonsGoldPerPoint = 200.0;

        // Category Multipliers
        public static double SeasonsMusicMultiplier = 1.0;
        public static double SeasonsPvMLootMultiplier = 1.0;

        /// Chest Calibration (initial spitball):
        /// - Chesting requires meaningful early skill investment in lockpicking and remove trap
        /// - High-value chests are often found near top-end threats
        /// - The playstyle is more specialized and less broadly accessible than baseline PvM
        /// - Weighted above PvM, but kept below theft for now
        public static double SeasonsChestMultiplier = 1.50;

        /// Theft Calibration (initial spitball):
        /// - Legitimate criminal gold theft opportunities are limited and inconsistent
        /// - Raw gold yield alone likely understates the effort and gating involved
        /// - Intentionally overweighted so successful thieves remain seasonally competitive
        public static double SeasonsTheftMultiplier = 1.75;

        // BOD profession multipliers
        // Tuned separately because expected raw value differs significantly by profession.
        public static double SeasonsSmithBODMultiplier = 1.0;
        public static double SeasonsTailorBODMultiplier = 1.9;    // about 600 / 313
        public static double SeasonsCarpenterBODMultiplier = 3.1; // about 600 / 190

        // Seasonal Limits & Diminishing Returns

        /// <summary>
        /// Maximum seasonal points an account can earn in one season.
        /// Defines the upper bound for progression and seasonal rewards.
        /// </summary>
        public static int SeasonsPointCap = 1000;

        /// <summary>
        /// Diminishing returns floor near the seasonal cap.
        ///
        /// Example:
        /// 0.25 = at 100% progress, gains are reduced to 25% of normal rate.
        ///
        /// Higher values = more linear progression
        /// Lower values = stronger compression near the cap
        /// </summary>
        public static double SeasonsMinimumAwardFactor = 0.25;

        #region Seasons - Banded Progression Scaling (BPS)
        // Seasons - Banded Progression Scaling (BPS)

        // Fraction of SeasonsPointCap where soft pre-cap throttling begins.
        // Example: 0.80 with a cap of 1000 -> soft start at 800.
        public static double SeasonsBpsPreCapSoftStartFraction = 0.80;

        // Width of the first post-threshold band.
        // Example: 200 with a threshold of 1000 -> first post-cap band ends at 1200.
        public static int SeasonsBpsFirstPostCapWidth = 200;

        // Width of the second post-threshold band, measured from the threshold.
        // Example: 600 with a threshold of 1000 -> second post-cap band ends at 1600.
        public static int SeasonsBpsSecondPostCapWidth = 600;

        // Width of the third post-threshold band, measured from the threshold.
        // Example: 1000 with a threshold of 1000 -> third post-cap band ends at 2000.
        public static int SeasonsBpsThirdPostCapWidth = 1000;

        // Award multipliers per band.
        public static double SeasonsBpsEarlyMultiplier = 1.00;
        public static double SeasonsBpsPreCapMultiplier = 0.75;
        public static double SeasonsBpsFirstPostCapMultiplier = 0.10;
        public static double SeasonsBpsSecondPostCapMultiplier = 0.08;
        public static double SeasonsBpsThirdPostCapMultiplier = 0.06;
        public static double SeasonsBpsFinalMultiplier = 0.05;
        #endregion Seasons - Banded Progression Scaling (BPS)

        #endregion Seasons

        // duration: how long wars last AFTER the final save.
        public static TimeSpan ServerWarsDuration = TimeSpan.FromMinutes(10);
        public static bool ServerWarsIgnore = true;

        #region Criminality
        public static int CriminalGrayMinutes = 2;      // 2 minutes
        public static int CriminalWhackSeconds = 10;    // 10 seconds
        public static int CriminalTravelMinutes = 2;    // 2 minutes

        public static int HarmfulPvPGrayMinutes = 10;   // 10 minutes (player against another player - criminal)
        public static int HarmfulGrayMinutes = 2;       // 2 minutes
        public static int HarmfulWhackSeconds = 10;     // 10 seconds
        public static int HarmfulTravelMinutes = 2;     // 2 minutes

        public static int AFKGrayMinutes = 240;         // 4 hours
        public static int AFKWhackSeconds = 10;         // 10 seconds
        public static int AFKTravelMinutes = 2;         // 2 minutes

        public static int ThievingGrayMinutes = 10;     // 10 minutes
        public static int ThievingWhackSeconds = 10;    // 10 seconds
        public static int ThievingTravelMinutes = 2;    // 2 minutes
        #endregion Criminality

        public static int StaticCorpseMinutes = 1440;   // 24 hours

        public static double PostTeleportMoveLockMs = 150.0;
        // Disconnect Protection:
        // if a player disconnects in combat, they are flagged for DCP.
        // If they reconnect within the DCPWindow, and they have less than DCPMaxUses flags, they can be protected (invulnerable and hidden) for a short time to allow them to log out properly.
        // If they exceed DCPMaxUses, then they are not protected
        public static int DCPMaxUses = 2;
        public static TimeSpan DCPWindow = TimeSpan.FromMinutes(60);

        public static TimeSpan PermaFlagDuration = TimeSpan.FromHours(24);
        public static double StatGainFactor = 1.0;

        #region Pacify

        public static double PacifyDamageScaleKnob = 12.0;
        public static double PacifyRobustnessBiasKnob = 25.0;
        public static double PacifyMusicCenterKnob = 80.0;
        public static double PacifyMusicDampenStrengthKnob = 0.90;
        public static double PacifyTimeWeakeningKnob = 1.25;

        #endregion Pacify

        #region Mortalis Passive Gain System
        public static double CmdAttackCoolMs = 30 * 1000;
        public static double CmdComeCoolMs = 60 * 1000;
        public static double CmdGuardCoolMs = 45 * 1000;
        public static double CmdFollowCoolMs = 45 * 1000;
        public static double CmdPatrolCoolMs = 45 * 1000;
        public static double CmdDefaultCoolMs = 45 * 1000;
        public static double FeedingCoolMs = 5.0 * 1000;
        public static double SkillMeditationCoolMs = 10.0 * 1000;
        public static double SkillDefaultCoolMs = 3.0 * 1000;
        public static double StatCoolMs = 8.0 * 1000;
        public static double HealCoolMs = 6.0 * 1000;
        public static double CureCoolMs = 10.0 * 1000;
        public static double GaveDamageCoolMs = 9 * 1000;
        #endregion Mortalis Passive Gain System

        // Mortalis: per-player daily vendor sell limit (total GOLD paid in any rolling 24h window)
        public static int DailySellGoldLimit = 30000;

        public static int StatsThreshold = 10;
        public static bool WatchListShowExceptions = false; // may have a client exception for running a Mac or similar
        public static bool RevealSpellDifficulty = false;
        public static bool EnablePackUp = false;

        public static double TrackRangeBase = 10.0;
        public static double TrackRangePerSkill = 0.1;
        public static int TrackRangeMin = 10;
        public static bool TrackDifficulty = false;
        public static double TrackArrowRangeMultiplier = 2.0;
        public static double TrackHintRangeMultiplier = 3.0;

        public static double FarmableCotton = 0.9;
        public static double FarmableFlax = 0.9;
        public static double BolaThrowLock = 2.0;
        public static double BolaMountLock = 3.0;
        public static int PJUMBountyCap = 25000;
        public static double AreaPeaceDelayBase = 1.0;
        public static double AreaPeaceDelayBonus = 2.0;
        public static int AreaPeaceCrowdSize = 6;
        public static double AreaPeaceSoloBonus = 0.0;
        public static double HitPoisonChanceIncr = 0.10; // incremental chance to hit weapon poison at GM poisoning
        public static double PoisonBandageSlipChance = 0.10; // control how often poison causes 'finger slip' when healing with bandages
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

        public static double MagicWandDropChance = 0.2;         // 20% chance at a magic wand
        public static double EnchantedScrollDropChance = 0.5;   // 50% chance at replacing the item with an enchanted scroll
        public static int BlankScrollMultiplier = 30;           // how many blank scrolls are dropped in treasure chests

        // Vorpal Bunny speed info
        public static double ActiveSpeedOverride = 0.175;       // speeds used for pets in Follow/Come/Guard mode
        public static double PassiveSpeedOverride = 0.350;
        // mounted humanoid mobiles
        public static double ActiveMountedSpeedOverride = 0.1;  // speeds used for mounted humanoids in Follow/Come/Guard mode
        public static double PassiveMountedSpeedOverride = 0.350;
        public static double MountEnduranceFactor = 2.0;    // multiplier for how much endurance is drained when mounted        

        public static int SystemAccount = 0;                // Persistent System Account
        public static int AdminAccount = 0x00013237;        // Persistent Admin Account
        public static int LordBritishAccount = 0;           // Persistent Lord British Account

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

        // Chance to broadcast newly IDOC houses over TownCrierBulletinSystem
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
        public static int MaxCharactersPerAccount = 5;  // 5 and 1 are natively supported by the client, but we can set this to whatever we want and enforce it server side.

        public static double SlayerWeaponDropRate = 0.05;
        public static double SlayerInstrumentDropRate = 0.05;
        public static double EnchantedEquipmentDropRate = 0.05; // currently hardcoded. 
        public static double SiegeGearDropFactor = 1.6;         // globally adjusts magic gear: PackMagicStuff() => SiegeGlobalTweakDrop()

        //Explosion Potion/firebomb timing
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

        [CallPriority(2)]  // must come before patcher.cs is run
        public static void Configure()
        {
            // must load before the world because of the patchbits
            EventSink.PreWorldLoad += new PreWorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoadCliloc);
        }

        /*
         * Adding a new dynamic feature:
         * 
         * 1. Append the new value to the END of CoreAI.FeatureBits.
         * 2. Use CoreAI.IsDynamicFeatureSet(), SetDynamicFeature(), and
         *      ClearDynamicFeature() to access it.
         * 3. No CoreAI.xml version change or migration is required.
         * 
         * Compatibility: CoreAI.xml (V1) persists features by NAME, while the
         * V0 upgrade in CoreAIPatcher maps legacy bit positions by ORDINAL.
         * Existing FeatureBits values must therefore never be renamed,
         * reordered, removed, or reused.
         * 
         * Retiring a feature:
         * - keep its original name and its original position (ordinal);
         * - mark it [Obsolete("Retired ... Retained for FeatureBits ordinal
         *      compatibility; do not reuse.")];
         * - remove its remaining source references rather than suppressing
         *      the resulting CS0618 warnings.
         * A retired slot is never reused; new features are always appended.
         */
        #region FeatureBits

        public enum FeatureBits
        {
            InmateRecallExploitCheck = 0,
            [Obsolete("Retired IOB feature. Retained for FeatureBits ordinal compatibility; do not reuse.")]
            IOBShardWide,
            FreezeHouseDecay,
            MeleePoisonSkillFactor,
            OpposePlayers,
            OpposePlayersPets,
            [Obsolete("Retired IOB feature. Retained for FeatureBits ordinal compatibility; do not reuse.")]
            IOBJoinEnabled,
            GuildKinChangeDisabled,
            HelpStuckDisabled,
            SecurePremises,
            TowerAllowed,
            RangedCorrosion,
            BreedingEnabled,
            RTTNotifyEnabled,
            RazorNegotiateFeaturesEnabled,
            RazorNegotiateWarnAndKick,
            NewPlayerStartingArea,
            NewPlayerGuild,
            PetNeedsLOS,
            LogStableCharges,
            ArmorAbsorbClassic,
            SpiritSpeakUsageReport,
            OldFlee,
            TreasureMapUsageReport,
            SeaGypsyUsageReport,
            IPBinderEnabled,
            FreezeTownshipDecay,
            FreezeAccountDecay,
            BulkOrdersEnabled,
            StaminaDrainByWeight,
            ShipTracking,
            MagicGearThrottleEnabled,
            MagicCraftSystem,
            ZoraSystem,
            Cooperatives,
            SiegeBless,
            ChatEnabled,
            CharDeleteUnrestricted,
            PassiveTamingEnabled,
            CmdAttackBlock,
            CmdComeBlock,
            CmdGuardBlock,
            CmdFollowBlock,
            CmdPatrolBlock,
            CmdDefaultBlock,
            FeedingBlock,
            SkillMeditationBlock,
            SkillDefaultBlock,
            StatBlock,
            HealBlock,
            CureBlock,
            GaveDamageBlock,
            LogPassiveGains,
            LogActualGains,
            EthicsEnabled,
            FactionsEnabled,
            DragonScaleArmor,
            TerritoriesEnabled,
            AngelIslandVisible,
            SiegePerilousVisible,
            MortalisVisible,
            SeasonsVisible,
            TestCenterVisible,
            UpheavalVisible
        }

        internal static bool[] DynamicFeatures = new bool[Enum.GetValues(typeof(FeatureBits)).Length];

        public static bool IsDynamicFeatureSet(FeatureBits feature)
        {
            return DynamicFeatures[(int)feature];
        }

        public static void SetDynamicFeature(FeatureBits feature)
        {
            DynamicFeatures[(int)feature] = true;
        }

        public static void ClearDynamicFeature(FeatureBits feature)
        {
            DynamicFeatures[(int)feature] = false;
        }

        #endregion

        public static void OnSave(WorldSaveEventArgs e)
        {
            Console.WriteLine("CoreAI Saving...");

            string saveDirectory = Path.Combine(Core.SavesDirectory, Core.Server);

            if (!Directory.Exists(saveDirectory))
                Directory.CreateDirectory(saveDirectory);

            string filePath = Path.Combine(saveDirectory, "CoreAI.xml");

            using (StreamWriter op = new StreamWriter(filePath))
            {
                #region Document Header
                XmlTextWriter xml = new XmlTextWriter(op)
                {
                    Formatting = Formatting.Indented,
                    IndentChar = '\t',
                    Indentation = 1
                };

                xml.WriteStartDocument(true);
                xml.WriteStartElement("CoreAI");
                xml.WriteAttributeString("version", ConfigVersion.ToString());
                #endregion Document Header

                #region Settings

                #region Seasons

                xml.WriteCoreXml("TownshipWallCostFactor", TownshipWallCostFactor);

                xml.WriteCoreXml("SeasonsGoldPerPoint", SeasonsGoldPerPoint);

                xml.WriteCoreXml("SeasonsMusicMultiplier", SeasonsMusicMultiplier);
                xml.WriteCoreXml("SeasonsSmithBODMultiplier ", SeasonsSmithBODMultiplier);
                xml.WriteCoreXml("SeasonsTailorBODMultiplier", SeasonsTailorBODMultiplier);
                xml.WriteCoreXml("SeasonsCarpenterBODMultiplier", SeasonsCarpenterBODMultiplier);
                xml.WriteCoreXml("SeasonsPvMMultiplier", SeasonsPvMLootMultiplier);
                xml.WriteCoreXml("SeasonsTheftMultiplier", SeasonsTheftMultiplier);
                xml.WriteCoreXml("SeasonsChestMultiplier", SeasonsChestMultiplier);

                xml.WriteCoreXml("SeasonsPointCap", SeasonsPointCap);

                xml.WriteCoreXml("SeasonsMinimumAwardFactor", SeasonsMinimumAwardFactor);

                xml.WriteCoreXml("SeasonsBpsPreCapSoftStartFraction", SeasonsBpsPreCapSoftStartFraction);

                xml.WriteCoreXml("SeasonsBpsFirstPostCapWidth", SeasonsBpsFirstPostCapWidth);
                xml.WriteCoreXml("SeasonsBpsSecondPostCapWidth", SeasonsBpsSecondPostCapWidth);
                xml.WriteCoreXml("SeasonsBpsThirdPostCapWidth", SeasonsBpsThirdPostCapWidth);

                xml.WriteCoreXml("SeasonsBpsEarlyMultiplier", SeasonsBpsEarlyMultiplier);
                xml.WriteCoreXml("SeasonsBpsPreCapMultiplier", SeasonsBpsPreCapMultiplier);
                xml.WriteCoreXml("SeasonsBpsFirstPostCapMultiplier", SeasonsBpsFirstPostCapMultiplier);
                xml.WriteCoreXml("SeasonsBpsSecondPostCapMultiplier", SeasonsBpsSecondPostCapMultiplier);
                xml.WriteCoreXml("SeasonsBpsThirdPostCapMultiplier", SeasonsBpsThirdPostCapMultiplier);
                xml.WriteCoreXml("SeasonsBpsFinalMultiplier", SeasonsBpsFinalMultiplier);
                #endregion Seasons


                xml.WriteCoreXml("LockdownLevel", (int)LockdownLevel);

                xml.WriteCoreXml("ServerWarsDuration", ServerWarsDuration);
                xml.WriteCoreXml("ServerWarsIgnore", ServerWarsIgnore);

                xml.WriteCoreXml("CriminalGrayMinutes", CriminalGrayMinutes);
                xml.WriteCoreXml("CriminalWhackSeconds", CriminalWhackSeconds);
                xml.WriteCoreXml("CriminalTravelMinutes", CriminalTravelMinutes);

                xml.WriteCoreXml("HarmfulGrayMinutes", HarmfulGrayMinutes);
                xml.WriteCoreXml("HarmfulPvPGrayMinutes", HarmfulPvPGrayMinutes);
                xml.WriteCoreXml("HarmfulWhackSeconds", HarmfulWhackSeconds);
                xml.WriteCoreXml("HarmfulTravelMinutes", HarmfulTravelMinutes);

                xml.WriteCoreXml("AFKGrayMinutes", AFKGrayMinutes);
                xml.WriteCoreXml("AFKWhackMinutes", AFKWhackSeconds);
                xml.WriteCoreXml("AFKTravelMinutes", AFKTravelMinutes);

                xml.WriteCoreXml("ThievingGrayMinutes", ThievingGrayMinutes);
                xml.WriteCoreXml("ThievingWhackSeconds", ThievingWhackSeconds);
                xml.WriteCoreXml("ThievingTravelMinutes", ThievingTravelMinutes);

                xml.WriteCoreXml("StaticCorpseMinutes", StaticCorpseMinutes);

                xml.WriteCoreXml("PostTeleportMoveLockMs", PostTeleportMoveLockMs);

                xml.WriteCoreXml("PermaFlagDuration", PermaFlagDuration);
                xml.WriteCoreXml("StatGainFactor", StatGainFactor);

                xml.WriteCoreXml("PacifyDamageScaleKnob", PacifyDamageScaleKnob);
                xml.WriteCoreXml("PacifyRobustnessBiasKnob", PacifyRobustnessBiasKnob);
                xml.WriteCoreXml("PacifyMusicCenterKnob", PacifyMusicCenterKnob);
                xml.WriteCoreXml("PacifyMusicDampenStrengthKnob", PacifyMusicDampenStrengthKnob);
                xml.WriteCoreXml("PacifyTimeWeakeningKnob", PacifyTimeWeakeningKnob);

                if (Core.RuleSets.AllShards)
                {
                    xml.WriteCoreXml("DailySellGoldLimit", DailySellGoldLimit);
                    xml.WriteCoreXml("CmdAttackCoolMs", CmdAttackCoolMs);
                    xml.WriteCoreXml("CmdComeCoolMs", CmdComeCoolMs);
                    xml.WriteCoreXml("CmdGuardCoolMs", CmdGuardCoolMs);
                    xml.WriteCoreXml("CmdFollowCoolMs", CmdFollowCoolMs);
                    xml.WriteCoreXml("CmdPatrolCoolMs", CmdPatrolCoolMs);
                    xml.WriteCoreXml("CmdDefaultCoolMs", CmdDefaultCoolMs);

                    xml.WriteCoreXml("FeedingCoolMs", FeedingCoolMs);

                    xml.WriteCoreXml("SkillMeditationCoolMs", SkillMeditationCoolMs);
                    xml.WriteCoreXml("SkillDefaultCoolMs", SkillDefaultCoolMs);

                    xml.WriteCoreXml("StatCoolMs", StatCoolMs);

                    xml.WriteCoreXml("HealCoolMs", HealCoolMs);
                    xml.WriteCoreXml("CureCoolMs", CureCoolMs);

                    xml.WriteCoreXml("GaveDamageCoolMs", GaveDamageCoolMs);
                }

                xml.WriteCoreXml("RevealSpellDifficulty", RevealSpellDifficulty.ToString());
                xml.WriteCoreXml("EnablePackUp", EnablePackUp.ToString());

                xml.WriteCoreXml("WatchListShowExceptions", WatchListShowExceptions.ToString());

                xml.WriteCoreXml("StatsThreshold", StatsThreshold);

                xml.WriteCoreXml("TrackRangeBase", TrackRangeBase);
                xml.WriteCoreXml("TrackRangePerSkill", TrackRangePerSkill);
                xml.WriteCoreXml("TrackRangeMin", TrackRangeMin);
                xml.WriteCoreXml("TrackDifficulty", TrackDifficulty.ToString());
                xml.WriteCoreXml("TrackArrowRangeMultiplier", TrackArrowRangeMultiplier);
                xml.WriteCoreXml("TrackHintRangeMultiplier", TrackHintRangeMultiplier);

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
                xml.WriteCoreXml("PoisonBandageSlipChance", PoisonBandageSlipChance);

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

                // V0 persistence - replaced by V1 feature saving
                //xml.WriteCoreXml("DynamicFeatures", DynamicFeatures);

                // tables
                SavePatchTable(xml);
                SaveDynamicFeatures(xml);
                //xml.WriteCoreXml("DynamicPatches", DynamicPatches);

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

                xml.WriteCoreXml("BlankScrollMultiplier", BlankScrollMultiplier);

                // speeds used for pets in Follow/Come/Guard mode
                xml.WriteCoreXml("ActiveSpeedOverride", ActiveSpeedOverride);
                xml.WriteCoreXml("PassiveSpeedOverride", PassiveSpeedOverride);

                xml.WriteCoreXml("ActiveMountedSpeedOverride", ActiveMountedSpeedOverride);
                xml.WriteCoreXml("PassiveMountedSpeedOverride", PassiveMountedSpeedOverride);
                xml.WriteCoreXml("MountEnduranceFactor", MountEnduranceFactor);

                // System accounts. Used for automated tasks that require a mobile
                xml.WriteCoreXml("SystemAccount", SystemAccount);
                xml.WriteCoreXml("AdminAccount", AdminAccount);
                xml.WriteCoreXml("LordBritishAccount", LordBritishAccount);

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
                xml.WriteCoreXml("RangedCorrosionModifier", RangedCorrosionModifier);
                xml.WriteCoreXml("GWRChangeDelayMinutes", GWRChangeDelayMinutes);
                xml.WriteCoreXml("RestockCharges", RestockCharges);
                xml.WriteCoreXml("GracePeriod", GracePeriod);
                xml.WriteCoreXml("ConnectionFloor", ConnectionFloor);
                xml.WriteCoreXml("MaxAccountsPerIP", MaxAccountsPerIP);
                xml.WriteCoreXml("MaxAddresses", MaxConcurrentAddresses);
                xml.WriteCoreXml("MaxAccountsPerMachine", MaxAccountsPerMachine);
                xml.WriteCoreXml("MaxCharactersPerAccount", MaxCharactersPerAccount);

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
                #endregion Settings

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

            string filePath = Path.Combine(
                Core.SavesDirectory,
                Core.Server,
                "CoreAI.xml");

            // Bootstrap legacy installations so the patcher can load
            // patch history before PatchV241 performs the migration.
            if (!File.Exists(filePath))
            {
                string legacyPath = Path.Combine(
                    Core.SavesDirectory,
                    "AngelIsland",
                    "CoreAI.xml");

                if (!File.Exists(legacyPath))
                    return;

                filePath = legacyPath;
            }

            if (!File.Exists(filePath))
                return;

            #region Document Header
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement? root = doc["CoreAI"];
            #endregion Document Header

            #region Settings

            #region Seasons
            TownshipWallCostFactor =
                GetDouble(GetText(root["TownshipWallCostFactor"], ""), TownshipWallCostFactor);

            SeasonsGoldPerPoint =
                GetDouble(GetText(root["SeasonsGoldPerPoint"], ""), SeasonsGoldPerPoint);

            SeasonsMusicMultiplier =
                GetDouble(GetText(root["SeasonsMusicMultiplier"], ""), SeasonsMusicMultiplier);

            SeasonsSmithBODMultiplier =
                GetDouble(GetText(root["SeasonsSmithBODMultiplier "], ""), SeasonsSmithBODMultiplier);

            SeasonsTailorBODMultiplier =
                GetDouble(GetText(root["SeasonsTailorBODMultiplier"], ""), SeasonsTailorBODMultiplier);

            SeasonsCarpenterBODMultiplier =
                GetDouble(GetText(root["SeasonsCarpenterBODMultiplier"], ""), SeasonsCarpenterBODMultiplier);

            SeasonsPvMLootMultiplier =
                GetDouble(GetText(root["SeasonsPvMMultiplier"], ""), SeasonsPvMLootMultiplier);

            SeasonsTheftMultiplier =
                GetDouble(GetText(root["SeasonsTheftMultiplier"], ""), SeasonsTheftMultiplier);

            SeasonsChestMultiplier =
                GetDouble(GetText(root["SeasonsChestMultiplier"], ""), SeasonsChestMultiplier);

            SeasonsPointCap =
                GetInt32(GetText(root["SeasonsPointCap"], ""), SeasonsPointCap);

            SeasonsMinimumAwardFactor =
                GetDouble(GetText(root["SeasonsMinimumAwardFactor"], ""), SeasonsMinimumAwardFactor);

            SeasonsBpsPreCapSoftStartFraction =
                GetDouble(GetText(root["SeasonsBpsPreCapSoftStartFraction"], ""), SeasonsBpsPreCapSoftStartFraction);

            SeasonsBpsFirstPostCapWidth =
                GetInt32(GetText(root["SeasonsBpsFirstPostCapWidth"], ""), SeasonsBpsFirstPostCapWidth);

            SeasonsBpsSecondPostCapWidth =
                GetInt32(GetText(root["SeasonsBpsSecondPostCapWidth"], ""), SeasonsBpsSecondPostCapWidth);

            SeasonsBpsThirdPostCapWidth =
                GetInt32(GetText(root["SeasonsBpsThirdPostCapWidth"], ""), SeasonsBpsThirdPostCapWidth);

            SeasonsBpsEarlyMultiplier =
                GetDouble(GetText(root["SeasonsBpsEarlyMultiplier"], ""), SeasonsBpsEarlyMultiplier);

            SeasonsBpsPreCapMultiplier =
                GetDouble(GetText(root["SeasonsBpsPreCapMultiplier"], ""), SeasonsBpsPreCapMultiplier);

            SeasonsBpsFirstPostCapMultiplier =
                GetDouble(GetText(root["SeasonsBpsFirstPostCapMultiplier"], ""), SeasonsBpsFirstPostCapMultiplier);

            SeasonsBpsSecondPostCapMultiplier =
                GetDouble(GetText(root["SeasonsBpsSecondPostCapMultiplier"], ""), SeasonsBpsSecondPostCapMultiplier);

            SeasonsBpsThirdPostCapMultiplier =
                GetDouble(GetText(root["SeasonsBpsThirdPostCapMultiplier"], ""), SeasonsBpsThirdPostCapMultiplier);

            SeasonsBpsFinalMultiplier =
                GetDouble(GetText(root["SeasonsBpsFinalMultiplier"], ""), SeasonsBpsFinalMultiplier);
            #endregion Seasons

            LockdownLevel = (AccessLevel)GetInt32(GetText(root["LockdownLevel"], ""), (int)LockdownLevel);

            ServerWarsDuration = GetTimeSpan(GetText(root["ServerWarsDuration"], ""), ServerWarsDuration);
            ServerWarsIgnore = GetBool(root["ServerWarsIgnore"], ServerWarsIgnore);

            CriminalGrayMinutes = GetInt32(GetText(root["CriminalGrayMinutes"], ""), CriminalGrayMinutes);
            CriminalWhackSeconds = GetInt32(GetText(root["CriminalWhackSeconds"], ""), CriminalWhackSeconds);
            CriminalTravelMinutes = GetInt32(GetText(root["CriminalTravelMinutes"], ""), CriminalTravelMinutes);

            HarmfulGrayMinutes = GetInt32(GetText(root["HarmfulGrayMinutes"], ""), HarmfulGrayMinutes);
            HarmfulPvPGrayMinutes = GetInt32(GetText(root["HarmfulPvPGrayMinutes"], ""), HarmfulPvPGrayMinutes);
            HarmfulWhackSeconds = GetInt32(GetText(root["HarmfulWhackSeconds"], ""), HarmfulWhackSeconds);
            HarmfulTravelMinutes = GetInt32(GetText(root["HarmfulTravelMinutes"], ""), HarmfulTravelMinutes);

            AFKGrayMinutes = GetInt32(GetText(root["AFKGrayMinutes"], ""), AFKGrayMinutes);
            AFKWhackSeconds = GetInt32(GetText(root["AFKWhackMinutes"], ""), AFKWhackSeconds);
            AFKTravelMinutes = GetInt32(GetText(root["AFKTravelMinutes"], ""), AFKTravelMinutes);

            ThievingGrayMinutes = GetInt32(GetText(root["ThievingGrayMinutes"], ""), ThievingGrayMinutes);
            ThievingWhackSeconds = GetInt32(GetText(root["ThievingWhackSeconds"], ""), ThievingWhackSeconds);
            ThievingTravelMinutes = GetInt32(GetText(root["ThievingTravelMinutes"], ""), ThievingTravelMinutes);

            StaticCorpseMinutes = GetInt32(GetText(root["StaticCorpseMinutes"], ""), StaticCorpseMinutes);

            PostTeleportMoveLockMs = GetDouble(GetText(root["PostTeleportMoveLockMs"], ""), PostTeleportMoveLockMs);

            PermaFlagDuration = GetTimeSpan(GetText(root["PermaFlagDuration"], ""), PermaFlagDuration);

            StatGainFactor = GetDouble(GetText(root["StatGainFactor"], ""), StatGainFactor);

            PacifyDamageScaleKnob = GetDouble(GetText(root["PacifyDamageScaleKnob"], ""), PacifyDamageScaleKnob);
            PacifyRobustnessBiasKnob = GetDouble(GetText(root["PacifyRobustnessBiasKnob"], ""), PacifyRobustnessBiasKnob);
            PacifyMusicCenterKnob = GetDouble(GetText(root["PacifyMusicCenterKnob"], ""), PacifyMusicCenterKnob);
            PacifyMusicDampenStrengthKnob = GetDouble(GetText(root["PacifyMusicDampenStrengthKnob"], ""), PacifyMusicDampenStrengthKnob);
            PacifyTimeWeakeningKnob = GetDouble(GetText(root["PacifyTimeWeakeningKnob"], ""), PacifyTimeWeakeningKnob);

            if (Core.RuleSets.AllShards)
            {
                DailySellGoldLimit = GetInt32(GetText(root["DailySellGoldLimit"], ""), DailySellGoldLimit);
                CmdAttackCoolMs = GetDouble(GetText(root["CmdAttackCoolMs"], ""), CmdAttackCoolMs);
                CmdComeCoolMs = GetDouble(GetText(root["CmdComeCoolMs"], ""), CmdComeCoolMs);
                CmdGuardCoolMs = GetDouble(GetText(root["CmdGuardCoolMs"], ""), CmdGuardCoolMs);
                CmdFollowCoolMs = GetDouble(GetText(root["CmdFollowCoolMs"], ""), CmdFollowCoolMs);
                CmdPatrolCoolMs = GetDouble(GetText(root["CmdPatrolCoolMs"], ""), CmdPatrolCoolMs);
                CmdDefaultCoolMs = GetDouble(GetText(root["CmdDefaultCoolMs"], ""), CmdDefaultCoolMs);

                FeedingCoolMs = GetDouble(GetText(root["FeedingCoolMs"], ""), FeedingCoolMs);

                SkillMeditationCoolMs = GetDouble(GetText(root["SkillMeditationCoolMs"], ""), SkillMeditationCoolMs);
                SkillDefaultCoolMs = GetDouble(GetText(root["SkillDefaultCoolMs"], ""), SkillDefaultCoolMs);

                StatCoolMs = GetDouble(GetText(root["StatCoolMs"], ""), StatCoolMs);

                HealCoolMs = GetDouble(GetText(root["HealCoolMs"], ""), HealCoolMs);
                CureCoolMs = GetDouble(GetText(root["CureCoolMs"], ""), CureCoolMs);

                GaveDamageCoolMs = GetDouble(GetText(root["GaveDamageCoolMs"], ""), GaveDamageCoolMs);
            }

            RevealSpellDifficulty = GetBool(root["RevealSpellDifficulty"], RevealSpellDifficulty);

            EnablePackUp = GetBool(root["EnablePackUp"], EnablePackUp);

            WatchListShowExceptions = GetBool(root["WatchListShowExceptions"], WatchListShowExceptions);

            StatsThreshold = GetInt32(GetText(root["StatsThreshold"], ""), StatsThreshold);

            TrackRangeBase = GetDouble(GetText(root["TrackRangeBase"], ""), TrackRangeBase);
            TrackRangePerSkill = GetDouble(GetText(root["TrackRangePerSkill"], ""), TrackRangePerSkill);
            TrackRangeMin = GetInt32(GetText(root["TrackRangeMin"], ""), TrackRangeMin);
            TrackDifficulty = GetBool(root["TrackDifficulty"], TrackDifficulty);
            TrackArrowRangeMultiplier = GetDouble(GetText(root["TrackArrowRangeMultiplier"], ""), TrackArrowRangeMultiplier);
            TrackHintRangeMultiplier = GetDouble(GetText(root["TrackHintRangeMultiplier"], ""), TrackHintRangeMultiplier);

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
            PoisonBandageSlipChance = GetDouble(GetText(root["PoisonBandageSlipChance"], ""), PoisonBandageSlipChance);

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

            // V0 persistence - replaced by V1 feature loading
            //DynamicFeatures = GetUInt64(GetText(root["DynamicFeatures"], "0"), DynamicFeatures);

            // tables            
            LoadPatchTable(root);
            LoadDynamicFeatures(root);

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

            BlankScrollMultiplier = GetValue(root["BlankScrollMultiplier"], BlankScrollMultiplier);

            ActiveSpeedOverride = GetDouble(GetText(root["ActiveSpeedOverride"], ""), ActiveSpeedOverride);
            PassiveSpeedOverride = GetDouble(GetText(root["PassiveSpeedOverride"], ""), PassiveSpeedOverride);

            ActiveMountedSpeedOverride = GetDouble(GetText(root["ActiveMountedSpeedOverride"], ""), ActiveMountedSpeedOverride);
            PassiveMountedSpeedOverride = GetDouble(GetText(root["PassiveMountedSpeedOverride"], ""), PassiveMountedSpeedOverride);
            MountEnduranceFactor = GetDouble(GetText(root["MountEnduranceFactor"], ""), MountEnduranceFactor);

            SystemAccount = GetValue(root["SystemAccount"], SystemAccount);
            AdminAccount = GetValue(root["AdminAccount"], AdminAccount);
            LordBritishAccount = GetValue(root["LordBritishAccount"], LordBritishAccount);

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

            // chance to broadcast newly IDOC houses over TownCrierBulletinSystem
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
            MaxCharactersPerAccount = GetInt32(GetText(root["MaxCharactersPerAccount"], MaxCharactersPerAccount.ToString()), 0);

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
            Utility.Monitor.WriteLine("[MaxCharactersPerAccount {0}.]", Core.ConsoleColorInformational(), CoreAI.MaxCharactersPerAccount);
            Utility.Monitor.WriteLine("[Force GMN CUO {0}.]", Core.ConsoleColorInformational(), CoreAI.ForceGMNCUO);
            Utility.Monitor.WriteLine("[Warn GMN CUO {0}.]", Core.ConsoleColorInformational(), CoreAI.WarnBadGMNCUO);
            Utility.Monitor.WriteLine("[Force GMN Razor {0}.]", Core.ConsoleColorInformational(), CoreAI.ForceGMNRZR);
            Utility.Monitor.WriteLine("[Warn GMN Razor {0}.]", Core.ConsoleColorInformational(), CoreAI.WarnBadGMNRZR);
            #endregion Settings

            if (AccountHandler.LockdownLevel > AccessLevel.Player)
                Utility.Monitor.WriteLine($"[The shard is currently in lockdown mode ({AccountHandler.LockdownLevel})]", Core.ConsoleColorWarning());
        }
        public static TimeSpan GetTimeSpan(string value, TimeSpan defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            value = value.Trim();

            // 1) Prefer .NET TimeSpan formats first (handles "1.00:00:00", "24:00:00", "0:30:00", etc.)
            // "c" is the round-trippable constant format (default TimeSpan.ToString()).
            if (TimeSpan.TryParseExact(value, "c", CultureInfo.InvariantCulture, out TimeSpan ts))
                return ts;

            // Try general invariant parsing (accepts a broader set, still no exceptions).
            if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out ts))
                return ts;

            // 2) Try ISO 8601 / XML duration (e.g. "PT24H", "PT30M", "P1DT2H").
            // XmlConvert has no Try* API, so prefilter to avoid using exceptions for control flow.
            // ISO durations typically start with 'P' or "-P".
            bool looksIsoDuration =
                value.Length > 0 &&
                (value[0] == 'P' || (value.Length > 1 && value[0] == '-' && value[1] == 'P'));

            if (looksIsoDuration)
            {
                try
                {
                    return XmlConvert.ToTimeSpan(value);
                }
                catch (FormatException)
                {
                    // Fall through to numeric parse / default.
                }
            }

            // 3) Try simple numeric hours (common in config files)
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double hours))
                return TimeSpan.FromHours(hours);

            return defaultValue;
        }

        public static int GetValue(XmlElement? node, int defaultValue)
        {
            return GetInt32(GetText(node, defaultValue.ToString()), defaultValue);
        }

        public static string GetText(XmlElement? node, string defaultValue)
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
        #region Features
        private static void LoadDynamicFeatures(XmlElement root)
        {
            Array.Clear(DynamicFeatures, 0, DynamicFeatures.Length);

            XmlElement? features = root["DynamicFeatures"];

            if (features == null)
                return;

            foreach (XmlElement node in features.GetElementsByTagName("Feature"))
            {
                FeatureBits feature;

                if (Enum.TryParse(node.InnerText, out feature))
                    DynamicFeatures[(int)feature] = true;
            }
        }
        private static void SaveDynamicFeatures(XmlTextWriter xml)
        {
            xml.WriteStartElement("DynamicFeatures");

            foreach (FeatureBits feature in Enum.GetValues(typeof(FeatureBits)))
            {
                if (!IsDynamicFeatureSet(feature))
                    continue;

                xml.WriteElementString("Feature", feature.ToString());
            }

            xml.WriteEndElement();
        }
        #endregion Features
        #region World Size
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
        #endregion World Size
    }
    /// <summary>
    /// Handles version migration of the persisted CoreAI configuration.
    ///
    /// CoreAIPatcher runs before CoreAI.OnLoad() and upgrades older CoreAI.xml
    /// formats to the current format. CoreAI itself therefore only needs to
    /// understand the current configuration format.
    ///
    /// Dynamic features are stored internally by CoreAI as an ordinal FeatureBits
    /// enum backed by a bool array, and are persisted by enum name. This removes
    /// the former UInt64/bit-mask limitation.
    ///
    /// Adding a new dynamic feature:
    ///
    ///     1. Append the new value to the END of CoreAI.FeatureBits.
    ///     2. Use CoreAI.IsDynamicFeatureSet(), SetDynamicFeature(), and
    ///        ClearDynamicFeature() to access it.
    ///     3. No CoreAI.xml version change or migration is required.
    ///
    /// Compatibility: V1 files are read and written by feature NAME, so a
    /// renamed or removed value would silently drop a saved setting. The
    /// V0 upgrade below maps each legacy bit by ORDINAL, so a reordered value
    /// would be misattributed and a removed one rejected as unknown.
    /// Existing FeatureBits values must therefore never be renamed,
    /// reordered, removed, or reused.
    ///
    /// Retiring a feature:
    ///
    ///     - keep its original name and its original position (ordinal);
    ///     - mark it [Obsolete("Retired ... Retained for FeatureBits ordinal
    ///       compatibility; do not reuse.")];
    ///     - remove its remaining source references rather than suppressing
    ///       the resulting CS0618 warnings.
    ///
    /// A retired value still parses from CoreAI.xml and still upgrades from
    /// V0 data. Its slot is never reused; new features are always appended.
    ///
    /// CoreAI.xml version history:
    ///
    ///     V0 - DynamicFeatures stored as a UInt64 bitfield.
    ///     V1 - DynamicFeatures stored as named Feature elements.
    ///
    /// Configuration migrations belong here, not in CoreAI.OnLoad().
    /// </summary>
    public static class CoreAIPatcher
    {
        [CallPriority(1)] // must come before coreai.xml is loaded
        public static void Configure()
        {
            EventSink.PreWorldLoad += new PreWorldLoadEventHandler(OnPatch);
        }
        private static void OnPatch()
        {
            string? filePath = GetCoreAIFilePath();

            if (filePath == null)
                return;

            int version = GetVersion(filePath);

            Console.WriteLine(
                "CoreAIPatcher: CoreAI.xml version {0}, current version {1}.",
                version,
                CoreAI.ConfigVersion);

            // Current version -- nothing to do.
            if (version == CoreAI.ConfigVersion)
                return;

            // Never attempt to load a configuration newer than we understand.
            if (version > CoreAI.ConfigVersion)
            {
                throw new InvalidDataException(
                    string.Format(
                        "CoreAI.xml version {0} is newer than supported version {1}.",
                        version,
                        CoreAI.ConfigVersion));
            }

            Console.WriteLine("CoreAIPatcher: CoreAI.xml requires upgrading.");

            if (version == 0)
                UpgradeV0ToV1(filePath);
        }
        private static void UpgradeV0ToV1(string filePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement? root = doc["CoreAI"];

            if (root == null)
                throw new InvalidDataException(
                    "Invalid CoreAI.xml: missing CoreAI root element.");

            XmlElement? features = root["DynamicFeatures"];

            if (features == null)
                throw new InvalidDataException(
                    "Invalid V0 CoreAI.xml: missing DynamicFeatures element.");

            ulong legacyBits;

            if (!ulong.TryParse(features.InnerText, out legacyBits))
                throw new InvalidDataException(
                    "Invalid V0 CoreAI.xml: invalid DynamicFeatures value.");

            //
            // Convert the legacy UInt64 bitfield into named features.
            //
            features.RemoveAll();

            for (int i = 0; i < 64; i++)
            {
                if ((legacyBits & (1UL << i)) == 0)
                    continue;

                CoreAI.FeatureBits feature = (CoreAI.FeatureBits)i;

                if (!Enum.IsDefined(typeof(CoreAI.FeatureBits), feature))
                    throw new InvalidDataException(
                        string.Format("V0 CoreAI.xml contains unknown feature bit {0}.", i));

                XmlElement node = doc.CreateElement("Feature");
                node.InnerText = feature.ToString();

                features.AppendChild(node);
            }

            //
            // This document is now V1.
            //
            root.SetAttribute("version", "1");

            SaveUpgrade(doc, filePath);

            Console.WriteLine("CoreAIPatcher: CoreAI.xml upgraded to version 1.");
        }
        private static void SaveUpgrade(XmlDocument doc, string filePath)
        {
            string newPath = filePath + ".new";
            string backupPath = filePath + ".v0.bak";

            //
            // Write the complete replacement first.
            //
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t"
            };

            using (XmlWriter writer = XmlWriter.Create(newPath, settings))
                doc.Save(writer);

            //
            // Preserve the original V0 file.
            //
            if (File.Exists(backupPath))
                File.Delete(backupPath);

            File.Replace(newPath, filePath, backupPath);
        }
        private static string? GetCoreAIFilePath()
        {
            string filePath = Path.Combine(
                Core.SavesDirectory,
                Core.Server,
                "CoreAI.xml");

            if (File.Exists(filePath))
                return filePath;

            // Legacy location
            string legacyPath = Path.Combine(
                Core.SavesDirectory,
                "AngelIsland",
                "CoreAI.xml");

            if (File.Exists(legacyPath))
                return legacyPath;

            return null;
        }
        private static int GetVersion(string filePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement? root = doc["CoreAI"];

            if (root == null)
                throw new InvalidDataException("Invalid CoreAI.xml: missing CoreAI root element.");

            string text = root.GetAttribute("version");

            if (string.IsNullOrEmpty(text))
                return 0;

            int version;

            if (!int.TryParse(text, out version))
                throw new InvalidDataException("Invalid CoreAI.xml version.");

            return version;
        }
    }
}