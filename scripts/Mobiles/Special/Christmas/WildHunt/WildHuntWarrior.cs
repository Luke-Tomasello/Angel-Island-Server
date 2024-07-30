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

/* Scripts/Mobiles/Special/Christmas/WildHunt/WildHuntWarrior.cs
 * ChangeLog
 *  1/1/24, Yoar
 *		Initial Version.
 */

using Server.Items;

namespace Server.Mobiles.WildHunt
{
    public interface IResurrected
    {
        bool Resurrected { get; set; }
    }

    [Rally]
    public class WildHuntWarrior : BaseCreature, IResurrected
    {
        private bool m_SteedKilled;
        private bool m_Resurrected;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SteedKilled
        {
            get { return m_SteedKilled; }
            set { m_SteedKilled = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Resurrected
        {
            get { return m_Resurrected; }
            set { m_Resurrected = value; }
        }

        [Constructable]
        public WildHuntWarrior()
            : this(false)
        {
        }

        [Constructable]
        public WildHuntWarrior(bool mounted)
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            BaseSoundID = 0x17E;

            SetStr(350);
            SetDex(100);
            SetInt(100);

            SetHits(350);

            SetDamage(10, 12);

            SetSkill(SkillName.Anatomy, 100.0);
            SetSkill(SkillName.MagicResist, 100.0);
            SetSkill(SkillName.Tactics, 100.0);

            Fame = (mounted ? 9000 : 6000);
            Karma = (mounted ? -9000 : -6000);

            VirtualArmor = 30;

            InitBody();
            InitOutfit();

            if (mounted)
            {
                BaseMount horse = new WildHuntHorse();
                horse.Tamable = false;
                horse.Rider = this;
            }

            WildHunt.Register(this);
        }

        public override bool ShowFameTitle { get { return false; } }
        public override bool AlwaysAttackable { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Regular; } }

        #region Outfit

        public override void InitBody()
        {
            if (Female = Utility.RandomBool())
                Body = 0x191;
            else
                Body = 0x190;

            Hue = 0x4001;

            Name = WildHunt.RandomName(Female);
            Title = WildHunt.RandomTitle();
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);

            AddItem(WildHunt.SetImmovable(WildHunt.SetHue(0x4001, new Cloak())));
            AddItem(WildHunt.SetImmovable(WildHunt.SetHue(0x4001, new Robe())));

            AddItem(WildHunt.SetImmovable(WildHunt.SetHue(0x4001, new NorseHelm())));

            switch (Utility.Random(6))
            {
                case 0:
                    {
                        SetSkill(SkillName.Parry, 100.0);
                        SetSkill(SkillName.Swords, 100.0);

                        AddItem(WildHunt.SetImmovable(WildHunt.Imbue(2, new VikingSword())));
                        AddItem(WildHunt.SetImmovable(WildHunt.Imbue(2, WildHunt.RandomShield())));

                        break;
                    }
                case 1:
                    {
                        SetSkill(SkillName.Parry, 100.0);
                        SetSkill(SkillName.Swords, 100.0);

#if false
                        AddItem(WildHunt.SetImmovable(WildHunt.Imbue(2, WildHunt.SetLayer(Layer.OneHanded, new BattleAxe()))));
                        AddItem(WildHunt.SetImmovable(WildHunt.Imbue(2, WildHunt.RandomShield())));
#else
                        AddItem(WildHunt.SetImmovable(WildHunt.Imbue(2, new BattleAxe())));
#endif

                        break;
                    }
                case 2:
                    {
                        SetSkill(SkillName.Fencing, 100.0);

                        AddItem(WildHunt.SetImmovable(WildHunt.Imbue(2, new Spear())));

                        break;
                    }
                case 3:
                    {
                        SetSkill(SkillName.Lumberjacking, 100.0);
                        SetSkill(SkillName.Swords, 100.0);

                        AddItem(WildHunt.SetImmovable(WildHunt.Imbue(2, new LargeBattleAxe())));

                        break;
                    }
                case 4:
                    {
                        SetSkill(SkillName.Macing, 100.0);
                        SetSkill(SkillName.Parry, 100.0);

                        AddItem(WildHunt.SetImmovable(WildHunt.Imbue(2, new WarAxe())));
                        AddItem(WildHunt.SetImmovable(WildHunt.Imbue(2, WildHunt.RandomShield())));

                        break;
                    }
                case 5:
                    {
                        AI = AIType.AI_Archer;

                        SetSkill(SkillName.Archery, 100.0);

                        AddItem(WildHunt.SetImmovable(WildHunt.Imbue(2, WildHunt.RandomBow())));

                        break;
                    }
            }

            HairItemID = WildHunt.RandomHair();
            HairHue = 0x4001;

            if (!Female)
            {
                FacialHairItemID = WildHunt.RandomFacialHair();
                FacialHairHue = 0x4001;
            }
        }

        #endregion

        public override bool DeleteCorpseOnDeath { get { return true; } }
        public override bool DropCorpseItems { get { return true; } }

        public override bool OnBeforeDeath()
        {
            BaseMount mount = Mount as BaseMount;

            if (mount == null)
            {
                if (!base.OnBeforeDeath())
                    return false;

                Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);

                if (!m_Resurrected)
                    new FallenSoldier().MoveToWorld(Location, Map);

                return true;
            }
            else
            {
                m_SteedKilled = true;

                mount.Rider = null;
                mount.Kill();

                Hits = HitsMax;

                return false;
            }
        }

        public override void GenerateLoot()
        {
            if (Spawning)
            {
            }
            else
            {
                if (m_SteedKilled)
                {
                    PackGold(600);

                    PackMagicEquipment(1, 2, 0.60, 0.60);
                    PackMagicEquipment(2, 3, 0.25, 0.25);
                }
                else
                {
                    PackGold(300);

                    PackMagicEquipment(1, 2, 0.40, 0.40);
                }
            }
        }

        public override bool CheckIdle()
        {
            return false; // we're never idle
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            WildHunt.Unregister(this);
        }

        public WildHuntWarrior(Serial serial)
            : base(serial)
        {
            WildHunt.Register(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((bool)m_Resurrected);

            writer.Write((bool)m_SteedKilled);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Resurrected = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        m_SteedKilled = reader.ReadBool();
                        break;
                    }
            }
        }
    }
}