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

/* Scripts\Engines\ChampionSpawn\Champs\Special\QueenZhah.cs
 * ChangeLog
 *	6/2/2023, Adam 
 *	First time checkin
 */

using Server.Engines.Alignment;
using Server.Engines.ChampionSpawn;
using Server.Engines.IOBSystem;
using Server.Items;
using System;

namespace Server.Mobiles
{
    [CorpseName("the corpse of Queen Zhah")]
    public class QueenZhah : BaseChampion
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentType.None; } }

        private TimeSpan m_SpeechDelay = TimeSpan.FromSeconds(10.0); // time between speech
        public DateTime m_NextSpeechTime;

        [Constructable]
        public QueenZhah()
            : base(AIType.AI_Hybrid, FightMode.All | FightMode.Weakest, 0.175, 0.350)
        {
            BardImmune = true;
            FightStyle = FightStyle.Melee | FightStyle.Magic | FightStyle.Smart | FightStyle.Bless | FightStyle.Curse;
            UsesHumanWeapons = false;
            UsesBandages = true;
            UsesPotions = true;
            CanRun = true;
            CanReveal = true; // magic and smart
            Tamable = false;

            SpeechHue = Utility.RandomSpeechHue();
            Name = "Queen Zhah";
            Female = true;
            Body = 4;   // gargoyle
            BaseSoundID = 372;
            Hue = 0xB8f;
            CanRun = true;
            CanReveal = true;
            m_NextSpeechTime = DateTime.UtcNow;

            SetStr(305, 425);
            SetDex(72, 150);
            SetInt(505, 750);

            SetHits(4200);
            SetStam(102, 300);

            SetDamage(25, 35);

            SetSkill(SkillName.EvalInt, 100.0, 110.0);
            SetSkill(SkillName.Magery, 100.0, 110.0);
            SetSkill(SkillName.Wrestling, 100.0, 125.0);
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

        }
        public override void InitOutfit()
        {

        }
        public override ChampionSkullType SkullType { get { return ChampionSkullType.None; } }
        public override bool AlwaysMurderer { get { return true; } }
        public override bool CanRummageCorpses { get { return false; } }
        public override Poison PoisonImmune { get { return null; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 5 : 0; } }
        public override AuraType MyAura { get { return Utility.RandomEnumValue<AuraType>(); } }
        public override int AuraRange { get { return 5; } }
        public override int AuraMin { get { return 5; } }
        public override int AuraMax { get { return 10; } }

        public override bool Uncalmable
        {
            get
            {
                if (Hits > 1 && Utility.Chance(0.1))
                    // The wingless ones cannot speak, and lack the intelligence of the winged ones
                    this.Say(false, "Anvolde lem ansa l�k, esh anten sk�tas de volde lem.");

                return BardImmune;
            }
        }

        public QueenZhah(Serial serial)
            : base(serial)
        {
        }
        #region DraggingMitigation
#if false
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
#endif
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
                            Scepter scepter = new Scepter();
                            scepter.Name = "the scepter of Queen Zhah";
                            scepter.LootType = LootType.UnStealable;
                            PackItem(scepter);
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
            // The wingless ones cannot speak, and lack the intelligence of the winged ones
            this.Say(false, "Anvolde lem ansa l�k, esh anten sk�tas de volde lem.");

            return base.OnBeforeDeath();
        }
        private bool m_SpawnedGuardians = false;
        private BoneDemon m_Guardian = null;
        public void CheckGuardians()
        {
            Point3D point = this.Location;
            Mobile combatant = this.Combatant;
            if (!m_SpawnedGuardians)
            {
                if (this.Hits <= 400)
                {
                    DoMutate();
                    m_Guardian = new BoneDemon();

                    ((BaseCreature)m_Guardian).Team = this.Team;

                    //m_Guardian.RawStr = Utility.Random(90, 110);

                    if (this.Combatant != null && this.Combatant is BaseCreature && (this.Combatant as BaseCreature).ControlMaster != null)
                    {   // target the pets master.
                        point = (this.Combatant as BaseCreature).ControlMaster.Location;
                        combatant = (this.Combatant as BaseCreature).ControlMaster;
                    } // else we just target the Combatant

                    // Here lie those that had no names.
                    Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(Tick), new object[] { "Teresta sit lem antende n�m.", null });

                    // Within the Codex is written the one right and true answer to any problem.
                    Timer.DelayCall(TimeSpan.FromSeconds(2.0), new TimerStateCallback(Tick), new object[] { "�nte k�dex skr�le pr� ben esh ver res qu� quae.", null });

                    m_Guardian.MoveToWorld(point, this.Map);

                    m_Guardian.Combatant = combatant;

                    m_SpawnedGuardians = true;
                }
            }
            else if (m_Guardian != null && m_Guardian.Deleted)
            {
                m_Guardian = null;
            }
        }

        private void Tick(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] != null && aState[0] is string)
                this.Say(false, aState[0] as string);
        }
        private void DoMutate()
        {
            this.FixedParticles(0x3709, 10, 30, 5052, 0, 0, EffectLayer.LeftFoot);
            this.PlaySound(0x208);
            Body = 40;  // balron
            BaseSoundID = 357;
            Hue = 0xB8f;
        }
        public override void Damage(int amount, Mobile from, object source_weapon)
        {
            Mobile combatant = this.Combatant;

            if (combatant != null && combatant.Map == this.Map && combatant.InRange(this, 8))
            {
                CheckGuardians();

                if (m_Guardian != null)    // while our guardian is up, we don't take damage.
                    amount = 1;

                if (this.Hits <= 200)
                {
                    if (Utility.RandomBool())
                    {
                        //this.Say(true, "Wretched Dog!");
                        m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;
                    }
                }
                else if (this.Hits <= 100)
                {
                    if (Utility.RandomBool())
                    {
                        //this.Say(true, "Vile Heathen!");
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
                    /*if (item is BloodDrenchedBandana)
                    {
                        this.Say("Leave these halls before it is too late!");
                        m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;
                    }
                    else
                    {
                        this.Say("Where is your bandana, friend?");
                        m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;
                    }*/
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

                    /*switch (phrase)
                    {
                        case 0: this.Say(true, "Yet another knuckle dragging heathen to deal with!"); break;
                        case 1: this.Say(true, "You must leave our sacred home vile heathen!"); break;
                        case 2: this.Say(true, "You must leave now!"); break;
                        case 3: this.Say(true, "Ah! You do bleed badly!"); break;
                    }*/

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