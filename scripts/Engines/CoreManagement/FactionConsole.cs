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

/* scripts\Engines\CoreManagement\FactionConsole.cs
 * CHANGELOG:
 *  1/1/23, Yoar
 *      Initial commit.
 */

using Server.Items;
using System;

namespace Server.Factions
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class FactionConsole : Item
    {
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan ElectionPendingPeriod
        {
            get { return FactionConfig.ElectionPendingPeriod; }
            set { FactionConfig.ElectionPendingPeriod = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan ElectionCampaignPeriod
        {
            get { return FactionConfig.ElectionCampaignPeriod; }
            set { FactionConfig.ElectionCampaignPeriod = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan ElectionVotingPeriod
        {
            get { return FactionConfig.ElectionVotingPeriod; }
            set { FactionConfig.ElectionVotingPeriod = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int ElectionMaxCandidates
        {
            get { return FactionConfig.ElectionMaxCandidates; }
            set { FactionConfig.ElectionMaxCandidates = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int ElectionCandidateRank
        {
            get { return FactionConfig.ElectionCandidateRank; }
            set { FactionConfig.ElectionCandidateRank = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan LeavePeriod
        {
            get { return FactionConfig.LeavePeriod; }
            set { FactionConfig.LeavePeriod = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public double SkillLossFactor
        {
            get { return FactionConfig.SkillLossFactor; }
            set { FactionConfig.SkillLossFactor = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan SkillLossPeriod
        {
            get { return FactionConfig.SkillLossPeriod; }
            set { FactionConfig.SkillLossPeriod = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int StabilityFactor
        {
            get { return FactionConfig.StabilityFactor; }
            set { FactionConfig.StabilityFactor = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int StabilityActivation
        {
            get { return FactionConfig.StabilityActivation; }
            set { FactionConfig.StabilityActivation = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan ItemExpirationPeriod
        {
            get { return FactionConfig.ItemExpirationPeriod; }
            set { FactionConfig.ItemExpirationPeriod = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int BroadcastsPerPeriod
        {
            get { return FactionConfig.BroadcastsPerPeriod; }
            set { FactionConfig.BroadcastsPerPeriod = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan BroadcastPeriod
        {
            get { return FactionConfig.BroadcastPeriod; }
            set { FactionConfig.BroadcastPeriod = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan SilverGivenExpirePeriod
        {
            get { return FactionConfig.SilverGivenExpirePeriod; }
            set { FactionConfig.SilverGivenExpirePeriod = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan TownTaxChangePeriod
        {
            get { return FactionConfig.TownTaxChangePeriod; }
            set { FactionConfig.TownTaxChangePeriod = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan TownIncomePeriod
        {
            get { return FactionConfig.TownIncomePeriod; }
            set { FactionConfig.TownIncomePeriod = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int TownCaptureSilver
        {
            get { return FactionConfig.TownCaptureSilver; }
            set { FactionConfig.TownCaptureSilver = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool TownSqualorEnabled
        {
            get { return FactionConfig.TownSqualorEnabled; }
            set { FactionConfig.TownSqualorEnabled = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int TownSoftSilverCap
        {
            get { return FactionConfig.TownSoftSilverCap; }
            set { FactionConfig.TownSoftSilverCap = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int WarHorseSilverPrice
        {
            get { return FactionConfig.WarHorseSilverPrice; }
            set { FactionConfig.WarHorseSilverPrice = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int WarHorseGoldPrice
        {
            get { return FactionConfig.WarHorseGoldPrice; }
            set { FactionConfig.WarHorseGoldPrice = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool WarHorseRankRequired
        {
            get { return FactionConfig.WarHorseRankRequired; }
            set { FactionConfig.WarHorseRankRequired = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan SigilCorruptionGrace
        {
            get { return FactionConfig.SigilCorruptionGrace; }
            set { FactionConfig.SigilCorruptionGrace = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan SigilCorruptionPeriod
        {
            get { return FactionConfig.SigilCorruptionPeriod; }
            set { FactionConfig.SigilCorruptionPeriod = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan SigilReturnPeriod
        {
            get { return FactionConfig.SigilReturnPeriod; }
            set { FactionConfig.SigilReturnPeriod = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan SigilPurificationPeriod
        {
            get { return FactionConfig.SigilPurificationPeriod; }
            set { FactionConfig.SigilPurificationPeriod = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool NewGumps
        {
            get { return FactionConfig.NewGumps; }
            set { FactionConfig.NewGumps = value; }
        }

        [Constructable]
        public FactionConsole()
            : base(0x1F14)
        {
            Hue = 1254;
            Name = "Faction Settings Console";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
        }

        public FactionConsole(Serial serial)
            : base(serial)
        {
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