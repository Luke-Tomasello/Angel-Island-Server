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

/* scripts\Engines\CoreManagement\GameSystemsConsole.cs
 * CHANGELOG:
 *  1/10/23, Yoar
 *      Initial commit.
 */

using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Engines.Consoles
{
    [Flipable(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class GameSystemsConsole : BaseConsole
    {
        [Constructable]
        public GameSystemsConsole()
            : base(0x1F14)
        {
            Hue = 0x482;
            Name = "Game Systems Console";
        }
        public GameSystemsConsole(Serial serial)
            : base(serial)
        {
        }
        // -------------------------------------------------------------------------------
        //
        // -------------------------------------------------------------------------------

        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan ServerWarsDuration
        {
            get
            {
                return CoreAI.ServerWarsDuration;
            }
            set
            {
                CoreAI.ServerWarsDuration = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool ServerWarsIgnore
        {
            get
            {
                return CoreAI.ServerWarsIgnore;
            }
            set
            {
                CoreAI.ServerWarsIgnore = value;
            }
        }

        // Maximum bounty allowed on players.
        [CommandProperty(AccessLevel.Administrator)]
        public int PJUMBountyCap
        {
            get
            {
                return CoreAI.PJUMBountyCap;
            }
            set
            {
                CoreAI.PJUMBountyCap = value;
            }
        }
        // Factor to apply to the NextMove delay for running creatures
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public double RunningWildFactor
        {
            get
            {
                return CoreAI.RunningWildFactor;
            }
            set
            {
                CoreAI.RunningWildFactor = value;
            }
        }

        // Factor to apply to the NextMove delay for running humanoids
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public double RunningHumFactor
        {
            get
            {
                return CoreAI.RunningHumFactor;
            }
            set
            {
                CoreAI.RunningHumFactor = value;
            }
        }

        // Factor to apply to the NextMove delay for running pets
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public double RunningPetFactor
        {
            get
            {
                return CoreAI.RunningPetFactor;
            }
            set
            {
                CoreAI.RunningPetFactor = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool GuildKinChangeDisabled
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.GuildKinChangeDisabled);
            }
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.GuildKinChangeDisabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.GuildKinChangeDisabled);
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool OpposePlayersPets
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.OpposePlayersPets);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.OpposePlayersPets);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.OpposePlayersPets);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool OpposePlayers
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.OpposePlayers);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.OpposePlayers);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.OpposePlayers);
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int CohesionLowerDelay
        {   // Spirit cohesion controls (ghost blindness)
            get
            {
                return CoreAI.CohesionLowerDelay;
            }
            set
            {
                CoreAI.CohesionLowerDelay = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int CohesionFactor
        {   // Spirit cohesion controls (ghost blindness)
            get
            {
                return CoreAI.CohesionFactor;
            }
            set
            {
                CoreAI.CohesionFactor = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int CohesionBaseDelay
        {   // Spirit cohesion controls (ghost blindness)
            get
            {
                return CoreAI.CohesionBaseDelay;
            }
            set
            {
                CoreAI.CohesionBaseDelay = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public double ReaggressIgnoreChance
        {   // Chance for creatures to ignore re-aggressive actions when not aggressing
            // already
            get
            {
                return CoreAI.ReaggressIgnoreChance;
            }
            set
            {
                CoreAI.ReaggressIgnoreChance = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int ConfusionBaseDelay
        {
            get
            {
                return CoreAI.ConfusionBaseDelay;
            }
            set
            {
                CoreAI.ConfusionBaseDelay = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int HarvConstX
        {
            get
            {
                return CoreAI.HarvestConstantX;
            }
            set
            {
                CoreAI.HarvestConstantX = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int HarvConstY
        {
            get
            {
                return CoreAI.HarvestConstantY;
            }
            set
            {
                CoreAI.HarvestConstantY = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int HarvConstM
        {
            get
            {
                return CoreAI.HarvestConstantM;
            }
            set
            {
                CoreAI.HarvestConstantM = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool ZoraSystem
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.ZoraSystem);
            }
            set
            {
                EnableZoraSystem(value);

                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.ZoraSystem);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.ZoraSystem);
            }
        }
        /*
         * Support functionality for above settings
         */
        private void EnableZoraSystem(bool on)
        {
            List<Mobile> list = ZoraSystemLinks();
            bool zoraSystemExists = list.Count > 0;
            if (on == true)
            {
                if (zoraSystemExists)
                {   // do nothing, we want zora and she's already here
                }
                else if (!zoraSystemExists)
                {   // zora doesn't exist, spawn her
                    // Can't use SpawnerCache here since the zora spawner is never 'running'
                    foreach (Item item in World.Items.Values)
                    {
                        if (item == null) continue;
                        if (item is Spawner sp)
                        {
                            if (sp == null) continue;
                            if (sp.Serial == 0x400025e9)
                                Console.WriteLine();
                            if (sp.ObjectNames == null) continue;
                            foreach (string name in sp.ObjectNames)
                            {
                                if (name.ToLower() == "zora")
                                {
                                    sp.Respawn();
                                }
                            }
                        }
                    }
                }
            }
            else // off
            {
                if (!zoraSystemExists)
                {   // do nothing, we don't want zora and she doesn't exist
                }
                else // zoraSystemExists == true
                {   // we have zora(s) but we don't want her
                    foreach (Mobile zora in list)
                    {
                        if (zora is BaseCreature bc)
                        {
                            if (bc == null) continue;
                            if (bc.Spawner != null)
                                bc.Spawner.Running = false;   // should never be the case with Zora
                            if (bc.Deleted == false)
                                bc.Delete();
                        }
                    }
                }
            }
        }
        private List<Mobile> ZoraSystemLinks()
        {
            Type? tx = ScriptCompiler.FindTypeByName("zora");
            List<Mobile> list = new List<Mobile>();
            if (tx != null)
            {
                foreach (Mobile mob in World.Mobiles.Values)
                {
                    if (mob != null && !mob.Deleted && tx.IsAssignableFrom(mob.GetType()))
                    {
                        list.Add(mob);
                    }
                }
            }
            else
            {
                ;
            }
            return list;
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool MagicCraftSystem
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.MagicCraftSystem);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.MagicCraftSystem);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.MagicCraftSystem);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool MagicGearThrottleEnabled
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.MagicGearThrottleEnabled);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.MagicGearThrottleEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.MagicGearThrottleEnabled);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int DungeonChestBuryRate
        {
            get
            {
                return CoreAI.DungeonChestBuryRate;
            }
            set
            {
                CoreAI.DungeonChestBuryRate = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int DungeonChestBuryMinLevel
        {
            get
            {
                return CoreAI.DungeonChestBuryMinLevel;
            }
            set
            {
                CoreAI.DungeonChestBuryMinLevel = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int MagicGearDropDowngrade
        {
            get
            {
                return CoreAI.MagicGearDropDowngrade;
            }
            set
            {
                CoreAI.MagicGearDropDowngrade = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int MagicGearDropChance
        {
            get
            {
                return CoreAI.MagicGearDropChance;
            }
            set
            {
                CoreAI.MagicGearDropChance = value;
            }
        }

        /*  Slayer Instruments
            No — not present in Publish 15.
            Slayer instruments were introduced later, with Age of Shadows (Publish 16).
            They were added alongside the rework of Bard mechanics (Discordance, Peacemaking, Provocation scaling).
            In Publish 15:
            Instruments had durability
            Success was skill-based
            No Slayer bonuses existed for instruments
        */
        [CommandProperty(AccessLevel.Administrator)]
        public double SlayerInstrumentDropRate
        {
            get
            {
                return CoreAI.SlayerInstrumentDropRate;
            }
            set
            {
                CoreAI.SlayerInstrumentDropRate = value;
            }
        }

        /* Slayer Weapons
            Ultima Online – Publish 15 (January 9, 2002)
            Slayer weapons (e.g., Repond, Undead, Elemental, etc.) existed well before Pub 15.
            They were part of the classic weapon loot system throughout late T2A and Renaissance.
            By Publish 15, Slayer weapons were fully established and commonly encountered.
        */
        [CommandProperty(AccessLevel.Administrator)]
        public double SlayerWeaponDropRate
        {
            get
            {
                return CoreAI.SlayerWeaponDropRate;
            }
            set
            {
                CoreAI.SlayerWeaponDropRate = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double SiegeGearDropFactor
        {
            get
            {
                return CoreAI.SiegeGearDropFactor;
            }
            set
            {
                CoreAI.SiegeGearDropFactor = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double EnchantedEquipmentDropRate
        {
            get
            {
                return CoreAI.EnchantedEquipmentDropRate;
            }
            set
            {
                CoreAI.EnchantedEquipmentDropRate = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool OldFlee
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.OldFlee);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.OldFlee);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.OldFlee);
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public double MagicWandDropChance
        {
            get
            {
                return CoreAI.MagicWandDropChance;
            }
            set
            {
                CoreAI.MagicWandDropChance = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool SpiritSpeakUsageReport
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SpiritSpeakUsageReport);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.SpiritSpeakUsageReport);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.SpiritSpeakUsageReport);
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public double EnchantedScrollDropChance
        {
            get
            {
                return CoreAI.EnchantedScrollDropChance;
            }
            set
            {
                CoreAI.EnchantedScrollDropChance = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public int BlankScrollMultiplier
        {
            get
            {
                return CoreAI.BlankScrollMultiplier;
            }
            set
            {
                CoreAI.BlankScrollMultiplier = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool RangedCorrosion
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.RangedCorrosion);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.RangedCorrosion);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.RangedCorrosion);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int RangedCorrosionModifier
        {
            get
            {
                return CoreAI.RangedCorrosionModifier;
            }
            set
            {
                CoreAI.RangedCorrosionModifier = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public double ExplPotThrowDelay
        {
            get
            {
                return CoreAI.ExplosionPotionThrowDelay;
            }
            set
            {
                CoreAI.ExplosionPotionThrowDelay = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public CoreAI.EPTM ExplPotTargetMethod
        {
            get
            {
                return CoreAI.ExplosionPotionTargetMethod;
            }
            set
            {
                CoreAI.ExplosionPotionTargetMethod = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int ExplPotSensitivity
        {
            get
            {
                return CoreAI.ExplosionPotionSensitivityLevel;
            }
            set
            {
                if (value > 0) //ignore negative numbers, they don't make sense
                {
                    CoreAI.ExplosionPotionSensitivityLevel = value;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public double ExplPotChance
        {
            get
            {
                return CoreAI.ExplosionPotionChance;
            }
            set
            {
                if (value >= 0.0 && value <= 1.0) //only valid values are 0.0-1.0
                {
                    CoreAI.ExplosionPotionChance = value;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public double ExplPotHealthThreshold
        {
            get
            {
                return CoreAI.ExplosionPotionThreshold;
            }
            set
            {
                if (value >= 0.0 && value <= 1.0) //only valid values are 0.0-1.0
                {
                    CoreAI.ExplosionPotionThreshold = value;
                }
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public double TreasureMapDrop
        {
            get
            {
                return CoreAI.TreasureMapDrop;
            }
            set
            {
                CoreAI.TreasureMapDrop = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool ShipTracking
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.ShipTracking);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.ShipTracking);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.ShipTracking);
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool StaminaDrainByWeight
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.StaminaDrainByWeight);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.StaminaDrainByWeight);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.StaminaDrainByWeight);
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool MeleePoisonSkillFactor
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.MeleePoisonSkillFactor);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.MeleePoisonSkillFactor);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.MeleePoisonSkillFactor);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool BulkOrdersEnabled
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.BulkOrdersEnabled);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.BulkOrdersEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.BulkOrdersEnabled);
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public double PowderOfTranslocation
        {
            get
            {
                return CoreAI.PowderOfTranslocationAvail;
            }
            set
            {
                CoreAI.PowderOfTranslocationAvail = value;
            }
        }
        /// <summary>
        /// StandingDelay: denotes the minimum time (in seconds) an archer must stand still
        /// before being able to fire.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public double StandingDelay
        {
            get
            {
                return CoreAI.StandingDelay;
            }
            set
            {
                CoreAI.StandingDelay = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public AccessLevel PlayAccessLevel
        {   // Control access level of Play command
            get
            {
                return CoreAI.PlayAccessLevel;
            }
            set
            {
                CoreAI.PlayAccessLevel = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int GWRChangeDelayMinutes
        {   // Guild War Ring change setting delay
            get
            {
                return CoreAI.GWRChangeDelayMinutes;
            }
            set
            {
                CoreAI.GWRChangeDelayMinutes = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool LogStableCharges
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.LogStableCharges);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.LogStableCharges);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.LogStableCharges);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool PetNeedsLOS
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.PetNeedsLOS);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.PetNeedsLOS);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.PetNeedsLOS);
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool SeaGypsyUsageReport
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SeaGypsyUsageReport);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.SeaGypsyUsageReport);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.SeaGypsyUsageReport);
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool TreasureMapUsageReport
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.TreasureMapUsageReport);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.TreasureMapUsageReport);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.TreasureMapUsageReport);
            }
        }

        // -------------------------------------------------------------------------------
        //
        // -------------------------------------------------------------------------------
        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            reader.ReadEncodedInt();
        }

    }
}