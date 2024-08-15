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

/* Scripts/Engines/Invasion/Undead/Mobiles/TheGuardian.cs
 * ChangeLog
 *  11/4/23, Yoar
 *      Increased HP from 1200 to 1500
 *  10/28/23, Yoar
 *		Initial Version.
 */

using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Engines.Invasion
{
    public class TheGuardian : BaseCreature
    {
        public static int TentaclesCount = 24;
        public static double ArcSpacing = 3 * Math.PI;
        public static int CircleSpacing = 12;
        public static int TeleportRadius = 12;
        public static int TeleportCooldown = 5;
        public static int GoodiesRadius = 8;
        public static int GoodiesTotalMin = 100000;
        public static int GoodiesTotalMax = 100000;
        public static TimeSpan DemorphDelay = TimeSpan.FromMinutes(30.0);
        public static int ArtifactsCount = 12;

        public override double DamageEntryExpireTimeSeconds { get { return 600.0; } } // 10 minutes

        private bool m_TrueForm;
        private List<TheGuardianTentacles> m_Tentacles;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool TrueForm
        {
            get { return m_TrueForm; }
            set
            {
                if (value)
                    Morph();
                else
                    Demorph();
            }
        }

        public List<TheGuardianTentacles> Tentacles
        {
            get { return m_Tentacles; }
        }

        [Constructable]
        public TheGuardian()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 18, 1, 0.25, 0.5)
        {
            Name = "the guardian";
            BodyValue = 146;
            Hue = 1157;

            SetStr(900, 1000);
            SetDex(125, 135);
            SetInt(1000, 1200);

            SetHits(1500);

            SetDamage(13, 20);

            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.Magery, 120.1, 130.0);
            SetSkill(SkillName.MagicResist, 130.1, 140.0);
            SetSkill(SkillName.Meditation, 100.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 100.0);

            Fame = 24000;
            Karma = -24000;

            VirtualArmor = 60;

            m_Tentacles = new List<TheGuardianTentacles>();
            m_DamageEntries = new Dictionary<Mobile, int>();
        }

        public override void GenerateLoot()
        {
            // SP custom

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

        public override bool Unprovokable { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        public override bool DisallowAllMoves { get { return m_TrueForm; } }

        public override bool OnBeforeDeath()
        {
            if (m_TrueForm)
            {
                bool result = base.OnBeforeDeath();

                if (!result)
                    return false;

                RegisterDamageTo(this);

                foreach (TheGuardianTentacles tentacles in m_Tentacles)
                    tentacles.Kill();

                m_Tentacles.Clear();

                if (!NoKillAwards)
                {
                    AwardArtifacts();

                    int goldDropped = GoodiesTimer.DropGoodies(this.Location, this.Map, GoodiesRadius, GoodiesTotalMin, GoodiesTotalMax);

                    LogHelper logger = new LogHelper("TheGuardian.log", false, true);
                    logger.Log(LogType.Mobile, this, string.Format("Dropped a total of {0} gold in goodies.", goldDropped.ToString()));
                    logger.Finish();
                }

                return true;
            }
            else
            {
                Morph();

                return false;
            }
        }

        private void Morph()
        {
            m_TrueForm = true;

            Name = "the true guardian";
            BodyValue = 780;

            Hits = HitsMax;
            Stam = StamMax;
            Mana = ManaMax;

            ProcessDelta();

            PlaySound(0x19C);
            Effects.SendLocationEffect(Location, Map, 0x3728, 10, 10, 0, 0);

            RespawnTentacles();

            ResetDemorph();
        }

        private void Demorph()
        {
            m_TrueForm = false;

            Name = "the guardian";
            BodyValue = 146;

            Hits = HitsMax;
            Stam = StamMax;
            Mana = ManaMax;

            ProcessDelta();

            PlaySound(0x19C);
            Effects.SendLocationEffect(Location, Map, 0x3728, 10, 10, 0, 0);

            RemoveTentacles();

            m_NextDemorph = DateTime.MinValue;

            m_DamageEntries.Clear();
        }

        private void RemoveTentacles()
        {
            foreach (Mobile m in m_Tentacles)
                m.Delete();

            m_Tentacles.Clear();
        }

        private void RespawnTentacles()
        {
            RemoveTentacles();

            SpawnTentacles(TentaclesCount);
        }

        private void SpawnTentacles(int count)
        {
            for (int i = 0; count > 0 && i < 10; i++) // max 10 circles
            {
                int radius = i * CircleSpacing;
                int toSpawn = Math.Min(count, (int)(2 * Math.PI * radius / ArcSpacing));

                SpawnTentacles(radius, toSpawn);

                count -= toSpawn;
            }
        }

        private void SpawnTentacles(int radius, int count)
        {
            Map map = this.Map;

            if (map == null)
                return;

            double dtheta = (1.0 / count) * 2.0 * Math.PI;

            for (int i = 0; i < count; i++)
            {
                double theta = i * dtheta;

                double rx = Math.Cos(theta);
                double ry = Math.Sin(theta);

                bool ok = false;
                int x = 0, y = 0, z = this.Z;

                for (int rdist = radius; !ok && rdist > 1; rdist--)
                {
                    x = this.X + (int)(rx * rdist);
                    y = this.Y + (int)(ry * rdist);

                    ok = IsValidSpawnLocation(map, x, y, ref z);
                }

                if (ok)
                    SpawnTentaclesAt(new Point3D(x, y, z), map);
            }
        }

        private static bool IsValidSpawnLocation(Map map, int x, int y, ref int z)
        {
            if (map.CanFit(x, y, z, 16))
                return true;

            z = map.GetAverageZ(x, y);

            if (map.CanFit(x, y, z, 16))
                return true;

            return false;
        }

        private void SpawnTentaclesAt(Point3D loc, Map map)
        {
            TheGuardianTentacles tentacles = new TheGuardianTentacles(this);

            tentacles.Team = Team;
            tentacles.GuardIgnore = true;
            tentacles.MoveToWorld(loc, map);

            PlaySound(0x19C);
            Effects.SendLocationEffect(loc, map, 0x3728, 10, 10, 0, 0);

            m_Tentacles.Add(tentacles);
        }

        public int MorphThreshold { get { return HitsMax / 40; } }

        private DateTime m_NextTeleport;
        private DateTime m_NextDemorph;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextTeleport
        {
            get { return m_NextTeleport; }
            set { m_NextTeleport = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextDemorph
        {
            get { return m_NextDemorph; }
            set { m_NextDemorph = value; }
        }

        public override void OnThink()
        {
            if (m_TrueForm && Combatant != null)
                ResetDemorph();

            if (m_TrueForm && DateTime.UtcNow >= m_NextDemorph && Hits >= MorphThreshold)
            {
                Demorph();
                return;
            }

            if (!m_TrueForm && Hits < MorphThreshold)
            {
                Morph();
                return;
            }

            if (DateTime.UtcNow >= m_NextTeleport)
            {
                DoTeleport(TeleportRadius, m_TrueForm);

                m_NextTeleport = DateTime.UtcNow + TimeSpan.FromSeconds(TeleportCooldown);
            }

            if (Combatant != null && Utility.RandomDouble() < 0.05)
                Taunt(1049503, 1049508, 1049509); // Come get some... | You will never steal my power! | How dare you awaken me...

            base.OnThink();
        }

        public override void OnDamage(int amount, Mobile from, bool willKill, object source_weapon)
        {
            if (m_TrueForm)
                ResetDemorph();

            base.OnDamage(amount, from, willKill, source_weapon);
        }

        private void ResetDemorph()
        {
            m_NextDemorph = DateTime.UtcNow + DemorphDelay;
        }

        private void DoTeleport(int radius, bool toUs)
        {
            List<Mobile> targets = new List<Mobile>();

            foreach (Mobile m in GetMobilesInRange(radius))
            {
                if (IsValidTarget(m) && GetDistanceToSqrt(m) <= TeleportRadius)
                    targets.Add(m);
            }

            if (targets.Count == 0)
                return;

            Mobile target = targets[Utility.Random(targets.Count)];

            if (toUs)
                Teleport(target, this);
            else
                Teleport(this, target);

            Combatant = target;
        }

        private bool IsValidTarget(Mobile m)
        {
            return (m != this && !m.Deleted && m.Alive && !m.IsDeadBondedPet && CanBeHarmful(m) && IsEnemy(m) && CanSee(m) && InLOS(m) && (m.AccessLevel == AccessLevel.Player || !m.Hidden));
        }

        private static void Teleport(Mobile source, Mobile target)
        {
            Map map = source.Map;

            if (map == null)
                return;

            Point3D pSource = source.Location;
            Point3D pTarget = target.Location;

            int rnd = Utility.Random(m_TeleportOffsets.Length);

            for (int i = 0; i < m_TeleportOffsets.Length; i++)
            {
                Point2D offset = m_TeleportOffsets[(rnd + i) % m_TeleportOffsets.Length];

                int x = pTarget.X + offset.X;
                int y = pTarget.Y + offset.Y;
                int z = pTarget.Z;

                if (IsValidSpawnLocation(map, x, y, ref z))
                {
                    pTarget = new Point3D(x, y, z);
                    break;
                }
            }

            source.Location = pTarget;

            Server.Spells.SpellHelper.Turn(source, target);
            Server.Spells.SpellHelper.Turn(target, source);

            source.ProcessDelta();

            Effects.SendLocationParticles(EffectItem.Create(pSource, map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
            Effects.SendLocationParticles(EffectItem.Create(pTarget, map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);

            source.PlaySound(0x1FE);
        }

        private static readonly Point2D[] m_TeleportOffsets = new Point2D[]
            {
                new Point2D(-1, -1),
                new Point2D(-1, +0),
                new Point2D(-1, +1),
                new Point2D(+0, -1),
                new Point2D(+0, +1),
                new Point2D(+1, -1),
                new Point2D(+1, +0),
                new Point2D(+1, +1),
            };

        public override void AlterDamageScalarTo(Mobile target, ref double scalar)
        {
            base.AlterDamageScalarTo(target, ref scalar);

            if (Utility.RandomDouble() < 0.25)
                Taunt(1049500, 1049505, 1049507); // Feel the delicious pain! | Your life will be mine! | Now you shall die.
        }

        public override void AlterMeleeDamageTo(Mobile to, ref int damage)
        {
            base.AlterMeleeDamageTo(to, ref damage);

            if (Utility.RandomDouble() < 0.25)
                Taunt(1049500, 1049505, 1049507); // Feel the delicious pain! | Your life will be mine! | Now you shall die.
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (Utility.RandomDouble() < 0.25)
            {
                if (!this.InRange(attacker, 1))
                    Taunt(1049506); // Dare you take a step closer?
                else
                    Taunt(1049501, 1049504); // You fight like a mongbat! | Oooh, that tickles.
            }
        }

        public override void OnDamagedBySpell(Mobile from)
        {
            base.OnDamagedBySpell(from);

            if (Utility.RandomDouble() < 0.25)
                Taunt(1049502); // You call that a spell?
        }

        public static TimeSpan TauntDelay = TimeSpan.FromSeconds(10.0);

        private DateTime m_NextTaunt;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextTaunt
        {
            get { return m_NextTaunt; }
            set { m_NextTaunt = value; }
        }

        private void Taunt(params int[] numbers)
        {
            Taunt(Utility.RandomList(numbers));
        }

        private void Taunt(int number)
        {
            if (DateTime.UtcNow >= m_NextTaunt)
            {
                Say(number);

                m_NextTaunt = DateTime.UtcNow + TauntDelay;
            }
        }

        private Dictionary<Mobile, int> m_DamageEntries;

        public void RegisterDamageTo(Mobile killed)
        {
            foreach (DamageEntry de in killed.DamageEntries)
            {
                if (de.Damager == null || de.DamageGiven <= 0 || de.HasExpired)
                    continue;

                Mobile damager = de.Damager;

                Mobile master = damager.GetDamageMaster(killed);

                if (master != null)
                    damager = master;

                RegisterDamage(damager, de.DamageGiven);
            }
        }

        public void RegisterDamage(Mobile from, int amount)
        {
            if (from == null || !from.Player)
                return;

            if (m_DamageEntries.ContainsKey(from))
                m_DamageEntries[from] += amount;
            else
                m_DamageEntries.Add(from, amount);

            if (Core.UOTC_CFG)
                from.SendMessage(string.Format("Total Damage: {0}", m_DamageEntries[from]));
        }

        private Mobile[] Lottery(int count)
        {
            if (count <= 0)
                return new Mobile[0];

            List<Mobile> eligible = new List<Mobile>();

            foreach (Mobile m in m_DamageEntries.Keys)
            {
                if (IsEligible(m))
                    eligible.Add(m);
            }

            if (eligible.Count == 0)
                return new Mobile[0];

            List<DamageResult> results = new List<DamageResult>();
            int total = 0;

            foreach (Mobile m in eligible)
            {
                int damage = m_DamageEntries[m];

                results.Add(new DamageResult(m, damage));
                total += damage;
            }

            List<Mobile> winners = new List<Mobile>();

            int div = count / eligible.Count;
            int rem = count % eligible.Count;

            for (int i = 0; i < div; i++)
                winners.AddRange(eligible);

            for (int i = 0; i < rem; i++)
            {
                int rnd = Utility.Random(total);

                for (int j = 0; j < results.Count; j++)
                {
                    DamageResult result = results[j];

                    if (rnd < result.Damage)
                    {
                        winners.Add(result.Mobile);

                        results.RemoveAt(j);
                        total -= result.Damage;

                        break;
                    }
                    else
                    {
                        rnd -= result.Damage;
                    }
                }
            }

            return winners.ToArray();
        }

        private bool IsEligible(Mobile m)
        {
            return (m.Player && m.Map == Map && m.InRange(Location, 100));
        }

        private struct DamageResult
        {
            public readonly Mobile Mobile;
            public readonly int Damage;

            public DamageResult(Mobile m, int damage)
            {
                Mobile = m;
                Damage = damage;
            }
        }

        private void AwardArtifacts()
        {
            foreach (Mobile m in Lottery(ArtifactsCount))
                AwardArtifact(m);
        }

        private void AwardArtifact(Mobile m)
        {
            ArtifactDef def = RandomArtifact();

            if (def == null)
                return;

            Item artifact = def.Construct();

            if (artifact == null)
                return;

            m.AddToBackpack(artifact);
            m.SendMessage("For your valor in combating the Guardian, a special artifact has been bestowed on you.");

            LogHelper logger = new LogHelper("TheGuardian.log", false, true);
            logger.Log(LogType.Mobile, m, string.Format("Received an artifact: {0} ({1}).", artifact.ToString(), artifact.GetType().Name));
            logger.Finish();
        }

        private static ArtifactDef RandomArtifact()
        {
            int total = 0;

            for (int i = 0; i < m_ArtifactDefs.Length; i++)
                total += m_ArtifactDefs[i].Weight;

            if (total <= 0)
                return null;

            int rnd = Utility.Random(total);

            for (int i = 0; i < m_ArtifactDefs.Length; i++)
            {
                if (rnd < m_ArtifactDefs[i].Weight)
                    return m_ArtifactDefs[i];
                else
                    rnd -= m_ArtifactDefs[i].Weight;
            }

            return null;
        }

        private static readonly ArtifactDef[] m_ArtifactDefs = new ArtifactDef[]
            {
                new ArtifactDef(1, typeof(ClothingBlessDeed)),
                new AnkhDef(1),
                new ArtifactDef(1, typeof(AltarAddonDeed)),

                // Harrower rewards
#if false
                new LeveledWeaponDef(2, Loot.ImbueLevel.Level6),
                new LeveledArmorDef(2, Loot.ImbueLevel.Level6),
#endif
                new RandomItemDef(1, false, typeof(SpecialHairDye), typeof(SpecialBeardDye)),
                new RareClothDef(1),
                new RandomItemDef(1, true,
                    typeof(PottedCactus), typeof(PottedCactus1), typeof(PottedCactus2),
                    typeof(PottedCactus3), typeof(PottedCactus4), typeof(PottedCactus5),
                    typeof(PottedPlant), typeof(PottedPlant1), typeof(PottedPlant2),
                    typeof(PottedTree), typeof(PottedTree1)),
                new RandomItemDef(1, true, typeof(LeatherArmorDyeTub)),
            };

        private class ArtifactDef
        {
            private Type m_ItemType;
            private int m_Weight;

            public Type ItemType { get { return m_ItemType; } }
            public int Weight { get { return m_Weight; } }

            public ArtifactDef(int weight, Type itemType)
            {
                m_ItemType = itemType;
                m_Weight = weight;
            }

            public virtual Item Construct()
            {
                return InvasionSystem.Construct<Item>(m_ItemType);
            }
        }

        private class RandomItemDef : ArtifactDef
        {
            private bool m_IsRare;
            private Type[] m_Types;

            public RandomItemDef(int weight, bool isRare, params Type[] types)
                : base(weight, typeof(Item))
            {
                m_IsRare = isRare;
                m_Types = types;
            }

            public override Item Construct()
            {
                Item item = Loot.Construct(m_Types);

                if (m_IsRare && item != null)
                    item.LootType = LootType.Rare;

                return item;
            }
        }

        private class AnkhDef : ArtifactDef
        {
            public AnkhDef(int weight)
                : base(weight, typeof(AnkhAddonDeed))
            {
            }

            public override Item Construct()
            {
                return new AnkhAddonDeed(true);
            }
        }

        private class LeveledWeaponDef : ArtifactDef
        {
            private Loot.ImbueLevel m_Level;

            public LeveledWeaponDef(int weight, Loot.ImbueLevel level)
                : base(weight, typeof(BaseWeapon))
            {
                m_Level = level;
            }

            public override Item Construct()
            {
                return Loot.ImbueWeaponOrArmor(true, Loot.RandomWeapon(), m_Level, 0, false);
            }
        }

        private class LeveledArmorDef : ArtifactDef
        {
            private Loot.ImbueLevel m_Level;

            public LeveledArmorDef(int weight, Loot.ImbueLevel level)
                : base(weight, typeof(BaseArmor))
            {
                m_Level = level;
            }

            public override Item Construct()
            {
                return Loot.ImbueWeaponOrArmor(true, Loot.RandomArmorOrShield(), m_Level, 0, false);
            }
        }

        private class RareClothDef : ArtifactDef
        {
            public RareClothDef(int weight)
                : base(weight, typeof(Cloth))
            {
            }

            public override Item Construct()
            {
                Item item = new UncutCloth(50);

                if (Utility.RandomBool())
                    item.Hue = Utility.RandomList(2213, 2219, 2207, 2425, 1109); // best ore hues (vet rewards) + really dark 'evil cloth'
                else
                    item.Hue = 0x01; // black cloth

                item.LootType = LootType.Rare;

                return item;
            }
        }

        public override void OnDelete()
        {
            RemoveTentacles();

            base.OnDelete();
        }

        public TheGuardian(Serial serial)
            : base(serial)
        {
            m_DamageEntries = new Dictionary<Mobile, int>();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((int)m_DamageEntries.Count);

            foreach (KeyValuePair<Mobile, int> kvp in m_DamageEntries)
            {
                writer.Write((Mobile)kvp.Key);
                writer.Write((int)kvp.Value);
            }

            writer.WriteDeltaTime(m_NextDemorph);

            writer.Write((bool)m_TrueForm);
            writer.WriteMobileList<TheGuardianTentacles>(m_Tentacles);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            Mobile m = reader.ReadMobile();
                            int damage = reader.ReadInt();

                            if (m != null)
                                m_DamageEntries[m] = damage;
                        }

                        goto case 1;
                    }
                case 1:
                    {
                        m_NextDemorph = reader.ReadDeltaTime();

                        goto case 0;
                    }
                case 0:
                    {
                        m_TrueForm = reader.ReadBool();
                        m_Tentacles = reader.ReadStrongMobileList<TheGuardianTentacles>();

                        break;
                    }
            }

            if (version < 1 && m_TrueForm)
                ResetDemorph();
        }
    }
}