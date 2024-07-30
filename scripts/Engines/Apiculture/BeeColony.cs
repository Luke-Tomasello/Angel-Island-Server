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

/* scripts\Engines\Apiculture\BeeColony.cs
 * CHANGELOG:
 *  8/8/23, Yoar
 *      Initial commit.
 */

using Server.Engines.Plants;
using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Engines.Apiculture
{
    [PropertyObject]
    public class BeeColony
    {
        private Beehive m_Hive;

        private HiveStage m_Stage;

        private DateTime m_NextGrowth;
        private HiveGrowthResult m_GrowthResult;

        private int m_Population; // in units of 10K
        private int m_Age;
        private int m_Health;

        private int m_ParasiteLevel;
        private int m_DiseaseLevel;

        private int m_ResWater;
        private int m_ResFlowers;
        private int m_ResHives;

        private int m_PotAgility;
        private int m_PotCure;
        private int m_PotHeal;
        private int m_PotPoison;
        private int m_PotStrength;

        private int m_Wax;
        private int m_Honey;

        [CommandProperty(AccessLevel.GameMaster)]
        public Beehive Hive
        {
            get { return m_Hive; }
            set { m_Hive = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HiveStage Stage
        {
            get { return m_Stage; }
            set
            {
                if (m_Stage != value)
                {
                    m_Stage = value;

                    if (m_Health > MaxHealth)
                        m_Health = MaxHealth;

                    InvalidateHive();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextGrowth
        {
            get { return m_NextGrowth; }
            set { m_NextGrowth = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HiveGrowthResult GrowthResult
        {
            get { return m_GrowthResult; }
            set { m_GrowthResult = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Population
        {
            get { return m_Population; }
            set
            {
                if (value < 0)
                    value = 0;
                else if (value > ApicultureSystem.MaxPopulation)
                    value = ApicultureSystem.MaxPopulation;

                if (m_Population != value)
                {
                    m_Population = value;

                    InvalidateHive();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Age
        {
            get { return m_Age; }
            set { m_Age = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Health
        {
            get { return m_Health; }
            set
            {
                if (value < 0)
                    value = 0;
                else if (value > MaxHealth)
                    value = MaxHealth;

                m_Health = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxHealth
        {
            get { return 10 + 2 * (int)m_Stage; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HiveHealthStatus HealthStatus
        {
            get
            {
                if (MaxHealth == 0)
                    return HiveHealthStatus.Dying;

                int perc = m_Health * 100 / MaxHealth;

                if (perc <= 33)
                    return HiveHealthStatus.Dying;
                else if (perc <= 66)
                    return HiveHealthStatus.Sickly;
                else if (perc <= 99)
                    return HiveHealthStatus.Healthy;
                else
                    return HiveHealthStatus.Thriving;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ParasiteLevel
        {
            get { return m_ParasiteLevel; }
            set
            {
                if (value < 0)
                    value = 0;
                else if (value > 2)
                    value = 2;

                m_ParasiteLevel = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DiseaseLevel
        {
            get { return m_DiseaseLevel; }
            set
            {
                if (value < 0)
                    value = 0;
                else if (value > 2)
                    value = 2;

                m_DiseaseLevel = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ResWater
        {
            get { return m_ResWater; }
            set { m_ResWater = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ResFlowers
        {
            get { return m_ResFlowers; }
            set { m_ResFlowers = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ResHives
        {
            get { return m_ResHives; }
            set { m_ResHives = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PotAgility
        {
            get { return m_PotAgility; }
            set { m_PotAgility = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PotCure
        {
            get { return m_PotCure; }
            set { m_PotCure = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PotHeal
        {
            get { return m_PotHeal; }
            set { m_PotHeal = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PotPoison
        {
            get { return m_PotPoison; }
            set { m_PotPoison = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PotStrength
        {
            get { return m_PotStrength; }
            set { m_PotStrength = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Wax
        {
            get { return m_Wax; }
            set
            {
                if (value < 0)
                    value = 0;
                else if (value > ApicultureSystem.MaxWax)
                    value = ApicultureSystem.MaxWax;

                m_Wax = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Honey
        {
            get { return m_Honey; }
            set
            {
                if (value < 0)
                    value = 0;
                else if (value > ApicultureSystem.MaxHoney)
                    value = ApicultureSystem.MaxHoney;

                m_Honey = value;
            }
        }

        public BeeColony()
        {
        }

        public void Init()
        {
            m_Stage = HiveStage.Colonizing;
            m_NextGrowth = DateTime.UtcNow + ApicultureSystem.GrowthDelay;
            m_Population = 1;
            m_Health = MaxHealth;
        }

        public void InvalidateHive()
        {
            if (m_Hive != null)
                m_Hive.InvalidateHive();
        }

        public void HiveMessage(Mobile from, string format, params object[] args)
        {
            if (m_Hive != null)
                m_Hive.HiveMessage(from, String.Format(format, args));
        }

        public void HiveMessage(Mobile from, int number, string args = "")
        {
            if (m_Hive != null)
                m_Hive.HiveMessage(from, number, args);
        }

        public HiveResourceStatus ScaleWater()
        {
            if (m_Stage == HiveStage.Empty || m_Population == 0)
                return HiveResourceStatus.None;

            // for every 50K bees, we need 2 water
            int perc = m_ResWater * 250 / m_Population;

            if (m_ResHives > 0)
                perc /= (m_ResHives + 1);

            return GetResourceStatus(perc);
        }

        public HiveResourceStatus ScaleFlowers()
        {
            if (m_Stage == HiveStage.Empty || m_Population == 0)
                return HiveResourceStatus.None;

            // for every 10K bees, we need 1 flower
            int perc = m_ResFlowers * 100 / m_Population;

            if (m_ResHives > 0)
                perc /= (m_ResHives + 1);

            return GetResourceStatus(perc);
        }

        private HiveResourceStatus GetResourceStatus(int perc)
        {
            if (perc <= 33)
                return HiveResourceStatus.VeryLow;
            else if (perc <= 66)
                return HiveResourceStatus.Low;
            else if (perc <= 100)
                return HiveResourceStatus.Normal;
            else if (perc <= 133)
                return HiveResourceStatus.High;
            else
                return HiveResourceStatus.VeryHigh;
        }

        public enum PotionResult
        {
            Success,
            Full,
            TooWeak,
            NoEffect,
        }

        public void Pour(Mobile from, Item item)
        {
            if (item is BasePotion)
            {
                BasePotion potion = (BasePotion)item;

                if (CheckApplyPotion(from, potion.PotionEffect))
                {
                    potion.Consume();

                    from.PlaySound(0x240);
                    from.AddToBackpack(new Bottle());
                }
            }
            else if (item is PotionKeg)
            {
                PotionKeg keg = (PotionKeg)item;

                if (keg.Held <= 0)
                {
                    HiveMessage(from, "The potion keg is empty.");
                }
                else if (CheckApplyPotion(from, keg.Type))
                {
                    keg.Held--;

                    from.PlaySound(0x240);
                }
            }
            else
            {
                HiveMessage(from, "You cannot use that on a beehive!");
            }
        }

        public bool CheckApplyPotion(Mobile from, PotionEffect effect)
        {
            PotionResult result = ApplyPotion(effect);

            SendPotionResult(from, result);

            return (result == PotionResult.Success);
        }

        public PotionResult ApplyPotion(PotionEffect effect)
        {
            if (effect == PotionEffect.PoisonGreater || effect == PotionEffect.PoisonDeadly)
            {
                if (m_PotPoison >= 2)
                    return PotionResult.Full;

                PotPoison++;

                return PotionResult.Success;
            }
            else if (effect == PotionEffect.CureGreater)
            {
                if (m_PotCure >= 2)
                    return PotionResult.Full;

                PotCure++;

                return PotionResult.Success;
            }
            else if (effect == PotionEffect.HealGreater)
            {
                if (m_PotHeal >= 2)
                    return PotionResult.Full;

                PotHeal++;

                return PotionResult.Success;
            }
            else if (effect == PotionEffect.StrengthGreater)
            {
                if (m_PotStrength >= 2)
                    return PotionResult.Full;

                PotStrength++;

                return PotionResult.Success;
            }
            else if (effect == PotionEffect.AgilityGreater)
            {
                if (m_PotAgility >= 2)
                    return PotionResult.Full;

                PotAgility++;

                return PotionResult.Success;
            }
            else
            {
                switch (effect)
                {
                    case PotionEffect.PoisonLesser:
                    case PotionEffect.Poison:
                    case PotionEffect.CureLesser:
                    case PotionEffect.Cure:
                    case PotionEffect.HealLesser:
                    case PotionEffect.Heal:
                    case PotionEffect.Strength:
                        return PotionResult.TooWeak;
                    default:
                        return PotionResult.NoEffect;
                }
            }
        }

        public void SendPotionResult(Mobile from, PotionResult result)
        {
            switch (result)
            {
                case PotionResult.Success:
                    HiveMessage(from, "You pour the potion into the beehive.");
                    break;
                case PotionResult.Full:
                    HiveMessage(from, "The beehive is already soaked with this type of potion!");
                    break;
                case PotionResult.TooWeak:
                    HiveMessage(from, "This potion is not powerful enough to use on a beehive!");
                    break;
                case PotionResult.NoEffect:
                    HiveMessage(from, "This type of potion has no effect on a beehive.");
                    break;
            }
        }

        public void DoGrowth()
        {
            if (m_Stage == HiveStage.Empty)
                return;

            Age++;

            FindResources();

            HiveResourceStatus water = ScaleWater();
            HiveResourceStatus flowers = ScaleFlowers();

            int consumePoison = Math.Max(0, Math.Min(m_PotPoison, m_ParasiteLevel));

            if (consumePoison > 0)
            {
                ParasiteLevel -= consumePoison;
                PotPoison -= consumePoison;
            }

            int consumeCure = Math.Max(0, Math.Min(m_PotCure, m_DiseaseLevel));

            if (consumeCure > 0)
            {
                DiseaseLevel -= consumeCure;
                PotCure -= consumeCure;
            }

            // cancel out remaining poison potions with cure potions
            if (m_PotPoison > 0 && m_PotCure > 0)
                PotPoison -= m_PotCure;

            if (m_ParasiteLevel <= 0 && m_DiseaseLevel <= 0)
                Health += 2 + 3 * m_PotHeal;

            int damage = 0;

            if (m_ParasiteLevel > 0)
                damage += m_ParasiteLevel * Utility.RandomMinMax(2, 4);

            if (m_DiseaseLevel > 0)
                damage += m_DiseaseLevel * Utility.RandomMinMax(2, 4);

            if (m_PotPoison > 0)
                damage += m_PotPoison * Utility.RandomMinMax(2, 4);

            if (water < HiveResourceStatus.Low)
                damage += (2 - (int)water) * Utility.RandomMinMax(2, 4);

            if (flowers < HiveResourceStatus.Low)
                damage += (2 - (int)flowers) * Utility.RandomMinMax(2, 4);

            Health -= damage;

            if (m_Health <= 0)
            {
                if (m_Stage >= HiveStage.Producing)
                    Population--;
                else
                    Population = 0;

                if (m_Population <= 0)
                    Stage = HiveStage.Empty;

                GrowthResult = HiveGrowthResult.PopulationDown;
            }
            else if (HealthStatus < HiveHealthStatus.Healthy)
            {
                GrowthResult = HiveGrowthResult.NotHealthy;
            }
            else if (water < HiveResourceStatus.Low || flowers < HiveResourceStatus.Low)
            {
                GrowthResult = HiveGrowthResult.LowResources;
            }
            else if (m_Stage < HiveStage.Producing)
            {
                Stage++;

                GrowthResult = HiveGrowthResult.Grown;
            }
            else if (m_Population < ApicultureSystem.MaxPopulation && water >= HiveResourceStatus.Normal && flowers >= HiveResourceStatus.Normal)
            {
                Population++;

                GrowthResult = HiveGrowthResult.PopulationUp;
            }
            else
            {
                GrowthResult = HiveGrowthResult.Grown;
            }

            if (m_Stage >= HiveStage.Producing)
            {
                if (m_Wax < ApicultureSystem.MaxWax)
                {
                    int produce = 1;

                    if (HealthStatus == HiveHealthStatus.Thriving)
                        produce++;

                    produce += m_PotAgility;
                    produce *= m_Population;

                    produce /= 3; // wax produces slower than honey

                    if (produce < 1)
                        produce = 1;

                    Wax += produce;
                }

                if (m_Honey < ApicultureSystem.MaxHoney)
                {
                    int produce = 1;

                    if (HealthStatus == HiveHealthStatus.Thriving)
                        produce++;

                    produce += m_PotAgility;
                    produce *= m_Population;

                    Honey += produce;
                }
            }

            if (m_Stage != HiveStage.Empty)
            {
                double parasiteChance = 0.30;

                parasiteChance += 0.010 * Math.Min(20, m_Age);
                parasiteChance += 0.100 * ((int)water - 3);
                parasiteChance -= 0.075 * m_PotStrength;

                if (Utility.RandomDouble() < parasiteChance)
                    m_ParasiteLevel++;

                double diseaseChance = 0.30;

                diseaseChance += 0.010 * Math.Min(20, m_Age);
                diseaseChance += 0.100 * ((int)flowers - 3);
                diseaseChance -= 0.075 * m_PotStrength;

                if (Utility.RandomDouble() < diseaseChance)
                    m_DiseaseLevel++;
            }

            PotAgility = 0;
            PotCure = 0;
            PotHeal = 0;
            PotPoison = 0;
            PotStrength = 0;

            InvalidateHive();
        }

        public void FindResources()
        {
            ResWater = 0;
            ResFlowers = 0;
            ResHives = 0;

            if (m_Hive == null)
                return;

            Map map = m_Hive.Map;

            if (map == null)
                return;

            int workRange = 2 + m_Population + m_PotAgility;

            List<Item> itemsInRange = new List<Item>();

            foreach (Item item in map.GetItemsInRange(m_Hive.Location, workRange))
            {
                if (item == m_Hive)
                    continue;

                itemsInRange.Add(item);

                if (item is AddonComponent)
                {
                    AddonComponent ac = (AddonComponent)item;

                    if (ac.Addon != null && !itemsInRange.Contains(ac.Addon))
                        itemsInRange.Add(ac.Addon);
                }
            }

            foreach (Item item in itemsInRange)
            {
                if (item is BaseBeverage)
                {
                    BaseBeverage bev = (BaseBeverage)item;

                    if (bev.Content == BeverageType.Water && bev.Quantity > 0)
                        ResWater++;
                }
                else if (item is IWaterSource)
                {
                    IWaterSource src = (IWaterSource)item;

                    // water sources/barrels provide more water
                    if (src.Quantity > 0)
                        ResWater += 3;
                }
                else if (item is BaseWaterContainer)
                {
                    BaseWaterContainer bwc = (BaseWaterContainer)item;

                    // water sources/barrels provide more water
                    if (bwc.Quantity > 0)
                        ResWater += 3;
                }
                else if (item is PlantItem)
                {
                    PlantItem plant = (PlantItem)item;

                    if (plant.PlantStatus >= PlantStatus.FullGrownPlant && plant.PlantStatus <= PlantStatus.DecorativePlant && Array.IndexOf(m_FlowerTypes, plant.PlantType) != -1)
                        ResFlowers++;
                }
                else if (ApicultureSystem.Competition && Utility.InRange(m_Hive.Location, item.Location, ApicultureSystem.CompetitionRange) && item is Beehive)
                {
                    Beehive hive = (Beehive)item;

                    if (hive.Colony.Stage != HiveStage.Empty)
                        ResHives++;
                }
            }
        }

        private static readonly PlantType[] m_FlowerTypes = new PlantType[]
            {
                PlantType.CampionFlowers,
                PlantType.Poppies,
                PlantType.Snowdrops,
                PlantType.Lilies
            };

        [Flags]
        private enum SaveFlag : uint
        {
            None = 0x0,

            Stage = 0x00000001,
            NextGrowth = 0x00000002,
            GrowthResult = 0x00000004,
            Population = 0x00000008,
            Age = 0x00000010,
            Health = 0x00000020,
            ParasiteLevel = 0x00000040,
            DiseaseLevel = 0x00000080,
            ResWater = 0x00000100,
            ResFlowers = 0x00000200,
            ResHives = 0x00000400,
            PotAgility = 0x00000800,
            PotCure = 0x00001000,
            PotHeal = 0x00002000,
            PotPoison = 0x00004000,
            PotStrength = 0x00008000,
            Wax = 0x00010000,
            Honey = 0x00020000,
        }

        private static void SetSaveFlag(ref SaveFlag flags, SaveFlag flag, bool condition)
        {
            if (condition)
                flags |= flag;
        }

        private static bool GetSaveFlag(SaveFlag flags, SaveFlag flag)
        {
            return (flags & flag) != 0;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version

            SaveFlag flags = SaveFlag.None;

            SetSaveFlag(ref flags, SaveFlag.Stage, m_Stage != 0);
            SetSaveFlag(ref flags, SaveFlag.NextGrowth, m_NextGrowth < DateTime.UtcNow);
            SetSaveFlag(ref flags, SaveFlag.GrowthResult, m_GrowthResult != 0);
            SetSaveFlag(ref flags, SaveFlag.Population, m_Population != 0);
            SetSaveFlag(ref flags, SaveFlag.Age, m_Age != 0);
            SetSaveFlag(ref flags, SaveFlag.Health, m_Health != 0);
            SetSaveFlag(ref flags, SaveFlag.ParasiteLevel, m_ParasiteLevel != 0);
            SetSaveFlag(ref flags, SaveFlag.DiseaseLevel, m_DiseaseLevel != 0);
            SetSaveFlag(ref flags, SaveFlag.ResWater, m_ResWater != 0);
            SetSaveFlag(ref flags, SaveFlag.ResFlowers, m_ResFlowers != 0);
            SetSaveFlag(ref flags, SaveFlag.ResHives, m_ResHives != 0);
            SetSaveFlag(ref flags, SaveFlag.PotAgility, m_PotAgility != 0);
            SetSaveFlag(ref flags, SaveFlag.PotCure, m_PotCure != 0);
            SetSaveFlag(ref flags, SaveFlag.PotHeal, m_PotHeal != 0);
            SetSaveFlag(ref flags, SaveFlag.PotPoison, m_PotPoison != 0);
            SetSaveFlag(ref flags, SaveFlag.PotStrength, m_PotStrength != 0);
            SetSaveFlag(ref flags, SaveFlag.Wax, m_Wax != 0);
            SetSaveFlag(ref flags, SaveFlag.Honey, m_Honey != 0);

            writer.Write((uint)flags);

            if (GetSaveFlag(flags, SaveFlag.Stage))
                writer.Write((byte)m_Stage);

            if (GetSaveFlag(flags, SaveFlag.NextGrowth))
                writer.WriteDeltaTime(m_NextGrowth);

            if (GetSaveFlag(flags, SaveFlag.GrowthResult))
                writer.Write((byte)m_GrowthResult);

            if (GetSaveFlag(flags, SaveFlag.Population))
                writer.WriteEncodedInt(m_Population);

            if (GetSaveFlag(flags, SaveFlag.Age))
                writer.WriteEncodedInt(m_Age);

            if (GetSaveFlag(flags, SaveFlag.Health))
                writer.WriteEncodedInt(m_Health);

            if (GetSaveFlag(flags, SaveFlag.ParasiteLevel))
                writer.WriteEncodedInt(m_ParasiteLevel);

            if (GetSaveFlag(flags, SaveFlag.DiseaseLevel))
                writer.WriteEncodedInt(m_DiseaseLevel);

            if (GetSaveFlag(flags, SaveFlag.ResWater))
                writer.WriteEncodedInt(m_ResWater);

            if (GetSaveFlag(flags, SaveFlag.ResFlowers))
                writer.WriteEncodedInt(m_ResFlowers);

            if (GetSaveFlag(flags, SaveFlag.ResHives))
                writer.WriteEncodedInt(m_ResHives);

            if (GetSaveFlag(flags, SaveFlag.PotAgility))
                writer.WriteEncodedInt(m_PotAgility);

            if (GetSaveFlag(flags, SaveFlag.PotCure))
                writer.WriteEncodedInt(m_PotCure);

            if (GetSaveFlag(flags, SaveFlag.PotHeal))
                writer.WriteEncodedInt(m_PotHeal);

            if (GetSaveFlag(flags, SaveFlag.PotPoison))
                writer.WriteEncodedInt(m_PotPoison);

            if (GetSaveFlag(flags, SaveFlag.PotStrength))
                writer.WriteEncodedInt(m_PotStrength);

            if (GetSaveFlag(flags, SaveFlag.Wax))
                writer.WriteEncodedInt(m_Wax);

            if (GetSaveFlag(flags, SaveFlag.Honey))
                writer.WriteEncodedInt(m_Honey);
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        SaveFlag flags = (SaveFlag)reader.ReadUInt();

                        if (GetSaveFlag(flags, SaveFlag.Stage))
                            m_Stage = (HiveStage)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.NextGrowth))
                            m_NextGrowth = reader.ReadDeltaTime();

                        if (GetSaveFlag(flags, SaveFlag.GrowthResult))
                            m_GrowthResult = (HiveGrowthResult)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.Population))
                            m_Population = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.Age))
                            m_Age = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.Health))
                            m_Health = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.ParasiteLevel))
                            m_ParasiteLevel = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.DiseaseLevel))
                            m_DiseaseLevel = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.ResWater))
                            m_ResWater = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.ResFlowers))
                            m_ResFlowers = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.ResHives))
                            m_ResHives = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.PotAgility))
                            m_PotAgility = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.PotCure))
                            m_PotCure = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.PotHeal))
                            m_PotHeal = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.PotPoison))
                            m_PotPoison = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.PotStrength))
                            m_PotStrength = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.Wax))
                            m_Wax = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.Honey))
                            m_Honey = reader.ReadEncodedInt();

                        break;
                    }
            }
        }

        public override string ToString()
        {
            return "...";
        }
    }
}