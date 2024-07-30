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

/* Scripts/Mobiles/Wind Elders/LordGuardian.cs
 * ChangeLog
 *	1/7/09, Adam
 *		- Remove old bandage code as we use new 'human bandages'
 *		- Add magery and EvalInt so I can cast, lol (needed for new hybrid ai)
 *	1/3/09, Adam
 *		Update to new AI .. give potions, bandages and a pouch to trap
 *  12/03/06 Taran Kain
 *      Set Female to false - no tranny Lord Guardians!
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 *	12/11/04, Pix
 *		Changed ControlSlots for IOBF.
 *  11/16/04, Froste
 *      Changed IOBAlignment to Council
 *  11/10/04, Froste
 *      Implemented new random IOB drop system and changed drop change to 12%
 *	11/05/04, Pigpen
 *		Made changes for Implementation of IOBSystem. Changes include:
 *		Removed IsEnemy and Aggressive Action Checks. These are now handled in BaseCreature.cs
 *		Set Creature IOBAlignment to Undead.
 *	9/23/04 smerX
 *		Enhanced speech
 *	8/4/04, Adam
 *		Remove Poison Immunity
 *	8/3/04, Adam
 *		Update Stats, Skills, and Damage/Resist values to be more consistent.
 *	7/21/04, mith
 *		IsEnemy() and AggressiveAction() code added to support Brethren property of BloodDrenchedBandana.
 *		OnMovement() modified to streamline the speech checking.
 *	5/23/04 smerX
 *		Enabled healing
 *		kilt 7% drop
 *		made mobile faster
 *	5/14/04, mith
 *		modified the way we do the warning speech.
 *	5/12/04, mith
 *		Fixed sandals to 7% drop
 *		Fixed hue
 *	5/9/04, mith
 *		Added speech.
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;
using System;

namespace Server.Mobiles
{
    [CorpseName("a corpse of the Lord Guardian")]
    public class LordGuardian : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Council }); } }

        private TimeSpan m_SpeechDelay = TimeSpan.FromSeconds(10.0); // time between speech
        public DateTime m_NextSpeechTime;

        [Constructable]
        public LordGuardian()
            : base(AIType.AI_Hybrid, FightMode.All | FightMode.Weakest, 10, 1, 0.15, 0.25)
        {
            BardImmune = true;
            FightStyle = FightStyle.Melee | FightStyle.Magic | FightStyle.Smart | FightStyle.Bless | FightStyle.Curse;
            UsesHumanWeapons = false;
            UsesBandages = true;
            UsesPotions = true;
            CanRun = true;
            CanReveal = true; // magic and smart


            SpeechHue = Utility.RandomSpeechHue();
            Name = "Lord Guardian";
            Female = false;
            Body = 0x190;
            Hue = 0x83F4;
            IOBAlignment = IOBAlignment.Council;
            ControlSlots = 6;

            BloodDrenchedBandana bandana = new BloodDrenchedBandana();
            bandana.LootType = LootType.Newbied;
            AddItem(bandana);

            Kilt kilt = new Kilt(0x1); //black kilt
            if (Utility.RandomDouble() <= 0.93)
                kilt.LootType = LootType.Newbied;
            AddItem(kilt);

            Sandals sandals = new Sandals(0x66C);
            if (Utility.RandomDouble() <= 0.93)
                sandals.LootType = LootType.Newbied;
            AddItem(sandals);

            SilverRing ring = new SilverRing();
            ring.Name = "To my darling Adam";
            if (Utility.RandomDouble() < 0.95)
                ring.LootType = LootType.Newbied;
            AddItem(ring);

            ChainChest tunic = new ChainChest();
            tunic.Resource = CraftResource.Gold;
            AddItem(tunic);

            ChainLegs legs = new ChainLegs();
            legs.Resource = CraftResource.Gold;
            AddItem(legs);

            RingmailArms arms = new RingmailArms();
            arms.Resource = CraftResource.Gold;
            AddItem(arms);

            GuardianKatana sword = new GuardianKatana();
            sword.Quality = WeaponQuality.Exceptional;
            sword.LootType = LootType.Newbied;
            if (Utility.RandomBool())
                sword.Poison = Poison.Deadly;
            else
                sword.Poison = Poison.Greater;
            sword.PoisonCharges = 30;
            AddItem(sword);

            PonyTail hair = new PonyTail();
            hair.Hue = 0x1BC;
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            SetStr(375, 400);
            SetDex(100, 125);
            SetInt(150, 175);

            SetDamage(8, 10);

            SetSkill(SkillName.EvalInt, 100.0, 110.0);
            SetSkill(SkillName.Magery, 100.0, 110.0);
            SetSkill(SkillName.Swords, 100.0, 125.0);
            SetSkill(SkillName.Tactics, 100.0, 125.0);
            SetSkill(SkillName.Anatomy, 100.0, 125.0);
            SetSkill(SkillName.Poisoning, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 83.5, 92.5);

            Fame = 5000;
            Karma = -5000;

            VirtualArmor = 40;

            m_NextSpeechTime = DateTime.UtcNow;

            PackItem(new Bandage(Utility.RandomMinMax(VirtualArmor, VirtualArmor * 2)), lootType: LootType.UnStealable);
            PackStrongPotions(6, 12);
            PackItem(new Pouch(), lootType: LootType.UnStealable);
        }

        public override bool AlwaysMurderer { get { return true; } }
        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : false; } }
        public override Poison PoisonImmune { get { return null; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 5 : 0; } }

        public override bool Uncalmable
        {
            get
            {
                if (Hits > 1)
                    Say("Peace, is it? I'll give you peace!");

                return BardImmune;
            }
        }

        public LordGuardian(Serial serial)
            : base(serial)
        {
        }
        private void AICrossShardLoot()
        {   // Angel Island creatures that exist on multiple shards use common loot
            //  i.e., that loot doesn't change across different shard configs
            PackGold(750, 800);

            PackItem(new TheGuardianBook());

            // Froste: 12% random IOB drop
            if (0.12 > Utility.RandomDouble())
            {
                Item iob = Loot.RandomIOB();
                PackItem(iob);
            }

            if (IOBRegions.GetIOBStronghold(this) == IOBAlignment)
            {
                // 30% boost to gold
                PackGold(base.GetGold() / 3);
            }
        }
        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {   // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                AICrossShardLoot();
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
                        AICrossShardLoot();
                    }
                }
                else
                {   // Standard RunUO
                    // ai special
                }
            }
        }

        public override bool OnBeforeDeath()
        {
            this.Say(true, "Kal Vas Xen Corp");
            this.Say(true, "Bring forth the council!");

            return base.OnBeforeDeath();
        }

        public override void Damage(int amount, Mobile from, object source_weapon)
        {
            Mobile combatant = this.Combatant;

            if (combatant != null && combatant.Map == this.Map && combatant.InRange(this, 8))
            {
                if (this.Hits <= 200)
                {
                    if (Utility.RandomBool())
                    {
                        this.Say(true, "Wretched Dog!");
                        m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;
                    }
                }
                else if (this.Hits <= 100)
                {
                    if (Utility.RandomBool())
                    {
                        this.Say(true, "Vile Heathen!");
                        m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;
                    }
                }
            }

            base.Damage(amount, from, source_weapon);
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {

            if (m.Player && m.Alive && m.InRange(this, 10) && m.AccessLevel == AccessLevel.Player && DateTime.UtcNow >= m_NextSpeechTime && Combatant == null)
            {
                Item item = m.FindItemOnLayer(Layer.Helm);

                if (this.InLOS(m) && this.CanSee(m))
                {
                    if (item is BloodDrenchedBandana)
                    {
                        this.Say("Leave these halls before it is too late!");
                        m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;
                    }
                    else
                    {
                        this.Say("Where is your bandana, friend?");
                        m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;
                    }
                }

            }

            base.OnMovement(m, oldLocation);
        }

        public override void OnThink()
        {
            if (DateTime.UtcNow >= m_NextSpeechTime)
            {
                Mobile combatant = this.Combatant;

                if (combatant != null && combatant.Map == this.Map && combatant.InRange(this, 7) && combatant.InLOS(this))
                {
                    int phrase = Utility.Random(4);

                    switch (phrase)
                    {
                        case 0: this.Say(true, "Yet another knuckle dragging heathen to deal with!"); break;
                        case 1: this.Say(true, "You must leave our sacred home vile heathen!"); break;
                        case 2: this.Say(true, "You must leave now!"); break;
                        case 3: this.Say(true, "Ah! You do bleed badly!"); break;
                    }

                    m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;
                }

                base.OnThink();
            }
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