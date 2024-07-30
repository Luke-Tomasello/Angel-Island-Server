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

/* Scripts/Engines/Plants/PlantTypes.cs
 *  3/28/22, Adam
 *      Added a bunch of new seeds
 *  3/26/22, Adam
 *      Update use of  PlantType.TREE_FIRST/LAST to work correctly
 *      Add RandomTreeSeed() to get a random tree seed (ignores plants)
 *      Add wrapper classes for all the tree seeds to:
 *          1. give them a human readable and meaningful name (we want players to use this to decorate selectively.)
 *          2. randomize different looking / same trees. There are 7 different Madrones, 2 Scrub Oaks, 2 Walnuts, etc.
 *          Note: the wrapper is lost when 'collecting seeds' from the trees themselves. this is fine and expected behavior.
 *  3/24/22, Adam
 *      Add support for trees
 *	10/10/05, Pix
 *		Fix for Hedge hue.
 *  04/07/05, Kitaras
 *	 Added check to set seed name to a solen seed if is a hedge plant type.
 */

using Server.Targeting;

namespace Server.Engines.Plants
{
    #region Old tree conversion
    [TypeAlias(
        "Server.Engines.Plants.AppleSeed",
        "Server.Engines.Plants.BananaSeed",
        "Server.Engines.Plants.BareTreeSeed",
        "Server.Engines.Plants.CedarSeed",
        "Server.Engines.Plants.CoconutPalmSeed",
        "Server.Engines.Plants.CypressSeed",
        "Server.Engines.Plants.DatePalmSeed",
        "Server.Engines.Plants.OakSeed",
        "Server.Engines.Plants.OhiiSeed",
        "Server.Engines.Plants.PeachSeed",
        "Server.Engines.Plants.PearSeed",
        "Server.Engines.Plants.SaplingSeed",
        "Server.Engines.Plants.ScrubOakSeed",
        "Server.Engines.Plants.SpiderSeed",
        "Server.Engines.Plants.MadroneSeed",
        "Server.Engines.Plants.WalnutSeed",
        "Server.Engines.Plants.WillowSeed",
        "Server.Engines.Plants.YuccaSeed")]
    #endregion
    public class Seed : Item
    {
        private PlantType m_PlantType;
        private PlantHue m_PlantHue;
        private bool m_ShowType;

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantType PlantType
        {
            get { return m_PlantType; }
            set
            {
                m_PlantType = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantHue PlantHue
        {
            get { return m_PlantHue; }
            set
            {
                m_PlantHue = value;
                Hue = PlantHueInfo.GetInfo(value).Hue;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowType
        {
            get { return m_ShowType; }
            set
            {
                m_ShowType = value;
                InvalidateProperties();
            }
        }

        public override string DefaultName
        {
            get
            {
                if (m_PlantType == PlantType.Hedge)
                    return "a solen seed";
                else if (TreeSeed.IsTree(m_PlantType))
                    return "a tree seed";
                else
                    return "a seed";
            }
        }

        public override int LabelNumber { get { return 1060810; } } // seed

        public override double DefaultWeight { get { return 1.0; } }

        [Constructable]
        public Seed()
            : this(PlantTypeInfo.RandomFirstGeneration(), PlantHueInfo.RandomFirstGeneration(), false)
        {
        }

        [Constructable]
        public Seed(PlantType plantType, PlantHue plantHue, bool showType)
            : base(0xDCF)
        {
            m_PlantType = plantType;
            m_PlantHue = plantHue;
            m_ShowType = showType;

            Hue = PlantHueInfo.GetInfo(plantHue).Hue;
        }

        public Seed(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            PlantHueInfo hueInfo = PlantHueInfo.GetInfo(m_PlantHue);

            if (m_ShowType)
            {
                PlantTypeInfo typeInfo = PlantTypeInfo.GetInfo(m_PlantType);
                list.Add(hueInfo.IsBright() ? 1061918 : 1061917, "#" + hueInfo.Name.ToString() + "\t" + typeInfo.Argument); // [bright] ~1_COLOR~ ~2_TYPE~ seed
            }
            else
            {
                list.Add(hueInfo.IsBright() ? 1060839 : 1060838, "#" + hueInfo.Name.ToString()); // [bright] ~1_val~ seed
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
                return;
            }

            from.Target = new InternalTarget(this);
            LabelTo(from, 1061916); // Choose a bowl of dirt to plant this seed in.
        }

        private class InternalTarget : Target
        {
            private Seed m_Seed;

            public InternalTarget(Seed seed)
                : base(3, false, TargetFlags.None)
            {
                m_Seed = seed;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                try
                {
                    if (m_Seed.Deleted || !(targeted is Item))
                        return;

                    Item item = (Item)targeted;

                    if (!m_Seed.IsChildOf(from.Backpack))
                    {
                        from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
                    }
                    else if (TreeSeed.IsTree(m_Seed.PlantType) && !(targeted is StaticPlantItem))
                    {
                        item.LabelTo(from, "You cannot plant a tree in a bowl of dirt!");
                    }
                    else if (targeted is PlantItem)
                    {
                        PlantItem plant = (PlantItem)targeted;

                        plant.PlantSeed(from, m_Seed);
                    }
                    else
                    {
                        item.LabelTo(from, 1061919); // You must use a seed on a bowl of dirt!
                    }
                }
                catch (System.Exception ex)
                {
                    Diagnostics.LogHelper.LogException(ex);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((int)m_PlantType);
            writer.Write((int)m_PlantHue);
            writer.Write((bool)m_ShowType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version < 1)
                m_PlantType = (PlantType)OldTreeConv(reader.ReadInt());
            else
                m_PlantType = (PlantType)reader.ReadInt();

            m_PlantHue = (PlantHue)reader.ReadInt();
            m_ShowType = reader.ReadBool();

            if (version < 2)
            {
                if (m_PlantType == PlantType.Hedge)
                {
                    m_PlantHue = PlantHue.Plain;
                    m_ShowType = true;
                }

                Name = null;
            }
        }

        #region Old tree conversion

        public static int OldTreeConv(int v)
        {
            int index = v - 18;

            switch (index)
            {
                case 00: return (int)PlantType.AppleTree; // AppleNoFruitTree
                case 01: return (int)PlantType.AppleTree; // AppleTree
                case 02: return (int)PlantType.BananaTree; // BananaTree
                case 03: return (int)PlantType.BareTree; // BareTree1
                case 04: return (int)PlantType.BareTree; // BareTree10
                case 05: return (int)PlantType.BareTree; // BareTree11
                case 06: return (int)PlantType.BareTree; // BareTree12
                case 07: return (int)PlantType.BareTree; // BareTree13
                case 08: return (int)PlantType.BareTree; // BareTree14
                case 09: return (int)PlantType.BareTree; // BareTree15
                case 10: return (int)PlantType.BareTree; // BareTree16
                case 11: return (int)PlantType.BareTree; // BareTree17
                case 12: return (int)PlantType.BareTree; // BareTree18
                case 13: return (int)PlantType.BareTree; // BareTree19
                case 14: return (int)PlantType.BareTree; // BareTree2
                case 15: return (int)PlantType.BareTree; // BareTree20
                case 16: return (int)PlantType.BareTree; // BareTree21
                case 17: return (int)PlantType.BareTree; // BareTree22
                case 18: return (int)PlantType.BareTree; // BareTree23
                case 19: return (int)PlantType.BareTree; // BareTree24
                case 20: return (int)PlantType.BareTree; // BareTree25
                case 21: return (int)PlantType.BareTree; // BareTree26
                case 22: return (int)PlantType.BareTree; // BareTree27
                case 23: return (int)PlantType.BareTree; // BareTree28
                case 24: return (int)PlantType.BareTree; // BareTree29
                case 25: return (int)PlantType.BareTree; // BareTree3
                case 26: return (int)PlantType.BareTree; // BareTree30
                case 27: return (int)PlantType.BareTree; // BareTree31
                case 28: return (int)PlantType.BareTree; // BareTree32
                case 29: return (int)PlantType.BareTree; // BareTree33
                case 30: return (int)PlantType.BareTree; // BareTree4
                case 31: return (int)PlantType.BareTree; // BareTree5
                case 32: return (int)PlantType.BareTree; // BareTree6
                case 33: return (int)PlantType.BareTree; // BareTree7
                case 34: return (int)PlantType.BareTree; // BareTree8
                case 35: return (int)PlantType.BareTree; // BareTree9
                case 36: return (int)PlantType.CedarTree; // CedarTree
                case 37: return (int)PlantType.CedarTree; // CedarTree2
                case 38: return (int)PlantType.CoconutPalm; // CoconutPalm
                case 39: return (int)PlantType.CypressTree; // CypressTree
                case 40: return (int)PlantType.DatePalm; // DatePalm
                case 41: return (int)PlantType.OakTree; // OakTree
                case 42: return (int)PlantType.OhiiTree; // OhiiTree
                case 43: return (int)PlantType.PeachTree; // PeachNoFruitTree
                case 44: return (int)PlantType.PeachTree; // PeachTree
                case 45: return (int)PlantType.PearTree; // PearFruitTree
                case 46: return (int)PlantType.PearTree; // PearTree
                case 47: return (int)PlantType.WalnutTree; // RedWalnutTree
                case 48: return (int)PlantType.WillowTree; // RedWillowTree
                case 49: return (int)PlantType.Sapling; // Sapling
                case 50: return (int)PlantType.BananaTree; // SmallBananaTree
                case 51: return (int)PlantType.PearTree; // SmallPearFruitTree
                case 52: return (int)PlantType.PearTree; // SmallPearTree
                case 53: return (int)PlantType.SpiderTree; // SpiderTree
                case 54: return (int)PlantType.BirchTree; // Tree1 (scrub oak)
                case 55: return (int)PlantType.BirchTree; // Tree2 (scrub oak)
                case 56: return (int)PlantType.Madrone; // Tree3 (Madrone)
                case 57: return (int)PlantType.Madrone; // Tree4 (Madrone)
                case 58: return (int)PlantType.Madrone; // Tree5 (Madrone)
                case 59: return (int)PlantType.Madrone; // Tree6 (Madrone)
                case 60: return (int)PlantType.Madrone; // Tree7 (Madrone)
                case 61: return (int)PlantType.Madrone; // Tree8 (Madrone)
                case 62: return (int)PlantType.BirchTree; // Tree9 (scrub oak)
                case 63: return (int)PlantType.WalnutTree; // WalnutTree
                case 64: return (int)PlantType.WalnutTree; // WalnutTree2
                case 65: return (int)PlantType.WillowTree; // WillowTree
                case 66: return (int)PlantType.BananaTree; // YoungBananaTree
                case 67: return (int)PlantType.OakTree; // YoungOakTree
                case 68: return (int)PlantType.Yucca; // Yucca
            }

            return v;
        }

        #endregion
    }
}