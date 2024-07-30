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

/* Multis/Boats/Hold.cs
 * ChangeLog:
 *	5/29/10, adam
 *		Make holds lockable by replaccing the base class BaseContainer with LockableContainer
 *		To accomplish this, see OneTimeBoatUpgrade() below and the comments in main.c for startup switch -BoatHoldUpgrade
 *  05/01/06 Taran Kain
 *		Added MaxItems override to differentiate between boat sizes. I also did the MaxWeight thing hella long ago,
 *		apparently I didn't document it.
 *  02/17/05, mith
 *		Changed base object from Container to BaseContainer to include fixes made there that override
 *			functionality in the core.
 */

using Server.Multis;
using Server.Network;
using System;
using System.Collections;

namespace Server.Items
{
    // adam, make lockable
    public class Hold : LockableContainer //BaseContainer
    {
        private BaseBoat m_Boat;
        public BaseBoat Boat { get { return m_Boat; } set { m_Boat = value; } }

        public override int MaxWeight
        {
            get
            {
                if (m_Boat is SmallBoat || m_Boat is SmallDragonBoat)
                    return 1200;
                if (m_Boat is MediumBoat || m_Boat is MediumDragonBoat)
                    return 2400;
                if (m_Boat is LargeBoat || m_Boat is LargeDragonBoat)
                    return 4800;
                return 0; // catch-all, player will definitely report this and erroneous boat will be found
            }
        }

        public override int MaxItems
        {
            get
            {
                if (m_Boat is SmallBoat || m_Boat is SmallDragonBoat)
                    return 125;
                if (m_Boat is MediumBoat || m_Boat is MediumDragonBoat)
                    return 250;
                if (m_Boat is LargeBoat || m_Boat is LargeDragonBoat)
                    return 375;
                return 0; // catch-all, player will definitely report this and erroneous boat will be found
            }
        }

        public override int DefaultGumpID { get { return 0x4C; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(46, 74, 150, 110); }
        }

        public Hold(BaseBoat boat)
            : base(0x3EAE)
        {
            m_Boat = boat;
            Movable = false;
        }

        public Hold(Serial serial)
            : base(serial)
        {
        }

        public void SetFacing(Direction dir)
        {
            switch (dir)
            {
                case Direction.East: ItemID = 0x3E65; break;
                case Direction.West: ItemID = 0x3E93; break;
                case Direction.North: ItemID = 0x3EAE; break;
                case Direction.South: ItemID = 0x3EB9; break;
            }
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            if (m_Boat == null || !m_Boat.Contains(from) || m_Boat.IsMoving)
                return false;

            return base.OnDragDrop(from, item);
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (m_Boat == null || !m_Boat.Contains(from) || m_Boat.IsMoving)
                return false;

            return base.OnDragDropInto(from, item, p);
        }

        public override bool CheckItemUse(Mobile from, Item item)
        {
            if (m_Boat == null || !m_Boat.Contains(from) || m_Boat.IsMoving)
                return false;

            return base.CheckItemUse(from, item);
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            if (m_Boat == null || !m_Boat.Contains(from) || m_Boat.IsMoving)
                return false;

            return base.CheckLift(from, item, ref reject);
        }

        public override void OnAfterDelete()
        {
            if (m_Boat != null)
                m_Boat.Delete();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Boat == null || !m_Boat.Contains(from))
            {
                if (m_Boat.TillerMan != null)
                    m_Boat.TillerMan.Say(502490); // You must be on the ship to open the hold.
            }
            else if (m_Boat.IsMoving)
            {
                if (m_Boat.TillerMan != null)
                    m_Boat.TillerMan.Say(502491); // I can not open the hold while the ship is moving.
            }
            else
            {
                base.OnDoubleClick(from);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

            writer.Write(m_Boat);
        }

        private bool OneTimeBoatUpgrade(GenericReader reader)
        {
            // only do this one time, or for any world reverts prior to 5/29/10
            //	to repatch boat holds start the server with the -BoatHoldUpgrade switch
            if (Core.BoatHoldUpgrade == true)
            {
                OldHold h = new OldHold(new SmallBoat());   // dummy hold
                h.Deserialize(reader);                      // read the stuffs off disk
                                                            // patch it!
                Utility.CopyProperties(this, h);
                this.Boat = h.Boat;
                uint keyValue = 0;
                if (this.Boat != null && h.Boat.PPlank != null)
                    keyValue = h.Boat.PPlank.KeyValue;
                else if (this.Boat != null && h.Boat.SPlank != null)
                    keyValue = h.Boat.SPlank.KeyValue;
                this.KeyValue = keyValue;

                // move items from old hold to new hold
                if (h.Items != null && h.Items.Count > 0)
                {
                    ArrayList list = new ArrayList();
                    for (int ix = 0; ix < h.Items.Count; ix++)
                    {
                        list.Add(h.Items[ix]);
                    }

                    for (int jx = 0; jx < list.Count; jx++)
                    {
                        h.RemoveItem(list[jx] as Item);
                        this.AddItem(list[jx] as Item);
                    }
                }


                // kill old boat hold
                h.Movable = true;
                h.Boat = null;
                h.Delete();
                h = null;
                return true;
            }

            return false;
        }

        public override void Deserialize(GenericReader reader)
        {
            // only do this one time, or for any world reverts prior to 5/29/10
            //	to repatch boat holds start the server with the -BoatHoldUpgrade switch
            if (OneTimeBoatUpgrade(reader) == false)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {
                            m_Boat = reader.ReadItem() as BaseBoat;

                            if (m_Boat == null || Parent != null)
                                Delete();

                            Movable = false;

                            break;
                        }

                    default:
                        Console.WriteLine("You must launch the server one-time with the -BoatHoldUpgrade switch to upgrade boats greated prior to 5/29/10.");
                        throw (new ApplicationException("You must launch the server one-time with the -BoatHoldUpgrade switch"));
                        //break;
                }
            }
        }
    }

    ////////////////////////////////////////////////// DELETE BELOW /////////////////////////////////////////////////////////
    // adam, make lockable
    public class OldHold : BaseContainer
    {
        private BaseBoat m_Boat;
        public BaseBoat Boat { get { return m_Boat; } set { m_Boat = value; } }

        public override int MaxWeight
        {
            get
            {
                if (m_Boat is SmallBoat || m_Boat is SmallDragonBoat)
                    return 1200;
                if (m_Boat is MediumBoat || m_Boat is MediumDragonBoat)
                    return 2400;
                if (m_Boat is LargeBoat || m_Boat is LargeDragonBoat)
                    return 4800;
                return 0; // catch-all, player will definitely report this and erroneous boat will be found
            }
        }

        public override int MaxItems
        {
            get
            {
                if (m_Boat is SmallBoat || m_Boat is SmallDragonBoat)
                    return 125;
                if (m_Boat is MediumBoat || m_Boat is MediumDragonBoat)
                    return 250;
                if (m_Boat is LargeBoat || m_Boat is LargeDragonBoat)
                    return 375;
                return 0; // catch-all, player will definitely report this and erroneous boat will be found
            }
        }

        public override int DefaultGumpID { get { return 0x4C; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(46, 74, 150, 110); }
        }

        public OldHold(BaseBoat boat)
            : base(0x3EAE)
        {
            m_Boat = boat;
            Movable = false;
        }

        public OldHold(Serial serial)
            : base(serial)
        {
        }

        public void SetFacing(Direction dir)
        {
            switch (dir)
            {
                case Direction.East: ItemID = 0x3E65; break;
                case Direction.West: ItemID = 0x3E93; break;
                case Direction.North: ItemID = 0x3EAE; break;
                case Direction.South: ItemID = 0x3EB9; break;
            }
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            if (m_Boat == null || !m_Boat.Contains(from) || m_Boat.IsMoving)
                return false;

            return base.OnDragDrop(from, item);
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (m_Boat == null || !m_Boat.Contains(from) || m_Boat.IsMoving)
                return false;

            return base.OnDragDropInto(from, item, p);
        }

        public override bool CheckItemUse(Mobile from, Item item)
        {
            if (m_Boat == null || !m_Boat.Contains(from) || m_Boat.IsMoving)
                return false;

            return base.CheckItemUse(from, item);
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            if (m_Boat == null || !m_Boat.Contains(from) || m_Boat.IsMoving)
                return false;

            return base.CheckLift(from, item, ref reject);
        }

        public override void OnAfterDelete()
        {
            if (m_Boat != null)
                m_Boat.Delete();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Boat == null || !m_Boat.Contains(from))
            {
                if (m_Boat.TillerMan != null)
                    m_Boat.TillerMan.Say(502490); // You must be on the ship to open the hold.
            }
            else if (m_Boat.IsMoving)
            {
                if (m_Boat.TillerMan != null)
                    m_Boat.TillerMan.Say(502491); // I can not open the hold while the ship is moving.
            }
            else
            {
                base.OnDoubleClick(from);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

            writer.Write(m_Boat);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Boat = reader.ReadItem() as BaseBoat;

                        if (m_Boat == null || Parent != null)
                            Delete();

                        Movable = false;

                        break;
                    }
            }
        }
    }
    ////////////////////////////////////////////////// DELETE ABOVE /////////////////////////////////////////////////////////
}