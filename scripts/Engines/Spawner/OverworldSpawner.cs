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

/* Scripts\Engines\Spawner\OverworldSpawner.cs
 * Changelog:
 *  11/24/2023, Yoar
 *      Initial creation.
 *      Intended for long-range spawning.
 */

using Server.Mobiles;

namespace Server.Items
{
    public class OverworldSpawner : Spawner
    {
        private int m_LandIDMin;
        private int m_LandIDMax;
        private int m_SpawnTries;
        private bool m_NoGuardZone;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public int LandIDMin { get { return m_LandIDMin; } set { m_LandIDMin = value; } }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public int LandIDMax { get { return m_LandIDMax; } set { m_LandIDMax = value; } }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public int SpawnTries { get { return m_SpawnTries; } set { m_SpawnTries = value; } }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public bool NoGuardZone { get { return m_NoGuardZone; } set { m_NoGuardZone = value; } }

        [Constructable]
        public OverworldSpawner()
            : base()
        {
            Name = "Overworld Spawner";

            m_SpawnTries = SpawnPointTries;
        }

        public override Point3D GetSpawnPosition(object o)
        {
            int temp = SpawnPointTries;

            SpawnPointTries = m_SpawnTries;

            Point3D loc = base.GetSpawnPosition(o);

            SpawnPointTries = temp;

            return loc;
        }

        public override bool ValidateSpawnPosition(Point3D loc, Map map)
        {
            if (map == null)
                return false; // sanity

            if (m_LandIDMin != 0 && m_LandIDMax != 0)
            {
                int landID = map.Tiles.GetLandTile(loc.X, loc.Y).ID;

                if (landID < m_LandIDMin || landID > m_LandIDMax)
                    return false;
            }

            if (m_NoGuardZone && Region.Find(loc, map).IsGuarded)
                return false;

            return true;
        }

        public OverworldSpawner(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);

            writer.Write((int)m_SpawnTries);
            writer.Write((bool)m_NoGuardZone);

            writer.Write((int)m_LandIDMin);
            writer.Write((int)m_LandIDMax);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_SpawnTries = reader.ReadInt();
                        m_NoGuardZone = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_LandIDMin = reader.ReadInt();
                        m_LandIDMax = reader.ReadInt();

                        break;
                    }
            }

            if (version < 1)
                m_SpawnTries = SpawnPointTries;
        }
    }
}