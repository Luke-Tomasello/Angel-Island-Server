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

/* Scripts/Mobiles/Monsters/Reptile/Magic/WhiteWyrm.cs
 * ChangeLog
 *  2/8/22, Yoar
 *      Special white wyrm hues are now retained during OnGrowth.
 *  1/6/2021, Yoar
 *      Added genes + enabled breeding.
 *      Added frost breath ability. For egg-born WWs only.
 *	4/10/10, adam
 *		Add speed management MCi to tune dragon speeds.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	12/21/05, Adam
 *		10% at a pure White Wyrm
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *	7/2/04
 *		Change chance to drop a magic item to 30% 
 *		add a 5% chance for a bonus drop at next intensity level
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

/*
 * Hunting with white wyrms
 * Wyrms have the ability to cast mass curse, and getting hit by this spell will trigger you to auto-defend. 
 * This can not only cause you to go gray (which can be a problem when fighting in towns or hunting in Felucca), 
 * but also might get you a murder count if an innocent dies in the process. Standing behind them will usually keep you from being hit. 
 * If you use Veterinary to heal your pet, it is wise to keep an eye on your wyrm's mana level, and if possible apply bandages after a battle. 
 * https://web.archive.org/web/20010610210738/http://uo.stratics.com/strat/tamer.shtml 
 * ---
 * 8/2/2023, Adam. RunUO beta .36 (2002) did not have it.
 * I assume it was a design flaw and subsequently removed.
 * We'll not include it
 */

using Server.Engines.Breeding;
using Server.Items;
using System;

namespace Server.Mobiles
{
    [CorpseName("a white wyrm corpse")]
    public class WhiteWyrm : BaseCreature
    {
        private static double WyrmActiveSpeed { get { return Server.Items.Consoles.DragonSpeedMCi.WyrmActiveSpeed; } }
        private static double WyrmPassiveSpeed { get { return Server.Items.Consoles.DragonSpeedMCi.WyrmPassiveSpeed; } }

        private bool m_HasFrostBreath; // only bred WWs have frost breath

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasFrostBreath
        {
            get { return m_HasFrostBreath; }
            set { m_HasFrostBreath = value; }
        }

        [Constructable]
        public WhiteWyrm()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, WyrmActiveSpeed, WyrmPassiveSpeed)
        {
            // Adam: 10% at a pure White Wyrm
            if (Utility.Chance(0.10))
            {
                Body = 180;
            }
            else
                Body = 49;

            Name = "a white wyrm";
            BaseSoundID = 362;

            SetStr(675 + StrMax / 100, 754 + StrMax / 100);
            SetInt(370 + IntMax / 100, 434 + IntMax / 100);
            SetDex(73 + DexMax / 100, 96 + DexMax / 100);

            SetSkill(SkillName.EvalInt, 99.1, 100.0);
            SetSkill(SkillName.Magery, 99.1, 100.0);
            SetSkill(SkillName.MagicResist, 99.1, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 18000;
            Karma = -18000;

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 96.3;
        }
        public override Characteristics MyCharacteristics { get { return Characteristics.Run | Characteristics.Fly; } }
        public override bool HasBreath { get { return m_HasFrostBreath; } }
        public override int BreathEffectHue { get { return 1150; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 4 : 4; } }
        public override HideType HideType { get { return HideType.Barbed; } }
        public override int Scales { get { return Utility.RandomBool() ? 7 : 10; } }
        public override ScaleType ScaleType { get { return ScaleType.White; } }
        public override FoodType FavoriteFood { get { return FoodType.Meat | FoodType.Gold; } }

        #region Genes

        private double m_StatCapFactor = 1.0;

        [Gene("Versatility", 1.975, 2.025, 1.950, 2.050, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public double StatCapFactor
        {
            get
            {
                return m_StatCapFactor;
            }
            set
            {
                m_StatCapFactor = value;
            }
        }

        private int m_StrMax, m_IntMax, m_DexMax;

        // 721, 760
        [Gene("StrMax", 1081, 1140, 901, 1330)] // breeding range: [-15%, +15%]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int StrMax
        {
            get
            {
                return m_StrMax;
            }
            set
            {
                m_StrMax = value;
            }
        }

        // 386, 425
        [Gene("IntMax", 581, 635, 481, 745)] // breeding range: [-15%, +15%]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int IntMax
        {
            get
            {
                return m_IntMax;
            }
            set
            {
                m_IntMax = value;
            }
        }

        // 101, 130
        [Gene("DexMax", 151, 195, 126, 225)] // breeding range: [-15%, +15%]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int DexMax
        {
            get
            {
                return m_DexMax;
            }
            set
            {
                m_DexMax = value;
            }
        }

        public override int StatCap { get { return (int)(m_StatCapFactor * (m_StrMax + m_IntMax + m_DexMax) / 3.0); } }

        private double m_HitsMaxFactor = 1.0;

        [Gene("Physique", 0.85, 1.00, 0.85, 1.15, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual double HitsMaxFactor
        {
            get
            {
                return m_HitsMaxFactor;
            }
            set
            {
                double hitsFrac = (double)this.Hits / this.HitsMax;

                m_HitsMaxFactor = value;

                this.Hits = Math.Max(0, Math.Min(this.HitsMax, (int)(hitsFrac * this.HitsMax)));
            }
        }

        // 433, 456
        public override int HitsMax
        {
            get { return Math.Max(1, (int)Math.Round(this.HitsMaxFactor * this.Str)); }
        }

        private double m_ClawAccuracy;

        [Gene("Claw Accuracy", 0.85, 1.00, 0.85, 1.15, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public double ClawAccuracy
        {
            get
            {
                return m_ClawAccuracy;
            }
            set
            {
                m_ClawAccuracy = value;
            }
        }

        public override double GetAccuracyScalar()
        {
            return m_ClawAccuracy;
        }

        // 17, 25
        [Gene("Claw Size", 14, 16, 10, 20, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int DamageMin
        {
            get
            {
                return base.DamageMin;
            }
            set
            {
                base.DamageMin = Math.Max(0, value);
                base.DamageMax = (3 + 4 * value) / 3; // = 1 + 1.333*min
            }
        }

        // 64
        [Gene("Scales", 58, 62, 50, 70, GeneVisibility.Wild)]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int VirtualArmor
        {
            get
            {
                return base.VirtualArmor;
            }
            set
            {
                base.VirtualArmor = value;
            }
        }

        private double m_BreathScaler;

        [Gene("Frost Breath", 0.85, 1.00, 0.85, 1.15, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public double BreathDamage
        {
            get
            {
                return m_BreathScaler;
            }
            set
            {
                m_BreathScaler = value;
            }
        }

        public override double BreathDamageScalar { get { return m_BreathScaler * base.BreathDamageScalar; } }

        private int m_Meat, m_Hides;

        [Gene("Meat", 18, 22, 12, 28, GeneVisibility.Wild)]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Meat
        {
            get
            {
                return m_Meat;
            }
            set
            {
                m_Meat = value;
            }
        }

        [Gene("Hides", 18, 22, 12, 28, GeneVisibility.Wild)]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Hides
        {
            get
            {
                return m_Hides;
            }
            set
            {
                m_Hides = value;
            }
        }

        private double m_RareHueFactor;

        [Gene("Rare Hue", 0.00, 0.00, 0.00, 1.00, 0.00, 1.00, GeneVisibility.Invisible, 0.24)] // <2% chance to spawn with a rare hue
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public double RareHueFactor
        {
            get
            {
                return m_RareHueFactor;
            }
            set
            {
                m_RareHueFactor = value;
                UpdateBody();
            }
        }

        public override string DescribeGene(System.Reflection.PropertyInfo prop, GeneAttribute attr)
        {
            if (attr != null)
            {
                switch (attr.Name)
                {
                    case "Versatility":
                        {
                            if (StatCapFactor < 1.97)
                                return "Limited";
                            else if (StatCapFactor < 1.99)
                                return "Reserved";
                            else if (StatCapFactor < 2.01)
                                return "Able";
                            else if (StatCapFactor < 2.03)
                                return "Versatile";
                            else
                                return "Dynamic";
                        }
                    case "Physique":
                        {
                            if (HitsMaxFactor < .90)
                                return "Frail";
                            else if (HitsMaxFactor < .95)
                                return "Spindly";
                            else if (HitsMaxFactor < 1.00)
                                return "Slight";
                            else if (HitsMaxFactor < 1.05)
                                return "Lithe";
                            else if (HitsMaxFactor < 1.10)
                                return "Sturdy";
                            else
                                return "Tough";
                        }
                    case "Claw Accuracy": // 0.85-1.15
                        {
                            if (ClawAccuracy < 0.90)
                                return "Unwieldy";
                            else if (ClawAccuracy < 1.00)
                                return "Able";
                            else if (ClawAccuracy < 1.10)
                                return "Deft";
                            else
                                return "Precise";
                        }
                    case "Claw Size": // 10-20
                        {
                            if (DamageMin < 12)
                                return "Undeveloped";
                            else if (DamageMin < 14)
                                return "Small";
                            else if (DamageMin < 16)
                                return "Ample";
                            else if (DamageMin < 18)
                                return "Large";
                            else
                                return "Frightening";
                        }
                    case "Scales":
                        {
                            if (VirtualArmor < 54)
                                return "Flimsy";
                            else if (VirtualArmor < 58)
                                return "Durable";
                            else if (VirtualArmor < 62)
                                return "Rugged";
                            else if (VirtualArmor < 66)
                                return "Hard";
                            else
                                return "Plated";
                        }
                    case "Frost Breath":
                        {
                            if (BreathDamage < 0.90)
                                return "Chilly";
                            else if (BreathDamage < 1.00)
                                return "Cold";
                            else if (BreathDamage < 1.10)
                                return "Freezing";
                            else
                                return "Arctic";
                        }
                    case "Meat":
                        {
                            if (Meat < 16)
                                return "Frail";
                            else if (Meat < 20)
                                return "Lean";
                            else if (Meat < 24)
                                return "Brawny";
                            else
                                return "Colossal";
                        }
                    case "Hides":
                        {
                            if (Hides < 15)
                                return "Delicate";
                            else if (Hides < 18)
                                return "Supple";
                            else if (Hides < 22)
                                return "Thick";
                            else if (Hides < 25)
                                return "Heavy";
                            else
                                return "Mountainous";
                        }
                    default:
                        return base.DescribeGene(prop, attr);
                }
            }

            return "Error";
        }

        #endregion

        #region Breeding

        public override bool Ageless { get { return false; } }
        public override Type EggType { get { return typeof(WhiteWyrmEgg); } }
        public override bool EatsBP { get { return true; } }
        public override bool EatsSA { get { return true; } }
        public override bool EatsKukuiNuts { get { return true; } }

        public override void OnHatch()
        {
            this.SetSkill(SkillName.EvalInt, 10.1, 20.0);
            this.SetSkill(SkillName.Magery, 15.1, 25.0);
            this.SetSkill(SkillName.MagicResist, 25.1, 40.0);
            this.SetSkill(SkillName.Tactics, 20.1, 30.0);
            this.SetSkill(SkillName.Wrestling, 25.1, 40.0);

            this.PlaySound(Utility.RandomList(111, 113, 114, 115)); // chicken noises

            m_HasFrostBreath = true;
        }

        public override void OnGrowth(Maturity oldMaturity)
        {
            UpdateBody();

            if (this.Maturity == Maturity.Infant)
                BreedingSystem.ScaleStats(this, 0.4);
            else if (this.Maturity == Maturity.Ancient)
                BreedingSystem.ScaleStats(this, 0.6);
        }

        private void UpdateBody()
        {
            bool pure = (m_RareHueFactor >= 0.88);

            switch (this.Maturity)
            {
                case Maturity.Infant:
                    {
                        this.Body = 0xCE; // lava lizard
                        if (IsRegularHue(this.Hue))
                            this.Hue = pure ? 1153 : 1150;
                        break;
                    }
                case Maturity.Child:
                    {
                        this.Body = 0x31F; // swamp dragon (armored)
                        if (IsRegularHue(this.Hue))
                            this.Hue = pure ? 1153 : 1150;
                        break;
                    }
                case Maturity.Youth:
                    {
                        this.Body = 0x3D; // drake
                        if (IsRegularHue(this.Hue))
                            this.Hue = pure ? 1153 : 1150;
                        break;
                    }
                default:
                case Maturity.Adult:
                    {
                        if (pure)
                        {
                            this.Body = 0x3B; // dragon
                            if (IsRegularHue(this.Hue))
                                this.Hue = 1153;
                        }
                        else
                        {
                            this.Body = 0x31; // white wyrm
                            if (IsRegularHue(this.Hue))
                                this.Hue = 0;
                        }

                        break;
                    }
                case Maturity.Ancient:
                    {
                        this.Body = 0x2E; // ancient wyrm
                        if (IsRegularHue(this.Hue))
                            this.Hue = pure ? 1153 : 1150;
                        break;
                    }
            }
        }

        private static bool IsRegularHue(int hue)
        {
            return (hue == 0 || hue == 1153 || hue == 1150);
        }

        public override bool CanBreed()
        {
            return base.CanBreed() && (Core.UOTC_CFG || this.Region.Name == "Ice");
        }

        private static readonly int[] m_MoanSounds = new int[]
            {
                362, 364, 365, 705, 711, 712, 714, 715, 718
            };

        public override void Moan()
        {
            this.PlaySound(m_MoanSounds[Utility.Random(m_MoanSounds.Length)]);
        }

        #endregion

        public WhiteWyrm(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                int gems = Utility.RandomMinMax(1, 5);

                for (int i = 0; i < gems; ++i)
                    PackGem();

                PackGold(800, 900);
                PackMagicEquipment(1, 3, 0.50, 0.50);
                PackMagicEquipment(1, 3, 0.15, 0.15);

                // Category 4 MID
                PackMagicItem(2, 3, 0.10);
                PackMagicItem(2, 3, 0.05);
                PackMagicItem(2, 3, 0.02);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020607002613/uo.stratics.com/hunters/whitewyrm.shtml
                    // 1800 to 2100 Gold, Gems, Level 4 Treasure Map, Magic Weapons and Armor, 7 or 10 White Scales, 19 Raw Meat, 20 Hides
                    if (Spawning)
                    {
                        PackGold(1800, 2100);
                    }
                    else
                    {
                        PackGem(Utility.Random(1, 5));
                        PackMagicEquipment(2, 3);
                    }
                }
                else
                {   // Standard RunUO
                    AddLoot(LootPack.FilthyRich, 2);
                    AddLoot(LootPack.Average);
                    AddLoot(LootPack.Gems, Utility.Random(1, 5));
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((bool)m_HasFrostBreath);

            writer.Write((double)m_StatCapFactor);
            writer.Write((int)m_IntMax);
            writer.Write((int)m_DexMax);
            writer.Write((int)m_StrMax);
            writer.Write((double)m_HitsMaxFactor);
            writer.Write((double)m_ClawAccuracy);
            writer.Write((double)m_BreathScaler);
            writer.Write((int)m_Meat);
            writer.Write((int)m_Hides);
            writer.Write((double)m_RareHueFactor);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_HasFrostBreath = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        m_StatCapFactor = reader.ReadDouble();
                        m_IntMax = reader.ReadInt();
                        m_DexMax = reader.ReadInt();
                        m_StrMax = reader.ReadInt();
                        m_HitsMaxFactor = reader.ReadDouble();
                        m_ClawAccuracy = reader.ReadDouble();
                        m_BreathScaler = reader.ReadDouble();
                        m_Hides = reader.ReadInt();
                        m_Meat = reader.ReadInt();
                        m_RareHueFactor = reader.ReadDouble();
                        break;
                    }
            }

            if (version < 1)
            {
                Genetics.InitGenes(this,
                    "Versatility", "IntMax", "DexMax", "StrMax", "Physique",
                    "Claw Accuracy", "Claw Size", "Scales", "Breath Damage",
                    "Meat", "Hides");

                if (this.Hue == 1153)
                    m_RareHueFactor = 0.94;
                else
                    m_RareHueFactor = 0.50;
            }
        }
    }

    public class WhiteWyrmEgg : BaseHatchableEgg
    {
        public override string BeakMessage { get { return "You can see a claw!"; } }

        [Constructable]
        public WhiteWyrmEgg()
            : this(new WhiteWyrm())
        {
        }

        public WhiteWyrmEgg(BaseCreature chick)
            : base(chick)
        {
            ItemID = 0x1363;
            Name = "a white wyrm egg";
            Hue = 1150;
        }

        public override Food Cook()
        {
            return new CookedDragonEgg();
        }

        public WhiteWyrmEgg(Serial serial)
            : base(serial)
        {
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