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

using Server.Engines.Craft;
using Server.Items;
using System;

namespace Server.Engines.BulkOrders
{
    public class DefTinker : BulkOrderSystem
    {
        private static BulkOrderSystem m_System;

        public static BulkOrderSystem System
        {
            get
            {
                if (m_System == null)
                    m_System = new DefTinker();

                return m_System;
            }
        }

        public override BulkOrderType BulkOrderType { get { return BulkOrderType.Tinker; } }
        public override CraftSystem CraftSystem { get { return DefTinkering.CraftSystem; } }

        public DefTinker()
            : base()
        {
            Skill = SkillName.Tinkering;
            DeedHue = 0x455;
        }

        public override SmallBOD ConstructSBOD(int amountMax, bool requireExceptional, BulkMaterialType material, int amountCur, Type type, int number, int graphic)
        {
            return new SmallTinkerBOD(amountMax, requireExceptional, material, amountCur, type, number, graphic);
        }

        public override LargeBOD ConstructLBOD(int amountMax, bool requireExceptional, BulkMaterialType material, LargeBulkEntry[] entries)
        {
            return new LargeTinkerBOD(amountMax, requireExceptional, material, entries);
        }

        public override bool UsesQuality(Type type)
        {
            return true;
        }

        public override bool UsesMaterial(Type type)
        {
            return typeof(BaseArmor).IsAssignableFrom(type);
        }

        public override SmallBulkEntry[][] GetSmallEntries()
        {
            return new SmallBulkEntry[0][];
        }

        public override SmallBulkEntry[][] GetLargeEntries()
        {
            return new SmallBulkEntry[0][];
        }

        private static readonly BulkMaterial[] m_Materials = new BulkMaterial[]
            {
            };

        public override BulkMaterial[] GetMaterials()
        {
            return m_Materials;
        }

        private static readonly RewardEntry[] m_RewardEntries = new RewardEntry[]
            {
            };

        public override RewardEntry[] GetRewardEntries()
        {
            return m_RewardEntries;
        }

        private static readonly RewardOption[] m_RewardOptions = new RewardOption[]
            {
            };

        public override RewardOption[] GetRewardOptions()
        {
            return m_RewardOptions;
        }

        public override int ComputePoints(BaseBOD bod)
        {
            return 0;
        }
    }
}