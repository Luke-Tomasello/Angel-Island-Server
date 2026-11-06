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

/* scripts\Engines\CoreManagement\TownshipConsole.cs
 * CHANGELOG:
 * 11/4/22, Adam
 *  move here from /Engines/Township/TownshipSettings.cs
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

using Server.Township;
using System;

namespace Server.Engines.Consoles
{
    [NoSort]
    public class TownshipConsole : BaseConsole
    {
        [Constructable]
        public TownshipConsole()
            : base("Township Settings Console")
        {
            Hue = TownshipSettings.Hue;   // keep original behavior
        }

        public TownshipConsole(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
        }

        // Read-only info
        [CommandProperty(AccessLevel.GameMaster)]
        public double FeePercentageCalc => TownshipSettings.FeePercentageCalc;

        [CommandProperty(AccessLevel.GameMaster)]
        public double FeeAcctInfoCount => TownshipSettings.FeeAcctInfoCount;

        // Configurable
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int TownshipHue
        {
            get => TownshipSettings.Hue;
            set
            {
                TownshipSettings.Hue = value;
                Hue = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AL_NoneToLow
        {
            get => TownshipSettings.NoneToLow;
            set => TownshipSettings.NoneToLow = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AL_LowToMedium
        {
            get => TownshipSettings.LowToMedium;
            set => TownshipSettings.LowToMedium = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AL_MediumToHigh
        {
            get => TownshipSettings.MediumToHigh;
            set => TownshipSettings.MediumToHigh = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AL_HighToBooming
        {
            get => TownshipSettings.HighToBooming;
            set => TownshipSettings.HighToBooming = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int TSDeedCost
        {
            get => TownshipSettings.TSDeedCost;
            set => TownshipSettings.TSDeedCost = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int InitialFunds
        {
            get => TownshipSettings.InitialFunds;
            set => TownshipSettings.InitialFunds = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int TStoneUseRange
        {
            get => TownshipSettings.TStoneUseRange;
            set => TownshipSettings.TStoneUseRange = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int BaseFee
        {
            get => TownshipSettings.BaseFee;
            set => TownshipSettings.BaseFee = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ExtendedFee
        {
            get => TownshipSettings.ExtendedFee;
            set => TownshipSettings.ExtendedFee = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NoGateOutFee
        {
            get => TownshipSettings.NoGateOutFee;
            set => TownshipSettings.NoGateOutFee = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NoGateInFee
        {
            get => TownshipSettings.NoGateInFee;
            set => TownshipSettings.NoGateInFee = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NoRecallOutFee
        {
            get => TownshipSettings.NoRecallOutFee;
            set => TownshipSettings.NoRecallOutFee = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NoRecallInFee
        {
            get => TownshipSettings.NoRecallInFee;
            set => TownshipSettings.NoRecallInFee = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawlessFee
        {
            get => TownshipSettings.LawlessFee;
            set => TownshipSettings.LawlessFee = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawAuthFee
        {
            get => TownshipSettings.LawAuthFee;
            set => TownshipSettings.LawAuthFee = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawGuardedFee
        {
            get => TownshipSettings.LawGuardedFee;
            set => TownshipSettings.LawGuardedFee = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NPCType1Fee
        {
            get => TownshipSettings.NPCType1Fee;
            set => TownshipSettings.NPCType1Fee = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NPCType2Fee
        {
            get => TownshipSettings.NPCType2Fee;
            set => TownshipSettings.NPCType2Fee = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NPCType3Fee
        {
            get => TownshipSettings.NPCType3Fee;
            set => TownshipSettings.NPCType3Fee = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawNormCharge
        {
            get => TownshipSettings.LawNormCharge;
            set => TownshipSettings.LawNormCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawlessCharge
        {
            get => TownshipSettings.LawlessCharge;
            set => TownshipSettings.LawlessCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawAuthCharge
        {
            get => TownshipSettings.LawAuthCharge;
            set => TownshipSettings.LawAuthCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ChangeTravelCharge
        {
            get => TownshipSettings.ChangeTravelCharge;
            set => TownshipSettings.ChangeTravelCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int UpdateEnemyCharge
        {
            get => TownshipSettings.UpdateEnemyCharge;
            set => TownshipSettings.UpdateEnemyCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ExtendedCharge
        {
            get => TownshipSettings.ExtendedCharge;
            set => TownshipSettings.ExtendedCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int EmissaryCharge
        {
            get => TownshipSettings.EmissaryCharge;
            set => TownshipSettings.EmissaryCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int EvocatorCharge
        {
            get => TownshipSettings.EvocatorCharge;
            set => TownshipSettings.EvocatorCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AlchemistCharge
        {
            get => TownshipSettings.AlchemistCharge;
            set => TownshipSettings.AlchemistCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AnimalTrainerCharge
        {
            get => TownshipSettings.AnimalTrainerCharge;
            set => TownshipSettings.AnimalTrainerCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int BankerCharge
        {
            get => TownshipSettings.BankerCharge;
            set => TownshipSettings.BankerCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int InnkeeperCharge
        {
            get => TownshipSettings.InnkeeperCharge;
            set => TownshipSettings.InnkeeperCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int MageCharge
        {
            get => TownshipSettings.MageCharge;
            set => TownshipSettings.MageCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ProvisionerCharge
        {
            get => TownshipSettings.ProvisionerCharge;
            set => TownshipSettings.ProvisionerCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ArmsTrainerCharge
        {
            get => TownshipSettings.ArmsTrainerCharge;
            set => TownshipSettings.ArmsTrainerCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int MageTrainerCharge
        {
            get => TownshipSettings.MageTrainerCharge;
            set => TownshipSettings.MageTrainerCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int RogueTrainerCharge
        {
            get => TownshipSettings.RogueTrainerCharge;
            set => TownshipSettings.RogueTrainerCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LookoutCharge
        {
            get => TownshipSettings.LookoutCharge;
            set => TownshipSettings.LookoutCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int FightBrokerCharge
        {
            get => TownshipSettings.FightBrokerCharge;
            set => TownshipSettings.FightBrokerCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int MinstrelCharge
        {
            get => TownshipSettings.MinstrelCharge;
            set => TownshipSettings.MinstrelCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int TownCrierCharge
        {
            get => TownshipSettings.TownCrierCharge;
            set => TownshipSettings.TownCrierCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NecromancerCharge
        {
            get => TownshipSettings.NecromancerCharge;
            set => TownshipSettings.NecromancerCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int RancherCharge
        {
            get => TownshipSettings.RancherCharge;
            set => TownshipSettings.RancherCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int FarmerCharge
        {
            get => TownshipSettings.FarmerCharge;
            set => TownshipSettings.FarmerCharge = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double GuildHousePercentage
        {
            get => TownshipSettings.GuildHousePercentage;
            set
            {
                if (value <= 1.0 && value > 0.0)
                    TownshipSettings.GuildHousePercentage = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierNone
        {
            get => TownshipSettings.LLModifierNone;
            set
            {
                if (value >= 0.0) TownshipSettings.LLModifierNone = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierLow
        {
            get => TownshipSettings.LLModifierLow;
            set
            {
                if (value >= 0.0) TownshipSettings.LLModifierLow = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierMed
        {
            get => TownshipSettings.LLModifierMed;
            set
            {
                if (value >= 0.0) TownshipSettings.LLModifierMed = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierHigh
        {
            get => TownshipSettings.LLModifierHigh;
            set
            {
                if (value >= 0.0) TownshipSettings.LLModifierHigh = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierBoom
        {
            get => TownshipSettings.LLModifierBoom;
            set
            {
                if (value >= 0.0) TownshipSettings.LLModifierBoom = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierNone
        {
            get => TownshipSettings.NPCModifierNone;
            set
            {
                if (value >= 0.0) TownshipSettings.NPCModifierNone = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierLow
        {
            get => TownshipSettings.NPCModifierLow;
            set
            {
                if (value >= 0.0) TownshipSettings.NPCModifierLow = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierMed
        {
            get => TownshipSettings.NPCModifierMed;
            set
            {
                if (value >= 0.0) TownshipSettings.NPCModifierMed = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierHigh
        {
            get => TownshipSettings.NPCModifierHigh;
            set
            {
                if (value >= 0.0) TownshipSettings.NPCModifierHigh = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierBoom
        {
            get => TownshipSettings.NPCModifierBoom;
            set
            {
                if (value >= 0.0) TownshipSettings.NPCModifierBoom = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierNone
        {
            get => TownshipSettings.BaseModifierNone;
            set
            {
                if (value >= 0.0) TownshipSettings.BaseModifierNone = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierLow
        {
            get => TownshipSettings.BaseModifierLow;
            set
            {
                if (value >= 0.0) TownshipSettings.BaseModifierLow = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierMed
        {
            get => TownshipSettings.BaseModifierMed;
            set
            {
                if (value >= 0.0) TownshipSettings.BaseModifierMed = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierHigh
        {
            get => TownshipSettings.BaseModifierHigh;
            set
            {
                if (value >= 0.0) TownshipSettings.BaseModifierHigh = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierBoom
        {
            get => TownshipSettings.BaseModifierBoom;
            set
            {
                if (value >= 0.0) TownshipSettings.BaseModifierBoom = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int HouseClearance
        {
            get => TownshipSettings.HouseClearance;
            set => TownshipSettings.HouseClearance = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NeutralHouseClearance
        {
            get => TownshipSettings.NeutralHouseClearance;
            set => TownshipSettings.NeutralHouseClearance = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int DecorationClearance
        {
            get => TownshipSettings.DecorationClearance;
            set => TownshipSettings.DecorationClearance = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int TeleporterClearance
        {
            get => TownshipSettings.TeleporterClearance;
            set => TownshipSettings.TeleporterClearance = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double ReqGuildedPercentage
        {
            get => TownshipSettings.ReqGuildedPercentage;
            set => TownshipSettings.ReqGuildedPercentage = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public bool NeedsGround
        {
            get => TownshipSettings.NeedsGround;
            set => TownshipSettings.NeedsGround = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double AuctioneerPayoutPercentage
        {
            get => TownshipSettings.AuctioneerPayoutPercentage;
            set
            {
                if (value > 0.0 && value <= 1.0)
                    TownshipSettings.AuctioneerPayoutPercentage = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int WallBuildTicks
        {
            get => TownshipSettings.WallBuildTicks;
            set => TownshipSettings.WallBuildTicks = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int WallHitsDecay
        {
            get => TownshipSettings.WallHitsDecay;
            set
            {
                if (value >= 0) TownshipSettings.WallHitsDecay = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int WallDamageTicks
        {
            get => TownshipSettings.WallDamageTicks;
            set => TownshipSettings.WallDamageTicks = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public TimeSpan WallDamageDelay
        {
            get => TownshipSettings.WallDamageDelay;
            set => TownshipSettings.WallDamageDelay = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int WallRepairTicks
        {
            get => TownshipSettings.WallRepairTicks;
            set => TownshipSettings.WallRepairTicks = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public TimeSpan WallRepairDelay
        {
            get => TownshipSettings.WallRepairDelay;
            set => TownshipSettings.WallRepairDelay = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public bool AFKCheck
        {
            get => TownshipSettings.AFKCheck;
            set => TownshipSettings.AFKCheck = value;
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