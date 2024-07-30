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

/* Scripts/Misc/Loot.cs
 * ChangeLog
 *  7/19/2023, Adam (Loot.FlipTable)
 *      Add the Loot.FlipTable for those flipable items that have no [flipable meta data, a 'wall torch' for example
 *  3/28/23, Adam
 *      Add new magic GnarledStaff - liches drop these
 *  3/23/23, Adam (Slayer Weapons)
 *      We'll be going with Silver only for Siege unless we can get more info wrt Slayers being available at the time (Publish 13.6)
 *      -----------
 *      History:
 *      Initially, the Slayer property was called Virtue Weapons, applied only to melee weapons, and operated in a far different manner than currently.
 *      The very first Slayer weapons in the game were Undead slayers, but were known as Silver weapons.
 *      Publish 16 (July 12-22, 2002) saw the introduction of Slayer Musical Instruments. They they worked the same way as Slayer melee weapons. However, they increased the chance of a successful use of the Barding skill that was mentioned in the tool tip, instead of doing double damage to the creatures for they were meant.
 *      https://www.uoguide.com/Slayer
 *      (Slayer Instruments)
 *      Slayer instruments didn't appear until Publish 16, 2002, so they are out with regard to Siege.
 *  8/9/22, Adam
 *      Add ChecMagicGearThrottle to determine if we should be throttling magic gear drops
 *      use Utility.RandomEnumValue<T>() for getting a random enumeration value. Used for armor and weapon attibs
 *  1/12/22, Adam (m_PvPWandTypes)
 *      Turn off PvP style wands
 *  11/30/21, Adam
 *  	Cleanup ImbueWeaponOrArmor()
 *  11/23/21, Adam
 *      Fix % drops of unusual keys.
 *  11/15/21, Adam
 *      Add support for magic weapons and armor throttle
 *          1. replace magic gear with one of several rares or other desirables including "an unusual key"
 *          2. Delete "an unusual key"s that were unassigned. 
 *          3. add loot table for level6 dungeon chests
 *          4. add loot table for "an unusual key"s
 *	3/2/11, Adam
 *		Turn on LibraryBookTypes
 *	5/18/10, Adam
 *		Add/repair the following baby rare factory items light
 *		0x0A07, // Wall torch (facing east) - LightType.WestBig
 * 		0x0A0C, // Wall torch (facing south) - LightType.NorthBig
 * 		0x09FE, // Wall sconce (facing east) - LightType.WestBig
 * 		0x0A02, // Wall sconce (facing south) - LightType.NorthBig
 * 		0x0B26, // Tall candlabra (unlit 0xA29) - LightType.Circle225;
 *	5/10/10, Adam
 *		Add a baby rare factory.
 *		Most creatures now have a small chance to drop something cool!
 *	9/18/05, Adam
 *		a. Add new "Britannian Militia" IOBs
 *		b. make IOB's Scissorable = false;
 *	7/13/05, erlein
 *		Added GenEScroll to handle enchanted scroll drops.
 *	4/26/05, Adam
 *		Added the HammerPick to weapon types
 *	4/20/05, Kit
 *		Added Daggers to weapon types
 * 	4/12/05, Adam
 *		Fix: Don't hue bows or staves.
 * 	4/11/05, Adam
 *		Move the hueing code to Loot.ImbueWeaponOrArmor
 *		reduce Shadow chance (ugly) and replace with Copper and Bronze
 *		Don't hue bows or staves.
 *	3/29/05, Adam
 *		Tweaks to ImbueWeaponOrArmor() table values.
 *	3/28/05, Adam
 *		Add new method ImbueWeaponOrArmor() to Imbue weapons and armor with
 *		magical properties.
 *	12/28/05, Adam
 *		Add in old style 'orc colored' masks
 *  1/15/05,Froste
 *      Set Dyable to false for IOB Items and added "pirate boots" for IOBAlignment.Pirate
 *  12/21/04, Froste
 *      Added "blood drenched sash" for Council IOBAlignment, added "a pirate skullcap" for IOBAlignment.Pirate
 *  11/16/04, Froste
 *      Added "Savage Mask" for IOBAlignment.Savage
 *  11/10/04/Froste,
 *      Implemented new random IOB drop system and added "sandals of the walking dead"
 *	7/6/04, Adam
 *		turn back on Magic Item Drops
 *	7/6/04, Adam
 *		turn off RandomClothingOrJewelry()
 *		map RandomClothingOrJewelry() to RandomGem()
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Diagnostics;   // LogHelper
using Server.Engines.CrownSterlingSystem;
using Server.Items;
using System;
using System.Collections.Generic;
using static Server.Utility;

namespace Server
{
    public class Loot
    {
        #region Rares
        [Flags]
        public enum RareType
        {
            invalid,
            MonsterDrop = 0x02,
            DungeonChestDrop = 0x04,
            TMapChestDrop = 0x08,
            UnusualChestDrop = 0x10,
            DungeonChestDropL6 = 0x20,
            CrystallinePowder = 0x40,
            ScarecrowEast = 0x80,
            RareUncutCloth = 0x100,
            RareBoltOfCloth = 0x200,
            PlowSouth = 0x400,
            PlowEast = 0x800,
            ScarecrowSouth = 0x1000,
            FruitBasket = 0x2000,
        }
        #region Flip Table
        public static Dictionary<int, int[]> FlipTable = new()
        {
            {0x1502, new int[]{0x1501}},   // Plough (south)
            {0x1501, new int[]{0x1502}},   // Plough (east)
            {0x1A9A, new int[]{0x1A99}}, // Flax (south)
            {0x1A99, new int[]{0x1A9A}}, // Flax (east)
            {0x1BD9, new int[]{0x1BDC}}, // Stack of boards (south)
            {0x1BDC, new int[]{0x1BD9}}, // Stack of boards (east)
            {0x1BDE, new int[]{0x1BE1}}, // Logs (east)
            {0x1BE1, new int[]{0x1BDE}}, // Logs (south)
            {0x0A07, new int[]{0x0A0C}}, // Wall torch (facing east) - LightType.WestBig
            {0x0A0C, new int[]{0x0A07}}, // Wall torch (facing south) - LightType.NorthBig
            {0x09FE, new int[]{0x0A02}}, // Wall sconce (facing east) - LightType.WestBig
            {0x0A02, new int[]{0x09FE}}, // Wall sconce (facing south) - LightType.NorthBig
            {0x0A92, new int[]{0x0A93}}, // Folded sheet (east)
            {0x0A93, new int[]{0x0A92}}, // Folded sheet (south)
            {0x1230, new int[]{0x125E}}, // Guillotine blade top (east)
            {0x125E, new int[]{0x1230}}, // Guillotine blade top (south)
            {0x1260, new int[]{0x1246}}, // Guillotine blade center (east)
            {0x1246, new int[]{0x1260}}, // Guillotine blade center(south)
            {0x09F9, new int[]{0x09F8}}, // a spoon (east)
            {0x09F8, new int[]{0x09F9}}, // a spoon (south)
            {0x1AE0, new int[]{0x1AE1}}, // a skull (east)
            {0x1AE1, new int[]{0x1AE0}}, // a skull (south)
            {0x1AE2, new int[]{0x1AE3}}, // a skull (east)
            {0x1AE3, new int[]{0x1AE2}}, // a skull (south)
            {0x185b, new int[]{0x185C}}, // empty vials (east) 
            {0x185C, new int[]{0x185b}}, // empty vials (south)
            {0x185D, new int[]{0x185E}}, // full vials (east)
            {0x185E, new int[]{0x185D}}, // full vials (south)
            {0x0995, new int[]{0x0996}}, // a ceramic mug 1
            {0x0996, new int[]{0x0995}}, // a ceramic mug 2
            {0x0997, new int[]{0x0999}}, // a ceramic mug 1
            {0x0999, new int[]{0x0997}}, // a ceramic mug 2
            {0x0FFB, new int[]{0x0ffc}}, // skull mug 1
            {0x0ffc, new int[]{0x0FFB}}, // skull mug 2
            {0x0FFD, new int[]{0x0FFE}}, // skull mug 1
            {0x0FFE, new int[]{0x0FFD}}, // skull mug 2
            {0xC1A, new int[]{0xC19}},   // a broken chair (east)
            {0xC19, new int[]{0xC1A}},   // a broken chair (south)
            {0x0C1E, new int[]{0x0C1D}}, // a broken chair (south)
            {0x0C1D, new int[]{0x0C1E}}, // a broken chair (east)
            {0x0C1B, new int[]{0x0C1C}}, // a broken chair (south)
            {0x0C1C, new int[]{0x0C1B}}, // a broken chair (east)
            {0x0C17, new int[]{0x0C18}}, // a covered chair (south)
            {0x0C18, new int[]{0x0C17}}, // a covered chair (east)
            {0x1CDE, new int[]{0x1CE6}}, // body 1
            {0x1CE6, new int[]{0x1CDE}}, // body 2
            {0x1CE0, new int[]{0x1CE8}}, // torso 1
            {0x1CE8, new int[]{0x1CE0}}, // torso 2
            {0x1CDF, new int[]{0x1CE7}}, // legs 1
            {0x1CE7, new int[]{0x1CDF}}, // legs 2
            {0x1ce4, new int[]{0x1CEB}}, // legs 1
            {0x1CEB, new int[]{0x1ce4}}, // legs 2
            {0x1CE1, new int[]{0x1CE9}}, // head 1
            {0x1CE9, new int[]{0x1CE1}}, // head 2
            {0x1CE2, new int[]{0x1CEC}}, // leg 1
            {0x1CEC, new int[]{0x1CE2}}, // leg 2
            {0x1CDD, new int[]{0x1CE5}}, // arm 1
            {0x1CE5, new int[]{0x1CDD}}, // arm 2
            {0x1CE3, new int[]{0x1CEA}}, // body part 1
            {0x1CEA, new int[]{0x1CE3}}, // body part 2
            {0xc22, new int[]{0xc20}},   // a broken dresser 1
            {0xc20, new int[]{0xc22}},   // a broken dresser 2
            {0x11EA, new int[]{0x11EB}}, // straw pillow (east)
            {0x11EB, new int[]{0x11EA}}, // straw pillow (south)
            {0x118d, new int[]{0x118E}}, // table 1
            {0x118E, new int[]{0x118d,}},// table 2
            {0x118B, new int[]{0x118C}}, // table 1
            {0x118C, new int[]{0x118B}}, // table 2
            {0x420, new int[]{0x0429}},  // a gruesome standard (east)
            {0x0429, new int[]{0x420}},  // a gruesome standard (south)
            {0x975, new int[]{0x0974}},  // a cauldron with stand (east)
            {0x0974, new int[]{0x975}},  // a cauldron with stand (south)
            {0xC14, new int[]{0xC15}},   // ruined bookcase south
            {0xC15, new int[]{0xC14}},   // ruined bookcase east
            {0x1BE9, new int[]{0x1BEC}},   // single gold ingot
            {0x1BEC, new int[]{0x1BE9}},   // single gold ingot
            {0x1BEA, new int[]{0x1BED}},   // 3 gold ingots
            {0x1BED, new int[]{0x1BEA}},   // 3 gold ingots
            {0x1BEB, new int[]{0x1BEE}},   // 5 gold ingots
            {0x1BEE, new int[]{0x1BEB}},   // 5 gold ingots
            {0x1BF5, new int[]{0x1BF8}},   // single silver ingot
            {0x1BF8, new int[]{0x1BF5}},   // single silver ingot
            {0x1BF6, new int[]{0x1BF9}},   // 3 silver ingots
            {0x1BF9, new int[]{0x1BF6}},   // 3 silver ingots
            {0x1BF7, new int[]{0x1BFA}},   // 5 silver ingots
            {0x1BFA, new int[]{0x1BF7}},   // 5 silver ingots
            {0x14ED, new int[]{0x14EE}},   // rolled map
            {0x14EE, new int[]{0x14ED}},   // rolled map
            {0x14F1, new int[]{0x14F2}},   // ship plans
            {0x14F2, new int[]{0x14F1}},   // ship plans
            {0x14F3, new int[]{0x14F4}},   // ship model
            {0x14F4, new int[]{0x14F3}},   // ship model
            {0x1BC6, new int[]{0x1BC7}},   // scale shield
            {0x1BC7, new int[]{0x1BC6}},   // scale shield
            {0x1E35, new int[]{0x1E34}},    // scarecrow
            {0x1E34, new int[]{0x1E35}},    // scarecrow
            {0x1E2A, new int[]{0x1E2B}},    // two oars
            {0x1E2B, new int[]{0x1E2A}},    // two oars
            {0x1E83, new int[]{0x1E84}}, // bird
            {0x1E84, new int[]{0x1E83}}, // bird
            {0x1E85, new int[]{0x1E86}}, // bird
            {0x1E86, new int[]{0x1E85}}, // bird
            {0x1E88, new int[]{0x1E89}}, // skinned goat
            {0x1E89, new int[]{0x1E88}}, // skinned goat
            {0x1E8C, new int[]{0x1E8D}}, // pig's feet
            {0x1E8D, new int[]{0x1E8C}}, // pig's feet
            {0x1E8E, new int[]{0x1E8F}}, // pig's head
            {0x1E8F, new int[]{0x1E8E}}, // pig's head
            {0x1E90, new int[]{0x1E91}}, // skinned deer
            {0x1E91, new int[]{0x1E90}}, // skinned deer
            
            {0x3A6D, new int[]{0x3A6E}}, // reindeer
            {0x3A6E, new int[]{0x3A6F}}, // reindeer
            {0x3A6F, new int[]{0x3A70}}, // reindeer
            {0x3A70, new int[]{0x3A71}}, // reindeer
            {0x3A71, new int[]{0x3A72}}, // reindeer
            {0x3A72, new int[]{0x3A6D}}, // reindeer
        };
        /*public static bool Contains(int itemID)
        {
            return FlipTable.ContainsKey(itemID);
        }*/
        #endregion Flip Table
        #region Rares
        private static int[] BasicRares = new int[]
            {
                // making this an addon
                //0x1502, // Plow (south)
                //0x1501, // Plow (east)

				0x1A9A, // Flax (south) � looks lovely stacked on a pitcher.
                0x1A99, // Flax (east)

                0x1BD9, // Stack of boards (south)
                0x1BDC, // Stack of boards (east)

                0x1BDE, // Logs (east)
                0x1BE1, // Logs (south)


                0x0FF3, // Small open book

				0x0F41, // Stacked arrows

				0x1BFD, // Stacked crossbow bolts

				0x0A07, // Wall torch (facing east) - LightType.WestBig
				0x0A0C, // Wall torch (facing south) - LightType.NorthBig

				0x09FE, // Wall sconce (facing east) - LightType.WestBig
				0x0A02, // Wall sconce (facing south) - LightType.NorthBig

				0x0B26, // Tall candelabra (unlit 0xA29) - LightType.Circle225;

				0x0B47, // Large vase

				0x0B48, // Vase

				0x11FC, // Bamboo stool

				0x0A92, // Folded sheet � (east)  making these dyeable would rock! (use deathrobe - dyable/not scissorable)
				0x0A93, // Folded sheet � (south) making these dyeable would rock! (use deathrobe - dyable/not scissorable)

				0x09F3, // Pan 

				0x09E1, // Pot 1

				0x09E0, // Pot 2

				0x108B, // Beads � buyable at a jeweler on OSI, not released on AI.

				0x166D, // Shackles

				0x1A0D, // Chains with bones. (not a whole skeleton like given away at an event.)

                // replacing with a 'camp' stealable
				//0x1230, // Guillotine (east)
                //0x125E, // Guillotine (south)
			};
        private static int[] UnusualRares = new int[]
            {
                // these exist in the world
                0x9DC,      // a dirty pot
                0x9DD,      // a dirty pot
                0x1600,     // a bowl of lettuce

                0x09F9,     // a spoon (east)
                0x09F8,     // a spoon (south)

                0xFAC,      // fire pit (we'll allow it)
                0xE7F,      // a keg
                
                0x1AE4,     // a skull (center) (we'll allow it)

                0x1AE0,     // a skull (east) (we'll allow it)
                0x1AE1,     // a skull (south) (we'll allow it)

                0x1AE2,     // a skull (east)
                0x1AE3,     // a skull (south)

                0x1ce8,     // a torso (south) (we'll allow it)
                0x1CE0,     // a torso (east) (we'll allow it)

                0x1ce9,     // a head (east) (we'll allow it)
                0x1CE1,     // a head (south) (we'll allow it)

                0x185b,     // empty vials (east) (we'll allow it)
                0x185C,     // empty vials (south) (we'll allow it)
                
                0x185D,     // full vials (east)
                0x185E,     // full vials (south)

                0x9ed,      // a kettle (has name, so ours is sort of unique)

                // these are unique

                0x0998,     // copper mug

                0x0995,      // a ceramic mug 1
                0x0996,      // a ceramic mug 2

                0x0997,      // a ceramic mug 1
                0x0999,      // a ceramic mug 2

                0x0FFB,      // skull mug 1
                0x0ffc,      // skull mug 2

                0x0FFD,      // skull mug 1
                0x0FFE,      // skull mug 2

                0x124B,     // an iron maiden 

                0xC16,      // damaged books 

                0xC1A,      // a broken chair (east)
                0xC19,      // a broken chair (south)

                0x0C1E,     // a broken chair (south)
                0x0C1D,     // a broken chair (east)

                0x0C1B,     // a broken chair (south)
                0x0C1C,     // a broken chair (east)

                0x0C17,     // a covered chair (south)
                0x0C18,     // a covered chair (east)

                0x1CDE,     // body 1
                0x1CE6,     // body 2

                0x1CE0,     // torso 1
                0x1CE8,     // torso 2

                0x1CDF,     // legs 1
                0x1CE7,     // legs 2
                
                0x1ce4,     // legs 1
                0x1CEB,     // legs 2

                0x1CE1,     // head 1
                0x1CE9,     // head 2

                0x1CE2,     // leg 1
                0x1CEC,     // leg 2

                0x1CDD,     // arm 1
                0x1CE5,     // arm 2

                0x1CE3,     // body part 1
                0x1CEA,     // body part 2

                0xc22,      // a broken dresser 1
                0xc20,      // a broken dresser 2

                0x163a,     // a pillow 

                0x11EA,     // straw pillow (east)
                0x11EB,     // straw pillow (south)

                //0x1191,     // table (addon component)

                0x118d,     // table 1
                0x118E,     // table 2

                0x118B,     // table 1
                0x118C,     // table 2

                0x420,      // a gruesome standard (east)
                0x0429,     // a gruesome standard (south)

                0x1224,     // statue - all different as far as i can tell - no facings
                0x1225,     // statue
                0x1226,     // statue
                0x1227,     // statue
                0x1228,     // statue

                0x975,      // a cauldron with stand (east)
                0x0974,     // a cauldron with stand (south)

                0xDE3,      // camp fire (animated)

                0xc24,      // broken furniture 1 (east)
                0x0C25,     // broken furniture 2 (south)

                0xc12,      // broken armoire 1 (south)
                0x0C13,     // broken armoire 2 (east)
            };
        private static int[] Level6Rares = new int[]
            {
                1,          // Tapestry1EastAddonDeed
                2,          // TatteredBanner1EastAddonDeed
                3,          // TatteredBanner2EastAddonDeed
                4,          // blood splatter East
                5,          // blood splatter  East
                6,          // vines1 south
                7,          // vines2 south
                8,          // vines1 east
                9,          // vines2 east
                10,         // vines3 east
                11,         // vines4 east
                12,         // vines5 east
                13,         // skeleton with meat 
                14,         // skeleton with meat 
                15,         // skeleton south (hanging)
                16,         // chains south (hanging)
                17,         // chains east (hanging)
                0x99D,      // bottles of liquor 
                0xC14,      // ruined bookcase south
                0xC15,      // ruined bookcase east
                0x9D8,      // a plate of food 
                0xE23,      // bloody water
                0xC24,      // broken furniture 1 (south)
                0xC25,      // broken furniture 2 (east)
            };
        #endregion Rares
        /*public static object[] RareFactoryTypes(RareType RareType)
        {
            if ((RareType & (RareType.MonsterDrop | RareType.DungeonChestDrop | RareType.TMapChestDrop)) != 0)
                return new List<object> (BasicRares);

            if ((RareType & RareType.UnusualChestDrop) != 0)
                return UnusualRares;

            if ((RareType & RareType.DungeonChestDropL6) != 0)
                return Level6Rares;

            if ((RareType & RareType.CrystallinePowder) != 0)
                return CrystallinePowder;

                return new Type[0];
        }*/
        public static Item RareFactoryItem(double chance, RareType RareType = RareType.MonsterDrop | RareType.DungeonChestDrop | RareType.TMapChestDrop)
        {
            if (chance < Utility.RandomDouble())
                return null;

            Item temp = null;
            if ((RareType & (RareType.MonsterDrop | RareType.DungeonChestDrop | RareType.TMapChestDrop)) != 0)
            {
                int ItemId = BasicRares[Utility.Random(BasicRares.Length)];

                switch (ItemId)
                {
                    case 0x0A92:    // Folded sheet � (east)
                    case 0x0A93:    // Folded sheet � (south)
                        {   // make it dyable - don't scissor it!!
                            temp = new DeathRobe();
                            temp.Hue = Utility.RandomDyedHue();
                            temp.Name = "a folded sheet";
                            temp.ItemID = ItemId;
                            temp.LootType = LootType.Regular;
                        }
                        break;

                    case 0x0FAC:    // fire pit
                    case 0x0DE3:    // camp fire animated
                        {
                            temp = new Item(ItemId);
                            temp.Weight = 1;
                            temp.Light = LightType.Circle225;
                        }
                        break;

                    case 0x0A07: // Wall torch (facing east)
                    case 0x09FE: // Wall sconce (facing east)
                        {
                            temp = new Item(ItemId);
                            temp.Weight = 1;
                            temp.Light = LightType.WestBig;
                        }
                        break;

                    case 0x0A0C: // Wall torch (facing south)
                    case 0x0A02: // Wall sconce (facing south)
                        {
                            temp = new Item(ItemId);
                            temp.Weight = 1;
                            temp.Light = LightType.NorthBig;
                        }
                        break;

                    case 0x0B26: // Tall candlabra
                        {
                            temp = new Item(ItemId);
                            temp.Weight = 1;
                            temp.Light = LightType.Circle225;
                        }
                        break;

                    default:
                        {
                            temp = new Item(ItemId);
                            temp.Weight = 1;
                        }
                        break;
                }

            }
            else if ((RareType & RareType.UnusualChestDrop) != 0)
            {
                if (chance >= Utility.RandomDouble())
                {
                    int ItemId = UnusualRares[Utility.Random(UnusualRares.Length)];

                    switch (ItemId)
                    {
                        case 0x163a:     // a pillow 
                            {
                                temp = new Item(ItemId);
                                temp.Weight = 1;
                                temp.Hue = Utility.RandomDyedHue();
                                break;
                            }
                        default:
                            {
                                temp = new Item(ItemId);
                                temp.Weight = 1;
                                break;
                            }
                    }
                }
            }
            else if ((RareType & RareType.DungeonChestDropL6) != 0)
            {
                if (chance >= Utility.RandomDouble())
                {
                    int ItemId = Level6Rares[Utility.Random(Level6Rares.Length)];

                    switch (ItemId)
                    {
                        case 1:
                            {
                                temp = new Tapestry1EastAddonDeed();
                                break;
                            }
                        case 2:
                            {
                                temp = new TatteredBanner1EastAddonDeed();
                                break;
                            }
                        case 3:
                            {
                                temp = new TatteredBanner2EastAddonDeed();
                                break;
                            }
                        case 4:
                            {
                                temp = new BloodSplatter1EastAddonDeed();
                                break;
                            }
                        case 5:
                            {
                                temp = new BloodSplatter2EastAddonDeed();
                                break;
                            }
                        case 6:
                            {
                                temp = new Vines1SouthAddonDeed();
                                break;
                            }
                        case 7:
                            {
                                temp = new Vines2SouthAddonDeed();
                                break;
                            }
                        case 8:
                            {
                                temp = new Vines1EastAddonDeed();
                                break;
                            }
                        case 9:
                            {
                                temp = new Vines2EastAddonDeed();
                                break;
                            }
                        case 10:
                            {
                                temp = new Vines3EastAddonDeed();
                                break;
                            }
                        case 11:
                            {
                                temp = new Vines4EastAddonDeed();
                                break;
                            }
                        case 12:
                            {
                                temp = new Vines5EastAddonDeed();
                                break;
                            }
                        case 13:
                            {
                                temp = new SkeletonWithMeat1SouthAddonDeed();
                                break;
                            }
                        case 14:
                            {
                                temp = new SkeletonWithMeat2EastAddonDeed();
                                break;
                            }
                        case 15:
                            {
                                temp = new Skeleton1SouthAddonDeed();
                                break;
                            }
                        case 16:
                            {
                                temp = new Chains1SouthAddonDeed();
                                break;
                            }
                        case 17:
                            {
                                temp = new Chains1EastAddonDeed();
                                break;
                            }

                        default:
                            {
                                temp = new Item(ItemId);
                                temp.Weight = 1;
                                break;
                            }
                    }
                }
            }
            else if ((RareType & RareType.CrystallinePowder) != 0)
            {
                Type type = CrystallinePowder[Utility.RandomMinMaxExp(CrystallinePowder.Length, Exponent.d0_20)];
                CrystallinePowder cp = (CrystallinePowder)Construct(type);
                cp.UsesRemaining = Utility.RandomMinMax(1, cp.MaxUses);
                temp = cp;
            }
            else if ((RareType & RareType.ScarecrowEast) != 0)
            {
                temp = new Items.Rares.ScarecrowEast();
            }
            else if ((RareType & RareType.ScarecrowSouth) != 0)
            {
                temp = new Items.Rares.ScarecrowSouth();
            }
            else if ((RareType & RareType.PlowSouth) != 0)
            {
                temp = new Items.Rares.PlowSouth();
            }
            else if ((RareType & RareType.PlowEast) != 0)
            {
                temp = new Items.Rares.PlowEast();
            }
            else if ((RareType & RareType.RareUncutCloth) != 0 || (RareType & RareType.RareBoltOfCloth) != 0)
            {   // special dye tub colors
                int hue = Utility.RandomSpecialHue();

                temp = (RareType & RareType.RareUncutCloth) != 0 ? new UncutCloth(50) : new BoltOfCloth();
                temp.Hue = hue;
            }
            else if ((RareType & RareType.FruitBasket) != 0)
            {
                temp = new Items.FruitBasket();
            }

            if (temp != null)
            {
                temp.LootType = LootType.Rare;
#if true
                // now filter for rares already being sold
                for (int ix = 0; ix < 10 && temp != null && AlreadyForSale(temp); ix++)
                {
                    temp.Delete();
                    temp = RareFactoryItem(chance: 1.0, RareType: RareType);
                }
#endif
            }

            return temp;
        }
#if false
        public static List<Type> TypeRegistry()
        {
            List<Type> new_list = new()
            {
                typeof(Tapestry1EastAddonDeed),
                typeof(TatteredBanner1EastAddonDeed),
                typeof(TatteredBanner2EastAddonDeed),
                typeof(BloodSplatter1EastAddonDeed),
                typeof(BloodSplatter2EastAddonDeed),
                typeof(Vines1SouthAddonDeed),
                typeof(Vines2SouthAddonDeed),
                typeof(Vines1EastAddonDeed),
                typeof(Vines2EastAddonDeed),
                typeof(Vines3EastAddonDeed),
                typeof(Vines4EastAddonDeed),
                typeof(Vines5EastAddonDeed),
                typeof(SkeletonWithMeat1SouthAddonDeed),
                typeof(SkeletonWithMeat2EastAddonDeed()),
                typeof(Skeleton1SouthAddonDeed),
                typeof(Chains1SouthAddonDeed),
                typeof(Chains1EastAddonDeed),
            };
            return new_list;
        }
        public static List<int> IDRegistry()
        {
            List<int> new_list = new List<int>(BasicRares);
            new_list.AddRange(UnusualRares);
            new_list.AddRange(new List<int>() {
                0x99D,      // bottles of liquor 
                0xC14,      // ruined bookcase south
                0xC15,      // ruined bookcase east
                0x9D8,      // a plate of food 
                0xE23,      // bloody water
                0xC24,      // broken furniture 1 (south)
                0xC25,      // broken furniture 2 (east)
             });
            return new_list;
        }
#endif
        public static bool AlreadyForSale(Item item)
        {
            if (item == null) return false;
            if (item is not AddonDeed)
                return CrownSterlingSystem.ForSale(item.ItemID);
            else
            {
                BaseAddon ba = ((AddonDeed)item).Addon;
                if (ba != null)
                {
                    if (ba.Components != null && ba.Components.Count == 1)
                    {
                        AddonComponent ac = ba.Components[0] as AddonComponent;
                        if (CrownSterlingSystem.ForSale(ac.ItemID))
                        {
                            ba.Delete();
                            return true;
                        }
                    }

                    ba.Delete();
                }
            }

            return false;
        }
        #endregion Rares

        #region List definitions
        private static Type[] m_CrystallinePowder = new Type[]
        {
            typeof(CrystallineDullCopper), typeof(CrystallineShadowIron), typeof(CrystallineCopper), typeof(CrystallineBronze),
            typeof(CrystallineGold), typeof(CrystallineAgapite), typeof(CrystallineVerite), typeof(CrystallineValorite)
        };
        public static Type[] CrystallinePowder { get { return m_CrystallinePowder; } }

        private static Type[] m_SEWeaponTypes = new Type[]
            {
//				typeof( Bokuto ),				typeof( Daisho ),				typeof( Kama ),
//				typeof( Lajatang ),				typeof( NoDachi ),				typeof( Nunchaku ),
//				typeof( Sai ),					typeof( Tekagi ),				typeof( Tessen ),
//				typeof( Tetsubo ),				typeof( Wakizashi )
			};

        public static Type[] SEWeaponTypes { get { return m_SEWeaponTypes; } }

        private static Type[] m_AosWeaponTypes = new Type[]
            {
                typeof( Scythe ),               typeof( BoneHarvester ),        typeof( Scepter ),
                typeof( BladedStaff ),          typeof( Pike ),                 typeof( DoubleBladedStaff ),
                typeof( Lance ),                typeof( CrescentBlade )
            };

        public static Type[] AosWeaponTypes { get { return m_AosWeaponTypes; } }

        private static Type[] m_WeaponTypes = new Type[]
            {
                typeof( Axe ),                  typeof( BattleAxe ),            typeof( DoubleAxe ),
                typeof( ExecutionersAxe ),      typeof( Hatchet ),              typeof( LargeBattleAxe ),
                typeof( TwoHandedAxe ),         typeof( WarAxe ),               typeof( Club ),
                typeof( Mace ),                 typeof( Maul ),                 typeof( WarHammer ),
                typeof( WarMace ),              typeof( Bardiche ),             typeof( Halberd ),
                typeof( Spear ),                typeof( ShortSpear ),           typeof( Pitchfork ),
                typeof( WarFork ),              typeof( BlackStaff ),           typeof( GnarledStaff ),
                typeof( QuarterStaff ),         typeof( Broadsword ),           typeof( Cutlass ),
                typeof( Katana ),               typeof( Kryss ),                typeof( Longsword ),
                typeof( Scimitar ),             typeof( VikingSword ),          typeof( Pickaxe ),
                typeof( HammerPick ),           typeof( ButcherKnife ),         typeof( Cleaver ),
                typeof( Dagger ),               typeof( SkinningKnife ),        typeof( ShepherdsCrook )
            };

        public static Type[] WeaponTypes { get { return m_WeaponTypes; } }

        private static Type[] m_SERangedWeaponTypes = new Type[]
            {
//				typeof( Yumi )
			};

        public static Type[] SERangedWeaponTypes { get { return m_SERangedWeaponTypes; } }

        private static Type[] m_AosRangedWeaponTypes = new Type[]
            {
                typeof( CompositeBow ),         typeof( RepeatingCrossbow )
            };

        public static Type[] AosRangedWeaponTypes { get { return m_AosRangedWeaponTypes; } }

        private static Type[] m_RangedWeaponTypes = new Type[]
            {
                typeof( Bow ),                  typeof( Crossbow ),             typeof( HeavyCrossbow )
            };

        public static Type[] RangedWeaponTypes { get { return m_RangedWeaponTypes; } }

        private static Type[] m_SEArmorTypes = new Type[]
            {
//				typeof( ChainHatsuburi ),		typeof( LeatherDo ),			typeof( LeatherHaidate ),
//				typeof( LeatherHiroSode ),		typeof( LeatherJingasa ),		typeof( LeatherMempo ),
//				typeof( LeatherNinjaHood ),		typeof( LeatherNinjaJacket ),	typeof( LeatherNinjaMitts ),
//				typeof( LeatherNinjaPants ),	typeof( LeatherSuneate ),		typeof( DecorativePlateKabuto ),
//				typeof( HeavyPlateJingasa ),	typeof( LightPlateJingasa ),	typeof( PlateBattleKabuto ),
//				typeof( PlateDo ),				typeof( PlateHaidate ),			typeof( PlateHatsuburi ),
//				typeof( PlateHiroSode ),		typeof( PlateMempo ),			typeof( PlateSuneate ),
//				typeof( SmallPlateJingasa ),	typeof( StandardPlateKabuto ),	typeof( StuddedDo ),
//				typeof( StuddedHaidate ),		typeof( StuddedHiroSode ),		typeof( StuddedMempo ),
//				typeof( StuddedSuneate )
			};

        public static Type[] SEArmorTypes { get { return m_SEArmorTypes; } }

        private static Type[] m_ArmorTypes = new Type[]
            {
                typeof( BoneArms ),             typeof( BoneChest ),            typeof( BoneGloves ),
                typeof( BoneLegs ),             typeof( BoneHelm ),             typeof( ChainChest ),
                typeof( ChainLegs ),            typeof( ChainCoif ),            typeof( Bascinet ),
                typeof( CloseHelm ),            typeof( Helmet ),               typeof( NorseHelm ),
                typeof( OrcHelm ),              typeof( FemaleLeatherChest ),   typeof( LeatherArms ),
                typeof( LeatherBustierArms ),   typeof( LeatherChest ),         typeof( LeatherGloves ),
                typeof( LeatherGorget ),        typeof( LeatherLegs ),          typeof( LeatherShorts ),
                typeof( LeatherSkirt ),         typeof( LeatherCap ),           typeof( FemalePlateChest ),
                typeof( PlateArms ),            typeof( PlateChest ),           typeof( PlateGloves ),
                typeof( PlateGorget ),          typeof( PlateHelm ),            typeof( PlateLegs ),
                typeof( RingmailArms ),         typeof( RingmailChest ),        typeof( RingmailGloves ),
                typeof( RingmailLegs ),         typeof( FemaleStuddedChest ),   typeof( StuddedArms ),
                typeof( StuddedBustierArms ),   typeof( StuddedChest ),         typeof( StuddedGloves ),
                typeof( StuddedGorget ),        typeof( StuddedLegs )
            };

        public static Type[] ArmorTypes { get { return m_ArmorTypes; } }

        private static Type[] m_AosShieldTypes = new Type[]
            {
                typeof( ChaosShield ),          typeof( OrderShield )
            };

        public static Type[] AosShieldTypes { get { return m_AosShieldTypes; } }

        private static Type[] m_ShieldTypes = new Type[]
            {
                typeof( BronzeShield ),         typeof( Buckler ),              typeof( HeaterShield ),
                typeof( MetalShield ),          typeof( MetalKiteShield ),      typeof( WoodenKiteShield ),
                typeof( WoodenShield )
            };

        public static Type[] ShieldTypes { get { return m_ShieldTypes; } }

        private static Type[] m_GemTypes = new Type[]
            {
                typeof( Amber ),                typeof( Amethyst ),             typeof( Citrine ),
                typeof( Diamond ),              typeof( Emerald ),              typeof( Ruby ),
                typeof( Sapphire ),             typeof( StarSapphire ),         typeof( Tourmaline )
            };

        public static Type[] GemTypes { get { return m_GemTypes; } }

        private static Type[] m_JewelryTypes = new Type[]
            {
                typeof( GoldRing ),             typeof( GoldBracelet ),
                typeof( SilverRing ),           typeof( SilverBracelet ),
                // 10/27/21, Adam: Add the following
                typeof (Necklace), typeof(GoldBeadNecklace), typeof(SilverBeadNecklace), typeof(GoldNecklace), typeof(SilverNecklace),
                typeof(GoldEarrings), typeof(SilverEarrings)
            };

        public static Type[] JewelryTypes { get { return m_JewelryTypes; } }

        private static Type[] m_RegTypes = new Type[]
            {
                typeof( BlackPearl ),           typeof( Bloodmoss ),            typeof( Garlic ),
                typeof( Ginseng ),              typeof( MandrakeRoot ),         typeof( Nightshade ),
                typeof( SulfurousAsh ),         typeof( SpidersSilk )
            };

        public static Type[] RegTypes { get { return m_RegTypes; } }

        private static Type[] m_NecroRegTypes = new Type[]
            {
                typeof( BatWing ),              typeof( GraveDust ),            typeof( DaemonBlood ),
                typeof( NoxCrystal ),           typeof( PigIron )
            };

        public static Type[] NecroRegTypes { get { return m_NecroRegTypes; } }

        private static Type[] m_PotionTypes = new Type[]
            {
                typeof( AgilityPotion ),        typeof( StrengthPotion ),       typeof( RefreshPotion ),
                typeof( LesserCurePotion ),     typeof( LesserHealPotion ),     typeof( LesserPoisonPotion )
            };

        public static Type[] PotionTypes { get { return m_PotionTypes; } }

        private static Type[] m_SEInstrumentTypes = new Type[]
            {
//				typeof( BambooFlute )
			};

        public static Type[] SEInstrumentTypes { get { return m_SEInstrumentTypes; } }

        private static Type[] m_InstrumentTypes = new Type[]
            {
                typeof( Drums ),                typeof( Harp ),                 typeof( LapHarp ),
                typeof( Lute ),                 typeof( Tambourine ),           typeof( TambourineTassel )
            };

        public static Type[] InstrumentTypes { get { return m_InstrumentTypes; } }

        private static Type[] m_StatueTypes = new Type[]
        {
            typeof( Statue1 ),                  typeof( Statue2 ),              typeof( Statue3 ),
            typeof( Statue4 ),                  typeof( Bust ),
        };

        public static Type[] StatueTypes { get { return m_StatueTypes; } }

        private static Type[] m_RegularScrollTypes = new Type[]
            {
                typeof( ClumsyScroll ),         typeof( CreateFoodScroll ),     typeof( FeeblemindScroll ),     typeof( HealScroll ),
                typeof( MagicArrowScroll ),     typeof( NightSightScroll ),     typeof( ReactiveArmorScroll ),  typeof( WeakenScroll ),
                typeof( AgilityScroll ),        typeof( CunningScroll ),        typeof( CureScroll ),           typeof( HarmScroll ),
                typeof( MagicTrapScroll ),      typeof( MagicUnTrapScroll ),    typeof( ProtectionScroll ),     typeof( StrengthScroll ),
                typeof( BlessScroll ),          typeof( FireballScroll ),       typeof( MagicLockScroll ),      typeof( PoisonScroll ),
                typeof( TelekinisisScroll ),    typeof( TeleportScroll ),       typeof( UnlockScroll ),         typeof( WallOfStoneScroll ),
                typeof( ArchCureScroll ),       typeof( ArchProtectionScroll ), typeof( CurseScroll ),          typeof( FireFieldScroll ),
                typeof( GreaterHealScroll ),    typeof( LightningScroll ),      typeof( ManaDrainScroll ),      typeof( RecallScroll ),
                typeof( BladeSpiritsScroll ),   typeof( DispelFieldScroll ),    typeof( IncognitoScroll ),      typeof( MagicReflectScroll ),
                typeof( MindBlastScroll ),      typeof( ParalyzeScroll ),       typeof( PoisonFieldScroll ),    typeof( SummonCreatureScroll ),
                typeof( DispelScroll ),         typeof( EnergyBoltScroll ),     typeof( ExplosionScroll ),      typeof( InvisibilityScroll ),
                typeof( MarkScroll ),           typeof( MassCurseScroll ),      typeof( ParalyzeFieldScroll ),  typeof( RevealScroll ),
                typeof( ChainLightningScroll ), typeof( EnergyFieldScroll ),    typeof( FlamestrikeScroll ),    typeof( GateTravelScroll ),
                typeof( ManaVampireScroll ),    typeof( MassDispelScroll ),     typeof( MeteorSwarmScroll ),    typeof( PolymorphScroll ),
                typeof( EarthquakeScroll ),     typeof( EnergyVortexScroll ),   typeof( ResurrectionScroll ),   typeof( SummonAirElementalScroll ),
                typeof( SummonDaemonScroll ),   typeof( SummonEarthElementalScroll ),   typeof( SummonFireElementalScroll ),    typeof( SummonWaterElementalScroll )
            };

        private static Type[] m_NecromancyScrollTypes = new Type[]
            {
                typeof( AnimateDeadScroll ),        typeof( BloodOathScroll ),      typeof( CorpseSkinScroll ), typeof( CurseWeaponScroll ),
                typeof( EvilOmenScroll ),           typeof( HorrificBeastScroll ),  typeof( LichFormScroll ),   typeof( MindRotScroll ),
                typeof( PainSpikeScroll ),          typeof( PoisonStrikeScroll ),   typeof( StrangleScroll ),   typeof( SummonFamiliarScroll ),
                typeof( VampiricEmbraceScroll ),    typeof( VengefulSpiritScroll ), typeof( WitherScroll ),     typeof( WraithFormScroll )
            };

        private static Type[] m_SENecromancyScrollTypes = new Type[]
        {
            typeof( AnimateDeadScroll ),        typeof( BloodOathScroll ),      typeof( CorpseSkinScroll ), typeof( CurseWeaponScroll ),
            typeof( EvilOmenScroll ),           typeof( HorrificBeastScroll ),  typeof( LichFormScroll ),   typeof( MindRotScroll ),
            typeof( PainSpikeScroll ),          typeof( PoisonStrikeScroll ),   typeof( StrangleScroll ),   typeof( SummonFamiliarScroll ),
            typeof( VampiricEmbraceScroll ),    typeof( VengefulSpiritScroll ), typeof( WitherScroll ),     typeof( WraithFormScroll ),
//			typeof( ExorcismScroll )
		};

        private static Type[] m_PaladinScrollTypes = new Type[0];

        public static Type[] RegularScrollTypes { get { return m_RegularScrollTypes; } }
        public static Type[] NecromancyScrollTypes { get { return m_NecromancyScrollTypes; } }
        public static Type[] SENecromancyScrollTypes { get { return m_SENecromancyScrollTypes; } }
        public static Type[] PaladinScrollTypes { get { return m_PaladinScrollTypes; } }

        private static Type[] m_GrimmochJournalTypes = new Type[]
        {
//			typeof( GrimmochJournal1 ),		typeof( GrimmochJournal2 ),		typeof( GrimmochJournal3 ),
//			typeof( GrimmochJournal6 ),		typeof( GrimmochJournal7 ),		typeof( GrimmochJournal11 ),
//			typeof( GrimmochJournal14 ),	typeof( GrimmochJournal17 ),	typeof( GrimmochJournal23 )
		};

        public static Type[] GrimmochJournalTypes { get { return m_GrimmochJournalTypes; } }

        private static Type[] m_LysanderNotebookTypes = new Type[]
        {
//			typeof( LysanderNotebook1 ),		typeof( LysanderNotebook2 ),		typeof( LysanderNotebook3 ),
//			typeof( LysanderNotebook7 ),		typeof( LysanderNotebook8 ),		typeof( LysanderNotebook11 )
		};

        public static Type[] LysanderNotebookTypes { get { return m_LysanderNotebookTypes; } }

        private static Type[] m_TavarasJournalTypes = new Type[]
        {
//			typeof( TavarasJournal1 ),		typeof( TavarasJournal2 ),		typeof( TavarasJournal3 ),
//			typeof( TavarasJournal6 ),		typeof( TavarasJournal7 ),		typeof( TavarasJournal8 ),
//			typeof( TavarasJournal9 ),		typeof( TavarasJournal11 ),		typeof( TavarasJournal14 ),
//			typeof( TavarasJournal16 ),		typeof( TavarasJournal16b ),	typeof( TavarasJournal17 ),
//			typeof( TavarasJournal19 )
		};
        public static Type[] TavarasJournalTypes { get { return m_TavarasJournalTypes; } }

        private static MagicItemEffect[] m_WandEnchantments = new MagicItemEffect[]
        {
        /*
         * Clumsiness,
         * Identification,
         * Healing,
         * Feeblemindedness,
         * Weakness,
         * MagicArrow,
         * Harming,
         * Fireball,
         * GreaterHealing,
         * Lightning,
         * ManaDraining,
         */
            MagicItemEffect.Clumsy,
            MagicItemEffect.Identification,
            MagicItemEffect.Heal,
            MagicItemEffect.Feeblemind,
            MagicItemEffect.Weaken,
            MagicItemEffect.MagicArrow,
            MagicItemEffect.Harm,
            MagicItemEffect.Fireball,
            MagicItemEffect.GreaterHeal,
            MagicItemEffect.Lightning,
            MagicItemEffect.ManaDrain
        };
        public static MagicItemEffect[] WandEnchantments { get { return m_WandEnchantments; } }

        private static MagicItemEffect[] m_WeaponEnchantments = new MagicItemEffect[]
        {
        /*
         * Burning    Magic Arrow 
         * Clumsiness    Clumsy 
         * Feeblemindness    Feeblemind 
         * Weakness    Weaken 
         * Wounding    Harm 
         * Daemon's Breath    Fireball 
         * Evil    Curse 
         * Ghoul's Touch    Paralyze 
         * Mage's Bane    Mana Drain 
         * Thunder    Lightning 
         * https://uolife.tistory.com/259
         */
            MagicItemEffect.MagicArrow,
            MagicItemEffect.Clumsy,
            MagicItemEffect.Feeblemind,
            MagicItemEffect.Weaken,
            MagicItemEffect.Harm,
            MagicItemEffect.Fireball,
            MagicItemEffect.Curse,
            MagicItemEffect.Paralyze,
            MagicItemEffect.ManaDrain,
            MagicItemEffect.Lightning
        };
        public static MagicItemEffect[] WeaponEnchantments { get { return m_WeaponEnchantments; } }

        private static MagicEquipEffect[] m_ArmorEnchantments = new MagicEquipEffect[]
        {
            MagicEquipEffect.NightSight,
            MagicEquipEffect.SpellReflection,
        };
        public static MagicEquipEffect[] ArmorEnchantments { get { return m_ArmorEnchantments; } }

        public static Item IDWand()
        {
            Item reward = new Wand();
            if (reward != null)
            {
                int minLevel = 1;
                int maxLevel = Utility.RandomList(new int[] { 1, 2, 3 });
                ((Wand)reward).SetRandomMagicEffect(minLevel, maxLevel);        // to get the random charges
                ((Wand)reward).MagicEffect = MagicItemEffect.Identification;    // reset to IDWand
            }

            return reward;
        }
        public static Item GnarledStaff()
        {
            Item reward = new GnarledStaff();
            if (reward != null)
            {   // we believe gnarled staves got Wand enchantments.
                //  info is hard to come by.
                int minLevel = 1;
                int maxLevel = Utility.RandomList(new int[] { 1, 2, 3 });
                ((GnarledStaff)reward).SetRandomMagicEffect(minLevel, maxLevel);        // to get the random charges
                ((GnarledStaff)reward).MagicEffect =                                    // reset to wand ability
                    WandEnchantments[Utility.Random(WandEnchantments.Length)];
            }

            return reward;
        }

        [Obsolete("Use Wand class instead")]
        public static Item RandomPvPWand()
        {
            Type t = m_PvPWandTypes[Utility.Random(m_PvPWandTypes.Length)];
            return (Item)Activator.CreateInstance(t);
        }
        [Obsolete("Use Wand class instead")]
        private static Type[] m_PvPWandTypes = new Type[]
            {
                typeof( ClumsyWand ),               typeof( FeebleWand ),           typeof( FireballWand ),
                typeof( GreaterHealWand ),          typeof( HarmWand ),             typeof( HealWand ),
                typeof( IDWand ),                   typeof( LightningWand ),        typeof( MagicArrowWand ),
                typeof( ManaDrainWand ),            typeof( WeaknessWand )

            };
        [Obsolete("Use Wand class instead")]
        public static Item RandomWand()
        {
            Type t = m_WandTypes[Utility.Random(m_WandTypes.Length)];
            return (Item)Activator.CreateInstance(t);
        }
        [Obsolete("Use Wand class instead")]
        private static Type[] m_WandTypes = new Type[]
            {
                typeof( IDWand ), typeof(NightSightWand),typeof(CreateFoodWand),typeof(MagicLockWand),typeof(UnlockWand)
            };
        [Obsolete("Use Wand class instead")]
        public static Type[] WandTypes { get { return m_WandTypes; } }

        private static Type[] m_SEClothingTypes = new Type[]
            {
//				typeof( ClothNinjaJacket ),		typeof( FemaleKimono ),			typeof( Hakama ),
//				typeof( HakamaShita ),			typeof( JinBaori ),				typeof( Kamishimo ),
//				typeof( MaleKimono ),			typeof( NinjaTabi ),			typeof( Obi ),
//				typeof( SamuraiTabi ),			typeof( TattsukeHakama ),		typeof( Waraji )
			};

        public static Type[] SEClothingTypes { get { return m_SEClothingTypes; } }

        private static Type[] m_AosClothingTypes = new Type[]
            {
                typeof( FurSarong ),            typeof( FurCape ),              typeof( FlowerGarland ),
                typeof( GildedDress ),          typeof( FurBoots ),             typeof( FormalShirt ),
        };

        public static Type[] AosClothingTypes { get { return m_AosClothingTypes; } }

        private static Type[] m_ClothingTypes = new Type[]
            {
                typeof( Cloak ),
                typeof( Bonnet ),               typeof( Cap ),                  typeof( FeatheredHat ),
                typeof( FloppyHat ),            typeof( JesterHat ),            typeof( Surcoat ),
                typeof( SkullCap ),             typeof( StrawHat ),             typeof( TallStrawHat ),
                typeof( TricorneHat ),          typeof( WideBrimHat ),          typeof( WizardsHat ),
                typeof( BodySash ),             typeof( Doublet ),              typeof( Boots ),
                typeof( FullApron ),            typeof( JesterSuit ),           typeof( Sandals ),
                typeof( Tunic ),                typeof( Shoes ),                typeof( Shirt ),
                typeof( Kilt ),                 typeof( Skirt ),                typeof( FancyShirt ),
                typeof( FancyDress ),           typeof( ThighBoots ),           typeof( LongPants ),
                typeof( PlainDress ),           typeof( Robe ),                 typeof( ShortPants ),
                typeof( HalfApron )
            };
        private static Type[] m_MagicClothingTypes = new Type[]
            {
                typeof(Boots), typeof(Cloak), typeof(BodySash), typeof(ThighBoots)
            };
        public static Type[] MagicClothingTypes { get { return m_MagicClothingTypes; } }
        public static Type[] ClothingTypes { get { return m_ClothingTypes; } }

        private static Type[] m_SEHatTypes = new Type[]
            {
//				typeof( ClothNinjaHood ),		typeof( Kasa )
			};

        public static Type[] SEHatTypes { get { return m_SEHatTypes; } }

        private static Type[] m_AosHatTypes = new Type[]
            {
                typeof( FlowerGarland ),    typeof( BearMask ),     typeof( DeerMask )	//Are Bear& Deer mask inside the Pre-AoS loottables too?
			};

        public static Type[] AosHatTypes { get { return m_AosHatTypes; } }

        private static Type[] m_HatTypes = new Type[]
            {
                typeof( SkullCap ),         typeof( Bandana ),      typeof( FloppyHat ),
                typeof( Cap ),              typeof( WideBrimHat ),  typeof( StrawHat ),
                typeof( TallStrawHat ),     typeof( WizardsHat ),   typeof( Bonnet ),
                typeof( FeatheredHat ),     typeof( TricorneHat ),  typeof( JesterHat )
            };

        public static Type[] HatTypes { get { return m_HatTypes; } }

        private static Type[] m_LibraryBookTypes = new Type[]
            {
                typeof( GrammarOfOrcish ),      typeof( CallToAnarchy ),                typeof( ArmsAndWeaponsPrimer ),
                typeof( SongOfSamlethe ),       typeof( TaleOfThreeTribes ),            typeof( GuideToGuilds ),
                typeof( BirdsOfBritannia ),     typeof( BritannianFlora ),              typeof( ChildrenTalesVol2 ),
                typeof( TalesOfVesperVol1 ),    typeof( DeceitDungeonOfHorror ),        typeof( DimensionalTravel ),
                typeof( EthicalHedonism ),      typeof( MyStory ),                      typeof( DiversityOfOurLand ),
                typeof( QuestOfVirtues ),       typeof( RegardingLlamas ),              typeof( TalkingToWisps ),
                typeof( TamingDragons ),        typeof( BoldStranger ),                 typeof( BurningOfTrinsic ),
                typeof( TheFight ),             typeof( LifeOfATravellingMinstrel ),    typeof( MajorTradeAssociation ),
                typeof( RankingsOfTrades ),     typeof( WildGirlOfTheForest ),          typeof( TreatiseOnAlchemy ),
                typeof( VirtueBook )
            };

        public static Type[] LibraryBookTypes { get { return m_LibraryBookTypes; } }
        #endregion

        #region Accessors

        public static BaseWeapon RandomRangedWeapon()
        {
            return RandomRangedWeapon(false);
        }

        public static BaseWeapon RandomRangedWeapon(bool inTokuno)
        {
            /*if (Core.SE && inTokuno)
				return Construct(m_SERangedWeaponTypes, m_AosRangedWeaponTypes, m_RangedWeaponTypes) as BaseWeapon;

			if (Core.AOS)
				return Construct(m_AosRangedWeaponTypes, m_RangedWeaponTypes) as BaseWeapon;*/

            return Construct(m_RangedWeaponTypes) as BaseWeapon;
        }

        public static BaseWeapon RandomWeapon()
        {
            if (Core.RuleSets.AOSRules())
                return Construct(m_AosWeaponTypes, m_WeaponTypes) as BaseWeapon;

            return Construct(m_WeaponTypes, m_RangedWeaponTypes) as BaseWeapon;
        }

        public static Item RandomWeaponOrJewelry()
        {
            if (Core.RuleSets.AOSRules())
                return Construct(m_AosWeaponTypes, m_WeaponTypes, m_JewelryTypes);

            return Construct(m_WeaponTypes, m_RangedWeaponTypes, m_JewelryTypes);
        }

        public static BaseJewel RandomJewelry()
        {
            return Construct(m_JewelryTypes) as BaseJewel;
        }

        public static BaseArmor RandomArmor()
        {
            return Construct(m_ArmorTypes) as BaseArmor;
        }

        public static BaseShield RandomShield()
        {
            return Construct(m_ShieldTypes) as BaseShield;
        }

        public static BaseArmor RandomArmorOrShield()
        {
            return Construct(m_ArmorTypes, m_ShieldTypes) as BaseArmor;
        }

        public static Item RandomArmorOrShieldOrJewelry()
        {
            return Construct(m_ArmorTypes, m_ShieldTypes, m_JewelryTypes);
        }

        public static Item RandomArmorOrShieldOrWeapon()
        {
            if (Core.RuleSets.AOSRules())
                return Construct(m_AosWeaponTypes, m_WeaponTypes, m_ArmorTypes, m_ShieldTypes);

            return Construct(m_WeaponTypes, m_RangedWeaponTypes, m_ArmorTypes, m_ShieldTypes);
        }

        public static Item RandomArmorOrWeapon()
        {
            if (Core.RuleSets.AOSRules())
                return Construct(m_AosWeaponTypes, m_WeaponTypes, m_ArmorTypes);

            return Construct(m_WeaponTypes, m_RangedWeaponTypes, m_ArmorTypes);
        }

        public static Item RandomArmorOrShieldOrWeaponOrJewelry()
        {
            if (Core.RuleSets.AOSRules())
                return Construct(m_AosWeaponTypes, m_WeaponTypes, m_ArmorTypes, m_ShieldTypes, m_JewelryTypes);

            return Construct(m_WeaponTypes, m_RangedWeaponTypes, m_ArmorTypes, m_ShieldTypes, m_JewelryTypes);
        }

        public static Item RandomClothingOrJewelry(bool must_support_magic = false)
        {
            if (must_support_magic == true)
                return Construct(m_JewelryTypes, m_MagicClothingTypes);
            else
                return Construct(m_JewelryTypes, m_ClothingTypes);
        }
        public static Item RandomJewelry(bool must_support_magic = false)
        {
            if (must_support_magic == true)
                return Construct(m_JewelryTypes);
            else
                return Construct(m_JewelryTypes);
        }
        public static Item RandomClothing(bool must_support_magic = false)
        {
            if (must_support_magic == true)
                return Construct(m_MagicClothingTypes);
            else
                return Construct(m_ClothingTypes);
        }
        public static Item RandomGem()
        {
            return Construct(m_GemTypes);
        }

        public static Item RandomReagent()
        {
            return Construct(m_RegTypes);
        }

        public static Item RandomNecromancyReagent()
        {
            return Construct(m_NecroRegTypes);
        }

        public static Item RandomPossibleReagent()
        {
            if (Core.RuleSets.AOSRules())
                return Construct(m_RegTypes, m_NecroRegTypes);

            return Construct(m_RegTypes);
        }

        public static Item RandomPotion()
        {
            return Construct(m_PotionTypes);
        }

        public static BaseInstrument RandomInstrument()
        {
            return Construct(m_InstrumentTypes) as BaseInstrument;
        }

        public static Item RandomStatue()
        {
            return Construct(m_StatueTypes);
        }

        public enum ImbueLevel : int
        {
            Level0 = 0, // regular
            Level1 = 1, // Level0 + Ruin,	| Defense
            Level2 = 2, // Level1 + Might,	| Guarding 
            Level3 = 3, // Level2 + Force,	| Hardening
            Level4 = 4, // Level3 + Power,	| Fortification
            Level5 = 5, // Level4 + Vanq
            Level6 = 6  // Level5 + force, power, vanq | Invulnerability
        }
        public static int ScaleOldLevelToImbueLevel(int level)
        {
            switch (level)
            {
                default:
                case 0: return 0;
                case 1: return Utility.RandomList(new int[] { 1, 2 });
                case 2: return Utility.RandomList(new int[] { 3, 4 });
                case 3: return Utility.RandomList(new int[] { 5, 6 });
            }
        }
        public static ImbueLevel TreasureMapLevelToImbueLevel(int treasure_level)
        {
            ImbueLevel value = ImbueLevel.Level0;
            if (Enum.IsDefined(typeof(ImbueLevel), treasure_level))
                value = (ImbueLevel)treasure_level;
            return value;
        }
        public static int[] ImbueLevelToMagicEnchantment(ImbueLevel level)
        {   // convert an ImbueLevel to a min/max for Magic Enchantment
            switch (level)
            {
                case ImbueLevel.Level0:
                case ImbueLevel.Level1:
                    return new int[] { 1, 1 };  // minimum enchantment
                case ImbueLevel.Level2:
                case ImbueLevel.Level3:
                    return new int[] { 1, 2 };  // reasonable enchantment
                case ImbueLevel.Level4:
                case ImbueLevel.Level5:
                    return new int[] { 1, 3 };  // chance at the best
                case ImbueLevel.Level6:
                    return new int[] { 2, 3 };  // good chance at the best
            }
            // should never get here
            return new int[] { 0, 0 };  // no enchantment
        }
        private static bool ChecMagicGearThrottle(bool noThrottle)
        {
            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.MagicGearThrottleEnabled))
                return noThrottle;

            // returning true here removes any Throttling
            return true;
        }
        private static int MagicGearThrottle(ImbueLevel level)
        {
            if (Utility.RandomChance(CoreAI.MagicGearDropChance))
            {
                if ((int)level <= 3)
                    return 3;
                else
                    return ((int)level - CoreAI.MagicGearDropDowngrade);
            }

            // causes the item to be deleted
            return -1;
        }
        /*private static bool IsSlayer(Item item)
        {
            if (item is BaseWeapon wep)
                if (wep.Slayer != SlayerName.None)
                    return true;

            return false;
        }*/
        public static Item RandomMagicClothingOrJewelry()
        {
            Item item = null;
            if (Utility.RandomBool())
            {
                switch (Utility.Random(4))
                {
                    case 0:
                        item = new Boots();
                        break;

                    case 1:
                        item = new Cloak();
                        break;

                    case 2:
                        item = new BodySash();
                        break;

                    case 3:
                        item = new ThighBoots();
                        break;
                }

                ((BaseClothing)item).SetRandomMagicEffect(1, 3);
            }
            else
            {
                item = RandomJewelry();
                ((BaseJewel)item).SetRandomMagicEffect(1, 3);

            }

            return item;
        }
        public const uint RedKeyValue = 0xDEADBEEF;
        public const uint BlueKeyValue = 0xBEEFFACE;
        public const uint YellowKeyValue = 0xBEEFCAFE;
        public static Dictionary<uint, int> KeyHueLookup = new Dictionary<uint, int>() { { RedKeyValue, 0x0668 }, { BlueKeyValue, 0x0546 }, { YellowKeyValue, 0x06A5 } };
        public static Item ThrottleWeaponOrArmor(Item item, ImbueLevel level)
        {
            ImbueLevel oldLevel = level;
            // Throttle this drop
            // down grade/eliminate weapons and armor as needed
            // This routine can either downgrade the gear, or eliminate it all together.
            // see: CoreAI.MagicGearDropDowngrade and CoreAI.MagicGearDropChance
            level = (ImbueLevel)MagicGearThrottle(level);
            if (level == ((ImbueLevel)(-1)))    // invalid(-1) means eliminate the gear and replace with something else or nothing
            {
                if (item != null && item.Deleted == false)
                {
                    item.Delete();
                    item = null;
                }

                if (0.03 >= Utility.RandomDouble())
                {
                    item = Loot.RandomMagicClothingOrJewelry();
                    return item;
                }
                else if (0.02 >= Utility.RandomDouble())
                {
                    Type t = m_WandTypes[Utility.Random(m_WandTypes.Length)];
                    item = (Item)Activator.CreateInstance(t);
                    return item;
                }
                else if (0.01 >= Utility.RandomDouble())
                {
                    return RareFactoryItem(1.0);
                }
                else
                {   // 30% chance at a key
                    return UnusualKey(oldLevel, 0.3);
                }
            }
            else
                return item;
        }
        // see test harness in Nuke TestUnusualKey
        public static Key UnusualKey(ImbueLevel level, double chance = 1.0)
        {
            if (chance >= Utility.RandomDouble())
            {
                Item item = new Key(KeyType.Magic);
                item.Name = "an unusual key";
                for (;/*keep going until we get a key*/; )
                {
                    double[,] chanceTab = new double[7, 3] {
                                   {0.010, 0.015, 0.300},   // level 0, chance for red, blue, yellow
                                   {0.040, 0.060, 0.400},   // level 1, chance for red, blue, yellow
                                   {0.050, 0.070, 0.500},   // level 2, chance for red, blue, yellow
                                   {0.060, 0.080, 0.600},   // level 3, chance for red, blue, yellow
                                   {0.070, 0.090, 0.500},   // level 4, chance for red, blue, yellow
                                   {0.080, 0.100, 0.400},   // level 5, chance for red, blue, yellow
                                   {0.090, 0.110, 0.300},   // level 6, chance for red, blue, yellow
                                };
                    bool redChance = chanceTab[(int)level, 0] >= Utility.RandomDouble();
                    bool blueChance = chanceTab[(int)level, 1] >= Utility.RandomDouble();
                    bool yellowChance = chanceTab[(int)level, 2] >= Utility.RandomDouble();

                    if (redChance)
                    {   // red key
                        item.Hue = KeyHueLookup[RedKeyValue];
                        (item as Key).KeyValue = RedKeyValue;
                        return item as Key;
                    }
                    else if (blueChance)
                    {   // blue key
                        item.Hue = KeyHueLookup[BlueKeyValue];
                        (item as Key).KeyValue = BlueKeyValue;
                        return item as Key;
                    }
                    else if (yellowChance)
                    {   // yellow key
                        item.Hue = KeyHueLookup[YellowKeyValue];
                        (item as Key).KeyValue = YellowKeyValue;
                        return item as Key;
                    }
                }
            }

            return null;
        }
        // see test harness in Nuke TestRandomGear
        public static T GetGearForLevel<T>(int level, double upgrade_chance = 0.0)
        {
            var v = Enum.GetValues(typeof(T));
            int[][] chance_table = Loot.LootTab();

            // add a chance at a item upgrade
            if ((level < chance_table.Length - 1) && (upgrade_chance > Utility.RandomDouble()))
                level++;

            int attributeLevel;
            attributeLevel = chance_table[level][Utility.Random(chance_table[level].Length)];

            return (T)v.GetValue(attributeLevel);
        }
        public static Item ImbueWeaponOrArmor(Item item, int min, int max)
        {
            return ImbueWeaponOrArmor(false, item, Utility.RandomEnumMinMaxScaled<ImbueLevel>(min, max), 0, false);
        }
        public static Item ImbueWeaponOrArmor(Item item, int level)
        {
            ImbueLevel value = ImbueLevel.Level0;
            if (Enum.IsDefined(typeof(ImbueLevel), level))
                value = (ImbueLevel)level;

            return ImbueWeaponOrArmor(false, item, value, 0, false);
        }
        // see test harness in Nuke TestImbuing()
        public static Item ImbueWeaponOrArmor(bool noThrottle, Item item, ImbueLevel level, double upgrade_chance, bool hueing)
        {
            if (item == null)
                return item;

            // do the transformation
            item = ImbueWeaponOrArmor(item, level, upgrade_chance, hueing);

            // convert to non WeaponOrArmor if throttling is on and noThrottle == false
            if (ChecMagicGearThrottle(noThrottle) == false)
                item = ThrottleWeaponOrArmor(item, level);

            return item;
        }

        // Adam: used for treasure chests / mini-bosses
        public static int[][] LootTab()
        {
            // Adam: decrease the chances to get the max level attribute for this level
            // Create an unevenly weighted table for chance resolution

            // start treasure chests
            int[] level0 = new int[] { 0 };             // regular

            // start treasure chests
            int[] level1 = new int[] { 0,0,0,0,0,		// regular
										  1,1,1 };      // Ruin,	Defense

            int[] level2 = new int[] { 0,0,0,			// regular
										  1,1,1,1,1,1,	// Ruin,	Defense
										  2,2,2 };      // Might,	Guarding

            int[] level3 = new int[] { 0,0,0,			// regular
										  1,1,1,		// Ruin,	Defense
										  2,2,2,2,2,2,	// Might,	Guarding
										  3,3,3 };      // Force,	Hardening

            int[] level4 = new int[] { 1,1,1,1,1,		// Ruin,	Defense
										  2,2,2,2,2,2,	// Might,	Guarding
										  3,3,3,3,3,	// Force,	Hardening
										  4,4 };        // Power,	Fortification

            int[] level5 = new int[] { 1,1,1,1,		    // Ruin,	Defense
										  2,2,2,2,2,	// Might,	Guarding
										  3,3,3,3,3,3,	// Force,	Hardening
										  4,4,4,4,4,	// Power,	Fortification
										  5,5 };        // Vanq,

            // mini boss loot table
            int[] level6 = new int[] { 3,3,3,3,		    // force
										  4,4,4,4,		// Power
										  5,5 };        // Vanq,	Invulnerability

            int[][] chance_table = new int[][]
            {
                level0, level1, level2, level3, level4, level5, level6
            };

            return chance_table;
        }
        // Adam: used for treasure chests / mini-bosses
        private static Item ImbueWeaponOrArmor(Item item, ImbueLevel level, double upgrade_chance, bool hueing)
        {
            int[][] chance_table = Loot.LootTab();

            // level is 1-table size
            if ((int)level > chance_table.Length)
            {
                LogHelper Logger = new LogHelper("MisuseOfLootGeneration.log", false, true);
                Logger.Log(LogType.Text, string.Format("Item Name: {0}, Item Serial: {1}, World Location:{2}", item.Name, item.Serial, item.GetWorldLocation()));
                Logger.Log(LogType.Text, string.Format("(level > chance.Length || level > Loot.ImbueLevel.LevelMax) == true"));
                Logger.Log(LogType.Text, new System.Diagnostics.StackTrace());
                Logger.Finish();
                // regular item
                return item;
            }

            // add a chance at a item upgrade
            // we -1 in ImbueWeaponOrArmor(), so being at chance.Length after level++ is cool
            if (((int)level < chance_table.Length - 1) && (upgrade_chance > Utility.RandomDouble()))
                level++;

            // If after all of this (no upgrade,) the level is Level0, just return the item.
            if (level == Loot.ImbueLevel.Level0)
                return item;

            return ImbueWeaponOrArmor(item, level, chance_table, hueing);
        }

        public static Item ImbueWeaponOrArmor(Item item, Loot.ImbueLevel level, int[][] chance_table, bool hueing)
        {
            if (item == null)
                return item;

            //our different resource metal types and weight table
            int[] color = new int[]
            {
                9,					// Valorite,	1 in 36 chance
				8,8,				// Verite,		2 in 36 chance
				7,7,7,				// Agapite,		3 in 36 chance
				6,6,6,6,			// Gold,		4 in 36 chance
				5,5,5,5,5,			// Bronze,		5 in 36 chance
				4,4,4,4,4,4,		// Copper,		6 in 36 chance
				1,1,1,1,1,1,1,		// ShadowIron,	7 in 36 chance
				2,2,2,2,2,2,2,2		// DullCopper,	8 in 36 chance
			};

            if (item is BaseWeapon)
            {
                BaseWeapon weapon = (BaseWeapon)item;

                // store the imbue level in the item
                weapon.ImbueLevel = (Byte)level;

                // some callers may set weapon.Slayer property before calling Imbue. We will unset it in Imbue, then reapply based on our drop chance.
                weapon.Slayer = SlayerName.None;

                // add a 5% chance at a slayer weapon
                //  The docs are unclear if slayer weapons were available during our Siege era, but the discussion of the boards
                //  was to keep them.
                if (CoreAI.SlayerWeaponDropRate >= Utility.RandomDouble())
                    /*if (Core.RuleSets.SiegeStyleRules())
                        weapon.Slayer = SlayerName.Silver;
                    else*/
                    weapon.Slayer = BaseRunicTool.GetRandomSlayer();

                if (Core.RuleSets.SiegeStyleRules())
                    // add a 5% chance at a MagicEnchantment
                    if (CoreAI.EnchantedEquipmentDropRate >= Utility.RandomDouble())
                    {
                        int[] minmax = ImbueLevelToMagicEnchantment(level);
                        weapon.SetRandomMagicEffect(minmax[0], minmax[1]);
                    }

                // find an appropriate weapon attribute for this level
                int DamageLevel;
                DamageLevel = chance_table[(int)level][Utility.Random(chance_table[(int)level].Length)];
                // Console.WriteLine(((int)DamageLevel).ToString());

                weapon.DamageLevel = (WeaponDamageLevel)DamageLevel;
                weapon.AccuracyLevel = Utility.RandomEnumValue<WeaponAccuracyLevel>();
                weapon.DurabilityLevel = Utility.RandomEnumValue<WeaponDurabilityLevel>();

                // hue it baby!
                if (hueing == true)
                {
                    weapon.Resource = (CraftResource)color[Utility.Random(color.Length)];

                    // hueing rules: don't hue bows or staves
                    if ((weapon is BaseRanged) || (weapon is BaseStaff))
                        weapon.Hue = 0;
                }
            }
            else if (item is BaseArmor)
            {
                BaseArmor armor = (BaseArmor)item;

                // store the imbue level in the item
                armor.ImbueLevel = (Byte)level;

                if (Core.RuleSets.SiegeStyleRules())
                    // add a 5% chance at a MagicEnchantment
                    if (CoreAI.EnchantedEquipmentDropRate >= Utility.RandomDouble())
                    {
                        int[] minmax = ImbueLevelToMagicEnchantment(level);
                        armor.SetRandomMagicEffect(minmax[0], minmax[1]);
                    }

                // find an appropriate armor attribute for this level
                int ProtectionLevel;
                ProtectionLevel = chance_table[(int)level][Utility.Random(chance_table[(int)level].Length)];
                // Console.WriteLine(((int)ProtectionLevel).ToString());

                armor.ProtectionLevel = (ArmorProtectionLevel)ProtectionLevel;
                armor.DurabilityLevel = Utility.RandomEnumValue<ArmorDurabilityLevel>();

                // hue it baby!
                if (hueing == true)
                {
                    armor.Resource = (CraftResource)color[Utility.Random(color.Length)];
                }
            }

            return item;
        }
        public static Item RandomMagicWeapon(int minLevel, int maxLevel, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return null;

            BaseWeapon weapon = Loot.RandomWeapon();

            if (weapon == null)
                return null;

            Item item = Loot.ImbueWeaponOrArmor((Item)weapon, minLevel, maxLevel);

            return item;
        }
        public static Item RandomMagicClothing(int minLevel, int maxLevel, double chance)
        {
            Item item = Construct(m_MagicClothingTypes);
            if (item != null)
                ((BaseClothing)item).SetRandomMagicEffect(minLevel, maxLevel);

            return item;
        }
        public static Item RandomMagicJewelry(int minLevel, int maxLevel, double chance)
        {
            Item item = Construct(m_JewelryTypes);
            if (item != null)
                ((BaseJewel)item).SetRandomMagicEffect(minLevel, maxLevel);

            return item;
        }
        public static Item RandomMagicWand(int minLevel, int maxLevel, double chance)
        {
            Wand wand = new Wand();

            if (wand != null)
                wand.SetRandomMagicEffect(minLevel, maxLevel);

            return wand;
        }
        public static Item RandomMagicArmor(int minLevel, int maxLevel, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return null;

            BaseArmor armor = Loot.RandomArmorOrShield();

            if (armor == null)
                return null;

            Item item = Loot.ImbueWeaponOrArmor((Item)armor, minLevel, maxLevel);

            return item;
        }
        public static void Cap(ref int val, int min, int max)
        {
            if (val < min)
                val = min;
            else if (val > max)
                val = max;
        }
        public static Item RandomIOB()
        {
            switch (Utility.Random(7))
            {
                case 0: // Undead - GUL
                    {
                        if (Utility.RandomBool())
                            return new BloodDrenchedBandana();
                        else
                        {
                            BodySash sash = new BodySash();
                            sash.Hue = 0x66C;
                            sash.IOBAlignment = IOBAlignment.Council;
                            sash.Name = "blood drenched sash";
                            sash.Dyable = false;
                            sash.Scissorable = false;
                            return sash;
                        }
                    }
                case 1: // Undead - UND
                    {
                        Sandals sandals = new Sandals();
                        if (Utility.RandomBool())
                            sandals.Hue = 0x66C;
                        else
                            sandals.Hue = 0x1;
                        sandals.IOBAlignment = IOBAlignment.Undead;
                        sandals.Name = "sandals of the walking dead";
                        sandals.Dyable = false;
                        sandals.Scissorable = false;
                        return sandals;


                    }
                case 2: // Orcish
                    {
                        if (Utility.RandomBool())
                        {   // green mask (brute color)
                            if (Utility.RandomBool())
                                return new OrcishKinMask();
                            else
                            {   // old style mask (orc colored)
                                OrcishKinMask mask = new OrcishKinMask();
                                mask.Hue = 0;
                                return mask;
                            }

                        }
                        else
                        {
                            return new OrcishKinHelm();

                        }
                    }
                case 3: //Savage
                    {
                        if (Utility.RandomBool())
                        {
                            if (Utility.RandomBool())
                            {
                                BearMask mask = new BearMask();
                                mask.IOBAlignment = IOBAlignment.Savage;
                                mask.Name = "bear mask of savage kin";
                                mask.Dyable = false;
                                return mask;
                            }
                            else
                            {
                                DeerMask mask = new DeerMask();
                                mask.IOBAlignment = IOBAlignment.Savage;
                                mask.Name = "deer mask of savage kin";
                                mask.Dyable = false;
                                return mask;
                            }
                        }
                        else
                        {
                            SavageMask mask = new SavageMask();
                            mask.IOBAlignment = IOBAlignment.Savage;
                            mask.Name = "tribal mask of savage kin";
                            mask.Dyable = false;
                            return mask;
                        }
                    }
                case 4: // Pirates
                    {
                        if (Utility.RandomBool())
                        {
                            if (Utility.RandomBool())
                            {
                                SkullCap skullcap = new SkullCap();
                                skullcap.IOBAlignment = IOBAlignment.Pirate;
                                skullcap.Name = "a pirate skullcap";
                                skullcap.Hue = 0x66C;
                                skullcap.Dyable = false;
                                skullcap.Scissorable = false;
                                return skullcap;
                            }
                            else
                            {
                                Boots boots = new Boots();
                                boots.IOBAlignment = IOBAlignment.Pirate;
                                boots.Name = "pirate kin boots";
                                boots.Hue = 0x66c;
                                boots.Dyable = false;
                                boots.Scissorable = false;
                                return boots;
                            }
                        }
                        else
                        {
                            return new PirateHat();
                        }

                    }
                case 5: // Brigands
                    {
                        if (Utility.RandomBool())
                        {
                            return new BrigandKinBandana();
                        }
                        else
                        {
                            return new BrigandKinBoots();
                        }
                    }
                case 6: // Good
                    {
                        switch (Utility.Random(4))
                        {
                            case 0:
                                Boots boots = new Boots(0x5E4);
                                boots.IOBAlignment = IOBAlignment.Good;
                                boots.Name = "Britannian Militia";
                                boots.Dyable = false;
                                return boots;
                            case 1:
                                Cloak cloak = new Cloak(Utility.RandomSpecialVioletHue());
                                cloak.IOBAlignment = IOBAlignment.Good;
                                cloak.Name = "Britannian Militia";
                                cloak.Dyable = false;
                                cloak.Scissorable = false;
                                return cloak;
                            case 2:
                                Surcoat surcoat = new Surcoat(Utility.RandomSpecialVioletHue());
                                surcoat.IOBAlignment = IOBAlignment.Good;
                                surcoat.Name = "Britannian Militia";
                                surcoat.Dyable = false;
                                surcoat.Scissorable = false;
                                return surcoat;
                            case 3:
                                BodySash bodySash = new BodySash(Utility.RandomSpecialRedHue());
                                bodySash.IOBAlignment = IOBAlignment.Good;
                                bodySash.Name = "Britannian Militia";
                                bodySash.Dyable = false;
                                bodySash.Scissorable = false;
                                return bodySash;
                        }
                        break;
                    }
            }
            return null;

        }

        public static SpellScroll RandomScroll(int minIndex, int maxIndex, SpellbookType type)
        {
            Type[] types;

            switch (type)
            {
                default:
                case SpellbookType.Regular: types = m_RegularScrollTypes; break;
                case SpellbookType.Necromancer: types = m_NecromancyScrollTypes; break;
                case SpellbookType.Paladin: types = m_PaladinScrollTypes; break;
            }

            return Construct(types, Utility.RandomMinMax(minIndex, maxIndex)) as SpellScroll;
        }
        #endregion
        #region Standard Loot Packs

        public static Item[] StandardMagicLoot(int level, int scroll_count = 0, int item_count = 0, int reagent_count = 0, int gem_count = 0)
        {
            List<Item> list = new List<Item>();
            Backpack cont = new Backpack();
            Loot.ImbueLevel imbueLevel = Loot.TreasureMapLevelToImbueLevel(level);
            if (imbueLevel != Loot.ImbueLevel.Level0)
            {
                #region Scrolls
                for (int i = 0; i < level * scroll_count; ++i)
                    cont.StackItem(Loot.RandomScroll(0, 63, SpellbookType.Regular));
                #endregion Scrolls

                #region Magic Items
                int[] minmax = Loot.ImbueLevelToMagicEnchantment(imbueLevel);
                for (int i = 0; i < level * item_count; ++i)
                {
                    Item item = null;
                    switch (Utility.Random(4))
                    {
                        case 0:
                        case 1:
                        case 2:
                            // magic wand chance (currently 2%)
                            if (Utility.Chance(CoreAI.MagicWandDropChance))
                            {   // wand
                                if ((item = new Wand()) != null)
                                {
                                    ((Wand)item).SetRandomMagicEffect(minmax[0], minmax[1]);
                                    cont.DropItem(item);
                                }
                            }
                            else
                            {   // weapon or armor
                                if (Utility.RandomBool())
                                    item = Loot.RandomArmorOrShield();
                                else
                                    item = Loot.RandomWeapon();
                                if (item != null)
                                {
                                    item = Loot.ImbueWeaponOrArmor(item, level);
                                    cont.DropItem(item);
                                }
                            }
                            break;
                        case 3:
                            // clothing or jewelry
                            if ((item = Loot.RandomClothingOrJewelry(must_support_magic: true)) != null)
                            {
                                if (item is BaseClothing)
                                    ((BaseClothing)item).SetRandomMagicEffect(minmax[0], minmax[1]);
                                else if (item is BaseJewel)
                                    ((BaseJewel)item).SetRandomMagicEffect(minmax[0], minmax[1]);

                                if (item != null)
                                    cont.DropItem(item);
                            }
                            break;
                    }
                }
                #endregion Magic Items

                #region Reagents
                {
                    for (int i = 0; i < level * reagent_count; ++i)
                        cont.StackItem(Loot.RandomPossibleReagent());
                }
                #endregion Reagents

                #region Gems
                {
                    for (int i = 0; i < ((level == 0) ? 2 : level * gem_count); ++i)
                        cont.StackItem(Loot.RandomGem());
                }
                #endregion Gems

                #region Pack it up
                foreach (Item item in cont.Items)
                    list.Add(item);

                foreach (Item item in list)
                    cont.RemoveItem(item);

                cont.Delete();
                #endregion Pack it up
            }
            return list.ToArray();
        }
        #endregion Standard Loot Packs
        #region Construction methods
        public static Item Construct(Type type)
        {
            try
            {
                return Activator.CreateInstance(type) as Item;
            }
            catch
            {
                return null;
            }
        }

        public static Item Construct(Type[] types)
        {
            if (types.Length > 0)
                return Construct(types, Utility.Random(types.Length));

            return null;
        }

        public static Item Construct(Type[] types, int index)
        {
            if (index >= 0 && index < types.Length)
                return Construct(types[index]);

            return null;
        }

        public static Item Construct(params Type[][] types)
        {
            int totalLength = 0;

            for (int i = 0; i < types.Length; ++i)
                totalLength += types[i].Length;

            if (totalLength > 0)
            {
                int index = Utility.Random(totalLength);

                for (int i = 0; i < types.Length; ++i)
                {
                    if (index >= 0 && index < types[i].Length)
                        return Construct(types[i][index]);

                    index -= types[i].Length;
                }
            }

            return null;
        }
        #endregion
    }
}