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

using Server.Items;

namespace Server.Engines.BulkOrders
{
    public enum BulkMaterialType
    {
        None,

        DullCopper,
        ShadowIron,
        Copper,
        Bronze,
        Gold,
        Agapite,
        Verite,
        Valorite,

        Spined,
        Horned,
        Barbed,

        Oak,
        Ash,
        Yew,
        Heartwood,
        Bloodwood,
        Frostwood,
    }

    public class BulkMaterial
    {
        private double m_ReqSkill;
        private int m_Weight;
        private BulkMaterialType m_Material;

        public double ReqSkill { get { return m_ReqSkill; } }
        public int Weight { get { return m_Weight; } }
        public BulkMaterialType Type { get { return m_Material; } }

        public BulkMaterial(double reqSkill, int weight, BulkMaterialType material)
        {
            m_ReqSkill = reqSkill;
            m_Weight = weight;
            m_Material = material;
        }
    }

    public struct BulkMaterialInfo
    {
        public static readonly BulkMaterialInfo Empty = new BulkMaterialInfo();

        private static readonly BulkMaterialInfo[] m_Table = new BulkMaterialInfo[]
            {
                Empty,

                new BulkMaterialInfo(1018332, 1045142, CraftResource.DullCopper),
                new BulkMaterialInfo(1018333, 1045143, CraftResource.ShadowIron),
                new BulkMaterialInfo(1018334, 1045144, CraftResource.Copper),
                new BulkMaterialInfo(1018335, 1045145, CraftResource.Bronze),
                new BulkMaterialInfo(1018336, 1045146, CraftResource.Gold),
                new BulkMaterialInfo(1018337, 1045147, CraftResource.Agapite),
                new BulkMaterialInfo(1018338, 1045148, CraftResource.Verite),
                new BulkMaterialInfo(1018339, 1045149, CraftResource.Valorite),

                new BulkMaterialInfo(1062236, 1049348, CraftResource.SpinedLeather),
                new BulkMaterialInfo(1062237, 1049349, CraftResource.HornedLeather),
                new BulkMaterialInfo(1062238, 1049350, CraftResource.BarbedLeather),

                new BulkMaterialInfo("Oak", "All items must be made with oak wood.", CraftResource.OakWood),
                new BulkMaterialInfo("Ash", "All items must be made with ash wood.", CraftResource.AshWood),
                new BulkMaterialInfo("Yew", "All items must be made with yew wood.", CraftResource.YewWood),
                new BulkMaterialInfo("Heartwood", "All items must be made with heartwood.", CraftResource.Heartwood),
                new BulkMaterialInfo("Bloodwood", "All items must be made with bloodwood.", CraftResource.Bloodwood),
                new BulkMaterialInfo("Frostwood", "All items must be made with frostwood.", CraftResource.Frostwood),
            };

        public static BulkMaterialInfo Lookup(BulkMaterialType type)
        {
            int index = (int)type;

            if (index >= 0 && index < m_Table.Length)
                return m_Table[index];

            return Empty;
        }

        private TextEntry m_Name;
        private TextEntry m_RequireLabel;
        private CraftResource m_Resource;

        public TextEntry Name { get { return m_Name; } }
        public TextEntry RequireLabel { get { return m_RequireLabel; } }
        public CraftResource Resource { get { return m_Resource; } }

        public BulkMaterialInfo(TextEntry name, TextEntry requireLabel, CraftResource resource)
        {
            m_Name = name;
            m_RequireLabel = requireLabel;
            m_Resource = resource;
        }
    }
}