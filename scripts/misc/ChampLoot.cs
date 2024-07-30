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

/* Scripts/Misc/ChampLoot.cs
 * ChangeLog
 *  7/1/2023, Adam (GetChampRewards())
 *      Break 'special loot' out into it's own function so it can be used by special mobs (adam's cat for instance.)
 *  8/20/21, Adam
 *      Change up the loot level selection.
 *      Add logging - who got what / how much
 *  3/15/08, Adam 
 *      Initial Creation
 *      Moved the Champ Loot generation from BasChampion to here for generic use.
 *		(building champ level creatures that are not BasChampion)
 */

using Server.Items;
using Server.Mobiles;
using System.Collections.Generic;

namespace Server
{
    public static class ChampLootPack
    {
        /*
            Level0 = 0, // regular
            Level1 = 1, // Level0 + Ruin,	| Defense
            Level2 = 2, // Level1 + Might,	| Guarding 
            Level3 = 3, // Level2 + Force,	| Hardening
            Level4 = 4, // Level3 + Power,	| Fortification
            Level5 = 5, // Level4 + Vanq
            Level6 = 6  // Level5 + force, power, vanq | Invulnerability
         */
        public static List<Item> GetChampMagicItems()
        {
            List<Item> rewards = new(CoreChamp.AmountOfChampMagicItems);
            for (int i = 0; i < CoreChamp.AmountOfChampMagicItems; ++i)
            {
                int level = (int)Utility.RandomEnumMinMaxScaled<Loot.ImbueLevel>(CoreChamp.MinChampMagicDropLevel, CoreChamp.MaxChampMagicDropLevel);
                Item reward = null;
                if (Utility.RandomBool())
                    reward = CreateWeapon(level);
                else
                    reward = CreateArmor(level);

                rewards.Add(reward);
            }

            return rewards;
        }
        public static List<Item> GetChampSpecialRewards()
        {
            // may contain nulls
            List<Item> rewards = new(CoreChamp.AmountOfChampSpecialItems);
            for (int i = 0; i < CoreChamp.AmountOfChampSpecialItems; ++i)
            {
                Item reward = null;
                switch (Utility.Random(5))
                {
                    case 0:     // hair/beard dye
                        {       // 1 in 5 chance
                            if (Utility.RandomBool())
                                reward = new SpecialHairDye();
                            else
                                reward = new SpecialBeardDye();
                            break;
                        }

                    case 1:     // special cloth
                        {       // 1 in 5 chance
                            reward = new UncutCloth(50);
                            if (Utility.RandomBool())
                                // best ore hues (vet rewards) + really dark 'evil cloth'
                                reward.Hue = Utility.RandomList(2213, 2219, 2207, 2425, 1109);
                            else
                                reward.Hue = 0x01;  // black cloth
                            break;
                        }

                    case 2:     // Magic Item Drop
                        {       // 1 in 5 chance
                            reward = Loot.RandomClothingOrJewelry(must_support_magic: true);
                            if (reward != null)
                            {
                                int minLevel = 3;
                                int maxLevel = 3;
                                if (reward is BaseClothing)
                                    ((BaseClothing)reward).SetRandomMagicEffect(minLevel, maxLevel);
                                else if (reward is BaseJewel)
                                    ((BaseJewel)reward).SetRandomMagicEffect(minLevel, maxLevel);
                            }
                            break;
                        }

                    case 3:     // wands
                        {       // 1 in 5 chance
                            reward = new Wand();
                            if (reward != null)
                            {
                                int minLevel = 3;
                                int maxLevel = 3;
                                ((Wand)reward).SetRandomMagicEffect(minLevel, maxLevel);
                                if (Core.RuleSets.AngelIslandRules())
                                    // only ID Wands on AI
                                    ((Wand)reward).MagicEffect = MagicItemEffect.Identification;

                                // else you get a chance at the full array of wands
                            }
                            break;
                        }

                    case 4:     // drop a few single-color leather dye tubs with 100 charges
                        {       // i in 5 chance
                            reward = new LeatherArmorDyeTub();
                            break;
                        }
                }

                // may be null;
                if (reward != null && reward is not BaseWand && reward is not BaseJewel && reward is not BaseClothing)
                    reward.LootType = LootType.Rare;
                rewards.Add(reward);
            }

            return rewards;
        }
        public static List<Item> GetHarrowerRewardLoot(int maxGifts)
        {
            List<Item> rewards = new();
            Item reward = null;
            int select = 0;
            for (int i = 0; i < maxGifts; ++i)
            {
                int total_rewards = rewards.Count;
                switch ((select = Utility.Random(13)))
                {
                    case 0:         // Power/Vanq Weapon
                    case 1:
                    case 2: // 3 in 10 chance	
                        {   // 33% chance at best
                            int level = (int)Utility.RandomEnumMinMaxScaled<Loot.ImbueLevel>(CoreChamp.MinHarrowerMagicDropLevel, CoreChamp.MaxHarrowerMagicDropLevel);
                            rewards.Add(CreateWeapon(level));
                            break;
                        }
                    case 3:         // Fort/Invul Armor
                    case 4:
                    case 5: // 3 in 10 chance 
                        {   // 33% chance at best
                            int level = (int)Utility.RandomEnumMinMaxScaled<Loot.ImbueLevel>(CoreChamp.MinHarrowerMagicDropLevel, CoreChamp.MaxHarrowerMagicDropLevel);
                            rewards.Add(CreateArmor(level));
                            break;
                        }
                    case 6:     // hair/beard dye
                        {       // 1 in 10 chance
                            if (Utility.RandomBool())
                                rewards.Add(new SpecialHairDye());
                            else
                                rewards.Add(new SpecialBeardDye());
                            break;
                        }
                    case 7:     // special cloth
                        {       // 1 in 10 chance
                            reward = new UncutCloth(50);
                            if (reward != null)
                            {
                                if (Utility.RandomBool())
                                    // best ore hues (vet rewards) + really dark 'evil cloth'
                                    reward.Hue = Utility.RandomList(2213, 2219, 2207, 2425, 1109);
                                else
                                    reward.Hue = 0x01;  // black cloth

                                reward.LootType = LootType.Rare;
                                rewards.Add(reward);
                            }
                            reward = null;
                            break;
                        }

                    case 8:     // potted plant
                        {       // 1 in 10 chance
                            switch (Utility.Random(11))
                            {
                                default:    // should never happen
                                case 0: reward = new PottedCactus(); break;
                                case 1: reward = new PottedCactus1(); break;
                                case 2: reward = new PottedCactus2(); break;
                                case 3: reward = new PottedCactus3(); break;
                                case 4: reward = new PottedCactus4(); break;
                                case 5: reward = new PottedCactus5(); break;
                                case 6: reward = new PottedPlant(); break;
                                case 7: reward = new PottedPlant1(); break;
                                case 8: reward = new PottedPlant2(); break;
                                case 9: reward = new PottedTree(); break;
                                case 10: reward = new PottedTree1(); break;
                            }
                            if (reward != null)
                            {
                                reward.LootType = LootType.Rare;
                                rewards.Add(reward);
                            }
                            reward = null;
                            break;
                        }

                    case 9:     // Magic Item Drop
                        {       // 1 in 10 chance
                            if (Utility.RandomBool())
                            {
                                reward = Loot.RandomClothingOrJewelry(must_support_magic: true);
                                if (reward != null)
                                {
                                    int minLevel = 3;
                                    int maxLevel = 3;
                                    if (reward is BaseClothing)
                                        ((BaseClothing)reward).SetRandomMagicEffect(minLevel, maxLevel);
                                    else if (reward is BaseJewel)
                                        ((BaseJewel)reward).SetRandomMagicEffect(minLevel, maxLevel);
                                }
                            }
                            else
                            {
                                reward = new Wand();
                                if (reward != null)
                                {
                                    int minLevel = 3;
                                    int maxLevel = 3;
                                    ((Wand)reward).SetRandomMagicEffect(minLevel, maxLevel);
                                    if (Core.RuleSets.AngelIslandRules())
                                        // only ID Wands on AI
                                        ((Wand)reward).MagicEffect = MagicItemEffect.Identification;

                                    // else you get a chance at the full array of wands
                                }
                            }

                            if (reward != null)
                                rewards.Add(reward);

                            reward = null;
                            break;
                        }

                    case 10:    // rare reagents
                        {
                            switch (Utility.Random(8))
                            {   // necro regs
                                case 0: reward = new BatWing(); break;
                                case 1: reward = new GraveDust(); break;
                                case 2: reward = new DaemonBlood(); break;
                                case 3: reward = new NoxCrystal(); break;
                                case 4: reward = new PigIron(); break;
                                // other reagent
                                case 5: reward = new Blackmoor(); break;
                                case 6: reward = new DaemonBone(); break;
                                case 7: reward = new DeadWood(); break;
                            }
                            if (reward != null)
                            {
                                reward.LootType = LootType.Rare;
                                rewards.Add(reward);
                            }
                            reward = null;
                            break;
                        }

                    case 11:    // rare shields
                        {
                            if (Utility.RandomBool())
                            {
                                reward = new ChaosShield();
                                (reward as ChaosShield).AutoPoof = false;
                            }
                            else
                            {
                                reward = new OrderShield();
                                (reward as OrderShield).AutoPoof = false;
                            }

                            if (reward != null)
                            {
                                reward.LootType = LootType.Rare;
                                rewards.Add(reward);
                            }
                            reward = null;
                            break;
                        }
                    case 12:
                        {
                            // drop a few single-color leather dye tubs with 100 charges
                            reward = new LeatherArmorDyeTub();

                            if (reward != null)
                            {
                                reward.LootType = LootType.Rare;
                                rewards.Add(reward);
                            }
                            reward = null;
                            break;
                        }
                }

                if (total_rewards == rewards.Count)
                {   // error
                    ;
                }
            }
            return rewards;
        }
        public static List<Item> GetHarrowerMagicItems()
        {
            List<Item> list = new();
            List<Item> magicItems = new();
            List<Item> ignore = new();
            while (magicItems.Count < CoreChamp.AmountOfChampMagicItems)
            {   // get a bunch of items
                list.AddRange(GetHarrowerRewardLoot(Harrower.MaxGifts));

                foreach (Item item in list)
                    // wands are 'special' items
                    if (!ignore.Contains(item) && ((item is BaseWeapon || item is BaseArmor) && item is not Wand))
                    {
                        magicItems.Add(item);
                        if (magicItems.Count >= CoreChamp.AmountOfChampMagicItems)
                            break;
                    }
                    else
                        ignore.Add(item);
            }

            // free up unused stuffs
            foreach (Item item in list)
                if (!magicItems.Contains(item))
                    item.Delete();

            return magicItems;
        }
        public static List<Item> GetHarrowerSpecialRewards()
        {
            List<Item> list = new();
            List<Item> specialRewards = new();
            List<Item> ignore = new();
            while (specialRewards.Count < CoreChamp.AmountOfChampSpecialItems)
            {   // get a bunch of items
                list.AddRange(GetHarrowerRewardLoot(Harrower.MaxGifts));

                foreach (Item item in list)
                    // wands are 'special' items
                    if (!ignore.Contains(item) && (!(item is BaseWeapon || item is BaseArmor) || item is Wand))
                    {
                        specialRewards.Add(item);
                        if (specialRewards.Count >= CoreChamp.AmountOfChampSpecialItems)
                            break;
                    }
                    else
                        ignore.Add(item);
            }

            // free up unused stuffs
            foreach (Item item in list)
                if (!specialRewards.Contains(item))
                    item.Delete();

            return specialRewards;
        }
        public static void PackChampLoot(BaseCreature bc, int MagicItemCount, int SpecialRewardCount)
        {
            // magic items
            List<Item> magicItems = (ChampLootPack.GetChampMagicItems());
            Utility.Shuffle(magicItems);
            for (int ix = 0; ix < magicItems.Count; ix++)
                if (MagicItemCount-- > 0)
                    bc.PackItem(magicItems[ix], no_scroll: true);
                else
                    magicItems[ix].Delete();

            //  special items / rares
            List<Item> specialRewards = ChampLootPack.GetChampSpecialRewards();
            Utility.Shuffle(specialRewards);
            for (int ix = 0; ix < specialRewards.Count; ix++)
                if (SpecialRewardCount-- > 0)
                    bc.PackItem(specialRewards[ix], no_scroll: true);
                else
                    specialRewards[ix].Delete();

            return;
        }
        private static Item CreateWeapon(int level)
        {
            BaseWeapon weapon = Loot.RandomWeapon();
            weapon = (BaseWeapon)Loot.ImbueWeaponOrArmor(weapon, level);
            return weapon;
        }
        private static Item CreateArmor(int level)
        {
            BaseArmor armor = Loot.RandomArmorOrShield();
            armor = (BaseArmor)Loot.ImbueWeaponOrArmor(armor, level);
            return armor;
        }
    }
}