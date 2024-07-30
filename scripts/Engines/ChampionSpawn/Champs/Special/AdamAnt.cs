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

/* Scripts\Engines\ChampionSpawn\Champs\Special\AdamAnt.cs
 * ChangeLog
 *	9/20/21, Adam 
 *	First time checkin
 */

using Server.Engines.Alignment;
using Server.Engines.ChampionSpawn;
using Server.Engines.IOBSystem;
using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [CorpseName("a corpse of the Lord Guardian")]
    public class AdamAnt : BaseChampion
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Council }); } }

        private TimeSpan m_SpeechDelay = TimeSpan.FromSeconds(10.0); // time between speech
        public DateTime m_NextSpeechTime;

        [Constructable]
        public AdamAnt()
            : base(AIType.AI_Hybrid, FightMode.All | FightMode.Weakest, 0.175, 0.350)
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
            m_NextSpeechTime = DateTime.UtcNow;

            SetStr(305, 425);
            SetDex(72, 150);
            SetInt(505, 750);

            SetHits(4200);
            SetStam(102, 300);

            SetDamage(25, 35);

            SetSkill(SkillName.EvalInt, 100.0, 110.0);
            SetSkill(SkillName.Magery, 100.0, 110.0);
            SetSkill(SkillName.Swords, 100.0, 125.0);
            SetSkill(SkillName.Tactics, 100.0, 125.0);
            SetSkill(SkillName.Anatomy, 100.0, 125.0);
            SetSkill(SkillName.Poisoning, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 83.5, 92.5);

            Fame = 22500;
            Karma = -22500;

            VirtualArmor = 70;

            InitBody();
            InitOutfit();

            PackItem(new Bandage(Utility.RandomMinMax(VirtualArmor, VirtualArmor * 2)), lootType: LootType.UnStealable);
            PackStrongPotions(6, 12);
            PackItem(new Pouch(), lootType: LootType.UnStealable);
        }
        public override void InitBody()
        {
            PonyTail hair = new PonyTail();
            hair.Hue = 0x1BC;
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);
        }
        public override void InitOutfit()
        {
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
        }
        public override ChampionSkullType SkullType { get { return ChampionSkullType.None; } }
        public override bool AlwaysMurderer { get { return true; } }
        public override bool CanRummageCorpses { get { return false; } }
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

        public AdamAnt(Serial serial)
            : base(serial)
        {
        }
        #region DraggingMitigation
        public override List<Mobile> GetDraggingMitigationHelpers()
        {
            List<Mobile> helpers = new List<Mobile>();
            // okay, now we take action against this bothersome individual!
            for (int ix = 0; ix < 2; ix++)
            {   // these will be out helpers
                switch (Utility.Random(4))
                {
                    case 0:
                    case 1:
                    case 2:
                        helpers.Add(new CouncilMember());
                        break;
                    default:
                        helpers.Add(new CouncilElder());
                        break;
                }
            }
            return helpers;
        }
        #endregion DraggingMitigation

        public override void GenerateLoot()
        {
            if (IsChampion)
            {
                if (Core.RuleSets.AngelIslandRules())
                {
                    // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
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
        }

        public override bool OnBeforeDeath()
        {
            this.Say(true, "Kal Vas Xen Corp");
            this.Say(true, "Bring forth the council!");

            return base.OnBeforeDeath();
        }
        private bool m_SpawnedGuardians = false;
        private LadyGuardian m_Jadey = null;
        public void CheckGuardians()
        {
            Point3D point = this.Location;
            Mobile combatant = this.Combatant;
            if (!m_SpawnedGuardians)
            {
                if (this.Hits <= 400)
                {
                    DoMutate();
                    m_Jadey = new LadyGuardian();

                    ((BaseCreature)m_Jadey).Team = this.Team;

                    m_Jadey.RawStr = Utility.Random(90, 110);

                    if (this.Combatant != null && this.Combatant is BaseCreature && (this.Combatant as BaseCreature).ControlMaster != null)
                    {   // target the pets master.
                        point = (this.Combatant as BaseCreature).ControlMaster.Location;
                        combatant = (this.Combatant as BaseCreature).ControlMaster;
                    } // else we just target the Combatant

                    Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(Tick), new object[] { "Come forth Lady Guardian!", null });
                    Timer.DelayCall(TimeSpan.FromSeconds(2.0), new TimerStateCallback(Tick), new object[] { "You shall never defeat me as long as I have my Lady Guardian!", null });

                    m_Jadey.MoveToWorld(point, this.Map);

                    m_Jadey.Combatant = combatant;

                    m_SpawnedGuardians = true;
                }
            }
            else if (m_Jadey != null && m_Jadey.Deleted)
            {
                m_Jadey = null;
            }
        }

        private void Tick(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] != null && aState[0] is string)
                this.Say(true, aState[0] as string);
        }
        private void DoMutate()
        {
            HoodedShroudOfShadows shroud = new HoodedShroudOfShadows();
            shroud.Hue = 1554;
            shroud.Name = "Robe of the Lord Guardian";
            shroud.LootType = LootType.Regular;
            AddItem(shroud);
            if (AIObject != null)
                AIObject.EquipWeapon();

            this.FixedParticles(0x3709, 10, 30, 5052, 0, 0, EffectLayer.LeftFoot);
            this.PlaySound(0x208);
        }
        public override void Damage(int amount, Mobile from, object source_weapon)
        {
            Mobile combatant = this.Combatant;

            if (combatant != null && combatant.Map == this.Map && combatant.InRange(this, 8))
            {
                CheckGuardians();

                if (m_Jadey != null)    // while our guardian is up, we don't take damage.
                    amount = 1;

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