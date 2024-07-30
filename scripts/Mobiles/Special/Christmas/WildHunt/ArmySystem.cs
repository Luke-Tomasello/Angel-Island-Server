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

/* Scripts/Mobiles/Special/Christmas/WildHunt/ArmySystem.cs
 * ChangeLog
 *  1/4/24, Yoar
 *		Initial Version.
 */

using Server.Items;
using System;
using System.Collections.Generic;
using SpawnRegistry = Server.Engines.Invasion.SpawnRegistry;

namespace Server.Mobiles.WildHunt
{
    [PropertyObject]
    public class ArmyState
    {
        private BaseCreature m_Owner;
        private ArmyDefinition m_Definition;
        private int m_Manpower;
        private SpawnRegistry m_Spawned;
        private Timer m_Timer;
        private DateTime m_NextRespawn;

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseCreature Owner
        {
            get { return m_Owner; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Manpower
        {
            get { return m_Manpower; }
            set { m_Manpower = Math.Max(0, Math.Min(m_Definition.MaxManpower, value)); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ManpowerMax
        {
            get { return m_Definition.MaxManpower; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Size
        {
            get { return m_Spawned.TotalCount(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SizeMax
        {
            get { return Misc.WinterEventSystem.WildHuntArmyScalar * m_Definition.MaxSize / 100; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Running
        {
            get { return (m_Timer != null); }
            set
            {
                if (Running != value)
                {
                    if (value)
                        StartTimer();
                    else
                        StopTimer();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextRespawn
        {
            get { return m_NextRespawn; }
            set { m_NextRespawn = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Manual_Respawn
        {
            get { return false; }
            set
            {
                if (value)
                    SpawnUpTo(m_Definition.RespawnCount);
            }
        }

        public ArmyState(BaseCreature owner, ArmyDefinition def)
        {
            m_Owner = owner;
            m_Definition = def;
            m_Spawned = new SpawnRegistry();
        }

        public void Begin()
        {
            Manpower = m_Definition.MaxManpower;
            m_NextRespawn = DateTime.UtcNow + m_Definition.RespawnDelay;

            SpawnUpTo(m_Definition.InitSize);

            StartTimer();
        }

        private void Slice()
        {
            Defrag();

            if (m_Owner.Deleted)
            {
                StopTimer();
                ClearHome();
                return;
            }

            UpdateHome();

            RecallUpTo(m_Definition.MaxSize, true);

            if (DateTime.UtcNow >= m_NextRespawn)
            {
                m_NextRespawn = DateTime.UtcNow + m_Definition.RespawnDelay;

                Manpower += m_Definition.Replenish;

                SpawnUpTo(m_Definition.RespawnCount);
            }

            UpdateCombatants();
        }

        private void Defrag()
        {
            List<BaseCreature> toRemove = new List<BaseCreature>();

            foreach (BaseCreature bc in m_Spawned)
            {
                if (bc.Deleted)
                    toRemove.Add(bc);
            }

            foreach (BaseCreature bc in toRemove)
                m_Spawned.Remove(bc);
        }

        public void ClearHome()
        {
            foreach (BaseCreature bc in m_Spawned)
                bc.Home = Point3D.Zero;
        }

        public void UpdateHome()
        {
            Point3D home = GetPointInDirection(m_Owner.Location, m_Owner.Direction);

            foreach (BaseCreature bc in m_Spawned)
                bc.Home = home;
        }

        public int RecallUpTo(int count, bool idle)
        {
            Point3D loc = m_Owner.Location;
            Map map = m_Owner.Map;

            List<Mobile> recallable = new List<Mobile>();

            foreach (BaseCreature bc in m_Spawned)
            {
                if (bc.Map == m_Owner.Map && bc.InRange(loc, m_Definition.RecallRange))
                    continue;

                if (idle && !IsIdle(bc))
                    continue;

                recallable.Add(bc);
            }

            WildHunt.SortByDistance(m_Owner.Location, recallable);

            int recalled = 0;

            for (int i = recallable.Count - 1; recalled < count && i >= 0; i--)
            {
                Point3D spawnLoc = GetSpawnLocation(loc, map, m_Definition.SpawnRange);

                if (spawnLoc == Point3D.Zero)
                    continue;

                Mobile toRecall = recallable[i];

                Poof(toRecall.Location, toRecall.Map);

                toRecall.MoveToWorld(spawnLoc, map);

                Poof(spawnLoc, map);

                recalled++;
            }

            return recalled;
        }

        private void UpdateCombatants()
        {
            Mobile combatant = m_Owner.Combatant;

            if (combatant == null)
                return;

            foreach (BaseCreature bc in m_Spawned)
            {
                if (bc.Combatant == null && bc.Map == combatant.Map && bc.InRange(combatant, bc.RangePerception))
                    bc.Combatant = combatant;
            }
        }

        public int SpawnUpTo(int count)
        {
            int remSize = SizeMax - Size;

            if (count > remSize)
                count = remSize;

            if (count > m_Manpower)
                count = m_Manpower;

            for (int i = 0; i < count; i++)
                DoSpawn();

            Manpower -= count;

            return count;
        }

        private void DoSpawn()
        {
            int spawnIndex = GetWeightedIndex(m_Definition.Soldiers);

            if (spawnIndex < 0 || spawnIndex >= m_Definition.Soldiers.Length)
                return;

            Point3D loc = m_Owner.Location;
            Map map = m_Owner.Map;

            Point3D spawnLoc = GetSpawnLocation(loc, map, m_Definition.SpawnRange);

            if (spawnLoc == Point3D.Zero)
                return;

            SoldierDefinition def = m_Definition.Soldiers[spawnIndex];

            BaseCreature bc = WildHunt.Construct(def.Type, def.Args);

            if (bc == null)
                return;

            bc.Direction = (Direction)Utility.Random(8);
            bc.Home = GetPointInDirection(loc, m_Owner.Direction);
            bc.RangeHome = m_Definition.HomeRange;

            bc.MoveToWorld(spawnLoc, map);
            bc.OnAfterSpawn();

            Poof(spawnLoc, map);

            m_Spawned.Add(bc, spawnIndex);
        }

        private int GetWeightedIndex(SoldierDefinition[] defs)
        {
            int bestIndex = -1;
            double minFactor = double.MaxValue;

            for (int i = 0; i < defs.Length; i++)
            {
                SoldierDefinition def = defs[i];

                double factor = m_Spawned.Count(i);

                if (def.Weight != 0)
                    factor /= def.Weight;

                if (factor < minFactor)
                {
                    bestIndex = i;
                    minFactor = factor;
                }
            }

            return bestIndex;
        }

        private static Point3D GetSpawnLocation(Point3D loc, Map map, int range)
        {
            if (map == null || map == Map.Internal)
                return Point3D.Zero;

            for (int i = 0; i < 20; i++)
            {
                int x = loc.X + Utility.RandomMinMax(-range, range);
                int y = loc.Y + Utility.RandomMinMax(-range, range);
                int z = loc.Z;

                if (Multis.BaseBoat.FindBoatAt(new Point2D(x, y), map) != null)
                    continue;

                if (map.CanFit(x, y, z, 16))
                    return new Point3D(x, y, z);

                z = map.GetAverageZ(x, y);

                if (map.CanFit(x, y, z, 16))
                    return new Point3D(x, y, z);
            }

            return Point3D.Zero;
        }

        private static Point3D GetPointInDirection(Point3D p, Direction d)
        {
            int x = p.X;
            int y = p.Y;

            Movement.Movement.Offset(d, ref x, ref y);

            return new Point3D(x, y, p.Z);
        }

        private static bool IsIdle(BaseCreature bc)
        {
            return (bc.Combatant == null || (bc.AIObject != null && !bc.AIObject.Active));
        }

        private static void Poof(Point3D loc, Map map)
        {
            Effects.SendLocationParticles(EffectItem.Create(loc, map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
        }

        public void StartTimer()
        {
            StopTimer();

            m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.0), Slice);
        }

        public void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)1); // version

            writer.Write((int)m_Manpower);

            m_Spawned.Serialize(writer);
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Manpower = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Spawned.Deserialize(reader);
                        break;
                    }
            }
        }

        public override string ToString()
        {
            return "...";
        }
    }

    public class ArmyDefinition
    {
        private int m_MaxSize;
        private int m_InitSize;
        private int m_SpawnRange;
        private int m_HomeRange;
        private int m_RecallRange;
        private int m_RespawnCount;
        private int m_MaxManpower;
        private int m_Replenish;
        private TimeSpan m_RespawnDelay;
        private SoldierDefinition[] m_Soldiers;

        public int MaxSize { get { return m_MaxSize; } set { m_MaxSize = value; } }
        public int InitSize { get { return m_InitSize; } set { m_InitSize = value; } }
        public int SpawnRange { get { return m_SpawnRange; } set { m_SpawnRange = value; } }
        public int HomeRange { get { return m_HomeRange; } set { m_HomeRange = value; } }
        public int RecallRange { get { return m_RecallRange; } set { m_RecallRange = value; } }
        public int RespawnCount { get { return m_RespawnCount; } set { m_RespawnCount = value; } }
        public int MaxManpower { get { return m_MaxManpower; } set { m_MaxManpower = value; } }
        public int Replenish { get { return m_Replenish; } set { m_Replenish = value; } }
        public TimeSpan RespawnDelay { get { return m_RespawnDelay; } set { m_RespawnDelay = value; } }
        public SoldierDefinition[] Soldiers { get { return m_Soldiers; } set { m_Soldiers = value; } }

        public ArmyDefinition()
        {
            m_Soldiers = new SoldierDefinition[0];
        }
    }

    public class SoldierDefinition
    {
        private int m_Weight;
        private Type m_Type;
        private object[] m_Args;

        public int Weight { get { return m_Weight; } }
        public Type Type { get { return m_Type; } }
        public object[] Args { get { return m_Args; } }

        public SoldierDefinition(int weight, Type type, params object[] args)
        {
            m_Weight = weight;
            m_Type = type;
            m_Args = args;
        }
    }
}