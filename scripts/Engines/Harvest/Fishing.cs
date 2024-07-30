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

/* Scripts/Engines/Harvest/Fishing.cs
 * ChangeLog
 *  10/14/23, Yoar
 *      Brought back level 5 MiBs
 *  7/13/2023, Adam (special MiB lootz (rares))
 *      Replace AngelIslandRules() => SpecialMibRares()
 *      in SpecialMibRares(), enable Siege.
 *  7/2/23, Yoar
 *      Changed the Siege SOS modifier from -50% to -33%
 *  7/1/23, Yoar
 *      Added special mutate table for Siege
 *      Refactored Good Fishing
 *  5/30/2023, Adam (Siege era)
 *      I've turned off all the AI special rates and gold reductions and replaced it with era accurate loot
 *      https://web.archive.org/web/20010801143153/http://uo.stratics.com/strat/treashunt.shtml#day3
 *  11/13/22, Adam (Siege)
 *      Fishermen will notice a decrease in the rate that they find SOS bottles and will not be able to fish up treasure maps at all
 *      https://www.uoguide.com/Siege_Perilous
 *      Decrease chance by max 50% for a MessageInABottle 
 *      Decrease chance to 0% for a TreasureMap
 *  11/29/21, Yoar
 *      Added the fields Fisher and Caught to BigFish.
 *      Setting these fields when a player catches a big fish.
 *	9/26/10, adam
 *		Increase leveled sos loot drop to (level * 8) so that a level 1 sos is 8% chance at a rare and level 5 is a 40% chance
 *		Also increase weapon drop a bit
 *	9/23/10, Adam
 *		Add new fishing system and bonus loot
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/16/04, mith
 *		Modified FinishHarvesting() to increase the range. 
 *			This allows for fishing while moving slow forward. The range is reset after FinishHarvesting() is done.
 */

using Server.Engines.Quests;
using Server.Engines.Quests.Collector;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using System;

namespace Server.Engines.Harvest
{
    public class Fishing : HarvestSystem
    {
        private static Fishing m_System;

        public static Fishing System
        {
            get
            {
                if (m_System == null)
                    m_System = new Fishing();

                return m_System;
            }
        }

        private HarvestDefinition m_Definition;

        public HarvestDefinition Definition
        {
            get { return m_Definition; }
        }

        private Fishing()
        {
            HarvestResource[] res;
            HarvestVein[] veins;

            #region Fishing
            HarvestDefinition fish = new HarvestDefinition();

            // Resource banks are every 8x8 tiles
            fish.BankWidth = 8;
            fish.BankHeight = 8;

            // Every bank holds from 5 to 15 fish
            fish.MinTotal = 5;
            fish.MaxTotal = 15;

            // A resource bank will respawn its content every 10 to 20 minutes
            fish.MinRespawn = TimeSpan.FromMinutes(10.0);
            fish.MaxRespawn = TimeSpan.FromMinutes(20.0);

            // Skill checking is done on the Fishing skill
            fish.Skill = SkillName.Fishing;

            // Set the list of harvestable tiles
            fish.Tiles = m_WaterTiles;
            fish.RangedTiles = true;

            // Players must be within 4 tiles to harvest
            fish.MaxRange = 4;

            // One fish per harvest action
            fish.ConsumedPerHarvest = 1;
            fish.ConsumedPerFeluccaHarvest = 1;

            // The fishing
            fish.EffectActions = new int[] { 12 };
            fish.EffectSounds = new int[0];
            fish.EffectCounts = new int[] { 1 };
            fish.EffectDelay = TimeSpan.Zero;
            fish.EffectSoundDelay = TimeSpan.FromSeconds(8.0);

            fish.NoResourcesMessage = 503172; // The fish don't seem to be biting here.
            fish.FailMessage = 503171; // You fish a while, but fail to catch anything.
            fish.TimedOutOfRangeMessage = 500976; // You need to be closer to the water to fish!
            fish.OutOfRangeMessage = 500976; // You need to be closer to the water to fish!
            fish.PackFullMessage = 503176; // You do not have room in your backpack for a fish.
            fish.ToolBrokeMessage = 503174; // You broke your fishing pole.

            res = new HarvestResource[]
                {
                    new HarvestResource( 00.0, 00.0, 100.0, 1043297, typeof( Fish ) )
                };

            veins = new HarvestVein[]
                {
                    new HarvestVein( 100.0, 0.0, res[0], null )
                };

            fish.Resources = res;
            fish.Veins = veins;

            m_Definition = fish;
            Definitions.Add(fish);
            #endregion
        }

        public override void OnConcurrentHarvest(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            from.SendLocalizedMessage(500972); // You are already fishing.
        }

        private class MutateEntry
        {
            public double m_ReqSkill, m_MinSkill, m_MaxSkill;
            public bool m_DeepWater;
            public Type[] m_Types;

            public MutateEntry(double reqSkill, double minSkill, double maxSkill, bool deepWater, params Type[] types)
            {
                m_ReqSkill = reqSkill;
                m_MinSkill = minSkill;
                m_MaxSkill = maxSkill;
                m_DeepWater = deepWater;
                m_Types = types;
            }
        }

        private static readonly MutateEntry[] m_MutateTable = new MutateEntry[]
            {
                new MutateEntry(  80.0,  80.0,  4080.0,  true, typeof( SpecialFishingNet ) ),
                new MutateEntry(  80.0,  80.0,  4080.0,  true, typeof( BigFish ) ),
                new MutateEntry(  90.0,  80.0,  4080.0,  true, typeof( TreasureMap ) ),
                new MutateEntry( 100.0,  80.0,  4080.0,  true, typeof( MessageInABottle ) ),
                new MutateEntry(   0.0, 125.0, -2375.0, false, typeof( PrizedFish ), typeof( WondrousFish ), typeof( TrulyRareFish ), typeof( PeculiarFish ) ),
                new MutateEntry(   0.0, 105.0,  -420.0, false, typeof( Boots ), typeof( Shoes ), typeof( Sandals ), typeof( ThighBoots ) ),
                new MutateEntry(   0.0, 200.0,  -200.0, false, new Type[1] { null } )
            };

        /* 7/1/23, Yoar: New mutate table for SP
         * Fishermen will notice a decrease in the rate that they find SOS bottles and will not be able to fish up treasure maps at all
         * https://www.uoguide.com/Siege_Perilous
         */
        private static readonly MutateEntry[] m_MutateTableSP = new MutateEntry[]
            {
                new MutateEntry(  80.0,  80.0,  4080.0,  true, typeof( SpecialFishingNet ) ),
                new MutateEntry(  80.0,  80.0,  4080.0,  true, typeof( BigFish ) ),
                new MutateEntry( 100.0,  80.0,  6080.0,  true, typeof( MessageInABottle ) ),
                new MutateEntry(   0.0, 125.0, -2375.0, false, typeof( PrizedFish ), typeof( WondrousFish ), typeof( TrulyRareFish ), typeof( PeculiarFish ) ),
                new MutateEntry(   0.0, 105.0,  -420.0, false, typeof( Boots ), typeof( Shoes ), typeof( Sandals ), typeof( ThighBoots ) ),
                new MutateEntry(   0.0, 200.0,  -200.0, false, new Type[1] { null } )
            };

        public override bool SpecialHarvest(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc)
        {
            PlayerMobile player = from as PlayerMobile;

            if (player != null)
            {
                QuestSystem qs = player.Quest;

                if (qs is CollectorQuest)
                {
                    QuestObjective obj = qs.FindObjective(typeof(FishPearlsObjective));

                    if (obj != null && !obj.Completed)
                    {
                        if (Utility.RandomDouble() < 0.5)
                        {
                            player.SendLocalizedMessage(1055086, "", 0x59); // You pull a shellfish out of the water, and find a rainbow pearl inside of it.

                            obj.CurProgress++;
                        }
                        else
                        {
                            player.SendLocalizedMessage(1055087, "", 0x2C); // You pull a shellfish out of the water, but it doesn't have a rainbow pearl.
                        }

                        return true;
                    }
                }
            }

            #region Fish Controller
            FishController controller = FishController.Find(from, loc);

            if (controller != null)
            {
                Item fish;
                string message;

                controller.OnFish(from, out fish, out message);

                if (fish != null)
                    from.AddToBackpack(fish);

                if (message != null)
                    from.SendMessage(message);

                return true;
            }
            #endregion

            return false;
        }

        public override Type MutateType(Type type, Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, HarvestResource resource)
        {
            bool deepWater = SpecialFishingNet.FullValidation(map, loc.X, loc.Y);
            bool goodFishing = GoodFishingHere(from, new Point2D(loc));

            double skillBase = from.Skills[SkillName.Fishing].Base;
            double skillValue = from.Skills[SkillName.Fishing].Value;

            MutateEntry[] mutateTable;

            if (Core.RuleSets.SiegeStyleRules())
                mutateTable = m_MutateTableSP;
            else
                mutateTable = m_MutateTable;

            for (int i = 0; i < mutateTable.Length; ++i)
            {
                MutateEntry entry = mutateTable[i];

                // no deepwater check if you are in a fishing hotspot
                if (!goodFishing && !deepWater && entry.m_DeepWater)
                    continue;

                if (skillBase >= entry.m_ReqSkill)
                {
                    double chance = (skillValue - entry.m_MinSkill) / (entry.m_MaxSkill - entry.m_MinSkill);

                    // check for sea quest bonus
                    if (goodFishing)
                        chance *= GoodFishingBonus(from, new Point2D(loc));

                    if (chance > Utility.RandomDouble())
                        return entry.m_Types[Utility.Random(entry.m_Types.Length)];
                }
            }

            return type;
        }

        #region Goood Fishin'

        public static int GoodFishingHereRange = 4;
        public static int GoodFishingNearRange = 10;

        private bool GoodFishingHere(Mobile from, Point2D loc)
        {
            BaseBoat boat = BaseBoat.FindBoatAt(from);

            return (boat != null && boat.GoodFishing && Utility.GetDistanceToSqrt(loc, boat.GoodFishingTarget) < GoodFishingHereRange);
        }

        private bool GoodFishingNear(Mobile from, Point2D loc)
        {
            BaseBoat boat = BaseBoat.FindBoatAt(from);

            return (boat != null && boat.GoodFishing && Utility.GetDistanceToSqrt(loc, boat.GoodFishingTarget) < GoodFishingNearRange);
        }

        private void GoodFishingAdvice(Mobile from, Point2D loc, bool noRes)
        {
            string hint = null;

            if (GoodFishingHere(from, new Point2D(loc)))
            {
                if (noRes)
                {
                    switch (Utility.Random(4))
                    {
                        case 0: hint = "Maybe try fishing fore and aft?"; break;
                        case 1: hint = "Have you tried port side?"; break;
                        case 2: hint = "Have you tried the starboard side?"; break;
                        case 3: hint = "Ar, thar's got to be fish here somewhere!"; break;
                    }
                }
                else if (Utility.Random(10) == 0)
                {
                    switch (Utility.Random(3))
                    {
                        case 0: hint = "Ar, ye be in the right spot there skipper."; break;
                        case 1: hint = "Methinks that be a good spot."; break;
                        case 2: hint = "Ar, too bad skipper. Try again."; break;
                    }
                }
            }
            else if (GoodFishingNear(from, new Point2D(loc)))
            {
                hint = "Ar, why ye be fishing over there?";
            }

            if (hint != null)
                GoodFishingHint(from, hint);
        }

        private double GoodFishingBonus(Mobile from, Point2D loc)
        {
            BaseBoat boat = BaseBoat.FindBoatAt(from);

            if (boat != null && boat.GoodFishing && Utility.GetDistanceToSqrt(loc, boat.GoodFishingTarget) < GoodFishingHereRange)
            {
                /* 7/7/23, Yoar: Make the bonus independent of sea chart level
                 * This way, lower level sea charts are worth people's time
                 */
#if false
                switch (boat.GoodFishingLevel)
                {
                    case 1:
                        return 2.0;
                    case 2:
                        return 3.0;
                    case 3:
                        return 4.0;
                    case 4:
                        return 5.0;
                    case 5:
                        return 6.0;
                }
#else
                return 4.0;
#endif
            }

            return 1.0;
        }

        private int GoodFishingLevel(Mobile from, Point2D loc)
        {
            BaseBoat boat = BaseBoat.FindBoatAt(from);

            if (boat != null && boat.GoodFishing && Utility.GetDistanceToSqrt(loc, boat.GoodFishingTarget) < GoodFishingHereRange)
                return boat.GoodFishingLevel;

            return 0;
        }

        private static readonly Memory m_HintMemory = new Memory();

        public static void GoodFishingHint(Mobile from, string message)
        {
            BaseBoat boat = BaseBoat.FindBoatAt(from);

            if (boat != null && m_HintMemory.Recall(boat) == null)
            {
                m_HintMemory.Remember(boat, 6.0);

                if (boat.TillerMan != null)
                    boat.TillerMan.Say(false, message);
            }
        }

        #endregion

        private static Map SafeMap(Map map)
        {
            if (map == null || map == Map.Internal)
                return Map.Trammel;

            return map;
        }

        public override bool CheckResources(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, bool timed)
        {
            Container pack = from.Backpack;

            if (pack != null)
            {
                Item[] messages = pack.FindItemsByType(typeof(SOS));

                for (int i = 0; i < messages.Length; ++i)
                {
                    SOS sos = (SOS)messages[i];

                    if (from.Map == sos.TargetMap && from.InRange(sos.TargetLocation, 60))
                        return true;
                }
            }

            #region Fish Controller
            if (FishController.Find(from, loc) != null)
                return true;
            #endregion

            bool result = base.CheckResources(from, tool, def, map, loc, timed);

            if (!result)
                GoodFishingAdvice(from, new Point2D(loc), true);

            return result;
        }

        public override Item Construct(Type type, Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, HarvestResource resource)
        {
            if (type == typeof(TreasureMap))
            {
                return new TreasureMap(1, from.Map == Map.Felucca ? Map.Felucca : Map.Trammel);
            }
            else if (type == typeof(MessageInABottle))
            {
                int goodFishingLevel = GoodFishingLevel(from, new Point2D(loc));

                int mibLevel;

                /* 7/7/23, Yoar: Make the MiB level independent of sea chart level
                 * This way, lower level sea charts are worth people's time
                 */
#if false
                mibLevel = goodFishingLevel;
#else
                int rnd = (goodFishingLevel > 0 ? Utility.Random(100) : 100);

                if (rnd < 2)
                    mibLevel = 5; // 2% chance
                else if (rnd < 8)
                    mibLevel = 4; // 6% chance
                else
                    mibLevel = 0; // otherwise, random level 1-3
#endif

                if (mibLevel > 0)
                    return new MessageInABottle(SafeMap(from.Map), mibLevel);
                else
                    return new MessageInABottle(SafeMap(from.Map));
            }

            Container pack = from.Backpack;

            if (pack != null)
            {
                #region SOS Fishing

                Item[] messages = pack.FindItemsByType(typeof(SOS));

                for (int i = 0; i < messages.Length; ++i)
                {
                    if (messages[i] is SOS sos && sos.Deleted == false)
                    {
                        if (from.Map == sos.TargetMap && from.InRange(sos.TargetLocation, 60))
                        {
                            bool aimo = (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules());

                            Item preLoot = null;

#if true
                            #region RUNUO 2.6 PreLoot
                            switch (Utility.Random(8))
                            {
                                case 0: // Body parts
                                    {
                                        int[] list = new int[]
                                            {
                                        0x1CDD, 0x1CE5, // arm
										0x1CE0, 0x1CE8, // torso
										0x1CE1, 0x1CE9, // head
										0x1CE2, 0x1CEC // leg
                                            };

                                        preLoot = new ShipwreckedItem(Utility.RandomList(list));
                                        break;
                                    }
                                case 1: // Bone parts
                                    {
                                        int[] list = new int[]
                                            {
                                        0x1AE0, 0x1AE1, 0x1AE2, 0x1AE3, 0x1AE4, // skulls
										0x1B09, 0x1B0A, 0x1B0B, 0x1B0C, 0x1B0D, 0x1B0E, 0x1B0F, 0x1B10, // bone piles
										0x1B15, 0x1B16 // pelvis bones
                                            };

                                        preLoot = new ShipwreckedItem(Utility.RandomList(list));
                                        break;
                                    }
                                case 2: // Paintings and portraits
                                    {
                                        preLoot = new ShipwreckedItem(Utility.Random(0xE9F, 10));
                                        break;
                                    }
                                case 3: // Pillows
                                    {
                                        preLoot = new ShipwreckedItem(Utility.Random(0x13A4, 11));
                                        break;
                                    }
                                case 4: // Shells
                                    {
                                        preLoot = new ShipwreckedItem(Utility.Random(0xFC4, 9));
                                        break;
                                    }
                                case 5: //Hats
                                    {
                                        if (Utility.RandomBool())
                                            preLoot = new SkullCap();
                                        else
                                            preLoot = new TricorneHat();

                                        break;
                                    }
                                case 6: // Misc
                                    {
                                        int[] list = new int[]
                                            {
                                        0x1EB5, // unfinished barrel
										0xA2A, // stool
										0xC1F, // broken clock
										0x1047, 0x1048, // globe
										0x1EB1, 0x1EB2, 0x1EB3, 0x1EB4 // barrel staves
                                            };

                                        if (Utility.Random(list.Length + 1) == 0)
                                            preLoot = new Candelabra();
                                        else
                                            preLoot = new ShipwreckedItem(Utility.RandomList(list));

                                        break;
                                    }
                            }

                            if (preLoot != null)
                            {
                                if (preLoot is IShipwreckedItem)
                                    ((IShipwreckedItem)preLoot).IsShipwreckedItem = true;

                                return preLoot;
                            }
                            #endregion RUNUO 2.6 PreLoot
#else
                            #region Old Angel Island PreLoot
                            switch (Utility.Random(7))
                            {
                                case 0: // Body parts
                                    {
                                        int[] list = new int[]
                                        {
                                        0x1CDD, 0x1CE5, // arm
										0x1CE0, 0x1CE8, // torso
										0x1CE1, 0x1CE9, // head
										0x1CE2, 0x1CEC // leg
                                        };

                                        preLoot = new ShipwreckedItem(Utility.RandomList(list));
                                        break;
                                    }
                                case 1: // Bone parts
                                    {
                                        int[] list = new int[]
                                        {
                                        0x1AE0, 0x1AE1, 0x1AE2, 0x1AE3, 0x1AE4, // skulls
										0x1B09, 0x1B0A, 0x1B0B, 0x1B0C, 0x1B0D, 0x1B0E, 0x1B0F, 0x1B10, // bone piles
										0x1B15, 0x1B16 // pelvis bones
                                        };

                                        preLoot = new ShipwreckedItem(Utility.RandomList(list));
                                        break;
                                    }
                                case 2: // Paintings and portraits
                                    {
                                        preLoot = new ShipwreckedItem(Utility.Random(0xE9F, 10));
                                        break;
                                    }
                                case 3: // Pillows
                                    {
                                        preLoot = new ShipwreckedItem(Utility.Random(0x13A4, 11));
                                        break;
                                    }
                                case 4: // Shells
                                    {
                                        preLoot = new ShipwreckedItem(Utility.Random(0xFC4, 9));
                                        break;
                                    }
                                case 5: // Misc
                                    {
                                        int[] list = new int[]
                                        {
                                        0x1EB5, // unfinished barrel
										0xA2A, // stool
										0xC1F, // broken clock
										0x1047, 0x1048, // globe
										0x1EB1, 0x1EB2, 0x1EB3, 0x1EB4 // barrel staves
                                        };

                                        preLoot = new ShipwreckedItem(Utility.RandomList(list));
                                        break;
                                    }
                            }

                            if (preLoot != null)
                                return preLoot;
                            #endregion Old Angel Island PreLoot
#endif

                            Container chest = null;

                            if ((!aimo || sos.Level >= 4) && Utility.RandomChance(10))
                            {
                                if (Utility.RandomChance(20))
                                    chest = new MetalGoldenChest();
                                else
                                    chest = new MetalChest();
                            }
                            else
                                chest = new WoodenChest();

                            // regular sos's have a chance at a 1, 2 or 3. high level sos's are high level chests
                            int level = (sos.Level >= 4) ? sos.Level : Utility.RandomMinMax(1, 3);
                            TreasureMapChest.Fill((chest as LockableContainer), level);

                            // Angel Island only modifications
                            if (aimo)
                            {   // add 50% more gold since we are decreasing the chance to get rares
                                Item[] golds = chest.FindItemsByType(typeof(Gold));
                                if (golds != null && golds.Length > 0)
                                {
                                    int total = 0;
                                    for (int tx = 0; tx < golds.Length; tx++)
                                        total += (golds[tx] as Gold).Amount;

                                    // add 50% more gold
                                    chest.DropItem(new Gold(total / 2));

                                    // add some cursed gold
                                    chest.DropItem(new CursedGold(total / 8));
                                }


                                // adjust high end loot by removing high end weapons and armor for Angel Island
                                //  We don't do this particular reduction for Siege etc. since they already have reduced power in weapon and armor drops.
                                //      see: TreasureMapChest.Fill(): Utility.RandomEnumMinMaxScaled()
                                if (sos.Level >= 4)
                                {
                                    // trim the high end weps and armor so as not to give away all the high end weapons usually reserved for Treasure Map hunters
                                    Item[] lootz = chest.FindItemsByType(new Type[] { typeof(BaseArmor), typeof(BaseWeapon) });
                                    int LootzKeep = level == 4 ? Utility.RandomList(2) + 2 : Utility.RandomList(3) + 2;

                                    if (lootz != null && lootz.Length > LootzKeep)
                                    {
                                        // remove some items as we're dropping too much for the sea chest
                                        int toDel = lootz.Length - LootzKeep;
                                        for (int ox = 0; ox < toDel; ox++)
                                        {
                                            Item dx = lootz[ox];
                                            chest.RemoveItem(dx);
                                            dx.Delete();
                                        }
                                    }
                                }
                            }

                            if (sos.IsAncient)
                                chest.DropItem(new FabledFishingNet());
                            else
                                chest.DropItem(new SpecialFishingNet());

                            // add Good Fishing bonus lootz
                            if (Core.RuleSets.SpecialMibRares())
                            {   // this code is broken. Creates items that are never dropped.
                                //  not critical as normal cleanup will clean them up, but :(
                                if (Utility.RandomChance(sos.Level * 8))
                                {
                                    switch (sos.Level)
                                    {
                                        case 1:
                                            if (Utility.RandomChance(10))
                                            {
                                                Item[] list = new Item[]
                                                {
                                            new AddonDeed(0x0DBB),  // 3515 (0x0DBB) Seaweed
											new WoodDebris(),       // 3118 (0x0C2E) debris - flippable to 0xC2D, 0xC2F, 0xC2E, 0xC30
                                                };
                                                chest.DropItem(Utility.RandomList(list));
                                            }
                                            else
                                            {
                                                Item[] list = new Item[]
                                                {
                                            new AddonDeed(0x154D),		// 5453 (0x054D) water barrel � empty or filled we don't have them on ai yet.
											new AddonDeed(0x0DC9),      // 3529 (0x0DC9) fishing net � unhued and oddly shaped 
                                                };
                                                chest.DropItem(Utility.RandomList(list));
                                            }
                                            break;
                                        case 2:
                                            if (Utility.RandomChance(10))
                                            {
                                                Item[] list = new Item[]
                                                {
                                            new AddonDeed(0x1E9A), // 7834 (0x1E9A) hook
											new AddonDeed(0x1E9D), // 7837 (0x1E9D) pullies
											new AddonDeed(0x1E9E), // 7838 (0x1E9E) Pulley
                                                };
                                                chest.DropItem(Utility.RandomList(list));
                                            }
                                            else
                                            {
                                                Item[] list = new Item[]
                                                {
                                            new AddonDeed(0x1EA0),                  // 7840 (0x1EA0) Rope
											new AddonDeed(0x1EA9,Direction.South),  // 7849 (0x1EA9) Winch � south
											new AddonDeed(0x1EAC,Direction.East),   // 7852 (0x1EAC) Winch � east
                                                };
                                                chest.DropItem(Utility.RandomList(list));
                                            }
                                            break;
                                        case 3:
                                            if (Utility.RandomChance(10))
                                            {
                                                Item[] list = new Item[]
                                                {
                                            new AddonDeed(0x0FCD,Direction.South),  // 4045 (0x0FCD) string of shells � south
											new AddonDeed(0x0FCE,Direction.South),  // 4046 (0x0FCE) string of shells � south
											new AddonDeed(0x0FCF,Direction.South),  // 4047 (0x0FCF) � string of shells � south
											new AddonDeed(0x0FD0,Direction.South),  // 4048 (0x0FD0) � string of shells � south
											new AddonDeed(0x0FD1,Direction.East),   // 4049 (0x0FD1) � string of shells � east
											new AddonDeed(0x0FD2,Direction.East),   // 4050 (0x0FD2) � string of shells � east
											new AddonDeed(0x0FD3,Direction.East),   // 4051 (0x0FD3) � string of shells � east
											new AddonDeed(0x0FD4,Direction.East),   // 4052 (0x0FD4) � string of shells � east
                                                };
                                                chest.DropItem(Utility.RandomList(list));
                                            }
                                            else
                                            {
                                                switch (Utility.Random(4))
                                                {
                                                    case 0:
                                                        // 4099 (0x1003) � Spittoon
                                                        chest.DropItem(new ShipwreckedItem(0x1003));
                                                        break;
                                                    case 1:
                                                        // 4091 (0x0FFB) � Skull mug 1
                                                        chest.DropItem(new ShipwreckedItem(0x0FFB));
                                                        break;
                                                    case 2:
                                                        // 4092 (0x0FFC) � Skull mug 2
                                                        chest.DropItem(new ShipwreckedItem(0x0FFC));
                                                        break;
                                                    case 3:
                                                        // 3700 (0x0E74) Cannon Balls
                                                        chest.DropItem(new AddonDeed(0x0E74));
                                                        break;
                                                }
                                            }
                                            break;
                                        case 4:
                                            if (Utility.RandomChance(10))
                                            {
                                                Item[] list = new Item[]
                                                {
                                            new AddonDeed(0x0C2C), // 3116 (0x0C2C) Ruined Painting
											new AddonDeed(0x0C18), // 3096 (0x0C18) Covered chair - (server birth on osi)
                                                };
                                                chest.DropItem(Utility.RandomList(list));
                                            }
                                            else
                                            {
                                                Item[] list = new Item[]
                                                {
                                            new AddonDeed(0x1EA3,Direction.South),	// 7843 (0x1EA3) net � south
											new AddonDeed(0x1EA4,Direction.East),	// 7844 (0x1EA4) net � east
											new AddonDeed(0x1EA5,Direction.South),	// 7845 (0x1EA5) net � south
											new AddonDeed(0x1EA6,Direction.East),   // 7846 (0x1EA6) net � east
                                                };
                                                chest.DropItem(Utility.RandomList(list));
                                            }
                                            break;
                                        case 5:
                                            if (Utility.RandomChance(10))
                                            {
                                                Item[] list = new Item[]
                                                {
                                            new DarkFlowerTapestrySouthDeed(),
                                            new DarkFlowerTapestryEastDeed(),
                                            new LightTapestrySouthDeed(),
                                            new LightTapestryEastDeed(),
                                                };
                                                chest.DropItem(Utility.RandomList(list));
                                            }
                                            else
                                            {
                                                Item[] list = new Item[]
                                                {
                                            new DarkTapestrySouthDeed(),
                                            new DarkTapestryEastDeed(),
                                            new LightFlowerTapestrySouthDeed(),
                                            new LightFlowerTapestryEastDeed(),
                                                };
                                                chest.DropItem(Utility.RandomList(list));
                                            }
                                            break;
                                    }
                                }

                                // 1 in 1000 chance at the actual item (rare)
                                if (Utility.RandomChance(.1))
                                {
                                    switch (sos.Level)
                                    {
                                        case 1:
                                            if (Utility.RandomChance(10))
                                            {
                                                int[] list = new int[]
                                            {
                                        0x0DBB, // 3515 (0x0DBB) Seaweed
										0x0C2E, // 3118 (0x0C2E) debris
                                            };
                                                chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                            }
                                            else
                                            {
                                                int[] list = new int[]
                                            {
                                        0x154D,		// 5453 (0x054D) water barrel � empty or filled we don't have them on ai yet.
										0x0DC9,     // 3529 (0x0DC9) fishing net � unhued and oddly shaped 
                                            };
                                                chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                            }
                                            break;
                                        case 2:
                                            if (Utility.RandomChance(10))
                                            {
                                                int[] list = new int[]
                                            {
                                        0x1E9A, // 7834 (0x1E9A) hook
                                        0x1E9C, // 7836 (0x1E9C) pullies
                                        0x1E9D, // 7837 (0x1E9D) pullies
										0x1E9E, // 7838 (0x1E9E) Pulley
                                            };
                                                chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                            }
                                            else
                                            {
                                                int[] list = new int[]
                                            {
                                        0x1EA0, // 7840 (0x1EA0) Rope
										0x1EA9, // 7849 (0x1EA9) Winch � south
										0x1EAC, // 7852 (0x1EAC) Winch � east
                                            };
                                                chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                            }
                                            break;
                                        case 3:
                                            if (Utility.RandomChance(10))
                                            {
                                                int[] list = new int[]
                                            {
                                        0x0FCD, // 4045 (0x0FCD) string of shells � south
										0x0FCE, // 4046 (0x0FCE) string of shells � south
										0x0FCF, // 4047 (0x0FCF) � string of shells � south
										0x0FD0, // 4048 (0x0FD0) � string of shells � south
										0x0FD1, // 4049 (0x0FD1) � string of shells � east
										0x0FD2, // 4050 (0x0FD2) � string of shells � east
										0x0FD3, // 4051 (0x0FD3) � string of shells � east
										0x0FD4, // 4052 (0x0FD4) � string of shells � east
                                            };
                                                chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                            }
                                            else
                                            {
                                                int[] list = new int[]
                                            {
                                        0x1003, // 4099 (0x1003) � Spittoon
										0x0FFB, // 4091 (0x0FFB) � Skull mug 1
										0x0FFC, // 4092 (0x0FFC) � Skull mug 2
										0x0E74, // 3700 (0x0E74) Cannon Balls
                                            };
                                                chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                            }
                                            break;
                                        case 4:
                                            if (Utility.RandomChance(10))
                                            {
                                                int[] list = new int[]
                                            {
                                        0x0C2C, // 3116 (0x0C2C) Ruined Painting
										0x0C18, // 3096 (0x0C18) Covered chair - (server birth on osi)
                                            };
                                                chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                            }
                                            else
                                            {
                                                int[] list = new int[]
                                            {
                                        0x1EA3, // 7843 (0x1EA3) net � south
										0x1EA4, // 7844 (0x1EA4) net � east
										0x1EA5, // 7845 (0x1EA5) net � south
										0x1EA6, // 7846 (0x1EA6) net � east
                                            };
                                                chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                            }
                                            break;
                                        case 5: // same frequent drop
                                            if (Utility.RandomChance(10))
                                            {
                                                Item[] list = new Item[]
                                            {
                                        new DarkFlowerTapestrySouthDeed(),
                                        new DarkFlowerTapestryEastDeed(),
                                        new LightTapestrySouthDeed(),
                                        new LightTapestryEastDeed(),
                                            };
                                                chest.DropItem(Utility.RandomList(list));
                                            }
                                            else
                                            {
                                                Item[] list = new Item[]
                                            {
                                        new DarkTapestrySouthDeed(),
                                        new DarkTapestryEastDeed(),
                                        new LightFlowerTapestrySouthDeed(),
                                        new LightFlowerTapestryEastDeed(),
                                            };
                                                chest.DropItem(Utility.RandomList(list));
                                            }
                                            break;
                                    }
                                }
                            }

                            (chest as LockableContainer).Movable = true;
                            (chest as LockableContainer).Locked = false;
                            (chest as LockableContainer).TrapType = TrapType.None;
                            (chest as LockableContainer).TrapPower = 0;
                            (chest as LockableContainer).TrapLevel = 0;

                            sos.Delete();

                            if (chest is IShipwreckedItem)
                                ((IShipwreckedItem)chest).IsShipwreckedItem = true;

                            return chest;
                        }
                    }
                }

                #endregion
            }

            return base.Construct(type, from, tool, def, map, loc, resource);
        }

        public override bool Give(Mobile m, Item item, bool placeAtFeet)
        {
            // chests at levels 4&5 are on a kraken
            if (item is TreasureMap || item is MessageInABottle || item is SpecialFishingNet)
            {
                BaseCreature serp;

                // 8/11/23, Yoar: If we spawn krakens for level 4/5 MiBs, it ruins the surprise of opening MiBs
#if false
                // mibs at levels 4&5 are on a kraken
                if ((item is MessageInABottle) && (item as MessageInABottle).Level >= 4)
                    serp = new Kraken();
                else
#endif
                {
                    if (0.25 > Utility.RandomDouble())
                        serp = new DeepSeaSerpent();
                    else
                        serp = new SeaSerpent();
                }

                int x = m.X, y = m.Y;

                Map map = m.Map;

                for (int i = 0; map != null && i < 20; ++i)
                {
                    int tx = m.X - 10 + Utility.Random(21);
                    int ty = m.Y - 10 + Utility.Random(21);

                    LandTile t = map.Tiles.GetLandTile(tx, ty);

                    if (t.Z == -5 && ((t.ID >= 0xA8 && t.ID <= 0xAB) || (t.ID >= 0x136 && t.ID <= 0x137)) && !Spells.SpellHelper.CheckMulti(new Point3D(tx, ty, -5), map))
                    {
                        x = tx;
                        y = ty;
                        break;
                    }
                }

                serp.MoveToWorld(new Point3D(x, y, -5), map);

                serp.Home = serp.Location;
                serp.RangeHome = 10;

                serp.PackItem(item);

                m.SendLocalizedMessage(503170); // Uh oh! That doesn't look like a fish!

                return true; // we don't want to give the item to the player, it's on the serpent
            }

            if (item is BigFish || item is WoodenChest || item is MetalChest || item is MetalGoldenChest)
                placeAtFeet = true;

            return base.Give(m, item, placeAtFeet);
        }

        public override void SendSuccessTo(Mobile from, Item item, HarvestResource resource)
        {
            if (item is BigFish)
            {
                from.SendLocalizedMessage(1042635); // Your fishing pole bends as you pull a big fish from the depths!

                BigFish bigFish = (BigFish)item;

                bigFish.Fisher = Utility.Intern(Server.Misc.Titles.FormatShort(from));
                bigFish.Caught = DateTime.UtcNow;
            }
            else if (item is WoodenChest || item is MetalChest || item is MetalGoldenChest)
            {
                from.SendLocalizedMessage(503175); // You pull up a heavy chest from the depths of the ocean!
            }
            else
            {
                int number;
                string name;

                if (item is BaseMagicFish)
                {
                    number = 1008124;
                    name = "a mess of small fish";
                }
                else if (item is Fish)
                {
                    number = 1008124;
                    name = "a fish";
                }
                else if (item is BaseShoes)
                {
                    number = 1008124;
                    name = item.ItemData.Name;
                }
                else if (item is TreasureMap)
                {
                    number = 1008125;
                    name = "a sodden piece of parchment";
                }
                else if (item is MessageInABottle)
                {
                    number = 1008125;
                    name = "a bottle, with a message in it";
                }
                else if (item is SpecialFishingNet)
                {
                    number = 1008125;
                    name = "a special fishing net"; // TODO: this is just a guess--what should it really be named?
                }
                else
                {
                    number = 1043297;

                    if ((item.ItemData.Flags & TileFlag.ArticleA) != 0)
                        name = "a " + item.ItemData.Name;
                    else if ((item.ItemData.Flags & TileFlag.ArticleAn) != 0)
                        name = "an " + item.ItemData.Name;
                    else
                        name = item.ItemData.Name;
                }

                if (number == 1043297)
                    from.SendLocalizedMessage(number, name);
                else
                    from.SendLocalizedMessage(number, true, name);
            }
        }

        public override void OnHarvestStarted(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            base.OnHarvestStarted(from, tool, def, toHarvest);

            int tileID;
            Map map;
            Point3D loc;

            if (GetHarvestDetails(from, tool, toHarvest, out tileID, out map, out loc))
                Timer.DelayCall(TimeSpan.FromSeconds(1.5), new TimerStateCallback(Splash_Callback), new object[] { loc, map });
        }

        // no fish bite
        public override void OnHarvestFailed(Type type, Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, HarvestResource resource)
        {
            GoodFishingAdvice(from, new Point2D(loc), false);
        }

        private void Splash_Callback(object state)
        {
            object[] args = (object[])state;
            Point3D loc = (Point3D)args[0];
            Map map = (Map)args[1];

            Effects.SendLocationEffect(loc, map, 0x352D, 16, 4);
            Effects.PlaySound(loc, map, 0x364);
        }

        public override object GetLock(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            return this;
        }

        public override bool BeginHarvesting(Mobile from, Item tool)
        {
            if (!base.BeginHarvesting(from, tool))
                return false;

            from.SendLocalizedMessage(500974); // What water do you want to fish in?
            return true;
        }

        public override bool CheckHarvest(Mobile from, Item tool)
        {
            if (!base.CheckHarvest(from, tool))
                return false;

            if (from.Mounted)
            {
                from.SendLocalizedMessage(500971); // You can't fish while riding!
                return false;
            }

            return true;
        }

        public override bool CheckHarvest(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            if (!base.CheckHarvest(from, tool, def, toHarvest))
                return false;

            if (from.Mounted)
            {
                from.SendLocalizedMessage(500971); // You can't fish while riding!
                return false;
            }

            return true;
        }

        public override void FinishHarvesting(Mobile from, Item tool, HarvestDefinition def, object toHarvest, object locked)
        {
            int tmpRangeHolder = def.MaxRange;
            def.MaxRange += 3;
            base.FinishHarvesting(from, tool, def, toHarvest, locked);
            def.MaxRange = tmpRangeHolder;
        }

        private static int[] m_WaterTiles = new int[]
            {
                0x00A8, 0x00AB,
                0x0136, 0x0137,
                0x5797, 0x579C,
                0x746E, 0x7485,
                0x7490, 0x74AB,
                0x74B5, 0x75D5
            };
    }
}