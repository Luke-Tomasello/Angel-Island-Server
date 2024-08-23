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

/* CHANGELOG
 *  8/22/2023, Adam (MakeCamp/LoadCamp)
 *      Basic framework for build 'Camps'
 *  8/4/2023, Adam
 *      Fix DecayDelay to use spawner Min/Max respawn times
 *  7/20/2023, Adam
 *      We no longer use the 'camp multi' as it injects a 'static chest' into the mix. This chest conflicts with the treasure chest we instantiate.
 *      (confuses players.) We now build these camps dynamically.
 *  7/20/2023, Adam
 *      In destruction, don't remove the mobile if it is BaseEscortable and they have an Escorter.
 *  6/10/2023, Adam
 *      Refactor so that camps are static until 'corrupted' Once corrupted, they will respawn on the next tick
 *          Corrupted is one of the camp mobiles or items is deleted, or a chest is unlocked.
 *          Next Tick, in the case of a camp on a spawner, is the respawn time of the spawner, otherwise it's a fixed 30 minutes.
 *  6/4/2023, Adam
 *      Use CanSpawnMobile and GetSpawnPosition to help place items and mobiles if default placing is bad.
 *  8/15/22, Adam (item.SetDynamicFeature(FeatureBits.CampComponent))
 *      Camps are special versions of multis that have live items - filled chests and whatnot.
 *      While enumerating world items, sometimes we would like to know if the items we found belong to one of these camps.
 *      We therefore mark such items: item.SetDynamicFeature(FeatureBits.CampComponent);
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using static Server.Utility;

namespace Server.Multis
{
    public abstract class BaseCamp : BaseMulti
    {
        // Do away with the core multi as it injects a static chest.
        //  With this new model, we can do interesting things with the camp
        private static List<MultiTileEntry> m_OrcLizardRatComponents = new() {
                new MultiTileEntry(1, 0, 0, 0, 0),
                new MultiTileEntry(2081, -1, -1, 0, 1),
                new MultiTileEntry(2081, -1, 0, 0, 1),
                new MultiTileEntry(2082, -1, 1, 0, 1),
                new MultiTileEntry(3088, 2, 0, 0, 1),   // broken chair
                new MultiTileEntry(2876, 2, 1, 0, 1),
                new MultiTileEntry(2908, 2, 2, 0, 1),   // chair
                // Eliminate redundant 'static' gate
                //new MultiTileEntry(2086, -2, 1, 0, 0),
                new MultiTileEntry(2081, -1, -2, 0, 1),
                new MultiTileEntry(7036, -2, -1, 0, 1),
                new MultiTileEntry(6942, -2, -2, 0, 1),
                new MultiTileEntry(2429, 2, 1, 6, 1),
                new MultiTileEntry(2909, 3, 1, 0, 1),   // chair
                new MultiTileEntry(2420, 1, 3, 0, 1),
                new MultiTileEntry(4012, 1, 3, 0, 1),
                new MultiTileEntry(5696, 1, -3, 0, 1),
                new MultiTileEntry(2081, -3, 0, 0, 1),
                new MultiTileEntry(2081, -3, -1, 0, 1),
                new MultiTileEntry(2081, -3, -2, 0, 1),
                new MultiTileEntry(2083, -1, -3, 0, 1),
                new MultiTileEntry(2083, -2, -3, 0, 1),
                new MultiTileEntry(2081, -3, 1, 0, 1),
                new MultiTileEntry(3715, 3, -2, 0, 1),
                new MultiTileEntry(4704, 4, -2, 0, 1),  // guillotine
                // Eliminate redundant 'static' chest
                //new MultiTileEntry(3708, 4, 4, 0, 1),
                };
        [Flags]
        private enum CampBools : byte
        {
            None = 0x00,
            Corrupted = 0x01,
        }
        private CampBools m_BoolTable = CampBools.None;
        public virtual List<MultiTileEntry> OrcLizardRatComponents { get { return m_OrcLizardRatComponents; } }
        private ArrayList m_Items, m_Mobiles;
        public ArrayList ItemComponents { get { return m_Items; } }
        public ArrayList MobileComponents { get { return m_Mobiles; } }
        private int m_TotalComponents;  // used determine if mobiles or items were removed in load
        public virtual int EventRange { get { return 10; } }
        // 30 minute standard decay time, Spawner.NextSpawn if on a spawner
        private Timer m_DecayTimer;
        private TimeSpan m_DecayDelay = TimeSpan.FromMinutes(30); // default for stand alone (not on a spawner) camps

        public BaseCamp(int multiID)
            : base(multiID | 0x4000)
        {
            m_Items = new ArrayList();
            m_Mobiles = new ArrayList();
            m_TotalComponents = 0;
            StartHeartbeat(true);

            Timer.DelayCall(TimeSpan.Zero, new TimerCallback(AddComponents));
        }
        private void SetBool(CampBools flag, bool value)
        {
            if (value)
                m_BoolTable |= flag;
            else
                m_BoolTable &= ~flag;
        }

        private bool GetBool(CampBools flag)
        {
            return ((m_BoolTable & flag) != 0);
        }
        public virtual TimeSpan DecayDelay
        {
            get
            {
                if (Spawner != null && !Spawner.Deleted)
                {
                    return m_DecayDelay = Utility.DeltaTime(DateTime.UtcNow, Spawner.MinDelay, Spawner.MaxDelay);
                }
                else
                    // default for standalone camps is 30 minutes
                    return m_DecayDelay;
            }
            set { m_DecayDelay = value; }
        }
        public virtual void AddStandardComponents()
        {
            foreach (var u in OrcLizardRatComponents)
                AddItem(new Static((int)u.m_ItemID), u.m_OffsetX, u.m_OffsetY, u.m_OffsetZ);
        }
        public bool ReplaceComponent(List<MultiTileEntry> list, ushort existingComponent, ushort newComponent)
        {
            List<MultiTileEntry> found = new();
            foreach (MultiTileEntry entry in list)
            {
                if (entry.m_ItemID == existingComponent)
                {
                    found.Add(entry);
                    break;
                }
            }
            foreach (var mte in found)
            {
                list.Remove(mte);
                list.Add(new MultiTileEntry(newComponent, mte.m_OffsetX, mte.m_OffsetY, mte.m_OffsetZ, mte.m_Flags));
            }
            return found.Count > 0;
        }
        public bool ReplaceComponentWithItem(List<MultiTileEntry> list, ushort existingComponent, Item newComponent)
        {
            List<MultiTileEntry> found = new();
            foreach (MultiTileEntry entry in list)
            {
                if (entry.m_ItemID == existingComponent)
                {
                    found.Add(entry);
                    break;
                }
            }
            foreach (var mte in found)
            {
                list.Remove(mte);
                AddItem(newComponent, mte.m_OffsetX, mte.m_OffsetY, mte.m_OffsetZ);
            }
            return found.Count > 0;
        }
        public virtual void AddComponents()
        {
        }
        public virtual void StartHeartbeat(bool setDecayTime)
        {
            if (Deleted)
                return;

            // one minute heart beat
            Timer.DelayCall(TimeSpan.FromMinutes(1), new TimerCallback(Heartbeat));
        }
        private void Heartbeat()
        {
            if (Deleted)
                return;

            // if the camp is corrupted, stop the heartbeat and start a delete timer.
            if (m_DecayTimer == null && CampCorrupted())
            {
                m_DecayTimer = Timer.DelayCall(DecayDelay, new TimerCallback(Delete));
                Utility.Monitor.DebugOut("Starting delete timer", ConsoleColor.DarkRed);
            }
            else if (m_DecayTimer == null)
                // one minute heart beat
                Timer.DelayCall(TimeSpan.FromMinutes(1), new TimerCallback(Heartbeat));
        }
        public virtual bool CampCorrupted()
        {
            // stealing camp component will also set Corrupted == true
            if (GetBool(CampBools.Corrupted) == false)
            {
                // item deleted or chest unlocked?
                if (m_Items != null)
                    foreach (Item item in m_Items)
                    {
                        if (item == null || item.Deleted)
                            SetBool(CampBools.Corrupted, true);
                        else if (item is LockableContainer lc && lc.Locked == false)
                            SetBool(CampBools.Corrupted, true);
                    }

                // mobile deleted or escorted?
                if (m_Mobiles != null)
                    foreach (Mobile mob in m_Mobiles)
                        if (mob != null && mob.Deleted || mob is BaseEscortable be && be.GetEscorterWithSideEffects() != null)
                            SetBool(CampBools.Corrupted, true);

                // mismatch?
                // this happens if we save the server with a corrupted camp. On reload, those deleted objects
                //  won't be reloaded, so the above tests will fail to show the discrepancy.
                if (m_Mobiles.Count + m_Items.Count != m_TotalComponents)
                    SetBool(CampBools.Corrupted, true);

                if (GetBool(CampBools.Corrupted))
                    Utility.Monitor.DebugOut("corrupted", ConsoleColor.DarkRed);
            }

            return GetBool(CampBools.Corrupted);
        }
        public override void OnAfterStolen(Mobile from)
        {
            base.OnAfterStolen(from);
            SetBool(CampBools.Corrupted, true);
            return;
        }
        public virtual void OnDeath(Mobile killed)
        {
            return;
        }
        public virtual void AddItem(Item item, int xOffset, int yOffset, int zOffset)
        {
            m_Items.Add(item);
            m_TotalComponents++;
            item.BaseCamp = this;
            if (item is DungeonTreasureChest dtc) dtc.Lifespan = TimeSpan.FromHours(3000);

            Point3D loc = new Point3D(X + xOffset, Y + yOffset, Z + zOffset);
            item.MoveToWorld(loc, Map);
        }
        public override void AddItem(Item item)
        {
            m_Items.Add(item);
            m_TotalComponents++;
            item.BaseCamp = this;
            if (item is DungeonTreasureChest dtc) dtc.Lifespan = TimeSpan.FromHours(3000);
        }
        public virtual void AddMobile(Mobile m, int wanderRange, int xOffset, int yOffset, int zOffset)
        {
            m_Mobiles.Add(m);
            m_TotalComponents++;
            m.BaseCamp = this;
            // we don't want our 'death star' deleting camp mobiles. The camp will handle that
            //  I'm afraid TimeSpan.MaxValue will overflow when added to DateTime.UtcNow, so we'll go with just over 3 months
            if (m is BaseCreature mob) mob.Lifespan = TimeSpan.FromHours(3000);

            Point3D loc = new Point3D(X + xOffset, Y + yOffset, Z + zOffset);
            BaseCreature bc = m as BaseCreature;

            if (bc != null)
            {
                bc.RangeHome = wanderRange;
                bc.Home = loc;
            }

            if (m is BaseVendor || m is Banker)
                m.Direction = Direction.South;

            if (Utility.CanSpawnLandMobile(this.Map, loc))
                m.MoveToWorld(loc, this.Map);
            else
                m.MoveToWorld(Spawner.GetSpawnPosition(this.Map, Location/*loc*/, homeRange: 2, SpawnFlags.None, m), this.Map);

            if (bc != null)
                bc.DeactivateIfAppropriate();
        }
        public virtual void OnEnter(Mobile m)
        {
        }
        public virtual void OnExit(Mobile m)
        {
        }
        public override bool HandlesOnMovement { get { return true; } }
        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            bool inOldRange = Utility.InRange(oldLocation, Location, EventRange);
            bool inNewRange = Utility.InRange(m.Location, Location, EventRange);

            if (inNewRange && !inOldRange)
                OnEnter(m);
            else if (inOldRange && !inNewRange)
                OnExit(m);
        }
        public bool StealCampItem(Item item)
        {
            bool contains = m_Items.Contains(item);
            if (contains)
                m_Items.Remove(item);
            return contains;
        }
        public override void OnAfterDelete()
        {
            Utility.Monitor.DebugOut("OnAfterDelete", ConsoleColor.DarkRed);
            base.OnAfterDelete();

            for (int i = 0; i < m_Items.Count; ++i)
            {
                Item item = m_Items[i] as Item;
                if (item != null && item.Deleted == false)
                {   // it's movable
                    if (item.Movable == true)
                        Utility.Monitor.DebugOut("Ignoring stolen item {0}", ConsoleColor.DarkRed, item);
                    else
                    {
                        Utility.Monitor.DebugOut("Deleting {0}", ConsoleColor.DarkRed, item);
                        item.Delete();
                    }
                }
                else
                {
                    Utility.Monitor.DebugOut("No item to delete", ConsoleColor.DarkRed);
                }
            }

            for (int i = 0; i < m_Mobiles.Count; ++i)
            {
                Mobile m = m_Mobiles[i] as Mobile;
                if (m != null && m.Deleted == false)
                {
                    if (m is BaseEscortable be && be.GetEscorterWithSideEffects() != null)
                    {
                        Utility.Monitor.DebugOut("Ignoring Escortable {0}", ConsoleColor.DarkRed, m);
                        continue;
                    }

                    Utility.Monitor.DebugOut("Deleting {0}", ConsoleColor.DarkRed, m);
                    m.Delete();
                }
                else
                {
                    Utility.Monitor.DebugOut("No mobile to delete", ConsoleColor.DarkRed);
                }
            }

            m_Items.Clear();
            m_Mobiles.Clear();
            m_TotalComponents = 0;

            Utility.Monitor.DebugOut("Component cleanup complete", ConsoleColor.DarkRed);
        }
        public BaseCamp(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 4;
            writer.Write(version); // version

            // version 3
            writer.Write((byte)m_BoolTable);

            // version 2 - eliminated in version 4
            //writer.Write(m_Spawner);

            // version 1
            writer.Write(m_TotalComponents);

            // version 0
            writer.WriteItemList(m_Items, true);
            writer.WriteMobileList(m_Mobiles, true);
            // removed in version 1
            //writer.WriteDeltaTime(m_DecayTime);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 4:
                    {
                        // eliminate private copy of spawner. Now handled in Item
                        goto case 3;
                    }
                case 3:
                    {
                        m_BoolTable = (CampBools)reader.ReadByte();
                        goto case 2;
                    }
                case 2:
                    {
                        if (version < 4)
                            base.Spawner = (Spawner)reader.ReadItem();
                        goto case 1;
                    }
                case 1:
                    {
                        m_TotalComponents = reader.ReadInt();
                        m_Items = reader.ReadItemList();
                        m_Mobiles = reader.ReadMobileList();
                        break;
                    }
                case 0:
                    {
                        m_Items = reader.ReadItemList();
                        m_Mobiles = reader.ReadMobileList();
                        reader.ReadDeltaTime();
                        break;
                    }
            }

            StartHeartbeat(false);
        }
        public static void Initialize()
        {
            Server.CommandSystem.Register("MakeCamp", AccessLevel.Administrator, new CommandEventHandler(MakeCamp));
            Server.CommandSystem.Register("LoadCamp", AccessLevel.Administrator, new CommandEventHandler(LoadCamp));
        }
        [Usage("MakeCamp <id> <key-value-pairs: name, items, statics, origin, sort, picker, caching, folder>")]
        [Description("Exports the bounded items/statics as addon to XML.")]
        public static void MakeCamp(CommandEventArgs e)
        {
            string options = "GolumCamp folder XmlAddon/Camps origin center caching false flash true";
            string[] arguments = options.Split();
            e = new CommandEventArgs(e.Mobile, "", "", arguments);
            e.Mobile.SendMessage("Select the area to capture...");
            XmlAddonSystem.XmlAddon_OnCommand(e);
        }
        public static void LoadCamp(CommandEventArgs e)
        {
            AddonData AddonData = XmlAddonSystem.GetData(Path.Combine(Core.DataDirectory, "XmlAddon/Camps"), "GolumCamp");
        }

    }
}