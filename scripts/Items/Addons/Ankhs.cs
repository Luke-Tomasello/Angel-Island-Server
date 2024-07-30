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

/* Scripts/Items/Addons/Ankhs.cs
 * ChangeLog
 *	7/29/23, Yoar
 *		Initial version
 */

namespace Server.Items
{
    [TypeAlias("Server.Items.AnkhNorthAddon", "Server.Items.AnkhWestAddon")]
    public class AnkhAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new AnkhAddonDeed(m_Resurrects); } }
        public override bool Redeedable { get { return true; } }

        private bool m_Resurrects;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Resurrects
        {
            get { return m_Resurrects; }
            set { m_Resurrects = value; }
        }

        [Constructable]
        public AnkhAddon()
            : this(false, 0)
        {
        }

        [Constructable]
        public AnkhAddon(bool resurrects)
            : this(resurrects, 0)
        {
        }

        [Constructable]
        public AnkhAddon(bool resurrects, int type)
            : base()
        {
            m_Resurrects = resurrects;

            switch (type)
            {
                case 0: // north
                    {
                        AddComponent(new AddonComponent(0x4), 0, 0, 0);
                        AddComponent(new AddonComponent(0x5), 1, 0, 0);
                        break;
                    }
                case 1: // west
                    {
                        AddComponent(new AddonComponent(0x3), 0, 0, 0);
                        AddComponent(new AddonComponent(0x2), 0, 1, 0);
                        break;
                    }
            }
        }

        public override bool HandlesOnMovement { get { return m_Resurrects; } }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m_Resurrects && Utility.InRange(Location, m.Location, 1) && !Utility.InRange(Location, oldLocation, 1))
                Ankhs.Resurrect(m, this);
        }

        public override void OnDoubleClickDead(Mobile m)
        {
            if (m_Resurrects)
                Ankhs.Resurrect(m, this);
        }

        public AnkhAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((bool)m_Resurrects);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Resurrects = reader.ReadBool();
                        break;
                    }
            }
        }
    }

    [TypeAlias("Server.Items.AnkhNorthAddonDeed", "Server.Items.AnkhWestAddonDeed")]
    public class AnkhAddonDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new AnkhAddon(m_Resurrects, m_Type); } }
        public override string DefaultName { get { return "an ankh deed"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Ankh (North)",
                "Ankh (West)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        private bool m_Resurrects;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Resurrects
        {
            get { return m_Resurrects; }
            set { m_Resurrects = value; }
        }

        [Constructable]
        public AnkhAddonDeed()
            : this(false)
        {
        }

        [Constructable]
        public AnkhAddonDeed(bool resurrects)
            : base()
        {
            m_Resurrects = resurrects;
        }

        public AnkhAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((bool)m_Resurrects);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Resurrects = reader.ReadBool();
                        break;
                    }
            }
        }
    }
}