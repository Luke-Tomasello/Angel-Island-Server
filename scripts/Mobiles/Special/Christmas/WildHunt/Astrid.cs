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

/* Scripts/Mobiles/Special/Christmas/WildHunt/Astrid.cs
 * ChangeLog
 *  1/1/24, Yoar
 *		Initial Version.
 */

using Server.Items;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Mobiles.WildHunt
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RallyAttribute : Attribute
    {
        public static bool CanRally(Type type)
        {
            return (type.GetCustomAttributes(typeof(RallyAttribute), true).Length != 0);
        }

        public RallyAttribute()
        {
        }
    }

    public class Astrid : BaseCreature, ICaptain
    {
        private bool m_SteedKilled;
        private ArmyState m_Army;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SteedKilled
        {
            get { return m_SteedKilled; }
            set { m_SteedKilled = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ArmyState Army
        {
            get { return m_Army; }
            set { }
        }

        [Constructable]
        public Astrid()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            BaseSoundID = 0x17E;

            Name = "Astrid";
            Title = "the Shieldmaiden";

            SetStr(800);
            SetDex(200);
            SetInt(100);

            SetHits(1200);

            SetDamage(11, 13);

            SetSkill(SkillName.Anatomy, 100.0);
            SetSkill(SkillName.Fencing, 110.0);
            SetSkill(SkillName.MagicResist, 100.0);
            SetSkill(SkillName.Tactics, 100.0);

            Fame = 12000;
            Karma = -12000;

            VirtualArmor = 46;

            InitBody();
            InitOutfit();

            BaseMount charger = new WildHuntCharger();
            charger.Tamable = false;
            charger.Rider = this;

            m_Army = new ArmyState(this, AstridsArmy.Instance);

            WildHunt.Register(this);
        }

        public override bool ShowFameTitle { get { return false; } }
        public override bool AlwaysAttackable { get { return true; } }
        public override bool Unprovokable { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Regular; } }

        #region Outfit

        public override void InitBody()
        {
            Female = true;
            Body = 0x191;
            Hue = 0x4001;
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);

            AddItem(WildHunt.SetImmovable(WildHunt.SetHue(0x4001, new Cloak())));
            AddItem(WildHunt.SetImmovable(WildHunt.SetHue(0x4001, new Robe())));

            AddItem(WildHunt.SetImmovable(WildHunt.SetHue(0x4001, new NorseHelm())));

            AddItem(WildHunt.SetImmovable(WildHunt.Imbue(4, new Spear())));

            HairItemID = 0x203D;
            HairHue = 0x4001;
        }

        #endregion

        public override void OnAfterSpawn()
        {
            base.OnAfterSpawn();

            m_Army.Begin();
        }

        private int HitsPerc
        {
            get
            {
                int hitsMax = HitsMax;

                return (hitsMax <= 0 ? 100 : 100 * Hits / hitsMax);
            }
        }

        public override void OnThink()
        {
            if (CheckRally(this))
            {
                Animate(Mounted ? 26 : 9, 5, 1, true, false, 0);

                PublicOverheadMessage(MessageType.Emote, 0x3B2, false, "*lifts her spear high*");
                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Together, we are unstoppable! Charge with me, brethren!");
            }

            if (Combatant != null && HitsPerc < 50)
                Bjorn.CheckRecall(this);

            base.OnThink();
        }

        #region Rally

        public static int RallyCount = 4;
        public static TimeSpan RallyCooldown = TimeSpan.FromSeconds(20.0);
        public static TimeSpan RampageCooldown = TimeSpan.FromSeconds(80.0);

        private static readonly Memory m_RallyCooldowns = new Memory();

        public static bool CheckRally(BaseCreature bc)
        {
            if (!m_RallyCooldowns.Recall(bc))
            {
                if (DoRally(bc))
                {
                    m_RallyCooldowns.Remember(bc, RallyCooldown.TotalSeconds);
                    return true;
                }

                m_RallyCooldowns.Remember(bc, 5.0);
            }

            return false;
        }

        public static bool DoRally(BaseCreature bc)
        {
            List<Mobile> list = new List<Mobile>();

            foreach (Mobile m in bc.GetMobilesInRange(bc.RangePerception))
            {
                if (m.Combatant != null && RallyAttribute.CanRally(m.GetType()) && !Bjorn.IsRampaging(m) && WildHunt.IsValidBeneficial(bc, m))
                    list.Add(m);
            }

            if (list.Count == 0)
                return false;

            WildHunt.SortByDistance(bc.Location, list);

            int rallied = 0;

            for (int i = 0; rallied < RallyCount && i < list.Count; i++)
            {
                Mobile m = list[i];

                if (Bjorn.CheckRampage(m, RampageCooldown))
                    rallied++;
            }

            return (rallied != 0);
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
                    PackMagicEquipment(2, 3, 1.00, 1.00);
                    PackMagicEquipment(2, 3, 1.00, 1.00);
                    PackMagicEquipment(2, 4, 0.60, 0.60);
                    PackMagicEquipment(2, 4, 0.25, 0.25);
                    PackMagicItem(1, 3, 0.60);
                }
                else
                {
                    PackMagicEquipment(2, 3, 1.00, 1.00);
                    PackMagicEquipment(2, 3, 0.60, 0.60);
                    PackMagicEquipment(2, 4, 0.40, 0.40);
                    PackMagicItem(1, 3, 0.40);
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

        public Astrid(Serial serial)
            : base(serial)
        {
            m_Army = new ArmyState(this, AstridsArmy.Instance);

            WildHunt.Register(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            m_Army.Serialize(writer);

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
                        m_Army.Deserialize(reader);
                        goto case 1;
                    }
                case 1:
                    {
                        m_SteedKilled = reader.ReadBool();
                        break;
                    }
            }

            m_Army.StartTimer();
        }
    }

    public class AstridsArmy : ArmyDefinition
    {
        public static readonly AstridsArmy Instance = new AstridsArmy();

        private AstridsArmy()
            : base()
        {
            MaxSize = 10;
            InitSize = 4;
            RespawnCount = 2;
            SpawnRange = 5;
            HomeRange = 8;
            RecallRange = 30;
            MaxManpower = 20;
            Replenish = 1;
            RespawnDelay = TimeSpan.FromSeconds(40.0);

            Soldiers = new SoldierDefinition[]
                {
                    new SoldierDefinition(4, typeof(WildHuntWarrior), false),
                    new SoldierDefinition(1, typeof(WildHuntMage), false),
                };
        }
    }
}