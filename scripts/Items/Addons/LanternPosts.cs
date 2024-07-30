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

/* Scripts/Items/Addons/LanternPost.cs
 * ChangeLog
 *  8/8/23, Yoar
 *      Now deriving from BaseAddonLight base class
 *	7/27/23, Yoar
 *		Initial version
 */

using Server.Township;

namespace Server.Items
{
    [TownshipAddon]
    public class LanternPostEastAddon : BaseAddonLight
    {
        public override bool Redeedable { get { return m_Legacy; } }
        public override BaseAddonDeed Deed { get { return new LanternPostEastAddonDeed(m_Legacy); } }

        // older versions of this addon are redeedable
        private bool m_Legacy;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Legacy
        {
            get { return m_Legacy; }
            set { m_Legacy = value; }
        }

        [Constructable]
        public LanternPostEastAddon()
            : this(false)
        {
        }

        [Constructable]
        public LanternPostEastAddon(bool legacy)
            : base(0xA18, 0xA15)
        {
            AddComponent(new AddonComponent(0xA1F), 0, 0, 0);

            m_Light.Z += 9;

            m_Legacy = legacy;
        }

        public LanternPostEastAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((bool)m_Legacy);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Legacy = reader.ReadBool();

                        goto case 1;
                    }
                case 1: break;
                case 0:
                    {
                        m_Light = reader.ReadItem();

                        break;
                    }
            }

            if (version < 2)
                m_Legacy = true;
        }
    }

    public class LanternPostEastAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LanternPostEastAddon(m_Legacy); } }
        public override string DefaultName { get { return "lantern post (east)"; } }

        // older versions of this addon are redeedable
        private bool m_Legacy;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Legacy
        {
            get { return m_Legacy; }
            set { m_Legacy = value; }
        }

        [Constructable]
        public LanternPostEastAddonDeed()
            : this(false)
        {
        }

        [Constructable]
        public LanternPostEastAddonDeed(bool legacy)
        {
            m_Legacy = legacy;
        }

        public LanternPostEastAddonDeed(Serial serial)
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

    [TownshipAddon]
    public class LanternPostSouthAddon : BaseAddonLight
    {
        public override bool Redeedable { get { return m_Legacy; } }
        public override BaseAddonDeed Deed { get { return new LanternPostSouthAddonDeed(m_Legacy); } }

        // older versions of this addon are redeedable
        private bool m_Legacy;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Legacy
        {
            get { return m_Legacy; }
            set { m_Legacy = value; }
        }

        [Constructable]
        public LanternPostSouthAddon()
            : this(false)
        {
        }

        [Constructable]
        public LanternPostSouthAddon(bool legacy)
            : base(0xA1D, 0xA1A)
        {
            AddComponent(new AddonComponent(0xA20), 0, 0, 0);

            m_Light.Z += 9;

            m_Legacy = legacy;
        }

        public LanternPostSouthAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((bool)m_Legacy);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Legacy = reader.ReadBool();

                        goto case 1;
                    }
                case 1: break;
                case 0:
                    {
                        m_Light = reader.ReadItem();

                        break;
                    }
            }

            if (version < 2)
                m_Legacy = true;
        }
    }

    public class LanternPostSouthAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LanternPostSouthAddon(m_Legacy); } }
        public override string DefaultName { get { return "lantern post (south)"; } }

        // older versions of this addon are redeedable
        private bool m_Legacy;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Legacy
        {
            get { return m_Legacy; }
            set { m_Legacy = value; }
        }

        [Constructable]
        public LanternPostSouthAddonDeed()
            : this(false)
        {
        }

        [Constructable]
        public LanternPostSouthAddonDeed(bool legacy)
        {
            m_Legacy = legacy;
        }

        public LanternPostSouthAddonDeed(Serial serial)
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