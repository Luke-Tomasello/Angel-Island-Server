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

/* Items/Construction/Doors/HouseDoors.cs
 * ChangeLog:
 *  3/7/07, Adam
 *      - Make OnChop suitable for derived classes by calling the new virtual Chop() for final processing.
 *      - add virtual Chop() for final chop processing
 *  3/6/07, Adam
 *      - Make BaseHouseDoor IChopable
 *      - Add OnChop() override to process chops. A door takes 3 hits with an axe
 *      - Add OnDelete() override to remove the door from BaseHouse doors list
 *	2/11/07, Pix
 *		Some tower doors weren't turning correctly.
 *		Added fix to 'Flip' to make sure that we're using the correct base IDs, so when a player
 *		uses an interior decorator to turn the doors, the broken doors will get fixed.
 *  10/15/06, Rhiannon
 *		Added Flip() to BaseHouseDoor
 *		Made all house doors flipable 
 *  9/01/06 Taran Kain
 *		Modified constructors to fit new BaseDoor constructor
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Gumps;
using Server.Multis;
using System.Collections;

namespace Server.Items
{
    [Flipable]
    public class MetalHouseDoor : BaseHouseDoor
    {
        [Constructable]
        public MetalHouseDoor(DoorFacing facing)
            : base(0x675, 0x676, -1, -1, 0xEC, 0xF3, facing, Point3D.Zero)
        {
        }

        public MetalHouseDoor(Serial serial)
            : base(serial)
        {
        }

        public override void Flip()
        {
            //Fix for doors that aren't right - it makes sure the base IDs get set correctly.
            this.ClosedID = 0x675; // 1653; 
            this.OpenedID = 0x676; // 1654;

            base.Flip();
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [Flipable]
    public class DarkWoodHouseDoor : BaseHouseDoor
    {
        [Constructable]
        public DarkWoodHouseDoor(DoorFacing facing)
            : base(0x6A5, 0x6A6, -1, -1, 0xEA, 0xF1, facing, Point3D.Zero)
        {
        }

        public DarkWoodHouseDoor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [Flipable]
    public class GenericHouseDoor : BaseHouseDoor
    {
        [Constructable]
        public GenericHouseDoor(DoorFacing facing, int baseItemID, int openedSound, int closedSound)
            : base(baseItemID, baseItemID + 1, -1, -1, openedSound, closedSound, facing, Point3D.Zero)
        {
        }

        public GenericHouseDoor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [FlipableAttribute]
    public abstract class BaseHouseDoor : BaseDoor, ISecurable, IChopable
    {
        private SecureLevel m_Level;
        int Hits = 0;                   // not serialized - reset on server up

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level
        {
            get { return m_Level; }
            set { m_Level = value; }
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);
            SetSecureLevelEntry.AddTo(from, this, list);
        }

        public BaseHouseDoor(int closedID, int openedID, int customClosedID, int customOpenedID, int openedSound, int closedSound, DoorFacing facing, Point3D offset)
            : base(closedID, openedID, customClosedID, customOpenedID, openedSound, closedSound, facing, offset)
        {
            m_Level = SecureLevel.Anyone;
        }

        protected virtual void Chop(Mobile from)
        {
            Effects.PlaySound(GetWorldLocation(), Map, 0x3B3);
            from.SendLocalizedMessage(500461); // You destroy the item.
            Delete();
        }

        public virtual void OnChop(Mobile from)
        {
            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house != null && house.IsOwner(from))
            {
                if (house != BaseHouse.FindHouseAt(from))
                    from.SendLocalizedMessage(502092); // You must be in your house to do this.
                else if (from.InRange(GetWorldLocation(), 3) == false)
                    from.SendLocalizedMessage(500446); // That is too far away.
                else
                {
                    switch (++Hits)
                    {
                        case 1:
                            Effects.PlaySound(GetWorldLocation(), Map, 0x3B3);
                            from.SendMessage("Your axe does great damage to the door.");
                            break;
                        case 2:
                            Effects.PlaySound(GetWorldLocation(), Map, 0x3B3);
                            from.SendMessage("The door begins to fall apart.");
                            break;
                        case 3:
                            // final effects and message played in Chop()
                            Chop(from);
                            break;
                    }
                }

            }
        }

        public override void OnDelete()
        {
            BaseHouse house = BaseHouse.FindHouseAt(this);
            if (house != null)
            {
                house.RemoveDoor(this);
            }

            base.OnDelete();
        }

        public BaseHouse FindHouse()
        {
            Point3D loc;

            if (Open)
                loc = new Point3D(X - Offset.X, Y - Offset.Y, Z - Offset.Z);
            else
                loc = this.Location;

            return BaseHouse.FindHouseAt(loc, Map, 20);
        }

        public bool CheckAccess(Mobile m)
        {
            BaseHouse house = FindHouse();

            if (house == null)
                return false;

            if (!house.IsAosRules)
                return true;

            if (house.Public ? house.IsBanned(m) : !house.HasAccess(m))
                return false;

            return house.HasSecureAccess(m, m_Level);
        }

        public override void OnOpened(Mobile from)
        {
            BaseHouse house = FindHouse();

            if (house != null && house.Public && !house.IsFriend(from))
                house.Visits++;

            // we need to clear the ban list under certain circumstances on Siege
            //  https://uo.stratics.com/content/basics/siege_archive.shtml
            if (house != null)
                house.OnDoorOpened(from, this);
        }

        public override bool UseLocks()
        {
            BaseHouse house = FindHouse();

            return (house == null || !house.IsAosRules);
        }

        public override void Use(Mobile from)
        {
            if (!CheckAccess(from))
                from.SendLocalizedMessage(1061637); // You are not allowed to access this.
            else
                base.Use(from);
        }

        public virtual void Flip()
        {
            if (Facing == DoorFacing.NorthCW)
                Facing = DoorFacing.WestCW;
            else
                Facing = (DoorFacing)((int)Facing + 1);
        }

        public BaseHouseDoor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((int)m_Level);

            // eliminated in version 2
            // writer.Write( (int) m_Facing );
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Level = (SecureLevel)reader.ReadInt();
                        break; // NO FALLTHROUGH - eliminated saving Facing
                    }
                case 1:
                    {
                        m_Level = (SecureLevel)reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 1)
                            m_Level = SecureLevel.Anyone;

                        Facing = (DoorFacing)reader.ReadInt();
                        break;
                    }
            }
        }

        public override bool IsInside(Mobile from)
        {
            return BaseDoor.CalcInside(this, from);
        }
    }
}