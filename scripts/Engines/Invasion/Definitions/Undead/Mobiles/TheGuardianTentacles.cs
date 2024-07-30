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

/* Scripts/Engines/Invasion/Undead/Mobiles/TheGuardianTentacles.cs
 * ChangeLog
 *  11/4/23, Yoar
 *      Added death explode effects + spawning of unholy bones
 *  11/4/23, Yoar
 *      Also drain at the previous 2 radii to prevent players from running through the drain unharmed.
 *  10/28/23, Yoar
 *		Initial Version.
 */

using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Engines.Invasion
{
    [CorpseName("lifeless tentacles")]
    public class TheGuardianTentacles : BaseCreature
    {
        public static TimeSpan DrainCooldown = TimeSpan.FromSeconds(12.0);
        public static int DrainRadius = 12;
        public static int DrainDamageMin = 16;
        public static int DrainDamageMax = 30;
        public static int DrainHealScalar = 200;
        public static int DamageDistMin = 2;
        public static int DamageDistMax = 10;
        public static int UnholyBoneCount = 15;

        public override double DamageEntryExpireTimeSeconds { get { return 600.0; } } // 10 minutes

        private TheGuardian m_Guardian;

        [CommandProperty(AccessLevel.GameMaster)]
        public TheGuardian Harrower
        {
            get { return m_Guardian; }
            set { m_Guardian = value; }
        }

        [Constructable]
        public TheGuardianTentacles()
            : this(null)
        {
        }

        public TheGuardianTentacles(TheGuardian guardian)
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            m_Guardian = guardian;
            m_NextDrain = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomDouble() * DrainCooldown.TotalSeconds);

            Name = "tentacles of the guardian";
            Body = 129;
            BaseSoundID = 352;

            SetStr(901, 1000);
            SetDex(126, 140);
            SetInt(1001, 1200);

            SetHits(1000);

            SetDamage(9, 16);

            SetSkill(SkillName.MagicResist, 100.1, 110.0);
            SetSkill(SkillName.Meditation, 100.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 18000;
            Karma = -18000;

            VirtualArmor = 60;
        }

        public override bool Unprovokable { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override bool DisallowAllMoves { get { return true; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 5 : 0; } }

        public override void GenerateLoot()
        {
            // SP custom

            if (Spawning)
            {
                PackGold(1800, 2400);
            }
            else
            {
                PackMagicEquipment(1, 3, 1.00, 1.00);
                PackMagicEquipment(2, 3, 0.60, 0.60);
                PackMagicItem(1, 3, 0.60);
            }
        }

        private DateTime m_NextDrain;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextDrain
        {
            get { return m_NextDrain; }
            set { m_NextDrain = value; }
        }

        public override void OnThink()
        {
            base.OnThink();

            if (DateTime.UtcNow >= m_NextDrain && HasDrainTarget())
            {
                m_NextDrain = DateTime.UtcNow + DrainCooldown;

                BeginDrain();
            }
        }

        private bool HasDrainTarget()
        {
            foreach (Mobile m in GetMobilesInRange(DrainRadius))
            {
                if (IsValidTarget(m) && GetDistanceToSqrt(m) <= DrainRadius)
                    return true;
            }

            return false;
        }

        private void BeginDrain()
        {
            FixedParticles(0x374A, 10, 15, 5013, 0x485, 5, EffectLayer.Waist);
            PlaySound(0x1F1);

            StartDrainTimer();
        }

        private Timer m_DrainTimer;

        private void StopDrainTimer()
        {
            if (m_DrainTimer != null)
            {
                m_DrainTimer.Stop();
                m_DrainTimer = null;
            }
        }

        private void StartDrainTimer()
        {
            StopDrainTimer();

            int range = Math.Max(1, Math.Min(18, DrainRadius));

            (m_DrainTimer = new DrainTimer(this, range)).Start();
        }

        private class DrainTimer : Timer
        {
            private TheGuardianTentacles m_Tentacles;
            private int m_Tick;
            private HashSet<Mobile> m_Hit;

            public DrainTimer(TheGuardianTentacles tentacles, int count)
                : base(TimeSpan.FromMilliseconds(600.0), TimeSpan.FromMilliseconds(300.0), count)
            {
                m_Tentacles = tentacles;
                m_Hit = new HashSet<Mobile>();
            }

            protected override void OnTick()
            {
                m_Tick++;

                // also drain at the previous 2 radii to prevent players from running through the drain unharmed
                if (m_Tick >= 2)
                    m_Tentacles.DoDrain(m_Tick - 2, m_Hit, false);
                if (m_Tick >= 1)
                    m_Tentacles.DoDrain(m_Tick - 1, m_Hit, false);

                m_Tentacles.DoDrain(m_Tick, m_Hit, true);
            }
        }

        private void DoDrain(int radius, HashSet<Mobile> hit, bool randomEffects)
        {
            Map map = this.Map;

            if (map == null)
                return;

            foreach (Point2D p2D in Circle(new Point2D(this.Location), radius))
            {
                Point3D p3D = new Point3D(p2D, this.Z);

                bool any = false;

                foreach (Mobile m in map.GetMobilesInRange(p3D, 0))
                {
                    if (IsValidTarget(m) && hit.Add(m))
                    {
                        any = true;

                        DrainTarget(m);

                        m.FixedParticles(0x374A, 10, 15, 5013, 0x485, 5, EffectLayer.Waist);
                        m.PlaySound(0x1F1);
                    }
                }

                if (randomEffects && !any && Utility.RandomDouble() < 0.2)
                {
                    Effects.SendLocationParticles(EffectItem.Create(p3D, map, EffectItem.DefaultDuration), 0x374A, 10, 15, 0x485, 5, 9917, 0);
                    Effects.PlaySound(p3D, map, 0x1FB);
                }
            }
        }

        private void DrainTarget(Mobile m)
        {
            DoHarmful(m);

            int drain = Utility.RandomMinMax(DrainDamageMin, DrainDamageMax);

            int heal = DrainHealScalar * drain / 100;

            Hits += heal;

            if (m_Guardian != null)
                m_Guardian.Hits += heal;

            m.Damage(drain, this, null);
        }

        private static Point2D[] Circle(Point2D center, int radius)
        {
            List<Point2D> list = new List<Point2D>();

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    int dist = (int)Math.Round(Math.Sqrt(x * x + y * y));

                    if (dist == radius)
                        list.Add(new Point2D(center.X + x, center.Y + y));
                }
            }

            return list.ToArray();
        }

        private bool IsValidTarget(Mobile m)
        {
            return (m != this && !m.Deleted && m.Alive && !m.IsDeadBondedPet && CanBeHarmful(m) && IsEnemy(m) && (m.AccessLevel == AccessLevel.Player || !m.Hidden));
        }

        public override void AlterDamageScalarFrom(Mobile caster, ref double scalar)
        {
            scalar *= GetDamageScalar(caster);
        }

        public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
        {
            double scalar = GetDamageScalar(from);

            damage = (int)(scalar * damage);
        }

        // TODO: Scale explosion potion damage?

        private double GetDamageScalar(Mobile from)
        {
            int dx = from.X - this.X;
            int dy = from.Y - this.Y;

            double dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist < DamageDistMin)
                return 1.0;

            if (dist >= DamageDistMax)
                return 0.0;

            return Math.Sqrt((DamageDistMax - dist) / (DamageDistMax - DamageDistMin));
        }

        public override void OnDeath(Container c)
        {
            GrotesqueBeast.Explode(Location, Map, 1, DrainRadius, Utility.RandomMinMax(15, 20));

            if (UnholyBoneCount > 0)
                GrotesqueBeast.Explode(Location, Map, DrainRadius, DrainRadius, UnholyBoneCount, new Type[] { typeof(UnholyBone) });

            base.OnDeath(c);

            if (m_Guardian != null)
                m_Guardian.RegisterDamageTo(this);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            StopDrainTimer();
        }

        public TheGuardianTentacles(Serial serial)
            : base(serial)
        {
            m_NextDrain = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomDouble() * DrainCooldown.TotalSeconds);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((Mobile)m_Guardian);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Guardian = reader.ReadMobile() as TheGuardian;

                        break;
                    }
            }
        }
    }
}