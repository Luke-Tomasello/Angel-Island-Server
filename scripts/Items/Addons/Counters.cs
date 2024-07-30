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

/* Scripts/Items/Addons/Counters.cs
 * ChangeLog
 *  8/9/23, Yoar
 *      Deed now dervies from BaseChoiceAddonDeed
 *      Offering the choice of 4 variants
 *	8/8/23, Yoar
 *		Initial version
 */

namespace Server.Items
{
    [TypeAlias("Server.Items.CounterEastAddon", "Server.Items.CounterSouthAddon")]
    public class CounterAddon : BaseAddon
    {
        public override bool Redeedable { get { return m_Legacy; } }
        public override BaseAddonDeed Deed { get { return new CounterAddonDeed(m_Legacy); } }

        // older versions of this addon are redeedable
        private bool m_Legacy;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Legacy
        {
            get { return m_Legacy; }
            set { m_Legacy = value; }
        }

        [Constructable]
        public CounterAddon()
            : this(0, false)
        {
        }

        [Constructable]
        public CounterAddon(int type)
            : this(type, false)
        {
        }

        [Constructable]
        public CounterAddon(int type, bool legacy)
        {
            switch (type)
            {
                case 0:
                    AddComponent(new AddonComponent(0xB40), 0, 0, 0);
                    break;
                case 1:
                    AddComponent(new AddonComponent(0xB3F), 0, 0, 0);
                    break;
                case 2:
                    AddComponent(new AddonComponent(0xB3E), 0, 0, 0);
                    break;
                case 3:
                    AddComponent(new AddonComponent(0xB3D), 0, 0, 0);
                    break;
            }

            m_Legacy = legacy;
        }

        public CounterAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((bool)m_Legacy);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Legacy = reader.ReadBool();
                        break;
                    }
            }

            if (version < 1)
                m_Legacy = true;
        }
    }

    [TypeAlias("Server.Items.CounterEastAddonDeed", "Server.Items.CounterSouthAddonDeed")]
    public class CounterAddonDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new CounterAddon(m_Type, m_Legacy); } }
        public override string DefaultName { get { return "counter"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                1075386, // South
                1075387, // East
                "South (Legless)",
                "East (Legless)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        // older versions of this addon are redeedable
        private bool m_Legacy;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Legacy
        {
            get { return m_Legacy; }
            set { m_Legacy = value; }
        }

        [Constructable]
        public CounterAddonDeed()
            : this(false)
        {
        }

        [Constructable]
        public CounterAddonDeed(bool legacy)
        {
            m_Legacy = legacy;
        }

        public CounterAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((bool)m_Legacy);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Legacy = reader.ReadBool();
                        break;
                    }
            }

            if (version < 1)
                m_Legacy = true;
        }
    }
}