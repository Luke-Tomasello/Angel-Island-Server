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

/* Items/Games/Backgammon.cs
 * CHANGELOG:
 *	11/14/21, Yoar
 *      Renamed board game related BaseBoard to BaseGameBoard.
 */

namespace Server.Items
{
    public class BasePiece : Item
    {
        private BaseGameBoard m_Board;

        public BaseGameBoard Board
        {
            get { return m_Board; }
            set { m_Board = value; }
        }

        public override bool IsVirtualItem { get { return true; } }

        public BasePiece(int itemID, string name, BaseGameBoard board)
            : base(itemID)
        {
            m_Board = board;
            Name = name;
        }

        public BasePiece(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
            writer.Write(m_Board);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Board = (BaseGameBoard)reader.ReadItem();

                        if (m_Board == null || Parent == null)
                            Delete();

                        break;
                    }
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Board == null || m_Board.Deleted)
                Delete();
            else if (!IsChildOf(m_Board))
                m_Board.DropItem(this);
            else
                base.OnSingleClick(from);
        }

        public override bool OnDragLift(Mobile from)
        {
            if (m_Board == null || m_Board.Deleted)
            {
                Delete();
                return false;
            }
            else if (!IsChildOf(m_Board))
            {
                m_Board.DropItem(this);
                return false;
            }
            else
            {
                return true;
            }
        }

        public override bool CanTarget { get { return false; } }

        public override bool DropToMobile(Mobile from, Mobile target, Point3D p)
        {
            return false;
        }

        public override bool DropToItem(Mobile from, Item target, Point3D p)
        {
            return (target == m_Board && p.X != -1 && p.Y != -1 && base.DropToItem(from, target, p));
        }

        public override bool DropToWorld(Mobile from, Point3D p)
        {
            return false;
        }

        public override int GetLiftSound(Mobile from)
        {
            return -1;
        }
    }
}