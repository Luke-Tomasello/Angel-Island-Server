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

/* scripts\Engines\Factions\Core\FactionConfig.cs
 * CHANGELOG:
 *  1/1/23, Yoar
 *      Initial commit.
 */

using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Server.Factions
{
    public static class FactionConfig
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
        }

        /* Election Options */

        public static TimeSpan ElectionPendingPeriod = TimeSpan.FromDays(5.0);
        public static TimeSpan ElectionCampaignPeriod = TimeSpan.FromDays(1.0);
        public static TimeSpan ElectionVotingPeriod = TimeSpan.FromDays(3.0);

        public static int ElectionMaxCandidates = 10;
        public static int ElectionCandidateRank = 5;

        /* Leave Options */

        public static TimeSpan LeavePeriod = TimeSpan.FromDays(7.0);

        /* Skill Loss Options */

        public static double SkillLossFactor = 1.0 / 3;
        public static TimeSpan SkillLossPeriod = TimeSpan.FromMinutes(20.0);

        /* Stability Options */

        public static int StabilityFactor = 300; // 300% greater (3 times) than smallest faction
        public static int StabilityActivation = 200; // Stablity code goes into effect when largest faction has > 200 people

        /* Faction Item Options */

        public static TimeSpan ItemExpirationPeriod = TimeSpan.FromDays(21.0);

        /* Broadcast Options */

        public static int BroadcastsPerPeriod = 2;
        public static TimeSpan BroadcastPeriod = TimeSpan.FromHours(1.0);

        /* Silver Award Options */

        // Time between two consecutive silver gains (due to killing/disarming) from the same player
        public static TimeSpan SilverGivenExpirePeriod = TimeSpan.FromHours(3.0);

        /* Town Options */

        public static TimeSpan TownTaxChangePeriod = TimeSpan.FromHours(12.0);
        public static TimeSpan TownIncomePeriod = TimeSpan.FromDays(1.0);
        public static int TownCaptureSilver = 10000;

        public static bool TownSqualorEnabled = false;
        public static int TownSoftSilverCap = 1200000; // at the soft silver cap, the amount of squalor equals the max. attainable income of 40K

        /* War Horse Options */

        public static int WarHorseSilverPrice = 500;
        public static int WarHorseGoldPrice = 3000;
        public static bool WarHorseRankRequired = true;

        /* Sigil Options */

        // Time corrupting faction has to return the sigil before corruption time resets
        public static TimeSpan SigilCorruptionGrace = TimeSpan.FromMinutes((Core.RuleSets.SERules()) ? 30.0 : 15.0);

        // Sigil must be held at a stronghold for this amount of time in order to become corrupted
        public static TimeSpan SigilCorruptionPeriod = ((Core.RuleSets.SERules()) ? TimeSpan.FromHours(10.0) : TimeSpan.FromHours(24.0));

        // After a sigil has been corrupted it must be returned to the town within this period of time
        public static TimeSpan SigilReturnPeriod = TimeSpan.FromHours(1.0);

        // Once it's been returned the corrupting faction owns the town for this period of time
        public static TimeSpan SigilPurificationPeriod = TimeSpan.FromDays(3.0);

        /* Gump Options */

        public static bool NewGumps = false;

        #region Save/Load

        private const string FilePath = "Saves/FactionConfig.xml";

        public static void OnSave(WorldSaveEventArgs e)
        {
            try
            {
                Console.WriteLine("FactionConfig Saving...");

                string directoryName = Path.GetDirectoryName(FilePath);

                if (!String.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                XmlTextWriter writer = new XmlTextWriter(FilePath, Encoding.Default);
                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument(true);
                writer.WriteStartElement("FactionConfig");
                writer.WriteAttributeString("version", "2");

                try
                {
                    // version 2

                    writer.WriteElementString("NewGumps", NewGumps.ToString());

                    // version 1

                    writer.WriteElementString("TownSqualorEnabled", TownSqualorEnabled.ToString());
                    writer.WriteElementString("TownSoftSilverCap", TownSoftSilverCap.ToString());

                    // version 0

                    writer.WriteElementString("ElectionPendingPeriod", ElectionPendingPeriod.ToString());
                    writer.WriteElementString("ElectionCampaignPeriod", ElectionCampaignPeriod.ToString());
                    writer.WriteElementString("ElectionVotingPeriod", ElectionVotingPeriod.ToString());

                    writer.WriteElementString("ElectionMaxCandidates", ElectionMaxCandidates.ToString());
                    writer.WriteElementString("ElectionCandidateRank", ElectionCandidateRank.ToString());

                    writer.WriteElementString("LeavePeriod", LeavePeriod.ToString());

                    writer.WriteElementString("SkillLossFactor", SkillLossFactor.ToString());
                    writer.WriteElementString("SkillLossPeriod", SkillLossPeriod.ToString());

                    writer.WriteElementString("StabilityFactor", StabilityFactor.ToString());
                    writer.WriteElementString("StabilityActivation", StabilityActivation.ToString());

                    writer.WriteElementString("ItemExpirationPeriod", ItemExpirationPeriod.ToString());

                    writer.WriteElementString("BroadcastsPerPeriod", BroadcastsPerPeriod.ToString());
                    writer.WriteElementString("BroadcastPeriod", BroadcastPeriod.ToString());

                    writer.WriteElementString("SilverGivenExpirePeriod", SilverGivenExpirePeriod.ToString());

                    writer.WriteElementString("TownTaxChangePeriod", TownTaxChangePeriod.ToString());
                    writer.WriteElementString("TownIncomePeriod", TownIncomePeriod.ToString());
                    writer.WriteElementString("TownCaptureSilver", TownCaptureSilver.ToString());

                    writer.WriteElementString("WarHorseSilverPrice", WarHorseSilverPrice.ToString());
                    writer.WriteElementString("WarHorseGoldPrice", WarHorseGoldPrice.ToString());
                    writer.WriteElementString("WarHorseRankRequired", WarHorseRankRequired.ToString());

                    writer.WriteElementString("SigilCorruptionGrace", SigilCorruptionGrace.ToString());
                    writer.WriteElementString("SigilCorruptionPeriod", SigilCorruptionPeriod.ToString());
                    writer.WriteElementString("SigilReturnPeriod", SigilReturnPeriod.ToString());
                    writer.WriteElementString("SigilPurificationPeriod", SigilPurificationPeriod.ToString());
                }
                finally
                {
                    writer.WriteEndDocument();
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        public static void OnLoad()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return;

                Console.WriteLine("FactionConfig Loading...");

                XmlTextReader reader = new XmlTextReader(FilePath);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                int version = Int32.Parse(reader.GetAttribute("version"));
                reader.ReadStartElement("FactionConfig");

                switch (version)
                {
                    case 2:
                        {
                            NewGumps = Boolean.Parse(reader.ReadElementString("NewGumps"));

                            goto case 1;
                        }
                    case 1:
                        {
                            TownSqualorEnabled = Boolean.Parse(reader.ReadElementString("TownSqualorEnabled"));
                            TownSoftSilverCap = Int32.Parse(reader.ReadElementString("TownSoftSilverCap"));

                            goto case 0;
                        }
                    case 0:
                        {
                            ElectionPendingPeriod = TimeSpan.Parse(reader.ReadElementString("ElectionPendingPeriod"));
                            ElectionCampaignPeriod = TimeSpan.Parse(reader.ReadElementString("ElectionCampaignPeriod"));
                            ElectionVotingPeriod = TimeSpan.Parse(reader.ReadElementString("ElectionVotingPeriod"));

                            ElectionMaxCandidates = Int32.Parse(reader.ReadElementString("ElectionMaxCandidates"));
                            ElectionCandidateRank = Int32.Parse(reader.ReadElementString("ElectionCandidateRank"));

                            LeavePeriod = TimeSpan.Parse(reader.ReadElementString("LeavePeriod"));

                            SkillLossFactor = Double.Parse(reader.ReadElementString("SkillLossFactor"));
                            SkillLossPeriod = TimeSpan.Parse(reader.ReadElementString("SkillLossPeriod"));

                            StabilityFactor = Int32.Parse(reader.ReadElementString("StabilityFactor"));
                            StabilityActivation = Int32.Parse(reader.ReadElementString("StabilityActivation"));

                            ItemExpirationPeriod = TimeSpan.Parse(reader.ReadElementString("ItemExpirationPeriod"));

                            BroadcastsPerPeriod = Int32.Parse(reader.ReadElementString("BroadcastsPerPeriod"));
                            BroadcastPeriod = TimeSpan.Parse(reader.ReadElementString("BroadcastPeriod"));

                            SilverGivenExpirePeriod = TimeSpan.Parse(reader.ReadElementString("SilverGivenExpirePeriod"));

                            TownTaxChangePeriod = TimeSpan.Parse(reader.ReadElementString("TownTaxChangePeriod"));
                            TownIncomePeriod = TimeSpan.Parse(reader.ReadElementString("TownIncomePeriod"));
                            TownCaptureSilver = Int32.Parse(reader.ReadElementString("TownCaptureSilver"));

                            WarHorseSilverPrice = Int32.Parse(reader.ReadElementString("WarHorseSilverPrice"));
                            WarHorseGoldPrice = Int32.Parse(reader.ReadElementString("WarHorseGoldPrice"));
                            WarHorseRankRequired = Boolean.Parse(reader.ReadElementString("WarHorseRankRequired"));

                            SigilCorruptionGrace = TimeSpan.Parse(reader.ReadElementString("SigilCorruptionGrace"));
                            SigilCorruptionPeriod = TimeSpan.Parse(reader.ReadElementString("SigilCorruptionPeriod"));
                            SigilReturnPeriod = TimeSpan.Parse(reader.ReadElementString("SigilReturnPeriod"));
                            SigilPurificationPeriod = TimeSpan.Parse(reader.ReadElementString("SigilPurificationPeriod"));

                            break;
                        }
                }

                reader.ReadEndElement();

                reader.Close();
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        #endregion
    }
}