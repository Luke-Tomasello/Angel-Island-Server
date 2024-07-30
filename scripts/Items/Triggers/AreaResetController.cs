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

/* Scripts\Items\Triggers\AreaResetController.cs
 * CHANGELOG:
 * 	7/22/2024, Adam
 * 		Initial version.
 */

using Server.Mobiles;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items.Triggers
{
    [NoSort]
    public class AreaResetController : CustomRegionControl, ITriggerable
    {
        #region Flags
        ARCBoolTable m_BoolTable;
        [Flags]
        public enum ARCBoolTable : byte
        {
            None=0x00,
            EjectPlayers=0x01,      // eject players from the area
            EmptyContainers=0x02,   // empty all containers
            KillTames=0x04,         // kill all tames
        }
        private void SetBool(ARCBoolTable flag, bool value)
        {
            if (value)
                m_BoolTable |= flag;
            else
                m_BoolTable &= ~flag;
        }
        private bool GetBool(ARCBoolTable flag)
        {
            return ((m_BoolTable & flag) != 0);
        }
        #endregion Flags

        #region Command Properties
        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EjectPlayers { get { return GetBool(ARCBoolTable.EjectPlayers); } set { SetBool(ARCBoolTable.EjectPlayers, value); } }
        private string m_EjectedPlayerMsg;
        [CommandProperty(AccessLevel.GameMaster)]
        public string EjectedPlayerMsg { get { return m_EjectedPlayerMsg; } set { m_EjectedPlayerMsg = value; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EmptyContainers { get { return GetBool(ARCBoolTable.EmptyContainers); } set { SetBool(ARCBoolTable.EmptyContainers, value); } }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool KillTames { get { return GetBool(ARCBoolTable.KillTames); } set { SetBool(ARCBoolTable.KillTames, value); } }
        private Rectangle2D m_EjectRect;
        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D EjectRect
        {
            get { return m_EjectRect; }
            set { m_EjectRect = value; }
        }
        private Map m_EjectMap;
        [CommandProperty(AccessLevel.GameMaster)]
        public Map EjectMap
        {
            get { return m_EjectMap; }
            set { m_EjectMap = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Map AreaMap
        {
            get { return this.CustomRegion.Map; }
            set { this.CustomRegion.Map = value; }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public bool Running
        {
            get { return base.IsRunning; }
            set
            {
                if (value == base.IsRunning)
                    return;

                base.IsRunning = value;
            }
        }
        #endregion Command Properties

        #region Dupe
        public override Item Dupe(int amount)
        {   // when duping a CustomRegionControl, we don't actually want the region itself as it's already
            //  been 'registered' with its own UId.
            // The region carries all the following info, which we will need for our dupe
            AreaResetController new_crc = new();
            if (this.CustomRegion != null)
            {
                Utility.CopyProperties(new_crc.CustomRegion, this.CustomRegion);
                new_crc.CustomRegion.Coords = new(this.CustomRegion.Coords);
            }
            return base.Dupe(new_crc, amount);
        }
        #endregion Dupe

        public override string DefaultName { get { return "Area Reset Controller"; } }
        [Constructable]
        public AreaResetController()
            : base()
        {
            Movable = false;
            Visible = false;
            Running = false;
            if (this.CustomRegion != null)
                this.CustomRegion.Name = DefaultName;
        }
        public override void OnSingleClick(Mobile m)
        {
            base.OnSingleClick(m);
            LabelTo(m, string.Format("({0})", CustomRegion.Map));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel < AccessLevel.Seer)
                from.SendMessage("You do not have access to this.");
            else
                base.OnDoubleClick(from);
        }
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new TriggerSystem.ActivateCME(((ITriggerable)this).CanTrigger(from)));
                list.Add(new TriggerSystem.LinkCME());
            }
        }

        #region ITriggerable

        bool ITriggerable.CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            if (!Running)
                return false;

            #region Belt-and-suspenders

            if (this.CustomRegion == null)
                return false;

            if (this.CustomRegion.Coords == null || this.CustomRegion.Coords.Count == 0)
                return false;

            if (m_BoolTable == ARCBoolTable.None)
                return false;

            if (m_EjectRect.Width + m_EjectRect.Height == 0)
                return false;

            if (m_EjectMap == null || m_EjectMap == Map.Internal)
                return false;

            if (this.CustomRegion.Map == null || this.CustomRegion.Map == Map.Internal)
                return false;

            #endregion Belt-and-suspenders

            return true;
        }
        private uint MobilesRelocated = 0;
        private uint TamesKilled = 0;
        private uint ContainersCleaned = 0;
        private uint ItemsDeleted = 0;
        void ITriggerable.OnTrigger(Mobile from)
        {
            MobilesRelocated = 0;
            TamesKilled = 0;
            ContainersCleaned = 0;
            ItemsDeleted = 0;

            CarryOutObjective();

            if (GetBool(ARCBoolTable.EjectPlayers))
                from.SendMessage(string.Format($"{MobilesRelocated} Mobiles relocated"));
            if (GetBool(ARCBoolTable.KillTames))
                from.SendMessage(string.Format($"{TamesKilled} Tames Killed"));
            if (GetBool(ARCBoolTable.EmptyContainers))
                from.SendMessage(string.Format($"{ContainersCleaned} Containers emptied of {ItemsDeleted} items"));
        }

        #endregion ITriggerable
        private void CarryOutObjective()
        {
            foreach (var item in World.Items.Values)
                DoEmptyContainer(item);

            foreach (var mobile in World.Mobiles.Values)
            {
                DoKillTame(mobile);
                DoEjectPlayer(mobile);
            }
        }
        private void DoEmptyContainer(Item item)
        {
            if (item is Container cont && cont.Parent == null && cont.Map == this.CustomRegion.Map)
                foreach (var rect in this.CustomRegion.Coords)
                    if (rect.Contains(cont.Location))
                    {
                        if (cont.Items != null && cont.Items.Count > 0)
                        {
                            List<Item> list = new List<Item>();
                            foreach (var citem in cont.Items)
                                list.Add(citem);

                            foreach (var litem in list)
                            {
                                cont.RemoveItem(litem);
                                litem.Delete();
                                ItemsDeleted++;
                            }

                            ContainersCleaned++;
                        }
                        return;
                    }
        }
        private void DoKillTame(Mobile m)
        {
            if (m is BaseCreature bc && bc.Map == this.CustomRegion.Map)
                foreach (var rect in this.CustomRegion.Coords)
                    if (rect.Contains(bc.Location))
                    {
                        if (bc.Controlled && bc.ControlMaster != null)
                        {
                            bc.Kill();
                            TamesKilled++;
                        }

                        return;
                    }
        }
        private void DoEjectPlayer(Mobile m)
        {
            if (m is PlayerMobile pm && pm.Map == this.CustomRegion.Map)
                foreach (var rect in this.CustomRegion.Coords)
                    if (rect.Contains(pm.Location))
                    {
                        if (pm.AccessLevel == AccessLevel.Player)
                        {
                            List<Point2D> list = m_EjectRect.PointsInRect2D();
                            Point3D px = Point3D.Zero;
                            for (int ix = 0; ix < list.Count; ix++)
                            {
                                Point2D temp = list[Utility.Random(list.Count)];
                                px = new Point3D(temp.X, temp.Y, m_EjectMap.GetAverageZ(temp.X, temp.Y));
                                if (Utility.CanSpawnLandMobile(m_EjectMap, px))
                                    break;
                            }
                            pm.MoveToWorld(px, m_EjectMap);
                            if (!string.IsNullOrEmpty(m_EjectedPlayerMsg))
                                pm.SendMessage(m_EjectedPlayerMsg);
                            MobilesRelocated++;
                        }
                        return;
                    }
        }
        
        public AreaResetController(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2);

            // version 2
            writer.Write(m_EjectedPlayerMsg);

            // version 1
            writer.Write(m_Event);
            writer.Write((byte)m_BoolTable);
            writer.Write(m_EjectRect);
            writer.Write(m_EjectMap);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_EjectedPlayerMsg = reader.ReadString();
                        goto case 1;
                    }
                case 1:
                    {
                        m_Event = reader.ReadString();
                        m_BoolTable = (ARCBoolTable)reader.ReadByte();
                        m_EjectRect = reader.ReadRect2D();
                        m_EjectMap = reader.ReadMap();
                        break;
                    }
                
            }
        }
    }
}