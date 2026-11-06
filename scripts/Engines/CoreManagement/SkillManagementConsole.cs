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

/* Engines/CoreManagement/SkillManagementConsole.cs
 * ChangeLog
 * 12/26/25, Adam
 *  Added Blocking properties for all passive hooks
 *	12/23/25, Adam
 *		Created.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

using System;

namespace Server.Engines.Consoles
{
    [NoSort]
    public class SkillManagementConsole : BaseConsole
    {
        [Constructable]
        public SkillManagementConsole()
            : base("Skill Management Console")
        {
        }

        public SkillManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double StatGainFactor
        {
            get => CoreAI.StatGainFactor;
            set => CoreAI.StatGainFactor = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool SpiritSpeakUsageReport
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SpiritSpeakUsageReport);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.SpiritSpeakUsageReport);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.SpiritSpeakUsageReport);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int DetectSkillDelay
        {
            get => CoreAI.DetectSkillDelay;
            set => CoreAI.DetectSkillDelay = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double HitPoisonChanceIncr
        {
            get => CoreAI.HitPoisonChanceIncr;
            set => CoreAI.HitPoisonChanceIncr = value;
        }
        [CommandProperty(AccessLevel.Administrator)]
        public static double PoisonBandageSlipChance
        {
            get => CoreAI.PoisonBandageSlipChance;
            set => CoreAI.PoisonBandageSlipChance = Math.Max(0.0, Math.Min(1.0, value));
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int AreaPeaceCrowdSize
        {
            get => CoreAI.AreaPeaceCrowdSize;
            set => CoreAI.AreaPeaceCrowdSize = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double AreaPeaceSoloBonus
        {
            get => CoreAI.AreaPeaceSoloBonus;
            set => CoreAI.AreaPeaceSoloBonus = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double AreaPeaceDelayBase
        {
            get => CoreAI.AreaPeaceDelayBase;
            set => CoreAI.AreaPeaceDelayBase = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double AreaPeaceDelayBonus
        {
            get => CoreAI.AreaPeaceDelayBonus;
            set => CoreAI.AreaPeaceDelayBonus = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double BolaThrowLock
        {
            get => CoreAI.BolaThrowLock;
            set => CoreAI.BolaThrowLock = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double BolaMountLock
        {
            get => CoreAI.BolaMountLock;
            set => CoreAI.BolaMountLock = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double TrackRangeBase
        {
            get => CoreAI.TrackRangeBase;
            set => CoreAI.TrackRangeBase = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double TrackRangePerSkill
        {
            get => CoreAI.TrackRangePerSkill;
            set => CoreAI.TrackRangePerSkill = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int TrackRangeMin
        {
            get => CoreAI.TrackRangeMin;
            set => CoreAI.TrackRangeMin = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool TrackDifficulty
        {
            get => CoreAI.TrackDifficulty;
            set => CoreAI.TrackDifficulty = value;
        }
        [CommandProperty(AccessLevel.Administrator)]
        public double TrackArrowRangeMultiplier
        {
            get => CoreAI.TrackArrowRangeMultiplier;
            set => CoreAI.TrackArrowRangeMultiplier = value;
        }
        [CommandProperty(AccessLevel.Administrator)]
        public double TrackHintRangeMultiplier
        {
            get => CoreAI.TrackHintRangeMultiplier;
            set => CoreAI.TrackHintRangeMultiplier = value;
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool RevealSpellDifficulty
        {
            get => CoreAI.RevealSpellDifficulty;
            set => CoreAI.RevealSpellDifficulty = value;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Administrator)
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}