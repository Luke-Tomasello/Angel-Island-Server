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

/* Items/Construction/Doors/BaseDoor.cs
 * ChangeLog:
 *  2/29/2024, Adam: Don't let staff refresh players house in this way (double clicking door) (unless it is their own house.)
 *  1/2/2021, Adam: disallow non-friends of a private house from using auto open/close macros on doors.
 *      ClassicUO's auto-open doors and Smooth Doors allow a non-friend of a private house to, what amounts to, 
 *      run right through an open door (not the entryway, the actual door.) This is accomplished with
 *      and EventSink_OpenDoorMacroUsed(). while arguably 'legal' it's not really how I want to see the system
 *      used/abused.
 *      Update: removing Smooth Door exploit code as we now have a real fix.
 *  12/24/07, Adam
 *  public override void OnDoubleClick( Mobile from )
 *      Looks like the RunUO guys had a LOS check here but commented it out. 
 *      We found that certain new houses allowed a locked door to be opened from the patio which was clearly LOS == false
 *      I believe the reason it was commented out was that LOS fails when you try to access the OPEN door.
 *      Resolution: Reinstate the LOS check but make it only for CLOSED doors .. this may restrict some old-school behaviors.
 *  9/03/06 Taran Kain
 *		Moved DoorFacing enum definition to this file.
 *  9/02/06 Taran Kain
 *		Fixed bug with closing door that'd allow you to close it on top of you.
 *  9/01/06 Taran Kain
 *		Abstracted OpenedID, ClosedID, Offset to depend on BaseDoor.Facing
 *		Promoted Facing up from BaseHouseDoor
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *   4/26/2004, pixie
 *     Changed to check pack for keyring containing the proper key
 *     to let the player open the locked door.
 * 
 */

using Server.Diagnostics;
using Server.Multis;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    public enum DoorFacing
    {
        WestCW,
        EastCCW,
        WestCCW,
        EastCW,
        SouthCW,
        NorthCCW,
        SouthCCW,
        NorthCW
    }

    public abstract class BaseDoor : Item, ILockable, ITelekinesisable
    {
        private bool m_Open, m_Locked, m_RustyLock;
        private int m_OpenedID, m_CustomOpenedID, m_OpenedSound;
        private int m_ClosedID, m_CustomClosedID, m_ClosedSound;
        private Point3D m_Offset;
        private DoorFacing m_Facing;
        private BaseDoor m_Link;
        private uint m_KeyValue;

        private Timer m_Timer;

        private static Point3D[] m_Offsets = new Point3D[]
            {
                new Point3D(-1, 1, 0 ),
                new Point3D( 1, 1, 0 ),
                new Point3D(-1, 0, 0 ),
                new Point3D( 1,-1, 0 ),
                new Point3D( 1, 1, 0 ),
                new Point3D( 1,-1, 0 ),
                new Point3D( 0, 0, 0 ),
                new Point3D( 0,-1, 0 )
            };

        // Called by RunUO
        public static void Initialize()
        {
            EventSink.OpenDoorMacroUsed += new OpenDoorMacroEventHandler(EventSink_OpenDoorMacroUsed);
            CommandSystem.Register("Link", AccessLevel.GameMaster, new CommandEventHandler(Link_OnCommand));
            CommandSystem.Register("ChainLink", AccessLevel.GameMaster, new CommandEventHandler(ChainLink_OnCommand));
        }

        [Usage("Link")]
        [Description("Links two targeted doors together.")]
        private static void Link_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(Link_OnFirstTarget));
            e.Mobile.SendMessage("Target the first door to link.");
        }

        private static void Link_OnFirstTarget(Mobile from, object targeted)
        {
            BaseDoor door = targeted as BaseDoor;

            if (door == null)
            {
                from.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(Link_OnFirstTarget));
                from.SendMessage("That is not a door. Try again.");
            }
            else
            {
                from.BeginTarget(-1, false, TargetFlags.None, new TargetStateCallback(Link_OnSecondTarget), door);
                from.SendMessage("Target the second door to link.");
            }
        }

        private static void Link_OnSecondTarget(Mobile from, object targeted, object state)
        {
            BaseDoor first = (BaseDoor)state;
            BaseDoor second = targeted as BaseDoor;

            if (second == null)
            {
                from.BeginTarget(-1, false, TargetFlags.None, new TargetStateCallback(Link_OnSecondTarget), first);
                from.SendMessage("That is not a door. Try again.");
            }
            else
            {
                first.Link = second;
                second.Link = first;
                from.SendMessage("The doors have been linked.");
            }
        }

        [Usage("ChainLink")]
        [Description("Chain-links two or more targeted doors together.")]
        private static void ChainLink_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetStateCallback(ChainLink_OnTarget), new ArrayList());
            e.Mobile.SendMessage("Target the first of a sequence of doors to link.");
        }

        private static void ChainLink_OnTarget(Mobile from, object targeted, object state)
        {
            BaseDoor door = targeted as BaseDoor;

            if (door == null)
            {
                from.BeginTarget(-1, false, TargetFlags.None, new TargetStateCallback(ChainLink_OnTarget), state);
                from.SendMessage("That is not a door. Try again.");
            }
            else
            {
                ArrayList list = (ArrayList)state;

                if (list.Count > 0 && list[0] == door)
                {
                    if (list.Count >= 2)
                    {
                        for (int i = 0; i < list.Count; ++i)
                            ((BaseDoor)list[i]).Link = ((BaseDoor)list[(i + 1) % list.Count]);

                        from.SendMessage("The chain of doors have been linked.");
                    }
                    else
                    {
                        from.BeginTarget(-1, false, TargetFlags.None, new TargetStateCallback(ChainLink_OnTarget), state);
                        from.SendMessage("You have not yet targeted two unique doors. Target the second door to link.");
                    }
                }
                else if (list.Contains(door))
                {
                    from.BeginTarget(-1, false, TargetFlags.None, new TargetStateCallback(ChainLink_OnTarget), state);
                    from.SendMessage("You have already targeted that door. Target another door, or retarget the first door to complete the chain.");
                }
                else
                {
                    list.Add(door);

                    from.BeginTarget(-1, false, TargetFlags.None, new TargetStateCallback(ChainLink_OnTarget), state);

                    if (list.Count == 1)
                        from.SendMessage("Target the second door to link.");
                    else
                        from.SendMessage("Target another door to link. To complete the chain, retarget the first door.");
                }
            }
        }

        private static void EventSink_OpenDoorMacroUsed(OpenDoorMacroEventArgs args)
        {
            Mobile m = args.Mobile;

            if (m.Map != null)
            {
                int x = m.X, y = m.Y;

                switch (m.Direction & Direction.Mask)
                {
                    case Direction.North: --y; break;
                    case Direction.Right: ++x; --y; break;
                    case Direction.East: ++x; break;
                    case Direction.Down: ++x; ++y; break;
                    case Direction.South: ++y; break;
                    case Direction.Left: --x; ++y; break;
                    case Direction.West: --x; break;
                    case Direction.Up: --x; --y; break;
                }

                Sector sector = m.Map.GetSector(x, y);

                foreach (Item item in sector.Items)
                {
                    if (item == null)
                        continue;

                    if (item.Location.X == x && item.Location.Y == y && (item.Z + item.ItemData.Height) > m.Z && (m.Z + 16) > item.Z && item is BaseDoor && m.CanSee(item) && m.InLOS(item))
                    {
                        // 1/2/2021, Adam: disallow non-friends of a private house from using auto open/close macros on doors.
                        //  See also: change log at the top of this file.
                        /*if (item is BaseDoor bd && BaseHouse.FindHouseAt(bd) != null && 
                            !BaseHouse.FindHouseAt(bd).Public && !BaseHouse.FindHouseAt(bd).IsFriend(m))
                            break;*/
                        if (m.CheckAlive())
                        {
                            m.SendLocalizedMessage(500024); // Opening door...
                            item.OnDoubleClick(m);
                        }

                        break;
                    }
                }
            }
        }

        public static Point3D GetOffset(DoorFacing facing)
        {
            return m_Offsets[(int)facing];
        }

        private class InternalTimer : Timer
        {
            private BaseDoor m_Door;

            public InternalTimer(BaseDoor door)
                : base(TimeSpan.FromSeconds(20.0), TimeSpan.FromSeconds(10.0))
            {
                Priority = TimerPriority.OneSecond;
                m_Door = door;
            }

            protected override void OnTick()
            {
                if (m_Door.Open && m_Door.IsFreeToClose())
                    m_Door.Open = false;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DoorFacing Facing
        {
            get
            {
                return m_Facing;
            }
            set
            {
                Open = false;

                m_Facing = value;

                ResetDisplay();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Locked
        {
            get
            {
                return m_Locked;
            }
            set
            {
                m_Locked = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RustyLock
        {
            get
            {
                return m_RustyLock;
            }
            set
            {
                m_RustyLock = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public uint KeyValue
        {
            get
            {
                return m_KeyValue;
            }
            set
            {
                m_KeyValue = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Open
        {
            get
            {
                return m_Open;
            }
            set
            {
                if (m_Open != value)
                {
                    m_Open = value;

                    ResetDisplay();

                    if (m_Open)
                        Location += Offset;
                    else
                        Location -= Offset;

                    Effects.PlaySound(this, Map, m_Open ? m_OpenedSound : m_ClosedSound);

                    if (m_Open)
                        m_Timer.Start();
                    else
                    {
                        m_Timer.Stop();
                    }
                }
            }
        }

        private void ResetDisplay()
        {
            if (m_Open)
                ItemID = OpenedID;
            else
                ItemID = ClosedID;
        }

        public bool CanClose()
        {
            if (!m_Open)
                return true;

            Map map = Map;

            if (map == null)
                return false;

            Point3D p = Location - Offset;

            return CheckFit(map, p, 16);
        }

        private bool CheckFit(Map map, Point3D p, int height)
        {
            if (map == Map.Internal)
                return false;

            int x = p.X;
            int y = p.Y;
            int z = p.Z;

            Sector sector = map.GetSector(x, y);
            List<Item> items = sector.Items;
            List<Mobile> mobs = sector.Mobiles;

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = items[i];

                if (!(item is BaseMulti) && item.ItemID <= TileData.MaxItemValue && item.AtWorldPoint(x, y) && !(item is BaseDoor))
                {
                    ItemData id = item.ItemData;
                    bool surface = id.Surface;
                    bool impassable = id.Impassable;

                    if ((surface || impassable) && (item.Z + id.CalcHeight) > z && (z + height) > item.Z)
                        return false;
                }
            }

            for (int i = 0; i < mobs.Count; ++i)
            {
                Mobile m = mobs[i];

                if (m.Location.X == x && m.Location.Y == y)
                {
                    if (m.Hidden && m.AccessLevel > AccessLevel.Player)
                        continue;

                    if (!m.Alive)
                        continue;

                    if ((m.Z + 16) > z && (z + height) > m.Z)
                        return false;
                }
            }

            return true;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OpenedID
        {
            get
            {
                if (m_CustomOpenedID != -1)
                    return m_CustomOpenedID;

                return m_OpenedID + (2 * (int)Facing);
            }
            set
            {
                m_OpenedID = value;

                ResetDisplay();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ClosedID
        {
            get
            {
                if (m_CustomClosedID != -1)
                    return m_CustomClosedID;

                return m_ClosedID + (2 * (int)Facing);
            }
            set
            {
                m_ClosedID = value;

                ResetDisplay();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CustomClosedID
        {
            get
            {
                return m_CustomClosedID;
            }
            set
            {
                m_CustomClosedID = value;

                ResetDisplay();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CustomOpenedID
        {
            get
            {
                return m_CustomOpenedID;
            }
            set
            {
                m_CustomOpenedID = value;

                ResetDisplay();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OpenedSound
        {
            get
            {
                return m_OpenedSound;
            }
            set
            {
                m_OpenedSound = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ClosedSound
        {
            get
            {
                return m_ClosedSound;
            }
            set
            {
                m_ClosedSound = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Offset
        {
            get
            {
                if (m_Offset != Point3D.Zero)
                    return m_Offset;

                return GetOffset(Facing);
            }
            set
            {
                m_Offset = value;

                ResetDisplay();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseDoor Link
        {
            get
            {
                if (m_Link != null && m_Link.Deleted)
                    m_Link = null;

                return m_Link;
            }
            set
            {
                m_Link = value;
            }
        }

        public virtual bool UseChainedFunctionality { get { return false; } }

        public ArrayList GetChain()
        {
            ArrayList list = new ArrayList();
            BaseDoor c = this;

            do
            {
                list.Add(c);
                c = c.Link;
            } while (c != null && !list.Contains(c));

            return list;
        }

        public bool IsFreeToClose()
        {
            if (!UseChainedFunctionality)
                return CanClose();

            ArrayList list = GetChain();

            bool freeToClose = true;

            for (int i = 0; freeToClose && i < list.Count; ++i)
                freeToClose = ((BaseDoor)list[i]).CanClose();

            return freeToClose;
        }

        public void OnTelekinesis(Mobile from)
        {
            Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x376A, 9, 32, 5022);
            Effects.PlaySound(Location, Map, 0x1F5);

            Use(from);
        }

        public virtual bool IsInside(Mobile from)
        {
            // 5/29/2021. Adam
            // This used to simply return false and left implementation to the derived class.
            //	But when the door in part of a static structure, the player can get locked inside (see the guard tower on Angel Island)
            //	We could make the guard tower a 'house', or a separate region of something, but this seems more straight forward.
            return BaseDoor.CalcInside(this, from);
        }
        public virtual void FailUnlockMessage(Mobile from, int number)
        {
            from.SendLocalizedMessage(number);
        }
        // only says if the mobile is inside the house (not if it's an inside door.)
        public static bool CalcInside(BaseDoor bd, Mobile from)
        {

            int x, y, w, h;

            const int r = 2;
            const int bs = r * 2 + 1;
            const int ss = r + 1;

            switch (bd.Facing)
            {
                case DoorFacing.WestCW:
                case DoorFacing.EastCCW: x = -r; y = -r; w = bs; h = ss; break;

                case DoorFacing.EastCW:
                case DoorFacing.WestCCW: x = -r; y = 0; w = bs; h = ss; break;

                case DoorFacing.SouthCW:
                case DoorFacing.NorthCCW: x = -r; y = -r; w = ss; h = bs; break;

                case DoorFacing.NorthCW:
                case DoorFacing.SouthCCW: x = 0; y = -r; w = ss; h = bs; break;

                default: return false;
            }

            int rx = from.X - bd.X;
            int ry = from.Y - bd.Y;
            int az = Math.Abs(from.Z - bd.Z);

            return (rx >= x && rx < (x + w) && ry >= y && ry < (y + h) && az <= 4);
            // return false;
        }

        public virtual bool UseLocks()
        {
            return true;
        }

        public virtual void Use(Mobile from)
        {
            //Pix: 2010.11.21 - not sure if this is a great place for this, but on UOSP, when a friend uses a house door, the house is refreshed
            if (Core.RuleSets.SiegeStyleRules())
            {
                try
                {
                    BaseHouse house = BaseHouse.FindHouseAt(this);
                    if (house != null)
                    {   // 2/29/2024, Adam: Don't let staff refresh players house in this way (unless it is their own house.)
                        if ((house.IsFriend(from) && from.AccessLevel == AccessLevel.Player) || from == house.Owner)
                        {
                            double dms = house.DecayMinutesStored;
                            house.Refresh();

                            //if we're more than one day (less than 14 days) from the max stored (15 days), 
                            //then tell the friend that the house is refreshed
                            if (dms < TimeSpan.FromDays(14.0).TotalMinutes)
                            {
                                from.SendMessage("You refresh the house.");
                                LogHelper Logger = new LogHelper("HouseRefresh.log", overwrite: false, sline: true);
                                Logger.Log(LogType.Mobile, from, string.Format("Refreshed the house {0}", house));
                                Logger.Finish();
                            }
                        }
                    }
                }
                catch { }
            }

            if (m_Locked && !m_Open && UseLocks())
            {
                if (from.AccessLevel >= AccessLevel.GameMaster)
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502502); // That is locked, but you open it with your godly powers.
                                                                                   //from.Send( new MessageLocalized( Serial, ItemID, MessageType.Regular, 0x3B2, 3, 502502, "", "" ) ); // That is locked, but you open it with your godly powers.
                }
                else
                {
                    Container pack = from.Backpack;
                    bool found = false;

                    if (pack != null)
                    {
                        Item[] items = pack.FindItemsByType(typeof(Key));

                        foreach (Key k in items)
                        {
                            if (k.KeyValue == this.KeyValue)
                            {
                                found = true;
                                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501282); // You quickly unlock, open, and relock the door
                                break;
                            }
                        }

                        //didn't find the loose key, look for a keyring with it!
                        if (!found)
                        {
                            Item[] items_keyrings = pack.FindItemsByType(typeof(KeyRing));
                            foreach (KeyRing r in items_keyrings)
                            {
                                if (r.IsKeyOnRing(this.KeyValue))
                                {
                                    found = true;
                                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501282); // You quickly unlock, open, and relock the door
                                    break;
                                }
                            }
                        }
                    }

                    if (!found && IsInside(from))
                    {
                        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501280); // That is locked, but is usable from the inside.
                    }
                    else if (!found)
                    {
                        if (Hue == 0x44E && Map == Map.Malas) // doom door into healer room in doom
                            this.SendLocalizedMessageTo(from, 1060014); // Only the dead may pass.
                        else
                            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502503); // That is locked.

                        return;
                    }
                }
            }

            if (m_Open && !IsFreeToClose())
                return;

            if (m_Open)
                OnClosed(from);
            else
                OnOpened(from);

            if (UseChainedFunctionality)
            {
                bool open = !m_Open;

                ArrayList list = GetChain();

                for (int i = 0; i < list.Count; ++i)
                    ((BaseDoor)list[i]).Open = open;
            }
            else
            {
                Open = !m_Open;

                BaseDoor link = this.Link;

                if (m_Open && link != null && !link.Open)
                    link.Open = true;
            }
        }

        public virtual void OnOpened(Mobile from)
        {
        }

        public virtual void OnClosed(Mobile from)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {   // Adam: Looks like the RunUO guys had a LOS check here but commented it out. 
            //  We found that certain new houses allowed a locked door to be opened from the patio which was clearly LOS == false
            //  I believe the reason it was commented out was that LOS fails when you try to access the OPEN door.
            //  Resolution: Reinstate the LOS check but make it only for CLOSED doors .. this may restrict some old-school behaviors.
            if (from.AccessLevel == AccessLevel.Player && ((!from.InLOS(this) && !this.Open) || !from.InRange(GetWorldLocation(), 2)))
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            else
                Use(from);
        }

        public BaseDoor(int closedID, int openedID, int customClosedID, int customOpenedID, int openedSound, int closedSound, DoorFacing facing, Point3D offset)
            : base(0) // set itemid to 0 temporarily
        {
            m_ClosedID = closedID;
            m_OpenedID = openedID;
            m_CustomClosedID = customClosedID;
            m_CustomOpenedID = customOpenedID;
            m_OpenedSound = openedSound;
            m_ClosedSound = closedSound;
            m_Facing = facing;
            m_Offset = offset;
            m_RustyLock = false;

            m_Timer = new InternalTimer(this);

            ItemID = ClosedID;
            Movable = false;
        }

        public BaseDoor(int closedID, int openedID, int openedSound, int closedSound, DoorFacing facing)
            : this(closedID, openedID, -1, -1, openedSound, closedSound, facing, Point3D.Zero)
        {
        }
        //public override Item Dupe(Item item, int amount)
        //{
        //    BaseDoor new_door = item as BaseDoor;
        //    //new_door.ClosedID = m_ClosedID;
        //    //new_door.OpenedID = m_OpenedID;
        //    //new_door.OpenedSound = m_OpenedSound;
        //    //new_door.ClosedSound = m_ClosedSound;
        //    //new_door.Facing = m_Facing;

        //    new_door.CustomClosedID = m_CustomClosedID;
        //    new_door.CustomClosedID = m_CustomOpenedID;
        //    new_door.Facing = m_Facing;
        //    new_door.KeyValue = m_KeyValue;
        //    new_door.Open = m_Open;
        //    new_door.Locked = m_Locked;
        //    new_door.OpenedID = m_OpenedID;
        //    new_door.ClosedID = m_ClosedID;
        //    new_door.OpenedSound = m_OpenedSound;
        //    new_door.ClosedSound = m_ClosedSound;
        //    new_door.Offset = m_Offset;
        //    new_door.Link = m_Link;

        //    return base.Dupe(new_door, amount);
        //}
        //public override Item Dupe(int amount)
        //{
        //    return base.Dupe(new BaseDoor(m_Facing), amount);
        //}
        public override void CopyProperties(Item item)
        {
            base.CopyProperties(item);
            BaseDoor new_door = item as BaseDoor;
            if (new_door != null)
            {
                new_door.CustomClosedID = this.CustomClosedID;
                new_door.CustomClosedID = this.CustomOpenedID;
                new_door.Facing = this.Facing;
                new_door.KeyValue = this.KeyValue;
                new_door.Open = this.Open;
                new_door.Locked = this.Locked;
                new_door.OpenedID = (this.OpenedID - (2 * (int)this.Facing));
                new_door.ClosedID = (this.ClosedID - (2 * (int)this.Facing));
                new_door.OpenedSound = this.OpenedSound;
                new_door.ClosedSound = this.ClosedSound;
                new_door.Offset = this.Offset;
                new_door.Link = this.Link;
            }
        }
        public BaseDoor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 2
            writer.Write(m_RustyLock);

            // version 1
            writer.Write(m_CustomClosedID);
            writer.Write(m_CustomOpenedID);
            writer.Write((int)m_Facing);

            writer.Write(m_KeyValue);

            writer.Write(m_Open);
            writer.Write(m_Locked);
            writer.Write(m_OpenedID);
            writer.Write(m_ClosedID);
            writer.Write(m_OpenedSound);
            writer.Write(m_ClosedSound);
            writer.Write(m_Offset);
            writer.Write(m_Link);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_RustyLock = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        m_CustomClosedID = reader.ReadInt();
                        m_CustomOpenedID = reader.ReadInt();
                        m_Facing = (DoorFacing)reader.ReadInt();
                        m_KeyValue = reader.ReadUInt();
                        m_Open = reader.ReadBool();
                        m_Locked = reader.ReadBool();
                        m_OpenedID = reader.ReadInt();
                        m_ClosedID = reader.ReadInt();
                        m_OpenedSound = reader.ReadInt();
                        m_ClosedSound = reader.ReadInt();
                        m_Offset = reader.ReadPoint3D();
                        m_Link = reader.ReadItem() as BaseDoor;

                        m_Timer = new InternalTimer(this);

                        if (m_Open)
                            m_Timer.Start();

                        break; // NO FALLTHROUGH
                    }
                case 0:
                    {
                        m_KeyValue = reader.ReadUInt();
                        m_Open = reader.ReadBool();
                        m_Locked = reader.ReadBool();
                        m_OpenedID = reader.ReadInt();
                        m_ClosedID = reader.ReadInt();
                        m_OpenedSound = reader.ReadInt();
                        m_ClosedSound = reader.ReadInt();
                        m_Offset = reader.ReadPoint3D();
                        m_Link = reader.ReadItem() as BaseDoor;

                        m_Timer = new InternalTimer(this);

                        if (m_Open)
                            m_Timer.Start();

                        // bring ver 0 up to ver 1
                        for (int i = 0; i < 8; i++)
                        {
                            if (m_Offset == m_Offsets[i])
                            {
                                m_OpenedID -= 2 * i;
                                m_ClosedID -= 2 * i;
                                m_CustomClosedID = -1;
                                m_CustomOpenedID = -1;
                                m_Offset = Point3D.Zero;
                                m_Facing = (DoorFacing)i;

                                break;
                            }
                        }
                        if (m_Offset != Point3D.Zero)
                        {
                            m_Facing = DoorFacing.EastCCW;
                            m_CustomOpenedID = m_OpenedID;
                            m_CustomClosedID = m_ClosedID;
                        }

                        break;
                    }
            }
        }
    }
}