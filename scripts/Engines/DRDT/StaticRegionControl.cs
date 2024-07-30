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

/* Scripts/Engines/DRDT/StaticRegionControl.cs
 * CHANGELOG
 *  1/18/23, Yoar
 *      Now keeping a direct ref to the region rather than an index
 *      Now serializing the static region ref by name, if the name is not null
 *  1/14/23, Yoar
 *      Removed InternalGump, use [StaticRegions command instead
 *      Renamed RegionIdx -> StaticIdx
 *      Added RegionNamer getter/setter to more attach a region to the controller using the region's name
 *  1/3/23, Yoar
 *      Implemented StaticRegionControl.StaticRegion setter
 *  1/2/23, Adam
 *      change the itemID to something different than CustomRegionControl
 *  9/22/22, Yoar
 *      Added Registered getter/setter
 *  9/19/22, Yoar
 *      Initial version
 */

using Server.Gumps;
using Server.Regions;
using System.Collections.Generic;

namespace Server.Items
{
    public interface IRegionControl
    {
        Region Region { get; }
    }

    public class StaticRegionControl : Item, IRegionControl
    {
        public static readonly List<StaticRegionControl> m_Instances = new List<StaticRegionControl>();

        public static List<StaticRegionControl> Instances { get { return m_Instances; } }

        public override string DefaultName { get { return "Static Region Controller"; } }

        private StaticRegion m_StaticRegion;

        [CommandProperty(AccessLevel.GameMaster)]
        public int StaticIdx
        {
            get { return (m_StaticRegion == null ? -1 : StaticRegion.XmlDatabase.IndexOf(m_StaticRegion)); }
            set
            {
                if (value == -1)
                    StaticRegion = null;
                else if (value >= 0 && value < StaticRegion.XmlDatabase.Count)
                    StaticRegion = StaticRegion.XmlDatabase[value];
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string RegionName
        {
            get { return (m_StaticRegion == null ? null : m_StaticRegion.Name); }
            set
            {
                StaticRegion sr;

                if (value == null)
                    StaticRegion = null;
                else if (Find(value, Map, out sr))
                    StaticRegion = sr;
            }
        }

        private static bool Find(string name, Map map, out StaticRegion found)
        {
            for (int i = 0; i < StaticRegion.XmlDatabase.Count; i++)
            {
                StaticRegion sr = StaticRegion.XmlDatabase[i];

                if (sr.Map == map && sr.Name == name)
                {
                    found = sr;
                    return true;
                }
            }

            found = null;
            return false;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public StaticRegion StaticRegion
        {
            get { return m_StaticRegion; }
            set
            {
                if (m_StaticRegion != value)
                {
                    m_StaticRegion = value;

                    Update();
                }
            }
        }

        Region IRegionControl.Region { get { return StaticRegion; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Registered
        {
            get { return (m_StaticRegion == null ? false : m_StaticRegion.Registered); }
            set
            {
                if (m_StaticRegion != null && Registered != value)
                {
                    m_StaticRegion.Registered = value;

                    Update();
                }
            }
        }

        [Constructable]
        public StaticRegionControl()
            : base(0x0BAC) // sign
        {
            Movable = false;
            Visible = false;

            Update();

            m_Instances.Add(this);
        }

        public void Update()
        {
            StaticRegion region = this.StaticRegion;

            if (region == null || !region.Registered)
                this.Hue = 53; // yellow
            else if (region.NoMurderCounts)
                this.Hue = 33; // red
            else
                this.Hue = 70; // green
        }

        public override void OnSingleClick(Mobile m)
        {
            StaticRegion region = this.StaticRegion;

            if (region != null && region.Name != null)
                LabelTo(m, region.Name);
            else
                base.OnSingleClick(m);
        }

        public override void OnDoubleClick(Mobile from)
        {
            StaticRegion region = this.StaticRegion;

            if (region != null && from.AccessLevel >= AccessLevel.GameMaster)
                from.SendGump(new RegionControlGump(region));
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            m_Instances.Remove(this);
        }

        public StaticRegionControl(Serial serial)
            : base(serial)
        {
            m_Instances.Add(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            if (m_StaticRegion == null)
            {
                writer.Write((byte)0x00);
            }
            else if (m_StaticRegion.Name != null)
            {
                writer.Write((byte)0x02);
                writer.Write((string)m_StaticRegion.Name);
            }
            else
            {
                writer.Write((byte)0x01);
                writer.Write((int)StaticIdx);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        byte type = reader.ReadByte();

                        object[] data = new object[0];

                        switch (type)
                        {
                            case 0x01: data = new object[] { reader.ReadInt() }; break;
                            case 0x02: data = new object[] { reader.ReadString() }; break;
                        }

                        // static regions haven't been loaded in yet
                        // let's store this data and lookup the region refs on Initialize
                        if (type != 0x00)
                            m_LoadTable[this] = new RegionRef(type, data);

                        break;
                    }
            }
        }

        private static readonly Dictionary<StaticRegionControl, RegionRef> m_LoadTable = new Dictionary<StaticRegionControl, RegionRef>();

        [CallPriority(-1)]
        public static void Initialize()
        {
            foreach (KeyValuePair<StaticRegionControl, RegionRef> kvp in m_LoadTable)
                kvp.Value.Assign(kvp.Key);

            m_LoadTable.Clear();
        }

        private class RegionRef
        {
            private byte m_Type;
            private object[] m_Data;

            public RegionRef(byte type, object[] data)
            {
                m_Type = type;
                m_Data = data;
            }

            public void Assign(StaticRegionControl src)
            {
                switch (m_Type)
                {
                    case 0x01:
                        {
                            if (m_Data.Length == 1 && m_Data[0] is int)
                                src.StaticIdx = (int)m_Data[0];

                            break;
                        }
                    case 0x02:
                        {
                            if (m_Data.Length == 1 && m_Data[0] is string)
                                src.RegionName = (string)m_Data[0];

                            break;
                        }
                }
            }
        }

        #region Region Controller Fixer

        public static int FixControllers()
        {
            int fixedCount = 0;

            foreach (ControllerFix fix in m_FixerTable)
            {
                StaticRegionControl src = null;

                foreach (Item item in fix.ControllerMap.GetItemsInRange(fix.ControllerLoc, 0))
                {
                    if (item is StaticRegionControl)
                    {
                        src = (StaticRegionControl)item;
                        break;
                    }
                }

                if (src != null && src.RegionName != fix.RegionName)
                {
                    src.RegionName = fix.RegionName;
                    fixedCount++;
                }
            }

            return fixedCount;
        }

        private static readonly ControllerFix[] m_FixerTable = new ControllerFix[]
            {
                new ControllerFix("Yew Cave", new Point3D(773, 1684, 0), Map.Felucca),
                new ControllerFix("Magincia", new Point3D(5429, 1166, 0), Map.Felucca),
                new ControllerFix("Minoc", new Point3D(5428, 1167, 0), Map.Felucca),
                new ControllerFix("North Territory Mine 2", new Point3D(5436, 1166, 0), Map.Felucca),
                new ControllerFix("Terathan Keep", new Point3D(4481, 1401, 39), Map.Felucca),
                new ControllerFix("Khaldun", new Point3D(5427, 1164, 0), Map.Felucca),
                new ControllerFix("Moongates", new Point3D(5431, 1161, 0), Map.Felucca),
                new ControllerFix("Moongate Houseblockers", new Point3D(5430, 1161, 0), Map.Felucca),
                new ControllerFix("Angel Island", new Point3D(5432, 1160, 0), Map.Felucca),
                new ControllerFix("Britain Graveyard", new Point3D(5432, 1166, 0), Map.Felucca),
                new ControllerFix("Cove", new Point3D(5431, 1162, 0), Map.Felucca),
                new ControllerFix("Britain", new Point3D(5427, 1161, 0), Map.Felucca),
                new ControllerFix("Jhelom Islands", new Point3D(5433, 1161, 0), Map.Felucca),
                new ControllerFix("Jhelom", new Point3D(5429, 1162, 0), Map.Felucca),
                new ControllerFix("Minoc", new Point3D(5433, 1165, 0), Map.Felucca),
                new ControllerFix("Minoc Cave 1", new Point3D(5430, 1165, 0), Map.Felucca),
                new ControllerFix("Minoc Cave 2", new Point3D(5428, 1161, 0), Map.Felucca),
                new ControllerFix("Jail", new Point3D(5430, 1162, 0), Map.Felucca),
                new ControllerFix("Green Acres", new Point3D(5434, 1161, 0), Map.Felucca),
                new ControllerFix("Britannia Royal Zoo", new Point3D(5427, 1166, 0), Map.Felucca),
                new ControllerFix("Ocllo", new Point3D(5433, 1160, 0), Map.Felucca),
                new ControllerFix("Trinsic", new Point3D(5431, 1164, 0), Map.Felucca),
                new ControllerFix("Vesper", new Point3D(5434, 1164, 0), Map.Felucca),
                new ControllerFix("Yew", new Point3D(5433, 1164, 0), Map.Felucca),
                new ControllerFix("Wind", new Point3D(5434, 1166, 0), Map.Felucca),
                new ControllerFix("Serpent's Hold", new Point3D(5432, 1164, 0), Map.Felucca),
                new ControllerFix("Skara Brae", new Point3D(5433, 1162, 0), Map.Felucca),
                new ControllerFix("Nujel'm", new Point3D(5432, 1162, 0), Map.Felucca),
                new ControllerFix("Moonglow", new Point3D(5428, 1159, 0), Map.Felucca),
                new ControllerFix("Magincia", new Point3D(5434, 1163, 0), Map.Felucca),
                new ControllerFix("Buccaneer's Den", new Point3D(5433, 1163, 0), Map.Felucca),
                new ControllerFix("Delucia", new Point3D(5432, 1163, 0), Map.Felucca),
                new ControllerFix("Papua", new Point3D(5431, 1163, 0), Map.Felucca),
                new ControllerFix("Covetous", new Point3D(5432, 1159, 0), Map.Felucca),
                new ControllerFix("Deceit", new Point3D(5431, 1159, 0), Map.Felucca),
                new ControllerFix("Despise", new Point3D(5429, 1160, 0), Map.Felucca),
                new ControllerFix("Destard", new Point3D(5428, 1160, 0), Map.Felucca),
                new ControllerFix("Hythloth", new Point3D(5427, 1160, 0), Map.Felucca),
                new ControllerFix("Shame", new Point3D(5434, 1159, 0), Map.Felucca),
                new ControllerFix("Baby Green Acres", new Point3D(5431, 1166, 0), Map.Felucca),
                new ControllerFix("Cave 1", new Point3D(5432, 1165, 0), Map.Felucca),
                new ControllerFix("Fire", new Point3D(5427, 1162, 0), Map.Felucca),
                new ControllerFix("Ice", new Point3D(5434, 1162, 0), Map.Felucca),
                new ControllerFix("Cave 2", new Point3D(5428, 1162, 0), Map.Felucca),
                new ControllerFix("Cave 3", new Point3D(5432, 1161, 0), Map.Felucca),
                new ControllerFix("Cave 4", new Point3D(5429, 1161, 0), Map.Felucca),
                new ControllerFix("Britain Mine 1", new Point3D(5434, 1160, 0), Map.Felucca),
                new ControllerFix("Britain Mine 2", new Point3D(5431, 1160, 0), Map.Felucca),
                new ControllerFix("Deceit Entrance", new Point3D(5433, 1159, 0), Map.Felucca),
                new ControllerFix("Covetous Mine", new Point3D(5429, 1163, 0), Map.Felucca),
                new ControllerFix("North Territory Mine 2", new Point3D(5430, 1164, 0), Map.Felucca),
                new ControllerFix("North Territory Mine 1", new Point3D(5427, 1165, 0), Map.Felucca),
                new ControllerFix("Ice Isle Cave 2", new Point3D(5434, 1165, 0), Map.Felucca),
                new ControllerFix("North Territory Cave", new Point3D(5431, 1165, 0), Map.Felucca),
                new ControllerFix("Minoc Mine", new Point3D(5430, 1166, 0), Map.Felucca),
                new ControllerFix("Minoc Cave 3", new Point3D(5433, 1166, 0), Map.Felucca),
                new ControllerFix("Ice Isle Cave 1", new Point3D(5428, 1166, 0), Map.Felucca),
                new ControllerFix("Yew Cave", new Point3D(5428, 1165, 0), Map.Felucca),
                new ControllerFix("Mt Kendall", new Point3D(5430, 1163, 0), Map.Felucca),
                new ControllerFix("Hythloth Entrance", new Point3D(5428, 1163, 0), Map.Felucca),
                new ControllerFix("Wrong Entrance", new Point3D(5430, 1159, 0), Map.Felucca),
                new ControllerFix("Ice Entrance", new Point3D(5429, 1159, 0), Map.Felucca),
                new ControllerFix("Destard Entrance", new Point3D(5430, 1160, 0), Map.Felucca),
                new ControllerFix("Covetous Entrance", new Point3D(5427, 1159, 0), Map.Felucca),
                new ControllerFix("Despise Entrance", new Point3D(5428, 1164, 0), Map.Felucca),
                new ControllerFix("Despise Passage", new Point3D(5429, 1164, 0), Map.Felucca),
                new ControllerFix("Ocllo Island", new Point3D(5429, 1165, 0), Map.Felucca),
                new ControllerFix("Yew Orc Fort Small Area", new Point3D(5427, 1167, 0), Map.Felucca),
                new ControllerFix("Broken ML Map", new Point3D(1399, 1458, 16), Map.Felucca),
                new ControllerFix("Marble Island Arena", new Point3D(1906, 2096, 5), Map.Felucca),
                new ControllerFix("the savage homeland", new Point3D(5438, 1163, 0), Map.Felucca),
                new ControllerFix("New Region", new Point3D(5205, 1757, 0), Map.Felucca),
                new ControllerFix("Bludchok'a-hai", new Point3D(5438, 1166, 0), Map.Felucca),
                new ControllerFix("Last Man Standing", new Point3D(4594, 3627, 54), Map.Felucca),
                new ControllerFix("Invasion Spawn", new Point3D(1430, 1389, 0), Map.Felucca),
                new ControllerFix("Last Man Standing:80", new Point3D(4592, 3619, 30), Map.Felucca),
                new ControllerFix("Rings", new Point3D(5438, 1164, 0), Map.Felucca),
                new ControllerFix("Arena", new Point3D(5438, 1159, 0), Map.Felucca),
                new ControllerFix("the undead stronghold of Necropolis", new Point3D(5438, 1162, 0), Map.Felucca),
                new ControllerFix("Memorial of the fallen", new Point3D(1317, 526, 37), Map.Felucca),
                new ControllerFix("the Militia stronghold in Yew", new Point3D(5436, 1168, 0), Map.Felucca),
                new ControllerFix("Fire Temple", new Point3D(4595, 3627, 54), Map.Felucca),
                new ControllerFix("Restricted Area", new Point3D(5438, 1165, 0), Map.Felucca),
                new ControllerFix("Esperidei's lair", new Point3D(5438, 1161, 0), Map.Felucca),
                new ControllerFix("The Palm Grove", new Point3D(1732, 3375, 0), Map.Felucca),
                new ControllerFix("Santa's Christmas Village", new Point3D(4029, 593, 0), Map.Felucca),
                new ControllerFix("the pirate stronghold on Moonglow island", new Point3D(5438, 1160, 0), Map.Felucca),
                new ControllerFix("the brigand stronghold", new Point3D(5436, 1159, 0), Map.Felucca),
                new ControllerFix("the pirate stronghold at Buccaneer's Den", new Point3D(5436, 1160, 0), Map.Felucca),
                new ControllerFix("the savage crypts", new Point3D(5436, 1161, 0), Map.Felucca),
                new ControllerFix("the orcish stronghold", new Point3D(5436, 1162, 0), Map.Felucca),
                new ControllerFix("the Council stronghold", new Point3D(5436, 1163, 0), Map.Felucca),
                new ControllerFix("the undead stronghold", new Point3D(5436, 1164, 0), Map.Felucca),
                new ControllerFix("Island Siege", new Point3D(5436, 1165, 0), Map.Felucca),
                new ControllerFix("Capture The Flag", new Point3D(98, 721, -28), Map.Ilshenar),
                new ControllerFix("Eiffel Island", new Point3D(5438, 1167, 0), Map.Felucca),
                new ControllerFix("the Militia stronghold in Vesper", new Point3D(5436, 1167, 0), Map.Felucca),
                new ControllerFix("the militia camp", new Point3D(5432, 1168, 0), Map.Felucca),
            };

        private class ControllerFix
        {
            public readonly string RegionName;
            public readonly Point3D ControllerLoc;
            public readonly Map ControllerMap;

            public ControllerFix(string regionName, Point3D controllerLoc, Map controllerMap)
            {
                RegionName = regionName;
                ControllerLoc = controllerLoc;
                ControllerMap = controllerMap;
            }
        }

        #endregion
    }
}