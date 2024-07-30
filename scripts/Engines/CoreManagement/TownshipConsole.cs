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

/* scripts\Engines\CoreManagement\TownshipConsole.cs
 * CHANGELOG:
 * 11/4/22, Adam
 *  move here from /Engines/Township/TownshipSettings.cs
 */
using Server.Items;
using System;

namespace Server.Township
{
    [NoSort]
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class TownshipConsole : Item
    {
        [Constructable]
        public TownshipConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            base.Hue = TownshipSettings.Hue;
            Name = "Township Settings Console";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
        }

        //PIX - added these for information
        [CommandProperty(AccessLevel.GameMaster)]
        public double FeePercentageCalc
        {
            get { return TownshipSettings.FeePercentageCalc; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double FeeAcctInfoCount
        {
            get { return TownshipSettings.FeeAcctInfoCount; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int TownshipHue
        {
            get { return TownshipSettings.Hue; }
            set { TownshipSettings.Hue = value; base.Hue = TownshipSettings.Hue; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AL_NoneToLow
        {
            get { return TownshipSettings.NoneToLow; }
            set { TownshipSettings.NoneToLow = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AL_LowToMedium
        {
            get { return TownshipSettings.LowToMedium; }
            set { TownshipSettings.LowToMedium = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AL_MediumToHigh
        {
            get { return TownshipSettings.MediumToHigh; }
            set { TownshipSettings.MediumToHigh = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AL_HighToBooming
        {
            get { return TownshipSettings.HighToBooming; }
            set { TownshipSettings.HighToBooming = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int TSDeedCost
        {
            get { return TownshipSettings.TSDeedCost; }
            set { TownshipSettings.TSDeedCost = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int InitialFunds
        {
            get { return TownshipSettings.InitialFunds; }
            set { TownshipSettings.InitialFunds = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int TStoneUseRange
        {
            get { return TownshipSettings.TStoneUseRange; }
            set { TownshipSettings.TStoneUseRange = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int BaseFee
        {
            get { return TownshipSettings.BaseFee; }
            set { TownshipSettings.BaseFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ExtendedFee
        {
            get { return TownshipSettings.ExtendedFee; }
            set { TownshipSettings.ExtendedFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NoGateOutFee
        {
            get { return TownshipSettings.NoGateOutFee; }
            set { TownshipSettings.NoGateOutFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NoGateInFee
        {
            get { return TownshipSettings.NoGateInFee; }
            set { TownshipSettings.NoGateInFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NoRecallOutFee
        {
            get { return TownshipSettings.NoRecallOutFee; }
            set { TownshipSettings.NoRecallOutFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NoRecallInFee
        {
            get { return TownshipSettings.NoRecallInFee; }
            set { TownshipSettings.NoRecallInFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawlessFee
        {
            get { return TownshipSettings.LawlessFee; }
            set { TownshipSettings.LawlessFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawAuthFee
        {
            get { return TownshipSettings.LawAuthFee; }
            set { TownshipSettings.LawAuthFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NPCType1Fee
        {
            get { return TownshipSettings.NPCType1Fee; }
            set { TownshipSettings.NPCType1Fee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NPCType2Fee
        {
            get { return TownshipSettings.NPCType2Fee; }
            set { TownshipSettings.NPCType2Fee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NPCType3Fee
        {
            get { return TownshipSettings.NPCType3Fee; }
            set { TownshipSettings.NPCType3Fee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawNormCharge
        {
            get { return TownshipSettings.LawNormCharge; }
            set { TownshipSettings.LawNormCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawlessCharge
        {
            get { return TownshipSettings.LawlessCharge; }
            set { TownshipSettings.LawlessCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawAuthCharge
        {
            get { return TownshipSettings.LawAuthCharge; }
            set { TownshipSettings.LawAuthCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ChangeTravelCharge
        {
            get { return TownshipSettings.ChangeTravelCharge; }
            set { TownshipSettings.ChangeTravelCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int UpdateEnemyCharge
        {
            get { return TownshipSettings.UpdateEnemyCharge; }
            set { TownshipSettings.UpdateEnemyCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ExtendedCharge
        {
            get { return TownshipSettings.ExtendedCharge; }
            set { TownshipSettings.ExtendedCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int EmissaryCharge
        {
            get { return TownshipSettings.EmissaryCharge; }
            set { TownshipSettings.EmissaryCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int EvocatorCharge
        {
            get { return TownshipSettings.EvocatorCharge; }
            set { TownshipSettings.EvocatorCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AlchemistCharge
        {
            get { return TownshipSettings.AlchemistCharge; }
            set { TownshipSettings.AlchemistCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AnimalTrainerCharge
        {
            get { return TownshipSettings.AnimalTrainerCharge; }
            set { TownshipSettings.AnimalTrainerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int BankerCharge
        {
            get { return TownshipSettings.BankerCharge; }
            set { TownshipSettings.BankerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int InnkeeperCharge
        {
            get { return TownshipSettings.InnkeeperCharge; }
            set { TownshipSettings.InnkeeperCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int MageCharge
        {
            get { return TownshipSettings.MageCharge; }
            set { TownshipSettings.MageCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ProvisionerCharge
        {
            get { return TownshipSettings.ProvisionerCharge; }
            set { TownshipSettings.ProvisionerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ArmsTrainerCharge
        {
            get { return TownshipSettings.ArmsTrainerCharge; }
            set { TownshipSettings.ArmsTrainerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int MageTrainerCharge
        {
            get { return TownshipSettings.MageTrainerCharge; }
            set { TownshipSettings.MageTrainerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int RogueTrainerCharge
        {
            get { return TownshipSettings.RogueTrainerCharge; }
            set { TownshipSettings.RogueTrainerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LookoutCharge
        {
            get { return TownshipSettings.LookoutCharge; }
            set { TownshipSettings.LookoutCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int FightBrokerCharge
        {
            get { return TownshipSettings.FightBrokerCharge; }
            set { TownshipSettings.FightBrokerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int MinstrelCharge
        {
            get { return TownshipSettings.MinstrelCharge; }
            set { TownshipSettings.MinstrelCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int TownCrierCharge
        {
            get { return TownshipSettings.TownCrierCharge; }
            set { TownshipSettings.TownCrierCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NecromancerCharge
        {
            get { return TownshipSettings.NecromancerCharge; }
            set { TownshipSettings.NecromancerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int RancherCharge
        {
            get { return TownshipSettings.RancherCharge; }
            set { TownshipSettings.RancherCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int FarmerCharge
        {
            get { return TownshipSettings.FarmerCharge; }
            set { TownshipSettings.FarmerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double GuildHousePercentage
        {
            get { return TownshipSettings.GuildHousePercentage; }
            set { if (value <= 1.0 && value > 0.0) TownshipSettings.GuildHousePercentage = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierNone
        {
            get { return TownshipSettings.LLModifierNone; }
            set { if (value >= 0.0) TownshipSettings.LLModifierNone = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierLow
        {
            get { return TownshipSettings.LLModifierLow; }
            set { if (value >= 0.0) TownshipSettings.LLModifierLow = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierMed
        {
            get { return TownshipSettings.LLModifierMed; }
            set { if (value >= 0.0) TownshipSettings.LLModifierMed = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierHigh
        {
            get { return TownshipSettings.LLModifierHigh; }
            set { if (value >= 0.0) TownshipSettings.LLModifierHigh = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierBoom
        {
            get { return TownshipSettings.LLModifierBoom; }
            set { if (value >= 0.0) TownshipSettings.LLModifierBoom = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierNone
        {
            get { return TownshipSettings.NPCModifierNone; }
            set { if (value >= 0.0) TownshipSettings.NPCModifierNone = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierLow
        {
            get { return TownshipSettings.NPCModifierLow; }
            set { if (value >= 0.0) TownshipSettings.NPCModifierLow = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierMed
        {
            get { return TownshipSettings.NPCModifierMed; }
            set { if (value >= 0.0) TownshipSettings.NPCModifierMed = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierHigh
        {
            get { return TownshipSettings.NPCModifierHigh; }
            set { if (value >= 0.0) TownshipSettings.NPCModifierHigh = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierBoom
        {
            get { return TownshipSettings.NPCModifierBoom; }
            set { if (value >= 0.0) TownshipSettings.NPCModifierBoom = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierNone
        {
            get { return TownshipSettings.BaseModifierNone; }
            set { if (value >= 0.0) TownshipSettings.BaseModifierNone = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierLow
        {
            get { return TownshipSettings.BaseModifierLow; }
            set { if (value >= 0.0) TownshipSettings.BaseModifierLow = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierMed
        {
            get { return TownshipSettings.BaseModifierMed; }
            set { if (value >= 0.0) TownshipSettings.BaseModifierMed = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierHigh
        {
            get { return TownshipSettings.BaseModifierHigh; }
            set { if (value >= 0.0) TownshipSettings.BaseModifierHigh = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierBoom
        {
            get { return TownshipSettings.BaseModifierBoom; }
            set { if (value >= 0.0) TownshipSettings.BaseModifierBoom = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int HouseClearance
        {
            get { return TownshipSettings.HouseClearance; }
            set { TownshipSettings.HouseClearance = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NeutralHouseClearance
        {
            get { return TownshipSettings.NeutralHouseClearance; }
            set { TownshipSettings.NeutralHouseClearance = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int DecorationClearance
        {
            get { return TownshipSettings.DecorationClearance; }
            set { TownshipSettings.DecorationClearance = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int TeleporterClearance
        {
            get { return TownshipSettings.TeleporterClearance; }
            set { TownshipSettings.TeleporterClearance = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double ReqGuildedPercentage
        {
            get { return TownshipSettings.ReqGuildedPercentage; }
            set { TownshipSettings.ReqGuildedPercentage = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public bool NeedsGround
        {
            get { return TownshipSettings.NeedsGround; }
            set { TownshipSettings.NeedsGround = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double AuctioneerPayoutPercentage
        {
            get { return TownshipSettings.AuctioneerPayoutPercentage; }
            set { if (value > 0.0 && value <= 1.0) TownshipSettings.AuctioneerPayoutPercentage = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int WallBuildTicks
        {
            get { return TownshipSettings.WallBuildTicks; }
            set { TownshipSettings.WallBuildTicks = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int WallHitsDecay
        {
            get { return TownshipSettings.WallHitsDecay; }
            set { if (value >= 0) TownshipSettings.WallHitsDecay = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int WallDamageTicks
        {
            get { return TownshipSettings.WallDamageTicks; }
            set { TownshipSettings.WallDamageTicks = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public TimeSpan WallDamageDelay
        {
            get { return TownshipSettings.WallDamageDelay; }
            set { TownshipSettings.WallDamageDelay = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int WallRepairTicks
        {
            get { return TownshipSettings.WallRepairTicks; }
            set { TownshipSettings.WallRepairTicks = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public TimeSpan WallRepairDelay
        {
            get { return TownshipSettings.WallRepairDelay; }
            set { TownshipSettings.WallRepairDelay = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public bool AFKCheck
        {
            get { return TownshipSettings.AFKCheck; }
            set { TownshipSettings.AFKCheck = value; }
        }

        #region Serialize/Deserialize - nothing to do

        public TownshipConsole(Serial serial)
            : base(serial)
        {
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

        #endregion
    }
}