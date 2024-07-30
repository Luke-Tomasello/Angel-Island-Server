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
    public class DefSmith : BulkOrderSystem
    {
        private static BulkOrderSystem m_System;

        public static BulkOrderSystem System
        {
            get
            {
                if (m_System == null)
                    m_System = new DefSmith();

                return m_System;
            }
        }

        public override BulkOrderType BulkOrderType { get { return BulkOrderType.Smith; } }
        public override CraftSystem CraftSystem { get { return DefBlacksmithy.CraftSystem; } }

        public DefSmith()
            : base()
        {
            Skill = SkillName.Blacksmith;
            DeedHue = 0x44E;
        }

        public override SmallBOD ConstructSBOD(int amountMax, bool requireExceptional, BulkMaterialType material, int amountCur, Type type, int number, int graphic)
        {
            return new SmallSmithBOD(amountMax, requireExceptional, material, amountCur, type, number, graphic);
        }

        public override LargeBOD ConstructLBOD(int amountMax, bool requireExceptional, BulkMaterialType material, LargeBulkEntry[] entries)
        {
            return new LargeSmithBOD(amountMax, requireExceptional, material, entries);
        }

        public override bool UsesQuality(Type type)
        {
            return true;
        }

        public override bool UsesMaterial(Type type)
        {
            return typeof(BaseArmor).IsAssignableFrom(type);
        }

        public override int GetMaterialMessage(bool isItem)
        {
            if (isItem)
                return 1045168; // The item is not made from the requested ore.
            else
                return 1045162; // Both orders must use the same ore type.
        }

        public override SmallBulkEntry[][] GetSmallEntries()
        {
            return new SmallBulkEntry[][]
                {
                    SmallBulkEntry.BlacksmithArmor,
                    SmallBulkEntry.BlacksmithWeapons,
                };
        }

        public override SmallBulkEntry[][] GetLargeEntries()
        {
            return new SmallBulkEntry[][]
                {
                    LargeBulkEntry.LargeRing,
                    LargeBulkEntry.LargePlate,
                    LargeBulkEntry.LargeChain,
                    // TODO: Large weapon BODs?
                };
        }

        private static readonly BulkMaterial[] m_Materials = new BulkMaterial[]
            {
                new BulkMaterial( 0.0, 257, BulkMaterialType.None),
                new BulkMaterial(65.0, 128, BulkMaterialType.DullCopper),
                new BulkMaterial(70.0,  64, BulkMaterialType.ShadowIron),
                new BulkMaterial(75.0,  32, BulkMaterialType.Copper),
                new BulkMaterial(80.0,  16, BulkMaterialType.Bronze),
                new BulkMaterial(85.0,   8, BulkMaterialType.Gold),
                new BulkMaterial(90.0,   4, BulkMaterialType.Agapite),
                new BulkMaterial(95.0,   2, BulkMaterialType.Verite),
                new BulkMaterial(99.0,   1, BulkMaterialType.Valorite),
            };

        public override BulkMaterial[] GetMaterials()
        {
            return m_Materials;
        }

        #region Reward Constructors

        private static readonly ConstructCallback SturdyShovel = new ConstructCallback(CreateSturdyShovel);
        private static readonly ConstructCallback SturdyPickaxe = new ConstructCallback(CreateSturdyPickaxe);
        private static readonly ConstructCallback MiningGloves = new ConstructCallback(CreateMiningGloves);
        private static readonly ConstructCallback GargoylesPickaxe = new ConstructCallback(CreateGargoylesPickaxe);
        private static readonly ConstructCallback ProspectorsTool = new ConstructCallback(CreateProspectorsTool);
        private static readonly ConstructCallback ColoredAnvil = new ConstructCallback(CreateColoredAnvil);
        private static readonly ConstructCallback MinersMap = new ConstructCallback(CreateMinersMap);
        private static readonly ConstructCallback BlacksmithCooperative = new ConstructCallback(CreateBlacksmithCooperativeDeed);
        private static readonly ConstructCallback AncientSmithyHammer = new ConstructCallback(CreateAncientSmithyHammer);
        private static readonly ConstructCallback FortificationPowder = new ConstructCallback(CreateFortificationPowder);
        private static readonly ConstructCallback RockHammer = new ConstructCallback(CreateRockHammer);

        private static Item CreateRockHammer(int type)
        {
            return new RockHammer(type);
        }
        private static Item CreateFortificationPowder(int charges)
        {
            return new PowderOfTemperament(charges);
        }
        private static Item CreateAncientSmithyHammer(int bonus)
        {
            return new AncientSmithyHammer(bonus, 200);
        }

        private static Item CreateBlacksmithCooperativeDeed(int type)
        {
            return new BlacksmithCooperativeDeed();
        }

        private static Item CreateSturdyShovel(int type)
        {
            return new SturdyShovel();
        }

        private static Item CreateSturdyPickaxe(int type)
        {
            return new SturdyPickaxe();
        }

        private static Item CreateMiningGloves(int type)
        {
            if (type == 1)
                return new LeatherGlovesOfMining(1);
            else if (type == 3)
                return new StuddedGlovesOfMining(3);
            else if (type == 5)
                return new RingmailGlovesOfMining(5);

            throw new InvalidOperationException();
        }

        private static Item CreateGargoylesPickaxe(int type)
        {
            return new GargoylesPickaxe();
        }

        private static Item CreateProspectorsTool(int type)
        {
            return new ProspectorsTool();
        }

        private static Item CreateColoredAnvil(int type)
        {
            // Generate an anvil deed, not an actual anvil.
            //return new ColoredAnvilDeed();

            return new ColoredAnvil();
        }

        private static Item CreateMinersMap(int type) // TODO: Variate charges?
        {
            switch (type)
            {
                case 5: return new ResourceMap(CraftResource.Gold, 60);
                case 6: return new ResourceMap(CraftResource.Agapite, 60);
                case 7: return new ResourceMap(CraftResource.Verite, 60);
                case 8: return new ResourceMap(CraftResource.Valorite, 60);
            }

            throw new InvalidOperationException();
        }

        #endregion

        private static readonly RewardEntry[] m_RewardEntries = new RewardEntry[]
            {
                new RewardEntry( 10, 1, SturdyShovel),
                new RewardEntry( 25, 1, SturdyPickaxe),
                new RewardEntry( 50, 1, MiningGloves, 1),
                new RewardEntry(200, 1, GargoylesPickaxe),
                new RewardEntry(200, 1, ProspectorsTool),
                new RewardEntry(200, 1, MiningGloves, 3),
                new RewardEntry(450, 1, MiningGloves, 5),
                new RewardEntry(450, 1, MinersMap, 5),
                new RewardEntry(450, 1, FortificationPowder, 10),
                new RewardEntry(600, 1, ColoredAnvil),
                new RewardEntry(650, 1, MinersMap, 6),
                new RewardEntry(700, 1, MinersMap, 7),
                new RewardEntry(750, 1, MinersMap, 8),
                new RewardEntry(800, 1, AncientSmithyHammer, 15),
            };

        public override RewardEntry[] GetRewardEntries()
        {
            return m_RewardEntries;
        }

        private static readonly RewardOption[] m_RewardOptions = new RewardOption[]
            {
                new RewardOption( 10, 0x0F39, 0x973, "Sturdy Shovel",           SturdyShovel),
                new RewardOption( 25, 0x0E86, 0x973, "Sturdy Pickaxe",          SturdyPickaxe),
                new RewardOption( 50, 0x13C6, 0x000, "+1 Mining Gloves",        MiningGloves, 1),
                new RewardOption(200, 0x0E86, 0x973, "Gargoyle's Pickaxe",      GargoylesPickaxe),
                new RewardOption(200, 0x0FB4, 0x000, "Prospector's Tool",       ProspectorsTool),
                new RewardOption(200, 0x13D5, 0x000, "+3 Mining Gloves",        MiningGloves, 3),
                new RewardOption(450, 0x13EB, 0x000, "+5 Mining Gloves",        MiningGloves, 5),
                new RewardOption(450, 0x14EC, 0x8A5, "Miner's Map (Gold)",      MinersMap, 5),
                new RewardOption(450, 0x1006, 0x973, "Powder of Fortification", FortificationPowder, 10),
                new RewardOption(600, 0x0FAF, 0x973, "Colored Anvil",           ColoredAnvil),
                new RewardOption(650, 0x14EC, 0x979, "Miner's Map (Agapite)",   MinersMap, 6),
                new RewardOption(700, 0x14EC, 0x89F, "Miner's Map (Verite)",    MinersMap, 7),
                new RewardOption(750, 0x14EC, 0x8AB, "Miner's Map (Valorite)",  MinersMap, 8),
                new RewardOption(800, 0x13E4, 0x482, "+15 Ancient Hammer",      AncientSmithyHammer, 15),
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

            if (bod is LargeBOD)
            {
                LargeBOD lbod = (LargeBOD)bod;

                if (ContainsType(lbod.Entries, typeof(RingmailChest)))
                    points += 200;
                else if (ContainsType(lbod.Entries, typeof(ChainChest)))
                    points += 300;
                else if (ContainsType(lbod.Entries, typeof(PlateChest)))
                    points += 400;
                else if (ContainsType(lbod.Entries, typeof(Halberd)))
                    points += 200;
                else if (ContainsType(lbod.Entries, typeof(Spear)))
                    points += 300;
                else if (ContainsType(lbod.Entries, typeof(Axe)))
                    points += 350;
                else if (ContainsType(lbod.Entries, typeof(Broadsword)))
                    points += 350;
                else if (ContainsType(lbod.Entries, typeof(Mace)))
                    points += 350;
            }

            if (bod.Material >= BulkMaterialType.DullCopper && bod.Material <= BulkMaterialType.Valorite)
                points += 200 + (50 * (bod.Material - BulkMaterialType.DullCopper));

            return points; // max 1200
        }

        private static bool ContainsType(LargeBulkEntry[] entries, Type type)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Details.Type == type)
                    return true;
            }

            return false;
        }
    }
}