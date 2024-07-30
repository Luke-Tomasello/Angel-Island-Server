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

/* Scripts/Items/Addons/LampPosts.cs
 * ChangeLog
 *	8/8/23, Yoar
 *		Initial version
 */

using Server.Township;

namespace Server.Items
{
    #region BaseAddonLight

    public abstract class BaseAddonLight : BaseAddon
    {
        protected Item m_Light;

        [Constructable]
        public BaseAddonLight(int unlitItemID, int litItemID)
        {
            m_Light = new InternalLight(this, unlitItemID, litItemID);
        }

        public override void OnLocationChange(Point3D oldLoc)
        {
            base.OnLocationChange(oldLoc);

            if (m_Light != null)
                m_Light.Location += (Location - oldLoc);
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            if (m_Light != null)
                m_Light.Map = Map;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Light != null)
                m_Light.Delete();
        }

        public BaseAddonLight(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((Item)m_Light);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = PeekInt(reader);

            if (version == 0)
                return; // old version

            reader.ReadInt(); // consume version

            switch (version)
            {
                case 1:
                    {
                        m_Light = reader.ReadItem();

                        break;
                    }
            }
        }

        private static int PeekInt(GenericReader reader)
        {
            int result = reader.ReadInt();
            reader.Seek(-4, System.IO.SeekOrigin.Current);
            return result;
        }

        [TypeAlias(
            "Server.Items.LanternPostEastAddon+InternalItem",
            "Server.Items.LanternPostSouthAddon+InternalItem")]
        public class InternalLight : BaseLight, IChopable
        {
            public override int UnlitItemID { get { return m_UnlitItemID; } }
            public override int LitItemID { get { return m_LitItemID; } }

            private BaseAddon m_Addon;
            private int m_UnlitItemID;
            private int m_LitItemID;

            public InternalLight(BaseAddon addon, int unlitItemID, int litItemID)
                : base(unlitItemID)
            {
                Movable = false;
                Light = LightType.Circle300;

                m_Addon = addon;
                m_UnlitItemID = unlitItemID;
                m_LitItemID = litItemID;
            }

            // TODO: Update addon map/location

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                if (m_Addon != null)
                    m_Addon.Delete();
            }

            #region IChopable

            public void OnChop(Mobile from)
            {
                if (m_Addon != null && from.InRange(GetWorldLocation(), 3))
                    m_Addon.OnChop(from);
                else
                    from.SendLocalizedMessage(500446); // That is too far away.
            }

            #endregion

            public InternalLight(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)1); // version

                writer.Write((Item)m_Addon);
                writer.Write((int)m_UnlitItemID);
                writer.Write((int)m_LitItemID);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            m_Addon = reader.ReadItem() as BaseAddon;
                            m_UnlitItemID = reader.ReadInt();
                            m_LitItemID = reader.ReadInt();

                            break;
                        }
                    case 0:
                        {
                            if (World.LoadingType == "Server.Items.LanternPostEastAddon+InternalItem")
                            {
                                m_UnlitItemID = 0xA18;
                                m_LitItemID = 0xA15;
                            }
                            else if (World.LoadingType == "Server.Items.LanternPostSouthAddon+InternalItem")
                            {
                                m_UnlitItemID = 0xA1D;
                                m_LitItemID = 0xA1A;
                            }

                            break;
                        }
                }
            }
        }
    }

    #endregion

    [TownshipAddon]
    public class LampPostSquareAddon : BaseAddonLight
    {
        public override bool Redeedable { get { return m_Legacy; } }
        public override BaseAddonDeed Deed { get { return new LampPostSquareAddonDeed(m_Legacy); } }

        // older versions of this addon are redeedable
        private bool m_Legacy;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Legacy
        {
            get { return m_Legacy; }
            set { m_Legacy = value; }
        }

        [Constructable]
        public LampPostSquareAddon()
            : this(false)
        {
        }

        [Constructable]
        public LampPostSquareAddon(bool legacy)
            : base(0xB21, 0xB20)
        {
            AddComponent(new AddonComponent(0x1), 0, 0, 0); // dummy component

            m_Legacy = legacy;
        }

        public LampPostSquareAddon(Serial serial)
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

    public class LampPostSquareAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LampPostSquareAddon(m_Legacy); } }
        public override string DefaultName { get { return "lamp post (square)"; } }

        // older versions of this addon are redeedable
        private bool m_Legacy;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Legacy
        {
            get { return m_Legacy; }
            set { m_Legacy = value; }
        }

        [Constructable]
        public LampPostSquareAddonDeed()
            : this(false)
        {
        }

        [Constructable]
        public LampPostSquareAddonDeed(bool legacy)
        {
            m_Legacy = legacy;
        }

        public LampPostSquareAddonDeed(Serial serial)
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
    public class LampPostRoundAddon : BaseAddonLight
    {
        public override bool Redeedable { get { return m_Legacy; } }
        public override BaseAddonDeed Deed { get { return new LampPostRoundAddonDeed(m_Legacy); } }

        // older versions of this addon are redeedable
        private bool m_Legacy;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Legacy
        {
            get { return m_Legacy; }
            set { m_Legacy = value; }
        }

        [Constructable]
        public LampPostRoundAddon()
            : this(false)
        {
        }

        [Constructable]
        public LampPostRoundAddon(bool legacy)
            : base(0xB23, 0xB22)
        {
            AddComponent(new AddonComponent(0x1), 0, 0, 0); // dummy component

            m_Legacy = legacy;
        }

        public LampPostRoundAddon(Serial serial)
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

    public class LampPostRoundAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LampPostRoundAddon(m_Legacy); } }
        public override string DefaultName { get { return "lamp post (round)"; } }

        // older versions of this addon are redeedable
        private bool m_Legacy;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Legacy
        {
            get { return m_Legacy; }
            set { m_Legacy = value; }
        }

        [Constructable]
        public LampPostRoundAddonDeed()
            : this(false)
        {
        }

        [Constructable]
        public LampPostRoundAddonDeed(bool legacy)
        {
            m_Legacy = legacy;
        }

        public LampPostRoundAddonDeed(Serial serial)
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
    public class LampPostOrnateAddon : BaseAddonLight
    {
        public override bool Redeedable { get { return m_Legacy; } }
        public override BaseAddonDeed Deed { get { return new LampPostOrnateAddonDeed(m_Legacy); } }

        // older versions of this addon are redeedable
        private bool m_Legacy;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Legacy
        {
            get { return m_Legacy; }
            set { m_Legacy = value; }
        }

        [Constructable]
        public LampPostOrnateAddon()
            : this(false)
        {
        }

        [Constructable]
        public LampPostOrnateAddon(bool legacy)
            : base(0xB25, 0xB24)
        {
            AddComponent(new AddonComponent(0x1), 0, 0, 0); // dummy component

            m_Legacy = legacy;
        }

        public LampPostOrnateAddon(Serial serial)
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

    public class LampPostOrnateAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LampPostOrnateAddon(m_Legacy); } }
        public override string DefaultName { get { return "lamp post (ornate)"; } }

        // older versions of this addon are redeedable
        private bool m_Legacy;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Legacy
        {
            get { return m_Legacy; }
            set { m_Legacy = value; }
        }

        [Constructable]
        public LampPostOrnateAddonDeed()
            : this(false)
        {
        }

        [Constructable]
        public LampPostOrnateAddonDeed(bool legacy)
        {
            m_Legacy = legacy;
        }

        public LampPostOrnateAddonDeed(Serial serial)
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