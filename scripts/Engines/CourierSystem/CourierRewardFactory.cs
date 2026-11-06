using Server.Items;
using Server.Mobiles;
using static Server.Loot;

namespace Server.Engines.CourierSystem
{
    public static class CourierRewardFactory
    {
        public static Item CreateReward(BaseVendor destination, ImbueLevel rewardLevel)
        {
            rewardLevel =
                CourierRewardCalculator.Normalize(rewardLevel);

            CourierRewardType rewardType =
                CourierRewardInventory.SelectRewardType(destination);

            VendorCourierBag bag =
                new VendorCourierBag(rewardLevel, rewardType);

            PackReward(
                bag,
                rewardType,
                rewardLevel);

            if (bag.Items.Count == 0)
                bag.DropItem(new Gold(GetFallbackGold(rewardLevel)));

            return bag;
        }

        private static void PackReward(
            VendorCourierBag bag,
            CourierRewardType rewardType,
            ImbueLevel level)
        {
            switch (rewardType)
            {
                case CourierRewardType.MeleeWeapon:
                    PackMeleeWeaponReward(bag, level);
                    break;

                case CourierRewardType.RangedWeapon:
                    PackRangedWeaponReward(bag, level);
                    break;

                case CourierRewardType.Armor:
                    PackArmorReward(bag, level);
                    break;

                case CourierRewardType.Shield:
                    PackShieldReward(bag, level);
                    break;

                case CourierRewardType.Reagent:
                    PackReagentReward(bag, level);
                    break;

                case CourierRewardType.Potion:
                    PackPotionReward(bag, level);
                    break;

                case CourierRewardType.Scroll:
                    PackScrollReward(bag, level);
                    break;

                case CourierRewardType.Wand:
                    PackWandReward(bag, level);
                    break;

                case CourierRewardType.Clothing:
                    PackClothingReward(bag, level);
                    break;

                case CourierRewardType.Jewelry:
                    PackJewelryReward(bag, level);
                    break;

                case CourierRewardType.Gold:
                default:
                    PackGoldReward(bag, level);
                    break;
            }
        }
        private static void PackMeleeWeaponReward(VendorCourierBag bag, ImbueLevel level)
        {
            FinishImbue(Loot.RandomMeleeWeapon(), bag, level);
        }

        private static void PackRangedWeaponReward(VendorCourierBag bag, ImbueLevel level)
        {
            FinishImbue(Loot.RandomRangedWeapon(), bag, level);
        }

        private static void PackArmorReward(VendorCourierBag bag, ImbueLevel level)
        {
            FinishImbue(Loot.RandomArmor(), bag, level);
        }

        private static void PackShieldReward(VendorCourierBag bag, ImbueLevel level)
        {
            FinishImbue(Loot.RandomShield(), bag, level);
        }
        private static void PackReagentReward(VendorCourierBag bag, ImbueLevel level)
        {
            TreasureMapChest.PackRegs(bag, (int)level * 5);
        }

        private static void PackPotionReward(VendorCourierBag bag, ImbueLevel level)
        {
            for (int ix = (int)level; ix > 0; ix--)
            {
                Item pot = Loot.RandomPotion();
                pot.Amount = Utility.RandomMinMax(1, (int)level);
                bag.StackItem(pot);
            }
        }

        private static void PackScrollReward(VendorCourierBag bag, ImbueLevel level)
        {
            int maxCircle = level switch
            {
                ImbueLevel.Level1 => 3,
                ImbueLevel.Level2 => 4,
                ImbueLevel.Level3 => 5,
                ImbueLevel.Level4 => 6,
                ImbueLevel.Level5 => 7,
                _ => 3
            };

            for (int ix = Utility.RandomMinMax(1, (int)level + 1); ix > 0; ix--)
                TreasureMapChest.PackScroll(
                    bag,
                    minCircle: 1,
                    maxCircle: maxCircle);
        }

        private static void PackWandReward(VendorCourierBag bag, ImbueLevel level)
        {
            int[] minmax = ImbueLevelToMagicEnchantment(level);
            bag.DropItem(Loot.RandomMagicWand(minmax[0], minmax[1], chance: 1));
        }

        private static void PackClothingReward(VendorCourierBag bag, ImbueLevel level)
        {
            int[] minmax = ImbueLevelToMagicEnchantment(level);
            bag.DropItem(Loot.RandomMagicClothing(minmax[0], minmax[1], chance: 1));
        }

        private static void PackJewelryReward(VendorCourierBag bag, ImbueLevel level)
        {
            int[] minmax = ImbueLevelToMagicEnchantment(level);
            bag.DropItem(Loot.RandomMagicJewelry(minmax[0], minmax[1], chance: 1));
        }

        private static void PackGoldReward(VendorCourierBag bag, ImbueLevel level)
        {
            bag.DropItem(new Gold(GetFallbackGold(level)));
        }
        private static int GetFallbackGold(ImbueLevel level)
        {   // standard treasuremap gold /2
            int amount = ((int)level * 1000) / 2;
            return amount;
        }

        private static void GetImbueRange(ImbueLevel level, out int min, out int max)
        {
            switch (level)
            {
                default:
                case ImbueLevel.Level1:
                    min = max = 1;
                    break;

                case ImbueLevel.Level2:
                    min = max = 2;
                    break;

                case ImbueLevel.Level3:
                    min = 2;
                    max = 3;
                    break;

                case ImbueLevel.Level4:
                    min = 3;
                    max = 4;
                    break;

                case ImbueLevel.Level5:
                    min = 3;
                    max = 5;
                    break;
            }
        }
        private static void FinishImbue(Item item, VendorCourierBag bag, ImbueLevel level)
        {
            GetImbueRange(level, out int min, out int max);

            bag.DropItem(
                Loot.ImbueWeaponOrArmor(item, min, max));
        }
    }
}