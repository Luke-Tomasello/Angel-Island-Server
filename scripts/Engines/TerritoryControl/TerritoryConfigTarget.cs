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

/* Scripts\Engines\TerritoryControl\TerritoryConfigTarget.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See TerritorySystem for an architectural overview
 */

using System;

namespace Server.Engines.TerritoryControl
{
    public class TerritoryConfigTarget
    {
        [CommandProperty(AccessLevel.Administrator)]
        public bool Enabled
        {
            get => TerritoryConfig.Enabled;
            set => TerritoryConfig.Enabled = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int DefaultSpawnerKillsNeeded
        {
            get => TerritoryConfig.DefaultSpawnerKillsNeeded;
            set => TerritoryConfig.DefaultSpawnerKillsNeeded = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int MiniBossKillInterval
        {
            get => TerritoryConfig.MiniBossKillInterval;
            set => TerritoryConfig.MiniBossKillInterval = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool DynamicGuardianCount
        {
            get => TerritoryConfig.DynamicGuardianCount;
            set => TerritoryConfig.DynamicGuardianCount = value;
        }

        // Respawn timing per style
        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan GuardianSwarmMinDelay
        {
            get => TerritoryConfig.GuardianSwarmMinDelay;
            set => TerritoryConfig.GuardianSwarmMinDelay = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan GuardianSwarmMaxDelay
        {
            get => TerritoryConfig.GuardianSwarmMaxDelay;
            set => TerritoryConfig.GuardianSwarmMaxDelay = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan GuardianEliteMinDelay
        {
            get => TerritoryConfig.GuardianEliteMinDelay;
            set => TerritoryConfig.GuardianEliteMinDelay = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan GuardianEliteMaxDelay
        {
            get => TerritoryConfig.GuardianEliteMaxDelay;
            set => TerritoryConfig.GuardianEliteMaxDelay = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan DefaultLevelRestartDelay
        {
            get => TerritoryConfig.DefaultLevelRestartDelay;
            set => TerritoryConfig.DefaultLevelRestartDelay = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double InvasionIntensity
        {
            get => TerritoryConfig.InvasionIntensity;
            set => TerritoryConfig.InvasionIntensity = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double HighEndAvgPowerThreshold
        {
            get => TerritoryConfig.HighEndAvgPowerThreshold;
            set => TerritoryConfig.HighEndAvgPowerThreshold = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double HighEndTotalPowerThreshold
        {
            get => TerritoryConfig.HighEndTotalPowerThreshold;
            set => TerritoryConfig.HighEndTotalPowerThreshold = value;
        }

        public override string ToString() => "Territory Control Configuration";
    }
}