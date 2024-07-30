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

/* Items/Triggers/Core/TriggerSwitch.cs
 * CHANGELOG:
 * 	2/9/24, Yoar
 * 		Initial version.
 */

namespace Server.Items.Triggers
{
    public class TriggerSwitch : TriggerRelay
    {
        public override string DefaultName { get { return "Trigger Switch"; } }

        private int m_Index;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Index
        {
            get { return m_Index; }
            set { m_Index = value; }
        }

        [Constructable]
        public TriggerSwitch()
            : base()
        {
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public override bool CanTrigger(Mobile from)
        {
            if (m_Index < 0 || m_Index >= Links.Count)
                return false;

            return TriggerSystem.CanTrigger(from, Links[m_Index]);
        }

        public override void OnTrigger(Mobile from)
        {
            if (m_Index < 0 || m_Index >= Links.Count)
                return;

            TriggerSystem.CheckTrigger(from, Links[m_Index]);
        }

        public TriggerSwitch(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_Index);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Index = reader.ReadInt();

                        break;
                    }
            }
        }
    }
}