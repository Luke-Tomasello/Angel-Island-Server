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

/* Scripts\Engines\Invasion\InvasionSpawner.cs
 * Changelog:
 *  10/7/23, Yoar
 *      Initial version.
 */

using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Engines.Invasion
{
    public class InvasionSpawner : Item, IMobSpawner
    {
        public override string DefaultName { get { return "Invasion Spawner"; } }

        private InvasionType m_InvasionType;
        private IRegionControl m_RegionControl;

        private int m_SpawnLimit;
        private int m_RespawnCount;
        private TimeSpan m_RespawnDelay;
        private int m_DespawnCount;
        private TimeSpan m_DespawnDelay;
        private int m_AtrophyAmount;
        private TimeSpan m_AtrophyDelay;

        private int m_Spacing;
        private bool m_Balanced;
        private int m_KillsCap;
        private bool m_NoKillAwards;

        private Timer m_Timer;
        private int m_Kills;
        private DateTime m_LastRespawn;
        private DateTime m_LastDespawn;
        private DateTime m_LastAtrophy;
        private SpawnRegistry m_Spawned;
        private SpawnMesh m_SpawnMesh;

        [CommandProperty(AccessLevel.GameMaster)]
        public InvasionType InvasionType
        {
            get { return m_InvasionType; }
            set { m_InvasionType = value; }
        }

        public IRegionControl RegionControl
        {
            get { return m_RegionControl; }
            set
            {
                if (m_RegionControl != value)
                {
                    m_RegionControl = value;

                    RegenerateMesh();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public StaticRegionControl StaticRegionControl
        {
            get { return RegionControl as StaticRegionControl; }
            set { RegionControl = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CustomRegionControl CustomRegionControl
        {
            get { return RegionControl as CustomRegionControl; }
            set { RegionControl = value; }
        }

        public Region Region
        {
            get
            {
                if (m_RegionControl != null)
                    return m_RegionControl.Region;

                return null;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpawnLimit
        {
            get { return m_SpawnLimit; }
            set { m_SpawnLimit = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RespawnCount
        {
            get { return m_RespawnCount; }
            set { m_RespawnCount = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan RespawnDelay
        {
            get { return m_RespawnDelay; }
            set { m_RespawnDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DespawnCount
        {
            get { return m_DespawnCount; }
            set { m_DespawnCount = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan DespawnDelay
        {
            get { return m_DespawnDelay; }
            set { m_DespawnDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AtrophyAmount
        {
            get { return m_AtrophyAmount; }
            set { m_AtrophyAmount = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan AtrophyDelay
        {
            get { return m_AtrophyDelay; }
            set { m_AtrophyDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Spacing
        {
            get { return m_Spacing; }
            set
            {
                if (m_Spacing != value)
                {
                    m_Spacing = value;

                    RegenerateMesh();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Balanced
        {
            get { return m_Balanced; }
            set { m_Balanced = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int KillsCap
        {
            get { return m_KillsCap; }
            set { m_KillsCap = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool NoKillAwards
        {
            get { return m_NoKillAwards; }
            set { m_NoKillAwards = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get { return (m_Timer != null); }
            set
            {
                if (value != Active)
                {
                    if (value)
                        StartTimer();
                    else
                        StopTimer();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Kills
        {
            get { return m_Kills; }
            set { m_Kills = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Tier
        {
            get
            {
                InvasionSystem system = InvasionSystem.Get(m_InvasionType);

                if (system != null)
                    return system.GetTier(m_Kills);

                return 0;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastRespawn
        {
            get { return m_LastRespawn; }
            set { m_LastRespawn = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastDespawn
        {
            get { return m_LastDespawn; }
            set { m_LastDespawn = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastAtrophy
        {
            get { return m_LastAtrophy; }
            set { m_LastAtrophy = value; }
        }

        public SpawnRegistry Spawned
        {
            get { return m_Spawned; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpawnedCount
        {
            get { return m_Spawned.TotalCount(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Manual_Clear
        {
            get { return false; }
            set
            {
                if (value)
                    ClearSpawned();
            }
        }

        public SpawnMesh SpawnMesh
        {
            get { return m_SpawnMesh; }
        }

        [Constructable]
        public InvasionSpawner()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_KillsCap = -1;

            m_Spawned = new SpawnRegistry();
        }

        private void RegenerateMesh()
        {
            Region region = Region;

            if (region == null || m_Spacing <= 1)
            {
                m_SpawnMesh = null;
            }
            else
            {
                m_SpawnMesh = new SpawnMesh(ToArea2D(region.Coords), m_Spacing);
                m_SpawnMesh.Fill(m_Spawned);
            }
        }

        private static Rectangle2D[] ToArea2D(IList<Rectangle3D> area3D)
        {
            Rectangle2D[] area2D = new Rectangle2D[area3D.Count];

            for (int i = 0; i < area2D.Length; i++)
                area2D[i] = new Rectangle2D(area3D[i].Start, area3D[i].End);

            return area2D;
        }

        public void OnTick()
        {
            InvasionSystem system = InvasionSystem.Get(m_InvasionType);
            Region region = Region;

            if (system == null || region == null || region.Map == null)
                return;

            int tier = system.GetTier(m_Kills);

            if (DateTime.UtcNow >= m_LastAtrophy + m_AtrophyDelay)
            {
                m_LastAtrophy = DateTime.UtcNow;

                int atrophy = Math.Min(m_AtrophyAmount, m_Kills);

                m_Kills -= atrophy;
            }

            List<BaseCreature> toRemove = new List<BaseCreature>();
            List<Mobile> eligible = new List<Mobile>();

            foreach (BaseCreature bc in m_Spawned)
            {
                if (bc.Deleted)
                {
                    if (bc != null && bc.LastKiller != null)
                    {
                        if (system.GetCreatureTier(bc.GetType()) == tier && (m_KillsCap == -1 || m_Kills < m_KillsCap))
                            m_Kills++;

                        if (!m_NoKillAwards)
                            system.HandleKill(bc, eligible);
                    }

                    toRemove.Add(bc);
                }
            }

            system.DistributeArtifacts(eligible, tier);

            if (DateTime.UtcNow >= m_LastDespawn + m_DespawnDelay)
            {
                m_LastDespawn = DateTime.UtcNow;

                int toDespawn = Math.Min(m_DespawnCount, m_Spawned.TotalCount());

                for (int i = 0; i < toDespawn; i++)
                {
                    BaseCreature bc = m_Spawned.GetRandom();

                    if (bc.Map == null || !bc.Map.GetSector(bc.X, bc.Y).Active)
                    {
                        bc.Delete();
                        toDespawn--;

                        toRemove.Add(bc);
                    }
                }
            }

            foreach (BaseCreature bc in toRemove)
                m_Spawned.Remove(bc);

            tier = system.GetTier(m_Kills);

            PublicOverheadMessage(Network.MessageType.Regular, 0x3B2, false, string.Format("Kills: {0}, Tier: {1}", m_Kills, tier));

            if (DateTime.UtcNow >= m_LastRespawn + m_RespawnDelay)
            {
                m_LastRespawn = DateTime.UtcNow;

                int toRespawn = Math.Min(m_RespawnCount, m_SpawnLimit - m_Spawned.TotalCount());

                for (int i = 0; i < toRespawn; i++)
                {
                    int index = GetSpawnIndex(system.CreatureDefs, tier);

                    if (index >= 0 && index < system.CreatureDefs.Length)
                    {
                        Point3D spawnLoc = GetSpawnLocation(region.Coords, region.Map);

                        if (spawnLoc != Point3D.Zero)
                        {
                            BaseCreature bc = system.CreatureDefs[index].Construct();

                            if (bc != null)
                            {
                                SpawnCreature(bc, spawnLoc, region.Map);

                                m_Spawned.Add(bc, index);

                                if (m_SpawnMesh != null)
                                    m_SpawnMesh.SetAt(spawnLoc.X, spawnLoc.Y, bc);
                            }
                        }
                    }
                }
            }
        }

        private int GetSpawnIndex(CreatureDefinition[] defs, int tier)
        {
            if (m_Balanced)
                return GetWeightedIndex(defs, tier);
            else
                return GetRandomIndex(defs, tier);
        }

        private int GetRandomIndex(CreatureDefinition[] defs, int tier)
        {
            int total = 0;

            for (int i = 0; i < defs.Length; i++)
            {
                CreatureDefinition def = defs[i];

                if (tier == def.Tier)
                    total += def.Weight;
            }

            if (total <= 0)
                return -1;

            int rnd = Utility.Random(total);

            for (int i = 0; i < defs.Length; i++)
            {
                CreatureDefinition def = defs[i];

                if (tier == def.Tier)
                {
                    if (rnd < def.Weight)
                        return i;
                    else
                        rnd -= def.Weight;
                }
            }

            return -1;
        }

        private int GetWeightedIndex(CreatureDefinition[] defs, int tier)
        {
            int bestIndex = -1;
            double minFactor = double.MaxValue;

            for (int i = 0; i < defs.Length; i++)
            {
                CreatureDefinition def = defs[i];

                if (tier == def.Tier)
                {
                    double factor = m_Spawned.Count(i);

                    if (def.Weight != 0)
                        factor /= def.Weight;

                    if (factor < minFactor)
                    {
                        bestIndex = i;
                        minFactor = factor;
                    }
                }
            }

            return bestIndex;
        }

        private Point3D GetSpawnLocation(IList<Rectangle3D> area, Map map)
        {
            for (int i = 0; i < 20; i++)
            {
                Point2D loc = GetRandomLocation(area);

                if (Multis.BaseBoat.FindBoatAt(loc, map) != null)
                    continue;

                if (m_SpawnMesh != null)
                {
                    BaseCreature bc = m_SpawnMesh.GetAt(loc.X, loc.Y);

                    if (bc != null && !bc.Deleted)
                        continue;
                }

                int x = loc.X;
                int y = loc.Y;
                int z = Z;

                if (map.CanFit(x, y, z, 16))
                    return new Point3D(x, y, z);

                z = map.GetAverageZ(x, y);

                if (map.CanFit(x, y, z, 16))
                    return new Point3D(x, y, z);
            }

            return Point3D.Zero;
        }

        private static Point2D GetRandomLocation(IList<Rectangle3D> area)
        {
            int totalArea = 0;

            for (int i = 0; i < area.Count; i++)
                totalArea += (area[i].Width * area[i].Height);

            if (totalArea <= 0)
                return Point2D.Zero;

            int rnd = Utility.Random(totalArea);

            for (int i = 0; i < area.Count; i++)
            {
                Rectangle3D rect = area[i];

                int curArea = rect.Width * rect.Height;

                if (rnd < curArea)
                {
                    int x = rect.Start.X + Utility.Random(rect.Width);
                    int y = rect.Start.Y + Utility.Random(rect.Height);

                    return new Point2D(x, y);
                }
                else
                {
                    rnd -= curArea;
                }
            }

            return Point2D.Zero;
        }

        private void SpawnCreature(BaseCreature bc, Point3D loc, Map map)
        {
            bc.SpawnerHandle = this;
            bc.Home = loc;
            bc.RangeHome = 8;
            bc.Tamable = false;
            bc.GuardIgnore = true;
            bc.MoveToWorld(loc, map);
            bc.OnAfterSpawn();
        }

        public void ClearSpawned()
        {
            foreach (BaseCreature bc in m_Spawned)
                bc.Delete();

            m_Spawned.Clear();
        }

        public void StartTimer()
        {
            StopTimer();

            (m_Timer = new InternalTimer(this)).Start();
        }

        public void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        private class InternalTimer : Timer
        {
            private InvasionSpawner m_Controller;

            public InternalTimer(InvasionSpawner controller)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(5.0))
            {
                m_Controller = controller;
            }

            protected override void OnTick()
            {
                m_Controller.OnTick();
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (Active)
                LabelTo(from, "(Active)");
            else
                LabelTo(from, "(Inactive)");
        }

        public override void OnAfterDelete()
        {
            StopTimer();
        }

        #region IMobSpawner

        void IMobSpawner.GenerateLoot(BaseCreature bc)
        {
            InvasionSystem system = InvasionSystem.Get(m_InvasionType);

            if (system != null)
                system.GenerateLoot(bc);
        }

        #endregion

        public InvasionSpawner(Serial serial)
            : base(serial)
        {
            m_Spawned = new SpawnRegistry();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((bool)m_NoKillAwards);

            writer.Write((int)m_KillsCap);

            writer.Write((int)m_InvasionType);
            writer.Write((Item)(m_RegionControl as Item));

            writer.Write((int)m_SpawnLimit);
            writer.Write((int)m_RespawnCount);
            writer.Write((TimeSpan)m_RespawnDelay);
            writer.Write((int)m_DespawnCount);
            writer.Write((TimeSpan)m_DespawnDelay);
            writer.Write((int)m_AtrophyAmount);
            writer.Write((TimeSpan)m_AtrophyDelay);

            writer.Write((int)m_Spacing);
            writer.Write((bool)m_Balanced);

            writer.Write((bool)(m_Timer != null));
            writer.Write((int)m_Kills);
            m_Spawned.Serialize(writer);

            if (m_SpawnMesh == null)
            {
                writer.Write((bool)false);
            }
            else
            {
                writer.Write((bool)true);
                m_SpawnMesh.Serialize(writer);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            bool running = false;

            switch (version)
            {
                case 2:
                    {
                        m_NoKillAwards = reader.ReadBool();

                        goto case 1;
                    }
                case 1:
                    {
                        m_KillsCap = reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        m_InvasionType = (InvasionType)reader.ReadInt();
                        m_RegionControl = reader.ReadItem() as IRegionControl;

                        m_SpawnLimit = reader.ReadInt();
                        m_RespawnCount = reader.ReadInt();
                        m_RespawnDelay = reader.ReadTimeSpan();
                        m_DespawnCount = reader.ReadInt();
                        m_DespawnDelay = reader.ReadTimeSpan();
                        m_AtrophyAmount = reader.ReadInt();
                        m_AtrophyDelay = reader.ReadTimeSpan();

                        m_Spacing = reader.ReadInt();
                        m_Balanced = reader.ReadBool();

                        running = reader.ReadBool();
                        m_Kills = reader.ReadInt();
                        m_Spawned.Deserialize(reader);

                        if (reader.ReadBool())
                            m_SpawnMesh = new SpawnMesh(reader);

                        break;
                    }
            }

            if (version < 1)
                m_KillsCap = -1;

            if (running)
                StartTimer();
        }
    }

    public class SpawnRegistry : IEnumerable<BaseCreature>
    {
        private Dictionary<int, List<BaseCreature>> m_Table;

        public Dictionary<int, List<BaseCreature>> Table { get { return m_Table; } }

        public SpawnRegistry()
        {
            m_Table = new Dictionary<int, List<BaseCreature>>();
        }

        public void Add(BaseCreature bc)
        {
            Add(bc, -1);
        }

        public void Add(BaseCreature bc, int index)
        {
            List<BaseCreature> list;

            if (!m_Table.TryGetValue(index, out list))
                m_Table[index] = list = new List<BaseCreature>();

            list.Add(bc);
        }

        public void Remove(BaseCreature bc)
        {
            foreach (KeyValuePair<int, List<BaseCreature>> kvp in m_Table)
            {
                if (kvp.Value.Remove(bc))
                    break;
            }
        }

        public int TotalCount()
        {
            int total = 0;

            foreach (KeyValuePair<int, List<BaseCreature>> kvp in m_Table)
                total += kvp.Value.Count;

            return total;
        }

        public int Count(int index)
        {
            List<BaseCreature> list;

            if (m_Table.TryGetValue(index, out list))
                return list.Count;

            return 0;
        }

        public BaseCreature GetRandom()
        {
            int total = 0;

            foreach (KeyValuePair<int, List<BaseCreature>> kvp in m_Table)
                total += kvp.Value.Count;

            if (total <= 0)
                return null;

            int rnd = Utility.Random(total);

            foreach (KeyValuePair<int, List<BaseCreature>> kvp in m_Table)
            {
                if (rnd < kvp.Value.Count)
                    return kvp.Value[rnd];
                else
                    rnd -= kvp.Value.Count;
            }

            return null;
        }

        public void Clear()
        {
            m_Table.Clear();
        }

        #region IEnumerable<BaseCreature>

        public IEnumerator GetEnumerator()
        {
            return new InternalEnumerator(m_Table.Values);
        }

        IEnumerator<BaseCreature> IEnumerable<BaseCreature>.GetEnumerator()
        {
            return (IEnumerator<BaseCreature>)this.GetEnumerator();
        }

        private struct InternalEnumerator : IEnumerator<BaseCreature>
        {
            private IEnumerator<List<BaseCreature>> m_Enum1;
            private IEnumerator<BaseCreature> m_Enum2;
            private bool m_Disposed;

            public object Current
            {
                get { return m_Enum2.Current; }
            }

            BaseCreature IEnumerator<BaseCreature>.Current
            {
                get { return (BaseCreature)this.Current; }
            }

            public InternalEnumerator(IEnumerable<List<BaseCreature>> eable)
            {
                m_Enum1 = eable.GetEnumerator();
                m_Enum2 = null;
                m_Disposed = false;
            }

            public bool MoveNext()
            {
                while (m_Enum2 == null || !m_Enum2.MoveNext())
                {
                    if (!m_Enum1.MoveNext())
                        return false;

                    m_Enum2 = m_Enum1.Current.GetEnumerator();
                }

                return true;
            }

            public void Reset()
            {
                m_Enum1.Reset();
                m_Enum2 = null;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;

                m_Enum1 = null;
                m_Enum2 = null;
                m_Disposed = true;
            }
        }

        #endregion

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version

            writer.Write((int)m_Table.Count);

            foreach (KeyValuePair<int, List<BaseCreature>> kvp in m_Table)
            {
                writer.Write((int)kvp.Key);
                writer.Write((int)kvp.Value.Count);

                foreach (BaseCreature bc in kvp.Value)
                    writer.Write((Mobile)bc);
            }
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            int tableCount = reader.ReadInt();

            for (int i = 0; i < tableCount; i++)
            {
                int index = reader.ReadInt();
                int count = reader.ReadInt();

                for (int j = 0; j < count; j++)
                {
                    BaseCreature bc = reader.ReadMobile() as BaseCreature;

                    if (bc != null)
                        Add(bc, index);
                }
            }
        }
    }

    public class SpawnMesh
    {
        private SpawnRaster[] m_Rasters;

        public SpawnRaster[] Rasters { get { return m_Rasters; } }

        public SpawnMesh(Rectangle2D[] area, int side)
        {
            m_Rasters = new SpawnRaster[area.Length];

            for (int i = 0; i < m_Rasters.Length; i++)
                m_Rasters[i] = new SpawnRaster(area[i], side);
        }

        public void Fill(IEnumerable<BaseCreature> spawned)
        {
            foreach (BaseCreature bc in spawned)
            {
                if (!bc.Deleted)
                {
                    Point2D p;

                    if (bc.Home != Point3D.Zero)
                        p = new Point2D(bc.Home);
                    else
                        p = new Point2D(bc.Location);

                    SetAt(p.X, p.Y, bc);
                }
            }
        }

        public bool Contains(int x, int y)
        {
            for (int i = 0; i < m_Rasters.Length; i++)
            {
                SpawnRaster r = m_Rasters[i];

                if (r.Contains(x, y))
                    return true;
            }

            return false;
        }

        public BaseCreature GetAt(int x, int y)
        {
            for (int i = 0; i < m_Rasters.Length; i++)
            {
                SpawnRaster r = m_Rasters[i];

                if (r.Contains(x, y))
                    return r.GetAt(x, y);
            }

            return null;
        }

        public void SetAt(int x, int y, BaseCreature bc)
        {
            for (int i = 0; i < m_Rasters.Length; i++)
            {
                SpawnRaster r = m_Rasters[i];

                if (r.Contains(x, y))
                    r.SetAt(x, y, bc);
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version

            writer.Write((int)m_Rasters.Length);

            for (int i = 0; i < m_Rasters.Length; i++)
                m_Rasters[i].Serialize(writer);
        }

        public SpawnMesh(GenericReader reader)
        {
            int version = reader.ReadInt();

            m_Rasters = new SpawnRaster[reader.ReadInt()];

            for (int i = 0; i < m_Rasters.Length; i++)
                m_Rasters[i] = new SpawnRaster(reader, version);
        }
    }

    public class SpawnRaster
    {
        private int m_Side;
        private Rectangle2D m_Rect;
        private BaseCreature[,] m_Matrix;

        public int Side { get { return m_Side; } }
        public Rectangle2D Rect { get { return m_Rect; } }
        public BaseCreature[,] Matrix { get { return m_Matrix; } }

        public SpawnRaster(Rectangle2D rect, int side)
        {
            int rows = (rect.Height + side - 1) / side;
            int cols = (rect.Width + side - 1) / side;

            m_Side = side;
            m_Rect = rect;
            m_Matrix = new BaseCreature[rows, cols];
        }

        public bool Contains(int x, int y)
        {
            return (x >= m_Rect.X && x < m_Rect.End.X && y >= m_Rect.Y && y < m_Rect.End.Y);
        }

        public BaseCreature GetAt(int x, int y)
        {
            int i = (y - m_Rect.Y) / m_Side;
            int j = (x - m_Rect.X) / m_Side;

            if (i >= 0 && i < m_Matrix.GetLength(0) && j >= 0 && j < m_Matrix.GetLength(1))
                return m_Matrix[i, j];

            return null;
        }

        public void SetAt(int x, int y, BaseCreature bc)
        {
            int i = (y - m_Rect.Y) / m_Side;
            int j = (x - m_Rect.X) / m_Side;

            if (i >= 0 && i < m_Matrix.GetLength(0) && j >= 0 && j < m_Matrix.GetLength(1))
                m_Matrix[i, j] = bc;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)m_Side);
            writer.Write((Rectangle2D)m_Rect);

            int rows = m_Matrix.GetLength(0);
            int cols = m_Matrix.GetLength(1);

            writer.Write((int)rows);
            writer.Write((int)cols);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    writer.Write((Mobile)m_Matrix[i, j]);
            }
        }

        public SpawnRaster(GenericReader reader, int version)
        {
            m_Side = reader.ReadInt();
            m_Rect = reader.ReadRect2D();

            int rows = reader.ReadInt();
            int cols = reader.ReadInt();

            m_Matrix = new BaseCreature[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    m_Matrix[i, j] = reader.ReadMobile() as BaseCreature;
            }
        }
    }
}