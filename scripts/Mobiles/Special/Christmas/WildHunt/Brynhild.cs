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

/* Scripts/Mobiles/Special/Christmas/WildHunt/Brynhild.cs
 * ChangeLog
 *  1/1/24, Yoar
 *		Initial Version.
 */

using Server.Items;
using Server.Network;
using System;

namespace Server.Mobiles.WildHunt
{
    public interface IValkyrie
    {
        IRitual Ritual { get; set; }
    }

    public class Brynhild : BaseCreature, IValkyrie, ICaptain
    {
        private bool m_SteedKilled;
        private IRitual m_Ritual;
        private ArmyState m_Army;

        [CommandProperty(AccessLevel.GameMaster)]
        public IRitual Ritual
        {
            get { return m_Ritual; }
            set { m_Ritual = value; }
        }

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
        public Brynhild()
            : base(AIType.AI_Archer, FightMode.Aggressor | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            BaseSoundID = 0x17E;

            Name = "Brynhild";
            Title = "the Valkyrie";

            SetStr(800);
            SetDex(200);
            SetInt(100);

            SetHits(1200);

            SetDamage(11, 13);

            SetSkill(SkillName.Anatomy, 100.0);
            SetSkill(SkillName.Archery, 110.0);
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

            m_Army = new ArmyState(this, BrynhildsArmy.Instance);

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

            AddItem(WildHunt.SetImmovable(WildHunt.Imbue(4, new RepeatingCrossbow())));

            HairItemID = 0x2049;
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
            if (Combatant != null && HitsPerc < 50)
                Bjorn.CheckRecall(this);

            base.OnThink();
        }

        private bool m_WasInRange;

        public override bool DoActionOverride(bool obey)
        {
            bool inRange;

            if (DoRitual(this, out inRange))
            {
                if (inRange && !m_WasInRange)
                {
                    PublicOverheadMessage(MessageType.Emote, 0x3B2, false, "*raises crossbow high*");
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Awaken, fallen heroes, and reclaim your glory!");
                }

                m_WasInRange = inRange;

                return true;
            }

            return base.DoActionOverride(obey);
        }

        #region Valkyrie Ritual

        public static TimeSpan RitualCooldown = TimeSpan.FromSeconds(5.0);
        public static TimeSpan AnimationCooldown = TimeSpan.FromSeconds(5.0);

        private static readonly Memory m_RitualCooldowns = new Memory();
        private static readonly Memory m_AnimationMemory = new Memory();

        public static bool DoRitual(BaseCreature bc, out bool inRange)
        {
            inRange = false;

            if (!(bc is IValkyrie))
                return false;

            IValkyrie valkyrie = (IValkyrie)bc;

            if (bc.Combatant != null)
                return false;

            IRitual ritual = valkyrie.Ritual;

            if (ritual != null && (ritual.Deleted || ritual.CurProgress >= ritual.MaxProgress))
                valkyrie.Ritual = ritual = null;

            if (!m_RitualCooldowns.Recall(bc))
            {
                m_RitualCooldowns.Remember(bc, RitualCooldown.TotalSeconds);

                valkyrie.Ritual = ritual = FindRitual(bc, bc.RangePerception);
            }

            if (ritual == null)
                return false;

            if (WalkInRange(bc, ritual.Location, 1))
            {
                inRange = true;

                bc.Direction = bc.GetDirectionTo(ritual.Location);

                DoAnimation(bc, ritual);

                if (++ritual.CurProgress >= ritual.MaxProgress)
                    ritual.Complete();
            }

            return true;
        }

        private static IRitual FindRitual(Mobile m, int range)
        {
            IRitual nearest = null;
            int distMin = int.MaxValue;

            foreach (Item item in m.GetItemsInRange(range))
            {
                IRitual ritual = item as IRitual;

                if (ritual != null && ritual.CurProgress < ritual.MaxProgress && m.CanSee(item) && m.InLOS(item) && CanPath(m, item.Location))
                {
                    int dx = item.X - m.X;
                    int dy = item.Y - m.Y;

                    int dist = (dx * dx + dy * dy);

                    if (dist < distMin)
                    {
                        nearest = (IRitual)item;
                        distMin = dist;
                    }
                }
            }

            return nearest;
        }

        private static bool CanPath(Mobile m, Point3D target)
        {
            if (m.InRange(target, 1))
                return true;

            MovementPath path = new MovementPath(new Movement.MovementObject(m, target));

            return path.Success;
        }

        private static bool WalkInRange(BaseCreature bc, Point3D target, int range)
        {
            if (bc.InRange(target, range))
                return true;

            bc.AIObject.MoveTo(target, false, range);

            return false;
        }

        private static void DoAnimation(Mobile m, IRitual ritual)
        {
            if (!m_AnimationMemory.Recall(m))
            {
                m_AnimationMemory.Remember(m, AnimationCooldown.TotalSeconds);

                if (m.Mounted)
                    m.Animate(26, 5, 1, true, false, 0);
                else
                    m.Animate(263, 7, 1, true, false, 0);

                Effects.PlaySound(ritual.Location, ritual.Map, 0x214);
                Effects.SendLocationParticles(ritual, 0x376A, 10, 16, 0, 0);
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

        public Brynhild(Serial serial)
            : base(serial)
        {
            m_Army = new ArmyState(this, BrynhildsArmy.Instance);

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

    public class BrynhildsArmy : ArmyDefinition
    {
        public static readonly BrynhildsArmy Instance = new BrynhildsArmy();

        private BrynhildsArmy()
            : base()
        {
            MaxSize = 12;
            InitSize = 6;
            RespawnCount = 2;
            SpawnRange = 5;
            HomeRange = 10;
            RecallRange = 30;
            MaxManpower = 24;
            Replenish = 1;
            RespawnDelay = TimeSpan.FromSeconds(40.0);

            Soldiers = new SoldierDefinition[]
                {
                    new SoldierDefinition(2, typeof(WildHuntWarrior), false),
                    new SoldierDefinition(1, typeof(WildHuntMage), false),
                };
        }
    }
}