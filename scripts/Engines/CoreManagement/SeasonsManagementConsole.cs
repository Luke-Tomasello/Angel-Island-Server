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

/* Scripts\Engines\CoreManagement\SeasonsManagementConsole.cs
 * ChangeLog
 *  3/27/2026, Adam
 *      First time checkin.
 */

using System;

namespace Server.Engines.Consoles
{
    public class SeasonsManagementConsole : BaseConsole
    {
        [Constructable]
        public SeasonsManagementConsole()
            : base("Seasons Management Console")
        {
        }

        public SeasonsManagementConsole(Serial serial)
            : base(serial)
        {
        }

        #region Seasons Configuration Properties

        /// <summary>
        /// Core conversion factor: gold -> seasonal points.
        ///
        /// Calibration reference:
        /// ~117,815 gold (about 90 dragons over a 90-day season) ~= 600 seasonal points
        /// -> 200 gold per point (this is the anchor)
        ///
        /// This value ties seasonal progression directly to real gameplay output.
        /// Adjust only if loot tables or gold generation rates change significantly.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsGoldPerPoint
        {
            get => CoreAI.SeasonsGoldPerPoint;
            set => CoreAI.SeasonsGoldPerPoint = value;
        }

        /// <summary>
        /// Category multipliers.
        ///
        /// These normalize different playstyles so they compete on roughly equal footing,
        /// despite varying natural gold generation rates.
        ///
        /// PvM/Loot serves as the baseline (1.0). Other activities are tuned relative to it.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsMusicMultiplier
        {
            get => CoreAI.SeasonsMusicMultiplier;
            set => CoreAI.SeasonsMusicMultiplier = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsTheftMultiplier
        {
            get => CoreAI.SeasonsTheftMultiplier;
            set => CoreAI.SeasonsTheftMultiplier = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsChestMultiplier
        {
            get => CoreAI.SeasonsChestMultiplier;
            set => CoreAI.SeasonsChestMultiplier = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsPvMLootMultiplier
        {
            get => CoreAI.SeasonsPvMLootMultiplier;
            set => CoreAI.SeasonsPvMLootMultiplier = value;
        }

        /// <summary>
        /// BOD profession multipliers.
        /// Tuned separately because material costs and gold/hour differ significantly
        /// between professions. These are normalized against the PvM baseline,
        /// not forced into artificial equality.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsSmithBODMultiplier
        {
            get => CoreAI.SeasonsSmithBODMultiplier;
            set => CoreAI.SeasonsSmithBODMultiplier = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsTailorBODMultiplier
        {
            get => CoreAI.SeasonsTailorBODMultiplier;
            set => CoreAI.SeasonsTailorBODMultiplier = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsCarpenterBODMultiplier
        {
            get => CoreAI.SeasonsCarpenterBODMultiplier;
            set => CoreAI.SeasonsCarpenterBODMultiplier = value;
        }

        /// <summary>
        /// Primary seasonal progression threshold.
        ///
        /// Under Banded Progression Scaling (BPS), this no longer acts as a hard stop.
        /// Instead, it serves as the main threshold around which progression bands are defined.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public int SeasonsPointCap
        {
            get => CoreAI.SeasonsPointCap;
            set => CoreAI.SeasonsPointCap = value;
        }

        /// <summary>
        /// Legacy diminishing-returns floor.
        /// No longer used under Banded Progression Scaling.
        /// Retained temporarily for backward compatibility with saved configuration.
        /// </summary>
        [Obsolete]
        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsMinimumAwardFactor
        {
            get => CoreAI.SeasonsMinimumAwardFactor;
            set => CoreAI.SeasonsMinimumAwardFactor = value;
        }

        /// <summary>
        /// Fraction of SeasonsPointCap where soft pre-threshold throttling begins.
        /// Example: 0.80 with a threshold of 1000 begins softening at 800.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsBpsPreCapSoftStartFraction
        {
            get => CoreAI.SeasonsBpsPreCapSoftStartFraction;
            set => CoreAI.SeasonsBpsPreCapSoftStartFraction = value;
        }

        /// <summary>
        /// Width of the first post-threshold band, measured from SeasonsPointCap.
        /// Example: 200 with a threshold of 1000 ends the first post-cap band at 1200.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public int SeasonsBpsFirstPostCapWidth
        {
            get => CoreAI.SeasonsBpsFirstPostCapWidth;
            set => CoreAI.SeasonsBpsFirstPostCapWidth = value;
        }

        /// <summary>
        /// Width of the second post-threshold band, measured from SeasonsPointCap.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public int SeasonsBpsSecondPostCapWidth
        {
            get => CoreAI.SeasonsBpsSecondPostCapWidth;
            set => CoreAI.SeasonsBpsSecondPostCapWidth = value;
        }

        /// <summary>
        /// Width of the third post-threshold band, measured from SeasonsPointCap.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public int SeasonsBpsThirdPostCapWidth
        {
            get => CoreAI.SeasonsBpsThirdPostCapWidth;
            set => CoreAI.SeasonsBpsThirdPostCapWidth = value;
        }

        /// <summary>
        /// Multiplier applied before soft pre-threshold scaling begins.
        /// Normally 1.00.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsBpsEarlyMultiplier
        {
            get => CoreAI.SeasonsBpsEarlyMultiplier;
            set => CoreAI.SeasonsBpsEarlyMultiplier = value;
        }

        /// <summary>
        /// Multiplier applied between the soft-start point and the primary threshold.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsBpsPreCapMultiplier
        {
            get => CoreAI.SeasonsBpsPreCapMultiplier;
            set => CoreAI.SeasonsBpsPreCapMultiplier = value;
        }

        /// <summary>
        /// Multiplier applied in the first post-threshold band.
        /// This is the primary "continued progress after 1000" throttle.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsBpsFirstPostCapMultiplier
        {
            get => CoreAI.SeasonsBpsFirstPostCapMultiplier;
            set => CoreAI.SeasonsBpsFirstPostCapMultiplier = value;
        }

        /// <summary>
        /// Multiplier applied in the second post-threshold band.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsBpsSecondPostCapMultiplier
        {
            get => CoreAI.SeasonsBpsSecondPostCapMultiplier;
            set => CoreAI.SeasonsBpsSecondPostCapMultiplier = value;
        }

        /// <summary>
        /// Multiplier applied in the third post-threshold band.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsBpsThirdPostCapMultiplier
        {
            get => CoreAI.SeasonsBpsThirdPostCapMultiplier;
            set => CoreAI.SeasonsBpsThirdPostCapMultiplier = value;
        }

        /// <summary>
        /// Multiplier applied after all defined post-threshold bands have been exceeded.
        /// This governs extreme long-tail progression.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator)]
        public double SeasonsBpsFinalMultiplier
        {
            get => CoreAI.SeasonsBpsFinalMultiplier;
            set => CoreAI.SeasonsBpsFinalMultiplier = value;
        }

        #endregion Seasons Configuration Properties

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.Administrator)
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