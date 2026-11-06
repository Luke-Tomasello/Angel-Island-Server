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

/* Scripts\Engines\TerritoryControl\TerritoryConfig.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See TerritorySystem for an architectural overview
 */

using Server.Diagnostics;
using System;
using System.IO;

namespace Server.Engines.TerritoryControl
{
    public static class TerritoryConfig
    {
        private static readonly string FilePath = Path.Combine("Saves", Core.Server, "TerritoryConfig.bin");
        private static bool _isLoaded = false; // not serialized

        [CommandProperty(AccessLevel.Administrator)]
        public static bool Enabled
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.TerritoriesEnabled);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.TerritoriesEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.TerritoriesEnabled);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static int DefaultSpawnerKillsNeeded { get; set; } = 10;

        [CommandProperty(AccessLevel.Administrator)]
        public static int MiniBossKillInterval { get; set; } = 150;

        [CommandProperty(AccessLevel.Administrator)]
        public static bool DynamicGuardianCount { get; set; } = true;

        // Respawn timing per style
        [CommandProperty(AccessLevel.Administrator)]
        public static TimeSpan GuardianSwarmMinDelay { get; set; } = TimeSpan.FromSeconds(1);

        [CommandProperty(AccessLevel.Administrator)]
        public static TimeSpan GuardianSwarmMaxDelay { get; set; } = TimeSpan.FromSeconds(2);

        [CommandProperty(AccessLevel.Administrator)]
        public static TimeSpan GuardianEliteMinDelay { get; set; } = TimeSpan.FromSeconds(3);

        [CommandProperty(AccessLevel.Administrator)]
        public static TimeSpan GuardianEliteMaxDelay { get; set; } = TimeSpan.FromSeconds(5);

        [CommandProperty(AccessLevel.Administrator)]
        public static TimeSpan DefaultLevelRestartDelay { get; set; } = TimeSpan.FromMinutes(5);

        [CommandProperty(AccessLevel.Administrator)]
        public static double InvasionIntensity { get; set; } = 1.0;

        /* HighEndAvgPowerThreshold and HighEndTotalPowerThreshold
         * Controls when an anchor spawn is treated as an Elite encounter
         * instead of a normal Swarm encounter.
         *
         * HighEndAvgPowerThreshold:
         * Used to identify individually powerful creatures.
         * If the average power per creature exceeds this value,
         * the anchor becomes Elite.
         *
         * Examples:
         * - Dragons
         * - Drakes
         * - Daemons
         * - Ancient creatures
         *
         * Raising this value prevents moderately strong creatures
         * such as town guards from being classified as Elite.
         *
         * HighEndTotalPowerThreshold:
         * Secondary safeguard for very small but dangerous spawns.
         * If a spawn has 1-2 creatures and the total combined power
         * exceeds this value, it is treated as Elite even if the
         * average-power logic changes in the future.
         *
         * This helps ensure solitary apex creatures still generate
         * elite-style invasion groups.
         */
        [CommandProperty(AccessLevel.Administrator)]
        public static double HighEndAvgPowerThreshold { get; set; } = 140.0;

        [CommandProperty(AccessLevel.Administrator)]
        public static double HighEndTotalPowerThreshold { get; set; } = 150.0;

        // not serialized
        [CommandProperty(AccessLevel.Administrator)]
        public static bool IsLoaded => _isLoaded;

        public static void Save()
        {
            Console.WriteLine("[TerritoryConfig] Saving...");
            Utility.EnsurePath(FilePath);

            BinaryFileWriter? writer = null;
            try
            {
                writer = new BinaryFileWriter(FilePath, true);
                writer.Write(1); // version

                // Config values
                writer.Write(DefaultSpawnerKillsNeeded);
                writer.Write(MiniBossKillInterval);
                writer.Write(DynamicGuardianCount);
                writer.Write(GuardianSwarmMinDelay.TotalSeconds);
                writer.Write(GuardianSwarmMaxDelay.TotalSeconds);
                writer.Write(GuardianEliteMinDelay.TotalSeconds);
                writer.Write(GuardianEliteMaxDelay.TotalSeconds);
                writer.Write(DefaultLevelRestartDelay.TotalMinutes);
                writer.Write(InvasionIntensity);
                writer.Write(HighEndAvgPowerThreshold);
                writer.Write(HighEndTotalPowerThreshold);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[TerritoryConfig] Error saving TerritoryConfig.bin");
                LogHelper.LogException(ex);
            }
            finally
            {
                writer?.Close();
            }
        }

        public static void Load()
        {
            _isLoaded = false;

            if (!File.Exists(FilePath))
            {
                Console.WriteLine("[TerritoryConfig] No save file found. Using defaults.");
                _isLoaded = true;                 // Defaults are valid
                return;
            }

            Console.WriteLine("[TerritoryConfig] Loading...");

            BinaryFileReader? reader = null;
            try
            {
                reader = new BinaryFileReader(new BinaryReader(new FileStream(FilePath, FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        DefaultSpawnerKillsNeeded = reader.ReadInt();
                        MiniBossKillInterval = reader.ReadInt();
                        DynamicGuardianCount = reader.ReadBool();
                        GuardianSwarmMinDelay = TimeSpan.FromSeconds(reader.ReadDouble());
                        GuardianSwarmMaxDelay = TimeSpan.FromSeconds(reader.ReadDouble());
                        GuardianEliteMinDelay = TimeSpan.FromSeconds(reader.ReadDouble());
                        GuardianEliteMaxDelay = TimeSpan.FromSeconds(reader.ReadDouble());
                        DefaultLevelRestartDelay = TimeSpan.FromMinutes(reader.ReadDouble());
                        InvasionIntensity = reader.ReadDouble();
                        HighEndAvgPowerThreshold = reader.ReadDouble();
                        HighEndTotalPowerThreshold = reader.ReadDouble();
                        break;
                }

                _isLoaded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[TerritoryConfig] Error loading TerritoryConfig.bin");
                LogHelper.LogException(ex);
            }
            finally
            {
                reader?.Close();
            }
        }

        public static void Initialize()
        {
            Console.WriteLine($"[TerritoryConfig] Enabled = {Enabled}, Intensity = {InvasionIntensity:F2}x");
        }
    }
}