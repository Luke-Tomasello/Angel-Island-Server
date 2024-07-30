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

/* Scripts/Engines/Plants/PlantSystem.cs
 * CHANGELOG:
 *  4/22/23, Yoar
 *      Added Enabled switch
 *  3/13/22, Adam (NextGrowth)
 *      Add a set property so cron can reliably grow plants.
 *	9/22/04 - Pix
 *		Made DoGlobalPlantGrowthCheck() return the number of plants checked.
 *	9/16/04 - Pix
 *		Changed so plants are checked for growth on server up.
 *		They now use the Heartbeat system.
 */

using System;

namespace Server.Engines.Plants
{
    public enum PlantHealth
    {
        Dying,
        Wilted,
        Healthy,
        Vibrant
    }

    public enum PlantGrowthIndicator
    {
        None,
        InvalidLocation,
        NotHealthy,
        Delay,
        Grown,
        DoubleGrown
    }

    [PropertyObject]
    public class PlantSystem
    {
        public static bool Enabled { get { return Core.RuleSets.PlantSystem(); } }

        public static readonly TimeSpan CheckDelay = TimeSpan.FromHours(23.0);

        private PlantItem m_Plant;
        private bool m_FertileDirt;

        private DateTime m_NextGrowth;
        private PlantGrowthIndicator m_GrowthIndicator;

        private int m_Water;

        private int m_Hits;
        private int m_Infestation;
        private int m_Fungus;
        private int m_Poison;
        private int m_Disease;
        private int m_PoisonPotion;
        private int m_CurePotion;
        private int m_HealPotion;
        private int m_StrengthPotion;

        private bool m_Pollinated;
        private PlantType m_SeedType;
        private PlantHue m_SeedHue;
        private int m_AvailableSeeds;
        private int m_LeftSeeds;

        private int m_AvailableResources;
        private int m_LeftResources;

        public PlantItem Plant { get { return m_Plant; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool FertileDirt
        {
            get { return m_FertileDirt; }
            set { m_FertileDirt = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextGrowth
        {
            get { return m_NextGrowth; }
            // 3/14/22, Adam: Note: we manually set this in CronTasks when our timer executes the PlantGrowth
            //  job to ensure we are in control of plant growth
            set { m_NextGrowth = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantGrowthIndicator GrowthIndicator
        {
            get { return m_GrowthIndicator; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFullWater { get { return m_Water >= 4; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Water
        {
            get { return m_Water; }
            set
            {
                if (value < 0)
                    m_Water = 0;
                else if (value > 4)
                    m_Water = 4;
                else
                    m_Water = value;

                m_Plant.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Hits
        {
            get { return m_Hits; }
            set
            {
                if (m_Hits == value)
                    return;

                if (value < 0)
                    m_Hits = 0;
                else if (value > MaxHits)
                    m_Hits = MaxHits;
                else
                    m_Hits = value;

                if (m_Hits == 0)
                    m_Plant.Die();

                m_Plant.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxHits
        {
            get { return 10 + (int)m_Plant.PlantStatus * 2; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantHealth Health
        {
            get
            {
                int perc = m_Hits * 100 / MaxHits;

                if (perc < 33)
                    return PlantHealth.Dying;
                else if (perc < 66)
                    return PlantHealth.Wilted;
                else if (perc < 100)
                    return PlantHealth.Healthy;
                else
                    return PlantHealth.Vibrant;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Infestation
        {
            get { return m_Infestation; }
            set
            {
                if (value < 0)
                    m_Infestation = 0;
                else if (value > 2)
                    m_Infestation = 2;
                else
                    m_Infestation = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Fungus
        {
            get { return m_Fungus; }
            set
            {
                if (value < 0)
                    m_Fungus = 0;
                else if (value > 2)
                    m_Fungus = 2;
                else
                    m_Fungus = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Poison
        {
            get { return m_Poison; }
            set
            {
                if (value < 0)
                    m_Poison = 0;
                else if (value > 2)
                    m_Poison = 2;
                else
                    m_Poison = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Disease
        {
            get { return m_Disease; }
            set
            {
                if (value < 0)
                    m_Disease = 0;
                else if (value > 2)
                    m_Disease = 2;
                else
                    m_Disease = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFullPoisonPotion { get { return m_PoisonPotion >= 2; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PoisonPotion
        {
            get { return m_PoisonPotion; }
            set
            {
                if (value < 0)
                    m_PoisonPotion = 0;
                else if (value > 2)
                    m_PoisonPotion = 2;
                else
                    m_PoisonPotion = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFullCurePotion { get { return m_CurePotion >= 2; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurePotion
        {
            get { return m_CurePotion; }
            set
            {
                if (value < 0)
                    m_CurePotion = 0;
                else if (value > 2)
                    m_CurePotion = 2;
                else
                    m_CurePotion = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFullHealPotion { get { return m_HealPotion >= 2; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HealPotion
        {
            get { return m_HealPotion; }
            set
            {
                if (value < 0)
                    m_HealPotion = 0;
                else if (value > 2)
                    m_HealPotion = 2;
                else
                    m_HealPotion = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFullStrengthPotion { get { return m_StrengthPotion >= 2; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int StrengthPotion
        {
            get { return m_StrengthPotion; }
            set
            {
                if (value < 0)
                    m_StrengthPotion = 0;
                else if (value > 2)
                    m_StrengthPotion = 2;
                else
                    m_StrengthPotion = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasMaladies
        {
            get { return Infestation > 0 || Fungus > 0 || Poison > 0 || Disease > 0 || Water != 2; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PollenProducing
        {
            get { return PlantHueInfo.IsCrossable(m_Plant.PlantHue) && m_Plant.PlantStatus >= PlantStatus.FullGrownPlant; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Pollinated
        {
            get { return m_Pollinated; }
            set { m_Pollinated = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantType SeedType
        {
            get
            {
                if (m_Pollinated)
                    return m_SeedType;
                else
                    return m_Plant.PlantType;
            }
            set { m_SeedType = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantHue SeedHue
        {
            get
            {
                if (m_Pollinated)
                    return m_SeedHue;
                else
                    return m_Plant.PlantHue;
            }
            set { m_SeedHue = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AvailableSeeds
        {
            get { return m_AvailableSeeds; }
            set { if (value >= 0) m_AvailableSeeds = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LeftSeeds
        {
            get { return m_LeftSeeds; }
            set { if (value >= 0) m_LeftSeeds = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AvailableResources
        {
            get { return m_AvailableResources; }
            set { if (value >= 0) m_AvailableResources = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LeftResources
        {
            get { return m_LeftResources; }
            set { if (value >= 0) m_LeftResources = value; }
        }

        public PlantSystem(PlantItem plant, bool fertileDirt)
        {
            m_Plant = plant;
            m_FertileDirt = fertileDirt;

            m_NextGrowth = DateTime.UtcNow + CheckDelay;
            m_GrowthIndicator = PlantGrowthIndicator.None;
            m_Hits = MaxHits;
            m_LeftSeeds = 8;
            m_LeftResources = 8;
        }

        public void Reset(bool potions)
        {
            m_NextGrowth = DateTime.UtcNow + CheckDelay;
            m_GrowthIndicator = PlantGrowthIndicator.None;

            Hits = MaxHits;
            m_Infestation = 0;
            m_Fungus = 0;
            m_Poison = 0;
            m_Disease = 0;

            if (potions)
            {
                m_PoisonPotion = 0;
                m_CurePotion = 0;
                m_HealPotion = 0;
                m_StrengthPotion = 0;
            }

            m_Pollinated = false;
            m_AvailableSeeds = 0;
            m_LeftSeeds = 8;

            m_AvailableResources = 0;
            m_LeftResources = 8;
        }

        public int GetLocalizedDirtStatus()
        {
            if (Water <= 1)
                return 1060826; // hard
            else if (Water <= 2)
                return 1060827; // soft
            else if (Water <= 3)
                return 1060828; // squishy
            else
                return 1060829; // sopping wet
        }

        public int GetLocalizedHealth()
        {
            switch (Health)
            {
                case PlantHealth.Dying: return 1060825; // dying
                case PlantHealth.Wilted: return 1060824; // wilted
                case PlantHealth.Healthy: return 1060823; // healthy
                default: return 1060822; // vibrant
            }
        }

        public static void Configure()
        {
            //9/13/04 - Pix - removing check for growth from world load
            // to our new heartbeat system.
            //EventSink.WorldLoad += new WorldLoadEventHandler( EventSink_WorldLoad );
            //EventSink.WorldSave += new WorldSaveEventHandler( EventSink_WorldSave );
            EventSink.Login += new LoginEventHandler(EventSink_Login);
        }

        private static void EventSink_Login(LoginEventArgs args)
        {
            Mobile from = args.Mobile;

            if (from.Backpack != null)
            {
                Item[] plants = from.Backpack.FindItemsByType(typeof(PlantItem));

                foreach (PlantItem plant in plants)
                {
                    if (plant.IsGrowable)
                        plant.PlantSystem.DoGrowthCheck();
                }
            }

            if (from.BankBox != null)
            {
                Item[] plants = from.BankBox.FindItemsByType(typeof(PlantItem));

                foreach (PlantItem plant in plants)
                {
                    if (plant.IsGrowable)
                        plant.PlantSystem.DoGrowthCheck();
                }
            }
        }

        //9/13/04 - Pix - removing check for growth from world load
        // to our new heartbeat system.
        //		private static void EventSink_WorldLoad()
        // 6/23/2021, Adam: Moved to heartbeat
        /*public static int DoGlobalPlantGrowthCheck()
		{
			int count = 0;

			foreach (Item item in World.Items.Values)
			{
				PlantItem plant = item as PlantItem;

				if (plant != null && plant.IsGrowable && (plant.RootParent as Mobile) == null && DateTime.UtcNow >= plant.PlantSystem.NextGrowth)
				{
					count++;
					plant.PlantSystem.DoGrowthCheck();
				}
			}

			return count;
		}*/

        private static void EventSink_WorldSave(WorldSaveEventArgs args)
        {
            foreach (Item item in World.Items.Values)
            {
                PlantItem plant = item as PlantItem;

                if (plant != null && plant.IsGrowable && (plant.RootParent as Mobile) == null && DateTime.UtcNow >= plant.PlantSystem.NextGrowth)
                    plant.PlantSystem.DoGrowthCheck();
            }
        }

        public void DoGrowthCheck()
        {
            if (!m_Plant.IsGrowable)
                return;

            if (DateTime.UtcNow < m_NextGrowth)
            {
                m_GrowthIndicator = PlantGrowthIndicator.Delay;
                return;
            }

            m_NextGrowth = DateTime.UtcNow + CheckDelay;

            if (!m_Plant.ValidGrowthLocation)
            {
                m_GrowthIndicator = PlantGrowthIndicator.InvalidLocation;
                return;
            }

            if (m_Plant.PlantStatus == PlantStatus.BowlOfDirt)
            {
                if (Water > 2 || Utility.RandomDouble() < 0.9)
                    Water--;
                return;
            }

            ApplyBeneficEffects();

            if (!ApplyMaladiesEffects()) // Dead
                return;

            Grow();

            UpdateMaladies();
        }

        private void ApplyBeneficEffects()
        {
            if (PoisonPotion >= Infestation)
            {
                PoisonPotion -= Infestation;
                Infestation = 0;
            }
            else
            {
                Infestation -= PoisonPotion;
                PoisonPotion = 0;
            }

            if (CurePotion >= Fungus)
            {
                CurePotion -= Fungus;
                Fungus = 0;
            }
            else
            {
                Fungus -= CurePotion;
                CurePotion = 0;
            }

            if (HealPotion >= Poison)
            {
                HealPotion -= Poison;
                Poison = 0;
            }
            else
            {
                Poison -= HealPotion;
                HealPotion = 0;
            }

            if (HealPotion >= Disease)
            {
                HealPotion -= Disease;
                Disease = 0;
            }
            else
            {
                Disease -= HealPotion;
                HealPotion = 0;
            }

            if (!HasMaladies)
            {
                if (HealPotion > 0)
                    Hits += HealPotion * 7;
                else
                    Hits += 2;
            }

            HealPotion = 0;
        }

        private bool ApplyMaladiesEffects()
        {
            int damage = 0;

            if (Infestation > 0)
                damage += Infestation * Utility.RandomMinMax(3, 6);

            if (Fungus > 0)
                damage += Fungus * Utility.RandomMinMax(3, 6);

            if (Poison > 0)
                damage += Poison * Utility.RandomMinMax(3, 6);

            if (Disease > 0)
                damage += Disease * Utility.RandomMinMax(3, 6);

            if (Water > 2)
                damage += (Water - 2) * Utility.RandomMinMax(3, 6);
            else if (Water < 2)
                damage += (2 - Water) * Utility.RandomMinMax(3, 6);

            Hits -= damage;

            return m_Plant.IsGrowable && m_Plant.PlantStatus != PlantStatus.BowlOfDirt;
        }

        private void Grow()
        {
            if (Health < PlantHealth.Healthy)
            {
                m_GrowthIndicator = PlantGrowthIndicator.NotHealthy;
            }
            else if (m_FertileDirt && m_Plant.PlantStatus <= PlantStatus.Stage5 && Utility.RandomDouble() < 0.1)
            {
                int curStage = (int)m_Plant.PlantStatus;
                m_Plant.PlantStatus = (PlantStatus)(curStage + 2);

                m_GrowthIndicator = PlantGrowthIndicator.DoubleGrown;
            }
            #region Apiculture
            else if (m_Plant.PlantStatus <= PlantStatus.Stage5 && Engines.Apiculture.ApicultureSystem.CheckDoubleGrow(m_Plant))
            {
                int curStage = (int)m_Plant.PlantStatus;
                m_Plant.PlantStatus = (PlantStatus)(curStage + 2);

                m_GrowthIndicator = PlantGrowthIndicator.DoubleGrown;
            }
            #endregion
            else if (m_Plant.PlantStatus < PlantStatus.Stage9)
            {
                int curStage = (int)m_Plant.PlantStatus;
                m_Plant.PlantStatus = (PlantStatus)(curStage + 1);

                m_GrowthIndicator = PlantGrowthIndicator.Grown;
            }
            else
            {
                if (Pollinated && LeftSeeds > 0 && PlantHueInfo.IsCrossable(m_Plant.PlantHue))
                {
                    LeftSeeds--;
                    AvailableSeeds++;
                }

                if (LeftResources > 0 && PlantResourceInfo.GetInfo(m_Plant.PlantType, m_Plant.PlantHue) != null)
                {
                    LeftResources--;
                    AvailableResources++;
                }

                m_GrowthIndicator = PlantGrowthIndicator.Grown;
            }

            if (m_Plant.PlantStatus >= PlantStatus.Stage9 && !Pollinated)
            {
                Pollinated = true;
                SeedType = m_Plant.PlantType;
                SeedHue = m_Plant.PlantHue;
            }
        }

        private void UpdateMaladies()
        {
            double infestationChance = 0.30 - StrengthPotion * 0.075 + (Water - 2) * 0.10;

            PlantTypeInfo typeInfo = PlantTypeInfo.GetInfo(m_Plant.PlantType);
            if (typeInfo.Flowery)
                infestationChance += 0.10;

            if (PlantHueInfo.IsBright(m_Plant.PlantHue))
                infestationChance += 0.10;

            if (Utility.RandomDouble() < infestationChance)
                Infestation++;


            double fungusChance = 0.15 - StrengthPotion * 0.075 + (Water - 2) * 0.10;

            if (Utility.RandomDouble() < fungusChance)
                Fungus++;

            if (Water > 2 || Utility.RandomDouble() < 0.9)
                Water--;

            if (PoisonPotion > 0)
            {
                Poison += PoisonPotion;
                PoisonPotion = 0;
            }

            if (CurePotion > 0)
            {
                Disease += CurePotion;
                CurePotion = 0;
            }

            StrengthPotion = 0;
        }

        public void Save(GenericWriter writer)
        {
            writer.Write((int)2); // version

            writer.Write((bool)m_FertileDirt);

            writer.Write((DateTime)m_NextGrowth);
            writer.Write((int)m_GrowthIndicator);

            writer.Write((int)m_Water);

            writer.Write((int)m_Hits);
            writer.Write((int)m_Infestation);
            writer.Write((int)m_Fungus);
            writer.Write((int)m_Poison);
            writer.Write((int)m_Disease);
            writer.Write((int)m_PoisonPotion);
            writer.Write((int)m_CurePotion);
            writer.Write((int)m_HealPotion);
            writer.Write((int)m_StrengthPotion);

            writer.Write((bool)m_Pollinated);
            writer.Write((int)m_SeedType);
            writer.Write((int)m_SeedHue);
            writer.Write((int)m_AvailableSeeds);
            writer.Write((int)m_LeftSeeds);

            writer.Write((int)m_AvailableResources);
            writer.Write((int)m_LeftResources);
        }

        public PlantSystem(PlantItem plant, GenericReader reader)
        {
            m_Plant = plant;

            int version = reader.ReadInt();

            m_FertileDirt = reader.ReadBool();

            if (version >= 1)
                m_NextGrowth = reader.ReadDateTime();
            else
                m_NextGrowth = reader.ReadDeltaTime();

            m_GrowthIndicator = (PlantGrowthIndicator)reader.ReadInt();

            m_Water = reader.ReadInt();

            m_Hits = reader.ReadInt();
            m_Infestation = reader.ReadInt();
            m_Fungus = reader.ReadInt();
            m_Poison = reader.ReadInt();
            m_Disease = reader.ReadInt();
            m_PoisonPotion = reader.ReadInt();
            m_CurePotion = reader.ReadInt();
            m_HealPotion = reader.ReadInt();
            m_StrengthPotion = reader.ReadInt();

            m_Pollinated = reader.ReadBool();

            if (version < 2)
                m_SeedType = (PlantType)Seed.OldTreeConv(reader.ReadInt());
            else
                m_SeedType = (PlantType)reader.ReadInt();

            m_SeedHue = (PlantHue)reader.ReadInt();
            m_AvailableSeeds = reader.ReadInt();
            m_LeftSeeds = reader.ReadInt();

            m_AvailableResources = reader.ReadInt();
            m_LeftResources = reader.ReadInt();
        }

        public override string ToString()
        {
            return "...";
        }
    }
}