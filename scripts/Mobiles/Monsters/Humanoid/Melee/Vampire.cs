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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/Vampire.cs
 * ChangeLog
 *  11/14/2023, Adam (Major rewrite)
 *      Applies to Vampires, Walking Dead, and Vlad Dracula and VampAI
 *      1. disable the crazy walk-around when the vamp is within one tile of the target
 *      2. Use a property to get the proper body value for the Expansion. (No bats in T2A, we use an imp graphic.)
 *      3. increase gold significantly
 *      4. add Holy Water and Consecrated Water as loot
 *      5. Support for Holy Water and Consecrated Water damage
 *          for Consecrated Water, damage is reduced, but the vamp is 'confused'. See #8 below
 *      6. Holy Water: Players that drink are somewhat immune to the vamps specials (stun and hypnotize)
 *      7. Add mana cost for Hypnotize (already a dex cost for stun)
 *      8. Consecrated Water now confuses vamps for a short while so that they cannot use special moves
 *      9. bump up hits
 *	7/19/10, adam
 *		remove kits wild speedups and use normal UO speeds.
 * 		reason: vampires are anti tamer to encourage warriors and mages, but the flee speed was crazy
 * 			and no warrior could catch them.
 *	7/19/10, adam
 *		o remove the silver bonus given here in CheckWeaponImmunity() since we now give the standard Slayer
 *		bonus in BaseWeapon.
 *		o CheckWeaponImmunity() still reduces damage done by non silver weapons to 25%
 *	5/12/10, adam
 *		1. change IsScaryCondition() to always be scary to pets
 *		2. call new PackSlayerWeapon() function.
 *		3. remove magic weapon drop (replaced with slayer)
 *	7/03/08, weaver
 *		New chase mode handling to prevent constant shapeshifting.
 *	6/26/08, Adam
 *		if a silver weapon, do 150% damage
 *	3/16/08, Adam
 *		- remove CanRummageCorpses .. vamps don't steal
 *		- Make IsScaryCondition() all the time (not just night)
 *		- remove Aura (was redundant with IsScaryCondition)
 *	3/15/08, Adam
 *		sweeping redesign
 *			redesign DoTransform
 *			redesign batform logic
 *			rename variables to be sensible
 *			convert to using common VampireAI.CheckNight() code
 *			change OnThink() logic to not turn into a bat during combat
 *			change core to use our standard GenerateLoot() logic
 *			redesign logic so that the class can be better inherited.
 *				flytile logic 
 *				damage modifiers
 * 7/02/06, Kit
 *		InitBody/InitOutfit additions
 * 2/11/06, Adam
 *		remove basecreature override, and set this variable instead: BardImmune = true;
 * 1/24/06, Adam
 *		Make vampire ashes 'light gray'. and with a Weight of 1
 * 12/28/05, Kit
 *		Changed fightmode to fightmode player, allowed uncalmable creatures
 *		to not be effected by hypnotize, added IsscarytoPets with condition 
 *		of 70% of time if at night.
 * 12/26/05, Kit
 *		Fixed timedelay with peace logic.
 * 12/24/05, Kit
 *		Added CanFly mode logic.
 * 12/22/05, Kit
 *		Fixed bug with vamps not leaving loot in bat form at night.
 * 12/18/05, Kit
 *		Added in detect hidden skill/lowered virtual armor
 * 12/17/05, Kit
 *		When dieing vampires now leave vampire ashes if dureing day.
 *		Extended vamp night hours to 9pm to 6am uo time, add poison/barding immunity.
 * 12/13/05, Kit
 *		Added Hypnotize ability.
 * 12/09/05, Kit
 *		Added TransformEffect classes, added life drain. 
 * 12/06/05, Kit
 *		Initial Creation
 */

using Server.Diagnostics;
using Server.Items;
using System;

namespace Server.Mobiles
{
    public class Vampire : BaseCreature
    {


        /* for runtime tuning only
		 * we can delete/hard-code this at some future date
		 */
        #region RUNTIME TUNING

        private static int m_HypnotizeCost = 11;
        [CommandProperty(AccessLevel.Administrator)]
        public int HypnotizeCost { get { return m_HypnotizeCost; } set { m_HypnotizeCost = value; } }

        private static int m_LifeDrainMin = 10;
        private static int m_LifeDrainMax = 30;

        [CommandProperty(AccessLevel.Administrator)]
        public int LifeDrainMin { get { return m_LifeDrainMin; } set { m_LifeDrainMin = value; } }

        [CommandProperty(AccessLevel.Administrator)]
        public int LifeDrainMax { get { return m_LifeDrainMax; } set { m_LifeDrainMax = value; } }

        public virtual int LifeDrain { get { return Utility.RandomMinMax(m_LifeDrainMin, m_LifeDrainMax); } }

        private static int m_StamDrainMin = 3;
        private static int m_StamDrainMax = 7;

        [CommandProperty(AccessLevel.Administrator)]
        public int StamDrainMin { get { return m_StamDrainMin; } set { m_StamDrainMin = value; } }

        [CommandProperty(AccessLevel.Administrator)]
        public int StamDrainMax { get { return m_StamDrainMax; } set { m_StamDrainMax = value; } }

        public virtual int StamDrain { get { return Utility.RandomMinMax(m_StamDrainMin, m_StamDrainMax); } }

        private static double m_WrestlingMin = 98.0;
        private static double m_WrestlingMax = 110;

        [CommandProperty(AccessLevel.Administrator)]
        public double WrestlingMin { get { return m_WrestlingMin; } set { m_WrestlingMin = value; } }

        [CommandProperty(AccessLevel.Administrator)]
        public double WrestlingMax { get { return m_WrestlingMax; } set { m_WrestlingMax = value; } }

        private static double m_AnatomyMin = 97;
        private static double m_AnatomyMax = 115;

        [CommandProperty(AccessLevel.Administrator)]
        public double AnatomyMin { get { return m_AnatomyMin; } set { m_AnatomyMin = value; } }

        [CommandProperty(AccessLevel.Administrator)]
        public double AnatomyMax { get { return m_AnatomyMax; } set { m_AnatomyMax = value; } }

        [CommandProperty(AccessLevel.Administrator)]
        public virtual double ActiveSpeedFast
        {
            get
            {
                if (AIObject is VampireAI)
                    return (AIObject as VampireAI).ActiveSpeedFast;
                else
                    return 0;
            }
        }
        #endregion

        [Constructable]
        public Vampire()
            // remove kits wild speedups and use normal UO speeds.
            : base(AIType.AI_Vamp, FightMode.All | FightMode.Closest, 12, 1, 0.2, 0.4/*0.175, 0.350*/)
        {
            FlyArray = FlyTiles; //assign to mobile fly array for movement code to use.
            BardImmune = true;

            SpeechHue = 0x21;
            Hue = 0;
            HueMod = 0;

            // vamp stats
            SetStr(200, 300);
            SetDex(105, 135);
            SetInt(80, 105);
            //SetHits(140, 176);
            SetHits(298, 315);      // UnholySteed
            SetDamage(1, 5);        // all damage is via life drain (OnGaveMeleeAttack)

            VirtualArmor = 20;

            // skills needed for common vamp behavior
            CoreVampSkills();

            Fame = 10000;
            Karma = 0;

            InitBody();
            InitOutfit();
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool Confused
        {
            get
            {
                return VampireAI.Confused.Recall(this);
            }
        }
        public override bool ClickTitle { get { return false; } }
        //Transform effect classes
        public BatToHuman toHumanForm = new BatToHuman();
        public HumanToBat toBatForm = new HumanToBat();
        public bool BatForm { get { return this.Body == BatGraphic(); } }
        public int[] FlyTiles { get { return new int[] { 18507, 18506, 18505, 18504 }; } }
        //private DateTime m_LastTransform = DateTime.UtcNow;
        public override Characteristics MyCharacteristics { get { return (base.MyCharacteristics | Characteristics.Fly) & ~Characteristics.DamageSlows; } }
        public virtual void CoreVampSkills()
        {
            SetSkill(SkillName.MagicResist, 99.5, 130.0);
            SetSkill(SkillName.Wrestling, m_WrestlingMin, m_WrestlingMax);
            SetSkill(SkillName.Anatomy, m_AnatomyMin, m_AnatomyMax);
            SetSkill(SkillName.DetectHidden, 100);
            //SetSkill(SkillName.Meditation, 100);    // for hypnosis (update, passive regen seems ok)
        }

        public override bool IsScaryToPets { get { return true; } }
        public override bool IsScaryCondition() { return Core.RuleSets.VampiresAreScary(); }
        public override bool AlwaysMurderer { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        // this is redundant with IsScaryToPets
        //public override 	AuraType 	MyAura{ get{ return AuraType.Fear; } }
        //public override 	int 		AuraRange{ get{ return 10; } }
        //public override		TimeSpan	NextAuraDelay{ get{ return TimeSpan.FromSeconds( 2.0 ); } }

        public override void InitBody()
        {
            if (Female = Utility.RandomBool())
            {
                Body = 0x191;
                //"Countess" "Areil"
                Name = NameList.RandomName("vampire_femaletitle") + " " + NameList.RandomName("vampire_female");
                Title = "the vampiress";
            }
            else
            {
                Body = 0x190;
                // Lord blah
                Name = NameList.RandomName("vampire_maletitle") + " " + NameList.RandomName("vampire_male");
                Title = "the vampire";
            }
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);

            if (this.Female)
            {

                Item hair = new Item(0x203C);
                Item dress = new PlainDress(0x1);
                //5% chance to drop black dress
                if (Utility.RandomDouble() < 0.95)
                    dress.LootType = LootType.Newbied;

                if (Utility.RandomMinMax(0, 100) <= 20) //20% chance to have black hair
                {
                    hair.Hue = 0x1;
                }
                else
                    hair.Hue = Utility.RandomHairHue();

                hair.Layer = Layer.Hair;
                AddItem(hair);
                AddItem(dress);
            }
            else
            {
                Item hair2 = new Item(Utility.RandomList(0x203C, 0x203B));
                Item pants = new LongPants(0x1);
                Item shirt = new FancyShirt();
                hair2.Hue = Utility.RandomHairHue();
                hair2.Layer = Layer.Hair;
                AddItem(hair2);
                //5% chance for black clothes
                if (Utility.RandomDouble() < 0.95)
                    shirt.LootType = LootType.Newbied;
                if (Utility.RandomDouble() < 0.95)
                    pants.LootType = LootType.Newbied;
                AddItem(pants);
                AddItem(shirt);
            }

            Item necklace = new GoldNecklace();
            AddItem(necklace);
            Item ring = new GoldRing();
            AddItem(ring);
            Item bracelet = new GoldBracelet();
            AddItem(bracelet);

            Item boots = new Sandals(0x1);
            boots.LootType = LootType.Newbied; //no dropping the black sandals.
            AddItem(boots);
        }

        #region Transformation effect classes
        public class BatToHuman : TransformEffect
        {
            public override void Transform(Mobile m)
            {
                Map fromMap = m.Map;
                Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y, m.Z + 4), fromMap, 0x3728, 13, 0x21, 0);
                Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y, m.Z), fromMap, 0x3728, 13, 0x21, 0);
                Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y, m.Z - 4), fromMap, 0x3728, 13, 0x21, 0);
                Effects.SendLocationEffect(new Point3D(m.X, m.Y + 1, m.Z + 4), fromMap, 0x3728, 13, 0x21, 0);
                Effects.SendLocationEffect(new Point3D(m.X, m.Y + 1, m.Z), fromMap, 0x3728, 13, 0x21, 0);
                Effects.SendLocationEffect(new Point3D(m.X, m.Y + 1, m.Z - 4), fromMap, 0x3728, 13, 0x21, 0);
                Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y + 1, m.Z + 11), fromMap, 0x3728, 13, 0x21, 0);
                Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y + 1, m.Z + 7), fromMap, 0x3728, 13, 0x21, 0);
                Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y + 1, m.Z + 3), fromMap, 0x3728, 13, 0x21, 0);
                Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y + 1, m.Z - 1), fromMap, 0x3728, 13, 0x21, 0);
                Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y, m.Z + 4), fromMap, 0x3728, 13, 0x21, 0);
            }
        }
        public class HumanToBat : TransformEffect
        {
            public override void Transform(Mobile m)
            {
                Map fromMap = m.Map;
                Effects.SendLocationEffect(new Point3D(m.X, m.Y, m.Z - 4), fromMap, 0x3728, 13, 0x21, 0);
            }
        }
        #endregion

        public bool Hypnotize(Mobile m)
        {
            // must have enough mana to peace/hypnotize a mobile
            if (this.Mana >= m_HypnotizeCost)
            {
                this.Mana -= m_HypnotizeCost;

                m.Warmode = false;
                m.Combatant = null;

                if (m is BaseCreature && !((BaseCreature)m).BardPacified && !((BaseCreature)m).Uncalmable)
                    ((BaseCreature)m).Pacify(this, DateTime.UtcNow + TimeSpan.FromSeconds(30.0));

                m.SendMessage("You feel a calming peace wash over you.");
                return true;
            }
            else
                DebugSay(DebugFlags.AI, "I have no mana, so I cannot hypnotize");

            return false;
        }

        public override void CheckWeaponImmunity(BaseWeapon wep, int damagein, out int damage)
        {
            // BaseCreature now has a mobile override which applies to any weapon wielded
            bool weaponSilver = Combatant is BaseCreature bc && bc.Slayer == SlayerName.Silver;
            // PlayerMobiles can drink Holy Water which gives them the silver-effect
            weaponSilver |= Combatant is PlayerMobile pm && HolyWater.UnderEffect(pm);
            // basic silver weapon
            weaponSilver |= wep.Slayer == SlayerName.Silver;
            // if not a silver or holy weapon, reduce damage to 25%
            if (weaponSilver == false && wep.HolyBlade == false)
                damage = (int)(damagein * .25);
            else
                // silver bonus given as usual slayer bonus in baseWeapon
                damage = damagein;
        }
        public override void Damage(int amount, Mobile from, object source_weapon)
        {
            if (source_weapon is HolyHandGrenade)
            {
                // We will 'confuse' the vamp so it can't do special moves for a few seconds..
                VampireAI.Confused.Remember(this, 5);

                // reduce damage to reasonable amount
                amount = Math.Min(amount, Utility.RandomMinMax((int)(HitsMax * 0.2), (int)(HitsMax * 0.3)));
            }
            base.Damage(amount, from, source_weapon);
        }
        public override void OnThink()
        {
            if (this.AIObject != null)
                if (VampireAI.CheckNight(this) && this.AIObject.Action != ActionType.Chase) //its nighttime - be a vamp unless we're chasing
                {
                    DebugSay(DebugFlags.AI, "It's nighttime! Be a vamp...");
                    // turn back to human if we are fighting (but not fleeing)
                    if (BatForm && this.AIObject.Action != ActionType.Flee)
                    {
                        DoTransform(this, (this.Female) ? 0x191 : 0x190, this.Hue, toHumanForm);
                        this.FightMode = FightMode.All | FightMode.Closest;     // suck blood from random creatures if wounded
                        CanFlyOver = false;
                    }
                }
                else // it's daytime
                {
                    DebugSay(DebugFlags.AI, "It's daytime! Be a bat...");
                    // return to bat form if we are not one and not fighting
                    if (!BatForm && this.AIObject.Action != ActionType.Combat)
                    {
                        DoTransform(this, BatGraphic(), toBatForm);      // become a bat
                        this.FightMode = FightMode.Aggressor;
                    }
                }

            base.OnThink();
        }
        public int BatGraphic()
        {
            if (Core.T2A)
                return 74; //  (0x4A) Imp (T2A doesn't have a bat graphic)
            else
                return 317; // OSI bat

            return 0;
        }
        /*public override bool CanTransform() 
		{	// can't change more often that once every 30 seconds
			if (DateTime.UtcNow - m_LastTransform > TimeSpan.FromSeconds(30))
				return true;
			else
				return false;
		}
		public override void LastTransform() { m_LastTransform = DateTime.UtcNow; }*/

        private class DeathTimer : Timer
        {
            private Mobile owner;

            public DeathTimer(Mobile target)
                : base(TimeSpan.FromSeconds(0.70))
            {
                owner = target;

                Priority = TimerPriority.FiftyMS;
            }

            protected override void OnTick()
            {
                if (owner != null)
                {
                    Item ashes = new Item(0xF8F);
                    ashes.Name = "vampire ashes";
                    ashes.Hue = 0x3B2;  // light gray
                    ashes.Weight = 1;
                    ashes.MoveToWorld(owner.Location, owner.Map);
                    owner.Delete();

                }
            }
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            //if vamp scores a melee hit do our lifedrain/plus small stamina drain
            base.OnGaveMeleeAttack(defender);

            defender.PlaySound(0x1F1);
            defender.FixedEffect(0x374A, 10, 16, 1200, 0);
            this.FixedEffect(0x374A, 10, 16, 1200, 0);

            int life = LifeDrain;
            if (defender.BlockDamage == false)  // special GM mode (to not take damage)
                defender.Hits -= life;
            // We don't want to call defender.Damage() here as it will unfreeze defender, which is sort of the point of being a vampire
            //  (vamps para you with either a stun punch (unarmed vamps) or hypnotize (armed vamps)
            //  Instead we invoke the mobiles show damage directly so we can see what's going on
            defender.ShowDamage(life, this);
            this.Hits += life;
            defender.SendMessage("You feel the life drain out of you!");
            int stam = StamDrain;
            if (defender.BlockDamage == false) // special GM mode (to not take damage)
                defender.Stam -= stam;
            this.Stam += stam;

            if (defender.BlockDamage == true) // special GM mode (to not take damage)
                if (defender.Player)
                    DebugSay(DebugFlags.Player, "I am immune!");
                else
                    DebugSay(DebugFlags.AI, "I am immune!");

        }

        public override bool OnBeforeDeath()
        {
            if (this.Female)
            {
                this.Body = 0x191;
            }
            else
            {
                this.Body = 0x190;
            }

            if (this.BatForm && VampireAI.CheckNight(this) == false) //its daytime dont drop anything
            {
                // make sure no clothes or weapons drop
                NewbieAllLayers();
                this.AIObject.Deactivate();
                Effects.PlaySound(this, this.Map, 648);
                this.FixedParticles(0x3709, 10, 30, 5052, EffectLayer.LeftFoot);
                this.PlaySound(0x208);
                DeathTimer t = new DeathTimer(this);
                t.Start();
            }

            return base.OnBeforeDeath();
        }

        public Vampire(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AllShards)
            {
                if (Spawning)
                {
                    // No at spawn loot
                }
                else
                {
                    //PackGold(170, 220); //add gold if its daytime
                    //PackMagicEquipment(2, 3, 0.60, 0.60);
                    //PackMagicEquipment(2, 3, 0.25, 0.25);
                    PackGold(Utility.RandomMinMax(1200, 1400) / 2); // 1/2 dragon gold

                    BaseWeapon weapon = PackSlayerWeapon(CoreAI.SlayerWeaponDropRate);
                    if (weapon != null)
                    {
                        LogHelper Logger = new LogHelper("SlayerWeapon.log", false, true);
                        Logger.Log(LogType.Item, weapon);
                        Logger.Finish();
                    }

                    Item blood = new BloodVial();
                    blood.Name = "blood of " + this.Name;
                    PackItem(blood);

                    PackHolyStuff();
                }
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
                {
                    // ai special
                }
            }
        }
        public void PackHolyStuff()
        {
            if (Utility.Chance(0.5))
                PackItem(new HolyWater());
            else if (Utility.Chance(0.5))
                PackItem(new HolyHandGrenade());
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
                                  //writer.Write(BatForm); // version 1
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    // remove batform bool from serialization
                    break;
                case 0:
                    bool dmy = reader.ReadBool();
                    break;
            }

        }
    }
}