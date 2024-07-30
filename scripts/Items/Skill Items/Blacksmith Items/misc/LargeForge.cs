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

using System;

namespace Server.Items
{
    [Server.Engines.Craft.Forge]
    public class LargeForgeWest : Item
    {
        private InternalItem m_Item;
        private InternalItem2 m_Item2;

        [Constructable]
        public LargeForgeWest()
            : base(0x199A)
        {
            Movable = false;

            m_Item = new InternalItem(this);
            m_Item2 = new InternalItem2(this);
        }

        public LargeForgeWest(Serial serial)
            : base(serial)
        {
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (m_Item != null)
                m_Item.Location = new Point3D(X, Y + 1, Z);
            if (m_Item2 != null)
                m_Item2.Location = new Point3D(X, Y + 2, Z);
        }

        public override void OnMapChange()
        {
            if (m_Item != null)
                m_Item.Map = Map;
            if (m_Item2 != null)
                m_Item2.Map = Map;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Item != null)
                m_Item.Delete();
            if (m_Item2 != null)
                m_Item2.Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Item);
            writer.Write(m_Item2);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Item = reader.ReadItem() as InternalItem;
            m_Item2 = reader.ReadItem() as InternalItem2;
        }

        [Server.Engines.Craft.Forge]
        private class InternalItem : Item
        {
            private LargeForgeWest m_Item;

            public InternalItem(LargeForgeWest item)
                : base(0x1996)
            {
                Movable = false;

                m_Item = item;
            }

            public InternalItem(Serial serial)
                : base(serial)
            {
            }

            public override void OnLocationChange(Point3D oldLocation)
            {
                if (m_Item != null)
                    m_Item.Location = new Point3D(X, Y - 1, Z);
            }

            public override void OnMapChange()
            {
                if (m_Item != null)
                    m_Item.Map = Map;
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                if (m_Item != null)
                    m_Item.Delete();
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0); // version

                writer.Write(m_Item);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                m_Item = reader.ReadItem() as LargeForgeWest;
            }
        }

        [Server.Engines.Craft.Forge]
        private class InternalItem2 : Item
        {
            private LargeForgeWest m_Item;

            public InternalItem2(LargeForgeWest item)
                : base(0x1992)
            {
                Movable = false;

                m_Item = item;
            }

            public InternalItem2(Serial serial)
                : base(serial)
            {
            }

            public override void OnLocationChange(Point3D oldLocation)
            {
                if (m_Item != null)
                    m_Item.Location = new Point3D(X, Y - 2, Z);
            }

            public override void OnMapChange()
            {
                if (m_Item != null)
                    m_Item.Map = Map;
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                if (m_Item != null)
                    m_Item.Delete();
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0); // version

                writer.Write(m_Item);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                m_Item = reader.ReadItem() as LargeForgeWest;
            }
        }
    }

    [Server.Engines.Craft.Forge]
    public class LargeForgeEast : Item
    {
        private InternalItem m_Item;
        private InternalItem2 m_Item2;

        [Constructable]
        public LargeForgeEast()
            : base(0x197A)
        {
            Movable = false;

            m_Item = new InternalItem(this);
            m_Item2 = new InternalItem2(this);
        }

        public LargeForgeEast(Serial serial)
            : base(serial)
        {
        }
        public override Item Dupe(int amount)
        {   // these composite items are problematic because we have no BaseAddon which we would used to reconstruct
            //  Instead we just dupe the graphic.
            Item new_component = new Item(this.ItemID);
            Utility.CopyProperties(new_component, this);
            return base.Dupe(new_component, amount);
        }
        public override void OnLocationChange(Point3D oldLocation)
        {
            if (m_Item != null)
                m_Item.Location = new Point3D(X + 1, Y, Z);
            if (m_Item2 != null)
                m_Item2.Location = new Point3D(X + 2, Y, Z);
        }

        public override void OnMapChange()
        {
            if (m_Item != null)
                m_Item.Map = Map;
            if (m_Item2 != null)
                m_Item2.Map = Map;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Item != null)
                m_Item.Delete();
            if (m_Item2 != null)
                m_Item2.Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Item);
            writer.Write(m_Item2);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Item = reader.ReadItem() as InternalItem;
            m_Item2 = reader.ReadItem() as InternalItem2;
        }

        [Server.Engines.Craft.Forge]
        private class InternalItem : Item
        {
            private LargeForgeEast m_Item;

            public InternalItem(LargeForgeEast item)
                : base(0x197E)
            {
                Movable = false;

                m_Item = item;
            }

            public InternalItem(Serial serial)
                : base(serial)
            {
            }
            public override Item Dupe(int amount)
            {   // these composite items are problematic because we have no BaseAddon which we would used to reconstruct
                //  Instead we just dupe the graphic.
                Item new_component = new Item(this.ItemID);
                Utility.CopyProperties(new_component, this);
                return base.Dupe(new_component, amount);
            }
            public override void OnLocationChange(Point3D oldLocation)
            {
                if (m_Item != null)
                    m_Item.Location = new Point3D(X - 1, Y, Z);
            }

            public override void OnMapChange()
            {
                if (m_Item != null)
                    m_Item.Map = Map;
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                if (m_Item != null)
                    m_Item.Delete();
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0); // version

                writer.Write(m_Item);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                m_Item = reader.ReadItem() as LargeForgeEast;
            }
        }

        [Server.Engines.Craft.Forge]
        private class InternalItem2 : Item
        {
            private LargeForgeEast m_Item;

            public InternalItem2(LargeForgeEast item)
                : base(0x1982)
            {
                Movable = false;

                m_Item = item;
            }

            public InternalItem2(Serial serial)
                : base(serial)
            {
            }
            public override Item Dupe(int amount)
            {   // these composite items are problematic because we have no BaseAddon which we would used to reconstruct
                //  Instead we just dupe the graphic.
                Item new_component = new Item(this.ItemID);
                Utility.CopyProperties(new_component, this);
                return base.Dupe(new_component, amount);
            }
            public override void OnLocationChange(Point3D oldLocation)
            {
                if (m_Item != null)
                    m_Item.Location = new Point3D(X - 2, Y, Z);
            }

            public override void OnMapChange()
            {
                if (m_Item != null)
                    m_Item.Map = Map;
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                if (m_Item != null)
                    m_Item.Delete();
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0); // version

                writer.Write(m_Item);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                m_Item = reader.ReadItem() as LargeForgeEast;
            }
        }
    }
}