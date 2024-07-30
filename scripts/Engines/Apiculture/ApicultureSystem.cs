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

/* scripts\Engines\Apiculture\ApicultureSystem.cs
 * CHANGELOG:
 *  8/8/23, Yoar
 *      Initial commit.
 */

using Server.Engines.Plants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Server.Engines.Apiculture
{
    public enum HiveStage : byte
    {
        Stage0,
        Stage1,
        Stage2,
        Stage3,
        Stage4,
        Stage5,

        Empty = 0,
        Colonizing = 1,
        Brooding = 3,
        Producing = 5
    }

    public enum HiveGrowthResult : byte
    {
        None,
        NotHealthy,
        LowResources,
        Grown,
        PopulationUp,
        PopulationDown
    }

    public enum HiveHealthStatus : byte
    {
        Dying,
        Sickly,
        Healthy,
        Thriving
    }

    public enum HiveResourceStatus : byte
    {
        None,
        VeryLow,
        Low,
        Normal,
        High,
        VeryHigh,
    }

    public static class ApicultureSystem
    {
        public static bool Enabled { get { return Core.RuleSets.AllShards; } }

        public static bool Competition = true;
        public static int MaxPopulation = 10;
        public static int MaxHoney = 255;
        public static int MaxWax = 255;
        public static int CompetitionRange = 4;
        public static int SwarmMinSkill = -90;
        public static int SwarmMaxSkill = 110;
        public static int PlantDoubleGrowRange = 4;
        public static int PlantDoubleGrowChance = 15;
        public static TimeSpan GrowthDelay = TimeSpan.FromHours(24.0);

        private static readonly List<Beehive> m_Hives = new List<Beehive>();

        public static List<Beehive> Hives { get { return m_Hives; } }

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
        }

        // 11/26/23, Yoar: Check hives on a Cron schedule instead
#if false
        public static void Initialize()
        {
            if (!Enabled)
                return;

            new UpdateTimer().Start();
        }
#endif

        public static void CheckAll()
        {
            foreach (Beehive hive in m_Hives)
                CheckGrowth(hive);
        }

        public static void CheckGrowth(Beehive hive)
        {
            if (DateTime.UtcNow >= hive.Colony.NextGrowth)
                DoGrowth(hive);
        }

        public static void GrowAll()
        {
            foreach (Beehive hive in m_Hives)
                DoGrowth(hive);
        }

        public static void DoGrowth(Beehive hive)
        {
            hive.Colony.NextGrowth = DateTime.UtcNow + GrowthDelay;
            hive.Colony.DoGrowth();
        }

        public static bool SkillCheck(Mobile from, double minSkill, double maxSkill)
        {
            double skillValue = 100.0; // TODO: Get skill

            return SkillCheck(from, (skillValue - minSkill) / (maxSkill - minSkill));
        }

        public static bool SkillCheck(Mobile from, double chance)
        {
            if (chance < 0.0)
                return false;
            else if (chance >= 1.0)
                return true;

            // TODO: Gain skill

            return (Utility.RandomDouble() < chance);
        }

        public static bool CheckDoubleGrow(PlantItem plant)
        {
            if (!Enabled || PlantDoubleGrowChance <= 0 || plant.Parent != null)
                return false;

            bool hasHive = false;

            foreach (Item item in plant.GetItemsInRange(PlantDoubleGrowRange))
            {
                if (item is Beehive)
                {
                    Beehive hive = (Beehive)item;

                    if (hive.Colony.Stage >= HiveStage.Producing)
                    {
                        hasHive = true;
                        break;
                    }
                }
            }

            return (hasHive && Utility.Random(100) < PlantDoubleGrowChance);
        }

#if false
        private class UpdateTimer : Timer
        {
            public UpdateTimer()
                : base(TimeSpan.FromMinutes(5.0), TimeSpan.FromMinutes(5.0))
            {
            }

            protected override void OnTick()
            {
                CheckAll();
            }
        }
#endif

        #region Save/Load

        private const string FilePath = "Saves/ApicultureConfig.xml";

        public static void OnSave(WorldSaveEventArgs e)
        {
            try
            {
                Console.WriteLine("ApicultureConfig Saving...");

                string directoryName = Path.GetDirectoryName(FilePath);

                if (!String.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                XmlTextWriter writer = new XmlTextWriter(FilePath, Encoding.Default);
                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument(true);
                writer.WriteStartElement("ApicultureConfig");
                writer.WriteAttributeString("version", "0");

                try
                {
                    // version 0

                    writer.WriteElementString("Competition", Competition.ToString());
                    writer.WriteElementString("MaxPopulation", MaxPopulation.ToString());
                    writer.WriteElementString("MaxHoney", MaxHoney.ToString());
                    writer.WriteElementString("MaxWax", MaxWax.ToString());
                    writer.WriteElementString("CompetitionRange", CompetitionRange.ToString());
                    writer.WriteElementString("SwarmMinSkill", SwarmMinSkill.ToString());
                    writer.WriteElementString("SwarmMaxSkill", SwarmMaxSkill.ToString());
                    writer.WriteElementString("PlantDoubleGrowRange", PlantDoubleGrowRange.ToString());
                    writer.WriteElementString("PlantDoubleGrowChance", PlantDoubleGrowChance.ToString());
                    writer.WriteElementString("GrowthDelay", GrowthDelay.ToString());
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

                Console.WriteLine("ApicultureConfig Loading...");

                XmlTextReader reader = new XmlTextReader(FilePath);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                int version = Int32.Parse(reader.GetAttribute("version"));
                reader.ReadStartElement("ApicultureConfig");

                switch (version)
                {
                    case 0:
                        {
                            Competition = Boolean.Parse(reader.ReadElementString("Competition"));
                            MaxPopulation = Int32.Parse(reader.ReadElementString("MaxPopulation"));
                            MaxHoney = Int32.Parse(reader.ReadElementString("MaxHoney"));
                            MaxWax = Int32.Parse(reader.ReadElementString("MaxWax"));
                            CompetitionRange = Int32.Parse(reader.ReadElementString("CompetitionRange"));
                            SwarmMinSkill = Int32.Parse(reader.ReadElementString("SwarmMinSkill"));
                            SwarmMaxSkill = Int32.Parse(reader.ReadElementString("SwarmMaxSkill"));
                            PlantDoubleGrowRange = Int32.Parse(reader.ReadElementString("PlantDoubleGrowRange"));
                            PlantDoubleGrowChance = Int32.Parse(reader.ReadElementString("PlantDoubleGrowChance"));
                            GrowthDelay = TimeSpan.Parse(reader.ReadElementString("GrowthDelay"));

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