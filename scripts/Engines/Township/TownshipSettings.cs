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

/* Scripts/Engines/Township/TownshipSettings.cs
 * CHANGELOG:
 * 4/29/22, Yoar
 *      Added HouseClearance: Clearance requirement to any house (guilded or not).
 * 2/18/22, Yoar
 *      Added TStoneUseRange: Sets the use range of the township stone.
 * 2/8/22, Yoar
 *      Added ReqGuildedPercentage: Sets the required percentage of guilded houses to unlock township building
 * 1/17/22, Yoar
 *      Renamed/reorganized some settings
 *		Renamed WallTeleporterDistance to TeleporterClearance
 *		Added HouseClearance, DecorationClearance
 * 1/12/22, Yoar
 *		Township cleanups
 * 11/23/21, Yoar
 *		Added: DamageTicks, DamageDelay, RepairTicks, RepairDelay, AFKCheck
 * 11/16/08, Pix
 *		Added WallTeleporterDistance for wall placement.
 * 10/10/08, Pix
 *		Added CalculateFeesBasedOnServerActivity to be called when we calc server activity.
 *	3/20/07, Pix
 *		Added InitialFunds dial.
 *	Pix: 4/19/07
 *		Added all fees/charges and modifiers.
 */

using System;
using System.IO;

namespace Server.Township
{
    public static class TownshipSettings
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
        }

        public static DateTime m_sLastActivityLevelLimitCalculation = DateTime.MinValue;

        public static void CalculateFeesBasedOnServerActivity(int acctsAccessedInLastWeek)
        {
            if (m_sLastActivityLevelLimitCalculation == DateTime.MinValue)
            {
                //if we've just come up from a restart, then set the last calc to Now.
                m_sLastActivityLevelLimitCalculation = DateTime.UtcNow;
            }
            else if (m_sLastActivityLevelLimitCalculation + TimeSpan.FromHours(24.0) < DateTime.UtcNow)
            {
                double idealAILW = 1000.0; //hahahahahah ideal.  good joke, eh?

                double iPercentage = ((double)acctsAccessedInLastWeek) / idealAILW;

                // make sure that we're > 10% (meaning a base fee would cost 250 gold per day (at high activity)
                if (iPercentage < 0.1) iPercentage = 0.1;

                FeeAcctInfoCount = acctsAccessedInLastWeek;
                FeePercentageCalc = iPercentage;

                BaseFee = (int)(2500 * iPercentage);
                ExtendedFee = (int)(2500 * iPercentage);
                NoGateOutFee = (int)(1000 * iPercentage);
                NoGateInFee = (int)(1000 * iPercentage);
                NoRecallOutFee = (int)(1000 * iPercentage);
                NoRecallInFee = (int)(1000 * iPercentage);
                LawlessFee = (int)(2000 * iPercentage);
                LawAuthFee = (int)(5000 * iPercentage);
                NPCType1Fee = (int)(1000 * iPercentage);
                NPCType2Fee = (int)(2000 * iPercentage);
                NPCType3Fee = (int)(5000 * iPercentage);
            }
        }

        #region Save/Load

        public static void OnSave(WorldSaveEventArgs e)
        {
            try
            {
                Console.WriteLine("TownshipSettings Saving...");
                if (!Directory.Exists("Saves/AngelIsland"))
                    Directory.CreateDirectory("Saves/AngelIsland");

                string filePath = Path.Combine("Saves/AngelIsland", "Township.bin");

                GenericWriter bin;
                bin = new BinaryFileWriter(filePath, true);

                bin.Write(21); // version

                //v21 addition
                bin.Write(FarmerCharge);

                //v20 addition
                bin.Write(RancherCharge);

                //v19 addition
                bin.Write(ExtendedCharge);

                //v18 addition
                bin.Write(NeedsGround);

                //v17 addition
                bin.Write(NecromancerCharge);

                //v16 addition
                bin.Write(HouseClearance);

                //v15 addition
                bin.Write(TStoneUseRange);

                //v14 addition
                bin.Write(ReqGuildedPercentage);

                //v13 addition
                bin.Write(NeutralHouseClearance);
                bin.Write(DecorationClearance);

                //v12 addition
                bin.Write(WallBuildTicks);

                //v11 addition
                bin.Write(AFKCheck);

                //v10 addition
                bin.Write(WallDamageTicks);
                bin.Write(WallDamageDelay);
                bin.Write(WallRepairTicks);
                bin.Write(WallRepairDelay);

                //v9 addition
                bin.Write(WallHitsDecay);

                //v8 addition
                bin.Write(AuctioneerPayoutPercentage);

                //v7 addition
                bin.Write(MinstrelCharge);

                //v6 addition
                bin.Write(FightBrokerCharge);

                //v5 addition
                bin.Write(TeleporterClearance);

                //v4 addition
                bin.Write(InitialFunds);

                //v3 additions
                bin.Write(TSDeedCost);
                bin.Write(GuildHousePercentage);
                bin.Write(LLModifierNone);
                bin.Write(LLModifierLow);
                bin.Write(LLModifierMed);
                bin.Write(LLModifierHigh);
                bin.Write(LLModifierBoom);
                bin.Write(NPCModifierNone);
                bin.Write(NPCModifierLow);
                bin.Write(NPCModifierMed);
                bin.Write(NPCModifierHigh);
                bin.Write(NPCModifierBoom);
                bin.Write(BaseModifierNone);
                bin.Write(BaseModifierLow);
                bin.Write(BaseModifierMed);
                bin.Write(BaseModifierHigh);
                bin.Write(BaseModifierBoom);

                //begin v2 additions
                bin.Write(BaseFee);
                bin.Write(ExtendedFee);
                bin.Write(NoGateOutFee);
                bin.Write(NoGateInFee);
                bin.Write(NoRecallOutFee);
                bin.Write(NoRecallInFee);
                bin.Write(LawlessFee);
                bin.Write(LawAuthFee);
                bin.Write(NPCType1Fee);
                bin.Write(NPCType2Fee);
                bin.Write(NPCType3Fee);
                bin.Write(LawNormCharge);
                bin.Write(LawlessCharge);
                bin.Write(LawAuthCharge);
                bin.Write(ChangeTravelCharge);
                bin.Write(UpdateEnemyCharge);
                bin.Write(EmissaryCharge);
                bin.Write(EvocatorCharge);
                bin.Write(AlchemistCharge);
                bin.Write(AnimalTrainerCharge);
                bin.Write(BankerCharge);
                bin.Write(InnkeeperCharge);
                bin.Write(MageCharge);
                bin.Write(ProvisionerCharge);
                bin.Write(ArmsTrainerCharge);
                bin.Write(MageTrainerCharge);
                bin.Write(RogueTrainerCharge);
                bin.Write(LookoutCharge);
                bin.Write(TownCrierCharge);

                //v1 below
                bin.Write(Hue);
                bin.Write(NoneToLow);
                bin.Write(LowToMedium);
                bin.Write(MediumToHigh);
                bin.Write(HighToBooming);

                bin.Close();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static void OnLoad()
        {
            try
            {
                Console.WriteLine("TownshipSettings Loading...");
                string filePath = Path.Combine("Saves/AngelIsland", "Township.bin");

                if (!File.Exists(filePath))
                    return;

                BinaryFileReader datreader = new BinaryFileReader(new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read)));
                int version = datreader.ReadInt();

                switch (version)
                {
                    case 21:
                        FarmerCharge = datreader.ReadInt();
                        goto case 20;
                    case 20:
                        RancherCharge = datreader.ReadInt();
                        goto case 19;
                    case 19:
                        ExtendedCharge = datreader.ReadInt();
                        goto case 18;
                    case 18:
                        NeedsGround = datreader.ReadBool();
                        goto case 17;
                    case 17:
                        NecromancerCharge = datreader.ReadInt();
                        goto case 16;
                    case 16:
                        HouseClearance = datreader.ReadInt();
                        goto case 15;
                    case 15:
                        TStoneUseRange = datreader.ReadInt();
                        goto case 14;
                    case 14:
                        ReqGuildedPercentage = datreader.ReadDouble();
                        goto case 13;
                    case 13:
                        NeutralHouseClearance = datreader.ReadInt();
                        DecorationClearance = datreader.ReadInt();
                        goto case 12;
                    case 12:
                        WallBuildTicks = datreader.ReadInt();
                        goto case 11;
                    case 11:
                        AFKCheck = datreader.ReadBool();
                        goto case 10;
                    case 10:
                        WallDamageTicks = datreader.ReadInt();
                        WallDamageDelay = datreader.ReadTimeSpan();
                        WallRepairTicks = datreader.ReadInt();
                        WallRepairDelay = datreader.ReadTimeSpan();
                        goto case 9;
                    case 9:
                        WallHitsDecay = datreader.ReadInt();
                        goto case 8;
                    case 8:
                        AuctioneerPayoutPercentage = datreader.ReadDouble();
                        goto case 7;
                    case 7:
                        MinstrelCharge = datreader.ReadInt();
                        goto case 6;
                    case 6:
                        FightBrokerCharge = datreader.ReadInt();
                        goto case 5;
                    case 5:
                        TeleporterClearance = datreader.ReadInt();
                        goto case 4;
                    case 4:
                        InitialFunds = datreader.ReadInt();
                        goto case 3;
                    case 3:
                        TSDeedCost = datreader.ReadInt();

                        GuildHousePercentage = datreader.ReadDouble();

                        LLModifierNone = datreader.ReadDouble();
                        LLModifierLow = datreader.ReadDouble();
                        LLModifierMed = datreader.ReadDouble();
                        LLModifierHigh = datreader.ReadDouble();
                        LLModifierBoom = datreader.ReadDouble();

                        NPCModifierNone = datreader.ReadDouble();
                        NPCModifierLow = datreader.ReadDouble();
                        NPCModifierMed = datreader.ReadDouble();
                        NPCModifierHigh = datreader.ReadDouble();
                        NPCModifierBoom = datreader.ReadDouble();

                        BaseModifierNone = datreader.ReadDouble();
                        BaseModifierLow = datreader.ReadDouble();
                        BaseModifierMed = datreader.ReadDouble();
                        BaseModifierHigh = datreader.ReadDouble();
                        BaseModifierBoom = datreader.ReadDouble();

                        goto case 2;
                    case 2:
                        BaseFee = datreader.ReadInt();
                        ExtendedFee = datreader.ReadInt();
                        NoGateOutFee = datreader.ReadInt();
                        NoGateInFee = datreader.ReadInt();
                        NoRecallOutFee = datreader.ReadInt();
                        NoRecallInFee = datreader.ReadInt();
                        LawlessFee = datreader.ReadInt();
                        LawAuthFee = datreader.ReadInt();
                        NPCType1Fee = datreader.ReadInt();
                        NPCType2Fee = datreader.ReadInt();
                        NPCType3Fee = datreader.ReadInt();
                        LawNormCharge = datreader.ReadInt();
                        LawlessCharge = datreader.ReadInt();
                        LawAuthCharge = datreader.ReadInt();
                        ChangeTravelCharge = datreader.ReadInt();
                        UpdateEnemyCharge = datreader.ReadInt();
                        EmissaryCharge = datreader.ReadInt();
                        EvocatorCharge = datreader.ReadInt();
                        AlchemistCharge = datreader.ReadInt();
                        AnimalTrainerCharge = datreader.ReadInt();
                        BankerCharge = datreader.ReadInt();
                        InnkeeperCharge = datreader.ReadInt();
                        MageCharge = datreader.ReadInt();
                        ProvisionerCharge = datreader.ReadInt();
                        ArmsTrainerCharge = datreader.ReadInt();
                        MageTrainerCharge = datreader.ReadInt();
                        RogueTrainerCharge = datreader.ReadInt();
                        LookoutCharge = datreader.ReadInt();
                        TownCrierCharge = datreader.ReadInt();
                        goto case 1;
                    case 1:
                        Hue = datreader.ReadInt();
                        NoneToLow = datreader.ReadInt();
                        LowToMedium = datreader.ReadInt();
                        MediumToHigh = datreader.ReadInt();
                        HighToBooming = datreader.ReadInt();
                        break;
                }

                datreader.Close();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

            //try to initialize informational fee stuff - not crucial as this is informational
            // and gets set when the activity calcs are done
            try
            {
                FeePercentageCalc = (double)BaseFee / 2500.0;
                FeeAcctInfoCount = (int)(FeePercentageCalc * 1000.0);
            }
            catch { }
        }

        #endregion

        //PIX - added these for information - they don't get saved or anything
        public static double FeePercentageCalc = 1.0;
        public static int FeeAcctInfoCount = 1000;

        //The guts... with initial values before load

        public static int Hue = 0x333;    // purple
        public static int RestorationHue = 0x46;     // green

        //these numbers are: number of people who enter the town in a week's time - every char
        // being counted once per day.  So theoretically, if someone knew how this worked, one
        // account could keep the setting at 35 if it logged in each of its characters every
        // day and visited the town.
        public static int NoneToLow = 36;
        public static int LowToMedium = 72;
        public static int MediumToHigh = 144;
        public static int HighToBooming = 288;

        public static int TSDeedCost = 250000;
        public static int InitialFunds = 125000;
        public static int TStoneUseRange = 2;

        //daily fees
        public static int BaseFee = 2500;
        public static int ExtendedFee = 2500;
        public static int NoGateOutFee = 1000;
        public static int NoGateInFee = 1000;
        public static int NoRecallOutFee = 1000;
        public static int NoRecallInFee = 1000;
        public static int LawlessFee = 2000;
        public static int LawAuthFee = 5000;
        public static int NPCType1Fee = 1000;
        public static int NPCType2Fee = 2000;
        public static int NPCType3Fee = 5000;

        //charges
        public static int LawNormCharge = 5000;
        public static int LawlessCharge = 500000;
        public static int LawAuthCharge = 1000000;
        public static int ChangeTravelCharge = 25000;
        public static int UpdateEnemyCharge = 1000;
        public static int ExtendedCharge = 0;

        //npc prices
        public static int EmissaryCharge = 100000;
        public static int EvocatorCharge = 100000;
        public static int AlchemistCharge = 100000;
        public static int AnimalTrainerCharge = 100000;
        public static int StableMasterCharge = 100000;
        public static int BankerCharge = 1000000;
        public static int InnkeeperCharge = 100000;
        public static int MageCharge = 100000;
        public static int ProvisionerCharge = 20000;
        public static int ArmsTrainerCharge = 20000;
        public static int MageTrainerCharge = 20000;
        public static int RogueTrainerCharge = 20000;
        public static int LookoutCharge = 50000;
        public static int FightBrokerCharge = 100000;
        public static int MinstrelCharge = 100000;
        public static int TownCrierCharge = 5000000;
        public static int NecromancerCharge = 50000;
        public static int RancherCharge = 20000;
        public static int FarmerCharge = 20000;

        //placement requirement numbers
        public static double GuildHousePercentage = 0.75;

        //AND FINALLY... modifiers
        public static double LLModifierNone = 10.0;
        public static double LLModifierLow = 5.0;
        public static double LLModifierMed = 2.5;
        public static double LLModifierHigh = 1.0;
        public static double LLModifierBoom = 0.5;

        public static double NPCModifierNone = 10.0;
        public static double NPCModifierLow = 6.0;
        public static double NPCModifierMed = 3.0;
        public static double NPCModifierHigh = 1.5;
        public static double NPCModifierBoom = 1.0;

        public static double BaseModifierNone = 5.0;
        public static double BaseModifierLow = 2.0;
        public static double BaseModifierMed = 1.5;
        public static double BaseModifierHigh = 1.0;
        public static double BaseModifierBoom = 0.75;

        //building stuff
        public static int HouseClearance = 1;
        public static int NeutralHouseClearance = 5;
        public static int DecorationClearance = 1;
        public static int TeleporterClearance = 2;
        public static double ReqGuildedPercentage = 1.0;
        public static bool NeedsGround = true;

        //auctioneer specials
        public static double AuctioneerPayoutPercentage = 1.0; //Initial setting is 100% is paid to the auction owner

        //wall damage/repair
        public static int WallBuildTicks = 120;
        public static int WallHitsDecay = 0;
        public static int WallDamageTicks = 120;
        public static TimeSpan WallDamageDelay = TimeSpan.FromHours(5.0);
        public static int WallRepairTicks = 120;
        public static TimeSpan WallRepairDelay = TimeSpan.FromHours(5.0);
        public static bool AFKCheck = true;
    }
}