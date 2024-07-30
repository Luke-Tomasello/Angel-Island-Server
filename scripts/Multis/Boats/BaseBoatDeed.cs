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

/* Scripts/Multis/Boats/BaseBoatDeed.cs
 * ChangeLog
 *  9/2/22, Yoar (WorldZone)
 *      Added WorldZone check in order to contain players within the world zone.
 *	6/10/10, Adam
 *		Have placement call new Server.Spells.SpellHelper.IsDungeonRules() function.
 *		This is needed since we moved away from the static regions of RunUO
 *	5/29/10, Adam
 *		Make holds now lockable		
 *			i.e., boat.Hold.KeyValue = keyValue;
 *	6/10/04, mith
 *		Removed newbieness of boat deeds
 */

using Server.Regions;
using Server.Targeting;

namespace Server.Multis
{
    public abstract class BaseBoatDeed : Item
    {
        //public
        private int m_MultiID;
        private Point3D m_Offset;

        [CommandProperty(AccessLevel.GameMaster)]
        public int MultiID { get { return m_MultiID; } set { m_MultiID = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Offset { get { return m_Offset; } set { m_Offset = value; } }

        public BaseBoatDeed(int id, Point3D offset)
            : base(0x14F2)
        {
            Weight = 1.0;

            //			if ( !Core.AOS )
            //				LootType = LootType.Newbied;

            m_MultiID = id & 0x3FFF;
            m_Offset = offset;
        }

        public BaseBoatDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_MultiID);
            writer.Write(m_Offset);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_MultiID = reader.ReadInt();
                        m_Offset = reader.ReadPoint3D();

                        break;
                    }
            }

            if (Weight == 0.0)
                Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.SendLocalizedMessage(502482); // Where do you wish to place the ship?

                from.Target = new InternalTarget(this);
            }
        }

        public abstract BaseBoat Boat { get; }

        public void OnPlacement(Mobile from, Point3D p)
        {
            if (Deleted)
            {
                return;
            }
            else if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                Map map = from.Map;

                if (map == null)
                    return;

                if (from.AccessLevel < AccessLevel.GameMaster && (map == Map.Ilshenar || map == Map.Malas))
                {
                    from.SendLocalizedMessage(1043284); // A ship can not be created here.
                    return;
                }

                BaseBoat boat = Boat;

                if (boat == null)
                    return;

                p = new Point3D(p.X - m_Offset.X, p.Y - m_Offset.Y, p.Z - m_Offset.Z);

                if (BaseBoat.IsValidLocation(from, p, map) && boat.CanFit(p, map))
                {
                    Delete();

                    boat.Owner = from;
                    boat.Anchored = true;

                    uint keyValue = boat.CreateKeys(from);

                    if (boat.PPlank != null)
                        boat.PPlank.KeyValue = keyValue;

                    if (boat.SPlank != null)
                        boat.SPlank.KeyValue = keyValue;

                    if (boat.Hold != null)
                        boat.Hold.KeyValue = keyValue;

                    boat.MoveToWorld(p, map);
                }
                else
                {
                    boat.Delete();
                    from.SendLocalizedMessage(1043284); // A ship can not be created here.
                }
            }
        }

        private class InternalTarget : MultiTarget
        {
            private BaseBoatDeed m_Deed;

            public InternalTarget(BaseBoatDeed deed)
                : base(deed.MultiID, deed.Offset)
            {
                m_Deed = deed;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                IPoint3D ip = o as IPoint3D;

                if (ip != null)
                {
                    if (ip is Item)
                        ip = ((Item)ip).GetWorldTop();

                    Point3D p = new Point3D(ip);

                    Region region = Region.Find(p, from.Map);

                    if (Misc.WinterEventSystem.Contains(p, from.Map))
                        from.SendLocalizedMessage(1010567); // You may not place a boat from this location.
                    else if (from.Region != null && from.Region.IsDungeonRules)
                        from.SendLocalizedMessage(502488); // You can not place a ship inside a dungeon.
                    else if (region is HouseRegion)
                        from.SendLocalizedMessage(1042549); // A boat may not be placed in this area.
                    #region World Zone
                    else if (from.AccessLevel == AccessLevel.Player && WorldZone.IsOutside(p, from.Map))
                        from.SendLocalizedMessage(1042549); // A boat may not be placed in this area.
                    #endregion
                    else
                        m_Deed.OnPlacement(from, p);
                }
            }
        }
    }
}