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

namespace Server.Items
{
    public class LocalizedSign : Sign
    {
        private int m_LabelNumber;

        public override int LabelNumber { get { return m_LabelNumber; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Number { get { return m_LabelNumber; } set { m_LabelNumber = value; InvalidateProperties(); } }

        [Constructable]
        public LocalizedSign(SignType type, SignFacing facing, int labelNumber)
            : base((0xB95 + (2 * (int)type)) + (int)facing)
        {
            m_LabelNumber = labelNumber;
        }

        [Constructable]
        public LocalizedSign(int itemID, int labelNumber)
            : base(itemID)
        {
            m_LabelNumber = labelNumber;
        }

        public LocalizedSign(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

            writer.Write(m_LabelNumber);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_LabelNumber = reader.ReadInt();
                        break;
                    }
            }
        }
    }
}