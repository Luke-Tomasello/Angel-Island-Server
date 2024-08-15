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

/* Scripts/items/misc/SeedBox.cs
 * ChangeLog
 *  8/20/23, Yoar
 *      Complete refactor
 *      Disallow dropping of seeds that aren't supported by the seed box
 *  12/21/06, Adam
 *      Use IBlockRazorSearch interface handle unwanted OnDoubleClick messages sent from razor.
 *	3/30/06 Taran Kain
 *		Added drop sound when a seed is added to the box.
 *  3/27/06 Taran Kain
 *		Removed OnAmountChange(), added CheckHold call to OnDragDrop to ensure we follow container rules
 *	10/15/05, erlein
 *		Replaced weird character in SendMessage() string to accurately display "You cannot access this."
 *	10/10/05, Pix
 *		Changes for PlantHue.None mutant fixing.
 *	05/27/05, Adam
 *		Comment	out	debug output
 *	05/08/05, Kit
 *	Added in override to totalitems	to keep	item count working,	added in serlization for security level
 *	05/06/05, Kit
 *	Modified runuo release for
 *	Added support for solen	seeds, added checks	for	exceeding lock downs of	house
 */

using Custom.Gumps;
using Server.Engines.Plants;
using Server.Multis;
using Server.Network;
using Server.Regions;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class SeedBox : Item
    {
        public const int MaxSeeds = 500;

        public override string DefaultName { get { return "Seed Box"; } }
        public override double DefaultWeight { get { return 60.0; } }

        private Dictionary<SeedIndex, int> m_Counts;
        private int m_ItemCount;

        private SecureLevel m_Level;

        public Dictionary<SeedIndex, int> Counts
        {
            get { return m_Counts; }
        }

        public int ItemCount
        {
            get { return m_ItemCount; }
            set { m_ItemCount = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level
        {
            get { return m_Level; }
            set { m_Level = value; }
        }

        public virtual TypeInfo[] TypeTable { get { return m_PlantTable; } }

        public override void UpdateTotals()
        {
            base.UpdateTotals();

            SetTotalItems(m_ItemCount);
        }

        [Constructable]
        public SeedBox()
            : base(0x9A9)
        {
            Hue = 0x1CE;

            m_Counts = new Dictionary<SeedIndex, int>();
            m_ItemCount = 0;

            m_Level = SecureLevel.CoOwners;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, 1060662, string.Format("Seeds\t{0}", SeedCount())); // ~1_val~: ~2_val~
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060662, "Seeds\t{0}", SeedCount()); // ~1_val~: ~2_val~
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else if (!CheckAccess(from))
            {
                from.SendLocalizedMessage(500447); // That is not accessible.
            }
            else
            {
                from.CloseGump(typeof(SeedBoxGump));
                from.SendGump(new SeedBoxGump(this));
            }
        }

        public bool CheckAccess(Mobile m)
        {
            if (!IsLockedDown || m.AccessLevel >= AccessLevel.GameMaster)
                return true;

            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house != null)
            {
                if (house.IsAosRules && (house.Public ? house.IsBanned(m) : !house.HasAccess(m)))
                    return false;

                return (house != null && house.HasSecureAccess(m, m_Level));
            }

            TownshipRegion tsr = TownshipRegion.GetTownshipAt(this);

            if (tsr != null && tsr.TStone != null && tsr.TStone.HasAccess(m, Township.TownshipAccess.Member))
                return true;

            return false;
        }

        #region Serialization

        public SeedBox(Serial serial)
            : base(serial)
        {
            m_Counts = new Dictionary<SeedIndex, int>();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            writer.Write((int)m_Level);

            writer.Write((int)m_ItemCount);

            writer.Write((int)m_Counts.Count);

            foreach (KeyValuePair<SeedIndex, int> kvp in m_Counts)
            {
                writer.Write((int)kvp.Key.TypeIndex);
                writer.Write((int)kvp.Key.HueIndex);
                writer.Write((int)kvp.Value);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt(); // version

            switch (version)
            {
                case 3:
                case 2:
                    {
                        m_Level = (SecureLevel)reader.ReadInt();

                        m_ItemCount = reader.ReadInt();
                        SetTotalItems(m_ItemCount);

                        if (version >= 3)
                        {
                            int entryCount = reader.ReadInt();

                            for (int i = 0; i < entryCount; i++)
                            {
                                int typeIndex = reader.ReadInt();
                                int hueIndex = reader.ReadInt();
                                int count = reader.ReadInt();

                                if (count != 0)
                                    m_Counts[new SeedIndex(typeIndex, hueIndex)] = count;
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 18; i++)
                            {
                                for (int j = 0; j < 21; j++)
                                {
                                    int count = reader.ReadInt();

                                    if (count != 0)
                                        m_Counts[new SeedIndex(i, j)] = count;
                                }
                            }
                        }

                        break;
                    }
                case 1:
                    {
                        m_Level = (SecureLevel)reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        m_ItemCount = reader.ReadInt();
                        SetTotalItems(m_ItemCount);

                        for (int i = 0; i < 18; i++)
                        {
                            for (int j = 0; j < 20; j++)
                            {
                                int count = reader.ReadInt();

                                if (count != 0)
                                    m_Counts[new SeedIndex(i, j)] = count;
                            }
                        }

                        break;
                    }
            }
        }

        #endregion

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (!CheckAccess(from))
            {
                from.SendMessage("You cannot access this");
                return false;
            }

            if (!(dropped is Seed))
            {
                from.SendMessage("You can only store seeds in this box.");
                return false;
            }

            Seed seed = (Seed)dropped;

            if (!IsValidPlantType(seed.PlantType) || !IsValidPlantHue(seed.PlantHue))
            {
                from.SendMessage("The seed box cannot hold this kind of seed.");
                return false;
            }

            if (SeedCount() >= MaxSeeds)
            {
                from.SendMessage("The seed box cannot hold any more seeds.");
                return false;
            }

            BaseHouse house = BaseHouse.FindHouseAt(from);

            if (house != null && house.IsLockedDown(this) && house.SumLockDownSecureCount >= house.MaxLockDowns)
            {
                from.SendMessage("This would exceed the houses lockdown limit.");
                return false;
            }

            AddSeed(seed.PlantType, seed.PlantHue);

            Container cont = Parent as Container;

            // calling the full version with (-itemcount - 1) prevents double-counting the seedbox
            if (cont != null && !cont.CheckHold(from, this, true, true, -TotalItems - 1, (int)(-TotalWeight - Weight)))
            {
                RemoveSeed(seed.PlantType, seed.PlantHue);
                return false;
            }

            int dropSound = dropped.GetDropSound();

            if (dropSound == -1)
                dropSound = 0x42;

            from.SendSound(dropSound, GetWorldLocation());

            dropped.Delete();

            from.CloseGump(typeof(SeedBoxGump));
            from.SendGump(new SeedBoxGump(this, PlantTypeToIndex(seed.PlantType)));

            return true;
        }

        public void AddSeed(PlantType plantType, PlantHue plantHue)
        {
            SeedIndex key = new SeedIndex(PlantTypeToIndex(plantType), PlantHueToIndex(plantHue));

            if (!m_Counts.ContainsKey(key))
                m_Counts[key] = 1;
            else
                m_Counts[key]++;

            m_ItemCount = SeedCount() / 5;
            TotalItems = m_ItemCount;

            InvalidateProperties();
        }

        public void RemoveSeed(PlantType plantType, PlantHue plantHue)
        {
            SeedIndex key = new SeedIndex(PlantTypeToIndex(plantType), PlantHueToIndex(plantHue));

            if (!m_Counts.ContainsKey(key))
                return;

            if (m_Counts[key] == 1)
                m_Counts.Remove(key);
            else
                m_Counts[key]--;

            m_ItemCount = SeedCount() / 5;
            TotalItems = m_ItemCount;

            InvalidateProperties();
        }

        public int SeedCount()
        {
            int total = 0;

            foreach (KeyValuePair<SeedIndex, int> kvp in m_Counts)
                total += kvp.Value;

            return total;
        }

        private static readonly TypeInfo[] m_PlantTable = new TypeInfo[]
            {
                new TypeInfo(PlantType.CampionFlowers,    0xC83, "Campion Flowers"), // 0
                new TypeInfo(PlantType.Poppies,           0xC86, "Poppies"), // 1
                new TypeInfo(PlantType.Snowdrops,         0xC88, "Snowdrops"), // 2
                new TypeInfo(PlantType.Bulrushes,         0xC94, "Bulrushes"), // 3
                new TypeInfo(PlantType.Lilies,            0xC8B, "Lilies"), // 4
                new TypeInfo(PlantType.PampasGrass,       0xCA5, "Pampas Grass"), // 5
                new TypeInfo(PlantType.Rushes,            0xCA7, "Rushes"), // 6
                new TypeInfo(PlantType.ElephantEarPlant,  0xC97, "Elephant Ear Plant"), // 7
                new TypeInfo(PlantType.Fern,              0xC9F, "Fern"), // 8
                new TypeInfo(PlantType.PonytailPalm,      0xCA6, "Ponytail Palm"), // 9
                new TypeInfo(PlantType.SmallPalm,         0xC9C, "Small Palm"), // 10
                new TypeInfo(PlantType.CenturyPlant,      0xD31, "Century Plant"), // 11
                new TypeInfo(PlantType.WaterPlant,        0xD04, "Water Plant"), // 12
                new TypeInfo(PlantType.SnakePlant,        0xCA9, "Snake Plant"), // 13
                new TypeInfo(PlantType.PricklyPearCactus, 0xD2C, "Prickly Pear Cactus"), // 14
                new TypeInfo(PlantType.BarrelCactus,      0xD26, "Barrel Cactus"), // 15
                new TypeInfo(PlantType.TribarrelCactus,   0xD27, "Tribarrel Cactus"), // 16
                new TypeInfo(PlantType.Hedge,             0xC8F, "Hedge"), // 17
            };

        public static TypeInfo[] PlantTable { get { return m_PlantTable; } }

        private static readonly TypeInfo[] m_TreeTable = new TypeInfo[]
            {
                new TypeInfo(PlantType.AppleTree,   0xCE9, "Apple Tree"),
                new TypeInfo(PlantType.BananaTree,  0xCE9, "Banana Tree"),
                new TypeInfo(PlantType.BareTree,    0xCE9, "Bare Tree"),
                new TypeInfo(PlantType.BirchTree,   0xCE9, "Birch Tree"),
                new TypeInfo(PlantType.CedarTree,   0xCE9, "Cedar Tree"),
                new TypeInfo(PlantType.CoconutPalm, 0xCE9, "Coconut Palm"),
                new TypeInfo(PlantType.CypressTree, 0xCE9, "Cypress Tree"),
                new TypeInfo(PlantType.DatePalm,    0xCE9, "Date Palm"),
                new TypeInfo(PlantType.Madrone,     0xCE9, "Madrone"),
                new TypeInfo(PlantType.OakTree,     0xCE9, "Oak Tree"),
                new TypeInfo(PlantType.OhiiTree,    0xCE9, "O'hii Tree"),
                new TypeInfo(PlantType.PeachTree,   0xCE9, "Peach Tree"),
                new TypeInfo(PlantType.PearTree,    0xCE9, "Pear Tree"),
                new TypeInfo(PlantType.Sapling,     0xCE9, "Sapling"),
                new TypeInfo(PlantType.SpiderTree,  0xCE9, "Spider Tree"),
                new TypeInfo(PlantType.WalnutTree,  0xCE9, "Walnut Tree"),
                new TypeInfo(PlantType.WillowTree,  0xCE9, "Willow Tree"),
                new TypeInfo(PlantType.Yucca,       0xCE9, "Yucca"),
            };

        public static TypeInfo[] TreeTable { get { return m_TreeTable; } }

        private static readonly HueInfo[] m_HueTable = new HueInfo[]
            {
                new HueInfo(PlantHue.Black,        0x455, "Black Mutation"), // 0
                new HueInfo(PlantHue.White,        0x481, "White Mutation"), // 1
                new HueInfo(PlantHue.Red,          0x66D, "Red"), // 2
                new HueInfo(PlantHue.BrightRed,    0x021, "Bright Red"), // 3
                new HueInfo(PlantHue.Green,        0x59B, "Green"), // 4
                new HueInfo(PlantHue.BrightGreen,  0x042, "Bright Green"), // 5
                new HueInfo(PlantHue.Blue,         0x53D, "Blue"), // 6
                new HueInfo(PlantHue.BrightBlue,   0x005, "Bright Blue"), // 7
                new HueInfo(PlantHue.Yellow,       0x8A5, "Yellow"), // 8
                new HueInfo(PlantHue.BrightYellow, 0x038, "Bright Yellow"), // 9
                new HueInfo(PlantHue.Orange,       0x46F, "Orange"), // 10
                new HueInfo(PlantHue.BrightOrange, 0x02B, "Bright Orange"), // 11
                new HueInfo(PlantHue.Purple,       0x00D, "Purple"), // 12
                new HueInfo(PlantHue.BrightPurple, 0x010, "Bright Purple"), // 13
                new HueInfo(PlantHue.Magenta,      0x486, "Rare Magenta"), // 14
                new HueInfo(PlantHue.Pink,         0x48E, "Rare Pink"), // 15
                new HueInfo(PlantHue.FireRed,      0x489, "Rare Fire Red"), // 16
                new HueInfo(PlantHue.Aqua,         0x495, "Rare Aqua"), // 17
                new HueInfo(PlantHue.Plain,        0x163, "Plain"), // 18
                new HueInfo(PlantHue.Solen,        0x03D, "Solen"), // 19
                new HueInfo(PlantHue.None,         0x03D, "Other Mutation"), // 20
            };

        public static HueInfo[] HueTable { get { return m_HueTable; } }

        #region Utility

        public TypeInfo GetTypeInfo(int index)
        {
            TypeInfo[] typeTable = TypeTable;

            if (index >= 0 && index < typeTable.Length)
                return typeTable[index];

            return null;
        }

        public TypeInfo GetTypeInfo(PlantType plantType)
        {
            TypeInfo[] typeTable = TypeTable;

            for (int i = 0; i < typeTable.Length; i++)
            {
                if (typeTable[i].PlantType == plantType)
                    return typeTable[i];
            }

            return null;
        }

        public PlantType IndexToPlantType(int index)
        {
            TypeInfo typeInfo = GetTypeInfo(index);

            if (typeInfo != null)
                return typeInfo.PlantType;

            return PlantType.CampionFlowers;
        }

        public int PlantTypeToIndex(PlantType plantType)
        {
            TypeInfo[] typeTable = TypeTable;

            for (int i = 0; i < typeTable.Length; i++)
            {
                if (typeTable[i].PlantType == plantType)
                    return i;
            }

            return 0; // campion flowers
        }

        public bool IsValidPlantType(PlantType plantType)
        {
            return (GetTypeInfo(plantType) != null);
        }

        public static HueInfo GetHueInfo(int index)
        {
            if (index >= 0 && index < m_HueTable.Length)
                return m_HueTable[index];

            return null;
        }

        public static HueInfo GetHueInfo(PlantHue plantHue)
        {
            for (int i = 0; i < m_HueTable.Length; i++)
            {
                if (m_HueTable[i].PlantHue == plantHue)
                    return m_HueTable[i];
            }

            return null;
        }

        public static PlantHue IndexToPlantHue(int index)
        {
            HueInfo hueInfo = GetHueInfo(index);

            if (hueInfo != null)
                return hueInfo.PlantHue;

            return PlantHue.Plain;
        }

        public static int PlantHueToIndex(PlantHue plantHue)
        {
            for (int i = 0; i < m_HueTable.Length; i++)
            {
                if (m_HueTable[i].PlantHue == plantHue)
                    return i;
            }

            return 18; // plain
        }

        public static bool IsValidPlantHue(PlantHue plantHue)
        {
            return (GetHueInfo(plantHue) != null);
        }

        #endregion

        public class TypeInfo
        {
            private PlantType m_PlantType;
            private int m_ItemID;
            private string m_Name;

            public PlantType PlantType { get { return m_PlantType; } }
            public int ItemID { get { return m_ItemID; } }
            public string Name { get { return m_Name; } }

            public TypeInfo(PlantType plantType, int itemID, string name)
            {
                m_PlantType = plantType;
                m_ItemID = itemID;
                m_Name = name;
            }
        }

        public class HueInfo
        {
            private PlantHue m_PlantHue;
            private int m_Hue;
            private string m_Name;

            public PlantHue PlantHue { get { return m_PlantHue; } }
            public int Hue { get { return m_Hue; } }
            public string Name { get { return m_Name; } }

            public HueInfo(PlantHue plantHue, int hue, string name)
            {
                m_PlantHue = plantHue;
                m_Hue = hue;
                m_Name = name;
            }
        }

        public struct SeedIndex : IEquatable<SeedIndex>
        {
            private int m_TypeIndex;
            private int m_HueIndex;

            public int TypeIndex { get { return m_TypeIndex; } }
            public int HueIndex { get { return m_HueIndex; } }

            public SeedIndex(int typeIndex, int hueIndex)
            {
                m_TypeIndex = typeIndex;
                m_HueIndex = hueIndex;
            }

            public override bool Equals(object obj)
            {
                return (obj is SeedIndex && Equals((SeedIndex)obj));
            }

            bool IEquatable<SeedIndex>.Equals(SeedIndex other)
            {
                return (m_TypeIndex == other.m_TypeIndex && m_HueIndex == other.m_HueIndex);
            }

            public override int GetHashCode()
            {
                return (m_TypeIndex.GetHashCode() ^ m_HueIndex.GetHashCode());
            }
        }
    }

    public class TreeSeedBox : SeedBox
    {
        public override string DefaultName { get { return "Tree Seed Box"; } }

        public override TypeInfo[] TypeTable { get { return TreeTable; } }

        [Constructable]
        public TreeSeedBox()
            : base()
        {
        }

        public TreeSeedBox(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}