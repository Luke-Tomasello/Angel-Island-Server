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

/* Scripts/Items/Skill Items/Fishing/Misc/MessageInABottle.cs
 * CHANGELOG:
 *	9/3/10, Adam
 *		Add in a notion of level
 *	6/29/04 - Pix
 *		Temporary fix for Mibs/SOSs set for Trammel
 */

namespace Server.Items
{
    public class MessageInABottle : Item
    {
        public override int LabelNumber { get { return 1041080; } } // a message in a bottle

        private Map m_TargetMap;
        private int m_level;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level
        {
            get { return m_level; }
            set { m_level = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map TargetMap
        {
            get { return m_TargetMap; }
            set { m_TargetMap = value; }
        }

        [Constructable]
        public MessageInABottle()
            : this(Map.Felucca)
        {
        }

        [Constructable]
        public MessageInABottle(Map map)
            : this(map, 0)
        {
            Weight = 1.0;
            m_TargetMap = map;
        }

        [Constructable]
        public MessageInABottle(Map map, int level)
            : base(0x099F)
        {
            Weight = 1.0;
            m_level = level;
            m_TargetMap = map;
        }

        public MessageInABottle(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 2
            writer.Write(m_level);

            // version 1
            writer.Write(m_TargetMap);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_level = reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        m_TargetMap = reader.ReadMap();
                        break;
                    }
                case 0:
                    {
                        m_TargetMap = Map.Trammel;
                        break;
                    }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                Consume();
                from.AddToBackpack(new SOS(m_TargetMap, m_level));
                from.LocalOverheadMessage(Network.MessageType.Regular, 0x3B2, 501891); // You extract the message from the bottle.
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }
    }
}