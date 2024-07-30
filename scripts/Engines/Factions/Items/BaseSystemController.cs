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

namespace Server.Factions
{
    public abstract class BaseSystemController : Item
    {
        private int m_LabelNumber;

        public virtual int DefaultLabelNumber { get { return base.LabelNumber; } }
        public new virtual string DefaultName { get { return null; } }

        public override int LabelNumber
        {
            get
            {
                if (m_LabelNumber > 0)
                    return m_LabelNumber;

                return DefaultLabelNumber;
            }
        }

        public virtual void AssignName(TextDefinition name)
        {
            if (name != null && name.Number > 0)
            {
                m_LabelNumber = name.Number;
                Name = null;
            }
            else if (name != null && name.String != null)
            {
                m_LabelNumber = 0;
                Name = name.String;
            }
            else
            {
                m_LabelNumber = 0;
                Name = DefaultName;
            }

            InvalidateProperties();
        }

        public BaseSystemController(int itemID) : base(itemID)
        {
        }

        public BaseSystemController(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}