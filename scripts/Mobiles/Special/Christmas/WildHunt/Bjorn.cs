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

/* Scripts/Mobiles/Special/Christmas/WildHunt/Bjorn.cs
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
    public interface ICaptain
    {
        ArmyState Army { get; }
    }

    [Rally]
    public class Bjorn : BaseCreature, ICaptain
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
        public Bjorn()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            BaseSoundID = 0x17E;

            Name = "Bjorn Ironfury";
            Title = "the Berserker";

            SetStr(800);
            SetDex(200);
            SetInt(100);

            SetHits(1200);

            SetDamage(11, 13);

            SetSkill(SkillName.Anatomy, 100.0);
            SetSkill(SkillName.Macing, 110.0);
            SetSkill(SkillName.MagicResist, 100.0);
            SetSkill(SkillName.Parry, 100.0);
            SetSkill(SkillName.Tactics, 100.0);

            Fame = 12000;
            Karma = -12000;

            VirtualArmor = 46;

            InitBody();
            InitOutfit();

            BaseMount charger = new WildHuntCharger();
            charger.Tamable = false;
            charger.Rider = this;

            m_Army = new ArmyState(this, BjornsArmy.Instance);

            WildHunt.Register(this);
        }

        public override bool ShowFameTitle { get { return false; } }
        public override bool AlwaysAttackable { get { return true; } }
        public override bool Unprovokable { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Regular; } }

        #region Outfit

        public override void InitBody()
        {
            Female = false;
            Body = 0x190;
            Hue = 0x4001;
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);

            AddItem(WildHunt.SetImmovable(WildHunt.SetHue(0x4001, new Cloak())));
            AddItem(WildHunt.SetImmovable(WildHunt.SetHue(0x4001, new Robe())));

            AddItem(WildHunt.SetImmovable(WildHunt.SetHue(0x4001, new BearMask())));

            AddItem(WildHunt.SetImmovable(WildHunt.Imbue(4, new WarAxe())));
            AddItem(WildHunt.SetImmovable(WildHunt.Imbue(4, new WoodenShield())));

            HairItemID = 0x203C;
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

        public static TimeSpan RampageCooldown = TimeSpan.FromSeconds(45.0);

        public override void OnThink()
        {
            if (Combatant != null && HitsPerc < 66 && CheckRampage(this, RampageCooldown))
            {
                Animate(Mounted ? 26 : 9, 5, 1, true, false, 0);

                PublicOverheadMessage(MessageType.Emote, 0x3B2, false, "*voice thundering across the battlefield*");
                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "I call upon the spirit of the bear!");
            }

            if (Combatant != null && HitsPerc < 50)
                CheckRecall(this);

            base.OnThink();
        }

        #region Rampage

        public static int RampageStrScalar = 150;
        public static int RampageDexScalar = 300;
        public static TimeSpan RampageDuration = TimeSpan.FromSeconds(20.0);

        private static readonly Dictionary<Mobile, Timer> m_RampageTimers = new Dictionary<Mobile, Timer>();
        private static readonly Memory m_RampageCooldowns = new Memory();

        public static Dictionary<Mobile, Timer> RampageTimers { get { return m_RampageTimers; } }

        public static bool IsRampaging(Mobile m)
        {
            return m_RampageTimers.ContainsKey(m);
        }

        public static bool CheckRampage(Mobile m, TimeSpan cooldown)
        {
            if (!m_RampageCooldowns.Recall(m))
            {
                m_RampageCooldowns.Remember(m, cooldown.TotalSeconds);

                BeginRampage(m);

                return true;
            }

            return false;
        }

        public static void BeginRampage(Mobile m)
        {
            EndRampage(m);

            m.AddStatMod(new StatMod(StatType.Str, "RampageStr", (int)(m.RawStr * ((RampageStrScalar - 100) / 100.0)), TimeSpan.Zero));
            m.AddStatMod(new StatMod(StatType.Dex, "RampageDex", (int)(m.RawDex * ((RampageDexScalar - 100) / 100.0)), TimeSpan.Zero));

            m.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
            m.PlaySound(0x1EA);

            (m_RampageTimers[m] = new RampageTimer(m, RampageDuration)).Start();
        }

        public static void EndRampage(Mobile m)
        {
            m.RemoveStatMod("RampageStr");
            m.RemoveStatMod("RampageDex");

            Timer timer;

            if (m_RampageTimers.TryGetValue(m, out timer))
            {
                timer.Stop();
                m_RampageTimers.Remove(m);
            }
        }

        private class RampageTimer : Timer
        {
            private Mobile m_Owner;
            private DateTime m_Expire;

            public RampageTimer(Mobile m, TimeSpan duration)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.25))
            {
                m_Owner = m;
                m_Expire = DateTime.UtcNow + duration;
            }

            protected override void OnTick()
            {
                if (DateTime.UtcNow >= m_Expire)
                {
                    EndRampage(m_Owner);

                    int stamMax = m_Owner.StamMax;

                    m_Owner.Stam = (stamMax <= 0 ? 0 : 15 * stamMax / 100);

                    Stop(); // sanity
                }
                else
                {
                    m_Owner.Stam = m_Owner.StamMax;
                }
            }
        }

        #endregion

        #region Recall Army

        public static int RecallAmount = 5;
        public static int RecallHealPerc = 25;
        public static TimeSpan RecallCooldown = TimeSpan.FromHours(2.0);

        private static readonly Memory m_RecallCooldowns = new Memory();

        public static void CheckRecall(Mobile m)
        {
            if (!m_RecallCooldowns.Recall(m))
            {
                m_RecallCooldowns.Remember(m, RecallCooldown.TotalSeconds);

                Recall(m);
            }
        }

        public static void Recall(Mobile m)
        {
            ICaptain captain = m as ICaptain;

            if (captain == null)
                return;

            m.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Protect your captain!");

            int count = RecallAmount;

            count -= captain.Army.RecallUpTo(count, false);

            if (count > 0)
                captain.Army.SpawnUpTo(count);

            int heal = RecallHealPerc * m.HitsMax / 100;

            m.Hits += heal;
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

        public Bjorn(Serial serial)
            : base(serial)
        {
            m_Army = new ArmyState(this, BjornsArmy.Instance);

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

    public class BjornsArmy : ArmyDefinition
    {
        public static readonly BjornsArmy Instance = new BjornsArmy();

        private BjornsArmy()
            : base()
        {
            MaxSize = 8;
            InitSize = 3;
            RespawnCount = 2;
            SpawnRange = 5;
            HomeRange = 6;
            RecallRange = 30;
            MaxManpower = 16;
            Replenish = 1;
            RespawnDelay = TimeSpan.FromSeconds(40.0);

            Soldiers = new SoldierDefinition[]
                {
                    new SoldierDefinition(1, typeof(WildHuntWarrior), false),
                };
        }
    }
}