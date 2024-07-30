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

/* Scripts/Mobiles/Special/Christmas/WildHunt/Odin.cs
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
    public class Odin : BaseCreature, ICaptain
    {
        public static int GoodiesRadius = 8;
        public static int GoodiesTotalMin = 100000;
        public static int GoodiesTotalMax = 100000;

        private ArmyState m_Army;

        [CommandProperty(AccessLevel.GameMaster)]
        public ArmyState Army
        {
            get { return m_Army; }
            set { }
        }

        [Constructable]
        public Odin()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            BaseSoundID = 0x300;

            Name = "Odin";
            Title = "the Allfather";

            SetStr(1000);
            SetDex(150);
            SetInt(1000);

            SetHits(1800);

            SetDamage(13, 23);

            SetSkill(SkillName.Anatomy, 100.0);
            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.Magery, 120.1, 130.0);
            SetSkill(SkillName.MagicResist, 130.1, 140.0);
            SetSkill(SkillName.Meditation, 100.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 100.0);

            Fame = 24000;
            Karma = -24000;

            VirtualArmor = 60;

            InitBody();
            InitOutfit();

            ConvertEquipment();

            BaseMount sleipnir = new Sleipnir();
            sleipnir.Tamable = false;
            sleipnir.Rider = this;

            m_Army = new ArmyState(this, OdinsArmy.Instance);

            WildHunt.Register(this);
        }

        public override bool ShowFameTitle { get { return false; } }
        public override bool AlwaysAttackable { get { return true; } }
        public override bool Unprovokable { get { return true; } }
        public override bool BolaImmune { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Deadly; } }

        #region Outfit

        public override void InitBody()
        {
            Female = false;
            Body = 0x190;
            Hue = 0x841C;
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);

            AddItem(new PlateArms());
            AddItem(new PlateChest());
            AddItem(new PlateGloves());
            AddItem(new NorseHelm());
            AddItem(new PlateLegs());

            AddItem(new Boots());

            AddItem(WildHunt.SetHue(2219, new Cloak()));

            AddItem(WildHunt.Imbue(5, WildHunt.SetName("Gungnir", new Spear())));

            HairItemID = 0x203C;
            FacialHairItemID = 0x203E;
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
            if (Combatant != null && InRange(Combatant, LightningRange) && CheckLightning(this))
            {
                Animate(Mounted ? 26 : 9, 5, 1, true, false, 0);

                PublicOverheadMessage(MessageType.Emote, 0x3B2, false, "*raises Gungnir high*");
                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Í óðni og eldingar!!!");
            }

            if (Combatant != null && HitsPerc < 50)
                Bjorn.CheckRecall(this);

            base.OnThink();
        }

        public override bool DoActionOverride(bool obey)
        {
            if (IsCasting(this))
                return true;

            return base.DoActionOverride(obey);
        }

        #region Lightning

        public static int LightningRange = 4;
        public static int LightningDamageMin = 30;
        public static int LightningDamageMax = 50;
        public static int LightningEffectCount = 6;
        public static TimeSpan LightningCooldown = TimeSpan.FromSeconds(18.0);

        private static readonly Memory m_LightningCooldowns = new Memory();

        public static bool CheckLightning(BaseCreature bc)
        {
            if (!m_LightningCooldowns.Recall(bc))
            {
                m_LightningCooldowns.Remember(bc, LightningCooldown.TotalSeconds);

                BeginCast(bc, TimeSpan.FromSeconds(1.0), EndLightning);

                return true;
            }

            return false;
        }

        public static void EndLightning(BaseCreature bc, object state)
        {
            List<Mobile> targets = new List<Mobile>();

            foreach (Mobile m in bc.GetMobilesInRange(LightningRange))
            {
                if (WildHunt.IsValidHarmful(bc, m))
                    targets.Add(m);
            }

            for (int i = 0; i < targets.Count; i++)
            {
                Mobile m = targets[i];

                int damage = Utility.RandomMinMax(LightningDamageMin, LightningDamageMax);

                if (WildHunt.IsFollower(m))
                    damage *= 2;

                bc.DoHarmful(m);

                m.Damage(damage, bc, null);

                m.BoltEffect(0);
            }

            LightningStormController.DoStorm(bc.Location, bc.Map, LightningRange, LightningEffectCount);
        }

        #endregion

        public override bool OnBeforeDeath()
        {
            BaseMount mount = Mount as BaseMount;

            if (mount == null)
            {
                if (!base.OnBeforeDeath())
                    return false;

                UnconvertEquipment();

                return true;
            }
            else
            {
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
                PackMagicEquipment(1, 3, 1.00, 1.00);
                PackMagicEquipment(1, 3, 1.00, 1.00);
                PackMagicEquipment(1, 3, 1.00, 1.00);
                PackMagicEquipment(2, 3, 1.00, 1.00);
                PackMagicEquipment(2, 3, 0.85, 0.85);
                PackMagicEquipment(2, 3, 0.60, 0.60);
                PackMagicItem(1, 3, 1.00);
                PackMagicItem(1, 3, 0.85);
                PackMagicItem(1, 3, 0.60);
            }
        }

        public override bool CheckIdle()
        {
            return false; // we're never idle
        }

        public override void OnCarve(Mobile from, Corpse corpse, Item tool)
        {
            from.SendLocalizedMessage(500494); // You can't use a bladed item on that!
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            WildHunt.Unregister(this);
        }

        public Odin(Serial serial)
            : base(serial)
        {
            m_Army = new ArmyState(this, OdinsArmy.Instance);

            WildHunt.Register(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            m_Army.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Army.Deserialize(reader);
                        break;
                    }
            }

            m_Army.StartTimer();
        }

        #region Equipment Mutation

        private void ConvertEquipment()
        {
            for (int i = 0; i < m_MutateTable.Length; i++)
            {
                MutateEntry e = m_MutateTable[i];

                Item item = FindItemOnLayer(e.Layer);

                if (item.ItemID == e.BaseItemID)
                {
                    item.ItemID = e.MutateItemID;
                    item.Hue = e.MutateHue;
                    item.Movable = false;
                }
            }
        }

        private void UnconvertEquipment()
        {
            for (int i = 0; i < m_MutateTable.Length; i++)
            {
                MutateEntry e = m_MutateTable[i];

                Item item = FindItemOnLayer(e.Layer);

                if (item.ItemID == e.MutateItemID)
                {
                    item.ItemID = e.BaseItemID;
                    item.Hue = 0;
                    item.Movable = true;
                }
            }
        }

        private static readonly MutateEntry[] m_MutateTable = new MutateEntry[]
            {
                new MutateEntry(Layer.Shoes, 0x170B, 0x2B12, 2101),
                new MutateEntry(Layer.Pants, 0x1411, 0x2B07, 2101),
                new MutateEntry(Layer.Helm, 0x140E, 0x2B10, 2101),
                new MutateEntry(Layer.Gloves, 0x1414, 0x2B0C, 2101),
                new MutateEntry(Layer.InnerTorso, 0x1415, 0x2B08, 2101),
                new MutateEntry(Layer.Arms, 0x1410, 0x2B0A, 2101),
            };

        private struct MutateEntry
        {
            public readonly Layer Layer;
            public readonly int BaseItemID;
            public readonly int MutateItemID;
            public readonly int MutateHue;

            public MutateEntry(Layer layer, int baseItemID, int mutateItemID, int mutateHue)
            {
                Layer = layer;
                BaseItemID = baseItemID;
                MutateItemID = mutateItemID;
                MutateHue = mutateHue;
            }
        }

        #endregion

        #region Cast Timer

        public delegate void CastCallback(BaseCreature bc, object state);

        private static readonly Dictionary<Mobile, Timer> m_CastTimers = new Dictionary<Mobile, Timer>();

        public static Dictionary<Mobile, Timer> CastTimers { get { return m_CastTimers; } }

        public static bool IsCasting(BaseCreature bc)
        {
            return m_CastTimers.ContainsKey(bc);
        }

        public static void BeginCast(BaseCreature bc, TimeSpan duration, CastCallback callback, object state = null)
        {
            EndCast(bc);

            (m_CastTimers[bc] = new CastTimer(bc, duration, callback, state)).Start();
        }

        public static void EndCast(BaseCreature bc)
        {
            Timer timer;

            if (m_CastTimers.TryGetValue(bc, out timer))
            {
                timer.Stop();
                m_CastTimers.Remove(bc);
            }
        }

        private class CastTimer : Timer
        {
            private BaseCreature m_Creature;
            private CastCallback m_Callback;
            private object m_State;

            public CastTimer(BaseCreature bc, TimeSpan duration, CastCallback callback, object state)
                : base(duration)
            {
                m_Creature = bc;
                m_Callback = callback;
                m_State = state;
            }

            protected override void OnTick()
            {
                m_Callback(m_Creature, m_State);

                EndCast(m_Creature);
            }
        }

        #endregion
    }

    public class OdinsArmy : ArmyDefinition
    {
        public static readonly OdinsArmy Instance = new OdinsArmy();

        private OdinsArmy()
            : base()
        {
            MaxSize = 20;
            InitSize = 10;
            RespawnCount = 3;
            SpawnRange = 6;
            HomeRange = 15;
            RecallRange = 30;
            MaxManpower = 40;
            Replenish = 2;
            RespawnDelay = TimeSpan.FromSeconds(40.0);

            Soldiers = new SoldierDefinition[]
                {
                    new SoldierDefinition(2, typeof(WildHuntWarrior), false),
                    new SoldierDefinition(1, typeof(WildHuntMage), false),
                };
        }
    }
}