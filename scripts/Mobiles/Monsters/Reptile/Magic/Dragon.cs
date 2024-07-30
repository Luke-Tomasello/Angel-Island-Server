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

/* Scripts/Mobiles/Monsters/Reptile/Magic/Dragon.cs
 * ChangeLog
 *  2/8/22, Yoar
 *      Special dragon hues are now retained during OnGrowth.
 *  1/6/22, Yoar
 *      Renamed "m_BreathDamage" field to "m_BreathScaler".
 *      Renamed "Breath Damage" gene to "Fire Breath".
 *  12/26/21, Yoar
 *      Fixed breedable pets' stat caps. Problem: Breedable pets are unable to gain stats.
 *      Cause: StatCap defaults to a value of 225. Solution: StatCap now returns the total
 *      of StrMax, DexMax, IntMax, scaled by the versatility gene.
 *  12/16/21, Yoar
 *      Cooked dragon egg is no longer stackable.
 *  10/23/21, Yoar
 *      Don't screw players; no random init. of genes during version update.
 *  10/22/21, Yoar
 *      Removed the "Rare Hue" gene.
 *  10/22/21, Yoar
 *      Added "Breath Damage", "Rare Hue" genes.
 *  10/22/21, Yoar
 *      Readded "Claw Accuracy" gene. Now gives a hit chance bonus.
 *  10/21/21, Yoar
 *      - Cleanups!
 *      - Now properly initializing m_StatCapFactor, m_HitsMaxFactor to 1.0.
 *      - Reduced spawn range of DamageMin by 1.
 *  10/20/21, Yoar: Breeding System overhaul
 *      - Abstracted the breeding system.
 *      - The behavior from DragonAI is now coded in the general-purpose MatingRitual object. Removed DragonAI.
 *      - DragonEgg now derives from BaseHatchableEgg.
 *      - Reorganized remaining genes/breeding code into two #regions.
 *      - Moved BP/SA eating logic to BreedingSystem.cs so that it may work on a variety of creatures.
 *      - Moved kukui nut eating logic to KukuiNut.cs so that it may work on a variety of creatures.
 *      - Changed "Physique" to being a percentage bonus.
 *      - Merged "Claw Accuracy" into "Claw Size".
 *	5/12/10, adam
 *		update AttackOrderHack to account for ScaredOfScaryThings
 *	4/10/10, adam
 *		Add speed management MCi to tune dragon speeds.
 *	9/1/07, Adam
 *		move helper code to the end of the file
 *		remove the 'force to dragonai' from deserialize.
 *			(because we should be able to dynamically change it. See Paragons.)
 *	7/3/07, Adam
 *		- remove OnControlMasterChanged() as it was continually resetting the hatch date everytime someone put
 *			their pet in the stables or took them out.
 *		- add a BreedingParticipant flag
 *		- deprecate using m_hatchDate as a flag, prefer BreedingParticipant flag.
 *		-Add BreedingParticipant(ion) checks for both males and females when the mood turns to procreation
 *  07/01/07 Taran Kain
 *      Fixed phantom stabled-deleted log warning.
 *  06/25/07 Taran Kain
 *      Added TC timers to play nice with prod values.
 *  06/24/07 Taran Kain
 *      Removed TC time/chance values.
 *      Fixed chicken sounds, changed bodies to depend on gender
 *  6/19/07, Adam
 *      - Added a TestCenter override for the 'in fire dungeon' check.
 *      on TC drags can breed anywhere (don't want testers getting PKed)
 *      - merged versions 3&4 as there was a versioning error.
 *	06/18/07 Taran Kain
 *		BREEDING!!!
 *	03/28/07 Taran Kain
 *		Added names to genes
 *  1/08/07 Taran Kain
 *      Updated old dragons to new #'s, add in some safety measures
 *  1/08/07 Taran Kain
 *      Changed *Max values
 *      Removed RawStr, RawInt, RawDex from genetics, now set (semi-)normally
 *      Fixed tiny bug with HitsMaxDiff not updating Hits correctly
 *      Added in dragon-specific SkillCheck logic for custom stat cap
 *  11/28/06 Taran Kain
 *      Changed HitsMaxDiff to be more in-line with previous values.
 *	11/20/06 Taran Kain
 *		Added genetics, allowed breeding logic, overrode several properties.
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *	7/2/04
 *		Change chance to drop a magic item to 30% 
 *		add a 5% chance for a bonus drop at next intensity level
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Breeding;
using Server.Items;
using System;

namespace Server.Mobiles
{
    [CorpseName("a dragon corpse")]
    public class Dragon : BaseCreature
    {
        [Constructable]
        public Dragon()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a dragon";
            Body = Female ? 0xC : 0x3B;
            BaseSoundID = 362;

            SetStr(675 + StrMax / 100, 754 + StrMax / 100);
            SetInt(370 + IntMax / 100, 434 + IntMax / 100);
            SetDex(73 + DexMax / 100, 96 + DexMax / 100);

            SetSkill(SkillName.EvalInt, 30.1, 40.0);
            SetSkill(SkillName.Magery, 30.1, 40.0);
            SetSkill(SkillName.MagicResist, 99.1, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 92.5);

            Fame = 15000;
            Karma = -15000;

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 93.9;
        }

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

        // 796, 825
        [Gene("StrMax", 1145, 1295, 995, 1445)]
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

        // 436, 475
        [Gene("IntMax", 640, 736, 545, 831)]
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

        // 86, 105
        [Gene("DexMax", 133, 159, 108, 184)]
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

        // Yoar: Changed "Physique" to being percentage bonus (the way it's done for Chicken)
#if old
        private int m_HitsMaxDiff;

        [Gene("Physique", -10, 10, -15, 15, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int HitsMaxDiff
        {
            get
            {
                return m_HitsMaxDiff;
            }
            set
            {
                m_HitsMaxDiff = value;

                if (Hits > HitsMax)
                    Hits = HitsMax;

                Delta(MobileDelta.Hits);
            }
        }

        public override int HitsMax
        {
            get { return base.Str - HitsMaxDiff; } // Yoar: This should be "+ HitsMaxDiff"!
        }
#else
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

        public override int HitsMax
        {
            get { return Math.Max(1, (int)Math.Round(this.HitsMaxFactor * this.Str)); }
        }
#endif

        // Yoar: Changed the effects of "Claw Accuracy" and "Claw Size"
#if old
        [Gene("Claw Accuracy", 15, 17, 12, 20, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int DamageMin
        {
            get
            {
                return base.DamageMin;
            }
            set
            {
                base.DamageMin = value;

                ValidateDamage(this);
            }
        }

        [Gene("Claw Size", 21, 23, 20, 24, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int DamageMax
        {
            get
            {
                return base.DamageMax;
            }
            set
            {
                base.DamageMax = value;

                ValidateDamage(this);
            }
        }
#else
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
#endif

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

        [Gene("Fire Breath", 0.85, 1.00, 0.85, 1.15, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public double BreathScaler
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
                    // Yoar: Changed "Physique" to being percentage bonus (the way it's done for Chicken)
#if old
                    case "Physique":
                        {
                            if (HitsMaxDiff < -10)
                                return "Frail";
                            else if (HitsMaxDiff < -5)
                                return "Spindly";
                            else if (HitsMaxDiff < 0)
                                return "Slight";
                            else if (HitsMaxDiff < 5)
                                return "Lithe";
                            else if (HitsMaxDiff < 10)
                                return "Sturdy";
                            else
                                return "Tough";
                        }
#else
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
#endif
                    // Yoar: Changed the effects of "Claw Accuracy" and "Claw Size"
#if old
                    case "Claw Accuracy":
                        {
                            if (DamageMin < 14)
                                return "Unwieldy";
                            else if (DamageMin < 16)
                                return "Able";
                            else if (DamageMin < 18)
                                return "Deft";
                            else
                                return "Precise";
                        }
                    case "Claw Size":
                        {
                            switch (DamageMax)
                            {
                                case 20:
                                    return "Undeveloped";
                                case 21:
                                    return "Small";
                                case 22:
                                    return "Ample";
                                case 23:
                                    return "Large";
                                case 24:
                                    return "Frightening";
                            }
                            break;
                        }
#else
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
#endif
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
                    case "Fire Breath":
                        {
                            if (BreathScaler < 0.90)
                                return "Fizzling";
                            else if (BreathScaler < 1.00)
                                return "Sizzling";
                            else if (BreathScaler < 1.10)
                                return "Scorching";
                            else
                                return "Blazing";
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
        public override Type EggType { get { return typeof(DragonEgg); } }
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
        }

        public override void OnGrowth(Maturity oldMaturity)
        {
            switch (this.Maturity)
            {
                case Maturity.Infant:
                    {
                        this.Body = 0xCE; // lava lizard
                        if (IsRegularHue(this.Hue))
                            this.Hue = this.Female ? 1053 : 1138;
                        BreedingSystem.ScaleStats(this, 0.4);
                        break;
                    }
                case Maturity.Child:
                    {
                        this.Body = 0x31F; // swamp dragon (armored)
                        if (IsRegularHue(this.Hue))
                            this.Hue = this.Female ? 1053 : 1138;
                        break;
                    }
                case Maturity.Youth:
                    {
                        this.Body = this.Female ? 0x3C : 0x3D; // drake
                        if (IsRegularHue(this.Hue))
                            this.Hue = 0;
                        break;
                    }
                case Maturity.Adult:
                    {
                        this.Body = this.Female ? 0xC : 0x3B; // dragon
                        if (IsRegularHue(this.Hue))
                            this.Hue = 0;
                        break;
                    }
                case Maturity.Ancient:
                    {
                        this.Body = 0x2E; // ancient wyrm
                        if (IsRegularHue(this.Hue))
                            this.Hue = 0;
                        BreedingSystem.ScaleStats(this, 0.6);
                        break;
                    }
            }
        }

        private static bool IsRegularHue(int hue)
        {
            return (hue == 0 || hue == 1053 || hue == 1138);
        }

        public override bool CanBreed()
        {
            return base.CanBreed() && (Core.UOTC_CFG || this.Region.Name == "Fire");
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
        public override Characteristics MyCharacteristics { get { return Characteristics.Run | Characteristics.Fly; } }
        public override bool HasBreath { get { return true; } } // fire breath enabled
        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 4 : 0; } }
        public override int Scales { get { return 7; } }
        public override ScaleType ScaleType { get { return (Body == 12 ? ScaleType.Yellow : ScaleType.Red); } }
        public override FoodType FavoriteFood { get { return FoodType.Meat; } }
        public override HideType HideType { get { return HideType.Barbed; } }

        public Dragon(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                for (int i = 0; i < 8; ++i)
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
                {   // http://web.archive.org/web/20020213035855/uo.stratics.com/hunters/dragon.shtml
                    // 1200 to 1400 Gold, Gems, Magic items, 19 Raw Ribs (carved), 20 Hides (carved)

                    if (Spawning)
                    {
                        PackGold(1200, 1400);
                    }
                    else
                    {
                        PackGem(1, .9);
                        PackGem(1, .5);
                        PackMagicStuff(1, 2, 0.05); // 8/1/2023, Adam: changed from 0.40
                        PackMagicStuff(1, 2, 0.05);
                    }
                }
                else
                {
                    AddLoot(LootPack.FilthyRich, 2);
                    AddLoot(LootPack.Gems, 8);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)8); // version

            // version 7
            writer.Write((double)m_BreathScaler);

            // version 6
            writer.Write((double)m_ClawAccuracy);

            // version 5
            writer.Write((double)m_HitsMaxFactor);

            // version 4: Breeding System overhaul

            // version 3
#if old
            writer.Write((int)m_Maturity);
            writer.Write(m_LastGrowth);
            writer.Write(m_GainFactor);
            writer.Write(m_Hatchdate);
            writer.Write(m_CheckedBody);
#endif

            // version 2
            // do nothing - one-time logic update placeholder

            // version 1
            writer.Write((int)m_IntMax);
            writer.Write((int)m_DexMax);
            writer.Write((int)m_StrMax);
            writer.Write((double)m_StatCapFactor);
            writer.Write((int)m_Hides);
            writer.Write((int)m_Meat);
#if old
            writer.Write(m_HitsMaxDiff);
#endif

            // version 0
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 8:
                case 7:
                    {
                        m_BreathScaler = reader.ReadDouble();

                        if (version < 8)
                            reader.ReadDouble(); // m_RareHueFactor

                        goto case 6;
                    }
                case 6:
                    {
                        m_ClawAccuracy = reader.ReadDouble();
                        goto case 5;
                    }
                case 5:
                    {
                        m_HitsMaxFactor = reader.ReadDouble();
                        goto case 4;
                    }
                case 4:
                case 3:
                    {
                        if (version < 4)
                        {
                            this.Maturity = (Maturity)(reader.ReadInt() + 1);
                            reader.ReadDateTime(); // m_LastGrowth
                            reader.ReadDouble(); // m_GainFactor
                            this.Birthdate = reader.ReadDateTime();
                            reader.ReadDateTime(); // m_CheckedBody
                        }

                        goto case 2;
                    }
                case 2:
                    {
                        goto case 1;
                    }
                case 1:
                    {
                        m_IntMax = reader.ReadInt();
                        m_DexMax = reader.ReadInt();
                        m_StrMax = reader.ReadInt();
                        m_StatCapFactor = reader.ReadDouble();
                        m_Hides = reader.ReadInt();
                        m_Meat = reader.ReadInt();

                        if (version < 5)
                        {
                            int hitsMaxDiff = reader.ReadInt();

                            this.HitsMaxFactor = 0.85 + Math.Max(0.00, Math.Min(0.30, (hitsMaxDiff + 15) / 100.0));
                        }

                        goto case 0;
                    }
                case 0:
                    break;
            }

            if (version < 3)
                this.Maturity = Maturity.Adult;

            if (version < 2)
            {
                StatCapFactor = Utility.RandomDouble() * 0.05 + 1.975;
                StrMax = (int)(Utility.RandomDouble() * 150) + 1145;
                IntMax = (int)(Utility.RandomDouble() * 96) + 640;
                DexMax = (int)(Utility.RandomDouble() * 26) + 133;
            }

            // version 4: Breeding system overhaul

            if (version < 5)
            {
                // convert claw accuracy
                switch (this.DamageMin)
                {
                    case 12: m_ClawAccuracy = 0.85; break;
                    case 13: m_ClawAccuracy = 0.90; break;
                    case 14: m_ClawAccuracy = 0.90; break;
                    case 15: m_ClawAccuracy = 0.95; break;
                    case 16: m_ClawAccuracy = 1.00; break;
                    case 17: m_ClawAccuracy = 1.05; break;
                    case 18: m_ClawAccuracy = 1.10; break;
                    case 19: m_ClawAccuracy = 1.10; break;
                    case 20: m_ClawAccuracy = 1.15; break;
                }

                // convert claw size
                switch (this.DamageMax)
                {
                    case 20: this.DamageMin = 11; break; // results in: [11,15]
                    case 21: this.DamageMin = 13; break; // results in: [13,18]
                    case 22: this.DamageMin = 15; break; // results in: [15,21]
                    case 23: this.DamageMin = 17; break; // results in: [17,23]
                    case 24: this.DamageMin = 19; break; // results in: [19,26]
                }
            }

            if (version == 5)
            {
                // don't screw players; no random init. of genes during version update
#if false
                Genetics.InitGenes(this, "Claw Accuracy");
#else
                m_ClawAccuracy = 1.10;
#endif
            }

            if (version < 7)
            {
                // don't screw players; no random init. of genes during version update
#if false
                Genetics.InitGenes(this, "Fire Breath");
#else
                m_BreathScaler = 1.0;
#endif
            }
        }
    }

    public class DragonEgg : BaseHatchableEgg
    {
        public override string BeakMessage { get { return "You can see a claw!"; } }

        [Constructable]
        public DragonEgg()
            : this(new Dragon())
        {
        }

        public DragonEgg(BaseCreature chick)
            : base(chick)
        {
            ItemID = 0x1363;
            Name = "a dragon egg";
            Hue = 1053;
        }

        public override Food Cook()
        {
            return new CookedDragonEgg();
        }

        public DragonEgg(Serial serial)
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

            if (version < 1)
            {
                this.Birthdate = reader.ReadDateTime();
                this.Chick = reader.ReadMobile() as BaseCreature;
                this.Health = reader.ReadInt();

                if (Name == "dragon egg")
                    Name = "a dragon egg";
            }
        }
    }

    public class CookedDragonEgg : Food
    {
        [Constructable]
        public CookedDragonEgg()
            : base(0x1363)
        {
            Weight = 8;
            FillFactor = 20;
            Name = "a cooked dragon egg";
        }

        public CookedDragonEgg(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(2); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version < 1)
                this.Name = "a cooked dragon egg";

            if (version < 2)
                this.Stackable = false;
        }

        public override bool Eat(Mobile from)
        {
            if (base.Eat(from))
            {
                from.AddStatMod(new StatMod(StatType.All, "CookedDragonEgg", 5.0, TimeSpan.FromMinutes(2.0)));
                from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
                from.PlaySound(0x1EA);

                return true;
            }

            return false;
        }
    }
}