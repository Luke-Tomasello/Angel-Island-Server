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
using System;

namespace Server.Engines.BulkOrders
{
    public enum BulkGenericType
    {
        None,

        Iron,
        Cloth,
        Leather,
        Wood,
    }

    public class BGTClassifier
    {
        public static BulkGenericType Classify(BODType deedType, Type itemType)
        {
            switch (deedType)
            {
                case BODType.Smith:
                case BODType.Tinker:
                    {
                        return BulkGenericType.Iron;
                    }
                case BODType.Tailor:
                    {
                        if (itemType == null || itemType.IsSubclassOf(typeof(BaseArmor)) || itemType.IsSubclassOf(typeof(BaseShoes)))
                            return BulkGenericType.Leather;
                        else
                            return BulkGenericType.Cloth;
                    }
                case BODType.Carpenter:
                case BODType.Fletcher:
                    {
                        return BulkGenericType.Wood;
                    }
            }

            return BulkGenericType.None;
        }
    }

    public struct BulkGenericInfo
    {
        public static readonly BulkGenericInfo Empty = new BulkGenericInfo();

        private static readonly BulkGenericInfo[] m_Table = new BulkGenericInfo[]
            {
                Empty,

                new BulkGenericInfo(1062226, CraftResource.Iron),
                new BulkGenericInfo(1044286, CraftResource.None),
                new BulkGenericInfo(1062235, CraftResource.RegularLeather),
                new BulkGenericInfo(1079435, CraftResource.RegularWood),
            };

        public static BulkGenericInfo Lookup(BulkGenericType type)
        {
            int index = (int)type;

            if (index >= 0 && index < m_Table.Length)
                return m_Table[index];

            return Empty;
        }

        private int m_Number;
        private CraftResource m_Resource;

        public int Number { get { return m_Number; } }
        public CraftResource Resource { get { return m_Resource; } }

        public BulkGenericInfo(int number, CraftResource resource)
        {
            m_Number = number;
            m_Resource = resource;
        }
    }
}