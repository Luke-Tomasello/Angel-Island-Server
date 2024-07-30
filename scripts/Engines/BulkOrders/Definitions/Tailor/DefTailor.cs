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
using static Server.Items.DyeTubToning;

namespace Server.Engines.BulkOrders
{
    public class DefTailor : BulkOrderSystem
    {
        private static BulkOrderSystem m_System;

        public static BulkOrderSystem System
        {
            get
            {
                if (m_System == null)
                    m_System = new DefTailor();

                return m_System;
            }
        }

        public override BulkOrderType BulkOrderType { get { return BulkOrderType.Tailor; } }
        public override CraftSystem CraftSystem { get { return DefTailoring.CraftSystem; } }

        public DefTailor()
            : base()
        {
            Skill = SkillName.Tailoring;
            DeedHue = 0x483;
        }

        public override SmallBOD ConstructSBOD(int amountMax, bool requireExceptional, BulkMaterialType material, int amountCur, Type type, int number, int graphic)
        {
            return new SmallTailorBOD(amountMax, requireExceptional, material, amountCur, type, number, graphic);
        }

        public override LargeBOD ConstructLBOD(int amountMax, bool requireExceptional, BulkMaterialType material, LargeBulkEntry[] entries)
        {
            return new LargeTailorBOD(amountMax, requireExceptional, material, entries);
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
                return 1049352; // The item is not made from the requested leather type.
            else
                return 1049351; // Both orders must use the same leather type.
        }

        public override SmallBulkEntry[][] GetSmallEntries()
        {
            return new SmallBulkEntry[][]
                {
                    SmallBulkEntry.TailorLeather,
                    SmallBulkEntry.TailorCloth,
                };
        }

        public override SmallBulkEntry[][] GetLargeEntries()
        {
            return new SmallBulkEntry[][]
                {
                    LargeBulkEntry.Farmer,
                    LargeBulkEntry.FemaleLeatherSet,
                    LargeBulkEntry.FisherGirl,
                    LargeBulkEntry.Gypsy,
                    LargeBulkEntry.HatSet,
                    LargeBulkEntry.Jester,
                    LargeBulkEntry.Lady,
                    LargeBulkEntry.MaleLeatherSet,
                    LargeBulkEntry.Pirate,
                    LargeBulkEntry.ShoeSet,
                    LargeBulkEntry.StuddedSet,
                    LargeBulkEntry.TownCrier,
                    LargeBulkEntry.Wizard,
                    LargeBulkEntry.BoneSet,
                };
        }

        private static readonly BulkMaterial[] m_Materials = new BulkMaterial[]
            {
                new BulkMaterial( 0.0, 439, BulkMaterialType.None),
                new BulkMaterial(65.0,  64, BulkMaterialType.Spined),
                new BulkMaterial(80.0,   8, BulkMaterialType.Horned),
                new BulkMaterial(99.0,   1, BulkMaterialType.Barbed),
            };

        public override BulkMaterial[] GetMaterials()
        {
            return m_Materials;
        }

        #region Reward Constructors

        private static readonly ConstructCallback SewingKit = new ConstructCallback(CreateSewingKit);
        private static readonly ConstructCallback Cloth = new ConstructCallback(CreateCloth);
        private static readonly ConstructCallback Sandals = new ConstructCallback(CreateSandals);
        private static readonly ConstructCallback StretchedHide = new ConstructCallback(CreateStretchedHide);
        private static readonly ConstructCallback Tapestry = new ConstructCallback(CreateTapestry);
        private static readonly ConstructCallback BearRug = new ConstructCallback(CreateBearRug);
        private static readonly ConstructCallback KnittingNeedles = new ConstructCallback(CreateKnittingNeedles);
        private static readonly ConstructCallback ClothesArmoire = new ConstructCallback(CreateClothesArmoire);
        private static readonly ConstructCallback HarvestersKnife = new ConstructCallback(CreateHarvestersKnife);
        private static readonly ConstructCallback TailorsSewingKit = new ConstructCallback(CreateTailorsSewingKit);

        private static Item CreateSewingKit(int type)
        {
            return new SewingKit(type);
        }

        // special dye tub colors
        private static ColorInfo[][] m_ColorTable = new ColorInfo[][]
            {
                new ColorInfo[]
                {
                new ColorInfo( 1230, 6, "Violet" ),          // Violet           1230 - 1235 (6)
                new ColorInfo( 1501, 8, "Tan" ),             // Tan              1501 - 1508 (8)
                new ColorInfo( 2013, 5, "Brown" ),           // Brown            2012 - 2017 (5)
                new ColorInfo( 1303, 6, "Dark Blue" ),       // Dark Blue        1303 - 1308 (6)
                new ColorInfo( 1420, 7, "Forest Green" ),    // Forest Green     1420 - 1426 (7)
                new ColorInfo( 1619, 8, "Pink" ),            // Pink             1619 - 1626 (8)
                new ColorInfo( 1640, 5, "Crimson" ),         // Crimson          1640 - 1644 (5) - renamed from "Red" to "Crimson"
                new ColorInfo( 2001, 5, "Olive" ),           // Olive            2001 - 2005 (5)
                },

                new ColorInfo[]
                {
                new ColorInfo( 2419, 6, "Dull Copper" ),     // Dull Copper      2419 - 2424 (6)
                new ColorInfo( 2406, 7, "Shadow Iron" ),     // Shadow Iron      2406 - 2412 (7)
                new ColorInfo( 2413, 6, "Copper" ),          // Copper           2413 - 2418 (6)
                new ColorInfo( 2414, 5, "Bronze" ),          // Bronze           2414 - 2418 (5)
                new ColorInfo( 2213, 6, "Gold" ),            // Gold             2213 - 2218 (6)
                new ColorInfo( 2425, 6, "Agapite" ),         // Agapite          2425 - 2430 (6)
                new ColorInfo( 2207, 6, "Verite" ),          // Verite           2207 - 2212 (6)
                new ColorInfo( 2219, 6, "Valorite" ),        // Valorite         2219 - 2224 (6)
                },

                new ColorInfo[]
                {
                new ColorInfo( 2113, 6, "Red" ),             // Red              2113 - 2118 (6)
                new ColorInfo( 2119, 6, "Blue" ),            // Blue             2119 - 2124 (6)
                new ColorInfo( 2126, 5, "Green" ),           // Green            2126 - 2130 (5)
                // yellow is a duplicate of Gold above
                new ColorInfo( 2213, 6, "Yellow" ),          // Yellow           2213 - 2218 (6)
                }
            };

        private static int HueLookup(ColorInfo[] table)
        {
            ColorInfo ci = table[Utility.Random(table.Length)];
            return ci.BaseHue + Utility.Random(ci.Shades);
        }

        private static Item CreateCloth(int type)
        {
            UncutCloth cloth = new UncutCloth(1);

            switch (type)
            {
                case 1:// level 1 cloth
                    cloth.Hue = HueLookup(m_ColorTable[2]); // red/green/blue
                    cloth.Amount = 50;
                    break;
                case 2:// level 2 cloth
                    cloth.Hue = HueLookup(m_ColorTable[1]); // metal hues
                    cloth.Amount = 50;
                    break;
                case 3:// level 3 cloth
                    cloth.Hue = HueLookup(m_ColorTable[0]); // lovely colors
                    cloth.Amount = 50;
                    break;
                case 4: // black cloth
                    cloth.Hue = 0x01;
                    cloth.Amount = 15;  // keep it rare, especially on such a small shard
                    break;
            }

            return cloth;
        }

        private static Item CreateSandals(int type)
        {
            int hue = HueLookup(m_ColorTable[2]);
            Sandals sandals = new Sandals(hue);
            if (type == 2)
            {
                sandals.LootType = LootType.Blessed;
                if (Utility.Chance(0.01))
                    sandals.Hue = 0x01;
            }
            return sandals;
        }

        private static Item CreateStretchedHide(int type)
        {
            switch (Utility.Random(4))
            {
                default:
                case 0: return new SmallStretchedHideEastDeed();
                case 1: return new SmallStretchedHideSouthDeed();
                case 2: return new MediumStretchedHideEastDeed();
                case 3: return new MediumStretchedHideSouthDeed();
            }
        }

        private static Item CreateTapestry(int type)
        {
            switch (Utility.Random(4))
            {
                default:
                case 0: return new LightFlowerTapestryEastDeed();
                case 1: return new LightFlowerTapestrySouthDeed();
                case 2: return new DarkFlowerTapestryEastDeed();
                case 3: return new DarkFlowerTapestrySouthDeed();
            }
        }

        private static Item CreateBearRug(int type)
        {
            switch (Utility.Random(4))
            {
                default:
                case 0: return new BrownBearRugEastDeed();
                case 1: return new BrownBearRugSouthDeed();
                case 2: return new PolarBearRugEastDeed();
                case 3: return new PolarBearRugSouthDeed();
            }
        }

        private static Item CreateKnittingNeedles(int type)
        {
            return new KnittingNeedles(type);
        }

        private static Item CreateClothesArmoire(int type)
        {
            switch (type)
            {
                case 1:
                    return new SturdyArmoire();
                case 2:
                    return new SturdyFancyArmoire();
            }
            return null;
        }

        private static Item CreateHarvestersKnife(int type)
        {
            return new HarvestersKnife();
        }

        private static Item CreateTailorsSewingKit(int type)
        {
            return new TailorsSewingKit();
        }

        #endregion

        private static readonly RewardEntry[] m_RewardEntries = new RewardEntry[]
            {
                new RewardEntry( 10, 1, SewingKit, 250),
                new RewardEntry( 10, 1, Cloth, 1),
                new RewardEntry( 25, 1, KnittingNeedles, 50),
                new RewardEntry( 50, 1, Cloth, 2),
                new RewardEntry(100, 1, Cloth, 3),
                new RewardEntry(150, 1, Sandals, 1),
                new RewardEntry(200, 1, Cloth, 4),
                new RewardEntry(200, 1, HarvestersKnife),
                new RewardEntry(250, 1, StretchedHide),
                new RewardEntry(250, 1, ClothesArmoire, 1),
                new RewardEntry(300, 1, ClothesArmoire, 2),
                new RewardEntry(300, 1, Tapestry),
                new RewardEntry(300, 1, BearRug),
                new RewardEntry(300, 1, Sandals, 2),
                new RewardEntry(300, 1, TailorsSewingKit),
                //new RewardEntry(300, 1, ClothingBlessDeed),
            };

        public override RewardEntry[] GetRewardEntries()
        {
            return m_RewardEntries;
        }

        private static readonly RewardOption[] m_RewardOptions = new RewardOption[]
            {
                new RewardOption( 10, 0x0F9D, 0x000, "Sewing Kit (250 Charges)",  SewingKit, 250),
                new RewardOption( 10, 0x1765, 0x000, "Colored Cloth (Tier 1)",    Cloth, 1),
                new RewardOption( 25, 0x0DF7, 0x000, "Knitting Sticks",           KnittingNeedles, 50),
                new RewardOption( 50, 0x1765, 0x000, "Colored Cloth (Tier 2)",    Cloth, 2),
                new RewardOption(100, 0x1765, 0x000, "Colored Cloth (Tier 3)",    Cloth, 3),
                new RewardOption(150, 0x170D, 0x000, "Colored Sandals",           Sandals, 1),
                new RewardOption(200, 0x1765, 0x001, "Jet Black Cloth",           Cloth, 4),
                new RewardOption(200, 0x13F6,  1191, "Harvester's Knife",         HarvestersKnife),
                new RewardOption(250, 0x14F0, 0x000, "Stretched Hide",            StretchedHide),
                new RewardOption(250, 0x0A4F, 0x000, "Clothes Armoire",           ClothesArmoire, 1),
                new RewardOption(300, 0x0A4D, 0x000, "Fancy Clothes Armoire",     ClothesArmoire, 2),
                new RewardOption(300, 0x14F0, 0x000, "Tapestry",                  Tapestry),
                new RewardOption(300, 0x14F0, 0x000, "BearRug",                   BearRug),
                new RewardOption(300, 0x170D, 0x000, "Colored Sandals (blessed)", Sandals, 2),
                new RewardOption(300, 0x0F9D, 0x000, "Master Tailor's Sewing Kit", TailorsSewingKit),
                //new RewardEntry(300, 0x14F0, 0x000, "Clothing Bless Deed",        ClothingBlessDeed),
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
                points += 100;

            if (bod is LargeBOD)
            {
                LargeBOD lbod = (LargeBOD)bod;

                if (lbod.Entries.Length == 4)
                    points += 300;
                else if (lbod.Entries.Length == 5)
                    points += 400;
                else if (lbod.Entries.Length == 6)
                    points += 500;
            }

            if (bod.Material == BulkMaterialType.Spined)
                points += 50;
            else if (bod.Material == BulkMaterialType.Horned)
                points += 100;
            else if (bod.Material == BulkMaterialType.Barbed)
                points += 150;

            return points; // max 800
        }
    }
}