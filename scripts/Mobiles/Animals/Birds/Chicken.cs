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

/* Scripts/Mobiles/Animals/Birds/Chicken.cs
 *	ChangeLog :
 *	8/26/23, Yoar
 *	    Added custom egg-laying mechanic. Enabled for all shards.
 *  12/26/21, Yoar
 *      Fixed breedable pets' stat caps. Problem: Breedable pets are unable to gain stats.
 *      Cause: StatCap defaults to a value of 225. Solution: StatCap now returns the total
 *      of StrMax, DexMax, IntMax, scaled by the versatility gene.
 *  10/23/21, Yoar
 *      Don't screw players; no random init. of genes during version update.
 *  10/22/21, Yoar
 *      Readded "Talon Accuracy" gene. Now gives a hit chance bonus.
 *  10/21/21, Yoar
 *      Now properly initializing m_StatCapFactor, m_HitsMaxFactor to 1.0.
 *  10/20/21, Yoar: Breeding System overhaul
 *      - Abstracted the breeding system.
 *      - The behavior from ChickenAI is now coded in the general-purpose MatingRitual object. Removed ChickenAI.
 *      - ChickenEgg now derives from BaseHatchableEgg.
 *      - Reorganized remaining genes/breeding code into two #regions.
 *      - Physique: Increased breeding range by +0.05.
 *      - Merged "Talon Accuracy" into "Talon Size".
 *	5/12/10, adam
 *		update AttackOrderHack to account for ScaredOfScaryThings
 *	06/18/07 Taran Kain
 *		Fixed proxy aggression with new abstraction
 *  6/13/07, Adam
 *      Emergency try/catch in HatchTick to stop this exception.
 *      We'll look deeper tomorrow.
 *      Exception: 
 *          System.NullReferenceException: Object reference not set to an instance of an object. 
 *          at Server.Mobiles.BaseCreature.Delete() 
 *          at Server.Mobiles.ChickenEgg.HatchTick() 
 *          at Server.Mobiles.ChickenEgg.<OnTick>b__0(ChickenEgg egg) 
 *  04/22/07 Taran Kain
 *      Fix disappearing eggs, hatching messages
 *  04/16/07 Taran Kain
 *      Fixed HitsMax to never return 0
 *  04/09/07 Taran Kain
 *      Bugfixes: Fixed ClientsInRange check to use GetWorldLocation, fixed house lockdown/egg deletion issue.
 *  04/05/07 Taran Kain
 *      Fixed crash in ChickenEgg
 *  04/04/07 Taran Kain
 *      Redesigned ChickenAI to be better encapsulated
 *      Added SpecialHue flag for reward chickens
 *      Made only tame chickens mate
 *  4/3/07, Adam
 *      Add a BreedingEnabled bit check
 *  03/29/07 Taran Kain
 *      Bugfixes
 *      Changed ChickenAI.DoActionInteract from a wannabe state machine to a real state machine
 *  03/28/07 Taran Kain
 *      Added in Genes to Chicken
 *      Created ChickenAI
 *      Created ChickenEgg
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 2 lines removed.
*/

using Server.Engines.Breeding;
using Server.Items;
using System;
using System.Reflection;

namespace Server.Mobiles
{
    [CorpseName("a chicken corpse")]
    public class Chicken : BaseCreature
    {
        [Constructable]
        public Chicken()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Name = "a chicken";
            Body = 0xD0;
            BaseSoundID = 0x6E;

            SetStr(StrMax / 6 - 1, StrMax / 6 + 1);
            SetDex(DexMax / 2 - 2, DexMax / 2 + 2);
            SetInt(IntMax / 4 - 1, IntMax / 4 + 1);

            SetSkill(SkillName.MagicResist, 4.0);
            SetSkill(SkillName.Tactics, 5.0);
            SetSkill(SkillName.Wrestling, 5.0);

            Fame = 150;
            Karma = 0;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -0.9;

            // enroll into breeding system; chickens are always breedable
            BreedingParticipant = true;
        }

        public override Characteristics MyCharacteristics { get { return Characteristics.Run | Characteristics.Fly; } }

        #region Egg-Laying

        private DateTime m_LastEggLaid;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastEggLaid
        {
            get { return m_LastEggLaid; }
            set { m_LastEggLaid = value; }
        }

        protected override bool OnMove(Direction d)
        {
            if (!base.OnMove(d))
                return false;

            // 8/28/23, Yoar: Added custom egg-laying mechanic. Enabled for all shards.
            if (Core.RuleSets.AllShards)
                CheckLayEgg();

            return true;
        }

        private void CheckLayEgg()
        {
            if (Map == null || Map == Map.Internal)
                return;

            if (m_LastEggLaid == DateTime.MinValue)
                m_LastEggLaid = DateTime.UtcNow - TimeSpan.FromHours(Utility.RandomMinMax(0, 12));

            if (DateTime.UtcNow >= m_LastEggLaid + TimeSpan.FromMinutes(15.0))
            {
                double hoursAgo = (DateTime.UtcNow - m_LastEggLaid).TotalHours;

                if (hoursAgo > 24.0)
                    hoursAgo = 24.0;

                double chance = hoursAgo * 0.0025; // for every hour passed since last laying, 0.25% chance to lay an egg

                if (Utility.RandomDouble() < chance)
                {
                    m_LastEggLaid = DateTime.UtcNow;

                    new Eggs().MoveToWorld(Location, Map);
                }
            }
        }

        #endregion

        #region Genes

        private double m_StatCapFactor = 1.0;

        [Gene("Versatility", 1.975, 2.025, 1.950, 2.050, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual double StatCapFactor
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

        [Gene("StrMax", 20, 30, 10, 100)]
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

        [Gene("IntMax", 15, 20, 10, 30)]
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

        [Gene("DexMax", 25, 35, 20, 120)]
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

        // Yoar: Increased breeding range by +0.05
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

        // Yoar: Changed the effects of "Talon Accuracy" and "Talon Size"
#if old
        [Gene("Talon Accuracy", 1, 1, 1, 3, GeneVisibility.Tame)]
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

        [Gene("Talon Size", 1, 1, 1, 5, GeneVisibility.Wild)]
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
        private double m_TalonAccuracy;

        [Gene("Talon Accuracy", 0.85, 1.00, 0.85, 1.15, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public double TalonAccuracy
        {
            get
            {
                return m_TalonAccuracy;
            }
            set
            {
                m_TalonAccuracy = value;
            }
        }

        public override double GetAccuracyScalar()
        {
            return m_TalonAccuracy;
        }

        [Gene("Talon Size", 1, 1, 1, 5, GeneVisibility.Wild)]
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

        [Gene("Feather Armor", 1, 3, 1, 6, GeneVisibility.Wild)]
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

        private int m_Meat, m_Feathers;

        [Gene("Meat", 1, 2, 1, 4, GeneVisibility.Wild)]
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

        [Gene("Feathers", 0.25, 0.25, 23, 27, 20, 30, GeneVisibility.Wild)]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Feathers
        {
            get
            {
                return m_Feathers;
            }
            set
            {
                m_Feathers = value;
            }
        }

        private bool m_SpecialHue;

        [Gene("Special Hue", 0, 0, 0, 0, 0, 1, GeneVisibility.Invisible, 1.0)]
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public double SpecialHue
        {
            get
            {
                if (m_SpecialHue)
                    return 1.0;
                else
                    return 0.0;
            }
            set
            {
                m_SpecialHue = (value >= 0.95);

                if (m_SpecialHue)
                    Hue = 642; // 5% of the time we get a brown chicken
            }
        }

        public override string DescribeGene(PropertyInfo prop, GeneAttribute attr)
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
                // Yoar: Increased breeding range by +0.05
#if old
                case "Physique":
                    {
                        if (HitsMaxFactor < .85)
                            return "Frail";
                        else if (HitsMaxFactor < .90)
                            return "Spindly";
                        else if (HitsMaxFactor < .95)
                            return "Slight";
                        else if (HitsMaxFactor < 1.00)
                            return "Lithe";
                        else if (HitsMaxFactor < 1.05)
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
                // Yoar: Changed the effects of "Talon Accuracy" and "Talon Size"
#if old
                case "Talon Accuracy":
                    {
                        switch (DamageMin)
                        {
                            case 1:
                                return "Unwieldy";
                            case 2:
                                return "Deft";
                            case 3:
                                return "Precise";
                        }
                        break;
                    }
                case "Talon Size":
                    {
                        switch (DamageMax)
                        {
                            case 1:
                                return "Undeveloped";
                            case 2:
                                return "Small";
                            case 3:
                                return "Ample";
                            case 4:
                                return "Large";
                            case 5:
                                return "Frightening";
                        }
                        break;
                    }
#else
                case "Talon Accuracy": // 0.85-1.15
                    {
                        if (TalonAccuracy < 0.90)
                            return "Unwieldy";
                        else if (TalonAccuracy < 1.00)
                            return "Able";
                        else if (TalonAccuracy < 1.10)
                            return "Deft";
                        else
                            return "Precise";
                    }
                case "Talon Size": // 1-5
                    {
                        if (DamageMin < 2)
                            return "Undeveloped";
                        else if (DamageMin < 3)
                            return "Small";
                        else if (DamageMin < 4)
                            return "Ample";
                        else if (DamageMin < 5)
                            return "Large";
                        else
                            return "Frightening";
                    }
#endif
                case "Feather Armor":
                    {
                        switch (VirtualArmor)
                        {
                            case 1:
                                return "Flimsy";
                            case 2:
                                return "Delicate";
                            case 3:
                                return "Durable";
                            case 4:
                                return "Rugged";
                            case 5:
                                return "Hard";
                            case 6:
                                return "Solid";
                        }
                        break;
                    }
                case "Meat":
                    {
                        switch (Meat)
                        {
                            case 1:
                                return "Frail";
                            case 2:
                                return "Lean";
                            case 3:
                                return "Brawny";
                            case 4:
                                return "Colossal";
                        }
                        break;
                    }
                case "Feathers":
                    {
                        if (Feathers < 22)
                            return "Nearly Bald";
                        else if (Feathers < 24)
                            return "Thin";
                        else if (Feathers < 26)
                            return "Healthy";
                        else if (Feathers < 28)
                            return "Thick";
                        else
                            return "Extremely Thick";
                    }
                default:
                    return base.DescribeGene(prop, attr);
            }

            return "Error";
        }

        #endregion

        #region Breeding

        public override Type EggType { get { return typeof(ChickenEgg); } }

        #endregion

        public override MeatType MeatType { get { return MeatType.Bird; } }
        public override FoodType FavoriteFood { get { return FoodType.GrainsAndHay; } }

        private static readonly CarveEntry[] m_RareCarvables = new CarveEntry[]
            {
                new CarveEntry(1, typeof(Item), 0x1E87), // chicken
                new CarveEntry(1, typeof(Item), 0x1E8B), // plucked chicken
            };

        public override double RareCarvableChance { get { return 0.015; } }
        public override CarveEntry[] RareCarvables { get { return m_RareCarvables; } }

        public Chicken(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)6);

            // version 6
            writer.WriteDeltaTime(m_LastEggLaid);

            // version 5
            writer.Write((double)m_TalonAccuracy);

            // version 4: SpecialHue fix, changed the effects of "Talon Accuracy" and "Talon Size"

            // version 3: Auto-enroll into breeding system

            // version 2
            writer.Write((bool)m_SpecialHue);

            // version 1
            writer.Write((int)m_StrMax);
            writer.Write((int)m_IntMax);
            writer.Write((int)m_DexMax);
            writer.Write((double)m_StatCapFactor);
            writer.Write((double)m_HitsMaxFactor);
            writer.Write((int)m_Meat);
            writer.Write((int)m_Feathers);

            // version 0
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 6:
                    {
                        m_LastEggLaid = reader.ReadDeltaTime();

                        goto case 5;
                    }
                case 5:
                    {
                        m_TalonAccuracy = reader.ReadDouble();

                        goto case 4;
                    }
                case 4:
                case 3:
                case 2:
                    {
                        m_SpecialHue = reader.ReadBool();

                        goto case 1;
                    }
                case 1:
                    {
                        m_StrMax = reader.ReadInt();
                        m_IntMax = reader.ReadInt();
                        m_DexMax = reader.ReadInt();
                        m_StatCapFactor = reader.ReadDouble();
                        m_HitsMaxFactor = reader.ReadDouble();
                        m_Meat = reader.ReadInt();
                        m_Feathers = reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }

            // do this AFTER reading in all other data, since we might have changed some of the limits
            if (version < 1)
                Genetics.InitGenes(this);

            if (version < 3)
                BreedingParticipant = true;

            // version 4: Breeding system overhaul

            if (version < 4)
            {
                m_SpecialHue = (this.Hue != 0);

                // convert talon accuracy
                switch (this.DamageMin)
                {
                    case 1: m_TalonAccuracy = 0.85; break;
                    case 2: m_TalonAccuracy = 1.00; break;
                    case 3: m_TalonAccuracy = 1.15; break;
                }

                // convert talon size
                switch (this.DamageMax)
                {
                    case 1: this.DamageMin = 1; break;
                    case 2: this.DamageMin = 2; break;
                    case 3: this.DamageMin = 3; break;
                    case 4: this.DamageMin = 4; break;
                    case 5: this.DamageMin = 5; break;
                }
            }

            if (version == 4)
            {
                // don't screw players; no random init. of genes during version update
#if false
                Genetics.InitGenes(this, "Talon Accuracy");
#else
                m_TalonAccuracy = 1.10;
#endif
            }
        }
    }

    public class ChickenEgg : BaseHatchableEgg
    {
        public override double DefaultWeight { get { return 0.5; } }
        public override TimeSpan HatchTime { get { return TimeSpan.FromDays(Utility.RandomMinMax(2, 3)); } }

        [Constructable]
        public ChickenEgg()
            : this(new Chicken())
        {
        }

        public ChickenEgg(BaseCreature chick)
            : base(chick)
        {
            ItemID = 0x9B5;
            Hue = 0;
            CookingLevel = 15;
        }

        public override Food Cook()
        {
            return new FriedEggs();
        }

        public ChickenEgg(Serial serial)
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
            }
        }
    }
}