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

/* Scripts/Mobiles/Monsters/Reptile/Magic/Basilisk.cs
 * ChangeLog
 *  1/13/22, Yoar
 *      Reimplemented bassy's hiding/stealth mechanics.
 *  1/7/22, Yoar
 *      Increased stat caps. The number of spendable stat points remains roughly equal.
 *      This means that basilisks don't necessarily get stronger. However, we can now
 *      properly shift stat points from one stat into the other. Furthermore, we can now
 *      train egg-born basilisks up to the same number of stat points as wild basilisks.
 *  12/26/21, Yoar
 *      Fixed breedable pets' stat caps. Problem: Breedable pets are unable to gain stats.
 *      Cause: StatCap defaults to a value of 225. Solution: StatCap now returns the total
 *      of StrMax, DexMax, IntMax, scaled by the versatility gene.
 *  10/23/21, Yoar
 *      Don't screw players; no random init. of genes during version update.
 *  10/22/21, Yoar
 *      Familiar heal particle effects/sounds are now hidden when the basilisk/master are hidden
 *  10/22/21, Yoar
 *      Changes to familiar heal:
 *      - Added map/range/alive checks.
 *      - Now works while the master is hidden.
 *      - Redid healing formula: Scales by #bonded days. Diminishing returns.
 *      Added two new genes:
 *      - "Toxicity": Grants a bonus to HitPoison.
 *      - "Healing Potency": Grants a bonus to familiar heal.
 *  10/22/21, Yoar
 *      Reduced the intensity of the Basilisk egg hue.
 *  10/22/21, Yoar
 *      Added "Claw Accuracy" gene. Gives a hit chance bonus.
 *  10/21/2021, Yoar
 *      - Now differentiating body/hue between male/female
 *      - Added genes + enabled breeding.
 *	9/28/21, Adam
 *	    Our new Basilisk is a 'Familiar'
 *		Replace AI with new BasiliskAI
 *		No more trying to manage specialities in OnThink()
 *		Females heal mana + 1 + healingBonus
 *		Males heal stam + 1 + healingBonus
 *		and once the Bassy reaches 100 days old
 *		either will heal Hits + 1 + healingBonus
 *		healingBonus = this.Age / 100
 *	5/19/06, Adam
 *		Add override ControlDifficulty(). Only use dragon difficulty for controling
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	10/1/04, Adam
 *		Up the skill cap for this baby to 800
 *	9/30/04, Adam
 *		Scale HitPoison (poison strength) based on poisoning skill
 *	9/28/04, Adam
 *		Heal the mana of the master if he is not hidden (like AOS ShadowWisp Familiar)
 *	9/21/04, Adam
 *		increase int from 101-140 to 436-475
 *		Add special ability - hide when master hides, attack what master attacks.
 *		only exhibit special abilities if bonded
 *	9/17/04, Adam
 *		1. Created
 */

using Server.Engines.Breeding;
using Server.Items;
using Server.Network;
using System;

namespace Server.Mobiles
{
    [CorpseName("a basilisk corpse")]
    public class Basilisk : BaseCreature
    {
        [Constructable]
        public Basilisk()
            : base(AIType.AI_Basilisk, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a basilisk";
            Body = this.Female ? 0x3C : 0x3D;
            Hue = this.Female ? 0x7D2 : 0x7D6;
            BaseSoundID = 362;
            SkillsCap = 8000;   // Adam: 800 cap {magic + poison + melee}

            SetStr(401, 430);
            SetDex(133, 152);
            SetInt(436, 475);

#if old
            SetHits(241, 258);

            SetDamage(11, 17);
#endif

            SetSkill(SkillName.Poisoning, 60.1, 80.0);
            SetSkill(SkillName.EvalInt, 30.1, 40.0);
            SetSkill(SkillName.Magery, 30.1, 40.0);
            SetSkill(SkillName.MagicResist, 99.1, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 92.5);

            Fame = 6000;
            Karma = -6000;

#if old
            VirtualArmor = 50;
#endif

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 98.9;
        }
        public override Characteristics MyCharacteristics { get { return Characteristics.Run | Characteristics.Fly; } }

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

        // 401, 430
        [Gene("StrMax", 601, 675, 511, 775)] // breeding range: [-15%, +15%]
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
        [Gene("IntMax", 651, 725, 556, 835)] // breeding range: [-15%, +15%]
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

        // 133, 152
        [Gene("DexMax", 176, 225, 151, 260)] // breeding range: [-15%, +15%]
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

        public override int HitsMax // approximately two-thirds of STR
        {
            get { return Math.Max(1, (int)Math.Round(this.HitsMaxFactor * 2.0 * this.Str / 3.0)); }
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

        [Gene("Claw Size", 12, 14, 8, 18, GeneVisibility.Tame)]
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

        [Gene("Scales", 48, 52, 40, 60, GeneVisibility.Wild)]
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

        private double m_Toxicity;

        [Gene("Toxicity", -0.10, +0.10, -0.20, +0.20, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public double Toxicity
        {
            get
            {
                return m_Toxicity;
            }
            set
            {
                m_Toxicity = value;
            }
        }

        private double m_HealingPotency;

        [Gene("Healing Potency", 0.90, 1.10, 0.80, 1.20, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public double HealingPotency
        {
            get
            {
                return m_HealingPotency;
            }
            set
            {
                m_HealingPotency = value;
            }
        }

        private int m_Meat, m_Hides;

        [Gene("Meat", 9, 11, 6, 14, GeneVisibility.Wild)]
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
                    case "Claw Accuracy": // [0.85, 1.15]
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
                    case "Claw Size": // [8, 18]
                        {
                            if (DamageMin < 10)
                                return "Undeveloped";
                            else if (DamageMin < 12)
                                return "Small";
                            else if (DamageMin < 14)
                                return "Ample";
                            else if (DamageMin < 16)
                                return "Large";
                            else
                                return "Frightening";
                        }
                    case "Toxicity": // [-0.20, +0.20]
                        {
                            if (Toxicity < -0.10)
                                return "Flat";
                            else if (Toxicity < +0.00)
                                return "Pungent";
                            else if (Toxicity < +0.10)
                                return "Putrid";
                            else
                                return "Noxious";
                        }
                    case "Healing Potency": // [0.80, 1.20]
                        {
                            if (HealingPotency < 0.90)
                                return "Weakened";
                            else if (HealingPotency < 1.00)
                                return "Regular";
                            else if (HealingPotency < 1.10)
                                return "Enhanced";
                            else
                                return "Powerful";
                        }
                    case "Scales":
                        {
                            if (VirtualArmor < 44)
                                return "Flimsy";
                            else if (VirtualArmor < 48)
                                return "Durable";
                            else if (VirtualArmor < 52)
                                return "Rugged";
                            else if (VirtualArmor < 56)
                                return "Hard";
                            else
                                return "Plated";
                        }
                    case "Meat":
                        {
                            if (Meat < 8)
                                return "Frail";
                            else if (Meat < 10)
                                return "Lean";
                            else if (Meat < 12)
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
        public override Type EggType { get { return typeof(BasiliskEgg); } }
        public override bool EatsBP { get { return true; } }
        public override bool EatsSA { get { return true; } }
        public override bool EatsKukuiNuts { get { return true; } }

        public override void OnHatch()
        {
            this.SetSkill(SkillName.Poisoning, 20.1, 30.0);
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
                        BreedingSystem.ScaleStats(this, 0.3); // lower than dragons as dragons spawn with higher stats
                        break;
                    }
                case Maturity.Child:
                    {
                        this.Body = 0x31F; // swamp dragon (armored)
                        break;
                    }
                case Maturity.Youth:
                    {
                        this.Body = this.Female ? 0x3C : 0x3D; // drake
                        break;
                    }
                case Maturity.Adult:
                    {
                        this.Body = this.Female ? 0x3C : 0x3D; // drake
                        break;
                    }
                case Maturity.Ancient: // TODO: Special body value?
                    {
                        this.Body = this.Female ? 0x3C : 0x3D; // drake
                        BreedingSystem.ScaleStats(this, 0.6);
                        break;
                    }
            }
        }

        public override bool CanBreed()
        {
            return base.CanBreed(); // TODO: Special requirement?
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

        public override bool ReAcquireOnMovement { get { return true; } }
        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }
        public override Poison PoisonImmune { get { return Poison.Deadly; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 4 : 0; } }
        public override HideType HideType { get { return HideType.Barbed; } }
        public override FoodType FavoriteFood { get { return FoodType.Meat | FoodType.Fish; } }

        public override double ControlDifficulty()
        {   // only use dragon difficulty for controling
            return 93.9;
        }

        public override Poison HitPoison
        {
            get
            {
                int baseFixed = this.Skills[SkillName.Poisoning].BaseFixedPoint;

                double rnd = Utility.RandomDouble();

                rnd += m_Toxicity; // bonus from "Toxicity" gene

                if (baseFixed > 1000)
                    return (0.8 >= rnd ? Poison.Greater : Poison.Deadly); // 80% greater, 20% deadly
                else if (baseFixed > 900)
                    return (0.9 >= rnd ? Poison.Greater : Poison.Deadly); // 90% greater, 10% deadly
                else if (baseFixed > 800)
                    return (0.8 >= rnd ? Poison.Regular : Poison.Greater); // 80% regular, 20% greater
                else if (baseFixed > 700)
                    return (0.9 >= rnd ? Poison.Regular : Poison.Greater); // 90% regular, 10% greater
                else if (baseFixed > 600)
                    return (0.8 >= rnd ? Poison.Lesser : Poison.Regular); // 80% lesser, 20% regular
                else if (baseFixed > 500)
                    return (0.9 >= rnd ? Poison.Lesser : Poison.Regular); // 90% lesser, 10% regular
                else
                    return (1.0 >= rnd ? Poison.Lesser : Poison.Regular); // 100% lesser, 0% regular
            }
        }

        public override int GetAttackSound()
        {
            return 713;
        }

        public override int GetAngerSound()
        {
            return 718;
        }

        public override int GetDeathSound()
        {
            return 716;
        }

        public override int GetHurtSound()
        {
            return 721;
        }

        public override int GetIdleSound()
        {
            return 725;
        }

        public Basilisk(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(400, 500);
                PackItem(new DeadlyPoisonPotion());
                PackMagicEquipment(1, 2, 0.20, 0.20);
                PackMagicEquipment(1, 2, 0.05, 0.05);

                // Category 3 MID
                PackMagicItem(1, 2, 0.10);
                PackMagicItem(1, 2, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // ai special
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                    }
                }
                else
                {   // Standard RunUO
                    // ai special
                }
            }
        }

        private DateTime m_BondedDate;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime BondedDate
        {
            get { return m_BondedDate; }
            set { m_BondedDate = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan BondedTime
        {
            get
            {
                if (m_BondedDate == DateTime.MinValue)
                    return TimeSpan.Zero;

                TimeSpan ts = DateTime.UtcNow - m_BondedDate;

                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero;

                return ts;
            }
            set { m_BondedDate = DateTime.UtcNow - value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BondedDays
        {
            get { return (int)BondedTime.TotalDays; }
            set { BondedTime = TimeSpan.FromDays(value); }
        }

        public override void OnAfterSetBonded()
        {
            if (this.IsBonded)
                m_BondedDate = DateTime.UtcNow;
            else
                m_BondedDate = DateTime.MinValue;
        }

        public bool IsFamiliar { get { return !this.Deleted && this.Map != null && this.Map != Map.Internal && this.Alive && !this.IsDeadPet && this.Controlled && this.ControlMaster != null && this.IsBonded; } }

        public override void OnThink()
        {
            base.OnThink();

            // only exhibit special abilities if we're a familiar
            if (!IsFamiliar)
                return;

            // who's your daddy!
            Mobile master = this.ControlMaster;

            // Heal the the master
            DoFamiliarHeal(master);

            // fake stealth - simply mimic the master
            if (master.Hidden)
                this.Hidden = true;
            if (this.AllowedStealthSteps < master.AllowedStealthSteps)
                this.AllowedStealthSteps = master.AllowedStealthSteps;
        }

        #region Familiar Heal

        [Flags]
        private enum HealFlags : byte
        {
            None = 0x0,

            Hits = 0x1,
            Stam = 0x2,
            Mana = 0x4,
        }

        private HealFlags GetHealFlags(Mobile target)
        {
            HealFlags flags = HealFlags.None;

            if (BondedDays >= 100 && target.Hits < target.HitsMax)
                flags |= HealFlags.Hits;

            if (!this.Female && target.Stam < target.StamMax)
                flags |= HealFlags.Stam;

            if (this.Female && target.Mana < target.ManaMax)
                flags |= HealFlags.Mana;

            return flags;
        }

        public static int HealDelayMin = 5;
        public static int HealDelayMax = 30;
        public static double HealScalar = (2.0 / Math.Sqrt(30.0)) + Math.Pow(10, -6); // = 0.36515

        private DateTime m_NextHeal = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(HealDelayMin, HealDelayMax));

        public void DoFamiliarHeal(Mobile target)
        {
            if (this.Map != target.Map || !this.InRange(target, 10)) // TODO: LOS check?
                return;

            if (DateTime.UtcNow < m_NextHeal)
                return;

            m_NextHeal = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(HealDelayMin, HealDelayMax));

            HealFlags flags = GetHealFlags(target);

            if (flags == HealFlags.None)
                return;

            if (this.Hidden)
            {
                target.ProcessDelta();
                target.Send(new TargetEffect(this, 0x37C4, 1, 12, 1109, 6));
                target.Send(new PlaySound(0x1D3, this));
            }
            else
            {
                this.FixedEffect(0x37C4, 1, 12, 1109, 6);
                this.PlaySound(0x1D3);
            }

            Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(FamiliarHeal_OnTick), target);
        }

        private void FamiliarHeal_OnTick(object state)
        {
            Mobile target = (Mobile)state;

            if (target.Hidden)
            {
                target.ProcessDelta();
                target.Send(new TargetEffect(target, 0x37C4, 1, 12, 1109, 3));
            }
            else
            {
                target.FixedEffect(0x37C4, 1, 12, 1109, 3);
            }

            HealFlags flags = GetHealFlags(target);

            if (flags == HealFlags.None)
                return;

            int days = Math.Min(10000, this.BondedDays); // cap the number of days

            /* Healing formula (diminishing returns)
             * 
             * <     30 days :  1 hits
             * >=    30 days :  2 hits
             * >=    68 days :  3 hits
             * >=   120 days :  4 hits
             * ...
             * >= 10000 days : 36 hits
             */
            double heal = HealScalar * Math.Sqrt(days);

            heal *= HealingPotency; // bonus from "Healing Potency" gene

            int toHeal = Math.Max(1, (int)heal);

            if (flags.HasFlag(HealFlags.Hits))
                target.Hits += toHeal;

            if (flags.HasFlag(HealFlags.Stam))
                target.Stam += toHeal;

            if (flags.HasFlag(HealFlags.Mana))
                target.Mana += toHeal;
        }

        #endregion

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)4);

            // version 3
            writer.Write((DateTime)m_BondedDate);
            writer.Write((double)m_Toxicity);
            writer.Write((double)m_HealingPotency);

            // version 2
            writer.Write((double)m_ClawAccuracy);

            // version 1
            writer.Write((double)m_HitsMaxFactor);
            writer.Write((int)m_IntMax);
            writer.Write((int)m_DexMax);
            writer.Write((int)m_StrMax);
            writer.Write((double)m_StatCapFactor);
            writer.Write((int)m_Hides);
            writer.Write((int)m_Meat);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 4:
                case 3:
                    {
                        m_BondedDate = reader.ReadDateTime();
                        m_Toxicity = reader.ReadDouble();
                        m_HealingPotency = reader.ReadDouble();
                        goto case 2;
                    }
                case 2:
                    {
                        m_ClawAccuracy = reader.ReadDouble();
                        goto case 1;
                    }
                case 1:
                    {
                        m_HitsMaxFactor = reader.ReadDouble();
                        m_IntMax = reader.ReadInt();
                        m_DexMax = reader.ReadInt();
                        m_StrMax = reader.ReadInt();
                        m_StatCapFactor = reader.ReadDouble();
                        m_Hides = reader.ReadInt();
                        m_Meat = reader.ReadInt();
                        break;
                    }
            }

            // upgrade the AI to the new BasiliskAI
            if (this.AIObject != null && this.AIObject is MageAI)
                this.AI = AIType.AI_Basilisk;

            // version 1: Breeding system overhaul

            if (version < 1)
            {
                /* Genes were added in version 1. We'd now randomize these newly added genes.
                 * However, this may negatively impact existing basilisks! For example, an
                 * existing basilisk may roll bad genes all across the board, significantly
                 * weakening a player's beloved pet.
                 * 
                 * Instead, initialize the genes with more sensible values.
                 */

                // don't screw players; no random init. of genes during version update
#if false
                Genetics.InitGenes(this, "Versatility", "StrMax", "IntMax", "DexMax", "Physique", "Claw Size", "Scales", "Meat", "Hides");
#else
                m_StatCapFactor = 2.0;
                m_StrMax = this.RawStr;
                m_IntMax = this.RawInt;
                m_DexMax = this.RawDex;
                m_HitsMaxFactor = 1.10;
                m_Meat = 10;
                m_Hides = 20;
#endif
            }

            if (version < 2)
            {
                // don't screw players; no random init. of genes during version update
#if false
                Genetics.InitGenes(this, "Claw Accuracy");
#else
                m_ClawAccuracy = 1.10;
#endif
            }

            if (version < 3)
            {
                DateTime dt = this.Created + TimeSpan.FromDays(7.0); // just a guess

                if (dt > DateTime.UtcNow)
                    dt = DateTime.UtcNow;

                m_BondedDate = dt;

                // don't screw players; no random init. of genes during version update
#if false
                Genetics.InitGenes(this, "Toxicity", "Healing Potency");
#else
                m_Toxicity = 0.0;
                m_HealingPotency = 1.0;
#endif
            }

            if (version < 4)
            {
                // stat cap fix
                m_StrMax = GetValue(GetFactor(Math.Max(RawStr, m_StrMax), 401, 430), 601, 675);
                m_DexMax = GetValue(GetFactor(Math.Max(RawDex, m_DexMax), 133, 152), 176, 225);
                m_IntMax = GetValue(GetFactor(Math.Max(RawInt, m_IntMax), 436, 475), 651, 725);
            }
        }

        private static double GetFactor(int value, int min, int max)
        {
            return Math.Max(0.0, Math.Min(1.0, (double)(value - min) / (max - min)));
        }

        private static int GetValue(double factor, int min, int max)
        {
            return Math.Max(min, Math.Min(max, min + (int)(factor * (max - min))));
        }
    }

    public class BasiliskEgg : BaseHatchableEgg
    {
        public override string BeakMessage { get { return "You can see a claw!"; } }

        [Constructable]
        public BasiliskEgg()
            : this(new Basilisk())
        {
        }

        public BasiliskEgg(BaseCreature chick)
            : base(chick)
        {
            Name = "a basilisk egg";
            Hue = 2001;
        }

        public override Food Cook()
        {
            Food cookedEgg = new CookedDragonEgg();

            cookedEgg.Name = "a cooked basilisk egg";

            return cookedEgg;
        }

        public BasiliskEgg(Serial serial)
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