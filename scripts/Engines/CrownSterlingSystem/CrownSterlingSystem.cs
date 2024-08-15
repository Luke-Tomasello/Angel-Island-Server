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

/* Scripts\Engines\CrownSterlingSystem\CrownSterlingSystem.cs
 * CHANGELOG:
 *  6/25/2024, Adam
 *      Initial version.
 */

using Pluralize.NET;
using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Server.Engines.CrownSterlingSystem
{
    public class CrownSterlingSystem
    {
        RewardSet m_RewardSet = RewardSet.Empty;
        private Mobile m_NPC = null;
        public Mobile Vendor { get { return m_NPC; } }
        internal CrownSterlingSystem(Mobile NPC, RewardSet rewardSet)
        {
            m_NPC = NPC;
            m_RewardSet = rewardSet;
        }
        public static CrownSterlingSystem Factory(Mobile NPC, RewardSet rewardSet)
        {
            if (!RewardsDatabase.ContainsKey(rewardSet))
                return null;

            return new CrownSterlingSystem(NPC, rewardSet);

        }
        private static List<Type> OtherIgnoredTypes
        {
            get
            {
                // leviathan rares
                List<Type> list = new()
                {
                    typeof(AdmiralsHeartyRum),
                    typeof(Anchor),
                    typeof(Hook),
                    typeof(Pulley),
                    typeof(SeahorseStatuette),
                    typeof(TallCandelabra),
                    // SOS Mib
                    typeof(Candelabra),
                    typeof(WoodDebris),
                    typeof(DarkFlowerTapestrySouthDeed),
                    typeof(DarkFlowerTapestryEastDeed),
                    typeof(LightTapestrySouthDeed),
                    typeof(LightTapestryEastDeed),
                    typeof(DarkTapestrySouthDeed),
                    typeof(DarkTapestryEastDeed),
                    typeof(LightFlowerTapestrySouthDeed),
                    typeof(LightFlowerTapestryEastDeed),
                    typeof(Pullies),    // fishing only had one of these facings, I've added it, then removed it from the CSV
                    // remains (other body parts are excluded by id)
                    typeof(Skull),
                    typeof(Brain),
                };

                return list;
            }
        }
        public static void Initialize()
        {
            List<Type> MasterList = new List<Type>();
            MasterList.AddRange(FillableEntry.Registry);
            if (Craft.CraftSystem.CraftableTypes.Count == 0)
                Craft.CraftSystem.InitCraftTable();
            MasterList.AddRange(Craft.CraftSystem.CraftableTypes);
            MasterList.AddRange(OtherIgnoredTypes);
            MasterList = MasterList.Distinct().ToList();

            // add the graphic ids of things sold on vendors.
            foreach (int id in GenericBuyInfo.Registry)
                if (!IgnoredIDs.Contains(id))
                    IgnoredIDs.Add(id);

            // add the graphic ids of things on spawners.
            foreach (int id in Spawner.Registry)
                if (!IgnoredIDs.Contains(id))
                    IgnoredIDs.Add(id);

            // Mibs/SOS
            int[] Paintings_portraits = Enumerable.Range(0xE9F, 10).ToArray();
            int[] Pillows = Enumerable.Range(0x13A4, 11).ToArray();
            int[] Shells = Enumerable.Range(0xFC4, 9).ToArray();


            // now, add all the stuff from the master list to our ignore list
            foreach (var t in MasterList)
            {
                if (Utility.HasParameterlessConstructor(t))
                {
                    object o = Activator.CreateInstance(t);
                    if (o is Item item)
                    {
                        if (!IgnoredIDs.Contains(item.ItemID))
                            IgnoredIDs.Add(item.ItemID);

                        // get any flippable IDs
                        FlipableAttribute[] attributes = (FlipableAttribute[])item.GetType().GetCustomAttributes(typeof(FlipableAttribute), false);
                        foreach (var fa in attributes)
                        {
                            if (fa.ItemIDs != null)
                            {
                                foreach (int id in fa.ItemIDs)
                                    if (!IgnoredIDs.Contains(id))
                                        IgnoredIDs.Add(id);
                            }
                            else
                            {
                                try
                                {
                                    MethodInfo flipMethod = item.GetType().GetMethod("Flip", Type.EmptyTypes);
                                    if (flipMethod != null)
                                    {
                                        flipMethod.Invoke(item, new object[0]);
                                        if (!IgnoredIDs.Contains(item.ItemID))
                                            IgnoredIDs.Add(item.ItemID);
                                    }
                                }
                                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                            }
                        }
                        item.Delete();
                    }
                    else if (o is Mobile mobile)
                        mobile.Delete();
                    else
                        ;
                }
            }

            // remove duplicates
            IgnoredIDs = IgnoredIDs.Distinct().ToList();

            // update ignore table to contain all the custom flips
            List<int> flips = new List<int>();
            foreach (int id in IgnoredIDs)
                if (Loot.FlipTable.ContainsKey(id))
                {   // add all flip variants of this id
                    int[] sisters = Loot.FlipTable[id];
                    flips.AddRange(sisters);
                }

            foreach (int id in flips)
                if (!IgnoredIDs.Contains(id))
                    IgnoredIDs.Add(id);

            IgnoredIDs.Sort();
        }
        private static List<int> IgnoredIDs = new()
        {   // See Initialize for further additions

            // remains (other body parts are excluded by type)
            0x1CED, //"a heart";
            0x1CEE, // "a liver";
            0x1CEF, // "entrails";

            // fishing harvest
            0x1CDD, 0x1CE5, // arm
            0x1CE0, 0x1CE8, // torso
			0x1CE1, 0x1CE9, // head
			0x1CE2, 0x1CEC, // leg
            0x1AE0, 0x1AE1, 0x1AE2, 0x1AE3, 0x1AE4, // skulls
			0x1B09, 0x1B0A, 0x1B0B, 0x1B0C, 0x1B0D, 0x1B0E, 0x1B0F, 0x1B10, // bone piles
			0x1B15, 0x1B16, // pelvis bones
            0x1EB5,         // unfinished barrel
			0xA2A,          // stool
			0xC1F,          // broken clock
			0x1047, 0x1048, // globe
			0x1EB1, 0x1EB2, 0x1EB3, 0x1EB4, // barrel staves                     
            0x0DBB,         // 3515 (0x0DBB) Seaweed
            0x054D,         // 5453 (0x054D) water barrel – empty or filled we don't have them on ai yet.
            0x0DC9,         // 3529 (0x0DC9) fishing net – unhued and oddly shaped 
            0x1E9A,         // 7834 (0x1E9A) hook
			0x1E9D,         // 7837 (0x1E9D) pulleys
            0x1E9C,         // 7836 (0x1E9C) pulleys
            0x1E9E,         // 7838 (0x1E9E) Pulley
            0x1EA0,         // 7840 (0x1EA0) Rope
            0x1EA9,         // 7849 (0x1EA9) Winch – south
            0x1EAC,         // 7852 (0x1EAC) Winch – east
            0x0FCD,         // 4045 (0x0FCD) string of shells – south
            0x0FCE,         // 4046 (0x0FCE) string of shells – south
            0x0FCF,         // 4047 (0x0FCF) – string of shells – south
            0x0FD0,         // 4048 (0x0FD0) – string of shells – south
            0x0FD1,         // 4049 (0x0FD1) – string of shells – east
            0x0FD2,         // 4050 (0x0FD2) – string of shells – east
            0x0FD3,         // 4051 (0x0FD3) – string of shells – east
            0x0FD4,         // 4052 (0x0FD4) – string of shells – east
            0x1003,         // 4099 (0x1003) – Spittoon
            0x0FFB,         // 4091 (0x0FFB) – Skull mug 1
            0x0FFC,         // 4092 (0x0FFC) – Skull mug 2
            0x0E74,         // 3700 (0x0E74) Cannon Balls
            0x0C2C,         // 3116 (0x0C2C) Ruined Painting
            0x0C18,         // 3096 (0x0C18) Covered chair - (server birth on osi)
            0x1EA3,         // 7843 (0x1EA3) net – south
            0x1EA4,         // 7844 (0x1EA4) net – east
            0x1EA5,         // 7845 (0x1EA5) net – south
            0x1EA6,         // 7846 (0x1EA6) net – east

            // angry miner camps
            0x1BEB, // GoldBricks

            // deep sea serpent skinnables
            0x1E15, 0x1E16, // RawFish
            0x1E17, 0x1E18, // RawFishHeadless
            0x1E19, 0x1E1A, // FishHead
            0x1E1B,         // FishHeads
            0x1E1C, 0x1E1D, // CookedFish
            
            // bone piles dropped from zombies
            0x1B09,
            0x1B0A,
            0x1B0B,
            0x1B0C,
            0x1B0D,
            0x1B0E,
            0x1B0F,
            0x1B10,

            0x1DA1, // left arm
            0x1DA2, // right arm
            0x1D9F, // torso
            0xf7e,  // bone
            0x1B17, // rib cage
            0x1B18, // rib cage
             //  vines (on WhippingVines)
            0xCEB+0,
            0xCEB+1,
            0xCEB+2,
            0xCEB+3,
            0xCEB+4,
            0xCEB+5,
            0xCEB+6,
            0xCEB+7,
            // MIB stuff
            0x0DBB, // 3515 (0x0DBB) Seaweed
            0x0C2E, // 3118 (0x0C2E) debris
            0x054D, // 5453 (0x054D) water barrel – empty or filled we don't have them on ai yet.
            0x0DC9, // 3529 (0x0DC9) fishing net – unhued and oddly shaped 
            0x1E9A, // 7834 (0x1E9A) hook
            0x1E9D, // 7837 (0x1E9D) pulleys
            0x1E9E, // 7838 (0x1E9E) Pulley
            0x1EA0, // 7840 (0x1EA0) Rope
            0x1EA9, // 7849 (0x1EA9) Winch – south
            0x1EAC, // 7852 (0x1EAC) Winch – east
            0x0FCD, // 4045 (0x0FCD) string of shells – south
            0x0FCE, // 4046 (0x0FCE) string of shells – south
            0x0FCF, // 4047 (0x0FCF) – string of shells – south
            0x0FD0, // 4048 (0x0FD0) – string of shells – south
            0x0FD1, // 4049 (0x0FD1) – string of shells – east
            0x0FD2, // 4050 (0x0FD2) – string of shells – east
            0x0FD3, // 4051 (0x0FD3) – string of shells – east
            0x0FD4, // 4052 (0x0FD4) – string of shells – east
            0x1003, // 4099 (0x1003) – Spittoon
            0x0FFB, // 4091 (0x0FFB) – Skull mug 1
            0x0FFC, // 4092 (0x0FFC) – Skull mug 2
            0x0E74, // 3700 (0x0E74) Cannon Balls
            0x0C2C, // 3116 (0x0C2C) Ruined Painting
            0x0C18, // 3096 (0x0C18) Covered chair - (server birth on osi)
            0x1EA3, // 7843 (0x1EA3) net – south
            0x1EA4, // 7844 (0x1EA4) net – east
            0x1EA5, // 7845 (0x1EA5) net – south
            0x1EA6, // 7846 (0x1EA6) net – east
            0x0DBB, // 3515 (0x0DBB) Seaweed
            0x0C2E, // 3118 (0x0C2E) debris
            0x054D, // 5453 (0x054D) water barrel – empty or filled we don't have them on ai yet.
            0x0DC9, // 3529 (0x0DC9) fishing net – unhued and oddly shaped 
            0x1E9A, // 7834 (0x1E9A) hook
            0x1E9D, // 7837 (0x1E9D) pulleys
            0x1E9E, // 7838 (0x1E9E) Pulley
            0x1EA0, // 7840 (0x1EA0) Rope
            0x1EA9, // 7849 (0x1EA9) Winch – south
            0x1EAC, // 7852 (0x1EAC) Winch – east
            0x0FCD, // 4045 (0x0FCD) string of shells – south
            0x0FCE, // 4046 (0x0FCE) string of shells – south
            0x0FCF, // 4047 (0x0FCF) – string of shells – south
            0x0FD0, // 4048 (0x0FD0) – string of shells – south
            0x0FD1, // 4049 (0x0FD1) – string of shells – east
            0x0FD2, // 4050 (0x0FD2) – string of shells – east
            0x0FD3, // 4051 (0x0FD3) – string of shells – east
            0x0FD4, // 4052 (0x0FD4) – string of shells – east
            0x1003, // 4099 (0x1003) – Spittoon
            0x0FFB, // 4091 (0x0FFB) – Skull mug 1
            0x0FFC, // 4092 (0x0FFC) – Skull mug 2
            0x0E74, // 3700 (0x0E74) Cannon Balls
            0x0C2C, // 3116 (0x0C2C) Ruined Painting
            0x0C18, // 3096 (0x0C18) Covered chair - (server birth on osi)
            0x1EA3, // 7843 (0x1EA3) net – south
            0x1EA4, // 7844 (0x1EA4) net – east
            0x1EA5, // 7845 (0x1EA5) net – south
            0x1EA6, // 7846 (0x1EA6) net – east
        };
        private int GetRewardItemIDs(List<CrownSterlingReward> table, List<int> matching)
        {
            for (int ix = 0; ix < table.Count; ix++)
                if (table[ix].ItemO is Item item)
                {
                    // just in case there are no flips
                    if (!matching.Contains(item.ItemID))
                        matching.Add(item.ItemID);

                    // first get intrinsic flip ids
                    FlipableAttribute[] attributes = (FlipableAttribute[])item.GetType().GetCustomAttributes(typeof(FlipableAttribute), false);
                    foreach (var fa in attributes)
                    {
                        if (fa.ItemIDs != null)
                        {
                            foreach (int id in fa.ItemIDs)
                                if (!matching.Contains(id))
                                    matching.Add(id);
                        }
                        else
                        {
                            try
                            {
                                MethodInfo flipMethod = item.GetType().GetMethod("Flip", Type.EmptyTypes);
                                if (flipMethod != null)
                                {
                                    flipMethod.Invoke(item, new object[0]);
                                    if (!matching.Contains(item.ItemID))
                                        matching.Add(item.ItemID);
                                }
                            }
                            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                        }
                    }

                    // update ignore table to contain all the custom flips
                    if (Loot.FlipTable.ContainsKey(item.ItemID))
                    {   // add all flip variants of this id
                        int[] sisters = Loot.FlipTable[item.ItemID];
                        matching.AddRange(sisters);
                    }
                }

            matching = matching.Distinct().ToList();
            return matching.Count;
        }
        public CrownSterlingReward[] GetCrownRewardOptions(List<CrownSterlingReward> addList, List<CrownSterlingReward> removeList)
        {
            // get the built-in list
            ReadOnlyCollection<CrownSterlingReward> temp = RewardsDatabase[m_RewardSet];

            // make a modifiable copy
            List<CrownSterlingReward> table = new List<CrownSterlingReward>(temp);

            // remove internal stuff - these are the objects like "board%s" which usually indicate they are the same graphic as the items in-game.
            for (int i = table.Count - 1; i > -1; i--)
                if (table[i].Label.Contains("%"))
                    table.RemoveAt(i);

            // we sometimes have both graphic ids, and items that share the same item ids. Keep the items, remove the simple graphic ids.
            List<int> graphicIds = new List<int>();
            GetRewardItemIDs(table, graphicIds);
            for (int i = table.Count - 1; i > -1; i--)
                if (table[i].ItemO is int id && graphicIds.Contains(id))
                    table.RemoveAt(i);

            // remove duplicates
            table = table.Distinct().ToList();

            // process ignore list
            for (int i = table.Count - 1; i > -1; i--)
                if (table[i].ItemO is Item item)
                {   // probably not safe. GMs can add variations of items that are otherwise ignored
                    ;//6420 (0x1914) Towel
                    if (IgnoredIDs.Contains(item.ItemID))
                        table.RemoveAt(i);
                }
                else if (table[i].ItemO is int id)
                {
                    if (IgnoredIDs.Contains((int)table[i].ItemO))
                        table.RemoveAt(i);
                }

            // add stuff
            foreach (CrownSterlingReward a in addList)
                if (!table.Contains(a))
                    table.Add(a);

            // remove stuff
            foreach (CrownSterlingReward r in removeList)
                if (table.Contains(r))
                    table.Remove(r);

            table.Sort((e1, e2) =>
            {
                return e1.Cost.CompareTo(e2.Cost);
            });
            return table.ToArray();
        }
        public enum RewardSet
        {
            Empty,
            Test,
            Ghoulish,
            Furniture,
            HomeDeco,
            Gravestones,
        }

        public static bool ForSale(int itemId)
        {   // when dropping rares in Loot.cs, we skip those rares that are being sold here
            foreach (var table in RewardsDatabase)
                foreach (var record in table.Value)
                    if (record.ItemO is int id && id == itemId)
                        // we have this item in our database, now see if there are any vendors selling this
                        foreach (var vendor in CrownSterlingVendor.Instances)
                            if (vendor.RewardSet == table.Key)
                                return true;
            return false;
        }
        public static readonly Dictionary<RewardSet, ReadOnlyCollection<CrownSterlingReward>> RewardsDatabase = new()
        {
            {RewardSet.Empty, new ReadOnlyCollection <CrownSterlingReward>(new List<CrownSterlingReward>())},

            {RewardSet.Test, new ReadOnlyCollection <CrownSterlingReward>(
                new List <CrownSterlingReward>()
                {
                    new CrownSterlingReward(itemO: 4171,baseCost: 5000,  0,label: null),    // clock
                    new CrownSterlingReward(itemO: 3834, baseCost: 5000, 0,label: null),    // spell book
                    new CrownSterlingReward(itemO: 3702, baseCost: 5000, 0,label: null),    // a bag
                    new CrownSterlingReward(itemO: 0x1BD9, baseCost: 5000, 0,label: null),  // Stack of boards (south)
                    new CrownSterlingReward(itemO: 0x1BDC, baseCost: 5000, 0,label: null),  // Stack of boards (east)
                })},

            {RewardSet.Furniture, new ReadOnlyCollection <CrownSterlingReward>(
                new List <CrownSterlingReward>()
                {
                    // cabinets and chests
                    new CrownSterlingReward(itemO: new ShortCabinet(), baseCost: 10000, 0,label: null),         // 
                    new CrownSterlingReward(itemO: new TallCabinet(), baseCost: 16000, 0,label: null),          // 
                    new CrownSterlingReward(itemO: new WoodenFootLocker(), baseCost: 18000, 0,label: null),     // 
                    new CrownSterlingReward(itemO: new PlainWoodenChest(), baseCost: 20000, 0,label: null),     // 
                    new CrownSterlingReward(itemO: new FinishedWoodenChest(), baseCost: 22000, 0,label: null),  // 
                    new CrownSterlingReward(itemO: new OrnateWoodenChest(), baseCost: 23000, 0,label: null),    // 
                    new CrownSterlingReward(itemO: new GildedWoodenChest(), baseCost: 25000, 0,label: null),    // 

                    // tables
                    new CrownSterlingReward(itemO: new PlainLowTable(), baseCost: 7200, 0,label: null),         // 
                    new CrownSterlingReward(itemO: new ElegantLowTable(), baseCost: 7400, 0,label: null),       // 

                    // Ruined stuff
                    new CrownSterlingReward(itemO: new RuinedFallenChairA(), baseCost: 10000, 0,label: null),
                    new CrownSterlingReward(itemO: new RuinedArmoire(), baseCost: 10000, 0,label: null),

                    new CrownSterlingReward(itemO: new CoveredChair(), baseCost: 10000, 0,label: null),
                    new CrownSterlingReward(itemO: new RuinedFallenChairB(), baseCost: 10000, 0,label: null),
                    new CrownSterlingReward(itemO: new RuinedChair(), baseCost: 10000, 0,label: null),
                    new CrownSterlingReward(itemO: new RuinedClock(), baseCost: 10000, 0,label: null),
                    new CrownSterlingReward(itemO: new RuinedDrawers(), baseCost: 10000, 0,label: null),
                    new CrownSterlingReward(itemO: new RuinedPainting(), baseCost: 10000, 0,label: null),       
                    
                    // bone furniture (addons)
                    new CrownSterlingReward(itemO: new AddonFactory(0x2A58, typeof(BoneThroneDeed)),baseCost: 12000,  0,label: null),   // bone throne
                    new CrownSterlingReward(itemO: new AddonFactory(0x2A5C, typeof(BoneTableDeed)),baseCost: 12000,  0,label: null),    // bone table
                    // multi component - need to handle display
                    //new CrownSterlingReward(itemO: new AddonFactory(0x2A5C, typeof(BoneCouchDeed)),baseCost: 12000,  0,label: null),    // bone couch

                    new CrownSterlingReward(itemO: 0x0C10 + 0,baseCost: 5000,  0,label: null),      // Furniture
                    new CrownSterlingReward(itemO: 0x0C10 + 1,baseCost: 5000,  0,label: null),      // 
                    new CrownSterlingReward(itemO: 0x0C10 + 2,baseCost: 5000,  0,label: null),      // 
                    new CrownSterlingReward(itemO: 0x0C10 + 3,baseCost: 5000,  0,label: null),      // 
                    new CrownSterlingReward(itemO: 0x0C10 + 7,baseCost: 5000,  0,label: null),      // 
                    new CrownSterlingReward(itemO: 0x0C10 + 8,baseCost: 5000,  0,label: null),      // 
                    new CrownSterlingReward(itemO: 0x0C10 + 9,baseCost: 5000,  0,label: null),      // 
                    new CrownSterlingReward(itemO: 0x0C10 + 10,baseCost: 5000,  0,label: null),     // 
                    new CrownSterlingReward(itemO: 0x0C10 + 11,baseCost: 5000,  0,label: null),     // 
                    new CrownSterlingReward(itemO: 0x0C10 + 12,baseCost: 5000,  0,label: null),     // 
                    new CrownSterlingReward(itemO: 0x0C10 + 13,baseCost: 5000,  0,label: null),     // 
                    new CrownSterlingReward(itemO: 0x0C10 + 14,baseCost: 5000,  0,label: null),     // 
                    new CrownSterlingReward(itemO: 0x0C10 + 15,baseCost: 5000,  0,label: null),     // 

                    new CrownSterlingReward(itemO: 0x0C2C + 0,baseCost: 5000,  0,label: null),      // ruined painting
                    //new CrownSterlingReward(itemO: 0x0C2C + 1,baseCost: 5000,  0,label: null),    // debris
                    //new CrownSterlingReward(itemO: 0x0C2C + 2,baseCost: 5000,  0,label: null),    // debris
                    //new CrownSterlingReward(itemO: 0x0C2C + 3,baseCost: 5000,  0,label: null),    // debris
                    //new CrownSterlingReward(itemO: 0x0C2C + 4,baseCost: 5000,  0,label: null),    // debris

                    new CrownSterlingReward(itemO: new AddonFactory(0x0C39,typeof(PegBoardDeed)), baseCost: 5000, 0,label: null),   // peg board
                })},

            {RewardSet.Gravestones, new ReadOnlyCollection <CrownSterlingReward>(
                new List <CrownSterlingReward>()
                {
                    new CrownSterlingReward(itemO: 0x0ED4 + 0,baseCost: 5000,  0,label: null),  // gravestones
                    new CrownSterlingReward(itemO: 0x0ED4 + 1,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x0ED4 + 2,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x0ED4 + 3,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x0ED4 + 4,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x0ED4 + 5,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x0ED4 + 6,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x0ED4 + 7,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x0ED4 + 8,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x0ED4 + 9,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x0ED4 + 10,baseCost: 5000,  0,label: null),

                    new CrownSterlingReward(itemO: 0x1165 + 0,baseCost: 5000,  0,label: null),  // gravestones
                    new CrownSterlingReward(itemO: 0x1165 + 1,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 2,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 3,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 4,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 5,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 6,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 7,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 8,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 9,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 10,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 11,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 12,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 13,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 14,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 15,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 16,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 17,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 18,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 19,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 20,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 21,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 22,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 23,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 24,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 25,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 26,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 27,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 28,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 29,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 30,baseCost: 5000,  0,label: null),
                    new CrownSterlingReward(itemO: 0x1165 + 31,baseCost: 5000,  0,label: null),
                })},

            {RewardSet.HomeDeco, new ReadOnlyCollection <CrownSterlingReward>(
                new List <CrownSterlingReward>()
                {
                    #region all rares from rares.cs
                    //new CrownSterlingReward(itemO: new Rope(), baseCost: 12500, 0,label: null), // creature drop
                    new CrownSterlingReward(itemO: new IronWire(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new SilverWire(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new GoldWire(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new CopperWire(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new Whip(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new PaintsAndBrush(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new PenAndInk(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new ChiselsNorth(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new ChiselsWest(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new BloodVial(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new DriedFlowers1(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new DriedFlowers2(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new WhiteDriedFlowers(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new GreenDriedFlowers(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new DriedOnions(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new DriedHerbs(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new RedCheckers(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new WhiteCheckers(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new BlackChessmen(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new WhiteChessmen(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new HorseShoes(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new EmptyJar(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new EmptyJars2(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new EmptyJars3(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new EmptyJars4(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new HalfEmptyJar(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new HalfEmptyJars2(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new HalfEmptyJars3(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new HalfEmptyJars4(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new FullJar(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new TwoFullJars(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new ThreeFullJars(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new FourFullJars(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new ForgedMetal(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new Toolkit(), baseCost: 12500, 0,label: null),
                    
                    // township gardening
                    //new CrownSterlingReward(itemO: new Rocks(), baseCost: 12500, 0,label: null),
                    //new CrownSterlingReward(itemO: new Rock(), baseCost: 12500, 0,label: null), 

                    // drop in themed treasure chests
                    //new CrownSterlingReward(itemO: new OphidianBardiche(), baseCost: 12500, 0,label: null), // ophidian bardiche - 9563 (0x255B)
                    //new CrownSterlingReward(itemO: new OgresClub(), baseCost: 12500, 0,label: null), // ogre's club - 9561 (0x2559)
                    //new CrownSterlingReward(itemO: new LizardmansStaff(), baseCost: 12500, 0,label: null), // lizardman's staff - 9560 (0x2558)
                    //new CrownSterlingReward(itemO: new EttinHammer(), baseCost: 12500, 0,label: null), // ettin hammer - 9557 (0x2555)
                    //new CrownSterlingReward(itemO: new LizardmansMace(), baseCost: 12500, 0,label: null), // lizardman's mace - 9559 (0x2557)
                    //new CrownSterlingReward(itemO: new SkeletonScimitar(), baseCost: 12500, 0,label: null), // skeleton scimitar - 9568 (0x2560)
                    //new CrownSterlingReward(itemO: new SkeletonAxe(), baseCost: 12500, 0,label: null), // skeleton axe - 9567 (0x255F)
                    //new CrownSterlingReward(itemO: new RatmanSword(), baseCost: 12500, 0,label: null), // ratman sword - 9566 (0x255E)
                    //new CrownSterlingReward(itemO: new RatmanAxe(), baseCost: 12500, 0,label: null), // ratman axe - 9565 (0x255D)
                    //new CrownSterlingReward(itemO: new OrcClub(), baseCost: 12500, 0,label: null), // orc club - 9564 (0x255C)
                    //new CrownSterlingReward(itemO: new TerathanStaff(), baseCost: 12500, 0,label: null), // terathan staff - 9569 (0x2561)
                    //new CrownSterlingReward(itemO: new TerathanSpear(), baseCost: 12500, 0,label: null), // terathan spear - 9570 (0x2562)
                    //new CrownSterlingReward(itemO: new TerathanMace(), baseCost: 12500, 0,label: null), // terathan mace - 9571 (0x2563)
                    //new CrownSterlingReward(itemO: new TrollAxe(), baseCost: 12500, 0,label: null), // troll axe - 9572 (0x2564)
                    //new CrownSterlingReward(itemO: new TrollMaul(), baseCost: 12500, 0,label: null), // troll maul - 9573 (0x2565)
                    //new CrownSterlingReward(itemO: new BoneMageStaff(), baseCost: 12500, 0,label: null), // bone mage staff - 9577 (0x2569)
                    //new CrownSterlingReward(itemO: new OrcMageStaff(), baseCost: 12500, 0,label: null), // orc mage staff - 9576 (0x2568)
                    //new CrownSterlingReward(itemO: new OrcLordBattleaxe(), baseCost: 12500, 0,label: null), // orc lord battleaxe - 9575 (0x2567)
                    //new CrownSterlingReward(itemO: new FrostTrollClub(), baseCost: 12500, 0,label: null),// frost troll club - 9574 (0x2566) 

                    new CrownSterlingReward(itemO: new DirtyPan(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new DirtySmallRoundPot(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new DirtyPot(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new DirtyRoundPot(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new DirtyFrypan(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new DirtySmallPot(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new DirtyKettle(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new CandelabraOfSouls(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new GhostShipAnchor(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new GoldBricks(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new SeahorseStatuette(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new ShipModelOfTheHMSCape(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new AdmiralsHeartyRum(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new SmallFish(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new ArrowShafts(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new RawFish(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new RawFishHeadless(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new FishHead(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new FishHeads(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new CookedFish(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new Anchor(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new Hook(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new Pulley(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new Pullies(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new TallCandelabra(), baseCost: 12500, 0,label: null),
                    new CrownSterlingReward(itemO: new Hay(), baseCost: 12500, 0,label: null),
                    #endregion all rares from rares.cs

                    #region dried flowers & herbs // 0xC3D
                    new CrownSterlingReward(itemO: 0x0C3B + 0,baseCost: 5000,  0,label: null),     // dried flowers & herbs
                    new CrownSterlingReward(itemO: 0x0C3B + 1,baseCost: 5000,  0,label: null),     // dried flowers & herbs
                    new CrownSterlingReward(itemO: 0x0C3B + 2,baseCost: 5000,  0,label: null),     // dried flowers & herbs
                    new CrownSterlingReward(itemO: 0x0C3B + 3,baseCost: 5000,  0,label: null),     // dried flowers & herbs
                    new CrownSterlingReward(itemO: 0x0C3B + 4,baseCost: 5000,  0,label: null),     // dried flowers & herbs
                    new CrownSterlingReward(itemO: 0x0C3B + 5,baseCost: 5000,  0,label: null),     // dried flowers & herbs
                    new CrownSterlingReward(itemO: 0x0C3B + 6,baseCost: 5000,  0,label: null),     // dried flowers & herbs
                    new CrownSterlingReward(itemO: 0x0C3B + 7,baseCost: 5000,  0,label: null),     // dried flowers & herbs
                    #endregion dried flowers & herbs

                    #region 5000
                    #region AddRare Command
                    new CrownSterlingReward(itemO: 2527, baseCost: 5000, 0,label: null),  // a dirty pot
                    new CrownSterlingReward(itemO: 2478, baseCost: 5000, 0,label: null),  // a dirty plate
                    new CrownSterlingReward(itemO: 2534, baseCost: 5000, 0,label: null),  // a dirty pot
                    new CrownSterlingReward(itemO: 2536, baseCost: 5000, 0,label: null),  // dirty pan
                    new CrownSterlingReward(itemO: 3094, baseCost: 5000, 0,label: null),  // damaged books
                    new CrownSterlingReward(itemO: 2518, baseCost: 5000, 0,label: null),  // a pitcher
                    new CrownSterlingReward(itemO: 2449, baseCost: 5000, 0,label: null),  // a tray
                    new CrownSterlingReward(itemO: 2530, baseCost: 5000, 0,label: null),  // a frypan
                    new CrownSterlingReward(itemO: 2541, baseCost: 5000, 0,label: null),  // a kettle
                    new CrownSterlingReward(itemO: 7713, baseCost: 5000, 0,label: null),  // books
                    new CrownSterlingReward(itemO: 2671, baseCost: 5000, 0,label: null),  // a folded blanket
                    new CrownSterlingReward(itemO: 0x9BF, baseCost: 2000, 0,label: null),  // a goblet
                    new CrownSterlingReward(itemO: 7716, baseCost: 5000, 0,label: null),  // books
                    new CrownSterlingReward(itemO: 7717, baseCost: 5000, 0,label: null),  // books
                    new CrownSterlingReward(itemO: 7714, baseCost: 5000, 0,label: null),  // books
                    new CrownSterlingReward(itemO: 7715, baseCost: 5000, 0,label: null),  // books
                    new CrownSterlingReward(itemO: 7712, baseCost: 5000, 0,label: null),  // a book
                    new CrownSterlingReward(itemO: 3118, baseCost: 5000, 0,label: null),  // debris
                    new CrownSterlingReward(itemO: 3119, baseCost: 5000, 0,label: null),  // debris
                    new CrownSterlingReward(itemO: 4167, baseCost: 5000, 0,label: null),  // a globe
                    new CrownSterlingReward(itemO: 4029, baseCost: 5000, 0,label: null),  // a book
                    new CrownSterlingReward(itemO: 6173, baseCost: 5000, 0,label: null),  // a alchemical symbol
                    new CrownSterlingReward(itemO: 6182, baseCost: 5000, 0,label: null),  // a alchemical symbol
                    new CrownSterlingReward(itemO: 6181, baseCost: 5000, 0,label: null),  // a alchemical symbol
                    new CrownSterlingReward(itemO: 6179, baseCost: 5000, 0,label: null),  // a alchemical symbol
                    new CrownSterlingReward(itemO: 6180, baseCost: 5000, 0,label: null),  // a alchemical symbol
                    new CrownSterlingReward(itemO: 6178, baseCost: 5000, 0,label: null),  // a alchemical symbol
                    new CrownSterlingReward(itemO: 6176, baseCost: 5000, 0,label: null),  // a alchemical symbol
                    new CrownSterlingReward(itemO: 6175, baseCost: 5000, 0,label: null),  // a alchemical symbol
                    new CrownSterlingReward(itemO: 6183, baseCost: 5000, 0,label: null),  // a alchemical symbol
                    new CrownSterlingReward(itemO: 6174, baseCost: 5000, 0,label: null),  // a alchemical symbol
                    new CrownSterlingReward(itemO: 6184, baseCost: 5000, 0,label: null),  // a alchemical symbol
                    new CrownSterlingReward(itemO: 6177, baseCost: 5000, 0,label: null),  // a alchemical symbol
                    #endregion AddRare Command

                    new CrownSterlingReward(itemO: new WoodDebris(), baseCost: 5000, 0,label: null),
                    new CrownSterlingReward(itemO: new RuinedBookcase(), baseCost: 5000, 0,label: null),
                    new CrownSterlingReward(itemO: new RuinedBooks(), baseCost: 5000, 0,label: null),
                    
                    // from UOFiddler
                    new CrownSterlingReward(itemO: 0x1BD1 + 0, baseCost: 5000, 0,label: null), // feathers
                    new CrownSterlingReward(itemO: 0x1BD1 + 1, baseCost: 5000, 0,label: null), // feathers
                    new CrownSterlingReward(itemO: 0x1BD1 + 2, baseCost: 5000, 0,label: null), // feathers
                    new CrownSterlingReward(itemO: 0x1BD1 + 3, baseCost: 5000, 0,label: null), // shafts
                    new CrownSterlingReward(itemO: 0x1BD1 + 4, baseCost: 5000, 0,label: null), // shafts
                    new CrownSterlingReward(itemO: 0x1BD1 + 5, baseCost: 5000, 0,label: null), // shafts
                    new CrownSterlingReward(itemO: 0x1BD1 + 6, baseCost: 5000, 0,label: null), // boards
                    new CrownSterlingReward(itemO: 0x1BD1 + 7, baseCost: 5000, 0,label: null), // boards
                    new CrownSterlingReward(itemO: 0x1BD1 + 8, baseCost: 5000, 0,label: null), // boards
                    new CrownSterlingReward(itemO: 0x1BD1 + 9, baseCost: 5000, 0,label: null), // boards
                    new CrownSterlingReward(itemO: 0x1BD1 + 10, baseCost: 5000, 0,label: null), // boards
                    new CrownSterlingReward(itemO: 0x1BD1 + 11, baseCost: 5000, 0,label: null), // boards
                    new CrownSterlingReward(itemO: 0x1BD1 + 12, baseCost: 5000, 0,label: null), // logs
                    new CrownSterlingReward(itemO: 0x1BD1 + 13, baseCost: 5000, 0,label: null), // logs
                    new CrownSterlingReward(itemO: 0x1BD1 + 14, baseCost: 5000, 0,label: null), // logs
                    new CrownSterlingReward(itemO: 0x1BD1 + 15, baseCost: 5000, 0,label: null), // logs
                    new CrownSterlingReward(itemO: 0x1BD1 + 16, baseCost: 5000, 0,label: null), // logs
                    new CrownSterlingReward(itemO: 0x1BD1 + 17, baseCost: 5000, 0,label: null), // logs

                    new CrownSterlingReward(itemO: 0x1BFB + 0, baseCost: 5000, 0,label: null), // crossbow bolt
                    new CrownSterlingReward(itemO: 0x1BFB + 1, baseCost: 5000, 0,label: null), // crossbow bolt
                    new CrownSterlingReward(itemO: 0x1BFB + 2, baseCost: 5000, 0,label: null), // crossbow bolt
                    new CrownSterlingReward(itemO: 0x1BFB + 3, baseCost: 2000, 0,label: null), // crossbow bolt (single)

                    new CrownSterlingReward(itemO: 0x0F3F + 0, baseCost: 5000, 0,label: null), // arrow
                    new CrownSterlingReward(itemO: 0x0F3F + 1, baseCost: 5000, 0,label: null), // arrow
                    new CrownSterlingReward(itemO: 0x0F3F + 2, baseCost: 5000, 0,label: null), // arrow
                    new CrownSterlingReward(itemO: 0x0F3F + 3, baseCost: 2000, 0,label: null), // arrow (single)
                    
                    new CrownSterlingReward(itemO: 0x0CEB + 0, baseCost: 5000, 0,label: null),  // vines
                    new CrownSterlingReward(itemO: 0x0CEB + 1, baseCost: 5000, 0,label: null),  // vines
                    new CrownSterlingReward(itemO: 0x0CEB + 2, baseCost: 5000, 0,label: null),  // vines
                    new CrownSterlingReward(itemO: 0x0CEB + 3, baseCost: 5000, 0,label: null),  // vines
                    new CrownSterlingReward(itemO: 0x0CEB + 4, baseCost: 5000, 0,label: null),  // vines
                    new CrownSterlingReward(itemO: 0x0CEB + 5, baseCost: 5000, 0,label: null),  // vines
                    new CrownSterlingReward(itemO: 0x0CEB + 6, baseCost: 5000, 0,label: null),  // vines
                    new CrownSterlingReward(itemO: 0x0CEB + 7, baseCost: 5000, 0,label: null),  // vines

                    new CrownSterlingReward(itemO: 0x099B + 0, baseCost: 5000, 0,label: null),  // bottle of liquor
                    new CrownSterlingReward(itemO: 0x099B + 1, baseCost: 5000, 0,label: null),  // bottle of liquor
                    new CrownSterlingReward(itemO: 0x099B + 2, baseCost: 5000, 0,label: null),  // bottle of liquor
                    new CrownSterlingReward(itemO: 0x099B + 3, baseCost: 5000, 0,label: null),  // bottle of liquor
                    new CrownSterlingReward(itemO: 0x099B + 4, baseCost: 5000, 0,label: null),  // bottle of liquor
                    new CrownSterlingReward(itemO: 0x099B + 5, baseCost: 5000, 0,label: null),  // bottle of liquor
                    new CrownSterlingReward(itemO: 0x099B + 6, baseCost: 5000, 0,label: null),  // bottle of liquor
                    new CrownSterlingReward(itemO: 0x099B + 7, baseCost: 5000, 0,label: null),  // bottle of liquor

                    new CrownSterlingReward(itemO: 0x0FAC, baseCost: 5000, 0,label: null),  // fire pit

                    new CrownSterlingReward(itemO: 0x10F2, baseCost: 5000, 0,label: null),  // garbage piles
                    new CrownSterlingReward(itemO: 0x10F3, baseCost: 5000, 0,label: null),  // garbage piles
                    #endregion 5000

                    #region 6000
                    new CrownSterlingReward(itemO: 0xEE3, baseCost: 6000, 0,label: null),  // spider webs
                    new CrownSterlingReward(itemO: 0xEE4, baseCost: 6000, 0,label: null),  // spider webs
                    new CrownSterlingReward(itemO: 0xEE5, baseCost: 6000, 0,label: null),  // spider webs
                    new CrownSterlingReward(itemO: 0xEE6, baseCost: 6000, 0,label: null),  // spider webs
                    #endregion 6000

                    #region 6500
                    new CrownSterlingReward(itemO: 0x2CF9 + 0, baseCost: 6500, 0,label: null),  // decorative vines
                    new CrownSterlingReward(itemO: 0x2CF9 + 1, baseCost: 6500, 0,label: null),  // decorative vines
                    new CrownSterlingReward(itemO: 0x2CF9 + 2, baseCost: 6500, 0,label: null),  // decorative vines
                    new CrownSterlingReward(itemO: 0x2CF9 + 3, baseCost: 6500, 0,label: null),  // decorative vines

                    new CrownSterlingReward(itemO: new AddonFactory(0x14E7, typeof(HitchingPostDeed)), baseCost: 6500, 0,label: null),  // a hitching post
                    #endregion 6500

                    #region 10000
                    new CrownSterlingReward(itemO: new AddonFactory(0x0420, typeof(GruesomeStandard1Deed)), baseCost: 10000, 0,label: null),  // gruesome standard
                    new CrownSterlingReward(itemO: new AddonFactory(0x041F, typeof(GruesomeStandard1Deed)), baseCost: 10000, 0,label: null),  // gruesome standard

                    new CrownSterlingReward(itemO: 0xE5C, baseCost: 10000, 0,label: null), // glowing rune (C)
                    new CrownSterlingReward(itemO: 0xE5F, baseCost: 10000, 0,label: null), // glowing rune (H)
                    new CrownSterlingReward(itemO: 0xE62, baseCost: 10000, 0,label: null), // glowing rune (M)
                    new CrownSterlingReward(itemO: 0xE65, baseCost: 10000, 0,label: null), // glowing rune (O)
                    new CrownSterlingReward(itemO: 0xE68, baseCost: 10000, 0,label: null), // glowing rune (W)
                    #endregion 10000
                    
                    #region 12000
                    // returned to townships
                    //new CrownSterlingReward(itemO: 0xE56, baseCost: 12000, 0,label: null), // tree stumps
                    //new CrownSterlingReward(itemO: 0xE57, baseCost: 12000, 0,label: null),
                    //new CrownSterlingReward(itemO: 0xE58, baseCost: 12000, 0,label: null),
                    //new CrownSterlingReward(itemO: 0xE59, baseCost: 12000, 0,label: null),  
                    #endregion 12000

                    #region 13000
                    new CrownSterlingReward(itemO: new AddonFactory(0x2A79, typeof(MountedPixieWhiteDeed)),baseCost: 13000,  0,label: null),    // mounted pixie
                    new CrownSterlingReward(itemO: new AddonFactory(0x2A73, typeof(MountedPixieOrangeDeed)),baseCost: 13000,  0,label: null),   // mounted pixie
                    new CrownSterlingReward(itemO: new AddonFactory(0x2A77, typeof(MountedPixieLimeDeed)),baseCost: 13000,  0,label: null),     // mounted pixie
                    new CrownSterlingReward(itemO: new AddonFactory(0x2A75, typeof(MountedPixieBlueDeed)),baseCost: 13000,  0,label: null),     // mounted pixie
                    new CrownSterlingReward(itemO: new AddonFactory(0x2A71, typeof(MountedPixieGreenDeed)),baseCost: 13000,  0,label: null),    // mounted pixie
                    #endregion 13000

                    #region 15000
                    // returned to townships
                    //new CrownSterlingReward(itemO: 0x0CCA, baseCost: 15000, 0,label: null),  // a tree (dead)
                    //new CrownSterlingReward(itemO: 0x0CCB, baseCost: 15000, 0,label: null),  // a tree (dead)
                    //new CrownSterlingReward(itemO: 0x0CCC, baseCost: 15000, 0,label: null),  // a tree (dead)
                    //new CrownSterlingReward(itemO: 0x0CCD, baseCost: 15000, 0,label: null),  // a tree (dead)

                    new CrownSterlingReward(itemO: new AddonFactory(0x2A47, typeof(SwordDisplayBlackDeed)),baseCost: 15000,  0,label: null),    // sword display
                    new CrownSterlingReward(itemO: new AddonFactory(0x2A45, typeof(SwordDisplayRedDeed)),baseCost: 15000,  0,label: null),      // sword display
                    new CrownSterlingReward(itemO: new AddonFactory(0x2A49, typeof(SwordDisplayVioletDeed)),baseCost: 15000,  0,label: null),   // sword display
                    new CrownSterlingReward(itemO: new AddonFactory(0x2A4B, typeof(SwordDisplayGreenDeed)),baseCost: 15000,  0,label: null),    // sword display
                    #endregion 15000

                    #region 20000
                    new CrownSterlingReward(itemO: 0x1223, baseCost: 20000, 0,label: null), // pedestal
                    new CrownSterlingReward(itemO: 0x1F2A, baseCost: 20000, 0,label: null), // pedestal
                    
                    #endregion 20000
                })},

            {RewardSet.Ghoulish, new ReadOnlyCollection <CrownSterlingReward>(
                new List <CrownSterlingReward>()
                {
                    new CrownSterlingReward(itemO: 0x1A01,baseCost: 5000,  0,label: null),   // hanging skeleton
                    new CrownSterlingReward(itemO: 0x1A02,baseCost: 5000,  0,label: null),   // hanging skeleton
                    new CrownSterlingReward(itemO: 0x1A03,baseCost: 5000,  0,label: null),   // hanging skeleton
                    new CrownSterlingReward(itemO: 0x1A04,baseCost: 5000,  0,label: null),   // hanging skeleton

                    new CrownSterlingReward(itemO: 0x1A0B,baseCost: 5000,  0,label: "a skeleton"),   // hanging skeleton - the item data is wrong for this, says "wooden wall"
                    new CrownSterlingReward(itemO: 0x1A0C,baseCost: 5000,  0,label: null),   // hanging skeleton
                    new CrownSterlingReward(itemO: 0x1B1D,baseCost: 5000,  0,label: null),   // hanging skeleton with meat
                    new CrownSterlingReward(itemO: 0x1B1E,baseCost: 5000,  0,label: null),   // hanging skeleton with meat
                    
                    new CrownSterlingReward(itemO: 0x1A05,baseCost: 5000,  0,label: null),   // hanging skeleton
                    new CrownSterlingReward(itemO: 0x1A06,baseCost: 5000,  0,label: null),   // hanging skeleton
                    new CrownSterlingReward(itemO: 0x1A09,baseCost: 5000,  0,label: null),   // hanging skeleton
                    new CrownSterlingReward(itemO: 0x1A0A,baseCost: 5000,  0,label: null),   // hanging skeleton
                    new CrownSterlingReward(itemO: 0x1A0D,baseCost: 5000,  0,label: null),   // hanging skeleton
                    new CrownSterlingReward(itemO: 0x1A0E,baseCost: 5000,  0,label: null),   // hanging skeleton

                    new CrownSterlingReward(itemO: 0x1B7C,baseCost: 5000,  0,label: null),   // hanging skeleton
                    new CrownSterlingReward(itemO: 0x1B7D,baseCost: 5000,  0,label: null),   // hanging skeleton
                    new CrownSterlingReward(itemO: 0x1B7F,baseCost: 5000,  0,label: null),   // hanging skeleton
                    new CrownSterlingReward(itemO: 0x1B80,baseCost: 5000,  0,label: null),   // hanging skeleton

                    new CrownSterlingReward(itemO: 0x1A07,baseCost: 5000,  0,label: null),   // chains
                    new CrownSterlingReward(itemO: 0x1A08,baseCost: 5000,  0,label: null),   // chains

                    new CrownSterlingReward(itemO: 0x1B09,baseCost: 5000,  0,label: null),   // bone pile // drops on zombie - added to ignore list 
                    new CrownSterlingReward(itemO: 0x1B0A,baseCost: 5000,  0,label: null),   // bone pile
                    new CrownSterlingReward(itemO: 0x1B0B,baseCost: 5000,  0,label: null),   // bone pile
                    new CrownSterlingReward(itemO: 0x1B0C,baseCost: 5000,  0,label: null),   // bone pile
                    new CrownSterlingReward(itemO: 0x1B0D,baseCost: 5000,  0,label: null),   // bone pile
                    new CrownSterlingReward(itemO: 0x1B0E,baseCost: 5000,  0,label: null),   // bone pile
                    new CrownSterlingReward(itemO: 0x1B0F,baseCost: 5000,  0,label: null),   // bone pile
                    new CrownSterlingReward(itemO: 0x1B10,baseCost: 5000,  0,label: null),   // bone pile

                    new CrownSterlingReward(itemO: 0x1B11,baseCost: 5000,  0,label: null),   // bone
                    new CrownSterlingReward(itemO: 0x1B12,baseCost: 5000,  0,label: null),   // bone

                    new CrownSterlingReward(itemO: 0x1B13,baseCost: 5000,  0,label: null),   // jaw bone
                    new CrownSterlingReward(itemO: 0x1B14,baseCost: 5000,  0,label: null),   // jaw bone

                    new CrownSterlingReward(itemO: 0x1B15,baseCost: 5000,  0,label: null),   // pelvis bone
                    new CrownSterlingReward(itemO: 0x1B16,baseCost: 5000,  0,label: null),   // pelvis bone

                    new CrownSterlingReward(itemO: 0x1B17,baseCost: 5000,  0,label: null),   // rib cage
                    new CrownSterlingReward(itemO: 0x1B18,baseCost: 5000,  0,label: null),   // rib cage

                    new CrownSterlingReward(itemO: 0x1B19,baseCost: 5000,  0,label: null),   // bone shards
                    new CrownSterlingReward(itemO: 0x1B1A,baseCost: 5000,  0,label: null),   // bone shards

                    new CrownSterlingReward(itemO: 0x1B1B,baseCost: 5000,  0,label: null),   // spine
                    new CrownSterlingReward(itemO: 0x1B1C,baseCost: 5000,  0,label: null),   // spine

                    new CrownSterlingReward(itemO: 0x1CDD+0,baseCost: 5000,  0,label: null),   // body parts (arm)
                    new CrownSterlingReward(itemO: 0x1CDD+1,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+2,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+3,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+4,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+5,baseCost: 5000,  0,label: null),   // body parts (leg)
                    new CrownSterlingReward(itemO: 0x1CDD+6,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+7,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+8,baseCost: 5000,  0,label: null),   // body parts (arm)
                    new CrownSterlingReward(itemO: 0x1CDD+9,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+10,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+11,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+12,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+13,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+14,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+15,baseCost: 5000,  0,label: null),   // body parts (leg)
                    new CrownSterlingReward(itemO: 0x1CDD+16,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+17,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+18,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+19,baseCost: 5000,  0,label: null),   // body parts
                    new CrownSterlingReward(itemO: 0x1CDD+20,baseCost: 5000,  0,label: null),   // body parts

                    new CrownSterlingReward(itemO: 0x1D92 + 0,baseCost: 5000,  0,label: null),   // blood
                    new CrownSterlingReward(itemO: 0x1D92 + 1,baseCost: 5000,  0,label: null),   // blood
                    new CrownSterlingReward(itemO: 0x1D92 + 2,baseCost: 5000,  0,label: null),   // blood
                    new CrownSterlingReward(itemO: 0x1D92 + 3,baseCost: 5000,  0,label: null),   // blood
                    new CrownSterlingReward(itemO: 0x1D92 + 4,baseCost: 5000,  0,label: null),   // blood
                    
                    new CrownSterlingReward(itemO: 0xE23,baseCost: 5000,  0,label: null),        // bloody water

                    new CrownSterlingReward(itemO: 0x122A + 0,baseCost: 5000,  0,label: null),   // blood
                    new CrownSterlingReward(itemO: 0x122A + 1,baseCost: 5000,  0,label: null),   // blood
                    new CrownSterlingReward(itemO: 0x122A + 2,baseCost: 5000,  0,label: null),   // blood
                    new CrownSterlingReward(itemO: 0x122A + 3,baseCost: 5000,  0,label: null),   // blood
                    new CrownSterlingReward(itemO: 0x122A + 4,baseCost: 5000,  0,label: null),   // blood

                    new CrownSterlingReward(itemO: 0x0ECA + 0,baseCost: 5000,  0,label: null),   // bones
                    new CrownSterlingReward(itemO: 0x0ECA + 1,baseCost: 5000,  0,label: null),   // bones
                    new CrownSterlingReward(itemO: 0x0ECA + 2,baseCost: 5000,  0,label: null),   // bones
                    new CrownSterlingReward(itemO: 0x0ECA + 3,baseCost: 5000,  0,label: null),   // bones
                    new CrownSterlingReward(itemO: 0x0ECA + 4,baseCost: 5000,  0,label: null),   // bones
                    new CrownSterlingReward(itemO: 0x0ECA + 5,baseCost: 5000,  0,label: null),   // bones
                    new CrownSterlingReward(itemO: 0x0ECA + 6,baseCost: 5000,  0,label: null),   // bones
                    new CrownSterlingReward(itemO: 0x0ECA + 7,baseCost: 5000,  0,label: null),   // bones
                    new CrownSterlingReward(itemO: 0x0ECA + 8,baseCost: 5000,  0,label: null),   // bones

                })},
        };
        public class AddonFactory : Item
        {
            Type m_Type = null;
            public Item Construct { get { return Activator.CreateInstance(m_Type) as Item; } }
            [Constructable]
            public AddonFactory(int graphic, Type type)
                : base(graphic)
            {
                m_Type = type;
            }
            public AddonFactory(Serial serial) : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.WriteEncodedInt(0); // version

                // version 0
                string type = m_Type == null ? null : m_Type.Name;
                writer.Write(type);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadEncodedInt();

                switch (version)
                {
                    case 0:
                        {
                            string typeName = reader.ReadString();
                            if (typeName != null)
                                m_Type = ScriptCompiler.FindTypeByName(typeName);
                            break;
                        }
                }
            }
        }
        private static string ClipName(AddonDeed deed)
        {
            if (deed == null || deed.Name == null)
                return null;

            string new_name = ClipName(deed.Name);

            deed.Delete();
            return new_name.Trim();
        }
        public static string ClipName(string name)
        {
            string new_name = name;
            new_name = name.Replace("a deed for ", "", StringComparison.OrdinalIgnoreCase);

            if (new_name.StartsWith("a ", StringComparison.OrdinalIgnoreCase))
                new_name = new_name.Replace("a ", "", StringComparison.OrdinalIgnoreCase);
            else if (new_name.StartsWith("an ", StringComparison.OrdinalIgnoreCase))
                new_name = new_name.Replace("an ", "", StringComparison.OrdinalIgnoreCase);
            else if (new_name.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
                new_name = new_name.Replace("the ", "", StringComparison.OrdinalIgnoreCase);

            if (new_name.Contains(" addon ", StringComparison.OrdinalIgnoreCase))
                new_name = new_name.Replace(" addon ", " ", StringComparison.OrdinalIgnoreCase);

            return new_name.Trim();
        }
        public static string Normalize(string name)
        {
            IPluralize ps = new Pluralizer();
            name = ClipFacing(name);
            name = ClipArticle(name);
            name = ps.Singularize(name);
            return name.Trim();
        }
        public static string ClipArticle(string name)
        {
            return ClipName(name);
        }
        public static string ClipFacing(string name)
        {
            string new_name = name;
            if (new_name != null)
            {
                new_name = new_name.Replace("(North)", "", StringComparison.OrdinalIgnoreCase);
                new_name = new_name.Replace("(South)", "", StringComparison.OrdinalIgnoreCase);
                new_name = new_name.Replace("(East)", "", StringComparison.OrdinalIgnoreCase);
                new_name = new_name.Replace("(West)", "", StringComparison.OrdinalIgnoreCase);
            }
            return new_name.Trim();
        }
        public bool ClaimReward(Mobile from, CrownSterlingReward reward)
        {
            if (from == null || from.Backpack == null || from.BankBox == null)
                return false;

            int buyerSterling = from.Backpack.GetAmount(typeof(Sterling)) + from.BankBox.GetAmount(typeof(Sterling));
            if (buyerSterling < reward.Cost)
            {
                if (m_NPC is BaseCreature bc)
                    bc.SayTo(from, 500192);//Begging thy pardon, but thou casnt afford that.
                return false;
            }

            /* construct the item
             */
            Item toGive = null;
            if (reward.ItemO is Item item)
            {
                if (item is AddonFactory af)
                    toGive = af.Construct;
                else
                {
                    toGive = Utility.Dupe(item);                                    // the inventory item probably has DeleteOnRestart set
                    toGive.SetItemBool(Item.ItemBoolTable.DeleteOnRestart, false);  // clear it 
                }
            }
            else
            {
                toGive = new Item((int)reward.ItemO);
                if (toGive.ItemID == 0x0FAC)
                    toGive.Light = LightType.Circle225;
            }

            if (toGive == null)
                return false;
            else
            {
                if (toGive is not BaseAddonDeed)    // will (should) have a Default name like "deed to a xyz"
                    toGive.Name = reward.Label;
                toGive.Hue = reward.Hue;
                toGive.LootType = LootType.Rare;    // UnStealable. Lootable, always.
                if (toGive.Weight == 0.0)
                    toGive.Weight = 1;
                toGive.Movable = true;              // heh, why is the ruined bookcase movable false?
                toGive.IsIntMapStorage = false;
            }

            if (!from.PlaceInBackpack(toGive))
            {
                from.SendLocalizedMessage(1078837); // Your backpack is full! Please make room and try again.
                toGive.Delete();
                return false;
            }

            // consume the Sterling. First from backpack, then from bankbox
            if (Purchase(from, reward) == false)
            {
                from.SayTo(from, 500192);//Begging thy pardon, but thou casnt afford that.
                toGive.Delete();
                return false;
            }

            from.SendMessage("Your purchase has been placed in your backpack.");
            from.PlaySound(0x5A7);

            LogHelper logger = new LogHelper("CrownSterlingPurchases.log", false, true);
            logger.Log(LogType.Mobile, from, string.Format($"Purchased rare {toGive} (Label={reward.Label}) for {reward.Cost} Sterling."));
            logger.Finish();

            return true;
        }

        private bool Purchase(Mobile from, CrownSterlingReward reward)
        {
            int backpackSterling = from.Backpack.GetAmount(typeof(Sterling));
            int bankboxSterling = from.BankBox.GetAmount(typeof(Sterling));

            if (backpackSterling + bankboxSterling < reward.Cost)
                return false;   // can't afford

            if (backpackSterling >= reward.Cost)
                return from.Backpack.ConsumeTotal(typeof(Sterling), reward.Cost);
            else
            {
                int have = backpackSterling;
                int need = reward.Cost - have;
                if (have > 0)
                    from.Backpack.ConsumeTotal(typeof(Sterling), have);
                if (need > 0)
                    from.BankBox.ConsumeTotal(typeof(Sterling), need);
                return true;
            }
        }
    }
}