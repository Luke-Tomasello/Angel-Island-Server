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

/* Scripts\Mobiles\Special\CollectionNPC.cs
 * ChangeLog:
* 2/10/22, Adam
 *      Moved CollectionNPC here to Mobiles (and mobiles namespace).
 * 12/16/21, Yoar
 *      Added CollectionNPC.
 *      Initial version.
 */

using Server.Items;
using Server.Network;
using System.Collections;

namespace Server.Mobiles
{
    public class CollectionNPC : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        public override void InitSBInfo()
        {
        }

        private CollectionBox m_Box;

        [CommandProperty(AccessLevel.GameMaster)]
        public CollectionBox Box
        {
            get { return m_Box; }
            set { m_Box = value; }
        }

        [Constructable]
        public CollectionNPC()
            : base("the collector")
        {
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (m_Box == null)
                return base.OnDragDrop(from, dropped);

            return m_Box.HandleDragDrop(this, from, dropped);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Box == null)
            {
                base.OnDoubleClick(from);
                return;
            }

            if (!from.InRange(this.Location, 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            m_Box.HandleDoubleClick(this, from);
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);

            if (m_Box != null)
                m_Box.HandleMovement(this, m, oldLocation);
        }

        public CollectionNPC(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((Item)m_Box);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Box = reader.ReadItem() as CollectionBox;
        }
    }
}