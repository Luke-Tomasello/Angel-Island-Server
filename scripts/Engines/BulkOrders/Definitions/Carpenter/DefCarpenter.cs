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
using Server.Mobiles;
using System;

namespace Server.Engines.BulkOrders
{
    public class DefCarpenter : BulkOrderSystem
    {
        private static BulkOrderSystem m_System;

        public static BulkOrderSystem System
        {
            get
            {
                if (m_System == null)
                    m_System = new DefCarpenter();

                return m_System;
            }
        }

        public override BulkOrderType BulkOrderType { get { return BulkOrderType.Carpenter; } }
        public override CraftSystem CraftSystem { get { return DefCarpentry.CraftSystem; } }

        public DefCarpenter()
            : base()
        {
            Skill = SkillName.Carpentry;
            DeedHue = 0x5E8;
        }

        public override SmallBOD ConstructSBOD(int amountMax, bool requireExceptional, BulkMaterialType material, int amountCur, Type type, int number, int graphic)
        {
            return new SmallCarpenterBOD(amountMax, requireExceptional, material, amountCur, type, number, graphic);
        }

        public override LargeBOD ConstructLBOD(int amountMax, bool requireExceptional, BulkMaterialType material, LargeBulkEntry[] entries)
        {
            return new LargeCarpenterBOD(amountMax, requireExceptional, material, entries);
        }

        public override bool UsesQuality(Type type)
        {
            return (typeof(BaseArmor).IsAssignableFrom(type) || typeof(BaseInstrument).IsAssignableFrom(type) || typeof(BaseWeapon).IsAssignableFrom(type)); // TODO: Include BaseCraftableItem, BaseContainer?
        }

        public override bool UsesMaterial(Type type)
        {
            return (type != typeof(Arrow) && type != typeof(Bolt));
        }

        private static readonly SmallBulkEntry[][] m_BulkEntries = new SmallBulkEntry[][]
            {
                new SmallBulkEntry[] // Weapons and Shields
                    {
                        new SmallBulkEntry(typeof(WoodenShield), 1027034, 0x1B7A),
                        new SmallBulkEntry(typeof(Club), 1025044, 0x13B4),
                        new SmallBulkEntry(typeof(GnarledStaff), 1025112, 0x13F8),
                        new SmallBulkEntry(typeof(QuarterStaff), 1023721, 0xE89),
                        new SmallBulkEntry(typeof(ShepherdsCrook), 1023713, 0xE81),
                    },
                new SmallBulkEntry[] // Furniture
                    {
                        new SmallBulkEntry(typeof(WoodenChair), 1044301, 0xB57),
                        new SmallBulkEntry(typeof(WoodenBench), 1022860, 0xB2D),
                        new SmallBulkEntry(typeof(WoodenThrone), 1044304, 0xB2E),
                        new SmallBulkEntry(typeof(WritingTable), 1022890, 0xB4A),
                    },
                new SmallBulkEntry[] // Containers
                    {
                        new SmallBulkEntry(typeof(WoodenBox), 1023709, 0x9AA),
                        new SmallBulkEntry(typeof(EmptyBookcase), 1022718, 0xA9D),
                        new SmallBulkEntry(typeof(Armoire), 1022643, 0xA4F),
                        new SmallBulkEntry(typeof(FancyArmoire), 1044312, 0xA4D),
                    },
                new SmallBulkEntry[] // Instruments
                    {
                        new SmallBulkEntry(typeof(Drums), 1023740, 0xE9C),
                        new SmallBulkEntry(typeof(LapHarp), 1023762, 0xEB2),
                        new SmallBulkEntry(typeof(Lute), 1023763, 0xEB3),
                        new SmallBulkEntry(typeof(Harp), 1023761, 0xEB1),
                    },
                new SmallBulkEntry[] // Ammunition
                    {
                        new SmallBulkEntry(typeof(Arrow), 1023903, 0xF3F),
                        new SmallBulkEntry(typeof(Bolt), 1027163, 0x1BFB),
                    },
                new SmallBulkEntry[] // Bows
                    {
                        new SmallBulkEntry(typeof(Bow), 1025042, 0x13B2),
                        new SmallBulkEntry(typeof(Crossbow), 1023919, 0xF50),
                        new SmallBulkEntry(typeof(HeavyCrossbow), 1025117, 0x13FD),
                    },
            };

        public override SmallBulkEntry[][] GetSmallEntries()
        {
            return m_BulkEntries;
        }

        public override SmallBulkEntry[][] GetLargeEntries()
        {
            return m_BulkEntries;
        }

        private static readonly BulkMaterial[] m_Materials = new BulkMaterial[]
            {
                new BulkMaterial( 0.0, 257, BulkMaterialType.None),
                new BulkMaterial(65.0, 192, BulkMaterialType.Oak),
                new BulkMaterial(75.0,  32, BulkMaterialType.Ash),
                new BulkMaterial(80.0,  24, BulkMaterialType.Yew),
                new BulkMaterial(90.0,   4, BulkMaterialType.Heartwood),
                new BulkMaterial(95.0,   2, BulkMaterialType.Bloodwood),
                new BulkMaterial(99.0,   1, BulkMaterialType.Frostwood),
            };

        public override BulkMaterial[] GetMaterials()
        {
            return m_Materials;
        }

        private static Item CreateFurnitureDyeTub(int type)
        {
            FurnitureDyeTub tub = new FurnitureDyeTub();

            tub.LootType = LootType.Regular;
            tub.UsesRemaining = type;

            return tub;
        }

        private static readonly RewardEntry[] m_RewardEntries = new RewardEntry[]
            {
                new RewardEntry( 10, 1, new ItemReward(typeof(SturdyHatchet))),
                new RewardEntry(200, 1, new ItemReward(typeof(CarpentersAxe))),
                new RewardEntry(350, 1, CreateFurnitureDyeTub, 10),
                new RewardEntry(400, 1, new RandomReward(typeof(SturdyPackHorseDeed), typeof(SturdyPackLlamaDeed))),
                new RewardEntry(450, 1, new ItemReward(typeof(ResourceMap), CraftResource.YewWood, 60)),
                new RewardEntry(500, 1, new ItemReward(typeof(RepairDeed), RepairDeed.RepairSkillType.Carpentry, 100.0)),
                new RewardEntry(600, 1, new ItemReward(typeof(ArcheryButteDeed))),
                new RewardEntry(650, 1, new ItemReward(typeof(ResourceMap), CraftResource.Heartwood, 60)),
                new RewardEntry(700, 1, new ItemReward(typeof(ResourceMap), CraftResource.Bloodwood, 60)),
                new RewardEntry(750, 1, new ItemReward(typeof(ResourceMap), CraftResource.Frostwood, 60)),
                new RewardEntry(800, 1, new ItemReward(typeof(CarpentersToolbox))),
            };

        public override RewardEntry[] GetRewardEntries()
        {
            return m_RewardEntries;
        }

        private static readonly RewardOption[] m_RewardOptions = new RewardOption[]
            {
                new RewardOption( 10, 0x0F43, 0x973, "Sturdy Hatchet", new ItemReward(typeof(SturdyHatchet))),
                new RewardOption(200, 0x0F43, 0x000, "Carpenter's Axe", new ItemReward(typeof(CarpentersAxe))),
                new RewardOption(350, 0x0FAB, 0x000, "Furniture Dye Tub (10 Charges)", CreateFurnitureDyeTub, 10),
                new RewardOption(400, 0x14F0, 0x000, "Sturdy Pack Horse", new ItemReward(typeof(SturdyPackHorseDeed))),
                new RewardOption(400, 0x14F0, 0x000, "Sturdy Pack Llama", new ItemReward(typeof(SturdyPackLlamaDeed))),
                new RewardOption(450, 0x14EC, 0x4A8, "Lumberjack's Map (Yew Wood)", new ItemReward(typeof(ResourceMap), CraftResource.YewWood, 60)),
                new RewardOption(500, 0x14F0, 0x1BC, "Carpentry Repair Deed (100 skill)", new ItemReward(typeof(RepairDeed), RepairDeed.RepairSkillType.Carpentry, 100.0)),
                new RewardOption(600, 0x14F0, 0x000, "Archery Butte", new ItemReward(typeof(ArcheryButteDeed))),
                new RewardOption(650, 0x14EC, 0x4A9, "Lumberjack's Map (Heartwood)", new ItemReward(typeof(ResourceMap), CraftResource.Heartwood, 60)),
                new RewardOption(700, 0x14EC, 0x612, "Lumberjack's Map (Bloodwood)", new ItemReward(typeof(ResourceMap), CraftResource.Bloodwood, 60)),
                new RewardOption(750, 0x14EC, 0xB8F, "Lumberjack's Map (Frostwood)", new ItemReward(typeof(ResourceMap), CraftResource.Frostwood, 60)),
                new RewardOption(800, 0x1EBB, 0x000, "Master Carpenter's Toolbox", new ItemReward(typeof(CarpentersToolbox))),
            };

        public override RewardOption[] GetRewardOptions()
        {
            return m_RewardOptions;
        }

        public override int ComputePoints(BaseBOD bod)
        {
            int points = 0;

            switch (bod.AmountMax)
            {
                case 10: points += 10; break;
                case 15: points += 25; break;
                case 20: points += 50; break;
            }

            if (bod.RequireExceptional)
                points += 200;

            if (bod.Material >= BulkMaterialType.Oak && bod.Material <= BulkMaterialType.Frostwood)
                points += 300 + (50 * (bod.Material - BulkMaterialType.Oak));

            if (bod is LargeBOD)
            {
                LargeBOD lbod = (LargeBOD)bod;

                if (lbod.Entries.Length == 2)
                    points += 100;
                else if (lbod.Entries.Length == 3)
                    points += 200;
                else if (lbod.Entries.Length == 4)
                    points += 300;
                else if (lbod.Entries.Length == 5)
                    points += 400;
            }

            return points; // max 800
        }

        public override int ComputeGold(BaseBOD bod, int points)
        {
            /* 1/20/23, Yoar
             * Vendors value carpentry craftables much lower than smithing/tailoring craftables (or armor/weapons)
             * Let's use the craft-value formula vor carpentry BODs
             */

            bool temp = RewardGoldByCraftValue;

            RewardGoldByCraftValue = true;

            int gold = base.ComputeGold(bod, points);

            RewardGoldByCraftValue = temp;

            return gold;
        }
    }
}