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

/* Server\Items\BankBox.cs
 * Changelog
 *  6/7/07, Adam
 *      - I don't think bankbox.m_Open should ever have been saved, but we force a Close() now so that the Freeze Dry system can FD the box if the 
 *          server saved while a player had the box open (can remained orphaned in the open position if the user never logs back in.)
 *      - renamed to BankBox.cs
 *	12/20/05 Taran Kain
 *		Removed FreezeTimer logic from Serialization ctor, now handled in base class Container
 *	12/18/05 Taran Kain
 *		Overrode new CanFreezeDry and CheckFreezeDry() functions - Bank will now only freeze after Close() has been called, due to movement or other means
 *		(Even if bankbox gump isn't actually open - m_Open might be wrong when it's set to true but it will always be right when it's set to false)
 *	12/17/05, Adam
 *		true was causing players to crash on recall - reset to false
 *		private static bool m_SendRemovePacket = false;
 *	12/14/05 Taran Kain
 *		Completed BankBox modifications for FreezeDry system
 *		Set SendRemoveOnClose to true by default
 */

using Server.Network;
using System;

namespace Server.Items
{
    public class BankBox : Container
    {
        private Mobile m_Owner;
        private bool m_Open;

        public override int MaxWeight
        {
            get
            {
                return 0;
            }
        }

        public BankBox(Serial serial)
            : base(serial)
        {
        }

        public Mobile Owner
        {
            get
            {
                return m_Owner;
            }
        }

        public bool Opened
        {
            get
            {
                return m_Open;
            }
        }

        public void Open()
        {
            m_Open = true;

            if (m_Owner != null)
            {
                m_Owner.PrivateOverheadMessage(MessageType.Regular, 0x3B2, true, String.Format("Bank container has {0} items, {1} stones", TotalItems, TotalWeight), m_Owner.NetState);
                m_Owner.Send(new EquipUpdate(this));
                DisplayTo(m_Owner);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((Mobile)m_Owner);
            writer.Write((bool)m_Open);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Owner = reader.ReadMobile();
                        m_Open = reader.ReadBool();
                        break;
                    }
            }

            if (m_Owner == null)
                Delete();

            // Adam: don't think m_Open should ever have been saved, but we force a close so that the Freeze Dry system can FD the box
            if (m_Open == true)
                Close();
        }

        // Adam: true was causing players to crash on recall
        private static bool m_SendRemovePacket = false;

        public static bool SendDeleteOnClose { get { return m_SendRemovePacket; } set { m_SendRemovePacket = value; } }

        public void Close()
        {
            m_Open = false;

            if (m_Owner != null && m_SendRemovePacket)
                m_Owner.Send(this.RemovePacket);
        }

        public override void OnSingleClick(Mobile from)
        {
            if (from.AccessLevel == AccessLevel.Owner)
                base.OnSingleClick(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel == AccessLevel.Owner)
                base.OnDoubleClick(from);
        }

        public override DeathMoveResult OnParentDeath(Mobile parent)
        {
            return DeathMoveResult.RemainEquiped;
        }

        public override int DefaultGumpID { get { return 0x4A; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(18, 105, 144, 73); }
        }

        public BankBox(Mobile owner)
            : base(0xE41)
        {
            Layer = Layer.Bank;
            Movable = false;
            m_Owner = owner;

            //m_Freeze = new FreezeTimer(this);
            //m_Freeze.Start();
        }

        public override bool IsAccessibleTo(Mobile check)
        {
            if ((check == m_Owner && m_Open) || check.AccessLevel >= AccessLevel.GameMaster)
                return base.IsAccessibleTo(check);
            else
                return false;
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if ((from == m_Owner && m_Open) || from.AccessLevel >= AccessLevel.GameMaster)
                return base.OnDragDrop(from, dropped);
            else
                return false;
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if ((from == m_Owner && m_Open) || from.AccessLevel >= AccessLevel.GameMaster)
                return base.OnDragDropInto(from, item, p);
            else
                return false;
        }
#if false
        public override bool CanFreezeDry
        {
            get
            {
                return true;
            }
        }

        public override bool CheckFreezeDry()
        {
            return !m_Open;
        }
#endif
    }
}