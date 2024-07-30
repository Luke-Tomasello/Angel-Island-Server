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

/* EXPERIMENTAL �GREEN COMPUTING� DESIGN
 * Special notes on the implementation of the Groundskeeper.
 * While there is lots of state information, the entire class was designed with no serilization data beyond the standard int version.
 * The design (you might call 'green') is intented to be completly restartable in a useful state without the need to save-to-disk
 * all state values.
 * This approach is probably only appropriate for a small percentage of implementations, but it's at least interesting to be aware
 * how rich the functionality can be without increasing either our save times, or footprint on disk.
 */

/* Scripts/Mobiles/Townfolk/Groundskeeper.cs
 * ChangeLog
 *  6/13/23, Yoar
 *      Groundskeepers ignore items in houses/boats
 *  8/16/22, Adam
 *      no longer pick up spawned items
 *  1/8/22, Adam
 *      Change the default time-on-ground Threshold from 1 second to 20 seconds.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 2 loops updated.
 *	4/28/08, Adam
 *		In DoSpawn() check to see if there is a groundskeeper already in LOS of the item we want to cleanup. 
 *			if so, assume he'll take care of it (don't spawn another.)
 *	4/23/08, Adam
 *		- Add and AllowFilter() to allow certain items
 *		- return the notion to of 'last scan' to prevent too many scans in too short of time
 *		- add a 5 item threshold. that is, managers (spawners) ignore 5 or less items on the ground. (groundskeepers do not observe this threshold.)
 *	4/22/08, Adam
 *		- Clear the item list between searches to prevent retrying missing or deleted items
 *		- remove item caching since we rescan anyway
 *		- remove notion of 'lastscan' and simply lastspawn
 *  4/20/08, Adam
 *		created by Adam
 */

using Server.Diagnostics;
using Server.Items;
using Server.Multis;
using System;
using System.Collections;
using static Server.Utility;

namespace Server.Mobiles
{
    public class Groundskeeper : BaseCreature
    {
        // state machibe states
        enum WorkState
        {
            Idle,
            Searching,
            SearchFar,
            Pickup,
            WalkTo,
        }
        private const int m_DeleteMinutes = 5;
        private WorkState m_workState = WorkState.Idle;
        private ArrayList m_itemsNear = new ArrayList();
        private DeleteTimer m_Timer;

        /*
		 * 1. m_scanRangeNear and m_reach should probably be the same, or at least reach should exceed scan range or you will get stuck finding things you can�t pickup.
		 * 2. m_scanRangeNear and m_reach of 1 (from a players perspective) looks about right. A value of 2 seems a bit aggressive as the NPC picks things from 3 tiles away which looks a bit odd (but it�s much �faster� should we need the cleanup.
		 * 3. We don�t currently serialize these values, but we expose them for tuning/testing and possibly so the spawn parent (guard) can crank up the aggressiveness.
		 */
        private int m_scanRangeNear = 1;        // how many tiles we look at when we search near
        private int m_scanRangeFar = 12;        // how many tiles we look at when we search far
        private int m_reach = 1;                // how long our arm is

        [CommandProperty(AccessLevel.GameMaster)]
        public int ScanRangeNear                // how many tiles we look at when we search near
        { get { return m_scanRangeNear; } set { m_scanRangeNear = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ScanRangeFar             // how many tiles we look at when we search far
        { get { return m_scanRangeFar; } set { m_scanRangeFar = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Reach                        // how long our arm is
        { get { return m_reach; } set { m_reach = value; } }

        TimeSpan m_threshold = new TimeSpan(0, 0, 20);
        [CommandProperty(AccessLevel.GameMaster)]
        // how long something has to be on the ground before we pick it up
        public TimeSpan Threshold
        { get { return m_threshold; } set { m_threshold = value; } }

        [Constructable]
        public Groundskeeper()
            : base(AIType.AI_Melee, FightMode.Aggressor, 22, 1, 0.2, 1.0)
        {
            InitBody();
            InitOutfit();
            Title = "the groundskeeper";
            ResetTimer();
        }

        public override void InitBody()
        {
            SetStr(90, 100);
            SetDex(90, 100);
            SetInt(15, 25);

            Hue = Utility.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 401;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 400;
                Name = NameList.RandomName("male");
            }
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);

            // Special dye colors: a good look for a groundskeeper and a new challange!
            AddItem(new Shirt(Utility.RandomSpecialTanHue()));
            AddItem(new LongPants(Utility.RandomSpecialDarkBlueHue()));
            AddItem(new Boots(Utility.RandomSpecialBrownHue()));

            switch (Utility.Random(4))
            {
                case 0: AddItem(new ShortHair(Utility.RandomHairHue())); break;
                case 1: AddItem(new TwoPigTails(Utility.RandomHairHue())); break;
                case 2: AddItem(new ReceedingHair(Utility.RandomHairHue())); break;
                case 3: AddItem(new KrisnaHair(Utility.RandomHairHue())); break;
            }

            PackGold(Utility.RandomMinMax(20, 50));
        }

        public override void OnThink()
        {
            try
            {   // be a groundskeeper
                ProcessWorkState();
            }
            catch (Exception ex)
            {   // don't be a poor groundskeeper!
                LogHelper.LogException(ex);
            }

            base.OnThink();
        }

        // simple state machine - called on each OnThink() tick
        private void ProcessWorkState()
        {
            switch (m_workState)
            {
                default: m_workState = WorkState.Idle; break;
                case WorkState.Idle: WorkStateIdle(); break;
                case WorkState.Searching: WorkStateSearching(); break;
                case WorkState.SearchFar: WorkStateSearchFar(); break;
                case WorkState.Pickup: WorkStatePickup(); break;
            }
        }

        // ignore items that have any of these characteristics
        private static bool IgnoreFilter(Item item, TimeSpan threshold)
        {
            if (item is Item == false ||                        // sanity
                item.Deleted == true ||                         // ignore if deleted 
                item.Movable == false ||                        // ignore if not movable
                item.Visible == false ||                        // ignore if not visible
                item.Parent != null ||                          // ignore in a pack/pouch/bag etc
                item.Decays == false ||                         // ignore if the item doesn't decay
                item.Map == Map.Internal ||                     // ignore if on the internal map
                Utility.IsSpawnedItem(item) ||                  // no spawned items please
                CheckHouse(item) ||                             // ignore items in houses
                CheckBoat(item) ||                              // ignore items on boats
                !CheckThreshold(item, threshold))               // ignore item if it has not been on the ground long enough
                return true;

            return false;
        }

        private static bool CheckHouse(Item item)
        {
            BaseHouse house = BaseHouse.FindHouseAt(item);

            return (house != null && house.Contains(item));
        }

        private static bool CheckBoat(Item item)
        {
            BaseBoat boat = BaseBoat.FindBoatAt(item);

            return (boat != null && boat.Contains(item));
        }

        // we'll try to be smart by allowing certain things, like a couple marked runes
        // (vendors leave runes)
        private static void AllowFilter(ArrayList list)
        {
            // recall runes:
            //	RULE: no more than 3, must be marked, must be named

            // count the runes
            int runeCount = 0;
            ArrayList remove = new ArrayList();
            foreach (Item ix in list)
            {
                if (ix is RecallRune)
                {
                    RecallRune rx = ix as RecallRune;
                    if (rx.Marked && rx.Description != null)
                        if (++runeCount <= 3)
                            remove.Add(ix);
                }
            }

            // remove all but 3 runes in the area
            foreach (Item ix in remove)
                list.Remove(ix);

            return;
        }

        // see if the item has been sitting on the ground long enough to qualify
        private static bool CheckThreshold(Item item, TimeSpan threshold)
        {
            return DateTime.UtcNow > (item.LastMoved + threshold);
        }

        private bool IgnoreFilter(Item item)
        {
            //if (DateTime.UtcNow > item.LastMoved + m_threshold)
            //Utility.ConsoleOut("Time to delete", ConsoleColor.Yellow);
            //else
            //Utility.ConsoleOut("Not yet time to delete", ConsoleColor.Yellow);
            return IgnoreFilter(item, m_threshold);
        }

        private void WorkStateIdle()
        {   // just advance to the looking around state
            m_workState = WorkState.Searching;
        }

        private void WorkStateSearching()
        {   // start with a clean list
            m_itemsNear.Clear();
            IPooledEnumerable eable = GetItemsInRange(m_scanRangeNear);
            foreach (Item item in eable)
            {
                if (IgnoreFilter(item) || !this.InLOS(item))
                    continue;

                if (m_itemsNear.Contains(item) == false)
                    m_itemsNear.Add(item);
            }
            eable.Free();

            // post process list to allow things like a couple marked runes
            AllowFilter(m_itemsNear);

            if (m_itemsNear.Count > 0)
                m_workState = WorkState.Pickup;
            else
                m_workState = WorkState.SearchFar;
        }

        private void WorkStatePickup()
        {
            int count = 0;
            foreach (Item item in m_itemsNear)
            {
                if (IgnoreFilter(item) || !this.InLOS(item))
                    continue;

                // is the item now out of range?
                if (this.GetDistanceToSqrt(item) > m_reach)
                {
                    DebugSay(DebugFlags.AI, "We must have walked past the item");
                    continue;
                }
                else
                {
                    DebugSay(DebugFlags.AI, "We will delete item {0}", item);
                    item.Delete();
                }

                // only do so much work on each tick
                if (count++ > 20)
                    break;
            }

            m_itemsNear.Clear();                        // since we're going to rescan anyway, there's no sense in remembering these items

            if (count > 0 && Utility.RandomBool())      // let the world know what we're doing
                Emote("*picks up trash*");

            if (count > 0)                              // only delete if we're not picking up trash
                ResetTimer();

            // go look for more trash
            m_workState = WorkState.Searching;
        }

        // make use of baseAI.DoActionWander() and waypoints to wander over to trash
        private ArrayList m_wayPointList = new ArrayList();
        private void WorkStateSearchFar()
        {
            ArrayList itemsFar = new ArrayList();
            IPooledEnumerable eable = GetItemsInRange(m_scanRangeFar);
            foreach (Item item in eable)
            {
                // we check LOS to prevent grounds keepers tossing their waypoint to some item on the ground if they are on the roof
                //	there's probably another groundskeeper there already to take care of that one
                if (IgnoreFilter(item) || !this.InLOS(item))
                    continue;

                if (itemsFar.Contains(item) == false)
                    itemsFar.Add(item);
            }
            eable.Free();

            // post process list to allow things like a couple marked runes
            AllowFilter(itemsFar);

            if (itemsFar.Count > 0)
            {
                Item item = itemsFar[Utility.Random(itemsFar.Count)] as Item;
                // drop an auto deleting waypoint at this bit of rubbish. DoActionWander will drive the mob towards the WayPoint
                //	when we arrive at the waypoint, it will auto distruct.
                DestroyWayPoints();             // destroy the old waypoints
                this.CurrentWayPoint = new AutoWayPoint();
                this.CurrentWayPoint.MoveToWorld(item.Location, item.Map);
                m_wayPointList.Add(this.CurrentWayPoint);
            }

            // go Idle while we wander over to the trash
            m_workState = WorkState.Idle;
        }

        // should only ever be 1
        private void DestroyWayPoints()
        {
            foreach (Item ix in m_wayPointList)
                if (ix is AutoWayPoint)
                    (ix as AutoWayPoint).Delete();

            m_wayPointList.Clear();
        }

        private class DeleteTimer : Timer
        {
            private Mobile m_mobile;

            public DeleteTimer(Mobile mob, DateTime time)
                : base(time - DateTime.UtcNow)
            {
                m_mobile = mob;
                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                if (m_mobile != null && m_mobile.Deleted == false)
                {
                    Effects.SendLocationParticles(EffectItem.Create(m_mobile.Location, m_mobile.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
                    m_mobile.PlaySound(0x1FE);
                    m_mobile.Delete();
                }
            }
        }

        private void ResetTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
            m_Timer = new DeleteTimer(this, DateTime.UtcNow + TimeSpan.FromMinutes(m_DeleteMinutes));
            m_Timer.Start();
        }

        public Groundskeeper(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            ResetTimer();
        }

        /*
		 * ******************** begin caller / host support routines ******************** 
		 */

        // knowledge any managers (of groundskeepers) will have. A timesheet if you will.
        private class GroundskeeperStatus
        {
            private DateTime m_LastScan = DateTime.UtcNow;
            public DateTime LastScan { get { return m_LastScan; } set { m_LastScan = value; } }
            public TimeSpan ScanFreq { get { return new TimeSpan(0, 0, 60 * 2 + Utility.Random(120)); } }   // can Scan every 3'ish minutes

            private DateTime m_LastSpawn = DateTime.UtcNow;
            public DateTime LastSpawn { get { return m_LastSpawn; } set { m_LastSpawn = value; } }
            public TimeSpan SpawnFreq { get { return new TimeSpan(0, 0, 60 * 3 + Utility.Random(120)); } }  // can spawn every 5'ish minutes
            public GroundskeeperStatus()
            {
            }
        }

        // table of managers for the groundskeepers. Currently Guards are the only managers.
        private static Hashtable m_managers = new Hashtable();

        // called from baseguard to manage trash pick-up in the area of the guard.
        public static void DoGroundskeeper(Mobile m)
        {
            GroundskeeperStatus al;
            if (m_managers.Contains(m))                         // if we have it already
                al = m_managers[m] as GroundskeeperStatus;      // get the GroundskeeperStatus for this manager
            else
            {
                m_managers[m] = new GroundskeeperStatus();      // start a new list of clients at this IP address
                DoGroundskeeper(m);
                return;
            }

            // now we have a GroundskeeperStatus for this manager

            // if we've spawned a groundskeeper recently, no need to continue
            // if we've scanned recently, no need to continue
            if (DateTime.UtcNow > al.LastSpawn + al.SpawnFreq && DateTime.UtcNow > al.LastScan + al.ScanFreq)
            {
                ArrayList list = new ArrayList();
                if (DoScan(m, al, list))
                {
                    System.Console.WriteLine("Groundskeeper spawn started ... ");
                    Utility.TimeCheck tc = new Utility.TimeCheck();
                    tc.Start();
                    DoSpawn(al, list);
                    tc.End();
                    System.Console.WriteLine("checked {0} items in {1}", list.Count, tc.TimeTaken);
                }
            }
        }

        private static bool DoScan(Mobile m, GroundskeeperStatus al, ArrayList list)
        {
            al.LastScan = DateTime.UtcNow;
            TimeSpan threshold = new TimeSpan(0, 3, 0);     // age of item on the ground before we care
            int trash = 0;
            foreach (Item item in m.GetItemsInRange(12))    // 12 tiles?
            {   // is this trash? No LOS checks here because the manager won't be trying to pick it up
                if (Groundskeeper.IgnoreFilter(item, threshold))
                    continue;

                list.Add(item);                         // add it
            }

            // post process list to allow things like a couple marked runes
            Groundskeeper.AllowFilter(list);
            trash = list.Count; // total trash after removing allowed items

            // Keep only representative items from different Zs by deleting all items except those with unique Zs
            ArrayList temp = new ArrayList();
            foreach (Item ix in list)
                temp.Add(ix);

            // only spawn 1 groundskeeper per Z plane and only when those Z planes are not within LOS.
            //	Example: a stack of scales will be on different Z panes, but within LOS (reachable by the same groundskeeper.)
            foreach (Item ix in temp)
                if (!DifferentZ(list, ix as Item))
                    list.Remove(ix);

            // did the scan find enough trash to pick up?
            //	if someone drops a handful of stuff (< 5 items), ignore it
            return trash > 5;
        }

        private static bool DifferentZ(ArrayList list, Item item)
        {   // see if this item is on a Z axis different that items we already know about
            foreach (Item ix in list)
            {
                if (ix == item)
                    continue;

                if (ix is Item && ix.Z == item.Z)
                    return false;
            }
            return true;
        }

        private static void DoSpawn(GroundskeeperStatus al, ArrayList list)
        {
            al.LastSpawn = DateTime.UtcNow;
            foreach (Item ix in list)
                if (ix is Item)
                {   // first see if there is already a grounds keeper on the job.
                    Sector sector = ix.Map.GetSector(ix.X, ix.Y);
                    bool alreadyHandled = false;
                    foreach (Mobile mx in sector.Mobiles)
                    {
                        if (mx == null)
                            continue;

                        if (mx is Groundskeeper && mx.Deleted == false)
                            if (((mx as Groundskeeper).GetDistanceToSqrt(ix) < 12) && ((mx as Groundskeeper).InLOS(ix)))
                            {   // If the grounds keeper can see the Item, then he's already working on it.
                                alreadyHandled = true;
                                break;
                            }
                    }
                    // if there is no groundskeeper on the job, spawn a new one
                    if (alreadyHandled == false)
                    {
                        Groundskeeper m = new Groundskeeper();
                        m.Home = ix.Location;                   // setup the groundskeeper's home
                        m.RangeHome = 10;                       // this is the default for a mobile, but I'd like to be explicit here
                        Point3D location = SpawnClose(ix, m);   // spawn as close as possible
                        m.MoveToWorld(location, ix.Map);
                        Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
                        m.PlaySound(0x1FE);
                    }
                }
        }

        // Description: Spawn as close-to or on-top-of the 'trash item' as possible.
        // Note1: you can for instance spawn atop a 'recall rune', but not a 'small crate'. This is because 
        //	CanSpawnMobile() checks for the impassable flag when testing the destination point. the 'small crate' has impassable 
        //  flags (i.e., you can't walk over it.) and the recall rune does not. 
        // Note2: When spawning a groundskeeper on a roof to pick up trash, you want the groundskeeper on the same Z and as close 
        //	to the item as possible else they may be spawned over-the-edge placing them on the gtound and unable to pick up the item.
        // It is for this reasons that we work so hard to place the groundskeeper as close to the 'trash' item as possible.
        private static Point3D SpawnClose(Item ix, object o)
        {
            Point3D location;

            // try 5 times at the same Z - starting close and moving outward
            for (int ir = 0; ir < 5; ir++)
            {
                location = Spawner.GetSpawnPosition(ix.Map, ix.Location, ir, SpawnFlags.ForceZ, o);
                if (location != ix.Location)
                    return location;
            }

            // try 5 times at any Z - starting close and moving outward
            for (int ir = 0; ir < 5; ir++)
            {
                location = Spawner.GetSpawnPosition(ix.Map, ix.Location, ir, SpawnFlags.None, o);
                if (location != ix.Location)
                    return location;
            }

            // give up
            return ix.Location;
        }
    }
}