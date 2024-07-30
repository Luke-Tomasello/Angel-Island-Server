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

/* Scripts/Engines/Plants/TreeSeed.cs
 *  10/19/23, Yoar
 *      Reworked tree seeds.
 */

namespace Server.Engines.Plants
{
    public enum TreeType
    {
        AppleTree,
        BananaTree,
        BareTree,
        BirchTree,
        CedarTree,
        CoconutPalm,
        CypressTree,
        DatePalm,
        Madrone,
        OakTree,
        OhiiTree,
        PeachTree,
        PearTree,
        Sapling,
        SpiderTree,
        WalnutTree,
        WillowTree,
        Yucca,
    }

    public class TreeSeed : Seed
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public TreeType TreeType
        {
            get { return GetTreeType(PlantType); }
            set { PlantType = GetPlantType(value); }
        }

        [Constructable]
        public TreeSeed()
            : this(RandomTree())
        {
        }

        [Constructable]
        public TreeSeed(TreeType treeType)
            : base(GetPlantType(treeType), PlantHue.Plain, true)
        {
        }

        public TreeSeed(Serial serial)
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

        public static TreeType RandomTree()
        {
            return (TreeType)Utility.Random(18);
        }

        public static bool IsTree(PlantType plantType)
        {
            int index = (int)plantType - 18;

            return (index >= 0 && index < 18);
        }

        public static PlantType GetPlantType(TreeType treeType)
        {
            return (PlantType)(18 + (int)treeType);
        }

        public static TreeType GetTreeType(PlantType plantType)
        {
            return (TreeType)((int)plantType - 18);
        }
    }
}