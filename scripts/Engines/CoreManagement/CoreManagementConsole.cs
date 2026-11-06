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

/* Engines/CoreManagement/CoreManagementConsole.cs
 * ChangeLog
 *  11/26/2023, Adam (RandomizeDecay())
 *      When we are turning off the global FreezeHouseDecay, we now RandomizeDecay for each house 
 *          so that we don't get 50 houses all dropping at exactly the same time. (since they all would have been at 15 days.)
 *  5/15/23, Yoar (BolaThrowLock, BolaMountLock)
 *      Added configurable bola lock delays
 *  5/10/23, Yoar (PJUMBountyCap)
 *      Added PJUMBountyCap
 *  5/6/23, Yoar (AreaPeaceCrowdSize, AreaPeaceSoloScalar, AreaPeaceDelayBase, AreaPeaceDelayBonus)
 *      Added configurable area peace delays
 *  5/1/23, Yoar (CharDeleteUnrestricted)
 *      Added CoreAI.CharDeleteUnrestricted: Toggle on/off char deletion restriction
 *  4/26/23, Yoar
 *      Added BaseHitPoisonChance
 *  4/23/23, Yoar
 *      Added ChatEnabled
 *  3/10/22, Yoar
 *      Renamed StaminaDrainScaling to StaminaDrainByWeight
 *  8/12/22, Adam
 *      Add DynamicPatches
 *      See also: CoreAI.xml and main.cs (Initialize)
 *  8/8/22, Adam
 *		MOVE the following out of CoreManagementConsole to ItemManagementConsole
 *		DungeonChestBuryRate, DungeonChestBuryMinLevel
 *		MagicGearDropDowngrade, MagicGearDropChance
 *		SlayerWeaponDropRate, SlayerInstrumentDropRate
 *		DebugDecay
 *		FreezeDryEnabled
 *  8/5/22, Adam
 *      Change MaxAddresses to go through CoreAI.MaxAddresses directly instead of the proxy in IPLimiter
 *  6/27/22: Yoar
 *      Added CoreAI.BaseVendorFee: Base player vendor fee per one UO day (one UO day = 2 RL hours).
 * 11/30/21: Yoar
 *      Removed the MobsThrowSnowballs bit. Only x-mas elfs may throw snowballs.
 *      Added a ShipTracking bit: Enable/disable the ship tracking system.
 * 11/26/21: Yoar
 *      Added a MobsThrowSnowballs bit: Allow mobs to throw snowballs *if* they have a SnowPile in their pack.
 * 11/25/21: Yoar
 *      Added a StaminaDrainScaling bit
 * 11/08/21: Yoar
 *      Added DungeonChestBuryRate
 * 11/08/21: Yoar
 *      Downgraded the access levels of DetectSkillDelay, MagicGearDropDowngrade,
 *      MagicGearDropChance from Owner to Administrator.
 * 10/28/21, Yoar
 *      Moved GSGG to SkillGainControl.
 * 10/24/21, Yoar
 *      Added a BulkOrdersEnabled bit
 * 9/14/21, Yoar
 *      Added DetectSkillDelay for controlling the skill delay of detect hidden
 * 9/10/21, Adam (MagicGearDropChance)
 *  Adding a variable to control the drop of magic weapons and armor globably
 *      This is done to give crafters a chance to sell their magic crafted goods.
 * 7/26/21, Pix
 *      Added ExplPotThrowDelay (which I missed from this addition!)
 *      Added HarvConstX, HarvConstY, HarvConstM (for harvest seeding control)
 *	3/5/16, Adam
 *		Add a new FeatureBits.PlayerAccountWipe for wiping all player accounts as part of usual 
 *			account cleanup logic (it's fully logged.) 
 *		This *should* be what all that is required for a shard wipe...
 *		See implementation in CronTasks.cs
 *	8/20/11, Adam
 *		Add IPBinderEnabled
 *	9/7/10, Adam
 *		Add SeaGypsyUsageReport
 *	8/8/10, Adam
 *		Add: OldFlee switch
 *			BaseAI seems to have a bug where they clear FocusMob in OnActionChanged().Flee
 *	7/23/10, adam
 *		Add SpiritSpeakUsageReport
 *		used to turing on/off SpiritSpeak UsageReports
 *	6/12/10, adam
 *		Add a switch for controling the algo used for calculating armor absorb (ArmorAbsorbClassic)
 *	5/14/10, adam
 *		Update the mindblast delay as per Az recomendations 12/30 to 1.55
 *	5/12/10, adam
 *		Add CoreAI.SlayerWeaponDropRate
 *	4/10/10, adam
 *		Add speed management MCi to tune dragon speeds.
 *	4/9/10, adam
 *		Add a SmartSpellMCi for managing the 8th level default spell behavior of high-level magical mobs
 *	4/4/10, adam
 *		Oops, make the min/max values as per Az recomendations 12/30
 *	3/19/10, adam
 *		Add MindBlastMCi
 *		Change Poison Resistance from Spells.SpellCircle.Third to Spells.SpellCircle.Sixth
 *		This change is based on testing Akarius Alexios, and Answer
 *	3/18/10, adam
 *		Add new stun management console prototype
 *		StunPunchMCi
 *	11/25/08, Adam
 *		Make MaxAddresses a console value
 *			in IPLimiter; controls how many of the same IP address can be concurrently logged in
 *	2/18/04, Adam
 *		Make MaxAccountsPerIP a console value
 *  01/04/08, Pix
 *      Added GWRChangeDelayMinutes setting.
 *  12/9/07, Adam
 *      Added NewPlayerGuild feature bit.
 *	8/26/07 - Pix
 *		Added NewPlayerStartingArea feature bit.
 *	8/1/07, Pix
 *		Added RazorNegFeaturesEnabled and RazorNegWarnAndKick settings.
 *  4/3/07, Adam
 *      Add a BreedingEnabled bit
 *      Add a RTTNotifyEnabled bit
 *	1/08/07 Taran Kain
 *		Changed GSGG lookups to reflect new location in PlayerMobile
 *	1/02/07 Pixie
 *		Added RangedCorrosionModifier.
 *		Added RangedCorrosion featurebit.
 *	10/16/06, Adam
 *		Add flag to disable tower placement
 *	10/16/06, Adam
 *		Add global override for SecurePremises
 *			i.e., CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SecurePremises)
 *	8/24/06, Adam
 *		Added TCAcctCleanupEnable to allow disabling of auto account cleanup
 *  8/19/06, Rhiannon
 *		Added accessors for CoreAI.PlayAccessLevel to control access to the Play command.
 *	8/5/06, weaver
 *		Added HelpStuckDisabled to disable help stuck.
 *	7/23/06, Pix
 *		Added GuildKinChangeDisabled
 *	6/2/06, Adam
 *		Give this console a special hue
 *  3/26/06, Pix
 *		Added IOBJoinEnabled;
 *	1/29/06, Adam
 *		TCAcctCleanupDays; trim all non-staff account and not logged in for N days - heartbeat
 *		Remove CommDeedBatch param
 *  12/28/05, Adam
 *		Add CommDeedBatch to set the number of containers processed per pass
 *  12/13/05 Taran Kain
 *		Added FreezeDryEnabled, removed FreezeDryLogEnabled
 *	12/13/05, Pix
 *		Added DebugDecay
 *	12/01/05 Pix
 *		Added WorldSaveFrequency
 *  10/10/05 Taran Kain
 *		Made ArmorDexLossStrFactor Admin-only access.
 *	10/02/05, erlein
 *		Added ConfusionBaseDelay to control tamed creature confusion.
 *	9/20/05, Adam
 *		a. Add setting for OpposePlayers. This flag modifies the bahavior of IsOpposition()
 *		in BaseCreature such that aligned PLAYERS appear as enimies to NPCs of a different alignment.
 *		b. Add a setting for OpposePlayersPets
 *	9/13/05, erlein
 *		Added MeleePoisonSkillFactor bool to control poison skill factoring in
 *		OnHit() equations of melee weapons.
 *	9/03/05, Adam
 *		Add Global FreezeHouseDecay function - World crisis mode :\
 *	9/02/05, erlein
 *		Added ReaggressIgnoreChance
 *  8/25/05, Taran Kain
 *		Added IDOCBroadcastChance
 *	7/13/05, erlein
 *		Added EScrollChance, EScrollSuccess.
 *	7/6/05, erlein
 *		Fixed Spirit Cohesion factors so accessed via CoreAI.
 *	6/13/05, erlein
 *		Added CohesionLowerDelay, CohesionBaseDelay and CohesionFactor
 *		to control new res delay.
 *	6/7/05, Adam
 *		Make TownCrierWordMinuteCost public via [CommandProperty,,]
 *	06/03/05 Taran Kain
 *		Added TownCrierWordMinuteCost
 *	6/3/05, Adam
 *		Add in ExplosionPotionThreshold to control the tossers
 *		health requirement
 *	4/30/05, Pix
 *		Removed ExplosionPotionAlchemyReduction.
 *	4/28/05, Adam
 *		add ServerWarMinutes to set the length of time for server wars
 *	4/26/05, Pix
 *		Made explode pots targetting method toggleable based on CoreAI/ConsoleManagement setting.
 *	4/23/05, Pix
 *		Added ExplosionPotionAlchemyReduction
 *	4/18/05, Adam
 *		Add TempDouble and TempInt for testing ingame settings
 *	04/18/05, Pix
 *		Added offline short term murder decay (only if it's turned on).
 *		Added potential exploding of carried explosion potions.
 *	4/15/05, Adam
 *		Add TreasureMapDrop as a global variable
 *	4/8/05, Adam
 *		Add handlers for the following CoreAI values
 *		SpiritDepotTRPots, SpiritFirstWaveVirtualArmor, SecondWaveVirtualArmor,
 *		SpiritThirdWaveVirtualArmor, SpiritBossVirtualArmor
 *	3/31/05, Adam
 *		Add the new global switch IOBShardWide to turn on/off IOB globalness
 *	3/7/05, Adam
 *		Add the new DynamicFeatures property so we can turn on/off shard wide features.
 *	2/4/05, Adam
 *		Created.
 *		Management console for the global values stored in Engines/AngelIsland/CoreAI.cs
 *		Manage Server.Misc.SkillCheck.GSGG
 *		Manage CoreAI.PowderOfTranslocationAvail
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

using Server.Diagnostics;
using Server.Engines.CronScheduler;
using Server.Misc;
using Server.Multis;
using System;
using System.Collections.Generic;

namespace Server.Engines.Consoles
{
    public class CoreManagementConsole : BaseConsole
    {
        [Constructable]
        public CoreManagementConsole()
            : base("Core Management Console")
        {
        }

        public CoreManagementConsole(Serial serial)
            : base(serial)
        {
        }

        #region Properties

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public int StaticCorpseMinutes
        {
            get => CoreAI.StaticCorpseMinutes;
            set => CoreAI.StaticCorpseMinutes = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public int StatsThreshold
        {
            get => CoreAI.StatsThreshold;
            set => CoreAI.StatsThreshold = value;
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public double PostTeleportMoveLockMs
        {
            get => CoreAI.PostTeleportMoveLockMs;
            set => CoreAI.PostTeleportMoveLockMs = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public TimeSpan PermaFlagDuration
        {
            get => CoreAI.PermaFlagDuration;
            set => CoreAI.PermaFlagDuration = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool Cooperatives
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.Cooperatives);
            set
            {
                EnableCooperatives(!value);

                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.Cooperatives);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.Cooperatives);
            }
        }

        private void EnableCooperatives(bool stash)
        {
            var signList = new List<Multis.HouseSign>();
            foreach (Item item in World.Items.Values)
            {
                if (item is Multis.HouseSign hs && !hs.Deleted)
                {
                    if ((stash && item.Map == Map.Felucca) || (!stash && item.Map == Map.Internal))
                        if (hs.CooperativeType != Multis.BaseHouse.CooperativeType.None)
                            signList.Add(hs);
                }
            }

            foreach (var hs in signList)
                hs.Structure.StashHouse(stash);
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public bool WatchListShowExceptions
        {
            get => CoreAI.WatchListShowExceptions;
            set => CoreAI.WatchListShowExceptions = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public bool EnablePackUp
        {
            get => CoreAI.EnablePackUp;
            set => CoreAI.EnablePackUp = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool CharDeleteUnrestricted
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.CharDeleteUnrestricted);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.CharDeleteUnrestricted);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.CharDeleteUnrestricted);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool ChatEnabled
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.ChatEnabled);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.ChatEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.ChatEnabled);
            }
        }

        [CommandProperty(AccessLevel.Owner)]
        public int BackupCount
        {
            get => CoreAI.BackupCount;
            set => CoreAI.BackupCount = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public CoreAI.WorldSize CurrentWorldSize
        {
            get => CoreAI.CurrentWorldSize;
            set => CoreAI.CurrentWorldSize = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool IPBinderEnabled
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.IPBinderEnabled);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.IPBinderEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.IPBinderEnabled);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public VisibleDamageType VisibleDamageType
        {
            get => Mobile.VisibleDamageType;
            set => Mobile.VisibleDamageType = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool NewPlayerStartingArea
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.NewPlayerStartingArea);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool NewPlayerGuild
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.NewPlayerGuild);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.NewPlayerGuild);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerGuild);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool RazorNegFeaturesEnabled
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.RazorNegotiateFeaturesEnabled);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.RazorNegotiateFeaturesEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.RazorNegotiateFeaturesEnabled);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool RazorNegWarnAndKick
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.RazorNegotiateWarnAndKick);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.RazorNegotiateWarnAndKick);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.RazorNegotiateWarnAndKick);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool RTTNotifyEnabled
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.RTTNotifyEnabled);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.RTTNotifyEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.RTTNotifyEnabled);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool BreedingEnabled
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.BreedingEnabled);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.BreedingEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.BreedingEnabled);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool TowerAllowed
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.TowerAllowed);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.TowerAllowed);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.TowerAllowed);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool SecurePremises
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SecurePremises);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.SecurePremises);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.SecurePremises);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int TCAcctCleanupDays
        {
            get => CoreAI.TCAcctCleanupDays;
            set => CoreAI.TCAcctCleanupDays = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int WorldSaveFrequency
        {
            get => CoreAI.WorldSaveFrequency;
            set
            {
                try
                {
                    if (Maintenance.Scheduled)
                    {
                        SendMessageToMe("WorldSaveFrequency cannot be changed while maintenance is scheduled or Server Wars are in progress. Cancel maintenance first, or wait until it completes.");
                        return;
                    }

                    if (value <= 0)
                        throw new ArgumentOutOfRangeException(nameof(WorldSaveFrequency), "WorldSaveFrequency must be >= 1 minute.");

                    if (!Cron.TryBuildCronSpecForMinutes(value, out string spec))
                        throw new ArgumentException($"WorldSaveFrequency={value} cannot be expressed as a stable cron schedule. Try values that divide 60 (minutes), divide 24 (hours), or are whole days.");

                    if (!Cron.Update("AutoSave", spec))
                        throw new InvalidOperationException("Failed to update cron job \"AutoSave\". Is it registered?");

                    CoreAI.WorldSaveFrequency = value;

                    if (!AutoSave.SavesEnabled)
                        SendMessageToMe("Notice: auto saves are currently disabled; the new schedule will take effect when saves are re-enabled.");

                    Cron.Flush("AutoSave");

                    SendMessageToMe($"WorldSaveFrequency set to {value} minute{(value == 1 ? "" : "s")}.");
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    SendMessageToMe(ex.Message);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool HelpStuckDisabled
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.HelpStuckDisabled);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.HelpStuckDisabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.HelpStuckDisabled);
            }
        }

        //[CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        //public string DynamicPatches => "0x" + CoreAI.DynamicPatches.ToString("X");

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool InmateEplt01
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.InmateRecallExploitCheck);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.InmateRecallExploitCheck);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.InmateRecallExploitCheck);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool FreezeHouseDecay
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.FreezeHouseDecay);
            set
            {
                if (!value && CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.FreezeHouseDecay))
                    RandomizeDecay();

                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.FreezeHouseDecay);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.FreezeHouseDecay);
            }
        }

        private void RandomizeDecay()
        {
            foreach (BaseHouse bh in BaseHouse.Instances.Keys)
            {
                if (!bh.Deleted && BaseHouse.Instances[bh])
                    bh.DecayMinutesStored += Utility.RandomMinMax(0, 21600); // up to +15 days
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool FreezeAccountDecay
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.FreezeAccountDecay);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.FreezeAccountDecay);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.FreezeAccountDecay);

                if (FreezeHouseDecay && !value)
                    SendMessageNearby("Accounts will not decay because FreezeHouseDecay is true.");
            }
        }

        [CommandProperty(AccessLevel.Owner, AccessLevel.Owner)]
        public bool FreezeTownshipDecay
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.FreezeTownshipDecay);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.FreezeTownshipDecay);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.FreezeTownshipDecay);
            }
        }

        [CommandProperty(AccessLevel.Owner, AccessLevel.Owner)]
        public Serial SystemAccount
        {
            get => CoreAI.SystemAccount;
            set => CoreAI.SystemAccount = value;
        }

        [CommandProperty(AccessLevel.Owner, AccessLevel.Owner)]
        public Serial AdminAccount
        {
            get => CoreAI.AdminAccount;
            set => CoreAI.AdminAccount = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int OfflineShortsHours
        {
            get => CoreAI.OfflineShortsDecayHours;
            set => CoreAI.OfflineShortsDecayHours = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool OfflineShortsDecay
        {
            get => CoreAI.OfflineShortsDecay != 0;
            set => CoreAI.OfflineShortsDecay = value ? 1 : 0;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int ServerWarMinutes
        {
            get => Server.Misc.AutoRestart.ServerWarsMinutes;
            set => Server.Misc.AutoRestart.ServerWarsMinutes = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int TownCrierWordMinuteCost
        {
            get => CoreAI.TownCrierWordMinuteCost;
            set => CoreAI.TownCrierWordMinuteCost = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double IDOCBroadcastChance
        {
            get => CoreAI.IDOCBroadcastChance;
            set => CoreAI.IDOCBroadcastChance = value;
        }

        #endregion

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Administrator)
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}