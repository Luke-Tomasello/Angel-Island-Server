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

/* Scripts\Engines\Catacomb\Mobiles\Navae.cs
 * ChangeLog:
 *  5/19/2024, Adam
 *      reduce HP by 25% (requested by Tabby)
 *      Reduced from 10 to 5 based on a request by Tabby
 */

using Server.Items;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Engines.Catacomb
{
    public class Navae : BaseCreature
    {
        [Constructable]
        public Navae()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "Navae";
            Title = "the tormented";
            Female = true;
            Body = 401;
            Hue = 0;
            BardImmune = true;

            SetStr(451, 500);
            SetDex(201, 250);
            SetInt(551, 600);

            SetHits((int)(1350 * .75)); // 5/19/2024, Adam: reduce HP by 25% (requested by Tabby)

            SetDamage(13, 24);

            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.MagicResist, 150.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 100.0);

            Fame = 30000;
            Karma = -30000;

            VirtualArmor = 30;

            HairItemID = 0x203C;
            HairHue = 0x455;

            int index = Utility.Random(16); // 25% chance to drop one piece of jewelry

            AddMovable(new RemembranceBracelet(), index == 0);
            AddMovable(new RemembranceEarrings(), index == 1);
            AddMovable(new RemembranceNecklace(), index == 2);
            AddMovable(new RemembranceRing(), index == 3);

            Item dress = new PlainDress();
            dress.Name = "Navae's dress";
            dress.Hue = 1109;
            //dress.SetItemBool(Item.ItemBoolTable.DeleteOnLift, true);
            dress.Movable = false;
            AddItem(dress);

            Item sandals = new Sandals();
            sandals.Name = "Navae's sandals";
            sandals.Hue = 1109;
            //sandals.SetItemBool(Item.ItemBoolTable.DeleteOnLift, true);
            sandals.Movable = false;
            AddItem(sandals);

            m_Spawned = new List<Mobile>();
        }

        private void AddMovable(Item item, bool movable)
        {
            item.Movable = movable;

            AddItem(item);
        }

        public override bool AlwaysMurderer { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Lesser; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool ClickTitle { get { return true; } }

        public override void GenerateLoot()
        {
            // TODO
        }

        public override void OnThink()
        {
            base.OnThink();

            if (Utility.RandomDouble() < 0.1)
                CheckSpawn();

            if (Utility.RandomDouble() < 0.1)
                CheckPoisonGas();

            if (Combatant != null && Utility.RandomDouble() < 0.05)
                CheckTaunt();
        }

        #region Unholy Bone

        public static double UnholyBoneChance = 0.1;

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (Utility.RandomDouble() < UnholyBoneChance)
                ThrowUnholyBone(defender);
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (Utility.RandomDouble() < UnholyBoneChance)
                ThrowUnholyBone(attacker);
        }

        public override void AlterDamageScalarFrom(Mobile caster, ref double scalar)
        {
            base.AlterDamageScalarFrom(caster, ref scalar);

            if (Utility.RandomDouble() < UnholyBoneChance)
                ThrowUnholyBone(caster);
        }

        private void ThrowUnholyBone(Mobile target)
        {
            if (this.Map == null)
                return;

            Direction = GetDirectionTo(target);

            MovingEffect(target, 0xF7E, 10, 1, true, false, 0x496, 0);

            new ThrowTimer(this, target).Start();
        }

        private class ThrowTimer : Timer
        {
            private Mobile m_Source;
            private Mobile m_Target;

            public ThrowTimer(Mobile source, Mobile target)
                : base(TimeSpan.FromSeconds(1.0))
            {
                m_Source = source;
                m_Target = target;
            }

            protected override void OnTick()
            {
                if (!m_Source.CanBeHarmful(m_Target))
                    return;

                m_Source.DoHarmful(m_Target);

                m_Target.Damage(Utility.RandomMinMax(10, 20), m_Source, null);

                new UnholyBone().MoveToWorld(m_Target.Location, m_Target.Map);
            }
        }

        #endregion

        #region Spawn Undead

        public static TimeSpan SpawnDelay = TimeSpan.FromSeconds(8.0);
        public static int SpawnLimit = 5;   // 5/19/2024, Adam: Reduced from 10 to 5 based on a request by Tabby

        private DateTime m_NextSpawn;
        private List<Mobile> m_Spawned;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextSpawn
        {
            get { return m_NextSpawn; }
            set { m_NextSpawn = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public List<Mobile> Spawned
        {
            get { return m_Spawned; }
        }

        private void CheckSpawn()
        {
            ProcessSpawned();

            if (DateTime.UtcNow < m_NextSpawn || m_Spawned.Count >= SpawnLimit)
                return;

            m_NextSpawn = DateTime.UtcNow + SpawnDelay;

            Point3D spawnLoc = GetSpawnLocation(4);

            if (spawnLoc == Point3D.Zero)
                return;

            BaseCreature bc;

            try
            {
                bc = (BaseCreature)Activator.CreateInstance(m_SpawnTypes[Utility.Random(m_SpawnTypes.Length)]);
            }
            catch
            {
                return;
            }

            string taunt = null;

            switch (Utility.Random(4))
            {
                case 0: taunt = "Riiiiise my friends, rise!"; break;
                case 1: taunt = "The dead don’t walk. Except, sometimes, when they do."; break;
            }

            if (taunt != null)
                Taunt(taunt, TimeSpan.FromSeconds(3.0));

            Effects.SendLocationParticles(EffectItem.Create(spawnLoc, Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);

            bc.Team = Team;
            bc.Home = Location;
            bc.RangeHome = 4;
            bc.Tamable = false;
            bc.MoveToWorld(spawnLoc, Map);
            bc.OnAfterSpawn();

            m_Spawned.Add(bc);
        }

        private void ProcessSpawned()
        {
            Mobile c = Combatant;

            for (int i = m_Spawned.Count - 1; i >= 0; i--)
            {
                Mobile m = m_Spawned[i];

                if (m.Deleted)
                {
                    m_Spawned.RemoveAt(i);
                    continue;
                }

                if (m is BaseCreature)
                    ((BaseCreature)m).Home = Location;

                if (c != null && m.Combatant == null)
                    m.Combatant = c;
            }
        }

        private Point3D GetSpawnLocation(int range)
        {
            Map map = this.Map;

            if (map == null || map == Map.Internal)
                return Point3D.Zero;

            for (int i = 0; i < 20; i++)
            {
                int x = Location.X + Utility.RandomMinMax(-range, range);
                int y = Location.Y + Utility.RandomMinMax(-range, range);
                int z = Location.Z;

                if (map.CanFit(x, y, z, 16, false, false, true) && InLOS(new Point3D(x, y, z)))
                    return new Point3D(x, y, z);

                z = map.GetAverageZ(x, y);

                if (map.CanFit(x, y, z, 16, false, false, true) && InLOS(new Point3D(x, y, z)))
                    return new Point3D(x, y, z);
            }

            return Point3D.Zero;
        }

        private static readonly Type[] m_SpawnTypes = new Type[]
            {
                typeof(SkeletalKnight),
                typeof(SkeletalMage)
            };

        #endregion

        #region Poison Gas

        public static TimeSpan PoisonGasDelay = TimeSpan.FromSeconds(15.0);

        private DateTime m_NextPoisonGas;

        private void CheckPoisonGas()
        {
            if (DateTime.UtcNow < m_NextPoisonGas)
                return;

            List<Mobile> targets = new List<Mobile>();

            foreach (Mobile m in GetMobilesInRange(4))
            {
                if (m == this || !m.Alive || m.IsDeadBondedPet || !CanBeHarmful(m) || m_Spawned.Contains(m) || IsFriend(m) || (m.Hidden && m.AccessLevel > AccessLevel.Player))
                    continue;

                targets.Add(m);
            }

            if (targets.Count == 0)
                return;

            m_NextPoisonGas = DateTime.UtcNow + PoisonGasDelay;

            HashSet<Point3D> locs = new HashSet<Point3D>();

            for (int i = 0; i < targets.Count; i++)
                locs.Add(targets[i].Location);

            int addLocs = 6 - locs.Count;

            for (int i = 0; i < addLocs; i++)
            {
                Point3D loc = GetSpawnLocation(4);

                if (loc != Point3D.Zero)
                    locs.Add(loc);
            }

            if (Utility.RandomBool())
                Taunt("Asphyxiation, suffocation! Joiiiin me!", TimeSpan.FromSeconds(3.0));

            foreach (Point3D loc in locs)
                Effects.SendLocationEffect(Location, Map, 0x11A6, 16, 3, 0, 0);

            Effects.PlaySound(Location, Map, 0x231);

            for (int i = 0; i < targets.Count; i++)
            {
                Mobile m = targets[i];

                m.ApplyPoison(m, Poison.Regular);
                m.LocalOverheadMessage(MessageType.Regular, 0x22, 500855); // You are enveloped by a noxious gas cloud!
            }
        }

        #endregion

        #region Taunts

        public static TimeSpan TauntDelay = TimeSpan.FromSeconds(12.0);

        private DateTime m_NextTaunt;

        private void CheckTaunt()
        {
            CheckTaunt(m_Taunts[Utility.Random(m_Taunts.Length)]);
        }

        private void CheckTaunt(string text)
        {
            if (DateTime.UtcNow >= m_NextTaunt)
                Taunt(text, TauntDelay);
        }

        private void Taunt(string text, TimeSpan delay)
        {
            m_NextTaunt = DateTime.UtcNow + delay;

            Say(text);
        }

        private static readonly string[] m_Taunts = new string[]
            {
                "Who disturbs my slumber!",
                "Get out! Get out!",
                "When I'm done with your friends I will haunt you for eternity!",
                "Dead, deceased, extinct, lifeless yet present. I'll show you a million way to die, each and every one unpleasant!",
                "Neither living, neither dead, leave my domain or off with thine head!",
                "Every breath you take is an insult to my presence!",
                "You'll never leave this catacomb... Alive!",
                "AHHHH",
            };

        #endregion

        public Navae(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.WriteMobileList(m_Spawned);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Spawned = reader.ReadStrongMobileList();

                        break;
                    }
            }
        }
    }
}