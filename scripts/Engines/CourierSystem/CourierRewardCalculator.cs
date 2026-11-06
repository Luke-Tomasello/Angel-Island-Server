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

/*
 * Courier Reward Philosophy
 *
 * The reward paid for a courier job attempts to model the value of
 * the service from the merchant's perspective rather than from an
 * arbitrary game mechanic.
 *
 * Three independent factors contribute:
 *
 * 1. Distance
 *    Merchants pay more for deliveries that impose a greater travel
 *    burden: local (same district), inter-town, or genuinely remote.
 *    Distance is absolute and independent of threat.
 *
 * 2. Threat
 *    Jobs beginning or ending in dangerous territory command a
 *    premium. The route is considered dangerous if either endpoint
 *    lies in contested territory.
 *
 * 3. Luck
 *    Reward level determines the quality band. The actual item
 *    within that band is intentionally biased toward the lower end
 *    using RandomMinMaxScaled(), preserving the excitement of
 *    exceptional rewards without inflating the economy.
 *
 * The result is a system that rewards effort and risk while keeping
 * merchants, rather than game mechanics, as the conceptual source
 * of compensation.
 *
 * NOTE:
 * This class embodies the courier economy.
 *
 * Before modifying reward values, consider the gameplay implications.
 * Most courier balancing should occur here rather than in the reward
 * generation code.
 */

/* Scripts\Engines\CourierSystem\CourierRewardCalculator.cs
 * Changelog
 *  8/6/2026, Adam
 *  Added distance and territorial-threat reward calculation.
 */

using Server.Engines.TerritoryControl;
using Server.Mobiles;
using System;
using static Server.Loot;

namespace Server.Engines.CourierSystem
{
    public enum CourierThreatLevel
    {
        Low,
        Medium,
        High
    }

    public sealed class CourierRewardCalculation
    {
        public int Distance { get; init; }
        public int DistanceBonus { get; init; }

        public CourierThreatLevel ThreatLevel { get; init; }
        public int ThreatBonus { get; init; }

        public ImbueLevel RewardLevel { get; init; }
    }

    public static class CourierRewardCalculator
    {
        /*
         * Destinations in another district at or beyond this tile distance
         * are considered genuinely remote. Roughly the median town-to-town
         * distance on Felucca, so inter-town routes split about evenly
         * between +1 and +2.
         */
        private const int FarDistance = 2000;

        public static ImbueLevel Calculate(
            BaseVendor source,
            BaseVendor destination,
            Mobile courier)
        {
            return CalculateDetails(
                source,
                destination,
                courier).RewardLevel;
        }

        public static CourierRewardCalculation CalculateDetails(
            BaseVendor source,
            BaseVendor destination,
            Mobile courier)
        {
            if (source == null ||
                source.Deleted ||
                destination == null ||
                destination.Deleted)
            {
                return new CourierRewardCalculation
                {
                    Distance = 0,
                    DistanceBonus = 0,
                    ThreatLevel = CourierThreatLevel.Low,
                    ThreatBonus = 0,
                    RewardLevel = ImbueLevel.Level1
                };
            }

            int distance;

            int distanceBonus = CalculateDistanceBonus(
                source,
                destination,
                out distance);

            CourierThreatLevel sourceThreat =
                CalculateLocationThreat(source);

            CourierThreatLevel destinationThreat =
                CalculateLocationThreat(destination);

            CourierThreatLevel threatLevel =
                (CourierThreatLevel)Math.Max(
                    (int)sourceThreat,
                    (int)destinationThreat);

            int threatBonus =
                CalculateThreatBonus(threatLevel);

            int numericLevel =
                1 +
                distanceBonus +
                threatBonus;

            ImbueLevel rewardLevel =
                Normalize((ImbueLevel)numericLevel);

            return new CourierRewardCalculation
            {
                Distance = distance,
                DistanceBonus = distanceBonus,
                ThreatLevel = threatLevel,
                ThreatBonus = threatBonus,
                RewardLevel = rewardLevel
            };
        }

        private static CourierThreatLevel CalculateLocationThreat(BaseVendor destination)
        {
            TerritoryDistrict district = destination.District;
            TerritoryZone zone = destination.Zone;

            /*
             * Vendors outside TerritoryControl are treated as safe.
             * They should normally be excluded during destination
             * selection, but the calculator remains defensive.
             */
            if (district == null)
                return CourierThreatLevel.Low;

            /*
             * Destination district is controlled by the human side.
             */
            if (district.IsConquered)
                return CourierThreatLevel.Low;

            /*
             * The district remains occupied, but the destination's
             * specific zone has been conquered.
             */
            if (zone != null && zone.IsConquered)
                return CourierThreatLevel.Medium;

            /*
             * Neither the district nor the destination zone
             * has been conquered.
             */
            return CourierThreatLevel.High;
        }

        private static int CalculateThreatBonus(
            CourierThreatLevel threatLevel)
        {
            return threatLevel switch
            {
                CourierThreatLevel.Low => 0,
                CourierThreatLevel.Medium => 1,
                CourierThreatLevel.High => 2,
                _ => 0
            };
        }

        public static ImbueLevel Normalize(
            ImbueLevel level)
        {
            int numericLevel = Math.Max(
                (int)ImbueLevel.Level1,
                Math.Min(
                    (int)ImbueLevel.Level5,
                    (int)level));

            return (ImbueLevel)numericLevel;
        }

        /// <summary>
        /// Player-facing description of a reward level. Players see that jobs
        /// differ in value without seeing levels or the calculation behind them.
        /// </summary>
        public static string GetPayDescription(ImbueLevel level)
        {
            return Normalize(level) switch
            {
                ImbueLevel.Level1 => "modest",
                ImbueLevel.Level2 => "fair",
                ImbueLevel.Level3 => "good",
                ImbueLevel.Level4 => "generous",
                _ => "exceptional"
            };
        }

        public static void GetImbueRange(
            ImbueLevel rewardLevel,
            out int min,
            out int max)
        {
            switch (Normalize(rewardLevel))
            {
                default:
                case ImbueLevel.Level1:
                    min = 1;
                    max = 1;
                    break;

                case ImbueLevel.Level2:
                    min = 2;
                    max = 2;
                    break;

                case ImbueLevel.Level3:
                    min = 2;
                    max = 3;
                    break;

                case ImbueLevel.Level4:
                    min = 3;
                    max = 4;
                    break;

                case ImbueLevel.Level5:
                    min = 3;
                    max = 5;
                    break;
            }
        }

        /*
         * Courier distance measures the actual travel burden of a route,
         * independently of threat (see CalculateLocationThreat):
         *
         *   Same district                        +0  local
         *   Another district, < FarDistance      +1  inter-town
         *   Another district, >= FarDistance     +2  genuinely remote
         *
         * We deliberately do not rank a route relative to the source vendor's
         * other available destinations. Destination selection prefers the same
         * vendor family, and those sets are often only one or two vendors, so a
         * relative ranking can make a trivial local route look maximally distant.
         */
        private static int CalculateDistanceBonus(
            BaseVendor source,
            BaseVendor destination,
            out int distance)
        {
            distance = 0;

            if (source == null || source.Deleted)
                return 0;

            if (destination == null || destination.Deleted)
                return 0;

            if (source.Map != destination.Map)
                return 0;

            distance = (int)source.GetDistanceToSqrt(destination.Location);

            if (source.District == destination.District)
                return 0;

            return distance >= FarDistance ? 2 : 1;
        }
    }
}